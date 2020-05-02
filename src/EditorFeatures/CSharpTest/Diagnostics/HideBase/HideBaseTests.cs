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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.HideBase
{
    public class HideBaseTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new HideBaseCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddNew)]
        public async Task TestAddNewToProperty()
        {
            await TestInRegularAndScriptAsync(
@"class Application
{
    public static Application Current { get; }
}

class App : Application
{
    [|public static App Current|] { get; set; }
}",
@"class Application
{
    public static Application Current { get; }
}

class App : Application
{
    public static new App Current { get; set; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddNew)]
        public async Task TestAddNewToMethod()
        {
            await TestInRegularAndScriptAsync(
@"class Application
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
}",
@"class Application
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddNew)]
        public async Task TestAddNewToField()
        {
            await TestInRegularAndScriptAsync(
@"class Application
{
    public string Test;
}

class App : Application
{
    [|public int Test;|]
}",
@"class Application
{
    public string Test;
}

class App : Application
{
    public new int Test;
}");
        }

        [WorkItem(18391, "https://github.com/dotnet/roslyn/issues/18391")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddNew)]
        public async Task TestAddNewToConstant()
        {
            await TestInRegularAndScriptAsync(
@"class Application
{
    public const int Test = 1;
}

class App : Application
{
    [|public const int Test = Application.Test + 1;|]
}",
@"class Application
{
    public const int Test = 1;
}

class App : Application
{
    public new const int Test = Application.Test + 1;
}");
        }

        [WorkItem(14455, "https://github.com/dotnet/roslyn/issues/14455")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddNew)]
        public async Task TestAddNewToConstantInternalFields()
        {
            await TestInRegularAndScriptAsync(
@"class A { internal const int i = 0; }
class B : A { [|internal const int i = 1;|] }
",
@"class A { internal const int i = 0; }
class B : A { internal new const int i = 1; }
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddNew)]
        public async Task TestAddNewToDisorderedModifiers() =>
            await TestInRegularAndScript1Async(
@"class Application
{
    public static string Test;
}

class App : Application
{
    [|static public int Test;|]
}",
@"class Application
{
    public static string Test;
}

class App : Application
{
    static public new int Test;
}");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddNew)]
        public async Task TestAddNewToOrderedModifiersWithTrivia() =>
            await TestInRegularAndScript1Async(
@"class Application
{
    public string Test;
}

class App : Application
{
    [|/* start */ public /* middle */ readonly /* end */ int Test;|]
}",
@"class Application
{
    public string Test;
}

class App : Application
{
    /* start */ public /* middle */ new readonly /* end */ int Test;
}");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveNew)]
        public async Task TestRemoveNewFromProperty()
        {
            await TestInRegularAndScriptAsync(
                @"class App
{
    [|public static new App Current|] { get; set; }
}",
                @"class App
{
    public static App Current { get; set; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveNew)]
        public async Task TestRemoveNewFromMethod()
        {
            await TestInRegularAndScriptAsync(
@"class App
{
    [|public static new void Method()
    {
    }|]
}",
@"class App
{
    public static void Method()
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveNew)]
        public async Task TestRemoveNewFromField()
        {
            await TestInRegularAndScriptAsync(
@"class App
{
    [|public new int Test;|]
}",
@"class App
{
    public int Test;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveNew)]
        public async Task TestRemoveNewFromConstant()
        {
            await TestInRegularAndScriptAsync(
@"class App
{
    [|public const new int Test = 1;|]
}",
@"class App
{
    public const int Test = 1;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveNew)]
        public async Task TestRemoveNewFirstModifier()
        {
            await TestInRegularAndScriptAsync(
                @"class App
{
    [|new App Current|] { get; set; }
}",
                @"class App
{
    App Current { get; set; }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveNew)]
        public async Task TestRemoveNewFromConstantInternalFields()
        {
            await TestInRegularAndScriptAsync(
@"class A { [|internal const new int i = 1;|] }",
@"class A { internal const int i = 1; }");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveNew)]
        [InlineData(
            "/* start */ public /* middle */ new /* end */ int Test;",
            "/* start */ public /* middle */ /* end */ int Test;")]
        [InlineData(
            "/* start */ public /* middle */ new    /* end */ int Test;",
            "/* start */ public /* middle */ /* end */ int Test;")]
        [InlineData(
            "/* start */ public /* middle */new /* end */ int Test;",
            "/* start */ public /* middle */ /* end */ int Test;")]
        [InlineData(
            "/* start */ public /* middle */ new/* end */ int Test;",
            "/* start */ public /* middle */ /* end */ int Test;")]
        [InlineData(
            "/* start */ public /* middle */new/* end */ int Test;",
            "/* start */ public /* middle *//* end */ int Test;")]
        [InlineData(
            "new /* end */ int Test;",
            "/* end */ int Test;")]
        [InlineData(
            "new     int Test;",
            "int Test;")]
        [InlineData(
            "/* start */ new /* end */ int Test;",
            "/* start */ /* end */ int Test;")]
        public async Task TestRemoveNewFromModifiersWithTrivia(string original, string expected) =>
            await TestInRegularAndScript1Async(
$@"class App
{{
    [|{original}|]
}}",
$@"class App
{{
    {expected}
}}");
    }
}
