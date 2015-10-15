// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ChangeSignature
{
    public partial class ChangeSignatureTests : AbstractChangeSignatureTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void ChangeSignature_Formatting_KeepCountsPerLine()
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
            var updatedSignature = new[] { 5, 4, 3, 2, 1, 0 };
            var expectedUpdatedCode = @"
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
}";
            TestChangeSignatureViaCommand(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void ChangeSignature_Formatting_Method()
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
            var updatedSignature = new[] { 1, 0 };
            var expectedUpdatedCode = @"
class C
{
    void Method(int b,
        int a)
    {
        Method(2,
            1);
    }
}";
            TestChangeSignatureViaCommand(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void ChangeSignature_Formatting_Constructor()
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
            var updatedSignature = new[] { 1, 0 };
            var expectedUpdatedCode = @"
class SomeClass
{
    SomeClass(int b,
        int a)
    {
        new SomeClass(2,
            1);
    }
}";
            TestChangeSignatureViaCommand(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void ChangeSignature_Formatting_Indexer()
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
            var updatedSignature = new[] { 1, 0 };
            var expectedUpdatedCode = @"
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
}";
            TestChangeSignatureViaCommand(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void ChangeSignature_Formatting_Delegate()
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
            var updatedSignature = new[] { 1, 0 };
            var expectedUpdatedCode = @"
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
}";
            TestChangeSignatureViaCommand(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void ChangeSignature_Formatting_AnonymousMethod()
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
            var updatedSignature = new[] { 1, 0 };
            var expectedUpdatedCode = @"
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
}";
            TestChangeSignatureViaCommand(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void ChangeSignature_Formatting_ConstructorInitializers()
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
            var updatedSignature = new[] { 1, 0 };
            var expectedUpdatedCode = @"
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
}";
            TestChangeSignatureViaCommand(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void ChangeSignature_Formatting_Attribute()
        {
            var markup = @"
[Custom(1,
    2)]
class CustomAttribute : System.Attribute
{
    public $$CustomAttribute(int x, int y) { }
}";
            var updatedSignature = new[] { 1, 0 };
            var expectedUpdatedCode = @"
[Custom(2,
    1)]
class CustomAttribute : System.Attribute
{
    public CustomAttribute(int y, int x) { }
}";
            TestChangeSignatureViaCommand(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }

        [WorkItem(946220)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void ChangeSignature_Formatting_LambdaAsArgument()
        {
            var markup = @"class C
{
    void M(System.Action<int, int> f, int z$$)
    {
        M((x, y) => System.Console.WriteLine(x + y), 5);
    }
}";
            var updatedSignature = new[] { 0 };
            var expectedUpdatedCode = @"class C
{
    void M(System.Action<int, int> f)
    {
        M((x, y) => System.Console.WriteLine(x + y));
    }
}";
            TestChangeSignatureViaCommand(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
        }
    }
}
