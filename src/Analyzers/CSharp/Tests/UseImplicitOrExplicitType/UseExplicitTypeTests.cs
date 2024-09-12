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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.UseExplicitType
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsUseExplicitType)]
    public partial class UseExplicitTypeTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
    {
        public UseExplicitTypeTests(ITestOutputHelper logger)
          : base(logger)
        {
        }

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
        public async Task NotOnFieldDeclaration()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                using System;

                class Program
                {
                    [|var|] _myfield = 5;
                }
                """, new TestParameters(options: ExplicitTypeEverywhere()));
        }

        [Fact]
        public async Task NotOnFieldLikeEvents()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                using System;

                class Program
                {
                    public event [|var|] _myevent;
                }
                """, new TestParameters(options: ExplicitTypeEverywhere()));
        }

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
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeEverywhere());
            await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeExceptWhereApparent());
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
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeEverywhere());
            await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeExceptWhereApparent());
        }

        [Fact]
        public async Task NotOnDeclarationWithMultipleDeclarators()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task NotOnDeclarationWithoutInitializer()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task NotDuringConflicts()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task NotIfAlreadyExplicitlyTyped()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27221")]
        public async Task NotIfRefTypeAlreadyExplicitlyTyped()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task NotOnRHS()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task NotOnErrorSymbol()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29718")]
        public async Task NotOnErrorConvertedType_ForEachVariableStatement()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29718")]
        public async Task NotOnErrorConvertedType_AssignmentExpressionStatement()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

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
            var after = """
                class Program
                {
                    void Method()
                    {
                        Program[] x = new Program[0];
                    }
                }
                """;
            // The type is apparent and not intrinsic
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeEverywhere());
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
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeEverywhere());
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeForBuiltInTypesOnly());
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeExceptWhereApparent()); // preference for builtin types dominates
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
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeEverywhere());
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeForBuiltInTypesOnly());
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeExceptWhereApparent());
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
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeEverywhere());
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeForBuiltInTypesOnly());
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeExceptWhereApparent());
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
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeEverywhere());
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeForBuiltInTypesOnly());
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeExceptWhereApparent());
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
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeEverywhere());
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeForBuiltInTypesOnly());
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeExceptWhereApparent());
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
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeEverywhere());
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeForBuiltInTypesOnly());
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeExceptWhereApparent());
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
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeEverywhere());
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeForBuiltInTypesOnly());
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeExceptWhereApparent());
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
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeEverywhere());
            await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeExceptWhereApparent());
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
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeEverywhere());
            await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeExceptWhereApparent());
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
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeEverywhere());
            await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeExceptWhereApparent());
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
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeEverywhere());
            await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeExceptWhereApparent());
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
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeEverywhere());
            await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeExceptWhereApparent());
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
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeEverywhere());
            await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeExceptWhereApparent());
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
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeEverywhere());
            await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeExceptWhereApparent());
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
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeEverywhere());
            await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeExceptWhereApparent());
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
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeEverywhere());
            await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeExceptWhereApparent());
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
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeEverywhere());
            await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeExceptWhereApparent());
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
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeEverywhere());
            await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeExceptWhereApparent());
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
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeEverywhere());
            await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeExceptWhereApparent());
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
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeEverywhere());
            await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeExceptWhereApparent());
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
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeEverywhere());
            await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeExceptWhereApparent());
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
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeEverywhere());
            await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeExceptWhereApparent());
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/37491")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/40477")]
        public async Task NotNullableType_ForeachVarDeconstruction()
        {
            // Semantic model doesn't yet handle var deconstruction foreach
            // https://github.com/dotnet/roslyn/issues/37491
            // https://github.com/dotnet/roslyn/issues/35010
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
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeEverywhere());
            await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeExceptWhereApparent());
        }

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
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeEverywhere());
            await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeExceptWhereApparent());
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
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeEverywhere());
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeForBuiltInTypesOnly());
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeExceptWhereApparent()); // preference for builtin types dominates
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
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeEverywhere());
            await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeExceptWhereApparent());
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
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeEverywhere());
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeForBuiltInTypesOnly());
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeExceptWhereApparent());
        }

        [Fact]
        public async Task NotOnDynamic()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task NotOnForEachVarWithAnonymousType()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23752")]
        public async Task OnDeconstructionVarParens()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: ExplicitTypeEverywhere());
        }

        [Fact]
        public async Task OnDeconstructionVar()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: ExplicitTypeEverywhere());
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23752")]
        public async Task OnNestedDeconstructionVar()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: ExplicitTypeEverywhere());
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23752")]
        public async Task OnBadlyFormattedNestedDeconstructionVar()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: ExplicitTypeEverywhere());
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23752")]
        public async Task OnForeachNestedDeconstructionVar()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: ExplicitTypeEverywhere());
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23752")]
        public async Task OnNestedDeconstructionVarWithTrivia()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: ExplicitTypeEverywhere());
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23752")]
        public async Task OnDeconstructionVarWithDiscard()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: ExplicitTypeEverywhere());
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23752")]
        public async Task OnDeconstructionVarWithErrorType()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: ExplicitTypeEverywhere());
        }

        [Fact]
        public async Task OnForEachVarWithExplicitType()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: ExplicitTypeEverywhere());
        }

        [Fact]
        public async Task NotOnAnonymousType()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task NotOnArrayOfAnonymousType()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task NotOnEnumerableOfAnonymousTypeFromAQueryExpression()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task SuggestExplicitTypeOnLocalWithIntrinsicTypeString()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: ExplicitTypeEverywhere());
        }

        [Fact]
        public async Task SuggestExplicitTypeOnIntrinsicType()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: ExplicitTypeEverywhere());
        }

        [Fact]
        public async Task SuggestExplicitTypeOnFrameworkType()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: ExplicitTypeEverywhere());
        }

        [Fact]
        public async Task SuggestExplicitTypeOnUserDefinedType()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: ExplicitTypeEverywhere());
        }

        [Fact]
        public async Task SuggestExplicitTypeOnGenericType()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: ExplicitTypeEverywhere());
        }

        [Fact]
        public async Task SuggestExplicitTypeOnSingleDimensionalArrayTypeWithNewOperator()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: ExplicitTypeEverywhere());
        }

        [Fact]
        public async Task SuggestExplicitTypeOnSingleDimensionalArrayTypeWithNewOperator2()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: ExplicitTypeEverywhere());
        }

        [Fact]
        public async Task SuggestExplicitTypeOnSingleDimensionalJaggedArrayType()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: ExplicitTypeEverywhere());
        }

        [Fact]
        public async Task SuggestExplicitTypeOnDeclarationWithObjectInitializer()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: ExplicitTypeEverywhere());
        }

        [Fact]
        public async Task SuggestExplicitTypeOnDeclarationWithCollectionInitializer()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: ExplicitTypeEverywhere());
        }

        [Fact]
        public async Task SuggestExplicitTypeOnDeclarationWithCollectionAndObjectInitializers()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: ExplicitTypeEverywhere());
        }

        [Fact]
        public async Task SuggestExplicitTypeOnForStatement()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: ExplicitTypeEverywhere());
        }

        [Fact]
        public async Task SuggestExplicitTypeOnForeachStatement()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: ExplicitTypeEverywhere());
        }

        [Fact]
        public async Task SuggestExplicitTypeOnQueryExpression()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: ExplicitTypeEverywhere());
        }

        [Fact]
        public async Task SuggestExplicitTypeInUsingStatement()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: ExplicitTypeEverywhere());
        }

        [Fact]
        public async Task SuggestExplicitTypeOnInterpolatedString()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: ExplicitTypeEverywhere());
        }

        [Fact]
        public async Task SuggestExplicitTypeOnExplicitConversion()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: ExplicitTypeEverywhere());
        }

        [Fact]
        public async Task SuggestExplicitTypeOnConditionalAccessExpression()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: ExplicitTypeEverywhere());
        }

        [Fact]
        public async Task SuggestExplicitTypeInCheckedExpression()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: ExplicitTypeEverywhere());
        }

        [Fact]
        public async Task SuggestExplicitTypeInAwaitExpression()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: ExplicitTypeEverywhere());
        }

        [Fact]
        public async Task SuggestExplicitTypeInBuiltInNumericType()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: ExplicitTypeForBuiltInTypesOnly());
        }

        [Fact]
        public async Task SuggestExplicitTypeInBuiltInCharType()
        {
            await TestInRegularAndScriptAsync(
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
                """, options: ExplicitTypeForBuiltInTypesOnly());
        }

        [Fact]
        public async Task SuggestExplicitTypeInBuiltInType_string()
        {
            // though string isn't an intrinsic type per the compiler
            // we in the IDE treat it as an intrinsic type for this feature.
            await TestInRegularAndScriptAsync(
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
                """, options: ExplicitTypeForBuiltInTypesOnly());
        }

        [Fact]
        public async Task SuggestExplicitTypeInBuiltInType_object()
        {
            // object isn't an intrinsic type per the compiler
            // we in the IDE treat it as an intrinsic type for this feature.
            await TestInRegularAndScriptAsync(
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
                """, options: ExplicitTypeForBuiltInTypesOnly());
        }

        [Fact]
        public async Task SuggestExplicitTypeNotificationLevelSilent()
        {
            var source =
                """
                using System;
                class C
                {
                    static void M()
                    {
                        [|var|] n1 = new C();
                    }
                }
                """;
            await TestDiagnosticInfoAsync(source,
                options: ExplicitTypeSilentEnforcement(),
                diagnosticId: IDEDiagnosticIds.UseExplicitTypeDiagnosticId,
                diagnosticSeverity: DiagnosticSeverity.Hidden);
        }

        [Fact]
        public async Task SuggestExplicitTypeNotificationLevelInfo()
        {
            var source =
                """
                using System;
                class C
                {
                    static void M()
                    {
                        [|var|] s = 5;
                    }
                }
                """;
            await TestDiagnosticInfoAsync(source,
                options: ExplicitTypeEnforcements(),
                diagnosticId: IDEDiagnosticIds.UseExplicitTypeDiagnosticId,
                diagnosticSeverity: DiagnosticSeverity.Info);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23907")]
        public async Task SuggestExplicitTypeNotificationLevelWarning()
        {
            var source =
                """
                using System;
                class C
                {
                    static void M()
                    {
                        [|var|] n1 = new[] { new C() }; // type not apparent and not intrinsic
                    }
                }
                """;
            await TestDiagnosticInfoAsync(source,
                options: ExplicitTypeEnforcements(),
                diagnosticId: IDEDiagnosticIds.UseExplicitTypeDiagnosticId,
                diagnosticSeverity: DiagnosticSeverity.Warning);
        }

        [Fact]
        public async Task SuggestExplicitTypeNotificationLevelError()
        {
            var source =
                """
                using System;
                class C
                {
                    static void M()
                    {
                        [|var|] n1 = new C();
                    }
                }
                """;
            await TestDiagnosticInfoAsync(source,
                options: ExplicitTypeEnforcements(),
                diagnosticId: IDEDiagnosticIds.UseExplicitTypeDiagnosticId,
                diagnosticSeverity: DiagnosticSeverity.Error);
        }

        [Fact]
        public async Task SuggestExplicitTypeOnLocalWithIntrinsicTypeTuple()
        {
            await TestInRegularAndScriptAsync(
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
options: ExplicitTypeEverywhere());
        }

        [Fact]
        public async Task SuggestExplicitTypeOnLocalWithIntrinsicTypeTupleWithNames()
        {
            await TestInRegularAndScriptAsync(
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
options: ExplicitTypeEverywhere());
        }

        [Fact]
        public async Task SuggestExplicitTypeOnLocalWithIntrinsicTypeTupleWithOneName()
        {
            await TestInRegularAndScriptAsync(
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
options: ExplicitTypeEverywhere());
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20437")]
        public async Task SuggestExplicitTypeOnDeclarationExpressionSyntax()
        {
            await TestInRegularAndScriptAsync(
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
options: ExplicitTypeEverywhere());
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20244")]
        public async Task ExplicitTypeOnPredefinedTypesByTheirMetadataNames1()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20244")]
        public async Task ExplicitTypeOnPredefinedTypesByTheirMetadataNames2()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20244")]
        public async Task ExplicitTypeOnPredefinedTypesByTheirMetadataNames3()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20244")]
        public async Task ExplicitTypeOnPredefinedTypesByTheirMetadataNames4()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20244")]
        public async Task ExplicitTypeOnPredefinedTypesByTheirMetadataNames5()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20244")]
        public async Task ExplicitTypeOnPredefinedTypesByTheirMetadataNames6()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20244")]
        public async Task ExplicitTypeOnPredefinedTypesByTheirMetadataNames7()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20244")]
        public async Task ExplicitTypeOnPredefinedTypesByTheirMetadataNames8()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20244")]
        public async Task ExplicitTypeOnPredefinedTypesByTheirMetadataNames9()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20244")]
        public async Task ExplicitTypeOnPredefinedTypesByTheirMetadataNames10()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20244")]
        public async Task ExplicitTypeOnPredefinedTypesByTheirMetadataNames11()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/26923")]
        public async Task NoSuggestionOnForeachCollectionExpression()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task NotOnConstVar()
        {
            // This error case is handled by a separate code fix (UseExplicitTypeForConst).
            await TestMissingInRegularAndScriptAsync(
                """
                class C
                {
                    void M()
                    {
                        const [|var|] v = 0;
                    }
                }
                """, new TestParameters(options: ExplicitTypeEverywhere()));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23907")]
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
                class Program
                {
                    void Method()
                    {
                        System.Func<int, string> x = (int i) => i.ToString();
                    }
                }
                """;
            // The type is not apparent and not intrinsic
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeEverywhere());
            await TestMissingInRegularAndScriptAsync(before, new TestParameters(options: ExplicitTypeForBuiltInTypesOnly()));
            await TestInRegularAndScriptAsync(before, after, options: ExplicitTypeExceptWhereApparent());
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
    }
}
