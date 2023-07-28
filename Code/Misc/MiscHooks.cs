using Celeste.Mod.VortexHelper.Entities;
using Microsoft.Xna.Framework;
using MonoMod.Utils;

namespace Celeste.Mod.VortexHelper.Misc {
    internal static class MiscHooks {
        public static void Hook() {
            // BubbleWrapBlock and ColorSwitch interactions with some vanilla entities.
            On.Celeste.FallingBlock.LandParticles += FallingBlock_LandParticles;
            On.Celeste.CrushBlock.Update += CrushBlock_Update;
        }

        public static void Unhook() {
            On.Celeste.FallingBlock.LandParticles -= FallingBlock_LandParticles;
            On.Celeste.CrushBlock.Update -= CrushBlock_Update;
        }

        private static void CrushBlock_Update(On.Celeste.CrushBlock.orig_Update orig, CrushBlock self) {
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

        private static void FallingBlock_LandParticles(On.Celeste.FallingBlock.orig_LandParticles orig, FallingBlock self) {
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
    }
}
