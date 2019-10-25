// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
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
                // (6,51): error CS8652: The feature 'discard parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         System.Func<short, string, long> f1 = (_, _) => 3L;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "_").WithArguments("discard parameters").WithLocation(6, 51),
                // (10,13): error CS8652: The feature 'discard parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //             _) => 4L;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "_").WithArguments("discard parameters").WithLocation(10, 13),
                // (13,13): error CS8652: The feature 'discard parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //             _) => 5L;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "_").WithArguments("discard parameters").WithLocation(13, 13),
                // (16,13): error CS8652: The feature 'discard parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //             _,
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "_").WithArguments("discard parameters").WithLocation(16, 13),
                // (17,13): error CS8652: The feature 'discard parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //             _) => 6L;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "_").WithArguments("discard parameters").WithLocation(17, 13),
                // (20,13): error CS8652: The feature 'discard parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //             _,
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "_").WithArguments("discard parameters").WithLocation(20, 13)
                );

            var tree = comp.SyntaxTrees.Single();
            var underscores = tree.GetRoot().DescendantNodes().OfType<ParameterSyntax>().Where(p => p.Identifier.ToString() == "_").ToArray();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            VerifyDiscardParameterSymbol(underscores[0], "System.Int16", CodeAnalysis.NullableAnnotation.NotAnnotated, model);
            VerifyDiscardParameterSymbol(underscores[1], "System.String", CodeAnalysis.NullableAnnotation.None, model);
        }

        [Fact]
        public void DiscardParameters_CSharp8_LocalFunctions()
        {
            var comp = CreateCompilation(@"
public class C
{
    public static void Main()
    {
        long f1(short _, string _) => 3L;
        System.Console.WriteLine(f1(1, null));
    }
}", parseOptions: TestOptions.Regular8);

            comp.VerifyDiagnostics(
                // (6,33): error CS8652: The feature 'discard parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //         long f1(short _, string _) => 3L;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "_").WithArguments("discard parameters").WithLocation(6, 33)
                );

            var tree = comp.SyntaxTrees.Single();
            var underscores = tree.GetRoot().DescendantNodes().OfType<ParameterSyntax>().Where(p => p.Identifier.ToString() == "_").ToArray();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            VerifyDiscardParameterSymbol(underscores[0], "System.Int16", CodeAnalysis.NullableAnnotation.NotAnnotated, model);
            VerifyDiscardParameterSymbol(underscores[1], "System.String", CodeAnalysis.NullableAnnotation.None, model);
        }

        [Fact]
        public void DiscardParameters_CSharp8_Methods()
        {
            var comp = CreateCompilation(@"
public class C
{
    public long M(short _, string _) => 3L;
}", parseOptions: TestOptions.Regular8);

            comp.VerifyDiagnostics(
                // (4,35): error CS8652: The feature 'discard parameters' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public long M(short _, string _) => 3L;
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "_").WithArguments("discard parameters").WithLocation(4, 35)
                );

            var tree = comp.SyntaxTrees.Single();
            var underscores = tree.GetRoot().DescendantNodes().OfType<ParameterSyntax>().Where(p => p.Identifier.ToString() == "_").ToArray();
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

            // Note: this is somewhat problematic because there is nothing the user can do to fix this. We could have an error for out discards
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

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DiscardParameters_OnLocalFunction_NotInScope()
        {
            var comp = CreateCompilation(@"
class C
{
    static void M()
    {
        int _ = 0;
        local(1, 2);
        void local(int _, int _) { _++; }
    }
}");

            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var underscore = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(p => p.ToString() == "_").Single();

            var localSymbol = model.GetSymbolInfo(underscore).Symbol;
            Assert.Equal("System.Int32 _", localSymbol.ToTestDisplayString());
            Assert.Equal(SymbolKind.Local, localSymbol.Kind);
        }

        [Fact]
        public void DiscardParameters_OnMethod()
        {
            var comp = CreateCompilation(@"
public class C
{
    public static void M(int _, int _)
    {
        M(1, 2);
        _ = """";
    }
}");

            comp.VerifyDiagnostics();

            var comp2 = CreateCompilation(@"
class D
{
    public static void M2()
    {
        C.M(1, 2);
    }
}
", references: new[] { comp.EmitToImageReference() });
            comp2.VerifyDiagnostics();
            var method = comp2.GlobalNamespace.GetMember("C.M");
            Assert.Equal("void C.M(System.Int32 <>_1, System.Int32 <>_2)", method.ToTestDisplayString());

            var comp3 = CreateCompilation(@"
class D
{
    public static void M2()
    {
        C.M(1, _: 2);
        C.M(_: 1, 2);
    }
}
", references: new[] { comp.EmitToImageReference() });
            comp3.VerifyDiagnostics(
                // (6,16): error CS1739: The best overload for 'M' does not have a parameter named '_'
                //         C.M(1, _: 2);
                Diagnostic(ErrorCode.ERR_BadNamedArgument, "_").WithArguments("M", "_").WithLocation(6, 16),
                // (7,13): error CS1739: The best overload for 'M' does not have a parameter named '_'
                //         C.M(_: 1, 2);
                Diagnostic(ErrorCode.ERR_BadNamedArgument, "_").WithArguments("M", "_").WithLocation(7, 13)
                );
        }

        [Fact]
        public void DiscardParameters_OnMethod_Partial()
        {
            var comp = CreateCompilation(@"
public class C
{
    public static void M(int _, int _)
    {
        M(1, 2);
        _ = """";
    }
}");

            comp.VerifyDiagnostics();

            var comp2 = CreateCompilation(@"
class D
{
    public static void M2()
    {
        C.M(1, 2);
    }
}
", references: new[] { comp.EmitToImageReference() });
            comp2.VerifyDiagnostics();

            var comp3 = CreateCompilation(@"
class D
{
    public static void M2()
    {
        C.M(1, _: 2);
        C.M(_: 1, 2);
    }
}
", references: new[] { comp.EmitToImageReference() });
            comp3.VerifyDiagnostics(
                // (6,16): error CS1739: The best overload for 'M' does not have a parameter named '_'
                //         C.M(1, _: 2);
                Diagnostic(ErrorCode.ERR_BadNamedArgument, "_").WithArguments("M", "_").WithLocation(6, 16),
                // (7,13): error CS1739: The best overload for 'M' does not have a parameter named '_'
                //         C.M(_: 1, 2);
                Diagnostic(ErrorCode.ERR_BadNamedArgument, "_").WithArguments("M", "_").WithLocation(7, 13)
                );
        }

        [Fact]
        public void DiscardParameters_OnMethod_NamedArgument()
        {
            var comp = CreateCompilation(@"
class C
{
    static void M(int _, string _)
    {
        M(1, _: null);
        M(_: 1, null);
    }
}");

            comp.VerifyDiagnostics(
                // (6,14): error CS1739: The best overload for 'M' does not have a parameter named '_'
                //         M(1, _: null);
                Diagnostic(ErrorCode.ERR_BadNamedArgument, "_").WithArguments("M", "_").WithLocation(6, 14),
                // (7,11): error CS1739: The best overload for 'M' does not have a parameter named '_'
                //         M(_: 1, null);
                Diagnostic(ErrorCode.ERR_BadNamedArgument, "_").WithArguments("M", "_").WithLocation(7, 11)
                );

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var calls = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
            Assert.Null(model.GetSymbolInfo(calls[0]).Symbol);
            Assert.Null(model.GetSymbolInfo(calls[1]).Symbol);
        }

        [Fact]
        public void DiscardParameters_OnMethod_NamedArgument_Underscore()
        {
            var comp = CreateCompilation(@"
class C
{
    static void M(int a, string _)
    {
        M(1, _: null);
    }
}");

            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var call = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
            Assert.Equal("void C.M(System.Int32 a, System.String _)", model.GetSymbolInfo(call).Symbol.ToTestDisplayString());
        }

        [Fact]
        public void DiscardParameters_OnMethod_NamedArgument_Underscore2()
        {
            var comp = CreateCompilation(@"
class C
{
    void M(int a, string _) { }
    void M(long _, string _)
    {
        M(1, _: null);
    }
}");

            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var call = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
            Assert.Equal("void C.M(System.Int32 a, System.String _)", model.GetSymbolInfo(call).Symbol.ToTestDisplayString());
        }

        [Fact]
        public void DiscardParameters_OnMethod_NamedArgumentDoesNotMatchDiscard()
        {
            var comp = CreateCompilation(@"
class C
{
    static void M(int _, string _)
    {
        M(1, b: null);
    }
}");

            comp.VerifyDiagnostics(
                // (6,14): error CS1739: The best overload for 'M' does not have a parameter named 'b'
                //         M(1, b: null);
                Diagnostic(ErrorCode.ERR_BadNamedArgument, "b").WithArguments("M", "b").WithLocation(6, 14)
                );

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var call = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
            Assert.Null(model.GetSymbolInfo(call).Symbol);
        }

        [Fact]
        public void DiscardParameters_OnMethod_WithXmlDoc()
        {
            var comp = CreateCompilation(@"
class C
{
    /// <summary></summary>
    /// <param name=""_"">1</param>
    /// <param name=""_"">2</param>
    void M(int _, int _)
    {
    }
}", parseOptions: TestOptions.RegularPreview.WithDocumentationMode(DocumentationMode.Diagnose));

            comp.VerifyDiagnostics(
                // (5,22): warning CS1572: XML comment has a param tag for '_', but there is no parameter by that name
                //     /// <param name="_">1</param>
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, "_").WithArguments("_").WithLocation(5, 22),
                // (6,22): warning CS1572: XML comment has a param tag for '_', but there is no parameter by that name
                //     /// <param name="_">2</param>
                Diagnostic(ErrorCode.WRN_UnmatchedParamTag, "_").WithArguments("_").WithLocation(6, 22)
                );
        }

        // TODO2 test as range variables?

        [Fact]
        public void DiscardParameters_OnMethod_Overridding()
        {
            var comp = CreateCompilation(@"
public class Base
{
    public virtual void M(int _, int _)
    {
    }
}
public class C : Base
{
    public override void M(int _, int _)
    {
    }
}");

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DiscardParameters_OnMethod_Overridding_SettingNames()
        {
            var comp = CreateCompilation(@"
public class Base
{
    public virtual void M(int _, int _)
    {
    }
}
public class C : Base
{
    public override void M(int a, int b)
    {
    }
}");

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DiscardParameters_OnMethod_Overridding_RemovingNames()
        {
            var comp = CreateCompilation(@"
public class Base
{
    public virtual void M(int a, int b)
    {
    }
}
public class C : Base
{
    public override void M(int _, int _)
    {
    }
}");

            comp.VerifyDiagnostics();
        }

        [Fact]
        public void DiscardParameters_OnConstructor()
        {
            var comp = CreateCompilation(@"
class C
{
    C(int _, string _)
    {
        new C(1, null);
        new C(1, _: null); // 1
        new C(_: 1, null); // 2
        _.ToString(); // 3
    }
}");

            comp.VerifyDiagnostics(
                // (7,18): error CS1739: The best overload for 'C' does not have a parameter named '_'
                //         new C(1, _: null); // 1
                Diagnostic(ErrorCode.ERR_BadNamedArgument, "_").WithArguments("C", "_").WithLocation(7, 18),
                // (8,15): error CS1739: The best overload for 'C' does not have a parameter named '_'
                //         new C(_: 1, null); // 2
                Diagnostic(ErrorCode.ERR_BadNamedArgument, "_").WithArguments("C", "_").WithLocation(8, 15),
                // (9,9): error CS0103: The name '_' does not exist in the current context
                //         _.ToString(); // 3
                Diagnostic(ErrorCode.ERR_NameNotInContext, "_").WithArguments("_").WithLocation(9, 9)
                );

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var calls = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().ToArray();
            Assert.Equal("C..ctor(System.Int32 _, System.String _)", model.GetSymbolInfo(calls[0]).Symbol.ToTestDisplayString());
            Assert.Null(model.GetSymbolInfo(calls[1]).Symbol);
            Assert.Null(model.GetSymbolInfo(calls[2]).Symbol);
        }

        [Fact]
        public void DiscardParameters_OnDelegate()
        {
            var comp = CreateCompilation(@"
class C
{
    delegate void Signature(int _, int _);

    static void M(Signature s)
    {
        s(1, _: 2);
    }
}");

            comp.VerifyDiagnostics(
                // (8,14): error CS1746: The delegate 'C.Signature' does not have a parameter named '_'
                //         s(1, _: 2);
                Diagnostic(ErrorCode.ERR_BadNamedArgumentForDelegateInvoke, "_").WithArguments("C.Signature", "_").WithLocation(8, 14)
                );
        }

        [Fact]
        public void DiscardParameters_VerifyMetadata()
        {
            var comp = CreateCompilation(@"
public class C
{
    public delegate int Delegate(string _, string _);
    public int this[string _, string _] => throw null;
    public int M1(string _, string _) => throw null;
    public int M2(int a, string _, string _) => throw null;
    public int M3(string _, int b, string _) => throw null;
    public int M4(string _, string _, int c) => throw null;
    public int M5(int a, string _, string _ = null) => throw null;
}

public interface I
{
    int M(int _, string b, int _);
}
");
            comp.VerifyDiagnostics();

            var comp2 = CreateCompilation("", new[] { comp.EmitToImageReference() });
            var cMembers = comp2.GetTypeByMetadataName("C").GetMembers();
            AssertEx.Equal(new[] {
                "System.Int32 C.this[System.String <>_1, System.String <>_2].get",
                "System.Int32 C.M1(System.String <>_1, System.String <>_2)",
                "System.Int32 C.M2(System.Int32 a, System.String <>_2, System.String <>_3)",
                "System.Int32 C.M3(System.String <>_1, System.Int32 b, System.String <>_3)",
                "System.Int32 C.M4(System.String <>_1, System.String <>_2, System.Int32 c)",
                "System.Int32 C.M5(System.Int32 a, System.String <>_2, [System.String <>_3 = null])",
                "C..ctor()",
                "System.Int32 C.this[System.String <>_1, System.String <>_2] { get; }",
                "C.Delegate" },
                cMembers.Select(m => m.ToTestDisplayString()));

            var iMembers = comp2.GetTypeByMetadataName("I").GetMembers();
            AssertEx.Equal(new[] {
                "System.Int32 I.M(System.Int32 <>_1, System.String b, System.Int32 <>_3)" },
                iMembers.Select(m => m.ToTestDisplayString()));

            var delegateMembers = cMembers.OfType<INamedTypeSymbol>().Single().GetMembers();
            AssertEx.Equal(new[] {
                "C.Delegate..ctor(System.Object @object, System.IntPtr method)",
                "System.Int32 C.Delegate.Invoke(System.String <>_1, System.String <>_2)",
                "System.IAsyncResult C.Delegate.BeginInvoke(System.String <>_1, System.String <>_2, System.AsyncCallback callback, System.Object @object)",
                "System.Int32 C.Delegate.EndInvoke(System.IAsyncResult result)" },
                delegateMembers.Select(m => m.ToTestDisplayString()));
        }

        [Fact]
        public void DiscardParameters_VerifyMetadata_OnPartialMethod()
        {
            var comp = CreateCompilation(@"
public partial class C
{
    partial void M1(string _, string _);
    partial void M2(string a, string b);
    partial void M3(string _, string _);
    partial void M4(string _, string _ = null);
}
public partial class C
{
    partial void M1(string _, string _) => throw null;
    partial void M2(string _, string _) => throw null;
    partial void M3(string a, string b) => throw null;
    partial void M4(string _, string _) => throw null;

    void M()
    {
        M1(null, null);

        M2(null, null);
        M2(a: null, null);
        M2(null, b: null);
        M2(_: null, null); // 1
        M2(null, _: null); // 2

        M3(null, null);
        M3(a: null, null); // 3
        M3(null, b: null); // 4
        M3(_: null, null); // 5
        M3(null, _: null); // 6
    }
}
");
            comp.VerifyDiagnostics(
                // (23,12): error CS1739: The best overload for 'M2' does not have a parameter named '_'
                //         M2(_: null, null); // 1
                Diagnostic(ErrorCode.ERR_BadNamedArgument, "_").WithArguments("M2", "_").WithLocation(23, 12),
                // (24,18): error CS1739: The best overload for 'M2' does not have a parameter named '_'
                //         M2(null, _: null); // 2
                Diagnostic(ErrorCode.ERR_BadNamedArgument, "_").WithArguments("M2", "_").WithLocation(24, 18),
                // (27,12): error CS1739: The best overload for 'M3' does not have a parameter named 'a'
                //         M3(a: null, null); // 3
                Diagnostic(ErrorCode.ERR_BadNamedArgument, "a").WithArguments("M3", "a").WithLocation(27, 12),
                // (28,18): error CS1739: The best overload for 'M3' does not have a parameter named 'b'
                //         M3(null, b: null); // 4
                Diagnostic(ErrorCode.ERR_BadNamedArgument, "b").WithArguments("M3", "b").WithLocation(28, 18),
                // (29,12): error CS1739: The best overload for 'M3' does not have a parameter named '_'
                //         M3(_: null, null); // 5
                Diagnostic(ErrorCode.ERR_BadNamedArgument, "_").WithArguments("M3", "_").WithLocation(29, 12),
                // (30,18): error CS1739: The best overload for 'M3' does not have a parameter named '_'
                //         M3(null, _: null); // 6
                Diagnostic(ErrorCode.ERR_BadNamedArgument, "_").WithArguments("M3", "_").WithLocation(30, 18)
                );
        }

        [Fact]
        public void DiscardParameters_OnIndexer()
        {
            var comp = CreateCompilation(@"
class C1
{
    int this[int _, int _] => _++; // 1
}");

            comp.VerifyDiagnostics(
                // (4,31): error CS0103: The name '_' does not exist in the current context
                //     int this[int _, int _] => _++; // 1
                Diagnostic(ErrorCode.ERR_NameNotInContext, "_").WithArguments("_").WithLocation(4, 31)
                );

            comp = CreateCompilation(@"
public class C
{
    public int this[int _, int _] => 1;
}");

            comp.VerifyDiagnostics();

            var comp2 = CreateCompilation(@"
class D
{
    public static void M2(C c)
    {
        _ = c[1, 2];
    }
}
", references: new[] { comp.EmitToImageReference() });
            comp2.VerifyDiagnostics();

            var getter = comp2.GetTypeByMetadataName("C").GetMembers().OfType<IMethodSymbol>().Where(m => m.Name == "get_Item").Single();
            Assert.Equal("System.Int32 C.this[System.Int32 <>_1, System.Int32 <>_2].get", getter.ToTestDisplayString());

            var comp3 = CreateCompilation(@"
class D
{
    public static void M2(C c)
    {
        _ = c[1, _: 2];
        _ = c[_: 1, 2];
    }
}
", references: new[] { comp.EmitToImageReference() });
            comp3.VerifyDiagnostics(
                // (6,18): error CS1739: The best overload for 'this' does not have a parameter named '_'
                //         _ = c[1, _: 2];
                Diagnostic(ErrorCode.ERR_BadNamedArgument, "_").WithArguments("this", "_").WithLocation(6, 18),
                // (7,15): error CS1739: The best overload for 'this' does not have a parameter named '_'
                //         _ = c[_: 1, 2];
                Diagnostic(ErrorCode.ERR_BadNamedArgument, "_").WithArguments("this", "_").WithLocation(7, 15)
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
        public void DiscardParameters_NotInScope_BindToOutsideLocal()
        {
            var comp = CreateCompilation(@"
class C
{
    static void M()
    {
        int _ = 0;
        System.Func<string, string, int> f = (_, _) => _++;
        System.Func<long, string, long> f2 = (_, a) => _++;
    }
}");
            // Note that naming one of the parameters seems irrelevant but results in a binding change
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree, ignoreAccessibility: false);
            var underscores = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(p => p.ToString() == "_").ToArray();
            Assert.Equal(2, underscores.Length);

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
class C
{
    static void M()
    {
        int _ = 0;
        System.Func<string, string, int> f = (_, _) =>
        {
            System.Func<string, string, int> f2 = (_, _) => _++;
            return f2(null, null);
        };
    }
}");
            comp.VerifyDiagnostics();

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
        System.Func<string, string, long> f2 = (_, a) => { long _ = 0; return _++; };
    }
}");
            // Note that naming one of the parameters seems irrelevant but results in a binding change
            comp.VerifyDiagnostics(
                // (7,65): error CS0136: A local or parameter named '_' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         System.Func<string, string, long> f2 = (_, a) => { long _ = 0; return _++; };
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "_").WithArguments("_").WithLocation(7, 65)
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
    }
}
