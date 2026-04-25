// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Settings;
using Microsoft.CodeAnalysis.Razor.Workspaces.Settings;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.Formatting;

public abstract class DocumentFormattingTestBase(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    private protected async Task RunFormattingTestAsync(
        TestCode input,
        string htmlFormatted,
        string expected,
        RazorFileKind? fileKind = null,
        bool inGlobalNamespace = false,
        bool codeBlockBraceOnNextLine = false,
        AttributeIndentStyle attributeIndentStyle = AttributeIndentStyle.AlignWithFirst,
        bool insertSpaces = true,
        int tabSize = 4,
        bool allowDiagnostics = false,
        bool debugAssertsEnabled = true,
        bool validateHtmlFormattedMatchesWebTools = true,
        RazorCSharpSyntaxFormattingOptions? csharpSyntaxFormattingOptions = null,
        (string fileName, string contents)[]? additionalFiles = null)
    {
        var document = CreateProjectAndRazorDocument(input.Text, fileKind, inGlobalNamespace: inGlobalNamespace, additionalFiles: additionalFiles);
        if (!allowDiagnostics)
        {
            //TODO: Tests in LanguageServer have extra components that are not present in this project, like Counter, etc.
            //      so we can't validate for diagnostics here until we make them the same. Since the test inputs are all
            //      shared this doesn't really matter while the language server tests are present.
            //var snapshotManager = OOPExportProvider.GetExportedValue<RemoteSnapshotManager>();
            //var snapshot = snapshotManager.GetSnapshot(document);
            //var codeDocument = await snapshot.GetGeneratedOutputAsync(DisposalToken);
            //var csharpDocument = codeDocument.GetCSharpDocument();
            //Assert.False(csharpDocument.Diagnostics.Any(), "Error creating document:" + Environment.NewLine + string.Join(Environment.NewLine, csharpDocument.Diagnostics));
        }

        csharpSyntaxFormattingOptions ??= RazorCSharpSyntaxFormattingOptions.Default;

        var formattingService = (RazorFormattingService)OOPExportProvider.GetExportedValue<IRazorFormattingService>();
        var accessor = formattingService.GetTestAccessor();
        accessor.SetDebugAssertsEnabled(debugAssertsEnabled);
        accessor.SetFormattingLoggerFactory(new TestFormattingLoggerFactory(TestOutputHelper));

        var generatedHtml = await RemoteServiceInvoker.TryInvokeAsync<IRemoteHtmlDocumentService, string?>(document.Project.Solution,
            (service, solutionInfo, ct) => service.GetHtmlDocumentTextAsync(solutionInfo, document.Id, ct),
            DisposalToken).ConfigureAwait(false);
        Assert.NotNull(generatedHtml);

        var uri = new Uri(document.CreateUri(), $"{document.FilePath}{LanguageServerConstants.HtmlVirtualDocumentSuffix}");
        TextEdit[] htmlEdits = [];

        var source = SourceText.From(input.Text);
        var htmlChanges = SourceText.From(htmlFormatted).GetTextChanges(source);
        htmlEdits = [.. htmlChanges.Select(source.GetTextEdit)];

        if (validateHtmlFormattedMatchesWebTools)
        {
#if NETFRAMEWORK
            var htmlFormattingService = new HtmlFormattingService();
            // Lets make sure everything is working as we expect in our tests
            var htmlEditsResult = await htmlFormattingService.GetDocumentFormattingEditsAsync(LoggerFactory, uri, generatedHtml, insertSpaces, tabSize);
            var htmlChangesResult = htmlEditsResult.Select(source.GetTextChange);

            var htmlEdited = source.WithChanges(htmlChanges);
            var htmlEditedLegacy = source.WithChanges(htmlChangesResult ?? []);
            AssertEx.EqualOrDiff(htmlEdited.ToString(), htmlEditedLegacy.ToString(), message: "htmlFormatted doesn't match WebTools");
            AssertEx.EqualOrDiff(htmlFormatted, htmlEditedLegacy.ToString());
            AssertEx.EqualOrDiff(htmlFormatted, htmlEdited.ToString());
#endif
        }

        var span = input.TryGetNamedSpans(string.Empty, out var spans)
            ? spans.First()
            : default;

        var edits = await GetFormattingEditsAsync(document, htmlEdits, span, codeBlockBraceOnNextLine, attributeIndentStyle, insertSpaces, tabSize, csharpSyntaxFormattingOptions);

        if (edits is null)
        {
            AssertEx.EqualOrDiff(expected, input.Text);
            return;
        }

        var inputText = await document.GetTextAsync(DisposalToken);
        var changes = edits.Select(inputText.GetTextChange);
        var finalText = inputText.WithChanges(changes);

        AssertEx.EqualOrDiff(expected, finalText.ToString());
    }

    private protected async Task<TextEdit[]?> GetFormattingEditsAsync(TextDocument document, TextEdit[]? htmlEdits, TextSpan span, bool codeBlockBraceOnNextLine, AttributeIndentStyle attributeIndentStyle, bool insertSpaces, int tabSize, RazorCSharpSyntaxFormattingOptions csharpSyntaxFormattingOptions)
    {
        var requestInvoker = new TestHtmlRequestInvoker([(Methods.TextDocumentFormattingName, htmlEdits)]);

        ClientSettingsManager.Update(ClientSettingsManager.GetClientSettings().AdvancedSettings with
        {
            CodeBlockBraceOnNextLine = codeBlockBraceOnNextLine,
            AttributeIndentStyle = attributeIndentStyle,
        });

        var edits = await GetFormattingEditsInternalAsync(span, insertSpaces, tabSize, document, requestInvoker, ClientSettingsManager, csharpSyntaxFormattingOptions);
        return edits;
    }

    private async Task<TextEdit[]?> GetFormattingEditsInternalAsync(
       TextSpan span,
       bool insertSpaces,
       int tabSize,
       TextDocument document,
       IHtmlRequestInvoker requestInvoker,
       IClientSettingsManager clientSettingsManager,
       RazorCSharpSyntaxFormattingOptions csharpSyntaxFormattingOptions)
    {
        if (span.IsEmpty)
        {
            var endpoint = new CohostDocumentFormattingEndpoint(IncompatibleProjectService, RemoteServiceInvoker, requestInvoker, clientSettingsManager, LoggerFactory);
            var request = new DocumentFormattingParams()
            {
                TextDocument = new TextDocumentIdentifier() { DocumentUri = document.CreateDocumentUri() },
                Options = new FormattingOptions()
                {
                    TabSize = tabSize,
                    InsertSpaces = insertSpaces
                }
            };

            return await endpoint.GetTestAccessor().HandleRequestAsync(request, document, csharpSyntaxFormattingOptions, DisposalToken);
        }

        var inputText = await document.GetTextAsync(DisposalToken);
        var rangeEndpoint = new CohostRangeFormattingEndpoint(IncompatibleProjectService, RemoteServiceInvoker, requestInvoker, clientSettingsManager, LoggerFactory);
        var rangeRequest = new DocumentRangeFormattingParams()
        {
            TextDocument = new TextDocumentIdentifier() { DocumentUri = document.CreateDocumentUri() },
            Options = new FormattingOptions()
            {
                TabSize = 4,
                InsertSpaces = true
            },
            Range = inputText.GetRange(span)
        };

        return await rangeEndpoint.GetTestAccessor().HandleRequestAsync(rangeRequest, document, DisposalToken);
    }
}
