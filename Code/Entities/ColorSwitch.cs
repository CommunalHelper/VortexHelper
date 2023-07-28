using Celeste.Mod.Entities;
using Celeste.Mod.VortexHelper.Misc.Extensions;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Linq;

namespace Celeste.Mod.VortexHelper.Entities {
    [CustomEntity("VortexHelper/ColorSwitch")]
    [Tracked(false)]
    public class ColorSwitch : Solid {
        private uint seed;

        private MTexture[,] edges = new MTexture[3, 3];
        private SoundSource idleSfx = new SoundSource();

        private static readonly Color defaultBackgroundColor = Calc.HexToColor("191919");
        private static readonly Color defaultEdgeColor = Calc.HexToColor("646464");

        private Color BackgroundColor = defaultBackgroundColor;
        private Color EdgeColor = defaultEdgeColor;
        private Color currentEdgeColor, currentBackgroundColor;

        private Vector2 scale = Vector2.One;
        private Vector2 scaleStrength = Vector2.One;

        private VortexHelperSession.SwitchBlockColor[] colors;
        private int nextColorIndex = 0;
        private bool singleColor = false;
        private bool random = false;

        public ColorSwitch(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Width, data.Height,
                  data.Bool("blue"), data.Bool("rose"), data.Bool("orange"), data.Bool("lime"), data.Bool("random")) { }

        public ColorSwitch(Vector2 position, int width, int height, bool blue, bool rose, bool orange, bool lime, bool random)
            : base(position, width, height, true) {
            SurfaceSoundIndex = SurfaceIndex.ZipMover;

            if (!blue && !rose && !orange && !lime) {
                blue = rose = orange = lime = true; // if all are false, then block is useless, so make it not useless.
            }

            string block = "objects/VortexHelper/onoff/switch";

            for (int i = 0; i < 3; i++) {
                for (int j = 0; j < 3; j++) {
                    edges[i, j] = GFX.Game[block].GetSubtexture(i * 8, j * 8, 8, 8);
                }
            }

            this.random = random;

            bool[] colorBools = new bool[] { blue, rose, orange, lime };
            int colorArraySize = colorBools.Count(b => b);

            colors = new VortexHelperSession.SwitchBlockColor[colorArraySize];
            singleColor = colorArraySize == 1;

            int arrIdx = 0;
            for (int i = 0; i < colorBools.Length; i++) {
                if (colorBools[i]) {
                    colors[arrIdx++] = (VortexHelperSession.SwitchBlockColor) i;
                }
            }

            NextColor(colors[nextColorIndex], true);

            Add(new LightOcclude());
            Add(idleSfx);
            idleSfx.Position += new Vector2(Width, Height) / 2f;

            if (width > 32) {
                scaleStrength.X = width / 32f;
            }

            if (height > 32) {
                scaleStrength.Y = height / 32f;
            }

            SetEdgeColor(EdgeColor, EdgeColor);
            Color col = colors[nextColorIndex].IsActive() ? defaultBackgroundColor : colors[nextColorIndex].GetColor();
            SetBackgroundColor(col, col);
            OnDashCollide = Dashed;
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            idleSfx.Play(SFX.game_01_console_static_loop);
        }

        public override void Render() {
            Vector2 position = Position;
            Position += Shake;

            int x = (int)(Center.X + (X + 1 - Center.X) * scale.X);
            int y = (int)(Center.Y + (Y + 1 - Center.Y) * scale.Y);
            int rectW = (int)((Width - 2) * scale.X);
            int rectH = (int)((Height - 2) * scale.Y);
            Rectangle rect = new Rectangle(x, y, rectW, rectH);

            Color col = random ?
                Color.Lerp(defaultBackgroundColor, Color.White, (float)(0.05f * Math.Sin(Scene.TimeActive * 5f)) + 0.05f)
                : (BackgroundColor != defaultBackgroundColor ?

                    Color.Lerp(currentBackgroundColor, Color.Black, 0.2f)
                    : currentBackgroundColor);

            Draw.Rect(rect, col);

            for (int i = rect.Y; i < rect.Bottom; i += 2) {
                float scale = 0.05f + (1f + (float)Math.Sin(i / 16f + Scene.TimeActive * 2f)) / 2f * 0.2f;
                Draw.Line(rect.X, i, rect.X + rect.Width, i, Color.White * 0.55f * scale);
            }

            uint tempseed = seed;
            PlaybackBillboard.DrawNoise(rect, ref tempseed, Color.White * 0.05f);

            int w = (int)(Width / 8f);
            int h = (int)(Height / 8f);

            for (int i = 0; i < w; i++) {
                for (int j = 0; j < h; j++) {
                    int num4 = (i != 0) ? ((i != w - 1f) ? 1 : 2) : 0;
                    int num5 = (j != 0) ? ((j != h - 1f) ? 1 : 2) : 0;

                    if (num4 != 1 || num5 != 1) {
                        Vector2 renderPos = (new Vector2(i, j) * 8f) + (Vector2.One * 4f) + Position;
                        renderPos.X = Center.X + (renderPos.X - Center.X) * scale.X;
                        renderPos.Y = Center.Y + (renderPos.Y - Center.Y) * scale.Y;

                        edges[num4, num5].DrawCentered(renderPos, currentEdgeColor, scale);
                    }
                }
            }

            base.Render();
            Position = position;
        }

        public override void Update() {
            base.Update();

            if (Scene.OnInterval(0.1f)) {
                seed++;
            }
            float t = Calc.Min(1f, 4f * Engine.DeltaTime);
            currentEdgeColor = Color.Lerp(currentEdgeColor, EdgeColor, t);
            currentBackgroundColor = Color.Lerp(currentBackgroundColor, BackgroundColor, t);

            t = Engine.DeltaTime * 4f;
            scale.X = Calc.Approach(scale.X, 1f, t);
            scale.Y = Calc.Approach(scale.Y, 1f, t);
        }

        private void SetEdgeColor(Color targetColor, Color currentColor) {
            EdgeColor = targetColor;
            currentEdgeColor = currentColor;
        }

        private void SetBackgroundColor(Color targetColor, Color currentColor) {
            BackgroundColor = targetColor;
            currentBackgroundColor = currentColor;
        }

        private DashCollisionResults Dashed(Player player, Vector2 direction) {
            if (!SaveData.Instance.Assists.Invincible && player.CollideCheck<Spikes>()) {
                return DashCollisionResults.NormalCollision;
            }

            if (colors[nextColorIndex].IsActive()) {
                return DashCollisionResults.NormalCollision;
            }
            // no switch, normal collision

            if (player.StateMachine.State == Player.StRedDash) {
                player.StateMachine.State = Player.StNormal;
            }

            Switch(direction);

            return DashCollisionResults.Rebound;
        }

        public void Switch(Vector2 direction) {
            scale = new Vector2(
                1f + (Math.Abs(direction.Y) * 0.5f - Math.Abs(direction.X) * 0.5f) / scaleStrength.X,
                1f + (Math.Abs(direction.X) * 0.5f - Math.Abs(direction.Y) * 0.5f) / scaleStrength.Y
            );

            if (random) {
                nextColorIndex = Calc.Random.Next(0, colors.Length);
            }

            VortexHelperModule.SessionProperties.SessionSwitchBlockColor = colors[nextColorIndex];
            Color col = VortexHelperModule.SessionProperties.SessionSwitchBlockColor.GetColor();

            UpdateColorSwitches(Scene, colors[nextColorIndex]);
            SetEdgeColor(defaultEdgeColor, col);
            currentBackgroundColor = Color.White;

            Audio.Play(CustomSFX.game_colorSwitch_hit, Center);
            if (SwitchBlock.RoomHasSwitchBlock(Scene, VortexHelperModule.SessionProperties.SessionSwitchBlockColor)) {
                Audio.Play(CustomSFX.game_switchBlock_switch,
                    "tone", VortexHelperModule.SessionProperties.SessionSwitchBlockColor.GetSoundParam()
                );
            }

            Input.Rumble(RumbleStrength.Strong, RumbleLength.Long);
            SceneAs<Level>().DirectionalShake(direction, 0.25f);
            StartShaking(0.25f);

            ParticleType p = LightningBreakerBox.P_Smash;
            p.Color = col; p.Color2 = Color.Lerp(col, Color.White, 0.5f);
            SmashParticles(direction.Perpendicular(), p);
            SmashParticles(-direction.Perpendicular(), p);
        }

        public static void UpdateColorSwitches(Scene scene, VortexHelperSession.SwitchBlockColor color) {
            foreach (ColorSwitch colorSwitch in scene.Tracker.GetEntities<ColorSwitch>()) {
                colorSwitch.NextColor(color, false);
            }
        }

        private void NextColor(VortexHelperSession.SwitchBlockColor colorNext, bool start) {
            if (colorNext == colors[nextColorIndex] && !singleColor) {
                if (!start) {
                    nextColorIndex++;
                }

                if (nextColorIndex > colors.Length - 1) {
                    nextColorIndex = 0;
                }

                if (colors[nextColorIndex].IsActive()) {
                    nextColorIndex++;
                }
            }
            BackgroundColor = colors[nextColorIndex].IsActive() ? defaultBackgroundColor : colors[nextColorIndex].GetColor();
        }

        private void SmashParticles(Vector2 dir, ParticleType smashParticle) {
            float direction;
            Vector2 position;
            Vector2 positionRange;
            int num;
            if (dir == Vector2.UnitX) {
                direction = 0f;
                position = CenterRight - Vector2.UnitX * 12f;
                positionRange = Vector2.UnitY * (Height - 6f) * 0.5f;
                num = (int)(Height / 8f) * 4;
            }
            else if (dir == -Vector2.UnitX) {
                direction = (float)Math.PI;
                position = CenterLeft + Vector2.UnitX * 12f;
                positionRange = Vector2.UnitY * (Height - 6f) * 0.5f;
                num = (int)(Height / 8f) * 4;
            }
            else if (dir == Vector2.UnitY) {
                direction = (float)Math.PI / 2f;
                position = BottomCenter - Vector2.UnitY * 12f;
                positionRange = Vector2.UnitX * (Width - 6f) * 0.5f;
                num = (int)(Width / 8f) * 4;
            }
            else {
                direction = -(float)Math.PI / 2f;
                position = TopCenter + Vector2.UnitY * 12f;
                positionRange = Vector2.UnitX * (Width - 6f) * 0.5f;
                num = (int)(Width / 8f) * 4;
            }
            num += 2;
            SceneAs<Level>().Particles.Emit(smashParticle, num, position, positionRange, direction);
        }
    }
}
