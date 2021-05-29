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

namespace Celeste.Mod.VortexHelper.Entities {
    [CustomEntity("VortexHelper/FloorBooster")]
    [Tracked(false)]
    class FloorBooster : Entity {
        private enum DisableMode {
            Disappear, ColorFade
        }

        public Facings Facing;
        private Vector2 imageOffset;
        private SoundSource idleSfx;
        private SoundSource activateSfx;

        private bool isPlaying = false;

        private List<Sprite> tiles;

        public bool IceMode;
        private bool notCoreMode;
        public bool NoRefillsOnIce;

        private DisableMode disableMode;

        public Color EnabledColor = Color.White;
        public Color DisabledColor = Color.Lerp(Color.White, Color.Black, 0.5f);

        public int MoveSpeed;

        public FloorBooster(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Width, data.Bool("left"), data.Int("speed"), data.Bool("iceMode"), data.Bool("noRefillOnIce"), data.Bool("notAttached")) { }

        public FloorBooster(Vector2 position, int width, bool left, int speed, bool iceMode, bool noRefillOnIce, bool notAttached)
            : base(position) {
            base.Tag = Tags.TransitionUpdate;
            base.Depth = 1999;
            NoRefillsOnIce = noRefillOnIce;
            notCoreMode = iceMode;
            IceMode = iceMode;
            MoveSpeed = (int)Calc.Max(0, speed);
            Facing = left ? Facings.Left : Facings.Right;

            base.Collider = new Hitbox(width, 3, 0, 5);
            if (!notCoreMode) {
                Add(new CoreModeListener(OnChangeMode));
            }

            Add(idleSfx = new SoundSource());
            Add(activateSfx = new SoundSource());

            if (!notAttached) // sure why not i guess
            {
                Add(new StaticMover {
                    OnShake = OnShake,
                    SolidChecker = IsRiding,
                    JumpThruChecker = IsRiding,
                    OnEnable = OnEnable,
                    OnDisable = OnDisable
                });
            }

            tiles = BuildSprite(left);
        }

        public void SetColor(Color color) {
            foreach (Component component in base.Components) {
                Image image = component as Image;
                if (image != null) {
                    image.Color = color;
                }
            }
        }

        private void OnEnable() {
            if (!IceMode) {
                idleSfx.Play("event:/env/local/09_core/conveyor_idle");
            }

            Active = Collidable = Visible = true;
            SetColor(EnabledColor);
        }

        private void OnDisable() {
            idleSfx.Stop();
            PlayActivateSfx(true);
            Active = Collidable = false;
            if (disableMode == DisableMode.ColorFade) {
                SetColor(DisabledColor);
            }
            else {
                Visible = false;
            }
        }

        private List<Sprite> BuildSprite(bool left) {
            List<Sprite> list = new List<Sprite>();
            for (int i = 0; i < base.Width; i += 8) {
                // Sprite Selection
                string id =
                    (i == 0) ? (!left ? "Left" : "Right") :
                    ((!((i + 16) > base.Width)) ? "Mid" :
                    (!left ? "Right" : "Left"));

                Sprite sprite = VortexHelperModule.FloorBoosterSpriteBank.Create("FloorBooster" + id);
                if (!left) {
                    sprite.FlipX = true;
                }

                sprite.Position = new Vector2(i, 0);
                list.Add(sprite);
                Add(sprite);
            }
            return list;
        }

        private void OnChangeMode(Session.CoreModes mode) {
            IceMode = (mode == Session.CoreModes.Cold);
            tiles.ForEach(delegate (Sprite t) {
                t.Play(IceMode ? "ice" : "hot");
            });
            if (IceMode) {
                idleSfx.Stop();
            }
            else if (!idleSfx.Playing) {
                idleSfx.Play("event:/env/local/09_core/conveyor_idle");
            }
        }

        private bool IsRiding(JumpThru jumpThru) {
            return CollideCheckOutside(jumpThru, Position + Vector2.UnitY);
        }
        private bool IsRiding(Solid solid) {
            if (CollideCheckOutside(solid, Position + Vector2.UnitY)) {
                disableMode = (solid is CassetteBlock || solid is SwitchBlock) ? DisableMode.ColorFade : DisableMode.Disappear;
                return true;
            }
            return false;
        }
        private void OnShake(Vector2 amount) {
            imageOffset += amount;
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            Session.CoreModes mode = IceMode ? Session.CoreModes.Cold : Session.CoreModes.Hot;
            Session.CoreModes levelCoreMode = SceneAs<Level>().CoreMode;
            if (levelCoreMode == Session.CoreModes.Cold && !notCoreMode) {
                mode = levelCoreMode;
            }
            OnChangeMode(mode);
        }

        public override void Update() {
            Player player = base.Scene.Tracker.GetEntity<Player>();
            PositionSfx(player);
            if (!(base.Scene as Level).Transitioning) {
                bool isUsed = false;
                base.Update();
                if (player != null) {
                    if (CollideCheck(player) && player.OnGround() && player.Bottom <= Bottom) {
                        isUsed = true;
                    }
                }
                PlayActivateSfx(IceMode ? true : !isUsed);
            }
        }

        private void PlayActivateSfx(bool end) {
            if ((isPlaying && !end) || (!isPlaying && end)) {
                return;
            }

            isPlaying = !end;
            if (end) {
                activateSfx.Param("end", 1f);
            }
            else {
                activateSfx.Play("event:/game/09_core/conveyor_activate", "end", 0f);
            }
        }

        public override void Render() {
            Vector2 position = Position; // Shake Offset
            Position += imageOffset;
            base.Render();
            Position = position;
        }

        private void PositionSfx(Player entity) {
            if (entity != null) {
                idleSfx.Position = Calc.ClosestPointOnLine(Position, Position + new Vector2(base.Width, 0f), entity.Center) - Position;
                idleSfx.Position.Y += 7;
                activateSfx.Position = idleSfx.Position;
                idleSfx.UpdateSfxPosition(); activateSfx.UpdateSfxPosition();
            }
        }

        public static class Hooks {
            public static void Hook() {
                IL.Celeste.Player.NormalUpdate += Player_FrictionNormalUpdate;
                On.Celeste.Player.NormalUpdate += Player_NormalUpdate;
                On.Celeste.Player.NormalBegin += Player_NormalBegin;
                On.Celeste.Player.RefillDash += Player_RefillDash;
            }

            public static void Unhook() {
                IL.Celeste.Player.NormalUpdate -= Player_FrictionNormalUpdate;
                On.Celeste.Player.NormalUpdate -= Player_NormalUpdate;
                On.Celeste.Player.NormalBegin -= Player_NormalBegin;
                On.Celeste.Player.RefillDash -= Player_RefillDash;
            }

            private static bool Player_RefillDash(On.Celeste.Player.orig_RefillDash orig, Player self) {
                // Fix crashes with vanilla entities that refill the dash.
                if (self.Scene == null || self.Dead) {
                    return false;
                }

                foreach (FloorBooster entity in self.Scene.Tracker.GetEntities<FloorBooster>()) {
                    if (!entity.IceMode) {
                        continue;
                    }

                    if (self.CollideCheck(entity) && self.OnGround()
                        && self.Bottom <= entity.Bottom && entity.NoRefillsOnIce) {
                        return false;
                    }
                }
                return orig(self);
            }

            private static void Player_NormalBegin(On.Celeste.Player.orig_NormalBegin orig, Player self) {
                orig(self);
                DynData<Player> playerData = self.GetData();
                playerData.Set("floorBoosterSpeed", 0f);
                playerData.Set<FloorBooster>("lastFloorBooster", null);
                playerData.Set("purpleBoosterEarlyExit", false);
            }

            private static int Player_NormalUpdate(On.Celeste.Player.orig_NormalUpdate orig, Player self) {
                DynData<Player> playerData = self.GetData();

                // thanks max480 for the bug report.
                if (!playerData.Data.ContainsKey("lastFloorBooster")) {
                    playerData.Set<FloorBooster>("lastFloorBooster", null);
                }

                FloorBooster lastFloorBooster = playerData.Get<FloorBooster>("lastFloorBooster");

                if (lastFloorBooster != null && !self.CollideCheck(lastFloorBooster)) {
                    Vector2 vec = Vector2.UnitX
                        * playerData.Get<float>("floorBoosterSpeed")
                        * (lastFloorBooster.Facing == Facings.Right ? lastFloorBooster.MoveSpeed : -lastFloorBooster.MoveSpeed);

                    if (self.OnGround()) {
                        self.LiftSpeed += vec / 1.6f;
                    }
                    self.Speed += vec / 1.6f;
                    playerData.Set<FloorBooster>("lastFloorBooster", null);
                }
                bool touchedFloorBooster = false;
                float floorBoosterSpeed = 0f;
                foreach (FloorBooster entity in self.Scene.Tracker.GetEntities<FloorBooster>()) {
                    if (entity.IceMode) {
                        continue;
                    }

                    if (self.CollideCheck(entity) && self.OnGround() && self.StateMachine != 1 && self.Bottom <= entity.Bottom) {
                        if (!touchedFloorBooster) {
                            floorBoosterSpeed = Calc.Approach(playerData.Get<float>("floorBoosterSpeed"), 1f, 4f * Engine.DeltaTime);
                            touchedFloorBooster = true;
                        }

                        float x = entity.Facing == Facings.Right ? entity.MoveSpeed : -entity.MoveSpeed;
                        self.MoveH(x * floorBoosterSpeed * Engine.DeltaTime);

                        playerData.Set("lastFloorBooster", entity);
                    }
                }
                if (!touchedFloorBooster) {
                    floorBoosterSpeed = Calc.Approach(playerData.Get<float>("floorBoosterSpeed"), 0f, 4f * Engine.DeltaTime);
                }

                playerData.Set("floorBoosterSpeed", floorBoosterSpeed);

                return orig(self);
            }

            // Thanks Extended Variants
            // https://github.com/max4805/Everest-ExtendedVariants/blob/master/ExtendedVariantMode/Variants/Friction.cs#L54
            private static void Player_FrictionNormalUpdate(ILContext il) {
                ILCursor cursor = new ILCursor(il);

                if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(0.65f))
                    && cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(1f))) {
                    cursor.EmitDelegate<Func<float>>(GetPlayerFriction);
                    cursor.Emit(OpCodes.Mul);
                }
            }

            private static float GetPlayerFriction() {
                Util.TryGetPlayer(out Player player);
                if (player != null) {
                    foreach (FloorBooster entity in player.Scene.Tracker.GetEntities<FloorBooster>()) {
                        if (!entity.IceMode) {
                            continue;
                        }

                        if (player.CollideCheck(entity) && player.OnGround() && player.StateMachine != 1
                            && player.Bottom <= entity.Bottom) {
                            return player.SceneAs<Level>().CoreMode == Session.CoreModes.Cold ? 0.4f : 0.2f;
                        }
                    }
                }

                return 1f;
            }
        }
    }
}