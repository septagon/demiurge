using System.Threading;

namespace DemiurgeConsole
{
    public class Program
    {
        const int StackSize = 10485760;

        static void Main(string[] args)
        {
            TestScenarios.RunWateryScenario();
            new Thread(TestScenarios.RunWaterHeightScenario, StackSize).Start();
            TestScenarios.RunMountainousScenario(1024, 1024, 0.005f);
            new Thread(TestScenarios.RunPopulationScenario, StackSize).Start();
            TestScenarios.RunSplineScenario();
            TestScenarios.RunZoomedInScenario();
        }
    }
}
