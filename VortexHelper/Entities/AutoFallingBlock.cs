using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.VortexHelper.Entities
{
    [CustomEntity("VortexHelper/AutoFallingBlock")]
    class AutoFallingBlock : Solid
    {
		private TileGrid tiles;
		private char TileType;
		private int originalY;

		public AutoFallingBlock(EntityData data, Vector2 offset)
        : this(data.Position + offset, data.Char("tiletype", '3'), data.Width, data.Height)
        { }

        public AutoFallingBlock(Vector2 position, char tile, int width, int height)
            : base(position, width, height, safe: false)
        {
			originalY = (int)position.Y;
			int newSeed = Calc.Random.Next();
			Calc.PushRandom(newSeed);
			Add(tiles = GFX.FGAutotiler.GenerateBox(tile, width / 8, height / 8).TileGrid);
			Calc.PopRandom();
			Add(new Coroutine(Sequence()));
			Add(new LightOcclude());
			Add(new TileInterceptor(tiles, highPriority: false));
			TileType = tile;
			SurfaceSoundIndex = SurfaceIndex.TileToIndex[tile];
		}

		public override void OnShake(Vector2 amount)
		{
			base.OnShake(amount);
			tiles.Position += amount;
		}

		private IEnumerator Sequence()
		{
			while (true)
			{
				for (int i = 2; (float)i < Width; i += 4)
				{
					if (Scene.CollideCheck<Solid>(TopLeft + new Vector2(i, -2f)))
					{
						SceneAs<Level>().Particles.Emit(FallingBlock.P_FallDustA, 2, new Vector2(X + (float)i, Y), Vector2.One * 4f, (float)Math.PI / 2f);
					}
					SceneAs<Level>().Particles.Emit(FallingBlock.P_FallDustB, 2, new Vector2(X + (float)i, Y), Vector2.One * 4f);
				}
				float speed = 0f;
				float maxSpeed = 160f;
				while (true)
				{
					Level level = SceneAs<Level>();
					speed = Calc.Approach(speed, maxSpeed, 500f * Engine.DeltaTime);
					if (MoveVCollideSolids(speed * Engine.DeltaTime, thruDashBlocks: true))
					{
						break;
					}
					Safe = false;
					if (Top > (float)(level.Bounds.Bottom + 16) || (Top > (float)(level.Bounds.Bottom - 1) && CollideCheck<Solid>(Position + new Vector2(0f, 1f))))
					{
						Collidable = (Visible = false);
						yield return 0.2f;
						if (level.Session.MapData.CanTransitionTo(level, new Vector2(Center.X, Bottom + 12f)))
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
				Safe = true;
				if (Y != originalY)
				{
					ImpactSfx();
					Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
					SceneAs<Level>().DirectionalShake(Vector2.UnitY, 0.3f);
					StartShaking();
					LandParticles();
					yield return 0.2f;
					StopShaking();
				}
				if (CollideCheck<SolidTiles>(Position + new Vector2(0f, 1f)))
				{
					break;
				}
				while (CollideCheck<Platform>(Position + new Vector2(0f, 1f)))
				{
					yield return 0.1f;
				}
			}
			Safe = true;
		}

		private void LandParticles()
		{
			for (int i = 2; (float)i <= base.Width; i += 4)
			{
				if (base.Scene.CollideCheck<Solid>(base.BottomLeft + new Vector2(i, 3f)))
				{
					SceneAs<Level>().ParticlesFG.Emit(FallingBlock.P_FallDustA, 1, new Vector2(base.X + (float)i, base.Bottom), Vector2.One * 4f, -(float)Math.PI / 2f);
					float direction = (!((float)i < base.Width / 2f)) ? 0f : ((float)Math.PI);
					SceneAs<Level>().ParticlesFG.Emit(FallingBlock.P_LandDust, 1, new Vector2(base.X + (float)i, base.Bottom), Vector2.One * 4f, direction);
				}
			}
		}

		private void ImpactSfx()
		{
			if (TileType == '3')
			{
				Audio.Play("event:/game/01_forsaken_city/fallblock_ice_impact", base.BottomCenter);
			}
			else if (TileType == '9')
			{
				Audio.Play("event:/game/03_resort/fallblock_wood_impact", base.BottomCenter);
			}
			else if (TileType == 'g')
			{
				Audio.Play("event:/game/06_reflection/fallblock_boss_impact", base.BottomCenter);
			}
			else
			{
				Audio.Play("event:/game/general/fallblock_impact", base.BottomCenter);
			}
		}
	}
}
