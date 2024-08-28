// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using System.Collections.Immutable;
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
        [ConditionalFact(typeof(IsRelease), typeof(NoIOperationValidation))]
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
        [ConditionalFact(typeof(IsRelease), typeof(NoIOperationValidation))]
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

        [ConditionalFact(typeof(IsRelease), typeof(NoIOperationValidation))]
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

        [ConditionalFact(typeof(IsRelease))]
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
            var diagnostics = comp.GetDiagnostics().Where(d => d is not { Severity: DiagnosticSeverity.Info, Code: (int)ErrorCode.INF_TooManyBoundLambdas });
            diagnostics.Verify();
        }

        /// <summary>
        /// A variation of <see cref="NestedLambdas_01"/> but with
        /// explicit parameter types and return type for the lambdas.
        /// </summary>
        [ConditionalFact(typeof(IsRelease))]
        public void NestedLambdas_WithParameterAndReturnTypes()
        {
            var source =
@"#nullable enable
using System.Linq;
class Program
{
    static void Main()
    {
        Enumerable.Range(0, 1).Sum(int (int a) =>
            Enumerable.Range(0, 1).Sum(int (int b) =>
            Enumerable.Range(0, 1).Sum(int (int c) =>
            Enumerable.Range(0, 1).Sum(int (int d) =>
            Enumerable.Range(0, 1).Sum(int (int e) =>
            Enumerable.Range(0, 1).Sum(int (int f) =>
            Enumerable.Range(0, 1).Sum(int (int g) =>
            Enumerable.Range(0, 1).Sum(int (int h) =>
            Enumerable.Range(0, 1).Sum(int (int i) =>
            Enumerable.Range(0, 1).Sum(int (int j) =>
            Enumerable.Range(0, 1).Sum(int (int k) =>
            Enumerable.Range(0, 1).Count(l => true))))))))))));
    }
}";
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        // Test should complete in several seconds if UnboundLambda.ReallyBind
        // uses results from _returnInferenceCache.
        [ConditionalFact(typeof(IsRelease))]
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
        public void NestedLambdas_MethodBody()
        {
            var source = """
                #pragma warning disable 649
                using System.Collections.Generic;
                using System.Linq;
                class Container
                {
                    public IEnumerable<Container> Items;
                    public int Value;
                }
                class Program
                {
                    static void Main()
                    {
                        var list = new List<Container>();
                        _ = list.Sum(
                            a => a.Items.Sum(
                                b => b.Items.Sum(
                                    c => c.Value)));
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (16,19): info CS9236: Compiling requires binding the lambda expression at least 100 times. Consider declaring the lambda expression with explicit parameter types, or if the containing method call is generic, consider using explicit type arguments.
                //                 b => b.Items.Sum(
                Diagnostic(ErrorCode.INF_TooManyBoundLambdas, "=>").WithArguments("100").WithLocation(16, 19),
                // (17,23): info CS9236: Compiling requires binding the lambda expression at least 1300 times. Consider declaring the lambda expression with explicit parameter types, or if the containing method call is generic, consider using explicit type arguments.
                //                     c => c.Value)));
                Diagnostic(ErrorCode.INF_TooManyBoundLambdas, "=>").WithArguments("1300").WithLocation(17, 23));
        }

        [Fact]
        public void NestedLambdas_FieldInitializer()
        {
            var source = """
                #pragma warning disable 649
                using System.Collections.Generic;
                using System.Linq;
                class Container
                {
                    public IEnumerable<Container> Items;
                    public int Value;
                }
                class Program(List<Container> list)
                {
                    int _f = list.Sum(
                        a => a.Items.Sum(
                            b => b.Items.Sum(
                                c => c.Value)));
                    static void Main()
                    {
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (13,15): info CS9236: Compiling requires binding the lambda expression at least 100 times. Consider declaring the lambda expression with explicit parameter types, or if the containing method call is generic, consider using explicit type arguments.
                //             b => b.Items.Sum(
                Diagnostic(ErrorCode.INF_TooManyBoundLambdas, "=>").WithArguments("100").WithLocation(13, 15),
                // (14,19): info CS9236: Compiling requires binding the lambda expression at least 1300 times. Consider declaring the lambda expression with explicit parameter types, or if the containing method call is generic, consider using explicit type arguments.
                //                 c => c.Value)));
                Diagnostic(ErrorCode.INF_TooManyBoundLambdas, "=>").WithArguments("1300").WithLocation(14, 19));
        }

        [ConditionalFact(typeof(NoIOperationValidation), Reason = "Timeouts")]
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
        [WorkItem(49746, "https://github.com/dotnet/roslyn/issues/49746")]
        public void AnalyzeMethodsInEnabledContextOnly()
        {
            const int nMethods = 10000;

            var builder = new StringBuilder();
            builder.AppendLine("static class Program");
            builder.AppendLine("{");
            for (int i = 0; i < nMethods; i++)
            {
                builder.AppendLine(i % 2 == 0 ? "#nullable enable" : "#nullable disable");
                builder.AppendLine($"    static object F{i}(object arg{i}) => arg{i};");
            }
            builder.AppendLine("}");

            var source = builder.ToString();
            var comp = CreateCompilation(source);
            var nullableAnalysisData = new NullableWalker.NullableAnalysisData();
            comp.TestOnlyCompilationData = nullableAnalysisData;
            comp.VerifyDiagnostics();

            int analyzed = nullableAnalysisData.Data.Where(pair => pair.Value.RequiredAnalysis).Count();
            Assert.Equal(nMethods / 2, analyzed);
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
            var nullableAnalysisData = new NullableWalker.NullableAnalysisData();
            comp.TestOnlyCompilationData = nullableAnalysisData;
            comp.VerifyDiagnostics();

            var method = comp.GetMember("Program.F2");
            Assert.Equal(1, nullableAnalysisData.Data[method].TrackedEntries);
        }

        [Fact]
        [WorkItem(49745, "https://github.com/dotnet/roslyn/issues/49745")]
        public void NullableStateLocalFunctions()
        {
            const int nFunctions = 2000;

            var builder = new StringBuilder();
            builder.AppendLine("#nullable enable");
            builder.AppendLine("class Program");
            builder.AppendLine("{");
            builder.AppendLine("    static void F(object arg)");
            builder.AppendLine("    {");
            for (int i = 0; i < nFunctions; i++)
            {
                builder.AppendLine($"        _ = F{i}(arg);");
                builder.AppendLine($"        static object F{i}(object arg{i}) => arg{i};");
            }
            builder.AppendLine("    }");
            builder.AppendLine("}");

            var source = builder.ToString();
            var comp = CreateCompilation(source);
            var nullableAnalysisData = new NullableWalker.NullableAnalysisData();
            comp.TestOnlyCompilationData = nullableAnalysisData;
            comp.VerifyDiagnostics();

            var method = comp.GetMember("Program.F");
            Assert.Equal(1, nullableAnalysisData.Data[method].TrackedEntries);
        }

        [ConditionalFact(typeof(NoIOperationValidation))]
        public void NullableStateTooManyLocals_01()
        {
            const int nLocals = 65536;

            var builder = new StringBuilder();
            builder.AppendLine("#pragma warning disable 168");
            builder.AppendLine("#nullable enable");
            builder.AppendLine("class Program");
            builder.AppendLine("{");
            builder.AppendLine("    static void F(object arg)");
            builder.AppendLine("    {");
            for (int i = 1; i < nLocals; i++)
            {
                builder.AppendLine($"        object i{i};");
            }
            builder.AppendLine("        object i0 = arg;");
            builder.AppendLine("        if (i0 == null) i0.ToString();");
            builder.AppendLine("    }");
            builder.AppendLine("}");

            var source = builder.ToString();
            var comp = CreateCompilation(source);
            // No warning for 'i0.ToString()' because the local is not tracked
            // by the NullableWalker.Variables instance (too many locals).
            comp.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(NoIOperationValidation), typeof(IsRelease))]
        public void NullableStateTooManyLocals_02()
        {
            const int nLocals = 65536;

            var builder = new StringBuilder();
            builder.AppendLine("#nullable enable");
            builder.AppendLine("class Program");
            builder.AppendLine("{");
            builder.AppendLine("    static object F()");
            builder.AppendLine("    {");
            builder.AppendLine("        object i0 = null;");
            for (int i = 1; i < nLocals; i++)
            {
                builder.AppendLine($"        var i{i} = i{i - 1};");
            }
            builder.AppendLine($"        return i{nLocals - 1};");
            builder.AppendLine("    }");
            builder.AppendLine("}");

            var source = builder.ToString();
            var comp = CreateCompilation(source);
            // https://github.com/dotnet/roslyn/issues/50588: Improve performance of assignments to many variables.
            comp.VerifyDiagnostics(
                // (6,21): warning CS8600: Converting null literal or possible null value to non-nullable type.
                //         object i0 = null;
                Diagnostic(ErrorCode.WRN_ConvertingNullableToNonNullable, "null").WithLocation(6, 21),
                // (65542,16): warning CS8603: Possible null reference return.
                //         return i65535;
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "i65535").WithLocation(65542, 16));
        }

        [ConditionalFact(typeof(NoIOperationValidation), typeof(IsRelease))]
        public void NullableStateManyNestedFunctions()
        {
            const int nFunctions = 32768;

            var builder = new StringBuilder();
            builder.AppendLine("#nullable enable");
            builder.AppendLine("class Program");
            builder.AppendLine("{");
            builder.AppendLine("    static void F0(System.Action a) { }");
            builder.AppendLine("    static U F1<T, U>(T arg, System.Func<T, U> f) => f(arg);");
            builder.AppendLine("    static object F2(object arg)");
            builder.AppendLine("    {");
            builder.AppendLine("        if (arg == null) { }");
            builder.AppendLine("        var value = arg;");
            builder.AppendLine("        F0(() => { });");
            for (int i = 0; i < nFunctions / 2; i++)
            {
                builder.AppendLine($"        F0(() => {{ value = F1(value, arg{i} => arg{i}?.ToString()); }});");
            }
            builder.AppendLine("        return value;");
            builder.AppendLine("    }");
            builder.AppendLine("}");

            var source = builder.ToString();
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (16395,16): warning CS8603: Possible null reference return.
                //         return value;
                Diagnostic(ErrorCode.WRN_NullReferenceReturn, "value").WithLocation(16395, 16));
        }

        [WorkItem(51739, "https://github.com/dotnet/roslyn/issues/51739")]
        [ConditionalFact(typeof(IsRelease))]
        public void NullableAnalysisNestedExpressionsInMethod()
        {
            const int nestingLevel = 400;

            var builder = new StringBuilder();
            builder.AppendLine("#nullable enable");
            builder.AppendLine("class C");
            builder.AppendLine("{");
            builder.AppendLine("    C F(int i) => this;");
            builder.AppendLine("    static void Main()");
            builder.AppendLine("    {");
            builder.AppendLine("        C c = new C()");
            for (int i = 0; i < nestingLevel; i++)
            {
                builder.AppendLine($"            .F({i})");
            }
            builder.AppendLine("            ;");
            builder.AppendLine("    }");
            builder.AppendLine("}");

            var source = builder.ToString();
            var comp = CreateCompilation(source);
            comp.TestOnlyCompilationData = new NullableWalker.NullableAnalysisData(maxRecursionDepth: nestingLevel / 2);
            comp.VerifyDiagnostics();
        }

        [WorkItem(51739, "https://github.com/dotnet/roslyn/issues/51739")]
        [ConditionalFact(typeof(IsRelease))]
        public void NullableAnalysisNestedExpressionsInLocalFunction()
        {
            const int nestingLevel = 400;

            var builder = new StringBuilder();
            builder.AppendLine("#nullable enable");
            builder.AppendLine("class C");
            builder.AppendLine("{");
            builder.AppendLine("    C F(int i) => this;");
            builder.AppendLine("    static void Main()");
            builder.AppendLine("    {");
            builder.AppendLine("        Local();");
            builder.AppendLine("        static void Local()");
            builder.AppendLine("        {");
            builder.AppendLine("        C c = new C()");
            for (int i = 0; i < nestingLevel; i++)
            {
                builder.AppendLine($"            .F({i})");
            }
            builder.AppendLine("            ;");
            builder.AppendLine("        }");
            builder.AppendLine("        Local();");
            builder.AppendLine("    }");
            builder.AppendLine("}");

            var source = builder.ToString();
            var comp = CreateCompilation(source);
            comp.TestOnlyCompilationData = new NullableWalker.NullableAnalysisData(maxRecursionDepth: nestingLevel / 2);
            comp.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(NoIOperationValidation), typeof(IsRelease))]
        public void NullableAnalysis_CondAccess_ComplexRightSide()
        {
            var source1 = @"
#nullable enable
object? x = null;
C? c = null;
if (
";
            var source2 = @"
    )
{
}

class C
{
    public bool M(object? obj) => false;
}
";
            var sourceBuilder = new StringBuilder();
            sourceBuilder.Append(source1);
            for (var i = 0; i < 15; i++)
            {
                sourceBuilder.AppendLine($"    c?.M(x = {i}) == (");
            }
            sourceBuilder.AppendLine("    c!.M(x)");

            sourceBuilder.Append("    ");
            for (var i = 0; i < 15; i++)
            {
                sourceBuilder.Append(')');
            }

            sourceBuilder.Append(source2);

            var comp = CreateCompilation(sourceBuilder.ToString());
            comp.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(IsRelease))]
        public void DefiniteAssignment_ManySwitchCasesAndLabels()
        {
            const int nLabels = 1500;

            // #nullable enable
            // class Program
            // {
            //     static int GetIndex() => 0;
            //     static void Main()
            //     {
            //         int index = 0;
            //         int tmp1;
            //         int tmp2; // unused
            //         goto L1498;
            // L0:
            //         if (index < 64) goto LSwitch;
            // L1:
            //         tmp1 = GetIndex();
            //         if (index != tmp1)
            //         {
            //             if (index < 64) goto LSwitch;
            //             goto L0;
            //         }
            // // repeat for L2:, ..., L1498:
            // // ...
            // L1499:
            //         tmp1 = GetIndex();
            //         return;
            // LSwitch:
            //         int tmp3 = index + 1;
            //         switch (GetIndex())
            //         {
            //             case 0:
            //                 index++;
            //                 goto L0;
            //             // repeat for case 1:, ..., case 1499:
            //             // ...
            //             default:
            //                 break;
            //         }
            //     }
            // }

            var builder = new StringBuilder();
            builder.AppendLine("#nullable enable");
            builder.AppendLine("class Program");
            builder.AppendLine("{");
            builder.AppendLine("    static int GetIndex() => 0;");
            builder.AppendLine("    static void Main()");
            builder.AppendLine("    {");
            builder.AppendLine("        int index = 0;");
            builder.AppendLine("        int tmp1;");
            builder.AppendLine("        int tmp2; // unused");
            builder.AppendLine($"        goto L{nLabels - 2};");
            builder.AppendLine("L0:");
            builder.AppendLine("        if (index < 64) goto LSwitch;");
            for (int i = 0; i < nLabels - 2; i++)
            {
                builder.AppendLine($"L{i + 1}:");
                builder.AppendLine("        tmp1 = GetIndex();");
                builder.AppendLine("        if (index != tmp1)");
                builder.AppendLine("        {");
                builder.AppendLine("            if (index < 64) goto LSwitch;");
                builder.AppendLine($"            goto L{i};");
                builder.AppendLine("        }");
            }
            builder.AppendLine($"L{nLabels - 1}:");
            builder.AppendLine("        tmp1 = GetIndex();");
            builder.AppendLine("        return;");
            builder.AppendLine("LSwitch:");
            builder.AppendLine("        int tmp3 = index + 1;");
            builder.AppendLine("        switch (GetIndex())");
            builder.AppendLine("        {");
            for (int i = 0; i < nLabels; i++)
            {
                builder.AppendLine($"            case {i}:");
                builder.AppendLine("                index++;");
                builder.AppendLine($"                goto L{i};");
            }
            builder.AppendLine("            default:");
            builder.AppendLine("                break;");
            builder.AppendLine("        }");
            builder.AppendLine("    }");
            builder.AppendLine("}");

            var source = builder.ToString();
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (9,13): warning CS0168: The variable 'tmp2' is declared but never used
                //         int tmp2; // unused
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "tmp2").WithArguments("tmp2").WithLocation(9, 13));
        }

        [ConditionalFact(typeof(IsRelease))]
        [WorkItem("https://github.com/dotnet/roslyn/issues/67926")]
        public void ExtensionOverloadsDistinctClasses_01()
        {
            const int n = 1000;

            var builder = new StringBuilder();
            builder.AppendLine(
                $$"""
                class Program
                {
                    static void Main()
                    {
                        var o = new object();
                        var c = new C1();
                        o.F(c, c => o.F(c, null));
                    }
                }
                """);

            for (int i = 0; i < n; i++)
            {
                builder.AppendLine(
                    $$"""
                    class C{{i}} { }
                    static class E{{i}}
                    {
                        public static void F(this object o, C{{i}} c, System.Action<C{{i}}> a) { }
                    }
                    """);
            }

            string source = builder.ToString();
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
        }

        [ConditionalFact(typeof(IsRelease))]
        [WorkItem("https://github.com/dotnet/roslyn/issues/67926")]
        public void ExtensionOverloadsDistinctClasses_02()
        {
            const int n = 1000;

            var builder = new StringBuilder();
            builder.AppendLine(
                $$"""
                class Program
                {
                    static void Main()
                    {
                        var o = new object();
                        o.F(null, c => o.F(c, null));
                    }
                }
                """);

            for (int i = 0; i < n; i++)
            {
                builder.AppendLine(
                    $$"""
                    class C{{i}} { }
                    static class E{{i}}
                    {
                        public static void F(this object o, C{{i}} c, System.Action<C{{i}}> a) { }
                    }
                    """);
            }

            string source = builder.ToString();
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (6,11): error CS0121: The call is ambiguous between the following methods or properties: 'E0.F(object, C0, Action<C0>)' and 'E1.F(object, C1, Action<C1>)'
                //         o.F(null, c => o.F(c, null));
                Diagnostic(ErrorCode.ERR_AmbigCall, "F").WithArguments("E0.F(object, C0, System.Action<C0>)", "E1.F(object, C1, System.Action<C1>)").WithLocation(6, 11),
                // (6,21): info CS9236: Compiling requires binding the lambda expression at least 1000 times. Consider declaring the lambda expression with explicit parameter types, or if the containing method call is generic, consider using explicit type arguments.
                //         o.F(null, c => o.F(c, null));
                Diagnostic(ErrorCode.INF_TooManyBoundLambdas, "=>").WithArguments("1000").WithLocation(6, 21));
        }

        [ConditionalFact(typeof(IsRelease))]
        [WorkItem("https://github.com/dotnet/roslyn/issues/67926")]
        public void ExtensionOverloadsDistinctClasses_03()
        {
            const int n = 1000;

            var builder = new StringBuilder();
            builder.AppendLine(
                $$"""
                class Program
                {
                    static void Main()
                    {
                        var o = new object();
                        var c = new C1();
                        o.F(c, c => { o.F( });
                    }
                }
                """);

            for (int i = 0; i < n; i++)
            {
                builder.AppendLine(
                    $$"""
                    class C{{i}} { }
                    static class E{{i}}
                    {
                        public static void F(this object o, C{{i}} c, System.Action<C{{i}}> a) { }
                    }
                    """);
            }

            string source = builder.ToString();
            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (7,18): info CS9236: Compiling requires binding the lambda expression at least 1000 times. Consider declaring the lambda expression with explicit parameter types, or if the containing method call is generic, consider using explicit type arguments.
                //         o.F(c, c => { o.F( });
                Diagnostic(ErrorCode.INF_TooManyBoundLambdas, "=>").WithArguments("1000").WithLocation(7, 18),
                // (7,25): error CS1501: No overload for method 'F' takes 0 arguments
                //         o.F(c, c => { o.F( });
                Diagnostic(ErrorCode.ERR_BadArgCount, "F").WithArguments("F", "0").WithLocation(7, 25),
                // (7,28): error CS1026: ) expected
                //         o.F(c, c => { o.F( });
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "}").WithLocation(7, 28),
                // (7,28): error CS1002: ; expected
                //         o.F(c, c => { o.F( });
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "}").WithLocation(7, 28));

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var expr = tree.GetCompilationUnitRoot().DescendantNodes().OfType<Syntax.InvocationExpressionSyntax>().Last();
            Assert.Equal("o.F( ", expr.ToString());
            _ = model.GetTypeInfo(expr);
        }

        [Fact]
        public void ExtensionOverloadsDistinctClasses_04()
        {
            // public abstract class A
            // {
            //     public static void F1(this object obj) { }
            //     public static void F3(this object obj) { }
            // }
            // public abstract class B : A
            // {
            //     public static void F2(this object obj) { }
            //     public static void F3(this object obj) { }
            // }
            string sourceA = """
                .assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89) }
                .assembly extern System.Core { }
                .assembly '<<GeneratedFileName>>'
                {
                    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
                }
                .class public abstract A
                {
                  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
                  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
                  .method public static void F1(object o)
                  {
                    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
                    ret
                  }
                  .method public static void F3(object o)
                  {
                    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
                    ret
                  }
                }
                .class public abstract B extends A
                {
                  .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
                  .method public hidebysig specialname rtspecialname instance void .ctor() { ret }
                  .method public static void F2(object o)
                  {
                    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
                    ret
                  }
                  .method public static void F3(object o)
                  {
                    .custom instance void [System.Core]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = ( 01 00 00 00 )
                    ret
                  }
                }
                """;
            var refA = CompileIL(sourceA, prependDefaultHeader: false);

            // The calls to B.F3(o) and o.F3() should bind to B.F3 and should not be
            // considered ambiguous with A.F3 because B is derived from A.
            string sourceB = """
                class Program
                {
                    static void M(object o)
                    {
                        B.F1(o);
                        B.F2(o);
                        B.F3(o);
                        o.F1();
                        o.F2();
                        o.F3();
                    }
                }
                """;
            var comp = CreateCompilation(sourceB, references: new[] { refA });
            comp.VerifyDiagnostics();
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var exprs = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().ToImmutableArray();
            var containingTypes = exprs.SelectAsArray(e => model.GetSymbolInfo(e).Symbol.ContainingSymbol).ToTestDisplayStrings();
            Assert.Equal(new[] { "A", "B", "B", "A", "B", "B" }, containingTypes);
        }
    }
}
