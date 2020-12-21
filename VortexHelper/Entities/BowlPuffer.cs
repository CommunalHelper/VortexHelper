using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;

namespace Celeste.Mod.VortexHelper.Entities
{
    [CustomEntity("VortexHelper/BowlPuffer")]
    class BowlPuffer : Actor
    {
		private enum States
		{
			Idle,
			Hit,
			Gone,
			Crystal,
			Shattering
		}
		

		[Tracked(false)]
		public class BowlPufferCollider : Component
		{
			public Action<BowlPuffer, Spring> OnCollide;
			public Collider Collider;
			public BowlPufferCollider(Action<BowlPuffer, Spring> onCollide, Collider collider = null)
				: base(active: false, visible: false)
			{
				OnCollide = onCollide;
				Collider = collider;
			}

			public void Check(BowlPuffer puffer)
			{
				if (OnCollide != null)
				{
					Collider collider = base.Entity.Collider;
					if (Collider != null)
					{
						base.Entity.Collider = Collider;
					}
					if (puffer.CollideCheck(base.Entity))
					{
						OnCollide(puffer, (Spring)base.Entity);
					}
					base.Entity.Collider = collider;
				}
			}
		}


		private Facings facing = Facings.Right;

		private static ParticleType P_Shatter, P_Crystal;

		private States state = States.Crystal;

		private bool noRespawn;
        private Sprite pufferSprite;
		private Vector2 anchorPosition;
		private SineWave idleSine;

		private Vector2 startPosition;
		private SimpleCurve returnCurve;
		private float goneTimer;
		private float eyeSpin;

		private Vector2 hitSpeed;
		private Vector2 lastSpeedPosition;
		private Vector2 lastSinePosition;

		private Vector2 previousPosition;
		private Vector2 Speed;
		private Vector2 prevLiftSpeed;

		private float noGravityTimer;
		private float cantExplodeTimer;
		private float alertTimer;
		private float cannotHitTimer;
		private float shatterTimer;

		private float playerAliveFade;
		private Vector2 lastPlayerPos;

		private Wiggler inflateWiggler;
		private Wiggler bounceWiggler;
		private Vector2 scale;

		private Circle pushRadius, detectRadius;

		private Level Level;
		private Holdable Hold;
		private Collision onCollideH;
		private Collision onCollideV;
		private HoldableCollider hitSeeker;

		private float hardVerticalHitSoundCooldown;
		private float swatTimer;

		// TODO: Custom sound.

		/*
		 * [ ]TODO: Custom Entity interaction:
			* [x] dash blocks, 
			* [x] falling blocks,  
			* [x] touch switches, 
			* [x] move blocks, 
			* [x] maybe seeker blocks (or we could just make a palette swap of the seeker block that only the puffer bowl can destroy), 
			* [x] kevin (which will be a lot harder because of determining direction), 
			* [ ] core mode switches, 
			* [x] breaker boxes, 
			* [x] color switches,
			* [ ] other pufferfish bowls!!!!!!!!!, 
			* [ ] square bumpers (later).
			*
		 */


		public BowlPuffer(EntityData data, Vector2 offset)
            : this(data.Position + offset + (Vector2.UnitY * 8f), data.Bool("noRespawn"))
        { }

        public BowlPuffer(Vector2 position, bool noRespawn)
            : base(position)
        {
            this.noRespawn = noRespawn;
			previousPosition = position;
			startPosition = position;
			base.Depth = 100;
			base.Collider = new Hitbox(8f, 10f, -4f, -10f);
			Add(new PlayerCollider(OnPlayer, new Hitbox(14f, 12f, -6f, -17f)));
			Add(pufferSprite = VortexHelperModule.PufferBowlSpriteBank.Create("bowlPufferFish"));
			pufferSprite.Y -= 10f;

			pushRadius = new Circle(40f);
			detectRadius = new Circle(32f);

			inflateWiggler = Wiggler.Create(0.6f, 2f);
			Add(inflateWiggler);

			Add(Hold = new Holdable(0.1f));
			Hold.PickupCollider = new Hitbox(21f, 17f, -11f, -17f);
			Hold.SlowFall = false;
			Hold.SlowRun = true;
			Hold.OnPickup = OnPickup;
			Hold.OnRelease = OnRelease;
			Hold.DangerousCheck = Dangerous;
			Hold.OnHitSeeker = HitSeeker;
			Hold.OnSwat = Swat;
			Hold.OnHitSpring = HitSpring;
			Hold.OnHitSpinner = HitSpinner;
			Hold.SpeedGetter = () => Speed;
			onCollideH = OnCollideH;
			onCollideV = OnCollideV;

			idleSine = new SineWave(0.5f, 0f);
			idleSine.Randomize();
			Add(idleSine);

			Add(bounceWiggler = Wiggler.Create(0.6f, 2.5f, delegate (float v)
			{
				pufferSprite.Rotation = v * 20f * ((float)Math.PI / 180f);
			}));

			scale = Vector2.One;

			LiftSpeedGraceTime = 0.1f;
			base.Tag = Tags.TransitionUpdate;
			Add(new MirrorReflection());
		}

        private void OnPlayer(Player player)
        {
			if (state == States.Gone || state == States.Crystal || !(cantExplodeTimer <= 0f))
			{
				return;
			}
			if (cannotHitTimer <= 0f)
			{
				if (player.Bottom > lastSpeedPosition.Y + 3f)
				{
					Explode();
					GotoGone();
				}
				else
				{
					player.Bounce(base.Top);
					GotoHit();
					MoveToX(anchorPosition.X);
					idleSine.Reset();
					anchorPosition = (lastSinePosition = Position);
					eyeSpin = 1f;
				}
			}
			cannotHitTimer = 0.1f;
		}

        public override void Added(Scene scene)
		{
			base.Added(scene);
			Level = SceneAs<Level>();
		}

		private void OnCollideH(CollisionData data)
		{
			if (state == States.Crystal)
			{
				if (data.Hit is DashSwitch)
				{
					(data.Hit as DashSwitch).OnDashCollide(null, Vector2.UnitX * Math.Sign(Speed.X));
				}
				Audio.Play("event:/game/05_mirror_temple/crystaltheo_hit_side", Position);
				if (Math.Abs(Speed.X) > 100f)
				{
					ImpactParticles(data.Direction);
				}
				Speed.X *= -0.4f;
			}
		}

		private void OnCollideV(CollisionData data)
		{
			if (state == States.Crystal)
			{
				if (data.Hit is DashSwitch)
				{
					(data.Hit as DashSwitch).OnDashCollide(null, Vector2.UnitY * Math.Sign(Speed.Y));
				}
				if (Speed.Y > 0f)
				{
					if (hardVerticalHitSoundCooldown <= 0f)
					{
						Audio.Play("event:/game/05_mirror_temple/crystaltheo_hit_ground", Position, "crystal_velocity", Calc.ClampedMap(Speed.Y, 0f, 200f));
						hardVerticalHitSoundCooldown = 0.5f;
					}
					else
					{
						Audio.Play("event:/game/05_mirror_temple/crystaltheo_hit_ground", Position, "crystal_velocity", 0f);
					}
				}
				if (Speed.Y > 160f)
				{
					ImpactParticles(data.Direction);
				}
				if (Speed.Y > 140f && !(data.Hit is SwapBlock) && !(data.Hit is DashSwitch))
				{
					Speed.Y *= -0.6f;
				}
				else
				{
					Speed.Y = 0f;
				}
			}
		}

		private void ImpactParticles(Vector2 dir)
		{
			float direction;
			Vector2 position;
			Vector2 positionRange;
			if (dir.X > 0f)
			{
				direction = (float)Math.PI;
				position = new Vector2(base.Right, base.Y - 4f);
				positionRange = Vector2.UnitY * 6f;
			}
			else if (dir.X < 0f)
			{
				direction = 0f;
				position = new Vector2(base.Left, base.Y - 4f);
				positionRange = Vector2.UnitY * 6f;
			}
			else if (dir.Y > 0f)
			{
				direction = -(float)Math.PI / 2f;
				position = new Vector2(base.X, base.Bottom);
				positionRange = Vector2.UnitX * 6f;
			}
			else
			{
				direction = (float)Math.PI / 2f;
				position = new Vector2(base.X, base.Top);
				positionRange = Vector2.UnitX * 6f;
			}
			Level.Particles.Emit(TheoCrystal.P_Impact, 12, position, positionRange, direction);
		}

		public void Swat(HoldableCollider hc, int dir)
		{
			if (Hold.IsHeld && hitSeeker == null)
			{
				swatTimer = 0.1f;
				hitSeeker = hc;
				Hold.Holder.Swat(dir);
			}
		}

		public bool Dangerous(HoldableCollider holdableCollider)
		{
			if (!Hold.IsHeld && Speed != Vector2.Zero)
			{
				return hitSeeker != holdableCollider;
			}
			return false;
		}

		protected override void OnSquish(CollisionData data)
		{
			if (!TrySquishWiggle(data) && state != States.Gone)
			{
				Explode();
				GotoGone();
			}
		}

		public override bool IsRiding(Solid solid)
		{
			if (Speed.Y == 0f)
			{
				return base.IsRiding(solid);
			}
			return false;
		}

		public void HitSeeker(Seeker seeker)
		{
			if (!Hold.IsHeld)
			{
				Speed = (base.Center - seeker.Center).SafeNormalize(120f);
			}
			Audio.Play("event:/game/05_mirror_temple/crystaltheo_hit_side", Position);
		}
		public void HitSpinner(Entity spinner)
		{
			if (!Hold.IsHeld && Speed.Length() < 0.01f && base.LiftSpeed.Length() < 0.01f && (previousPosition - base.ExactPosition).Length() < 0.01f && OnGround())
			{
				int num = Math.Sign(base.X - spinner.X);
				if (num == 0)
				{
					num = 1;
				}
				Speed.X = (float)num * 120f;
				Speed.Y = -30f;
			}
		}
		public bool HitSpring(Spring spring)
		{
			if (state == States.Hit || state == States.Idle)
			{
				switch (spring.Orientation)
				{
					default:
						if (hitSpeed.Y >= 0f)
						{
							GotoHitSpeed(224f * -Vector2.UnitY);
							MoveTowardsX(spring.CenterX, 4f);
							bounceWiggler.Start();
							Alert(restart: true, playSfx: false);
							return true;
						}
						return false;
					case Spring.Orientations.WallLeft:
						if (hitSpeed.X <= 60f)
						{
							facing = Facings.Right;
							GotoHitSpeed(280f * Vector2.UnitX);
							MoveTowardsY(spring.CenterY, 4f);
							bounceWiggler.Start();
							Alert(restart: true, playSfx: false);
							return true;
						}
						return false;
					case Spring.Orientations.WallRight:
						if (hitSpeed.X >= -60f)
						{
							facing = Facings.Left;
							GotoHitSpeed(280f * -Vector2.UnitX);
							MoveTowardsY(spring.CenterY, 4f);
							bounceWiggler.Start();
							Alert(restart: true, playSfx: false);
							return true;
						}
						return false;
				}
			}
			else if (!Hold.IsHeld)
			{
				if (spring.Orientation == Spring.Orientations.Floor && Speed.Y >= 0f)
				{
					Speed.X *= 0.5f;
					Speed.Y = -160f;
					noGravityTimer = 0.15f;
					return true;
				}
				if (spring.Orientation == Spring.Orientations.WallLeft && Speed.X <= 0f)
				{
					MoveTowardsY(spring.CenterY + 5f, 4f);
					Speed.X = 220f;
					Speed.Y = -80f;
					noGravityTimer = 0.1f;
					return true;
				}
				if (spring.Orientation == Spring.Orientations.WallRight && Speed.X >= 0f)
				{
					MoveTowardsY(spring.CenterY + 5f, 4f);
					Speed.X = -220f;
					Speed.Y = -80f;
					noGravityTimer = 0.1f;
					return true;
				}
			}
			return false;
		}

		private void OnPickup()
		{
			Speed = Vector2.Zero;
			AddTag(Tags.Persistent);
		}

		private void GotoHit()
		{
			scale = new Vector2(1.2f, 0.8f);
			hitSpeed = Vector2.UnitY * 200f;
			state = States.Hit;
			bounceWiggler.Start();
			Alert(restart: true, playSfx: false);
			Audio.Play("event:/new_content/game/10_farewell/puffer_boop", Position);
		}

		private void GotoHitSpeed(Vector2 speed)
		{
			hitSpeed = speed;
			state = States.Hit;
		}

		private void OnRelease(Vector2 force)
		{
			RemoveTag(Tags.Persistent);
			if (force.X != 0f && force.Y == 0f)
			{
				force.Y = -0.4f;
			}
			Speed = force * 200f;
			if (Speed != Vector2.Zero)
			{
				noGravityTimer = 0.1f;
			}
		}

		private void Explode()
		{
			base.Collider = pushRadius;
			Audio.Play("event:/new_content/game/10_farewell/puffer_splode", Position);
			pufferSprite.Play("explode");
			if (state == States.Crystal) ShatterBowl();

			// Yeah, there's a lot going in there.
			DoEntityCustomInteraction();

			base.Collider = null;
			Level level = SceneAs<Level>();
			level.Shake();
			level.Displacement.AddBurst(Position, 0.4f, 12f, 36f, 0.5f);
			level.Displacement.AddBurst(Position, 0.4f, 24f, 48f, 0.5f);
			level.Displacement.AddBurst(Position, 0.4f, 36f, 60f, 0.5f);
			for (float num = 0f; num < (float)Math.PI * 2f; num += 0.17453292f)
			{
				Vector2 position = base.Center + Calc.AngleToVector(num + Calc.Random.Range(-(float)Math.PI / 90f, (float)Math.PI / 90f), Calc.Random.Range(12, 18));
				level.Particles.Emit(Seeker.P_Regen, position, num);
			}
		}

		private void DoEntityCustomInteraction()
		{
			// Player
			Player player = CollideFirst<Player>();
			if (player != null && !base.Scene.CollideCheck<Solid>(Position, player.Center))
			{
				player.ExplodeLaunch(Position, snapUp: false, sidesOnly: true);
			}
			player = SceneAs<Level>().Tracker.GetEntity<Player>();

			// Theo Crystal
			TheoCrystal theoCrystal = CollideFirst<TheoCrystal>();
			if (theoCrystal != null && !base.Scene.CollideCheck<Solid>(Position, theoCrystal.Center))
			{
				theoCrystal.ExplodeLaunch(Position);
			}

			// Touch Switches
			foreach (TouchSwitch entity2 in base.Scene.Tracker.GetEntities<TouchSwitch>())
			{
				if (CollideCheck(entity2))
				{
					entity2.TurnOn();
				}
			}

			// Floating Debris
			foreach (FloatingDebris entity3 in base.Scene.Tracker.GetEntities<FloatingDebris>())
			{
				if (CollideCheck(entity3))
				{
					entity3.OnExplode(Position);
				}
			}

			foreach(Actor e in CollideAll<Actor>())
            {
				if(e is Puffer)
				{
					VortexHelperModule.Puffer_Explode.Invoke(e, new object[] { });
					VortexHelperModule.Puffer_GotoGone.Invoke(e, new object[] { });
				}
            }

			foreach (Solid e in CollideAll<Solid>())
			{
				// Temple Cracked Blocks
				if (e is TempleCrackedBlock)
				{
					(e as TempleCrackedBlock).Break(Position); continue;
				}

				// Color Switches
				if (e is ColorSwitch)
				{
					(e as ColorSwitch).Switch(Calc.FourWayNormal(e.Center - Center)); continue;
				}

				// Dash Blocks
				if (e is DashBlock)
				{
					(e as DashBlock).Break(Center, Calc.FourWayNormal(e.Center - Center), true, true); continue;
				}

				// Falling Blocks
				if (e is FallingBlock)
				{
					(e as FallingBlock).Triggered = true; continue;
				}

				// Move Blocks
				if (e is MoveBlock)
				{
					(e as MoveBlock).OnStaticMoverTrigger(null); continue;
				}

				// Kevins
				if (e is CrushBlock)
				{
					// e.OnDashed(Player player = null, Vector2 direction = Calc.FourWayNormal(e.Center - Center));
					VortexHelperModule.CrushBlock_OnDashed.Invoke(e as CrushBlock, new object[] { null, Calc.FourWayNormal(e.Center - Center) });
					continue;
				}

				// Lightning Breaker Boxes
				if(e is LightningBreakerBox)
                {
					if (player != null) 
					{
						VortexHelperModule.AllowPlayerDashRefills = false;
						(e as LightningBreakerBox).OnDashCollide(player, Calc.FourWayNormal(e.Center - Center));
						VortexHelperModule.AllowPlayerDashRefills = true;
					}
					continue;
                }
			}
		}

		private void GotoGone()
		{
			Vector2 control = Position + (startPosition - Position) * 0.5f;
			if ((startPosition - Position).LengthSquared() > 100f)
			{
				if (Math.Abs(Position.Y - startPosition.Y) > Math.Abs(Position.X - startPosition.X))
				{
					if (Position.X > startPosition.X)
					{
						control += Vector2.UnitX * -24f;
					}
					else
					{
						control += Vector2.UnitX * 24f;
					}
				}
				else if (Position.Y > startPosition.Y)
				{
					control += Vector2.UnitY * -24f;
				}
				else
				{
					control += Vector2.UnitY * 24f;
				}
			}
			returnCurve = new SimpleCurve(Position, startPosition, control);
			Collidable = false;
			goneTimer = 2.5f;
			if(state == States.Idle || state == States.Hit) Collider = new Hitbox(8f, 10f, -4f, -10f);
			state = States.Gone;
			base.Collider = null;
		}
		private void GotoCrystal()
		{
			Add(Hold);
			base.Collider = new Hitbox(8f, 10f, -4f, -10f);
			facing = Facings.Right;
			if (state == States.Gone)
			{
				Position = startPosition;

				pufferSprite.Play("recover");
				Audio.Play("event:/new_content/game/10_farewell/puffer_reform", Position);
			}
			Speed = Vector2.Zero;
			noGravityTimer = 0.25f;
			state = States.Crystal;
		}
		private void GotoShatter(Player player)
        {
			PlayerThrowSelf(player);
			Remove(Hold);
			state = States.Shattering;
			shatterTimer = 1f;
		}

		private void GotoIdle(bool shatter)
        {
			state = States.Idle;
			Speed = Vector2.Zero;
			if (shatter)
			{
				pufferSprite.Play("idleNoBowl", true); 
				ShatterBowl();
			}
			cantExplodeTimer = 0.5f;
			Collider = new Hitbox(12f, 10f, -5f, -15f);
			lastSinePosition = (lastSpeedPosition = (anchorPosition = Position));
			hitSpeed = Vector2.Zero;
			idleSine.Reset();
		}

		private bool CollidePufferBarrierCheck()
		{
			bool res = false;
			foreach (PufferBarrier barrier in Scene.Tracker.GetEntities<PufferBarrier>())
			{
				barrier.Collidable = true;
			}

			PufferBarrier collided = CollideFirst<PufferBarrier>();
			if(collided != null)
            {
				collided.OnTouchPuffer();
				res = true;
            }

			foreach (PufferBarrier barrier in Scene.Tracker.GetEntities<PufferBarrier>())
			{
				barrier.Collidable = false;
			}

			return res;
		}

		private void CollideWithSprings()
        {
			foreach (BowlPufferCollider c in Scene.Tracker.GetComponents<BowlPufferCollider>())
			{
				c.Check(this);
			}
        }

		private void PlayerThrowSelf(Player player)
        {
			if (player != null)
			{
				player.Throw();
			}
		}

		private void ShatterBowl()
        {
			Input.Rumble(RumbleStrength.Strong, RumbleLength.Short);
			Level level = SceneAs<Level>();
			level.Shake(0.175f);
			for (int i = 0; i < 10; i++)
			{
				level.ParticlesFG.Emit(P_Shatter, 1, Center, Vector2.One * 7f, Calc.Random.NextFloat() * (float)Math.PI * 2);
			}
			for (float t = 0f; t < (float)Math.PI * 2f; t += 0.17453292f)
			{
				level.Particles.Emit(P_Crystal, base.Center + (Vector2.UnitY * -6), t);
			}
		}

		public override void Update()
		{
			base.Update();
			
			eyeSpin = Calc.Approach(eyeSpin, 0f, Engine.DeltaTime * 1.5f);
			scale = Calc.Approach(scale, Vector2.One, 1f * Engine.DeltaTime);

			if (state != States.Gone && cantExplodeTimer > 0f)
			{
				cantExplodeTimer -= Engine.DeltaTime;
			}
			if (cannotHitTimer > 0f)
			{
				cannotHitTimer -= Engine.DeltaTime;
			}
			if (alertTimer > 0f)
			{
				alertTimer -= Engine.DeltaTime;
			}
			if (shatterTimer > 0f)
			{
				shatterTimer -= 1.5f * Engine.DeltaTime;
			}
			Player entity = base.Scene.Tracker.GetEntity<Player>();
			if (entity == null)
			{
				playerAliveFade = Calc.Approach(playerAliveFade, 0f, 1f * Engine.DeltaTime);
			}
			else
			{
				playerAliveFade = Calc.Approach(playerAliveFade, 1f, 1f * Engine.DeltaTime);
				lastPlayerPos = entity.Center;
			}

			switch (state)
			{
				default:
					break;

				case States.Crystal:
					if (swatTimer > 0f)
					{
						swatTimer -= Engine.DeltaTime;
					}
					hardVerticalHitSoundCooldown -= Engine.DeltaTime;
					base.Depth = 100;
					if (CollidePufferBarrierCheck())
					{
						Explode();
						GotoGone();
						PlayerThrowSelf(entity);
						return;
					} 
					else if(CollideCheck<Water>())
					{
						GotoShatter(entity);
						return;
					}

					if (Hold.IsHeld)
					{
						prevLiftSpeed = Vector2.Zero;
					}
					else
					{
						if (OnGround())
						{
							float target = (!OnGround(Position + Vector2.UnitX * 3f)) ? 20f : (OnGround(Position - Vector2.UnitX * 3f) ? 0f : (-20f));
							Speed.X = Calc.Approach(Speed.X, target, 800f * Engine.DeltaTime);
							Vector2 liftSpeed = base.LiftSpeed;
							if (liftSpeed == Vector2.Zero && prevLiftSpeed != Vector2.Zero)
							{
								Speed = prevLiftSpeed;
								prevLiftSpeed = Vector2.Zero;
								Speed.Y = Math.Min(Speed.Y * 0.6f, 0f);
								if (Speed.X != 0f && Speed.Y == 0f)
								{
									Speed.Y = -60f;
								}
								if (Speed.Y < 0f)
								{
									noGravityTimer = 0.15f;
								}
							}
							else
							{
								prevLiftSpeed = liftSpeed;
								if (liftSpeed.Y < 0f && Speed.Y < 0f)
								{
									Speed.Y = 0f;
								}
							}
						}
						else if (Hold.ShouldHaveGravity)
						{
							float num1 = 800f;
							if (Math.Abs(Speed.Y) <= 30f)
							{
								num1 *= 0.5f;
							}
							float num2 = 350f;
							if (Speed.Y < 0f)
							{
								num2 *= 0.5f;
							}
							Speed.X = Calc.Approach(Speed.X, 0f, num2 * Engine.DeltaTime);
							if (noGravityTimer > 0f)
							{
								noGravityTimer -= Engine.DeltaTime;
							}
							else
							{
								Speed.Y = Calc.Approach(Speed.Y, 200f, num1 * Engine.DeltaTime);
							}
						}
						previousPosition = base.ExactPosition;
						MoveH(Speed.X * Engine.DeltaTime, onCollideH);
						MoveV(Speed.Y * Engine.DeltaTime, onCollideV);
						if (base.Center.X > (float)Level.Bounds.Right)
						{
							MoveH(32f * Engine.DeltaTime);
							if (base.Left - 8f > (float)Level.Bounds.Right)
							{
								RemoveSelf();
							}
						}
						else if (base.Left < (float)Level.Bounds.Left)
						{
							base.Left = Level.Bounds.Left;
							Speed.X *= -0.4f;
						}
						else if (base.Top < (float)(Level.Bounds.Top - 4))
						{
							base.Top = Level.Bounds.Top + 4;
							Speed.Y = 0f;
						}
						else if (base.Top > (float)Level.Bounds.Bottom)
						{
							MoveV(-5);
							Explode();
							GotoGone();
						}

						if (base.X < (float)(Level.Bounds.Left + 10))
						{
							MoveH(32f * Engine.DeltaTime);
						}
						TempleGate templeGate = CollideFirst<TempleGate>();
						if (templeGate != null && entity != null)
						{
							templeGate.Collidable = false;
							MoveH((float)(Math.Sign(entity.X - base.X) * 32) * Engine.DeltaTime);
							templeGate.Collidable = true;
						}
					}
					Hold.CheckAgainstColliders();
					if (hitSeeker != null && swatTimer <= 0f && !hitSeeker.Check(Hold))
					{
						hitSeeker = null;
					}
					break;

				case States.Hit:
					lastSpeedPosition = Position;
					MoveH(hitSpeed.X * Engine.DeltaTime, onCollideH);
					MoveV(hitSpeed.Y * Engine.DeltaTime, OnCollideV);
					anchorPosition = Position;
					hitSpeed.X = Calc.Approach(hitSpeed.X, 0f, 150f * Engine.DeltaTime);
					hitSpeed = Calc.Approach(hitSpeed, Vector2.Zero, 320f * Engine.DeltaTime);
					if (ProximityExplodeCheck())
					{
						Explode();
						GotoGone();
						break;
					}
					if (base.Top >= (float)(SceneAs<Level>().Bounds.Bottom + 5))
					{
						pufferSprite.Play("hidden");
						GotoGone();
						break;
					}
					CollideWithSprings();
					if (hitSpeed == Vector2.Zero)
					{
						ZeroRemainderX();
						ZeroRemainderY();
						GotoIdle(shatter: false);
					}
					break;

				case States.Gone:
					float num = goneTimer;
					goneTimer -= Engine.DeltaTime;
					if (goneTimer <= 0.5f)
					{
						if (num > 0.5f && returnCurve.GetLengthParametric(8) > 8f)
						{
							Audio.Play("event:/new_content/game/10_farewell/puffer_return", Position);
						}
						Position = returnCurve.GetPoint(Ease.CubeInOut(Calc.ClampedMap(goneTimer, 0.5f, 0f)));
					}
					if (goneTimer <= 2.1f && num > 2.1f && noRespawn)
					{
						RemoveSelf();
					}
					if (goneTimer <= 0f)
					{
						Visible = (Collidable = true);
						GotoCrystal();
					}
					break;

				case States.Idle:
						if (Position != lastSinePosition)
						{
							anchorPosition += Position - lastSinePosition;
						}
						Vector2 vector = anchorPosition + new Vector2(idleSine.Value * 3f, idleSine.ValueOverTwo * 2f);
						MoveToX(vector.X);
						MoveToY(vector.Y);
						lastSinePosition = Position;
						if (ProximityExplodeCheck())
						{
							Explode();
							GotoGone();
							break;
						}
						if (AlertedCheck())
						{
							Alert(restart: false, playSfx: true);
						}
						else if (pufferSprite.CurrentAnimationID == "alerted" && alertTimer <= 0f)
						{
							Audio.Play("event:/new_content/game/10_farewell/puffer_shrink", Position);
							pufferSprite.Play("unalert");
						}
						CollideWithSprings();
						break;

				case States.Shattering:
					Speed = Calc.Approach(Speed, Vector2.Zero, 575f * Engine.DeltaTime);
					MoveH(Speed.X * Engine.DeltaTime, onCollideH);
					MoveV(Speed.Y * Engine.DeltaTime, onCollideV);
					Level level = SceneAs<Level>();
					if (shatterTimer <= 0f)
                    {
						GotoIdle(shatter: true);
					}
					break;
			}
		}

		private bool ProximityExplodeCheck()
		{
			if (cantExplodeTimer > 0f)
			{
				return false;
			}
			bool result = false;
			Collider collider = base.Collider;
			base.Collider = detectRadius;
			Player player;
			if ((player = CollideFirst<Player>()) != null && player.CenterY >= base.Y + collider.Bottom - 4f && !base.Scene.CollideCheck<Solid>(Position, player.Center))
			{
				result = true;
			}
			base.Collider = collider;
			return result;
		}

		private bool AlertedCheck()
		{
			Player entity = base.Scene.Tracker.GetEntity<Player>();
			if (entity != null)
			{
				return (entity.Center - base.Center).Length() < 60f;
			}
			return false;
		}

		private void Alert(bool restart, bool playSfx)
		{
			if (pufferSprite.CurrentAnimationID == "idleNoBowl")
			{
				if (playSfx)
				{
					Audio.Play("event:/new_content/game/10_farewell/puffer_expand", Position);
				}
				pufferSprite.Play("alert");
				inflateWiggler.Start();
			}
			else if (restart && playSfx)
			{
				Audio.Play("event:/new_content/game/10_farewell/puffer_expand", Position);
			}
			alertTimer = 2f;
		}

		public override void Render()
		{
			if(state == States.Shattering)
            {
				float a = .7f * shatterTimer + .2f + (float)(Math.Sin(15f * shatterTimer) / 5f);
				pufferSprite.Color = Color.White * a;
			} else
            {
				pufferSprite.Color = Color.White;
            }
			pufferSprite.Scale = scale * (1f + inflateWiggler.Value * 0.4f);
			if (state == States.Idle || state == States.Hit)
			{
				pufferSprite.FlipX = facing != Facings.Right;
				pufferSprite.DrawSimpleOutline();
			} else
            {
				pufferSprite.FlipX = false;
			}

			Vector2 position = Position;
			Position.Y -= 6;
			float num = playerAliveFade * Calc.ClampedMap((Position - lastPlayerPos).Length(), 128f, 96f);
			if (num > 0f && state == States.Idle || state == States.Hit)
			{
				bool flag2 = false;
				Vector2 value = lastPlayerPos;
				if (value.Y < base.Y)
				{
					value.Y = base.Y - (value.Y - base.Y) * 0.5f;
					value.X += value.X - base.X;
					flag2 = true;
				}
				float radiansB = (value - Position).Angle();
				for (int i = 0; i < 28; i++)
				{
					float num2 = (float)Math.Sin(base.Scene.TimeActive * 0.5f) * 0.02f;
					float num3 = Calc.Map((float)i / 28f + num2, 0f, 1f, -(float)Math.PI / 30f, 3.24631262f);
					num3 += bounceWiggler.Value * 20f * ((float)Math.PI / 180f);
					Vector2 value2 = Calc.AngleToVector(num3, 1f);
					Vector2 vector = Position + value2 * 32f;
					float t = Calc.ClampedMap(Calc.AbsAngleDiff(num3, radiansB), (float)Math.PI / 2f, 0.17453292f);
					t = Ease.CubeOut(t) * 0.8f * num;
					if (!(t > 0f))
					{
						continue;
					}
					if (i == 0 || i == 27)
					{
						Draw.Line(vector, vector - value2 * 10f, Color.White * t);
						continue;
					}
					Vector2 vector2 = value2 * (float)Math.Sin(base.Scene.TimeActive * 2f + (float)i * 0.6f);
					if (i % 2 == 0)
					{
						vector2 *= -1f;
					}
					vector += vector2;
					if (!flag2 && Calc.AbsAngleDiff(num3, radiansB) <= 0.17453292f)
					{
						Draw.Line(vector, vector - value2 * 3f, Color.White * t);
					}
					else
					{
						Draw.Point(vector, Color.White * t);
					}
				}
			}
			Position = position;

			base.Render();
			if (pufferSprite.CurrentAnimationID == "alerted")
			{
				Vector2 vector3 = Position + (new Vector2(3f, -4) * pufferSprite.Scale);
				vector3.X -= facing == Facings.Left ? 9 : 0;
				Vector2 to = lastPlayerPos + new Vector2(0f, -4f);
				Vector2 vector4 = Calc.AngleToVector(Calc.Angle(vector3, to) + eyeSpin * ((float)Math.PI * 2f) * 2f, 1f);
				Vector2 vector5 = vector3 + new Vector2((float)Math.Round(vector4.X), (float)Math.Round(Calc.ClampedMap(vector4.Y, -1f, 1f, -1f, 2f)));
				vector5.Y -= 10;
				Draw.Point(vector5, Color.Black);
			}
		}

        public static void InitializeParticles()
        {
			P_Shatter = new ParticleType(Refill.P_Shatter)
			{
				Color = Color.White,
				Color2 = Color.LightBlue,
				ColorMode = ParticleType.ColorModes.Blink,
				SpeedMax = 75f,
				SpeedMin = 0f,
            };
			P_Crystal = new ParticleType(Seeker.P_Regen)
			{
				SpeedMax = 40f,
				Acceleration = Vector2.UnitY * 30
			};
        }
	}
}
