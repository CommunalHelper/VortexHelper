using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections;

namespace Celeste.Mod.VortexHelper.Entities
{
    [CustomEntity("VortexHelper/DashBubble")]
    [Tracked(false)]
    class DashBubble : Entity
    {
        private const float RespawnTime = 3f;
        private Image sprite;
        private string spriteState;
        private bool spiked;
        private bool singleUse;
        private bool wobble;
        private Level level;
        private Wiggler wiggler;
        private Wiggler moveWiggle;
        private Vector2 moveWiggleDir;
        private float respawnTimer;
        
        public DashBubble(Vector2 position, bool spiked, bool singleUse, bool wobble)
            : base(position)
        {
            spriteState = "idle";
            if (spiked) spriteState = "spiked";
            this.spiked = spiked;
            this.singleUse = singleUse;
            base.Collider = new Hitbox(20f, 20f, -10f, -10f);
            Add(new PlayerCollider(OnPlayer));
            Add(sprite = new Image(GFX.Game["objects/VortexHelper/dashBubble/" + spriteState + "00"]));
            sprite.CenterOrigin();
            Add(wiggler = Wiggler.Create(1f, 4f, delegate (float v)
            {
                sprite.Scale = Vector2.One * (1f + v * 0.2f);
            }));
            moveWiggle = Wiggler.Create(0.8f, 2f);
            moveWiggle.StartZero = true;
            Add(moveWiggle);
            if(wobble) UpdateY();
        }
        public DashBubble(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Bool("spiked"), data.Bool("singleUse"), data.Bool("wobble"))
        { }
        public override void Added(Scene scene)
        {
            base.Added(scene);
            level = SceneAs<Level>();
        }

        public override void Update()
        {
            base.Update();
            if (respawnTimer > 0f)
            {
                respawnTimer -= Engine.DeltaTime;
                if (respawnTimer <= 0f)
                {
                    Respawn();
                }
            }
            if(wobble) UpdateY();
        }
        private void Respawn()
        {
            if (!Collidable)
            {
                Collidable = true;
                sprite.Visible = true;
                wiggler.Start();
                Audio.Play("event:/game/04_cliffside/greenbooster_reappear", Position);
            }
        }
        private void OnPlayer(Player player)
        {
            Vector2 speed = player.Speed;
            if (!player.DashAttacking)
            {
                player.PointBounce(base.Center);
                moveWiggle.Start();
                moveWiggleDir = (base.Center - player.Center).SafeNormalize(Vector2.UnitY);
                Audio.Play("event:/game/06_reflection/feather_bubble_bounce", Position);
                Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                if (spiked) player.Die((player.Position - Position).SafeNormalize());
                return;
            }
            Audio.Play("event:/game/05_mirror_temple/redbooster_end");
            sprite.Visible = false;
            Collidable = false;
            if (!singleUse)
            {
                respawnTimer = 3f;
            }
        }
            private void UpdateY()
        {
            sprite.X = 0f;
            sprite.Position += moveWiggleDir * moveWiggle.Value * -8f;
        }
    }
}
