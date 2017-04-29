// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.MakeFieldReadonly;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MakeFieldReadonly
{
    public class MakeFieldReadonlyTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpMakeFieldReadonlyDiagnosticAnalyzer(), new CSharpMakeFieldReadonlyCodeFixProvider());

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly),
        InlineData("public"),
        InlineData("internal"),
        InlineData("protected"),
        InlineData("protected internal")]
        public async Task NonPrivateField(string accessibility)
        {
            await TestMissingInRegularAndScriptAsync(
$@"class MyClass
{{
    {accessibility} int[| _foo |];
}}");
        }
        
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldIsEvent()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private event System.EventHandler [|Foo|];
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldIsReadonly()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private readonly int [|_foo|];
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldNotAssigned()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_foo|];
}",
@"class MyClass
{
    private readonly int _foo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldNotAssigned_Struct()
        {
            await TestInRegularAndScriptAsync(
@"struct MyStruct
{
    private int [|_foo|];
}",
@"struct MyStruct
{
    private readonly int _foo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedInline()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_foo|] = 0;
}",
@"class MyClass
{
    private readonly int _foo = 0;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task MultipleFieldsAssignedInline_AllCanBeReadonly()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_foo|] = 0, _bar = 0;
}",
@"class MyClass
{
    private readonly int _foo = 0;
    private int _bar = 0;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task ThreeFieldsAssignedInline_AllCanBeReadonly_SeparatesAllAndKeepsThemInOrder()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int _foo = 0, [|_bar|] = 0, _fizz = 0;
}",
@"class MyClass
{
    private int _foo = 0;
    private readonly int _bar = 0;
    private int _fizz = 0;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task MultipleFieldsAssignedInline_OneIsAssignedInMethod()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int _foo = 0, [|_bar|] = 0;
    Foo()
    {
        _foo = 0;
    }
}",
@"class MyClass
{
    private int _foo = 0;
    private readonly int _bar = 0;
    Foo()
    {
        _foo = 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task MultipleFieldsAssignedInline_NoInitializer()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_foo|], _bar = 0;
}",
@"class MyClass
{
    private readonly int _foo;
    private int _bar = 0;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedInCtor()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_foo|];
    MyClass()
    {
        _foo = 0;
    }
}",
@"class MyClass
{
    private readonly int _foo;
    MyClass()
    {
        _foo = 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedInLambdaInCtor()
        {
            await TestMissingInRegularAndScriptAsync(
@"public class MyClass
{
    private int [|_foo|];
    public MyClass()
    {
        this.E += (_, __) => this._foo = 0;
    }

    public event EventHandler E;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedInLambdaWithBlockInCtor()
        {
            await TestMissingInRegularAndScriptAsync(
@"public class MyClass
{
    private int [|_foo|];
    public MyClass()
    {
        this.E += (_, __) => { this._foo = 0; }
    }

    public event EventHandler E;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedInCtor_DifferentInstance()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_foo|];
    MyClass()
    {
        var foo = new MyClass();
        foo._foo = 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedInCtor_DifferentInstance_ObjectInitializer()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_foo|];
    MyClass()
    {
        var foo = new MyClass { _foo = 0 };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedInCtor_QualifiedWithThis()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_foo|];
    MyClass()
    {
        this._foo = 0;
    }
}",
@"class MyClass
{
    private readonly int _foo;
    MyClass()
    {
        this._foo = 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldReturnedInProperty()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_foo|];
    int Foo
    {
        get { return _foo; }
    }
}",
@"class MyClass
{
    private readonly int _foo;
    int Foo
    {
        get { return _foo; }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedInProperty()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_foo|];
    int Foo
    {
        get { return _foo; }
        set { _foo = value; }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedInMethod()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_foo|];
    int Foo()
    {
        _foo = 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task VariableAssignedToFieldInMethod()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_foo|];
    int Foo()
    {
        var i = _foo;
    }
}",
@"class MyClass
{
    private readonly int _foo;
    int Foo()
    {
        var i = _foo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedInMethodWithCompoundOperator()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_foo|] = 0;
    int Foo(int value)
    {
        _foo += value;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldUsedWithPostfixIncrement()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_foo|] = 0;
    int Foo(int value)
    {
        _foo++;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldUsedWithPrefixDecrement()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_foo|] = 0;
    int Foo(int value)
    {
        --_foo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task AssignedInPartialClass()
        {
            await TestMissingInRegularAndScriptAsync(
@"partial class MyClass
{
    private int [|_foo|];
}
partial class MyClass
{
    void SetFoo()
    {
        _foo = 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task PassedAsParameter()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_foo|];
    void Foo()
    {
        Bar(_foo);
    }
    void Bar(int foo)
    {
    }
}",
@"class MyClass
{
    private readonly int _foo;
    void Foo()
    {
        Bar(_foo);
    }
    void Bar(int foo)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task PassedAsOutParameter()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_foo|];
    void Foo()
    {
        int.TryParse(""123"", out _foo);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task PassedAsRefParameter()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_foo|];
    void Foo()
    {
        Bar(ref _foo);
    }
    void Bar(ref int foo)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task PassedAsOutParameterInCtor()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_foo|];
    MyClass()
    {
        int.TryParse(""123"", out _foo);
    }
}",
@"class MyClass
{
    private readonly int _foo;
    MyClass()
    {
        int.TryParse(""123"", out _foo);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task PassedAsRefParameterInCtor()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_foo|];
    MyClass()
    {
        Bar(ref _foo);
    }
    void Bar(ref int foo)
    {
    }
}",
@"class MyClass
{
    private readonly int _foo;
    MyClass()
    {
        Bar(ref _foo);
    }
    void Bar(ref int foo)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task StaticFieldAssignedInStaticCtor()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private static int [|_foo|];
    static MyClass()
    {
        _foo = 0;
    }
}",
@"class MyClass
{
    private static readonly int _foo;
    static MyClass()
    {
        _foo = 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task StaticFieldAssignedInNonStaticCtor()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private static int [|_foo|];
    MyClass()
    {
        _foo = 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FixAll()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int {|FixAllInDocument:_foo|} = 0, _bar = 0;
    private int _fizz = 0;
}",
@"class MyClass
{
    private readonly int _foo = 0, _bar = 0;
    private readonly int _fizz = 0;
}");
        }
    }
}