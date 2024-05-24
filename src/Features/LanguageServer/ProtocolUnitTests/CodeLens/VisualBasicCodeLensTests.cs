// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.CodeLens;

public class VisualBasicCodeLensTests : AbstractCodeLensTests
{
    public VisualBasicCodeLensTests(ITestOutputHelper? testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory, CombinatorialData]
    public async Task TestNoReferenceAsync(bool mutatingLspWorkspace)
    {
        var markup =
@"Class A
    Sub {|codeLens:M|}()
    End Sub
End Class";
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup, mutatingLspWorkspace);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    public async Task TestOneReferenceAsync(bool mutatingLspWorkspace)
    {
        var markup =
@"Class A
    Sub {|codeLens:M|}()
        M()
    End Sub
End Class";
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup, mutatingLspWorkspace);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 1);
    }

    [Theory, CombinatorialData]
    public async Task TestMultipleReferencesAsync(bool mutatingLspWorkspace)
    {
        var markup =
@"Class A
    Sub {|codeLens:M|}()
        M()
        M()
    End Sub
End Class";
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup, mutatingLspWorkspace);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 2);
    }

    [Theory, CombinatorialData]
    public async Task TestClassDeclarationAsync(bool mutatingLspWorkspace)
    {
        var markup =
@"Class {|codeLens:A|}
End Class";
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup, mutatingLspWorkspace);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    public async Task TestInterfaceDeclarationAsync(bool mutatingLspWorkspace)
    {
        var markup =
@"Interface {|codeLens:A|}
End Interface";
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup, mutatingLspWorkspace);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    public async Task TestEnumDeclarationAsync(bool mutatingLspWorkspace)
    {
        var markup =
@"Enum {|codeLens:A|}
    One
End Enum";
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup, mutatingLspWorkspace);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/69583")]
    public async Task TestEnumMemberDeclarationAsync(bool mutatingLspWorkspace)
    {
        var markup =
@"Enum A
    {|codeLens:One|}
End Enum";
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup, mutatingLspWorkspace);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/69583")]
    public async Task TestFieldDeclaration1Async(bool mutatingLspWorkspace)
    {
        var markup =
@"Class A
    Public {|codeLens:One|} As Integer
End Class";
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup, mutatingLspWorkspace);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/69583")]
    public async Task TestFieldDeclaration2Async(bool mutatingLspWorkspace)
    {
        var markup =
@"Class A
    Public {|codeLens:One|}, Two As Integer
End Class";
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup, mutatingLspWorkspace);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/69583")]
    public async Task TestFieldDeclaration3Async(bool mutatingLspWorkspace)
    {
        var markup =
@"Class A
    Public One, {|codeLens:Two|} As Integer
End Class";
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup, mutatingLspWorkspace);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/69583")]
    public async Task TestFieldDeclaration4Async(bool mutatingLspWorkspace)
    {
        var markup =
@"Class A
    Public {|codeLens:One|} As Integer, Two As Integer
End Class";
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup, mutatingLspWorkspace);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/69583")]
    public async Task TestFieldDeclaration5Async(bool mutatingLspWorkspace)
    {
        var markup =
@"Class A
    Public One As Integer, {|codeLens:Two|} As Integer
End Class";
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup, mutatingLspWorkspace);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/69583")]
    public async Task TestEventDeclarationAsync(bool mutatingLspWorkspace)
    {
        var markup =
@"Class A
    Public Event {|codeLens:One|} As System.EventHandler
End Class";
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup, mutatingLspWorkspace);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    public async Task TestPropertyDeclarationAsync(bool mutatingLspWorkspace)
    {
        var markup =
@"Class A
    Property {|codeLens:SomeString|} As String
End Class";
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup, mutatingLspWorkspace);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    public async Task TestSubDeclarationAsync(bool mutatingLspWorkspace)
    {
        var markup =
@"Class A
    Sub {|codeLens:M|}()
    End Sub
End Class";
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup, mutatingLspWorkspace);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/69583")]
    public async Task TestDelegateSubDeclarationAsync(bool mutatingLspWorkspace)
    {
        var markup =
@"Class A
    Delegate Sub {|codeLens:M|}()
End Class";
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup, mutatingLspWorkspace);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    public async Task TestFunctionDeclarationAsync(bool mutatingLspWorkspace)
    {
        var markup =
@"Class A
    Function {|codeLens:M|}()
    End Function
End Class";
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup, mutatingLspWorkspace);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/69583")]
    public async Task TestDelegateFunctionDeclarationAsync(bool mutatingLspWorkspace)
    {
        var markup =
@"Class A
    Delegate Function {|codeLens:M|}() As Integer
End Class";
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup, mutatingLspWorkspace);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    public async Task TestStructDeclarationAsync(bool mutatingLspWorkspace)
    {
        var markup =
@"Structure {|codeLens:A|}
End Structure";
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup, mutatingLspWorkspace);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    public async Task TestSubNewDeclarationAsync(bool mutatingLspWorkspace)
    {
        var markup =
@"Class A
    Sub {|codeLens:New|}()
    End Sub
End Class";
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup, mutatingLspWorkspace);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/69583")]
    public async Task TestDestructorDeclarationAsync(bool mutatingLspWorkspace)
    {
        var markup =
@"Class A
    Protected Overrides Sub {|codeLens:Finalize|}()
    End Sub
End Class";
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup, mutatingLspWorkspace);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }

    [Theory, CombinatorialData]
    public async Task TestModuleDeclarationAsync(bool mutatingLspWorkspace)
    {
        var markup =
@"Module {|codeLens:A|}
End Module";
        await using var testLspServer = await CreateVisualBasicTestLspServerAsync(markup, mutatingLspWorkspace);
        await VerifyCodeLensAsync(testLspServer, expectedNumberOfReferences: 0);
    }
}
