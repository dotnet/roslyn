// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.MakeFieldReadonly;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.MakeFieldReadonly;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MakeFieldReadonly
{
    public class MakeFieldReadonlyTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new MakeFieldReadonlyDiagnosticAnalyzer(), new CSharpMakeFieldReadonlyCodeFixProvider());

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
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

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        [InlineData("")]
        [InlineData("\r\n")]
        [InlineData("\r\n\r\n")]
        public async Task MultipleFieldsAssignedInline_LeadingCommentAndWhitespace(string leadingTrvia)
        {
            await TestInRegularAndScriptAsync(
$@"class MyClass
{{
    //Comment{leadingTrvia}
    private int _goo = 0, [|_bar|] = 0;
}}",
$@"class MyClass
{{
    //Comment{leadingTrvia}
    private int _goo = 0;
    private readonly int _bar = 0;
}}");
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
        [WorkItem(29746, "https://github.com/dotnet/roslyn/issues/29746")]
        public async Task FieldReturnedInMethod()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private string [|_s|];
    public MyClass(string s) => _s = s;
    public string Method()
    {
        return _s;
    }
}",
@"class MyClass
{
    private readonly string [|_s|];
    public MyClass(string s) => _s = s;
    public string Method()
    {
        return _s;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        [WorkItem(29746, "https://github.com/dotnet/roslyn/issues/29746")]
        public async Task FieldReadInMethod()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private string [|_s|];
    public MyClass(string s) => _s = s;
    public string Method()
    {
        return _s.ToUpper();
    }
}",
@"class MyClass
{
    private readonly string [|_s|];
    public MyClass(string s) => _s = s;
    public string Method()
    {
        return _s.ToUpper();
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
        public async Task FieldInNestedTypeAssignedInConstructor()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    class NestedType
    {
        private int [|_goo|];

        public NestedType()
        {
            _goo = 0;
        }
    }
}",
@"class MyClass
{
    class NestedType
    {
        private readonly int _goo;

        public NestedType()
        {
            _goo = 0;
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
        public async Task NotAssignedInPartialClass1()
        {
            await TestInRegularAndScriptAsync(
@"partial class MyClass
{
    private int [|_goo|];
}",
@"partial class MyClass
{
    private readonly int _goo;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task NotAssignedInPartialClass2()
        {
            await TestInRegularAndScriptAsync(
@"partial class MyClass
{
    private int [|_goo|];
}
partial class MyClass
{
}",
@"partial class MyClass
{
    private readonly int _goo;
}
partial class MyClass
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task NotAssignedInPartialClass3()
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
    void M()
    {
    }
}
        </Document>
    </Project>
</Workspace>",
@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>partial class MyClass
{
    private readonly int _goo;
}
        </Document>
        <Document>partial class MyClass
{
    void M()
    {
    }
}
        </Document>
    </Project>
</Workspace>");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task AssignedInPartialClass1()
        {
            await TestMissingInRegularAndScriptAsync(
@"partial class MyClass
{
    private int [|_goo|];

    void SetGoo()
    {
        _goo = 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task AssignedInPartialClass2()
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
        public async Task AssignedInPartialClass3()
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
    void SetGoo()
    {
        _goo = 0;
    }
}
        </Document>
    </Project>
</Workspace>");
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
    private int _x = 0, _y = 0, _z = 0;
    private int _fizz = 0;

    void Method() { _z = 1; }
}",
@"class MyClass
{
    private readonly int _goo = 0, _bar = 0;
    private readonly int _x = 0;
    private readonly int _y = 0;
    private int _z = 0;
    private readonly int _fizz = 0;

    void Method() { _z = 1; }
}");
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FixAll2()
        {
            await TestInRegularAndScriptAsync(
@"  using System;

    partial struct MyClass
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
@"  using System;

    partial struct MyClass
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

        [WorkItem(26262, "https://github.com/dotnet/roslyn/issues/26262")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedInCtor_InParens()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    MyClass()
    {
        (_goo) = 0;
    }
}",
@"class MyClass
{
    private readonly int _goo;
    MyClass()
    {
        (_goo) = 0;
    }
}");
        }

        [WorkItem(26262, "https://github.com/dotnet/roslyn/issues/26262")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedInCtor_QualifiedWithThis_InParens()
        {
            await TestInRegularAndScriptAsync(
@"class MyClass
{
    private int [|_goo|];
    MyClass()
    {
        (this._goo) = 0;
    }
}",
@"class MyClass
{
    private readonly int _goo;
    MyClass()
    {
        (this._goo) = 0;
    }
}");
        }

        [WorkItem(26264, "https://github.com/dotnet/roslyn/issues/26264")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedInMethod_InDeconstruction()
        {
            await TestMissingAsync(
@"class C
{
    [|int i;|]
    int j;

    void M()
    {
        (i, j) = (1, 2);
    }
}");
        }

        [WorkItem(26264, "https://github.com/dotnet/roslyn/issues/26264")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedInMethod_InDeconstruction_InParens()
        {
            await TestMissingAsync(
@"class C
{
    [|int i;|]
    int j;

    void M()
    {
        ((i, j), j) = ((1, 2), 3);
    }
}");
        }

        [WorkItem(26264, "https://github.com/dotnet/roslyn/issues/26264")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedInMethod_InDeconstruction_WithThis_InParens()
        {
            await TestMissingAsync(
@"class C
{
    [|int i;|]
    int j;

    void M()
    {
        ((this.i, j), j) = (1, 2);
    }
}");
        }

        [WorkItem(26264, "https://github.com/dotnet/roslyn/issues/26264")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldUsedInTupleExpressionOnRight()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    [|int i;|]
    int j;

    void M()
    {
        (j, j) = (i, i);
    }
}",
@"class C
{
    readonly int i;
    int j;

    void M()
    {
        (j, j) = (i, i);
    }
}");
        }

        [WorkItem(26264, "https://github.com/dotnet/roslyn/issues/26264")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldInTypeWithGeneratedCode()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    [|private int i;|]

    [System.CodeDom.Compiler.GeneratedCodeAttribute("""", """")]
    private int j;

    void M()
    {
    }
}",
@"class C
{
    private readonly int i;

    [System.CodeDom.Compiler.GeneratedCodeAttribute("""", """")]
    private int j;

    void M()
    {
    }
}");
        }

        [WorkItem(26364, "https://github.com/dotnet/roslyn/issues/26364")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldIsFixed()
        {
            await TestMissingInRegularAndScriptAsync(
@"unsafe struct S
{
    [|private fixed byte b[8];|]
}");
        }

        [WorkItem(38995, "https://github.com/dotnet/roslyn/issues/38995")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedToLocalRef()
        {
            await TestMissingAsync(
@"
class Program
{
    [|int i;|]

    void M()
    {
        ref var value = ref i;
        value += 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
        public async Task FieldAssignedToLocalReadOnlyRef()
        {
            await TestInRegularAndScriptAsync(
@"
class Program
{
    [|int i;|]

    void M()
    {
        ref readonly var value = ref i;
    }
}",
@"
class Program
{
    [|readonly int i;|]

    void M()
    {
        ref readonly var value = ref i;
    }
}");
        }
    }
}
