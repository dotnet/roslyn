// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_Lambda_ModernExtensionProperty_Tests : CompilingTestBase
{
    [Fact]
    public void Property_OnFunc_Executes()
    {
        var source = """
            using System;

            public static class Ext
            {
                extension(Func<int, int> f)
                {
                    public int AppliedToFive => f(5);
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write(((int x) => x * 2).AppliedToFive);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "10").VerifyDiagnostics();
    }

    [Fact]
    public void GenericProperty_OnFunc_Executes()
    {
        var source = """
            using System;

            public static class Ext
            {
                extension<T>(Func<T, T> f)
                {
                    public T AppliedToDefault => f(default!);
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write(((int x) => x + 7).AppliedToDefault);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "7").VerifyDiagnostics();
    }

    [Fact]
    public void Property_NoCandidateInScope_FallsBackToBadUnaryOp()
    {
        // No extension property `Length` is in scope. The typeless-receiver feature only
        // engages when at least one extension candidate exists; without one, the helper
        // returns null and the legacy unbound-lambda rejection produces ERR_BadUnaryOp.
        var source = """
            public class Goo
            {
                public static void M()
                {
                    _ = (x => x).Length;
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (5,13): error CS0023: Operator '.' cannot be applied to operand of type 'lambda expression'
            //         _ = (x => x).Length;
            Diagnostic(ErrorCode.ERR_BadUnaryOp, "(x => x).Length").WithArguments(".", "lambda expression").WithLocation(5, 13));
    }
}
