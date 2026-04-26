// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_CollectionExpression_ClassicExtensionMethod_Tests : CompilingTestBase
{
    #region Positive: target type variety

    [Fact]
    public void Array_Executes()
    {
        var source = """
            public static class Ext
            {
                public static int Sum(this int[] xs)
                {
                    int s = 0;
                    foreach (var x in xs) s += x;
                    return s;
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write([1, 2, 3, 4].Sum());
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "10").VerifyDiagnostics();
    }

    [Fact]
    public void List_Executes()
    {
        var source = """
            using System.Collections.Generic;

            public static class Ext
            {
                public static int Count(this List<int> xs) => xs.Count;
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write([1, 2, 3].Count());
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "3").VerifyDiagnostics();
    }

    [Fact]
    public void IEnumerable_GenericInference_Executes()
    {
        var source = """
            using System.Collections.Generic;

            public static class Ext
            {
                public static T First<T>(this IEnumerable<T> xs)
                {
                    foreach (var x in xs) return x;
                    return default!;
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write([10, 20, 30].First());
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "10").VerifyDiagnostics();
    }

    [Fact]
    public void IReadOnlyList_GenericInference()
    {
        var source = """
            using System.Collections.Generic;

            public static class Ext
            {
                public static T Last<T>(this IReadOnlyList<T> xs) => xs[xs.Count - 1];
            }

            public class Goo
            {
                public static void Main()
                {
                    _ = [1, 2, 3].Last();
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }

    #endregion

    #region Edge cases: collection-expression-specific

    [Fact]
    public void EmptyCollection_GenericInference_CannotInfer()
    {
        // T cannot be inferred from `[]` because there are no elements.
        var source = """
            using System.Collections.Generic;

            public static class Ext
            {
                public static int Count<T>(this IEnumerable<T> xs)
                {
                    int n = 0;
                    foreach (var _ in xs) n++;
                    return n;
                }
            }

            public class Goo
            {
                public static void M()
                {
                    _ = [].Count();
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (17,16): error CS0411: The type arguments for method 'Ext.Count<T>(IEnumerable<T>)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
            //         _ = [].Count();
            Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Count").WithArguments("Ext.Count<T>(System.Collections.Generic.IEnumerable<T>)").WithLocation(17, 16));
    }

    [Fact]
    public void Spread_Executes()
    {
        var source = """
            using System.Collections.Generic;

            public static class Ext
            {
                public static int Sum(this IEnumerable<int> xs)
                {
                    int s = 0;
                    foreach (var x in xs) s += x;
                    return s;
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    int[] a = [1, 2];
                    int[] b = [3, 4];
                    System.Console.Write([..a, ..b].Sum());
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "10").VerifyDiagnostics();
    }

    [Fact]
    public void NestedCollection_Executes()
    {
        var source = """
            using System.Collections.Generic;

            public static class Ext
            {
                public static int CountOuter<T>(this IEnumerable<T> xs)
                {
                    int n = 0;
                    foreach (var _ in xs) n++;
                    return n;
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write([(int[])[1, 2], (int[])[3, 4, 5]].CountOuter());
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "2").VerifyDiagnostics();
    }

    [Fact]
    public void StringElements_Executes()
    {
        // The element type is non-trivial to confirm inference works for reference element types.
        var source = """
            using System.Collections.Generic;

            public static class Ext
            {
                public static string Concat(this IEnumerable<string> xs)
                {
                    string r = "";
                    foreach (var x in xs) r += x;
                    return r;
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write(["a", "b", "c"].Concat());
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "abc").VerifyDiagnostics();
    }

    #endregion

    #region Generic inference

    [Fact]
    public void TwoParamGeneric_InferredFromReceiverAndArg()
    {
        var source = """
            using System.Collections.Generic;

            public static class Ext
            {
                public static (T, U) Pair<T, U>(this IEnumerable<T> xs, U other) => (xs.GetEnumerator().Current, other);
            }

            public class Goo
            {
                public static void M()
                {
                    var p = [1, 2, 3].Pair("hi");
                    _ = p;
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }

    [Fact]
    public void ConstrainedGeneric_StructConstraint()
    {
        var source = """
            using System.Collections.Generic;

            public static class Ext
            {
                public static T First<T>(this IEnumerable<T> xs) where T : struct
                {
                    foreach (var x in xs) return x;
                    return default;
                }
            }

            public class Goo
            {
                public static void M()
                {
                    _ = [1, 2, 3].First();
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }

    [Fact]
    public void ConstrainedGeneric_StructConstraintViolation()
    {
        var source = """
            using System.Collections.Generic;

            public static class Ext
            {
                public static T First<T>(this IEnumerable<T> xs) where T : struct
                {
                    foreach (var x in xs) return x;
                    return default;
                }
            }

            public class Goo
            {
                public static void M()
                {
                    _ = ["a", "b"].First();
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (16,24): error CS0453: The type 'string' must be a non-nullable value type in order to use it as parameter 'T' in the generic type or method 'Ext.First<T>(IEnumerable<T>)'
            //         _ = ["a", "b"].First();
            Diagnostic(ErrorCode.ERR_ValConstraintNotSatisfied, "First").WithArguments("Ext.First<T>(System.Collections.Generic.IEnumerable<T>)", "T", "string").WithLocation(16, 24));
    }

    #endregion

    #region Negative

    [Fact]
    public void ReceiverTypeNotConstructibleFromCollectionExpression()
    {
        // Extension's first parameter is a class type that has no collection-expression
        // conversion; the candidate is reported as not constructible.
        var source = """
            public class Bag { }

            public static class Ext
            {
                public static int Count(this Bag b) => 0;
            }

            public class Goo
            {
                public static void M()
                {
                    _ = [1, 2, 3].Count();
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (12,13): error CS9174: Cannot initialize type 'Bag' with a collection expression because the type is not constructible.
            //         _ = [1, 2, 3].Count();
            Diagnostic(ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible, "[1, 2, 3]").WithArguments("Bag").WithLocation(12, 13));
    }

    [Fact]
    public void NoExtensionInScope_FallsBackToCollectionExpressionNoTargetType()
    {
        // No extension method named Count is in scope. The typeless-receiver feature only
        // engages when at least one extension candidate exists; without one, the helper
        // returns null and the legacy `BindToNaturalType` path produces the pre-feature
        // ERR_CollectionExpressionNoTargetType. This avoids a misleading "feature is in
        // Preview" diagnostic for a plain typo where no extension would have applied either.
        var source = """
            public class Goo
            {
                public static void M()
                {
                    _ = [1, 2, 3].Count();
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (5,13): error CS9176: There is no target type for the collection expression.
            //         _ = [1, 2, 3].Count();
            Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[1, 2, 3]").WithLocation(5, 13));
    }

    [Fact]
    public void Ambiguous_BothApplicable()
    {
        // Two applicable candidates whose first parameter types are equally specific
        // for the collection-expression source produce overload-resolution ambiguity.
        var source = """
            using System.Collections.Generic;

            public static class ExtA
            {
                public static int M(this IEnumerable<int> xs) => 1;
            }
            public static class ExtB
            {
                public static int M(this IEnumerable<int> xs) => 2;
            }

            public class Goo
            {
                public static void Main()
                {
                    _ = [1, 2, 3].M();
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (16,23): error CS0121: The call is ambiguous between the following methods or properties: 'ExtA.M(IEnumerable<int>)' and 'ExtB.M(IEnumerable<int>)'
            //         _ = [1, 2, 3].M();
            Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("ExtA.M(System.Collections.Generic.IEnumerable<int>)", "ExtB.M(System.Collections.Generic.IEnumerable<int>)").WithLocation(16, 23));
    }

    [Fact]
    public void HasErroredElement_ProducesOnlyInnerError()
    {
        // Collection expression with an undeclared element causes the receiver to bind with
        // HasErrors=true. TryBindMemberAccessOnTypelessReceiver takes ownership of the errored
        // case and returns a BoundBadExpression, so the user sees only the inner "name does
        // not exist" error - no cascading "no target type" diagnostic on top.
        var source = """
            using System.Collections.Generic;

            public static class Ext
            {
                public static int CountIt<T>(this IEnumerable<T> xs) => 0;
            }

            public class Goo
            {
                public static void M()
                {
                    _ = [1, undecl].CountIt();
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (12,17): error CS0103: The name 'undecl' does not exist in the current context
            //         _ = [1, undecl].CountIt();
            Diagnostic(ErrorCode.ERR_NameNotInContext, "undecl").WithArguments("undecl").WithLocation(12, 17));
    }

    [Fact]
    public void OverloadResolution_PrefersMoreSpecific()
    {
        // T[] is more specific than IEnumerable<T>. Both are applicable for [1,2,3];
        // overload resolution picks T[].
        var source = """
            using System.Collections.Generic;

            public static class Ext
            {
                public static string Tag(this IEnumerable<int> xs) => "enumerable";
                public static string Tag(this int[] xs) => "array";
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write([1, 2, 3].Tag());
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "array").VerifyDiagnostics();
    }

    #endregion

    #region Chained extension calls

    [Fact]
    public void Chained_CollectionExprThenTypedReceiver_Executes()
    {
        // First call uses the typeless-receiver path; result has a type so the
        // second call uses the existing typed-receiver path.
        var source = """
            using System.Collections.Generic;

            public static class Ext
            {
                public static List<T> ToList<T>(this IEnumerable<T> xs)
                {
                    var r = new List<T>();
                    foreach (var x in xs) r.Add(x);
                    return r;
                }

                public static int Sum(this List<int> xs)
                {
                    int s = 0;
                    foreach (var x in xs) s += x;
                    return s;
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write([1, 2, 3].ToList().Sum());
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "6").VerifyDiagnostics();
    }

    #endregion

    #region Argument forwarding

    [Fact]
    public void AdditionalArguments_Forwarded()
    {
        var source = """
            using System.Collections.Generic;

            public static class Ext
            {
                public static int CountAtLeast<T>(this IEnumerable<T> xs, int min)
                {
                    int n = 0;
                    foreach (var _ in xs)
                    {
                        n++;
                        if (n >= min) return n;
                    }
                    return n;
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write([1, 2, 3, 4].CountAtLeast(2));
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "2").VerifyDiagnostics();
    }

    [Fact]
    public void ParamsAdditionalArgs_Executes()
    {
        // params on the additional argument list expands as expected when the receiver is a
        // typeless collection expression.
        var source = """
            using System.Collections.Generic;

            public static class Ext
            {
                public static int Combine<T>(this IEnumerable<T> xs, params int[] extras)
                {
                    int s = 0;
                    foreach (var _ in xs) s++;
                    foreach (var x in extras) s += x;
                    return s;
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write([1, 2, 3].Combine(10, 20, 30));
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "63").VerifyDiagnostics();
    }

    [Fact]
    public void NamedArguments_OnAdditionalArgs()
    {
        var source = """
            using System.Collections.Generic;

            public static class Ext
            {
                public static int Combine<T>(this IEnumerable<T> xs, int a, int b)
                {
                    int s = a + b;
                    foreach (var _ in xs) s++;
                    return s;
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write([1, 2, 3].Combine(b: 100, a: 10));
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "113").VerifyDiagnostics();
    }

    #endregion

    #region Attribute interactions

    [Fact]
    public void ObsoleteExtensionMethod_OnTypelessReceiver_ReportsObsolete()
    {
        // [Obsolete] on an extension method called via a typeless collection-expression
        // receiver still produces the obsolete diagnostic at the call site.
        var source = """
            using System;
            using System.Collections.Generic;

            public static class Ext
            {
                [Obsolete("don't use", error: true)]
                public static int CountIt<T>(this IEnumerable<T> xs) => 0;
            }

            public class Goo
            {
                public static void M()
                {
                    _ = [1, 2, 3].CountIt();
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (14,13): error CS0619: 'Ext.CountIt<T>(IEnumerable<T>)' is obsolete: 'don't use'
            //         _ = [1, 2, 3].CountIt();
            Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "[1, 2, 3].CountIt()").WithArguments("Ext.CountIt<T>(System.Collections.Generic.IEnumerable<T>)", "don't use").WithLocation(14, 13));
    }

    [Fact]
    public void OverloadResolutionPriority_OnTypelessReceiver_PicksHigherPriority()
    {
        // Two equally-applicable extension candidates differ only in
        // [OverloadResolutionPriority]. The higher-priority candidate is selected for the
        // typeless-collection-expression receiver, just as it would be for a typed receiver.
        var source = """
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;

            public static class Ext
            {
                [OverloadResolutionPriority(1)]
                public static string Tag(this IEnumerable<int> xs) => "high";

                public static string Tag(this int[] xs) => "low";
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write([1, 2, 3].Tag());
                }
            }
            """;
        CompileAndVerify([source, OverloadResolutionPriorityAttributeDefinition], parseOptions: TestOptions.RegularPreview, expectedOutput: "high").VerifyDiagnostics();
    }

    [Fact]
    public void DynamicArgument_OnTypelessReceiver_ReportsBadArgTypeDynamic()
    {
        // Extension methods cannot be dynamically dispatched. With a typeless receiver, the
        // diagnostic for that situation (CS1973) used to assert that the method group's
        // ReceiverOpt had a non-null Type - which is never true for a typeless receiver. This
        // test pins that the call now produces a clean ERR_BadArgTypeDynamicExtension instead
        // of tripping the assert under debug.
        var source = """
            using System.Collections.Generic;

            public static class Ext
            {
                public static int CombineWith<T>(this IEnumerable<T> xs, object o) => o.GetHashCode();
            }

            public class Goo
            {
                public static void M()
                {
                    dynamic d = 7;
                    _ = [1, 2, 3].CombineWith(d);
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview, references: new[] { CSharpRef }).VerifyDiagnostics(
            // (13,13): error CS1973: 'collection expression' has no applicable method named 'CombineWith' but appears to have an extension method by that name. Extension methods cannot be dynamically dispatched. Consider casting the dynamic arguments or calling the extension method without the extension method syntax.
            //         _ = [1, 2, 3].CombineWith(d);
            Diagnostic(ErrorCode.ERR_BadArgTypeDynamicExtension, "[1, 2, 3].CombineWith(d)").WithArguments("collection expression", "CombineWith").WithLocation(13, 13),
            // (13,13): error CS9176: There is no target type for the collection expression.
            //         _ = [1, 2, 3].CombineWith(d);
            Diagnostic(ErrorCode.ERR_CollectionExpressionNoTargetType, "[1, 2, 3]").WithLocation(13, 13));
    }

    #endregion
}
