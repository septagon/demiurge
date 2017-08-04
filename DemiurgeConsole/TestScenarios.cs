using DemiurgeLib;
using DemiurgeLib.Common;
using DemiurgeLib.Noise;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using static DemiurgeConsole.Utils;

namespace DemiurgeConsole
{
    public class TestScenarios
    {
        public static void RunMeterScaleMapScenario()
        {
            var wta = new WaterTableArgs();
            var waters = new FieldFromBitmap(new Bitmap(wta.inputPath + "rivers.png"));
            var heights = new FieldFromBitmap(new Bitmap(wta.inputPath + "base_heights.png"));
            var msmArgs = new MeterScaleMap.Args(waters, heights, null);
            msmArgs.seed = System.DateTime.UtcNow.Ticks;
            msmArgs.metersPerPixel = 1600;
            msmArgs.riverCapacityToMetersWideFunc = c => (float)Math.Sqrt(msmArgs.metersPerPixel * c);
            var msm = new MeterScaleMap(msmArgs);

            msm.OutputHighLevelMaps(new Bitmap(waters.Width, waters.Height), "C:\\Users\\Justin Murray\\Desktop\\terrain\\");
            msm.OutputMapGrid(100, "C:\\Users\\Justin Murray\\Desktop\\terrain\\", "submap");
        }

        public static void RunMeterScaleMapScenarioUR()
        {
            var wta = new WaterTableArgs();
            var waters = new FieldFromBitmap(new Bitmap(wta.inputPath + "rivers_ur_alt.png"));
            var heights = new FieldFromBitmap(new Bitmap(wta.inputPath + "base_heights_ur_alt.png"));
            var msmArgs = new MeterScaleMap.Args(waters, heights, null);
            msmArgs.seed = System.DateTime.UtcNow.Ticks;
            msmArgs.metersPerPixel = 1600 / 5;
            msmArgs.riverCapacityToMetersWideFunc = c => (float)Math.Pow(msmArgs.metersPerPixel * c, 0.5f) / 4f;
            var msm = new MeterScaleMap(msmArgs);

            msm.OutputHighLevelMaps(new Bitmap(waters.Width, waters.Height), "C:\\Users\\Justin Murray\\Desktop\\terrain\\");
            msm.OutputMapGrid(20, "C:\\Users\\Justin Murray\\Desktop\\terrain\\", "submap", 256);
        }

        public static void RunPathScenario()
        {
            IField2d<float> costs = new MountainNoise(1024, 1024, 0.01f);// new Transformation2d(new Simplex2D(1024, 1024, 0.1f), v => (float)Math.Round(v));
            var path = Search.FindPath(new Rectangle(0, 0, costs.Width, costs.Height), new DemiurgeLib.Common.Point2d(10, 10), new DemiurgeLib.Common.Point2d(costs.Width - 10, costs.Height - 10), (a, b) =>
            {
                if (a.x < 0 || b.x < 0 || a.y < 0 || b.y < 0 ||
                    a.x >= costs.Width || b.x >= costs.Width | a.y >= costs.Height || b.y >= costs.Height)
                    return float.PositiveInfinity;

                float cost = Point2d.Distance(a, b);
                cost += (float)Math.Pow(1000f * Math.Abs(costs[a.y, a.x] - costs[b.y, b.x]), 2); // Steep slope penalty.
                cost += 100f * costs[b.y, b.x];                             // High altitude penalty.
                return cost;
            });
            Bitmap bmp = new Bitmap(costs.Width, costs.Height);
            for (int y = 0; y < costs.Height; y++)
                for (int x = 0; x < costs.Width; x++)
                    bmp.SetPixel(x, y, Color.FromArgb((int)(255 * costs[y, x]), (int)(255 * costs[y, x]), (int)(255 * costs[y, x])));
            bmp.Save("C:\\Users\\Justin Murray\\Desktop\\mountains.png");
            foreach (var pnt in path)
                bmp.SetPixel(pnt.x, pnt.y, Color.Red);
            bmp.Save("C:\\Users\\Justin Murray\\Desktop\\path_test.png");
        }

        public static void RunSplineScenario()
        {
            Random r = new Random();
            List<vFloat> cps = new List<vFloat>();
            for (int i = 0; i < 20; i++)
            {
                cps.Add(new vFloat(r.Next(32, 480), r.Next(32, 480), r.Next(16, 240), r.Next(16, 240), r.Next(16, 240)));
            }

            var spline = new CenCatRomSpline(cps.ToArray(), 0.5f);

            Bitmap bmp = new Bitmap(512, 512);
            foreach (var s in spline.GetSamples(10000))
            {
                bmp.SetPixel((int)s[0], (int)s[1], Color.FromArgb((int)s[2], (int)s[3], (int)s[4]));
            }

            bmp.Save("C:\\Users\\Justin Murray\\Desktop\\spline.png");
        }

        public static void RunMountainousScenario(int width, int height, float startingScale)
        {
            string input = "C:\\Users\\Justin Murray\\Desktop\\maps\\input\\rivers.png";
            Bitmap jranjana = new Bitmap(input);
            //int needToMake = 0;
            //for (int x = 0, y = 0; y < jranjana.Height / 24; y += ++x / (jranjana.Width / 24), x %= (jranjana.Width / 24))
            //{
            //    if (24 * x + 23 <= jranjana.Width && 24 * y + 23 <= jranjana.Height)
            //    {
            //        for (int j = 24 * y; j < 24 * y + 24; j++)
            //        {
            //            for (int i = 24 * x; i < 24 * x + 24; i++)
            //            {
            //                if (jranjana.GetPixel(i, j).GetBrightness() > 0.3f)
            //                {
            //                    needToMake++;
            //                    i += 24;
            //                    j += 24;
            //                }
            //            }
            //        }
            //    }
            //}
            //System.Console.WriteLine("Have to make " + needToMake);

            long seed = System.DateTime.Now.Ticks;

            IField2d<float> mountainNoise0 = new MountainNoise(width, height, startingScale, seed, 0, 0);
            //IField2d<float> mountainNoise1 = new MountainNoise(width, height, startingScale, seed, 1024, 0);
            //IField2d<float> mountainNoise2 = new MountainNoise(width, height, startingScale, seed, 0, 1024);
            //IField2d<float> mountainNoise3 = new MountainNoise(width, height, startingScale, seed, 1024, 1024);

            //IField2d<float> valleyNoise = new Transformation2d(mountainNoise, (x, y, val) =>
            //{
            //    float t = Math.Min(1f, Math.Abs(mountainNoise.Width / 2 - x) / 100f);
            //    return val * t;
            //});

            Utils.OutputField(mountainNoise0, new Bitmap(width, height), "C:\\Users\\Justin Murray\\Desktop\\m0.png");
            //var scaled = new ReResField(new SubField<float>(new Utils.FieldFromBitmap(jranjana), new Rectangle(400, 300, 200, 200)), 5f);
            //Utils.OutputField(scaled, new Bitmap(scaled.Width, scaled.Height), "C:\\Users\\Justin Murray\\Desktop\\scaled.png");
            //Utils.OutputField(mountainNoise1, new Bitmap(width, height), "C:\\Users\\Justin Murray\\Desktop\\m1.png");
            //Utils.OutputField(mountainNoise2, new Bitmap(width, height), "C:\\Users\\Justin Murray\\Desktop\\m2.png");
            //Utils.OutputField(mountainNoise3, new Bitmap(width, height), "C:\\Users\\Justin Murray\\Desktop\\m3.png");
        }

        public static void RunWaterHeightScenario()
        {
            //RunWaterHeightScenario("rivers_ur_alt.png", "base_heights_ur_alt.png");
            RunWaterHeightScenario("rivers.png", "base_heights.png");
        }

        public static void RunWaterHeightScenario(string simpleWatersMapName, string simpleAltitudesMapName)
        {
            WaterTableArgs args = new WaterTableArgs();
            args.seed = System.DateTime.UtcNow.Ticks;
            Random random = new Random((int)args.seed);

            Bitmap jranjana = new Bitmap(args.inputPath + simpleWatersMapName);
            Field2d<float> field = new Utils.FieldFromBitmap(jranjana);

            IField2d<float> bf = new Utils.FieldFromBitmap(new Bitmap(args.inputPath + simpleAltitudesMapName));
            bf = new NormalizedComposition2d<float>(bf, new ScaleTransform(new Simplex2D(bf.Width, bf.Height, args.baseNoiseScale, args.seed), args.baseNoiseScalar));
            Utils.OutputField(bf, jranjana, args.outputPath + "basis.png");

            BrownianTree tree = BrownianTree.CreateFromOther(field, (x) => x > 0.5f ? BrownianTree.Availability.Available : BrownianTree.Availability.Unavailable, random);
            tree.RunDefaultTree();
            Utils.OutputField(new Transformation2d<BrownianTree.Availability, float>(tree, val => val == BrownianTree.Availability.Available ? 1f : 0f),
                jranjana, args.outputPath + "rivers.png");

            HydrologicalField hydro = new HydrologicalField(tree, args.hydroSensitivity, args.hydroShoreThreshold);
            WaterTableField wtf = new WaterTableField(bf, hydro, args.wtfShore, args.wtfIt, args.wtfLen, args.wtfGrade, () =>
            {
                return (float)(args.wtfCarveAdd + random.NextDouble() * args.wtfCarveMul);
            });
            Utils.OutputAsTributaryMap(wtf.GeographicFeatures, wtf.RiverSystems, wtf.DrainageField, jranjana, args.outputPath + "tributaries.png");

            Utils.OutputField(new NormalizedComposition2d<float>(new Transformation2d<float, float, float>(bf, wtf, (b, w) => Math.Abs(b - w))),
                jranjana, args.outputPath + "errors.png");

            Utils.SerializeMap(hydro, wtf, args.seed, args.outputPath + "serialization.bin");

            Utils.OutputField(wtf, jranjana, args.outputPath + "heightmap.png");
            Utils.OutputAsColoredMap(wtf, wtf.RiverSystems, jranjana, args.outputPath + "colored_map.png");
        }

        public static void RunBlurryScenario()
        {
            Bitmap jranjana = new Bitmap("C:\\Users\\Justin Murray\\Desktop\\maps\\input\\rivers_hr.png");
            Field2d<float> field = new Utils.FieldFromBitmap(jranjana);
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

        public static void RunWateryScenario()
        {
            Bitmap jranjana = new Bitmap("C:\\Users\\Justin Murray\\Desktop\\maps\\input\\rivers_lr.png");
            Field2d<float> field = new Utils.FieldFromBitmap(jranjana);
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

            Utils.OutputAsTributaryMap(sets, riverSets, draino, jranjana, "C:\\Users\\Justin Murray\\Desktop\\maps\\output\\tree.png");
        }

        public static void RunNoisyScenario()
        {
            const int Width = 1024;
            const int Height = 1024;
            const float Scale = 0.01f;

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

        public static void RunPopulationScenario()
        {
            WaterTableArgs args = new WaterTableArgs();
            Bitmap bmp = new Bitmap(args.inputPath + "rivers.png");

            IField2d<float> baseMap = new Utils.FieldFromBitmap(new Bitmap(args.inputPath + "base_heights.png"));
            baseMap = new ReResField(baseMap, (float)bmp.Width / baseMap.Width);

            var wtf = Utils.GenerateWaters(bmp, baseMap);
            Utils.OutputAsColoredMap(wtf, wtf.RiverSystems, bmp, args.outputPath + "colored_map.png");

            IField2d<float> rainfall = new Utils.FieldFromBitmap(new Bitmap(args.inputPath + "rainfall.png"));
            rainfall = new ReResField(rainfall, (float)wtf.Width / rainfall.Width);

            IField2d<float> wateriness = Utils.GetWaterinessMap(wtf, rainfall);
            Utils.OutputField(new NormalizedComposition2d<float>(wateriness), bmp, args.outputPath + "wateriness.png");

            var locations = Utils.GetSettlementLocations(wtf, wateriness);
            SparseField2d<float> settlementMap = new SparseField2d<float>(wtf.Width, wtf.Height, 0f);
            foreach (var loc in locations) settlementMap.Add(loc, wateriness[loc.y, loc.x]);
            Utils.OutputField(settlementMap, bmp, args.outputPath + "settlements.png");

            TriangleNet.Geometry.InputGeometry pointSet = new TriangleNet.Geometry.InputGeometry();
            foreach (var loc in locations)
            {
                pointSet.AddPoint(loc.x, loc.y);
            }
            TriangleNet.Mesh mesh = new TriangleNet.Mesh();
            mesh.Triangulate(pointSet);
            //TriangleNet.Tools.AdjacencyMatrix mat = new TriangleNet.Tools.AdjacencyMatrix(mesh);

            Field2d<float> meshField = new Field2d<float>(settlementMap);
            foreach (var e in mesh.Edges)
            {
                var v0 = mesh.GetVertex(e.P0);
                var v1 = mesh.GetVertex(e.P1);

                float distance = (float)Math.Sqrt(Math.Pow(v0.X - v1.X, 2) + Math.Pow(v0.Y - v1.Y, 2));

                for (float t = 0f; t <= 1f; t += 0.5f / distance)
                {
                    int x = (int)Math.Round((1f - t) * v0.X + t * v1.X);
                    int y = (int)Math.Round((1f - t) * v0.Y + t * v1.Y);

                    meshField[y, x] = 0.5f;
                }

                meshField[(int)v0.Y, (int)v0.X] = 1f;
                meshField[(int)v1.Y, (int)v1.X] = 1f;
            }
            Utils.OutputField(meshField, bmp, args.outputPath + "mesh.png");
        }

        public static void RunZoomedInScenario()
        {
            // 32x32 up to 1024x1024 will, from the fifth-of-a-mile-per-pixel source, get us approximately 10m per pixel.
            // 16x16 would get us 5
            // 8x8 would get us 2.5
            // 4x4 would get us 1.25
            // 2x2 would get us .75
            // 1x1 would get us .375, which is slightly over 1 foot.
            // I think 16x16 is the sweet spot.  That's just over 9 square miles per small map.
            const int SMALL_MAP_SIDE_LEN = 64;
            const float STARTING_SCALE = 0.005f * SMALL_MAP_SIDE_LEN / 32;
            const int SMALL_MAP_RESIZED_LEN = 1024;

            Random random = new Random();

            WaterTableArgs args = new WaterTableArgs();
            Bitmap bmp = new Bitmap(args.inputPath + "rivers.png");

            IField2d<float> baseMap = new Utils.FieldFromBitmap(new Bitmap(args.inputPath + "base_heights.png"));
            baseMap = new ReResField(baseMap, (float)bmp.Width / baseMap.Width);

            var wtf = Utils.GenerateWaters(bmp, baseMap);
            Utils.OutputAsColoredMap(wtf, wtf.RiverSystems, bmp, args.outputPath + "colored_map.png");

            var hasWater = new Transformation2d<HydrologicalField.LandType, float>(wtf.HydroField, t => t == HydrologicalField.LandType.Land ? 0f : 1f);
            var noiseDamping = new Transformation2d(new BlurredField(hasWater, 2f), v => 3.5f * v);

            // Create the spline map.
            SparseField2d<List<SplineTree>> relevantSplines = new SparseField2d<List<SplineTree>>(wtf.Width, wtf.Height, null);
            {
                //HashSet<TreeNode<Point2d>> relevantRivers = new HashSet<TreeNode<Point2d>>();
                foreach (var system in wtf.RiverSystems)
                {
                    SplineTree tree = null;

                    foreach (var p in system.value)
                    {
                        if (relevantSplines[p.value.y, p.value.x] == null)
                            relevantSplines[p.value.y, p.value.x] = new List<SplineTree>();
                        relevantSplines[p.value.y, p.value.x].Add(tree ?? (tree = new SplineTree(system.value, wtf, random)));
                    }
                }
            }

            Rectangle rect = new Rectangle(518 + 15, 785 + 45, SMALL_MAP_SIDE_LEN, SMALL_MAP_SIDE_LEN);
            var smallMap = new SubField<float>(wtf, rect);
            var scaledUp = new BlurredField(new ReResField(smallMap, SMALL_MAP_RESIZED_LEN / smallMap.Width), SMALL_MAP_RESIZED_LEN / (4 * SMALL_MAP_SIDE_LEN));
            var smallDamp = new SubField<float>(noiseDamping, rect);
            var scaledDamp = new BlurredField(new ReResField(smallDamp, SMALL_MAP_RESIZED_LEN / smallMap.Width), SMALL_MAP_RESIZED_LEN / (4 * SMALL_MAP_SIDE_LEN));

            // Do spline-y things.
            Field2d<float> riverbeds;
            List<SplineTree> splines = new List<SplineTree>();
            {
                // Collect a comprehensive list of the spline trees for the local frame.
                for (int y = rect.Top - 1; y <= rect.Bottom + 1; y++)
                {
                    for (int x = rect.Left - 1; x <= rect.Right + 1; x++)
                    {
                        List<SplineTree> trees = relevantSplines[y, x];
                        if (trees != null)
                            splines.AddRange(trees);
                    }
                }

                // Crafts the actual river kernel.  Probably not the best way to go about this.
                riverbeds = new Field2d<float>(new ConstantField<float>(SMALL_MAP_RESIZED_LEN, SMALL_MAP_RESIZED_LEN, float.MaxValue));
                foreach (var s in splines)
                {
                    var samples = s.GetSamplesPerControlPoint(1f * SMALL_MAP_RESIZED_LEN / SMALL_MAP_SIDE_LEN);

                    int priorX = int.MinValue;
                    int priorY = int.MinValue;

                    foreach (var p in samples)
                    {
                        int x = (int)((p[0] - rect.Left) * SMALL_MAP_RESIZED_LEN / SMALL_MAP_SIDE_LEN);
                        int y = (int)((p[1] - rect.Top) * SMALL_MAP_RESIZED_LEN / SMALL_MAP_SIDE_LEN);

                        if (x == priorX && y == priorY)
                        {
                            continue;
                        }
                        else
                        {
                            priorX = x;
                            priorY = y;
                        }

                        if (0 <= x && x < SMALL_MAP_RESIZED_LEN && 0 <= y && y < SMALL_MAP_RESIZED_LEN)
                        {
                            const int r = 1024 / SMALL_MAP_SIDE_LEN;

                            for (int j = -r; j <= r; j++)
                            {
                                for (int i = -r; i <= r; i++)
                                {
                                    int xx = x + i;
                                    int yy = y + j;

                                    if (0 <= xx && xx < SMALL_MAP_RESIZED_LEN && 0 <= yy && yy < SMALL_MAP_RESIZED_LEN)
                                    {
                                        float dSq = i * i + j * j;
                                        riverbeds[yy, xx] = Math.Min(riverbeds[yy, xx], p[2] + dSq / (512f * 32 / SMALL_MAP_SIDE_LEN));
                                        //scaledDamp[yy, xx] = 1f;
                                        //scaledUp[yy, xx] = Math.Min(scaledUp[yy, xx], p[2] + (float)Math.Sqrt(xx * xx + yy * yy) / 1f);
                                    }
                                }
                            }
                        }
                    }
                }
                //Utils.OutputField(riverbeds, new Bitmap(riverbeds.Width, riverbeds.Height), args.outputPath + "river_field.png");
            }

            var mountainous = new ScaleTransform(new MountainNoise(1024, 1024, STARTING_SCALE), 1f);
            var hilly = new ScaleTransform(new Simplex2D(1024, 1024, STARTING_SCALE * 4), 0.1f);
            var terrainNoise = new Transformation2d<float, float, float>(mountainous, hilly, (x, y, m, h) =>
            {
                float a = scaledUp[y, x];
                float sh = Math.Max(-2f * Math.Abs(a - 0.2f) / 3f + 1f, 0f);
                float sm = Math.Min(1.3f * a, 1f);
                return h * sh + m * sm;
            });

            IField2d<float> combined =
                new NormalizedComposition2d<float>(
                    new Transformation2d<float, float, float>(riverbeds,
                        new Composition2d<float>(scaledUp,
                            new Transformation2d<float, float, float>(scaledDamp, terrainNoise, (s, m) => (1 - Math.Min(1, s)) * m)
                        ),
                    (r, c) => r == float.MaxValue ? c : Math.Min(r, c))
                );

            Bitmap img = new Bitmap(combined.Width, combined.Height);
            Utils.OutputField(combined, img, args.outputPath + "combined.png");

            Utils.OutputAsOBJ(combined, splines, rect, img, args.outputPath);
        }
    }
}
