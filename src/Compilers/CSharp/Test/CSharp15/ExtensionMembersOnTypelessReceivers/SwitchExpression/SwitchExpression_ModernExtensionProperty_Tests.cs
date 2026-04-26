// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_SwitchExpression_ModernExtensionProperty_Tests : CompilingTestBase
{
    [Fact]
    public void Property_OnSwitch_Executes()
    {
        var source = """
            public static class Ext
            {
                extension(int? n)
                {
                    public int OrZero => n ?? 0;
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    int n = 1;
                    System.Console.Write((n switch { 0 => null, _ => 9 }).OrZero);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "9").VerifyDiagnostics();
    }

    [Fact]
    public void Property_NoCandidateInScope_FallsBackToSwitchExpressionNoBestType()
    {
        // No extension is in scope. Helper returns null; legacy ERR_SwitchExpressionNoBestType
        // fires.
        var source = """
            public class Goo
            {
                public static void M()
                {
                    int n = 0;
                    _ = (n switch { 0 => null, _ => 5 }).DoesNotExist;
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (6,16): error CS8506: No best type was found for the switch expression.
            //         _ = (n switch { 0 => null, _ => 5 }).DoesNotExist;
            Diagnostic(ErrorCode.ERR_SwitchExpressionNoBestType, "switch").WithLocation(6, 16));
    }
}
