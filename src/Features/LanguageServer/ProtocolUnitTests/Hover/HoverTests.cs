// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Hover
{
    public class HoverTests : AbstractLanguageServerProtocolTests
    {
        [Fact]
        public async Task TestGetHoverAsync()
        {
            var markup =
@"class A
{
    /// <summary>
    /// A great method
    /// </summary>
    /// <param name='i'>an int</param>
    /// <returns>a string</returns>
    private string {|caret:Method|}(int i)
    {
    }
}";
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var expectedLocation = locations["caret"].Single();
            var expected = CreateHover(expectedLocation, "string A.Method(int i)\r\n> A great method");

            var results = await RunGetHoverAsync(workspace.CurrentSolution, expectedLocation).ConfigureAwait(false);
            AssertJsonEquals(expected, results);
        }

        [Fact]
        public async Task TestGetHoverAsync_InvalidLocation()
        {
            var markup =
@"class A
{
    /// <summary>
    /// A great method
    /// </summary>
    /// <param name='i'>an int</param>
    /// <returns>a string</returns>
    private string Method(int i)
    {
        {|caret:|}
    }
}";
            using var workspace = CreateTestWorkspace(markup, out var locations);

            var results = await RunGetHoverAsync(workspace.CurrentSolution, locations["caret"].Single()).ConfigureAwait(false);
            Assert.Null(results);
        }

        private static async Task<LSP.Hover> RunGetHoverAsync(Solution solution, LSP.Location caret)
            => await GetLanguageServer(solution).GetHoverAsync(solution, CreateTextDocumentPositionParams(caret), new LSP.ClientCapabilities(), CancellationToken.None);

        private static LSP.Hover CreateHover(LSP.Location location, string text)
            => new LSP.Hover()
            {
                Contents = new LSP.MarkupContent()
                {
                    Kind = LSP.MarkupKind.Markdown,
                    Value = text
                },
                Range = location.Range
            };
    }
}
