using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.RemoveUnusedVar;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.RemoveUnusedVar
{
    public partial class RemoveUnusedVarTest : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, CodeFixProvider>(
                null, new RemoveUnusedVarCodeFixProvider());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVar)]
        public async Task RemoveUnusedVar()
        {
            await TestAsync(
@"class Class
{
    void Method()
    {
        [|int a = 3;|]
    }
}",
@"class Class
{
    void Method()
    {
    }
}");
        }
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVar)]
        public async Task RemoveUnusedVar1()
        {
            await TestAsync(
@"class Class
{
    void Method()
    {
        [|string a;|]
        string b = "";
        var c = b;
    }
}",
@"class Class
{
    void Method()
    {
        string b = "";
        var c = b;
    }
}");
        }
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVar)]
        public async Task RemoveUnusedVar3()
        {
            await TestAsync(
@"class Class
{
    void Method()
    {
        [|string a;|]
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
