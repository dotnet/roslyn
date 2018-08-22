using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    class ConfigureSeverityLevelCodeActionSilent : CodeAction
    {
        private readonly Document _document;
        private readonly Diagnostic _diagnostic;
        private readonly Dictionary<string, Option<CodeStyleOption<bool>>> _languageOptions;
        private readonly Dictionary<string, Option<CodeStyleOption<ExpressionBodyPreference>>> _expressionOptions;
        private readonly string _language;

        public override string EquivalenceKey
        {
            get { return Title + _diagnostic.Id; }
        }

        public ConfigureSeverityLevelCodeActionSilent(
            Document document,
            Diagnostic diagnostic,
            Dictionary<string, Option<CodeStyleOption<bool>>> languageOptions,
            Dictionary<string, Option<CodeStyleOption<ExpressionBodyPreference>>> expressionOptions,
            string language)
        {
            _document = document;
            _diagnostic = diagnostic;
            _languageOptions = languageOptions;
            _expressionOptions = expressionOptions;
            _language = language;

        }

        protected override Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
        {
            return ConfigureSeverityLevelCodeAction.ConfigureEditorConfig(
                Title.ToLowerInvariant(),
                _diagnostic,
                _document.Project,
                _languageOptions,
                _expressionOptions,
                _language,
                cancellationToken
                );
        }

        public override string Title
        {
            get { return "Silent"; }
        }

    }
}
