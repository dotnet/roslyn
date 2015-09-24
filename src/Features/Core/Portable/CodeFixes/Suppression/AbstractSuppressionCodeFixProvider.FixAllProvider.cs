// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal abstract partial class AbstractSuppressionCodeFixProvider : ISuppressionFixProvider
    {
        private class SuppressionFixAllProvider : FixAllProvider
        {
            public static readonly SuppressionFixAllProvider Instance = new SuppressionFixAllProvider();

            private SuppressionFixAllProvider()
            {
            }

            public async override Task<CodeAction> GetFixAsync(FixAllContext fixAllContext)
            {
                var batchFixer = (BatchFixAllProvider)WellKnownFixAllProviders.BatchFixer;
                var fixMultipleContext = fixAllContext as FixMultipleContext;
                var suppressionFixer = (AbstractSuppressionCodeFixProvider)((WrapperCodeFixProvider)fixAllContext.CodeFixProvider).SuppressionFixProvider;
                var isGlobalSuppression = NestedSuppressionCodeAction.IsEquivalenceKeyForGlobalSuppression(fixAllContext.CodeActionEquivalenceKey);
                if (!isGlobalSuppression)
                {
                    // Pragma warning fix all.
                    batchFixer = new PragmaWarningBatchFixAllProvider(suppressionFixer);
                }

                var title = fixAllContext.CodeActionEquivalenceKey;
                if (fixAllContext.Document != null)
                {
                    var documentsAndDiagnosticsToFixMap = fixMultipleContext != null ?
                        fixMultipleContext.DocumentDiagnosticsToFix :
                        await batchFixer.GetDocumentDiagnosticsToFixAsync(fixAllContext).ConfigureAwait(false);

                    return !isGlobalSuppression ?
                        await batchFixer.GetFixAsync(documentsAndDiagnosticsToFixMap, fixAllContext).ConfigureAwait(false) :
                        CreateGlobalSuppressionFixAllAction(title, suppressionFixer, fixAllContext.Document, documentsAndDiagnosticsToFixMap);
                }
                else
                {
                    var projectsAndDiagnosticsToFixMap = fixMultipleContext != null ?
                        fixMultipleContext.ProjectDiagnosticsToFix :
                        await batchFixer.GetProjectDiagnosticsToFixAsync(fixAllContext).ConfigureAwait(false);

                    return !isGlobalSuppression ?
                        await batchFixer.GetFixAsync(projectsAndDiagnosticsToFixMap, fixAllContext).ConfigureAwait(false) :
                        CreateGlobalSuppressionFixAllAction(title, suppressionFixer, fixAllContext.Project, projectsAndDiagnosticsToFixMap);
                }
            }

            private static CodeAction CreateGlobalSuppressionFixAllAction(string title, AbstractSuppressionCodeFixProvider fixer, Document triggerDocument, ImmutableDictionary<Document, ImmutableArray<Diagnostic>> diagnosticsByDocument)
            {
                return new CodeAction.SolutionChangeAction(title,
                    ct => GlobalSuppressMessageFixAllCodeAction.CreateChangedSolutionAsync(fixer, triggerDocument, diagnosticsByDocument, ct),
                    equivalenceKey: title);
            }

            private static CodeAction CreateGlobalSuppressionFixAllAction(string title, AbstractSuppressionCodeFixProvider fixer, Project triggerProject, ImmutableDictionary<Project, ImmutableArray<Diagnostic>> diagnosticsByProject)
            {
                return new CodeAction.SolutionChangeAction(title,
                    ct => GlobalSuppressMessageFixAllCodeAction.CreateChangedSolutionAsync(fixer, triggerProject, diagnosticsByProject, ct),
                    equivalenceKey: title);
            }
        }
    }
}
