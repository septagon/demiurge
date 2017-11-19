using DemiurgeLib;
using DemiurgeLib.Common;
using System;
using System.Drawing;
using System.IO;
using System.Windows;
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

        private void RunMeterScaleMapScenario(object sender, RoutedEventArgs e)
        {
            var wta = new WaterTableArgs();
            wta.inputPath = "C:\\Users\\Justin Murray\\Desktop\\egwethoon\\input\\";
            var waters = new FieldFromBitmap(new Bitmap(wta.inputPath + "coastline.png"));
            var heights = new FieldFromBitmap(new Bitmap(wta.inputPath + "heights.png"));
            var roughness = new FieldFromBitmap(new Bitmap(wta.inputPath + "roughness.png"));
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
