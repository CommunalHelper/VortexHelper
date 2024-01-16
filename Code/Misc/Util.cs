using Monocle;
using MonoMod.Utils;
using System.Reflection;

namespace Celeste.Mod.VortexHelper.Misc;

public static class Util
{
    // https://github.com/CommunalHelper/CommunalHelper/blob/dev/src/CommunalHelperModule.cs#L196
    public static bool TryGetPlayer(out Player player)
    {
        player = Engine.Scene?.Tracker?.GetEntity<Player>();
        return player is not null;
    }

    public static void LoadDelegates()
    {
        player_WallJumpCheck = typeof(Player).GetMethod("WallJumpCheck", BindingFlags.NonPublic | BindingFlags.Instance);
        player_SuperWallJump = typeof(Player).GetMethod("SuperWallJump", BindingFlags.NonPublic | BindingFlags.Instance);
    }
    public static MethodInfo player_WallJumpCheck;
    public static MethodInfo player_SuperWallJump;
}
