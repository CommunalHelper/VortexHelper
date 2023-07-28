using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace Celeste.Mod.VortexHelper.Entities;

[CustomEntity("VortexHelper/BowlPuffer")]
public class BowlPuffer : Actor
{
    [Tracked(false)]
    public class BowlPufferCollider : Component
    {
        public Action<BowlPuffer, Spring> OnCollide;
        public Collider Collider;

        public BowlPufferCollider(Action<BowlPuffer, Spring> onCollide, Collider collider = null)
            : base(active: false, visible: false)
        {
            this.OnCollide = onCollide;
            this.Collider = collider;
        }

        public void Check(BowlPuffer puffer)
        {
            if (this.OnCollide is not null)
            {
                Collider collider = this.Entity.Collider;
                if (this.Collider is not null)
                    this.Entity.Collider = this.Collider;

                if (puffer.CollideCheck(this.Entity))
                    this.OnCollide(puffer, (Spring) this.Entity);

                this.Entity.Collider = collider;
            }
        }
    }

    private static ParticleType P_Shatter, P_Crystal;

    private enum States
    {
        Gone,
        Crystal
    }
    private States state = States.Crystal;
    private Facings facing = Facings.Right;

    private readonly bool noRespawn;

    // Uses three sprites so that we can play the vanilla puffer animations without extra sprite work.
    private readonly Sprite pufferBowlBottom, puffer, pufferBowlTop;

    private readonly Vector2 startPosition;

    private SimpleCurve returnCurve;
    private float goneTimer;
    private float eyeSpin;

    private Vector2 previousPosition;
    private Vector2 Speed;
    private Vector2 prevLiftSpeed;

    private float noGravityTimer;
    private float cantExplodeTimer;
    private float shatterTimer;

    private readonly float explodeTime;
    private float explodeTimeLeft;
    private bool fused = false;

    private Vector2 lastPlayerPos;

    private readonly Wiggler inflateWiggler;
    private Vector2 scale;

    private bool exploded = false;

    private readonly Circle pushRadius;

    private Level Level;

    private readonly Holdable Hold;
    private readonly Collision onCollideH, onCollideV;
    private HoldableCollider hitSeeker;

    private float hardVerticalHitSoundCooldown;
    private float swatTimer;

    private const float ChainExplosionDelay = 0.1f;
    private float chainTimer = ChainExplosionDelay;
    private bool chainExplode = false;

    // TODO: Custom sound.

    public BowlPuffer(EntityData data, Vector2 offset)
        : this(data.Position + offset + Vector2.UnitY * 8f, data.Bool("noRespawn"), data.Float("explodeTimer", 0f)) { }

    public BowlPuffer(Vector2 position, bool noRespawn, float explodeTimer)
        : base(position)
    {
        this.noRespawn = noRespawn;
        this.explodeTime = explodeTimer;
        this.previousPosition = position;
        this.startPosition = position;
        this.Depth = Depths.TheoCrystal;
        this.Collider = new Hitbox(8f, 10f, -4f, -10f);

        Add(this.pufferBowlBottom = VortexHelperModule.PufferBowlSpriteBank.Create("pufferBowlBottom"));
        Add(this.puffer = GFX.SpriteBank.Create("pufferFish"));
        Add(this.pufferBowlTop = VortexHelperModule.PufferBowlSpriteBank.Create("pufferBowlTop"));
        this.puffer.Y -= 9f;

        // Weird offset needed
        var bowlOffset = new Vector2(-32, -43);
        this.pufferBowlTop.Position = this.pufferBowlBottom.Position += bowlOffset;

        this.pushRadius = new Circle(40f);

        this.inflateWiggler = Wiggler.Create(0.6f, 2f);
        Add(this.inflateWiggler);

        Add(this.Hold = new Holdable(0.1f)
        {
            PickupCollider = new Hitbox(21f, 17f, -11f, -17f),
            SlowFall = false,
            SlowRun = true,
            OnPickup = OnPickup,
            OnRelease = OnRelease,
            DangerousCheck = Dangerous,
            OnHitSeeker = HitSeeker,
            OnSwat = Swat,
            OnHitSpring = HitSpring,
            OnHitSpinner = HitSpinner,
            SpeedGetter = () => this.Speed,
        });

        this.onCollideH = OnCollideH;
        this.onCollideV = OnCollideV;

        this.scale = Vector2.One;

        this.LiftSpeedGraceTime = 0.1f;
        this.Tag = Tags.TransitionUpdate;
        Add(new MirrorReflection());
    }

    private void AllSpritesPlay(string anim)
    {
        this.pufferBowlBottom.Play(anim);
        this.puffer.Play(anim);
        this.pufferBowlTop.Play(anim);
    }

    public override void Added(Scene scene)
    {
        base.Added(scene);
        this.Level = SceneAs<Level>();
    }

    private void OnCollideH(CollisionData data)
    {
        if (this.state is not States.Crystal)
            return;

        if (data.Hit is DashSwitch dashSwitch)
            dashSwitch.OnDashCollide(null, Vector2.UnitX * Math.Sign(this.Speed.X));

        Audio.Play(SFX.game_05_crystaltheo_impact_side, this.Position);

        if (Math.Abs(this.Speed.X) > 100f)
            ImpactParticles(data.Direction);

        this.Speed.X *= -0.4f;
    }

    private void OnCollideV(CollisionData data)
    {
        if (this.state is not States.Crystal)
            return;

        if (data.Hit is DashSwitch dashSwitch)
            dashSwitch.OnDashCollide(null, Vector2.UnitY * Math.Sign(this.Speed.Y));

        if (this.Speed.Y > 0f)
        {
            if (this.hardVerticalHitSoundCooldown <= 0f)
            {
                Audio.Play(SFX.game_05_crystaltheo_impact_ground, this.Position, "crystal_velocity", Calc.ClampedMap(this.Speed.Y, 0f, 200f));
                this.hardVerticalHitSoundCooldown = 0.5f;
            }
            else
                Audio.Play(SFX.game_05_crystaltheo_impact_ground, this.Position, "crystal_velocity", 0f);
        }

        if (this.Speed.Y > 160f)
            ImpactParticles(data.Direction);

        if (this.Speed.Y > 140f && data.Hit is not SwapBlock or DashSwitch)
            this.Speed.Y *= -0.6f;
        else
            this.Speed.Y = 0f;
    }

    private void ImpactParticles(Vector2 dir)
    {
        float direction;
        Vector2 position, positionRange;

        if (dir.X > 0f)
        {
            direction = (float) Math.PI;
            position = new Vector2(this.Right, this.Y - 4f);
            positionRange = Vector2.UnitY * 6f;
        }
        else if (dir.X < 0f)
        {
            direction = 0f;
            position = new Vector2(this.Left, this.Y - 4f);
            positionRange = Vector2.UnitY * 6f;
        }
        else if (dir.Y > 0f)
        {
            direction = -(float) Math.PI / 2f;
            position = new Vector2(this.X, this.Bottom);
            positionRange = Vector2.UnitX * 6f;
        }
        else
        {
            direction = (float) Math.PI / 2f;
            position = new Vector2(this.X, this.Top);
            positionRange = Vector2.UnitX * 6f;
        }

        this.Level.Particles.Emit(TheoCrystal.P_Impact, 12, position, positionRange, direction);
    }

    public void Swat(HoldableCollider hc, int dir)
    {
        if (!this.Hold.IsHeld || this.hitSeeker is not null)
            return;

        this.swatTimer = 0.1f;
        this.hitSeeker = hc;
        this.Hold.Holder.Swat(dir);
    }

    public bool Dangerous(HoldableCollider hc)
        => !this.Hold.IsHeld && this.Speed != Vector2.Zero && this.hitSeeker != hc;

    protected override void OnSquish(CollisionData data)
    {
        if (TrySquishWiggle(data) || this.state is States.Gone)
            return;

        Explode();
        GotoGone();
    }

    public override bool IsRiding(Solid solid)
        => this.Speed.Y == 0f && base.IsRiding(solid);

    public void HitSeeker(Seeker seeker)
    {
        if (!this.Hold.IsHeld)
            this.Speed = (this.Center - seeker.Center).SafeNormalize(120f);
        Audio.Play(SFX.game_05_crystaltheo_impact_side, this.Position);
    }

    #region Hitting other entities

    public void HitSpinner(Entity spinner)
    {
        if (!this.Hold.IsHeld && this.Speed.Length() < 0.01f && this.LiftSpeed.Length() < 0.01f && (this.previousPosition - this.ExactPosition).Length() < 0.01f && OnGround())
        {
            int num = Math.Sign(this.X - spinner.X);
            if (num == 0) num = 1;

            this.Speed.X = num * 120f;
            this.Speed.Y = -30f;
        }
    }

    public bool HitSpring(Spring spring)
    {
        if (this.Hold.IsHeld)
            return false;

        if (spring.Orientation == Spring.Orientations.Floor && this.Speed.Y >= 0f)
        {
            this.Speed.X *= 0.5f;
            this.Speed.Y = -160f;
            this.noGravityTimer = 0.15f;
            return true;
        }

        if (spring.Orientation == Spring.Orientations.WallLeft && this.Speed.X <= 0f)
        {
            MoveTowardsY(spring.CenterY + 5f, 4f);
            this.Speed.X = 220f;
            this.Speed.Y = -80f;
            this.noGravityTimer = 0.1f;
            return true;
        }

        if (spring.Orientation == Spring.Orientations.WallRight && this.Speed.X >= 0f)
        {
            MoveTowardsY(spring.CenterY + 5f, 4f);
            this.Speed.X = -220f;
            this.Speed.Y = -80f;
            this.noGravityTimer = 0.1f;
            return true;
        }

        return false;
    }

    #endregion

    #region Picking up & releasing

    private void OnPickup()
    {
        this.Speed = Vector2.Zero;
        AddTag(Tags.Persistent);
    }

    private void OnRelease(Vector2 force)
    {
        RemoveTag(Tags.Persistent);

        if (force.X != 0f && force.Y == 0f)
            force.Y = -0.4f;

        this.Speed = force * 200f;
        if (this.Speed != Vector2.Zero)
            this.noGravityTimer = 0.1f;
    }

    #endregion

    #region Explosiong

    private void Explode(bool playsound = true)
    {
        this.Collider = this.pushRadius;
        if (playsound)
            Audio.Play(SFX.game_10_puffer_splode, this.Position);

        this.puffer.Play("explode");
        if (this.state == States.Crystal)
            ShatterBowl();

        this.exploded = true;

        // Yeah, there's a lot going in there.
        DoEntityCustomInteraction();

        this.exploded = false;
        this.Collider = null;

        Level level = SceneAs<Level>();
        level.Shake();
        level.Displacement.AddBurst(this.Position, 0.4f, 12f, 36f, 0.5f);
        level.Displacement.AddBurst(this.Position, 0.4f, 24f, 48f, 0.5f);
        level.Displacement.AddBurst(this.Position, 0.4f, 36f, 60f, 0.5f);

        for (float num = 0f; num < (float) Math.PI * 2f; num += 0.17453292f)
        {
            Vector2 position = this.Center + Calc.AngleToVector(num + Calc.Random.Range(-(float) Math.PI / 90f, (float) Math.PI / 90f), Calc.Random.Range(12, 18));
            level.Particles.Emit(Seeker.P_Regen, position, num);
        }
    }

    private void DoEntityCustomInteraction()
    {
        Player player = SceneAs<Level>().Tracker.GetEntity<Player>();

        // Touch Switches
        foreach (TouchSwitch e in this.Scene.Tracker.GetEntities<TouchSwitch>())
            if (CollideCheck(e))
                e.TurnOn();

        // Floating Debris
        foreach (FloatingDebris e in this.Scene.Tracker.GetEntities<FloatingDebris>())
            if (CollideCheck(e))
                e.OnExplode(this.Position);

        foreach (Actor e in CollideAll<Actor>())
        {
            switch (e)
            {
                case Player p:
                    p.ExplodeLaunch(this.Position, snapUp: false, sidesOnly: true);
                    break;

                case TheoCrystal crystal:
                    if (!this.Scene.CollideCheck<Solid>(this.Position, crystal.Center))
                        crystal.ExplodeLaunch(this.Position);
                    break;

                case Puffer puffer:
                    VortexHelperModule.Puffer_Explode.Invoke(puffer, null);
                    VortexHelperModule.Puffer_GotoGone.Invoke(puffer, null);
                    break;

                case BowlPuffer puffer:
                    puffer.chainExplode = !puffer.exploded && puffer.state != States.Gone;
                    break;
            }
        }

        foreach (Solid e in CollideAll<Solid>())
        {
            switch (e)
            {
                case TempleCrackedBlock block:
                    block.Break(this.Position);
                    break;

                case ColorSwitch colorSwitch:
                    colorSwitch.Switch(Calc.FourWayNormal(e.Center - this.Center));
                    break;

                case DashBlock block:
                    block.Break(this.Center, Calc.FourWayNormal(e.Center - this.Center), true, true);
                    break;

                case FallingBlock block:
                    block.Triggered = true;
                    break;

                case MoveBlock block:
                    block.OnStaticMoverTrigger(null);
                    break;

                case CrushBlock block:
                    VortexHelperModule.CrushBlock_OnDashed.Invoke(block, new object[] { null, Calc.FourWayNormal(block.Center - this.Center) });
                    break;

                case BubbleWrapBlock block:
                    block.Break();
                    break;

                case LightningBreakerBox box:
                    if (player is not null)
                    {
                        float stamina = player.Stamina;
                        int dashes = player.Dashes;
                        box.OnDashCollide(player, Calc.FourWayNormal(box.Center - this.Center));
                        player.Dashes = dashes;
                        player.Stamina = stamina;
                    }
                    break;
            };
        }
    }

    #endregion

    private void SetBowlVisible(bool visible) => this.pufferBowlBottom.Visible = this.pufferBowlTop.Visible = visible;

    #region State gotos

    private void GotoGone()
    {
        SetBowlVisible(false);

        Vector2 control = this.Position + (this.startPosition - this.Position) * 0.5f;

        if (Vector2.DistanceSquared(this.startPosition, this.Position) > 100f)
        {
            if (Math.Abs(this.Position.Y - this.startPosition.Y) > Math.Abs(this.Position.X - this.startPosition.X))
                control.X += this.Position.X > this.startPosition.X ? -24 : 24;
            else
                control.Y += this.Position.Y > this.startPosition.Y ? -24f : 24;
        }

        this.returnCurve = new SimpleCurve(this.Position, this.startPosition, control);
        this.goneTimer = 2.5f;

        this.state = States.Gone;

        this.Collidable = false;
        this.Collider = null;
    }

    private void GotoCrystal()
    {
        SetBowlVisible(true);

        this.fused = false;

        Add(this.Hold);
        this.Collider = new Hitbox(8f, 10f, -4f, -10f);

        this.facing = Facings.Right;

        if (this.state is States.Gone)
        {
            this.Position = this.startPosition;
            AllSpritesPlay("recover");
            Audio.Play(SFX.game_10_puffer_reform, this.Position);
        }

        this.Speed = this.prevLiftSpeed = Vector2.Zero;

        if (!CollideCheck<Solid>(this.Position + Vector2.UnitY))
            this.noGravityTimer = 0.25f;

        this.state = States.Crystal;
    }

    #endregion

    private bool CollidePufferBarrierCheck()
    {
        foreach (PufferBarrier barrier in this.Scene.Tracker.GetEntities<PufferBarrier>())
        {
            barrier.Collidable = true;
            if (CollideCheck(barrier))
            {
                barrier.OnTouchPuffer();
                barrier.Collidable = false;
                return true;
            }
            barrier.Collidable = false;
        }

        return false;
    }

    private void PlayerThrowSelf(Player player)
    {
        if (player?.Holding?.Entity == this)
            player.Throw();
    }

    private void ShatterBowl()
    {
        Input.Rumble(RumbleStrength.Strong, RumbleLength.Short);
        Level level = SceneAs<Level>();
        level.Shake(0.175f);

        for (int i = 0; i < 10; i++)
            level.ParticlesFG.Emit(P_Shatter, 1, this.Center, Vector2.One * 7f, Calc.Random.NextFloat() * (float) Math.PI * 2);
        for (float t = 0f; t < (float) Math.PI * 2f; t += 0.17453292f)
            level.Particles.Emit(P_Crystal, this.Center + Vector2.UnitY * -6, t);
    }

    public override void Update()
    {
        base.Update();

        this.eyeSpin = Calc.Approach(this.eyeSpin, 0f, Engine.DeltaTime * 1.5f);
        this.scale = Calc.Approach(this.scale, Vector2.One, Engine.DeltaTime);

        if (this.state != States.Gone && this.cantExplodeTimer > 0f)
            this.cantExplodeTimer -= Engine.DeltaTime;

        if (this.shatterTimer > 0f)
            this.shatterTimer -= 1.5f * Engine.DeltaTime;

        Player player = this.Scene.Tracker.GetEntity<Player>();
        if (player is not null)
            this.lastPlayerPos = player.Center;

        switch (this.state)
        {
            default:
                break;

            case States.Crystal:
            {
                if (this.swatTimer > 0f)
                    this.swatTimer -= Engine.DeltaTime;

                this.hardVerticalHitSoundCooldown -= Engine.DeltaTime;
                this.Depth = Depths.TheoCrystal;

                if (this.fused)
                {
                    if (CollideCheck<Water>())
                    {
                        this.fused = false;
                        Audio.Play(SFX.game_10_puffer_shrink, this.Position);
                        this.puffer.Play("unalert");
                    }
                }
                else
                {
                    if (CollidePufferBarrierCheck())
                    {
                        this.fused = true;
                        this.explodeTimeLeft = this.explodeTime;
                        Audio.Play(SFX.game_10_puffer_expand, this.Position);
                        this.puffer.Play("alert");
                    }
                }

                if (this.fused)
                {
                    if (this.explodeTimeLeft > 0f)
                        this.explodeTimeLeft -= Engine.DeltaTime;

                    if (this.explodeTimeLeft <= 0f)
                    {
                        GotoGone();
                        ShatterBowl();
                        Explode();
                        PlayerThrowSelf(player);
                        return;
                    }
                }

                if (this.chainExplode)
                {
                    if (this.chainTimer > 0f)
                    {
                        this.chainTimer -= Engine.DeltaTime;
                    }

                    if (this.chainTimer <= 0f)
                    {
                        this.chainTimer = ChainExplosionDelay;
                        this.chainExplode = false;
                        GotoGone();
                        ShatterBowl();
                        Explode();
                        PlayerThrowSelf(player);
                        return;
                    }
                }

                if (this.Hold.IsHeld)
                {
                    this.prevLiftSpeed = Vector2.Zero;
                    this.noGravityTimer = 0f;
                }
                else
                {
                    bool inWater = CollideCheck<Water>(this.Position + Vector2.UnitY * -8);

                    if (OnGround())
                    {
                        float target = (!OnGround(this.Position + Vector2.UnitX * 3f)) ? 20f : (OnGround(this.Position - Vector2.UnitX * 3f) ? 0f : (-20f));
                        this.Speed.X = Calc.Approach(this.Speed.X, target, 800f * Engine.DeltaTime);

                        Vector2 liftSpeed = this.LiftSpeed;
                        if (liftSpeed == Vector2.Zero && this.prevLiftSpeed != Vector2.Zero)
                        {
                            this.Speed = this.prevLiftSpeed;
                            this.prevLiftSpeed = Vector2.Zero;
                            this.Speed.Y = Math.Min(this.Speed.Y * 0.6f, 0f);

                            if (this.Speed.X != 0f && this.Speed.Y == 0f)
                                this.Speed.Y = -60f;
                            if (this.Speed.Y < 0f)
                                this.noGravityTimer = 0.15f;
                        }
                        else
                        {
                            this.prevLiftSpeed = liftSpeed;
                            if (liftSpeed.Y < 0f && this.Speed.Y < 0f)
                                this.Speed.Y = 0f;
                        }

                        if (inWater)
                            this.Speed.Y = Calc.Approach(this.Speed.Y, -30, 800f * Engine.DeltaTime * 0.8f);

                    }
                    else if (this.Hold.ShouldHaveGravity)
                    {
                        float gravityRate = Math.Abs(this.Speed.Y) <= 30f ? 400 : 800;
                        float frictionRate = this.Speed.Y < 0f ? 175 : 350;

                        this.Speed.X = Calc.Approach(this.Speed.X, 0f, frictionRate * Engine.DeltaTime);
                        if (this.noGravityTimer > 0f)
                            this.noGravityTimer -= Engine.DeltaTime;
                        else
                            this.Speed.Y = Calc.Approach(this.Speed.Y, inWater ? -30f : 200f, gravityRate * Engine.DeltaTime * (inWater ? 0.7f : 1));
                    }

                    this.previousPosition = this.ExactPosition;

                    MoveH(this.Speed.X * Engine.DeltaTime, this.onCollideH);
                    MoveV(this.Speed.Y * Engine.DeltaTime, this.onCollideV);

                    if (!this.Level.Transitioning)
                    {
                        if (this.Center.X > this.Level.Bounds.Right)
                        {
                            MoveH(32f * Engine.DeltaTime);
                            if (this.Left - 8f > this.Level.Bounds.Right)
                                RemoveSelf();
                        }
                        else if (this.Left < this.Level.Bounds.Left)
                        {
                            this.Left = this.Level.Bounds.Left;
                            this.Speed.X *= -0.4f;
                        }
                        else if (this.Top < this.Level.Bounds.Top - 4)
                        {
                            this.Top = this.Level.Bounds.Top + 4;
                            this.Speed.Y = 0f;
                        }
                        else if (this.Top > this.Level.Bounds.Bottom)
                        {
                            MoveV(-5);
                            Explode();
                            GotoGone();
                        }

                        if (this.X < this.Level.Bounds.Left + 10)
                            MoveH(32f * Engine.DeltaTime);
                    }

                    if (player is not null)
                    {
                        TempleGate templeGate = CollideFirst<TempleGate>();
                        if (templeGate is not null)
                        {
                            templeGate.Collidable = false;
                            MoveH(Math.Sign(player.X - this.X) * 32 * Engine.DeltaTime);
                            templeGate.Collidable = true;
                        }
                    }
                }

                this.Hold.CheckAgainstColliders();
                if (this.hitSeeker is not null && this.swatTimer <= 0f && !this.hitSeeker.Check(this.Hold))
                    this.hitSeeker = null;
                break;
            }

            case States.Gone:
            {
                float prev = this.goneTimer;
                this.goneTimer -= Engine.DeltaTime;

                if (this.goneTimer <= 0.5f)
                {
                    if (prev > 0.5f && this.returnCurve.GetLengthParametric(8) > 8f)
                        Audio.Play(SFX.game_10_puffer_return, this.Position);
                    this.Position = this.returnCurve.GetPoint(Ease.CubeInOut(Calc.ClampedMap(this.goneTimer, 0.5f, 0f)));
                }

                if (this.goneTimer <= 2.1f && prev > 2.1f && this.noRespawn)
                    RemoveSelf();

                if (this.goneTimer <= 0f)
                {
                    this.Visible = this.Collidable = true;
                    GotoCrystal();
                }
                break;
            }
        }
    }

    public override void Render()
    {
        this.puffer.Scale = this.scale * (1f + this.inflateWiggler.Value * 0.4f);
        this.puffer.FlipX = false;

        Vector2 position = this.Position;
        this.Position.Y -= 6.0f;
        this.Position = position;

        base.Render();

        if (this.puffer.CurrentAnimationID is "alerted")
        {
            Vector2 pos = this.Position + new Vector2(3f, -4) * this.puffer.Scale;
            pos.X -= this.facing == Facings.Left ? 9 : 0;

            Vector2 to = this.lastPlayerPos + new Vector2(0f, -4f);
            Vector2 eyeOffset = Calc.AngleToVector(Calc.Angle(pos, to) + this.eyeSpin * ((float) Math.PI * 2f) * 2f, 1f);
            Vector2 eyePos = pos + new Vector2((float) Math.Round(eyeOffset.X) - 1, (float) Math.Round(Calc.ClampedMap(eyeOffset.Y, -1f, 1f, -1f, 2f)) - 9);

            Draw.Point(eyePos, Color.Black);
        }

        if (this.fused && this.explodeTime != 0)
        {
            float r = this.explodeTimeLeft / this.explodeTime;
            for (float a = 0f; a < Math.PI * 2 * r; a += 0.06f)
            {
                Vector2 p = this.Center + new Vector2((float) Math.Sin(a), -(float) Math.Cos(a)) * 16 - Vector2.UnitY * 5 - Vector2.UnitX;
                Draw.Point(p, Color.Lerp(Color.OrangeRed, Color.LawnGreen, a / (float) Math.PI));
            }
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

    public static class Hooks
    {
        public static void Hook()
        {
            On.Celeste.Spring.ctor_Vector2_Orientations_bool += Spring_orig;
            On.Celeste.Puffer.Update += Puffer_Update;
        }

        public static void Unhook()
        {
            On.Celeste.Spring.ctor_Vector2_Orientations_bool -= Spring_orig;
            On.Celeste.Puffer.Update -= Puffer_Update;
        }

        private static void Puffer_Update(On.Celeste.Puffer.orig_Update orig, Puffer self)
        {
            orig(self);

            if (!self.Collidable)
                return;

            foreach (PufferBarrier barrier in self.Scene.Tracker.GetEntities<PufferBarrier>())
                barrier.Collidable = true;

            PufferBarrier collided = self.CollideFirst<PufferBarrier>();
            if (collided is not null)
            {
                collided.OnTouchPuffer();

                VortexHelperModule.Puffer_Explode.Invoke(self, new object[] { });
                VortexHelperModule.Puffer_GotoGone.Invoke(self, new object[] { });
            }

            foreach (PufferBarrier barrier in self.Scene.Tracker.GetEntities<PufferBarrier>())
                barrier.Collidable = false;
        }

        private static void Spring_orig(On.Celeste.Spring.orig_ctor_Vector2_Orientations_bool orig, Spring self, Vector2 position, Spring.Orientations orientation, bool playerCanUse)
        {
            orig(self, position, orientation, playerCanUse);
            self.Add(new BowlPufferCollider(Spring_OnBowlPuffer));
        }

        private static void Spring_OnBowlPuffer(BowlPuffer puffer, Spring self)
        {
            puffer.HitSpring(self);
            VortexHelperModule.Spring_BounceAnimate.Invoke(self, new object[] { });
        }
    }
}
