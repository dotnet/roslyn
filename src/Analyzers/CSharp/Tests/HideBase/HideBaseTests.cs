// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.HideBase;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.HideBase;

[Trait(Traits.Feature, Traits.Features.CodeActionsAddNew)]
public sealed class HideBaseTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public HideBaseTests(ITestOutputHelper logger)
       : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer?, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (null, new HideBaseCodeFixProvider());

    [Fact]
    public Task TestAddNewToProperty()
        => TestInRegularAndScriptAsync(
            """
            class Application
            {
                public static Application Current { get; }
            }

            class App : Application
            {
                [|public static App Current|] { get; set; }
            }
            """,
            """
            class Application
            {
                public static Application Current { get; }
            }

            class App : Application
            {
                public static new App Current { get; set; }
            }
            """);

    [Fact]
    public Task TestAddNewToMethod()
        => TestInRegularAndScriptAsync(
            """
            class Application
            {
                public static void Method()
                {
                }
            }

            class App : Application
            {
                [|public static void Method()
                {
                }|]
            }
            """,
            """
            class Application
            {
                public static void Method()
                {
                }
            }

            class App : Application
            {
                public static new void Method()
                {
                }
            }
            """);

    [Fact]
    public Task TestAddNewToField()
        => TestInRegularAndScriptAsync(
            """
            class Application
            {
                public string Test;
            }

            class App : Application
            {
                [|public int Test;|]
            }
            """,
            """
            class Application
            {
                public string Test;
            }

            class App : Application
            {
                public new int Test;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18391")]
    public Task TestAddNewToConstant()
        => TestInRegularAndScriptAsync(
            """
            class Application
            {
                public const int Test = 1;
            }

            class App : Application
            {
                [|public const int Test = Application.Test + 1;|]
            }
            """,
            """
            class Application
            {
                public const int Test = 1;
            }

            class App : Application
            {
                public new const int Test = Application.Test + 1;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/14455")]
    public Task TestAddNewToConstantInternalFields()
        => TestInRegularAndScriptAsync(
            """
            class A { internal const int i = 0; }
            class B : A { [|internal const int i = 1;|] }
            """,
            """
            class A { internal const int i = 0; }
            class B : A { internal new const int i = 1; }
            """);

    [Fact]
    public async Task TestAddNewToDisorderedModifiers()
        => await TestInRegularAndScriptAsync(
            """
            class Application
            {
                public static string Test;
            }

            class App : Application
            {
                [|static public int Test;|]
            }
            """,
            """
            class Application
            {
                public static string Test;
            }

            class App : Application
            {
                static public new int Test;
            }
            """);

    [Fact]
    public async Task TestAddNewToOrderedModifiersWithTrivia()
        => await TestInRegularAndScriptAsync(
            """
            class Application
            {
                public string Test;
            }

            class App : Application
            {
                [|/* start */ public /* middle */ readonly /* end */ int Test;|]
            }
            """,
            """
            class Application
            {
                public string Test;
            }

            class App : Application
            {
                /* start */ public /* middle */ new readonly /* end */ int Test;
            }
            """);
}
