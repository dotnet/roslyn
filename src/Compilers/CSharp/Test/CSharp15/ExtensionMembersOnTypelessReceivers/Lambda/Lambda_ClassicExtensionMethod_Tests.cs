// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;

[CompilerTrait(CompilerFeature.Extensions)]
public sealed class ExtensionMembersOnTypelessReceivers_Lambda_ClassicExtensionMethod_Tests : CompilingTestBase
{
    [Fact]
    public void FuncTarget_Executes()
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
    public void ActionTarget_Executes()
    {
        var source = """
            using System;

            public static class Ext
            {
                public static void RunIt(this Action a) { a(); a(); }
            }

            public class Goo
            {
                public static void Main()
                {
                    int n = 0;
                    (() => n++).RunIt();
                    System.Console.Write(n);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "2").VerifyDiagnostics();
    }

    [Fact]
    public void TwoParamLambda_Executes()
    {
        var source = """
            using System;

            public static class Ext
            {
                public static int Combine(this Func<int, int, int> f, int a, int b) => f(a, b);
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write(((a, b) => a + b).Combine(3, 4));
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "7").VerifyDiagnostics();
    }

    [Fact]
    public void TypedParamLambda_StillTypeless_Executes()
    {
        // Even with an explicit parameter type, the lambda has no natural type and routes
        // through the typeless-receiver path until the extension binds it to a delegate type.
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
                    System.Console.Write(((int x) => x + 1).Apply(10));
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "11").VerifyDiagnostics();
    }

    [Fact]
    public void GenericExtension_LambdaToDelegate()
    {
        var source = """
            using System;

            public static class Ext
            {
                public static T Apply<T>(this Func<T, T> f, T arg) => f(arg);
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write(((int x) => x * 3).Apply(4));
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "12").VerifyDiagnostics();
    }

    [Fact]
    public void NoExtensionInScope_FallsBackToBadUnaryOp()
    {
        // No extension method named DoesNotExist is in scope. The typeless-receiver feature
        // only engages when at least one extension candidate exists; without one, the helper
        // returns null and the legacy unbound-lambda rejection in BindMemberAccessWithBoundLeft
        // produces the pre-feature ERR_BadUnaryOp.
        var source = """
            public class Goo
            {
                public static void M()
                {
                    _ = (x => x).DoesNotExist();
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (5,13): error CS0023: Operator '.' cannot be applied to operand of type 'lambda expression'
            //         _ = (x => x).DoesNotExist();
            Diagnostic(ErrorCode.ERR_BadUnaryOp, "(x => x).DoesNotExist").WithArguments(".", "lambda expression").WithLocation(5, 13));
    }

    [Fact]
    public void Ambiguous_BothApplicable()
    {
        var source = """
            using System;

            public static class ExtA
            {
                public static int M(this Func<int, int> f) => 1;
            }
            public static class ExtB
            {
                public static int M(this Func<int, int> f) => 2;
            }

            public class Goo
            {
                public static void Main()
                {
                    _ = (x => x).M();
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(
            // (16,22): error CS0121: The call is ambiguous between the following methods or properties: 'ExtA.M(Func<int, int>)' and 'ExtB.M(Func<int, int>)'
            //         _ = (x => x).M();
            Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("ExtA.M(System.Func<int, int>)", "ExtB.M(System.Func<int, int>)").WithLocation(16, 22));
    }

    [Fact]
    public void OverloadResolution_PrefersMoreSpecific()
    {
        var source = """
            using System;

            public static class Ext
            {
                public static string Tag(this Delegate d) => "delegate";
                public static string Tag(this Func<int, int> f) => "func";
            }

            public class Goo
            {
                public static void Main()
                {
                    System.Console.Write(((int x) => x).Tag());
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "func").VerifyDiagnostics();
    }

    [Fact]
    public void Chained_LambdaThenTypedReceiver_Executes()
    {
        var source = """
            using System;

            public static class Ext
            {
                public static Func<int, int> Memoize(this Func<int, int> f)
                {
                    var cache = new System.Collections.Generic.Dictionary<int, int>();
                    return x => cache.TryGetValue(x, out var v) ? v : (cache[x] = f(x));
                }
            }

            public class Goo
            {
                public static void Main()
                {
                    var memoized = ((int x) => x * x).Memoize();
                    System.Console.Write(memoized(5));
                    System.Console.Write(memoized(5));
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "2525").VerifyDiagnostics();
    }

    [Fact]
    public void AsyncExtension_OnLambdaReceiver_Executes()
    {
        // An async extension method (returning Task<int>) on a typeless lambda receiver
        // executes correctly: the lambda is the receiver, and the extension awaits a value
        // computed from invoking it.
        var source = """
            using System;
            using System.Threading.Tasks;

            public static class Ext
            {
                public static async Task<int> ApplyAsync(this Func<int, int> f, int arg)
                {
                    await Task.Yield();
                    return f(arg);
                }
            }

            public class Goo
            {
                public static async Task Main()
                {
                    int r = await ((int x) => x + 1).ApplyAsync(41);
                    System.Console.Write(r);
                }
            }
            """;
        CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "42").VerifyDiagnostics();
    }

    [Fact]
    public void TypelessLambdaReceiver_InsideExpressionTree_Compiles()
    {
        // A typeless lambda receiver participating in an extension call inside an
        // expression-tree lambda compiles cleanly: the inner lambda is typed via the
        // extension-method receiver inference and the call is captured by the tree.
        var source = """
            using System;
            using System.Linq.Expressions;

            public static class Ext
            {
                public static int Apply(this Func<int, int> f, int arg) => f(arg);
            }

            public class Goo
            {
                public static void M()
                {
                    Expression<Func<int>> e = () => ((int x) => x + 1).Apply(1);
                }
            }
            """;
        CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics();
    }
}
