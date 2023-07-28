using Celeste.Mod.Entities;
using Celeste.Mod.VortexHelper.Misc;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Celeste.Mod.VortexHelper.Entities;

[CustomEntity("VortexHelper/Lilly")]
public class Lilly : Solid
{
    private class LillyArmEnd : Solid
    {
        public Vector2 startPosition;
        public float Distance => this.Position.X - this.startPosition.X;

        public LillyArmEnd(Vector2 position, int height, List<StaticMover> newStaticMovers)
            : base(position + Vector2.UnitY, 6, height, true)
        {
            this.startPosition = position;
            this.SurfaceSoundIndex = SurfaceIndex.CassetteBlock;
            this.staticMovers = newStaticMovers;
        }
    }

    private class LillyArm : JumpThru
    {
        private readonly LillyArmEnd end;
        private readonly int origin;
        private readonly int endOffset;

        private int From => Math.Min(this.origin, (int) this.end.X);
        private int To => Math.Max(this.origin, (int) this.end.X + this.endOffset);

        public LillyArm(Vector2 position, LillyArmEnd to, int fromX, int endOffset)
            : base(position, 32, true)
        {
            this.end = to;
            this.origin = fromX;
            this.endOffset = endOffset;
            this.SurfaceSoundIndex = SurfaceIndex.CassetteBlock;
        }

        public void UpdateArm(float move)
        {
            MoveH(move);
            this.X = this.From;
            this.Collider = Math.Abs(this.end.Distance) > 0 ? new Hitbox(this.To - this.From, 5) : null;
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
    private readonly Sprite face;

    private Vector2 scale = Vector2.One;

    private Level level;

    private List<MTexture> arm;
    private bool armsExtended = false;
    private float rightLength, leftLength;

    /*
     * 4D array :
     * i & j ---> x and y tile position in texture.
     * k     ---> frame of the texture, between 0 and 1.
     * l     ---> state of the texture (0 = 'block', 1 = 'active_block').
     */
    private static readonly MTexture[,,,] blockTextures = new MTexture[3, 4, 2, 2];
    private static readonly MTexture[] armEndTextures = new MTexture[4];

    private readonly int maxLength;

    private readonly SoundSource sfx;

    private bool Activated => this.faceState is FaceState.Dash or FaceState.Retract;
    private bool WasUsedOnce => this.faceState is FaceState.IdleAlt or FaceState.ClimbedOnAlt;

    private readonly BloomPoint bloom;

    private readonly List<StaticMover> leftStaticMovers = new();
    private readonly List<StaticMover> rightStaticMovers = new();

    public Lilly(EntityData data, Vector2 offset)
        : this(data.Position + offset, data.Height, data.Int("maxLength")) { }

    public Lilly(Vector2 position, int height, int maxLength)
        : base(position, 24, height, true)
    {
        this.SurfaceSoundIndex = SurfaceIndex.CassetteBlock;
        this.arm = GFX.Game.GetAtlasSubtextures("objects/VortexHelper/squareBumperNew/arm");

        this.maxLength = Math.Abs(maxLength);

        Vector2 middle = new Vector2(this.Width, height) / 2;

        Add(this.sfx = new SoundSource()
        {
            Position = middle
        });

        this.face = VortexHelperModule.LillySpriteBank.Create("lillyFace");
        this.face.Position = middle + Vector2.UnitY;
        this.face.Color = IdleColor;
        Add(this.face);

        this.OnDashCollide = OnDashed;

        Add(this.bloom = new BloomPoint(.65f, 16f)
        {
            Position = middle,
            Visible = false
        });
    }

    private DashCollisionResults OnDashed(Player player, Vector2 dir)
    {
        if (this.Activated)
            return DashCollisionResults.NormalCollision;

        this.scale = new Vector2(1f + Math.Abs(dir.Y) * 0.4f - Math.Abs(dir.X) * 0.4f, 1f + Math.Abs(dir.X) * 0.4f - Math.Abs(dir.Y) * 0.4f);
        Add(new Coroutine(DashedSequence()));
        return DashCollisionResults.Rebound;
    }

    private IEnumerator DashedSequence()
    {
        // Dashed in, shaking.
        this.faceState = FaceState.Dash;
        this.face.Play("dashed", true);
        ChangeColor(DashColor);
        StartShaking(0.375f);
        Audio.Play(CustomSFX.game_lilly_dashed, this.Center);
        yield return 0.5f;

        // Arms extend.
        this.rightLength = this.leftLength = 0f;
        this.bloom.Visible = true;
        Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
        this.level.DirectionalShake(Vector2.UnitX, 0.1f);
        this.armsExtended = true;

        this.sfx.Play(CustomSFX.game_lilly_conveyor, "end", 0f);

        var rightArmEnd = new LillyArmEnd(this.Position + new Vector2(this.Width - 6, 0), (int) this.Height - 9, this.rightStaticMovers);
        var leftArmEnd = new LillyArmEnd(this.Position, (int) this.Height - 9, this.leftStaticMovers);
        var rightArm = new LillyArm(new Vector2(this.X + this.Width, this.Y), rightArmEnd, (int) (this.X + this.Width), 6);
        var leftArm = new LillyArm(new Vector2(this.X + 6, this.Y), leftArmEnd, (int) this.X, 0);
        AddArm(rightArmEnd, rightArm); AddArm(leftArmEnd, leftArm);

        bool rightArmExtended = false, leftArmExtended = false;
        while (!rightArmExtended || !leftArmExtended)
        {
            this.Collidable = false;
            if (!rightArmExtended)
            {
                float moveAmount = rightArmEnd.X;
                float move = (rightArmExtended = rightArmEnd.X + ArmSpeed * Engine.DeltaTime > rightArmEnd.startPosition.X + this.maxLength) ?
                    rightArmEnd.startPosition.X + this.maxLength - rightArmEnd.X : ArmSpeed * Engine.DeltaTime;

                rightArmEnd.MoveHCollideSolids(move, true, delegate
                {
                    rightArmExtended = true;
                });

                moveAmount = rightArmEnd.X - moveAmount;
                rightArm.UpdateArm(moveAmount);

                if (rightArmExtended)
                {
                    this.level.DirectionalShake(Vector2.UnitX, 0.25f);
                    Audio.Play(CustomSFX.game_lilly_arm_impact, rightArmEnd.Center, "retract", 0f);
                    Input.Rumble(RumbleStrength.Strong, RumbleLength.Short);
                }
            }

            if (!leftArmExtended)
            {
                float moveAmount = leftArmEnd.X;
                float move = (leftArmExtended = leftArmEnd.X - ArmSpeed * Engine.DeltaTime < leftArmEnd.startPosition.X - this.maxLength) ?
                    leftArmEnd.startPosition.X - this.maxLength - leftArmEnd.X : -ArmSpeed * Engine.DeltaTime;

                leftArmEnd.MoveHCollideSolids(move, true, delegate
                {
                    leftArmExtended = true;
                });

                moveAmount = leftArmEnd.X - moveAmount;
                leftArm.UpdateArm(moveAmount);

                if (leftArmExtended)
                {
                    this.level.DirectionalShake(Vector2.UnitX, 0.25f);
                    Audio.Play(CustomSFX.game_lilly_arm_impact, leftArmEnd.Center, "retract", 0f);
                    Input.Rumble(RumbleStrength.Strong, RumbleLength.Short);
                }
            }

            this.rightLength = rightArmEnd.Distance;
            this.leftLength = leftArmEnd.Distance;
            this.Collidable = true;
            yield return null;
        }

        // Arms collide, waiting 1s.
        this.sfx.Param("end", 1f);
        yield return 1f;

        // Arms are retracting.
        this.faceState = FaceState.Retract;
        this.face.Play("retract");
        this.sfx.Play(CustomSFX.game_lilly_conveyor, "end", 0f);
        ChangeColor(RetractColor);

        float retractFactor = 0f;
        while (rightArmExtended || leftArmExtended)
        {
            retractFactor = Calc.Approach(retractFactor, 1f, Engine.DeltaTime * 3f);
            float retractSpeed = ArmSpeedRetract * retractFactor;
            bool scrapeParticles = this.level.OnInterval(.3f);

            if (rightArmExtended)
            {
                float newX = rightArmEnd.X - retractSpeed * Engine.DeltaTime;
                bool finished = false;

                if (newX < rightArmEnd.startPosition.X)
                {
                    finished = true;
                    newX = rightArmEnd.startPosition.X;
                    StartShaking(0.1f);
                    Audio.Play(CustomSFX.game_lilly_arm_impact, this.Center, "retract", 1f);
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
                    Audio.Play(CustomSFX.game_lilly_arm_impact, this.Center, "retract", 1f);
                }

                float move = newX - leftArmEnd.X;
                leftArmEnd.MoveH(move);
                leftArm.UpdateArm(move);
                leftArmExtended = !finished;
            }

            this.rightLength = rightArmEnd.Distance;
            this.leftLength = leftArmEnd.Distance;
            yield return null;
        }

        // Back together.
        this.bloom.Visible = false;
        this.sfx.Param("end", 1f);
        this.faceState = FaceState.IdleAlt;
        this.face.Play("end_retract");
        ChangeColor(IdleAltColor);

        this.armsExtended = false;
        RemoveArms(rightArmEnd, rightArm);
        RemoveArms(leftArmEnd, leftArm);
        yield return 0.25f;
    }

    private void AddArm(LillyArmEnd armEnd, LillyArm arm)
    {
        this.level.Add(armEnd);
        this.level.Add(arm);
        armEnd.Added(this.level); arm.Added(this.level);
    }

    private void RemoveArms(LillyArmEnd armEnd, LillyArm arm)
    {
        this.level.Remove(armEnd); this.level.Remove(arm);
    }

    private void ChangeColor(Color to)
    {
        this.colorTo = to;
        this.colorFrom = this.face.Color;
        this.colorLerp = 0f;
    }

    private void UpdateFace()
    {
        this.scale = Calc.Approach(this.scale, Vector2.One, Engine.DeltaTime * 4f);
        this.face.Scale = this.scale;

        this.colorLerp = Calc.Approach(this.colorLerp, 1f, Engine.DeltaTime * 3f);
        this.face.Color = Color.Lerp(this.colorFrom, this.colorTo, this.colorLerp);

        Util.TryGetPlayer(out Player player);
        if (player is null && this.WasUsedOnce)
        {
            this.faceState = FaceState.Horrified;
            this.face.Play("horrified");
            ChangeColor(HorrifiedColor);
        }

        if (this.Activated || this.faceState == FaceState.Horrified)
            return;

        bool ridden = HasPlayerRider();
        if (ridden && this.faceState == FaceState.Idle)
        {
            this.faceState = FaceState.ClimbedOn;
            this.face.Play("idle_climb");
            ChangeColor(ClimbedOnColor);
        }
        else if (!ridden && this.faceState == FaceState.ClimbedOn)
        {
            this.faceState = FaceState.Idle;
            this.face.Play("climb_idle");
            ChangeColor(IdleColor);
        }
        else if (ridden && this.faceState == FaceState.IdleAlt)
        {
            this.faceState = FaceState.ClimbedOnAlt;
            this.face.Play("climb_alt");
            ChangeColor(ClimbedOnAltColor);
        }
        else if (!ridden && this.faceState == FaceState.ClimbedOnAlt)
        {
            this.faceState = FaceState.IdleAlt;
            this.face.Play("idle_alt");
            ChangeColor(IdleAltColor);
        }
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        this.level = SceneAs<Level>();
        Add();

        foreach (StaticMover sm in this.staticMovers)
        {
            if (sm.Entity.Top < this.Top)
                continue;

            if (sm.Entity.Left < this.Left)
            {
                this.leftStaticMovers.Add(sm);
                continue;
            }

            if (sm.Entity.Right > this.Right)
            {
                this.rightStaticMovers.Add(sm);
                continue;
            }
        }

        foreach (StaticMover sm in this.leftStaticMovers)
            this.staticMovers.Remove(sm);
        foreach (StaticMover sm in this.rightStaticMovers)
            this.staticMovers.Remove(sm);
    }

    public override void Update()
    {
        base.Update();
        UpdateFace();
    }

    public override void Render()
    {
        Vector2 pos = this.Position;
        this.Position += this.Shake;

        Vector2 leftArmPos = this.Position + new Vector2(8 + this.leftLength, 0);
        Vector2 rightArmPos = this.Position + new Vector2(this.Width - 8 + this.rightLength, 0);

        int frame = (int) (this.Scene.TimeActive * 12) % 2;

        if (this.armsExtended)
        {
            MTexture armTexture = this.arm[frame];

            int x = (int) (this.rightLength + 16);
            while (x > 8)
            {
                armTexture.Draw(new Vector2(this.X + x, this.Y));
                x -= 8;
            }

            x = (int) this.leftLength;
            while (x < 8)
            {
                armTexture.Draw(new Vector2(this.X + x, this.Y));
                x += 8;
            }
        }

        Vector2 renderOffset = Vector2.One * 4;

        int h = (int) (this.Height / 8);
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < h; j++)
            {
                int ty = j == 0 ? 0 : (j == h - 1 ? 3 : (j == h - 2 ? 2 : 1));
                MTexture tile = blockTextures[i, ty, frame, this.armsExtended ? 1 : 0];

                Vector2 p = this.Position + new Vector2(i * 8, j * 8);
                Vector2 newPos = this.Center + (p - this.Center) * this.scale + this.Shake;

                tile.DrawCentered(newPos + renderOffset, Color.White, this.scale);
            }
        }

        base.Render();

        if (this.armsExtended)
        {
            for (int j = 0; j < h; j++)
            {
                int ty = j == 0 ? 0 : (j == h - 1 ? 3 : (j == h - 2 ? 2 : 1));
                MTexture armEnd = armEndTextures[ty];
                Vector2 yOffset = Vector2.UnitY * j * 8;
                armEnd.Draw(rightArmPos + yOffset);
                armEnd.Draw(leftArmPos + yOffset, Vector2.Zero, Color.White, new Vector2(-1, 1));
            }
        }

        this.Position = pos;
    }

    public static void InitializeTextures()
    {
        MTexture block00 = GFX.Game["objects/VortexHelper/squareBumperNew/block00"];
        MTexture block01 = GFX.Game["objects/VortexHelper/squareBumperNew/block01"];
        MTexture active_block00 = GFX.Game["objects/VortexHelper/squareBumperNew/active_block00"];
        MTexture active_block01 = GFX.Game["objects/VortexHelper/squareBumperNew/active_block01"];
        MTexture armend = GFX.Game["objects/VortexHelper/squareBumperNew/armend"];

        for (int j = 0; j < 4; j++)
        {
            int ty = j == 0 ? 0 : (j == 3 ? 16 : (j == 2 ? 8 : 24));
            for (int i = 0; i < 3; i++)
            {
                int tx = i * 8;
                blockTextures[i, j, 0, 0] = block00.GetSubtexture(tx, ty, 8, 8);
                blockTextures[i, j, 1, 0] = block01.GetSubtexture(tx, ty, 8, 8);
                blockTextures[i, j, 0, 1] = active_block00.GetSubtexture(tx, ty, 8, 8);
                blockTextures[i, j, 1, 1] = active_block01.GetSubtexture(tx, ty, 8, 8);
            }
            armEndTextures[j] = armend.GetSubtexture(0, ty, 8, 8);
        }
    }
}
