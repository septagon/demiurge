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
        public MainWindow()
        {
            InitializeComponent();
        }

        private void RunMeterScaleMapFromArguments(object sender, RoutedEventArgs e)
        {
            var waters = File.Exists(this.WatersInput.Text) ? new FieldFromBitmap(new Bitmap(this.WatersInput.Text)) : null;
            var heights = File.Exists(this.HeightsInput.Text) ? new FieldFromBitmap(new Bitmap(this.HeightsInput.Text)) : null;
            var roughness = File.Exists(this.RoughnessInput.Text) ? new FieldFromBitmap(new Bitmap(this.RoughnessInput.Text)) : null;
            var rain = File.Exists(this.RainInput.Text) ? new FieldFromBitmap(new Bitmap(this.RainInput.Text)) : null;
            var args = new MeterScaleMap.Args(waters, heights, roughness, rain);

            if (!long.TryParse(this.Seed.Text, out args.seed)) args.seed = 0;
            if (!float.TryParse(this.MetersPerPixelIn.Text, out args.metersPerPixel)) args.metersPerPixel = 1600f;
            if (!float.TryParse(this.HeightsMeterScale.Text, out args.baseHeightMaxInMeters)) args.baseHeightMaxInMeters = 2000f;
            if (!float.TryParse(this.RoughnessMeterScale.Text, out args.mountainHeightMaxInMeters)) args.mountainHeightMaxInMeters = 2000f;
            if (!float.TryParse(this.ValleyRadiusMeters.Text, out args.valleyRadiusInMeters)) args.valleyRadiusInMeters = 5000f;
            if (!float.TryParse(this.CanyonRadiusMeters.Text, out args.canyonRadiusInMeters)) args.canyonRadiusInMeters = 1000f;
            if (!float.TryParse(this.ErosionRadiusMeters.Text, out args.erosionRadiusInMeters)) args.erosionRadiusInMeters = 50f;

            if (!float.TryParse(this.ValleyStrength.Text, out args.valleyStrength)) args.valleyStrength = 0.8f;
            if (!float.TryParse(this.CanyonStrength.Text, out args.canyonStrength)) args.canyonStrength = 0.999f;

            if (!int.TryParse(this.HydroSensitivity.Text, out args.hydroSensitivity)) args.hydroSensitivity = 8;
            if (!float.TryParse(this.WtfShoreThreshold.Text, out args.hydroShoreThreshold)) args.hydroShoreThreshold = 0.5f;
            if (!float.TryParse(this.WtfShore.Text, out args.wtfShore)) args.wtfShore = 0.01f;
            if (!int.TryParse(this.WtfIt.Text, out args.wtfIt)) args.wtfIt = 10;
            if (!int.TryParse(this.WtfLen.Text, out args.wtfLen)) args.wtfLen = 5;
            if (!float.TryParse(this.WtfGrade.Text, out args.wtfGrade)) args.wtfGrade = 0f;
            if (!float.TryParse(this.WtfCarve.Text, out args.wtfCarveAdd)) args.wtfCarveAdd = 0.3f;
            if (!float.TryParse(this.WtfMultiplier.Text, out args.wtfCarveMul)) args.wtfCarveMul = 1.3f;

            string outputDir = this.OutputDirectory.Text;
            string outputPrefix = this.OutputSubmapPrefix.Text;
            float metersPerPixelOut = float.Parse(this.MetersPerPixelOut.Text);
            int outputSourceResolution = int.Parse(this.OutputSourceResolution.Text);

            var msm = new MeterScaleMap(args);
            msm.OutputHighLevelMaps(new Bitmap(waters.Width, waters.Height), outputDir);
            msm.OutputMapGrid(metersPerPixelOut, outputDir, outputPrefix, outputSourceResolution);
        }

        private void RunMeterScaleMapScenario(object sender, RoutedEventArgs e)
        {
            string inputPath = "C:\\Users\\Justin Murray\\Desktop\\egwethoon\\input\\";
            var waters = new FieldFromBitmap(new Bitmap(inputPath + "coastline.png"));
            var heights = new FieldFromBitmap(new Bitmap(inputPath + "heights.png"));
            var roughness = new FieldFromBitmap(new Bitmap(inputPath + "roughness.png"));
            var msmArgs = new MeterScaleMap.Args(waters, heights, roughness, null);
            msmArgs.seed = System.DateTime.UtcNow.Ticks;
            msmArgs.metersPerPixel = 800;
            msmArgs.riverCapacityToMetersWideFunc = c => (float)Math.Pow(msmArgs.metersPerPixel * SplineTree.CAPACITY_DIVISOR * c, 0.5f) / 4f;
            msmArgs.baseHeightMaxInMeters = 500;
            msmArgs.valleyStrength = 0.98f;
            var msm = new MeterScaleMap(msmArgs);
            
            msm.OutputHighLevelMaps(new Bitmap(waters.Width, waters.Height), "C:\\Users\\Justin Murray\\Desktop\\egwethoon\\");
            msm.OutputMapGrid(100, "C:\\Users\\Justin Murray\\Desktop\\egwethoon\\", "submap", 32);
        }
    }
}
