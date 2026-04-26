// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_NullLiteral_ModernExtensionIndexer_Tests : CompilingTestBase
{
    // Same pattern as previous indexer pin PRs: instance indexers in extension(T) blocks
    // aren't allowed (CS9282), and `[]` on a typeless receiver is out of scope per the proposal.

    [Fact]
    public void InstanceIndexer_InExtensionBlock_OnString_NotAllowed()
    {
        var source = """
            public static class Ext
            {
                extension(string s)
                {
                    public char this[int i] { get => s[i]; }
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (5,21): error CS9282: This member is not allowed in an extension block
            //         public char this[int i] { get => s[i]; }
            Diagnostic(ErrorCode.ERR_ExtensionDisallowsMember, "this").WithLocation(5, 21));
    }

    [Fact]
    public void IndexerAccess_OnNull_BadIndexLHS()
    {
        var source = """
            public class Goo
            {
                public static void M()
                {
                    _ = null[0];
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (5,13): error CS0021: Cannot apply indexing with [] to an expression of type '<null>'
            //         _ = null[0];
            Diagnostic(ErrorCode.ERR_BadIndexLHS, "null[0]").WithArguments("<null>").WithLocation(5, 13));
    }
}
