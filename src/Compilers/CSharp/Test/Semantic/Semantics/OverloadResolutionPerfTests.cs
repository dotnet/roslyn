// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using System.Linq;
using System.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    // Tests that should take a long time, perhaps even exceeding
    // test timeout, without shortcuts in overload resolution.
    public class OverloadResolutionPerfTests : CSharpTestBase
    {
        [WorkItem(13685, "https://github.com/dotnet/roslyn/issues/13685")]
        [ConditionalFactAttribute(typeof(IsRelease), typeof(NoIOperationValidation))]
        public void Overloads()
        {
            const int n = 3000;
            var builder = new StringBuilder();
            builder.AppendLine("class C");
            builder.AppendLine("{");
            builder.AppendLine($"    static void F() {{ F(null); }}"); // matches n overloads: F(C0), F(C1), ...
            for (int i = 0; i < n; i++)
            {
                builder.AppendLine($"    static void F(C{i} c) {{ }}");
            }
            builder.AppendLine("}");
            for (int i = 0; i < n; i++)
            {
                builder.AppendLine($"class C{i} {{ }}");
            }
            var source = builder.ToString();
            var comp = CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics(
                // (3,23): error CS0121: The call is ambiguous between the following methods or properties: 'C.F(C0)' and 'C.F(C1)'
                //     static void F() { F(null); }
                Diagnostic(ErrorCode.ERR_AmbigCall, "F").WithArguments("C.F(C0)", "C.F(C1)").WithLocation(3, 23));
        }

        [WorkItem(13685, "https://github.com/dotnet/roslyn/issues/13685")]
        [ConditionalFactAttribute(typeof(IsRelease), typeof(NoIOperationValidation))]
        public void BinaryOperatorOverloads()
        {
            const int n = 3000;
            var builder = new StringBuilder();
            builder.AppendLine("class C");
            builder.AppendLine("{");
            builder.AppendLine($"    static object F(C x) => x + null;"); // matches n overloads: +(C, C0), +(C, C1), ...
            for (int i = 0; i < n; i++)
            {
                builder.AppendLine($"    public static object operator+(C x, C{i} y) => null;");
            }
            builder.AppendLine("}");
            for (int i = 0; i < n; i++)
            {
                builder.AppendLine($"class C{i} {{ }}");
            }
            var source = builder.ToString();
            var comp = CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics(
                // (3,29): error CS0034: Operator '+' is ambiguous on operands of type 'C' and '<null>'
                //     static object F(C x) => x + null;
                Diagnostic(ErrorCode.ERR_AmbigBinaryOps, "x + null").WithArguments("+", "C", "<null>").WithLocation(3, 29));
        }

        [ConditionalFact(typeof(IsRelease))]
        public void StaticMethodsWithLambda()
        {
            const int n = 100;
            var builder = new StringBuilder();
            builder.AppendLine("using System;");
            for (int i = 0; i < n; i++)
            {
                builder.AppendLine($"class C{i} {{ }}");
            }
            builder.AppendLine("static class S");
            builder.AppendLine("{");
            for (int i = 0; i < n; i++)
            {
                builder.AppendLine($"    internal static void F(C{i} x, Action<C{i}> a) {{ F(x, y => F(y, z => F(z, w => {{ }}))); }}");
            }
            builder.AppendLine("}");
            var source = builder.ToString();
            var comp = CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(IsRelease))]
        public void ConstructorsWithLambdaAndParams()
        {
            const int n = 100;
            var builder = new StringBuilder();
            builder.AppendLine("using System;");
            for (int i = 0; i < n; i++)
            {
                builder.AppendLine($"class C{i} {{ }}");
            }
            builder.AppendLine("class C");
            builder.AppendLine("{");
            for (int i = 0; i < n; i++)
            {
                builder.AppendLine($"    internal static C F(C{i} x, params object[] args) => new C(x, y => F(y, args[1]), args[0]);");
                builder.AppendLine($"    internal C(C{i} x, Func<C{i}, C> f, params object[] args) {{ }}");
            }
            builder.AppendLine("}");
            var source = builder.ToString();
            var comp = CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(IsRelease))]
        public void ExtensionMethodsWithLambda()
        {
            const int n = 100;
            var builder = new StringBuilder();
            builder.AppendLine("using System;");
            for (int i = 0; i < n; i++)
            {
                builder.AppendLine($"class C{i} {{ }}");
            }
            builder.AppendLine("static class S");
            builder.AppendLine("{");
            for (int i = 0; i < n; i++)
            {
                builder.AppendLine($"    internal static void F(this C{i} x, Action<C{i}> a) {{ x.F(y => y.F(z => z.F(w => {{ }}))); }}");
            }
            builder.AppendLine("}");
            var source = builder.ToString();
            var comp = CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(IsRelease))]
        public void ExtensionMethodsWithLambdaAndParams()
        {
            const int n = 100;
            var builder = new StringBuilder();
            builder.AppendLine("using System;");
            for (int i = 0; i < n; i++)
            {
                builder.AppendLine($"class C{i} {{ }}");
            }
            builder.AppendLine("static class S");
            builder.AppendLine("{");
            for (int i = 0; i < n; i++)
            {
                builder.AppendLine($"    internal static void F(this C{i} x, Action<C{i}> a, params object[] args) {{ x.F(y => y.F(z => z.F(w => {{ }}), args[1]), args[0]); }}");
            }
            builder.AppendLine("}");
            var source = builder.ToString();
            var comp = CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics();
        }

        [ConditionalFactAttribute(typeof(IsRelease), typeof(NoIOperationValidation))]
        public void ExtensionMethodsWithLambdaAndErrors()
        {
            const int n = 200;
            var builder = new StringBuilder();
            builder.AppendLine("using System;");
            for (int i = 0; i < n; i++)
            {
                builder.AppendLine($"class C{i} {{ }}");
            }
            builder.AppendLine("static class S");
            builder.AppendLine("{");
            for (int i = 0; i < n; i++)
            {
                if (i % 2 == 0)
                {
                    builder.AppendLine($"    internal static void F(this C{i} x) {{ x.G(y => y.G(z => z.F())); }}"); // No match for x.G(...).
                }
                else
                {
                    builder.AppendLine($"    internal static void G(this C{i} x, Action<C{i}> a) {{ }}");
                }
            }
            builder.AppendLine("}");
            var source = builder.ToString();
            var comp = CreateCompilationWithMscorlib40AndSystemCore(source);
            // error CS1929: 'Ci' does not contain a definition for 'G' and the best extension method overload 'S.G(C1, Action<C1>)' requires a receiver of type 'C1'
            var diagnostics = Enumerable.Range(0, n / 2).
                Select(i => Diagnostic(ErrorCode.ERR_BadInstanceArgType, "x").WithArguments($"C{i * 2}", "G", "S.G(C1, System.Action<C1>)", "C1")).
                ToArray();
            comp.VerifyDiagnostics(diagnostics);
        }

        [Fact, WorkItem(29360, "https://github.com/dotnet/roslyn/pull/29360")]
        public void RaceConditionOnImproperlyCapturedAnalyzedArguments()
        {
            const int n = 6;
            var builder = new StringBuilder();
            builder.AppendLine("using System;");

            for (int i = 0; i < n; i++)
            {
                builder.AppendLine($"public class C{i}");
                builder.AppendLine("{");

                for (int j = 0; j < n; j++)
                {
                    builder.AppendLine($"    public string M{j}()");
                    builder.AppendLine("    {");

                    for (int k = 0; k < n; k++)
                    {
                        for (int l = 0; l < n; l++)
                        {
                            builder.AppendLine($"        Class.Method((C{k} x{k}) => x{k}.M{l});");
                        }
                    }

                    builder.AppendLine("        return null;");
                    builder.AppendLine("    }");
                }

                builder.AppendLine("}");
            }

            builder.AppendLine(@"
public static class Class
{
    public static void Method<TClass>(Func<TClass, Func<string>> method) { }
    public static void Method<TClass>(Func<TClass, Func<string, string>> method) { }
}
");
            var source = builder.ToString();
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [WorkItem(35949, "https://github.com/dotnet/roslyn/issues/35949")]
        [ConditionalFact(typeof(IsRelease))]
        public void NotNull_Complexity()
        {
            var source = @"
#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
class C
{
    C f = null!;

    void M(C c)
    {
        c.f = c;
        c.NotNull(
            x => x.f.NotNull(
                y => y.f.NotNull(
                    z => z.f.NotNull(
                        q => q.f.NotNull(
                            w => w.f.NotNull(
                                e => e.f.NotNull(
                                    r => r.f.NotNull(
                                        _ =>
                                        {
                                            """".NotNull(s => s);
                                            """".NotNull(s => s);
                                            """".NotNull(s => s);
                                            """".NotNull(s => s);
                                            """".NotNull(s => s);
                                            """".NotNull(s => s);
                                            """".NotNull(s => s);
                                            """".NotNull(s => s);
                                            """".NotNull(s => s);
                                            """".NotNull(s => s);
                                            """".NotNull(s => s);
                                            """".NotNull(s => s);

                                            return """";
                                        }))))))));
    }
}

static class Ext
{
    public static V NotNull<T, V>([NotNull] this T t, Func<T, V> f) => throw null!;
}
";
            var comp = CreateCompilation(new[] { NotNullAttributeDefinition, source });
            comp.VerifyDiagnostics();
        }

        [ConditionalFactAttribute(typeof(IsRelease))]
        [WorkItem(40495, "https://github.com/dotnet/roslyn/issues/40495")]
        public void NestedLambdas_01()
        {
            var source =
@"#nullable enable
using System.Linq;
class Program
{
    static void Main()
    {
        Enumerable.Range(0, 1).Sum(a =>
            Enumerable.Range(0, 1).Sum(b =>
            Enumerable.Range(0, 1).Sum(c =>
            Enumerable.Range(0, 1).Sum(d =>
            Enumerable.Range(0, 1).Sum(e =>
            Enumerable.Range(0, 1).Sum(f =>
            Enumerable.Range(0, 1).Count(g => true)))))));
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        // Test should complete in several seconds if UnboundLambda.ReallyBind
        // uses results from _returnInferenceCache.
        [ConditionalFactAttribute(typeof(IsRelease))]
        [WorkItem(1083969, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1083969")]
        public void NestedLambdas_02()
        {
            var source =
@"using System.Collections.Generic;
using System.Linq;
class Program
{
    static void F(IEnumerable<int[]> x)
    {
        x.GroupBy(y => y[1]).SelectMany(x =>
        x.GroupBy(y => y[2]).SelectMany(x =>
        x.GroupBy(y => y[3]).SelectMany(x =>
        x.GroupBy(y => y[4]).SelectMany(x =>
        x.GroupBy(y => y[5]).SelectMany(x =>
        x.GroupBy(y => y[6]).SelectMany(x =>
        x.GroupBy(y => y[7]).SelectMany(x =>
        x.GroupBy(y => y[8]).SelectMany(x =>
        x.GroupBy(y => y[9]).SelectMany(x =>
        x.GroupBy(y => y[0]).SelectMany(x =>
        x.GroupBy(y => y[1]).SelectMany(x =>
        x.GroupBy(y => y[2]).SelectMany(x =>
        x.GroupBy(y => y[3]).SelectMany(x =>
        x.GroupBy(y => y[4]).SelectMany(x =>
        x.GroupBy(y => y[5]).SelectMany(x =>
        x.GroupBy(y => y[6]).SelectMany(x =>
        x.GroupBy(y => y[7]).SelectMany(x =>
        x.GroupBy(y => y[8]).SelectMany(x =>
        x.GroupBy(y => y[9]).SelectMany(x =>
        x.GroupBy(y => y[0]).Select(x => x.Average(z => z[0])))))))))))))))))))));
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(48886, "https://github.com/dotnet/roslyn/issues/48886")]
        public void ArrayInitializationAnonymousTypes()
        {
            const int nTypes = 250;
            const int nItemsPerType = 1000;

            var builder = new StringBuilder();
            for (int i = 0; i < nTypes; i++)
            {
                builder.AppendLine($"class C{i}");
                builder.AppendLine("{");
                builder.AppendLine("    static object[] F = new[]");
                builder.AppendLine("    {");
                for (int j = 0; j < nItemsPerType; j++)
                {
                    builder.AppendLine($"        new {{ Id = {j} }},");
                }
                builder.AppendLine("    };");
                builder.AppendLine("}");
            }

            var source = builder.ToString();
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics();
        }

        [Fact]
        [WorkItem(49745, "https://github.com/dotnet/roslyn/issues/49745")]
        public void NullableStateLambdas()
        {
            const int nFunctions = 10000;

            var builder = new StringBuilder();
            builder.AppendLine("#nullable enable");
            builder.AppendLine("class Program");
            builder.AppendLine("{");
            builder.AppendLine("    static void F1(System.Func<object, object> f) { }");
            builder.AppendLine("    static void F2(object arg)");
            builder.AppendLine("    {");
            for (int i = 0; i < nFunctions; i++)
            {
                builder.AppendLine($"        F1(arg{i} => arg{i});");
            }
            builder.AppendLine("    }");
            builder.AppendLine("}");

            var source = builder.ToString();
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            CheckIsSimpleMethod(comp, "F2", true);
        }

        [Theory]
        [InlineData("class Program { static object F() => null; }", "F", true)]
        [InlineData("class Program { static void F() { } }", "F", true)]
        [InlineData("class Program { static void F() { { } { } { } } }", "F", true)]
        [InlineData("class Program { static void F() { ;;; } }", "F", false)]
        [InlineData("class Program { static void F2(System.Action a) { } static void F() { F2(() => { }); } }", "F", true)]
        [InlineData("class Program { static void F() { void Local() { } } }", "F", false)]
        [InlineData("class Program { static void F() { System.Action a = () => { }; } }", "F", false)]
        [InlineData("class Program { static void F() { if (true) { } } }", "F", false)]
        [InlineData("class Program { static void F() { while (true) { } } }", "F", false)]
        [InlineData("class Program { static void F() { try { } finally { } } }", "F", false)]
        [InlineData("class Program { static void F() { label: F(); } }", "F", false)]
        [WorkItem(49745, "https://github.com/dotnet/roslyn/issues/49745")]
        public void NullableState_IsSimpleMethod(string source, string methodName, bool expectedResult)
        {
            var comp = CreateCompilation(source);
            var diagnostics = comp.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error);
            diagnostics.Verify();
            CheckIsSimpleMethod(comp, methodName, expectedResult);
        }

        private static void CheckIsSimpleMethod(CSharpCompilation comp, string methodName, bool expectedResult)
        {
            var tree = comp.SyntaxTrees[0];
            var model = (CSharpSemanticModel)comp.GetSemanticModel(tree);
            var methodDeclaration = tree.GetCompilationUnitRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.ToString() == methodName);
            var methodBody = methodDeclaration.Body;
            BoundBlock block;
            if (methodBody is { })
            {
                var binder = model.GetEnclosingBinder(methodBody.SpanStart);
                block = binder.BindEmbeddedBlock(methodBody, new DiagnosticBag());
            }
            else
            {
                var expressionBody = methodDeclaration.ExpressionBody;
                var binder = model.GetEnclosingBinder(expressionBody.SpanStart);
                block = binder.BindExpressionBodyAsBlock(expressionBody, new DiagnosticBag());
            }
            var actualResult = NullableWalker.IsSimpleMethodVisitor.IsSimpleMethod(block);
            Assert.Equal(expectedResult, actualResult);
        }
    }
}
