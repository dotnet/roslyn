// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_DefaultLiteral_ModernExtensionIndexer_Tests : CompilingTestBase
{
    [Fact]
    public void Indexer_OnDefault_Executes()
    {
        var source = """
            public static class Ext
            {
                extension(int[] xs)
                {
                    public int this[long i] => xs is null ? -1 : xs[(int)i];
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write(default[0L]);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "-1").VerifyDiagnostics();
    }

    [Fact]
    public void Indexer_OnDefault_StringReceiver_Executes()
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
                    System.Console.Write(default[0L]);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "empty").VerifyDiagnostics();
    }

    [Fact]
    public void Indexer_NoCandidateInScope_FallsBackToBadOpOnNullOrDefaultOrNew()
    {
        // No extension indexer is in scope. Helper returns null; legacy default-literal rejection
        // surfaces.
        var source = """
            public class Goo
            {
                public static void M()
                {
                    _ = default[0];
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (5,13): error CS8716: There is no target type for the default literal.
            //         _ = default[0];
            Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(5, 13));
    }
}
