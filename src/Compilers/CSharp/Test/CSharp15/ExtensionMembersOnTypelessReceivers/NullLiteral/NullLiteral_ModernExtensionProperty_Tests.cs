// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_NullLiteral_ModernExtensionProperty_Tests : CompilingTestBase
{
    [Fact]
    public void Property_NullToString_Executes()
    {
        var source = """
            public static class Ext
            {
                extension(string s)
                {
                    public string OrEmpty => s ?? "empty";
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write(null.OrEmpty);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "empty").VerifyDiagnostics();
    }

    [Fact]
    public void Property_NoCandidateInScope_FallsBackToBadUnaryOp()
    {
        // No extension is in scope. Helper returns null; legacy "operator '.' cannot be applied
        // to <null>" diagnostic fires.
        var source = """
            public class Goo
            {
                public static void M()
                {
                    _ = null.DoesNotExist;
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (5,13): error CS0023: Operator '.' cannot be applied to operand of type '<null>'
            //         _ = null.DoesNotExist;
            Diagnostic(ErrorCode.ERR_BadUnaryOp, "null.DoesNotExist").WithArguments(".", "<null>").WithLocation(5, 13));
    }
}
