using Celeste.Mod.Entities;
using Celeste.Mod.Meta;
using Celeste.Mod.VortexHelper.Misc;
using Celeste.Mod.VortexHelper.Misc.Extensions;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections;

namespace Celeste.Mod.VortexHelper.Entities;

[CustomEntity("VortexHelper/PurpleBooster")]
[Tracked]
public class PurpleBooster : Entity
{
    internal const string POSSIBLE_EARLY_DASHSPEED = "purpleBoostPossibleEarlyDashSpeed";
    internal const string QUALITYOFLIFEUPDATE = "purpleBoostQoL";

    private readonly Sprite sprite;
    private readonly Wiggler wiggler;
    private Entity outline;

    private readonly MTexture linkSegCenter, linkSegCenterOutline, linkSeg, linkSegOutline;

    private readonly Coroutine dashRoutine;
    private readonly DashListener dashListener;

    private float respawnTimer;
    private float cannotUseTimer;
    public bool BoostingPlayer
    {
        get;
        set;
    }
    public bool StartedBoosting;

    private bool linkVisible = false;
    private float actualLinkPercent = 1.0f;
    private float linkPercent = 1.0f;

    public static readonly ParticleType P_Burst = new(Booster.P_Burst);
    public static readonly ParticleType P_Appear = new(Booster.P_Appear);
    public static readonly ParticleType P_BurstExplode = new(Booster.P_Burst);

    private readonly SoundSource loopingSfx;
    public readonly bool QoL;
    public PurpleBooster(EntityData data, Vector2 offset)
        : this(data.Position + offset) {
        QoL = data.Bool("QoL");
    }

    public PurpleBooster(Vector2 position)
        : base(position)
    {
        this.Depth = Depths.Above;
        this.Collider = new Circle(10f, 0f, 2f);

        this.sprite = VortexHelperModule.PurpleBoosterSpriteBank.Create("purpleBooster");
        Add(this.sprite);

        Add(new PlayerCollider(OnPlayer));
        Add(new VertexLight(Color.White, 1f, 16, 32));
        Add(new BloomPoint(0.1f, 16f));
        Add(this.wiggler = Wiggler.Create(0.5f, 4f, delegate (float f)
        {
            this.sprite.Scale = Vector2.One * (1f + f * 0.25f);
        }));

        this.linkSegCenter = GFX.Game["objects/VortexHelper/slingBooster/link03"];
        this.linkSegCenterOutline = GFX.Game["objects/VortexHelper/slingBooster/link02"];
        this.linkSeg = GFX.Game["objects/VortexHelper/slingBooster/link01"];
        this.linkSegOutline = GFX.Game["objects/VortexHelper/slingBooster/link00"];

        Add(this.dashRoutine = new Coroutine(removeOnComplete: false));
        Add(this.dashListener = new DashListener());

        Add(new MirrorReflection());
        Add(this.loopingSfx = new SoundSource());

        this.dashListener.OnDash = OnPlayerDashed;
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);

        var image = new Image(GFX.Game["objects/booster/outline"]);
        image.CenterOrigin();
        image.Color = Color.White * 0.75f;

        this.outline = new Entity(this.Position)
        {
            Depth = Depths.BGDecals - 1,
            Visible = false
        };
        this.outline.Y += 2f;
        this.outline.Add(image);
        this.outline.Add(new MirrorReflection());
        scene.Add(this.outline);
    }

    private void AppearParticles()
    {
        ParticleSystem particlesBG = SceneAs<Level>().ParticlesBG;
        for (int i = 0; i < 360; i += 30)
            particlesBG.Emit(P_Appear, 1, this.Center, Vector2.One * 2f, i * ((float) Math.PI / 180f));
    }

    private void OnPlayer(Player player)
    {
        if (this.respawnTimer <= 0f && this.cannotUseTimer <= 0f && !this.BoostingPlayer)
        {
            this.linkPercent = this.actualLinkPercent = 1.0f;
            this.linkVisible = false;

            this.cannotUseTimer = 0.45f;

            Boost(player, this);

            Audio.Play(SFX.game_04_greenbooster_enter, this.Position);
            this.wiggler.Start();
            this.sprite.Play("inside", false, false);
        }
    }

    public static void Boost(Player player, PurpleBooster booster)
    {
        player.StateMachine.State = VortexHelperModule.PurpleBoosterState;
        player.Speed = Vector2.Zero;
        DynamicData playerData = DynamicData.For(player);
        playerData.Set("boostTarget", booster.Center);
        playerData.Set(QUALITYOFLIFEUPDATE, booster.QoL);
        booster.StartedBoosting = true;
    }

    public void PlayerBoosted(Player player, Vector2 direction)
    {
        this.StartedBoosting = false;
        this.BoostingPlayer = false;
        this.linkVisible = true;

        Audio.Play(SFX.game_04_greenbooster_dash, this.Position);

        this.loopingSfx.Play(SFX.game_05_redbooster_move_loop);
        this.loopingSfx.DisposeOnTransition = false;

        this.BoostingPlayer = true;
        this.Tag = Tags.Persistent | Tags.TransitionUpdate;

        this.sprite.Play("spin", false, false);
        this.wiggler.Start();
        this.dashRoutine.Replace(BoostRoutine(player, direction));
    }

    private IEnumerator BoostRoutine(Player player, Vector2 dir)
    {
        Level level = SceneAs<Level>();

        while (player.StateMachine.State == VortexHelperModule.PurpleBoosterDashState && this.BoostingPlayer)
        {
            if (player.Dead)
                PlayerDied();
            else
            {
                this.sprite.RenderPosition = player.Center;
                this.loopingSfx.Position = this.sprite.Position;

                if (this.Scene.OnInterval(0.02f))
                    level.ParticlesBG.Emit(P_Burst, 2, player.Center - dir * 3f + new Vector2(0f, -2f), new Vector2(3f, 3f));

                yield return null;
            }
        }

        PlayerReleased();

        if (player.StateMachine.State == Player.StBoost)
        {
            this.sprite.Visible = false;
        }

        this.linkVisible = player.StateMachine.State is Player.StDash or Player.StNormal;
        this.linkPercent = this.linkVisible ? 0.0f : 1.0f;

        if (!this.linkVisible)
            LaunchPlayerParticles(player, -dir, P_BurstExplode);

        while (SceneAs<Level>().Transitioning)
            yield return null;

        this.Tag = 0;
        yield break;
    }

    private void OnPlayerDashed(Vector2 direction)
    {
        if (this.BoostingPlayer)
            this.BoostingPlayer = false;
    }

    private void PlayerReleased()
    {
        Audio.Play(SFX.game_05_redbooster_end, this.sprite.RenderPosition);
        this.sprite.Play("pop");
        this.cannotUseTimer = 0f;
        this.respawnTimer = 1f;
        this.BoostingPlayer = false;
        this.outline.Visible = true;
        this.loopingSfx.Stop();
    }

    private void PlayerDied()
    {
        if (!this.BoostingPlayer)
            return;

        PlayerReleased();
        this.dashRoutine.Active = false;
        this.Tag = 0;
    }

    private void Respawn()
    {
        Audio.Play(SFX.game_04_greenbooster_reappear, this.Position);

        this.sprite.Position = Vector2.Zero;
        this.sprite.Play("appear", restart: true);
        this.sprite.Visible = true;

        this.outline.Visible = false;
        AppearParticles();
    }

    public override void Update()
    {
        base.Update();

        this.actualLinkPercent = Calc.Approach(this.actualLinkPercent, this.linkPercent, 5f * Engine.DeltaTime);

        if (this.cannotUseTimer > 0f)
            this.cannotUseTimer -= Engine.DeltaTime;

        if (this.respawnTimer > 0f)
        {
            this.respawnTimer -= Engine.DeltaTime;
            if (this.respawnTimer <= 0f)
                Respawn();
        }

        if (!this.dashRoutine.Active && this.respawnTimer <= 0f)
        {
            Vector2 target = Vector2.Zero;
            Player entity = this.Scene.Tracker.GetEntity<Player>();
            if (entity is not null && CollideCheck(entity))
                target = entity.Center + Booster.playerOffset - this.Position;
            this.sprite.Position = Calc.Approach(this.sprite.Position, target, 80f * Engine.DeltaTime);
        }

        if (this.sprite.CurrentAnimationID == "inside" && !this.BoostingPlayer && !CollideCheck<Player>())
            this.sprite.Play("loop");
    }

    public static void LaunchPlayerParticles(Player player, Vector2 dir, ParticleType p)
    {
        Level level = player.SceneAs<Level>();
        float angle = dir.Angle() - 0.5f;
        for (int i = 0; i < 20; i++)
            level.ParticlesBG.Emit(p, 1, player.Center, new Vector2(3f, 3f), angle + Calc.Random.NextFloat());
    }

    public override void Render()
    {
        Vector2 position = this.sprite.Position;
        this.sprite.Position = position.Floor();

        if (this.sprite.CurrentAnimationID != "pop" && this.sprite.Visible)
            this.sprite.DrawOutline();

        if (this.linkVisible)
            RenderPurpleBoosterLink(12, 0.35f);

        base.Render();
        this.sprite.Position = position;
    }

    private void RenderPurpleBoosterLink(int spriteCount, float minScale)
    {
        float increment = 1f / spriteCount;
        float centerSegmentScale = 0.7f + 0.3f * this.actualLinkPercent;

        this.linkSegCenterOutline.DrawOutlineCentered(this.Center, Color.Black, centerSegmentScale);
        for (float t = increment; t <= this.actualLinkPercent; t += increment) // Black Outline
        {
            var vec = Vector2.Lerp(this.Center, this.sprite.RenderPosition, t * this.actualLinkPercent);
            this.linkSegOutline.DrawOutlineCentered(vec, Color.Black, 1.01f - t * minScale);
        }

        this.linkSegCenterOutline.DrawCentered(this.Center, Color.White, centerSegmentScale);
        for (float t = increment; t <= this.actualLinkPercent; t += increment) // Pink Outline
        {
            var vec = Vector2.Lerp(this.Center, this.sprite.RenderPosition, t * this.actualLinkPercent);
            this.linkSegOutline.DrawCentered(vec, Color.White, 1.01f - t * minScale);
        }

        this.linkSegCenter.DrawCentered(this.Center, Color.White, centerSegmentScale); // Sprites
        for (float t = increment; t <= this.actualLinkPercent; t += increment)
        {
            var vec = Vector2.Lerp(this.Center, this.sprite.RenderPosition, t * this.actualLinkPercent);
            this.linkSeg.DrawCentered(vec, Color.White, 1f - t * minScale);
        }
    }

    public static void InitializeParticles()
    {
        P_Burst.Color = Calc.HexToColor("8c2c95");
        P_Appear.Color = Calc.HexToColor("b64acf");

        P_BurstExplode.Color = P_Burst.Color;
        P_BurstExplode.SpeedMax = 250; // felt like good value
    }

    #region Custom Purple Booster Behavior
    // TODO: Merge the two states into one. Don't know why I separated them...

    // Inside the Purple Booster
    public static void PurpleBoostBegin()
    {
        Util.TryGetPlayer(out Player player);
        player.CurrentBooster = null;

        // Fixes hair sticking out of the bubble sprite when entering it ducking.
        // If for whatever reason this breaks an older map, this will be removed.
        player.Ducking = false;

        Level level = player.SceneAs<Level>();

        bool? flag;
        if (level is null)
            flag = null;
        else
        {
            MapMetaModeProperties meta = level.Session.MapData.GetMeta();
            flag = meta?.TheoInBubble;
        }

        bool? flag2 = flag;
        player.RefillDash();
        player.RefillStamina();

        if (flag2.GetValueOrDefault())
            return;

        player.Drop();
    }

    public static int PurpleBoostUpdate()
    {
        Util.TryGetPlayer(out Player player);
        DynamicData playerData = DynamicData.For(player);

        Vector2 boostTarget = playerData.Get<Vector2>("boostTarget");
        Vector2 value = Input.Aim.Value * 3f;
        Vector2 vector = Calc.Approach(player.ExactPosition, boostTarget - player.Collider.Center + value, 80f * Engine.DeltaTime);

        player.MoveToX(vector.X, null);
        player.MoveToY(vector.Y, null);

        if (Vector2.DistanceSquared(player.Center, boostTarget) >= 275f)
        {
            foreach (PurpleBooster b in player.Scene.Tracker.GetEntities<PurpleBooster>())
                if (b.StartedBoosting)
                    b.PlayerReleased();
            return 0;
        }

        // now supports demobutton presses
        if (Input.DashPressed || Input.CrouchDashPressed)
        {
            // we don't need to do this, we're not actually dashing here- but fastbubbling.
            //demoDashed = Input.CrouchDashPressed;
            Input.Dash.ConsumePress();
            Input.CrouchDash.ConsumeBuffer();
            return VortexHelperModule.PurpleBoosterDashState;
        }

        return VortexHelperModule.PurpleBoosterState;
    }

    public static void PurpleBoostEnd()
    {
        Util.TryGetPlayer(out Player player);
        DynamicData playerData = DynamicData.For(player);
        Vector2 boostTarget = playerData.Get<Vector2>("boostTarget");
        Vector2 vector = (boostTarget - player.Collider.Center).Floor();
        player.MoveToX(vector.X, null);
        player.MoveToY(vector.Y, null);
    }

    public static IEnumerator PurpleBoostCoroutine()
    {
        yield return 0.3f;

        Util.TryGetPlayer(out Player player);
        player.StateMachine.State = VortexHelperModule.PurpleBoosterDashState;
    }

    // Arc Motion
    public static void PurpleDashingBegin()
    {
        Celeste.Freeze(0.05f); // this freeze makes fastbubbling much more lenient

        Util.TryGetPlayer(out Player player);
        DynamicData playerData = DynamicData.For(player);
        player.DashDir = Input.GetAimVector(player.Facing);
        playerData.Set(POSSIBLE_EARLY_DASHSPEED, Vector2.Zero);

        foreach (PurpleBooster b in player.Scene.Tracker.GetEntities<PurpleBooster>())
        {
            if (b.StartedBoosting)
            {
                b.PlayerBoosted(player, player.DashDir);
                return;
            }

            if (b.BoostingPlayer)
                return;
        }
    }

    public static int PurpleDashingUpdate()
    {
        Util.TryGetPlayer(out Player player);
        DynamicData playerData = DynamicData.For(player);
        bool QoL = playerData.Get(QUALITYOFLIFEUPDATE) is bool b && b;
        if (Input.DashPressed || Input.CrouchDashPressed)
        {
            if (QoL) player.Speed += playerData.Get<Vector2>(POSSIBLE_EARLY_DASHSPEED);
            else player.LiftSpeed += playerData.Get<Vector2>(POSSIBLE_EARLY_DASHSPEED);
            return player.StartDash();
        }
        if (QoL && Math.Abs(player.DashDir.X) <= 0.02 &&
            Input.Jump.Pressed && player.CanUnDuck &&
            (player.DashDir.Y < 0 ? playerData.Get<Vector2>(POSSIBLE_EARLY_DASHSPEED).Y == 0 : playerData.Get<Vector2>(POSSIBLE_EARLY_DASHSPEED).Y < 0))
        {
            if ((bool)Util.player_WallJumpCheck.Invoke(player, new object[1]{1}))
            {
                Util.player_SuperWallJump.Invoke(player,new object[1]{-1});
                return 0;
            }else if ((bool) Util.player_WallJumpCheck.Invoke(player, new object[1]{-1}))
            {
                Util.player_SuperWallJump.Invoke(player, new object[1]{1});
                return 0;
            }
        }
        return VortexHelperModule.PurpleBoosterDashState;
    }

    public static IEnumerator PurpleDashingCoroutine()
    {
        float t = 0f;
        Util.TryGetPlayer(out Player player);
        DynamicData playerData = DynamicData.For(player);
        Vector2 origin = playerData.Get<Vector2>("boostTarget");
        if(playerData.Get(QUALITYOFLIFEUPDATE) is bool a && a) {
            yield return null;
            player.DashDir = playerData.Get<Vector2>("lastAim"); 
        }

        Vector2 earlyExitBoost = Vector2.Zero;
        while (t < 1f)
        {
            t = Calc.Approach(t, 1.0f, Engine.DeltaTime * 1.5f);
            Vector2 vec = origin + Vector2.UnitY * 6f + player.DashDir * 60f * (float) Math.Sin(t * Math.PI);
            
            if(playerData.Get(QUALITYOFLIFEUPDATE) is bool b && b)
            {
                if(t == 1f)
                {
                    // frame 0: mimics speed at launch exit exactly, Input.MoveX.Value == -Math.Sign(player.DashDir) ? 300 : 250
                    earlyExitBoost = 250f * -player.DashDir;
                    Vector2 aim = Input.GetAimVector(player.Facing).EightWayNormal().Sign();
                    if (aim.X == Math.Sign(earlyExitBoost.X)) earlyExitBoost.X *= 1.2f;
                    if (aim.Y == Math.Sign(earlyExitBoost.Y)) earlyExitBoost.Y *= 1.2f;
                } else if(t > 0.93f)
                {
                    // frame -2 : 200 speed
                    // frame -1 : 205 speed
                    earlyExitBoost = (float)Math.Round(210f * t) * -player.DashDir;
                }
            }
            else if (t > 0.6f)
            {
                earlyExitBoost = (t - .5f) * 200f * -player.DashDir;
            }
            playerData.Set(POSSIBLE_EARLY_DASHSPEED, earlyExitBoost);

            if (player.CollideCheck<Solid>(vec))
            {
                player.StateMachine.State = Player.StNormal;
                yield break;
            }
            player.MoveToX(vec.X); player.MoveToY(vec.Y);
            yield return null;
        }

        player.LiftSpeed += 120f * -player.DashDir;
        PurpleBoosterExplodeLaunch(player, playerData, player.Center - player.DashDir, origin);
    }

    public static void PurpleDashingEnd()
    {
        Util.TryGetPlayer(out Player player);
        DynamicData playerData = DynamicData.For(player);
        playerData.Set(QUALITYOFLIFEUPDATE, false);
    }
    public static void PurpleBoosterExplodeLaunch(Player player, DynamicData playerData, Vector2 from, Vector2? origin, float factor = 1f)
    {
        bool QoL = playerData?.Get(QUALITYOFLIFEUPDATE) is bool b && b;
        Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
        Celeste.Freeze(QoL ? 0.05f : 0.1f);
        playerData.Set("launchApproachX", null);
        Level level = player.SceneAs<Level>();

        if (origin is not null)
            level.Displacement.AddBurst((Vector2) origin, 0.25f, 8f, 64f, 0.5f, Ease.QuadIn, Ease.QuadOut);

        level.Shake(0.15f);

        Vector2 vector = (player.Center - from).SafeNormalize(-Vector2.UnitY);
        if (Math.Abs(vector.X) < 1f && Math.Abs(vector.Y) < 1f)
            vector *= 1.1f;

        player.Speed = 250f * -vector;

        Vector2 aim = Input.GetAimVector(player.Facing).EightWayNormal().Sign();
        if (aim.X == Math.Sign(player.Speed.X)) player.Speed.X *= 1.2f;
        if (aim.Y == Math.Sign(player.Speed.Y)) player.Speed.Y *= 1.2f;

        SlashFx.Burst(player.Center, player.Speed.Angle());
        if (!player.Inventory.NoRefills)
            player.RefillDash();
        player.RefillStamina();
        if (QoL && playerData?.Get("dashCooldownTimer") is float f)
            playerData.Set("dashCooldownTimer", f > 0.06f ? 0.06f : f);
        player.StateMachine.State = Player.StLaunch;
        player.Speed *= factor;
    }

    #endregion

    internal static class Hooks
    {

        public static void Hook()
        {
            On.Celeste.Player.ctor += Player_ctor;
            IL.Celeste.Player.WallJumpCheck += Player_WallJumpCheck;
        }


        public static void Unhook()
        {
            On.Celeste.Player.ctor -= Player_ctor;
            IL.Celeste.Player.WallJumpCheck -= Player_WallJumpCheck;
        }

        private static void Player_ctor(On.Celeste.Player.orig_ctor orig, Player self, Vector2 position, PlayerSpriteMode spriteMode)
        {
            orig(self, position, spriteMode);

            // Custom Purple Booster State
            VortexHelperModule.PurpleBoosterState = self.StateMachine.AddState(PurpleBoostUpdate, PurpleBoostCoroutine, PurpleBoostBegin, PurpleBoostEnd);

            // Custom Purple Booster State (Arc Motion)
            VortexHelperModule.PurpleBoosterDashState = self.StateMachine.AddState(PurpleDashingUpdate, PurpleDashingCoroutine, PurpleDashingBegin, PurpleDashingEnd);
        }

        private static void Player_WallJumpCheck(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            if (cursor.TryGotoNext(MoveType.After, i => i.MatchCallvirt<Player>("get_DashAttacking")))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<bool, Player, bool>>((b, p) =>
                {
                    if (b) return true;
                    try { if (DynamicData.For(p).TryGet<bool>(QUALITYOFLIFEUPDATE, out bool c) && c) return true; }
                    catch (NullReferenceException) { return false; }
                    return false;
                });
            }
            if(cursor.TryGotoNext(MoveType.After, i => i.MatchLdcR4(-1) && i.Next.MatchCeq()))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<float, Player, float>>((f, p) =>
                {
                    try { if (DynamicData.For(p).TryGet<bool>(QUALITYOFLIFEUPDATE, out bool c) && c) return p.DashDir.Y; }
                    catch (NullReferenceException) { return f; }
                    return f;
                });
            }


        }

    }
}
