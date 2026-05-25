// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_TupleExpression_ModernExtensionIndexer_Tests : CompilingTestBase
{
    [Fact]
    public void InstanceIndexer_InExtensionBlock_NotAllowed()
    {
        var source = """
            public static class Ext
            {
                extension((int A, int B) p)
                {
                    public int this[int i] { get => p.A + p.B + i; }
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (5,20): error CS9282: This member is not allowed in an extension block
            //         public int this[int i] { get => p.A + p.B + i; }
            Diagnostic(ErrorCode.ERR_ExtensionDisallowsMember, "this").WithLocation(5, 20));
    }

    [Fact]
    public void IndexerAccess_OnTuple_DoesNotBind()
    {
        var source = """
            public class Goo
            {
                public static void M()
                {
                    _ = (1, 2)[0];
                }
            }
            """;
        var c = CreateCompilation(source, parseOptions: TestOptions.RegularPreview);
        var diags = c.GetDiagnostics();
        Assert.NotEmpty(diags);
    }
}
