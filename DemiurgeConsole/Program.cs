using DemiurgeLib;
using DemiurgeLib.Common;
using DemiurgeLib.Noise;
using System;
using System.Drawing;
using System.Threading;
using static DemiurgeConsole.Utils;

namespace DemiurgeConsole
{
    public class Program
    {
        const int StackSize = 10485760;

        static void Main(string[] args)
        {
            //TestScenarios.RunWateryScenario();
            //new Thread(TestScenarios.RunWaterHeightScenario, StackSize).Start();
            //TestScenarios.RunMountainousScenario(1024, 1024, 0.005f);
            //new Thread(TestScenarios.RunPopulationScenario, StackSize).Start();
            //TestScenarios.RunSplineScenario();
            //TestScenarios.RunZoomedInScenario();
            //TestScenarios.RunPathScenario();

            var wta = new WaterTableArgs();
            var waters = new FieldFromBitmap(new Bitmap(wta.inputPath + "rivers.png"));
            var heights = new FieldFromBitmap(new Bitmap(wta.inputPath + "base_heights.png"));
            var msmArgs = new MeterScaleMap.Args(waters, heights, null);
            msmArgs.seed = System.DateTime.UtcNow.Ticks;
            var msm = new MeterScaleMap(msmArgs);

            msm.OutputMapGrid(100, "C:\\Users\\Justin Murray\\Desktop\\terrain\\", "submap");
        }
    }
}
