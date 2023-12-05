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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.InvokeDelegateWithConditionalAccess
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
    public partial class InvokeDelegateWithConditionalAccessTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        public InvokeDelegateWithConditionalAccessTests(ITestOutputHelper logger)
           : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new InvokeDelegateWithConditionalAccessAnalyzer(), new InvokeDelegateWithConditionalAccessCodeFixProvider());

        [Fact]
        public async Task Test1()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestOnIf()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestOnInvoke()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13226")]
        public async Task TestMissingBeforeCSharp6()
        {
            await TestMissingAsync(
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
        }

        [Fact]
        public async Task TestInvertedIf()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestIfWithNoBraces()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestWithComplexExpression()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestMissingWithElseClause()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMissingOnDeclarationWithMultipleVariables()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        /// <remarks>
        /// With multiple variables in the same declaration, the fix _is not_ offered on the declaration
        /// itself, but _is_ offered on the invocation pattern.
        /// </remarks>
        [Fact]
        public async Task TestLocationWhereOfferedWithMultipleVariables()
        {
            await TestInRegularAndScript1Async(
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
        }

        /// <remarks>
        /// If we have a variable declaration and if it is read/written outside the delegate 
        /// invocation pattern, the fix is not offered on the declaration.
        /// </remarks>
        [Fact]
        public async Task TestMissingOnDeclarationIfUsedOutside()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        /// <remarks>
        /// If we have a variable declaration and if it is read/written outside the delegate 
        /// invocation pattern, the fix is not offered on the declaration but is offered on
        /// the invocation pattern itself.
        /// </remarks>
        [Fact]
        public async Task TestLocationWhereOfferedIfUsedOutside()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestSimpleForm1()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestSimpleForm2()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestInElseClause1()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestInElseClause2()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestTrivia1()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestTrivia2()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51563")]
        public async Task TestTrivia3()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51563")]
        public async Task TestTrivia4()
        {
            await TestInRegularAndScript1Async(
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
        }

        /// <remarks>
        /// tests locations where the fix is offered.
        /// </remarks>
        [Fact]
        public async Task TestFixOfferedOnIf()
        {
            await TestInRegularAndScript1Async(
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
        }

        /// <remarks>
        /// tests locations where the fix is offered.
        /// </remarks>
        [Fact]
        public async Task TestFixOfferedInsideIf()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestMissingOnConditionalInvocation()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMissingOnConditionalInvocation2()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMissingOnConditionalInvocation3()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMissingOnNonNullCheckExpressions()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMissingOnNonNullCheckExpressions2()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        /// <remarks>
        /// if local declaration is not immediately preceding the invocation pattern, 
        /// the fix is not offered on the declaration.
        /// </remarks>
        [Fact]
        public async Task TestLocalNotImmediatelyPrecedingNullCheckAndInvokePattern()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        /// <remarks>
        /// if local declaration is not immediately preceding the invocation pattern, 
        /// the fix is not offered on the declaration but is offered on the invocation pattern itself.
        /// </remarks>
        [Fact]
        public async Task TestLocalDNotImmediatelyPrecedingNullCheckAndInvokePattern2()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestMissingOnFunc()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13226")]
        public async Task TestWithLambdaInitializer()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13226")]
        public async Task TestWithLambdaInitializer2()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13226")]
        public async Task TestForWithAnonymousMethod()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13226")]
        public async Task TestWithMethodReference()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31827")]
        public async Task TestWithExplicitInvokeCall1()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31827")]
        public async Task TestWithExplicitInvokeCall2()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/50976")]
        public async Task TestMissingOnFunctionPointer()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }
    }
}
