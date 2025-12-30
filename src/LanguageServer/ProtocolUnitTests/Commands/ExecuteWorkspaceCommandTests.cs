// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Commands;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Commands;

public sealed class ExecuteWorkspaceCommandTests : AbstractLanguageServerProtocolTests
{
    protected override TestComposition Composition => base.Composition.AddParts(
            typeof(TestWorkspaceCommandHandler));

    public ExecuteWorkspaceCommandTests(ITestOutputHelper? testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory, CombinatorialData]
    public async Task TestExecuteWorkspaceCommand(bool mutatingLspWorkspace)
    {
        await using var server = await CreateTestLspServerAsync("", mutatingLspWorkspace);

        var request = new ExecuteCommandParams()
        {
            Arguments = [JsonSerializer.Serialize(new TextDocumentIdentifier { DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(@"C:\someFile.cs") })],
            Command = TestWorkspaceCommandHandler.CommandName
        };
        var response = await server.ExecuteRequestAsync<ExecuteCommandParams, object>(Methods.WorkspaceExecuteCommandName, request, CancellationToken.None);
        AssertEx.NotNull(response);
        Assert.True((bool)response);

    }

    [ExportCSharpVisualBasicStatelessLspService(typeof(TestWorkspaceCommandHandler)), Shared, PartNotDiscoverable]
    [Command(CommandName)]
    internal sealed class TestWorkspaceCommandHandler : AbstractExecuteWorkspaceCommandHandler
    {
        internal const string CommandName = nameof(TestWorkspaceCommandHandler);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestWorkspaceCommandHandler()
        {
        }

        public override string Command => CommandName;

        public override bool MutatesSolutionState => false;

        public override bool RequiresLSPSolution => true;

        public override TextDocumentIdentifier GetTextDocumentIdentifier(ExecuteCommandParams request)
        {
            return JsonSerializer.Deserialize<TextDocumentIdentifier>((JsonElement)request.Arguments!.First(), ProtocolConversions.LspJsonSerializerOptions)!;
        }

        public override async Task<object> HandleRequestAsync(ExecuteCommandParams request, RequestContext context, CancellationToken cancellationToken)
        {
            return true;
        }
    }
}
