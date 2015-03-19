﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using ProprietaryTestResources = Microsoft.CodeAnalysis.Test.Resources.Proprietary;
using System.Xml;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BuildTasks;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    public class CompilerServerUnitTests : TestBase
    {
        private const string CompilerServerExeName = "VBCSCompiler.exe";
        private const string CSharpClientExeName = "csc2.exe";
        private const string BasicClientExeName = "vbc2.exe";
        private const string BuildTaskDllName = "Microsoft.Build.Tasks.Roslyn.dll";

        private static string s_msbuildDirectory;
        private static string MSBuildDirectory
        {
            get
            {
                if (s_msbuildDirectory == null)
                {
                    var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\MSBuild\ToolsVersions\14.0", false);

                    if (key != null)
                    {
                        var toolsPath = key.GetValue("MSBuildToolsPath");
                        if (toolsPath != null)
                        {
                            s_msbuildDirectory = toolsPath.ToString();
                        }
                    }
                }
                return s_msbuildDirectory;
            }
        }

        private static string MSBuildExecutable { get; } = Path.Combine(MSBuildDirectory, "MSBuild.exe");

        private static readonly string s_workingDirectory = Environment.CurrentDirectory;
        private static string ResolveAssemblyPath(string exeName)
        {
            var path = Path.Combine(s_workingDirectory, exeName);
            if (File.Exists(path))
            {
                return path;
            }
            else
            {
                path = Path.Combine(MSBuildDirectory, exeName);
                if (File.Exists(path))
                {
                    var currentAssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
                    var loadedAssemblyVersion = Assembly.LoadFile(path).GetName().Version;
                    if (currentAssemblyVersion == loadedAssemblyVersion)
                    {
                        return path;
                    }
                }
                return null;
            }
        }

        private static readonly string s_compilerServerExecutableSrc = ResolveAssemblyPath(CompilerServerExeName);
        private static readonly string s_buildTaskDllSrc = ResolveAssemblyPath(BuildTaskDllName);
        private static readonly string s_CSharpCompilerExecutableSrc = ResolveAssemblyPath("csc.exe");
        private static readonly string s_basicCompilerExecutableSrc = ResolveAssemblyPath("vbc.exe");
        private static readonly string s_microsoftCodeAnalysisDllSrc = ResolveAssemblyPath("Microsoft.CodeAnalysis.dll");
        private static readonly string s_systemCollectionsImmutableDllSrc = ResolveAssemblyPath("System.Collections.Immutable.dll");

        // The native client executables can't be loaded via Assembly.Load, so we just use the
        // compiler server resolved path
        private static readonly string s_clientExecutableBasePath = Path.GetDirectoryName(s_compilerServerExecutableSrc);
        private static readonly string s_CSharpCompilerClientSrcPath = Path.Combine(s_clientExecutableBasePath, CSharpClientExeName);
        private static readonly string s_basicCompilerClientSrcPath = Path.Combine(s_clientExecutableBasePath, BasicClientExeName);

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

        private static readonly string[] s_allCompilerFiles =
        {
            s_CSharpCompilerExecutableSrc,
            s_basicCompilerExecutableSrc,
            s_compilerServerExecutableSrc,
            s_microsoftCodeAnalysisDllSrc,
            s_CSharpCompilerClientSrcPath,
            s_basicCompilerClientSrcPath,
            s_systemCollectionsImmutableDllSrc,
            s_buildTaskDllSrc,
            ResolveAssemblyPath("System.Reflection.Metadata.dll"),
            ResolveAssemblyPath("Microsoft.CodeAnalysis.Desktop.dll"),
            ResolveAssemblyPath("Microsoft.CodeAnalysis.CSharp.dll"),
            ResolveAssemblyPath("Microsoft.CodeAnalysis.CSharp.Desktop.dll"),
            ResolveAssemblyPath("Microsoft.CodeAnalysis.VisualBasic.dll"),
            ResolveAssemblyPath("Microsoft.CodeAnalysis.VisualBasic.Desktop.dll"),
            Path.Combine(s_clientExecutableBasePath, CompilerServerExeName + ".config"),
            Path.Combine(s_clientExecutableBasePath, "csc.rsp"),
            Path.Combine(s_clientExecutableBasePath, "vbc.rsp")
        };

        private readonly TempDirectory _tempDirectory;
        private readonly string _compilerDirectory;

        private readonly string _csharpCompilerClientExecutable;
        private readonly string _basicCompilerClientExecutable;
        private readonly string _compilerServerExecutable;
        private readonly string _buildTaskDll;

        public CompilerServerUnitTests()
        {
            _tempDirectory = Temp.CreateDirectory();

            // Copy the compiler files to a temporary directory
            _compilerDirectory = Temp.CreateDirectory().Path;
            foreach (var path in s_allCompilerFiles)
            {
                var filename = Path.GetFileName(path);
                File.Copy(path, Path.Combine(_compilerDirectory, filename));
            }

            _csharpCompilerClientExecutable = Path.Combine(_compilerDirectory, CSharpClientExeName);
            _basicCompilerClientExecutable = Path.Combine(_compilerDirectory, BasicClientExeName);
            _compilerServerExecutable = Path.Combine(_compilerDirectory, CompilerServerExeName);
            _buildTaskDll = Path.Combine(_compilerDirectory, BuildTaskDllName);
        }

        public override void Dispose()
        {
            KillCompilerServer();

            base.Dispose();
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

        private List<Process> GetProcessesByFullPath(string path)
        {
            var matchingProcesses = new List<Process>();

            var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(path));
            foreach (var p in processes)
            {
                int pathSize = path.Length * 2;
                var exeNameBuffer = new StringBuilder(pathSize);
                IntPtr handle = IntPtr.Zero;

                try
                {
                    // If the process has exited in between asking for the list and getting the handle,
                    // this will throw an exception. We want to ignore it and keep on going with the
                    // next process
                    handle = p.Handle;
                }
                catch (InvalidOperationException) { }

                if (handle != IntPtr.Zero &&
                    QueryFullProcessImageName(handle,
                                              0, // Win32 path format
                                              exeNameBuffer,
                                              ref pathSize) &&
                    string.Equals(exeNameBuffer.ToString(),
                                  path,
                                  StringComparison.OrdinalIgnoreCase))
                {
                    matchingProcesses.Add(p);
                }
            }

            return matchingProcesses;
        }

        private void KillProcess(string path)
        {
            foreach (var p in GetProcessesByFullPath(path))
            {
                p.Kill();
                p.WaitForExit();
            }
        }

        private void WaitForProcessExit(string path, TimeSpan interval)
        {
            while (GetProcessesByFullPath(path).Any())
            {
                Thread.Sleep(interval);
            }
        }

        private async Task WaitForProcessExitAsync(string path)
        {
            while (GetProcessesByFullPath(path).Any())
            {
                await Task.Yield();
            }
        }

        /// <summary>
        /// Get the file path of the executable that started this process.
        /// </summary>
        /// <param name="processHandle"></param>
        /// <param name="flags">Should always be 0: Win32 path format.</param>
        /// <param name="exeNameBuffer">Buffer for the name</param>
        /// <param name="bufferSize">
        /// Size of the buffer coming in, chars written coming out.
        /// </param>
        [DllImport("Kernel32.dll", EntryPoint = "QueryFullProcessImageNameW", CharSet = CharSet.Unicode)]
        private static extern bool QueryFullProcessImageName(
            IntPtr processHandle,
            int flags,
            StringBuilder exeNameBuffer,
            ref int bufferSize);

        // In order that the compiler server doesn't stay around and prevent future builds, we explicitly
        // kill it after each test.
        private void KillCompilerServer()
        {
            KillProcess(_compilerServerExecutable);
            KillProcess(s_compilerServerExecutableSrc);
        }

        private ProcessResult RunCommandLineCompiler(
            string compilerPath,
            string arguments,
            string currentDirectory,
            IEnumerable<KeyValuePair<string, string>> additionalEnvironmentVars = null)
        {
            return ProcessLauncher.Run(
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
            return ProcessLauncher.Run(file.Path, "", Path.GetDirectoryName(file.Path));
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

        [Fact]
        public void FallbackToCsc()
        {
            // Delete VBCSCompiler.exe so csc2 is forced to fall back to csc.exe
            File.Delete(_compilerServerExecutable);
            var result = RunCommandLineCompiler(_csharpCompilerClientExecutable, "/nologo hello.cs", _tempDirectory, s_helloWorldSrcCs);
            VerifyResultAndOutput(result, _tempDirectory, "Hello, world.\r\n");
        }

        [Fact]
        public void CscFallBackOutputNoUtf8()
        {
            var files = new Dictionary<string, string> { { "hello.cs", "♕" } };

            // Delete VBCSCompiler.exe so csc2 is forced to fall back to csc.exe
            File.Delete(_compilerServerExecutable);
            var result = RunCommandLineCompiler(_csharpCompilerClientExecutable, "/nologo hello.cs", _tempDirectory, files);
            Assert.Equal(result.ExitCode, 1);
            Assert.True(result.ContainsErrors);
            Assert.Equal("hello.cs(1,1): error CS1056: Unexpected character '?'", result.Output.Trim());
        }

        [Fact]
        public void CscFallBackOutputUtf8()
        {
            var srcFile = _tempDirectory.CreateFile("test.cs").WriteAllText("♕").Path;
            var tempOut = _tempDirectory.CreateFile("output.txt");

            // Delete VBCSCompiler.exe so csc2 is forced to fall back to csc.exe
            File.Delete(_compilerServerExecutable);

            var result = ProcessLauncher.Run("cmd",
                string.Format("/C {0} /utf8output /nologo /t:library {1} > {2}",
                _csharpCompilerClientExecutable,
                srcFile, tempOut.Path));

            Assert.Equal("", result.Output.Trim());
            Assert.Equal("test.cs(1,1): error CS1056: Unexpected character '♕'".Trim(),
                tempOut.ReadAllText().Trim().Replace(srcFile, "test.cs"));
            Assert.Equal(1, result.ExitCode);
        }

        [Fact]
        public void VbcFallbackNoUtf8()
        {
            var srcFile = _tempDirectory.CreateFile("test.vb").WriteAllText("♕").Path;

            // Delete VBCSCompiler.exe so csc2 is forced to fall back to csc.exe
            File.Delete(_compilerServerExecutable);

            var result = ProcessLauncher.Run(
                _basicCompilerClientExecutable,
                "/nologo test.vb",
                _tempDirectory.Path);

            Assert.Equal(result.ExitCode, 1);
            Assert.True(result.ContainsErrors);
            Assert.Equal(@"test.vb(1) : error BC30037: Character is not valid.

?
~", result.Output.Trim().Replace(srcFile, "test.vb"));
        }

        [Fact]
        public void VbcFallbackUtf8()
        {
            var srcFile = _tempDirectory.CreateFile("test.vb").WriteAllText("♕").Path;
            var tempOut = _tempDirectory.CreateFile("output.txt");

            // Delete VBCSCompiler.exe so csc2 is forced to fall back to csc.exe
            File.Delete(_compilerServerExecutable);

            var result = ProcessLauncher.Run("cmd",
                string.Format("/C {0} /utf8output /nologo /t:library {1} > {2}",
                _basicCompilerClientExecutable,
                srcFile, tempOut.Path));

            Assert.Equal("", result.Output.Trim());
            Assert.Equal(@"test.vb(1) : error BC30037: Character is not valid.

♕
~", tempOut.ReadAllText().Trim().Replace(srcFile, "test.vb"));
            Assert.Equal(1, result.ExitCode);
        }

        [Fact]
        public void FallbackToVbc()
        {
            // Delete VBCSCompiler.exe so vbc2 is forced to fall back to vbc.exe
            File.Delete(_compilerServerExecutable);
            var result = RunCommandLineCompiler(_basicCompilerClientExecutable, "/nologo hello.vb", _tempDirectory, s_helloWorldSrcVb);
            VerifyResultAndOutput(result, _tempDirectory, "Hello from VB\r\n");
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public void HelloWorldCS()
        {
            var result = RunCommandLineCompiler(_csharpCompilerClientExecutable, "/nologo hello.cs", _tempDirectory, s_helloWorldSrcCs);
            VerifyResultAndOutput(result, _tempDirectory, "Hello, world.\r\n");
        }

        [Fact]
        [WorkItem(946954)]
        public void CompilerBinariesAreNotX86()
        {
            Assert.NotEqual(ProcessorArchitecture.X86,
                AssemblyName.GetAssemblyName(_compilerServerExecutable).ProcessorArchitecture);
        }

        /// <summary>
        /// This method tests that when a 64-bit compiler server loads a 
        /// 64-bit mscorlib with /platform:x86 enabled no warning about
        /// emitting a refence to a 64-bit assembly is produced.
        /// The test should pass on x86 or amd64, but can only fail on
        /// amd64.
        /// </summary>
        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public void Platformx86MscorlibCsc()
        {
            var files = new Dictionary<string, string> { { "c.cs", "class C {}" } };
            var result = RunCommandLineCompiler(_csharpCompilerClientExecutable,
                                                "/nologo /t:library /platform:x86 c.cs",
                                                _tempDirectory,
                                                files);
            VerifyResult(result);
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public void Platformx86MscorlibVbc()
        {
            var files = new Dictionary<string, string> { { "c.vb", "Class C\nEnd Class" } };
            var result = RunCommandLineCompiler(_basicCompilerClientExecutable,
                                                "/nologo /t:library /platform:x86 c.vb",
                                                _tempDirectory,
                                                files);
            VerifyResult(result);
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public void ExtraMSCorLibCS()
        {
            var result = RunCommandLineCompiler(_csharpCompilerClientExecutable,
                                                "/nologo /r:mscorlib.dll hello.cs",
                                                _tempDirectory,
                                                s_helloWorldSrcCs);
            VerifyResultAndOutput(result, _tempDirectory, "Hello, world.\r\n");
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public void HelloWorldVB()
        {
            var result = RunCommandLineCompiler(_basicCompilerClientExecutable,
                                                "/nologo /r:Microsoft.VisualBasic.dll hello.vb",
                                                _tempDirectory,
                                                s_helloWorldSrcVb);
            VerifyResultAndOutput(result, _tempDirectory, "Hello from VB\r\n");
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public void ExtraMSCorLibVB()
        {
            var result = RunCommandLineCompiler(_basicCompilerClientExecutable,
                "/nologo /r:mscorlib.dll /r:Microsoft.VisualBasic.dll hello.vb",
                _tempDirectory,
                s_helloWorldSrcVb);
            VerifyResultAndOutput(result, _tempDirectory, "Hello from VB\r\n");
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public void CompileErrorsCS()
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

            var result = RunCommandLineCompiler(_csharpCompilerClientExecutable, "hello.cs", _tempDirectory, files);

            // Should output errors, but not create output file.                  
            Assert.Contains("Copyright (C) Microsoft Corporation. All rights reserved.", result.Output, StringComparison.Ordinal);
            Assert.Contains("hello.cs(5,42): error CS1002: ; expected\r\n", result.Output, StringComparison.Ordinal);
            Assert.Equal("", result.Errors);
            Assert.Equal(1, result.ExitCode);
            Assert.False(File.Exists(Path.Combine(_tempDirectory.Path, "hello.exe")));
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public void CompileErrorsVB()
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

            var result = RunCommandLineCompiler(_basicCompilerClientExecutable, "/r:Microsoft.VisualBasic.dll hellovb.vb", _tempDirectory, files);

            // Should output errors, but not create output file.
            Assert.Contains("Copyright (C) Microsoft Corporation. All rights reserved.", result.Output, StringComparison.Ordinal);
            Assert.Contains("hellovb.vb(3) : error BC30625: 'Module' statement must end with a matching 'End Module'.\r\n", result.Output, StringComparison.Ordinal);
            Assert.Contains("hellovb.vb(7) : error BC30460: 'End Class' must be preceded by a matching 'Class'.\r\n", result.Output, StringComparison.Ordinal);
            Assert.Equal("", result.Errors);
            Assert.Equal(1, result.ExitCode);
            Assert.False(File.Exists(Path.Combine(_tempDirectory.Path, "hello.exe")));
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public void MissingFileErrorCS()
        {
            var result = RunCommandLineCompiler(_csharpCompilerClientExecutable, "missingfile.cs", _tempDirectory, new Dictionary<string, string>());

            // Should output errors, but not create output file.
            Assert.Equal("", result.Errors);
            Assert.Contains("Copyright (C) Microsoft Corporation. All rights reserved.", result.Output, StringComparison.Ordinal);
            Assert.Contains("error CS2001: Source file", result.Output, StringComparison.Ordinal);
            Assert.Equal(1, result.ExitCode);
            Assert.False(File.Exists(Path.Combine(_tempDirectory.Path, "missingfile.exe")));
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public void MissingReferenceErrorCS()
        {
            var result = RunCommandLineCompiler(_csharpCompilerClientExecutable, "/r:missing.dll hello.cs", _tempDirectory, s_helloWorldSrcCs);

            // Should output errors, but not create output file.
            Assert.Equal("", result.Errors);
            Assert.Contains("Copyright (C) Microsoft Corporation. All rights reserved.", result.Output, StringComparison.Ordinal);
            Assert.Contains("error CS0006: Metadata file", result.Output, StringComparison.Ordinal);
            Assert.Equal(1, result.ExitCode);
            Assert.False(File.Exists(Path.Combine(_tempDirectory.Path, "hello.exe")));
        }

        [WorkItem(546067, "DevDiv")]
        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public void InvalidMetadataFileErrorCS()
        {
            Dictionary<string, string> files =
                                   new Dictionary<string, string> {
                                               { "Lib.cs", "public class C {}"},
                                               { "app.cs", "class Test { static void Main() {} }"},
                                           };

            var result = RunCommandLineCompiler(_csharpCompilerClientExecutable, "/r:Lib.cs app.cs", _tempDirectory, files);

            // Should output errors, but not create output file.
            Assert.Equal("", result.Errors);
            Assert.Contains("Copyright (C) Microsoft Corporation. All rights reserved.", result.Output, StringComparison.Ordinal);
            Assert.Contains("error CS0009: Metadata file", result.Output, StringComparison.Ordinal);
            Assert.Equal(1, result.ExitCode);
            Assert.False(File.Exists(Path.Combine(_tempDirectory.Path, "app.exe")));
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public void MissingFileErrorVB()
        {
            var result = RunCommandLineCompiler(_basicCompilerClientExecutable, "missingfile.vb", _tempDirectory, new Dictionary<string, string>());

            // Should output errors, but not create output file.
            Assert.Equal("", result.Errors);
            Assert.Contains("Copyright (C) Microsoft Corporation. All rights reserved.", result.Output, StringComparison.Ordinal);
            Assert.Contains("error BC2001", result.Output, StringComparison.Ordinal);
            Assert.Equal(1, result.ExitCode);
            Assert.False(File.Exists(Path.Combine(_tempDirectory.Path, "missingfile.exe")));
        }

        [Fact(), WorkItem(761131, "DevDiv")]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public void MissingReferenceErrorVB()
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

            var result = RunCommandLineCompiler(_basicCompilerClientExecutable, "/nologo /r:Microsoft.VisualBasic.dll /r:missing.dll hellovb.vb", _tempDirectory, files);

            // Should output errors, but not create output file.
            Assert.Equal("", result.Errors);
            Assert.Contains("error BC2017: could not find library", result.Output, StringComparison.Ordinal);
            Assert.Equal(1, result.ExitCode);
            Assert.False(File.Exists(Path.Combine(_tempDirectory.Path, "hellovb.exe")));
        }

        [WorkItem(546067, "DevDiv")]
        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public void InvalidMetadataFileErrorVB()
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

            var result = RunCommandLineCompiler(_basicCompilerClientExecutable, "/r:Lib.vb app.vb", _tempDirectory, files);

            // Should output errors, but not create output file.
            Assert.Equal("", result.Errors);
            Assert.Contains("error BC31519", result.Output, StringComparison.Ordinal);
            Assert.Equal(1, result.ExitCode);
            Assert.False(File.Exists(Path.Combine(_tempDirectory.Path, "app.exe")));
        }

        [Fact()]
        [WorkItem(723280, "DevDiv")]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public void ReferenceCachingVB()
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

            using (var tmpFile = GetResultFile(rootDirectory, "lib.dll"))
            {
                var result = RunCommandLineCompiler(_basicCompilerClientExecutable, "src1.vb /nologo /t:library /out:lib.dll", rootDirectory, files);
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
                    result = RunCommandLineCompiler(_basicCompilerClientExecutable, "hello1.vb /nologo /r:Microsoft.VisualBasic.dll /r:lib.dll /out:hello1.exe", rootDirectory, files);
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
                        result = RunCommandLineCompiler(_basicCompilerClientExecutable, "hello2.vb /nologo /r:Microsoft.VisualBasic.dll /r:lib.dll /out:hello2.exe", rootDirectory, files);
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

                        result = RunCommandLineCompiler(_basicCompilerClientExecutable, "src2.vb /nologo /t:library /out:lib.dll", rootDirectory, files);
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
                            result = RunCommandLineCompiler(_basicCompilerClientExecutable, "hello3.vb /nologo /r:Microsoft.VisualBasic.dll /r:lib.dll /out:hello3.exe", rootDirectory, files);
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
            }

            GC.KeepAlive(rootDirectory);
        }

        [Fact()]
        [WorkItem(723280, "DevDiv")]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public void ReferenceCachingCS()
        {
            TempDirectory rootDirectory = _tempDirectory.CreateDirectory("ReferenceCachingCS");

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

                var result = RunCommandLineCompiler(_csharpCompilerClientExecutable, "src1.cs /nologo /t:library /out:lib.dll", rootDirectory, files);
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
                    result = RunCommandLineCompiler(_csharpCompilerClientExecutable, "hello1.cs /nologo /r:lib.dll /out:hello1.exe", rootDirectory, files);
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
                        result = RunCommandLineCompiler(_csharpCompilerClientExecutable, "hello2.cs /nologo /r:lib.dll /out:hello2.exe", rootDirectory, files);
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

                        result = RunCommandLineCompiler(_csharpCompilerClientExecutable, "src2.cs /nologo /t:library /out:lib.dll", rootDirectory, files);
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
                            result = RunCommandLineCompiler(_csharpCompilerClientExecutable, "hello3.cs /nologo /r:lib.dll /out:hello3.exe", rootDirectory, files);
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
        private Process RunCompilerCS(TempDirectory dir, int i)
        {
            return ProcessLauncher.StartProcess(_csharpCompilerClientExecutable, string.Format("/nologo hello{0}.cs /out:hellocs{0}.exe", i), dir.Path);
        }

        // Run compiler in directory set up by SetupDirectory
        private Process RunCompilerVB(TempDirectory dir, int i)
        {
            return ProcessLauncher.StartProcess(_basicCompilerClientExecutable, string.Format("/nologo hello{0}.vb /r:Microsoft.VisualBasic.dll /out:hellovb{0}.exe", i), dir.Path);
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
        [Fact, WorkItem(761326, "DevDiv")]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public void MultipleSimultaneousCompiles()
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
                processesCS[i] = RunCompilerCS(directories[i], i);
            }

            for (int i = 0; i < numberOfCompiles; ++i)
            {
                processesVB[i] = RunCompilerVB(directories[i], i);
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
        }

        private void AssertNoOutputOrErrors(Process process)
        {
            Assert.Equal(string.Empty, process.StandardOutput.ReadToEnd());
            Assert.Equal(string.Empty, process.StandardError.ReadToEnd());
        }

        // A dictionary with name and contents of all the files we want to create for the SimpleMSBuild test.
        private Dictionary<string, string> SimpleMsBuildFiles => new Dictionary<string, string> {
{ "HelloSolution.sln",
@"
Microsoft Visual Studio Solution File, Format Version 11.00
# Visual Studio 2010
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""HelloProj"", ""HelloProj.csproj"", ""{7F4CCBA2-1184-468A-BF3D-30792E4E8003}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""HelloLib"", ""HelloLib.csproj"", ""{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}""
EndProject
Project(""{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"") = ""VBLib"", ""VBLib.vbproj"", ""{F21C894B-28E5-4212-8AF7-C8E0E5455737}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Debug|Mixed Platforms = Debug|Mixed Platforms
		Debug|x86 = Debug|x86
		Release|Any CPU = Release|Any CPU
		Release|Mixed Platforms = Release|Mixed Platforms
		Release|x86 = Release|x86
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{7F4CCBA2-1184-468A-BF3D-30792E4E8003}.Debug|Any CPU.ActiveCfg = Debug|x86
		{7F4CCBA2-1184-468A-BF3D-30792E4E8003}.Debug|Mixed Platforms.ActiveCfg = Debug|x86
		{7F4CCBA2-1184-468A-BF3D-30792E4E8003}.Debug|Mixed Platforms.Build.0 = Debug|x86
		{7F4CCBA2-1184-468A-BF3D-30792E4E8003}.Debug|x86.ActiveCfg = Debug|x86
		{7F4CCBA2-1184-468A-BF3D-30792E4E8003}.Debug|x86.Build.0 = Debug|x86
		{7F4CCBA2-1184-468A-BF3D-30792E4E8003}.Release|Any CPU.ActiveCfg = Release|x86
		{7F4CCBA2-1184-468A-BF3D-30792E4E8003}.Release|Mixed Platforms.ActiveCfg = Release|x86
		{7F4CCBA2-1184-468A-BF3D-30792E4E8003}.Release|Mixed Platforms.Build.0 = Release|x86
		{7F4CCBA2-1184-468A-BF3D-30792E4E8003}.Release|x86.ActiveCfg = Release|x86
		{7F4CCBA2-1184-468A-BF3D-30792E4E8003}.Release|x86.Build.0 = Release|x86
		{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}.Debug|Mixed Platforms.ActiveCfg = Debug|Any CPU
		{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}.Debug|Mixed Platforms.Build.0 = Debug|Any CPU
		{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}.Debug|x86.ActiveCfg = Debug|Any CPU
		{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}.Release|Any CPU.Build.0 = Release|Any CPU
		{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}.Release|Mixed Platforms.ActiveCfg = Release|Any CPU
		{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}.Release|Mixed Platforms.Build.0 = Release|Any CPU
		{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}.Release|x86.ActiveCfg = Release|Any CPU
		{F21C894B-28E5-4212-8AF7-C8E0E5455737}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{F21C894B-28E5-4212-8AF7-C8E0E5455737}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{F21C894B-28E5-4212-8AF7-C8E0E5455737}.Debug|Mixed Platforms.ActiveCfg = Debug|Any CPU
		{F21C894B-28E5-4212-8AF7-C8E0E5455737}.Debug|Mixed Platforms.Build.0 = Debug|Any CPU
		{F21C894B-28E5-4212-8AF7-C8E0E5455737}.Debug|x86.ActiveCfg = Debug|Any CPU
		{F21C894B-28E5-4212-8AF7-C8E0E5455737}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{F21C894B-28E5-4212-8AF7-C8E0E5455737}.Release|Any CPU.Build.0 = Release|Any CPU
		{F21C894B-28E5-4212-8AF7-C8E0E5455737}.Release|Mixed Platforms.ActiveCfg = Release|Any CPU
		{F21C894B-28E5-4212-8AF7-C8E0E5455737}.Release|Mixed Platforms.Build.0 = Release|Any CPU
		{F21C894B-28E5-4212-8AF7-C8E0E5455737}.Release|x86.ActiveCfg = Release|Any CPU	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal
"},

{ "HelloProj.csproj",
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <UsingTask TaskName=""Microsoft.CodeAnalysis.BuildTasks.Csc"" AssemblyFile=""" + _buildTaskDll + @""" />
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">x86</Platform>
    <ProductVersion>8.0.30703</ProductVersion>
    <SchemaVersion>2.0</SchemaVersion>
    <ProjectGuid>{7F4CCBA2-1184-468A-BF3D-30792E4E8003}</ProjectGuid>
    <OutputType>Exe</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>HelloProj</RootNamespace>
    <AssemblyName>HelloProj</AssemblyName>
    <TargetFrameworkVersion>v4.0</TargetFrameworkVersion>
    <TargetFrameworkProfile>Client</TargetFrameworkProfile>
    <FileAlignment>512</FileAlignment>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|x86' "">
    <PlatformTarget>x86</PlatformTarget>
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|x86' "">
    <PlatformTarget>x86</PlatformTarget>
    <DebugType>pdbonly</DebugType>
    <Optimize>true</Optimize>
    <OutputPath>bin\Release\</OutputPath>
    <DefineConstants>TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""System"" />
    <Reference Include=""System.Core"" />
    <Reference Include=""System.Xml.Linq"" />
    <Reference Include=""System.Xml"" />
  </ItemGroup>
  <ItemGroup>
    <Compile Include=""Program.cs"" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include=""HelloLib.csproj"">
      <Project>{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}</Project>
      <Name>HelloLib</Name>
    </ProjectReference>
    <ProjectReference Include=""VBLib.vbproj"">
      <Project>{F21C894B-28E5-4212-8AF7-C8E0E5455737}</Project>
      <Name>VBLib</Name>
    </ProjectReference>
  </ItemGroup>
  <ItemGroup>
    <Folder Include=""Properties\"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>"},

{ "Program.cs",
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HelloLib;
using VBLib;

namespace HelloProj
{
    class Program
    {
        static void Main(string[] args)
        {
            HelloLibClass.SayHello();
            VBLibClass.SayThere();
            Console.WriteLine(""World"");
        }
    }
}
"},

{ "HelloLib.csproj",
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <UsingTask TaskName=""Microsoft.CodeAnalysis.BuildTasks.Csc"" AssemblyFile=""" + _buildTaskDll + @""" />
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProductVersion>8.0.30703</ProductVersion>
    <SchemaVersion>2.0</SchemaVersion>
    <ProjectGuid>{C1170A4A-80CF-4B4F-AA58-2FAEA9158D31}</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>HelloLib</RootNamespace>
    <AssemblyName>HelloLib</AssemblyName>
    <TargetFrameworkVersion>v4.0</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
    <DebugType>pdbonly</DebugType>
    <Optimize>true</Optimize>
    <OutputPath>bin\Release\</OutputPath>
    <DefineConstants>TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""System"" />
    <Reference Include=""System.Core"" />
    <Reference Include=""System.Xml.Linq"" />
    <Reference Include=""System.Xml"" />
  </ItemGroup>
  <ItemGroup>
    <Compile Include=""HelloLib.cs"" />
  </ItemGroup>
  <ItemGroup>
    <Folder Include=""Properties\"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>"},

{ "HelloLib.cs",
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HelloLib
{
    public class HelloLibClass
    {
        public static void SayHello()
        {
            Console.WriteLine(""Hello"");
        }
    }
}
"},

 { "VBLib.vbproj",
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <UsingTask TaskName=""Microsoft.CodeAnalysis.BuildTasks.Vbc"" AssemblyFile=""" + _buildTaskDll + @""" />
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProductVersion>
    </ProductVersion>
    <SchemaVersion>
    </SchemaVersion>
    <ProjectGuid>{F21C894B-28E5-4212-8AF7-C8E0E5455737}</ProjectGuid>
    <OutputType>Library</OutputType>
    <RootNamespace>VBLib</RootNamespace>
    <AssemblyName>VBLib</AssemblyName>
    <FileAlignment>512</FileAlignment>
    <MyType>Windows</MyType>
    <TargetFrameworkVersion>v4.0</TargetFrameworkVersion>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <DefineDebug>true</DefineDebug>
    <DefineTrace>true</DefineTrace>
    <OutputPath>bin\Debug\</OutputPath>
    <DocumentationFile>VBLib.xml</DocumentationFile>
    <NoWarn>42016,41999,42017,42018,42019,42032,42036,42020,42021,42022</NoWarn>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
    <DebugType>pdbonly</DebugType>
    <DefineDebug>false</DefineDebug>
    <DefineTrace>true</DefineTrace>
    <Optimize>true</Optimize>
    <OutputPath>bin\Release\</OutputPath>
    <DocumentationFile>VBLib.xml</DocumentationFile>
    <NoWarn>42016,41999,42017,42018,42019,42032,42036,42020,42021,42022</NoWarn>
  </PropertyGroup>
  <PropertyGroup>
    <OptionExplicit>On</OptionExplicit>
  </PropertyGroup>
  <PropertyGroup>
    <OptionCompare>Binary</OptionCompare>
  </PropertyGroup>
  <PropertyGroup>
    <OptionStrict>Off</OptionStrict>
  </PropertyGroup>
  <PropertyGroup>
    <OptionInfer>On</OptionInfer>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""System"" />
  </ItemGroup>
  <ItemGroup>
    <Import Include=""Microsoft.VisualBasic"" />
    <Import Include=""System"" />
    <Import Include=""System.Collections.Generic"" />
  </ItemGroup>
  <ItemGroup>
    <Compile Include=""VBLib.vb"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.VisualBasic.targets"" />
  <!-- To modify your build process, add your task inside one of the targets below and uncomment it. 
       Other similar extension points exist, see Microsoft.Common.targets.
  -->
</Project>"},

 { "VBLib.vb",
@"
Public Class VBLibClass
    Public Shared Sub SayThere()
        Console.WriteLine(""there"")
    End Sub
End Class
"}
            };

        [Fact()]
        public void SimpleMSBuild()
        {
            string arguments = string.Format(@"/m /nr:false /t:Rebuild /p:UseRoslyn=1 HelloSolution.sln");
            var result = RunCommandLineCompiler(MSBuildExecutable, arguments, _tempDirectory, SimpleMsBuildFiles);

            using (var resultFile = GetResultFile(_tempDirectory, @"bin\debug\helloproj.exe"))
            {
                // once we stop issuing BC40998 (NYI), we can start making stronger assertions
                // about our ouptut in the general case
                if (result.ExitCode != 0)
                {
                    Assert.Equal("", result.Output);
                    Assert.Equal("", result.Errors);
                }
                Assert.Equal(0, result.ExitCode);
                var runningResult = RunCompilerOutput(resultFile);
                Assert.Equal("Hello\r\nthere\r\nWorld\r\n", runningResult.Output);
            }
        }



        private Dictionary<string, string> GetMultiFileMSBuildFiles()
        {
            // Return a dictionary with name and contents of all the files we want to create for the SimpleMSBuild test.

            return new Dictionary<string, string> {
{"ConsoleApplication1.sln",
@"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 2012
Project(""{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"") = ""Mod1"", ""Mod1.vbproj"", ""{DEF6D929-FA03-4076-8A05-7BFA33DCC829}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""assem1"", ""assem1.csproj"", ""{1245560C-55E4-49D7-904C-18281B369763}""
EndProject
Project(""{F184B08F-C81C-45F6-A57F-5ABD9991F28F}"") = ""ConsoleApplication1"", ""ConsoleApplication1.vbproj"", ""{52F3466B-DD3F-435C-ADA6-CD023CC82E91}""
EndProject
Global
    GlobalSection(SolutionConfigurationPlatforms) = preSolution
        Debug|Any CPU = Debug|Any CPU
        Release|Any CPU = Release|Any CPU
    EndGlobalSection
    GlobalSection(ProjectConfigurationPlatforms) = postSolution
        {1245560C-55E4-49D7-904C-18281B369763}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
        {1245560C-55E4-49D7-904C-18281B369763}.Debug|Any CPU.Build.0 = Debug|Any CPU
        {1245560C-55E4-49D7-904C-18281B369763}.Release|Any CPU.ActiveCfg = Release|Any CPU
        {1245560C-55E4-49D7-904C-18281B369763}.Release|Any CPU.Build.0 = Release|Any CPU
        {52F3466B-DD3F-435C-ADA6-CD023CC82E91}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
        {52F3466B-DD3F-435C-ADA6-CD023CC82E91}.Debug|Any CPU.Build.0 = Debug|Any CPU
        {52F3466B-DD3F-435C-ADA6-CD023CC82E91}.Release|Any CPU.ActiveCfg = Release|Any CPU
        {52F3466B-DD3F-435C-ADA6-CD023CC82E91}.Release|Any CPU.Build.0 = Release|Any CPU
        {DEF6D929-FA03-4076-8A05-7BFA33DCC829}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
        {DEF6D929-FA03-4076-8A05-7BFA33DCC829}.Debug|Any CPU.Build.0 = Debug|Any CPU
        {DEF6D929-FA03-4076-8A05-7BFA33DCC829}.Release|Any CPU.ActiveCfg = Release|Any CPU
        {DEF6D929-FA03-4076-8A05-7BFA33DCC829}.Release|Any CPU.Build.0 = Release|Any CPU
    EndGlobalSection
    GlobalSection(SolutionProperties) = preSolution
        HideSolutionNode = FALSE
    EndGlobalSection
EndGlobal
"},
{"ConsoleApplication1.vbproj",
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"" Condition=""Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')"" />
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProjectGuid>{52F3466B-DD3F-435C-ADA6-CD023CC82E91}</ProjectGuid>
    <OutputType>Exe</OutputType>
    <StartupObject>ConsoleApplication1.ConsoleApp</StartupObject>
    <RootNamespace>ConsoleApplication1</RootNamespace>
    <AssemblyName>ConsoleApplication1</AssemblyName>
    <FileAlignment>512</FileAlignment>
    <MyType>Console</MyType>
    <TargetFrameworkVersion>v4.5</TargetFrameworkVersion>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <PlatformTarget>AnyCPU</PlatformTarget>
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <DefineDebug>true</DefineDebug>
    <DefineTrace>true</DefineTrace>
    <OutputPath>bin\Debug</OutputPath>
    <DocumentationFile>ConsoleApplication1.xml</DocumentationFile>
    <NoWarn>42016,41999,42017,42018,42019,42032,42036,42020,42021,42022</NoWarn>
  </PropertyGroup>
  <PropertyGroup>
    <OptionExplicit>On</OptionExplicit>
  </PropertyGroup>
  <PropertyGroup>
    <OptionCompare>Binary</OptionCompare>
  </PropertyGroup>
  <PropertyGroup>
    <OptionStrict>Off</OptionStrict>
  </PropertyGroup>
  <PropertyGroup>
    <OptionInfer>On</OptionInfer>
  </PropertyGroup>
  <ItemGroup>
    <Import Include=""Microsoft.VisualBasic"" />
    <Import Include=""System"" />
  </ItemGroup>
  <ItemGroup>
    <Compile Include=""ConsoleApp.vb"" />
  </ItemGroup>
  <ItemGroup>
    <Reference Include=""obj\debug\assem1.dll"">
    </Reference>
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.VisualBasic.targets"" />
  <!-- To modify your build process, add your task inside one of the targets below and uncomment it. 
       Other similar extension points exist, see Microsoft.Common.targets.
  <Target Name=""BeforeBuild"">
  </Target>
  <Target Name=""AfterBuild"">
  </Target>
  -->
</Project>
"},
{"ConsoleApp.vb",
@"
Module ConsoleApp
    Sub Main()
        Console.WriteLine(""Hello"")
        Console.WriteLine(AssemClass.GetNames())
        Console.WriteLine(ModClass2.Name)
    End Sub
End Module
"},
{"assem1.csproj",
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"" Condition=""Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')"" />
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProjectGuid>{1245560C-55E4-49D7-904C-18281B369763}</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>assem1</RootNamespace>
    <AssemblyName>assem1</AssemblyName>
    <TargetFrameworkVersion>v4.5</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include=""Assem1.cs"" />
  </ItemGroup>
  <ItemGroup>
    <AddModules Include=""obj\Debug\Mod1.netmodule"">
    </AddModules>
  </ItemGroup>
  <ItemGroup>
    <Folder Include=""Properties\"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
  <!-- To modify your build process, add your task inside one of the targets below and uncomment it. 
       Other similar extension points exist, see Microsoft.Common.targets.
  <Target Name=""BeforeBuild"">
  </Target>
  <Target Name=""AfterBuild"">
  </Target>
  -->
</Project>
"},
{"Assem1.cs",
@"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class AssemClass
{
    public static string Name = ""AssemClass"";

    public static string GetNames()
    {
        return Name + "" "" + ModClass.Name;
    }
}
"},
{"Mod1.vbproj",
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"" Condition=""Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')"" />
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProjectGuid>{DEF6D929-FA03-4076-8A05-7BFA33DCC829}</ProjectGuid>
    <OutputType>Module</OutputType>
    <RootNamespace>
    </RootNamespace>
    <AssemblyName>Mod1</AssemblyName>
    <FileAlignment>512</FileAlignment>
    <MyType>Windows</MyType>
    <TargetFrameworkVersion>v4.5</TargetFrameworkVersion>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <DefineDebug>true</DefineDebug>
    <DefineTrace>true</DefineTrace>
    <OutputPath>bin\Debug\</OutputPath>
    <DocumentationFile>Mod1.xml</DocumentationFile>
    <NoWarn>42016,41999,42017,42018,42019,42032,42036,42020,42021,42022</NoWarn>
  </PropertyGroup>
  <PropertyGroup>
    <OptionExplicit>On</OptionExplicit>
  </PropertyGroup>
  <PropertyGroup>
    <OptionCompare>Binary</OptionCompare>
  </PropertyGroup>
  <PropertyGroup>
    <OptionStrict>Off</OptionStrict>
  </PropertyGroup>
  <PropertyGroup>
    <OptionInfer>On</OptionInfer>
  </PropertyGroup>
  <ItemGroup>
    <Import Include=""Microsoft.VisualBasic"" />
    <Import Include=""System"" />
  </ItemGroup>
  <ItemGroup>
    <Compile Include=""Mod1.vb"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.VisualBasic.targets"" />
  <!-- To modify your build process, add your task inside one of the targets below and uncomment it. 
       Other similar extension points exist, see Microsoft.Common.targets.
  <Target Name=""BeforeBuild"">
  </Target>
  <Target Name=""AfterBuild"">
  </Target>
  -->
</Project>
"},
{"Mod1.vb",
@"
Friend Class ModClass
    Public Shared Name As String = ""ModClass""
End Class

Public Class ModClass2
    Public Shared Name As String = ""ModClass2""
End Class
"}
            };
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public void UseLibVariableCS()
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

            var result = RunCommandLineCompiler(_csharpCompilerClientExecutable,
                                                "src1.cs /nologo /t:library /out:" + libDirectory.Path + "\\lib.dll",
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
            result = RunCommandLineCompiler(_csharpCompilerClientExecutable, "hello1.cs /nologo /r:lib.dll /out:hello1.exe", _tempDirectory, files,
                                            additionalEnvironmentVars: new Dictionary<string, string>() { { "LIB", libDirectory.Path } });

            Assert.Equal("", result.Output);
            Assert.Equal("", result.Errors);
            Assert.Equal(0, result.ExitCode);

            var resultFile = Temp.AddFile(GetResultFile(_tempDirectory, "hello1.exe"));
        }

        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public void UseLibVariableVB()
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

            var result = RunCommandLineCompiler(_basicCompilerClientExecutable,
                                                "src1.vb /nologo /t:library /out:" + libDirectory.Path + "\\lib.dll",
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
            result = RunCommandLineCompiler(_basicCompilerClientExecutable, "hello1.vb /nologo /r:Microsoft.VisualBasic.dll /r:lib.dll /out:hello1.exe", _tempDirectory, files,
                                            additionalEnvironmentVars: new Dictionary<string, string>() { { "LIB", libDirectory.Path } });

            Assert.Equal("", result.Output);
            Assert.Equal("", result.Errors);
            Assert.Equal(0, result.ExitCode);

            var resultFile = Temp.AddFile(GetResultFile(_tempDirectory, "hello1.exe"));
        }

        [WorkItem(545446, "DevDiv")]
        [Fact()]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public void Utf8Output_WithRedirecting_Off_csc2()
        {
            var srcFile = _tempDirectory.CreateFile("test.cs").WriteAllText("♕").Path;
            var tempOut = _tempDirectory.CreateFile("output.txt");

            var result = ProcessLauncher.Run("cmd",
                string.Format("/C {0} /nologo /t:library {1} > {2}",
                _csharpCompilerClientExecutable,
                srcFile, tempOut.Path));

            Assert.Equal("", result.Output.Trim());
            Assert.Equal("SRC.CS(1,1): error CS1056: Unexpected character '?'".Trim(),
                tempOut.ReadAllText().Trim().Replace(srcFile, "SRC.CS"));
            Assert.Equal(1, result.ExitCode);
        }

        [WorkItem(545446, "DevDiv")]
        [Fact()]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public void Utf8Output_WithRedirecting_Off_vbc2()
        {
            var srcFile = _tempDirectory.CreateFile("test.vb").WriteAllText(@"♕").Path;
            var tempOut = _tempDirectory.CreateFile("output.txt");

            var result = ProcessLauncher.Run("cmd", string.Format("/C {0} /nologo /t:library {1} > {2}",
                _basicCompilerClientExecutable,
                srcFile, tempOut.Path));

            Assert.Equal("", result.Output.Trim());
            Assert.Equal(@"SRC.VB(1) : error BC30037: Character is not valid.

?
~
".Trim(),
                        tempOut.ReadAllText().Trim().Replace(srcFile, "SRC.VB"));
            Assert.Equal(1, result.ExitCode);
        }

        [WorkItem(545446, "DevDiv")]
        [Fact()]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public void Utf8Output_WithRedirecting_On_csc2()
        {
            var srcFile = _tempDirectory.CreateFile("test.cs").WriteAllText("♕").Path;
            var tempOut = _tempDirectory.CreateFile("output.txt");

            var result = ProcessLauncher.Run("cmd", string.Format("/C {0} /utf8output /nologo /t:library {1} > {2}",
                _csharpCompilerClientExecutable,
                srcFile, tempOut.Path));

            Assert.Equal("", result.Output.Trim());
            Assert.Equal("SRC.CS(1,1): error CS1056: Unexpected character '♕'".Trim(),
                tempOut.ReadAllText().Trim().Replace(srcFile, "SRC.CS"));
            Assert.Equal(1, result.ExitCode);
        }

        [WorkItem(545446, "DevDiv")]
        [Fact()]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public void Utf8Output_WithRedirecting_On_vbc2()
        {
            var srcFile = _tempDirectory.CreateFile("test.vb").WriteAllText(@"♕").Path;
            var tempOut = _tempDirectory.CreateFile("output.txt");

            var result = ProcessLauncher.Run("cmd", string.Format("/C {0} /utf8output /nologo /t:library {1} > {2}",
                _basicCompilerClientExecutable,
                srcFile, tempOut.Path));

            Assert.Equal("", result.Output.Trim());
            Assert.Equal(@"SRC.VB(1) : error BC30037: Character is not valid.

♕
~
".Trim(),
                        tempOut.ReadAllText().Trim().Replace(srcFile, "SRC.VB"));
            Assert.Equal(1, result.ExitCode);
        }

        [WorkItem(871477, "DevDiv")]
        [Fact]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public void AssemblyIdentityComparer1()
        {
            _tempDirectory.CreateFile("mscorlib20.dll").WriteAllBytes(ProprietaryTestResources.NetFX.v2_0_50727.mscorlib);
            _tempDirectory.CreateFile("mscorlib40.dll").WriteAllBytes(ProprietaryTestResources.NetFX.v4_0_21006.mscorlib);

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

            var result = RunCommandLineCompiler(_csharpCompilerClientExecutable,
                                                "ref_mscorlib2.cs /nologo /nostdlib /noconfig /t:library /r:mscorlib20.dll",
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
            result = RunCommandLineCompiler(_csharpCompilerClientExecutable,
                                            "main.cs /nologo /nostdlib /noconfig /r:mscorlib40.dll /r:ref_mscorlib2.dll",
                                            _tempDirectory, files);

            Assert.Equal("", result.Output);
            Assert.Equal("", result.Errors);
            Assert.Equal(0, result.ExitCode);
        }

        private Dictionary<string, string> GetAnalyzerProjectFiles()
        {
            return new Dictionary<string, string>()
            {
                {
                    "MyAnalyzer.csproj",
                    @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""14.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <CscToolPath>" + _compilerDirectory + @"</CscToolPath>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProjectGuid>{6BD0BE3E-D565-42C2-A7DE-B7A2161BDBF8}</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>MyAnalyzer</RootNamespace>
    <AssemblyName>MyAnalyzer</AssemblyName>
    <TargetFrameworkVersion>v4.5</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
    <DebugType>pdbonly</DebugType>
    <Optimize>true</Optimize>
    <OutputPath>bin\Release\</OutputPath>
    <DefineConstants>TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""Microsoft.CodeAnalysis"">
      <HintPath>" + s_microsoftCodeAnalysisDllSrc + @"</HintPath>
    </Reference>
    <Reference Include=""System"" />
    <Reference Include=""System.Collections.Immutable"">
      <HintPath>" + s_systemCollectionsImmutableDllSrc + @"</HintPath>
    </Reference>
  </ItemGroup>
  <ItemGroup>
    <Compile Include=""MyAnalyzer.cs"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>"
                },
                {
                    "MyAnalyzer.cs",
                    @"using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
class MyAnalyzer : DiagnosticAnalyzer
{
    internal static readonly long loadTime = DateTime.Now.Ticks;
    internal static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor(""MyAnalyzer01"", string.Empty, ""Analyzer loaded at: {0}"", string.Empty, DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get { return ImmutableArray.Create(descriptor); }
    }

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
    }

    private void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        context.ReportDiagnostic(Diagnostic.Create(descriptor, context.Symbol.Locations.First(), loadTime));
    }
}"
                }
            };
        }

        [Fact, WorkItem(1119752)]
        [Trait(Traits.Environment, Traits.Environments.VSProductInstall)]
        public void AnalyzerChangesOnDisk()
        {
            var timeout = TimeSpan.FromMinutes(5);
            const string sourceText = @"using System;

class Hello
{
    static void Main()
    {
        Console.WriteLine(""Hello, world.""); 
    }
}";

            var directory = _tempDirectory.CreateDirectory("AnalyzerChangesOnDisk");

            // First, build the analyzer assembly
            string arguments = string.Format(@"/m /nr:false /t:Rebuild /p:UseRoslyn=1 MyAnalyzer.csproj");
            var resultTask = Task.Run(() => RunCommandLineCompiler(MSBuildExecutable, arguments, directory, GetAnalyzerProjectFiles()));
            WaitForProcess(resultTask, timeout, string.Format("Compiler server {0} timed out after {1} seconds", MSBuildExecutable, timeout.TotalSeconds), MSBuildExecutable);
            var result = resultTask.Result;

            Assert.True(directory.CreateFile("hello.cs").WriteAllTextAsync(sourceText).Wait(timeout),
                        string.Format("Took longer than {0} seconds to create files 'hello.cs' in {1}", timeout.TotalSeconds, directory.Path));
            var log = directory.CreateFile("Server.log");

            var environmentVars = new Dictionary<string, string>
            {
                { "RoslynCommandLineLogFile", log.Path }
            };

            // Run a build using the analyzer
            var firstBuildResultTask = Task.Run(() => RunCommandLineCompiler(_csharpCompilerClientExecutable, "/nologo hello.cs /a:bin\\Debug\\MyAnalyzer.dll", directory.Path, environmentVars));
            WaitForProcess(firstBuildResultTask, timeout, string.Format("Compiler server {0} timed out after {1} seconds", _csharpCompilerClientExecutable, timeout.TotalSeconds), _csharpCompilerClientExecutable);
            var firstBuildResult = firstBuildResultTask.Result;

            // Change the analyzer to cause it to be reloaded
            File.SetLastWriteTime(Path.Combine(directory.Path, "bin", "Debug", "MyAnalyzer.dll"), DateTime.Now);

            Assert.True(WaitForProcessExitAsync(_compilerServerExecutable).Wait(timeout),
                string.Format("Compiler server did not exit after {0} seconds, number of vbcscompiler.exe proccesses found: {1}",
                    timeout.Milliseconds / 1000,
                    GetProcessesByFullPath(_compilerServerExecutable).Count));

            // Run another build using the analyzer
            var secondBuildResultTask = Task.Run(() => RunCommandLineCompiler(_csharpCompilerClientExecutable, "/nologo hello.cs /a:bin\\Debug\\MyAnalyzer.dll", directory.Path, environmentVars));
            WaitForProcess(secondBuildResultTask, timeout, string.Format("Compiler server {0} timed out after {1} seconds", _csharpCompilerClientExecutable, timeout.TotalSeconds), _csharpCompilerClientExecutable);
            var secondBuildResult = secondBuildResultTask.Result;

            var firstBuildOutput = firstBuildResult.Output;
            var secondBuildOutput = secondBuildResult.Output;

            var assertMessage = string.Format("Output should be different, but is not.\r\nfirstBuildOutput:\r\n{0}\r\nsecondBuildOutput:\r\n{1}\r\n", firstBuildOutput, secondBuildOutput);
            // The output message of the analyzer includes a time stamp for when the analyzer was loaded. So if the analyzer was 
            // reloaded (which is what we want) then the output messages of the first and second builds will be different.
            Assert.False(firstBuildOutput.Equals(secondBuildOutput), assertMessage);
        }

        private void WaitForProcess(Task<ProcessResult> resultTask, TimeSpan timeout, string message, string processPath)
        {
            bool hasCompleted = resultTask.Wait(timeout);
            if (!hasCompleted)
            {
                Task.WhenAll(GetProcessesByFullPath(processPath).Select(x => DumpProcess(x.Id))).Wait();
                Assert.True(false, message + Environment.NewLine + "See dump for more info.");
            }
        }

        private Task<int> DumpProcess(int pid)
        {
            string pathToProcDump;
            if (!TryFindProcDumpPath(out pathToProcDump))
            {
                return Task.FromResult(0);
            }

            var source = new TaskCompletionSource<int>();
            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.CreateNoWindow = true;
            processStartInfo.FileName = pathToProcDump;
            processStartInfo.WorkingDirectory = Environment.CurrentDirectory;
            processStartInfo.UseShellExecute = false;

            processStartInfo.Arguments = " -accepteula -c 0 -ma " + pid.ToString();
            var process = new Process();
            process.StartInfo = processStartInfo;
            process.Exited += (s, e) => source.TrySetResult(process.ExitCode);
            process.ErrorDataReceived += (s, e) => source.TrySetException(new Exception("Unable to create dump: " + e.Data));
            process.Start();
            return source.Task;
        }

        private static bool TryFindProcDumpPath(out string path)
        {
            string executableDirectory = Path.GetDirectoryName(typeof(CompilerServerUnitTests).Assembly.Location);
            path = Path.Combine(executableDirectory, "ProcDump.exe");

            if (File.Exists(path))
            {
                return true;
            }

            path = Path.Combine(executableDirectory, @"..\ProcDump\ProcDump.exe");

            if (File.Exists(path))
            {
                return true;
            }

            return false;
        }

        [WorkItem(979588)]
        [Fact]
        public void Utf8OutputInRspFileCsc()
        {
            var srcFile = _tempDirectory.CreateFile("test.cs").WriteAllText("♕").Path;
            var tempOut = _tempDirectory.CreateFile("output.txt");
            var rspFile = _tempDirectory.CreateFile("temp.rsp").WriteAllText(
                string.Format("/utf8output /nologo /t:library {0}", srcFile));

            var result = ProcessLauncher.Run("cmd",
                string.Format(
                    "/C {0} /noconfig @{1} > {2}",
                    _csharpCompilerClientExecutable,
                    rspFile,
                    tempOut));

            Assert.Equal("", result.Output.Trim());
            Assert.Equal("src.cs(1,1): error CS1056: Unexpected character '♕'",
                tempOut.ReadAllText().Trim().Replace(srcFile, "src.cs"));
            Assert.Equal(1, result.ExitCode);
        }

        [WorkItem(979588)]
        [Fact]
        public void Utf8OutputInRspFileVbc()
        {
            var srcFile = _tempDirectory.CreateFile("test.cs").WriteAllText("♕").Path;
            var tempOut = _tempDirectory.CreateFile("output.txt");
            var rspFile = _tempDirectory.CreateFile("temp.rsp").WriteAllText(
                string.Format("/utf8output /nologo /t:library {0}", srcFile));

            var result = ProcessLauncher.Run("cmd",
                string.Format(
                    "/C {0} /noconfig @{1} > {2}",
                    _basicCompilerClientExecutable,
                    rspFile,
                    tempOut));

            Assert.Equal("", result.Output.Trim());
            Assert.Equal(@"src.vb(1) : error BC30037: Character is not valid.

♕
~", tempOut.ReadAllText().Trim().Replace(srcFile, "src.vb"));
            Assert.Equal(1, result.ExitCode);
        }

        [Fact(Skip = "DevDiv 1095079"), WorkItem(1095079)]
        public async Task ServerRespectsAppConfig()
        {
            var exeConfigPath = Path.Combine(_compilerDirectory, CompilerServerExeName + ".config");
            var doc = new XmlDocument();
            using (XmlReader reader = XmlReader.Create(exeConfigPath, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
            {
                doc.Load(reader);
            }
            var root = doc.DocumentElement;

            root.SelectSingleNode("appSettings/add/@value").Value = "1";
            doc.Save(exeConfigPath);

            var proc = ProcessLauncher.StartProcess(_compilerServerExecutable, "");
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
            var result = RunCommandLineCompiler(_csharpCompilerClientExecutable, "/keepalive", _tempDirectory.Path);

            Assert.True(result.ContainsErrors);
            Assert.Equal(1, result.ExitCode);
            Assert.Equal("Missing argument for '/keepalive' option.", result.Output.Trim());
            Assert.Equal("", result.Errors);
        }

        [Fact]
        public void BadKeepAlive2()
        {
            var result = RunCommandLineCompiler(_csharpCompilerClientExecutable, "/keepalive:foo", _tempDirectory.Path);

            Assert.True(result.ContainsErrors);
            Assert.Equal(1, result.ExitCode);
            Assert.Equal("Argument to '/keepalive' option is not a 32-bit integer.", result.Output.Trim());
            Assert.Equal("", result.Errors);
        }

        [Fact]
        public void BadKeepAlive3()
        {
            var result = RunCommandLineCompiler(_csharpCompilerClientExecutable, "/keepalive:-100", _tempDirectory.Path);

            Assert.True(result.ContainsErrors);
            Assert.Equal(1, result.ExitCode);
            Assert.Equal("Arguments to '/keepalive' option below -1 are invalid.", result.Output.Trim());
            Assert.Equal("", result.Errors);
        }

        [Fact]
        public void BadKeepAlive4()
        {
            var result = RunCommandLineCompiler(_csharpCompilerClientExecutable, "/keepalive:9999999999", _tempDirectory.Path);

            Assert.True(result.ContainsErrors);
            Assert.Equal(1, result.ExitCode);
            Assert.Equal("Argument to '/keepalive' option is not a 32-bit integer.", result.Output.Trim());
            Assert.Equal("", result.Errors);
        }

        [Fact]
        public void SimpleKeepAlive()
        {
            var result = RunCommandLineCompiler(_csharpCompilerClientExecutable,
                                                $"/nologo /keepalive:1 hello.cs",
                                                _tempDirectory,
                                                s_helloWorldSrcCs);
            VerifyResultAndOutput(result, _tempDirectory, "Hello, world.\r\n");
        }

        [Fact, WorkItem(1024619, "DevDiv")]
        public void Bug1024619_01()
        {
            var srcFile = _tempDirectory.CreateFile("test.cs").WriteAllText("").Path;

            _tempDirectory.CreateDirectory("Temp");
            var tmp = Path.Combine(_tempDirectory.Path, "Temp");

            var result = ProcessLauncher.Run("cmd",
                string.Format("/C \"SET TMP={2} && {0} /nologo /t:library {1}\"",
                _csharpCompilerClientExecutable,
                srcFile, tmp));

            Assert.Equal(0, result.ExitCode);

            Directory.Delete(tmp);

            result = ProcessLauncher.Run("cmd",
                string.Format("/C {0} /nologo /t:library {1}",
                _csharpCompilerClientExecutable,
                srcFile));

            Assert.Equal("", result.Output.Trim());
            Assert.Equal(0, result.ExitCode);
        }

        [Fact, WorkItem(1024619, "DevDiv")]
        public void Bug1024619_02()
        {
            var srcFile = _tempDirectory.CreateFile("test.vb").WriteAllText("").Path;

            _tempDirectory.CreateDirectory("Temp");
            var tmp = Path.Combine(_tempDirectory.Path, "Temp");

            var result = ProcessLauncher.Run("cmd",
                string.Format("/C \"SET TMP={2} && {0} /nologo /t:library {1}\"",
                _basicCompilerClientExecutable,
                srcFile, tmp));

            Assert.Equal(0, result.ExitCode);

            Directory.Delete(tmp);

            result = ProcessLauncher.Run("cmd",
                string.Format("/C {0} /nologo /t:library {1}",
                _basicCompilerClientExecutable,
                srcFile));

            Assert.Equal("", result.Output.Trim());
            Assert.Equal(0, result.ExitCode);
        }

        [Fact]
        public void ExecuteCscBuildTaskWithServer()
        {
            var csc = new Csc();
            csc.ToolPath = _compilerDirectory;
            var srcFile = _tempDirectory.CreateFile(s_helloWorldSrcCs[0].Key).WriteAllText(s_helloWorldSrcCs[0].Value).Path;
            var exeFile = Path.Combine(_tempDirectory.Path, "hello.exe");

            var engine = new MockEngine();
            csc.BuildEngine = engine;
            csc.Sources = new[] { new Build.Utilities.TaskItem(srcFile) };
            csc.NoLogo = true;
            csc.OutputAssembly = new Build.Utilities.TaskItem(exeFile);
            csc.ToolPath = "";
            csc.ToolExe = "";
            csc.UseSharedCompilation = true;

            csc.Execute();

            Assert.Equal(0, csc.ExitCode);
            Assert.Equal(string.Empty, engine.Warnings);
            Assert.Equal(string.Empty, engine.Errors);

            Assert.True(File.Exists(exeFile));

            var result = ProcessLauncher.Run(exeFile, "");
            Assert.Equal(0, result.ExitCode);
            Assert.Equal("Hello, world.", result.Output.Trim());
        }

        [Fact]
        public void ExecuteVbcBuildTaskWithServer()
        {
            var vbc = new Vbc();
            vbc.ToolPath = _compilerDirectory;
            var srcFile = _tempDirectory.CreateFile(s_helloWorldSrcVb[0].Key).WriteAllText(s_helloWorldSrcVb[0].Value).Path;
            var exeFile = Path.Combine(_tempDirectory.Path, "hello.exe");

            var engine = new MockEngine();
            vbc.BuildEngine = engine;
            vbc.Sources = new[] { new Build.Utilities.TaskItem(srcFile) };
            vbc.NoLogo = true;
            vbc.OutputAssembly = new Build.Utilities.TaskItem(exeFile);
            vbc.ToolPath = "";
            vbc.ToolExe = "";
            vbc.UseSharedCompilation = true;

            vbc.Execute();

            Assert.Equal(0, vbc.ExitCode);
            Assert.Equal(string.Empty, engine.Warnings);
            Assert.Equal(string.Empty, engine.Errors);

            Assert.True(File.Exists(exeFile));

            var result = ProcessLauncher.Run(exeFile, "");
            Assert.Equal(0, result.ExitCode);
            Assert.Equal("Hello from VB", result.Output.Trim());
        }
    }
}
