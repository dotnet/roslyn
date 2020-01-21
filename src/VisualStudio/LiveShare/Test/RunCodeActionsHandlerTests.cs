// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.LanguageServer.CustomProtocol;
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
            var (solution, ranges) = CreateTestSolution(markup);
            var codeActionLocation = ranges["caret"].First();

            var results = await TestHandleAsync<LSP.ExecuteCommandParams, object>(solution, CreateExecuteCommandParams(codeActionLocation, CSharpFeaturesResources.Use_implicit_type));
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
