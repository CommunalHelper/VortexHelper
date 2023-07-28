using Celeste.Mod.Entities;
using Celeste.Mod.VortexHelper.Entities;
using Celeste.Mod.VortexHelper.Misc.Extensions;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.VortexHelper.Triggers;

[CustomEntity("VortexHelper/ColorSwitchTrigger")]
public class ColorSwitchTrigger : Trigger
{
    private readonly bool oneUse, silent;
    private readonly VortexHelperSession.SwitchBlockColor color;

    public ColorSwitchTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        this.oneUse = data.Bool("oneUse");
        this.silent = data.Bool("silent");

        this.color = data.Int("index") switch
        {
            1 => VortexHelperSession.SwitchBlockColor.Rose,
            2 => VortexHelperSession.SwitchBlockColor.Orange,
            3 => VortexHelperSession.SwitchBlockColor.Lime,
            _ => VortexHelperSession.SwitchBlockColor.Blue,
        };
    }

    public override void Awake(Scene scene) => base.Awake(scene);

    public override void OnEnter(Player player)
    {
        base.OnEnter(player);
        VortexHelperModule.SessionProperties.SessionSwitchBlockColor = this.color;
        ColorSwitch.UpdateColorSwitches(this.Scene, this.color);
        if (SwitchBlock.RoomHasSwitchBlock(this.Scene, VortexHelperModule.SessionProperties.SessionSwitchBlockColor) && !this.silent)
            Audio.Play(CustomSFX.game_switchBlock_switch, "tone", VortexHelperModule.SessionProperties.SessionSwitchBlockColor.GetSoundParam());

        if (this.oneUse)
            RemoveSelf();
    }
}
