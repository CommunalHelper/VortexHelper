using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.VortexHelper.Entities;

[Tracked(false)]
public class PufferBarrierRenderer : Entity
{
    private class Edge
    {
        public PufferBarrier Parent;

        public Vector2 A;
        public Vector2 B;
        public Vector2 Min;
        public Vector2 Max;

        public Vector2 Normal;
        public Vector2 Perpendicular;

        public float[] Wave;
        public float Length;
        public bool Visible;

        public Edge(PufferBarrier parent, Vector2 a, Vector2 b)
        {
            this.Parent = parent;

            this.A = a;
            this.B = b;
            this.Min = new Vector2(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));
            this.Max = new Vector2(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));

            this.Normal = (b - a).SafeNormalize();
            this.Perpendicular = -this.Normal.Perpendicular();
            this.Length = (a - b).Length();
            this.Visible = true;
        }

        public void UpdateWave(float time)
        {
            if (this.Wave is null || this.Wave.Length <= this.Length)
                this.Wave = new float[(int) this.Length + 2];

            for (int i = 0; i <= this.Length; i++)
                this.Wave[i] = GetWaveAt(time, i, this.Length);
        }

        private float GetWaveAt(float offset, float along, float length)
        {
            if (along <= 1f || along >= length - 1f)
            {
                return 0f;
            }
            float time = offset + along * 0.25f;
            float sine = (float) (Math.Sin(time) * 2.0 + Math.Sin(time * 0.25f));

            return 1f + sine * Ease.SineInOut(Calc.YoYo(along / length));
        }

        public bool InView(ref Rectangle view)
            => view.Left < this.Parent.X + this.Max.X
            && view.Right > this.Parent.X + this.Min.X
            && view.Top < this.Parent.Y + this.Max.Y
            && view.Bottom > this.Parent.Y + this.Min.Y;
    }

    private readonly List<PufferBarrier> list = new();
    private readonly List<Edge> edges = new();

    private VirtualMap<bool> tiles;
    private Rectangle levelTileBounds;

    private bool dirty;

    public PufferBarrierRenderer()
    {
        this.Tag = Tags.Global | Tags.TransitionUpdate;
        this.Depth = Depths.Player;
        Add(new CustomBloom(OnRenderBloom));
    }

    public void Track(PufferBarrier block)
    {
        this.list.Add(block);
        if (this.tiles is null)
        {
            this.levelTileBounds = (this.Scene as Level).TileBounds;
            this.tiles = new VirtualMap<bool>(this.levelTileBounds.Width, this.levelTileBounds.Height, emptyValue: false);
        }

        for (int i = (int) block.X / 8; i < block.Right / 8f; i++)
            for (int j = (int) block.Y / 8; j < block.Bottom / 8f; j++)
                this.tiles[i - this.levelTileBounds.X, j - this.levelTileBounds.Y] = true;

        this.dirty = true;
    }

    public void Untrack(PufferBarrier block)
    {
        this.list.Remove(block);

        if (this.list.Count <= 0)
            this.tiles = null;
        else
        {
            for (int i = (int) block.X / 8; i < block.Right / 8f; i++)
                for (int j = (int) block.Y / 8; j < block.Bottom / 8f; j++)
                    this.tiles[i - this.levelTileBounds.X, j - this.levelTileBounds.Y] = false;
        }

        this.dirty = true;
    }

    public override void Update()
    {
        if (this.dirty)
            RebuildEdges();
        UpdateEdges();
    }

    public void UpdateEdges()
    {
        Camera camera = SceneAs<Level>().Camera;
        var view = new Rectangle((int) camera.Left - 4, (int) camera.Top - 4, (int) (camera.Right - camera.Left) + 8, (int) (camera.Bottom - camera.Top) + 8);
        for (int i = 0; i < this.edges.Count; i++)
        {
            if (this.edges[i].Visible)
            {
                if (this.Scene.OnInterval(0.25f, i * 0.01f) && !this.edges[i].InView(ref view))
                    this.edges[i].Visible = false;
            }
            else if (this.Scene.OnInterval(0.05f, i * 0.01f) && this.edges[i].InView(ref view))
                this.edges[i].Visible = true;

            if (this.edges[i].Visible && (this.Scene.OnInterval(0.05f, i * 0.01f) || this.edges[i].Wave is null))
                this.edges[i].UpdateWave(this.Scene.TimeActive * 3f);
        }
    }

    private void RebuildEdges()
    {
        this.dirty = false;
        this.edges.Clear();

        if (this.list.Count > 0)
        {
            var array = new Point[4] { new Point(0, -1), new Point(0, 1), new Point(-1, 0), new Point(1, 0) };

            foreach (PufferBarrier item in this.list)
            {
                for (int i = (int) item.X / 8; i < item.Right / 8f; i++)
                {
                    for (int j = (int) item.Y / 8; j < item.Bottom / 8f; j++)
                    {
                        Point[] array2 = array;

                        for (int k = 0; k < array2.Length; k++)
                        {
                            Point point = array2[k];
                            var point2 = new Point(-point.Y, point.X);

                            if (!Inside(i + point.X, j + point.Y) && (!Inside(i - point2.X, j - point2.Y) || Inside(i + point.X - point2.X, j + point.Y - point2.Y)))
                            {
                                var point3 = new Point(i, j);
                                var point4 = new Point(i + point2.X, j + point2.Y);

                                Vector2 value = new Vector2(4f) + new Vector2(point.X - point2.X, point.Y - point2.Y) * 4f;
                                while (Inside(point4.X, point4.Y) && !Inside(point4.X + point.X, point4.Y + point.Y))
                                {
                                    point4.X += point2.X;
                                    point4.Y += point2.Y;
                                }

                                Vector2 a = new Vector2(point3.X, point3.Y) * 8f + value - item.Position;
                                Vector2 b = new Vector2(point4.X, point4.Y) * 8f + value - item.Position;

                                this.edges.Add(new Edge(item, a, b));
                            }
                        }
                    }
                }
            }
        }
    }

    private bool Inside(int tx, int ty) => this.tiles[tx - this.levelTileBounds.X, ty - this.levelTileBounds.Y];

    private void OnRenderBloom()
    {
        foreach (PufferBarrier item in this.list)
            if (item.Visible)
                Draw.Rect(item.X, item.Y, item.Width, item.Height, Color.White);

        foreach (Edge edge in this.edges)
        {
            if (edge.Visible)
            {
                Vector2 value = edge.Parent.Position + edge.A;
                for (int i = 0; i <= edge.Length; i++)
                {
                    Vector2 vector = value + edge.Normal * i;
                    Draw.Line(vector, vector + edge.Perpendicular * edge.Wave[i], Color.White);
                }
            }
        }
    }

    public override void Render()
    {
        if (this.list.Count > 0)
        {
            Color color = Color.Orange * .45f;
            Color value = Color.Orange * .3f;

            foreach (PufferBarrier item in this.list)
                if (item.Visible)
                    Draw.Rect(item.Collider, color);

            if (this.edges.Count > 0)
            {
                foreach (Edge edge in this.edges)
                {
                    if (edge.Visible)
                    {
                        Vector2 value2 = edge.Parent.Position + edge.A;
                        Color.Lerp(value, Color.White, edge.Parent.Flash);
                        for (int i = 0; i <= edge.Length; i++)
                        {
                            Vector2 vector = value2 + edge.Normal * i;
                            Draw.Line(vector, vector + edge.Perpendicular * edge.Wave[i], color);
                        }
                    }
                }
            }
        }
    }

    public static class Hooks
    {
        public static void Hook()
        {
            On.Celeste.LevelLoader.LoadingThread += LevelLoader_LoadingThread;
        }

        public static void Unhook()
        {
            On.Celeste.LevelLoader.LoadingThread -= LevelLoader_LoadingThread;
        }

        private static void LevelLoader_LoadingThread(On.Celeste.LevelLoader.orig_LoadingThread orig, LevelLoader self)
        {
            // Allows for PufferBarrier entities to be rendered with just one PufferBarrierRenderer.
            self.Level.Add(new PufferBarrierRenderer());
            orig(self);
        }
    }
}
