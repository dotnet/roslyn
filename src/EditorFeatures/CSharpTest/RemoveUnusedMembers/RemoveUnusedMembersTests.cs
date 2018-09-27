﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.RemoveUnusedMembers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using static Roslyn.Test.Utilities.TestHelpers;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnusedMembers
{
    public class RemoveUnusedMembersTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpRemoveUnusedMembersDiagnosticAnalyzer(), new CSharpRemoveUnusedMembersCodeFixProvider());

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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
    {accessibility} int [|_goo|];
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [InlineData("public")]
        [InlineData("internal")]
        [InlineData("protected")]
        [InlineData("protected internal")]
        [InlineData("private protected")]
        public async Task NonPrivateFieldWithConstantInitializer(string accessibility)
        {
            await TestMissingInRegularAndScriptAsync(
$@"class MyClass
{{
    {accessibility} int [|_goo|] = 0;
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [InlineData("public")]
        [InlineData("internal")]
        [InlineData("protected")]
        [InlineData("protected internal")]
        [InlineData("private protected")]
        public async Task NonPrivateFieldWithNonConstantInitializer(string accessibility)
        {
            await TestMissingInRegularAndScriptAsync(
$@"class MyClass
{{
    {accessibility} int [|_goo|] = _goo2;
    private static readonly int _goo2 = 0;
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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
    {accessibility} int [|P|] {{ get; }}
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task GenericMethodIsUnused()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|M|]<T>() => 0;
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodInGenericTypeIsUnused()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass<T>
{
    private int [|M|]() => 0;
}",
@"class MyClass<T>
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task InstanceConstructorIsUnused_NoArguments()
        {
            // We only flag constructors with arguments.
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private [|MyClass()|] { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task InstanceConstructorIsUnused_WithArguments()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private [|MyClass(int i)|] { }
}",
@"class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task StaticConstructorIsNotFlagged()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    static [|MyClass()|] { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task DestructorIsNotFlagged()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    ~[|MyClass()|] { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task EntryPointMethodNotFlagged()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private static void [|Main|]() { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task EntryPointMethodNotFlagged_02()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System.Threading.Tasks;

class MyClass
{
    private static async Task [|Main|]() => await Task.CompletedTask;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task EntryPointMethodNotFlagged_03()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System.Threading.Tasks;

class MyClass
{
    private static async Task<int> [|Main|]() => await Task.FromResult(0);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task EntryPointMethodNotFlagged_04()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System.Threading.Tasks;

class MyClass
{
    private static Task [|Main|]() => Task.CompletedTask;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodIsUnused_Extern()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System.Runtime.InteropServices;

class C
{
    [DllImport(""Assembly.dll"")]
    private static extern void [|M|]();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodIsUnused_Abstract()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    private abstract void [|M|]();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodIsUnused_InterfaceMethod()
        {
            await TestMissingInRegularAndScriptAsync(
@"interface I
{
    void [|M|]();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodIsUnused_ExplicitInterfaceImplementation()
        {
            await TestMissingInRegularAndScriptAsync(
@"interface I
{
    void M();
}

class C : I
{
    void I.[|M|]() { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task PropertyIsUnused_ExplicitInterfaceImplementation()
        {
            await TestMissingInRegularAndScriptAsync(
@"interface I
{
    int P { get; set; }
}

class C : I
{
    int I.[|P|] { get { return 0; } set { } }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_ExpressionBody()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    public int M() => _goo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_BlockBody()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    public int M() { return _goo; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_DifferentInstance()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    public int M() => new MyClass()._goo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_ThisInstance()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    int M() => this._goo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodIsInvoked()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|M1|] => 0
    public int M2() => M1();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task GenericMethodIsInvoked_ExplicitTypeArguments()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|M1|]<T>() => 0;
    private int M2() => M1<int>();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task GenericMethodIsInvoked_ImplicitTypeArguments()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private T [|M1|]<T>(T t) => t;
    private int M2() => M1(0);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodInGenericTypeIsInvoked_NoTypeArguments()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass<T>
{
    private int [|M1|]() => 0;
    private int M2() => M1();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodInGenericTypeIsInvoked_NonConstructedType()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass<T>
{
    private int [|M1|]() => 0;
    private int M2(MyClass<T> m) => m.M1();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodInGenericTypeIsInvoked_ConstructedType()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass<T>
{
    private int [|M1|]() => 0;
    private int M2(MyClass<int> m) => m.M1();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task InstanceConstructorIsUsed_NoArguments()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private [|MyClass()|] { }
    public static readonly MyClass Instance = new MyClass();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task InstanceConstructorIsUsed_WithArguments()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private [|MyClass(int i)|] { }
    public static readonly MyClass Instance = new MyClass(0);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task PropertyIsRead()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|P|] => 0;
    public int M() => P;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task IndexerIsRead()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|this|][int x] { get { return 0; } set { } }
    public int M(int x) => this[x];
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldInNameOf()
        {
            await TestDiagnosticsAsync(
@"class MyClass
{
    private int [|_goo|];
    private string _goo2 = nameof(_goo);
}",
    expected: Diagnostic("IDE0052"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldInDocComment()
        {
            await TestDiagnosticsAsync(
@"
/// <summary>
/// <see cref=""C._goo""/>
/// </summary>
class C
{
    private static int [|_goo|];
}",
    expected: Diagnostic("IDE0052"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldInDocComment_02()
        {
            await TestDiagnosticsAsync(
@"
class C
{
    /// <summary>
    /// <see cref=""_goo""/>
    /// </summary>
    private static int [|_goo|];
}",
    expected: Diagnostic("IDE0052"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldInDocComment_03()
        {
            await TestDiagnosticsAsync(
@"
class C
{
    /// <summary>
    /// <see cref=""_goo""/>
    /// </summary>
    public void M() { }

    private static int [|_goo|];
}",
    expected: Diagnostic("IDE0052"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsOnlyWritten()
        {
            await TestDiagnosticsAsync(
@"class MyClass
{
    private int [|_goo|];
    public void M()
    {
        _goo = 0;
    }
}",
    expected: Diagnostic("IDE0052"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task PropertyIsOnlyWritten()
        {
            await TestDiagnosticsAsync(
@"class MyClass
{
    private int [|P|] { get; set; }
    public void M()
    {
        P = 0;
    }
}",
    expected: Diagnostic("IDE0052"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task IndexerIsOnlyWritten()
        {
            await TestDiagnosticsAsync(
@"class MyClass
{
    private int [|this|][int x] { get { return 0; } set { } }
    public void M(int x, int y)
    {
        this[x] = y;
    }
}",
    expected: Diagnostic("IDE0052"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsOnlyInitialized_NonConstant()
        {
            await TestDiagnosticsAsync(
@"class MyClass
{
    private int [|_goo|] = M();
    public static int M() => 0;
}",
    expected: Diagnostic("IDE0052"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsOnlyWritten_Deconstruction()
        {
            await TestDiagnosticsAsync(
@"class MyClass
{
    private int [|_goo|];
    public void M()
    {
        int x;
        (_goo, x) = (0, 0);
    }
}",
    expected: Diagnostic("IDE0052"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsOnlyWritten_ObjectInitializer()
        {
            await TestDiagnosticsAsync(
@"
class MyClass
{
    private int [|_goo|];
    public MyClass M() => new MyClass() { _goo = 0 };
}",
    expected: Diagnostic("IDE0052"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsOnlyWritten_InProperty()
        {
            await TestDiagnosticsAsync(
@"class MyClass
{
    private int [|_goo|];
    int Goo
    {
        get { return 0; }
        set { _goo = value; }
    }
}",
    expected: Diagnostic("IDE0052"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsIncrementedAndValueUsed()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    public int M1() => ++_goo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsIncrementedAndValueUsed_02()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    public int M1() { return ++_goo; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsIncrementedAndValueDropped()
        {
            await TestDiagnosticsAsync(
@"class MyClass
{
    private int [|_goo|];
    public void M1() => ++_goo;
}",
    expected: Diagnostic("IDE0052"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsIncrementedAndValueDropped_02()
        {
            await TestDiagnosticsAsync(
@"class MyClass
{
    private int [|_goo|];
    public void M1() { ++_goo; }
}",
    expected: Diagnostic("IDE0052"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task PropertyIsIncrementedAndValueUsed()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|P|] { get; set; }
    public int M1() => ++P;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task PropertyIsIncrementedAndValueDropped()
        {
            await TestDiagnosticsAsync(
@"class MyClass
{
    private int [|P|] { get; set; }
    public void M1() { ++P; }
}",
    expected: Diagnostic("IDE0052"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task IndexerIsIncrementedAndValueUsed()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|this|][int x] { get { return 0; } set { } }
    public int M1(int x) => ++this[x];
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task IndexerIsIncrementedAndValueDropped()
        {
            await TestDiagnosticsAsync(
@"class MyClass
{
    private int [|this|][int x] { get { return 0; } set { } }
    public void M1(int x) => ++this[x];
}",
    expected: Diagnostic("IDE0052"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsTargetOfCompoundAssignmentAndValueUsed()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    public int M1(int x) => _goo += x;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsTargetOfCompoundAssignmentAndValueUsed_02()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    public int M1(int x) { return _goo += x; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsTargetOfCompoundAssignmentAndValueDropped()
        {
            await TestDiagnosticsAsync(
@"class MyClass
{
    private int [|_goo|];
    public void M1(int x) => _goo += x;
}",
    expected: Diagnostic("IDE0052"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsTargetOfCompoundAssignmentAndValueDropped_02()
        {
            await TestDiagnosticsAsync(
@"class MyClass
{
    private int [|_goo|];
    public void M1(int x) { _goo += x; }
}",
    expected: Diagnostic("IDE0052"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task PropertyIsTargetOfCompoundAssignmentAndValueUsed()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|P|] { get; set; }
    public int M1(int x) => P += x;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task PropertyIsTargetOfCompoundAssignmentAndValueDropped()
        {
            await TestDiagnosticsAsync(
@"class MyClass
{
    private int [|P|] { get; set; }
    public void M1(int x) { P += x; }
}",
    expected: Diagnostic("IDE0052"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task IndexerIsTargetOfCompoundAssignmentAndValueUsed()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|this|][int x] { get { return 0; } set { } }
    public int M1(int x, int y) => this[x] += y;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task IndexerIsTargetOfCompoundAssignmentAndValueDropped()
        {
            await TestDiagnosticsAsync(
@"class MyClass
{
    private int [|this|][int x] { get { return 0; } set { } }
    public void M1(int x, int y) => this[x] += y;
}",
    expected: Diagnostic("IDE0052"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsTargetOfAssignmentAndParenthesized()
        {
            await TestDiagnosticsAsync(
@"class MyClass
{
    private int [|_goo|];
    public void M1(int x) => (_goo) = x;
}",
    expected: Diagnostic("IDE0052"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsTargetOfAssignmentAndHasImplicitConversion()
        {
            await TestDiagnosticsAsync(
@"class MyClass
{
    private int [|_goo|];
    public static implicit operator int(MyClass c) => 0;
    public void M1(MyClass c) => _goo = c;
}",
    expected: Diagnostic("IDE0052"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsOutArg()
        {
            await TestDiagnosticsAsync(
@"class MyClass
{
    private int [|_goo|];
    public int M1() => M2(out _goo);
    public int M2(out int i) { i = 0; return i; }
}",
    expected: Diagnostic("IDE0052"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MultipleFields_SomeUnused_02()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|] = 0, _bar = 0;
    public int M() => _goo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsOnlyWritten_PartialClass_DifferentFile()
        {
            await TestDiagnosticsAsync(
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
    expected: Diagnostic("IDE0052"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_InParens()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    public int M() => (_goo);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsWritten_InParens()
        {
            await TestDiagnosticsAsync(
@"class MyClass
{
    private int [|_goo|];
    public void M() { (_goo) = 1; }
}",
    expected: Diagnostic("IDE0052"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsWritten_InParens_02()
        {
            await TestDiagnosticsAsync(
@"class MyClass
{
    private int [|_goo|];
    public int M() => (_goo) = 1;
}",
    expected: Diagnostic("IDE0052"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsUnusedInType_SyntaxError()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    private int [|i|];

    public int M() { return = ; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsUnusedInType_SemanticError()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    private int [|i|];

    // 'ii' is undefined.
    public int M() => ii;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsUnusedInType_SemanticErrorInDifferentType()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    private int [|i|];
}

class C2
{
    // 'ii' is undefined.
    public int M() => ii;
}",
@"class C
{
}

class C2
{
    // 'ii' is undefined.
    public int M() => ii;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task StructLayoutAttribute_ExplicitLayout()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Explicit)]
class C
{
    [FieldOffset(0)]
    private int [|i|];

    [FieldOffset(4)]
    private int i2;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task StructLayoutAttribute_SequentialLayout()
        {
            await TestMissingInRegularAndScriptAsync(
@"using System.Runtime.InteropServices;

[StructLayoutAttribute(LayoutKind.Sequential)]
struct S
{
    private int [|i|];
    private int i2;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task DebuggerDisplayAttribute_OnType_ReferencesField()
        {
            await TestMissingInRegularAndScriptAsync(
@"[System.Diagnostics.DebuggerDisplayAttribute(""{s}"")]
class C
{
    private string [|s|];
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task DebuggerDisplayAttribute_OnType_ReferencesMethod()
        {
            await TestMissingInRegularAndScriptAsync(
@"[System.Diagnostics.DebuggerDisplayAttribute(""{GetString()}"")]
class C
{
    private string [|GetString|]() => """";
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task DebuggerDisplayAttribute_OnType_ReferencesProperty()
        {
            await TestMissingInRegularAndScriptAsync(
@"[System.Diagnostics.DebuggerDisplayAttribute(""{MyString}"")]
class C
{
    private string [|MyString|] => """";
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task DebuggerDisplayAttribute_OnField_ReferencesField()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    private string [|s|];

    [System.Diagnostics.DebuggerDisplayAttribute(""{s}"")]
    public int M;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task DebuggerDisplayAttribute_OnProperty_ReferencesMethod()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    private string [|GetString|]() => """";

    [System.Diagnostics.DebuggerDisplayAttribute(""{GetString()}"")]
    public int M => 0;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task DebuggerDisplayAttribute_OnProperty_ReferencesProperty()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    private string [|MyString|] { get { return """"; } }

    [System.Diagnostics.DebuggerDisplayAttribute(""{MyString}"")]
    public int M { get { return 0; } }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task DebuggerDisplayAttribute_OnNestedTypeMember_ReferencesField()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    private static string [|s|];

    class Nested
    {
        [System.Diagnostics.DebuggerDisplayAttribute(""{C.s}"")]
        public int M;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
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
    private int {|FixAllInProject:f1|}, f2 = 0, f3;
    private void M1() { }
    private int P1 => 0;
    private int this[int x] { get { return 0; } set { } }
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
