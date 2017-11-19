using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemiurgeLib.Common
{
    public class TreeNode<T> : IEnumerable<TreeNode<T>>
    {
        public T value;
        public TreeNode<T> parent;
        public HashSet<TreeNode<T>> children;

        private int size, depth, forkRank;
        private bool sizeDirty, depthDirty, forkRankDirty;

        public TreeNode(T value, TreeNode<T> parent = null)
        {
            this.value = value;
            this.children = new HashSet<TreeNode<T>>();

            SetParent(parent);
            MarkDirty();
        }

        protected void MarkDirty()
        {
            sizeDirty = true;
            depthDirty = true;
            forkRankDirty = true;

            if (this.parent != null)
            {
                this.parent.MarkDirty();
            }
        }

        public void SetParent(TreeNode<T> newParent)
        {
            if (this.parent != null)
            {
                this.parent.children.Remove(this);
                this.parent.MarkDirty();
            }

            this.parent = newParent;

            if (this.parent != null)
            {
                this.parent.children.Add(this);
                this.parent.MarkDirty();
            }
        }

        public int Size()
        {
            if (this.sizeDirty)
            {
                this.size = 1 + this.children.Sum(node => node.Size());
                this.sizeDirty = false;
            }
            return this.size;
        }

        public int Depth()
        {
            if (this.depthDirty)
            {
                this.depth = this.children.Count == 0 ? 0 : 1 + this.children.Select(node => node.Depth()).Max();
                this.depthDirty = false;
            }
            return this.depth;
        }

        public int ForkRank()
        {
            if (this.forkRankDirty)
            {
                this.forkRank = this.children.Count <= 1 ? 0 : this.children.Select(node => node.Depth()).Min();
                this.forkRankDirty = false;
            }
            return this.forkRank;
            
        }

        // TODO: this is WILDLY inefficient without caching results.  Cache results or eliminate this method altogether.
        public int MaxForkRank()
        {
            return this.children.Count == 0 ? 0 : Math.Max(ForkRank(), this.children.Select(node => node.MaxForkRank()).Max());
        }

        public T GetDeepestValue()
        {
            int depth = 0;
            return GetDeepestValue(ref depth);
        }

        private T GetDeepestValue(ref int depthOfRet)
        {
            int depthOfVal = depthOfRet + 1;
            T ret = this.value, val;

            foreach (var child in this.children)
            {
                val = child.GetDeepestValue(ref depthOfVal);
                if (depthOfVal > depthOfRet)
                {
                    depthOfRet = depthOfVal;
                    ret = val;
                }
            }

            return ret;
        }

        public void Prune(int prunableSubtreeSize)
        {
            if (this.Size() < prunableSubtreeSize)
            {
                // Base case: if this subtree is too small, cut itself off from the tree.
                this.SetParent(null);
            }
            else
            {
                List<TreeNode<T>> toPrune = this.children.ToList();
                foreach (var child in toPrune)
                {
                    child.Prune(prunableSubtreeSize);
                }
            }
        }
        
        public IEnumerator<TreeNode<T>> GetEnumerator()
        {
            // Don't do this recursively, as doing so is ludicrously
            // inefficient in terms of allocations.
            Queue<TreeNode<T>> nodes = new Queue<TreeNode<T>>();
            nodes.Enqueue(this);

            while (nodes.Count > 0)
            {
                TreeNode<T> node = nodes.Dequeue();
                yield return node;

                foreach (var child in node.children)
                {
                    nodes.Enqueue(child);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public interface ITraversalFunctor
        {
            void TraversalStarted(TreeNode<T> node);
            void TraversalFinished();
        }

        public void Traverse<TFunctor>() where TFunctor : ITraversalFunctor, new()
        {
            TFunctor functor = new TFunctor();
            functor.TraversalStarted(this);

            foreach (var child in this.children)
            {
                child.Traverse<TFunctor>();
            }

            functor.TraversalFinished();
        }
    }

    public class Utils
    {
        public static Point2d? Bfs(Point2d start, Func<Point2d, bool> isInDomain, Func<Point2d, bool> isSatisfactory)
        {
            PointSet2d alreadyIncluded = new PointSet2d();
            alreadyIncluded.Add(start);

            Point2d[] neighbors = new Point2d[8];

            Queue<Point2d> searchSpace = new Queue<Point2d>();
            searchSpace.Enqueue(start);

            while (searchSpace.Count > 0)
            {
                var current = searchSpace.Dequeue();

                SetNeighbors(current, ref neighbors);
                for (int idx = 0; idx < neighbors.Length; idx++)
                {
                    Point2d pt = neighbors[idx];
                    if (isInDomain(pt) && !alreadyIncluded.Contains(pt))
                    {
                        if (isSatisfactory(pt))
                        {
                            return pt;
                        }
                        else
                        {
                            searchSpace.Enqueue(pt);
                            alreadyIncluded.Add(pt);
                        }
                    }
                }
            }

            return null;
        }

        public static TreeNode<Point2d> MakeBfsTree(Point2d rootPt, Func<Point2d, bool> isInDomain)
        {
            TreeNode<Point2d> root = new TreeNode<Point2d>(rootPt);

            PointSet2d alreadyIncluded = new PointSet2d();
            alreadyIncluded.Add(root.value);

            Point2d[] neighbors = new Point2d[8];

            Queue<TreeNode<Point2d>> searchSpace = new Queue<TreeNode<Point2d>>();
            searchSpace.Enqueue(root);

            while (searchSpace.Count > 0)
            {
                var node = searchSpace.Dequeue();

                SetNeighbors(node.value, ref neighbors);
                for (int idx = 0; idx < neighbors.Length; idx++)
                {
                    Point2d pt = neighbors[idx];
                    if (isInDomain(pt) && !alreadyIncluded.Contains(pt))
                    {
                        searchSpace.Enqueue(new TreeNode<Point2d>(pt, node));
                        alreadyIncluded.Add(pt);
                    }
                }
            }

            return root;
        }

        public static void SetNeighbors(Point2d center, ref Point2d[] neighbors)
        {
            // Set the eight neighbor positions, clockwise.
            neighbors[0].x = center.x - 1; neighbors[0].y = center.y - 1;
            neighbors[1].x = center.x + 0; neighbors[1].y = center.y - 1;
            neighbors[2].x = center.x + 1; neighbors[2].y = center.y - 1;
            neighbors[3].x = center.x + 1; neighbors[3].y = center.y + 0;
            neighbors[4].x = center.x + 1; neighbors[4].y = center.y + 1;
            neighbors[5].x = center.x + 0; neighbors[5].y = center.y + 1;
            neighbors[6].x = center.x - 1; neighbors[6].y = center.y + 1;
            neighbors[7].x = center.x - 1; neighbors[7].y = center.y + 0;
        }

        public class FieldFromBitmap : Field2d<float>
        {
            public FieldFromBitmap(Bitmap bmp) : base(bmp.Width, bmp.Height)
            {
                for (int x = 0, y = 0; y < this.Height; y += ++x / this.Width, x %= this.Width)
                {
                    this[y, x] = bmp.GetPixel(x, y).GetBrightness();
                }
            }
        }

        public class FieldFromPreciseBitmap : Field2d<float>
        {
            public FieldFromPreciseBitmap(Bitmap bmp) : base(GetPrototypeField(bmp)) { }

            private static IField2d<float> GetPrototypeField(Bitmap bmp)
            {
                return new FunctionField<float>(bmp.Width, bmp.Height, (x, y) =>
                {
                    Color c = bmp.GetPixel(x, y);
                    return 1000 * c.R + 10 * c.G + 0.1f * c.B;
                });
            }
        }

        public class StreamedChunkedPreciseHeightField : ChunkField<float>
        {
            private Func<int, int, Chunk?> LoaderFunction { get; }

            public StreamedChunkedPreciseHeightField(int width, int height, int cacheSize, Func<int, int, Chunk?> loaderFunction) : base(width, height, cacheSize)
            {
                this.LoaderFunction = loaderFunction;
            }

            protected override Chunk? GetChunkForPosition(int x, int y)
            {
                var baseChunk = base.GetChunkForPosition(x, y);

                if (baseChunk.HasValue)
                {
                    return baseChunk;
                }
                else
                {
                    var chunk = LoaderFunction(x, y);
                    if (chunk.HasValue)
                    {
                        this.TryAddChunk(chunk.Value.MinPoint.X, chunk.Value.MinPoint.Y, chunk.Value.Field);
                    }
                }

                baseChunk = base.GetChunkForPosition(x, y);
                this.CompressToCache();
                return baseChunk;
            }
        }

        public class ImageServer
        {
            private Dictionary<Rectangle, (int, int, string)> images = new Dictionary<Rectangle, (int, int, string)>();

            public void AddImage(string imagePath)
            {
                string actualName = Path.GetFileNameWithoutExtension(imagePath);
                int[] ulCoords = actualName.Remove(0, "submap_".Length).Split('_').Select(t => int.Parse(t)).ToArray();

                int ulX = ulCoords[0] * 512 / 32;
                int ulY = ulCoords[1] * 512 / 32;

                this.images.Add(new Rectangle(ulX, ulY, 512, 512), (ulX, ulY, imagePath));
            }

            public (int x, int y, string path) TryGetPathForPoint(int x, int y)
            {
                foreach (var pair in this.images)
                {
                    if (pair.Key.Contains(x, y))
                    {
                        return pair.Value;
                    }
                }

                return (-1, -1, null);
            }

            public IEnumerable<(int x, int y, string path)> GetImages()
            {
                return this.images.Values;
            }
        }

        public static Color Lerp(Color from, Color to, float t)
        {
            t = Math.Max(0, Math.Min(t, 1f));
            return Color.FromArgb(
                (int)(from.A * (1f - t) + to.A * t),
                (int)(from.R * (1f - t) + to.R * t),
                (int)(from.G * (1f - t) + to.G * t),
                (int)(from.B * (1f - t) + to.B * t)
                );
        }

        #region Output
        public static void OutputField(IField2d<float> field, Bitmap bmp, string filename)
        {
            int w = Math.Min(field.Width, bmp.Width);
            int h = Math.Min(field.Height, bmp.Height);

            for (int x = 0, y = 0; y < h; y += ++x / w, x %= w)
            {
                int value = Math.Max(0, Math.Min((int)(255 * field[y, x]), 255));
                bmp.SetPixel(x, y, Color.FromArgb(value, value, value));
            }
            bmp.Save(filename);
        }

        public static void OutputAsTributaryMap(
            Dictionary<HydrologicalField.LandType, HashSet<PointSet2d>> contiguousSets,
            List<TreeNode<TreeNode<Point2d>>> riverSystems,
            DrainageField drainageField,
            Bitmap bmp,
            string outputFile)
        {
            System.Random rand = new System.Random();
            foreach (var hs in contiguousSets.Values)
            {
                foreach (var ps in hs)
                {
                    Color color = Color.FromArgb(rand.Next(192), rand.Next(192), rand.Next(192));
                    foreach (Point2d p in ps)
                    {
                        bmp.SetPixel(p.x, p.y, color);
                    }
                }
            }
            foreach (var river in riverSystems)
            {
                river.IterateAllSubtrees().Iterate(iterator =>
                {
                    Color color = Color.FromArgb(rand.Next(192), rand.Next(192), rand.Next(192));
                    iterator.Iterate(node =>
                    {
                        Point2d p = node.value;
                        bmp.SetPixel(p.x, p.y, color);
                    });
                });
            }
            foreach (var landmass in contiguousSets[HydrologicalField.LandType.Land])
            {
                foreach (Point2d p in landmass)
                {
                    Point2d drain = drainageField[p.y, p.x];
                    Color c = bmp.GetPixel(drain.x, drain.y);
                    c = Color.FromArgb(c.R + 64, c.G + 64, c.B + 64);
                    bmp.SetPixel(p.x, p.y, c);
                }
            }
            bmp.Save(outputFile);
        }

        public static void SerializeMap(HydrologicalField hydro, WaterTableField wtf, long seed, string outputPath)
        {
            using (var binFile = System.IO.File.OpenWrite(outputPath + "serialized.bin"))
            using (var binWriter = new System.IO.BinaryWriter(binFile))
            using (var sumFile = System.IO.File.OpenWrite(outputPath + "summarized.txt"))
            using (var sumWriter = new System.IO.StreamWriter(sumFile))
            {
                int[,] idAssociations = new int[wtf.Height, wtf.Width];
                int id;

                // Version
                binWriter.Write(1);
                sumWriter.WriteLine("Version 1");

                // Seed
                binWriter.Write(seed);
                sumWriter.WriteLine("Seed: " + seed);

                // Impose order
                var oceans = wtf.GeographicFeatures[HydrologicalField.LandType.Ocean].ToArray();
                id = 0;
                // Number of oceans
                binWriter.Write(oceans.Length);
                sumWriter.WriteLine("Total oceans: " + oceans.Length);
                // For each ocean
                foreach (var ocean in oceans)
                {
                    // Size
                    binWriter.Write(ocean.Count);
                    sumWriter.WriteLine("\tOcean " + id + " size: " + ocean.Count);

                    foreach (Point2d pt in ocean)
                    {
                        idAssociations[pt.y, pt.x] = id;
                    }

                    id++;
                }

                // Impose order
                var landmasses = wtf.GeographicFeatures[HydrologicalField.LandType.Land].ToArray();
                id = 0;
                // Number of landmasses
                binWriter.Write(landmasses.Length);
                sumWriter.WriteLine("Total landmasses: " + landmasses.Length);
                // For each landmass
                foreach (var landmass in landmasses)
                {
                    // Size
                    binWriter.Write(landmass.Count);
                    sumWriter.WriteLine("\tLandmass " + id + " size: " + landmass.Count);

                    foreach (Point2d pt in landmass)
                    {
                        idAssociations[pt.y, pt.x] = id;
                    }

                    id++;
                }

                // Total number of rivers
                binWriter.Write(wtf.RiverSystems.Sum(system => system.Size()));
                sumWriter.WriteLine("Total rivers: " + wtf.RiverSystems.Sum(system => system.Size()));

                // Prepare to output rivers.
                Queue<Tuple<TreeNode<TreeNode<Point2d>>, int>> riversAndParentIds = new Queue<Tuple<TreeNode<TreeNode<Point2d>>, int>>();
                id = 0;
                foreach (var system in wtf.RiverSystems)
                {
                    riversAndParentIds.Enqueue(new Tuple<TreeNode<TreeNode<Point2d>>, int>(system, -1));
                }

                // For each river system
                while (riversAndParentIds.Count > 0)
                {
                    var pair = riversAndParentIds.Dequeue();
                    var riverSystem = pair.Item1;
                    var parentId = pair.Item2;

                    // Size
                    binWriter.Write(riverSystem.value.Size());

                    // Depth
                    binWriter.Write(riverSystem.value.Depth());

                    // Parent
                    binWriter.Write(parentId);
                    sumWriter.WriteLine("\tRiver " + id + " of size " + riverSystem.value.Size() + " and depth " + riverSystem.value.Depth() + " and parent " + parentId);

                    foreach (var child in riverSystem.children)
                    {
                        riversAndParentIds.Enqueue(new Tuple<TreeNode<TreeNode<Point2d>>, int>(child, id));
                    }

                    riverSystem.IteratePrimarySubtree().Iterate(node => idAssociations[node.value.y, node.value.x] = id);

                    id++;
                }

                // Map dimensions
                binWriter.Write(wtf.Width);
                binWriter.Write(wtf.Height);

                // For each pixel
                for (int x = 0, y = 0; y < wtf.Height; y += ++x / wtf.Width, x %= wtf.Width)
                {
                    // Water table height
                    binWriter.Write(wtf[y, x]);

                    // Hydrological type
                    binWriter.Write((int)hydro[y, x]);

                    // Feature id
                    binWriter.Write(idAssociations[y, x]);

                    // Drain
                    var drain = wtf.DrainageField[y, x];
                    binWriter.Write(drain.x);
                    binWriter.Write(drain.y);
                }
            }
        }

        private static float MaxValue(IField2d<float> field)
        {
            float maxValue = float.NegativeInfinity;
            for (int y = 0; y < field.Height; y++)
            {
                for (int x = 0; x < field.Width; x++)
                {
                    maxValue = Math.Max(maxValue, field[y, x]);
                }
            }
            return maxValue;
        }

        public static void OutputAsColoredMap(IField2d<float> field, List<TreeNode<TreeNode<Point2d>>> riverSystems, Bitmap bmp, string filename)
        {
            for (int x = 0, y = 0; y < field.Height; y += ++x / field.Width, x %= field.Width)
            {
                float value = field[y, x];
                Color color;
                if (value == 0f)
                    color = Color.DarkBlue;
                else if (value < 0.1f)
                    color = Utils.Lerp(Color.Beige, Color.LightGreen, value / 0.1f);
                else if (value < 0.4f)
                    color = Utils.Lerp(Color.LightGreen, Color.DarkGreen, (value - 0.1f) / 0.3f);
                else if (value < 0.8f)
                    color = Utils.Lerp(Color.DarkGreen, Color.Gray, (value - 0.4f) / 0.4f);
                else if (value < 0.9f)
                    color = Utils.Lerp(Color.Gray, Color.White, (value - 0.8f) / 0.1f);
                else
                    color = Color.White;
                bmp.SetPixel(x, y, color);
            }

            foreach (var riverSystem in riverSystems)
            {
                float maxDepth = riverSystem.value.Depth();

                foreach (var node in riverSystem.value)
                {
                    float t = node.Depth() / maxDepth;
                    Point2d pt = node.value;
                    bmp.SetPixel(pt.x, pt.y, Utils.Lerp(bmp.GetPixel(pt.x, pt.y), Color.Blue, t));
                }
            }

            bmp.Save(filename);
        }

        public static void OutputAsOBJ(IField2d<float> heights, IEnumerable<SplineTree> rivers, Rectangle rect, Bitmap bmp, string outputDir, string outputName = "terrain")
        {
            using (var objWriter = new StreamWriter(File.OpenWrite(outputDir + outputName + ".obj")))
            {
                objWriter.WriteLine("mtllib " + outputName + ".mtl");
                objWriter.WriteLine("o " + outputName + "_o");

                IField2d<vFloat> verts = new Transformation2d<float, vFloat>(heights, (x, y, z) => new vFloat(x, -y, z * 4096 / rect.Width));

                // One vertex per pixel.
                for (int y = 0; y < verts.Height; y++)
                {
                    for (int x = 0; x < verts.Width; x++)
                    {
                        vFloat v = verts[y, x];
                        objWriter.WriteLine("v " + v[0] + " " + v[1] + " " + v[2]);
                    }
                }

                // Normal of the vertex is the cross product of skipping vectors in X and Y.  If skipping is unavailable, use the local vector.
                for (int y = 0; y < verts.Height; y++)
                {
                    for (int x = 0; x < verts.Width; x++)
                    {
                        int xl = Math.Max(x - 1, 0);
                        int xr = Math.Min(x + 1, verts.Width - 1);
                        int yl = Math.Max(y - 1, 0);
                        int yr = Math.Min(y + 1, verts.Height - 1);

                        vFloat xAxis = verts[y, xr] - verts[y, xl];
                        vFloat yAxis = verts[yr, x] - verts[yl, x];

                        vFloat n = vFloat.Cross3d(xAxis, yAxis);
                        if (n[2] < 0f)
                            n = -n;
                        n = n.norm();

                        objWriter.WriteLine("vn " + n[0] + " " + n[1] + " " + n[2]);
                    }
                }

                // Texture coordinate is trivial.
                for (int y = 0; y < verts.Height; y++)
                {
                    for (int x = 0; x < verts.Width; x++)
                    {
                        objWriter.WriteLine("vt " + (1f * x / (verts.Width - 1)) + " " + (1f - 1f * y / (verts.Height - 1)));
                    }
                }

                objWriter.WriteLine("g " + outputName + "_g");
                objWriter.WriteLine("usemtl " + outputName + "_mtl");

                // Now the faces.  Since we're not optimizing at all, this is incredibly simple.
                // TODO: Fix bug where including edge pixels causes tris to go across entire map for some reason.  For now, just workaround.
                for (int y = 1; y < verts.Height - 2; y++)
                {
                    for (int x = 1; x < verts.Width - 2; x++)
                    {
                        int tl = y * verts.Width + x;
                        int tr = tl + 1;
                        int bl = tl + verts.Width;
                        int br = bl + 1;

                        objWriter.WriteLine("f " +
                            tl + "/" + tl + "/" + tl + " " +
                            br + "/" + br + "/" + br + " " +
                            tr + "/" + tr + "/" + tr);

                        objWriter.WriteLine("f " +
                            tl + "/" + tl + "/" + tl + " " +
                            bl + "/" + bl + "/" + bl + " " +
                            br + "/" + br + "/" + br);
                    }
                }
            }

            using (var mtlWriter = new StreamWriter(File.OpenWrite(outputDir + outputName + ".mtl")))
            {
                mtlWriter.WriteLine("newmtl " + outputName + "_mtl");
                mtlWriter.WriteLine("Ka 0.000000 0.000000 0.000000");
                mtlWriter.WriteLine("Kd 0.800000 0.800000 0.800000");
                mtlWriter.WriteLine("Ks 0.200000 0.200000 0.200000");
                mtlWriter.WriteLine("Ns 1.000000");
                mtlWriter.WriteLine("d 1.000000");
                mtlWriter.WriteLine("illum 1");
                mtlWriter.WriteLine("map_Kd " + outputName + ".jpg");
            }

            //CenCatRomSpline colors = new CenCatRomSpline(
            //    new vFloat[]
            //    {
            //        new vFloat(Color.Blue.R, Color.Blue.G, Color.Blue.B, 11),
            //        new vFloat(Color.Blue.R, Color.Blue.G, Color.Blue.B, 0),
            //        new vFloat(Color.SandyBrown.R, Color.SandyBrown.G, Color.SandyBrown.B, 0),
            //        new vFloat(Color.LawnGreen.R, Color.LawnGreen.G, Color.LawnGreen.B, 0),
            //        new vFloat(Color.ForestGreen.R, Color.ForestGreen.G, Color.ForestGreen.B, 0),
            //        new vFloat(Color.SlateGray.R, Color.SlateGray.G, Color.SlateGray.B, 0),
            //        new vFloat(Color.LightGray.R, Color.LightGray.G, Color.LightGray.B, 0),
            //        new vFloat(Color.White.R, Color.White.G, Color.White.B, 0),
            //        new vFloat(Color.White.R, Color.White.G, Color.White.B, 1)
            //        },
            //        0.5f);
            //for (int y = 0; y < bmp.Height; y++)
            //{
            //    for (int x = 0; x < bmp.Width; x++)
            //    {
            //        vFloat c = colors.Sample(heights[y, x]);
            //        bmp.SetPixel(x, y, Color.FromArgb(
            //            (byte)c[0],
            //            (byte)c[1],
            //            (byte)c[2]
            //            ));
            //    }
            //}
            for (int x = 0, y = 0; y < heights.Height; y += ++x / heights.Width, x %= heights.Width)
            {
                float value = heights[y, x];
                Color color;
                if (value < 0.05f)
                    color = Color.DodgerBlue;
                else if (value < 0.1f)
                    color = Utils.Lerp(Color.Beige, Color.LawnGreen, (value - 0.05f) / 0.05f);
                else if (value < 0.4f)
                    color = Utils.Lerp(Color.LawnGreen, Color.ForestGreen, (value - 0.1f) / 0.3f);
                else if (value < 0.8f)
                    color = Utils.Lerp(Color.ForestGreen, Color.SlateGray, (value - 0.4f) / 0.4f);
                else if (value < 0.9f)
                    color = Utils.Lerp(Color.SlateGray, Color.White, (value - 0.8f) / 0.1f);
                else
                    color = Color.White;
                bmp.SetPixel(x, y, color);
            }

            foreach (var s in rivers)
            {
                var samples = s.GetSamplesFromAll(32000 / rect.Width);

                int priorX = int.MinValue;
                int priorY = int.MinValue;

                foreach (var p in samples)
                {
                    int x = (int)((p[0] - rect.Left) * heights.Width / rect.Width);
                    int y = (int)((p[1] - rect.Top) * heights.Height / rect.Height);

                    if (x == priorX && y == priorY)
                    {
                        continue;
                    }
                    else
                    {
                        priorX = x;
                        priorY = y;
                    }

                    if (0 <= x && x < heights.Width && 0 <= y && y < heights.Height)
                    {
                        int r = 5 * 32 / rect.Width;

                        for (int j = -r; j <= r; j++)
                        {
                            for (int i = -r; i <= r; i++)
                            {
                                int xx = x + i;
                                int yy = y + j;

                                if (0 <= xx && xx < heights.Width && 0 <= yy && yy < heights.Height)
                                {
                                    bmp.SetPixel(xx, yy, Color.DodgerBlue);
                                    //float dSq = xx * xx + yy * yy;
                                    //riverbeds[yy, xx] = p[2] + dSq / (1024f * 32 / rect.Width);
                                }
                            }
                        }
                    }
                }
            }
            bmp.Save(outputDir + outputName + ".jpg");
        }
        #endregion Output
    }
}
