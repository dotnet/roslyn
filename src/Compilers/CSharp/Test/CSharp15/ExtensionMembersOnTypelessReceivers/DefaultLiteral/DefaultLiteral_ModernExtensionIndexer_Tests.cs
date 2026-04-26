// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_DefaultLiteral_ModernExtensionIndexer_Tests : CompilingTestBase
{
    // Same pattern as previous indexer pin PRs.

    [Fact]
    public void InstanceIndexer_InExtensionBlock_OnInt_NotAllowed()
    {
        var source = """
            public static class Ext
            {
                extension(int n)
                {
                    public int this[int i] { get => n + i; }
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (5,20): error CS9282: This member is not allowed in an extension block
            //         public int this[int i] { get => n + i; }
            Diagnostic(ErrorCode.ERR_ExtensionDisallowsMember, "this").WithLocation(5, 20));
    }

    [Fact]
    public void IndexerAccess_OnDefault_NoTargetType()
    {
        var source = """
            public class Goo
            {
                public static void M()
                {
                    _ = default[0];
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (5,13): error CS8716: There is no target type for the default literal.
            //         _ = default[0];
            Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(5, 13));
    }
}
