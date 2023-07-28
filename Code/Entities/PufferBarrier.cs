using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;

namespace Celeste.Mod.VortexHelper.Entities;

[CustomEntity("VortexHelper/PufferBarrier")]
[Tracked(false)]
public class PufferBarrier : Solid
{
    private float solidifyDelay;

    private static readonly Color P_Color = Color.Lerp(Color.Orange, Color.White, 0.5f);
    public float Flash;
    public bool Flashing;

    private readonly List<Vector2> particles = new();
    private readonly float[] speeds = new float[3] { 12f, 20f, 40f };

    private readonly List<PufferBarrier> adjacent = new();

    public PufferBarrier(Vector2 position, float width, float height)
        : base(position, width, height, safe: false)
    {
        this.Collidable = false;

        for (int i = 0; i < this.Width * this.Height / 16f; i++)
            this.particles.Add(new Vector2(Calc.Random.NextFloat(this.Width - 1f), Calc.Random.NextFloat(this.Height - 1f) - this.Height));
    }

    public PufferBarrier(EntityData data, Vector2 offset)
        : this(data.Position + offset, data.Width, data.Height)
    { }

    public override void Added(Scene scene)
    {
        base.Added(scene);
        scene.Tracker.GetEntity<PufferBarrierRenderer>().Track(this);
    }

    public override void Removed(Scene scene)
    {
        base.Removed(scene);
        scene.Tracker.GetEntity<PufferBarrierRenderer>().Untrack(this);
    }

    public override void Update()
    {
        if (this.Flashing)
        {
            this.Flash = Calc.Approach(this.Flash, 0f, Engine.DeltaTime * 4f);
            if (this.Flash <= 0f)
                this.Flashing = false;
        }
        else if (this.solidifyDelay > 0f)
            this.solidifyDelay -= Engine.DeltaTime;

        int speedsCount = this.speeds.Length;
        float height = this.Height;

        int i = 0;
        for (int count = this.particles.Count; i < count; i++)
        {
            Vector2 value = this.particles[i] - Vector2.UnitY * this.speeds[i % speedsCount] * Engine.DeltaTime;
            value.Y %= height - 1;
            this.particles[i] = value;
        }
        base.Update();
    }

    public void OnTouchPuffer()
    {
        this.Flash = 1f;
        this.solidifyDelay = 1f;
        this.Flashing = true;

        this.Scene.CollideInto(new Rectangle((int) this.X, (int) this.Y - 2, (int) this.Width, (int) this.Height + 4), this.adjacent);
        this.Scene.CollideInto(new Rectangle((int) this.X - 2, (int) this.Y, (int) this.Width + 4, (int) this.Height), this.adjacent);

        foreach (PufferBarrier item in this.adjacent)
            if (!item.Flashing)
                item.OnTouchPuffer();

        this.adjacent.Clear();
    }

    public override void Render()
    {
        Vector2 v = Vector2.UnitY * (this.Height - 1);

        foreach (Vector2 particle in this.particles)
            Draw.Pixel.Draw(this.Position + particle + v, Vector2.Zero, P_Color);

        if (this.Flashing)
            Draw.Rect(this.Collider, Color.White * this.Flash * 0.5f);
    }
}
