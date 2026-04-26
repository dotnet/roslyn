// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_Smoke_Tests : CompilingTestBase
{
    [Fact]
    public void CollectionExpression_ClassicExtensionMethod_Executes()
    {
        var source = """
            using System.Collections.Generic;

            public static class Ext
            {
                public static int CountIt<T>(this IEnumerable<T> source)
                {
                    int count = 0;
                    foreach (var _ in source) count++;
                    return count;
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
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "3").VerifyDiagnostics();
    }

    [Fact(Skip = "TODO: enable null-literal receiver in Phase 2 NullLiteral area PRs.")]
    public void NullLiteral_ClassicExtensionMethod_Executes()
    {
        var source = """
            public static class Ext
            {
                public static string OrEmpty(this string s) => s ?? "empty";
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write(null.OrEmpty());
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "empty").VerifyDiagnostics();
    }

    [Fact]
    public void Lambda_ClassicExtensionMethod_Executes()
    {
        var source = """
            using System;

            public static class Ext
            {
                public static int Apply(this Func<int, int> f, int arg) => f(arg);
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write((x => x * 2).Apply(5));
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "10").VerifyDiagnostics();
    }

    [Fact]
    public void ThrowExpression_RemainsError()
    {
        var source = """
            public static class Ext
            {
                public static int CountIt(this object obj) => 0;
            }

            public class Goo
            {
                public static void Main()
                {
                    _ = (throw new System.Exception()).CountIt();
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (10,14): error CS8115: A throw expression is not allowed in this context.
            Diagnostic(ErrorCode.ERR_ThrowMisplaced, "throw").WithLocation(10, 14));
    }

    [Fact]
    public void LangVersion_CSharp14_ReportsFeaturePreviewError()
    {
        var source = """
            using System.Collections.Generic;

            public static class Ext
            {
                public static int CountIt<T>(this IEnumerable<T> source) => 0;
            }

            public class Goo
            {
                public static void Main()
                {
                    _ = [1, 2, 3].CountIt();
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.Regular14).VerifyDiagnostics(
            // (12,13): error CS9202: Feature 'extension members on typeless receivers' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
            Diagnostic(ErrorCode.ERR_FeatureInPreview, "[1, 2, 3].CountIt").WithArguments("extension members on typeless receivers").WithLocation(12, 13));
    }
}
