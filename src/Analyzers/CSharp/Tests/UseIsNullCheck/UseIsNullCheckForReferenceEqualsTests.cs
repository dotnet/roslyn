// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseIsNullCheck;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseIsNullCheck;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseIsNullCheck)]
public sealed partial class UseIsNullCheckForReferenceEqualsTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public UseIsNullCheckForReferenceEqualsTests(ITestOutputHelper logger)
        : base(logger)
    {
    }

    private static readonly ParseOptions CSharp7 = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7);
    private static readonly ParseOptions CSharp8 = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8);
    private static readonly ParseOptions CSharp9 = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9);

    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpUseIsNullCheckForReferenceEqualsDiagnosticAnalyzer(), new CSharpUseIsNullCheckForReferenceEqualsCodeFixProvider());

    [Fact]
    public Task TestIdentifierName()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string s)
                {
                    if ([||]ReferenceEquals(s, null))
                        return;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(string s)
                {
                    if (s is null)
                        return;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58483")]
    public Task TestIsNullTitle()
        => TestExactActionSetOfferedAsync(
            """
            using System;

            class C
            {
                void M(string s)
                {
                    if ([||]ReferenceEquals(s, null))
                        return;
                }
            }
            """,
            [CSharpAnalyzersResources.Use_is_null_check]);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58483")]
    public Task TestIsObjectTitle()
        => TestExactActionSetOfferedAsync(
            """
            using System;

            class C
            {
                void M(string s)
                {
                    if (![||]ReferenceEquals(s, null))
                        return;
                }
            }
            """,
            [CSharpAnalyzersResources.Use_is_object_check],
            new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8)));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58483")]
    public Task TestIsNotNullTitle()
        => TestExactActionSetOfferedAsync(
            """
            using System;

            class C
            {
                void M(string s)
                {
                    if (![||]ReferenceEquals(s, null))
                        return;
                }
            }
            """,
            [CSharpAnalyzersResources.Use_is_not_null_check],
            new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9)));

    [Fact]
    public Task TestBuiltInType()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string s)
                {
                    if (object.[||]ReferenceEquals(s, null))
                        return;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(string s)
                {
                    if (s is null)
                        return;
                }
            }
            """);

    [Fact]
    public Task TestNamedType()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string s)
                {
                    if (Object.[||]ReferenceEquals(s, null))
                        return;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(string s)
                {
                    if (s is null)
                        return;
                }
            }
            """);

    [Fact]
    public Task TestReversed()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string s)
                {
                    if ([||]ReferenceEquals(null, s))
                        return;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(string s)
                {
                    if (s is null)
                        return;
                }
            }
            """);

    [Fact]
    public Task TestNegated_CSharp7()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string s)
                {
                    if (![||]ReferenceEquals(null, s))
                        return;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(string s)
                {
                    if (s is object)
                        return;
                }
            }
            """, new TestParameters(parseOptions: CSharp7));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42368")]
    public Task TestNegated_CSharp9()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string s)
                {
                    if (![||]ReferenceEquals(null, s))
                        return;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(string s)
                {
                    if (s is not null)
                        return;
                }
            }
            """, new TestParameters(parseOptions: CSharp9));

    [Fact]
    public Task TestNotInCSharp6()
        => TestMissingAsync(
            """
            using System;

            class C
            {
                void M(string s)
                {
                    if ([||]ReferenceEquals(null, s))
                        return;
                }
            }
            """, parameters: new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));

    [Fact]
    public Task TestFixAll1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string s1, string s2)
                {
                    if ({|FixAllInDocument:ReferenceEquals|}(s1, null) ||
                        ReferenceEquals(s2, null))
                        return;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(string s1, string s2)
                {
                    if (s1 is null ||
                        s2 is null)
                        return;
                }
            }
            """);

    [Fact]
    public Task TestFixAll2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string s1, string s2)
                {
                    if (ReferenceEquals(s1, null) ||
                        {|FixAllInDocument:ReferenceEquals|}(s2, null))
                        return;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(string s1, string s2)
                {
                    if (s1 is null ||
                        s2 is null)
                        return;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23581")]
    public Task TestValueParameterTypeIsUnconstrainedGeneric_CSharp7()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public static void NotNull<T>(T value)
                {
                    if ([||]ReferenceEquals(value, null))
                    {
                        return;
                    }
                }
            }
            """, """
            class C
            {
                public static void NotNull<T>(T value)
                {
                    if (value == null)
                    {
                        return;
                    }
                }
            }
            """, new TestParameters(parseOptions: CSharp7));

    [Fact, WorkItem(23581, "https://github.com/dotnet/roslyn/issues/47972")]
    public Task TestValueParameterTypeIsUnconstrainedGeneric_CSharp8()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                public static void NotNull<T>(T value)
                {
                    if ({|FixAllInDocument:ReferenceEquals|}(value, null))
                    {
                        return;
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                public static void NotNull<T>(T value)
                {
                    if (value is null)
                    {
                        return;
                    }
                }
            }
            """, new TestParameters(parseOptions: CSharp8));

    [Fact]
    public Task TestValueParameterTypeIsUnconstrainedGenericNegated_CSharp7()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public static void NotNull<T>(T value)
                {
                    if (![||]ReferenceEquals(value, null))
                    {
                        return;
                    }
                }
            }
            """, """
            class C
            {
                public static void NotNull<T>(T value)
                {
                    if (value is object)
                    {
                        return;
                    }
                }
            }
            """, new TestParameters(parseOptions: CSharp7));

    [Fact]
    public Task TestValueParameterTypeIsUnconstrainedGenericNegated_CSharp9()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public static void NotNull<T>(T value)
                {
                    if (![||]ReferenceEquals(value, null))
                    {
                        return;
                    }
                }
            }
            """, """
            class C
            {
                public static void NotNull<T>(T value)
                {
                    if (value is not null)
                    {
                        return;
                    }
                }
            }
            """, new TestParameters(parseOptions: CSharp9));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23581")]
    public Task TestValueParameterTypeIsRefConstraintGeneric()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public static void NotNull<T>(T value) where T:class
                {
                    if ([||]ReferenceEquals(value, null))
                    {
                        return;
                    }
                }
            }
            """,
            """
            class C
            {
                public static void NotNull<T>(T value) where T:class
                {
                    if (value is null)
                    {
                        return;
                    }
                }
            }
            """);

    [Fact]
    public Task TestValueParameterTypeIsRefConstraintGenericNegated_CSharp7()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public static void NotNull<T>(T value) where T:class
                {
                    if (![||]ReferenceEquals(value, null))
                    {
                        return;
                    }
                }
            }
            """,
            """
            class C
            {
                public static void NotNull<T>(T value) where T:class
                {
                    if (value is object)
                    {
                        return;
                    }
                }
            }
            """, new TestParameters(parseOptions: CSharp7));

    [Fact]
    public Task TestValueParameterTypeIsRefConstraintGenericNegated_CSharp9()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                public static void NotNull<T>(T value) where T:class
                {
                    if (![||]ReferenceEquals(value, null))
                    {
                        return;
                    }
                }
            }
            """,
            """
            class C
            {
                public static void NotNull<T>(T value) where T:class
                {
                    if (value is not null)
                    {
                        return;
                    }
                }
            }
            """, new TestParameters(parseOptions: CSharp9));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23581")]
    public Task TestValueParameterTypeIsValueConstraintGeneric()
        => TestMissingAsync(
            """
            class C
            {
                public static void NotNull<T>(T value) where T:struct
                {
                    if ([||]ReferenceEquals(value, null))
                    {
                        return;
                    }
                }
            }
            """);

    [Fact]
    public Task TestValueParameterTypeIsValueConstraintGenericNegated()
        => TestMissingAsync(
            """
            class C
            {
                public static void NotNull<T>(T value) where T:struct
                {
                    if (![||]ReferenceEquals(value, null))
                    {
                        return;
                    }
                }
            }
            """);

    [Fact]
    public Task TestFixAllNested1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string s2)
                {
                    if ({|FixAllInDocument:ReferenceEquals|}(ReferenceEquals(s2, null), null))
                        return;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(string s2)
                {
                    if (ReferenceEquals(s2, null) is null)
                        return;
                }
            }
            """);

    [Fact, WorkItem(23581, "https://github.com/dotnet/roslyn/issues/47972")]
    public Task TestValueParameterTypeIsBaseTypeConstraintGeneric()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                public static void NotNull<T>(T value) where T:C
                {
                    if ({|FixAllInDocument:ReferenceEquals|}(value, null))
                    {
                        return;
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                public static void NotNull<T>(T value) where T:C
                {
                    if (value is null)
                    {
                        return;
                    }
                }
            }
            """, new TestParameters(parseOptions: CSharp7));
}
