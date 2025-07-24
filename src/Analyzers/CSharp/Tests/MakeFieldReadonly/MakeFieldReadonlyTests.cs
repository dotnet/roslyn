// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Analyzers.MakeFieldReadonly;
using Microsoft.CodeAnalysis.CSharp.MakeFieldReadonly;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MakeFieldReadonly;

[Trait(Traits.Feature, Traits.Features.CodeActionsMakeFieldReadonly)]
public sealed class MakeFieldReadonlyTests(ITestOutputHelper logger)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    private static readonly ParseOptions s_strictFeatureFlag = CSharpParseOptions.Default.WithFeatures([KeyValuePair.Create("strict", "true")]);

    private const string s_inlineArrayAttribute = """
        namespace System.Runtime.CompilerServices
        {
            [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
            public sealed class InlineArrayAttribute : Attribute
            {
                public InlineArrayAttribute (int length)
                {
                    Length = length;
                }
            
                public int Length { get; }
            }
        }
        """;

    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpMakeFieldReadonlyDiagnosticAnalyzer(), new CSharpMakeFieldReadonlyCodeFixProvider());

    [Theory]
    [InlineData("public")]
    [InlineData("internal")]
    [InlineData("protected")]
    [InlineData("protected internal")]
    [InlineData("private protected")]
    public Task NonPrivateField(string accessibility)
        => TestMissingInRegularAndScriptAsync(
            $$"""
            class MyClass
            {
                {{accessibility}} int[| _goo |];
            }
            """);

    [Fact]
    public Task FieldIsEvent()
        => TestMissingInRegularAndScriptAsync(
            """
            class MyClass
            {
                private event System.EventHandler [|Goo|];
            }
            """);

    [Fact]
    public Task FieldIsReadonly()
        => TestMissingInRegularAndScriptAsync(
            """
            class MyClass
            {
                private readonly int [|_goo|];
            }
            """);

    [Fact]
    public Task FieldIsConst()
        => TestMissingInRegularAndScriptAsync(
            """
            class MyClass
            {
                private const int [|_goo|];
            }
            """);

    [Fact]
    public Task FieldNotAssigned()
        => TestInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_goo|];
            }
            """,
            """
            class MyClass
            {
                private readonly int _goo;
            }
            """);

    [Fact]
    public Task FieldNotAssigned_Struct()
        => TestInRegularAndScriptAsync(
            """
            struct MyStruct
            {
                private int [|_goo|];
            }
            """,
            """
            struct MyStruct
            {
                private readonly int _goo;
            }
            """);

    [Fact]
    public Task FieldAssignedInline()
        => TestInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_goo|] = 0;
            }
            """,
            """
            class MyClass
            {
                private readonly int _goo = 0;
            }
            """);

    [Fact]
    public Task MultipleFieldsAssignedInline_AllCanBeReadonly()
        => TestInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_goo|] = 0, _bar = 0;
            }
            """,
            """
            class MyClass
            {
                private readonly int _goo = 0;
                private int _bar = 0;
            }
            """);

    [Fact]
    public Task ThreeFieldsAssignedInline_AllCanBeReadonly_SeparatesAllAndKeepsThemInOrder()
        => TestInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int _goo = 0, [|_bar|] = 0, _fizz = 0;
            }
            """,
            """
            class MyClass
            {
                private int _goo = 0;
                private readonly int _bar = 0;
                private int _fizz = 0;
            }
            """);

    [Fact]
    public Task MultipleFieldsAssignedInline_OneIsAssignedInMethod()
        => TestInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int _goo = 0, [|_bar|] = 0;
                Goo()
                {
                    _goo = 0;
                }
            }
            """,
            """
            class MyClass
            {
                private int _goo = 0;
                private readonly int _bar = 0;

                Goo()
                {
                    _goo = 0;
                }
            }
            """);

    [Fact]
    public Task MultipleFieldsAssignedInline_NoInitializer()
        => TestInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_goo|], _bar = 0;
            }
            """,
            """
            class MyClass
            {
                private readonly int _goo;
                private int _bar = 0;
            }
            """);

    [Theory]
    [InlineData("")]
    [InlineData("\r\n")]
    [InlineData("\r\n\r\n")]
    public Task MultipleFieldsAssignedInline_LeadingCommentAndWhitespace(string leadingTrvia)
        => TestInRegularAndScriptAsync(
            $$"""
            class MyClass
            {
                //Comment{{leadingTrvia}}
                private int _goo = 0, [|_bar|] = 0;
            }
            """,
            $$"""
            class MyClass
            {
                //Comment{{leadingTrvia}}
                private int _goo = 0;
                private readonly int _bar = 0;
            }
            """);

    [Fact]
    public Task FieldAssignedInCtor()
        => TestInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_goo|];
                MyClass()
                {
                    _goo = 0;
                }
            }
            """,
            """
            class MyClass
            {
                private readonly int _goo;
                MyClass()
                {
                    _goo = 0;
                }
            }
            """);

    [Fact]
    public Task FieldAssignedInSimpleLambdaInCtor()
        => TestMissingInRegularAndScriptAsync(
            """
            public class MyClass
            {
                private int [|_goo|];
                public MyClass()
                {
                    this.E = x => this._goo = 0;
                }

                public Action<int> E;
            }
            """);

    [Fact]
    public Task FieldAssignedInLambdaInCtor()
        => TestMissingInRegularAndScriptAsync(
            """
            public class MyClass
            {
                private int [|_goo|];
                public MyClass()
                {
                    this.E += (_, __) => this._goo = 0;
                }

                public event EventHandler E;
            }
            """);

    [Fact]
    public Task FieldAssignedInLambdaWithBlockInCtor()
        => TestMissingInRegularAndScriptAsync(
            """
            public class MyClass
            {
                private int [|_goo|];
                public MyClass()
                {
                    this.E += (_, __) => { this._goo = 0; }
                }

                public event EventHandler E;
            }
            """);

    [Fact]
    public Task FieldAssignedInAnonymousFunctionInCtor()
        => TestMissingInRegularAndScriptAsync(
            """
            public class MyClass
            {
                private int [|_goo|];
                public MyClass()
                {
                    this.E = delegate { this._goo = 0; };
                }

                public Action<int> E;
            }
            """);

    [Fact]
    public Task FieldAssignedInLocalFunctionExpressionBodyInCtor()
        => TestMissingInRegularAndScriptAsync(
            """
            public class MyClass
            {
                private int [|_goo|];
                public MyClass()
                {
                    void LocalFunction() => this._goo = 0;
                }
            }
            """);

    [Fact]
    public Task FieldAssignedInLocalFunctionBlockBodyInCtor()
        => TestMissingInRegularAndScriptAsync(
            """
            public class MyClass
            {
                private int [|_goo|];
                public MyClass()
                {
                    void LocalFunction() { this._goo = 0; }
                }
            }
            """);

    [Fact]
    public Task FieldAssignedInCtor_DifferentInstance()
        => TestMissingInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_goo|];
                MyClass()
                {
                    var goo = new MyClass();
                    goo._goo = 0;
                }
            }
            """);

    [Fact]
    public Task FieldAssignedInCtor_DifferentInstance_ObjectInitializer()
        => TestMissingInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_goo|];
                MyClass()
                {
                    var goo = new MyClass { _goo = 0 };
                }
            }
            """);

    [Fact]
    public Task FieldAssignedInCtor_QualifiedWithThis()
        => TestInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_goo|];
                MyClass()
                {
                    this._goo = 0;
                }
            }
            """,
            """
            class MyClass
            {
                private readonly int _goo;
                MyClass()
                {
                    this._goo = 0;
                }
            }
            """);

    [Fact]
    public Task FieldReturnedInProperty()
        => TestInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_goo|];
                int Goo
                {
                    get { return _goo; }
                }
            }
            """,
            """
            class MyClass
            {
                private readonly int _goo;
                int Goo
                {
                    get { return _goo; }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29746")]
    public Task FieldReturnedInMethod()
        => TestInRegularAndScriptAsync(
            """
            class MyClass
            {
                private string [|_s|];
                public MyClass(string s) => _s = s;
                public string Method()
                {
                    return _s;
                }
            }
            """,
            """
            class MyClass
            {
                private readonly string [|_s|];
                public MyClass(string s) => _s = s;
                public string Method()
                {
                    return _s;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29746")]
    public Task FieldReadInMethod()
        => TestInRegularAndScriptAsync(
            """
            class MyClass
            {
                private string [|_s|];
                public MyClass(string s) => _s = s;
                public string Method()
                {
                    return _s.ToUpper();
                }
            }
            """,
            """
            class MyClass
            {
                private readonly string [|_s|];
                public MyClass(string s) => _s = s;
                public string Method()
                {
                    return _s.ToUpper();
                }
            }
            """);

    [Fact]
    public Task FieldAssignedInProperty()
        => TestMissingInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_goo|];
                int Goo
                {
                    get { return _goo; }
                    set { _goo = value; }
                }
            }
            """);

    [Fact]
    public Task FieldAssignedInMethod()
        => TestMissingInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_goo|];
                int Goo()
                {
                    _goo = 0;
                }
            }
            """);

    [Fact]
    public Task FieldAssignedInNestedTypeConstructor()
        => TestMissingInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_goo|];

                class Derived : MyClass
                {
                    Derived()
                    {
                        _goo = 1;
                    }
                }
            }
            """);

    [Fact]
    public Task FieldAssignedInNestedTypeMethod()
        => TestMissingInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_goo|];

                class Derived : MyClass
                {
                    void Method()
                    {
                        _goo = 1;
                    }
                }
            }
            """);

    [Fact]
    public Task FieldInNestedTypeAssignedInConstructor()
        => TestInRegularAndScriptAsync(
            """
            class MyClass
            {
                class NestedType
                {
                    private int [|_goo|];

                    public NestedType()
                    {
                        _goo = 0;
                    }
                }
            }
            """,
            """
            class MyClass
            {
                class NestedType
                {
                    private readonly int _goo;

                    public NestedType()
                    {
                        _goo = 0;
                    }
                }
            }
            """);

    [Fact]
    public Task VariableAssignedToFieldInMethod()
        => TestInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_goo|];
                int Goo()
                {
                    var i = _goo;
                }
            }
            """,
            """
            class MyClass
            {
                private readonly int _goo;
                int Goo()
                {
                    var i = _goo;
                }
            }
            """);

    [Fact]
    public Task FieldAssignedInMethodWithCompoundOperator()
        => TestMissingInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_goo|] = 0;
                int Goo(int value)
                {
                    _goo += value;
                }
            }
            """);

    [Fact]
    public Task FieldUsedWithPostfixIncrement()
        => TestMissingInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_goo|] = 0;
                int Goo(int value)
                {
                    _goo++;
                }
            }
            """);

    [Fact]
    public Task FieldUsedWithPrefixDecrement()
        => TestMissingInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_goo|] = 0;
                int Goo(int value)
                {
                    --_goo;
                }
            }
            """);

    [Fact]
    public Task NotAssignedInPartialClass1()
        => TestInRegularAndScriptAsync(
            """
            partial class MyClass
            {
                private int [|_goo|];
            }
            """,
            """
            partial class MyClass
            {
                private readonly int _goo;
            }
            """);

    [Fact]
    public Task NotAssignedInPartialClass2()
        => TestInRegularAndScriptAsync(
            """
            partial class MyClass
            {
                private int [|_goo|];
            }
            partial class MyClass
            {
            }
            """,
            """
            partial class MyClass
            {
                private readonly int _goo;
            }
            partial class MyClass
            {
            }
            """);

    [Fact]
    public Task NotAssignedInPartialClass3()
        => TestInRegularAndScriptAsync(
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
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
            </Workspace>
            """,
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
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
            </Workspace>
            """);

    [Fact]
    public Task AssignedInPartialClass1()
        => TestMissingInRegularAndScriptAsync(
            """
            partial class MyClass
            {
                private int [|_goo|];

                void SetGoo()
                {
                    _goo = 0;
                }
            }
            """);

    [Fact]
    public Task AssignedInPartialClass2()
        => TestMissingInRegularAndScriptAsync(
            """
            partial class MyClass
            {
                private int [|_goo|];
            }
            partial class MyClass
            {
                void SetGoo()
                {
                    _goo = 0;
                }
            }
            """);

    [Fact]
    public Task AssignedInPartialClass3()
        => TestMissingInRegularAndScriptAsync(
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
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
            </Workspace>
            """);

    [Fact]
    public Task PassedAsParameter()
        => TestInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_goo|];
                void Goo()
                {
                    Bar(_goo);
                }
                void Bar(int goo)
                {
                }
            }
            """,
            """
            class MyClass
            {
                private readonly int _goo;
                void Goo()
                {
                    Bar(_goo);
                }
                void Bar(int goo)
                {
                }
            }
            """);

    [Fact]
    public Task PassedAsOutParameter()
        => TestMissingInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_goo|];
                void Goo()
                {
                    int.TryParse("123", out _goo);
                }
            }
            """);

    [Fact]
    public Task PassedAsRefParameter()
        => TestMissingInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_goo|];
                void Goo()
                {
                    Bar(ref _goo);
                }
                void Bar(ref int goo)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33009")]
    public Task ReturnedByRef1()
        => TestMissingInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_goo|];
                internal ref int Goo()
                {
                    return ref _goo;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33009")]
    public Task ReturnedByRef2()
        => TestMissingInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_goo|];
                internal ref int Goo()
                    => ref _goo;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33009")]
    public Task ReturnedByRef3()
        => TestMissingInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_goo|];
                internal struct Accessor
                {
                    private MyClass _instance;
                    internal ref int Goo => ref _instance._goo;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33009")]
    public async Task ReturnedByRef4()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_a|];
                private int _b;
                internal ref int Goo(bool first)
                {
                    return ref (first ? ref _a : ref _b);
                }
            }
            """);

        await TestMissingInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int _a;
                private int [|_b|];
                internal ref int Goo(bool first)
                {
                    return ref (first ? ref _a : ref _b);
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33009")]
    public async Task ReturnedByRef5()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_a|];
                private int _b;
                internal ref int Goo(bool first)
                    => ref (first ? ref _a : ref _b);
            }
            """);

        await TestMissingInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int _a;
                private int [|_b|];
                internal ref int Goo(bool first)
                    => ref (first ? ref _a : ref _b);
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33009")]
    public Task ReturnedByRef6()
        => TestMissingInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_goo|];
                internal int Goo()
                {
                    return Local();

                    ref int Local()
                    {
                        return ref _goo;
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33009")]
    public Task ReturnedByRef7()
        => TestMissingInRegularAndScriptAsync(
            """
            class MyClass
            {
                delegate ref int D();

                private int [|_goo|];
                internal int Goo()
                {
                    D d = () => ref _goo;

                    return d();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33009")]
    public Task ReturnedByRef8()
        => TestMissingInRegularAndScriptAsync(
            """
            class MyClass
            {
                delegate ref int D();

                private int [|_goo|];
                internal int Goo()
                {
                    D d = delegate { return ref _goo; };

                    return d();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33009")]
    public Task ReturnedByRefReadonly1()
        => TestInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_goo|];
                internal ref readonly int Goo()
                {
                    return ref _goo;
                }
            }
            """,
            """
            class MyClass
            {
                private readonly int _goo;
                internal ref readonly int Goo()
                {
                    return ref _goo;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33009")]
    public Task ReturnedByRefReadonly2()
        => TestInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_goo|];
                internal ref readonly int Goo()
                    => ref _goo;
            }
            """,
            """
            class MyClass
            {
                private readonly int _goo;
                internal ref readonly int Goo()
                    => ref _goo;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33009")]
    public Task ReturnedByRefReadonly3()
        => TestInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_goo|];
                internal struct Accessor
                {
                    private MyClass _instance;
                    internal ref readonly int Goo => ref _instance._goo;
                }
            }
            """,
            """
            class MyClass
            {
                private readonly int _goo;
                internal struct Accessor
                {
                    private MyClass _instance;
                    internal ref readonly int Goo => ref _instance._goo;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33009")]
    public async Task ReturnedByRefReadonly4()
    {
        await TestInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_a|];
                private int _b;
                internal ref readonly int Goo(bool first)
                {
                    return ref (first ? ref _a : ref _b);
                }
            }
            """,
            """
            class MyClass
            {
                private readonly int _a;
                private int _b;
                internal ref readonly int Goo(bool first)
                {
                    return ref (first ? ref _a : ref _b);
                }
            }
            """);

        await TestInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int _a;
                private int [|_b|];
                internal ref readonly int Goo(bool first)
                {
                    return ref (first ? ref _a : ref _b);
                }
            }
            """,
            """
            class MyClass
            {
                private int _a;
                private readonly int _b;
                internal ref readonly int Goo(bool first)
                {
                    return ref (first ? ref _a : ref _b);
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33009")]
    public async Task ReturnedByRefReadonly5()
    {
        await TestInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_a|];
                private int _b;
                internal ref readonly int Goo(bool first)
                    => ref (first ? ref _a : ref _b);
            }
            """,
            """
            class MyClass
            {
                private readonly int _a;
                private int _b;
                internal ref readonly int Goo(bool first)
                    => ref (first ? ref _a : ref _b);
            }
            """);

        await TestInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int _a;
                private int [|_b|];
                internal ref readonly int Goo(bool first)
                    => ref (first ? ref _a : ref _b);
            }
            """,
            """
            class MyClass
            {
                private int _a;
                private readonly int _b;
                internal ref readonly int Goo(bool first)
                    => ref (first ? ref _a : ref _b);
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33009")]
    public Task ReturnedByRefReadonly6()
        => TestInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_goo|];
                internal int Goo()
                {
                    return Local();

                    ref readonly int Local()
                    {
                        return ref _goo;
                    }
                }
            }
            """,
            """
            class MyClass
            {
                private readonly int _goo;
                internal int Goo()
                {
                    return Local();

                    ref readonly int Local()
                    {
                        return ref _goo;
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33009")]
    public Task ReturnedByRefReadonly7()
        => TestInRegularAndScriptAsync(
            """
            class MyClass
            {
                delegate ref readonly int D();

                private int [|_goo|];
                internal int Goo()
                {
                    D d = () => ref _goo;

                    return d();
                }
            }
            """,
            """
            class MyClass
            {
                delegate ref readonly int D();

                private readonly int _goo;
                internal int Goo()
                {
                    D d = () => ref _goo;

                    return d();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33009")]
    public Task ReturnedByRefReadonly8()
        => TestInRegularAndScriptAsync(
            """
            class MyClass
            {
                delegate ref readonly int D();

                private int [|_goo|];
                internal int Goo()
                {
                    D d = delegate { return ref _goo; };

                    return d();
                }
            }
            """,
            """
            class MyClass
            {
                delegate ref readonly int D();

                private readonly int _goo;
                internal int Goo()
                {
                    D d = delegate { return ref _goo; };

                    return d();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33009")]
    public Task ConditionOfRefConditional1()
        => TestInRegularAndScriptAsync(
            """
            class MyClass
            {
                private bool [|_a|];
                private int _b;
                internal ref int Goo()
                {
                    return ref (_a ? ref _b : ref _b);
                }
            }
            """,
            """
            class MyClass
            {
                private readonly bool _a;
                private int _b;
                internal ref int Goo()
                {
                    return ref (_a ? ref _b : ref _b);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33009")]
    public Task ConditionOfRefConditional2()
        => TestInRegularAndScriptAsync(
            """
            class MyClass
            {
                private bool [|_a|];
                private int _b;
                internal ref int Goo()
                    => ref (_a ? ref _b : ref _b);
            }
            """,
            """
            class MyClass
            {
                private readonly bool _a;
                private int _b;
                internal ref int Goo()
                    => ref (_a ? ref _b : ref _b);
            }
            """);

    [Fact]
    public Task PassedAsOutParameterInCtor()
        => TestInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_goo|];
                MyClass()
                {
                    int.TryParse("123", out _goo);
                }
            }
            """,
            """
            class MyClass
            {
                private readonly int _goo;
                MyClass()
                {
                    int.TryParse("123", out _goo);
                }
            }
            """);

    [Fact]
    public Task PassedAsRefParameterInCtor()
        => TestInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_goo|];
                MyClass()
                {
                    Bar(ref _goo);
                }
                void Bar(ref int goo)
                {
                }
            }
            """,
            """
            class MyClass
            {
                private readonly int _goo;
                MyClass()
                {
                    Bar(ref _goo);
                }
                void Bar(ref int goo)
                {
                }
            }
            """);

    [Fact]
    public Task StaticFieldAssignedInStaticCtor()
        => TestInRegularAndScriptAsync(
            """
            class MyClass
            {
                private static int [|_goo|];
                static MyClass()
                {
                    _goo = 0;
                }
            }
            """,
            """
            class MyClass
            {
                private static readonly int _goo;
                static MyClass()
                {
                    _goo = 0;
                }
            }
            """);

    [Fact]
    public Task StaticFieldAssignedInNonStaticCtor()
        => TestMissingInRegularAndScriptAsync(
            """
            class MyClass
            {
                private static int [|_goo|];
                MyClass()
                {
                    _goo = 0;
                }
            }
            """);

    [Fact]
    public Task FieldTypeIsMutableStruct()
        => TestMissingInRegularAndScriptAsync(
            """
            struct MyStruct
            {
                private int _goo;
            }
            class MyClass
            {
                private MyStruct [|_goo|];
            }
            """);

    [Fact]
    public Task FieldTypeIsCustomImmutableStruct()
        => TestInRegularAndScriptAsync(
            """
            struct MyStruct
            {
                private readonly int _goo;
                private const int _bar = 0;
                private static int _fizz;
            }
            class MyClass
            {
                private MyStruct [|_goo|];
            }
            """,
            """
            struct MyStruct
            {
                private readonly int _goo;
                private const int _bar = 0;
                private static int _fizz;
            }
            class MyClass
            {
                private readonly MyStruct _goo;
            }
            """);

    [Fact]
    public Task FixAll()
        => TestInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int {|FixAllInDocument:_goo|} = 0, _bar = 0;
                private int _x = 0, _y = 0, _z = 0;
                private int _fizz = 0;

                void Method() { _z = 1; }
            }
            """,
            """
            class MyClass
            {
                private readonly int _goo = 0, _bar = 0;
                private readonly int _x = 0;
                private readonly int _y = 0;
                private int _z = 0;
                private readonly int _fizz = 0;

                void Method() { _z = 1; }
            }
            """);

    [Fact]
    public Task FixAll2()
        => TestInRegularAndScriptAsync(
            """
            using System;

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

              partial struct MyClass { }
            """,
            """
            using System;

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

              partial struct MyClass { }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26262")]
    public Task FieldAssignedInCtor_InParens()
        => TestInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_goo|];
                MyClass()
                {
                    (_goo) = 0;
                }
            }
            """,
            """
            class MyClass
            {
                private readonly int _goo;
                MyClass()
                {
                    (_goo) = 0;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26262")]
    public Task FieldAssignedInCtor_QualifiedWithThis_InParens()
        => TestInRegularAndScriptAsync(
            """
            class MyClass
            {
                private int [|_goo|];
                MyClass()
                {
                    (this._goo) = 0;
                }
            }
            """,
            """
            class MyClass
            {
                private readonly int _goo;
                MyClass()
                {
                    (this._goo) = 0;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26264")]
    public Task FieldAssignedInMethod_InDeconstruction()
        => TestMissingAsync(
            """
            class C
            {
                [|int i;|]
                int j;

                void M()
                {
                    (i, j) = (1, 2);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26264")]
    public Task FieldAssignedInMethod_InDeconstruction_InParens()
        => TestMissingAsync(
            """
            class C
            {
                [|int i;|]
                int j;

                void M()
                {
                    ((i, j), j) = ((1, 2), 3);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26264")]
    public Task FieldAssignedInMethod_InDeconstruction_WithThis_InParens()
        => TestMissingAsync(
            """
            class C
            {
                [|int i;|]
                int j;

                void M()
                {
                    ((this.i, j), j) = (1, 2);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26264")]
    public Task FieldUsedInTupleExpressionOnRight()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                [|int i;|]
                int j;

                void M()
                {
                    (j, j) = (i, i);
                }
            }
            """,
            """
            class C
            {
                readonly int i;
                int j;

                void M()
                {
                    (j, j) = (i, i);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26264")]
    public Task FieldInTypeWithGeneratedCode()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                [|private int i;|]

                [System.CodeDom.Compiler.GeneratedCodeAttribute("", "")]
                private int j;

                void M()
                {
                }
            }
            """,
            """
            class C
            {
                private readonly int i;

                [System.CodeDom.Compiler.GeneratedCodeAttribute("", "")]
                private int j;

                void M()
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26364")]
    public Task FieldIsFixed()
        => TestMissingInRegularAndScriptAsync(
            """
            unsafe struct S
            {
                [|private fixed byte b[8];|]
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38995")]
    public Task FieldAssignedToLocalRef()
        => TestMissingAsync(
            """
            class Program
            {
                [|int i;|]

                void M()
                {
                    ref var value = ref i;
                    value += 1;
                }
            }
            """);

    [Fact]
    public Task FieldAssignedToLocalReadOnlyRef()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                [|int i;|]

                void M()
                {
                    ref readonly var value = ref i;
                }
            }
            """,
            """
            class Program
            {
                [|readonly int i;|]

                void M()
                {
                    ref readonly var value = ref i;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26213")]
    public Task TestFieldAccessesOnLeftOfDot()
        => TestInRegularAndScriptAsync(
            """
            interface IFaceServiceClient
            {
                void DetectAsync();
            }

            public class Repro
            {
                private static IFaceServiceClient [|faceServiceClient|] = null;

                public static void Run()
                {
                    faceServiceClient.DetectAsync();
                }
            }
            """,
            """
            interface IFaceServiceClient
            {
                void DetectAsync();
            }

            public class Repro
            {
                private static readonly IFaceServiceClient faceServiceClient = null;

                public static void Run()
                {
                    faceServiceClient.DetectAsync();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42759")]
    public Task TestVolatileField1()
        => TestInRegularAndScriptAsync(
            """
            class TestClass
            {
                private volatile object [|first|]; 
            }
            """,
            """
            class TestClass
            {
                private readonly object first; 
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42759")]
    public Task TestVolatileField2()
        => TestInRegularAndScriptAsync(
            """
            class TestClass
            {
                private volatile object [|first|], second; 
            }
            """,
            """
            class TestClass
            {
                private readonly object first;
                private volatile object second;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42759")]
    public Task TestVolatileField3()
        => TestInRegularAndScriptAsync(
            """
            class TestClass
            {
                private volatile object first, [|second|]; 
            }
            """,
            """
            class TestClass
            {
                private volatile object first;
                private readonly object second;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46785")]
    public Task UsedAsRef_NoDiagnostic()
        => TestMissingInRegularAndScriptAsync(
            """
            public class C
            {
                private string [|x|] = string.Empty;

                public bool M()
                {
                    ref var myVar = ref x;
                    return myVar is null;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57983")]
    public Task UsedAsRef_NoDiagnostic_02()
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Runtime.CompilerServices;

            public class Test
            {
                private ulong [|nextD3D12ComputeFenceValue|];

                internal void Repro()
                {
                    ref ulong d3D12FenceValue = ref Unsafe.NullRef<ulong>();
                    d3D12FenceValue = ref nextD3D12ComputeFenceValue;
                    d3D12FenceValue++;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42760")]
    public Task WithThreadStaticAttribute_NoDiagnostic()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                [ThreadStatic]
                private static object [|t_obj|];
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50925")]
    public Task Test_MemberUsedInGeneratedCode()
        => TestMissingInRegularAndScriptAsync(
            """
            <Workspace>
                <Project Language = "C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document FilePath = "File1.cs">
            public sealed partial class Test
            {
                private int [|_value|];

                public static void M()
                    => _ = new Test { Value = 1 };
            }
                    </Document>
                    <Document FilePath = "File2.g.cs">
            using System.CodeDom.Compiler;

            [GeneratedCode(null, null)]
            public sealed partial class Test
            {
                public int Value
                {
                    get => _value;
                    set => _value = value;
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40644")]
    public Task ShouldNotWarnForDataMemberFieldsInDataContractClasses()
        => TestMissingAsync(
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferencesNet45="true">
                    <Document>
            [System.Runtime.Serialization.DataContractAttribute]
            public class MyClass
            {
                [System.Runtime.Serialization.DataMember]
                private bool [|isReadOnly|];
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40644")]
    public Task ShouldWarnForDataMemberFieldsInNonDataContractClasses()
        => TestInRegularAndScriptAsync(
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferencesNet45="true">
                    <Document>
            public class MyClass
            {
                [System.Runtime.Serialization.DataMember]
                private bool [|isReadOnly|];
            }
                    </Document>
                </Project>
            </Workspace>
            """,
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferencesNet45="true">
                    <Document>
            public class MyClass
            {
                [System.Runtime.Serialization.DataMember]
                private readonly bool isReadOnly;
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40644")]
    public Task ShouldWarnForPrivateNonDataMemberFieldsInDataContractClasses()
        => TestInRegularAndScriptAsync(
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferencesNet45="true">
                    <Document>
            [System.Runtime.Serialization.DataContractAttribute]
            public class MyClass
            {
                [System.Runtime.Serialization.DataMember]
                private bool isReadOnly;

                private bool [|isReadOnly2|];
            }
                    </Document>
                </Project>
            </Workspace>
            """,
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferencesNet45="true">
                    <Document>
            [System.Runtime.Serialization.DataContractAttribute]
            public class MyClass
            {
                [System.Runtime.Serialization.DataMember]
                private bool isReadOnly;

                private readonly bool isReadOnly2;
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40644")]
    public Task ShouldNotWarnForPublicImplicitDataMemberFieldsInDataContractClasses()
        => TestMissingAsync(
            """
            <Workspace>
                <Project Language="C#" AssemblyName="Assembly1" CommonReferencesNet45="true">
                    <Document>
            [System.Runtime.Serialization.DataContractAttribute]
            public class MyClass
            {
                public bool [|isReadOnly|];
            }
                    </Document>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/59577")]
    public Task TestInStruct()
        => TestInRegularAndScriptAsync(
            """
            struct MyClass
            {
                private int [|_goo|];
            }
            """,
            """
            struct MyClass
            {
                private readonly int _goo;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/59577")]
    public Task MissingForMemberInStructThatOverwritesThis()
        => TestMissingAsync(
            """
            struct MyClass
            {
                private int [|_goo|];

                void M()
                {
                    this = default;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38468")]
    public Task PreserveLeadingTrivia1()
        => TestInRegularAndScriptAsync(
            """
            public class C
            {
                int x;

                int [|y|];
            }
            """,
            """
            public class C
            {
                int x;

                readonly int y;
            }
            """);

    [Fact, WorkItem(47197, "https://github.com/dotnet/roslyn/issues/47197")]
    public Task StrictFeatureFlagAssignment1()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;

            class C<T>
            {
                private static IEqualityComparer<T> [|s_value|];

                static C()
                {
                    C<T>.s_value = null;
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;

            class C<T>
            {
                private static readonly IEqualityComparer<T> s_value;

                static C()
                {
                    C<T>.s_value = null;
                }
            }
            """, new(parseOptions: s_strictFeatureFlag));

    [Fact, WorkItem(47197, "https://github.com/dotnet/roslyn/issues/47197")]
    public Task StrictFeatureFlagAssignment2()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;

            class C<T>
            {
                private static IEqualityComparer<T> [|s_value|];

                static C()
                {
                    C<string>.s_value = null;
                }
            }
            """, new TestParameters(parseOptions: s_strictFeatureFlag));

    [Fact, WorkItem(47197, "https://github.com/dotnet/roslyn/issues/47197")]
    public Task StrictFeatureFlagAssignment3()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;

            class C<T>
            {
                private static IEqualityComparer<T> [|s_value|];

                static C()
                {
                    C<string>.s_value = null;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/69143")]
    public Task DoNotAddReadonlyToInlineArrayInstanceMember()
        => TestMissingInRegularAndScriptAsync($$"""
            using System;
            using System.Runtime.CompilerServices;

            {{s_inlineArrayAttribute}}

            [InlineArray(4)]
            struct S
            {
                private int [|i|];
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75995")]
    public Task AddReadonlyToInlineArrayStaticMember1()
        => TestInRegularAndScriptAsync($$"""
            using System;
            using System.Runtime.CompilerServices;

            {{s_inlineArrayAttribute}}

            [InlineArray(4)]
            struct S
            {
                private static int [|j|];
            }
            """, $$"""
            using System;
            using System.Runtime.CompilerServices;

            {{s_inlineArrayAttribute}}

            [InlineArray(4)]
            struct S
            {
                private static readonly int j;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47198")]
    public Task TestIndexedAndAssignedField_StructType()
        => TestMissingAsync(
            """
            class GreenNode { }

            struct SyntaxListBuilder<TNode>
            {
                public GreenNode this[int index]
                {
                    get => default;
                    set { }
                }
            }

            class SkippedTriviaBuilder
                private SyntaxListBuilder<GreenNode> [|_triviaListBuilder|];

                public AddSkippedTrivia(GreenNode trivia)
                {
                    _triviaListBuilder[0] = trivia;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47198")]
    public Task TestIndexedAndAssignedField_ClassType()
        => TestInRegularAndScriptAsync(
            """
            class GreenNode { }

            class SyntaxListBuilder<TNode>
            {
                public GreenNode this[int index]
                {
                    get => default;
                    set { }
                }
            }

            class SkippedTriviaBuilder
            {
                private SyntaxListBuilder<GreenNode> [|_triviaListBuilder|];

                public AddSkippedTrivia(GreenNode trivia)
                {
                    _triviaListBuilder[0] = trivia;
                }
            }
            """,
            """
            class GreenNode { }

            class SyntaxListBuilder<TNode>
            {
                public GreenNode this[int index]
                {
                    get => default;
                    set { }
                }
            }

            class SkippedTriviaBuilder
            {
                private readonly SyntaxListBuilder<GreenNode> _triviaListBuilder;

                public AddSkippedTrivia(GreenNode trivia)
                {
                    _triviaListBuilder[0] = trivia;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49290")]
    public Task TestPropertyMutatedField_StructType()
        => TestMissingAsync(
            """
            interface I
            {
                int P { get; set; }
            }

            class C<T> where T : struct, I
            {
                private T [|_x|];

                public void Foo() => _x.P = 42;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49290")]
    public Task TestPropertyMutatedField_ClassType()
        => TestInRegularAndScriptAsync(
            """
            interface I
            {
                int P { get; set; }
            }

            class C<T> where T : class, I
            {
                private T [|_x|];

                public void Foo() => _x.P = 42;
            }
            """,
            """
            interface I
            {
                int P { get; set; }
            }
            
            class C<T> where T : class, I
            {
                private readonly T _x;
            
                public void Foo() => _x.P = 42;
            }
            """);
}
