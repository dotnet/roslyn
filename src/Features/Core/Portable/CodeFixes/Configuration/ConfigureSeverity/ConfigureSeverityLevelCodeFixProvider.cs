// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.Options.EditorConfig;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureSeverity
{
    [ExportConfigurationFixProvider(PredefinedCodeFixProviderNames.ConfigureSeverity, LanguageNames.CSharp, LanguageNames.VisualBasic), Shared]
    [ExtensionOrder(Before = PredefinedCodeFixProviderNames.Suppression)]
    internal sealed partial class ConfigureSeverityLevelCodeFixProvider : IConfigurationFixProvider
    {
        private static readonly ImmutableArray<(string name, string value)> s_editorConfigSeverityStrings =
            ImmutableArray.Create(
                (nameof(EditorConfigSeverityStrings.None), EditorConfigSeverityStrings.None),
                (nameof(EditorConfigSeverityStrings.Silent), EditorConfigSeverityStrings.Silent),
                (nameof(EditorConfigSeverityStrings.Suggestion), EditorConfigSeverityStrings.Suggestion),
                (nameof(EditorConfigSeverityStrings.Warning), EditorConfigSeverityStrings.Warning),
                (nameof(EditorConfigSeverityStrings.Error), EditorConfigSeverityStrings.Error));

        // We only offer fix for configurable diagnostics.
        // Also skip suppressed diagnostics defensively, though the code fix engine should ideally never call us for suppressed diagnostics.
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
            // Bail out if NativeEditorConfigSupport experiment is not enabled.
            if (!EditorConfigDocumentOptionsProviderFactory.ShouldUseNativeEditorConfigSupport(project.Solution.Workspace))
            {
                return ImmutableArray<CodeFix>.Empty;
            }

            var result = ArrayBuilder<CodeFix>.GetInstance();
            foreach (var diagnostic in diagnostics)
            {
                var nestedActions = ArrayBuilder<CodeAction>.GetInstance();
                foreach (var (name, value) in s_editorConfigSeverityStrings)
                {
                    nestedActions.Add(
                        new SolutionChangeAction(name, solution => ConfigurationUpdater.ConfigureSeverityAsync(value, diagnostic, project, cancellationToken)));
                }

                var codeAction = new TopLevelConfigureSeverityCodeAction(diagnostic, nestedActions.ToImmutableAndFree());
                result.Add(new CodeFix(project, codeAction, diagnostic));
            }

            return result.ToImmutableAndFree();
        }
    }
}
