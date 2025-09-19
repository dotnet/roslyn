// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ChangeSignature;

[Trait(Traits.Feature, Traits.Features.ChangeSignature)]
public sealed partial class ChangeSignatureTests : AbstractChangeSignatureTests
{
    [Fact]
    public async Task ChangeSignature_Formatting_KeepCountsPerLine()
    {
        var updatedSignature = new[] { 5, 4, 3, 2, 1, 0 };
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
                    int c, int b,
                    int a)
                {
                    Method(6,
                        5, 4,
                        3, 2, 1);
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28156")]
    public async Task ChangeSignature_Formatting_KeepTrivia()
    {
        var updatedSignature = new[] { 1, 2, 3, 4, 5 };
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
                    int e, int f)
                {
                    Method(
                        2, 3, 4,
                        5, 6);
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28156")]
    public async Task ChangeSignature_Formatting_KeepTrivia_WithArgumentNames()
    {
        var updatedSignature = new[] { 1, 2, 3, 4, 5 };
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
                    int e, int f)
                {
                    Method(
                        b: 2, c: 3, d: 4,
                        e: 5, f: 6);
                }
            }
            """);
    }

    [Fact]
    public async Task ChangeSignature_Formatting_Method()
    {
        var updatedSignature = new[] { 1, 0 };
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
                    int a)
                {
                    Method(2,
                        1);
                }
            }
            """);
    }

    [Fact]
    public async Task ChangeSignature_Formatting_Constructor()
    {
        var updatedSignature = new[] { 1, 0 };
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
                    int a)
                {
                    new SomeClass(2,
                        1);
                }
            }
            """);
    }

    [Fact]
    public async Task ChangeSignature_Formatting_Indexer()
    {
        var updatedSignature = new[] { 1, 0 };
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
                    int a]
                {
                    get
                    {
                        return new SomeClass()[2,
                            1];
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task ChangeSignature_Formatting_Delegate()
    {
        var updatedSignature = new[] { 1, 0 };
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
                    int a);

                void M(int b,
                    int a)
                {
                    var myDel = new MyDelegate(M);
                    myDel(2,
                        1);
                }
            }
            """);
    }

    [Fact]
    public async Task ChangeSignature_Formatting_AnonymousMethod()
    {
        var updatedSignature = new[] { 1, 0 };
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
                    int a);

                void M()
                {
                    MyDelegate myDel = delegate (int y,
                        int x)
                    {
                        // Nothing
                    };
                }
            }
            """);
    }

    [Fact]
    public async Task ChangeSignature_Formatting_ConstructorInitializers()
    {
        var updatedSignature = new[] { 1, 0 };
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
                public B(int y, int x) { }
                public B() : this(2,
                    1)
                { }
            }

            class D : B
            {
                public D() : base(2,
                    1)
                { }
            }
            """);
    }

    [Fact]
    public async Task ChangeSignature_Formatting_Attribute()
    {
        var updatedSignature = new[] { 1, 0 };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            [Custom(1,
                2)]
            class CustomAttribute : System.Attribute
            {
                public $$CustomAttribute(int x, int y) { }
            }
            """, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: """
            [Custom(2,
                1)]
            class CustomAttribute : System.Attribute
            {
                public CustomAttribute(int y, int x) { }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28156")]
    public async Task ChangeSignature_Formatting_Attribute_KeepTrivia()
    {
        var updatedSignature = new[] { 1 };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            [Custom(
                1, 2)]
            class CustomAttribute : System.Attribute
            {
                public $$CustomAttribute(int x, int y) { }
            }
            """, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: """
            [Custom(
                2)]
            class CustomAttribute : System.Attribute
            {
                public CustomAttribute(int y) { }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28156")]
    public async Task ChangeSignature_Formatting_Attribute_KeepTrivia_RemovingSecond()
    {
        var updatedSignature = new[] { 0 };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            [Custom(
                1, 2)]
            class CustomAttribute : System.Attribute
            {
                public $$CustomAttribute(int x, int y) { }
            }
            """, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: """
            [Custom(
                1)]
            class CustomAttribute : System.Attribute
            {
                public CustomAttribute(int x) { }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28156")]
    public async Task ChangeSignature_Formatting_Attribute_KeepTrivia_RemovingBoth()
    {
        var updatedSignature = new int[] { };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            [Custom(
                1, 2)]
            class CustomAttribute : System.Attribute
            {
                public $$CustomAttribute(int x, int y) { }
            }
            """, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: """
            [Custom(
            )]
            class CustomAttribute : System.Attribute
            {
                public CustomAttribute() { }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28156")]
    public async Task ChangeSignature_Formatting_Attribute_KeepTrivia_RemovingBeforeNewlineComma()
    {
        var updatedSignature = new[] { 1, 2 };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            [Custom(1
                , 2, 3)]
            class CustomAttribute : System.Attribute
            {
                public $$CustomAttribute(int x, int y, int z) { }
            }
            """, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: """
            [Custom(2, 3)]
            class CustomAttribute : System.Attribute
            {
                public CustomAttribute(int y, int z) { }
            }
            """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/946220")]
    public async Task ChangeSignature_Formatting_LambdaAsArgument()
    {
        var updatedSignature = new[] { 0 };
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
                void M(System.Action<int, int> f)
                {
                    M((x, y) => System.Console.WriteLine(x + y));
                }
            }
            """);
    }
}
