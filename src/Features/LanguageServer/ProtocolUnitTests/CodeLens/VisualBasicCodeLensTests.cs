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

public class VisualBasicCodeLensTests : AbstractCodeLensTests
{
    public VisualBasicCodeLensTests(ITestOutputHelper? testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task TestNoReferenceAsync()
    {
        var markup =
@"Class A
    Sub {|codeLens:M|}()
    End Sub
End Class";
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Fact]
    public async Task TestOneReferenceAsync()
    {
        var markup =
@"Class A
    Sub {|codeLens:M|}()
        M()
    End Sub
End Class";
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 1);
    }

    [Fact]
    public async Task TestMultipleReferencesAsync()
    {
        var markup =
@"Class A
    Sub {|codeLens:M|}()
        M()
        M()
    End Sub
End Class";
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 2);
    }

    [Fact]
    public async Task TestClassDeclarationAsync()
    {
        var markup =
@"Class {|codeLens:A|}
End Class";
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Fact]
    public async Task TestInterfaceDeclarationAsync()
    {
        var markup =
@"Interface {|codeLens:A|}
End Interface";
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Fact]
    public async Task TestEnumDeclarationAsync()
    {
        var markup =
@"Enum {|codeLens:A|}
    One
End Enum";
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Fact]
    public async Task TestPropertyDeclarationAsync()
    {
        var markup =
@"Class A
    Property {|codeLens:SomeString|} As String
End Class";
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Fact]
    public async Task TestSubDeclarationAsync()
    {
        var markup =
@"Class A
    Sub {|codeLens:M|}()
    End Sub
End Class";
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Fact]
    public async Task TestFunctionDeclarationAsync()
    {
        var markup =
@"Class A
    Function {|codeLens:M|}()
    End Function
End Class";
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Fact]
    public async Task TestStructDeclarationAsync()
    {
        var markup =
@"Structure {|codeLens:A|}
End Structure";
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Fact]
    public async Task TestSubNewDeclarationAsync()
    {
        var markup =
@"Class A
    Sub {|codeLens:New|}()
    End Sub
End Class";
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Fact]
    public async Task TestModuleDeclarationAsync()
    {
        var markup =
@"Module {|codeLens:A|}
End Module";
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }
}
