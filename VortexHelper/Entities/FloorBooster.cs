using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;

namespace Celeste.Mod.VortexHelper.Entities
{
    [CustomEntity("VortexHelper/FloorBooster")]
    [Tracked(false)]
    class FloorBooster : Entity
    {
        public Facings Facing;
        private Vector2 imageOffset;
        private SoundSource idleSfx;
        private SoundSource activateSfx;

        private bool isPlaying = false;

        private List<Sprite> tiles;

        public bool IceMode;
        private bool notCoreMode;
        public bool NoRefillsOnIce;

        public Color EnabledColor = Color.White;
        public Color DisabledColor = Color.Lerp(Color.White, Color.Black, 0.5f);

        public int MoveSpeed;

        public FloorBooster(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Width, data.Bool("left"), data.Int("speed"), data.Bool("iceMode"), data.Bool("noRefillOnIce"), data.Bool("notAttached"))
        { }

        public FloorBooster(Vector2 position, int width, bool left, int speed, bool iceMode, bool noRefillOnIce, bool notAttached)
            : base(position)
        {
            base.Tag = Tags.TransitionUpdate;
            base.Depth = 1999;
            NoRefillsOnIce = noRefillOnIce;
            this.notCoreMode = iceMode;
            IceMode = iceMode;
            MoveSpeed = (int)Calc.Max(0, speed);
            Facing = left ? Facings.Left : Facings.Right;

            base.Collider = new Hitbox(width, 3, 0, 5);
            if(!notCoreMode) Add(new CoreModeListener(OnChangeMode));
            Add(idleSfx = new SoundSource());
            Add(activateSfx = new SoundSource());

            if (!notAttached) // sure why not i guess
            {
                Add(new StaticMover
                {
                    OnShake = OnShake,
                    SolidChecker = IsRiding,
                    JumpThruChecker = IsRiding,
                    OnEnable = OnEnable,
                    OnDisable = OnDisable
                });
            }

            tiles = BuildSprite(left);
        }

        public void SetColor(Color color)
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
            if(!IceMode) idleSfx.Play("event:/env/local/09_core/conveyor_idle");
            Active = Collidable = true;
            SetColor(EnabledColor);
        }

        private void OnDisable()
        {
            idleSfx.Stop();
            PlayActivateSfx(true);
            Active = Collidable = false;
            SetColor(DisabledColor);
        }

        private List<Sprite> BuildSprite(bool left)
        {
            List<Sprite> list = new List<Sprite>();
            for (int i = 0; i < base.Width; i += 8)
            {
                // Sprite Selection
                string id = 
                    (i == 0) ? (!left ? "Left" : "Right") : 
                    ((!((i + 16) > base.Width)) ? "Mid" : 
                    (!left ? "Right" : "Left"));

                Sprite sprite = VortexHelperModule.FloorBoosterSpriteBank.Create("FloorBooster" + id);
                if (!left) sprite.FlipX = true;
                sprite.Position = new Vector2(i, 0);
                list.Add(sprite);
                Add(sprite);
            }
            return list;
        }

        private void OnChangeMode(Session.CoreModes mode)
        {
            IceMode = (mode == Session.CoreModes.Cold);
            tiles.ForEach(delegate (Sprite t)
            {
                t.Play(IceMode ? "ice" : "hot");
            });
            if (IceMode)
            {
                idleSfx.Stop();
            }
            else if (!idleSfx.Playing)
            {
                idleSfx.Play("event:/env/local/09_core/conveyor_idle");
            }
        }

        private bool IsRiding(JumpThru jumpThru)
        {
            return CollideCheckOutside(jumpThru, Position + Vector2.UnitY);
        }
        private bool IsRiding(Solid solid)
        {
            return CollideCheckOutside(solid, Position + Vector2.UnitY);
        }
        private void OnShake(Vector2 amount)
        {
            imageOffset += amount;
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            Session.CoreModes mode = IceMode ? Session.CoreModes.Cold : Session.CoreModes.Hot;
            Session.CoreModes levelCoreMode = SceneAs<Level>().CoreMode;
            if (levelCoreMode == Session.CoreModes.Cold && !notCoreMode)
            {
                mode = levelCoreMode;
            }
            OnChangeMode(mode);
        }

        public override void Update()
        {
            Player player = base.Scene.Tracker.GetEntity<Player>();
            PositionSfx(player);
            if (!(base.Scene as Level).Transitioning)
            {
                bool isUsed = false;
                base.Update();
                if(player != null)
                {
                    if (CollideCheck(player) && player.OnGround() && player.Bottom <= Bottom) isUsed = true;
                }
                PlayActivateSfx(IceMode ? true : !isUsed);
            }
        }

        private void PlayActivateSfx(bool end)
        {
            if ((isPlaying && !end) || (!isPlaying && end)) return;
            isPlaying = !end;
            if (end)
            {
                activateSfx.Param("end", 1f);
            } else
            {
                activateSfx.Play("event:/game/09_core/conveyor_activate", "end", 0f);
            }
        }

        public override void Render()
        {
            Vector2 position = Position; // Shake Offset
            Position += imageOffset;
            base.Render();
            Position = position;
        }

        private void PositionSfx(Player entity)
        {
            if (entity != null)
            {
                idleSfx.Position = Calc.ClosestPointOnLine(Position, Position + new Vector2(base.Width, 0f), entity.Center) - Position;
                idleSfx.Position.Y += 7;
                activateSfx.Position = idleSfx.Position;
                idleSfx.UpdateSfxPosition(); activateSfx.UpdateSfxPosition();
            }
        }
    }
}