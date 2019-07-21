// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommandLine;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    public class CompilerServerUnitTests : TestBase
    {
        internal static readonly RequestLanguage CSharpCompilerClientExecutable = RequestLanguage.CSharpCompile;

        internal static readonly RequestLanguage BasicCompilerClientExecutable = RequestLanguage.VisualBasicCompile;

        internal static readonly UTF8Encoding UTF8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        private static readonly KeyValuePair<string, string>[] s_helloWorldSrcCs =
        {
            new KeyValuePair<string, string>("hello.cs",
@"using System;
using System.Diagnostics;
class Hello
{
    static void Main()
    {
        var obj = new Process();
        Console.WriteLine(""Hello, world.""); 
    }
}")
        };

        private static readonly KeyValuePair<string, string>[] s_helloWorldSrcVb =
        {
            new KeyValuePair<string, string>("hello.vb",
@"Imports System
Imports System.Diagnostics

Module Module1
    Sub Main()
        Dim p As New Process()
        Console.WriteLine(""Hello from VB"")
    End Sub
End Module")
        };

        private readonly TempDirectory _tempDirectory;

        public CompilerServerUnitTests()
        {
            _tempDirectory = Temp.CreateDirectory();
        }

        #region Helpers

        private static IEnumerable<KeyValuePair<string, string>> AddForLoggingEnvironmentVars(IEnumerable<KeyValuePair<string, string>> vars)
        {
            vars = vars ?? new KeyValuePair<string, string>[] { };
            if (!vars.Where(kvp => kvp.Key == "RoslynCommandLineLogFile").Any())
            {
                var list = vars.ToList();
                list.Add(new KeyValuePair<string, string>(
                    "RoslynCommandLineLogFile",
                    typeof(CompilerServerUnitTests).Assembly.Location + ".client-server.log"));
                return list;
            }

            return vars;
        }

        private static void CheckForBadShared(List<string> arguments)
        {
            bool hasShared;
            string keepAlive;
            string errorMessage;
            string pipeName;
            List<string> parsedArgs;
            if (CommandLineParser.TryParseClientArgs(
                    arguments,
                    out parsedArgs,
                    out hasShared,
                    out keepAlive,
                    out pipeName,
                    out errorMessage))
            {
                if (hasShared && string.IsNullOrEmpty(pipeName))
                {
                    throw new InvalidOperationException("Must specify a pipe name in these suites to ensure we're not running out of proc servers");
                }
            }
        }

        private static void ReferenceNetstandardDllIfCoreClr(TempDirectory currentDirectory, List<string> arguments)
        {
#if !NET472
            var filePath = Path.Combine(currentDirectory.Path, "netstandard.dll");
            File.WriteAllBytes(filePath, TestResources.NetFX.netstandard20.netstandard);
            arguments.Add("/nostdlib");
            arguments.Add("/r:netstandard.dll");
#endif
        }

        private static void CreateFiles(TempDirectory currentDirectory, IEnumerable<KeyValuePair<string, string>> files)
        {
            if (files != null)
            {
                foreach (var pair in files)
                {
                    TempFile file = currentDirectory.CreateFile(pair.Key);
                    file.WriteAllText(pair.Value);
                }
            }
        }

        private static T ApplyEnvironmentVariables<T>(
            IEnumerable<KeyValuePair<string, string>> environmentVariables,
            Func<T> func)
        {
            if (environmentVariables == null)
            {
                return func();
            }

            var resetVariables = new Dictionary<string, string>();
            try
            {
                foreach (var variable in environmentVariables)
                {
                    resetVariables.Add(variable.Key, Environment.GetEnvironmentVariable(variable.Key));
                    Environment.SetEnvironmentVariable(variable.Key, variable.Value);
                }

                return func();
            }
            finally
            {
                foreach (var variable in resetVariables)
                {
                    Environment.SetEnvironmentVariable(variable.Key, variable.Value);
                }
            }
        }

        private static (T Result, string Output) UseTextWriter<T>(Encoding encoding, Func<TextWriter, T> func)
        {
            MemoryStream memoryStream;
            TextWriter writer;
            if (encoding == null)
            {
                memoryStream = null;
                writer = new StringWriter();
            }
            else
            {
                memoryStream = new MemoryStream();
                writer = new StreamWriter(memoryStream, encoding);
            }

            var result = func(writer);
            writer.Flush();
            if (memoryStream != null)
            {
                return (result, encoding.GetString(memoryStream.GetBuffer(), 0, (int)memoryStream.Length));
            }
            else
            {
                return (result, ((StringWriter)writer).ToString());
            }
        }

        internal static (int ExitCode, string Output) RunCommandLineCompiler(
            RequestLanguage language,
            string argumentsSingle,
            TempDirectory currentDirectory,
            IEnumerable<KeyValuePair<string, string>> filesInDirectory = null,
            IEnumerable<KeyValuePair<string, string>> additionalEnvironmentVars = null,
            Encoding redirectEncoding = null,
            bool shouldRunOnServer = true)
        {
            var arguments = new List<string>(argumentsSingle.Split(' '));

            // This is validating that localization to a specific locale works no matter what the locale of the 
            // machine running the tests are. 
            arguments.Add("/preferreduilang:en");

            ReferenceNetstandardDllIfCoreClr(currentDirectory, arguments);
            CheckForBadShared(arguments);
            CreateFiles(currentDirectory, filesInDirectory);

            // Create a client to run the build.  Infinite timeout is used to account for the
            // case where these tests are run under extreme load.  In high load scenarios the
            // client will correctly drop down to a local compilation if the server doesn't respond
            // fast enough.
            var client = ServerUtil.CreateBuildClient(language);
            client.TimeoutOverride = Timeout.Infinite;

            var sdkDir = ServerUtil.DefaultSdkDirectory;

            var buildPaths = new BuildPaths(
                clientDir: Path.GetDirectoryName(typeof(CommonCompiler).Assembly.Location),
                workingDir: currentDirectory.Path,
                sdkDir: sdkDir,
                tempDir: BuildServerConnection.GetTempPath(currentDirectory.Path));

            var (result, output) = UseTextWriter(redirectEncoding, writer => ApplyEnvironmentVariables(additionalEnvironmentVars, () => client.RunCompilation(arguments, buildPaths, writer)));
            Assert.Equal(shouldRunOnServer, result.RanOnServer);
            return (result.ExitCode, output);
        }

        private static DisposableFile GetResultFile(TempDirectory directory, string resultFileName)
        {
            return new DisposableFile(Path.Combine(directory.Path, resultFileName));
        }

        private static void RunCompilerOutput(TempFile file, string expectedOutput)
        {
            if (RuntimeHostInfo.IsDesktopRuntime)
            {
                var result = ProcessUtilities.Run(file.Path, "", Path.GetDirectoryName(file.Path));
                Assert.Equal(expectedOutput.Trim(), result.Output.Trim());
            }
        }

        private static void VerifyResult((int ExitCode, string Output) result)
        {
            AssertEx.AssertEqualToleratingWhitespaceDifferences("", result.Output);
            Assert.Equal(0, result.ExitCode);
        }

        private void VerifyResultAndOutput((int ExitCode, string Output) result, TempDirectory path, string expectedOutput)
        {
            using (var resultFile = GetResultFile(path, "hello.exe"))
            {
                VerifyResult(result);

                RunCompilerOutput(resultFile, expectedOutput);
            }
        }

        #endregion

        [ConditionalFact(typeof(UnixLikeOnly))]
        public async Task ServerFailsWithLongTempPathUnix()
        {
            var newTempDir = _tempDirectory.CreateDirectory(new string('a', 100 - _tempDirectory.Path.Length));
            await ApplyEnvironmentVariables(
                new[] { new KeyValuePair<string, string>("TMPDIR", newTempDir.Path) },
                async () =>
            {
                using (var serverData = await ServerUtil.CreateServer())
                {
                    var result = RunCommandLineCompiler(
                        CSharpCompilerClientExecutable,
                        $"/shared:{serverData.PipeName} /nologo hello.cs",
                        _tempDirectory,
                        s_helloWorldSrcCs,
                        shouldRunOnServer: true);
                    VerifyResultAndOutput(result, _tempDirectory, "Hello, world.");
                    await serverData.Verify(connections: 1, completed: 1).ConfigureAwait(true);
                }
            });
        }

        [Fact]
        public async Task FallbackToCsc()
        {
            // Verify csc will fall back to command line when server fails to process
            using (var serverData = await ServerUtil.CreateServerFailsConnection())
            {
                var result = RunCommandLineCompiler(CSharpCompilerClientExecutable, $"/shared:{serverData.PipeName} /nologo hello.cs", _tempDirectory, s_helloWorldSrcCs, shouldRunOnServer: false);
                VerifyResultAndOutput(result, _tempDirectory, "Hello, world.");
                // the server still counts failed connections as completing a connection (but with a failed result), hence the "completed: 1"
                await serverData.Verify(connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [Fact]
        public async Task CscFallBackOutputNoUtf8()
        {
            // Verify csc will fall back to command line when server fails to process
            using (var serverData = await ServerUtil.CreateServerFailsConnection())
            {
                var files = new Dictionary<string, string> { { "hello.cs", "♕" } };

                var result = RunCommandLineCompiler(CSharpCompilerClientExecutable, $"/shared:{serverData.PipeName} /nologo hello.cs", _tempDirectory, files, redirectEncoding: Encoding.ASCII, shouldRunOnServer: false);
                Assert.Equal(result.ExitCode, 1);
                Assert.Equal("hello.cs(1,1): error CS1056: Unexpected character '?'", result.Output.Trim());
                await serverData.Verify(connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [Fact]
        public async Task CscFallBackOutputUtf8()
        {
            var srcFile = _tempDirectory.CreateFile("test.cs").WriteAllText("♕").Path;

            using (var serverData = await ServerUtil.CreateServerFailsConnection())
            {
                var result = RunCommandLineCompiler(
                    CSharpCompilerClientExecutable,
                    $"/shared:{serverData.PipeName} /utf8output /nologo /t:library {srcFile}",
                    _tempDirectory,
                    redirectEncoding: UTF8Encoding,
                    shouldRunOnServer: false);

                Assert.Equal("test.cs(1,1): error CS1056: Unexpected character '♕'".Trim(),
                    result.Output.Trim().Replace(srcFile, "test.cs"));
                Assert.Equal(1, result.ExitCode);
                await serverData.Verify(connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [Fact]
        public async Task VbcFallbackNoUtf8()
        {
            var srcFile = _tempDirectory.CreateFile("test.vb").WriteAllText("♕").Path;

            using (var serverData = await ServerUtil.CreateServerFailsConnection())
            {
                var result = RunCommandLineCompiler(
                    BasicCompilerClientExecutable,
                    $"/shared:{serverData.PipeName} /vbruntime* /nologo test.vb",
                    _tempDirectory,
                    redirectEncoding: Encoding.ASCII,
                    shouldRunOnServer: false);

                Assert.Equal(result.ExitCode, 1);
                Assert.Equal(@"test.vb(1) : error BC30037: Character is not valid.

?
~", result.Output.Trim().Replace(srcFile, "test.vb"));
                await serverData.Verify(connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [Fact]
        public async Task VbcFallbackUtf8()
        {
            var srcFile = _tempDirectory.CreateFile("test.vb").WriteAllText("♕").Path;

            using (var serverData = await ServerUtil.CreateServerFailsConnection())
            {
                var result = RunCommandLineCompiler(
                    BasicCompilerClientExecutable,
                    $"/shared:{serverData.PipeName} /vbruntime* /utf8output /nologo /t:library {srcFile}",
                    _tempDirectory,
                    redirectEncoding: UTF8Encoding,
                    shouldRunOnServer: false);

                Assert.Equal(@"test.vb(1) : error BC30037: Character is not valid.

♕
~", result.Output.Trim().Replace(srcFile, "test.vb"));
                Assert.Equal(1, result.ExitCode);
                await serverData.Verify(connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [Fact]
        public async Task FallbackToVbc()
        {
            using (var serverData = await ServerUtil.CreateServerFailsConnection())
            {
                var result = RunCommandLineCompiler(BasicCompilerClientExecutable, $"/shared:{serverData.PipeName} /nologo /vbruntime* hello.vb", _tempDirectory, s_helloWorldSrcVb, shouldRunOnServer: false);
                VerifyResultAndOutput(result, _tempDirectory, "Hello from VB");
                await serverData.Verify(connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task HelloWorldCS()
        {
            using (var serverData = await ServerUtil.CreateServer())
            {
                var result = RunCommandLineCompiler(CSharpCompilerClientExecutable, $"/shared:{serverData.PipeName} /nologo hello.cs", _tempDirectory, s_helloWorldSrcCs);
                VerifyResultAndOutput(result, _tempDirectory, "Hello, world.");
                await serverData.Verify(connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task HelloWorldCSDashShared()
        {
            using (var serverData = await ServerUtil.CreateServer())
            {
                var result = RunCommandLineCompiler(CSharpCompilerClientExecutable, $"-shared:{serverData.PipeName} /nologo hello.cs", _tempDirectory, s_helloWorldSrcCs);
                VerifyResultAndOutput(result, _tempDirectory, "Hello, world.");
                await serverData.Verify(connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [WorkItem(946954, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/946954")]
        public void CompilerBinariesAreNotX86()
        {
            var basePath = Path.GetDirectoryName(typeof(CompilerServerUnitTests).Assembly.Location);
            var compilerServerExecutable = Path.Combine(basePath, "VBCSCompiler.exe");
            Assert.NotEqual(ProcessorArchitecture.X86,
                AssemblyName.GetAssemblyName(compilerServerExecutable).ProcessorArchitecture);
        }

        /// <summary>
        /// This method tests that when a 64-bit compiler server loads a 
        /// 64-bit mscorlib with /platform:x86 enabled no warning about
        /// emitting a reference to a 64-bit assembly is produced.
        /// The test should pass on x86 or amd64, but can only fail on
        /// amd64.
        /// </summary>
        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task Platformx86MscorlibCsc()
        {
            using (var serverData = await ServerUtil.CreateServer())
            {
                var files = new Dictionary<string, string> { { "c.cs", "class C {}" } };
                var result = RunCommandLineCompiler(CSharpCompilerClientExecutable,
                                                    $"/shared:{serverData.PipeName} /nologo /t:library /platform:x86 c.cs",
                                                    _tempDirectory,
                                                    files);
                VerifyResult(result);
                await serverData.Verify(connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task Platformx86MscorlibVbc()
        {
            using (var serverData = await ServerUtil.CreateServer())
            {
                var files = new Dictionary<string, string> { { "c.vb", "Class C\nEnd Class" } };
                var result = RunCommandLineCompiler(BasicCompilerClientExecutable,
                                                    $"/shared:{serverData.PipeName} /vbruntime* /nologo /t:library /platform:x86 c.vb",
                                                    _tempDirectory,
                                                    files);
                VerifyResult(result);
                await serverData.Verify(connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task ExtraMSCorLibCS()
        {
            using (var serverData = await ServerUtil.CreateServer())
            {
                var result = RunCommandLineCompiler(CSharpCompilerClientExecutable,
                                                    $"/shared:{serverData.PipeName} /nologo /r:mscorlib.dll hello.cs",
                                                    _tempDirectory,
                                                    s_helloWorldSrcCs);
                VerifyResultAndOutput(result, _tempDirectory, "Hello, world.");
                await serverData.Verify(connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task HelloWorldVB()
        {
            using (var serverData = await ServerUtil.CreateServer())
            {
                var result = RunCommandLineCompiler(BasicCompilerClientExecutable,
                                                    $"/shared:{serverData.PipeName} /nologo /vbruntime* hello.vb",
                                                    _tempDirectory,
                                                    s_helloWorldSrcVb);
                VerifyResultAndOutput(result, _tempDirectory, "Hello from VB");
                await serverData.Verify(connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task ExtraMSCorLibVB()
        {
            using (var serverData = await ServerUtil.CreateServer())
            {
                var result = RunCommandLineCompiler(BasicCompilerClientExecutable,
                    $"/shared:{serverData.PipeName} /nologo /r:mscorlib.dll /vbruntime* hello.vb",
                    _tempDirectory,
                    s_helloWorldSrcVb);
                VerifyResultAndOutput(result, _tempDirectory, "Hello from VB");
                await serverData.Verify(connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task CompileErrorsCS()
        {
            using (var serverData = await ServerUtil.CreateServer())
            {
                Dictionary<string, string> files =
                                       new Dictionary<string, string> {
                                           { "hello.cs",
@"using System;
class Hello 
{
    static void Main()
    { Console.WriteLine(""Hello, world."") }
}"}};

                var result = RunCommandLineCompiler(CSharpCompilerClientExecutable, $"/shared:{serverData.PipeName} hello.cs", _tempDirectory, files);

                // Should output errors, but not create output file.
                Assert.Contains("Copyright (C) Microsoft Corporation. All rights reserved.", result.Output, StringComparison.Ordinal);
                Assert.Contains("hello.cs(5,42): error CS1002: ; expected", result.Output, StringComparison.Ordinal);
                Assert.Equal(1, result.ExitCode);
                Assert.False(File.Exists(Path.Combine(_tempDirectory.Path, "hello.exe")));
                await serverData.Verify(connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task CompileErrorsVB()
        {
            using (var serverData = await ServerUtil.CreateServer())
            {
                Dictionary<string, string> files =
                                       new Dictionary<string, string> {
                                           { "hellovb.vb",
@"Imports System

Module Module1
    Sub Main()
        Console.WriteLine(""Hello from VB"")
    End Sub
End Class"}};

                var result = RunCommandLineCompiler(BasicCompilerClientExecutable, $"/shared:{serverData.PipeName} /vbruntime* hellovb.vb", _tempDirectory, files);

                // Should output errors, but not create output file.
                Assert.Contains("Copyright (C) Microsoft Corporation. All rights reserved.", result.Output, StringComparison.Ordinal);
                Assert.Contains("hellovb.vb(3) : error BC30625: 'Module' statement must end with a matching 'End Module'.", result.Output, StringComparison.Ordinal);
                Assert.Contains("hellovb.vb(7) : error BC30460: 'End Class' must be preceded by a matching 'Class'.", result.Output, StringComparison.Ordinal);
                Assert.Equal(1, result.ExitCode);
                Assert.False(File.Exists(Path.Combine(_tempDirectory.Path, "hello.exe")));
                await serverData.Verify(connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task MissingFileErrorCS()
        {
            using (var serverData = await ServerUtil.CreateServer())
            {
                var result = RunCommandLineCompiler(CSharpCompilerClientExecutable, $"/shared:{serverData.PipeName} missingfile.cs", _tempDirectory);

                // Should output errors, but not create output file.
                Assert.Contains("Copyright (C) Microsoft Corporation. All rights reserved.", result.Output, StringComparison.Ordinal);
                Assert.Contains("error CS2001: Source file", result.Output, StringComparison.Ordinal);
                Assert.Equal(1, result.ExitCode);
                Assert.False(File.Exists(Path.Combine(_tempDirectory.Path, "missingfile.exe")));
                await serverData.Verify(connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task MissingReferenceErrorCS()
        {
            using (var serverData = await ServerUtil.CreateServer())
            {
                var result = RunCommandLineCompiler(CSharpCompilerClientExecutable, $"/shared:{serverData.PipeName} /r:missing.dll hello.cs", _tempDirectory, s_helloWorldSrcCs);

                // Should output errors, but not create output file.
                Assert.Contains("Copyright (C) Microsoft Corporation. All rights reserved.", result.Output, StringComparison.Ordinal);
                Assert.Contains("error CS0006: Metadata file", result.Output, StringComparison.Ordinal);
                Assert.Equal(1, result.ExitCode);
                Assert.False(File.Exists(Path.Combine(_tempDirectory.Path, "hello.exe")));
                await serverData.Verify(connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [WorkItem(546067, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546067")]
        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task InvalidMetadataFileErrorCS()
        {
            using (var serverData = await ServerUtil.CreateServer())
            {
                Dictionary<string, string> files =
                                       new Dictionary<string, string> {
                                               { "Lib.cs", "public class C {}"},
                                               { "app.cs", "class Test { static void Main() {} }"},
                                               };

                var result = RunCommandLineCompiler(CSharpCompilerClientExecutable, $"/shared:{serverData.PipeName} /r:Lib.cs app.cs", _tempDirectory, files);

                // Should output errors, but not create output file.
                Assert.Contains("Copyright (C) Microsoft Corporation. All rights reserved.", result.Output, StringComparison.Ordinal);
                Assert.Contains("error CS0009: Metadata file", result.Output, StringComparison.Ordinal);
                Assert.Equal(1, result.ExitCode);
                Assert.False(File.Exists(Path.Combine(_tempDirectory.Path, "app.exe")));
                await serverData.Verify(connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task MissingFileErrorVB()
        {
            using (var serverData = await ServerUtil.CreateServer())
            {
                var result = RunCommandLineCompiler(BasicCompilerClientExecutable, $"/shared:{serverData.PipeName} /vbruntime* missingfile.vb", _tempDirectory);

                // Should output errors, but not create output file.
                Assert.Contains("Copyright (C) Microsoft Corporation. All rights reserved.", result.Output, StringComparison.Ordinal);
                Assert.Contains("error BC2001", result.Output, StringComparison.Ordinal);
                Assert.Equal(1, result.ExitCode);
                Assert.False(File.Exists(Path.Combine(_tempDirectory.Path, "missingfile.exe")));
                await serverData.Verify(connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [Fact, WorkItem(761131, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/761131")]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task MissingReferenceErrorVB()
        {
            using (var serverData = await ServerUtil.CreateServer())
            {
                Dictionary<string, string> files =
                                       new Dictionary<string, string> {
                                           { "hellovb.vb",
@"Imports System.Diagnostics

Module Module1
    Sub Main()
        Dim p As New Process()
        Console.WriteLine(""Hello from VB"")
    End Sub
End Module"}};

                var result = RunCommandLineCompiler(BasicCompilerClientExecutable, $"/shared:{serverData.PipeName} /nologo /vbruntime* /r:missing.dll hellovb.vb", _tempDirectory, files);

                // Should output errors, but not create output file.
                Assert.Contains("error BC2017: could not find library", result.Output, StringComparison.Ordinal);
                Assert.Equal(1, result.ExitCode);
                Assert.False(File.Exists(Path.Combine(_tempDirectory.Path, "hellovb.exe")));
                await serverData.Verify(connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [WorkItem(546067, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546067")]
        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task InvalidMetadataFileErrorVB()
        {
            using (var serverData = await ServerUtil.CreateServer())
            {
                Dictionary<string, string> files =
                                       new Dictionary<string, string> {
                                            { "Lib.vb",
@"Class C
End Class" },
                                            { "app.vb",
@"Module M1
    Sub Main()
    End Sub
End Module"}};

                var result = RunCommandLineCompiler(BasicCompilerClientExecutable, $"/shared:{serverData.PipeName} /nologo /vbruntime* /r:Lib.vb app.vb", _tempDirectory, files);

                // Should output errors, but not create output file.
                Assert.Contains("error BC31519", result.Output, StringComparison.Ordinal);
                Assert.Equal(1, result.ExitCode);
                Assert.False(File.Exists(Path.Combine(_tempDirectory.Path, "app.exe")));
                await serverData.Verify(connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/20345")]
        [WorkItem(723280, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/723280")]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task ReferenceCachingVB()
        {
            TempDirectory rootDirectory = _tempDirectory.CreateDirectory("ReferenceCachingVB");

            // Create DLL "lib.dll"
            Dictionary<string, string> files =
                                   new Dictionary<string, string> {
                                           { "src1.vb",
@"Imports System
Public Class Library 

    Public Shared Function GetString() As String
        Return ""library1""
    End Function
End Class
"}};

            using (var serverData = await ServerUtil.CreateServer())
            using (var tmpFile = GetResultFile(rootDirectory, "lib.dll"))
            {
                var result = RunCommandLineCompiler(BasicCompilerClientExecutable, $"src1.vb /shared:{serverData.PipeName} /nologo /t:library /out:lib.dll", rootDirectory, files);
                Assert.Equal("", result.Output);
                Assert.Equal(0, result.ExitCode);

                using (var hello1_file = GetResultFile(rootDirectory, "hello1.exe"))
                {
                    // Create EXE "hello1.exe"
                    files = new Dictionary<string, string> {
                                           { "hello1.vb",
@"Imports System
Module Module1 
    Public Sub Main()
        Console.WriteLine(""Hello1 from {0}"", Library.GetString())
    End Sub
End Module
"}};
                    result = RunCommandLineCompiler(BasicCompilerClientExecutable, $"hello1.vb /shared:{serverData.PipeName} /nologo /vbruntime* /r:lib.dll /out:hello1.exe", rootDirectory, files);
                    Assert.Equal("", result.Output);
                    Assert.Equal(0, result.ExitCode);

                    // Run hello1.exe.
                    RunCompilerOutput(hello1_file, "Hello1 from library1");

                    using (var hello2_file = GetResultFile(rootDirectory, "hello2.exe"))
                    {
                        // Create EXE "hello2.exe" referencing same DLL
                        files = new Dictionary<string, string> {
                                                { "hello2.vb",
@"Imports System
Module Module1 
Public Sub Main()
    Console.WriteLine(""Hello2 from {0}"", Library.GetString())
End Sub
End Module
"}};
                        result = RunCommandLineCompiler(BasicCompilerClientExecutable, $"hello2.vb /shared:{serverData.PipeName} /nologo /vbruntime* /r:lib.dll /out:hello2.exe", rootDirectory, files);
                        Assert.Equal("", result.Output);
                        Assert.Equal(0, result.ExitCode);

                        // Run hello2.exe.
                        RunCompilerOutput(hello2_file, "Hello2 from library1");

                        // Change DLL "lib.dll" to something new.
                        files =
                                               new Dictionary<string, string> {
                                           { "src2.vb",
@"Imports System
Public Class Library 
    Public Shared Function GetString() As String
        Return ""library2""
    End Function
    Public Shared Function GetString2() As String
        Return ""library3""
    End Function
End Class
"}};

                        result = RunCommandLineCompiler(BasicCompilerClientExecutable, $"src2.vb /shared:{serverData.PipeName} /nologo /t:library /out:lib.dll", rootDirectory, files);
                        Assert.Equal("", result.Output);
                        Assert.Equal(0, result.ExitCode);

                        using (var hello3_file = GetResultFile(rootDirectory, "hello3.exe"))
                        {
                            // Create EXE "hello3.exe" referencing new DLL
                            files = new Dictionary<string, string> {
                                           { "hello3.vb",
@"Imports System
Module Module1 
    Public Sub Main()
        Console.WriteLine(""Hello3 from {0}"", Library.GetString2())
    End Sub
End Module
"}};
                            result = RunCommandLineCompiler(BasicCompilerClientExecutable, $"hello3.vb /shared:{serverData.PipeName} /nologo /vbruntime* /r:lib.dll /out:hello3.exe", rootDirectory, files);
                            Assert.Equal("", result.Output);
                            Assert.Equal(0, result.ExitCode);

                            // Run hello3.exe. Should work.
                            RunCompilerOutput(hello3_file, "Hello3 from library3");

                            // Run hello2.exe one more time. Should have different output than before from updated library.
                            RunCompilerOutput(hello2_file, "Hello2 from library2");
                        }
                    }
                }

                await serverData.Verify(connections: 5, completed: 5).ConfigureAwait(true);
            }

            GC.KeepAlive(rootDirectory);
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/19763")]
        [WorkItem(723280, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/723280")]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task ReferenceCachingCS()
        {
            TempDirectory rootDirectory = _tempDirectory.CreateDirectory("ReferenceCachingCS");

            using (var serverData = await ServerUtil.CreateServer())
            using (var tmpFile = GetResultFile(rootDirectory, "lib.dll"))
            {
                // Create DLL "lib.dll"
                Dictionary<string, string> files =
                                       new Dictionary<string, string> {
                                           { "src1.cs",
@"using System;
public class Library 
{
    public static string GetString()
    { return ""library1""; }
}"}};

                var result = RunCommandLineCompiler(CSharpCompilerClientExecutable, $"src1.cs /shared:{serverData.PipeName} /nologo /t:library /out:lib.dll", rootDirectory, files);
                Assert.Equal("", result.Output);
                Assert.Equal(0, result.ExitCode);

                using (var hello1_file = GetResultFile(rootDirectory, "hello1.exe"))
                {
                    // Create EXE "hello1.exe"
                    files = new Dictionary<string, string> {
                                           { "hello1.cs",
@"using System;
class Hello 
{
    public static void Main()
    { Console.WriteLine(""Hello1 from {0}"", Library.GetString()); }
}"}};
                    result = RunCommandLineCompiler(CSharpCompilerClientExecutable, $"hello1.cs /shared:{serverData.PipeName} /nologo /r:lib.dll /out:hello1.exe", rootDirectory, files);
                    Assert.Equal("", result.Output);
                    Assert.Equal(0, result.ExitCode);

                    // Run hello1.exe.
                    RunCompilerOutput(hello1_file, "Hello1 from library1");

                    using (var hello2_file = GetResultFile(rootDirectory, "hello2.exe"))
                    {
                        var hello2exe = Temp.AddFile(hello2_file);

                        // Create EXE "hello2.exe" referencing same DLL
                        files = new Dictionary<string, string> {
                                               { "hello2.cs",
@"using System;
class Hello 
{
    public static void Main()
    { Console.WriteLine(""Hello2 from {0}"", Library.GetString()); }
}"}};
                        result = RunCommandLineCompiler(CSharpCompilerClientExecutable, $"hello2.cs /shared:{serverData.PipeName} /nologo /r:lib.dll /out:hello2.exe", rootDirectory, files);
                        Assert.Equal("", result.Output);
                        Assert.Equal(0, result.ExitCode);

                        // Run hello2.exe.
                        RunCompilerOutput(hello2exe, "Hello2 from library1");

                        // Change DLL "lib.dll" to something new.
                        files =
                                               new Dictionary<string, string> {
                                           { "src2.cs",
@"using System;
public class Library 
{
    public static string GetString()
    { return ""library2""; }

    public static string GetString2()
    { return ""library3""; }
}"}};

                        result = RunCommandLineCompiler(CSharpCompilerClientExecutable, $"src2.cs /shared:{serverData.PipeName} /nologo /t:library /out:lib.dll", rootDirectory, files);
                        Assert.Equal("", result.Output);
                        Assert.Equal(0, result.ExitCode);

                        using (var hello3_file = GetResultFile(rootDirectory, "hello3.exe"))
                        {
                            // Create EXE "hello3.exe" referencing new DLL
                            files = new Dictionary<string, string> {
                                           { "hello3.cs",
@"using System;
class Hello 
{
    public static void Main()
    { Console.WriteLine(""Hello3 from {0}"", Library.GetString2()); }
}"}};
                            result = RunCommandLineCompiler(CSharpCompilerClientExecutable, $"hello3.cs /shared:{serverData.PipeName} /nologo /r:lib.dll /out:hello3.exe", rootDirectory, files);
                            Assert.Equal("", result.Output);
                            Assert.Equal(0, result.ExitCode);

                            // Run hello3.exe. Should work.
                            RunCompilerOutput(hello3_file, "Hello3 from library3");

                            // Run hello2.exe one more time. Should have different output than before from updated library.
                            RunCompilerOutput(hello2_file, "Hello2 from library2");
                        }
                    }
                }

                await serverData.Verify(connections: 5, completed: 5).ConfigureAwait(true);
            }

            GC.KeepAlive(rootDirectory);
        }

        private static Task<DisposableFile> RunCompilationAsync(RequestLanguage language, string pipeName, int i, TempDirectory compilationDir)
        {
            string sourceFile;
            string exeFileName;
            string prefix;
            string sourceText;
            string additionalArgument;

            if (language == RequestLanguage.CSharpCompile)
            {
                exeFileName = $"hellocs{i}.exe";
                prefix = "CS";
                sourceFile = $"hello{i}.cs";
                sourceText =
$@"using System;
class Hello 
{{
    public static void Main()
    {{ Console.WriteLine(""{prefix} Hello number {i}""); }}
}}";
                additionalArgument = "";
            }
            else
            {
                exeFileName = $"hellovb{i}.exe";
                prefix = "VB";
                sourceFile = $"hello{i}.vb";
                sourceText =
$@"Imports System
Module Hello 
    Sub Main()
       Console.WriteLine(""{prefix} Hello number {i}"") 
    End Sub
End Module";
                additionalArgument = " /vbruntime*";
            }

            var arguments = $"/shared:{pipeName} /nologo {sourceFile} /out:{exeFileName}{additionalArgument}";
            var filesInDirectory = new Dictionary<string, string>()
            {
                { sourceFile, sourceText }
            };

            return Task.Run(() =>
            {
                var result = RunCommandLineCompiler(language, string.Join(" ", arguments), compilationDir, filesInDirectory: filesInDirectory);

                Assert.Equal(0, result.ExitCode);

                // Run the EXE and verify it prints the desired output.
                var exeFile = GetResultFile(compilationDir, exeFileName);
                RunCompilerOutput(exeFile, $"{prefix} Hello number {i}");
                return exeFile;
            });
        }

        [WorkItem(997372, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/997372")]
        [WorkItem(761326, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/761326")]
        [ConditionalFact(typeof(WindowsOnly))]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task MultipleSimultaneousCompiles()
        {
            using (var serverData = await ServerUtil.CreateServer())
            {
                // Run this many compiles simultaneously in different directories.
                const int numberOfCompiles = 20;
                var tasks = new Task<DisposableFile>[numberOfCompiles];

                for (int i = 0; i < numberOfCompiles; ++i)
                {
                    var language = i % 2 == 0 ? RequestLanguage.CSharpCompile : RequestLanguage.VisualBasicCompile;
                    var compilationDir = Temp.CreateDirectory();
                    tasks[i] = RunCompilationAsync(language, serverData.PipeName, i, compilationDir);
                }

                await Task.WhenAll(tasks);

                await serverData.Verify(numberOfCompiles, numberOfCompiles);

                foreach (var task in tasks)
                {
                    Temp.AddFile(task.Result);
                }
            }
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task UseLibVariableCS()
        {
            var libDirectory = _tempDirectory.CreateDirectory("LibraryDir");

            // Create DLL "lib.dll"
            Dictionary<string, string> files =
                                   new Dictionary<string, string> {
                                           { "src1.cs",
@"
public class Library 
{
    public static string GetString()
    { return ""library1""; }
}"}};

            using (var serverData = await ServerUtil.CreateServer())
            {
                var result = RunCommandLineCompiler(CSharpCompilerClientExecutable,
                                                    $"src1.cs /shared:{serverData.PipeName} /nologo /t:library /out:" + Path.Combine(libDirectory.Path, "lib.dll"),
                                                    _tempDirectory, files);

                Assert.Equal("", result.Output);
                Assert.Equal(0, result.ExitCode);

                Temp.AddFile(GetResultFile(libDirectory, "lib.dll"));

                // Create EXE "hello1.exe"
                files = new Dictionary<string, string> {
                                           { "hello1.cs",
@"using System;
class Hello 
{
    public static void Main()
    { Console.WriteLine(""Hello1 from {0}"", Library.GetString()); }
}"}};
                result = RunCommandLineCompiler(CSharpCompilerClientExecutable, $"hello1.cs /shared:{serverData.PipeName} /nologo /r:lib.dll /out:hello1.exe", _tempDirectory, files,
                                                additionalEnvironmentVars: new Dictionary<string, string>() { { "LIB", libDirectory.Path } });

                Assert.Equal("", result.Output);
                Assert.Equal(0, result.ExitCode);

                var resultFile = Temp.AddFile(GetResultFile(_tempDirectory, "hello1.exe"));
                await serverData.Verify(connections: 2, completed: 2).ConfigureAwait(true);
            }
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task UseLibVariableVB()
        {
            var libDirectory = _tempDirectory.CreateDirectory("LibraryDir");

            // Create DLL "lib.dll"
            Dictionary<string, string> files =
                                   new Dictionary<string, string> {
                                           { "src1.vb",
@"Imports System
Public Class Library 

    Public Shared Function GetString() As String
        Return ""library1""
    End Function
End Class
"}};

            using (var serverData = await ServerUtil.CreateServer())
            {
                var result = RunCommandLineCompiler(BasicCompilerClientExecutable,
                                                    $"src1.vb /shared:{serverData.PipeName} /vbruntime* /nologo /t:library /out:" + Path.Combine(libDirectory.Path, "lib.dll"),
                                                    _tempDirectory, files);

                Assert.Equal("", result.Output);
                Assert.Equal(0, result.ExitCode);

                Temp.AddFile(GetResultFile(libDirectory, "lib.dll"));

                // Create EXE "hello1.exe"
                files = new Dictionary<string, string> {
                                           { "hello1.vb",
@"Imports System
Module Module1 
    Public Sub Main()
        Console.WriteLine(""Hello1 from {0}"", Library.GetString())
    End Sub
End Module
"}};
                result = RunCommandLineCompiler(BasicCompilerClientExecutable, $"hello1.vb /shared:{serverData.PipeName} /nologo /vbruntime* /r:lib.dll /out:hello1.exe", _tempDirectory, files,
                                                additionalEnvironmentVars: new Dictionary<string, string>() { { "LIB", libDirectory.Path } });

                Assert.Equal("", result.Output);
                Assert.Equal(0, result.ExitCode);

                var resultFile = Temp.AddFile(GetResultFile(_tempDirectory, "hello1.exe"));
                await serverData.Verify(connections: 2, completed: 2).ConfigureAwait(true);
            }
        }

        [WorkItem(545446, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545446")]
        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task Utf8Output_WithRedirecting_Off_Shared()
        {
            var srcFile = _tempDirectory.CreateFile("test.cs").WriteAllText("♕").Path;

            using (var serverData = await ServerUtil.CreateServer())
            {
                var result = RunCommandLineCompiler(
                    CSharpCompilerClientExecutable,
                    $"/shared:{serverData.PipeName} /nologo /t:library {srcFile}",
                    _tempDirectory,
                    redirectEncoding: Encoding.ASCII);

                Assert.Equal("test.cs(1,1): error CS1056: Unexpected character '?'".Trim(),
                    result.Output.Trim());
                Assert.Equal(1, result.ExitCode);
                await serverData.Verify(connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [WorkItem(545446, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545446")]
        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task Utf8Output_WithRedirecting_Off_Share()
        {
            var srcFile = _tempDirectory.CreateFile("test.vb").WriteAllText(@"♕").Path;
            var tempOut = _tempDirectory.CreateFile("output.txt");

            using (var serverData = await ServerUtil.CreateServer())
            {
                var result = RunCommandLineCompiler(
                    BasicCompilerClientExecutable,
                    $"/shared:{serverData.PipeName} /nologo /vbruntime* /t:library {srcFile}",
                    _tempDirectory,
                    redirectEncoding: Encoding.ASCII);

                Assert.Equal(@"SRC.VB(1) : error BC30037: Character is not valid.

?
~
".Trim(),
                            result.Output.Trim().Replace(srcFile, "SRC.VB"));
                Assert.Equal(1, result.ExitCode);
                await serverData.Verify(connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [WorkItem(545446, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545446")]
        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task Utf8Output_WithRedirecting_On_Shared_CS()
        {
            var srcFile = _tempDirectory.CreateFile("test.cs").WriteAllText("♕").Path;

            using (var serverData = await ServerUtil.CreateServer())
            {
                var result = RunCommandLineCompiler(
                    CSharpCompilerClientExecutable,
                    $"/shared:{serverData.PipeName} /utf8output /nologo /t:library {srcFile}",
                    _tempDirectory,
                    redirectEncoding: UTF8Encoding);

                Assert.Equal("test.cs(1,1): error CS1056: Unexpected character '♕'".Trim(),
                    result.Output.Trim());
                Assert.Equal(1, result.ExitCode);
                await serverData.Verify(connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [WorkItem(545446, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545446")]
        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task Utf8Output_WithRedirecting_On_Shared_VB()
        {
            var srcFile = _tempDirectory.CreateFile("test.vb").WriteAllText(@"♕").Path;

            using (var serverData = await ServerUtil.CreateServer())
            {
                var result = RunCommandLineCompiler(
                    BasicCompilerClientExecutable,
                    $"/shared:{serverData.PipeName} /utf8output /nologo /vbruntime* /t:library {srcFile}",
                    _tempDirectory,
                    redirectEncoding: UTF8Encoding);

                Assert.Equal(@"SRC.VB(1) : error BC30037: Character is not valid.

♕
~
".Trim(),
                            result.Output.Trim().Replace(srcFile, "SRC.VB"));
                Assert.Equal(1, result.ExitCode);
                await serverData.Verify(connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [WorkItem(871477, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/871477")]
        [ConditionalFact(typeof(DesktopOnly))]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task AssemblyIdentityComparer1()
        {
            _tempDirectory.CreateFile("mscorlib20.dll").WriteAllBytes(TestResources.NetFX.v2_0_50727.mscorlib);
            _tempDirectory.CreateFile("mscorlib40.dll").WriteAllBytes(TestResources.NetFX.v4_0_21006.mscorlib);

            // Create DLL "lib.dll"
            Dictionary<string, string> files =
                                   new Dictionary<string, string> {
                                           { "ref_mscorlib2.cs",
@"public class C
{
    public System.Exception GetException()
    {
        return new System.Exception();
    }
}
"}};

            using (var serverData = await ServerUtil.CreateServer())
            {
                var result = RunCommandLineCompiler(CSharpCompilerClientExecutable,
                                                    $"ref_mscorlib2.cs /shared:{serverData.PipeName} /nologo /nostdlib /noconfig /t:library /r:mscorlib20.dll",
                                                    _tempDirectory, files);

                Assert.Equal("", result.Output);
                Assert.Equal(0, result.ExitCode);

                Temp.AddFile(GetResultFile(_tempDirectory, "ref_mscorlib2.dll"));

                // Create EXE "main.exe"
                files = new Dictionary<string, string> {
                                           { "main.cs",
@"using System;

class Program
{
    static void Main(string[] args)
    {
        var e = new C().GetException();
        Console.WriteLine(e);
    }
}
"}};
                result = RunCommandLineCompiler(CSharpCompilerClientExecutable,
                                                $"main.cs /shared:{serverData.PipeName} /nologo /nostdlib /noconfig /r:mscorlib40.dll /r:ref_mscorlib2.dll",
                                                _tempDirectory, files);

                Assert.Equal("", result.Output);
                Assert.Equal(0, result.ExitCode);
                await serverData.Verify(connections: 2, completed: 2).ConfigureAwait(true);
            }
        }

        [WorkItem(979588, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/979588")]
        [Fact]
        public async Task Utf8OutputInRspFileCsc()
        {
            using (var serverData = await ServerUtil.CreateServer())
            {
                var srcFile = _tempDirectory.CreateFile("test.cs").WriteAllText("♕").Path;
                var rspFile = _tempDirectory.CreateFile("temp.rsp").WriteAllText(
                    string.Format("/utf8output /nologo /t:library {0}", srcFile));

                var result = RunCommandLineCompiler(
                    CSharpCompilerClientExecutable,
                    $"/shared:{serverData.PipeName} /noconfig @{rspFile}",
                    _tempDirectory,
                    redirectEncoding: UTF8Encoding);

                Assert.Equal("test.cs(1,1): error CS1056: Unexpected character '♕'",
                    result.Output.Trim());
                Assert.Equal(1, result.ExitCode);
                await serverData.Verify(connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [WorkItem(979588, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/979588")]
        [Fact]
        public async Task Utf8OutputInRspFileVbc()
        {
            using (var serverData = await ServerUtil.CreateServer())
            {
                var srcFile = _tempDirectory.CreateFile("test.cs").WriteAllText("♕").Path;
                var rspFile = _tempDirectory.CreateFile("temp.rsp").WriteAllText(
                    string.Format("/utf8output /nologo /vbruntime* /t:library {0}", srcFile));

                var result = RunCommandLineCompiler(
                    BasicCompilerClientExecutable,
                    $"/shared:{serverData.PipeName} /noconfig @{rspFile}",
                    _tempDirectory,
                    redirectEncoding: UTF8Encoding);

                Assert.Equal(@"src.vb(1) : error BC30037: Character is not valid.

♕
~", result.Output.Trim().Replace(srcFile, "src.vb"));
                Assert.Equal(1, result.ExitCode);
                await serverData.Verify(connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [WorkItem(25777, "https://github.com/dotnet/roslyn/issues/25777")]
        [ConditionalFact(typeof(DesktopOnly), typeof(IsEnglishLocal))]
        public void BadKeepAlive1()
        {
            var result = RunCommandLineCompiler(CSharpCompilerClientExecutable, "/shared /keepalive", _tempDirectory, shouldRunOnServer: false);

            Assert.Equal(1, result.ExitCode);
            Assert.Equal("Missing argument for '/keepalive' option.", result.Output.Trim());
        }

        [WorkItem(25777, "https://github.com/dotnet/roslyn/issues/25777")]
        [ConditionalFact(typeof(DesktopOnly), typeof(IsEnglishLocal))]
        public void BadKeepAlive2()
        {
            var result = RunCommandLineCompiler(CSharpCompilerClientExecutable, "/shared /keepalive:goo", _tempDirectory, shouldRunOnServer: false);

            Assert.Equal(1, result.ExitCode);
            Assert.Equal("Argument to '/keepalive' option is not a 32-bit integer.", result.Output.Trim());
        }

        [WorkItem(25777, "https://github.com/dotnet/roslyn/issues/25777")]
        [ConditionalFact(typeof(DesktopOnly), typeof(IsEnglishLocal))]
        public void BadKeepAlive3()
        {
            var result = RunCommandLineCompiler(CSharpCompilerClientExecutable, "/shared /keepalive:-100", _tempDirectory, shouldRunOnServer: false);

            Assert.Equal(1, result.ExitCode);
            Assert.Equal("Arguments to '/keepalive' option below -1 are invalid.", result.Output.Trim());
        }

        [WorkItem(25777, "https://github.com/dotnet/roslyn/issues/25777")]
        [ConditionalFact(typeof(DesktopOnly), typeof(IsEnglishLocal))]
        public void BadKeepAlive4()
        {
            var result = RunCommandLineCompiler(CSharpCompilerClientExecutable, "/shared /keepalive:9999999999", _tempDirectory, shouldRunOnServer: false);

            Assert.Equal(1, result.ExitCode);
            Assert.Equal("Argument to '/keepalive' option is not a 32-bit integer.", result.Output.Trim());
        }

        [ConditionalFact(typeof(DesktopOnly))]
        [WorkItem(1024619, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1024619")]
        public async Task Bug1024619_01()
        {
            using (var serverData = await ServerUtil.CreateServer())
            {
                var srcFile = _tempDirectory.CreateFile("test.cs").WriteAllText("").Path;

                _tempDirectory.CreateDirectory("Temp");
                var tmp = Path.Combine(_tempDirectory.Path, "Temp");

                var result = RunCommandLineCompiler(
                    CSharpCompilerClientExecutable,
                    $"/shared:{serverData.PipeName} /nologo /t:library {srcFile}",
                    _tempDirectory,
                    additionalEnvironmentVars: new Dictionary<string, string> { { "TMP", tmp } });

                Assert.Equal(0, result.ExitCode);

                Directory.Delete(tmp);

                result = RunCommandLineCompiler(
                    CSharpCompilerClientExecutable,
                    $"/nologo /t:library {srcFile}",
                    _tempDirectory,
                    shouldRunOnServer: false);

                Assert.Equal("", result.Output.Trim());
                Assert.Equal(0, result.ExitCode);
                await serverData.Verify(connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [Fact]
        [WorkItem(1024619, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1024619")]
        public async Task Bug1024619_02()
        {
            using (var serverData = await ServerUtil.CreateServer())
            {
                var srcFile = _tempDirectory.CreateFile("test.vb").WriteAllText("").Path;

                _tempDirectory.CreateDirectory("Temp");
                var tmp = Path.Combine(_tempDirectory.Path, "Temp");

                var result = RunCommandLineCompiler(
                    BasicCompilerClientExecutable,
                    $"/shared:{serverData.PipeName} /vbruntime* /nologo /t:library {srcFile}",
                    _tempDirectory,
                    additionalEnvironmentVars: new Dictionary<string, string> { { "TMP", tmp } });

                Assert.Equal("", result.Output.Trim());
                Assert.Equal(0, result.ExitCode);

                Directory.Delete(tmp);

                result = RunCommandLineCompiler(
                    CSharpCompilerClientExecutable,
                    $"/shared:{serverData.PipeName} /nologo /t:library {srcFile}",
                    _tempDirectory);

                Assert.Equal("", result.Output.Trim());
                Assert.Equal(0, result.ExitCode);
                await serverData.Verify(connections: 2, completed: 2).ConfigureAwait(true);
            }
        }

        [WorkItem(406649, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=406649")]
        [ConditionalFact(typeof(DesktopOnly))]
        public void MissingCompilerAssembly_CompilerServer()
        {
            var basePath = Path.GetDirectoryName(typeof(CompilerServerUnitTests).Assembly.Location);
            var compilerServerExecutable = Path.Combine(basePath, "VBCSCompiler.exe");
            var dir = Temp.CreateDirectory();
            var workingDirectory = dir.Path;
            var serverExe = dir.CopyFile(compilerServerExecutable).Path;
            dir.CopyFile(typeof(System.Collections.Immutable.ImmutableArray).Assembly.Location);

            // Missing Microsoft.CodeAnalysis.dll launching server.
            var result = ProcessUtilities.Run(serverExe, arguments: $"-pipename:{GetUniqueName()}", workingDirectory: workingDirectory);
            Assert.Equal(1, result.ExitCode);
            // Exception is logged rather than written to output/error streams.
            Assert.Equal("", result.Output.Trim());
        }

        [WorkItem(406649, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=406649")]
        [WorkItem(19213, "https://github.com/dotnet/roslyn/issues/19213")]
        [Fact(Skip = "19213")]
        public async Task MissingCompilerAssembly_CompilerServerHost()
        {
            var host = new TestableCompilerServerHost((request, cancellationToken) =>
            {
                throw new FileNotFoundException();
            });
            using (var serverData = await ServerUtil.CreateServer(compilerServerHost: host))
            {
                var request = new BuildRequest(1, RequestLanguage.CSharpCompile, string.Empty, new BuildRequest.Argument[0]);
                var compileTask = ServerUtil.Send(serverData.PipeName, request);
                var response = await compileTask;
                Assert.Equal(BuildResponse.ResponseType.Completed, response.Type);
                Assert.Equal(0, ((CompletedBuildResponse)response).ReturnCode);
                await serverData.Verify(connections: 1, completed: 1).ConfigureAwait(true);
            }
        }
    }
}
