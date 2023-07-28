using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.VortexHelper.Entities;

[CustomEntity("VortexHelper/VortexCustomBumper")]
public class VortexBumper : Entity
{
    private readonly Sprite sprite;
    private readonly Sprite spriteEvil;

    private readonly VertexLight light;
    private readonly BloomPoint bloom;

    public static ParticleType P_GreenAmbience, P_OrangeAmbience;
    public static ParticleType P_GreenLaunch, P_OrangeLaunch;

    private readonly ParticleType p_ambiance, p_launch;

    private readonly Wiggler hitWiggler;
    private Vector2 hitDir;
    private readonly SineWave sine;

    private Vector2 anchor;
    private bool goBack;

    private float respawnTimer;
    private bool fireMode;

    private readonly bool twoDashes, oneUse;
    private bool deadly;

    private readonly bool notCoreMode, wobble;

    public VortexBumper(EntityData data, Vector2 offset)
        : this(data.Position + offset, data.FirstNodeNullable(offset), data.Attr("style", "Green"), data.Bool("notCoreMore"), data.Bool("wobble", true), data.Attr("sprite").Trim().TrimEnd('/')) { }

    public VortexBumper(Vector2 position, Vector2? node, string style, bool notCoreMode, bool wobble, string customSpritePath)
        : base(position)
    {
        this.Collider = new Circle(12f);
        Add(new PlayerCollider(OnPlayer));

        Add(this.sine = new SineWave(0.44f, 0f).Randomize());
        this.wobble = wobble;

        if (!string.IsNullOrEmpty(customSpritePath))
            this.sprite = BuildCustomSprite(customSpritePath, style.ToLower());

        switch (style)
        {
            default:
            case "Green":
                this.sprite ??= VortexHelperModule.VortexBumperSpriteBank.Create("greenBumper");
                this.twoDashes = true;
                this.p_ambiance = P_GreenAmbience;
                this.p_launch = P_GreenLaunch;
                break;

            case "Orange":
                this.sprite ??= VortexHelperModule.VortexBumperSpriteBank.Create("orangeBumper");
                this.oneUse = true;
                this.p_ambiance = P_OrangeAmbience;
                this.p_launch = P_OrangeLaunch;
                break;
        }
        this.notCoreMode = notCoreMode;

        Add(this.sprite);
        Add(this.spriteEvil = GFX.SpriteBank.Create("bumper_evil"));
        this.spriteEvil.Visible = false;

        Add(this.light = new VertexLight(this.twoDashes ? Color.DarkGreen : Color.DarkOrange, 1f, 16, 32));
        Add(this.bloom = new BloomPoint(0.5f, 16f));

        this.anchor = this.Position;
        if (node.HasValue)
        {
            Vector2 start = this.Position;
            Vector2 end = node.Value;
            var tween = Tween.Create(Tween.TweenMode.Looping, Ease.CubeInOut, 1.81818187f, start: true);
            tween.OnUpdate = t => this.anchor = this.goBack ? Vector2.Lerp(end, start, t.Eased) : Vector2.Lerp(start, end, t.Eased);
            tween.OnComplete = _ => this.goBack = !this.goBack;
            Add(tween);
        }

        UpdatePosition();

        Add(this.hitWiggler = Wiggler.Create(1.2f, 2f, delegate
        {
            this.spriteEvil.Position = this.hitDir * this.hitWiggler.Value * 8f;
        }));

        if (!notCoreMode)
            Add(new CoreModeListener(OnChangeMode));
    }

    private static Sprite BuildCustomSprite(string path, string name)
    {
        var sprite = new Sprite(GFX.Game, path + "/");

        // <Anim id="on" path=name frames="42-44" delay="0.06" goto="idle"/>
        sprite.Add("on", name, 0.06f, "idle", 42, 43, 44);

        // <Loop id="idle" path=name frames="0-33" delay="0.06"/>
        sprite.AddLoop("idle", name, 0.06f, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33); // yes

        // <Anim id="hit" path=name frames="34-42" delay="0.06" goto="off"/>
        sprite.Add("hit", name, 0.06f, "off", 34, 35, 36, 37, 38, 39, 40, 41, 42);

        // <Loop id="off" path=name frames="42" delay="0.06"/>
        sprite.AddLoop("off", name, 0.06f, 42);

        sprite.JustifyOrigin(0.5f, 0.5f);
        sprite.Play("idle");
        return sprite;
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);
        if (this.notCoreMode)
            return;

        this.fireMode = SceneAs<Level>().CoreMode == Session.CoreModes.Hot;
        this.spriteEvil.Visible = this.fireMode;
        this.sprite.Visible = !this.fireMode;
    }

    private void OnChangeMode(Session.CoreModes coreMode)
    {
        this.fireMode = coreMode == Session.CoreModes.Hot;
        if (!this.fireMode && this.deadly)
            return;

        this.spriteEvil.Visible = this.fireMode;
        this.sprite.Visible = !this.fireMode;
    }

    private void UpdatePosition()
        => this.Position = this.anchor + new Vector2(this.sine.Value * 3f, this.sine.ValueOverTwo * 2f) * (this.wobble ? 1f : 0f);

    public override void Update()
    {
        base.Update();
        if (this.respawnTimer > 0f)
        {
            this.respawnTimer -= Engine.DeltaTime;
            if (this.respawnTimer <= 0f)
            {
                this.light.Visible = true;
                this.bloom.Visible = true;

                this.sprite.Play("on");
                this.spriteEvil.Play("on");

                if (this.deadly)
                {
                    this.fireMode = true;
                    this.spriteEvil.Visible = this.fireMode;
                    this.sprite.Visible = !this.fireMode;
                }

                Audio.Play(SFX.game_06_pinballbumper_reset, this.Position);
            }
        }
        else if (this.Scene.OnInterval(0.05f))
        {
            float num = Calc.Random.NextAngle();
            float direction = this.fireMode ? (-(float) Math.PI / 2f) : num;
            float length = this.fireMode ? 12 : 8;

            ParticleType type = this.fireMode ? Bumper.P_FireAmbience : this.p_ambiance;
            SceneAs<Level>().Particles.Emit(type, 1, this.Center + Calc.AngleToVector(num, length), Vector2.One * 2f, direction);
        }

        UpdatePosition();
    }

    private void OnPlayer(Player player)
    {
        Level level = SceneAs<Level>();
        if (this.fireMode)
        {
            if (!SaveData.Instance.Assists.Invincible)
            {
                Vector2 vector = (player.Center - this.Center).SafeNormalize();
                this.hitDir = -vector;
                this.hitWiggler.Start();
                Audio.Play(SFX.game_09_hotpinball_activate, this.Position);
                this.respawnTimer = 0.6f;
                player.Die(vector);
                level.Particles.Emit(Bumper.P_FireHit, 12, this.Center + vector * 12f, Vector2.One * 3f, vector.Angle());
            }
        }
        else if (this.respawnTimer <= 0f)
        {
            Audio.Play(SFX.game_06_pinballbumper_hit, this.Position);
            this.respawnTimer = 0.6f;
            Vector2 vector2 = player.ExplodeLaunch(this.Position, snapUp: false, sidesOnly: false);

            if (this.twoDashes)
                player.Dashes = 2;
            if (this.oneUse)
                this.deadly = true;

            this.sprite.Play("hit", restart: true);
            this.spriteEvil.Play("hit", restart: true);

            this.light.Visible = false;
            this.bloom.Visible = false;

            level.DirectionalShake(vector2, 0.15f);
            level.Displacement.AddBurst(this.Center, 0.3f, 8f, 32f, 0.8f);
            level.Particles.Emit(this.p_launch, 12, this.Center + vector2 * 12f, Vector2.One * 3f, vector2.Angle());
        }
    }

    public static void InitializeParticles()
    {
        P_GreenAmbience = new ParticleType(Bumper.P_Ambience)
        {
            Color = Calc.HexToColor("5dcc47"),
            Color2 = Calc.HexToColor("c4ffc9")
        };

        P_OrangeAmbience = new ParticleType(Bumper.P_Ambience)
        {
            Color = Calc.HexToColor("cc9747"),
            Color2 = Calc.HexToColor("ffdfc4")
        };

        P_GreenLaunch = new ParticleType(Bumper.P_Launch)
        {
            Color = P_GreenAmbience.Color,
            Color2 = P_GreenAmbience.Color2
        };

        P_OrangeLaunch = new ParticleType(Bumper.P_Launch)
        {
            Color = P_OrangeAmbience.Color,
            Color2 = P_OrangeAmbience.Color2
        };
    }
}
