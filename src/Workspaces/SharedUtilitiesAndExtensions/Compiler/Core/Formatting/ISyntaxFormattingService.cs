// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Options;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.Host;
#endif

namespace Microsoft.CodeAnalysis.Formatting
{
    internal interface ISyntaxFormattingService
#if !CODE_STYLE
        : ILanguageService
#endif
    {
        SyntaxFormattingOptions GetFormattingOptions(AnalyzerConfigOptions options);
        IEnumerable<AbstractFormattingRule> GetDefaultFormattingRules();
        IFormattingResult GetFormattingResult(SyntaxNode node, IEnumerable<TextSpan>? spans, SyntaxFormattingOptions options, IEnumerable<AbstractFormattingRule>? rules, CancellationToken cancellationToken);
    }

    internal abstract class SyntaxFormattingOptions
    {
        public readonly bool UseTabs;
        public readonly int TabSize;
        public readonly int IndentationSize;
        public readonly string NewLine;

        public readonly bool SeparateImportDirectiveGroups;

        protected SyntaxFormattingOptions(
            bool useTabs,
            int tabSize,
            int indentationSize,
            string newLine,
            bool separateImportDirectiveGroups)
        {
            UseTabs = useTabs;
            TabSize = tabSize;
            IndentationSize = indentationSize;
            NewLine = newLine;
            SeparateImportDirectiveGroups = separateImportDirectiveGroups;
        }

#if !CODE_STYLE
        public static SyntaxFormattingOptions Create(OptionSet options, HostWorkspaceServices services, string language)
        {
            var formattingService = services.GetRequiredLanguageService<ISyntaxFormattingService>(language);
            var configOptions = options.AsAnalyzerConfigOptions(services.GetRequiredService<IOptionService>(), language);
            return formattingService.GetFormattingOptions(configOptions);
        }

        public static async Task<SyntaxFormattingOptions> FromDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            return Create(documentOptions, document.Project.Solution.Workspace.Services, document.Project.Language);
        }
#endif
    }
}
