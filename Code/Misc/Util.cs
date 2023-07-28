using Monocle;

namespace Celeste.Mod.VortexHelper.Misc {
    public static class Util {

        // https://github.com/CommunalHelper/CommunalHelper/blob/dev/src/CommunalHelperModule.cs#L196
        public static bool TryGetPlayer(out Player player) {
            player = Engine.Scene?.Tracker?.GetEntity<Player>();
            return player != null;
        }

    }
}
