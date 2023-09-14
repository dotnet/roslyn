// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CodeAnalysisSuggestions
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp), Shared]
    internal sealed class CodeAnalysisSuggestionsCodeRefactoringProvider
        : CodeRefactoringProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CodeAnalysisSuggestionsCodeRefactoringProvider()
        {
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, span, cancellationToken) = context;
            var configService = document.Project.Solution.Services.GetRequiredService<ICopilotConfigService>();
            var configData = await configService.TryGetCodeAnalysisSuggestionsConfigDataAsync(document.Project, cancellationToken).ConfigureAwait(false);
            if (configData.IsEmpty)
                return;

            var nestedActions = GetCodeAnalysisSuggestionActions(configData, document);
            if (nestedActions.IsEmpty)
                return;

            context.RegisterRefactoring(
                CodeAction.Create(
                    FeaturesResources.Copilot_code_analysis_suggestions,
                    nestedActions,
                    isInlinable: false,
                    CodeActionPriority.Low),
                span);
        }

        private static ImmutableArray<CodeAction> GetCodeAnalysisSuggestionActions(
            ImmutableArray<(string, ImmutableArray<string>)> configData,
            Document document)
        {
            var infoCache = document.Project.Solution.Workspace.Services.SolutionServices.ExportProvider.GetExports<DiagnosticAnalyzerInfoCache.SharedGlobalCache>().FirstOrDefault();
            if (infoCache == null)
                return ImmutableArray<CodeAction>.Empty;

            var analyzerInfoCache = infoCache.Value.AnalyzerInfoCache;
            using var _1 = ArrayBuilder<CodeAction>.GetInstance(out var actionsBuilder);
            using var _2 = ArrayBuilder<CodeAction>.GetInstance(out var nestedActionsBuilder);
            foreach (var (category, ids) in configData)
            {
                foreach (var id in ids)
                {
                    if (analyzerInfoCache.TryGetDescriptorForDiagnosticId(id, out var descriptor))
                    {
                        if (!string.Equals(descriptor.Category, category, StringComparison.OrdinalIgnoreCase))
                            continue;

                        // TODO: Add logic for createChangedSolution.
                        //       We should also make sure that the ID isn't already configured to warning/error severity.
                        var title = "Enforce as build warning";
                        var nestedNestedAction = CodeAction.Create(title,
                            createChangedSolution: _ => Task.FromResult(document.Project.Solution),
                            equivalenceKey: id + title);

                        // TODO: Add nested nested actions for FixAll

                        title = $"{id}: {descriptor.Title}";
                        var nestedAction = CodeAction.Create(title, ImmutableArray.Create(nestedNestedAction), isInlinable: false);
                        nestedActionsBuilder.Add(nestedAction);
                    }
                }

                if (nestedActionsBuilder.Count == 0)
                    continue;

                var action = CodeAction.Create($"'{category}' improvements", nestedActionsBuilder.ToImmutableAndClear(), isInlinable: false);
                actionsBuilder.Add(action);
            }

            return actionsBuilder.ToImmutable();
        }
    }
}
