// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.Razor;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.SimplifyMethod
{
    public class SimplifyMethodTests : AbstractLanguageServerProtocolTests
    {
        public SimplifyMethodTests(ITestOutputHelper? testOutputHelper) : base(testOutputHelper)
        {
        }

        [Theory, CombinatorialData]
        public async Task TestGetSimplifyMethodAsync(bool mutatingLspWorkspace)
        {
            var markup =
@"
using System;
using System.Threading.Tasks;
namespace test;
class A
{
{|caret:|}
}";
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
            var methodInsertionLocation = testLspServer.GetLocations("caret").First();
            var method = "private global::System.Threading.Tasks.Task test() => throw new global::System.NotImplementedException();";
            var expectedEdit = new TextEdit()
            {
                NewText = "private Task test() => throw new NotImplementedException();",
                Range = methodInsertionLocation.Range,
            };

            var results = await RunRenameAsync(testLspServer, CreateSimplifyMethodParams(methodInsertionLocation, method));
            Assert.NotNull(results);
            AssertJsonEquals(expectedEdit, results.First());
        }

        private static async Task<TextEdit[]?> RunRenameAsync(TestLspServer testLspServer, SimplifyMethodParams @params)
        {
            return await testLspServer.ExecuteRequestAsync<SimplifyMethodParams, TextEdit[]>(SimplifyMethodHandler.SimplifyMethodMethodName, @params, CancellationToken.None);
        }

        private static SimplifyMethodParams CreateSimplifyMethodParams(LSP.Location location, string newText)
            => new SimplifyMethodParams()
            {
                TextDocument = CreateTextDocumentIdentifier(location.Uri),
                TextEdit = new TextEdit() { Range = location.Range, NewText = newText },
            };
    }
}
