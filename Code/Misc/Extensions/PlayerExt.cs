using MonoMod.Utils;

namespace Celeste.Mod.VortexHelper.Misc.Extensions;

public static class PlayerExt
{
    private static DynData<Player> cachedPlayerData;

    public static DynData<Player> GetData(this Player player)
        => cachedPlayerData is not null && cachedPlayerData.IsAlive && cachedPlayerData.Target == player
            ? cachedPlayerData
            : (cachedPlayerData = new DynData<Player>(player));
}
