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

    [Fact]
    public async Task TestNoReferenceAsync()
    {
        var markup =
@"class A
{
    void {|codeLens:M|}()
    {
    }
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Fact]
    public async Task TestOneReferenceAsync()
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
        await using var testLspServer = await CreateTestLspServerAsync(markup, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 1);
    }

    [Fact]
    public async Task TestMultipleReferencesAsync()
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
        await using var testLspServer = await CreateTestLspServerAsync(markup, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 2);
    }

    [Fact]
    public async Task TestMultipleReferencesCappedAsync()
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
        await using var testLspServer = await CreateTestLspServerAsync(markup, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 100, isCapped: true);
    }

    [Fact]
    public async Task TestClassDeclarationAsync()
    {
        var markup =
@"class {|codeLens:A|}
{
    void M(A a)
    {
    }
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 1);
    }

    [Fact]
    public async Task TestInterfaceDeclarationAsync()
    {
        var markup =
@"interface {|codeLens:A|}
{
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Fact]
    public async Task TestEnumDeclarationAsync()
    {
        var markup =
@"enum {|codeLens:A|}
{
    One
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Fact]
    public async Task TestPropertyDeclarationAsync()
    {
        var markup =
@"class A
{
    public int {|codeLens:I|} { get; set; }
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Fact]
    public async Task TestMethodDeclarationAsync()
    {
        var markup =
@"class A
{
    public int {|codeLens:M|}()
    {
    }
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Fact]
    public async Task TestStructDeclarationAsync()
    {
        var markup =
@"struct {|codeLens:A|}
{
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Fact]
    public async Task TestConstructorDeclarationAsync()
    {
        var markup =
@"class A
{
    public {|codeLens:A|}()
    {
    }
}";
        await using var testLspServer = await CreateTestLspServerAsync(markup, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Fact]
    public async Task TestRecordDeclarationAsync()
    {
        var markup =
@"record {|codeLens:A|}(int SomeInt)";
        await using var testLspServer = await CreateTestLspServerAsync(markup, CapabilitiesWithVSExtensions);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }
}
