using Celeste.Mod.Entities;
using Celeste.Mod.VortexHelper.Misc.Extensions;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Linq;

namespace Celeste.Mod.VortexHelper.Entities;

[CustomEntity("VortexHelper/ColorSwitch")]
[Tracked(false)]
public class ColorSwitch : Solid
{
    private uint seed;

    private readonly MTexture[,] edges = new MTexture[3, 3];

    private static readonly Color defaultBackgroundColor = Calc.HexToColor("191919");
    private static readonly Color defaultEdgeColor = Calc.HexToColor("646464");

    private Color BackgroundColor = defaultBackgroundColor;
    private Color EdgeColor = defaultEdgeColor;
    private Color currentEdgeColor, currentBackgroundColor;

    private Vector2 scale = Vector2.One;
    private Vector2 scaleStrength = Vector2.One;

    private readonly VortexHelperSession.SwitchBlockColor[] colors;
    private int nextColorIndex = 0;
    private readonly bool singleColor, random;

    public ColorSwitch(EntityData data, Vector2 offset)
        : this(data.Position + offset, data.Width, data.Height,
              data.Bool("blue"), data.Bool("rose"), data.Bool("orange"), data.Bool("lime"), data.Bool("random"))
    { }

    public ColorSwitch(Vector2 position, int width, int height, bool blue, bool rose, bool orange, bool lime, bool random)
        : base(position, width, height, true)
    {
        this.SurfaceSoundIndex = SurfaceIndex.ZipMover;

        // if all are false, then block is useless, so make it not useless.
        if (!blue && !rose && !orange && !lime)
            blue = rose = orange = lime = true;

        string block = "objects/VortexHelper/onoff/switch";
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                this.edges[i, j] = GFX.Game[block].GetSubtexture(i * 8, j * 8, 8, 8);

        this.random = random;

        bool[] colorBools = new bool[] { blue, rose, orange, lime };
        int colorArraySize = colorBools.Count(b => b);

        this.colors = new VortexHelperSession.SwitchBlockColor[colorArraySize];
        this.singleColor = colorArraySize == 1;

        int arrIdx = 0;
        for (int i = 0; i < colorBools.Length; i++)
            if (colorBools[i])
                this.colors[arrIdx++] = (VortexHelperSession.SwitchBlockColor) i;

        NextColor(this.colors[this.nextColorIndex], true);

        Add(new LightOcclude());
        Add(new SoundSource(SFX.game_01_console_static_loop)
        {
            Position = new Vector2(this.Width, this.Height) / 2f,
        });

        if (width > 32)
            this.scaleStrength.X = width / 32f;
        if (height > 32)
            this.scaleStrength.Y = height / 32f;

        Color col = this.colors[this.nextColorIndex].IsActive() ? defaultBackgroundColor : this.colors[this.nextColorIndex].GetColor();
        SetEdgeColor(this.EdgeColor, this.EdgeColor);
        SetBackgroundColor(col, col);

        this.OnDashCollide = Dashed;
    }

    public override void Render()
    {
        Vector2 position = this.Position;
        this.Position += this.Shake;

        int x = (int) (this.Center.X + (this.X + 1 - this.Center.X) * this.scale.X);
        int y = (int) (this.Center.Y + (this.Y + 1 - this.Center.Y) * this.scale.Y);
        int rectW = (int) ((this.Width - 2) * this.scale.X);
        int rectH = (int) ((this.Height - 2) * this.scale.Y);
        var rect = new Rectangle(x, y, rectW, rectH);

        Color col = this.random
            ? Color.Lerp(defaultBackgroundColor, Color.White, (float) (0.05f * Math.Sin(this.Scene.TimeActive * 5f)) + 0.05f)
            : this.BackgroundColor != defaultBackgroundColor
                ? Color.Lerp(this.currentBackgroundColor, Color.Black, 0.2f)
                : this.currentBackgroundColor;

        Draw.Rect(rect, col);

        for (int i = rect.Y; i < rect.Bottom; i += 2)
        {
            float scale = 0.05f + (1f + (float) Math.Sin(i / 16f + this.Scene.TimeActive * 2f)) / 2f * 0.2f;
            Draw.Line(rect.X, i, rect.X + rect.Width, i, Color.White * 0.55f * scale);
        }

        uint tempseed = this.seed;
        PlaybackBillboard.DrawNoise(rect, ref tempseed, Color.White * 0.05f);

        int w = (int) (this.Width / 8f);
        int h = (int) (this.Height / 8f);

        for (int i = 0; i < w; i++)
        {
            for (int j = 0; j < h; j++)
            {
                int tx = (i != 0) ? ((i != w - 1f) ? 1 : 2) : 0;
                int ty = (j != 0) ? ((j != h - 1f) ? 1 : 2) : 0;

                if (tx == 1 && ty == 1)
                    continue;

                Vector2 renderPos = new Vector2(i, j) * 8f + Vector2.One * 4f + this.Position;
                renderPos = this.Center + (renderPos - Center) * scale;
                this.edges[tx, ty].DrawCentered(renderPos, this.currentEdgeColor, this.scale);
            }
        }

        base.Render();
        this.Position = position;
    }

    public override void Update()
    {
        base.Update();

        if (this.Scene.OnInterval(0.1f))
            this.seed++;

        float t = Calc.Min(1f, 4f * Engine.DeltaTime);
        this.currentEdgeColor = Color.Lerp(this.currentEdgeColor, this.EdgeColor, t);
        this.currentBackgroundColor = Color.Lerp(this.currentBackgroundColor, this.BackgroundColor, t);

        this.scale = Calc.Approach(this.scale, Vector2.One, Engine.DeltaTime * 4f);
    }

    private void SetEdgeColor(Color targetColor, Color currentColor)
    {
        this.EdgeColor = targetColor;
        this.currentEdgeColor = currentColor;
    }

    private void SetBackgroundColor(Color targetColor, Color currentColor)
    {
        this.BackgroundColor = targetColor;
        this.currentBackgroundColor = currentColor;
    }

    private DashCollisionResults Dashed(Player player, Vector2 direction)
    {
        if (!SaveData.Instance.Assists.Invincible && player.CollideCheck<Spikes>())
            return DashCollisionResults.NormalCollision;

        if (this.colors[this.nextColorIndex].IsActive())
            return DashCollisionResults.NormalCollision;

        // no switch, normal collision
        if (player.StateMachine.State == Player.StRedDash)
            player.StateMachine.State = Player.StNormal;

        Switch(direction);
        return DashCollisionResults.Rebound;
    }

    public void Switch(Vector2 direction)
    {
        this.scale = new Vector2(
            1f + (Math.Abs(direction.Y) * 0.5f - Math.Abs(direction.X) * 0.5f) / this.scaleStrength.X,
            1f + (Math.Abs(direction.X) * 0.5f - Math.Abs(direction.Y) * 0.5f) / this.scaleStrength.Y
        );

        if (this.random)
            this.nextColorIndex = Calc.Random.Next(0, this.colors.Length);

        VortexHelperModule.SessionProperties.SessionSwitchBlockColor = this.colors[this.nextColorIndex];
        Color col = VortexHelperModule.SessionProperties.SessionSwitchBlockColor.GetColor();

        UpdateColorSwitches(this.Scene, this.colors[this.nextColorIndex]);
        SetEdgeColor(defaultEdgeColor, col);
        this.currentBackgroundColor = Color.White;

        Audio.Play(CustomSFX.game_colorSwitch_hit, this.Center);
        if (SwitchBlock.RoomHasSwitchBlock(this.Scene, VortexHelperModule.SessionProperties.SessionSwitchBlockColor))
            Audio.Play(CustomSFX.game_switchBlock_switch, "tone", VortexHelperModule.SessionProperties.SessionSwitchBlockColor.GetSoundParam());

        Input.Rumble(RumbleStrength.Strong, RumbleLength.Long);
        SceneAs<Level>().DirectionalShake(direction, 0.25f);
        StartShaking(0.25f);

        ParticleType p = LightningBreakerBox.P_Smash;
        p.Color = col; p.Color2 = Color.Lerp(col, Color.White, 0.5f);
        SmashParticles(direction.Perpendicular(), p);
        SmashParticles(-direction.Perpendicular(), p);
    }

    public static void UpdateColorSwitches(Scene scene, VortexHelperSession.SwitchBlockColor color)
    {
        foreach (ColorSwitch colorSwitch in scene.Tracker.GetEntities<ColorSwitch>())
            colorSwitch.NextColor(color, false);
    }

    private void NextColor(VortexHelperSession.SwitchBlockColor colorNext, bool start)
    {
        if (colorNext == this.colors[this.nextColorIndex] && !this.singleColor)
        {
            if (!start)
                this.nextColorIndex++;

            if (this.nextColorIndex > this.colors.Length - 1)
                this.nextColorIndex = 0;

            if (this.colors[this.nextColorIndex].IsActive())
                this.nextColorIndex++;
        }

        this.BackgroundColor = this.colors[this.nextColorIndex].IsActive() ? defaultBackgroundColor : this.colors[this.nextColorIndex].GetColor();
    }

    private void SmashParticles(Vector2 dir, ParticleType smashParticle)
    {
        float direction;
        Vector2 position;
        Vector2 positionRange;
        int num;

        if (dir == Vector2.UnitX)
        {
            direction = 0f;
            position = this.CenterRight - Vector2.UnitX * 12f;
            positionRange = Vector2.UnitY * (this.Height - 6f) * 0.5f;
            num = (int) (this.Height / 8f) * 4;
        }
        else if (dir == -Vector2.UnitX)
        {
            direction = (float) Math.PI;
            position = this.CenterLeft + Vector2.UnitX * 12f;
            positionRange = Vector2.UnitY * (this.Height - 6f) * 0.5f;
            num = (int) (this.Height / 8f) * 4;
        }
        else if (dir == Vector2.UnitY)
        {
            direction = (float) Math.PI / 2f;
            position = this.BottomCenter - Vector2.UnitY * 12f;
            positionRange = Vector2.UnitX * (this.Width - 6f) * 0.5f;
            num = (int) (this.Width / 8f) * 4;
        }
        else
        {
            direction = -(float) Math.PI / 2f;
            position = this.TopCenter + Vector2.UnitY * 12f;
            positionRange = Vector2.UnitX * (this.Width - 6f) * 0.5f;
            num = (int) (this.Width / 8f) * 4;
        }

        num += 2;
        SceneAs<Level>().Particles.Emit(smashParticle, num, position, positionRange, direction);
    }
}
