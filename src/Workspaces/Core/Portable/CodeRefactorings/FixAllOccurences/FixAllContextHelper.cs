// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    internal static class FixAllContextHelper
    {
        public static string GetDefaultFixAllTitle(FixAllContext fixAllContext)
            => GetDefaultFixAllTitle(fixAllContext.Scope, fixAllContext.CodeAction, fixAllContext.Document, fixAllContext.Project);

        public static string GetDefaultFixAllTitle(
            FixAllScope fixAllScope,
            CodeAction codeAction,
            Document? triggerDocument,
            Project triggerProject)
        {
            var title = codeAction.Title;
            return fixAllScope switch
            {
                FixAllScope.Custom => string.Format(WorkspaceExtensionsResources.Fix_all_0, title),
                FixAllScope.Document => string.Format(WorkspaceExtensionsResources.Fix_all_0_in_1, title, triggerDocument!.Name),
                FixAllScope.Project => string.Format(WorkspaceExtensionsResources.Fix_all_0_in_1, title, triggerProject.Name),
                FixAllScope.Solution => string.Format(WorkspaceExtensionsResources.Fix_all_0_in_Solution, title),
                FixAllScope.Selection => string.Format(WorkspaceExtensionsResources.Fix_all_0_in_selection_for_1, title, triggerDocument!.Name),
                FixAllScope.ContainingMember => string.Format(WorkspaceExtensionsResources.Fix_all_0_in_containing_member_for_1, title, triggerDocument!.Name),
                FixAllScope.ContainingType => string.Format(WorkspaceExtensionsResources.Fix_all_0_in_containing_type_for_1, title, triggerDocument!.Name),
                _ => throw ExceptionUtilities.UnexpectedValue(fixAllScope),
            };
        }
    }
}
