using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal abstract class UnitTestingServiceHubBase : ServiceHubServiceBase
    {
        public UnitTestingServiceHubBase(IServiceProvider serviceProvider, Stream stream) : base(serviceProvider, stream)
        {
        }

        protected Task<Solution> GetSolutionAsync(UnitTestingPinnedSolutionInfoWrapper pinnedSolutionInfo, CancellationToken cancellationToken)
            => GetSolutionAsync(pinnedSolutionInfo.UnderlyingObject, cancellationToken);

        protected void UnitTesting_StartService()
            => base.StartService();

        protected Task<T> UnitTesting_RunServiceAsync<T>(Func<Task<T>> callAsync, CancellationToken cancellationToken)
            => base.RunServiceAsync<T>(callAsync, cancellationToken);

        protected Task UnitTesting_RunServiceAsync<T>(Func<Task> callAsync, CancellationToken cancellationToken)
            => base.RunServiceAsync(callAsync, cancellationToken);

        protected void UnitTesing_LogException(Exception e)
            => base.LogException(e);

    }
}
