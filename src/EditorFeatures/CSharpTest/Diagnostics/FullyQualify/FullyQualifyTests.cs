// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.FullyQualify;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.FullyQualify
{
    public class FullyQualifyTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return Tuple.Create<DiagnosticAnalyzer, CodeFixProvider>(null, new CSharpFullyQualifyCodeFixProvider());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestTypeFromMultipleNamespaces1()
        {
            Test(
@"class Class { [|IDictionary|] Method() { Foo(); } }",
@"class Class { System.Collections.IDictionary Method() { Foo(); } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestTypeFromMultipleNamespaces2()
        {
            Test(
@"class Class { [|IDictionary|] Method() { Foo(); } }",
@"class Class { System.Collections.Generic.IDictionary Method() { Foo(); } }",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestGenericWithNoArgs()
        {
            Test(
@"class Class { [|List|] Method() { Foo(); } }",
@"class Class { System.Collections.Generic.List Method() { Foo(); } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestGenericWithCorrectArgs()
        {
            Test(
@"class Class { [|List<int>|] Method() { Foo(); } }",
@"class Class { System.Collections.Generic.List<int> Method() { Foo(); } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestSmartTagDisplayText()
        {
            TestSmartTagText(
@"class Class { [|List<int>|] Method() { Foo(); } }",
"System.Collections.Generic.List");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestGenericWithWrongArgs()
        {
            TestMissing(
@"class Class { [|List<int,string>|] Method() { Foo(); } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestGenericInLocalDeclaration()
        {
            Test(
@"class Class { void Foo() { [|List<int>|] a = new List<int>(); } }",
@"class Class { void Foo() { System.Collections.Generic.List<int> a = new List<int>(); } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestGenericItemType()
        {
            Test(
@"using System.Collections.Generic; class Class { List<[|Int32|]> l; }",
@"using System.Collections.Generic; class Class { List<System.Int32> l; }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestGenerateWithExistingUsings()
        {
            Test(
@"using System; class Class { [|List<int>|] Method() { Foo(); } }",
@"using System; class Class { System.Collections.Generic.List<int> Method() { Foo(); } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestGenerateInNamespace()
        {
            Test(
@"namespace N { class Class { [|List<int>|] Method() { Foo(); } } }",
@"namespace N { class Class { System.Collections.Generic.List<int> Method() { Foo(); } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestGenerateInNamespaceWithUsings()
        {
            Test(
@"namespace N { using System; class Class { [|List<int>|] Method() { Foo(); } } }",
@"namespace N { using System; class Class { System.Collections.Generic.List<int> Method() { Foo(); } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestExistingUsing()
        {
            TestActionCount(
@"using System.Collections.Generic; class Class { [|IDictionary|] Method() { Foo(); } }",
count: 2);

            Test(
@"using System.Collections.Generic; class Class { [|IDictionary|] Method() { Foo(); } }",
@"using System.Collections.Generic; class Class { System.Collections.IDictionary Method() { Foo(); } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestMissingIfUniquelyBound()
        {
            TestMissing(
@"using System; class Class { [|String|] Method() { Foo(); } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestMissingIfUniquelyBoundGeneric()
        {
            TestMissing(
@"using System.Collections.Generic; class Class { [|List<int>|] Method() { Foo(); } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestOnEnum()
        {
            Test(
@"class Class { void Foo() { var a = [|Colors|].Red; } } namespace A { enum Colors {Red, Green, Blue} }",
@"class Class { void Foo() { var a = A.Colors.Red; } } namespace A { enum Colors {Red, Green, Blue} }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestOnClassInheritance()
        {
            Test(
@"class Class : [|Class2|] { } namespace A { class Class2 { } }",
@"class Class : A.Class2 { } namespace A { class Class2 { } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestOnImplementedInterface()
        {
            Test(
@"class Class : [|IFoo|] { } namespace A { interface IFoo { } }",
@"class Class : A.IFoo { } namespace A { interface IFoo { } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestAllInBaseList()
        {
            Test(
@"class Class : [|IFoo|], Class2 { } namespace A { class Class2 { } } namespace B { interface IFoo { } } ",
@"class Class : B.IFoo, Class2 { } namespace A { class Class2 { } } namespace B { interface IFoo { } }");

            Test(
@"class Class : B.IFoo, [|Class2|] { } namespace A { class Class2 { } } namespace B { interface IFoo { } } ",
@"class Class : B.IFoo, A.Class2 { } namespace A { class Class2 { } } namespace B { interface IFoo { } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestAttributeUnexpanded()
        {
            Test(
@"[[|Obsolete|]]class Class { }",
@"[System.Obsolete]class Class { }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestAttributeExpanded()
        {
            Test(
@"[[|ObsoleteAttribute|]]class Class { }",
@"[System.ObsoleteAttribute]class Class { }");
        }

        [WorkItem(527360)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestExtensionMethods()
        {
            TestMissing(
@"using System.Collections.Generic; class Foo { void Bar() { var values = new List<int>() { 1, 2, 3 }; values.[|Where|](i => i > 1); } }");
        }

        [WorkItem(538018)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestAfterNew()
        {
            Test(
@"class Class { void Foo() { List<int> l; l = new [|List<int>|](); } }",
@"class Class { void Foo() { List<int> l; l = new System.Collections.Generic.List<int>(); } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestArgumentsInMethodCall()
        {
            Test(
@"class Class { void Test() { Console.WriteLine([|DateTime|].Today); } }",
@"class Class { void Test() { Console.WriteLine(System.DateTime.Today); } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestCallSiteArgs()
        {
            Test(
@"class Class { void Test([|DateTime|] dt) { } }",
@"class Class { void Test(System.DateTime dt) { } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestUsePartialClass()
        {
            Test(
@"namespace A { public class Class { [|PClass|] c; } } namespace B{ public partial class PClass { } }",
@"namespace A { public class Class { B.PClass c; } } namespace B{ public partial class PClass { } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestGenericClassInNestedNamespace()
        {
            Test(
@"namespace A { namespace B { class GenericClass<T> { } } } namespace C { class Class { [|GenericClass<int>|] c; } }",
@"namespace A { namespace B { class GenericClass<T> { } } } namespace C { class Class { A.B.GenericClass<int> c; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestBeforeStaticMethod()
        {
            Test(
@"class Class { void Test() { [|Math|].Sqrt(); }",
@"class Class { void Test() { System.Math.Sqrt(); }");
        }

        [WorkItem(538136)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestBeforeNamespace()
        {
            Test(
@"namespace A { class Class { [|C|].Test t; } } namespace B { namespace C { class Test { } } }",
@"namespace A { class Class { B.C.Test t; } } namespace B { namespace C { class Test { } } }");
        }

        [WorkItem(527395)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestSimpleNameWithLeadingTrivia()
        {
            Test(
@"class Class { void Test() { /*foo*/[|Int32|] i; } }",
@"class Class { void Test() { /*foo*/System.Int32 i; } }",
compareTokens: false);
        }

        [WorkItem(527395)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestGenericNameWithLeadingTrivia()
        {
            Test(
@"class Class { void Test() { /*foo*/[|List<int>|] l; } }",
@"class Class { void Test() { /*foo*/System.Collections.Generic.List<int> l; } }",
compareTokens: false);
        }

        [WorkItem(538740)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestFullyQualifyTypeName()
        {
            Test(
@"public class Program { public class Inner { } } class Test { [|Inner|] i; }",
@"public class Program { public class Inner { } } class Test { Program.Inner i; }");
        }

        [WorkItem(538740)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestFullyQualifyTypeName_NotForGenericType()
        {
            TestMissing(
@"class Program<T> { public class Inner { } } class Test { [|Inner|] i; }");
        }

        [WorkItem(538764)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestFullyQualifyThroughAlias()
        {
            Test(
@"using Alias = System; class C { [|Int32|] i; }",
@"using Alias = System; class C { Alias.Int32 i; }");
        }

        [WorkItem(538763)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestFullyQualifyPrioritizeTypesOverNamespaces1()
        {
            Test(
@"namespace Outer { namespace C { class C { } } } class Test { [|C|] c; }",
@"namespace Outer { namespace C { class C { } } } class Test { Outer.C.C c; }");
        }

        [WorkItem(538763)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestFullyQualifyPrioritizeTypesOverNamespaces2()
        {
            Test(
@"namespace Outer { namespace C { class C { } } } class Test { [|C|] c; }",
@"namespace Outer { namespace C { class C { } } } class Test { Outer.C c; }",
index: 1);
        }

        [WorkItem(539853)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void BugFix5950()
        {
            Test(
@"using System.Console; WriteLine([|Expression|].Constant(123));",
@"using System.Console; WriteLine(System.Linq.Expressions.Expression.Constant(123));",
parseOptions: GetScriptOptions());
        }

        [WorkItem(540318)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestAfterAlias()
        {
            TestMissing(
@"using System ; using System . Collections . Generic ; using System . Linq ; class Program { static void Main ( string [ ] args ) { System :: [|Console|] :: WriteLine ( ""TEST"" ) ; } } ");
        }

        [WorkItem(540942)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestMissingOnIncompleteStatement()
        {
            TestMissing(
@"using System ; using System . IO ; class C { static void Main ( string [ ] args ) { [|Path|] } } ");
        }

        [WorkItem(542643)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestAssemblyAttribute()
        {
            Test(
@"[ assembly : [|InternalsVisibleTo|] ( ""Project"" ) ] ",
@"[ assembly : System . Runtime . CompilerServices . InternalsVisibleTo ( ""Project"" ) ] ");
        }

        [WorkItem(543388)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestMissingOnAliasName()
        {
            TestMissing(
@"using [|GIBBERISH|] = Foo . GIBBERISH ; class Program { static void Main ( string [ ] args ) { GIBBERISH x ; } } namespace Foo { public class GIBBERISH { } } ");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestMissingOnAttributeOverloadResolutionError()
        {
            TestMissing(
@"using System . Runtime . InteropServices ; class M { [ [|DllImport|] ( ) ] static extern int ? My ( ) ; } ");
        }

        [WorkItem(544950)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestNotOnAbstractConstructor()
        {
            TestMissing(
@"using System . IO ; class Program { static void Main ( string [ ] args ) { var s = new [|Stream|] ( ) ; } } ");
        }

        [WorkItem(545774)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestAttribute()
        {
            var input = @"[ assembly : [|Guid|] ( ""9ed54f84-a89d-4fcd-a854-44251e925f09"" ) ] ";
            TestActionCount(input, 1);

            Test(
input,
@"[ assembly : System . Runtime . InteropServices . Guid ( ""9ed54f84-a89d-4fcd-a854-44251e925f09"" ) ] ");
        }

        [WorkItem(546027)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void TestGeneratePropertyFromAttribute()
        {
            TestMissing(
@"using System ; [ AttributeUsage ( AttributeTargets . Class ) ] class MyAttrAttribute : Attribute { } [ MyAttr ( 123 , [|Version|] = 1 ) ] class D { } ");
        }

        [WorkItem(775448)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void ShouldTriggerOnCS0308()
        {
            // CS0308: The non-generic type 'A' cannot be used with type arguments
            Test(
@"using System.Collections;

class Test
{
    static void Main(string[] args)
    {
        [|IEnumerable<int>|] f;
    }
}",
@"using System.Collections;

class Test
{
    static void Main(string[] args)
    {
        System.Collections.Generic.IEnumerable<int> f;
    }
}");
        }

        [WorkItem(947579)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void AmbiguousTypeFix()
        {
            Test(
@"using n1;
using n2;

class B { void M1() { [|var a = new A();|] }}

namespace n1 { class A { }}
namespace n2 { class A { }}",
@"using n1;
using n2;

class B { void M1() { var a = new n1.A(); }}

namespace n1 { class A { }}
namespace n2 { class A { }}");
        }

        [WorkItem(995857)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsFullyQualify)]
        public void NonPublicNamespaces()
        {
            Test(
@"namespace MS.Internal.Xaml { private class A { } }
namespace System.Xaml { public class A { } }
public class Program { static void M() { [|Xaml|] } }",
@"namespace MS.Internal.Xaml { private class A { } }
namespace System.Xaml { public class A { } }
public class Program { static void M() { System.Xaml } }");

            Test(
@"namespace MS.Internal.Xaml { public class A { } }
namespace System.Xaml { public class A { } }
public class Program { static void M() { [|Xaml|] } }",
@"namespace MS.Internal.Xaml { public class A { } }
namespace System.Xaml { public class A { } }
public class Program { static void M() { MS.Internal.Xaml } }");
        }
    }
}
