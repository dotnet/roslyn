// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.ReplaceMethodWithProperty;
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestMethodWithGetName()
        {
            await TestAsync(
@"class C { int [||]GetFoo() { } }",
@"class C { int Foo { get { } } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestMethodWithoutGetName()
        {
            await TestAsync(
@"class C { int [||]Foo() { } }",
@"class C { int Foo { get { } } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        [WorkItem(6034, "https://github.com/dotnet/roslyn/issues/6034")]
        public async Task TestMethodWithArrowBody()
        {
            await TestAsync(
@"class C { int [||]GetFoo() => 0; }",
@"class C { int Foo => 0; }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestMethodWithoutBody()
        {
            await TestAsync(
@"class C { int [||]GetFoo(); }",
@"class C { int Foo { get; } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestMethodWithModifiers()
        {
            await TestAsync(
@"class C { public static int [||]GetFoo() { } }",
@"class C { public static int Foo { get { } } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestMethodWithAttributes()
        {
            await TestAsync(
@"class C { [A]int [||]GetFoo() { } }",
@"class C { [A]int Foo { get { } } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestMethodWithTrivia_1()
        {
            await TestAsync(
@"class C
{
    // Foo
    int [||]GetFoo()
    {
    }
}",
@"class C
{
    // Foo
    int Foo
    {
        get
        {
        }
    }
}",
compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestIfDefMethod()
        {
            await TestAsync(
@"class C
{
#if true
    int [||]GetFoo()
    {
    }
#endif
}",
@"class C
{
#if true
    int Foo
    {
        get
        {
        }
    }
#endif
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestMethodWithTrivia_2()
        {
            await TestAsync(
@"class C
{
    // Foo
    int [||]GetFoo()
    {
    }
    // SetFoo
    void SetFoo(int i)
    {
    }
}",
@"class C
{
    // Foo
    // SetFoo
    int Foo
    {
        get
        {
        }

        set
        {
        }
    }
}",
index: 1,
compareTokens: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestExplicitInterfaceMethod_1()
        {
            await TestAsync(
@"class C { int [||]I.GetFoo() { } }",
@"class C { int I.Foo { get { } } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestExplicitInterfaceMethod_2()
        {
            await TestAsync(
@"interface I { int GetFoo(); } class C : I { int [||]I.GetFoo() { } }",
@"interface I { int Foo { get; } } class C : I { int I.Foo { get { } } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestExplicitInterfaceMethod_3()
        {
            await TestAsync(
@"interface I { int [||]GetFoo(); } class C : I { int I.GetFoo() { } }",
@"interface I { int Foo { get; } } class C : I { int I.Foo { get { } } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestInAttribute()
        {
            await TestMissingAsync(
@"class C { [At[||]tr]int GetFoo() { } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestInMethod()
        {
            await TestMissingAsync(
@"class C { int GetFoo() { [||] } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestVoidMethod()
        {
            await TestMissingAsync(
@"class C { void [||]GetFoo() { } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestAsyncMethod()
        {
            await TestMissingAsync(
@"class C { async Task [||]GetFoo() { } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestGenericMethod()
        {
            await TestMissingAsync(
@"class C { int [||]GetFoo<T>() { } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestExtensionMethod()
        {
            await TestMissingAsync(
@"static class C { int [||]GetFoo(this int i) { } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestMethodWithParameters_1()
        {
            await TestMissingAsync(
@"class C { int [||]GetFoo(int i) { } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestMethodWithParameters_2()
        {
            await TestMissingAsync(
@"class C { int [||]GetFoo(int i = 0) { } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestNotInSignature_1()
        {
            await TestMissingAsync(
@"class C { [At[||]tr]int GetFoo() { } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestNotInSignature_2()
        {
            await TestMissingAsync(
@"class C { int GetFoo() { [||] } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetReferenceNotInMethod()
        {
            await TestAsync(
@"class C { int [||]GetFoo() { } void Bar() { var x = GetFoo(); } }",
@"class C { int Foo { get { } } void Bar() { var x = Foo; } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetReferenceSimpleInvocation()
        {
            await TestAsync(
@"class C { int [||]GetFoo() { } void Bar() { var x = GetFoo(); } }",
@"class C { int Foo { get { } } void Bar() { var x = Foo; } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetReferenceMemberAccessInvocation()
        {
            await TestAsync(
@"class C { int [||]GetFoo() { } void Bar() { var x = this.GetFoo(); } }",
@"class C { int Foo { get { } } void Bar() { var x = this.Foo; } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetReferenceBindingMemberInvocation()
        {
            await TestAsync(
@"class C { int [||]GetFoo() { } void Bar() { C x; var v = x?.GetFoo(); } }",
@"class C { int Foo { get { } } void Bar() { C x; var v = x?.Foo; } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetReferenceInMethod()
        {
            await TestAsync(
@"class C { int [||]GetFoo() { return GetFoo(); } }",
@"class C { int Foo { get { return Foo; } } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestOverride()
        {
            await TestAsync(
@"class C { public virtual int [||]GetFoo() { } } class D : C { public override int GetFoo() { } }",
@"class C { public virtual int Foo { get { } } } class D : C { public override int Foo { get { } } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetReference_NonInvoked()
        {
            await TestAsync(
@"using System; class C { int [||]GetFoo() { } void Bar() { Action<int> i = GetFoo; } }",
@"using System; class C { int Foo { get { } } void Bar() { Action<int> i = {|Conflict:Foo|}; } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetReference_ImplicitReference()
        {
            await TestAsync(
@"using System.Collections; class C { public IEnumerator [||]GetEnumerator() { } void Bar() { foreach (var x in this) { } } }",
@"using System.Collections; class C { public IEnumerator Enumerator { get { } } void Bar() { {|Conflict:foreach (var x in this) { }|} } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetSet()
        {
            await TestAsync(
@"using System; class C { int [||]GetFoo() { } void SetFoo(int i) { } }",
@"using System; class C { int Foo { get { } set { } } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetSetReference_NonInvoked()
        {
            await TestAsync(
@"using System; class C { int [||]GetFoo() { } void SetFoo(int i) { } void Bar() { Action<int> i = SetFoo; } }",
@"using System; class C { int Foo { get { } set { } } void Bar() { Action<int> i = {|Conflict:Foo|}; } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetSet_SetterAccessibility()
        {
            await TestAsync(
@"using System; class C { public int [||]GetFoo() { } private void SetFoo(int i) { } }",
@"using System; class C { public int Foo { get { } private set { } } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetSet_ExpressionBodies()
        {
            await TestAsync(
@"using System; class C { int [||]GetFoo() => 0; void SetFoo(int i) => Bar(); }",
@"using System; class C { int Foo { get { return 0; } set { Bar(); } } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetSet_GetInSetReference()
        {
            await TestAsync(
@"using System; class C { int [||]GetFoo() { } void SetFoo(int i) { } void Bar() { SetFoo(GetFoo() + 1); } }",
@"using System; class C { int Foo { get { } set { } } void Bar() { Foo = Foo + 1; } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetSet_UpdateSetParameterName_1()
        {
            await TestAsync(
@"using System; class C { int [||]GetFoo() { } void SetFoo(int i) { v = i; } }",
@"using System; class C { int Foo { get { } set { v = value; } } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetSet_UpdateSetParameterName_2()
        {
            await TestAsync(
@"using System; class C { int [||]GetFoo() { } void SetFoo(int value) { v = value; } }",
@"using System; class C { int Foo { get { } set { v = value; } } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetSet_SetReferenceInSetter()
        {
            await TestAsync(
@"using System; class C { int [||]GetFoo() { } void SetFoo(int i) { SetFoo(i - 1); } }",
@"using System; class C { int Foo { get { } set { Foo = value - 1; } } }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestVirtualGetWithOverride_1()
        {
            await TestAsync(
@"class C { protected virtual int [||]GetFoo() { } } class D : C { protected override int GetFoo() { } }",
@"class C { protected virtual int Foo { get { } } } class D : C { protected override int Foo{ get { } } }",
index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestVirtualGetWithOverride_2()
        {
            await TestAsync(
@"class C { protected virtual int [||]GetFoo() { } } class D : C { protected override int GetFoo() { base.GetFoo(); } }",
@"class C { protected virtual int Foo { get { } } } class D : C { protected override int Foo { get { base.Foo; } } }",
index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestGetWithInterface()
        {
            await TestAsync(
@"interface I { int [||]GetFoo(); } class C : I { public int GetFoo() { } }",
@"interface I { int Foo { get; } } class C : I { public int Foo { get { } } }",
index: 0);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestWithPartialClasses()
        {
            await TestAsync(
@"partial class C { int [||]GetFoo() { } } partial class C { void SetFoo(int i) { } }",
@"partial class C { int Foo { get { } set { } } } partial class C { }",
index: 1);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestUpdateGetSetCaseInsensitive()
        {
            await TestAsync(
@"using System; class C { int [||]getFoo() { } void setFoo(int i) { } }",
@"using System; class C { int Foo { get { } set { } } }",
index: 1);
        }
    }
}