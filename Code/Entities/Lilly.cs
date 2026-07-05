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

        public void UpdateArm(float move, bool armsGiveLiftSpeed)
        {
            MoveH(move);
            if (this.Collidable && armsGiveLiftSpeed)
            {
                foreach (Actor entity in Scene.Tracker.GetEntities<Actor>())
                {
                    if (entity.IsRiding(this))
                    {
                        if (entity.TreatNaive)
                        {
                            entity.LiftSpeed = LiftSpeed;
                        }
                        else
                        {
                            entity.LiftSpeed = LiftSpeed;
                        }
                    }
                }
            }

            this.X = this.From;
            this.Collider = Math.Abs(this.end.Distance) > 0 ? new Hitbox(this.To - this.From, 5) : null;
        }
    }
    
    public float ArmSpeed;
    public const float ArmSpeedRetract = 112;

    private readonly Color idleColor = Calc.HexToColor("0061ff");
    private readonly Color climbedOnColor = Calc.HexToColor("ff38f1");
    private readonly Color dashColor = Calc.HexToColor("ff0033");
    private readonly Color retractColor = Calc.HexToColor("4800ff");
    private readonly Color idleAltColor = Calc.HexToColor("00d0ff");
    private readonly Color climbedOnAltColor = Calc.HexToColor("f432ff");
    private readonly Color horrifiedColor = Calc.HexToColor("bc51ff");

    private Color colorTo = Calc.HexToColor("0061ff"), colorFrom = Calc.HexToColor("0061ff"); // IdleColor
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
    private readonly MTexture[,,,] blockTextures = new MTexture[3, 4, 2, 2];
    private readonly MTexture[] armEndTextures = new MTexture[4];

    private readonly int maxLength;

    private readonly SoundSource sfx;

    private bool Activated => this.faceState is FaceState.Dash or FaceState.Retract;
    private bool WasUsedOnce => this.faceState is FaceState.IdleAlt or FaceState.ClimbedOnAlt;
    
    private readonly bool overClocked;
    
    public readonly bool armsGiveLiftSpeed;

    private readonly BloomPoint bloom;

    private readonly List<StaticMover> leftStaticMovers = new();
    private readonly List<StaticMover> rightStaticMovers = new();

    public Lilly(EntityData data, Vector2 offset)
        : this(data.Position + offset, data.Height, data.Int("maxLength"), data.Attr("spriteDir", "").Trim().TrimEnd('/'), data.Bool("overclocked"), data.Bool("armsGiveLiftSpeed"), 
            data.HexColor("idleColor", Calc.HexToColor("0061ff")), data.HexColor("climbedOnColor", Calc.HexToColor("ff38f1")), data.HexColor("dashColor", Calc.HexToColor("ff0033")), data.HexColor("retractColor", Calc.HexToColor("4800ff")),
            data.HexColor("idleAltColor", Calc.HexToColor("00d0ff")), data.HexColor("climbedOnAltColor", Calc.HexToColor("f432ff")), data.HexColor("horrifiedColor", Calc.HexToColor("bc51ff")))
    { }

    public Lilly(Vector2 position, int height, int maxLength, string spriteDir, bool overClocked, bool armsGiveLiftSpeed,
        Color idleColor, Color climbedOnColor, Color dashColor, Color retractColor, Color idleAltColor, Color climbedOnAltColor, Color horrifiedColor)
        : base(position, 24, height, true)
    {
        this.SurfaceSoundIndex = SurfaceIndex.CassetteBlock;
        this.arm = GFX.Game.GetAtlasSubtextures(string.IsNullOrEmpty(spriteDir) ? (overClocked ? "objects/VortexHelper/squareBumperNew/OCarm" : "objects/VortexHelper/squareBumperNew/arm") : spriteDir + "/arm");

        this.maxLength = Math.Abs(maxLength);

        Vector2 middle = new Vector2(this.Width, height) / 2;

        Add(this.sfx = new SoundSource()
        {
            Position = middle
        });

        this.armsGiveLiftSpeed = armsGiveLiftSpeed;
        this.overClocked = overClocked;
        this.idleColor = idleColor;
        this.climbedOnColor = climbedOnColor;
        this.dashColor = dashColor;
        this.retractColor = retractColor;
        this.idleAltColor = idleAltColor;
        this.climbedOnAltColor = climbedOnAltColor;
        this.horrifiedColor = horrifiedColor;
        this.colorFrom = this.colorTo = this.idleColor;

        this.face = string.IsNullOrEmpty(spriteDir) ? VortexHelperModule.LillySpriteBank.Create("lillyFace") : BuildCustomFaceSprite(spriteDir);
        this.face.Position = middle + Vector2.UnitY;
        this.face.Color = this.idleColor;
        Add(this.face);

        if (!string.IsNullOrEmpty(spriteDir)) 
            InitializeTextures(spriteDir);
        else 
            InitializeTextures("objects/VortexHelper/squareBumperNew");

        this.OnDashCollide = OnDashed;

        this.ArmSpeed = overClocked ? 360f : 240f;

        Add(this.bloom = new BloomPoint(.65f, 16f)
        {
            Position = middle,
            Visible = false,
        });

    }

    private static Sprite BuildCustomFaceSprite(string path)
    {
        Sprite faceSprite = new Sprite(GFX.Game, path + "/");

        // <Loop id="idle" path="face" frames="0"/>
        faceSprite.AddLoop("idle", "face", 0f, 0);
        // <Anim id="idle_climb" path="face" frames="1-3" delay="0.08"/>
        faceSprite.Add("idle_climb", "face", 0.08f, 1, 2, 3);
        // <Anim id="climb_idle" path="face" frames="2,1,0" delay="0.08" goto="idle"/>
        faceSprite.Add("climb_idle", "face", 0.08f, "idle", 2, 1, 0);

        // <Anim id="dashed" path="face" frames="4" delay="0.5" goto="dash"/>
        faceSprite.Add("dashed", "face", 0.5f, "dash", 4);
        // <Loop id="dash" path="face" frames="5,6" delay="0.08"/>
        faceSprite.AddLoop("dash", "face", 0.08f, 5, 6);

        // <Anim id="retract" path="face" frames="7-9" delay="0.08"/>
        faceSprite.Add("retract", "face", 0.08f, 7, 8, 9);
        // <Anim id="end_retract" path="face" frames="10-12" delay="0.08" goto="idle_alt"/>
        faceSprite.Add("end_retract", "face", 0.08f, "idle_alt", 10, 11, 12);

        // <Loop id="idle_alt" path="face" frames="12" delay="0.08"/>
        faceSprite.AddLoop("idle_alt", "face", 0.08f, 12);
        // <Loop id="climb_alt" path="face" frames="13" delay="0.08"/>
        faceSprite.AddLoop("climb_alt", "face", 0.08f, 13);

        // <Anim id="horrified" path="face" frames="14,15,16,15,16,15,16,15,16,15,16,15,16,17,18,0" delay="0.06"/>
        faceSprite.Add("horrified", "face", 0.06f, 14, 15, 16, 15, 16, 15, 16, 15, 16, 15, 16, 15, 16, 17, 18, 0);

        faceSprite.JustifyOrigin(0.5f, 0.5f);
        faceSprite.Play("idle");
        return faceSprite;
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
        if (this.overClocked)
            this.face.Rate = 2;
        ChangeColor(dashColor);
        StartShaking(this.overClocked ? 0.1875f : 0.375f);
        Audio.Play(this.overClocked ? CustomSFX.game_lilly_OCdashed : CustomSFX.game_lilly_dashed, this.Center);
        yield return this.overClocked ? 0.25f : 0.5f;

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
                rightArm.UpdateArm(moveAmount, this.armsGiveLiftSpeed);

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
                leftArm.UpdateArm(moveAmount, this.armsGiveLiftSpeed);

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
        ChangeColor(retractColor);

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
                rightArm.UpdateArm(move, this.armsGiveLiftSpeed);
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
                leftArm.UpdateArm(move, this.armsGiveLiftSpeed);
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
        ChangeColor(idleAltColor);

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
            ChangeColor(horrifiedColor);
        }

        if (this.Activated || this.faceState == FaceState.Horrified)
            return;

        bool ridden = HasPlayerRider();
        if (ridden && this.faceState == FaceState.Idle)
        {
            this.faceState = FaceState.ClimbedOn;
            this.face.Play("idle_climb");
            ChangeColor(climbedOnColor);
        }
        else if (!ridden && this.faceState == FaceState.ClimbedOn)
        {
            this.faceState = FaceState.Idle;
            this.face.Play("climb_idle");
            ChangeColor(idleColor);
        }
        else if (ridden && this.faceState == FaceState.IdleAlt)
        {
            this.faceState = FaceState.ClimbedOnAlt;
            this.face.Play("climb_alt");
            ChangeColor(climbedOnAltColor);
        }
        else if (!ridden && this.faceState == FaceState.ClimbedOnAlt)
        {
            this.faceState = FaceState.IdleAlt;
            this.face.Play("idle_alt");
            ChangeColor(idleAltColor);
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

    public void InitializeTextures (string path)
    {
        MTexture block00 = GFX.Game[path + (this.overClocked ? "/OCblock00" : "/block00")];
        MTexture block01 = GFX.Game[path + (this.overClocked ? "/OCblock01" : "/block01")];
        MTexture active_block00 = GFX.Game[path + (this.overClocked ? "/OCactive_block00" : "/active_block00")];
        MTexture active_block01 = GFX.Game[path + (this.overClocked ? "/OCactive_block01" : "/active_block01")];
        MTexture armend = GFX.Game[path + "/armend"];

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
