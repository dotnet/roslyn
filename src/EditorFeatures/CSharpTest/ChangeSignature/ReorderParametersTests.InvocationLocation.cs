// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ChangeSignature;

[Trait(Traits.Feature, Traits.Features.ChangeSignature)]
public sealed partial class ChangeSignatureTests : AbstractChangeSignatureTests
{
    #region Methods 

    [Fact]
    public async Task ReorderMethodParameters_InvokeBeforeMethodName()
    {
        var permutation = new[] { 1, 0 };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            using System;
            class MyClass
            {
                public void $$Goo(int x, string y)
                {
                }
            }
            """, updatedSignature: permutation,
            expectedUpdatedInvocationDocumentCode: """
            using System;
            class MyClass
            {
                public void Goo(string y, int x)
                {
                }
            }
            """, expectedSelectedIndex: 0);
    }

    [Fact]
    public async Task ReorderMethodParameters_InvokeInParameterList()
    {
        var permutation = new[] { 1, 0 };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            using System;
            class MyClass
            {
                public void Goo(int x, $$string y)
                {
                }
            }
            """, updatedSignature: permutation,
            expectedUpdatedInvocationDocumentCode: """
            using System;
            class MyClass
            {
                public void Goo(string y, int x)
                {
                }
            }
            """, expectedSelectedIndex: 1);
    }

    [Fact]
    public async Task ReorderMethodParameters_InvokeAfterParameterList()
    {
        var permutation = new[] { 1, 0 };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            using System;
            class MyClass
            {
                public void Goo(int x, string y)$$
                {
                }
            }
            """, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: """
            using System;
            class MyClass
            {
                public void Goo(string y, int x)
                {
                }
            }
            """);
    }

    [Fact]
    public async Task ReorderMethodParameters_InvokeBeforeMethodDeclaration()
    {
        var permutation = new[] { 1, 0 };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            using System;
            class MyClass
            {
                $$public void Goo(int x, string y)
                {
                }
            }
            """, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: """
            using System;
            class MyClass
            {
                public void Goo(string y, int x)
                {
                }
            }
            """);
    }

    [Fact]
    public Task ReorderMethodParameters_InvokeOnMetadataReference_InIdentifier_ShouldFail()
        => TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            class C
            {
                static void Main(string[] args)
                {
                    ((System.IFormattable)null).ToSt$$ring("test", null);
                }
            }
            """, expectedSuccess: false, expectedFailureReason: ChangeSignatureFailureKind.DefinedInMetadata);

    [Fact]
    public Task ReorderMethodParameters_InvokeOnMetadataReference_AtBeginningOfInvocation_ShouldFail()
        => TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            class C
            {
                static void Main(string[] args)
                {
                    $$((System.IFormattable)null).ToString("test", null);
                }
            }
            """, expectedSuccess: false, expectedFailureReason: ChangeSignatureFailureKind.DefinedInMetadata);

    [Fact]
    public Task ReorderMethodParameters_InvokeOnMetadataReference_InArgumentsOfInvocation_ShouldFail()
        => TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            class C
            {
                static void Main(string[] args)
                {
                    ((System.IFormattable)null).ToString("test",$$ null);
                }
            }
            """, expectedSuccess: false, expectedFailureReason: ChangeSignatureFailureKind.DefinedInMetadata);

    [Fact]
    public Task ReorderMethodParameters_InvokeOnMetadataReference_AfterInvocation_ShouldFail()
        => TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            class C
            {
                string s = ((System.IFormattable)null).ToString("test", null)$$;
            }
            """, expectedSuccess: false, expectedFailureReason: ChangeSignatureFailureKind.IncorrectKind);

    [Fact]
    public Task ReorderMethodParameters_InvokeInMethodBody_ViaCommand()
        => TestChangeSignatureViaCommandAsync(
            LanguageNames.CSharp, """
            using System;
            class MyClass
            {
                public void Goo(int x, string y)
                {
                    $$
                }
            }
            """, expectedSuccess: false);

    [Fact]
    public Task ReorderMethodParameters_InvokeInMethodBody_ViaSmartTag()
        => TestMissingAsync("""
            using System;
            class MyClass
            {
                public void Goo(int x, string y)
                {
                    [||]
                }
            }
            """);

    [Fact]
    public async Task ReorderMethodParameters_InvokeOnReference_BeginningOfIdentifier()
    {
        var permutation = new[] { 1, 0 };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            using System;
            class MyClass
            {
                public void Goo(int x, string y)
                {
                    $$Bar(x, y);
                }

                public void Bar(int x, string y)
                {
                }
            }
            """, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: """
            using System;
            class MyClass
            {
                public void Goo(int x, string y)
                {
                    Bar(y, x);
                }

                public void Bar(string y, int x)
                {
                }
            }
            """);
    }

    [Fact]
    public async Task ReorderMethodParameters_InvokeOnReference_ArgumentList()
    {
        var permutation = new[] { 1, 0 };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            using System;
            class MyClass
            {
                public void Goo(int x, string y)
                {
                    $$Bar(x, y);
                }

                public void Bar(int x, string y)
                {
                }
            }
            """, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: """
            using System;
            class MyClass
            {
                public void Goo(int x, string y)
                {
                    Bar(y, x);
                }

                public void Bar(string y, int x)
                {
                }
            }
            """);
    }

    [Fact]
    public async Task ReorderMethodParameters_InvokeOnReference_NestedCalls1()
    {
        var permutation = new[] { 1, 0 };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            using System;
            class MyClass
            {
                public void Goo(int x, string y)
                {
                    Bar($$Baz(x, y), y);
                }

                public void Bar(int x, string y)
                {
                }

                public int Baz(int x, string y)
                {
                    return 1;
                }
            }
            """, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: """
            using System;
            class MyClass
            {
                public void Goo(int x, string y)
                {
                    Bar(Baz(y, x), y);
                }

                public void Bar(int x, string y)
                {
                }

                public int Baz(string y, int x)
                {
                    return 1;
                }
            }
            """);
    }

    [Fact]
    public async Task ReorderMethodParameters_InvokeOnReference_NestedCalls2()
    {
        var permutation = new[] { 1, 0 };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            using System;
            class MyClass
            {
                public void Goo(int x, string y)
                {
                    Bar$$(Baz(x, y), y);
                }

                public void Bar(int x, string y)
                {
                }

                public int Baz(int x, string y)
                {
                    return 1;
                }
            }
            """, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: """
            using System;
            class MyClass
            {
                public void Goo(int x, string y)
                {
                    Bar(y, Baz(x, y));
                }

                public void Bar(string y, int x)
                {
                }

                public int Baz(int x, string y)
                {
                    return 1;
                }
            }
            """);
    }

    [Fact]
    public async Task ReorderMethodParameters_InvokeOnReference_NestedCalls3()
    {
        var permutation = new[] { 1, 0 };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            using System;
            class MyClass
            {
                public void Goo(int x, string y)
                {
                    Bar(Baz(x, y), $$y);
                }

                public void Bar(int x, string y)
                {
                }

                public int Baz(int x, string y)
                {
                    return 1;
                }
            }
            """, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: """
            using System;
            class MyClass
            {
                public void Goo(int x, string y)
                {
                    Bar(y, Baz(x, y));
                }

                public void Bar(string y, int x)
                {
                }

                public int Baz(int x, string y)
                {
                    return 1;
                }
            }
            """);
    }

    [Fact]
    public async Task ReorderMethodParameters_InvokeOnReference_Attribute()
    {
        var permutation = new[] { 1, 0 };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            using System;

            [$$My(1, 2)]
            class MyAttribute : Attribute
            {
                public MyAttribute(int x, int y)
                {
                }
            }
            """, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: """
            using System;

            [My(2, 1)]
            class MyAttribute : Attribute
            {
                public MyAttribute(int y, int x)
                {
                }
            }
            """);
    }

    [Fact]
    public async Task ReorderMethodParameters_InvokeOnReference_OnlyHasCandidateSymbols()
    {
        var permutation = new[] { 1, 0 };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            class Test
            {
                void M(int x, string y) { }
                void M(int x, double y) { }
                void M2() { $$M("s", 1); }
            }
            """, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: """
            class Test
            {
                void M(string y, int x) { }
                void M(int x, double y) { }
                void M2() { M(1, "s"); }
            }
            """);
    }

    [Fact]
    public async Task ReorderMethodParameters_InvokeOnReference_CallToOtherConstructor()
    {
        var permutation = new[] { 2, 1, 0 };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            class Program
            {
                public Program(int x, int y) : this(1, 2, 3)$$
                {
                }

                public Program(int x, int y, int z)
                {
                }
            }
            """, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: """
            class Program
            {
                public Program(int x, int y) : this(3, 2, 1)
                {
                }

                public Program(int z, int y, int x)
                {
                }
            }
            """);
    }

    [Fact]
    public async Task ReorderMethodParameters_InvokeOnReference_CallToBaseConstructor()
    {
        var permutation = new[] { 1, 0 };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            class B
            {
                public B(int a, int b)
                {
                }
            }

            class D : B
            {
                public D(int x, int y) : base(1, 2)$$
                {
                }
            }
            """, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: """
            class B
            {
                public B(int b, int a)
                {
                }
            }

            class D : B
            {
                public D(int x, int y) : base(2, 1)
                {
                }
            }
            """);
    }

    #endregion

    #region Indexers

    [Fact]
    public async Task ReorderIndexerParameters_InvokeAtBeginningOfDeclaration()
    {
        var permutation = new[] { 1, 0 };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            class Program
            {
                $$int this[int x, string y]
                {
                    get { return 5; }
                    set { }
                }
            }
            """, updatedSignature: permutation,
            expectedUpdatedInvocationDocumentCode: """
            class Program
            {
                int this[string y, int x]
                {
                    get { return 5; }
                    set { }
                }
            }
            """, expectedSelectedIndex: 0);
    }

    [Fact]
    public async Task ReorderIndexerParameters_InParameters()
    {
        var permutation = new[] { 1, 0 };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            class Program
            {
                int this[int x, $$string y]
                {
                    get { return 5; }
                    set { }
                }
            }
            """, updatedSignature: permutation,
            expectedUpdatedInvocationDocumentCode: """
            class Program
            {
                int this[string y, int x]
                {
                    get { return 5; }
                    set { }
                }
            }
            """, expectedSelectedIndex: 1);
    }

    [Fact]
    public async Task ReorderIndexerParameters_InvokeAtEndOfDeclaration()
    {
        var permutation = new[] { 1, 0 };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            class Program
            {
                int this[int x, string y]$$
                {
                    get { return 5; }
                    set { }
                }
            }
            """, updatedSignature: permutation,
            expectedUpdatedInvocationDocumentCode: """
            class Program
            {
                int this[string y, int x]
                {
                    get { return 5; }
                    set { }
                }
            }
            """, expectedSelectedIndex: 0);
    }

    [Fact]
    public async Task ReorderIndexerParameters_InvokeInAccessor()
    {
        var permutation = new[] { 1, 0 };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            class Program
            {
                int this[int x, string y]
                {
                    get { return $$5; }
                    set { }
                }
            }
            """, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: """
            class Program
            {
                int this[string y, int x]
                {
                    get { return 5; }
                    set { }
                }
            }
            """);
    }

    [Fact]
    public async Task ReorderIndexerParameters_InvokeOnReference_BeforeTarget()
    {
        var permutation = new[] { 1, 0 };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            class Program
            {
                void M(Program p)
                {
                    var t = $$p[5, "test"];
                }

                int this[int x, string y]
                {
                    get { return 5; }
                    set { }
                }
            }
            """, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: """
            class Program
            {
                void M(Program p)
                {
                    var t = p["test", 5];
                }

                int this[string y, int x]
                {
                    get { return 5; }
                    set { }
                }
            }
            """);
    }

    [Fact]
    public async Task ReorderIndexerParameters_InvokeOnReference_InArgumentList()
    {
        var permutation = new[] { 1, 0 };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            class Program
            {
                void M(Program p)
                {
                    var t = p[5, "test"$$];
                }

                int this[int x, string y]
                {
                    get { return 5; }
                    set { }
                }
            }
            """, updatedSignature: permutation,
            expectedUpdatedInvocationDocumentCode: """
            class Program
            {
                void M(Program p)
                {
                    var t = p["test", 5];
                }

                int this[string y, int x]
                {
                    get { return 5; }
                    set { }
                }
            }
            """, expectedSelectedIndex: 0);
    }

    #endregion

    #region Delegates

    [Fact]
    public async Task ReorderDelegateParameters_ObjectCreation1()
    {
        var permutation = new[] { 1, 0 };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            public class C
            {
                void T()
                {
                    var d = new $$D((x, y) => { });
                }

                public delegate void D(int x, int y);
            }
            """, updatedSignature: permutation,
            expectedUpdatedInvocationDocumentCode: """
            public class C
            {
                void T()
                {
                    var d = new D((y, x) => { });
                }

                public delegate void D(int y, int x);
            }
            """, expectedSelectedIndex: 0);
    }

    [Fact]
    public async Task ReorderDelegateParameters_ObjectCreation2()
    {
        var permutation = new[] { 1, 0 };
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, """
            public class CD<T>
            {
                public delegate void D(T t, T u);
            }
            class Test
            {
                public void M()
                {
                    var dele = new CD<int>.$$D((int x, int y) => { });
                }
            }
            """, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: """
            public class CD<T>
            {
                public delegate void D(T u, T t);
            }
            class Test
            {
                public void M()
                {
                    var dele = new CD<int>.D((int y, int x) => { });
                }
            }
            """);
    }

    #endregion

    #region CodeRefactoring
    [Fact]
    public async Task ReorderMethodParameters_CodeRefactoring_InvokeBeforeMethodName()
    {
        var permutation = new[] { 1, 0 };
        await TestChangeSignatureViaCodeActionAsync("""
            using System;
            class MyClass
            {
                public void [||]Goo(int x, string y)
                {
                }
            }
            """, expectedCodeAction: true, updatedSignature: permutation, expectedCode: """
            using System;
            class MyClass
            {
                public void Goo(string y, int x)
                {
                }
            }
            """);
    }

    [Fact]
    public Task ReorderMethodParameters_CodeRefactoring_NotInMethodBody()
        => TestChangeSignatureViaCodeActionAsync("""
            using System;
            class MyClass
            {
                public void Goo(int x, string y)
                {
                    [||]
                }
            }
            """, expectedCodeAction: false);

    [Fact]
    public async Task ReorderMethodParameters_CodeRefactoring_InLambda()
    {
        var permutation = new[] { 1, 0 };
        await TestChangeSignatureViaCodeActionAsync("""
            class Program
            {
                void M(int x)
                {
                    System.Func<int, int, int> f = (a, b)[||] => { return a; };
                }
            }
            """, expectedCodeAction: true, updatedSignature: permutation, expectedCode: """
            class Program
            {
                void M(int x)
                {
                    System.Func<int, int, int> f = (b, a) => { return a; };
                }
            }
            """);
    }

    [Fact]
    public Task ReorderMethodParameters_CodeRefactoring_NotInLambdaBody()
        => TestChangeSignatureViaCodeActionAsync("""
            class Program
            {
                void M(int x)
                {
                    System.Func<int, int, int> f = (a, b) => { [||]return a; };
                }
            }
            """, expectedCodeAction: false);

    [Fact]
    public async Task ReorderMethodParameters_CodeRefactoring_AtCallSite_ViaCommand()
    {
        var permutation = new[] { 1, 0 };
        await TestChangeSignatureViaCommandAsync(
            LanguageNames.CSharp, """
            class Program
            {
                void M(int x, int y)
                {
                    M($$5, 6);
                }
            }
            """, updatedSignature: permutation,
            expectedUpdatedInvocationDocumentCode: """
            class Program
            {
                void M(int y, int x)
                {
                    M(6, 5);
                }
            }
            """);
    }

    [Fact]
    public Task ReorderMethodParameters_CodeRefactoring_AtCallSite_ViaCodeAction()
        => TestMissingAsync("""
            class Program
            {
                void M(int x, int y)
                {
                    M([||]5, 6);
                }
            }
            """);

    #endregion
}
