using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.VortexHelper.Entities {
    [CustomEntity("VortexHelper/VortexCustomBumper")]
    class VortexBumper : Entity {
        private Sprite sprite;
        private Sprite spriteEvil;

        private VertexLight light;
        private BloomPoint bloom;

        public static ParticleType P_GreenAmbience, P_OrangeAmbience;
        public static ParticleType P_GreenLaunch, P_OrangeLaunch;

        private ParticleType p_ambiance, p_launch;

        private Wiggler hitWiggler;
        private Vector2 hitDir;
        private SineWave sine;

        private Vector2 anchor;
        private bool goBack;

        private float respawnTimer;
        private bool fireMode;

        private bool twoDashes = false;
        private bool oneUse = false;
        private bool deadly = false;

        private bool notCoreMode;
        private bool wobble;

        public VortexBumper(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.FirstNodeNullable(offset), data.Attr("style", "Green"), data.Bool("notCoreMore"), data.Bool("wobble", true)) { }

        public VortexBumper(Vector2 position, Vector2? node, string style, bool notCoreMode, bool wobble)
            : base(position) {
            Collider = new Circle(12f);
            Add(new PlayerCollider(OnPlayer));

            Add(sine = new SineWave(0.44f, 0f).Randomize());
            this.wobble = wobble;

            switch (style) {
                default:
                case "Green":
                    sprite = VortexHelperModule.VortexBumperSpriteBank.Create("greenBumper");
                    twoDashes = true;
                    p_ambiance = P_GreenAmbience;
                    p_launch = P_GreenLaunch;
                    break;
                case "Orange":
                    sprite = VortexHelperModule.VortexBumperSpriteBank.Create("orangeBumper");
                    oneUse = true;
                    p_ambiance = P_OrangeAmbience;
                    p_launch = P_OrangeLaunch;
                    break;
            }
            this.notCoreMode = notCoreMode;

            Add(sprite);
            Add(spriteEvil = GFX.SpriteBank.Create("bumper_evil"));
            spriteEvil.Visible = false;

            Add(light = new VertexLight(twoDashes ? Color.DarkGreen : Color.DarkOrange, 1f, 16, 32));
            Add(bloom = new BloomPoint(0.5f, 16f));

            anchor = Position;
            if (node.HasValue) {
                Vector2 start = Position;
                Vector2 end = node.Value;
                Tween tween = Tween.Create(Tween.TweenMode.Looping, Ease.CubeInOut, 1.81818187f, start: true);
                tween.OnUpdate = delegate (Tween t) {
                    if (goBack) {
                        anchor = Vector2.Lerp(end, start, t.Eased);
                    }
                    else {
                        anchor = Vector2.Lerp(start, end, t.Eased);
                    }
                };
                tween.OnComplete = delegate {
                    goBack = !goBack;
                };
                Add(tween);
            }

            UpdatePosition();

            Add(hitWiggler = Wiggler.Create(1.2f, 2f, delegate {
                spriteEvil.Position = hitDir * hitWiggler.Value * 8f;
            }));

            if (!notCoreMode) {
                Add(new CoreModeListener(OnChangeMode));
            }
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            if (!notCoreMode) {
                fireMode = SceneAs<Level>().CoreMode == Session.CoreModes.Hot;
                spriteEvil.Visible = fireMode;
                sprite.Visible = !fireMode;
            }
        }

        private void OnChangeMode(Session.CoreModes coreMode) {
            fireMode = coreMode == Session.CoreModes.Hot;
            if (!fireMode && deadly) {
                return;
            }

            spriteEvil.Visible = fireMode;
            sprite.Visible = !fireMode;
        }

        private void UpdatePosition() {
            Position = anchor + new Vector2(sine.Value * 3f, sine.ValueOverTwo * 2f) * (wobble ? 1f : 0f);
        }

        public override void Update() {
            base.Update();
            if (respawnTimer > 0f) {
                respawnTimer -= Engine.DeltaTime;
                if (respawnTimer <= 0f) {
                    light.Visible = true;
                    bloom.Visible = true;

                    sprite.Play("on");
                    spriteEvil.Play("on");

                    if (deadly) {
                        fireMode = true;
                        spriteEvil.Visible = fireMode;
                        sprite.Visible = !fireMode;
                    }
                    Audio.Play("event:/game/06_reflection/pinballbumper_reset", Position);
                }
            }
            else if (Scene.OnInterval(0.05f)) {
                float num = Calc.Random.NextAngle();
                float direction = fireMode ? (-(float)Math.PI / 2f) : num;
                float length = fireMode ? 12 : 8;

                ParticleType type = fireMode ? Bumper.P_FireAmbience : p_ambiance;
                SceneAs<Level>().Particles.Emit(type, 1, Center + Calc.AngleToVector(num, length), Vector2.One * 2f, direction);
            }
            UpdatePosition();
        }

        private void OnPlayer(Player player) {
            Level level = SceneAs<Level>();
            if (fireMode) {
                if (!SaveData.Instance.Assists.Invincible) {
                    Vector2 vector = (player.Center - Center).SafeNormalize();
                    hitDir = -vector;
                    hitWiggler.Start();
                    Audio.Play("event:/game/09_core/hotpinball_activate", Position);
                    respawnTimer = 0.6f;
                    player.Die(vector);
                    level.Particles.Emit(Bumper.P_FireHit, 12, Center + vector * 12f, Vector2.One * 3f, vector.Angle());
                }
            } else if (respawnTimer <= 0f) {
                Audio.Play("event:/game/06_reflection/pinballbumper_hit", Position);
                respawnTimer = 0.6f;
                Vector2 vector2 = player.ExplodeLaunch(Position, snapUp: false, sidesOnly: false);

                if (twoDashes) {
                    player.Dashes = 2;
                }
                if (oneUse) {
                    deadly = true;
                }

                sprite.Play("hit", restart: true);
                spriteEvil.Play("hit", restart: true);

                light.Visible = false;
                bloom.Visible = false;

                level.DirectionalShake(vector2, 0.15f);
                level.Displacement.AddBurst(Center, 0.3f, 8f, 32f, 0.8f);
                level.Particles.Emit(p_launch, 12, Center + vector2 * 12f, Vector2.One * 3f, vector2.Angle());
            }
        }

        public static void InitializeParticles() {
            P_GreenAmbience = new ParticleType(Bumper.P_Ambience) {
                Color = Calc.HexToColor("5dcc47"),
                Color2 = Calc.HexToColor("c4ffc9")
            };

            P_OrangeAmbience = new ParticleType(Bumper.P_Ambience) {
                Color = Calc.HexToColor("cc9747"),
                Color2 = Calc.HexToColor("ffdfc4")
            };

            P_GreenLaunch = new ParticleType(Bumper.P_Launch) {
                Color = P_GreenAmbience.Color,
                Color2 = P_GreenAmbience.Color2
            };

            P_OrangeLaunch = new ParticleType(Bumper.P_Launch) {
                Color = P_OrangeAmbience.Color,
                Color2 = P_OrangeAmbience.Color2
            };
        }
    }
}
