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
    public partial class ImplementExplicitlyTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpImplementExplicitlyCodeRefactoringProvider();

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
    public void [||]Goo1() { }

    public void Goo2() { }

    public void Bar() { }
}",
@"
interface IGoo { void Goo1(); void Goo2(); }
interface IBar { void Bar(); }

class C : IGoo, IBar
{
    void IGoo.Goo1() { }

    public void Goo2() { }

    public void Bar() { }
}");
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
    public void [||]Goo1() { }

    public void Goo2() { }

    public void Bar() { }
}",
@"
interface IGoo { void Goo1(); void Goo2(); }
interface IBar { void Bar(); }

class C : IGoo, IBar
{
    void IGoo.Goo1() { }

    void IGoo.Goo2() { }

    public void Bar() { }
}", index: 1);
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
    public void [||]Goo1() { }

    public void Goo2() { }

    public void Bar() { }
}",
@"
interface IGoo { void Goo1(); void Goo2(); }
interface IBar { void Bar(); }

class C : IGoo, IBar
{
    void IGoo.Goo1() { }

    void IGoo.Goo2() { }

    void IBar.Bar() { }
}", index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestProperty()
        {
            await TestInRegularAndScriptAsync(
@"
interface IGoo { int Goo1 { get; } }

class C : IGoo
{
    public int [||]Goo1 { get { } }
}",
@"
interface IGoo { int Goo1 { get; } }

class C : IGoo
{
    int IGoo.Goo1 { get { } }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestEvent()
        {
            await TestInRegularAndScriptAsync(
@"
interface IGoo { event Action E; }

class C : IGoo
{
    public event Action [||]E { add { } remove { } }
}",
@"
interface IGoo { event Action E; }

class C : IGoo
{
    event Action IGoo.E { add { } remove { } }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestNotOnExplicitMember()
        {
            await TestMissingAsync(
@"
interface IGoo { void Goo1(); }

class C : IGoo
{
    void IGoo.[||]Goo1() { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestNotOnUnboundImplicitImpl()
        {
            await TestMissingAsync(
@"
interface IGoo { void Goo1(); }

class C
{
    public void [||]Goo1() { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestUpdateReferences_InsideDeclarations_Explicit()
        {
            await TestInRegularAndScriptAsync(
@"
using System;
interface IGoo { int Prop { get; set; } int M(int i); event Action Ev; }

class C : IGoo
{
    public int [||]Prop { get { return this.Prop; } set { this.Prop = value; } }
    public int M(int i) { return this.M(i); }
    public event Action Ev { add { this.Ev += value; } remove { this.Ev -= value; } }
}",
@"
using System;
interface IGoo { int Prop { get; set; } int M(int i); event Action Ev; }

class C : IGoo
{
    int IGoo.Prop { get { return ((IGoo)this).Prop; } set { ((IGoo)this).Prop = value; } }
    int IGoo.M(int i) { return ((IGoo)this).M(i); }
    event Action IGoo.Ev { add { ((IGoo)this).Ev += value; } remove { ((IGoo)this).Ev -= value; } }
}", index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestUpdateReferences_InsideDeclarations_Implicit()
        {
            await TestInRegularAndScriptAsync(
@"
using System;
interface IGoo { int Prop { get; set; } int M(int i); event Action Ev; }

class C : IGoo
{
    public int [||]Prop { get { return Prop; } set { Prop = value; } }
    public int M(int i) { return M(i); }
    public event Action Ev { add { Ev += value; } remove { Ev -= value; } }
}",
@"
using System;
interface IGoo { int Prop { get; set; } int M(int i); event Action Ev; }

class C : IGoo
{
    int IGoo.Prop { get { return ((IGoo)this).Prop; } set { ((IGoo)this).Prop = value; } }
    int IGoo.M(int i) { return ((IGoo)this).M(i); }
    event Action IGoo.Ev { add { ((IGoo)this).Ev += value; } remove { ((IGoo)this).Ev -= value; } }
}", index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestUpdateReferences_InternalImplicit()
        {
            await TestInRegularAndScriptAsync(
@"
using System;
interface IGoo { int Prop { get; set; } int M(int i); event Action Ev; }

class C : IGoo
{
    public int [||]Prop { get { } set { } }
    public int M(int i) { }
    public event Action Ev { add { } remove { } }

    void InternalImplicit()
    {
        var v = Prop;
        Prop = 1;
        Prop++;
        ++Prop;

        M(0);
        M(M(0));

        Ev += () => {};

        var v1 = nameof(Prop);
    }
}",
@"
using System;
interface IGoo { int Prop { get; set; } int M(int i); event Action Ev; }

class C : IGoo
{
    int IGoo.Prop { get { } set { } }
    int IGoo.M(int i) { }
    event Action IGoo.Ev { add { } remove { } }

    void InternalImplicit()
    {
        var v = ((IGoo)this).Prop;
        ((IGoo)this).Prop = 1;
        ((IGoo)this).Prop++;
        ++((IGoo)this).Prop;

        ((IGoo)this).M(0);
        ((IGoo)this).M(((IGoo)this).M(0));

        ((IGoo)this).Ev += () => {};

        var v1 = nameof(((IGoo)this).Prop);
    }
}", index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestUpdateReferences_InternalExplicit()
        {
            await TestInRegularAndScriptAsync(
@"
using System;
interface IGoo { int Prop { get; set; } int M(int i); event Action Ev; }

class C : IGoo
{
    public int [||]Prop { get { } set { } }
    public int M(int i) { }
    public event Action Ev { add { } remove { } }

    void InternalExplicit()
    {
        var v = this.Prop;
        this.Prop = 1;
        this.Prop++;
        ++this.Prop;

        this.M(0);
        this.M(this.M(0));

        this.Ev += () => {};

        var v1 = nameof(this.Prop);
    }
}",
@"
using System;
interface IGoo { int Prop { get; set; } int M(int i); event Action Ev; }

class C : IGoo
{
    int IGoo.Prop { get { } set { } }
    int IGoo.M(int i) { }
    event Action IGoo.Ev { add { } remove { } }

    void InternalExplicit()
    {
        var v = ((IGoo)this).Prop;
        ((IGoo)this).Prop = 1;
        ((IGoo)this).Prop++;
        ++((IGoo)this).Prop;

        ((IGoo)this).M(0);
        ((IGoo)this).M(((IGoo)this).M(0));

        ((IGoo)this).Ev += () => {};

        var v1 = nameof(((IGoo)this).Prop);
    }
}", index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsImplementInterface)]
        public async Task TestUpdateReferences_External()
        {
            await TestInRegularAndScriptAsync(
@"
using System;
interface IGoo { int Prop { get; set; } int M(int i); event Action Ev; }

class C : IGoo
{
    public int [||]Prop { get { } set { } }
    public int M(int i) { }
    public event Action Ev { add { } remove { } }
}

class T
{
    void External(C c)
    {
        var v = c.Prop;
        c.Prop = 1;
        c.Prop++;
        ++c.Prop;

        c.M(0);
        c.M(c.M(0));

        c.Ev += () => {};

        new C
        {
            Prop = 1
        };

        var v1 = nameof(c.Prop);
    }
}",
@"
using System;
interface IGoo { int Prop { get; set; } int M(int i); event Action Ev; }

class C : IGoo
{
    int IGoo.Prop { get { } set { } }
    int IGoo.M(int i) { }
    event Action IGoo.Ev { add { } remove { } }
}

class T
{
    void External(C c)
    {
        var v = ((IGoo)c).Prop;
        ((IGoo)c).Prop = 1;
        ((IGoo)c).Prop++;
        ++((IGoo)c).Prop;

        ((IGoo)c).M(0);
        ((IGoo)c).M(((IGoo)c).M(0));

        ((IGoo)c).Ev += () => {};

        new C
        {
            Prop = 1
        };

        var v1 = nameof(((IGoo)c).Prop);
    }
}", index: 1);
        }
    }
}
