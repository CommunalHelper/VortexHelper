using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;

namespace Celeste.Mod.VortexHelper.Entities
{
	[CustomEntity("VortexHelper/SwitchBlock")]
	[Tracked(false)]
    class SwitchBlock : Solid
    {
		private class BoxSide : Entity
		{
			private SwitchBlock block;
			private Color color;

			public BoxSide(SwitchBlock block, Color color)
			{
				this.block = block;
				this.color = color;
			}

			public override void Render()
			{
				Draw.Rect(block.X, block.Y + block.Height - 8f, block.Width, 8 + block.blockHeight, color);
			}
		}

		private bool Activated;
		private VortexHelperSession.SwitchBlockColor SwitchBlockColor;
		private int Index;
		private Color color;

		private LightOcclude occluder;
		private Wiggler wiggler;
		private Vector2 wigglerScaler;

		private List<SwitchBlock> group;
		private bool groupLeader;
		private Vector2 groupOrigin;

		private List<Image> pressed = new List<Image>();
		private List<Image> solid = new List<Image>();
		private List<Image> all = new List<Image>();

		private int blockHeight = 2;

		public SwitchBlock(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Width, data.Height, data.Int("index", 0))
        { }
         
        public SwitchBlock(Vector2 position, int width, int height, int index)
            : base(position, width, height, true)
        {
            SurfaceSoundIndex = 35; // Aww.
            Index = index;

			switch (Index)
			{
				default:
				case 0:
					SwitchBlockColor = VortexHelperSession.SwitchBlockColor.Blue;
					color = Calc.HexToColor("3232ff");
					break;
				case 1:
					SwitchBlockColor = VortexHelperSession.SwitchBlockColor.Rose;
					color = Calc.HexToColor("ff3265");
					break;
				case 2:
					SwitchBlockColor = VortexHelperSession.SwitchBlockColor.Orange;
					color = Calc.HexToColor("ff9532");
					break;
				case 3:
					SwitchBlockColor = VortexHelperSession.SwitchBlockColor.Lime;
					color = Calc.HexToColor("9cff32");
					break;
			}
			Activated = Collidable = SwitchBlockColor == VortexHelperModule.SessionProperties.switchBlockColor;

			Add(occluder = new LightOcclude());
		}

		public override void Awake(Scene scene)
		{
			base.Awake(scene);
			Color color = Calc.HexToColor("667da5");
			Color disabledColor = new Color(color.R / 255f * (this.color.R / 255f), color.G / 255f * (this.color.G / 255f), color.B / 255f * (this.color.B / 255f), 1f);

			foreach (StaticMover staticMover in staticMovers)
			{
				if (staticMover.Entity is Spikes spikes)
				{
					spikes.EnabledColor = this.color;
					spikes.DisabledColor = disabledColor;
					spikes.VisibleWhenDisabled = true;
					spikes.SetSpikeColor(this.color);
				}
				if (staticMover.Entity is Spring spring)
				{
					spring.DisabledColor = disabledColor;
					spring.VisibleWhenDisabled = true;
				}
				if (staticMover.Entity is FloorBooster floorBooster) // hello there
				{
					floorBooster.EnabledColor = this.color;
					floorBooster.DisabledColor = disabledColor;
					floorBooster.SetColor(this.color);
				}
			}

			if (group == null)
			{
				groupLeader = true;
				group = new List<SwitchBlock>();
				group.Add(this);
				FindInGroup(this);
				float num = float.MaxValue;
				float num2 = float.MinValue;
				float num3 = float.MaxValue;
				float num4 = float.MinValue;
				foreach (SwitchBlock item in group)
				{
					if (item.Left < num)
					{
						num = item.Left;
					}
					if (item.Right > num2)
					{
						num2 = item.Right;
					}
					if (item.Bottom > num4)
					{
						num4 = item.Bottom;
					}
					if (item.Top < num3)
					{
						num3 = item.Top;
					}
				}
				groupOrigin = new Vector2((int)(num + (num2 - num) / 2f), (int)num4);
				wigglerScaler = new Vector2(Calc.ClampedMap(num2 - num, 32f, 96f, 1f, 0.2f), Calc.ClampedMap(num4 - num3, 32f, 96f, 1f, 0.2f));
				Add(wiggler = Wiggler.Create(0.3f, 3f));
				foreach (SwitchBlock item2 in group)
				{
					item2.wiggler = wiggler;
					item2.wigglerScaler = wigglerScaler;
					item2.groupOrigin = groupOrigin;
				}
			}

			foreach (StaticMover staticMover2 in staticMovers)
			{
				(staticMover2.Entity as Spikes)?.SetOrigins(groupOrigin);
			}

			string idx;
			switch (Index)
			{
				default:
				case 0:
					idx = "blue";
					break;
				case 1:
					idx = "red";
					break;
				case 2:
					idx = "orange";
					break;
				case 3:
					idx = "green";
					break;
			}

			// oh, boy.
			for (float num5 = base.Left; num5 < base.Right; num5 += 8f)
			{
				for (float num6 = base.Top; num6 < base.Bottom; num6 += 8f)
				{
					bool flag = CheckForSame(num5 - 8f, num6);
					bool flag2 = CheckForSame(num5 + 8f, num6);
					bool flag3 = CheckForSame(num5, num6 - 8f);
					bool flag4 = CheckForSame(num5, num6 + 8f);
					if (flag && flag2 && flag3 && flag4)
					{
						if (!CheckForSame(num5 + 8f, num6 - 8f))
						{
							SetImage(num5, num6, 3, 0, idx);
						}
						else if (!CheckForSame(num5 - 8f, num6 - 8f))
						{
							SetImage(num5, num6, 3, 1, idx);
						}
						else if (!CheckForSame(num5 + 8f, num6 + 8f))
						{
							SetImage(num5, num6, 3, 2, idx);
						}
						else if (!CheckForSame(num5 - 8f, num6 + 8f))
						{
							SetImage(num5, num6, 3, 3, idx);
						}
						else
						{
							SetImage(num5, num6, 1, 1, idx);
						}
					}
					else if (flag && flag2 && !flag3 && flag4)
					{
						SetImage(num5, num6, 1, 0, idx);
					}
					else if (flag && flag2 && flag3 && !flag4)
					{
						SetImage(num5, num6, 1, 2, idx);
					}
					else if (flag && !flag2 && flag3 && flag4)
					{
						SetImage(num5, num6, 2, 1, idx);
					}
					else if (!flag && flag2 && flag3 && flag4)
					{
						SetImage(num5, num6, 0, 1, idx);
					}
					else if (flag && !flag2 && !flag3 && flag4)
					{
						SetImage(num5, num6, 2, 0, idx);
					}
					else if (!flag && flag2 && !flag3 && flag4)
					{
						SetImage(num5, num6, 0, 0, idx);
					}
					else if (flag && !flag2 && flag3 && !flag4)
					{
						SetImage(num5, num6, 2, 2, idx);
					}
					else if (!flag && flag2 && flag3 && !flag4)
					{
						SetImage(num5, num6, 0, 2, idx);
					}
				}
			}
			if (!Activated) DisableStaticMovers();
			UpdateVisualState();
		}

		private void FindInGroup(SwitchBlock block)
		{
			foreach (SwitchBlock entity in base.Scene.Tracker.GetEntities<SwitchBlock>())
			{
				if (entity != this && entity != block && entity.Index == Index 
					&& (entity.CollideRect(new Rectangle((int)block.X - 1, (int)block.Y, (int)block.Width + 2, (int)block.Height)) 
					|| entity.CollideRect(new Rectangle((int)block.X, (int)block.Y - 1, (int)block.Width, (int)block.Height + 2))) 
					&& !group.Contains(entity)) // recursion, beeg ending condition
				{
					group.Add(entity);
					FindInGroup(entity);
					entity.group = group;
				}
			}
		}

		private bool CheckForSame(float x, float y)
		{
			foreach (SwitchBlock entity in base.Scene.Tracker.GetEntities<SwitchBlock>())
			{
				if (entity.Index == Index && entity.Collider.Collide(new Rectangle((int)x, (int)y, 8, 8)))
				{
					return true;
				}
			}
			return false;
		}

		private void SetImage(float x, float y, int tx, int ty, string idx)
		{
			pressed.Add(CreateImage(x, y, tx, ty, GFX.Game["objects/VortexHelper/onoff/outline_" + idx]));
			solid.Add(CreateImage(x, y, tx, ty, GFX.Game["objects/VortexHelper/onoff/solid"]));
		}

		private Image CreateImage(float x, float y, int tx, int ty, MTexture tex)
		{
			Vector2 value = new Vector2(x - base.X, y - base.Y);
			Image image = new Image(tex.GetSubtexture(tx * 8, ty * 8, 8, 8));
			Vector2 vector = groupOrigin - Position;
			image.Origin = vector - value;
			image.Position = vector;
			image.Color = color;
			Add(image);
			all.Add(image);
			return image;
		}

		private void UpdateVisualState()
		{
			base.Depth = Collidable ? -10 : 9880;
			foreach (StaticMover staticMover in staticMovers)
			{
				staticMover.Entity.Depth = base.Depth + 1;
			}
			occluder.Visible = Collidable;
			foreach (Image item in solid)
			{
				item.Visible = Collidable;
			}
			foreach (Image item2 in pressed)
			{
				item2.Visible = !Collidable;
			}
			if (groupLeader)
			{
				Vector2 scale = new Vector2(1f + wiggler.Value * 0.05f * wigglerScaler.X, 1f + wiggler.Value * 0.15f * wigglerScaler.Y);
				foreach (SwitchBlock item3 in group)
				{
					foreach (Image item4 in item3.all)
					{
						item4.Scale = scale;
					}
					foreach (StaticMover staticMover2 in item3.staticMovers)
					{
						if (staticMover2.Entity is Spikes spikes)
						{
							foreach (Component component in spikes.Components)
							{
								if (component is Image image)
								{
									image.Scale = scale;
								}
							}
						}
					}
				}
			}
		}

		private bool CheckPlayerSafe()
		{
			Player player = Scene.Tracker.GetEntity<Player>();
			if (player == null) return true;

			foreach (SwitchBlock block in group)
			{
				if (block.CollideCheck(player)) return false;
			}

			return true;
		}

		public override void Update()
		{
			base.Update();
			Activated = SwitchBlockColor == VortexHelperModule.SessionProperties.switchBlockColor;

			if (groupLeader && Activated && !Collidable)
			{
				bool isPlayerSafe = CheckPlayerSafe();
				if (isPlayerSafe)
				{
					foreach (SwitchBlock item2 in group)
					{
						item2.Collidable = true;
						item2.EnableStaticMovers();
					}
					wiggler.Start();
				}
			}
			else if (!Activated && Collidable)
			{
				Collidable = false;
				DisableStaticMovers();
			}
			UpdateVisualState();
		}

		public static bool RoomHasSwitchBlock(Scene scene, VortexHelperSession.SwitchBlockColor targetColor)
		{
			foreach(SwitchBlock switchBlock in scene.Tracker.GetEntities<SwitchBlock>())
			{
				if (switchBlock.SwitchBlockColor == targetColor) return true;
			}
			return false;
		}
	} 
}
