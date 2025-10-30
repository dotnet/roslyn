// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.Completion;

internal interface IContextProvider
{
    ValueTask ProvideContextItemsAsync(
        Document document,
        int position,
        IReadOnlyDictionary<string, object> activeExperiments,
        Func<ImmutableArray<IContextItem>, CancellationToken, ValueTask> callback,
        CancellationToken cancellationToken);
}
