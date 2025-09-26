// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Emit
{
    public class Perf : CSharpTestBase
    {
        [Fact]
        public void Test()
        {
            // This test ensures that our perf benchmark code compiles without problems.
            // Benchmark code can be found in the following file under the 
            // "CompilerTestResources" project that is part of Roslyn.slnx -
            //      $/Roslyn/Main/Open/Compilers/Test/Resources/Core/PerfTests/CSPerfTest.cs

            // You can also use VS's "Navigate To" feature to find the above file easily -
            // Just hit "Ctrl + ," and type "CSPerfTest.cs" in the dialog that pops up.

            // Please note that if this test fails, it is likely because of a bug in the
            // *product* and not in the *test* / *benchmark code* :)
            // The benchmark code has been verified to compile fine against Dev10.
            // So if the test fails we should fix the product bug that is causing the failure
            // as opposed to 'fixing' the test by updating the benchmark code.

            //GNAMBOO: Changing this code has implications for perf tests.
            CompileAndVerify(TestResources.PerfTests.CSPerfTest).
                             VerifyDiagnostics(
                                // (2416,9): info CS8019: Unnecessary using directive.
                                //         using nested;
                                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using nested;"));
        }
    }
}
