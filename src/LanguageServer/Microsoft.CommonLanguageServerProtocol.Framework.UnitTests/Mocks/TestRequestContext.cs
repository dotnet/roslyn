// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CommonLanguageServerProtocol.Framework.UnitTests;

internal sealed class TestRequestContext
{
    internal sealed class Factory : AbstractRequestContextFactory<TestRequestContext>
    {
        public static readonly Factory Instance = new();

        public override async Task<TestRequestContext> CreateRequestContextAsync<TRequestParam>(IQueueItem<TestRequestContext> queueItem, IMethodHandler methodHandler, TRequestParam requestParam, CancellationToken cancellationToken)
            => new TestRequestContext();
    }
}
