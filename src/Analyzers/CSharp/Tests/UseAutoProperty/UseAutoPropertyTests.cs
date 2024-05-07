// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UseAutoProperty;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseAutoProperty;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)]
public sealed class UseAutoPropertyTests(ITestOutputHelper logger)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest(logger)
{
    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpUseAutoPropertyAnalyzer(), GetCSharpUseAutoPropertyCodeFixProvider());

    [Fact]
    public async Task TestSingleGetterFromField()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                [|int i|];

                int P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            """,
            """
            class Class
            {
                int P { get; }
            }
            """);
    }

    [Fact]
    public async Task TestSingleGetterFromField_FileScopedNamespace()
    {
        await TestInRegularAndScript1Async(
            """
            namespace N;

            class Class
            {
                [|int i|];

                int P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            """,
            """
            namespace N;

            class Class
            {
                int P { get; }
            }
            """);
    }

    [Fact]
    public async Task TestSingleGetterFromField_InRecord()
    {
        await TestInRegularAndScript1Async(
            """
            record Class
            {
                [|int i|];

                int P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            """,
            """
            record Class
            {
                int P { get; }
            }
            """, new TestParameters(TestOptions.RegularPreview));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28511")]
    public async Task TestNullable1()
    {
        // ⚠ The expected outcome of this test should not change.
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|MutableInt? i|];

                MutableInt? P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            struct MutableInt { public int Value; }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28511")]
    public async Task TestNullable2()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                [|readonly MutableInt? i|];

                MutableInt? P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            struct MutableInt { public int Value; }
            """,
            """
            class Class
            {
                MutableInt? P { get; }
            }
            struct MutableInt { public int Value; }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28511")]
    public async Task TestNullable3()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                [|int? i|];

                int? P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            """,
            """
            class Class
            {
                int? P { get; }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28511")]
    public async Task TestNullable4()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                [|readonly int? i|];

                int? P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            """,
            """
            class Class
            {
                int? P { get; }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28511")]
    public async Task TestNullable5()
    {
        // Recursive type check
        await TestMissingInRegularAndScriptAsync(
            """
            using System;
            class Class
            {
                [|Nullable<MutableInt?> i|];

                Nullable<MutableInt?> P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            struct MutableInt { public int Value; }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28511")]
    public async Task TestMutableValueType1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|MutableInt i|];

                MutableInt P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            struct MutableInt { public int Value; }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28511")]
    public async Task TestMutableValueType2()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                [|readonly MutableInt i|];

                MutableInt P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            struct MutableInt { public int Value; }
            """,
            """
            class Class
            {
                MutableInt P { get; }
            }
            struct MutableInt { public int Value; }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28511")]
    public async Task TestMutableValueType3()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|MutableInt i|];

                MutableInt P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            struct MutableInt { public int Value { get; set; } }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28511")]
    public async Task TestErrorType1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|ErrorType i|];

                ErrorType P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28511")]
    public async Task TestErrorType2()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                [|readonly ErrorType i|];

                ErrorType P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            """,
            """
            class Class
            {
                ErrorType P { get; }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28511")]
    public async Task TestErrorType3()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|ErrorType? i|];

                ErrorType? P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28511")]
    public async Task TestErrorType4()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                [|readonly ErrorType? i|];

                ErrorType? P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            """,
            """
            class Class
            {
                ErrorType? P { get; }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28511")]
    public async Task TestErrorType5()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                [|ErrorType[] i|];

                ErrorType[] P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            """,
            """
            class Class
            {
                ErrorType[] P { get; }
            }
            """);
    }

    [Fact]
    public async Task TestCSharp5_1()
    {
        await TestAsync(
            """
            class Class
            {
                [|int i|];

                public int P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            """,
            """
            class Class
            {
                public int P { get; private set; }
            }
            """,
        CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
    }

    [Fact]
    public async Task TestCSharp5_2()
    {
        await TestMissingAsync(
            """
            class Class
            {
                [|readonly int i|];

                int P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            """, new TestParameters(CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5)));
    }

    [Fact]
    public async Task TestInitializer()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                [|int i = 1|];

                int P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            """,
            """
            class Class
            {
                int P { get; } = 1;
            }
            """);
    }

    [Fact]
    public async Task TestInitializer_CSharp5()
    {
        await TestMissingAsync(
            """
            class Class
            {
                [|int i = 1|];

                int P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            """, new TestParameters(CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5)));
    }

    [Fact]
    public async Task TestSingleGetterFromProperty()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                int i;

                [|int P
                {
                    get
                    {
                        return i;
                    }
                }|]
            }
            """,
            """
            class Class
            {
                int P { get; }
            }
            """);
    }

    [Fact]
    public async Task TestSingleSetter()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|int i|];

                int P
                {
                    set
                    {
                        i = value;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestGetterAndSetter()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                [|int i|];

                int P
                {
                    get
                    {
                        return i;
                    }

                    set
                    {
                        i = value;
                    }
                }
            }
            """,
            """
            class Class
            {
                int P { get; set; }
            }
            """);
    }

    [Fact]
    public async Task TestSingleGetterWithThis()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                [|int i|];

                int P
                {
                    get
                    {
                        return this.i;
                    }
                }
            }
            """,
            """
            class Class
            {
                int P { get; }
            }
            """);
    }

    [Fact]
    public async Task TestSingleSetterWithThis()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|int i|];

                int P
                {
                    set
                    {
                        this.i = value;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestGetterAndSetterWithThis()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                [|int i|];

                int P
                {
                    get
                    {
                        return this.i;
                    }

                    set
                    {
                        this.i = value;
                    }
                }
            }
            """,
            """
            class Class
            {
                int P { get; set; }
            }
            """);
    }

    [Fact]
    public async Task TestGetterWithMutipleStatements()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|int i|];

                int P
                {
                    get
                    {
                        ;
                        return i;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestSetterWithMutipleStatements()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|int i|];

                int P
                {
                    set
                    {
                        ;
                        i = value;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestSetterWithMultipleStatementsAndGetterWithSingleStatement()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|int i|];

                int P
                {
                    get
                    {
                        return i;
                    }

                    set
                    {
                        ;
                        i = value;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestGetterAndSetterUseDifferentFields()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|int i|];
                int j;

                int P
                {
                    get
                    {
                        return i;
                    }

                    set
                    {
                        j = value;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestFieldAndPropertyHaveDifferentStaticInstance()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|static int i|];

                int P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestNotIfFieldUsedInRefArgument1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|int i|];

                int P
                {
                    get
                    {
                        return i;
                    }
                }

                void M(ref int x)
                {
                    M(ref i);
                }
            }
            """);
    }

    [Fact]
    public async Task TestNotIfFieldUsedInRefArgument2()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|int i|];

                int P
                {
                    get
                    {
                        return i;
                    }
                }

                void M(ref int x)
                {
                    M(ref this.i);
                }
            }
            """);
    }

    [Fact]
    public async Task TestNotIfFieldUsedInOutArgument()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|int i|];

                int P
                {
                    get
                    {
                        return i;
                    }
                }

                void M(out int x)
                {
                    M(out i);
                }
            }
            """);
    }

    [Fact]
    public async Task TestNotIfFieldUsedInInArgument()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|int i|];

                int P
                {
                    get
                    {
                        return i;
                    }
                }

                void M(in int x)
                {
                    M(in i);
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25429")]
    public async Task TestNotIfFieldUsedInRefExpression()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|int i|];

                int P
                {
                    get
                    {
                        return i;
                    }
                }

                void M()
                {
                    ref int x = ref i;
                }
            }
            """);
    }

    [Fact]
    public async Task TestNotIfFieldUsedInRefExpression_AsCandidateSymbol()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|int i|];

                int P
                {
                    get
                    {
                        return i;
                    }
                }

                void M()
                {
                    // because we refer to 'i' statically, it only gets resolved as a candidate symbol
                    // let's be conservative here and disable the analyzer if we're not sure
                    ref int x = ref Class.i;
                }
            }
            """);
    }

    [Fact]
    public async Task TestIfUnrelatedSymbolUsedInRefExpression()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                [|int i|];
                int j;

                int P
                {
                    get
                    {
                        return i;
                    }
                }

                void M()
                {
                    int i;
                    ref int x = ref i;
                    ref int y = ref j;
                }
            }
            """,
            """
            class Class
            {
                int j;

                int P { get; }

                void M()
                {
                    int i;
                    ref int x = ref i;
                    ref int y = ref j;
                }
            }
            """);
    }

    [Fact]
    public async Task TestNotWithVirtualProperty()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|int i|];

                public virtual int P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestNotWithConstField()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|const int i|];

                int P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25379")]
    public async Task TestNotWithVolatileField()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|volatile int i|];

                int P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestFieldWithMultipleDeclarators1()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                int [|i|], j, k;

                int P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            """,
            """
            class Class
            {
                int j, k;

                int P { get; }
            }
            """);
    }

    [Fact]
    public async Task TestFieldWithMultipleDeclarators2()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                int i, [|j|], k;

                int P
                {
                    get
                    {
                        return j;
                    }
                }
            }
            """,
            """
            class Class
            {
                int i, k;

                int P { get; }
            }
            """);
    }

    [Fact]
    public async Task TestFieldWithMultipleDeclarators3()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                int i, j, [|k|];

                int P
                {
                    get
                    {
                        return k;
                    }
                }
            }
            """,
            """
            class Class
            {
                int i, j;

                int P { get; }
            }
            """);
    }

    [Fact]
    public async Task TestFieldAndPropertyInDifferentParts()
    {
        await TestInRegularAndScript1Async(
            """
            partial class Class
            {
                [|int i|];
            }

            partial class Class
            {
                int P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            """,
            """
            partial class Class
            {
            }

            partial class Class
            {
                int P { get; }
            }
            """);
    }

    [Fact]
    public async Task TestNotWithFieldWithAttribute()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|[A]
                int i|];

                int P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestUpdateReferences()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                [|int i|];

                int P
                {
                    get
                    {
                        return i;
                    }
                }

                public Class()
                {
                    i = 1;
                }
            }
            """,
            """
            class Class
            {
                int P { get; }

                public Class()
                {
                    P = 1;
                }
            }
            """);
    }

    [Fact]
    public async Task TestUpdateReferencesConflictResolution()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                [|int i|];

                int P
                {
                    get
                    {
                        return i;
                    }
                }

                public Class(int P)
                {
                    i = 1;
                }
            }
            """,
            """
            class Class
            {
                int P { get; }

                public Class(int P)
                {
                    this.P = 1;
                }
            }
            """);
    }

    [Fact]
    public async Task TestWriteInConstructor()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                [|int i|];

                int P
                {
                    get
                    {
                        return i;
                    }
                }

                public Class()
                {
                    i = 1;
                }
            }
            """,
            """
            class Class
            {
                int P { get; }

                public Class()
                {
                    P = 1;
                }
            }
            """);
    }

    [Fact]
    public async Task TestWriteInNotInConstructor1()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                [|int i|];

                int P
                {
                    get
                    {
                        return i;
                    }
                }

                public void Goo()
                {
                    i = 1;
                }
            }
            """,
            """
            class Class
            {
                int P { get; set; }

                public void Goo()
                {
                    P = 1;
                }
            }
            """);
    }

    [Fact]
    public async Task TestWriteInNotInConstructor2()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                [|int i|];

                public int P
                {
                    get
                    {
                        return i;
                    }
                }

                public void Goo()
                {
                    i = 1;
                }
            }
            """,
            """
            class Class
            {
                public int P { get; private set; }

                public void Goo()
                {
                    P = 1;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30108")]
    public async Task TestWriteInSimpleExpressionLambdaInConstructor()
    {
        await TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                [|int i|];
                int P => i;

                C()
                {
                    Action<int> x = _ => i = 1;
                }
            }
            """,
            """
            using System;

            class C
            {
                int P { get; set; }

                C()
                {
                    Action<int> x = _ => P = 1;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30108")]
    public async Task TestWriteInSimpleBlockLambdaInConstructor()
    {
        await TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                [|int i|];
                int P => i;

                C()
                {
                    Action<int> x = _ =>
                    {
                        i = 1;
                    };
                }
            }
            """,
            """
            using System;

            class C
            {
                int P { get; set; }

                C()
                {
                    Action<int> x = _ =>
                    {
                        P = 1;
                    };
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30108")]
    public async Task TestWriteInParenthesizedExpressionLambdaInConstructor()
    {
        await TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                [|int i|];
                int P => i;

                C()
                {
                    Action x = () => i = 1;
                }
            }
            """,
            """
            using System;

            class C
            {
                int P { get; set; }

                C()
                {
                    Action x = () => P = 1;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30108")]
    public async Task TestWriteInParenthesizedBlockLambdaInConstructor()
    {
        await TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                [|int i|];
                int P => i;

                C()
                {
                    Action x = () =>
                    {
                        i = 1;
                    };
                }
            }
            """,
            """
            using System;

            class C
            {
                int P { get; set; }

                C()
                {
                    Action x = () =>
                    {
                        P = 1;
                    };
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30108")]
    public async Task TestWriteInAnonymousMethodInConstructor()
    {
        await TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                [|int i|];
                int P => i;

                C()
                {
                    Action x = delegate ()
                    {
                        i = 1;
                    };
                }
            }
            """,
            """
            using System;

            class C
            {
                int P { get; set; }

                C()
                {
                    Action x = delegate ()
                    {
                        P = 1;
                    };
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30108")]
    public async Task TestWriteInLocalFunctionInConstructor()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                [|int i|];
                int P => i;

                C()
                {
                    void F()
                    {
                        i = 1;
                    }
                }
            }
            """,
            """
            class C
            {
                int P { get; set; }

                C()
                {
                    void F()
                    {
                        P = 1;
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30108")]
    public async Task TestWriteInExpressionBodiedLocalFunctionInConstructor()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                [|int i|];
                int P => i;

                C()
                {
                    void F() => i = 1;
                }
            }
            """,
            """
            class C
            {
                int P { get; set; }

                C()
                {
                    void F() => P = 1;
                }
            }
            """);
    }

    [Fact]
    public async Task TestReadInExpressionBodiedLocalFunctionInConstructor()
    {
        await TestInRegularAndScript1Async(
            """
            class C
            {
                [|int i|];
                int P => i;

                C()
                {
                    bool F() => i == 1;
                }
            }
            """,
            """
            class C
            {
                int P { get; }

                C()
                {
                    bool F() => P == 1;
                }
            }
            """);
    }

    [Fact]
    public async Task TestAlreadyAutoPropertyWithGetterWithNoBody()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                public int [|P|] { get; }
            }
            """);
    }

    [Fact]
    public async Task TestAlreadyAutoPropertyWithGetterAndSetterWithNoBody()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                public int [|P|] { get; set; }
            }
            """);
    }

    [Fact]
    public async Task TestSingleLine1()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                [|int i|];
                int P { get { return i; } }
            }
            """,
            """
            class Class
            {
                int P { get; }
            }
            """);
    }

    [Fact]
    public async Task TestSingleLine2()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                [|int i|];
                int P
                {
                    get { return i; }
                }
            }
            """,
            """
            class Class
            {
                int P { get; }
            }
            """);
    }

    [Fact]
    public async Task TestSingleLine3()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                [|int i|];
                int P
                {
                    get { return i; }
                    set { i = value; }
                }
            }
            """,
            """
            class Class
            {
                int P { get; set; }
            }
            """);
    }

    [Fact]
    public async Task Tuple_SingleGetterFromField()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                [|readonly (int, string) i|];

                (int, string) P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            """,
            """
            class Class
            {
                (int, string) P { get; }
            }
            """);
    }

    [Fact]
    public async Task TupleWithNames_SingleGetterFromField()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                [|readonly (int a, string b) i|];

                (int a, string b) P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            """,
            """
            class Class
            {
                (int a, string b) P { get; }
            }
            """);
    }

    [Fact]
    public async Task TupleWithDifferentNames_SingleGetterFromField()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|readonly (int a, string b) i|];

                (int c, string d) P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TupleWithOneName_SingleGetterFromField()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                [|readonly (int a, string) i|];

                (int a, string) P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            """,
            """
            class Class
            {
                (int a, string) P { get; }
            }
            """);
    }

    [Fact]
    public async Task Tuple_Initializer()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                [|readonly (int, string) i = (1, "hello")|];

                (int, string) P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            """,
            """
            class Class
            {
                (int, string) P { get; } = (1, "hello");
            }
            """);
    }

    [Fact]
    public async Task Tuple_GetterAndSetter()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|(int, string) i|];

                (int, string) P
                {
                    get
                    {
                        return i;
                    }

                    set
                    {
                        i = value;
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23216")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/23215")]
    public async Task TestFixAllInDocument()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                {|FixAllInDocument:int i|};

                int P
                {
                    get
                    {
                        return i;
                    }
                }

                int j;

                int Q
                {
                    get
                    {
                        return j;
                    }
                }
            }
            """,
            """
            class Class
            {
                int P { get; }

                int Q { get; }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23735")]
    public async Task ExplicitInterfaceImplementationGetterOnly()
    {
        await TestMissingInRegularAndScriptAsync("""
            namespace RoslynSandbox
            {
                public interface IFoo
                {
                    object Bar { get; }
                }

                class Foo : IFoo
                {
                    public Foo(object bar)
                    {
                        this.bar = bar;
                    }

                    readonly object [|bar|];

                    object IFoo.Bar
                    {
                        get { return bar; }
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23735")]
    public async Task ExplicitInterfaceImplementationGetterAndSetter()
    {
        await TestMissingInRegularAndScriptAsync("""
            namespace RoslynSandbox
            {
                public interface IFoo
                {
                    object Bar { get; set; }
                }

                class Foo : IFoo
                {
                    public Foo(object bar)
                    {
                        this.bar = bar;
                    }

                    object [|bar|];

                    object IFoo.Bar
                    {
                        get { return bar; }
                        set { bar = value; }
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task ExpressionBodiedMemberGetOnly()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                int [|i|];
                int P
                {
                    get => i;
                }
            }
            """,
            """
            class Class
            {
                int P { get; }
            }
            """);
    }

    [Fact]
    public async Task ExpressionBodiedMemberGetOnlyWithInitializer()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                int [|i|] = 1;
                int P
                {
                    get => i;
                }
            }
            """,
            """
            class Class
            {
                int P { get; } = 1;
            }
            """);
    }

    [Fact]
    public async Task ExpressionBodiedMemberGetOnlyWithInitializerAndNeedsSetter()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                int [|i|] = 1;
                int P
                {
                    get => i;
                }
                void M() { i = 2; }
            }
            """,
            """
            class Class
            {
                int P { get; set; } = 1;
                void M() { P = 2; }
            }
            """);
    }

    [Fact]
    public async Task ExpressionBodiedMemberGetterAndSetter()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                int [|i|];
                int P
                {
                    get => i;
                    set { i = value; }
                }
            }
            """,
            """
            class Class
            {
                int P { get; set; }
            }
            """);
    }

    [Fact]
    public async Task ExpressionBodiedMemberGetter()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                int [|i|];
                int P => i;
            }
            """,
            """
            class Class
            {
                int P { get; }
            }
            """);
    }

    [Fact]
    public async Task ExpressionBodiedMemberGetterWithSetterNeeded()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                int [|i|];
                int P => i;
                void M() { i = 1; }
            }
            """,
            """
            class Class
            {
                int P { get; set; }
                void M() { P = 1; }
            }
            """);
    }

    [Fact]
    public async Task ExpressionBodiedMemberGetterWithInitializer()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                int [|i|] = 1;
                int P => i;
            }
            """,
            """
            class Class
            {
                int P { get; } = 1;
            }
            """);
    }

    [Fact]
    public async Task ExpressionBodiedGetterAndSetter()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                int [|i|];
                int P { 
                    get => i;
                    set => i = value;
                }
            }
            """,
            """
            class Class
            {
                int P { get; set; }
            }
            """);
    }

    [Fact]
    public async Task ExpressionBodiedGetterAndSetterWithInitializer()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                int [|i|] = 1;
                int P { 
                    get => i;
                    set => i = value;
                }
            }
            """,
            """
            class Class
            {
                int P { get; set; } = 1;
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25401")]
    public async Task TestGetterAccessibilityDiffers()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                [|int i|];

                public int P
                {
                    protected get
                    {
                        return i;
                    }

                    set
                    {
                        i = value;
                    }
                }
            }
            """,
            """
            class Class
            {
                public int P { protected get; set; }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25401")]
    public async Task TestSetterAccessibilityDiffers()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                [|int i|];

                public int P
                {
                    get
                    {
                        return i;
                    }

                    protected set
                    {
                        i = value;
                    }
                }
            }
            """,
            """
            class Class
            {
                public int P { get; protected set; }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26858")]
    public async Task TestPreserveTrailingTrivia1()
    {
        await TestInRegularAndScript1Async(
            """
            class Goo
            {
                private readonly object [|bar|] = new object();

                public object Bar => bar;
                public int Baz => 0;
            }
            """,
            """
            class Goo
            {
                public object Bar { get; } = new object();
                public int Baz => 0;
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26858")]
    public async Task TestPreserveTrailingTrivia2()
    {
        await TestInRegularAndScript1Async(
            """
            class Goo
            {
                private readonly object [|bar|] = new object();

                public object Bar => bar; // prop comment
                public int Baz => 0;
            }
            """,
            """
            class Goo
            {
                public object Bar { get; } = new object(); // prop comment
                public int Baz => 0;
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26858")]
    public async Task TestPreserveTrailingTrivia3()
    {
        await TestInRegularAndScript1Async(
            """
            class Goo
            {
                private readonly object [|bar|] = new object();

                // doc
                public object Bar => bar; // prop comment
                public int Baz => 0;
            }
            """,
            """
            class Goo
            {
                // doc
                public object Bar { get; } = new object(); // prop comment
                public int Baz => 0;
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26858")]
    public async Task TestKeepLeadingBlank()
    {
        await TestInRegularAndScript1Async(
            """
            class Goo
            {

                private readonly object [|bar|] = new object();

                // doc
                public object Bar => bar; // prop comment
                public int Baz => 0;
            }
            """,
            """
            class Goo
            {

                // doc
                public object Bar { get; } = new object(); // prop comment
                public int Baz => 0;
            }
            """);
    }

    [Fact]
    public async Task TestMultipleFieldsAbove1()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                [|int i|];
                int j;

                int P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            """,
            """
            class Class
            {
                int j;

                int P { get; }
            }
            """);
    }

    [Fact]
    public async Task TestMultipleFieldsAbove2()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                int j;
                [|int i|];

                int P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            """,
            """
            class Class
            {
                int j;

                int P { get; }
            }
            """);
    }

    [Fact]
    public async Task TestMultipleFieldsAbove3()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                [|int i|];

                int j;

                int P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            """,
            """
            class Class
            {
                int j;

                int P { get; }
            }
            """);
    }

    [Fact]
    public async Task TestMultipleFieldsAbove4()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                int j;

                [|int i|];

                int P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            """,
            """
            class Class
            {
                int j;

                int P { get; }
            }
            """);
    }

    [Fact]
    public async Task TestMultipleFieldsBelow1()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                int P
                {
                    get
                    {
                        return i;
                    }
                }

                [|int i|];
                int j;
            }
            """,
            """
            class Class
            {
                int P { get; }

                int j;
            }
            """);
    }

    [Fact]
    public async Task TestMultipleFieldsBelow2()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                int P
                {
                    get
                    {
                        return i;
                    }
                }

                int j;
                [|int i|];
            }
            """,
            """
            class Class
            {
                int P { get; }

                int j;
            }
            """);
    }

    [Fact]
    public async Task TestMultipleFieldsBelow3()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                int P
                {
                    get
                    {
                        return i;
                    }
                }

                [|int i|];

                int j;
            }
            """,
            """
            class Class
            {
                int P { get; }

                int j;
            }
            """);
    }

    [Fact]
    public async Task TestMultipleFieldsBelow4()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                int P
                {
                    get
                    {
                        return i;
                    }
                }

                int j;

                [|int i|];
            }
            """,
            """
            class Class
            {
                int P { get; }

                int j;
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27675")]
    public async Task TestSingleLineWithDirective()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                #region Test
                [|int i|];
                #endregion

                int P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            """,
            """
            class Class
            {
                #region Test
                #endregion

                int P { get; }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27675")]
    public async Task TestMultipleFieldsWithDirective()
    {
        await TestInRegularAndScript1Async(
            """
            class Class
            {
                #region Test
                [|int i|];
                int j;
                #endregion

                int P
                {
                    get
                    {
                        return i;
                    }
                }

            }
            """,
            """
            class Class
            {
                #region Test
                int j;
                #endregion

                int P { get; }

            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27675")]
    public async Task TestSingleLineWithDoubleDirectives()
    {
        await TestInRegularAndScript1Async(
            """
            class TestClass
            {
                #region Field
                [|int i|];
                #endregion

                #region Property
                int P
                {
                    get { return i; }
                }
                #endregion
            }
            """,
            """
            class TestClass
            {
                #region Field
                #endregion

                #region Property
                int P { get; }
                #endregion
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40622")]
    public async Task TestUseTabs()
    {
        await TestInRegularAndScript1Async(
            """
            public class Foo
            {
            	private readonly object o;

            	[||]public object O => o;
            }
            """,
            """
            public class Foo
            {
            	public object O { get; }
            }
            """, new TestParameters(options: Option(FormattingOptions2.UseTabs, true)));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40622")]
    public async Task TestUseSpaces()
    {
        await TestInRegularAndScript1Async(
            """
            public class Foo
            {
            	private readonly object o;

            	[||]public object O => o;
            }
            """,
            """
            public class Foo
            {
                public object O { get; }
            }
            """, new TestParameters(options: Option(FormattingOptions2.UseTabs, false)));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40622")]
    public async Task TestUseTabs_Editorconfig()
    {
        await TestInRegularAndScript1Async(
            """
            <Workspace>
                <Project Language = "C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document FilePath = "z:\\file.cs">
            public class Foo
            {
            	private readonly object o;

            	[||]public object O => o;
            }
                    </Document>
                    <AnalyzerConfigDocument FilePath = "z:\\.editorconfig">
            [*]
            indent_style = tab
            </AnalyzerConfigDocument>
                </Project>
            </Workspace>
            """,
            """
            <Workspace>
                <Project Language = "C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document FilePath = "z:\\file.cs">
            public class Foo
            {
            	public object O { get; }
            }
                    </Document>
                    <AnalyzerConfigDocument FilePath = "z:\\.editorconfig">
            [*]
            indent_style = tab
            </AnalyzerConfigDocument>
                </Project>
            </Workspace>
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40622")]
    public async Task TestUseSpaces_Editorconfig()
    {
        await TestInRegularAndScript1Async(
            """
            <Workspace>
                <Project Language = "C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document FilePath = "z:\\file.cs">
            public class Foo
            {
            	private readonly object o;

            	[||]public object O => o;
            }
                    </Document>
                    <AnalyzerConfigDocument FilePath = "z:\\.editorconfig">
            [*]
            indent_style = space
            </AnalyzerConfigDocument>
                </Project>
            </Workspace>
            """,
            """
            <Workspace>
                <Project Language = "C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document FilePath = "z:\\file.cs">
            public class Foo
            {
                public object O { get; }
            }
                    </Document>
                    <AnalyzerConfigDocument FilePath = "z:\\.editorconfig">
            [*]
            indent_style = space
            </AnalyzerConfigDocument>
                </Project>
            </Workspace>
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34783")]
    public async Task TestNotOnSerializableType()
    {
        await TestMissingAsync(
            """
            [System.Serializable]
            class Class
            {
                [|int i|];

                int P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47999")]
    public async Task TestPropertyIsReadOnlyAndSetterNeeded()
    {
        await TestInRegularAndScript1Async(
            """
            struct S
            {
                [|int i|];
                public readonly int P => i;
                public void SetP(int value) => i = value;
            }
            """,
            """
            struct S
            {
                public int P { get; private set; }
                public void SetP(int value) => P = value;
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47999")]
    public async Task TestPropertyIsReadOnlyWithNoAccessModifierAndSetterNeeded()
    {
        await TestInRegularAndScript1Async(
            """
            struct S
            {
                [|int i|];
                readonly int P => i;
                public void SetP(int value) => i = value;
            }
            """,
            """
            struct S
            {
                int P { get; set; }
                public void SetP(int value) => P = value;
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47999")]
    public async Task TestPropertyIsReadOnlyAndSetterUnneeded()
    {
        await TestInRegularAndScript1Async(
            """
            struct S
            {
                [|int i|];
                public readonly int P => i;
            }
            """,
            """
            struct S
            {
                public readonly int P { get; }
            }
            """);
    }

    [Fact]
    public async Task TestPropertyInRecordStruct()
    {
        await TestInRegularAndScript1Async(
            """
            record struct S
            {
                [|int i|];
                public readonly int P => i;
            }
            """,
            """
            record struct S
            {
                public readonly int P { get; }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38286")]
    public async Task TestPointer1()
    {
        await TestInRegularAndScriptAsync(
            """
            class Class
            {
                [|int* i|];

                int* P => i;
            }
            """,
            """
            class Class
            {
                int* P { get; }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38286")]
    public async Task TestPointer2()
    {
        await TestMissingAsync(
            """
            class Class
            {
                [|int* i|];

                int* P => i;

                void M()
                {
                    fixed (int** ii = &i)
                    {
                    }
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25408")]
    public async Task TestLinkedFile()
    {
        await TestInRegularAndScript1Async(
            """
            <Workspace>
                <Project Language='C#' CommonReferences='true' AssemblyName='LinkedProj' Name='CSProj.1'>
                    <Document FilePath='C.cs'>class C
            {
                private readonly [|int _value|];

                public C(int value)
                {
                    _value = value;
                }

                public int Value
                {
                    get { return _value; }
                }
            }</Document>
                </Project>
                <Project Language='C#' CommonReferences='true' AssemblyName='LinkedProj' Name='CSProj.2'>
                    <Document IsLinkFile='true' LinkProjectName='CSProj.1' LinkFilePath='C.cs'/>
                </Project>
            </Workspace>
            """,
            """
            class C
            {
                public C(int value)
                {
                    Value = value;
                }

                public int Value { get; }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32597")]
    public async Task TestUnassignedVariable1()
    {
        await TestMissingAsync(
            """
            public struct UInt128
            {
                [|private ulong s0;|]
                private ulong s1;

                public ulong S0 { get { return s0; } }
                public ulong S1 { get { return s1; } }

                public static void Create(out UInt128 c, uint r0, uint r1, uint r2, uint r3)
                {
                    c.s0 = (ulong)r1 << 32 | r0;
                    c.s1 = (ulong)r3 << 32 | r2;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32597")]
    public async Task TestAssignedVariable1()
    {
        await TestInRegularAndScript1Async(
            """
            public struct UInt128
            {
                [|private ulong s0;|]
                private ulong s1;

                public ulong S0 { get { return s0; } }
                public ulong S1 { get { return s1; } }

                public static void Create(out UInt128 c, uint r0, uint r1, uint r2, uint r3)
                {
                    c = default;
                    c.s0 = (ulong)r1 << 32 | r0;
                    c.s1 = (ulong)r3 << 32 | r2;
                }
            }
            """,
            """
            public struct UInt128
            {
                private ulong s1;

                public ulong S0 { get; private set; }
                public ulong S1 { get { return s1; } }

                public static void Create(out UInt128 c, uint r0, uint r1, uint r2, uint r3)
                {
                    c = default;
                    c.S0 = (ulong)r1 << 32 | r0;
                    c.s1 = (ulong)r3 << 32 | r2;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71032")]
    public async Task TestWithInitProperty1()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;

            public class C
            {
                [|private string _action;|]
                public required string Action
                {
                    get => _action;
                    init => _action = value;
                }
            }
            """,
            """
            using System;

            public class C
            {
                public required string Action { get; init; }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71032")]
    public async Task TestNotWithInitProperty1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            public class C
            {
                [|private string _action;|]
                public required string Action
                {
                    get => _action;
                    init => _action = value;
                }

                private void SetAction(string newAction) => _action = newAction;
            }
            """);
    }
}
