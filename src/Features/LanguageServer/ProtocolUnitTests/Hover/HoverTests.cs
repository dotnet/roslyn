// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
            using var testLspServer = await CreateTestLspServerAsync(markup, CapabilitiesWithVSExtensions);
            var expectedLocation = testLspServer.GetLocations("caret").Single();

            var results = await RunGetHoverAsync(testLspServer, expectedLocation).ConfigureAwait(false);

            VerifyVSContent(results, $"string A.Method(int i)|A great method|{FeaturesResources.Returns_colon}|  |a string");
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
            using var testLspServer = await CreateTestLspServerAsync(markup, CapabilitiesWithVSExtensions);
            var expectedLocation = testLspServer.GetLocations("caret").Single();

            var results = await RunGetHoverAsync(testLspServer, expectedLocation).ConfigureAwait(false);
            VerifyVSContent(results, $"string A.Method(int i)|A great method|{FeaturesResources.Exceptions_colon}|  System.NullReferenceException");
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
            using var testLspServer = await CreateTestLspServerAsync(markup, CapabilitiesWithVSExtensions);
            var expectedLocation = testLspServer.GetLocations("caret").Single();

            var results = await RunGetHoverAsync(testLspServer, expectedLocation).ConfigureAwait(false);
            VerifyVSContent(results, "string A.Method(int i)|A great method|Remarks are cool too.");
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
            using var testLspServer = await CreateTestLspServerAsync(markup, CapabilitiesWithVSExtensions);
            var expectedLocation = testLspServer.GetLocations("caret").Single();

            var results = await RunGetHoverAsync(testLspServer, expectedLocation).ConfigureAwait(false);
            VerifyVSContent(results, "string A.Method(int i)|A great method|• |Item 1.|• |Item 2.");
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
            using var testLspServer = await CreateTestLspServerAsync(markup);

            var results = await RunGetHoverAsync(testLspServer, testLspServer.GetLocations("caret").Single()).ConfigureAwait(false);
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

            using var testLspServer = await CreateXmlTestLspServerAsync(workspaceXml, clientCapabilities: CapabilitiesWithVSExtensions);
            var location = testLspServer.GetLocations("caret").Single();

            foreach (var project in testLspServer.GetCurrentSolution().Projects)
            {
                var result = await RunGetHoverAsync(testLspServer, location, project.Id);

                var expectedConstant = project.Name == "Net472" ? "Target in net472" : "Target in netcoreapp3.1";
                VerifyVSContent(result, $"({FeaturesResources.constant}) string WithConstant.Target = \"{expectedConstant}\"");
            }
        }

        [Fact]
        public async Task TestGetHoverAsync_UsingMarkupContent()
        {
            var markup =
@"class A
{
    /// <summary>
    /// A cref <see cref=""AMethod""/>
    /// <br/>
    /// <strong>strong text</strong>
    /// <br/>
    /// <em>italic text</em>
    /// <br/>
    /// <u>underline text</u>
    /// <para>
    /// <list type='bullet'>
    /// <item>
    /// <description>Item 1.</description>
    /// </item>
    /// <item>
    /// <description>Item 2.</description>
    /// </item>
    /// </list>
    /// <a href = ""https://google.com"" > link text</a>
    /// </para>
    /// </summary>
    /// <exception cref='System.NullReferenceException'>
    /// Oh no!
    /// </exception>
    /// <param name='i'>an int</param>
    /// <returns>a string</returns>
    /// <remarks>
    /// Remarks are cool too.
    /// </remarks>
    void {|caret:AMethod|}(int i)
    {
    }
}";
            var clientCapabilities = new LSP.ClientCapabilities
            {
                TextDocument = new LSP.TextDocumentClientCapabilities { Hover = new LSP.HoverSetting { ContentFormat = new LSP.MarkupKind[] { LSP.MarkupKind.Markdown } } }
            };
            using var testLspServer = await CreateTestLspServerAsync(markup, clientCapabilities);
            var expectedLocation = testLspServer.GetLocations("caret").Single();

            var expectedMarkdown = @$"```csharp
void A.AMethod(int i)
```
  
A&nbsp;cref&nbsp;A\.AMethod\(int\)  
**strong&nbsp;text**  
_italic&nbsp;text_  
<u>underline&nbsp;text</u>  
  
•&nbsp;Item&nbsp;1\.  
•&nbsp;Item&nbsp;2\.  
  
[link text](https://google.com)  
  
Remarks&nbsp;are&nbsp;cool&nbsp;too\.  
  
{FeaturesResources.Returns_colon}  
&nbsp;&nbsp;a&nbsp;string  
  
{FeaturesResources.Exceptions_colon}  
&nbsp;&nbsp;System\.NullReferenceException  
";

            var results = await RunGetHoverAsync(
                testLspServer,
                expectedLocation).ConfigureAwait(false);
            Assert.Equal(expectedMarkdown, results.Contents.Third.Value);
        }

        [Fact]
        public async Task TestGetHoverAsync_WithoutMarkdownClientSupport()
        {
            var markup =
@"class A
{
    /// <summary>
    /// A cref <see cref=""AMethod""/>
    /// <br/>
    /// <strong>strong text</strong>
    /// <br/>
    /// <em>italic text</em>
    /// <br/>
    /// <u>underline text</u>
    /// <para>
    /// <list type='bullet'>
    /// <item>
    /// <description>Item 1.</description>
    /// </item>
    /// <item>
    /// <description>Item 2.</description>
    /// </item>
    /// </list>
    /// <a href = ""https://google.com"" > link text</a>
    /// </para>
    /// </summary>
    /// <exception cref='System.NullReferenceException'>
    /// Oh no!
    /// </exception>
    /// <param name='i'>an int</param>
    /// <returns>a string</returns>
    /// <remarks>
    /// Remarks are cool too.
    /// </remarks>
    void {|caret:AMethod|}(int i)
    {
    }
}";
            using var testLspServer = await CreateTestLspServerAsync(markup);
            var expectedLocation = testLspServer.GetLocations("caret").Single();

            var expectedText = @$"void A.AMethod(int i)
A cref A.AMethod(int)
strong text
italic text
underline text

• Item 1.
• Item 2.

link text

Remarks are cool too.

{FeaturesResources.Returns_colon}
  a string

{FeaturesResources.Exceptions_colon}
  System.NullReferenceException
";

            var results = await RunGetHoverAsync(
                testLspServer,
                expectedLocation).ConfigureAwait(false);
            Assert.Equal(expectedText, results.Contents.Third.Value);
        }

        [Fact]
        public async Task TestGetHoverAsync_UsingMarkupContentProperlyEscapes()
        {
            var markup =
@"class A
{
    /// <summary>
    /// Some {curly} [braces] and (parens)
    /// <br/>
    /// #Hashtag
    /// <br/>
    /// 1 + 1 - 1
    /// <br/>
    /// Period.
    /// <br/>
    /// Exclaim!
    /// <br/>
    /// <strong>strong\** text</strong>
    /// <br/>
    /// <em>italic_ **text**</em>
    /// <br/>
    /// <a href = ""https://google.com""> closing] link</a>
    /// </summary>
    void {|caret:AMethod|}(int i)
    {
    }
}";
            var clientCapabilities = new LSP.ClientCapabilities
            {
                TextDocument = new LSP.TextDocumentClientCapabilities { Hover = new LSP.HoverSetting { ContentFormat = new LSP.MarkupKind[] { LSP.MarkupKind.Markdown } } }
            };
            using var testLspServer = await CreateTestLspServerAsync(markup, clientCapabilities);
            var expectedLocation = testLspServer.GetLocations("caret").Single();

            var expectedMarkdown = @"```csharp
void A.AMethod(int i)
```
  
Some&nbsp;\{curly\}&nbsp;\[braces\]&nbsp;and&nbsp;\(parens\)  
\#Hashtag  
1&nbsp;\+&nbsp;1&nbsp;\-&nbsp;1  
Period\.  
Exclaim\!  
**strong\\\*\*&nbsp;text**  
_italic\_&nbsp;\*\*text\*\*_  
[closing\] link](https://google.com)  
";

            var results = await RunGetHoverAsync(
                testLspServer,
                expectedLocation).ConfigureAwait(false);
            Assert.Equal(expectedMarkdown, results.Contents.Third.Value);
        }

        private static async Task<LSP.Hover> RunGetHoverAsync(
            TestLspServer testLspServer,
            LSP.Location caret,
            ProjectId projectContext = null)
        {
            return await testLspServer.ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.Hover>(LSP.Methods.TextDocumentHoverName,
                CreateTextDocumentPositionParams(caret, projectContext), CancellationToken.None);
        }

        private void VerifyVSContent(LSP.Hover hover, string expectedContent)
        {
            var vsHover = Assert.IsType<LSP.VSInternalHover>(hover);
            var containerElement = (ContainerElement)vsHover.RawContent;
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
