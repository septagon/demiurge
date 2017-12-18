using DemiurgeLib;
using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using static DemiurgeLib.Common.Utils;

namespace DemiurgeApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        IField2d<float> waters;
        IField2d<float> heights;
        IField2d<float> roughness;
        IField2d<float> rain;
        string outputDir;
        string outputPrefix;
        float metersPerPixelOut;
        int outputSourceResolution;
        private MeterScaleMap msm = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BuildMeterScaleMapFromArguments()
        {
            this.waters = File.Exists(this.WatersInput.Text) ? new FieldFromBitmap(new Bitmap(this.WatersInput.Text)) : null;
            this.heights = File.Exists(this.HeightsInput.Text) ? new FieldFromBitmap(new Bitmap(this.HeightsInput.Text)) : null;
            this.roughness = File.Exists(this.RoughnessInput.Text) ? new FieldFromBitmap(new Bitmap(this.RoughnessInput.Text)) : null;
            this.rain = File.Exists(this.RainInput.Text) ? new FieldFromBitmap(new Bitmap(this.RainInput.Text)) : null;
            var args = new MeterScaleMap.Args(this.waters, this.heights, this.roughness, this.rain);

            if (!long.TryParse(this.Seed.Text, out args.seed)) args.seed = 0;
            if (!float.TryParse(this.MetersPerPixelIn.Text, out args.metersPerPixel)) args.metersPerPixel = 1600f;
            if (!float.TryParse(this.HeightsMeterScale.Text, out args.baseHeightMaxInMeters)) args.baseHeightMaxInMeters = 2000f;
            if (!float.TryParse(this.RoughnessMeterScale.Text, out args.mountainHeightMaxInMeters)) args.mountainHeightMaxInMeters = 2000f;
            if (!float.TryParse(this.ValleyRadiusMeters.Text, out args.valleyRadiusInMeters)) args.valleyRadiusInMeters = 5000f;
            if (!float.TryParse(this.CanyonRadiusMeters.Text, out args.canyonRadiusInMeters)) args.canyonRadiusInMeters = 1000f;
            if (!float.TryParse(this.ErosionRadiusMeters.Text, out args.erosionRadiusInMeters)) args.erosionRadiusInMeters = 50f;
            args.riverCapacityToMetersWideFunc = c => (float)Math.Pow(args.metersPerPixel * SplineTree.CAPACITY_DIVISOR * c, 0.5f) / 4f;

            if (!float.TryParse(this.ValleyStrength.Text, out args.valleyStrength)) args.valleyStrength = 0.8f;
            if (!float.TryParse(this.CanyonStrength.Text, out args.canyonStrength)) args.canyonStrength = 0.999f;

            if (!int.TryParse(this.HydroSensitivity.Text, out args.hydroSensitivity)) args.hydroSensitivity = 8;
            if (!float.TryParse(this.WtfShoreThreshold.Text, out args.hydroShoreThreshold)) args.hydroShoreThreshold = 0.5f;
            if (!float.TryParse(this.RiverSeparationMeters.Text, out args.riverSeparationMeters)) args.riverSeparationMeters = 5000;
            if (!float.TryParse(this.WtfShore.Text, out args.wtfShore)) args.wtfShore = 0.01f;
            if (!int.TryParse(this.WtfIt.Text, out args.wtfIt)) args.wtfIt = 10;
            if (!int.TryParse(this.WtfLen.Text, out args.wtfLen)) args.wtfLen = 5;
            if (!float.TryParse(this.WtfGrade.Text, out args.wtfGrade)) args.wtfGrade = 0f;
            if (!float.TryParse(this.WtfCarve.Text, out args.wtfCarveAdd)) args.wtfCarveAdd = 0.3f;
            if (!float.TryParse(this.WtfMultiplier.Text, out args.wtfCarveMul)) args.wtfCarveMul = 1.3f;

            this.outputDir = this.OutputDirectory.Text;
            this.outputPrefix = this.OutputSubmapPrefix.Text;
            this.metersPerPixelOut = float.Parse(this.MetersPerPixelOut.Text);
            this.outputSourceResolution = int.Parse(this.OutputSourceResolution.Text);

            this.msm = new MeterScaleMap(args);
        }

        private void RegenerateMeterScaleMap(object sender, RoutedEventArgs e)
        {
            BuildMeterScaleMapFromArguments();
            this.GenerateSubmapButton.IsEnabled = true;
            this.RunFullMapButton.IsEnabled = true;
        }

        private void GenerateSubmap(object sender, RoutedEventArgs e)
        {
            int x = int.Parse(this.SubmapX.Text);
            int y = int.Parse(this.SubmapY.Text);
            int w = int.Parse(this.SubmapW.Text);
            int h = int.Parse(this.SubmapH.Text);
            int urf = int.Parse(this.SubmapRes.Text);
            
            this.msm.OutputMapForRectangle(
                new Rectangle(x, y, w, h),
                new Bitmap(w * urf, h * urf),
                this.outputDir,
                this.outputPrefix
                );
        }

        private void RunFullMap(object sender, RoutedEventArgs e)
        {
            this.msm.OutputHighLevelMaps(new Bitmap(this.waters.Width, this.waters.Height), outputDir);
            this.msm.OutputMapGrid(metersPerPixelOut, outputDir, outputPrefix, outputSourceResolution);
        }
    }
}
