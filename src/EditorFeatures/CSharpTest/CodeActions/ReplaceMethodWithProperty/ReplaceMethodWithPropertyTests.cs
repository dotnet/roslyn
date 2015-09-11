using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ReplaceMethodWithProperty;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.ReplaceMethodWithProperty
{
    public class ReplaceMethodWithPropertyTests : AbstractCSharpCodeActionTest
    {
        protected override object CreateCodeRefactoringProvider(Workspace workspace)
        {
            return new ReplaceMethodWithPropertyCodeRefactoringProvider();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public void TestMethodWithGetName()
        {
            Test(
@"class C { int [||]GetFoo() { } }",
@"class C { int Foo { get { } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public void TestMethodWithoutGetName()
        {
            Test(
@"class C { int [||]Foo() { } }",
@"class C { int Foo { get { } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public void TestMethodWithArrowBody()
        {
            Test(
@"class C { int [||]GetFoo() => 0; }",
@"class C { int Foo { get; } => 0; }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public void TestMethodWithoutBody()
        {
            Test(
@"class C { int [||]GetFoo(); }",
@"class C { int Foo { get; } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public void TestMethodWithModifiers()
        {
            Test(
@"class C { public static int [||]GetFoo() { } }",
@"class C { public static int Foo { get { } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public void TestMethodWithAttributes()
        {
            Test(
@"class C { [A]int [||]GetFoo() { } }",
@"class C { [A]int Foo { get { } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public void TestExplicitInterfaceMethod()
        {
            Test(
@"class C { int [||]I.GetFoo() { } }",
@"class C { int I.Foo { get { } } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public void TestVoidMethod()
        {
            TestMissing(
@"class C { void [||]GetFoo() { } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public void TestMethodWithParameters_1()
        {
            TestMissing(
@"class C { int [||]GetFoo(int i) { } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public void TestMethodWithParameters_2()
        {
            TestMissing(
@"class C { int [||]GetFoo(int i = 0) { } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public void TestNotInSignature_1()
        {
            TestMissing(
@"class C { [At[||]tr]int GetFoo() { } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public void TestNotInSignature_2()
        {
            TestMissing(
@"class C { int GetFoo() { [||] } }");
        }
    }
}
