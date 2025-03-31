using Monocle;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Celeste.Mod.VortexHelper.Entities;

public class ColorSwitchFlagsController : Entity
{
    private static readonly Dictionary<VortexHelperSession.SwitchBlockColor, string> colorsToFlagNames = new() {
        { VortexHelperSession.SwitchBlockColor.Blue, "VortexHelper/SwitchBlockColor_Blue" },
        { VortexHelperSession.SwitchBlockColor.Rose, "VortexHelper/SwitchBlockColor_Rose" },
        { VortexHelperSession.SwitchBlockColor.Lime, "VortexHelper/SwitchBlockColor_Lime" },
        { VortexHelperSession.SwitchBlockColor.Orange, "VortexHelper/SwitchBlockColor_Orange" },
    };

    public ColorSwitchFlagsController() : base(Vector2.Zero)
    {
        Depth = -int.MaxValue; // update last
        Tag = Tags.Global | Tags.TransitionUpdate;
    }

    public override void Update()
    {
        Session session = SceneAs<Level>().Session;

        foreach (string flag in colorsToFlagNames.Values)
        {
            session.SetFlag(flag, false);
        }

        session.SetFlag(colorsToFlagNames[VortexHelperModule.SessionProperties.SessionSwitchBlockColor]);
    }

    internal static class Hooks
    {
        public static void Hook()
        {
            Everest.Events.LevelLoader.OnLoadingThread += OnLoadingThread;
        }

        public static void Unhook()
        {
            Everest.Events.LevelLoader.OnLoadingThread -= OnLoadingThread;
        }

        private static void OnLoadingThread(Level level) => level.Add(new ColorSwitchFlagsController());
    }
}