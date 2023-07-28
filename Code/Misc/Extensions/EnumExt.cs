using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.VortexHelper.Misc.Extensions {
    public static class EnumExt {

        public static Color SwitchBlockBlue = Calc.HexToColor("3232ff");
        public static Color SwitchBlockRose = Calc.HexToColor("ff3265");
        public static Color SwitchBlockOrange = Calc.HexToColor("ff9532");
        public static Color SwitchBlockLime = Calc.HexToColor("9cff32");

        public static Color GetColor(this VortexHelperSession.SwitchBlockColor color) {
            return color switch {
                VortexHelperSession.SwitchBlockColor.Rose => SwitchBlockRose,
                VortexHelperSession.SwitchBlockColor.Orange => SwitchBlockOrange,
                VortexHelperSession.SwitchBlockColor.Lime => SwitchBlockLime,
                _ => SwitchBlockBlue,
            };
        }

        public static int GetSoundParam(this VortexHelperSession.SwitchBlockColor color) {
            return color switch {
                VortexHelperSession.SwitchBlockColor.Rose => 1,
                VortexHelperSession.SwitchBlockColor.Orange => 2,
                VortexHelperSession.SwitchBlockColor.Lime => 3,
                _ => 0,
            };
        }

        public static bool IsActive(this VortexHelperSession.SwitchBlockColor color) {
            return color == VortexHelperModule.SessionProperties.SessionSwitchBlockColor;
        }

    }
}
