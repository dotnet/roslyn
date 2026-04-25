// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_ConditionalExpression_ModernExtensionProperty_Tests : CompilingTestBase
{
    [Fact]
    public void Property_OnConditional_Executes()
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
                    bool b = true;
                    System.Console.Write((b ? null : 5).OrZero);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "0").VerifyDiagnostics();
    }

    [Fact]
    public void Property_NoCandidateInScope_ReportsNoSuchMember()
    {
        var source = """
            public class Goo
            {
                public static void M()
                {
                    bool b = true;
                    _ = (b ? null : 5).DoesNotExist;
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (6,28): error CS0117: 'target-typed conditional expression' does not contain a definition for 'DoesNotExist'
            //         _ = (b ? null : 5).DoesNotExist;
            Diagnostic(ErrorCode.ERR_NoSuchMember, "DoesNotExist").WithArguments("target-typed conditional expression", "DoesNotExist").WithLocation(6, 28));
    }
}
