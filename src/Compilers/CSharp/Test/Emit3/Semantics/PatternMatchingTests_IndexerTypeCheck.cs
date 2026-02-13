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
    public void IndexerEvaluation_DifferentResultTypes()
    {
        // This test demonstrates that IsEqualEvaluation should check IndexerType
        // when comparing two BoundDagIndexerEvaluation nodes.
        // In theory, slicing can result in a collection with an element type
        // that is different from the element type of the original collection.
        var source = """
            using System;
            
            class C
            {
                public int Length => 3;
                public object this[int i] => i;
                public D Slice(int start, int length) => new D();
            }
            
            class D
            {
                public int Length => 1;
                public string this[int i] => i.ToString();
            }
            
            class Program
            {
                static bool Test(C c)
                {
                    // Pattern matching that accesses both the outer and inner indexers
                    // c[0] returns object, but slice[0] returns string
                    return c is [var x, ..var slice, var y] && 
                           slice is [var z] &&
                           x != null && y != null && z != null;
                }
                
                static void Main()
                {
                    var c = new C();
                    Console.WriteLine(Test(c));
                }
            }
            """;

        var compilation = CreateCompilationWithIndexAndRange(source, options: TestOptions.ReleaseExe);
        compilation.VerifyDiagnostics();
        
        CompileAndVerify(compilation, expectedOutput: "True");
    }
}
