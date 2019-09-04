using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal readonly struct UnitTestingKeepAliveSessionWrapper
    {
        internal UnitTestingKeepAliveSessionWrapper(KeepAliveSession underlyingObject)
            => UnderlyingObject = underlyingObject;

        internal KeepAliveSession UnderlyingObject { get; }

        public Task<bool> TryInvokeAsync(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
            => UnderlyingObject.TryInvokeAsync(targetName, arguments, cancellationToken);

        public Task<bool> TryInvokeAsync(string targetName, Solution solution, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
            => UnderlyingObject.TryInvokeAsync(targetName, solution, arguments, cancellationToken);

        public Task<T> TryInvokeAsync<T>(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
            => UnderlyingObject.TryInvokeAsync<T>(targetName, arguments, cancellationToken);
    }
}
