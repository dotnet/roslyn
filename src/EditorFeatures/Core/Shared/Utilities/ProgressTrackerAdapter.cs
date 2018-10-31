// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    /// <summary>
    /// An adapter between editor's <see cref="IUIThreadOperationScope"/> (which supports reporting
    /// progress) and <see cref="IProgressTracker"/>.
    /// </summary>
    internal class ProgressTrackerAdapter : IProgressTracker
    {
        private readonly IUIThreadOperationScope _uiThreadOperationScope;
        private int _completedItems;
        private int _totalItems;
        private string _description;

        public ProgressTrackerAdapter(IUIThreadOperationScope uiThreadOperationScope)
        {
            Requires.NotNull(uiThreadOperationScope, nameof(uiThreadOperationScope));
            _uiThreadOperationScope = uiThreadOperationScope;
        }

        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                _uiThreadOperationScope.Description = value;
            }
        }

        public int CompletedItems => _completedItems;

        public int TotalItems => _totalItems;

        public void AddItems(int count)
        {
            Interlocked.Add(ref _totalItems, count);
            ReportProgress();
        }

        public void Clear()
        {
            Interlocked.Exchange(ref _completedItems, 0);
            Interlocked.Exchange(ref _totalItems, 0);
            ReportProgress();
        }

        public void ItemCompleted()
        {
            Interlocked.Increment(ref _completedItems);
            ReportProgress();
        }

        private void ReportProgress()
            => _uiThreadOperationScope.Progress.Report(new ProgressInfo(_completedItems, _totalItems));
    }
}
