// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Progress;

internal static class IUIThreadOperationContextExtensions
{
    public static IProgress<CodeAnalysisProgress> GetCodeAnalysisProgress(this IUIThreadOperationContext context)
        => context.Scopes.LastOrDefault().GetCodeAnalysisProgress();

    public static IProgress<CodeAnalysisProgress> GetCodeAnalysisProgress(this IUIThreadOperationScope? scope)
        => scope is null ? CodeAnalysisProgress.None : new UIThreadOperationScopeProgress(scope);

    private sealed class UIThreadOperationScopeProgress(IUIThreadOperationScope scope) : IProgress<CodeAnalysisProgress>
    {
        private int _completedItems;
        private int _totalItems;

        public void Report(CodeAnalysisProgress value)
        {
            if (value.DescriptionValue != null)
                scope.Description = value.DescriptionValue;

            if (value.ClearValue)
            {
                Interlocked.Exchange(ref _totalItems, 0);
                Interlocked.Exchange(ref _completedItems, 0);
            }
            else
            {
                if (value.IncompleteItemsValue != null)
                    Interlocked.Add(ref _totalItems, value.IncompleteItemsValue.Value);

                if (value.CompleteItemValue != null)
                    Interlocked.Add(ref _completedItems, value.CompleteItemValue.Value);
            }

            scope.Progress.Report(new ProgressInfo(_completedItems, _totalItems));
        }
    }
}
