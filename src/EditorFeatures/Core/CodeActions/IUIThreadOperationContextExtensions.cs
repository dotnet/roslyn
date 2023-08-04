// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Progress;

internal static class IUIThreadOperationContextExtensions
{
    public static IProgress<CodeAnalysisProgress> GetCodeAnalysisProgress(this IUIThreadOperationContext context)
        => context.Scopes.Last().GetCodeAnalysisProgress();

    public static IProgress<CodeAnalysisProgress> GetCodeAnalysisProgress(this IUIThreadOperationScope scope)
        => new CodeAnalysisProgressTracker((description, completedItems, totalItems) =>
        {
            if (description != null)
                scope.Description = description;

            scope.Progress.Report(new ProgressInfo(completedItems, totalItems));
        });
}
