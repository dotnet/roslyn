// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_MethodGroup_ModernExtensionProperty_Tests : CompilingTestBase
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
                    public int AppliedToFour => f(4);
                }
            }

            public class Goo
            {
                public static int Inc(int x) => x + 1;

                public static void Main()
                {
                    System.Console.Write(Inc.AppliedToFour);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "5").VerifyDiagnostics();
    }

    // TODO: A generic-extension-property test (extension<T>(Func<T,T>) Property) accessed on a
    // method group hits a Debug.Fail in OverloadResolutionResult.TypeInferenceFailed -> non-
    // serializable diagnostic argument. Same root cause as the deferred empty-collection +
    // generic property test; tracked separately, outside this PR's scope.

    [Fact]
    public void Property_NoCandidateInScope_FallsBackToBadSKunknown()
    {
        // No extension property `Length` is in scope. The typeless-receiver feature only
        // engages when at least one extension candidate exists; without one, the helper
        // returns null and the legacy method-group binding produces ERR_BadSKunknown.
        var source = """
            public class Goo
            {
                public static int Square(int x) => x * x;
                public static void M()
                {
                    _ = Square.Length;
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (6,13): error CS0119: 'Goo.Square(int)' is a method, which is not valid in the given context
            //         _ = Square.Length;
            Diagnostic(ErrorCode.ERR_BadSKunknown, "Square").WithArguments("Goo.Square(int)", "method").WithLocation(6, 13));
    }
}
