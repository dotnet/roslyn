// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    public async Task TestRecordDeclarationAsync(bool lspMutatingWorkspace)
    {
        var markup =
@"record {|codeLens:A|}(int SomeInt)";
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }
}
