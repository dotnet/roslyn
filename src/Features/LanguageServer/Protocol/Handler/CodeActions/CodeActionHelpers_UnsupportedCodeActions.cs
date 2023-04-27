// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.UnifiedSuggestions;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions
{
    internal static partial class CodeActionHelpers
    {
        private static bool IsCodeActionNotSupportedByLSP(IUnifiedSuggestedAction suggestedAction)
        {
            // Filter out code actions with options since they'll show dialogs and we can't remote the UI and the options.
            return suggestedAction.OriginalCodeAction is CodeActionWithOptions
            // Skip code actions that requires non-document changes.  We can't apply them in LSP currently.
            // https://github.com/dotnet/roslyn/issues/48698
            || suggestedAction.OriginalCodeAction.Tags.Contains(CodeAction.RequiresNonDocumentChange)
            // Filter the configure and suppress fixer if it is not VS LSP, because it would generate many nested code actions.
            // Tracking issue: https://github.com/microsoft/language-server-protocol/issues/994 
            || suggestedAction.OriginalCodeAction is AbstractConfigurationActionWithNestedActions;
        }
    }
}
