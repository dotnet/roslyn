// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_CollectionExpression_ModernExtensionProperty_Tests : CompilingTestBase
{
    [Fact]
    public void Property_OnCollectionExpression_Executes()
    {
        var source = """
            using System.Collections.Generic;

            public static class Ext
            {
                extension(IEnumerable<int> source)
                {
                    public int First
                    {
                        get
                        {
                            foreach (var x in source) return x;
                            return 0;
                        }
                    }
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write([10, 20, 30].First);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "10").VerifyDiagnostics();
    }

    [Fact]
    public void GenericProperty_OnCollectionExpression_Executes()
    {
        var source = """
            using System.Collections.Generic;

            public static class Ext
            {
                extension<T>(IEnumerable<T> source)
                {
                    public int Count
                    {
                        get
                        {
                            int n = 0;
                            foreach (var _ in source) n++;
                            return n;
                        }
                    }
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write([1, 2, 3, 4].Count);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "4").VerifyDiagnostics();
    }

    [Fact]
    public void Property_OnReadOnlyList()
    {
        var source = """
            using System.Collections.Generic;

            public static class Ext
            {
                extension<T>(IReadOnlyList<T> source)
                {
                    public T Last => source[source.Count - 1];
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write([1, 2, 3].Last);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "3").VerifyDiagnostics();
    }

    [Fact]
    public void Property_OnArray()
    {
        var source = """
            public static class Ext
            {
                extension(int[] source)
                {
                    public int Sum
                    {
                        get
                        {
                            int s = 0;
                            foreach (var x in source) s += x;
                            return s;
                        }
                    }
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write([1, 2, 3, 4, 5].Sum);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "15").VerifyDiagnostics();
    }

    [Fact]
    public void Property_StringElements()
    {
        var source = """
            using System.Collections.Generic;

            public static class Ext
            {
                extension(IEnumerable<string> source)
                {
                    public string Joined
                    {
                        get
                        {
                            string r = "";
                            foreach (var s in source) r += s;
                            return r;
                        }
                    }
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write(["a", "b", "c"].Joined);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "abc").VerifyDiagnostics();
    }

    // TODO: A test for empty-collection + generic extension property (`_ = [].Count;` where
    // Count is an extension property on IEnumerable<T>) should fail with ERR_CantInferTypeArgs
    // or similar. Currently triggers a Debug.Fail in DiagnosticInfo.AssertMessageSerializable
    // inside OverloadResolutionResult.TypeInferenceFailed during property-resolution error
    // reporting, indicating a non-serializable argument. Tracked separately; outside this PR's
    // scope (the diagnostic-reporting bug also affects typed receivers in the same scenario).

    [Fact]
    public void Property_NoCandidateInScope_FallsBackToCollectionExpressionNoTargetType()
    {
        // No extension property `Length` is in scope. The typeless-receiver feature only
        // engages when at least one extension candidate exists; without one, the helper
        // returns null and the legacy `BindToNaturalType` path produces the pre-feature
        // ERR_CollectionExpressionNoTargetType.
        var source = """
            public class Goo
            {
                public static void M()
                {
                    _ = [1, 2, 3].Length;
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (5,13): error CS9176: There is no target type for the collection expression.
            //         _ = [1, 2, 3].Length;
            Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[1, 2, 3]").WithLocation(5, 13));
    }

    [Fact]
    public void Property_ChainedAccess_Executes()
    {
        // Property access returns a typed value; chained member access uses the existing typed
        // member-access path.
        var source = """
            using System.Collections.Generic;

            public static class Ext
            {
                extension<T>(IEnumerable<T> source)
                {
                    public List<T> AsList
                    {
                        get
                        {
                            var l = new List<T>();
                            foreach (var x in source) l.Add(x);
                            return l;
                        }
                    }
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write([1, 2, 3].AsList.Count);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "3").VerifyDiagnostics();
    }

    [Fact]
    public void Property_Spread_Executes()
    {
        var source = """
            using System.Collections.Generic;

            public static class Ext
            {
                extension(IEnumerable<int> source)
                {
                    public int Sum
                    {
                        get
                        {
                            int s = 0;
                            foreach (var x in source) s += x;
                            return s;
                        }
                    }
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    int[] a = [1, 2];
                    int[] b = [3, 4];
                    System.Console.Write([..a, ..b].Sum);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "10").VerifyDiagnostics();
    }
}
