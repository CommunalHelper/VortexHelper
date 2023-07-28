using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;

namespace Celeste.Mod.VortexHelper.Entities;

[CustomEntity("VortexHelper/AutoFallingBlock")]
[Tracked]
public class AutoFallingBlock : Solid
{
    private readonly TileGrid tiles;
    private readonly char TileType;
    private readonly int originalY;

    public AutoFallingBlock(EntityData data, Vector2 offset)
    : this(data.Position + offset, data.Char("tiletype", '3'), data.Width, data.Height) { }

    public AutoFallingBlock(Vector2 position, char tile, int width, int height)
        : base(position, width, height, safe: false)
    {
        this.originalY = (int) position.Y;

        int newSeed = Calc.Random.Next();
        Calc.PushRandom(newSeed);
        Add(this.tiles = GFX.FGAutotiler.GenerateBox(tile, width / 8, height / 8).TileGrid);
        Calc.PopRandom();

        Add(new Coroutine(Sequence()));
        Add(new LightOcclude());
        Add(new TileInterceptor(this.tiles, highPriority: false));

        this.TileType = tile;
        this.SurfaceSoundIndex = SurfaceIndex.TileToIndex[tile];
    }

    public override void OnShake(Vector2 amount)
    {
        base.OnShake(amount);
        this.tiles.Position += amount;
    }

    private IEnumerator Sequence()
    {
        while (true)
        {
            for (int i = 2; i < this.Width; i += 4)
            {
                if (this.Scene.CollideCheck<Solid>(this.TopLeft + new Vector2(i, -2f)))
                    SceneAs<Level>().Particles.Emit(FallingBlock.P_FallDustA, 2, new Vector2(this.X + i, this.Y), Vector2.One * 4f, (float) Math.PI / 2f);
                SceneAs<Level>().Particles.Emit(FallingBlock.P_FallDustB, 2, new Vector2(this.X + i, this.Y), Vector2.One * 4f);
            }

            float speed = 0f;
            float maxSpeed = 160f;

            while (true)
            {
                Level level = SceneAs<Level>();

                speed = Calc.Approach(speed, maxSpeed, 500f * Engine.DeltaTime);
                if (MoveVCollideSolids(speed * Engine.DeltaTime, thruDashBlocks: true))
                    break;

                this.Safe = false;
                if (this.Top > level.Bounds.Bottom + 16 || (this.Top > level.Bounds.Bottom - 1 && CollideCheck<Solid>(this.Position + new Vector2(0f, 1f))))
                {
                    this.Collidable = this.Visible = false;
                    yield return 0.2f;

                    if (level.Session.MapData.CanTransitionTo(level, new Vector2(this.Center.X, this.Bottom + 12f)))
                    {
                        yield return 0.2f;
                        SceneAs<Level>().Shake();
                        Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                    }

                    RemoveSelf();
                    DestroyStaticMovers();
                    yield break;
                }

                yield return null;
            }

            this.Safe = true;

            if (this.Y != this.originalY)
            {
                ImpactSfx();
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
                SceneAs<Level>().DirectionalShake(Vector2.UnitY, 0.3f);
                StartShaking();
                LandParticles();

                foreach (BubbleWrapBlock e in this.Scene.Tracker.GetEntities<BubbleWrapBlock>())
                    if (CollideCheck(e, this.Position + Vector2.UnitY))
                        e.Break();

                foreach (ColorSwitch e in this.Scene.Tracker.GetEntities<ColorSwitch>())
                    if (CollideCheck(e, this.Position + Vector2.UnitY))
                        e.Switch(Vector2.UnitY);

                yield return 0.2f;
                StopShaking();
            }

            if (CollideCheck<SolidTiles>(this.Position + new Vector2(0f, 1f)))
                break;

            while (CollideCheck<Platform>(this.Position + new Vector2(0f, 1f)))
                yield return 0.1f;
        }

        this.Safe = true;
    }

    private void LandParticles()
    {
        for (int i = 2; i <= this.Width; i += 4)
        {
            if (this.Scene.CollideCheck<Solid>(this.BottomLeft + new Vector2(i, 3f)))
            {
                SceneAs<Level>().ParticlesFG.Emit(FallingBlock.P_FallDustA, 1, new Vector2(this.X + i, this.Bottom), Vector2.One * 4f, -(float) Math.PI / 2f);
                float direction = (!(i < this.Width / 2f)) ? 0f : ((float) Math.PI);
                SceneAs<Level>().ParticlesFG.Emit(FallingBlock.P_LandDust, 1, new Vector2(this.X + i, this.Bottom), Vector2.One * 4f, direction);
            }
        }
    }

    private void ImpactSfx()
    {
        Audio.Play(this.TileType switch
        {
            '3' => SFX.game_01_fallingblock_ice_impact,
            '9' => SFX.game_03_fallingblock_wood_impact,
            'g' => SFX.game_06_fallingblock_boss_impact,
            _ => SFX.game_gen_fallblock_impact,
        });
    }
}
