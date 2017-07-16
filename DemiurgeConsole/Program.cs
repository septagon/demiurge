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

            const int SOURCE_DIM = 256;
            
            for (int y = 16; y < waters.Height - SOURCE_DIM - 16; y += SOURCE_DIM)
            {
                //new Thread(() =>
                //{
                    // This check is necessary because of what appears to be an EXTREMELY 
                    // subtle behavior in the way the nested loops are being handled.
                    // Stepping through in the debugger, it appears that, sometimes, this
                    // inner thread is being started BEFORE THE OUTER LOOP has had the 
                    // chance to actually evaluate that the "y" provided is actually illegal.
                    // Presumably, the runtime does this for efficiency and simply intends
                    // to throw away the result of the unintended iteration; but in this case,
                    // the unintended iteration actually causes a crash and so must be manually
                    // checked against.  Can't decide if this is a bug in .NET, or just a 
                    // really weird behavior.
                    if (y >= waters.Height - SOURCE_DIM - 16)
                        return;

                    Bitmap bmp = new Bitmap(1024, 1024);
                    for (int x = 16; x < waters.Width - SOURCE_DIM - 16; x += SOURCE_DIM)
                    {
                        Rectangle rect = new Rectangle(x, y, SOURCE_DIM, SOURCE_DIM);

                        bool isWorthwhile = false;
                        for (int j = rect.Top; !isWorthwhile && j < rect.Bottom; j++)
                        {
                            for (int i = rect.Left; !isWorthwhile && i < rect.Right; i++)
                            {
                                isWorthwhile |= msm.wtf.HydroField[j, i] == HydrologicalField.LandType.Land;
                            }
                        }
                        if (!isWorthwhile)
                            continue;

                        msm.OutputMapForRectangle(rect, bmp, name: "submap_" + rect.Left + "_" + rect.Top);
                    }
                //}).Start();
            }
        }
    }
}
