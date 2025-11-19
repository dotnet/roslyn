// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
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
public sealed partial class UseAutoPropertyTests(ITestOutputHelper logger)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    private const string s_allowNullAttribute = """
        
        namespace System.Diagnostics.CodeAnalysis
        {
            [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property)]
            public sealed class AllowNullAttribute : Attribute
            {
            }
        }
        """;

    private readonly ParseOptions CSharp12 = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12);

    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpUseAutoPropertyAnalyzer(), new CSharpUseAutoPropertyCodeFixProvider());

    [Fact]
    public Task TestSingleGetterFromField()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestSingleGetterFromField_FileScopedNamespace()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestSingleGetterFromField_InRecord()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76813")]
    public Task TestSingleGetterFromField_CommentBeforeField()
        => TestInRegularAndScriptAsync(
            """
            class Class
            {
                // Comment to preserve
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
                // Comment to preserve
                int P { get; }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28511")]
    public Task TestNullable1()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28511")]
    public Task TestNullable2()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28511")]
    public Task TestNullable3()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28511")]
    public Task TestNullable4()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28511")]
    public Task TestNullable5()
        => TestMissingInRegularAndScriptAsync(
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

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/76815")]
    [InlineData("DateTime")]
    [InlineData("ArraySegment<int>")]
    [InlineData("DateTimeOffset")]
    [InlineData("Guid")]
    [InlineData("Index")]
    [InlineData("Range")]
    [InlineData("ReadOnlyMemory<int>")]
    [InlineData("ReadOnlySpan<int>")]
    [InlineData("TimeSpan")]
    public Task TestWellKnownImmutableValueType1(string typeName)
        => TestInRegularAndScriptAsync(
            $$"""
            class Class
            {
                [|System.{{typeName}} i|];

                System.{{typeName}} P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            """,
            $$"""
            class Class
            {
                System.{{typeName}} P { get; }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28511")]
    public Task TestMutableValueType1()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28511")]
    public Task TestMutableValueType2()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28511")]
    public Task TestMutableValueType3()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28511")]
    public Task TestErrorType1()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28511")]
    public Task TestErrorType2()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28511")]
    public Task TestErrorType3()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28511")]
    public Task TestErrorType4()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28511")]
    public Task TestErrorType5()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestCSharp5_1()
        => TestAsync(
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
            new(CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5)));

    [Fact]
    public Task TestCSharp5_2()
        => TestMissingAsync(
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

    [Fact]
    public Task TestInitializer()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestInitializer_CSharp5()
        => TestMissingAsync(
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

    [Fact]
    public Task TestSingleGetterFromProperty()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestSingleSetter()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestGetterAndSetter()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestSingleGetterWithThis()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestSingleSetterWithThis()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestGetterAndSetterWithThis()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestGetterWithMultipleStatements_CSharp12()
        => TestMissingInRegularAndScriptAsync(
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
            """, new TestParameters(parseOptions: CSharp12));

    [Fact]
    public Task TestSetterWithMultipleStatements_CSharp12()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestSetterWithMultipleStatementsAndGetterWithSingleStatement()
        => TestMissingInRegularAndScriptAsync(
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
            """, new TestParameters(parseOptions: CSharp12));

    [Fact]
    public Task TestGetterAndSetterUseDifferentFields()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestFieldAndPropertyHaveDifferentStaticInstance()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestNotIfFieldUsedInRefArgument1()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestNotIfFieldUsedInRefArgument2()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestNotIfFieldUsedInOutArgument()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestNotIfFieldUsedInInArgument()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25429")]
    public Task TestNotIfFieldUsedInRefExpression()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestNotIfFieldUsedInRefExpression_AsCandidateSymbol()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestIfUnrelatedSymbolUsedInRefExpression()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestNotWithVirtualProperty()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestNotWithConstField()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25379")]
    public Task TestNotWithVolatileField()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestFieldWithMultipleDeclarators1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestFieldWithMultipleDeclarators2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestFieldWithMultipleDeclarators3()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestFieldAndPropertyInDifferentParts()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestWithFieldWithAttribute()
        => TestInRegularAndScriptAsync(
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
            """,
            """
            class Class
            {
                [field: A]
                int P { get; }
            }
            """);

    [Fact]
    public Task TestUpdateReferences()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestUpdateReferencesConflictResolution()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestWriteInConstructor()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestWriteInNotInConstructor1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestWriteInNotInConstructor2()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30108")]
    public Task TestWriteInSimpleExpressionLambdaInConstructor()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30108")]
    public Task TestWriteInSimpleBlockLambdaInConstructor()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30108")]
    public Task TestWriteInParenthesizedExpressionLambdaInConstructor()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30108")]
    public Task TestWriteInParenthesizedBlockLambdaInConstructor()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30108")]
    public Task TestWriteInAnonymousMethodInConstructor()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30108")]
    public Task TestWriteInLocalFunctionInConstructor()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30108")]
    public Task TestWriteInExpressionBodiedLocalFunctionInConstructor()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReadInExpressionBodiedLocalFunctionInConstructor()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestAlreadyAutoPropertyWithGetterWithNoBody()
        => TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                public int [|P|] { get; }
            }
            """);

    [Fact]
    public Task TestAlreadyAutoPropertyWithGetterAndSetterWithNoBody()
        => TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                public int [|P|] { get; set; }
            }
            """);

    [Fact]
    public Task TestSingleLine1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestSingleLine2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestSingleLine3()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task Tuple_SingleGetterFromField()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TupleWithNames_SingleGetterFromField()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TupleWithDifferentNames_SingleGetterFromField()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TupleWithOneName_SingleGetterFromField()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task Tuple_Initializer()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task Tuple_GetterAndSetter()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23216")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/23215")]
    public Task TestFixAllInDocument1()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26527")]
    public Task TestFixAllInDocument2()
        => TestInRegularAndScriptAsync(
            """
            internal struct StringFormat
            {
                private readonly object {|FixAllInDocument:_argument1|};
                private readonly object _argument2;
                private readonly object _argument3;
                private readonly object[] _arguments;

                public object Argument1
                {
                    get { return _argument1; }
                }

                public object Argument2
                {
                    get { return _argument2; }
                }

                public object Argument3
                {
                    get { return _argument3; }
                }

                public object[] Arguments
                {
                    get { return _arguments; }
                }
            }
            """,
            """
            internal struct StringFormat
            {
                public object Argument1 { get; }

                public object Argument2 { get; }

                public object Argument3 { get; }

                public object[] Arguments { get; }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23735")]
    public Task ExplicitInterfaceImplementationGetterOnly()
        => TestMissingInRegularAndScriptAsync("""
            namespace RoslynSandbox
            {
                public interface IGoo
                {
                    object Bar { get; }
                }

                class Goo : IGoo
                {
                    public Goo(object bar)
                    {
                        this.bar = bar;
                    }

                    readonly object [|bar|];

                    object IGoo.Bar
                    {
                        get { return bar; }
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23735")]
    public Task ExplicitInterfaceImplementationGetterAndSetter()
        => TestMissingInRegularAndScriptAsync("""
            namespace RoslynSandbox
            {
                public interface IGoo
                {
                    object Bar { get; set; }
                }

                class Goo : IGoo
                {
                    public Goo(object bar)
                    {
                        this.bar = bar;
                    }

                    object [|bar|];

                    object IGoo.Bar
                    {
                        get { return bar; }
                        set { bar = value; }
                    }
                }
            }
            """);

    [Fact]
    public Task ExpressionBodiedMemberGetOnly()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task ExpressionBodiedMemberGetOnlyWithInitializer()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task ExpressionBodiedMemberGetOnlyWithInitializerAndNeedsSetter()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task ExpressionBodiedMemberGetterAndSetter()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task ExpressionBodiedMemberGetter()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task ExpressionBodiedMemberGetterWithSetterNeeded()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task ExpressionBodiedMemberGetterWithInitializer()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task ExpressionBodiedGetterAndSetter()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task ExpressionBodiedGetterAndSetterWithInitializer()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25401")]
    public Task TestGetterAccessibilityDiffers()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25401")]
    public Task TestSetterAccessibilityDiffers()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26858")]
    public Task TestPreserveTrailingTrivia1()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26858")]
    public Task TestPreserveTrailingTrivia2()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26858")]
    public Task TestPreserveTrailingTrivia3()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26858")]
    public Task TestKeepLeadingBlank()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestMultipleFieldsAbove1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestMultipleFieldsAbove2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestMultipleFieldsAbove3()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestMultipleFieldsAbove4()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestMultipleFieldsBelow1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestMultipleFieldsBelow2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestMultipleFieldsBelow3()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestMultipleFieldsBelow4()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27675")]
    public Task TestSingleLineWithDirective()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27675")]
    public Task TestMultipleFieldsWithDirective()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27675")]
    public Task TestSingleLineWithDoubleDirectives()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40622")]
    public Task TestUseTabs()
        => TestInRegularAndScriptAsync(
            """
            public class Goo
            {
            	private readonly object o;

            	[||]public object O => o;
            }
            """,
            """
            public class Goo
            {
            	public object O { get; }
            }
            """, new TestParameters(options: Option(FormattingOptions2.UseTabs, true)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40622")]
    public Task TestUseSpaces()
        => TestInRegularAndScriptAsync(
            """
            public class Goo
            {
            	private readonly object o;

            	[||]public object O => o;
            }
            """,
            """
            public class Goo
            {
                public object O { get; }
            }
            """, new TestParameters(options: Option(FormattingOptions2.UseTabs, false)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40622")]
    public Task TestUseTabs_Editorconfig()
        => TestInRegularAndScriptAsync(
            """
            <Workspace>
                <Project Language = "C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document FilePath = "file.cs">
            public class Goo
            {
            	private readonly object o;

            	[||]public object O => o;
            }
                    </Document>
                    <AnalyzerConfigDocument FilePath = ".editorconfig">
            [*]
            indent_style = tab
            </AnalyzerConfigDocument>
                </Project>
            </Workspace>
            """,
            """
            <Workspace>
                <Project Language = "C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document FilePath = "file.cs">
            public class Goo
            {
            	public object O { get; }
            }
                    </Document>
                    <AnalyzerConfigDocument FilePath = ".editorconfig">
            [*]
            indent_style = tab
            </AnalyzerConfigDocument>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40622")]
    public Task TestUseSpaces_Editorconfig()
        => TestInRegularAndScriptAsync(
            """
            <Workspace>
                <Project Language = "C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document FilePath = "file.cs">
            public class Goo
            {
            	private readonly object o;

            	[||]public object O => o;
            }
                    </Document>
                    <AnalyzerConfigDocument FilePath = ".editorconfig">
            [*]
            indent_style = space
            </AnalyzerConfigDocument>
                </Project>
            </Workspace>
            """,
            """
            <Workspace>
                <Project Language = "C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document FilePath = "file.cs">
            public class Goo
            {
                public object O { get; }
            }
                    </Document>
                    <AnalyzerConfigDocument FilePath = ".editorconfig">
            [*]
            indent_style = space
            </AnalyzerConfigDocument>
                </Project>
            </Workspace>
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/34783")]
    public Task TestNotOnSerializableType()
        => TestMissingAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47999")]
    public Task TestPropertyIsReadOnlyAndSetterNeeded()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47999")]
    public Task TestPropertyIsReadOnlyWithNoAccessModifierAndSetterNeeded()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47999")]
    public Task TestPropertyIsReadOnlyAndSetterUnneeded()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestPropertyInRecordStruct()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38286")]
    public Task TestPointer1()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38286")]
    public Task TestPointer2()
        => TestMissingAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/25408")]
    public Task TestLinkedFile()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32597")]
    public Task TestUnassignedVariable1()
        => TestMissingAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32597")]
    public Task TestAssignedVariable1()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71032")]
    public Task TestWithInitProperty1()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71032")]
    public Task TestNotWithInitProperty1()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76634")]
    public Task TestRefField()
        => TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|ref int i|];

                int P
                {
                    get
                    {
                        return i;
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/78225")]
    public Task TestRefProperty1()
        => TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|int i|];

                ref int P
                {
                    get
                    {
                        return ref i;
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/78225")]
    public Task TestRefProperty2()
        => TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|int i|];

                ref int P => ref i;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/78225")]
    public Task TestRefProperty3()
        => TestMissingInRegularAndScriptAsync(
            """
            class Class
            {
                [|int i|];

                readonly ref int P => ref i;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/77011")]
    public Task TestRemoveThisIfPreferredCodeStyle()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                [|private readonly string a;|]

                public C(string a)
                {
                    this.a = a;
                }

                public string A => a;
            }
            """,
            """
            class C
            {
                public C(string a)
                {
                    A = a;
                }

                public string A { get; }
            }
            """,
            new(options: Option(CodeStyleOptions2.QualifyPropertyAccess, false, NotificationOption2.Error)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/77011")]
    public Task TestKeepThisIfPreferredCodeStyle()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                [|private readonly string a;|]

                public C(string a)
                {
                    this.a = a;
                }

                public string A => a;
            }
            """,
            """
            class C
            {
                public C(string a)
                {
                    this.A = a;
                }

                public string A { get; }
            }
            """,
            new(options: Option(CodeStyleOptions2.QualifyPropertyAccess, true, NotificationOption2.Error)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79734")]
    public Task TestAttributeOnAccessor()
        => TestInRegularAndScriptAsync(
            """
            class AAttribute : Attribute;

            class C
            {
                [|int _i;|]
                int I
                {
                    [A]
                    get => _i;
                }
            }
            """,
            """
            class AAttribute : Attribute;

            class C
            {
                int I
                {
                    [A]
                    get;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/pull/81179")]
    public Task TestNullableFieldNonNullableProperty_WithAllowNullAttribute1()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable
            class C
            {
                [|private string? _goo = "";|]

                public string Goo => _goo ?? throw new System.InvalidOperationException();

                public void Reset()
                {
                    _goo = null;
                }
            }
            """ + s_allowNullAttribute,
            """
            #nullable enable
            using System.Diagnostics.CodeAnalysis;

            class C
            {
                [AllowNull]
                public string Goo { get => field ?? throw new System.InvalidOperationException(); private set; } = "";

                public void Reset()
                {
                    Goo = null;
                }
            }
            """ + s_allowNullAttribute);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/pull/81179")]
    public Task TestNullableFieldNonNullableProperty_WithAllowNullAttribute2()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable
            class C
            {
                [|private string? _goo = "";|]

                public string Goo => _goo ?? throw new System.InvalidOperationException();

                public void Reset()
                {
                    this._goo = null;
                }
            }
            """ + s_allowNullAttribute,
            """
            #nullable enable
            using System.Diagnostics.CodeAnalysis;

            class C
            {
                [AllowNull]
                public string Goo { get => field ?? throw new System.InvalidOperationException(); private set; } = "";

                public void Reset()
                {
                    Goo = null;
                }
            }
            """ + s_allowNullAttribute);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/pull/81179")]
    public Task TestNullableFieldNonNullableProperty_NoNullWrites()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable
            class C
            {
                [|private string? _goo = "";|]
            
                public string Goo => _goo ?? throw new System.InvalidOperationException();

                public void Reset()
                {
                    _goo = "";
                }
            }
            """ + s_allowNullAttribute,
            """
            #nullable enable
            class C
            {
                public string Goo { get => field ?? throw new System.InvalidOperationException(); private set; } = "";

                public void Reset()
                {
                    Goo = "";
                }
            }
            """ + s_allowNullAttribute);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/pull/81179")]
    public Task TestNullableFieldNonNullableProperty_ReadOnly_NoAllowNull()
        => TestInRegularAndScriptAsync(
            """
            #nullable enable
            class C
            {
                [|private readonly string? _goo = "";|]
            
                public string Goo => _goo ?? throw new System.InvalidOperationException();
            }
            """ + s_allowNullAttribute,
            """
            #nullable enable
            class C
            {
                public string Goo { get => field ?? throw new System.InvalidOperationException(); } = "";
            }
            """ + s_allowNullAttribute);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/81320")]
    public Task TestStaticFieldWrittenInInstanceConstructor_ReadOnlyProperty()
        => TestInRegularAndScriptAsync(
            """
            public sealed class Test
            {
                [|private static Test? s_instance;|]
                public static Test Instance => s_instance!;

                public Test()
                {
                    s_instance = this;
                }
            }
            """,
            """
            public sealed class Test
            {
                public static Test Instance { get; private set; }

                public Test()
                {
                    Instance = this;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/XXXXX")]
    public Task TestStaticFieldWrittenInStaticConstructor_ReadOnlyProperty()
        => TestInRegularAndScriptAsync(
            """
            public sealed class Test
            {
                [|private static Test? s_instance;|]
                public static Test Instance => s_instance!;

                static Test()
                {
                    s_instance = new Test();
                }
            }
            """,
            """
            public sealed class Test
            {
                public static Test Instance { get; } = new Test();
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/XXXXX")]
    public Task TestStaticFieldWrittenInInstanceConstructor_WithSetter()
        => TestInRegularAndScriptAsync(
            """
            public sealed class Test
            {
                [|private static Test? s_instance;|]
                public static Test Instance
                {
                    get => s_instance!;
                    set => s_instance = value;
                }

                public Test()
                {
                    s_instance = this;
                }
            }
            """,
            """
            public sealed class Test
            {
                public static Test Instance { get; set; }

                public Test()
                {
                    Instance = this;
                }
            }
            """);
}
