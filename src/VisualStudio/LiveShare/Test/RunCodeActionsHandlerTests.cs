// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.LanguageServer.CustomProtocol;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json.Linq;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.UnitTests
{
    public class RunCodeActionsHandlerTests : AbstractLiveShareRequestHandlerTests
    {
        [WpfFact]
        public async Task TestRunCodeActionsAsync()
        {
            var markup =
@"class A
{
    void M()
    {
        {|caret:|}int i = 1;
    }
}";
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var codeActionLocation = locations["caret"].First();

            var results = await TestHandleAsync<LSP.ExecuteCommandParams, object>(workspace.CurrentSolution, CreateExecuteCommandParams(codeActionLocation, CSharpAnalyzersResources.Use_implicit_type), Methods.WorkspaceExecuteCommandName);
            Assert.True((bool)results);
        }

        private static LSP.ExecuteCommandParams CreateExecuteCommandParams(LSP.Location location, string title)
            => new LSP.ExecuteCommandParams()
            {
                Command = "_liveshare.remotecommand.Roslyn",
                Arguments = new object[]
                {
                    JObject.FromObject(new LSP.Command()
                    {
                        CommandIdentifier = "Roslyn.RunCodeAction",
                        Arguments = new RunCodeActionParams[]
                        {
                            CreateRunCodeActionParams(title, location)
                        },
                        Title = title
                    })
                }
            };
    }
}
