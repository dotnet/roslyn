// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

extern alias MSBuildTask;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.Win32;
using Roslyn.Test.Utilities;
using Xunit;
using System.Xml;
using System.Threading.Tasks;
using MSBuildTask::Microsoft.CodeAnalysis.BuildTasks;
using Microsoft.CodeAnalysis.CommandLine;
using Moq;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    public class CompilerServerUnitTests : TestBase
    {
        private const string CompilerServerExeName = "VBCSCompiler.exe";
        private const string CSharpClientExeName = "csc.exe";
        private const string BasicClientExeName = "vbc.exe";

        /// <summary>
        /// True when we are running the unit tests on a machine where we did not build.  In that case we need to 
        /// pick up the executables and our dependencies in their installed locations.
        /// </summary>
        internal static bool IsRunningAgainstInstallation { get; }
        internal static string CompilerDirectory { get; }
        internal static string CSharpCompilerClientExecutable { get; }
        internal static string BasicCompilerClientExecutable { get; }
        internal static string CompilerServerExecutable { get; }

        static CompilerServerUnitTests()
        {
            var basePath = Path.GetDirectoryName(typeof(CompilerServerUnitTests).Assembly.Location);
            if (!File.Exists(Path.Combine(basePath, CompilerServerExeName)) || 
                !File.Exists(Path.Combine(basePath, CSharpClientExeName)) || 
                !File.Exists(Path.Combine(basePath, BasicClientExeName)))
            {
                IsRunningAgainstInstallation = true;

                // VBCSCompiler is used as a DLL in these tests, need to hook the resolve to the installed location.
                AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
                basePath = GetMSBuildDirectory();
                if (basePath == null)
                {
                    return;
                }
            }

            CompilerDirectory = basePath;
            CSharpCompilerClientExecutable = Path.Combine(basePath, CSharpClientExeName);
            BasicCompilerClientExecutable = Path.Combine(basePath, BasicClientExeName);
            CompilerServerExecutable = Path.Combine(basePath, CompilerServerExeName);
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs e)
        {
            if (e.Name.StartsWith("VBCSCompiler"))
            {
                return Assembly.LoadFrom(CompilerServerExecutable);
            }

            return null;
        }

        private static string GetMSBuildDirectory()
        {
            using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\MSBuild\ToolsVersions\14.0", false))
            {
                if (key != null)
                {
                    var toolsPath = key.GetValue("MSBuildToolsPath");
                    if (toolsPath != null)
                    {
                        return toolsPath.ToString();
                    }
                }
            }

            return null;
        }

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
@"Imports System.Diagnostics

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
            if (CompilerDirectory == null)
            {
                throw new InvalidOperationException("Could not locate the compilers");
            }

            _tempDirectory = Temp.CreateDirectory();
        }

        #region Helpers

        private IEnumerable<KeyValuePair<string, string>> AddForLoggingEnvironmentVars(IEnumerable<KeyValuePair<string, string>> vars)
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

        private static void Kill(Process process)
        {
            try
            {
                process.Kill();
                process.WaitForExit();
            }
            catch (Exception)
            {
                // Happens when process is killed before the Kill command is executed.  That's fine.  We
                // just want to make sure the process is gone.
            }
        }

        private static void CheckForBadShared(string arguments)
        {
            bool hasShared;
            string keepAlive;
            string errorMessage;
            string pipeName;
            List<string> parsedArgs;
            if (CommandLineParser.TryParseClientArgs(
                    arguments.Split(' '),
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

        public Process StartProcess(string fileName, string arguments, string workingDirectory = null)
        {
            CheckForBadShared(arguments);
            return ProcessUtilities.StartProcess(fileName, arguments, workingDirectory);
        }

        private ProcessResult RunCommandLineCompiler(
            string compilerPath,
            string arguments,
            string currentDirectory,
            IEnumerable<KeyValuePair<string, string>> additionalEnvironmentVars = null)
        {
            CheckForBadShared(arguments);
            return ProcessUtilities.Run(
                compilerPath,
                arguments,
                currentDirectory,
                additionalEnvironmentVars: AddForLoggingEnvironmentVars(additionalEnvironmentVars));
        }

        private ProcessResult RunCommandLineCompiler(
            string compilerPath,
            string arguments,
            TempDirectory currentDirectory,
            IEnumerable<KeyValuePair<string, string>> filesInDirectory,
            IEnumerable<KeyValuePair<string, string>> additionalEnvironmentVars = null)
        {
            CheckForBadShared(arguments);
            foreach (var pair in filesInDirectory)
            {
                TempFile file = currentDirectory.CreateFile(pair.Key);
                file.WriteAllText(pair.Value);
            }

            return RunCommandLineCompiler(
                compilerPath,
                arguments,
                currentDirectory.Path,
                additionalEnvironmentVars: AddForLoggingEnvironmentVars(additionalEnvironmentVars));
        }

        private DisposableFile GetResultFile(TempDirectory directory, string resultFileName)
        {
            return new DisposableFile(Path.Combine(directory.Path, resultFileName));
        }

        private ProcessResult RunCompilerOutput(TempFile file)
        {
            return ProcessUtilities.Run(file.Path, "", Path.GetDirectoryName(file.Path));
        }

        private static void VerifyResult(ProcessResult result)
        {
            Assert.Equal("", result.Output);
            Assert.Equal("", result.Errors);
            Assert.Equal(0, result.ExitCode);
        }

        private void VerifyResultAndOutput(ProcessResult result, TempDirectory path, string expectedOutput)
        {
            using (var resultFile = GetResultFile(path, "hello.exe"))
            {
                VerifyResult(result);

                var runningResult = RunCompilerOutput(resultFile);
                Assert.Equal(expectedOutput, runningResult.Output);
            }
        }

        #endregion

        private static async Task Verify(ServerData serverData, int connections, int completed)
        {
            var serverStats = await serverData.Complete().ConfigureAwait(true);
            Assert.Equal(connections, serverStats.Connections);
            Assert.Equal(completed, serverStats.CompletedConnections);
        }

        [Fact]
        public async Task FallbackToCsc()
        {
            // Verify csc will fall back to command line when server fails to process
            using (var serverData = ServerUtil.CreateServerFailsConnection())
            {
                var result = RunCommandLineCompiler(CSharpCompilerClientExecutable, $"/shared:{serverData.PipeName} /nologo hello.cs", _tempDirectory, s_helloWorldSrcCs);
                VerifyResultAndOutput(result, _tempDirectory, "Hello, world.\r\n");
                await Verify(serverData, connections: 1, completed: 0).ConfigureAwait(true);
            }
        }

        [Fact]
        public async Task CscFallBackOutputNoUtf8()
        {
            // Verify csc will fall back to command line when server fails to process
            using (var serverData = ServerUtil.CreateServerFailsConnection())
            {
                var files = new Dictionary<string, string> { { "hello.cs", "♕" } };

                var result = RunCommandLineCompiler(CSharpCompilerClientExecutable, $"/shared:{serverData.PipeName} /nologo hello.cs", _tempDirectory, files);
                Assert.Equal(result.ExitCode, 1);
                Assert.True(result.ContainsErrors);
                Assert.Equal("hello.cs(1,1): error CS1056: Unexpected character '?'", result.Output.Trim());
                await Verify(serverData, connections: 1, completed: 0).ConfigureAwait(true);
            }
        }

        [Fact]
        public async Task CscFallBackOutputUtf8()
        {
            var srcFile = _tempDirectory.CreateFile("test.cs").WriteAllText("♕").Path;
            var tempOut = _tempDirectory.CreateFile("output.txt");

            using (var serverData = ServerUtil.CreateServerFailsConnection())
            {
                var result = ProcessUtilities.Run("cmd",
                    string.Format("/C {0} /shared:{3} /utf8output /nologo /t:library {1} > {2}",
                    CSharpCompilerClientExecutable,
                    srcFile,
                    tempOut.Path,
                    serverData.PipeName));

                Assert.Equal("", result.Output.Trim());
                Assert.Equal("test.cs(1,1): error CS1056: Unexpected character '♕'".Trim(),
                    tempOut.ReadAllText().Trim().Replace(srcFile, "test.cs"));
                Assert.Equal(1, result.ExitCode);
                await Verify(serverData, connections: 1, completed: 0).ConfigureAwait(true);
            }
        }

        [Fact]
        public async Task VbcFallbackNoUtf8()
        {
            var srcFile = _tempDirectory.CreateFile("test.vb").WriteAllText("♕").Path;

            using (var serverData = ServerUtil.CreateServerFailsConnection())
            {
                var result = ProcessUtilities.Run(
                    BasicCompilerClientExecutable,
                    $"/shared:{serverData.PipeName} /nologo test.vb",
                    _tempDirectory.Path);

                Assert.Equal(result.ExitCode, 1);
                Assert.True(result.ContainsErrors);
                Assert.Equal(@"test.vb(1) : error BC30037: Character is not valid.

?
~", result.Output.Trim().Replace(srcFile, "test.vb"));
                await Verify(serverData, connections: 1, completed: 0).ConfigureAwait(true);
            }
        }

        [Fact]
        public async Task VbcFallbackUtf8()
        {
            var srcFile = _tempDirectory.CreateFile("test.vb").WriteAllText("♕").Path;
            var tempOut = _tempDirectory.CreateFile("output.txt");

            using (var serverData = ServerUtil.CreateServerFailsConnection())
            {
                var result = ProcessUtilities.Run("cmd",
                    string.Format("/C {0} /shared:{3} /utf8output /nologo /t:library {1} > {2}",
                    BasicCompilerClientExecutable,
                    srcFile, 
                    tempOut.Path,
                    serverData.PipeName));

                Assert.Equal("", result.Output.Trim());
                Assert.Equal(@"test.vb(1) : error BC30037: Character is not valid.

♕
~", tempOut.ReadAllText().Trim().Replace(srcFile, "test.vb"));
                Assert.Equal(1, result.ExitCode);
                await Verify(serverData, connections: 1, completed: 0).ConfigureAwait(true);
            }
        }

        [Fact]
        public async Task FallbackToVbc()
        {
            using (var serverData = ServerUtil.CreateServerFailsConnection())
            {
                var result = RunCommandLineCompiler(BasicCompilerClientExecutable, $"/shared:{serverData.PipeName} /nologo hello.vb", _tempDirectory, s_helloWorldSrcVb);
                VerifyResultAndOutput(result, _tempDirectory, "Hello from VB\r\n");
                await Verify(serverData, connections: 1, completed: 0).ConfigureAwait(true);
            }
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task HelloWorldCS()
        {
            using (var serverData = ServerUtil.CreateServer())
            {
                var result = RunCommandLineCompiler(CSharpCompilerClientExecutable, $"/shared:{serverData.PipeName} /nologo hello.cs", _tempDirectory, s_helloWorldSrcCs);
                VerifyResultAndOutput(result, _tempDirectory, "Hello, world.\r\n");
                await Verify(serverData, connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [Fact]
        [WorkItem(946954)]
        public void CompilerBinariesAreNotX86()
        {
            Assert.NotEqual(ProcessorArchitecture.X86,
                AssemblyName.GetAssemblyName(CompilerServerExecutable).ProcessorArchitecture);
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
            using (var serverData = ServerUtil.CreateServer())
            {
                var files = new Dictionary<string, string> { { "c.cs", "class C {}" } };
                var result = RunCommandLineCompiler(CSharpCompilerClientExecutable,
                                                    $"/shared:{serverData.PipeName} /nologo /t:library /platform:x86 c.cs",
                                                    _tempDirectory,
                                                    files);
                VerifyResult(result);
                await Verify(serverData, connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task Platformx86MscorlibVbc()
        {
            using (var serverData = ServerUtil.CreateServer())
            {
                var files = new Dictionary<string, string> { { "c.vb", "Class C\nEnd Class" } };
                var result = RunCommandLineCompiler(BasicCompilerClientExecutable,
                                                    $"/shared:{serverData.PipeName} /nologo /t:library /platform:x86 c.vb",
                                                    _tempDirectory,
                                                    files);
                VerifyResult(result);
                await Verify(serverData, connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task ExtraMSCorLibCS()
        {
            using (var serverData = ServerUtil.CreateServer())
            {
                var result = RunCommandLineCompiler(CSharpCompilerClientExecutable,
                                                    $"/shared:{serverData.PipeName} /nologo /r:mscorlib.dll hello.cs",
                                                    _tempDirectory,
                                                    s_helloWorldSrcCs);
                VerifyResultAndOutput(result, _tempDirectory, "Hello, world.\r\n");
                await Verify(serverData, connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task HelloWorldVB()
        {
            using (var serverData = ServerUtil.CreateServer())
            {
                var result = RunCommandLineCompiler(BasicCompilerClientExecutable,
                                                    $"/shared:{serverData.PipeName} /nologo /r:Microsoft.VisualBasic.dll hello.vb",
                                                    _tempDirectory,
                                                    s_helloWorldSrcVb);
                VerifyResultAndOutput(result, _tempDirectory, "Hello from VB\r\n");
                await Verify(serverData, connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task ExtraMSCorLibVB()
        {
            using (var serverData = ServerUtil.CreateServer())
            {
                var result = RunCommandLineCompiler(BasicCompilerClientExecutable,
                    $"/shared:{serverData.PipeName} /nologo /r:mscorlib.dll /r:Microsoft.VisualBasic.dll hello.vb",
                    _tempDirectory,
                    s_helloWorldSrcVb);
                VerifyResultAndOutput(result, _tempDirectory, "Hello from VB\r\n");
                await Verify(serverData, connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task CompileErrorsCS()
        {
            using (var serverData = ServerUtil.CreateServer())
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
                Assert.Contains("hello.cs(5,42): error CS1002: ; expected\r\n", result.Output, StringComparison.Ordinal);
                Assert.Equal("", result.Errors);
                Assert.Equal(1, result.ExitCode);
                Assert.False(File.Exists(Path.Combine(_tempDirectory.Path, "hello.exe")));
                await Verify(serverData, connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task CompileErrorsVB()
        {
            using (var serverData = ServerUtil.CreateServer())
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

                var result = RunCommandLineCompiler(BasicCompilerClientExecutable, $"/shared:{serverData.PipeName} /r:Microsoft.VisualBasic.dll hellovb.vb", _tempDirectory, files);

                // Should output errors, but not create output file.
                Assert.Contains("Copyright (C) Microsoft Corporation. All rights reserved.", result.Output, StringComparison.Ordinal);
                Assert.Contains("hellovb.vb(3) : error BC30625: 'Module' statement must end with a matching 'End Module'.\r\n", result.Output, StringComparison.Ordinal);
                Assert.Contains("hellovb.vb(7) : error BC30460: 'End Class' must be preceded by a matching 'Class'.\r\n", result.Output, StringComparison.Ordinal);
                Assert.Equal("", result.Errors);
                Assert.Equal(1, result.ExitCode);
                Assert.False(File.Exists(Path.Combine(_tempDirectory.Path, "hello.exe")));
                await Verify(serverData, connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task MissingFileErrorCS()
        {
            using (var serverData = ServerUtil.CreateServer())
            {
                var result = RunCommandLineCompiler(CSharpCompilerClientExecutable, $"/shared:{serverData.PipeName} missingfile.cs", _tempDirectory, new Dictionary<string, string>());

                // Should output errors, but not create output file.
                Assert.Equal("", result.Errors);
                Assert.Contains("Copyright (C) Microsoft Corporation. All rights reserved.", result.Output, StringComparison.Ordinal);
                Assert.Contains("error CS2001: Source file", result.Output, StringComparison.Ordinal);
                Assert.Equal(1, result.ExitCode);
                Assert.False(File.Exists(Path.Combine(_tempDirectory.Path, "missingfile.exe")));
                await Verify(serverData, connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task MissingReferenceErrorCS()
        {
            using (var serverData = ServerUtil.CreateServer())
            {
                var result = RunCommandLineCompiler(CSharpCompilerClientExecutable, $"/shared:{serverData.PipeName} /r:missing.dll hello.cs", _tempDirectory, s_helloWorldSrcCs);

                // Should output errors, but not create output file.
                Assert.Equal("", result.Errors);
                Assert.Contains("Copyright (C) Microsoft Corporation. All rights reserved.", result.Output, StringComparison.Ordinal);
                Assert.Contains("error CS0006: Metadata file", result.Output, StringComparison.Ordinal);
                Assert.Equal(1, result.ExitCode);
                Assert.False(File.Exists(Path.Combine(_tempDirectory.Path, "hello.exe")));
                await Verify(serverData, connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [WorkItem(546067, "DevDiv")]
        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task InvalidMetadataFileErrorCS()
        {
            using (var serverData = ServerUtil.CreateServer())
            {
                Dictionary<string, string> files =
                                       new Dictionary<string, string> {
                                               { "Lib.cs", "public class C {}"},
                                               { "app.cs", "class Test { static void Main() {} }"},
                                               };

                var result = RunCommandLineCompiler(CSharpCompilerClientExecutable, $"/shared:{serverData.PipeName} /r:Lib.cs app.cs", _tempDirectory, files);

                // Should output errors, but not create output file.
                Assert.Equal("", result.Errors);
                Assert.Contains("Copyright (C) Microsoft Corporation. All rights reserved.", result.Output, StringComparison.Ordinal);
                Assert.Contains("error CS0009: Metadata file", result.Output, StringComparison.Ordinal);
                Assert.Equal(1, result.ExitCode);
                Assert.False(File.Exists(Path.Combine(_tempDirectory.Path, "app.exe")));
                await Verify(serverData, connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task MissingFileErrorVB()
        {
            using (var serverData = ServerUtil.CreateServer())
            {
                var result = RunCommandLineCompiler(BasicCompilerClientExecutable, $"/shared:{serverData.PipeName} missingfile.vb", _tempDirectory, new Dictionary<string, string>());

                // Should output errors, but not create output file.
                Assert.Equal("", result.Errors);
                Assert.Contains("Copyright (C) Microsoft Corporation. All rights reserved.", result.Output, StringComparison.Ordinal);
                Assert.Contains("error BC2001", result.Output, StringComparison.Ordinal);
                Assert.Equal(1, result.ExitCode);
                Assert.False(File.Exists(Path.Combine(_tempDirectory.Path, "missingfile.exe")));
                await Verify(serverData, connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [Fact(), WorkItem(761131, "DevDiv")]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task MissingReferenceErrorVB()
        {
            using (var serverData = ServerUtil.CreateServer())
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

                var result = RunCommandLineCompiler(BasicCompilerClientExecutable, $"/shared:{serverData.PipeName} /nologo /r:Microsoft.VisualBasic.dll /r:missing.dll hellovb.vb", _tempDirectory, files);

                // Should output errors, but not create output file.
                Assert.Equal("", result.Errors);
                Assert.Contains("error BC2017: could not find library", result.Output, StringComparison.Ordinal);
                Assert.Equal(1, result.ExitCode);
                Assert.False(File.Exists(Path.Combine(_tempDirectory.Path, "hellovb.exe")));
                await Verify(serverData, connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [WorkItem(546067, "DevDiv")]
        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task InvalidMetadataFileErrorVB()
        {
            using (var serverData = ServerUtil.CreateServer())
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

                var result = RunCommandLineCompiler(BasicCompilerClientExecutable, $"/shared:{serverData.PipeName} /r:Lib.vb app.vb", _tempDirectory, files);

                // Should output errors, but not create output file.
                Assert.Equal("", result.Errors);
                Assert.Contains("error BC31519", result.Output, StringComparison.Ordinal);
                Assert.Equal(1, result.ExitCode);
                Assert.False(File.Exists(Path.Combine(_tempDirectory.Path, "app.exe")));
                await Verify(serverData, connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [Fact()]
        [WorkItem(723280, "DevDiv")]
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

            using (var serverData = ServerUtil.CreateServer())
            using (var tmpFile = GetResultFile(rootDirectory, "lib.dll"))
            {
                var result = RunCommandLineCompiler(BasicCompilerClientExecutable, $"src1.vb /shared:{serverData.PipeName} /nologo /t:library /out:lib.dll", rootDirectory, files);
                Assert.Equal("", result.Output);
                Assert.Equal("", result.Errors);
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
                    result = RunCommandLineCompiler(BasicCompilerClientExecutable, $"hello1.vb /shared:{serverData.PipeName} /nologo /r:Microsoft.VisualBasic.dll /r:lib.dll /out:hello1.exe", rootDirectory, files);
                    Assert.Equal("", result.Output);
                    Assert.Equal("", result.Errors);
                    Assert.Equal(0, result.ExitCode);

                    // Run hello1.exe.
                    var runningResult = RunCompilerOutput(hello1_file);
                    Assert.Equal("Hello1 from library1\r\n", runningResult.Output);

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
                        result = RunCommandLineCompiler(BasicCompilerClientExecutable, $"hello2.vb /shared:{serverData.PipeName} /nologo /r:Microsoft.VisualBasic.dll /r:lib.dll /out:hello2.exe", rootDirectory, files);
                        Assert.Equal("", result.Output);
                        Assert.Equal("", result.Errors);
                        Assert.Equal(0, result.ExitCode);

                        // Run hello2.exe.
                        runningResult = RunCompilerOutput(hello2_file);
                        Assert.Equal("Hello2 from library1\r\n", runningResult.Output);

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
                        Assert.Equal("", result.Errors);
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
                            result = RunCommandLineCompiler(BasicCompilerClientExecutable, $"hello3.vb /shared:{serverData.PipeName} /nologo /r:Microsoft.VisualBasic.dll /r:lib.dll /out:hello3.exe", rootDirectory, files);
                            Assert.Equal("", result.Output);
                            Assert.Equal("", result.Errors);
                            Assert.Equal(0, result.ExitCode);

                            // Run hello3.exe. Should work.
                            runningResult = RunCompilerOutput(hello3_file);
                            Assert.Equal("Hello3 from library3\r\n", runningResult.Output);

                            // Run hello2.exe one more time. Should have different output than before from updated library.
                            runningResult = RunCompilerOutput(hello2_file);
                            Assert.Equal("Hello2 from library2\r\n", runningResult.Output);
                        }
                    }
                }

                await Verify(serverData, connections: 5, completed: 5).ConfigureAwait(true);
            }

            GC.KeepAlive(rootDirectory);
        }

        [Fact()]
        [WorkItem(723280, "DevDiv")]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task ReferenceCachingCS()
        {
            TempDirectory rootDirectory = _tempDirectory.CreateDirectory("ReferenceCachingCS");

            using (var serverData = ServerUtil.CreateServer())
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
                Assert.Equal("", result.Errors);
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
                    Assert.Equal("", result.Errors);
                    Assert.Equal(0, result.ExitCode);

                    // Run hello1.exe.
                    var runningResult = RunCompilerOutput(hello1_file);
                    Assert.Equal("Hello1 from library1\r\n", runningResult.Output);

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
                        Assert.Equal("", result.Errors);
                        Assert.Equal(0, result.ExitCode);

                        // Run hello2.exe.
                        runningResult = RunCompilerOutput(hello2exe);
                        Assert.Equal("Hello2 from library1\r\n", runningResult.Output);

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
                        Assert.Equal("", result.Errors);
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
                            Assert.Equal("", result.Errors);
                            Assert.Equal(0, result.ExitCode);

                            // Run hello3.exe. Should work.
                            runningResult = RunCompilerOutput(hello3_file);
                            Assert.Equal("Hello3 from library3\r\n", runningResult.Output);

                            // Run hello2.exe one more time. Should have different output than before from updated library.
                            runningResult = RunCompilerOutput(hello2_file);
                            Assert.Equal("Hello2 from library2\r\n", runningResult.Output);
                        }
                    }
                }

                await Verify(serverData, connections: 5, completed: 5).ConfigureAwait(true);
            }

            GC.KeepAlive(rootDirectory);
        }

        // Set up directory for multiple simultaneous compilers.
        private TempDirectory SetupDirectory(TempRoot root, int i)
        {
            TempDirectory dir = root.CreateDirectory();
            var helloFileCs = dir.CreateFile(string.Format("hello{0}.cs", i));
            helloFileCs.WriteAllText(string.Format(
@"using System;
class Hello 
{{
    public static void Main()
    {{ Console.WriteLine(""CS Hello number {0}""); }}
}}", i));

            var helloFileVb = dir.CreateFile(string.Format("hello{0}.vb", i));
            helloFileVb.WriteAllText(string.Format(
@"Imports System
Module Hello 
    Sub Main()
       Console.WriteLine(""VB Hello number {0}"") 
    End Sub
End Module", i));

            return dir;
        }

        // Run compiler in directory set up by SetupDirectory
        private Process RunCompilerCS(TempDirectory dir, int i, ServerData serverData)
        {
            return StartProcess(CSharpCompilerClientExecutable, string.Format("/shared:{1} /nologo hello{0}.cs /out:hellocs{0}.exe", i, serverData.PipeName), dir.Path);
        }

        // Run compiler in directory set up by SetupDirectory
        private Process RunCompilerVB(TempDirectory dir, int i, ServerData serverData)
        {
            return StartProcess(BasicCompilerClientExecutable, string.Format("/shared:{1} /nologo hello{0}.vb /r:Microsoft.VisualBasic.dll /out:hellovb{0}.exe", i, serverData.PipeName), dir.Path);
        }

        // Run output in directory set up by SetupDirectory
        private void RunOutput(TempRoot root, TempDirectory dir, int i)
        {
            var exeFile = root.AddFile(GetResultFile(dir, string.Format("hellocs{0}.exe", i)));
            var runningResult = RunCompilerOutput(exeFile);
            Assert.Equal(string.Format("CS Hello number {0}\r\n", i), runningResult.Output);

            exeFile = root.AddFile(GetResultFile(dir, string.Format("hellovb{0}.exe", i)));
            runningResult = RunCompilerOutput(exeFile);
            Assert.Equal(string.Format("VB Hello number {0}\r\n", i), runningResult.Output);
        }


        [WorkItem(997372)]
        [WorkItem(761326, "DevDiv")]
        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task MultipleSimultaneousCompiles()
        {
            using (var serverData = ServerUtil.CreateServer())
            {
                // Run this many compiles simultaneously in different directories.
                const int numberOfCompiles = 10;
                TempDirectory[] directories = new TempDirectory[numberOfCompiles];
                Process[] processesVB = new Process[numberOfCompiles];
                Process[] processesCS = new Process[numberOfCompiles];

                for (int i = 0; i < numberOfCompiles; ++i)
                {
                    directories[i] = SetupDirectory(Temp, i);
                }

                for (int i = 0; i < numberOfCompiles; ++i)
                {
                    processesCS[i] = RunCompilerCS(directories[i], i, serverData);
                }

                for (int i = 0; i < numberOfCompiles; ++i)
                {
                    processesVB[i] = RunCompilerVB(directories[i], i, serverData);
                }

                for (int i = 0; i < numberOfCompiles; ++i)
                {
                    AssertNoOutputOrErrors(processesCS[i]);
                    processesCS[i].WaitForExit();
                    processesCS[i].Close();
                    AssertNoOutputOrErrors(processesVB[i]);
                    processesVB[i].WaitForExit();
                    processesVB[i].Close();
                }

                for (int i = 0; i < numberOfCompiles; ++i)
                {
                    RunOutput(Temp, directories[i], i);
                }

                var total = numberOfCompiles * 2;
                await Verify(serverData, total, total);
            }
        }

        private void AssertNoOutputOrErrors(Process process)
        {
            Assert.Equal(string.Empty, process.StandardOutput.ReadToEnd());
            Assert.Equal(string.Empty, process.StandardError.ReadToEnd());
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

            using (var serverData = ServerUtil.CreateServer())
            {
                var result = RunCommandLineCompiler(CSharpCompilerClientExecutable,
                                                    $"src1.cs /shared:{serverData.PipeName} /nologo /t:library /out:" + libDirectory.Path + "\\lib.dll",
                                                    _tempDirectory, files);

                Assert.Equal("", result.Output);
                Assert.Equal("", result.Errors);
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
                Assert.Equal("", result.Errors);
                Assert.Equal(0, result.ExitCode);

                var resultFile = Temp.AddFile(GetResultFile(_tempDirectory, "hello1.exe"));
                await Verify(serverData, connections: 2, completed: 2).ConfigureAwait(true);
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

            using (var serverData = ServerUtil.CreateServer())
            {
                var result = RunCommandLineCompiler(BasicCompilerClientExecutable,
                                                    $"src1.vb /shared:{serverData.PipeName} /nologo /t:library /out:" + libDirectory.Path + "\\lib.dll",
                                                    _tempDirectory, files);

                Assert.Equal("", result.Output);
                Assert.Equal("", result.Errors);
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
                result = RunCommandLineCompiler(BasicCompilerClientExecutable, $"hello1.vb /shared:{serverData.PipeName} /nologo /r:Microsoft.VisualBasic.dll /r:lib.dll /out:hello1.exe", _tempDirectory, files,
                                                additionalEnvironmentVars: new Dictionary<string, string>() { { "LIB", libDirectory.Path } });

                Assert.Equal("", result.Output);
                Assert.Equal("", result.Errors);
                Assert.Equal(0, result.ExitCode);

                var resultFile = Temp.AddFile(GetResultFile(_tempDirectory, "hello1.exe"));
                await Verify(serverData, connections: 2, completed: 2).ConfigureAwait(true);
            }
        }

        [WorkItem(545446, "DevDiv")]
        [Fact()]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task Utf8Output_WithRedirecting_Off_Shared()
        {
            var srcFile = _tempDirectory.CreateFile("test.cs").WriteAllText("♕").Path;
            var tempOut = _tempDirectory.CreateFile("output.txt");

            using (var serverData = ServerUtil.CreateServer())
            {
                var result = ProcessUtilities.Run("cmd",
                    string.Format("/C {0} /shared:{3} /nologo /t:library {1} > {2}",
                    CSharpCompilerClientExecutable,
                    srcFile, 
                    tempOut.Path,
                    serverData.PipeName));

                Assert.Equal("", result.Output.Trim());
                Assert.Equal("SRC.CS(1,1): error CS1056: Unexpected character '?'".Trim(),
                    tempOut.ReadAllText().Trim().Replace(srcFile, "SRC.CS"));
                Assert.Equal(1, result.ExitCode);
                await Verify(serverData, connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [WorkItem(545446, "DevDiv")]
        [Fact()]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task Utf8Output_WithRedirecting_Off_Share()
        {
            var srcFile = _tempDirectory.CreateFile("test.vb").WriteAllText(@"♕").Path;
            var tempOut = _tempDirectory.CreateFile("output.txt");

            using (var serverData = ServerUtil.CreateServer())
            {
                var result = ProcessUtilities.Run("cmd", string.Format("/C {0} /nologo /shared:{3} /t:library {1} > {2}",
                    BasicCompilerClientExecutable,
                    srcFile,
                    tempOut.Path,
                    serverData.PipeName));

                Assert.Equal("", result.Output.Trim());
                Assert.Equal(@"SRC.VB(1) : error BC30037: Character is not valid.

?
~
".Trim(),
                            tempOut.ReadAllText().Trim().Replace(srcFile, "SRC.VB"));
                Assert.Equal(1, result.ExitCode);
                await Verify(serverData, connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [WorkItem(545446, "DevDiv")]
        [Fact()]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task Utf8Output_WithRedirecting_On_Shared_CS()
        {
            var srcFile = _tempDirectory.CreateFile("test.cs").WriteAllText("♕").Path;
            var tempOut = _tempDirectory.CreateFile("output.txt");

            using (var serverData = ServerUtil.CreateServer())
            {
                var result = ProcessUtilities.Run("cmd", string.Format("/C {0} /shared:{3} /utf8output /nologo /t:library {1} > {2}",
                    CSharpCompilerClientExecutable,
                    srcFile,
                    tempOut.Path,
                    serverData.PipeName));

                Assert.Equal("", result.Output.Trim());
                Assert.Equal("SRC.CS(1,1): error CS1056: Unexpected character '♕'".Trim(),
                    tempOut.ReadAllText().Trim().Replace(srcFile, "SRC.CS"));
                Assert.Equal(1, result.ExitCode);
                await Verify(serverData, connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [WorkItem(545446, "DevDiv")]
        [Fact()]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public async Task Utf8Output_WithRedirecting_On_Shared_VB()
        {
            var srcFile = _tempDirectory.CreateFile("test.vb").WriteAllText(@"♕").Path;
            var tempOut = _tempDirectory.CreateFile("output.txt");

            using (var serverData = ServerUtil.CreateServer())
            {
                var result = ProcessUtilities.Run("cmd", string.Format("/C {0} /utf8output /nologo /shared:{3} /t:library {1} > {2}",
                    BasicCompilerClientExecutable,
                    srcFile,
                    tempOut.Path,
                    serverData.PipeName));

                Assert.Equal("", result.Output.Trim());
                Assert.Equal(@"SRC.VB(1) : error BC30037: Character is not valid.

♕
~
".Trim(),
                            tempOut.ReadAllText().Trim().Replace(srcFile, "SRC.VB"));
                Assert.Equal(1, result.ExitCode);
                await Verify(serverData, connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [WorkItem(871477, "DevDiv")]
        [Fact]
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
        return null;
    }
}
"}};

            using (var serverData = ServerUtil.CreateServer())
            {
                var result = RunCommandLineCompiler(CSharpCompilerClientExecutable,
                                                    $"ref_mscorlib2.cs /shared:{serverData.PipeName} /nologo /nostdlib /noconfig /t:library /r:mscorlib20.dll",
                                                    _tempDirectory, files);

                Assert.Equal("", result.Output);
                Assert.Equal("", result.Errors);
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
                Assert.Equal("", result.Errors);
                Assert.Equal(0, result.ExitCode);
                await Verify(serverData, connections: 2, completed: 2).ConfigureAwait(true);
            }
        }

        [WorkItem(979588)]
        [Fact]
        public async Task Utf8OutputInRspFileCsc()
        {
            using (var serverData = ServerUtil.CreateServer())
            {
                var srcFile = _tempDirectory.CreateFile("test.cs").WriteAllText("♕").Path;
                var tempOut = _tempDirectory.CreateFile("output.txt");
                var rspFile = _tempDirectory.CreateFile("temp.rsp").WriteAllText(
                    string.Format("/utf8output /nologo /t:library {0}", srcFile));

                var result = ProcessUtilities.Run("cmd",
                    string.Format(
                        "/C {0} /shared:{3} /noconfig @{1} > {2}",
                        CSharpCompilerClientExecutable,
                        rspFile,
                        tempOut,
                        serverData.PipeName));

                Assert.Equal("", result.Output.Trim());
                Assert.Equal("src.cs(1,1): error CS1056: Unexpected character '♕'",
                    tempOut.ReadAllText().Trim().Replace(srcFile, "src.cs"));
                Assert.Equal(1, result.ExitCode);
                await Verify(serverData, connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [WorkItem(979588)]
        [Fact]
        public async Task Utf8OutputInRspFileVbc()
        {
            using (var serverData = ServerUtil.CreateServer())
            {
                var srcFile = _tempDirectory.CreateFile("test.cs").WriteAllText("♕").Path;
                var tempOut = _tempDirectory.CreateFile("output.txt");
                var rspFile = _tempDirectory.CreateFile("temp.rsp").WriteAllText(
                    string.Format("/utf8output /nologo /t:library {0}", srcFile));

                var result = ProcessUtilities.Run("cmd",
                    string.Format(
                        "/C {0} /shared:{3} /noconfig @{1} > {2}",
                        BasicCompilerClientExecutable,
                        rspFile,
                        tempOut,
                        serverData.PipeName));

                Assert.Equal("", result.Output.Trim());
                Assert.Equal(@"src.vb(1) : error BC30037: Character is not valid.

♕
~", tempOut.ReadAllText().Trim().Replace(srcFile, "src.vb"));
                Assert.Equal(1, result.ExitCode);
                await Verify(serverData, connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [Fact(Skip = "DevDiv 1095079"), WorkItem(1095079)]
        public async Task ServerRespectsAppConfig()
        {
            var exeConfigPath = Path.Combine(CompilerDirectory, CompilerServerExeName + ".config");
            var doc = new XmlDocument();
            using (XmlReader reader = XmlReader.Create(exeConfigPath, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
            {
                doc.Load(reader);
            }
            var root = doc.DocumentElement;

            root.SelectSingleNode("appSettings/add/@value").Value = "1";
            doc.Save(exeConfigPath);

            var proc = StartProcess(CompilerServerExecutable, "");
            await Task.Delay(TimeSpan.FromSeconds(3)).ConfigureAwait(false); // Give 2s leeway

            var exited = proc.HasExited;
            if (!exited)
            {
                proc.Kill();
                Assert.True(false, "Compiler server did not exit in time");
            }
        }

        [Fact]
        public void BadKeepAlive1()
        {
            var result = RunCommandLineCompiler(CSharpCompilerClientExecutable, "/shared /keepalive", _tempDirectory.Path);

            Assert.True(result.ContainsErrors);
            Assert.Equal(1, result.ExitCode);
            Assert.Equal("Missing argument for '/keepalive' option.", result.Output.Trim());
            Assert.Equal("", result.Errors);
        }

        [Fact]
        public void BadKeepAlive2()
        {
            var result = RunCommandLineCompiler(CSharpCompilerClientExecutable, "/shared /keepalive:foo", _tempDirectory.Path);

            Assert.True(result.ContainsErrors);
            Assert.Equal(1, result.ExitCode);
            Assert.Equal("Argument to '/keepalive' option is not a 32-bit integer.", result.Output.Trim());
            Assert.Equal("", result.Errors);
        }

        [Fact]
        public void BadKeepAlive3()
        {
            var result = RunCommandLineCompiler(CSharpCompilerClientExecutable, "/shared /keepalive:-100", _tempDirectory.Path);

            Assert.True(result.ContainsErrors);
            Assert.Equal(1, result.ExitCode);
            Assert.Equal("Arguments to '/keepalive' option below -1 are invalid.", result.Output.Trim());
            Assert.Equal("", result.Errors);
        }

        [Fact]
        public void BadKeepAlive4()
        {
            var result = RunCommandLineCompiler(CSharpCompilerClientExecutable, "/shared /keepalive:9999999999", _tempDirectory.Path);

            Assert.True(result.ContainsErrors);
            Assert.Equal(1, result.ExitCode);
            Assert.Equal("Argument to '/keepalive' option is not a 32-bit integer.", result.Output.Trim());
            Assert.Equal("", result.Errors);
        }

        [Fact, WorkItem(1024619, "DevDiv")]
        public async Task Bug1024619_01()
        {
            using (var serverData = ServerUtil.CreateServer())
            {
                var srcFile = _tempDirectory.CreateFile("test.cs").WriteAllText("").Path;

                _tempDirectory.CreateDirectory("Temp");
                var tmp = Path.Combine(_tempDirectory.Path, "Temp");

                var result = ProcessUtilities.Run("cmd",
                    string.Format("/C \"SET TMP={2} && {0} /shared:{3} /nologo /t:library {1}\"",
                    CSharpCompilerClientExecutable,
                    srcFile,
                    tmp,
                    serverData.PipeName));

                Assert.Equal(0, result.ExitCode);

                Directory.Delete(tmp);

                result = ProcessUtilities.Run("cmd",
                    string.Format("/C {0} /nologo /t:library {1}",
                    CSharpCompilerClientExecutable,
                    srcFile));

                Assert.Equal("", result.Output.Trim());
                Assert.Equal(0, result.ExitCode);
                await Verify(serverData, connections: 1, completed: 1).ConfigureAwait(true);
            }
        }

        [Fact, WorkItem(1024619, "DevDiv")]
        public async Task Bug1024619_02()
        {
            using (var serverData = ServerUtil.CreateServer())
            {
                var srcFile = _tempDirectory.CreateFile("test.vb").WriteAllText("").Path;

                _tempDirectory.CreateDirectory("Temp");
                var tmp = Path.Combine(_tempDirectory.Path, "Temp");

                var result = ProcessUtilities.Run("cmd",
                    string.Format("/C \"SET TMP={2} && {0} /shared:{3} /nologo /t:library {1}\"",
                    BasicCompilerClientExecutable,
                    srcFile,
                    tmp,
                    serverData.PipeName));

                Assert.Equal(0, result.ExitCode);

                Directory.Delete(tmp);

                result = ProcessUtilities.Run("cmd",
                    string.Format("/C {0} /shared:{2} /nologo /t:library {1}",
                    BasicCompilerClientExecutable,
                    srcFile,
                    serverData.PipeName));

                Assert.Equal("", result.Output.Trim());
                Assert.Equal(0, result.ExitCode);
                await Verify(serverData, connections: 2, completed: 2).ConfigureAwait(true);
            }
        }
    }
}
