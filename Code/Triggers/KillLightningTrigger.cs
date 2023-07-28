using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.VortexHelper.Triggers;

[CustomEntity("VortexHelper/KillLightningTrigger")]
public class KillLightningTrigger : Trigger
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
        if (this.permanent && (this.Scene as Level).Session.GetFlag("disable_lightning"))
            RemoveSelf();
    }

    public override void OnEnter(Player player)
    {
        base.OnEnter(player);
        Audio.Play(CustomSFX.game_killLightningTrigger_break, this.Center);
        Break();
        Input.Rumble(RumbleStrength.Strong, RumbleLength.Long);
    }

    private void Break()
    {
        Session session = (this.Scene as Level).Session;
        if (this.permanent)
            session.SetFlag("disable_lightning");
        RumbleTrigger.ManuallyTrigger(this.Center.X, 1.2f);
        this.Tag = Tags.Persistent;
        Add(new Coroutine(Lightning.RemoveRoutine(SceneAs<Level>(), RemoveSelf)));
    }
}
