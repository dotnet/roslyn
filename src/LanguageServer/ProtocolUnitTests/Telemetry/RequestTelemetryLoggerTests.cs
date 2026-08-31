// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests.Logging;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Telemetry;

[UseExportProvider]
public sealed class RequestTelemetryLoggerTests
{
    [Fact]
    public async Task ReportsEmptyResultPosition()
    {
        using var workspace = TestWorkspace.CreateCSharp(
            """
            class C
            {

            }
            """);
        var document = workspace.CurrentSolution.Projects.Single().Documents.Single();
        var logger = new TestTelemetryLogger();

        await RequestTelemetryLogger.ReportEmptySymbolResultAsync(
            logger,
            WellKnownLspServerKinds.CSharpVisualBasicLspServer.ToTelemetryString(),
            Methods.TextDocumentDefinitionName,
            document,
            new LinePosition(line: 2, character: 0),
            CancellationToken.None);

        var telemetryEvent = Assert.Single(logger.PostedEvents);
        Assert.Equal("vs/ide/vbcs/lsp/symbolrequest/emptyresult", telemetryEvent.Name);
        var properties = telemetryEvent.Properties;
        var text = await document.GetTextAsync();
        var root = await document.GetSyntaxRootAsync();
        var absolutePosition = text.Lines[2].Start;

        Assert.Equal(absolutePosition, properties["vs.ide.vbcs.lsp.symbolrequest.emptyresult.absoluteposition"]);
        Assert.Equal(0, properties["vs.ide.vbcs.lsp.symbolrequest.emptyresult.character"]);
        Assert.Equal(LanguageNames.CSharp, properties["vs.ide.vbcs.lsp.symbolrequest.emptyresult.language"]);
        Assert.Equal(2, properties["vs.ide.vbcs.lsp.symbolrequest.emptyresult.line"]);
        Assert.Equal(text.Lines.Count, properties["vs.ide.vbcs.lsp.symbolrequest.emptyresult.linecount"]);
        Assert.Equal(0, properties["vs.ide.vbcs.lsp.symbolrequest.emptyresult.linelength"]);
        Assert.Equal(Methods.TextDocumentDefinitionName, properties["vs.ide.vbcs.lsp.symbolrequest.emptyresult.method"]);
        Assert.Equal("EndOfLine", properties["vs.ide.vbcs.lsp.symbolrequest.emptyresult.positionkind"]);
        Assert.Equal(WellKnownLspServerKinds.CSharpVisualBasicLspServer.ToTelemetryString(), properties["vs.ide.vbcs.lsp.symbolrequest.emptyresult.server"]);
        Assert.Equal(root!.FindToken(absolutePosition, findInsideTrivia: true).RawKind, properties["vs.ide.vbcs.lsp.symbolrequest.emptyresult.tokenrawkind"]);
        Assert.Equal(document.Project.Solution.WorkspaceKind, properties["vs.ide.vbcs.lsp.symbolrequest.emptyresult.workspacekind"]);
    }
}

[CollectionDefinition(nameof(RequestTelemetryHandlerTests), DisableParallelization = true)]
public sealed class RequestTelemetryHandlerTestsCollection;

[Collection(nameof(RequestTelemetryHandlerTests))]
[UseExportProvider]
public sealed class RequestTelemetryHandlerTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    [Theory, CombinatorialData]
    public async Task StreamedReferencesWithResultsAreNotReportedAsEmpty(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class C
            {
                void M()
                {
                    {|caret:|}M();
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, CapabilitiesWithVSExtensions);
        using var progress = BufferedProgress.Create<object>(null);
        var logger = new TestTelemetryLogger();
        var previousLogger = Logger.SetLogger(logger);

        try
        {
            var results = await References.FindAllReferencesHandlerTests.RunFindAllReferencesAsync(
                testLspServer,
                testLspServer.GetLocations("caret").Single(),
                progress);

            Assert.NotEmpty(results);
            Assert.DoesNotContain(
                logger.PostedEvents,
                telemetryEvent => telemetryEvent.Name == "vs/ide/vbcs/lsp/symbolrequest/emptyresult");
        }
        finally
        {
            Logger.SetLogger(previousLogger);
        }
    }
}
