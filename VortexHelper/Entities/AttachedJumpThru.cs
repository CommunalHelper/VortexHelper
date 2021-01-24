using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.VortexHelper.Entities
{
    [CustomEntity("VortexHelper/AttachedJumpThru")]
    [Tracked(false)]
    class AttachedJumpThru : JumpThru
    {
        private int columns;
        private int overrideSoundIndex = -1;
        private string overrideTexture;
        private StaticMover staticMover;
        private Platform Platform;
        private Vector2 imageOffset;

        public Color EnabledColor = Color.White;
        public Color DisabledColor = Color.White;
        public bool VisibleWhenDisabled;

        public AttachedJumpThru(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Width, data.Attr("texture", "default"), data.Int("surfaceIndex", -1))
        { }

        public AttachedJumpThru(Vector2 position, int width, string overrideTexture, int overrideSoundIndex = -1)
            : base(position, width, safe: false)
        {
            columns = width / 8;
            base.Depth = -60;
            this.overrideTexture = overrideTexture;
            this.overrideSoundIndex = overrideSoundIndex;
            staticMover = new StaticMover();
            staticMover.OnAttach = delegate (Platform p)
            {
                base.Depth = p.Depth + 1;
            };
            Add(new StaticMover
            {
                OnMove = OnMove,
                OnShake = OnShake,
                SolidChecker = IsRiding,
                OnEnable = OnEnable,
                OnDisable = OnDisable
            });
        }
        public override void Awake(Scene scene)
        {
            base.Awake(scene);
            AreaData areaData = AreaData.Get(scene);
            string jumpthru = areaData.Jumpthru;
            areaData.Jumpthru = jumpthru;
            if (!string.IsNullOrEmpty(overrideTexture) && !overrideTexture.Equals("default"))
            {
                areaData.Jumpthru = overrideTexture;
            }
            if (overrideSoundIndex > 0)
            {
                SurfaceSoundIndex = overrideSoundIndex;
            }
            else
            {
                switch (jumpthru.ToLower())
                {
                    case "dream":
                        SurfaceSoundIndex = 32;
                        break;
                    case "temple":
                    case "templeb":
                        SurfaceSoundIndex = 8;
                        break;
                    case "core":
                        SurfaceSoundIndex = 3;
                        break;
                    default:
                        SurfaceSoundIndex = 5;
                        break;
                }
            }
            MTexture mTexture = GFX.Game["objects/jumpthru/" + areaData.Jumpthru];
            int num = mTexture.Width / 8;
            for (int i = 0; i < columns; i++)
            {
                int num2;
                int num3;
                if (i == 0)
                {
                    num2 = 0;
                    num3 = (!CollideCheck<Solid>(Position + new Vector2(-1f, 0f))) ? 1 : 0;
                }
                else if (i == columns - 1)
                {
                    num2 = num - 1;
                    num3 = (!CollideCheck<Solid>(Position + new Vector2(1f, 0f))) ? 1 : 0;
                }
                else
                {
                    num2 = 1 + Calc.Random.Next(num - 2);
                    num3 = Calc.Random.Choose(0, 1);
                }
                Image image = new Image(mTexture.GetSubtexture(num2 * 8, num3 * 8, 8, 8));
                image.X = i * 8;
                Add(image);
            }
            foreach (StaticMover component in scene.Tracker.GetComponents<StaticMover>())
            {
                if (component.IsRiding(this) && component.Platform == null)
                {
                    staticMovers.Add(component);
                    component.Platform = this;
                    if (component.OnAttach != null)
                    {
                        component.OnAttach(this);
                    }
                }
            }
        }
        private bool IsRiding(Solid solid)
        {
            if (CollideCheck(solid, Position + Vector2.UnitX) || CollideCheck(solid, Position - Vector2.UnitX))
            {
                staticMover.Platform = Platform = solid;
                if (VisibleWhenDisabled = solid is CassetteBlock || solid is SwitchBlock)
                {
                    DisabledColor = Color.Lerp(Color.Black, Color.White, .4f);
                }
                return true;
            }
            else return false;
        }
        public override void Render()
        {
            Vector2 position = Position;
            Position += imageOffset;
            base.Render();
            Position = position;
        }
        private void OnMove(Vector2 amount)
        {
            float moveH = amount.X;
            float moveV = amount.Y;

            if (Platform is MoveBlock && new DynData<MoveBlock>(Platform as MoveBlock).Get<float>("speed") == 0f)
            {
                MoveHNaive(moveH);
                MoveVNaive(moveV);
            }

            else
            {
                MoveH(moveH);
                MoveV(moveV);
            }
        }
        public override void OnShake(Vector2 amount)
        {
            imageOffset += amount;
            ShakeStaticMovers(amount);
        }
        private void OnDisable()
        {
            Active = (Collidable = false);
            DisableStaticMovers();
            if (VisibleWhenDisabled)
            {
                SetColor(DisabledColor);
            }
            else
            {
                Visible = false;
            }
        }

        private void SetColor(Color color)
        {
            foreach (Component component in base.Components)
            {
                Image image = component as Image;
                if (image != null)
                {
                    image.Color = color;
                }
            }
        }

        private void OnEnable()
        {
            EnableStaticMovers();
            Active = (Visible = (Collidable = true));
            SetColor(Color.White);
        }
        public override void OnStaticMoverTrigger(StaticMover sm)
        {
            TriggerPlatform();
        }

        public override void Update()
        {
            base.Update();
            Player playerRider = GetPlayerRider();
            if (playerRider != null && playerRider.Speed.Y >= 0f)
            {
                TriggerPlatform();
            }
        }
        private void TriggerPlatform()
        {
            if (Platform != null)
            {
                Platform.OnStaticMoverTrigger(staticMover);
            }
        }
    }
}