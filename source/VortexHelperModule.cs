using Celeste.Mod.VortexHelper.Entities;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Reflection;

namespace Celeste.Mod.VortexHelper {
    class VortexHelperModule : EverestModule {
        public static VortexHelperModule Instance;

        public static SpriteBank FloorBoosterSpriteBank;
        public static SpriteBank PurpleBoosterSpriteBank;
        public static SpriteBank LavenderBoosterSpriteBank;
        public static SpriteBank VortexBumperSpriteBank;
        public static SpriteBank PufferBowlSpriteBank;
        public static SpriteBank LillySpriteBank;

        public static int PurpleBoosterState;
        public static int LavenderBoosterState;
        public static int PurpleBoosterDashState;

        public static MethodInfo Spring_BounceAnimate = typeof(Spring).GetMethod("BounceAnimate", BindingFlags.Instance | BindingFlags.NonPublic);
        public static MethodInfo CrushBlock_OnDashed = typeof(CrushBlock).GetMethod("OnDashed", BindingFlags.Instance | BindingFlags.NonPublic);
        public static MethodInfo CoreModeToggle_OnPlayer = typeof(CoreModeToggle).GetMethod("OnPlayer", BindingFlags.Instance | BindingFlags.NonPublic);
        public static MethodInfo Puffer_Explode = typeof(Puffer).GetMethod("Explode", BindingFlags.Instance | BindingFlags.NonPublic);
        public static MethodInfo Puffer_GotoGone = typeof(Puffer).GetMethod("GotoGone", BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool AllowPlayerDashRefills = true;

        public override Type SessionType => typeof(VortexHelperSession);
        public static VortexHelperSession SessionProperties => (VortexHelperSession)Instance._Session;

        public VortexHelperModule() {
            Instance = this;
        }

        public override void LoadContent(bool firstLoad) {
            FloorBoosterSpriteBank = new SpriteBank(GFX.Game, "Graphics/FloorBoosterSprites.xml");

            PufferBowlSpriteBank = new SpriteBank(GFX.Game, "Graphics/BowlPufferSprites.xml");
            BowlPuffer.InitializeParticles();

            PurpleBoosterSpriteBank = new SpriteBank(GFX.Game, "Graphics/PurpleBoosterSprites.xml");
            LavenderBoosterSpriteBank = new SpriteBank(GFX.Game, "Graphics/LavenderBoosterSprites.xml");
            PurpleBooster.InitializeParticles();

            VortexBumperSpriteBank = new SpriteBank(GFX.Game, "Graphics/VortexCustomBumperSprites.xml");
            VortexBumper.InitializeParticles();

            BubbleWrapBlock.InitializeParticles();

            LillySpriteBank = new SpriteBank(GFX.Game, "Graphics/LillySprites.xml");
            Lilly.InitializeTextures();
        }
        public override void Load() {
            IL.Celeste.Player.NormalUpdate += Player_FrictionNormalUpdate;
            On.Celeste.Player.NormalUpdate += Player_NormalUpdate;
            On.Celeste.Player.NormalBegin += Player_NormalBegin;
            On.Celeste.Player.RefillDash += Player_RefillDash;
            On.Celeste.Player.DashBegin += Player_DashBegin;
            On.Celeste.Player.DashEnd += Player_DashEnd;

            On.Celeste.LevelLoader.LoadingThread += LevelLoader_LoadingThread;

            On.Celeste.Player.ctor += Player_ctor;

            On.Celeste.Spring.ctor_Vector2_Orientations_bool += Spring_orig;

            On.Celeste.Puffer.Update += Puffer_Update;

            On.Celeste.FallingBlock.LandParticles += FallingBlock_LandParticles;

            On.Celeste.CrushBlock.Update += CrushBlock_Update;
        }

        public override void Unload() {
            IL.Celeste.Player.NormalUpdate -= Player_FrictionNormalUpdate;
            On.Celeste.Player.NormalUpdate -= Player_NormalUpdate;
            On.Celeste.Player.NormalBegin -= Player_NormalBegin;
            On.Celeste.Player.RefillDash -= Player_RefillDash;
            On.Celeste.Player.DashBegin -= Player_DashBegin;
            On.Celeste.Player.DashEnd -= Player_DashEnd;

            On.Celeste.LevelLoader.LoadingThread -= LevelLoader_LoadingThread;

            On.Celeste.Player.ctor -= Player_ctor;

            On.Celeste.Spring.ctor_Vector2_Orientations_bool -= Spring_orig;

            On.Celeste.Puffer.Update -= Puffer_Update;

            On.Celeste.FallingBlock.LandParticles -= FallingBlock_LandParticles;

            On.Celeste.CrushBlock.Update -= CrushBlock_Update;
        }

        private void Player_DashEnd(On.Celeste.Player.orig_DashEnd orig, Player self) {
            orig(self);
            foreach (PurpleBooster b in self.Scene.Tracker.GetEntities<PurpleBooster>()) {
                if (b.BoostingPlayer) {
                    b.BoostingPlayer = false;
                    foreach (PurpleBooster other_booster in self.Scene.Tracker.GetEntities<PurpleBooster>()) {
                        if (other_booster != b) {
                            if (self.CollideCheck(other_booster)) {
                                return;
                            }
                        }
                    }
                    PurpleBooster.PurpleBoosterExplodeLaunch(self, new DynData<Player>(self), self.Center - self.DashDir, null, -1f);
                    return;
                }

            }
        }

        private void CrushBlock_Update(On.Celeste.CrushBlock.orig_Update orig, CrushBlock self) {
            orig(self);
            DynData<CrushBlock> data = new DynData<CrushBlock>(self);

            Vector2 crushDir = data.Get<Vector2>("crushDir");

            Vector2 oldCrushDir;
            if (data.Data.TryGetValue("oldCrushDir", out object value) && value is Vector2 vec) {
                oldCrushDir = vec;
            }
            else {
                data["oldCrushDir"] = oldCrushDir = Vector2.Zero;
            }

            if (oldCrushDir != Vector2.Zero && crushDir == Vector2.Zero) {
                // we hit something!
                foreach (BubbleWrapBlock e in self.Scene.Tracker.GetEntities<BubbleWrapBlock>()) {
                    if (self.CollideCheck(e, self.Position + oldCrushDir)) {
                        e.Break();
                    }
                }
                foreach (ColorSwitch e in self.Scene.Tracker.GetEntities<ColorSwitch>()) {
                    if (self.CollideCheck(e, self.Position + oldCrushDir)) {
                        e.Switch(oldCrushDir);
                    }
                }
            }
            data.Set("oldCrushDir", crushDir);
        }

        private void FallingBlock_LandParticles(On.Celeste.FallingBlock.orig_LandParticles orig, FallingBlock self) {
            orig(self);
            foreach (BubbleWrapBlock e in self.Scene.Tracker.GetEntities<BubbleWrapBlock>()) {
                if (self.CollideCheck(e, self.Position + Vector2.UnitY)) {
                    e.Break();
                }
            }
            foreach (ColorSwitch e in self.Scene.Tracker.GetEntities<ColorSwitch>()) {
                if (self.CollideCheck(e, self.Position + Vector2.UnitY)) {
                    e.Switch(Vector2.UnitY);
                }
            }
        }

        private void Puffer_Update(On.Celeste.Puffer.orig_Update orig, Puffer self) {
            orig(self);
            if (!self.Collidable) {
                return;
            }

            foreach (PufferBarrier barrier in self.Scene.Tracker.GetEntities<PufferBarrier>()) {
                barrier.Collidable = true;
            }

            PufferBarrier collided = self.CollideFirst<PufferBarrier>();
            if (collided != null) {
                collided.OnTouchPuffer();

                Puffer_Explode.Invoke(self, new object[] { });
                Puffer_GotoGone.Invoke(self, new object[] { });
            }

            foreach (PufferBarrier barrier in self.Scene.Tracker.GetEntities<PufferBarrier>()) {
                barrier.Collidable = false;
            }
        }

        private void Spring_orig(On.Celeste.Spring.orig_ctor_Vector2_Orientations_bool orig, Spring self, Vector2 position, Spring.Orientations orientation, bool playerCanUse) {
            orig(self, position, orientation, playerCanUse);
            self.Add(new BowlPuffer.BowlPufferCollider(Spring_OnBowlPuffer));
        }

        private void Spring_OnBowlPuffer(BowlPuffer puffer, Spring self) {
            puffer.HitSpring(self);
            Spring_BounceAnimate.Invoke(self, new object[] { });
        }

        private void LevelLoader_LoadingThread(On.Celeste.LevelLoader.orig_LoadingThread orig, LevelLoader self) {
            orig(self);

            // Allows for PufferBarrier entities to be rendered with just one PufferBarrierRenderer.
            self.Level.Add(new PufferBarrierRenderer());
        }

        private void Player_DashBegin(On.Celeste.Player.orig_DashBegin orig, Player self) {
            // Fix
            orig(self);
            DynData<Player> playerData = new DynData<Player>(self);
            if (playerData.Get<bool>("purpleBoosterEarlyExit")) {
                --self.Dashes;
                playerData.Set("purpleBoosterEarlyExit", false);
            }
        }

        private void Player_ctor(On.Celeste.Player.orig_ctor orig, Player self, Vector2 position, PlayerSpriteMode spriteMode) {
            orig(self, position, spriteMode);

            AllowPlayerDashRefills = true;

            // Custom Purple Booster State
            PurpleBoosterState = self.StateMachine.AddState(
                new Func<int>(PurpleBooster.PurpleBoostUpdate),
                PurpleBooster.PurpleBoostCoroutine,
                PurpleBooster.PurpleBoostBegin,
                PurpleBooster.PurpleBoostEnd);
            LavenderBoosterState = self.StateMachine.AddState(
                new Func<int>(PurpleBooster.LavenderBoostUpdate),
                PurpleBooster.LavenderBoostCoroutine,
                PurpleBooster.LavenderBoostBegin,
                PurpleBooster.LavenderBoostEnd);
            PurpleBoosterDashState = self.StateMachine.AddState(
                new Func<int>(PurpleBooster.PurpleDashingUpdate),
                PurpleBooster.PurpleDashingCoroutine,
                PurpleBooster.PurpleDashingBegin);
        }

        private bool Player_RefillDash(On.Celeste.Player.orig_RefillDash orig, Player self) {
            if (!AllowPlayerDashRefills) {
                return false;
            }

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

        private void Player_FrictionNormalUpdate(ILContext il) {
            ILCursor cursor = new ILCursor(il);

            if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(0.65f))
                && cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(1f))) {
                cursor.EmitDelegate<Func<float>>(GetPlayerFriction);
                cursor.Emit(OpCodes.Mul);
            }
        }

        private float GetPlayerFriction() {
            Player player = GetPlayer();
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

        private void Player_NormalBegin(On.Celeste.Player.orig_NormalBegin orig, Player self) {
            orig(self);
            DynData<Player> playerData = new DynData<Player>(self);
            playerData.Set("floorBoosterSpeed", 0f);
            playerData.Set<FloorBooster>("lastFloorBooster", null);
            playerData.Set("purpleBoosterEarlyExit", false);
        }

        private int Player_NormalUpdate(On.Celeste.Player.orig_NormalUpdate orig, Player self) {
            DynData<Player> playerData = new DynData<Player>(self);

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

        public static Player GetPlayer() {
            try {
                return (Engine.Scene as Level).Tracker.GetEntity<Player>();
            }
            catch (NullReferenceException) {
                return null;
            }
        }
    }


    public static class StateMachineExt {
        /// <summary>
        /// Adds a state to a StateMachine
        /// </summary>
        /// <returns>The index of the new state</returns>
        public static int AddState(this StateMachine machine, Func<int> onUpdate, Func<IEnumerator> coroutine = null, Action begin = null, Action end = null) {
            Action[] begins = (Action[])StateMachine_begins.GetValue(machine);
            Func<int>[] updates = (Func<int>[])StateMachine_updates.GetValue(machine);
            Action[] ends = (Action[])StateMachine_ends.GetValue(machine);
            Func<IEnumerator>[] coroutines = (Func<IEnumerator>[])StateMachine_coroutines.GetValue(machine);
            int nextIndex = begins.Length;
            // Now let's expand the arrays
            Array.Resize(ref begins, begins.Length + 1);
            Array.Resize(ref updates, begins.Length + 1);
            Array.Resize(ref ends, begins.Length + 1);
            Array.Resize(ref coroutines, coroutines.Length + 1);
            // Store the resized arrays back into the machine
            StateMachine_begins.SetValue(machine, begins);
            StateMachine_updates.SetValue(machine, updates);
            StateMachine_ends.SetValue(machine, ends);
            StateMachine_coroutines.SetValue(machine, coroutines);
            // And now we add the new functions
            machine.SetCallbacks(nextIndex, onUpdate, coroutine, begin, end);
            return nextIndex;
        }
        private static FieldInfo StateMachine_begins = typeof(StateMachine).GetField("begins", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo StateMachine_updates = typeof(StateMachine).GetField("updates", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo StateMachine_ends = typeof(StateMachine).GetField("ends", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo StateMachine_coroutines = typeof(StateMachine).GetField("coroutines", BindingFlags.Instance | BindingFlags.NonPublic);
    } // Thanks, Ja.
}
