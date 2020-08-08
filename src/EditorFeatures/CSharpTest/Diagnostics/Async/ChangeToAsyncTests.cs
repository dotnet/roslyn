// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.Testing.EmptyDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.CSharp.CodeFixes.Async.CSharpConvertToAsyncMethodCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.Async
{
    public class ChangeToAsyncTests
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
        {|CS4008:await gt()|};
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
            await VerifyCS.VerifyCodeFixAsync(initial, expected);
        }
    }
}
