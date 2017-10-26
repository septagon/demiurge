using DemiurgeLib;
using DemiurgeLib.Common;
using DemiurgeLib.Noise;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace DemiurgeConsole
{
    class MeterScaleMap
    {
        /// <summary>
        /// The following types of direct input are implemented:
        ///   - watersDrawing: a sketch of oceans and other major waters, where land is white and water is black.
        ///   - heightsDrawing: a sketch indicating the approximate regional altitudes, where light is high and dark is low.
        ///   - rainDrawing: a sketch indicating the approximate rainfall by region, where light is wet and dark is dry.
        /// The following types of direct input are posited:
        ///   - hillDrawing: a sketch indicating how rugged the terrain is by area, where light is rough and dark is smooth.
        ///   - valleyDrawing: a sketch indicating the typical shape of valleys (format undecided)
        ///   - volcanoDrawing: a sketch indicating the location of stratovolcanos, where any color other than black or white indicates a volcano.
        ///   
        /// All input fields are expected to come in with the same dimensions and normalized (containing values in the range [0, 1] inclusive).
        /// </summary>
        public class Args
        {
            public IField2d<float> watersDrawing;
            public IField2d<float> heightsDrawing;
            public IField2d<float> roughnessDrawing;
            public IField2d<float> rainDrawing;

            public long seed = 0;

            public float metersPerPixel = 1600f;
            public float baseHeightMaxInMeters = 2000f;
            public float mountainHeightMaxInMeters = 2000f;
            public float valleyRadiusInMeters = 5000f;
            public float canyonRadiusInMeters = 1000f;
            public Func<float, float> riverCapacityToMetersWideFunc = w => SplineTree.CAPACITY_DIVISOR * w;

            public float valleyStrength = 0.8f;
            public float canyonStrength = 0.999f;

            public int hydroSensitivity = 8;
            public float hydroShoreThreshold = 0.5f;
            public float wtfShore = 0.01f;
            public int wtfIt = 10;
            public int wtfLen = 5;
            public float wtfGrade = 0f;
            public float wtfCarveAdd = 0.3f;
            public float wtfCarveMul = 1.3f;

            public Args(IField2d<float> waters, IField2d<float> heights, IField2d<float> roughness, IField2d<float> rain)
            {
                this.watersDrawing = waters;
                this.heightsDrawing = heights;
                this.roughnessDrawing = roughness;
                this.rainDrawing = rain;
            }
        }

        private Args args;

        private WaterTableField wtf;
        private ContinuousField distanceToWater;
        private SparseField2d<List<SplineTree>> splines;
        private ContinuousMountainNoise mountainNoise;

        public MeterScaleMap(Args args)
        {
            this.args = args;

            Random random = new Random((int)this.args.seed);

            this.wtf = InitializeWaterTableField(this.args, random);
            this.distanceToWater = InitializeDistanceFromWater(this.wtf, this.args);
            this.splines = InitializeSplines(this.wtf, random);
            this.mountainNoise = InitializeMountainNoise(this.wtf, this.args.seed, this.args.metersPerPixel);
        }

        public void OutputMapGrid(float metersPerPixel, string dir, string name, int sourceResolution = 64)
        {
            int ratio = (int)Math.Round(this.args.metersPerPixel / metersPerPixel);

            OutputMapGrid(sourceResolution, sourceResolution * ratio, dir, name);
        }

        public void OutputMapGrid(int sourceResolution, int targetResolution, string dir, string name)
        {
            Bitmap bmp = new Bitmap(targetResolution, targetResolution);
            int bufferSize = (int)Math.Ceiling(sourceResolution / 20f);

            for (int y = bufferSize; y < this.wtf.Height - sourceResolution - bufferSize; y += sourceResolution)
            {
                for (int x = bufferSize; x < this.wtf.Width - sourceResolution - bufferSize; x += sourceResolution)
                {
                    Rectangle rect = new Rectangle(x, y, sourceResolution, sourceResolution);

                    bool isWorthwhile = false;
                    for (int j = rect.Top; !isWorthwhile && j < rect.Bottom; j++)
                    {
                        for (int i = rect.Left; !isWorthwhile && i < rect.Right; i++)
                        {
                            isWorthwhile |= this.wtf.HydroField[j, i] == HydrologicalField.LandType.Land;
                        }
                    }

                    if (!isWorthwhile)
                        continue;

                    OutputMapForRectangle(rect, bmp, dir, name + "_" + x + "_" + y);
                }
            }
        }
        
        public void OutputHighLevelMaps(Bitmap bmp, string outputPath)
        {
            Utils.OutputAsTributaryMap(this.wtf.GeographicFeatures, this.wtf.RiverSystems, this.wtf.DrainageField, bmp, outputPath + "tributaries.png");

            Utils.OutputField(this.wtf, bmp, outputPath + "heightmap.png");

            Utils.OutputAsColoredMap(this.wtf, this.wtf.RiverSystems, bmp, outputPath + "colored_map.png");
        }

        public void OutputMapForRectangle(Rectangle sourceRect, Bitmap bmp, string dir = "C:\\Users\\Justin Murray\\Desktop\\terrain\\", string name = "submap")
        {
            // Hack that adds buffers, use to work around unidentified bugs and differences of behavior near edges.
            Rectangle rect = new Rectangle(
                sourceRect.Left - sourceRect.Width / 20,
                sourceRect.Top - sourceRect.Height / 20,
                sourceRect.Width * 11 / 10,
                sourceRect.Height * 11 / 10);

            int width = (int)Math.Ceiling(bmp.Width * 1.1f);
            int height = (int)Math.Ceiling(bmp.Height * 1.1f);

            IField2d<float> waterTable = new BlurredField(new SubContinuum<float>(width, height, new ContinuousField(this.wtf), rect), 0.5f * width / rect.Width);
            IField2d<float> mountains = new SubContinuum<float>(width, height, this.mountainNoise, rect);
            // TODO: hills?

            IField2d<float> roughness = new BlurredField(new SubContinuum<float>(width, height, new ContinuousField(this.args.roughnessDrawing), rect), 0.5f * width / rect.Width);

            IField2d<float> riverbeds = GetRiverFieldForRectangle(width, height, rect);
            IField2d<float> damping = GetDampingFieldForRectangle(rect, riverbeds);

            IField2d<float> heightmap;
            {
                IField2d<float> dampedNoise = new Transformation2d<float, float, float>(mountains, damping, (m, d) => Math.Max(1f - d, 0f) * m);
                IField2d<float> scaledDampedNoise = new Transformation2d<float, float, float>(dampedNoise, roughness, (n, r) => n * r * this.args.mountainHeightMaxInMeters);
                IField2d<float> groundHeight = new Transformation2d<float, float, float>(waterTable, scaledDampedNoise, (w, m) => w + m);

                // TODO: Erode the groundHeight.

                heightmap = new Transformation2d<float, float, float>(groundHeight, riverbeds, Math.Min);

                //DEBUG
                heightmap = Erosion.DropletHydraulic(heightmap, 2 * heightmap.Width * heightmap.Height, 100, maxHeight: this.args.baseHeightMaxInMeters + this.args.mountainHeightMaxInMeters);
            }

            IField2d<float> riverField = new SubField<float>(riverbeds, new Rectangle(bmp.Width / 20, bmp.Height / 20, bmp.Width, bmp.Height));
            IField2d<float> heightField = new SubField<float>(heightmap, new Rectangle(bmp.Width / 20, bmp.Height / 20, bmp.Width, bmp.Height));

            // TODO: DEBUG
            OutputAsOBJ(heightField, new Transformation2d<float, bool>(riverField, r => !float.IsPositiveInfinity(r)), sourceRect, bmp, dir, name);
            OutputAsPreciseHeightmap(heightField, riverField, dir + name + ".png");
        }

        private static WaterTableField InitializeWaterTableField(Args args, Random random)
        {
            BrownianTree tree = BrownianTree.CreateFromOther(args.watersDrawing, (x) => x > 0.5f ? BrownianTree.Availability.Available : BrownianTree.Availability.Unavailable, random);
            tree.RunDefaultTree();

            HydrologicalField hydro = new HydrologicalField(tree, args.hydroSensitivity, args.hydroShoreThreshold);
            IField2d<float> scaledHeights = new ScaleTransform(args.heightsDrawing, args.baseHeightMaxInMeters);
            WaterTableField wtf = new WaterTableField(scaledHeights, hydro, args.wtfShore, args.wtfIt, args.wtfLen, args.wtfGrade, () =>
            {
                return (float)(args.wtfCarveAdd + random.NextDouble() * args.wtfCarveMul);
            });

            return wtf;
        }

        private static ContinuousField InitializeDistanceFromWater(WaterTableField wtf, Args args)
        {
            var dists = new Transformation2d<Point2d, float>(wtf.DrainageField, (x, y, p) =>
            {
                if (wtf.HydroField[y, x] != HydrologicalField.LandType.Land)
                    return 0f;

                // We subtract 1 to compute the distance as water-adjacent as well as on-the-water.
                // This is a slight bit of a hack, but helps account for problems at varying resolutions.
                return args.metersPerPixel * (Point2d.Distance(new Point2d(x, y), p) - 1f);
            });
            return new ContinuousField(dists);
        }

        private static SparseField2d<List<SplineTree>> InitializeSplines(WaterTableField wtf, Random random)
        {
            var splines = new SparseField2d<List<SplineTree>>(wtf.Width, wtf.Height, null);

            foreach (var system in wtf.RiverSystems)
            {
                SplineTree tree = null;

                foreach (var p in system.value)
                {
                    if (splines[p.value.y, p.value.x] == null)
                        splines[p.value.y, p.value.x] = new List<SplineTree>();

                    splines[p.value.y, p.value.x].Add(tree ?? (tree = new SplineTree(system.value, wtf, random)));
                }
            }

            return splines;
        }

        private static ContinuousMountainNoise InitializeMountainNoise(WaterTableField wtf, long seed, float metersPerPixel)
        {
            return new ContinuousMountainNoise(wtf.Width, wtf.Height, GetMountainNoiseStartingScale(metersPerPixel), seed);
        }

        private List<SplineTree> GetSplinesInRectangle(Rectangle rect)
        {
            List<SplineTree> localSplines = new List<SplineTree>();
            
            // Collect a comprehensive list of the spline trees for the local frame.
            for (int y = rect.Top - 1; y <= rect.Bottom + 1; y++)
            {
                for (int x = rect.Left - 1; x <= rect.Right + 1; x++)
                {
                    List<SplineTree> trees = this.splines[y, x];
                    if (trees != null)
                        localSplines.AddRange(trees);
                }
            }

            return localSplines;
        }

        // Gets the exact river heights for the given rectangle. Does NOT produce a valley or canyon kernel.
        private IField2d<float> GetRiverFieldForRectangle(int newWidth, int newHeight, Rectangle sourceRect)
        {
            float metersPerPixel = this.args.metersPerPixel * sourceRect.Width / newWidth;

            var localSplines = GetSplinesInRectangle(sourceRect);

            var riverField = new Field2d<float>(new ConstantField<float>(newWidth, newHeight, float.PositiveInfinity));
            foreach (var s in localSplines)
            {
                var samples = s.GetSamplesPerControlPoint(1f * newWidth / sourceRect.Width);

                int priorX = int.MinValue;
                int priorY = int.MinValue;

                foreach (var p in samples)
                {
                    int x = (int)((p[0] - sourceRect.Left) * newWidth / sourceRect.Width);
                    int y = (int)((p[1] - sourceRect.Top) * newHeight / sourceRect.Height);

                    if (x == priorX && y == priorY)
                    {
                        continue;
                    }
                    else
                    {
                        priorX = x;
                        priorY = y;
                    }

                    if (0 <= x && x < newWidth && 0 <= y && y < newHeight)
                    {
                        float riverRadiusInPixels = this.args.riverCapacityToMetersWideFunc(p[3]) / metersPerPixel / 2f;
                        int l = -(int)(riverRadiusInPixels + 0.5f);
                        int r = (int)riverRadiusInPixels;
                        
                        for (int j = l; j <= r; j++)
                        {
                            for (int i = l; i <= r; i++)
                            {
                                int xx = x + i;
                                int yy = y + j;

                                if (0 <= xx && xx < newWidth && 0 <= yy && yy < newHeight && riverField[yy, xx] > p[2])
                                {
                                    riverField[yy, xx] = p[2];
                                }
                            }
                        }
                    }
                }
            }

            return riverField;
        }

        private IField2d<float> GetDampingFieldForRectangle(Rectangle rect, IField2d<float> riverbeds)
        {
            float metersPerPixel = this.args.metersPerPixel * rect.Width / riverbeds.Width;
            
            IField2d<float> upres = new BlurredField(new SubContinuum<float>(riverbeds.Width, riverbeds.Height, this.distanceToWater, rect), riverbeds.Width / rect.Width);
            IField2d<float> manhats = new ScaleTransform(new ManhattanDistanceField(new Transformation2d<float, bool>(riverbeds, r => !float.IsPositiveInfinity(r))), metersPerPixel);

            IField2d<float> dists = new BlurredField(new Transformation2d<float, float, float>(upres, manhats, Math.Min), 200f / metersPerPixel);
            
            return new Transformation2d(dists, d =>
            {
                float valleyFactor = 1f - d / this.args.valleyRadiusInMeters;
                float canyonFactor = 1f - (float)Math.Pow(d / this.args.canyonRadiusInMeters, 2f);
                return Math.Max(Math.Max(valleyFactor * this.args.valleyStrength, canyonFactor * this.args.canyonStrength), 0f);
            });
        }

        /// <summary>
        /// These mountains generally want to be around 2048 meters in prominence, or
        /// a little shorter.  Too much shorter and they'll be pretty shallow (and at
        /// that point should probably be replaced with hills), too much taller and
        /// they'll be VERY steep indeed.  Note that steepness can be adjusted with the
        /// parameter--larger than 1 is steeper, smaller is shallower.
        /// </summary>
        private static float GetMountainNoiseStartingScale(float metersPerPixel, float steepness = 1f)
        {
            return steepness * 0.00016f * metersPerPixel;
        }

        public void OutputAsPreciseHeightmap(IField2d<float> heights, IField2d<float> rivers, string outputPath)
        {
            Bitmap bmp = new Bitmap(heights.Width, heights.Height);

            for (int y = 0; y < heights.Height; y++)
            {
                for (int x = 0; x < heights.Width; x++)
                {
                    float h = Math.Max(heights[y, x], 0f);
                    float r = Math.Max(rivers[y, x], 0f);
                    
                    int m1k = (int)(h / 1000);
                    int m10 = (int)((h - m1k * 1000) / 10);
                    int m_1 = (int)((h - m1k * 1000 - m10 * 10) * 10);
                    int w = float.IsPositiveInfinity(r) ? 255 : 128;

                    bmp.SetPixel(x, y, Color.FromArgb(w, m1k, m10, m_1));
                }
            }

            bmp.Save(outputPath);
        }

        public void OutputAsOBJ(IField2d<float> heights, IField2d<bool> riverField, Rectangle rect, Bitmap bmp, string outputDir, string outputName = "terrain")
        {
            using (var objWriter = new StreamWriter(File.OpenWrite(outputDir + outputName + ".obj")))
            {
                objWriter.WriteLine("mtllib " + outputName + ".mtl");
                objWriter.WriteLine("o " + outputName + "_o");

                float metersPerPixel = this.args.metersPerPixel * rect.Width / this.wtf.Width;
                IField2d<vFloat> verts = new Transformation2d<float, vFloat>(heights,
                    (x, y, z) => new vFloat(x * metersPerPixel, -y * metersPerPixel, z));

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
            
            for (int x = 0, y = 0; y < heights.Height; y += ++x / heights.Width, x %= heights.Width)
            {
                float value = heights[y, x] / (this.args.baseHeightMaxInMeters + this.args.mountainHeightMaxInMeters);
                Color color;
                if (riverField[y, x] || value < 0.05f)
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
            bmp.Save(outputDir + outputName + ".jpg");
        }
    }
}
