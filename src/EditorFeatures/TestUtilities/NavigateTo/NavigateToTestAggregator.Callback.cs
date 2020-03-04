﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Roslyn.Utilities;

namespace Roslyn.Test.EditorUtilities.NavigateTo
{
    public sealed partial class NavigateToTestAggregator
    {
        private sealed class Callback : INavigateToCallback
        {
            private readonly ConcurrentBag<NavigateToItem> _itemsReceived;

            private readonly TaskCompletionSource<IEnumerable<NavigateToItem>> _taskCompletionSource =
                new TaskCompletionSource<IEnumerable<NavigateToItem>>();

            public Callback(INavigateToOptions options)
            {
                Contract.ThrowIfNull(options);

                Options = options;
                _itemsReceived = new ConcurrentBag<NavigateToItem>();
            }

            public void AddItem(NavigateToItem item)
            {
                _itemsReceived.Add(item);
            }

            public void Done()
            {
                _taskCompletionSource.SetResult(_itemsReceived);
            }

            public void Invalidate()
            {
                throw new InvalidOperationException("Unexpected call to Invalidate.");
            }

            public Task<IEnumerable<NavigateToItem>> GetItemsAsync()
                => _taskCompletionSource.Task;

            public INavigateToOptions Options { get; }

            public void ReportProgress(int current, int maximum)
            {
            }
        }
    }
}
