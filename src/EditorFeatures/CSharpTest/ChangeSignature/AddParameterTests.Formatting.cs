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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ChangeSignature;

[Trait(Traits.Feature, Traits.Features.ChangeSignature)]
public sealed partial class ChangeSignatureTests : AbstractChangeSignatureTests
{
    [Fact]
    public async Task AddParameter_Formatting_KeepCountsPerLine()
    {
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(5),
            new AddedParameterOrExistingIndex(4),
            new AddedParameterOrExistingIndex(3),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte"),
            new AddedParameterOrExistingIndex(2),
            new AddedParameterOrExistingIndex(1),
            new AddedParameterOrExistingIndex(0)};
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
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
            }
            """, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: """
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
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28156")]
    public async Task AddParameter_Formatting_KeepTrivia()
    {
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(1),
            new AddedParameterOrExistingIndex(2),
            new AddedParameterOrExistingIndex(3),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte"),
            new AddedParameterOrExistingIndex(4),
            new AddedParameterOrExistingIndex(5)};
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
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
            }
            """, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: """
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
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28156")]
    public async Task AddParameter_Formatting_KeepTrivia_WithArgumentNames()
    {
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(1),
            new AddedParameterOrExistingIndex(2),
            new AddedParameterOrExistingIndex(3),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte"),
            new AddedParameterOrExistingIndex(4),
            new AddedParameterOrExistingIndex(5)};
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
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
            }
            """, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: """
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
            }
            """);
    }

    [Fact]
    public async Task AddParameter_Formatting_Method()
    {
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(1),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte"),
            new AddedParameterOrExistingIndex(0)};
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            class C
            {
                void $$Method(int a, 
                    int b)
                {
                    Method(1,
                        2);
                }
            }
            """, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: """
            class C
            {
                void Method(int b,
                    byte bb, int a)
                {
                    Method(2,
                        34, 1);
                }
            }
            """);
    }

    [Fact]
    public async Task AddParameter_Formatting_Constructor()
    {
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(1),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte"),
            new AddedParameterOrExistingIndex(0)};
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            class SomeClass
            {
                $$SomeClass(int a,
                    int b)
                {
                    new SomeClass(1,
                        2);
                }
            }
            """, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: """
            class SomeClass
            {
                SomeClass(int b,
                    byte bb, int a)
                {
                    new SomeClass(2,
                        34, 1);
                }
            }
            """);
    }

    [Fact]
    public async Task AddParameter_Formatting_Indexer()
    {
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(1),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte"),
            new AddedParameterOrExistingIndex(0)};
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
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
            }
            """, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: """
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
            }
            """);
    }

    [Fact]
    public async Task AddParameter_Formatting_Delegate()
    {
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(1),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte"),
            new AddedParameterOrExistingIndex(0)};
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
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
            }
            """, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: """
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
            }
            """);
    }

    [Fact]
    public async Task AddParameter_Formatting_AnonymousMethod()
    {
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(1),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte"),
            new AddedParameterOrExistingIndex(0)};
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
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
            }
            """, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: """
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
            }
            """);
    }

    [Fact]
    public async Task AddParameter_Formatting_ConstructorInitializers()
    {
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(1),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte"),
            new AddedParameterOrExistingIndex(0)};
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
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
            }
            """, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: """
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
            }
            """);
    }

    [Fact]
    public async Task AddParameter_Formatting_Attribute()
    {
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(1),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte"),
            new AddedParameterOrExistingIndex(0)};
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            [Custom(1,
                2)]
            class CustomAttribute : System.Attribute
            {
                public $$CustomAttribute(int x, int y) { }
            }
            """, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: """
            [Custom(2,
                34, 1)]
            class CustomAttribute : System.Attribute
            {
                public CustomAttribute(int y, byte bb, int x) { }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28156")]
    public async Task AddParameter_Formatting_Attribute_KeepTrivia()
    {
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(1),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte") };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            [Custom(
                1, 2)]
            class CustomAttribute : System.Attribute
            {
                public $$CustomAttribute(int x, int y) { }
            }
            """, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: """
            [Custom(
                2, 34)]
            class CustomAttribute : System.Attribute
            {
                public CustomAttribute(int y, byte bb) { }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28156")]
    public async Task AddParameter_Formatting_Attribute_KeepTrivia_RemovingSecond()
    {
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(0),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte")};
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            [Custom(
                1, 2)]
            class CustomAttribute : System.Attribute
            {
                public $$CustomAttribute(int x, int y) { }
            }
            """, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: """
            [Custom(
                1, 34)]
            class CustomAttribute : System.Attribute
            {
                public CustomAttribute(int x, byte bb) { }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28156")]
    public async Task AddParameter_Formatting_Attribute_KeepTrivia_RemovingBothAddingNew()
    {
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte")};
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            [Custom(
                1, 2)]
            class CustomAttribute : System.Attribute
            {
                public $$CustomAttribute(int x, int y) { }
            }
            """, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: """
            [Custom(
                34)]
            class CustomAttribute : System.Attribute
            {
                public CustomAttribute(byte bb) { }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28156")]
    public async Task AddParameter_Formatting_Attribute_KeepTrivia_RemovingBeforeNewlineComma()
    {
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(1),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte"),
            new AddedParameterOrExistingIndex(2)};
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            [Custom(1
                , 2, 3)]
            class CustomAttribute : System.Attribute
            {
                public $$CustomAttribute(int x, int y, int z) { }
            }
            """, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: """
            [Custom(2, 34, 3)]
            class CustomAttribute : System.Attribute
            {
                public CustomAttribute(int y, byte bb, int z) { }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/946220")]
    public async Task AddParameter_Formatting_LambdaAsArgument()
    {
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(0),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "byte", "bb", CallSiteKind.Value, "34"), "byte")};
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            class C
            {
                void M(System.Action<int, int> f, int z$$)
                {
                    M((x, y) => System.Console.WriteLine(x + y), 5);
                }
            }
            """, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: """
            class C
            {
                void M(System.Action<int, int> f, byte bb)
                {
                    M((x, y) => System.Console.WriteLine(x + y), 34);
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46595")]
    public async Task AddParameter_Formatting_PreserveIndentBraces()
    {
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "a", CallSiteKind.Value, "12345"), "int")};
        await TestChangeSignatureViaCommandAsync(
            LanguageNames.CSharp, """
            public class C
                {
                public void M$$()
                    {
                    }
                }
            """, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: """
            public class C
                {
                public void M(int a)
                    {
                    }
                }
            """,
            options: Option(CSharpFormattingOptions2.IndentBraces, true));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46595")]
    public async Task AddParameter_Formatting_PreserveIndentBraces_Editorconfig()
    {
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "a", CallSiteKind.Value, "12345"), "int")};
        await TestChangeSignatureViaCommandAsync("XML", """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document FilePath="file.cs">public class C
                {
                public void M$$()
                    {
                    }
                }</Document>
                    <AnalyzerConfigDocument FilePath=".editorconfig">[*.cs]
            csharp_indent_braces = true
                    </AnalyzerConfigDocument>
                </Project>
            </Workspace>
            """, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: """
            public class C
                {
                public void M(int a)
                    {
                    }
                }
            """);
    }
}
