// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.CodeFixes.Configuration
{
    [ExportSuppressionFixProvider(PredefinedCodeFixProviderNames.ConfigureSeverity, LanguageNames.CSharp, LanguageNames.VisualBasic), Shared]
    internal sealed partial class ConfigureSeverityLevelCodeFixProvider : ISuppressionOrConfigurationFixProvider
    {
        public bool IsFixableDiagnostic(Diagnostic diagnostic)
            => !diagnostic.IsSuppressed && !SuppressionHelpers.IsNotConfigurableDiagnostic(diagnostic);

        public FixAllProvider GetFixAllProvider()
            => null;

        public Task<ImmutableArray<CodeFix>> GetFixesAsync(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
            => Task.FromResult(GetConfigurations(document.Project, diagnostics, cancellationToken));

        public Task<ImmutableArray<CodeFix>> GetFixesAsync(Project project, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
            => Task.FromResult(GetConfigurations(project, diagnostics, cancellationToken));

        private static ImmutableArray<CodeFix> GetConfigurations(Project project, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            var nestedActions = ArrayBuilder<CodeAction>.GetInstance();
            var result = ArrayBuilder<CodeFix>.GetInstance();
            foreach (var diagnostic in diagnostics)
            {
                nestedActions.Add(
                    new SolutionChangeAction(
                        "None",
                        solution => ConfigurationUpdater.ConfigureEditorConfig(EditorConfigSeverityStrings.None, diagnostic, project, cancellationToken)));
                nestedActions.Add(
                    new SolutionChangeAction(
                        "Silent",
                        solution => ConfigurationUpdater.ConfigureEditorConfig(EditorConfigSeverityStrings.Silent, diagnostic, project, cancellationToken)));
                nestedActions.Add(
                    new SolutionChangeAction(
                        "Suggestion",
                        solution => ConfigurationUpdater.ConfigureEditorConfig(EditorConfigSeverityStrings.Suggestion, diagnostic, project, cancellationToken)));
                nestedActions.Add(
                    new SolutionChangeAction(
                        "Warning",
                        solution => ConfigurationUpdater.ConfigureEditorConfig(EditorConfigSeverityStrings.Warning, diagnostic, project, cancellationToken)));
                nestedActions.Add(
                    new SolutionChangeAction(
                        "Error",
                        solution => ConfigurationUpdater.ConfigureEditorConfig(EditorConfigSeverityStrings.Error, diagnostic, project, cancellationToken)));

                var codeAction = new TopLevelConfigureSeverityCodeAction(diagnostic, nestedActions.ToImmutable());
                result.Add(new CodeFix(project, codeAction, diagnostic));

                nestedActions.Clear();
            }

            nestedActions.Free();
            return result.ToImmutableAndFree();
        }
    }
}
