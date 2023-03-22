// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.LambdaDiscardParameters)]
    public class LambdaDiscardParametersTests : CompilingTestBase
    {
        [Fact]
        public void DiscardParameters_CSharp8()
        {
            var comp = CreateCompilation(@"
public class C
{
    public static void Main()
    {
        System.Func<short, string, long> f1 = (_, _) => 3L;
        System.Console.WriteLine(f1(1, null));

        System.Func<int, int, int, long> f2 = (a, _,
            _) => 4L;

        System.Func<int, int, int, long> f3 = (_, a,
            _) => 5L;

        System.Func<int, int, int, long> f4 = (_,
            _,
            _) => 6L;

        System.Func<int, int, int, long> f5 = (_,
            _,
            a) => 7L;
    }
}", parseOptions: TestOptions.Regular8);

            comp.VerifyDiagnostics(
                // (6,51): error CS8400: Feature 'lambda discard parameters' is not available in C# 8.0. Please use language version 9.0 or greater.
                //         System.Func<short, string, long> f1 = (_, _) => 3L;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "_").WithArguments("lambda discard parameters", "9.0").WithLocation(6, 51),
                // (10,13): error CS8400: Feature 'lambda discard parameters' is not available in C# 8.0. Please use language version 9.0 or greater.
                //             _) => 4L;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "_").WithArguments("lambda discard parameters", "9.0").WithLocation(10, 13),
                // (13,13): error CS8400: Feature 'lambda discard parameters' is not available in C# 8.0. Please use language version 9.0 or greater.
                //             _) => 5L;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "_").WithArguments("lambda discard parameters", "9.0").WithLocation(13, 13),
                // (16,13): error CS8400: Feature 'lambda discard parameters' is not available in C# 8.0. Please use language version 9.0 or greater.
                //             _,
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "_").WithArguments("lambda discard parameters", "9.0").WithLocation(16, 13),
                // (17,13): error CS8400: Feature 'lambda discard parameters' is not available in C# 8.0. Please use language version 9.0 or greater.
                //             _) => 6L;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "_").WithArguments("lambda discard parameters", "9.0").WithLocation(17, 13),
                // (20,13): error CS8400: Feature 'lambda discard parameters' is not available in C# 8.0. Please use language version 9.0 or greater.
                //             _,
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "_").WithArguments("lambda discard parameters", "9.0").WithLocation(20, 13)
                );

            var tree = comp.SyntaxTrees.Single();
            var underscores = tree.GetRoot().DescendantNodes().OfType<ParameterSyntax>().Where(p => p.Identifier.ToString() == "_").ToArray();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            VerifyDiscardParameterSymbol(underscores[0], "System.Int16", CodeAnalysis.NullableAnnotation.NotAnnotated, model);
            VerifyDiscardParameterSymbol(underscores[1], "System.String", CodeAnalysis.NullableAnnotation.None, model);
        }

        [Fact]
        public void DiscardParameters_LocalFunctions()
        {
            var comp = CreateCompilation(@"
public class C
{
    public static void Main()
    {
        long f1(short _, string _) => 3L;
        System.Console.WriteLine(f1(1, null));
    }
}", parseOptions: TestOptions.Regular9);

            comp.VerifyDiagnostics(
                // (6,33): error CS0100: The parameter name '_' is a duplicate
                //         long f1(short _, string _) => 3L;
                Diagnostic(ErrorCode.ERR_DuplicateParamName, "_").WithArguments("_").WithLocation(6, 33)
                );
        }

        [Fact]
        public void DiscardParameters_Methods()
        {
            var comp = CreateCompilation(@"
public class C
{
    public long M(short _, string _) => 3L;
}", parseOptions: TestOptions.Regular9);

            comp.VerifyDiagnostics(
                // (4,35): error CS0100: The parameter name '_' is a duplicate
                //     public long M(short _, string _) => 3L;
                Diagnostic(ErrorCode.ERR_DuplicateParamName, "_").WithArguments("_").WithLocation(4, 35)
                );
        }

        private static void VerifyDiscardParameterSymbol(ParameterSyntax underscore, string expectedType, CodeAnalysis.NullableAnnotation expectedAnnotation, SemanticModel model)
        {
            Assert.Null(model.GetSymbolInfo(underscore).Symbol);
            var symbol1 = model.GetDeclaredSymbol(underscore);
            Assert.Equal(expectedType, symbol1.Type.ToTestDisplayString());
            Assert.Equal("_", symbol1.Name);
            Assert.True(symbol1.IsDiscard);
            Assert.Equal(expectedType, symbol1.Type.ToTestDisplayString());
            Assert.Equal(expectedAnnotation, symbol1.NullableAnnotation);
        }

        [Fact]
        public void DiscardParameters()
        {
            var comp = CreateCompilation(@"
public class C
{
    public static void Main()
    {
        System.Func<short, short, long> f1 = (_, _) => { long _ = 3; return _; };
        System.Console.Write(f1(0, 0));

        System.Func<short, short, int, long> f2 = (_, _, a) => 4L + a;
        System.Console.Write(f2(0, 0, 1));

        System.Func<int, short, short, long> f3 = (a, _, _) => 5L + a;
        System.Console.Write(f3(1, 0, 0));
    }
}", options: TestOptions.DebugExe);

            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "356");
        }

        [Fact]
        public void DiscardParameters_RefAndOut()
        {
            var comp = CreateCompilation(@"
class C
{
    delegate int RefAndOut(ref int i, out int j);
    static void M()
    {
        RefAndOut f1 = (ref int _, out int _) =>
            {
                return 2;
            };
    }
}");

            comp.VerifyDiagnostics(
                // (9,17): error CS0177: The out parameter '_' must be assigned to before control leaves the current method
                //                 return 2;
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "return 2;").WithArguments("_").WithLocation(9, 17)
                );
        }

        [Fact]
        public void DiscardParameters_OnLocalFunction()
        {
            var comp = CreateCompilation(@"
class C
{
    static void M()
    {
        local(1, 2);
        void local(int _, int _) { }
    }
}");

            comp.VerifyDiagnostics(
                // (7,31): error CS0100: The parameter name '_' is a duplicate
                //         void local(int _, int _) { }
                Diagnostic(ErrorCode.ERR_DuplicateParamName, "_").WithArguments("_").WithLocation(7, 31)
                );
        }

        [Fact]
        public void DiscardParameters_UnicodeUnderscore()
        {
            var comp = CreateCompilation(@"
public class C
{
    public static void Main()
    {
        System.Func<short, short, long> f1 = (\u005f, \u005f) => 3L;
        \u005f = 1;
    }
}");
            comp.VerifyDiagnostics(
                // (6,55): error CS0100: The parameter name '_' is a duplicate
                //         System.Func<short, short, long> f1 = (\u005f, \u005f) => 3L;
                Diagnostic(ErrorCode.ERR_DuplicateParamName, @"\u005f").WithArguments("_").WithLocation(6, 55),
                // (7,9): error CS0103: The name '_' does not exist in the current context
                //         \u005f = 1;
                Diagnostic(ErrorCode.ERR_NameNotInContext, @"\u005f").WithArguments("_").WithLocation(7, 9)
                );
        }

        [Fact]
        public void DiscardParameters_EscapedUnderscore()
        {
            var comp = CreateCompilation(@"
public class C
{
    public static void Main()
    {
        System.Func<short, short, long> f1 = (@_, @_) => 3L;
        @_ = 1;
    }
}");
            comp.VerifyDiagnostics(
                // (6,51): error CS0100: The parameter name '_' is a duplicate
                //         System.Func<short, short, long> f1 = (@_, @_) => 3L;
                Diagnostic(ErrorCode.ERR_DuplicateParamName, "@_").WithArguments("_").WithLocation(6, 51),
                // (7,9): error CS0103: The name '_' does not exist in the current context
                //         @_ = 1;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "@_").WithArguments("_").WithLocation(7, 9)
                );
        }

        [Fact]
        public void DiscardParameters_SingleUnderscoreParameter()
        {
            var comp = CreateCompilation(@"
public class C
{
    public static void Main()
    {
        System.Func<short, short, long> f1 = (_, a) =>
        {
            int _ = 0; // 1
            return _;
        };
    }
}");
            comp.VerifyDiagnostics(
                // (8,17): error CS0136: A local or parameter named '_' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             int _ = 0; // 1
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "_").WithArguments("_").WithLocation(8, 17)
                );
        }

        [Fact]
        public void DiscardParameters_SingleUnderscoreParameter_InScopeWithUnderscoreLocal()
        {
            var src = @"
public class C
{
    public static int M()
    {
        int _ = 0;
        System.Func<short, short, long> f1 = (_, a) => 0;
        System.Func<short, short, long> f2 = (_, _) => 0;
        return _;
    }
}";
            var comp = CreateCompilation(src, parseOptions: TestOptions.Regular7_3);
            comp.VerifyDiagnostics(
                // (7,47): error CS0136: A local or parameter named '_' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         System.Func<short, short, long> f1 = (_, a) => 0;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "_").WithArguments("_").WithLocation(7, 47),
                // (8,50): error CS8370: Feature 'lambda discard parameters' is not available in C# 7.3. Please use language version 9.0 or greater.
                //         System.Func<short, short, long> f2 = (_, _) => 0;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7_3, "_").WithArguments("lambda discard parameters", "9.0").WithLocation(8, 50)
                );

            var comp2 = CreateCompilation(src, parseOptions: TestOptions.Regular8);
            comp2.VerifyDiagnostics(
                // (8,50): error CS8400: Feature 'lambda discard parameters' is not available in C# 8.0. Please use language version 9.0 or greater.
                //         System.Func<short, short, long> f2 = (_, _) => 0;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "_").WithArguments("lambda discard parameters", "9.0").WithLocation(8, 50)
                );

            var comp3 = CreateCompilation(src, parseOptions: TestOptions.Regular9);
            comp3.VerifyDiagnostics();
        }

        [Fact]
        public void DiscardParameters_WithTypes()
        {
            var comp = CreateCompilation(@"
public class C
{
    public static void Main()
    {
        System.Func<short, short, long> f1 = (short _, short _) => 3L;
        System.Console.Write(f1(0, 0));

        System.Func<short, short, int, long> f2 = (short _, short _, int a) => 4L + a;
        System.Console.Write(f2(0, 0, 1));

        System.Func<int, short, short, long> f3 = (int a, short _, short _) => 5L + a;
        System.Console.Write(f3(1, 0, 0));
    }
}", options: TestOptions.DebugExe);

            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "356");
        }

        [Fact]
        public void DiscardParameters_InDelegates()
        {
            var comp = CreateCompilation(@"
public class C
{
    public static void Main()
    {
        System.Func<int, int, long> f1 = delegate(int _, int _) { return 3L; };
        System.Console.Write(f1(0, 0));

        System.Func<int, int, int, long> f2 = delegate(int _, int _, int a) { return 4L + a; };
        System.Console.Write(f2(0, 0, 1));
    }
}", options: TestOptions.DebugExe);

            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "35");
        }

        [Fact]
        public void DiscardParameters_InDelegates_WithAttribute()
        {
            var comp = CreateCompilation(@"
public class C
{
    public static void Main()
    {
        System.Func<int, int, long> f1 = delegate([System.Obsolete]int _, int _ = 0) { return 3L; };
    }
}");

            comp.VerifyDiagnostics(
                // (6,51): error CS7014: Attributes are not valid in this context.
                //         System.Func<int, int, long> f1 = delegate([System.Obsolete]int _, int _ = 0) { return 3L; };
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[System.Obsolete]").WithLocation(6, 51),
                // (6,81): error CS1065: Default values are not valid in this context.
                //         System.Func<int, int, long> f1 = delegate([System.Obsolete]int _, int _ = 0) { return 3L; };
                Diagnostic(ErrorCode.ERR_DefaultValueNotAllowed, "=").WithLocation(6, 81));
        }

        [Fact]
        public void DiscardParameters_WithAttribute()
        {
            var source =
@"using System;
class AAttribute : Attribute { }
class C
{
    static void Main()
    {
        Action<object, object> a;
        a = ([A] _, y) => { };
        a = (object x, [A] object _) => { };
    }
}";

            var comp = CreateCompilation(source, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (8,14): error CS8773: Feature 'lambda attributes' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         a = ([A] _, y) => { };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "[A]").WithArguments("lambda attributes", "10.0").WithLocation(8, 14),
                // (9,24): error CS8773: Feature 'lambda attributes' is not available in C# 9.0. Please use language version 10.0 or greater.
                //         a = (object x, [A] object _) => { };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion9, "[A]").WithArguments("lambda attributes", "10.0").WithLocation(9, 24));
            verifyAttributes(comp);

            comp = CreateCompilation(source, parseOptions: TestOptions.Regular10);
            comp.VerifyDiagnostics();
            verifyAttributes(comp);

            static void verifyAttributes(CSharpCompilation comp)
            {
                var tree = comp.SyntaxTrees[0];
                var model = comp.GetSemanticModel(tree);
                var exprs = tree.GetRoot().DescendantNodes().OfType<LambdaExpressionSyntax>();
                var lambdas = exprs.Select(e => (IMethodSymbol)model.GetSymbolInfo(e).Symbol).ToArray();
                Assert.Equal(2, lambdas.Length);
                Assert.Equal(new[] { "AAttribute" }, getParameterAttributes(lambdas[0].Parameters[0]));
                Assert.Equal(new string[0], getParameterAttributes(lambdas[0].Parameters[1]));
                Assert.Equal(new string[0], getParameterAttributes(lambdas[1].Parameters[0]));
                Assert.Equal(new[] { "AAttribute" }, getParameterAttributes(lambdas[1].Parameters[1]));
            }

            static ImmutableArray<string> getParameterAttributes(IParameterSymbol parameter) => parameter.GetAttributes().SelectAsArray(a => a.ToString());
        }

        [Fact]
        public void DiscardParameters_NotInScope()
        {
            var comp = CreateCompilation(@"
public class C
{
    public static void Main()
    {
        System.Func<int, short, int> f = (_, _) => _;
    }
}");

            comp.VerifyDiagnostics(
                // (6,52): error CS0103: The name '_' does not exist in the current context
                //         System.Func<int, short, int> f = (_, _) => _;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "_").WithArguments("_").WithLocation(6, 52)
                );

            var tree = comp.SyntaxTrees.Single();
            var underscoreParameters = tree.GetRoot().DescendantNodes().OfType<ParameterSyntax>().Where(p => p.ToString() == "_").ToArray();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            VerifyDiscardParameterSymbol(underscoreParameters[0], "System.Int32", CodeAnalysis.NullableAnnotation.NotAnnotated, model);
            VerifyDiscardParameterSymbol(underscoreParameters[1], "System.Int16", CodeAnalysis.NullableAnnotation.NotAnnotated, model);

            var underscore = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(p => p.ToString() == "_").Single();
            Assert.Null(model.GetSymbolInfo(underscore).Symbol);
        }

        [Fact]
        public void DiscardParameters_NotInScope_BindToOutsideLocal()
        {
            var comp = CreateCompilation(@"
public class C
{
    public static void Main()
    {
        int _ = 42;
        System.Func<string, string, int> f = (_, _) => ++_;
        System.Func<long, string, long> f2 = (_, a) => ++_;
        System.Console.Write(f(null, null) + "" "");
        System.Console.Write(f2(1, null) + "" "");
        System.Console.Write(_);
    }
}", options: TestOptions.DebugExe);
            // Note that naming one of the parameters seems irrelevant but results in a binding change
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "43 2 43");

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var underscores = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(p => p.ToString() == "_").ToArray();
            Assert.Equal(3, underscores.Length);

            var localSymbol = model.GetSymbolInfo(underscores[0]).Symbol;
            Assert.Equal("System.Int32 _", localSymbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Local, localSymbol.Kind);

            var parameterSymbol = model.GetSymbolInfo(underscores[1]).Symbol;
            Assert.Equal("System.Int64 _", parameterSymbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Parameter, parameterSymbol.Kind);
        }

        [Fact]
        public void DiscardParameters_NotInScope_BindToOutsideLocal_Nested()
        {
            var comp = CreateCompilation(@"
public class C
{
    public static void Main()
    {
        int _ = 42;
        System.Func<string, string, int> f = (_, _) =>
        {
            System.Func<string, string, int> f2 = (_, _) => ++_;
            return f2(null, null);
        };
        System.Console.Write(f(null, null));
    }
}", options: TestOptions.DebugExe);
            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "43");

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var underscore = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(p => p.ToString() == "_").Single();

            var localSymbol = model.GetSymbolInfo(underscore).Symbol;
            Assert.Equal("System.Int32 _", localSymbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Local, localSymbol.Kind);
        }

        [Fact]
        public void DiscardParameters_NotInScope_DeclareLocalNamedUnderscoreInside()
        {
            var comp = CreateCompilation(@"
class C
{
    static void M()
    {
        System.Func<string, string, long> f = (_, _) => { long _ = 0; return _++; };
        System.Func<string, string, long> f2 = (_, a) => {
            long _ = 0; // 1
            return _++;
        };
    }
}");
            // Note that naming one of the parameters seems irrelevant but results in a binding change
            comp.VerifyDiagnostics(
                // (8,18): error CS0136: A local or parameter named '_' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             long _ = 0; // 1
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "_").WithArguments("_").WithLocation(8, 18)
                );

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var underscores = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(p => p.ToString() == "_").ToArray();
            Assert.Equal(2, underscores.Length);

            var localSymbol = model.GetSymbolInfo(underscores[0]).Symbol;
            Assert.Equal("System.Int64 _", localSymbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Local, localSymbol.Kind);

            var parameterSymbol = model.GetSymbolInfo(underscores[1]).Symbol;
            Assert.Equal("System.Int64 _", parameterSymbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Local, parameterSymbol.Kind);
        }

        [Fact]
        public void DiscardParameters_NotInScope_Nameof()
        {
            var comp = CreateCompilation(@"
class C
{
    static void M()
    {
        System.Func<string, string, string> f = (_, _) => nameof(_); // 1
        System.Func<long, string, string> f2 = (_, a) => nameof(_);
        System.Func<long, string> f3 = (_) => nameof(_);
    }
}");
            // Note that naming one of the parameters seems irrelevant but results in a binding change
            comp.VerifyDiagnostics(
                // (6,66): error CS0103: The name '_' does not exist in the current context
                //         System.Func<string, string, string> f = (_, _) => nameof(_); // 1
                Diagnostic(ErrorCode.ERR_NameNotInContext, "_").WithArguments("_").WithLocation(6, 66)
                );
        }

        [Fact]
        public void DiscardParameters_NotADiscardWhenSingleUnderscore()
        {
            var comp = CreateCompilation(@"
public class C
{
    public static void Main()
    {
        System.Func<int, int, int> f = (a, _) => _;
        System.Console.Write(f(1, 2));

        System.Func<int, int, int> g = (_, a) => _;
        System.Console.Write(g(1, 2));
    }
}", options: TestOptions.DebugExe);

            comp.VerifyDiagnostics();
            CompileAndVerify(comp, expectedOutput: "21");

            var tree = comp.SyntaxTrees.Single();
            var underscoreParameters = tree.GetRoot().DescendantNodes().OfType<ParameterSyntax>().Where(p => p.ToString() == "_").ToArray();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);

            var parameterSymbol1 = model.GetDeclaredSymbol(underscoreParameters[0]);
            Assert.NotNull(parameterSymbol1);
            Assert.False(parameterSymbol1.IsDiscard);

            var parameterSymbol2 = model.GetDeclaredSymbol(underscoreParameters[1]);
            Assert.NotNull(parameterSymbol2);
            Assert.False(parameterSymbol2.IsDiscard);
        }

        [Fact]
        public void DiscardParameters_Shadowing()
        {
            var comp = CreateCompilation(@"
using System;
public class C
{
    public static void M()
    {
        Action<int> f1 = (_) =>
        {
            _.ToString(); // ok
            Action<int> g2 = (_) => _.ToString(); // ok
        };

        Action<int, int> f2 = (_, _) =>
        {
            _.ToString(); // error
            Action<int> g2 = (_) => _.ToString(); // ok
        };
    }
}");

            comp.VerifyDiagnostics(
                // (15,13): error CS0103: The name '_' does not exist in the current context
                //             _.ToString(); // error
                Diagnostic(ErrorCode.ERR_NameNotInContext, "_").WithArguments("_").WithLocation(15, 13)
                );
        }
    }
}
