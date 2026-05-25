// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_ThrowExpression_ClassicExtensionMethod_Tests : CompilingTestBase
{
    // Throw expressions are explicitly excluded from the typeless-receivers proposal: a throw
    // expression can never produce a value the extension would receive, so calling
    // (throw ...).M() is always unreachable. The compiler keeps reporting the existing
    // ERR_ThrowMisplaced where the throw is in a position that doesn't allow it.

    [Fact]
    public void ThrowAsReceiver_RejectedByParser()
    {
        var source = """
            public static class Ext
            {
                public static int M(this object o) => 0;
            }

            public class Goo
            {
                public static void M2()
                {
                    _ = (throw new System.Exception()).M();
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (10,14): error CS8115: A throw expression is not allowed in this context.
            //         _ = (throw new System.Exception()).M();
            Diagnostic(ErrorCode.ERR_ThrowMisplaced, "throw").WithLocation(10, 14));
    }
}
