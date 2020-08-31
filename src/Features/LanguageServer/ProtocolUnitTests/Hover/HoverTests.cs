// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Text.Adornments;
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

            var results = await RunGetHoverAsync(workspace.CurrentSolution, expectedLocation).ConfigureAwait(false);

            VerifyContent(results, $"string A.Method(int i)|A great method|{FeaturesResources.Returns_colon}|  |a string");
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

            var results = await RunGetHoverAsync(workspace.CurrentSolution, expectedLocation).ConfigureAwait(false);
            VerifyContent(results, $"string A.Method(int i)|A great method|{FeaturesResources.Exceptions_colon}|  System.NullReferenceException");
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

            var results = await RunGetHoverAsync(workspace.CurrentSolution, expectedLocation).ConfigureAwait(false);
            VerifyContent(results, "string A.Method(int i)|A great method|Remarks are cool too.");
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

            var results = await RunGetHoverAsync(workspace.CurrentSolution, expectedLocation).ConfigureAwait(false);
            VerifyContent(results, "string A.Method(int i)|A great method|• |Item 1.|• |Item 2.");
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

        // Test that if we pass a project context along to hover, the right context is chosen. We are using hover
        // as a general proxy to test that our context tracking works in all cases, although there's nothing specific
        // about hover that needs to be different here compared to any other feature.
        [Fact]
        public async Task TestGetHoverWithProjectContexts()
        {
            var source = @"
using System;

#if NET472

class WithConstant
{
    public const string Target = ""Target in net472"";
}

#else

class WithConstant
{
    public const string Target = ""Target in netcoreapp3.1"";
}

#endif

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine(WithConstant.{|caret:Target|});
    }
}";

            var workspaceXml =
$@"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Net472"" PreprocessorSymbols=""NET472"">
        <Document FilePath=""C:\C.cs""><![CDATA[${source}]]></Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""NetCoreApp3"" PreprocessorSymbols=""NETCOREAPP3.1"">
        <Document IsLinkFile=""true"" LinkFilePath=""C:\C.cs"" LinkAssemblyName=""Net472""></Document>
    </Project>
</Workspace>";

            using var workspace = CreateXmlTestWorkspace(workspaceXml, out var locations);
            var location = locations["caret"].Single();

            foreach (var project in workspace.Projects)
            {
                var result = await RunGetHoverAsync(workspace.CurrentSolution, location, project.Id);

                var expectedConstant = project.Name == "Net472" ? "Target in net472" : "Target in netcoreapp3.1";
                VerifyContent(result, $"({FeaturesResources.constant}) string WithConstant.Target = \"{expectedConstant}\"");
            }
        }

        private static async Task<LSP.VSHover> RunGetHoverAsync(Solution solution, LSP.Location caret, ProjectId projectContext = null)
            => (LSP.VSHover)await GetLanguageServer(solution).ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.Hover>(LSP.Methods.TextDocumentHoverName,
                CreateTextDocumentPositionParams(caret, projectContext), new LSP.ClientCapabilities(), null, CancellationToken.None);

        private void VerifyContent(LSP.VSHover result, string expectedContent)
        {
            var containerElement = (ContainerElement)result.RawContent;
            using var _ = ArrayBuilder<ClassifiedTextElement>.GetInstance(out var classifiedTextElements);
            GetClassifiedTextElements(containerElement, classifiedTextElements);
            Assert.False(classifiedTextElements.SelectMany(classifiedTextElements => classifiedTextElements.Runs).Any(run => run.NavigationAction != null));
            var content = string.Join("|", classifiedTextElements.Select(cte => string.Join(string.Empty, cte.Runs.Select(ctr => ctr.Text))));
            Assert.Equal(expectedContent, content);
        }

        private void GetClassifiedTextElements(ContainerElement container, ArrayBuilder<ClassifiedTextElement> classifiedTextElements)
        {
            foreach (var element in container.Elements)
            {
                if (element is ClassifiedTextElement classifiedTextElement)
                {
                    classifiedTextElements.Add(classifiedTextElement);
                }
                else if (element is ContainerElement containerElement)
                {
                    GetClassifiedTextElements(containerElement, classifiedTextElements);
                }
            }
        }
    }
}
