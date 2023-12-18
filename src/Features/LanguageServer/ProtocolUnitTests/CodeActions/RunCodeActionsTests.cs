// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;
using Newtonsoft.Json.Linq;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.CodeActions
{
    public class RunCodeActionsTests : AbstractLanguageServerProtocolTests
    {
        public RunCodeActionsTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [WpfTheory(Skip = "https://github.com/dotnet/roslyn/issues/65303"), CombinatorialData]
        public async Task TestRunCodeActions(bool mutatingLspWorkspace)
        {
            var markup =
@"class A
{
    class {|caret:|}B
    {
    }
}";

            var expectedTextForB =
@"partial class A
{
    class B
    {
    }
}";

            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
            var caretLocation = testLspServer.GetLocations("caret").Single();
            var documentId = new LSP.TextDocumentIdentifier
            {
                Uri = caretLocation.Uri
            };

            var titlePath = new[] { string.Format(FeaturesResources.Move_type_to_0, "B.cs") };
            var commandArgument = new CodeActionResolveData(string.Format(FeaturesResources.Move_type_to_0, "B.cs"), customTags: ImmutableArray<string>.Empty, caretLocation.Range, documentId, fixAllFlavors: null, nestedCodeActions: null, codeActionPath: titlePath);

            var results = await ExecuteRunCodeActionCommandAsync(testLspServer, commandArgument);

            var documentForB = testLspServer.TestWorkspace.CurrentSolution.Projects.Single().Documents.Single(doc => doc.Name.Equals("B.cs", StringComparison.OrdinalIgnoreCase));
            var textForB = await documentForB.GetTextAsync();
            Assert.Equal(expectedTextForB, textForB.ToString());
        }

        private static async Task<bool> ExecuteRunCodeActionCommandAsync(
            TestLspServer testLspServer,
            CodeActionResolveData codeActionData)
        {
            var command = new LSP.ExecuteCommandParams
            {
                Command = CodeActionsHandler.RunCodeActionCommandName,
                Arguments =
                [
                    JToken.FromObject(codeActionData)
                ]
            };

            var result = await testLspServer.ExecuteRequestAsync<LSP.ExecuteCommandParams, object>(
                LSP.Methods.WorkspaceExecuteCommandName, command, CancellationToken.None);
            Contract.ThrowIfNull(result);
            return (bool)result;
        }
    }
}
