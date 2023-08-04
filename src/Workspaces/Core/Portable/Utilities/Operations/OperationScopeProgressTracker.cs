//// Licensed to the .NET Foundation under one or more agreements.
//// The .NET Foundation licenses this file to you under the MIT license.
//// See the LICENSE file in the project root for more information.

//using Microsoft.CodeAnalysis.Shared.Utilities;

//namespace Microsoft.CodeAnalysis.Utilities
//{
//    /// <summary>
//    /// Wrapper around an <see cref="ILongRunningOperationScope"/> to bridge it to an <see cref="IProgressTracker"/>.
//    /// </summary>
//    internal sealed class LongRunningOperationScopeProgressTracker(ILongRunningOperationScope operationScope) : IProgressTracker
//    {
//        private readonly object _gate = new();
//        private int _completedItems;
//        private int _totalItems;

//        public string? Description { get => operationScope.Description; set => operationScope.Description = value ?? ""; }

//        public int CompletedItems => _completedItems;

//        public int TotalItems => _totalItems;

//        public void AddItems(int count)
//        {
//            int completedItems, totalItems;
//            lock (_gate)
//            {
//                _totalItems += count;
//                completedItems = _completedItems;
//                totalItems = _totalItems;
//            }

//            operationScope.Progress.Report(new LongRunningOperationProgress(completedItems, totalItems));
//        }

//        public void ItemCompleted()
//        {
//            int completedItems, totalItems;
//            lock (_gate)
//            {
//                _completedItems++;
//                completedItems = _completedItems;
//                totalItems = _totalItems;
//            }

//            operationScope.Progress.Report(new LongRunningOperationProgress(completedItems, totalItems));
//        }

//        public void Clear()
//        {
//            lock (_gate)
//            {
//                _completedItems = 0;
//                _totalItems = 0;
//            }

//            operationScope.Progress.Report(new LongRunningOperationProgress(0, 0));
//        }
//    }
//}
