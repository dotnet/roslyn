// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ReorderParameters
{
    public partial class ReorderParametersTests
    {
        #region Methods 
        
        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderMethodParameters_InvokeBeforeMethodName()
        {
            var markup = @"
using System;
class MyClass
{
    public void $$Foo(int x, string y)
    {
    }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
using System;
class MyClass
{
    public void Foo(string y, int x)
    {
    }
}";

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderMethodParameters_InvokeInParameterList()
        {
            var markup = @"
using System;
class MyClass
{
    public void Foo(int x, $$string y)
    {
    }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
using System;
class MyClass
{
    public void Foo(string y, int x)
    {
    }
}";

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderMethodParameters_InvokeAfterParameterList()
        {
            var markup = @"
using System;
class MyClass
{
    public void Foo(int x, string y)$$
    {
    }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
using System;
class MyClass
{
    public void Foo(string y, int x)
    {
    }
}";

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderMethodParameters_InvokeBeforeMethodDeclaration()
        {
            var markup = @"
using System;
class MyClass
{
    $$public void Foo(int x, string y)
    {
    }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
using System;
class MyClass
{
    public void Foo(string y, int x)
    {
    }
}";

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderMethodParameters_InvokeOnMetadataReference_InIdentifier_ShouldFail()
        {
            var markup = @"
class C
{
    static void Main(string[] args)
    {
        ((System.IFormattable)null).ToSt$$ring(""test"", null);
    }
}";

            TestReorderParameters(LanguageNames.CSharp, markup, expectedSuccess: false, expectedErrorText: FeaturesResources.The_member_is_defined_in_metadata);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderMethodParameters_InvokeOnMetadataReference_AtBeginningOfInvocation_ShouldFail()
        {
            var markup = @"
class C
{
    static void Main(string[] args)
    {
        $$((System.IFormattable)null).ToString(""test"", null);
    }
}";

            TestReorderParameters(LanguageNames.CSharp, markup, expectedSuccess: false, expectedErrorText: FeaturesResources.The_member_is_defined_in_metadata);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderMethodParameters_InvokeOnMetadataReference_InArgumentsOfInvocation_ShouldFail()
        {
            var markup = @"
class C
{
    static void Main(string[] args)
    {
        ((System.IFormattable)null).ToString(""test"",$$ null);
    }
}";

            TestReorderParameters(LanguageNames.CSharp, markup, expectedSuccess: false, expectedErrorText: FeaturesResources.The_member_is_defined_in_metadata);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderMethodParameters_InvokeOnMetadataReference_AfterInvocation_ShouldFail()
        {
            var markup = @"
class C
{
    string s = ((System.IFormattable)null).ToString(""test"", null)$$;
}";

            TestReorderParameters(LanguageNames.CSharp, markup, expectedSuccess: false, expectedErrorText: FeaturesResources.You_can_only_change_the_signature_of_a_constructor_indexer_method_or_delegate);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderMethodParameters_InvokeInMethodBody()
        {
            var markup = @"
using System;
class MyClass
{
    public void Foo(int x, string y)
    {
        $$
    }
}";
            var permutation = new[] { 1, 0 };
            var updatedCode = @"
using System;
class MyClass
{
    public void Foo(string y, int x)
    {
        
    }
}";

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderMethodParameters_InvokeOnReference_BeginningOfIdentifier()
        {
            var markup = @"
using System;
class MyClass
{
    public void Foo(int x, string y)
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
    public void Foo(int x, string y)
    {
        Bar(y, x);
    }

    public void Bar(string y, int x)
    {
    }
}";

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderMethodParameters_InvokeOnReference_ArgumentList()
        {
            var markup = @"
using System;
class MyClass
{
    public void Foo(int x, string y)
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
    public void Foo(int x, string y)
    {
        Bar(y, x);
    }

    public void Bar(string y, int x)
    {
    }
}";

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderMethodParameters_InvokeOnReference_NestedCalls1()
        {
            var markup = @"
using System;
class MyClass
{
    public void Foo(int x, string y)
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
    public void Foo(int x, string y)
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

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderMethodParameters_InvokeOnReference_NestedCalls2()
        {
            var markup = @"
using System;
class MyClass
{
    public void Foo(int x, string y)
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
    public void Foo(int x, string y)
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

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderMethodParameters_InvokeOnReference_NestedCalls3()
        {
            var markup = @"
using System;
class MyClass
{
    public void Foo(int x, string y)
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
    public void Foo(int x, string y)
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

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderMethodParameters_InvokeOnReference_Attribute()
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

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderMethodParameters_InvokeOnReference_OnlyHasCandidateSymbols()
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

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderMethodParameters_InvokeOnReference_CallToOtherConstructor()
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

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderMethodParameters_InvokeOnReference_CallToBaseConstructor()
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

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        #endregion

        #region Indexers

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderIndexerParameters_InvokeAtBeginningOfDeclaration()
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

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderIndexerParameters_InParameters()
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

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderIndexerParameters_InvokeAtEndOfDeclaration()
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

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderIndexerParameters_InvokeInAccessor()
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

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderIndexerParameters_InvokeOnReference_BeforeTarget()
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

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderIndexerParameters_InvokeOnReference_InArgumentList()
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

            TestReorderParameters(LanguageNames.CSharp, markup, permutation: permutation, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        #endregion 
    }
}
