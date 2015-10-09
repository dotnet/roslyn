﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.GenerateConstructor;
using Microsoft.CodeAnalysis.CSharp.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.GenerateConstructor
{
    public class GenerateConstructorTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, CodeFixProvider>(
                null, new GenerateConstructorCodeFixProvider());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestWithSimpleArgument()
        {
            Test(
@"class C { void M() { new [|C|](1); } }",
@"class C { private int v; public C(int v) { this.v = v; } void M() { new C(1); } }");
        }

        [Fact, WorkItem(910589), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestWithNoArgs()
        {
            Test(
@"class C { public C(int v) { } void M() { new [|C|](); } }",
@"class C { public C() { } public C(int v) { } void M() { new C(); } }");
        }

        [Fact, WorkItem(910589), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestWithNamedArg()
        {
            Test(
@"class C { void M() { new [|C(foo: 1)|]; } }",
@"class C { private int foo; public C(int foo) { this.foo = foo; } void M() { new C(foo: 1); } }");
        }

        [Fact, WorkItem(910589), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestWithExistingField1()
        {
            Test(
@"class C { void M() { new [|D(foo: 1)|]; } } class D { private int foo; }",
@"class C { void M() { new D(foo: 1); } } class D { private int foo; public D(int foo) { this.foo = foo; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestWithExistingField2()
        {
            Test(
@"class C { void M() { new [|D|](1); } } class D { private string v; }",
@"class C { void M() { new D(1); } } class D { private string v; private int v1; public D(int v1) { this.v1 = v1; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestWithExistingField3()
        {
            Test(
@"class C { void M() { new [|D|](1); } } class B { protected int v; } class D : B { }",
@"class C { void M() { new D(1); } } class B { protected int v; } class D : B { public D(int v) { this.v = v; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestWithExistingField4()
        {
            Test(
@"class C { void M() { new [|D|](1); } } class B { private int v; } class D : B { }",
@"class C { void M() { new D(1); } } class B { private int v; } class D : B { private int v; public D(int v) { this.v = v; } }");
        }

        [WorkItem(539444)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestWithExistingField5()
        {
            Test(
@"class C { void M(int X) { new [|D|](X); } } class D { int X; }",
@"class C { void M(int X) { new D(X); } } class D { int X; public D(int x) { X = x; } }");
        }

        [WorkItem(539444)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestWithExistingField5WithQualification()
        {
            Test(
@"class C { void M(int X) { new [|D|](X); } } class D { int X; }",
@"class C { void M(int X) { new D(X); } } class D { int X; public D(int x) { this.X = x; } }",
                options: new Dictionary<OptionKey, object> { { new OptionKey(SimplificationOptions.QualifyMemberAccessWithThisOrMe, "C#"), true } });
        }

        [WorkItem(539444)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestWithExistingField6()
        {
            Test(
@"class C { void M(int X) { new [|D|](X); } } class B { private int X; } class D : B { }",
@"class C { void M(int X) { new D(X); } } class B { private int X; } class D : B { private int x; public D(int x) { this.x = x; } }");
        }

        [WorkItem(539444)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestWithExistingField7()
        {
            Test(
@"class C { void M(int X) { new [|D|](X); } } class B { protected int X; } class D : B { }",
@"class C { void M(int X) { new D(X); } } class B { protected int X; } class D : B { public D(int x) { X = x; } }");
        }

        [WorkItem(539444)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestWithExistingField7WithQualification()
        {
            Test(
@"class C { void M(int X) { new [|D|](X); } } class B { protected int X; } class D : B { }",
@"class C { void M(int X) { new D(X); } } class B { protected int X; } class D : B { public D(int x) { this.X = x; } }",
                options: new Dictionary<OptionKey, object> { { new OptionKey(SimplificationOptions.QualifyMemberAccessWithThisOrMe, "C#"), true } });
        }

        [WorkItem(539444)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestWithExistingField8()
        {
            Test(
@"class C { void M(int X) { new [|D|](X); } } class B { protected static int x; } class D : B { }",
@"class C { void M(int X) { new D(X); } } class B { protected static int x; } class D : B { private int x1; public D(int x1) { this.x1 = x1; } }");
        }

        [WorkItem(539444)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestWithExistingField9()
        {
            Test(
@"class C { void M(int X) { new [|D|](X); } } class B { protected int x; } class D : B { int X; }",
@"class C { void M(int X) { new D(X); } } class B { protected int x; } class D : B { int X; public D(int x) { this.x = x; } }");
        }

        [WorkItem(539444)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestWithExistingProperty1()
        {
            Test(
@"class C { void M(int X) { new [|D|](X); } } class D { public int X { get; private set; } }",
@"class C { void M(int X) { new D(X); } } class D { public D(int x) { X = x; } public int X { get; private set; } }");
        }

        [WorkItem(539444)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestWithExistingProperty1WithQualification()
        {
            Test(
@"class C { void M(int X) { new [|D|](X); } } class D { public int X { get; private set; } }",
@"class C { void M(int X) { new D(X); } } class D { public D(int x) { this.X = x; } public int X { get; private set; } }",
                options: new Dictionary<OptionKey, object> { { new OptionKey(SimplificationOptions.QualifyMemberAccessWithThisOrMe, "C#"), true } });
        }

        [WorkItem(539444)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestWithExistingProperty2()
        {
            Test(
@"class C { void M(int X) { new [|D|](X); } } class B { public int X { get; private set; } } class D : B { }",
@"class C { void M(int X) { new D(X); } } class B { public int X { get; private set; } } class D : B { private int x; public D(int x) { this.x = x; } }");
        }

        [WorkItem(539444)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestWithExistingProperty3()
        {
            Test(
@"class C { void M(int X) { new [|D|](X); } } class B { public int X { get; protected set; } } class D : B { }",
@"class C { void M(int X) { new D(X); } } class B { public int X { get; protected set; } } class D : B { public D(int x) { X = x; } }");
        }

        [WorkItem(539444)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestWithExistingProperty3WithQualification()
        {
            Test(
@"class C { void M(int X) { new [|D|](X); } } class B { public int X { get; protected set; } } class D : B { }",
@"class C { void M(int X) { new D(X); } } class B { public int X { get; protected set; } } class D : B { public D(int x) { this.X = x; } }",
                options: new Dictionary<OptionKey, object> { { new OptionKey(SimplificationOptions.QualifyMemberAccessWithThisOrMe, "C#"), true } });
        }

        [WorkItem(539444)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestWithExistingProperty4()
        {
            Test(
@"class C { void M(int X) { new [|D|](X); } } class B { protected int X { get; set; } } class D : B { }",
@"class C { void M(int X) { new D(X); } } class B { protected int X { get; set; } } class D : B { public D(int x) { X = x; } }");
        }

        [WorkItem(539444)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestWithExistingProperty4WithQualification()
        {
            Test(
@"class C { void M(int X) { new [|D|](X); } } class B { protected int X { get; set; } } class D : B { }",
@"class C { void M(int X) { new D(X); } } class B { protected int X { get; set; } } class D : B { public D(int x) { this.X = x; } }",
                options: new Dictionary<OptionKey, object> { { new OptionKey(SimplificationOptions.QualifyMemberAccessWithThisOrMe, "C#"), true } });
        }

        [WorkItem(539444)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestWithExistingProperty5()
        {
            Test(
@"class C { void M(int X) { new [|D|](X); } } class B { protected int X { get; } } class D : B { }",
@"class C { void M(int X) { new D(X); } } class B { protected int X { get; } } class D : B { private int x; public D(int x) { this.x = x; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestWithOutParam()
        {
            Test(
@"class C { void M(int i) { new [|D|](out i); } } class D { }",
@"class C { void M(int i) { new D(out i); } } class D { public D(out int i) { i = 0; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestWithBaseDelegatingConstructor1()
        {
            Test(
@"class C { void M() { new [|D|](1); } } class B { protected B(int x) { } } class D : B { }",
@"class C { void M() { new D(1); } } class B { protected B(int x) { } } class D : B { public D(int x) : base(x) { } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestWithBaseDelegatingConstructor2()
        {
            Test(
@"class C { void M() { new [|D|](1); } } class B { private B(int x) { } } class D : B { }",
@"class C { void M() { new D(1); } } class B { private B(int x) { } } class D : B { private int v; public D(int v) { this.v = v; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestStructInLocalInitializerWithSystemType()
        {
            Test(
@"struct S { void M() { S s = new [|S|](System.DateTime.Now); } }",
@"using System; struct S { private DateTime now; public S(DateTime now) { this.now = now; } void M() { S s = new S(System.DateTime.Now); } }");
        }

        [WorkItem(539489)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestEscapedName()
        {
            Test(
@"class C { void M() { new [|@C|](1); } }",
@"class C { private int v; public C(int v) { this.v = v; } void M() { new @C(1); } }");
        }

        [WorkItem(539489)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestEscapedKeyword()
        {
            Test(
@"class @int { void M() { new [|@int|](1); } }",
@"class @int { private int v; public @int(int v) { this.v = v; } void M() { new @int(1); } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestIsSymbolAccessibleWithInternalField()
        {
            Test(
@"class Base { internal long field ; void Main ( ) { int field = 5 ; new [|Derived|] ( field ) ; } } class Derived : Base { } ",
@"class Base { internal long field ; void Main ( ) { int field = 5 ; new Derived ( field ) ; } } class Derived : Base { public Derived ( int field ) { this . field = field ; } } ");
        }

        [WorkItem(539548)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestFormatting()
        {
            Test(
@"class C
{
    void M()
    {
        new [|C|](1);
    }
}",
@"class C
{
    private int v;

    public C(int v)
    {
        this.v = v;
    }

    void M()
    {
        new C(1);
    }
}",
compareTokens: false);
        }

        [WorkItem(5864, "DevDiv_Projects/Roslyn")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestNotOnStructConstructor()
        {
            TestMissing(
@"struct Struct { void Main ( ) { Struct s = new [|Struct|] ( ) ; } } ");
        }

        [WorkItem(539787)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestGenerateIntoCorrectPart()
        {
            Test(
@"partial class C { } partial class C { void Method ( ) { C c = new [|C|] ( ""a"" ) ; } } ",
@"partial class C { } partial class C { private string v ; public C ( string v ) { this . v = v ; } void Method ( ) { C c = new C ( ""a"" ) ; } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestDelegateToSmallerConstructor1()
        {
            Test(
@"class A { void M ( ) { Delta d1 = new Delta ( ""ss"" , 3 ) ; Delta d2 = new [|Delta|] ( ""ss"" , 5 , true ) ; } } class Delta { private string v1 ; private int v2 ; public Delta ( string v1 , int v2 ) { this . v1 = v1 ; this . v2 = v2 ; } } ",
@"class A { void M ( ) { Delta d1 = new Delta ( ""ss"" , 3 ) ; Delta d2 = new Delta ( ""ss"" , 5 , true ) ; } } class Delta { private bool v ; private string v1 ; private int v2 ; public Delta ( string v1 , int v2 ) { this . v1 = v1 ; this . v2 = v2 ; } public Delta ( string v1 , int v2 , bool v ) : this ( v1 , v2 ) { this . v = v ; } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestDelegateToSmallerConstructor2()
        {
            Test(
@"class A { void M ( ) { Delta d1 = new Delta ( ""ss"" , 3 ) ; Delta d2 = new [|Delta|] ( ""ss"" , 5 , true ) ; } } class Delta { private string a ; private int b ; public Delta ( string a , int b ) { this . a = a ; this . b = b ; } } ",
@"class A { void M ( ) { Delta d1 = new Delta ( ""ss"" , 3 ) ; Delta d2 = new Delta ( ""ss"" , 5 , true ) ; } } class Delta { private string a ; private int b ; private bool v ; public Delta ( string a , int b ) { this . a = a ; this . b = b ; } public Delta ( string a , int b , bool v) : this ( a , b ) { this . v = v ; } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestDelegateToSmallerConstructor3()
        {
            Test(
@"class A { void M ( ) { var d1 = new Base ( ""ss"" , 3 ) ; var d2 = new [|Delta|] ( ""ss"" , 5 , true ) ; } } class Base { private string v1 ; private int v2 ; public Base ( string v1 , int v2 ) { this . v1 = v1 ; this . v2 = v2 ; } } class Delta : Base { } ",
@"class A { void M ( ) { var d1 = new Base ( ""ss"" , 3 ) ; var d2 = new Delta ( ""ss"" , 5 , true ) ; } } class Base { private string v1 ; private int v2 ; public Base ( string v1 , int v2 ) { this . v1 = v1 ; this . v2 = v2 ; } } class Delta : Base { private bool v ; public Delta ( string v1 , int v2 , bool v ) : base ( v1 , v2 ) { this . v = v ; } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestDelegateToSmallerConstructor4()
        {
            Test(
@"class A { void M ( ) { Delta d1 = new Delta ( ""ss"" , 3 ) ; Delta d2 = new [|Delta|] ( ""ss"" , 5 , true ) ; } } class Delta { private string v1 ; private int v2 ; public Delta ( string v1 , int v2 ) { this . v1 = v1 ; this . v2 = v2 ; } } ",
@"class A { void M ( ) { Delta d1 = new Delta ( ""ss"" , 3 ) ; Delta d2 = new Delta ( ""ss"" , 5 , true ) ; } } class Delta { private bool v ; private string v1 ; private int v2 ;  public Delta ( string v1 , int v2 ) { this . v1 = v1 ; this . v2 = v2 ; } public Delta ( string v1 , int v2 , bool v ) : this ( v1 , v2 ) { this . v = v ; } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestGenerateFromThisInitializer1()
        {
            Test(
@"class C { public C ( ) [|: this ( 4 )|] { } } ",
@"class C { private int v ; public C ( ) : this ( 4 ) { } public C ( int v ) { this . v = v ; } } ");
        }

        [Fact, WorkItem(910589), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestGenerateFromThisInitializer2()
        {
            Test(
@"class C { public C ( int i ) [|: this ( )|] { } } ",
@"class C { public C ( ) { } public C ( int i ) : this ( ) { } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestGenerateFromBaseInitializer1()
        {
            Test(
@"class C : B { public C ( int i ) [|: base ( i )|] { } } class B { } ",
@"class C : B { public C ( int i ) : base ( i ) { } } class B { private int i ; public B ( int i ) { this . i = i ; } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestGenerateFromBaseInitializer2()
        {
            Test(
@"class C : B { public C ( int i ) [|: base ( i )|] { } } class B { int i ; } ",
@"class C : B { public C ( int i ) : base ( i ) { } } class B { int i ; public B ( int i ) { this . i = i ; } } ");
        }

        [WorkItem(539969)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestNotOnExistingConstructor()
        {
            TestMissing(
@"class C { private class D { } } class A { void M ( ) { C . D d = new C . [|D|] ( ) ; } } ");
        }

        [WorkItem(539972)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestUnavailableTypeParameters()
        {
            Test(
@"class C < T1 , T2 > { public void Foo ( T1 t1 , T2 t2 ) { A a = new [|A|] ( t1 , t2 ) ; } } internal class A { } ",
@"class C < T1 , T2 > { public void Foo ( T1 t1 , T2 t2 ) { A a = new A ( t1 , t2 ) ; } } internal class A { private object t1 ; private object t2 ; public A ( object t1 , object t2 ) { this . t1 = t1 ; this . t2 = t2 ; } } ");
        }

        [WorkItem(541020)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestGenerateCallToDefaultConstructorInStruct()
        {
            Test(
@"class Program { void Main ( ) { Apartment Metropolitan = new Apartment ( [|""Pine""|] ) ; } } struct Apartment { private int v1 ; public Apartment ( int v1 ) { this . v1 = v1 ; } } ",
@"class Program { void Main ( ) { Apartment Metropolitan = new Apartment ( ""Pine"" ) ; } } struct Apartment { private string v ; private int v1 ; public Apartment ( string v ) : this ( ) { this . v = v ; } public Apartment ( int v1 ) { this . v1 = v1 ; } }");
        }

        [WorkItem(541121)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestReadonlyFieldDelegation()
        {
            Test(
@"class C { private readonly int x ; void Test ( ) { int x = 10 ; C c = new [|C|] ( x ) ; } } ",
@"class C { private readonly int x ; public C ( int x ) { this . x = x ; } void Test ( ) { int x = 10 ; C c = new C ( x ) ; } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public void TestNoGenerationIntoEntirelyHiddenType()
        {
            TestMissing(
@"
class C
{
    void Foo()
    {
        new [|D|](1, 2, 3);
    }
}

#line hidden
class D
{
}
#line default
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public void TestNestedConstructorCall()
        {
            Test(
@"
class C
{
    void Foo()
    {
        var d = new D([|v|]: new D(u: 1));
    }
}

class D
{
    private int u;

    public D(int u)
    {
    }
}
",
@"
class C
{
    void Foo()
    {
        var d = new D(v: new D(u: 1));
    }
}

class D
{
    private int u;
    private D v;

    public D(D v)
    {
        this.v = v;
    }

    public D(int u)
    {
    }
}
");
        }

        [WorkItem(530003)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestAttributesWithArgument()
        {
            Test(
@"using System; [AttributeUsage(AttributeTargets.Class)] class MyAttribute : Attribute {} [[|MyAttribute(123)|]] class D {} ",
@"using System; [AttributeUsage(AttributeTargets.Class)] class MyAttribute : Attribute { private int v; public MyAttribute(int v) { this.v = v; } } [MyAttribute(123)] class D {} ");
        }

        [WorkItem(530003)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestAttributesWithMultipleArguments()
        {
            Test(
@"using System; [AttributeUsage(AttributeTargets.Class)] class MyAttribute : Attribute {} [[|MyAttribute(true, 1, ""hello"")|]] class D {} ",
@"using System; [AttributeUsage(AttributeTargets.Class)] class MyAttribute : Attribute { private bool v1; private int v2; private string v3; public MyAttribute(bool v1, int v2, string v3) { this.v1 = v1; this.v2 = v2; this.v3 = v3; } } [MyAttribute(true, 1, ""hello"")] class D {} ");
        }

        [WorkItem(530003)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestAttributesWithNamedArguments()
        {
            Test(
@"using System; [AttributeUsage(AttributeTargets.Class)] class MyAttribute : Attribute {} [[|MyAttribute(true, 1, topic = ""hello"")|]] class D {} ",
@"using System; [AttributeUsage(AttributeTargets.Class)] class MyAttribute : Attribute { private string topic; private bool v1; private int v2; public MyAttribute(bool v1, int v2, string topic) { this.v1 = v1; this.v2 = v2; this.topic = topic; } } [MyAttribute(true, 1, topic = ""hello"")] class D {} ");
        }

        [WorkItem(530003)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestAttributesWithAdditionalConstructors()
        {
            Test(
@"using System; [AttributeUsage(AttributeTargets.Class)] class MyAttribute : Attribute { private int v; public MyAttribute(int v) { this.v = v; } } [[|MyAttribute(true, 1)|]] class D {} ",
@"using System; [AttributeUsage(AttributeTargets.Class)] class MyAttribute : Attribute { private int v; private bool v1; private int v2; public MyAttribute(int v) { this.v = v; } public MyAttribute(bool v1, int v2) { this.v1 = v1; this.v2 = v2; } } [MyAttribute(true, 1)] class D {} ");
        }

        [WorkItem(530003)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestAttributesWithOverloading()
        {
            Test(
@"using System; [AttributeUsage(AttributeTargets.Class)] class MyAttribute : Attribute { private int v; public MyAttribute(int v) { this.v = v; } } [[|MyAttribute(true)|]] class D {} ",
@"using System; [AttributeUsage(AttributeTargets.Class)] class MyAttribute : Attribute { private int v; private bool v1; public MyAttribute(bool v1) { this.v1 = v1; } public MyAttribute(int v) { this.v = v; } } [MyAttribute(true)] class D {} ");
        }

        [WorkItem(530003)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestAttributesWithOverloadingMultipleParameters()
        {
            Test(
@"using System; [AttributeUsage(AttributeTargets.Class)] class MyAttrAttribute : Attribute { private bool v1; private int v2; public MyAttrAttribute(bool v1, int v2) { this.v1 = v1; this.v2 = v2; } } [|[MyAttrAttribute(1,true)]|] class D { } ",
@"using System; [AttributeUsage(AttributeTargets.Class)] class MyAttrAttribute : Attribute { private int v; private bool v1; private int v2; private bool v3; public MyAttrAttribute(int v, bool v3) { this.v = v; this.v3 = v3; } public MyAttrAttribute(bool v1, int v2) { this.v1 = v1; this.v2 = v2; } } [MyAttrAttribute(1,true)] class D { } ");
        }

        [WorkItem(530003)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestAttributesWithAllValidParameters()
        {
            Test(
@"using System; enum A { A1 } [AttributeUsage(AttributeTargets.Class)] class MyAttrAttribute : Attribute { } [|[MyAttrAttribute(new int[] { 1, 2, 3}, A.A1, true, (byte)1, 'a', (short)12, (int) 1, (long) 5L, 5D, 3.5F, ""hello"")]|] class D { } ",
@"using System; enum A { A1 } [AttributeUsage(AttributeTargets.Class)] class MyAttrAttribute : Attribute { private A a1; private int[] v1; private string v10; private bool v2; private byte v3; private char v4; private short v5; private int v6; private long v7; private double v8; private float v9; public MyAttrAttribute(int[] v1, A a1, bool v2, byte v3, char v4, short v5, int v6, long v7, double v8, float v9, string v10) { this.v1 = v1; this.a1 = a1; this.v2 = v2; this.v3 = v3; this.v4 = v4; this.v5 = v5; this.v6 = v6; this.v7 = v7; this.v8 = v8; this.v9 = v9; this.v10 = v10; } } [MyAttrAttribute(new int[] { 1, 2, 3 }, A.A1, true, (byte)1, 'a', (short)12, (int)1, (long)5L, 5D, 3.5F, ""hello"")] class D { } ");
        }

        [WorkItem(530003)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestAttributesWithDelegation()
        {
            TestMissing(
@"using System; [AttributeUsage(AttributeTargets.Class)] class MyAttrAttribute : Attribute { } [|[MyAttrAttribute(()=>{return;})]|] class D { } ");
        }

        [WorkItem(530003)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public void TestAttributesWithLambda()
        {
            TestMissing(
@"using System; [AttributeUsage(AttributeTargets.Class)] class MyAttrAttribute : Attribute { } [|[MyAttrAttribute(()=>5)]|] class D { } ");
        }

        [WorkItem(889349)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public void TestConstructorGenerationForDifferentNamedParameter()
        {
            Test(
@"
class Program
{
    static void Main(string[] args)
    {
        var ss = new [|Program(wde: 1)|];
    }

    Program(int s)
    {

    }
}
",
@"
class Program
{
    private int wde;

    static void Main(string[] args)
    {
        var ss = new Program(wde: 1);
    }

    Program(int s)
    {

    }

    public Program(int wde)
    {
        this.wde = wde;
    }
}
", compareTokens: false);
        }

        [WorkItem(528257)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public void TestGenerateInInaccessibleType()
        {
            Test(
@"class Foo { class Bar { } } class A { static void Main(string[] args) { var s = new [|Foo.Bar(5)|]; } }",
@"class Foo { class Bar { private int v; public Bar(int v) { this.v = v; } } } class A { static void Main(string[] args) { var s = new Foo.Bar(5); } }");
        }

        public partial class GenerateConstructorTestsWithFindMissingIdentifiersAnalyzer : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
        {
            internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
            {
                return new Tuple<DiagnosticAnalyzer, CodeFixProvider>(
                new CSharpUnboundIdentifiersDiagnosticAnalyzer(), new GenerateConstructorCodeFixProvider());
            }

            [WorkItem(1241, @"https://github.com/dotnet/roslyn/issues/1241")]
            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
            public void TestGenerateConstructorInIncompleteLambda()
            {
                Test(
    @"using System . Threading . Tasks ; class C { C ( ) { Task . Run ( ( ) => { new [|C|] ( 0 ) } ) ; } } ",
    @"using System . Threading . Tasks ; class C { private int v ; public C ( int v ) { this . v = v ; } C ( ) { Task . Run ( ( ) => { new C ( 0 ) } ) ; } } ");
            }
        }

        [WorkItem(5274, "https://github.com/dotnet/roslyn/issues/5274")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public void TestGenerateIntoDerivedClassWithAbstractBase()
        {
            Test(
@"
class Class1
{
    private void Foo(string value)
    {
        var rewriter = new [|Derived|](value);
    }

    private class Derived : Base
    {
    }

    public abstract partial class Base
    {
        private readonly bool _val;

        public Base(bool val = false)
        {
            _val = val;
        }
    }
}",
@"
class Class1
{
    private void Foo(string value)
    {
        var rewriter = new Derived(value);
    }

    private class Derived : Base
    {
        private string value;

        public Derived(string value)
        {
            this.value = value;
        }
    }

    public abstract partial class Base
    {
        private readonly bool _val;

        public Base(bool val = false)
        {
            _val = val;
        }
    }
}");
        }
    }
}
