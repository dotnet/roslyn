// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CommonLanguageServerProtocol.Framework.UnitTests;

internal class TestRequestContext
{
    internal class Factory : AbstractRequestContextFactory<TestRequestContext>
    {
        public static readonly Factory Instance = new();

        public override Task<TestRequestContext> CreateRequestContextAsync<TRequestParam>(IQueueItem<TestRequestContext> queueItem, IMethodHandler methodHandler, TRequestParam requestParam, CancellationToken cancellationToken)
            => Task.FromResult(new TestRequestContext());
    }
}
