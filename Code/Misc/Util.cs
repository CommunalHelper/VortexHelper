using Monocle;
using MonoMod.Utils;

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
        player_WallJumpCheck = typeof(Player).GetMethod("WallJumpCheck", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetFastDelegate();
        player_SuperWallJump = typeof(Player).GetMethod("SuperWallJump", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetFastDelegate();
    }
    public static FastReflectionDelegate player_WallJumpCheck;
    public static FastReflectionDelegate player_SuperWallJump;
}
