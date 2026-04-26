// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_ConditionalExpression_ClassicExtensionMethod_Tests : CompilingTestBase
{
    [Fact]
    public void Conditional_NullAndInt_Executes()
    {
        // (b ? null : 5) has no common type between null and int. Target-typed against the
        // extension's first parameter (int?), the conditional binds.
        var source = """
            public static class Ext
            {
                public static int OrZero(this int? n) => n ?? 0;
            }

            public class Goo
            {
                public static void Main()
                {
                    bool b = false;
                    System.Console.Write((b ? null : 5).OrZero());
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "5").VerifyDiagnostics();
    }

    [Fact]
    public void Conditional_TwoArmTypelessElements_Executes()
    {
        // Both arms are themselves typeless (default and null). They share no common type, so
        // the conditional itself is typeless and target-types against the extension's parameter.
        var source = """
            public static class Ext
            {
                public static string Or(this string s, string fallback) => s ?? fallback;
            }

            public class Goo
            {
                public static void Main()
                {
                    bool b = true;
                    System.Console.Write((b ? default : null).Or("fallback"));
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "fallback").VerifyDiagnostics();
    }

    [Fact]
    public void Conditional_NoExtensionInScope_FallsBackToInvalidQM()
    {
        // No extension is in scope. The typeless-receiver feature only engages when at least
        // one extension candidate exists; without one, the helper returns null and the legacy
        // conditional-expression binding produces ERR_InvalidQM.
        var source = """
            public class Goo
            {
                public static void M()
                {
                    bool b = true;
                    _ = (b ? null : 5).DoesNotExist();
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (6,14): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between '<null>' and 'int'
            //         _ = (b ? null : 5).DoesNotExist();
            Diagnostic(ErrorCode.ERR_InvalidQM, "b ? null : 5").WithArguments("<null>", "int").WithLocation(6, 14));
    }
}
