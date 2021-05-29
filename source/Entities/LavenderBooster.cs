using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;

namespace Celeste.Mod.VortexHelper.Entities {
    [CustomEntity("VortexHelper/LavenderBooster")]
    [TrackedAs(typeof(Booster))]
    class LavenderBooster : Booster {

        public static readonly ParticleType P_BurstLavender = new ParticleType(P_Burst);
        public static readonly ParticleType P_BurstExplodeLavender = new ParticleType(P_Burst);

        private DynData<Booster> boosterData;

        public LavenderBooster(EntityData data, Vector2 offset)
            : base(data.Position + offset, red: false) { 

            boosterData = new DynData<Booster>(this);

            Sprite oldSprite = boosterData.Get<Sprite>("sprite");
            Remove(oldSprite);
            Add((Sprite) (boosterData["sprite"] = VortexHelperModule.LavenderBoosterSpriteBank.Create("lavenderBooster")));

            boosterData["particleType"] = P_BurstLavender;
        }

        public static void InitializeParticles() {
            P_BurstLavender.Color = Calc.HexToColor("6a38b0");

            P_BurstExplodeLavender.Color = P_BurstLavender.Color;
            P_BurstExplodeLavender.SpeedMax = 250;
        }
    }
}
