// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.CodeActions;

internal static class IUIThreadOperationContextExtensions
{
    public static IProgress<CodeActionProgress> GetCodeActionProgress(this IUIThreadOperationContext context)
        => context.Scopes.Last().GetCodeActionProgress();

    public static IProgress<CodeActionProgress> GetCodeActionProgress(this IUIThreadOperationScope scope)
        => new CodeActionProgressTracker((description, completedItems, totalItems) =>
        {
            if (description != null)
                scope.Description = description;

            scope.Progress.Report(new ProgressInfo(completedItems, totalItems));
        });
}
