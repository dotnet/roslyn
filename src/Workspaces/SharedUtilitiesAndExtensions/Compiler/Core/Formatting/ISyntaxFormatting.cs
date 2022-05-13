// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Text;

#if !CODE_STYLE
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
#endif

namespace Microsoft.CodeAnalysis.Formatting
{
    internal interface ISyntaxFormatting
    {
        SyntaxFormattingOptions DefaultOptions { get; }
        SyntaxFormattingOptions GetFormattingOptions(AnalyzerConfigOptions options);

        ImmutableArray<AbstractFormattingRule> GetDefaultFormattingRules();
        IFormattingResult GetFormattingResult(SyntaxNode node, IEnumerable<TextSpan>? spans, SyntaxFormattingOptions options, IEnumerable<AbstractFormattingRule>? rules, CancellationToken cancellationToken);
    }

    [DataContract]
    internal readonly record struct LineFormattingOptions(
        [property: DataMember(Order = 0)] bool UseTabs = false,
        [property: DataMember(Order = 1)] int TabSize = 4,
        [property: DataMember(Order = 2)] int IndentationSize = 4,
        string? NewLine = null)
    {
        [property: DataMember(Order = 3)]
        public string NewLine { get; init; } = NewLine ?? Environment.NewLine;

        public LineFormattingOptions()
            : this(NewLine: null)
        {
        }

        public static readonly LineFormattingOptions Default = new();

        public static LineFormattingOptions Create(AnalyzerConfigOptions options)
            => new(
                UseTabs: options.GetOption(FormattingOptions2.UseTabs),
                TabSize: options.GetOption(FormattingOptions2.TabSize),
                IndentationSize: options.GetOption(FormattingOptions2.IndentationSize),
                NewLine: options.GetOption(FormattingOptions2.NewLine));

#if !CODE_STYLE
        public static async Task<LineFormattingOptions> FromDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            var documentOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
            return Create(documentOptions);
        }
#endif
    }

    internal abstract class SyntaxFormattingOptions
    {
        [DataMember(Order = 0)]
        public LineFormattingOptions LineFormatting;

        [DataMember(Order = 1)]
        public readonly bool SeparateImportDirectiveGroups;

        protected const int BaseMemberCount = 2;

        protected SyntaxFormattingOptions(
            LineFormattingOptions lineFormatting,
            bool separateImportDirectiveGroups)
        {
            LineFormatting = lineFormatting;
            SeparateImportDirectiveGroups = separateImportDirectiveGroups;
        }

        public abstract SyntaxFormattingOptions With(LineFormattingOptions lineFormatting);

        public bool UseTabs => LineFormatting.UseTabs;
        public int TabSize => LineFormatting.TabSize;
        public int IndentationSize => LineFormatting.IndentationSize;
        public string NewLine => LineFormatting.NewLine;

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
