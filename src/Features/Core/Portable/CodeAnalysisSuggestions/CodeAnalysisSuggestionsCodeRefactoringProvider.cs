// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddPackage;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureSeverity;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

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

            using var _ = ArrayBuilder<CodeAction>.GetInstance(out var actionsBuilder);

            var ruleConfigData = await configService.TryGetCodeAnalysisSuggestionsConfigDataAsync(document.Project, cancellationToken).ConfigureAwait(false);
            if (!ruleConfigData.IsEmpty)
            {
                var actions = await GetCodeAnalysisSuggestionActionsAsync(ruleConfigData, document, cancellationToken).ConfigureAwait(false);
                actionsBuilder.AddRange(actions);
            }

            var workspaceServices = document.Project.Solution.Services;
            var installerService = workspaceServices.GetService<IPackageInstallerService>();
            if (installerService is not null)
            {
                var packageConfigData = await configService.TryGetCodeAnalysisPackageSuggestionConfigDataAsync(document.Project, cancellationToken).ConfigureAwait(false);
                if (packageConfigData is not null)
                {
                    actionsBuilder.Add(GetCodeAnalysisPackageSuggestionAction(packageConfigData, document, installerService));
                }
            }

            if (actionsBuilder.Count > 0)
            {
                context.RegisterRefactoring(
                    CodeAction.Create(
                        FeaturesResources.Copilot_code_analysis_suggestions,
                        actionsBuilder.ToImmutable(),
                        isInlinable: false,
                        CodeActionPriority.Low),
                    span);
            }
        }

        private static CodeAction GetCodeAnalysisPackageSuggestionAction(
            string packageName,
            Document document, IPackageInstallerService installerService)
        {
            return new InstallPackageParentCodeAction(installerService, source: null, packageName, includePrerelease: true, document);
        }

        private static async Task<ImmutableArray<CodeAction>> GetCodeAnalysisSuggestionActionsAsync(
            ImmutableArray<(string, ImmutableArray<DiagnosticDescriptor>)> configData,
            Document document,
            CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var location = root.GetLocation();

            using var _1 = ArrayBuilder<CodeAction>.GetInstance(out var actionsBuilder);
            using var _2 = ArrayBuilder<CodeAction>.GetInstance(out var nestedActionsBuilder);
            foreach (var (category, descriptors) in configData)
            {
                foreach (var descriptor in descriptors)
                {
                    Debug.Assert(string.Equals(descriptor.Category, category, StringComparison.OrdinalIgnoreCase));

                    var diagnostic = Diagnostic.Create(descriptor, location);
                    if (SuppressionHelpers.IsNotConfigurableDiagnostic(diagnostic))
                        continue;

                    var nestedNestedAction = ConfigureSeverityLevelCodeFixProvider.CreateSeverityConfigurationCodeAction(diagnostic, document.Project);

                    // TODO: Add actions to ignore all the rules here by adding them to .editorconfig and set to None or Silent.
                    // Further, None could be used to filter out rules to suggest as they indicate user is aware of them and explicitly disabled them.

                    // TODO: Add nested nested actions for FixAll

                    var title = $"{descriptor.Id}: {descriptor.Title}";
                    var nestedAction = CodeAction.Create(title, ImmutableArray.Create(nestedNestedAction), isInlinable: false);
                    nestedActionsBuilder.Add(nestedAction);
                }

                if (nestedActionsBuilder.Count == 0)
                    continue;

                // Add code action to Configure severity for the entire 'Category'
                var categoryConfigurationAction = ConfigureSeverityLevelCodeFixProvider.CreateBulkSeverityConfigurationCodeAction(category, document.Project);
                nestedActionsBuilder.Add(categoryConfigurationAction);

                var action = CodeAction.Create($"'{category}' improvements", nestedActionsBuilder.ToImmutableAndClear(), isInlinable: false);
                actionsBuilder.Add(action);
            }

            return actionsBuilder.ToImmutable();
        }
    }
}
