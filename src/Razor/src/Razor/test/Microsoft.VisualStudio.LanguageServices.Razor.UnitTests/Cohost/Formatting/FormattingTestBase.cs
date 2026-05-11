// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Razor.Settings;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.Formatting;

public abstract class FormattingTestBase : DocumentFormattingTestBase
{
    private readonly FormattingTestContext _context;
    private readonly HtmlFormattingService _htmlFormattingService;

    private protected FormattingTestBase(FormattingTestContext context, HtmlFormattingService htmlFormattingService, ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
        _context = context;
        _htmlFormattingService = htmlFormattingService;
    }

    private protected async Task RunOnTypeFormattingTestAsync(
        TestCode input,
        string expected,
        char triggerCharacter,
        bool inGlobalNamespace = false,
        bool insertSpaces = true,
        int tabSize = 4,
        RazorFileKind? fileKind = null,
        int? expectedChangedLines = null)
    {
        (input, _, expected) = ProcessFormattingContext(input, "", expected);

        var document = CreateProjectAndRazorDocument(input.Text, fileKind: fileKind, inGlobalNamespace: inGlobalNamespace);
        var inputText = await document.GetTextAsync(DisposalToken);
        var position = inputText.GetPosition(input.Position);

        var formattingService = (RazorFormattingService)OOPExportProvider.GetExportedValue<IRazorFormattingService>();
        var accessor = formattingService.GetTestAccessor();
        accessor.SetDebugAssertsEnabled(debugAssertsEnabled: true);
        accessor.SetFormattingLoggerFactory(new TestFormattingLoggerFactory(TestOutputHelper));

        var generatedHtml = await RemoteServiceInvoker.TryInvokeAsync<IRemoteHtmlDocumentService, string?>(document.Project.Solution,
            (service, solutionInfo, ct) => service.GetHtmlDocumentTextAsync(solutionInfo, document.Id, ct),
            DisposalToken).ConfigureAwait(false);
        Assert.NotNull(generatedHtml);

        var uri = new Uri(document.CreateUri(), $"{document.FilePath}{LanguageServerConstants.HtmlVirtualDocumentSuffix}");
        var htmlEdits = await _htmlFormattingService.GetOnTypeFormattingEditsAsync(LoggerFactory, uri, generatedHtml, position, insertSpaces: true, tabSize: 4);

        var requestInvoker = new TestHtmlRequestInvoker([(Methods.TextDocumentOnTypeFormattingName, htmlEdits)]);

        var clientSettingsManager = new ClientSettingsManager(changeTriggers: []);

        var endpoint = new CohostOnTypeFormattingEndpoint(IncompatibleProjectService, RemoteServiceInvoker, requestInvoker, clientSettingsManager, LoggerFactory);

        var request = new DocumentOnTypeFormattingParams()
        {
            TextDocument = new TextDocumentIdentifier() { DocumentUri = document.CreateDocumentUri() },
            Options = new FormattingOptions()
            {
                TabSize = tabSize,
                InsertSpaces = insertSpaces
            },
            Character = triggerCharacter.ToString(),
            Position = position
        };

        var edits = await endpoint.GetTestAccessor().HandleRequestAsync(request, document, DisposalToken);

        if (edits is null)
        {
            Assert.Equal(expected, input.Text);
            return;
        }

        var changes = edits.Select(inputText.GetTextChange);
        var finalText = inputText.WithChanges(changes);

        AssertEx.EqualOrDiff(expected, finalText.ToString());

        if (expectedChangedLines is { } changedLines)
        {
            var firstLine = changes.Min(e => inputText.GetLinePositionSpan(e.Span).Start.Line);
            var lastLine = changes.Max(e => inputText.GetLinePositionSpan(e.Span).End.Line);
            var delta = lastLine - firstLine + changes.Count(e => e.NewText.AssumeNotNull().Contains(Environment.NewLine));
            Assert.Equal(changedLines, delta + 1);
        }
    }

    private (TestCode, string, string) ProcessFormattingContext(TestCode input, string htmlFormatted, string expected)
    {
        Assert.True(_context.CreatedByFormattingDiscoverer, "Test class is using FormattingTestContext, but not using [FormattingTestFact] or [FormattingTestTheory]");

        if (_context.ShouldFlipLineEndings)
        {
            // flip the line endings of the stings (LF to CRLF and vice versa) and run again
            input = new TestCode(FormattingTestContext.FlipLineEndings(input.OriginalInput));
            expected = FormattingTestContext.FlipLineEndings(expected);
            htmlFormatted = FormattingTestContext.FlipLineEndings(htmlFormatted);
        }

        return (input, htmlFormatted, expected);
    }
}
