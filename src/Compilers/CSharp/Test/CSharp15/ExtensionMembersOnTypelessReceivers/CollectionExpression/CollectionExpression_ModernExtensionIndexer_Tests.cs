// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_CollectionExpression_ModernExtensionIndexer_Tests : CompilingTestBase
{
    // Modern extension indexers and the typeless-receivers feature don't currently meet:
    //
    //   - Instance indexers are not allowed inside `extension(T) { ... }` blocks: declaring
    //     `public int this[int i] { ... }` reports CS9282 / ERR_ExtensionDisallowsMember. So no
    //     instance indexer can be reached through the typeless-receiver path.
    //
    //   - Element access via `expr[args]` on a typeless receiver is explicitly out of scope per
    //     the proposal. The proposal is dot-form only (`expr.Member`), and `expr?.Member` /
    //     `expr[args]` continue to produce ERR_CollectionExpressionNoTargetType.
    //
    // The two tests below pin those two facts so that if either changes (instance indexers
    // become allowed inside extension blocks, or `[]` on typeless receivers is added to the
    // proposal scope), the failures here will surface and we can revisit.

    [Fact]
    public void InstanceIndexer_InExtensionBlock_NotAllowed()
    {
        var source = """
            public static class Ext
            {
                extension(int[] source)
                {
                    public int this[long i] { get => 0; }
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (5,20): error CS9282: This member is not allowed in an extension block
            //         public int this[long i] { get => 0; }
            Diagnostic(ErrorCode.ERR_ExtensionDisallowsMember, "this").WithLocation(5, 20));
    }

    [Fact]
    public void IndexerAccess_OnCollectionExpression_NoTargetType()
    {
        // `[1, 2, 3][0]` is element access on a typeless collection-expression receiver. The
        // proposal scopes the new behavior to dot-form member access only; element access
        // stays out of scope and continues to report ERR_CollectionExpressionNoTargetType.
        var source = """
            public class Goo
            {
                public static void M()
                {
                    _ = [1, 2, 3][0];
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (5,13): error CS9176: There is no target type for the collection expression.
            //         _ = [1, 2, 3][0];
            Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[1, 2, 3]").WithLocation(5, 13));
    }
}
