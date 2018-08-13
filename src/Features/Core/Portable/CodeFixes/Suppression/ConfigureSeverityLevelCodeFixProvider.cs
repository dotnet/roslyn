using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal abstract class ConfigureSeverityLevelCodeFixProvider : ISuppressionFixProvider
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

        public bool CanBeSuppressedOrUnsuppressed(Diagnostic diagnostic)
        {
            return ConfigureSeverityLevelCodeAction.diagnosticToEditorConfigDotNet.ContainsKey(diagnostic.Id) ||
                _languageOptions.ContainsKey(diagnostic.Id) ||
                (_expressionOptionsOpt != null && _expressionOptionsOpt.ContainsKey(diagnostic.Id)) ||
                (diagnostic.Properties != null && diagnostic.Properties.ContainsKey("OptionName") && diagnostic.Properties.ContainsKey("OptionCurrent"));
        }

        public FixAllProvider GetFixAllProvider()
        {
            return null;
        }

        public Task<ImmutableArray<CodeFix>> GetSuppressionsAsync(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            var nestedActions = ArrayBuilder<CodeAction>.GetInstance();
            var result = ArrayBuilder<CodeFix>.GetInstance();
            foreach (var diagnostic in diagnostics)
            {
                nestedActions.Add(new ConfigureSeverityLevelCodeActionNone(document, diagnostic, _languageOptions, _expressionOptionsOpt, _language));
                nestedActions.Add(new ConfigureSeverityLevelCodeActionSuggestion(document, diagnostic, _languageOptions, _expressionOptionsOpt, _language));
                nestedActions.Add(new ConfigureSeverityLevelCodeActionWarning(document, diagnostic, _languageOptions, _expressionOptionsOpt, _language));
                nestedActions.Add(new ConfigureSeverityLevelCodeActionError(document, diagnostic, _languageOptions, _expressionOptionsOpt, _language));
                var codeAction = new ConfigureSeverityLevelCodeAction(diagnostic, nestedActions.ToImmutableAndFree());
                result.Add(new CodeFix(document.Project, codeAction, diagnostic));
            }
            return Task.FromResult(result.ToImmutableAndFree());
        }

        public Task<ImmutableArray<CodeFix>> GetSuppressionsAsync(Project project, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
