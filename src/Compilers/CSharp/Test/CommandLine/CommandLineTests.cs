// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DiaSymReader;
using Roslyn.Test.PdbUtilities;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Roslyn.Utilities;
using TestResources.Analyzers;
using Xunit;
using static Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers;
using static Roslyn.Test.Utilities.SharedResourceHelpers;

namespace Microsoft.CodeAnalysis.CSharp.CommandLine.UnitTests
{
    public class CommandLineTests : CommandLineTestBase
    {
#if NET
        private static readonly string s_CSharpCompilerExecutable;
        private static readonly string s_DotnetCscRun;
#else
        private static readonly string s_CSharpCompilerExecutable = Path.Combine(
            Path.GetDirectoryName(typeof(CommandLineTests).GetTypeInfo().Assembly.Location),
            Path.Combine("dependency", "csc.exe"));
        private static readonly string s_DotnetCscRun = ExecutionConditionUtil.IsMonoDesktop ? "mono" : string.Empty;
#endif
        private static readonly string s_CSharpScriptExecutable;

        private static readonly string s_compilerVersion = CommonCompiler.GetProductVersion(typeof(CommandLineTests));

        static CommandLineTests()
        {
#if NET
            var cscDllPath = Path.Combine(
                Path.GetDirectoryName(typeof(CommandLineTests).GetTypeInfo().Assembly.Location),
                Path.Combine("dependency", "csc.dll"));
            var dotnetExe = DotNetCoreSdk.ExePath;
            var netStandardDllPath = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => !assembly.IsDynamic && assembly.Location.EndsWith("netstandard.dll")).Location;
            var netStandardDllDir = Path.GetDirectoryName(netStandardDllPath);
            // Since we are using references based on the UnitTest's runtime, we need to use
            // its runtime config when executing out program.
            var runtimeConfigPath = Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, "runtimeconfig.json");

            s_CSharpCompilerExecutable = $@"""{dotnetExe}"" ""{cscDllPath}"" /r:""{netStandardDllPath}"" /r:""{netStandardDllDir}/System.Private.CoreLib.dll"" /r:""{netStandardDllDir}/System.Console.dll"" /r:""{netStandardDllDir}/System.Runtime.dll""";
            s_DotnetCscRun = $@"""{dotnetExe}"" exec --runtimeconfig ""{runtimeConfigPath}""";
            s_CSharpScriptExecutable = s_CSharpCompilerExecutable.Replace("csc.dll", Path.Combine("csi", "csi.dll"));
#else
            s_CSharpScriptExecutable = s_CSharpCompilerExecutable.Replace("csc.exe", Path.Combine("csi", "csi.exe"));
#endif
        }

        private class TestCommandLineParser : CSharpCommandLineParser
        {
            private readonly Dictionary<string, string> _responseFiles;
            private readonly Dictionary<string, string[]> _recursivePatterns;
            private readonly Dictionary<string, string[]> _patterns;

            public TestCommandLineParser(
                Dictionary<string, string> responseFiles = null,
                Dictionary<string, string[]> patterns = null,
                Dictionary<string, string[]> recursivePatterns = null,
                bool isInteractive = false)
                : base(isInteractive)
            {
                _responseFiles = responseFiles;
                _recursivePatterns = recursivePatterns;
                _patterns = patterns;
            }

            internal override IEnumerable<string> EnumerateFiles(string directory,
                                                                 string fileNamePattern,
                                                                 SearchOption searchOption)
            {
                var key = directory + "|" + fileNamePattern;
                if (searchOption == SearchOption.TopDirectoryOnly)
                {
                    return _patterns[key];
                }
                else
                {
                    return _recursivePatterns[key];
                }
            }

            internal override TextReader CreateTextFileReader(string fullPath)
            {
                return new StringReader(_responseFiles[fullPath]);
            }
        }

        private CSharpCommandLineArguments ScriptParse(IEnumerable<string> args, string baseDirectory)
        {
            return CSharpCommandLineParser.Script.Parse(args, baseDirectory, SdkDirectory);
        }

        private CSharpCommandLineArguments FullParse(string commandLine, string baseDirectory, string sdkDirectory = null, string additionalReferenceDirectories = null)
        {
            sdkDirectory = sdkDirectory ?? SdkDirectory;
            var args = CommandLineParser.SplitCommandLineIntoArguments(commandLine, removeHashComments: true);
            return CSharpCommandLineParser.Default.Parse(args, baseDirectory, sdkDirectory, additionalReferenceDirectories);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly))]
        [WorkItem(34101, "https://github.com/dotnet/roslyn/issues/34101")]
        public void SuppressedWarnAsErrorsStillEmit()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"
#pragma warning disable 1591

public class P {
    public static void Main() {}
}");
            const string docName = "doc.xml";

            var cmd = CreateCSharpCompiler(null, dir.Path, new[] { "/nologo", "/errorlog:errorlog", $"/doc:{docName}", "/warnaserror", src.Path });

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = cmd.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString());

            string exePath = Path.Combine(dir.Path, "temp.exe");
            Assert.True(File.Exists(exePath));
            var result = ProcessUtilities.Run(exePath, arguments: "");
            Assert.Equal(0, result.ExitCode);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsWindowsTypes)]
        public void XmlMemoryMapped()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText("class C {}");
            const string docName = "doc.xml";

            var cmd = CreateCSharpCompiler(null, dir.Path, new[] { "/nologo", "/t:library", "/preferreduilang:en", $"/doc:{docName}", src.Path });

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = cmd.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString());

            var xmlPath = Path.Combine(dir.Path, docName);
            using (var fileStream = new FileStream(xmlPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var mmf = MemoryMappedFile.CreateFromFile(fileStream, "xmlMap", 0, MemoryMappedFileAccess.Read, HandleInheritability.None, leaveOpen: true))
            {
                exitCode = cmd.Run(outWriter);
                Assert.StartsWith($"error CS0016: Could not write to output file '{xmlPath}' -- ", outWriter.ToString());
                Assert.Equal(1, exitCode);
            }
        }

        [Fact]
        public void SimpleAnalyzerConfig()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("test.cs").WriteAllText(@"
class C
{
    int _f;
}");
            var analyzerConfig = dir.CreateFile(".editorconfig").WriteAllText(@"
[*.cs]
dotnet_diagnostic.cs0169.severity = none");
            var cmd = CreateCSharpCompiler(null, dir.Path, new[] {
                "/nologo",
                "/t:library",
                "/preferreduilang:en",
                "/analyzerconfig:" + analyzerConfig.Path,
                src.Path });

            Assert.Equal(analyzerConfig.Path, Assert.Single(cmd.Arguments.AnalyzerConfigPaths));

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = cmd.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString());

            Assert.Null(cmd.AnalyzerOptions);
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/72657")]
        public void AnalyzerConfig_DoubleSlash(bool doubleSlashAnalyzerConfig, bool doubleSlashSource)
        {
            var dir = Temp.CreateDirectory();
            var analyzer = new CompilationAnalyzerWithSeverity(DiagnosticSeverity.Warning, configurable: true);
            var src = dir.CreateFile("Class1.cs").WriteAllText("""
                public ﻿class C
                {
                    public void M() { }
                }
                """);

            // The analyzer should produce a warning.
            var output = VerifyOutput(dir, src, includeCurrentAssemblyAsAnalyzerReference: false, analyzers: [analyzer], expectedWarningCount: 1);
            AssertEx.Equal("Class1.cs(1,1): warning ID1000:", output.Trim());

            // But not when this editorconfig is applied.
            var editorconfig = dir.CreateFile(".editorconfig").WriteAllText("""
                root = true

                [*.cs]
                dotnet_analyzer_diagnostic.severity = none

                generated_code = true
                """);
            var cmd = CreateCSharpCompiler(
                [
                    "/nologo",
                    "/preferreduilang:en",
                    "/t:library",
                    "/analyzerconfig:" + modifyPath(editorconfig.Path, doubleSlashAnalyzerConfig),
                    modifyPath(src.Path, doubleSlashSource),
                ],
                [analyzer]);
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = cmd.Run(outWriter);
            Assert.Equal(0, exitCode);
            AssertEx.Equal("", outWriter.ToString());

            static string modifyPath(string path, bool doubleSlash)
            {
                if (!doubleSlash)
                {
                    return path;
                }

                // Find the second-to-last slash.
                char[] separators = ['/', '\\'];
                var lastSlashIndex = path.LastIndexOfAny(separators);
                lastSlashIndex = path.LastIndexOfAny(separators, lastSlashIndex - 1);

                // Duplicate that slash.
                var lastSlash = path[lastSlashIndex];
                return path[0..lastSlashIndex] + lastSlash + path[lastSlashIndex..];
            }
        }

        [Fact]
        public void AnalyzerConfigWithOptions()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("test.cs").WriteAllText(@"
class C
{
    int _f;
}");
            var additionalFile = dir.CreateFile("file.txt");
            var analyzerConfig = dir.CreateFile(".editorconfig").WriteAllText(@"
[*.cs]
dotnet_diagnostic.cs0169.severity = none
dotnet_diagnostic.Warning01.severity = none
my_option = my_val

[*.txt]
dotnet_diagnostic.cs0169.severity = none
my_option2 = my_val2");
            var cmd = CreateCSharpCompiler(null, dir.Path, new[] {
                "/nologo",
                "/t:library",
                "/analyzerconfig:" + analyzerConfig.Path,
                "/analyzer:" + Assembly.GetExecutingAssembly().Location,
                "/nowarn:8032",
                "/additionalfile:" + additionalFile.Path,
                src.Path });

            Assert.Equal(analyzerConfig.Path, Assert.Single(cmd.Arguments.AnalyzerConfigPaths));

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = cmd.Run(outWriter);
            Assert.Equal("", outWriter.ToString());
            Assert.Equal(0, exitCode);

            var comp = cmd.Compilation;
            var tree = comp.SyntaxTrees.Single();
            var compilerTreeOptions = comp.Options.SyntaxTreeOptionsProvider;
            Assert.True(compilerTreeOptions.TryGetDiagnosticValue(tree, "cs0169", CancellationToken.None, out var severity));
            Assert.Equal(ReportDiagnostic.Suppress, severity);
            Assert.True(compilerTreeOptions.TryGetDiagnosticValue(tree, "warning01", CancellationToken.None, out severity));
            Assert.Equal(ReportDiagnostic.Suppress, severity);

            var analyzerOptions = cmd.AnalyzerOptions.AnalyzerConfigOptionsProvider;
            var options = analyzerOptions.GetOptions(tree);
            Assert.NotNull(options);
            Assert.True(options.TryGetValue("my_option", out string val));
            Assert.Equal("my_val", val);
            Assert.False(options.TryGetValue("my_option2", out _));
            Assert.False(options.TryGetValue("dotnet_diagnostic.cs0169.severity", out _));

            options = analyzerOptions.GetOptions(cmd.AnalyzerOptions.AdditionalFiles.Single());
            Assert.NotNull(options);
            Assert.True(options.TryGetValue("my_option2", out val));
            Assert.Equal("my_val2", val);
            Assert.False(options.TryGetValue("my_option", out _));
            Assert.False(options.TryGetValue("dotnet_diagnostic.cs0169.severity", out _));
        }

        [Fact]
        public void AnalyzerConfigBadSeverity()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("test.cs").WriteAllText(@"
class C
{
    int _f;
}");
            var analyzerConfig = dir.CreateFile(".editorconfig").WriteAllText(@"
[*.cs]
dotnet_diagnostic.cs0169.severity = garbage");
            var cmd = CreateCSharpCompiler(null, dir.Path, new[] {
                "/nologo",
                "/t:library",
                "/preferreduilang:en",
                "/analyzerconfig:" + analyzerConfig.Path,
                src.Path });

            Assert.Equal(analyzerConfig.Path, Assert.Single(cmd.Arguments.AnalyzerConfigPaths));

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = cmd.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal(
$@"warning InvalidSeverityInAnalyzerConfig: The diagnostic 'cs0169' was given an invalid severity 'garbage' in the analyzer config file at '{analyzerConfig.Path}'.
test.cs(4,9): warning CS0169: The field 'C._f' is never used
", outWriter.ToString());

            Assert.Null(cmd.AnalyzerOptions);
        }

        [Fact]
        public void AnalyzerConfigsInSameDir()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("test.cs").WriteAllText(@"
class C
{
    int _f;
}");
            var configText = @"
[*.cs]
dotnet_diagnostic.cs0169.severity = suppress";

            var analyzerConfig1 = dir.CreateFile("analyzerconfig1").WriteAllText(configText);
            var analyzerConfig2 = dir.CreateFile("analyzerconfig2").WriteAllText(configText);

            var cmd = CreateCSharpCompiler(null, dir.Path, new[] {
                "/nologo",
                "/t:library",
                "/preferreduilang:en",
                "/analyzerconfig:" + analyzerConfig1.Path,
                "/analyzerconfig:" + analyzerConfig2.Path,
                src.Path
            });

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = cmd.Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Equal(
                $"error CS8700: Multiple analyzer config files cannot be in the same directory ('{dir.Path}').",
                outWriter.ToString().TrimEnd());
        }

        // This test should only run when the machine's default encoding is shift-JIS
        [ConditionalFact(typeof(WindowsDesktopOnly), typeof(HasShiftJisDefaultEncoding), Reason = "https://github.com/dotnet/roslyn/issues/30321")]
        public void CompileShiftJisOnShiftJis()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("sjis.cs").WriteAllBytes(TestResources.General.ShiftJisSource);

            var cmd = CreateCSharpCompiler(null, dir.Path, new[] { "/nologo", src.Path });

            Assert.Null(cmd.Arguments.Encoding);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = cmd.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString());

            var result = ProcessUtilities.Run(Path.Combine(dir.Path, "sjis.exe"), arguments: "", workingDirectory: dir.Path);
            Assert.Equal(0, result.ExitCode);
            Assert.Equal("星野 八郎太", File.ReadAllText(Path.Combine(dir.Path, "output.txt"), Encoding.GetEncoding(932)));
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/30321")]
        public void RunWithShiftJisFile()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("sjis.cs").WriteAllBytes(TestResources.General.ShiftJisSource);

            var cmd = CreateCSharpCompiler(null, dir.Path, new[] { "/nologo", "/codepage:932", src.Path });

            Assert.Equal(932, cmd.Arguments.Encoding?.WindowsCodePage);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = cmd.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString());

            var result = ProcessUtilities.Run(Path.Combine(dir.Path, "sjis.exe"), arguments: "", workingDirectory: dir.Path);
            Assert.Equal(0, result.ExitCode);
            Assert.Equal("星野 八郎太", File.ReadAllText(Path.Combine(dir.Path, "output.txt"), Encoding.GetEncoding(932)));
        }

        [WorkItem(946954, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/946954")]
        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/30321")]
        public void CompilerBinariesAreAnyCPU()
        {
#pragma warning disable SYSLIB0037
            // warning SYSLIB0037: 'AssemblyName.ProcessorArchitecture' is obsolete: 'AssemblyName members HashAlgorithm, ProcessorArchitecture, and VersionCompatibility are obsolete and not supported.'
            Assert.Equal(ProcessorArchitecture.MSIL, AssemblyName.GetAssemblyName(s_CSharpCompilerExecutable).ProcessorArchitecture);
#pragma warning restore SYSLIB0037
        }

        [Fact]
        public void ResponseFiles1()
        {
            string rsp = Temp.CreateFile().WriteAllText(@"
/r:System.dll
/nostdlib
# this is ignored
System.Console.WriteLine(""*?"");  # this is error
a.cs
").Path;
            var cmd = CreateCSharpCompiler(rsp, WorkingDirectory, new[] { "b.cs" });

            cmd.Arguments.Errors.Verify(
                // error CS2001: Source file 'System.Console.WriteLine(*?);' could not be found
                Diagnostic(ErrorCode.ERR_FileNotFound).WithArguments("System.Console.WriteLine(*?);"));

            AssertEx.Equal(new[] { "System.dll" }, cmd.Arguments.MetadataReferences.Select(r => r.Reference));
            AssertEx.Equal(new[] { Path.Combine(WorkingDirectory, "a.cs"), Path.Combine(WorkingDirectory, "b.cs") }, cmd.Arguments.SourceFiles.Select(file => file.Path));

            CleanupAllGeneratedFiles(rsp);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.TestExecutionNeedsWindowsTypes)]
        public void ResponseFiles_RelativePaths()
        {
            var parentDir = Temp.CreateDirectory();
            var baseDir = parentDir.CreateDirectory("temp");
            var dirX = baseDir.CreateDirectory("x");
            var dirAB = baseDir.CreateDirectory("a b");
            var dirSubDir = baseDir.CreateDirectory("subdir");
            var dirGoo = parentDir.CreateDirectory("goo");
            var dirBar = parentDir.CreateDirectory("bar");

            string basePath = baseDir.Path;
            Func<string, string> prependBasePath = fileName => Path.Combine(basePath, fileName);

            var parser = new TestCommandLineParser(responseFiles: new Dictionary<string, string>()
            {
                { prependBasePath(@"a.rsp"), @"
""@subdir\b.rsp""
/r:..\v4.0.30319\System.dll
/r:.\System.Data.dll
a.cs @""..\c.rsp"" @\d.rsp
/libpaths:..\goo;../bar;""a b""
"
                },
                { Path.Combine(dirSubDir.Path, @"b.rsp"), @"
b.cs
"
                },
                { prependBasePath(@"..\c.rsp"), @"
c.cs /lib:x
"
                },
                {  Path.Combine(Path.GetPathRoot(basePath), @"d.rsp"), @"

# comment
d.cs
"
                }
            }, isInteractive: false);

            var args = parser.Parse(new[] { "first.cs", "second.cs", "@a.rsp", "last.cs" }, basePath, SdkDirectory);
            args.Errors.Verify();
            Assert.False(args.IsScriptRunner);

            string[] resolvedSourceFiles = args.SourceFiles.Select(f => f.Path).ToArray();
            string[] references = args.MetadataReferences.Select(r => r.Reference).ToArray();

            AssertEx.Equal(new[] { "first.cs", "second.cs", "b.cs", "a.cs", "c.cs", "d.cs", "last.cs" }.Select(prependBasePath), resolvedSourceFiles);
            AssertEx.Equal(new[] { typeof(object).Assembly.Location, @"..\v4.0.30319\System.dll", @".\System.Data.dll" }, references);
            AssertEx.Equal(new[] { RuntimeEnvironment.GetRuntimeDirectory() }.Concat(new[] { @"x", @"..\goo", @"../bar", @"a b" }.Select(prependBasePath)), args.ReferencePaths.ToArray());
            Assert.Equal(basePath, args.BaseDirectory);
        }

#nullable enable
        [ConditionalFact(typeof(WindowsOnly))]
        public void NullBaseDirectoryNotAddedToKeyFileSearchPaths()
        {
            var parser = CSharpCommandLineParser.Default.Parse(new[] { "c:/test.cs" }, baseDirectory: null, SdkDirectory);
            AssertEx.Equal(ImmutableArray.Create<string>(), parser.KeyFileSearchPaths);
            Assert.Null(parser.OutputDirectory);
            parser.Errors.Verify(
                // error CS8762: Output directory could not be determined
                Diagnostic(ErrorCode.ERR_NoOutputDirectory).WithLocation(1, 1)
                );
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void NullBaseDirectoryWithAdditionalFiles()
        {
            var parser = CSharpCommandLineParser.Default.Parse(new[] { "/additionalfile:web.config", "c:/test.cs" }, baseDirectory: null, SdkDirectory);
            AssertEx.Equal(ImmutableArray.Create<string>(), parser.KeyFileSearchPaths);
            Assert.Null(parser.OutputDirectory);
            parser.Errors.Verify(
                // error CS2021: File name 'web.config' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments("web.config").WithLocation(1, 1),
                // error CS8762: Output directory could not be determined
                Diagnostic(ErrorCode.ERR_NoOutputDirectory).WithLocation(1, 1)
                );
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void NullBaseDirectoryWithAdditionalFiles_Wildcard()
        {
            var parser = CSharpCommandLineParser.Default.Parse(new[] { "/additionalfile:*", "c:/test.cs" }, baseDirectory: null, SdkDirectory);
            AssertEx.Equal(ImmutableArray.Create<string>(), parser.KeyFileSearchPaths);
            Assert.Null(parser.OutputDirectory);
            parser.Errors.Verify(
                // error CS2001: Source file '*' could not be found.
                Diagnostic(ErrorCode.ERR_FileNotFound).WithArguments("*").WithLocation(1, 1),
                // error CS8762: Output directory could not be determined
                Diagnostic(ErrorCode.ERR_NoOutputDirectory).WithLocation(1, 1)
                );
        }
#nullable disable

        [Fact, WorkItem(29252, "https://github.com/dotnet/roslyn/issues/29252")]
        public void NoSdkPath()
        {
            var parentDir = Temp.CreateDirectory();
            var parser = CSharpCommandLineParser.Default.Parse(new[] { "file.cs", $"-out:{parentDir.Path}", "/noSdkPath" }, parentDir.Path, null);
            AssertEx.Equal(ImmutableArray<string>.Empty, parser.ReferencePaths);
        }

        [Fact, WorkItem(29252, "https://github.com/dotnet/roslyn/issues/29252")]
        public void NoSdkPathReferenceSystemDll()
        {
            string source = @"
class C
{
}
";
            var dir = Temp.CreateDirectory();

            var file = dir.CreateFile("a.cs");
            file.WriteAllText(source);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(null, dir.Path, new[] { "/nologo", "/preferreduilang:en", "/nosdkpath", "/r:System.dll", "a.cs" });
            var exitCode = csc.Run(outWriter);

            Assert.Equal(1, exitCode);
            Assert.Equal("error CS0006: Metadata file 'System.dll' could not be found", outWriter.ToString().Trim());
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void SourceFiles_Patterns()
        {
            var parser = new TestCommandLineParser(
                patterns: new Dictionary<string, string[]>()
                {
                    { @"C:\temp|*.cs", new[] { "a.cs", "b.cs", "c.cs" } }
                },
                recursivePatterns: new Dictionary<string, string[]>()
                {
                    { @"C:\temp\a|*.cs", new[] { @"a\x.cs", @"a\b\b.cs", @"a\c.cs" } },
                });

            var args = parser.Parse(new[] { @"*.cs", @"/recurse:a\*.cs" }, @"C:\temp", SdkDirectory);
            args.Errors.Verify();

            string[] resolvedSourceFiles = args.SourceFiles.Select(f => f.Path).ToArray();

            AssertEx.Equal(new[] { @"C:\temp\a.cs", @"C:\temp\b.cs", @"C:\temp\c.cs", @"C:\temp\a\x.cs", @"C:\temp\a\b\b.cs", @"C:\temp\a\c.cs" }, resolvedSourceFiles);
        }

        [Fact]
        public void ParseQuotedMainType()
        {
            // Verify the main switch are unquoted when used because of the issue with
            // MSBuild quoting some usages and not others. A quote character is not valid in either
            // these names.

            CSharpCommandLineArguments args;
            var folder = Temp.CreateDirectory();
            CreateFile(folder, "a.cs");

            args = DefaultParse(new[] { "/main:Test", "a.cs" }, folder.Path);
            args.Errors.Verify();
            Assert.Equal("Test", args.CompilationOptions.MainTypeName);

            args = DefaultParse(new[] { "/main:\"Test\"", "a.cs" }, folder.Path);
            args.Errors.Verify();
            Assert.Equal("Test", args.CompilationOptions.MainTypeName);

            args = DefaultParse(new[] { "/main:\"Test.Class1\"", "a.cs" }, folder.Path);
            args.Errors.Verify();
            Assert.Equal("Test.Class1", args.CompilationOptions.MainTypeName);

            args = DefaultParse(new[] { "/m:Test", "a.cs" }, folder.Path);
            args.Errors.Verify();
            Assert.Equal("Test", args.CompilationOptions.MainTypeName);

            args = DefaultParse(new[] { "/m:\"Test\"", "a.cs" }, folder.Path);
            args.Errors.Verify();
            Assert.Equal("Test", args.CompilationOptions.MainTypeName);

            args = DefaultParse(new[] { "/m:\"Test.Class1\"", "a.cs" }, folder.Path);
            args.Errors.Verify();
            Assert.Equal("Test.Class1", args.CompilationOptions.MainTypeName);

            // Use of Cyrillic namespace
            args = DefaultParse(new[] { "/m:\"решения.Class1\"", "a.cs" }, folder.Path);
            args.Errors.Verify();
            Assert.Equal("решения.Class1", args.CompilationOptions.MainTypeName);
        }

        [Fact]
        [WorkItem(21508, "https://github.com/dotnet/roslyn/issues/21508")]
        public void ArgumentStartWithDashAndContainingSlash()
        {
            CSharpCommandLineArguments args;
            var folder = Temp.CreateDirectory();

            args = DefaultParse(new[] { "-debug+/debug:portable" }, folder.Path);
            args.Errors.Verify(
                // error CS2007: Unrecognized option: '-debug+/debug:portable'
                Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("-debug+/debug:portable").WithLocation(1, 1),
                // warning CS2008: No source files specified.
                Diagnostic(ErrorCode.WRN_NoSources).WithLocation(1, 1),
                // error CS1562: Outputs without source must have the /out option specified
                Diagnostic(ErrorCode.ERR_OutputNeedsName).WithLocation(1, 1)
                );
        }

        [WorkItem(546009, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546009")]
        [WorkItem(545991, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545991")]
        [ConditionalFact(typeof(WindowsOnly))]
        public void SourceFiles_Patterns2()
        {
            var folder = Temp.CreateDirectory();
            CreateFile(folder, "a.cs");
            CreateFile(folder, "b.vb");
            CreateFile(folder, "c.cpp");

            var folderA = folder.CreateDirectory("A");
            CreateFile(folderA, "A_a.cs");
            CreateFile(folderA, "A_b.cs");
            CreateFile(folderA, "A_c.vb");

            var folderB = folder.CreateDirectory("B");
            CreateFile(folderB, "B_a.cs");
            CreateFile(folderB, "B_b.vb");
            CreateFile(folderB, "B_c.cpx");

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            int exitCode = CreateCSharpCompiler(null, folder.Path, new[] { "/nologo", "/preferreduilang:en", "/t:library", @"/recurse:.", "/out:abc.dll" }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("warning CS2008: No source files specified.", outWriter.ToString().Trim());

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = CreateCSharpCompiler(null, folder.Path, new[] { "/nologo", "/preferreduilang:en", "/t:library", @"/recurse:.  ", "/out:abc.dll" }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("warning CS2008: No source files specified.", outWriter.ToString().Trim());

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = CreateCSharpCompiler(null, folder.Path, new[] { "/nologo", "/preferreduilang:en", "/t:library", @"/recurse:  .  ", "/out:abc.dll" }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("warning CS2008: No source files specified.", outWriter.ToString().Trim());

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = CreateCSharpCompiler(null, folder.Path, new[] { "/nologo", "/preferreduilang:en", "/t:library", @"/recurse:././.", "/out:abc.dll" }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("warning CS2008: No source files specified.", outWriter.ToString().Trim());

            CSharpCommandLineArguments args;
            string[] resolvedSourceFiles;

            args = DefaultParse(new[] { @"/recurse:*.cp*", @"/recurse:a\*.c*", @"/out:a.dll" }, folder.Path);
            args.Errors.Verify();
            resolvedSourceFiles = args.SourceFiles.Select(f => f.Path).ToArray();
            AssertEx.Equal(new[] { folder.Path + @"\c.cpp", folder.Path + @"\B\B_c.cpx", folder.Path + @"\a\A_a.cs", folder.Path + @"\a\A_b.cs", }, resolvedSourceFiles);

            args = DefaultParse(new[] { @"/recurse:.\\\\\\*.cs", @"/out:a.dll" }, folder.Path);
            args.Errors.Verify();
            resolvedSourceFiles = args.SourceFiles.Select(f => f.Path).ToArray();
            Assert.Equal(4, resolvedSourceFiles.Length);

            args = DefaultParse(new[] { @"/recurse:.////*.cs", @"/out:a.dll" }, folder.Path);
            args.Errors.Verify();
            resolvedSourceFiles = args.SourceFiles.Select(f => f.Path).ToArray();
            Assert.Equal(4, resolvedSourceFiles.Length);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void SourceFile_BadPath()
        {
            var args = DefaultParse(new[] { @"e:c:\test\test.cs", "/t:library" }, WorkingDirectory);
            Assert.Equal(3, args.Errors.Length);
            Assert.Equal((int)ErrorCode.FTL_InvalidInputFileName, args.Errors[0].Code);
            Assert.Equal((int)ErrorCode.WRN_NoSources, args.Errors[1].Code);
            Assert.Equal((int)ErrorCode.ERR_OutputNeedsName, args.Errors[2].Code);
        }

        private void CreateFile(TempDirectory folder, string file)
        {
            var f = folder.CreateFile(file);
            f.WriteAllText("");
        }

        [Fact, WorkItem(546023, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546023")]
        public void Win32ResourceArguments()
        {
            string[] args = new string[]
            {
                @"/win32manifest:..\here\there\everywhere\nonexistent"
            };

            var parsedArgs = DefaultParse(args, WorkingDirectory);
            var compilation = CreateCompilation(new SyntaxTree[0]);
            IEnumerable<DiagnosticInfo> errors;
            CSharpCompiler.GetWin32ResourcesInternal(StandardFileSystem.Instance, MessageProvider.Instance, parsedArgs, compilation, out errors);
            Assert.Equal(1, errors.Count());
            Assert.Equal((int)ErrorCode.ERR_CantOpenWin32Manifest, errors.First().Code);
            Assert.Equal(2, errors.First().Arguments.Count());

            args = new string[]
            {
                @"/Win32icon:\bogus"
            };

            parsedArgs = DefaultParse(args, WorkingDirectory);

            CSharpCompiler.GetWin32ResourcesInternal(StandardFileSystem.Instance, MessageProvider.Instance, parsedArgs, compilation, out errors);
            Assert.Equal(1, errors.Count());
            Assert.Equal((int)ErrorCode.ERR_CantOpenIcon, errors.First().Code);
            Assert.Equal(2, errors.First().Arguments.Count());

            args = new string[]
            {
                @"/Win32Res:\bogus"
            };

            parsedArgs = DefaultParse(args, WorkingDirectory);
            CSharpCompiler.GetWin32ResourcesInternal(StandardFileSystem.Instance, MessageProvider.Instance, parsedArgs, compilation, out errors);
            Assert.Equal(1, errors.Count());
            Assert.Equal((int)ErrorCode.ERR_CantOpenWin32Res, errors.First().Code);
            Assert.Equal(2, errors.First().Arguments.Count());

            args = new string[]
            {
                @"/Win32Res:goo.win32data:bar.win32data2"
            };

            parsedArgs = DefaultParse(args, WorkingDirectory);
            CSharpCompiler.GetWin32ResourcesInternal(StandardFileSystem.Instance, MessageProvider.Instance, parsedArgs, compilation, out errors);
            Assert.Equal(1, errors.Count());
            Assert.Equal((int)ErrorCode.ERR_CantOpenWin32Res, errors.First().Code);
            Assert.Equal(2, errors.First().Arguments.Count());

            args = new string[]
            {
                @"/Win32icon:goo.win32data:bar.win32data2"
            };

            parsedArgs = DefaultParse(args, WorkingDirectory);
            CSharpCompiler.GetWin32ResourcesInternal(StandardFileSystem.Instance, MessageProvider.Instance, parsedArgs, compilation, out errors);
            Assert.Equal(1, errors.Count());
            Assert.Equal((int)ErrorCode.ERR_CantOpenIcon, errors.First().Code);
            Assert.Equal(2, errors.First().Arguments.Count());

            args = new string[]
            {
                @"/Win32manifest:goo.win32data:bar.win32data2"
            };

            parsedArgs = DefaultParse(args, WorkingDirectory);
            CSharpCompiler.GetWin32ResourcesInternal(StandardFileSystem.Instance, MessageProvider.Instance, parsedArgs, compilation, out errors);
            Assert.Equal(1, errors.Count());
            Assert.Equal((int)ErrorCode.ERR_CantOpenWin32Manifest, errors.First().Code);
            Assert.Equal(2, errors.First().Arguments.Count());
        }

        [Fact]
        public void Win32ResConflicts()
        {
            var parsedArgs = DefaultParse(new[] { "/win32res:goo", "/win32icon:goob", "a.cs" }, WorkingDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.ERR_CantHaveWin32ResAndIcon, parsedArgs.Errors.First().Code);

            parsedArgs = DefaultParse(new[] { "/win32res:goo", "/win32manifest:goob", "a.cs" }, WorkingDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.ERR_CantHaveWin32ResAndManifest, parsedArgs.Errors.First().Code);

            parsedArgs = DefaultParse(new[] { "/win32res:", "a.cs" }, WorkingDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.ERR_NoFileSpec, parsedArgs.Errors.First().Code);
            Assert.Equal(1, parsedArgs.Errors.First().Arguments.Count);

            parsedArgs = DefaultParse(new[] { "/win32Icon: ", "a.cs" }, WorkingDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.ERR_NoFileSpec, parsedArgs.Errors.First().Code);
            Assert.Equal(1, parsedArgs.Errors.First().Arguments.Count);

            parsedArgs = DefaultParse(new[] { "/win32Manifest:", "a.cs" }, WorkingDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.ERR_NoFileSpec, parsedArgs.Errors.First().Code);
            Assert.Equal(1, parsedArgs.Errors.First().Arguments.Count);

            parsedArgs = DefaultParse(new[] { "/win32Manifest:goo", "/noWin32Manifest", "a.cs" }, WorkingDirectory);
            Assert.Equal(0, parsedArgs.Errors.Length);
            Assert.True(parsedArgs.NoWin32Manifest);
            Assert.Null(parsedArgs.Win32Manifest);
        }

        [Fact]
        public void Win32ResInvalid()
        {
            var parsedArgs = DefaultParse(new[] { "/win32res", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("/win32res"));

            parsedArgs = DefaultParse(new[] { "/win32res+", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/win32res+"));

            parsedArgs = DefaultParse(new[] { "/win32icon", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("/win32icon"));

            parsedArgs = DefaultParse(new[] { "/win32icon+", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/win32icon+"));

            parsedArgs = DefaultParse(new[] { "/win32manifest", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("/win32manifest"));

            parsedArgs = DefaultParse(new[] { "/win32manifest+", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/win32manifest+"));
        }

        [Fact]
        public void Win32IconContainsGarbage()
        {
            string tmpFileName = Temp.CreateFile().WriteAllBytes(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }).Path;

            var parsedArgs = DefaultParse(new[] { "/win32icon:" + tmpFileName, "a.cs" }, WorkingDirectory);
            var compilation = CreateCompilation(new SyntaxTree[0]);
            IEnumerable<DiagnosticInfo> errors;

            CSharpCompiler.GetWin32ResourcesInternal(StandardFileSystem.Instance, MessageProvider.Instance, parsedArgs, compilation, out errors);
            Assert.Equal(1, errors.Count());
            Assert.Equal((int)ErrorCode.ERR_ErrorBuildingWin32Resources, errors.First().Code);
            Assert.Equal(1, errors.First().Arguments.Count());

            CleanupAllGeneratedFiles(tmpFileName);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/30289")]
        public void Win32ResQuotes()
        {
            string[] responseFile = new string[] {
                @" /win32res:d:\\""abc def""\a""b c""d\a.res",
            };

            CSharpCommandLineArguments args = DefaultParse(CSharpCommandLineParser.ParseResponseLines(responseFile), @"c:\");
            Assert.Equal(@"d:\abc def\ab cd\a.res", args.Win32ResourceFile);

            responseFile = new string[] {
                @" /win32icon:d:\\""abc def""\a""b c""d\a.ico",
            };

            args = DefaultParse(CSharpCommandLineParser.ParseResponseLines(responseFile), @"c:\");
            Assert.Equal(@"d:\abc def\ab cd\a.ico", args.Win32Icon);

            responseFile = new string[] {
                @" /win32manifest:d:\\""abc def""\a""b c""d\a.manifest",
            };

            args = DefaultParse(CSharpCommandLineParser.ParseResponseLines(responseFile), @"c:\");
            Assert.Equal(@"d:\abc def\ab cd\a.manifest", args.Win32Manifest);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/30289")]
        public void TryParseResourceDescription()
        {
            var diags = new List<Diagnostic>();
            CommandLineResource resource;

            Assert.True(TryParse(@"\somepath\someFile.goo.bar", out resource));
            Assert.Equal(0, diags.Count);
            Assert.Equal(@"someFile.goo.bar", resource.LinkedResourceFileName);
            Assert.Equal("someFile.goo.bar", resource.ResourceName);

            Assert.True(TryParse(@"\somepath\someFile.goo.bar,someName", out resource));
            Assert.Equal(0, diags.Count);
            Assert.Equal(@"someFile.goo.bar", resource.LinkedResourceFileName);
            Assert.Equal("someName", resource.ResourceName);

            Assert.True(TryParse(@"\somepath\s""ome Fil""e.goo.bar,someName", out resource));
            Assert.Equal(0, diags.Count);
            Assert.Equal(@"some File.goo.bar", resource.LinkedResourceFileName);
            Assert.Equal("someName", resource.ResourceName);

            Assert.True(TryParse(@"\somepath\someFile.goo.bar,""some Name"",public", out resource));
            Assert.Equal(0, diags.Count);
            Assert.Equal(@"someFile.goo.bar", resource.LinkedResourceFileName);
            Assert.Equal("some Name", resource.ResourceName);
            Assert.True(resource.IsPublic);

            // Use file name in place of missing resource name.
            Assert.True(TryParse(@"\somepath\someFile.goo.bar,,private", out resource));
            Assert.Equal(0, diags.Count);
            Assert.Equal(@"someFile.goo.bar", resource.LinkedResourceFileName);
            Assert.Equal("someFile.goo.bar", resource.ResourceName);
            Assert.False(resource.IsPublic);

            // Quoted accessibility is fine.
            Assert.True(TryParse(@"\somepath\someFile.goo.bar,,""private""", out resource));
            Assert.Equal(0, diags.Count);
            Assert.Equal(@"someFile.goo.bar", resource.LinkedResourceFileName);
            Assert.Equal("someFile.goo.bar", resource.ResourceName);
            Assert.False(resource.IsPublic);

            // Leading commas are not ignored...
            Assert.False(TryParse(@",,\somepath\someFile.goo.bar,,private", out resource));
            diags.Verify(
                // error CS1906: Invalid option '\somepath\someFile.goo.bar'; Resource visibility must be either 'public' or 'private'
                Diagnostic(ErrorCode.ERR_BadResourceVis).WithArguments(@"\somepath\someFile.goo.bar"));
            diags.Clear();

            // ...even if there's whitespace between them.
            Assert.False(TryParse(@", ,\somepath\someFile.goo.bar,,private", out resource));
            diags.Verify(
                // error CS1906: Invalid option '\somepath\someFile.goo.bar'; Resource visibility must be either 'public' or 'private'
                Diagnostic(ErrorCode.ERR_BadResourceVis).WithArguments(@"\somepath\someFile.goo.bar"));
            diags.Clear();

            // Trailing commas are ignored...
            Assert.True(TryParse(@"\somepath\someFile.goo.bar,,private", out resource));
            diags.Verify();
            diags.Clear();
            Assert.Equal("someFile.goo.bar", resource.LinkedResourceFileName);
            Assert.Equal("someFile.goo.bar", resource.ResourceName);
            Assert.False(resource.IsPublic);

            // ...even if there's whitespace between them.
            Assert.True(TryParse(@"\somepath\someFile.goo.bar,,private, ,", out resource));
            diags.Verify();
            diags.Clear();
            Assert.Equal("someFile.goo.bar", resource.LinkedResourceFileName);
            Assert.Equal("someFile.goo.bar", resource.ResourceName);
            Assert.False(resource.IsPublic);

            Assert.False(TryParse(@"\somepath\someFile.goo.bar,someName,publi", out resource));
            diags.Verify(Diagnostic(ErrorCode.ERR_BadResourceVis).WithArguments("publi"));
            diags.Clear();

            Assert.False(TryParse(@"D:rive\relative\path,someName,public", out resource));
            diags.Verify(Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments(@"D:rive\relative\path"));
            diags.Clear();

            Assert.False(TryParse(@"inva\l*d?path,someName,public", out resource));
            diags.Verify(Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments(@"inva\l*d?path"));
            diags.Clear();

            Assert.False(TryParse((string)null, out resource));
            diags.Verify(Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments(""));
            diags.Clear();

            Assert.False(TryParse("", out resource));
            diags.Verify(Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments(""));
            diags.Clear();

            Assert.False(TryParse(" ", out resource));
            diags.Verify(
                // error CS2005: Missing file specification for '' option
                Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("").WithLocation(1, 1));
            diags.Clear();

            Assert.False(TryParse(" , ", out resource));
            diags.Verify(
                // error CS2005: Missing file specification for '' option
                Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("").WithLocation(1, 1));
            diags.Clear();

            Assert.True(TryParse("path, ", out resource));
            diags.Verify();
            diags.Clear();
            Assert.Equal("path", resource.LinkedResourceFileName);
            Assert.Equal("path", resource.ResourceName);
            Assert.True(resource.IsPublic);

            Assert.False(TryParse(" ,name", out resource));
            diags.Verify(
                // error CS2005: Missing file specification for '' option
                Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("").WithLocation(1, 1));
            diags.Clear();

            Assert.False(TryParse(" , , ", out resource));
            diags.Verify(
                // error CS1906: Invalid option ' '; Resource visibility must be either 'public' or 'private'
                Diagnostic(ErrorCode.ERR_BadResourceVis).WithArguments(" "));
            diags.Clear();

            Assert.False(TryParse("path, , ", out resource));
            diags.Verify(
                // error CS1906: Invalid option ' '; Resource visibility must be either 'public' or 'private'
                Diagnostic(ErrorCode.ERR_BadResourceVis).WithArguments(" "));
            diags.Clear();

            Assert.False(TryParse(" ,name, ", out resource));
            diags.Verify(
                // error CS1906: Invalid option ' '; Resource visibility must be either 'public' or 'private'
                Diagnostic(ErrorCode.ERR_BadResourceVis).WithArguments(" "));
            diags.Clear();

            Assert.False(TryParse(" , ,private", out resource));
            diags.Verify(
                // error CS2005: Missing file specification for '' option
                Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("").WithLocation(1, 1));
            diags.Clear();

            Assert.False(TryParse("path,name,", out resource));
            diags.Verify(
                // CONSIDER: Dev10 actually prints "Invalid option '|'" (note the pipe)
                // error CS1906: Invalid option ''; Resource visibility must be either 'public' or 'private'
                Diagnostic(ErrorCode.ERR_BadResourceVis).WithArguments(""));
            diags.Clear();

            Assert.False(TryParse("path,name,,", out resource));
            diags.Verify(
                // CONSIDER: Dev10 actually prints "Invalid option '|'" (note the pipe)
                // error CS1906: Invalid option ''; Resource visibility must be either 'public' or 'private'
                Diagnostic(ErrorCode.ERR_BadResourceVis).WithArguments(""));
            diags.Clear();

            Assert.False(TryParse("path,name, ", out resource));
            diags.Verify(
                // error CS1906: Invalid option ''; Resource visibility must be either 'public' or 'private'
                Diagnostic(ErrorCode.ERR_BadResourceVis).WithArguments(" "));
            diags.Clear();

            Assert.True(TryParse("path, ,private", out resource));
            diags.Verify();
            diags.Clear();
            Assert.Equal("path", resource.LinkedResourceFileName);
            Assert.Equal("path", resource.ResourceName);
            Assert.False(resource.IsPublic);

            Assert.False(TryParse(" ,name,private", out resource));
            diags.Verify(
                // error CS2005: Missing file specification for '' option
                Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("").WithLocation(1, 1));
            diags.Clear();

            var longE = new String('e', 1024);

            Assert.True(TryParse($"path,{longE},private", out resource));
            diags.Verify(); // Now checked during emit.
            diags.Clear();
            Assert.Equal("path", resource.LinkedResourceFileName);
            Assert.Equal(longE, resource.ResourceName);
            Assert.False(resource.IsPublic);

            var longI = new String('i', 260);

            Assert.False(TryParse($"{longI},e,private", out resource));
            diags.Verify(
                // error CS2021: File name '...' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments(longI).WithLocation(1, 1));

            bool TryParse(string value, out CommandLineResource resource) =>
               CSharpCommandLineParser.TryParseResourceDescription(argName: "", value.AsMemory(), WorkingDirectory, diags, isEmbedded: false, out resource);
        }

        [Fact]
        public void ManagedResourceOptions()
        {
            CSharpCommandLineArguments parsedArgs;
            ResourceDescription resourceDescription;

            parsedArgs = DefaultParse(new[] { "/resource:a", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.DisplayHelp);
            resourceDescription = parsedArgs.ManifestResources.Single();
            Assert.Null(resourceDescription.FileName); // since embedded
            Assert.Equal("a", resourceDescription.ResourceName);

            parsedArgs = DefaultParse(new[] { "/res:b", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.DisplayHelp);
            resourceDescription = parsedArgs.ManifestResources.Single();
            Assert.Null(resourceDescription.FileName); // since embedded
            Assert.Equal("b", resourceDescription.ResourceName);

            parsedArgs = DefaultParse(new[] { "/linkresource:c", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.DisplayHelp);
            resourceDescription = parsedArgs.ManifestResources.Single();
            Assert.Equal("c", resourceDescription.FileName);
            Assert.Equal("c", resourceDescription.ResourceName);

            parsedArgs = DefaultParse(new[] { "/linkres:d", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.DisplayHelp);
            resourceDescription = parsedArgs.ManifestResources.Single();
            Assert.Equal("d", resourceDescription.FileName);
            Assert.Equal("d", resourceDescription.ResourceName);
        }

        [Fact]
        public void ManagedResourceOptions_SimpleErrors()
        {
            var parsedArgs = DefaultParse(new[] { "/resource:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("/resource:"));

            parsedArgs = DefaultParse(new[] { "/resource: ", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("/resource:"));

            parsedArgs = DefaultParse(new[] { "/res", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/res"));

            parsedArgs = DefaultParse(new[] { "/RES+", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/RES+"));

            parsedArgs = DefaultParse(new[] { "/res-:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/res-:"));

            parsedArgs = DefaultParse(new[] { "/linkresource:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("/linkresource:"));

            parsedArgs = DefaultParse(new[] { "/linkresource: ", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("/linkresource:"));

            parsedArgs = DefaultParse(new[] { "/linkres", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/linkres"));

            parsedArgs = DefaultParse(new[] { "/linkRES+", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/linkRES+"));

            parsedArgs = DefaultParse(new[] { "/linkres-:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/linkres-:"));
        }

        [Fact]
        public void Link_SimpleTests()
        {
            var parsedArgs = DefaultParse(new[] { "/link:a", "/link:b,,,,c", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            AssertEx.Equal(new[] { "a", "b", "c" },
                       parsedArgs.MetadataReferences.
                                  Where((res) => res.Properties.EmbedInteropTypes).
                                  Select((res) => res.Reference));

            parsedArgs = DefaultParse(new[] { "/Link: ,,, b ,,", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            AssertEx.Equal(new[] { " b " },
                           parsedArgs.MetadataReferences.
                                      Where((res) => res.Properties.EmbedInteropTypes).
                                      Select((res) => res.Reference));

            parsedArgs = DefaultParse(new[] { "/l:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("/l:"));

            parsedArgs = DefaultParse(new[] { "/L", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "/L"));

            parsedArgs = DefaultParse(new[] { "/l+", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/l+"));

            parsedArgs = DefaultParse(new[] { "/link-:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/link-:"));
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void Recurse_SimpleTests()
        {
            var dir = Temp.CreateDirectory();
            var file1 = dir.CreateFile("a.cs");
            var file2 = dir.CreateFile("b.cs");
            var file3 = dir.CreateFile("c.txt");
            var file4 = dir.CreateDirectory("d1").CreateFile("d.txt");
            var file5 = dir.CreateDirectory("d2").CreateFile("e.cs");

            file1.WriteAllText("");
            file2.WriteAllText("");
            file3.WriteAllText("");
            file4.WriteAllText("");
            file5.WriteAllText("");

            var parsedArgs = DefaultParse(new[] { "/recurse:" + dir.ToString() + "\\*.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            AssertEx.Equal(new[] { "{DIR}\\a.cs", "{DIR}\\b.cs", "{DIR}\\d2\\e.cs" },
                           parsedArgs.SourceFiles.Select((file) => file.Path.Replace(dir.ToString(), "{DIR}")));

            parsedArgs = DefaultParse(new[] { "*.cs" }, dir.ToString());
            parsedArgs.Errors.Verify();
            AssertEx.Equal(new[] { "{DIR}\\a.cs", "{DIR}\\b.cs" },
                           parsedArgs.SourceFiles.Select((file) => file.Path.Replace(dir.ToString(), "{DIR}")));

            parsedArgs = DefaultParse(new[] { "/reCURSE:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("/reCURSE:"));

            parsedArgs = DefaultParse(new[] { "/RECURSE: ", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("/RECURSE:"));

            parsedArgs = DefaultParse(new[] { "/recurse", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/recurse"));

            parsedArgs = DefaultParse(new[] { "/recurse+", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/recurse+"));

            parsedArgs = DefaultParse(new[] { "/recurse-:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/recurse-:"));

            CleanupAllGeneratedFiles(file1.Path);
            CleanupAllGeneratedFiles(file2.Path);
            CleanupAllGeneratedFiles(file3.Path);
            CleanupAllGeneratedFiles(file4.Path);
            CleanupAllGeneratedFiles(file5.Path);
        }

        [Fact]
        public void Reference_SimpleTests()
        {
            var parsedArgs = DefaultParse(new[] { "/nostdlib", "/r:a", "/REFERENCE:b,,,,c", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            AssertEx.Equal(new[] { "a", "b", "c" },
                           parsedArgs.MetadataReferences.
                                      Where((res) => !res.Properties.EmbedInteropTypes).
                                      Select((res) => res.Reference));

            parsedArgs = DefaultParse(new[] { "/Reference: ,,, b ,,", "/nostdlib", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            AssertEx.Equal(new[] { " b " },
                           parsedArgs.MetadataReferences.
                                      Where((res) => !res.Properties.EmbedInteropTypes).
                                      Select((res) => res.Reference));

            parsedArgs = DefaultParse(new[] { "/Reference:a=b,,,", "/nostdlib", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal("a", parsedArgs.MetadataReferences.Single().Properties.Aliases.Single());
            Assert.Equal("b", parsedArgs.MetadataReferences.Single().Reference);

            parsedArgs = DefaultParse(new[] { "/r:a=b,,,c", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_OneAliasPerReference));

            parsedArgs = DefaultParse(new[] { "/r:1=b", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadExternIdentifier).WithArguments("1"));

            parsedArgs = DefaultParse(new[] { "/r:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("/r:"));

            parsedArgs = DefaultParse(new[] { "/R", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "/R"));

            parsedArgs = DefaultParse(new[] { "/reference+", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/reference+"));

            parsedArgs = DefaultParse(new[] { "/reference-:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/reference-:"));
        }

        [Fact]
        public void Target_SimpleTests()
        {
            var parsedArgs = DefaultParse(new[] { "/target:exe", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(OutputKind.ConsoleApplication, parsedArgs.CompilationOptions.OutputKind);

            parsedArgs = DefaultParse(new[] { "/t:module", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(OutputKind.NetModule, parsedArgs.CompilationOptions.OutputKind);

            parsedArgs = DefaultParse(new[] { "/target:library", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(OutputKind.DynamicallyLinkedLibrary, parsedArgs.CompilationOptions.OutputKind);

            parsedArgs = DefaultParse(new[] { "/TARGET:winexe", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(OutputKind.WindowsApplication, parsedArgs.CompilationOptions.OutputKind);

            parsedArgs = DefaultParse(new[] { "/target:appcontainerexe", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(OutputKind.WindowsRuntimeApplication, parsedArgs.CompilationOptions.OutputKind);

            parsedArgs = DefaultParse(new[] { "/target:winmdobj", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(OutputKind.WindowsRuntimeMetadata, parsedArgs.CompilationOptions.OutputKind);

            parsedArgs = DefaultParse(new[] { "/target:winexe", "/T:exe", "/target:module", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(OutputKind.NetModule, parsedArgs.CompilationOptions.OutputKind);

            parsedArgs = DefaultParse(new[] { "/t", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/t"));

            parsedArgs = DefaultParse(new[] { "/target:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.FTL_InvalidTarget));

            parsedArgs = DefaultParse(new[] { "/target:xyz", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.FTL_InvalidTarget));

            parsedArgs = DefaultParse(new[] { "/T+", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/T+"));

            parsedArgs = DefaultParse(new[] { "/TARGET-:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/TARGET-:"));
        }

        [Fact]
        public void Target_SimpleTestsNoSource()
        {
            var parsedArgs = DefaultParse(new[] { "/target:exe" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // warning CS2008: No source files specified.
                Diagnostic(ErrorCode.WRN_NoSources).WithLocation(1, 1),
                // error CS1562: Outputs without source must have the /out option specified
                Diagnostic(ErrorCode.ERR_OutputNeedsName).WithLocation(1, 1));
            Assert.Equal(OutputKind.ConsoleApplication, parsedArgs.CompilationOptions.OutputKind);

            parsedArgs = DefaultParse(new[] { "/t:module" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // warning CS2008: No source files specified.
                Diagnostic(ErrorCode.WRN_NoSources).WithLocation(1, 1),
                // error CS1562: Outputs without source must have the /out option specified
                Diagnostic(ErrorCode.ERR_OutputNeedsName).WithLocation(1, 1));
            Assert.Equal(OutputKind.NetModule, parsedArgs.CompilationOptions.OutputKind);

            parsedArgs = DefaultParse(new[] { "/target:library" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // warning CS2008: No source files specified.
                Diagnostic(ErrorCode.WRN_NoSources).WithLocation(1, 1),
                // error CS1562: Outputs without source must have the /out option specified
                Diagnostic(ErrorCode.ERR_OutputNeedsName).WithLocation(1, 1));
            Assert.Equal(OutputKind.DynamicallyLinkedLibrary, parsedArgs.CompilationOptions.OutputKind);

            parsedArgs = DefaultParse(new[] { "/TARGET:winexe" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // warning CS2008: No source files specified.
                Diagnostic(ErrorCode.WRN_NoSources).WithLocation(1, 1),
                // error CS1562: Outputs without source must have the /out option specified
                Diagnostic(ErrorCode.ERR_OutputNeedsName).WithLocation(1, 1));
            Assert.Equal(OutputKind.WindowsApplication, parsedArgs.CompilationOptions.OutputKind);

            parsedArgs = DefaultParse(new[] { "/target:appcontainerexe" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // warning CS2008: No source files specified.
                Diagnostic(ErrorCode.WRN_NoSources).WithLocation(1, 1),
                // error CS1562: Outputs without source must have the /out option specified
                Diagnostic(ErrorCode.ERR_OutputNeedsName).WithLocation(1, 1));
            Assert.Equal(OutputKind.WindowsRuntimeApplication, parsedArgs.CompilationOptions.OutputKind);

            parsedArgs = DefaultParse(new[] { "/target:winmdobj" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // warning CS2008: No source files specified.
                Diagnostic(ErrorCode.WRN_NoSources).WithLocation(1, 1),
                // error CS1562: Outputs without source must have the /out option specified
                Diagnostic(ErrorCode.ERR_OutputNeedsName).WithLocation(1, 1));
            Assert.Equal(OutputKind.WindowsRuntimeMetadata, parsedArgs.CompilationOptions.OutputKind);

            parsedArgs = DefaultParse(new[] { "/target:winexe", "/T:exe", "/target:module" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // warning CS2008: No source files specified.
                Diagnostic(ErrorCode.WRN_NoSources).WithLocation(1, 1),
                // error CS1562: Outputs without source must have the /out option specified
                Diagnostic(ErrorCode.ERR_OutputNeedsName).WithLocation(1, 1));
            Assert.Equal(OutputKind.NetModule, parsedArgs.CompilationOptions.OutputKind);

            parsedArgs = DefaultParse(new[] { "/t" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2007: Unrecognized option: '/t'
                Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/t").WithLocation(1, 1),
                // warning CS2008: No source files specified.
                Diagnostic(ErrorCode.WRN_NoSources).WithLocation(1, 1),
                // error CS1562: Outputs without source must have the /out option specified
                Diagnostic(ErrorCode.ERR_OutputNeedsName).WithLocation(1, 1));

            parsedArgs = DefaultParse(new[] { "/target:" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2019: Invalid target type for /target: must specify 'exe', 'winexe', 'library', or 'module'
                Diagnostic(ErrorCode.FTL_InvalidTarget).WithLocation(1, 1),
                // warning CS2008: No source files specified.
                Diagnostic(ErrorCode.WRN_NoSources).WithLocation(1, 1),
                // error CS1562: Outputs without source must have the /out option specified
                Diagnostic(ErrorCode.ERR_OutputNeedsName).WithLocation(1, 1));

            parsedArgs = DefaultParse(new[] { "/target:xyz" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2019: Invalid target type for /target: must specify 'exe', 'winexe', 'library', or 'module'
                Diagnostic(ErrorCode.FTL_InvalidTarget).WithLocation(1, 1),
                // warning CS2008: No source files specified.
                Diagnostic(ErrorCode.WRN_NoSources).WithLocation(1, 1),
                // error CS1562: Outputs without source must have the /out option specified
                Diagnostic(ErrorCode.ERR_OutputNeedsName).WithLocation(1, 1));

            parsedArgs = DefaultParse(new[] { "/T+" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2007: Unrecognized option: '/T+'
                Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/T+").WithLocation(1, 1),
                // warning CS2008: No source files specified.
                Diagnostic(ErrorCode.WRN_NoSources).WithLocation(1, 1),
                // error CS1562: Outputs without source must have the /out option specified
                Diagnostic(ErrorCode.ERR_OutputNeedsName).WithLocation(1, 1));

            parsedArgs = DefaultParse(new[] { "/TARGET-:" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2007: Unrecognized option: '/TARGET-:'
                Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/TARGET-:").WithLocation(1, 1),
                // warning CS2008: No source files specified.
                Diagnostic(ErrorCode.WRN_NoSources).WithLocation(1, 1),
                // error CS1562: Outputs without source must have the /out option specified
                Diagnostic(ErrorCode.ERR_OutputNeedsName).WithLocation(1, 1));
        }

        [Fact]
        public void ModuleManifest()
        {
            CSharpCommandLineArguments args = DefaultParse(new[] { "/win32manifest:blah", "/target:module", "a.cs" }, WorkingDirectory);
            args.Errors.Verify(
                // warning CS1927: Ignoring /win32manifest for module because it only applies to assemblies
                Diagnostic(ErrorCode.WRN_CantHaveManifestForModule));

            // Illegal, but not clobbered.
            Assert.Equal("blah", args.Win32Manifest);
        }

        // The following test is failing in the Linux Debug test leg of CI.
        // This issue is being tracked by https://github.com/dotnet/roslyn/issues/58077
        [ConditionalFact(typeof(WindowsOrMacOSOnly))]
        public void ArgumentParsing()
        {
            var sdkDirectory = SdkDirectory;
            var parsedArgs = CSharpCommandLineParser.Script.Parse(new[] { "a + b" }, WorkingDirectory, sdkDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.DisplayHelp);
            Assert.True(parsedArgs.SourceFiles.Any());

            parsedArgs = CSharpCommandLineParser.Script.Parse(new[] { "a + b; c" }, WorkingDirectory, sdkDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.DisplayHelp);
            Assert.True(parsedArgs.SourceFiles.Any());

            parsedArgs = CSharpCommandLineParser.Script.Parse(new[] { "/help" }, WorkingDirectory, sdkDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.DisplayHelp);
            Assert.False(parsedArgs.SourceFiles.Any());

            parsedArgs = CSharpCommandLineParser.Script.Parse(new[] { "/version" }, WorkingDirectory, sdkDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.DisplayVersion);
            Assert.False(parsedArgs.SourceFiles.Any());

            parsedArgs = CSharpCommandLineParser.Script.Parse(new[] { "/langversion:?" }, WorkingDirectory, sdkDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.DisplayLangVersions);
            Assert.False(parsedArgs.SourceFiles.Any());

            parsedArgs = CSharpCommandLineParser.Script.Parse(new[] { "//langversion:?" }, WorkingDirectory, sdkDirectory);
            parsedArgs.Errors.Verify(
                // error CS2001: Source file '//langversion:?' could not be found.
                Diagnostic(ErrorCode.ERR_FileNotFound).WithArguments("//langversion:?").WithLocation(1, 1)
                );

            parsedArgs = CSharpCommandLineParser.Script.Parse(new[] { "/version", "c.csx" }, WorkingDirectory, sdkDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.DisplayVersion);
            Assert.True(parsedArgs.SourceFiles.Any());

            parsedArgs = CSharpCommandLineParser.Script.Parse(new[] { "/version:something" }, WorkingDirectory, sdkDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.DisplayVersion);
            Assert.False(parsedArgs.SourceFiles.Any());

            parsedArgs = CSharpCommandLineParser.Script.Parse(new[] { "/?" }, WorkingDirectory, sdkDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.DisplayHelp);
            Assert.False(parsedArgs.SourceFiles.Any());

            parsedArgs = CSharpCommandLineParser.Script.Parse(new[] { "c.csx  /langversion:6" }, WorkingDirectory, sdkDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.DisplayHelp);
            Assert.True(parsedArgs.SourceFiles.Any());

            parsedArgs = CSharpCommandLineParser.Script.Parse(new[] { "/langversion:-1", "c.csx", }, WorkingDirectory, sdkDirectory);
            parsedArgs.Errors.Verify(
                // error CS1617: Invalid option '-1' for /langversion. Use '/langversion:?' to list supported values.
                Diagnostic(ErrorCode.ERR_BadCompatMode).WithArguments("-1").WithLocation(1, 1));

            Assert.False(parsedArgs.DisplayHelp);
            Assert.Equal(1, parsedArgs.SourceFiles.Length);

            parsedArgs = CSharpCommandLineParser.Script.Parse(new[] { "c.csx  /r:s=d /r:d.dll" }, WorkingDirectory, sdkDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.DisplayHelp);
            Assert.True(parsedArgs.SourceFiles.Any());

            parsedArgs = CSharpCommandLineParser.Script.Parse(new[] { "@roslyn_test_non_existing_file" }, WorkingDirectory, sdkDirectory);
            parsedArgs.Errors.Verify(
                // error CS2011: Error opening response file 'D:\R0\Main\Binaries\Debug\dd'
                Diagnostic(ErrorCode.ERR_OpenResponseFile).WithArguments(Path.Combine(WorkingDirectory, @"roslyn_test_non_existing_file")));

            Assert.False(parsedArgs.DisplayHelp);
            Assert.False(parsedArgs.SourceFiles.Any());

            parsedArgs = CSharpCommandLineParser.Script.Parse(new[] { "c /define:DEBUG" }, WorkingDirectory, sdkDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.DisplayHelp);
            Assert.True(parsedArgs.SourceFiles.Any());

            parsedArgs = CSharpCommandLineParser.Script.Parse(new[] { "\\" }, WorkingDirectory, sdkDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.DisplayHelp);
            Assert.True(parsedArgs.SourceFiles.Any());

            parsedArgs = CSharpCommandLineParser.Script.Parse(new[] { "/r:d.dll", "c.csx" }, WorkingDirectory, sdkDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.DisplayHelp);
            Assert.True(parsedArgs.SourceFiles.Any());

            parsedArgs = CSharpCommandLineParser.Script.Parse(new[] { "/define:goo", "c.csx" }, WorkingDirectory, sdkDirectory);
            parsedArgs.Errors.Verify(
                // error CS2007: Unrecognized option: '/define:goo'
                Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/define:goo"));
            Assert.False(parsedArgs.DisplayHelp);
            Assert.True(parsedArgs.SourceFiles.Any());

            parsedArgs = CSharpCommandLineParser.Script.Parse(new[] { "\"/r d.dll\"" }, WorkingDirectory, sdkDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.DisplayHelp);
            Assert.True(parsedArgs.SourceFiles.Any());

            parsedArgs = CSharpCommandLineParser.Script.Parse(new[] { "/r: d.dll", "a.cs" }, WorkingDirectory, sdkDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.DisplayHelp);
            Assert.True(parsedArgs.SourceFiles.Any());
        }

        [Theory]
        [InlineData("iso-1", LanguageVersion.CSharp1)]
        [InlineData("iso-2", LanguageVersion.CSharp2)]
        [InlineData("1", LanguageVersion.CSharp1)]
        [InlineData("1.0", LanguageVersion.CSharp1)]
        [InlineData("2", LanguageVersion.CSharp2)]
        [InlineData("2.0", LanguageVersion.CSharp2)]
        [InlineData("3", LanguageVersion.CSharp3)]
        [InlineData("3.0", LanguageVersion.CSharp3)]
        [InlineData("4", LanguageVersion.CSharp4)]
        [InlineData("4.0", LanguageVersion.CSharp4)]
        [InlineData("5", LanguageVersion.CSharp5)]
        [InlineData("5.0", LanguageVersion.CSharp5)]
        [InlineData("6", LanguageVersion.CSharp6)]
        [InlineData("6.0", LanguageVersion.CSharp6)]
        [InlineData("7", LanguageVersion.CSharp7)]
        [InlineData("7.0", LanguageVersion.CSharp7)]
        [InlineData("7.1", LanguageVersion.CSharp7_1)]
        [InlineData("7.2", LanguageVersion.CSharp7_2)]
        [InlineData("7.3", LanguageVersion.CSharp7_3)]
        [InlineData("8", LanguageVersion.CSharp8)]
        [InlineData("8.0", LanguageVersion.CSharp8)]
        [InlineData("9", LanguageVersion.CSharp9)]
        [InlineData("9.0", LanguageVersion.CSharp9)]
        [InlineData("preview", LanguageVersion.Preview)]
        public void LangVersion_CanParseCorrectVersions(string value, LanguageVersion expectedVersion)
        {
            var parsedArgs = DefaultParse(new[] { $"/langversion:{value}", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(expectedVersion, parsedArgs.ParseOptions.LanguageVersion);
            Assert.Equal(expectedVersion, parsedArgs.ParseOptions.SpecifiedLanguageVersion);

            var scriptParsedArgs = ScriptParse(new[] { $"/langversion:{value}" }, WorkingDirectory);
            scriptParsedArgs.Errors.Verify();
            Assert.Equal(expectedVersion, scriptParsedArgs.ParseOptions.LanguageVersion);
            Assert.Equal(expectedVersion, scriptParsedArgs.ParseOptions.SpecifiedLanguageVersion);
        }

        [Theory]
        [InlineData("6", "7", LanguageVersion.CSharp7)]
        [InlineData("7", "6", LanguageVersion.CSharp6)]
        [InlineData("7", "1", LanguageVersion.CSharp1)]
        [InlineData("6", "iso-1", LanguageVersion.CSharp1)]
        [InlineData("6", "iso-2", LanguageVersion.CSharp2)]
        [InlineData("6", "default", LanguageVersion.Default)]
        [InlineData("7", "default", LanguageVersion.Default)]
        [InlineData("iso-2", "6", LanguageVersion.CSharp6)]
        public void LangVersion_LatterVersionOverridesFormerOne(string formerValue, string latterValue, LanguageVersion expectedVersion)
        {
            var parsedArgs = DefaultParse(new[] { $"/langversion:{formerValue}", $"/langversion:{latterValue}", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(expectedVersion, parsedArgs.ParseOptions.SpecifiedLanguageVersion);
        }

        [Fact]
        public void LangVersion_DefaultMapsCorrectly()
        {
            LanguageVersion defaultEffectiveVersion = LanguageVersion.Default.MapSpecifiedToEffectiveVersion();
            Assert.NotEqual(LanguageVersion.Default, defaultEffectiveVersion);

            var parsedArgs = DefaultParse(new[] { "/langversion:default", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();

            Assert.Equal(LanguageVersion.Default, parsedArgs.ParseOptions.SpecifiedLanguageVersion);
            Assert.Equal(defaultEffectiveVersion, parsedArgs.ParseOptions.LanguageVersion);
        }

        [Fact]
        public void LangVersion_LatestMapsCorrectly()
        {
            LanguageVersion latestEffectiveVersion = LanguageVersion.Latest.MapSpecifiedToEffectiveVersion();
            Assert.NotEqual(LanguageVersion.Latest, latestEffectiveVersion);

            var parsedArgs = DefaultParse(new[] { "/langversion:latest", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();

            Assert.Equal(LanguageVersion.Latest, parsedArgs.ParseOptions.SpecifiedLanguageVersion);
            Assert.Equal(latestEffectiveVersion, parsedArgs.ParseOptions.LanguageVersion);
        }

        [Fact]
        public void LangVersion_NoValueSpecified()
        {
            var parsedArgs = DefaultParse(new[] { "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(LanguageVersion.Default, parsedArgs.ParseOptions.SpecifiedLanguageVersion);
        }

        [Theory]
        [InlineData("iso-3")]
        [InlineData("iso1")]
        [InlineData("8.1")]
        [InlineData("10.1")]
        [InlineData("15")]
        [InlineData("1000")]
        public void LangVersion_BadVersion(string value)
        {
            DefaultParse(new[] { $"/langversion:{value}", "a.cs" }, WorkingDirectory).Errors.Verify(
                // error CS1617: Invalid option 'XXX' for /langversion. Use '/langversion:?' to list supported values.
                Diagnostic(ErrorCode.ERR_BadCompatMode).WithArguments(value).WithLocation(1, 1)
                );

            // The canary check is a reminder that this test needs to be updated when a language version is added
            LanguageVersionAdded_Canary();
        }

        [Theory]
        [InlineData("0")]
        [InlineData("05")]
        [InlineData("07")]
        [InlineData("07.1")]
        [InlineData("08")]
        [InlineData("09")]
        public void LangVersion_LeadingZeroes(string value)
        {
            DefaultParse(new[] { $"/langversion:{value}", "a.cs" }, WorkingDirectory).Errors.Verify(
                // error CS8303: Specified language version 'XXX' cannot have leading zeroes
                Diagnostic(ErrorCode.ERR_LanguageVersionCannotHaveLeadingZeroes).WithArguments(value).WithLocation(1, 1));
        }

        [Theory]
        [InlineData("/langversion")]
        [InlineData("/langversion:")]
        [InlineData("/LANGversion:")]
        public void LangVersion_NoVersion(string option)
        {
            DefaultParse(new[] { option, "a.cs" }, WorkingDirectory).Errors.Verify(
                // error CS2006: Command-line syntax error: Missing '<text>' for '/langversion:' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "/langversion:").WithLocation(1, 1));
        }

        [Fact]
        public void LangVersion_LangVersions()
        {
            var args = DefaultParse(new[] { "/langversion:?" }, WorkingDirectory);
            args.Errors.Verify(
                // warning CS2008: No source files specified.
                Diagnostic(ErrorCode.WRN_NoSources).WithLocation(1, 1),
                // error CS1562: Outputs without source must have the /out option specified
                Diagnostic(ErrorCode.ERR_OutputNeedsName).WithLocation(1, 1)
                );
            Assert.True(args.DisplayLangVersions);
        }

        [Fact]
        public void LanguageVersionAdded_Canary()
        {
            // When a new version is added, this test will break.
            // This list must be checked for this repo:
            // - [ ] update the feature status page
            // - [ ] update all the tests that call this canary
            // - [ ] replace all references to C# "Next" (such as `TestOptions.RegularNext` or `LanguageVersionFacts.CSharpNext`) with the new version and fix failing tests
            // - [ ] update _MaxAvailableLangVersion cap (a relevant test should break when new version is introduced)
            // - [ ] update the "UpgradeProject" codefixer
            // - [ ] Remove the `ExperimentalUrl` section from any entries for language features being shipped in Syntax.xml and OperationInterfaces.xml, and rerun the generator
            // - [ ] Search the codebase for references tied to issues linked from Syntax.xml and OperationInterfaces.xml, and remove suppressions or attributes added for those issues.
            // - [ ] test VS insertion and deal with breaking changes. (note: the runtime repo uses "preview" so breaks are resolved sooner)
            //
            // Other repos also need updates:
            // - [ ] email release management to add to the release notes. See csharp-version in release.json in previous example: https://github.com/dotnet/core/pull/9493
            // - [ ] make csharplang updates documented at https://github.com/dotnet/csharplang/blob/main/Design-Process.md#steps-to-move-a-triaged-feature-to-an-implemented-feature
            // - [ ] push the list of specs to Codex. See previous example: https://devdiv.visualstudio.com/OnlineServices/_git/CodexV2Data/pullrequest/618779
            AssertEx.SetEqual(["default", "1", "2", "3", "4", "5", "6", "7.0", "7.1", "7.2", "7.3", "8.0", "9.0", "10.0", "11.0", "12.0", "13.0", "14.0", "latest", "latestmajor", "preview"],
                Enum.GetValues(typeof(LanguageVersion)).Cast<LanguageVersion>().Select(v => v.ToDisplayString()));
            // For minor versions and new major versions, the format should be "x.y", such as "7.1"
        }

        [Fact]
        public void LanguageVersion_GetErrorCode()
        {
            var versions = Enum.GetValues(typeof(LanguageVersion))
                .Cast<LanguageVersion>()
                .Except(new[] {
                    LanguageVersion.Default,
                    LanguageVersion.Latest,
                    LanguageVersion.LatestMajor,
                    LanguageVersion.Preview
                })
                .Select(v => v.GetErrorCode());

            var errorCodes = new[]
            {
                ErrorCode.ERR_FeatureNotAvailableInVersion1,
                ErrorCode.ERR_FeatureNotAvailableInVersion2,
                ErrorCode.ERR_FeatureNotAvailableInVersion3,
                ErrorCode.ERR_FeatureNotAvailableInVersion4,
                ErrorCode.ERR_FeatureNotAvailableInVersion5,
                ErrorCode.ERR_FeatureNotAvailableInVersion6,
                ErrorCode.ERR_FeatureNotAvailableInVersion7,
                ErrorCode.ERR_FeatureNotAvailableInVersion7_1,
                ErrorCode.ERR_FeatureNotAvailableInVersion7_2,
                ErrorCode.ERR_FeatureNotAvailableInVersion7_3,
                ErrorCode.ERR_FeatureNotAvailableInVersion8,
                ErrorCode.ERR_FeatureNotAvailableInVersion9,
                ErrorCode.ERR_FeatureNotAvailableInVersion10,
                ErrorCode.ERR_FeatureNotAvailableInVersion11,
                ErrorCode.ERR_FeatureNotAvailableInVersion12,
                ErrorCode.ERR_FeatureNotAvailableInVersion13,
                ErrorCode.ERR_FeatureNotAvailableInVersion14,
            };

            AssertEx.SetEqual(versions, errorCodes);

            // The canary check is a reminder that this test needs to be updated when a language version is added
            LanguageVersionAdded_Canary();
        }

        [Theory,
            InlineData(LanguageVersion.CSharp1, LanguageVersion.CSharp1),
            InlineData(LanguageVersion.CSharp2, LanguageVersion.CSharp2),
            InlineData(LanguageVersion.CSharp3, LanguageVersion.CSharp3),
            InlineData(LanguageVersion.CSharp4, LanguageVersion.CSharp4),
            InlineData(LanguageVersion.CSharp5, LanguageVersion.CSharp5),
            InlineData(LanguageVersion.CSharp6, LanguageVersion.CSharp6),
            InlineData(LanguageVersion.CSharp7, LanguageVersion.CSharp7),
            InlineData(LanguageVersion.CSharp7_1, LanguageVersion.CSharp7_1),
            InlineData(LanguageVersion.CSharp7_2, LanguageVersion.CSharp7_2),
            InlineData(LanguageVersion.CSharp7_3, LanguageVersion.CSharp7_3),
            InlineData(LanguageVersion.CSharp8, LanguageVersion.CSharp8),
            InlineData(LanguageVersion.CSharp9, LanguageVersion.CSharp9),
            InlineData(LanguageVersion.CSharp10, LanguageVersion.CSharp10),
            InlineData(LanguageVersion.CSharp11, LanguageVersion.CSharp11),
            InlineData(LanguageVersion.CSharp12, LanguageVersion.CSharp12),
            InlineData(LanguageVersion.CSharp13, LanguageVersion.CSharp13),
            InlineData(LanguageVersion.CSharp14, LanguageVersion.CSharp14),
            InlineData(LanguageVersion.CSharp14, LanguageVersion.LatestMajor),
            InlineData(LanguageVersion.CSharp14, LanguageVersion.Latest),
            InlineData(LanguageVersion.CSharp14, LanguageVersion.Default),
            InlineData(LanguageVersion.Preview, LanguageVersion.Preview),
            ]
        public void LanguageVersion_MapSpecifiedToEffectiveVersion(LanguageVersion expectedMappedVersion, LanguageVersion input)
        {
            Assert.Equal(expectedMappedVersion, input.MapSpecifiedToEffectiveVersion());
            Assert.True(expectedMappedVersion.IsValid());

            // The canary check is a reminder that this test needs to be updated when a language version is added
            LanguageVersionAdded_Canary();
        }

        [Theory,
            InlineData("iso-1", true, LanguageVersion.CSharp1),
            InlineData("ISO-1", true, LanguageVersion.CSharp1),
            InlineData("iso-2", true, LanguageVersion.CSharp2),
            InlineData("1", true, LanguageVersion.CSharp1),
            InlineData("1.0", true, LanguageVersion.CSharp1),
            InlineData("2", true, LanguageVersion.CSharp2),
            InlineData("2.0", true, LanguageVersion.CSharp2),
            InlineData("3", true, LanguageVersion.CSharp3),
            InlineData("3.0", true, LanguageVersion.CSharp3),
            InlineData("4", true, LanguageVersion.CSharp4),
            InlineData("4.0", true, LanguageVersion.CSharp4),
            InlineData("5", true, LanguageVersion.CSharp5),
            InlineData("5.0", true, LanguageVersion.CSharp5),
            InlineData("05", false, LanguageVersion.Default),
            InlineData("6", true, LanguageVersion.CSharp6),
            InlineData("6.0", true, LanguageVersion.CSharp6),
            InlineData("7", true, LanguageVersion.CSharp7),
            InlineData("7.0", true, LanguageVersion.CSharp7),
            InlineData("07", false, LanguageVersion.Default),
            InlineData("7.1", true, LanguageVersion.CSharp7_1),
            InlineData("7.2", true, LanguageVersion.CSharp7_2),
            InlineData("7.3", true, LanguageVersion.CSharp7_3),
            InlineData("8", true, LanguageVersion.CSharp8),
            InlineData("8.0", true, LanguageVersion.CSharp8),
            InlineData("9", true, LanguageVersion.CSharp9),
            InlineData("9.0", true, LanguageVersion.CSharp9),
            InlineData("10", true, LanguageVersion.CSharp10),
            InlineData("10.0", true, LanguageVersion.CSharp10),
            InlineData("11", true, LanguageVersion.CSharp11),
            InlineData("11.0", true, LanguageVersion.CSharp11),
            InlineData("12", true, LanguageVersion.CSharp12),
            InlineData("12.0", true, LanguageVersion.CSharp12),
            InlineData("13", true, LanguageVersion.CSharp13),
            InlineData("13.0", true, LanguageVersion.CSharp13),
            InlineData("08", false, LanguageVersion.Default),
            InlineData("07.1", false, LanguageVersion.Default),
            InlineData("default", true, LanguageVersion.Default),
            InlineData("latest", true, LanguageVersion.Latest),
            InlineData("latestmajor", true, LanguageVersion.LatestMajor),
            InlineData("preview", true, LanguageVersion.Preview),
            InlineData("latestpreview", false, LanguageVersion.Default),
            InlineData(null, true, LanguageVersion.Default),
            InlineData("bad", false, LanguageVersion.Default)]
        public void LanguageVersion_TryParseDisplayString(string input, bool success, LanguageVersion expected)
        {
            Assert.Equal(success, LanguageVersionFacts.TryParse(input, out var version));
            Assert.Equal(expected, version);

            // The canary check is a reminder that this test needs to be updated when a language version is added
            LanguageVersionAdded_Canary();
        }

        [Fact]
        public void LanguageVersion_TryParseTurkishDisplayString()
        {
            var originalCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR", useUserOverride: false);
            Assert.True(LanguageVersionFacts.TryParse("ISO-1", out var version));
            Assert.Equal(LanguageVersion.CSharp1, version);
            Thread.CurrentThread.CurrentCulture = originalCulture;
        }

        [Fact]
        public void LangVersion_ListLangVersions()
        {
            var dir = Temp.CreateDirectory();
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(null, dir.Path, new[] { "/langversion:?" });
            int exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);

            var expected = Enum.GetValues(typeof(LanguageVersion)).Cast<LanguageVersion>()
                .Select(v => v.ToDisplayString());

            var actual = outWriter.ToString();
            var acceptableSurroundingChar = new[] { '\r', '\n', '(', ')', ' ' };
            foreach (var version in expected)
            {
                if (version == "latest")
                    continue;

                var foundIndex = actual.IndexOf(version);
                Assert.True(foundIndex > 0, $"Missing version '{version}'");
                Assert.True(Array.IndexOf(acceptableSurroundingChar, actual[foundIndex - 1]) >= 0);
                Assert.True(Array.IndexOf(acceptableSurroundingChar, actual[foundIndex + version.Length]) >= 0);
            }
        }

        [Fact]
        [WorkItem(546961, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546961")]
        public void Define()
        {
            var parsedArgs = DefaultParse(new[] { "a.cs" }, WorkingDirectory);
            Assert.Equal(0, parsedArgs.ParseOptions.PreprocessorSymbolNames.Count());
            Assert.False(parsedArgs.Errors.Any());

            parsedArgs = DefaultParse(new[] { "/d:GOO", "a.cs" }, WorkingDirectory);
            Assert.Equal(1, parsedArgs.ParseOptions.PreprocessorSymbolNames.Count());
            Assert.Contains("GOO", parsedArgs.ParseOptions.PreprocessorSymbolNames);
            Assert.False(parsedArgs.Errors.Any());

            parsedArgs = DefaultParse(new[] { "/d:GOO;BAR,ZIP", "a.cs" }, WorkingDirectory);
            Assert.Equal(3, parsedArgs.ParseOptions.PreprocessorSymbolNames.Count());
            Assert.Contains("GOO", parsedArgs.ParseOptions.PreprocessorSymbolNames);
            Assert.Contains("BAR", parsedArgs.ParseOptions.PreprocessorSymbolNames);
            Assert.Contains("ZIP", parsedArgs.ParseOptions.PreprocessorSymbolNames);
            Assert.False(parsedArgs.Errors.Any());

            parsedArgs = DefaultParse(new[] { "/d:GOO;4X", "a.cs" }, WorkingDirectory);
            Assert.Equal(1, parsedArgs.ParseOptions.PreprocessorSymbolNames.Count());
            Assert.Contains("GOO", parsedArgs.ParseOptions.PreprocessorSymbolNames);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.WRN_DefineIdentifierRequired, parsedArgs.Errors.First().Code);
            Assert.Equal("4X", parsedArgs.Errors.First().Arguments[0]);

            IEnumerable<Diagnostic> diagnostics;

            // The docs say /d:def1[;def2]
            string compliant = "def1;def2;def3";
            var expected = new[] { "def1", "def2", "def3" };
            var parsed = CSharpCommandLineParser.ParseConditionalCompilationSymbols(compliant, out diagnostics);
            diagnostics.Verify();
            Assert.Equal<string>(expected, parsed);

            // Bug 17360: Dev11 allows for a terminating semicolon
            var dev11Compliant = "def1;def2;def3;";
            parsed = CSharpCommandLineParser.ParseConditionalCompilationSymbols(dev11Compliant, out diagnostics);
            diagnostics.Verify();
            Assert.Equal<string>(expected, parsed);

            // And comma
            dev11Compliant = "def1,def2,def3,";
            parsed = CSharpCommandLineParser.ParseConditionalCompilationSymbols(dev11Compliant, out diagnostics);
            diagnostics.Verify();
            Assert.Equal<string>(expected, parsed);

            // This breaks everything
            var nonCompliant = "def1;;def2;";
            parsed = CSharpCommandLineParser.ParseConditionalCompilationSymbols(nonCompliant, out diagnostics);
            diagnostics.Verify(
                // warning CS2029: Invalid name for a preprocessing symbol; '' is not a valid identifier
                Diagnostic(ErrorCode.WRN_DefineIdentifierRequired).WithArguments(""));
            Assert.Equal(new[] { "def1", "def2" }, parsed);

            // Bug 17360
            parsedArgs = DefaultParse(new[] { "/d:public1;public2;", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
        }

        [Fact]
        public void Debug()
        {
            var platformPdbKind = PathUtilities.IsUnixLikePlatform ? DebugInformationFormat.PortablePdb : DebugInformationFormat.Pdb;

            var parsedArgs = DefaultParse(new[] { "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.CompilationOptions.DebugPlusMode);
            Assert.False(parsedArgs.EmitPdb);
            Assert.False(parsedArgs.EmitPdbFile);
            Assert.Equal(parsedArgs.EmitOptions.DebugInformationFormat, platformPdbKind);

            parsedArgs = DefaultParse(new[] { "/debug-", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.CompilationOptions.DebugPlusMode);
            Assert.False(parsedArgs.EmitPdb);
            Assert.False(parsedArgs.EmitPdbFile);
            Assert.Equal(parsedArgs.EmitOptions.DebugInformationFormat, platformPdbKind);

            parsedArgs = DefaultParse(new[] { "/debug", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.CompilationOptions.DebugPlusMode);
            Assert.True(parsedArgs.EmitPdb);
            Assert.True(parsedArgs.EmitPdbFile);
            Assert.Equal(parsedArgs.EmitOptions.DebugInformationFormat, platformPdbKind);

            parsedArgs = DefaultParse(new[] { "/debug+", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.CompilationOptions.DebugPlusMode);
            Assert.True(parsedArgs.EmitPdb);
            Assert.True(parsedArgs.EmitPdbFile);
            Assert.Equal(parsedArgs.EmitOptions.DebugInformationFormat, platformPdbKind);

            parsedArgs = DefaultParse(new[] { "/debug+", "/debug-", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.CompilationOptions.DebugPlusMode);
            Assert.False(parsedArgs.EmitPdb);
            Assert.False(parsedArgs.EmitPdbFile);
            Assert.Equal(parsedArgs.EmitOptions.DebugInformationFormat, platformPdbKind);

            parsedArgs = DefaultParse(new[] { "/debug:full", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.CompilationOptions.DebugPlusMode);
            Assert.True(parsedArgs.EmitPdb);
            Assert.Equal(parsedArgs.EmitOptions.DebugInformationFormat, platformPdbKind);

            parsedArgs = DefaultParse(new[] { "/debug:FULL", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.CompilationOptions.DebugPlusMode);
            Assert.True(parsedArgs.EmitPdb);
            Assert.Equal(parsedArgs.EmitOptions.DebugInformationFormat, platformPdbKind);
            Assert.Equal(Path.Combine(WorkingDirectory, "a.pdb"), parsedArgs.GetPdbFilePath("a.dll"));

            parsedArgs = DefaultParse(new[] { "/debug:pdbonly", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.CompilationOptions.DebugPlusMode);
            Assert.True(parsedArgs.EmitPdb);
            Assert.Equal(parsedArgs.EmitOptions.DebugInformationFormat, platformPdbKind);

            parsedArgs = DefaultParse(new[] { "/debug:portable", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.CompilationOptions.DebugPlusMode);
            Assert.True(parsedArgs.EmitPdb);
            Assert.Equal(DebugInformationFormat.PortablePdb, parsedArgs.EmitOptions.DebugInformationFormat);
            Assert.Equal(Path.Combine(WorkingDirectory, "a.pdb"), parsedArgs.GetPdbFilePath("a.dll"));

            parsedArgs = DefaultParse(new[] { "/debug:embedded", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.CompilationOptions.DebugPlusMode);
            Assert.True(parsedArgs.EmitPdb);
            Assert.Equal(DebugInformationFormat.Embedded, parsedArgs.EmitOptions.DebugInformationFormat);
            Assert.Equal(Path.Combine(WorkingDirectory, "a.pdb"), parsedArgs.GetPdbFilePath("a.dll"));

            parsedArgs = DefaultParse(new[] { "/debug:PDBONLY", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.CompilationOptions.DebugPlusMode);
            Assert.True(parsedArgs.EmitPdb);
            Assert.Equal(parsedArgs.EmitOptions.DebugInformationFormat, platformPdbKind);

            parsedArgs = DefaultParse(new[] { "/debug:full", "/debug:pdbonly", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.CompilationOptions.DebugPlusMode);
            Assert.True(parsedArgs.EmitPdb);
            Assert.Equal(parsedArgs.EmitOptions.DebugInformationFormat, platformPdbKind);

            parsedArgs = DefaultParse(new[] { "/debug:pdbonly", "/debug:full", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.CompilationOptions.DebugPlusMode);
            Assert.True(parsedArgs.EmitPdb);
            Assert.Equal(platformPdbKind, parsedArgs.EmitOptions.DebugInformationFormat);

            parsedArgs = DefaultParse(new[] { "/debug:pdbonly", "/debug-", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.CompilationOptions.DebugPlusMode);
            Assert.False(parsedArgs.EmitPdb);
            Assert.Equal(platformPdbKind, parsedArgs.EmitOptions.DebugInformationFormat);

            parsedArgs = DefaultParse(new[] { "/debug:pdbonly", "/debug-", "/debug", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.CompilationOptions.DebugPlusMode);
            Assert.True(parsedArgs.EmitPdb);
            Assert.Equal(platformPdbKind, parsedArgs.EmitOptions.DebugInformationFormat);

            parsedArgs = DefaultParse(new[] { "/debug:pdbonly", "/debug-", "/debug+", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.CompilationOptions.DebugPlusMode);
            Assert.True(parsedArgs.EmitPdb);
            Assert.Equal(platformPdbKind, parsedArgs.EmitOptions.DebugInformationFormat);

            parsedArgs = DefaultParse(new[] { "/debug:embedded", "/debug-", "/debug+", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.CompilationOptions.DebugPlusMode);
            Assert.True(parsedArgs.EmitPdb);
            Assert.Equal(DebugInformationFormat.Embedded, parsedArgs.EmitOptions.DebugInformationFormat);

            parsedArgs = DefaultParse(new[] { "/debug:embedded", "/debug-", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.CompilationOptions.DebugPlusMode);
            Assert.False(parsedArgs.EmitPdb);
            Assert.Equal(DebugInformationFormat.Embedded, parsedArgs.EmitOptions.DebugInformationFormat);

            parsedArgs = DefaultParse(new[] { "/debug:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "debug"));

            parsedArgs = DefaultParse(new[] { "/debug:+", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadDebugType).WithArguments("+"));

            parsedArgs = DefaultParse(new[] { "/debug:invalid", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadDebugType).WithArguments("invalid"));

            parsedArgs = DefaultParse(new[] { "/debug-:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/debug-:"));
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/30289")]
        public void Pdb()
        {
            var parsedArgs = DefaultParse(new[] { "/pdb:something", "a.cs" }, WorkingDirectory);
            Assert.Equal(Path.Combine(WorkingDirectory, "something.pdb"), parsedArgs.PdbPath);
            Assert.Equal(Path.Combine(WorkingDirectory, "something.pdb"), parsedArgs.GetPdbFilePath("a.dll"));
            Assert.False(parsedArgs.EmitPdbFile);

            parsedArgs = DefaultParse(new[] { "/pdb:something", "/debug:embedded", "a.cs" }, WorkingDirectory);
            Assert.Equal(Path.Combine(WorkingDirectory, "something.pdb"), parsedArgs.PdbPath);
            Assert.Equal(Path.Combine(WorkingDirectory, "something.pdb"), parsedArgs.GetPdbFilePath("a.dll"));
            Assert.False(parsedArgs.EmitPdbFile);

            parsedArgs = DefaultParse(new[] { "/debug", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Null(parsedArgs.PdbPath);
            Assert.True(parsedArgs.EmitPdbFile);
            Assert.Equal(Path.Combine(WorkingDirectory, "a.pdb"), parsedArgs.GetPdbFilePath("a.dll"));

            parsedArgs = DefaultParse(new[] { "/pdb", "/debug", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("/pdb"));
            Assert.Equal(Path.Combine(WorkingDirectory, "a.pdb"), parsedArgs.GetPdbFilePath("a.dll"));

            parsedArgs = DefaultParse(new[] { "/pdb:", "/debug", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("/pdb:"));

            parsedArgs = DefaultParse(new[] { "/pdb:something", "/debug", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();

            // temp: path changed
            //parsedArgs = DefaultParse(new[] { "/debug", "/pdb:.x", "a.cs" }, baseDirectory);
            //parsedArgs.Errors.Verify(
            //    // error CS2021: File name '.x' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
            //    Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments(".x"));

            parsedArgs = DefaultParse(new[] { @"/pdb:""""", "/debug", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2005: Missing file specification for '/pdb:""' option
                Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments(@"/pdb:""""").WithLocation(1, 1));

            parsedArgs = DefaultParse(new[] { "/pdb:C:\\", "/debug", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments("C:\\"));

            // Should preserve fully qualified paths
            parsedArgs = DefaultParse(new[] { @"/pdb:C:\MyFolder\MyPdb.pdb", "/debug", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(@"C:\MyFolder\MyPdb.pdb", parsedArgs.PdbPath);

            // Should preserve fully qualified paths
            parsedArgs = DefaultParse(new[] { @"/pdb:c:\MyPdb.pdb", "/debug", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(@"c:\MyPdb.pdb", parsedArgs.PdbPath);

            parsedArgs = DefaultParse(new[] { @"/pdb:\MyFolder\MyPdb.pdb", "/debug", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(Path.Combine(Path.GetPathRoot(WorkingDirectory), @"MyFolder\MyPdb.pdb"), parsedArgs.PdbPath);

            // Should handle quotes
            parsedArgs = DefaultParse(new[] { @"/pdb:""C:\My Folder\MyPdb.pdb""", "/debug", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(@"C:\My Folder\MyPdb.pdb", parsedArgs.PdbPath);

            // Should expand partially qualified paths
            parsedArgs = DefaultParse(new[] { @"/pdb:MyPdb.pdb", "/debug", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(FileUtilities.ResolveRelativePath("MyPdb.pdb", WorkingDirectory), parsedArgs.PdbPath);

            // Should expand partially qualified paths
            parsedArgs = DefaultParse(new[] { @"/pdb:..\MyPdb.pdb", "/debug", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            // Temp: Path info changed
            // Assert.Equal(FileUtilities.ResolveRelativePath("MyPdb.pdb", "..\\", baseDirectory), parsedArgs.PdbPath);

            parsedArgs = DefaultParse(new[] { @"/pdb:\\b", "/debug", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2021: File name '.x' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments(@"\\b"));
            Assert.Null(parsedArgs.PdbPath);

            parsedArgs = DefaultParse(new[] { @"/pdb:\\b\OkFileName.pdb", "/debug", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2021: File name '.x' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments(@"\\b\OkFileName.pdb"));
            Assert.Null(parsedArgs.PdbPath);

            parsedArgs = DefaultParse(new[] { @"/pdb:\\server\share\MyPdb.pdb", "/debug", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(@"\\server\share\MyPdb.pdb", parsedArgs.PdbPath);

            // invalid name:
            parsedArgs = DefaultParse(new[] { "/pdb:a.b\0b", "/debug", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments("a.b\0b"));
            Assert.Null(parsedArgs.PdbPath);

            parsedArgs = DefaultParse(new[] { "/pdb:a\uD800b.pdb", "/debug", "a.cs" }, WorkingDirectory);
            //parsedArgs.Errors.Verify(
            //    Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments("a\uD800b.pdb"));
            Assert.Null(parsedArgs.PdbPath);

            // Dev11 reports CS0016: Could not write to output file 'd:\Temp\q\a<>.z'
            parsedArgs = DefaultParse(new[] { @"/pdb:""a<>.pdb""", "a.vb" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2021: File name 'a<>.pdb' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments("a<>.pdb"));
            Assert.Null(parsedArgs.PdbPath);

            parsedArgs = DefaultParse(new[] { "/pdb:.x", "/debug", "a.cs" }, WorkingDirectory);
            //parsedArgs.Errors.Verify(
            //    // error CS2021: File name '.x' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
            //    Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments(".x"));
            Assert.Null(parsedArgs.PdbPath);
        }

        [Fact]
        public void SourceLink()
        {
            var parsedArgs = DefaultParse(new[] { "/sourcelink:sl.json", "/debug:portable", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(Path.Combine(WorkingDirectory, "sl.json"), parsedArgs.SourceLink);

            parsedArgs = DefaultParse(new[] { "/sourcelink:sl.json", "/debug:embedded", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(Path.Combine(WorkingDirectory, "sl.json"), parsedArgs.SourceLink);

            parsedArgs = DefaultParse(new[] { @"/sourcelink:""s l.json""", "/debug:embedded", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(Path.Combine(WorkingDirectory, "s l.json"), parsedArgs.SourceLink);

            parsedArgs = DefaultParse(new[] { "/sourcelink:sl.json", "/debug:full", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new[] { "/sourcelink:sl.json", "/debug:pdbonly", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new[] { "/sourcelink:sl.json", "/debug-", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SourceLinkRequiresPdb));

            parsedArgs = DefaultParse(new[] { "/sourcelink:sl.json", "/debug+", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new[] { "/sourcelink:sl.json", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SourceLinkRequiresPdb));
        }

        [Fact]
        public void SourceLink_EndToEnd_EmbeddedPortable()
        {
            var dir = Temp.CreateDirectory();

            var src = dir.CreateFile("a.cs");
            src.WriteAllText(@"class C { public static void Main() {} }");

            var sl = dir.CreateFile("sl.json");
            sl.WriteAllText(@"{ ""documents"" : {} }");

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(null, dir.Path, new[] { "/nologo", "/debug:embedded", "/sourcelink:sl.json", "a.cs" });
            int exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);

            var peStream = File.OpenRead(Path.Combine(dir.Path, "a.exe"));

            using (var peReader = new PEReader(peStream))
            {
                var entry = peReader.ReadDebugDirectory().Single(e => e.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
                using (var mdProvider = peReader.ReadEmbeddedPortablePdbDebugDirectoryData(entry))
                {
                    var blob = mdProvider.GetMetadataReader().GetSourceLinkBlob();
                    AssertEx.Equal(File.ReadAllBytes(sl.Path), blob);
                }
            }

            // Clean up temp files
            CleanupAllGeneratedFiles(src.Path);
        }

        [Fact]
        public void SourceLink_EndToEnd_Portable()
        {
            var dir = Temp.CreateDirectory();

            var src = dir.CreateFile("a.cs");
            src.WriteAllText(@"class C { public static void Main() {} }");

            var sl = dir.CreateFile("sl.json");
            sl.WriteAllText(@"{ ""documents"" : {} }");

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(null, dir.Path, new[] { "/nologo", "/debug:portable", "/sourcelink:sl.json", "a.cs" });
            int exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);

            var pdbStream = File.OpenRead(Path.Combine(dir.Path, "a.pdb"));

            using (var mdProvider = MetadataReaderProvider.FromPortablePdbStream(pdbStream))
            {
                var blob = mdProvider.GetMetadataReader().GetSourceLinkBlob();
                AssertEx.Equal(File.ReadAllBytes(sl.Path), blob);
            }

            // Clean up temp files
            CleanupAllGeneratedFiles(src.Path);
        }

        [Fact]
        public void SourceLink_EndToEnd_Windows()
        {
            var dir = Temp.CreateDirectory();

            var src = dir.CreateFile("a.cs");
            src.WriteAllText(@"class C { public static void Main() {} }");

            var sl = dir.CreateFile("sl.json");
            byte[] slContent = Encoding.UTF8.GetBytes(@"{ ""documents"" : {} }");
            sl.WriteAllBytes(slContent);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(null, dir.Path, new[] { "/nologo", "/debug:full", "/sourcelink:sl.json", "a.cs" });
            int exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);

            var pdbStream = File.OpenRead(Path.Combine(dir.Path, "a.pdb"));
            var actualData = PdbValidation.GetSourceLinkData(pdbStream);
            AssertEx.Equal(slContent, actualData);

            // Clean up temp files
            CleanupAllGeneratedFiles(src.Path);
        }

        [Fact]
        public void Embed()
        {
            var parsedArgs = DefaultParse(new[] { "a.cs " }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Empty(parsedArgs.EmbeddedFiles);

            parsedArgs = DefaultParse(new[] { "/embed", "/debug:portable", "a.cs", "b.cs", "c.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            AssertEx.Equal(parsedArgs.SourceFiles, parsedArgs.EmbeddedFiles);
            AssertEx.Equal(
                new[] { "a.cs", "b.cs", "c.cs" }.Select(f => Path.Combine(WorkingDirectory, f)),
                parsedArgs.EmbeddedFiles.Select(f => f.Path));

            parsedArgs = DefaultParse(new[] { "/embed:a.cs", "/embed:b.cs", "/debug:embedded", "a.cs", "b.cs", "c.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            AssertEx.Equal(
                new[] { "a.cs", "b.cs" }.Select(f => Path.Combine(WorkingDirectory, f)),
                parsedArgs.EmbeddedFiles.Select(f => f.Path));

            parsedArgs = DefaultParse(new[] { "/embed:a.cs;b.cs", "/debug:portable", "a.cs", "b.cs", "c.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            AssertEx.Equal(
                new[] { "a.cs", "b.cs" }.Select(f => Path.Combine(WorkingDirectory, f)),
                parsedArgs.EmbeddedFiles.Select(f => f.Path));

            parsedArgs = DefaultParse(new[] { "/embed:a.cs,b.cs", "/debug:portable", "a.cs", "b.cs", "c.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            AssertEx.Equal(
                new[] { "a.cs", "b.cs" }.Select(f => Path.Combine(WorkingDirectory, f)),
                parsedArgs.EmbeddedFiles.Select(f => f.Path));

            parsedArgs = DefaultParse(new[] { @"/embed:""a,b.cs""", "/debug:portable", "a,b.cs", "c.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            AssertEx.Equal(
                new[] { "a,b.cs" }.Select(f => Path.Combine(WorkingDirectory, f)),
                parsedArgs.EmbeddedFiles.Select(f => f.Path));

            parsedArgs = DefaultParse(new[] { "/embed:a.txt", "/embed", "/debug:portable", "a.cs", "b.cs", "c.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(); ;
            AssertEx.Equal(
                new[] { "a.txt", "a.cs", "b.cs", "c.cs" }.Select(f => Path.Combine(WorkingDirectory, f)),
                parsedArgs.EmbeddedFiles.Select(f => f.Path));

            parsedArgs = DefaultParse(new[] { "/embed", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_CannotEmbedWithoutPdb));

            parsedArgs = DefaultParse(new[] { "/embed:a.txt", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_CannotEmbedWithoutPdb));

            parsedArgs = DefaultParse(new[] { "/embed", "/debug-", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_CannotEmbedWithoutPdb));

            parsedArgs = DefaultParse(new[] { "/embed:a.txt", "/debug-", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_CannotEmbedWithoutPdb));

            parsedArgs = DefaultParse(new[] { "/embed", "/debug:full", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new[] { "/embed", "/debug:pdbonly", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new[] { "/embed", "/debug+", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
        }

        [Theory]
        [InlineData("/debug:portable", "/embed", new[] { "embed.cs", "embed2.cs", "embed.xyz" })]
        [InlineData("/debug:portable", "/embed:embed.cs", new[] { "embed.cs", "embed.xyz" })]
        [InlineData("/debug:portable", "/embed:embed2.cs", new[] { "embed2.cs" })]
        [InlineData("/debug:portable", "/embed:embed.xyz", new[] { "embed.xyz" })]
        [InlineData("/debug:embedded", "/embed", new[] { "embed.cs", "embed2.cs", "embed.xyz" })]
        [InlineData("/debug:embedded", "/embed:embed.cs", new[] { "embed.cs", "embed.xyz" })]
        [InlineData("/debug:embedded", "/embed:embed2.cs", new[] { "embed2.cs" })]
        [InlineData("/debug:embedded", "/embed:embed.xyz", new[] { "embed.xyz" })]
        public void Embed_EndToEnd_Portable(string debugSwitch, string embedSwitch, string[] expectedEmbedded)
        {
            // embed.cs: large enough to compress, has #line directives
            const string embed_cs =
@"///////////////////////////////////////////////////////////////////////////////
class Program {
    static void Main() {
#line 1 ""embed.xyz""
        System.Console.WriteLine(""Hello, World"");

#line 3
        System.Console.WriteLine(""Goodbye, World"");
    }
}
///////////////////////////////////////////////////////////////////////////////";

            // embed2.cs: small enough to not compress, no sequence points
            const string embed2_cs =
@"class C
{
}";
            // target of #line
            const string embed_xyz =
@"print Hello, World

print Goodbye, World";

            Assert.True(embed_cs.Length >= EmbeddedText.CompressionThreshold);
            Assert.True(embed2_cs.Length < EmbeddedText.CompressionThreshold);

            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("embed.cs");
            var src2 = dir.CreateFile("embed2.cs");
            var txt = dir.CreateFile("embed.xyz");

            src.WriteAllText(embed_cs);
            src2.WriteAllText(embed2_cs);
            txt.WriteAllText(embed_xyz);

            var expectedEmbeddedMap = new Dictionary<string, string>();
            if (expectedEmbedded.Contains("embed.cs"))
            {
                expectedEmbeddedMap.Add(src.Path, embed_cs);
            }

            if (expectedEmbedded.Contains("embed2.cs"))
            {
                expectedEmbeddedMap.Add(src2.Path, embed2_cs);
            }

            if (expectedEmbedded.Contains("embed.xyz"))
            {
                expectedEmbeddedMap.Add(txt.Path, embed_xyz);
            }

            var output = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(null, dir.Path, new[] { "/nologo", debugSwitch, embedSwitch, "embed.cs", "embed2.cs" });
            int exitCode = csc.Run(output);
            Assert.Equal("", output.ToString().Trim());
            Assert.Equal(0, exitCode);

            switch (debugSwitch)
            {
                case "/debug:embedded":
                    ValidateEmbeddedSources_Portable(expectedEmbeddedMap, dir, isEmbeddedPdb: true);
                    break;
                case "/debug:portable":
                    ValidateEmbeddedSources_Portable(expectedEmbeddedMap, dir, isEmbeddedPdb: false);
                    break;
                case "/debug:full":
                    ValidateEmbeddedSources_Windows(expectedEmbeddedMap, dir);
                    break;
            }

            Assert.Empty(expectedEmbeddedMap);
            CleanupAllGeneratedFiles(src.Path);
        }

        private static void ValidateEmbeddedSources_Portable(Dictionary<string, string> expectedEmbeddedMap, TempDirectory dir, bool isEmbeddedPdb)
        {
            using (var peReader = new PEReader(File.OpenRead(Path.Combine(dir.Path, "embed.exe"))))
            {
                var entry = peReader.ReadDebugDirectory().SingleOrDefault(e => e.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
                Assert.Equal(isEmbeddedPdb, entry.DataSize > 0);

                using (var mdProvider = isEmbeddedPdb ?
                    peReader.ReadEmbeddedPortablePdbDebugDirectoryData(entry) :
                    MetadataReaderProvider.FromPortablePdbStream(File.OpenRead(Path.Combine(dir.Path, "embed.pdb"))))
                {
                    var mdReader = mdProvider.GetMetadataReader();

                    foreach (var handle in mdReader.Documents)
                    {
                        var doc = mdReader.GetDocument(handle);
                        var docPath = mdReader.GetString(doc.Name);

                        SourceText embeddedSource = mdReader.GetEmbeddedSource(handle);
                        if (embeddedSource == null)
                        {
                            continue;
                        }

                        Assert.Equal(expectedEmbeddedMap[docPath], embeddedSource.ToString());
                        Assert.True(expectedEmbeddedMap.Remove(docPath));
                    }
                }
            }
        }

        private static void ValidateEmbeddedSources_Windows(Dictionary<string, string> expectedEmbeddedMap, TempDirectory dir)
        {
            ISymUnmanagedReader5 symReader = null;

            try
            {
                symReader = SymReaderFactory.CreateReader(File.OpenRead(Path.Combine(dir.Path, "embed.pdb")));

                foreach (var doc in symReader.GetDocuments())
                {
                    var docPath = doc.GetName();

                    var sourceBlob = doc.GetEmbeddedSource();
                    if (sourceBlob.Array == null)
                    {
                        continue;
                    }

                    var sourceStr = Encoding.UTF8.GetString(sourceBlob.Array, sourceBlob.Offset, sourceBlob.Count);

                    Assert.Equal(expectedEmbeddedMap[docPath], sourceStr);
                    Assert.True(expectedEmbeddedMap.Remove(docPath));
                }
            }
            catch
            {
                symReader?.Dispose();
            }
        }

        private static void ValidateWrittenSources(Dictionary<string, Dictionary<string, string>> expectedFilesMap, Encoding encoding = null)
        {
            foreach ((var dirPath, var fileMap) in expectedFilesMap.ToArray())
            {
                foreach (var file in Directory.GetFiles(dirPath))
                {
                    var name = Path.GetFileName(file);
                    var content = File.ReadAllText(file, encoding ?? Encoding.UTF8);

                    Assert.Equal(fileMap[name], content);
                    Assert.True(fileMap.Remove(name));
                }
                Assert.Empty(fileMap);
                Assert.True(expectedFilesMap.Remove(dirPath));
            }
            Assert.Empty(expectedFilesMap);
        }

        [Fact]
        public void Optimize()
        {
            var parsedArgs = DefaultParse(new[] { "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(new CSharpCompilationOptions(OutputKind.ConsoleApplication).OptimizationLevel, parsedArgs.CompilationOptions.OptimizationLevel);

            parsedArgs = DefaultParse(new[] { "/optimize-", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(OptimizationLevel.Debug, parsedArgs.CompilationOptions.OptimizationLevel);

            parsedArgs = DefaultParse(new[] { "/optimize", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(OptimizationLevel.Release, parsedArgs.CompilationOptions.OptimizationLevel);

            parsedArgs = DefaultParse(new[] { "/optimize+", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(OptimizationLevel.Release, parsedArgs.CompilationOptions.OptimizationLevel);

            parsedArgs = DefaultParse(new[] { "/optimize+", "/optimize-", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(OptimizationLevel.Debug, parsedArgs.CompilationOptions.OptimizationLevel);

            parsedArgs = DefaultParse(new[] { "/optimize:+", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/optimize:+"));

            parsedArgs = DefaultParse(new[] { "/optimize:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/optimize:"));

            parsedArgs = DefaultParse(new[] { "/optimize-:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/optimize-:"));

            parsedArgs = DefaultParse(new[] { "/o-", "a.cs" }, WorkingDirectory);
            Assert.Equal(OptimizationLevel.Debug, parsedArgs.CompilationOptions.OptimizationLevel);

            parsedArgs = DefaultParse(new string[] { "/o", "a.cs" }, WorkingDirectory);
            Assert.Equal(OptimizationLevel.Release, parsedArgs.CompilationOptions.OptimizationLevel);

            parsedArgs = DefaultParse(new string[] { "/o+", "a.cs" }, WorkingDirectory);
            Assert.Equal(OptimizationLevel.Release, parsedArgs.CompilationOptions.OptimizationLevel);

            parsedArgs = DefaultParse(new string[] { "/o+", "/optimize-", "a.cs" }, WorkingDirectory);
            Assert.Equal(OptimizationLevel.Debug, parsedArgs.CompilationOptions.OptimizationLevel);

            parsedArgs = DefaultParse(new string[] { "/o:+", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/o:+"));

            parsedArgs = DefaultParse(new string[] { "/o:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/o:"));

            parsedArgs = DefaultParse(new string[] { "/o-:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/o-:"));
        }

        [Fact]
        public void Deterministic()
        {
            var parsedArgs = DefaultParse(new[] { "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.CompilationOptions.Deterministic);

            parsedArgs = DefaultParse(new[] { "/deterministic+", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.CompilationOptions.Deterministic);

            parsedArgs = DefaultParse(new[] { "/deterministic", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.CompilationOptions.Deterministic);

            parsedArgs = DefaultParse(new[] { "/deterministic-", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.CompilationOptions.Deterministic);
        }

        [Fact]
        public void ParseReferences()
        {
            var parsedArgs = DefaultParse(new string[] { "/r:goo.dll", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(2, parsedArgs.MetadataReferences.Length);

            parsedArgs = DefaultParse(new string[] { "/r:goo.dll;", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(2, parsedArgs.MetadataReferences.Length);

            Assert.Equal(MscorlibFullPath, parsedArgs.MetadataReferences[0].Reference);
            Assert.Equal(MetadataReferenceProperties.Assembly, parsedArgs.MetadataReferences[0].Properties);

            Assert.Equal("goo.dll", parsedArgs.MetadataReferences[1].Reference);
            Assert.Equal(MetadataReferenceProperties.Assembly, parsedArgs.MetadataReferences[1].Properties);

            parsedArgs = DefaultParse(new string[] { @"/l:goo.dll", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(2, parsedArgs.MetadataReferences.Length);

            Assert.Equal(MscorlibFullPath, parsedArgs.MetadataReferences[0].Reference);
            Assert.Equal(MetadataReferenceProperties.Assembly, parsedArgs.MetadataReferences[0].Properties);

            Assert.Equal("goo.dll", parsedArgs.MetadataReferences[1].Reference);
            Assert.Equal(MetadataReferenceProperties.Assembly.WithEmbedInteropTypes(true), parsedArgs.MetadataReferences[1].Properties);

            parsedArgs = DefaultParse(new string[] { @"/addmodule:goo.dll", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(2, parsedArgs.MetadataReferences.Length);

            Assert.Equal(MscorlibFullPath, parsedArgs.MetadataReferences[0].Reference);
            Assert.Equal(MetadataReferenceProperties.Assembly, parsedArgs.MetadataReferences[0].Properties);

            Assert.Equal("goo.dll", parsedArgs.MetadataReferences[1].Reference);
            Assert.Equal(MetadataReferenceProperties.Module, parsedArgs.MetadataReferences[1].Properties);

            parsedArgs = DefaultParse(new string[] { @"/r:a=goo.dll", "/l:b=bar.dll", "/addmodule:c=mod.dll", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(4, parsedArgs.MetadataReferences.Length);

            Assert.Equal(MscorlibFullPath, parsedArgs.MetadataReferences[0].Reference);
            Assert.Equal(MetadataReferenceProperties.Assembly, parsedArgs.MetadataReferences[0].Properties);

            Assert.Equal("goo.dll", parsedArgs.MetadataReferences[1].Reference);
            Assert.Equal(MetadataReferenceProperties.Assembly.WithAliases(new[] { "a" }), parsedArgs.MetadataReferences[1].Properties);

            Assert.Equal("bar.dll", parsedArgs.MetadataReferences[2].Reference);
            Assert.Equal(MetadataReferenceProperties.Assembly.WithAliases(new[] { "b" }).WithEmbedInteropTypes(true), parsedArgs.MetadataReferences[2].Properties);

            Assert.Equal("c=mod.dll", parsedArgs.MetadataReferences[3].Reference);
            Assert.Equal(MetadataReferenceProperties.Module, parsedArgs.MetadataReferences[3].Properties);

            // TODO: multiple files, quotes, etc.
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71022")]
        public void ParseReferencesAlias()
        {
            assert(@"/r:a=util.dll", @"util.dll", ["a"], false);
            assert(@"/r:""a=util.dll""", @"a=util.dll", [], false);
            assert(@"/r:""c:\users\app=exe\util.dll""", @"c:\users\app=exe\util.dll", [], false);
            assert(@"/r:a=b=util.dll", @"b=util.dll", ["a"], false);
            assert(@"/r:""a=b""=util.dll", @"a=b=util.dll", [], false);
            assert(@"/r:\""a=b\""=util.dll", @"""a=b""=util.dll", [], false);
            assert(@"/r:a""b=util.dll", "ab=util.dll", [], false);
            assert(@"/r:a\""b=util.dll", @"a""b=util.dll", [], false);
            assert(@"/r:""a""=""util.dll""", @"a=util.dll", [], false);
            assert(@"/r:\""a\""=\""util.dll""", @"""a""=""util.dll", [], false);

            void assert(string arg, string expectedRef, string[] expectedAliases, bool expectedEmbed)
            {
                var result = parseRef(arg);
                Assert.Equal(expectedRef, result.Reference);
                Assert.Equal(expectedAliases, result.Properties.Aliases);
                Assert.Equal(expectedEmbed, result.Properties.EmbedInteropTypes);
            }

            CommandLineReference parseRef(string refText)
            {
                var parsedArgs = DefaultParse([refText, "test.cs"], WorkingDirectory);
                Assert.Equal(2, parsedArgs.MetadataReferences.Length);
                Assert.Empty(parsedArgs.Errors);
                return parsedArgs.MetadataReferences[1];
            }
        }

        [Fact]
        public void ParseReferencesAliasErrors()
        {
            parseRef(@"/reference:a\b=util.dll").Verify(
                Diagnostic(ErrorCode.ERR_BadExternIdentifier).WithArguments(@"a\b").WithLocation(1, 1));

            parseRef(@"/reference:a$b=util.dll").Verify(
                Diagnostic(ErrorCode.ERR_BadExternIdentifier).WithArguments(@"a$b").WithLocation(1, 1));

            parseRef(@"/reference:a=util.dll,util2.dll").Verify(
                Diagnostic(ErrorCode.ERR_OneAliasPerReference).WithLocation(1, 1));

            parseRef(@"/reference:a=").Verify(
                Diagnostic(ErrorCode.ERR_AliasMissingFile).WithArguments("a").WithLocation(1, 1));

            ImmutableArray<Diagnostic> parseRef(string refText)
            {
                var parsedArgs = DefaultParse([refText, "test.cs"], WorkingDirectory);
                return parsedArgs.Errors;
            }
        }

        [Fact]
        public void ParseAnalyzers()
        {
            var parsedArgs = DefaultParse(new string[] { @"/a:goo.dll", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(1, parsedArgs.AnalyzerReferences.Length);
            Assert.Equal("goo.dll", parsedArgs.AnalyzerReferences[0].FilePath);

            parsedArgs = DefaultParse(new string[] { @"/analyzer:goo.dll", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(1, parsedArgs.AnalyzerReferences.Length);
            Assert.Equal("goo.dll", parsedArgs.AnalyzerReferences[0].FilePath);

            parsedArgs = DefaultParse(new string[] { "/analyzer:\"goo.dll\"", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(1, parsedArgs.AnalyzerReferences.Length);
            Assert.Equal("goo.dll", parsedArgs.AnalyzerReferences[0].FilePath);

            parsedArgs = DefaultParse(new string[] { @"/a:goo.dll;bar.dll", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(2, parsedArgs.AnalyzerReferences.Length);
            Assert.Equal("goo.dll", parsedArgs.AnalyzerReferences[0].FilePath);
            Assert.Equal("bar.dll", parsedArgs.AnalyzerReferences[1].FilePath);

            parsedArgs = DefaultParse(new string[] { @"/a:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("/a:"));

            parsedArgs = DefaultParse(new string[] { "/a", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "/a"));
        }

        [Fact]
        public void Analyzers_Missing()
        {
            string source = @"
class C
{
}
";
            var dir = Temp.CreateDirectory();

            var file = dir.CreateFile("a.cs");
            file.WriteAllText(source);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(null, dir.Path, new[] { "/nologo", "/preferreduilang:en", "/a:missing.dll", "a.cs" });
            int exitCode = csc.Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Equal("error CS0006: Metadata file 'missing.dll' could not be found", outWriter.ToString().Trim());

            // Clean up temp files
            CleanupAllGeneratedFiles(file.Path);
        }

        [Fact]
        public void Analyzers_Empty()
        {
            string source = @"
class C
{
}
";
            var dir = Temp.CreateDirectory();

            var file = dir.CreateFile("a.cs");
            file.WriteAllText(source);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(null, dir.Path, new[] { "/nologo", "/preferreduilang:en", "/t:library", "/a:" + typeof(object).Assembly.Location, "a.cs" });
            int exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.DoesNotContain("warning", outWriter.ToString());

            CleanupAllGeneratedFiles(file.Path);
        }

        private TempFile CreateRuleSetFile(string source)
        {
            var dir = Temp.CreateDirectory();
            var file = dir.CreateFile("a.ruleset");
            file.WriteAllText(source);
            return file;
        }

        [Fact]
        public void RuleSetSwitchPositive()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <IncludeAll Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Error"" />
    <Rule Id=""CA1013"" Action=""Warning"" />
    <Rule Id=""CA1014"" Action=""None"" />
  </Rules>
</RuleSet>
";
            var file = CreateRuleSetFile(source);
            var parsedArgs = DefaultParse(new string[] { @"/ruleset:" + file.Path, "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(expected: file.Path, actual: parsedArgs.RuleSetPath);
            Assert.True(parsedArgs.CompilationOptions.SpecificDiagnosticOptions.ContainsKey("CA1012"));
            Assert.True(parsedArgs.CompilationOptions.SpecificDiagnosticOptions["CA1012"] == ReportDiagnostic.Error);
            Assert.True(parsedArgs.CompilationOptions.SpecificDiagnosticOptions.ContainsKey("CA1013"));
            Assert.True(parsedArgs.CompilationOptions.SpecificDiagnosticOptions["CA1013"] == ReportDiagnostic.Warn);
            Assert.True(parsedArgs.CompilationOptions.SpecificDiagnosticOptions.ContainsKey("CA1014"));
            Assert.True(parsedArgs.CompilationOptions.SpecificDiagnosticOptions["CA1014"] == ReportDiagnostic.Suppress);
            Assert.True(parsedArgs.CompilationOptions.GeneralDiagnosticOption == ReportDiagnostic.Warn);
        }

        [Fact]
        public void RuleSetSwitchQuoted()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <IncludeAll Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Error"" />
    <Rule Id=""CA1013"" Action=""Warning"" />
    <Rule Id=""CA1014"" Action=""None"" />
  </Rules>
</RuleSet>
";
            var file = CreateRuleSetFile(source);
            var parsedArgs = DefaultParse(new string[] { @"/ruleset:" + "\"" + file.Path + "\"", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(expected: file.Path, actual: parsedArgs.RuleSetPath);
        }

        [Fact]
        public void RuleSetSwitchParseErrors()
        {
            var parsedArgs = DefaultParse(new string[] { @"/ruleset", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                 Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "ruleset"));
            Assert.Null(parsedArgs.RuleSetPath);

            parsedArgs = DefaultParse(new string[] { @"/ruleset:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "ruleset"));
            Assert.Null(parsedArgs.RuleSetPath);

            parsedArgs = DefaultParse(new string[] { @"/ruleset:blah", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                Diagnostic(ErrorCode.ERR_CantReadRulesetFile).WithArguments(Path.Combine(TempRoot.Root, "blah"), "File not found."));
            Assert.Equal(expected: Path.Combine(TempRoot.Root, "blah"), actual: parsedArgs.RuleSetPath);

            parsedArgs = DefaultParse(new string[] { @"/ruleset:blah;blah.ruleset", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                Diagnostic(ErrorCode.ERR_CantReadRulesetFile).WithArguments(Path.Combine(TempRoot.Root, "blah;blah.ruleset"), "File not found."));
            Assert.Equal(expected: Path.Combine(TempRoot.Root, "blah;blah.ruleset"), actual: parsedArgs.RuleSetPath);

            var file = CreateRuleSetFile("Random text");
            parsedArgs = DefaultParse(new string[] { @"/ruleset:" + file.Path, "a.cs" }, WorkingDirectory);
            //parsedArgs.Errors.Verify(
            //    Diagnostic(ErrorCode.ERR_CantReadRulesetFile).WithArguments(file.Path, "Data at the root level is invalid. Line 1, position 1."));
            Assert.Equal(expected: file.Path, actual: parsedArgs.RuleSetPath);
            var err = parsedArgs.Errors.Single();

            Assert.Equal((int)ErrorCode.ERR_CantReadRulesetFile, err.Code);
            Assert.Equal(2, err.Arguments.Count);
            Assert.Equal(file.Path, (string)err.Arguments[0]);
            var currentUICultureName = Thread.CurrentThread.CurrentUICulture.Name;
            if (currentUICultureName.Length == 0 || currentUICultureName.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Equal("Data at the root level is invalid. Line 1, position 1.", (string)err.Arguments[1]);
            }
        }

        [WorkItem(892467, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/892467")]
        [Fact]
        public void Analyzers_Found()
        {
            string source = @"
class C
{
}
";
            var dir = Temp.CreateDirectory();

            var file = dir.CreateFile("a.cs");
            file.WriteAllText(source);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            // This assembly has a MockAbstractDiagnosticAnalyzer type which should get run by this compilation.
            var csc = CreateCSharpCompiler(null, dir.Path, new[] { "/nologo", "/preferreduilang:en", "/t:library", "/a:" + Assembly.GetExecutingAssembly().Location, "a.cs" });
            int exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            // Diagnostic thrown
            Assert.True(outWriter.ToString().Contains("a.cs(2,7): warning Warning01: Throwing a diagnostic for types declared"));
            // Diagnostic cannot be instantiated
            Assert.True(outWriter.ToString().Contains("warning CS8032"));

            CleanupAllGeneratedFiles(file.Path);
        }

        [Fact]
        public void Analyzers_WithRuleSet()
        {
            string source = @"
class C
{
    int x;
}
";
            var dir = Temp.CreateDirectory();

            var file = dir.CreateFile("a.cs");
            file.WriteAllText(source);

            string rulesetSource = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"">
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""Warning01"" Action=""Error"" />
  </Rules>
</RuleSet>
";
            var ruleSetFile = CreateRuleSetFile(rulesetSource);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            // This assembly has a MockAbstractDiagnosticAnalyzer type which should get run by this compilation.
            var csc = CreateCSharpCompiler(null, dir.Path, new[] { "/nologo", "/preferreduilang:en", "/t:library", "/a:" + Assembly.GetExecutingAssembly().Location, "a.cs", "/ruleset:" + ruleSetFile.Path });
            int exitCode = csc.Run(outWriter);
            Assert.Equal(1, exitCode);
            // Diagnostic thrown as error.
            Assert.True(outWriter.ToString().Contains("a.cs(2,7): error Warning01: Throwing a diagnostic for types declared"));

            // Clean up temp files
            CleanupAllGeneratedFiles(file.Path);
        }

        [WorkItem(912906, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/912906")]
        [Fact]
        public void Analyzers_CommandLineOverridesRuleset1()
        {
            string source = @"
class C
{
}
";
            var dir = Temp.CreateDirectory();

            var file = dir.CreateFile("a.cs");
            file.WriteAllText(source);

            string rulesetSource = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"">
  <IncludeAll Action=""Warning"" />
</RuleSet>
";
            var ruleSetFile = CreateRuleSetFile(rulesetSource);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            // This assembly has a MockAbstractDiagnosticAnalyzer type which should get run by this compilation.
            var csc = CreateCSharpCompiler(null, dir.Path,
                new[] {
                    "/nologo", "/preferreduilang:en", "/t:library",
                    "/a:" + Assembly.GetExecutingAssembly().Location, "a.cs",
                    "/ruleset:" + ruleSetFile.Path, "/warnaserror+", "/nowarn:8032" });
            int exitCode = csc.Run(outWriter);
            Assert.Equal(1, exitCode);
            // Diagnostic thrown as error: command line always overrides ruleset.
            Assert.Contains("a.cs(2,7): error Warning01: Throwing a diagnostic for types declared", outWriter.ToString(), StringComparison.Ordinal);

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            csc = CreateCSharpCompiler(null, dir.Path,
                new[] {
                    "/nologo", "/preferreduilang:en", "/t:library",
                    "/a:" + Assembly.GetExecutingAssembly().Location, "a.cs",
                    "/warnaserror+", "/ruleset:" + ruleSetFile.Path, "/nowarn:8032" });
            exitCode = csc.Run(outWriter);
            Assert.Equal(1, exitCode);
            // Diagnostic thrown as error: command line always overrides ruleset.
            Assert.Contains("a.cs(2,7): error Warning01: Throwing a diagnostic for types declared", outWriter.ToString(), StringComparison.Ordinal);

            // Clean up temp files
            CleanupAllGeneratedFiles(file.Path);
        }

        [Fact]
        [WorkItem(468, "https://github.com/dotnet/roslyn/issues/468")]
        public void RuleSet_GeneralCommandLineOptionOverridesGeneralRuleSetOption()
        {
            var dir = Temp.CreateDirectory();

            string ruleSetSource = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"">
  <IncludeAll Action=""Warning"" />
</RuleSet>
";
            var ruleSetFile = dir.CreateFile("Rules.ruleset").WriteAllText(ruleSetSource);

            var arguments = DefaultParse(
                new[]
                {
                    "/nologo",
                    "/t:library",
                    "/ruleset:Rules.ruleset",
                    "/warnaserror+",
                    "a.cs"
                },
                dir.Path);

            var errors = arguments.Errors;
            Assert.Empty(errors);

            Assert.Equal(actual: arguments.CompilationOptions.GeneralDiagnosticOption, expected: ReportDiagnostic.Error);
        }

        [Fact]
        [WorkItem(468, "https://github.com/dotnet/roslyn/issues/468")]
        public void RuleSet_GeneralWarnAsErrorPromotesWarningFromRuleSet()
        {
            var dir = Temp.CreateDirectory();

            string ruleSetSource = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"">
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""Test001"" Action=""Warning"" />
  </Rules>
</RuleSet>
";
            var ruleSetFile = dir.CreateFile("Rules.ruleset").WriteAllText(ruleSetSource);

            var arguments = DefaultParse(
                new[]
                {
                    "/nologo",
                    "/t:library",
                    "/ruleset:Rules.ruleset",
                    "/warnaserror+",
                    "a.cs"
                },
                dir.Path);

            var errors = arguments.Errors;
            Assert.Empty(errors);

            Assert.Equal(actual: arguments.CompilationOptions.GeneralDiagnosticOption, expected: ReportDiagnostic.Error);
            Assert.Equal(actual: arguments.CompilationOptions.SpecificDiagnosticOptions["Test001"], expected: ReportDiagnostic.Error);
        }

        [Fact]
        [WorkItem(468, "https://github.com/dotnet/roslyn/issues/468")]
        public void RuleSet_GeneralWarnAsErrorDoesNotPromoteInfoFromRuleSet()
        {
            var dir = Temp.CreateDirectory();

            string ruleSetSource = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"">
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""Test001"" Action=""Info"" />
  </Rules>
</RuleSet>
";
            var ruleSetFile = dir.CreateFile("Rules.ruleset").WriteAllText(ruleSetSource);

            var arguments = DefaultParse(
                new[]
                {
                    "/nologo",
                    "/t:library",
                    "/ruleset:Rules.ruleset",
                    "/warnaserror+",
                    "a.cs"
                },
                dir.Path);

            var errors = arguments.Errors;
            Assert.Empty(errors);

            Assert.Equal(actual: arguments.CompilationOptions.GeneralDiagnosticOption, expected: ReportDiagnostic.Error);
            Assert.Equal(actual: arguments.CompilationOptions.SpecificDiagnosticOptions["Test001"], expected: ReportDiagnostic.Info);
        }

        [Fact]
        [WorkItem(468, "https://github.com/dotnet/roslyn/issues/468")]
        public void RuleSet_SpecificWarnAsErrorPromotesInfoFromRuleSet()
        {
            var dir = Temp.CreateDirectory();

            string ruleSetSource = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"">
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""Test001"" Action=""Info"" />
  </Rules>
</RuleSet>
";
            var ruleSetFile = dir.CreateFile("Rules.ruleset").WriteAllText(ruleSetSource);

            var arguments = DefaultParse(
                new[]
                {
                    "/nologo",
                    "/t:library",
                    "/ruleset:Rules.ruleset",
                    "/warnaserror+:Test001",
                    "a.cs"
                },
                dir.Path);

            var errors = arguments.Errors;
            Assert.Empty(errors);

            Assert.Equal(actual: arguments.CompilationOptions.GeneralDiagnosticOption, expected: ReportDiagnostic.Default);
            Assert.Equal(actual: arguments.CompilationOptions.SpecificDiagnosticOptions["Test001"], expected: ReportDiagnostic.Error);
        }

        [Fact]
        [WorkItem(468, "https://github.com/dotnet/roslyn/issues/468")]
        public void RuleSet_GeneralWarnAsErrorMinusResetsRules()
        {
            var dir = Temp.CreateDirectory();

            string ruleSetSource = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"">
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""Test001"" Action=""Warning"" />
  </Rules>
</RuleSet>
";
            var ruleSetFile = dir.CreateFile("Rules.ruleset").WriteAllText(ruleSetSource);

            var arguments = DefaultParse(
                new[]
                {
                    "/nologo",
                    "/t:library",
                    "/ruleset:Rules.ruleset",
                    "/warnaserror+",
                    "/warnaserror-",
                    "a.cs"
                },
                dir.Path);

            var errors = arguments.Errors;
            Assert.Empty(errors);

            Assert.Equal(actual: arguments.CompilationOptions.GeneralDiagnosticOption, expected: ReportDiagnostic.Default);
            Assert.Equal(actual: arguments.CompilationOptions.SpecificDiagnosticOptions["Test001"], expected: ReportDiagnostic.Warn);
        }

        [Fact]
        [WorkItem(468, "https://github.com/dotnet/roslyn/issues/468")]
        public void RuleSet_SpecificWarnAsErrorMinusResetsRules()
        {
            var dir = Temp.CreateDirectory();

            string ruleSetSource = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"">
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""Test001"" Action=""Warning"" />
  </Rules>
</RuleSet>
";
            var ruleSetFile = dir.CreateFile("Rules.ruleset").WriteAllText(ruleSetSource);

            var arguments = DefaultParse(
                new[]
                {
                    "/nologo",
                    "/t:library",
                    "/ruleset:Rules.ruleset",
                    "/warnaserror+",
                    "/warnaserror-:Test001",
                    "a.cs"
                },
                dir.Path);

            var errors = arguments.Errors;
            Assert.Empty(errors);

            Assert.Equal(actual: arguments.CompilationOptions.GeneralDiagnosticOption, expected: ReportDiagnostic.Error);
            Assert.Equal(actual: arguments.CompilationOptions.SpecificDiagnosticOptions["Test001"], expected: ReportDiagnostic.Warn);
        }

        [Fact]
        [WorkItem(468, "https://github.com/dotnet/roslyn/issues/468")]
        public void RuleSet_SpecificWarnAsErrorMinusDefaultsRuleNotInRuleSet()
        {
            var dir = Temp.CreateDirectory();

            string ruleSetSource = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"">
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""Test001"" Action=""Warning"" />
  </Rules>
</RuleSet>
";
            var ruleSetFile = dir.CreateFile("Rules.ruleset").WriteAllText(ruleSetSource);

            var arguments = DefaultParse(
                new[]
                {
                    "/nologo",
                    "/t:library",
                    "/ruleset:Rules.ruleset",
                    "/warnaserror+:Test002",
                    "/warnaserror-:Test002",
                    "a.cs"
                },
                dir.Path);

            var errors = arguments.Errors;
            Assert.Empty(errors);

            Assert.Equal(actual: arguments.CompilationOptions.GeneralDiagnosticOption, expected: ReportDiagnostic.Default);
            Assert.Equal(actual: arguments.CompilationOptions.SpecificDiagnosticOptions["Test001"], expected: ReportDiagnostic.Warn);
            Assert.Equal(actual: arguments.CompilationOptions.SpecificDiagnosticOptions["Test002"], expected: ReportDiagnostic.Default);
        }

        [Fact]
        [WorkItem(468, "https://github.com/dotnet/roslyn/issues/468")]
        public void NoWarn_SpecificNoWarnOverridesRuleSet()
        {
            var dir = Temp.CreateDirectory();

            string ruleSetSource = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"">
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""Test001"" Action=""Warning"" />
  </Rules>
</RuleSet>
";
            var ruleSetFile = dir.CreateFile("Rules.ruleset").WriteAllText(ruleSetSource);

            var arguments = DefaultParse(
                new[]
                {
                    "/nologo",
                    "/t:library",
                    "/ruleset:Rules.ruleset",
                    "/nowarn:Test001",
                    "a.cs"
                },
                dir.Path);

            var errors = arguments.Errors;
            Assert.Empty(errors);

            Assert.Equal(expected: ReportDiagnostic.Default, actual: arguments.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(expected: 1, actual: arguments.CompilationOptions.SpecificDiagnosticOptions.Count);
            Assert.Equal(expected: ReportDiagnostic.Suppress, actual: arguments.CompilationOptions.SpecificDiagnosticOptions["Test001"]);
        }

        [Fact]
        [WorkItem(468, "https://github.com/dotnet/roslyn/issues/468")]
        public void NoWarn_SpecificNoWarnOverridesGeneralWarnAsError()
        {
            var dir = Temp.CreateDirectory();

            string ruleSetSource = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"">
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""Test001"" Action=""Warning"" />
  </Rules>
</RuleSet>
";
            var ruleSetFile = dir.CreateFile("Rules.ruleset").WriteAllText(ruleSetSource);

            var arguments = DefaultParse(
                new[]
                {
                    "/nologo",
                    "/t:library",
                    "/ruleset:Rules.ruleset",
                    "/warnaserror+",
                    "/nowarn:Test001",
                    "a.cs"
                },
                dir.Path);

            var errors = arguments.Errors;
            Assert.Empty(errors);

            Assert.Equal(expected: ReportDiagnostic.Error, actual: arguments.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(expected: 1, actual: arguments.CompilationOptions.SpecificDiagnosticOptions.Count);
            Assert.Equal(expected: ReportDiagnostic.Suppress, actual: arguments.CompilationOptions.SpecificDiagnosticOptions["Test001"]);
        }

        [Fact]
        [WorkItem(468, "https://github.com/dotnet/roslyn/issues/468")]
        public void NoWarn_SpecificNoWarnOverridesSpecificWarnAsError()
        {
            var dir = Temp.CreateDirectory();

            string ruleSetSource = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"">
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""Test001"" Action=""Warning"" />
  </Rules>
</RuleSet>
";
            var ruleSetFile = dir.CreateFile("Rules.ruleset").WriteAllText(ruleSetSource);

            var arguments = DefaultParse(
                new[]
                {
                    "/nologo",
                    "/t:library",
                    "/ruleset:Rules.ruleset",
                    "/nowarn:Test001",
                    "/warnaserror+:Test001",
                    "a.cs"
                },
                dir.Path);

            var errors = arguments.Errors;
            Assert.Empty(errors);

            Assert.Equal(expected: ReportDiagnostic.Default, actual: arguments.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(expected: 1, actual: arguments.CompilationOptions.SpecificDiagnosticOptions.Count);
            Assert.Equal(expected: ReportDiagnostic.Suppress, actual: arguments.CompilationOptions.SpecificDiagnosticOptions["Test001"]);
        }

        [Fact]
        [WorkItem(35748, "https://github.com/dotnet/roslyn/issues/35748")]
        public void NoWarn_Nullable()
        {
            var dir = Temp.CreateDirectory();

            var arguments = DefaultParse(
                new[]
                {
                    "/nologo",
                    "/t:library",
                    "/nowarn:nullable",
                    "a.cs"
                },
                dir.Path);

            var errors = arguments.Errors;
            Assert.Empty(errors);

            Assert.Equal(expected: ReportDiagnostic.Default, actual: arguments.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(expected: ErrorFacts.NullableWarnings.Count + 2, actual: arguments.CompilationOptions.SpecificDiagnosticOptions.Count);

            foreach (string warning in ErrorFacts.NullableWarnings)
            {
                Assert.Equal(expected: ReportDiagnostic.Suppress, actual: arguments.CompilationOptions.SpecificDiagnosticOptions[warning]);
            }

            Assert.Equal(expected: ReportDiagnostic.Suppress,
                actual: arguments.CompilationOptions.SpecificDiagnosticOptions[MessageProvider.Instance.GetIdForErrorCode((int)ErrorCode.WRN_MissingNonNullTypesContextForAnnotation)]);
            Assert.Equal(expected: ReportDiagnostic.Suppress,
                actual: arguments.CompilationOptions.SpecificDiagnosticOptions[MessageProvider.Instance.GetIdForErrorCode((int)ErrorCode.WRN_MissingNonNullTypesContextForAnnotationInGeneratedCode)]);
        }

        [Fact]
        [WorkItem(35748, "https://github.com/dotnet/roslyn/issues/35748")]
        public void NoWarn_Nullable_Capitalization()
        {
            var dir = Temp.CreateDirectory();

            var arguments = DefaultParse(
                new[]
                {
                    "/nologo",
                    "/t:library",
                    "/nowarn:NullABLE",
                    "a.cs"
                },
                dir.Path);

            var errors = arguments.Errors;
            Assert.Empty(errors);

            Assert.Equal(expected: ReportDiagnostic.Default, actual: arguments.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(expected: ErrorFacts.NullableWarnings.Count + 2, actual: arguments.CompilationOptions.SpecificDiagnosticOptions.Count);

            foreach (string warning in ErrorFacts.NullableWarnings)
            {
                Assert.Equal(expected: ReportDiagnostic.Suppress, actual: arguments.CompilationOptions.SpecificDiagnosticOptions[warning]);
            }

            Assert.Equal(expected: ReportDiagnostic.Suppress,
                actual: arguments.CompilationOptions.SpecificDiagnosticOptions[MessageProvider.Instance.GetIdForErrorCode((int)ErrorCode.WRN_MissingNonNullTypesContextForAnnotation)]);
            Assert.Equal(expected: ReportDiagnostic.Suppress,
                actual: arguments.CompilationOptions.SpecificDiagnosticOptions[MessageProvider.Instance.GetIdForErrorCode((int)ErrorCode.WRN_MissingNonNullTypesContextForAnnotationInGeneratedCode)]);
        }

        [Fact]
        [WorkItem(35748, "https://github.com/dotnet/roslyn/issues/35748")]
        public void NoWarn_Nullable_MultipleArguments()
        {
            var dir = Temp.CreateDirectory();

            var arguments = DefaultParse(
                new[]
                {
                    "/nologo",
                    "/t:library",
                    "/nowarn:nullable,Test001",
                    "a.cs"
                },
                dir.Path);

            var errors = arguments.Errors;
            Assert.Empty(errors);

            Assert.Equal(expected: ReportDiagnostic.Default, actual: arguments.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(expected: ErrorFacts.NullableWarnings.Count + 3, actual: arguments.CompilationOptions.SpecificDiagnosticOptions.Count);

            foreach (string warning in ErrorFacts.NullableWarnings)
            {
                Assert.Equal(expected: ReportDiagnostic.Suppress, actual: arguments.CompilationOptions.SpecificDiagnosticOptions[warning]);
            }

            Assert.Equal(expected: ReportDiagnostic.Suppress, actual: arguments.CompilationOptions.SpecificDiagnosticOptions["Test001"]);
            Assert.Equal(expected: ReportDiagnostic.Suppress,
                actual: arguments.CompilationOptions.SpecificDiagnosticOptions[MessageProvider.Instance.GetIdForErrorCode((int)ErrorCode.WRN_MissingNonNullTypesContextForAnnotation)]);
            Assert.Equal(expected: ReportDiagnostic.Suppress,
                actual: arguments.CompilationOptions.SpecificDiagnosticOptions[MessageProvider.Instance.GetIdForErrorCode((int)ErrorCode.WRN_MissingNonNullTypesContextForAnnotationInGeneratedCode)]);
        }

        [Fact]
        [WorkItem(35748, "https://github.com/dotnet/roslyn/issues/35748")]
        public void WarnAsError_Nullable()
        {
            var dir = Temp.CreateDirectory();

            var arguments = DefaultParse(
                new[]
                {
                    "/nologo",
                    "/t:library",
                    "/warnaserror:nullable",
                    "a.cs"
                },
                dir.Path);

            var errors = arguments.Errors;
            Assert.Empty(errors);

            Assert.Equal(expected: ReportDiagnostic.Default, actual: arguments.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(expected: ErrorFacts.NullableWarnings.Count + 2, actual: arguments.CompilationOptions.SpecificDiagnosticOptions.Count);

            foreach (string warning in ErrorFacts.NullableWarnings)
            {
                Assert.Equal(expected: ReportDiagnostic.Error, actual: arguments.CompilationOptions.SpecificDiagnosticOptions[warning]);
            }

            Assert.Equal(expected: ReportDiagnostic.Error,
                actual: arguments.CompilationOptions.SpecificDiagnosticOptions[MessageProvider.Instance.GetIdForErrorCode((int)ErrorCode.WRN_MissingNonNullTypesContextForAnnotation)]);
            Assert.Equal(expected: ReportDiagnostic.Error,
                actual: arguments.CompilationOptions.SpecificDiagnosticOptions[MessageProvider.Instance.GetIdForErrorCode((int)ErrorCode.WRN_MissingNonNullTypesContextForAnnotationInGeneratedCode)]);
        }

        [WorkItem(912906, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/912906")]
        [Fact]
        public void Analyzers_CommandLineOverridesRuleset2()
        {
            string source = @"
class C
{
}
";
            var dir = Temp.CreateDirectory();

            var file = dir.CreateFile("a.cs");
            file.WriteAllText(source);

            string rulesetSource = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"">
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""Warning01"" Action=""Error"" />
  </Rules>
</RuleSet>
";
            var ruleSetFile = CreateRuleSetFile(rulesetSource);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            // This assembly has a MockAbstractDiagnosticAnalyzer type which should get run by this compilation.
            var csc = CreateCSharpCompiler(null, dir.Path,
                new[] {
                    "/nologo", "/t:library",
                    "/a:" + Assembly.GetExecutingAssembly().Location, "a.cs",
                    "/ruleset:" + ruleSetFile.Path, "/warn:0" });
            int exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            // Diagnostic suppressed: commandline always overrides ruleset.
            Assert.DoesNotContain("Warning01", outWriter.ToString(), StringComparison.Ordinal);

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            csc = CreateCSharpCompiler(null, dir.Path,
                new[] {
                    "/nologo", "/t:library",
                    "/a:" + Assembly.GetExecutingAssembly().Location, "a.cs",
                    "/warn:0", "/ruleset:" + ruleSetFile.Path });
            exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            // Diagnostic suppressed: commandline always overrides ruleset.
            Assert.DoesNotContain("Warning01", outWriter.ToString(), StringComparison.Ordinal);

            // Clean up temp files
            CleanupAllGeneratedFiles(file.Path);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/30289")]
        public void DiagnosticFormatting()
        {
            string source = @"
using System;

class C
{
        public static void Main()
        {
            Goo(0);
#line 10 ""c:\temp\a\1.cs""
            Goo(1);
#line 20 ""C:\a\..\b.cs""
            Goo(2);
#line 30 ""C:\a\../B.cs""
            Goo(3);
#line 40 ""../b.cs""
            Goo(4);
#line 50 ""..\b.cs""
            Goo(5);
#line 60 ""C:\X.cs""
            Goo(6);
#line 70 ""C:\x.cs""
            Goo(7);
#line 90 ""      ""
		    Goo(9);
#line 100 ""C:\*.cs""
		    Goo(10);
#line 110 """"
		    Goo(11);
#line hidden
            Goo(12);
#line default
            Goo(13);
#line 140 ""***""
            Goo(14);
        }
    }
";
            var dir = Temp.CreateDirectory();
            dir.CreateFile("a.cs").WriteAllText(source);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(null, dir.Path, new[] { "/nologo", "/preferreduilang:en", "/t:library", "a.cs" });
            int exitCode = csc.Run(outWriter);
            Assert.Equal(1, exitCode);

            // with /fullpaths off
            string expected = @"
a.cs(8,13): error CS0103: The name 'Goo' does not exist in the current context
c:\temp\a\1.cs(10,13): error CS0103: The name 'Goo' does not exist in the current context
C:\b.cs(20,13): error CS0103: The name 'Goo' does not exist in the current context
C:\B.cs(30,13): error CS0103: The name 'Goo' does not exist in the current context
" + Path.GetFullPath(Path.Combine(dir.Path, @"..\b.cs")) + @"(40,13): error CS0103: The name 'Goo' does not exist in the current context
" + Path.GetFullPath(Path.Combine(dir.Path, @"..\b.cs")) + @"(50,13): error CS0103: The name 'Goo' does not exist in the current context
C:\X.cs(60,13): error CS0103: The name 'Goo' does not exist in the current context
C:\x.cs(70,13): error CS0103: The name 'Goo' does not exist in the current context
      (90,7): error CS0103: The name 'Goo' does not exist in the current context
C:\*.cs(100,7): error CS0103: The name 'Goo' does not exist in the current context
(110,7): error CS0103: The name 'Goo' does not exist in the current context
(112,13): error CS0103: The name 'Goo' does not exist in the current context
a.cs(32,13): error CS0103: The name 'Goo' does not exist in the current context
***(140,13): error CS0103: The name 'Goo' does not exist in the current context";

            AssertEx.Equal(
                expected.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries),
                outWriter.ToString().Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries),
                itemSeparator: "\r\n");

            // with /fullpaths on
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            csc = CreateCSharpCompiler(null, dir.Path, new[] { "/nologo", "/preferreduilang:en", "/t:library", "/fullpaths", "a.cs" });
            exitCode = csc.Run(outWriter);
            Assert.Equal(1, exitCode);

            expected = @"
" + Path.Combine(dir.Path, @"a.cs") + @"(8,13): error CS0103: The name 'Goo' does not exist in the current context
c:\temp\a\1.cs(10,13): error CS0103: The name 'Goo' does not exist in the current context
C:\b.cs(20,13): error CS0103: The name 'Goo' does not exist in the current context
C:\B.cs(30,13): error CS0103: The name 'Goo' does not exist in the current context
" + Path.GetFullPath(Path.Combine(dir.Path, @"..\b.cs")) + @"(40,13): error CS0103: The name 'Goo' does not exist in the current context
" + Path.GetFullPath(Path.Combine(dir.Path, @"..\b.cs")) + @"(50,13): error CS0103: The name 'Goo' does not exist in the current context
C:\X.cs(60,13): error CS0103: The name 'Goo' does not exist in the current context
C:\x.cs(70,13): error CS0103: The name 'Goo' does not exist in the current context
      (90,7): error CS0103: The name 'Goo' does not exist in the current context
C:\*.cs(100,7): error CS0103: The name 'Goo' does not exist in the current context
(110,7): error CS0103: The name 'Goo' does not exist in the current context
(112,13): error CS0103: The name 'Goo' does not exist in the current context
" + Path.Combine(dir.Path, @"a.cs") + @"(32,13): error CS0103: The name 'Goo' does not exist in the current context
***(140,13): error CS0103: The name 'Goo' does not exist in the current context";

            AssertEx.Equal(
                expected.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries),
                outWriter.ToString().Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries),
                itemSeparator: "\r\n");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47310")]
        public void DiagnosticFormatting_UrlFormat_ObsoleteAttribute()
        {
            var dir = Temp.CreateDirectory();
            var file = dir.CreateFile("a.cs");
            file.WriteAllText("""
                #pragma warning disable CS0436 // System.Obsolete conflict
                #nullable enable
                using System;

                var c1 = new C1();
                var c2 = new C2();
                var c3 = new C3();
                var c4 = new C4();

                [Obsolete("Do not use C1", UrlFormat = "https://example.org/{0}")]
                public class C1 { }
                [Obsolete("Do not use C2", error: true, UrlFormat = "https://example.org/2/{0}")]
                public class C2 { }
                [Obsolete("Do not use C3", error: true, DiagnosticId = "OBSOLETEC3", UrlFormat = "https://example.org/3/{0}")]
                public class C3 { }
                [Obsolete("Do not use C4", DiagnosticId = "OBSOLETEC4", UrlFormat = "https://example.org/4")]
                public class C4 { }

                namespace System
                {
                    public class ObsoleteAttribute : Attribute
                    {
                        public ObsoleteAttribute() { }
                        public ObsoleteAttribute(string? message) { }
                        public ObsoleteAttribute(string? message, bool error) { }

                        public string? DiagnosticId { get; set; }
                        public string? UrlFormat { get; set; }
                    }
                }
                """);

            var output = VerifyOutput(dir, file,
                includeCurrentAssemblyAsAnalyzerReference: false,
                expectedWarningCount: 2,
                expectedErrorCount: 2,
                additionalFlags: new[] { "/t:exe" });

            AssertEx.Equal("""
                a.cs(5,14): warning CS0618: 'C1' is obsolete: 'Do not use C1' (https://example.org/CS0618)
                a.cs(6,14): error CS0619: 'C2' is obsolete: 'Do not use C2' (https://example.org/2/CS0619)
                a.cs(7,14): error OBSOLETEC3: 'C3' is obsolete: 'Do not use C3' (https://example.org/3/OBSOLETEC3)
                a.cs(8,14): warning OBSOLETEC4: 'C4' is obsolete: 'Do not use C4' (https://example.org/4)
                """,
                output.Trim());

            CleanupAllGeneratedFiles(file.Path);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47310")]
        public void DiagnosticFormatting_DiagnosticAnalyzer()
        {
            var dir = Temp.CreateDirectory();
            var file = dir.CreateFile("a.cs");
            file.WriteAllText("class C { }");

            var output = VerifyOutput(dir, file,
                includeCurrentAssemblyAsAnalyzerReference: false,
                expectedWarningCount: 1,
                analyzers: new[] { new WarningWithUrlDiagnosticAnalyzer() });

            AssertEx.Equal("""
                a.cs(1,7): warning Warning02: Throwing a diagnostic for types declared (https://example.org/analyzer)
                """,
                output.Trim());

            CleanupAllGeneratedFiles(file.Path);
        }

        [WorkItem(540891, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540891")]
        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/30289")]
        public void ParseOut()
        {
            const string baseDirectory = @"C:\abc\def\baz";

            var parsedArgs = DefaultParse(new[] { @"/out:""""", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2021: File name '' contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments(""));

            parsedArgs = DefaultParse(new[] { @"/out:", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2005: Missing file specification for '/out:' option
                Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("/out:"));

            parsedArgs = DefaultParse(new[] { @"/refout:", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2005: Missing file specification for '/refout:' option
                Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("/refout:"));

            parsedArgs = DefaultParse(new[] { @"/refout:ref.dll", "/refonly", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS8301: Do not use refout when using refonly.
                Diagnostic(ErrorCode.ERR_NoRefOutWhenRefOnly).WithLocation(1, 1));

            parsedArgs = DefaultParse(new[] { @"/refout:ref.dll", "/link:b", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new[] { "/refonly", "/link:b", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new[] { "/refonly:incorrect", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2007: Unrecognized option: '/refonly:incorrect'
                Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/refonly:incorrect").WithLocation(1, 1)
                );

            parsedArgs = DefaultParse(new[] { @"/refout:ref.dll", "/target:module", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS8302: Cannot compile net modules when using /refout or /refonly.
                Diagnostic(ErrorCode.ERR_NoNetModuleOutputWhenRefOutOrRefOnly).WithLocation(1, 1)
                );

            parsedArgs = DefaultParse(new[] { @"/refonly", "/target:module", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS8302: Cannot compile net modules when using /refout or /refonly.
                Diagnostic(ErrorCode.ERR_NoNetModuleOutputWhenRefOutOrRefOnly).WithLocation(1, 1)
                );

            // Dev11 reports CS2007: Unrecognized option: '/out'
            parsedArgs = DefaultParse(new[] { @"/out", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2005: Missing file specification for '/out' option
                Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("/out"));

            parsedArgs = DefaultParse(new[] { @"/out+", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/out+"));

            // Should preserve fully qualified paths
            parsedArgs = DefaultParse(new[] { @"/out:C:\MyFolder\MyBinary.dll", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal("MyBinary", parsedArgs.CompilationName);
            Assert.Equal("MyBinary.dll", parsedArgs.OutputFileName);
            Assert.Equal("MyBinary.dll", parsedArgs.CompilationOptions.ModuleName);
            Assert.Equal(@"C:\MyFolder", parsedArgs.OutputDirectory);
            Assert.Equal(@"C:\MyFolder\MyBinary.dll", parsedArgs.GetOutputFilePath(parsedArgs.OutputFileName));

            // Should handle quotes
            parsedArgs = DefaultParse(new[] { @"/out:""C:\My Folder\MyBinary.dll""", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(@"MyBinary", parsedArgs.CompilationName);
            Assert.Equal("MyBinary.dll", parsedArgs.OutputFileName);
            Assert.Equal("MyBinary.dll", parsedArgs.CompilationOptions.ModuleName);
            Assert.Equal(@"C:\My Folder", parsedArgs.OutputDirectory);

            // Should expand partially qualified paths
            parsedArgs = DefaultParse(new[] { @"/out:MyBinary.dll", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal("MyBinary", parsedArgs.CompilationName);
            Assert.Equal("MyBinary.dll", parsedArgs.OutputFileName);
            Assert.Equal("MyBinary.dll", parsedArgs.CompilationOptions.ModuleName);
            Assert.Equal(baseDirectory, parsedArgs.OutputDirectory);
            Assert.Equal(Path.Combine(baseDirectory, "MyBinary.dll"), parsedArgs.GetOutputFilePath(parsedArgs.OutputFileName));

            // Should expand partially qualified paths
            parsedArgs = DefaultParse(new[] { @"/out:..\MyBinary.dll", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal("MyBinary", parsedArgs.CompilationName);
            Assert.Equal("MyBinary.dll", parsedArgs.OutputFileName);
            Assert.Equal("MyBinary.dll", parsedArgs.CompilationOptions.ModuleName);
            Assert.Equal(@"C:\abc\def", parsedArgs.OutputDirectory);

            // not specified: exe
            parsedArgs = DefaultParse(new[] { @"a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Null(parsedArgs.CompilationName);
            Assert.Null(parsedArgs.OutputFileName);
            Assert.Null(parsedArgs.CompilationOptions.ModuleName);
            Assert.Equal(baseDirectory, parsedArgs.OutputDirectory);

            // not specified: dll
            parsedArgs = DefaultParse(new[] { @"/target:library", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal("a", parsedArgs.CompilationName);
            Assert.Equal("a.dll", parsedArgs.OutputFileName);
            Assert.Equal("a.dll", parsedArgs.CompilationOptions.ModuleName);
            Assert.Equal(baseDirectory, parsedArgs.OutputDirectory);

            // not specified: module
            parsedArgs = DefaultParse(new[] { @"/target:module", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Null(parsedArgs.CompilationName);
            Assert.Equal("a.netmodule", parsedArgs.CompilationOptions.ModuleName);
            Assert.Equal(baseDirectory, parsedArgs.OutputDirectory);

            // not specified: appcontainerexe
            parsedArgs = DefaultParse(new[] { @"/target:appcontainerexe", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Null(parsedArgs.CompilationName);
            Assert.Null(parsedArgs.OutputFileName);
            Assert.Null(parsedArgs.CompilationOptions.ModuleName);
            Assert.Equal(baseDirectory, parsedArgs.OutputDirectory);

            // not specified: winmdobj
            parsedArgs = DefaultParse(new[] { @"/target:winmdobj", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal("a", parsedArgs.CompilationName);
            Assert.Equal("a.winmdobj", parsedArgs.OutputFileName);
            Assert.Equal("a.winmdobj", parsedArgs.CompilationOptions.ModuleName);
            Assert.Equal(baseDirectory, parsedArgs.OutputDirectory);

            // drive-relative path:
            char currentDrive = Directory.GetCurrentDirectory()[0];
            parsedArgs = DefaultParse(new[] { currentDrive + @":a.cs", "b.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2021: File name 'D:a.cs' is contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments(currentDrive + ":a.cs"));

            Assert.Null(parsedArgs.CompilationName);
            Assert.Null(parsedArgs.OutputFileName);
            Assert.Null(parsedArgs.CompilationOptions.ModuleName);
            Assert.Equal(baseDirectory, parsedArgs.OutputDirectory);

            // UNC
            parsedArgs = DefaultParse(new[] { @"/out:\\b", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2021: File name '.x' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments(@"\\b"));

            Assert.Null(parsedArgs.OutputFileName);
            Assert.Null(parsedArgs.CompilationName);
            Assert.Null(parsedArgs.CompilationOptions.ModuleName);

            parsedArgs = DefaultParse(new[] { @"/out:\\server\share\file.exe", "a.vb" }, baseDirectory);
            parsedArgs.Errors.Verify();

            Assert.Equal(@"\\server\share", parsedArgs.OutputDirectory);
            Assert.Equal("file.exe", parsedArgs.OutputFileName);
            Assert.Equal("file", parsedArgs.CompilationName);
            Assert.Equal("file.exe", parsedArgs.CompilationOptions.ModuleName);

            // invalid name:
            parsedArgs = DefaultParse(new[] { "/out:a.b\0b", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2021: File name '.x' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments("a.b\0b"));

            Assert.Null(parsedArgs.OutputFileName);
            Assert.Null(parsedArgs.CompilationName);
            Assert.Null(parsedArgs.CompilationOptions.ModuleName);

            // Temporary skip following scenarios because of the error message changed (path)
            //parsedArgs = DefaultParse(new[] { "/out:a\uD800b.dll", "a.cs" }, baseDirectory);
            //parsedArgs.Errors.Verify(
            //    // error CS2021: File name '.x' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
            //    Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments("a\uD800b.dll"));

            // Dev11 reports CS0016: Could not write to output file 'd:\Temp\q\a<>.z'
            parsedArgs = DefaultParse(new[] { @"/out:""a<>.dll""", "a.vb" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2021: File name 'a<>.dll' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments("a<>.dll"));

            Assert.Null(parsedArgs.OutputFileName);
            Assert.Null(parsedArgs.CompilationName);
            Assert.Null(parsedArgs.CompilationOptions.ModuleName);

            parsedArgs = DefaultParse(new[] { @"/out:.exe", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2021: File name '.exe' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments(".exe")
                );

            Assert.Null(parsedArgs.OutputFileName);
            Assert.Null(parsedArgs.CompilationName);
            Assert.Null(parsedArgs.CompilationOptions.ModuleName);

            parsedArgs = DefaultParse(new[] { @"/t:exe", @"/out:.exe", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2021: File name '.exe' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments(".exe")
                );

            Assert.Null(parsedArgs.OutputFileName);
            Assert.Null(parsedArgs.CompilationName);
            Assert.Null(parsedArgs.CompilationOptions.ModuleName);

            parsedArgs = DefaultParse(new[] { @"/t:library", @"/out:.dll", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2021: File name '.dll' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments(".dll")
                );

            Assert.Null(parsedArgs.OutputFileName);
            Assert.Null(parsedArgs.CompilationName);
            Assert.Null(parsedArgs.CompilationOptions.ModuleName);

            parsedArgs = DefaultParse(new[] { @"/t:module", @"/out:.netmodule", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2021: File name '.netmodule' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments(".netmodule")
                );

            Assert.Null(parsedArgs.OutputFileName);
            Assert.Null(parsedArgs.CompilationName);
            Assert.Null(parsedArgs.CompilationOptions.ModuleName);

            parsedArgs = DefaultParse(new[] { ".cs" }, baseDirectory);
            parsedArgs.Errors.Verify();

            Assert.Null(parsedArgs.OutputFileName);
            Assert.Null(parsedArgs.CompilationName);
            Assert.Null(parsedArgs.CompilationOptions.ModuleName);

            parsedArgs = DefaultParse(new[] { @"/t:exe", ".cs" }, baseDirectory);
            parsedArgs.Errors.Verify();

            Assert.Null(parsedArgs.OutputFileName);
            Assert.Null(parsedArgs.CompilationName);
            Assert.Null(parsedArgs.CompilationOptions.ModuleName);

            parsedArgs = DefaultParse(new[] { @"/t:library", ".cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2021: File name '.dll' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments(".dll")
                );

            Assert.Null(parsedArgs.OutputFileName);
            Assert.Null(parsedArgs.CompilationName);
            Assert.Null(parsedArgs.CompilationOptions.ModuleName);

            parsedArgs = DefaultParse(new[] { @"/t:module", ".cs" }, baseDirectory);
            parsedArgs.Errors.Verify();

            Assert.Equal(".netmodule", parsedArgs.OutputFileName);
            Assert.Null(parsedArgs.CompilationName);
            Assert.Equal(".netmodule", parsedArgs.CompilationOptions.ModuleName);
        }

        [WorkItem(546012, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546012")]
        [WorkItem(546007, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546007")]
        [Fact]
        public void ParseOut2()
        {
            var parsedArgs = DefaultParse(new[] { "/out:.x", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2021: File name '.x' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments(".x"));

            Assert.Null(parsedArgs.OutputFileName);
            Assert.Null(parsedArgs.CompilationName);
            Assert.Null(parsedArgs.CompilationOptions.ModuleName);

            parsedArgs = DefaultParse(new[] { "/out:.x", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2021: File name '.x' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments(".x"));

            Assert.Null(parsedArgs.OutputFileName);
            Assert.Null(parsedArgs.CompilationName);
            Assert.Null(parsedArgs.CompilationOptions.ModuleName);
        }

        [Fact]
        public void ParseInstrumentTestNames()
        {
            var parsedArgs = DefaultParse(SpecializedCollections.EmptyEnumerable<string>(), WorkingDirectory);
            Assert.True(parsedArgs.EmitOptions.InstrumentationKinds.SequenceEqual(ImmutableArray<InstrumentationKind>.Empty));

            parsedArgs = DefaultParse(new[] { @"/instrument", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing '<text>' for 'instrument' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "instrument"));
            Assert.True(parsedArgs.EmitOptions.InstrumentationKinds.SequenceEqual(ImmutableArray<InstrumentationKind>.Empty));

            parsedArgs = DefaultParse(new[] { @"/instrument:""""", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing '<text>' for 'instrument' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "instrument"));
            Assert.True(parsedArgs.EmitOptions.InstrumentationKinds.SequenceEqual(ImmutableArray<InstrumentationKind>.Empty));

            parsedArgs = DefaultParse(new[] { @"/instrument:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing '<text>' for 'instrument' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "instrument"));
            Assert.True(parsedArgs.EmitOptions.InstrumentationKinds.SequenceEqual(ImmutableArray<InstrumentationKind>.Empty));

            parsedArgs = DefaultParse(new[] { "/instrument:", "Test.Flag.Name", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing '<text>' for 'instrument' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "instrument"));
            Assert.True(parsedArgs.EmitOptions.InstrumentationKinds.SequenceEqual(ImmutableArray<InstrumentationKind>.Empty));

            parsedArgs = DefaultParse(new[] { "/instrument:InvalidOption", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                Diagnostic(ErrorCode.ERR_InvalidInstrumentationKind).WithArguments("InvalidOption"));
            Assert.True(parsedArgs.EmitOptions.InstrumentationKinds.SequenceEqual(ImmutableArray<InstrumentationKind>.Empty));

            parsedArgs = DefaultParse(new[] { "/instrument:None", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                Diagnostic(ErrorCode.ERR_InvalidInstrumentationKind).WithArguments("None"));
            Assert.True(parsedArgs.EmitOptions.InstrumentationKinds.SequenceEqual(ImmutableArray<InstrumentationKind>.Empty));

            parsedArgs = DefaultParse(new[] { "/instrument:TestCoverage,InvalidOption", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                Diagnostic(ErrorCode.ERR_InvalidInstrumentationKind).WithArguments("InvalidOption"));
            Assert.True(parsedArgs.EmitOptions.InstrumentationKinds.SequenceEqual(ImmutableArray.Create(InstrumentationKind.TestCoverage)));

            parsedArgs = DefaultParse(new[] { "/instrument:TestCoverage", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.EmitOptions.InstrumentationKinds.SequenceEqual(ImmutableArray.Create(InstrumentationKind.TestCoverage)));

            parsedArgs = DefaultParse(new[] { @"/instrument:""TestCoverage""", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.EmitOptions.InstrumentationKinds.SequenceEqual(ImmutableArray.Create(InstrumentationKind.TestCoverage)));

            parsedArgs = DefaultParse(new[] { @"/instrument:""TESTCOVERAGE""", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.EmitOptions.InstrumentationKinds.SequenceEqual(ImmutableArray.Create(InstrumentationKind.TestCoverage)));

            parsedArgs = DefaultParse(new[] { "/instrument:TestCoverage,TestCoverage", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.EmitOptions.InstrumentationKinds.SequenceEqual(ImmutableArray.Create(InstrumentationKind.TestCoverage)));

            parsedArgs = DefaultParse(new[] { "/instrument:TestCoverage", "/instrument:TestCoverage", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.EmitOptions.InstrumentationKinds.SequenceEqual(ImmutableArray.Create(InstrumentationKind.TestCoverage)));
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/30289")]
        public void ParseDoc()
        {
            const string baseDirectory = @"C:\abc\def\baz";

            var parsedArgs = DefaultParse(new[] { @"/doc:""""", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing '<text>' for '/doc:' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "/doc:"));
            Assert.Null(parsedArgs.DocumentationPath);

            parsedArgs = DefaultParse(new[] { @"/doc:", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing '<text>' for '/doc:' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "/doc:"));
            Assert.Null(parsedArgs.DocumentationPath);

            // NOTE: no colon in error message '/doc'
            parsedArgs = DefaultParse(new[] { @"/doc", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing '<text>' for '/doc' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "/doc"));
            Assert.Null(parsedArgs.DocumentationPath);

            parsedArgs = DefaultParse(new[] { @"/doc+", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/doc+"));
            Assert.Null(parsedArgs.DocumentationPath);

            // Should preserve fully qualified paths
            parsedArgs = DefaultParse(new[] { @"/doc:C:\MyFolder\MyBinary.xml", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(@"C:\MyFolder\MyBinary.xml", parsedArgs.DocumentationPath);
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode);

            // Should handle quotes
            parsedArgs = DefaultParse(new[] { @"/doc:""C:\My Folder\MyBinary.xml""", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(@"C:\My Folder\MyBinary.xml", parsedArgs.DocumentationPath);
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode);

            // Should expand partially qualified paths
            parsedArgs = DefaultParse(new[] { @"/doc:MyBinary.xml", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(Path.Combine(baseDirectory, "MyBinary.xml"), parsedArgs.DocumentationPath);
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode);

            // Should expand partially qualified paths
            parsedArgs = DefaultParse(new[] { @"/doc:..\MyBinary.xml", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(@"C:\abc\def\MyBinary.xml", parsedArgs.DocumentationPath);
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode);

            // drive-relative path:
            char currentDrive = Directory.GetCurrentDirectory()[0];
            parsedArgs = DefaultParse(new[] { "/doc:" + currentDrive + @":a.xml", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2021: File name 'D:a.xml' is contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments(currentDrive + ":a.xml"));

            Assert.Null(parsedArgs.DocumentationPath);
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode); //Even though the format was incorrect

            // UNC
            parsedArgs = DefaultParse(new[] { @"/doc:\\b", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments(@"\\b"));

            Assert.Null(parsedArgs.DocumentationPath);
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode); //Even though the format was incorrect

            parsedArgs = DefaultParse(new[] { @"/doc:\\server\share\file.xml", "a.vb" }, baseDirectory);
            parsedArgs.Errors.Verify();

            Assert.Equal(@"\\server\share\file.xml", parsedArgs.DocumentationPath);
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode);

            // invalid name:
            parsedArgs = DefaultParse(new[] { "/doc:a.b\0b", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments("a.b\0b"));

            Assert.Null(parsedArgs.DocumentationPath);
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode); //Even though the format was incorrect

            // Temp
            // parsedArgs = DefaultParse(new[] { "/doc:a\uD800b.xml", "a.cs" }, baseDirectory);
            // parsedArgs.Errors.Verify(
            //    Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments("a\uD800b.xml"));

            // Assert.Null(parsedArgs.DocumentationPath);
            // Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode); //Even though the format was incorrect

            parsedArgs = DefaultParse(new[] { @"/doc:""a<>.xml""", "a.vb" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2021: File name 'a<>.xml' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments("a<>.xml"));

            Assert.Null(parsedArgs.DocumentationPath);
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode); //Even though the format was incorrect
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/30289")]
        public void ParseErrorLog()
        {
            const string baseDirectory = @"C:\abc\def\baz";

            var parsedArgs = DefaultParse(new[] { @"/errorlog:""""", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing '<(error log option format>' for '/errorlog:' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments(CSharpCommandLineParser.ErrorLogOptionFormat, "/errorlog:"));
            Assert.Null(parsedArgs.ErrorLogOptions);
            Assert.False(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics);

            parsedArgs = DefaultParse(new[] { @"/errorlog:", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing '<(error log option format>' for '/errorlog:' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments(CSharpCommandLineParser.ErrorLogOptionFormat, "/errorlog:"));
            Assert.Null(parsedArgs.ErrorLogOptions);
            Assert.False(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics);

            parsedArgs = DefaultParse(new[] { @"/errorlog", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing '<(error log option format>' for '/errorlog' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments(CSharpCommandLineParser.ErrorLogOptionFormat, "/errorlog"));
            Assert.Null(parsedArgs.ErrorLogOptions);
            Assert.False(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics);

            // Should preserve fully qualified paths
            parsedArgs = DefaultParse(new[] { @"/errorlog:C:\MyFolder\MyBinary.xml", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(@"C:\MyFolder\MyBinary.xml", parsedArgs.ErrorLogOptions.Path);
            Assert.True(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics);

            // Escaped quote in the middle is an error
            parsedArgs = DefaultParse(new[] { @"/errorlog:C:\""My Folder""\MyBinary.xml", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                 Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments(@"C:""My Folder\MyBinary.xml").WithLocation(1, 1));

            // Should handle quotes
            parsedArgs = DefaultParse(new[] { @"/errorlog:""C:\My Folder\MyBinary.xml""", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(@"C:\My Folder\MyBinary.xml", parsedArgs.ErrorLogOptions.Path);
            Assert.True(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics);

            // Should expand partially qualified paths
            parsedArgs = DefaultParse(new[] { @"/errorlog:MyBinary.xml", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(Path.Combine(baseDirectory, "MyBinary.xml"), parsedArgs.ErrorLogOptions.Path);
            Assert.True(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics);

            // Should expand partially qualified paths
            parsedArgs = DefaultParse(new[] { @"/errorlog:..\MyBinary.xml", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(@"C:\abc\def\MyBinary.xml", parsedArgs.ErrorLogOptions.Path);
            Assert.True(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics);

            // drive-relative path:
            char currentDrive = Directory.GetCurrentDirectory()[0];
            parsedArgs = DefaultParse(new[] { "/errorlog:" + currentDrive + @":a.xml", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2021: File name 'D:a.xml' is contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments(currentDrive + ":a.xml"));

            Assert.Null(parsedArgs.ErrorLogOptions);
            Assert.False(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics);

            // UNC
            parsedArgs = DefaultParse(new[] { @"/errorlog:\\b", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments(@"\\b"));

            Assert.Null(parsedArgs.ErrorLogOptions);
            Assert.False(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics);

            parsedArgs = DefaultParse(new[] { @"/errorlog:\\server\share\file.xml", "a.vb" }, baseDirectory);
            parsedArgs.Errors.Verify();

            Assert.Equal(@"\\server\share\file.xml", parsedArgs.ErrorLogOptions.Path);

            // invalid name:
            parsedArgs = DefaultParse(new[] { "/errorlog:a.b\0b", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments("a.b\0b"));

            Assert.Null(parsedArgs.ErrorLogOptions);
            Assert.False(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics);

            parsedArgs = DefaultParse(new[] { @"/errorlog:""a<>.xml""", "a.vb" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2021: File name 'a<>.xml' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments("a<>.xml"));

            Assert.Null(parsedArgs.ErrorLogOptions);
            Assert.False(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics);

            // Parses SARIF version.
            parsedArgs = DefaultParse(new[] { @"/errorlog:C:\MyFolder\MyBinary.xml,version=2", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(@"C:\MyFolder\MyBinary.xml", parsedArgs.ErrorLogOptions.Path);
            Assert.Equal(SarifVersion.Sarif2, parsedArgs.ErrorLogOptions.SarifVersion);
            Assert.True(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics);

            // Invalid SARIF version.
            string[] invalidSarifVersions = new string[] { @"C:\MyFolder\MyBinary.xml,version=1.0.0", @"C:\MyFolder\MyBinary.xml,version=2.1.0", @"C:\MyFolder\MyBinary.xml,version=42" };

            foreach (string invalidSarifVersion in invalidSarifVersions)
            {
                parsedArgs = DefaultParse(new[] { $"/errorlog:{invalidSarifVersion}", "a.cs" }, baseDirectory);
                parsedArgs.Errors.Verify(
                    // error CS2046: Command-line syntax error: 'C:\MyFolder\MyBinary.xml,version=42' is not a valid value for the '/errorlog:' option. The value must be of the form '<file>[,version={1|1.0|2|2.1}]'.
                    Diagnostic(ErrorCode.ERR_BadSwitchValue).WithArguments(invalidSarifVersion, "/errorlog:", CSharpCommandLineParser.ErrorLogOptionFormat));
                Assert.Null(parsedArgs.ErrorLogOptions);
                Assert.False(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics);
            }

            // Invalid errorlog qualifier.
            const string InvalidErrorLogQualifier = @"C:\MyFolder\MyBinary.xml,invalid=42";
            parsedArgs = DefaultParse(new[] { $"/errorlog:{InvalidErrorLogQualifier}", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2046: Command-line syntax error: 'C:\MyFolder\MyBinary.xml,invalid=42' is not a valid value for the '/errorlog:' option. The value must be of the form '<file>[,version={1|1.0|2|2.1}]'.
                Diagnostic(ErrorCode.ERR_BadSwitchValue).WithArguments(InvalidErrorLogQualifier, "/errorlog:", CSharpCommandLineParser.ErrorLogOptionFormat));
            Assert.Null(parsedArgs.ErrorLogOptions);
            Assert.False(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics);

            // Too many errorlog qualifiers.
            const string TooManyErrorLogQualifiers = @"C:\MyFolder\MyBinary.xml,version=2,version=2";
            parsedArgs = DefaultParse(new[] { $"/errorlog:{TooManyErrorLogQualifiers}", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2046: Command-line syntax error: 'C:\MyFolder\MyBinary.xml,version=2,version=2' is not a valid value for the '/errorlog:' option. The value must be of the form '<file>[,version={1|1.0|2|2.1}]'.
                Diagnostic(ErrorCode.ERR_BadSwitchValue).WithArguments(TooManyErrorLogQualifiers, "/errorlog:", CSharpCommandLineParser.ErrorLogOptionFormat));
            Assert.Null(parsedArgs.ErrorLogOptions);
            Assert.False(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void AppConfigParse()
        {
            const string baseDirectory = @"C:\abc\def\baz";

            var parsedArgs = DefaultParse(new[] { @"/appconfig:""""", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing ':<text>' for '/appconfig:' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments(":<text>", "/appconfig:"));
            Assert.Null(parsedArgs.AppConfigPath);

            parsedArgs = DefaultParse(new[] { "/appconfig:", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing ':<text>' for '/appconfig:' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments(":<text>", "/appconfig:"));
            Assert.Null(parsedArgs.AppConfigPath);

            parsedArgs = DefaultParse(new[] { "/appconfig", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing ':<text>' for '/appconfig' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments(":<text>", "/appconfig"));
            Assert.Null(parsedArgs.AppConfigPath);

            parsedArgs = DefaultParse(new[] { "/appconfig:a.exe.config", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(@"C:\abc\def\baz\a.exe.config", parsedArgs.AppConfigPath);

            // If ParseDoc succeeds, all other possible AppConfig paths should succeed as well -- they both call ParseGenericFilePath
        }

        [Fact]
        public void AppConfigBasic()
        {
            var srcFile = Temp.CreateFile().WriteAllText(@"class A { static void Main(string[] args) { } }");
            var srcDirectory = Path.GetDirectoryName(srcFile.Path);
            var appConfigFile = Temp.CreateFile().WriteAllText(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
       <supportPortability PKT=""7cec85d7bea7798e"" enable=""false""/>
    </assemblyBinding>
  </runtime>
</configuration>");

            var silverlight = Temp.CreateFile().WriteAllBytes(Silverlight.System).Path;
            var net4_0dll = Temp.CreateFile().WriteAllBytes(Net461.Resources.System).Path;

            // Test linking two appconfig dlls with simple src
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = CreateCSharpCompiler(null, srcDirectory,
                new[] { "/nologo",
                        "/r:" + silverlight,
                        "/r:" + net4_0dll,
                        "/appconfig:" + appConfigFile.Path,
                        srcFile.Path }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString().Trim());

            CleanupAllGeneratedFiles(srcFile.Path);
            CleanupAllGeneratedFiles(appConfigFile.Path);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void AppConfigBasicFail()
        {
            var srcFile = Temp.CreateFile().WriteAllText(@"class A { static void Main(string[] args) { } }");
            var srcDirectory = Path.GetDirectoryName(srcFile.Path);
            string root = Path.GetPathRoot(srcDirectory); // Make sure we pick a drive that exists and is plugged in to avoid 'Drive not ready'

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = CreateCSharpCompiler(null, srcDirectory,
                new[] { "/nologo", "/preferreduilang:en",
                        $@"/appconfig:{root}DoesNotExist\NOwhere\bonobo.exe.config" ,
                        srcFile.Path }).Run(outWriter);
            Assert.NotEqual(0, exitCode);
            Assert.Equal($@"error CS7093: Cannot read config file '{root}DoesNotExist\NOwhere\bonobo.exe.config' -- 'Could not find a part of the path '{root}DoesNotExist\NOwhere\bonobo.exe.config'.'", outWriter.ToString().Trim());

            CleanupAllGeneratedFiles(srcFile.Path);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void ParseDocAndOut()
        {
            const string baseDirectory = @"C:\abc\def\baz";

            // Can specify separate directories for binary and XML output.
            var parsedArgs = DefaultParse(new[] { @"/doc:a\b.xml", @"/out:c\d.exe", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();

            Assert.Equal(@"C:\abc\def\baz\a\b.xml", parsedArgs.DocumentationPath);

            Assert.Equal(@"C:\abc\def\baz\c", parsedArgs.OutputDirectory);
            Assert.Equal("d.exe", parsedArgs.OutputFileName);

            // XML does not fall back on output directory.
            parsedArgs = DefaultParse(new[] { @"/doc:b.xml", @"/out:c\d.exe", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();

            Assert.Equal(@"C:\abc\def\baz\b.xml", parsedArgs.DocumentationPath);

            Assert.Equal(@"C:\abc\def\baz\c", parsedArgs.OutputDirectory);
            Assert.Equal("d.exe", parsedArgs.OutputFileName);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void ParseErrorLogAndOut()
        {
            const string baseDirectory = @"C:\abc\def\baz";

            // Can specify separate directories for binary and error log output.
            var parsedArgs = DefaultParse(new[] { @"/errorlog:a\b.xml", @"/out:c\d.exe", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();

            Assert.Equal(@"C:\abc\def\baz\a\b.xml", parsedArgs.ErrorLogOptions.Path);

            Assert.Equal(@"C:\abc\def\baz\c", parsedArgs.OutputDirectory);
            Assert.Equal("d.exe", parsedArgs.OutputFileName);

            // XML does not fall back on output directory.
            parsedArgs = DefaultParse(new[] { @"/errorlog:b.xml", @"/out:c\d.exe", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();

            Assert.Equal(@"C:\abc\def\baz\b.xml", parsedArgs.ErrorLogOptions.Path);

            Assert.Equal(@"C:\abc\def\baz\c", parsedArgs.OutputDirectory);
            Assert.Equal("d.exe", parsedArgs.OutputFileName);
        }

        [Fact]
        public void ModuleAssemblyName()
        {
            var parsedArgs = DefaultParse(new[] { @"/target:module", "/moduleassemblyname:goo", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal("goo", parsedArgs.CompilationName);
            Assert.Equal("a.netmodule", parsedArgs.CompilationOptions.ModuleName);

            parsedArgs = DefaultParse(new[] { @"/target:library", "/moduleassemblyname:goo", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS0734: The /moduleassemblyname option may only be specified when building a target type of 'module'
                Diagnostic(ErrorCode.ERR_AssemblyNameOnNonModule));

            parsedArgs = DefaultParse(new[] { @"/target:exe", "/moduleassemblyname:goo", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS0734: The /moduleassemblyname option may only be specified when building a target type of 'module'
                Diagnostic(ErrorCode.ERR_AssemblyNameOnNonModule));

            parsedArgs = DefaultParse(new[] { @"/target:winexe", "/moduleassemblyname:goo", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS0734: The /moduleassemblyname option may only be specified when building a target type of 'module'
                Diagnostic(ErrorCode.ERR_AssemblyNameOnNonModule));
        }

        [Fact]
        public void ModuleName()
        {
            var parsedArgs = DefaultParse(new[] { @"/target:module", "/modulename:goo", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal("goo", parsedArgs.CompilationOptions.ModuleName);

            parsedArgs = DefaultParse(new[] { @"/target:library", "/modulename:bar", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal("bar", parsedArgs.CompilationOptions.ModuleName);

            parsedArgs = DefaultParse(new[] { @"/target:exe", "/modulename:CommonLanguageRuntimeLibrary", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal("CommonLanguageRuntimeLibrary", parsedArgs.CompilationOptions.ModuleName);

            parsedArgs = DefaultParse(new[] { @"/target:winexe", "/modulename:goo", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal("goo", parsedArgs.CompilationOptions.ModuleName);

            parsedArgs = DefaultParse(new[] { @"/target:exe", "/modulename:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing '<text>' for 'modulename' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "modulename").WithLocation(1, 1)
                );
        }

        [Fact]
        public void ModuleName001()
        {
            var dir = Temp.CreateDirectory();

            var file1 = dir.CreateFile("a.cs");
            file1.WriteAllText(@"
                    class c1
                    {
                        public static void Main(){}
                    }
                ");

            var exeName = "aa.exe";
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(null, dir.Path, new[] { "/modulename:hocusPocus ", "/out:" + exeName + " ", file1.Path });
            int exitCode = csc.Run(outWriter);
            if (exitCode != 0)
            {
                Console.WriteLine(outWriter.ToString());
                Assert.Equal(0, exitCode);
            }

            Assert.Equal(1, Directory.EnumerateFiles(dir.Path, exeName).Count());

            using (var metadata = ModuleMetadata.CreateFromImage(File.ReadAllBytes(Path.Combine(dir.Path, "aa.exe"))))
            {
                var peReader = metadata.Module.GetMetadataReader();

                Assert.True(peReader.IsAssembly);

                Assert.Equal("aa", peReader.GetString(peReader.GetAssemblyDefinition().Name));
                Assert.Equal("hocusPocus", peReader.GetString(peReader.GetModuleDefinition().Name));
            }

            if (System.IO.File.Exists(exeName))
            {
                System.IO.File.Delete(exeName);
            }

            CleanupAllGeneratedFiles(file1.Path);
        }

        [Fact]
        public void ParsePlatform()
        {
            var parsedArgs = DefaultParse(new[] { @"/platform:x64", "a.cs" }, WorkingDirectory);
            Assert.False(parsedArgs.Errors.Any());
            Assert.Equal(Platform.X64, parsedArgs.CompilationOptions.Platform);

            parsedArgs = DefaultParse(new[] { @"/platform:X86", "a.cs" }, WorkingDirectory);
            Assert.False(parsedArgs.Errors.Any());
            Assert.Equal(Platform.X86, parsedArgs.CompilationOptions.Platform);

            parsedArgs = DefaultParse(new[] { @"/platform:itanum", "a.cs" }, WorkingDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.ERR_BadPlatformType, parsedArgs.Errors.First().Code);
            Assert.Equal(Platform.AnyCpu, parsedArgs.CompilationOptions.Platform);

            parsedArgs = DefaultParse(new[] { "/platform:itanium", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(Platform.Itanium, parsedArgs.CompilationOptions.Platform);

            parsedArgs = DefaultParse(new[] { "/platform:anycpu", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(Platform.AnyCpu, parsedArgs.CompilationOptions.Platform);

            parsedArgs = DefaultParse(new[] { "/platform:anycpu32bitpreferred", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(Platform.AnyCpu32BitPreferred, parsedArgs.CompilationOptions.Platform);

            parsedArgs = DefaultParse(new[] { "/platform:arm", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(Platform.Arm, parsedArgs.CompilationOptions.Platform);

            parsedArgs = DefaultParse(new[] { "/platform", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing '<string>' for 'platform' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<string>", "/platform"));
            Assert.Equal(Platform.AnyCpu, parsedArgs.CompilationOptions.Platform);  //anycpu is default

            parsedArgs = DefaultParse(new[] { "/platform:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing '<string>' for 'platform' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<string>", "/platform:"));
            Assert.Equal(Platform.AnyCpu, parsedArgs.CompilationOptions.Platform);  //anycpu is default
        }

        [WorkItem(546016, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546016")]
        [WorkItem(545997, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545997")]
        [WorkItem(546019, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546019")]
        [WorkItem(546029, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546029")]
        [Fact]
        public void ParseBaseAddress()
        {
            var parsedArgs = DefaultParse(new[] { @"/baseaddress:x64", "a.cs" }, WorkingDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.ERR_BadBaseNumber, parsedArgs.Errors.First().Code);

            parsedArgs = DefaultParse(new[] { @"/platform:x64", @"/baseaddress:0x8000000000011111", "a.cs" }, WorkingDirectory);
            Assert.False(parsedArgs.Errors.Any());
            Assert.Equal(0x8000000000011111ul, parsedArgs.EmitOptions.BaseAddress);

            parsedArgs = DefaultParse(new[] { @"/platform:x86", @"/baseaddress:0x8000000000011111", "a.cs" }, WorkingDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.ERR_BadBaseNumber, parsedArgs.Errors.First().Code);

            parsedArgs = DefaultParse(new[] { @"/baseaddress:", "a.cs" }, WorkingDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.ERR_SwitchNeedsNumber, parsedArgs.Errors.First().Code);

            parsedArgs = DefaultParse(new[] { @"/baseaddress:-23", "a.cs" }, WorkingDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.ERR_BadBaseNumber, parsedArgs.Errors.First().Code);

            parsedArgs = DefaultParse(new[] { @"/platform:x64", @"/baseaddress:01777777777777777777777", "a.cs" }, WorkingDirectory);
            Assert.Equal(ulong.MaxValue, parsedArgs.EmitOptions.BaseAddress);

            parsedArgs = DefaultParse(new[] { @"/platform:x64", @"/baseaddress:0x0000000100000000", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new[] { @"/platform:x64", @"/baseaddress:0xffff8000", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new[] { "test.cs", "/platform:x86", "/baseaddress:0xffffffff" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadBaseNumber).WithArguments("0xFFFFFFFF"));

            parsedArgs = DefaultParse(new[] { "test.cs", "/platform:x86", "/baseaddress:0xffff8000" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadBaseNumber).WithArguments("0xFFFF8000"));

            parsedArgs = DefaultParse(new[] { "test.cs", "/baseaddress:0xffff8000" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadBaseNumber).WithArguments("0xFFFF8000"));

            parsedArgs = DefaultParse(new[] { "C:\\test.cs", "/platform:x86", "/baseaddress:0xffff7fff" }, WorkingDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new[] { "C:\\test.cs", "/platform:x64", "/baseaddress:0xffff8000" }, WorkingDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new[] { "C:\\test.cs", "/platform:x64", "/baseaddress:0x100000000" }, WorkingDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new[] { "test.cs", "/baseaddress:0xFFFF0000FFFF0000" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadBaseNumber).WithArguments("0xFFFF0000FFFF0000"));

            parsedArgs = DefaultParse(new[] { "C:\\test.cs", "/platform:x64", "/baseaddress:0x10000000000000000" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadBaseNumber).WithArguments("0x10000000000000000"));

            parsedArgs = DefaultParse(new[] { "C:\\test.cs", "/baseaddress:0xFFFF0000FFFF0000" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadBaseNumber).WithArguments("0xFFFF0000FFFF0000"));
        }

        [Fact]
        public void ParseFileAlignment()
        {
            var parsedArgs = DefaultParse(new[] { @"/filealign:x64", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2024: Invalid file section alignment number 'x64'
                Diagnostic(ErrorCode.ERR_InvalidFileAlignment).WithArguments("x64"));

            parsedArgs = DefaultParse(new[] { @"/filealign:0x200", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(0x200, parsedArgs.EmitOptions.FileAlignment);

            parsedArgs = DefaultParse(new[] { @"/filealign:512", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(512, parsedArgs.EmitOptions.FileAlignment);

            parsedArgs = DefaultParse(new[] { @"/filealign:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2035: Command-line syntax error: Missing ':<number>' for 'filealign' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsNumber).WithArguments("filealign"));

            parsedArgs = DefaultParse(new[] { @"/filealign:-23", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2024: Invalid file section alignment number '-23'
                Diagnostic(ErrorCode.ERR_InvalidFileAlignment).WithArguments("-23"));

            parsedArgs = DefaultParse(new[] { @"/filealign:020000", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(8192, parsedArgs.EmitOptions.FileAlignment);

            parsedArgs = DefaultParse(new[] { @"/filealign:0", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2024: Invalid file section alignment number '0'
                Diagnostic(ErrorCode.ERR_InvalidFileAlignment).WithArguments("0"));

            parsedArgs = DefaultParse(new[] { @"/filealign:123", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2024: Invalid file section alignment number '123'
                Diagnostic(ErrorCode.ERR_InvalidFileAlignment).WithArguments("123"));
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void SdkPathAndLibEnvVariable()
        {
            var dir = Temp.CreateDirectory();
            var lib1 = dir.CreateDirectory("lib1");
            var lib2 = dir.CreateDirectory("lib2");
            var lib3 = dir.CreateDirectory("lib3");

            var sdkDirectory = SdkDirectory;
            var parsedArgs = DefaultParse(new[] { @"/lib:lib1", @"/libpath:lib2", @"/libpaths:lib3", "a.cs" }, dir.Path, sdkDirectory: sdkDirectory);
            AssertEx.Equal(new[]
            {
                sdkDirectory,
                lib1.Path,
                lib2.Path,
                lib3.Path
            }, parsedArgs.ReferencePaths);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void SdkPathAndLibEnvVariable_Errors()
        {
            var parsedArgs = DefaultParse(new[] { @"/lib:c:lib2", @"/lib:o:\sdk1", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // warning CS1668: Invalid search path 'c:lib2' specified in '/LIB option' -- 'path is too long or invalid'
                Diagnostic(ErrorCode.WRN_InvalidSearchPathDir).WithArguments(@"c:lib2", "/LIB option", "path is too long or invalid"),
                // warning CS1668: Invalid search path 'o:\sdk1' specified in '/LIB option' -- 'directory does not exist'
                Diagnostic(ErrorCode.WRN_InvalidSearchPathDir).WithArguments(@"o:\sdk1", "/LIB option", "directory does not exist"));

            parsedArgs = DefaultParse(new[] { @"/lib:c:\Windows,o:\Windows;e:;", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // warning CS1668: Invalid search path 'o:\Windows' specified in '/LIB option' -- 'directory does not exist'
                Diagnostic(ErrorCode.WRN_InvalidSearchPathDir).WithArguments(@"o:\Windows", "/LIB option", "directory does not exist"),
                // warning CS1668: Invalid search path 'e:' specified in '/LIB option' -- 'path is too long or invalid'
                Diagnostic(ErrorCode.WRN_InvalidSearchPathDir).WithArguments(@"e:", "/LIB option", "path is too long or invalid"));

            parsedArgs = DefaultParse(new[] { @"/lib:c:\Windows,.\Windows;e;", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // warning CS1668: Invalid search path '.\Windows' specified in '/LIB option' -- 'directory does not exist'
                Diagnostic(ErrorCode.WRN_InvalidSearchPathDir).WithArguments(@".\Windows", "/LIB option", "directory does not exist"),
                // warning CS1668: Invalid search path 'e' specified in '/LIB option' -- 'directory does not exist'
                Diagnostic(ErrorCode.WRN_InvalidSearchPathDir).WithArguments(@"e", "/LIB option", "directory does not exist"));

            parsedArgs = DefaultParse(new[] { @"/lib:c:\Windows,o:\Windows;e:; ; ; ; ", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // warning CS1668: Invalid search path 'o:\Windows' specified in '/LIB option' -- 'directory does not exist'
                Diagnostic(ErrorCode.WRN_InvalidSearchPathDir).WithArguments(@"o:\Windows", "/LIB option", "directory does not exist"),
                // warning CS1668: Invalid search path 'e:' specified in '/LIB option' -- 'path is too long or invalid'
                Diagnostic(ErrorCode.WRN_InvalidSearchPathDir).WithArguments("e:", "/LIB option", "path is too long or invalid"),
                // warning CS1668: Invalid search path ' ' specified in '/LIB option' -- 'path is too long or invalid'
                Diagnostic(ErrorCode.WRN_InvalidSearchPathDir).WithArguments(" ", "/LIB option", "path is too long or invalid"),
                // warning CS1668: Invalid search path ' ' specified in '/LIB option' -- 'path is too long or invalid'
                Diagnostic(ErrorCode.WRN_InvalidSearchPathDir).WithArguments(" ", "/LIB option", "path is too long or invalid"),
                // warning CS1668: Invalid search path ' ' specified in '/LIB option' -- 'path is too long or invalid'
                Diagnostic(ErrorCode.WRN_InvalidSearchPathDir).WithArguments(" ", "/LIB option", "path is too long or invalid"));

            parsedArgs = DefaultParse(new[] { @"/lib", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<path list>", "lib"));

            parsedArgs = DefaultParse(new[] { @"/lib:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<path list>", "lib"));

            parsedArgs = DefaultParse(new[] { @"/lib+", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/lib+"));

            parsedArgs = DefaultParse(new[] { @"/lib: ", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<path list>", "lib"));
        }

        [Fact, WorkItem(546005, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546005")]
        public void SdkPathAndLibEnvVariable_Relative_csc()
        {
            var tempFolder = Temp.CreateDirectory();
            var baseDirectory = tempFolder.ToString();

            var subFolder = tempFolder.CreateDirectory("temp");
            var subDirectory = subFolder.ToString();

            var src = Temp.CreateFile("a.cs");
            src.WriteAllText("public class C{}");

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            int exitCode = CreateCSharpCompiler(null, subDirectory, new[] { "/nologo", "/t:library", "/out:abc.xyz", src.ToString() }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString().Trim());

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = CreateCSharpCompiler(null, baseDirectory, new[] { "/nologo", "/lib:temp", "/r:abc.xyz", "/t:library", src.ToString() }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString().Trim());

            CleanupAllGeneratedFiles(src.Path);
        }

        [Fact]
        public void UnableWriteOutput_OutputFileIsDirectory()
        {
            var tempFolder = Temp.CreateDirectory();
            var baseDirectory = tempFolder.ToString();
            var subFolder = tempFolder.CreateDirectory("temp");

            var src = Temp.CreateFile("a.cs");
            src.WriteAllText("public class C{}");

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            int exitCode = CreateCSharpCompiler(null, baseDirectory, new[] { "/nologo", "/preferreduilang:en", "/t:library", "/out:" + subFolder.ToString(), src.ToString() }).Run(outWriter);
            Assert.Equal(1, exitCode);
            var output = outWriter.ToString().Trim();
            Assert.StartsWith($"error CS2012: Cannot open '{subFolder}' for writing -- ", output); // Cannot create a file when that file already exists.

            CleanupAllGeneratedFiles(src.Path);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void UnableWriteOutput_OutputFileLocked()
        {
            var tempFolder = Temp.CreateDirectory();
            var baseDirectory = tempFolder.ToString();
            var filePath = tempFolder.CreateFile("temp").Path;

            using var _ = new FileStream(filePath, FileMode.Open, FileAccess.Write, FileShare.None);
            var currentProcess = Process.GetCurrentProcess();

            var src = Temp.CreateFile("a.cs");
            src.WriteAllText("public class C{}");

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            int exitCode = CreateCSharpCompiler(responseFile: null, baseDirectory, ["/nologo", "/preferreduilang:en", "/t:library", "/out:" + filePath, src.ToString()]).Run(outWriter);
            Assert.Equal(1, exitCode);
            var output = outWriter.ToString().Trim();

            var pattern = @"error CS2012: Cannot open '(?<path>.*)' for writing -- (?<message>.*); file may be locked by '(?<app>.*)' \((?<pid>.*)\)";
            var match = Regex.Match(output, pattern);
            Assert.True(match.Success, $"Expected pattern:{Environment.NewLine}{pattern}{Environment.NewLine}Actual:{Environment.NewLine}{output}");
            Assert.Equal(filePath, match.Groups["path"].Value);
            Assert.Contains("testhost", match.Groups["app"].Value);
            Assert.Equal(currentProcess.Id, int.Parse(match.Groups["pid"].Value));

            CleanupAllGeneratedFiles(src.Path);
        }

        [Fact]
        public void ParseHighEntropyVA()
        {
            var parsedArgs = DefaultParse(new[] { @"/highentropyva", "a.cs" }, WorkingDirectory);
            Assert.False(parsedArgs.Errors.Any());
            Assert.True(parsedArgs.EmitOptions.HighEntropyVirtualAddressSpace);
            parsedArgs = DefaultParse(new[] { @"/highentropyva+", "a.cs" }, WorkingDirectory);
            Assert.False(parsedArgs.Errors.Any());
            Assert.True(parsedArgs.EmitOptions.HighEntropyVirtualAddressSpace);
            parsedArgs = DefaultParse(new[] { @"/highentropyva-", "a.cs" }, WorkingDirectory);
            Assert.False(parsedArgs.Errors.Any());
            Assert.False(parsedArgs.EmitOptions.HighEntropyVirtualAddressSpace);
            parsedArgs = DefaultParse(new[] { @"/highentropyva:-", "a.cs" }, WorkingDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal(EmitOptions.Default.HighEntropyVirtualAddressSpace, parsedArgs.EmitOptions.HighEntropyVirtualAddressSpace);

            parsedArgs = DefaultParse(new[] { @"/highentropyva:", "a.cs" }, WorkingDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal(EmitOptions.Default.HighEntropyVirtualAddressSpace, parsedArgs.EmitOptions.HighEntropyVirtualAddressSpace);

            //last one wins
            parsedArgs = DefaultParse(new[] { @"/highenTROPyva+", @"/HIGHentropyva-", "a.cs" }, WorkingDirectory);
            Assert.False(parsedArgs.Errors.Any());
            Assert.False(parsedArgs.EmitOptions.HighEntropyVirtualAddressSpace);
        }

        [Fact]
        public void Checked()
        {
            var parsedArgs = DefaultParse(new[] { @"/checked+", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.CompilationOptions.CheckOverflow);

            parsedArgs = DefaultParse(new[] { @"/checked-", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.CompilationOptions.CheckOverflow);

            parsedArgs = DefaultParse(new[] { @"/checked", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.CompilationOptions.CheckOverflow);

            parsedArgs = DefaultParse(new[] { @"/checked-", @"/checked", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.CompilationOptions.CheckOverflow);

            parsedArgs = DefaultParse(new[] { @"/checked:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/checked:"));
        }

        [Fact]
        public void Nullable()
        {
            var parsedArgs = DefaultParse(new[] { "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable+", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Enable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable+", "/langversion:7.0", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8630: Invalid 'nullable' value: 'Enabled' for C# 7.0. Please use language version '8.0' or greater.
                Diagnostic(ErrorCode.ERR_NullableOptionNotAvailable).WithArguments("nullable", "Enable", "7.0", "8.0").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Enable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable-", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Enable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing '<text>' for 'nullable' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "nullable").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:yes", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8636: Invalid option 'yes' for /nullable; must be 'disable', 'enable', 'warnings' or 'annotations'
                Diagnostic(ErrorCode.ERR_BadNullableContextOption).WithArguments("yes").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:enable", "/langversion:7.0", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8630: Invalid 'nullable' value: 'Enable' for C# 7.0. Please use language version '8.0' or greater.
                Diagnostic(ErrorCode.ERR_NullableOptionNotAvailable).WithArguments("nullable", "Enable", "7.0", "8.0").WithLocation(1, 1));
            Assert.Equal(NullableContextOptions.Enable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:disable", "/langversion:7.0", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:safeonly", "/langversion:7.0", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8636: Invalid option 'safeonly' for /nullable; must be 'disable', 'enable', 'warnings' or 'annotations'
                Diagnostic(ErrorCode.ERR_BadNullableContextOption).WithArguments("safeonly").WithLocation(1, 1));

            parsedArgs = DefaultParse(new[] { @"/nullable+", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Enable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable-", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Enable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing '<text>' for 'nullable' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "nullable").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:yes", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8636: Invalid option 'yes' for /nullable; must be 'disable', 'enable', 'warnings' or 'annotations'
                Diagnostic(ErrorCode.ERR_BadNullableContextOption).WithArguments("yes").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:eNable", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Enable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:disablE", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:Safeonly", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8636: Invalid option 'Safeonly' for /nullable; must be 'disable', 'enable', 'warnings' or 'annotations'
                Diagnostic(ErrorCode.ERR_BadNullableContextOption).WithArguments("Safeonly").WithLocation(1, 1)
                );

            parsedArgs = DefaultParse(new[] { @"/nullable-", @"/nullable-", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable-", @"/nullable", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Enable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable-", @"/nullable+", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Enable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable-", @"/nullable:", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing '<text>' for 'nullable' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "nullable").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable-", @"/nullable:YES", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8636: Invalid option 'YES' for /nullable; must be 'disable', 'enable', 'warnings' or 'annotations'
                Diagnostic(ErrorCode.ERR_BadNullableContextOption).WithArguments("YES").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable-", @"/nullable:disable", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable-", @"/nullable:enable", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Enable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable-", @"/nullable:safeonly", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8636: Invalid option 'safeonly' for /nullable; must be 'disable', 'enable', 'warnings' or 'annotations'
                Diagnostic(ErrorCode.ERR_BadNullableContextOption).WithArguments("safeonly").WithLocation(1, 1)
                );

            parsedArgs = DefaultParse(new[] { @"/nullable+", @"/nullable-", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable+", @"/nullable", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Enable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable+", @"/nullable+", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Enable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable+", @"/nullable:", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing '<text>' for 'nullable' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "nullable").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Enable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable+", @"/nullable:YES", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8636: Invalid option 'YES' for /nullable; must be 'disable', 'enable', 'warnings' or 'annotations'
                Diagnostic(ErrorCode.ERR_BadNullableContextOption).WithArguments("YES").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Enable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable+", @"/nullable:disable", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable+", @"/nullable:enable", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Enable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable+", @"/nullable:safeonly", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8636: Invalid option 'safeonly' for /nullable; must be 'disable', 'enable', 'warnings' or 'annotations'
                Diagnostic(ErrorCode.ERR_BadNullableContextOption).WithArguments("safeonly").WithLocation(1, 1)
                );

            parsedArgs = DefaultParse(new[] { @"/nullable:safeonly", @"/nullable-", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8636: Invalid option 'safeonly' for /nullable; must be 'disable', 'enable', 'warnings' or 'annotations'
                Diagnostic(ErrorCode.ERR_BadNullableContextOption).WithArguments("safeonly").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:safeonly", @"/nullable", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8636: Invalid option 'safeonly' for /nullable; must be 'disable', 'enable', 'warnings' or 'annotations'
                Diagnostic(ErrorCode.ERR_BadNullableContextOption).WithArguments("safeonly").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Enable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:safeonly", @"/nullable+", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8636: Invalid option 'safeonly' for /nullable; must be 'disable', 'enable', 'warnings' or 'annotations'
                Diagnostic(ErrorCode.ERR_BadNullableContextOption).WithArguments("safeonly").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Enable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:safeonly", @"/nullable:", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8636: Invalid option 'safeonly' for /nullable; must be 'disable', 'enable', 'warnings' or 'annotations'
                Diagnostic(ErrorCode.ERR_BadNullableContextOption).WithArguments("safeonly").WithLocation(1, 1),
                // error CS2006: Command-line syntax error: Missing '<text>' for 'nullable' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "nullable").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:safeonly", @"/nullable:YES", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8636: Invalid option 'safeonly' for /nullable; must be 'disable', 'enable', 'warnings' or 'annotations'
                Diagnostic(ErrorCode.ERR_BadNullableContextOption).WithArguments("safeonly").WithLocation(1, 1),
                // error CS8636: Invalid option 'YES' for /nullable; must be 'disable', 'enable', 'warnings' or 'annotations'
                Diagnostic(ErrorCode.ERR_BadNullableContextOption).WithArguments("YES").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:safeonly", @"/nullable:disable", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8636: Invalid option 'safeonly' for /nullable; must be 'disable', 'enable', 'warnings' or 'annotations'
                Diagnostic(ErrorCode.ERR_BadNullableContextOption).WithArguments("safeonly").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:safeonly", @"/nullable:enable", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8636: Invalid option 'safeonly' for /nullable; must be 'disable', 'enable', 'warnings' or 'annotations'
                Diagnostic(ErrorCode.ERR_BadNullableContextOption).WithArguments("safeonly").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Enable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:safeonly", @"/nullable:safeonly", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8636: Invalid option 'safeonly' for /nullable; must be 'disable', 'enable', 'warnings' or 'annotations'
                Diagnostic(ErrorCode.ERR_BadNullableContextOption).WithArguments("safeonly").WithLocation(1, 1),
                // error CS8636: Invalid option 'safeonly' for /nullable; must be 'disable', 'enable', 'warnings' or 'annotations'
                Diagnostic(ErrorCode.ERR_BadNullableContextOption).WithArguments("safeonly").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:", "/langversion:7.3", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing '<text>' for 'nullable' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "nullable").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:yeS", "/langversion:7.0", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8636: Invalid option 'yeS' for /nullable; must be 'disable', 'enable', 'warnings' or 'annotations'
                Diagnostic(ErrorCode.ERR_BadNullableContextOption).WithArguments("yeS").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable+", "/langversion:7.3", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8630: Invalid 'nullable' value: 'Enable' for C# 7.3. Please use language version '8.0' or greater.
                Diagnostic(ErrorCode.ERR_NullableOptionNotAvailable).WithArguments("nullable", "Enable", "7.3", "8.0").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Enable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable-", "/langversion:7.0", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable", "/langversion:7.3", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8630: Invalid 'nullable' value: 'Enabled' for C# 7.3. Please use language version '8.0' or greater.
                Diagnostic(ErrorCode.ERR_NullableOptionNotAvailable).WithArguments("nullable", "Enable", "7.3", "8.0").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Enable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:enable", "/langversion:7.3", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8630: Invalid 'nullable' value: 'Enabled' for C# 7.3. Please use language version '8.0' or greater.
                Diagnostic(ErrorCode.ERR_NullableOptionNotAvailable).WithArguments("nullable", "Enable", "7.3", "8.0").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Enable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:disable", "/langversion:7.0", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:safeonly", "/langversion:7.3", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8636: Invalid option 'safeonly' for /nullable; must be 'disable', 'enable', 'warnings' or 'annotations'
                Diagnostic(ErrorCode.ERR_BadNullableContextOption).WithArguments("safeonly").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { "a.cs", "/langversion:8" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { "a.cs", "/langversion:7.3" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:""safeonly""", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8636: Invalid option 'safeonly' for /nullable; must be 'disable', 'enable', 'warnings' or 'annotations'
                Diagnostic(ErrorCode.ERR_BadNullableContextOption).WithArguments("safeonly").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:\""enable\""", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8636: Invalid option '"enable"' for /nullable; must be 'disable', 'enable', 'warnings' or 'annotations'
                Diagnostic(ErrorCode.ERR_BadNullableContextOption).WithArguments("\"enable\"").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:\\disable\\", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8636: Invalid option '\\disable\\' for /nullable; must be 'disable', 'enable', 'warnings' or 'annotations'
                Diagnostic(ErrorCode.ERR_BadNullableContextOption).WithArguments("\\\\disable\\\\").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:\\""enable\\""", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8636: Invalid option '\enable\' for /nullable; must be 'disable', 'enable', 'warnings' or 'annotations'
                Diagnostic(ErrorCode.ERR_BadNullableContextOption).WithArguments("\\enable\\").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:safeonlywarnings", "/langversion:7.0", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8636: Invalid option 'safeonlywarnings' for /nullable; must be 'disable', 'enable', 'warnings' or 'annotations'
                Diagnostic(ErrorCode.ERR_BadNullableContextOption).WithArguments("safeonlywarnings").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:SafeonlyWarnings", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8636: Invalid option 'SafeonlyWarnings' for /nullable; must be 'disable', 'enable', 'warnings' or 'annotations'
                Diagnostic(ErrorCode.ERR_BadNullableContextOption).WithArguments("SafeonlyWarnings").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable-", @"/nullable:safeonlyWarnings", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8636: Invalid option 'safeonlyWarnings' for /nullable; must be 'disable', 'enable', 'warnings' or 'annotations'
                Diagnostic(ErrorCode.ERR_BadNullableContextOption).WithArguments("safeonlyWarnings").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:warnings", "/langversion:7.0", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8630: Invalid 'nullable' value: 'Warnings' for C# 7.0. Please use language version '8.0' or greater.
                Diagnostic(ErrorCode.ERR_NullableOptionNotAvailable).WithArguments("nullable", "Warnings", "7.0", "8.0").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Warnings, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:Warnings", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Warnings, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable-", @"/nullable:Warnings", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Warnings, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable+", @"/nullable:Warnings", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Warnings, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:Warnings", @"/nullable-", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:Warnings", @"/nullable", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Enable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:Warnings", @"/nullable+", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Enable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:Warnings", @"/nullable:", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing '<text>' for 'nullable' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "nullable").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Warnings, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:Warnings", @"/nullable:YES", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8636: Invalid option 'YES' for /nullable; must be 'disable', 'enable', 'warnings' or 'annotations'
                Diagnostic(ErrorCode.ERR_BadNullableContextOption).WithArguments("YES").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Warnings, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:Warnings", @"/nullable:disable", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:Warnings", @"/nullable:enable", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Enable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:Warnings", @"/nullable:Warnings", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Warnings, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:Warnings", "/langversion:7.3", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8630: Invalid 'nullable' value: 'Annotations' for C# 7.3. Please use language version '8.0' or greater.
                Diagnostic(ErrorCode.ERR_NullableOptionNotAvailable).WithArguments("nullable", "Warnings", "7.3", "8.0").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Warnings, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:annotations", "/langversion:7.0", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8630: Invalid 'nullable' value: 'Annotations' for C# 7.0. Please use language version '8.0' or greater.
                Diagnostic(ErrorCode.ERR_NullableOptionNotAvailable).WithArguments("nullable", "Annotations", "7.0", "8.0").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Annotations, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:Annotations", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Annotations, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable-", @"/nullable:Annotations", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Annotations, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable+", @"/nullable:Annotations", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Annotations, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:Annotations", @"/nullable-", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:Annotations", @"/nullable", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Enable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:Annotations", @"/nullable+", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Enable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:Annotations", @"/nullable:", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing '<text>' for 'nullable' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "nullable").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Annotations, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:Annotations", @"/nullable:YES", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8636: Invalid option 'YES' for /nullable; must be 'disable', 'enable', 'warnings' or 'annotations'
                Diagnostic(ErrorCode.ERR_BadNullableContextOption).WithArguments("YES").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Annotations, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:Annotations", @"/nullable:disable", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Disable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:Annotations", @"/nullable:enable", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Enable, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:Annotations", @"/nullable:Annotations", "/langversion:8", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(NullableContextOptions.Annotations, parsedArgs.CompilationOptions.NullableContextOptions);

            parsedArgs = DefaultParse(new[] { @"/nullable:Annotations", "/langversion:7.3", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS8630: Invalid 'nullable' value: 'Annotations' for C# 7.3. Please use language version '8.0' or greater.
                Diagnostic(ErrorCode.ERR_NullableOptionNotAvailable).WithArguments("nullable", "Annotations", "7.3", "8.0").WithLocation(1, 1)
                );
            Assert.Equal(NullableContextOptions.Annotations, parsedArgs.CompilationOptions.NullableContextOptions);
        }

        [Fact]
        public void Usings()
        {
            CSharpCommandLineArguments parsedArgs;

            var sdkDirectory = SdkDirectory;
            parsedArgs = CSharpCommandLineParser.Script.Parse(new string[] { "/u:Goo.Bar" }, WorkingDirectory, sdkDirectory);
            parsedArgs.Errors.Verify();
            AssertEx.Equal(new[] { "Goo.Bar" }, parsedArgs.CompilationOptions.Usings.AsEnumerable());

            parsedArgs = CSharpCommandLineParser.Script.Parse(new string[] { "/u:Goo.Bar;Baz", "/using:System.Core;System" }, WorkingDirectory, sdkDirectory);
            parsedArgs.Errors.Verify();
            AssertEx.Equal(new[] { "Goo.Bar", "Baz", "System.Core", "System" }, parsedArgs.CompilationOptions.Usings.AsEnumerable());

            parsedArgs = CSharpCommandLineParser.Script.Parse(new string[] { "/u:Goo;;Bar" }, WorkingDirectory, sdkDirectory);
            parsedArgs.Errors.Verify();
            AssertEx.Equal(new[] { "Goo", "Bar" }, parsedArgs.CompilationOptions.Usings.AsEnumerable());

            parsedArgs = CSharpCommandLineParser.Script.Parse(new string[] { "/u:" }, WorkingDirectory, sdkDirectory);
            parsedArgs.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing '<namespace>' for '/u:' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<namespace>", "/u:"));
        }

        [Fact]
        public void WarningsErrors()
        {
            var parsedArgs = DefaultParse(new string[] { "/nowarn", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2035: Command-line syntax error: Missing ':<number>' for 'nowarn' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsNumber).WithArguments("nowarn"));

            parsedArgs = DefaultParse(new string[] { "/nowarn:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2035: Command-line syntax error: Missing ':<number>' for 'nowarn' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsNumber).WithArguments("nowarn"));

            // Previous versions of the compiler used to report a warning (CS1691)
            // whenever an unrecognized warning code was supplied via /nowarn or /warnaserror.
            // We no longer generate a warning in such cases.
            parsedArgs = DefaultParse(new string[] { "/nowarn:-1", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new string[] { "/nowarn:abc", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new string[] { "/warnaserror:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2035: Command-line syntax error: Missing ':<number>' for 'warnaserror' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsNumber).WithArguments("warnaserror"));

            parsedArgs = DefaultParse(new string[] { "/warnaserror:-1", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new string[] { "/warnaserror:70000", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new string[] { "/warnaserror:abc", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new string[] { "/warnaserror+:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2035: Command-line syntax error: Missing ':<number>' for '/warnaserror+:' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsNumber).WithArguments("warnaserror+"));

            parsedArgs = DefaultParse(new string[] { "/warnaserror-:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2035: Command-line syntax error: Missing ':<number>' for '/warnaserror-:' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsNumber).WithArguments("warnaserror-"));

            parsedArgs = DefaultParse(new string[] { "/w", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2035: Command-line syntax error: Missing ':<number>' for '/w' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsNumber).WithArguments("w"));

            parsedArgs = DefaultParse(new string[] { "/w:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2035: Command-line syntax error: Missing ':<number>' for '/w:' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsNumber).WithArguments("w"));

            parsedArgs = DefaultParse(new string[] { "/warn:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2035: Command-line syntax error: Missing ':<number>' for '/warn:' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsNumber).WithArguments("warn"));

            parsedArgs = DefaultParse(new string[] { "/w:-1", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS1900: Warning level must be zero or greater
                Diagnostic(ErrorCode.ERR_BadWarningLevel));

            parsedArgs = DefaultParse(new string[] { "/w:5", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new string[] { "/warn:-1", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS1900: Warning level must be zero or greater
                Diagnostic(ErrorCode.ERR_BadWarningLevel));

            parsedArgs = DefaultParse(new string[] { "/warn:5", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();

            // Previous versions of the compiler used to report a warning (CS1691)
            // whenever an unrecognized warning code was supplied via /nowarn or /warnaserror.
            // We no longer generate a warning in such cases.
            parsedArgs = DefaultParse(new string[] { "/warnaserror:1,2,3", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new string[] { "/nowarn:1,2,3", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new string[] { "/nowarn:1;2;;3", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
        }

        private static void AssertSpecificDiagnostics(int[] expectedCodes, ReportDiagnostic[] expectedOptions, CSharpCommandLineArguments args)
        {
            var actualOrdered = args.CompilationOptions.SpecificDiagnosticOptions.OrderBy(entry => entry.Key);

            AssertEx.Equal(
                expectedCodes.Select(i => MessageProvider.Instance.GetIdForErrorCode(i)),
                actualOrdered.Select(entry => entry.Key));

            AssertEx.Equal(expectedOptions, actualOrdered.Select(entry => entry.Value));
        }

        [Fact]
        public void WarningsParse()
        {
            var parsedArgs = DefaultParse(new string[] { "/warnaserror", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ReportDiagnostic.Error, parsedArgs.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(4, parsedArgs.CompilationOptions.WarningLevel);
            Assert.Equal(0, parsedArgs.CompilationOptions.SpecificDiagnosticOptions.Count);

            parsedArgs = DefaultParse(new string[] { "/warnaserror:1062,1066,1734", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(4, parsedArgs.CompilationOptions.WarningLevel);
            AssertSpecificDiagnostics(new[] { 1062, 1066, 1734 }, new[] { ReportDiagnostic.Error, ReportDiagnostic.Error, ReportDiagnostic.Error }, parsedArgs);

            parsedArgs = DefaultParse(new string[] { "/warnaserror:+1062,+1066,+1734", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(4, parsedArgs.CompilationOptions.WarningLevel);
            AssertSpecificDiagnostics(new[] { 1062, 1066, 1734 }, new[] { ReportDiagnostic.Error, ReportDiagnostic.Error, ReportDiagnostic.Error }, parsedArgs);

            parsedArgs = DefaultParse(new string[] { "/warnaserror+", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ReportDiagnostic.Error, parsedArgs.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(4, parsedArgs.CompilationOptions.WarningLevel);
            AssertSpecificDiagnostics(new int[0], new ReportDiagnostic[0], parsedArgs);

            parsedArgs = DefaultParse(new string[] { "/warnaserror+:1062,1066,1734", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(4, parsedArgs.CompilationOptions.WarningLevel);
            AssertSpecificDiagnostics(new[] { 1062, 1066, 1734 }, new[] { ReportDiagnostic.Error, ReportDiagnostic.Error, ReportDiagnostic.Error }, parsedArgs);

            parsedArgs = DefaultParse(new string[] { "/warnaserror-", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(4, parsedArgs.CompilationOptions.WarningLevel);
            AssertSpecificDiagnostics(new int[0], new ReportDiagnostic[0], parsedArgs);

            parsedArgs = DefaultParse(new string[] { "/warnaserror-:1062,1066,1734", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(4, parsedArgs.CompilationOptions.WarningLevel);
            AssertSpecificDiagnostics(new[] { 1062, 1066, 1734 }, new[] { ReportDiagnostic.Default, ReportDiagnostic.Default, ReportDiagnostic.Default }, parsedArgs);

            parsedArgs = DefaultParse(new string[] { "/warnaserror+:1062,1066,1734", "/warnaserror-:1762,1974", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(4, parsedArgs.CompilationOptions.WarningLevel);
            AssertSpecificDiagnostics(
                new[] { 1062, 1066, 1734, 1762, 1974 },
                new[] { ReportDiagnostic.Error, ReportDiagnostic.Error, ReportDiagnostic.Error, ReportDiagnostic.Default, ReportDiagnostic.Default },
                parsedArgs);

            parsedArgs = DefaultParse(new string[] { "/warnaserror+:1062,1066,1734", "/warnaserror-:1062,1974", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(4, parsedArgs.CompilationOptions.WarningLevel);
            Assert.Equal(4, parsedArgs.CompilationOptions.SpecificDiagnosticOptions.Count);
            AssertSpecificDiagnostics(new[] { 1062, 1066, 1734, 1974 }, new[] { ReportDiagnostic.Default, ReportDiagnostic.Error, ReportDiagnostic.Error, ReportDiagnostic.Default }, parsedArgs);

            parsedArgs = DefaultParse(new string[] { "/warnaserror-:1062,1066,1734", "/warnaserror+:1062,1974", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(4, parsedArgs.CompilationOptions.WarningLevel);
            AssertSpecificDiagnostics(new[] { 1062, 1066, 1734, 1974 }, new[] { ReportDiagnostic.Error, ReportDiagnostic.Default, ReportDiagnostic.Default, ReportDiagnostic.Error }, parsedArgs);

            parsedArgs = DefaultParse(new string[] { "/w:1", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(1, parsedArgs.CompilationOptions.WarningLevel);
            AssertSpecificDiagnostics(new int[0], new ReportDiagnostic[0], parsedArgs);

            parsedArgs = DefaultParse(new string[] { "/warn:1", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(1, parsedArgs.CompilationOptions.WarningLevel);
            AssertSpecificDiagnostics(new int[0], new ReportDiagnostic[0], parsedArgs);

            parsedArgs = DefaultParse(new string[] { "/warn:1", "/warnaserror+:1062,1974", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(1, parsedArgs.CompilationOptions.WarningLevel);
            AssertSpecificDiagnostics(new[] { 1062, 1974 }, new[] { ReportDiagnostic.Error, ReportDiagnostic.Error }, parsedArgs);

            parsedArgs = DefaultParse(new string[] { "/nowarn:1062,1066,1734", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(4, parsedArgs.CompilationOptions.WarningLevel);
            AssertSpecificDiagnostics(new[] { 1062, 1066, 1734 }, new[] { ReportDiagnostic.Suppress, ReportDiagnostic.Suppress, ReportDiagnostic.Suppress }, parsedArgs);

            parsedArgs = DefaultParse(new string[] { @"/nowarn:""1062 1066 1734""", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(4, parsedArgs.CompilationOptions.WarningLevel);
            AssertSpecificDiagnostics(new[] { 1062, 1066, 1734 }, new[] { ReportDiagnostic.Suppress, ReportDiagnostic.Suppress, ReportDiagnostic.Suppress }, parsedArgs);

            parsedArgs = DefaultParse(new string[] { "/nowarn:1062,1066,1734", "/warnaserror:1066,1762", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(4, parsedArgs.CompilationOptions.WarningLevel);
            AssertSpecificDiagnostics(new[] { 1062, 1066, 1734, 1762 }, new[] { ReportDiagnostic.Suppress, ReportDiagnostic.Suppress, ReportDiagnostic.Suppress, ReportDiagnostic.Error }, parsedArgs);

            parsedArgs = DefaultParse(new string[] { "/warnaserror:1066,1762", "/nowarn:1062,1066,1734", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(4, parsedArgs.CompilationOptions.WarningLevel);
            AssertSpecificDiagnostics(new[] { 1062, 1066, 1734, 1762 }, new[] { ReportDiagnostic.Suppress, ReportDiagnostic.Suppress, ReportDiagnostic.Suppress, ReportDiagnostic.Error }, parsedArgs);
        }

        [Fact]
        public void AllowUnsafe()
        {
            CSharpCommandLineArguments parsedArgs = DefaultParse(new[] { "/unsafe", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.CompilationOptions.AllowUnsafe);

            parsedArgs = DefaultParse(new[] { "/unsafe+", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.CompilationOptions.AllowUnsafe);

            parsedArgs = DefaultParse(new[] { "/UNSAFE-", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.CompilationOptions.AllowUnsafe);

            parsedArgs = DefaultParse(new[] { "/unsafe-", "/unsafe+", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.CompilationOptions.AllowUnsafe);

            parsedArgs = DefaultParse(new[] { "a.cs" }, WorkingDirectory); // default
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.CompilationOptions.AllowUnsafe);

            parsedArgs = DefaultParse(new[] { "/unsafe:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/unsafe:"));

            parsedArgs = DefaultParse(new[] { "/unsafe:+", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/unsafe:+"));

            parsedArgs = DefaultParse(new[] { "/unsafe-:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/unsafe-:"));
        }

        [Fact]
        public void DelaySign()
        {
            CSharpCommandLineArguments parsedArgs;

            parsedArgs = DefaultParse(new[] { "/delaysign", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.NotNull(parsedArgs.CompilationOptions.DelaySign);
            Assert.True((bool)parsedArgs.CompilationOptions.DelaySign);

            parsedArgs = DefaultParse(new[] { "/delaysign+", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.NotNull(parsedArgs.CompilationOptions.DelaySign);
            Assert.True((bool)parsedArgs.CompilationOptions.DelaySign);

            parsedArgs = DefaultParse(new[] { "/DELAYsign-", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.NotNull(parsedArgs.CompilationOptions.DelaySign);
            Assert.False((bool)parsedArgs.CompilationOptions.DelaySign);

            parsedArgs = DefaultParse(new[] { "/delaysign:-", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2007: Unrecognized option: '/delaysign:-'
                Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/delaysign:-"));

            Assert.Null(parsedArgs.CompilationOptions.DelaySign);
        }

        [Fact]
        public void PublicSign()
        {
            var parsedArgs = DefaultParse(new[] { "/publicsign", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.CompilationOptions.PublicSign);

            parsedArgs = DefaultParse(new[] { "/publicsign+", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.CompilationOptions.PublicSign);

            parsedArgs = DefaultParse(new[] { "/PUBLICsign-", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.CompilationOptions.PublicSign);

            parsedArgs = DefaultParse(new[] { "/publicsign:-", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2007: Unrecognized option: '/publicsign:-'
                Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/publicsign:-").WithLocation(1, 1));

            Assert.False(parsedArgs.CompilationOptions.PublicSign);
        }

        [WorkItem(8360, "https://github.com/dotnet/roslyn/issues/8360")]
        [Fact]
        public void PublicSign_KeyFileRelativePath()
        {
            var parsedArgs = DefaultParse(new[] { "/publicsign", "/keyfile:test.snk", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(Path.Combine(WorkingDirectory, "test.snk"), parsedArgs.CompilationOptions.CryptoKeyFile);
        }

        [Fact]
        [WorkItem(11497, "https://github.com/dotnet/roslyn/issues/11497")]
        public void PublicSignWithEmptyKeyPath()
        {
            DefaultParse(new[] { "/publicsign", "/keyfile:", "a.cs" }, WorkingDirectory).Errors.Verify(
                // error CS2005: Missing file specification for 'keyfile' option
                Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("keyfile").WithLocation(1, 1));
        }

        [Fact]
        [WorkItem(11497, "https://github.com/dotnet/roslyn/issues/11497")]
        public void PublicSignWithEmptyKeyPath2()
        {
            DefaultParse(new[] { "/publicsign", "/keyfile:\"\"", "a.cs" }, WorkingDirectory).Errors.Verify(
                // error CS2005: Missing file specification for 'keyfile' option
                Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("keyfile").WithLocation(1, 1));
        }

        [WorkItem(546301, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546301")]
        [Fact]
        public void SubsystemVersionTests()
        {
            CSharpCommandLineArguments parsedArgs = DefaultParse(new[] { "/subsystemversion:4.0", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(SubsystemVersion.Create(4, 0), parsedArgs.EmitOptions.SubsystemVersion);

            // wrongly supported subsystem version. CompilationOptions data will be faithful to the user input.
            // It is normalized at the time of emit.
            parsedArgs = DefaultParse(new[] { "/subsystemversion:0.0", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(); // no error in Dev11
            Assert.Equal(SubsystemVersion.Create(0, 0), parsedArgs.EmitOptions.SubsystemVersion);

            parsedArgs = DefaultParse(new[] { "/subsystemversion:0", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(); // no error in Dev11
            Assert.Equal(SubsystemVersion.Create(0, 0), parsedArgs.EmitOptions.SubsystemVersion);

            parsedArgs = DefaultParse(new[] { "/subsystemversion:3.99", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(); // no error in Dev11
            Assert.Equal(SubsystemVersion.Create(3, 99), parsedArgs.EmitOptions.SubsystemVersion);

            parsedArgs = DefaultParse(new[] { "/subsystemversion:4.0", "/SUBsystemversion:5.333", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(SubsystemVersion.Create(5, 333), parsedArgs.EmitOptions.SubsystemVersion);

            parsedArgs = DefaultParse(new[] { "/subsystemversion:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "subsystemversion"));

            parsedArgs = DefaultParse(new[] { "/subsystemversion", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "subsystemversion"));

            parsedArgs = DefaultParse(new[] { "/subsystemversion-", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/subsystemversion-"));

            parsedArgs = DefaultParse(new[] { "/subsystemversion: ", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "subsystemversion"));

            parsedArgs = DefaultParse(new[] { "/subsystemversion: 4.1", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_InvalidSubsystemVersion).WithArguments(" 4.1"));

            parsedArgs = DefaultParse(new[] { "/subsystemversion:4 .0", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_InvalidSubsystemVersion).WithArguments("4 .0"));

            parsedArgs = DefaultParse(new[] { "/subsystemversion:4. 0", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_InvalidSubsystemVersion).WithArguments("4. 0"));

            parsedArgs = DefaultParse(new[] { "/subsystemversion:.", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_InvalidSubsystemVersion).WithArguments("."));

            parsedArgs = DefaultParse(new[] { "/subsystemversion:4.", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_InvalidSubsystemVersion).WithArguments("4."));

            parsedArgs = DefaultParse(new[] { "/subsystemversion:.0", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_InvalidSubsystemVersion).WithArguments(".0"));

            parsedArgs = DefaultParse(new[] { "/subsystemversion:4.2 ", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new[] { "/subsystemversion:4.65536", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_InvalidSubsystemVersion).WithArguments("4.65536"));

            parsedArgs = DefaultParse(new[] { "/subsystemversion:65536.0", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_InvalidSubsystemVersion).WithArguments("65536.0"));

            parsedArgs = DefaultParse(new[] { "/subsystemversion:-4.0", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_InvalidSubsystemVersion).WithArguments("-4.0"));

            // TODO: incompatibilities: versions lower than '6.2' and 'arm', 'winmdobj', 'appcontainer'
        }

        [Fact]
        public void MainType()
        {
            CSharpCommandLineArguments parsedArgs = DefaultParse(new[] { "/m:A.B.C", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal("A.B.C", parsedArgs.CompilationOptions.MainTypeName);

            parsedArgs = DefaultParse(new[] { "/m: ", "a.cs" }, WorkingDirectory); // Mimicking Dev11
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "m"));
            Assert.Null(parsedArgs.CompilationOptions.MainTypeName);

            //  overriding the value
            parsedArgs = DefaultParse(new[] { "/m:A.B.C", "/MAIN:X.Y.Z", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal("X.Y.Z", parsedArgs.CompilationOptions.MainTypeName);

            //  error
            parsedArgs = DefaultParse(new[] { "/maiN:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "main"));

            parsedArgs = DefaultParse(new[] { "/MAIN+", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/MAIN+"));

            parsedArgs = DefaultParse(new[] { "/M", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "m"));

            //  incompatible values /main && /target
            parsedArgs = DefaultParse(new[] { "/main:a", "/t:library", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_NoMainOnDLL));

            parsedArgs = DefaultParse(new[] { "/main:a", "/t:module", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_NoMainOnDLL));
        }

        [Fact]
        public void Codepage()
        {
            CSharpCommandLineArguments parsedArgs = DefaultParse(new[] { "/CodePage:1200", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal("Unicode", parsedArgs.Encoding.EncodingName);

            parsedArgs = DefaultParse(new[] { "/CodePage:1200", "/codePAGE:65001", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal("Unicode (UTF-8)", parsedArgs.Encoding.EncodingName);

            parsedArgs = DefaultParse(new[] { "/CodePage:1252", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(1252, parsedArgs.Encoding.CodePage);

            //  error
            parsedArgs = DefaultParse(new[] { "/codepage:0", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.FTL_BadCodepage).WithArguments("0"));

            parsedArgs = DefaultParse(new[] { "/codepage:abc", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.FTL_BadCodepage).WithArguments("abc"));

            parsedArgs = DefaultParse(new[] { "/codepage:-5", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.FTL_BadCodepage).WithArguments("-5"));

            parsedArgs = DefaultParse(new[] { "/codepage: ", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.FTL_BadCodepage).WithArguments(""));

            parsedArgs = DefaultParse(new[] { "/codepage:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.FTL_BadCodepage).WithArguments(""));

            parsedArgs = DefaultParse(new[] { "/codepage", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "codepage"));

            parsedArgs = DefaultParse(new[] { "/codepage+", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/codepage+"));
        }

        [Fact, WorkItem(24735, "https://github.com/dotnet/roslyn/issues/24735")]
        public void ChecksumAlgorithm()
        {
            CSharpCommandLineArguments parsedArgs = DefaultParse(new[] { "/checksumAlgorithm:sHa1", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(SourceHashAlgorithm.Sha1, parsedArgs.ChecksumAlgorithm);
            Assert.Equal(HashAlgorithmName.SHA256, parsedArgs.EmitOptions.PdbChecksumAlgorithm);

            parsedArgs = DefaultParse(new[] { "/checksumAlgorithm:sha256", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(SourceHashAlgorithm.Sha256, parsedArgs.ChecksumAlgorithm);
            Assert.Equal(HashAlgorithmName.SHA256, parsedArgs.EmitOptions.PdbChecksumAlgorithm);

            parsedArgs = DefaultParse(new[] { "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();

            Assert.Equal(SourceHashAlgorithm.Sha256, parsedArgs.ChecksumAlgorithm);
            Assert.Equal(HashAlgorithmName.SHA256, parsedArgs.EmitOptions.PdbChecksumAlgorithm);

            //  error
            parsedArgs = DefaultParse(new[] { "/checksumAlgorithm:256", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.FTL_BadChecksumAlgorithm).WithArguments("256"));

            parsedArgs = DefaultParse(new[] { "/checksumAlgorithm:sha-1", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.FTL_BadChecksumAlgorithm).WithArguments("sha-1"));

            parsedArgs = DefaultParse(new[] { "/checksumAlgorithm:sha", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.FTL_BadChecksumAlgorithm).WithArguments("sha"));

            parsedArgs = DefaultParse(new[] { "/checksumAlgorithm: ", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "checksumalgorithm"));

            parsedArgs = DefaultParse(new[] { "/checksumAlgorithm:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "checksumalgorithm"));

            parsedArgs = DefaultParse(new[] { "/checksumAlgorithm", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "checksumalgorithm"));

            parsedArgs = DefaultParse(new[] { "/checksumAlgorithm+", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/checksumAlgorithm+"));
        }

        [Fact]
        public void AddModule()
        {
            CSharpCommandLineArguments parsedArgs = DefaultParse(new[] { "/noconfig", "/nostdlib", "/addmodule:abc.netmodule", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(1, parsedArgs.MetadataReferences.Length);
            Assert.Equal("abc.netmodule", parsedArgs.MetadataReferences[0].Reference);
            Assert.Equal(MetadataImageKind.Module, parsedArgs.MetadataReferences[0].Properties.Kind);

            parsedArgs = DefaultParse(new[] { "/noconfig", "/nostdlib", "/aDDmodule:c:\\abc;c:\\abc;d:\\xyz", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(3, parsedArgs.MetadataReferences.Length);
            Assert.Equal("c:\\abc", parsedArgs.MetadataReferences[0].Reference);
            Assert.Equal(MetadataImageKind.Module, parsedArgs.MetadataReferences[0].Properties.Kind);
            Assert.Equal("c:\\abc", parsedArgs.MetadataReferences[1].Reference);
            Assert.Equal(MetadataImageKind.Module, parsedArgs.MetadataReferences[1].Properties.Kind);
            Assert.Equal("d:\\xyz", parsedArgs.MetadataReferences[2].Reference);
            Assert.Equal(MetadataImageKind.Module, parsedArgs.MetadataReferences[2].Properties.Kind);

            //  error
            parsedArgs = DefaultParse(new[] { "/ADDMODULE", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "/addmodule:"));

            parsedArgs = DefaultParse(new[] { "/ADDMODULE+", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/ADDMODULE+"));

            parsedArgs = DefaultParse(new[] { "/ADDMODULE:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("/ADDMODULE:"));
        }

        [Fact, WorkItem(530751, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530751")]
        public void CS7061fromCS0647_ModuleWithCompilationRelaxations()
        {
            string source1 = Temp.CreateFile(prefix: "", extension: ".cs").WriteAllText(@"
using System.Runtime.CompilerServices;
[assembly: CompilationRelaxations(CompilationRelaxations.NoStringInterning)]
public class Mod { }").Path;

            string source2 = Temp.CreateFile(prefix: "", extension: ".cs").WriteAllText(@"
using System.Runtime.CompilerServices;
[assembly: CompilationRelaxations(4)]
public class Mod { }").Path;

            string source = Temp.CreateFile(prefix: "", extension: ".cs").WriteAllText(@"
using System.Runtime.CompilerServices;
[assembly: CompilationRelaxations(CompilationRelaxations.NoStringInterning)]
class Test { static void Main() {} }").Path;

            var baseDir = Path.GetDirectoryName(source);
            // === Scenario 1 ===
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            int exitCode = CreateCSharpCompiler(null, baseDir, new[] { "/nologo", "/t:module", source1 }).Run(outWriter);
            Assert.Equal(0, exitCode);

            var modfile = source1.Substring(0, source1.Length - 2) + "netmodule";
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var parsedArgs = DefaultParse(new[] { "/nologo", "/addmodule:" + modfile, source }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            exitCode = CreateCSharpCompiler(null, baseDir, new[] { "/nologo", "/addmodule:" + modfile, source }).Run(outWriter);
            Assert.Empty(outWriter.ToString());

            // === Scenario 2 ===
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = CreateCSharpCompiler(null, baseDir, new[] { "/nologo", "/t:module", source2 }).Run(outWriter);
            Assert.Equal(0, exitCode);

            modfile = source2.Substring(0, source2.Length - 2) + "netmodule";
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            parsedArgs = DefaultParse(new[] { "/nologo", "/addmodule:" + modfile, source }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            exitCode = CreateCSharpCompiler(null, baseDir, new[] { "/nologo", "/preferreduilang:en", "/addmodule:" + modfile, source }).Run(outWriter);
            Assert.Equal(1, exitCode);
            // Dev11: CS0647 (Emit)
            Assert.Contains("error CS7061: Duplicate 'CompilationRelaxationsAttribute' attribute in", outWriter.ToString(), StringComparison.Ordinal);

            CleanupAllGeneratedFiles(source1);
            CleanupAllGeneratedFiles(source2);
            CleanupAllGeneratedFiles(source);
        }

        [Fact, WorkItem(530780, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530780")]
        public void AddModuleWithExtensionMethod()
        {
            string source1 = Temp.CreateFile(prefix: "", extension: ".cs").WriteAllText(@"public static class Extensions { public static bool EB(this bool b) { return b; } }").Path;
            string source2 = Temp.CreateFile(prefix: "", extension: ".cs").WriteAllText(@"class C { static void Main() {} }").Path;
            var baseDir = Path.GetDirectoryName(source2);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            int exitCode = CreateCSharpCompiler(null, baseDir, new[] { "/nologo", "/t:module", source1 }).Run(outWriter);
            Assert.Equal(0, exitCode);

            var modfile = source1.Substring(0, source1.Length - 2) + "netmodule";
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = CreateCSharpCompiler(null, baseDir, new[] { "/nologo", "/addmodule:" + modfile, source2 }).Run(outWriter);
            Assert.Equal(0, exitCode);

            CleanupAllGeneratedFiles(source1);
            CleanupAllGeneratedFiles(source2);
        }

        [Fact, WorkItem(546297, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546297")]
        public void OLDCS0013FTL_MetadataEmitFailureSameModAndRes()
        {
            string source1 = Temp.CreateFile(prefix: "", extension: ".cs").WriteAllText(@"class Mod { }").Path;
            string source2 = Temp.CreateFile(prefix: "", extension: ".cs").WriteAllText(@"class C { static void Main() {} }").Path;
            var baseDir = Path.GetDirectoryName(source2);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            int exitCode = CreateCSharpCompiler(null, baseDir, new[] { "/nologo", "/t:module", source1 }).Run(outWriter);
            Assert.Equal(0, exitCode);

            var modfile = source1.Substring(0, source1.Length - 2) + "netmodule";
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = CreateCSharpCompiler(null, baseDir, new[] { "/nologo", "/preferreduilang:en", "/addmodule:" + modfile, "/linkres:" + modfile, source2 }).Run(outWriter);
            Assert.Equal(1, exitCode);
            // Native gives CS0013 at emit stage
            Assert.Equal("error CS7041: Each linked resource and module must have a unique filename. Filename '" + Path.GetFileName(modfile) + "' is specified more than once in this assembly", outWriter.ToString().Trim());

            CleanupAllGeneratedFiles(source1);
            CleanupAllGeneratedFiles(source2);
        }

        [Fact]
        public void Utf8Output()
        {
            CSharpCommandLineArguments parsedArgs = DefaultParse(new[] { "/utf8output", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.True((bool)parsedArgs.Utf8Output);

            parsedArgs = DefaultParse(new[] { "/utf8output", "/utf8output", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.True((bool)parsedArgs.Utf8Output);

            parsedArgs = DefaultParse(new[] { "/utf8output:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/utf8output:"));
        }

        [Fact]
        public void CscCompile_WithSourceCodeRedirectedViaStandardInput_ProducesRunnableProgram()
        {
            string tempDir = Temp.CreateDirectory().Path;
            ProcessResult result = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                ProcessUtilities.Run("cmd", $@"/C echo  ^
class A                                                 ^
{{                                                      ^
    public static void Main() =^^^>                     ^
        System.Console.WriteLine(""Hello World!"");     ^
}} | {s_CSharpCompilerExecutable} /nologo /t:exe -"
    .Replace(Environment.NewLine, string.Empty), workingDirectory: tempDir) :
                ProcessUtilities.Run("/usr/bin/env", $@"sh -c ""echo  \
class A                                                               \
{{                                                                    \
    public static void Main\(\) =\>                                   \
        System.Console.WriteLine\(\\\""Hello World\!\\\""\)\;         \
}} | {s_CSharpCompilerExecutable} /nologo /t:exe -""", workingDirectory: tempDir,
                    // we are testing shell's piped/redirected stdin behavior explicitly
                    // instead of using Process.StandardInput.Write(), so we set
                    // redirectStandardInput to true, which implies that isatty of child
                    // process is false and thereby Console.IsInputRedirected will return
                    // true in csc code.
                    redirectStandardInput: true);

            Assert.False(result.ContainsErrors, $"Compilation error(s) occurred: {result.Output} {result.Errors}");

            string output = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                ProcessUtilities.RunAndGetOutput("cmd.exe", $@"/C ""{s_DotnetCscRun} -.exe""", expectedRetCode: 0, startFolder: tempDir) :
                ProcessUtilities.RunAndGetOutput("sh", $@"-c ""{s_DotnetCscRun} -.exe""", expectedRetCode: 0, startFolder: tempDir);

            Assert.Equal("Hello World!", output.Trim());
        }

        [Fact]
        public void CscCompile_WithSourceCodeRedirectedViaStandardInput_ProducesLibrary()
        {
            var nameGuid = Guid.NewGuid().ToString();
            var name = nameGuid + ".dll";
            string tempDir = Temp.CreateDirectory().Path;
            ProcessResult result = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                ProcessUtilities.Run("cmd", $@"/C echo  ^
class A                                                 ^
{{                                                      ^
    public A Get() =^^^> default;                       ^
}} | {s_CSharpCompilerExecutable} /nologo /t:library /out:{name} -"
    .Replace(Environment.NewLine, string.Empty), workingDirectory: tempDir) :
                ProcessUtilities.Run("/usr/bin/env", $@"sh -c ""echo  \
class A                                                               \
{{                                                                    \
    public A Get\(\) =\> default\;                                    \
}} | {s_CSharpCompilerExecutable} /nologo /t:library /out:{name} -""", workingDirectory: tempDir,
                    // we are testing shell's piped/redirected stdin behavior explicitly
                    // instead of using Process.StandardInput.Write(), so we set
                    // redirectStandardInput to true, which implies that isatty of child
                    // process is false and thereby Console.IsInputRedirected will return
                    // true in csc code.
                    redirectStandardInput: true);

            Assert.False(result.ContainsErrors, $"Compilation error(s) occurred: {result.Output} {result.Errors}");

            var assemblyName = AssemblyName.GetAssemblyName(Path.Combine(tempDir, name));

            Assert.Equal(nameGuid, assemblyName.Name);
            Assert.Equal("0.0.0.0", assemblyName.Version.ToString());
            Assert.Equal(string.Empty, assemblyName.CultureName);
            Assert.Equal(Array.Empty<byte>(), assemblyName.GetPublicKeyToken());
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/55727")]
        public void CsiScript_WithSourceCodeRedirectedViaStandardInput_ExecutesNonInteractively()
        {
            string tempDir = Temp.CreateDirectory().Path;
            ProcessResult result = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                ProcessUtilities.Run("cmd", $@"/C echo Console.WriteLine(""Hello World!"") | {s_CSharpScriptExecutable} -") :
                ProcessUtilities.Run("/usr/bin/env", $@"sh -c ""echo Console.WriteLine\(\\\""Hello World\!\\\""\) | {s_CSharpScriptExecutable} -""",
                workingDirectory: tempDir,
                    // we are testing shell's piped/redirected stdin behavior explicitly
                    // instead of using Process.StandardInput.Write(), so we set
                    // redirectStandardInput to true, which implies that isatty of child
                    // process is false and thereby Console.IsInputRedirected will return
                    // true in csc code.
                    redirectStandardInput: true);

            Assert.False(result.ContainsErrors, $"Compilation error(s) occurred: {result.Output} {result.Errors}");
            Assert.Equal("Hello World!", result.Output.Trim());
        }

        [Fact]
        public void CscCompile_WithRedirectedInputIndicatorAndStandardInputNotRedirected_ReportsCS8782()
        {
            if (Console.IsInputRedirected)
            {
                // [applicable to both Windows and Unix]
                // if our parent (xunit) process itself has input redirected, we cannot test this
                // error case because our child process will inherit it and we cannot achieve what
                // we are aiming for: isatty(0):true and thereby Console.IsInputerRedirected:false in
                // child. running this case will make StreamReader to hang (waiting for input, that
                // we do not propagate: parent.In->child.In).
                //
                // note: in Unix we can "close" fd0 by appending `0>&-` in the `sh -c` command below,
                // but that will also not impact the result of isatty(), and in turn causes a different
                // compiler error.
                return;
            }

            string tempDir = Temp.CreateDirectory().Path;
            ProcessResult result = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                ProcessUtilities.Run("cmd", $@"/C ""{s_CSharpCompilerExecutable} /nologo /t:exe -""", workingDirectory: tempDir) :
                ProcessUtilities.Run("/usr/bin/env", $@"sh -c ""{s_CSharpCompilerExecutable} /nologo /t:exe -""", workingDirectory: tempDir);

            Assert.True(result.ContainsErrors);
            Assert.Contains(((int)ErrorCode.ERR_StdInOptionProvidedButConsoleInputIsNotRedirected).ToString(), result.Output);
        }

        [Fact]
        public void CscCompile_WithMultipleStdInOperators_WarnsCS2002()
        {
            string tempDir = Temp.CreateDirectory().Path;
            ProcessResult result = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                ProcessUtilities.Run("cmd", $@"/C echo  ^
class A                                                 ^
{{                                                      ^
    public static void Main() =^^^>                     ^
        System.Console.WriteLine(""Hello World!"");     ^
}} | {s_CSharpCompilerExecutable} /nologo - /t:exe -"
    .Replace(Environment.NewLine, string.Empty)) :
                ProcessUtilities.Run("/usr/bin/env", $@"sh -c ""echo  \
class A                                                               \
{{                                                                    \
    public static void Main\(\) =\>                                   \
        System.Console.WriteLine\(\\\""Hello World\!\\\""\)\;         \
}} | {s_CSharpCompilerExecutable} /nologo - /t:exe -""", workingDirectory: tempDir,
                    // we are testing shell's piped/redirected stdin behavior explicitly
                    // instead of using Process.StandardInput.Write(), so we set
                    // redirectStandardInput to true, which implies that isatty of child
                    // process is false and thereby Console.IsInputRedirected will return
                    // true in csc code.
                    redirectStandardInput: true);

            Assert.Contains(((int)ErrorCode.WRN_FileAlreadyIncluded).ToString(), result.Output);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/30321")]
        public void CscUtf8Output_WithRedirecting_Off()
        {
            var srcFile = Temp.CreateFile().WriteAllText("\u265A").Path;

            var tempOut = Temp.CreateFile();

            var output = ProcessUtilities.RunAndGetOutput("cmd", "/C \"" + s_CSharpCompilerExecutable + "\" /nologo /preferreduilang:en /t:library " + srcFile + " > " + tempOut.Path, expectedRetCode: 1);
            Assert.Equal("", output.Trim());
            Assert.Equal("SRC.CS(1,1): error CS1056: Unexpected character '?'", tempOut.ReadAllText().Trim().Replace(srcFile, "SRC.CS"));

            CleanupAllGeneratedFiles(srcFile);
            CleanupAllGeneratedFiles(tempOut.Path);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/30321")]
        public void CscUtf8Output_WithRedirecting_On()
        {
            var srcFile = Temp.CreateFile().WriteAllText("\u265A").Path;

            var tempOut = Temp.CreateFile();

            var output = ProcessUtilities.RunAndGetOutput("cmd", "/C \"" + s_CSharpCompilerExecutable + "\" /utf8output /nologo /preferreduilang:en /t:library " + srcFile + " > " + tempOut.Path, expectedRetCode: 1);
            Assert.Equal("", output.Trim());
            Assert.Equal("SRC.CS(1,1): error CS1056: Unexpected character '♚'", tempOut.ReadAllText().Trim().Replace(srcFile, "SRC.CS"));

            CleanupAllGeneratedFiles(srcFile);
            CleanupAllGeneratedFiles(tempOut.Path);
        }

        [WorkItem(546653, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546653")]
        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/30321")]
        public void NoSourcesWithModule()
        {
            var folder = Temp.CreateDirectory();
            var aCs = folder.CreateFile("a.cs");
            aCs.WriteAllText("public class C {}");

            var output = ProcessUtilities.RunAndGetOutput(s_CSharpCompilerExecutable, $"/nologo /t:module /out:a.netmodule \"{aCs}\"", startFolder: folder.ToString());
            Assert.Equal("", output.Trim());

            output = ProcessUtilities.RunAndGetOutput(s_CSharpCompilerExecutable, "/nologo /t:library /out:b.dll /addmodule:a.netmodule ", startFolder: folder.ToString());
            Assert.Equal("", output.Trim());

            output = ProcessUtilities.RunAndGetOutput(s_CSharpCompilerExecutable, "/nologo /preferreduilang:en /t:module /out:b.dll /addmodule:a.netmodule ", startFolder: folder.ToString());
            Assert.Equal("warning CS2008: No source files specified.", output.Trim());

            CleanupAllGeneratedFiles(aCs.Path);
        }

        [WorkItem(546653, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546653")]
        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/30321")]
        public void NoSourcesWithResource()
        {
            var folder = Temp.CreateDirectory();
            var aCs = folder.CreateFile("a.cs");
            aCs.WriteAllText("public class C {}");

            var output = ProcessUtilities.RunAndGetOutput(s_CSharpCompilerExecutable, "/nologo /t:library /out:b.dll /resource:a.cs", startFolder: folder.ToString());
            Assert.Equal("", output.Trim());

            CleanupAllGeneratedFiles(aCs.Path);
        }

        [WorkItem(546653, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546653")]
        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/30321")]
        public void NoSourcesWithLinkResource()
        {
            var folder = Temp.CreateDirectory();
            var aCs = folder.CreateFile("a.cs");
            aCs.WriteAllText("public class C {}");

            var output = ProcessUtilities.RunAndGetOutput(s_CSharpCompilerExecutable, "/nologo /t:library /out:b.dll /linkresource:a.cs", startFolder: folder.ToString());
            Assert.Equal("", output.Trim());

            CleanupAllGeneratedFiles(aCs.Path);
        }

        [Fact]
        public void KeyContainerAndKeyFile()
        {
            // KEYCONTAINER
            CSharpCommandLineArguments parsedArgs = DefaultParse(new[] { "/keycontainer:RIPAdamYauch", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal("RIPAdamYauch", parsedArgs.CompilationOptions.CryptoKeyContainer);

            parsedArgs = DefaultParse(new[] { "/keycontainer", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing '<text>' for 'keycontainer' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "keycontainer"));
            Assert.Null(parsedArgs.CompilationOptions.CryptoKeyContainer);

            parsedArgs = DefaultParse(new[] { "/keycontainer-", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2007: Unrecognized option: '/keycontainer-'
                Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/keycontainer-"));
            Assert.Null(parsedArgs.CompilationOptions.CryptoKeyContainer);

            parsedArgs = DefaultParse(new[] { "/keycontainer:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing '<text>' for 'keycontainer' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "keycontainer"));
            Assert.Null(parsedArgs.CompilationOptions.CryptoKeyContainer);

            parsedArgs = DefaultParse(new[] { "/keycontainer: ", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "keycontainer"));
            Assert.Null(parsedArgs.CompilationOptions.CryptoKeyContainer);

            // KEYFILE
            parsedArgs = DefaultParse(new[] { @"/keyfile:\somepath\s""ome Fil""e.goo.bar", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            //EDMAURER let's not set the option in the event that there was an error.
            //Assert.Equal(@"\somepath\some File.goo.bar", parsedArgs.CompilationOptions.CryptoKeyFile);

            parsedArgs = DefaultParse(new[] { "/keyFile", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2005: Missing file specification for 'keyfile' option
                Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("keyfile"));
            Assert.Null(parsedArgs.CompilationOptions.CryptoKeyFile);

            parsedArgs = DefaultParse(new[] { "/keyFile: ", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("keyfile"));
            Assert.Null(parsedArgs.CompilationOptions.CryptoKeyFile);

            parsedArgs = DefaultParse(new[] { "/keyfile-", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS2007: Unrecognized option: '/keyfile-'
                Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/keyfile-"));
            Assert.Null(parsedArgs.CompilationOptions.CryptoKeyFile);

            // DEFAULTS
            parsedArgs = DefaultParse(new[] { "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Null(parsedArgs.CompilationOptions.CryptoKeyFile);
            Assert.Null(parsedArgs.CompilationOptions.CryptoKeyContainer);

            // KEYFILE | KEYCONTAINER conflicts
            parsedArgs = DefaultParse(new[] { "/keyFile:a", "/keyContainer:b", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal("a", parsedArgs.CompilationOptions.CryptoKeyFile);
            Assert.Equal("b", parsedArgs.CompilationOptions.CryptoKeyContainer);

            parsedArgs = DefaultParse(new[] { "/keyContainer:b", "/keyFile:a", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal("a", parsedArgs.CompilationOptions.CryptoKeyFile);
            Assert.Equal("b", parsedArgs.CompilationOptions.CryptoKeyContainer);
        }

        [Fact, WorkItem(554551, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/554551")]
        public void CS1698WRN_AssumedMatchThis()
        {
            // compile with: /target:library /keyfile:mykey.snk
            var text1 = @"[assembly:System.Reflection.AssemblyVersion(""2"")]
public class CS1698_a {}
";
            // compile with: /target:library /reference:CS1698_a.dll /keyfile:mykey.snk
            var text2 = @"public class CS1698_b : CS1698_a {}
";
            //compile with: /target:library /out:cs1698_a.dll /reference:cs1698_b.dll /keyfile:mykey.snk
            var text = @"[assembly:System.Reflection.AssemblyVersion(""3"")]
public class CS1698_c : CS1698_b {}
public class CS1698_a {}
";

            var folder = Temp.CreateDirectory();
            var cs1698a = folder.CreateFile("CS1698a.cs");
            cs1698a.WriteAllText(text1);

            var cs1698b = folder.CreateFile("CS1698b.cs");
            cs1698b.WriteAllText(text2);

            var cs1698 = folder.CreateFile("CS1698.cs");
            cs1698.WriteAllText(text);

            var snkFile = Temp.CreateFile().WriteAllBytes(TestResources.General.snKey);
            var kfile = "/keyfile:" + snkFile.Path;

            CSharpCommandLineArguments parsedArgs = DefaultParse(new[] { "/t:library", kfile, "CS1698a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new[] { "/t:library", kfile, "/r:" + cs1698a.Path, "CS1698b.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new[] { "/t:library", kfile, "/r:" + cs1698b.Path, "/out:" + cs1698a.Path, "CS1698.cs" }, WorkingDirectory);

            // Roslyn no longer generates a warning for this...since this was only a warning, we're not really
            // saving anyone...does not provide high value to implement...

            // warning CS1698: Circular assembly reference 'CS1698a, Version=2.0.0.0, Culture=neutral,PublicKeyToken = 9e9d6755e7bb4c10'
            // does not match the output assembly name 'CS1698a, Version = 3.0.0.0, Culture = neutral, PublicKeyToken = 9e9d6755e7bb4c10'.
            // Try adding a reference to 'CS1698a, Version = 2.0.0.0, Culture = neutral, PublicKeyToken = 9e9d6755e7bb4c10' or changing the output assembly name to match.
            parsedArgs.Errors.Verify();

            CleanupAllGeneratedFiles(snkFile.Path);
            CleanupAllGeneratedFiles(cs1698a.Path);
            CleanupAllGeneratedFiles(cs1698b.Path);
            CleanupAllGeneratedFiles(cs1698.Path);
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/dotnet/roslyn/issues/30926")]
        public void BinaryFileErrorTest()
        {
            var binaryPath = Temp.CreateFile().WriteAllBytes(Net461.Resources.mscorlib).Path;
            var csc = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/nologo", "/preferreduilang:en", binaryPath });
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            int exitCode = csc.Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Equal(
                "error CS2015: '" + binaryPath + "' is a binary file instead of a text file",
                outWriter.ToString().Trim());

            CleanupAllGeneratedFiles(binaryPath);
        }

#if !NETCOREAPP
        [WorkItem(530221, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530221")]
        [WorkItem(5660, "https://github.com/dotnet/roslyn/issues/5660")]
        [ConditionalFact(typeof(WindowsOnly), typeof(IsEnglishLocal))]
        public void Bug15538()
        {
            // Several Jenkins VMs are still running with local systems permissions.  This suite won't run properly
            // in that environment.  Removing this check is being tracked by issue #79.
            using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
            {
                if (identity.IsSystem)
                {
                    return;
                }

                // The icacls command fails on our Helix machines and it appears to be related to the use of the $ in
                // the username.
                // https://github.com/dotnet/roslyn/issues/28836
                if (StringComparer.OrdinalIgnoreCase.Equals(Environment.UserDomainName, "WORKGROUP"))
                {
                    return;
                }
            }

            var folder = Temp.CreateDirectory();
            var source = folder.CreateFile("src.vb").WriteAllText("").Path;
            var _ref = folder.CreateFile("ref.dll").WriteAllText("").Path;
            try
            {
                var output = ProcessUtilities.RunAndGetOutput("cmd", "/C icacls " + _ref + " /inheritance:r /Q");
                Assert.Equal("Successfully processed 1 files; Failed processing 0 files", output.Trim());

                output = ProcessUtilities.RunAndGetOutput("cmd", "/C icacls " + _ref + @" /deny %USERDOMAIN%\%USERNAME%:(r,WDAC) /Q");
                Assert.Equal("Successfully processed 1 files; Failed processing 0 files", output.Trim());

                output = ProcessUtilities.RunAndGetOutput("cmd", "/C \"" + s_CSharpCompilerExecutable + "\" /nologo /preferreduilang:en /r:" + _ref + " /t:library " + source, expectedRetCode: 1);
                Assert.Equal("error CS0009: Metadata file '" + _ref + "' could not be opened -- Access to the path '" + _ref + "' is denied.", output.Trim());
            }
            finally
            {
                var output = ProcessUtilities.RunAndGetOutput("cmd", "/C icacls " + _ref + " /reset /Q");
                Assert.Equal("Successfully processed 1 files; Failed processing 0 files", output.Trim());
                File.Delete(_ref);
            }

            CleanupAllGeneratedFiles(source);
        }
#endif

        [WorkItem(545832, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545832")]
        [Fact]
        public void ResponseFilesWithEmptyAliasReference()
        {
            string source = Temp.CreateFile("a.cs").WriteAllText(@"
// <Area> ExternAlias - command line alias</Area>
// <Title>
// negative test cases: empty file name ("""")
// </Title>
// <Description>
// </Description>
// <RelatedBugs></RelatedBugs>

//<Expects Status=error>CS1680:.*myAlias=</Expects>

// <Code>
class myClass
{
    static int Main()
    {
        return 1;
    }
}
// </Code>
").Path;

            string rsp = Temp.CreateFile().WriteAllText(@"
/nologo
/r:myAlias=""""
").Path;

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            // csc errors_whitespace_008.cs @errors_whitespace_008.cs.rsp
            var csc = CreateCSharpCompiler(rsp, WorkingDirectory, new[] { source, "/preferreduilang:en" });
            int exitCode = csc.Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Equal("error CS1680: Invalid reference alias option: 'myAlias=' -- missing filename", outWriter.ToString().Trim());

            CleanupAllGeneratedFiles(source);
            CleanupAllGeneratedFiles(rsp);
        }

        [Fact]
        public void ResponseFileOrdering()
        {
            var rspFilePath1 = Temp.CreateFile().WriteAllText(@"
/b
/c
").Path;

            assertOrder(
                new[] { "/a", "/b", "/c", "/d" },
                new[] { "/a", @$"@""{rspFilePath1}""", "/d" });

            var rspFilePath2 = Temp.CreateFile().WriteAllText(@"
/c
/d
").Path;

            rspFilePath1 = Temp.CreateFile().WriteAllText(@$"
/b
@""{rspFilePath2}""
").Path;

            assertOrder(
                new[] { "/a", "/b", "/c", "/d", "/e" },
                new[] { "/a", @$"@""{rspFilePath1}""", "/e" });

            rspFilePath1 = Temp.CreateFile().WriteAllText(@$"
/b
").Path;

            rspFilePath2 = Temp.CreateFile().WriteAllText(@"
# this will be ignored
/c
/d
").Path;

            assertOrder(
                new[] { "/a", "/b", "/c", "/d", "/e" },
                new[] { "/a", @$"@""{rspFilePath1}""", $@"@""{rspFilePath2}""", "/e" });

            void assertOrder(string[] expected, string[] args)
            {
                var flattenedArgs = ArrayBuilder<string>.GetInstance();
                var diagnostics = new List<Diagnostic>();
                CSharpCommandLineParser.Default.FlattenArgs(
                    args,
                    diagnostics,
                    flattenedArgs,
                    scriptArgsOpt: null,
                    baseDirectory: Path.DirectorySeparatorChar == '\\' ? @"c:\" : "/");

                Assert.Empty(diagnostics);
                Assert.Equal(expected, flattenedArgs);
                flattenedArgs.Free();
            }
        }

        [WorkItem(545832, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545832")]
        [Fact]
        public void ResponseFilesWithEmptyAliasReference2()
        {
            string source = Temp.CreateFile("a.cs").WriteAllText(@"
// <Area> ExternAlias - command line alias</Area>
// <Title>
// negative test cases: empty file name ("""")
// </Title>
// <Description>
// </Description>
// <RelatedBugs></RelatedBugs>

//<Expects Status=error>CS1680:.*myAlias=</Expects>

// <Code>
class myClass
{
    static int Main()
    {
        return 1;
    }
}
// </Code>
").Path;

            string rsp = Temp.CreateFile().WriteAllText(@"
/nologo
/r:myAlias=""  ""
").Path;

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            // csc errors_whitespace_008.cs @errors_whitespace_008.cs.rsp
            var csc = CreateCSharpCompiler(rsp, WorkingDirectory, new[] { source, "/preferreduilang:en" });
            int exitCode = csc.Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Equal("error CS1680: Invalid reference alias option: 'myAlias=' -- missing filename", outWriter.ToString().Trim());

            CleanupAllGeneratedFiles(source);
            CleanupAllGeneratedFiles(rsp);
        }

        [WorkItem(1784, "https://github.com/dotnet/roslyn/issues/1784")]
        [Fact]
        public void QuotedDefineInRespFile()
        {
            string source = Temp.CreateFile("a.cs").WriteAllText(@"
#if NN
class myClass
{
#endif
    static int Main()
#if DD
    {
        return 1;
#endif

#if AA
    }
#endif

#if BB
}
#endif

").Path;

            string rsp = Temp.CreateFile().WriteAllText(@"
/d:""DD""
/d:""AA;BB""
/d:""N""N
").Path;

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            // csc errors_whitespace_008.cs @errors_whitespace_008.cs.rsp
            var csc = CreateCSharpCompiler(rsp, WorkingDirectory, new[] { source, "/preferreduilang:en" });
            int exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);

            CleanupAllGeneratedFiles(source);
            CleanupAllGeneratedFiles(rsp);
        }

        [WorkItem(1784, "https://github.com/dotnet/roslyn/issues/1784")]
        [Fact]
        public void QuotedDefineInRespFileErr()
        {
            string source = Temp.CreateFile("a.cs").WriteAllText(@"
#if NN
class myClass
{
#endif
    static int Main()
#if DD
    {
        return 1;
#endif

#if AA
    }
#endif

#if BB
}
#endif

").Path;

            string rsp = Temp.CreateFile().WriteAllText(@"
/d:""DD""""
/d:""AA;BB""
/d:""N"" ""N
").Path;

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            // csc errors_whitespace_008.cs @errors_whitespace_008.cs.rsp
            var csc = CreateCSharpCompiler(rsp, WorkingDirectory, new[] { source, "/preferreduilang:en" });
            int exitCode = csc.Run(outWriter);
            Assert.Equal(1, exitCode);

            CleanupAllGeneratedFiles(source);
            CleanupAllGeneratedFiles(rsp);
        }

        [Fact]
        public void ResponseFileSplitting()
        {
            string[] responseFile;

            responseFile = new string[] {
                @"a.cs b.cs ""c.cs e.cs""",
                @"hello world # this is a comment"
            };

            IEnumerable<string> args = CSharpCommandLineParser.ParseResponseLines(responseFile);
            AssertEx.Equal(new[] { "a.cs", "b.cs", @"c.cs e.cs", "hello", "world" }, args);

            // Check comment handling; comment character only counts at beginning of argument
            responseFile = new string[] {
                @"   # ignore this",
                @"   # ignore that ""hello""",
                @"  a.cs #3.cs",
                @"  b#.cs c#d.cs #e.cs",
                @"  ""#f.cs""",
                @"  ""#g.cs #h.cs"""
            };

            args = CSharpCommandLineParser.ParseResponseLines(responseFile);
            AssertEx.Equal(new[] { "a.cs", "b#.cs", "c#d.cs", "#f.cs", "#g.cs #h.cs" }, args);

            // Check backslash escaping
            responseFile = new string[] {
                @"a\b\c d\\e\\f\\ \\\g\\\h\\\i \\\\ \\\\\k\\\\\",
            };
            args = CSharpCommandLineParser.ParseResponseLines(responseFile);
            AssertEx.Equal(new[] { @"a\b\c", @"d\\e\\f\\", @"\\\g\\\h\\\i", @"\\\\", @"\\\\\k\\\\\" }, args);

            // More backslash escaping and quoting
            responseFile = new string[] {
                @"a\""a b\\""b c\\\""c d\\\\""d e\\\\\""e f"" g""",
            };
            args = CSharpCommandLineParser.ParseResponseLines(responseFile);
            AssertEx.Equal(new[] { @"a\""a", @"b\\""b c\\\""c d\\\\""d", @"e\\\\\""e", @"f"" g""" }, args);

            // Quoting inside argument is valid.
            responseFile = new string[] {
                @"  /o:""goo.cs"" /o:""abc def""\baz ""/o:baz bar""bing",
            };
            args = CSharpCommandLineParser.ParseResponseLines(responseFile);
            AssertEx.Equal(new[] { @"/o:""goo.cs""", @"/o:""abc def""\baz", @"""/o:baz bar""bing" }, args);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void SourceFileQuoting()
        {
            string[] responseFile = new string[] {
                @"d:\\""abc def""\baz.cs ab""c d""e.cs",
            };

            CSharpCommandLineArguments args = DefaultParse(CSharpCommandLineParser.ParseResponseLines(responseFile), @"c:\");
            AssertEx.Equal(new[] { @"d:\abc def\baz.cs", @"c:\abc de.cs" }, args.SourceFiles.Select(file => file.Path));
        }

        [WorkItem(544441, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544441")]
        [Fact]
        public void OutputFileName1()
        {
            string source1 = @"
class A
{
}
";
            string source2 = @"
class B
{
    static void Main() { }
}
";
            // Name comes from first input (file, not class) name, since DLL.
            CheckOutputFileName(
                source1, source2,
                inputName1: "p.cs", inputName2: "q.cs",
                commandLineArguments: new[] { "/target:library" },
                expectedOutputName: "p.dll");
        }

        [WorkItem(544441, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544441")]
        [Fact]
        public void OutputFileName2()
        {
            string source1 = @"
class A
{
}
";
            string source2 = @"
class B
{
    static void Main() { }
}
";
            // Name comes from command-line option.
            CheckOutputFileName(
                source1, source2,
                inputName1: "p.cs", inputName2: "q.cs",
                commandLineArguments: new[] { "/target:library", "/out:r.dll" },
                expectedOutputName: "r.dll");
        }

        [WorkItem(544441, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544441")]
        [Fact]
        public void OutputFileName3()
        {
            string source1 = @"
class A
{
}
";
            string source2 = @"
class B
{
    static void Main() { }
}
";
            // Name comes from name of file containing entrypoint, since EXE.
            CheckOutputFileName(
                source1, source2,
                inputName1: "p.cs", inputName2: "q.cs",
                commandLineArguments: new[] { "/target:exe" },
                expectedOutputName: "q.exe");
        }

        [WorkItem(544441, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544441")]
        [Fact]
        public void OutputFileName4()
        {
            string source1 = @"
class A
{
}
";
            string source2 = @"
class B
{
    static void Main() { }
}
";
            // Name comes from command-line option.
            CheckOutputFileName(
                source1, source2,
                inputName1: "p.cs", inputName2: "q.cs",
                commandLineArguments: new[] { "/target:exe", "/out:r.exe" },
                expectedOutputName: "r.exe");
        }

        [WorkItem(544441, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544441")]
        [Fact]
        public void OutputFileName5()
        {
            string source1 = @"
class A
{
    static void Main() { }
}
";
            string source2 = @"
class B
{
    static void Main() { }
}
";
            // Name comes from name of file containing entrypoint - affected by /main, since EXE.
            CheckOutputFileName(
                source1, source2,
                inputName1: "p.cs", inputName2: "q.cs",
                commandLineArguments: new[] { "/target:exe", "/main:A" },
                expectedOutputName: "p.exe");
        }

        [WorkItem(544441, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544441")]
        [Fact]
        public void OutputFileName6()
        {
            string source1 = @"
class A
{
    static void Main() { }
}
";
            string source2 = @"
class B
{
    static void Main() { }
}
";
            // Name comes from name of file containing entrypoint - affected by /main, since EXE.
            CheckOutputFileName(
                source1, source2,
                inputName1: "p.cs", inputName2: "q.cs",
                commandLineArguments: new[] { "/target:exe", "/main:B" },
                expectedOutputName: "q.exe");
        }

        [WorkItem(544441, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544441")]
        [Fact]
        public void OutputFileName7()
        {
            string source1 = @"
partial class A
{
    static partial void Main() { }
}
";
            string source2 = @"
partial class A
{
    static partial void Main();
}
";
            // Name comes from name of file containing entrypoint, since EXE.
            CheckOutputFileName(
                source1, source2,
                inputName1: "p.cs", inputName2: "q.cs",
                commandLineArguments: new[] { "/target:exe" },
                expectedOutputName: "p.exe");
        }

        [WorkItem(544441, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544441")]
        [Fact]
        public void OutputFileName8()
        {
            string source1 = @"
partial class A
{
    static partial void Main();
}
";
            string source2 = @"
partial class A
{
    static partial void Main() { }
}
";
            // Name comes from name of file containing entrypoint, since EXE.
            CheckOutputFileName(
                source1, source2,
                inputName1: "p.cs", inputName2: "q.cs",
                commandLineArguments: new[] { "/target:exe" },
                expectedOutputName: "q.exe");
        }

        [Fact]
        public void OutputFileName9()
        {
            string source1 = @"
class A
{
}
";
            string source2 = @"
class B
{
    static void Main() { }
}
";
            // Name comes from first input (file, not class) name, since winmdobj.
            CheckOutputFileName(
                source1, source2,
                inputName1: "p.cs", inputName2: "q.cs",
                commandLineArguments: new[] { "/target:winmdobj" },
                expectedOutputName: "p.winmdobj");
        }

        [Fact]
        public void OutputFileName10()
        {
            string source1 = @"
class A
{
}
";
            string source2 = @"
class B
{
    static void Main() { }
}
";
            // Name comes from name of file containing entrypoint, since appcontainerexe.
            CheckOutputFileName(
                source1, source2,
                inputName1: "p.cs", inputName2: "q.cs",
                commandLineArguments: new[] { "/target:appcontainerexe" },
                expectedOutputName: "q.exe");
        }

        [Fact]
        public void OutputFileName_Switch()
        {
            string source1 = @"
class A
{
}
";
            string source2 = @"
class B
{
    static void Main() { }
}
";
            // Name comes from name of file containing entrypoint, since EXE.
            CheckOutputFileName(
                source1, source2,
                inputName1: "p.cs", inputName2: "q.cs",
                commandLineArguments: new[] { "/target:exe", "/out:r.exe" },
                expectedOutputName: "r.exe");
        }

        [Fact]
        public void OutputFileName_NoEntryPoint()
        {
            string source = @"
class C
{
}
";
            var dir = Temp.CreateDirectory();

            var file = dir.CreateFile("a.cs");
            file.WriteAllText(source);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(null, dir.Path, new[] { "/nologo", "/preferreduilang:en", "/target:exe", "a.cs" });
            int exitCode = csc.Run(outWriter);
            Assert.NotEqual(0, exitCode);
            Assert.Equal("error CS5001: Program does not contain a static 'Main' method suitable for an entry point", outWriter.ToString().Trim());

            CleanupAllGeneratedFiles(file.Path);
        }

        [Fact, WorkItem(1093063, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1093063")]
        public void VerifyDiagnosticSeverityNotLocalized()
        {
            string source = @"
class C
{
}
";
            var dir = Temp.CreateDirectory();

            var file = dir.CreateFile("a.cs");
            file.WriteAllText(source);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(null, dir.Path, new[] { "/nologo", "/target:exe", "a.cs" });
            int exitCode = csc.Run(outWriter);
            Assert.NotEqual(0, exitCode);

            // If "error" was localized, below assert will fail on PLOC builds. The output would be something like: "!pTCvB!vbc : !FLxft!error 表! CS5001:"
            Assert.Contains("error CS5001:", outWriter.ToString().Trim());

            CleanupAllGeneratedFiles(file.Path);
        }

        [Fact]
        public void NoLogo_1()
        {
            string source = @"
class C
{
}
";
            var dir = Temp.CreateDirectory();

            var file = dir.CreateFile("a.cs");
            file.WriteAllText(source);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(null, dir.Path, new[] { "/nologo", "/target:library", "a.cs" });
            int exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal(@"",
                outWriter.ToString().Trim());

            CleanupAllGeneratedFiles(file.Path);
        }

        [Fact]
        public void NoLogo_2()
        {
            string source = @"
class C
{
}
";
            var dir = Temp.CreateDirectory();

            var file = dir.CreateFile("a.cs");
            file.WriteAllText(source);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(null, dir.Path, new[] { "/target:library", "/preferreduilang:en", "a.cs" });
            int exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);

            var patched = Regex.Replace(outWriter.ToString().Trim(), "version \\d+\\.\\d+\\.\\d+(-[\\w\\d]+)*", "version A.B.C-d");
            patched = ReplaceCommitHash(patched);
            Assert.Equal(@"
Microsoft (R) Visual C# Compiler version A.B.C-d (HASH)
Copyright (C) Microsoft Corporation. All rights reserved.".Trim(),
                patched);

            CleanupAllGeneratedFiles(file.Path);
        }

        [Theory,
            InlineData("Microsoft (R) Visual C# Compiler version A.B.C-d (<developer build>)",
                "Microsoft (R) Visual C# Compiler version A.B.C-d (HASH)"),
            InlineData("Microsoft (R) Visual C# Compiler version A.B.C-d (ABCDEF01)",
                "Microsoft (R) Visual C# Compiler version A.B.C-d (HASH)"),
            InlineData("Microsoft (R) Visual C# Compiler version A.B.C-d (abcdef90)",
                "Microsoft (R) Visual C# Compiler version A.B.C-d (HASH)"),
            InlineData("Microsoft (R) Visual C# Compiler version A.B.C-d (12345678)",
                "Microsoft (R) Visual C# Compiler version A.B.C-d (HASH)")]
        public void TestReplaceCommitHash(string orig, string expected)
        {
            Assert.Equal(expected, ReplaceCommitHash(orig));
        }

        private static string ReplaceCommitHash(string s)
        {
            // open paren, followed by either <developer build> or 8 hex, followed by close paren
            return Regex.Replace(s, "(\\((<developer build>|[a-fA-F0-9]{8})\\))", "(HASH)");
        }

        private void CheckOutputFileName(string source1, string source2, string inputName1, string inputName2, string[] commandLineArguments, string expectedOutputName)
        {
            var dir = Temp.CreateDirectory();

            var file1 = dir.CreateFile(inputName1);
            file1.WriteAllText(source1);

            var file2 = dir.CreateFile(inputName2);
            file2.WriteAllText(source2);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(null, dir.Path, commandLineArguments.Concat(new[] { inputName1, inputName2 }).ToArray());
            int exitCode = csc.Run(outWriter);
            if (exitCode != 0)
            {
                Console.WriteLine(outWriter.ToString());
                Assert.Equal(0, exitCode);
            }

            Assert.Equal(1, Directory.EnumerateFiles(dir.Path, "*" + PathUtilities.GetExtension(expectedOutputName)).Count());
            Assert.Equal(1, Directory.EnumerateFiles(dir.Path, expectedOutputName).Count());

            using (var metadata = ModuleMetadata.CreateFromImage(File.ReadAllBytes(Path.Combine(dir.Path, expectedOutputName))))
            {
                var peReader = metadata.Module.GetMetadataReader();

                Assert.True(peReader.IsAssembly);

                Assert.Equal(PathUtilities.RemoveExtension(expectedOutputName), peReader.GetString(peReader.GetAssemblyDefinition().Name));
                Assert.Equal(expectedOutputName, peReader.GetString(peReader.GetModuleDefinition().Name));
            }

            if (System.IO.File.Exists(expectedOutputName))
            {
                System.IO.File.Delete(expectedOutputName);
            }

            CleanupAllGeneratedFiles(file1.Path);
            CleanupAllGeneratedFiles(file2.Path);
        }

        [Fact]
        public void MissingReference()
        {
            string source = @"
class C
{
}
";
            var dir = Temp.CreateDirectory();

            var file = dir.CreateFile("a.cs");
            file.WriteAllText(source);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(null, dir.Path, new[] { "/nologo", "/preferreduilang:en", "/r:missing.dll", "a.cs" });
            int exitCode = csc.Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Equal("error CS0006: Metadata file 'missing.dll' could not be found", outWriter.ToString().Trim());

            CleanupAllGeneratedFiles(file.Path);
        }

        [WorkItem(545025, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545025")]
        [ConditionalFact(typeof(WindowsOnly))]
        public void CompilationWithWarnAsError_01()
        {
            string source = @"
public class C
{
    public static void Main()
    {
    }
}";

            // Baseline without warning options (expect success)
            int exitCode = GetExitCode(source, "a.cs", new String[] { });
            Assert.Equal(0, exitCode);

            // The case with /warnaserror (expect to be success, since there will be no warning)
            exitCode = GetExitCode(source, "b.cs", new[] { "/warnaserror" });
            Assert.Equal(0, exitCode);

            // The case with /warnaserror and /nowarn:1 (expect success)
            // Note that even though the command line option has a warning, it is not going to become an error
            // in order to avoid the halt of compilation.
            exitCode = GetExitCode(source, "c.cs", new[] { "/warnaserror", "/nowarn:1" });
            Assert.Equal(0, exitCode);
        }

        [WorkItem(545025, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545025")]
        [ConditionalFact(typeof(WindowsOnly))]
        public void CompilationWithWarnAsError_02()
        {
            string source = @"
public class C
{
    public static void Main()
    {
        int x; // CS0168
    }
}";

            // Baseline without warning options (expect success)
            int exitCode = GetExitCode(source, "a.cs", new String[] { });
            Assert.Equal(0, exitCode);

            // The case with /warnaserror (expect failure)
            exitCode = GetExitCode(source, "b.cs", new[] { "/warnaserror" });
            Assert.NotEqual(0, exitCode);

            // The case with /warnaserror:168 (expect failure)
            exitCode = GetExitCode(source, "c.cs", new[] { "/warnaserror:168" });
            Assert.NotEqual(0, exitCode);

            // The case with /warnaserror:219 (expect success)
            exitCode = GetExitCode(source, "c.cs", new[] { "/warnaserror:219" });
            Assert.Equal(0, exitCode);

            // The case with /warnaserror and /nowarn:168 (expect success)
            exitCode = GetExitCode(source, "d.cs", new[] { "/warnaserror", "/nowarn:168" });
            Assert.Equal(0, exitCode);
        }

        private int GetExitCode(string source, string fileName, string[] commandLineArguments)
        {
            var dir = Temp.CreateDirectory();
            var file = dir.CreateFile(fileName);
            file.WriteAllText(source);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(null, dir.Path, commandLineArguments.Concat(new[] { fileName }).ToArray());
            int exitCode = csc.Run(outWriter);

            return exitCode;
        }

        [WorkItem(545247, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545247")]
        [ConditionalFact(typeof(WindowsOnly))]
        public void CompilationWithNonExistingOutPath()
        {
            string source = @"
public class C
{
    public static void Main()
    {
    }
}";

            var fileName = "a.cs";
            var dir = Temp.CreateDirectory();
            var file = dir.CreateFile(fileName);
            file.WriteAllText(source);
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(null, dir.Path, new[] { fileName, "/preferreduilang:en", "/target:exe", "/out:sub\\a.exe" });
            int exitCode = csc.Run(outWriter);

            Assert.Equal(1, exitCode);
            Assert.Contains("error CS2012: Cannot open '" + dir.Path + "\\sub\\a.exe' for writing", outWriter.ToString(), StringComparison.Ordinal);

            CleanupAllGeneratedFiles(file.Path);
        }

        [WorkItem(545247, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545247")]
        [Fact]
        public void CompilationWithWrongOutPath_01()
        {
            string source = @"
public class C
{
    public static void Main()
    {
    }
}";

            var fileName = "a.cs";
            var dir = Temp.CreateDirectory();
            var file = dir.CreateFile(fileName);
            file.WriteAllText(source);
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(null, dir.Path, new[] { fileName, "/preferreduilang:en", "/target:exe", "/out:sub\\" });
            int exitCode = csc.Run(outWriter);

            Assert.Equal(1, exitCode);
            var message = outWriter.ToString();
            Assert.Contains("error CS2021: File name", message, StringComparison.Ordinal);
            Assert.Contains("sub", message, StringComparison.Ordinal);

            CleanupAllGeneratedFiles(file.Path);
        }

        [WorkItem(545247, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545247")]
        [Fact]
        public void CompilationWithWrongOutPath_02()
        {
            string source = @"
public class C
{
    public static void Main()
    {
    }
}";

            var fileName = "a.cs";
            var dir = Temp.CreateDirectory();
            var file = dir.CreateFile(fileName);
            file.WriteAllText(source);
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(null, dir.Path, new[] { fileName, "/preferreduilang:en", "/target:exe", "/out:sub\\ " });
            int exitCode = csc.Run(outWriter);

            Assert.Equal(1, exitCode);
            var message = outWriter.ToString();
            Assert.Contains("error CS2021: File name", message, StringComparison.Ordinal);
            Assert.Contains("sub", message, StringComparison.Ordinal);

            CleanupAllGeneratedFiles(file.Path);
        }

        [WorkItem(545247, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545247")]
        [ConditionalFact(typeof(WindowsDesktopOnly))]
        public void CompilationWithWrongOutPath_03()
        {
            string source = @"
public class C
{
    public static void Main()
    {
    }
}";

            var fileName = "a.cs";
            var dir = Temp.CreateDirectory();
            var file = dir.CreateFile(fileName);
            file.WriteAllText(source);
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(null, dir.Path, new[] { fileName, "/preferreduilang:en", "/target:exe", "/out:aaa:\\a.exe" });
            int exitCode = csc.Run(outWriter);

            Assert.Equal(1, exitCode);
            Assert.Contains(@"error CS2021: File name 'aaa:\a.exe' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long", outWriter.ToString(), StringComparison.Ordinal);

            CleanupAllGeneratedFiles(file.Path);
        }

        [WorkItem(545247, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545247")]
        [Fact]
        public void CompilationWithWrongOutPath_04()
        {
            string source = @"
public class C
{
    public static void Main()
    {
    }
}";

            var fileName = "a.cs";
            var dir = Temp.CreateDirectory();
            var file = dir.CreateFile(fileName);
            file.WriteAllText(source);
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(null, dir.Path, new[] { fileName, "/preferreduilang:en", "/target:exe", "/out: " });
            int exitCode = csc.Run(outWriter);

            Assert.Equal(1, exitCode);
            Assert.Contains("error CS2005: Missing file specification for '/out:' option", outWriter.ToString(), StringComparison.Ordinal);

            CleanupAllGeneratedFiles(file.Path);
        }

        [Fact]
        public void EmittedSubsystemVersion()
        {
            var compilation = CSharpCompilation.Create("a.dll", references: new[] { MscorlibRef }, options: TestOptions.ReleaseDll);
            var peHeaders = new PEHeaders(compilation.EmitToStream(options: new EmitOptions(subsystemVersion: SubsystemVersion.Create(5, 1))));
            Assert.Equal(5, peHeaders.PEHeader.MajorSubsystemVersion);
            Assert.Equal(1, peHeaders.PEHeader.MinorSubsystemVersion);
        }

        [Fact]
        public void CreateCompilationWithKeyFile()
        {
            string source = @"
public class C
{
    public static void Main()
    {
    }
}";

            var fileName = "a.cs";
            var dir = Temp.CreateDirectory();
            var file = dir.CreateFile(fileName);
            file.WriteAllText(source);

            var cmd = CreateCSharpCompiler(null, dir.Path, new[] { "/nologo", "a.cs", "/keyfile:key.snk", });
            var comp = cmd.CreateCompilation(TextWriter.Null, new TouchedFileLogger(), NullErrorLogger.Instance);

            Assert.IsType<DesktopStrongNameProvider>(comp.Options.StrongNameProvider);
        }

        [Fact]
        public void CreateCompilationWithKeyContainer()
        {
            string source = @"
public class C
{
    public static void Main()
    {
    }
}";

            var fileName = "a.cs";
            var dir = Temp.CreateDirectory();
            var file = dir.CreateFile(fileName);
            file.WriteAllText(source);

            var cmd = CreateCSharpCompiler(null, dir.Path, new[] { "/nologo", "a.cs", "/keycontainer:bbb", });
            var comp = cmd.CreateCompilation(TextWriter.Null, new TouchedFileLogger(), NullErrorLogger.Instance);

            Assert.Equal(typeof(DesktopStrongNameProvider), comp.Options.StrongNameProvider.GetType());
        }

        [Fact]
        public void CreateCompilationFallbackCommand()
        {
            string source = @"
public class C
{
    public static void Main()
    {
    }
}";

            var fileName = "a.cs";
            var dir = Temp.CreateDirectory();
            var file = dir.CreateFile(fileName);
            file.WriteAllText(source);

            Assert.Equal("UseLegacyStrongNameProvider", Feature.UseLegacyStrongNameProvider);
            var cmd = CreateCSharpCompiler(null, dir.Path, new[] { "/nologo", "a.cs", "/keyFile:key.snk", "/features:UseLegacyStrongNameProvider" });
            var comp = cmd.CreateCompilation(TextWriter.Null, new TouchedFileLogger(), NullErrorLogger.Instance);

            Assert.Equal(typeof(DesktopStrongNameProvider), comp.Options.StrongNameProvider.GetType());
        }

        [Fact]
        public void CreateCompilationWithDisableLengthBasedSwitch()
        {
            string source = """
public class C
{
}
""";

            var fileName = "a.cs";
            var dir = Temp.CreateDirectory();
            var file = dir.CreateFile(fileName);
            file.WriteAllText(source);

            var cmd = CreateCSharpCompiler(null, dir.Path, new[] { "a.cs", "/features:disable-length-based-switch" });
            var comp = cmd.CreateCompilation(TextWriter.Null, new TouchedFileLogger(), NullErrorLogger.Instance);
            Assert.True(((CSharpCompilation)comp).FeatureDisableLengthBasedSwitch);

            cmd = CreateCSharpCompiler(null, dir.Path, new[] { "a.cs" });
            comp = cmd.CreateCompilation(TextWriter.Null, new TouchedFileLogger(), NullErrorLogger.Instance);
            Assert.False(((CSharpCompilation)comp).FeatureDisableLengthBasedSwitch);
        }

        [Fact]
        public void CreateCompilation_MainAndTargetIncompatibilities()
        {
            string source = @"
public class C
{
    public static void Main()
    {
    }
}";

            var fileName = "a.cs";
            var dir = Temp.CreateDirectory();
            var file = dir.CreateFile(fileName);
            file.WriteAllText(source);

            var compilation = CSharpCompilation.Create("a.dll", options: TestOptions.ReleaseDll);

            var options = compilation.Options;

            Assert.Equal(0, options.Errors.Length);

            options = options.WithMainTypeName("a");

            options.Errors.Verify(
    // error CS2017: Cannot specify /main if building a module or library
    Diagnostic(ErrorCode.ERR_NoMainOnDLL)
                );

            var comp = CSharpCompilation.Create("a.dll", options: options);

            comp.GetDiagnostics().Verify(
    // error CS2017: Cannot specify /main if building a module or library
    Diagnostic(ErrorCode.ERR_NoMainOnDLL)
                );

            options = options.WithOutputKind(OutputKind.WindowsApplication);
            options.Errors.Verify();

            comp = CSharpCompilation.Create("a.dll", options: options);
            comp.GetDiagnostics().Verify(
    // error CS1555: Could not find 'a' specified for Main method
    Diagnostic(ErrorCode.ERR_MainClassNotFound).WithArguments("a")
                );

            options = options.WithOutputKind(OutputKind.NetModule);
            options.Errors.Verify(
    // error CS2017: Cannot specify /main if building a module or library
    Diagnostic(ErrorCode.ERR_NoMainOnDLL)
                );

            comp = CSharpCompilation.Create("a.dll", options: options);
            comp.GetDiagnostics().Verify(
    // error CS2017: Cannot specify /main if building a module or library
    Diagnostic(ErrorCode.ERR_NoMainOnDLL)
                );

            options = options.WithMainTypeName(null);
            options.Errors.Verify();

            comp = CSharpCompilation.Create("a.dll", options: options);
            comp.GetDiagnostics().Verify();

            CleanupAllGeneratedFiles(file.Path);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/30328")]
        public void SpecifyProperCodePage()
        {
            byte[] source = {
                                0x63, // c
                                0x6c, // l
                                0x61, // a
                                0x73, // s
                                0x73, // s
                                0x20, //
                                0xd0, 0x96, // Utf-8 Cyrillic character
                                0x7b, // {
                                0x7d, // }
                            };

            var fileName = "a.cs";
            var dir = Temp.CreateDirectory();
            var file = dir.CreateFile(fileName);
            file.WriteAllBytes(source);

            var output = ProcessUtilities.RunAndGetOutput(s_CSharpCompilerExecutable, $"/nologo /t:library \"{file}\"", startFolder: dir.Path);
            Assert.Equal("", output); // Autodetected UTF-8, NO ERROR

            output = ProcessUtilities.RunAndGetOutput(s_CSharpCompilerExecutable, $"/nologo /preferreduilang:en /t:library /codepage:20127 \"{file}\"", expectedRetCode: 1, startFolder: dir.Path); // 20127: US-ASCII
            // 0xd0, 0x96 ==> ERROR
            Assert.Equal(@"
a.cs(1,7): error CS1001: Identifier expected
a.cs(1,7): error CS1514: { expected
a.cs(1,7): error CS1513: } expected
a.cs(1,7): error CS8803: Top-level statements must precede namespace and type declarations.
a.cs(1,7): error CS1525: Invalid expression term '??'
a.cs(1,9): error CS1525: Invalid expression term '{'
a.cs(1,9): error CS1002: ; expected
".Trim(),
                Regex.Replace(output, "^.*a.cs", "a.cs", RegexOptions.Multiline).Trim());

            CleanupAllGeneratedFiles(file.Path);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void DefaultWin32ResForExe()
        {
            var source = @"
class C
{
    static void Main() { }
}
";

            CheckManifestString(source, OutputKind.ConsoleApplication, explicitManifest: null, expectedManifest:
@"<?xml version=""1.0"" encoding=""utf-16""?>
<ManifestResource Size=""490"">
  <Contents><![CDATA[<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>

<assembly xmlns=""urn:schemas-microsoft-com:asm.v1"" manifestVersion=""1.0"">
  <assemblyIdentity version=""1.0.0.0"" name=""MyApplication.app""/>
  <trustInfo xmlns=""urn:schemas-microsoft-com:asm.v2"">
    <security>
      <requestedPrivileges xmlns=""urn:schemas-microsoft-com:asm.v3"">
        <requestedExecutionLevel level=""asInvoker"" uiAccess=""false""/>
      </requestedPrivileges>
    </security>
  </trustInfo>
</assembly>]]></Contents>
</ManifestResource>");
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void DefaultManifestForDll()
        {
            var source = @"
class C
{
}
";

            CheckManifestString(source, OutputKind.DynamicallyLinkedLibrary, explicitManifest: null, expectedManifest: null);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void DefaultManifestForWinExe()
        {
            var source = @"
class C
{
    static void Main() { }
}
";

            CheckManifestString(source, OutputKind.WindowsApplication, explicitManifest: null, expectedManifest:
@"<?xml version=""1.0"" encoding=""utf-16""?>
<ManifestResource Size=""490"">
  <Contents><![CDATA[<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>

<assembly xmlns=""urn:schemas-microsoft-com:asm.v1"" manifestVersion=""1.0"">
  <assemblyIdentity version=""1.0.0.0"" name=""MyApplication.app""/>
  <trustInfo xmlns=""urn:schemas-microsoft-com:asm.v2"">
    <security>
      <requestedPrivileges xmlns=""urn:schemas-microsoft-com:asm.v3"">
        <requestedExecutionLevel level=""asInvoker"" uiAccess=""false""/>
      </requestedPrivileges>
    </security>
  </trustInfo>
</assembly>]]></Contents>
</ManifestResource>");
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void DefaultManifestForAppContainerExe()
        {
            var source = @"
class C
{
    static void Main() { }
}
";

            CheckManifestString(source, OutputKind.WindowsRuntimeApplication, explicitManifest: null, expectedManifest:
@"<?xml version=""1.0"" encoding=""utf-16""?>
<ManifestResource Size=""490"">
  <Contents><![CDATA[<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>

<assembly xmlns=""urn:schemas-microsoft-com:asm.v1"" manifestVersion=""1.0"">
  <assemblyIdentity version=""1.0.0.0"" name=""MyApplication.app""/>
  <trustInfo xmlns=""urn:schemas-microsoft-com:asm.v2"">
    <security>
      <requestedPrivileges xmlns=""urn:schemas-microsoft-com:asm.v3"">
        <requestedExecutionLevel level=""asInvoker"" uiAccess=""false""/>
      </requestedPrivileges>
    </security>
  </trustInfo>
</assembly>]]></Contents>
</ManifestResource>");
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void DefaultManifestForWinMD()
        {
            var source = @"
class C
{
}
";

            CheckManifestString(source, OutputKind.WindowsRuntimeMetadata, explicitManifest: null, expectedManifest: null);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void DefaultWin32ResForModule()
        {
            var source = @"
class C
{
}
";

            CheckManifestString(source, OutputKind.NetModule, explicitManifest: null, expectedManifest: null);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void ExplicitWin32ResForExe()
        {
            var source = @"
class C
{
    static void Main() { }
}
";

            var explicitManifest =
@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<assembly xmlns=""urn:schemas-microsoft-com:asm.v1"" manifestVersion=""1.0"">
  <assemblyIdentity version=""1.0.0.0"" name=""Test.app""/>
  <trustInfo xmlns=""urn:schemas-microsoft-com:asm.v2"">
    <security>
      <requestedPrivileges xmlns=""urn:schemas-microsoft-com:asm.v3"">
        <requestedExecutionLevel level=""asInvoker"" uiAccess=""false""/>
      </requestedPrivileges>
    </security>
  </trustInfo>
</assembly>";

            var explicitManifestStream = new MemoryStream(Encoding.UTF8.GetBytes(explicitManifest));

            var expectedManifest =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<ManifestResource Size=""476"">
  <Contents><![CDATA[" +
explicitManifest +
@"]]></Contents>
</ManifestResource>";

            CheckManifestString(source, OutputKind.ConsoleApplication, explicitManifest, expectedManifest);
        }

        // DLLs don't get the default manifest, but they do respect explicitly set manifests.
        [ConditionalFact(typeof(WindowsOnly))]
        public void ExplicitWin32ResForDll()
        {
            var source = @"
class C
{
    static void Main() { }
}
";

            var explicitManifest =
@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<assembly xmlns=""urn:schemas-microsoft-com:asm.v1"" manifestVersion=""1.0"">
  <assemblyIdentity version=""1.0.0.0"" name=""Test.app""/>
  <trustInfo xmlns=""urn:schemas-microsoft-com:asm.v2"">
    <security>
      <requestedPrivileges xmlns=""urn:schemas-microsoft-com:asm.v3"">
        <requestedExecutionLevel level=""asInvoker"" uiAccess=""false""/>
      </requestedPrivileges>
    </security>
  </trustInfo>
</assembly>";

            var expectedManifest =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<ManifestResource Size=""476"">
  <Contents><![CDATA[" +
explicitManifest +
@"]]></Contents>
</ManifestResource>";

            CheckManifestString(source, OutputKind.DynamicallyLinkedLibrary, explicitManifest, expectedManifest);
        }

        // Modules don't have manifests, even if one is explicitly specified.
        [ConditionalFact(typeof(WindowsOnly))]
        public void ExplicitWin32ResForModule()
        {
            var source = @"
class C
{
}
";

            var explicitManifest =
@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<assembly xmlns=""urn:schemas-microsoft-com:asm.v1"" manifestVersion=""1.0"">
  <assemblyIdentity version=""1.0.0.0"" name=""Test.app""/>
  <trustInfo xmlns=""urn:schemas-microsoft-com:asm.v2"">
    <security>
      <requestedPrivileges xmlns=""urn:schemas-microsoft-com:asm.v3"">
        <requestedExecutionLevel level=""asInvoker"" uiAccess=""false""/>
      </requestedPrivileges>
    </security>
  </trustInfo>
</assembly>";

            CheckManifestString(source, OutputKind.NetModule, explicitManifest, expectedManifest: null);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary([In] IntPtr hFile);

        private void CheckManifestString(string source, OutputKind outputKind, string explicitManifest, string expectedManifest)
        {
            var dir = Temp.CreateDirectory();
            var sourceFile = dir.CreateFile("Test.cs").WriteAllText(source);

            string outputFileName;
            string target;
            switch (outputKind)
            {
                case OutputKind.ConsoleApplication:
                    outputFileName = "Test.exe";
                    target = "exe";
                    break;
                case OutputKind.WindowsApplication:
                    outputFileName = "Test.exe";
                    target = "winexe";
                    break;
                case OutputKind.DynamicallyLinkedLibrary:
                    outputFileName = "Test.dll";
                    target = "library";
                    break;
                case OutputKind.NetModule:
                    outputFileName = "Test.netmodule";
                    target = "module";
                    break;
                case OutputKind.WindowsRuntimeMetadata:
                    outputFileName = "Test.winmdobj";
                    target = "winmdobj";
                    break;
                case OutputKind.WindowsRuntimeApplication:
                    outputFileName = "Test.exe";
                    target = "appcontainerexe";
                    break;
                default:
                    throw TestExceptionUtilities.UnexpectedValue(outputKind);
            }

            MockCSharpCompiler csc;
            if (explicitManifest == null)
            {
                csc = CreateCSharpCompiler(null, dir.Path, new[]
                {
                    string.Format("/target:{0}", target),
                    string.Format("/out:{0}", outputFileName),
                    Path.GetFileName(sourceFile.Path),
                });
            }
            else
            {
                var manifestFile = dir.CreateFile("Test.config").WriteAllText(explicitManifest);
                csc = CreateCSharpCompiler(null, dir.Path, new[]
                {
                    string.Format("/target:{0}", target),
                    string.Format("/out:{0}", outputFileName),
                    string.Format("/win32manifest:{0}", Path.GetFileName(manifestFile.Path)),
                    Path.GetFileName(sourceFile.Path),
                });
            }

            int actualExitCode = csc.Run(new StringWriter(CultureInfo.InvariantCulture));

            Assert.Equal(0, actualExitCode);

            //Open as data
            IntPtr lib = LoadLibraryEx(Path.Combine(dir.Path, outputFileName), IntPtr.Zero, 0x00000002);
            if (lib == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error());

            const string resourceType = "#24";
            var resourceId = outputKind == OutputKind.DynamicallyLinkedLibrary ? "#2" : "#1";

            uint manifestSize;
            if (expectedManifest == null)
            {
                Assert.Throws<Win32Exception>(() => Win32Res.GetResource(lib, resourceId, resourceType, out manifestSize));
            }
            else
            {
                IntPtr manifestResourcePointer = Win32Res.GetResource(lib, resourceId, resourceType, out manifestSize);
                string actualManifest = Win32Res.ManifestResourceToXml(manifestResourcePointer, manifestSize);
                Assert.Equal(expectedManifest, actualManifest);
            }

            FreeLibrary(lib);
        }

        [WorkItem(544926, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544926")]
        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/30289")]
        public void ResponseFilesWithNoconfig_01()
        {
            string source = Temp.CreateFile("a.cs").WriteAllText(@"
public class C
{
    public static void Main()
    {
        int x; // CS0168
    }
}").Path;

            string rsp = Temp.CreateFile().WriteAllText(@"
/warnaserror
").Path;
            // Checks the base case without /noconfig (expect to see error)
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(rsp, WorkingDirectory, new[] { source, "/preferreduilang:en" });
            int exitCode = csc.Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Contains("error CS0168: The variable 'x' is declared but never used\r\n", outWriter.ToString(), StringComparison.Ordinal);

            // Checks the case with /noconfig (expect to see warning, instead of error)
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            csc = CreateCSharpCompiler(rsp, WorkingDirectory, new[] { source, "/noconfig", "/preferreduilang:en" });
            exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Contains("warning CS0168: The variable 'x' is declared but never used\r\n", outWriter.ToString(), StringComparison.Ordinal);

            // Checks the case with /NOCONFIG (expect to see warning, instead of error)
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            csc = CreateCSharpCompiler(rsp, WorkingDirectory, new[] { source, "/NOCONFIG", "/preferreduilang:en" });
            exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Contains("warning CS0168: The variable 'x' is declared but never used\r\n", outWriter.ToString(), StringComparison.Ordinal);

            // Checks the case with -noconfig (expect to see warning, instead of error)
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            csc = CreateCSharpCompiler(rsp, WorkingDirectory, new[] { source, "-noconfig", "/preferreduilang:en" });
            exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Contains("warning CS0168: The variable 'x' is declared but never used\r\n", outWriter.ToString(), StringComparison.Ordinal);

            CleanupAllGeneratedFiles(source);
            CleanupAllGeneratedFiles(rsp);
        }

        [WorkItem(544926, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544926")]
        [ConditionalFact(typeof(WindowsOnly))]
        public void ResponseFilesWithNoconfig_02()
        {
            string source = Temp.CreateFile("a.cs").WriteAllText(@"
public class C
{
    public static void Main()
    {
    }
}").Path;

            string rsp = Temp.CreateFile().WriteAllText(@"
/noconfig
").Path;
            // Checks the case with /noconfig inside the response file (expect to see warning)
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(rsp, WorkingDirectory, new[] { source, "/preferreduilang:en" });
            int exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Contains("warning CS2023: Ignoring /noconfig option because it was specified in a response file\r\n", outWriter.ToString(), StringComparison.Ordinal);

            // Checks the case with /noconfig inside the response file as along with /nowarn (expect to see warning)
            // to verify that this warning is not suppressed by the /nowarn option (See MSDN).
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            csc = CreateCSharpCompiler(rsp, WorkingDirectory, new[] { source, "/preferreduilang:en", "/nowarn:2023" });
            exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Contains("warning CS2023: Ignoring /noconfig option because it was specified in a response file\r\n", outWriter.ToString(), StringComparison.Ordinal);

            CleanupAllGeneratedFiles(source);
            CleanupAllGeneratedFiles(rsp);
        }

        [WorkItem(544926, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544926")]
        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/30289")]
        public void ResponseFilesWithNoconfig_03()
        {
            string source = Temp.CreateFile("a.cs").WriteAllText(@"
public class C
{
    public static void Main()
    {
    }
}").Path;

            string rsp = Temp.CreateFile().WriteAllText(@"
/NOCONFIG
").Path;
            // Checks the case with /noconfig inside the response file (expect to see warning)
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(rsp, WorkingDirectory, new[] { source, "/preferreduilang:en" });
            int exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Contains("warning CS2023: Ignoring /noconfig option because it was specified in a response file\r\n", outWriter.ToString(), StringComparison.Ordinal);

            // Checks the case with /NOCONFIG inside the response file as along with /nowarn (expect to see warning)
            // to verify that this warning is not suppressed by the /nowarn option (See MSDN).
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            csc = CreateCSharpCompiler(rsp, WorkingDirectory, new[] { source, "/preferreduilang:en", "/nowarn:2023" });
            exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Contains("warning CS2023: Ignoring /noconfig option because it was specified in a response file\r\n", outWriter.ToString(), StringComparison.Ordinal);

            CleanupAllGeneratedFiles(source);
            CleanupAllGeneratedFiles(rsp);
        }

        [WorkItem(544926, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544926")]
        [ConditionalFact(typeof(WindowsOnly))]
        public void ResponseFilesWithNoconfig_04()
        {
            string source = Temp.CreateFile("a.cs").WriteAllText(@"
public class C
{
    public static void Main()
    {
    }
}").Path;

            string rsp = Temp.CreateFile().WriteAllText(@"
-noconfig
").Path;
            // Checks the case with /noconfig inside the response file (expect to see warning)
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(rsp, WorkingDirectory, new[] { source, "/preferreduilang:en" });
            int exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Contains("warning CS2023: Ignoring /noconfig option because it was specified in a response file\r\n", outWriter.ToString(), StringComparison.Ordinal);

            // Checks the case with -noconfig inside the response file as along with /nowarn (expect to see warning)
            // to verify that this warning is not suppressed by the /nowarn option (See MSDN).
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            csc = CreateCSharpCompiler(rsp, WorkingDirectory, new[] { source, "/preferreduilang:en", "/nowarn:2023" });
            exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Contains("warning CS2023: Ignoring /noconfig option because it was specified in a response file\r\n", outWriter.ToString(), StringComparison.Ordinal);

            CleanupAllGeneratedFiles(source);
            CleanupAllGeneratedFiles(rsp);
        }

        [Fact, WorkItem(530024, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530024")]
        public void NoStdLib()
        {
            var src = Temp.CreateFile("a.cs");

            src.WriteAllText("public class C{}");

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            int exitCode = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/nologo", "/t:library", src.ToString() }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString().Trim());

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/nologo", "/preferreduilang:en", "/nostdlib", "/t:library", src.ToString() }).Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Equal("{FILE}(1,14): error CS0518: Predefined type 'System.Object' is not defined or imported",
                         outWriter.ToString().Replace(Path.GetFileName(src.Path), "{FILE}").Trim());

            // Bug#15021: breaking change - empty source no error with /nostdlib
            src.WriteAllText("namespace System { }");
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/nologo", "/nostdlib", "/t:library", "/runtimemetadataversion:v4.0.30319", "/langversion:8", src.ToString() }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString().Trim());

            CleanupAllGeneratedFiles(src.Path);
        }

        private string GetDefaultResponseFilePath()
        {
            var cscRsp = global::TestResources.ResourceLoader.GetResourceBlob("csc.rsp");
            return Temp.CreateFile().WriteAllBytes(cscRsp).Path;
        }

        [Fact, WorkItem(530359, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530359")]
        public void NoStdLib02()
        {
            #region "source"
            var source = @"
// <Title>A collection initializer can be declared with a user-defined IEnumerable that is declared in a user-defined System.Collections</Title>
using System.Collections;

class O<T> where T : new()
{
    public T list = new T();
}

class C
{
    static StructCollection sc = new StructCollection { 1 };
    public static int Main()
    {
        ClassCollection cc = new ClassCollection { 2 };
        var o1 = new O<ClassCollection> { list = { 5 } };
        var o2 = new O<StructCollection> { list = sc };
        return 0;
    }
}

struct StructCollection : IEnumerable
{
    public int added;
#region IEnumerable Members
    public void Add(int t)
    {
        added = t;
    }
#endregion
}

class ClassCollection : IEnumerable
{
    public int added;
#region IEnumerable Members
    public void Add(int t)
    {
        added = t;
    }
#endregion
}

namespace System.Collections
{
    public interface IEnumerable
    {
        void Add(int t);
    }
}
";
            #endregion

            #region "mslib"
            var mslib = @"
namespace System
{
    public class Object {}
    public struct Byte { }
    public struct Int16 { }
    public struct Int32 { }
    public struct Int64 { }
    public struct Single { }
    public struct Double { }
    public struct SByte { }
    public struct UInt32 { }
    public struct UInt64 { }
    public struct Char { }
    public struct Boolean { }
    public struct UInt16 { }
    public struct UIntPtr { }
    public struct IntPtr { }
    public class Delegate { }
    public class String {
        public int Length    {    get { return 10; }    }
    }
    public class MulticastDelegate { }
    public class Array { }
    public class Exception { public Exception(string s){} }
    public class Type { }
    public class ValueType { }
    public class Enum { }
    public interface IEnumerable { }
    public interface IDisposable { }
    public class Attribute { }
    public class ParamArrayAttribute { }
    public struct Void { }
    public struct RuntimeFieldHandle { }
    public struct RuntimeTypeHandle { }
    public class Activator
    {
         public static T CreateInstance<T>(){return default(T);}
    }

    namespace Collections
    {
        public interface IEnumerator { }
    }

    namespace Runtime
    {
        namespace InteropServices
        {
            public class OutAttribute { }
        }

        namespace CompilerServices
        {
            public class RuntimeHelpers { }
        }
    }

    namespace Reflection
    {
        public class DefaultMemberAttribute { }
    }
}
";
            #endregion

            var src = Temp.CreateFile("NoStdLib02.cs");
            src.WriteAllText(source + mslib);

            Assert.Equal("noRefSafetyRulesAttribute", Feature.NoRefSafetyRulesAttribute);
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            int exitCode = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/nologo", "/noconfig", "/nostdlib", "/runtimemetadataversion:v4.0.30319", "/nowarn:8625", "/features:noRefSafetyRulesAttribute", src.ToString() }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString().Trim());

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/nologo", "/nostdlib", "/runtimemetadataversion:v4.0.30319", "/nowarn:8625", "/features:noRefSafetyRulesAttribute", src.ToString() }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString().Trim());
            string OriginalSource = src.Path;

            src = Temp.CreateFile("NoStdLib02b.cs");
            src.WriteAllText(mslib);
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = CreateCSharpCompiler(GetDefaultResponseFilePath(), WorkingDirectory, new[] { "/nologo", "/noconfig", "/nostdlib", "/t:library", "/runtimemetadataversion:v4.0.30319", "/nowarn:8625", "/features:noRefSafetyRulesAttribute", src.ToString() }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString().Trim());

            CleanupAllGeneratedFiles(OriginalSource);
            CleanupAllGeneratedFiles(src.Path);
        }

        [Fact, WorkItem(546018, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546018"), WorkItem(546020, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546020"), WorkItem(546024, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546024"), WorkItem(546049, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546049")]
        public void InvalidDefineSwitch()
        {
            var src = Temp.CreateFile("a.cs");

            src.WriteAllText("public class C{}");

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            int exitCode = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/nologo", "/preferreduilang:en", src.ToString(), "/define" }).Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Equal("error CS2006: Command-line syntax error: Missing '<text>' for '/define' option", outWriter.ToString().Trim());

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/nologo", "/preferreduilang:en", "/t:library", src.ToString(), @"/define:""""" }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("warning CS2029: Invalid name for a preprocessing symbol; '' is not a valid identifier", outWriter.ToString().Trim());

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/nologo", "/preferreduilang:en", "/t:library", src.ToString(), "/define: " }).Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Equal("error CS2006: Command-line syntax error: Missing '<text>' for '/define:' option", outWriter.ToString().Trim());

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/nologo", "/preferreduilang:en", "/t:library", src.ToString(), "/define:" }).Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Equal("error CS2006: Command-line syntax error: Missing '<text>' for '/define:' option", outWriter.ToString().Trim());

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/nologo", "/preferreduilang:en", "/t:library", src.ToString(), "/define:,,," }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("warning CS2029: Invalid name for a preprocessing symbol; '' is not a valid identifier", outWriter.ToString().Trim());

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/nologo", "/preferreduilang:en", "/t:library", src.ToString(), "/define:,blah,Blah" }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("warning CS2029: Invalid name for a preprocessing symbol; '' is not a valid identifier", outWriter.ToString().Trim());

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/nologo", "/preferreduilang:en", "/t:library", src.ToString(), "/define:a;;b@" }).Run(outWriter);
            Assert.Equal(0, exitCode);
            var errorLines = outWriter.ToString().Trim().Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            Assert.Equal("warning CS2029: Invalid name for a preprocessing symbol; '' is not a valid identifier", errorLines[0]);
            Assert.Equal("warning CS2029: Invalid name for a preprocessing symbol; 'b@' is not a valid identifier", errorLines[1]);

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/nologo", "/preferreduilang:en", "/t:library", src.ToString(), "/define:a,b@;" }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("warning CS2029: Invalid name for a preprocessing symbol; 'b@' is not a valid identifier", outWriter.ToString().Trim());

            //Bug 531612 - Native would normally not give the 2nd warning
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/nologo", "/preferreduilang:en", "/t:library", src.ToString(), @"/define:OE_WIN32=-1:LANG_HOST_EN=-1:LANG_OE_EN=-1:LANG_PRJ_EN=-1:HOST_COM20SDKEVERETT=-1:EXEMODE=-1:OE_NT5=-1:Win32=-1", @"/d:TRACE=TRUE,DEBUG=TRUE" }).Run(outWriter);
            Assert.Equal(0, exitCode);
            errorLines = outWriter.ToString().Trim().Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            Assert.Equal(@"warning CS2029: Invalid name for a preprocessing symbol; 'OE_WIN32=-1:LANG_HOST_EN=-1:LANG_OE_EN=-1:LANG_PRJ_EN=-1:HOST_COM20SDKEVERETT=-1:EXEMODE=-1:OE_NT5=-1:Win32=-1' is not a valid identifier", errorLines[0]);
            Assert.Equal(@"warning CS2029: Invalid name for a preprocessing symbol; 'TRACE=TRUE' is not a valid identifier", errorLines[1]);

            CleanupAllGeneratedFiles(src.Path);
        }

        [WorkItem(733242, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/733242")]
        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/30321")]
        public void Bug733242()
        {
            var dir = Temp.CreateDirectory();

            var src = dir.CreateFile("a.cs");
            src.WriteAllText(
@"
/// <summary>ABC...XYZ</summary>
class C {} ");

            var xml = dir.CreateFile("a.xml");
            xml.WriteAllText("EMPTY");

            using (var xmlFileHandle = File.Open(xml.ToString(), FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.ReadWrite))
            {
                var output = ProcessUtilities.RunAndGetOutput(s_CSharpCompilerExecutable, String.Format("/nologo /t:library /doc:\"{1}\" \"{0}\"", src.ToString(), xml.ToString()), startFolder: dir.ToString());
                Assert.Equal("", output.Trim());

                Assert.True(File.Exists(Path.Combine(dir.ToString(), "a.xml")));

                using (var reader = new StreamReader(xmlFileHandle))
                {
                    var content = reader.ReadToEnd();
                    Assert.Equal(
@"<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>a</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <summary>ABC...XYZ</summary>
        </member>
    </members>
</doc>".Trim(), content.Trim());
                }
            }

            CleanupAllGeneratedFiles(src.Path);
            CleanupAllGeneratedFiles(xml.Path);
        }

        [WorkItem(768605, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/768605")]
        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/30321")]
        public void Bug768605()
        {
            var dir = Temp.CreateDirectory();

            var src = dir.CreateFile("a.cs");
            src.WriteAllText(
@"
/// <summary>ABC</summary>
class C {}
/// <summary>XYZ</summary>
class E {}
");

            var xml = dir.CreateFile("a.xml");
            xml.WriteAllText("EMPTY");

            var output = ProcessUtilities.RunAndGetOutput(s_CSharpCompilerExecutable, String.Format("/nologo /t:library /doc:\"{1}\" \"{0}\"", src.ToString(), xml.ToString()), startFolder: dir.ToString());
            Assert.Equal("", output.Trim());

            using (var reader = new StreamReader(xml.ToString()))
            {
                var content = reader.ReadToEnd();
                Assert.Equal(
@"<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>a</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <summary>ABC</summary>
        </member>
        <member name=""T:E"">
            <summary>XYZ</summary>
        </member>
    </members>
</doc>".Trim(), content.Trim());
            }

            src.WriteAllText(
@"
/// <summary>ABC</summary>
class C {}
");

            output = ProcessUtilities.RunAndGetOutput(s_CSharpCompilerExecutable, String.Format("/nologo /t:library /doc:\"{1}\" \"{0}\"", src.ToString(), xml.ToString()), startFolder: dir.ToString());
            Assert.Equal("", output.Trim());

            using (var reader = new StreamReader(xml.ToString()))
            {
                var content = reader.ReadToEnd();
                Assert.Equal(
@"<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>a</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <summary>ABC</summary>
        </member>
    </members>
</doc>".Trim(), content.Trim());
            }

            CleanupAllGeneratedFiles(src.Path);
            CleanupAllGeneratedFiles(xml.Path);
        }

        [Fact]
        public void ParseFullpaths()
        {
            var parsedArgs = DefaultParse(new[] { "a.cs" }, WorkingDirectory);
            Assert.False(parsedArgs.PrintFullPaths);

            parsedArgs = DefaultParse(new[] { "a.cs", "/fullpaths" }, WorkingDirectory);
            Assert.True(parsedArgs.PrintFullPaths);

            parsedArgs = DefaultParse(new[] { "a.cs", "/fullpaths:" }, WorkingDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.ERR_BadSwitch, parsedArgs.Errors.First().Code);

            parsedArgs = DefaultParse(new[] { "a.cs", "/fullpaths: " }, WorkingDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.ERR_BadSwitch, parsedArgs.Errors.First().Code);

            parsedArgs = DefaultParse(new[] { "a.cs", "/fullpaths+" }, WorkingDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.ERR_BadSwitch, parsedArgs.Errors.First().Code);

            parsedArgs = DefaultParse(new[] { "a.cs", "/fullpaths+:" }, WorkingDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.ERR_BadSwitch, parsedArgs.Errors.First().Code);
        }

        [Fact]
        public void CheckFullpaths()
        {
            string source = Temp.CreateFile(prefix: "", extension: ".cs").WriteAllText(@"
public class C
{
    public static void Main()
    {
        string x;
    }
}").Path;

            var baseDir = Path.GetDirectoryName(source);
            var fileName = Path.GetFileName(source);

            // Checks the base case without /fullpaths (expect to see relative path name)
            //      c:\temp> csc.exe c:\temp\a.cs
            //      a.cs(6,16): warning CS0168: The variable 'x' is declared but never used
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(null, baseDir, new[] { source, "/preferreduilang:en" });
            int exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Contains(fileName + "(6,16): warning CS0168: The variable 'x' is declared but never used", outWriter.ToString(), StringComparison.Ordinal);

            // Checks the base case without /fullpaths when the file is located in the sub-folder (expect to see relative path name)
            //      c:\temp> csc.exe c:\temp\example\a.cs
            //      example\a.cs(6,16): warning CS0168: The variable 'x' is declared but never used
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            csc = CreateCSharpCompiler(null, Directory.GetParent(baseDir).FullName, new[] { source, "/preferreduilang:en" });
            exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Contains(fileName + "(6,16): warning CS0168: The variable 'x' is declared but never used", outWriter.ToString(), StringComparison.Ordinal);
            Assert.DoesNotContain(source, outWriter.ToString(), StringComparison.Ordinal);

            // Checks the base case without /fullpaths when the file is not located under the base directory (expect to see the full path name)
            //      c:\temp> csc.exe c:\test\a.cs
            //      c:\test\a.cs(6,16): warning CS0168: The variable 'x' is declared but never used
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            csc = CreateCSharpCompiler(null, Temp.CreateDirectory().Path, new[] { source, "/preferreduilang:en" });
            exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Contains(source + "(6,16): warning CS0168: The variable 'x' is declared but never used", outWriter.ToString(), StringComparison.Ordinal);

            // Checks the case with /fullpaths (expect to see the full paths)
            //      c:\temp> csc.exe c:\temp\a.cs /fullpaths
            //      c:\temp\a.cs(6,16): warning CS0168: The variable 'x' is declared but never used
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            csc = CreateCSharpCompiler(null, baseDir, new[] { source, "/fullpaths", "/preferreduilang:en" });
            exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Contains(source + @"(6,16): warning CS0168: The variable 'x' is declared but never used", outWriter.ToString(), StringComparison.Ordinal);

            // Checks the base case without /fullpaths when the file is located in the sub-folder (expect to see the full path name)
            //      c:\temp> csc.exe c:\temp\example\a.cs /fullpaths
            //      c:\temp\example\a.cs(6,16): warning CS0168: The variable 'x' is declared but never used
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            csc = CreateCSharpCompiler(null, Directory.GetParent(baseDir).FullName, new[] { source, "/preferreduilang:en", "/fullpaths" });
            exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Contains(source + "(6,16): warning CS0168: The variable 'x' is declared but never used", outWriter.ToString(), StringComparison.Ordinal);

            // Checks the base case without /fullpaths when the file is not located under the base directory (expect to see the full path name)
            //      c:\temp> csc.exe c:\test\a.cs /fullpaths
            //      c:\test\a.cs(6,16): warning CS0168: The variable 'x' is declared but never used
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            csc = CreateCSharpCompiler(null, Temp.CreateDirectory().Path, new[] { source, "/preferreduilang:en", "/fullpaths" });
            exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Contains(source + "(6,16): warning CS0168: The variable 'x' is declared but never used", outWriter.ToString(), StringComparison.Ordinal);

            CleanupAllGeneratedFiles(source);
            CleanupAllGeneratedFiles(Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(source)), Path.GetFileName(source)));
        }

        [Fact]
        public void DefaultResponseFile()
        {
            var sdkDirectory = SdkDirectory;
            MockCSharpCompiler csc = new MockCSharpCompiler(
                GetDefaultResponseFilePath(),
                RuntimeUtilities.CreateBuildPaths(WorkingDirectory, sdkDirectory),
                new string[0]);
            AssertEx.Equal(csc.Arguments.MetadataReferences.Select(r => r.Reference), new string[]
            {
                MscorlibFullPath,
                "Accessibility.dll",
                "Microsoft.CSharp.dll",
                "System.Configuration.dll",
                "System.Configuration.Install.dll",
                "System.Core.dll",
                "System.Data.dll",
                "System.Data.DataSetExtensions.dll",
                "System.Data.Linq.dll",
                "System.Data.OracleClient.dll",
                "System.Deployment.dll",
                "System.Design.dll",
                "System.DirectoryServices.dll",
                "System.dll",
                "System.Drawing.Design.dll",
                "System.Drawing.dll",
                "System.EnterpriseServices.dll",
                "System.Management.dll",
                "System.Messaging.dll",
                "System.Runtime.Remoting.dll",
                "System.Runtime.Serialization.dll",
                "System.Runtime.Serialization.Formatters.Soap.dll",
                "System.Security.dll",
                "System.ServiceModel.dll",
                "System.ServiceModel.Web.dll",
                "System.ServiceProcess.dll",
                "System.Transactions.dll",
                "System.Web.dll",
                "System.Web.Extensions.Design.dll",
                "System.Web.Extensions.dll",
                "System.Web.Mobile.dll",
                "System.Web.RegularExpressions.dll",
                "System.Web.Services.dll",
                "System.Windows.Forms.dll",
                "System.Workflow.Activities.dll",
                "System.Workflow.ComponentModel.dll",
                "System.Workflow.Runtime.dll",
                "System.Xml.dll",
                "System.Xml.Linq.dll",
            }, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void DefaultResponseFileNoConfig()
        {
            MockCSharpCompiler csc = CreateCSharpCompiler(GetDefaultResponseFilePath(), WorkingDirectory, new[] { "/noconfig" });
            Assert.Equal(csc.Arguments.MetadataReferences.Select(r => r.Reference), new string[]
            {
                MscorlibFullPath,
            }, StringComparer.OrdinalIgnoreCase);
        }

        [Fact, WorkItem(545954, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545954")]
        public void TestFilterParseDiagnostics()
        {
            string source = Temp.CreateFile(prefix: "", extension: ".cs").WriteAllText(@"
#pragma warning disable 440
using global = A; // CS0440
class A
{
static void Main() {
#pragma warning suppress 440
}
}").Path;

            var baseDir = Path.GetDirectoryName(source);
            var fileName = Path.GetFileName(source);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            int exitCode = CreateCSharpCompiler(null, baseDir, new[] { "/nologo", "/preferreduilang:en", source.ToString() }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal(Path.GetFileName(source) + "(7,17): warning CS1634: Expected 'disable' or 'restore'", outWriter.ToString().Trim());

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = CreateCSharpCompiler(null, baseDir, new[] { "/nologo", "/nowarn:1634", source.ToString() }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString().Trim());

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = CreateCSharpCompiler(null, baseDir, new[] { "/nologo", "/preferreduilang:en", Path.Combine(baseDir, "nonexistent.cs"), source.ToString() }).Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Equal("error CS2001: Source file '" + Path.Combine(baseDir, "nonexistent.cs") + "' could not be found.", outWriter.ToString().Trim());

            CleanupAllGeneratedFiles(source);
        }

        [Fact, WorkItem(546058, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546058")]
        public void TestNoWarnParseDiagnostics()
        {
            string source = Temp.CreateFile(prefix: "", extension: ".cs").WriteAllText(@"
class Test
{
 static void Main()
 {
  //Generates warning CS1522: Empty switch block
  switch (1)   { }

  //Generates warning CS0642: Possible mistaken empty statement
  while (false) ;
  {  }
 }
}
").Path;

            var baseDir = Path.GetDirectoryName(source);
            var fileName = Path.GetFileName(source);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            int exitCode = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/nologo", "/nowarn:1522,642", source.ToString() }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString().Trim());

            CleanupAllGeneratedFiles(source);
        }

        [Fact, WorkItem(41610, "https://github.com/dotnet/roslyn/issues/41610")]
        public void TestWarnAsError_CS8632()
        {
            string source = Temp.CreateFile(prefix: "", extension: ".cs").WriteAllText(@"
public class C
{
    public string? field;
    public static void Main()
    {
    }
}
").Path;

            var baseDir = Path.GetDirectoryName(source);
            var fileName = Path.GetFileName(source);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            int exitCode = CreateCSharpCompiler(null, baseDir, new[] { "/nologo", "/preferreduilang:en", "/warn:3", "/warnaserror:nullable", source.ToString() }).Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Equal(
$@"{fileName}(4,18): error CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.", outWriter.ToString().Trim());

            CleanupAllGeneratedFiles(source);
        }

        [Fact, WorkItem(546076, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546076")]
        public void TestWarnAsError_CS1522()
        {
            string source = Temp.CreateFile(prefix: "", extension: ".cs").WriteAllText(@"
public class Test
{
    // CS0169 (level 3)
    private int x;
    // CS0109 (level 4)
    public new void Method() { }
    public static int Main()
    {
        int i = 5;
        // CS1522 (level 1)
        switch (i) { }
        return 0;
        // CS0162 (level 2)
        i = 6;
    }
}
").Path;

            var baseDir = Path.GetDirectoryName(source);
            var fileName = Path.GetFileName(source);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            int exitCode = CreateCSharpCompiler(null, baseDir, new[] { "/nologo", "/preferreduilang:en", "/warn:3", "/warnaserror", source.ToString() }).Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Equal(
$@"{fileName}(12,20): error CS1522: Empty switch block
{fileName}(15,9): error CS0162: Unreachable code detected
{fileName}(5,17): error CS0169: The field 'Test.x' is never used", outWriter.ToString().Trim());

            CleanupAllGeneratedFiles(source);
        }

        [Fact, WorkItem(546025, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546025")]
        public void TestWin32ResWithBadResFile_CS1583ERR_BadWin32Res_01()
        {
            string source = Temp.CreateFile(prefix: "", extension: ".cs").WriteAllText(@"class Test { static void Main() {} }").Path;
            string badres = Temp.CreateFile().WriteAllBytes(TestResources.DiagnosticTests.badresfile).Path;

            var baseDir = Path.GetDirectoryName(source);
            var fileName = Path.GetFileName(source);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            int exitCode = CreateCSharpCompiler(null, baseDir, new[]
            {
                "/nologo",
                "/preferreduilang:en",
                "/win32res:" + badres,
                source
            }).Run(outWriter);

            Assert.Equal(1, exitCode);
            // https://github.com/dotnet/roslyn/issues/79351: This used to write "Image is too small." instead of "Unknown file format."
            Assert.Equal("error CS1583: Error reading Win32 resources -- Unknown file format.", outWriter.ToString().Trim());

            CleanupAllGeneratedFiles(source);
            CleanupAllGeneratedFiles(badres);
        }

        [Fact(), WorkItem(217718, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=217718")]
        public void TestWin32ResWithBadResFile_CS1583ERR_BadWin32Res_02()
        {
            string source = Temp.CreateFile(prefix: "", extension: ".cs").WriteAllText(@"class Test { static void Main() {} }").Path;
            string badres = Temp.CreateFile().WriteAllBytes(new byte[] { 0, 0 }).Path;

            var baseDir = Path.GetDirectoryName(source);
            var fileName = Path.GetFileName(source);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            int exitCode = CreateCSharpCompiler(null, baseDir, new[]
            {
                "/nologo",
                "/preferreduilang:en",
                "/win32res:" + badres,
                source
            }).Run(outWriter);

            Assert.Equal(1, exitCode);
            Assert.Equal("error CS1583: Error reading Win32 resources -- Unrecognized resource file format.", outWriter.ToString().Trim());

            CleanupAllGeneratedFiles(source);
            CleanupAllGeneratedFiles(badres);
        }

        [Fact, WorkItem(546114, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546114")]
        public void TestFilterCommandLineDiagnostics()
        {
            string source = Temp.CreateFile(prefix: "", extension: ".cs").WriteAllText(@"
class A
{
static void Main() { }
}").Path;
            var baseDir = Path.GetDirectoryName(source);
            var fileName = Path.GetFileName(source);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            int exitCode = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/nologo", "/target:library", "/out:goo.dll", "/nowarn:2008" }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString().Trim());

            System.IO.File.Delete(System.IO.Path.Combine(baseDir, "goo.dll"));
            CleanupAllGeneratedFiles(source);
        }

        [Fact, WorkItem(546452, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546452")]
        public void CS1691WRN_BadWarningNumber_Bug15905()
        {
            string source = Temp.CreateFile(prefix: "", extension: ".cs").WriteAllText(@"
class Program
{
        public static void Main() { }
} ").Path;
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);

            // Repro case 1
            int exitCode = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/nologo", "/warnaserror", source.ToString() }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString().Trim());

            // Repro case 2
            exitCode = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/nologo", "/nowarn:1998", source.ToString() }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString().Trim());

            CleanupAllGeneratedFiles(source);
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void ExistingPdb()
        {
            var dir = Temp.CreateDirectory();

            var source1 = dir.CreateFile("program1.cs").WriteAllText(@"
class " + new string('a', 10000) + @"
{
    public static void Main()
    {
    }
}");
            var source2 = dir.CreateFile("program2.cs").WriteAllText(@"
class Program2
{
        public static void Main() { }
}");
            var source3 = dir.CreateFile("program3.cs").WriteAllText(@"
class Program3
{
        public static void Main() { }
}");

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);

            int oldSize = 16 * 1024;

            var exe = dir.CreateFile("Program.exe");
            using (var stream = File.OpenWrite(exe.Path))
            {
                byte[] buffer = new byte[oldSize];
                stream.Write(buffer, 0, buffer.Length);
            }

            var pdb = dir.CreateFile("Program.pdb");
            using (var stream = File.OpenWrite(pdb.Path))
            {
                byte[] buffer = new byte[oldSize];
                stream.Write(buffer, 0, buffer.Length);
            }

            int exitCode1 = CreateCSharpCompiler(null, dir.Path, new[] { "/debug:full", "/out:Program.exe", source1.Path }).Run(outWriter);
            Assert.NotEqual(0, exitCode1);

            ValidateZeroes(exe.Path, oldSize);
            ValidateZeroes(pdb.Path, oldSize);

            int exitCode2 = CreateCSharpCompiler(null, dir.Path, new[] { "/debug:full", "/out:Program.exe", source2.Path }).Run(outWriter);
            Assert.Equal(0, exitCode2);

            using (var peFile = File.OpenRead(exe.Path))
            {
                PdbValidation.ValidateDebugDirectory(peFile, null, pdb.Path, hashAlgorithm: default, hasEmbeddedPdb: false, isDeterministic: false);
            }

            Assert.True(new FileInfo(exe.Path).Length < oldSize);
            Assert.True(new FileInfo(pdb.Path).Length < oldSize);

            int exitCode3 = CreateCSharpCompiler(null, dir.Path, new[] { "/debug:full", "/out:Program.exe", source3.Path }).Run(outWriter);
            Assert.Equal(0, exitCode3);

            using (var peFile = File.OpenRead(exe.Path))
            {
                PdbValidation.ValidateDebugDirectory(peFile, null, pdb.Path, hashAlgorithm: default, hasEmbeddedPdb: false, isDeterministic: false);
            }
        }

        private static void ValidateZeroes(string path, int count)
        {
            using (var stream = File.OpenRead(path))
            {
                byte[] buffer = new byte[count];
                stream.Read(buffer, 0, buffer.Length);

                for (int i = 0; i < buffer.Length; i++)
                {
                    if (buffer[i] != 0)
                    {
                        Assert.True(false);
                    }
                }
            }
        }

        /// <summary>
        /// When the output file is open with <see cref="FileShare.Read"/> | <see cref="FileShare.Delete"/>
        /// the compiler should delete the file to unblock build while allowing the reader to continue
        /// reading the previous snapshot of the file content.
        ///
        /// On Windows we can read the original data directly from the stream without creating a memory map.
        /// </summary>
        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        public void FileShareDeleteCompatibility_Windows()
        {
            var dir = Temp.CreateDirectory();
            var libSrc = dir.CreateFile("Lib.cs").WriteAllText("class C { }");
            var libDll = dir.CreateFile("Lib.dll").WriteAllText("DLL");
            var libPdb = dir.CreateFile("Lib.pdb").WriteAllText("PDB");

            var fsDll = new FileStream(libDll.Path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
            var fsPdb = new FileStream(libPdb.Path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            int exitCode = CreateCSharpCompiler(null, dir.Path, new[] { "/target:library", "/debug:full", libSrc.Path }).Run(outWriter);
            if (exitCode != 0)
            {
                AssertEx.AssertEqualToleratingWhitespaceDifferences("", outWriter.ToString());
            }

            Assert.Equal(0, exitCode);

            AssertEx.Equal(new byte[] { 0x4D, 0x5A }, ReadBytes(libDll.Path, 2));
            AssertEx.Equal(new[] { (byte)'D', (byte)'L', (byte)'L' }, ReadBytes(fsDll, 3));

            AssertEx.Equal(new byte[] { 0x4D, 0x69 }, ReadBytes(libPdb.Path, 2));
            AssertEx.Equal(new[] { (byte)'P', (byte)'D', (byte)'B' }, ReadBytes(fsPdb, 3));

            fsDll.Dispose();
            fsPdb.Dispose();

            AssertEx.Equal(new[] { "Lib.cs", "Lib.dll", "Lib.pdb" }, Directory.GetFiles(dir.Path).Select(p => Path.GetFileName(p)).Order());
        }

        /// <summary>
        /// On Linux/Mac <see cref="FileShare.Delete"/> on its own doesn't do anything.
        /// We need to create the actual memory map. This works on Windows as well.
        /// </summary>
        [WorkItem(8896, "https://github.com/dotnet/roslyn/issues/8896")]
        [ConditionalFact(typeof(WindowsDesktopOnly), typeof(IsEnglishLocal), Reason = "https://github.com/dotnet/roslyn/issues/30321")]
        public void FileShareDeleteCompatibility_Xplat()
        {
            var bytes = TestResources.MetadataTests.InterfaceAndClass.CSClasses01;
            var mvid = ReadMvid(new MemoryStream(bytes));

            var dir = Temp.CreateDirectory();
            var libSrc = dir.CreateFile("Lib.cs").WriteAllText("class C { }");
            var libDll = dir.CreateFile("Lib.dll").WriteAllBytes(bytes);
            var libPdb = dir.CreateFile("Lib.pdb").WriteAllBytes(bytes);

            var fsDll = new FileStream(libDll.Path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
            var fsPdb = new FileStream(libPdb.Path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);

            var peDll = new PEReader(fsDll);
            var pePdb = new PEReader(fsPdb);

            // creates memory map view:
            var imageDll = peDll.GetEntireImage();
            var imagePdb = pePdb.GetEntireImage();

            var output = ProcessUtilities.RunAndGetOutput(s_CSharpCompilerExecutable, $"/target:library /debug:portable \"{libSrc.Path}\"", startFolder: dir.ToString());
            AssertEx.AssertEqualToleratingWhitespaceDifferences($@"
Microsoft (R) Visual C# Compiler version {s_compilerVersion}
Copyright (C) Microsoft Corporation. All rights reserved.", output);

            // reading original content from the memory map:
            Assert.Equal(mvid, ReadMvid(new MemoryStream(imageDll.GetContent().ToArray())));
            Assert.Equal(mvid, ReadMvid(new MemoryStream(imagePdb.GetContent().ToArray())));

            // reading original content directly from the streams:
            fsDll.Position = 0;
            fsPdb.Position = 0;
            Assert.Equal(mvid, ReadMvid(fsDll));
            Assert.Equal(mvid, ReadMvid(fsPdb));

            // reading new content from the file:
            using (var fsNewDll = File.OpenRead(libDll.Path))
            {
                Assert.NotEqual(mvid, ReadMvid(fsNewDll));
            }

            // Portable PDB metadata signature:
            AssertEx.Equal(new[] { (byte)'B', (byte)'S', (byte)'J', (byte)'B' }, ReadBytes(libPdb.Path, 4));

            // dispose PEReaders (they dispose the underlying file streams)
            peDll.Dispose();
            pePdb.Dispose();

            AssertEx.Equal(new[] { "Lib.cs", "Lib.dll", "Lib.pdb" }, Directory.GetFiles(dir.Path).Select(p => Path.GetFileName(p)).Order());

            // files can be deleted now:
            File.Delete(libSrc.Path);
            File.Delete(libDll.Path);
            File.Delete(libPdb.Path);

            // directory can be deleted (should be empty):
            Directory.Delete(dir.Path, recursive: false);
        }

        private static Guid ReadMvid(Stream stream)
        {
            using (var peReader = new PEReader(stream, PEStreamOptions.LeaveOpen))
            {
                var mdReader = peReader.GetMetadataReader();
                return mdReader.GetGuid(mdReader.GetModuleDefinition().Mvid);
            }
        }

        // Seems like File.SetAttributes(libDll.Path, FileAttributes.ReadOnly) doesn't restrict access to the file on Mac (Linux passes).
        [ConditionalFact(typeof(WindowsOnly)), WorkItem(8939, "https://github.com/dotnet/roslyn/issues/8939")]
        public void FileShareDeleteCompatibility_ReadOnlyFiles()
        {
            var dir = Temp.CreateDirectory();
            var libSrc = dir.CreateFile("Lib.cs").WriteAllText("class C { }");
            var libDll = dir.CreateFile("Lib.dll").WriteAllText("DLL");

            File.SetAttributes(libDll.Path, FileAttributes.ReadOnly);

            var fsDll = new FileStream(libDll.Path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            int exitCode = CreateCSharpCompiler(null, dir.Path, new[] { "/target:library", "/preferreduilang:en", libSrc.Path }).Run(outWriter);
            Assert.Contains($"error CS2012: Cannot open '{libDll.Path}' for writing", outWriter.ToString());

            AssertEx.Equal(new[] { (byte)'D', (byte)'L', (byte)'L' }, ReadBytes(libDll.Path, 3));
            AssertEx.Equal(new[] { (byte)'D', (byte)'L', (byte)'L' }, ReadBytes(fsDll, 3));

            fsDll.Dispose();

            AssertEx.Equal(new[] { "Lib.cs", "Lib.dll" }, Directory.GetFiles(dir.Path).Select(p => Path.GetFileName(p)).Order());
        }

        [Fact]
        public void FileShareDeleteCompatibility_ExistingDirectory()
        {
            var dir = Temp.CreateDirectory();
            var libSrc = dir.CreateFile("Lib.cs").WriteAllText("class C { }");
            var libDll = dir.CreateDirectory("Lib.dll");

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            int exitCode = CreateCSharpCompiler(null, dir.Path, new[] { "/target:library", "/preferreduilang:en", libSrc.Path }).Run(outWriter);
            Assert.Contains($"error CS2012: Cannot open '{libDll.Path}' for writing", outWriter.ToString());
        }

        private byte[] ReadBytes(Stream stream, int count)
        {
            var buffer = new byte[count];
            stream.Read(buffer, 0, count);
            return buffer;
        }

        private byte[] ReadBytes(string path, int count)
        {
            using (var stream = File.OpenRead(path))
            {
                return ReadBytes(stream, count);
            }
        }

        [Fact]
        public void IOFailure_DisposeOutputFile()
        {
            var srcPath = MakeTrivialExe(Temp.CreateDirectory().Path);
            var exePath = Path.Combine(Path.GetDirectoryName(srcPath), "test.exe");
            var csc = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/nologo", "/preferreduilang:en", $"/out:{exePath}", srcPath });
            csc.FileSystem = TestableFileSystem.CreateForStandard(openFileFunc: (file, mode, access, share) =>
            {
                if (file == exePath)
                {
                    return new TestStream(backingStream: new MemoryStream(),
                        dispose: () => { throw new IOException("Fake IOException"); });
                }

                return File.Open(file, mode, access, share);
            });

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            Assert.Equal(1, csc.Run(outWriter));
            Assert.Contains($"error CS0016: Could not write to output file '{exePath}' -- 'Fake IOException'{Environment.NewLine}", outWriter.ToString());
        }

        [Fact]
        public void IOFailure_DisposePdbFile()
        {
            var srcPath = MakeTrivialExe(Temp.CreateDirectory().Path);
            var exePath = Path.Combine(Path.GetDirectoryName(srcPath), "test.exe");
            var pdbPath = Path.ChangeExtension(exePath, "pdb");
            var csc = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/nologo", "/preferreduilang:en", "/debug", $"/out:{exePath}", srcPath });
            csc.FileSystem = TestableFileSystem.CreateForStandard(openFileFunc: (file, mode, access, share) =>
            {
                if (file == pdbPath)
                {
                    return new TestStream(backingStream: new MemoryStream(),
                        dispose: () => { throw new IOException("Fake IOException"); });
                }

                return File.Open(file, mode, access, share);
            });

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            Assert.Equal(1, csc.Run(outWriter));
            Assert.Contains($"error CS0016: Could not write to output file '{pdbPath}' -- 'Fake IOException'{Environment.NewLine}", outWriter.ToString());
        }

        [Fact]
        public void IOFailure_DisposeXmlFile()
        {
            var srcPath = MakeTrivialExe(Temp.CreateDirectory().Path);
            var xmlPath = Path.Combine(Path.GetDirectoryName(srcPath), "test.xml");
            var csc = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/nologo", "/preferreduilang:en", $"/doc:{xmlPath}", srcPath });
            csc.FileSystem = TestableFileSystem.CreateForStandard(openFileFunc: (file, mode, access, share) =>
            {
                if (file == xmlPath)
                {
                    return new TestStream(backingStream: new MemoryStream(),
                        dispose: () => { throw new IOException("Fake IOException"); });
                }

                return File.Open(file, mode, access, share);
            });

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            Assert.Equal(1, csc.Run(outWriter));
            Assert.Equal($"error CS0016: Could not write to output file '{xmlPath}' -- 'Fake IOException'{Environment.NewLine}", outWriter.ToString());
        }

        [Theory]
        [InlineData("portable")]
        [InlineData("full")]
        public void IOFailure_DisposeSourceLinkFile(string format)
        {
            var srcPath = MakeTrivialExe(Temp.CreateDirectory().Path);
            var sourceLinkPath = Path.Combine(Path.GetDirectoryName(srcPath), "test.json");
            var csc = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/nologo", "/preferreduilang:en", "/debug:" + format, $"/sourcelink:{sourceLinkPath}", srcPath });
            csc.FileSystem = TestableFileSystem.CreateForStandard(openFileFunc: (file, mode, access, share) =>
            {
                if (file == sourceLinkPath)
                {
                    return new TestStream(backingStream: new MemoryStream(Encoding.UTF8.GetBytes(@"
{
  ""documents"": {
     ""f:/build/*"" : ""https://raw.githubusercontent.com/my-org/my-project/1111111111111111111111111111111111111111/*""
  }
}
")),
                        dispose: () => { throw new IOException("Fake IOException"); });
                }

                return File.Open(file, mode, access, share);
            });

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            Assert.Equal(1, csc.Run(outWriter));
            Assert.Equal($"error CS0016: Could not write to output file '{sourceLinkPath}' -- 'Fake IOException'{Environment.NewLine}", outWriter.ToString());
        }

        [Fact]
        public void IOFailure_OpenOutputFile()
        {
            string sourcePath = MakeTrivialExe();
            string exePath = Path.Combine(Path.GetDirectoryName(sourcePath), "test.exe");
            var csc = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/nologo", "/preferreduilang:en", $"/out:{exePath}", sourcePath });
            csc.FileSystem = TestableFileSystem.CreateForStandard(openFileFunc: (file, mode, access, share) =>
            {
                if (file == exePath)
                {
                    throw new IOException();
                }

                return File.Open(file, mode, access, share);
            });

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            Assert.Equal(1, csc.Run(outWriter));
            Assert.Contains($"error CS2012: Cannot open '{exePath}' for writing", outWriter.ToString());

            System.IO.File.Delete(sourcePath);
            System.IO.File.Delete(exePath);
            CleanupAllGeneratedFiles(sourcePath);
        }

        [Fact]
        public void IOFailure_OpenPdbFileNotCalled()
        {
            string sourcePath = MakeTrivialExe();
            string exePath = Path.Combine(Path.GetDirectoryName(sourcePath), "test.exe");
            string pdbPath = Path.ChangeExtension(exePath, ".pdb");
            var csc = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/nologo", "/debug-", $"/out:{exePath}", sourcePath });
            csc.FileSystem = TestableFileSystem.CreateForStandard(openFileFunc: (file, mode, access, share) =>
            {
                if (file == pdbPath)
                {
                    throw new IOException();
                }

                return File.Open(file, (FileMode)mode, (FileAccess)access, (FileShare)share);
            });

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            Assert.Equal(0, csc.Run(outWriter));

            System.IO.File.Delete(sourcePath);
            System.IO.File.Delete(exePath);
            System.IO.File.Delete(pdbPath);
            CleanupAllGeneratedFiles(sourcePath);
        }

        [Fact]
        public void IOFailure_OpenXmlFinal()
        {
            string sourcePath = MakeTrivialExe();
            string xmlPath = Path.Combine(WorkingDirectory, "Test.xml");
            var csc = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/nologo", "/preferreduilang:en", "/doc:" + xmlPath, sourcePath });
            csc.FileSystem = TestableFileSystem.CreateForStandard(openFileFunc: (file, mode, access, share) =>
            {
                if (file == xmlPath)
                {
                    throw new IOException();
                }
                else
                {
                    return File.Open(file, (FileMode)mode, (FileAccess)access, (FileShare)share);
                }
            });

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            int exitCode = csc.Run(outWriter);

            var expectedOutput = string.Format("error CS0016: Could not write to output file '{0}' -- 'I/O error occurred.'", xmlPath);
            Assert.Equal(expectedOutput, outWriter.ToString().Trim());

            Assert.NotEqual(0, exitCode);

            System.IO.File.Delete(xmlPath);
            System.IO.File.Delete(sourcePath);
            CleanupAllGeneratedFiles(sourcePath);
        }

        private string MakeTrivialExe(string directory = null)
        {
            return Temp.CreateFile(directory: directory, prefix: "", extension: ".cs").WriteAllText(@"
class Program
{
    public static void Main() { }
} ").Path;
        }

        [Fact, WorkItem(546452, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546452")]
        public void CS1691WRN_BadWarningNumber_AllErrorCodes()
        {
            const int jump = 200;
            for (int i = 0; i < 8000; i += (8000 / jump))
            {
                int startErrorCode = (int)i * jump;
                int endErrorCode = startErrorCode + jump;
                string source = ComputeSourceText(startErrorCode, endErrorCode);

                // Previous versions of the compiler used to report a warning (CS1691)
                // whenever an unrecognized warning code was supplied in a #pragma directive
                // (or via /nowarn /warnaserror flags on the command line).
                // Going forward, we won't generate any warning in such cases. This will make
                // maintenance of backwards compatibility easier (we no longer need to worry
                // about breaking existing projects / command lines if we deprecate / remove
                // an old warning code).
                Test(source, startErrorCode, endErrorCode);
            }
        }

        private static string ComputeSourceText(int startErrorCode, int endErrorCode)
        {
            string pragmaDisableWarnings = String.Empty;

            for (int errorCode = startErrorCode; errorCode < endErrorCode; errorCode++)
            {
                string pragmaDisableStr = @"#pragma warning disable " + errorCode.ToString() + @"
";
                pragmaDisableWarnings += pragmaDisableStr;
            }

            return pragmaDisableWarnings + @"
public class C
{
    public static void Main() { }
}";
        }

        private void Test(string source, int startErrorCode, int endErrorCode)
        {
            string sourcePath = Temp.CreateFile(prefix: "", extension: ".cs").WriteAllText(source).Path;

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            int exitCode = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/nologo", sourcePath }).Run(outWriter);
            Assert.Equal(0, exitCode);
            var cscOutput = outWriter.ToString().Trim();

            for (int errorCode = startErrorCode; errorCode < endErrorCode; errorCode++)
            {
                Assert.True(cscOutput == string.Empty, "Failed at error code: " + errorCode);
            }

            CleanupAllGeneratedFiles(sourcePath);
        }

        [Fact]
        public void WriteXml()
        {
            var source = @"
/// <summary>
/// A subtype of <see cref=""object""/>.
/// </summary>
public class C { }
";

            var sourcePath = Temp.CreateFile(directory: WorkingDirectory, extension: ".cs").WriteAllText(source).Path;
            string xmlPath = Path.Combine(WorkingDirectory, "Test.xml");
            var csc = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/target:library", "/out:Test.dll", "/doc:" + xmlPath, sourcePath });

            var writer = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = csc.Run(writer);
            if (exitCode != 0)
            {
                Console.WriteLine(writer.ToString());
                Assert.Equal(0, exitCode);
            }

            var bytes = File.ReadAllBytes(xmlPath);
            var actual = new string(Encoding.UTF8.GetChars(bytes));
            var expected = @"
<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>Test</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <summary>
            A subtype of <see cref=""T:System.Object""/>.
            </summary>
        </member>
    </members>
</doc>
";
            Assert.Equal(expected.Trim(), actual.Trim());

            System.IO.File.Delete(xmlPath);
            System.IO.File.Delete(sourcePath);

            CleanupAllGeneratedFiles(sourcePath);
            CleanupAllGeneratedFiles(xmlPath);
        }

        [WorkItem(546468, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546468")]
        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/30289")]
        public void CS2002WRN_FileAlreadyIncluded()
        {
            const string cs2002 = @"warning CS2002: Source file '{0}' specified multiple times";

            TempDirectory tempParentDir = Temp.CreateDirectory();
            TempDirectory tempDir = tempParentDir.CreateDirectory("tmpDir");
            TempFile tempFile = tempDir.CreateFile("a.cs").WriteAllText(@"public class A { }");

            // Simple case
            var commandLineArgs = new[] { "a.cs", "a.cs" };
            // warning CS2002: Source file 'a.cs' specified multiple times
            string aWrnString = String.Format(cs2002, "a.cs");
            TestCS2002(commandLineArgs, tempDir.Path, 0, aWrnString);

            // Multiple duplicates
            commandLineArgs = new[] { "a.cs", "a.cs", "a.cs" };
            // warning CS2002: Source file 'a.cs' specified multiple times
            var warnings = new[] { aWrnString };
            TestCS2002(commandLineArgs, tempDir.Path, 0, warnings);

            // Case-insensitive
            commandLineArgs = new[] { "a.cs", "A.cs" };
            // warning CS2002: Source file 'A.cs' specified multiple times
            string AWrnString = String.Format(cs2002, "A.cs");
            TestCS2002(commandLineArgs, tempDir.Path, 0, AWrnString);

            // Different extensions
            tempDir.CreateFile("a.csx");
            commandLineArgs = new[] { "a.cs", "a.csx" };
            // No errors or warnings
            TestCS2002(commandLineArgs, tempDir.Path, 0, String.Empty);

            // Absolute vs Relative
            commandLineArgs = new[] { @"tmpDir\a.cs", tempFile.Path };
            // warning CS2002: Source file 'tmpDir\a.cs' specified multiple times
            string tmpDiraString = String.Format(cs2002, @"tmpDir\a.cs");
            TestCS2002(commandLineArgs, tempParentDir.Path, 0, tmpDiraString);

            // Both relative
            commandLineArgs = new[] { @"tmpDir\..\tmpDir\a.cs", @"tmpDir\a.cs" };
            // warning CS2002: Source file 'tmpDir\a.cs' specified multiple times
            TestCS2002(commandLineArgs, tempParentDir.Path, 0, tmpDiraString);

            // With wild cards
            commandLineArgs = new[] { tempFile.Path, @"tmpDir\*.cs" };
            // warning CS2002: Source file 'tmpDir\a.cs' specified multiple times
            TestCS2002(commandLineArgs, tempParentDir.Path, 0, tmpDiraString);

            // "/recurse" scenarios
            commandLineArgs = new[] { @"/recurse:a.cs", @"tmpDir\a.cs" };
            // warning CS2002: Source file 'tmpDir\a.cs' specified multiple times
            TestCS2002(commandLineArgs, tempParentDir.Path, 0, tmpDiraString);

            commandLineArgs = new[] { @"/recurse:a.cs", @"/recurse:tmpDir\..\tmpDir\*.cs" };
            // warning CS2002: Source file 'tmpDir\a.cs' specified multiple times
            TestCS2002(commandLineArgs, tempParentDir.Path, 0, tmpDiraString);

            // Invalid file/path characters
            const string cs1504 = @"error CS1504: Source file '{0}' could not be opened -- {1}";
            commandLineArgs = new[] { "/preferreduilang:en", tempFile.Path, "tmpDir\a.cs" };
            // error CS1504: Source file '{0}' could not be opened: Illegal characters in path.
            var formattedcs1504Str = String.Format(cs1504, PathUtilities.CombineAbsoluteAndRelativePaths(tempParentDir.Path, "tmpDir\a.cs"), "Illegal characters in path.");
            TestCS2002(commandLineArgs, tempParentDir.Path, 1, formattedcs1504Str);

            commandLineArgs = new[] { tempFile.Path, @"tmpDi\r*a?.cs" };
            var parseDiags = new[] {
                // error CS2021: File name 'tmpDi\r*a?.cs' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments(@"tmpDi\r*a?.cs"),
                // error CS2001: Source file 'tmpDi\r*a?.cs' could not be found.
                Diagnostic(ErrorCode.ERR_FileNotFound).WithArguments(@"tmpDi\r*a?.cs")};
            TestCS2002(commandLineArgs, tempParentDir.Path, 1, (string[])null, parseDiags);

            char currentDrive = Directory.GetCurrentDirectory()[0];
            commandLineArgs = new[] { tempFile.Path, currentDrive + @":a.cs" };
            parseDiags = new[] {
                // error CS2021: File name 'e:a.cs' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InvalidInputFileName).WithArguments(currentDrive + @":a.cs")};
            TestCS2002(commandLineArgs, tempParentDir.Path, 1, (string[])null, parseDiags);

            commandLineArgs = new[] { "/preferreduilang:en", tempFile.Path, @":a.cs" };
            // error CS1504: Source file '{0}' could not be opened: {1}
            var formattedcs1504 = String.Format(cs1504, PathUtilities.CombineAbsoluteAndRelativePaths(tempParentDir.Path, @":a.cs"), @"The given path's format is not supported.");
            TestCS2002(commandLineArgs, tempParentDir.Path, 1, formattedcs1504);

            CleanupAllGeneratedFiles(tempFile.Path);
            System.IO.Directory.Delete(tempParentDir.Path, true);
        }

        private void TestCS2002(string[] commandLineArgs, string baseDirectory, int expectedExitCode, string compileDiagnostic, params DiagnosticDescription[] parseDiagnostics)
        {
            TestCS2002(commandLineArgs, baseDirectory, expectedExitCode, new[] { compileDiagnostic }, parseDiagnostics);
        }

        private void TestCS2002(string[] commandLineArgs, string baseDirectory, int expectedExitCode, string[] compileDiagnostics, params DiagnosticDescription[] parseDiagnostics)
        {
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var allCommandLineArgs = new[] { "/nologo", "/preferreduilang:en", "/t:library" }.Concat(commandLineArgs).ToArray();

            // Verify command line parser diagnostics.
            DefaultParse(allCommandLineArgs, baseDirectory).Errors.Verify(parseDiagnostics);

            // Verify compile.
            int exitCode = CreateCSharpCompiler(null, baseDirectory, allCommandLineArgs).Run(outWriter);
            Assert.Equal(expectedExitCode, exitCode);

            if (parseDiagnostics.IsEmpty())
            {
                // Verify compile diagnostics.
                string outString = String.Empty;
                for (int i = 0; i < compileDiagnostics.Length; i++)
                {
                    if (i != 0)
                    {
                        outString += @"
";
                    }

                    outString += compileDiagnostics[i];
                }

                Assert.Equal(outString, outWriter.ToString().Trim());
            }
            else
            {
                Assert.Null(compileDiagnostics);
            }
        }

        [Fact]
        public void ErrorLineEnd()
        {
            var tree = SyntaxFactory.ParseSyntaxTree("class C public { }", path: "goo");

            var comp = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/errorendlocation" });
            var loc = new SourceLocation(tree.GetCompilationUnitRoot().FindToken(6));
            var diag = new CSDiagnostic(new DiagnosticInfo(MessageProvider.Instance, (int)ErrorCode.ERR_MetadataNameTooLong, "<name>"), loc);
            var text = comp.DiagnosticFormatter.Format(diag);

            string stringStart = "goo(1,7,1,8)";

            Assert.Equal(stringStart, text.Substring(0, stringStart.Length));
        }

        [Fact]
        public void ReportAnalyzer()
        {
            var parsedArgs1 = DefaultParse(new[] { "a.cs", "/reportanalyzer" }, WorkingDirectory);
            Assert.True(parsedArgs1.ReportAnalyzer);

            var parsedArgs2 = DefaultParse(new[] { "a.cs", "" }, WorkingDirectory);
            Assert.False(parsedArgs2.ReportAnalyzer);
        }

        [Fact]
        public void ReportAnalyzerOutput()
        {
            var srcFile = Temp.CreateFile().WriteAllText(@"class C {}");
            var srcDirectory = Path.GetDirectoryName(srcFile.Path);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(
                responseFile: null,
                srcDirectory,
                new[] { "/reportanalyzer", "/t:library", srcFile.Path },
                analyzers: [new WarningDiagnosticAnalyzer(), new DiagnosticSuppressorForId("Warning01", "Suppressor01")],
                generators: new[] { new DoNothingGenerator().AsSourceGenerator() });
            var exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            var output = outWriter.ToString();
            Assert.Contains(CodeAnalysisResources.AnalyzerExecutionTimeColumnHeader, output, StringComparison.Ordinal);
            Assert.Contains($"{nameof(WarningDiagnosticAnalyzer)} (Warning01)", output, StringComparison.Ordinal);
            Assert.Contains($"{nameof(DiagnosticSuppressorForId)} (Suppressor01)", output, StringComparison.Ordinal);
            Assert.Contains(CodeAnalysisResources.GeneratorNameColumnHeader, output, StringComparison.Ordinal);
            Assert.Contains(typeof(DoNothingGenerator).FullName, output, StringComparison.Ordinal);
            CleanupAllGeneratedFiles(srcFile.Path);
        }

        [Fact]
        [WorkItem(40926, "https://github.com/dotnet/roslyn/issues/40926")]
        public void SkipAnalyzersParse()
        {
            var parsedArgs = DefaultParse(new[] { "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.SkipAnalyzers);

            parsedArgs = DefaultParse(new[] { "/skipanalyzers+", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.SkipAnalyzers);

            parsedArgs = DefaultParse(new[] { "/skipanalyzers", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.SkipAnalyzers);

            parsedArgs = DefaultParse(new[] { "/SKIPANALYZERS+", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.SkipAnalyzers);

            parsedArgs = DefaultParse(new[] { "/skipanalyzers-", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.SkipAnalyzers);

            parsedArgs = DefaultParse(new[] { "/skipanalyzers-", "/skipanalyzers+", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.SkipAnalyzers);

            parsedArgs = DefaultParse(new[] { "/skipanalyzers", "/skipanalyzers-", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.SkipAnalyzers);
        }

        [Fact]
        [WorkItem(40926, "https://github.com/dotnet/roslyn/issues/40926")]
        public void SkipAnalyzersFlagFiltersAnalyzers()
        {
            var srcFile = Temp.CreateFile().WriteAllText(@"class C {}");
            var srcDirectory = Path.GetDirectoryName(srcFile.Path);

            var args = new List<string>() { "/reportanalyzer", "/t:library", "/a:" + typeof(DoNothingGenerator).Assembly.Location, srcFile.Path };
            var csc = CreateCSharpCompiler(
                responseFile: null,
                srcDirectory,
                args.ToArray());

            csc.ResolveAnalyzersFromArguments(
                skipAnalyzers: false,
                out _,
                out var analyzers,
                out var generators);
            Assert.NotEmpty(analyzers);
            Assert.NotEmpty(generators);

            csc.ResolveAnalyzersFromArguments(
                skipAnalyzers: true,
                out _,
                out analyzers,
                out generators);
            Assert.All(analyzers, static x => Assert.IsAssignableFrom<DiagnosticSuppressor>(x));
            Assert.NotEmpty(generators);

            CleanupAllGeneratedFiles(srcFile.Path);
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(40926, "https://github.com/dotnet/roslyn/issues/40926")]
        public void NoAnalyzersReportSemantics(bool skipAnalyzers)
        {
            var srcFile = Temp.CreateFile().WriteAllText(@"class C {}");
            var srcDirectory = Path.GetDirectoryName(srcFile.Path);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var analyzers = skipAnalyzers
                ? Array.Empty<DiagnosticAnalyzer>()
                : new DiagnosticAnalyzer[] { new HiddenDiagnosticAnalyzer(), new WarningDiagnosticAnalyzer() };
            var csc = CreateCSharpCompiler(
                responseFile: null,
                srcDirectory,
                new[] { "/reportanalyzer", "/t:library", srcFile.Path },
                analyzers: analyzers);
            var exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            var output = outWriter.ToString();
            if (skipAnalyzers)
            {
                Assert.DoesNotContain(CodeAnalysisResources.AnalyzerExecutionTimeColumnHeader, output, StringComparison.Ordinal);
                Assert.DoesNotContain(new WarningDiagnosticAnalyzer().ToString(), output, StringComparison.Ordinal);
            }
            else
            {
                Assert.Contains(CodeAnalysisResources.AnalyzerExecutionTimeColumnHeader, output, StringComparison.Ordinal);
                Assert.Contains(new WarningDiagnosticAnalyzer().ToString(), output, StringComparison.Ordinal);
            }

            CleanupAllGeneratedFiles(srcFile.Path);
        }

        [Fact]
        [WorkItem(24835, "https://github.com/dotnet/roslyn/issues/24835")]
        public void TestCompilationSuccessIfOnlySuppressedDiagnostics()
        {
            var srcFile = Temp.CreateFile().WriteAllText(@"
#pragma warning disable Warning01
class C { }
");

            var errorLog = Temp.CreateFile();
            var csc = CreateCSharpCompiler(
                null,
                workingDirectory: Path.GetDirectoryName(srcFile.Path),
                args: new[] { "/errorlog:" + errorLog.Path, "/warnaserror+", "/nologo", "/t:library", srcFile.Path },
                analyzers: new[] { new WarningDiagnosticAnalyzer() });

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = csc.Run(outWriter);

            // Previously, the compiler would return error code 1 without printing any diagnostics
            Assert.Empty(outWriter.ToString());
            Assert.Equal(0, exitCode);

            CleanupAllGeneratedFiles(srcFile.Path);
            CleanupAllGeneratedFiles(errorLog.Path);
        }

        [Fact]
        [WorkItem(1759, "https://github.com/dotnet/roslyn/issues/1759")]
        public void AnalyzerDiagnosticThrowsInGetMessage()
        {
            var srcFile = Temp.CreateFile().WriteAllText(@"class C {}");
            var srcDirectory = Path.GetDirectoryName(srcFile.Path);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/t:library", srcFile.Path },
               analyzers: new[] { new AnalyzerThatThrowsInGetMessage() });

            var exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            var output = outWriter.ToString();

            // Verify that the diagnostic reported by AnalyzerThatThrowsInGetMessage is reported, though it doesn't have the message.
            Assert.Contains(AnalyzerThatThrowsInGetMessage.Rule.Id, output, StringComparison.Ordinal);

            // Verify that the analyzer exception diagnostic for the exception throw in AnalyzerThatThrowsInGetMessage is also reported.
            Assert.Contains(AnalyzerExecutor.AnalyzerExceptionDiagnosticId, output, StringComparison.Ordinal);
            Assert.Contains(nameof(NotImplementedException), output, StringComparison.Ordinal);

            CleanupAllGeneratedFiles(srcFile.Path);
        }

        [Fact]
        [WorkItem(3707, "https://github.com/dotnet/roslyn/issues/3707")]
        public void AnalyzerExceptionDiagnosticCanBeConfigured()
        {
            var srcFile = Temp.CreateFile().WriteAllText(@"class C {}");
            var srcDirectory = Path.GetDirectoryName(srcFile.Path);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/t:library", $"/warnaserror:{AnalyzerExecutor.AnalyzerExceptionDiagnosticId}", srcFile.Path },
               analyzers: new[] { new AnalyzerThatThrowsInGetMessage() });

            var exitCode = csc.Run(outWriter);
            Assert.NotEqual(0, exitCode);
            var output = outWriter.ToString();

            // Verify that the analyzer exception diagnostic for the exception throw in AnalyzerThatThrowsInGetMessage is also reported.
            Assert.Contains(AnalyzerExecutor.AnalyzerExceptionDiagnosticId, output, StringComparison.Ordinal);
            Assert.Contains(nameof(NotImplementedException), output, StringComparison.Ordinal);

            CleanupAllGeneratedFiles(srcFile.Path);
        }

        [Fact]
        [WorkItem(4589, "https://github.com/dotnet/roslyn/issues/4589")]
        public void AnalyzerReportsMisformattedDiagnostic()
        {
            var srcFile = Temp.CreateFile().WriteAllText(@"class C {}");
            var srcDirectory = Path.GetDirectoryName(srcFile.Path);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/t:library", srcFile.Path },
               analyzers: new[] { new AnalyzerReportingMisformattedDiagnostic() });

            var exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            var output = outWriter.ToString();

            // Verify that the diagnostic reported by AnalyzerReportingMisformattedDiagnostic is reported with the message format string, instead of the formatted message.
            Assert.Contains(AnalyzerThatThrowsInGetMessage.Rule.Id, output, StringComparison.Ordinal);
            Assert.Contains(AnalyzerThatThrowsInGetMessage.Rule.MessageFormat.ToString(CultureInfo.InvariantCulture), output, StringComparison.Ordinal);

            CleanupAllGeneratedFiles(srcFile.Path);
        }

        [Fact]
        public void ErrorPathsFromLineDirectives()
        {
            string sampleProgram = @"
#line 10 "".."" //relative path
using System*
";
            var syntaxTree = SyntaxFactory.ParseSyntaxTree(sampleProgram, path: "filename.cs");
            var comp = CreateCSharpCompiler(null, WorkingDirectory, new string[] { });
            var text = comp.DiagnosticFormatter.Format(syntaxTree.GetDiagnostics().First());
            //Pull off the last segment of the current directory.
            var expectedPath = Path.GetDirectoryName(WorkingDirectory);
            //the end of the diagnostic's "file" portion should be signaled with the '(' of the line/col info.
            Assert.Equal('(', text[expectedPath.Length]);

            sampleProgram = @"
#line 10 "".>"" //invalid path character
using System*
";
            syntaxTree = SyntaxFactory.ParseSyntaxTree(sampleProgram, path: "filename.cs");
            text = comp.DiagnosticFormatter.Format(syntaxTree.GetDiagnostics().First());
            Assert.True(text.StartsWith(".>", StringComparison.Ordinal));

            sampleProgram = @"
#line 10 ""http://goo.bar/baz.aspx"" //URI
using System*
";
            syntaxTree = SyntaxFactory.ParseSyntaxTree(sampleProgram, path: "filename.cs");
            text = comp.DiagnosticFormatter.Format(syntaxTree.GetDiagnostics().First());
            Assert.True(text.StartsWith("http://goo.bar/baz.aspx", StringComparison.Ordinal));
        }

        [WorkItem(1119609, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1119609")]
        [Fact]
        public void PreferredUILang()
        {
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            int exitCode = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/preferreduilang" }).Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Contains("CS2006", outWriter.ToString(), StringComparison.Ordinal);

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/preferreduilang:" }).Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Contains("CS2006", outWriter.ToString(), StringComparison.Ordinal);

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/preferreduilang:zz" }).Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Contains("CS2038", outWriter.ToString(), StringComparison.Ordinal);

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/preferreduilang:en-zz" }).Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Contains("CS2038", outWriter.ToString(), StringComparison.Ordinal);

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/preferreduilang:en-US" }).Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.DoesNotContain("CS2038", outWriter.ToString(), StringComparison.Ordinal);

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/preferreduilang:de" }).Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.DoesNotContain("CS2038", outWriter.ToString(), StringComparison.Ordinal);

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/preferreduilang:de-AT" }).Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.DoesNotContain("CS2038", outWriter.ToString(), StringComparison.Ordinal);
        }

        [WorkItem(531263, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531263")]
        [Fact]
        public void EmptyFileName()
        {
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = CreateCSharpCompiler(null, WorkingDirectory, new[] { "" }).Run(outWriter);
            Assert.NotEqual(0, exitCode);

            // error CS2021: File name '' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
            Assert.Contains("CS2021", outWriter.ToString(), StringComparison.Ordinal);
        }

        [WorkItem(747219, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/747219")]
        [Fact]
        public void NoInfoDiagnostics()
        {
            string filePath = Temp.CreateFile().WriteAllText(@"
using System.Diagnostics; // Unused.
").Path;
            var cmd = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/nologo", "/target:library", filePath });
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = cmd.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString().Trim());

            CleanupAllGeneratedFiles(filePath);
        }

        [Fact]
        public void RuntimeMetadataVersion()
        {
            var parsedArgs = DefaultParse(new[] { "a.cs", "/runtimemetadataversion" }, WorkingDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.ERR_SwitchNeedsString, parsedArgs.Errors.First().Code);

            parsedArgs = DefaultParse(new[] { "a.cs", "/runtimemetadataversion:" }, WorkingDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.ERR_SwitchNeedsString, parsedArgs.Errors.First().Code);

            parsedArgs = DefaultParse(new[] { "a.cs", "/runtimemetadataversion:  " }, WorkingDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.ERR_SwitchNeedsString, parsedArgs.Errors.First().Code);

            parsedArgs = DefaultParse(new[] { "a.cs", "/runtimemetadataversion:v4.0.30319" }, WorkingDirectory);
            Assert.Equal(0, parsedArgs.Errors.Length);
            Assert.Equal("v4.0.30319", parsedArgs.EmitOptions.RuntimeMetadataVersion);

            parsedArgs = DefaultParse(new[] { "a.cs", "/runtimemetadataversion:-_+@%#*^" }, WorkingDirectory);
            Assert.Equal(0, parsedArgs.Errors.Length);
            Assert.Equal("-_+@%#*^", parsedArgs.EmitOptions.RuntimeMetadataVersion);

            var comp = CreateEmptyCompilation(string.Empty, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute());
            Assert.Equal("v4.0.30319", ModuleMetadata.CreateFromImage(comp.EmitToArray(new EmitOptions(runtimeMetadataVersion: "v4.0.30319"))).Module.MetadataVersion);

            comp = CreateEmptyCompilation(string.Empty, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute());
            Assert.Equal("_+@%#*^", ModuleMetadata.CreateFromImage(comp.EmitToArray(new EmitOptions(runtimeMetadataVersion: "_+@%#*^"))).Module.MetadataVersion);
        }

        [WorkItem(715339, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/715339")]
        [ConditionalFact(typeof(WindowsOnly))]
        public void WRN_InvalidSearchPathDir()
        {
            var baseDir = Temp.CreateDirectory();
            var sourceFile = baseDir.CreateFile("Source.cs");

            var invalidPath = "::";
            var nonExistentPath = "DoesNotExist";

            // lib switch
            DefaultParse(new[] { "/lib:" + invalidPath, sourceFile.Path }, WorkingDirectory).Errors.Verify(
                // warning CS1668: Invalid search path '::' specified in '/LIB option' -- 'path is too long or invalid'
                Diagnostic(ErrorCode.WRN_InvalidSearchPathDir).WithArguments("::", "/LIB option", "path is too long or invalid"));
            DefaultParse(new[] { "/lib:" + nonExistentPath, sourceFile.Path }, WorkingDirectory).Errors.Verify(
                // warning CS1668: Invalid search path 'DoesNotExist' specified in '/LIB option' -- 'directory does not exist'
                Diagnostic(ErrorCode.WRN_InvalidSearchPathDir).WithArguments("DoesNotExist", "/LIB option", "directory does not exist"));

            // LIB environment variable
            DefaultParse(new[] { sourceFile.Path }, WorkingDirectory, additionalReferenceDirectories: invalidPath).Errors.Verify(
                // warning CS1668: Invalid search path '::' specified in 'LIB environment variable' -- 'path is too long or invalid'
                Diagnostic(ErrorCode.WRN_InvalidSearchPathDir).WithArguments("::", "LIB environment variable", "path is too long or invalid"));
            DefaultParse(new[] { sourceFile.Path }, WorkingDirectory, additionalReferenceDirectories: nonExistentPath).Errors.Verify(
                // warning CS1668: Invalid search path 'DoesNotExist' specified in 'LIB environment variable' -- 'directory does not exist'
                Diagnostic(ErrorCode.WRN_InvalidSearchPathDir).WithArguments("DoesNotExist", "LIB environment variable", "directory does not exist"));

            CleanupAllGeneratedFiles(sourceFile.Path);
        }

        [WorkItem(650083, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/650083")]
        [InlineData("a.cs /t:library /appconfig:.\\aux.config")]
        [InlineData("a.cs /out:com1.dll")]
        [InlineData("a.cs /doc:..\\lpt2.xml")]
        [InlineData("a.cs /pdb:..\\prn.pdb")]
        [Theory]
        public void ReservedDeviceNameAsFileName(string commandLine)
        {
            var parsedArgs = DefaultParse(commandLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries), WorkingDirectory);
            if (ExecutionConditionUtil.OperatingSystemRestrictsFileNames)
            {
                Assert.Equal(1, parsedArgs.Errors.Length);
                Assert.Equal((int)ErrorCode.FTL_InvalidInputFileName, parsedArgs.Errors.First().Code);
            }
            else
            {
                Assert.Equal(0, parsedArgs.Errors.Length);
            }
        }

        [Fact]
        public void ReservedDeviceNameAsFileName2()
        {
            string filePath = Temp.CreateFile().WriteAllText(@"class C {}").Path;
            // make sure reserved device names don't
            var cmd = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/r:com2.dll", "/target:library", "/preferreduilang:en", filePath });
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = cmd.Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Contains("error CS0006: Metadata file 'com2.dll' could not be found", outWriter.ToString(), StringComparison.Ordinal);

            cmd = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/link:..\\lpt8.dll", "/target:library", "/preferreduilang:en", filePath });
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = cmd.Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Contains("error CS0006: Metadata file '..\\lpt8.dll' could not be found", outWriter.ToString(), StringComparison.Ordinal);

            cmd = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/lib:aux", "/preferreduilang:en", filePath });
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = cmd.Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Contains("warning CS1668: Invalid search path 'aux' specified in '/LIB option' -- 'directory does not exist'", outWriter.ToString(), StringComparison.Ordinal);

            CleanupAllGeneratedFiles(filePath);
        }

        [Fact]
        public void ParseFeatures()
        {
            var args = DefaultParse(new[] { "/features:Test", "a.vb" }, WorkingDirectory);
            args.Errors.Verify();
            Assert.Equal("Test", args.ParseOptions.Features.Single().Key);

            args = DefaultParse(new[] { "/features:Test", "a.vb", "/Features:Experiment" }, WorkingDirectory);
            args.Errors.Verify();
            Assert.Equal(2, args.ParseOptions.Features.Count);
            Assert.True(args.ParseOptions.HasFeature("Test"));
            Assert.True(args.ParseOptions.HasFeature("Experiment"));

            args = DefaultParse(new[] { "/features:Test=false,Key=value", "a.vb" }, WorkingDirectory);
            args.Errors.Verify();
            Assert.True(args.ParseOptions.Features.SetEquals(new Dictionary<string, string> { { "Test", "false" }, { "Key", "value" } }));

            args = DefaultParse(new[] { "/features:Test,", "a.vb" }, WorkingDirectory);
            args.Errors.Verify();
            Assert.True(args.ParseOptions.Features.SetEquals(new Dictionary<string, string> { { "Test", "true" } }));
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/30289")]
        public void ParseAdditionalFile()
        {
            var args = DefaultParse(new[] { "/additionalfile:web.config", "a.cs" }, WorkingDirectory);
            args.Errors.Verify();
            Assert.Equal(Path.Combine(WorkingDirectory, "web.config"), args.AdditionalFiles.Single().Path);

            args = DefaultParse(new[] { "/additionalfile:web.config", "a.cs", "/additionalfile:app.manifest" }, WorkingDirectory);
            args.Errors.Verify();
            Assert.Equal(2, args.AdditionalFiles.Length);
            Assert.Equal(Path.Combine(WorkingDirectory, "web.config"), args.AdditionalFiles[0].Path);
            Assert.Equal(Path.Combine(WorkingDirectory, "app.manifest"), args.AdditionalFiles[1].Path);

            args = DefaultParse(new[] { "/additionalfile:web.config", "a.cs", "/additionalfile:web.config" }, WorkingDirectory);
            args.Errors.Verify();
            Assert.Equal(2, args.AdditionalFiles.Length);
            Assert.Equal(Path.Combine(WorkingDirectory, "web.config"), args.AdditionalFiles[0].Path);
            Assert.Equal(Path.Combine(WorkingDirectory, "web.config"), args.AdditionalFiles[1].Path);

            args = DefaultParse(new[] { "/additionalfile:..\\web.config", "a.cs" }, WorkingDirectory);
            args.Errors.Verify();
            Assert.Equal(Path.Combine(WorkingDirectory, "..\\web.config"), args.AdditionalFiles.Single().Path);

            var baseDir = Temp.CreateDirectory();
            baseDir.CreateFile("web1.config");
            baseDir.CreateFile("web2.config");
            baseDir.CreateFile("web3.config");

            args = DefaultParse(new[] { "/additionalfile:web*.config", "a.cs" }, baseDir.Path);
            args.Errors.Verify();
            Assert.Equal(3, args.AdditionalFiles.Length);
            Assert.Equal(Path.Combine(baseDir.Path, "web1.config"), args.AdditionalFiles[0].Path);
            Assert.Equal(Path.Combine(baseDir.Path, "web2.config"), args.AdditionalFiles[1].Path);
            Assert.Equal(Path.Combine(baseDir.Path, "web3.config"), args.AdditionalFiles[2].Path);

            args = DefaultParse(new[] { "/additionalfile:web.config;app.manifest", "a.cs" }, WorkingDirectory);
            args.Errors.Verify();
            Assert.Equal(2, args.AdditionalFiles.Length);
            Assert.Equal(Path.Combine(WorkingDirectory, "web.config"), args.AdditionalFiles[0].Path);
            Assert.Equal(Path.Combine(WorkingDirectory, "app.manifest"), args.AdditionalFiles[1].Path);

            args = DefaultParse(new[] { "/additionalfile:web.config,app.manifest", "a.cs" }, WorkingDirectory);
            args.Errors.Verify();
            Assert.Equal(2, args.AdditionalFiles.Length);
            Assert.Equal(Path.Combine(WorkingDirectory, "web.config"), args.AdditionalFiles[0].Path);
            Assert.Equal(Path.Combine(WorkingDirectory, "app.manifest"), args.AdditionalFiles[1].Path);

            args = DefaultParse(new[] { "/additionalfile:web.config,app.manifest", "a.cs" }, WorkingDirectory);
            args.Errors.Verify();
            Assert.Equal(2, args.AdditionalFiles.Length);
            Assert.Equal(Path.Combine(WorkingDirectory, "web.config"), args.AdditionalFiles[0].Path);
            Assert.Equal(Path.Combine(WorkingDirectory, "app.manifest"), args.AdditionalFiles[1].Path);

            args = DefaultParse(new[] { @"/additionalfile:""web.config,app.manifest""", "a.cs" }, WorkingDirectory);
            args.Errors.Verify();
            Assert.Equal(1, args.AdditionalFiles.Length);
            Assert.Equal(Path.Combine(WorkingDirectory, "web.config,app.manifest"), args.AdditionalFiles[0].Path);

            args = DefaultParse(new[] { "/additionalfile:web.config:app.manifest", "a.cs" }, WorkingDirectory);
            args.Errors.Verify();
            Assert.Equal(1, args.AdditionalFiles.Length);
            Assert.Equal(Path.Combine(WorkingDirectory, "web.config:app.manifest"), args.AdditionalFiles[0].Path);

            args = DefaultParse(new[] { "/additionalfile", "a.cs" }, WorkingDirectory);
            args.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<file list>", "additionalfile"));
            Assert.Equal(0, args.AdditionalFiles.Length);

            args = DefaultParse(new[] { "/additionalfile:", "a.cs" }, WorkingDirectory);
            args.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<file list>", "additionalfile"));
            Assert.Equal(0, args.AdditionalFiles.Length);
        }

        [Fact]
        public void ParseEditorConfig()
        {
            var args = DefaultParse(new[] { "/analyzerconfig:.editorconfig", "a.cs" }, WorkingDirectory);
            args.Errors.Verify();
            Assert.Equal(Path.Combine(WorkingDirectory, ".editorconfig"), args.AnalyzerConfigPaths.Single());

            args = DefaultParse(new[] { "/analyzerconfig:.editorconfig", "a.cs", "/analyzerconfig:subdir\\.editorconfig" }, WorkingDirectory);
            args.Errors.Verify();
            Assert.Equal(2, args.AnalyzerConfigPaths.Length);
            Assert.Equal(Path.Combine(WorkingDirectory, ".editorconfig"), args.AnalyzerConfigPaths[0]);
            Assert.Equal(Path.Combine(WorkingDirectory, "subdir\\.editorconfig"), args.AnalyzerConfigPaths[1]);

            args = DefaultParse(new[] { "/analyzerconfig:.editorconfig", "a.cs", "/analyzerconfig:.editorconfig" }, WorkingDirectory);
            args.Errors.Verify();
            Assert.Equal(2, args.AnalyzerConfigPaths.Length);
            Assert.Equal(Path.Combine(WorkingDirectory, ".editorconfig"), args.AnalyzerConfigPaths[0]);
            Assert.Equal(Path.Combine(WorkingDirectory, ".editorconfig"), args.AnalyzerConfigPaths[1]);

            args = DefaultParse(new[] { "/analyzerconfig:..\\.editorconfig", "a.cs" }, WorkingDirectory);
            args.Errors.Verify();
            Assert.Equal(Path.Combine(WorkingDirectory, "..\\.editorconfig"), args.AnalyzerConfigPaths.Single());

            args = DefaultParse(new[] { "/analyzerconfig:.editorconfig;subdir\\.editorconfig", "a.cs" }, WorkingDirectory);
            args.Errors.Verify();
            Assert.Equal(2, args.AnalyzerConfigPaths.Length);
            Assert.Equal(Path.Combine(WorkingDirectory, ".editorconfig"), args.AnalyzerConfigPaths[0]);
            Assert.Equal(Path.Combine(WorkingDirectory, "subdir\\.editorconfig"), args.AnalyzerConfigPaths[1]);

            args = DefaultParse(new[] { "/analyzerconfig", "a.cs" }, WorkingDirectory);
            args.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing '<file list>' for 'analyzerconfig' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<file list>", "analyzerconfig").WithLocation(1, 1));
            Assert.Equal(0, args.AnalyzerConfigPaths.Length);

            args = DefaultParse(new[] { "/analyzerconfig:", "a.cs" }, WorkingDirectory);
            args.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing '<file list>' for 'analyzerconfig' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<file list>", "analyzerconfig").WithLocation(1, 1));
            Assert.Equal(0, args.AnalyzerConfigPaths.Length);
        }

        [Fact]
        public void NullablePublicOnly()
        {
            string source =
@"namespace System.Runtime.CompilerServices
{
    public sealed class NullableAttribute : Attribute { } // missing constructor
}
public class Program
{
    private object? F = null;
}";
            string errorMessage = "error CS0656: Missing compiler required member 'System.Runtime.CompilerServices.NullableAttribute..ctor'";

            string filePath = Temp.CreateFile().WriteAllText(source).Path;
            int exitCode;
            string output;

            // No /feature
            (exitCode, output) = compileAndRun(featureOpt: null);
            Assert.Equal(1, exitCode);
            Assert.Contains(errorMessage, output, StringComparison.Ordinal);

            // /features:nullablePublicOnly
            (exitCode, output) = compileAndRun("/features:nullablePublicOnly");
            Assert.Equal(0, exitCode);
            Assert.DoesNotContain(errorMessage, output, StringComparison.Ordinal);

            // /features:nullablePublicOnly=true
            (exitCode, output) = compileAndRun("/features:nullablePublicOnly=true");
            Assert.Equal(0, exitCode);
            Assert.DoesNotContain(errorMessage, output, StringComparison.Ordinal);

            // /features:nullablePublicOnly=false (the value is ignored)
            (exitCode, output) = compileAndRun("/features:nullablePublicOnly=false");
            Assert.Equal(0, exitCode);
            Assert.DoesNotContain(errorMessage, output, StringComparison.Ordinal);

            CleanupAllGeneratedFiles(filePath);

            (int, string) compileAndRun(string featureOpt)
            {
                var args = new[] { "/target:library", "/preferreduilang:en", "/langversion:8", "/nullable+", filePath };
                if (featureOpt != null) args = args.Concat(featureOpt).ToArray();
                var compiler = CreateCSharpCompiler(null, WorkingDirectory, args);
                var outWriter = new StringWriter(CultureInfo.InvariantCulture);
                int exitCode = compiler.Run(outWriter);
                return (exitCode, outWriter.ToString());
            }
        }

        // See also NullableContextTests.NullableAnalysisFlags_01().
        [Fact]
        public void NullableAnalysisFlags()
        {
            string source =
@"class Program
{
#nullable enable
    static object F1() => null;
#nullable disable
    static object F2() => null;
}";

            string filePath = Temp.CreateFile().WriteAllText(source).Path;
            string fileName = Path.GetFileName(filePath);

            string[] expectedWarningsAll = new[] { fileName + "(4,27): warning CS8603: Possible null reference return." };
            string[] expectedWarningsNone = Array.Empty<string>();

            AssertEx.Equal(expectedWarningsAll, compileAndRun(featureOpt: null));
            AssertEx.Equal(expectedWarningsAll, compileAndRun("/features:run-nullable-analysis"));
            AssertEx.Equal(expectedWarningsAll, compileAndRun("/features:run-nullable-analysis=always"));
            AssertEx.Equal(expectedWarningsNone, compileAndRun("/features:run-nullable-analysis=never"));
            AssertEx.Equal(expectedWarningsAll, compileAndRun("/features:run-nullable-analysis=ALWAYS")); // unrecognized value (incorrect case) ignored
            AssertEx.Equal(expectedWarningsAll, compileAndRun("/features:run-nullable-analysis=NEVER")); // unrecognized value (incorrect case) ignored
            AssertEx.Equal(expectedWarningsAll, compileAndRun("/features:run-nullable-analysis=true")); // unrecognized value ignored
            AssertEx.Equal(expectedWarningsAll, compileAndRun("/features:run-nullable-analysis=false")); // unrecognized value ignored
            AssertEx.Equal(expectedWarningsAll, compileAndRun("/features:run-nullable-analysis=unknown")); // unrecognized value ignored

            CleanupAllGeneratedFiles(filePath);

            string[] compileAndRun(string featureOpt)
            {
                var args = new[] { "/target:library", "/preferreduilang:en", "/nologo", filePath };
                if (featureOpt != null) args = args.Concat(featureOpt).ToArray();
                var compiler = CreateCSharpCompiler(null, WorkingDirectory, args);
                var outWriter = new StringWriter(CultureInfo.InvariantCulture);
                int exitCode = compiler.Run(outWriter);
                return outWriter.ToString().Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        [Fact]
        public void Compiler_Uses_DriverCache()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"
class C
{
}");
            int sourceCallbackCount = 0;
            var generator = new PipelineCallbackGenerator((ctx) =>
            {
                ctx.RegisterSourceOutput(ctx.ParseOptionsProvider, (spc, po) =>
                {
                    sourceCallbackCount++;
                    spc.AddSource("output.cs", "");
                });
            });

            // with no cache, we'll see the callback execute multiple times
            RunWithNoCache();
            Assert.Equal(1, sourceCallbackCount);

            RunWithNoCache();
            Assert.Equal(2, sourceCallbackCount);

            RunWithNoCache();
            Assert.Equal(3, sourceCallbackCount);

            // now re-run with a cache
            GeneratorDriverCache cache = new GeneratorDriverCache();
            sourceCallbackCount = 0;

            RunWithCache();
            Assert.Equal(1, sourceCallbackCount);

            RunWithCache();
            Assert.Equal(1, sourceCallbackCount);

            RunWithCache();
            Assert.Equal(1, sourceCallbackCount);

            // Clean up temp files
            CleanupAllGeneratedFiles(src.Path);
            Directory.Delete(dir.Path, true);

            void RunWithNoCache() => VerifyOutput(dir, src, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/langversion:preview" }, generators: new[] { generator.AsSourceGenerator() }, analyzers: null);

            void RunWithCache() => VerifyOutput(dir, src, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/langversion:preview", "/features:enable-generator-cache" }, generators: new[] { generator.AsSourceGenerator() }, driverCache: cache, analyzers: null);
        }

        [Fact]
        public void Compiler_Doesnt_Use_Cache_From_Other_Compilation()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"
class C
{
}");
            int sourceCallbackCount = 0;
            var generator = new PipelineCallbackGenerator((ctx) =>
            {
                ctx.RegisterSourceOutput(ctx.ParseOptionsProvider, (spc, po) =>
                {
                    sourceCallbackCount++;
                    spc.AddSource("output.cs", "");
                });
            });

            // now re-run with a cache
            GeneratorDriverCache cache = new GeneratorDriverCache();
            sourceCallbackCount = 0;

            RunWithCache("1.dll");
            Assert.Equal(1, sourceCallbackCount);

            RunWithCache("1.dll");
            Assert.Equal(1, sourceCallbackCount);

            // now emulate a new compilation, and check we were invoked, but only once
            RunWithCache("2.dll");
            Assert.Equal(2, sourceCallbackCount);

            RunWithCache("2.dll");
            Assert.Equal(2, sourceCallbackCount);

            // now re-run our first compilation
            RunWithCache("1.dll");
            Assert.Equal(2, sourceCallbackCount);

            // a new one
            RunWithCache("3.dll");
            Assert.Equal(3, sourceCallbackCount);

            // and another old one
            RunWithCache("2.dll");
            Assert.Equal(3, sourceCallbackCount);

            RunWithCache("1.dll");
            Assert.Equal(3, sourceCallbackCount);

            // Clean up temp files
            CleanupAllGeneratedFiles(src.Path);
            Directory.Delete(dir.Path, true);

            void RunWithCache(string outputPath) => VerifyOutput(dir, src, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/langversion:preview", "/out:" + outputPath, "/features:enable-generator-cache" }, generators: new[] { generator.AsSourceGenerator() }, driverCache: cache, analyzers: null);
        }

        [Fact]
        public void Compiler_Can_Enable_DriverCache()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"
class C
{
}");
            int sourceCallbackCount = 0;
            var generator = new PipelineCallbackGenerator((ctx) =>
            {
                ctx.RegisterSourceOutput(ctx.ParseOptionsProvider, (spc, po) =>
                {
                    sourceCallbackCount++;
                    spc.AddSource("output.cs", "");
                });
            });

            // run with the cache
            GeneratorDriverCache cache = new GeneratorDriverCache();
            sourceCallbackCount = 0;

            RunWithCache();
            Assert.Equal(1, sourceCallbackCount);

            RunWithCache();
            Assert.Equal(1, sourceCallbackCount);

            RunWithCache();
            Assert.Equal(1, sourceCallbackCount);

            // now re-run with the cache disabled
            sourceCallbackCount = 0;

            RunWithCacheDisabled();
            Assert.Equal(1, sourceCallbackCount);

            RunWithCacheDisabled();
            Assert.Equal(2, sourceCallbackCount);

            RunWithCacheDisabled();
            Assert.Equal(3, sourceCallbackCount);

            // now clear the cache as well as disabling, and verify we don't put any entries into it either
            cache = new GeneratorDriverCache();
            sourceCallbackCount = 0;

            RunWithCacheDisabled();
            Assert.Equal(1, sourceCallbackCount);
            Assert.Equal(0, cache.CacheSize);

            RunWithCacheDisabled();
            Assert.Equal(2, sourceCallbackCount);
            Assert.Equal(0, cache.CacheSize);

            RunWithCacheDisabled();
            Assert.Equal(3, sourceCallbackCount);
            Assert.Equal(0, cache.CacheSize);

            // Clean up temp files
            CleanupAllGeneratedFiles(src.Path);
            Directory.Delete(dir.Path, true);

            void RunWithCache() => VerifyOutput(dir, src, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/langversion:preview", "/features:enable-generator-cache" }, generators: new[] { generator.AsSourceGenerator() }, driverCache: cache, analyzers: null);

            void RunWithCacheDisabled() => VerifyOutput(dir, src, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/langversion:preview" }, generators: new[] { generator.AsSourceGenerator() }, driverCache: cache, analyzers: null);
        }

        [Fact]
        public void Adding_Or_Removing_A_Generator_Invalidates_Cache()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"
class C
{
}");
            int sourceCallbackCount = 0;
            int sourceCallbackCount2 = 0;
            var generator = new PipelineCallbackGenerator((ctx) =>
            {
                ctx.RegisterSourceOutput(ctx.ParseOptionsProvider, (spc, po) =>
                {
                    sourceCallbackCount++;
                    spc.AddSource("output.cs", "");
                });
            });

            var generator2 = new PipelineCallbackGenerator2((ctx) =>
            {
                ctx.RegisterSourceOutput(ctx.ParseOptionsProvider, (spc, po) =>
                {
                    sourceCallbackCount2++;
                    spc.AddSource("output.cs", "");
                });
            });

            // run with the cache
            GeneratorDriverCache cache = new GeneratorDriverCache();

            RunWithOneGenerator();
            Assert.Equal(1, sourceCallbackCount);
            Assert.Equal(0, sourceCallbackCount2);

            RunWithOneGenerator();
            Assert.Equal(1, sourceCallbackCount);
            Assert.Equal(0, sourceCallbackCount2);

            RunWithTwoGenerators();
            Assert.Equal(2, sourceCallbackCount);
            Assert.Equal(1, sourceCallbackCount2);

            RunWithTwoGenerators();
            Assert.Equal(2, sourceCallbackCount);
            Assert.Equal(1, sourceCallbackCount2);

            // this seems counterintuitive, but when the only thing to change is the generator, we end up back at the state of the project when 
            // we just ran a single generator. Thus we already have an entry in the cache we can use (the one created by the original call to
            // RunWithOneGenerator above) meaning we can use the previously cached results and not run.
            RunWithOneGenerator();
            Assert.Equal(2, sourceCallbackCount);
            Assert.Equal(1, sourceCallbackCount2);

            void RunWithOneGenerator() => VerifyOutput(dir, src, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/langversion:preview", "/features:enable-generator-cache" }, generators: new[] { generator.AsSourceGenerator() }, driverCache: cache, analyzers: null);

            void RunWithTwoGenerators() => VerifyOutput(dir, src, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/langversion:preview", "/features:enable-generator-cache" }, generators: new[] { generator.AsSourceGenerator(), generator2.AsSourceGenerator() }, driverCache: cache, analyzers: null);
        }

        [Fact(Skip = "Additional file comparison is disabled due to https://github.com/dotnet/roslyn/issues/59209")]
        public void Compiler_Updates_Cached_Driver_AdditionalTexts()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText("class C { }");
            var additionalFile = dir.CreateFile("additionalFile.txt").WriteAllText("some text");

            int sourceCallbackCount = 0;
            int additionalFileCallbackCount = 0;
            var generator = new PipelineCallbackGenerator((ctx) =>
            {
                ctx.RegisterSourceOutput(ctx.ParseOptionsProvider, (spc, po) =>
                {
                    sourceCallbackCount++;
                });

                ctx.RegisterSourceOutput(ctx.AdditionalTextsProvider, (spc, po) =>
                {
                    additionalFileCallbackCount++;
                });

            });

            GeneratorDriverCache cache = new GeneratorDriverCache();

            RunWithCache();
            Assert.Equal(1, sourceCallbackCount);
            Assert.Equal(1, additionalFileCallbackCount);

            RunWithCache();
            Assert.Equal(1, sourceCallbackCount);
            Assert.Equal(1, additionalFileCallbackCount);

            additionalFile.WriteAllText("some new content");

            RunWithCache();
            Assert.Equal(1, sourceCallbackCount);
            Assert.Equal(2, additionalFileCallbackCount); // additional file was updated

            // Clean up temp files
            CleanupAllGeneratedFiles(src.Path);
            Directory.Delete(dir.Path, true);

            void RunWithCache() => VerifyOutput(dir, src, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/langversion:preview", "/features:enable-generator-cache", "/additionalFile:" + additionalFile.Path }, generators: new[] { generator.AsSourceGenerator() }, driverCache: cache, analyzers: null);
        }

        [Fact]
        public void Compiler_DoesNotCache_Driver_ConfigProvider()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText("class C { }");
            var editorconfig = dir.CreateFile(".editorconfig").WriteAllText(@"
[temp.cs]
a = localA
");

            var globalconfig = dir.CreateFile(".globalconfig").WriteAllText(@"
is_global = true
a = globalA");

            int sourceCallbackCount = 0;
            int configOptionsCallbackCount = 0;
            int filteredGlobalCallbackCount = 0;
            int filteredLocalCallbackCount = 0;
            string globalA = "";
            string localA = "";
            var generator = new PipelineCallbackGenerator((ctx) =>
            {
                ctx.RegisterSourceOutput(ctx.ParseOptionsProvider, (spc, po) =>
                {
                    sourceCallbackCount++;
                    spc.AddSource("output.cs", "");
                });

                ctx.RegisterSourceOutput(ctx.AnalyzerConfigOptionsProvider, (spc, po) =>
                {
                    configOptionsCallbackCount++;
                    po.GlobalOptions.TryGetValue("a", out globalA);
                });

                ctx.RegisterSourceOutput(ctx.AnalyzerConfigOptionsProvider.Select((p, _) => { p.GlobalOptions.TryGetValue("a", out var value); return value; }), (spc, value) =>
                {
                    filteredGlobalCallbackCount++;
                    globalA = value;
                });

                var syntaxTreeInput = ctx.CompilationProvider.Select((c, _) => c.SyntaxTrees.First());
                ctx.RegisterSourceOutput(ctx.AnalyzerConfigOptionsProvider.Combine(syntaxTreeInput).Select((p, _) => { p.Left.GetOptions(p.Right).TryGetValue("a", out var value); return value; }), (spc, value) =>
                {
                    filteredLocalCallbackCount++;
                    localA = value;
                });
            });

            GeneratorDriverCache cache = new GeneratorDriverCache();

            RunWithCache();
            Assert.Equal(1, sourceCallbackCount);
            Assert.Equal(1, configOptionsCallbackCount);
            Assert.Equal(1, filteredGlobalCallbackCount);
            Assert.Equal(1, filteredLocalCallbackCount);
            Assert.Equal("globalA", globalA);
            Assert.Equal("localA", localA);

            RunWithCache();
            Assert.Equal(1, sourceCallbackCount);
            Assert.Equal(2, configOptionsCallbackCount); // we can't compare the provider directly, so we consider it modified
            Assert.Equal(1, filteredGlobalCallbackCount); // however, the values in it will cache out correctly.
            Assert.Equal(1, filteredLocalCallbackCount);

            editorconfig.WriteAllText(@"
[temp.cs]
a = diffLocalA
");

            RunWithCache();
            Assert.Equal(1, sourceCallbackCount);
            Assert.Equal(3, configOptionsCallbackCount);
            Assert.Equal(1, filteredGlobalCallbackCount); // the provider changed, but only the local value changed
            Assert.Equal(2, filteredLocalCallbackCount);
            Assert.Equal("globalA", globalA);
            Assert.Equal("diffLocalA", localA);

            globalconfig.WriteAllText(@"
is_global = true
a = diffGlobalA
");

            RunWithCache();
            Assert.Equal(1, sourceCallbackCount);
            Assert.Equal(4, configOptionsCallbackCount);
            Assert.Equal(2, filteredGlobalCallbackCount); // only the global value was changed
            Assert.Equal(2, filteredLocalCallbackCount);
            Assert.Equal("diffGlobalA", globalA);
            Assert.Equal("diffLocalA", localA);

            // Clean up temp files
            CleanupAllGeneratedFiles(src.Path);
            Directory.Delete(dir.Path, true);

            void RunWithCache() => VerifyOutput(dir, src, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/langversion:preview", "/features:enable-generator-cache", "/analyzerConfig:" + editorconfig.Path, "/analyzerConfig:" + globalconfig.Path }, generators: new[] { generator.AsSourceGenerator() }, driverCache: cache, analyzers: null);
        }

        [Fact]
        public void Compiler_DoesNotCache_Compilation()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"
class C
{
}");
            int sourceCallbackCount = 0;
            var generator = new PipelineCallbackGenerator((ctx) =>
            {
                ctx.RegisterSourceOutput(ctx.CompilationProvider, (spc, po) =>
                {
                    sourceCallbackCount++;
                    spc.AddSource("output.cs", "");
                });
            });

            // now re-run with a cache
            GeneratorDriverCache cache = new GeneratorDriverCache();

            RunWithCache();
            Assert.Equal(1, sourceCallbackCount);

            RunWithCache();
            Assert.Equal(2, sourceCallbackCount);

            RunWithCache();
            Assert.Equal(3, sourceCallbackCount);

            // Clean up temp files
            CleanupAllGeneratedFiles(src.Path);
            Directory.Delete(dir.Path, true);

            void RunWithCache() => VerifyOutput(dir, src, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/langversion:preview", "/features:enable-generator-cache" }, generators: new[] { generator.AsSourceGenerator() }, driverCache: cache, analyzers: null);
        }

        [Fact]
        public void Compiler_DoesNot_RunHostOutputs()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"
class C
{
}");
            bool hostOutputRan = false;
            bool sourceOutputRan = false;

            var generator = new PipelineCallbackGenerator((ctx) =>
            {
#pragma warning disable RSEXPERIMENTAL004 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                ctx.RegisterHostOutput(ctx.CompilationProvider, (hostCtx, value) =>
                {
                    hostOutputRan = true;
                    hostCtx.AddOutput("output", "value");
                });
#pragma warning restore RSEXPERIMENTAL004 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

                ctx.RegisterSourceOutput(ctx.CompilationProvider, (spc, po) =>
                {
                    sourceOutputRan = true;
                    spc.AddSource("output.cs", "//value");
                });
            });

            VerifyOutput(dir, src, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/langversion:preview" }, generators: new[] { generator.AsSourceGenerator() }, analyzers: null);

            Assert.False(hostOutputRan);
            Assert.True(sourceOutputRan);

            // Clean up temp files
            CleanupAllGeneratedFiles(src.Path);
            Directory.Delete(dir.Path, true);
        }

        private static int OccurrenceCount(string source, string word)
        {
            var n = 0;
            var index = source.IndexOf(word, StringComparison.Ordinal);
            while (index >= 0)
            {
                ++n;
                index = source.IndexOf(word, index + word.Length, StringComparison.Ordinal);
            }
            return n;
        }

        private string VerifyOutput(TempDirectory sourceDir, TempFile sourceFile,
                                           bool includeCurrentAssemblyAsAnalyzerReference = true,
                                           string[] additionalFlags = null,
                                           int expectedInfoCount = 0,
                                           int expectedWarningCount = 0,
                                           int expectedErrorCount = 0,
                                           int? expectedExitCode = null,
                                           bool errorlog = false,
                                           bool skipAnalyzers = false,
                                           DiagnosticAnalyzer[] analyzers = null,
                                           ISourceGenerator[] generators = null,
                                           GeneratorDriverCache driverCache = null)
        {
            var args = new[] {
                                "/nologo", "/preferreduilang:en", "/t:library",
                                sourceFile.Path
                             };
            if (includeCurrentAssemblyAsAnalyzerReference)
            {
                args = args.Append("/a:" + Assembly.GetExecutingAssembly().Location);
            }

            if (errorlog)
            {
                args = args.Append("/errorlog:errorlog");
            }

            if (skipAnalyzers)
            {
                args = args.Append("/skipAnalyzers");
            }

            if (additionalFlags != null)
            {
                args = args.Append(additionalFlags);
            }

            var csc = CreateCSharpCompiler(null, sourceDir.Path, args, analyzers: analyzers, generators: generators, driverCache: driverCache);
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = csc.Run(outWriter);
            var output = outWriter.ToString();

            expectedExitCode ??= expectedErrorCount > 0 ? 1 : 0;
            Assert.True(
                expectedExitCode == exitCode,
                string.Format("Expected exit code to be '{0}' was '{1}'.{2} Output:{3}{4}",
                expectedExitCode, exitCode, Environment.NewLine, Environment.NewLine, output));

            Assert.DoesNotContain("hidden", output, StringComparison.Ordinal);

            if (expectedInfoCount == 0)
            {
                Assert.DoesNotContain("info", output, StringComparison.Ordinal);
            }
            else
            {
                // Info diagnostics are only logged with /errorlog.
                Assert.True(errorlog);
                Assert.Equal(expectedInfoCount, OccurrenceCount(output, "info"));
            }

            if (expectedWarningCount == 0)
            {
                Assert.DoesNotContain("warning", output, StringComparison.Ordinal);
            }
            else
            {
                Assert.Equal(expectedWarningCount, OccurrenceCount(output, "warning"));
            }

            if (expectedErrorCount == 0)
            {
                Assert.DoesNotContain("error", output, StringComparison.Ordinal);
            }
            else
            {
                Assert.Equal(expectedErrorCount, OccurrenceCount(output, "error"));
            }

            return output;
        }

        [WorkItem(899050, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/899050")]
        [Fact]
        public void NoWarnAndWarnAsError_AnalyzerDriverWarnings()
        {
            // This assembly has an abstract MockAbstractDiagnosticAnalyzer type which should cause
            // compiler warning CS8032 to be produced when compilations created in this test try to load it.
            string source = @"using System;";
            var dir = Temp.CreateDirectory();
            var file = dir.CreateFile("a.cs");
            file.WriteAllText(source);

            var output = VerifyOutput(dir, file, expectedWarningCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that compiler warning CS8032 can be suppressed via /warn:0.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warn:0" });
            Assert.True(string.IsNullOrEmpty(output));

            // TEST: Verify that compiler warning CS8032 can be individually suppressed via /nowarn:.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/nowarn:CS8032" });
            Assert.True(string.IsNullOrEmpty(output));

            // TEST: Verify that compiler warning CS8032 can be promoted to an error via /warnaserror.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror" }, expectedErrorCount: 1);
            Assert.Contains("error CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that compiler warning CS8032 can be individually promoted to an error via /warnaserror:.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror:8032" }, expectedErrorCount: 1);
            Assert.Contains("error CS8032", output, StringComparison.Ordinal);

            CleanupAllGeneratedFiles(file.Path);
        }

        [WorkItem(899050, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/899050")]
        [WorkItem(981677, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/981677")]
        [WorkItem(1021115, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1021115")]
        [Fact]
        public void NoWarnAndWarnAsError_HiddenDiagnostic()
        {
            // This assembly has a HiddenDiagnosticAnalyzer type which should produce custom hidden
            // diagnostics for #region directives present in the compilations created in this test.
            var source = @"using System;
#region Region
#endregion";
            var dir = Temp.CreateDirectory();
            var file = dir.CreateFile("a.cs");
            file.WriteAllText(source);

            var output = VerifyOutput(dir, file, expectedWarningCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that /warn:0 has no impact on custom hidden diagnostic Hidden01.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warn:0" });
            Assert.True(string.IsNullOrEmpty(output));

            // TEST: Verify that /nowarn: has no impact on custom hidden diagnostic Hidden01.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/nowarn:Hidden01" }, expectedWarningCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that /warnaserror+ has no impact on custom hidden diagnostic Hidden01.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror+", "/nowarn:8032" });
            Assert.True(string.IsNullOrEmpty(output));

            // TEST: Verify that /warnaserror- has no impact on custom hidden diagnostic Hidden01.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror-" }, expectedWarningCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that /warnaserror: promotes custom hidden diagnostic Hidden01 to an error.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror:Hidden01" }, expectedWarningCount: 1, expectedErrorCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(2,1): error Hidden01: Throwing a diagnostic for #region", output, StringComparison.Ordinal);

            // TEST: Verify that /warnaserror-: has no impact on custom hidden diagnostic Hidden01.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror-:Hidden01" }, expectedWarningCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify /nowarn: overrides /warnaserror:.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror:Hidden01", "/nowarn:Hidden01" }, expectedWarningCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify /nowarn: overrides /warnaserror:.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/nowarn:Hidden01", "/warnaserror:Hidden01" }, expectedWarningCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify /nowarn: overrides /warnaserror-:.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror-:Hidden01", "/nowarn:Hidden01" }, expectedWarningCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify /nowarn: overrides /warnaserror-:.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/nowarn:Hidden01", "/warnaserror-:Hidden01" }, expectedWarningCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that /warn:0 has no impact on custom hidden diagnostic Hidden01.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warn:0", "/warnaserror:Hidden01" });
            Assert.True(string.IsNullOrEmpty(output));

            // TEST: Verify that /warn:0 has no impact on custom hidden diagnostic Hidden01.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror:Hidden01", "/warn:0" });
            Assert.True(string.IsNullOrEmpty(output));

            // TEST: Verify that last /warnaserror[+/-]: flag on command line wins.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror+:Hidden01", "/warnaserror-:Hidden01" }, expectedWarningCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that last /warnaserror[+/-]: flag on command line wins.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror-:Hidden01", "/warnaserror+:Hidden01" }, expectedWarningCount: 1, expectedErrorCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(2,1): error Hidden01: Throwing a diagnostic for #region", output, StringComparison.Ordinal);

            // TEST: Verify that last one wins between /warnaserror[+/-] and /warnaserror[+/-]:.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror-", "/warnaserror+:Hidden01" }, expectedWarningCount: 1, expectedErrorCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(2,1): error Hidden01: Throwing a diagnostic for #region", output, StringComparison.Ordinal);

            // TEST: Verify that last one wins between /warnaserror[+/-]: and /warnaserror[+/-].
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror-:Hidden01", "/warnaserror+" }, expectedErrorCount: 1);
            Assert.Contains("error CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that last one wins between /warnaserror[+/-] and /warnaserror[+/-]:.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror+", "/warnaserror+:Hidden01", "/nowarn:8032" }, expectedErrorCount: 1);
            Assert.Contains("a.cs(2,1): error Hidden01: Throwing a diagnostic for #region", output, StringComparison.Ordinal);

            // TEST: Verify that last one wins between /warnaserror[+/-]: and /warnaserror[+/-].
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror+:Hidden01", "/warnaserror+", "/nowarn:8032" });
            Assert.True(string.IsNullOrEmpty(output));

            // TEST: Verify that last one wins between /warnaserror[+/-]: and /warnaserror[+/-].
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror+:Hidden01", "/warnaserror-" }, expectedWarningCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that last one wins between /warnaserror[+/-] and /warnaserror[+/-]:.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror+", "/warnaserror-:Hidden01", "/nowarn:8032" });
            Assert.True(string.IsNullOrEmpty(output));

            // TEST: Verify that last one wins between /warnaserror[+/-]: and /warnaserror[+/-].
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror-:Hidden01", "/warnaserror-" }, expectedWarningCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that last one wins between /warnaserror[+/-] and /warnaserror[+/-]:.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror-", "/warnaserror-:Hidden01" }, expectedWarningCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            CleanupAllGeneratedFiles(file.Path);
        }

        [WorkItem(899050, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/899050")]
        [WorkItem(981677, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/981677")]
        [WorkItem(1021115, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1021115")]
        [WorkItem(42166, "https://github.com/dotnet/roslyn/issues/42166")]
        [CombinatorialData, Theory]
        public void NoWarnAndWarnAsError_InfoDiagnostic(bool errorlog)
        {
            // NOTE: Info diagnostics are only logged on command line when /errorlog is specified. See https://github.com/dotnet/roslyn/issues/42166 for details.

            // This assembly has an InfoDiagnosticAnalyzer type which should produce custom info
            // diagnostics for the #pragma warning restore directives present in the compilations created in this test.
            var source = @"using System;
#pragma warning restore";
            var name = "a.cs";
            string output;
            output = GetOutput(name, source, expectedWarningCount: 1, expectedInfoCount: errorlog ? 1 : 0, errorlog: errorlog);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);
            if (errorlog)
                Assert.Contains("a.cs(2,1): info Info01: Throwing a diagnostic for #pragma restore", output, StringComparison.Ordinal);

            // TEST: Verify that /warn:0 suppresses custom info diagnostic Info01.
            output = GetOutput(name, source, additionalFlags: new[] { "/warn:0" }, errorlog: errorlog);

            // TEST: Verify that custom info diagnostic Info01 can be individually suppressed via /nowarn:.
            output = GetOutput(name, source, additionalFlags: new[] { "/nowarn:Info01" }, expectedWarningCount: 1, errorlog: errorlog);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that custom info diagnostic Info01 can never be promoted to an error via /warnaserror+.
            output = GetOutput(name, source, additionalFlags: new[] { "/warnaserror+", "/nowarn:8032" }, expectedInfoCount: errorlog ? 1 : 0, errorlog: errorlog);
            if (errorlog)
                Assert.Contains("a.cs(2,1): info Info01: Throwing a diagnostic for #pragma restore", output, StringComparison.Ordinal);

            // TEST: Verify that custom info diagnostic Info01 is still reported as an info when /warnaserror- is used.
            output = GetOutput(name, source, additionalFlags: new[] { "/warnaserror-" }, expectedWarningCount: 1, expectedInfoCount: errorlog ? 1 : 0, errorlog: errorlog);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);
            if (errorlog)
                Assert.Contains("a.cs(2,1): info Info01: Throwing a diagnostic for #pragma restore", output, StringComparison.Ordinal);

            // TEST: Verify that custom info diagnostic Info01 can be individually promoted to an error via /warnaserror:.
            output = GetOutput(name, source, additionalFlags: new[] { "/warnaserror:Info01" }, expectedWarningCount: 1, expectedErrorCount: 1, errorlog: errorlog);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(2,1): error Info01: Throwing a diagnostic for #pragma restore", output, StringComparison.Ordinal);

            // TEST: Verify that custom info diagnostic Info01 is still reported as an info when passed to /warnaserror-:.
            output = GetOutput(name, source, additionalFlags: new[] { "/warnaserror-:Info01" }, expectedWarningCount: 1, expectedInfoCount: errorlog ? 1 : 0, errorlog: errorlog);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);
            if (errorlog)
                Assert.Contains("a.cs(2,1): info Info01: Throwing a diagnostic for #pragma restore", output, StringComparison.Ordinal);

            // TEST: Verify /nowarn overrides /warnaserror.
            output = GetOutput(name, source, additionalFlags: new[] { "/warnaserror:Info01", "/nowarn:Info01" }, expectedWarningCount: 1, errorlog: errorlog);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify /nowarn overrides /warnaserror.
            output = GetOutput(name, source, additionalFlags: new[] { "/nowarn:Info01", "/warnaserror:Info01" }, expectedWarningCount: 1, errorlog: errorlog);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify /nowarn overrides /warnaserror-.
            output = GetOutput(name, source, additionalFlags: new[] { "/warnaserror-:Info01", "/nowarn:Info01" }, expectedWarningCount: 1, errorlog: errorlog);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify /nowarn overrides /warnaserror-.
            output = GetOutput(name, source, additionalFlags: new[] { "/nowarn:Info01", "/warnaserror-:Info01" }, expectedWarningCount: 1, errorlog: errorlog);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that /warn:0 has no impact on custom info diagnostic Info01.
            output = GetOutput(name, source, additionalFlags: new[] { "/warn:0", "/warnaserror:Info01" }, errorlog: errorlog);

            // TEST: Verify that /warn:0 has no impact on custom info diagnostic Info01.
            output = GetOutput(name, source, additionalFlags: new[] { "/warnaserror:Info01", "/warn:0" });

            // TEST: Verify that last /warnaserror[+/-]: flag on command line wins.
            output = GetOutput(name, source, additionalFlags: new[] { "/warnaserror+:Info01", "/warnaserror-:Info01" }, expectedWarningCount: 1, expectedInfoCount: errorlog ? 1 : 0, errorlog: errorlog);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);
            if (errorlog)
                Assert.Contains("a.cs(2,1): info Info01: Throwing a diagnostic for #pragma restore", output, StringComparison.Ordinal);

            // TEST: Verify that last /warnaserror[+/-]: flag on command line wins.
            output = GetOutput(name, source, additionalFlags: new[] { "/warnaserror-:Info01", "/warnaserror+:Info01" }, expectedWarningCount: 1, expectedErrorCount: 1, errorlog: errorlog);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(2,1): error Info01: Throwing a diagnostic for #pragma restore", output, StringComparison.Ordinal);

            // TEST: Verify that last one wins between /warnaserror[+/-] and /warnaserror[+/-]:.
            output = GetOutput(name, source, additionalFlags: new[] { "/warnaserror-", "/warnaserror+:Info01" }, expectedWarningCount: 1, expectedErrorCount: 1, errorlog: errorlog);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(2,1): error Info01: Throwing a diagnostic for #pragma restore", output, StringComparison.Ordinal);

            // TEST: Verify that last one wins between /warnaserror[+/-]: and /warnaserror[+/-].
            output = GetOutput(name, source, additionalFlags: new[] { "/warnaserror-:Info01", "/warnaserror+", "/nowarn:8032" }, expectedInfoCount: errorlog ? 1 : 0, errorlog: errorlog);
            if (errorlog)
                Assert.Contains("a.cs(2,1): info Info01: Throwing a diagnostic for #pragma restore", output, StringComparison.Ordinal);

            // TEST: Verify that last one wins between /warnaserror[+/-]: and /warnaserror[+/-].
            output = GetOutput(name, source, additionalFlags: new[] { "/warnaserror+:Info01", "/warnaserror+", "/nowarn:8032" }, expectedInfoCount: errorlog ? 1 : 0, errorlog: errorlog);
            if (errorlog)
                Assert.Contains("a.cs(2,1): info Info01: Throwing a diagnostic for #pragma restore", output, StringComparison.Ordinal);

            // TEST: Verify that last one wins between /warnaserror[+/-] and /warnaserror[+/-]:.
            output = GetOutput(name, source, additionalFlags: new[] { "/warnaserror+", "/warnaserror+:Info01", "/nowarn:8032" }, expectedErrorCount: 1, errorlog: errorlog);
            Assert.Contains("a.cs(2,1): error Info01: Throwing a diagnostic for #pragma restore", output, StringComparison.Ordinal);

            // TEST: Verify that last one wins between /warnaserror[+/-]: and /warnaserror[+/-].
            output = GetOutput(name, source, additionalFlags: new[] { "/warnaserror+:Info01", "/warnaserror-" }, expectedWarningCount: 1, expectedInfoCount: errorlog ? 1 : 0, errorlog: errorlog);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);
            if (errorlog)
                Assert.Contains("a.cs(2,1): info Info01: Throwing a diagnostic for #pragma restore", output, StringComparison.Ordinal);

            // TEST: Verify that last one wins between /warnaserror[+/-] and /warnaserror[+/-]:.
            output = GetOutput(name, source, additionalFlags: new[] { "/warnaserror+", "/warnaserror-:Info01", "/nowarn:8032" }, expectedInfoCount: errorlog ? 1 : 0, errorlog: errorlog);
            if (errorlog)
                Assert.Contains("a.cs(2,1): info Info01: Throwing a diagnostic for #pragma restore", output, StringComparison.Ordinal);

            // TEST: Verify that last one wins between /warnaserror[+/-]: and /warnaserror[+/-].
            output = GetOutput(name, source, additionalFlags: new[] { "/warnaserror-:Info01", "/warnaserror-" }, expectedWarningCount: 1, expectedInfoCount: errorlog ? 1 : 0, errorlog: errorlog);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);
            if (errorlog)
                Assert.Contains("a.cs(2,1): info Info01: Throwing a diagnostic for #pragma restore", output, StringComparison.Ordinal);

            // TEST: Verify that last one wins between /warnaserror[+/-] and /warnaserror[+/-]:.
            output = GetOutput(name, source, additionalFlags: new[] { "/warnaserror-", "/warnaserror-:Info01" }, expectedWarningCount: 1, expectedInfoCount: errorlog ? 1 : 0, errorlog: errorlog);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);
            if (errorlog)
                Assert.Contains("a.cs(2,1): info Info01: Throwing a diagnostic for #pragma restore", output, StringComparison.Ordinal);
        }

        private string GetOutput(
            string name,
            string source,
            bool includeCurrentAssemblyAsAnalyzerReference = true,
            string[] additionalFlags = null,
            int expectedInfoCount = 0,
            int expectedWarningCount = 0,
            int expectedErrorCount = 0,
            bool errorlog = false)
        {
            var dir = Temp.CreateDirectory();
            var file = dir.CreateFile(name);
            file.WriteAllText(source);

            var output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReference, additionalFlags, expectedInfoCount, expectedWarningCount, expectedErrorCount, null, errorlog);
            CleanupAllGeneratedFiles(file.Path);
            return output;
        }

        [WorkItem(11368, "https://github.com/dotnet/roslyn/issues/11368")]
        [WorkItem(899050, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/899050")]
        [WorkItem(981677, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/981677")]
        [WorkItem(998069, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/998069")]
        [WorkItem(998724, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/998724")]
        [WorkItem(1021115, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1021115")]
        [Fact]
        public void NoWarnAndWarnAsError_WarningDiagnostic()
        {
            // This assembly has a WarningDiagnosticAnalyzer type which should produce custom warning
            // diagnostics for source types present in the compilations created in this test.
            string source = @"
class C
{
    static void Main()
    {
        int i;
    }
}
";
            var dir = Temp.CreateDirectory();
            var file = dir.CreateFile("a.cs");
            file.WriteAllText(source);

            var output = VerifyOutput(dir, file, expectedWarningCount: 3);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(2,7): warning Warning01: Throwing a diagnostic for types declared", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(6,13): warning CS0168: The variable 'i' is declared but never used", output, StringComparison.Ordinal);

            // TEST: Verify that compiler warning CS0168 as well as custom warning diagnostic Warning01 can be suppressed via /warn:0.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warn:0" });
            Assert.True(string.IsNullOrEmpty(output));

            // TEST: Verify that compiler warning CS0168 as well as custom warning diagnostic Warning01 can be individually suppressed via /nowarn:.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/nowarn:0168,Warning01,58000" }, expectedWarningCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that diagnostic ids are processed in case-sensitive fashion inside /nowarn:.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/nowarn:cs0168,warning01,700000" }, expectedWarningCount: 3);
            Assert.Contains("a.cs(2,7): warning Warning01: Throwing a diagnostic for types declared", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(6,13): warning CS0168: The variable 'i' is declared but never used", output, StringComparison.Ordinal);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that compiler warning CS0168 as well as custom warning diagnostic Warning01 can be promoted to errors via /warnaserror.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror", "/nowarn:8032" }, expectedErrorCount: 2);
            Assert.Contains("a.cs(2,7): error Warning01: Throwing a diagnostic for types declared", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(6,13): error CS0168: The variable 'i' is declared but never used", output, StringComparison.Ordinal);

            // TEST: Verify that compiler warning CS0168 as well as custom warning diagnostic Warning01 can be promoted to errors via /warnaserror+.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror+", "/nowarn:8032" }, expectedErrorCount: 2);
            Assert.Contains("a.cs(2,7): error Warning01: Throwing a diagnostic for types declared", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(6,13): error CS0168: The variable 'i' is declared but never used", output, StringComparison.Ordinal);

            // TEST: Verify that /warnaserror- keeps compiler warning CS0168 as well as custom warning diagnostic Warning01 as warnings.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror-" }, expectedWarningCount: 3);
            Assert.Contains("a.cs(2,7): warning Warning01: Throwing a diagnostic for types declared", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(6,13): warning CS0168: The variable 'i' is declared but never used", output, StringComparison.Ordinal);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that custom warning diagnostic Warning01 can be individually promoted to an error via /warnaserror:.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror:Something,Warning01" }, expectedWarningCount: 2, expectedErrorCount: 1);
            Assert.Contains("a.cs(2,7): error Warning01: Throwing a diagnostic for types declared", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(6,13): warning CS0168: The variable 'i' is declared but never used", output, StringComparison.Ordinal);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that compiler warning CS0168 can be individually promoted to an error via /warnaserror+:.
            // This doesn't work correctly currently - promoting compiler warning CS0168 to an error causes us to no longer report any custom warning diagnostics as errors (Bug 998069).
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror+:CS0168" }, expectedWarningCount: 2, expectedErrorCount: 1);
            Assert.Contains("a.cs(2,7): warning Warning01: Throwing a diagnostic for types declared", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(6,13): error CS0168: The variable 'i' is declared but never used", output, StringComparison.Ordinal);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that diagnostic ids are processed in case-sensitive fashion inside /warnaserror.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror:cs0168,warning01,58000" }, expectedWarningCount: 3);
            Assert.Contains("a.cs(2,7): warning Warning01: Throwing a diagnostic for types declared", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(6,13): warning CS0168: The variable 'i' is declared but never used", output, StringComparison.Ordinal);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that custom warning diagnostic Warning01 as well as compiler warning CS0168 can be promoted to errors via /warnaserror:.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror:CS0168,Warning01" }, expectedWarningCount: 1, expectedErrorCount: 2);
            Assert.Contains("a.cs(2,7): error Warning01: Throwing a diagnostic for types declared", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(6,13): error CS0168: The variable 'i' is declared but never used", output, StringComparison.Ordinal);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that /warn:0 overrides /warnaserror+.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warn:0", "/warnaserror+" });

            // TEST: Verify that /warn:0 overrides /warnaserror.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror", "/warn:0" });

            // TEST: Verify that /warn:0 overrides /warnaserror-.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror-", "/warn:0" });

            // TEST: Verify that /warn:0 overrides /warnaserror-.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warn:0", "/warnaserror-" });

            // TEST: Verify that /nowarn: overrides /warnaserror:.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror:Something,CS0168,Warning01", "/nowarn:0168,Warning01,58000" }, expectedWarningCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that /nowarn: overrides /warnaserror:.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/nowarn:0168,Warning01,58000", "/warnaserror:Something,CS0168,Warning01" }, expectedWarningCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that /nowarn: overrides /warnaserror-:.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror-:Something,CS0168,Warning01", "/nowarn:0168,Warning01,58000" }, expectedWarningCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that /nowarn: overrides /warnaserror-:.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/nowarn:0168,Warning01,58000", "/warnaserror-:Something,CS0168,Warning01" }, expectedWarningCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that /nowarn: overrides /warnaserror+.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror+", "/nowarn:0168,Warning01,58000,8032" });

            // TEST: Verify that /nowarn: overrides /warnaserror+.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/nowarn:0168,Warning01,58000,8032", "/warnaserror+" });

            // TEST: Verify that /nowarn: overrides /warnaserror-.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror-", "/nowarn:0168,Warning01,58000,8032" });

            // TEST: Verify that /nowarn: overrides /warnaserror-.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/nowarn:0168,Warning01,58000,8032", "/warnaserror-" });

            // TEST: Verify that /warn:0 overrides /warnaserror:.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror:Something,CS0168,Warning01", "/warn:0" });

            // TEST: Verify that /warn:0 overrides /warnaserror:.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warn:0", "/warnaserror:Something,CS0168,Warning01" });

            // TEST: Verify that last /warnaserror[+/-] flag on command line wins.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror-", "/warnaserror+" }, expectedErrorCount: 1);
            Assert.Contains("error CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that last /warnaserror[+/-] flag on command line wins.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror", "/warnaserror-" }, expectedWarningCount: 3);
            Assert.Contains("a.cs(2,7): warning Warning01: Throwing a diagnostic for types declared", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(6,13): warning CS0168: The variable 'i' is declared but never used", output, StringComparison.Ordinal);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that last /warnaserror[+/-]: flag on command line wins.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror-:Warning01", "/warnaserror+:Warning01" }, expectedWarningCount: 2, expectedErrorCount: 1);
            Assert.Contains("a.cs(2,7): error Warning01: Throwing a diagnostic for types declared", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(6,13): warning CS0168: The variable 'i' is declared but never used", output, StringComparison.Ordinal);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that last /warnaserror[+/-]: flag on command line wins.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror+:Warning01", "/warnaserror-:Warning01" }, expectedWarningCount: 3);
            Assert.Contains("a.cs(2,7): warning Warning01: Throwing a diagnostic for types declared", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(6,13): warning CS0168: The variable 'i' is declared but never used", output, StringComparison.Ordinal);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that last one wins between /warnaserror[+/-]: and /warnaserror[+/-].
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror-:Warning01,CS0168,58000,8032", "/warnaserror+" }, expectedErrorCount: 1);
            Assert.Contains("error CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that last one wins between /warnaserror[+/-] and /warnaserror[+/-]:.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror", "/warnaserror-:Warning01,CS0168,58000,8032" }, expectedWarningCount: 3);
            Assert.Contains("a.cs(2,7): warning Warning01: Throwing a diagnostic for types declared", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(6,13): warning CS0168: The variable 'i' is declared but never used", output, StringComparison.Ordinal);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that last one wins between /warnaserror[+/-]: and /warnaserror[+/-].
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror:Warning01,58000,8032", "/warnaserror-" }, expectedWarningCount: 3);
            Assert.Contains("a.cs(2,7): warning Warning01: Throwing a diagnostic for types declared", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(6,13): warning CS0168: The variable 'i' is declared but never used", output, StringComparison.Ordinal);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that last one wins between /warnaserror[+/-] and /warnaserror[+/-]:.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror-", "/warnaserror+:Warning01" }, expectedWarningCount: 2, expectedErrorCount: 1);
            Assert.Contains("a.cs(2,7): error Warning01: Throwing a diagnostic for types declared", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(6,13): warning CS0168: The variable 'i' is declared but never used", output, StringComparison.Ordinal);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that last one wins between /warnaserror[+/-]: and /warnaserror[+/-].
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror:Warning01,CS0168,58000", "/warnaserror+" }, expectedErrorCount: 1);
            Assert.Contains("error CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that last one wins between /warnaserror[+/-] and /warnaserror[+/-]:.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror", "/warnaserror+:Warning01,CS0168,58000" }, expectedErrorCount: 1);
            Assert.Contains("error CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that last one wins between /warnaserror[+/-]: and /warnaserror[+/-].
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror-:Warning01,58000,8032", "/warnaserror-" }, expectedWarningCount: 3);
            Assert.Contains("a.cs(2,7): warning Warning01: Throwing a diagnostic for types declared", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(6,13): warning CS0168: The variable 'i' is declared but never used", output, StringComparison.Ordinal);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that last one wins between /warnaserror[+/-] and /warnaserror[+/-]:.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror-", "/warnaserror-:Warning01,58000,8032" }, expectedWarningCount: 3);
            Assert.Contains("a.cs(2,7): warning Warning01: Throwing a diagnostic for types declared", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(6,13): warning CS0168: The variable 'i' is declared but never used", output, StringComparison.Ordinal);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            CleanupAllGeneratedFiles(file.Path);
        }

        [WorkItem(899050, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/899050")]
        [WorkItem(981677, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/981677")]
        [Fact]
        public void NoWarnAndWarnAsError_ErrorDiagnostic()
        {
            // This assembly has an ErrorDiagnosticAnalyzer type which should produce custom error
            // diagnostics for #pragma warning disable directives present in the compilations created in this test.
            string source = @"using System;
#pragma warning disable";
            var dir = Temp.CreateDirectory();
            var file = dir.CreateFile("a.cs");
            file.WriteAllText(source);

            var output = VerifyOutput(dir, file, expectedErrorCount: 1, expectedWarningCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(2,1): error Error01: Throwing a diagnostic for #pragma disable", output, StringComparison.Ordinal);

            // TEST: Verify that custom error diagnostic Error01 can't be suppressed via /warn:0.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warn:0" }, expectedErrorCount: 1);
            Assert.Contains("a.cs(2,1): error Error01: Throwing a diagnostic for #pragma disable", output, StringComparison.Ordinal);

            // TEST: Verify that custom error diagnostic Error01 can be suppressed via /nowarn:.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/nowarn:Error01" }, expectedWarningCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that /nowarn: overrides /warnaserror+.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror+", "/nowarn:Error01" }, expectedErrorCount: 1);
            Assert.Contains("error CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that /nowarn: overrides /warnaserror.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/nowarn:Error01", "/warnaserror" }, expectedErrorCount: 1);
            Assert.Contains("error CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that /nowarn: overrides /warnaserror+:.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/nowarn:Error01", "/warnaserror+:Error01" }, expectedWarningCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that /nowarn: overrides /warnaserror:.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror:Error01", "/nowarn:Error01" }, expectedWarningCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that /nowarn: overrides /warnaserror-.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror-", "/nowarn:Error01" }, expectedWarningCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that /nowarn: overrides /warnaserror-.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/nowarn:Error01", "/warnaserror-" }, expectedWarningCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that /nowarn: overrides /warnaserror-.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror-:Error01", "/nowarn:Error01" }, expectedWarningCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that /nowarn: overrides /warnaserror-.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/nowarn:Error01", "/warnaserror-:Error01" }, expectedWarningCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that nothing bad happens when using /warnaserror[+/-] when custom error diagnostic Error01 is present.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror" }, expectedErrorCount: 1);
            Assert.Contains("error CS8032", output, StringComparison.Ordinal);

            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror+" }, expectedErrorCount: 1);
            Assert.Contains("error CS8032", output, StringComparison.Ordinal);

            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror-" }, expectedErrorCount: 1, expectedWarningCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(2,1): error Error01: Throwing a diagnostic for #pragma disable", output, StringComparison.Ordinal);

            // TEST: Verify that nothing bad happens if someone passes custom error diagnostic Error01 to /warnaserror[+/-]:.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror:Error01" }, expectedErrorCount: 1, expectedWarningCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(2,1): error Error01: Throwing a diagnostic for #pragma disable", output, StringComparison.Ordinal);

            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror+:Error01" }, expectedErrorCount: 1, expectedWarningCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(2,1): error Error01: Throwing a diagnostic for #pragma disable", output, StringComparison.Ordinal);

            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror-:Error01" }, expectedErrorCount: 1, expectedWarningCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(2,1): error Error01: Throwing a diagnostic for #pragma disable", output, StringComparison.Ordinal);

            CleanupAllGeneratedFiles(file.Path);
        }

        [Fact]
        [WorkItem(11497, "https://github.com/dotnet/roslyn/issues/11497")]
        public void ConsistentErrorMessageWhenProvidingNoKeyFile()
        {
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/keyfile:", "/target:library", "/nologo", "/preferreduilang:en", "a.cs" });
            int exitCode = csc.Run(outWriter);

            Assert.Equal(1, exitCode);
            Assert.Equal("error CS2005: Missing file specification for 'keyfile' option", outWriter.ToString().Trim());
        }

        [Fact]
        [WorkItem(11497, "https://github.com/dotnet/roslyn/issues/11497")]
        public void ConsistentErrorMessageWhenProvidingEmptyKeyFile()
        {
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/keyfile:\"\"", "/target:library", "/nologo", "/preferreduilang:en", "a.cs" });
            int exitCode = csc.Run(outWriter);

            Assert.Equal(1, exitCode);
            Assert.Equal("error CS2005: Missing file specification for 'keyfile' option", outWriter.ToString().Trim());
        }

        [Fact]
        [WorkItem(11497, "https://github.com/dotnet/roslyn/issues/11497")]
        public void ConsistentErrorMessageWhenProvidingNoKeyFile_PublicSign()
        {
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/keyfile:", "/publicsign", "/target:library", "/nologo", "/preferreduilang:en", "a.cs" });
            int exitCode = csc.Run(outWriter);

            Assert.Equal(1, exitCode);
            Assert.Equal("error CS2005: Missing file specification for 'keyfile' option", outWriter.ToString().Trim());
        }

        [Fact]
        [WorkItem(11497, "https://github.com/dotnet/roslyn/issues/11497")]
        public void ConsistentErrorMessageWhenProvidingEmptyKeyFile_PublicSign()
        {
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(null, WorkingDirectory, new[] { "/keyfile:\"\"", "/publicsign", "/target:library", "/nologo", "/preferreduilang:en", "a.cs" });
            int exitCode = csc.Run(outWriter);

            Assert.Equal(1, exitCode);
            Assert.Equal("error CS2005: Missing file specification for 'keyfile' option", outWriter.ToString().Trim());
        }

        [WorkItem(981677, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/981677")]
        [Fact]
        public void NoWarnAndWarnAsError_CompilerErrorDiagnostic()
        {
            string source = @"using System;
class C
{
    static void Main()
    {
        int i = new Exception();
    }
}";
            var dir = Temp.CreateDirectory();
            var file = dir.CreateFile("a.cs");
            file.WriteAllText(source);

            var output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReference: false, expectedErrorCount: 1);
            Assert.Contains("a.cs(6,17): error CS0029: Cannot implicitly convert type 'System.Exception' to 'int'", output, StringComparison.Ordinal);

            // TEST: Verify that compiler error CS0029 can't be suppressed via /warn:0.
            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/warn:0" }, expectedErrorCount: 1);
            Assert.Contains("a.cs(6,17): error CS0029: Cannot implicitly convert type 'System.Exception' to 'int'", output, StringComparison.Ordinal);

            // TEST: Verify that compiler error CS0029 can't be suppressed via /nowarn:.
            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/nowarn:29" }, expectedErrorCount: 1);
            Assert.Contains("a.cs(6,17): error CS0029: Cannot implicitly convert type 'System.Exception' to 'int'", output, StringComparison.Ordinal);

            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/nowarn:CS0029" }, expectedErrorCount: 1);
            Assert.Contains("a.cs(6,17): error CS0029: Cannot implicitly convert type 'System.Exception' to 'int'", output, StringComparison.Ordinal);

            // TEST: Verify that nothing bad happens when using /warnaserror[+/-] when compiler error CS0029 is present.
            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/warnaserror" }, expectedErrorCount: 1);
            Assert.Contains("a.cs(6,17): error CS0029: Cannot implicitly convert type 'System.Exception' to 'int'", output, StringComparison.Ordinal);

            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/warnaserror+" }, expectedErrorCount: 1);
            Assert.Contains("a.cs(6,17): error CS0029: Cannot implicitly convert type 'System.Exception' to 'int'", output, StringComparison.Ordinal);

            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/warnaserror-" }, expectedErrorCount: 1);
            Assert.Contains("a.cs(6,17): error CS0029: Cannot implicitly convert type 'System.Exception' to 'int'", output, StringComparison.Ordinal);

            // TEST: Verify that nothing bad happens if someone passes compiler error CS0029 to /warnaserror[+/-]:.
            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/warnaserror:0029" }, expectedErrorCount: 1);
            Assert.Contains("a.cs(6,17): error CS0029: Cannot implicitly convert type 'System.Exception' to 'int'", output, StringComparison.Ordinal);

            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/warnaserror+:CS0029" }, expectedErrorCount: 1);
            Assert.Contains("a.cs(6,17): error CS0029: Cannot implicitly convert type 'System.Exception' to 'int'", output, StringComparison.Ordinal);

            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/warnaserror-:29" }, expectedErrorCount: 1);
            Assert.Contains("a.cs(6,17): error CS0029: Cannot implicitly convert type 'System.Exception' to 'int'", output, StringComparison.Ordinal);

            output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/warnaserror-:CS0029" }, expectedErrorCount: 1);
            Assert.Contains("a.cs(6,17): error CS0029: Cannot implicitly convert type 'System.Exception' to 'int'", output, StringComparison.Ordinal);

            CleanupAllGeneratedFiles(file.Path);
        }

        [WorkItem(1021115, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1021115")]
        [Fact]
        public void WarnAsError_LastOneWins1()
        {
            var arguments = DefaultParse(new[] { "/warnaserror-:3001", "/warnaserror" }, null);
            var options = arguments.CompilationOptions;

            var comp = CreateCompilation(@"[assembly: System.CLSCompliant(true)]
public class C
{
    public void M(ushort i)
    {
    }
    public static void Main(string[] args) {}
}", options: options);

            comp.VerifyDiagnostics(
                // (4,26): warning CS3001: Argument type 'ushort' is not CLS-compliant
                //     public void M(ushort i)
                Diagnostic(ErrorCode.WRN_CLS_BadArgType, "i")
                    .WithArguments("ushort")
                    .WithLocation(4, 26)
                    .WithWarningAsError(true));
        }

        [WorkItem(1021115, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1021115")]
        [Fact]
        public void WarnAsError_LastOneWins2()
        {
            var arguments = DefaultParse(new[] { "/warnaserror", "/warnaserror-:3001" }, null);
            var options = arguments.CompilationOptions;

            var comp = CreateCompilation(@"[assembly: System.CLSCompliant(true)]
public class C
{
    public void M(ushort i)
    {
    }
    public static void Main(string[] args) {}
}", options: options);

            comp.VerifyDiagnostics(
                // (4,26): warning CS3001: Argument type 'ushort' is not CLS-compliant
                //     public void M(ushort i)
                Diagnostic(ErrorCode.WRN_CLS_BadArgType, "i")
                    .WithArguments("ushort")
                    .WithLocation(4, 26)
                    .WithWarningAsError(false));
        }

        [WorkItem(1091972, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1091972")]
        [WorkItem(444, "CodePlex")]
        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/30321")]
        public void Bug1091972()
        {
            var dir = Temp.CreateDirectory();

            var src = dir.CreateFile("a.cs");
            src.WriteAllText(
@"
/// <summary>ABC...XYZ</summary>
class C {
    static void Main()
    {
        var textStreamReader = new System.IO.StreamReader(typeof(C).Assembly.GetManifestResourceStream(""doc.xml""));
        System.Console.WriteLine(textStreamReader.ReadToEnd());
    }
} ");

            var output = ProcessUtilities.RunAndGetOutput(s_CSharpCompilerExecutable, String.Format("/nologo /doc:doc.xml /out:out.exe /resource:doc.xml \"{0}\"", src.ToString()), startFolder: dir.ToString());
            Assert.Equal("", output.Trim());

            Assert.True(File.Exists(Path.Combine(dir.ToString(), "doc.xml")));

            var expected =
@"<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>out</name>
    </assembly>
    <members>
        <member name=""T:C"">
            <summary>ABC...XYZ</summary>
        </member>
    </members>
</doc>".Trim();

            using (var reader = new StreamReader(Path.Combine(dir.ToString(), "doc.xml")))
            {
                var content = reader.ReadToEnd();
                Assert.Equal(expected, content.Trim());
            }

            output = ProcessUtilities.RunAndGetOutput(Path.Combine(dir.ToString(), "out.exe"), startFolder: dir.ToString());
            Assert.Equal(expected, output.Trim());

            CleanupAllGeneratedFiles(src.Path);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void CommandLineMisc()
        {
            CSharpCommandLineArguments args = null;
            string baseDirectory = @"c:\test";
            Func<string, CSharpCommandLineArguments> parse = (x) => FullParse(x, baseDirectory);

            args = parse(@"/out:""a.exe""");
            Assert.Equal(@"a.exe", args.OutputFileName);

            args = parse(@"/pdb:""a.pdb""");
            Assert.Equal(Path.Combine(baseDirectory, @"a.pdb"), args.PdbPath);

            // The \ here causes " to be treated as a quote, not as an escaping construct
            args = parse(@"a\""b c""\d.cs");
            Assert.Equal(
                new[] { @"c:\test\a""b", @"c:\test\c\d.cs" },
                args.SourceFiles.Select(x => x.Path));

            args = parse(@"a\\""b c""\d.cs");
            Assert.Equal(
                new[] { @"c:\test\a\b c\d.cs" },
                args.SourceFiles.Select(x => x.Path));

            args = parse(@"/nostdlib /r:""a.dll"",""b.dll"" c.cs");
            Assert.Equal(
                new[] { @"a.dll", @"b.dll" },
                args.MetadataReferences.Select(x => x.Reference));

            args = parse(@"/nostdlib /r:""a-s.dll"",""b-s.dll"" c.cs");
            Assert.Equal(
                new[] { @"a-s.dll", @"b-s.dll" },
                args.MetadataReferences.Select(x => x.Reference));

            args = parse(@"/nostdlib /r:""a,;s.dll"",""b,;s.dll"" c.cs");
            Assert.Equal(
                new[] { @"a,;s.dll", @"b,;s.dll" },
                args.MetadataReferences.Select(x => x.Reference));
        }

        [Fact]
        public void CommandLine_ScriptRunner1()
        {
            var args = ScriptParse(new[] { "--", "script.csx", "b", "c" }, baseDirectory: WorkingDirectory);
            AssertEx.Equal(new[] { Path.Combine(WorkingDirectory, "script.csx") }, args.SourceFiles.Select(f => f.Path));
            AssertEx.Equal(new[] { "b", "c" }, args.ScriptArguments);

            args = ScriptParse(new[] { "--", "@script.csx", "b", "c" }, baseDirectory: WorkingDirectory);
            AssertEx.Equal(new[] { Path.Combine(WorkingDirectory, "@script.csx") }, args.SourceFiles.Select(f => f.Path));
            AssertEx.Equal(new[] { "b", "c" }, args.ScriptArguments);

            args = ScriptParse(new[] { "--", "-script.csx", "b", "c" }, baseDirectory: WorkingDirectory);
            AssertEx.Equal(new[] { Path.Combine(WorkingDirectory, "-script.csx") }, args.SourceFiles.Select(f => f.Path));
            AssertEx.Equal(new[] { "b", "c" }, args.ScriptArguments);

            args = ScriptParse(new[] { "script.csx", "--", "b", "c" }, baseDirectory: WorkingDirectory);
            AssertEx.Equal(new[] { Path.Combine(WorkingDirectory, "script.csx") }, args.SourceFiles.Select(f => f.Path));
            AssertEx.Equal(new[] { "--", "b", "c" }, args.ScriptArguments);

            args = ScriptParse(new[] { "script.csx", "a", "b", "c" }, baseDirectory: WorkingDirectory);
            AssertEx.Equal(new[] { Path.Combine(WorkingDirectory, "script.csx") }, args.SourceFiles.Select(f => f.Path));
            AssertEx.Equal(new[] { "a", "b", "c" }, args.ScriptArguments);

            args = ScriptParse(new[] { "script.csx", "a", "--", "b", "c" }, baseDirectory: WorkingDirectory);
            AssertEx.Equal(new[] { Path.Combine(WorkingDirectory, "script.csx") }, args.SourceFiles.Select(f => f.Path));
            AssertEx.Equal(new[] { "a", "--", "b", "c" }, args.ScriptArguments);

            args = ScriptParse(new[] { "-i", "script.csx", "a", "b", "c" }, baseDirectory: WorkingDirectory);
            Assert.True(args.InteractiveMode);
            AssertEx.Equal(new[] { Path.Combine(WorkingDirectory, "script.csx") }, args.SourceFiles.Select(f => f.Path));
            AssertEx.Equal(new[] { "a", "b", "c" }, args.ScriptArguments);

            args = ScriptParse(new[] { "-i", "--", "script.csx", "a", "b", "c" }, baseDirectory: WorkingDirectory);
            Assert.True(args.InteractiveMode);
            AssertEx.Equal(new[] { Path.Combine(WorkingDirectory, "script.csx") }, args.SourceFiles.Select(f => f.Path));
            AssertEx.Equal(new[] { "a", "b", "c" }, args.ScriptArguments);

            args = ScriptParse(new[] { "-i", "--", "--", "--" }, baseDirectory: WorkingDirectory);
            Assert.True(args.InteractiveMode);
            AssertEx.Equal(new[] { Path.Combine(WorkingDirectory, "--") }, args.SourceFiles.Select(f => f.Path));
            Assert.True(args.SourceFiles[0].IsScript);
            AssertEx.Equal(new[] { "--" }, args.ScriptArguments);

            // TODO: fails on Linux (https://github.com/dotnet/roslyn/issues/5904)
            // Result: C:\/script.csx
            //args = ScriptParse(new[] { "-i", "script.csx", "--", "--" }, baseDirectory: @"C:\");
            //Assert.True(args.InteractiveMode);
            //AssertEx.Equal(new[] { @"C:\script.csx" }, args.SourceFiles.Select(f => f.Path));
            //Assert.True(args.SourceFiles[0].IsScript);
            //AssertEx.Equal(new[] { "--" }, args.ScriptArguments);
        }

        [WorkItem(127403, "https://devdiv.visualstudio.com:443/defaultcollection/DevDiv/_workitems/edit/127403")]
        [Fact]
        public void ParseSeparatedPaths_QuotedComma()
        {
            var paths = CSharpCommandLineParser.ParseSeparatedPaths(@"""a, b""");
            Assert.Equal(
                new[] { @"a, b" },
                paths);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.Determinism)]
        public void PathMapParser()
        {
            var s = PathUtilities.DirectorySeparatorStr;

            var parsedArgs = DefaultParse(new[] { "/pathmap:", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ImmutableArray.Create<KeyValuePair<string, string>>(), parsedArgs.PathMap);

            parsedArgs = DefaultParse(new[] { "/pathmap:K1=V1", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(KeyValuePair.Create("K1" + s, "V1" + s), parsedArgs.PathMap[0]);

            parsedArgs = DefaultParse(new[] { $"/pathmap:abc{s}=/", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(KeyValuePair.Create("abc" + s, "/"), parsedArgs.PathMap[0]);

            parsedArgs = DefaultParse(new[] { "/pathmap:K1=V1,K2=V2", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(KeyValuePair.Create("K1" + s, "V1" + s), parsedArgs.PathMap[0]);
            Assert.Equal(KeyValuePair.Create("K2" + s, "V2" + s), parsedArgs.PathMap[1]);

            parsedArgs = DefaultParse(new[] { "/pathmap:,", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ImmutableArray.Create<KeyValuePair<string, string>>(), parsedArgs.PathMap);

            parsedArgs = DefaultParse(new[] { "/pathmap:,,", "a.cs" }, WorkingDirectory);
            Assert.Equal(1, parsedArgs.Errors.Count());
            Assert.Equal((int)ErrorCode.ERR_InvalidPathMap, parsedArgs.Errors[0].Code);

            parsedArgs = DefaultParse(new[] { "/pathmap:,,,", "a.cs" }, WorkingDirectory);
            Assert.Equal(1, parsedArgs.Errors.Count());
            Assert.Equal((int)ErrorCode.ERR_InvalidPathMap, parsedArgs.Errors[0].Code);

            parsedArgs = DefaultParse(new[] { "/pathmap:k=,=v", "a.cs" }, WorkingDirectory);
            Assert.Equal(2, parsedArgs.Errors.Count());
            Assert.Equal((int)ErrorCode.ERR_InvalidPathMap, parsedArgs.Errors[0].Code);
            Assert.Equal((int)ErrorCode.ERR_InvalidPathMap, parsedArgs.Errors[1].Code);

            parsedArgs = DefaultParse(new[] { "/pathmap:k=v=bad", "a.cs" }, WorkingDirectory);
            Assert.Equal(1, parsedArgs.Errors.Count());
            Assert.Equal((int)ErrorCode.ERR_InvalidPathMap, parsedArgs.Errors[0].Code);

            parsedArgs = DefaultParse(new[] { "/pathmap:k=", "a.cs" }, WorkingDirectory);
            Assert.Equal(1, parsedArgs.Errors.Count());
            Assert.Equal((int)ErrorCode.ERR_InvalidPathMap, parsedArgs.Errors[0].Code);

            parsedArgs = DefaultParse(new[] { "/pathmap:=v", "a.cs" }, WorkingDirectory);
            Assert.Equal(1, parsedArgs.Errors.Count());
            Assert.Equal((int)ErrorCode.ERR_InvalidPathMap, parsedArgs.Errors[0].Code);

            parsedArgs = DefaultParse(new[] { "/pathmap:\"supporting spaces=is hard\"", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(KeyValuePair.Create("supporting spaces" + s, "is hard" + s), parsedArgs.PathMap[0]);

            parsedArgs = DefaultParse(new[] { "/pathmap:\"K 1=V 1\",\"K 2=V 2\"", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(KeyValuePair.Create("K 1" + s, "V 1" + s), parsedArgs.PathMap[0]);
            Assert.Equal(KeyValuePair.Create("K 2" + s, "V 2" + s), parsedArgs.PathMap[1]);

            parsedArgs = DefaultParse(new[] { "/pathmap:\"K 1\"=\"V 1\",\"K 2\"=\"V 2\"", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(KeyValuePair.Create("K 1" + s, "V 1" + s), parsedArgs.PathMap[0]);
            Assert.Equal(KeyValuePair.Create("K 2" + s, "V 2" + s), parsedArgs.PathMap[1]);

            parsedArgs = DefaultParse(new[] { "/pathmap:\"a ==,,b\"=\"1,,== 2\",\"x ==,,y\"=\"3 4\",", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(KeyValuePair.Create("a =,b" + s, "1,= 2" + s), parsedArgs.PathMap[0]);
            Assert.Equal(KeyValuePair.Create("x =,y" + s, "3 4" + s), parsedArgs.PathMap[1]);

            parsedArgs = DefaultParse(new[] { @"/pathmap:C:\temp\=/_1/,C:\temp\a\=/_2/,C:\temp\a\b\=/_3/", "a.cs", @"a\b.cs", @"a\b\c.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(KeyValuePair.Create(@"C:\temp\a\b\", "/_3/"), parsedArgs.PathMap[0]);
            Assert.Equal(KeyValuePair.Create(@"C:\temp\a\", "/_2/"), parsedArgs.PathMap[1]);
            Assert.Equal(KeyValuePair.Create(@"C:\temp\", "/_1/"), parsedArgs.PathMap[2]);
        }

        [Theory]
        [InlineData("", new string[0])]
        [InlineData(",", new[] { "", "" })]
        [InlineData(",,", new[] { "," })]
        [InlineData(",,,", new[] { ",", "" })]
        [InlineData(",,,,", new[] { ",," })]
        [InlineData("a,", new[] { "a", "" })]
        [InlineData("a,b", new[] { "a", "b" })]
        [InlineData(",,a,,,,,b,,", new[] { ",a,,", "b," })]
        public void SplitWithDoubledSeparatorEscaping(string str, string[] expected)
        {
            AssertEx.Equal(expected, CommandLineParser.SplitWithDoubledSeparatorEscaping(str, ','));
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/30289")]
        [CompilerTrait(CompilerFeature.Determinism)]
        public void PathMapPdbParser()
        {
            var dir = Path.Combine(WorkingDirectory, "a");
            var parsedArgs = DefaultParse(new[] { $@"/pathmap:{dir}=b:\", "a.cs", @"/pdb:a\data.pdb", "/debug:full" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(Path.Combine(dir, @"data.pdb"), parsedArgs.PdbPath);

            // This value is calculate during Emit phases and should be null even in the face of a pathmap targeting it.
            Assert.Null(parsedArgs.EmitOptions.PdbFilePath);
        }

        [ConditionalFact(typeof(WindowsOnly), Reason = ConditionalSkipReason.NativePdbRequiresDesktop)]
        [CompilerTrait(CompilerFeature.Determinism)]
        public void PathMapPdbEmit()
        {
            void AssertPdbEmit(TempDirectory dir, string pdbPath, string pePdbPath, params string[] extraArgs)
            {
                var source = @"class Program { static void Main() { } }";
                var src = dir.CreateFile("a.cs").WriteAllText(source);
                var defaultArgs = new[] { "/nologo", "a.cs", "/out:a.exe", "/debug:full", $"/pdb:{pdbPath}" };
                var isDeterministic = extraArgs.Contains("/deterministic");
                var args = defaultArgs.Concat(extraArgs).ToArray();
                var outWriter = new StringWriter(CultureInfo.InvariantCulture);

                var csc = CreateCSharpCompiler(null, dir.Path, args);
                int exitCode = csc.Run(outWriter);
                Assert.Equal(0, exitCode);

                var exePath = Path.Combine(dir.Path, "a.exe");
                Assert.True(File.Exists(exePath));
                Assert.True(File.Exists(pdbPath));
                using (var peStream = File.OpenRead(exePath))
                {
                    PdbValidation.ValidateDebugDirectory(peStream, null, pePdbPath, hashAlgorithm: default, hasEmbeddedPdb: false, isDeterministic);
                }
            }

            // Case with no mappings
            using (var dir = new DisposableDirectory(Temp))
            {
                var pdbPath = Path.Combine(dir.Path, "a.pdb");
                AssertPdbEmit(dir, pdbPath, pdbPath);
            }

            // Simple mapping
            using (var dir = new DisposableDirectory(Temp))
            {
                var pdbPath = Path.Combine(dir.Path, "a.pdb");
                AssertPdbEmit(dir, pdbPath, @"q:\a.pdb", $@"/pathmap:{dir.Path}=q:\");
            }

            // Simple mapping deterministic
            using (var dir = new DisposableDirectory(Temp))
            {
                var pdbPath = Path.Combine(dir.Path, "a.pdb");
                AssertPdbEmit(dir, pdbPath, @"q:\a.pdb", $@"/pathmap:{dir.Path}=q:\", "/deterministic");
            }

            // Partial mapping
            using (var dir = new DisposableDirectory(Temp))
            {
                dir.CreateDirectory("pdb");
                var pdbPath = Path.Combine(dir.Path, @"pdb\a.pdb");
                AssertPdbEmit(dir, pdbPath, @"q:\pdb\a.pdb", $@"/pathmap:{dir.Path}=q:\");
            }

            // Legacy feature flag
            using (var dir = new DisposableDirectory(Temp))
            {
                Assert.Equal("pdb-path-determinism", Feature.PdbPathDeterminism);
                var pdbPath = Path.Combine(dir.Path, "a.pdb");
                AssertPdbEmit(dir, pdbPath, @"a.pdb", $@"/features:pdb-path-determinism");
            }

            // Unix path map
            using (var dir = new DisposableDirectory(Temp))
            {
                var pdbPath = Path.Combine(dir.Path, "a.pdb");
                AssertPdbEmit(dir, pdbPath, @"/a.pdb", $@"/pathmap:{dir.Path}=/");
            }

            // Multi-specified path map with mixed slashes
            using (var dir = new DisposableDirectory(Temp))
            {
                var pdbPath = Path.Combine(dir.Path, "a.pdb");
                AssertPdbEmit(dir, pdbPath, "/goo/a.pdb", $"/pathmap:{dir.Path}=/goo,{dir.Path}{PathUtilities.DirectorySeparatorChar}=/bar");
            }
        }

        [CompilerTrait(CompilerFeature.Determinism)]
        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/30321")]
        public void DeterministicPdbsRegardlessOfBitness()
        {
            var dir = Temp.CreateDirectory();
            var dir32 = dir.CreateDirectory("32");
            var dir64 = dir.CreateDirectory("64");

            var programExe32 = dir32.CreateFile("Program.exe");
            var programPdb32 = dir32.CreateFile("Program.pdb");
            var programExe64 = dir64.CreateFile("Program.exe");
            var programPdb64 = dir64.CreateFile("Program.pdb");

            var sourceFile = dir.CreateFile("Source.cs").WriteAllText(@"
using System;
using System.Linq;
using System.Collections.Generic;

namespace N
{
    using I4 = System.Int32;

    class Program
    {
        public static IEnumerable<int> F()
        {
            I4 x = 1;
            yield return 1;
            yield return x;
        }

        public static void Main(string[] args)
        {
            dynamic x = 1;
            const int a = 1;
            F().ToArray();
            Console.WriteLine(x + a);
        }
    }
}");
            var csc32src = $@"
using System;
using System.Reflection;

class Runner
{{
    static int Main(string[] args)
    {{
        var assembly = Assembly.LoadFrom(@""{s_CSharpCompilerExecutable}"");
        var program = assembly.GetType(""Microsoft.CodeAnalysis.CSharp.CommandLine.Program"");
        var main = program.GetMethod(""Main"");
        return (int)main.Invoke(null, new object[] {{ args }});
    }}
}}
";
            var csc32 = CreateCompilationWithMscorlib46(csc32src, options: TestOptions.ReleaseExe.WithPlatform(Platform.X86), assemblyName: "csc32");
            var csc32exe = dir.CreateFile("csc32.exe").WriteAllBytes(csc32.EmitToArray());

            dir.CopyFile(Path.ChangeExtension(s_CSharpCompilerExecutable, ".exe.config"), "csc32.exe.config");
            dir.CopyFile(Path.Combine(Path.GetDirectoryName(s_CSharpCompilerExecutable), "csc.rsp"));

            var output = ProcessUtilities.RunAndGetOutput(csc32exe.Path, $@"/nologo /debug:full /deterministic /out:Program.exe /pathmap:""{dir32.Path}""=X:\ ""{sourceFile.Path}""", expectedRetCode: 0, startFolder: dir32.Path);
            Assert.Equal("", output);

            output = ProcessUtilities.RunAndGetOutput(s_CSharpCompilerExecutable, $@"/nologo /debug:full /deterministic /out:Program.exe /pathmap:""{dir64.Path}""=X:\ ""{sourceFile.Path}""", expectedRetCode: 0, startFolder: dir64.Path);
            Assert.Equal("", output);

            AssertEx.Equal(programExe32.ReadAllBytes(), programExe64.ReadAllBytes());
            AssertEx.Equal(programPdb32.ReadAllBytes(), programPdb64.ReadAllBytes());
        }

        [WorkItem(7588, "https://github.com/dotnet/roslyn/issues/7588")]
        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/30321")]
        public void Version()
        {
            var folderName = Temp.CreateDirectory().ToString();
            var argss = new[]
            {
                "/version",
                "a.cs /version /preferreduilang:en",
                "/version /nologo",
                "/version /help",
            };

            foreach (var args in argss)
            {
                var output = ProcessUtilities.RunAndGetOutput(s_CSharpCompilerExecutable, args, startFolder: folderName);
                Assert.Equal(s_compilerVersion, output.Trim());
            }
        }

        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/30321")]
        public void RefOut()
        {
            var dir = Temp.CreateDirectory();
            var refDir = dir.CreateDirectory("ref");

            var src = dir.CreateFile("a.cs");
            src.WriteAllText(@"
public class C
{
    /// <summary>Main method</summary>
    public static void Main()
    {
        System.Console.Write(""Hello"");
    }
    /// <summary>Private method</summary>
    private static void PrivateMethod()
    {
        System.Console.Write(""Private"");
    }
}");

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(null, dir.Path,
                new[] { "/nologo", "/out:a.exe", "/refout:ref/a.dll", "/doc:doc.xml", "/deterministic", "/langversion:7", "a.cs" });

            int exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);

            var exe = Path.Combine(dir.Path, "a.exe");
            Assert.True(File.Exists(exe));

            MetadataReaderUtils.VerifyPEMetadata(exe,
                new[] { "TypeDefinition:<Module>", "TypeDefinition:C" },
                new[] { "MethodDefinition:Void C.Main()", "MethodDefinition:Void C.PrivateMethod()", "MethodDefinition:Void C..ctor()" },
                new[] { "CompilationRelaxationsAttribute", "RuntimeCompatibilityAttribute", "DebuggableAttribute" }
                );

            var doc = Path.Combine(dir.Path, "doc.xml");
            Assert.True(File.Exists(doc));

            var content = File.ReadAllText(doc);
            var expectedDoc =
@"<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>a</name>
    </assembly>
    <members>
        <member name=""M:C.Main"">
            <summary>Main method</summary>
        </member>
        <member name=""M:C.PrivateMethod"">
            <summary>Private method</summary>
        </member>
    </members>
</doc>";
            Assert.Equal(expectedDoc, content.Trim());

            var output = ProcessUtilities.RunAndGetOutput(exe, startFolder: dir.Path);
            Assert.Equal("Hello", output.Trim());

            var refDll = Path.Combine(refDir.Path, "a.dll");
            Assert.True(File.Exists(refDll));

            // The types and members that are included needs further refinement.
            // See issue https://github.com/dotnet/roslyn/issues/17612
            MetadataReaderUtils.VerifyPEMetadata(refDll,
                new[] { "TypeDefinition:<Module>", "TypeDefinition:C" },
                new[] { "MethodDefinition:Void C.Main()", "MethodDefinition:Void C..ctor()" },
                new[] { "CompilationRelaxationsAttribute", "RuntimeCompatibilityAttribute", "DebuggableAttribute", "ReferenceAssemblyAttribute" }
                );

            // Clean up temp files
            CleanupAllGeneratedFiles(dir.Path);
            CleanupAllGeneratedFiles(refDir.Path);
        }

        [Fact]
        public void RefOutWithError()
        {
            var dir = Temp.CreateDirectory();
            dir.CreateDirectory("ref");

            var src = dir.CreateFile("a.cs");
            src.WriteAllText(@"class C { public static void Main() { error(); } }");

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(null, dir.Path,
                new[] { "/nologo", "/out:a.dll", "/refout:ref/a.dll", "/deterministic", "/preferreduilang:en", "a.cs" });
            int exitCode = csc.Run(outWriter);
            Assert.Equal(1, exitCode);

            var dll = Path.Combine(dir.Path, "a.dll");
            Assert.False(File.Exists(dll));

            var refDll = Path.Combine(dir.Path, Path.Combine("ref", "a.dll"));
            Assert.False(File.Exists(refDll));

            Assert.Equal("a.cs(1,39): error CS0103: The name 'error' does not exist in the current context", outWriter.ToString().Trim());

            // Clean up temp files
            CleanupAllGeneratedFiles(dir.Path);
        }

        [Fact]
        public void RefOnly()
        {
            var dir = Temp.CreateDirectory();

            var src = dir.CreateFile("a.cs");
            src.WriteAllText(@"
using System;
class C
{
    /// <summary>Main method</summary>
    public static void Main()
    {
        error(); // semantic error in method body
    }
    private event Action E1
    {
        add { }
        remove { }
    }
    private event Action E2;

    /// <summary>Private Class Field</summary>
    private int field;

    /// <summary>Private Struct</summary>
    private struct S
    {
        /// <summary>Private Struct Field</summary>
        private int field;
    }
}");

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(null, dir.Path,
                new[] { "/nologo", "/out:a.dll", "/refonly", "/debug", "/deterministic", "/langversion:7", "/doc:doc.xml", "a.cs" });
            int exitCode = csc.Run(outWriter);
            Assert.Equal("", outWriter.ToString());
            Assert.Equal(0, exitCode);

            var refDll = Path.Combine(dir.Path, "a.dll");
            Assert.True(File.Exists(refDll));

            // The types and members that are included needs further refinement.
            // See issue https://github.com/dotnet/roslyn/issues/17612
            MetadataReaderUtils.VerifyPEMetadata(refDll,
                new[] { "TypeDefinition:<Module>", "TypeDefinition:C", "TypeDefinition:S" },
                new[] { "MethodDefinition:Void C.Main()", "MethodDefinition:Void C..ctor()" },
                new[] { "CompilationRelaxationsAttribute", "RuntimeCompatibilityAttribute", "DebuggableAttribute", "ReferenceAssemblyAttribute" }
                );

            var pdb = Path.Combine(dir.Path, "a.pdb");
            Assert.False(File.Exists(pdb));

            var doc = Path.Combine(dir.Path, "doc.xml");
            Assert.True(File.Exists(doc));

            var content = File.ReadAllText(doc);
            var expectedDoc =
@"<?xml version=""1.0""?>
<doc>
    <assembly>
        <name>a</name>
    </assembly>
    <members>
        <member name=""M:C.Main"">
            <summary>Main method</summary>
        </member>
        <member name=""F:C.field"">
            <summary>Private Class Field</summary>
        </member>
        <member name=""T:C.S"">
            <summary>Private Struct</summary>
        </member>
        <member name=""F:C.S.field"">
            <summary>Private Struct Field</summary>
        </member>
    </members>
</doc>";
            Assert.Equal(expectedDoc, content.Trim());

            // Clean up temp files
            CleanupAllGeneratedFiles(dir.Path);
        }

        [Fact]
        public void CompilingCodeWithInvalidPreProcessorSymbolsShouldProvideDiagnostics()
        {
            var parsedArgs = DefaultParse(new[] { "/define:1", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // warning CS2029: Invalid name for a preprocessing symbol; '1' is not a valid identifier
                Diagnostic(ErrorCode.WRN_DefineIdentifierRequired).WithArguments("1").WithLocation(1, 1));
        }

        [Fact]
        public void WhitespaceInDefine()
        {
            var parsedArgs = DefaultParse(new[] { "/define:\" a\"", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal("a", parsedArgs.ParseOptions.PreprocessorSymbols.Single());
        }

        [Fact]
        public void WhitespaceInDefine_OnlySpaces()
        {
            var parsedArgs = DefaultParse(new[] { "/define:\"   \"", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                Diagnostic(ErrorCode.WRN_DefineIdentifierRequired).WithArguments("   ").WithLocation(1, 1)
                );
            Assert.True(parsedArgs.ParseOptions.PreprocessorSymbols.IsEmpty);
        }

        [Fact]
        public void CompilingCodeWithInvalidLanguageVersionShouldProvideDiagnostics()
        {
            var parsedArgs = DefaultParse(new[] { "/langversion:1000", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // error CS1617: Invalid option '1000' for /langversion. Use '/langversion:?' to list supported values.
                Diagnostic(ErrorCode.ERR_BadCompatMode).WithArguments("1000").WithLocation(1, 1));
        }

        [Fact, WorkItem(16913, "https://github.com/dotnet/roslyn/issues/16913")]
        public void CompilingCodeWithMultipleInvalidPreProcessorSymbolsShouldErrorOut()
        {
            var parsedArgs = DefaultParse(new[] { "/define:valid1,2invalid,valid3", "/define:4,5,valid6", "a.cs" }, WorkingDirectory);
            parsedArgs.Errors.Verify(
                // warning CS2029: Invalid value for '/define'; '2invalid' is not a valid identifier
                Diagnostic(ErrorCode.WRN_DefineIdentifierRequired).WithArguments("2invalid"),
                // warning CS2029: Invalid value for '/define'; '4' is not a valid identifier
                Diagnostic(ErrorCode.WRN_DefineIdentifierRequired).WithArguments("4"),
                // warning CS2029: Invalid value for '/define'; '5' is not a valid identifier
                Diagnostic(ErrorCode.WRN_DefineIdentifierRequired).WithArguments("5"));
        }

        [WorkItem(406649, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=406649")]
        [ConditionalFact(typeof(WindowsDesktopOnly), typeof(IsEnglishLocal), Reason = "https://github.com/dotnet/roslyn/issues/30321")]
        public void MissingCompilerAssembly()
        {
            var dir = Temp.CreateDirectory();
            var cscPath = dir.CopyFile(s_CSharpCompilerExecutable).Path;
            dir.CopyFile(typeof(Compilation).Assembly.Location);

            // Missing Microsoft.CodeAnalysis.CSharp.dll.
            var result = ProcessUtilities.Run(cscPath, arguments: "/nologo /t:library unknown.cs", workingDirectory: dir.Path);
            Assert.Equal(1, result.ExitCode);
            AssertEx.Equal(
                $"Could not load file or assembly '{typeof(CSharpCompilation).Assembly.FullName}' or one of its dependencies. The system cannot find the file specified.",
                result.Output.Trim());

            // Missing System.Collections.Immutable.dll.
            dir.CopyFile(typeof(CSharpCompilation).Assembly.Location);
            result = ProcessUtilities.Run(cscPath, arguments: "/nologo /t:library unknown.cs", workingDirectory: dir.Path);
            Assert.Equal(1, result.ExitCode);
            AssertEx.Equal(
                $"Could not load file or assembly '{typeof(ImmutableArray).Assembly.FullName.Replace(".1", ".0")}' or one of its dependencies. The system cannot find the file specified.",
                result.Output.Trim());
        }

#if NET472
        [ConditionalFact(typeof(WindowsDesktopOnly), typeof(IsEnglishLocal), Reason = "https://github.com/dotnet/roslyn/issues/30321")]
        public void LoadinganalyzerNetStandard13()
        {
            var analyzerFileName = "AnalyzerNS13.dll";
            var srcFileName = "src.cs";

            var analyzerDir = Temp.CreateDirectory();

            var analyzerFile = analyzerDir.CreateFile(analyzerFileName).WriteAllBytes(CreateCSharpAnalyzerNetStandard13(Path.GetFileNameWithoutExtension(analyzerFileName)));
            var srcFile = analyzerDir.CreateFile(srcFileName).WriteAllText("public class C { }");

            var result = ProcessUtilities.Run(s_CSharpCompilerExecutable, arguments: $"/nologo /t:library /analyzer:{analyzerFileName} {srcFileName}", workingDirectory: analyzerDir.Path);
            var outputWithoutPaths = Regex.Replace(result.Output, " in .*", "");
            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                $@"warning AD0001: Analyzer 'TestAnalyzer' threw an exception of type 'System.NotImplementedException' with message '28'.
System.NotImplementedException: 28
   at TestAnalyzer.get_SupportedDiagnostics()
   at Microsoft.CodeAnalysis.Diagnostics.AnalyzerManager.AnalyzerExecutionContext.<>c__DisplayClass21_0.<ComputeDiagnosticDescriptors_NoLock>b__0(Object _)
   at Microsoft.CodeAnalysis.Diagnostics.AnalyzerExecutor.ExecuteAndCatchIfThrows_NoLock[TArg](DiagnosticAnalyzer analyzer, Action`1 analyze, TArg argument, Nullable`1 info, CancellationToken cancellationToken)
-----
Analyzer 'TestAnalyzer' threw an exception of type 'System.NotImplementedException' with message '28'.
System.NotImplementedException: 28
   at TestAnalyzer.get_SupportedDiagnostics()
   at Microsoft.CodeAnalysis.Diagnostics.AnalyzerExecutor.CreateDisablingMessage(DiagnosticAnalyzer analyzer, String analyzerName)
-----
", outputWithoutPaths);

            Assert.Equal(0, result.ExitCode);
        }

        private static ImmutableArray<byte> CreateCSharpAnalyzerNetStandard13(string analyzerAssemblyName)
        {
            var minSystemCollectionsImmutableSource = @"
[assembly: System.Reflection.AssemblyVersion(""1.2.3.0"")]

namespace System.Collections.Immutable
{
    public struct ImmutableArray<T>
    {
    }
}
";

            var minCodeAnalysisSource = @"
using System;

[assembly: System.Reflection.AssemblyVersion(""2.0.0.0"")]

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DiagnosticAnalyzerAttribute : Attribute
    {
        public DiagnosticAnalyzerAttribute(string firstLanguage, params string[] additionalLanguages) {}
    }

    public abstract class DiagnosticAnalyzer
    {
        public abstract System.Collections.Immutable.ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
        public abstract void Initialize(AnalysisContext context);
    }

    public abstract class AnalysisContext
    {
    }
}

namespace Microsoft.CodeAnalysis
{
    public sealed class DiagnosticDescriptor
    {
    }
}
";
            var minSystemCollectionsImmutableImage = CSharpCompilation.Create(
                "System.Collections.Immutable",
                new[] { SyntaxFactory.ParseSyntaxTree(minSystemCollectionsImmutableSource) },
                new MetadataReference[] { NetStandard13.References.SystemRuntime },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, cryptoPublicKey: TestResources.TestKeys.PublicKey_b03f5f7f11d50a3a)).EmitToArray();

            var minSystemCollectionsImmutableRef = MetadataReference.CreateFromImage(minSystemCollectionsImmutableImage);

            var minCodeAnalysisImage = CSharpCompilation.Create(
                "Microsoft.CodeAnalysis",
                new[] { SyntaxFactory.ParseSyntaxTree(minCodeAnalysisSource) },
                new MetadataReference[]
                {
                    NetStandard13.References.SystemRuntime,
                    minSystemCollectionsImmutableRef
                },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, cryptoPublicKey: TestResources.TestKeys.PublicKey_31bf3856ad364e35)).EmitToArray();

            var minCodeAnalysisRef = MetadataReference.CreateFromImage(minCodeAnalysisImage);

            var analyzerSource = @"
using System;
using System.Collections.ObjectModel;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Win32.SafeHandles;

[DiagnosticAnalyzer(""C#"")]
public class TestAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => throw new NotImplementedException(new[]                                 
    {                                                                                                                                   
        typeof(Win32Exception),           // Microsoft.Win32.Primitives
        typeof(AppContext),               // System.AppContext
        typeof(Console),                  // System.Console
        typeof(ValueTuple),               // System.ValueTuple
        typeof(FileVersionInfo),          // System.Diagnostics.FileVersionInfo
        typeof(Process),                  // System.Diagnostics.Process
        typeof(ChineseLunisolarCalendar), // System.Globalization.Calendars
        typeof(ZipArchive),               // System.IO.Compression
        typeof(ZipFile),                  // System.IO.Compression.ZipFile
        typeof(FileOptions),              // System.IO.FileSystem
        typeof(FileAttributes),           // System.IO.FileSystem.Primitives
        typeof(HttpClient),               // System.Net.Http
        typeof(AuthenticatedStream),      // System.Net.Security
        typeof(IOControlCode),            // System.Net.Sockets
        typeof(RuntimeInformation),       // System.Runtime.InteropServices.RuntimeInformation
        typeof(SerializationException),   // System.Runtime.Serialization.Primitives
        typeof(GenericIdentity),          // System.Security.Claims
        typeof(Aes),                      // System.Security.Cryptography.Algorithms
        typeof(CspParameters),            // System.Security.Cryptography.Csp
        typeof(AsnEncodedData),           // System.Security.Cryptography.Encoding
        typeof(AsymmetricAlgorithm),      // System.Security.Cryptography.Primitives
        typeof(SafeX509ChainHandle),      // System.Security.Cryptography.X509Certificates
        typeof(IXmlLineInfo),             // System.Xml.ReaderWriter
        typeof(XmlNode),                  // System.Xml.XmlDocument
        typeof(XPathDocument),            // System.Xml.XPath
        typeof(XDocumentExtensions),      // System.Xml.XPath.XDocument
        typeof(CodePagesEncodingProvider),// System.Text.Encoding.CodePages
        typeof(ValueTask<>),              // System.Threading.Tasks.Extensions

        // csc doesn't ship with facades for the following assemblies. 
        // Analyzers can't use them unless they carry the facade with them.

        // typeof(SafePipeHandle),           // System.IO.Pipes
        // typeof(StackFrame),               // System.Diagnostics.StackTrace
        // typeof(BindingFlags),             // System.Reflection.TypeExtensions
        // typeof(AccessControlActions),     // System.Security.AccessControl
        // typeof(SafeAccessTokenHandle),    // System.Security.Principal.Windows
        // typeof(Thread),                   // System.Threading.Thread
    }.Length.ToString());

    public override void Initialize(AnalysisContext context)
    {
    }
}";

            var references =
                new MetadataReference[]
                {
                    minCodeAnalysisRef,
                    minSystemCollectionsImmutableRef
                };
            references = references.Concat(NetStandard13.References.All).ToArray();

            var analyzerImage = CSharpCompilation.Create(
                analyzerAssemblyName,
                new SyntaxTree[] { SyntaxFactory.ParseSyntaxTree(analyzerSource) },
                references: references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)).EmitToArray();

            return analyzerImage;
        }

#endif

        [WorkItem(406649, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=484417")]
        [ConditionalFact(typeof(WindowsDesktopOnly), typeof(IsEnglishLocal), Reason = "https://github.com/dotnet/roslyn/issues/30321")]
        public void MicrosoftDiaSymReaderNativeAltLoadPath()
        {
            var dir = Temp.CreateDirectory();
            var cscDir = Path.GetDirectoryName(s_CSharpCompilerExecutable);

            // copy csc and dependencies except for DSRN:
            foreach (var filePath in Directory.EnumerateFiles(cscDir))
            {
                var fileName = Path.GetFileName(filePath);

                if (fileName.StartsWith("csc") ||
                    fileName.StartsWith("System.") ||
                    fileName.StartsWith("Microsoft.") && !fileName.StartsWith("Microsoft.DiaSymReader.Native"))
                {
                    dir.CopyFile(filePath);
                }
            }

            dir.CreateFile("Source.cs").WriteAllText("class C { void F() { } }");

            var cscCopy = Path.Combine(dir.Path, "csc.exe");

            var arguments = "/nologo /t:library /debug:full Source.cs";

            // env variable not set (deterministic) -- DSRN is required:
            var result = ProcessUtilities.Run(
                cscCopy,
                arguments + " /deterministic",
                workingDirectory: dir.Path,
                additionalEnvironmentVars: new[] { KeyValuePair.Create("MICROSOFT_DIASYMREADER_NATIVE_ALT_LOAD_PATH", "") });

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
                "error CS0041: Unexpected error writing debug information -- 'Unable to load DLL 'Microsoft.DiaSymReader.Native.amd64.dll': " +
                "The specified module could not be found. (Exception from HRESULT: 0x8007007E)'", result.Output.Trim());

            // env variable not set (non-deterministic) -- globally registered SymReader is picked up:
            result = ProcessUtilities.Run(cscCopy, arguments, workingDirectory: dir.Path);
            AssertEx.AssertEqualToleratingWhitespaceDifferences("", result.Output.Trim());

            // env variable set:
            result = ProcessUtilities.Run(
                cscCopy,
                arguments + " /deterministic",
                workingDirectory: dir.Path,
                additionalEnvironmentVars: new[] { KeyValuePair.Create("MICROSOFT_DIASYMREADER_NATIVE_ALT_LOAD_PATH", cscDir) });

            Assert.Equal("", result.Output.Trim());
        }

        [ConditionalFact(typeof(WindowsOnly))]
        [WorkItem(21935, "https://github.com/dotnet/roslyn/issues/21935")]
        public void PdbPathNotEmittedWithoutPdb()
        {
            var dir = Temp.CreateDirectory();

            var source = @"class Program { static void Main() { } }";
            var src = dir.CreateFile("a.cs").WriteAllText(source);
            var args = new[] { "/nologo", "a.cs", "/out:a.exe", "/debug-" };
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);

            var csc = CreateCSharpCompiler(null, dir.Path, args);
            int exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);

            var exePath = Path.Combine(dir.Path, "a.exe");
            Assert.True(File.Exists(exePath));
            using (var peStream = File.OpenRead(exePath))
            using (var peReader = new PEReader(peStream))
            {
                var debugDirectory = peReader.PEHeaders.PEHeader.DebugTableDirectory;
                Assert.Equal(0, debugDirectory.Size);
                Assert.Equal(0, debugDirectory.RelativeVirtualAddress);
            }
        }

        [Fact]
        public void StrongNameProviderWithCustomTempPath()
        {
            var tempDir = Temp.CreateDirectory();
            var workingDir = Temp.CreateDirectory();
            workingDir.CreateFile("a.cs");

            var buildPaths = new BuildPaths(clientDir: "", workingDir: workingDir.Path, sdkDir: null, tempDir: tempDir.Path);
            var csc = new MockCSharpCompiler(null, buildPaths, args: new[] { "/features:UseLegacyStrongNameProvider", "/nostdlib", "a.cs" });
            var comp = csc.CreateCompilation(new StringWriter(), new TouchedFileLogger(), errorLogger: null);
            Assert.True(!comp.SignUsingBuilder);
        }

        [Theory]
        [InlineData(@"/features:InterceptorsNamespaces=NS1.NS2;NS3.NS4")]
        [InlineData(@"/features:""InterceptorsNamespaces=NS1.NS2;NS3.NS4""")]
        public void FeaturesInterceptorsNamespaces_OptionParsing(string features)
        {
            Assert.Equal("InterceptorsNamespaces", Feature.InterceptorsNamespaces);
            var tempDir = Temp.CreateDirectory();
            var workingDir = Temp.CreateDirectory();
            workingDir.CreateFile("a.cs");

            var buildPaths = new BuildPaths(clientDir: "", workingDir: workingDir.Path, sdkDir: null, tempDir: tempDir.Path);
            var csc = new MockCSharpCompiler(null, buildPaths, args: new[] { features, "a.cs" });
            var comp = (CSharpCompilation)csc.CreateCompilation(new StringWriter(), new TouchedFileLogger(), errorLogger: null);
            var options = comp.SyntaxTrees[0].Options;
            Assert.Equal(1, options.Features.Count);
            Assert.Equal("NS1.NS2;NS3.NS4", options.Features[Feature.InterceptorsNamespaces]);

            var previewNamespaces = ((CSharpParseOptions)options).InterceptorsNamespaces;
            Assert.Equal(2, previewNamespaces.Length);
            Assert.Equal(new[] { "NS1", "NS2" }, previewNamespaces[0]);
            Assert.Equal(new[] { "NS3", "NS4" }, previewNamespaces[1]);
        }

        [Fact]
        public void FeaturesInterceptorsNamespaces_Duplicate()
        {
            var tempDir = Temp.CreateDirectory();
            var workingDir = Temp.CreateDirectory();
            workingDir.CreateFile("a.cs");

            var buildPaths = new BuildPaths(clientDir: "", workingDir: workingDir.Path, sdkDir: null, tempDir: tempDir.Path);
            var csc = new MockCSharpCompiler(null, buildPaths, args: new[] { @"/features:InterceptorsNamespaces=NS1.NS2", @"/features:InterceptorsNamespaces=NS3.NS4", "a.cs" });
            var comp = (CSharpCompilation)csc.CreateCompilation(new StringWriter(), new TouchedFileLogger(), errorLogger: null);
            var options = comp.SyntaxTrees[0].Options;
            Assert.Equal(1, options.Features.Count);
            Assert.Equal("NS3.NS4", options.Features[Feature.InterceptorsNamespaces]);

            var previewNamespaces = ((CSharpParseOptions)options).InterceptorsNamespaces;
            Assert.Equal(1, previewNamespaces.Length);
            Assert.Equal(new[] { "NS3", "NS4" }, previewNamespaces[0]);
        }

        [Fact]
        public void FeaturesInterceptorsPreviewNamespaces_NotRecognizedInCommandLine()
        {
            // '<InterceptorsPreviewNamespaces>' is recognized in the build task and passed through as a '/features:InterceptorsNamespaces=...' argument.
            // '/features:InterceptorsPreviewNamespaces=...' is included in the Features dictionary but does not enable the interceptors feature.
            var tempDir = Temp.CreateDirectory();
            var workingDir = Temp.CreateDirectory();
            workingDir.CreateFile("a.cs");

            var buildPaths = new BuildPaths(clientDir: "", workingDir: workingDir.Path, sdkDir: null, tempDir: tempDir.Path);
            var csc = new MockCSharpCompiler(null, buildPaths, args: new[] { @"/features:InterceptorsPreviewNamespaces=NS1.NS2", "a.cs" });
            var comp = (CSharpCompilation)csc.CreateCompilation(new StringWriter(), new TouchedFileLogger(), errorLogger: null);
            var options = comp.SyntaxTrees[0].Options;

            Assert.Equal(1, options.Features.Count);
            Assert.Equal("NS1.NS2", options.Features["InterceptorsPreviewNamespaces"]);

            Assert.False(options.HasFeature(Feature.InterceptorsNamespaces));
            Assert.Empty(((CSharpParseOptions)options).InterceptorsNamespaces);
        }

        public class QuotedArgumentTests : CommandLineTestBase
        {
            private static readonly string s_rootPath = ExecutionConditionUtil.IsWindows
                ? @"c:\"
                : "/";

            private void VerifyQuotedValid<T>(string name, string value, T expected, Func<CSharpCommandLineArguments, T> getValue)
            {
                var args = DefaultParse(new[] { $"/{name}:{value}", "a.cs" }, s_rootPath);
                Assert.Equal(0, args.Errors.Length);
                Assert.Equal(expected, getValue(args));

                args = DefaultParse(new[] { $@"/{name}:""{value}""", "a.cs" }, s_rootPath);
                Assert.Equal(0, args.Errors.Length);
                Assert.Equal(expected, getValue(args));
            }

            private void VerifyQuotedInvalid<T>(string name, string value, T expected, Func<CSharpCommandLineArguments, T> getValue)
            {
                var args = DefaultParse(new[] { $"/{name}:{value}", "a.cs" }, s_rootPath);
                Assert.Equal(0, args.Errors.Length);
                Assert.Equal(expected, getValue(args));

                args = DefaultParse(new[] { $@"/{name}:""{value}""", "a.cs" }, s_rootPath);
                Assert.True(args.Errors.Length > 0);
            }

            [WorkItem(12427, "https://github.com/dotnet/roslyn/issues/12427")]
            [Fact]
            public void DebugFlag()
            {
                var platformPdbKind = PathUtilities.IsUnixLikePlatform ? DebugInformationFormat.PortablePdb : DebugInformationFormat.Pdb;

                var list = new List<Tuple<string, DebugInformationFormat>>()
                {
                    Tuple.Create("portable", DebugInformationFormat.PortablePdb),
                    Tuple.Create("full", platformPdbKind),
                    Tuple.Create("pdbonly", platformPdbKind),
                    Tuple.Create("embedded", DebugInformationFormat.Embedded)
                };

                foreach (var tuple in list)
                {
                    VerifyQuotedValid("debug", tuple.Item1, tuple.Item2, x => x.EmitOptions.DebugInformationFormat);
                }
            }

            [WorkItem(12427, "https://github.com/dotnet/roslyn/issues/12427")]
            [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/30328")]
            public void CodePage()
            {
                VerifyQuotedValid("codepage", "1252", 1252, x => x.Encoding.CodePage);
            }

            [WorkItem(12427, "https://github.com/dotnet/roslyn/issues/12427")]
            [Fact]
            public void Target()
            {
                var list = new List<Tuple<string, OutputKind>>()
                {
                    Tuple.Create("exe", OutputKind.ConsoleApplication),
                    Tuple.Create("winexe", OutputKind.WindowsApplication),
                    Tuple.Create("library", OutputKind.DynamicallyLinkedLibrary),
                    Tuple.Create("module", OutputKind.NetModule),
                    Tuple.Create("appcontainerexe", OutputKind.WindowsRuntimeApplication),
                    Tuple.Create("winmdobj", OutputKind.WindowsRuntimeMetadata)
                };

                foreach (var tuple in list)
                {
                    VerifyQuotedInvalid("target", tuple.Item1, tuple.Item2, x => x.CompilationOptions.OutputKind);
                }
            }

            [WorkItem(12427, "https://github.com/dotnet/roslyn/issues/12427")]
            [Fact]
            public void PlatformFlag()
            {
                var list = new List<Tuple<string, Platform>>()
                {
                    Tuple.Create("x86", Platform.X86),
                    Tuple.Create("x64", Platform.X64),
                    Tuple.Create("itanium", Platform.Itanium),
                    Tuple.Create("anycpu", Platform.AnyCpu),
                    Tuple.Create("anycpu32bitpreferred",Platform.AnyCpu32BitPreferred),
                    Tuple.Create("arm", Platform.Arm)
                };

                foreach (var tuple in list)
                {
                    VerifyQuotedValid("platform", tuple.Item1, tuple.Item2, x => x.CompilationOptions.Platform);
                }
            }

            [WorkItem(12427, "https://github.com/dotnet/roslyn/issues/12427")]
            [Fact]
            public void WarnFlag()
            {
                VerifyQuotedValid("warn", "1", 1, x => x.CompilationOptions.WarningLevel);
            }

            [WorkItem(12427, "https://github.com/dotnet/roslyn/issues/12427")]
            [Fact]
            public void LangVersionFlag()
            {
                VerifyQuotedValid("langversion", "2", LanguageVersion.CSharp2, x => x.ParseOptions.LanguageVersion);
            }
        }

        [Fact]
        [WorkItem(23525, "https://github.com/dotnet/roslyn/issues/23525")]
        public void InvalidPathCharacterInPathMap()
        {
            string filePath = Temp.CreateFile().WriteAllText("").Path;
            var compiler = CreateCSharpCompiler(null, WorkingDirectory, new[]
            {
                filePath,
                "/debug:embedded",
                "/pathmap:test\\=\"",
                "/target:library",
                "/preferreduilang:en"
            });

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = compiler.Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Contains("error CS8101: The pathmap option was incorrectly formatted.", outWriter.ToString(), StringComparison.Ordinal);
        }

        [WorkItem(23525, "https://github.com/dotnet/roslyn/issues/23525")]
        [ConditionalFact(typeof(WindowsDesktopOnly), Reason = "https://github.com/dotnet/roslyn/issues/30289")]
        public void InvalidPathCharacterInPdbPath()
        {
            string filePath = Temp.CreateFile().WriteAllText("").Path;
            var compiler = CreateCSharpCompiler(null, WorkingDirectory, new[]
            {
                filePath,
                "/debug:embedded",
                "/pdb:test\\?.pdb",
                "/target:library",
                "/preferreduilang:en"
            });

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = compiler.Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Contains("error CS2021: File name 'test\\?.pdb' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long", outWriter.ToString(), StringComparison.Ordinal);
        }

        [WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")]
        [ConditionalFact(typeof(IsEnglishLocal))]
        public void TestSuppression_CompilerParserWarningAsError()
        {
            string source = @"
class C
{
    long M(int i)
    {
        // warning CS0078 : The 'l' suffix is easily confused with the digit '1' -- use 'L' for clarity
        return 0l;
    }
}
";
            var srcDirectory = Temp.CreateDirectory();
            var srcFile = srcDirectory.CreateFile("a.cs");
            srcFile.WriteAllText(source);

            // Verify that parser warning CS0078 is reported.
            var output = VerifyOutput(srcDirectory, srcFile, expectedWarningCount: 1, includeCurrentAssemblyAsAnalyzerReference: false);
            Assert.Contains("warning CS0078", output, StringComparison.Ordinal);

            // Verify that parser warning CS0078 is reported as error for /warnaserror.
            output = VerifyOutput(srcDirectory, srcFile, expectedErrorCount: 1,
                additionalFlags: new[] { "/warnAsError" }, includeCurrentAssemblyAsAnalyzerReference: false);
            Assert.Contains("error CS0078", output, StringComparison.Ordinal);

            // Verify that parser warning CS0078 is suppressed with diagnostic suppressor even with /warnaserror
            // and info diagnostic is logged with programmatic suppression information.
            var suppressor = new DiagnosticSuppressorForId("CS0078");
            output = VerifyOutput(srcDirectory, srcFile, expectedInfoCount: 1, expectedWarningCount: 0, expectedErrorCount: 0,
                additionalFlags: new[] { "/warnAsError" },
                includeCurrentAssemblyAsAnalyzerReference: false,
                errorlog: true,
                analyzers: new[] { suppressor });
            Assert.DoesNotContain($"error CS0078", output, StringComparison.Ordinal);
            Assert.DoesNotContain($"warning CS0078", output, StringComparison.Ordinal);

            // Diagnostic '{0}: {1}' was programmatically suppressed by a DiagnosticSuppressor with suppression ID '{2}' and justification '{3}'
            var suppressionMessage = string.Format(CodeAnalysisResources.SuppressionDiagnosticDescriptorMessage,
                suppressor.SuppressionDescriptor.SuppressedDiagnosticId,
                new CSDiagnostic(new CSDiagnosticInfo(ErrorCode.WRN_LowercaseEllSuffix), Location.None).GetMessage(CultureInfo.InvariantCulture),
                suppressor.SuppressionDescriptor.Id,
                suppressor.SuppressionDescriptor.Justification);
            Assert.Contains("info SP0001", output, StringComparison.Ordinal);
            Assert.Contains(suppressionMessage, output, StringComparison.Ordinal);

            CleanupAllGeneratedFiles(srcFile.Path);
        }

        [WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")]
        [ConditionalTheory(typeof(IsEnglishLocal)), CombinatorialData]
        public void TestSuppression_CompilerSyntaxWarning(bool skipAnalyzers)
        {
            // warning CS1522: Empty switch block
            // NOTE: Empty switch block warning is reported by the C# language parser
            string source = @"
class C
{
    void M(int i)
    {
        switch (i)
        {
        }
    }
}";
            var srcDirectory = Temp.CreateDirectory();
            var srcFile = srcDirectory.CreateFile("a.cs");
            srcFile.WriteAllText(source);

            // Verify that compiler warning CS1522 is reported.
            var output = VerifyOutput(srcDirectory, srcFile, expectedWarningCount: 1, includeCurrentAssemblyAsAnalyzerReference: false, skipAnalyzers: skipAnalyzers);
            Assert.Contains("warning CS1522", output, StringComparison.Ordinal);

            // Verify that compiler warning CS1522 is suppressed with diagnostic suppressor
            // and info diagnostic is logged with programmatic suppression information.
            var suppressor = new DiagnosticSuppressorForId("CS1522");

            // Diagnostic '{0}: {1}' was programmatically suppressed by a DiagnosticSuppressor with suppression ID '{2}' and justification '{3}'
            var suppressionMessage = string.Format(CodeAnalysisResources.SuppressionDiagnosticDescriptorMessage,
                suppressor.SuppressionDescriptor.SuppressedDiagnosticId,
                new CSDiagnostic(new CSDiagnosticInfo(ErrorCode.WRN_EmptySwitch), Location.None).GetMessage(CultureInfo.InvariantCulture),
                suppressor.SuppressionDescriptor.Id,
                suppressor.SuppressionDescriptor.Justification);

            output = VerifyOutput(srcDirectory, srcFile, expectedInfoCount: 1, expectedWarningCount: 0, includeCurrentAssemblyAsAnalyzerReference: false,
                analyzers: new[] { suppressor }, errorlog: true, skipAnalyzers: skipAnalyzers);
            Assert.DoesNotContain($"warning CS1522", output, StringComparison.Ordinal);
            Assert.Contains($"info SP0001", output, StringComparison.Ordinal);
            Assert.Contains(suppressionMessage, output, StringComparison.Ordinal);

            // Verify that compiler warning CS1522 is reported as error for /warnaserror.
            output = VerifyOutput(srcDirectory, srcFile, expectedErrorCount: 1,
                additionalFlags: new[] { "/warnAsError" }, includeCurrentAssemblyAsAnalyzerReference: false, skipAnalyzers: skipAnalyzers);
            Assert.Contains("error CS1522", output, StringComparison.Ordinal);

            // Verify that compiler warning CS1522 is suppressed with diagnostic suppressor even with /warnaserror
            // and info diagnostic is logged with programmatic suppression information.
            output = VerifyOutput(srcDirectory, srcFile, expectedInfoCount: 1, expectedWarningCount: 0, expectedErrorCount: 0,
                additionalFlags: new[] { "/warnAsError" },
                includeCurrentAssemblyAsAnalyzerReference: false,
                errorlog: true,
                skipAnalyzers: skipAnalyzers,
                analyzers: new[] { suppressor });
            Assert.DoesNotContain($"error CS1522", output, StringComparison.Ordinal);
            Assert.DoesNotContain($"warning CS1522", output, StringComparison.Ordinal);
            Assert.Contains("info SP0001", output, StringComparison.Ordinal);
            Assert.Contains(suppressionMessage, output, StringComparison.Ordinal);

            CleanupAllGeneratedFiles(srcFile.Path);
        }

        [WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")]
        [ConditionalTheory(typeof(IsEnglishLocal)), CombinatorialData]
        public void TestSuppression_CompilerSemanticWarning(bool skipAnalyzers)
        {
            string source = @"
class C
{
    // warning CS0169: The field 'C.f' is never used
    private readonly int f;
}";
            var srcDirectory = Temp.CreateDirectory();
            var srcFile = srcDirectory.CreateFile("a.cs");
            srcFile.WriteAllText(source);

            // Verify that compiler warning CS0169 is reported.
            var output = VerifyOutput(srcDirectory, srcFile, expectedWarningCount: 1, includeCurrentAssemblyAsAnalyzerReference: false, skipAnalyzers: skipAnalyzers);
            Assert.Contains("warning CS0169", output, StringComparison.Ordinal);

            // Verify that compiler warning CS0169 is suppressed with diagnostic suppressor
            // and info diagnostic is logged with programmatic suppression information.
            var suppressor = new DiagnosticSuppressorForId("CS0169");

            // Diagnostic '{0}: {1}' was programmatically suppressed by a DiagnosticSuppressor with suppression ID '{2}' and justification '{3}'
            var suppressionMessage = string.Format(CodeAnalysisResources.SuppressionDiagnosticDescriptorMessage,
                suppressor.SuppressionDescriptor.SuppressedDiagnosticId,
                new CSDiagnostic(new CSDiagnosticInfo(ErrorCode.WRN_UnreferencedField, "C.f"), Location.None).GetMessage(CultureInfo.InvariantCulture),
                suppressor.SuppressionDescriptor.Id,
                suppressor.SuppressionDescriptor.Justification);

            output = VerifyOutput(srcDirectory, srcFile, expectedInfoCount: 1, expectedWarningCount: 0, includeCurrentAssemblyAsAnalyzerReference: false,
                analyzers: new[] { suppressor }, errorlog: true, skipAnalyzers: skipAnalyzers);
            Assert.DoesNotContain($"warning CS0169", output, StringComparison.Ordinal);
            Assert.Contains("info SP0001", output, StringComparison.Ordinal);
            Assert.Contains(suppressionMessage, output, StringComparison.Ordinal);

            // Verify that compiler warning CS0169 is reported as error for /warnaserror.
            output = VerifyOutput(srcDirectory, srcFile, expectedErrorCount: 1,
                additionalFlags: new[] { "/warnAsError" }, includeCurrentAssemblyAsAnalyzerReference: false, skipAnalyzers: skipAnalyzers);
            Assert.Contains("error CS0169", output, StringComparison.Ordinal);

            // Verify that compiler warning CS0169 is suppressed with diagnostic suppressor even with /warnaserror
            // and info diagnostic is logged with programmatic suppression information.
            output = VerifyOutput(srcDirectory, srcFile, expectedInfoCount: 1, expectedWarningCount: 0, expectedErrorCount: 0,
                additionalFlags: new[] { "/warnAsError" },
                includeCurrentAssemblyAsAnalyzerReference: false,
                errorlog: true,
                skipAnalyzers: skipAnalyzers,
                analyzers: new[] { suppressor });
            Assert.DoesNotContain($"error CS0169", output, StringComparison.Ordinal);
            Assert.DoesNotContain($"warning CS0169", output, StringComparison.Ordinal);
            Assert.Contains("info SP0001", output, StringComparison.Ordinal);
            Assert.Contains(suppressionMessage, output, StringComparison.Ordinal);

            CleanupAllGeneratedFiles(srcFile.Path);
        }

        [WorkItem(62540, "https://github.com/dotnet/roslyn/issues/62540")]
        [ConditionalTheory(typeof(IsEnglishLocal)), CombinatorialData]
        public void TestSuppression_CompilerSyntaxParseError_SuppressWarningCaughtDuringParsingStage(bool skipAnalyzers)
        {
            const string SourceCode = @"
                using System;

                class X
                {
                    public bool Select<T>(Func<int, T> selector) => true;
                    public static int operator +(Action a, X right) => 0;
                }

                public class PrecedenceInversionClass
                {
                    void M1()
                    {
                        var src = new X();
                        var b = false && from x in src select x; // Parsing warning -- CS8848: Operator 'from' cannot be used here due to precedence
                    }
                }

                public class {} // Parsing error -- CS1001: Identifier expected";

            var sourceDirectory = Temp.CreateDirectory();
            var sourceFile = sourceDirectory.CreateFile("BuggyCode.cs");
            sourceFile.WriteAllText(SourceCode);

            // During the parsing stage, both CS8848 (a warning) and CS1001 (an unsuppressible error) will be detected.
            // This test verifies that CS8848 is correctly suppressed, and that CS1001 is correctly reported.
            var precedenceInversionWarningSuppressor = new DiagnosticSuppressorForId("CS8848");

            // Diagnostic '{0}: {1}' was programmatically suppressed by a DiagnosticSuppressor with suppression ID '{2}' and justification '{3}'
            var suppressionMessage =
                string.Format(
                    CodeAnalysisResources.SuppressionDiagnosticDescriptorMessage,
                    precedenceInversionWarningSuppressor.SuppressionDescriptor.SuppressedDiagnosticId,
                    new CSDiagnostic(new CSDiagnosticInfo(ErrorCode.WRN_PrecedenceInversion, "from"), Location.None).GetMessage(CultureInfo.InvariantCulture),
                    precedenceInversionWarningSuppressor.SuppressionDescriptor.Id,
                    precedenceInversionWarningSuppressor.SuppressionDescriptor.Justification);

            // CS8848 is automatically suppressed if the warning level is <5.
            // Set the warning level to 5 to ensure that it will not get automatically suppressed, and leave it up to the `precedenceInversionWarningSuppressor` to suppress it.
            var output =
                VerifyOutput(
                    sourceDirectory,
                    sourceFile,
                    additionalFlags: new[] { "/warn:5" },
                    expectedErrorCount: 1,
                    expectedInfoCount: 1,
                    expectedWarningCount: 0,
                    includeCurrentAssemblyAsAnalyzerReference: false,
                    skipAnalyzers: skipAnalyzers,
                    analyzers: new[] { precedenceInversionWarningSuppressor },
                    errorlog: true);

            Assert.DoesNotContain("warning CS8848", output, StringComparison.Ordinal);

            Assert.Contains(suppressionMessage, output, StringComparison.Ordinal);
            Assert.Contains("info SP0001", output, StringComparison.Ordinal);
            Assert.Contains("error CS1001", output, StringComparison.Ordinal);

            // During the parsing stage, both CS8848 (a warning) and CS1001 (an unsuppressible error) will be detected.
            // This test verifies that CS8848 is correctly suppressed even when elevated as an error (using `warnaserror`), and that CS1001 is correctly reported.
            output =
                VerifyOutput(
                    sourceDirectory,
                    sourceFile,
                    expectedErrorCount: 1,
                    expectedInfoCount: 1,
                    expectedWarningCount: 0,
                    additionalFlags: new[] { "/warn:5", "/warnaserror" },
                    includeCurrentAssemblyAsAnalyzerReference: false,
                    skipAnalyzers: skipAnalyzers,
                    errorlog: true,
                    analyzers: new[] { precedenceInversionWarningSuppressor });

            Assert.DoesNotContain($"error CS8848", output, StringComparison.Ordinal);
            Assert.DoesNotContain($"warning CS8848", output, StringComparison.Ordinal);

            Assert.Contains(suppressionMessage, output, StringComparison.Ordinal);
            Assert.Contains("info SP0001", output, StringComparison.Ordinal);
            Assert.Contains("error CS1001", output, StringComparison.Ordinal);

            CleanupAllGeneratedFiles(sourceFile.Path);
        }

        [WorkItem(62540, "https://github.com/dotnet/roslyn/issues/62540")]
        [ConditionalTheory(typeof(IsEnglishLocal)), CombinatorialData]
        public void TestSuppression_CompilerSyntaxDeclarationError_SuppressWarningTriggeredByGenerator(bool skipAnalyzers)
        {
            const string SourceCode = @"
                partial struct MyPartialStruct
                {
                    public int MyInt;

                    public void SetMyInt(int value)
                    {
                        MyInt = value;
                    }
                }

                public abstract class MyAbstractClass
                {
                    // error CS0180: Methods cannot be both extern and abstract -- this is a declaration error
                    public extern abstract void MyFaultyMethod()
                    {
                    }
                }";
            var sourceDirectory = Temp.CreateDirectory();
            var sourceFile = sourceDirectory.CreateFile("NotGenerated.cs");
            sourceFile.WriteAllText(SourceCode);

            const string GeneratedCode =
                @"// warning CS0282: Partial struct warning
                partial struct MyPartialStruct
                {
                    public bool MyBoolean;

                    public void SetMyBoolean(bool value)
                    {
                        MyBoolean = value;
                    }
                }";
            var partialStructGenerator = new SingleFileTestGenerator(GeneratedCode, "Generated.cs");

            // The generated code will trigger `CS0282`. This test verifies 3 things:
            // 1. Compiler warning `CS0282` is suppressed with diagnostic suppressor,
            // 2. Info diagnostic for the suppression is logged with programmatic suppression information,
            // 3. Compiler error `CS0180` is reported.
            var partialStructWarningSuppressor = new DiagnosticSuppressorForId("CS0282");

            // Diagnostic '{0}: {1}' was programmatically suppressed by a DiagnosticSuppressor with suppression ID '{2}' and justification '{3}'
            var suppressionMessage =
                string.Format(
                    CodeAnalysisResources.SuppressionDiagnosticDescriptorMessage,
                    partialStructWarningSuppressor.SuppressionDescriptor.SuppressedDiagnosticId,
                    new CSDiagnostic(new CSDiagnosticInfo(ErrorCode.WRN_SequentialOnPartialClass, "MyPartialStruct"), Location.None).GetMessage(CultureInfo.InvariantCulture),
                    partialStructWarningSuppressor.SuppressionDescriptor.Id,
                    partialStructWarningSuppressor.SuppressionDescriptor.Justification);

            var output =
                VerifyOutput(
                    sourceDirectory,
                    sourceFile,
                    expectedErrorCount: 1,
                    expectedInfoCount: 1,
                    expectedWarningCount: 0,
                    includeCurrentAssemblyAsAnalyzerReference: false,
                    skipAnalyzers: skipAnalyzers,
                    generators: new[] { partialStructGenerator },
                    analyzers: new[] { partialStructWarningSuppressor },
                    errorlog: true);

            Assert.DoesNotContain("warning CS0282", output, StringComparison.Ordinal);

            Assert.Contains(suppressionMessage, output, StringComparison.Ordinal);
            Assert.Contains("info SP0001", output, StringComparison.Ordinal);
            Assert.Contains("error CS0180", output, StringComparison.Ordinal);

            // The generated code will trigger `CS0282`. This test verifies 3 things:
            // 1. Compiler warning `CS0282` is suppressed with diagnostic suppressor even when elevated as an error (using `/warnaserror`),
            // 2. Info diagnostic for the suppression is logged with programmatic suppression information,
            // 3. Compiler error `CS0180` is reported.
            output =
                VerifyOutput(
                    sourceDirectory,
                    sourceFile,
                    expectedErrorCount: 1,
                    expectedInfoCount: 1,
                    expectedWarningCount: 0,
                    additionalFlags: new[] { "/warnaserror" },
                    includeCurrentAssemblyAsAnalyzerReference: false,
                    skipAnalyzers: skipAnalyzers,
                    generators: new[] { partialStructGenerator },
                    errorlog: true,
                    analyzers: new[] { partialStructWarningSuppressor });

            Assert.DoesNotContain($"error CS0282", output, StringComparison.Ordinal);
            Assert.DoesNotContain($"warning CS0282", output, StringComparison.Ordinal);

            Assert.Contains(suppressionMessage, output, StringComparison.Ordinal);
            Assert.Contains("info SP0001", output, StringComparison.Ordinal);
            Assert.Contains("error CS0180", output, StringComparison.Ordinal);

            CleanupAllGeneratedFiles(sourceFile.Path);
        }

        [WorkItem(62540, "https://github.com/dotnet/roslyn/issues/62540")]
        [ConditionalTheory(typeof(IsEnglishLocal)), CombinatorialData]
        public void TestSuppression_CompilerSyntaxBindingError_SuppressWarningTriggeredByGenerator(bool skipAnalyzers)
        {
            const string SourceCode = @"
                // warning CS0282: Partial struct warning
                partial struct MyPartialStruct
                {
                    public int MyInt;

                    public void SetMyInt(int value)
                    {
                        MyInt = value;
                    }
                }

                public class MyClass
                {
                    void MyPrivateMethod()
                    {
                    }
                }
                public class YourClass
                { 
                    void YourPrivateMethod()
                    {
                        // Cannot access private method
                        new MyClass().MyPrivateMethod();
                    }
                }";

            var sourceDir = Temp.CreateDirectory();
            var sourceFile = sourceDir.CreateFile("NotGenerated.cs");
            sourceFile.WriteAllText(SourceCode);

            const string GeneratedSource =
                @"// warning CS0282: Partial struct warning
                partial struct MyPartialStruct
                {
                    public bool MyBoolean;

                    public void SetMyBoolean(bool value)
                    {
                        MyBoolean = value;
                    }
                }";
            var partialStructGenerator = new SingleFileTestGenerator(GeneratedSource, "Generated.cs");

            // The generated code will trigger `CS0282`. This test verifies 3 things:
            // 1. Compiler warning `CS0282` is suppressed with diagnostic suppressor,
            // 2. Info diagnostic for the suppression is logged with programmatic suppression information,
            // 3. Compiler error `CS1001` is reported.
            var partialStructWarningSuppressor = new DiagnosticSuppressorForId("CS0282");

            // Diagnostic '{0}: {1}' was programmatically suppressed by a DiagnosticSuppressor with suppression ID '{2}' and justification '{3}'
            var suppressionMessage =
                string.Format(
                    CodeAnalysisResources.SuppressionDiagnosticDescriptorMessage,
                    partialStructWarningSuppressor.SuppressionDescriptor.SuppressedDiagnosticId,
                    new CSDiagnostic(new CSDiagnosticInfo(ErrorCode.WRN_SequentialOnPartialClass, "MyPartialStruct"), Location.None).GetMessage(CultureInfo.InvariantCulture),
                    partialStructWarningSuppressor.SuppressionDescriptor.Id,
                    partialStructWarningSuppressor.SuppressionDescriptor.Justification);

            var output =
                VerifyOutput(
                    sourceDir,
                    sourceFile,
                    expectedErrorCount: 1,
                    expectedInfoCount: 1,
                    expectedWarningCount: 0,
                    includeCurrentAssemblyAsAnalyzerReference: false,
                    skipAnalyzers: skipAnalyzers,
                    generators: new[] { partialStructGenerator },
                    analyzers: new[] { partialStructWarningSuppressor },
                    errorlog: true);

            Assert.DoesNotContain("warning CS0282", output, StringComparison.Ordinal);

            Assert.Contains(suppressionMessage, output, StringComparison.Ordinal);
            Assert.Contains("info SP0001", output, StringComparison.Ordinal);
            Assert.Contains("error CS0122", output, StringComparison.Ordinal);

            // The generated code will trigger `CS0282`. This test verifies 3 things:
            // 1. Compiler warning `CS0282` is suppressed with diagnostic suppressor even when elevated as an error (using `/warnaserror`),
            // 2. Info diagnostic for the suppression is logged with programmatic suppression information,
            // 3. Compiler error `CS1001` is reported.
            output =
                VerifyOutput(
                    sourceDir,
                    sourceFile,
                    expectedErrorCount: 1,
                    expectedInfoCount: 1,
                    expectedWarningCount: 0,
                    additionalFlags: new[] { "/warnaserror" },
                    includeCurrentAssemblyAsAnalyzerReference: false,
                    skipAnalyzers: skipAnalyzers,
                    errorlog: true,
                    generators: new[] { partialStructGenerator },
                    analyzers: new[] { partialStructWarningSuppressor });

            Assert.DoesNotContain($"error CS0282", output, StringComparison.Ordinal);
            Assert.DoesNotContain($"warning CS0282", output, StringComparison.Ordinal);

            Assert.Contains(suppressionMessage, output, StringComparison.Ordinal);
            Assert.Contains("info SP0001", output, StringComparison.Ordinal);
            Assert.Contains("error CS0122", output, StringComparison.Ordinal);

            CleanupAllGeneratedFiles(sourceFile.Path);
        }

        [WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")]
        [Fact]
        public void TestNoSuppression_CompilerSyntaxError()
        {
            // error CS1001: Identifier expected
            string source = @"
class { }";
            var srcDirectory = Temp.CreateDirectory();
            var srcFile = srcDirectory.CreateFile("a.cs");
            srcFile.WriteAllText(source);

            // Verify that compiler syntax error CS1001 is reported.
            var output = VerifyOutput(srcDirectory, srcFile, expectedErrorCount: 1, includeCurrentAssemblyAsAnalyzerReference: false);
            Assert.Contains("error CS1001", output, StringComparison.Ordinal);

            // Verify that compiler syntax error CS1001 cannot be suppressed with diagnostic suppressor.
            output = VerifyOutput(srcDirectory, srcFile, expectedErrorCount: 1, includeCurrentAssemblyAsAnalyzerReference: false,
                analyzers: new[] { new DiagnosticSuppressorForId("CS1001") });
            Assert.Contains("error CS1001", output, StringComparison.Ordinal);

            CleanupAllGeneratedFiles(srcFile.Path);
        }

        [WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")]
        [Fact]
        public void TestNoSuppression_CompilerSemanticError()
        {
            // error CS0246: The type or namespace name 'UndefinedType' could not be found (are you missing a using directive or an assembly reference?)
            string source = @"
class C
{
    void M(UndefinedType x) { }
}";
            var srcDirectory = Temp.CreateDirectory();
            var srcFile = srcDirectory.CreateFile("a.cs");
            srcFile.WriteAllText(source);

            // Verify that compiler error CS0246 is reported.
            var output = VerifyOutput(srcDirectory, srcFile, expectedErrorCount: 1, includeCurrentAssemblyAsAnalyzerReference: false);
            Assert.Contains("error CS0246", output, StringComparison.Ordinal);

            // Verify that compiler error CS0246 cannot be suppressed with diagnostic suppressor.
            output = VerifyOutput(srcDirectory, srcFile, expectedErrorCount: 1, includeCurrentAssemblyAsAnalyzerReference: false,
                analyzers: new[] { new DiagnosticSuppressorForId("CS0246") });
            Assert.Contains("error CS0246", output, StringComparison.Ordinal);

            CleanupAllGeneratedFiles(srcFile.Path);
        }

        [WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")]
        [ConditionalFact(typeof(IsEnglishLocal))]
        public void TestSuppression_AnalyzerWarning()
        {
            string source = @"
class C { }";
            var srcDirectory = Temp.CreateDirectory();
            var srcFile = srcDirectory.CreateFile("a.cs");
            srcFile.WriteAllText(source);

            // Verify that analyzer warning is reported.
            var analyzer = new CompilationAnalyzerWithSeverity(DiagnosticSeverity.Warning, configurable: true);
            var output = VerifyOutput(srcDirectory, srcFile, expectedWarningCount: 1,
                includeCurrentAssemblyAsAnalyzerReference: false,
                analyzers: new[] { analyzer });
            Assert.Contains($"warning {analyzer.Descriptor.Id}", output, StringComparison.Ordinal);

            // Verify that analyzer warning is suppressed with diagnostic suppressor
            // and info diagnostic is logged with programmatic suppression information.
            var suppressor = new DiagnosticSuppressorForId(analyzer.Descriptor.Id);

            // Diagnostic '{0}: {1}' was programmatically suppressed by a DiagnosticSuppressor with suppression ID '{2}' and justification '{3}'
            var suppressionMessage = string.Format(CodeAnalysisResources.SuppressionDiagnosticDescriptorMessage,
                suppressor.SuppressionDescriptor.SuppressedDiagnosticId,
                analyzer.Descriptor.MessageFormat,
                suppressor.SuppressionDescriptor.Id,
                suppressor.SuppressionDescriptor.Justification);

            var analyzerAndSuppressor = new DiagnosticAnalyzer[] { analyzer, suppressor };
            output = VerifyOutput(srcDirectory, srcFile, expectedInfoCount: 1, expectedWarningCount: 0,
                includeCurrentAssemblyAsAnalyzerReference: false,
                errorlog: true,
                analyzers: analyzerAndSuppressor);
            Assert.DoesNotContain($"warning {analyzer.Descriptor.Id}", output, StringComparison.Ordinal);
            Assert.Contains("info SP0001", output, StringComparison.Ordinal);
            Assert.Contains(suppressionMessage, output, StringComparison.Ordinal);

            // Verify that analyzer warning is reported as error for /warnaserror.
            output = VerifyOutput(srcDirectory, srcFile, expectedErrorCount: 1,
                additionalFlags: new[] { "/warnAsError" },
                includeCurrentAssemblyAsAnalyzerReference: false,
                analyzers: new[] { analyzer });
            Assert.Contains($"error {analyzer.Descriptor.Id}", output, StringComparison.Ordinal);

            // Verify that analyzer warning is suppressed with diagnostic suppressor even with /warnaserror
            // and info diagnostic is logged with programmatic suppression information.
            output = VerifyOutput(srcDirectory, srcFile, expectedInfoCount: 1, expectedWarningCount: 0,
                additionalFlags: new[] { "/warnAsError" },
                includeCurrentAssemblyAsAnalyzerReference: false,
                errorlog: true,
                analyzers: analyzerAndSuppressor);
            Assert.DoesNotContain($"warning {analyzer.Descriptor.Id}", output, StringComparison.Ordinal);
            Assert.Contains("info SP0001", output, StringComparison.Ordinal);
            Assert.Contains(suppressionMessage, output, StringComparison.Ordinal);

            // Verify that "NotConfigurable" analyzer warning cannot be suppressed with diagnostic suppressor.
            analyzer = new CompilationAnalyzerWithSeverity(DiagnosticSeverity.Warning, configurable: false);
            suppressor = new DiagnosticSuppressorForId(analyzer.Descriptor.Id);
            analyzerAndSuppressor = new DiagnosticAnalyzer[] { analyzer, suppressor };
            output = VerifyOutput(srcDirectory, srcFile, expectedWarningCount: 1,
                includeCurrentAssemblyAsAnalyzerReference: false,
                analyzers: analyzerAndSuppressor);
            Assert.Contains($"warning {analyzer.Descriptor.Id}", output, StringComparison.Ordinal);

            CleanupAllGeneratedFiles(srcFile.Path);
        }

        [WorkItem(20242, "https://github.com/dotnet/roslyn/issues/20242")]
        [Fact]
        public void TestNoSuppression_AnalyzerError()
        {
            string source = @"
class C { }";
            var srcDirectory = Temp.CreateDirectory();
            var srcFile = srcDirectory.CreateFile("a.cs");
            srcFile.WriteAllText(source);

            // Verify that analyzer error is reported.
            var analyzer = new CompilationAnalyzerWithSeverity(DiagnosticSeverity.Error, configurable: true);
            var output = VerifyOutput(srcDirectory, srcFile, expectedErrorCount: 1,
                includeCurrentAssemblyAsAnalyzerReference: false,
                analyzers: new[] { analyzer });
            Assert.Contains($"error {analyzer.Descriptor.Id}", output, StringComparison.Ordinal);

            // Verify that analyzer error cannot be suppressed with diagnostic suppressor.
            var suppressor = new DiagnosticSuppressorForId(analyzer.Descriptor.Id);
            var analyzerAndSuppressor = new DiagnosticAnalyzer[] { analyzer, suppressor };
            output = VerifyOutput(srcDirectory, srcFile, expectedErrorCount: 1,
                includeCurrentAssemblyAsAnalyzerReference: false,
                analyzers: analyzerAndSuppressor);
            Assert.Contains($"error {analyzer.Descriptor.Id}", output, StringComparison.Ordinal);

            CleanupAllGeneratedFiles(srcFile.Path);
        }

        [WorkItem(38674, "https://github.com/dotnet/roslyn/issues/38674")]
        [InlineData(DiagnosticSeverity.Warning, false)]
        [InlineData(DiagnosticSeverity.Info, true)]
        [InlineData(DiagnosticSeverity.Info, false)]
        [InlineData(DiagnosticSeverity.Hidden, false)]
        [Theory]
        public void TestCategoryBasedBulkAnalyzerDiagnosticConfiguration(DiagnosticSeverity defaultSeverity, bool errorlog)
        {
            var analyzer = new NamedTypeAnalyzerWithConfigurableEnabledByDefault(isEnabledByDefault: true, defaultSeverity);

            var diagnosticId = analyzer.Descriptor.Id;
            var category = analyzer.Descriptor.Category;

            // Verify category based configuration without any diagnostic ID configuration is respected.
            var analyzerConfigText = $@"
[*.cs]
dotnet_analyzer_diagnostic.category-{category}.severity = error";
            TestBulkAnalyzerConfigurationCore(analyzer, analyzerConfigText, errorlog, expectedDiagnosticSeverity: ReportDiagnostic.Error);

            // Verify category based configuration does not get applied for suppressed diagnostic.
            TestBulkAnalyzerConfigurationCore(analyzer, analyzerConfigText, errorlog, expectedDiagnosticSeverity: ReportDiagnostic.Suppress, noWarn: true);

            // Verify category based configuration does not get applied for diagnostic configured in ruleset.
            var rulesetText = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <Rules AnalyzerId=""Microsoft.CodeAnalysis"" RuleNamespace=""Microsoft.CodeAnalysis"">
    <Rule Id=""{diagnosticId}"" Action=""Warning"" />
  </Rules>
</RuleSet>";
            TestBulkAnalyzerConfigurationCore(analyzer, analyzerConfigText, errorlog, expectedDiagnosticSeverity: ReportDiagnostic.Warn, rulesetText: rulesetText);

            // Verify category based configuration before diagnostic ID configuration is not respected.
            analyzerConfigText = $@"
[*.cs]
dotnet_analyzer_diagnostic.category-{category}.severity = error
dotnet_diagnostic.{diagnosticId}.severity = warning";
            TestBulkAnalyzerConfigurationCore(analyzer, analyzerConfigText, errorlog, expectedDiagnosticSeverity: ReportDiagnostic.Warn);

            // Verify category based configuration after diagnostic ID configuration is not respected.
            analyzerConfigText = $@"
[*.cs]
dotnet_diagnostic.{diagnosticId}.severity = warning
dotnet_analyzer_diagnostic.category-{category}.severity = error";
            TestBulkAnalyzerConfigurationCore(analyzer, analyzerConfigText, errorlog, expectedDiagnosticSeverity: ReportDiagnostic.Warn);

            // Verify global config based configuration before diagnostic ID configuration is not respected.
            analyzerConfigText = $@"
is_global = true
dotnet_analyzer_diagnostic.category-{category}.severity = error
dotnet_diagnostic.{diagnosticId}.severity = warning";
            TestBulkAnalyzerConfigurationCore(analyzer, analyzerConfigText, errorlog, expectedDiagnosticSeverity: ReportDiagnostic.Warn);

            // Verify global config based configuration after diagnostic ID configuration is not respected.
            analyzerConfigText = $@"
is_global = true
dotnet_diagnostic.{diagnosticId}.severity = warning
dotnet_analyzer_diagnostic.category-{category}.severity = error";
            TestBulkAnalyzerConfigurationCore(analyzer, analyzerConfigText, errorlog, expectedDiagnosticSeverity: ReportDiagnostic.Warn);

            // Verify category based configuration to warning + /warnaserror reports errors.
            analyzerConfigText = $@"
[*.cs]
dotnet_analyzer_diagnostic.category-{category}.severity = warning";
            TestBulkAnalyzerConfigurationCore(analyzer, analyzerConfigText, errorlog, warnAsError: true, expectedDiagnosticSeverity: ReportDiagnostic.Error);

            // Verify disabled by default analyzer is not enabled by category based configuration.
            analyzer = new NamedTypeAnalyzerWithConfigurableEnabledByDefault(isEnabledByDefault: false, defaultSeverity);
            analyzerConfigText = $@"
[*.cs]
dotnet_analyzer_diagnostic.category-{category}.severity = error";
            TestBulkAnalyzerConfigurationCore(analyzer, analyzerConfigText, errorlog, expectedDiagnosticSeverity: ReportDiagnostic.Suppress);

            // Verify disabled by default analyzer is not enabled by category based configuration in global config
            analyzer = new NamedTypeAnalyzerWithConfigurableEnabledByDefault(isEnabledByDefault: false, defaultSeverity);
            analyzerConfigText = $@"
is_global=true
dotnet_analyzer_diagnostic.category-{category}.severity = error";
            TestBulkAnalyzerConfigurationCore(analyzer, analyzerConfigText, errorlog, expectedDiagnosticSeverity: ReportDiagnostic.Suppress);

            if (defaultSeverity == DiagnosticSeverity.Hidden ||
                defaultSeverity == DiagnosticSeverity.Info && !errorlog)
            {
                // Verify analyzer with Hidden severity OR Info severity + no /errorlog is not executed.
                analyzer = new NamedTypeAnalyzerWithConfigurableEnabledByDefault(isEnabledByDefault: true, defaultSeverity, throwOnAllNamedTypes: true);
                TestBulkAnalyzerConfigurationCore(analyzer, analyzerConfigText: string.Empty, errorlog, expectedDiagnosticSeverity: ReportDiagnostic.Suppress);

                // Verify that bulk configuration 'none' entry does not enable this analyzer.
                analyzerConfigText = $@"
[*.cs]
dotnet_analyzer_diagnostic.category-{category}.severity = none";
                TestBulkAnalyzerConfigurationCore(analyzer, analyzerConfigText, errorlog, expectedDiagnosticSeverity: ReportDiagnostic.Suppress);

                // Verify that bulk configuration 'none' entry does not enable this analyzer via global config
                analyzerConfigText = $@"
[*.cs]
dotnet_analyzer_diagnostic.category-{category}.severity = none";
                TestBulkAnalyzerConfigurationCore(analyzer, analyzerConfigText, errorlog, expectedDiagnosticSeverity: ReportDiagnostic.Suppress);
            }
        }

        [WorkItem(38674, "https://github.com/dotnet/roslyn/issues/38674")]
        [InlineData(DiagnosticSeverity.Warning, false, false)]
        [InlineData(DiagnosticSeverity.Warning, false, true)]
        [InlineData(DiagnosticSeverity.Info, true, false)]
        [InlineData(DiagnosticSeverity.Info, false, false)]
        [InlineData(DiagnosticSeverity.Info, true, true)]
        [InlineData(DiagnosticSeverity.Info, false, true)]
        [InlineData(DiagnosticSeverity.Hidden, false, false)]
        [InlineData(DiagnosticSeverity.Hidden, false, true)]
        [Theory]
        public void TestBulkAnalyzerDiagnosticConfiguration(DiagnosticSeverity defaultSeverity, bool errorlog, bool customConfigurable)
        {
            var analyzer = new NamedTypeAnalyzerWithConfigurableEnabledByDefault(isEnabledByDefault: true, defaultSeverity, customConfigurable, throwOnAllNamedTypes: false);

            var diagnosticId = analyzer.Descriptor.Id;

            // Verify bulk configuration without any diagnostic ID configuration is respected,
            // unless analyzer reports 'CustomConfigurable' diagnostics, which explicitly disables bulk configuration.
            var defaultReportDiagnostic = DiagnosticDescriptor.MapSeverityToReport(defaultSeverity);
            var expectedDiagnosticSeverity = customConfigurable ? defaultReportDiagnostic : ReportDiagnostic.Error;
            var analyzerConfigText = $@"
[*.cs]
dotnet_analyzer_diagnostic.severity = error";
            TestBulkAnalyzerConfigurationCore(analyzer, analyzerConfigText, errorlog, expectedDiagnosticSeverity);

            // Verify bulk configuration does not get applied for suppressed diagnostic.
            TestBulkAnalyzerConfigurationCore(analyzer, analyzerConfigText, errorlog, expectedDiagnosticSeverity: ReportDiagnostic.Suppress, noWarn: true);

            // Verify bulk configuration does not get applied for diagnostic configured in ruleset.
            var rulesetText = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <Rules AnalyzerId=""Microsoft.CodeAnalysis"" RuleNamespace=""Microsoft.CodeAnalysis"">
    <Rule Id=""{diagnosticId}"" Action=""Warning"" />
  </Rules>
</RuleSet>";
            TestBulkAnalyzerConfigurationCore(analyzer, analyzerConfigText, errorlog, expectedDiagnosticSeverity: ReportDiagnostic.Warn, rulesetText: rulesetText);

            // Verify bulk configuration before diagnostic ID configuration is not respected.
            // If the analyzer reports 'CustomConfigurable' diagnostics, all editorconfig configurations are ignored.
            expectedDiagnosticSeverity = customConfigurable ? defaultReportDiagnostic : ReportDiagnostic.Warn;
            analyzerConfigText = $@"
[*.cs]
dotnet_analyzer_diagnostic.severity = error
dotnet_diagnostic.{diagnosticId}.severity = warning";
            TestBulkAnalyzerConfigurationCore(analyzer, analyzerConfigText, errorlog, expectedDiagnosticSeverity);

            // Verify bulk configuration after diagnostic ID configuration is not respected.
            // If the analyzer reports 'CustomConfigurable' diagnostics, all editorconfig configurations are ignored.
            analyzerConfigText = $@"
[*.cs]
dotnet_diagnostic.{diagnosticId}.severity = warning
dotnet_analyzer_diagnostic.severity = error";
            TestBulkAnalyzerConfigurationCore(analyzer, analyzerConfigText, errorlog, expectedDiagnosticSeverity);

            // Verify bulk configuration to warning + /warnaserror reports errors.
            // If the analyzer reports 'CustomConfigurable' diagnostics, all editorconfig configurations are ignored.
            expectedDiagnosticSeverity = customConfigurable && defaultReportDiagnostic != ReportDiagnostic.Warn ? defaultReportDiagnostic : ReportDiagnostic.Error;
            analyzerConfigText = $@"
[*.cs]
dotnet_analyzer_diagnostic.severity = warning";
            TestBulkAnalyzerConfigurationCore(analyzer, analyzerConfigText, errorlog, expectedDiagnosticSeverity, warnAsError: true);

            // Verify disabled by default analyzer is not enabled by bulk configuration.
            // However, analyzer reporting 'CustomConfigurable' diagnostics is considered to be enabled.
            analyzer = new NamedTypeAnalyzerWithConfigurableEnabledByDefault(isEnabledByDefault: false, defaultSeverity, customConfigurable, throwOnAllNamedTypes: false);
            expectedDiagnosticSeverity = customConfigurable ? defaultReportDiagnostic : ReportDiagnostic.Suppress;
            analyzerConfigText = $@"
[*.cs]
dotnet_analyzer_diagnostic.severity = error";
            TestBulkAnalyzerConfigurationCore(analyzer, analyzerConfigText, errorlog, expectedDiagnosticSeverity);

            if (defaultSeverity == DiagnosticSeverity.Hidden ||
                defaultSeverity == DiagnosticSeverity.Info && !errorlog)
            {
                // Verify analyzer with Hidden severity OR Info severity + no /errorlog is not executed.
                // Unless the analyzer reports 'CustomConfigurable' diagnostics, in which case it is always executed.
                expectedDiagnosticSeverity = customConfigurable ? defaultReportDiagnostic : ReportDiagnostic.Suppress;
                analyzer = new NamedTypeAnalyzerWithConfigurableEnabledByDefault(isEnabledByDefault: true, defaultSeverity, customConfigurable, throwOnAllNamedTypes: !customConfigurable);
                TestBulkAnalyzerConfigurationCore(analyzer, analyzerConfigText: string.Empty, errorlog, expectedDiagnosticSeverity);

                // Verify that bulk configuration 'none' entry does not enable this analyzer.
                // However, analyzer reporting 'CustomConfigurable' diagnostics is considered to be enabled.
                expectedDiagnosticSeverity = customConfigurable ? defaultReportDiagnostic : ReportDiagnostic.Suppress;
                analyzerConfigText = $@"
[*.cs]
dotnet_analyzer_diagnostic.severity = none";
                TestBulkAnalyzerConfigurationCore(analyzer, analyzerConfigText, errorlog, expectedDiagnosticSeverity);
            }
        }

        [WorkItem(38674, "https://github.com/dotnet/roslyn/issues/38674")]
        [InlineData(DiagnosticSeverity.Warning, false)]
        [InlineData(DiagnosticSeverity.Info, true)]
        [InlineData(DiagnosticSeverity.Info, false)]
        [InlineData(DiagnosticSeverity.Hidden, false)]
        [Theory]
        public void TestMixedCategoryBasedAndBulkAnalyzerDiagnosticConfiguration(DiagnosticSeverity defaultSeverity, bool errorlog)
        {
            var analyzer = new NamedTypeAnalyzerWithConfigurableEnabledByDefault(isEnabledByDefault: true, defaultSeverity);

            var diagnosticId = analyzer.Descriptor.Id;
            var category = analyzer.Descriptor.Category;

            // Verify category based configuration before bulk analyzer diagnostic configuration is respected.
            var analyzerConfigText = $@"
[*.cs]
dotnet_analyzer_diagnostic.category-{category}.severity = error
dotnet_analyzer_diagnostic.severity = warning";
            TestBulkAnalyzerConfigurationCore(analyzer, analyzerConfigText, errorlog, expectedDiagnosticSeverity: ReportDiagnostic.Error);

            // Verify category based configuration after bulk analyzer diagnostic configuration is respected.
            analyzerConfigText = $@"
[*.cs]
dotnet_analyzer_diagnostic.severity = warning
dotnet_analyzer_diagnostic.category-{category}.severity = error";
            TestBulkAnalyzerConfigurationCore(analyzer, analyzerConfigText, errorlog, expectedDiagnosticSeverity: ReportDiagnostic.Error);

            // Verify neither category based nor bulk diagnostic configuration is respected when specific diagnostic ID is configured in analyzer config.
            analyzerConfigText = $@"
[*.cs]
dotnet_diagnostic.{diagnosticId}.severity = warning
dotnet_analyzer_diagnostic.category-{category}.severity = none
dotnet_analyzer_diagnostic.severity = suggestion";
            TestBulkAnalyzerConfigurationCore(analyzer, analyzerConfigText, errorlog, expectedDiagnosticSeverity: ReportDiagnostic.Warn);

            // Verify neither category based nor bulk diagnostic configuration is respected when specific diagnostic ID is configured in ruleset.
            analyzerConfigText = $@"
[*.cs]
dotnet_analyzer_diagnostic.category-{category}.severity = none
dotnet_analyzer_diagnostic.severity = suggestion";
            var rulesetText = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <Rules AnalyzerId=""Microsoft.CodeAnalysis"" RuleNamespace=""Microsoft.CodeAnalysis"">
    <Rule Id=""{diagnosticId}"" Action=""Warning"" />
  </Rules>
</RuleSet>";
            TestBulkAnalyzerConfigurationCore(analyzer, analyzerConfigText, errorlog, expectedDiagnosticSeverity: ReportDiagnostic.Warn, rulesetText);
        }

        private void TestBulkAnalyzerConfigurationCore(
            NamedTypeAnalyzerWithConfigurableEnabledByDefault analyzer,
            string analyzerConfigText,
            bool errorlog,
            ReportDiagnostic expectedDiagnosticSeverity,
            string rulesetText = null,
            bool noWarn = false,
            bool warnAsError = false)
        {
            var diagnosticId = analyzer.Descriptor.Id;
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("test.cs").WriteAllText(@"class C { }");
            var analyzerConfig = dir.CreateFile(".editorconfig").WriteAllText(analyzerConfigText);

            var arguments = new[] {
                "/nologo",
                "/t:library",
                "/preferreduilang:en",
                "/analyzerconfig:" + analyzerConfig.Path,
                src.Path };
            if (noWarn)
            {
                arguments = arguments.Append($"/nowarn:{diagnosticId}");
            }

            if (warnAsError)
            {
                arguments = arguments.Append($"/warnaserror");
            }

            if (errorlog)
            {
                arguments = arguments.Append($"/errorlog:errorlog");
            }

            if (rulesetText != null)
            {
                var rulesetFile = CreateRuleSetFile(rulesetText);
                arguments = arguments.Append($"/ruleset:{rulesetFile.Path}");
            }

            var cmd = CreateCSharpCompiler(null, dir.Path, arguments,
                analyzers: new[] { analyzer });

            Assert.Equal(analyzerConfig.Path, Assert.Single(cmd.Arguments.AnalyzerConfigPaths));

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = cmd.Run(outWriter);

            var expectedErrorCode = expectedDiagnosticSeverity == ReportDiagnostic.Error ? 1 : 0;
            Assert.Equal(expectedErrorCode, exitCode);

            var prefix = expectedDiagnosticSeverity switch
            {
                ReportDiagnostic.Error => "error",
                ReportDiagnostic.Warn => "warning",
                ReportDiagnostic.Info => errorlog ? "info" : null,
                ReportDiagnostic.Hidden => null,
                ReportDiagnostic.Suppress => null,
                _ => throw ExceptionUtilities.UnexpectedValue(expectedDiagnosticSeverity)
            };

            if (prefix == null)
            {
                Assert.DoesNotContain(diagnosticId, outWriter.ToString());
            }
            else
            {
                Assert.Contains($"{prefix} {diagnosticId}: {analyzer.Descriptor.MessageFormat}", outWriter.ToString());
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [WorkItem(37779, "https://github.com/dotnet/roslyn/issues/37779")]
        public void CompilerWarnAsErrorDoesNotEmit(bool warnAsError)
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"
class C
{
    int _f;     // CS0169: unused field
}");

            var docName = "temp.xml";
            var pdbName = "temp.pdb";
            var additionalArgs = new[] { $"/doc:{docName}", $"/pdb:{pdbName}", "/debug" };
            if (warnAsError)
            {
                additionalArgs = additionalArgs.Append("/warnaserror").AsArray();
            }

            var expectedErrorCount = warnAsError ? 1 : 0;
            var expectedWarningCount = !warnAsError ? 1 : 0;
            var output = VerifyOutput(dir, src, includeCurrentAssemblyAsAnalyzerReference: false,
                                      additionalArgs,
                                      expectedErrorCount: expectedErrorCount,
                                      expectedWarningCount: expectedWarningCount);

            var expectedOutput = warnAsError ? "error CS0169" : "warning CS0169";
            Assert.Contains(expectedOutput, output);

            string binaryPath = Path.Combine(dir.Path, "temp.dll");
            Assert.True(File.Exists(binaryPath) == !warnAsError);

            string pdbPath = Path.Combine(dir.Path, pdbName);
            Assert.True(File.Exists(pdbPath) == !warnAsError);

            string xmlDocFilePath = Path.Combine(dir.Path, docName);
            Assert.True(File.Exists(xmlDocFilePath) == !warnAsError);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [WorkItem(37779, "https://github.com/dotnet/roslyn/issues/37779")]
        public void AnalyzerConfigSeverityEscalationToErrorDoesNotEmit(bool analyzerConfigSetToError)
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"
class C
{
    int _f;     // CS0169: unused field
}");

            var docName = "temp.xml";
            var pdbName = "temp.pdb";
            var additionalArgs = new[] { $"/doc:{docName}", $"/pdb:{pdbName}", "/debug" };

            if (analyzerConfigSetToError)
            {
                var analyzerConfig = dir.CreateFile(".editorconfig").WriteAllText(@"
[*.cs]
dotnet_diagnostic.cs0169.severity = error");

                additionalArgs = additionalArgs.Append("/analyzerconfig:" + analyzerConfig.Path).ToArray();
            }

            var expectedErrorCount = analyzerConfigSetToError ? 1 : 0;
            var expectedWarningCount = !analyzerConfigSetToError ? 1 : 0;
            var output = VerifyOutput(dir, src, includeCurrentAssemblyAsAnalyzerReference: false,
                                      additionalArgs,
                                      expectedErrorCount: expectedErrorCount,
                                      expectedWarningCount: expectedWarningCount);

            var expectedOutput = analyzerConfigSetToError ? "error CS0169" : "warning CS0169";
            Assert.Contains(expectedOutput, output);

            string binaryPath = Path.Combine(dir.Path, "temp.dll");
            Assert.True(File.Exists(binaryPath) == !analyzerConfigSetToError);

            string pdbPath = Path.Combine(dir.Path, pdbName);
            Assert.True(File.Exists(pdbPath) == !analyzerConfigSetToError);

            string xmlDocFilePath = Path.Combine(dir.Path, docName);
            Assert.True(File.Exists(xmlDocFilePath) == !analyzerConfigSetToError);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [WorkItem(37779, "https://github.com/dotnet/roslyn/issues/37779")]
        public void RulesetSeverityEscalationToErrorDoesNotEmit(bool rulesetSetToError)
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"
class C
{
    int _f;     // CS0169: unused field
}");

            var docName = "temp.xml";
            var pdbName = "temp.pdb";
            var additionalArgs = new[] { $"/doc:{docName}", $"/pdb:{pdbName}", "/debug" };

            if (rulesetSetToError)
            {
                string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <Rules AnalyzerId=""Microsoft.CodeAnalysis"" RuleNamespace=""Microsoft.CodeAnalysis"">
    <Rule Id=""CS0169"" Action=""Error"" />
  </Rules>
</RuleSet>
";
                var rulesetFile = CreateRuleSetFile(source);
                additionalArgs = additionalArgs.Append("/ruleset:" + rulesetFile.Path).ToArray();
            }

            var expectedErrorCount = rulesetSetToError ? 1 : 0;
            var expectedWarningCount = !rulesetSetToError ? 1 : 0;
            var output = VerifyOutput(dir, src, includeCurrentAssemblyAsAnalyzerReference: false,
                                      additionalArgs,
                                      expectedErrorCount: expectedErrorCount,
                                      expectedWarningCount: expectedWarningCount);

            var expectedOutput = rulesetSetToError ? "error CS0169" : "warning CS0169";
            Assert.Contains(expectedOutput, output);

            string binaryPath = Path.Combine(dir.Path, "temp.dll");
            Assert.True(File.Exists(binaryPath) == !rulesetSetToError);

            string pdbPath = Path.Combine(dir.Path, pdbName);
            Assert.True(File.Exists(pdbPath) == !rulesetSetToError);

            string xmlDocFilePath = Path.Combine(dir.Path, docName);
            Assert.True(File.Exists(xmlDocFilePath) == !rulesetSetToError);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [WorkItem(37779, "https://github.com/dotnet/roslyn/issues/37779")]
        public void AnalyzerWarnAsErrorDoesNotEmit(bool warnAsError)
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText("class C { }");

            var additionalArgs = warnAsError ? new[] { "/warnaserror" } : null;
            var expectedErrorCount = warnAsError ? 1 : 0;
            var expectedWarningCount = !warnAsError ? 1 : 0;
            var output = VerifyOutput(dir, src, includeCurrentAssemblyAsAnalyzerReference: false,
                                      additionalArgs,
                                      expectedErrorCount: expectedErrorCount,
                                      expectedWarningCount: expectedWarningCount,
                                      analyzers: new[] { new WarningDiagnosticAnalyzer() });

            var expectedDiagnosticSeverity = warnAsError ? "error" : "warning";
            Assert.Contains($"{expectedDiagnosticSeverity} {WarningDiagnosticAnalyzer.Warning01.Id}", output);

            string binaryPath = Path.Combine(dir.Path, "temp.dll");
            Assert.True(File.Exists(binaryPath) == !warnAsError);
        }

        // Currently, configuring no location diagnostics through editorconfig is not supported.
        [Theory(Skip = "https://github.com/dotnet/roslyn/issues/38042")]
        [CombinatorialData]
        public void AnalyzerConfigRespectedForNoLocationDiagnostic(ReportDiagnostic reportDiagnostic, bool isEnabledByDefault, bool noWarn, bool errorlog)
        {
            var analyzer = new AnalyzerWithNoLocationDiagnostics(isEnabledByDefault);
            TestAnalyzerConfigRespectedCore(analyzer, analyzer.Descriptor, reportDiagnostic, noWarn, errorlog, customConfigurable: false);
        }

        [WorkItem(37876, "https://github.com/dotnet/roslyn/issues/37876")]
        [Theory]
        [CombinatorialData]
        public void AnalyzerConfigRespectedForDisabledByDefaultDiagnostic(ReportDiagnostic analyzerConfigSeverity, bool isEnabledByDefault, bool noWarn, bool errorlog, bool customConfigurable)
        {
            var analyzer = new NamedTypeAnalyzerWithConfigurableEnabledByDefault(isEnabledByDefault, defaultSeverity: DiagnosticSeverity.Warning, customConfigurable, throwOnAllNamedTypes: false);
            TestAnalyzerConfigRespectedCore(analyzer, analyzer.Descriptor, analyzerConfigSeverity, noWarn, errorlog, customConfigurable);
        }

        private void TestAnalyzerConfigRespectedCore(DiagnosticAnalyzer analyzer, DiagnosticDescriptor descriptor, ReportDiagnostic analyzerConfigSeverity, bool noWarn, bool errorlog, bool customConfigurable)
        {
            if (analyzerConfigSeverity == ReportDiagnostic.Default)
            {
                // "dotnet_diagnostic.ID.severity = default" is not supported.
                return;
            }

            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("test.cs").WriteAllText(@"class C { }");
            var analyzerConfig = dir.CreateFile(".editorconfig").WriteAllText($@"
[*.cs]
dotnet_diagnostic.{descriptor.Id}.severity = {analyzerConfigSeverity.ToAnalyzerConfigString()}");

            // Severity of 'CustomSeverityConfigurable' diagnostics should not be affected by editorconfig entries.
            if (customConfigurable)
                analyzerConfigSeverity = DiagnosticDescriptor.MapSeverityToReport(descriptor.DefaultSeverity);

            var arguments = new[] {
                "/nologo",
                "/t:library",
                "/preferreduilang:en",
                "/analyzerconfig:" + analyzerConfig.Path,
                src.Path };
            if (noWarn)
            {
                arguments = arguments.Append($"/nowarn:{descriptor.Id}");
            }

            if (errorlog)
            {
                arguments = arguments.Append($"/errorlog:errorlog");
            }

            var cmd = CreateCSharpCompiler(null, dir.Path, arguments,
                analyzers: new[] { analyzer });

            Assert.Equal(analyzerConfig.Path, Assert.Single(cmd.Arguments.AnalyzerConfigPaths));

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = cmd.Run(outWriter);

            var expectedErrorCode = !noWarn && analyzerConfigSeverity == ReportDiagnostic.Error ? 1 : 0;
            Assert.Equal(expectedErrorCode, exitCode);

            // NOTE: Info diagnostics are only logged on command line when /errorlog is specified. See https://github.com/dotnet/roslyn/issues/42166 for details.
            if (!noWarn &&
                (analyzerConfigSeverity == ReportDiagnostic.Error ||
                analyzerConfigSeverity == ReportDiagnostic.Warn ||
                (analyzerConfigSeverity == ReportDiagnostic.Info && errorlog)))
            {
                var prefix = analyzerConfigSeverity == ReportDiagnostic.Error ? "error" : analyzerConfigSeverity == ReportDiagnostic.Warn ? "warning" : "info";
                Assert.Contains($"{prefix} {descriptor.Id}: {descriptor.MessageFormat}", outWriter.ToString());
            }
            else
            {
                Assert.DoesNotContain(descriptor.Id.ToString(), outWriter.ToString());
            }
        }

        [Fact]
        [WorkItem(3705, "https://github.com/dotnet/roslyn/issues/3705")]
        public void IsUserConfiguredGeneratedCodeInAnalyzerConfig()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"
class C
{
    void M(C? c)
    {
        _ = c.ToString();   // warning CS8602: Dereference of a possibly null reference.
    }
}");
            var output = VerifyOutput(dir, src, additionalFlags: new[] { "/nullable" }, expectedWarningCount: 1, includeCurrentAssemblyAsAnalyzerReference: false);
            // warning CS8602: Dereference of a possibly null reference.
            Assert.Contains("warning CS8602", output, StringComparison.Ordinal);

            // generated_code = true
            var analyzerConfigFile = dir.CreateFile(".editorconfig");
            var analyzerConfig = analyzerConfigFile.WriteAllText(@"
[*.cs]
generated_code = true");
            output = VerifyOutput(dir, src, additionalFlags: new[] { "/nullable", "/analyzerconfig:" + analyzerConfig.Path }, expectedWarningCount: 1, includeCurrentAssemblyAsAnalyzerReference: false);
            Assert.DoesNotContain("warning CS8602", output, StringComparison.Ordinal);
            // warning CS8669: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context. Auto-generated code requires an explicit '#nullable' directive in source.
            Assert.Contains("warning CS8669", output, StringComparison.Ordinal);

            // generated_code = false
            analyzerConfig = analyzerConfigFile.WriteAllText(@"
[*.cs]
generated_code = false");
            output = VerifyOutput(dir, src, additionalFlags: new[] { "/nullable", "/analyzerconfig:" + analyzerConfig.Path }, expectedWarningCount: 1, includeCurrentAssemblyAsAnalyzerReference: false);
            // warning CS8602: Dereference of a possibly null reference.
            Assert.Contains("warning CS8602", output, StringComparison.Ordinal);

            // generated_code = auto
            analyzerConfig = analyzerConfigFile.WriteAllText(@"
[*.cs]
generated_code = auto");
            output = VerifyOutput(dir, src, additionalFlags: new[] { "/nullable", "/analyzerconfig:" + analyzerConfig.Path }, expectedWarningCount: 1, includeCurrentAssemblyAsAnalyzerReference: false);
            // warning CS8602: Dereference of a possibly null reference.
            Assert.Contains("warning CS8602", output, StringComparison.Ordinal);
        }

        [WorkItem(42166, "https://github.com/dotnet/roslyn/issues/42166")]
        [CombinatorialData, Theory]
        public void TestAnalyzerFilteringBasedOnSeverity(DiagnosticSeverity defaultSeverity, bool errorlog, bool customConfigurable)
        {
            // This test verifies that analyzer execution is skipped at build time for the following:
            //   1. Analyzer reporting Hidden diagnostics
            //   2. Analyzer reporting Info diagnostics, when /errorlog is not specified
            // However, an analyzer that reports diagnostics with "CustomSeverityConfigurable" tag should never be skipped for execution.
            var analyzerShouldBeSkipped = (defaultSeverity == DiagnosticSeverity.Hidden ||
                defaultSeverity == DiagnosticSeverity.Info && !errorlog) && !customConfigurable;

            // We use an analyzer that throws an exception on every analyzer callback.
            // So an AD0001 analyzer exception diagnostic is reported if analyzer executed, otherwise not.
            var analyzer = new NamedTypeAnalyzerWithConfigurableEnabledByDefault(isEnabledByDefault: true, defaultSeverity, customConfigurable, throwOnAllNamedTypes: true);

            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("test.cs").WriteAllText(@"class C { }");
            var args = new[] { "/nologo", "/t:library", "/preferreduilang:en", src.Path };
            if (errorlog)
                args = args.Append("/errorlog:errorlog");

            var cmd = CreateCSharpCompiler(null, dir.Path, args, analyzers: new[] { analyzer });
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = cmd.Run(outWriter);
            Assert.Equal(0, exitCode);
            var output = outWriter.ToString();
            if (analyzerShouldBeSkipped)
            {
                Assert.Empty(output);
            }
            else
            {
                Assert.Contains("warning AD0001: Analyzer 'Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers+NamedTypeAnalyzerWithConfigurableEnabledByDefault' threw an exception of type 'System.NotImplementedException'",
                    output, StringComparison.Ordinal);
            }
        }

        [WorkItem(47017, "https://github.com/dotnet/roslyn/issues/47017")]
        [CombinatorialData, Theory]
        public void TestWarnAsErrorMinusDoesNotEnableDisabledByDefaultAnalyzers(DiagnosticSeverity defaultSeverity, bool isEnabledByDefault, bool customConfigurable)
        {
            // This test verifies that '/warnaserror-:DiagnosticId' does not affect if analyzers are executed or skipped..
            // Setup the analyzer to always throw an exception on analyzer callbacks for cases where we expect analyzer execution to be skipped:
            //   1. Disabled by default analyzer, i.e. 'isEnabledByDefault == false'.
            //   2. Default severity Hidden/Info: We only execute analyzers reporting Warning/Error severity diagnostics on command line builds.
            // However, an analyzer reporting diagnostics with "CustomSeverityConfigurable" tag should never be skipped for execution.
            var analyzerShouldBeSkipped = (!isEnabledByDefault ||
                defaultSeverity is DiagnosticSeverity.Hidden or DiagnosticSeverity.Info) && !customConfigurable;

            var analyzer = new NamedTypeAnalyzerWithConfigurableEnabledByDefault(isEnabledByDefault, defaultSeverity, customConfigurable, throwOnAllNamedTypes: analyzerShouldBeSkipped);
            var diagnosticId = analyzer.Descriptor.Id;

            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("test.cs").WriteAllText(@"class C { }");

            // Verify '/warnaserror-:DiagnosticId' behavior.
            var args = new[] { "/warnaserror+", $"/warnaserror-:{diagnosticId}", "/nologo", "/t:library", "/preferreduilang:en", src.Path };

            var cmd = CreateCSharpCompiler(null, dir.Path, args, analyzers: new[] { analyzer });
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = cmd.Run(outWriter);
            var expectedExitCode = !analyzerShouldBeSkipped && defaultSeverity == DiagnosticSeverity.Error ? 1 : 0;
            Assert.Equal(expectedExitCode, exitCode);

            var output = outWriter.ToString();
            if (analyzerShouldBeSkipped || customConfigurable && defaultSeverity is DiagnosticSeverity.Hidden or DiagnosticSeverity.Info)
            {
                Assert.Empty(output);
            }
            else
            {
                var prefix = defaultSeverity == DiagnosticSeverity.Warning ? "warning" : "error";
                Assert.Contains($"{prefix} {diagnosticId}: {analyzer.Descriptor.MessageFormat}", output);
            }
        }

        [WorkItem(49446, "https://github.com/dotnet/roslyn/issues/49446")]
        [Theory]
        // Verify '/warnaserror-:ID' prevents escalation to 'Error' when config file bumps severity to 'Warning'
        [InlineData(false, false, DiagnosticSeverity.Info, DiagnosticSeverity.Warning, null, DiagnosticSeverity.Error)]
        [InlineData(true, false, DiagnosticSeverity.Info, DiagnosticSeverity.Warning, null, DiagnosticSeverity.Warning)]
        [InlineData(false, true, DiagnosticSeverity.Info, DiagnosticSeverity.Warning, null, DiagnosticSeverity.Error)]
        [InlineData(true, true, DiagnosticSeverity.Info, DiagnosticSeverity.Warning, null, DiagnosticSeverity.Warning)]
        // Verify '/warnaserror-:ID' prevents escalation to 'Error' when custom configured analyzer bumps severity to 'Warning'
        [InlineData(false, false, DiagnosticSeverity.Info, null, DiagnosticSeverity.Warning, DiagnosticSeverity.Error)]
        [InlineData(true, false, DiagnosticSeverity.Info, null, DiagnosticSeverity.Warning, DiagnosticSeverity.Warning)]
        [InlineData(false, true, DiagnosticSeverity.Info, null, DiagnosticSeverity.Warning, DiagnosticSeverity.Error)]
        [InlineData(true, true, DiagnosticSeverity.Info, null, DiagnosticSeverity.Warning, DiagnosticSeverity.Warning)]
        // Verify '/warnaserror-:ID' prevents escalation to 'Error' when default severity is 'Warning' and no config file or custom configured setting is specified.
        [InlineData(false, false, DiagnosticSeverity.Warning, null, null, DiagnosticSeverity.Error)]
        [InlineData(true, false, DiagnosticSeverity.Warning, null, null, DiagnosticSeverity.Warning)]
        [InlineData(false, true, DiagnosticSeverity.Warning, null, null, DiagnosticSeverity.Error)]
        [InlineData(true, true, DiagnosticSeverity.Warning, null, null, DiagnosticSeverity.Warning)]
        // Verify '/warnaserror-:ID' prevents escalation to 'Error' when default severity is 'Warning' and config file bumps severity to 'Error'
        [InlineData(false, false, DiagnosticSeverity.Warning, DiagnosticSeverity.Error, null, DiagnosticSeverity.Error)]
        [InlineData(true, false, DiagnosticSeverity.Warning, DiagnosticSeverity.Error, null, DiagnosticSeverity.Warning)]
        [InlineData(false, true, DiagnosticSeverity.Warning, DiagnosticSeverity.Error, null, DiagnosticSeverity.Error)]
        [InlineData(true, true, DiagnosticSeverity.Warning, DiagnosticSeverity.Error, null, DiagnosticSeverity.Warning)]
        // Verify '/warnaserror-:ID' has no effect when default severity is 'Info' and config file bumps severity to 'Error'
        [InlineData(false, false, DiagnosticSeverity.Info, DiagnosticSeverity.Error, null, DiagnosticSeverity.Error)]
        [InlineData(true, false, DiagnosticSeverity.Info, DiagnosticSeverity.Error, null, DiagnosticSeverity.Error)]
        [InlineData(false, true, DiagnosticSeverity.Info, DiagnosticSeverity.Error, null, DiagnosticSeverity.Error)]
        [InlineData(true, true, DiagnosticSeverity.Info, DiagnosticSeverity.Error, null, DiagnosticSeverity.Error)]
        // Verify '/warnaserror-:ID' has no effect when default severity is 'Info' or 'Warning' and custom configured severity is 'Error'
        [InlineData(false, false, DiagnosticSeverity.Info, null, DiagnosticSeverity.Error, DiagnosticSeverity.Error)]
        [InlineData(true, false, DiagnosticSeverity.Info, null, DiagnosticSeverity.Error, DiagnosticSeverity.Error)]
        [InlineData(false, true, DiagnosticSeverity.Info, null, DiagnosticSeverity.Error, DiagnosticSeverity.Error)]
        [InlineData(true, true, DiagnosticSeverity.Info, null, DiagnosticSeverity.Error, DiagnosticSeverity.Error)]
        [InlineData(false, false, DiagnosticSeverity.Warning, null, DiagnosticSeverity.Error, DiagnosticSeverity.Error)]
        [InlineData(true, false, DiagnosticSeverity.Warning, null, DiagnosticSeverity.Error, DiagnosticSeverity.Error)]
        [InlineData(false, true, DiagnosticSeverity.Warning, null, DiagnosticSeverity.Error, DiagnosticSeverity.Error)]
        [InlineData(true, true, DiagnosticSeverity.Warning, null, DiagnosticSeverity.Error, DiagnosticSeverity.Error)]
        public void TestWarnAsErrorMinusDoesNotNullifyEditorConfig(
            bool warnAsErrorMinus,
            bool useGlobalConfig,
            DiagnosticSeverity defaultSeverity,
            DiagnosticSeverity? severityInConfigFile,
            DiagnosticSeverity? customConfiguredSeverityByAnalyzer,
            DiagnosticSeverity expectedEffectiveSeverity)
        {
            var customConfigurable = customConfiguredSeverityByAnalyzer.HasValue;
            var reportedSeverity = customConfiguredSeverityByAnalyzer ?? defaultSeverity;
            var analyzer = new NamedTypeAnalyzerWithConfigurableEnabledByDefault(isEnabledByDefault: true, defaultSeverity, reportedSeverity, customConfigurable, throwOnAllNamedTypes: false);
            var diagnosticId = analyzer.Descriptor.Id;

            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("test.cs").WriteAllText(@"class C { }");
            var additionalFlags = new[] { "/warnaserror+" };

            if (severityInConfigFile.HasValue)
            {
                var severityString = DiagnosticDescriptor.MapSeverityToReport(severityInConfigFile.Value).ToAnalyzerConfigString();

                TempFile analyzerConfig;
                if (useGlobalConfig)
                {
                    analyzerConfig = dir.CreateFile(".globalconfig").WriteAllText($@"
is_global = true
dotnet_diagnostic.{diagnosticId}.severity = {severityString}");
                }
                else
                {
                    analyzerConfig = dir.CreateFile(".editorconfig").WriteAllText($@"
[*.cs]
dotnet_diagnostic.{diagnosticId}.severity = {severityString}");
                }

                additionalFlags = additionalFlags.Append($"/analyzerconfig:{analyzerConfig.Path}").ToArray();
            }

            if (warnAsErrorMinus)
            {
                additionalFlags = additionalFlags.Append($"/warnaserror-:{diagnosticId}").ToArray();
            }

            int expectedWarningCount = 0, expectedErrorCount = 0;
            switch (expectedEffectiveSeverity)
            {
                case DiagnosticSeverity.Warning:
                    expectedWarningCount = 1;
                    break;

                case DiagnosticSeverity.Error:
                    expectedErrorCount = 1;
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(expectedEffectiveSeverity);
            }

            VerifyOutput(dir, src, includeCurrentAssemblyAsAnalyzerReference: false,
                expectedWarningCount: expectedWarningCount,
                expectedErrorCount: expectedErrorCount,
                additionalFlags: additionalFlags,
                analyzers: new[] { analyzer });
        }

        [Fact]
        public void SourceGenerators_EmbeddedSources()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"
class C
{
}");

            var generatedSource = "public class D { }";
            var generator = new SingleFileTestGenerator(generatedSource, "generatedSource.cs");

            VerifyOutput(dir, src, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/debug:embedded", "/out:embed.exe" }, generators: new[] { generator }, analyzers: null);

            var generatorPrefix = GeneratorDriver.GetFilePathPrefixForGenerator(dir.Path, generator);
            ValidateEmbeddedSources_Portable(new Dictionary<string, string> { { Path.Combine(dir.Path, generatorPrefix, $"generatedSource.cs"), generatedSource } }, dir, true);

            // Clean up temp files
            CleanupAllGeneratedFiles(src.Path);
            Directory.Delete(dir.Path, true);
        }

        [Theory, CombinatorialData]
        [WorkItem(40926, "https://github.com/dotnet/roslyn/issues/40926")]
        public void TestSourceGeneratorsWithAnalyzers(bool includeCurrentAssemblyAsAnalyzerReference, bool skipAnalyzers)
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"
class C
{
}");

            var generatedSource = "public class D { }";
            var generator = new SingleFileTestGenerator(generatedSource, "generatedSource.cs");

            // 'skipAnalyzers' should have no impact on source generator execution, but should prevent analyzer execution.
            var skipAnalyzersFlag = "/skipAnalyzers" + (skipAnalyzers ? "+" : "-");

            // Verify analyzers were executed only if both the following conditions were satisfied:
            //  1. Current assembly was added as an analyzer reference, i.e. "includeCurrentAssemblyAsAnalyzerReference = true" and
            //  2. We did not explicitly request skipping analyzers, i.e. "skipAnalyzers = false".
            var expectedAnalyzerExecution = includeCurrentAssemblyAsAnalyzerReference && !skipAnalyzers;

            // 'WarningDiagnosticAnalyzer' generates a warning for each named type.
            // We expect two warnings for this test: type "C" defined in source and the source generator defined type.
            // Additionally, we also have an analyzer that generates "warning CS8032: An instance of analyzer cannot be created"
            // CS8032 is generated with includeCurrentAssemblyAsAnalyzerReference even when we are skipping analyzers as we will instantiate all analyzers, just not execute them.
            var expectedWarningCount = expectedAnalyzerExecution ? 3 : (includeCurrentAssemblyAsAnalyzerReference ? 1 : 0);

            var output = VerifyOutput(dir, src, includeCurrentAssemblyAsAnalyzerReference,
                expectedWarningCount: expectedWarningCount,
                additionalFlags: new[] { "/debug:embedded", "/out:embed.exe", skipAnalyzersFlag },
                generators: new[] { generator });

            // Verify source generator was executed, regardless of the value of 'skipAnalyzers'.
            var generatorPrefix = GeneratorDriver.GetFilePathPrefixForGenerator(dir.Path, generator);
            ValidateEmbeddedSources_Portable(new Dictionary<string, string> { { Path.Combine(dir.Path, generatorPrefix, "generatedSource.cs"), generatedSource } }, dir, true);

            if (expectedAnalyzerExecution)
            {
                Assert.Contains("warning Warning01", output, StringComparison.Ordinal);
                Assert.Contains("warning CS8032", output, StringComparison.Ordinal);
            }
            else if (includeCurrentAssemblyAsAnalyzerReference)
            {
                Assert.Contains("warning CS8032", output, StringComparison.Ordinal);
            }
            else
            {
                Assert.Empty(output);
            }

            // Clean up temp files
            CleanupAllGeneratedFiles(src.Path);
        }

        [Theory]
        [InlineData("partial class D {}", "file1.cs", "partial class E {}", "file2.cs")] // different files, different names
        [InlineData("partial class D {}", "file1.cs", "partial class E {}", "file1.cs")] // different files, same names
        [InlineData("partial class D {}", "file1.cs", "partial class D {}", "file2.cs")] // same files, different names
        [InlineData("partial class D {}", "file1.cs", "partial class D {}", "file1.cs")] // same files, same names
        [InlineData("partial class D {}", "file1.cs", "", "file2.cs")] // empty second file
        public void SourceGenerators_EmbeddedSources_MultipleFiles(string source1, string source1Name, string source2, string source2Name)
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"
class C
{
}");
            var generator = new SingleFileTestGenerator(source1, source1Name);
            var generator2 = new SingleFileTestGenerator2(source2, source2Name);

            VerifyOutput(dir, src, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/debug:embedded", "/out:embed.exe" }, generators: new[] { generator, generator2 }, analyzers: null);

            var generator1Prefix = GeneratorDriver.GetFilePathPrefixForGenerator(dir.Path, generator);
            var generator2Prefix = GeneratorDriver.GetFilePathPrefixForGenerator(dir.Path, generator2);

            ValidateEmbeddedSources_Portable(new Dictionary<string, string>
            {
                { Path.Combine(dir.Path, generator1Prefix, source1Name), source1},
                { Path.Combine(dir.Path, generator2Prefix, source2Name), source2},
            }, dir, true);

            // Clean up temp files
            CleanupAllGeneratedFiles(src.Path);
            Directory.Delete(dir.Path, true);
        }

        [Theory]
        [InlineData([new[] { "/langversion:preview", "/out:checksum.exe", "/pdb:checksum.pdb", "/debug:portable", "/checksumAlgorithm:SHA256" }])]
        [InlineData([new[] { "/langversion:preview", "/out:checksum.exe", "/pdb:checksum.pdb", "/debug:portable" }])]
        public void SourceGenerators_ChecksumAlgorithm(string[] additionalFlags)
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs");
            src.WriteAllText("class C { }");

            var genPath1 = Path.Combine(dir.Path, "Microsoft.CodeAnalysis.Test.Utilities", "Roslyn.Test.Utilities.TestGenerators.TestSourceGenerator", "hint1.cs");
            var genPath2 = Path.Combine(dir.Path, "Microsoft.CodeAnalysis.Test.Utilities", "Roslyn.Test.Utilities.TestGenerators.TestSourceGenerator", "hint2.cs");

            var generator = new TestSourceGenerator()
            {
                ExecuteImpl = context =>
                {
                    context.AddSource("hint1", "class G1 { void F() {} }");
                    context.AddSource("hint2", SourceText.From("class G2 { void F() {} }", Encoding.UTF8, checksumAlgorithm: SourceHashAlgorithm.Sha1));
                }
            };

            VerifyOutput(
                dir,
                src,
                includeCurrentAssemblyAsAnalyzerReference: false,
                additionalFlags,
                generators: new[] { generator },
                analyzers: null);

            using (Stream peStream = File.OpenRead(Path.Combine(dir.Path, "checksum.exe")), pdbStream = File.OpenRead(Path.Combine(dir.Path, "checksum.pdb")))
            {
                PdbValidation.VerifyPdb(peStream, pdbStream, $@"
<symbols>
  <files>
    <file id=""1"" name=""{src.Path}"" language=""C#"" checksumAlgorithm=""SHA256"" checksum=""A0-78-BB-A8-E8-B1-E1-3B-E8-63-80-7D-CE-CC-4B-0D-14-EF-06-D3-9B-14-52-E1-95-C6-64-D1-36-EC-7C-25"" />
    <file id=""2"" name=""{genPath1}"" language=""C#"" checksumAlgorithm=""SHA256"" checksum=""FC-9C-F6-B3-BB-61-93-0E-1E-03-A2-62-0B-B5-D9-CE-1D-C9-40-79-72-4F-3A-6A-C6-5D-F3-84-69-5F-62-10""><![CDATA[﻿class G1 {{ void F() {{}} }}]]></file>
    <file id=""3"" name=""{genPath2}"" language=""C#"" checksumAlgorithm=""SHA256"" checksum=""64-A9-4B-81-04-84-18-CD-73-F7-F8-3B-06-32-4B-9C-F9-36-D4-7A-7B-D0-2F-34-ED-8C-B7-AA-48-43-55-35""><![CDATA[﻿class G2 {{ void F() {{}} }}]]></file>
  </files>
</symbols>", PdbValidationOptions.ExcludeMethods);
            }

            Directory.Delete(dir.Path, true);
        }

        [Fact]
        public void SourceGenerators_ChecksumAlgorithm_Sha1()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs");
            src.WriteAllText("class C { }");

            var genPath1 = Path.Combine(dir.Path, "Microsoft.CodeAnalysis.Test.Utilities", "Roslyn.Test.Utilities.TestGenerators.TestSourceGenerator", "hint1.cs");
            var genPath2 = Path.Combine(dir.Path, "Microsoft.CodeAnalysis.Test.Utilities", "Roslyn.Test.Utilities.TestGenerators.TestSourceGenerator", "hint2.cs");

            var generator = new TestSourceGenerator()
            {
                ExecuteImpl = context =>
                {
                    context.AddSource("hint1", "class G1 { void F() {} }");
                    context.AddSource("hint2", SourceText.From("class G2 { void F() {} }", Encoding.UTF8, checksumAlgorithm: SourceHashAlgorithm.Sha256));
                }
            };

            VerifyOutput(
                dir,
                src,
                includeCurrentAssemblyAsAnalyzerReference: false,
                additionalFlags: new[] { "/langversion:preview", "/out:checksum.exe", "/pdb:checksum.pdb", "/debug:portable", "/checksumAlgorithm:SHA1" },
                generators: new[] { generator },
                analyzers: null);

            using (Stream peStream = File.OpenRead(Path.Combine(dir.Path, "checksum.exe")), pdbStream = File.OpenRead(Path.Combine(dir.Path, "checksum.pdb")))
            {
                PdbValidation.VerifyPdb(peStream, pdbStream, $@"
<symbols>
  <files>
    <file id=""1"" name=""{src.Path}"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""8A-B8-FF-37-76-5D-12-10-93-F1-CF-51-3E-51-1B-76-2B-90-15-C4"" />
    <file id=""2"" name=""{genPath1}"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""D8-87-89-A3-FE-EA-FD-AB-49-31-5A-25-B0-05-6B-6F-00-00-C2-DD""><![CDATA[﻿class G1 {{ void F() {{}} }}]]></file>
    <file id=""3"" name=""{genPath2}"" language=""C#"" checksumAlgorithm=""SHA1"" checksum=""F1-D0-FD-F0-08-9F-1B-32-9F-EF-41-A1-58-A3-14-FF-E8-06-A8-38""><![CDATA[﻿class G2 {{ void F() {{}} }}]]></file>
  </files>
</symbols>", PdbValidationOptions.ExcludeMethods);
            }

            Directory.Delete(dir.Path, true);
        }

        [Theory]
        [InlineData("generatedSource.cs", "", "generatedSource.cs")]
        [InlineData("..", "", "...cs")]
        [InlineData(".", "", "..cs")]
        [InlineData("abc/", "abc", ".cs")]
        [InlineData("abc\\", "abc", ".cs")]
        [InlineData("abc/ ", "abc", " .cs")]
        [InlineData("a/b/c", "a/b", "c.cs")]
        [InlineData("a/b\\c", "a/b", "c.cs")]
        [InlineData("a\\b\\c", "a/b", "c.cs")]
        [InlineData(" abc ", "", " abc .cs")]
        [InlineData(" abc/generated.cs", " abc", "generated.cs")]
        [InlineData(" abc\\generated.cs", " abc", "generated.cs")]
        [InlineData(" a/ b/ generated.cs", " a/ b", " generated.cs")]
        [InlineData(" a\\ b\\ generated.cs", " a/ b", " generated.cs")]
        public void SourceGenerators_WriteGeneratedSources(string hintName, string expectedDir, string expectedFileName)
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"
class C
{
}");
            var generatedDir = dir.CreateDirectory("generated");

            var generatedSource = "public class D { }";
            var generator = new SingleFileTestGenerator(generatedSource, hintName);

            VerifyOutput(dir, src, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/generatedfilesout:" + generatedDir.Path, "/langversion:preview", "/out:embed.exe" }, generators: new[] { generator }, analyzers: null);

            var generatorPrefix = GeneratorDriver.GetFilePathPrefixForGenerator(generatedDir.Path, generator);
            ValidateWrittenSources(new()
            {
                { Path.Combine(generatedDir.Path, generatorPrefix, expectedDir), new() { { expectedFileName, generatedSource } } }
            });

            // Clean up temp files
            CleanupAllGeneratedFiles(src.Path);
            Directory.Delete(dir.Path, true);
        }

        [Fact]
        public void SourceGenerators_OverwriteGeneratedSources()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"
class C
{
}");
            var generatedDir = dir.CreateDirectory("generated");

            var generatedSource1 = "class D { } class E { }";
            var generator1 = new SingleFileTestGenerator(generatedSource1, "generatedSource.cs");

            VerifyOutput(dir, src, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/generatedfilesout:" + generatedDir.Path, "/langversion:preview", "/out:embed.exe" }, generators: new[] { generator1 }, analyzers: null);

            var generatorPrefix = GeneratorDriver.GetFilePathPrefixForGenerator(generatedDir.Path, generator1);
            ValidateWrittenSources(new() { { Path.Combine(generatedDir.Path, generatorPrefix), new() { { "generatedSource.cs", generatedSource1 } } } });

            var generatedSource2 = "public class D { }";
            var generator2 = new SingleFileTestGenerator(generatedSource2, "generatedSource.cs");

            VerifyOutput(dir, src, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/generatedfilesout:" + generatedDir.Path, "/langversion:preview", "/out:embed.exe" }, generators: new[] { generator2 }, analyzers: null);

            ValidateWrittenSources(new() { { Path.Combine(generatedDir.Path, generatorPrefix), new() { { "generatedSource.cs", generatedSource2 } } } });

            // Clean up temp files
            CleanupAllGeneratedFiles(src.Path);
            Directory.Delete(dir.Path, true);
        }

        [Theory]
        [InlineData("partial class D {}", "file1.cs", "partial class E {}", "file2.cs")] // different files, different names
        [InlineData("partial class D {}", "file1.cs", "partial class E {}", "file1.cs")] // different files, same names
        [InlineData("partial class D {}", "file1.cs", "partial class D {}", "file2.cs")] // same files, different names
        [InlineData("partial class D {}", "file1.cs", "partial class D {}", "file1.cs")] // same files, same names
        [InlineData("partial class D {}", "file1.cs", "", "file2.cs")] // empty second file
        public void SourceGenerators_WriteGeneratedSources_MultipleFiles(string source1, string source1Name, string source2, string source2Name)
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"
class C
{
}");
            var generatedDir = dir.CreateDirectory("generated");

            var generator = new SingleFileTestGenerator(source1, source1Name);
            var generator2 = new SingleFileTestGenerator2(source2, source2Name);

            VerifyOutput(dir, src, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/generatedfilesout:" + generatedDir.Path, "/langversion:preview", "/out:embed.exe" }, generators: new[] { generator, generator2 }, analyzers: null);

            var generator1Prefix = GeneratorDriver.GetFilePathPrefixForGenerator(generatedDir.Path, generator);
            var generator2Prefix = GeneratorDriver.GetFilePathPrefixForGenerator(generatedDir.Path, generator2);

            ValidateWrittenSources(new()
            {
                { generator1Prefix, new() { { source1Name, source1 } } },
                { generator2Prefix, new() { { source2Name, source2 } } }
            });

            // Clean up temp files
            CleanupAllGeneratedFiles(src.Path);
            Directory.Delete(dir.Path, true);
        }

        [Theory]
        [InlineData("subdir")]
        [InlineData("a/b/c")]
        [InlineData("a\\b\\c", "a/b/c")]
        [InlineData(" subdir")]
        [InlineData(" a/ b/ c")]
        [InlineData(" a\\ b/ c", " a/ b/ c")]
        [InlineData("abc/")]
        public void SourceGenerators_WriteGeneratedSources_WithDirectories(string subdir, string expectedDir = null)
        {
            expectedDir ??= subdir;

            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText("""
                class C
                {
                }
                """);
            var generatedDir = dir.CreateDirectory("generated");

            var generatedSource = "public class D { }";
            var generatedFileName = "generatedSource.cs";
            var generatedPath = Path.Combine(subdir, generatedFileName);
            var generator = new SingleFileTestGenerator(generatedSource, generatedPath);

            VerifyOutput(dir, src, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/generatedfilesout:" + generatedDir.Path, "/langversion:preview", "/out:embed.exe" }, generators: new[] { generator }, analyzers: null);

            var generatorPrefix = GeneratorDriver.GetFilePathPrefixForGenerator(generatedDir.Path, generator);
            ValidateWrittenSources(new()
            {
                { Path.Combine(generatedDir.Path, generatorPrefix, expectedDir), new() { { generatedFileName, generatedSource } } }
            });

            // Clean up temp files
            CleanupAllGeneratedFiles(src.Path);
            Directory.Delete(dir.Path, true);
        }

        [ConditionalFact(typeof(DesktopClrOnly))]  //CoreCLR doesn't support SxS loading
        [WorkItem(47990, "https://github.com/dotnet/roslyn/issues/47990")]
        public void SourceGenerators_SxS_AssemblyLoading()
        {
            // compile the generators
            var dir = Temp.CreateDirectory();
            var snk = Temp.CreateFile("TestKeyPair_", ".snk", dir.Path).WriteAllBytes(TestResources.General.snKey);
            var src = dir.CreateFile("generator.cs");
            var virtualSnProvider = new DesktopStrongNameProvider(ImmutableArray.Create(dir.Path));

            string createGenerator(string version)
            {
                var generatorSource = $@"
using Microsoft.CodeAnalysis;
[assembly:System.Reflection.AssemblyVersion(""{version}"")]

[Generator]
public class TestGenerator : ISourceGenerator
{{
            public void Execute(GeneratorExecutionContext context) {{ context.AddSource(""generatedSource.cs"", ""//from version {version}""); }}
            public void Initialize(GeneratorInitializationContext context) {{ }}
 }}";

                var path = Path.Combine(dir.Path, Guid.NewGuid().ToString() + ".dll");

                var comp = CreateEmptyCompilation(source: generatorSource,
                                             references: TargetFrameworkUtil.NetStandard20References.Add(MetadataReference.CreateFromAssemblyInternal(typeof(ISourceGenerator).Assembly)),
                                             options: TestOptions.DebugDll.WithCryptoKeyFile(Path.GetFileName(snk.Path)).WithStrongNameProvider(virtualSnProvider),
                                             assemblyName: "generator");
                comp.VerifyDiagnostics();
                comp.Emit(path);
                return path;
            }

            var gen1 = createGenerator("1.0.0.0");
            var gen2 = createGenerator("2.0.0.0");

            var generatedDir = dir.CreateDirectory("generated");
            VerifyOutput(dir, src, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/generatedfilesout:" + generatedDir.Path, "/analyzer:" + gen1, "/analyzer:" + gen2 }.ToArray());

            // This is wrong! Both generators are writing the same file out, over the top of each other
            // See https://github.com/dotnet/roslyn/issues/47990
            ValidateWrittenSources(new()
            {
                //  { Path.Combine(generatedDir.Path,  "generator", "TestGenerator"), new() { { "generatedSource.cs", "//from version 1.0.0.0" } } },
                { Path.Combine(generatedDir.Path, "generator", "TestGenerator"), new() { { "generatedSource.cs", "//from version 2.0.0.0" } } }
            });
        }

        [Fact]
        public void SourceGenerators_DoNotWriteGeneratedSources_When_No_Directory_Supplied()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"
class C
{
}");
            var generatedDir = dir.CreateDirectory("generated");

            var generatedSource = "public class D { }";
            var generator = new SingleFileTestGenerator(generatedSource, "generatedSource.cs");

            VerifyOutput(dir, src, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/langversion:preview", "/out:embed.exe" }, generators: new[] { generator }, analyzers: null);
            ValidateWrittenSources(new() { { generatedDir.Path, new() } });

            // Clean up temp files
            CleanupAllGeneratedFiles(src.Path);
            Directory.Delete(dir.Path, true);
        }

        [Fact]
        public void SourceGenerators_Error_When_GeneratedDir_NotExist()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"
class C
{
}");

            var generatedDirPath = Path.Combine(dir.Path, "noexist");
            var generatedSource = "public class D { }";
            var generator = new SingleFileTestGenerator(generatedSource, "generatedSource.cs");

            var output = VerifyOutput(dir, src, expectedErrorCount: 1, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/generatedfilesout:" + generatedDirPath, "/langversion:preview", "/out:embed.exe" }, generators: new[] { generator }, analyzers: null);
            Assert.Contains("CS0016:", output);

            // Clean up temp files
            CleanupAllGeneratedFiles(src.Path);
            Directory.Delete(dir.Path, true);
        }

        [Fact]
        public void SourceGenerators_GeneratedDir_Has_Spaces()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"
class C
{
}");
            var generatedDir = dir.CreateDirectory("generated files");

            var generatedSource = "public class D { }";
            var generator = new SingleFileTestGenerator(generatedSource, "generatedSource.cs");

            VerifyOutput(dir, src, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/generatedfilesout:" + generatedDir.Path, "/langversion:preview", "/out:embed.exe" }, generators: new[] { generator }, analyzers: null);

            var generatorPrefix = GeneratorDriver.GetFilePathPrefixForGenerator(generatedDir.Path, generator);
            ValidateWrittenSources(new() { { Path.Combine(generatedDir.Path, generatorPrefix), new() { { "generatedSource.cs", generatedSource } } } });

            // Clean up temp files
            CleanupAllGeneratedFiles(src.Path);
            Directory.Delete(dir.Path, true);
        }

        [Fact]
        public void ParseGeneratedFilesOut()
        {
            string root = PathUtilities.IsUnixLikePlatform ? "/" : "c:\\";
            string baseDirectory = Path.Combine(root, "abc", "def");

            var parsedArgs = DefaultParse(new[] { @"/generatedfilesout:", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing '<text>' for '/generatedfilesout:' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "/generatedfilesout:"));
            Assert.Null(parsedArgs.GeneratedFilesOutputDirectory);

            parsedArgs = DefaultParse(new[] { @"/generatedfilesout:""""", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing '<text>' for '/generatedfilesout:' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "/generatedfilesout:\"\""));
            Assert.Null(parsedArgs.GeneratedFilesOutputDirectory);

            parsedArgs = DefaultParse(new[] { @"/generatedfilesout:outdir", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(Path.Combine(baseDirectory, "outdir"), parsedArgs.GeneratedFilesOutputDirectory);

            parsedArgs = DefaultParse(new[] { @"/generatedfilesout:""outdir""", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(Path.Combine(baseDirectory, "outdir"), parsedArgs.GeneratedFilesOutputDirectory);

            parsedArgs = DefaultParse(new[] { @"/generatedfilesout:out dir", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(Path.Combine(baseDirectory, "out dir"), parsedArgs.GeneratedFilesOutputDirectory);

            parsedArgs = DefaultParse(new[] { @"/generatedfilesout:""out dir""", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(Path.Combine(baseDirectory, "out dir"), parsedArgs.GeneratedFilesOutputDirectory);

            var absPath = Path.Combine(root, "outdir");
            parsedArgs = DefaultParse(new[] { $@"/generatedfilesout:{absPath}", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(absPath, parsedArgs.GeneratedFilesOutputDirectory);

            parsedArgs = DefaultParse(new[] { $@"/generatedfilesout:""{absPath}""", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(absPath, parsedArgs.GeneratedFilesOutputDirectory);

            absPath = Path.Combine(root, "generated files");
            parsedArgs = DefaultParse(new[] { $@"/generatedfilesout:{absPath}", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(absPath, parsedArgs.GeneratedFilesOutputDirectory);

            parsedArgs = DefaultParse(new[] { $@"/generatedfilesout:""{absPath}""", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(absPath, parsedArgs.GeneratedFilesOutputDirectory);
        }

        [Fact]
        public void SourceGenerators_Error_When_NoDirectoryArgumentGiven()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"
class C
{
}");
            var output = VerifyOutput(dir, src, expectedErrorCount: 2, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/generatedfilesout:", "/langversion:preview", "/out:embed.exe" });
            Assert.Contains("error CS2006: Command-line syntax error: Missing '<text>' for '/generatedfilesout:' option", output);

            // Clean up temp files
            CleanupAllGeneratedFiles(src.Path);
            Directory.Delete(dir.Path, true);
        }

        [Fact]
        public void SourceGenerators_ReportedWrittenFiles_To_TouchedFilesLogger()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"
class C
{
}");
            var generatedDir = dir.CreateDirectory("generated");

            var generatedSource = "public class D { }";
            var generator = new SingleFileTestGenerator(generatedSource, "generatedSource.cs");

            VerifyOutput(dir, src, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/generatedfilesout:" + generatedDir.Path, $"/touchedfiles:{dir.Path}/touched", "/langversion:preview", "/out:embed.exe" }, generators: new[] { generator }, analyzers: null);

            var touchedFiles = Directory.GetFiles(dir.Path, "touched*");
            Assert.Equal(2, touchedFiles.Length);

            string[] writtenText = File.ReadAllLines(Path.Combine(dir.Path, "touched.write"));
            Assert.Equal(2, writtenText.Length);
            Assert.EndsWith("EMBED.EXE", writtenText[0], StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith("GENERATEDSOURCE.CS", writtenText[1], StringComparison.OrdinalIgnoreCase);

            // Clean up temp files
            CleanupAllGeneratedFiles(src.Path);
            Directory.Delete(dir.Path, true);
        }

        [Fact]
        public void Interceptors_RelativePath_GeneratedFiles_EndToEnd()
        {
            var dir = Temp.CreateDirectory();

            var srcDir = dir.CreateDirectory("src");
            var src = srcDir.CreateFile("Program.cs").WriteAllText("""
                class Program
                {
                    static void Main()
                    {
                        M();
                    }

                    public static void M() => throw null!;
                }
                """);

            // final path will look like:
            // 'TempDir/{assemblyName}/{generatorName}/Generated.cs'
            // Note that generator will have access to the full path of the generated file, before adding it to the compilation
            // additionally we plan to add public API to determine a "correct relative path" to use here without any additional tricks
            var generatedSource = """
                using System.Runtime.CompilerServices;
                using System;

                namespace Generated
                {
                    static class Interceptors
                    {
                        [InterceptsLocation("../../src/Program.cs", 5, 9)]
                        public static void M1() => Console.Write(1);
                    }
                }

                namespace System.Runtime.CompilerServices
                {
                    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
                    public sealed class InterceptsLocationAttribute : Attribute
                    {
                        public InterceptsLocationAttribute(string filePath, int line, int character)
                        {
                        }
                    }
                }
                """;
            var generator = new SingleFileTestGenerator(generatedSource, "Generated.cs");

            // Future note: it would be good to have end-to-end tests using InterceptableLocation APIs.
            // Once support for path-based attributes is fully dropped, consider porting these 'Interceptors_' tests accordingly.
            VerifyOutput(
                dir,
                src,
                includeCurrentAssemblyAsAnalyzerReference: false,
                additionalFlags: ["/langversion:preview",
                    "/out:embed.exe",
                    "/features:InterceptorsNamespaces=Generated",
                    "/warn:9"],
                expectedWarningCount: 1,
                generators: [generator],
                analyzers: null);
            ValidateWrittenSources([]);

            // Clean up temp files
            CleanupAllGeneratedFiles(src.Path);
            Directory.Delete(dir.Path, true);
        }

        [Fact]
        public void Interceptors_RelativePath_GeneratedFiles_EndToEnd_OutputDirectoryNested()
        {
            // Demonstrates the difference between defaulting the generated files base path to 'Arguments.OutputDirectory'
            // versus 'Arguments.BaseDirectory' (which occurs implicitly for relative paths in command line arguments)

            var dir = Temp.CreateDirectory();
            var srcDir = dir.CreateDirectory("src");
            var src = srcDir.CreateFile("Program.cs").WriteAllText("""
                class Program
                {
                    static void Main()
                    {
                        M();
                    }

                    public static void M() => throw null!;
                }
                """);

            // final path will look like:
            // 'TempDir/obj/{assemblyName}/{generatorName}/Generated.cs'
            // Note that generator will have access to the full path of the generated file, before adding it to the compilation
            // additionally we plan to add public API to determine a "correct relative path" to use here without any additional tricks
            var generatedSource = """
                using System.Runtime.CompilerServices;
                using System;

                namespace Generated
                {
                    static class Interceptors
                    {
                        [InterceptsLocation("../../../src/Program.cs", 5, 9)]
                        public static void M1() => Console.Write(1);
                    }
                }

                namespace System.Runtime.CompilerServices
                {
                    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
                    public sealed class InterceptsLocationAttribute : Attribute
                    {
                        public InterceptsLocationAttribute(string filePath, int line, int character)
                        {
                        }
                    }
                }
                """;
            var generator = new SingleFileTestGenerator(generatedSource, "Generated.cs");
            var objDir = dir.CreateDirectory("obj");

            VerifyOutput(
                dir,
                src,
                includeCurrentAssemblyAsAnalyzerReference: false,
                additionalFlags: ["/langversion:preview",
                    $"/out:{objDir.Path}/embed.exe",
                    "/features:InterceptorsNamespaces=Generated",
                    "/warn:9"],
                expectedWarningCount: 1,
                generators: [generator],
                analyzers: null);
            ValidateWrittenSources([]);

            // Clean up temp files
            CleanupAllGeneratedFiles(src.Path);
            Directory.Delete(dir.Path, true);
        }

        [Theory]
        [InlineData("")]
        [InlineData($"/pathmap:DIRPATH=/_/")]
        [InlineData($"/pathmap:SRCDIRPATH=a/,OBJDIRPATH=b/")]
        public void Interceptors_RelativePath_GeneratedFiles_EndToEnd_GeneratedFilesOutSpecified(string pathMapArgument)
        {
            var dir = Temp.CreateDirectory();

            var srcDir = dir.CreateDirectory("src");
            var src = srcDir.CreateFile("Program.cs").WriteAllText("""
                class Program
                {
                    static void Main()
                    {
                        M();
                    }

                    public static void M() => throw null!;
                }
                """);

            var objDir = dir.CreateDirectory("obj");

            // final path will look like:
            // 'TempDir/obj/{assemblyName}/{generatorName}/Generated.cs'
            // Note that generator will have access to the full path of the generated file, before adding it to the compilation
            // additionally we plan to add public API to determine a "correct relative path" to use here without any additional tricks
            var generatedSource = """
                using System.Runtime.CompilerServices;
                using System;

                namespace Generated
                {
                    static class Interceptors
                    {
                        [InterceptsLocation("../../../src/Program.cs", 5, 9)]
                        public static void M1() => Console.Write(1);
                    }
                }

                namespace System.Runtime.CompilerServices
                {
                    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
                    public sealed class InterceptsLocationAttribute : Attribute
                    {
                        public InterceptsLocationAttribute(string filePath, int line, int character)
                        {
                        }
                    }
                }
                """;
            var generator = new SingleFileTestGenerator(generatedSource, "Generated.cs");

            pathMapArgument = pathMapArgument.Replace("DIRPATH", dir.Path).Replace("SRCDIRPATH", srcDir.Path).Replace("OBJDIRPATH", objDir.Path);
            VerifyOutput(
                dir,
                src,
                includeCurrentAssemblyAsAnalyzerReference: false,
                additionalFlags: [
                    "/generatedfilesout:" + objDir.Path,
                    "/langversion:preview",
                    "/out:embed.exe",
                    "/features:InterceptorsNamespaces=Generated",
                    "/warn:9",
                    .. string.IsNullOrEmpty(pathMapArgument) ? default(Span<string>) : [pathMapArgument]
                    ],
                expectedWarningCount: 1,
                generators: [generator],
                analyzers: null);

            var generatorPrefix = GeneratorDriver.GetFilePathPrefixForGenerator(objDir.Path, generator);
            ValidateWrittenSources(new()
            {
                { generatorPrefix, new() { { "Generated.cs", generatedSource } } },
            });

            // Clean up temp files
            CleanupAllGeneratedFiles(src.Path);
            Directory.Delete(dir.Path, true);
        }

        [Fact]
        [WorkItem(44087, "https://github.com/dotnet/roslyn/issues/44087")]
        public void SourceGeneratorsAndAnalyzerConfig()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"
class C
{
}");
            var analyzerConfig = dir.CreateFile(".editorconfig").WriteAllText(@"
[*.cs]
key = value");

            var generator = new SingleFileTestGenerator("public class D {}", "generated.cs");

            VerifyOutput(dir, src, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/analyzerconfig:" + analyzerConfig.Path }, generators: new[] { generator }, analyzers: null);
        }

        [Fact]
        public void SourceGeneratorsCanReadAnalyzerConfig()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"
class C
{
}");
            var analyzerConfig1 = dir.CreateFile(".globaleditorconfig").WriteAllText(@"
is_global = true
key1 = value1

[*.cs]
key2 = value2

[*.vb]
key3 = value3");

            var analyzerConfig2 = dir.CreateFile(".editorconfig").WriteAllText(@"
[*.cs]
key4 = value4

[*.vb]
key5 = value5");

            var subDir = dir.CreateDirectory("subDir");
            var analyzerConfig3 = subDir.CreateFile(".editorconfig").WriteAllText(@"
[*.cs]
key6 = value6

[*.vb]
key7 = value7");

            var generator = new CallbackGenerator((ic) => { }, (gc) =>
            {
                // can get the global options
                var globalOptions = gc.AnalyzerConfigOptions.GlobalOptions;
                Assert.True(globalOptions.TryGetValue("key1", out var keyValue));
                Assert.Equal("value1", keyValue);
                Assert.False(globalOptions.TryGetValue("key2", out _));
                Assert.False(globalOptions.TryGetValue("key3", out _));
                Assert.False(globalOptions.TryGetValue("key4", out _));
                Assert.False(globalOptions.TryGetValue("key5", out _));
                Assert.False(globalOptions.TryGetValue("key6", out _));
                Assert.False(globalOptions.TryGetValue("key7", out _));

                // can get the options for class C
                var classOptions = gc.AnalyzerConfigOptions.GetOptions(gc.Compilation.SyntaxTrees.First());
                Assert.True(classOptions.TryGetValue("key1", out keyValue));
                Assert.Equal("value1", keyValue);
                Assert.False(classOptions.TryGetValue("key2", out _));
                Assert.False(classOptions.TryGetValue("key3", out _));
                Assert.True(classOptions.TryGetValue("key4", out keyValue));
                Assert.Equal("value4", keyValue);
                Assert.False(classOptions.TryGetValue("key5", out _));
                Assert.False(classOptions.TryGetValue("key6", out _));
                Assert.False(classOptions.TryGetValue("key7", out _));
            });

            var args = new[] {
                "/analyzerconfig:" + analyzerConfig1.Path,
                "/analyzerconfig:" + analyzerConfig2.Path,
                "/analyzerconfig:" + analyzerConfig3.Path,
                "/t:library",
                src.Path
            };

            var cmd = CreateCSharpCompiler(null, dir.Path, args, generators: new[] { generator });
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = cmd.Run(outWriter);
            Assert.Equal(0, exitCode);

            // test for both the original tree and the generated one
            var provider = cmd.AnalyzerOptions.AnalyzerConfigOptionsProvider;

            // get the global options
            var globalOptions = provider.GlobalOptions;
            Assert.True(globalOptions.TryGetValue("key1", out var keyValue));
            Assert.Equal("value1", keyValue);
            Assert.False(globalOptions.TryGetValue("key2", out _));
            Assert.False(globalOptions.TryGetValue("key3", out _));
            Assert.False(globalOptions.TryGetValue("key4", out _));
            Assert.False(globalOptions.TryGetValue("key5", out _));
            Assert.False(globalOptions.TryGetValue("key6", out _));
            Assert.False(globalOptions.TryGetValue("key7", out _));

            // get the options for class C
            var classOptions = provider.GetOptions(cmd.Compilation.SyntaxTrees.First());
            Assert.True(classOptions.TryGetValue("key1", out keyValue));
            Assert.Equal("value1", keyValue);
            Assert.False(classOptions.TryGetValue("key2", out _));
            Assert.False(classOptions.TryGetValue("key3", out _));
            Assert.True(classOptions.TryGetValue("key4", out keyValue));
            Assert.Equal("value4", keyValue);
            Assert.False(classOptions.TryGetValue("key5", out _));
            Assert.False(classOptions.TryGetValue("key6", out _));
            Assert.False(classOptions.TryGetValue("key7", out _));

            // get the options for generated class D
            var generatedOptions = provider.GetOptions(cmd.Compilation.SyntaxTrees.Last());
            Assert.True(generatedOptions.TryGetValue("key1", out keyValue));
            Assert.Equal("value1", keyValue);
            Assert.False(generatedOptions.TryGetValue("key2", out _));
            Assert.False(generatedOptions.TryGetValue("key3", out _));
            Assert.True(classOptions.TryGetValue("key4", out keyValue));
            Assert.Equal("value4", keyValue);
            Assert.False(generatedOptions.TryGetValue("key5", out _));
            Assert.False(generatedOptions.TryGetValue("key6", out _));
            Assert.False(generatedOptions.TryGetValue("key7", out _));
        }

        internal class FailsExecuteGenerator : ISourceGenerator
        {
            public void Initialize(GeneratorInitializationContext context) { }
            public void Execute(GeneratorExecutionContext context) => throw new System.Exception("THROW");
        }

        [Fact, WorkItem(65313, "https://github.com/dotnet/roslyn/issues/65313")]
        public void FailedGeneratorExecuteWarning_AsError()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText("""
class C
{
}
""");

            var cmd = CreateCSharpCompiler(null, dir.Path,
                new[] { "/t:library", "/nologo", "/warnaserror+", src.Path },
                generators: new[] { new FailsExecuteGenerator() });
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = cmd.Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.StartsWith($"error CS8785: Generator 'FailsExecuteGenerator' failed to generate source. It will not contribute to the output and compilation errors may occur as a result. Exception was of type 'Exception' with message 'THROW'",
                outWriter.ToString());
        }

        [Fact, WorkItem(65313, "https://github.com/dotnet/roslyn/issues/65313")]
        public void FailedGeneratorExecuteWarning_Suppressed()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText("""
class C
{
}
""");

            var cmd = CreateCSharpCompiler(null, dir.Path,
                new[] { "/t:library", "/nologo", "/nowarn:CS8785", src.Path },
                generators: new[] { new FailsExecuteGenerator() });
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = cmd.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString());
        }

        internal class FailsInitializeGenerator : ISourceGenerator
        {
            public void Initialize(GeneratorInitializationContext context) => throw new System.Exception("THROW");
            public void Execute(GeneratorExecutionContext context) { }
        }

        [Fact, WorkItem(65313, "https://github.com/dotnet/roslyn/issues/65313")]
        public void FailedGeneratorInitializeWarning_AsError()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText("""
class C
{
}
""");

            var cmd = CreateCSharpCompiler(null, dir.Path,
                new[] { "/t:library", "/nologo", "/warnaserror+", src.Path },
                generators: new[] { new FailsInitializeGenerator() });
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = cmd.Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.StartsWith($"error CS8784: Generator 'FailsInitializeGenerator' failed to initialize. It will not contribute to the output and compilation errors may occur as a result. Exception was of type 'Exception' with message 'THROW'",
                outWriter.ToString());
        }

        [Fact, WorkItem(65313, "https://github.com/dotnet/roslyn/issues/65313")]
        public void FailedGeneratorInitializeWarning_Suppressed()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText("""
class C
{
}
""");

            var cmd = CreateCSharpCompiler(null, dir.Path,
                new[] { "/t:library", "/nologo", "/nowarn:CS8784", src.Path },
                generators: new[] { new FailsInitializeGenerator() });
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = cmd.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString());
        }

        [Theory]
        [CombinatorialData]
        public void SourceGeneratorsRunRegardlessOfLanguageVersion(LanguageVersion version)
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"class C {}");
            var generator = new CallbackGenerator(i => { }, e => throw null);

            var output = VerifyOutput(dir, src, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/langversion:" + version.ToDisplayString() }, generators: new[] { generator }, expectedWarningCount: 1, expectedErrorCount: 1, expectedExitCode: 0);
            Assert.Contains("CS8785: Generator 'CallbackGenerator' failed to generate source.", output);
        }

        [Fact]
        [WorkItem(59209, "https://github.com/dotnet/roslyn/issues/59209")]
        public void SourceGenerators_Binary_Additional_File()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"
class C
{
}");

            var additionalFile = dir.CreateFile("temp.bin").WriteAllBytes(TestResources.NetFX.Minimal.mincorlib);

            var generatedSource = "public class D { }";
            var generator = new SingleFileTestGenerator(generatedSource, "generatedSource.cs");

            VerifyOutput(dir, src, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/additionalfile:" + additionalFile.Path, "/langversion:preview", "/out:embed.exe" }, generators: new[] { generator }, analyzers: null);

            // Clean up temp files
            CleanupAllGeneratedFiles(src.Path);
            Directory.Delete(dir.Path, true);
        }

        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        private sealed class FieldAnalyzer : DiagnosticAnalyzer
        {
            private static readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor("Id", "Title", "Message", "Category", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(_rule);

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSyntaxNodeAction(AnalyzeFieldDeclaration, SyntaxKind.FieldDeclaration);
            }

            private static void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context)
            {
            }
        }

        [Fact]
        [WorkItem(44000, "https://github.com/dotnet/roslyn/issues/44000")]
        public void TupleField_ForceComplete()
        {
            var source =
@"namespace System
{
    public struct ValueTuple<T1>
    {
        public T1 Item1;
        public ValueTuple(T1 item1)
        {
            Item1 = item1;
        }
    }
}";
            var srcFile = Temp.CreateFile().WriteAllText(source);
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = CreateCSharpCompiler(
                null,
                WorkingDirectory,
                new[] { "/nologo", "/t:library", srcFile.Path },
               analyzers: new[] { new FieldAnalyzer() }); // at least one analyzer required
            var exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            var output = outWriter.ToString();
            Assert.Empty(output);
            CleanupAllGeneratedFiles(srcFile.Path);
        }

        [Fact]
        public void GlobalAnalyzerConfigsAllowedInSameDir()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("test.cs").WriteAllText(@"
class C
{
    int _f;
}");
            var configText = @"
is_global = true
";

            var analyzerConfig1 = dir.CreateFile("analyzerconfig1").WriteAllText(configText);
            var analyzerConfig2 = dir.CreateFile("analyzerconfig2").WriteAllText(configText);

            var cmd = CreateCSharpCompiler(null, dir.Path, new[] {
                "/nologo",
                "/t:library",
                "/preferreduilang:en",
                "/analyzerconfig:" + analyzerConfig1.Path,
                "/analyzerconfig:" + analyzerConfig2.Path,
                src.Path
            });

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = cmd.Run(outWriter);
            Assert.Equal(0, exitCode);
        }

        [Fact]
        public void GlobalAnalyzerConfigMultipleSetKeys()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"
class C
{
}");
            var analyzerConfigFile = dir.CreateFile(".globalconfig");
            var analyzerConfig = analyzerConfigFile.WriteAllText(@"
is_global = true
global_level = 100
option1 = abc");

            var analyzerConfigFile2 = dir.CreateFile(".globalconfig2");
            var analyzerConfig2 = analyzerConfigFile2.WriteAllText(@"
is_global = true
global_level = 100
option1 = def");

            var output = VerifyOutput(dir, src, additionalFlags: new[] { "/analyzerconfig:" + analyzerConfig.Path + "," + analyzerConfig2.Path }, expectedWarningCount: 1, includeCurrentAssemblyAsAnalyzerReference: false);

            // warning MultipleGlobalAnalyzerKeys: Multiple global analyzer config files set the same key 'option1' in section 'Global Section'. It has been unset. Key was set by the following files: ...
            Assert.Contains("MultipleGlobalAnalyzerKeys:", output, StringComparison.Ordinal);
            Assert.Contains("'option1'", output, StringComparison.Ordinal);
            Assert.Contains("'Global Section'", output, StringComparison.Ordinal);

            analyzerConfig = analyzerConfigFile.WriteAllText(@"
is_global = true
global_level = 100
[/file.cs]
option1 = abc");

            analyzerConfig2 = analyzerConfigFile2.WriteAllText(@"
is_global = true
global_level = 100
[/file.cs]
option1 = def");

            output = VerifyOutput(dir, src, additionalFlags: new[] { "/analyzerconfig:" + analyzerConfig.Path + "," + analyzerConfig2.Path }, expectedWarningCount: 1, includeCurrentAssemblyAsAnalyzerReference: false);

            // warning MultipleGlobalAnalyzerKeys: Multiple global analyzer config files set the same key 'option1' in section 'file.cs'. It has been unset. Key was set by the following files: ...
            Assert.Contains("MultipleGlobalAnalyzerKeys:", output, StringComparison.Ordinal);
            Assert.Contains("'option1'", output, StringComparison.Ordinal);
            Assert.Contains("'/file.cs'", output, StringComparison.Ordinal);
        }

        [Fact]
        public void GlobalAnalyzerConfigWithOptions()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("test.cs").WriteAllText(@"
class C
{
}");
            var additionalFile = dir.CreateFile("file.txt");
            var analyzerConfig = dir.CreateFile(".editorconfig").WriteAllText(@"
[*.cs]
key1 = value1

[*.txt]
key2 = value2");

            var globalConfig = dir.CreateFile(".globalconfig").WriteAllText(@"
is_global = true
key3 = value3");

            var cmd = CreateCSharpCompiler(null, dir.Path, new[] {
                "/nologo",
                "/t:library",
                "/analyzerconfig:" + analyzerConfig.Path,
                "/analyzerconfig:" + globalConfig.Path,
                "/analyzer:" + Assembly.GetExecutingAssembly().Location,
                "/nowarn:8032,Warning01",
                "/additionalfile:" + additionalFile.Path,
                src.Path });

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = cmd.Run(outWriter);
            Assert.Equal("", outWriter.ToString());
            Assert.Equal(0, exitCode);

            var comp = cmd.Compilation;
            var tree = comp.SyntaxTrees.Single();

            var provider = cmd.AnalyzerOptions.AnalyzerConfigOptionsProvider;
            var options = provider.GetOptions(tree);
            Assert.NotNull(options);
            Assert.True(options.TryGetValue("key1", out string val));
            Assert.Equal("value1", val);
            Assert.False(options.TryGetValue("key2", out _));
            Assert.True(options.TryGetValue("key3", out val));
            Assert.Equal("value3", val);

            options = provider.GetOptions(cmd.AnalyzerOptions.AdditionalFiles.Single());
            Assert.NotNull(options);
            Assert.False(options.TryGetValue("key1", out _));
            Assert.True(options.TryGetValue("key2", out val));
            Assert.Equal("value2", val);
            Assert.True(options.TryGetValue("key3", out val));
            Assert.Equal("value3", val);

            options = provider.GlobalOptions;
            Assert.NotNull(options);
            Assert.False(options.TryGetValue("key1", out _));
            Assert.False(options.TryGetValue("key2", out _));
            Assert.True(options.TryGetValue("key3", out val));
            Assert.Equal("value3", val);
        }

        [Fact]
        [WorkItem(44087, "https://github.com/dotnet/roslyn/issues/44804")]
        public void GlobalAnalyzerConfigDiagnosticOptionsCanBeOverridenByCommandLine()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"
class C
{
    void M()
    {
label1:;
    }
}");
            var globalConfig = dir.CreateFile(".globalconfig").WriteAllText(@"
is_global = true
dotnet_diagnostic.CS0164.severity = error;
");

            var analyzerConfig = dir.CreateFile(".editorconfig").WriteAllText(@"
[*.cs]
dotnet_diagnostic.CS0164.severity = warning;
");
            var none = Array.Empty<TempFile>();
            var globalOnly = new[] { globalConfig };
            var globalAndSpecific = new[] { globalConfig, analyzerConfig };

            // by default a warning, which can be suppressed via cmdline
            verify(configs: none, expectedWarnings: 1);
            verify(configs: none, noWarn: "CS0164", expectedWarnings: 0);

            // the global analyzer config ups the warning to an error, but the cmdline setting overrides it
            verify(configs: globalOnly, expectedErrors: 1);
            verify(configs: globalOnly, noWarn: "CS0164", expectedWarnings: 0);
            verify(configs: globalOnly, noWarn: "164", expectedWarnings: 0); // cmdline can be shortened, but still works

            // the editor config downgrades the error back to warning, but the cmdline setting overrides it
            verify(configs: globalAndSpecific, expectedWarnings: 1);
            verify(configs: globalAndSpecific, noWarn: "CS0164", expectedWarnings: 0);

            void verify(TempFile[] configs, int expectedWarnings = 0, int expectedErrors = 0, string noWarn = "0")
                => VerifyOutput(dir, src,
                                expectedErrorCount: expectedErrors,
                                expectedWarningCount: expectedWarnings,
                                includeCurrentAssemblyAsAnalyzerReference: false,
                                analyzers: null,
                                additionalFlags: configs.SelectAsArray(c => "/analyzerconfig:" + c.Path)
                                                         .Add("/noWarn:" + noWarn).ToArray());
        }

        [Fact]
        [WorkItem(44087, "https://github.com/dotnet/roslyn/issues/44804")]
        public void GlobalAnalyzerConfigSpecificDiagnosticOptionsOverrideGeneralCommandLineOptions()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"
class C
{
    void M()
    {
label1:;
    }
}");
            var globalConfig = dir.CreateFile(".globalconfig").WriteAllText($@"
is_global = true
dotnet_diagnostic.CS0164.severity = none;
");

            VerifyOutput(dir, src, additionalFlags: new[] { "/warnaserror+", "/analyzerconfig:" + globalConfig.Path }, includeCurrentAssemblyAsAnalyzerReference: false);
        }

        [Theory, CombinatorialData]
        [WorkItem(43051, "https://github.com/dotnet/roslyn/issues/43051")]
        public void WarnAsErrorIsRespectedForForWarningsConfiguredInRulesetOrGlobalConfig(bool useGlobalConfig)
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"
class C
{
    void M()
    {
label1:;
    }
}");
            var additionalFlags = new[] { "/warnaserror+" };
            if (useGlobalConfig)
            {
                var globalConfig = dir.CreateFile(".globalconfig").WriteAllText($@"
is_global = true
dotnet_diagnostic.CS0164.severity = warning;
");
                additionalFlags = additionalFlags.Append("/analyzerconfig:" + globalConfig.Path).ToArray();
            }
            else
            {
                string ruleSetSource = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""15.0"">
  <Rules AnalyzerId=""Compiler"" RuleNamespace=""Compiler"">
    <Rule Id=""CS0164"" Action=""Warning"" />
  </Rules>
</RuleSet>
";
                _ = dir.CreateFile("Rules.ruleset").WriteAllText(ruleSetSource);
                additionalFlags = additionalFlags.Append("/ruleset:Rules.ruleset").ToArray();
            }

            VerifyOutput(dir, src, additionalFlags: additionalFlags, expectedErrorCount: 1, includeCurrentAssemblyAsAnalyzerReference: false);
        }

        [Fact]
        [WorkItem(44087, "https://github.com/dotnet/roslyn/issues/44804")]
        public void GlobalAnalyzerConfigSectionsDoNotOverrideCommandLine()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(@"
class C
{
    void M()
    {
label1:;
    }
}");
            var globalConfig = dir.CreateFile(".globalconfig").WriteAllText($@"
is_global = true

[{PathUtilities.NormalizeWithForwardSlash(src.Path)}]
dotnet_diagnostic.CS0164.severity = error;
");

            VerifyOutput(dir, src, additionalFlags: new[] { "/nowarn:0164", "/analyzerconfig:" + globalConfig.Path }, expectedErrorCount: 0, includeCurrentAssemblyAsAnalyzerReference: false);
        }

        [Fact]
        [WorkItem(44087, "https://github.com/dotnet/roslyn/issues/44804")]
        public void GlobalAnalyzerConfigCanSetDiagnosticWithNoLocation()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("test.cs").WriteAllText(@"
class C
{
}");
            var globalConfig = dir.CreateFile(".globalconfig").WriteAllText(@"
is_global = true
dotnet_diagnostic.Warning01.severity = error;
");

            VerifyOutput(dir, src, additionalFlags: new[] { "/analyzerconfig:" + globalConfig.Path }, expectedErrorCount: 1, includeCurrentAssemblyAsAnalyzerReference: false, analyzers: new[] { new WarningDiagnosticAnalyzer() });

            VerifyOutput(dir, src, additionalFlags: new[] { "/nowarn:Warning01", "/analyzerconfig:" + globalConfig.Path }, includeCurrentAssemblyAsAnalyzerReference: false, analyzers: new[] { new WarningDiagnosticAnalyzer() });
        }

        [Theory, CombinatorialData]
        public void TestAdditionalFileAnalyzer(bool registerFromInitialize)
        {
            var srcDirectory = Temp.CreateDirectory();

            var source = "class C { }";
            var srcFile = srcDirectory.CreateFile("a.cs");
            srcFile.WriteAllText(source);

            var additionalText = "Additional Text";
            var additionalFile = srcDirectory.CreateFile("b.txt");
            additionalFile.WriteAllText(additionalText);

            var diagnosticSpan = new TextSpan(2, 2);
            var analyzer = new AdditionalFileAnalyzer(registerFromInitialize, diagnosticSpan);

            var output = VerifyOutput(srcDirectory, srcFile, expectedWarningCount: 1, includeCurrentAssemblyAsAnalyzerReference: false,
                additionalFlags: new[] { "/additionalfile:" + additionalFile.Path },
                analyzers: new[] { analyzer });
            Assert.Contains("b.txt(1,3): warning ID0001", output, StringComparison.Ordinal);

            CleanupAllGeneratedFiles(srcDirectory.Path);
        }

        [Theory]
        // "/warnaserror" tests
        [InlineData(/*analyzerConfigSeverity*/"warning", "/warnaserror", /*expectError*/true, /*expectWarning*/false)]
        [InlineData(/*analyzerConfigSeverity*/"error", "/warnaserror", /*expectError*/true, /*expectWarning*/false)]
        [InlineData(/*analyzerConfigSeverity*/null, "/warnaserror", /*expectError*/true, /*expectWarning*/false)]
        // "/warnaserror:CS0169" tests
        [InlineData(/*analyzerConfigSeverity*/"warning", "/warnaserror:CS0169", /*expectError*/true, /*expectWarning*/false)]
        [InlineData(/*analyzerConfigSeverity*/"error", "/warnaserror:CS0169", /*expectError*/true, /*expectWarning*/false)]
        [InlineData(/*analyzerConfigSeverity*/null, "/warnaserror:CS0169", /*expectError*/true, /*expectWarning*/false)]
        // "/nowarn" tests
        [InlineData(/*analyzerConfigSeverity*/"warning", "/nowarn:CS0169", /*expectError*/false, /*expectWarning*/false)]
        [InlineData(/*analyzerConfigSeverity*/"error", "/nowarn:CS0169", /*expectError*/false, /*expectWarning*/false)]
        [InlineData(/*analyzerConfigSeverity*/null, "/nowarn:CS0169", /*expectError*/false, /*expectWarning*/false)]
        // Neither "/nowarn" nor "/warnaserror" tests
        [InlineData(/*analyzerConfigSeverity*/"warning", /*additionalArg*/null, /*expectError*/false, /*expectWarning*/true)]
        [InlineData(/*analyzerConfigSeverity*/"error", /*additionalArg*/null, /*expectError*/true, /*expectWarning*/false)]
        [InlineData(/*analyzerConfigSeverity*/null, /*additionalArg*/null, /*expectError*/false, /*expectWarning*/true)]
        [WorkItem(43051, "https://github.com/dotnet/roslyn/issues/43051")]
        public void TestCompilationOptionsOverrideAnalyzerConfig_CompilerWarning(string analyzerConfigSeverity, string additionalArg, bool expectError, bool expectWarning)
        {
            var src = @"
class C
{
    int _f;     // CS0169: unused field
}";
            TestCompilationOptionsOverrideAnalyzerConfigCore(src, diagnosticId: "CS0169", analyzerConfigSeverity, additionalArg, expectError, expectWarning);
        }

        [Theory]
        // "/warnaserror" tests
        [InlineData(/*analyzerConfigSeverity*/"warning", "/warnaserror", /*expectError*/true, /*expectWarning*/false)]
        [InlineData(/*analyzerConfigSeverity*/"error", "/warnaserror", /*expectError*/true, /*expectWarning*/false)]
        [InlineData(/*analyzerConfigSeverity*/null, "/warnaserror", /*expectError*/true, /*expectWarning*/false)]
        // "/warnaserror:DiagnosticId" tests
        [InlineData(/*analyzerConfigSeverity*/"warning", "/warnaserror:" + CompilationAnalyzerWithSeverity.DiagnosticId, /*expectError*/true, /*expectWarning*/false)]
        [InlineData(/*analyzerConfigSeverity*/"error", "/warnaserror:" + CompilationAnalyzerWithSeverity.DiagnosticId, /*expectError*/true, /*expectWarning*/false)]
        [InlineData(/*analyzerConfigSeverity*/null, "/warnaserror:" + CompilationAnalyzerWithSeverity.DiagnosticId, /*expectError*/true, /*expectWarning*/false)]
        // "/nowarn" tests
        [InlineData(/*analyzerConfigSeverity*/"warning", "/nowarn:" + CompilationAnalyzerWithSeverity.DiagnosticId, /*expectError*/false, /*expectWarning*/false)]
        [InlineData(/*analyzerConfigSeverity*/"error", "/nowarn:" + CompilationAnalyzerWithSeverity.DiagnosticId, /*expectError*/false, /*expectWarning*/false)]
        [InlineData(/*analyzerConfigSeverity*/null, "/nowarn:" + CompilationAnalyzerWithSeverity.DiagnosticId, /*expectError*/false, /*expectWarning*/false)]
        // Neither "/nowarn" nor "/warnaserror" tests
        [InlineData(/*analyzerConfigSeverity*/"warning", /*additionalArg*/null, /*expectError*/false, /*expectWarning*/true)]
        [InlineData(/*analyzerConfigSeverity*/"error", /*additionalArg*/null, /*expectError*/true, /*expectWarning*/false)]
        [InlineData(/*analyzerConfigSeverity*/null, /*additionalArg*/null, /*expectError*/false, /*expectWarning*/true)]
        [WorkItem(43051, "https://github.com/dotnet/roslyn/issues/43051")]
        public void TestCompilationOptionsOverrideAnalyzerConfig_AnalyzerWarning(string analyzerConfigSeverity, string additionalArg, bool expectError, bool expectWarning)
        {
            var analyzer = new CompilationAnalyzerWithSeverity(DiagnosticSeverity.Warning, configurable: true);
            var src = @"class C { }";
            TestCompilationOptionsOverrideAnalyzerConfigCore(src, CompilationAnalyzerWithSeverity.DiagnosticId, analyzerConfigSeverity, additionalArg, expectError, expectWarning, analyzer);
        }

        private void TestCompilationOptionsOverrideAnalyzerConfigCore(
            string source,
            string diagnosticId,
            string analyzerConfigSeverity,
            string additionalArg,
            bool expectError,
            bool expectWarning,
            params DiagnosticAnalyzer[] analyzers)
        {
            Assert.True(!expectError || !expectWarning);

            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("temp.cs").WriteAllText(source);

            var additionalArgs = Array.Empty<string>();
            if (analyzerConfigSeverity != null)
            {
                var analyzerConfig = dir.CreateFile(".editorconfig").WriteAllText($@"
[*.cs]
dotnet_diagnostic.{diagnosticId}.severity = {analyzerConfigSeverity}");

                additionalArgs = additionalArgs.Append("/analyzerconfig:" + analyzerConfig.Path).ToArray();
            }

            if (!string.IsNullOrEmpty(additionalArg))
            {
                additionalArgs = additionalArgs.Append(additionalArg);
            }

            var output = VerifyOutput(dir, src, includeCurrentAssemblyAsAnalyzerReference: false,
                                        additionalArgs,
                                        expectedErrorCount: expectError ? 1 : 0,
                                        expectedWarningCount: expectWarning ? 1 : 0,
                                        analyzers: analyzers);
            if (expectError)
            {
                Assert.Contains($"error {diagnosticId}", output);
            }
            else if (expectWarning)
            {
                Assert.Contains($"warning {diagnosticId}", output);
            }
            else
            {
                Assert.DoesNotContain(diagnosticId, output);
            }
        }

        [ConditionalFact(typeof(CoreClrOnly), Reason = "Can't load a coreclr targeting generator on net framework / mono")]
        public void TestGeneratorsCantTargetNetFramework()
        {
            var directory = Temp.CreateDirectory();
            var src = directory.CreateFile("test.cs").WriteAllText(@"
class C
{
}");

            // core
            var coreGenerator = emitGenerator(".NETCoreApp,Version=v5.0");
            VerifyOutput(directory, src, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/analyzer:" + coreGenerator });

            // netstandard
            var nsGenerator = emitGenerator(".NETStandard,Version=v2.0");
            VerifyOutput(directory, src, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/analyzer:" + nsGenerator });

            // no target
            var ntGenerator = emitGenerator(targetFramework: null);
            VerifyOutput(directory, src, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/analyzer:" + ntGenerator });

            // framework
            var frameworkGenerator = emitGenerator(".NETFramework,Version=v4.7.2");
            var output = VerifyOutput(directory, src, expectedWarningCount: 2, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/analyzer:" + frameworkGenerator });
            Assert.Contains("CS8850", output); // ref's net fx
            Assert.Contains("CS8033", output); // no analyzers in assembly

            // framework, suppressed
            output = VerifyOutput(directory, src, expectedWarningCount: 1, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/nowarn:CS8850", "/analyzer:" + frameworkGenerator });
            Assert.Contains("CS8033", output);
            VerifyOutput(directory, src, includeCurrentAssemblyAsAnalyzerReference: false, additionalFlags: new[] { "/nowarn:CS8850,CS8033", "/analyzer:" + frameworkGenerator });

            string emitGenerator(string targetFramework)
            {
                string targetFrameworkAttributeText = targetFramework is object
                                                        ? $"[assembly: System.Runtime.Versioning.TargetFramework(\"{targetFramework}\")]"
                                                        : string.Empty;

                string generatorSource = $@"
using Microsoft.CodeAnalysis;

{targetFrameworkAttributeText}

[Generator]
public class Generator : ISourceGenerator
{{
            public void Execute(GeneratorExecutionContext context) {{ }}
            public void Initialize(GeneratorInitializationContext context) {{ }}
 }}";

                var directory = Temp.CreateDirectory();

                var generatorPath = Path.Combine(directory.Path, "generator.dll");

                var compilation = CSharpCompilation.Create($"generator",
                                                           new[] { CSharpSyntaxTree.ParseText(generatorSource) },
                                                           TargetFrameworkUtil.GetReferences(TargetFramework.Standard, new[] { MetadataReference.CreateFromAssemblyInternal(typeof(ISourceGenerator).Assembly) }),
                                                           new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                compilation.VerifyDiagnostics();
                var result = compilation.Emit(generatorPath);
                Assert.True(result.Success);

                return generatorPath;
            }
        }

        [Theory]
        [InlineData("a.txt", "b.txt", 2)]
        [InlineData("a.txt", "a.txt", 1)]
        [InlineData("abc/a.txt", "def/a.txt", 2)]
        [InlineData("abc/a.txt", "abc/a.txt", 1)]
        [InlineData("abc/a.txt", "abc/../a.txt", 2)]
        [InlineData("abc/a.txt", "abc/./a.txt", 1)]
        [InlineData("abc/a.txt", "abc/../abc/a.txt", 1)]
        [InlineData("abc/a.txt", "abc/.././abc/a.txt", 1)]
        [InlineData("abc/a.txt", "./abc/a.txt", 1)]
        [InlineData("abc/a.txt", "../abc/../abc/a.txt", 2)]
        [InlineData("abc/a.txt", "./abc/../abc/a.txt", 1)]
        [InlineData("../abc/a.txt", "../abc/../abc/a.txt", 1)]
        [InlineData("../abc/a.txt", "../abc/a.txt", 1)]
        [InlineData("./abc/a.txt", "abc/a.txt", 1)]
        public void TestDuplicateAdditionalFiles(string additionalFilePath1, string additionalFilePath2, int expectedCount)
        {
            var srcDirectory = Temp.CreateDirectory();
            var srcFile = srcDirectory.CreateFile("a.cs").WriteAllText("class C { }");

            // make sure any parent or sub dirs exist too
            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(srcDirectory.Path, additionalFilePath1)));
            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(srcDirectory.Path, additionalFilePath2)));

            var additionalFile1 = srcDirectory.CreateFile(additionalFilePath1);
            var additionalFile2 = expectedCount == 2 ? srcDirectory.CreateFile(additionalFilePath2) : null;

            string path1 = additionalFile1.Path;
            string path2 = additionalFile2?.Path ?? Path.Combine(srcDirectory.Path, additionalFilePath2);

            int count = 0;
            var generator = new PipelineCallbackGenerator(ctx =>
            {
                ctx.RegisterSourceOutput(ctx.AdditionalTextsProvider, (spc, t) =>
                {
                    count++;
                });
            });

            var output = VerifyOutput(srcDirectory, srcFile, includeCurrentAssemblyAsAnalyzerReference: false,
                additionalFlags: new[] { "/additionalfile:" + path1, "/additionalfile:" + path2 },
                generators: new[] { generator.AsSourceGenerator() });

            Assert.Equal(expectedCount, count);

            CleanupAllGeneratedFiles(srcDirectory.Path);
        }

        [ConditionalTheory(typeof(WindowsOnly))]
        [InlineData("abc/a.txt", "abc\\a.txt", 1)]
        [InlineData("abc\\a.txt", "abc\\a.txt", 1)]
        [InlineData("abc/a.txt", "abc\\..\\a.txt", 2)]
        [InlineData("abc/a.txt", "abc\\..\\abc\\a.txt", 1)]
        [InlineData("abc/a.txt", "../abc\\../abc\\a.txt", 2)]
        [InlineData("abc/a.txt", "./abc\\../abc\\a.txt", 1)]
        [InlineData("../abc/a.txt", "../abc\\../abc\\a.txt", 1)]
        [InlineData("a.txt", "A.txt", 1)]
        [InlineData("abc/a.txt", "ABC\\a.txt", 1)]
        [InlineData("abc/a.txt", "ABC\\A.txt", 1)]
        public void TestDuplicateAdditionalFiles_Windows(string additionalFilePath1, string additionalFilePath2, int expectedCount) => TestDuplicateAdditionalFiles(additionalFilePath1, additionalFilePath2, expectedCount);

        [ConditionalTheory(typeof(LinuxOnly))]
        [InlineData("a.txt", "A.txt", 2)]
        [InlineData("abc/a.txt", "abc/A.txt", 2)]
        [InlineData("abc/a.txt", "ABC/a.txt", 2)]
        [InlineData("abc/a.txt", "./../abc/A.txt", 2)]
        [InlineData("abc/a.txt", "./../ABC/a.txt", 2)]
        public void TestDuplicateAdditionalFiles_Linux(string additionalFilePath1, string additionalFilePath2, int expectedCount) => TestDuplicateAdditionalFiles(additionalFilePath1, additionalFilePath2, expectedCount);

        [Fact, WorkItem(1434159, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1434159")]
        public void CanSuppressAnalyzerLoadWarning()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("test.cs").WriteAllText(@"
class C {} 
");
            var notAnalyzer = dir.CreateFile("random.txt");

            // not suppressed
            var output = VerifyOutput(dir, src, additionalFlags: new[] { "/analyzer:" + notAnalyzer.Path }, expectedWarningCount: 1, includeCurrentAssemblyAsAnalyzerReference: false);
            Assert.Contains("warning CS8034", output, StringComparison.Ordinal);

            // suppressed
            VerifyOutput(dir, src, additionalFlags: new[] { "/analyzer:" + notAnalyzer.Path, "/nowarn:CS8034" }, expectedWarningCount: 0, includeCurrentAssemblyAsAnalyzerReference: false);

            // elevated
            output = VerifyOutput(dir, src, additionalFlags: new[] { "/analyzer:" + notAnalyzer.Path, "/warnAsError:CS8034" }, expectedErrorCount: 1, includeCurrentAssemblyAsAnalyzerReference: false);
            Assert.Contains("error CS8034", output, StringComparison.Ordinal);
        }

        [Fact, WorkItem(1434159, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1434159")]
        public void GlobalAnalyzerConfigCanSuppressAnalyzerLoadWarning()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("test.cs").WriteAllText(@"
class C {} 
");
            var globalconfig = dir.CreateFile(".globalconfig").WriteAllText(@"
dotnet_diagnostic.CS8034.severity = none
");
            var notAnalyzer = dir.CreateFile("random.txt");

            // not suppressed
            var output = VerifyOutput(dir, src, additionalFlags: new[] { "/analyzer:" + notAnalyzer.Path }, expectedWarningCount: 1, includeCurrentAssemblyAsAnalyzerReference: false);
            Assert.Contains("warning CS8034", output, StringComparison.Ordinal);

            // suppressed via global analyzer config
            VerifyOutput(dir, src, additionalFlags: new[] { "/analyzer:" + notAnalyzer.Path, "/analyzerConfig:" + globalconfig.Path }, includeCurrentAssemblyAsAnalyzerReference: false);
        }

        [Fact]
        public void ExperimentalAttribute_SuppressedWithEditorConfig()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("test.cs").WriteAllText("""
C.M();

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    public sealed class ExperimentalAttribute : Attribute
    {
        public ExperimentalAttribute(string diagnosticId) { }

        public string UrlFormat { get; set; }
    }
}

[System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
public class C
{
    public static void M() { }
}
""");

            var analyzerConfig = dir.CreateFile(".editorconfig").WriteAllText("""
[*.cs]
dotnet_diagnostic.DiagID1.severity = none
""");
            // Without editorconfig
            var cmd = CreateCSharpCompiler(null, dir.Path, new[] {
                "/nologo",
                "/t:exe",
                "/preferreduilang:en",
                src.Path });

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = cmd.Run(outWriter);
            Assert.Equal(1, exitCode);

            // With editorconfig
            cmd = CreateCSharpCompiler(null, dir.Path, new[] {
                "/nologo",
                "/t:exe",
                "/preferreduilang:en",
                "/analyzerconfig:" + analyzerConfig.Path,
                src.Path });

            Assert.Equal(analyzerConfig.Path, Assert.Single(cmd.Arguments.AnalyzerConfigPaths));

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = cmd.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString());
        }

        [Fact]
        public void ExperimentalAttribute_SuppressedWithSpecificNoWarn()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("test.cs").WriteAllText("""
C.M();

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    public sealed class ExperimentalAttribute : Attribute
    {
        public ExperimentalAttribute(string diagnosticId) { }

        public string UrlFormat { get; set; }
    }
}

[System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
public class C
{
    public static void M() { }
}
""");

            // Without nowarn
            var cmd = CreateCSharpCompiler(null, dir.Path, new[] {
               "/nologo",
               "/t:exe",
               "/preferreduilang:en",
               src.Path });

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = cmd.Run(outWriter);
            Assert.Equal(1, exitCode);

            // With nowarn
            cmd = CreateCSharpCompiler(null, dir.Path, new[] {
               "/nologo",
               "/t:exe",
               "/preferreduilang:en",
               "/nowarn:DiagID1",
               src.Path });

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = cmd.Run(outWriter);
            Assert.Equal(0, exitCode);
        }

        [Fact]
        public void ExperimentalAttribute_SuppressedWithGlobalNoWarn()
        {
            var src = """
C.M();

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    public sealed class ExperimentalAttribute : Attribute
    {
        public ExperimentalAttribute(string diagnosticId) { }

        public string UrlFormat { get; set; }
    }
}

[System.Diagnostics.CodeAnalysis.Experimental("DiagID1")]
public class C
{
    public static void M() { }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe.WithGeneralDiagnosticOption(ReportDiagnostic.Suppress));
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ExperimentalAttributeWithMessage_SuppressedWithEditorConfig()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("test.cs").WriteAllText("""
C.M();

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    public sealed class ExperimentalAttribute : Attribute
    {
        public ExperimentalAttribute(string diagnosticId) { }

        public string UrlFormat { get; set; }
        public string Message { get; set; }
    }
}

[System.Diagnostics.CodeAnalysis.Experimental("DiagID1", Message = "use CCC")]
public class C
{
    public static void M() { }
}
""");

            var analyzerConfig = dir.CreateFile(".editorconfig").WriteAllText("""
[*.cs]
dotnet_diagnostic.DiagID1.severity = none
""");
            // Without editorconfig
            var cmd = CreateCSharpCompiler(null, dir.Path, new[] {
                "/nologo",
                "/t:exe",
                "/preferreduilang:en",
                src.Path });

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = cmd.Run(outWriter);
            Assert.Equal(1, exitCode);

            // With editorconfig
            cmd = CreateCSharpCompiler(null, dir.Path, new[] {
                "/nologo",
                "/t:exe",
                "/preferreduilang:en",
                "/analyzerconfig:" + analyzerConfig.Path,
                src.Path });

            Assert.Equal(analyzerConfig.Path, Assert.Single(cmd.Arguments.AnalyzerConfigPaths));

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = cmd.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString());
        }

        [Fact]
        public void ExperimentalAttributeWithMessage_SuppressedWithSpecificNoWarn()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("test.cs").WriteAllText("""
C.M();

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    public sealed class ExperimentalAttribute : Attribute
    {
        public ExperimentalAttribute(string diagnosticId) { }

        public string UrlFormat { get; set; }
        public string Message { get; set; }
    }
}

[System.Diagnostics.CodeAnalysis.Experimental("DiagID1", Message = "use CCC")]
public class C
{
    public static void M() { }
}
""");

            // Without nowarn
            var cmd = CreateCSharpCompiler(null, dir.Path, new[] {
               "/nologo",
               "/t:exe",
               "/preferreduilang:en",
               src.Path });

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = cmd.Run(outWriter);
            Assert.Equal(1, exitCode);

            // With nowarn
            cmd = CreateCSharpCompiler(null, dir.Path, new[] {
               "/nologo",
               "/t:exe",
               "/preferreduilang:en",
               "/nowarn:DiagID1",
               src.Path });

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = cmd.Run(outWriter);
            Assert.Equal(0, exitCode);
        }

        [Fact]
        public void ExperimentalAttributeWithMessage_SuppressedWithGlobalNoWarn()
        {
            var src = """
C.M();

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    public sealed class ExperimentalAttribute : Attribute
    {
        public ExperimentalAttribute(string diagnosticId) { }

        public string UrlFormat { get; set; }
        public string Message { get; set; }
    }
}

[System.Diagnostics.CodeAnalysis.Experimental("DiagID1", Message = "use CCC")]
public class C
{
    public static void M() { }
}
""";

            var comp = CreateCompilation(src, options: TestOptions.DebugExe.WithGeneralDiagnosticOption(ReportDiagnostic.Suppress));
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void ExperimentalWithWhitespaceDiagnosticID_WarnForInvalidDiagID()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("test.cs").WriteAllText("""
C.M();

[System.Diagnostics.CodeAnalysis.Experimental(" ")]
public class C
{
    public static void M() { }
}

namespace System.Diagnostics.CodeAnalysis
{
    public sealed class ExperimentalAttribute : Attribute
    {
        public ExperimentalAttribute(string diagnosticId) { }
    }
}
""");
            var analyzerConfig = dir.CreateFile(".editorconfig").WriteAllText("""
[*.cs]
dotnet_diagnostic.CS9211.severity = warning
""");
            Assert.Equal((ErrorCode)9211, ErrorCode.ERR_InvalidExperimentalDiagID);
            var cmd = CreateCSharpCompiler(null, dir.Path, new[] {
                "/nologo",
                "/t:exe",
                "/preferreduilang:en",
                "/analyzerconfig:" + analyzerConfig.Path,
                src.Path });

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = cmd.Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.StartsWith("test.cs(3,47): error CS9211: The diagnosticId argument to the 'Experimental' attribute must be a valid identifier",
                outWriter.ToString());
        }

        [Fact]
        public void ExperimentalWithValidDiagnosticID_WarnForDiagID()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("test.cs").WriteAllText("""
C.M();

[System.Diagnostics.CodeAnalysis.Experimental("DiagID")]
public class C
{
    public static void M() { }
}

namespace System.Diagnostics.CodeAnalysis
{
    public sealed class ExperimentalAttribute : Attribute
    {
        public ExperimentalAttribute(string diagnosticId) { }
    }
}
""");
            var analyzerConfig = dir.CreateFile(".editorconfig").WriteAllText("""
[*.cs]
dotnet_diagnostic.DiagID.severity = warning
""");
            var cmd = CreateCSharpCompiler(null, dir.Path, new[] {
                "/nologo",
                "/t:exe",
                "/preferreduilang:en",
                "/analyzerconfig:" + analyzerConfig.Path,
                src.Path });

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = cmd.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.StartsWith("test.cs(1,1): warning DiagID: 'C' is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.",
                outWriter.ToString());
        }

        [Fact]
        public void ExperimentalWithValidDiagnosticID_WarnForExperimental()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("test.cs").WriteAllText("""
C.M();

[System.Diagnostics.CodeAnalysis.Experimental("DiagID")]
public class C
{
    public static void M() { }
}

namespace System.Diagnostics.CodeAnalysis
{
    public sealed class ExperimentalAttribute : Attribute
    {
        public ExperimentalAttribute(string diagnosticId) { }
    }
}
""");
            var analyzerConfig = dir.CreateFile(".editorconfig").WriteAllText("""
[*.cs]
dotnet_diagnostic.CS9204.severity = warning
""");
            Assert.Equal((ErrorCode)9204, ErrorCode.WRN_Experimental);
            var cmd = CreateCSharpCompiler(null, dir.Path, new[] {
                "/nologo",
                "/t:exe",
                "/preferreduilang:en",
                "/analyzerconfig:" + analyzerConfig.Path,
                src.Path });

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = cmd.Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.StartsWith("test.cs(1,1): error DiagID: 'C' is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.",
                outWriter.ToString());
        }
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    internal abstract class CompilationStartedAnalyzer : DiagnosticAnalyzer
    {
        public abstract override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
        public abstract void CreateAnalyzerWithinCompilation(CompilationStartAnalysisContext context);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(CreateAnalyzerWithinCompilation);
        }
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    internal class HiddenDiagnosticAnalyzer : CompilationStartedAnalyzer
    {
        internal static readonly DiagnosticDescriptor Hidden01 = new DiagnosticDescriptor("Hidden01", "", "Throwing a diagnostic for #region", "", DiagnosticSeverity.Hidden, isEnabledByDefault: true);
        internal static readonly DiagnosticDescriptor Hidden02 = new DiagnosticDescriptor("Hidden02", "", "Throwing a diagnostic for something else", "", DiagnosticSeverity.Hidden, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Hidden01, Hidden02);
            }
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            context.ReportDiagnostic(Diagnostic.Create(Hidden01, context.Node.GetLocation()));
        }

        public override void CreateAnalyzerWithinCompilation(CompilationStartAnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.RegionDirectiveTrivia);
        }
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    internal class InfoDiagnosticAnalyzer : CompilationStartedAnalyzer
    {
        internal static readonly DiagnosticDescriptor Info01 = new DiagnosticDescriptor("Info01", "", "Throwing a diagnostic for #pragma restore", "", DiagnosticSeverity.Info, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Info01);
            }
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if ((context.Node as PragmaWarningDirectiveTriviaSyntax).DisableOrRestoreKeyword.IsKind(SyntaxKind.RestoreKeyword))
            {
                context.ReportDiagnostic(Diagnostic.Create(Info01, context.Node.GetLocation()));
            }
        }

        public override void CreateAnalyzerWithinCompilation(CompilationStartAnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.PragmaWarningDirectiveTrivia);
        }
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    internal class WarningDiagnosticAnalyzer : CompilationStartedAnalyzer
    {
        internal static readonly DiagnosticDescriptor Warning01 = new DiagnosticDescriptor("Warning01", "", "Throwing a diagnostic for types declared", "", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Warning01);
            }
        }

        public override void CreateAnalyzerWithinCompilation(CompilationStartAnalysisContext context)
        {
            context.RegisterSymbolAction(
                (symbolContext) =>
                {
                    symbolContext.ReportDiagnostic(Diagnostic.Create(Warning01, symbolContext.Symbol.Locations.First()));
                },
                SymbolKind.NamedType);
        }
    }

    internal class WarningWithUrlDiagnosticAnalyzer : CompilationStartedAnalyzer
    {
        internal static readonly DiagnosticDescriptor Warning02 = new DiagnosticDescriptor("Warning02", "", "Throwing a diagnostic for types declared", "", DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: "https://example.org/analyzer");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Warning02);

        public override void CreateAnalyzerWithinCompilation(CompilationStartAnalysisContext context)
        {
            context.RegisterSymbolAction(
                static (symbolContext) =>
                {
                    symbolContext.ReportDiagnostic(Diagnostic.Create(Warning02, symbolContext.Symbol.Locations.First()));
                },
                SymbolKind.NamedType);
        }
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    internal class ErrorDiagnosticAnalyzer : CompilationStartedAnalyzer
    {
        internal static readonly DiagnosticDescriptor Error01 = new DiagnosticDescriptor("Error01", "", "Throwing a diagnostic for #pragma disable", "", DiagnosticSeverity.Error, isEnabledByDefault: true);
        internal static readonly DiagnosticDescriptor Error02 = new DiagnosticDescriptor("Error02", "", "Throwing a diagnostic for something else", "", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Error01, Error02);
            }
        }

        public override void CreateAnalyzerWithinCompilation(CompilationStartAnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(
                (nodeContext) =>
                {
                    if ((nodeContext.Node as PragmaWarningDirectiveTriviaSyntax).DisableOrRestoreKeyword.IsKind(SyntaxKind.DisableKeyword))
                    {
                        nodeContext.ReportDiagnostic(Diagnostic.Create(Error01, nodeContext.Node.GetLocation()));
                    }
                },
                SyntaxKind.PragmaWarningDirectiveTrivia
                );
        }
    }
}
