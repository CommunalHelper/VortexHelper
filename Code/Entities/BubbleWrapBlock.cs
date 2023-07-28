using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;

namespace Celeste.Mod.VortexHelper.Entities;

[CustomEntity("VortexHelper/BubbleWrapBlock")]
[Tracked(false)]
public class BubbleWrapBlock : Solid
{
    public static ParticleType P_Respawn;

    private enum States
    {
        Idle,
        Gone
    }

    private States state = States.Idle;

    private readonly bool canDash;
    private readonly float respawnTime;
    private float timer;
    private float rectEffectInflate = 0f;

    private readonly SoundSource breakSfx;

    private readonly MTexture[,,] nineSlice;

    private Vector2 wobbleScale = Vector2.One;
    private readonly Wiggler wobble;

    public BubbleWrapBlock(EntityData data, Vector2 offset)
        : this(data.Position + offset, data.Width, data.Height, data.Bool("canDash"), data.Float("respawnTime")) { }

    public BubbleWrapBlock(Vector2 position, int width, int height, bool canDash, float respawnTime)
        : base(position, width, height, safe: true)
    {
        this.SurfaceSoundIndex = SurfaceIndex.Brick;

        this.canDash = canDash;
        this.respawnTime = respawnTime;

        MTexture block = GFX.Game["objects/VortexHelper/bubbleWrapBlock/bubbleBlock"];
        MTexture outline = GFX.Game["objects/VortexHelper/bubbleWrapBlock/bubbleOutline"];

        this.nineSlice = new MTexture[3, 3, 2];
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                this.nineSlice[i, j, 0] = block.GetSubtexture(new Rectangle(i * 8, j * 8, 8, 8));
                this.nineSlice[i, j, 1] = outline.GetSubtexture(new Rectangle(i * 8, j * 8, 8, 8));
            }
        }

        Add(new LightOcclude());
        Add(this.breakSfx = new SoundSource());
        Add(this.wobble = Wiggler.Create(.75f, 4.2f, delegate (float v)
        {
            this.wobbleScale = new Vector2(1f + v * 0.12f, 1f - v * 0.12f);
        }));

        this.OnDashCollide = Dashed;
    }

    public override void Render()
    {
        Vector2 drawOffset = Vector2.One * 4;

        float w = this.Collider.Width / 8f - 1f;
        float h = this.Collider.Height / 8f - 1f;

        for (int i = 0; i <= w; i++)
        {
            for (int j = 0; j <= h; j++)
            {
                int tx = (i < w) ? Math.Min(i, 1) : 2;
                int ty = (j < h) ? Math.Min(j, 1) : 2;

                Vector2 pos = this.Position + new Vector2(i * 8, j * 8);
                Vector2 newPos = this.Center + (pos - this.Center) * this.wobbleScale + this.Shake;

                this.nineSlice[tx, ty, this.state == States.Idle ? 0 : 1].DrawCentered(newPos + drawOffset, Color.White, this.wobbleScale);
            }
        }

        if (this.state == States.Gone)
            DrawInflatedHollowRectangle((int) this.X, (int) this.Y, (int) this.Width, (int) this.Height, (int) this.rectEffectInflate, Color.White * ((3 - this.rectEffectInflate) / 3));

        base.Render();
    }

    private void RespawnParticles()
    {
        Level level = SceneAs<Level>();

        for (int i = 0; i < this.Width; i += 4)
        {
            level.Particles.Emit(P_Respawn, new Vector2(this.X + 2f + i + Calc.Random.Range(-1, 1), this.Y), -(float) Math.PI / 2f);
            level.Particles.Emit(P_Respawn, new Vector2(this.X + 2f + i + Calc.Random.Range(-1, 1), this.Bottom - 1f), (float) Math.PI / 2f);
        }

        for (int j = 0; j < this.Height; j += 4)
        {
            level.Particles.Emit(P_Respawn, new Vector2(this.X, this.Y + 2f + j + Calc.Random.Range(-1, 1)), (float) Math.PI);
            level.Particles.Emit(P_Respawn, new Vector2(this.Right - 1f, this.Y + 2f + j + Calc.Random.Range(-1, 1)), 0f);
        }
    }

    private void DrawInflatedHollowRectangle(int x, int y, int width, int height, int inflate, Color color)
        => Draw.HollowRect(x - inflate, y - inflate, width + 2 * inflate, height + 2 * inflate, color);

    public DashCollisionResults Dashed(Player player, Vector2 direction)
    {
        if (!SaveData.Instance.Assists.Invincible && player.CollideCheck<Spikes>())
            return DashCollisionResults.NormalCollision;

        if (!this.canDash)
            return DashCollisionResults.NormalCollision;

        if (this.state == States.Gone)
            return DashCollisionResults.Ignore;

        if (player.StateMachine.State == Player.StRedDash)
            player.StateMachine.State = Player.StNormal;

        Break();

        return DashCollisionResults.Rebound;
    }

    public void Break()
    {
        this.breakSfx.Play(SFX.game_gen_wallbreak_stone);

        for (int i = 0; i < this.Width / 8f; i++)
        {
            for (int j = 0; j < this.Height / 8f; j++)
            {
                Debris debris = new Debris().orig_Init(this.Position + new Vector2(4 + i * 8, 4 + j * 8), '1').BlastFrom(this.Center);
                var debrisData = new DynData<Debris>(debris);
                debrisData.Get<Image>("image").Texture = GFX.Game["debris/VortexHelper/BubbleWrapBlock"];
                this.Scene.Add(debris);
            }
        }

        SceneAs<Level>().Shake(0.1f);
        DisableStaticMovers();

        this.state = States.Gone;
        this.Collidable = false;
        this.timer = this.respawnTime;
    }

    public override void Update()
    {
        base.Update();

        if (this.timer > 0f)
            this.timer -= Engine.DeltaTime;

        if (this.timer <= 0f)
            if (CheckEntitySafe())
                Respawn();

        if (this.state == States.Gone)
            this.rectEffectInflate = Calc.Approach(this.rectEffectInflate, 3, 20 * Engine.DeltaTime);
    }

    private void Respawn()
    {
        if (this.Collidable)
            return;

        this.wobble.Start();
        RespawnParticles();
        this.rectEffectInflate = 0f;

        EnableStaticMovers();
        this.breakSfx.Play(SFX.game_05_redbooster_reappear);
        this.Collidable = true;
        this.state = States.Idle;
    }

    private bool CheckEntitySafe()
    {
        foreach (Solid e in this.Scene.Tracker.GetEntities<Solid>())
            if (e is CrushBlock or MoveBlock && CollideCheck(e))
                return false;

        return !CollideCheck<Actor>()
            && !CollideCheck<FallingBlock>()
            && !CollideCheck<AutoFallingBlock>();
    }

    public static void InitializeParticles() => P_Respawn = new ParticleType
    {
        Color = Color.Lerp(Color.Purple, Color.White, .2f),
        FadeMode = ParticleType.FadeModes.Late,
        SpeedMin = 20f,
        SpeedMax = 50f,
        SpeedMultiplier = 0.1f,
        Size = 1f,
        LifeMin = 0.4f,
        LifeMax = 0.8f,
        DirectionRange = (float) Math.PI / 6f
    };
}
