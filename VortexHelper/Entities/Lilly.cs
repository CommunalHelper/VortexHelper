using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using VortexHelper;

namespace Celeste.Mod.VortexHelper.Entities
{
    [CustomEntity("VortexHelper/Lilly")]
    class Lilly : Solid
    {
        private class LillyArmEnd : Solid
        {
            public Vector2 startPosition;
            public float Distance => Position.X - startPosition.X;

            public LillyArmEnd(Vector2 position, List<StaticMover> newStaticMovers) 
                : base(position + Vector2.UnitY, 6, 17, true)
            {
                startPosition = position;
                SurfaceSoundIndex = SurfaceIndex.CassetteBlock;
                staticMovers = newStaticMovers;
            }
        }

        private class LillyArm : JumpThru
        {
            private LillyArmEnd end;
            private int origin;
            private int endOffset;

            private int From => Math.Min(origin, (int)end.X);
            private int To => Math.Max(origin, (int)end.X + endOffset);

            public LillyArm(Vector2 position, LillyArmEnd to, int fromX, int endOffset)
                : base(position, 32, true)
            {
                end = to;
                origin = fromX;
                this.endOffset = endOffset;
                SurfaceSoundIndex = SurfaceIndex.CassetteBlock;
            }

            public void UpdateArm(float move)
            {
                MoveH(move);
                X = From; 
                Collider = Math.Abs(end.Distance) > 0 ? new Hitbox(To - From, 5) : null;
            }
        }

        public const float ArmSpeed = 240;
        public const float ArmSpeedRetract = 112;

        public static readonly Color IdleColor = Calc.HexToColor("0061ff");
        public static readonly Color ClimbedOnColor = Calc.HexToColor("ff38f1");
        public static readonly Color DashColor = Calc.HexToColor("ff0033");
        public static readonly Color RetractColor = Calc.HexToColor("4800ff");
        public static readonly Color IdleAltColor = Calc.HexToColor("00d0ff");
        public static readonly Color ClimbedOnAltColor = Calc.HexToColor("f432ff");
        public static readonly Color HorrifiedColor = Calc.HexToColor("bc51ff");

        private Color colorTo = IdleColor, colorFrom = IdleColor;
        private float colorLerp = 1f;

        public enum FaceState
        {
            Idle,
            ClimbedOn,
            Dash,
            Retract,
            IdleAlt,
            ClimbedOnAlt,
            Horrified
        }
        private FaceState faceState;
        private Sprite block, face;

        private Vector2 scale = Vector2.One;

        private Level level;

        private MTexture armEnd;
        private List<MTexture> arm;
        private bool armsExtended = false;
        private float leftArmOffset, rightArmOffset;

        private int maxLength;

        private SoundSource sfx;

        private bool Activated => faceState == FaceState.Dash || faceState == FaceState.Retract;
        private bool WasUsedOnce => faceState == FaceState.IdleAlt || faceState == FaceState.ClimbedOnAlt;

        private BloomPoint bloom;

        private List<StaticMover> leftStaticMovers = new List<StaticMover>();
        private List<StaticMover> rightStaticMovers = new List<StaticMover>();

        public Lilly(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Int("maxLength")) { }

        public Lilly(Vector2 position, int maxLength)
            : base(position, 24, 24, true)
        {
            SurfaceSoundIndex = SurfaceIndex.CassetteBlock;

            armEnd = GFX.Game["objects/VortexHelper/squareBumperNew/armEnd"];
            arm = GFX.Game.GetAtlasSubtextures("objects/VortexHelper/squareBumperNew/arm");

            this.maxLength = Math.Abs(maxLength);

            Vector2 middle = new Vector2(12, 12);

            block = VortexHelperModule.LillySpriteBank.Create("lillyBlock");
            block.Position = middle;
            Add(block);

            Add(sfx = new SoundSource()
            {
                Position = middle
            });

            face = VortexHelperModule.LillySpriteBank.Create("lillyFace");
            face.Position = new Vector2(12, 13);
            face.Color = IdleColor;
            Add(face);

            OnDashCollide = OnDashed;

            Add(bloom = new BloomPoint(.65f, 16f)
            {
                Position = middle,
                Visible = false
            });
        }

        private DashCollisionResults OnDashed(Player player, Vector2 dir)
        {
            if (!Activated)
            {
                scale = new Vector2(1f + Math.Abs(dir.Y) * 0.4f - Math.Abs(dir.X) * 0.4f, 1f + Math.Abs(dir.X) * 0.4f - Math.Abs(dir.Y) * 0.4f);
                Add(new Coroutine(DashedSequence()));
                return DashCollisionResults.Rebound;
            }
            return DashCollisionResults.NormalCollision;
        }

        private IEnumerator DashedSequence()
        {
            // Dashed in, shaking.
            faceState = FaceState.Dash;
            face.Play("dashed", true);
            ChangeColor(DashColor);
            StartShaking(.375f);
            Audio.Play(CustomSFX.game_lilly_dashed, Center);
            yield return .5f;

            // Arms extend.
            leftArmOffset = 0f;
            rightArmOffset = 0f;
            bloom.Visible = true;
            block.Play("active", true);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
            level.DirectionalShake(Vector2.UnitX, 0.1f);
            armsExtended = true;

            sfx.Play(CustomSFX.game_lilly_conveyor, "end", 0f);

            LillyArmEnd rightArmEnd = new LillyArmEnd(Position + new Vector2(Width - 6, 0), rightStaticMovers);
            LillyArmEnd leftArmEnd = new LillyArmEnd(Position, leftStaticMovers);
            LillyArm rightArm = new LillyArm(new Vector2(X + Width, Y), rightArmEnd, (int)(X + Width), 6);
            LillyArm leftArm = new LillyArm(new Vector2(X + 6, Y), leftArmEnd, (int)X, 0);
            AddArm(rightArmEnd, rightArm); AddArm(leftArmEnd, leftArm);
            bool rightArmExtended = false, leftArmExtended = false;
            while (!rightArmExtended || !leftArmExtended)
            {
                Collidable = false;
                if (!rightArmExtended)
                {
                    float moveAmount = rightArmEnd.X;
                    float move = (rightArmExtended = (rightArmEnd.X + ArmSpeed * Engine.DeltaTime > rightArmEnd.startPosition.X + maxLength)) ?
                        (rightArmEnd.startPosition.X + maxLength) - rightArmEnd.X : ArmSpeed * Engine.DeltaTime;

                    rightArmEnd.MoveHCollideSolids(move, true, delegate
                    {
                        rightArmExtended = true;
                    });

                    moveAmount = rightArmEnd.X - moveAmount;
                    rightArm.UpdateArm(moveAmount);

                    if (rightArmExtended)
                    {
                        level.DirectionalShake(Vector2.UnitX, 0.25f);
                        Audio.Play(CustomSFX.game_lilly_arm_impact, rightArmEnd.Center, "retract", 0f);
                        Input.Rumble(RumbleStrength.Strong, RumbleLength.Short);
                    }
                }
                if (!leftArmExtended)
                {
                    float moveAmount = leftArmEnd.X;
                    float move = (leftArmExtended = (leftArmEnd.X - ArmSpeed * Engine.DeltaTime < leftArmEnd.startPosition.X - maxLength)) ?
                        (leftArmEnd.startPosition.X - maxLength) - leftArmEnd.X : -ArmSpeed * Engine.DeltaTime;

                    leftArmEnd.MoveHCollideSolids(move, true, delegate
                    {
                        leftArmExtended = true;
                    });

                    moveAmount = leftArmEnd.X - moveAmount;
                    leftArm.UpdateArm(moveAmount);

                    if (leftArmExtended)
                    {
                        level.DirectionalShake(Vector2.UnitX, 0.25f);
                        Audio.Play(CustomSFX.game_lilly_arm_impact, leftArmEnd.Center, "retract", 0f);
                        Input.Rumble(RumbleStrength.Strong, RumbleLength.Short);
                    }
                }
                leftArmOffset = leftArmEnd.Distance;
                rightArmOffset = rightArmEnd.Distance;
                Collidable = true;
                yield return null;
            }

            // Arms collide, waiting 1s.
            sfx.Param("end", 1f);
            yield return 1f;

            // Arms are retracting.
            faceState = FaceState.Retract;
            face.Play("retract");
            sfx.Play(CustomSFX.game_lilly_conveyor, "end", 0f);
            ChangeColor(RetractColor);
            float retractFactor = 0f;
            while(rightArmExtended || leftArmExtended)
            {
                retractFactor = Calc.Approach(retractFactor, 1f, Engine.DeltaTime * 3f);
                float retractSpeed = ArmSpeedRetract * retractFactor;
                bool scrapeParticles = level.OnInterval(.3f);
                if (rightArmExtended)
                {
                    float newX = rightArmEnd.X - retractSpeed * Engine.DeltaTime;
                    bool finished = false;
                    if (newX < rightArmEnd.startPosition.X)
                    {
                        finished = true;
                        newX = rightArmEnd.startPosition.X;
                        StartShaking(0.1f);
                        Audio.Play(CustomSFX.game_lilly_arm_impact, Center, "retract", 1f);
                    }
                    float move = newX - rightArmEnd.X;
                    rightArmEnd.MoveH(move);
                    rightArm.UpdateArm(move);
                    rightArmExtended = !finished;
                }
                if (leftArmExtended)
                {
                    float newX = leftArmEnd.X + retractSpeed * Engine.DeltaTime;
                    bool finished = false;
                    if (newX > leftArmEnd.startPosition.X)
                    {
                        finished = true;
                        newX = leftArmEnd.startPosition.X;
                        StartShaking(0.1f);
                        Audio.Play(CustomSFX.game_lilly_arm_impact, Center, "retract", 1f);
                    }
                    float move = newX - leftArmEnd.X;
                    leftArmEnd.MoveH(move);
                    leftArm.UpdateArm(move);
                    leftArmExtended = !finished;
                }
                leftArmOffset = leftArmEnd.Distance;
                rightArmOffset = rightArmEnd.Distance;
                yield return null;
            }

            // Back together.
            bloom.Visible = false;
            sfx.Param("end", 1f);
            faceState = FaceState.IdleAlt;
            face.Play("end_retract");
            block.Play("inactive", true);
            ChangeColor(IdleAltColor);

            armsExtended = false;
            RemoveArms(rightArmEnd, rightArm);
            RemoveArms(leftArmEnd, leftArm);
            yield return .25f;
        }

        private void AddArm(LillyArmEnd armEnd, LillyArm arm)
        {
            level.Add(armEnd);
            level.Add(arm);
            armEnd.Added(level); arm.Added(level);
        }

        private void RemoveArms(LillyArmEnd armEnd, LillyArm arm)
        {
            level.Remove(armEnd); level.Remove(arm);
        }

        private void ChangeColor(Color to)
        {
            colorTo = to;
            colorFrom = face.Color;
            colorLerp = 0f;
        }

        private void UpdateFace()
        {
            scale = Calc.Approach(scale, Vector2.One, Engine.DeltaTime * 4f);
            face.Scale = block.Scale = scale;

            colorLerp = Calc.Approach(colorLerp, 1f, Engine.DeltaTime * 3f);
            face.Color = Color.Lerp(colorFrom, colorTo, colorLerp);

            Player player = VortexHelperModule.GetPlayer();
            if (player == null && WasUsedOnce)
            {
                faceState = FaceState.Horrified;
                face.Play("horrified");
                ChangeColor(HorrifiedColor);
            }

            if (!Activated && faceState != FaceState.Horrified)
            {
                bool riden = HasPlayerRider();
                if (riden && faceState == FaceState.Idle)
                {
                    faceState = FaceState.ClimbedOn;
                    face.Play("idle_climb");
                    ChangeColor(ClimbedOnColor);
                }
                else if (!riden && faceState == FaceState.ClimbedOn)
                {
                    faceState = FaceState.Idle;
                    face.Play("climb_idle");
                    ChangeColor(IdleColor);
                }
                else if (riden && faceState == FaceState.IdleAlt)
                {
                    faceState = FaceState.ClimbedOnAlt;
                    face.Play("climb_alt");
                    ChangeColor(ClimbedOnAltColor);
                }
                else if (!riden && faceState == FaceState.ClimbedOnAlt)
                {
                    faceState = FaceState.IdleAlt;
                    face.Play("idle_alt");
                    ChangeColor(IdleAltColor);
                }
            }
        }

        public override void Awake(Scene scene)
        {
            base.Awake(scene);
            level = SceneAs<Level>();
            Add();

            foreach(StaticMover sm in staticMovers)
            {
                if (sm.Entity.Top < Top) continue;

                if (sm.Entity.Left < Left)
                {
                    leftStaticMovers.Add(sm);
                    continue;
                }
                if (sm.Entity.Right > Right)
                {
                    rightStaticMovers.Add(sm);
                    continue;
                }
            }

            foreach(StaticMover sm in leftStaticMovers)
                staticMovers.Remove(sm);
            foreach (StaticMover sm in rightStaticMovers)
                staticMovers.Remove(sm);
        }

        public override void Update()
        {
            base.Update();
            UpdateFace();
        }

        public override void Render()
        {
            Vector2 pos = Position;
            Position += Shake;

            Vector2 leftArmPos = Position + new Vector2(8 + leftArmOffset, 0);
            Vector2 rightArmPos = Position + new Vector2(Width - 8 + rightArmOffset, 0);

            if (armsExtended)
            {
                MTexture armTexture = arm[(int)(Scene.TimeActive * 12) % 2];
                for(int i = (int)leftArmPos.X - 8; i <= Left + 8; i += 8)
                {
                    armTexture.Draw(new Vector2(i, Y));
                }
                for (int i = (int)rightArmPos.X - 1; i >= Right - 16; i -= 8)
                {
                    armTexture.Draw(new Vector2(i, Y));
                }
            }

            base.Render();
            if (armsExtended)
            {
                armEnd.Draw(rightArmPos);
                armEnd.Draw(leftArmPos, Vector2.Zero, Color.White, new Vector2(-1, 1));
            }
            Position = pos;
        }
    }
}
