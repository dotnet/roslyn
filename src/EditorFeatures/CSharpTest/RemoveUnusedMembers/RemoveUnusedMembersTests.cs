// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.RemoveUnusedMembers;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using static Roslyn.Test.Utilities.TestHelpers;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnusedMembers
{
    public class RemoveUnusedMembersTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpRemoveUnusedMembersDiagnosticAnalyzer(), new CSharpRemoveUnusedMembersCodeFixProvider());

        // Ensure that we explicitly test missing IDE0052, which has no corresponding code fix (non-fixable diagnostic).
        private Task TestDiagnosticMissingAsync(string initialMarkup)
            => TestDiagnosticMissingAsync(initialMarkup, new TestParameters(retainNonFixableDiagnostics: true));

        [Fact, WorkItem(31582, "https://github.com/dotnet/roslyn/issues/31582")]
        public async Task FieldReadViaSuppression()
        {
            await TestDiagnosticMissingAsync(@"
#nullable enable
class MyClass
{
    string? [|_field|] = null;
    void M()
    {
        _field!.ToString();
    }
}", new TestParameters(retainNonFixableDiagnostics: true, parseOptions: new CSharpParseOptions(LanguageVersion.CSharp8)));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [InlineData("public")]
        [InlineData("internal")]
        [InlineData("protected")]
        [InlineData("protected internal")]
        [InlineData("private protected")]
        public async Task NonPrivateField(string accessibility)
        {
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    static [|MyClass()|] { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task DestructorIsNotFlagged()
        {
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private static void [|Main|]() { }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task EntryPointMethodNotFlagged_02()
        {
            await TestDiagnosticMissingAsync(
@"using System.Threading.Tasks;

class MyClass
{
    private static async Task [|Main|]() => await Task.CompletedTask;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task EntryPointMethodNotFlagged_03()
        {
            await TestDiagnosticMissingAsync(
@"using System.Threading.Tasks;

class MyClass
{
    private static async Task<int> [|Main|]() => await Task.FromResult(0);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task EntryPointMethodNotFlagged_04()
        {
            await TestDiagnosticMissingAsync(
@"using System.Threading.Tasks;

class MyClass
{
    private static Task [|Main|]() => Task.CompletedTask;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(31572, "https://github.com/dotnet/roslyn/issues/31572")]
        public async Task EntryPointMethodNotFlagged_05()
        {
            await TestDiagnosticMissingAsync(
@"using System.Threading.Tasks;

class MyClass
{
    private static int [|Main|]() => 0;
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
@"class C
{
    protected abstract void [|M|]();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodIsUnused_InterfaceMethod()
        {
            await TestDiagnosticMissingAsync(
@"interface I
{
    void [|M|]();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodIsUnused_ExplicitInterfaceImplementation()
        {
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
        [WorkItem(30965, "https://github.com/dotnet/roslyn/issues/30965")]
        public async Task EventIsUnused_ExplicitInterfaceImplementation()
        {
            await TestDiagnosticMissingAsync(
@"interface I
{
    event System.Action E;
}

class C : I
{
    event System.Action [|I.E|]
    {
        add { }
        remove { }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(30894, "https://github.com/dotnet/roslyn/issues/30894")]
        public async Task WriteOnlyProperty_NotWritten()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [|P|] { set { } }
}",
@"class C
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(30894, "https://github.com/dotnet/roslyn/issues/30894")]
        public async Task WriteOnlyProperty_Written()
        {
            await TestDiagnosticMissingAsync(
@"class C
{
    int [|P|] { set { } }
    void M(int i) => P = i;
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
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int [|_goo|];
    public int M() => _goo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_BlockBody()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int [|_goo|];
    public int M() { return _goo; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_ExpressionLambda()
        {
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int [|_goo|];
    public int M() => new MyClass()._goo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_ObjectInitializer()
        {
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int [|_goo|];
    int M() => this._goo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_Attribute()
        {
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int [|M1|] => 0
    public int M2() => M1();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodIsAddressTaken()
        {
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int [|M1|]<T>() => 0;
    private int M2() => M1<int>();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task GenericMethodIsInvoked_ImplicitTypeArguments()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private T [|M1|]<T>(T t) => t;
    private int M2() => M1(0);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodInGenericTypeIsInvoked_NoTypeArguments()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass<T>
{
    private int [|M1|]() => 0;
    private int M2() => M1();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodInGenericTypeIsInvoked_NonConstructedType()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass<T>
{
    private int [|M1|]() => 0;
    private int M2(MyClass<T> m) => m.M1();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task MethodInGenericTypeIsInvoked_ConstructedType()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass<T>
{
    private int [|M1|]() => 0;
    private int M2(MyClass<int> m) => m.M1();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task InstanceConstructorIsUsed_NoArguments()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private [|MyClass()|] { }
    public static readonly MyClass Instance = new MyClass();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task InstanceConstructorIsUsed_WithArguments()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private [|MyClass(int i)|] { }
    public static readonly MyClass Instance = new MyClass(0);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task PropertyIsRead()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int [|P|] => 0;
    public int M() => P;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task IndexerIsRead()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int [|this|][int x] { get { return 0; } set { } }
    public int M(int x) => this[x];
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task EventIsRead()
        {
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
        [WorkItem(32488, "https://github.com/dotnet/roslyn/issues/32488")]
        public async Task FieldInNameOf()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int [|_goo|];
    private string _goo2 = nameof(_goo);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(33765, "https://github.com/dotnet/roslyn/issues/33765")]
        public async Task GenericFieldInNameOf()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass<T>
{
    private T [|_goo|];
    private string _goo2 = nameof(MyClass<int>._goo);
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(31581, "https://github.com/dotnet/roslyn/issues/31581")]
        public async Task MethodInNameOf()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private void [|M|]() { }
    private string _goo = nameof(M);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(33765, "https://github.com/dotnet/roslyn/issues/33765")]
        public async Task GenericMethodInNameOf()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass<T>
{
    private void [|M|]() { }
    private string _goo2 = nameof(MyClass<int>.M);
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(31581, "https://github.com/dotnet/roslyn/issues/31581")]
        public async Task PropertyInNameOf()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int [|P|] { get; }
    private string _goo = nameof(P);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(32522, "https://github.com/dotnet/roslyn/issues/32522")]
        public async Task TestDynamicInvocation()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private void [|M|](dynamic d) { }
    public void M2(dynamic d) => M(d);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(32522, "https://github.com/dotnet/roslyn/issues/32522")]
        public async Task TestDynamicObjectCreation()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private [|MyClass|](int i) { }
    public static MyClass Create(dynamic d) => new MyClass(d);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(32522, "https://github.com/dotnet/roslyn/issues/32522")]
        public async Task TestDynamicIndexerAccess()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int[] _list;
    private int [|this|][int index] => _list[index];
    public int M2(dynamic d) => this[d];
}");
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
        [WorkItem(33994, "https://github.com/dotnet/roslyn/issues/33994")]
        public async Task PropertyIsOnlyWritten()
        {
            var source =
@"class MyClass
{
    private int [|P|] { get; set; }
    public void M()
    {
        P = 0;
    }
}";
            var testParameters = new TestParameters(retainNonFixableDiagnostics: true);
            using (var workspace = CreateWorkspaceFromOptions(source, testParameters))
            {
                var diagnostics = await GetDiagnosticsAsync(workspace, testParameters).ConfigureAwait(false);
                diagnostics.Verify(Diagnostic("IDE0052", "P").WithLocation(3, 17));
                var expectedMessage = string.Format(FeaturesResources.Private_property_0_can_be_converted_to_a_method_as_its_get_accessor_is_never_invoked, "MyClass.P");
                Assert.Equal(expectedMessage, diagnostics.Single().GetMessage());
            }
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
        [WorkItem(30397, "https://github.com/dotnet/roslyn/issues/30397")]
        public async Task FieldIsIncrementedAndValueUsed()
        {
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int [|_goo|];
    public int M1() => ++_goo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(30397, "https://github.com/dotnet/roslyn/issues/30397")]
        public async Task FieldIsIncrementedAndValueUsed_02()
        {
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int [|_goo|];
    public int M1(int x) => _goo += x;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsTargetOfCompoundAssignmentAndValueUsed_02()
        {
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
@"class MyClass
{
    private int [|_goo|] = 0, _bar = 0;
    public int M() => _goo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsRead_InNestedType()
        {
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
@"class C
{
    private int [|i|];

    public int M() { return = ; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task FieldIsUnusedInType_SemanticError()
        {
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
@"[System.Diagnostics.DebuggerDisplayAttribute(""{s}"")]
class C
{
    private string [|s|];
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task DebuggerDisplayAttribute_OnType_ReferencesMethod()
        {
            await TestDiagnosticMissingAsync(
@"[System.Diagnostics.DebuggerDisplayAttribute(""{GetString()}"")]
class C
{
    private string [|GetString|]() => """";
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task DebuggerDisplayAttribute_OnType_ReferencesProperty()
        {
            await TestDiagnosticMissingAsync(
@"[System.Diagnostics.DebuggerDisplayAttribute(""{MyString}"")]
class C
{
    private string [|MyString|] => """";
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        public async Task DebuggerDisplayAttribute_OnField_ReferencesField()
        {
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
            await TestDiagnosticMissingAsync(
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
        [WorkItem(30886, "https://github.com/dotnet/roslyn/issues/30886")]
        public async Task SerializableConstructor_TypeImplementsISerializable()
        {
            await TestDiagnosticMissingAsync(
@"using System.Runtime.Serialization;

class C : ISerializable
{
    public C()
    {
    }

    private [|C|](SerializationInfo info, StreamingContext context)
    {
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(30886, "https://github.com/dotnet/roslyn/issues/30886")]
        public async Task SerializableConstructor_BaseTypeImplementsISerializable()
        {
            await TestDiagnosticMissingAsync(
@"using System;
using System.Runtime.Serialization;

class C : Exception 
{
    public C()
    {
    }

    private [|C|](SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
    }
}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [InlineData(@"[System.Runtime.Serialization.OnDeserializingAttribute]")]
        [InlineData(@"[System.Runtime.Serialization.OnDeserializedAttribute]")]
        [InlineData(@"[System.Runtime.Serialization.OnSerializingAttribute]")]
        [InlineData(@"[System.Runtime.Serialization.OnSerializedAttribute]")]
        [InlineData(@"[System.Runtime.InteropServices.ComRegisterFunctionAttribute]")]
        [InlineData(@"[System.Runtime.InteropServices.ComUnregisterFunctionAttribute]")]
        public async Task MethodsWithSpecialAttributes(string attribute)
        {
            await TestDiagnosticMissingAsync(
$@"class C
{{
    {attribute}
    private void [|M|]()
    {{
    }}
}}");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [InlineData("ShouldSerialize")]
        [InlineData("Reset")]
        [WorkItem(30887, "https://github.com/dotnet/roslyn/issues/30887")]
        public async Task ShouldSerializeOrResetPropertyMethod(string prefix)
        {
            await TestDiagnosticMissingAsync(
$@"class C
{{
    private bool [|{prefix}Data|]()
    {{
        return true;
    }}

    public int Data {{ get; private set; }}
}}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(30377, "https://github.com/dotnet/roslyn/issues/30377")]
        public async Task EventHandlerMethod()
        {
            await TestDiagnosticMissingAsync(
$@"using System;

class C
{{
    private void [|M|](object o, EventArgs args)
    {{
    }}
}}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(32727, "https://github.com/dotnet/roslyn/issues/32727")]
        public async Task NestedStructLayoutTypeWithReference()
        {
            await TestDiagnosticMissingAsync(
@"using System.Runtime.InteropServices;

class Program
{
    private const int [|MAX_PATH|] = 260;

    [StructLayout(LayoutKind.Sequential)]
    internal struct ProcessEntry32
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
        public string szExeFile;
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(32702, "https://github.com/dotnet/roslyn/issues/32702")]
        public async Task UsedExtensionMethod_ReferencedFromPartialMethod()
        {
            await TestDiagnosticMissingAsync(
@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
static partial class B
{
    static partial void PartialMethod();
}
        </Document>
        <Document>
static partial class B
{
    static partial void PartialMethod()
    {
        UsedMethod();
    }

    private static void [|UsedMethod|]() { }
}
        </Document>
    </Project>
</Workspace>");
        }

        [Fact, WorkItem(32842, "https://github.com/dotnet/roslyn/issues/32842")]
        public async Task FieldIsRead_NullCoalesceAssignment()
        {
            await TestDiagnosticMissingAsync(@"
public class MyClass
{
    private MyClass [|_field|];
    public MyClass Property => _field ??= new MyClass();
}", new TestParameters(retainNonFixableDiagnostics: true, parseOptions: new CSharpParseOptions(LanguageVersion.CSharp8)));
        }

        [Fact, WorkItem(32842, "https://github.com/dotnet/roslyn/issues/32842")]
        public async Task FieldIsNotRead_NullCoalesceAssignment()
        {
            await TestDiagnosticsAsync(@"
public class MyClass
{
    private MyClass [|_field|];
    public void M() => _field ??= new MyClass();
}", new TestParameters(retainNonFixableDiagnostics: true, parseOptions: new CSharpParseOptions(LanguageVersion.CSharp8)),
    expected: Diagnostic("IDE0052"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedMembers)]
        [WorkItem(37213, "https://github.com/dotnet/roslyn/issues/37213")]
        public async Task UsedPrivateExtensionMethod()
        {
            await TestDiagnosticMissingAsync(
@"public static class B
{
    public static void PublicExtensionMethod(this string s) => s.PrivateExtensionMethod();
    private static void [|PrivateExtensionMethod|](this string s) { }
}");
        }
    }
}
