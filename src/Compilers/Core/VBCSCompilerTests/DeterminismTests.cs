using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    class DeterminismTests: TestBase
    {
        /*
        [Fact]
        public void TestDeterminism()
        {
            var srcFile = _tempDirectory.CreateFile("test.cs").WriteAllText("♕").Path;
            var tempOut = _tempDirectory.CreateFile("output.txt");

            // Delete VBCSCompiler.exe so /shared is forced to fall back to csc.exe
            File.Delete(_compilerServerExecutable);

            var result = ProcessUtilities.Run("cmd",
                string.Format("/C {0} /shared /utf8output /nologo /t:library {1} > {2}",
                _csharpCompilerClientExecutable,
                srcFile, tempOut.Path));

            Assert.Equal("", result.Output.Trim());
            Assert.Equal("test.cs(1,1): error CS1056: Unexpected character '♕'".Trim(),
                tempOut.ReadAllText().Trim().Replace(srcFile, "test.cs"));
            Assert.Equal(1, result.ExitCode);
        }
        */
    }
}
