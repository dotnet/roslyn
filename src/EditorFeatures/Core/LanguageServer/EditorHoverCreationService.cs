// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.QuickInfo;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer;

[ExportWorkspaceService(typeof(ILspHoverResultCreationService), ServiceLayer.Editor), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class EditorLspHoverResultCreationService(IGlobalOptionService globalOptions) : ILspHoverResultCreationService
{
    private readonly IGlobalOptionService _globalOptions = globalOptions;

    public async Task<Hover> CreateHoverAsync(
        Document document, QuickInfoItem info, ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
    {
        var supportsVSExtensions = clientCapabilities.HasVisualStudioLspCapability();

        if (!supportsVSExtensions)
            return await DefaultLspHoverResultCreationService.CreateDefaultHoverAsync(document, info, clientCapabilities, cancellationToken).ConfigureAwait(false);

        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var language = document.Project.Language;

        var classificationOptions = _globalOptions.GetClassificationOptions(language);

        // We can pass null for all these parameter values as they're only needed for quick-info content navigation
        // and we explicitly calling BuildContentWithoutNavigationActionsAsync.
        var context = document is null
            ? null
            : new IntellisenseQuickInfoBuilderContext(
                document,
                classificationOptions,
                await document.GetLineFormattingOptionsAsync(cancellationToken).ConfigureAwait(false),
                threadingContext: null,
                operationExecutor: null,
                asynchronousOperationListener: null,
                streamingPresenter: null);

        var element = await IntellisenseQuickInfoBuilder.BuildContentWithoutNavigationActionsAsync(info, context, cancellationToken).ConfigureAwait(false);
        return new VSInternalHover
        {
            Range = ProtocolConversions.TextSpanToRange(info.Span, text),
            Contents = new SumType<string, MarkedString, SumType<string, MarkedString>[], MarkupContent>(string.Empty),
            // Build the classified text without navigation actions - they are not serializable.
            // TODO - Switch to markup content once it supports classifications.
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/918138
            RawContent = element.ToLSPElement(),
        };
    }
}
