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

#if !CODE_STYLE
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Host;
#endif

namespace Microsoft.CodeAnalysis.Formatting
{
    internal interface ISyntaxFormattingService
#if !CODE_STYLE
        : ILanguageService
#endif
    {
        IEnumerable<AbstractFormattingRule> GetDefaultFormattingRules();
        IFormattingResult GetFormattingResult(SyntaxNode node, IEnumerable<TextSpan>? spans, SyntaxFormattingOptions options, IEnumerable<AbstractFormattingRule>? rules, CancellationToken cancellationToken);
    }

    internal readonly record struct SyntaxFormattingOptions(
        AnalyzerConfigOptions Options)
    {
        public static readonly SyntaxFormattingOptions Default = Create(DictionaryAnalyzerConfigOptions.Empty);

        public static SyntaxFormattingOptions Create(AnalyzerConfigOptions options)
            => new(options);

#if !CODE_STYLE
        public static SyntaxFormattingOptions Create(OptionSet options, HostWorkspaceServices services, string language)
            => new(options.AsAnalyzerConfigOptions(services.GetRequiredService<IOptionService>(), language));

        public static async Task<SyntaxFormattingOptions> FromDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            return Create(documentOptions, document.Project.Solution.Workspace.Services, document.Project.Language);
        }
#endif
    }
}
