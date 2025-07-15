// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias InteractiveHost;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Interactive
{
    using InteractiveHost::Microsoft.CodeAnalysis.Interactive;
    using Xunit.Abstractions;

    [Trait(Traits.Feature, Traits.Features.InteractiveHost)]
    public sealed class InteractiveHostCoreTests(ITestOutputHelper testOutputHelper) : AbstractInteractiveHostTests(testOutputHelper)
    {
        internal override InteractiveHostPlatform DefaultPlatform => InteractiveHostPlatform.Core;
        internal override bool UseDefaultInitializationFile => false;

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/53392")]
        public async Task StackOverflow()
        {
            var process = Host.TryGetProcess();

            await Execute(@"
int goo(int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9) 
{ 
    return goo(0,1,2,3,4,5,6,7,8,9) + goo(0,1,2,3,4,5,6,7,8,9); 
} 
goo(0,1,2,3,4,5,6,7,8,9)
            ");

            var output = await ReadOutputToEnd();
            Assert.Equal("", output);

            // Hosting process exited with exit code ###.
            var errorOutput = (await ReadErrorOutputToEnd()).Trim();
            Assert.True(errorOutput.StartsWith("Stack overflow.\n"));
            Assert.True(errorOutput.EndsWith(string.Format(InteractiveHostResources.Hosting_process_exited_with_exit_code_0, process!.ExitCode)));

            await Execute(@"1+1");
            output = await ReadOutputToEnd();
            Assert.Equal("2\r\n", output.ToString());
        }
    }
}
