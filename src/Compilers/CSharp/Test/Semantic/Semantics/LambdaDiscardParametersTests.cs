// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        // This method should be removed once the lambda discard parameters feature is slotted into a C# language version
        public new static CSharpCompilation CreateCompilation(
            CSharpTestSource source,
            System.Collections.Generic.IEnumerable<MetadataReference> references = null,
            CSharpCompilationOptions options = null,
            CSharpParseOptions parseOptions = null,
            Roslyn.Test.Utilities.TargetFramework targetFramework = Roslyn.Test.Utilities.TargetFramework.Standard,
            string assemblyName = "",
            string sourceFileName = "",
            bool skipUsesIsNullable = false)
            => CSharpTestBase.CreateCompilation(source, references, options, parseOptions: parseOptions ?? TestOptions.RegularPreview, targetFramework, assemblyName, sourceFileName, skipUsesIsNullable);

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
                // (6,51): error CS8652: The feature 'lambda discard parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         System.Func<short, string, long> f1 = (_, _) => 3L;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "_").WithArguments("lambda discard parameters").WithLocation(6, 51),
                // (10,13): error CS8652: The feature 'lambda discard parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //             _) => 4L;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "_").WithArguments("lambda discard parameters").WithLocation(10, 13),
                // (13,13): error CS8652: The feature 'lambda discard parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //             _) => 5L;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "_").WithArguments("lambda discard parameters").WithLocation(13, 13),
                // (16,13): error CS8652: The feature 'lambda discard parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //             _,
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "_").WithArguments("lambda discard parameters").WithLocation(16, 13),
                // (17,13): error CS8652: The feature 'lambda discard parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //             _) => 6L;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "_").WithArguments("lambda discard parameters").WithLocation(17, 13),
                // (20,13): error CS8652: The feature 'lambda discard parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //             _,
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "_").WithArguments("lambda discard parameters").WithLocation(20, 13)
                );

            var tree = comp.SyntaxTrees.Single();
            var underscores = tree.GetRoot().DescendantNodes().OfType<ParameterSyntax>().Where(p => p.ToString() == "_").ToArray();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            VerifyDiscardParameterSymbol(underscores[0], "System.Int16", CodeAnalysis.NullableAnnotation.NotAnnotated, model);
            VerifyDiscardParameterSymbol(underscores[1], "System.String", CodeAnalysis.NullableAnnotation.None, model);
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
        public void DiscardParameters_OnLocalFunction()
        {
            var comp = CreateCompilation(@"
class C
{
    static void M()
    {
        local();
        void local(int _, int _) {}
    }
}");

            comp.VerifyDiagnostics(
                // (6,9): error CS7036: There is no argument given that corresponds to the required formal parameter '_' of 'local(int, int)'
                //         local();
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "local").WithArguments("_", "local(int, int)").WithLocation(6, 9),
                // (7,31): error CS0100: The parameter name '_' is a duplicate
                //         void local(int _, int _) {}
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
                Diagnostic(ErrorCode.ERR_DefaultValueNotAllowed, "=").WithLocation(6, 81)
                );
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
    }
}
