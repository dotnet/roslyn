// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_Lambda_ModernExtensionIndexer_Tests : CompilingTestBase
{
    // Modern extension indexers and the typeless-receivers feature don't currently meet:
    //
    //   - Instance indexers are not allowed inside `extension(T) { ... }` blocks (CS9282
    //     ERR_ExtensionDisallowsMember). So no instance indexer can be reached.
    //
    //   - Element access via `expr[args]` on a typeless receiver is explicitly out of scope
    //     per the proposal: dot-form only.
    //
    // The two tests below pin those facts. Same pattern as
    // CollectionExpression_ModernExtensionIndexer_Tests.

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
    public void IndexerAccess_OnLambda_BadUnaryOp()
    {
        // Element access via `[]` on a lambda is not in scope of this proposal. The lambda
        // syntax also makes `(x => x)[0]` ambiguous to parse - here it parses as the lambda
        // itself indexed, which produces a binding-time error since lambda has no indexer.
        var source = """
            public class Goo
            {
                public static void M()
                {
                    _ = (x => x)[0];
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (5,13): error CS0021: Cannot apply indexing with [] to an expression of type 'lambda expression'
            //         _ = (x => x)[0];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "(x => x)[0]").WithArguments("lambda expression").WithLocation(5, 13));
    }
}
