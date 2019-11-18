// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.ImplementInterface;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ImplementInterface
{
    public partial class ImplementImplicitlyTests : AbstractCSharpCodeActionTest
    {
        private const int SingleMember = 1;
        private const int SameInterface = 2;
        private const int AllInterfaces = 3;

        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpImplementImplicitlyCodeRefactoringProvider();

        protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
            => FlattenActions(actions);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestSingleMember()
        {
            await TestInRegularAndScriptAsync(
@"
interface IGoo { void Goo1(); void Goo2(); }
interface IBar { void Bar(); }

class C : IGoo, IBar
{
    void IGoo.[||]Goo1() { }

    void IGoo.Goo2() { }

    void IBar.Bar() { }
}",
@"
interface IGoo { void Goo1(); void Goo2(); }
interface IBar { void Bar(); }

class C : IGoo, IBar
{
    public void Goo1() { }

    void IGoo.Goo2() { }

    void IBar.Bar() { }
}", index: SingleMember);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestSameInterface()
        {
            await TestInRegularAndScriptAsync(
@"
interface IGoo { void Goo1(); void Goo2(); }
interface IBar { void Bar(); }

class C : IGoo, IBar
{
    void IGoo.[||]Goo1() { }

    void IGoo.Goo2() { }

    void IBar.Bar() { }
}",
@"
interface IGoo { void Goo1(); void Goo2(); }
interface IBar { void Bar(); }

class C : IGoo, IBar
{
    public void Goo1() { }

    public void Goo2() { }

    void IBar.Bar() { }
}", index: SameInterface);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestAllInterfaces()
        {
            await TestInRegularAndScriptAsync(
@"
interface IGoo { void Goo1(); void Goo2(); }
interface IBar { void Bar(); }

class C : IGoo, IBar
{
    void IGoo.[||]Goo1() { }

    void IGoo.Goo2() { }

    void IBar.Bar() { }
}",
@"
interface IGoo { void Goo1(); void Goo2(); }
interface IBar { void Bar(); }

class C : IGoo, IBar
{
    public void Goo1() { }

    public void Goo2() { }

    public void Bar() { }
}", index: AllInterfaces);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestProperty()
        {
            await TestInRegularAndScriptAsync(
@"
interface IGoo { int Goo1 { get; } }

class C : IGoo
{
    int IGoo.[||]Goo1 { get { } }
}",
@"
interface IGoo { int Goo1 { get; } }

class C : IGoo
{
    public int Goo1 { get { } }
}", index: SingleMember);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestEvent()
        {
            await TestInRegularAndScriptAsync(
@"
interface IGoo { event Action E; }

class C : IGoo
{
    event Action IGoo.[||]E { add { } remove { } }
}",
@"
interface IGoo { event Action E; }

class C : IGoo
{
    public event Action E { add { } remove { } }
}", index: SingleMember);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestNotOnImplicitMember()
        {
            await TestMissingAsync(
@"
interface IGoo { void Goo1(); }

class C : IGoo
{
    public void [||]Goo1() { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestNotOnUnboundExplicitImpl()
        {
            await TestMissingAsync(
@"
class C : IGoo
{
    void IGoo.[||]Goo1() { }
}");
        }
    }
}
