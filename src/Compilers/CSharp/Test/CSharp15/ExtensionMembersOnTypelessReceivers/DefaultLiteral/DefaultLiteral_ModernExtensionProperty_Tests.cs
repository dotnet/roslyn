// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_DefaultLiteral_ModernExtensionProperty_Tests : CompilingTestBase
{
    [Fact]
    public void Property_DefaultToInt_Executes()
    {
        var source = """
            public static class Ext
            {
                extension(int n)
                {
                    public int PlusOne => n + 1;
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write(default.PlusOne);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "1").VerifyDiagnostics();
    }

    [Fact]
    public void Property_NoCandidateInScope_FallsBackToDefaultLiteralNoTargetType()
    {
        // No extension is in scope. Helper returns null; legacy ERR_DefaultLiteralNoTargetType
        // fires.
        var source = """
            public class Goo
            {
                public static void M()
                {
                    _ = default.DoesNotExist;
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (5,13): error CS8716: There is no target type for the default literal.
            //         _ = default.DoesNotExist;
            Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(5, 13));
    }
}
