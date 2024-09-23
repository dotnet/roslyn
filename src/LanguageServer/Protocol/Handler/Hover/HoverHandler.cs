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
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// TODO - This must be moved to the MS.CA.LanguageServer.Protocol project once it
    /// no longer references VS icon or classified text run types.
    /// See https://github.com/dotnet/roslyn/issues/55142
    /// </summary>
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

        public async Task<Hover?> HandleRequestAsync(TextDocumentPositionParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.GetRequiredDocument();
            var clientCapabilities = context.GetRequiredClientCapabilities();

            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);
            var options = _globalOptions.GetSymbolDescriptionOptions(document.Project.Language);
            return await GetHoverAsync(document, position, options, clientCapabilities, cancellationToken).ConfigureAwait(false);
        }

        internal static async Task<Hover?> GetHoverAsync(
            Document document,
            int position,
            SymbolDescriptionOptions options,
            ClientCapabilities clientCapabilities,
            CancellationToken cancellationToken)
        {
            // Get the quick info service to compute quick info.
            // This code path is only invoked for C# and VB, so we can directly cast to QuickInfoServiceWithProviders.
            var quickInfoService = document.GetRequiredLanguageService<QuickInfoService>();
            var info = await quickInfoService.GetQuickInfoAsync(document, position, options, cancellationToken).ConfigureAwait(false);
            if (info == null)
                return null;

            var supportsVSExtensions = clientCapabilities.HasVisualStudioLspCapability();

            return supportsVSExtensions
                ? await CreateVsHoverAsync(document, info, options, cancellationToken).ConfigureAwait(false)
                : await CreateDefaultHoverAsync(document, info, clientCapabilities, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<VSInternalHover> CreateVsHoverAsync(
            Document document, QuickInfoItem info, SymbolDescriptionOptions options, CancellationToken cancellationToken)
        {
            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

            var context = document is not null
                ? new QuickInfoContentBuilderContext(
                    document,
                    options.ClassificationOptions,
                    await document.GetLineFormattingOptionsAsync(cancellationToken).ConfigureAwait(false),
                    // Build the classified text without navigation actions - they are not serializable.
                    navigationActionFactory: null)
                : null;

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
            TextDocument document, QuickInfoItem info, ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
        {
            var clientSupportsMarkdown = clientCapabilities?.TextDocument?.Hover?.ContentFormat?.Contains(MarkupKind.Markdown) == true;

            // Insert line breaks in between sections to ensure we get double spacing between sections.
            ImmutableArray<TaggedText> tags = [
                .. info.Sections.SelectMany(static s => s.TaggedParts.Add(new TaggedText(TextTags.LineBreak, Environment.NewLine)))
            ];

            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var language = document.Project.Language;

            return new Hover
            {
                Range = ProtocolConversions.TextSpanToRange(info.Span, text),
                Contents = ProtocolConversions.GetDocumentationMarkupContent(tags, language, clientSupportsMarkdown),
            };
        }
    }
}
