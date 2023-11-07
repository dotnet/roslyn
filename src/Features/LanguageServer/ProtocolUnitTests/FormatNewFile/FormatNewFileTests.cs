﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.Razor;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.SimplifyMethod
{
    public class FormatNewFileTests(ITestOutputHelper? testOutputHelper) : AbstractLanguageServerProtocolTests(testOutputHelper)
    {
        [Theory, CombinatorialData]
        public async Task TestFormatNewFileAsync(bool mutatingLspWorkspace)
        {
            var markup =
                """
                // This is a file header

                using System;
                using System.Threading.Tasks;

                namespace test;

                class Goo
                {
                    public void M()
                    {
                    }
                }
                """;

            var input = """
                using System;

                namespace test;

                public partial class MyComponent
                {
                }
                """;

            var expected = """
                // This is a file header
                
                namespace test
                {
                    public partial class MyComponent
                    {
                    }
                }
                """;

            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

            var newFilePath = "C:\\MyComponent.razor.cs";

            var result = await RunHandlerAsync(testLspServer, newFilePath, input);
            AssertEx.EqualOrDiff(expected, result);
        }

        private static async Task<string?> RunHandlerAsync(TestLspServer testLspServer, string newFilePath, string input)
        {
            var project = testLspServer.GetCurrentSolution().Projects.First();
            Contract.ThrowIfNull(project.FilePath);

            var parameters = new FormatNewFileParams()
            {
                Project = new TextDocumentIdentifier
                {
                    Uri = ProtocolConversions.CreateAbsoluteUri(project.FilePath)
                },
                Document = new TextDocumentIdentifier
                {
                    Uri = ProtocolConversions.CreateAbsoluteUri(newFilePath)
                },
                Contents = input
            };

            return await testLspServer.ExecuteRequestAsync<FormatNewFileParams, string?>(FormatNewFileHandler.FormatNewFileMethodName, parameters, CancellationToken.None);
        }
    }
}
