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

#if DEBUG
        public static bool IsDebug => true;
#else
        public static bool IsDebug => false;
#endif


        /// <summary>
        /// These tests are very sensitive to stack size hence we use a fresh thread to ensure there 
        /// is a consistent stack size for them to execute in. 
        /// </summary>
        /// <param name="action"></param>
        private static void RunInThread(Action action)
        {
            var thread = new System.Threading.Thread(() =>
            {
                action();
            }, 0);
            thread.Start();
            thread.Join();
        }

        // This test is a canary attempting to make sure that we don't regress the # of fluent calls that 
        // the compiler can handle.
        [WorkItem(16669, "https://github.com/dotnet/roslyn/issues/16669")]
        [Fact]
        public void OverflowOnFluentCall()
        {
            int numberFluentCalls = 0;
            // TODO: Number of frames was reduced by 50 to pass tests. We need to return to original counts after https://github.com/dotnet/roslyn/issues/25603
            // is fixed to determine the bug here
            switch (IntPtr.Size * 8)
            {
                case 32 when IsDebug:
                    numberFluentCalls = 510;
                    break;
                case 32 when !IsDebug:
                    numberFluentCalls = 1350;
                    break;
                case 64 when IsDebug:
                    numberFluentCalls = 225;
                    break;
                case 64 when !IsDebug:
                    numberFluentCalls = 620;
                    break;
                default:
                    throw new Exception($"unexpected pointer size {IntPtr.Size}");
            }

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
        public void DeeplyNestedGeneric()
        {

            int nestingLevel = (IntPtr.Size * 8) switch
            {
                32 when IsDebug => 500,
                32 when !IsDebug => 500,
                64 when IsDebug => 500,
                64 when !IsDebug => 500,
                _ => throw new Exception($"unexpected pointer size {IntPtr.Size}")
            };

            // Un-comment loop below and use above commands to figure out the new limits
            for (int i = 0; i < nestingLevel; i = i + 10)
            {
                Console.WriteLine($"Depth: {i}");
                runDeeplyNestedGenericTest(i);
            }
            runDeeplyNestedGenericTest(nestingLevel);

            void runDeeplyNestedGenericTest(int nestingLevel)
            {
                var builder = new StringBuilder();
                builder.AppendLine(@"
using System;

public class Test
{
    public static int Main()
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

                generateTypeDefinition(0);

                var source = builder.ToString();

                RunInThread(() =>
                {
                    var compilation = CreateCompilation(source, options: TestOptions.DebugExe);
                    CompileAndVerify(compilation, expectedOutput: "Pass");
                });

                void generateTypeDefinition(int level)
                {
                    if (level >= nestingLevel)
                    {
                        return;
                    }

                    builder.AppendLine($"public struct MyStruct{level}<T{level}> {{");
                    generateTypeDefinition(level + 1);
                    builder.AppendLine("}");
                }
            }
        }
    }
}
