// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.UnifiedSuggestions;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions
{
    internal static partial class CodeActionHelpers
    {
        private static readonly ImmutableArray<string> s_notSupportFixerAndRefactoringProviderNames = ImmutableArray.Create(
            // Extract method requires inline rename support, not supported by lsp.
            PredefinedCodeRefactoringProviderNames.ExtractMethod,
            // There is no good way to modify the project file in lsp server.
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1810443
            PredefinedCodeRefactoringProviderNames.EnableNullable);

        /// <summary>
        /// Indicate If code action is supported by standard LSP.
        /// </summary>
        private static bool IsCodeActionNotSupportedByStandardLsp(IUnifiedSuggestedAction suggestedAction)
        {
            return IsCodeActionNotSupportedByVSLsp(suggestedAction)
                // Filter the configure and suppress fixer if it is not VS LSP, because it would generate many nested code actions.
                // Tracking issue: https://github.com/microsoft/language-server-protocol/issues/994 
                || suggestedAction.OriginalCodeAction is AbstractConfigurationActionWithNestedActions;
        }

        /// <summary>
        /// Indicate If code action is supported by VS LSP, which is a superset of the standard LSP.
        /// </summary>
        private static bool IsCodeActionNotSupportedByVSLsp(IUnifiedSuggestedAction suggestedAction)
        {
            var originalCodeAction = suggestedAction.OriginalCodeAction;
            // Filter out code actions with options since they'll show dialogs and we can't remote the UI and the options.
            return originalCodeAction is CodeActionWithOptions
                // Skip code actions that requires non-document changes.  We can't apply them in LSP currently.
                // https://github.com/dotnet/roslyn/issues/48698
                || originalCodeAction.Tags.Contains(CodeAction.RequiresNonDocumentChange)
                || s_notSupportFixerAndRefactoringProviderNames.Any(originalCodeAction.CustomTags.Contains);
        }
    }
}
