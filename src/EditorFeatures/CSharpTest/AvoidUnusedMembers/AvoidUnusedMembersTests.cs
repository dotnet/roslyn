// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.AvoidUnusedMembers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.AvoidUnusedMembers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AvoidUnusedMembers
{
    public class AvoidUnusedMembersTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new AvoidUnusedMembersDiagnosticAnalyzer(), new CSharpAvoidUnusedMembersCodeFixProvider());

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        [InlineData("public")]
        [InlineData("internal")]
        [InlineData("protected")]
        [InlineData("protected internal")]
        [InlineData("private protected")]
        public async Task NonPrivateField(string accessibility)
        {
            await TestMissingInRegularAndScriptAsync(
$@"class MyClass
{{
    {accessibility} int[| _goo |];
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        [InlineData("public")]
        [InlineData("internal")]
        [InlineData("protected")]
        [InlineData("protected internal")]
        [InlineData("private protected")]
        public async Task NonPrivateMethod(string accessibility)
        {
            await TestMissingInRegularAndScriptAsync(
$@"class MyClass
{{
    {accessibility} void [|M|]() {{ }}
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        [InlineData("public")]
        [InlineData("internal")]
        [InlineData("protected")]
        [InlineData("protected internal")]
        [InlineData("private protected")]
        public async Task NonPrivateProperty(string accessibility)
        {
            await TestMissingInRegularAndScriptAsync(
$@"class MyClass
{{
    {accessibility} int[| P |] {{ get; }}
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        [InlineData("public")]
        [InlineData("internal")]
        [InlineData("protected")]
        [InlineData("protected internal")]
        [InlineData("private protected")]
        public async Task NonPrivateIndexer(string accessibility)
        {
            await TestMissingInRegularAndScriptAsync(
$@"class MyClass
{{
    {accessibility} int [|this|] {{ get {{ return 0; }} set {{ }} }}
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        [InlineData("public")]
        [InlineData("internal")]
        [InlineData("protected")]
        [InlineData("protected internal")]
        [InlineData("private protected")]
        public async Task NonPrivateEvent(string accessibility)
        {
            await TestMissingInRegularAndScriptAsync(
$@"using System;

class MyClass
{{
    {accessibility} event EventHandler [|RaiseCustomEvent|];
}}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsUnused()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task MethodIsUnused()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|M()|] => 0;
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task PropertyIsUnused()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|P|] { get; set; }
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task IndexerIsUnused()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|this|][int x] { get { return 0; } set { } }
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task EventIsUnused()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private event System.EventHandler [|e|];
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsUnused_ReadOnly()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private readonly int [|_goo|];
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task PropertyIsUnused_ReadOnly()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|P|] { get; }
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task EventIsUnused_ReadOnly()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private readonly event System.EventHandler [|E|];
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsUnused_Static()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private static int [|_goo|];
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task MethodIsUnused_Static()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private static void [|M|] { }
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task PropertyIsUnused_Static()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private static int [|P|] { get { return 0; } }
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task IndexerIsUnused_Static()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private static int [|this|][int x] { get { return 0; } set { } }
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task EventIsUnused_Static()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private static event System.EventHandler [|e1|];
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsUnused_Const()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private const int [|_goo|] = 0;
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsRead_ExpressionBody()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    public int M() => _goo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsRead_BlockBody()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    public int M() { return _goo; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsRead_ExpressionLambda()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    public void M()
    {
        Func<int> getGoo = () => _goo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsRead_BlockLambda()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    public void M()
    {
        Func<int> getGoo = () => { return _goo; }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsRead_Delegate()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    public void M()
    {
        Func<int> getGoo = delegate { return _goo; }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsRead_ExpressionBodyLocalFunction()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    public int M()
    {
        int LocalFunction() => _goo;
        return LocalFunction();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsRead_BlockBodyLocalFunction()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    public int M()
    {
        int LocalFunction() { return _goo; }
        return LocalFunction();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsRead_Accessor()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    public void Goo
    {
        get
        {
            return _goo;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsRead_Deconstruction()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    public void M(int x)
    {
        var y = (_goo, x);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsRead_DifferentInstance()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    public int M() => new MyClass()._goo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsRead_ObjectInitializer()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    public int F;
}
class MyClass
{
    private int [|_goo|];
    public C M() => new C() { F = _goo };
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsRead_ThisInstance()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    int M() => this._goo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsRead_Attribute()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private const string [|_goo|] = """";

    [System.Obsolete(_goo)]
    public void M() { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task MethodIsInvoked()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|M1|] => 0
    public int M2() => M1();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task MethodIsAddressTaken()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|M1|] => 0
    public void M2()
    {
        System.Func<int> m1 = M1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task PropertyIsRead()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|P|] => 0;
    public int M() => P;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task IndexerIsRead()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|this|][int x] { get { return 0; } set { } }
    public int M(int x) => this[x];
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task EventIsRead()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class MyClass
{
    private event EventHandler [|e|];
    public EventHandler P => e;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task EventIsSubscribed()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class MyClass
{
    private event EventHandler [|e|];
    public void M()
    {
        e += MyHandler;
    }

    static void MyHandler(object sender, EventArgs e)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task EventIsRaised()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class MyClass
{
    private event EventHandler [|_eventHandler|];

    public void RaiseEvent(EventArgs e)
    {
        _eventHandler(this, e);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldInNameOf()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    private string _goo2 = nameof(_goo);
}",
@"class MyClass
{
    private string _goo2 = nameof(_goo);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsOnlyWritten()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    public void M()
    {
        _goo = 0;
    }
}",
@"class MyClass
{
    public void M()
    {
        _goo = 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task PropertyIsOnlyWritten()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|P|] { get; set; }
    public void M()
    {
        P = 0;
    }
}",
@"class MyClass
{
    public void M()
    {
        P = 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task IndexerIsOnlyWritten()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|this|][int x] { get { return 0; } set { } }
    public void M(int x, int y)
    {
        this[x] = y;
    }
}",
@"class MyClass
{
    public void M(int x, int y)
    {
        this[x] = y;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task EventIsOnlyWritten()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private event System.EventHandler [|e|] { add { } remove { } }
    public void M()
    {
        // CS0079: The event 'MyClass.e' can only appear on the left hand side of += or -=
        e = null;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsOnlyWritten_Deconstruction()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    public void M()
    {
        int x;
        (_goo, x) = (0, 0);
    }
}",
@"class MyClass
{
    public void M()
    {
        int x;
        (_goo, x) = (0, 0);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsOnlyWritten_ObjectInitializer()
        {
            await TestInRegularAndScriptAsync(
@"
class MyClass
{
    private int [|_goo|];
    public MyClass M() => new MyClass() { _goo = 0 };
}",
@"
class MyClass
{
    public MyClass M() => new MyClass() { _goo = 0 };
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsOnlyWritten_InProperty()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    int Goo
    {
        get { return 0; }
        set { _goo = value; }
    }
}",
@"class MyClass
{
    int Goo
    {
        get { return 0; }
        set { _goo = value; }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsReadAndWritten()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    public void M()
    {
        _goo = 0;
        System.Console.WriteLine(_goo);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task PropertyIsReadAndWritten()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|P|] { get; set; }
    public void M()
    {
        P = 0;
        System.Console.WriteLine(P);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task IndexerIsReadAndWritten()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|this|][int x] { get { return 0; } set { } }
    public void M(int x)
    {
        this[x] = 0;
        System.Console.WriteLine(this[x]);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsReadAndWritten_InProperty()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    int Goo
    {
        get { return _goo; }
        set { _goo = value; }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsIncremented()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    public int M1() => ++_goo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task PropertyIsIncremented()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|P|] { get; set; }
    public int M1() => ++P;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task IndexerIsIncremented()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|this|][int x] { get { return 0; } set { } }
    public int M1(int x) => ++this[x];
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsTargetOfCompoundAssignment()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    public int M1(int x) => _goo += x;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task PropertyIsTargetOfCompoundAssignment()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|P|] { get; set; }
    public int M1(int x) => P += x;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task IndexerIsTargetOfCompoundAssignment()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|this|][int x] { get { return 0; } set { } }
    public int M1(int x, int y) => this[x] += y;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsArg()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    public int M1() => M2(_goo);
    public int M2(int i) => { i = 0; return i; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsInArg()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    public int M1() => M2(_goo);
    public int M2(in int i) => { i = 0; return i; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsRefArg()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    public int M1() => M2(ref _goo);
    public int M2(ref int i) => i;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsOutArg()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    public int M1() => M2(out _goo);
    public int M2(out int i) => { i = 0; return i; }
}",
@"class MyClass
{
    public int M1() => M2(out _goo);
    public int M2(out int i) => { i = 0; return i; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task MethodIsArg()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|M|]() => 0;
    public int M1() => M2(M);
    public int M2(System.Func<int> m) => m();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task PropertyIsArg()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|P|] => 0;
    public int M1() => M2(P);
    public int M2(int p) => p;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task IndexerIsArg()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|this|][int x] { get { return 0; } set { } }
    public int M1(int x) => M2(this[x]);
    public int M2(int p) => p;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task EventIsArg()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System;

class MyClass
{
    private event EventHandler [|_e|];
    public EventHandler M1() => M2(_e);
    public EventHandler M2(EventHandler e) => e;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task MultipleFields_AllUnused()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|] = 0, _bar = 0;
}",
@"class MyClass
{
    private int _bar = 0;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task MultipleFields_AllUnused_02()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int _goo = 0, [|_bar|];
}",
@"class MyClass
{
    private int _goo = 0;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task MultipleFields_SomeUnused()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|] = 0, _bar = 0;
    public int M() => _bar;
}",
@"class MyClass
{
    private int _bar = 0;
    public int M() => _bar;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task MultipleFields_SomeUnused_02()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|] = 0, _bar = 0;
    public int M() => _goo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsRead_InNestedType()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];

    class Derived : MyClass
    {
        public in M() => _goo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task MethodIsInvoked_InNestedType()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|M1|]() => 0;

    class Derived : MyClass
    {
        public in M2() => M1();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldOfNestedTypeIsUnused()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    class NestedType
    {
        private int [|_goo|];
    }
}",
@"class MyClass
{
    class NestedType
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldOfNestedTypeIsRead()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    class NestedType
    {
        private int [|_goo|];

        public int M() => _goo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsUnused_PartialClass()
        {
            await TestInRegularAndScriptAsync(
@"partial class MyClass
{
    private int [|_goo|];
}",
@"partial class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsRead_PartialClass()
        {
            await TestMissingInRegularAndScriptAsync(
@"partial class MyClass
{
    private int [|_goo|];
}
partial class MyClass
{
    public int M() => _goo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsRead_PartialClass_DifferentFile()
        {
            await TestMissingInRegularAndScriptAsync(
@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>partial class MyClass
{
    private int [|_goo|];
}
        </Document>
        <Document>partial class MyClass
{
    public int M() => _goo;
}
        </Document>
    </Project>
</Workspace>");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsOnlyWritten_PartialClass_DifferentFile()
        {
            await TestInRegularAndScriptAsync(
@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>partial class MyClass
{
    private int [|_goo|];
}
        </Document>
        <Document>partial class MyClass
{
    public void M() { _goo = 0; }
}
        </Document>
    </Project>
</Workspace>",
@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>partial class MyClass
{
}
        </Document>
        <Document>partial class MyClass
{
    public void M() { _goo = 0; }
}
        </Document>
    </Project>
</Workspace>");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsRead_InParens()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    public int M() => (_goo);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsWritten_InParens()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    public int M() { (_goo) = 1; }
}",
@"class MyClass
{
    public int M() { (_goo) = 1; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsRead_InDeconstruction_InParens()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    private int [|i|];

    public void M()
    {
        var x = ((i, 0), 0);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldInTypeWithGeneratedCode()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    private int [|i|];

    [System.CodeDom.Compiler.GeneratedCodeAttribute("""", """")]
    private int j;

    public void M()
    {
    }
}",
@"class C
{
    [System.CodeDom.Compiler.GeneratedCodeAttribute("""", """")]
    private int j;

    public void M()
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldIsGeneratedCode()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    [System.CodeDom.Compiler.GeneratedCodeAttribute("""", """")]
    [|private int i;|]

    public void M()
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FieldUsedInGeneratedCode()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    private int [|i|];

    [System.CodeDom.Compiler.GeneratedCodeAttribute("""", """")]
    public int M() => i;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FixAllFields_Document()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int {|FixAllInDocument:_goo|} = 0, _bar;
    private int _x = 0, _y, _z = 0;
    private string _fizz = null;

    public int Method() => _z;
}",
@"class MyClass
{
    private int _z = 0;

    public int Method() => _z;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FixAllMethods_Document()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int {|FixAllInDocument:M1|}() => 0;
    private void M2() { }
    private static void M3() { }
    private class NestedClass
    {
        private void M4() { }
    }
}",
@"class MyClass
{
    private class NestedClass
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FixAllProperties_Document()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int {|FixAllInDocument:P1|} => 0;
    private int P2 { get; set; }
    private int P3 { get { return 0; } set { } }
    private int this[int i] { get { return 0; } }
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FixAllEvents_Document()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class MyClass
{
    private event EventHandler {|FixAllInDocument:E1|}, E2 = null, E3;
    private event EventHandler E4, E5 = null;
    private event EventHandler E
    {
        add { }
        remove { }
    }

    public void M()
    {
        EventHandler handler = E2;
    }
}",
@"using System;

class MyClass
{
    private event EventHandler E2 = null;

    public void M()
    {
        EventHandler handler = E2;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAvoidUnusedMembers)]
        public async Task FixAllMembers_Project()
        {
            await TestInRegularAndScriptAsync(
@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

partial class MyClass
{
    private int {|FixAllInProject:_f1|}, f2 = 0, f3;
    private void M1() { }
    private int P1 => 0;
    private int this[x] { get { return 0; } set { } }
    private event EventHandler e1, e2 = null;
}

class MyClass2
{
    private void M2() { }
}
        </Document>
        <Document>
partial class MyClass
{
    private void M3() { }
    public int M4() => f2;
}

static class MyClass3
{
    private static void M5() { }
}
        </Document>
    </Project>
</Workspace>",
@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

partial class MyClass
{
    private int f2 = 0;
}

class MyClass2
{
}
        </Document>
        <Document>
partial class MyClass
{
    public int M4() => f2;
}

static class MyClass3
{
}
        </Document>
    </Project>
</Workspace>");
        }
    }
}
