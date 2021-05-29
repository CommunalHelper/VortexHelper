using MonoMod.Utils;

namespace Celeste.Mod.VortexHelper.Misc.Extensions {
    public static class PlayerExt {

        private static DynData<Player> cachedPlayerData;

        public static DynData<Player> GetData(this Player player) {
            if (cachedPlayerData != null && cachedPlayerData.IsAlive && cachedPlayerData.Target == player)
                return cachedPlayerData;
            return cachedPlayerData = new DynData<Player>(player);
        }

    }
}
