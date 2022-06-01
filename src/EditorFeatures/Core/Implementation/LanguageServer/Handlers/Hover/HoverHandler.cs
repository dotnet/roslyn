// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// TODO - This must be moved to the MS.CA.LanguageServer.Protocol project once it
    /// no longer references VS icon or classified text run types.
    /// See https://github.com/dotnet/roslyn/issues/55142
    /// </summary>
    [ExportRoslynLanguagesLspRequestHandlerProvider(typeof(HoverHandler)), Shared]
    [Method(Methods.TextDocumentHoverName)]
    internal sealed class HoverHandler : AbstractStatelessRequestHandler<TextDocumentPositionParams, Hover?>
    {
        private readonly IGlobalOptionService _globalOptions;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public HoverHandler(IGlobalOptionService globalOptions)
        {
            _globalOptions = globalOptions;
        }

        public override bool MutatesSolutionState => false;
        public override bool RequiresLSPSolution => true;

        public override TextDocumentIdentifier? GetTextDocumentIdentifier(TextDocumentPositionParams request) => request.TextDocument;

        public override async Task<Hover?> HandleRequestAsync(TextDocumentPositionParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.Document;
            Contract.ThrowIfNull(document);

            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);
            var quickInfoService = document.Project.LanguageServices.GetRequiredService<QuickInfoService>();
            var options = _globalOptions.GetSymbolDescriptionOptions(document.Project.Language);
            var info = await quickInfoService.GetQuickInfoAsync(document, position, options, cancellationToken).ConfigureAwait(false);
            if (info == null)
            {
                return null;
            }

            var classificationOptions = _globalOptions.GetClassificationOptions(document.Project.Language);
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return await GetHoverAsync(info, text, document.Project.Language, document, classificationOptions, context.ClientCapabilities, cancellationToken).ConfigureAwait(false);
        }

        internal static async Task<Hover?> GetHoverAsync(
            SemanticModel semanticModel,
            int position,
            SymbolDescriptionOptions options,
            HostLanguageServices languageServices,
            CancellationToken cancellationToken)
        {
            Debug.Assert(semanticModel.Language is LanguageNames.CSharp or LanguageNames.VisualBasic);

            // Get the quick info service to compute quick info.
            // This code path is only invoked for C# and VB, so we can directly cast to QuickInfoServiceWithProviders.
            var quickInfoService = (QuickInfoServiceWithProviders)languageServices.GetRequiredService<QuickInfoService>();
            var info = await quickInfoService.GetQuickInfoAsync(semanticModel, position, options, cancellationToken).ConfigureAwait(false);
            if (info == null)
            {
                return null;
            }

            var text = await semanticModel.SyntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return await GetHoverAsync(info, text, semanticModel.Language, document: null, classificationOptions: null, clientCapabilities: null, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<Hover> GetHoverAsync(
            QuickInfoItem info,
            SourceText text,
            string language,
            Document? document,
            ClassificationOptions? classificationOptions,
            ClientCapabilities? clientCapabilities,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(document is null == (classificationOptions == null));

            var supportsVSExtensions = clientCapabilities.HasVisualStudioLspCapability();

            if (supportsVSExtensions)
            {
                var context = document == null
                    ? null
                    : new IntellisenseQuickInfoBuilderContext(
                        document,
                        classificationOptions!.Value,
                        threadingContext: null,
                        operationExecutor: null,
                        asynchronousOperationListener: null,
                        streamingPresenter: null);
                return new VSInternalHover
                {
                    Range = ProtocolConversions.TextSpanToRange(info.Span, text),
                    Contents = new SumType<SumType<string, MarkedString>, SumType<string, MarkedString>[], MarkupContent>(string.Empty),
                    // Build the classified text without navigation actions - they are not serializable.
                    // TODO - Switch to markup content once it supports classifications.
                    // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/918138
                    RawContent = await IntellisenseQuickInfoBuilder.BuildContentWithoutNavigationActionsAsync(info, context, cancellationToken).ConfigureAwait(false)
                };
            }
            else
            {
                return new Hover
                {
                    Range = ProtocolConversions.TextSpanToRange(info.Span, text),
                    Contents = GetContents(info, language, clientCapabilities),
                };
            }

            // Local functions.
            static MarkupContent GetContents(QuickInfoItem info, string language, ClientCapabilities? clientCapabilities)
            {
                var clientSupportsMarkdown = clientCapabilities?.TextDocument?.Hover?.ContentFormat.Contains(MarkupKind.Markdown) == true;
                // Insert line breaks in between sections to ensure we get double spacing between sections.
                var tags = info.Sections
                    .SelectMany(section => section.TaggedParts.Add(new TaggedText(TextTags.LineBreak, Environment.NewLine)))
                    .ToImmutableArray();
                return ProtocolConversions.GetDocumentationMarkupContent(tags, language, clientSupportsMarkdown);
            }
        }
    }
}
