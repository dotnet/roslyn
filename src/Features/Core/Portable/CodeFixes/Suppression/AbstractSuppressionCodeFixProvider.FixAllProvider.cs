﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal abstract partial class AbstractSuppressionCodeFixProvider : IConfigurationFixProvider
    {
        private class SuppressionFixAllProvider : FixAllProvider
        {
            public static readonly SuppressionFixAllProvider Instance = new();

            private SuppressionFixAllProvider()
            {
            }

            public override async Task<CodeAction> GetFixAsync(FixAllContext fixAllContext)
            {
                // currently there's no FixAll support for local suppression, just bail out
                if (NestedSuppressionCodeAction.IsEquivalenceKeyForLocalSuppression(fixAllContext.CodeActionEquivalenceKey))
                {
                    return null;
                }

                var suppressionFixer = (AbstractSuppressionCodeFixProvider)((WrapperCodeFixProvider)fixAllContext.CodeFixProvider).SuppressionFixProvider;

                if (NestedSuppressionCodeAction.IsEquivalenceKeyForGlobalSuppression(fixAllContext.CodeActionEquivalenceKey))
                {
                    // For global suppressions, we defer to the global suppression system to handle directly.
                    var title = fixAllContext.CodeActionEquivalenceKey;
                    return fixAllContext.Document != null
                        ? GlobalSuppressMessageFixAllCodeAction.Create(
                            title, suppressionFixer, fixAllContext.Document,
                            await fixAllContext.GetDocumentDiagnosticsToFixAsync().ConfigureAwait(false))
                        : GlobalSuppressMessageFixAllCodeAction.Create(
                            title, suppressionFixer, fixAllContext.Project,
                            await fixAllContext.GetProjectDiagnosticsToFixAsync().ConfigureAwait(false));
                }

                if (NestedSuppressionCodeAction.IsEquivalenceKeyForPragmaWarning(fixAllContext.CodeActionEquivalenceKey))
                {
                    var batchFixer = new PragmaWarningBatchFixAllProvider(suppressionFixer);
                    return await batchFixer.GetFixAsync(fixAllContext).ConfigureAwait(false);
                }

                if (NestedSuppressionCodeAction.IsEquivalenceKeyForRemoveSuppression(fixAllContext.CodeActionEquivalenceKey))
                {
                    var batchFixer = RemoveSuppressionCodeAction.GetBatchFixer(suppressionFixer);
                    return await batchFixer.GetFixAsync(fixAllContext).ConfigureAwait(false);
                }

                throw ExceptionUtilities.Unreachable;
            }
        }
    }
}
