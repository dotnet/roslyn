// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using static Roslyn.Test.Utilities.SharedResourceHelpers;
using System.Reflection;
using Microsoft.CodeAnalysis.CompilerServer;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    public class TouchedFileLoggingTests : TestBase
    {
        private static readonly string s_libDirectory = Environment.GetEnvironmentVariable("LIB");
        private readonly string _baseDirectory = TempRoot.Root;
        private const string HelloWorldCS = @"using System;

class C
{
    public static void Main(string[] args)
    {
        Console.WriteLine(""Hello, world"");
    }
}";

        private const string HelloWorldVB = @"Imports System
Class C
    Shared Sub Main(args As String())
        Console.WriteLine(""Hello, world"")
    End Sub
End Class
";

        [ConditionalFact(typeof(DesktopOnly))]
        public void CSharpTrivialMetadataCaching()
        {
            var loader = DefaultAnalyzerAssemblyLoader.CreateNonLockingLoader(
#if NET
                loadContext: null,
#endif
                Temp.CreateDirectory().Path);
            var filelist = new List<string>();

            // Do the following compilation twice.
            // The compiler server API should hold on to the mscorlib bits
            // in memory, but the file tracker should still map that it was
            // touched.
            for (int i = 0; i < 2; i++)
            {
                var source1 = Temp.CreateFile().WriteAllText(HelloWorldCS).Path;
                var touchedDir = Temp.CreateDirectory();
                var touchedBase = Path.Combine(touchedDir.Path, "touched");
                var clientDirectory = AppContext.BaseDirectory;

                filelist.Add(source1);
                var outWriter = new StringWriter();
                var cmd = new CSharpCompilerServer(
                    CompilerServerHost.SharedAssemblyReferenceProvider,
                    responseFile: null,
                    new[] { "/nologo", "/touchedfiles:" + touchedBase, source1 },
                    new BuildPaths(clientDirectory, _baseDirectory, RuntimeEnvironment.GetRuntimeDirectory(), Path.GetTempPath()),
                    s_libDirectory,
                    loader,
                    driverCache: null);

                List<string> expectedReads;
                List<string> expectedWrites;
                BuildTouchedFiles(cmd,
                                  Path.ChangeExtension(source1, "exe"),
                                  out expectedReads,
                                  out expectedWrites);

                var exitCode = cmd.Run(outWriter);

                Assert.Equal(string.Empty, outWriter.ToString().Trim());
                Assert.Equal(0, exitCode);

                AssertTouchedFilesEqual(expectedReads,
                                        expectedWrites,
                                        touchedBase);
            }

            foreach (String f in filelist)
            {
                CleanupAllGeneratedFiles(f);
            }
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void VisualBasicTrivialMetadataCaching()
        {
            var loader = DefaultAnalyzerAssemblyLoader.CreateNonLockingLoader(
#if NET
                loadContext: null,
#endif
                Temp.CreateDirectory().Path);
            var filelist = new List<string>();

            // Do the following compilation twice.
            // The compiler server API should hold on to the mscorlib bits
            // in memory, but the file tracker should still map that it was
            // touched.
            for (int i = 0; i < 2; i++)
            {
                var source1 = Temp.CreateFile().WriteAllText(HelloWorldVB).Path;
                var touchedDir = Temp.CreateDirectory();
                var touchedBase = Path.Combine(touchedDir.Path, "touched");
                var clientDirectory = AppContext.BaseDirectory;

                filelist.Add(source1);
                var outWriter = new StringWriter();
                var cmd = new VisualBasicCompilerServer(
                    CompilerServerHost.SharedAssemblyReferenceProvider,
                    responseFile: null,
                    new[] { "/nologo", "/touchedfiles:" + touchedBase, source1 },
                    new BuildPaths(clientDirectory, _baseDirectory, RuntimeEnvironment.GetRuntimeDirectory(), Path.GetTempPath()),
                    s_libDirectory,
                    loader,
                    driverCache: null);

                List<string> expectedReads;
                List<string> expectedWrites;
                BuildTouchedFiles(cmd,
                                  Path.ChangeExtension(source1, "exe"),
                                  out expectedReads,
                                  out expectedWrites);

                var exitCode = cmd.Run(outWriter);

                Assert.Equal(string.Empty, outWriter.ToString().Trim());
                Assert.Equal(0, exitCode);

                AssertTouchedFilesEqual(expectedReads,
                                        expectedWrites,
                                        touchedBase);
            }

            foreach (string f in filelist)
            {
                CleanupAllGeneratedFiles(f);
            }
        }

        /// <summary>
        /// Builds the expected base of touched files.
        /// Adds a hook for temporary file creation as well,
        /// so this method must be called before the execution of
        /// Csc.Run.
        /// </summary>
        private static void BuildTouchedFiles(CommonCompiler cmd,
                                              string outputPath,
                                              out List<string> expectedReads,
                                              out List<string> expectedWrites)
        {
            expectedReads = new List<string>();
            expectedReads.AddRange(cmd.Arguments.MetadataReferences.Select(r => r.Reference));

            if (cmd.Arguments is VisualBasicCommandLineArguments { DefaultCoreLibraryReference: { } reference })
            {
                expectedReads.Add(reference.Reference);
            }

            foreach (var file in cmd.Arguments.SourceFiles)
            {
                expectedReads.Add(file.Path);
            }

            var writes = new List<string>();
            writes.Add(outputPath);

            expectedWrites = writes;
        }

        private static void AssertTouchedFilesEqual(
            List<string> expectedReads,
            List<string> expectedWrites,
            string touchedFilesBase)
        {
            var touchedReadPath = touchedFilesBase + ".read";
            var touchedWritesPath = touchedFilesBase + ".write";

            var expected = expectedReads.Select(s => s.ToUpperInvariant()).OrderBy(s => s);
            Assert.Equal(string.Join("\r\n", expected),
                         File.ReadAllText(touchedReadPath).Trim());

            expected = expectedWrites.Select(s => s.ToUpperInvariant()).OrderBy(s => s);
            Assert.Equal(string.Join("\r\n", expected),
                         File.ReadAllText(touchedWritesPath).Trim());
        }
    }
}
