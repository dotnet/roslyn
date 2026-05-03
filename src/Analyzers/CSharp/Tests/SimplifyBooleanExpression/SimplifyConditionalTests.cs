// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.SimplifyBooleanExpression;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.SimplifyBooleanExpression;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SimplifyBooleanExpression;

[Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyConditional)]
public sealed partial class SimplifyConditionalTests(ITestOutputHelper logger)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpSimplifyConditionalDiagnosticAnalyzer(), new SimplifyConditionalCodeFixProvider());

    [Fact]
    public Task TestSimpleCase()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                bool M()
                {
                    return [|X() && Y() ? true : false|];
                }

                private bool X() => throw new NotImplementedException();
                private bool Y() => throw new NotImplementedException();
            }
            """,
            """
            using System;

            class C
            {
                bool M()
                {
                    return X() && Y();
                }

                private bool X() => throw new NotImplementedException();
                private bool Y() => throw new NotImplementedException();
            }
            """);

    [Fact]
    public Task TestSimpleNegatedCase()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                bool M()
                {
                    return [|X() && Y() ? false : true|];
                }

                private bool X() => throw new NotImplementedException();
                private bool Y() => throw new NotImplementedException();
            }
            """,
            """
            using System;

            class C
            {
                bool M()
                {
                    return !X() || !Y();
                }

                private bool X() => throw new NotImplementedException();
                private bool Y() => throw new NotImplementedException();
            }
            """);

    [Fact]
    public Task TestMustBeBool1()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                string M()
                {
                    return [|X() && Y() ? "" : null|];
                }

                private bool X() => throw new NotImplementedException();
                private bool Y() => throw new NotImplementedException();
            }
            """);

    [Fact]
    public Task TestMustBeBool2()
        => TestMissingAsync(
            """
            using System;

            class C
            {
                string M()
                {
                    return [|X() && Y() ? null : ""|];
                }

                private bool X() => throw new NotImplementedException();
                private bool Y() => throw new NotImplementedException();
            }
            """);

    [Fact]
    public Task TestWithTrueTrue()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                bool M()
                {
                    return [|X() && Y() ? true : true|];
                }

                private bool X() => throw new NotImplementedException();
                private bool Y() => throw new NotImplementedException();
            }
            """,
            """
            using System;

            class C
            {
                bool M()
                {
                    return X() && Y() || true;
                }

                private bool X() => throw new NotImplementedException();
                private bool Y() => throw new NotImplementedException();
            }
            """);

    [Fact]
    public Task TestWithFalseFalse()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                bool M()
                {
                    return [|X() && Y() ? false : false|];
                }

                private bool X() => throw new NotImplementedException();
                private bool Y() => throw new NotImplementedException();
            }
            """,
            """
            using System;

            class C
            {
                bool M()
                {
                    return X() && Y() && false;
                }

                private bool X() => throw new NotImplementedException();
                private bool Y() => throw new NotImplementedException();
            }
            """);

    [Fact]
    public Task TestWhenTrueIsTrueAndWhenFalseIsUnknown()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                string M()
                {
                    return [|X() ? true : Y()|];
                }

                private bool X() => throw new NotImplementedException();
                private bool Y() => throw new NotImplementedException();
            }
            """,
            """
            using System;

            class C
            {
                string M()
                {
                    return X() || Y();
                }

                private bool X() => throw new NotImplementedException();
                private bool Y() => throw new NotImplementedException();
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71418")]
    public Task TestWhenTrueIsTrueAndWhenFalseIsUnknown_A()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                string M()
                {
                    return [|X()
                        ? true
                        : Y()|];
                }

                private bool X() => throw new NotImplementedException();
                private bool Y() => throw new NotImplementedException();
            }
            """,
            """
            using System;

            class C
            {
                string M()
                {
                    return X() || Y();
                }

                private bool X() => throw new NotImplementedException();
                private bool Y() => throw new NotImplementedException();
            }
            """);

    [Fact]
    public Task TestWhenTrueIsFalseAndWhenFalseIsUnknown()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                string M()
                {
                    return [|X() ? false : Y()|];
                }

                private bool X() => throw new NotImplementedException();
                private bool Y() => throw new NotImplementedException();
            }
            """,
            """
            using System;

            class C
            {
                string M()
                {
                    return !X() && Y();
                }

                private bool X() => throw new NotImplementedException();
                private bool Y() => throw new NotImplementedException();
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71418")]
    public Task TestWhenTrueIsFalseAndWhenFalseIsUnknown_A()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                string M()
                {
                    return [|X()
                        ? false
                        : Y()|];
                }

                private bool X() => throw new NotImplementedException();
                private bool Y() => throw new NotImplementedException();
            }
            """,
            """
            using System;

            class C
            {
                string M()
                {
                    return !X() && Y();
                }

                private bool X() => throw new NotImplementedException();
                private bool Y() => throw new NotImplementedException();
            }
            """);

    [Fact]
    public Task TestWhenTrueIsUnknownAndWhenFalseIsTrue()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                string M()
                {
                    return [|X() ? Y() : true|];
                }

                private bool X() => throw new NotImplementedException();
                private bool Y() => throw new NotImplementedException();
            }
            """,
            """
            using System;

            class C
            {
                string M()
                {
                    return !X() || Y();
                }

                private bool X() => throw new NotImplementedException();
                private bool Y() => throw new NotImplementedException();
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71418")]
    public Task TestWhenTrueIsUnknownAndWhenFalseIsTrue_A()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                string M()
                {
                    return [|X()
                        ? Y()
                        : true|];
                }

                private bool X() => throw new NotImplementedException();
                private bool Y() => throw new NotImplementedException();
            }
            """,
            """
            using System;

            class C
            {
                string M()
                {
                    return !X() || Y();
                }

                private bool X() => throw new NotImplementedException();
                private bool Y() => throw new NotImplementedException();
            }
            """);

    [Fact]
    public Task TestWhenTrueIsUnknownAndWhenFalseIsFalse()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                string M()
                {
                    return [|X() ? Y() : false|];
                }

                private bool X() => throw new NotImplementedException();
                private bool Y() => throw new NotImplementedException();
            }
            """,
            """
            using System;

            class C
            {
                string M()
                {
                    return X() && Y();
                }

                private bool X() => throw new NotImplementedException();
                private bool Y() => throw new NotImplementedException();
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71418")]
    public Task TestWhenTrueIsUnknownAndWhenFalseIsFalse_A()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                string M()
                {
                    return [|X()
                        ? Y()
                        : false|];
                }

                private bool X() => throw new NotImplementedException();
                private bool Y() => throw new NotImplementedException();
            }
            """,
            """
            using System;

            class C
            {
                string M()
                {
                    return X() && Y();
                }

                private bool X() => throw new NotImplementedException();
                private bool Y() => throw new NotImplementedException();
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/62827")]
    public Task TestFixAll()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                public bool M(object x, object y, Func<object, object, bool> isEqual)
                {
                    return {|FixAllInDocument:x == null ? false : y == null ? false : isEqual == null ? x.Equals(y) : isEqual(x, y)|};
                }
            }
            """,
            """
            using System;

            class C
            {
                public bool M(object x, object y, Func<object, object, bool> isEqual)
                {
                    return x != null && y != null && (isEqual == null ? x.Equals(y) : isEqual(x, y));
                }
            }
            """);
}
