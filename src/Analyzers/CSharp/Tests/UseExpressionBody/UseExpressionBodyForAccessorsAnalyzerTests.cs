// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseExpressionBody;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseExpressionBody;

using VerifyCS = CSharpCodeFixVerifier<
    UseExpressionBodyDiagnosticAnalyzer,
    UseExpressionBodyCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseExpressionBody)]
public sealed class UseExpressionBodyForAccessorsTests
{
    private static async Task TestWithUseExpressionBody(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string code,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string fixedCode,
        LanguageVersion version = LanguageVersion.CSharp8)
    {
        var test = new VerifyCS.Test
        {
            ReferenceAssemblies = version == LanguageVersion.CSharp9 ? ReferenceAssemblies.Net.Net50 : ReferenceAssemblies.Default,
            TestCode = code,
            FixedCode = fixedCode,
            LanguageVersion = version,
            Options =
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.WhenPossible  },
                { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, ExpressionBodyPreference.Never },
                { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, ExpressionBodyPreference.Never },
            },
        };

        await test.RunAsync();
    }

    private static Task TestWithUseExpressionBodyIncludingPropertiesAndIndexers(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string code,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string fixedCode,
        LanguageVersion version = LanguageVersion.CSharp8)
        => new VerifyCS.Test
        {
            ReferenceAssemblies = version == LanguageVersion.CSharp9 ? ReferenceAssemblies.Net.Net50 : ReferenceAssemblies.Default,
            TestCode = code,
            FixedCode = fixedCode,
            LanguageVersion = version,
            Options =
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.WhenPossible  },
                { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, ExpressionBodyPreference.WhenPossible },
                { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, ExpressionBodyPreference.WhenPossible },
            }
        }.RunAsync();

    private static Task TestWithUseBlockBodyIncludingPropertiesAndIndexers(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string code,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string fixedCode,
        LanguageVersion version = LanguageVersion.CSharp8)
        => new VerifyCS.Test
        {
            ReferenceAssemblies = version == LanguageVersion.CSharp9 ? ReferenceAssemblies.Net.Net50 : ReferenceAssemblies.Default,
            TestCode = code,
            FixedCode = fixedCode,
            LanguageVersion = version,
            Options =
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.Never  },
                { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, ExpressionBodyPreference.Never },
                { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, ExpressionBodyPreference.Never },
            }
        }.RunAsync();

    [Fact]
    public Task TestUseExpressionBody1()
        => TestWithUseExpressionBody("""
            class C
            {
                int Bar() { return 0; }

                int Goo
                {
                    {|IDE0027:get
                    {
                        return Bar();
                    }|}
                }
            }
            """, """
            class C
            {
                int Bar() { return 0; }

                int Goo
                {
                    get => Bar();
                }
            }
            """);

    [Fact]
    public Task TestUpdatePropertyInsteadOfAccessor()
        => TestWithUseExpressionBodyIncludingPropertiesAndIndexers("""
            class C
            {
                int Bar() { return 0; }

                {|IDE0025:int Goo
                {
                    get
                    {
                        return Bar();
                    }
                }|}
            }
            """, """
            class C
            {
                int Bar() { return 0; }

                int Goo => Bar();
            }
            """);

    [Fact]
    public Task TestOnIndexer1()
        => TestWithUseExpressionBody("""
            class C
            {
                int Bar() { return 0; }

                int this[int i]
                {
                    {|IDE0027:get
                    {
                        return Bar();
                    }|}
                }
            }
            """, """
            class C
            {
                int Bar() { return 0; }

                int this[int i]
                {
                    get => Bar();
                }
            }
            """);

    [Fact]
    public Task TestUpdateIndexerIfIndexerAndAccessorCanBeUpdated()
        => TestWithUseExpressionBodyIncludingPropertiesAndIndexers("""
            class C
            {
                int Bar() { return 0; }

                {|IDE0026:int this[int i]
                {
                    get
                    {
                        return Bar();
                    }
                }|}
            }
            """, """
            class C
            {
                int Bar() { return 0; }

                int this[int i] => Bar();
            }
            """);

    [Fact]
    public Task TestOnSetter1()
        => TestWithUseExpressionBody("""
            class C
            {
                void Bar() { }

                int Goo
                {
                    {|IDE0027:set
                    {
                        Bar();
                    }|}
                }
            }
            """, """
            class C
            {
                void Bar() { }

                int Goo
                {
                    set => Bar();
                }
            }
            """);

    [Fact]
    public Task TestOnInit1()
        => TestWithUseExpressionBody("""
            class C
            {
                int Goo
                {
                    {|IDE0027:init
                    {
                        Bar();
                    }|}
                }

                int Bar() { return 0; }
            }
            """, """
            class C
            {
                int Goo
                {
                    init => Bar();
                }

                int Bar() { return 0; }
            }
            """, LanguageVersion.CSharp9);

    [Fact]
    public Task TestMissingWithOnlySetter()
        => VerifyCS.VerifyAnalyzerAsync("""
            class C
            {
                void Bar() { }

                int Goo
                {
                    set => Bar();
                }
            }
            """);

    [Fact]
    public Task TestMissingWithOnlyInit()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            TestCode = """
            class C
            {
                int Goo
                {
                    init => Bar();
                }

                int Bar() { return 0; }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
        }.RunAsync();

    [Fact]
    public Task TestUseExpressionBody3()
        => TestWithUseExpressionBody("""
            using System;

            class C
            {
                int Goo
                {
                    {|IDE0027:get
                    {
                        throw new NotImplementedException();
                    }|}
                }
            }
            """, """
            using System;

            class C
            {
                int Goo
                {
                    get => throw new NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestUseExpressionBody4()
        => TestWithUseExpressionBody("""
            using System;

            class C
            {
                int Goo
                {
                    {|IDE0027:get
                    {
                        throw new NotImplementedException(); // comment
                    }|}
                }
            }
            """, """
            using System;

            class C
            {
                int Goo
                {
                    get => throw new NotImplementedException(); // comment
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/59255")]
    public Task TestUseExpressionBody5()
        => TestWithUseExpressionBody("""
            using System;

            class C
            {
                event EventHandler Goo
                {
                    {|IDE0027:add
                    {
                        throw new NotImplementedException();
                    }|}

                    {|IDE0027:remove
                    {
                        throw new NotImplementedException();
                    }|}
                }
            }
            """, """
            using System;

            class C
            {
                event EventHandler Goo
                {
                    add => throw new NotImplementedException();

                    remove => throw new NotImplementedException();
                }
            }
            """);

    [Fact]
    public Task TestUseBlockBody1()
        => TestWithUseBlockBodyIncludingPropertiesAndIndexers("""
            class C
            {
                int Bar() { return 0; }

                int Goo
                {
                    {|IDE0027:get => Bar();|}
                }
            }
            """, """
            class C
            {
                int Bar() { return 0; }

                int Goo
                {
                    get
                    {
                        return Bar();
                    }
                }
            }
            """);

    [Fact]
    public Task TestUseBlockBodyForSetter1()
        => TestWithUseBlockBodyIncludingPropertiesAndIndexers("""
            class C
            {
                void Bar() { }

                int Goo
                {
                    {|IDE0027:set => Bar();|}
                    }
                }
            """, """
            class C
            {
                void Bar() { }

                int Goo
                {
                    set
                    {
                        Bar();
                    }
                }
            }
            """);

    [Fact]
    public Task TestUseBlockBodyForInit1()
        => TestWithUseBlockBodyIncludingPropertiesAndIndexers("""
            class C
            {
                int Goo
                {
                    {|IDE0027:init => Bar();|}
                    }

                int Bar() { return 0; }
                }
            """, """
            class C
            {
                int Goo
                {
                    init
                    {
                        Bar();
                    }
                }

                int Bar() { return 0; }
                }
            """, LanguageVersion.CSharp9);

    [Fact]
    public Task TestUseBlockBody3()
        => TestWithUseBlockBodyIncludingPropertiesAndIndexers("""
            using System;

            class C
            {
                int Goo
                {
                    {|IDE0027:get => throw new NotImplementedException();|}
                    }
                }
            """, """
            using System;

            class C
            {
                int Goo
                {
                    get
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            """);

    [Fact]
    public Task TestUseBlockBody4()
        => TestWithUseBlockBodyIncludingPropertiesAndIndexers("""
            using System;

            class C
            {
                int Goo
                {
                    {|IDE0027:get => throw new NotImplementedException();|} // comment
                }
            }
            """, """
            using System;

            class C
            {
                int Goo
                {
                    get
                    {
                        throw new NotImplementedException(); // comment
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31308")]
    public Task TestUseBlockBody5()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                C this[int index]
                {
                    get => default;
                }
            }
            """,
            Options =
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.WhenOnSingleLine, NotificationOption2.None },
                { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, ExpressionBodyPreference.WhenOnSingleLine, NotificationOption2.None },
                { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, ExpressionBodyPreference.WhenOnSingleLine, NotificationOption2.None },
            }
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/59255")]
    public Task TestUseBlockBody6()
        => TestWithUseBlockBodyIncludingPropertiesAndIndexers("""
            using System;

            class C
            {
                event EventHandler Goo
                {
                    {|IDE0027:add => throw new NotImplementedException();|}
                    {|IDE0027:remove => throw new NotImplementedException();|}
                    }
                }
            """, """
            using System;

            class C
            {
                event EventHandler Goo
                {
                    add
                    {
                        throw new NotImplementedException();
                    }

                    remove
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20350")]
    public Task TestAccessorListFormatting()
        => TestWithUseBlockBodyIncludingPropertiesAndIndexers("""
            class C
            {
                int Bar() { return 0; }

                int Goo { {|IDE0027:get => Bar();|} }
            }
            """, """
            class C
            {
                int Bar() { return 0; }

                int Goo
                {
                    get
                    {
                        return Bar();
                    }
                }
            }
            """);

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/20350")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/61279")]
    public async Task TestAccessorListFormatting_FixAll1()
    {
        var fixedCode = """
            class C
            {
                int Bar() { return 0; }

                int Goo
                {
                    get
                    {
                        return Bar();
                    }

                    set
                    {
                        Bar();
                    }
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                int Bar() { return 0; }

                int Goo { {|IDE0027:get => Bar();|} {|IDE0027:set => Bar();|} }
            }
            """,
            FixedCode = fixedCode,
            BatchFixedCode = fixedCode,
            Options =
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.Never  },
                { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, ExpressionBodyPreference.Never },
                { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, ExpressionBodyPreference.Never },
            },
        }.RunAsync();
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/20350")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/61279")]
    public Task TestAccessorListFormatting_FixAll2()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                int Bar() { return 0; }

                int Goo { {|IDE0027:get => Bar();|} {|IDE0027:set => Bar();|} }
            }
            """,
            FixedCode = """
            class C
            {
                int Bar() { return 0; }

                int Goo
                {
                    get
                    {
                        return Bar();
                    }
                    set => Bar();
                }
            }
            """,
            BatchFixedCode = """
            class C
            {
                int Bar() { return 0; }

                int Goo
                {
                    get
                    {
                        return Bar();
                    }

                    set
                    {
                        Bar();
                    }
                }
            }
            """,
            DiagnosticSelector = diagnostics => diagnostics[0],
            CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
            Options =
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.Never  },
                { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, ExpressionBodyPreference.Never },
                { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, ExpressionBodyPreference.Never },
            },
            FixedState =
            {
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(7,9): hidden IDE0027: Use block body for accessor
                    VerifyCS.Diagnostic("IDE0027").WithMessage(CSharpAnalyzersResources.Use_block_body_for_accessor).WithSpan(11, 9, 11, 22).WithOptions(DiagnosticOptions.IgnoreAdditionalLocations),
                }
            },
        }.RunAsync();

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/20350")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/61279")]
    public Task TestAccessorListFormatting_FixAll3()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                int Bar() { return 0; }

                int Goo { {|IDE0027:get => Bar();|} {|IDE0027:set => Bar();|} }
            }
            """,
            FixedCode = """
            class C
            {
                int Bar() { return 0; }

                int Goo
                {
                    get => Bar();
                    set
                    {
                        Bar();
                    }
                }
            }
            """,
            BatchFixedCode = """
            class C
            {
                int Bar() { return 0; }

                int Goo
                {
                    get
                    {
                        return Bar();
                    }

                    set
                    {
                        Bar();
                    }
                }
            }
            """,
            DiagnosticSelector = diagnostics => diagnostics[1],
            CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
            Options =
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.Never  },
                { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, ExpressionBodyPreference.Never },
                { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, ExpressionBodyPreference.Never },
            },
            FixedState =
            {
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(7,9): hidden IDE0027: Use block body for accessor
                    VerifyCS.Diagnostic("IDE0027").WithMessage(CSharpAnalyzersResources.Use_block_body_for_accessor).WithSpan(7, 9, 7, 22).WithOptions(DiagnosticOptions.IgnoreAdditionalLocations),
                }
            },
        }.RunAsync();

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/20350")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/61279")]
    public async Task TestAccessorListFormatting_FixAll4()
    {
        var fixedCode =
            """
            class C
            {
                int Goo
                {
                    get
                    {
                        return Bar();
                    }

                    init
                    {
                        Bar();
                    }
                }

                int Bar() { return 0; }
            }
            """;

        await new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            TestCode = """
            class C
            {
                int Goo { {|IDE0027:get => Bar();|} {|IDE0027:init => Bar();|} }

                int Bar() { return 0; }
            }
            """,
            FixedCode = fixedCode,
            BatchFixedCode = fixedCode,
            LanguageVersion = LanguageVersion.CSharp9,
            Options =
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, ExpressionBodyPreference.Never },
                { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, ExpressionBodyPreference.Never },
                { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, ExpressionBodyPreference.Never },
            }
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20362")]
    public Task TestOfferToConvertToBlockEvenIfExpressionBodyPreferredIfPriorToCSharp7()
        => TestWithUseExpressionBody("""
            using System;
            class C
            {
                int Goo { {|IDE0027:get {|CS8059:=>|} {|CS8059:throw|} new NotImplementedException();|} }
            }
            """, """
            using System;
            class C
            {
                int Goo
                {
                    get
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            """, LanguageVersion.CSharp6);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20362")]
    public Task TestOfferToConvertToBlockEvenIfExpressionBodyPreferredIfPriorToCSharp7_FixAll()
        => TestWithUseExpressionBody("""
            using System;
            class C
            {
                int Goo { {|IDE0027:get {|CS8059:=>|} {|CS8059:throw|} new NotImplementedException();|} }
                int Bar { {|IDE0027:get {|CS8059:=>|} {|CS8059:throw|} new NotImplementedException();|} }
            }
            """, """
            using System;
            class C
            {
                int Goo
                {
                    get
                    {
                        throw new NotImplementedException();
                    }
                }
                int Bar
                {
                    get
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            """, LanguageVersion.CSharp6);
}
