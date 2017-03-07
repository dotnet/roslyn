// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.HideBase;
using Microsoft.CodeAnalysis.Diagnostics;
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
    }
}