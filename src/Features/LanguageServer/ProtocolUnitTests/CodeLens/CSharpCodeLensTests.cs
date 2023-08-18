// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeLens;
using Newtonsoft.Json;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.CodeLens;

public class CSharpCodeLensTests : AbstractCodeLensTests
{
    public CSharpCodeLensTests(ITestOutputHelper? testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory, CombinatorialData]
    public async Task TestNoReferenceAsync(bool mutatingLspWorkspace)
    {
        var markup =
@"class A
{
    void {|codeLens:M|}()
    {
    }
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    public async Task TestOneReferenceAsync(bool mutatingLspWorkspace)
    {
        var markup =
@"class A
{
    void {|codeLens:M|}()
    {
    }

    void UseM()
    {
        M();
    }
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 1);
    }

    [Theory, CombinatorialData]
    public async Task TestMultipleReferencesAsync(bool mutatingLspWorkspace)
    {
        var markup =
@"class A
{
    void {|codeLens:M|}()
    {
    }

    void UseM()
    {
        M();
        M();
    }
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 2);
    }

    [Theory, CombinatorialData]
    public async Task TestMultipleReferencesCappedAsync(bool lspMutatingWorkspace)
    {
        var markup =
@"class A
{
    void {|codeLens:M|}()
    {
    }

    void UseM()
    {
        M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();
        M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();
        M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();
        M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();
        M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();M();
    }
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 100, isCapped: true);
    }

    [Theory, CombinatorialData]
    public async Task TestClassDeclarationAsync(bool lspMutatingWorkspace)
    {
        var markup =
@"class {|codeLens:A|}
{
    void M(A a)
    {
    }
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 1);
    }

    [Theory, CombinatorialData]
    public async Task TestInterfaceDeclarationAsync(bool lspMutatingWorkspace)
    {
        var markup =
@"interface {|codeLens:A|}
{
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    public async Task TestEnumDeclarationAsync(bool lspMutatingWorkspace)
    {
        var markup =
@"enum {|codeLens:A|}
{
    One
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/69583")]
    public async Task TestEnumMemberDeclarationAsync(bool lspMutatingWorkspace)
    {
        var markup =
@"enum A
{
    {|codeLens:One|}
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    public async Task TestPropertyDeclarationAsync(bool lspMutatingWorkspace)
    {
        var markup =
@"class A
{
    public int {|codeLens:I|} { get; set; }
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/69583")]
    public async Task TestEventDeclarationAsync(bool lspMutatingWorkspace)
    {
        var markup =
@"class A
{
    public event System.EventHandler {|codeLens:I|} { add { } remove { } }
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/69583")]
    public async Task TestEventFieldDeclaration1Async(bool lspMutatingWorkspace)
    {
        var markup =
@"class A
{
    public event System.EventHandler {|codeLens:I|};
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/69583")]
    public async Task TestEventFieldDeclaration2Async(bool lspMutatingWorkspace)
    {
        var markup =
@"class A
{
    public event System.EventHandler {|codeLens:I|}, I2;
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/69583")]
    public async Task TestEventFieldDeclaration3Async(bool lspMutatingWorkspace)
    {
        var markup =
@"class A
{
    public event System.EventHandler I, {|codeLens:I2|};
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/69583")]
    public async Task TestFieldDeclaration1Async(bool lspMutatingWorkspace)
    {
        var markup =
@"class A
{
    public int {|codeLens:I|};
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/69583")]
    public async Task TestFieldDeclaration2Async(bool lspMutatingWorkspace)
    {
        var markup =
@"class A
{
    public int {|codeLens:I|}, I2;
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/69583")]
    public async Task TestFieldDeclaration3Async(bool lspMutatingWorkspace)
    {
        var markup =
@"class A
{
    public int I, {|codeLens:I2|};
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/69583")]
    public async Task TestConstantDeclaration1Async(bool lspMutatingWorkspace)
    {
        var markup =
@"class A
{
    public const int {|codeLens:I|} = 0;
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/69583")]
    public async Task TestConstantDeclaration2Async(bool lspMutatingWorkspace)
    {
        var markup =
@"class A
{
    public const int {|codeLens:I|} = 0, I2 = 0;
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/69583")]
    public async Task TestConstantDeclaration3Async(bool lspMutatingWorkspace)
    {
        var markup =
@"class A
{
    public const int I = 0, {|codeLens:I2|} = 0;
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    public async Task TestMethodDeclarationAsync(bool lspMutatingWorkspace)
    {
        var markup =
@"class A
{
    public int {|codeLens:M|}()
    {
    }
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    public async Task TestStructDeclarationAsync(bool lspMutatingWorkspace)
    {
        var markup =
@"struct {|codeLens:A|}
{
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/69583")]
    public async Task TestDelegateDeclarationAsync(bool lspMutatingWorkspace)
    {
        var markup =
@"delegate void {|codeLens:A|}();";
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    public async Task TestConstructorDeclarationAsync(bool lspMutatingWorkspace)
    {
        var markup =
@"class A
{
    public {|codeLens:A|}()
    {
    }
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/69583")]
    public async Task TestDestructorDeclarationAsync(bool lspMutatingWorkspace)
    {
        var markup =
@"class A
{
    ~{|codeLens:A|}()
    {
    }
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    public async Task TestRecordDeclarationAsync(bool lspMutatingWorkspace)
    {
        var markup =
@"record {|codeLens:A|}(int SomeInt)";
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    public async Task TestDoesNotCrashWhenSyntaxVersionsMismatch(bool mutatingLspWorkspace)
    {
        var markup =
@"class A
{
    void {|codeLens:M|}()
    {
    }

    void UseM()
    {
        M();
    }
}";

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var documentUri = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single().GetURI();
        var codeLensParamsDoc1 = new LSP.CodeLensParams
        {
            TextDocument = CreateTextDocumentIdentifier(documentUri)
        };

        var actualCodeLenses = await testLspServer.ExecuteRequestAsync<LSP.CodeLensParams, LSP.CodeLens[]?>(LSP.Methods.TextDocumentCodeLensName, codeLensParamsDoc1, CancellationToken.None);
        var firstCodeLens = actualCodeLenses.First();
        var data = JsonConvert.DeserializeObject<CodeLensResolveData>(firstCodeLens.Data!.ToString());
        AssertEx.NotNull(data);

        // Update the document so the syntax version changes
        await testLspServer.OpenDocumentAsync(documentUri);
        await testLspServer.InsertTextAsync(documentUri, (0, 0, "A"));

        // Assert that we don't crash when sending an old request to a new document
        var firstDocumentResult2 = await testLspServer.ExecuteRequestAsync<LSP.CodeLens, LSP.CodeLens>(LSP.Methods.CodeLensResolveName, firstCodeLens, CancellationToken.None);
        Assert.NotNull(firstDocumentResult2?.Command?.Title);
    }

    [Theory, CombinatorialData]
    public async Task TestNoCodeLensWhenReferencesDisabledAsync(bool lspMutatingWorkspace)
    {
        var markup =
@"class {|codeLens:A|}
{
    void M(A a)
    {
    }
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace, new InitializationOptions
        {
            ClientCapabilities = CapabilitiesWithVSExtensions,
            OptionUpdater = (globalOptions) =>
            {
                globalOptions.SetGlobalOption(LspOptionsStorage.LspEnableReferencesCodeLens, LanguageNames.CSharp, false);
            }
        });
        var actualCodeLenses = await GetCodeLensAsync(testLspServer);
        Assert.Empty(actualCodeLenses);
    }

    [Theory, CombinatorialData]
    public async Task TestHasTestsCommandAsync(bool mutatingLspWorkspace)
    {
        var markup =
@"using System;
namespace Xunit
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class FactAttribute : Attribute { }
}
namespace Test
{
    using Xunit;
    class A
    {
        [Fact]
        public void {|codeLens:M|}()
        {
        }
    }
}
";
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, new InitializationOptions
        {
            ClientCapabilities = CapabilitiesWithVSExtensions,
            OptionUpdater = (globalOptions) =>
            {
                globalOptions.SetGlobalOption(LspOptionsStorage.LspUsingDevkitFeatures, false);
            }
        });
        await VerifyTestCodeLensAsync(testLspServer, FeaturesResources.Run_Test, FeaturesResources.Debug_Test);
    }

    [Theory, CombinatorialData]
    public async Task TestHasAllTestsCommandAsync(bool mutatingLspWorkspace)
    {
        var markup =
@"using System;
namespace Xunit
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class FactAttribute : Attribute { }
}
namespace Test
{
    using Xunit;
    class {|codeLens:A|}
    {
        [Fact]
        public void M()
        {
        }
    }
}
";
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, new InitializationOptions
        {
            ClientCapabilities = CapabilitiesWithVSExtensions,
            OptionUpdater = (globalOptions) =>
            {
                globalOptions.SetGlobalOption(LspOptionsStorage.LspUsingDevkitFeatures, false);
            }
        });
        await VerifyTestCodeLensAsync(testLspServer, FeaturesResources.Run_All_Tests, FeaturesResources.Debug_All_Tests);
    }

    [Theory, CombinatorialData]
    public async Task TestDoesNotHaveTestCommandWhenInDevkitAsync(bool mutatingLspWorkspace)
    {
        var markup =
@"using System;
namespace Xunit
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class FactAttribute : Attribute { }
}
namespace Test
{
    using Xunit;
    class A
    {
        [Fact]
        public void {|codeLens:M|}()
        {
        }
    }
}
";
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, new InitializationOptions
        {
            ClientCapabilities = CapabilitiesWithVSExtensions,
            OptionUpdater = (globalOptions) =>
            {
                globalOptions.SetGlobalOption(LspOptionsStorage.LspUsingDevkitFeatures, true);
            }
        });
        await VerifyTestCodeLensMissingAsync(testLspServer);
    }

    [Theory, CombinatorialData]
    public async Task TestDoesNotHaveTestCommandWhenDisabledAsync(bool mutatingLspWorkspace)
    {
        var markup =
@"using System;
namespace Xunit
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class FactAttribute : Attribute { }
}
namespace Test
{
    using Xunit;
    class A
    {
        [Fact]
        public void {|codeLens:M|}()
        {
        }
    }
}
";
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, new InitializationOptions
        {
            ClientCapabilities = CapabilitiesWithVSExtensions,
            OptionUpdater = (globalOptions) =>
            {
                globalOptions.SetGlobalOption(LspOptionsStorage.LspEnableTestsCodeLens, LanguageNames.CSharp, false);
            }
        });
        await VerifyTestCodeLensMissingAsync(testLspServer);
    }
}
