// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_ConditionalExpression_ModernExtensionIndexer_Tests : CompilingTestBase
{
    [Fact]
    public void InstanceIndexer_InExtensionBlock_OnNullable_NotAllowed()
    {
        var source = """
            public static class Ext
            {
                extension(int? n)
                {
                    public int this[int i] { get => (n ?? 0) + i; }
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (5,20): error CS9282: This member is not allowed in an extension block
            //         public int this[int i] { get => (n ?? 0) + i; }
            Diagnostic(ErrorCode.ERR_ExtensionDisallowsMember, "this").WithLocation(5, 20));
    }

    [Fact]
    public void IndexerAccess_OnConditional_BadIndex()
    {
        var source = """
            public class Goo
            {
                public static void M()
                {
                    bool b = true;
                    _ = (b ? null : 5)[0];
                }
            }
            """;
        var c = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
        // Pin: `[]` element access on a typeless conditional is out of scope per the proposal.
        // Whatever diagnostic the compiler currently reports here, this test confirms that the
        // expression does not bind successfully.
        var diags = c.GetDiagnostics();
        Assert.NotEmpty(diags);
    }
}
