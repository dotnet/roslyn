// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SemanticSearch;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SemanticSearch;

internal sealed class MockSemanticSearchResultsObserver() : ISemanticSearchResultsObserver
{
    public Action<ISymbol>? OnDefinitionFoundImpl { get; set; }
    public Action<UserCodeExceptionInfo>? OnUserCodeExceptionImpl { get; set; }
    public Action<ImmutableArray<QueryCompilationError>>? OnCompilationFailureImpl { get; set; }
    public Action<int>? ItemsCompletedImpl { get; set; }
    public Action<int>? AddItemsImpl { get; set; }

    public ValueTask AddItemsAsync(int itemCount, CancellationToken cancellationToken)
    {
        AddItemsImpl?.Invoke(itemCount);
        return ValueTaskFactory.CompletedTask;
    }

    public ValueTask ItemsCompletedAsync(int itemCount, CancellationToken cancellationToken)
    {
        ItemsCompletedImpl?.Invoke(itemCount);
        return ValueTaskFactory.CompletedTask;
    }

    public ValueTask OnSymbolFoundAsync(Solution solution, ISymbol symbol, CancellationToken cancellationToken)
    {
        OnDefinitionFoundImpl?.Invoke(symbol);
        return ValueTaskFactory.CompletedTask;
    }

    public ValueTask OnUserCodeExceptionAsync(UserCodeExceptionInfo exception, CancellationToken cancellationToken)
    {
        OnUserCodeExceptionImpl?.Invoke(exception);
        return ValueTaskFactory.CompletedTask;
    }
}

