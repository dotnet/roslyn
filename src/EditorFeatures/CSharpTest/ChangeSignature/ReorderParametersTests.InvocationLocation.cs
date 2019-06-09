// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ChangeSignature
{
    public partial class ChangeSignatureTests : AbstractChangeSignatureTests
    {
        #region Methods 

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderMethodParameters_InvokeBeforeMethodName()
        {
            var markup = @"
using System;
class MyClass
{
    public void $$Goo(int x, string y)
    {
    }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
using System;
class MyClass
{
    public void Goo(string y, int x)
    {
    }
}";

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation,
                expectedUpdatedInvocationDocumentCode: updatedCode, expectedSelectedIndex: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderMethodParameters_InvokeInParameterList()
        {
            var markup = @"
using System;
class MyClass
{
    public void Goo(int x, $$string y)
    {
    }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
using System;
class MyClass
{
    public void Goo(string y, int x)
    {
    }
}";

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation,
                expectedUpdatedInvocationDocumentCode: updatedCode, expectedSelectedIndex: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderMethodParameters_InvokeAfterParameterList()
        {
            var markup = @"
using System;
class MyClass
{
    public void Goo(int x, string y)$$
    {
    }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
using System;
class MyClass
{
    public void Goo(string y, int x)
    {
    }
}";

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderMethodParameters_InvokeBeforeMethodDeclaration()
        {
            var markup = @"
using System;
class MyClass
{
    $$public void Goo(int x, string y)
    {
    }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
using System;
class MyClass
{
    public void Goo(string y, int x)
    {
    }
}";

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderMethodParameters_InvokeOnMetadataReference_InIdentifier_ShouldFail()
        {
            var markup = @"
class C
{
    static void Main(string[] args)
    {
        ((System.IFormattable)null).ToSt$$ring(""test"", null);
    }
}";

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, expectedSuccess: false, expectedErrorText: FeaturesResources.The_member_is_defined_in_metadata);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderMethodParameters_InvokeOnMetadataReference_AtBeginningOfInvocation_ShouldFail()
        {
            var markup = @"
class C
{
    static void Main(string[] args)
    {
        $$((System.IFormattable)null).ToString(""test"", null);
    }
}";

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, expectedSuccess: false, expectedErrorText: FeaturesResources.The_member_is_defined_in_metadata);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderMethodParameters_InvokeOnMetadataReference_InArgumentsOfInvocation_ShouldFail()
        {
            var markup = @"
class C
{
    static void Main(string[] args)
    {
        ((System.IFormattable)null).ToString(""test"",$$ null);
    }
}";

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, expectedSuccess: false, expectedErrorText: FeaturesResources.The_member_is_defined_in_metadata);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderMethodParameters_InvokeOnMetadataReference_AfterInvocation_ShouldFail()
        {
            var markup = @"
class C
{
    string s = ((System.IFormattable)null).ToString(""test"", null)$$;
}";

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, expectedSuccess: false, expectedErrorText: FeaturesResources.You_can_only_change_the_signature_of_a_constructor_indexer_method_or_delegate);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderMethodParameters_InvokeInMethodBody_ViaCommand()
        {
            var markup = @"
using System;
class MyClass
{
    public void Goo(int x, string y)
    {
        $$
    }
}";

            await TestChangeSignatureViaCommandAsync(
                LanguageNames.CSharp, markup, expectedSuccess: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderMethodParameters_InvokeInMethodBody_ViaSmartTag()
        {
            var markup = @"
using System;
class MyClass
{
    public void Goo(int x, string y)
    {
        [||]
    }
}";

            await TestMissingAsync(markup);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderMethodParameters_InvokeOnReference_BeginningOfIdentifier()
        {
            var markup = @"
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
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
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
}";

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderMethodParameters_InvokeOnReference_ArgumentList()
        {
            var markup = @"
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
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
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
}";

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderMethodParameters_InvokeOnReference_NestedCalls1()
        {
            var markup = @"
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
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
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
}";

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderMethodParameters_InvokeOnReference_NestedCalls2()
        {
            var markup = @"
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
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
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
}";

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderMethodParameters_InvokeOnReference_NestedCalls3()
        {
            var markup = @"
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
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
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
}";

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderMethodParameters_InvokeOnReference_Attribute()
        {
            var markup = @"
using System;

[$$My(1, 2)]
class MyAttribute : Attribute
{
    public MyAttribute(int x, int y)
    {
    }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
using System;

[My(2, 1)]
class MyAttribute : Attribute
{
    public MyAttribute(int y, int x)
    {
    }
}";

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderMethodParameters_InvokeOnReference_OnlyHasCandidateSymbols()
        {
            var markup = @"
class Test
{
    void M(int x, string y) { }
    void M(int x, double y) { }
    void M2() { $$M(""s"", 1); }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
class Test
{
    void M(string y, int x) { }
    void M(int x, double y) { }
    void M2() { M(1, ""s""); }
}";

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderMethodParameters_InvokeOnReference_CallToOtherConstructor()
        {
            var markup = @"
class Program
{
    public Program(int x, int y) : this(1, 2, 3)$$
    {
    }

    public Program(int x, int y, int z)
    {
    }
}";
            var permutation = new[] { 2, 1, 0 };
            var updatedCode = @"
class Program
{
    public Program(int x, int y) : this(3, 2, 1)
    {
    }

    public Program(int z, int y, int x)
    {
    }
}";

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderMethodParameters_InvokeOnReference_CallToBaseConstructor()
        {
            var markup = @"
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
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
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
}";

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        #endregion

        #region Indexers

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderIndexerParameters_InvokeAtBeginningOfDeclaration()
        {
            var markup = @"
class Program
{
    $$int this[int x, string y]
    {
        get { return 5; }
        set { }
    }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
class Program
{
    int this[string y, int x]
    {
        get { return 5; }
        set { }
    }
}";

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation,
                expectedUpdatedInvocationDocumentCode: updatedCode, expectedSelectedIndex: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderIndexerParameters_InParameters()
        {
            var markup = @"
class Program
{
    int this[int x, $$string y]
    {
        get { return 5; }
        set { }
    }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
class Program
{
    int this[string y, int x]
    {
        get { return 5; }
        set { }
    }
}";

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation,
                expectedUpdatedInvocationDocumentCode: updatedCode, expectedSelectedIndex: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderIndexerParameters_InvokeAtEndOfDeclaration()
        {
            var markup = @"
class Program
{
    int this[int x, string y]$$
    {
        get { return 5; }
        set { }
    }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
class Program
{
    int this[string y, int x]
    {
        get { return 5; }
        set { }
    }
}";

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation,
                expectedUpdatedInvocationDocumentCode: updatedCode, expectedSelectedIndex: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderIndexerParameters_InvokeInAccessor()
        {
            var markup = @"
class Program
{
    int this[int x, string y]
    {
        get { return $$5; }
        set { }
    }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
class Program
{
    int this[string y, int x]
    {
        get { return 5; }
        set { }
    }
}";

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderIndexerParameters_InvokeOnReference_BeforeTarget()
        {
            var markup = @"
class Program
{
    void M(Program p)
    {
        var t = $$p[5, ""test""];
    }

    int this[int x, string y]
    {
        get { return 5; }
        set { }
    }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
class Program
{
    void M(Program p)
    {
        var t = p[""test"", 5];
    }

    int this[string y, int x]
    {
        get { return 5; }
        set { }
    }
}";

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderIndexerParameters_InvokeOnReference_InArgumentList()
        {
            var markup = @"
class Program
{
    void M(Program p)
    {
        var t = p[5, ""test""$$];
    }

    int this[int x, string y]
    {
        get { return 5; }
        set { }
    }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
class Program
{
    void M(Program p)
    {
        var t = p[""test"", 5];
    }

    int this[string y, int x]
    {
        get { return 5; }
        set { }
    }
}";

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation,
                expectedUpdatedInvocationDocumentCode: updatedCode, expectedSelectedIndex: 0);
        }

        #endregion

        #region Delegates

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderDelegateParameters_ObjectCreation1()
        {
            var markup = @"
public class C
{
    void T()
    {
        var d = new $$D((x, y) => { });
    }

    public delegate void D(int x, int y);
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
public class C
{
    void T()
    {
        var d = new D((y, x) => { });
    }

    public delegate void D(int y, int x);
}";

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation,
                expectedUpdatedInvocationDocumentCode: updatedCode, expectedSelectedIndex: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderDelegateParameters_ObjectCreation2()
        {
            var markup = @"
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
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
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
}";

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        #endregion

        #region CodeRefactoring
        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderMethodParameters_CodeRefactoring_InvokeBeforeMethodName()
        {
            var markup = @"
using System;
class MyClass
{
    public void [||]Goo(int x, string y)
    {
    }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
using System;
class MyClass
{
    public void Goo(string y, int x)
    {
    }
}";
            await TestChangeSignatureViaCodeActionAsync(markup, expectedCodeAction: true, updatedSignature: permutation, expectedCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderMethodParameters_CodeRefactoring_NotInMethodBody()
        {
            var markup = @"
using System;
class MyClass
{
    public void Goo(int x, string y)
    {
        [||]
    }
}";
            await TestChangeSignatureViaCodeActionAsync(markup, expectedCodeAction: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderMethodParameters_CodeRefactoring_InLambda()
        {
            var markup = @"
class Program
{
    void M(int x)
    {
        System.Func<int, int, int> f = (a, b)[||] => { return a; };
    }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
class Program
{
    void M(int x)
    {
        System.Func<int, int, int> f = (b, a) => { return a; };
    }
}";
            await TestChangeSignatureViaCodeActionAsync(markup, expectedCodeAction: true, updatedSignature: permutation, expectedCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderMethodParameters_CodeRefactoring_NotInLambdaBody()
        {
            var markup = @"
class Program
{
    void M(int x)
    {
        System.Func<int, int, int> f = (a, b) => { [||]return a; };
    }
}";
            await TestChangeSignatureViaCodeActionAsync(markup, expectedCodeAction: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderMethodParameters_CodeRefactoring_AtCallSite_ViaCommand()
        {
            var markup = @"
class Program
{
    void M(int x, int y)
    {
        M($$5, 6);
    }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
class Program
{
    void M(int y, int x)
    {
        M(6, 5);
    }
}";
            await TestChangeSignatureViaCommandAsync(
                LanguageNames.CSharp, markup, updatedSignature: permutation,
                expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderMethodParameters_CodeRefactoring_AtCallSite_ViaCodeAction()
        {
            var markup = @"
class Program
{
    void M(int x, int y)
    {
        M([||]5, 6);
    }
}";
            await TestMissingAsync(markup);
        }

        #endregion
    }
}
