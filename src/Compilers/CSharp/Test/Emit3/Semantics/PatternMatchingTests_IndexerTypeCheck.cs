// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

[CompilerTrait(CompilerFeature.Patterns)]
public class PatternMatchingTests_IndexerTypeCheck : PatternMatchingTestBase
{
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76868")]
    public void IndexerEvaluation_DifferentResultTypes_Debug()
    {
        // This test attempts to create a scenario where IsEqualEvaluation's missing type check
        // could cause problems. Without the fix, two indexer evaluations with the same index
        // but different IndexerTypes might be incorrectly considered equal.
        // This could potentially cause assertion failures in Debug builds or incorrect IL.
        var source = """
            using System;
            
            // Collection with object indexer
            class CollectionA
            {
                public int Length => 3;
                public object this[int i] => $"A{i}";
                public CollectionB Slice(int start, int length) => new CollectionB();
            }
            
            // Slice result with string indexer  
            class CollectionB
            {
                public int Length => 1;
                public string this[int i] => $"B{i}";
            }
            
            class Program
            {
                static string Test(CollectionA a1, CollectionA a2)
                {
                    // Complex pattern that creates multiple indexer evaluations
                    // Both collections get sliced and their [0] elements accessed
                    // a1 and a2 produce CollectionB slices with string indexers
                    // Original a1[0] and a2[0] have object indexers
                    return (a1, a2) switch
                    {
                        ([var x1, .. var slice1, _], [var x2, .. var slice2, _]) 
                            when slice1 is [var z1] && slice2 is [var z2] => $"{x1},{x2},{z1},{z2}",
                        _ => "none"
                    };
                }
                
                static void Main()
                {
                    var a1 = new CollectionA();
                    var a2 = new CollectionA();
                    Console.WriteLine(Test(a1, a2));
                }
            }
            """;

        var compilation = CreateCompilationWithIndexAndRange(source, options: TestOptions.DebugExe);
        // Note: Without the fix, this might fail with an assertion in Debug builds
        // or produce incorrect/unverifiable IL
        compilation.VerifyDiagnostics();

        var verifier = CompileAndVerify(compilation, expectedOutput: "A0,A0,B0,B0", verify: Verification.Passes);
    }
}
