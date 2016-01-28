// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.HideBase;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using Xunit;
using Roslyn.Test.Utilities;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.HideBase
{
    public class HideBaseTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return Tuple.Create<DiagnosticAnalyzer, CodeFixProvider>(null, new HideBaseCodeFixProvider());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddNew)]
        public async Task TestAddNewToProperty()
        {
            await TestAsync(
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
            await TestAsync(
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
            await TestAsync(
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
