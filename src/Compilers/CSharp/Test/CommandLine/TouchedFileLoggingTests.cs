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
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Resources.Proprietary;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using static Roslyn.Test.Utilities.SharedResourceHelpers;
using static Roslyn.Test.Utilities.TestMetadata;

namespace Microsoft.CodeAnalysis.CSharp.CommandLine.UnitTests
{
    public class TouchedFileLoggingTests : CommandLineTestBase
    {
        private static readonly string s_libDirectory = Environment.GetEnvironmentVariable("LIB");
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

            var cmd = CreateCSharpCompiler(new[] { "/nologo", hello,
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

            var silverlight = Temp.CreateFile().WriteAllBytes(ProprietaryTestResources.silverlight_v5_0_5_0.System_v5_0_5_0_silverlight).Path;
            var net4_0dll = Temp.CreateFile().WriteAllBytes(ResourcesNet451.System).Path;

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var cmd = CreateCSharpCompiler(
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
            var cmd = CreateCSharpCompiler(
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

            var cmd = CreateCSharpCompiler(new[]
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
    }
}
