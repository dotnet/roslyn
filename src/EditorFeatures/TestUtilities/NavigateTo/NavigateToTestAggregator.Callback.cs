// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;

namespace Roslyn.Test.EditorUtilities.NavigateTo;

public sealed partial class NavigateToTestAggregator
{
    private sealed class Callback : INavigateToCallback
    {
        private readonly List<NavigateToItem> _itemsReceived = [];

        private readonly TaskCompletionSource<IEnumerable<NavigateToItem>> _taskCompletionSource = new();

        public Callback(INavigateToOptions options)
        {
            Contract.ThrowIfNull(options);

            Options = options;
        }

        public void AddItem(NavigateToItem item)
        {
            lock (_itemsReceived)
                _itemsReceived.Add(item);
        }

        public void Done()
            => _taskCompletionSource.SetResult(_itemsReceived);

        public void Invalidate()
            => throw new InvalidOperationException("Unexpected call to Invalidate.");

        public Task<IEnumerable<NavigateToItem>> GetItemsAsync()
            => _taskCompletionSource.Task;

        public INavigateToOptions Options { get; }

        public void ReportProgress(int current, int maximum)
        {
        }
    }
}
