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
public sealed partial class UseIsNullCheckForCastAndEqualityOperatorTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public UseIsNullCheckForCastAndEqualityOperatorTests(ITestOutputHelper logger)
      : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpUseIsNullCheckForCastAndEqualityOperatorDiagnosticAnalyzer(), new CSharpUseIsNullCheckForCastAndEqualityOperatorCodeFixProvider());

    [Fact]
    public Task TestEquality()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string s)
                {
                    if ([||](object)s == null)
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
                    if ([||](object)s == null)
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
                    if ([||](object)s != null)
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
                    if ([||](object)s != null)
                        return;
                }
            }
            """,
            [CSharpAnalyzersResources.Use_is_not_null_check],
            new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9)));

    [Fact]
    public Task TestEqualitySwapped()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string s)
                {
                    if ([||]null == (object)s)
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
    public Task TestNotEquality_CSharp8()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string s)
                {
                    if ([||](object)s != null)
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
            """, new(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8)));

    [Fact]
    public Task TestNotEquality_CSharp9()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string s)
                {
                    if ([||](object)s != null)
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
            """, new(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9)));

    [Fact]
    public Task TestNotEqualitySwapped_CSharp8()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string s)
                {
                    if ([||]null != (object)s)
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
            """, new(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8)));

    [Fact]
    public Task TestNotEqualitySwapped_CSharp9()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string s)
                {
                    if ([||]null != (object)s)
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
            """, new(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp9)));

    [Fact]
    public Task TestMissingPreCSharp7()
        => TestMissingAsync(
            """
            using System;

            class C
            {
                void M(string s)
                {
                    if ([||](object)s == null)
                        return;
                }
            }
            """, parameters: new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));

    [Fact]
    public Task TestOnlyForObjectCast()
        => TestMissingAsync(
            """
            using System;

            class C
            {
                void M(string s)
                {
                    if ([||](string)s == null)
                        return;
                }
            }
            """);

    [Fact]
    public Task TestFixAll1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string s)
                {
                    if ({|FixAllInDocument:|}(object)s == null)
                        return;

                    if (null == (object)s)
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

                    if (s is null)
                        return;
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
                    if ({|FixAllInDocument:|}(object)((object)s2 == null) == null))
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
                    if ((s2 is null) is null))
                        return;
                }
            }
            """);

    [Fact]
    public Task TestTrivia1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string s)
                {
                    if ( /*1*/ [||]( /*2*/ object /*3*/ ) /*4*/ s /*5*/ == /*6*/ null /*7*/ )
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
                    if ( /*1*/ s /*5*/ is /*6*/ null /*7*/ )
                        return;
                }
            }
            """);

    [Fact]
    public Task TestTrivia2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string s)
                {
                    if ( /*1*/ [||]null /*2*/ == /*3*/ ( /*4*/ object /*5*/ ) /*6*/ s /*7*/ )
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
                    if ( /*1*/ s /*2*/ is /*3*/ null /*7*/ )
                        return;
                }
            }
            """);

    [Fact]
    public Task TestConstrainedTypeParameter()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M<T>(T s) where T : class
                {
                    if ([||](object)s == null)
                        return;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M<T>(T s) where T : class
                {
                    if (s is null)
                        return;
                }
            }
            """);

    [Fact]
    public Task TestUnconstrainedTypeParameter()
        => TestMissingAsync(
            """
            using System;

            class C
            {
                void M<T>(T s)
                {
                    if ([||](object)s == null)
                        return;
                }
            }
            """);

    [Fact]
    public Task TestComplexExpr()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(string[] s)
                {
                    if ([||](object)s[0] == null)
                        return;
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(string[] s)
                {
                    if (s[0] is null)
                        return;
                }
            }
            """);

    [Fact]
    public Task TestNotOnDefault()
        => TestMissingAsync(
            """
            using System;

            class C
            {
                void M(string[] s)
                {
                    if ([||](object)s[0] == default)
                        return;
                }
            }
            """);
}
