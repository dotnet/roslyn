// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.QuickInfo.Presentation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [ExportCSharpVisualBasicStatelessLspService(typeof(HoverHandler)), Shared]
    [Method(Methods.TextDocumentHoverName)]
    internal sealed class HoverHandler : ILspServiceDocumentRequestHandler<TextDocumentPositionParams, Hover?>
    {
        private readonly IGlobalOptionService _globalOptions;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public HoverHandler(IGlobalOptionService globalOptions)
        {
            _globalOptions = globalOptions;
        }

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        public TextDocumentIdentifier GetTextDocumentIdentifier(TextDocumentPositionParams request) => request.TextDocument;

        public Task<Hover?> HandleRequestAsync(TextDocumentPositionParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.GetRequiredDocument();
            var clientCapabilities = context.GetRequiredClientCapabilities();

            var linePosition = ProtocolConversions.PositionToLinePosition(request.Position);
            var supportsVSExtensions = clientCapabilities.HasVisualStudioLspCapability();
            var supportsMarkdown = clientCapabilities?.TextDocument?.Hover?.ContentFormat?.Contains(MarkupKind.Markdown) == true;

            return GetHoverAsync(document, linePosition, _globalOptions, supportsVSExtensions, supportsMarkdown, cancellationToken);
        }

        // Used by the LSIF Generator
        internal static Task<Hover?> GetHoverAsync(
            Document document,
            int position,
            SymbolDescriptionOptions options,
            ClientCapabilities clientCapabilities,
            CancellationToken cancellationToken)
        {
            var supportsVSExtensions = clientCapabilities.HasVisualStudioLspCapability();
            var supportsMarkdown = clientCapabilities?.TextDocument?.Hover?.ContentFormat?.Contains(MarkupKind.Markdown) == true;

            return GetHoverAsync(document, position, options, supportsVSExtensions, supportsMarkdown, cancellationToken);
        }

        internal static async Task<Hover?> GetHoverAsync(
            Document document,
            LinePosition linePosition,
            IGlobalOptionService globalOptions,
            bool supportsVSExtensions,
            bool supportsMarkdown,
            CancellationToken cancellationToken)
        {
            var position = await document
                .GetPositionFromLinePositionAsync(linePosition, cancellationToken)
                .ConfigureAwait(false);

            var options = globalOptions.GetSymbolDescriptionOptions(document.Project.Language);

            return await GetHoverAsync(
                document, position, options, supportsVSExtensions, supportsMarkdown, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<Hover?> GetHoverAsync(
            Document document,
            int position,
            SymbolDescriptionOptions options,
            bool supportsVSExtensions,
            bool supportsMarkdown,
            CancellationToken cancellationToken)
        {
            // Get the quick info service to compute quick info.
            // This code path is only invoked for C# and VB, so we can directly cast to QuickInfoServiceWithProviders.
            var quickInfoService = document.GetRequiredLanguageService<QuickInfoService>();
            var info = await quickInfoService.GetQuickInfoAsync(document, position, options, cancellationToken).ConfigureAwait(false);
            if (info == null)
                return null;

            return supportsVSExtensions
                ? await CreateVsHoverAsync(document, info, options, cancellationToken).ConfigureAwait(false)
                : await CreateDefaultHoverAsync(document, info, supportsMarkdown, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<VSInternalHover> CreateVsHoverAsync(
            Document document, QuickInfoItem info, SymbolDescriptionOptions options, CancellationToken cancellationToken)
        {
            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var formattingOptions = await document.GetLineFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);

            var context = new QuickInfoContentBuilderContext(
                document,
                options.ClassificationOptions,
                formattingOptions,
                // Build the classified text without navigation actions - they are not serializable.
                navigationActionFactory: null);

            var content = await QuickInfoContentBuilder.BuildInteractiveContentAsync(info, context, cancellationToken).ConfigureAwait(false);

            return new VSInternalHover
            {
                Range = ProtocolConversions.TextSpanToRange(info.Span, text),
                Contents = new SumType<string, MarkedString, SumType<string, MarkedString>[], MarkupContent>(string.Empty),

                // TODO - Switch to markup content once it supports classifications.
                // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/918138
                RawContent = content.ToLSPElement(),
            };
        }

        private static async Task<Hover> CreateDefaultHoverAsync(
            TextDocument document, QuickInfoItem info, bool supportsMarkdown, CancellationToken cancellationToken)
        {
            // Insert line breaks in between sections to ensure we get double spacing between sections.
            ImmutableArray<TaggedText> tags = [
                .. info.Sections.SelectMany(static s => s.TaggedParts.Add(new TaggedText(TextTags.LineBreak, Environment.NewLine)))
            ];

            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var language = document.Project.Language;

            return new Hover
            {
                Range = ProtocolConversions.TextSpanToRange(info.Span, text),
                Contents = ProtocolConversions.GetDocumentationMarkupContent(tags, language, supportsMarkdown),
            };
        }
    }
}
