// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.Async;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.Async
{
    public partial class ChangeToAsyncTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsChangeToAsync)]
        public async Task CantAwaitAsyncVoid()
        {
            var initial =
@"using System.Threading.Tasks;

class Program
{
    async Task rtrt()
    {
        [|await gt();|]
    }

    async void gt()
    {
    }
}";

            var expected =
@"using System.Threading.Tasks;

class Program
{
    async Task rtrt()
    {
        await gt();
    }

    async 
    Task
gt()
    {
    }
}";
            await TestInRegularAndScriptAsync(initial, expected);
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpConvertToAsyncMethodCodeFixProvider());
    }
}
