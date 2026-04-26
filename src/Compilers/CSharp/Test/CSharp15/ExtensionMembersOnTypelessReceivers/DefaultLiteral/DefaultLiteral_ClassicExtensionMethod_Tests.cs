// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_DefaultLiteral_ClassicExtensionMethod_Tests : CompilingTestBase
{
    [Fact]
    public void DefaultToInt_Executes()
    {
        var source = """
            public static class Ext
            {
                public static int Plus(this int n, int m) => n + m;
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write(default.Plus(7));
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "7").VerifyDiagnostics();
    }

    [Fact]
    public void DefaultToString_Executes()
    {
        var source = """
            public static class Ext
            {
                public static string OrEmpty(this string s) => s ?? "empty";
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write(default.OrEmpty());
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "empty").VerifyDiagnostics();
    }

    [Fact]
    public void NoExtensionInScope_FallsBackToDefaultLiteralNoTargetType()
    {
        // No extension is in scope. The typeless-receiver feature only engages when at least
        // one extension candidate exists; without one, the helper returns null and the legacy
        // default-literal binding produces ERR_DefaultLiteralNoTargetType.
        var source = """
            public class Goo
            {
                public static void M()
                {
                    _ = default.DoesNotExist();
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (5,13): error CS8716: There is no target type for the default literal.
            //         _ = default.DoesNotExist();
            Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(5, 13));
    }
}
