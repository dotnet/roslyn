// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_ConditionalExpression_ModernExtensionMethod_Tests : CompilingTestBase
{
    [Fact]
    public void Conditional_NullAndInt_Executes()
    {
        var source = """
            public static class Ext
            {
                extension(int? n)
                {
                    public int OrZero() => n ?? 0;
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    bool b = true;
                    System.Console.Write((b ? null : 9).OrZero());
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "0").VerifyDiagnostics();
    }

    [Fact]
    public void Conditional_NoExtensionInScope_FallsBackToInvalidQM()
    {
        // No extension is in scope. Helper returns null; legacy ERR_InvalidQM fires.
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
