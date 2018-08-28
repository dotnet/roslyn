using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal abstract class ConfigureSeverityLevelCodeFixProvider : ISuppressionOrConfigurationFixProvider
    {
        private Dictionary<string, Option<CodeStyleOption<bool>>> _languageOptions;
        private readonly string _language;
        private Dictionary<string, Option<CodeStyleOption<ExpressionBodyPreference>>> _expressionOptionsOpt;

        public ConfigureSeverityLevelCodeFixProvider(Dictionary<string, Option<CodeStyleOption<bool>>> languageOptions, string language, Dictionary<string, Option<CodeStyleOption<ExpressionBodyPreference>>> expressionOptionsOpt = null)
        {
            _languageOptions = languageOptions;
            _language = language;
            _expressionOptionsOpt = expressionOptionsOpt;
        }

        public bool CanBeConfigured(Diagnostic diagnostic)
        {
            return ConfigureSeverityLevelCodeAction.diagnosticToEditorConfigDotNet.ContainsKey(diagnostic.Id) ||
                _languageOptions.ContainsKey(diagnostic.Id) ||
                (_expressionOptionsOpt != null && _expressionOptionsOpt.ContainsKey(diagnostic.Id)) ||
                (diagnostic.Properties != null && diagnostic.Properties.ContainsKey(AbstractCodeStyleDiagnosticAnalyzer.OptionName) && diagnostic.Properties.ContainsKey(AbstractCodeStyleDiagnosticAnalyzer.OptionCurrent));
        }

        public FixAllProvider GetFixAllProvider()
        {
            return null;
        }

        public Task<ImmutableArray<CodeFix>> GetSuppressionsOrConfigurationsAsync(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            var nestedActions = ArrayBuilder<CodeAction>.GetInstance();
            var result = ArrayBuilder<CodeFix>.GetInstance();
            foreach (var diagnostic in diagnostics)
            {
                nestedActions.Add(new SolutionChangeAction("None", (solution => ConfigureSeverityLevelCodeAction.ConfigureEditorConfig(EditorConfigSeverityStrings.None, diagnostic, document.Project, _languageOptions, _expressionOptionsOpt, _language, cancellationToken))));
                nestedActions.Add(new SolutionChangeAction("Silent", (solution => ConfigureSeverityLevelCodeAction.ConfigureEditorConfig(EditorConfigSeverityStrings.Silent, diagnostic, document.Project, _languageOptions, _expressionOptionsOpt, _language, cancellationToken))));
                nestedActions.Add(new SolutionChangeAction("Suggestion", (solution => ConfigureSeverityLevelCodeAction.ConfigureEditorConfig(EditorConfigSeverityStrings.Suggestion, diagnostic, document.Project, _languageOptions, _expressionOptionsOpt, _language, cancellationToken))));
                nestedActions.Add(new SolutionChangeAction("Warning", (solution => ConfigureSeverityLevelCodeAction.ConfigureEditorConfig(EditorConfigSeverityStrings.Warning, diagnostic, document.Project, _languageOptions, _expressionOptionsOpt, _language, cancellationToken))));
                nestedActions.Add(new SolutionChangeAction("Error", (solution => ConfigureSeverityLevelCodeAction.ConfigureEditorConfig(EditorConfigSeverityStrings.Error, diagnostic, document.Project, _languageOptions, _expressionOptionsOpt, _language, cancellationToken))));

                var codeAction = new ConfigureSeverityLevelCodeAction(diagnostic, nestedActions.ToImmutableAndFree());
                result.Add(new CodeFix(document.Project, codeAction, diagnostic));
            }
            return Task.FromResult(result.ToImmutableAndFree());
        }

        public Task<ImmutableArray<CodeFix>> GetSuppressionsOrConfigurationsAsync(Project project, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
