// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Runtime.Serialization;
using System.Threading.Tasks;
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

    [DataContract]
    internal abstract class SyntaxFormattingOptions
    {
        [DataMember(Order = 0)]
        public readonly bool UseTabs;

        [DataMember(Order = 1)]
        public readonly int TabSize;

        [DataMember(Order = 2)]
        public readonly int IndentationSize;

        [DataMember(Order = 3)]
        public readonly string NewLine;

        [DataMember(Order = 4)]
        public readonly bool SeparateImportDirectiveGroups;

        protected const int BaseMemberCount = 5;

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

        public abstract SyntaxFormattingOptions With(bool useTabs, int tabSize, int indentationSize);

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
