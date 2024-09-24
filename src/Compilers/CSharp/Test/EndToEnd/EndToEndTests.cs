// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Roslyn.Test.Utilities;
using System;
using System.Text;
using Xunit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using Roslyn.Test.Utilities.TestGenerators;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.EndToEnd
{
    [TestCaseOrderer("XUnit.Project.Orderers.AlphabeticalOrderer", "XUnit.Project")]
    public class EndToEndTests : EmitMetadataTestBase
    {
        /// <summary>
        /// These tests are very sensitive to stack size hence we use a fresh thread to ensure there 
        /// is a consistent stack size for them to execute in. 
        /// </summary>
        /// <param name="action"></param>
        private static void RunInThread(Action action, TimeSpan? timeout = null)
        {
            Exception exception = null;
            var thread = new System.Threading.Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            }, 0);

            thread.Start();
            if (timeout is { } t && !Debugger.IsAttached)
            {
                if (!thread.Join(t))
                {
                    throw new TimeoutException(t.ToString());
                }
            }
            else
            {
                thread.Join();
            }

            if (exception is object)
            {
                Assert.False(true, exception.ToString());
            }
        }

        private static void RunTest(int expectedDepth, Action<int> runTest)
        {
            if (runTestAndCatch(expectedDepth))
            {
                return;
            }

            int minDepth = 0;
            int maxDepth = expectedDepth;
            int actualDepth;
            while (true)
            {
                int depth = (maxDepth - minDepth) / 2 + minDepth;
                if (depth <= minDepth)
                {
                    actualDepth = minDepth;
                    break;
                }
                if (depth >= maxDepth)
                {
                    actualDepth = maxDepth;
                    break;
                }
                if (runTestAndCatch(depth))
                {
                    minDepth = depth;
                }
                else
                {
                    maxDepth = depth;
                }
            }
            Assert.Equal(expectedDepth, actualDepth);

            bool runTestAndCatch(int depth)
            {
                try
                {
                    runTest(depth);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        // This test is a canary attempting to make sure that we don't regress the # of fluent calls that 
        // the compiler can handle. 
        [WorkItem(16669, "https://github.com/dotnet/roslyn/issues/16669")]
        [ConditionalFact(typeof(WindowsOrLinuxOnly)), WorkItem(34880, "https://github.com/dotnet/roslyn/issues/34880")]
        public void OverflowOnFluentCall()
        {
            int numberFluentCalls = (IntPtr.Size, ExecutionConditionUtil.Configuration) switch
            {
                (4, ExecutionConfiguration.Debug) => 4000,
                (4, ExecutionConfiguration.Release) => 4000,
                (8, ExecutionConfiguration.Debug) => 4000,
                (8, ExecutionConfiguration.Release) => 4000,
                _ => throw new Exception($"Unexpected configuration {IntPtr.Size * 8}-bit {ExecutionConditionUtil.Configuration}")
            };

            // <path>\xunit.console.exe "<path>\CSharpCompilerEmitTest\Roslyn.Compilers.CSharp.Emit.UnitTests.dll"  -noshadow -verbose -class "Microsoft.CodeAnalysis.CSharp.UnitTests.Emit.EndToEndTests"
            // <path>\xunit.console.x86.exe "<path>\CSharpCompilerEmitTest\Roslyn.Compilers.CSharp.Emit.UnitTests.dll"  -noshadow -verbose -class "Microsoft.CodeAnalysis.CSharp.UnitTests.Emit.EndToEndTests"
            // Un-comment loop below and use above commands to figure out the new limits
            //for (int i = 0; i < numberFluentCalls; i = i + 10)
            //{
            //    Console.WriteLine($"Depth: {i}");
            //    tryCompileDeepFluentCalls(i);
            //}

            tryCompileDeepFluentCalls(numberFluentCalls);

            void tryCompileDeepFluentCalls(int depth)
            {
                var builder = new StringBuilder();
                builder.AppendLine(
        @"class C {
    C M(string x) { return this; }
    void M2() {
        global::C.GetC()
");
                for (int i = 0; i < depth; i++)
                {
                    builder.AppendLine(@"            .M(""test"")");
                }
                builder.AppendLine(
                   @"            .M(""test"");
    }

    static C GetC() => new C();
}
");

                var source = builder.ToString();
                RunInThread(() =>
                {
                    var options = TestOptions.DebugDll.WithConcurrentBuild(false);
                    var compilation = CreateCompilation(source, options: options);
                    compilation.VerifyDiagnostics();
                    compilation.EmitToArray();
                });
            }
        }

        // This test is a canary attempting to make sure that we don't regress the # of fluent calls that 
        // the compiler can handle. 
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/72678"), WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1874763")]
        public void OverflowOnFluentCall_ExtensionMethods()
        {
            int numberFluentCalls = (IntPtr.Size, ExecutionConditionUtil.Configuration, RuntimeUtilities.IsDesktopRuntime, RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) switch
            {
                (8, ExecutionConfiguration.Debug, false, false) => 750,
                (8, ExecutionConfiguration.Release, false, false) => 750, // Should be ~3_400, but is flaky.
                (4, ExecutionConfiguration.Debug, true, false) => 450,
                (4, ExecutionConfiguration.Release, true, false) => 1_600,
                (8, ExecutionConfiguration.Debug, true, false) => 1_100,
                (8, ExecutionConfiguration.Release, true, false) => 3_300,
                (_, _, _, true) => 200,
                _ => throw new Exception($"Unexpected configuration {IntPtr.Size * 8}-bit {ExecutionConditionUtil.Configuration}, Desktop: {RuntimeUtilities.IsDesktopRuntime}")
            };

            // Un-comment the call below to figure out the new limits.
            //testLimits();

            try
            {
                tryCompileDeepFluentCalls(numberFluentCalls);
            }
            catch (Exception e)
            {
                testLimits(e);
            }

            void testLimits(Exception innerException = null)
            {
                for (int i = 0; i < int.MaxValue; i += 10)
                {
                    try
                    {
                        tryCompileDeepFluentCalls(i);
                    }
                    catch (Exception e)
                    {
                        if (innerException != null)
                        {
                            e = new AggregateException(e, innerException);
                        }

                        throw new Exception($"Depth: {i}, Bytes: {IntPtr.Size}, Config: {ExecutionConditionUtil.Configuration}, Desktop: {RuntimeUtilities.IsDesktopRuntime}", e);
                    }
                }
            }

            void tryCompileDeepFluentCalls(int depth)
            {
                var builder = new StringBuilder();
                builder.AppendLine("""
                    static class E
                    {
                        public static C M(this C c, string x) { return c; }
                    }
                    class C
                    {
                        static C GetC() => new C();
                        void M2()
                        {
                            GetC()
                    """);
                for (int i = 0; i < depth; i++)
                {
                    builder.AppendLine(""".M("test")""");
                }
                builder.AppendLine("""; } }""");

                var source = builder.ToString();
                RunInThread(() =>
                {
                    var options = TestOptions.DebugDll.WithConcurrentBuild(false);
                    var compilation = CreateCompilation(source, options: options);
                    compilation.VerifyEmitDiagnostics();
                });
            }
        }

        [ConditionalFact(typeof(WindowsOrLinuxOnly))]
        [WorkItem(33909, "https://github.com/dotnet/roslyn/issues/33909")]
        [WorkItem(34880, "https://github.com/dotnet/roslyn/issues/34880")]
        [WorkItem(53361, "https://github.com/dotnet/roslyn/issues/53361")]
        public void DeeplyNestedGeneric()
        {
            int nestingLevel = (IntPtr.Size, ExecutionConditionUtil.Configuration) switch
            {
                // Legacy baselines are indicated by comments
                (4, ExecutionConfiguration.Debug) => 370, // 270
                (4, ExecutionConfiguration.Release) => 1290, // 1290
                (8, ExecutionConfiguration.Debug) => 270, // 170
                (8, ExecutionConfiguration.Release) => 730, // 730
                _ => throw new Exception($"Unexpected configuration {IntPtr.Size * 8}-bit {ExecutionConditionUtil.Configuration}")
            };

            // Un-comment loop below and use above commands to figure out the new limits
            //Console.WriteLine($"Using architecture: {ExecutionConditionUtil.Architecture}, configuration: {ExecutionConditionUtil.Configuration}");
            //for (int i = nestingLevel; i < int.MaxValue; i = i + 10)
            //{
            //    var start = DateTime.UtcNow;
            //    Console.Write($"Depth: {i}");
            //    runDeeplyNestedGenericTest(i);
            //    Console.WriteLine($" - {DateTime.UtcNow - start}");
            //}

            runDeeplyNestedGenericTest(nestingLevel);

            void runDeeplyNestedGenericTest(int nestingLevel)
            {
                var builder = new StringBuilder();
                builder.AppendLine(@"
#pragma warning disable 168 // Unused local
using System;

public class Test
{
    public static void Main(string[] args)
    {
");

                for (var i = 0; i < nestingLevel; i++)
                {
                    if (i > 0)
                    {
                        builder.Append('.');
                    }
                    builder.Append($"MyStruct{i}<int>");
                }

                builder.AppendLine(" local;");
                builder.AppendLine(@"
        Console.WriteLine(""Pass"");
    }
}");

                for (int i = 0; i < nestingLevel; i++)
                {
                    builder.AppendLine($"public struct MyStruct{i}<T{i}> {{");
                }
                for (int i = 0; i < nestingLevel; i++)
                {
                    builder.AppendLine("}");
                }

                var source = builder.ToString();
                RunInThread(() =>
                {
                    var compilation = CreateCompilation(source, options: TestOptions.DebugExe.WithConcurrentBuild(false));
                    compilation.VerifyDiagnostics();

                    // PEVerify is skipped here as it doesn't scale to this level of nested generics. After 
                    // about 600 levels of nesting it will not return in any reasonable amount of time.
                    CompileAndVerify(compilation, expectedOutput: "Pass", verify: Verification.Skipped);
                });
            }
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/69515")]
        public void GenericInheritanceCascade_CSharp(bool reverse, bool concurrent)
        {
            const int number = 17;

            /*
                class C0<T>;
                class C1<T> : C0<T>;
                class C2<T> : C1<T>;
                ...
            */
            var declarations = new string[number];
            declarations[0] = "class C0<T0> { }";
            for (int i = 1; i < number; i++)
            {
                declarations[i] = $$"""class C{{i}}<T{{i}}> : C{{i - 1}}<T{{i}}> { }""";
            }

            if (reverse)
            {
                Array.Reverse(declarations);
            }

            var source = string.Join(Environment.NewLine, declarations);
            var options = TestOptions.DebugDll.WithConcurrentBuild(concurrent);

            RunInThread(() =>
            {
                CompileAndVerify(source, options: options).VerifyDiagnostics();
            }, timeout: TimeSpan.FromSeconds(10));
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/69515")]
        public void GenericInheritanceCascade_VisualBasic(bool reverse, bool concurrent)
        {
            const int number = 17;

            /*
                Class C0(Of T)
                End Class
                Class C1(Of T)
                    Inherits C0(Of T)
                End Class
                Class C2(Of T)
                    Inherits C1(Of T)
                End Class
                ...
            */
            var declarations = new string[number];
            declarations[0] = """
                Class C0(Of T0)
                End Class
                """;
            for (int i = 1; i < number; i++)
            {
                declarations[i] = $"""
                    Class C{i}(Of T{i})
                        Inherits C{i - 1}(Of T{i})
                    End Class
                    """;
            }

            if (reverse)
            {
                Array.Reverse(declarations);
            }

            var source = string.Join(Environment.NewLine, declarations);
            var options = new VisualBasic.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithConcurrentBuild(concurrent);

            RunInThread(() =>
            {
                CreateVisualBasicCompilation(source, compilationOptions: options).VerifyDiagnostics();
            }, timeout: TimeSpan.FromSeconds(10));
        }

        [ConditionalFact(typeof(WindowsOrLinuxOnly), typeof(NoIOperationValidation))]
        public void NestedIfStatements()
        {
            int nestingLevel = (IntPtr.Size, ExecutionConditionUtil.Configuration) switch
            {
                (4, ExecutionConfiguration.Debug) => 310,
                (4, ExecutionConfiguration.Release) => 1650,
                (8, ExecutionConfiguration.Debug) => 200,
                (8, ExecutionConfiguration.Release) => 780,
                _ => throw new Exception($"Unexpected configuration {IntPtr.Size * 8}-bit {ExecutionConditionUtil.Configuration}")
            };

            RunTest(nestingLevel, runTest);

            static void runTest(int nestingLevel)
            {
                var builder = new StringBuilder();
                builder.AppendLine(
@"class Program
{
    static bool F(int i) => true;
    static void Main()
    {");
                for (int i = 0; i < nestingLevel; i++)
                {
                    builder.AppendLine(
$@"        if (F({i}))
        {{");
                }
                for (int i = 0; i < nestingLevel; i++)
                {
                    builder.AppendLine("        }");
                }
                builder.AppendLine(
@"    }
}");
                var source = builder.ToString();
                RunInThread(() =>
                {
                    var comp = CreateCompilation(source, options: TestOptions.DebugDll.WithConcurrentBuild(false));
                    comp.VerifyDiagnostics();
                });
            }
        }

        [WorkItem(42361, "https://github.com/dotnet/roslyn/issues/42361")]
        [ConditionalFact(typeof(WindowsOrLinuxOnly))]
        public void Constraints()
        {
            int n = (IntPtr.Size, ExecutionConditionUtil.Configuration) switch
            {
                (4, ExecutionConfiguration.Debug) => 420,
                (4, ExecutionConfiguration.Release) => 1100,
                (8, ExecutionConfiguration.Debug) => 180,
                (8, ExecutionConfiguration.Release) => 400,
                _ => throw new Exception($"Unexpected configuration {IntPtr.Size * 8}-bit {ExecutionConditionUtil.Configuration}")
            };

            RunTest(n, runTest);

            static void runTest(int n)
            {
                // class C0<T> where T : C1<T> { }
                // class C1<T> where T : C2<T> { }
                // ...
                // class CN<T> where T : C0<T> { }
                var sourceBuilder = new StringBuilder();
                var diagnosticsBuilder = ArrayBuilder<DiagnosticDescription>.GetInstance();
                for (int i = 0; i <= n; i++)
                {
                    int next = (i == n) ? 0 : i + 1;
                    sourceBuilder.AppendLine($"class C{i}<T> where T : C{next}<T> {{ }}");
                    diagnosticsBuilder.Add(Diagnostic(ErrorCode.ERR_GenericConstraintNotSatisfiedRefType, "T").WithArguments($"C{i}<T>", $"C{next}<T>", "T", "T"));
                }
                var source = sourceBuilder.ToString();
                var diagnostics = diagnosticsBuilder.ToArrayAndFree();

                RunInThread(() =>
                {
                    var comp = CreateCompilation(source, options: TestOptions.DebugDll.WithConcurrentBuild(false));
                    var type = comp.GetMember<NamedTypeSymbol>("C0");
                    var typeParameter = type.TypeParameters[0];
                    Assert.True(typeParameter.IsReferenceType);
                    comp.VerifyDiagnostics(diagnostics);
                });
            }
        }

        [ConditionalFact(typeof(WindowsOrMacOSOnly), Reason = "https://github.com/dotnet/roslyn/issues/69210"), WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1819416")]
        public void LongInitializerList()
        {
            var sb = new StringBuilder();
            sb.AppendLine("""
                    _ = new System.Collections.Generic.Dictionary<string, string>
                    {
                    """);

            for (int i = 0; i < 100; i++)
            {
                sb.AppendLine("""    { "a", "b" },""");
            }

            sb.AppendLine("};");

            var comp = CreateCompilation(sb.ToString());
            var counter = new MemberSemanticModel.MemberSemanticBindingCounter();
            comp.TestOnlyCompilationData = counter;
            comp.VerifyEmitDiagnostics();
            Assert.Equal(0, counter.BindCount);

            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);

            var literals = tree.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>().ToArray();
            Assert.Equal(200, literals.Length);
            foreach (var literal in literals)
            {
                var type = model.GetTypeInfo(literal).Type;
                Assert.Equal(SpecialType.System_String, type.SpecialType);
            }

            Assert.Equal(1, counter.BindCount);
        }

        [Fact]
        public void Interceptors()
        {
            const int numberOfInterceptors = 10000;

            // write a program which has many intercepted calls.
            // each interceptor is in a different file.
            var files = ArrayBuilder<(string source, string path)>.GetInstance();

            // Build a top-level-statements main like:
            //    C.M();
            //    C.M();
            //    C.M();
            //    ...
            var builder = new StringBuilder();
            for (int i = 0; i < numberOfInterceptors; i++)
            {
                builder.AppendLine("C.M();");
            }

            files.Add((builder.ToString(), "Program.cs"));

            files.Add(("""
                class C
                {
                    public static void M() => throw null!;
                }

                namespace System.Runtime.CompilerServices
                {
                    public class InterceptsLocationAttribute : Attribute
                    {
                        public InterceptsLocationAttribute(string path, int line, int column) { }
                    }
                }
                """, "C.cs"));

            for (int i = 0; i < numberOfInterceptors; i++)
            {
                files.Add(($$"""
                    using System;
                    using System.Runtime.CompilerServices;

                    class C{{i}}
                    {
                        [InterceptsLocation("Program.cs", {{i + 1}}, 3)]
                        public static void M()
                        {
                            Console.WriteLine({{i}});
                        }
                    }
                    """, $"C{i}.cs"));
            }

            var verifier = CompileAndVerify(files.ToArrayAndFree(), parseOptions: TestOptions.Regular.WithFeature("InterceptorsNamespaces", "global"), expectedOutput: makeExpectedOutput());
            verifier.VerifyDiagnostics();

            string makeExpectedOutput()
            {
                builder.Clear();
                for (int i = 0; i < numberOfInterceptors; i++)
                {
                    builder.AppendLine($"{i}");
                }
                return builder.ToString();
            }
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/69093")]
        public void NestedLambdas(bool localFunctions)
        {
            const int overloads1Number = 20;
            const int overloads2Number = 10;

            /*
                interface I0 { }
                // ...
                interface I9 { }
            */
            var builder1 = new StringBuilder();
            var interfacesNumber = Math.Max(overloads1Number, overloads2Number);
            for (int i = 0; i < interfacesNumber; i++)
            {
                builder1.AppendLine($$"""interface I{{i}} { }""");
            }

            /*
                void M1(System.Action<I0> a) { }
                // ...
                void M1(System.Action<I9> a) { }
            */
            var builder2 = new StringBuilder();
            for (int i = 0; i < overloads1Number; i++)
            {
                builder2.AppendLine($$"""void M1(System.Action<I{{i}}> a) { }""");
            }

            /*
                void M2(I0 x, System.Func<string, I0> f) { }
                // ...
                void M2(I9 x, System.Func<string, I9> f) { }
            */
            for (int i = 0; i < overloads2Number; i++)
            {
                builder2.AppendLine($$"""void M2(I{{i}} x, System.Func<string, I{{i}}> f) { }""");
            }

            // Local functions should be similarly fast as lambdas.
            var inner = localFunctions ? """
                M2(x, L0);
                static I0 L0(string arg) {
                    arg = arg + "0";
                    return default;
                }
                """ : """
                M2(x, static I0 (string arg) => {
                    arg = arg + "0";
                    return default;
                });
                """;

            var source = $$"""
                {{builder1}}
                class C
                {
                    {{builder2}}
                    void Main()
                    {
                        M1(x =>
                        {
                            {{inner}}
                        });
                    }
                }
                """;
            RunInThread(() =>
            {
                var comp = CreateCompilation(source, options: TestOptions.DebugDll.WithConcurrentBuild(false));
                var data = new LambdaBindingData();
                comp.TestOnlyCompilationData = data;
                comp.VerifyDiagnostics();
                Assert.Equal(localFunctions ? 20 : 40, data.LambdaBindingCount);
            }, timeout: TimeSpan.FromSeconds(5));
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/pull/70791")]
        public void ForAttributeWithMetadataName_DeepRecursion()
        {
            var deeplyRecursive = string.Join("+", Enumerable.Repeat(""" "a" """, 20_000));
            var source = $$"""
                class Ex
                {
                    void M()
                    {
                        var v ={{deeplyRecursive}};
                    }
                }

                [N1.X]
                class C1 { }
                [N2.X]
                class C2 { }

                namespace N1
                {
                    class XAttribute : System.Attribute { }
                }

                namespace N2
                {
                    class XAttribute : System.Attribute { }
                }
                """;
            var parseOptions = TestOptions.RegularPreview;
            Compilation compilation = CreateCompilation(source, options: TestOptions.DebugDllThrowing, parseOptions: parseOptions);

            Assert.Single(compilation.SyntaxTrees);

            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(ctx =>
            {
                var input = ctx.SyntaxProvider.ForAttributeWithMetadataName(
                    "N1.XAttribute",
                    (node, _) => node is ClassDeclarationSyntax,
                    (context, _) => (ClassDeclarationSyntax)context.TargetNode);
                ctx.RegisterSourceOutput(input, (spc, node) => { });
            }));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new ISourceGenerator[] { generator }, parseOptions: parseOptions, driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
            driver = driver.RunGenerators(compilation);
            var runResult = driver.GetRunResult().Results[0];

            Assert.Collection(runResult.TrackedSteps["result_ForAttributeWithMetadataName"],
                step => Assert.True(step.Outputs.Single().Value is ClassDeclarationSyntax { Identifier.ValueText: "C1" }));
        }

        [Fact]
        public void ManyBinaryPatterns()
        {
            const string Preamble = $"""
                int i = 2;

                System.Console.Write(i is
                """;
            const string Append = $"""

                or 
                """;
            const string Postscript = """

                ? 1 : 0);
                """;

            const int NumBinaryExpressions = 12_000;

            var builder = new StringBuilder(Preamble.Length + Postscript.Length + Append.Length * NumBinaryExpressions + 5 /* Max num digit characters */ * NumBinaryExpressions);

            builder.AppendLine(Preamble);

            builder.Append(0);

            for (int i = 1; i < NumBinaryExpressions; i++)
            {
                builder.Append(Append);
                // Make sure the emitter has to handle lots of nodes
                builder.Append(i * 2);
            }

            builder.AppendLine(Postscript);

            var source = builder.ToString();
            RunInThread(() =>
            {
                var comp = CreateCompilation(source, options: TestOptions.DebugExe.WithConcurrentBuild(false));
                CompileAndVerify(comp, expectedOutput: "1");

                var tree = comp.SyntaxTrees[0];
                var isPattern = tree.GetRoot().DescendantNodes().OfType<IsPatternExpressionSyntax>().Single();
                var model = comp.GetSemanticModel(tree);
                var operation = model.GetOperation(isPattern);
                Assert.NotNull(operation);

                for (; operation.Parent is not null; operation = operation.Parent) { }

                Assert.NotNull(ControlFlowGraph.Create((IMethodBodyOperation)operation));
            });
        }
    }
}
