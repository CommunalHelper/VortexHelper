using Celeste.Mod.Entities;
using Celeste.Mod.VortexHelper.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.VortexHelper.Triggers {

    [CustomEntity("VortexHelper/ColorSwitchTrigger")]
    class ColorSwitchTrigger : Trigger {
        private bool oneUse, silent;
        private VortexHelperSession.SwitchBlockColor color;

        public ColorSwitchTrigger(EntityData data, Vector2 offset)
            : base(data, offset) {
            oneUse = data.Bool("oneUse");
            silent = data.Bool("silent");

            switch (data.Int("index")) {
                default:
                case 0:
                    color = VortexHelperSession.SwitchBlockColor.Blue;
                    break;
                case 1:
                    color = VortexHelperSession.SwitchBlockColor.Rose;
                    break;
                case 2:
                    color = VortexHelperSession.SwitchBlockColor.Orange;
                    break;
                case 3:
                    color = VortexHelperSession.SwitchBlockColor.Lime;
                    break;
            }
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);
            VortexHelperModule.SessionProperties.switchBlockColor = color;
            ColorSwitch.UpdateColorSwitches(Scene, color);
            if (SwitchBlock.RoomHasSwitchBlock(Scene, VortexHelperModule.SessionProperties.switchBlockColor) && !silent) {
                Audio.Play("event:/vortexHelperEvents/game/switchBlock/switch",
                    "tone", ColorSwitch.GetSoundParam(VortexHelperModule.SessionProperties.switchBlockColor));
            }

            if (oneUse) {
                RemoveSelf();
            }
        }
    }
}
