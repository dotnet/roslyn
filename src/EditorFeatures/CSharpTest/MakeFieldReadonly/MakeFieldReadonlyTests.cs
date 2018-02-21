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

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        [InlineData("public")]
        [InlineData("internal")]
        [InlineData("protected")]
        [InlineData("protected internal")]
        public async Task NonPrivateField(string accessibility)
        {
            await TestMissingInRegularAndScriptAsync(
$@"class MyClass
{{
    {accessibility} int[| _goo |];
}}");
        }
        
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldIsEvent()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private event System.EventHandler [|Goo|];
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldIsReadonly()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private readonly int [|_goo|];
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldIsConst()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private const int [|_goo|];
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldNotAssigned()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
}",
@"class MyClass
{
    private readonly int _goo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldNotAssigned_Struct()
        {
            await TestInRegularAndScriptAsync(
@"struct MyStruct
{
    private int [|_goo|];
}",
@"struct MyStruct
{
    private readonly int _goo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedInline()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|] = 0;
}",
@"class MyClass
{
    private readonly int _goo = 0;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task MultipleFieldsAssignedInline_AllCanBeReadonly()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|] = 0, _bar = 0;
}",
@"class MyClass
{
    private readonly int _goo = 0;
    private int _bar = 0;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task ThreeFieldsAssignedInline_AllCanBeReadonly_SeparatesAllAndKeepsThemInOrder()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int _goo = 0, [|_bar|] = 0, _fizz = 0;
}",
@"class MyClass
{
    private int _goo = 0;
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
    private int _goo = 0, [|_bar|] = 0;
    Goo()
    {
        _goo = 0;
    }
}",
@"class MyClass
{
    private int _goo = 0;
    private readonly int _bar = 0;

    Goo()
    {
        _goo = 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task MultipleFieldsAssignedInline_NoInitializer()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|], _bar = 0;
}",
@"class MyClass
{
    private readonly int _goo;
    private int _bar = 0;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedInCtor()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    MyClass()
    {
        _goo = 0;
    }
}",
@"class MyClass
{
    private readonly int _goo;
    MyClass()
    {
        _goo = 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedInSimpleLambdaInCtor()
        {
            await TestMissingInRegularAndScriptAsync(
@"public class MyClass
{
    private int [|_goo|];
    public MyClass()
    {
        this.E = x => this._goo = 0;
    }

    public Action<int> E;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedInLambdaInCtor()
        {
            await TestMissingInRegularAndScriptAsync(
@"public class MyClass
{
    private int [|_goo|];
    public MyClass()
    {
        this.E += (_, __) => this._goo = 0;
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
    private int [|_goo|];
    public MyClass()
    {
        this.E += (_, __) => { this._goo = 0; }
    }

    public event EventHandler E;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedInAnonymousFunctionInCtor()
        {
            await TestMissingInRegularAndScriptAsync(
@"public class MyClass
{
    private int [|_goo|];
    public MyClass()
    {
        this.E = delegate { this._goo = 0; };
    }

    public Action<int> E;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedInLocalFunctionExpressionBodyInCtor()
        {
            await TestMissingInRegularAndScriptAsync(
@"public class MyClass
{
    private int [|_goo|];
    public MyClass()
    {
        void LocalFunction() => this._goo = 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedInLocalFunctionBlockBodyInCtor()
        {
            await TestMissingInRegularAndScriptAsync(
@"public class MyClass
{
    private int [|_goo|];
    public MyClass()
    {
        void LocalFunction() { this._goo = 0; }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedInCtor_DifferentInstance()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    MyClass()
    {
        var goo = new MyClass();
        goo._goo = 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedInCtor_DifferentInstance_ObjectInitializer()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    MyClass()
    {
        var goo = new MyClass { _goo = 0 };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedInCtor_QualifiedWithThis()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    MyClass()
    {
        this._goo = 0;
    }
}",
@"class MyClass
{
    private readonly int _goo;
    MyClass()
    {
        this._goo = 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldReturnedInProperty()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    int Goo
    {
        get { return _goo; }
    }
}",
@"class MyClass
{
    private readonly int _goo;
    int Goo
    {
        get { return _goo; }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedInProperty()
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedInMethod()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    int Goo()
    {
        _goo = 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedInNestedTypeConstructor()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];

    class Derived : MyClass
    {
        Derived()
        {
            _goo = 1;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedInNestedTypeMethod()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];

    class Derived : MyClass
    {
        void Method()
        {
            _goo = 1;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task VariableAssignedToFieldInMethod()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    int Goo()
    {
        var i = _goo;
    }
}",
@"class MyClass
{
    private readonly int _goo;
    int Goo()
    {
        var i = _goo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedInMethodWithCompoundOperator()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|] = 0;
    int Goo(int value)
    {
        _goo += value;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldUsedWithPostfixIncrement()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|] = 0;
    int Goo(int value)
    {
        _goo++;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldUsedWithPrefixDecrement()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|] = 0;
    int Goo(int value)
    {
        --_goo;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task AssignedInPartialClass()
        {
            await TestMissingInRegularAndScriptAsync(
@"partial class MyClass
{
    private int [|_goo|];
}
partial class MyClass
{
    void SetGoo()
    {
        _goo = 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task PassedAsParameter()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    void Goo()
    {
        Bar(_goo);
    }
    void Bar(int goo)
    {
    }
}",
@"class MyClass
{
    private readonly int _goo;
    void Goo()
    {
        Bar(_goo);
    }
    void Bar(int goo)
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
    private int [|_goo|];
    void Goo()
    {
        int.TryParse(""123"", out _goo);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task PassedAsRefParameter()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    void Goo()
    {
        Bar(ref _goo);
    }
    void Bar(ref int goo)
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
    private int [|_goo|];
    MyClass()
    {
        int.TryParse(""123"", out _goo);
    }
}",
@"class MyClass
{
    private readonly int _goo;
    MyClass()
    {
        int.TryParse(""123"", out _goo);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task PassedAsRefParameterInCtor()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    MyClass()
    {
        Bar(ref _goo);
    }
    void Bar(ref int goo)
    {
    }
}",
@"class MyClass
{
    private readonly int _goo;
    MyClass()
    {
        Bar(ref _goo);
    }
    void Bar(ref int goo)
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
    private static int [|_goo|];
    static MyClass()
    {
        _goo = 0;
    }
}",
@"class MyClass
{
    private static readonly int _goo;
    static MyClass()
    {
        _goo = 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task StaticFieldAssignedInNonStaticCtor()
        {
            await TestMissingInRegularAndScriptAsync(
@"class MyClass
{
    private static int [|_goo|];
    MyClass()
    {
        _goo = 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldTypeIsMutableStruct()
        {
            await TestMissingInRegularAndScriptAsync(
@"struct MyStruct
{
    private int _goo;
}
class MyClass
{
    private MyStruct [|_goo|];
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldTypeIsCustomImmutableStruct()
        {
            await TestInRegularAndScriptAsync(
@"struct MyStruct
{
    private readonly int _goo;
    private const int _bar = 0;
    private static int _fizz;
}
class MyClass
{
    private MyStruct [|_goo|];
}",
@"struct MyStruct
{
    private readonly int _goo;
    private const int _bar = 0;
    private static int _fizz;
}
class MyClass
{
    private readonly MyStruct _goo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FixAll()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int {|FixAllInDocument:_goo|} = 0, _bar = 0;
    private int _fizz = 0;
}",
@"class MyClass
{
    private readonly int _goo = 0, _bar = 0;
    private readonly int _fizz = 0;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FixAll2()
        {
            await TestInRegularAndScriptAsync(
@"  partial struct MyClass
    {
        private static Func<int, bool> {|FixAllInDocument:_test1|} = x => x > 0;
        private static Func<int, bool> _test2 = x => x < 0;

        private static Func<int, bool> _test3 = x =>
        {
            return x == 0;
        };

        private static Func<int, bool> _test4 = x =>
        {
            return x != 0;
        };
    }

    partial struct MyClass { }",
@"  partial struct MyClass
    {
        private static readonly Func<int, bool> _test1 = x => x > 0;
        private static readonly Func<int, bool> _test2 = x => x < 0;

        private static readonly Func<int, bool> _test3 = x =>
        {
            return x == 0;
        };

        private static readonly Func<int, bool> _test4 = x =>
        {
            return x != 0;
        };
    }

    partial struct MyClass { }");
        }
    }
}
