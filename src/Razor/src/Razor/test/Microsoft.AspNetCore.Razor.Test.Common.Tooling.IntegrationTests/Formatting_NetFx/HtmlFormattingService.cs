// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Test.Common.Mef;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Microsoft.WebTools.Languages.Shared.ContentTypes;

namespace Microsoft.CodeAnalysis.Razor.Formatting;

internal sealed class HtmlFormattingService : IDisposable
{
    private ExportProvider? _exportProvider;

    private ExportProvider ExportProvider => _exportProvider ?? (_exportProvider = TestComposition.Editor.ExportProviderFactory.CreateExportProvider());

    public void Dispose()
    {
        if (_exportProvider is not null)
        {
            _exportProvider.Dispose();
        }
    }

    public Task<TextEdit[]?> GetDocumentFormattingEditsAsync(ILoggerFactory loggerFactory, Uri uri, string generatedHtml, bool insertSpaces, int tabSize)
    {
        var request = $$"""
            {
                "Options":
                {
                    "UseSpaces": {{(insertSpaces ? "true" : "false")}},
                    "TabSize": {{tabSize}},
                    "IndentSize": {{tabSize}}
                },
                "Uri": "{{uri}}",
                "GeneratedChanges": [],
            }
            """;

        return CallWebToolsApplyFormattedEditsHandlerAsync(loggerFactory, request, uri, generatedHtml);
    }

    public Task<TextEdit[]?> GetOnTypeFormattingEditsAsync(ILoggerFactory loggerFactory, Uri uri, string generatedHtml, Position position, bool insertSpaces, int tabSize)
    {
        var generatedHtmlSource = SourceText.From(generatedHtml, Encoding.UTF8);
        var absoluteIndex = generatedHtmlSource.GetRequiredAbsoluteIndex(position);

        var request = $$"""
            {
                "Options":
                {
                    "UseSpaces": {{(insertSpaces ? "true" : "false")}},
                    "TabSize": {{tabSize}},
                    "IndentSize": {{tabSize}}
                },
                "Uri": "{{uri}}",
                "GeneratedChanges": [],
                "OperationType": "FormatOnType",
                "SpanToFormat":
                {
                    "Start": {{absoluteIndex}},
                    "End": {{absoluteIndex}}
                }
            }
            """;

        return CallWebToolsApplyFormattedEditsHandlerAsync(loggerFactory, request, uri, generatedHtml);
    }

    private async Task<TextEdit[]?> CallWebToolsApplyFormattedEditsHandlerAsync(ILoggerFactory loggerFactory, string serializedValue, Uri documentUri, string generatedHtml)
    {
        var contentTypeService = ExportProvider.GetExportedValue<IContentTypeRegistryService>();

        lock (contentTypeService)
        {
            if (!contentTypeService.ContentTypes.Any(t => t.TypeName == HtmlContentTypeDefinition.HtmlContentType))
            {
                contentTypeService.AddContentType(HtmlContentTypeDefinition.HtmlContentType, [StandardContentTypeNames.Text]);
            }
        }

        var textBufferFactoryService = (ITextBufferFactoryService3)ExportProvider.GetExportedValue<ITextBufferFactoryService>();
        var bufferManager = WebTools.BufferManager.New(contentTypeService, textBufferFactoryService, []);
        var logger = loggerFactory.GetOrCreateLogger("ApplyFormattedEditsHandler");
        var applyFormatEditsHandler = WebTools.ApplyFormatEditsHandler.New(textBufferFactoryService, bufferManager, logger);

        // Make sure the buffer manager knows about the source document
        var textSnapshot = bufferManager.CreateBuffer(
            documentUri: documentUri,
            contentTypeName: HtmlContentTypeDefinition.HtmlContentType,
            initialContent: generatedHtml,
            snapshotVersionFromLSP: 0);

        var requestContext = WebTools.RequestContext.New(textSnapshot);

        var request = WebTools.ApplyFormatEditsParam.DeserializeFrom(serializedValue);
        var response = await applyFormatEditsHandler.HandleRequestAsync(request, requestContext, CancellationToken.None);

        var sourceText = SourceText.From(generatedHtml);

        using var edits = new PooledArrayBuilder<TextEdit>();

        foreach (var textChange in response.TextChanges)
        {
            var span = new TextSpan(textChange.Position, textChange.Length);
            var edit = LspFactory.CreateTextEdit(sourceText.GetRange(span), textChange.NewText);

            edits.Add(edit);
        }

        return edits.ToArray();
    }
}
