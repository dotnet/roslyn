// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    [ExportWorkspaceService(typeof(ILspHoverResultCreationService), ServiceLayer.Editor), Shared]
    internal sealed class EditorLspHoverResultCreationService : ILspHoverResultCreationService
    {
        private readonly IGlobalOptionService _optionService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public EditorLspHoverResultCreationService(IGlobalOptionService optionService)
        {
            _optionService = optionService;
        }

        public async Task<Hover> CreateHoverAsync(
            SourceText text, string language, QuickInfoItem info, Document? document, ClientCapabilities? clientCapabilities, CancellationToken cancellationToken)
        {
            var supportsVSExtensions = clientCapabilities.HasVisualStudioLspCapability();

            if (!supportsVSExtensions)
                return DefaultLspHoverResultCreationService.CreateDefaultHover(text, language, info, clientCapabilities);

            var classificationOptions = _optionService.GetClassificationOptions(language);

            // We can pass null for all these parameter values as they're only needed for quick-info content navigation
            // and we explicitly calling BuildContentWithoutNavigationActionsAsync.
            var context = document is null
                ? null
                : new IntellisenseQuickInfoBuilderContext(
                    document,
                    classificationOptions,
                    threadingContext: null,
                    operationExecutor: null,
                    asynchronousOperationListener: null,
                    streamingPresenter: null);
            return new VSInternalHover
            {
                Range = ProtocolConversions.TextSpanToRange(info.Span, text),
                Contents = new SumType<string, MarkedString, SumType<string, MarkedString>[], MarkupContent>(string.Empty),
                // Build the classified text without navigation actions - they are not serializable.
                // TODO - Switch to markup content once it supports classifications.
                // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/918138
                RawContent = await IntellisenseQuickInfoBuilder.BuildContentWithoutNavigationActionsAsync(info, context, cancellationToken).ConfigureAwait(false)
            };
        }
    }
}
