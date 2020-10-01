﻿// Licensed to the .NET Foundation under one or more agreements.
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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnusedLocalFunction
{
    public partial class RemoveUnusedLocalFunctionTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpRemoveUnusedLocalFunctionCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedLocalFunction)]
        public async Task RemoveUnusedLocalFunction()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        void [|Goo|]() { }
    }
}",
@"class Class
{
    void Method()
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedLocalFunction)]
        public async Task RemoveUnusedLocalFunctionFixAll1()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        void {|FixAllInDocument:F|}() { }
        void G() { }
    }
}",
@"class Class
{
    void Method()
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedLocalFunction)]
        public async Task RemoveUnusedLocalFunctionFixAll2()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        void G() { }
        void {|FixAllInDocument:F|}() { }
    }
}",
@"class Class
{
    void Method()
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedLocalFunction)]
        public async Task RemoveUnusedLocalFunctionFixAll3()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        void {|FixAllInDocument:F|}() { void G() { } }
    }
}",
@"class Class
{
    void Method()
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedLocalFunction)]
        public async Task RemoveUnusedLocalFunctionFixAll4()
        {
            await TestInRegularAndScriptAsync(
@"class Class
{
    void Method()
    {
        void G() { void {|FixAllInDocument:F|}() { } }
    }
}",
@"class Class
{
    void Method()
    {
    }
}");
        }

        [WorkItem(44272, "https://github.com/dotnet/roslyn/issues/44272")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedLocalFunction)]
        public async Task TopLevelStatement()
        {
            await TestAsync(@"
void [|local()|] { }
",
@"
", TestOptions.Regular);
        }
    }
}
