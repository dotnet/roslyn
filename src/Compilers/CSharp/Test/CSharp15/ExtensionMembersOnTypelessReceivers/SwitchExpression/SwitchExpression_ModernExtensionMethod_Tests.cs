// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_SwitchExpression_ModernExtensionMethod_Tests : CompilingTestBase
{
    [Fact]
    public void Switch_NullAndInt_Executes()
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
                    int n = 0;
                    System.Console.Write((n switch { 0 => null, _ => 7 }).OrZero());
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "0").VerifyDiagnostics();
    }

    [Fact]
    public void Switch_NoApplicableExtension_ReportsNoSuchMember()
    {
        var source = """
            public class Goo
            {
                public static void M()
                {
                    int n = 0;
                    _ = (n switch { 0 => null, _ => 7 }).DoesNotExist();
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (6,46): error CS0117: '<switch expression>' does not contain a definition for 'DoesNotExist'
            //         _ = (n switch { 0 => null, _ => 7 }).DoesNotExist();
            Diagnostic(ErrorCode.ERR_NoSuchMember, "DoesNotExist").WithArguments("<switch expression>", "DoesNotExist").WithLocation(6, 46));
    }
}
