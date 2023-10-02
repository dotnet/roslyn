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
        => new CodeAnalysisProgressCallback((description, completedItems, totalItems) =>
        {
            if (scope != null)
            {
                if (description != null)
                    scope.Description = description;

                scope.Progress.Report(new ProgressInfo(completedItems, totalItems));
            }
        });

    private sealed class CodeAnalysisProgressCallback(Action<string?, int, int>? updateAction) : IProgress<CodeAnalysisProgress>
    {
        private string? _description;
        private int _completedItems;
        private int _totalItems;

        public CodeAnalysisProgressCallback() : this(updateAction: null)
        {
        }

        public void Report(CodeAnalysisProgress value)
        {
            if (value.DescriptionValue != null)
                _description = value.DescriptionValue;

            if (value.IncompleteItemsValue != null)
                Interlocked.Add(ref _totalItems, value.IncompleteItemsValue.Value);

            if (value.CompleteItemValue != null)
                Interlocked.Add(ref _completedItems, value.CompleteItemValue.Value);

            updateAction?.Invoke(_description, _completedItems, _totalItems);
        }
    }
}
