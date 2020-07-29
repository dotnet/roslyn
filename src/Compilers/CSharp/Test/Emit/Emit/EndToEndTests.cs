// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Test.Utilities;
using System;
using System.Text;
using Xunit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Emit
{
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
            int numberFluentCalls = (ExecutionConditionUtil.Architecture, ExecutionConditionUtil.Configuration) switch
            {
                (ExecutionArchitecture.x86, ExecutionConfiguration.Debug) => 510,
                (ExecutionArchitecture.x86, ExecutionConfiguration.Release) => 1310,
                (ExecutionArchitecture.x64, ExecutionConfiguration.Debug) => 225,
                (ExecutionArchitecture.x64, ExecutionConfiguration.Release) => 620,
                _ => throw new Exception($"Unexpected configuration {ExecutionConditionUtil.Architecture} {ExecutionConditionUtil.Configuration}")
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
        new C()
");
                for (int i = 0; i < depth; i++)
                {
                    builder.AppendLine(@"            .M(""test"")");
                }
                builder.AppendLine(
                   @"            .M(""test"");
    }
}");

                var source = builder.ToString();
                RunInThread(() =>
                {
                    var options = new CSharpCompilationOptions(outputKind: OutputKind.DynamicallyLinkedLibrary, concurrentBuild: false);
                    var compilation = CreateCompilation(source, options: options);
                    compilation.VerifyDiagnostics();
                    compilation.EmitToArray();
                });
            }
        }

        [Fact]
        [WorkItem(33909, "https://github.com/dotnet/roslyn/issues/33909")]
        [WorkItem(34880, "https://github.com/dotnet/roslyn/issues/34880")]
        public void DeeplyNestedGeneric()
        {
            int nestingLevel = (ExecutionConditionUtil.Architecture, ExecutionConditionUtil.Configuration) switch
            {
                // Legacy baselines are indicated by comments
                (ExecutionArchitecture.x64, ExecutionConfiguration.Debug) when ExecutionConditionUtil.IsMacOS => 180, // 100
                (ExecutionArchitecture.x64, ExecutionConfiguration.Release) when ExecutionConditionUtil.IsMacOS => 520, // 100
                _ when ExecutionConditionUtil.IsCoreClrUnix => 1200, // 1200
                _ when ExecutionConditionUtil.IsMonoDesktop => 730, // 730
                (ExecutionArchitecture.x86, ExecutionConfiguration.Debug) => 450, // 270
                (ExecutionArchitecture.x86, ExecutionConfiguration.Release) => 1290, // 1290
                (ExecutionArchitecture.x64, ExecutionConfiguration.Debug) => 250, // 170
                (ExecutionArchitecture.x64, ExecutionConfiguration.Release) => 730, // 730
                _ => throw new Exception($"Unexpected configuration {ExecutionConditionUtil.Architecture} {ExecutionConditionUtil.Configuration}")
            };

            // Un-comment loop below and use above commands to figure out the new limits
            // for (int i = nestingLevel; i < int.MaxValue; i = i + 10)
            // {
            //     var start = DateTime.UtcNow;
            //     Console.Write($"Depth: {i}");
            //     runDeeplyNestedGenericTest(i);
            //     Console.WriteLine($" - {DateTime.UtcNow - start}");
            // }

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

        [ConditionalFact(typeof(WindowsOrLinuxOnly))]
        public void NestedIfStatements()
        {
            int nestingLevel = (ExecutionConditionUtil.Architecture, ExecutionConditionUtil.Configuration) switch
            {
                (ExecutionArchitecture.x86, ExecutionConfiguration.Debug) => 310,
                (ExecutionArchitecture.x86, ExecutionConfiguration.Release) => 1650,
                (ExecutionArchitecture.x64, ExecutionConfiguration.Debug) => 200,
                (ExecutionArchitecture.x64, ExecutionConfiguration.Release) => 780,
                _ => throw new Exception($"Unexpected configuration {ExecutionConditionUtil.Architecture} {ExecutionConditionUtil.Configuration}")
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
            int n = (ExecutionConditionUtil.Architecture, ExecutionConditionUtil.Configuration) switch
            {
                (ExecutionArchitecture.x86, ExecutionConfiguration.Debug) => 420,
                (ExecutionArchitecture.x86, ExecutionConfiguration.Release) => 1100,
                (ExecutionArchitecture.x64, ExecutionConfiguration.Debug) => 200,
                (ExecutionArchitecture.x64, ExecutionConfiguration.Release) => 520,
                _ => throw new Exception($"Unexpected configuration {ExecutionConditionUtil.Architecture} {ExecutionConditionUtil.Configuration}")
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
    }
}
