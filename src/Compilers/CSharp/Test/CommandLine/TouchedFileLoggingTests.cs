// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CompilerServer;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

using static Roslyn.Test.Utilities.SharedResourceHelpers;
using System.Reflection;

namespace Microsoft.CodeAnalysis.CSharp.CommandLine.UnitTests
{
    public class TouchedFileLoggingTests : CSharpTestBase
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

        [ConditionalFact(typeof(WindowsOnly))]
        public void TrivialSourceFileOnlyCsc()
        {
            var hello = Temp.CreateFile().WriteAllText(helloWorldCS).Path;
            var touchedDir = Temp.CreateDirectory();
            var touchedBase = Path.Combine(touchedDir.Path, "touched");

            var cmd = new MockCSharpCompiler(null, _baseDirectory, new[] { "/nologo", hello,
               string.Format(@"/touchedfiles:""{0}""", touchedBase) });
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);

            List<string> expectedReads;
            List<string> expectedWrites;
            BuildTouchedFiles(cmd,
                              Path.ChangeExtension(hello, "exe"),
                              out expectedReads,
                              out expectedWrites);
            var exitCode = cmd.Run(outWriter);

            Assert.Equal("", outWriter.ToString().Trim());
            Assert.Equal(0, exitCode);
            AssertTouchedFilesEqual(expectedReads,
                                    expectedWrites,
                                    touchedBase);

            CleanupAllGeneratedFiles(hello);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void AppConfigCsc()
        {
            var hello = Temp.CreateFile().WriteAllText(helloWorldCS).Path;
            var touchedDir = Temp.CreateDirectory();
            var touchedBase = Path.Combine(touchedDir.Path, "touched");
            var appConfigPath = Temp.CreateFile().WriteAllText(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
       <supportPortability PKT=""7cec85d7bea7798e"" enable=""false""/>
    </assemblyBinding>
  </runtime>
</configuration>").Path;

            var silverlight = Temp.CreateFile().WriteAllBytes(TestResources.NetFX.silverlight_v5_0_5_0.System_v5_0_5_0_silverlight).Path;
            var net4_0dll = Temp.CreateFile().WriteAllBytes(TestResources.NetFX.v4_0_30319.System).Path;

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var cmd = new MockCSharpCompiler(null, _baseDirectory,
                new[] { "/nologo",
                        "/r:" + silverlight,
                        "/r:" + net4_0dll,
                        "/appconfig:" + appConfigPath,
                        "/touchedfiles:" + touchedBase,
                        hello });

            List<string> expectedReads;
            List<string> expectedWrites;
            BuildTouchedFiles(cmd,
                              Path.ChangeExtension(hello, "exe"),
                              out expectedReads,
                              out expectedWrites);
            expectedReads.Add(appConfigPath);

            var exitCode = cmd.Run(outWriter);
            Assert.Equal("", outWriter.ToString().Trim());
            Assert.Equal(0, exitCode);
            AssertTouchedFilesEqual(expectedReads,
                                    expectedWrites,
                                    touchedBase);

            CleanupAllGeneratedFiles(hello);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void StrongNameKeyCsc()
        {
            var hello = Temp.CreateFile().WriteAllText(helloWorldCS).Path;
            var snkPath = Temp.CreateFile("TestKeyPair_", ".snk").WriteAllBytes(TestResources.General.snKey).Path;
            var touchedDir = Temp.CreateDirectory();
            var touchedBase = Path.Combine(touchedDir.Path, "touched");

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var cmd = new MockCSharpCompiler(null, _baseDirectory,
                new[] { "/nologo",
                        "/touchedfiles:" + touchedBase,
                        "/keyfile:" + snkPath,
                        hello });

            List<string> expectedReads;
            List<string> expectedWrites;
            BuildTouchedFiles(cmd,
                              Path.ChangeExtension(hello, "exe"),
                              out expectedReads,
                              out expectedWrites);
            expectedReads.Add(snkPath);

            var exitCode = cmd.Run(outWriter);

            Assert.Equal(string.Empty, outWriter.ToString().Trim());
            Assert.Equal(0, exitCode);

            AssertTouchedFilesEqual(expectedReads,
                                    expectedWrites,
                                    touchedBase);

            CleanupAllGeneratedFiles(hello);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void XmlDocumentFileCsc()
        {
            var sourcePath = Temp.CreateFile().WriteAllText(@"
/// <summary>
/// A subtype of <see cref=""object""/>.
/// </summary>
public class C { }").Path;
            var xml = Temp.CreateFile();
            var touchedDir = Temp.CreateDirectory();
            var touchedBase = Path.Combine(touchedDir.Path, "touched");

            var cmd = new MockCSharpCompiler(null, _baseDirectory, new[]
            {
                "/nologo",
                "/target:library",
                "/doc:" + xml.Path,
                "/touchedfiles:" + touchedDir.Path + "\\touched",
                sourcePath
            });

            // Build touched files
            List<string> expectedReads;
            List<string> expectedWrites;
            BuildTouchedFiles(cmd,
                              Path.ChangeExtension(sourcePath, "dll"),
                              out expectedReads,
                              out expectedWrites);
            expectedWrites.Add(xml.Path);

            var writer = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = cmd.Run(writer);
            Assert.Equal(string.Empty, writer.ToString().Trim());
            Assert.Equal(0, exitCode);
            Assert.Equal(string.Format(@"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>{0}</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <summary>
            A subtype of <see cref=""T:System.Object""/>.
            </summary>
        </member>
    </members>
</doc>", Path.GetFileNameWithoutExtension(sourcePath)).Trim(),
                xml.ReadAllText().Trim());

            AssertTouchedFilesEqual(expectedReads,
                                    expectedWrites,
                                    touchedBase);

            CleanupAllGeneratedFiles(sourcePath);
        }

        [ConditionalFact(typeof(WindowsOnly))]
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
                    null,
                    _baseDirectory,
                    RuntimeEnvironment.GetRuntimeDirectory(),
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
