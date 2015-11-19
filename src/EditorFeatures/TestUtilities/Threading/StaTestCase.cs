using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Xunit.Abstractions;
using Xunit.Sdk;

namespace Roslyn.Test.Utilities
{
    public class StaTestCase : XunitTestCase
    {
        public StaTestCase(IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, ITestMethod testMethod, object[] testMethodArguments = null)
            : base(diagnosticMessageSink, defaultMethodDisplay, testMethod, testMethodArguments) { }

        public override Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink, IMessageBus messageBus, object[] constructorArguments, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
            => Task.Factory.StartNew(() =>
            {
                // All of the tests which use StaTestCase require an STA thread, so assert that we are 
                // actually running from an STA Thread (which should be from the StaTaskScheduler pool).
                Debug.Assert(Thread.CurrentThread.GetApartmentState() == ApartmentState.STA);

                // xUnit will set and clean up its own context, so assert that none is currently
                // set. If our assert fails, then something wasn't cleaned up properly...
                Debug.Assert(SynchronizationContext.Current == null);

                // Just call back into the normal xUnit dispatch process now that we are on an STA Thread with no synchronization context.
                return base.RunAsync(diagnosticMessageSink, messageBus, constructorArguments, aggregator, cancellationTokenSource).Result;
            }, CancellationToken.None, TaskCreationOptions.None, StaTaskScheduler.DefaultSta);
    }
}
