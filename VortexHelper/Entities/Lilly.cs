using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;

namespace Celeste.Mod.VortexHelper.Entities
{
    [CustomEntity("VortexHelper/Lilly")]
    class Lilly : Solid
    {
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

        private bool Activated => faceState == FaceState.Dash || faceState == FaceState.Retract;
        private bool WasUsedOnce => faceState == FaceState.IdleAlt || faceState == FaceState.ClimbedOnAlt;

        public Lilly(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Width, data.Height) { }

        public Lilly(Vector2 position, int width, int height)
            : base(position, width, height, true)
        {
            SurfaceSoundIndex = SurfaceIndex.CassetteBlock;

            block = VortexHelperModule.LillySpriteBank.Create("lillyBlock");
            block.Position = new Vector2(width / 2, height / 2);
            Add(block);

            face = VortexHelperModule.LillySpriteBank.Create("lillyFace");
            face.Position = new Vector2(width / 2, height / 2 + 1);
            face.Color = IdleColor;
            Add(face);

            OnDashCollide = OnDashed;
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
            StartShaking(.3f);
            yield return .5f;

            // Arms extend.
            block.Play("active", true);
            level.DirectionalShake(Vector2.UnitX, 0.1f);
            yield return 1f;

            // Arms collide, waiting 300ms.
            level.DirectionalShake(Vector2.UnitX, 0.2f);
            yield return 0.3f;

            // Arms are retracting.
            faceState = FaceState.Retract;
            face.Play("retract");
            ChangeColor(RetractColor);
            yield return 1f;

            // Back together.
            faceState = FaceState.IdleAlt;
            face.Play("end_retract");
            block.Play("inactive", true);
            ChangeColor(IdleAltColor);
            StartShaking(0.1f);
            yield return .25f;
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
            base.Render();
            Position = pos;
        }
    }
}
