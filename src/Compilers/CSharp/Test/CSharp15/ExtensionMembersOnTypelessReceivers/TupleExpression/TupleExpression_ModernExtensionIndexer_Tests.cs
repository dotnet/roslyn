// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_TupleExpression_ModernExtensionIndexer_Tests : CompilingTestBase
{
    [Fact]
    public void Indexer_OnTuple_TypelessElement_Executes()
    {
        var source = """
            public static class Ext
            {
                extension((int, string) t)
                {
                    public string this[long i] => i == 0L ? t.Item1.ToString() : (t.Item2 ?? "empty");
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write((1, null)[1L]);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "empty").VerifyDiagnostics();
    }

    [Fact]
    public void Indexer_NoCandidateInScope_FallsBackToLegacy()
    {
        // No extension indexer is in scope. Helper returns null; legacy fallback path produces
        // diagnostics about the typeless tuple element.
        var source = """
            public class Goo
            {
                public static void M()
                {
                    _ = (1, null)[0];
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (5,13): error CS0021: Cannot apply indexing with [] to an expression of type '(int, <null>)'
            //         _ = (1, null)[0];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "(1, null)[0]").WithArguments("(int, <null>)").WithLocation(5, 13));
    }
}
