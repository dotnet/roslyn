// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_MethodGroup_ModernExtensionIndexer_Tests : CompilingTestBase
{
    // Modern extension indexers and the typeless-receivers feature don't currently meet:
    //
    //   - Instance indexers are not allowed inside `extension(T) { ... }` blocks (CS9282
    //     ERR_ExtensionDisallowsMember).
    //
    //   - Element access via `expr[args]` on a typeless receiver is explicitly out of scope
    //     per the proposal: dot-form only.
    //
    // Same pattern as CollectionExpression / Lambda area indexer pin tests.

    [Fact]
    public void InstanceIndexer_InExtensionBlock_OnFunc_NotAllowed()
    {
        var source = """
            using System;

            public static class Ext
            {
                extension(Func<int, int> f)
                {
                    public int this[int i] { get => f(i); }
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (7,20): error CS9282: This member is not allowed in an extension block
            //         public int this[int i] { get => f(i); }
            Diagnostic(ErrorCode.ERR_ExtensionDisallowsMember, "this").WithLocation(7, 20));
    }

    [Fact]
    public void IndexerAccess_OnMethodGroup_BadIndexLHS()
    {
        var source = """
            public class Goo
            {
                public static int Square(int x) => x * x;
                public static void M()
                {
                    _ = Square[0];
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (6,13): error CS0021: Cannot apply indexing with [] to an expression of type 'method group'
            //         _ = Square[0];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "Square[0]").WithArguments("method group").WithLocation(6, 13));
    }
}
