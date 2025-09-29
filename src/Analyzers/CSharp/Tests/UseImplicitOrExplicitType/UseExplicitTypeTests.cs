// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.TypeStyle;
using Microsoft.CodeAnalysis.CSharp.TypeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.UseImplicitOrExplicitType;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
public sealed partial class UseExplicitTypeTests(ITestOutputHelper logger)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpUseExplicitTypeDiagnosticAnalyzer(), new UseExplicitTypeCodeFixProvider());

    private readonly CodeStyleOption2<bool> offWithSilent = new(false, NotificationOption2.Silent);
    private readonly CodeStyleOption2<bool> onWithInfo = new(true, NotificationOption2.Suggestion);
    private readonly CodeStyleOption2<bool> offWithInfo = new(false, NotificationOption2.Suggestion);
    private readonly CodeStyleOption2<bool> offWithWarning = new(false, NotificationOption2.Warning);
    private readonly CodeStyleOption2<bool> offWithError = new(false, NotificationOption2.Error);

    // specify all options explicitly to override defaults.
    private OptionsCollection ExplicitTypeEverywhere()
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.VarElsewhere, offWithInfo },
            { CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithInfo },
            { CSharpCodeStyleOptions.VarForBuiltInTypes, offWithInfo },
        };

    private OptionsCollection ExplicitTypeExceptWhereApparent()
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.VarElsewhere, offWithInfo },
            { CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithInfo },
            { CSharpCodeStyleOptions.VarForBuiltInTypes, offWithInfo },
        };

    private OptionsCollection ExplicitTypeForBuiltInTypesOnly()
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.VarElsewhere, onWithInfo },
            { CSharpCodeStyleOptions.VarWhenTypeIsApparent, onWithInfo },
            { CSharpCodeStyleOptions.VarForBuiltInTypes, offWithInfo },
        };

    private OptionsCollection ExplicitTypeEnforcements()
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.VarElsewhere, offWithWarning },
            { CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithError },
            { CSharpCodeStyleOptions.VarForBuiltInTypes, offWithInfo },
        };

    private OptionsCollection ExplicitTypeSilentEnforcement()
        => new(GetLanguage())
        {
            { CSharpCodeStyleOptions.VarElsewhere, offWithSilent },
            { CSharpCodeStyleOptions.VarWhenTypeIsApparent, offWithSilent },
            { CSharpCodeStyleOptions.VarForBuiltInTypes, offWithSilent },
        };

    #region Error Cases

    [Fact]
    public Task NotOnFieldDeclaration()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                [|var|] _myfield = 5;
            }
            """, new TestParameters(options: ExplicitTypeEverywhere()));

    [Fact]
    public Task NotOnFieldLikeEvents()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                public event [|var|] _myevent;
            }
            """, new TestParameters(options: ExplicitTypeEverywhere()));

    [Fact]
    public async Task OnAnonymousMethodExpression()
    {
        var before =
            """
            using System;

            class Program
            {
                void Method()
                {
                    [|var|] comparer = delegate (string value) {
                        return value != "0";
                    };
                }
            }
            """;
        var after =
            """
            using System;

            class Program
            {
                void Method()
                {
                    Func<string, bool> comparer = delegate (string value) {
                        return value != "0";
                    };
                }
            }
            """;
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeEverywhere()));
        await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeExceptWhereApparent()));
    }

    [Fact]
    public async Task OnLambdaExpression()
    {
        var before =
            """
            using System;

            class Program
            {
                void Method()
                {
                    [|var|] x = (int y) => y * y;
                }
            }
            """;
        var after =
            """
            using System;

            class Program
            {
                void Method()
                {
                    Func<int, int> x = (int y) => y * y;
                }
            }
            """;
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeEverywhere()));
        await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeExceptWhereApparent()));
    }

    [Fact]
    public Task NotOnDeclarationWithMultipleDeclarators()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Method()
                {
                    [|var|] x = 5, y = x;
                }
            }
            """, new TestParameters(options: ExplicitTypeEverywhere()));

    [Fact]
    public Task NotOnDeclarationWithoutInitializer()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Method()
                {
                    [|var|] x;
                }
            }
            """, new TestParameters(options: ExplicitTypeEverywhere()));

    [Fact]
    public Task NotDuringConflicts()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Method()
                {
                    [|var|] p = new var();
                }

                class var
                {
                }
            }
            """, new TestParameters(options: ExplicitTypeEverywhere()));

    [Fact]
    public Task NotIfAlreadyExplicitlyTyped()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Method()
                {
                    [|Program|] p = new Program();
                }
            }
            """, new TestParameters(options: ExplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27221")]
    public Task NotIfRefTypeAlreadyExplicitlyTyped()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            struct Program
            {
                void Method()
                {
                    ref [|Program|] p = Ref();
                }
                ref Program Ref() => throw null;
            }
            """, new TestParameters(options: ExplicitTypeEverywhere()));

    [Fact]
    public Task NotOnRHS()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    var c = new [|var|]();
                }
            }

            class var
            {
            }
            """);

    [Fact]
    public Task NotOnErrorSymbol()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Method()
                {
                    [|var|] x = new Goo();
                }
            }
            """, new TestParameters(options: ExplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29718")]
    public Task NotOnErrorConvertedType_ForEachVariableStatement()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    // Error CS1061: 'KeyValuePair<int, int>' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'KeyValuePair<int, int>' could be found (are you missing a using directive or an assembly reference?)
                    // Error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'KeyValuePair<int, int>', with 2 out parameters and a void return type.
                    foreach ([|var|] (key, value) in new Dictionary<int, int>())
                    {
                    }
                }
            }
            """, new TestParameters(options: ExplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29718")]
    public Task NotOnErrorConvertedType_AssignmentExpressionStatement()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;

            class C
            {
                void M(C c)
                {
                    // Error CS1061: 'C' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'C' could be found (are you missing a using directive or an assembly reference?)
                    // Error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'C', with 2 out parameters and a void return type.
                    [|var|] (key, value) = c;
                }
            }
            """, new TestParameters(options: ExplicitTypeEverywhere()));

    #endregion

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23907")]
    public async Task InArrayType()
    {
        var before = """
            class Program
            {
                void Method()
                {
                    [|var|] x = new Program[0];
                }
            }
            """;
        // The type is apparent and not intrinsic
        await TestInRegularAndScriptAsync(before, """
            class Program
            {
                void Method()
                {
                    Program[] x = new Program[0];
                }
            }
            """, new(options: ExplicitTypeEverywhere()));
        await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
        await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeExceptWhereApparent()));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23907")]
    public async Task InArrayTypeWithIntrinsicType()
    {
        var before = """
            class Program
            {
                void Method()
                {
                    [|var|] x = new int[0];
                }
            }
            """;
        var after = """
            class Program
            {
                void Method()
                {
                    int[] x = new int[0];
                }
            }
            """;
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeEverywhere()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeForBuiltInTypesOnly()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeExceptWhereApparent())); // preference for builtin types dominates
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23907")]
    public async Task InNullableIntrinsicType()
    {
        var before = """
            class Program
            {
                void Method(int? x)
                {
                    [|var|] y = x;
                }
            }
            """;
        var after = """
            class Program
            {
                void Method(int? x)
                {
                    int? y = x;
                }
            }
            """;
        // The type is intrinsic and not apparent
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeEverywhere()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeForBuiltInTypesOnly()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeExceptWhereApparent()));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42986")]
    public async Task InNativeIntIntrinsicType()
    {
        var before = """
            class Program
            {
                void Method(nint x)
                {
                    [|var|] y = x;
                }
            }
            """;
        var after = """
            class Program
            {
                void Method(nint x)
                {
                    nint y = x;
                }
            }
            """;
        // The type is intrinsic and not apparent
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeEverywhere()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeForBuiltInTypesOnly()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeExceptWhereApparent()));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42986")]
    public async Task InNativeUnsignedIntIntrinsicType()
    {
        var before = """
            class Program
            {
                void Method(nuint x)
                {
                    [|var|] y = x;
                }
            }
            """;
        var after = """
            class Program
            {
                void Method(nuint x)
                {
                    nuint y = x;
                }
            }
            """;
        // The type is intrinsic and not apparent
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeEverywhere()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeForBuiltInTypesOnly()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeExceptWhereApparent()));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27221")]
    public async Task WithRefIntrinsicType()
    {
        var before = """
            class Program
            {
                void Method()
                {
                    ref [|var|] y = Ref();
                }
                ref int Ref() => throw null;
            }
            """;
        var after = """
            class Program
            {
                void Method()
                {
                    ref int y = Ref();
                }
                ref int Ref() => throw null;
            }
            """;
        // The type is intrinsic and not apparent
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeEverywhere()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeForBuiltInTypesOnly()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeExceptWhereApparent()));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27221")]
    public async Task WithRefIntrinsicTypeInForeach()
    {
        var before = """
            class E
            {
                public ref int Current => throw null;
                public bool MoveNext() => throw null;
                public E GetEnumerator() => throw null;

                void M()
                {
                    foreach (ref [|var|] x in this) { }
                }
            }
            """;
        var after = """
            class E
            {
                public ref int Current => throw null;
                public bool MoveNext() => throw null;
                public E GetEnumerator() => throw null;

                void M()
                {
                    foreach (ref int x in this) { }
                }
            }
            """;
        // The type is intrinsic and not apparent
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeEverywhere()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeForBuiltInTypesOnly()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeExceptWhereApparent()));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23907")]
    public async Task InArrayOfNullableIntrinsicType()
    {
        var before = """
            class Program
            {
                void Method(int?[] x)
                {
                    [|var|] y = x;
                }
            }
            """;
        var after = """
            class Program
            {
                void Method(int?[] x)
                {
                    int?[] y = x;
                }
            }
            """;
        // The type is intrinsic and not apparent
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeEverywhere()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeForBuiltInTypesOnly()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeExceptWhereApparent()));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23907")]
    public async Task InNullableCustomType()
    {
        var before = """
            struct Program
            {
                void Method(Program? x)
                {
                    [|var|] y = x;
                }
            }
            """;
        var after = """
            struct Program
            {
                void Method(Program? x)
                {
                    Program? y = x;
                }
            }
            """;
        // The type is not intrinsic and not apparent
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeEverywhere()));
        await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeExceptWhereApparent()));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40477")]
    public async Task NullableType()
    {
        var before = """
            #nullable enable
            class Program
            {
                void Method(Program x)
                {
                    [|var|] y = x;
                    y = null;
                }
            }
            """;
        var after = """
            #nullable enable
            class Program
            {
                void Method(Program x)
                {
                    Program? y = x;
                    y = null;
                }
            }
            """;
        // The type is not intrinsic and not apparent
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeEverywhere()));
        await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeExceptWhereApparent()));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40477")]
    public async Task ObliviousType()
    {
        var before = """
            #nullable enable
            class Program
            {
                void Method(Program x)
                {
            #nullable disable
                    [|var|] y = x;
                    y = null;
                }
            }
            """;
        var after = """
            #nullable enable
            class Program
            {
                void Method(Program x)
                {
            #nullable disable
                    Program y = x;
                    y = null;
                }
            }
            """;
        // The type is not intrinsic and not apparent
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeEverywhere()));
        await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeExceptWhereApparent()));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40477")]
    public async Task NotNullableType()
    {
        var before = """
            class Program
            {
                void Method(Program x)
                {
            #nullable enable
                    [|var|] y = x;
                    y = null;
                }
            }
            """;
        var after = """
            class Program
            {
                void Method(Program x)
                {
            #nullable enable
                    Program? y = x;
                    y = null;
                }
            }
            """;
        // The type is not intrinsic and not apparent
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeEverywhere()));
        await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeExceptWhereApparent()));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40477")]
    public async Task NullableType_OutVar()
    {
        var before = """
            #nullable enable
            class Program
            {
                void Method(out Program? x)
                {
                    Method(out [|var|] y1);
                    throw null!;
                }
            }
            """;
        var after = """
            #nullable enable
            class Program
            {
                void Method(out Program? x)
                {
                    Method(out Program? y1);
                    throw null!;
                }
            }
            """;
        // The type is not intrinsic and not apparent
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeEverywhere()));
        await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeExceptWhereApparent()));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40477")]
    public async Task NotNullableType_OutVar()
    {
        var before = """
            #nullable enable
            class Program
            {
                void Method(out Program x)
                {
                    Method(out [|var|] y1);
                    throw null!;
                }
            }
            """;
        var after = """
            #nullable enable
            class Program
            {
                void Method(out Program x)
                {
                    Method(out Program? y1);
                    throw null!;
                }
            }
            """;
        // The type is not intrinsic and not apparent
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeEverywhere()));
        await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeExceptWhereApparent()));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40477")]
    public async Task ObliviousType_OutVar()
    {
        var before = """
            class Program
            {
                void Method(out Program x)
                {
                    Method(out [|var|] y1);
                    throw null;
                }
            }
            """;
        var after = """
            class Program
            {
                void Method(out Program x)
                {
                    Method(out Program y1);
                    throw null;
                }
            }
            """;
        // The type is not intrinsic and not apparent
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeEverywhere()));
        await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeExceptWhereApparent()));
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/40925")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/40477")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/40925")]
    public async Task NullableTypeAndNotNullableType_VarDeconstruction()
    {
        var before = """
            #nullable enable
            class Program2 { }
            class Program
            {
                void Method(Program? x, Program2 x2)
                {
                    [|var|] (y1, y2) = (x, x2);
                }
            }
            """;
        var after = """
            #nullable enable
            class Program2 { }
            class Program
            {
                void Method(Program? x, Program2 x2)
                {
                    (Program? y1, Program2? y2) = (x, x2);
                }
            }
            """;
        // The type is not intrinsic and not apparent
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeEverywhere()));
        await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeExceptWhereApparent()));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40477")]
    public async Task ObliviousType_VarDeconstruction()
    {
        var before = """
            #nullable enable
            class Program2 { }
            class Program
            {
                void Method(Program x, Program2 x2)
                {
            #nullable disable
                    [|var|] (y1, y2) = (x, x2);
                }
            }
            """;
        var after = """
            #nullable enable
            class Program2 { }
            class Program
            {
                void Method(Program x, Program2 x2)
                {
            #nullable disable
                    (Program y1, Program2 y2) = (x, x2);
                }
            }
            """;
        // The type is not intrinsic and not apparent
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeEverywhere()));
        await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeExceptWhereApparent()));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40477")]
    public async Task ObliviousType_Deconstruction()
    {
        var before = """
            #nullable enable
            class Program
            {
                void Method(Program x)
                {
            #nullable disable
                    ([|var|] y1, Program y2) = (x, x);
                }
            }
            """;
        var after = """
            #nullable enable
            class Program
            {
                void Method(Program x)
                {
            #nullable disable
                    (Program y1, Program y2) = (x, x);
                }
            }
            """;
        // The type is not intrinsic and not apparent
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeEverywhere()));
        await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeExceptWhereApparent()));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40477")]
    public async Task NotNullableType_Deconstruction()
    {
        var before = """
            class Program
            {
                void Method(Program x)
                {
            #nullable enable
                    ([|var|] y1, Program y2) = (x, x);
                }
            }
            """;
        var after = """
            class Program
            {
                void Method(Program x)
                {
            #nullable enable
                    (Program? y1, Program y2) = (x, x);
                }
            }
            """;
        // The type is not intrinsic and not apparent
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeEverywhere()));
        await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeExceptWhereApparent()));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40477")]
    public async Task NullableType_Deconstruction()
    {
        var before = """
            class Program
            {
                void Method(Program? x)
                {
            #nullable enable
                    ([|var|] y1, Program y2) = (x, x);
                }
            }
            """;
        var after = """
            class Program
            {
                void Method(Program? x)
                {
            #nullable enable
                    (Program? y1, Program y2) = (x, x);
                }
            }
            """;
        // The type is not intrinsic and not apparent
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeEverywhere()));
        await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeExceptWhereApparent()));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40477")]
    public async Task ObliviousType_Foreach()
    {
        var before = """
            #nullable enable
            class Program
            {
                void Method(System.Collections.Generic.IEnumerable<Program> x)
                {
            #nullable disable
                    foreach ([|var|] y in x)
                    {
                    }
                }
            }
            """;
        var after = """
            #nullable enable
            class Program
            {
                void Method(System.Collections.Generic.IEnumerable<Program> x)
                {
            #nullable disable
                    foreach ([|Program|] y in x)
                    {
                    }
                }
            }
            """;
        // The type is not intrinsic and not apparent
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeEverywhere()));
        await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeExceptWhereApparent()));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40477")]
    public async Task NotNullableType_Foreach()
    {
        var before = """
            class Program
            {
                void Method(System.Collections.Generic.IEnumerable<Program> x)
                {
            #nullable enable
                    foreach ([|var|] y in x)
                    {
                    }
                }
            }
            """;
        var after = """
            class Program
            {
                void Method(System.Collections.Generic.IEnumerable<Program> x)
                {
            #nullable enable
                    foreach (Program? y in x)
                    {
                    }
                }
            }
            """;
        // The type is not intrinsic and not apparent
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeEverywhere()));
        await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeExceptWhereApparent()));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40477")]
    public async Task NullableType_Foreach()
    {
        var before = """
            class Program
            {
                void Method(System.Collections.Generic.IEnumerable<Program> x)
                {
            #nullable enable
                    foreach ([|var|] y in x)
                    {
                    }
                }
            }
            """;
        var after = """
            class Program
            {
                void Method(System.Collections.Generic.IEnumerable<Program> x)
                {
            #nullable enable
                    foreach (Program? y in x)
                    {
                    }
                }
            }
            """;
        // The type is not intrinsic and not apparent
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeEverywhere()));
        await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeExceptWhereApparent()));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40477")]
    public async Task NotNullableType_ForeachVarDeconstruction1()
    {
        var before = """
            class Program
            {
                void Method(System.Collections.Generic.IEnumerable<(Program, Program)> x)
                {
            #nullable enable
                    foreach ([|var|] (y1, y2) in x)
                    {
                    }
                }
            }
            """;
        var after = """
            class Program
            {
                void Method(System.Collections.Generic.IEnumerable<(Program, Program)> x)
                {
            #nullable enable
                    foreach ((Program? y1, Program? y2) in x)
                    {
                    }
                }
            }
            """;
        // The type is not intrinsic and not apparent
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeEverywhere()));
        await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeExceptWhereApparent()));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47038")]
    public Task NotNullableType_ForeachVarDeconstruction2()
        => TestInRegularAndScriptAsync("""
            #nullable enable

            class C
            {
                public void Deconstruct(out string? s1, out string? s2)
                {
                    s1 = null;
                    s2 = null;
                }

                void M(C[] items)
                {
                    foreach ([||]var (s1, s2) in items)
                    {

                    }
                }
            }
            """, """
            #nullable enable

            class C
            {
                public void Deconstruct(out string? s1, out string? s2)
                {
                    s1 = null;
                    s2 = null;
                }

                void M(C[] items)
                {
                    foreach ((string? s1, string? s2) in items)
                    {

                    }
                }
            }
            """, new(options: ExplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/40477")]
    public async Task NotNullableType_ForeachDeconstruction()
    {
        var before = """
            class Program
            {
                void Method(System.Collections.Generic.IEnumerable<(Program, Program)> x)
                {
            #nullable enable
                    foreach (([|var|] y1, var y2) in x)
                    {
                    }
                }
            }
            """;
        var after = """
            class Program
            {
                void Method(System.Collections.Generic.IEnumerable<(Program, Program)> x)
                {
            #nullable enable
                    foreach ((Program? y1, var y2) in x)
                    {
                    }
                }
            }
            """;
        // The type is not intrinsic and not apparent
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeEverywhere()));
        await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeExceptWhereApparent()));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23907")]
    public async Task InPointerTypeWithIntrinsicType()
    {
        var before = """
            unsafe class Program
            {
                void Method(int* y)
                {
                    [|var|] x = y;
                }
            }
            """;
        var after = """
            unsafe class Program
            {
                void Method(int* y)
                {
                    int* x = y;
                }
            }
            """;
        // The type is intrinsic and not apparent
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeEverywhere()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeForBuiltInTypesOnly()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeExceptWhereApparent())); // preference for builtin types dominates
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23907")]
    public async Task InPointerTypeWithCustomType()
    {
        var before = """
            unsafe class Program
            {
                void Method(Program* y)
                {
                    [|var|] x = y;
                }
            }
            """;
        var after = """
            unsafe class Program
            {
                void Method(Program* y)
                {
                    Program* x = y;
                }
            }
            """;
        // The type is not intrinsic and not apparent
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeEverywhere()));
        await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeExceptWhereApparent()));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23893")]
    public async Task InOutParameter()
    {
        var before = """
            class Program
            {
                void Method(out int x)
                {
                    Method(out [|var|] x);
                }
            }
            """;
        var after = """
            class Program
            {
                void Method(out int x)
                {
                    Method(out int x);
                }
            }
            """;
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeEverywhere()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeForBuiltInTypesOnly()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeExceptWhereApparent()));
    }

    [Fact]
    public Task NotOnDynamic()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Method()
                {
                    [|dynamic|] x = 1;
                }
            }
            """, new TestParameters(options: ExplicitTypeEverywhere()));

    [Fact]
    public Task NotOnForEachVarWithAnonymousType()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Linq;

            class Program
            {
                void Method()
                {
                    var values = Enumerable.Range(1, 5).Select(i => new { Value = i });

                    foreach ([|var|] value in values)
                    {
                        Console.WriteLine(value.Value);
                    }
                }
            }
            """, new TestParameters(options: ExplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23752")]
    public Task OnDeconstructionVarParens()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class Program
            {
                void M()
                {
                    [|var|] (x, y) = new Program();
                }
                void Deconstruct(out int i, out string s) { i = 1; s = "hello"; }
            }
            """, """
            using System;
            class Program
            {
                void M()
                {
                    (int x, string y) = new Program();
                }
                void Deconstruct(out int i, out string s) { i = 1; s = "hello"; }
            }
            """, new(options: ExplicitTypeEverywhere()));

    [Fact]
    public Task OnDeconstructionVar()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class Program
            {
                void M()
                {
                    ([|var|] x, var y) = new Program();
                }
                void Deconstruct(out int i, out string s) { i = 1; s = "hello"; }
            }
            """, """
            using System;
            class Program
            {
                void M()
                {
                    (int x, var y) = new Program();
                }
                void Deconstruct(out int i, out string s) { i = 1; s = "hello"; }
            }
            """, new(options: ExplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23752")]
    public Task OnNestedDeconstructionVar()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class Program
            {
                void M()
                {
                    [|var|] (x, (y, z)) = new Program();
                }
                void Deconstruct(out int i, out Program s) { i = 1; s = null; }
            }
            """, """
            using System;
            class Program
            {
                void M()
                {
                    (int x, (int y, Program z)) = new Program();
                }
                void Deconstruct(out int i, out Program s) { i = 1; s = null; }
            }
            """, new(options: ExplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23752")]
    public Task OnBadlyFormattedNestedDeconstructionVar()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class Program
            {
                void M()
                {
                    [|var|](x,(y,z)) = new Program();
                }
                void Deconstruct(out int i, out Program s) { i = 1; s = null; }
            }
            """, """
            using System;
            class Program
            {
                void M()
                {
                    (int x, (int y, Program z)) = new Program();
                }
                void Deconstruct(out int i, out Program s) { i = 1; s = null; }
            }
            """, new(options: ExplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23752")]
    public Task OnForeachNestedDeconstructionVar()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class Program
            {
                void M()
                {
                    foreach ([|var|] (x, (y, z)) in new[] { new Program() } { }
                }
                void Deconstruct(out int i, out Program s) { i = 1; s = null; }
            }
            """, """
            using System;
            class Program
            {
                void M()
                {
                    foreach ((int x, (int y, Program z)) in new[] { new Program() } { }
                }
                void Deconstruct(out int i, out Program s) { i = 1; s = null; }
            }
            """, new(options: ExplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23752")]
    public Task OnNestedDeconstructionVarWithTrivia()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class Program
            {
                void M()
                {
                    /*before*/[|var|]/*after*/ (/*x1*/x/*x2*/, /*yz1*/(/*y1*/y/*y2*/, /*z1*/z/*z2*/)/*yz2*/) /*end*/ = new Program();
                }
                void Deconstruct(out int i, out Program s) { i = 1; s = null; }
            }
            """, """
            using System;
            class Program
            {
                void M()
                {
                    /*before*//*after*/(/*x1*/int x/*x2*/, /*yz1*/(/*y1*/int y/*y2*/, /*z1*/Program z/*z2*/)/*yz2*/) /*end*/ = new Program();
                }
                void Deconstruct(out int i, out Program s) { i = 1; s = null; }
            }
            """, new(options: ExplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23752")]
    public Task OnDeconstructionVarWithDiscard()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class Program
            {
                void M()
                {
                    [|var|] (x, _) = new Program();
                }
                void Deconstruct(out int i, out string s) { i = 1; s = "hello"; }
            }
            """, """
            using System;
            class Program
            {
                void M()
                {
                    (int x, string _) = new Program();
                }
                void Deconstruct(out int i, out string s) { i = 1; s = "hello"; }
            }
            """, new(options: ExplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23752")]
    public Task OnDeconstructionVarWithErrorType()
        => TestInRegularAndScriptAsync(
            """
            using System;
            class Program
            {
                void M()
                {
                    [|var|] (x, y) = new Program();
                }
                void Deconstruct(out int i, out Error s) { i = 1; s = null; }
            }
            """, """
            using System;
            class Program
            {
                void M()
                {
                    (int x, Error y) = new Program();
                }
                void Deconstruct(out int i, out Error s) { i = 1; s = null; }
            }
            """, new(options: ExplicitTypeEverywhere()));

    [Fact]
    public Task OnForEachVarWithExplicitType()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Linq;

            class Program
            {
                void Method()
                {
                    var values = Enumerable.Range(1, 5);

                    foreach ([|var|] value in values)
                    {
                        Console.WriteLine(value.Value);
                    }
                }
            }
            """,
            """
            using System;
            using System.Linq;

            class Program
            {
                void Method()
                {
                    var values = Enumerable.Range(1, 5);

                    foreach (int value in values)
                    {
                        Console.WriteLine(value.Value);
                    }
                }
            }
            """, new(options: ExplicitTypeEverywhere()));

    [Fact]
    public Task NotOnAnonymousType()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Method()
                {
                    [|var|] x = new { Amount = 108, Message = "Hello" };
                }
            }
            """, new TestParameters(options: ExplicitTypeEverywhere()));

    [Fact]
    public Task NotOnArrayOfAnonymousType()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Method()
                {
                    [|var|] x = new[] { new { name = "apple", diam = 4 }, new { name = "grape", diam = 1 } };
                }
            }
            """, new TestParameters(options: ExplicitTypeEverywhere()));

    [Fact]
    public Task NotOnEnumerableOfAnonymousTypeFromAQueryExpression()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                void Method()
                {
                    var products = new List<Product>();
                    [|var|] productQuery = from prod in products
                                       select new { prod.Color, prod.Price };
                }
            }

            class Product
            {
                public ConsoleColor Color { get; set; }
                public int Price { get; set; }
            }
            """);

    [Fact]
    public Task SuggestExplicitTypeOnLocalWithIntrinsicTypeString()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void M()
                {
                    [|var|] s = "hello";
                }
            }
            """,
            """
            using System;

            class C
            {
                static void M()
                {
                    string s = "hello";
                }
            }
            """, new(options: ExplicitTypeEverywhere()));

    [Fact]
    public Task SuggestExplicitTypeOnIntrinsicType()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void M()
                {
                    [|var|] s = 5;
                }
            }
            """,
            """
            using System;

            class C
            {
                static void M()
                {
                    int s = 5;
                }
            }
            """, new(options: ExplicitTypeEverywhere()));

    [Fact]
    public Task SuggestExplicitTypeOnFrameworkType()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                static void M()
                {
                    [|var|] c = new List<int>();
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                static void M()
                {
                    List<int> c = new List<int>();
                }
            }
            """, new(options: ExplicitTypeEverywhere()));

    [Fact]
    public Task SuggestExplicitTypeOnUserDefinedType()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    [|var|] c = new C();
                }
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    C c = new C();
                }
            }
            """, new(options: ExplicitTypeEverywhere()));

    [Fact]
    public Task SuggestExplicitTypeOnGenericType()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C<T>
            {
                static void M()
                {
                    [|var|] c = new C<int>();
                }
            }
            """,
            """
            using System;

            class C<T>
            {
                static void M()
                {
                    C<int> c = new C<int>();
                }
            }
            """, new(options: ExplicitTypeEverywhere()));

    [Fact]
    public Task SuggestExplicitTypeOnSingleDimensionalArrayTypeWithNewOperator()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void M()
                {
                    [|var|] n1 = new int[4] { 2, 4, 6, 8 };
                }
            }
            """,
            """
            using System;

            class C
            {
                static void M()
                {
                    int[] n1 = new int[4] { 2, 4, 6, 8 };
                }
            }
            """, new(options: ExplicitTypeEverywhere()));

    [Fact]
    public Task SuggestExplicitTypeOnSingleDimensionalArrayTypeWithNewOperator2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void M()
                {
                    [|var|] n1 = new[] { 2, 4, 6, 8 };
                }
            }
            """,
            """
            using System;

            class C
            {
                static void M()
                {
                    int[] n1 = new[] { 2, 4, 6, 8 };
                }
            }
            """, new(options: ExplicitTypeEverywhere()));

    [Fact]
    public Task SuggestExplicitTypeOnSingleDimensionalJaggedArrayType()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void M()
                {
                    [|var|] cs = new[] {
                        new[] { 1, 2, 3, 4 },
                        new[] { 5, 6, 7, 8 }
                    };
                }
            }
            """,
            """
            using System;

            class C
            {
                static void M()
                {
                    int[][] cs = new[] {
                        new[] { 1, 2, 3, 4 },
                        new[] { 5, 6, 7, 8 }
                    };
                }
            }
            """, new(options: ExplicitTypeEverywhere()));

    [Fact]
    public Task SuggestExplicitTypeOnDeclarationWithObjectInitializer()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void M()
                {
                    [|var|] cc = new Customer { City = "Chennai" };
                }

                private class Customer
                {
                    public string City { get; set; }
                }
            }
            """,
            """
            using System;

            class C
            {
                static void M()
                {
                    Customer cc = new Customer { City = "Chennai" };
                }

                private class Customer
                {
                    public string City { get; set; }
                }
            }
            """, new(options: ExplicitTypeEverywhere()));

    [Fact]
    public Task SuggestExplicitTypeOnDeclarationWithCollectionInitializer()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;

            class C
            {
                static void M()
                {
                    [|var|] digits = new List<int> { 1, 2, 3 };
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;

            class C
            {
                static void M()
                {
                    List<int> digits = new List<int> { 1, 2, 3 };
                }
            }
            """, new(options: ExplicitTypeEverywhere()));

    [Fact]
    public Task SuggestExplicitTypeOnDeclarationWithCollectionAndObjectInitializers()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;

            class C
            {
                static void M()
                {
                    [|var|] cs = new List<Customer>
                    {
                        new Customer { City = "Chennai" }
                    };
                }

                private class Customer
                {
                    public string City { get; set; }
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;

            class C
            {
                static void M()
                {
                    List<Customer> cs = new List<Customer>
                    {
                        new Customer { City = "Chennai" }
                    };
                }

                private class Customer
                {
                    public string City { get; set; }
                }
            }
            """, new(options: ExplicitTypeEverywhere()));

    [Fact]
    public Task SuggestExplicitTypeOnForStatement()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void M()
                {
                    for ([|var|] i = 0; i < 5; i++)
                    {
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                static void M()
                {
                    for (int i = 0; i < 5; i++)
                    {
                    }
                }
            }
            """, new(options: ExplicitTypeEverywhere()));

    [Fact]
    public Task SuggestExplicitTypeOnForeachStatement()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;

            class C
            {
                static void M()
                {
                    var l = new List<int> { 1, 3, 5 };
                    foreach ([|var|] item in l)
                    {
                    }
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;

            class C
            {
                static void M()
                {
                    var l = new List<int> { 1, 3, 5 };
                    foreach (int item in l)
                    {
                    }
                }
            }
            """, new(options: ExplicitTypeEverywhere()));

    [Fact]
    public Task SuggestExplicitTypeOnQueryExpression()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                static void M()
                {
                    var customers = new List<Customer>();
                    [|var|] expr = from c in customers
                               where c.City == "London"
                               select c;
                }

                private class Customer
                {
                    public string City { get; set; }
                }
            }
            }
            """,
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                static void M()
                {
                    var customers = new List<Customer>();
                    IEnumerable<Customer> expr = from c in customers
                               where c.City == "London"
                               select c;
                }

                private class Customer
                {
                    public string City { get; set; }
                }
            }
            }
            """, new(options: ExplicitTypeEverywhere()));

    [Fact]
    public Task SuggestExplicitTypeInUsingStatement()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void M()
                {
                    using ([|var|] r = new Res())
                    {
                    }
                }

                private class Res : IDisposable
                {
                    public void Dispose()
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                static void M()
                {
                    using (Res r = new Res())
                    {
                    }
                }

                private class Res : IDisposable
                {
                    public void Dispose()
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            """, new(options: ExplicitTypeEverywhere()));

    [Fact]
    public Task SuggestExplicitTypeOnInterpolatedString()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Method()
                {
                    [|var|] s = $"Hello, {name}"
                }
            }
            """,
            """
            using System;

            class Program
            {
                void Method()
                {
                    string s = $"Hello, {name}"
                }
            }
            """, new(options: ExplicitTypeEverywhere()));

    [Fact]
    public Task SuggestExplicitTypeOnExplicitConversion()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void M()
                {
                    double x = 1234.7;
                    [|var|] a = (int)x;
                }
            }
            """,
            """
            using System;

            class C
            {
                static void M()
                {
                    double x = 1234.7;
                    int a = (int)x;
                }
            }
            """, new(options: ExplicitTypeEverywhere()));

    [Fact]
    public Task SuggestExplicitTypeOnConditionalAccessExpression()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void M()
                {
                    C obj = new C();
                    [|var|] anotherObj = obj?.Test();
                }

                C Test()
                {
                    return this;
                }
            }
            """,
            """
            using System;

            class C
            {
                static void M()
                {
                    C obj = new C();
                    C anotherObj = obj?.Test();
                }

                C Test()
                {
                    return this;
                }
            }
            """, new(options: ExplicitTypeEverywhere()));

    [Fact]
    public Task SuggestExplicitTypeInCheckedExpression()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void M()
                {
                    long number1 = int.MaxValue + 20L;
                    [|var|] intNumber = checked((int)number1);
                }
            }
            """,
            """
            using System;

            class C
            {
                static void M()
                {
                    long number1 = int.MaxValue + 20L;
                    int intNumber = checked((int)number1);
                }
            }
            """, new(options: ExplicitTypeEverywhere()));

    [Fact]
    public Task SuggestExplicitTypeInAwaitExpression()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public async void ProcessRead()
                {
                    [|var|] text = await ReadTextAsync(null);
                }

                private async Task<string> ReadTextAsync(string filePath)
                {
                    return string.Empty;
                }
            }
            """,
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public async void ProcessRead()
                {
                    string text = await ReadTextAsync(null);
                }

                private async Task<string> ReadTextAsync(string filePath)
                {
                    return string.Empty;
                }
            }
            """, new(options: ExplicitTypeEverywhere()));

    [Fact]
    public Task SuggestExplicitTypeInBuiltInNumericType()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                public void ProcessRead()
                {
                    [|var|] text = 1;
                }
            }
            """,
            """
            using System;

            class C
            {
                public void ProcessRead()
                {
                    int text = 1;
                }
            }
            """, new(options: ExplicitTypeForBuiltInTypesOnly()));

    [Fact]
    public Task SuggestExplicitTypeInBuiltInCharType()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                public void ProcessRead()
                {
                    [|var|] text = GetChar();
                }

                public char GetChar() => 'c';
            }
            """,
            """
            using System;

            class C
            {
                public void ProcessRead()
                {
                    char text = GetChar();
                }

                public char GetChar() => 'c';
            }
            """, new(options: ExplicitTypeForBuiltInTypesOnly()));

    [Fact]
    public Task SuggestExplicitTypeInBuiltInType_string()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                public void ProcessRead()
                {
                    [|var|] text = string.Empty;
                }
            }
            """,
            """
            using System;

            class C
            {
                public void ProcessRead()
                {
                    string text = string.Empty;
                }
            }
            """, new(options: ExplicitTypeForBuiltInTypesOnly()));

    [Fact]
    public Task SuggestExplicitTypeInBuiltInType_object()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                public void ProcessRead()
                {
                    object j = new C();
                    [|var|] text = j;
                }
            }
            """,
            """
            using System;

            class C
            {
                public void ProcessRead()
                {
                    object j = new C();
                    object text = j;
                }
            }
            """, new(options: ExplicitTypeForBuiltInTypesOnly()));

    [Fact]
    public Task SuggestExplicitTypeNotificationLevelSilent()
        => TestDiagnosticInfoAsync("""
            using System;
            class C
            {
                static void M()
                {
                    [|var|] n1 = new C();
                }
            }
            """,
            options: ExplicitTypeSilentEnforcement(),
            diagnosticId: IDEDiagnosticIds.UseExplicitTypeDiagnosticId,
            diagnosticSeverity: DiagnosticSeverity.Hidden);

    [Fact]
    public Task SuggestExplicitTypeNotificationLevelInfo()
        => TestDiagnosticInfoAsync("""
            using System;
            class C
            {
                static void M()
                {
                    [|var|] s = 5;
                }
            }
            """,
            options: ExplicitTypeEnforcements(),
            diagnosticId: IDEDiagnosticIds.UseExplicitTypeDiagnosticId,
            diagnosticSeverity: DiagnosticSeverity.Info);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23907")]
    public Task SuggestExplicitTypeNotificationLevelWarning()
        => TestDiagnosticInfoAsync("""
            using System;
            class C
            {
                static void M()
                {
                    [|var|] n1 = new[] { new C() }; // type not apparent and not intrinsic
                }
            }
            """,
            options: ExplicitTypeEnforcements(),
            diagnosticId: IDEDiagnosticIds.UseExplicitTypeDiagnosticId,
            diagnosticSeverity: DiagnosticSeverity.Warning);

    [Fact]
    public Task SuggestExplicitTypeNotificationLevelError()
        => TestDiagnosticInfoAsync("""
            using System;
            class C
            {
                static void M()
                {
                    [|var|] n1 = new C();
                }
            }
            """,
            options: ExplicitTypeEnforcements(),
            diagnosticId: IDEDiagnosticIds.UseExplicitTypeDiagnosticId,
            diagnosticSeverity: DiagnosticSeverity.Error);

    [Fact]
    public Task SuggestExplicitTypeOnLocalWithIntrinsicTypeTuple()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                static void M()
                {
                    [|var|] s = (1, "hello");
                }
            }
            """,
            """
            class C
            {
                static void M()
                {
                    (int, string) s = (1, "hello");
                }
            }
            """,
            new(options: ExplicitTypeEverywhere()));

    [Fact]
    public Task SuggestExplicitTypeOnLocalWithIntrinsicTypeTupleWithNames()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                static void M()
                {
                    [|var|] s = (a: 1, b: "hello");
                }
            }
            """,
            """
            class C
            {
                static void M()
                {
                    (int a, string b) s = (a: 1, b: "hello");
                }
            }
            """,
            new(options: ExplicitTypeEverywhere()));

    [Fact]
    public Task SuggestExplicitTypeOnLocalWithIntrinsicTypeTupleWithOneName()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                static void M()
                {
                    [|var|] s = (a: 1, "hello");
                }
            }
            """,
            """
            class C
            {
                static void M()
                {
                    (int a, string) s = (a: 1, "hello");
                }
            }
            """,
            new(options: ExplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20437")]
    public Task SuggestExplicitTypeOnDeclarationExpressionSyntax()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void M()
                {
                    DateTime.TryParse(string.Empty, [|out var|] date);
                }
            }
            """,
            """
            using System;

            class C
            {
                static void M()
                {
                    DateTime.TryParse(string.Empty, out DateTime date);
                }
            }
            """,
            new(options: ExplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20244")]
    public Task ExplicitTypeOnPredefinedTypesByTheirMetadataNames1()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Method()
                {
                    [|String|] test = new String(' ', 4);
                }
            }
            """, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20244")]
    public Task ExplicitTypeOnPredefinedTypesByTheirMetadataNames2()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Main()
                {
                    foreach ([|String|] test in new String[] { "test1", "test2" })
                    {
                    }
                }
            }
            """, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20244")]
    public Task ExplicitTypeOnPredefinedTypesByTheirMetadataNames3()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Main()
                {
                    [|Int32[]|] array = new[] { 1, 2, 3 };
                }
            }
            """, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20244")]
    public Task ExplicitTypeOnPredefinedTypesByTheirMetadataNames4()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Main()
                {
                    [|Int32[][]|] a = new Int32[][]
                    {
                        new[] { 1, 2 },
                        new[] { 3, 4 }
                    };
                }
            }
            """, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20244")]
    public Task ExplicitTypeOnPredefinedTypesByTheirMetadataNames5()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;

            class Program
            {
                void Main()
                {
                    [|IEnumerable<Int32>|] a = new List<Int32> { 1, 2 }.Where(x => x > 1);
                }
            }
            """, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20244")]
    public Task ExplicitTypeOnPredefinedTypesByTheirMetadataNames6()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Main()
                {
                    String name = "name";
                    [|String|] s = $"Hello, {name}"
                }
            }
            """, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20244")]
    public Task ExplicitTypeOnPredefinedTypesByTheirMetadataNames7()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Main()
                {
                    Object name = "name";
                    [|String|] s = (String) name;
                }
            }
            """, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20244")]
    public Task ExplicitTypeOnPredefinedTypesByTheirMetadataNames8()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Threading.Tasks;

            class C
            {
                public async void ProcessRead()
                {
                    [|String|] text = await ReadTextAsync(null);
                }

                private async Task<string> ReadTextAsync(string filePath)
                {
                    return String.Empty;
                }
            }
            """, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20244")]
    public Task ExplicitTypeOnPredefinedTypesByTheirMetadataNames9()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Main()
                {
                    String number = "12";
                    Int32.TryParse(name, out [|Int32|] number)
                }
            }
            """, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20244")]
    public Task ExplicitTypeOnPredefinedTypesByTheirMetadataNames10()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                void Main()
                {
                    for ([|Int32|] i = 0; i < 5; i++)
                    {
                    }
                }
            }
            """, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20244")]
    public Task ExplicitTypeOnPredefinedTypesByTheirMetadataNames11()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;

            class Program
            {
                void Main()
                {
                    [|List<Int32>|] a = new List<Int32> { 1, 2 };
                }
            }
            """, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26923")]
    public Task NoSuggestionOnForeachCollectionExpression()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;

            class Program
            {
                void Method(List<int> var)
                {
                    foreach (int value in [|var|])
                    {
                        Console.WriteLine(value.Value);
                    }
                }
            }
            """, new TestParameters(options: ExplicitTypeEverywhere()));

    [Fact]
    public Task NotOnConstVar()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    const [|var|] v = 0;
                }
            }
            """, new TestParameters(options: ExplicitTypeEverywhere()));

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/23907")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/24034")]
    public async Task WithNormalFuncSynthesizedLambdaType()
    {
        var before = """
            class Program
            {
                void Method()
                {
                    [|var|] x = (int i) => i.ToString();
                }
            }
            """;
        var after = """
            using System;

            class Program
            {
                void Method()
                {
                    Func<int, string> x = (int i) => i.ToString();
                }
            }
            """;
        // The type is not apparent and not intrinsic
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeEverywhere()));
        await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
        await TestInRegularAndScriptAsync(before, after, new(options: ExplicitTypeExceptWhereApparent()));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23907")]
    public async Task WithAnonymousSynthesizedLambdaType()
    {
        var before = """
            class Program
            {
                void Method()
                {
                    [|var|] x = (ref int i) => i.ToString();
                }
            }
            """;
        // The type is apparent and not intrinsic
        await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeEverywhere()));
        await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
        await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeExceptWhereApparent()));
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58404")]
    public Task TestLambdaNaturalType()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                static void M()
                {
                    [|var|] s = int () => { };
                }
            }
            """,
            """
            using System;

            class C
            {
                static void M()
                {
                    Func<int> s = int () => { };
                }
            }
            """, new(options: ExplicitTypeEverywhere()));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74372")]
    public Task TestAnonymousType()
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Linq;

            public class Temp
            {
                public void temp()
                {
                    var y = new[] { new { t = 0 } }.ToList();

                    y.ToDictionary(x => x.t, x => x).TryGetValue(0, out [|var|] y2);
                }
            }
            """, new TestParameters(options: ExplicitTypeEverywhere()));
}
