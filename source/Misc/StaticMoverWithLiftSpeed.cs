using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.VortexHelper.Misc {
    /// <summary>
    /// A static mover that has an extra "event" OnSetLiftSpeed that is called with the exact value of the lift speed just before any move.
    /// This allows to attach solids to solids with both solids having the same lift speed.
    /// <para/>
    /// <see href="https://github.com/max4805/MaxHelpingHand/blob/e76ac4ca2b06869f80f7dca82f231f4709a5aeb2/Entities/StaticMoverWithLiftSpeed.cs">Originally implemented by max480 in MaxHelpingHand.</see>
    /// </summary>
    [TrackedAs(typeof(StaticMover))]
    public class StaticMoverWithLiftSpeed : StaticMover {
        public Action<Vector2> OnSetLiftSpeed;

        public static class Hooks {
            private static LinkedList<Platform> currentPlatforms = new();

            public static void Hook() {
                On.Celeste.Platform.MoveStaticMovers += Platform_MoveStaticMovers;
                On.Celeste.StaticMover.Move += StaticMover_Move;
            }

            public static void Unhook() {
                On.Celeste.Platform.MoveStaticMovers -= Platform_MoveStaticMovers;
                On.Celeste.StaticMover.Move -= StaticMover_Move;
            }

            private static void Platform_MoveStaticMovers(On.Celeste.Platform.orig_MoveStaticMovers orig, Platform self, Vector2 amount) {
                currentPlatforms.AddLast(self);
                orig(self, amount);
                currentPlatforms.RemoveLast();
            }

            private static void StaticMover_Move(On.Celeste.StaticMover.orig_Move orig, StaticMover self, Vector2 amount) {
                if (self is StaticMoverWithLiftSpeed staticMover) {
                    staticMover.OnSetLiftSpeed?.Invoke(currentPlatforms.Last.Value.LiftSpeed);
                }

                orig(self, amount);
            }
        }
    }
}
