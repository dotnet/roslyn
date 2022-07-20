// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.Rename)]
    public partial class RenameRewriterTests
    {
        #region CSharp

        [Fact]
        public async Task TestCSharpRenameMultipleSymbolsInSingleDocument()
        {
            var cancellationToken = CancellationToken.None;
            using var verifier = new Verifier(@"
                   <Workspace>
                       <Project Language=""C#"" CommonReferences=""true"">
                           <Document FilePath=""test.cs"">
public class {|Rename1:Apple|}
{

    public {|classRef:Apple|}()
    {
        {|propertyRef1:Orange|} = 10;
        {|methodRef:Goo|}({|propertyRef2:Orange|});
    }

    public void {|Rename2:Goo|}(int x)
    {
    }

    public int {|Rename3:Orange|}
    {
        get;
        set;
    }
}
            </Document>
       </Project>
    </Workspace>
");

            var renameOption = new SymbolRenameOptions();
            await verifier.RenameAndAnnotatedDocumentAsync(
                documentFilePath: "test.cs",
                new()
                {
                    { "Rename1", ("Apple2", renameOption) },
                    { "Rename2", ("Goo2", renameOption) },
                    { "Rename3", ("Orange2", renameOption) },
                }, cancellationToken);
            await verifier.VerifyAsync(documentFilePath: "test.cs", "Rename1", "Apple2", cancellationToken);
            await verifier.VerifyAsync(documentFilePath: "test.cs", "classRef", "Apple2", cancellationToken);

            await verifier.VerifyAsync(documentFilePath: "test.cs", "Rename2", "Goo2", cancellationToken);
            await verifier.VerifyAsync(documentFilePath: "test.cs", "methodRef", "Goo2", cancellationToken);

            await verifier.VerifyAsync(documentFilePath: "test.cs", "Rename3", "Orange2", cancellationToken);
            await verifier.VerifyAsync(documentFilePath: "test.cs", "propertyRef", "Orange2", cancellationToken);

            await verifier.VerifyDocumentAsync("test.cs",
                @"
public class Apple2
{

    public Apple2()
    {
        Orange2 = 10;
        Goo2(Orange2);
    }

    public void Goo2(int x)
    {
    }

    public int Orange2
    {
        get;
        set;
    }
}", cancellationToken);
        }

        [Fact]
        public async Task TestCSharpRenameInCommentsAndStrings()
        {
            var cancellationToken = CancellationToken.None;
            using var verifier = new Verifier(@"
                   <Workspace>
                       <Project Language=""C#"" CommonReferences=""true"">
                           <Document FilePath=""test1.cs"">
<![CDATA[
/// <summary>
/// <see cref=""Apple""/> Apple is not Lemon. Lemon is not banana
/// </summary>
/// <Lemon>
/// </Lemon>
public class {|Rename1:Apple|}
{
    // banana is not Apple
    public void {|Rename2:Lemon|}(int {|Rename3:banana|})
    {
        string Apple = ""Apple, Lemon and banana are fruit"";
        string Lemon = $""Apple, Lemon and {banana} are fruit"";
    }
}]]>
                           </Document>
                    </Project>
                </Workspace>
");

            var renameOption = new SymbolRenameOptions() { RenameInComments = true, RenameInStrings = true };

            await verifier.RenameAndAnnotatedDocumentAsync(
                documentFilePath: "test1.cs",
                new()
                {
                    { "Rename1", ("Apple2", renameOption) },
                    { "Rename2", ("Lemon2", renameOption) },
                    { "Rename3", ("banana2", renameOption) },
                }, cancellationToken);
            await verifier.VerifyDocumentAsync("test1.cs",
@"
/// <summary>
/// <see cref=""Apple2""/> Apple2 is not Lemon2. Lemon2 is not banana2
/// </summary>
/// <Lemon2>
/// </Lemon2>
public class Apple2
{
    // banana2 is not Apple2
    public void Lemon2(int banana2)
    {
        string Apple = ""Apple2, Lemon2 and banana2 are fruit"";
        string Lemon = $""Apple2, Lemon2 and {banana2} are fruit"";
    }
}", cancellationToken);
        }

        [Fact]
        public async Task TestCSharpRenameComplexification()
        {
            var cancellationToken = CancellationToken.None;
            using var verifier = new Verifier(@"
                   <Workspace>
                       <Project Language=""C#"" CommonReferences=""true"">
                           <Document FilePath=""test1.cs"">
<![CDATA[
public class {|Rename1:Apple|}
{
    public void Lemon(int banana)
    {
        {|Conflict:{|Rename2:banana2|}|} = 10;
    }
    public static int banana2
    {
        get; private set;
    }
}]]>
                           </Document>
                    </Project>
                </Workspace>
");

            var renameOption = new SymbolRenameOptions();
            await verifier.RenameAndAnnotatedDocumentAsync(
                documentFilePath: "test1.cs",
                new()
                {
                    { "Rename1", ("Apple2", renameOption) },
                    { "Rename2", ("banana", renameOption) },
                }, cancellationToken);
            await verifier.VerifyDocumentAsync("test1.cs",
@"
public class Apple2
{
    public void Lemon(int banana)
    {
        global::Apple2.banana = 10;
    }
    public static int banana
    {
        get; private set;
    }
}", cancellationToken);
        }

        [Fact]
        public async Task TestRenameFailed()
        {
            var cancellationToken = CancellationToken.None;
            using var verifier = new Verifier(@"
                   <Workspace>
                       <Project Language=""C#"" CommonReferences=""true"">
                           <Document FilePath=""test1.cs"">
interface IBar
{
    void {|Rename1:Hello|}();
}

class Bar : IBar
{
    public void {|Rename2:Hello|}();
}

                           </Document>
                    </Project>
                </Workspace>
");

            var renameOption = new SymbolRenameOptions();
            await Assert.ThrowsAsync<ArgumentException>(() =>
                verifier.RenameAndAnnotatedDocumentAsync(
                    documentFilePath: "test1.cs",
                    new()
                    {
                        { "Rename1", ("Hello1", renameOption) },
                        { "Rename2", ("Hello2", renameOption) },
                    }, cancellationToken));
        }

        [Fact]
        public async Task TestRenameStringFail()
        {
            var cancellationToken = CancellationToken.None;
            using var verifier = new Verifier(@"
                   <Workspace>
                       <Project Language=""C#"" CommonReferences=""true"">
                           <Document FilePath=""test1.cs"">
class {|Rename1:World|}
{
    public void Hello();
    {
        var x = ""Hello World"";
    }
}

class World_X
{
    public void {|Rename2:World|}()
    {
    }
}

                           </Document>
                    </Project>
                </Workspace>
");

            var renameOption = new SymbolRenameOptions() { RenameInStrings = true };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                verifier.RenameAndAnnotatedDocumentAsync(
                    documentFilePath: "test1.cs",
                    new()
                    {
                        { "Rename1", ("World1", renameOption) },
                        { "Rename2", ("Hello2", renameOption) },
                    }, cancellationToken));
        }

        #endregion

        #region Visual Basic

        [Fact]
        public async Task TestVBRenameMultipleSymbolsInSingleDocument()
        {
            var cancellationToken = CancellationToken.None;
            using var verifier = new Verifier(@"
                   <Workspace>
                       <Project Language=""Visual Basic"" CommonReferences=""true"">
                           <Document FilePath=""test.vb"">
Class {|Rename1:Apple|}
    Private _orange As Integer

    Sub New()
        Orange = 10
        {|methodRef:Goo|}({|propertyRef:Orange|})
    End Sub

    Public Sub {|Rename2:Goo|}(x As Integer)

    End Sub

    Public Property {|Rename3:Orange|} As Integer
        Get
            Return _orange
        End Get

        Set(value As Integer)
        End Set
    End Property
End Class
        </Document>
    </Project>
</Workspace>");

            var renameOption = new SymbolRenameOptions();
            await verifier.RenameAndAnnotatedDocumentAsync(
                documentFilePath: "test.vb",
                new()
                {
                    { "Rename1", ("Apple2", renameOption) },
                    { "Rename2", ("Goo2", renameOption) },
                    { "Rename3", ("Orange2", renameOption) },
                }, cancellationToken);
            await verifier.VerifyAsync(documentFilePath: "test.vb", "Rename1", "Apple2", cancellationToken);

            await verifier.VerifyAsync(documentFilePath: "test.vb", "Rename2", "Goo2", cancellationToken);
            await verifier.VerifyAsync(documentFilePath: "test.vb", "methodRef", "Goo2", cancellationToken);

            await verifier.VerifyAsync(documentFilePath: "test.vb", "Rename3", "Orange2", cancellationToken);
            await verifier.VerifyAsync(documentFilePath: "test.vb", "propertyRef", "Orange2", cancellationToken);

            await verifier.VerifyDocumentAsync("test.vb", @"
Class Apple2
    Private _orange As Integer

    Sub New()
        Orange2 = 10
        Goo2(Orange2)
    End Sub

    Public Sub Goo2(x As Integer)

    End Sub

    Public Property Orange2 As Integer
        Get
            Return _orange
        End Get

        Set(value As Integer)
        End Set
    End Property
End Class", cancellationToken);
        }

        [Fact]
        public async Task TestVBRenameInCommentsAndStrings()
        {
            var cancellationToken = CancellationToken.None;
            using var verifier = new Verifier(@"
                   <Workspace>
                       <Project Language=""Visual Basic"" CommonReferences=""true"">
                           <Document FilePath=""test1.vb"">
<![CDATA[
''' <summary>
''' <see cref=""Apple""/> Apple is not Lemon, Lemon is not banana.
''' </summary>
Class {|Rename1:Apple|}

    ' banana is not Apple
    Sub {|Rename2:Lemon|}({|Rename3:banana|} As Integer)
        Dim Apple As String = ""Apple, Lemon and banana are fruit""
        Dim Lemon As String = $""Apple, Lemon and {banana} are fruit""
    End Sub
End Class
]]>
                           </Document>
                    </Project>
                </Workspace>
");

            var renameOption = new SymbolRenameOptions() { RenameInComments = true, RenameInStrings = true };

            await verifier.RenameAndAnnotatedDocumentAsync(
                documentFilePath: "test1.vb",
                new()
                {
                    { "Rename1", ("Apple2", renameOption) },
                    { "Rename2", ("Lemon2", renameOption) },
                    { "Rename3", ("banana2", renameOption) },
                }, cancellationToken);
            await verifier.VerifyDocumentAsync("test1.vb",
@"
''' <summary>
''' <see cref=""Apple2""/> Apple2 is not Lemon2, Lemon2 is not banana2.
''' </summary>
Class Apple2

    ' banana2 is not Apple2
    Sub Lemon2(banana2 As Integer)
        Dim Apple As String = ""Apple2, Lemon2 and banana2 are fruit""
        Dim Lemon As String = $""Apple2, Lemon2 and {banana2} are fruit""
    End Sub
End Class
", cancellationToken);
        }

        [Fact]
        public async Task TestVBRenameComplexification()
        {
            var cancellationToken = CancellationToken.None;
            using var verifier = new Verifier(@"
                   <Workspace>
                       <Project Language=""Visual Basic"" CommonReferences=""true"">
                           <Document FilePath=""test1.vb"">
<![CDATA[
Class {|Rename1:Apple|}
    Sub Lemon(banana As Integer)
        {|Conflict:banana2|} = 10
    End Sub

    Public Shared Property {|Rename2:banana2|} As Integer
        Get
        End Get
        Set(value As Integer)

        End Set
    End Property
End Class
]]>
                           </Document>
                    </Project>
                </Workspace>
");

            var renameOption = new SymbolRenameOptions();
            await verifier.RenameAndAnnotatedDocumentAsync(
                documentFilePath: "test1.vb",
                new()
                {
                    { "Rename1", ("Apple2", renameOption) },
                    { "Rename2", ("banana", renameOption) },
                }, cancellationToken);
            await verifier.VerifyDocumentAsync("test1.vb",
@"
Class Apple2
    Sub Lemon(banana As Integer)
        Global.Apple2.banana = 10
    End Sub

    Public Shared Property banana As Integer
        Get
        End Get
        Set(value As Integer)

        End Set
    End Property
End Class
", cancellationToken);
        }

        #endregion
    }
}
