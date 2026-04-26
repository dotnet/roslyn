// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_NewExpression_ModernExtensionIndexer_Tests : CompilingTestBase
{
    // Same pattern as the CollectionExpression / Lambda / MethodGroup indexer pin PRs:
    // instance indexers in extension(T) blocks aren't allowed (CS9282), and `[]` on a typeless
    // receiver is out of scope per the proposal.

    [Fact]
    public void InstanceIndexer_InExtensionBlock_NotAllowed()
    {
        var source = """
            public class Bag { public int N; }
            public static class Ext
            {
                extension(Bag b)
                {
                    public int this[int i] { get => b.N + i; }
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (6,20): error CS9282: This member is not allowed in an extension block
            //         public int this[int i] { get => b.N + i; }
            Diagnostic(ErrorCode.ERR_ExtensionDisallowsMember, "this").WithLocation(6, 20));
    }

    [Fact]
    public void IndexerAccess_OnNew_ReportsBadIndexLHS()
    {
        // `[]` on `new()` doesn't enter the typeless extension path. Without a target type,
        // `new()` reports the existing diagnostic.
        var source = """
            public class Goo
            {
                public static void M()
                {
                    _ = new()[0];
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (5,13): error CS8754: There is no target type for 'new()'
            //         _ = new()[0];
            Diagnostic(ErrorCode.ERR_ImplicitObjectCreationNoTargetType, "new()").WithArguments("new()").WithLocation(5, 13));
    }
}
