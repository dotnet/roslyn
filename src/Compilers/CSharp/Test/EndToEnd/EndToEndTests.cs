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
        private static void RunInThread(Action action)
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
            thread.Join();

            if (exception is object)
            {
                throw exception;
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

        [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1819416")]
        public void LongInitializerList()
        {
            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            int iterations = 0;
            try
            {
                initializerTest(cts.Token, ref iterations);
            }
            catch (Exception e) when (e is OperationCanceledException or TaskCanceledException)
            {
                Assert.True(false, $"Test timed out while getting all semantic info for long initializer list. Got to {iterations} iterations.");
            }

            static void initializerTest(CancellationToken ct, ref int iterationReached)
            {
                var sb = new StringBuilder();
                sb.AppendLine("""
                    _ = new System.Collections.Generic.Dictionary<string, string>
                    {
                    """);

                for (int i = 0; i < 50000; i++)
                {
                    sb.AppendLine("""    { "a", "b" },""");
                }

                sb.AppendLine("};");

                var comp = CreateCompilation(sb.ToString());
                comp.VerifyEmitDiagnostics();

                var tree = comp.SyntaxTrees[0];
                var model = comp.GetSemanticModel(tree);

                // If we regress perf here, this test will time out. The original condition here was a O(n^2) algorithm because the syntactic parent of each literal
                // was being rebound on every call to GetTypeInfo.
                iterationReached = 0;
                foreach (var literal in tree.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>())
                {
                    ct.ThrowIfCancellationRequested();
                    iterationReached++;
                    var type = model.GetTypeInfo(literal).Type;
                    Assert.Equal(SpecialType.System_String, type.SpecialType);
                }
            }
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

            var verifier = CompileAndVerify(files.ToArrayAndFree(), parseOptions: TestOptions.Regular.WithFeature("InterceptorsPreview"), expectedOutput: makeExpectedOutput());
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
    }
}
