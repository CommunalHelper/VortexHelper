using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;

namespace Celeste.Mod.VortexHelper.Entities {
    [CustomEntity("VortexHelper/PufferBarrier")]
    [Tracked(false)]
    public class PufferBarrier : Solid {

        private float solidifyDelay;

        private static readonly Color P_Color = Color.Lerp(Color.Orange, Color.White, 0.5f);
        public float Flash;
        public bool Flashing;

        private List<Vector2> particles = new List<Vector2>();

        private List<PufferBarrier> adjacent = new List<PufferBarrier>();

        private float[] speeds = new float[3] { 12f, 20f, 40f };

        public PufferBarrier(Vector2 position, float width, float height)
            : base(position, width, height, safe: false) {
            Collidable = false;
            for (int i = 0; i < Width * Height / 16f; i++) {
                particles.Add(new Vector2(Calc.Random.NextFloat(Width - 1f), Calc.Random.NextFloat(Height - 1f) - Height));
            }
        }

        public PufferBarrier(EntityData data, Vector2 offset)
            : this(data.Position + offset, data.Width, data.Height) {
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            scene.Tracker.GetEntity<PufferBarrierRenderer>().Track(this);
        }

        public override void Removed(Scene scene) {
            base.Removed(scene);
            scene.Tracker.GetEntity<PufferBarrierRenderer>().Untrack(this);
        }

        public override void Update() {
            if (Flashing) {
                Flash = Calc.Approach(Flash, 0f, Engine.DeltaTime * 4f);
                if (Flash <= 0f) {
                    Flashing = false;
                }
            } else if (solidifyDelay > 0f) {
                solidifyDelay -= Engine.DeltaTime;
            }

            int speedsCount = speeds.Length;
            float height = Height;

            int i = 0;
            for (int count = particles.Count; i < count; i++) {
                Vector2 value = particles[i] - Vector2.UnitY * speeds[i % speedsCount] * Engine.DeltaTime;
                value.Y %= height - 1;
                particles[i] = value;
            }
            base.Update();
        }

        public void OnTouchPuffer() {
            Flash = 1f;
            solidifyDelay = 1f;
            Flashing = true;

            Scene.CollideInto(new Rectangle((int)X, (int)Y - 2, (int)Width, (int)Height + 4), adjacent);
            Scene.CollideInto(new Rectangle((int)X - 2, (int)Y, (int)Width + 4, (int)Height), adjacent);

            foreach (PufferBarrier item in adjacent) {
                if (!item.Flashing) {
                    item.OnTouchPuffer();
                }
            }

            adjacent.Clear();
        }

        public override void Render() {
            Vector2 v = Vector2.UnitY * (Height - 1);

            foreach (Vector2 particle in particles)
                Draw.Pixel.Draw(Position + particle + v, Vector2.Zero, P_Color);

            if (Flashing) {
                Draw.Rect(Collider, Color.White * Flash * 0.5f);
            }
        }
    }
}
