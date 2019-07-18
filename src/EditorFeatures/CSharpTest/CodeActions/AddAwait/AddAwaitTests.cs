// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.AddAwait;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.AddAwait
{
    [Trait(Traits.Feature, Traits.Features.AddAwait)]
    public class AddAwaitTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpAddAwaitCodeRefactoringProvider();

        [Fact]
        public async Task Simple()
        {
            await TestInRegularAndScriptAsync(@"
using System.Threading.Tasks;
class Program
{
    async Task<int> GetNumberAsync()
    {
        var x = GetNumberAsync()[||];
    }
}", @"
using System.Threading.Tasks;
class Program
{
    async Task<int> GetNumberAsync()
    {
        var x = await GetNumberAsync();
    }
}");
        }

        [Fact]
        public async Task SimpleWithConfigureAwait()
        {
            await TestInRegularAndScriptAsync(@"
using System.Threading.Tasks;
class Program
{
    async Task<int> GetNumberAsync()
    {
        var x = GetNumberAsync()[||];
    }
}", @"
using System.Threading.Tasks;
class Program
{
    async Task<int> GetNumberAsync()
    {
        var x = await GetNumberAsync().ConfigureAwait(false);
    }
}", index: 1);
        }

        [Fact]
        public async Task InArgument()
        {
            await TestInRegularAndScriptAsync(@"
using System.Threading.Tasks;
class Program
{
    async Task<int> GetNumberAsync(int argument)
    {
        var x = GetNumberAsync(arg[||]ument);
    }
}", @"
using System.Threading.Tasks;
class Program
{
    async Task<int> GetNumberAsync(int argument)
    {
        var x = await GetNumberAsync(argument);
    }
}");
        }

        [Fact]
        public async Task InvocationInArgument()
        {
            await TestInRegularAndScriptAsync(@"
using System.Threading.Tasks;
class Program
{
    async Task<int> GetNumberAsync()
    {
        M(GetNumberAsync()[||]);
    }
}", @"
using System.Threading.Tasks;
class Program
{
    async Task<int> GetNumberAsync()
    {
        M(await GetNumberAsync());
    }
}");
        }

        [Fact]
        public async Task InvocationInArgumentWithConfigureAwait()
        {
            await TestInRegularAndScriptAsync(@"
using System.Threading.Tasks;
class Program
{
    async Task<int> GetNumberAsync()
    {
        M(GetNumberAsync()[||]);
    }
}", @"
using System.Threading.Tasks;
class Program
{
    async Task<int> GetNumberAsync()
    {
        M(await GetNumberAsync().ConfigureAwait(false));
    }
}", index: 1);
        }

        [Fact]
        public async Task AlreadyAwaited()
        {
            await TestMissingInRegularAndScriptAsync(@"
using System.Threading.Tasks;
class Program
{
    async Task<int> GetNumberAsync()
    {
        var x = await GetNumberAsync()[||];
    }
}");
        }

        [Fact]
        public async Task AlreadyAwaitedAndConfigured()
        {
            await TestMissingInRegularAndScriptAsync(@"
using System.Threading.Tasks;
class Program
{
    async Task<int> GetNumberAsync()
    {
        var x = await GetNumberAsync()[||].ConfigureAwait(false);
    }
}");
        }

        [Fact]
        public async Task AlreadyAwaitedAndConfigured2()
        {
            await TestMissingInRegularAndScriptAsync(@"
using System.Threading.Tasks;
class Program
{
    async Task<int> GetNumberAsync()
    {
        var x = await GetNumberAsync().ConfigureAwait(false)[||];
    }
}");
        }

        [Fact]
        public async Task SimpleWithTrivia()
        {
            await TestInRegularAndScriptAsync(@"
using System.Threading.Tasks;
class Program
{
    async Task<int> GetNumberAsync()
    {
        var x = // comment
            GetNumberAsync()[||] /* comment */
    }
}", @"
using System.Threading.Tasks;
class Program
{
    async Task<int> GetNumberAsync()
    {
        var x = // comment
            await GetNumberAsync()[||] /* comment */
    }
}");
        }

        [Fact]
        public async Task SimpleWithTrivia2()
        {
            await TestInRegularAndScriptAsync(@"
using System.Threading.Tasks;
class Program
{
    async Task<int> GetNumberAsync()
    {
        var x = /* comment */ GetNumberAsync()[||] // comment
    }
}", @"
using System.Threading.Tasks;
class Program
{
    async Task<int> GetNumberAsync()
    {
        var x = /* comment */ await GetNumberAsync()[||] // comment
    }
}");
        }

        [Fact]
        public async Task SimpleWithTriviaWithConfigureAwait()
        {
            await TestInRegularAndScriptAsync(@"
using System.Threading.Tasks;
class Program
{
    async Task<int> GetNumberAsync()
    {
        var x = // comment
            GetNumberAsync()[||] /* comment */
    }
}", @"
using System.Threading.Tasks;
class Program
{
    async Task<int> GetNumberAsync()
    {
        var x = // comment
            await GetNumberAsync().ConfigureAwait(false) /* comment */
    }
}", index: 1);
        }

        [Fact]
        public async Task SimpleWithTrivia2WithConfigureAwait()
        {
            await TestInRegularAndScriptAsync(@"
using System.Threading.Tasks;
class Program
{
    async Task<int> GetNumberAsync()
    {
        var x = /* comment */ GetNumberAsync()[||] // comment
    }
}", @"
using System.Threading.Tasks;
class Program
{
    async Task<int> GetNumberAsync()
    {
        var x = /* comment */ await GetNumberAsync().ConfigureAwait(false) // comment
    }
}", index: 1);
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task OnSemiColon()
        {
            await TestInRegularAndScriptAsync(@"
using System.Threading.Tasks;
class Program
{
    async Task<int> GetNumberAsync()
    {
        var x = GetNumberAsync();[||]
    }
}", @"
using System.Threading.Tasks;
class Program
{
    async Task<int> GetNumberAsync()
    {
        var x = await GetNumberAsync();
    }
}");
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task Selection()
        {
            await TestInRegularAndScriptAsync(@"
using System.Threading.Tasks;
class Program
{
    async Task<int> GetNumberAsync()
    {
        var x = [|GetNumberAsync()|];
    }
}", @"
using System.Threading.Tasks;
class Program
{
    async Task<int> GetNumberAsync()
    {
        var x = await GetNumberAsync();
    }
}");
        }

        [Fact]
        [WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")]
        public async Task Selection2()
        {
            await TestInRegularAndScriptAsync(@"
using System.Threading.Tasks;
class Program
{
    async Task<int> GetNumberAsync()
    {
        [|var x = GetNumberAsync();|]
    }
}", @"
using System.Threading.Tasks;
class Program
{
    async Task<int> GetNumberAsync()
    {
        var x = await GetNumberAsync();
    }
}");
        }


        [Fact]
        public async Task ChainedInvocation()
        {
            await TestMissingInRegularAndScriptAsync(@"
using System.Threading.Tasks;
class Program
{
    Task<int> GetNumberAsync() => throw null;
    async void M()
    {
        var x = GetNumberAsync()[||].ToString();
    }
}");
        }

        [Fact]
        public async Task ChainedInvocation_ExpressionOfInvalidInvocation()
        {
            await TestInRegularAndScript1Async(@"
using System.Threading.Tasks;
class Program
{
    Task<int> GetNumberAsync() => throw null;
    async void M()
    {
        var x = GetNumberAsync()[||].Invalid();
    }
}", @"
using System.Threading.Tasks;
class Program
{
    Task<int> GetNumberAsync() => throw null;
    async void M()
    {
        var x = (await GetNumberAsync()).Invalid();
    }
}");
        }
    }
}
