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

            var expected = CreateHover(expectedLocation, $"string A.Method(int i)\r\n A great method\r\n\r\n{FeaturesResources.Returns_colon}\r\n  a string");

            var results = await RunGetHoverAsync(workspace.CurrentSolution, expectedLocation).ConfigureAwait(false);
            AssertJsonEquals(expected, results);
        }

        [Fact]
        public async Task TestGetHoverAsync_WithExceptions()
        {
            var markup =
@"class A
{
    /// <summary>
    /// A great method
    /// </summary>
    /// <exception cref='System.NullReferenceException'>
    /// Oh no!
    /// </exception>
    private string {|caret:Method|}(int i)
    {
    }
}";
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var expectedLocation = locations["caret"].Single();
            var expected = CreateHover(expectedLocation, $"string A.Method(int i)\r\n A great method\r\n\r\n{FeaturesResources.Exceptions_colon}\r\n  System.NullReferenceException");

            var results = await RunGetHoverAsync(workspace.CurrentSolution, expectedLocation).ConfigureAwait(false);
            AssertJsonEquals(expected, results);
        }

        [Fact]
        public async Task TestGetHoverAsync_WithRemarks()
        {
            var markup =
@"class A
{
    /// <summary>
    /// A great method
    /// </summary>
    /// <remarks>
    /// Remarks are cool too.
    /// </remarks>
    private string {|caret:Method|}(int i)
    {
    }
}";
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var expectedLocation = locations["caret"].Single();
            var expected = CreateHover(expectedLocation, "string A.Method(int i)\r\n A great method\r\n\r\nRemarks are cool too.");

            var results = await RunGetHoverAsync(workspace.CurrentSolution, expectedLocation).ConfigureAwait(false);
            AssertJsonEquals(expected, results);
        }

        [Fact]
        public async Task TestGetHoverAsync_WithList()
        {
            var markup =
@"class A
{
    /// <summary>
    /// A great method
    /// <list type='bullet'>
    /// <item>
    /// <description>Item 1.</description>
    /// </item>
    /// <item>
    /// <description>Item 2.</description>
    /// </item>
    /// </list>
    /// </summary>
    private string {|caret:Method|}(int i)
    {
    }
}";
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var expectedLocation = locations["caret"].Single();
            var expected = CreateHover(expectedLocation, "string A.Method(int i)\r\n A great method\r\n\r\n• Item 1.\r\n• Item 2.");

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
