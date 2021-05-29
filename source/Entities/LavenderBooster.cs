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
            Add((Sprite)(boosterData["sprite"] = VortexHelperModule.LavenderBoosterSpriteBank.Create("lavenderBooster")));

            boosterData["particleType"] = P_BurstLavender;
        }

        public static void InitializeParticles() {
            P_BurstLavender.Color = Calc.HexToColor("6a38b0");

            P_BurstExplodeLavender.Color = P_BurstLavender.Color;
            P_BurstExplodeLavender.SpeedMax = 250;
        }

        public static class Hooks {
            public static void Hook() {
                On.Celeste.Player.DashEnd += Player_DashEnd;
                On.Celeste.Booster.AppearParticles += Booster_AppearParticles;
            }

            public static void Unhook() {
                On.Celeste.Player.DashEnd -= Player_DashEnd;
                On.Celeste.Booster.AppearParticles -= Booster_AppearParticles;
            }

            private static void Player_DashEnd(On.Celeste.Player.orig_DashEnd orig, Player self) {
                orig(self);
                if (self.LastBooster is LavenderBooster booster && booster.BoostingPlayer) {
                    Audio.Play(SFX.game_05_redbooster_end, self.Center);
                    PurpleBooster.LaunchPlayerParticles(self, self.DashDir, LavenderBooster.P_BurstExplodeLavender);
                    PurpleBooster.PurpleBoosterExplodeLaunch(self, new DynData<Player>(self), self.Center - self.DashDir, null, -1f);
                }
            }

            // https://github.com/CommunalHelper/CommunalHelper/blob/dev/src/Entities/BoosterStuff/CustomBooster.cs#L113
            private static void Booster_AppearParticles(On.Celeste.Booster.orig_AppearParticles orig, Booster self) {
                if (self is LavenderBooster) {
                    ParticleSystem particlesBG = self.SceneAs<Level>().ParticlesBG;
                    for (int i = 0; i < 360; i += 30) {
                        particlesBG.Emit(PurpleBooster.P_Appear, 1, self.Center, Vector2.One * 2f, i * ((float)Math.PI / 180f));
                    }
                }
                else {
                    orig(self);
                }
            }
        }
    }
}
