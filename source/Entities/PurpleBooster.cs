using Celeste.Mod.Entities;
using Celeste.Mod.Meta;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;

namespace Celeste.Mod.VortexHelper.Entities {
    [CustomEntity("VortexHelper/PurpleBooster")]
    [Tracked]
    class PurpleBooster : Entity {
        private Sprite sprite;
        private Wiggler wiggler;
        private Entity outline;

        private MTexture linkSegCenter, linkSegCenterOutline, linkSeg, linkSegOutline;

        private Coroutine dashRoutine;
        private DashListener dashListener;

        private float respawnTimer;
        private float cannotUseTimer;
        public bool BoostingPlayer {
            get;
            set;
        }
        public bool StartedBoosting;

        private bool lavender;

        private bool linkVisible = false;
        private float actualLinkPercent = 1.0f;
        private float linkPercent = 1.0f;

        private static readonly ParticleType P_Burst = new ParticleType(Booster.P_Burst);
        private static readonly ParticleType P_Appear = new ParticleType(Booster.P_Appear);
        private static readonly ParticleType P_BurstExplode = new ParticleType(Booster.P_Burst);

        private static readonly ParticleType P_BurstLavender = new ParticleType(Booster.P_Burst);
        private static readonly ParticleType P_BurstExplodeLavender = new ParticleType(Booster.P_Burst);

        private SoundSource loopingSfx;
        public PurpleBooster(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Bool("lavender", false)) { }

        public PurpleBooster(Vector2 position, bool lavender)
            : base(position) {
            this.lavender = lavender;

            base.Depth = -8500;
            base.Collider = new Circle(10f, 0f, 2f);

            sprite = lavender ?
                VortexHelperModule.LavenderBoosterSpriteBank.Create("lavenderBooster") : VortexHelperModule.PurpleBoosterSpriteBank.Create("purpleBooster");

            Add(sprite);

            Add(new PlayerCollider(OnPlayer));
            Add(new VertexLight(Color.White, 1f, 16, 32));
            Add(new BloomPoint(0.1f, 16f));
            Add(wiggler = Wiggler.Create(0.5f, 4f, delegate (float f) {
                sprite.Scale = Vector2.One * (1f + f * 0.25f);
            }));

            linkSegCenter = GFX.Game["objects/VortexHelper/slingBooster/link03"];
            linkSegCenterOutline = GFX.Game["objects/VortexHelper/slingBooster/link02"];
            linkSeg = GFX.Game["objects/VortexHelper/slingBooster/link01"];
            linkSegOutline = GFX.Game["objects/VortexHelper/slingBooster/link00"];

            Add(dashRoutine = new Coroutine(removeOnComplete: false));
            Add(dashListener = new DashListener());

            Add(new MirrorReflection());
            Add(loopingSfx = new SoundSource());

            dashListener.OnDash = OnPlayerDashed;
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            Image image = new Image(GFX.Game["objects/booster/outline"]);
            image.CenterOrigin();
            image.Color = Color.White * 0.75f;
            outline = new Entity(Position);
            outline.Depth = 8999;
            outline.Visible = false;
            outline.Y += 2f;
            outline.Add(image);
            outline.Add(new MirrorReflection());
            scene.Add(outline);
        }

        private void AppearParticles() {
            ParticleSystem particlesBG = SceneAs<Level>().ParticlesBG;
            for (int i = 0; i < 360; i += 30) {
                particlesBG.Emit(P_Appear, 1, base.Center, Vector2.One * 2f, i * ((float)Math.PI / 180f));
            }
        }

        private void OnPlayer(Player player) {
            if (respawnTimer <= 0f && cannotUseTimer <= 0f && !BoostingPlayer) {
                linkPercent = actualLinkPercent = 1.0f;
                linkVisible = false;

                cannotUseTimer = 0.45f;

                Boost(player, this);

                Audio.Play("event:/game/04_cliffside/greenbooster_enter", Position);
                wiggler.Start();
                sprite.Play("inside", false, false);
            }
        }

        public static void Boost(Player player, PurpleBooster booster) {
            player.StateMachine.State = booster.lavender ? VortexHelperModule.LavenderBoosterState : VortexHelperModule.PurpleBoosterState;
            player.Speed = Vector2.Zero;
            DynData<Player> playerData = new DynData<Player>(player);
            playerData.Set("boostTarget", booster.Center);
            booster.StartedBoosting = true;
        }

        public void PlayerBoosted(Player player, Vector2 direction) {
            StartedBoosting = false;
            BoostingPlayer = false;
            linkVisible = true;
            Audio.Play("event:/game/04_cliffside/greenbooster_dash", Position);

            if (!lavender) {
                loopingSfx.Play("event:/game/05_mirror_temple/redbooster_move"); // temporary
            }

            loopingSfx.DisposeOnTransition = false;

            BoostingPlayer = true;
            base.Tag = (Tags.Persistent | Tags.TransitionUpdate);
            sprite.Play("spin", false, false);
            wiggler.Start();
            dashRoutine.Replace(BoostRoutine(player, direction));
        }

        private IEnumerator BoostRoutine(Player player, Vector2 dir) {
            Level level = SceneAs<Level>();
            while (player.StateMachine.State == (lavender ? 2 : VortexHelperModule.PurpleBoosterDashState) && BoostingPlayer) {
                if (player.Dead) {
                    PlayerDied();
                }
                else {
                    sprite.RenderPosition = player.Center;
                    loopingSfx.Position = sprite.Position;
                    if (Scene.OnInterval(0.02f)) {
                        level.ParticlesBG.Emit(lavender ? P_BurstLavender : P_Burst, 2, player.Center - dir * 3f + new Vector2(0f, -2f), new Vector2(3f, 3f));
                    }
                    yield return null;
                }

            }
            PlayerReleased();
            if (player.StateMachine.State == 4) {
                sprite.Visible = false;
            }
            linkVisible = player.StateMachine.State == 2 || player.StateMachine.State == 0;
            linkPercent = linkVisible ? 0.0f : 1.0f;

            if (!linkVisible) {
                float angle = (lavender ? player.DashDir : (-dir)).Angle() - 0.5f;
                for (int i = 0; i < 20; i++) {
                    level.ParticlesBG.Emit(lavender ? P_BurstExplodeLavender : P_BurstExplode, 1, player.Center, new Vector2(3f, 3f), angle + Calc.Random.NextFloat());
                }
            }
            while (SceneAs<Level>().Transitioning) {
                yield return null;
            }
            Tag = 0;
            yield break;
        }

        private void OnPlayerDashed(Vector2 direction) {
            if (BoostingPlayer && !lavender) {
                BoostingPlayer = false;
            }
        }

        private void PlayerReleased() {
            Audio.Play("event:/game/05_mirror_temple/redbooster_end", sprite.RenderPosition);
            sprite.Play("pop");
            cannotUseTimer = 0f;
            respawnTimer = 1f;
            BoostingPlayer = false;
            outline.Visible = true;
            loopingSfx.Stop();
        }

        private void PlayerDied() {
            if (BoostingPlayer) {
                PlayerReleased();
                dashRoutine.Active = false;
                base.Tag = 0;
            }
        }

        private void Respawn() {
            Audio.Play("event:/game/04_cliffside/greenbooster_reappear", Position);
            sprite.Position = Vector2.Zero;
            sprite.Play("appear", restart: true);
            sprite.Visible = true;
            outline.Visible = false;
            AppearParticles();
        }

        public override void Update() {
            base.Update();

            actualLinkPercent = Calc.Approach(actualLinkPercent, linkPercent, 5f * Engine.DeltaTime);

            if (cannotUseTimer > 0f) {
                cannotUseTimer -= Engine.DeltaTime;
            }
            if (respawnTimer > 0f) {
                respawnTimer -= Engine.DeltaTime;
                if (respawnTimer <= 0f) {
                    Respawn();
                }
            }
            if (!dashRoutine.Active && respawnTimer <= 0f) {
                Vector2 target = Vector2.Zero;
                Player entity = base.Scene.Tracker.GetEntity<Player>();
                if (entity != null && CollideCheck(entity)) {
                    target = entity.Center + Booster.playerOffset - Position;
                }
                sprite.Position = Calc.Approach(sprite.Position, target, 80f * Engine.DeltaTime);
            }
            if (sprite.CurrentAnimationID == "inside" && !BoostingPlayer && !CollideCheck<Player>()) {
                sprite.Play("loop");
            }
        }

        public override void Render() {
            Vector2 position = sprite.Position;
            sprite.Position = position.Floor();

            if (sprite.CurrentAnimationID != "pop" && sprite.Visible) {
                sprite.DrawOutline();
            }

            if (linkVisible && !lavender) {
                RenderPurpleBoosterLink(12, 0.35f);
            }

            base.Render();
            sprite.Position = position;
        }

        private void RenderPurpleBoosterLink(int spriteCount, float minScale) {
            float increment = 1f / spriteCount;
            float centerSegmentScale = 0.7f + 0.3f * actualLinkPercent;
            linkSegCenterOutline.DrawOutlineCentered(base.Center, Color.Black, centerSegmentScale);
            for (float t = increment; t <= actualLinkPercent; t += increment) // Black Outline
            {
                Vector2 vec = Vector2.Lerp(base.Center, sprite.RenderPosition, t * actualLinkPercent);
                linkSegOutline.DrawOutlineCentered(vec, Color.Black, 1.01f - (t * minScale));
            }
            linkSegCenterOutline.DrawCentered(base.Center, Color.White, centerSegmentScale);
            for (float t = increment; t <= actualLinkPercent; t += increment) // Pink Outline
            {
                Vector2 vec = Vector2.Lerp(base.Center, sprite.RenderPosition, t * actualLinkPercent);
                linkSegOutline.DrawCentered(vec, Color.White, 1.01f - (t * minScale));
            }

            linkSegCenter.DrawCentered(base.Center, Color.White, centerSegmentScale); // Sprites
            for (float t = increment; t <= actualLinkPercent; t += increment) {
                Vector2 vec = Vector2.Lerp(base.Center, sprite.RenderPosition, t * actualLinkPercent);
                linkSeg.DrawCentered(vec, Color.White, 1f - (t * minScale));
            }
        }

        public static void InitializeParticles() {
            P_Burst.Color = Calc.HexToColor("8c2c95");
            P_Appear.Color = Calc.HexToColor("b64acf");

            P_BurstExplode.Color = P_Burst.Color;
            P_BurstExplode.SpeedMax = 250; // felt like good value

            P_BurstLavender.Color = Calc.HexToColor("6a38b0");

            P_BurstExplodeLavender.Color = P_BurstLavender.Color;
            P_BurstExplodeLavender.SpeedMax = 250;
        }



        #region Custom Purple Booster Behavior

        /* Inside the Purple Booster */
        public static void PurpleBoostBegin() {
            Player player = VortexHelperModule.GetPlayer();
            player.CurrentBooster = null;
            Level level = player.SceneAs<Level>();
            bool? flag;
            if (level == null) {
                flag = null;
            }
            else {
                MapMetaModeProperties meta = level.Session.MapData.GetMeta();
                flag = ((meta != null) ? meta.TheoInBubble : null);
            }
            bool? flag2 = flag;
            player.RefillDash();
            player.RefillStamina();
            if (flag2.GetValueOrDefault()) {
                return;
            }
            player.Drop();
        }
        public static int PurpleBoostUpdate() {
            Player player = VortexHelperModule.GetPlayer();
            DynData<Player> playerData = new DynData<Player>(player);
            Vector2 boostTarget = playerData.Get<Vector2>("boostTarget");
            Vector2 value = Input.Aim.Value * 3f;
            Vector2 vector = Calc.Approach(player.ExactPosition, boostTarget - player.Collider.Center + value, 80f * Engine.DeltaTime);
            player.MoveToX(vector.X, null);
            player.MoveToY(vector.Y, null);

            // Fixing the solid - purple booster glitch, no more jank :( 
            if (Vector2.DistanceSquared(player.Center, boostTarget) >= 275f) {
                foreach (PurpleBooster b in player.Scene.Tracker.GetEntities<PurpleBooster>()) {
                    if (b.StartedBoosting) {
                        b.PlayerReleased();
                    }
                }
                return 0;
            }

            if (Input.Dash.Pressed) {
                Input.Dash.ConsumePress();
                return VortexHelperModule.PurpleBoosterDashState;
            }
            return VortexHelperModule.PurpleBoosterState;
        }
        public static void PurpleBoostEnd() {
            Player player = VortexHelperModule.GetPlayer();
            Vector2 boostTarget = new DynData<Player>(player).Get<Vector2>("boostTarget");
            Vector2 vector = (boostTarget - player.Collider.Center).Floor();
            player.MoveToX(vector.X, null);
            player.MoveToY(vector.Y, null);
        }
        public static IEnumerator PurpleBoostCoroutine() {
            yield return 0.3f;
            VortexHelperModule.GetPlayer().StateMachine.State = VortexHelperModule.PurpleBoosterDashState;
        }

        /* Arc Motion */
        public static void PurpleDashingBegin() {
            Player player = VortexHelperModule.GetPlayer();
            DynData<Player> playerData = new DynData<Player>(player);
            player.DashDir = Input.LastAim.EightWayNormal();
            playerData.Set("purpleBoostPossibleEarlyDashSpeed", Vector2.Zero);

            foreach (PurpleBooster b in player.Scene.Tracker.GetEntities<PurpleBooster>()) {
                if (b.StartedBoosting) {
                    b.PlayerBoosted(player, player.DashDir);
                    return;
                }
                if (b.BoostingPlayer) {
                    return;
                }
            }
        }
        public static int PurpleDashingUpdate() {
            if (Input.Dash.Pressed) {
                Player player = VortexHelperModule.GetPlayer();
                DynData<Player> playerData = new DynData<Player>(player);

                playerData.Set("purpleBoosterEarlyExit", true);
                player.LiftSpeed += playerData.Get<Vector2>("purpleBoostPossibleEarlyDashSpeed");

                Input.Dash.ConsumePress();
                return 2;
            }
            return VortexHelperModule.PurpleBoosterDashState;
        }

        public static IEnumerator PurpleDashingCoroutine() {
            float t = 0f;
            Player player = VortexHelperModule.GetPlayer();
            DynData<Player> playerData = new DynData<Player>(player);
            Vector2 origin = playerData.Get<Vector2>("boostTarget");

            Vector2 earlyExitBoost;
            while (t < 1f) {
                t = Calc.Approach(t, 1.0f, Engine.DeltaTime * 1.5f);
                Vector2 vec = origin + (Vector2.UnitY * 6f) + (player.DashDir * 60f * (float)Math.Sin(t * Math.PI));

                playerData.Set("purpleBoostPossibleEarlyDashSpeed", earlyExitBoost = (t > .6f) ? (t - .5f) * 200f * -player.DashDir : Vector2.Zero);

                if (player.CollideCheck<Solid>(vec)) {
                    player.StateMachine.State = 0;
                    yield break;
                }
                player.MoveToX(vec.X); player.MoveToY(vec.Y);
                yield return null;
            }

            player.LiftSpeed += 120f * -player.DashDir;
            PurpleBoosterExplodeLaunch(player, playerData, player.Center - player.DashDir, origin);
        }

        public static void PurpleBoosterExplodeLaunch(Player player, DynData<Player> playerData, Vector2 from, Vector2? origin, float factor = 1f) {
            Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
            Celeste.Freeze(0.1f);
            playerData.Set<float?>("launchApproachX", null);
            Level level = player.SceneAs<Level>();

            if (origin != null) {
                level.Displacement.AddBurst((Vector2)origin, 0.25f, 8f, 64f, 0.5f, Ease.QuadIn, Ease.QuadOut);
            }

            level.Shake(0.15f);

            Vector2 vector = (player.Center - from).SafeNormalize(-Vector2.UnitY);
            if (Math.Abs(vector.X) < 1f
                && Math.Abs(vector.Y) < 1f) {
                vector *= 1.1f;
            }

            player.Speed = 250f * -vector;
            if (Input.MoveX.Value == Math.Sign(player.Speed.X)) {
                player.Speed.X *= 1.2f;
            }
            if (Input.MoveY.Value == Math.Sign(player.Speed.Y)) {
                player.Speed.Y *= 1.2f;
            }

            SlashFx.Burst(player.Center, player.Speed.Angle());
            if (!player.Inventory.NoRefills) {
                player.RefillDash();
            }
            player.RefillStamina();
            player.StateMachine.State = 7;
            player.Speed *= factor;
        }

        public static void LavenderBoostBegin() {
            Player player = VortexHelperModule.GetPlayer();
            if ((player.SceneAs<Level>()?.Session.MapData.GetMeta()?.TheoInBubble).GetValueOrDefault()) {
                player.RefillDash();
                player.RefillStamina();
            }
            else {
                player.RefillDash();
                player.RefillStamina();
                if (player.Holding != null) {
                    player.Drop();
                }
            }
        }

        public static int LavenderBoostUpdate() {
            Player player = VortexHelperModule.GetPlayer();
            Vector2 boostTarget = new DynData<Player>(player).Get<Vector2>("boostTarget");

            Vector2 value = Input.Aim.Value * 3f;
            Vector2 vector = Calc.Approach(player.ExactPosition, boostTarget - player.Collider.Center + value, 80f * Engine.DeltaTime);
            player.MoveToX(vector.X);
            player.MoveToY(vector.Y);
            if (Input.Dash.Pressed) {
                Input.Dash.ConsumePress();
                return 2;
            }
            return VortexHelperModule.LavenderBoosterState;
        }

        public static IEnumerator LavenderBoostCoroutine() {
            yield return 0.25f;
            VortexHelperModule.GetPlayer().StateMachine.State = 2;
        }

        public static void LavenderBoostEnd() {
            Player player = VortexHelperModule.GetPlayer();
            Vector2 vector = (new DynData<Player>(player).Get<Vector2>("boostTarget") - player.Collider.Center).Floor();
            player.MoveToX(vector.X);
            player.MoveToY(vector.Y);
            foreach (PurpleBooster b in player.Scene.Tracker.GetEntities<PurpleBooster>()) {
                if (b.StartedBoosting) {
                    b.PlayerBoosted(player, player.DashDir);
                    return;
                }
                if (b.BoostingPlayer) {
                    return;
                }
            }
        }

        #endregion
    }
}
