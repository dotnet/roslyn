// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Test.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;

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

        [Fact(Skip = "Test is too delicate to fix up for a demo")]
        [WorkItem(33909, "https://github.com/dotnet/roslyn/issues/33909")]
        [WorkItem(34880, "https://github.com/dotnet/roslyn/issues/34880")]
        public void DeeplyNestedGeneric()
        {
            int nestingLevel = (ExecutionConditionUtil.Architecture, ExecutionConditionUtil.Configuration) switch
            {
                _ when ExecutionConditionUtil.IsMacOS => 100,
                _ when ExecutionConditionUtil.IsCoreClrUnix => 1200,
                _ when ExecutionConditionUtil.IsMonoDesktop => 730,
                (ExecutionArchitecture.x86, ExecutionConfiguration.Debug) => 270,
                (ExecutionArchitecture.x86, ExecutionConfiguration.Release) => 1290,
                (ExecutionArchitecture.x64, ExecutionConfiguration.Debug) => 170,
                (ExecutionArchitecture.x64, ExecutionConfiguration.Release) => 730,
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
                    var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
                    compilation.VerifyDiagnostics();

                    // PEVerify is skipped here as it doesn't scale to this level of nested generics. After 
                    // about 600 levels of nesting it will not return in any reasonable amount of time.
                    CompileAndVerify(compilation, expectedOutput: "Pass", verify: Verification.Skipped);
                });
            }
        }
    }
}
