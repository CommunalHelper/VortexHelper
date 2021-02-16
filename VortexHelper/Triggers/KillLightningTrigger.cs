using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VortexHelper;

namespace Celeste.Mod.VortexHelper.Triggers
{
    [CustomEntity("VortexHelper/KillLightningTrigger")]
    class KillLightningTrigger : Trigger
    {
        private bool permanent;

        public KillLightningTrigger(EntityData data, Vector2 offset)
            : base(data, offset)
        {
            this.permanent = data.Bool("permanent");
        }

        public override void Awake(Scene scene)
        {
            base.Awake(scene);
            if (permanent && (base.Scene as Level).Session.GetFlag("disable_lightning"))
            {
                RemoveSelf();
            }
        }

        public override void OnEnter(Player player)
        {
            base.OnEnter(player);
            Audio.Play(CustomSFX.game_killLightningTrigger_break, Center);
			Break();
            Input.Rumble(RumbleStrength.Strong, RumbleLength.Long);
        }

		private void Break()
		{
			Session session = (base.Scene as Level).Session;
            if (permanent)
            {
                session.SetFlag("disable_lightning");
            }
            RumbleTrigger.ManuallyTrigger(base.Center.X, 1.2f);
			base.Tag = Tags.Persistent;
			Add(new Coroutine(Lightning.RemoveRoutine(SceneAs<Level>(), base.RemoveSelf)));
		}
	}
}
