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
public partial class SimplifyConditionalTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public SimplifyConditionalTests(ITestOutputHelper logger)
      : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new CSharpSimplifyConditionalDiagnosticAnalyzer(), new SimplifyConditionalCodeFixProvider());

    [Fact]
    public async Task TestSimpleCase()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestSimpleNegatedCase()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestMustBeBool1()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact]
    public async Task TestMustBeBool2()
    {
        await TestMissingAsync(
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
    }

    [Fact]
    public async Task TestWithTrueTrue()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestWithFalseFalse()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestWhenTrueIsTrueAndWhenFalseIsUnknown()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestWhenTrueIsFalseAndWhenFalseIsUnknown()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestWhenTrueIsUnknownAndWhenFalseIsTrue()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact]
    public async Task TestWhenTrueIsUnknownAndWhenFalseIsFalse()
    {
        await TestInRegularAndScript1Async(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/62827")]
    public async Task TestFixAll()
    {
        await TestInRegularAndScriptAsync(
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
}
