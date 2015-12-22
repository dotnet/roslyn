using System.IO;
using Roslyn.Test.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    public class EndToEndDeterminismTest: TestBase
    {
        private string _flags = "/shared /deterministic+ /nologo /t:library /pdb:none";

        /// <summary>
        /// Compiles some source code and returns the bytes that were contained in the compiled DLL file.
        /// 
        /// Each time that this function is called, it will be compiled in a different directory.
        /// 
        /// The default flags are "/shared /deterministic+ /nologo /t:library".
        /// </summary>
        /// <param name="source"> The source code for the program that will be compiled </param>
        /// <param name="additionalFlags"> A string containing any additional compiler flags </param>
        /// <returns> An array of bytes that were read from the compiled DLL</returns>
        private byte[] CompileAndGetBytes(string source, string additionalFlags, out string finalFlags)
        {

            // Setup
            var tempDir = Temp.CreateDirectory();
            var srcFile = tempDir.CreateFile("test.cs").WriteAllText(source).Path;
            var outFile = srcFile.Replace("test.cs", "test.dll");

            finalFlags = $"{ _flags } { additionalFlags } /pathmap:{tempDir.Path}=/";
            try
            {
                var errorsFile = srcFile + ".errors";

                // Compile
                var result = ProcessUtilities.Run("cmd", $"/C {CompilerServerUnitTests.CSharpCompilerClientExecutable} { finalFlags } { srcFile } /out:{ outFile } > { errorsFile }");
                if (result.ExitCode != 0)
                {
                    var errors = File.ReadAllText(errorsFile);
                    AssertEx.Fail($"Deterministic compile failed \n stderr: { result.Errors } \n stdout:  { errors }");
                }
                var bytes = File.ReadAllBytes(outFile);
                AssertEx.NotNull(bytes);

                return bytes;
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
        /// <param name="additionalFlags"> A string containing any additional compiler flags </param>
        private void RunDeterministicTest(string source, string additionalFlags = "")
        {
            string finalFlags1;
            string finalFlags2;

            var first = CompileAndGetBytes(source, additionalFlags, out finalFlags1);
            var second = CompileAndGetBytes(source, additionalFlags, out finalFlags2);
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
        public void HelloWorld()
        {
            var source = @"using System;
class Hello
{
    static void Main()
    {
        Console.WriteLine(""Hello, world.""); 
    }
}";

            RunDeterministicTest(source);
        }

        [Fact]
        public void CallerInfo()
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
            RunDeterministicTest(source);
        }

        [Fact]
        public void AnonType()
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
            RunDeterministicTest(source);
        }

        [Fact]
        public void LineDirective()
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
            RunDeterministicTest(source);
        }

    }
}
