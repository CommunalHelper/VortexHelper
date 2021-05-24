using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;

namespace Celeste.Mod.VortexHelper.Entities {
    [Tracked(false)]
    public class PufferBarrierRenderer : Entity {
        private class Edge {
            public PufferBarrier Parent;

            public bool Visible;

            public Vector2 A;

            public Vector2 B;

            public Vector2 Min;

            public Vector2 Max;

            public Vector2 Normal;

            public Vector2 Perpendicular;

            public float[] Wave;

            public float Length;

            public Edge(PufferBarrier parent, Vector2 a, Vector2 b) {
                Parent = parent;
                Visible = true;
                A = a;
                B = b;
                Min = new Vector2(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));
                Max = new Vector2(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
                Normal = (b - a).SafeNormalize();
                Perpendicular = -Normal.Perpendicular();
                Length = (a - b).Length();
            }

            public void UpdateWave(float time) {
                if (Wave == null || Wave.Length <= Length) {
                    Wave = new float[(int)Length + 2];
                }
                for (int i = 0; i <= Length; i++) {
                    Wave[i] = GetWaveAt(time, i, Length);
                }
            }

            private float GetWaveAt(float offset, float along, float length) {
                if (along <= 1f || along >= length - 1f) {
                    return 0f;
                }
                float num = offset + along * 0.25f;
                float num2 = (float)(Math.Sin(num) * 2.0 + Math.Sin(num * 0.25f));
                return (1f + num2 * Ease.SineInOut(Calc.YoYo(along / length)));
            }

            public bool InView(ref Rectangle view) {
                if (view.Left < Parent.X + Max.X && view.Right > Parent.X + Min.X && view.Top < Parent.Y + Max.Y) {
                    return view.Bottom > Parent.Y + Min.Y;
                }
                return false;
            }
        }

        private List<PufferBarrier> list = new List<PufferBarrier>();

        private List<Edge> edges = new List<Edge>();

        private VirtualMap<bool> tiles;

        private Rectangle levelTileBounds;

        private bool dirty;

        public PufferBarrierRenderer() {
            base.Tag = (Tags.Global | Tags.TransitionUpdate);
            base.Depth = 0;
            Add(new CustomBloom(OnRenderBloom));
        }

        public void Track(PufferBarrier block) {
            list.Add(block);
            if (tiles == null) {
                levelTileBounds = (base.Scene as Level).TileBounds;
                tiles = new VirtualMap<bool>(levelTileBounds.Width, levelTileBounds.Height, emptyValue: false);
            }
            for (int i = (int)block.X / 8; i < block.Right / 8f; i++) {
                for (int j = (int)block.Y / 8; j < block.Bottom / 8f; j++) {
                    tiles[i - levelTileBounds.X, j - levelTileBounds.Y] = true;
                }
            }
            dirty = true;
        }

        public void Untrack(PufferBarrier block) {
            list.Remove(block);
            if (list.Count <= 0) {
                tiles = null;
            }
            else {
                for (int i = (int)block.X / 8; i < block.Right / 8f; i++) {
                    for (int j = (int)block.Y / 8; j < block.Bottom / 8f; j++) {
                        tiles[i - levelTileBounds.X, j - levelTileBounds.Y] = false;
                    }
                }
            }
            dirty = true;
        }

        public override void Update() {
            if (dirty) {
                RebuildEdges();
            }
            UpdateEdges();
        }

        public void UpdateEdges() {
            Camera camera = (base.Scene as Level).Camera;
            Rectangle view = new Rectangle((int)camera.Left - 4, (int)camera.Top - 4, (int)(camera.Right - camera.Left) + 8, (int)(camera.Bottom - camera.Top) + 8);
            for (int i = 0; i < edges.Count; i++) {
                if (edges[i].Visible) {
                    if (base.Scene.OnInterval(0.25f, i * 0.01f) && !edges[i].InView(ref view)) {
                        edges[i].Visible = false;
                    }
                }
                else if (base.Scene.OnInterval(0.05f, i * 0.01f) && edges[i].InView(ref view)) {
                    edges[i].Visible = true;
                }
                if (edges[i].Visible && (base.Scene.OnInterval(0.05f, i * 0.01f) || edges[i].Wave == null)) {
                    edges[i].UpdateWave(base.Scene.TimeActive * 3f);
                }
            }
        }

        private void RebuildEdges() {
            dirty = false;
            edges.Clear();
            if (list.Count > 0) {
                Level obj = base.Scene as Level;
                _ = obj.TileBounds.Left;
                _ = obj.TileBounds.Top;
                _ = obj.TileBounds.Right;
                _ = obj.TileBounds.Bottom;
                Point[] array = new Point[4]
                {
                new Point(0, -1),
                new Point(0, 1),
                new Point(-1, 0),
                new Point(1, 0)
                };
                foreach (PufferBarrier item in list) {
                    for (int i = (int)item.X / 8; i < item.Right / 8f; i++) {
                        for (int j = (int)item.Y / 8; j < item.Bottom / 8f; j++) {
                            Point[] array2 = array;
                            for (int k = 0; k < array2.Length; k++) {
                                Point point = array2[k];
                                Point point2 = new Point(-point.Y, point.X);
                                if (!Inside(i + point.X, j + point.Y) && (!Inside(i - point2.X, j - point2.Y) || Inside(i + point.X - point2.X, j + point.Y - point2.Y))) {
                                    Point point3 = new Point(i, j);
                                    Point point4 = new Point(i + point2.X, j + point2.Y);
                                    Vector2 value = new Vector2(4f) + new Vector2(point.X - point2.X, point.Y - point2.Y) * 4f;
                                    while (Inside(point4.X, point4.Y) && !Inside(point4.X + point.X, point4.Y + point.Y)) {
                                        point4.X += point2.X;
                                        point4.Y += point2.Y;
                                    }
                                    Vector2 a = new Vector2(point3.X, point3.Y) * 8f + value - item.Position;
                                    Vector2 b = new Vector2(point4.X, point4.Y) * 8f + value - item.Position;
                                    edges.Add(new Edge(item, a, b));
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool Inside(int tx, int ty) {
            return tiles[tx - levelTileBounds.X, ty - levelTileBounds.Y];
        }

        private void OnRenderBloom() {
            Camera camera = (base.Scene as Level).Camera;
            new Rectangle((int)camera.Left, (int)camera.Top, (int)(camera.Right - camera.Left), (int)(camera.Bottom - camera.Top));
            foreach (PufferBarrier item in list) {
                if (item.Visible) {
                    Draw.Rect(item.X, item.Y, item.Width, item.Height, Color.White);
                }
            }
            foreach (Edge edge in edges) {
                if (edge.Visible) {
                    Vector2 value = edge.Parent.Position + edge.A;
                    _ = edge.Parent.Position + edge.B;
                    for (int i = 0; i <= edge.Length; i++) {
                        Vector2 vector = value + edge.Normal * i;
                        Draw.Line(vector, vector + edge.Perpendicular * edge.Wave[i], Color.White);
                    }
                }
            }
        }

        public override void Render() {
            if (list.Count > 0) {
                Color color = Color.Orange * .45f;
                Color value = Color.Orange * .3f;
                foreach (PufferBarrier item in list) {
                    if (item.Visible) {
                        Draw.Rect(item.Collider, color);
                    }
                }
                if (edges.Count > 0) {
                    foreach (Edge edge in edges) {
                        if (edge.Visible) {
                            Vector2 value2 = edge.Parent.Position + edge.A;
                            _ = edge.Parent.Position + edge.B;
                            Color.Lerp(value, Color.White, edge.Parent.Flash);
                            for (int i = 0; i <= edge.Length; i++) {
                                Vector2 vector = value2 + edge.Normal * i;
                                Draw.Line(vector, vector + edge.Perpendicular * edge.Wave[i], color);
                            }
                        }
                    }
                }
            }
        }
    }
}
