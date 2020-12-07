using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;

namespace Celeste.Mod.VortexHelper.Entities
{
    [CustomEntity("VortexHelper/BubbleWrapBlock")]
    [Tracked(false)]
	class BubbleWrapBlock : Solid
	{
		private enum States
        {
			Idle,
			Gone
        }

		private States state = States.Idle;
		private bool canDash;
		private float respawnTime;
		private float RespawnTimer;
		private string SpriteState;
		private SoundSource breakSfx;
		private MTexture[,] nineSlice;
		private LightOcclude occluder;

		public BubbleWrapBlock(EntityData data, Vector2 offset)
			: this(data.Position + offset, data.Width, data.Height, data.Bool("canDash"), data.Float("respawnTime"))
		{ }

		public BubbleWrapBlock(Vector2 position, int width, int height, bool canDash, float respawnTime)
			: base(position, width, height, safe: true)
        {
			SurfaceSoundIndex = 8;
			this.canDash = canDash;
			this.respawnTime = respawnTime;
			switch(state)
            {
				default:
				case States.Idle:
					SpriteState = "Block";
					break;
				case States.Gone:
					SpriteState = "Outline";
					break;
            }
			MTexture mTexture = GFX.Game["objects/VortexHelper/bubbleWrapBlock/bubble" + SpriteState];
			nineSlice = new MTexture[3, 3];
			for (int i = 0; i < 3; i++)
			{
				for (int j = 0; j < 3; j++)
				{
					nineSlice[i, j] = mTexture.GetSubtexture(new Rectangle(i * 8, j * 8, 8, 8));
				}
			}
			Add(occluder = new LightOcclude());
			Add(breakSfx = new SoundSource());
			OnDashCollide = Dashed;
		}
		public override void Render()
		{
			float num = base.Collider.Width / 8f - 1f;
			float num2 = base.Collider.Height / 8f - 1f;
			for (int i = 0; (float)i <= num; i++)
			{
				for (int j = 0; (float)j <= num2; j++)
				{
					int num3 = ((float)i < num) ? Math.Min(i, 1) : 2;
					int num4 = ((float)j < num2) ? Math.Min(j, 1) : 2;
					nineSlice[num3, num4].Draw(Position + base.Shake + new Vector2(i * 8, j * 8));
				}
			}
			base.Render();
		}

		private DashCollisionResults Dashed(Player player, Vector2 direction)
		{
			if (!SaveData.Instance.Assists.Invincible && player.CollideCheck<Spikes>())
				return DashCollisionResults.NormalCollision;

			if (!canDash)
				return DashCollisionResults.NormalCollision;

			if (state == States.Gone)
				return DashCollisionResults.Ignore;

			if (player.StateMachine.State == 5) player.StateMachine.State = 0;

			Break();

			return DashCollisionResults.Rebound;
		}

		private void Break()
        {
			breakSfx.Play("event:/game/general/wall_break_stone");
			for (int i = 0; (float)i < base.Width / 8f; i++)
			{
				for (int j = 0; (float)j < base.Height / 8f; j++)
				{
					Debris debris = new Debris().orig_Init(Position + new Vector2(4 + i * 8, 4 + j * 8), '1').BlastFrom(base.Center);
					DynData<Debris> debrisData = new DynData<Debris>(debris);
					debrisData.Get<Image>("image").Texture = GFX.Game["debris/VortexHelper/BubbleWrapBlock"];
					base.Scene.Add(debris);
				}
			}
			DisableStaticMovers();
			state = States.Gone;
			Collidable = false;
			RespawnTimer = respawnTime;
		}

		public override void Update()
		{
			base.Update();
			if (RespawnTimer > 0f)
			{
				RespawnTimer -= Engine.DeltaTime;
			}
			if (RespawnTimer <= 0f)
			{
				if (CheckEntitySafe())
					Respawn();
			}
		}

		private void Respawn()
        {
			if (!Collidable)
			{
				EnableStaticMovers();
				breakSfx.Play("event:/game/05_mirror_temple/redbooster_reappear");
				Collidable = true;
				state = States.Idle;
			}
        }

		private bool CheckEntitySafe()
        {
            Player player = Scene.Tracker.GetEntity<Player>();
            if (player == null) return true;

            if (!CollideCheck(player))
                return true; // Only checks for player right now, since this is copied from switch blocks;
			// should also check for theo crystal, jelly, moveblocks, seekers, kevin, falling blocks, auto falling blocks, pufferbowls
			return false;
        }
    }
}
