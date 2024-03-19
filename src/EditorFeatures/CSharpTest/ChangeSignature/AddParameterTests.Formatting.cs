// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.ChangeSignature;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ChangeSignature
{
    [Trait(Traits.Feature, Traits.Features.ChangeSignature)]
    public partial class ChangeSignatureTests : AbstractChangeSignatureTests
    {
        [Fact]
        public async Task AddParameter_Formatting_KeepCountsPerLine()
        {
            var markup = @"
class C
{
    void $$Method(int a, int b, int c,
        int d, int e,
        int f)
    {
        Method(1,
            2, 3,
            4, 5, 6);
    }
}";
            var updatedSignature = new[] {
                new AddedParameterOrExistingIndex(5),
                new AddedParameterOrExistingIndex(4),
                new AddedParameterOrExistingIndex(3),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(2),
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(0)};
            var expectedUpdatedCode = @"
class C
{
    void Method(int f, int e, int d,
        byte bb, int c,
        int b, int a)
    {
        Method(6,
            5, 4,
            34, 3, 2, 1);
    }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28156")]
        public async Task AddParameter_Formatting_KeepTrivia()
        {
            var markup = @"
class C
{
    void $$Method(
        int a, int b, int c,
        int d, int e,
        int f)
    {
        Method(
            1, 2, 3,
            4, 5, 6);
    }
}";
            var updatedSignature = new[] {
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(2),
                new AddedParameterOrExistingIndex(3),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(4),
                new AddedParameterOrExistingIndex(5)};
            var expectedUpdatedCode = @"
class C
{
    void Method(
        int b, int c, int d,
        byte bb, int e,
        int f)
    {
        Method(
            2, 3, 4,
            34, 5, 6);
    }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28156")]
        public async Task AddParameter_Formatting_KeepTrivia_WithArgumentNames()
        {
            var markup = @"
class C
{
    void $$Method(
        int a, int b, int c,
        int d, int e,
        int f)
    {
        Method(
            a: 1, b: 2, c: 3,
            d: 4, e: 5, f: 6);
    }
}";
            var updatedSignature = new[] {
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(2),
                new AddedParameterOrExistingIndex(3),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(4),
                new AddedParameterOrExistingIndex(5)};
            var expectedUpdatedCode = @"
class C
{
    void Method(
        int b, int c, int d,
        byte bb, int e,
        int f)
    {
        Method(
            b: 2, c: 3, d: 4,
            bb: 34, e: 5, f: 6);
    }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [Fact]
        public async Task AddParameter_Formatting_Method()
        {
            var markup = @"
class C
{
    void $$Method(int a, 
        int b)
    {
        Method(1,
            2);
    }
}";
            var updatedSignature = new[] {
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(0)};
            var expectedUpdatedCode = @"
class C
{
    void Method(int b,
        byte bb, int a)
    {
        Method(2,
            34, 1);
    }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [Fact]
        public async Task AddParameter_Formatting_Constructor()
        {
            var markup = @"
class SomeClass
{
    $$SomeClass(int a,
        int b)
    {
        new SomeClass(1,
            2);
    }
}";
            var updatedSignature = new[] {
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(0)};
            var expectedUpdatedCode = @"
class SomeClass
{
    SomeClass(int b,
        byte bb, int a)
    {
        new SomeClass(2,
            34, 1);
    }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [Fact]
        public async Task AddParameter_Formatting_Indexer()
        {
            var markup = @"
class SomeClass
{
    public int $$this[int a,
        int b]
    {
        get
        {
            return new SomeClass()[1,
                2];
        }
    }
}";
            var updatedSignature = new[] {
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(0)};
            var expectedUpdatedCode = @"
class SomeClass
{
    public int this[int b,
        byte bb, int a]
    {
        get
        {
            return new SomeClass()[2,
                34, 1];
        }
    }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [Fact]
        public async Task AddParameter_Formatting_Delegate()
        {
            var markup = @"
class SomeClass
{
    delegate void $$MyDelegate(int a,
        int b);

    void M(int a,
        int b)
    {
        var myDel = new MyDelegate(M);
        myDel(1,
            2);
    }
}";
            var updatedSignature = new[] {
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(0)};
            var expectedUpdatedCode = @"
class SomeClass
{
    delegate void MyDelegate(int b,
        byte bb, int a);

    void M(int b,
        byte bb, int a)
    {
        var myDel = new MyDelegate(M);
        myDel(2,
            34, 1);
    }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [Fact]
        public async Task AddParameter_Formatting_AnonymousMethod()
        {
            var markup = @"
class SomeClass
{
    delegate void $$MyDelegate(int a,
        int b);

    void M()
    {
        MyDelegate myDel = delegate (int x,
            int y)
        {
            // Nothing
        };
    }
}";
            var updatedSignature = new[] {
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(0)};
            var expectedUpdatedCode = @"
class SomeClass
{
    delegate void MyDelegate(int b,
        byte bb, int a);

    void M()
    {
        MyDelegate myDel = delegate (int y,
            byte bb, int x)
        {
            // Nothing
        };
    }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [Fact]
        public async Task AddParameter_Formatting_ConstructorInitializers()
        {
            var markup = @"
class B
{
    public $$B(int x, int y) { }
    public B() : this(1,
        2)
    { }
}

class D : B
{
    public D() : base(1,
        2)
    { }
}";
            var updatedSignature = new[] {
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(0)};
            var expectedUpdatedCode = @"
class B
{
    public B(int y, byte bb, int x) { }
    public B() : this(2,
        34, 1)
    { }
}

class D : B
{
    public D() : base(2,
        34, 1)
    { }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [Fact]
        public async Task AddParameter_Formatting_Attribute()
        {
            var markup = @"
[Custom(1,
    2)]
class CustomAttribute : System.Attribute
{
    public $$CustomAttribute(int x, int y) { }
}";
            var updatedSignature = new[] {
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(0)};
            var expectedUpdatedCode = @"
[Custom(2,
    34, 1)]
class CustomAttribute : System.Attribute
{
    public CustomAttribute(int y, byte bb, int x) { }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28156")]
        public async Task AddParameter_Formatting_Attribute_KeepTrivia()
        {
            var markup = @"
[Custom(
    1, 2)]
class CustomAttribute : System.Attribute
{
    public $$CustomAttribute(int x, int y) { }
}";
            var updatedSignature = new[] {
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte") };
            var expectedUpdatedCode = @"
[Custom(
    2, 34)]
class CustomAttribute : System.Attribute
{
    public CustomAttribute(int y, byte bb) { }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28156")]
        public async Task AddParameter_Formatting_Attribute_KeepTrivia_RemovingSecond()
        {
            var markup = @"
[Custom(
    1, 2)]
class CustomAttribute : System.Attribute
{
    public $$CustomAttribute(int x, int y) { }
}";
            var updatedSignature = new[] {
                new AddedParameterOrExistingIndex(0),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte")};
            var expectedUpdatedCode = @"
[Custom(
    1, 34)]
class CustomAttribute : System.Attribute
{
    public CustomAttribute(int x, byte bb) { }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28156")]
        public async Task AddParameter_Formatting_Attribute_KeepTrivia_RemovingBothAddingNew()
        {
            var markup = @"
[Custom(
    1, 2)]
class CustomAttribute : System.Attribute
{
    public $$CustomAttribute(int x, int y) { }
}";
            var updatedSignature = new[] {
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte")};
            var expectedUpdatedCode = @"
[Custom(
    34)]
class CustomAttribute : System.Attribute
{
    public CustomAttribute(byte bb) { }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28156")]
        public async Task AddParameter_Formatting_Attribute_KeepTrivia_RemovingBeforeNewlineComma()
        {
            var markup = @"
[Custom(1
    , 2, 3)]
class CustomAttribute : System.Attribute
{
    public $$CustomAttribute(int x, int y, int z) { }
}";
            var updatedSignature = new[] {
                new AddedParameterOrExistingIndex(1),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte"),
                new AddedParameterOrExistingIndex(2)};
            var expectedUpdatedCode = @"
[Custom(2, 34, 3)]
class CustomAttribute : System.Attribute
{
    public CustomAttribute(int y, byte bb, int z) { }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/946220")]
        public async Task AddParameter_Formatting_LambdaAsArgument()
        {
            var markup = @"class C
{
    void M(System.Action<int, int> f, int z$$)
    {
        M((x, y) => System.Console.WriteLine(x + y), 5);
    }
}";
            var updatedSignature = new[] {
                new AddedParameterOrExistingIndex(0),
                new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte")};
            var expectedUpdatedCode = @"class C
{
    void M(System.Action<int, int> f, byte bb)
    {
        M((x, y) => System.Console.WriteLine(x + y), 34);
    }
}";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46595")]
        public async Task AddParameter_Formatting_PreserveIndentBraces()
        {
            var markup =
@"public class C
    {
    public void M$$()
        {
        }
    }";
            var updatedSignature = new[] {
                new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "a", CallSiteKind.Value, "12345"), "int")};
            var expectedUpdatedCode =
@"public class C
    {
    public void M(int a)
        {
        }
    }";
            await TestChangeSignatureViaCommandAsync(
                LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode,
                options: Option(CSharpFormattingOptions2.IndentBraces, true));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46595")]
        public async Task AddParameter_Formatting_PreserveIndentBraces_Editorconfig()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.cs"">
public class C
    {
    public void M$$()
        {
        }
    }
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]
csharp_indent_braces = true
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";
            var updatedSignature = new[] {
                new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "a", CallSiteKind.Value, "12345"), "int")};
            var expectedUpdatedCode = @"
public class C
    {
    public void M(int a)
        {
        }
    }
        ";

            await TestChangeSignatureViaCommandAsync("XML", markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }
    }
}
