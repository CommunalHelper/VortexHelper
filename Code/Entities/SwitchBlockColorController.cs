using Celeste.Mod.Entities;
using Celeste.Mod.VortexHelper.Misc.Extensions;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.VortexHelper.Entities;

[CustomEntity("VortexHelper/SwitchBlockColorController")]
[Tracked]
public class SwitchBlockColorController : Entity
{
    public readonly Color BlueColor, RoseColor, OrangeColor, LimeColor;
    public readonly Color SwitchBackgroundColor, SwitchEdgeColor;

    public SwitchBlockColorController(EntityData data, Vector2 offset) : base(data.Position + offset)
    {
        BlueColor = data.HexColor("blueColor", EnumExt.SwitchBlockBlue);
        RoseColor = data.HexColor("roseColor", EnumExt.SwitchBlockRose);
        OrangeColor = data.HexColor("orangeColor", EnumExt.SwitchBlockOrange);
        LimeColor = data.HexColor("limeColor", EnumExt.SwitchBlockLime);

        SwitchBackgroundColor = data.HexColor("switchBackgroundColor", ColorSwitch.DefaultBackgroundColor);
        SwitchEdgeColor = data.HexColor("switchEdgeColor", ColorSwitch.DefaultEdgeColor);
    }
}