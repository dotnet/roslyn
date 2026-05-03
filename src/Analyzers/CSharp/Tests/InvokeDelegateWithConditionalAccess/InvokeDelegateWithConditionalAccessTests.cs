// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.InvokeDelegateWithConditionalAccess;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.InvokeDelegateWithConditionalAccess;

[Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
public sealed partial class InvokeDelegateWithConditionalAccessTests(ITestOutputHelper logger)
    : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor(logger)
{
    internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (new InvokeDelegateWithConditionalAccessAnalyzer(), new InvokeDelegateWithConditionalAccessCodeFixProvider());

    [Fact]
    public Task Test1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    [||]var v = a;
                    if (v != null)
                    {
                        v();
                    }
                }
            }
            """,
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    a?.Invoke();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76423")]
    public Task Test1_TopLevel()
        => TestInRegularAndScriptAsync(
            """
            var v = () => {};
            [||]if (v != null)
            {
                v();
            }
            """,
            """
            var v = () => {};
            v?.Invoke();
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76423")]
    public Task Test2_TopLevel()
        => TestAsync(
            """
            Action a = null;
            [||]var v = a;
            if (v != null)
            {
                v();
            }
            """,
            """
            Action a = null;

            a?.Invoke();
            """, new(parseOptions: CSharpParseOptions.Default));

    [Fact]
    public Task TestOnIf()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    var v = a;
                    [||]if (v != null)
                    {
                        v();
                    }
                }
            }
            """,
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    a?.Invoke();
                }
            }
            """);

    [Fact]
    public Task TestOnInvoke()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    var v = a;
                    if (v != null)
                    {
                        [||]v();
                    }
                }
            }
            """,
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    a?.Invoke();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13226")]
    public Task TestMissingBeforeCSharp6()
        => TestMissingAsync(
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    [||]var v = a;
                    if (v != null)
                    {
                        v();
                    }
                }
            }
            """, new TestParameters(CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5)));

    [Fact]
    public Task TestInvertedIf()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    [||]var v = a;
                    if (null != v)
                    {
                        v();
                    }
                }
            }
            """,
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    a?.Invoke();
                }
            }
            """);

    [Fact]
    public Task TestIfWithNoBraces()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    [||]var v = a;
                    if (null != v)
                        v();
                }
            }
            """,
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    a?.Invoke();
                }
            }
            """);

    [Fact]
    public Task TestWithComplexExpression()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    bool b = true;
                    [||]var v = b ? a : null;
                    if (v != null)
                    {
                        v();
                    }
                }
            }
            """,
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    bool b = true;
                    (b ? a : null)?.Invoke();
                }
            }
            """);

    [Fact]
    public Task TestMissingWithElseClause()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    [||]var v = a;
                    if (v != null)
                    {
                        v();
                    }
                    else
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task TestMissingOnDeclarationWithMultipleVariables()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    [||]var v = a, x = a;
                    if (v != null)
                    {
                        v();
                    }
                }
            }
            """);

    /// <remarks>
    /// With multiple variables in the same declaration, the fix _is not_ offered on the declaration
    /// itself, but _is_ offered on the invocation pattern.
    /// </remarks>
    [Fact]
    public Task TestLocationWhereOfferedWithMultipleVariables()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    var v = a, x = a;
                    [||]if (v != null)
                    {
                        v();
                    }
                }
            }
            """,
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    var v = a, x = a;
                    v?.Invoke();
                }
            }
            """);

    /// <remarks>
    /// If we have a variable declaration and if it is read/written outside the delegate 
    /// invocation pattern, the fix is not offered on the declaration.
    /// </remarks>
    [Fact]
    public Task TestMissingOnDeclarationIfUsedOutside()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    [||]var v = a;
                    if (v != null)
                    {
                        v();
                    }

                    v = null;
                }
            }
            """);

    /// <remarks>
    /// If we have a variable declaration and if it is read/written outside the delegate 
    /// invocation pattern, the fix is not offered on the declaration but is offered on
    /// the invocation pattern itself.
    /// </remarks>
    [Fact]
    public Task TestLocationWhereOfferedIfUsedOutside()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    var v = a;
                    [||]if (v != null)
                    {
                        v();
                    }

                    v = null;
                }
            }
            """,
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    var v = a;
                    v?.Invoke();

                    v = null;
                }
            }
            """);

    [Fact]
    public Task TestSimpleForm1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                public event EventHandler E;

                void M()
                {
                    [||]if (this.E != null)
                    {
                        this.E(this, EventArgs.Empty);
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                public event EventHandler E;

                void M()
                {
                    this.E?.Invoke(this, EventArgs.Empty);
                }
            }
            """);

    [Fact]
    public Task TestSimpleForm2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                public event EventHandler E;

                void M()
                {
                    if (this.E != null)
                    {
                        [||]this.E(this, EventArgs.Empty);
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                public event EventHandler E;

                void M()
                {
                    this.E?.Invoke(this, EventArgs.Empty);
                }
            }
            """);

    [Fact]
    public Task TestInElseClause1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                public event EventHandler E;

                void M()
                {
                    if (true != true)
                    {
                    }
                    else [||]if (this.E != null)
                    {
                        this.E(this, EventArgs.Empty);
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                public event EventHandler E;

                void M()
                {
                    if (true != true)
                    {
                    }
                    else
                    {
                        this.E?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
            """);

    [Fact]
    public Task TestInElseClause2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                public event EventHandler E;

                void M()
                {
                    if (true != true)
                    {
                    }
                    else [||]if (this.E != null)
                        this.E(this, EventArgs.Empty);
                }
            }
            """,
            """
            using System;

            class C
            {
                public event EventHandler E;

                void M()
                {
                    if (true != true)
                    {
                    }
                    else this.E?.Invoke(this, EventArgs.Empty);
                }
            }
            """);

    [Fact]
    public Task TestTrivia1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                System.Action a;
                void Goo()
                {
                    // Comment
                    [||]var v = a;
                    if (v != null)
                    {
                        v(); // Comment2
                    }
                }
            }
            """,
            """
            class C
            {
                System.Action a;
                void Goo()
                {
                    // Comment
                    a?.Invoke(); // Comment2
                }
            }
            """);

    [Fact]
    public Task TestTrivia2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                System.Action a;
                void Goo()
                {
                    // Comment
                    [||]if (a != null)
                    {
                        a(); // Comment2
                    }
                }
            }
            """,
            """
            class C
            {
                System.Action a;
                void Goo()
                {
                    // Comment
                    a?.Invoke(); // Comment2
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51563")]
    public Task TestTrivia3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                System.Action a;
                void Goo()
                {
                    // Comment
                    [||]var v = a;
                    if (v != null) { v(); /* 123 */ } // trails
                    System.Console.WriteLine();
                }
            }
            """,
            """
            class C
            {
                System.Action a;
                void Goo()
                {
                    // Comment
                    a?.Invoke(); /* 123 */  // trails
                    System.Console.WriteLine();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51563")]
    public Task TestTrivia4()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                System.Action a;
                void Goo()
                {
                    [||]if (a != null) { a(); /* 123 */ } // trails
                    System.Console.WriteLine();
                }
            }
            """,
            """
            class C
            {
                System.Action a;
                void Goo()
                {
                    a?.Invoke(); /* 123 */  // trails
                    System.Console.WriteLine();
                }
            }
            """);

    /// <remarks>
    /// tests locations where the fix is offered.
    /// </remarks>
    [Fact]
    public Task TestFixOfferedOnIf()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    var v = a;
                    [||]if (v != null)
                    {
                        v();
                    }
                }
            }
            """,
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    a?.Invoke();
                }
            }
            """);

    /// <remarks>
    /// tests locations where the fix is offered.
    /// </remarks>
    [Fact]
    public Task TestFixOfferedInsideIf()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    var v = a;
                    if (v != null)
                    {
                        [||]v();
                    }
                }
            }
            """,
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    a?.Invoke();
                }
            }
            """);

    [Fact]
    public Task TestMissingOnConditionalInvocation()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    [||]var v = a;
                    v?.Invoke();
                }
            }
            """);

    [Fact]
    public Task TestMissingOnConditionalInvocation2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    var v = a;
                    [||]v?.Invoke();
                }
            }
            """);

    [Fact]
    public Task TestMissingOnConditionalInvocation3()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    [||]a?.Invoke();
                }
            }
            """);

    [Fact]
    public Task TestMissingOnNonNullCheckExpressions()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    var v = a;
                    if (v == a)
                    {
                        [||]v();
                    }
                }
            }
            """);

    [Fact]
    public Task TestMissingOnNonNullCheckExpressions2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    var v = a;
                    if (v == null)
                    {
                        [||]v();
                    }
                }
            }
            """);

    /// <remarks>
    /// if local declaration is not immediately preceding the invocation pattern, 
    /// the fix is not offered on the declaration.
    /// </remarks>
    [Fact]
    public Task TestLocalNotImmediatelyPrecedingNullCheckAndInvokePattern()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    [||]var v = a;
                    int x;
                    if (v != null)
                    {
                        v();
                    }
                }
            }
            """);

    /// <remarks>
    /// if local declaration is not immediately preceding the invocation pattern, 
    /// the fix is not offered on the declaration but is offered on the invocation pattern itself.
    /// </remarks>
    [Fact]
    public Task TestLocalDNotImmediatelyPrecedingNullCheckAndInvokePattern2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    var v = a;
                    int x;
                    [||]if (v != null)
                    {
                        v();
                    }
                }
            }
            """,
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    var v = a;
                    int x;
                    v?.Invoke();
                }
            }
            """);

    [Fact]
    public Task TestMissingOnFunc()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                System.Func<int> a;

                int Goo()
                {
                    var v = a;
                    [||]if (v != null)
                    {
                        return v();
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13226")]
    public Task TestWithLambdaInitializer()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Action v = () => {};
                    [||]if (v != null)
                    {
                        v();
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Action v = () => {};
                    v?.Invoke();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13226")]
    public Task TestWithLambdaInitializer2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Action v = (() => {});
                    [||]if (v != null)
                    {
                        v();
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Action v = (() => {});
                    v?.Invoke();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13226")]
    public Task TestForWithAnonymousMethod()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Action v = delegate {};
                    [||]if (v != null)
                    {
                        v();
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Action v = delegate {};
                    v?.Invoke();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13226")]
    public Task TestWithMethodReference()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void Goo()
                {
                    Action v = Console.WriteLine;
                    [||]if (v != null)
                    {
                        v();
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                void Goo()
                {
                    Action v = Console.WriteLine;
                    v?.Invoke();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31827")]
    public Task TestWithExplicitInvokeCall1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    [||]if (Event != null)
                        Event.Invoke(this, EventArgs.Empty);
                }

                event EventHandler Event;
            }
            """,
            """
            using System;

            class C
            {
                void M()
                {
                    Event?.Invoke(this, EventArgs.Empty);
                }

                event EventHandler Event;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31827")]
    public Task TestWithExplicitInvokeCall2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    [||]var v = a;
                    if (v != null)
                    {
                        v.Invoke();
                    }
                }
            }
            """,
            """
            class C
            {
                System.Action a;

                void Goo()
                {
                    a?.Invoke();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76423")]
    public Task TestWithExplicitInvokeCall2_TopLevel()
        => TestInRegularAndScriptAsync(
            """
            var v = () => {};
            [||]if (v != null)
            {
                v.Invoke();
            }
            """,
            """
            var v = () => {};
            v?.Invoke();
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50976")]
    public Task TestMissingOnFunctionPointer()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                unsafe void M(delegate* managed<void> func)
                {
                    if (func != null)
                    {
                        [||]func();
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76422")]
    public Task TestInvokeMethodOnNonDelegate()
        => TestMissingAsync(
            """
            class C
            {
                void M()
                {
                    [||]var v = new C();
                    if (v != null)
                    {
                        v.Invoke();
                    }
                }
            }
                        
            class C
            {
                public void Invoke() { }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76422")]
    public Task TestInvokeMethodOnNonDelegate_TopLevel()
        => TestMissingAsync(
            """
            var v = new C();
            [||]if (v != null)
            {
                v.Invoke();
            }
                        
            class C
            {
                public void Invoke() { }
            }
            """);
}
