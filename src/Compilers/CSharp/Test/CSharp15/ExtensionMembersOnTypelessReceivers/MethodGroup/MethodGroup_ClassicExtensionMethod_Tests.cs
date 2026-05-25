// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_MethodGroup_ClassicExtensionMethod_Tests : CompilingTestBase
{
    [Fact]
    public void StaticMethodGroup_FuncTarget_Executes()
    {
        // The headline scenario from the proposal: SomeMethod.Memoize() / SomeMethod.RunIt(...)
        // The receiver is a method group; the extension applies to a delegate type.
        var source = """
            using System;

            public static class Ext
            {
                public static int RunIt(this Func<int, int> f, int arg) => f(arg);
            }

            public class Goo
            {
                public static int Square(int x) => x * x;

                public static void Main()
                {
                    System.Console.Write(Square.RunIt(5));
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "25").VerifyDiagnostics();
    }

    [Fact]
    public void InstanceMethodGroup_FuncTarget_Executes()
    {
        var source = """
            using System;

            public static class Ext
            {
                public static int RunIt(this Func<int, int> f, int arg) => f(arg);
            }

            public class Goo
            {
                public int Inc(int x) => x + 1;

                public static void Main()
                {
                    var g = new Goo();
                    System.Console.Write(g.Inc.RunIt(10));
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "11").VerifyDiagnostics();
    }

    [Fact]
    public void GenericExtension_OnFunc_Executes()
    {
        var source = """
            using System;

            public static class Ext
            {
                public static T Apply<T>(this Func<T, T> f, T arg) => f(arg);
            }

            public class Goo
            {
                public static int Triple(int x) => x * 3;

                public static void Main()
                {
                    System.Console.Write(Triple.Apply(7));
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "21").VerifyDiagnostics();
    }

    [Fact]
    public void Memoize_Executes()
    {
        var source = """
            using System;
            using System.Collections.Generic;

            public static class Ext
            {
                public static Func<int, int> Memoize(this Func<int, int> f)
                {
                    var cache = new Dictionary<int, int>();
                    return x => cache.TryGetValue(x, out var v) ? v : (cache[x] = f(x));
                }
            }

            public class Goo
            {
                public static int Square(int x) => x * x;

                public static void Main()
                {
                    var memoized = Square.Memoize();
                    System.Console.Write(memoized(6));
                    System.Console.Write(memoized(6));
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "3636").VerifyDiagnostics();
    }

    [Fact]
    public void OverloadedMethodGroup_AmbiguousNaturalType_RoutesThroughTypelessPath()
    {
        // When the method group is overloaded, it has no natural type. Without the typeless-
        // receiver feature, this would produce no candidate. With the feature, the lambda is
        // target-typed against the extension's first parameter type and binds correctly.
        var source = """
            using System;

            public static class Ext
            {
                public static int RunInt(this Func<int, int> f, int arg) => f(arg);
            }

            public class Goo
            {
                public static int Identity(int x) => x;
                public static string Identity(string s) => s;

                public static void Main()
                {
                    // Identity is overloaded; group has no natural type. Extension takes
                    // Func<int,int> so target-typing picks the int overload.
                    System.Console.Write(Identity.RunInt(42));
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "42").VerifyDiagnostics();
    }

    [Fact]
    public void NoExtensionInScope_FallsBackToBadSKunknown()
    {
        // No extension method named DoesNotExist is in scope. The typeless-receiver feature
        // only engages when at least one extension candidate exists; without one, the helper
        // returns null and the legacy method-group binding produces ERR_BadSKunknown
        // ("'method' is not valid in the given context").
        var source = """
            using System;

            public static class Ext
            {
                public static int RunIt(this Func<int, int> f, int arg) => f(arg);
            }

            public class Goo
            {
                public static int Square(int x) => x * x;

                public static void M()
                {
                    _ = Square.DoesNotExist();
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (14,13): error CS0119: 'Goo.Square(int)' is a method, which is not valid in the given context
            //         _ = Square.DoesNotExist();
            Diagnostic(ErrorCode.ERR_BadSKunknown, "Square").WithArguments("Goo.Square(int)", "method").WithLocation(14, 13));
    }

    [Fact]
    public void InaccessibleMethodGroup_DoesNotEnterTypelessPath()
    {
        // The method-group lookup finds a private method whose accessibility check fails;
        // ResultKind is Inaccessible, so IsValidMethodGroupReceiver rejects the receiver and
        // we fall back to the existing diagnostic about accessibility instead of routing
        // through the new feature.
        var source = """
            using System;

            public static class Ext
            {
                public static int RunIt(this Func<int, int> f, int arg) => f(arg);
            }

            public class Other
            {
                private static int Square(int x) => x * x;
            }

            public class Goo
            {
                public static void M()
                {
                    _ = Other.Square.RunIt(5);
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (17,19): error CS0122: 'Other.Square(int)' is inaccessible due to its protection level
            //         _ = Other.Square.RunIt(5);
            Diagnostic(ErrorCode.ERR_BadAccess, "Square").WithArguments("Other.Square(int)").WithLocation(17, 19));
    }

    [Fact]
    public void IteratorExtension_OnMethodGroupReceiver_Executes()
    {
        // An iterator extension method (returning IEnumerable<int> via yield) on a typeless
        // method-group receiver enumerates as expected.
        var source = """
            using System;
            using System.Collections.Generic;

            public static class Ext
            {
                public static IEnumerable<int> Take3(this Func<int, int> f)
                {
                    yield return f(0);
                    yield return f(1);
                    yield return f(2);
                }
            }

            public class Goo
            {
                public static int Square(int x) => x * x;

                public static void Main()
                {
                    int s = 0;
                    foreach (var v in Square.Take3()) s += v;
                    System.Console.Write(s);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "5").VerifyDiagnostics();
    }

}
