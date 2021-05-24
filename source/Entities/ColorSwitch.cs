using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using VortexHelper;

namespace Celeste.Mod.VortexHelper.Entities {
    [CustomEntity("VortexHelper/ColorSwitch")]
    [Tracked(false)]
    class ColorSwitch : Solid {
        private uint Seed;

        private MTexture[,] edges = new MTexture[3, 3];
        private SoundSource idleSfx = new SoundSource();

        private static readonly Color defaultBackgroundColor = Calc.HexToColor("191919");
        private static readonly Color defaultEdgeColor = Calc.HexToColor("646464");

        private static readonly Color blueColor = Calc.HexToColor("3232ff");
        private static readonly Color roseColor = Calc.HexToColor("ff3265");
        private static readonly Color orangeColor = Calc.HexToColor("ff9532");
        private static readonly Color limeColor = Calc.HexToColor("9cff32");

        private Color BackgroundColor = defaultBackgroundColor;
        private Color EdgeColor = defaultEdgeColor;
        private Color actualEdgeColor, actualBackgroundColor;

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
            SurfaceSoundIndex = 9;

            if (!blue && !rose && !orange && !lime) {
                blue = rose = orange = lime = true; // lame but simple fix
            }

            string block = "objects/VortexHelper/onoff/switch";

            /* let's go, tiles. */
            int i, j;
            for (i = 0; i < 3; i++) {
                for (j = 0; j < 3; j++) {
                    edges[i, j] = GFX.Game[block].GetSubtexture(i * 8, j * 8, 8, 8);
                }
            }

            this.random = random;

            // Weird color stuff...
            int colorArraySize = 0;
            if (blue) {
                colorArraySize++;
            }

            if (rose) {
                colorArraySize++;
            }

            if (orange) {
                colorArraySize++;
            }

            if (lime) {
                colorArraySize++;
            }

            colors = new VortexHelperSession.SwitchBlockColor[colorArraySize];
            if (colorArraySize == 1) {
                singleColor = true;
            }

            i = -1;
            if (blue) { i++; colors[i] = VortexHelperSession.SwitchBlockColor.Blue; }
            if (rose) { i++; colors[i] = VortexHelperSession.SwitchBlockColor.Rose; }
            if (orange) { i++; colors[i] = VortexHelperSession.SwitchBlockColor.Orange; }
            if (lime) { i++; colors[i] = VortexHelperSession.SwitchBlockColor.Lime; }
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
            Color col = colors[nextColorIndex] == VortexHelperModule.SessionProperties.switchBlockColor ? defaultBackgroundColor : GetColor(colors[nextColorIndex]);
            SetBackgroundColor(col, col);
            OnDashCollide = Dashed;
        }

        public override void Awake(Scene scene) {
            base.Awake(scene);
            idleSfx.Play("event:/game/01_forsaken_city/console_static_loop");
        }

        public override void Render() {
            Vector2 position = Position;
            Position += base.Shake;

            int x = (int)(Center.X + (X + 1 - Center.X) * scale.X);
            int y = (int)(Center.Y + (Y + 1 - Center.Y) * scale.Y);
            int rectW = (int)((Width - 2) * scale.X);
            int rectH = (int)((Height - 2) * scale.Y);
            Rectangle rect = new Rectangle(x, y, rectW, rectH);

            uint seed = Seed;

            Color col = random ?
                Color.Lerp(defaultBackgroundColor, Color.White, (float)(0.05f * Math.Sin(base.Scene.TimeActive * 5f)) + 0.05f)
                : (BackgroundColor != defaultBackgroundColor ?

                    Color.Lerp(actualBackgroundColor, Color.Black, 0.2f)
                    : actualBackgroundColor);

            Draw.Rect(rect, col);

            for (int i = rect.Y; (float)i < rect.Bottom; i += 2) {
                float scale = 0.05f + (1f + (float)Math.Sin(i / 16f + base.Scene.TimeActive * 2f)) / 2f * 0.2f;
                Draw.Line(rect.X, i, rect.X + rect.Width, i, Color.White * 0.55f * scale);
            }
            PlaybackBillboard.DrawNoise(rect, ref seed, Color.White * 0.05f);

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

                        edges[num4, num5].DrawCentered(renderPos, actualEdgeColor, scale);
                    }
                }
            }


            base.Render();
            Position = position;
        }

        public override void Update() {
            base.Update();

            if (base.Scene.OnInterval(0.1f)) {
                Seed++;
            }
            float t = Calc.Min(1f, 4f * Engine.DeltaTime);
            actualEdgeColor = Color.Lerp(actualEdgeColor, EdgeColor, t);
            actualBackgroundColor = Color.Lerp(actualBackgroundColor, BackgroundColor, t);

            t = Engine.DeltaTime * 4f;
            scale.X = Calc.Approach(scale.X, 1f, t);
            scale.Y = Calc.Approach(scale.Y, 1f, t);
            // unsquish :(
        }

        private void SetEdgeColor(Color targetColor, Color actualColor) {
            EdgeColor = targetColor;
            actualEdgeColor = actualColor;
        }

        private void SetBackgroundColor(Color targetColor, Color actualColor) {
            BackgroundColor = targetColor;
            actualBackgroundColor = actualColor;
        }

        private DashCollisionResults Dashed(Player player, Vector2 direction) {
            if (!SaveData.Instance.Assists.Invincible && player.CollideCheck<Spikes>()) {
                return DashCollisionResults.NormalCollision;
            }

            if (colors[nextColorIndex] == VortexHelperModule.SessionProperties.switchBlockColor) {
                return DashCollisionResults.NormalCollision;
            }
            // no switch, normal collision

            if (player.StateMachine.State == 5) {
                player.StateMachine.State = 0;
            }

            Switch(direction);

            return DashCollisionResults.Rebound;
        }

        public void Switch(Vector2 direction) {
            scale = new Vector2(
                1f + (Math.Abs(direction.Y) * 0.5f - Math.Abs(direction.X) * 0.5f) / scaleStrength.X,
                1f + (Math.Abs(direction.X) * 0.5f - Math.Abs(direction.Y) * 0.5f) / scaleStrength.Y);
            //that's a squishification vector!

            if (random) {
                nextColorIndex = Calc.Random.Next(0, colors.Length);
            }

            VortexHelperModule.SessionProperties.switchBlockColor = colors[nextColorIndex];
            Color col = GetColor(VortexHelperModule.SessionProperties.switchBlockColor);
            UpdateColorSwitches(Scene, colors[nextColorIndex]);
            SetEdgeColor(defaultEdgeColor, col);
            actualBackgroundColor = Color.White;

            Audio.Play(CustomSFX.game_colorSwitch_hit, base.Center);
            if (SwitchBlock.RoomHasSwitchBlock(Scene, VortexHelperModule.SessionProperties.switchBlockColor)) {
                Audio.Play(CustomSFX.game_switchBlock_switch,
                    "tone", GetSoundParam(VortexHelperModule.SessionProperties.switchBlockColor));
            }

            (base.Scene as Level).DirectionalShake(direction, 0.25f);
            StartShaking(0.25f);

            ParticleType p = LightningBreakerBox.P_Smash;
            p.Color = col; p.Color2 = Color.Lerp(col, Color.White, 0.5f);
            Input.Rumble(RumbleStrength.Strong, RumbleLength.Long);
            SmashParticles(direction.Perpendicular(), p);
            SmashParticles(-direction.Perpendicular(), p);
        }

        private static Color GetColor(VortexHelperSession.SwitchBlockColor color) {
            switch (color) {
                default:
                case VortexHelperSession.SwitchBlockColor.Blue:
                    return blueColor;
                case VortexHelperSession.SwitchBlockColor.Rose:
                    return roseColor;
                case VortexHelperSession.SwitchBlockColor.Orange:
                    return orangeColor;
                case VortexHelperSession.SwitchBlockColor.Lime:
                    return limeColor;
            }
        }

        public static int GetSoundParam(VortexHelperSession.SwitchBlockColor color) {
            switch (color) {
                default:
                case VortexHelperSession.SwitchBlockColor.Blue:
                    return 0;
                case VortexHelperSession.SwitchBlockColor.Rose:
                    return 1;
                case VortexHelperSession.SwitchBlockColor.Orange:
                    return 2;
                case VortexHelperSession.SwitchBlockColor.Lime:
                    return 3;
            }
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

                if (colors[nextColorIndex] == VortexHelperModule.SessionProperties.switchBlockColor) {
                    nextColorIndex++;
                }
            }
            BackgroundColor = colors[nextColorIndex] == VortexHelperModule.SessionProperties.switchBlockColor ? defaultBackgroundColor : GetColor(colors[nextColorIndex]);
        }

        private void SmashParticles(Vector2 dir, ParticleType smashParticle) {
            float direction;
            Vector2 position;
            Vector2 positionRange;
            int num;
            if (dir == Vector2.UnitX) {
                direction = 0f;
                position = base.CenterRight - Vector2.UnitX * 12f;
                positionRange = Vector2.UnitY * (base.Height - 6f) * 0.5f;
                num = (int)(base.Height / 8f) * 4;
            }
            else if (dir == -Vector2.UnitX) {
                direction = (float)Math.PI;
                position = base.CenterLeft + Vector2.UnitX * 12f;
                positionRange = Vector2.UnitY * (base.Height - 6f) * 0.5f;
                num = (int)(base.Height / 8f) * 4;
            }
            else if (dir == Vector2.UnitY) {
                direction = (float)Math.PI / 2f;
                position = base.BottomCenter - Vector2.UnitY * 12f;
                positionRange = Vector2.UnitX * (base.Width - 6f) * 0.5f;
                num = (int)(base.Width / 8f) * 4;
            }
            else {
                direction = -(float)Math.PI / 2f;
                position = base.TopCenter + Vector2.UnitY * 12f;
                positionRange = Vector2.UnitX * (base.Width - 6f) * 0.5f;
                num = (int)(base.Width / 8f) * 4;
            }
            num += 2;
            SceneAs<Level>().Particles.Emit(smashParticle, num, position, positionRange, direction);
        }
    }
}
