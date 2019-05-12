// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    public class EndToEndDeterminismTest : TestBase
    {
        private string _flags = "/deterministic+ /nologo /t:library /pdb:none";

        /// <summary>
        /// Compiles some source code and returns the bytes that were contained in the compiled DLL file.
        /// 
        /// Each time that this function is called, it will be compiled in a different directory.
        /// 
        /// The default flags are "/shared /deterministic+ /nologo /t:library".
        /// </summary>
        /// <param name="source"> The source code for the program that will be compiled </param>
        /// <returns> An array of bytes that were read from the compiled DLL</returns>
        private async Task<(byte[] assemblyBytes, string finalFlags)> CompileAndGetBytes(string source)
        {
            // Setup
            var tempDir = Temp.CreateDirectory();
            var srcFile = tempDir.CreateFile("test.cs").WriteAllText(source).Path;
            var outFile = srcFile.Replace("test.cs", "test.dll");

            try
            {
                string finalFlags = null;
                using (var serverData = await ServerUtil.CreateServer())
                {
                    finalFlags = $"{ _flags } /shared:{ serverData.PipeName } /pathmap:{tempDir.Path}=/ /out:{ outFile } { srcFile }";
                    var result = CompilerServerUnitTests.RunCommandLineCompiler(
                        CompilerServerUnitTests.CSharpCompilerClientExecutable,
                        finalFlags,
                        currentDirectory: tempDir);
                    if (result.ExitCode != 0)
                    {
                        AssertEx.Fail($"Deterministic compile failed \n stdout:  { result.Output }");
                    }
                    await serverData.Verify(connections: 1, completed: 1).ConfigureAwait(true);
                }
                var bytes = File.ReadAllBytes(outFile);
                AssertEx.NotNull(bytes);

                return (bytes, finalFlags);
            }
            finally
            {
                File.Delete(srcFile);
                File.Delete(outFile);
            }
        }

        /// <summary>
        /// Runs CompileAndGetBytes twice and compares the output. 
        /// </summary>
        /// <param name="source"> The source of the program that will be compiled </param>
        private async Task RunDeterministicTest(string source)
        {
            var (first, finalFlags1) = await CompileAndGetBytes(source);
            var (second, finalFlags2) = await CompileAndGetBytes(source);
            Assert.Equal(first.Length, second.Length);
            for (int i = 0; i < first.Length; i++)
            {
                if (first[i] != second[i])
                {
                    AssertEx.Fail($"Bytes were different at position { i } ({ first[i] } vs { second[i] }).  Flags used were (\"{ finalFlags1 }\" vs \"{ finalFlags2 }\")");
                }
            }
        }

        [Fact]
        public async Task HelloWorld()
        {
            var source = @"using System;
class Hello
{
    static void Main()
    {
        Console.WriteLine(""Hello, world.""); 
    }
}";

            await RunDeterministicTest(source);
        }

        [Fact]
        public async Task CallerInfo()
        {
            var source = @"using System;
class CallerInfo {
    public static void DoProcessing()
    {
        TraceMessage(""Something happened."");
    }
    public static void TraceMessage(string message,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = """",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = """",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
    {
        System.Diagnostics.Trace.WriteLine(""message: "" + message);
        System.Diagnostics.Trace.WriteLine(""member name: "" + memberName);
        System.Diagnostics.Trace.WriteLine(""source file path: "" + sourceFilePath);
        System.Diagnostics.Trace.WriteLine(""source line number: "" + sourceLineNumber);
    }
    static void Main() {
        DoProcessing();
    }
}";
            await RunDeterministicTest(source);
        }

        [Fact]
        public async Task AnonType()
        {
            var source = @"using System;
class AnonType {
    public static void Main() {
        var person = new { firstName = ""john"", lastName = ""Smith"" };
        var date = new { year = 2015, month = ""jan"", day = 5 };
        var color = new { red = 255, green = 125, blue = 0 };
        
        Console.WriteLine(person);
        Console.WriteLine(date);
        Console.WriteLine(color);
    }
}";
            await RunDeterministicTest(source);
        }

        [Fact]
        public async Task LineDirective()
        {
            var source = @"using System;
class CallerInfo {
    public static void TraceMessage(string message,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = """",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = """",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
    {
        System.Diagnostics.Trace.WriteLine(""message: "" + message);
        System.Diagnostics.Trace.WriteLine(""member name: "" + memberName);
        System.Diagnostics.Trace.WriteLine(""source file path: "" + sourceFilePath);
        System.Diagnostics.Trace.WriteLine(""source line number: "" + sourceLineNumber);
    }
    static void Main() {
        TraceMessage(""from main"");
#line 10 ""coolFile.cs""
        TraceMessage(""from the cool file"");
#line default
        TraceMessage(""back in main"");
    }
}";
            await RunDeterministicTest(source);
        }
    }
}
