// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    [CompilerTrait(CompilerFeature.AsyncStreams, CompilerFeature.Async)]
    public class CodeGenRuntimeAsyncIteratorTests : EmitMetadataTestBase
    {
        [Fact]
        public void RuntimeAsyncIterator_NotYetImplemented()
        {
            // Test that async-iterators with runtime-async enabled report an error
            var source = @"
using System.Collections.Generic;
using System.Threading.Tasks;

class C
{
    async IAsyncEnumerable<int> M()
    {
        await Task.CompletedTask;
        yield return 42;
    }
}
";
            var comp = CreateRuntimeAsyncCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (7,33): error CS9328: Method 'C.M()' uses a feature that is not supported by runtime async currently. Opt the method out of runtime async by attributing it with 'System.Runtime.CompilerServices.RuntimeAsyncMethodGenerationAttribute(false)'.
                //     async IAsyncEnumerable<int> M()
                Diagnostic(ErrorCode.ERR_UnsupportedFeatureInRuntimeAsync, "M").WithArguments("C.M()").WithLocation(7, 33));
        }

        [Fact]
        public void RuntimeAsyncIterator_IAsyncEnumerator_NotYetImplemented()
        {
            // Test that async-enumerators with runtime-async enabled report an error
            var source = @"
using System.Collections.Generic;
using System.Threading.Tasks;

class C
{
    async IAsyncEnumerator<int> M()
    {
        await Task.CompletedTask;
        yield return 42;
    }
}
";
            var comp = CreateRuntimeAsyncCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (7,33): error CS9328: Method 'C.M()' uses a feature that is not supported by runtime async currently. Opt the method out of runtime async by attributing it with 'System.Runtime.CompilerServices.RuntimeAsyncMethodGenerationAttribute(false)'.
                //     async IAsyncEnumerator<int> M()
                Diagnostic(ErrorCode.ERR_UnsupportedFeatureInRuntimeAsync, "M").WithArguments("C.M()").WithLocation(7, 33));
        }

        [Fact]
        public void RuntimeAsync_RegularAsyncMethod_StillWorks()
        {
            // Test that regular async methods (non-iterators) still work with runtime-async
            var source = @"
using System;
using System.Threading.Tasks;

class C
{
    static async Task Main()
    {
        Console.Write(await M());
    }

    static async Task<int> M()
    {
        await Task.CompletedTask;
        return 42;
    }
}
";
            var comp = CreateRuntimeAsyncCompilation(source);
            comp.VerifyEmitDiagnostics();
        }
    }
}
