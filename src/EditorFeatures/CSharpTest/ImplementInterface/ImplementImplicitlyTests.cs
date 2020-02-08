﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private const int SingleMember = 0;
        private const int SameInterface = 1;
        private const int AllInterfaces = 2;

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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestCollision()
        {
            // Currently we don't do anything special here.  But we just test here to make sure we
            // don't blow up here.
            await TestInRegularAndScriptAsync(
@"
interface IGoo { void Goo1(); }

class C : IGoo
{
    void IGoo.[||]Goo1() { }

    private void Goo1() { }
}",
@"
interface IGoo { void Goo1(); }

class C : IGoo
{
    public void Goo1() { }

    private void Goo1() { }
}", index: SingleMember);
        }
    }
}
