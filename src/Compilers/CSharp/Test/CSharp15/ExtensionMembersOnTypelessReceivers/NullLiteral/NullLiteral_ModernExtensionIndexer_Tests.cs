// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_NullLiteral_ModernExtensionIndexer_Tests : CompilingTestBase
{
    [Fact]
    public void Indexer_OnNullLiteral_Executes()
    {
        var source = """
            public static class Ext
            {
                extension(string s)
                {
                    public string this[long i] => s ?? "empty";
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write(null[0L]);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "empty").VerifyDiagnostics();
    }

    [Fact]
    public void Indexer_OnNullLiteral_IntReceiver_Executes()
    {
        var source = """
            public static class Ext
            {
                extension(int[] xs)
                {
                    public int this[long i] => xs?[0] ?? -1;
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write(null[0L]);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "-1").VerifyDiagnostics();
    }

    [Fact]
    public void Indexer_NoCandidateInScope_FallsBackToBadIndexLHS()
    {
        // No extension indexer is in scope. Helper returns null; legacy null-literal rejection
        // produces the existing element-access diagnostic.
        var source = """
            public class Goo
            {
                public static void M()
                {
                    _ = null[0];
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (5,13): error CS0021: Cannot apply indexing with [] to an expression of type '<null>'
            //         _ = null[0];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "null[0]").WithArguments("<null>").WithLocation(5, 13));
    }
}
