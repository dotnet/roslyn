// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_SwitchExpression_ClassicExtensionMethod_Tests : CompilingTestBase
{
    [Fact]
    public void Switch_NullAndInt_Executes()
    {
        // (n switch { 0 => null, _ => 5 }) has no common arm type. Target-typed against the
        // extension's first parameter (int?), the switch binds.
        var source = """
            public static class Ext
            {
                public static int OrZero(this int? n) => n ?? 0;
            }

            public class Goo
            {
                public static void Main()
                {
                    int n = 1;
                    System.Console.Write((n switch { 0 => null, _ => 5 }).OrZero());
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "5").VerifyDiagnostics();
    }

    [Fact]
    public void Switch_NoExtensionInScope_FallsBackToSwitchExpressionNoBestType()
    {
        // No extension is in scope. The typeless-receiver feature only engages when at least
        // one extension candidate exists; without one, the helper returns null and the legacy
        // switch-expression binding produces ERR_SwitchExpressionNoBestType.
        var source = """
            public class Goo
            {
                public static void M()
                {
                    int n = 1;
                    _ = (n switch { 0 => null, _ => 5 }).DoesNotExist();
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (6,16): error CS8506: No best type was found for the switch expression.
            //         _ = (n switch { 0 => null, _ => 5 }).DoesNotExist();
            Diagnostic(ErrorCode.ERR_SwitchExpressionNoBestType, "switch").WithLocation(6, 16));
    }
}
