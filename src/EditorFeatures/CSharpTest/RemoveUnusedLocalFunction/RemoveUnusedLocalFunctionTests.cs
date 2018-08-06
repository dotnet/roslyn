// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.RemoveUnusedLocalFunction;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
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
    }
}
