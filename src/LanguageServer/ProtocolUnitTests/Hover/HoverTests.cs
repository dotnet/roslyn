// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Test.Utilities;
using Roslyn.Text.Adornments;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Hover;

public sealed class HoverTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    [Theory, CombinatorialData]
    public async Task TestGetHoverAsync(bool mutatingLspWorkspace)
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
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, CapabilitiesWithVSExtensions);
        var expectedLocation = testLspServer.GetLocations("caret").Single();

        var results = await RunGetHoverAsync(testLspServer, expectedLocation).ConfigureAwait(false);

        VerifyVSContent(results, $"string A.Method(int i)|A great method|{FeaturesResources.Returns_colon}|  |a string");
    }

    [Theory, CombinatorialData]
    public async Task TestGetHoverAsync_WithExceptions(bool mutatingLspWorkspace)
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
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, CapabilitiesWithVSExtensions);
        var expectedLocation = testLspServer.GetLocations("caret").Single();

        var results = await RunGetHoverAsync(testLspServer, expectedLocation).ConfigureAwait(false);
        VerifyVSContent(results, $"string A.Method(int i)|A great method|{WorkspacesResources.Exceptions_colon}|  System.NullReferenceException");
    }

    [Theory, CombinatorialData]
    public async Task TestGetHoverAsync_WithRemarks(bool mutatingLspWorkspace)
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
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, CapabilitiesWithVSExtensions);
        var expectedLocation = testLspServer.GetLocations("caret").Single();

        var results = await RunGetHoverAsync(testLspServer, expectedLocation).ConfigureAwait(false);
        VerifyVSContent(results, "string A.Method(int i)|A great method|Remarks are cool too.");
    }

    [Theory, CombinatorialData]
    public async Task TestGetHoverAsync_WithList(bool mutatingLspWorkspace)
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
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, CapabilitiesWithVSExtensions);
        var expectedLocation = testLspServer.GetLocations("caret").Single();

        var results = await RunGetHoverAsync(testLspServer, expectedLocation).ConfigureAwait(false);
        VerifyVSContent(results, "string A.Method(int i)|A great method|• |Item 1.|• |Item 2.");
    }

    [Theory, CombinatorialData]
    public async Task TestGetHoverAsync_InvalidLocation(bool mutatingLspWorkspace)
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
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var results = await RunGetHoverAsync(testLspServer, testLspServer.GetLocations("caret").Single()).ConfigureAwait(false);
        Assert.Null(results);
    }

    // Test that if we pass a project context along to hover, the right context is chosen. We are using hover
    // as a general proxy to test that our context tracking works in all cases, although there's nothing specific
    // about hover that needs to be different here compared to any other feature.
    [Theory, CombinatorialData]
    public async Task TestGetHoverWithProjectContexts(bool mutatingLspWorkspace)
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

        await using var testLspServer = await CreateXmlTestLspServerAsync(workspaceXml, mutatingLspWorkspace, initializationOptions: new InitializationOptions { ClientCapabilities = CapabilitiesWithVSExtensions });
        var location = testLspServer.GetLocations("caret").Single();

        foreach (var project in testLspServer.GetCurrentSolution().Projects)
        {
            var result = await RunGetHoverAsync(testLspServer, location, project.Id);

            var expectedConstant = project.Name == "Net472" ? "Target in net472" : "Target in netcoreapp3.1";
            VerifyVSContent(result, $"({FeaturesResources.constant}) string WithConstant.Target = \"{expectedConstant}\"");
        }
    }

    [Theory, CombinatorialData]
    public async Task TestGetHoverAsync_UsingMarkupContent(bool mutatingLspWorkspace)
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
            TextDocument = new LSP.TextDocumentClientCapabilities { Hover = new LSP.HoverSetting { ContentFormat = [LSP.MarkupKind.Markdown] } }
        };
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, clientCapabilities);
        var expectedLocation = testLspServer.GetLocations("caret").Single();
        var results = await RunGetHoverAsync(
            testLspServer,
            expectedLocation).ConfigureAwait(false);
        Assert.Equal(@$"```csharp
void A.AMethod(int i)
```
  
A cref&nbsp;A\.AMethod\(int\)  
**strong text**  
_italic text_  
<u>underline text</u>  
  
•&nbsp;Item 1\.  
•&nbsp;Item 2\.  
  
[link text](https://google.com)  
  
Remarks are cool too\.  
  
{FeaturesResources.Returns_colon}  
&nbsp;&nbsp;a string  
  
{WorkspacesResources.Exceptions_colon}  
&nbsp;&nbsp;System\.NullReferenceException  
", results.Contents.Fourth.Value);
    }

    [Theory, CombinatorialData]
    public async Task TestGetHoverAsync_WithoutMarkdownClientSupport(bool mutatingLspWorkspace)
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
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var expectedLocation = testLspServer.GetLocations("caret").Single();
        var results = await RunGetHoverAsync(
            testLspServer,
            expectedLocation).ConfigureAwait(false);
        Assert.Equal(@$"void A.AMethod(int i)
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

{WorkspacesResources.Exceptions_colon}
  System.NullReferenceException
", results.Contents.Fourth.Value);
    }

    [Theory, CombinatorialData]
    public async Task TestGetHoverAsync_UsingMarkupContentProperlyEscapes(bool mutatingLspWorkspace)
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
            TextDocument = new LSP.TextDocumentClientCapabilities { Hover = new LSP.HoverSetting { ContentFormat = [LSP.MarkupKind.Markdown] } }
        };
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, clientCapabilities);
        var expectedLocation = testLspServer.GetLocations("caret").Single();
        var results = await RunGetHoverAsync(
            testLspServer,
            expectedLocation).ConfigureAwait(false);
        Assert.Equal(@"```csharp
void A.AMethod(int i)
```
  
Some \{curly\} \[braces\] and \(parens\)  
\#Hashtag  
1 \+ 1 \- 1  
Period\.  
Exclaim\!  
**strong\\\*\* text**  
_italic\_ \*\*text\*\*_  
[closing\] link](https://google.com)  
", results.Contents.Fourth.Value);
    }

    [Theory, CombinatorialData]
    public async Task TestGetHoverAsync_UsingMarkupContentDoesNotEscapeCode(bool mutatingLspWorkspace)
    {
        var markup =
@"class A
{
    /// <summary>
    /// <c>
    /// if (true) {
    ///     Console.WriteLine(""hello"");
    /// }
    /// </c>
    /// </summary>
    void {|caret:AMethod|}(int i)
    {
    }
}";
        var clientCapabilities = new LSP.ClientCapabilities
        {
            TextDocument = new LSP.TextDocumentClientCapabilities { Hover = new LSP.HoverSetting { ContentFormat = [LSP.MarkupKind.Markdown] } }
        };
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, clientCapabilities);
        var expectedLocation = testLspServer.GetLocations("caret").Single();
        var results = await RunGetHoverAsync(
            testLspServer,
            expectedLocation).ConfigureAwait(false);
        Assert.Equal(@"```csharp
void A.AMethod(int i)
```
  
`if (true) { Console.WriteLine(""hello""); }`  
", results.Contents.Fourth.Value);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/75181")]
    public async Task TestGetHoverAsync_UsingMarkupContentDoesNotEscapeCodeBlock(bool mutatingLspWorkspace)
    {
        var markup =
@"class A
{
    /// <summary>
    /// <code>
    /// if (true) {
    ///     Console.WriteLine(""hello"");
    /// }
    /// </code>
    /// </summary>
    void {|caret:AMethod|}(int i)
    {
    }
}";
        var clientCapabilities = new LSP.ClientCapabilities
        {
            TextDocument = new LSP.TextDocumentClientCapabilities { Hover = new LSP.HoverSetting { ContentFormat = [LSP.MarkupKind.Markdown] } }
        };
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, clientCapabilities);
        var expectedLocation = testLspServer.GetLocations("caret").Single();
        var results = await RunGetHoverAsync(
            testLspServer,
            expectedLocation).ConfigureAwait(false);
        Assert.Equal(@"```csharp
void A.AMethod(int i)
```
  

```text
if (true) {
    Console.WriteLine(""hello"");
}
```  
", results.Contents.Fourth.Value);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/microsoft/vscode-dotnettools/issues/1499")]
    public async Task TestGetHoverAsync_UsingMarkupContentEscapesBacktickInCode(bool mutatingLspWorkspace)
    {
        var markup =
@"class A
{
    /// <summary>
    /// Hello <c>A`1[B,C]</c>
    /// </summary>
    void {|caret:AMethod|}(int i)
    {
    }
}";
        var clientCapabilities = new LSP.ClientCapabilities
        {
            TextDocument = new LSP.TextDocumentClientCapabilities { Hover = new LSP.HoverSetting { ContentFormat = [LSP.MarkupKind.Markdown] } }
        };
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, clientCapabilities);
        var expectedLocation = testLspServer.GetLocations("caret").Single();
        var results = await RunGetHoverAsync(
            testLspServer,
            expectedLocation).ConfigureAwait(false);
        Assert.Equal(@"```csharp
void A.AMethod(int i)
```
  
Hello&nbsp;``A`1[B,C]``  
", results.Contents.Fourth.Value);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/vscode-csharp/issues/6577")]
    public async Task TestGetHoverAsync_UsesInlineCodeFencesInAwaitReturn(bool mutatingLspWorkspace)
    {
        var markup =
@"using System.Threading.Tasks;
class C
{
    public async Task<string> DoAsync()
    {
        return {|caret:await|} DoAsync();
    }
}";
        var clientCapabilities = new LSP.ClientCapabilities
        {
            TextDocument = new LSP.TextDocumentClientCapabilities { Hover = new LSP.HoverSetting { ContentFormat = [LSP.MarkupKind.Markdown] } }
        };
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, clientCapabilities);
        var expectedLocation = testLspServer.GetLocations("caret").Single();
        var results = await RunGetHoverAsync(
            testLspServer,
            expectedLocation).ConfigureAwait(false);
        Assert.Equal($@"{string.Format(FeaturesResources.Awaited_task_returns_0, "`class System.String`")}  
", results.Contents.Fourth.Value);
    }

    [Theory, CombinatorialData]
    public async Task TestGetHoverAsync_UsesNonBreakingSpaceForSupportedPlatforms(bool mutatingLspWorkspace)
    {
        var source = """
            using System;
            class WithConstant
            {
            #if NET472
                public const string Target = "Target in net472";
            #endif
            }
            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine(WithConstant.{|caret:Target|});
                }
            }
            """;

        var workspaceXml =
            $"""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="Net472" PreprocessorSymbols="NET472">
                    <Document FilePath="C:\C.cs"><![CDATA[${source}]]></Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="NetCoreApp3" PreprocessorSymbols="NETCOREAPP3.1">
                    <Document IsLinkFile="true" LinkFilePath="C:\C.cs" LinkAssemblyName="Net472"></Document>
                </Project>
            </Workspace>
            """;
        var clientCapabilities = new LSP.ClientCapabilities
        {
            TextDocument = new LSP.TextDocumentClientCapabilities { Hover = new LSP.HoverSetting { ContentFormat = [LSP.MarkupKind.Markdown] } }
        };
        await using var testLspServer = await CreateXmlTestLspServerAsync(workspaceXml, mutatingLspWorkspace, initializationOptions: new InitializationOptions { ClientCapabilities = clientCapabilities });
        var location = testLspServer.GetLocations("caret").Single();

        var project = testLspServer.GetCurrentSolution().Projects.Single(p => p.AssemblyName == "Net472");
        var result = await RunGetHoverAsync(testLspServer, location, project.Id);

        AssertEx.NotNull(result);
        Assert.Equal($"""
            ```csharp
            ({FeaturesResources.constant}) string WithConstant.Target = "Target in net472"
            ```
              
              
            &nbsp;&nbsp;&nbsp;&nbsp;{string.Format(FeaturesResources._0_1, "Net472", FeaturesResources.Available).Replace("-", "\\-")}  
            &nbsp;&nbsp;&nbsp;&nbsp;{string.Format(FeaturesResources._0_1, "NetCoreApp3", FeaturesResources.Not_Available).Replace("-", "\\-")}  
              
            {FeaturesResources.You_can_use_the_navigation_bar_to_switch_contexts.Replace(".", "\\.")}  
            
            """, result.Contents.Fourth.Value);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/vscode-csharp/issues/6577")]
    public async Task TestGetHoverAsync_EscapesAngleBracketsInGenerics(bool mutatingLspWorkspace)
    {
        var markup =
            """
            using System.Collections.Generic;
            using System.Collections.Immutable;
            using System.Threading.Tasks;
            class C
            {
                private async Task<IDictionary<string, ImmutableArray<int>>> GetData()
                {
                    {|caret:var|} d = await GetData();
                    return null;
                }
            }
            """;
        var clientCapabilities = new LSP.ClientCapabilities
        {
            TextDocument = new LSP.TextDocumentClientCapabilities { Hover = new LSP.HoverSetting { ContentFormat = [LSP.MarkupKind.Markdown] } }
        };
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, clientCapabilities);
        var expectedLocation = testLspServer.GetLocations("caret").Single();
        var results = await RunGetHoverAsync(
            testLspServer,
            expectedLocation).ConfigureAwait(false);
        Assert.Equal("""
            ```csharp
            interface System.Collections.Generic.IDictionary<TKey, TValue>
            ```
              
              
            TKey&nbsp;is&nbsp;string  
            TValue&nbsp;is&nbsp;ImmutableArray\<int\>  
            
            """, results.Contents.Fourth.Value);
    }

    private static async Task<LSP.Hover> RunGetHoverAsync(
        TestLspServer testLspServer,
        LSP.Location caret,
        ProjectId projectContext = null)
    {
        return await testLspServer.ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.Hover>(LSP.Methods.TextDocumentHoverName,
            CreateTextDocumentPositionParams(caret, projectContext), CancellationToken.None);
    }

    private static void VerifyVSContent(LSP.Hover hover, string expectedContent)
    {
        var vsHover = Assert.IsType<LSP.VSInternalHover>(hover);
        var containerElement = (ContainerElement)vsHover.RawContent;
        using var _ = ArrayBuilder<ClassifiedTextElement>.GetInstance(out var classifiedTextElements);
        GetClassifiedTextElements(containerElement, classifiedTextElements);
        Assert.False(classifiedTextElements.SelectMany(classifiedTextElements => classifiedTextElements.Runs).Any(run => run.NavigationAction != null));
        var content = string.Join("|", classifiedTextElements.Select(cte => string.Join(string.Empty, cte.Runs.Select(ctr => ctr.Text))));
        Assert.Equal(expectedContent, content);
    }

    private static void GetClassifiedTextElements(ContainerElement container, ArrayBuilder<ClassifiedTextElement> classifiedTextElements)
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
