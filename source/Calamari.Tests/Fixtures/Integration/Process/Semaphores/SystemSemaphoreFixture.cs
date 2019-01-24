using Calamari.Integration.Processes.Semaphores;
using Calamari.Tests.Helpers;
using NUnit.Framework;

namespace Calamari.Tests.Fixtures.Integration.Process.Semaphores
{
    [TestFixture]
    public class SystemSemaphoreFixture : SemaphoreFixtureBase
    {
        [Test]
        [Category(TestCategory.CompatibleOS.Windows)]
        public void SystemSemaphoreWaitsUntilFirstSemaphoreIsReleased()
        {
            SecondSemaphoreWaitsUntilFirstSemaphoreIsReleased(new SystemSemaphoreManager());
        }

        [Test]
        [Category(TestCategory.CompatibleOS.Windows)]
        public void SystemSemaphoreShouldIsolate()
        {
            ShouldIsolate(new SystemSemaphoreManager());
        }
    }
}