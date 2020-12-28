﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.Options.EditorConfig;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureSeverity
{
    [ExportConfigurationFixProvider(PredefinedCodeFixProviderNames.ConfigureSeverity, LanguageNames.CSharp, LanguageNames.VisualBasic), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.Suppression)]
    internal sealed partial class ConfigureSeverityLevelCodeFixProvider : IConfigurationFixProvider
    {
        private static readonly ImmutableArray<(string name, string value)> s_editorConfigSeverityStrings =
            ImmutableArray.Create(
                (nameof(EditorConfigSeverityStrings.None), EditorConfigSeverityStrings.None),
                (nameof(EditorConfigSeverityStrings.Silent), EditorConfigSeverityStrings.Silent),
                (nameof(EditorConfigSeverityStrings.Suggestion), EditorConfigSeverityStrings.Suggestion),
                (nameof(EditorConfigSeverityStrings.Warning), EditorConfigSeverityStrings.Warning),
                (nameof(EditorConfigSeverityStrings.Error), EditorConfigSeverityStrings.Error));

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public ConfigureSeverityLevelCodeFixProvider()
        {
        }

        // We only offer fix for configurable diagnostics.
        // Also skip suppressed diagnostics defensively, though the code fix engine should ideally never call us for suppressed diagnostics.
        public bool IsFixableDiagnostic(Diagnostic diagnostic)
            => !diagnostic.IsSuppressed && !SuppressionHelpers.IsNotConfigurableDiagnostic(diagnostic);

        public FixAllProvider? GetFixAllProvider()
            => null;

        public Task<ImmutableArray<CodeFix>> GetFixesAsync(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
            => Task.FromResult(GetConfigurations(document.Project, diagnostics, cancellationToken));

        public Task<ImmutableArray<CodeFix>> GetFixesAsync(Project project, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
            => Task.FromResult(GetConfigurations(project, diagnostics, cancellationToken));

        private static ImmutableArray<CodeFix> GetConfigurations(Project project, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            var result = ArrayBuilder<CodeFix>.GetInstance();
            var analyzerDiagnosticsByCategory = new SortedDictionary<string, ArrayBuilder<Diagnostic>>();
            using var disposer = ArrayBuilder<Diagnostic>.GetInstance(out var analyzerDiagnostics);
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

                // Bulk configuration is only supported for analyzer diagnostics.
                if (!SuppressionHelpers.IsCompilerDiagnostic(diagnostic))
                {
                    // Ensure diagnostic has a valid non-empty 'Category' for category based configuration.
                    if (!string.IsNullOrEmpty(diagnostic.Descriptor.Category))
                    {
                        var diagnosticsForCategory = analyzerDiagnosticsByCategory.GetOrAdd(diagnostic.Descriptor.Category, _ => ArrayBuilder<Diagnostic>.GetInstance());
                        diagnosticsForCategory.Add(diagnostic);
                    }

                    analyzerDiagnostics.Add(diagnostic);
                }
            }

            foreach (var (category, diagnosticsWithCategory) in analyzerDiagnosticsByCategory)
            {
                AddBulkConfigurationCodeFixes(diagnosticsWithCategory.ToImmutableAndFree(), category);
            }

            if (analyzerDiagnostics.Count > 0)
            {
                AddBulkConfigurationCodeFixes(analyzerDiagnostics.ToImmutable(), category: null);
            }

            return result.ToImmutableAndFree();

            void AddBulkConfigurationCodeFixes(ImmutableArray<Diagnostic> diagnostics, string? category)
            {
                var nestedActions = ArrayBuilder<CodeAction>.GetInstance();
                foreach (var (name, value) in s_editorConfigSeverityStrings)
                {
                    nestedActions.Add(
                        new SolutionChangeAction(
                            name,
                            solution => category != null
                                ? ConfigurationUpdater.BulkConfigureSeverityAsync(value, category, project, cancellationToken)
                                : ConfigurationUpdater.BulkConfigureSeverityAsync(value, project, cancellationToken)));
                }

                var codeAction = new TopLevelBulkConfigureSeverityCodeAction(nestedActions.ToImmutableAndFree(), category);
                result.Add(new CodeFix(project, codeAction, diagnostics));
            }
        }
    }
}
