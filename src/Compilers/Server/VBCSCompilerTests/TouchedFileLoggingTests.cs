// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

namespace Microsoft.CodeAnalysis.CSharp.CommandLine.UnitTests
{
    public class TouchedFileLoggingTests : TestBase
    {
        private static readonly string s_libDirectory = Environment.GetEnvironmentVariable("LIB");
        private readonly string _baseDirectory = TempRoot.Root;
        private const string helloWorldCS = @"using System;

class C
{
    public static void Main(string[] args)
    {
        Console.WriteLine(""Hello, world"");
    }
}";

        [ConditionalFact(typeof(DesktopOnly))]
        public void TrivialMetadataCaching()
        {
            List<String> filelist = new List<string>();

            // Do the following compilation twice.
            // The compiler server API should hold on to the mscorlib bits
            // in memory, but the file tracker should still map that it was
            // touched.
            for (int i = 0; i < 2; i++)
            {
                var source1 = Temp.CreateFile().WriteAllText(helloWorldCS).Path;
                var touchedDir = Temp.CreateDirectory();
                var touchedBase = Path.Combine(touchedDir.Path, "touched");

                filelist.Add(source1);
                var outWriter = new StringWriter();
                var cmd = new CSharpCompilerServer(
                    DesktopCompilerServerHost.SharedAssemblyReferenceProvider,
                    new[] { "/nologo", "/touchedfiles:" + touchedBase, source1 },
                    new BuildPaths(null, _baseDirectory, RuntimeEnvironment.GetRuntimeDirectory(), Path.GetTempPath()),
                    s_libDirectory,
                    new TestAnalyzerAssemblyLoader());

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

        /// <summary>
        /// Builds the expected base of touched files.
        /// Adds a hook for temporary file creation as well,
        /// so this method must be called before the execution of
        /// Csc.Run.
        /// </summary>
        private static void BuildTouchedFiles(CSharpCompiler cmd,
                                              string outputPath,
                                              out List<string> expectedReads,
                                              out List<string> expectedWrites)
        {
            expectedReads = cmd.Arguments.MetadataReferences
                .Select(r => r.Reference).ToList();

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

        private class TestAnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
        {
            public void AddDependencyLocation(string fullPath)
            {
                throw new NotImplementedException();
            }

            public Assembly LoadFromPath(string fullPath)
            {
                throw new NotImplementedException();
            }
        }
    }
}
