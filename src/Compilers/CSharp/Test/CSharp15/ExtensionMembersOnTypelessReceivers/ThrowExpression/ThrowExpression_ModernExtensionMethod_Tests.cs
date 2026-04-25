// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_ThrowExpression_ModernExtensionMethod_Tests : CompilingTestBase
{
    // Same pin as the classic shape: throw expressions are excluded from the proposal.

    [Fact]
    public void ThrowAsReceiver_RejectedByParser()
    {
        var source = """
            public static class Ext
            {
                extension(object o)
                {
                    public int M() => 0;
                }
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
            // (13,14): error CS8115: A throw expression is not allowed in this context.
            //         _ = (throw new System.Exception()).M();
            Diagnostic(ErrorCode.ERR_ThrowMisplaced, "throw").WithLocation(13, 14));
    }
}
