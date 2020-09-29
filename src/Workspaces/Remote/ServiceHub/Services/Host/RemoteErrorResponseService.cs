// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteErrorResponseService : BrokeredServiceBase, IRemoteErrorResponseService
    {
        public RemoteErrorResponseService(in ServiceConstructionArguments arguments)
            : base(arguments)
        {
        }

        public ValueTask FailFastAsync(string message, CancellationToken cancellationToken)
        {
            Environment.FailFast(message);
            return default;
        }

        internal sealed class Factory : FactoryBase<IRemoteErrorResponseService>
        {
            protected override IRemoteErrorResponseService CreateService(in ServiceConstructionArguments arguments)
                => new RemoteErrorResponseService(arguments);
        }
    }
}
