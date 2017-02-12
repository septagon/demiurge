using DemiurgeLib;
using DemiurgeLib.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace DemiurgeConsole
{
    public class Program
    {
        const int Width = 1024;
        const int Height = 1024;
        const float Scale = 0.01f;

        static void Main(string[] args)
        {
            //RunWateryScenario();
            RunWaterHeightScenario();
        }

        public static void RunWaterHeightScenario()
        {
            Bitmap jranjana = new Bitmap("C:\\Users\\Justin Murray\\Desktop\\jranjana_landmasses_rivers.png");
            Field2d<float> field = new FieldFromBitmap(jranjana);
            BrownianTree tree = BrownianTree.CreateFromOther(field, (x) => x > 0.5f ? BrownianTree.Availability.Available : BrownianTree.Availability.Unavailable);
            tree.RunDefaultTree();
            HydrologicalField hydro = new HydrologicalField(tree);
            
            // This naive approach to creating the reference field has some flaws, particularly
            // regarding the unseemly prominence of extremely short rivers (those that extend to
            // sources very near the ocean).
            BlurredField bf = new BlurredField(new ScaleTransform(field, 0.8f));

            WaterTableField wtf = new WaterTableField(bf, hydro);

            OutputField(wtf, "C:\\Users\\Justin Murray\\Desktop\\heightmap.png");
            OutputAsColoredMap(wtf, "C:\\Users\\Justin Murray\\Desktop\\colored_map.png");
        }

        private static void RunBlurryScenario()
        {
            Bitmap jranjana = new Bitmap("C:\\Users\\Justin Murray\\Desktop\\jranjana_landmasses_rivers_fullres.png");
            Field2d<float> field = new FieldFromBitmap(jranjana);
            BlurredField blurred = new BlurredField(field);

            Bitmap output = new Bitmap(blurred.Width, blurred.Height);
            for (int x = 0, y = 0; y < blurred.Height; y += ++x / blurred.Width, x %= blurred.Width)
            {
                float v = blurred[y, x];
                int value = (int)(255f * v);
                output.SetPixel(x, y, Color.FromArgb(value, value, value));
            }
            output.Save("C:\\Users\\Justin Murray\\Desktop\\blurred.png");
        }

        private static void RunWateryScenario()
        {
            Bitmap jranjana = new Bitmap("C:\\Users\\Justin Murray\\Desktop\\jranjana_landmasses_rivers_small.png");
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

            using (var file = System.IO.File.OpenWrite("C:\\Users\\Justin Murray\\Desktop\\report.txt"))
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

            Bitmap bmp = new Bitmap(hydro.Width, hydro.Height);
            //for (int x = 0, y = 0; y < bmp.Height; y += ++x / bmp.Width, x %= bmp.Width)
            //{
            //    //bmp.SetPixel(x, y, tree[y, x] == BrownianTree.Availability.Available ? Color.White : Color.Black);
            //    switch (hydro[y, x])
            //    {
            //        case HydrologicalField.LandType.Land:
            //            bmp.SetPixel(x, y, Color.Wheat);
            //            break;
            //        case HydrologicalField.LandType.Shore:
            //            bmp.SetPixel(x, y, Color.Teal);
            //            break;
            //        case HydrologicalField.LandType.Ocean:
            //            bmp.SetPixel(x, y, Color.DarkBlue);
            //            break;
            //    }
            //}
            System.Random rand = new System.Random();
            foreach (var hs in sets.Values)
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
            foreach(var river in riverSets)
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
            foreach (var landmass in sets[HydrologicalField.LandType.Land])
            {
                foreach (Point2d p in landmass)
                {
                    Point2d drain = draino[p.y, p.x];
                    Color c = bmp.GetPixel(drain.x, drain.y);
                    c = Color.FromArgb(c.R + 64, c.G + 64, c.B + 64);
                    bmp.SetPixel(p.x, p.y, c);
                }
            }
            bmp.Save("C:\\Users\\Justin Murray\\Desktop\\tree.png");
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
            bmp.Save("C:\\Users\\Justin Murray\\Desktop\\output.png");
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

        /// <summary>
        /// TODO: This is a draft class as it doesn't quite behave properly for all scenarios.
        /// Flaws include the dendency to produce singularly prominent soures of for extremely
        /// short rivers, and to completely eliminate the heightmaps for landmasses that contain
        /// few or no rivers.  Both of these are deal-breaking flaws and must be overcome before
        /// this approach can be accepted as a method of generating base altitudes; that said,
        /// other than those flaws, it's working splendidly!
        /// </summary>
        private class WaterTableField : Field2d<float>
        {
            public WaterTableField(
                IField2d<float> baseField,
                IField2d<HydrologicalField.LandType> hydroField)
                : base(baseField.Width, baseField.Height)
            {
                float epsilon = 0.05f;
                int blurIterations = 10;
                int minWaterwayLength = 5;

                var geographicFeatures = hydroField.FindContiguousSets();

                var waterways = geographicFeatures.GetRiverSystems(hydroField).Where(ww => ww.Depth() >= minWaterwayLength).ToList();

                var riverSystems = waterways.GetRivers();

                DrainageField draino = new DrainageField(hydroField, waterways);

                foreach (var sea in geographicFeatures[HydrologicalField.LandType.Ocean])
                {
                    foreach (var p in sea)
                    {
                        this[p.y, p.x] = 0f;
                    }
                }

                // Set the heights of all the river systems.
                foreach (var river in riverSystems)
                {
                    Queue<TreeNode<TreeNode<Point2d>>> mouths = new Queue<TreeNode<TreeNode<Point2d>>>();
                    mouths.Enqueue(river);

                    Point2d p = river.value.value;
                    this[p.y, p.x] = 0f;

                    while (mouths.Count > 0)
                    {
                        var mouth = mouths.Dequeue();

                        p = mouth.value.value;
                        float mouthAlti = this[p.y, p.x];
                        p = mouth.value.GetDeepestValue();
                        float sourceAlti = baseField[p.y, p.x];

                        float inc = (sourceAlti - mouthAlti) / mouth.value.Depth();

                        mouth.IteratePrimarySubtree().Iterate(node =>
                        {
                            if (node.parent != null)
                            {
                                p = node.value;
                                Point2d pt = node.parent.value;
                                this[p.y, p.x] = this[pt.y, pt.x] + inc;
                            }
                        });

                        foreach (var child in mouth.children)
                        {
                            mouths.Enqueue(child);
                        }
                    }
                }

                // At this point, all the water pixels have a defined height; set every
                // land pixel to be the same height as its drain iff it drains to a river.
                foreach (var land in geographicFeatures[HydrologicalField.LandType.Land])
                {
                    foreach (var p in land)
                    {
                        Point2d drain = draino[p.y, p.x];
                        if (hydroField[drain.y, drain.x] == HydrologicalField.LandType.Shore)
                        {
                            this[p.y, p.x] = this[drain.y, drain.x] + epsilon;
                        }
                        else
                        {
                            this[p.y, p.x] = baseField[p.y, p.x] + epsilon;
                        }
                    }
                }
                
                for (int idx = 0; idx < blurIterations; idx++)
                {
                    BlurredField bf = new BlurredField(this, 1);
                    foreach (var land in geographicFeatures[HydrologicalField.LandType.Land])
                    {
                        foreach (var p in land)
                        {
                            this[p.y, p.x] = bf[p.y, p.x];
                        }
                    }
                }

                System.Diagnostics.Debug.Assert(waterways.AreWaterwaysLegalForField(this));
            }
        }

        private static void OutputField(IField2d<float> field, string filename)
        {
            Bitmap bmp = new Bitmap(field.Width, field.Height);
            for (int x = 0, y = 0; y < field.Height; y += ++x / field.Width, x %= field.Width)
            {
                int value = Math.Min((int)(255 * field[y, x]), 255);
                bmp.SetPixel(x, y, Color.FromArgb(value, value, value));
            }
            bmp.Save(filename);
        }

        private static void OutputAsColoredMap(IField2d<float> field, string filename)
        {
            Bitmap bmp = new Bitmap(field.Width, field.Height);
            for (int x = 0, y = 0; y < field.Height; y += ++x / field.Width, x %= field.Width)
            {
                float value = field[y, x];
                Color color;
                if (value == 0f)
                    color = Color.DarkBlue;
                else if (value < 0.1f)
                    color = Color.Beige;
                else if (value < 0.4f)
                    color = Color.LightGreen;
                else if (value < 0.8f)
                    color = Color.DarkGreen;
                else if (value < 0.9f)
                    color = Color.Gray;
                else
                    color = Color.White;
                bmp.SetPixel(x, y, color);
            }
            bmp.Save(filename);
        }
    }
}
