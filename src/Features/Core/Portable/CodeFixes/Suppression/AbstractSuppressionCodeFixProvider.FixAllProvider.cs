// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal abstract partial class AbstractSuppressionCodeFixProvider : IConfigurationFixProvider
    {
        private class SuppressionFixAllProvider : FixAllProvider
        {
            public static readonly SuppressionFixAllProvider Instance = new SuppressionFixAllProvider();

            private SuppressionFixAllProvider()
            {
            }

            public async override Task<CodeAction> GetFixAsync(FixAllContext fixAllContext)
            {
                // currently there's no FixAll support for local suppression, just bail out
                if (NestedSuppressionCodeAction.IsEquivalenceKeyForLocalSuppression(fixAllContext.CodeActionEquivalenceKey))
                {
                    return null;
                }

                var batchFixer = WellKnownFixAllProviders.BatchFixer;
                var suppressionFixer = (AbstractSuppressionCodeFixProvider)((WrapperCodeFixProvider)fixAllContext.CodeFixProvider).SuppressionFixProvider;
                var isGlobalSuppression = NestedSuppressionCodeAction.IsEquivalenceKeyForGlobalSuppression(fixAllContext.CodeActionEquivalenceKey);
                if (!isGlobalSuppression)
                {
                    var isPragmaWarningSuppression = NestedSuppressionCodeAction.IsEquivalenceKeyForPragmaWarning(fixAllContext.CodeActionEquivalenceKey);
                    Contract.ThrowIfFalse(isPragmaWarningSuppression || NestedSuppressionCodeAction.IsEquivalenceKeyForRemoveSuppression(fixAllContext.CodeActionEquivalenceKey));

                    batchFixer = isPragmaWarningSuppression
                        ? new PragmaWarningBatchFixAllProvider(suppressionFixer)
                        : RemoveSuppressionCodeAction.GetBatchFixer(suppressionFixer);
                }

                var title = fixAllContext.CodeActionEquivalenceKey;
                if (!isGlobalSuppression)
                {
                    // Regular batch fixer will handle getting the document/project fixes here.
                    return await batchFixer.GetFixAsync(fixAllContext).ConfigureAwait(false);
                }

                // For global suppressions, we defer to the global suppression system to handle directly.
                return fixAllContext.Document != null
                    ? GlobalSuppressMessageFixAllCodeAction.Create(
                        title, suppressionFixer, fixAllContext.Document,
                        await fixAllContext.GetDocumentDiagnosticsToFixAsync().ConfigureAwait(false))
                    : GlobalSuppressMessageFixAllCodeAction.Create(
                        title, suppressionFixer, fixAllContext.Project,
                        await fixAllContext.GetProjectDiagnosticsToFixAsync().ConfigureAwait(false));
            }
        }
    }
}
