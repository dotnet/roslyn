using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.UseObjectInitializer;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseObjectInitializer
{
    public partial class UseObjectInitializerTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, CodeFixProvider>(
                new CSharpUseObjectInitializerDiagnosticAnalyzer(),
                new CSharpUseObjectInitializerCodeFixProvider());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
        public async Task TestOnVariableDeclarator()
        {
            await TestAsync(
@"
class C
{
    int i;
    void M()
    {
        var c = [||]new C();
        c.i = 1;
    }
}",
@"
class C
{
    int i;
    void M()
    {
        var c = new C()
        {
            i = 1
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
        public async Task TestOnAssignmentExpression()
        {
            await TestAsync(
@"
class C
{
    int i;
    void M()
    {
        C c = null;
        c = [||]new C();
        c.i = 1;
    }
}",
@"
class C
{
    int i;
    void M()
    {
        C c = null;
        c = new C()
        {
            i = 1
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
        public async Task TestStopOnDuplicateMember()
        {
            await TestAsync(
@"
class C
{
    int i;
    void M()
    {
        var c = [||]new C();
        c.i = 1;
        c.i = 2;
    }
}",
@"
class C
{
    int i;
    void M()
    {
        var c = new C()
        {
            i = 1
        };
        c.i = 2;
    }
}");
        }
    }
}
