using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.VortexHelper.Entities {
    [CustomEntity("VortexHelper/DashBubble")]
    [Tracked(false)]
    class DashBubble : Entity {
        private const float RespawnTime = 3f;

        private Image sprite;

        private SineWave sine;
        private Wiggler moveWiggle, sizeWiggle;
        private Vector2 moveWiggleDir;

        private bool spiked;
        private bool singleUse;
        private bool wobble;
        private float respawnTimer;

        public DashBubble(Vector2 position, bool spiked, bool singleUse, bool wobble)
            : base(position) {
            this.spiked = spiked;
            this.wobble = wobble;
            this.singleUse = singleUse;
            base.Collider = new Circle(12);

            moveWiggle = Wiggler.Create(0.8f, 2f);
            moveWiggle.StartZero = true;
            Add(moveWiggle);

            Add(new PlayerCollider(OnPlayer));
            Add(sine = new SineWave(0.6f, 0f).Randomize());
            Add(sprite = new Image(GFX.Game["objects/VortexHelper/dashBubble/" + (spiked ? "spiked" : "idle") + "00"]));
            sprite.CenterOrigin();

            sizeWiggle = Wiggler.Create(1f, 4f, delegate (float v) {
                sprite.Scale = Vector2.One * (1f + v / 8);
            });
            sizeWiggle.StartZero = true;
            Add(sizeWiggle);
        }
        public DashBubble(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Bool("spiked"), data.Bool("singleUse"), data.Bool("wobble")) { }
        public override void Added(Scene scene) {
            base.Added(scene);
        }

        public override void Update() {
            base.Update();
            if (respawnTimer > 0f) {
                respawnTimer -= Engine.DeltaTime;
                if (respawnTimer <= 0f) {
                    Respawn();
                }
            }
            if (wobble) {
                UpdateY();
            }
        }
        private void Respawn() {
            if (!Collidable) {
                Collidable = true;
                sprite.Visible = true;
                sizeWiggle.Start();
                Audio.Play("event:/game/04_cliffside/greenbooster_reappear", Position);
            }
        }
        private void OnPlayer(Player player) {
            if (!player.DashAttacking) {
                PlayerPointBounce(player, base.Center, false);
                Audio.Play("event:/game/06_reflection/feather_bubble_bounce", Position);
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                moveWiggle.Start();
                moveWiggleDir = (Center - player.Center).SafeNormalize(Vector2.UnitY);
                sizeWiggle.Start();
                if (spiked) {
                    player.Die((player.Position - Position).SafeNormalize());
                }

                return;
            }

            Audio.Play("event:/game/05_mirror_temple/redbooster_end");
            sprite.Visible = false;
            Collidable = false;
            Celeste.Freeze(0.05f);
            if (!singleUse) {
                respawnTimer = RespawnTime;
            }
            SceneAs<Level>().ParticlesFG.Emit(Player.P_CassetteFly, 6, Center, Vector2.One * 7f);
        }

        private void UpdateY() {
            sprite.Position = Vector2.UnitY * sine.Value * 2f + (moveWiggleDir * moveWiggle.Value * -8f);
        }

        public static void PlayerPointBounce(Player player, Vector2 from, bool refillPlayer = false) {
            if (player.StateMachine.State == 2) {
                player.StateMachine.State = 0;
            }
            if (player.StateMachine.State == 4 && player.CurrentBooster != null) {
                player.CurrentBooster.PlayerReleased();
            }
            if (refillPlayer) {
                player.RefillDash();
                player.RefillStamina();
            }
            Vector2 value = (player.Center - from).SafeNormalize();
            if (value.Y > -0.2f && value.Y <= 0.4f) {
                value.Y = -0.2f;
            }
            player.Speed = value * 200;
            if (Math.Abs(player.Speed.X) < 80f) {
                if (player.Speed.X == 0f) {
                    player.Speed.X = (float)(0 - player.Facing) * 80;
                }
                else {
                    player.Speed.X = (float)Math.Sign(player.Speed.X) * 80;
                }
            }
        }
    }
}
