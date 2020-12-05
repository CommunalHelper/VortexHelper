namespace Celeste.Mod.VortexHelper
{
    class VortexHelperSession : EverestModuleSession
    {
        public enum SwitchBlockColor
        {
            Blue, Rose, Orange, Lime
        }

        public SwitchBlockColor switchBlockColor { get; set; } = SwitchBlockColor.Blue;
    }
}
