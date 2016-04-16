// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Roslyn.Utilities;

namespace Roslyn.Test.EditorUtilities.NavigateTo
{
    public sealed partial class NavigateToTestAggregator
    {
        private sealed class Callback : INavigateToCallback
        {
            private readonly INavigateToOptions _options;
            private readonly ConcurrentBag<NavigateToItem> _itemsReceived;

            /// <summary>
            /// A manual reset event that is signaled once INavigateToCallback's Done method has
            /// been called. This indicates the provider is done providing events, and thus the
            /// items in itemsReceived is complete.
            /// </summary>
            private readonly ManualResetEvent _doneCalled;

            public Callback(INavigateToOptions options)
            {
                Contract.ThrowIfNull(options);

                _options = options;
                _itemsReceived = new ConcurrentBag<NavigateToItem>();
                _doneCalled = new ManualResetEvent(initialState: false);
            }

            public void AddItem(NavigateToItem item)
            {
                _itemsReceived.Add(item);
            }

            public void Done()
            {
                _doneCalled.Set();
            }

            public void Invalidate()
            {
                throw new InvalidOperationException("Unexpected call to Invalidate.");
            }

            public IEnumerable<NavigateToItem> GetItemsSynchronously()
            {
                _doneCalled.WaitOne();
                var items = _itemsReceived.ToArray();

                NavigateToItem dummy;
                while (_itemsReceived.TryTake(out dummy))
                {
                    // do nothing.
                }

                return items;
            }

            public INavigateToOptions Options
            {
                get { return _options; }
            }

            public void ReportProgress(int current, int maximum)
            {
            }
        }
    }
}
