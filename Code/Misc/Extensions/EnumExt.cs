using Celeste.Mod.VortexHelper.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.VortexHelper.Misc.Extensions;

public static class EnumExt
{
    public static readonly Color SwitchBlockBlue = Calc.HexToColor("3232ff");
    public static readonly Color SwitchBlockRose = Calc.HexToColor("ff3265");
    public static readonly Color SwitchBlockOrange = Calc.HexToColor("ff9532");
    public static readonly Color SwitchBlockLime = Calc.HexToColor("9cff32");

    public static Color GetColor(this VortexHelperSession.SwitchBlockColor color, Level level)
    {
        SwitchBlockColorController controller = level.Tracker.GetEntity<SwitchBlockColorController>();
        return color switch
        {
            VortexHelperSession.SwitchBlockColor.Blue => controller?.BlueColor ?? SwitchBlockBlue,
            VortexHelperSession.SwitchBlockColor.Rose => controller?.RoseColor ?? SwitchBlockRose,
            VortexHelperSession.SwitchBlockColor.Orange => controller?.OrangeColor ?? SwitchBlockOrange,
            VortexHelperSession.SwitchBlockColor.Lime => controller?.LimeColor ?? SwitchBlockLime,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static int GetSoundParam(this VortexHelperSession.SwitchBlockColor color) => color switch
    {
        VortexHelperSession.SwitchBlockColor.Blue => 0,
        VortexHelperSession.SwitchBlockColor.Rose => 1,
        VortexHelperSession.SwitchBlockColor.Orange => 2,
        VortexHelperSession.SwitchBlockColor.Lime => 3,
        _ => throw new ArgumentOutOfRangeException()
    };

    public static bool IsActive(this VortexHelperSession.SwitchBlockColor color) => color == VortexHelperModule.SessionProperties.SessionSwitchBlockColor;
}
