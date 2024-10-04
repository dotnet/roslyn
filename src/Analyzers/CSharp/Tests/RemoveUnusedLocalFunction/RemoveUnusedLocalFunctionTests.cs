// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.RemoveUnusedLocalFunction;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnusedLocalFunction;

[Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedLocalFunction)]
public partial class RemoveUnusedLocalFunctionTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public RemoveUnusedLocalFunctionTests(ITestOutputHelper logger)
      : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer?, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (null, new CSharpRemoveUnusedLocalFunctionCodeFixProvider());

    [Fact]
    public async Task RemoveUnusedLocalFunction()
    {
        await TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    void [|Goo|]() { }
                }
            }
            """,
            """
            class Class
            {
                void Method()
                {
                }
            }
            """);
    }

    [Fact]
    public async Task RemoveUnusedLocalFunctionFixAll1()
    {
        await TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    void {|FixAllInDocument:F|}() { }
                    void G() { }
                }
            }
            """,
            """
            class Class
            {
                void Method()
                {
                }
            }
            """);
    }

    [Fact]
    public async Task RemoveUnusedLocalFunctionFixAll2()
    {
        await TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    void G() { }
                    void {|FixAllInDocument:F|}() { }
                }
            }
            """,
            """
            class Class
            {
                void Method()
                {
                }
            }
            """);
    }

    [Fact]
    public async Task RemoveUnusedLocalFunctionFixAll3()
    {
        await TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    void {|FixAllInDocument:F|}() { void G() { } }
                }
            }
            """,
            """
            class Class
            {
                void Method()
                {
                }
            }
            """);
    }

    [Fact]
    public async Task RemoveUnusedLocalFunctionFixAll4()
    {
        await TestInRegularAndScriptAsync(
            """
            class Class
            {
                void Method()
                {
                    void G() { void {|FixAllInDocument:F|}() { } }
                }
            }
            """,
            """
            class Class
            {
                void Method()
                {
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/44272")]
    public async Task TopLevelStatement()
    {
        await TestAsync("""
            void [|local()|] { }
            """,
            """

            """, TestOptions.Regular);
    }
}
