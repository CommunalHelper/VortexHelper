using Celeste.Mod.VortexHelper.Entities;
using Celeste.Mod.VortexHelper.Misc;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Reflection;

namespace Celeste.Mod.VortexHelper {
    class VortexHelperModule : EverestModule {
        public static VortexHelperModule Instance;

        public static SpriteBank FloorBoosterSpriteBank;
        public static SpriteBank PurpleBoosterSpriteBank;
        public static SpriteBank LavenderBoosterSpriteBank;
        public static SpriteBank VortexBumperSpriteBank;
        public static SpriteBank PufferBowlSpriteBank;
        public static SpriteBank LillySpriteBank;

        public static int PurpleBoosterState;
        public static int PurpleBoosterDashState;

        public static MethodInfo Spring_BounceAnimate = typeof(Spring).GetMethod("BounceAnimate", BindingFlags.Instance | BindingFlags.NonPublic);
        public static MethodInfo CrushBlock_OnDashed = typeof(CrushBlock).GetMethod("OnDashed", BindingFlags.Instance | BindingFlags.NonPublic);
        public static MethodInfo CoreModeToggle_OnPlayer = typeof(CoreModeToggle).GetMethod("OnPlayer", BindingFlags.Instance | BindingFlags.NonPublic);
        public static MethodInfo Puffer_Explode = typeof(Puffer).GetMethod("Explode", BindingFlags.Instance | BindingFlags.NonPublic);
        public static MethodInfo Puffer_GotoGone = typeof(Puffer).GetMethod("GotoGone", BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool AllowPlayerDashRefills = true;

        public override Type SessionType => typeof(VortexHelperSession);
        public static VortexHelperSession SessionProperties => (VortexHelperSession)Instance._Session;

        public VortexHelperModule() {
            Instance = this;
        }

        public override void LoadContent(bool firstLoad) {
            FloorBoosterSpriteBank = new SpriteBank(GFX.Game, "Graphics/FloorBoosterSprites.xml");

            PufferBowlSpriteBank = new SpriteBank(GFX.Game, "Graphics/BowlPufferSprites.xml");

            PurpleBoosterSpriteBank = new SpriteBank(GFX.Game, "Graphics/PurpleBoosterSprites.xml");
            LavenderBoosterSpriteBank = new SpriteBank(GFX.Game, "Graphics/LavenderBoosterSprites.xml");

            VortexBumperSpriteBank = new SpriteBank(GFX.Game, "Graphics/VortexCustomBumperSprites.xml");

            LillySpriteBank = new SpriteBank(GFX.Game, "Graphics/LillySprites.xml");

            BowlPuffer.InitializeParticles();
            PurpleBooster.InitializeParticles();
            LavenderBooster.InitializeParticles();
            VortexBumper.InitializeParticles();
            BubbleWrapBlock.InitializeParticles();
            Lilly.InitializeTextures();
        }

        public override void Load() {
            Everest.Events.Level.OnLoadEntity += Level_OnLoadEntity;

            FloorBooster.Hooks.Hook();
            PurpleBooster.Hooks.Hook();
            LavenderBooster.Hooks.Hook();
            BowlPuffer.Hooks.Hook();
            PufferBarrierRenderer.Hooks.Hook();
            MiscHooks.Hook();
        }

        public override void Unload() {
            Everest.Events.Level.OnLoadEntity -= Level_OnLoadEntity;

            FloorBooster.Hooks.Unhook();
            PurpleBooster.Hooks.Unhook();
            LavenderBooster.Hooks.Unhook();
            BowlPuffer.Hooks.Unhook();
            PufferBarrierRenderer.Hooks.Unhook();
            MiscHooks.Unhook();
        }

        private static bool Level_OnLoadEntity(Level level, LevelData levelData, Vector2 offset, EntityData entityData) {

            // We're doing this because, in the past, we had the lavender booster just be an option rather than
            // a different entity, and now that it's already been published, we can't delete change it, but I thought
            // I'd still make it a different entity. This just converts the old lavender option to the actual lavender booster entity.
            if (entityData.Name == "VortexHelper/PurpleBooster" && entityData.Bool("lavender")) {
                entityData.Name = "VortexHelper/LavenderBooster";
                return Level.LoadCustomEntity(entityData, level);
            }

            return false;
        }
    }
}
