using DemiurgeLib;
using DemiurgeLib.Common;
using DemiurgeLib.Noise;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace DemiurgeConsole
{
    public class Program
    {
        const int Width = 1024;
        const int Height = 1024;
        const float Scale = 0.01f;
        const int StackSize = 10485760;

        static void Main(string[] args)
        {
            //RunWateryScenario();
            new Thread(RunWaterHeightScenario, StackSize).Start();
        }

        public static void RunWaterHeightScenario()
        {
            string inputPath = "C:\\Users\\Justin Murray\\Desktop\\maps\\input\\";
            string outputPath = "C:\\Users\\Justin Murray\\Desktop\\maps\\output\\";

            Bitmap jranjana = new Bitmap(inputPath + "rivers_ur.png");
            Field2d<float> field = new FieldFromBitmap(jranjana);

            BrownianTree tree = BrownianTree.CreateFromOther(field, (x) => x > 0.5f ? BrownianTree.Availability.Available : BrownianTree.Availability.Unavailable);
            tree.RunDefaultTree();
            OutputField(new Transformation2d<BrownianTree.Availability, float>(tree, val => val == BrownianTree.Availability.Available ? 1f : 0f),
                jranjana, outputPath + "river_map_ur.png");
            
            IField2d<float> bf = new FieldFromBitmap(new Bitmap(inputPath + "base_heights_ur.png"));
            bf = new NormalizedComposition2d<float>(bf, new ScaleTransform(new Simplex2D(bf.Width, bf.Height, 0.015f), 0.1f));
            OutputField(bf, jranjana, outputPath + "base_heights_ur.png");

            HydrologicalField hydro = new HydrologicalField(tree);
            WaterTableField wtf = new WaterTableField(bf, hydro);
            OutputAsTributaryMap(wtf.GeographicFeatures, wtf.RiverSystems, wtf.DrainageField, jranjana, outputPath + "tributary_map_ur.png");

            OutputField(new NormalizedComposition2d<float>(new Transformation2d<float, float, float>(bf, wtf, (b, w) => Math.Abs(b - w))),
                jranjana, outputPath + "error_map_ur.png");

            SerializeMap(hydro, wtf, outputPath + "serialization.bin");

            OutputField(wtf, jranjana, outputPath + "heightmap_ur.png");
            OutputAsColoredMap(wtf, jranjana, outputPath + "colored_map_ur.png");
        }

        private static void RunBlurryScenario()
        {
            Bitmap jranjana = new Bitmap("C:\\Users\\Justin Murray\\Desktop\\maps\\input\\rivers_hr.png");
            Field2d<float> field = new FieldFromBitmap(jranjana);
            BlurredField blurred = new BlurredField(field);

            Bitmap output = new Bitmap(blurred.Width, blurred.Height);
            for (int x = 0, y = 0; y < blurred.Height; y += ++x / blurred.Width, x %= blurred.Width)
            {
                float v = blurred[y, x];
                int value = (int)(255f * v);
                output.SetPixel(x, y, Color.FromArgb(value, value, value));
            }
            output.Save("C:\\Users\\Justin Murray\\Desktop\\maps\\output\\blurred.png");
        }

        private static void RunWateryScenario()
        {
            Bitmap jranjana = new Bitmap("C:\\Users\\Justin Murray\\Desktop\\maps\\input\\rivers_lr.png");
            Field2d<float> field = new FieldFromBitmap(jranjana);
            BrownianTree tree = BrownianTree.CreateFromOther(field, (x) => x > 0.5f ? BrownianTree.Availability.Available : BrownianTree.Availability.Unavailable);
            tree.RunDefaultTree();
            HydrologicalField hydro = new HydrologicalField(tree);
            var sets = hydro.FindContiguousSets();
            List<TreeNode<Point2d>> riverForest = new List<TreeNode<Point2d>>();
            foreach (var river in sets[HydrologicalField.LandType.Shore])
            {
                riverForest.Add(river.MakeTreeFromContiguousSet(pt =>
                {
                    // Warning: naive non-boundary-checking test-only implementation.  This will probably CRASH THE PROGRAM
                    // if a river happens to border the edge of the map.
                    return
                        hydro[pt.y + 1, pt.x + 1] == HydrologicalField.LandType.Ocean ||
                        hydro[pt.y + 1, pt.x + 0] == HydrologicalField.LandType.Ocean ||
                        hydro[pt.y + 1, pt.x - 1] == HydrologicalField.LandType.Ocean ||
                        hydro[pt.y + 0, pt.x - 1] == HydrologicalField.LandType.Ocean ||
                        hydro[pt.y - 1, pt.x - 1] == HydrologicalField.LandType.Ocean ||
                        hydro[pt.y - 1, pt.x + 0] == HydrologicalField.LandType.Ocean ||
                        hydro[pt.y - 1, pt.x + 1] == HydrologicalField.LandType.Ocean ||
                        hydro[pt.y + 0, pt.x + 1] == HydrologicalField.LandType.Ocean;
                }));
            }
            DrainageField draino = new DrainageField(hydro, riverForest);
            List<TreeNode<TreeNode<Point2d>>> riverSets = new List<TreeNode<TreeNode<Point2d>>>();
            foreach (var river in riverForest)
            {
                riverSets.Add(river.GetMajorSubtrees(node => node.Depth() > 15));
            }

            using (var file = System.IO.File.OpenWrite("C:\\Users\\Justin Murray\\Desktop\\maps\\output\\report.txt"))
            using (var writer = new System.IO.StreamWriter(file))
            {
                riverSets.OrderByDescending(set => set.Size()).Select(riverSet =>
                {
                    writer.WriteLine("River of size " + riverSet.value.Size() + " with " + riverSet.Size() + " separate sub-rivers.");
                    foreach (var river in riverSet.ToArray().OrderByDescending(t => t.Depth()))
                    {
                        writer.WriteLine("\tPart of river with " + river.value.Depth() + " depth and " + (river.Size() - 1) + " tributaries.");
                    }
                    writer.WriteLine();
                    return 0;
                }).ToArray();
            }
            
            OutputAsTributaryMap(sets, riverSets, draino, jranjana, "C:\\Users\\Justin Murray\\Desktop\\maps\\output\\tree.png");
        }

        private static void RunNoisyScenario()
        {
            IField2d<float> basis = new DemiurgeLib.Noise.DiamondSquare2D(Width, Height);

            IField2d<float> field;
            field = new DemiurgeLib.Noise.Simplex2D(Height, Width, Scale);
            field = new InvertTransform(new AbsTransform(field));

            IField2d<float> field2;
            field2 = new DemiurgeLib.Noise.Simplex2D(Height, Width, Scale * 10);
            field2 = new ScaleTransform(new InvertTransform(new AbsTransform(field2)), 0.1f);

            field = new NormalizedComposition2d<float>(basis, field, field2);

            Bitmap bmp = new Bitmap(Height, Width);
            for (int x = 0, y = 0; y < bmp.Height; y += ++x / bmp.Width, x %= bmp.Width)
            {
                int intensity = (int)(255.0 * field[x, y]);
                bmp.SetPixel(x, y, Color.FromArgb(intensity, intensity, intensity));
            }
            bmp.Save("C:\\Users\\Justin Murray\\Desktop\\maps\\output\\noisy.png");
        }

        private class FieldFromBitmap : Field2d<float>
        {
            public FieldFromBitmap(Bitmap bmp) : base(bmp.Width, bmp.Height)
            {
                for (int x = 0, y = 0; y < this.Height; y += ++x / this.Width, x %= this.Width)
                {
                    this[y, x] = bmp.GetPixel(x, y).GetBrightness();
                }
            }
        }

        private static void OutputField(IField2d<float> field, Bitmap bmp, string filename)
        {
            for (int x = 0, y = 0; y < field.Height; y += ++x / field.Width, x %= field.Width)
            {
                int value = Math.Min((int)(255 * field[y, x]), 255);
                bmp.SetPixel(x, y, Color.FromArgb(value, value, value));
            }
            bmp.Save(filename);
        }

        private static void OutputAsColoredMap(IField2d<float> field, Bitmap bmp, string filename)
        {
            for (int x = 0, y = 0; y < field.Height; y += ++x / field.Width, x %= field.Width)
            {
                float value = field[y, x];
                Color color;
                if (value == 0f)
                    color = Color.DarkBlue;
                else if (value < 0.1f)
                    color = Lerp(Color.Beige, Color.LightGreen, value / 0.1f);
                else if (value < 0.4f)
                    color = Lerp(Color.LightGreen, Color.DarkGreen, (value - 0.1f) / 0.3f);
                else if (value < 0.8f)
                    color = Lerp(Color.DarkGreen, Color.Gray, (value - 0.4f) / 0.4f);
                else if (value < 0.9f)
                    color = Lerp(Color.Gray, Color.White, (value - 0.8f) / 0.1f);
                else
                    color = Color.White;
                bmp.SetPixel(x, y, color);
            }
            bmp.Save(filename);
        }

        private static void OutputAsTributaryMap(
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

        private static void SerializeMap(HydrologicalField hydro, WaterTableField wtf, string outputPath)
        {
            using (var binFile = System.IO.File.OpenWrite(outputPath + "serialized.bin"))
            using (var binWriter = new System.IO.BinaryWriter(binFile))
            using (var sumFile = System.IO.File.OpenWrite(outputPath + "summarized.txt"))
            using (var sumWriter = new System.IO.StreamWriter(sumFile))
            {
                int[,] idAssociations = new int[wtf.Height, wtf.Width];
                int id;

                // Version
                binWriter.Write(0);
                sumWriter.WriteLine("Version 0");

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

        private static Color Lerp(Color from, Color to, float t)
        {
            t = Math.Max(0, Math.Min(t, 1f));
            return Color.FromArgb(
                (int)(from.A * (1f - t) + to.A * t),
                (int)(from.R * (1f - t) + to.R * t),
                (int)(from.G * (1f - t) + to.G * t),
                (int)(from.B * (1f - t) + to.B * t)
                );
        }
    }
}
