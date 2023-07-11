// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.QuickInfo;
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

            var hoverService = document.Project.Solution.Services.GetRequiredService<ILspHoverResultCreationService>();
            return await hoverService.CreateHoverAsync(document, info, clientCapabilities, cancellationToken).ConfigureAwait(false);
        }
    }
}
