// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.GenerateVariable;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.GenerateVariable
{
    public class GenerateVariableTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, CodeFixProvider>(
                null, new GenerateVariableCodeFixProvider());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestSimpleLowercaseIdentifier1()
        {
            Test(
@"class Class { void Method() { [|foo|]; } }",
@"class Class { private object foo; void Method() { foo; } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestSimpleLowercaseIdentifier2()
        {
            Test(
@"class Class { void Method() { [|foo|]; } }",
@"class Class { private readonly object foo; void Method() { foo; } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestTestSimpleLowercaseIdentifier3()
        {
            Test(
@"class Class { void Method() { [|foo|]; } }",
@"class Class { public object foo { get; private set; } void Method() { foo; } }",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestSimpleUppercaseIdentifier1()
        {
            Test(
@"class Class { void Method() { [|Foo|]; } }",
@"class Class { public object Foo { get; private set; } void Method() { Foo; } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestSimpleUppercaseIdentifier2()
        {
            Test(
@"class Class { void Method() { [|Foo|]; } }",
@"class Class { private object Foo; void Method() { Foo; } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestSimpleUppercaseIdentifier3()
        {
            Test(
@"class Class { void Method() { [|Foo|]; } }",
@"class Class { private readonly object Foo; void Method() { Foo; } }",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestSimpleRead1()
        {
            Test(
@"class Class { void Method(int i) { Method([|foo|]); } }",
@"class Class { private int foo; void Method(int i) { Method(foo); } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestSimpleWriteCount()
        {
            TestExactActionSetOffered(
@"class Class { void Method(int i) { [|foo|] = 1; } }",
new[] { string.Format(FeaturesResources.GenerateFieldIn, "foo", "Class"), string.Format(FeaturesResources.GeneratePropertyIn, "foo", "Class"), string.Format(FeaturesResources.GenerateLocal, "foo") });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestSimpleWrite1()
        {
            Test(
@"class Class { void Method(int i) { [|foo|] = 1; } }",
@"class Class { private int foo; void Method(int i) { foo = 1; } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestSimpleWrite2()
        {
            Test(
@"class Class { void Method(int i) { [|foo|] = 1; } }",
@"class Class { public int foo{ get; private set; } void Method(int i) { foo = 1; } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInRefCodeActionCount()
        {
            TestExactActionSetOffered(
@"class Class { void Method(ref int i) { Method(ref [|foo|]); } }",
new[] { string.Format(FeaturesResources.GenerateFieldIn, "foo", "Class"), string.Format(FeaturesResources.GenerateLocal, "foo") });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInRef1()
        {
            Test(
@"class Class { void Method(ref int i) { Method(ref [|foo|]); } }",
@"class Class { private int foo; void Method(ref int i) { Method(ref foo); } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInOutCodeActionCount()
        {
            TestExactActionSetOffered(
@"class Class { void Method(out int i) { Method(out [|foo|]); } }",
new[] { string.Format(FeaturesResources.GenerateFieldIn, "foo", "Class"), string.Format(FeaturesResources.GenerateLocal, "foo") });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInOut1()
        {
            Test(
@"class Class { void Method(out int i) { Method(out [|foo|]); } }",
@"class Class { private int foo; void Method(out int i) { Method(out foo); } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateInStaticMember1()
        {
            Test(
@"class Class { static void Method() { [|foo|]; } }",
@"class Class { private static object foo; static void Method() { foo; } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateInStaticMember2()
        {
            Test(
@"class Class { static void Method() { [|foo|]; } }",
@"class Class { private static readonly object foo; static void Method() { foo; } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateInStaticMember3()
        {
            Test(
@"class Class { static void Method() { [|foo|]; } }",
@"class Class { public static object foo { get; private set; } static void Method() { foo; } }",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateOffInstance1()
        {
            Test(
@"class Class { void Method() { this.[|foo|]; } }",
@"class Class { private object foo; void Method() { this.foo; } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateOffInstance2()
        {
            Test(
@"class Class { void Method() { this.[|foo|]; } }",
@"class Class { private readonly object foo; void Method() { this.foo; } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateOffInstance3()
        {
            Test(
@"class Class { void Method() { this.[|foo|]; } }",
@"class Class { public object foo { get; private set; } void Method() { this.foo; } }",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateOffWrittenInstance1()
        {
            Test(
@"class Class { void Method() { this.[|foo|] = 1; } }",
@"class Class { private int foo; void Method() { this.foo = 1; } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateOffWrittenInstance2()
        {
            Test(
@"class Class { void Method() { this.[|foo|] = 1; } }",
@"class Class { public int foo { get; private set; } void Method() { this.foo = 1; } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateOffStatic1()
        {
            Test(
@"class Class { void Method() { Class.[|foo|]; } }",
@"class Class { private static object foo; void Method() { Class.foo; } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateOffStatic2()
        {
            Test(
@"class Class { void Method() { Class.[|foo|]; } }",
@"class Class { private static readonly object foo; void Method() { Class.foo; } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateOffStatic3()
        {
            Test(
@"class Class { void Method() { Class.[|foo|]; } }",
@"class Class { public static object foo { get; private set; } void Method() { Class.foo; } }",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateOffWrittenStatic1()
        {
            Test(
@"class Class { void Method() { Class.[|foo|] = 1; } }",
@"class Class { private static int foo; void Method() { Class.foo = 1; } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateOffWrittenStatic2()
        {
            Test(
@"class Class { void Method() { Class.[|foo|] = 1; } }",
@"class Class { public static int foo { get; private set; } void Method() { Class.foo = 1; } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateInstanceIntoSibling1()
        {
            Test(
@"class Class { void Method() { new D().[|foo|]; } } class D { }",
@"class Class { void Method() { new D().foo; } } class D { internal object foo; }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateInstanceIntoOuter1()
        {
            Test(
@"class Outer { class Class { void Method() { new Outer().[|foo|]; } } }",
@"class Outer { private object foo; class Class { void Method() { new Outer().foo; } } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateInstanceIntoDerived1()
        {
            Test(
@"class Class : Base { void Method(Base b) { b.[|foo|]; } } class Base { }",
@"class Class : Base { void Method(Base b) { b.foo; } } class Base { internal object foo; }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateStaticIntoDerived1()
        {
            Test(
@"class Class : Base { void Method(Base b) { Base.[|foo|]; } } class Base { }",
@"class Class : Base { void Method(Base b) { Base.foo; } } class Base { protected static object foo; }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateIntoInterfaceFixCount()
        {
            TestActionCount(
@"class Class { void Method(I i) { i.[|foo|]; } } interface I { }",
count: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateIntoInterface1()
        {
            Test(
@"class Class { void Method(I i) { i.[|Foo|]; } } interface I { }",
@"class Class { void Method(I i) { i.Foo; } } interface I { object Foo { get; set; } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateIntoInterface2()
        {
            Test(
@"class Class { void Method(I i) { i.[|Foo|]; } } interface I { }",
@"class Class { void Method(I i) { i.Foo; } } interface I { object Foo { get; } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateStaticIntoInterfaceMissing()
        {
            TestMissing(
@"class Class { void Method(I i) { I.[|Foo|]; } } interface I { }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateWriteIntoInterfaceFixCount()
        {
            TestActionCount(
@"class Class { void Method(I i) { i.[|Foo|] = 1; } } interface I { }",
count: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateWriteIntoInterface1()
        {
            Test(
@"class Class { void Method(I i) { i.[|Foo|] = 1; } } interface I { }",
@"class Class { void Method(I i) { i.Foo = 1; } } interface I { int Foo { get; set; } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateInGenericType()
        {
            Test(
@"class Class<T> { void Method(T t) { [|foo|] = t; } }",
@"class Class<T> { private T foo; void Method(T t) { foo = t; } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateInGenericMethod1()
        {
            Test(
@"class Class { void Method<T>(T t) { [|foo|] = t; } }",
@"class Class { private object foo; void Method<T>(T t) { foo = t; } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateInGenericMethod2()
        {
            Test(
@"class Class { void Method<T>(IList<T> t) { [|foo|] = t; } }",
@"class Class { private IList<object> foo; void Method<T>(IList<T> t) { foo = t; } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateFieldBeforeFirstField()
        {
            Test(
@"class Class { int i; void Method() { [|foo|]; } }",
@"class Class { private object foo; int i; void Method() { foo; } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateFieldAfterLastField()
        {
            Test(
@"class Class { void Method() { [|foo|]; } int i; }",
@"class Class { void Method() { foo; } int i; private object foo; }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGeneratePropertyAfterLastField1()
        {
            Test(
@"class Class { int Bar; void Method() { [|Foo|]; } }",
@"class Class { int Bar; public object Foo { get; private set; } void Method() { Foo; } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGeneratePropertyAfterLastField2()
        {
            Test(
@"class Class { void Method() { [|Foo|]; } int Bar; }",
@"class Class { void Method() { Foo; } int Bar; public object Foo { get; private set; } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGeneratePropertyBeforeFirstProperty()
        {
            Test(
@"class Class { int Quux { get; } void Method() { [|Foo|]; } }",
@"class Class { public object Foo { get; private set; } int Quux { get; } void Method() { Foo; } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGeneratePropertyBeforeFirstPropertyEvenWithField1()
        {
            Test(
@"class Class { int Bar; int Quux { get; } void Method() { [|Foo|]; } }",
@"class Class { int Bar; public object Foo { get; private set; } int Quux { get; } void Method() { Foo; } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGeneratePropertyAfterLastPropertyEvenWithField2()
        {
            Test(
@"class Class { int Quux { get; } int Bar; void Method() { [|Foo|]; } }",
@"class Class { int Quux { get; } public object Foo { get; private set; } int Bar; void Method() { Foo; } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestMissingInInvocation()
        {
            TestMissing(
@"class Class { void Method() { [|Foo|](); } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestMissingInObjectCreation()
        {
            TestMissing(
@"class Class { void Method() { new [|Foo|](); } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestMissingInTypeDeclaration()
        {
            TestMissing(
@"class Class { void Method() { [|A|] a; } }");

            TestMissing(
@"class Class { void Method() { [|A.B|] a; } }");

            TestMissing(
@"class Class { void Method() { [|A|].B a; } }");

            TestMissing(
@"class Class { void Method() { A.[|B|] a; } }");

            TestMissing(
@"class Class { void Method() { [|A.B.C|] a; } }");

            TestMissing(
@"class Class { void Method() { [|A.B|].C a; } }");

            TestMissing(
@"class Class { void Method() { A.B.[|C|] a; } }");

            TestMissing(
@"class Class { void Method() { [|A|].B.C a; } }");

            TestMissing(
@"class Class { void Method() { A.[|B|].C a; } }");
        }

        [WorkItem(539336)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestMissingInAttribute()
        {
            TestMissing(
@"[[|A|]]class Class { }");

            TestMissing(
@"[[|A.B|]]class Class { }");

            TestMissing(
@"[[|A|].B]class Class { }");

            TestMissing(
@"[A.[|B|]]class Class { }");

            TestMissing(
@"[[|A.B.C|]]class Class { }");

            TestMissing(
@"[[|A.B|].C]class Class { }");

            TestMissing(
@"[A.B.[|C|]]class Class { }");

            TestMissing(
@"[[|A|].B.C]class Class { }");

            TestMissing(
@"[A.B.[|C|]]class Class { }");
        }

        [WorkItem(539340)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestSpansField()
        {
            TestSpans(
@"class C { void M() { this.[|Foo|] }",
@"class C { void M() { this.[|Foo|] }");

            TestSpans(
@"class C { void M() { this.[|Foo|]; }",
@"class C { void M() { this.[|Foo|]; }");

            TestSpans(
@"class C { void M() { this.[|Foo|] = 1 }",
@"class C { void M() { this.[|Foo|] = 1 }");

            TestSpans(
@"class C { void M() { this.[|Foo|] = 1 + 2 }",
@"class C { void M() { this.[|Foo|] = 1 + 2 }");

            TestSpans(
@"class C { void M() { this.[|Foo|] = 1 + 2; }",
@"class C { void M() { this.[|Foo|] = 1 + 2; }");

            TestSpans(
@"class C { void M() { this.[|Foo|] += Bar() }",
@"class C { void M() { this.[|Foo|] += Bar() }");

            TestSpans(
@"class C { void M() { this.[|Foo|] += Bar(); }",
@"class C { void M() { this.[|Foo|] += Bar(); }");
        }

        [WorkItem(539427)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateFromLambda()
        {
            Test(
@"class Class { void Method(int i) { [|foo|] = () => { return 2 }; } }",
@"using System; class Class { private Func<int> foo; void Method(int i) { foo = () => { return 2 }; } }");
        }

        // TODO: Move to TypeInferrer.InferTypes, or something
        [WorkItem(539466)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateInMethodOverload1()
        {
            Test(
@"class Class { void Method(int i) { System.Console.WriteLine([|foo|]); } }",
@"class Class { private bool foo; void Method(int i) { System.Console.WriteLine(foo); } }");
        }

        // TODO: Move to TypeInferrer.InferTypes, or something
        [WorkItem(539466)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateInMethodOverload2()
        {
            Test(
@"class Class { void Method(int i) { System.Console.WriteLine(this.[|foo|]); } }",
@"class Class { private bool foo; void Method(int i) { System.Console.WriteLine(this.foo); } }");
        }

        [WorkItem(539468)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestExplicitProperty1()
        {
            Test(
@"class Class : ITest { bool ITest.[|SomeProp|] { get; set; } } interface ITest { }",
@"class Class : ITest { bool ITest.SomeProp { get; set; } } interface ITest { bool SomeProp { get; set; } }");
        }

        [WorkItem(539468)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestExplicitProperty2()
        {
            Test(
@"class Class : ITest { bool ITest.[|SomeProp|] { } } interface ITest { }",
@"class Class : ITest { bool ITest.SomeProp { } } interface ITest { bool SomeProp { get; set; } }");
        }

        [WorkItem(539468)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestExplicitProperty3()
        {
            Test(
@"class Class : ITest { bool ITest.[|SomeProp|] { } } interface ITest { }",
@"class Class : ITest { bool ITest.SomeProp { } } interface ITest { bool SomeProp { get; } }",
index: 1);
        }

        [WorkItem(539468)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestExplicitProperty4()
        {
            TestMissing(
@"class Class { bool ITest.[|SomeProp|] { } } interface ITest { }");
        }

        [WorkItem(539468)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestExplicitProperty5()
        {
            TestMissing(
@"class Class : ITest { bool ITest.[|SomeProp|] { } } interface ITest { bool SomeProp { get; } }");
        }

        [WorkItem(539489)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestEscapedName()
        {
            Test(
@"class Class { void Method() { [|@foo|]; } }",
@"class Class { private object foo; void Method() { @foo; } }");
        }

        [WorkItem(539489)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestEscapedKeyword()
        {
            Test(
@"class Class { void Method() { [|@int|]; } }",
@"class Class { private object @int; void Method() { @int; } }");
        }

        [WorkItem(539529)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestRefLambda()
        {
            Test(
@"class Class { void Method() { [|test|] = (ref int x) => x = 10; } }",
@"class Class { private object test; void Method() { test = (ref int x) => x = 10; } }");
        }

        [WorkItem(539595)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestNotOnError()
        {
            TestMissing(
@"class Class { void F<U,V>(U u1, V v1) { Foo<string,int>([|u1|], u2); } }");
        }

        [WorkItem(539571)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestNameSimplification()
        {
            Test(
@"namespace TestNs { class Program { class Test { void Meth ( ) { Program . [|blah|] = new Test ( ) ; } } } } ",
@"namespace TestNs { class Program { private static Test blah ; class Test { void Meth ( ) { Program . blah = new Test ( ) ; } } } } ");
        }

        [WorkItem(539717)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestPostIncrement()
        {
            Test(
@"class Program { static void Main ( string [ ] args ) { [|i|] ++ ; } } ",
@"class Program { private static int i ; static void Main ( string [ ] args ) { i ++ ; } } ");
        }

        [WorkItem(539717)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestPreDecrement()
        {
            Test(
@"class Program { static void Main ( string [ ] args ) { -- [|i|] ; } } ",
@"class Program { private static int i ; static void Main ( string [ ] args ) { -- i ; } } ");
        }

        [WorkItem(539738)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateIntoScript()
        {
            Test(
@"using C ; static class C { } C . [|i|] ++ ; ",
@"using C ; static class C { internal static int i ; } C . i ++ ; ",
parseOptions: Options.Script);
        }

        [WorkItem(539558)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void BugFix5565()
        {
            Test(
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        [|Foo|]#();
    }
}",
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    public static object Foo { get; private set; }

    static void Main(string[] args)
    {
        Foo#();
    }
}",
compareTokens: false);
        }

        [WorkItem(539536)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void BugFix5538()
        {
            Test(
@"using System ; using System . Collections . Generic ; using System . Linq ; class Program { static void Main ( string [ ] args ) { new ( [|foo|] ) ( ) ; } } ",
@"using System ; using System . Collections . Generic ; using System . Linq ; class Program { public static object foo { get ; private set ; } static void Main ( string [ ] args ) { new ( foo ) ( ) ; } } ",
index: 2);
        }

        [WorkItem(539665)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void BugFix5697()
        {
            Test(
@"class C { }
class D
{
    void M()
    {
        C.[|P|] = 10;
    }
}
",
@"class C
{
    public static int P { get; internal set; }
}
class D
{
    void M()
    {
        C.P = 10;
    }
}
",
compareTokens: false);
        }

        [WorkItem(539793)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestIncrement()
        {
            TestExactActionSetOffered(
@"class Program { static void Main ( ) { [|p|] ++ ; } } ",
new[] { string.Format(FeaturesResources.GenerateFieldIn, "p", "Program"), string.Format(FeaturesResources.GeneratePropertyIn, "p", "Program"), string.Format(FeaturesResources.GenerateLocal, "p") });

            Test(
@"class Program { static void Main ( ) { [|p|] ++ ; } } ",
@"class Program { private static int p ; static void Main ( ) { p ++ ; } } ");
        }

        [WorkItem(539834)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public void TestNotInGoto()
        {
            TestMissing(
@"class Program { static void Main ( ) { goto [|foo|] ; } } ");
        }

        [WorkItem(539826)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestOnLeftOfDot()
        {
            Test(
@"class Program { static void Main ( ) { [|foo|] . ToString ( ) ; } } ",
@"class Program { private static object foo ; static void Main ( ) { foo . ToString ( ) ; } } ");
        }

        [WorkItem(539840)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestNotBeforeAlias()
        {
            TestMissing(
@"using System ; using System . Collections . Generic ; using System . Linq ; class Program { static void Main ( string [ ] args ) { [|global|] :: System . String s ; } } ");
        }

        [WorkItem(539871)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestMissingOnGenericName()
        {
            TestMissing(
@"class C < T > { public delegate void Foo < R > ( R r ) ; static void M ( ) { Foo < T > r = [|Goo < T >|] ; } } ");
        }

        [WorkItem(539934)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestOnDelegateAddition()
        {
            Test(
@"class C { delegate void D ( ) ; void M ( ) { D d = [|M1|] + M2 ; } } ",
@"class C { private D M1 { get ; set ; } delegate void D ( ) ; void M ( ) { D d = M1 + M2 ; } } ",
parseOptions: null);
        }

        [WorkItem(539986)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestReferenceTypeParameter1()
        {
            Test(
@"class C < T > { public void Test ( ) { C < T > c = A . [|M|] ; } } class A { } ",
@"class C < T > { public void Test ( ) { C < T > c = A . M ; } } class A { public static C < object > M { get ; internal set ; } } ");
        }

        [WorkItem(539986)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestReferenceTypeParameter2()
        {
            Test(
@"class C < T > { public void Test ( ) { C < T > c = A . [|M|] ; } class A { } } ",
@"class C < T > { public void Test ( ) { C < T > c = A . M ; } class A { public static C < T > M { get ; internal set ; } } } ");
        }

        [WorkItem(540159)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestEmptyIdentifierName()
        {
            TestMissing(
@"class C { static void M ( ) { int i = [|@|] } } ");
            TestMissing(
@"class C { static void M ( ) { int i = [|@ |]} } ");
        }

        [WorkItem(541194)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestForeachVar()
        {
            Test(
@"class C { void M ( ) { foreach ( var v in [|list|] ) { } } } ",
@"using System.Collections.Generic; class C { private IEnumerable < object > list ; void M ( ) { foreach ( var v in list ) { } } } ");
        }

        [WorkItem(541265)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestExtensionMethodUsedAsInstance()
        {
            Test(
@"using System ; class C { public static void Main ( ) { string s = ""Hello"" ; [|f|] = s . ExtensionMethod ; } } public static class MyExtension { public static int ExtensionMethod ( this String s ) { return s . Length ; } } ",
@"using System ; class C { private static Func < int > f ; public static void Main ( ) { string s = ""Hello"" ; f = s . ExtensionMethod ; } } public static class MyExtension { public static int ExtensionMethod ( this String s ) { return s . Length ; } } ",
parseOptions: null);
        }

        [WorkItem(541549)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestDelegateInvoke()
        {
            Test(
@"using System ; class Program { static void Main ( string [ ] args ) { Func < int , int > f = x => x + 1 ; f ( [|x|] ) ; } } ",
@"using System ; class Program { private static int x ; static void Main ( string [ ] args ) { Func < int , int > f = x => x + 1 ; f ( x ) ; } } ");
        }

        [WorkItem(541597)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestComplexAssign1()
        {
            Test(
@"class Program { static void Main ( string [ ] args ) { [|a|] = a + 10 ; } } ",
@"class Program { private static int a ; static void Main ( string [ ] args ) { a = a + 10 ; } } ");
        }

        [WorkItem(541597)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestComplexAssign2()
        {
            Test(
@"class Program { static void Main ( string [ ] args ) { a = [|a|] + 10 ; } } ",
@"class Program { private static int a ; static void Main ( string [ ] args ) { a = a + 10 ; } } ");
        }

        [WorkItem(541659)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestTypeNamedVar()
        {
            Test(
@"using System ; class Program { public static void Main ( ) { var v = [|p|] ; } } class var { } ",
@"using System ; class Program { private static var p ; public static void Main ( ) { var v = p ; } } class var { } ");
        }

        [WorkItem(541675)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestStaticExtensionMethodArgument()
        {
            Test(
@"using System ; class Program { static void Main ( string [ ] args ) { MyExtension . ExMethod ( [|ss|] ) ; } } static class MyExtension { public static int ExMethod ( this string s ) { return s . Length ; } } ",
@"using System ; class Program { private static string ss ; static void Main ( string [ ] args ) { MyExtension . ExMethod ( ss ) ; } } static class MyExtension { public static int ExMethod ( this string s ) { return s . Length ; } } ");
        }

        [WorkItem(539675)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void AddBlankLineBeforeCommentBetweenMembers1()
        {
            Test(
@"class Program
{
    //method
    static void Main(string[] args)
    {
        [|P|] = 10;
    }
}",
@"class Program
{
    public static int P { get; private set; }

    //method
    static void Main(string[] args)
    {
        P = 10;
    }
}",
compareTokens: false);
        }

        [WorkItem(539675)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void AddBlankLineBeforeCommentBetweenMembers2()
        {
            Test(
@"class Program
{
    //method
    static void Main(string[] args)
    {
        [|P|] = 10;
    }
}",
@"class Program
{
    private static int P;

    //method
    static void Main(string[] args)
    {
        P = 10;
    }
}",
index: 1,
compareTokens: false);
        }

        [WorkItem(543813)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void AddBlankLineBetweenMembers1()
        {
            Test(
@"class Program
{
    static void Main(string[] args)
    {
        [|P|] = 10;
    }
}",
@"class Program
{
    private static int P;

    static void Main(string[] args)
    {
        P = 10;
    }
}",
index: 1,
compareTokens: false);
        }

        [WorkItem(543813)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void AddBlankLineBetweenMembers2()
        {
            Test(
@"class Program
{
    static void Main(string[] args)
    {
        [|P|] = 10;
    }
}",
@"class Program
{
    public static int P { get; private set; }

    static void Main(string[] args)
    {
        P = 10;
    }
}",
index: 0,
compareTokens: false);
        }

        [WorkItem(543813)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void DontAddBlankLineBetweenFields()
        {
            Test(
@"class Program
{
    private static int P;

    static void Main(string[] args)
    {
        P = 10;
        [|A|] = 9;
    }
}",
@"class Program
{
    private static int A;
    private static int P;

    static void Main(string[] args)
    {
        P = 10;
        A = 9;
    }
}",
index: 1,
compareTokens: false);
        }

        [WorkItem(543813)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void DontAddBlankLineBetweenAutoProperties()
        {
            Test(
@"class Program
{
    public static int P { get; private set; }

    static void Main(string[] args)
    {
        P = 10;
        [|A|] = 9;
    }
}",
@"class Program
{
    public static int A { get; private set; }
    public static int P { get; private set; }

    static void Main(string[] args)
    {
        P = 10;
        A = 9;
    }
}",
index: 0,
compareTokens: false);
        }

        [WorkItem(539665)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestIntoEmptyClass()
        {
            Test(
@"class C { }
class D
{
    void M()
    {
        C.[|P|] = 10;
    }
}",
@"class C
{
    public static int P { get; internal set; }
}
class D
{
    void M()
    {
        C.P = 10;
    }
}",
compareTokens: false);
        }

        [WorkItem(540595)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGeneratePropertyInScript()
        {
            Test(
@"[|Foo|]",
@"object Foo { get; private set; }

Foo",
parseOptions: Options.Script,
compareTokens: false);
        }

        [WorkItem(542535)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestConstantInParameterValue()
        {
            const string Initial = @"class C { const int y = 1 ; public void Foo ( bool x = [|undeclared|] ) { } } ";

            TestActionCount(
Initial,
count: 1);

            Test(
Initial,
@"class C { private const bool undeclared ; const int y = 1 ; public void Foo ( bool x = undeclared ) { } } ");
        }

        [WorkItem(542900)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateFromAttributeNamedArgument1()
        {
            Test(
@"using System ; class ProgramAttribute : Attribute { [ Program ( [|Name|] = 0 ) ] static void Main ( string [ ] args ) { } } ",
@"using System ; class ProgramAttribute : Attribute { public int Name { get ; set ; } [ Program ( Name = 0 ) ] static void Main ( string [ ] args ) { } } ");
        }

        [WorkItem(542900)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateFromAttributeNamedArgument2()
        {
            Test(
@"using System ; class ProgramAttribute : Attribute { [ Program ( [|Name|] = 0 ) ] static void Main ( string [ ] args ) { } } ",
@"using System ; class ProgramAttribute : Attribute { public int Name ; [ Program ( Name = 0 ) ] static void Main ( string [ ] args ) { } } ",
index: 1);
        }

        [WorkItem(541698)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestMinimalAccessibility1_InternalPrivate()
        {
            Test(
@"class Program { public static void Main ( ) { C c = [|P|] ; } private class C { } } ",
@"class Program { private static C P { get ; set ; } public static void Main ( ) { C c = P ; } private class C { } } ",
parseOptions: null);
        }

        [WorkItem(541698)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestMinimalAccessibility2_InternalProtected()
        {
            Test(
@"class Program { public static void Main ( ) { C c = [|P|] ; } protected class C { } } ",
@"class Program { protected static C P { get ; private set ; } public static void Main ( ) { C c = P ; } protected class C { } } ",
parseOptions: null);
        }

        [WorkItem(541698)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestMinimalAccessibility3_InternalInternal()
        {
            Test(
@"class Program { public static void Main ( ) { C c = [|P|] ; } internal class C { } } ",
@"class Program { public static C P { get ; private set ; } public static void Main ( ) { C c = P ; } internal class C { } } ",
parseOptions: null);
        }

        [WorkItem(541698)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestMinimalAccessibility4_InternalProtectedInternal()
        {
            Test(
@"class Program { public static void Main ( ) { C c = [|P|] ; } protected internal class C { } } ",
@"class Program { public static C P { get ; private set ; } public static void Main ( ) { C c = P ; } protected internal class C { } } ",
parseOptions: null);
        }

        [WorkItem(541698)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestMinimalAccessibility5_InternalPublic()
        {
            Test(
@"class Program { public static void Main ( ) { C c = [|P|] ; } public class C { } } ",
@"class Program { public static C P { get ; private set ; } public static void Main ( ) { C c = P ; } public class C { } } ",
parseOptions: null);
        }

        [WorkItem(541698)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestMinimalAccessibility6_PublicInternal()
        {
            Test(
@"public class Program { public static void Main ( ) { C c = [|P|] ; } internal class C { } } ",
@"public class Program { internal static C P { get ; private set ; } public static void Main ( ) { C c = P ; } internal class C { } } ",
parseOptions: null);
        }

        [WorkItem(541698)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestMinimalAccessibility7_PublicProtectedInternal()
        {
            Test(
@"public class Program { public static void Main ( ) { C c = [|P|] ; } protected internal class C { } } ",
@"public class Program { protected internal static C P { get ; private set ; } public static void Main ( ) { C c = P ; } protected internal class C { } } ",
parseOptions: null);
        }

        [WorkItem(541698)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestMinimalAccessibility8_PublicProtected()
        {
            Test(
@"public class Program { public static void Main ( ) { C c = [|P|] ; } protected class C { } } ",
@"public class Program { protected static C P { get ; private set ; } public static void Main ( ) { C c = P ; } protected class C { } } ",
parseOptions: null);
        }

        [WorkItem(541698)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestMinimalAccessibility9_PublicPrivate()
        {
            Test(
@"public class Program { public static void Main ( ) { C c = [|P|] ; } private class C { } } ",
@"public class Program { private static C P { get ; set ; } public static void Main ( ) { C c = P ; } private class C { } } ",
parseOptions: null);
        }

        [WorkItem(541698)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestMinimalAccessibility10_PrivatePrivate()
        {
            Test(
@"class outer { private class Program { public static void Main ( ) { C c = [|P|] ; } private class C { } } }",
@"class outer { private class Program { public static C P { get ; private set ; } public static void Main ( ) { C c = P ; } private class C { } } }",
parseOptions: null);
        }

        [WorkItem(541698)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestMinimalAccessibility11_PrivateProtected()
        {
            Test(
@"class outer { private class Program { public static void Main ( ) { C c = [|P|] ; } protected class C { } } }",
@"class outer { private class Program { public static C P { get ; private set ; } public static void Main ( ) { C c = P ; } protected class C { } } }",
parseOptions: null);
        }

        [WorkItem(541698)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestMinimalAccessibility12_PrivateProtectedInternal()
        {
            Test(
@"class outer { private class Program { public static void Main ( ) { C c = [|P|] ; } protected internal class C { } } }",
@"class outer { private class Program { public static C P { get ; private set ; } public static void Main ( ) { C c = P ; } protected internal class C { } } }",
parseOptions: null);
        }

        [WorkItem(541698)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestMinimalAccessibility13_PrivateInternal()
        {
            Test(
@"class outer { private class Program { public static void Main ( ) { C c = [|P|] ; } internal class C { } } }",
@"class outer { private class Program { public static C P { get ; private set ; } public static void Main ( ) { C c = P ; } internal class C { } } }",
parseOptions: null);
        }

        [WorkItem(541698)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestMinimalAccessibility14_ProtectedPrivate()
        {
            Test(
@"class outer { protected class Program { public static void Main ( ) { C c = [|P|] ; } private class C { } } }",
@"class outer { protected class Program { private static C P { get ; set ; } public static void Main ( ) { C c = P ; } private class C { } } }",
parseOptions: null);
        }

        [WorkItem(541698)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestMinimalAccessibility15_ProtectedInternal()
        {
            Test(
@"class outer { protected class Program { public static void Main ( ) { C c = [|P|] ; } internal class C { } } }",
@"class outer { protected class Program { public static C P { get ; private set ; } public static void Main ( ) { C c = P ; } internal class C { } } }",
parseOptions: null);
        }

        [WorkItem(541698)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestMinimalAccessibility16_ProtectedInternalProtected()
        {
            Test(
@"class outer { protected internal class Program { public static void Main ( ) { C c = [|P|] ; } protected class C { } } }",
@"class outer { protected internal class Program { protected static C P { get ; private set ; } public static void Main ( ) { C c = P ; } protected class C { } } }",
parseOptions: null);
        }

        [WorkItem(541698)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestMinimalAccessibility17_ProtectedInternalInternal()
        {
            Test(
@"class outer { protected internal class Program { public static void Main ( ) { C c = [|P|] ; } internal class C { } } }",
@"class outer { protected internal class Program { public static C P { get ; private set ; } public static void Main ( ) { C c = P ; } internal class C { } } }",
parseOptions: null);
        }

        [WorkItem(543153)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestAnonymousObjectInitializer1()
        {
            Test(
@"class C { void M ( ) { var a = new { x = 5 } ; a = new { x = [|HERE|] } ; } } ",
@"class C { private int HERE ; void M ( ) { var a = new { x = 5 } ; a = new { x = HERE } ; } } ",
index: 1);
        }

        [WorkItem(543124)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestNoGenerationIntoAnonymousType()
        {
            TestMissing(
@"class Program { static void Main ( string [ ] args ) { var v = new { } ; bool b = v . [|Bar|] ; } } ");
        }

        [WorkItem(543543)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestNotOfferedForBoundParametersOfOperators()
        {
            TestMissing(
@"class Program { public Program ( string s ) { } static void Main ( string [ ] args ) { Program p = """" ; } public static implicit operator Program ( string str ) { return new Program ( [|str|] ) ; } } ");
        }

        [WorkItem(544175)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestNotOnNamedParameterName1()
        {
            TestMissing(
@"using System ; class class1 { public void Test ( ) { Foo ( [|x|] : x ) ; } public string Foo ( int x ) { } } ");
        }

        [WorkItem(544271)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestNotOnNamedParameterName2()
        {
            TestMissing(
@"class Foo { public Foo ( int a = 42 ) { } } class DogBed : Foo { public DogBed ( int b ) : base ( [|a|] : b ) { } } ");
        }

        [WorkItem(544164)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestPropertyOnObjectInitializer()
        {
            Test(
@"class Foo { } class Bar { void foo ( ) { var c = new Foo { [|Gibberish|] = 24 } ; } } ",
@"class Foo { public int Gibberish { get ; internal set ; } } class Bar { void foo ( ) { var c = new Foo { Gibberish = 24 } ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestPropertyOnObjectInitializer1()
        {
            Test(
@"class Foo { } class Bar { void foo ( ) { var c = new Foo { [|Gibberish|] = Gibberish } ; } } ",
@"class Foo { public object Gibberish { get ; internal set ; } } class Bar { void foo ( ) { var c = new Foo { Gibberish = Gibberish } ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestPropertyOnObjectInitializer2()
        {
            Test(
@"class Foo { } class Bar { void foo ( ) { var c = new Foo { Gibberish = [|Gibberish|] } ; } } ",
@"class Foo { } class Bar { public object Gibberish { get ; private set ; } void foo ( ) { var c = new Foo { Gibberish = Gibberish } ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestFieldOnObjectInitializer()
        {
            Test(
@"class Foo { } class Bar { void foo ( ) { var c = new Foo { [|Gibberish|] = 24 } ; } } ",
@"class Foo { internal int Gibberish ; } class Bar { void foo ( ) { var c = new Foo { Gibberish = 24 } ; } } ",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestFieldOnObjectInitializer1()
        {
            Test(
@"class Foo { } class Bar { void foo ( ) { var c = new Foo { [|Gibberish|] = Gibberish } ; } } ",
@"class Foo { internal object Gibberish ; } class Bar { void foo ( ) { var c = new Foo { Gibberish = Gibberish } ; } } ",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestFieldOnObjectInitializer2()
        {
            Test(
@"class Foo { } class Bar { void foo ( ) { var c = new Foo { Gibberish = [|Gibberish|] } ; } } ",
@"class Foo { } class Bar { private object Gibberish ; void foo ( ) { var c = new Foo { Gibberish = Gibberish } ; } } ",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestOnlyPropertyAndFieldOfferedForObjectInitializer()
        {
            TestActionCount(
@"class Foo { } class Bar { void foo ( ) { var c = new Foo { . [|Gibberish|] = 24 } ; } } ",
2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateLocalInObjectInitializerValue()
        {
            Test(
@"class Foo { } class Bar { void foo ( ) { var c = new Foo { Gibberish = [|blah|] } ; } } ",
@"class Foo { } class Bar { void foo ( ) { object blah = null ; var c = new Foo { Gibberish = blah } ; } } ",
index: 3);
        }

        [WorkItem(544319)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestNotOnIncompleteMember1()
        {
            TestMissing(
@"using System; class Class1 { Console.[|WriteLine|](); }");
        }

        [WorkItem(544319)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestNotOnIncompleteMember2()
        {
            TestMissing(
@"using System; class Class1 { [|WriteLine|](); }");
        }

        [WorkItem(544319)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestNotOnIncompleteMember3()
        {
            TestMissing(
@"using System; class Class1 { [|WriteLine|] }");
        }

        [WorkItem(544384)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestPointerType()
        {
            Test(
@"class Program { static int x ; unsafe static void F ( int * p ) { * p = 1 ; } static unsafe void Main ( string [ ] args ) { int [ ] a = new int [ 10 ] ; fixed ( int * p2 = & x , int * p3 = ) F ( GetP2 ( [|p2|] ) ) ; } unsafe private static int * GetP2 ( int * p2 ) { return p2 ; } } ",
@"class Program { static int x ; private static unsafe int * p2 ; unsafe static void F ( int * p ) { * p = 1 ; } static unsafe void Main ( string [ ] args ) { int [ ] a = new int [ 10 ] ; fixed ( int * p2 = & x , int * p3 = ) F ( GetP2 ( p2 ) ) ; } unsafe private static int * GetP2 ( int * p2 ) { return p2 ; } } ");
        }

        [WorkItem(544510)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestNotOnUsingAlias()
        {
            TestMissing(
@"using [|S|] = System ; S . Console . WriteLine ( ""hello world"" ) ; ");
        }

        [WorkItem(544907)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestExpressionTLambda()
        {
            Test(
@"using System ; using System . Linq . Expressions ; class C { static void Main ( ) { Expression < Func < int , int > > e = x => [|Foo|] ; } } ",
@"using System ; using System . Linq . Expressions ; class C { public static int Foo { get ; private set ; } static void Main ( ) { Expression < Func < int , int > > e = x => Foo ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestNoGenerationIntoEntirelyHiddenType()
        {
            TestMissing(
@"
class C
{
    void Foo()
    {
        int i = D.[|Bar|];
    }
}

#line hidden
class D
{
}
#line default
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInReturnStatement()
        {
            Test(
@"class Program { void Main ( ) { return [|foo|] ; } } ",
@"class Program { private object foo ; void Main ( ) { return foo ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestLocal1()
        {
            Test(
@"class Program { void Main ( ) { Foo ( [|bar|] ) ; } static void Foo ( int i ) { } } ",
@"class Program { void Main ( ) { int bar = 0 ; Foo ( bar ) ; } static void Foo ( int i ) { } } ",
index: 3);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestLocalMissingForVar()
        {
            TestMissing(
@"class Program { void Main ( ) { var x = [|var|] ; } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestOutLocal1()
        {
            Test(
@"class Program { void Main ( ) { Foo ( out [|bar|] ) ; } static void Foo ( out int i ) { } } ",
@"class Program { void Main ( ) { int bar ; Foo ( out bar ) ; } static void Foo ( out int i ) { } } ",
index: 1);
        }

        [WorkItem(809542)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestLocalBeforeComment()
        {
            Test(
@"class Program 
{ 
    void Main ()
    {
#if true
        // Banner Line 1
        // Banner Line 2
        int.TryParse(""123"", out [|local|]);
#endif
    }
}",
@"class Program 
{ 
    void Main ()
    {
#if true
        int local;
        // Banner Line 1
        // Banner Line 2
        int.TryParse(""123"", out [|local|]);
#endif
    }
}",
index: 1);
        }

        [WorkItem(809542)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestLocalAfterComment()
        {
            Test(
@"class Program 
{ 
    void Main ()
    {
#if true
        // Banner Line 1
        // Banner Line 2

        int.TryParse(""123"", out [|local|]);
#endif
    }
}",
@"class Program 
{ 
    void Main ()
    {
#if true
        // Banner Line 1
        // Banner Line 2
        int local;
        int.TryParse(""123"", out [|local|]);
#endif
    }
}",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateIntoVisiblePortion()
        {
            Test(
@"using System;

#line hidden
class Program
{
    void Main()
    {
#line default
        Foo(Program.[|X|])
    }
}",
@"using System;

#line hidden
class Program
{
    void Main()
    {
#line default
        Foo(Program.X)
    }

    public static object X { get; private set; }
}",
compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestMissingWhenNoAvailableRegionToGenerateInto()
        {
            TestMissing(
@"using System;

#line hidden
class Program
{
    void Main()
    {
#line default
        Foo(Program.[|X|])
#line hidden
    }
}
#line default");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateLocalAvailableIfBlockIsNotHidden()
        {
            Test(
@"using System;

#line hidden
class Program
{
#line default
    void Main()
    {
        Foo([|x|]);
    }
#line hidden
}
#line default",
@"using System;

#line hidden
class Program
{
#line default
    void Main()
    {
        object x = null;
        Foo(x);
    }
#line hidden
}
#line default",
compareTokens: false);
        }

        [WorkItem(545217)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateLocalNameSimplification()
        {
            Test(
@"class Program { void foo ( ) { bar ( [|xyz|] ) ; } struct sfoo { } void bar ( sfoo x ) { } } ",
@"class Program { void foo ( ) { sfoo xyz = default ( sfoo ) ; bar ( xyz ) ; } struct sfoo { } void bar ( sfoo x ) { } } ",
index: 3);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestParenthesizedExpression()
        {
            Test(
@"class Program { void Main ( ) { int v = 1 + ( [|k|] ) ; } } ",
@"class Program { private int k ; void Main ( ) { int v = 1 + ( k ) ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInSelect()
        {
            Test(
@"using System . Linq ; class Program { void Main ( string [ ] args ) { var q = from a in args select [|v|] ; } } ",
@"using System . Linq ; class Program { private object v ; void Main ( string [ ] args ) { var q = from a in args select v ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInChecked()
        {
            Test(
@"class Program { void Main ( ) { int [ ] a = null ; int [ ] temp = checked ( [|foo|] ) ; } } ",
@"class Program { private int [ ] foo ; void Main ( ) { int [ ] a = null ; int [ ] temp = checked ( foo ) ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInArrayRankSpecifier()
        {
            Test(
@"class Program { void Main ( ) { var v = new int [ [|k|] ] ; } } ",
@"class Program { private int k ; void Main ( ) { var v = new int [ k ] ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInConditional1()
        {
            Test(
@"class Program { static void Main ( ) { int i = [|foo|] ? bar : baz ; } } ",
@"class Program { private static bool foo ; static void Main ( ) { int i = foo ? bar : baz ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInConditional2()
        {
            Test(
@"class Program { static void Main ( ) { int i = foo ? [|bar|] : baz ; } } ",
@"class Program { private static int bar ; static void Main ( ) { int i = foo ? bar : baz ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInConditional3()
        {
            Test(
@"class Program { static void Main ( ) { int i = foo ? bar : [|baz|] ; } } ",
@"class Program { private static int baz ; static void Main ( ) { int i = foo ? bar : baz ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInCast()
        {
            Test(
@"class Program { void Main ( ) { var x = ( int ) [|y|] ; } } ",
@"class Program { private int y ; void Main ( ) { var x = ( int ) y ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInIf()
        {
            Test(
@"class Program { void Main ( ) { if ( [|foo|] ) { } } } ",
@"class Program { private bool foo ; void Main ( ) { if ( foo ) { } } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInSwitch()
        {
            Test(
@"class Program { void Main ( ) { switch ( [|foo|] ) { } } } ",
@"class Program { private int foo ; void Main ( ) { switch ( foo ) { } } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestMissingOnNamespace()
        {
            TestMissing(
@"class Program { void Main ( ) { [|System|] . Console . WriteLine ( 4 ) ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestMissingOnType()
        {
            TestMissing(
@"class Program { void Main ( ) { [|System . Console|] . WriteLine ( 4 ) ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestMissingOnBase()
        {
            TestMissing(
@"class Program { void Main ( ) { [|base|] . ToString ( ) ; } } ");
        }

        [WorkItem(545273)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateFromAssign1()
        {
            Test(
@"class Program { void Main ( ) { [|undefined|] = 1 ; } } ",
@"class Program { void Main ( ) { var undefined = 1 ; } } ",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestFuncAssignment()
        {
            Test(
@"class Program { void Main ( ) { [|undefined|] = ( x ) => 2 ; } } ",
@"class Program { void Main ( ) { System.Func < object , int > undefined =  ( x ) => 2 ; } } ",
index: 2);
        }

        [WorkItem(545273)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateFromAssign1NotAsVar()
        {
            Test(
@"class Program { void Main ( ) { [|undefined|] = 1 ; } } ",
@"class Program { void Main ( ) { int undefined = 1 ; } } ",
index: 2,
options: new Dictionary<OptionKey, object> { { new OptionKey(CSharpCodeStyleOptions.UseVarWhenDeclaringLocals), false } });
        }

        [WorkItem(545273)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateFromAssign2()
        {
            Test(
@"class Program { void Main ( ) { [|undefined|] = new { P = ""1"" } ; } } ",
@"class Program { void Main ( ) { var undefined = new { P = ""1"" } ; } } ",
index: 2);
        }

        [WorkItem(545269)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateInVenus1()
        {
            TestMissing(
@"
class C
{
#line 1 ""foo""
    void Foo()
    {
        this.[|Bar|] = 1;
    }
#line default
#line hidden
}
");
        }

        [WorkItem(545269)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateInVenus2()
        {
            var code = @"
class C
{
#line 1 ""foo""
    void Foo()
    {
        [|Bar|] = 1;
    }
#line default
#line hidden
}
";
            TestExactActionSetOffered(code, new[] { string.Format(FeaturesResources.GenerateLocal, "Bar") });

            Test(code,
@"
class C
{
#line 1 ""foo""
    void Foo()
    {
        var [|Bar|] = 1;
    }
#line default
#line hidden
}
");
        }

        [WorkItem(546027)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGeneratePropertyFromAttribute()
        {
            Test(
@"using System ; [ AttributeUsage ( AttributeTargets . Class ) ] class MyAttrAttribute : Attribute { } [ MyAttr ( 123 , [|Version|] = 1 ) ] class D { } ",
@"using System ; [ AttributeUsage ( AttributeTargets . Class ) ] class MyAttrAttribute : Attribute { public int Version { get ; set ; } } [ MyAttr ( 123 , Version = 1 ) ] class D { } ");
        }

        [WorkItem(545232)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestNewLinePreservationBeforeInsertingLocal()
        {
            Test(
@"using System;
namespace CSharpDemoApp
{
    class Program
    {
        static void Main(string[] args)
        {
            const int MEGABYTE = 1024 * 1024;
            Console.WriteLine(MEGABYTE);
 
            Calculate([|multiplier|]);
        }
        static void Calculate(double multiplier = Math.PI)
        {
        }
    }
}
",
@"using System;
namespace CSharpDemoApp
{
    class Program
    {
        static void Main(string[] args)
        {
            const int MEGABYTE = 1024 * 1024;
            Console.WriteLine(MEGABYTE);

            double multiplier = 0;
            Calculate(multiplier);
        }
        static void Calculate(double multiplier = Math.PI)
        {
        }
    }
}
",
index: 3,
compareTokens: false);
        }

        [WorkItem(863346)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateInGenericMethod_Local()
        {
            Test(
@"using System;
class TestClass<T1>
{
    static T TestMethod<T>(T item)
    {
        T t = WrapFunc<T>([|NewLocal|]);
        return t;
    }

    private static T WrapFunc<T>(Func<T1, T> function)
    {
        T1 zoo = default(T1);
        return function(zoo);
    }
}
",
@"using System;
class TestClass<T1>
{
    static T TestMethod<T>(T item)
    {
        Func<T1, T> NewLocal = null;
        T t = WrapFunc<T>(NewLocal);
        return t;
    }

    private static T WrapFunc<T>(Func<T1, T> function)
    {
        T1 zoo = default(T1);
        return function(zoo);
    }
}
",
index: 3,
compareTokens: false);
        }

        [WorkItem(863346)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateInGenericMethod_Property()
        {
            Test(
@"using System;
class TestClass<T1>
{
    static T TestMethod<T>(T item)
    {
        T t = WrapFunc<T>([|NewLocal|]);
        return t;
    }

    private static T WrapFunc<T>(Func<T1, T> function)
    {
        T1 zoo = default(T1);
        return function(zoo);
    }
}
",
@"using System;
class TestClass<T1>
{
    public static Func<T1, object> NewLocal { get; private set; }

    static T TestMethod<T>(T item)
    {
        T t = WrapFunc<T>(NewLocal);
        return t;
    }

    private static T WrapFunc<T>(Func<T1, T> function)
    {
        T1 zoo = default(T1);
        return function(zoo);
    }
}
",
compareTokens: false);
        }

        [WorkItem(865067)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestWithYieldReturn()
        {
            Test(
@"using System; using System.Collections.Generic; class Program { IEnumerable<DayOfWeek> Foo ( ) { yield return [|abc|]; } }",
@"using System; using System.Collections.Generic; class Program { private DayOfWeek abc; IEnumerable<DayOfWeek> Foo ( ) { yield return abc; } }");
        }

        [WorkItem(877580)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestWithThrow()
        {
            Test(
@"using System; class Program { void Foo ( ) { throw [|MyExp|]; } }",
@"using System; class Program { private Exception MyExp; void Foo ( ) { throw MyExp; } }", index: 1);
        }

        [WorkItem(530177)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestUnsafeField()
        {
            Test(
@"class Class { void Method() { [|int* a = foo|]; } }",
@"class Class { private unsafe int* foo; void Method() { int* a = foo; } }");
        }

        [WorkItem(530177)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestUnsafeField2()
        {
            Test(
@"class Class { void Method() { [|int*[] a = foo|]; } }",
@"class Class { private unsafe int*[] foo; void Method() { int*[] a = foo; } }");
        }

        [WorkItem(530177)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestUnsafeFieldInUnsafeClass()
        {
            Test(
@"unsafe class Class { void Method() { [|int* a = foo|]; } }",
@"unsafe class Class { private int* foo; void Method() { int* a = foo; } }");
        }

        [WorkItem(530177)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestUnsafeFieldInNestedClass()
        {
            Test(
@"unsafe class Class { class MyClass { void Method() { [|int* a = foo|]; } } }",
@"unsafe class Class { class MyClass { private int* foo; void Method() { int* a = foo; } } }");
        }

        [WorkItem(530177)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestUnsafeFieldInNestedClass2()
        {
            Test(
@"class Class { unsafe class MyClass { void Method() { [|int* a = Class.foo|]; } } }",
@"class Class { private static unsafe int* foo; unsafe class MyClass { void Method() { int* a = Class.foo; } } }");
        }

        [WorkItem(530177)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestUnsafeReadOnlyField()
        {
            Test(
@"class Class { void Method() { [|int* a = foo|]; } }",
@"class Class { private readonly unsafe int* foo; void Method() { int* a = foo; } }",
index: 1);
        }

        [WorkItem(530177)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestUnsafeReadOnlyField2()
        {
            Test(
@"class Class { void Method() { [|int*[] a = foo|]; } }",
@"class Class { private readonly unsafe int*[] foo; void Method() { int*[] a = foo; } }",
index: 1);
        }

        [WorkItem(530177)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestUnsafeReadOnlyFieldInUnsafeClass()
        {
            Test(
@"unsafe class Class { void Method() { [|int* a = foo|]; } }",
@"unsafe class Class { private readonly int* foo; void Method() { int* a = foo; } }",
index: 1);
        }

        [WorkItem(530177)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestUnsafeReadOnlyFieldInNestedClass()
        {
            Test(
@"unsafe class Class { class MyClass { void Method() { [|int* a = foo|]; } } }",
@"unsafe class Class { class MyClass { private readonly int* foo; void Method() { int* a = foo; } } }",
index: 1);
        }

        [WorkItem(530177)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestUnsafeReadOnlyFieldInNestedClass2()
        {
            Test(
@"class Class { unsafe class MyClass { void Method() { [|int* a = Class.foo|]; } } }",
@"class Class { private static readonly unsafe int* foo; unsafe class MyClass { void Method() { int* a = Class.foo; } } }",
index: 1);
        }

        [WorkItem(530177)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestUnsafeProperty()
        {
            Test(
@"class Class { void Method() { [|int* a = foo|]; } }",
@"class Class { public unsafe int* foo { get; private set; } void Method() { int* a = foo; } }",
index: 2);
        }

        [WorkItem(530177)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestUnsafeProperty2()
        {
            Test(
@"class Class { void Method() { [|int*[] a = foo|]; } }",
@"class Class { public unsafe int*[] foo { get; private set; } void Method() { int*[] a = foo; } }",
index: 2);
        }

        [WorkItem(530177)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestUnsafePropertyInUnsafeClass()
        {
            Test(
@"unsafe class Class { void Method() { [|int* a = foo|]; } }",
@"unsafe class Class { public int* foo { get; private set; } void Method() { int* a = foo; } }",
index: 2);
        }

        [WorkItem(530177)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestUnsafePropertyInNestedClass()
        {
            Test(
@"unsafe class Class { class MyClass { void Method() { [|int* a = foo|]; } } }",
@"unsafe class Class { class MyClass { public int* foo { get; private set; } void Method() { int* a = foo; } } }",
index: 2);
        }

        [WorkItem(530177)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestUnsafePropertyInNestedClass2()
        {
            Test(
@"class Class { unsafe class MyClass { void Method() { [|int* a = Class.foo|]; } } }",
@"class Class { public static unsafe int* foo { get; private set; } unsafe class MyClass { void Method() { int* a = Class.foo; } } }",
index: 2);
        }

        [WorkItem(1032176)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInsideNameOfProperty()
        {
            Test(
@"class C { void M() { var x = nameof([|Z|]); } }",
@"class C { public object Z { get; private set; } void M() { var x = nameof(Z); } }");
        }

        [WorkItem(1032176)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInsideNameOfField()
        {
            Test(
@"class C { void M() { var x = nameof([|Z|]); } }",
@"class C { private object Z; void M() { var x = nameof(Z); } }",
index: 1);
        }

        [WorkItem(1032176)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInsideNameOfReadonlyField()
        {
            Test(
@"class C { void M() { var x = nameof([|Z|]); } }",
@"class C { private readonly object Z; void M() { var x = nameof(Z); } }",
index: 2);
        }

        [WorkItem(1032176)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInsideNameOfLocal()
        {
            Test(
@"class C { void M() { var x = nameof([|Z|]); } }",
@"class C { void M() { object Z = null; var x = nameof(Z); } }",
index: 3);
        }

        [WorkItem(1032176)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInsideNameOfProperty2()
        {
            Test(
@"class C { void M() { var x = nameof([|Z.X|]); } }",
@"class C { public object Z { get; private set; } void M() { var x = nameof(Z.X); } }");
        }

        [WorkItem(1032176)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInsideNameOfField2()
        {
            Test(
@"class C { void M() { var x = nameof([|Z.X|]); } }",
@"class C { private object Z; void M() { var x = nameof(Z.X); } }",
index: 1);
        }

        [WorkItem(1032176)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInsideNameOfReadonlyField2()
        {
            Test(
@"class C { void M() { var x = nameof([|Z.X|]); } }",
@"class C { private readonly object Z; void M() { var x = nameof(Z.X); } }",
index: 2);
        }

        [WorkItem(1032176)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInsideNameOfLocal2()
        {
            Test(
@"class C { void M() { var x = nameof([|Z.X|]); } }",
@"class C { void M() { object Z = null; var x = nameof(Z.X); } }",
index: 3);
        }

        [WorkItem(1032176)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInsideNameOfProperty3()
        {
            Test(
@"class C { void M() { var x = nameof([|Z.X.Y|]); } }",
@"class C { public object Z { get; private set; } void M() { var x = nameof(Z.X.Y); } }");
        }

        [WorkItem(1032176)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInsideNameOfField3()
        {
            Test(
@"class C { void M() { var x = nameof([|Z.X.Y|]); } }",
@"class C { private object Z; void M() { var x = nameof(Z.X.Y); } }",
index: 1);
        }

        [WorkItem(1032176)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInsideNameOfReadonlyField3()
        {
            Test(
@"class C { void M() { var x = nameof([|Z.X.Y|]); } }",
@"class C { private readonly object Z; void M() { var x = nameof(Z.X.Y); } }",
index: 2);
        }

        [WorkItem(1032176)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInsideNameOfLocal3()
        {
            Test(
@"class C { void M() { var x = nameof([|Z.X.Y|]); } }",
@"class C { void M() { object Z = null; var x = nameof(Z.X.Y); } }",
index: 3);
        }

        [WorkItem(1032176)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInsideNameOfMissing()
        {
            TestMissing(@"class C { void M() { var x = [|nameof(1 + 2)|]; } }");
        }

        [WorkItem(1032176)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInsideNameOfMissing2()
        {
            TestMissing(@"class C { void M() { var y = 1 + 2; var x = [|nameof(y)|]; } }");
        }

        [WorkItem(1032176)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInsideNameOfMissing3()
        {
            TestMissing(@"class C { void M() { var y = 1 + 2; var z = """"; var x = [|nameof(y, z)|]; } }");
        }

        [WorkItem(1032176)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInsideNameOfProperty4()
        {
            Test(
@"class C { void M() { var x = nameof([|y|], z); } }",
@"class C { public object y { get; private set; } void M() { var x = nameof(y, z); } }",
index: 2);
        }

        [WorkItem(1032176)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInsideNameOfField4()
        {
            Test(
@"class C { void M() { var x = nameof([|y|], z); } }",
@"class C { private object y; void M() { var x = nameof(y, z); } }");
        }

        [WorkItem(1032176)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInsideNameOfReadonlyField4()
        {
            Test(
@"class C { void M() { var x = nameof([|y|], z); } }",
@"class C { private readonly object y; void M() { var x = nameof(y, z); } }",
index: 1);
        }

        [WorkItem(1032176)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInsideNameOfLocal4()
        {
            Test(
@"class C { void M() { var x = nameof([|y|], z); } }",
@"class C { void M() { object y = null; var x = nameof(y, z); } }",
index: 3);
        }

        [WorkItem(1032176)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInsideNameOfProperty5()
        {
            Test(
@"class C { void M() { var x = nameof([|y|]); } private object nameof(object y) { return null; } }",
@"class C { public object y { get; private set; } void M() { var x = nameof(y); } private object nameof(object y) { return null; } }",
index: 2);
        }

        [WorkItem(1032176)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInsideNameOfField5()
        {
            Test(
@"class C { void M() { var x = nameof([|y|]); } private object nameof(object y) { return null; } }",
@"class C { private object y; void M() { var x = nameof(y); } private object nameof(object y) { return null; } }");
        }

        [WorkItem(1032176)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInsideNameOfReadonlyField5()
        {
            Test(
@"class C { void M() { var x = nameof([|y|]); } private object nameof(object y) { return null; } }",
@"class C { private readonly object y; void M() { var x = nameof(y); } private object nameof(object y) { return null; } }",
index: 1);
        }

        [WorkItem(1032176)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestInsideNameOfLocal5()
        {
            Test(
@"class C { void M() { var x = nameof([|y|]); } private object nameof(object y) { return null; } }",
@"class C { void M() { object y = null; var x = nameof(y); } private object nameof(object y) { return null; } }",
index: 3);
        }

        [WorkItem(1064748)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestConditionalAccessProperty()
        {
            Test(
@"class C { void Main ( C a ) { C x = a ? [|. Instance|] ; } } ",
@"class C { public C Instance { get ; private set ; } void Main ( C a ) { C x = a ? . Instance ; } } ");
        }

        [WorkItem(1064748)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestConditionalAccessField()
        {
            Test(
@"class C { void Main ( C a ) { C x = a ? [|. Instance|] ; } } ",
@"class C { private C Instance ; void Main ( C a ) { C x = a ? . Instance ; } } ",
index: 1);
        }

        [WorkItem(1064748)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestConditionalAccessReadonlyField()
        {
            Test(
@"class C { void Main ( C a ) { C x = a ? [|. Instance|] ; } } ",
@"class C { private readonly C Instance ; void Main ( C a ) { C x = a ? . Instance ; } } ",
index: 2);
        }

        [WorkItem(1064748)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestConditionalAccessVarProperty()
        {
            Test(
@"class C { void Main ( C a ) { var x = a ? [|. Instance|] ; } } ",
@"class C { public object Instance { get ; private set ; } void Main ( C a ) { var x = a ? . Instance ; } } ");
        }

        [WorkItem(1064748)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestConditionalAccessVarField()
        {
            Test(
@"class C { void Main ( C a ) { var x = a ? [|. Instance|] ; } } ",
@"class C { private object Instance ; void Main ( C a ) { var x = a ? . Instance ; } } ",
index: 1);
        }

        [WorkItem(1064748)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestConditionalAccessVarReadOnlyField()
        {
            Test(
@"class C { void Main ( C a ) { var x = a ? [|. Instance|] ; } } ",
@"class C { private readonly object Instance ; void Main ( C a ) { var x = a ? . Instance ; } } ",
index: 2);
        }

        [WorkItem(1064748)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestConditionalAccessNullableProperty()
        {
            Test(
@"class C { void Main ( C a ) { int ? x = a ? [|. B|] ; } } ",
@"class C { public int B { get ; private set ; } void Main ( C a ) { int ? x = a ? . B ; } } ");
        }

        [WorkItem(1064748)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestConditionalAccessNullableField()
        {
            Test(
@"class C { void Main ( C a ) { int ? x = a ? [|. B|] ; } } ",
@"class C { private int B ; void Main ( C a ) { int ? x = a ? . B ; } } ",
index: 1);
        }

        [WorkItem(1064748)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestConditionalAccessNullableReadonlyField()
        {
            Test(
@"class C { void Main ( C a ) { int ? x = a ? [|. B|] ; } } ",
@"class C { private readonly int B ; void Main ( C a ) { int ? x = a ? . B ; } } ",
index: 2);
        }

        [WorkItem(1064748)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGeneratePropertyInConditionalAccessExpression()
        {
            Test(
@"class C { public E B { get ; private set ; } void Main ( C a ) { C x = a ? . B . [|C|] ; } public class E { } } ",
@"class C { public E B { get ; private set ; } void Main ( C a ) { C x = a ? . B . C ; } public class E { public C C { get ; internal set ; } } } ");
        }

        [WorkItem(1064748)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGeneratePropertyInConditionalAccessExpression2()
        {
            Test(
@"class C { public E B { get ; private set ; } void Main ( C a ) { int x = a ? . B . [|C|] ; } public class E { } } ",
@"class C { public E B { get ; private set ; } void Main ( C a ) { int x = a ? . B . C ; } public class E { public int C { get ; internal set ; } } } ");
        }

        [WorkItem(1064748)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGeneratePropertyInConditionalAccessExpression3()
        {
            Test(
@"class C { public E B { get ; private set ; } void Main ( C a ) { int ? x = a ? . B . [|C|] ; } public class E { } } ",
@"class C { public E B { get ; private set ; } void Main ( C a ) { int ? x = a ? . B . C ; } public class E { public int C { get ; internal set ; } } } ");
        }

        [WorkItem(1064748)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGeneratePropertyInConditionalAccessExpression4()
        {
            Test(
@"class C { public E B { get ; private set ; } void Main ( C a ) { var x = a ? . B . [|C|] ; } public class E { } } ",
@"class C { public E B { get ; private set ; } void Main ( C a ) { var x = a ? . B . C ; } public class E { public object C { get ; internal set ; } } } ");
        }

        [WorkItem(1064748)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateFieldInConditionalAccessExpression()
        {
            Test(
@"class C { public E B { get ; private set ; } void Main ( C a ) { C x = a ? . B . [|C|] ; } public class E { } } ",
@"class C { public E B { get ; private set ; } void Main ( C a ) { C x = a ? . B . C ; } public class E { internal C C ; } } ",
index: 1);
        }

        [WorkItem(1064748)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateFieldInConditionalAccessExpression2()
        {
            Test(
@"class C { public E B { get ; private set ; } void Main ( C a ) { int x = a ? . B . [|C|] ; } public class E { } } ",
@"class C { public E B { get ; private set ; } void Main ( C a ) { int x = a ? . B . C ; } public class E { internal int C ; } } ",
index: 1);
        }

        [WorkItem(1064748)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateFieldInConditionalAccessExpression3()
        {
            Test(
@"class C { public E B { get ; private set ; } void Main ( C a ) { int ? x = a ? . B . [|C|] ; } public class E { } } ",
@"class C { public E B { get ; private set ; } void Main ( C a ) { int ? x = a ? . B . C ; } public class E { internal int C ; } } ",
index: 1);
        }

        [WorkItem(1064748)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateFieldInConditionalAccessExpression4()
        {
            Test(
@"class C { public E B { get ; private set ; } void Main ( C a ) { var x = a ? . B . [|C|] ; } public class E { } } ",
@"class C { public E B { get ; private set ; } void Main ( C a ) { var x = a ? . B . C ; } public class E { internal object C ; } } ",
index: 1);
        }

        [WorkItem(1064748)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateReadonlyFieldInConditionalAccessExpression()
        {
            Test(
@"class C { public E B { get ; private set ; } void Main ( C a ) { C x = a ? . B . [|C|] ; } public class E { } } ",
@"class C { public E B { get ; private set ; } void Main ( C a ) { C x = a ? . B . C ; } public class E { internal readonly C C ; } } ",
index: 2);
        }

        [WorkItem(1064748)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateReadonlyFieldInConditionalAccessExpression2()
        {
            Test(
@"class C { public E B { get ; private set ; } void Main ( C a ) { int x = a ? . B . [|C|] ; } public class E { } } ",
@"class C { public E B { get ; private set ; } void Main ( C a ) { int x = a ? . B . C ; } public class E { internal readonly int C ; } } ",
index: 2);
        }

        [WorkItem(1064748)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateReadonlyFieldInConditionalAccessExpression3()
        {
            Test(
@"class C { public E B { get ; private set ; } void Main ( C a ) { int ? x = a ? . B . [|C|] ; } public class E { } } ",
@"class C { public E B { get ; private set ; } void Main ( C a ) { int ? x = a ? . B . C ; } public class E { internal readonly int C ; } } ",
index: 2);
        }

        [WorkItem(1064748)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateReadonlyFieldInConditionalAccessExpression4()
        {
            Test(
@"class C { public E B { get ; private set ; } void Main ( C a ) { var x = a ? . B . [|C|] ; } public class E { } } ",
@"class C { public E B { get ; private set ; } void Main ( C a ) { var x = a ? . B . C ; } public class E { internal readonly object C ; } } ",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateFieldInPropertyInitializers()
        {
            Test(
@"using System ; using System . Collections . Generic ; using System . Linq ; using System . Threading . Tasks ; class Program { public int MyProperty { get ; } = [|y|] ; } ",
@"using System ; using System . Collections . Generic ; using System . Linq ; using System . Threading . Tasks ; class Program { private static int y ; public int MyProperty { get ; } = y ; } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateReadonlyFieldInPropertyInitializers()
        {
            Test(
@"using System ; using System . Collections . Generic ; using System . Linq ; using System . Threading . Tasks ; class Program { public int MyProperty { get ; } = [|y|] ; } ",
@"using System ; using System . Collections . Generic ; using System . Linq ; using System . Threading . Tasks ; class Program { private static readonly int y ; public int MyProperty { get ; } = y ; } ",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGeneratePropertyInPropertyInitializers()
        {
            Test(
@"using System ; using System . Collections . Generic ; using System . Linq ; using System . Threading . Tasks ; class Program { public int MyProperty { get ; } = [|y|] ; } ",
@"using System ; using System . Collections . Generic ; using System . Linq ; using System . Threading . Tasks ; class Program { public static int y { get ; private set ; } public int MyProperty { get ; } = y ;  } ",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateFieldInExpressionBodyMember()
        {
            Test(
@"class Program { public int Y => [|y|] ; } ",
@"class Program { private int y ; public int Y => y ; } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateReadonlyFieldInExpressionBodyMember()
        {
            Test(
@"class Program { public int Y => [|y|] ; } ",
@"class Program { private readonly int y ; public int Y => y ; } ",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGeneratePropertyInExpressionBodyMember()
        {
            Test(
@"class Program { public int Y => [|y|] ; } ",
@"class Program { public int y { get; private set; } public int Y => y ; } ",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateFieldInExpressionBodyMember2()
        {
            Test(
@"class C { public static C operator -- ( C p ) => [|x|] ; } ",
@"class C { private static C x ; public static C operator -- ( C p ) => x ; } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateReadOnlyFieldInExpressionBodyMember2()
        {
            Test(
@"class C { public static C operator -- ( C p ) => [|x|] ; } ",
@"class C { private static readonly C x ; public static C operator -- ( C p ) => x ; } ",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGeneratePropertyInExpressionBodyMember2()
        {
            Test(
@"class C { public static C operator -- ( C p ) => [|x|] ; } ",
@"class C { public static C x { get ; private set ; } public static C operator -- ( C p ) => x ; } ",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateFieldInExpressionBodyMember3()
        {
            Test(
@"class C { public static C GetValue ( C p ) => [|x|] ; } ",
@"class C { private static C x ; public static C GetValue ( C p ) => x ; } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateReadOnlyFieldInExpressionBodyMember3()
        {
            Test(
@"class C { public static C GetValue ( C p ) => [|x|] ; } ",
@"class C { private static readonly C x ; public static C GetValue ( C p ) => x ; } ",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGeneratePropertyInExpressionBodyMember3()
        {
            Test(
@"class C { public static C GetValue ( C p ) => [|x|] ; } ",
@"class C { public static C x { get ; private set ; } public static C GetValue ( C p ) => x ; } ",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateFieldInDictionaryInitializer()
        {
            Test(
@"using System . Collections . Generic ; class Program { static void Main ( string [ ] args ) { var x = new Dictionary < string , int > { [ [|key|] ] = 0 } ; } } ",
@"using System . Collections . Generic ; class Program { private static string key ; static void Main ( string [ ] args ) { var x = new Dictionary < string , int > { [ key ] = 0 } ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGeneratePropertyInDictionaryInitializer()
        {
            Test(
@"using System . Collections . Generic ; class Program { static void Main ( string [ ] args ) { var x = new Dictionary < string , int > { [ ""Zero"" ] = 0 , [ [|One|] ] = 1 , [ ""Two"" ] = 2 } ; } } ",
@"using System . Collections . Generic ; class Program { public static string One { get ; private set ; } static void Main ( string [ ] args ) { var x = new Dictionary < string , int > { [ ""Zero"" ] = 0 , [ One ] = 1 , [ ""Two"" ] = 2 } ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateFieldInDictionaryInitializer2()
        {
            Test(
@"using System . Collections . Generic ; class Program { static void Main ( string [ ] args ) { var x = new Dictionary < string , int > { [ ""Zero"" ] = [|i|] } ; } } ",
@"using System . Collections . Generic ; class Program { private static int i ; static void Main ( string [ ] args ) { var x = new Dictionary < string , int > { [ ""Zero"" ] = i } ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateReadOnlyFieldInDictionaryInitializer()
        {
            Test(
@"using System . Collections . Generic ; class Program { static void Main ( string [ ] args ) { var x = new Dictionary < string , int > { [ [|key|] ] = 0 } ; } } ",
@"using System . Collections . Generic ; class Program { private static readonly string key ; static void Main ( string [ ] args ) { var x = new Dictionary < string , int > { [ key ] = 0 } ; } } ",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateFieldInDictionaryInitializer3()
        {
            Test(
@"using System . Collections . Generic ; class Program { static void Main ( string [ ] args ) { var x = new Dictionary < string , int > { [ ""Zero"" ] = 0 , [ [|One|] ] = 1 , [ ""Two"" ] = 2 } ; } } ",
@"using System . Collections . Generic ; class Program { private static string One ; static void Main ( string [ ] args ) { var x = new Dictionary < string , int > { [ ""Zero"" ] = 0 , [ One ] = 1 , [ ""Two"" ] = 2 } ; } } ",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateReadOnlyFieldInDictionaryInitializer2()
        {
            Test(
@"using System . Collections . Generic ; class Program { static void Main ( string [ ] args ) { var x = new Dictionary < string , int > { [ ""Zero"" ] = [|i|] } ; } } ",
@"using System . Collections . Generic ; class Program { private static readonly int i ; static void Main ( string [ ] args ) { var x = new Dictionary < string , int > { [ ""Zero"" ] = i } ; } } ",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGeneratePropertyInDictionaryInitializer2()
        {
            Test(
@"using System . Collections . Generic ; class Program { static void Main ( string [ ] args ) { var x = new Dictionary < string , int > { [ [|key|] ] = 0 } ; } } ",
@"using System . Collections . Generic ; class Program { public static string key { get ; private set ; } static void Main ( string [ ] args ) { var x = new Dictionary < string , int > { [ key ] = 0 } ; } } ",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateReadOnlyFieldInDictionaryInitializer3()
        {
            Test(
@"using System . Collections . Generic ; class Program { static void Main ( string [ ] args ) { var x = new Dictionary < string , int > { [ ""Zero"" ] = 0 , [ [|One|] ] = 1 , [ ""Two"" ] = 2 } ; } } ",
@"using System . Collections . Generic ; class Program { private static readonly string One ; static void Main ( string [ ] args ) { var x = new Dictionary < string , int > { [ ""Zero"" ] = 0 , [ One ] = 1 , [ ""Two"" ] = 2 } ; } } ",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGeneratePropertyInDictionaryInitializer3()
        {
            Test(
@"using System . Collections . Generic ; class Program { static void Main ( string [ ] args ) { var x = new Dictionary < string , int > { [ ""Zero"" ] = [|i|] } ; } } ",
@"using System . Collections . Generic ; class Program { public static int i { get ; private set ; } static void Main ( string [ ] args ) { var x = new Dictionary < string , int > { [ ""Zero"" ] = i } ; } } ",
index: 2);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateLocalInDictionaryInitializer()
        {
            Test(
@"using System . Collections . Generic ; class Program { static void Main ( string [ ] args ) { var x = new Dictionary < string , int > { [ [|key|] ] = 0 } ; } } ",
@"using System . Collections . Generic ; class Program { static void Main ( string [ ] args ) { string key = null ; var x = new Dictionary < string , int > { [ key ] = 0 } ; } } ",
index: 3);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateLocalInDictionaryInitializer2()
        {
            Test(
@"using System . Collections . Generic ; class Program { static void Main ( string [ ] args ) { var x = new Dictionary < string , int > { [ ""Zero"" ] = 0 , [ [|One|] ] = 1 , [ ""Two"" ] = 2 } ; } } ",
@"using System . Collections . Generic ; class Program { static void Main ( string [ ] args ) { string One = null ; var x = new Dictionary < string , int > { [ ""Zero"" ] = 0 , [ One ] = 1 , [ ""Two"" ] = 2 } ; } } ",
index: 3);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateLocalInDictionaryInitializer3()
        {
            Test(
@"using System . Collections . Generic ; class Program { static void Main ( string [ ] args ) { var x = new Dictionary < string , int > { [ ""Zero"" ] = [|i|] } ; } } ",
@"using System . Collections . Generic ; class Program { static void Main ( string [ ] args ) { int i = 0 ; var x = new Dictionary < string , int > { [ ""Zero"" ] = i } ; } } ",
index: 3);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateVariableFromLambda()
        {
            Test(
@"using System ; class Program { static void Main ( string [ ] args ) { [|foo|] = ( ) => { return 0 ; } ; } } ",
@"using System ; class Program { private static Func < int > foo ; static void Main ( string [ ] args ) { foo = ( ) => { return 0 ; } ; } } ");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateVariableFromLambda2()
        {
            Test(
@"using System ; class Program { static void Main ( string [ ] args ) { [|foo|] = ( ) => { return 0 ; } ; } } ",
@"using System ; class Program { public static Func < int > foo { get ; private set ; } static void Main ( string [ ] args ) { foo = ( ) => { return 0 ; } ; } } ",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)]
        public void TestGenerateVariableFromLambda3()
        {
            Test(
@"using System ; class Program { static void Main ( string [ ] args ) { [|foo|] = ( ) => { return 0 ; } ; } } ",
@"using System ; class Program { static void Main ( string [ ] args ) { Func < int > foo = ( ) => { return 0 ; } ; } } ",
index: 2);
        }
    }
}
