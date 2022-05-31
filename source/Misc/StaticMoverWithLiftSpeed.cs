using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.VortexHelper.Misc {
    /// <summary>
    /// A static mover that has an extra "event" OnSetLiftSpeed that is called with the exact value of the lift speed just before any move.
    /// This allows to attach solids to solids with both solids having the same lift speed.
    /// </summary>
    [TrackedAs(typeof(StaticMover))]
    public class StaticMoverWithLiftSpeed : StaticMover {
        public Action<Vector2> OnSetLiftSpeed;

        public static class Hooks {
            private static Platform currentPlatform;

            public static void Hook() {
                On.Celeste.Platform.MoveStaticMovers += Platform_MoveStaticMovers;
                On.Celeste.StaticMover.Move += StaticMover_Move;
            }

            public static void Unhook() {
                On.Celeste.Platform.MoveStaticMovers -= Platform_MoveStaticMovers;
                On.Celeste.StaticMover.Move -= StaticMover_Move;
            }

            private static void Platform_MoveStaticMovers(On.Celeste.Platform.orig_MoveStaticMovers orig, Platform self, Vector2 amount) {
                currentPlatform = self;
                orig(self, amount);
                currentPlatform = null;
            }

            private static void StaticMover_Move(On.Celeste.StaticMover.orig_Move orig, StaticMover self, Vector2 amount) {
                if (self is StaticMoverWithLiftSpeed staticMover) {
                    staticMover.OnSetLiftSpeed?.Invoke(currentPlatform.LiftSpeed);
                }

                orig(self, amount);
            }
        }
    }
}
