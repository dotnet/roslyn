// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_CollectionExpression_ModernExtensionMethod_Tests : CompilingTestBase
{
    // TODO: Modern (C# 14) extension members declared inside `extension(T) { ... }` blocks
    // are not yet wired up for typeless receivers. The classic-extension path runs through
    // BindInstanceMemberAccess, which TryBindMemberAccessOnTypelessReceiver routes to; the
    // modern path runs through GetExtensionMemberAccess and ExtensionLookup machinery that
    // does not yet honor a typeless receiver. Each test below documents the current
    // ERR_CollectionExpressionNoTargetType behavior so the file is green; once the modern
    // path is enabled (likely as part of the Phase 1.5 / modern-binder follow-up) these
    // tests should be flipped to assert successful binding.

    [Fact]
    public void Method_OnCollectionExpression_NotYetSupported()
    {
        var source = """
            using System.Collections.Generic;

            public static class Ext
            {
                extension(IEnumerable<int> source)
                {
                    public int Sum()
                    {
                        int s = 0;
                        foreach (var x in source) s += x;
                        return s;
                    }
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write([1, 2, 3].Sum());
                }
            }
            """;
        // TODO: should compile and print "6" once modern extensions are wired up.
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (20,30): error CS9176: There is no target type for the collection expression.
            //         System.Console.Write([1, 2, 3].Sum());
            Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[1, 2, 3]").WithLocation(20, 30));
    }

    [Fact]
    public void GenericMethod_OnCollectionExpression_NotYetSupported()
    {
        var source = """
            using System.Collections.Generic;

            public static class Ext
            {
                extension<T>(IEnumerable<T> source)
                {
                    public int CountIt()
                    {
                        int n = 0;
                        foreach (var _ in source) n++;
                        return n;
                    }
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write([1, 2, 3].CountIt());
                }
            }
            """;
        // TODO: should compile and print "3" once modern extensions are wired up.
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (20,30): error CS9176: There is no target type for the collection expression.
            //         System.Console.Write([1, 2, 3].CountIt());
            Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[1, 2, 3]").WithLocation(20, 30));
    }
}
