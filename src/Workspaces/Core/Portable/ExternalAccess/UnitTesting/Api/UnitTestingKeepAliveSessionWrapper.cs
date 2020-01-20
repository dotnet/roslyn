﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal readonly struct UnitTestingKeepAliveSessionWrapper
    {
        internal UnitTestingKeepAliveSessionWrapper(KeepAliveSession underlyingObject)
            => UnderlyingObject = underlyingObject ?? throw new ArgumentNullException(nameof(underlyingObject));

        internal KeepAliveSession UnderlyingObject { get; }

        public async Task<T> TryInvokeAsync<T>(string targetName, Solution solution, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
        {
            var result = await UnderlyingObject.TryInvokeAsync<T>(targetName, solution, arguments, cancellationToken).ConfigureAwait(false);
            return result.HasValue ? result.Value : default;
        }

        public Task<bool> TryInvokeAsync(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
            => UnderlyingObject.TryInvokeAsync(targetName, arguments, cancellationToken);

        public Task<bool> TryInvokeAsync(string targetName, Solution solution, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
            => UnderlyingObject.TryInvokeAsync(targetName, solution, arguments, cancellationToken);

        public async Task<T> TryInvokeAsync<T>(string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
        {
            var result = await UnderlyingObject.TryInvokeAsync<T>(targetName, arguments, cancellationToken).ConfigureAwait(false);
            return result.HasValue ? result.Value : default;
        }
    }
}
