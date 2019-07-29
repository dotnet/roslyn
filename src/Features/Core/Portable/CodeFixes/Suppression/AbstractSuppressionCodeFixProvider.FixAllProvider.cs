// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

                    batchFixer = isPragmaWarningSuppression ?
                        new PragmaWarningBatchFixAllProvider(suppressionFixer) :
                        RemoveSuppressionCodeAction.GetBatchFixer(suppressionFixer);
                }

                var title = fixAllContext.CodeActionEquivalenceKey;
                if (fixAllContext.Document != null)
                {
                    var documentsAndDiagnosticsToFixMap =
                        await fixAllContext.GetDocumentDiagnosticsToFixAsync().ConfigureAwait(false);

                    return !isGlobalSuppression
                        ? await batchFixer.GetFixAsync(
                            documentsAndDiagnosticsToFixMap, fixAllContext.State, fixAllContext.CancellationToken).ConfigureAwait(false)
                        : GlobalSuppressMessageFixAllCodeAction.Create(title, suppressionFixer, fixAllContext.Document, documentsAndDiagnosticsToFixMap);
                }
                else
                {
                    var projectsAndDiagnosticsToFixMap =
                        await fixAllContext.GetProjectDiagnosticsToFixAsync().ConfigureAwait(false);

                    return !isGlobalSuppression
                        ? await batchFixer.GetFixAsync(
                            projectsAndDiagnosticsToFixMap, fixAllContext.State, fixAllContext.CancellationToken).ConfigureAwait(false)
                        : GlobalSuppressMessageFixAllCodeAction.Create(title, suppressionFixer, fixAllContext.Project, projectsAndDiagnosticsToFixMap);
                }
            }
        }
    }
}
