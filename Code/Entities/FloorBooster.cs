using Celeste.Mod.Entities;
using Celeste.Mod.VortexHelper.Misc;
using Celeste.Mod.VortexHelper.Misc.Extensions;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.VortexHelper.Entities;

[CustomEntity("VortexHelper/FloorBooster")]
[Tracked(false)]
public class FloorBooster : Entity
{
    private enum DisableMode
    {
        Disappear, ColorFade
    }

    public Facings Facing;
    private Vector2 imageOffset;
    private readonly SoundSource idleSfx, activateSfx;

    private bool isPlaying = false;

    private readonly List<Sprite> tiles;

    private readonly bool notCoreMode;

    public bool IceMode;
    public bool NoRefillsOnIce;

    private DisableMode disableMode;

    public Color EnabledColor = Color.White;
    public Color DisabledColor = Color.Lerp(Color.White, Color.Black, 0.5f);

    public int MoveSpeed;

    public FloorBooster(EntityData data, Vector2 offset)
        : this(data.Position + offset, data.Width, data.Bool("left"), data.Int("speed"), data.Bool("iceMode"), data.Bool("noRefillOnIce"), data.Bool("notAttached")) { }

    public FloorBooster(Vector2 position, int width, bool left, int speed, bool iceMode, bool noRefillOnIce, bool notAttached)
        : base(position)
    {
        this.Tag = Tags.TransitionUpdate;
        this.Depth = Depths.Below - 1;

        this.NoRefillsOnIce = noRefillOnIce;
        this.notCoreMode = iceMode;
        this.IceMode = iceMode;
        this.MoveSpeed = (int) Calc.Max(0, speed);
        this.Facing = left ? Facings.Left : Facings.Right;

        this.Collider = new Hitbox(width, 3, 0, 5);
        if (!this.notCoreMode)
            Add(new CoreModeListener(OnChangeMode));

        Add(this.idleSfx = new SoundSource());
        Add(this.activateSfx = new SoundSource());

        if (!notAttached)
        {
            Add(new StaticMover
            {
                OnShake = OnShake,
                SolidChecker = IsRiding,
                JumpThruChecker = IsRiding,
                OnEnable = OnEnable,
                OnDisable = OnDisable
            });
        }

        this.tiles = BuildSprite(left);
    }

    public void SetColor(Color color)
    {
        foreach (Component component in this.Components)
        {
            if (component is Image image)
            {
                image.Color = color;
            }
        }
    }

    private void OnEnable()
    {
        if (!this.IceMode)
            this.idleSfx.Play(SFX.env_loc_09_conveyer_idle);

        this.Active = this.Collidable = this.Visible = true;
        SetColor(this.EnabledColor);
    }

    private void OnDisable()
    {
        this.idleSfx.Stop();
        PlayActivateSfx(true);

        this.Active = this.Collidable = false;
        if (this.disableMode == DisableMode.ColorFade)
            SetColor(this.DisabledColor);
        else
            this.Visible = false;
    }

    private List<Sprite> BuildSprite(bool left)
    {
        var list = new List<Sprite>();
        for (int i = 0; i < this.Width; i += 8)
        {
            // Sprite Selection
            string id = (i == 0)
                ? left
                    ? "Right"
                    : "Left"
                : (i + 16) > this.Width
                    ? left
                        ? "Left"
                        : "Right"
                    : "Mid";

            Sprite sprite = VortexHelperModule.FloorBoosterSpriteBank.Create("FloorBooster" + id);
            if (!left)
                sprite.FlipX = true;

            sprite.Position = new Vector2(i, 0);
            list.Add(sprite);
            Add(sprite);
        }

        return list;
    }

    private void OnChangeMode(Session.CoreModes mode)
    {
        this.IceMode = mode == Session.CoreModes.Cold;

        string anim = this.IceMode ? "ice" : "hot";
        foreach (Sprite s in tiles)
            s.Play(anim);

        if (this.IceMode)
            this.idleSfx.Stop();
        else if (!this.idleSfx.Playing)
            this.idleSfx.Play(SFX.env_loc_09_conveyer_idle);
    }

    private bool IsRiding(JumpThru jumpThru) => CollideCheckOutside(jumpThru, this.Position + Vector2.UnitY);

    private bool IsRiding(Solid solid)
    {
        if (CollideCheckOutside(solid, this.Position + Vector2.UnitY))
        {
            this.disableMode = (solid is CassetteBlock or SwitchBlock) ? DisableMode.ColorFade : DisableMode.Disappear;
            return true;
        }

        return false;
    }

    private void OnShake(Vector2 amount) => this.imageOffset += amount;

    public override void Added(Scene scene)
    {
        base.Added(scene);

        Session.CoreModes mode = this.IceMode
            ? Session.CoreModes.Cold
            : Session.CoreModes.Hot;
        Session.CoreModes lvlMode = SceneAs<Level>().CoreMode;

        if (lvlMode is Session.CoreModes.Cold && !this.notCoreMode)
            mode = lvlMode;

        OnChangeMode(mode);
    }

    public override void Update()
    {
        Player player = this.Scene.Tracker.GetEntity<Player>();
        PositionSfx(player);

        if (SceneAs<Level>().Transitioning)
            return;

        bool isUsed = false;
        base.Update();

        if (player is not null && CollideCheck(player) && player.OnGround() && player.Bottom <= this.Bottom)
            isUsed = true;

        PlayActivateSfx(this.IceMode || !isUsed);
    }

    private void PlayActivateSfx(bool end)
    {
        if (this.isPlaying ^ end)
            return;

        this.isPlaying = !end;
        if (end)
            this.activateSfx.Param("end", 1f);
        else
            this.activateSfx.Play(SFX.game_09_conveyor_activate, "end", 0f);
    }

    public override void Render()
    {
        Vector2 position = this.Position;
        this.Position += this.imageOffset;
        base.Render();
        this.Position = position;
    }

    private void PositionSfx(Player entity)
    {
        if (entity is null)
            return;

        this.idleSfx.Position = Calc.ClosestPointOnLine(this.Position, this.Position + new Vector2(this.Width, 0f), entity.Center) - this.Position;
        this.idleSfx.Position.Y += 7;
        this.activateSfx.Position = this.idleSfx.Position;
        this.idleSfx.UpdateSfxPosition(); this.activateSfx.UpdateSfxPosition();
    }

    internal static class Hooks
    {
        public static void Hook()
        {
            IL.Celeste.Player.NormalUpdate += Player_FrictionNormalUpdate;
            On.Celeste.Player.NormalUpdate += Player_NormalUpdate;
            On.Celeste.Player.NormalBegin += Player_NormalBegin;
            On.Celeste.Player.RefillDash += Player_RefillDash;
        }

        public static void Unhook()
        {
            IL.Celeste.Player.NormalUpdate -= Player_FrictionNormalUpdate;
            On.Celeste.Player.NormalUpdate -= Player_NormalUpdate;
            On.Celeste.Player.NormalBegin -= Player_NormalBegin;
            On.Celeste.Player.RefillDash -= Player_RefillDash;
        }

        private static bool Player_RefillDash(On.Celeste.Player.orig_RefillDash orig, Player self)
        {
            // Fix crashes with vanilla entities that refill the dash.
            if (self.Scene is null || self.Dead)
                return orig(self);

            // Prevent blocking refill on room transitions
            Level level = self.Scene as Level;
            if (level.Transitioning)
                return orig(self);

            foreach (FloorBooster entity in self.Scene.Tracker.GetEntities<FloorBooster>())
            {
                if (!entity.IceMode)
                    continue;

                if (self.CollideCheck(entity) && self.OnGround()
                    && self.Bottom <= entity.Bottom
                    && entity.NoRefillsOnIce)
                    return false;
            }

            return orig(self);
        }

        private static void Player_NormalBegin(On.Celeste.Player.orig_NormalBegin orig, Player self)
        {
            orig(self);
            DynamicData playerData = DynamicData.For(self);
            playerData.Set("floorBoosterSpeed", 0f);
            playerData.Set("lastFloorBooster", null);
        }

        private static int Player_NormalUpdate(On.Celeste.Player.orig_NormalUpdate orig, Player self)
        {
            DynamicData playerData = DynamicData.For(self);

            // thanks max480 for the bug report.
            if (!playerData.Data.ContainsKey("lastFloorBooster"))
                playerData.Set("lastFloorBooster", null);

            FloorBooster lastFloorBooster = playerData.Get<FloorBooster>("lastFloorBooster");

            if (lastFloorBooster is not null && !self.CollideCheck(lastFloorBooster))
            {
                Vector2 vec = Vector2.UnitX
                    * playerData.Get<float>("floorBoosterSpeed")
                    * (lastFloorBooster.Facing == Facings.Right ? lastFloorBooster.MoveSpeed : -lastFloorBooster.MoveSpeed);

                if (self.OnGround())
                    self.LiftSpeed += vec / 1.6f;
                self.Speed += vec / 1.6f;

                playerData.Set("lastFloorBooster", null);
            }

            bool touchedFloorBooster = false;
            float floorBoosterSpeed = 0f;
            
            foreach (FloorBooster entity in self.Scene.Tracker.GetEntities<FloorBooster>())
            {
                if (entity.IceMode)
                    continue;

                if (self.CollideCheck(entity) && self.OnGround() && self.StateMachine != Player.StClimb && self.Bottom <= entity.Bottom)
                {
                    if (!touchedFloorBooster)
                    {
                        floorBoosterSpeed = Calc.Approach(playerData.Get<float>("floorBoosterSpeed"), 1f, 4f * Engine.DeltaTime);
                        touchedFloorBooster = true;
                    }

                    self.MoveH(entity.MoveSpeed * (int) entity.Facing * floorBoosterSpeed * Engine.DeltaTime);
                    playerData.Set("lastFloorBooster", entity);
                }
            }

            if (!touchedFloorBooster && playerData?.Get("floorBoosterSpeed") is float f)
                floorBoosterSpeed = Calc.Approach(f, 0f, 4f * Engine.DeltaTime);
            playerData.Set("floorBoosterSpeed", floorBoosterSpeed);

            return orig(self);
        }

        // Thanks Extended Variants
        // https://github.com/max4805/Everest-ExtendedVariants/blob/master/ExtendedVariantMode/Variants/Friction.cs#L54
        private static void Player_FrictionNormalUpdate(ILContext il)
        {
            var cursor = new ILCursor(il);

            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(0.65f))
                && cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(1f)))
            {
                cursor.EmitDelegate(GetPlayerFriction);
                cursor.Emit(OpCodes.Mul);
            }
        }

        private static float GetPlayerFriction()
        {
            if (!Util.TryGetPlayer(out Player player))
                return 1.0f;

            foreach (FloorBooster entity in player.Scene.Tracker.GetEntities<FloorBooster>())
            {
                if (!entity.IceMode)
                    continue;

                if (player.CollideCheck(entity) && player.OnGround() && player.StateMachine != Player.StClimb
                    && player.Bottom <= entity.Bottom)
                    return player.SceneAs<Level>().CoreMode is Session.CoreModes.Cold ? 0.4f : 0.2f;
            }

            return 1.0f;
        }
    }
}