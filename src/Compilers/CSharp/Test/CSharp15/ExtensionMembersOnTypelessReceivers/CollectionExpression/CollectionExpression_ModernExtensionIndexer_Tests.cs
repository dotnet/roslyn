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
    [Fact]
    public void Indexer_OnCollectionExpression_Executes()
    {
        var source = """
            public static class Ext
            {
                extension(int[] xs)
                {
                    public int this[long i] => xs[(int)i];
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write([10, 20, 30][1L]);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "20").VerifyDiagnostics();
    }

    [Fact]
    public void GenericIndexer_OnCollectionExpression_Executes()
    {
        var source = """
            using System.Collections.Generic;

            public static class Ext
            {
                extension<T>(IReadOnlyList<T> source)
                {
                    public T this[long i] => source[(int)i];
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write([10, 20, 30][1L]);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "20").VerifyDiagnostics();
    }

    [Fact]
    public void Indexer_TwoArgs_Executes()
    {
        var source = """
            public static class Ext
            {
                extension(int[] xs)
                {
                    public int this[int i, int offset] => xs[i] + offset;
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write([10, 20, 30][1, 5]);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "25").VerifyDiagnostics();
    }

    [Fact]
    public void Indexer_StringElements_Executes()
    {
        var source = """
            using System.Collections.Generic;

            public static class Ext
            {
                extension(IReadOnlyList<string> source)
                {
                    public string this[long i] => source[(int)i];
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write(["a", "b", "c"][2L]);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "c").VerifyDiagnostics();
    }

    [Fact]
    public void Indexer_ChainedAccess_Executes()
    {
        var source = """
            using System.Collections.Generic;

            public static class Ext
            {
                extension<T>(IReadOnlyList<T> source)
                {
                    public List<T> this[long start, long count]
                    {
                        get
                        {
                            var l = new List<T>();
                            for (int i = (int)start; i < start + count; i++) l.Add(source[i]);
                            return l;
                        }
                    }
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write([1, 2, 3, 4, 5][1L, 3L].Count);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "3").VerifyDiagnostics();
    }

    [Fact]
    public void Indexer_Spread_Executes()
    {
        var source = """
            public static class Ext
            {
                extension(int[] xs)
                {
                    public int this[long i] => xs[(int)i];
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    int[] a = [1, 2];
                    int[] b = [3, 4];
                    System.Console.Write([..a, ..b][2L]);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "3").VerifyDiagnostics();
    }

    [Fact]
    public void Indexer_NoCandidateInScope_FallsBackToCollectionExpressionNoTargetType()
    {
        // No extension indexer is in scope. The typeless-receiver feature only engages when at
        // least one extension indexer candidate exists; without one, the helper returns null and
        // the legacy `BindToNaturalType` path produces the pre-feature
        // ERR_CollectionExpressionNoTargetType.
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
