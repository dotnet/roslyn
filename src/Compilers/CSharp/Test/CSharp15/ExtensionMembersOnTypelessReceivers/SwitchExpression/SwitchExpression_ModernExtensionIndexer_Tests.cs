// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_SwitchExpression_ModernExtensionIndexer_Tests : CompilingTestBase
{
    [Fact]
    public void Indexer_OnSwitch_Executes()
    {
        var source = """
            public static class Ext
            {
                extension(int? n)
                {
                    public int this[long offset] => (n ?? 0) + (int)offset;
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    int x = 1;
                    System.Console.Write((x switch { 1 => null, _ => 5 })[3L]);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "3").VerifyDiagnostics();
    }

    [Fact]
    public void Indexer_NoCandidateInScope_FallsBackToSwitchExpressionNoBestType()
    {
        // No extension indexer is in scope. Helper returns null; legacy switch-expression
        // no-best-type diagnostic surfaces.
        var source = """
            public class Goo
            {
                public static void M()
                {
                    int x = 1;
                    _ = (x switch { 1 => null, _ => 5 })[0];
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (6,16): error CS8506: No best type was found for the switch expression.
            //         _ = (x switch { 1 => null, _ => 5 })[0];
            Diagnostic(ErrorCode.ERR_SwitchExpressionNoBestType, "switch").WithLocation(6, 16));
    }
}
