using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace ProxySetter.Tests
{
    [TestClass]
    public class CancelableTaskTests
    {
        void Worker()
        {
            while (true)
            {
                //Console.WriteLine("hello");
                Wait(300);
            }
        }

        void Wait(int milSeconds)
        {
            Task.Delay(milSeconds).Wait();
        }

        [TestMethod]
        public void RunTaskTest()
        {
#if DEBUG
            var cancelableTask = new Libs.Sys.CancelableTask(Worker);
            cancelableTask.Start();
            Wait(1000);
            cancelableTask.Stop();
            Wait(1000);
            cancelableTask.Start();
            Wait(1000);
            cancelableTask.Stop();
            Wait(1000);
#endif
        }
    }
}
