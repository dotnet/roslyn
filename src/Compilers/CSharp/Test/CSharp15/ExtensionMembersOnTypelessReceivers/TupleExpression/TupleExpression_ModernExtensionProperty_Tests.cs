// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_TupleExpression_ModernExtensionProperty_Tests : CompilingTestBase
{
    [Fact]
    public void Property_OnTuple_Executes()
    {
        var source = """
            public static class Ext
            {
                extension((int A, int B) p)
                {
                    public int Sum => p.A + p.B;
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write((10, 20).Sum);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "30").VerifyDiagnostics();
    }

    [Fact]
    public void Property_NoCandidateInScope_ReportsNoSuchMember()
    {
        var source = """
            public class Goo
            {
                public static void M()
                {
                    _ = (1, 2).DoesNotExist;
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (5,20): error CS1061: '(int, int)' does not contain a definition for 'DoesNotExist' and no accessible extension method 'DoesNotExist' accepting a first argument of type '(int, int)' could be found (are you missing a using directive or an assembly reference?)
            //         _ = (1, 2).DoesNotExist;
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "DoesNotExist").WithArguments("(int, int)", "DoesNotExist").WithLocation(5, 20));
    }
}
