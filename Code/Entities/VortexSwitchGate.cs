using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;

namespace Celeste.Mod.VortexHelper.Entities;

[CustomEntity("VortexHelper/VortexSwitchGate")]
public class VortexSwitchGate : Solid
{
    public enum SwitchGateBehavior
    {
        Crush,
        Shatter
    }

    private static readonly ParticleType P_Behind = SwitchGate.P_Behind;
    private static readonly ParticleType P_Dust = SwitchGate.P_Dust;

    private Vector2 node;
    private readonly bool persistent;
    public SwitchGateBehavior Behavior;

    private Color inactiveColor = Calc.HexToColor("5fcde4");
    private Color activeColor = Color.White;
    private Color finishColor = Calc.HexToColor("f141df");

    private Vector2 iconOffset;
    private readonly Sprite icon;

    private readonly Wiggler wiggler;
    private readonly MTexture[,] nineSlice;
    private readonly SoundSource openSfx;
    private readonly string debrisPath;
    private readonly float crushSpeed;

    public VortexSwitchGate(EntityData data, Vector2 offset)
        : this(data.Position + offset, data.Width, data.Height, data.Nodes[0] + offset, data.Bool("persistent"), data.Attr("sprite", "block"), data.Enum("behavior", SwitchGateBehavior.Crush), data.Float("crushDuration")) { }

    public VortexSwitchGate(Vector2 position, int width, int height, Vector2 node, bool persistent, string spriteName, SwitchGateBehavior behavior, float crushSpeed)
        : base(position, width, height, safe: false)
    {
        this.node = node;
        this.persistent = persistent;
        this.crushSpeed = Calc.Min(Calc.Max(crushSpeed, 0.5f), 2f); // 0.5 < crushSpeed < 2 so the sound doesn't get messed up.

        this.Behavior = behavior;

        this.debrisPath = spriteName switch
        {
            "mirror" => "debris/VortexHelper/disintegate/2",
            "temple" => "debris/VortexHelper/disintegate/3",
            "stars" => "debris/VortexHelper/disintegate/4",
            _ => "debris/VortexHelper/disintegate/1",
        };
        Add(this.icon = new Sprite(GFX.Game, "objects/switchgate/icon")
        {
            Rate = 0f,
            Color = inactiveColor,
            Position = this.iconOffset = new Vector2(width / 2f, height / 2f)
        });

        this.icon.Add("spin", "", 0.1f, "spin");
        this.icon.Play("spin");
        this.icon.CenterOrigin();

        Add(this.wiggler = Wiggler.Create(0.5f, 4f, delegate (float f)
        {
            this.icon.Scale = Vector2.One * (1f + f);
        }));

        MTexture mTexture = GFX.Game["objects/switchgate/" + spriteName];
        this.nineSlice = new MTexture[3, 3];
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                this.nineSlice[i, j] = mTexture.GetSubtexture(new Rectangle(i * 8, j * 8, 8, 8));

        Add(this.openSfx = new SoundSource());
        Add(new LightOcclude(0.5f));
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        if (Switch.CheckLevelFlag(SceneAs<Level>()))
        {
            if (this.Behavior == SwitchGateBehavior.Crush)
            {
                MoveTo(this.node);
                this.icon.Rate = 0f;
                this.icon.SetAnimationFrame(0);
                this.icon.Color = this.finishColor;
            }
            else
                RemoveSelf();
        }
        else
        {
            if (this.Behavior == SwitchGateBehavior.Crush)
                Add(new Coroutine(CrushSequence(this.node)));
            else
                Add(new Coroutine(ShatterSequence()));
        }
    }

    public override void Render()
    {
        float w = this.Collider.Width / 8f - 1f;
        float h = this.Collider.Height / 8f - 1f;

        for (int i = 0; i <= w; i++)
        {
            for (int j = 0; j <= h; j++)
            {
                int tx = (i < w) ? Math.Min(i, 1) : 2;
                int ty = (j < h) ? Math.Min(j, 1) : 2;
                this.nineSlice[tx, ty].Draw(this.Position + this.Shake + new Vector2(i * 8, j * 8));
            }
        }

        this.icon.Position = this.iconOffset + this.Shake;
        this.icon.DrawOutline();

        base.Render();
    }

    private IEnumerator ShatterSequence()
    {
        Level level = SceneAs<Level>();
        while (!Switch.Check(this.Scene))
            yield return null;

        if (this.persistent)
            Switch.SetLevelFlag(level);

        this.openSfx.Play(SFX.game_gen_fallblock_shake);

        yield return 0.1f;
        StartShaking(0.5f);

        while (this.icon.Rate < 1f)
        {
            this.icon.Color = Color.Lerp(this.inactiveColor, this.finishColor, this.icon.Rate);
            this.icon.Rate += Engine.DeltaTime * 2f;
            yield return null;
        }

        yield return 0.1f;
        for (int m = 0; m < 32; m++)
        {
            float num = Calc.Random.NextFloat((float) Math.PI * 2f);
            SceneAs<Level>().ParticlesFG.Emit(TouchSwitch.P_Fire, this.Position + this.iconOffset + Calc.AngleToVector(num, 4f), num);
        }

        this.openSfx.Stop();
        Audio.Play(SFX.game_gen_wallbreak_stone, this.Center);
        Audio.Play(SFX.game_gen_touchswitch_gate_finish, this.Center);
        level.Shake();

        for (int i = 0; i < this.Width / 8f; i++)
        {
            for (int j = 0; j < this.Height / 8f; j++)
            {
                Debris debris = new Debris().orig_Init(this.Position + new Vector2(4 + i * 8, 4 + j * 8), '1').BlastFrom(this.Center);
                var debrisData = new DynData<Debris>(debris);
                debrisData.Get<Image>("image").Texture = GFX.Game[this.debrisPath];
                this.Scene.Add(debris);
            }
        }

        DestroyStaticMovers();
        RemoveSelf();
    }

    private IEnumerator CrushSequence(Vector2 node)
    {
        Level level = SceneAs<Level>();
        Vector2 start = this.Position;

        while (!Switch.Check(this.Scene))
            yield return null;

        if (this.persistent)
            Switch.SetLevelFlag(level);

        yield return 0.1f;
        StartShaking(0.5f);
        this.openSfx.Play(SFX.game_gen_touchswitch_gate_open);

        while (this.icon.Rate < 1f)
        {
            this.icon.Color = Color.Lerp(this.inactiveColor, this.activeColor, this.icon.Rate);
            this.icon.Rate += Engine.DeltaTime * 2f;
            yield return null;
        }

        yield return 0.1f;

        int particleAt = 0;
        var tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeIn, this.crushSpeed, start: true);
        tween.OnUpdate = delegate (Tween t)
        {
            MoveTo(Vector2.Lerp(start, node, t.Eased));
            if (this.Scene.OnInterval(0.1f))
            {
                particleAt++;
                particleAt %= 2;

                for (int n = 0; n < this.Width / 8f; n++)
                    for (int num2 = 0; num2 < this.Height / 8f; num2++)
                        if ((n + num2) % 2 == particleAt)
                            level.ParticlesBG.Emit(P_Behind, this.Position + new Vector2(n * 8, num2 * 8) + Calc.Random.Range(Vector2.One * 2f, Vector2.One * 6f));
            }
        };
        Add(tween);

        yield return this.crushSpeed;

        bool collidable = this.Collidable;
        this.Collidable = false;

        // Particles
        if (node.X <= start.X)
        {
            var value = new Vector2(0f, 2f);
            for (int i = 0; i < this.Height / 8f; i++)
            {
                var vector = new Vector2(this.Left - 1f, this.Top + 4f + i * 8);
                Vector2 point = vector + Vector2.UnitX;
                if (this.Scene.CollideCheck<Solid>(vector) && !this.Scene.CollideCheck<Solid>(point))
                {
                    level.ParticlesFG.Emit(P_Dust, vector + value, (float) Math.PI);
                    level.ParticlesFG.Emit(P_Dust, vector - value, (float) Math.PI);
                }
            }
        }
        if (node.X >= start.X)
        {
            var value2 = new Vector2(0f, 2f);
            for (int j = 0; j < this.Height / 8f; j++)
            {
                var vector2 = new Vector2(this.Right + 1f, this.Top + 4f + j * 8);
                Vector2 point2 = vector2 - Vector2.UnitX * 2f;
                if (this.Scene.CollideCheck<Solid>(vector2) && !this.Scene.CollideCheck<Solid>(point2))
                {
                    level.ParticlesFG.Emit(P_Dust, vector2 + value2, 0f);
                    level.ParticlesFG.Emit(P_Dust, vector2 - value2, 0f);
                }
            }
        }
        if (node.Y <= start.Y)
        {
            var value3 = new Vector2(2f, 0f);
            for (int k = 0; k < this.Width / 8f; k++)
            {
                var vector3 = new Vector2(this.Left + 4f + k * 8, this.Top - 1f);
                Vector2 point3 = vector3 + Vector2.UnitY;
                if (this.Scene.CollideCheck<Solid>(vector3) && !this.Scene.CollideCheck<Solid>(point3))
                {
                    level.ParticlesFG.Emit(P_Dust, vector3 + value3, -(float) Math.PI / 2f);
                    level.ParticlesFG.Emit(P_Dust, vector3 - value3, -(float) Math.PI / 2f);
                }
            }
        }
        if (node.Y >= start.Y)
        {
            var value4 = new Vector2(2f, 0f);
            for (int l = 0; l < this.Width / 8f; l++)
            {
                var vector4 = new Vector2(this.Left + 4f + l * 8, this.Bottom + 1f);
                Vector2 point4 = vector4 - Vector2.UnitY * 2f;
                if (this.Scene.CollideCheck<Solid>(vector4) && !this.Scene.CollideCheck<Solid>(point4))
                {
                    level.ParticlesFG.Emit(P_Dust, vector4 + value4, (float) Math.PI / 2f);
                    level.ParticlesFG.Emit(P_Dust, vector4 - value4, (float) Math.PI / 2f);
                }
            }
        }

        this.Collidable = collidable;
        Audio.Play(SFX.game_gen_touchswitch_gate_finish, this.Position);
        this.openSfx.Stop();
        StartShaking(0.2f);
        level.Shake();

        while (this.icon.Rate > 0f)
        {
            this.icon.Color = Color.Lerp(this.activeColor, this.finishColor, 1f - this.icon.Rate);
            this.icon.Rate -= Engine.DeltaTime * 4f;
            yield return null;
        }

        this.icon.Rate = 0f;
        this.icon.SetAnimationFrame(0);
        this.wiggler.Start();

        collidable = this.Collidable;
        this.Collidable = false;
        if (!this.Scene.CollideCheck<Solid>(this.Center))
        {
            for (int m = 0; m < 32; m++)
            {
                float num = Calc.Random.NextFloat((float) Math.PI * 2f);
                SceneAs<Level>().ParticlesFG.Emit(TouchSwitch.P_Fire, this.Position + this.iconOffset + Calc.AngleToVector(num, 4f), num);
            }
        }

        this.Collidable = collidable;
    }
}
