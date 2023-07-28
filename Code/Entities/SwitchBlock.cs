using Celeste.Mod.Entities;
using Celeste.Mod.VortexHelper.Misc.Extensions;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;

namespace Celeste.Mod.VortexHelper.Entities;

[CustomEntity("VortexHelper/SwitchBlock")]
[Tracked(false)]
public class SwitchBlock : Solid
{
    private class BoxSide : Entity
    {
        private readonly SwitchBlock block;
        private Color color;

        public BoxSide(SwitchBlock block, Color color)
        {
            this.block = block;
            this.color = color;
        }

        public override void Render()
            => Draw.Rect(
                this.block.X,
                this.block.Y + this.block.Height - 8f,
                this.block.Width, 8 + this.block.blockHeight,
                this.color
            );
    }

    private bool Activated;
    private readonly VortexHelperSession.SwitchBlockColor switchBlockColor;
    private readonly int index;
    private readonly Color color;

    private readonly LightOcclude occluder;
    private Wiggler wiggler;
    private Vector2 wigglerScaler;

    private List<SwitchBlock> group;
    private bool groupLeader;
    private Vector2 groupOrigin;

    private readonly List<Image> pressed = new();
    private readonly List<Image> solid = new();
    private readonly List<Image> all = new();

    private readonly int blockHeight = 2;

    public SwitchBlock(EntityData data, Vector2 offset)
        : this(data.Position + offset, data.Width, data.Height, data.Int("index", 0)) { }

    public SwitchBlock(Vector2 position, int width, int height, int index)
        : base(position, width, height, true)
    {
        this.SurfaceSoundIndex = SurfaceIndex.CassetteBlock;
        this.index = index;

        this.switchBlockColor = this.index switch
        {
            1 => VortexHelperSession.SwitchBlockColor.Rose,
            2 => VortexHelperSession.SwitchBlockColor.Orange,
            3 => VortexHelperSession.SwitchBlockColor.Lime,
            _ => VortexHelperSession.SwitchBlockColor.Blue,
        };

        this.color = this.switchBlockColor.GetColor();
        this.Activated = this.Collidable = this.switchBlockColor.IsActive();

        Add(this.occluder = new LightOcclude());
    }

    public override void Awake(Scene scene)
    {
        base.Awake(scene);
        Color color = Calc.HexToColor("667da5");
        var disabledColor = new Color(color.R / 255f * (this.color.R / 255f), color.G / 255f * (this.color.G / 255f), color.B / 255f * (this.color.B / 255f), 1f);

        foreach (StaticMover staticMover in this.staticMovers)
        {
            switch (staticMover.Entity)
            {
                case Spikes e:
                {
                    e.EnabledColor = this.color;
                    e.DisabledColor = disabledColor;
                    e.VisibleWhenDisabled = true;
                    e.SetSpikeColor(this.color);
                    break;
                }

                case Spring e:
                {
                    e.DisabledColor = disabledColor;
                    e.VisibleWhenDisabled = true;
                    break;
                }

                case FloorBooster e:
                {
                    e.EnabledColor = this.color;
                    e.DisabledColor = disabledColor;
                    e.SetColor(this.color);
                    break;
                }

                default:
                    break;
            }
        }

        if (this.group is null)
        {
            this.groupLeader = true;
            this.group = new List<SwitchBlock> {
                this
            };
            FindInGroup(this);

            float groupLeft = float.MaxValue;
            float groupRight = float.MinValue;
            float groupTop = float.MaxValue;
            float groupBottom = float.MinValue;
            foreach (SwitchBlock item in this.group)
            {
                if (item.Left < groupLeft)
                    groupLeft = item.Left;
                if (item.Right > groupRight)
                    groupRight = item.Right;
                if (item.Bottom > groupBottom)
                    groupBottom = item.Bottom;
                if (item.Top < groupTop)
                    groupTop = item.Top;
            }

            this.groupOrigin = new Vector2((int) (groupLeft + (groupRight - groupLeft) / 2f), (int) groupBottom);
            this.wigglerScaler = new Vector2(Calc.ClampedMap(groupRight - groupLeft, 32f, 96f, 1f, 0.2f), Calc.ClampedMap(groupBottom - groupTop, 32f, 96f, 1f, 0.2f));
            Add(this.wiggler = Wiggler.Create(0.3f, 3f));
            foreach (SwitchBlock block in this.group)
            {
                block.wiggler = this.wiggler;
                block.wigglerScaler = this.wigglerScaler;
                block.groupOrigin = this.groupOrigin;
            }
        }

        foreach (StaticMover staticMover2 in this.staticMovers)
            (staticMover2.Entity as Spikes)?.SetOrigins(this.groupOrigin);

        string idx = this.index switch
        {
            1 => "red",
            2 => "orange",
            3 => "green",
            _ => "blue",
        };

        // cassette block autotiling
        for (float x = this.Left; x < this.Right; x += 8f)
        {
            for (float y = this.Top; y < this.Bottom; y += 8f)
            {
                bool flag1 = CheckForSame(x - 8f, y);
                bool flag2 = CheckForSame(x + 8f, y);
                bool flag3 = CheckForSame(x, y - 8f);
                bool flag4 = CheckForSame(x, y + 8f);
                
                if (flag1 && flag2 && flag3 && flag4)
                {
                    if (!CheckForSame(x + 8f, y - 8f))
                        SetImage(x, y, 3, 0, idx);
                    else if (!CheckForSame(x - 8f, y - 8f))
                        SetImage(x, y, 3, 1, idx);
                    else if (!CheckForSame(x + 8f, y + 8f))
                        SetImage(x, y, 3, 2, idx);
                    else if (!CheckForSame(x - 8f, y + 8f))
                        SetImage(x, y, 3, 3, idx);
                    else
                        SetImage(x, y, 1, 1, idx);
                }
                else if (flag1 && flag2 && !flag3 && flag4)
                    SetImage(x, y, 1, 0, idx);
                else if (flag1 && flag2 && flag3 && !flag4)
                    SetImage(x, y, 1, 2, idx);
                else if (flag1 && !flag2 && flag3 && flag4)
                    SetImage(x, y, 2, 1, idx);
                else if (!flag1 && flag2 && flag3 && flag4)
                    SetImage(x, y, 0, 1, idx);
                else if (flag1 && !flag2 && !flag3 && flag4)
                    SetImage(x, y, 2, 0, idx);
                else if (!flag1 && flag2 && !flag3 && flag4)
                    SetImage(x, y, 0, 0, idx);
                else if (flag1 && !flag2 && flag3 && !flag4)
                    SetImage(x, y, 2, 2, idx);
                else if (!flag1 && flag2 && flag3 && !flag4)
                    SetImage(x, y, 0, 2, idx);
            }
        }

        if (!this.Activated)
            DisableStaticMovers();

        UpdateVisualState();
    }

    private void FindInGroup(SwitchBlock block)
    {
        foreach (SwitchBlock entity in this.Scene.Tracker.GetEntities<SwitchBlock>())
        {
            if (entity != this && entity != block && entity.index == this.index &&
                (entity.CollideRect(new Rectangle((int) block.X - 1, (int) block.Y, (int) block.Width + 2, (int) block.Height)) ||
                entity.CollideRect(new Rectangle((int) block.X, (int) block.Y - 1, (int) block.Width, (int) block.Height + 2))) &&
                !this.group.Contains(entity))
            {
                this.group.Add(entity);
                FindInGroup(entity);
                entity.group = this.group;
            }
        }
    }

    private bool CheckForSame(float x, float y)
    {
        foreach (SwitchBlock entity in this.Scene.Tracker.GetEntities<SwitchBlock>())
            if (entity.index == this.index && entity.Collider.Collide(new Rectangle((int) x, (int) y, 8, 8)))
                return true;
        return false;
    }

    private void SetImage(float x, float y, int tx, int ty, string idx)
    {
        this.pressed.Add(CreateImage(x, y, tx, ty, GFX.Game["objects/VortexHelper/onoff/outline_" + idx]));
        this.solid.Add(CreateImage(x, y, tx, ty, GFX.Game["objects/VortexHelper/onoff/solid"]));
    }

    private Image CreateImage(float x, float y, int tx, int ty, MTexture tex)
    {
        var value = new Vector2(x - this.X, y - this.Y);
        Vector2 vector = this.groupOrigin - this.Position;

        var image = new Image(tex.GetSubtexture(tx * 8, ty * 8, 8, 8))
        {
            Origin = vector - value,
            Position = vector,
            Color = color
        };

        Add(image);
        this.all.Add(image);
        return image;
    }

    private void UpdateVisualState()
    {
        this.Depth = this.Collidable ? Depths.Player - 10 : 9880;

        foreach (StaticMover staticMover in this.staticMovers)
            staticMover.Entity.Depth = this.Depth + 1;

        this.occluder.Visible = this.Collidable;

        foreach (Image image in this.solid)
            image.Visible = this.Collidable;
        foreach (Image image in this.pressed)
            image.Visible = !this.Collidable;

        if (this.groupLeader)
        {
            var scale = new Vector2(1f + this.wiggler.Value * 0.05f * this.wigglerScaler.X, 1f + this.wiggler.Value * 0.15f * this.wigglerScaler.Y);
            foreach (SwitchBlock item3 in this.group)
            {
                foreach (Image img in item3.all)
                    img.Scale = scale;

                foreach (StaticMover staticMover2 in item3.staticMovers)
                    if (staticMover2.Entity is Spikes spikes)
                        foreach (Component component in spikes.Components)
                            if (component is Image image)
                                image.Scale = scale;
            }
        }
    }

    private bool CheckPlayerSafe()
    {
        Player player = this.Scene.Tracker.GetEntity<Player>();
        if (player is null)
            return true;

        foreach (SwitchBlock block in this.group)
            if (block.CollideCheck(player))
                return false;

        return true;
    }

    public override void Update()
    {
        base.Update();
        this.Activated = this.switchBlockColor.IsActive();

        if (this.groupLeader && this.Activated && !this.Collidable)
        {
            bool isPlayerSafe = CheckPlayerSafe();
            if (isPlayerSafe)
            {
                foreach (SwitchBlock item2 in this.group)
                {
                    item2.Collidable = true;
                    item2.EnableStaticMovers();
                }
                this.wiggler.Start();
            }
        }
        else if (!this.Activated && this.Collidable)
        {
            this.Collidable = false;
            DisableStaticMovers();
        }

        UpdateVisualState();
    }

    public static bool RoomHasSwitchBlock(Scene scene, VortexHelperSession.SwitchBlockColor targetColor)
    {
        foreach (SwitchBlock switchBlock in scene.Tracker.GetEntities<SwitchBlock>())
            if (switchBlock.switchBlockColor == targetColor)
                return true;
        return false;
    }
}
