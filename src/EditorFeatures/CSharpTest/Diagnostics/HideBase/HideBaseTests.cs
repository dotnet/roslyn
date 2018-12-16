// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public async Task TestAddNewToMember()
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
    }
}
