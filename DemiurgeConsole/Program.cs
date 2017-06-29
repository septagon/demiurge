using DemiurgeLib;
using DemiurgeLib.Common;
using DemiurgeLib.Noise;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
            //new Thread(RunWaterHeightScenario, StackSize).Start();
            //RunMountainousScenario(1024, 1024, 0.005f);
            //new Thread(RunPopulationScenario, StackSize).Start();
            //RunSplineScenario();
            RunZoomedInScenario();
        }

        private static void RunSplineScenario()
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

        private static void RunMountainousScenario(int width, int height, float startingScale)
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

            OutputField(mountainNoise0, new Bitmap(width, height), "C:\\Users\\Justin Murray\\Desktop\\m0.png");
            var scaled = new ReResField(new SubField<float>(new FieldFromBitmap(jranjana), new Rectangle(400, 300, 200, 200)), 5f);
            OutputField(scaled, new Bitmap(scaled.Width, scaled.Height), "C:\\Users\\Justin Murray\\Desktop\\scaled.png");
            //OutputField(mountainNoise1, new Bitmap(width, height), "C:\\Users\\Justin Murray\\Desktop\\m1.png");
            //OutputField(mountainNoise2, new Bitmap(width, height), "C:\\Users\\Justin Murray\\Desktop\\m2.png");
            //OutputField(mountainNoise3, new Bitmap(width, height), "C:\\Users\\Justin Murray\\Desktop\\m3.png");
        }

        private class WaterTableArgs
        {
            public string inputPath = "C:\\Users\\Justin Murray\\Desktop\\maps\\input\\";
            public string outputPath = "C:\\Users\\Justin Murray\\Desktop\\maps\\output\\";
            public long seed = 0;
            public float baseNoiseScale = 0.015f;
            public float baseNoiseScalar = 0.1f;
            public int hydroSensitivity = 8;
            public float hydroShoreThreshold = 0.5f;
            public float wtfShore = 0.01f;
            public int wtfIt = 10;
            public int wtfLen = 5;
            public float wtfGrade = 0f;
            public float wtfCarveAdd = 0.3f;
            public float wtfCarveMul = 1.3f;
        }

        private static void RunZoomedInScenario()
        {
            // 32x32 up to 1024x1024 will, from the fifth-of-a-mile-per-pixel source, get us approximately 10m per pixel.
            // 16x16 would get us 5
            // 8x8 would get us 2.5
            // 4x4 would get us 1.25
            // 2x2 would get us .75
            // 1x1 would get us .375, which is slightly over 1 foot.
            // I think 16x16 is the sweet spot.  That's just over 9 square miles per small map.
            const int SMALL_MAP_SIDE_LEN = 256;
            const float STARTING_SCALE = 0.005f * SMALL_MAP_SIDE_LEN / 32;
            const int SMALL_MAP_RESIZED_LEN = 1024;

            Random random = new Random();

            WaterTableArgs args = new WaterTableArgs();
            Bitmap bmp = new Bitmap(args.inputPath + "rivers.png");

            IField2d<float> baseMap = new FieldFromBitmap(new Bitmap(args.inputPath + "base_heights.png"));
            baseMap = new ReResField(baseMap, (float)bmp.Width / baseMap.Width);

            var wtf = GenerateWaters(bmp, baseMap);
            OutputAsColoredMap(wtf, wtf.RiverSystems, bmp, args.outputPath + "colored_map.png");

            var hasWater = new Transformation2d<HydrologicalField.LandType, float>(wtf.HydroField, t => t == HydrologicalField.LandType.Land ? 0f : 1f);
            var noiseDamping = new Transformation2d(new BlurredField(hasWater, 2f), v => 3.5f * v);
            
            // Create the spline map.
            SparseField2d<List<SplineTree>> relevantSplines = new SparseField2d<List<SplineTree>>(wtf.Width, wtf.Height, null);
            {
                //HashSet<TreeNode<Point2d>> relevantRivers = new HashSet<TreeNode<Point2d>>();
                foreach (var system in wtf.RiverSystems)
                {
                    SplineTree tree = new SplineTree(system.value, wtf, random);

                    foreach (var p in system.value)
                    {
                        if (relevantSplines[p.value.y, p.value.x] == null)
                            relevantSplines[p.value.y, p.value.x] = new List<SplineTree>();
                        relevantSplines[p.value.y, p.value.x].Add(tree);
                    }
                }
            }

            Rectangle rect = new Rectangle(518, 785 - 128, SMALL_MAP_SIDE_LEN, SMALL_MAP_SIDE_LEN);
            var smallMap = new SubField<float>(wtf, rect);
            var scaledUp = new BlurredField(new ReResField(smallMap, SMALL_MAP_RESIZED_LEN / smallMap.Width), SMALL_MAP_RESIZED_LEN / (4 * SMALL_MAP_SIDE_LEN));
            var smallDamp = new SubField<float>(noiseDamping, rect);
            var scaledDamp = new BlurredField(new ReResField(smallDamp, SMALL_MAP_RESIZED_LEN / smallMap.Width), SMALL_MAP_RESIZED_LEN / (4 * SMALL_MAP_SIDE_LEN));

            // Do spline-y things.
            SparseField2d<float> riverbeds;
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
                riverbeds = new SparseField2d<float>(SMALL_MAP_RESIZED_LEN, SMALL_MAP_RESIZED_LEN, float.MinValue);
                foreach (var s in splines)
                {
                    var samples = s.GetSamples(32000 / SMALL_MAP_SIDE_LEN);

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
                            const int r = 5 * 32 / SMALL_MAP_SIDE_LEN;

                            for (int j = -r; j <= r; j++)
                            {
                                for (int i = -r; i <= r; i++)
                                {
                                    int xx = x + i;
                                    int yy = y + j;

                                    if (0 <= xx && xx < SMALL_MAP_RESIZED_LEN && 0 <= yy && yy < SMALL_MAP_RESIZED_LEN)
                                    {
                                        float dSq = xx * xx + yy * yy;
                                        riverbeds[yy, xx] = p[2] +  dSq / (1024f * 32 / SMALL_MAP_SIDE_LEN);
                                        //scaledDamp[yy, xx] = 1f;
                                        //scaledUp[yy, xx] = Math.Min(scaledUp[yy, xx], p[2] + (float)Math.Sqrt(xx * xx + yy * yy) / 1f);
                                    }
                                }
                            }
                        }
                    }
                }
                //OutputField(riverbeds, new Bitmap(riverbeds.Width, riverbeds.Height), args.outputPath + "river_field.png");
            }

            var mountainous = new ScaleTransform(new MountainNoise(1024, 1024, STARTING_SCALE), 1f);
            var hilly = new ScaleTransform(new Simplex2D(1024, 1024, STARTING_SCALE), 0.1f);
            var terrainNoise = new Transformation2d<float, float, float>(mountainous, hilly, (x, y, m, h) =>
            {
                float a = scaledUp[y, x];
                float sh = Math.Max(-2f * Math.Abs(a - 0.2f) / 3f + 1f, 0f);
                float sm = Math.Min(1.3f * a, 1f);
                return h * sh + m * sm;
            });

            IField2d<float> combined = new NormalizedComposition2d<float>(new Transformation2d<float, float, float>(riverbeds,
                new Composition2d<float>(scaledUp, new Transformation2d<float, float, float>(scaledDamp, terrainNoise, (s, m) => (1 - Math.Min(1, s)) * m)),
                (r, c) => r == float.MinValue ? c : Math.Min(r, c)));

            Bitmap img = new Bitmap(combined.Width, combined.Height);
            OutputField(combined, img, args.outputPath + "combined.png");

            OutputAsOBJ(combined, splines, rect, img, args.outputPath);
        }

        private static void RunPopulationScenario()
        {
            WaterTableArgs args = new WaterTableArgs();
            Bitmap bmp = new Bitmap(args.inputPath + "rivers.png");

            IField2d<float> baseMap = new FieldFromBitmap(new Bitmap(args.inputPath + "base_heights.png"));
            baseMap = new ReResField(baseMap, (float)bmp.Width / baseMap.Width);

            var wtf = GenerateWaters(bmp, baseMap);
            OutputAsColoredMap(wtf, wtf.RiverSystems, bmp, args.outputPath + "colored_map.png");

            IField2d<float> rainfall = new FieldFromBitmap(new Bitmap(args.inputPath + "rainfall.png"));
            rainfall = new ReResField(rainfall, (float)wtf.Width / rainfall.Width);

            IField2d<float> wateriness = GetWaterinessMap(wtf, rainfall);
            OutputField(new NormalizedComposition2d<float>(wateriness), bmp, args.outputPath + "wateriness.png");
            
            var locations = GetSettlementLocations(wtf, wateriness);
            SparseField2d<float> settlementMap = new SparseField2d<float>(wtf.Width, wtf.Height, 0f);
            foreach (var loc in locations) settlementMap.Add(loc, wateriness[loc.y, loc.x]);
            OutputField(settlementMap, bmp, args.outputPath + "settlements.png");

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
            OutputField(meshField, bmp, args.outputPath + "mesh.png");
        }

        private static IField2d<float> GetWaterinessMap(WaterTableField wtf, IField2d<float> rainfall, float waterPortability = 5f, float waterinessAttenuation = 20f)
        {
            // Generate "water flow" map using the rainfall map and the water table map, characterizing
            // how much water is in an area based on upstream.  Note that, in the simplistic case of 
            // identical universal rainfall, this is just a scalar on depth; this whole shindig is
            // intended to support variable rainfall, as characterized by the rainfall map.
            Field2d<float> waterFlow = new Field2d<float>(rainfall);
            float maxValue = float.MinValue;
            foreach (TreeNode<Point2d> waterway in wtf.Waterways)
            {
                List<TreeNode<Point2d>> flattenedReversedRiverTree = new List<TreeNode<Point2d>>(waterway);
                flattenedReversedRiverTree.Reverse();

                foreach (var node in flattenedReversedRiverTree)
                {
                    if (node.parent != null)
                    {
                        Point2d cur = node.value;
                        Point2d par = node.parent.value;

                        waterFlow[par.y, par.x] += waterFlow[cur.y, cur.x];
                    }

                    maxValue = Math.Max(maxValue, waterFlow[node.value.y, node.value.x]);
                }
            }
            IField2d<float> waterinessUnblurred = new FunctionField<float>(waterFlow.Width, waterFlow.Height,
                (x, y) => 1f / (1f + (float)Math.Pow(Math.E, -waterinessAttenuation * waterFlow[y, x] / maxValue)));
            return new BlurredField(waterinessUnblurred, waterPortability);
        }

        private static List<Point2d> GetSettlementLocations(WaterTableField wtf, IField2d<float> wateriness,
            float noiseScale = 0.1f, float noiseBlur = 3f, float noiseContribution = 0.15f)
        {
            IField2d<float> noise = new BlurredField(new MountainNoise(wtf.Width, wtf.Height, noiseScale), noiseBlur);
            Transformation2d<float, float, float> combination = new Transformation2d<float, float, float>(wateriness, noise, (w, n) => w + noiseContribution * n);

            List<Point2d> locations = new List<Point2d>();
            
            for (int y = 1; y < combination.Height - 1; y++)
            {
                for (int x = 1; x < combination.Width - 1; x++)
                {
                    if (wtf[y, x] > 0f &&
                        combination[y - 1, x - 1] < combination[y, x] &&
                        combination[y - 1, x + 0] < combination[y, x] &&
                        combination[y - 1, x + 1] < combination[y, x] &&
                        combination[y + 0, x + 1] < combination[y, x] &&
                        combination[y + 1, x + 1] < combination[y, x] &&
                        combination[y + 1, x + 0] < combination[y, x] &&
                        combination[y + 1, x - 1] < combination[y, x] &&
                        combination[y + 0, x - 1] < combination[y, x])
                    {
                        locations.Add(new Point2d(x, y));
                    }
                }
            }

            return locations;
        }

        private static void RunWaterHeightScenario()
        {
            //RunWaterHeightScenario("rivers_ur_alt.png", "base_heights_ur_alt.png");
            RunWaterHeightScenario("rivers.png", "base_heights.png");
        }

        private static void RunWaterHeightScenario(string simpleWatersMapName, string simpleAltitudesMapName)
        {
            WaterTableArgs args = new WaterTableArgs();
            args.seed = System.DateTime.UtcNow.Ticks;
            Random random = new Random((int)args.seed);

            Bitmap jranjana = new Bitmap(args.inputPath + simpleWatersMapName);
            Field2d<float> field = new FieldFromBitmap(jranjana);

            IField2d<float> bf = new FieldFromBitmap(new Bitmap(args.inputPath + simpleAltitudesMapName));
            bf = new NormalizedComposition2d<float>(bf, new ScaleTransform(new Simplex2D(bf.Width, bf.Height, args.baseNoiseScale, args.seed), args.baseNoiseScalar));
            OutputField(bf, jranjana, args.outputPath + "basis.png");

            BrownianTree tree = BrownianTree.CreateFromOther(field, (x) => x > 0.5f ? BrownianTree.Availability.Available : BrownianTree.Availability.Unavailable, random);
            tree.RunDefaultTree();
            OutputField(new Transformation2d<BrownianTree.Availability, float>(tree, val => val == BrownianTree.Availability.Available ? 1f : 0f),
                jranjana, args.outputPath + "rivers.png");

            HydrologicalField hydro = new HydrologicalField(tree, args.hydroSensitivity, args.hydroShoreThreshold);
            WaterTableField wtf = new WaterTableField(bf, hydro, args.wtfShore, args.wtfIt, args.wtfLen, args.wtfGrade, () =>
            {
                return (float)(args.wtfCarveAdd + random.NextDouble() * args.wtfCarveMul);
            });
            OutputAsTributaryMap(wtf.GeographicFeatures, wtf.RiverSystems, wtf.DrainageField, jranjana, args.outputPath + "tributaries.png");

            OutputField(new NormalizedComposition2d<float>(new Transformation2d<float, float, float>(bf, wtf, (b, w) => Math.Abs(b - w))),
                jranjana, args.outputPath + "errors.png");

            SerializeMap(hydro, wtf, args.seed, args.outputPath + "serialization.bin");

            OutputField(wtf, jranjana, args.outputPath + "heightmap.png");
            OutputAsColoredMap(wtf, wtf.RiverSystems, jranjana, args.outputPath + "colored_map.png");
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
            int w = Math.Min(field.Width, bmp.Width);
            int h = Math.Min(field.Height, bmp.Height);

            for (int x = 0, y = 0; y < h; y += ++x / w, x %= w)
            {
                int value = Math.Max(0, Math.Min((int)(255 * field[y, x]), 255));
                bmp.SetPixel(x, y, Color.FromArgb(value, value, value));
            }
            bmp.Save(filename);
        }

        private static void OutputAsColoredMap(IField2d<float> field, List<TreeNode<TreeNode<Point2d>>> riverSystems, Bitmap bmp, string filename)
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

            foreach (var riverSystem in riverSystems)
            {
                float maxDepth = riverSystem.value.Depth();

                foreach (var node in riverSystem.value)
                {
                    float t = node.Depth() / maxDepth;
                    Point2d pt = node.value;
                    bmp.SetPixel(pt.x, pt.y, Lerp(bmp.GetPixel(pt.x, pt.y), Color.Blue, t));
                }
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

        private static void OutputAsOBJ(IField2d<float> heights, IEnumerable<SplineTree> rivers, Rectangle rect, Bitmap bmp, string outputDir, string outputName = "terrain")
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
                if (value < 0.01f)
                    color = Color.DodgerBlue;
                else if (value < 0.1f)
                    color = Lerp(Color.Beige, Color.LawnGreen, value / 0.1f);
                else if (value < 0.4f)
                    color = Lerp(Color.LawnGreen, Color.ForestGreen, (value - 0.1f) / 0.3f);
                else if (value < 0.8f)
                    color = Lerp(Color.ForestGreen, Color.SlateGray, (value - 0.4f) / 0.4f);
                else if (value < 0.9f)
                    color = Lerp(Color.SlateGray, Color.White, (value - 0.8f) / 0.1f);
                else
                    color = Color.White;
                bmp.SetPixel(x, y, color);
            }

            foreach (var s in rivers)
            {
                var samples = s.GetSamples(32000 / rect.Width);

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

        private static WaterTableField GenerateWaters(Bitmap bmp, IField2d<float> baseField = null, WaterTableArgs args = null, Random random = null)
        {
            args = args ?? new WaterTableArgs() { seed = System.DateTime.UtcNow.Ticks };
            random = random ?? new Random((int)args.seed);
            baseField = baseField ?? new Simplex2D(bmp.Width, bmp.Height, args.baseNoiseScale, args.seed);

            Field2d<float> field = new FieldFromBitmap(bmp);
            
            baseField = new NormalizedComposition2d<float>(baseField, new ScaleTransform(new Simplex2D(baseField.Width, baseField.Height, args.baseNoiseScale, args.seed), args.baseNoiseScalar));

            BrownianTree tree = BrownianTree.CreateFromOther(field, (x) => x > 0.5f ? BrownianTree.Availability.Available : BrownianTree.Availability.Unavailable, random);
            tree.RunDefaultTree();

            HydrologicalField hydro = new HydrologicalField(tree, args.hydroSensitivity, args.hydroShoreThreshold);
            WaterTableField wtf = new WaterTableField(baseField, hydro, args.wtfShore, args.wtfIt, args.wtfLen, args.wtfGrade, () =>
            {
                return (float)(args.wtfCarveAdd + random.NextDouble() * args.wtfCarveMul);
            });

            return wtf;
        }

        private static void SerializeMap(HydrologicalField hydro, WaterTableField wtf, long seed, string outputPath)
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

        private static Bitmap ScaleBitmap(Bitmap src, float scale)
        {
            Bitmap dst = new Bitmap((int)(src.Width * scale), (int)(src.Height * scale));

            scale = (float)dst.Width / src.Width;

            for (int x = 0, y = 0; y < dst.Height; y += ++x / dst.Width, x %= dst.Width)
            {
                float i = x / scale;
                float j = y / scale;

                int iMin = (int)Math.Floor(i);
                int jMin = (int)Math.Floor(j);
                int iMax = (int)Math.Min(iMin + 1, src.Width - 1);
                int jMax = (int)Math.Min(jMin + 1, src.Height - 1);

                Color ul = src.GetPixel(iMin, jMin);
                Color ur = src.GetPixel(iMax, jMin);
                Color ll = src.GetPixel(iMin, jMax);
                Color lr = src.GetPixel(iMax, jMax);

                float tx = 1f - (i - iMin);
                float ty = 1f - (j - jMin);

                int r = (int)Math.Min(255f, ((ul.R + ll.R) * tx + (ur.R + lr.R) * (1f - tx)) / 2f);
                int g = (int)Math.Min(255f, ((ul.G + ll.G) * tx + (ur.G + lr.G) * (1f - tx)) / 2f);
                int b = (int)Math.Min(255f, ((ul.B + ll.B) * tx + (ur.B + lr.B) * (1f - tx)) / 2f);

                dst.SetPixel(x, y, Color.FromArgb(255, r, g, b));
            }

            return dst;
        }
    }
}
