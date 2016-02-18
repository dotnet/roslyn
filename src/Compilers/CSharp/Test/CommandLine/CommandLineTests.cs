// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

using static Roslyn.Test.Utilities.SharedResourceHelpers;
using static Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers;

namespace Microsoft.CodeAnalysis.CSharp.CommandLine.UnitTests
{
    public class CommandLineTests : CSharpTestBase
    {
        private static readonly string s_CSharpCompilerExecutable = typeof(Microsoft.CodeAnalysis.CSharp.CommandLine.Csc).Assembly.Location;
        private static readonly string s_defaultSdkDirectory = RuntimeEnvironment.GetRuntimeDirectory();

        private readonly string _baseDirectory = TempRoot.Root;

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

            internal override IEnumerable<string> EnumerateFiles(string directory, string fileNamePattern, object searchOption)
            {
                var key = directory + "|" + fileNamePattern;
                if (searchOption == PortableShim.SearchOption.TopDirectoryOnly)
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

        private static CSharpCommandLineArguments DefaultParse(IEnumerable<string> args, string baseDirectory, string sdkDirectory = null, string additionalReferenceDirectories = null)
        {
            sdkDirectory = sdkDirectory ?? s_defaultSdkDirectory;
            return CSharpCommandLineParser.Default.Parse(args, baseDirectory, sdkDirectory, additionalReferenceDirectories);
        }

        private static CSharpCommandLineArguments ScriptParse(IEnumerable<string> args, string baseDirectory)
        {
            return CSharpCommandLineParser.ScriptRunner.Parse(args, baseDirectory, s_defaultSdkDirectory);
        }

        private static CSharpCommandLineArguments FullParse(string commandLine, string baseDirectory, string sdkDirectory = null, string additionalReferenceDirectories = null)
        {
            sdkDirectory = sdkDirectory ?? s_defaultSdkDirectory;
            var args = CommandLineParser.SplitCommandLineIntoArguments(commandLine, removeHashComments: true);
            return CSharpCommandLineParser.Default.Parse(args, baseDirectory, sdkDirectory, additionalReferenceDirectories);
        }

        // This test should only run when the machine's default encoding is shift-JIS
        [ConditionalFact(typeof(HasShiftJisDefaultEncoding))]
        public void CompileShiftJisOnShiftJis()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("sjis.cs").WriteAllBytes(TestResources.General.ShiftJisSource);

            var cmd = new MockCSharpCompiler(null, dir.Path, new[] { "/nologo", src.Path });

            Assert.Null(cmd.Arguments.Encoding);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = cmd.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString());

            var result = ProcessUtilities.Run(Path.Combine(dir.Path, "sjis.exe"), arguments: "", workingDirectory: dir.Path);
            Assert.Equal(0, result.ExitCode);
            Assert.Equal("星野 八郎太", File.ReadAllText(Path.Combine(dir.Path, "output.txt"), Encoding.GetEncoding(932)));
        }

        [Fact]
        public void RunWithShiftJisFile()
        {
            var dir = Temp.CreateDirectory();
            var src = dir.CreateFile("sjis.cs").WriteAllBytes(TestResources.General.ShiftJisSource);

            var cmd = new MockCSharpCompiler(null, dir.Path, new[] { "/nologo", "/codepage:932", src.Path });

            Assert.Equal(932, cmd.Arguments.Encoding?.WindowsCodePage);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = cmd.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString());

            var result = ProcessUtilities.Run(Path.Combine(dir.Path, "sjis.exe"), arguments: "", workingDirectory: dir.Path);
            Assert.Equal(0, result.ExitCode);
            Assert.Equal("星野 八郎太", File.ReadAllText(Path.Combine(dir.Path, "output.txt"), Encoding.GetEncoding(932)));
        }

        [Fact]
        [WorkItem(946954, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/946954")]
        public void CompilerBinariesAreAnyCPU()
        {
            Assert.Equal(ProcessorArchitecture.MSIL, AssemblyName.GetAssemblyName(s_CSharpCompilerExecutable).ProcessorArchitecture);
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
            var cmd = new MockCSharpCompiler(rsp, _baseDirectory, new[] { "b.cs" });

            cmd.Arguments.Errors.Verify(
                // error CS2001: Source file 'System.Console.WriteLine(*?);' could not be found
                Diagnostic(ErrorCode.ERR_FileNotFound).WithArguments("System.Console.WriteLine(*?);"));

            AssertEx.Equal(new[] { "System.dll" }, cmd.Arguments.MetadataReferences.Select(r => r.Reference));
            AssertEx.Equal(new[] { Path.Combine(_baseDirectory, "a.cs"), Path.Combine(_baseDirectory, "b.cs") }, cmd.Arguments.SourceFiles.Select(file => file.Path));

            CleanupAllGeneratedFiles(rsp);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void ResponseFiles_RelativePaths()
        {
            var parentDir = Temp.CreateDirectory();
            var baseDir = parentDir.CreateDirectory("temp");
            var dirX = baseDir.CreateDirectory("x");
            var dirAB = baseDir.CreateDirectory("a b");
            var dirSubDir = baseDir.CreateDirectory("subdir");
            var dirFoo = parentDir.CreateDirectory("foo");
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
/libpaths:..\foo;../bar;""a b""
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

            var args = parser.Parse(new[] { "first.cs", "second.cs", "@a.rsp", "last.cs" }, basePath, s_defaultSdkDirectory);
            args.Errors.Verify();
            Assert.False(args.IsScriptRunner);

            string[] resolvedSourceFiles = args.SourceFiles.Select(f => f.Path).ToArray();
            string[] references = args.MetadataReferences.Select(r => r.Reference).ToArray();

            AssertEx.Equal(new[] { "first.cs", "second.cs", "b.cs", "a.cs", "c.cs", "d.cs", "last.cs" }.Select(prependBasePath), resolvedSourceFiles);
            AssertEx.Equal(new[] { typeof(object).Assembly.Location, @"..\v4.0.30319\System.dll", @".\System.Data.dll" }, references);
            AssertEx.Equal(new[] { RuntimeEnvironment.GetRuntimeDirectory() }.Concat(new[] { @"x", @"..\foo", @"../bar", @"a b" }.Select(prependBasePath)), args.ReferencePaths.ToArray());
            Assert.Equal(basePath, args.BaseDirectory);
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
                    // TODO (tomat): Fix PathUtilities.GetDirectoryName to strip trailing \ and then the key should be @"C:\temp\a|*.cs"
                    { @"C:\temp\a\|*.cs", new[] { @"a\x.cs", @"a\b\b.cs", @"a\c.cs" } },
                });

            var args = parser.Parse(new[] { @"*.cs", @"/recurse:a\*.cs" }, @"C:\temp", s_defaultSdkDirectory);
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
            int exitCode = new MockCSharpCompiler(null, folder.Path, new[] { "/nologo", "/preferreduilang:en", "/t:library", @"/recurse:.", "/out:abc.dll" }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("warning CS2008: No source files specified.", outWriter.ToString().Trim());

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = new MockCSharpCompiler(null, folder.Path, new[] { "/nologo", "/preferreduilang:en", "/t:library", @"/recurse:.  ", "/out:abc.dll" }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("warning CS2008: No source files specified.", outWriter.ToString().Trim());

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = new MockCSharpCompiler(null, folder.Path, new[] { "/nologo", "/preferreduilang:en", "/t:library", @"/recurse:  .  ", "/out:abc.dll" }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("warning CS2008: No source files specified.", outWriter.ToString().Trim());

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = new MockCSharpCompiler(null, folder.Path, new[] { "/nologo", "/preferreduilang:en", "/t:library", @"/recurse:././.", "/out:abc.dll" }).Run(outWriter);
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
            var args = DefaultParse(new[] { @"e:c:\test\test.cs", "/t:library" }, _baseDirectory);
            Assert.Equal(3, args.Errors.Length);
            Assert.Equal((int)ErrorCode.FTL_InputFileNameTooLong, args.Errors[0].Code);
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

            var parsedArgs = DefaultParse(args, _baseDirectory);
            var compilation = CreateCompilationWithMscorlib(new SyntaxTree[0]);
            IEnumerable<DiagnosticInfo> errors;
            CSharpCompiler.GetWin32ResourcesInternal(MessageProvider.Instance, parsedArgs, compilation, out errors);
            Assert.Equal(1, errors.Count());
            Assert.Equal((int)ErrorCode.ERR_CantOpenWin32Manifest, errors.First().Code);
            Assert.Equal(2, errors.First().Arguments.Count());

            args = new string[]
            {
                @"/Win32icon:\bogus"
            };

            parsedArgs = DefaultParse(args, _baseDirectory);

            CSharpCompiler.GetWin32ResourcesInternal(MessageProvider.Instance, parsedArgs, compilation, out errors);
            Assert.Equal(1, errors.Count());
            Assert.Equal((int)ErrorCode.ERR_CantOpenIcon, errors.First().Code);
            Assert.Equal(2, errors.First().Arguments.Count());

            args = new string[]
            {
                @"/Win32Res:\bogus"
            };

            parsedArgs = DefaultParse(args, _baseDirectory);
            CSharpCompiler.GetWin32ResourcesInternal(MessageProvider.Instance, parsedArgs, compilation, out errors);
            Assert.Equal(1, errors.Count());
            Assert.Equal((int)ErrorCode.ERR_CantOpenWin32Res, errors.First().Code);
            Assert.Equal(2, errors.First().Arguments.Count());

            args = new string[]
            {
                @"/Win32Res:foo.win32data:bar.win32data2"
            };

            parsedArgs = DefaultParse(args, _baseDirectory);
            CSharpCompiler.GetWin32ResourcesInternal(MessageProvider.Instance, parsedArgs, compilation, out errors);
            Assert.Equal(1, errors.Count());
            Assert.Equal((int)ErrorCode.ERR_CantOpenWin32Res, errors.First().Code);
            Assert.Equal(2, errors.First().Arguments.Count());

            args = new string[]
            {
                @"/Win32icon:foo.win32data:bar.win32data2"
            };

            parsedArgs = DefaultParse(args, _baseDirectory);
            CSharpCompiler.GetWin32ResourcesInternal(MessageProvider.Instance, parsedArgs, compilation, out errors);
            Assert.Equal(1, errors.Count());
            Assert.Equal((int)ErrorCode.ERR_CantOpenIcon, errors.First().Code);
            Assert.Equal(2, errors.First().Arguments.Count());

            args = new string[]
            {
                @"/Win32manifest:foo.win32data:bar.win32data2"
            };

            parsedArgs = DefaultParse(args, _baseDirectory);
            CSharpCompiler.GetWin32ResourcesInternal(MessageProvider.Instance, parsedArgs, compilation, out errors);
            Assert.Equal(1, errors.Count());
            Assert.Equal((int)ErrorCode.ERR_CantOpenWin32Manifest, errors.First().Code);
            Assert.Equal(2, errors.First().Arguments.Count());
        }

        [Fact]
        public void Win32ResConflicts()
        {
            var parsedArgs = DefaultParse(new[] { "/win32res:foo", "/win32icon:foob", "a.cs" }, _baseDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.ERR_CantHaveWin32ResAndIcon, parsedArgs.Errors.First().Code);

            parsedArgs = DefaultParse(new[] { "/win32res:foo", "/win32manifest:foob", "a.cs" }, _baseDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.ERR_CantHaveWin32ResAndManifest, parsedArgs.Errors.First().Code);

            parsedArgs = DefaultParse(new[] { "/win32res:", "a.cs" }, _baseDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.ERR_NoFileSpec, parsedArgs.Errors.First().Code);
            Assert.Equal(1, parsedArgs.Errors.First().Arguments.Count);

            parsedArgs = DefaultParse(new[] { "/win32Icon: ", "a.cs" }, _baseDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.ERR_NoFileSpec, parsedArgs.Errors.First().Code);
            Assert.Equal(1, parsedArgs.Errors.First().Arguments.Count);

            parsedArgs = DefaultParse(new[] { "/win32Manifest:", "a.cs" }, _baseDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.ERR_NoFileSpec, parsedArgs.Errors.First().Code);
            Assert.Equal(1, parsedArgs.Errors.First().Arguments.Count);

            parsedArgs = DefaultParse(new[] { "/win32Manifest:foo", "/noWin32Manifest", "a.cs" }, _baseDirectory);
            Assert.Equal(0, parsedArgs.Errors.Length);
            Assert.True(parsedArgs.NoWin32Manifest);
            Assert.Equal(null, parsedArgs.Win32Manifest);
        }

        [Fact]
        public void Win32ResInvalid()
        {
            var parsedArgs = DefaultParse(new[] { "/win32res", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("/win32res"));

            parsedArgs = DefaultParse(new[] { "/win32res+", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/win32res+"));

            parsedArgs = DefaultParse(new[] { "/win32icon", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("/win32icon"));

            parsedArgs = DefaultParse(new[] { "/win32icon+", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/win32icon+"));

            parsedArgs = DefaultParse(new[] { "/win32manifest", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("/win32manifest"));

            parsedArgs = DefaultParse(new[] { "/win32manifest+", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/win32manifest+"));
        }

        [Fact]
        public void Win32IconContainsGarbage()
        {
            string tmpFileName = Temp.CreateFile().WriteAllBytes(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }).Path;

            var parsedArgs = DefaultParse(new[] { "/win32icon:" + tmpFileName, "a.cs" }, _baseDirectory);
            var compilation = CreateCompilationWithMscorlib(new SyntaxTree[0]);
            IEnumerable<DiagnosticInfo> errors;

            CSharpCompiler.GetWin32ResourcesInternal(MessageProvider.Instance, parsedArgs, compilation, out errors);
            Assert.Equal(1, errors.Count());
            Assert.Equal((int)ErrorCode.ERR_ErrorBuildingWin32Resources, errors.First().Code);
            Assert.Equal(1, errors.First().Arguments.Count());

            CleanupAllGeneratedFiles(tmpFileName);
        }

        [Fact]
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

        [ConditionalFact(typeof(WindowsOnly))]
        public void ParseResources()
        {
            var diags = new List<Diagnostic>();

            ResourceDescription desc = CSharpCommandLineParser.ParseResourceDescription("", @"\somepath\someFile.foo.bar", _baseDirectory, diags, embedded: false);
            Assert.Equal(0, diags.Count);
            Assert.Equal(@"someFile.foo.bar", desc.FileName);
            Assert.Equal("someFile.foo.bar", desc.ResourceName);

            desc = CSharpCommandLineParser.ParseResourceDescription("", @"\somepath\someFile.foo.bar,someName", _baseDirectory, diags, embedded: false);
            Assert.Equal(0, diags.Count);
            Assert.Equal(@"someFile.foo.bar", desc.FileName);
            Assert.Equal("someName", desc.ResourceName);

            desc = CSharpCommandLineParser.ParseResourceDescription("", @"\somepath\s""ome Fil""e.foo.bar,someName", _baseDirectory, diags, embedded: false);
            Assert.Equal(0, diags.Count);
            Assert.Equal(@"some File.foo.bar", desc.FileName);
            Assert.Equal("someName", desc.ResourceName);

            desc = CSharpCommandLineParser.ParseResourceDescription("", @"\somepath\someFile.foo.bar,""some Name"",public", _baseDirectory, diags, embedded: false);
            Assert.Equal(0, diags.Count);
            Assert.Equal(@"someFile.foo.bar", desc.FileName);
            Assert.Equal("some Name", desc.ResourceName);
            Assert.True(desc.IsPublic);

            // Use file name in place of missing resource name.
            desc = CSharpCommandLineParser.ParseResourceDescription("", @"\somepath\someFile.foo.bar,,private", _baseDirectory, diags, embedded: false);
            Assert.Equal(0, diags.Count);
            Assert.Equal(@"someFile.foo.bar", desc.FileName);
            Assert.Equal("someFile.foo.bar", desc.ResourceName);
            Assert.False(desc.IsPublic);

            // Quoted accessibility is fine.
            desc = CSharpCommandLineParser.ParseResourceDescription("", @"\somepath\someFile.foo.bar,,""private""", _baseDirectory, diags, embedded: false);
            Assert.Equal(0, diags.Count);
            Assert.Equal(@"someFile.foo.bar", desc.FileName);
            Assert.Equal("someFile.foo.bar", desc.ResourceName);
            Assert.False(desc.IsPublic);

            // Leading commas are not ignored...
            desc = CSharpCommandLineParser.ParseResourceDescription("", @",,\somepath\someFile.foo.bar,,private", _baseDirectory, diags, embedded: false);
            diags.Verify(
                // error CS1906: Invalid option '\somepath\someFile.foo.bar'; Resource visibility must be either 'public' or 'private'
                Diagnostic(ErrorCode.ERR_BadResourceVis).WithArguments(@"\somepath\someFile.foo.bar"));
            diags.Clear();
            Assert.Null(desc);

            // ...even if there's whitespace between them.
            desc = CSharpCommandLineParser.ParseResourceDescription("", @", ,\somepath\someFile.foo.bar,,private", _baseDirectory, diags, embedded: false);
            diags.Verify(
                // error CS1906: Invalid option '\somepath\someFile.foo.bar'; Resource visibility must be either 'public' or 'private'
                Diagnostic(ErrorCode.ERR_BadResourceVis).WithArguments(@"\somepath\someFile.foo.bar"));
            diags.Clear();
            Assert.Null(desc);

            // Trailing commas are ignored...
            desc = CSharpCommandLineParser.ParseResourceDescription("", @"\somepath\someFile.foo.bar,,private", _baseDirectory, diags, embedded: false);
            diags.Verify();
            diags.Clear();
            Assert.Equal("someFile.foo.bar", desc.FileName);
            Assert.Equal("someFile.foo.bar", desc.ResourceName);
            Assert.False(desc.IsPublic);

            // ...even if there's whitespace between them.
            desc = CSharpCommandLineParser.ParseResourceDescription("", @"\somepath\someFile.foo.bar,,private, ,", _baseDirectory, diags, embedded: false);
            diags.Verify();
            diags.Clear();
            Assert.Equal("someFile.foo.bar", desc.FileName);
            Assert.Equal("someFile.foo.bar", desc.ResourceName);
            Assert.False(desc.IsPublic);

            desc = CSharpCommandLineParser.ParseResourceDescription("", @"\somepath\someFile.foo.bar,someName,publi", _baseDirectory, diags, embedded: false);
            diags.Verify(Diagnostic(ErrorCode.ERR_BadResourceVis).WithArguments("publi"));
            Assert.Null(desc);
            diags.Clear();

            desc = CSharpCommandLineParser.ParseResourceDescription("", @"D:rive\relative\path,someName,public", _baseDirectory, diags, embedded: false);
            diags.Verify(Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments(@"D:rive\relative\path"));
            Assert.Null(desc);
            diags.Clear();

            desc = CSharpCommandLineParser.ParseResourceDescription("", @"inva\l*d?path,someName,public", _baseDirectory, diags, embedded: false);
            diags.Verify(Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments(@"inva\l*d?path"));
            Assert.Null(desc);
            diags.Clear();

            desc = CSharpCommandLineParser.ParseResourceDescription("", null, _baseDirectory, diags, embedded: false);
            diags.Verify(Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments(""));
            Assert.Null(desc);
            diags.Clear();

            desc = CSharpCommandLineParser.ParseResourceDescription("", "", _baseDirectory, diags, embedded: false);
            diags.Verify(Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments(""));
            Assert.Null(desc);
            diags.Clear();

            desc = CSharpCommandLineParser.ParseResourceDescription("", " ", _baseDirectory, diags, embedded: false);
            diags.Verify(
                // error CS2021: File name ' ' contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments(" "));
            diags.Clear();
            Assert.Null(desc);

            desc = CSharpCommandLineParser.ParseResourceDescription("", " , ", _baseDirectory, diags, embedded: false);
            diags.Verify(
                // error CS2021: File name ' ' contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments(" "));
            diags.Clear();
            Assert.Null(desc);

            desc = CSharpCommandLineParser.ParseResourceDescription("", "path, ", _baseDirectory, diags, embedded: false);
            diags.Verify();
            diags.Clear();
            Assert.Equal("path", desc.FileName);
            Assert.Equal("path", desc.ResourceName);
            Assert.True(desc.IsPublic);

            desc = CSharpCommandLineParser.ParseResourceDescription("", " ,name", _baseDirectory, diags, embedded: false);
            diags.Verify(
                // error CS2021: File name ' ' contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments(" "));
            diags.Clear();
            Assert.Null(desc);

            desc = CSharpCommandLineParser.ParseResourceDescription("", " , , ", _baseDirectory, diags, embedded: false);
            diags.Verify(
                // error CS1906: Invalid option ' '; Resource visibility must be either 'public' or 'private'
                Diagnostic(ErrorCode.ERR_BadResourceVis).WithArguments(" "));
            diags.Clear();
            Assert.Null(desc);

            desc = CSharpCommandLineParser.ParseResourceDescription("", "path, , ", _baseDirectory, diags, embedded: false);
            diags.Verify(
                // error CS1906: Invalid option ' '; Resource visibility must be either 'public' or 'private'
                Diagnostic(ErrorCode.ERR_BadResourceVis).WithArguments(" "));
            diags.Clear();
            Assert.Null(desc);

            desc = CSharpCommandLineParser.ParseResourceDescription("", " ,name, ", _baseDirectory, diags, embedded: false);
            diags.Verify(
                // error CS1906: Invalid option ' '; Resource visibility must be either 'public' or 'private'
                Diagnostic(ErrorCode.ERR_BadResourceVis).WithArguments(" "));
            diags.Clear();
            Assert.Null(desc);

            desc = CSharpCommandLineParser.ParseResourceDescription("", " , ,private", _baseDirectory, diags, embedded: false);
            diags.Verify(
                // error CS2021: File name ' ' contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments(" "));
            diags.Clear();
            Assert.Null(desc);

            desc = CSharpCommandLineParser.ParseResourceDescription("", "path,name,", _baseDirectory, diags, embedded: false);
            diags.Verify(
                // CONSIDER: Dev10 actually prints "Invalid option '|'" (note the pipe)
                // error CS1906: Invalid option ''; Resource visibility must be either 'public' or 'private'
                Diagnostic(ErrorCode.ERR_BadResourceVis).WithArguments(""));
            diags.Clear();
            Assert.Null(desc);

            desc = CSharpCommandLineParser.ParseResourceDescription("", "path,name,,", _baseDirectory, diags, embedded: false);
            diags.Verify(
                // CONSIDER: Dev10 actually prints "Invalid option '|'" (note the pipe)
                // error CS1906: Invalid option ''; Resource visibility must be either 'public' or 'private'
                Diagnostic(ErrorCode.ERR_BadResourceVis).WithArguments(""));
            diags.Clear();
            Assert.Null(desc);

            desc = CSharpCommandLineParser.ParseResourceDescription("", "path,name, ", _baseDirectory, diags, embedded: false);
            diags.Verify(
                // error CS1906: Invalid option ''; Resource visibility must be either 'public' or 'private'
                Diagnostic(ErrorCode.ERR_BadResourceVis).WithArguments(" "));
            diags.Clear();
            Assert.Null(desc);

            desc = CSharpCommandLineParser.ParseResourceDescription("", "path, ,private", _baseDirectory, diags, embedded: false);
            diags.Verify();
            diags.Clear();
            Assert.Equal("path", desc.FileName);
            Assert.Equal("path", desc.ResourceName);
            Assert.False(desc.IsPublic);

            desc = CSharpCommandLineParser.ParseResourceDescription("", " ,name,private", _baseDirectory, diags, embedded: false);
            diags.Verify(
                // error CS2021: File name ' ' contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments(" "));
            diags.Clear();
            Assert.Null(desc);

            var longE = new String('e', 1024);

            desc = CSharpCommandLineParser.ParseResourceDescription("", String.Format("path,{0},private", longE), _baseDirectory, diags, embedded: false);
            diags.Verify(); // Now checked during emit.
            diags.Clear();
            Assert.Equal("path", desc.FileName);
            Assert.Equal(longE, desc.ResourceName);
            Assert.False(desc.IsPublic);

            var longI = new String('i', 260);

            desc = CSharpCommandLineParser.ParseResourceDescription("", String.Format("{0},e,private", longI), _baseDirectory, diags, embedded: false);
            diags.Verify(); // Now checked during emit.
            diags.Clear();
            Assert.Equal(longI, desc.FileName);
            Assert.Equal("e", desc.ResourceName);
            Assert.False(desc.IsPublic);
        }

        [Fact]
        public void ManagedResourceOptions()
        {
            CSharpCommandLineArguments parsedArgs;
            ResourceDescription resourceDescription;

            parsedArgs = DefaultParse(new[] { "/resource:a", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(false, parsedArgs.DisplayHelp);
            resourceDescription = parsedArgs.ManifestResources.Single();
            Assert.Null(resourceDescription.FileName); // since embedded
            Assert.Equal("a", resourceDescription.ResourceName);

            parsedArgs = DefaultParse(new[] { "/res:b", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(false, parsedArgs.DisplayHelp);
            resourceDescription = parsedArgs.ManifestResources.Single();
            Assert.Null(resourceDescription.FileName); // since embedded
            Assert.Equal("b", resourceDescription.ResourceName);

            parsedArgs = DefaultParse(new[] { "/linkresource:c", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(false, parsedArgs.DisplayHelp);
            resourceDescription = parsedArgs.ManifestResources.Single();
            Assert.Equal("c", resourceDescription.FileName);
            Assert.Equal("c", resourceDescription.ResourceName);

            parsedArgs = DefaultParse(new[] { "/linkres:d", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(false, parsedArgs.DisplayHelp);
            resourceDescription = parsedArgs.ManifestResources.Single();
            Assert.Equal("d", resourceDescription.FileName);
            Assert.Equal("d", resourceDescription.ResourceName);
        }

        [Fact]
        public void ManagedResourceOptions_SimpleErrors()
        {
            var parsedArgs = DefaultParse(new[] { "/resource:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("/resource:"));

            parsedArgs = DefaultParse(new[] { "/resource: ", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("/resource:"));

            parsedArgs = DefaultParse(new[] { "/res", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/res"));

            parsedArgs = DefaultParse(new[] { "/RES+", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/RES+"));

            parsedArgs = DefaultParse(new[] { "/res-:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/res-:"));

            parsedArgs = DefaultParse(new[] { "/linkresource:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("/linkresource:"));

            parsedArgs = DefaultParse(new[] { "/linkresource: ", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("/linkresource:"));

            parsedArgs = DefaultParse(new[] { "/linkres", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/linkres"));

            parsedArgs = DefaultParse(new[] { "/linkRES+", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/linkRES+"));

            parsedArgs = DefaultParse(new[] { "/linkres-:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/linkres-:"));
        }

        [Fact]
        public void Link_SimpleTests()
        {
            var parsedArgs = DefaultParse(new[] { "/link:a", "/link:b,,,,c", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            AssertEx.Equal(new[] { "a", "b", "c" },
                       parsedArgs.MetadataReferences.
                                  Where((res) => res.Properties.EmbedInteropTypes).
                                  Select((res) => res.Reference));

            parsedArgs = DefaultParse(new[] { "/Link: ,,, b ,,", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            AssertEx.Equal(new[] { " b " },
                           parsedArgs.MetadataReferences.
                                      Where((res) => res.Properties.EmbedInteropTypes).
                                      Select((res) => res.Reference));

            parsedArgs = DefaultParse(new[] { "/l:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("/l:"));

            parsedArgs = DefaultParse(new[] { "/L", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "/L"));

            parsedArgs = DefaultParse(new[] { "/l+", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/l+"));

            parsedArgs = DefaultParse(new[] { "/link-:", "a.cs" }, _baseDirectory);
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

            var parsedArgs = DefaultParse(new[] { "/recurse:" + dir.ToString() + "\\*.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            AssertEx.Equal(new[] { "{DIR}\\a.cs", "{DIR}\\b.cs", "{DIR}\\d2\\e.cs" },
                           parsedArgs.SourceFiles.Select((file) => file.Path.Replace(dir.ToString(), "{DIR}")));

            parsedArgs = DefaultParse(new[] { "*.cs" }, dir.ToString());
            parsedArgs.Errors.Verify();
            AssertEx.Equal(new[] { "{DIR}\\a.cs", "{DIR}\\b.cs" },
                           parsedArgs.SourceFiles.Select((file) => file.Path.Replace(dir.ToString(), "{DIR}")));

            parsedArgs = DefaultParse(new[] { "/reCURSE:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("/reCURSE:"));

            parsedArgs = DefaultParse(new[] { "/RECURSE: ", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("/RECURSE:"));

            parsedArgs = DefaultParse(new[] { "/recurse", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/recurse"));

            parsedArgs = DefaultParse(new[] { "/recurse+", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/recurse+"));

            parsedArgs = DefaultParse(new[] { "/recurse-:", "a.cs" }, _baseDirectory);
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
            var parsedArgs = DefaultParse(new[] { "/nostdlib", "/r:a", "/REFERENCE:b,,,,c", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            AssertEx.Equal(new[] { "a", "b", "c" },
                           parsedArgs.MetadataReferences.
                                      Where((res) => !res.Properties.EmbedInteropTypes).
                                      Select((res) => res.Reference));

            parsedArgs = DefaultParse(new[] { "/Reference: ,,, b ,,", "/nostdlib", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            AssertEx.Equal(new[] { " b " },
                           parsedArgs.MetadataReferences.
                                      Where((res) => !res.Properties.EmbedInteropTypes).
                                      Select((res) => res.Reference));

            parsedArgs = DefaultParse(new[] { "/Reference:a=b,,,", "/nostdlib", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal("a", parsedArgs.MetadataReferences.Single().Properties.Aliases.Single());
            Assert.Equal("b", parsedArgs.MetadataReferences.Single().Reference);

            parsedArgs = DefaultParse(new[] { "/r:a=b,,,c", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_OneAliasPerReference).WithArguments("b,,,c"));

            parsedArgs = DefaultParse(new[] { "/r:1=b", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadExternIdentifier).WithArguments("1"));

            parsedArgs = DefaultParse(new[] { "/r:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("/r:"));

            parsedArgs = DefaultParse(new[] { "/R", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "/R"));

            parsedArgs = DefaultParse(new[] { "/reference+", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/reference+"));

            parsedArgs = DefaultParse(new[] { "/reference-:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/reference-:"));
        }

        [Fact]
        public void Target_SimpleTests()
        {
            var parsedArgs = DefaultParse(new[] { "/target:exe", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(OutputKind.ConsoleApplication, parsedArgs.CompilationOptions.OutputKind);

            parsedArgs = DefaultParse(new[] { "/t:module", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(OutputKind.NetModule, parsedArgs.CompilationOptions.OutputKind);

            parsedArgs = DefaultParse(new[] { "/target:library", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(OutputKind.DynamicallyLinkedLibrary, parsedArgs.CompilationOptions.OutputKind);

            parsedArgs = DefaultParse(new[] { "/TARGET:winexe", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(OutputKind.WindowsApplication, parsedArgs.CompilationOptions.OutputKind);

            parsedArgs = DefaultParse(new[] { "/target:appcontainerexe", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(OutputKind.WindowsRuntimeApplication, parsedArgs.CompilationOptions.OutputKind);

            parsedArgs = DefaultParse(new[] { "/target:winmdobj", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(OutputKind.WindowsRuntimeMetadata, parsedArgs.CompilationOptions.OutputKind);

            parsedArgs = DefaultParse(new[] { "/target:winexe", "/T:exe", "/target:module", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(OutputKind.NetModule, parsedArgs.CompilationOptions.OutputKind);

            parsedArgs = DefaultParse(new[] { "/t", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/t"));

            parsedArgs = DefaultParse(new[] { "/target:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.FTL_InvalidTarget));

            parsedArgs = DefaultParse(new[] { "/target:xyz", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.FTL_InvalidTarget));

            parsedArgs = DefaultParse(new[] { "/T+", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/T+"));

            parsedArgs = DefaultParse(new[] { "/TARGET-:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/TARGET-:"));
        }

        [Fact]
        public void Target_SimpleTestsNoSource()
        {
            var parsedArgs = DefaultParse(new[] { "/target:exe"}, _baseDirectory);
            parsedArgs.Errors.Verify(
                // warning CS2008: No source files specified.
                Diagnostic(ErrorCode.WRN_NoSources).WithLocation(1, 1),
                // error CS1562: Outputs without source must have the /out option specified
                Diagnostic(ErrorCode.ERR_OutputNeedsName).WithLocation(1, 1) );
            Assert.Equal(OutputKind.ConsoleApplication, parsedArgs.CompilationOptions.OutputKind);

            parsedArgs = DefaultParse(new[] { "/t:module"}, _baseDirectory);
            parsedArgs.Errors.Verify(
                // warning CS2008: No source files specified.
                Diagnostic(ErrorCode.WRN_NoSources).WithLocation(1, 1),
                // error CS1562: Outputs without source must have the /out option specified
                Diagnostic(ErrorCode.ERR_OutputNeedsName).WithLocation(1, 1) );
            Assert.Equal(OutputKind.NetModule, parsedArgs.CompilationOptions.OutputKind);

            parsedArgs = DefaultParse(new[] { "/target:library"}, _baseDirectory);
            parsedArgs.Errors.Verify(
                // warning CS2008: No source files specified.
                Diagnostic(ErrorCode.WRN_NoSources).WithLocation(1, 1),
                // error CS1562: Outputs without source must have the /out option specified
                Diagnostic(ErrorCode.ERR_OutputNeedsName).WithLocation(1, 1) );
            Assert.Equal(OutputKind.DynamicallyLinkedLibrary, parsedArgs.CompilationOptions.OutputKind);

            parsedArgs = DefaultParse(new[] { "/TARGET:winexe"}, _baseDirectory);
            parsedArgs.Errors.Verify(
                // warning CS2008: No source files specified.
                Diagnostic(ErrorCode.WRN_NoSources).WithLocation(1, 1),
                // error CS1562: Outputs without source must have the /out option specified
                Diagnostic(ErrorCode.ERR_OutputNeedsName).WithLocation(1, 1) );
            Assert.Equal(OutputKind.WindowsApplication, parsedArgs.CompilationOptions.OutputKind);

            parsedArgs = DefaultParse(new[] { "/target:appcontainerexe"}, _baseDirectory);
            parsedArgs.Errors.Verify(
                // warning CS2008: No source files specified.
                Diagnostic(ErrorCode.WRN_NoSources).WithLocation(1, 1),
                // error CS1562: Outputs without source must have the /out option specified
                Diagnostic(ErrorCode.ERR_OutputNeedsName).WithLocation(1, 1) );
            Assert.Equal(OutputKind.WindowsRuntimeApplication, parsedArgs.CompilationOptions.OutputKind);

            parsedArgs = DefaultParse(new[] { "/target:winmdobj"}, _baseDirectory);
            parsedArgs.Errors.Verify(
                // warning CS2008: No source files specified.
                Diagnostic(ErrorCode.WRN_NoSources).WithLocation(1, 1),
                // error CS1562: Outputs without source must have the /out option specified
                Diagnostic(ErrorCode.ERR_OutputNeedsName).WithLocation(1, 1) );
            Assert.Equal(OutputKind.WindowsRuntimeMetadata, parsedArgs.CompilationOptions.OutputKind);

            parsedArgs = DefaultParse(new[] { "/target:winexe", "/T:exe", "/target:module"}, _baseDirectory);
            parsedArgs.Errors.Verify(
                // warning CS2008: No source files specified.
                Diagnostic(ErrorCode.WRN_NoSources).WithLocation(1, 1),
                // error CS1562: Outputs without source must have the /out option specified
                Diagnostic(ErrorCode.ERR_OutputNeedsName).WithLocation(1, 1) );
            Assert.Equal(OutputKind.NetModule, parsedArgs.CompilationOptions.OutputKind);

            parsedArgs = DefaultParse(new[] { "/t"}, _baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2007: Unrecognized option: '/t'
                Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/t").WithLocation(1, 1),
                // warning CS2008: No source files specified.
                Diagnostic(ErrorCode.WRN_NoSources).WithLocation(1, 1),
                // error CS1562: Outputs without source must have the /out option specified
                Diagnostic(ErrorCode.ERR_OutputNeedsName).WithLocation(1, 1) );

            parsedArgs = DefaultParse(new[] { "/target:"}, _baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2019: Invalid target type for /target: must specify 'exe', 'winexe', 'library', or 'module'
                Diagnostic(ErrorCode.FTL_InvalidTarget).WithLocation(1, 1),
                // warning CS2008: No source files specified.
                Diagnostic(ErrorCode.WRN_NoSources).WithLocation(1, 1),
                // error CS1562: Outputs without source must have the /out option specified
                Diagnostic(ErrorCode.ERR_OutputNeedsName).WithLocation(1, 1));

            parsedArgs = DefaultParse(new[] { "/target:xyz"}, _baseDirectory);
            parsedArgs.Errors.Verify(    
                // error CS2019: Invalid target type for /target: must specify 'exe', 'winexe', 'library', or 'module'
                Diagnostic(ErrorCode.FTL_InvalidTarget).WithLocation(1, 1),
                // warning CS2008: No source files specified.
                Diagnostic(ErrorCode.WRN_NoSources).WithLocation(1, 1),
                // error CS1562: Outputs without source must have the /out option specified
                Diagnostic(ErrorCode.ERR_OutputNeedsName).WithLocation(1, 1));

            parsedArgs = DefaultParse(new[] { "/T+"}, _baseDirectory);
            parsedArgs.Errors.Verify(    
                // error CS2007: Unrecognized option: '/T+'
                Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/T+").WithLocation(1, 1),
                // warning CS2008: No source files specified.
                Diagnostic(ErrorCode.WRN_NoSources).WithLocation(1, 1),
                // error CS1562: Outputs without source must have the /out option specified
                Diagnostic(ErrorCode.ERR_OutputNeedsName).WithLocation(1, 1));

            parsedArgs = DefaultParse(new[] { "/TARGET-:"}, _baseDirectory);
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
            CSharpCommandLineArguments args = DefaultParse(new[] { "/win32manifest:blah", "/target:module", "a.cs" }, _baseDirectory);
            args.Errors.Verify(
                // warning CS1927: Ignoring /win32manifest for module because it only applies to assemblies
                Diagnostic(ErrorCode.WRN_CantHaveManifestForModule));

            // Illegal, but not clobbered.
            Assert.Equal("blah", args.Win32Manifest);
        }

        [Fact]
        public void ArgumentParsing()
        {
            var parsedArgs = CSharpCommandLineParser.ScriptRunner.Parse(new[] { "a + b" }, _baseDirectory, s_defaultSdkDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(false, parsedArgs.DisplayHelp);
            Assert.Equal(true, parsedArgs.SourceFiles.Any());

            parsedArgs = CSharpCommandLineParser.ScriptRunner.Parse(new[] { "a + b; c" }, _baseDirectory, s_defaultSdkDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(false, parsedArgs.DisplayHelp);
            Assert.Equal(true, parsedArgs.SourceFiles.Any());

            parsedArgs = CSharpCommandLineParser.ScriptRunner.Parse(new[] { "/help" }, _baseDirectory, s_defaultSdkDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(true, parsedArgs.DisplayHelp);
            Assert.Equal(false, parsedArgs.SourceFiles.Any());

            parsedArgs = CSharpCommandLineParser.ScriptRunner.Parse(new[] { "/?" }, _baseDirectory, s_defaultSdkDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(true, parsedArgs.DisplayHelp);
            Assert.Equal(false, parsedArgs.SourceFiles.Any());

            parsedArgs = CSharpCommandLineParser.ScriptRunner.Parse(new[] { "c.csx  /langversion:6" }, _baseDirectory, s_defaultSdkDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(false, parsedArgs.DisplayHelp);
            Assert.Equal(true, parsedArgs.SourceFiles.Any());

            parsedArgs = CSharpCommandLineParser.ScriptRunner.Parse(new[] { "/langversion:-1", "c.csx", }, _baseDirectory, s_defaultSdkDirectory);
            parsedArgs.Errors.Verify(
                // error CS2007: Unrecognized option: '/langversion:-1'
                Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/langversion:-1"));

            Assert.Equal(false, parsedArgs.DisplayHelp);
            Assert.Equal(1, parsedArgs.SourceFiles.Length);

            parsedArgs = CSharpCommandLineParser.ScriptRunner.Parse(new[] { "c.csx  /r:s=d /r:d.dll" }, _baseDirectory, s_defaultSdkDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(false, parsedArgs.DisplayHelp);
            Assert.Equal(true, parsedArgs.SourceFiles.Any());

            parsedArgs = CSharpCommandLineParser.ScriptRunner.Parse(new[] { "@roslyn_test_non_existing_file" }, _baseDirectory, s_defaultSdkDirectory);
            parsedArgs.Errors.Verify(
                // error CS2011: Error opening response file 'D:\R0\Main\Binaries\Debug\dd'
                Diagnostic(ErrorCode.ERR_OpenResponseFile).WithArguments(Path.Combine(_baseDirectory, @"roslyn_test_non_existing_file")));

            Assert.Equal(false, parsedArgs.DisplayHelp);
            Assert.Equal(false, parsedArgs.SourceFiles.Any());

            parsedArgs = CSharpCommandLineParser.ScriptRunner.Parse(new[] { "c /define:DEBUG" }, _baseDirectory, s_defaultSdkDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(false, parsedArgs.DisplayHelp);
            Assert.Equal(true, parsedArgs.SourceFiles.Any());

            parsedArgs = CSharpCommandLineParser.ScriptRunner.Parse(new[] { "\\" }, _baseDirectory, s_defaultSdkDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(false, parsedArgs.DisplayHelp);
            Assert.Equal(true, parsedArgs.SourceFiles.Any());

            parsedArgs = CSharpCommandLineParser.ScriptRunner.Parse(new[] { "/r:d.dll", "c.csx" }, _baseDirectory, s_defaultSdkDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(false, parsedArgs.DisplayHelp);
            Assert.Equal(true, parsedArgs.SourceFiles.Any());

            parsedArgs = CSharpCommandLineParser.ScriptRunner.Parse(new[] { "/define:foo", "c.csx" }, _baseDirectory, s_defaultSdkDirectory);
            parsedArgs.Errors.Verify(
                // error CS2007: Unrecognized option: '/define:foo'
                Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/define:foo"));
            Assert.Equal(false, parsedArgs.DisplayHelp);
            Assert.Equal(true, parsedArgs.SourceFiles.Any());

            parsedArgs = CSharpCommandLineParser.ScriptRunner.Parse(new[] { "\"/r d.dll\"" }, _baseDirectory, s_defaultSdkDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(false, parsedArgs.DisplayHelp);
            Assert.Equal(true, parsedArgs.SourceFiles.Any());

            parsedArgs = CSharpCommandLineParser.ScriptRunner.Parse(new[] { "/r: d.dll", "a.cs" }, _baseDirectory, s_defaultSdkDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(false, parsedArgs.DisplayHelp);
            Assert.Equal(true, parsedArgs.SourceFiles.Any());
        }

        [Fact]
        public void LangVersion()
        {
            const LanguageVersion DefaultVersion = LanguageVersion.CSharp6;

            var parsedArgs = DefaultParse(new[] { "/langversion:1", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(LanguageVersion.CSharp1, parsedArgs.ParseOptions.LanguageVersion);

            parsedArgs = DefaultParse(new[] { "/langversion:2", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(LanguageVersion.CSharp2, parsedArgs.ParseOptions.LanguageVersion);

            parsedArgs = DefaultParse(new[] { "/langversion:3", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(LanguageVersion.CSharp3, parsedArgs.ParseOptions.LanguageVersion);

            parsedArgs = DefaultParse(new[] { "/langversion:4", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(LanguageVersion.CSharp4, parsedArgs.ParseOptions.LanguageVersion);

            parsedArgs = DefaultParse(new[] { "/langversion:5", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(LanguageVersion.CSharp5, parsedArgs.ParseOptions.LanguageVersion);

            parsedArgs = DefaultParse(new[] { "/langversion:6", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(LanguageVersion.CSharp6, parsedArgs.ParseOptions.LanguageVersion);

            parsedArgs = DefaultParse(new[] { "/langversion:default", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(DefaultVersion, parsedArgs.ParseOptions.LanguageVersion);

            parsedArgs = DefaultParse(new[] { "/langversion:iso-1", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(LanguageVersion.CSharp1, parsedArgs.ParseOptions.LanguageVersion);

            parsedArgs = DefaultParse(new[] { "/langversion:iso-2", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(LanguageVersion.CSharp2, parsedArgs.ParseOptions.LanguageVersion);

            parsedArgs = DefaultParse(new[] { "/langversion:default", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(DefaultVersion, parsedArgs.ParseOptions.LanguageVersion);

            // default value
            parsedArgs = DefaultParse(new[] { "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(DefaultVersion, parsedArgs.ParseOptions.LanguageVersion);

            // override value with iso-1
            parsedArgs = DefaultParse(new[] { "/langversion:6", "/langversion:iso-1", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(LanguageVersion.CSharp1, parsedArgs.ParseOptions.LanguageVersion);

            // override value with iso-2
            parsedArgs = DefaultParse(new[] { "/langversion:6", "/langversion:iso-2", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(LanguageVersion.CSharp2, parsedArgs.ParseOptions.LanguageVersion);

            // override value with default
            parsedArgs = DefaultParse(new[] { "/langversion:6", "/langversion:default", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(DefaultVersion, parsedArgs.ParseOptions.LanguageVersion);

            // override value with numeric
            parsedArgs = DefaultParse(new[] { "/langversion:iso-2", "/langversion:6", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(LanguageVersion.CSharp6, parsedArgs.ParseOptions.LanguageVersion);

            //  errors
            parsedArgs = DefaultParse(new[] { "/langversion:iso-3", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadCompatMode).WithArguments("iso-3"));
            Assert.Equal(DefaultVersion, parsedArgs.ParseOptions.LanguageVersion);

            parsedArgs = DefaultParse(new[] { "/langversion:iso1", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadCompatMode).WithArguments("iso1"));
            Assert.Equal(DefaultVersion, parsedArgs.ParseOptions.LanguageVersion);

            parsedArgs = DefaultParse(new[] { "/langversion:0", "/langversion:7", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                Diagnostic(ErrorCode.ERR_BadCompatMode).WithArguments("0"),
                Diagnostic(ErrorCode.ERR_BadCompatMode).WithArguments("7"));
            Assert.Equal(DefaultVersion, parsedArgs.ParseOptions.LanguageVersion);

            parsedArgs = DefaultParse(new[] { "/langversion:0", "/langversion:1000", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                Diagnostic(ErrorCode.ERR_BadCompatMode).WithArguments("0"),
                Diagnostic(ErrorCode.ERR_BadCompatMode).WithArguments("1000"));
            Assert.Equal(DefaultVersion, parsedArgs.ParseOptions.LanguageVersion);

            parsedArgs = DefaultParse(new[] { "/langversion", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "/langversion:"));
            Assert.Equal(DefaultVersion, parsedArgs.ParseOptions.LanguageVersion);

            parsedArgs = DefaultParse(new[] { "/LANGversion:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "/langversion:"));
            Assert.Equal(DefaultVersion, parsedArgs.ParseOptions.LanguageVersion);

            parsedArgs = DefaultParse(new[] { "/langversion: ", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "/langversion:"));
            Assert.Equal(DefaultVersion, parsedArgs.ParseOptions.LanguageVersion);
        }

        [Fact]
        [WorkItem(546961, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546961")]
        public void Define()
        {
            var parsedArgs = DefaultParse(new[] { "a.cs" }, _baseDirectory);
            Assert.Equal(0, parsedArgs.ParseOptions.PreprocessorSymbolNames.Count());
            Assert.Equal(false, parsedArgs.Errors.Any());

            parsedArgs = DefaultParse(new[] { "/d:FOO", "a.cs" }, _baseDirectory);
            Assert.Equal(1, parsedArgs.ParseOptions.PreprocessorSymbolNames.Count());
            Assert.Contains("FOO", parsedArgs.ParseOptions.PreprocessorSymbolNames);
            Assert.Equal(false, parsedArgs.Errors.Any());

            parsedArgs = DefaultParse(new[] { "/d:FOO;BAR,ZIP", "a.cs" }, _baseDirectory);
            Assert.Equal(3, parsedArgs.ParseOptions.PreprocessorSymbolNames.Count());
            Assert.Contains("FOO", parsedArgs.ParseOptions.PreprocessorSymbolNames);
            Assert.Contains("BAR", parsedArgs.ParseOptions.PreprocessorSymbolNames);
            Assert.Contains("ZIP", parsedArgs.ParseOptions.PreprocessorSymbolNames);
            Assert.Equal(false, parsedArgs.Errors.Any());

            parsedArgs = DefaultParse(new[] { "/d:FOO;4X", "a.cs" }, _baseDirectory);
            Assert.Equal(1, parsedArgs.ParseOptions.PreprocessorSymbolNames.Count());
            Assert.Contains("FOO", parsedArgs.ParseOptions.PreprocessorSymbolNames);
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
                // warning CS2029: Invalid value for '/define'; '' is not a valid identifier
                Diagnostic(ErrorCode.WRN_DefineIdentifierRequired).WithArguments(""));
            Assert.Equal(new[] { "def1", "def2" }, parsed);

            // Bug 17360
            parsedArgs = DefaultParse(new[] { "/d:public1;public2;", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
        }

        [Fact]
        public void Debug()
        {
            var parsedArgs = DefaultParse(new[] { "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(false, parsedArgs.CompilationOptions.DebugPlusMode);
            Assert.Equal(parsedArgs.EmitOptions.DebugInformationFormat, DebugInformationFormat.Pdb);

            parsedArgs = DefaultParse(new[] { "/debug-", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(false, parsedArgs.CompilationOptions.DebugPlusMode);
            Assert.Equal(parsedArgs.EmitOptions.DebugInformationFormat, DebugInformationFormat.Pdb);

            parsedArgs = DefaultParse(new[] { "/debug", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(false, parsedArgs.CompilationOptions.DebugPlusMode);
            Assert.Equal(parsedArgs.EmitOptions.DebugInformationFormat, DebugInformationFormat.Pdb);

            parsedArgs = DefaultParse(new[] { "/debug+", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(true, parsedArgs.CompilationOptions.DebugPlusMode);
            Assert.Equal(parsedArgs.EmitOptions.DebugInformationFormat, DebugInformationFormat.Pdb);

            parsedArgs = DefaultParse(new[] { "/debug+", "/debug-", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(false, parsedArgs.CompilationOptions.DebugPlusMode);
            Assert.Equal(parsedArgs.EmitOptions.DebugInformationFormat, DebugInformationFormat.Pdb);

            parsedArgs = DefaultParse(new[] { "/debug:full", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(false, parsedArgs.CompilationOptions.DebugPlusMode);
            Assert.Equal(parsedArgs.EmitOptions.DebugInformationFormat, DebugInformationFormat.Pdb);

            parsedArgs = DefaultParse(new[] { "/debug:FULL", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(false, parsedArgs.CompilationOptions.DebugPlusMode);
            Assert.Equal(parsedArgs.EmitOptions.DebugInformationFormat, DebugInformationFormat.Pdb);

            parsedArgs = DefaultParse(new[] { "/debug:pdbonly", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(false, parsedArgs.CompilationOptions.DebugPlusMode);
            Assert.Equal(parsedArgs.EmitOptions.DebugInformationFormat, DebugInformationFormat.Pdb);

            parsedArgs = DefaultParse(new[] { "/debug:portable", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(false, parsedArgs.CompilationOptions.DebugPlusMode);
            Assert.Equal(parsedArgs.EmitOptions.DebugInformationFormat, DebugInformationFormat.PortablePdb);

            parsedArgs = DefaultParse(new[] { "/debug:embedded", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(false, parsedArgs.CompilationOptions.DebugPlusMode);
            Assert.Equal(parsedArgs.EmitOptions.DebugInformationFormat, DebugInformationFormat.Embedded);

            parsedArgs = DefaultParse(new[] { "/debug:PDBONLY", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(false, parsedArgs.CompilationOptions.DebugPlusMode);
            Assert.Equal(parsedArgs.EmitOptions.DebugInformationFormat, DebugInformationFormat.Pdb);

            parsedArgs = DefaultParse(new[] { "/debug:full", "/debug:pdbonly", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(false, parsedArgs.CompilationOptions.DebugPlusMode);
            Assert.Equal(parsedArgs.EmitOptions.DebugInformationFormat, DebugInformationFormat.Pdb);

            parsedArgs = DefaultParse(new[] { "/debug:pdbonly", "/debug:full", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.EmitPdb);
            Assert.Equal(DebugInformationFormat.Pdb, parsedArgs.EmitOptions.DebugInformationFormat);
            Assert.Equal(false, parsedArgs.CompilationOptions.DebugPlusMode);

            parsedArgs = DefaultParse(new[] { "/debug:pdbonly", "/debug-", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.EmitPdb);
            Assert.Equal(DebugInformationFormat.Pdb, parsedArgs.EmitOptions.DebugInformationFormat);
            Assert.Equal(false, parsedArgs.CompilationOptions.DebugPlusMode);

            parsedArgs = DefaultParse(new[] { "/debug:pdbonly", "/debug-", "/debug", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.EmitPdb);
            Assert.Equal(DebugInformationFormat.Pdb, parsedArgs.EmitOptions.DebugInformationFormat);
            Assert.Equal(false, parsedArgs.CompilationOptions.DebugPlusMode);

            parsedArgs = DefaultParse(new[] { "/debug:pdbonly", "/debug-", "/debug+", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.EmitPdb);
            Assert.Equal(DebugInformationFormat.Pdb, parsedArgs.EmitOptions.DebugInformationFormat);
            Assert.Equal(true, parsedArgs.CompilationOptions.DebugPlusMode);

            parsedArgs = DefaultParse(new[] { "/debug:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "debug"));

            parsedArgs = DefaultParse(new[] { "/debug:+", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadDebugType).WithArguments("+"));

            parsedArgs = DefaultParse(new[] { "/debug:invalid", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadDebugType).WithArguments("invalid"));

            parsedArgs = DefaultParse(new[] { "/debug-:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/debug-:"));
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void Pdb()
        {
            var parsedArgs = DefaultParse(new[] { "/pdb:something", "a.cs" }, _baseDirectory);
            Assert.Equal(Path.Combine(_baseDirectory, "something.pdb"), parsedArgs.PdbPath);

            // No pdb
            parsedArgs = DefaultParse(new[] { @"/debug", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Null(parsedArgs.PdbPath);

            parsedArgs = DefaultParse(new[] { "/pdb", "/debug", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("/pdb"));

            parsedArgs = DefaultParse(new[] { "/pdb:", "/debug", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("/pdb:"));

            parsedArgs = DefaultParse(new[] { "/pdb:something", "/debug", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();

            // temp: path changed
            //parsedArgs = DefaultParse(new[] { "/debug", "/pdb:.x", "a.cs" }, baseDirectory);
            //parsedArgs.Errors.Verify(
            //    // error CS2021: File name '.x' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
            //    Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments(".x"));

            parsedArgs = DefaultParse(new[] { @"/pdb:""""", "/debug", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments(""));

            parsedArgs = DefaultParse(new[] { "/pdb:C:\\", "/debug", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments("C:\\"));

            // Should preserve fully qualified paths
            parsedArgs = DefaultParse(new[] { @"/pdb:C:\MyFolder\MyPdb.pdb", "/debug", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(@"C:\MyFolder\MyPdb.pdb", parsedArgs.PdbPath);

            // Should preserve fully qualified paths
            parsedArgs = DefaultParse(new[] { @"/pdb:c:\MyPdb.pdb", "/debug", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(@"c:\MyPdb.pdb", parsedArgs.PdbPath);

            parsedArgs = DefaultParse(new[] { @"/pdb:\MyFolder\MyPdb.pdb", "/debug", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(Path.Combine(Path.GetPathRoot(_baseDirectory), @"MyFolder\MyPdb.pdb"), parsedArgs.PdbPath);

            // Should handle quotes
            parsedArgs = DefaultParse(new[] { @"/pdb:""C:\My Folder\MyPdb.pdb""", "/debug", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(@"C:\My Folder\MyPdb.pdb", parsedArgs.PdbPath);

            // Should expand partially qualified paths
            parsedArgs = DefaultParse(new[] { @"/pdb:MyPdb.pdb", "/debug", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(FileUtilities.ResolveRelativePath("MyPdb.pdb", _baseDirectory), parsedArgs.PdbPath);

            // Should expand partially qualified paths
            parsedArgs = DefaultParse(new[] { @"/pdb:..\MyPdb.pdb", "/debug", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            // Temp: Path info changed
            // Assert.Equal(FileUtilities.ResolveRelativePath("MyPdb.pdb", "..\\", baseDirectory), parsedArgs.PdbPath);

            parsedArgs = DefaultParse(new[] { @"/pdb:\\b", "/debug", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2021: File name '.x' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments(@"\\b"));
            Assert.Null(parsedArgs.PdbPath);

            parsedArgs = DefaultParse(new[] { @"/pdb:\\b\OkFileName.pdb", "/debug", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2021: File name '.x' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments(@"\\b\OkFileName.pdb"));
            Assert.Null(parsedArgs.PdbPath);

            parsedArgs = DefaultParse(new[] { @"/pdb:\\server\share\MyPdb.pdb", "/debug", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(@"\\server\share\MyPdb.pdb", parsedArgs.PdbPath);

            // invalid name:
            parsedArgs = DefaultParse(new[] { "/pdb:a.b\0b", "/debug", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments("a.b\0b"));
            Assert.Null(parsedArgs.PdbPath);


            parsedArgs = DefaultParse(new[] { "/pdb:a\uD800b.pdb", "/debug", "a.cs" }, _baseDirectory);
            //parsedArgs.Errors.Verify(
            //    Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments("a\uD800b.pdb"));
            Assert.Null(parsedArgs.PdbPath);

            // Dev11 reports CS0016: Could not write to output file 'd:\Temp\q\a<>.z'
            parsedArgs = DefaultParse(new[] { @"/pdb:""a<>.pdb""", "a.vb" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2021: File name 'a<>.pdb' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments("a<>.pdb"));
            Assert.Null(parsedArgs.PdbPath);

            parsedArgs = DefaultParse(new[] { "/pdb:.x", "/debug", "a.cs" }, _baseDirectory);
            //parsedArgs.Errors.Verify(
            //    // error CS2021: File name '.x' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
            //    Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments(".x"));
            Assert.Null(parsedArgs.PdbPath);
        }


        [Fact]
        public void Optimize()
        {
            var parsedArgs = DefaultParse(new[] { "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(new CSharpCompilationOptions(OutputKind.ConsoleApplication).OptimizationLevel, parsedArgs.CompilationOptions.OptimizationLevel);

            parsedArgs = DefaultParse(new[] { "/optimize-", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(OptimizationLevel.Debug, parsedArgs.CompilationOptions.OptimizationLevel);

            parsedArgs = DefaultParse(new[] { "/optimize", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(OptimizationLevel.Release, parsedArgs.CompilationOptions.OptimizationLevel);

            parsedArgs = DefaultParse(new[] { "/optimize+", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(OptimizationLevel.Release, parsedArgs.CompilationOptions.OptimizationLevel);

            parsedArgs = DefaultParse(new[] { "/optimize+", "/optimize-", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(OptimizationLevel.Debug, parsedArgs.CompilationOptions.OptimizationLevel);

            parsedArgs = DefaultParse(new[] { "/optimize:+", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/optimize:+"));

            parsedArgs = DefaultParse(new[] { "/optimize:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/optimize:"));

            parsedArgs = DefaultParse(new[] { "/optimize-:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/optimize-:"));

            parsedArgs = DefaultParse(new[] { "/o-", "a.cs" }, _baseDirectory);
            Assert.Equal(OptimizationLevel.Debug, parsedArgs.CompilationOptions.OptimizationLevel);

            parsedArgs = DefaultParse(new string[] { "/o", "a.cs" }, _baseDirectory);
            Assert.Equal(OptimizationLevel.Release, parsedArgs.CompilationOptions.OptimizationLevel);

            parsedArgs = DefaultParse(new string[] { "/o+", "a.cs" }, _baseDirectory);
            Assert.Equal(OptimizationLevel.Release, parsedArgs.CompilationOptions.OptimizationLevel);

            parsedArgs = DefaultParse(new string[] { "/o+", "/optimize-", "a.cs" }, _baseDirectory);
            Assert.Equal(OptimizationLevel.Debug, parsedArgs.CompilationOptions.OptimizationLevel);

            parsedArgs = DefaultParse(new string[] { "/o:+", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/o:+"));

            parsedArgs = DefaultParse(new string[] { "/o:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/o:"));

            parsedArgs = DefaultParse(new string[] { "/o-:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/o-:"));
        }

        [Fact]
        public void Deterministic()
        {
            var parsedArgs = DefaultParse(new[] { "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(false, parsedArgs.CompilationOptions.Deterministic);

            parsedArgs = DefaultParse(new[] { "/deterministic+", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(true, parsedArgs.CompilationOptions.Deterministic);

            parsedArgs = DefaultParse(new[] { "/deterministic", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(true, parsedArgs.CompilationOptions.Deterministic);

            parsedArgs = DefaultParse(new[] { "/deterministic-", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(false, parsedArgs.CompilationOptions.Deterministic);
        }

        [Fact]
        public void ParseReferences()
        {
            var parsedArgs = DefaultParse(new string[] { "/r:foo.dll", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(2, parsedArgs.MetadataReferences.Length);

            parsedArgs = DefaultParse(new string[] { "/r:foo.dll;", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(2, parsedArgs.MetadataReferences.Length);

            Assert.Equal(typeof(object).Assembly.Location, parsedArgs.MetadataReferences[0].Reference);
            Assert.Equal(MetadataReferenceProperties.Assembly, parsedArgs.MetadataReferences[0].Properties);

            Assert.Equal("foo.dll", parsedArgs.MetadataReferences[1].Reference);
            Assert.Equal(MetadataReferenceProperties.Assembly, parsedArgs.MetadataReferences[1].Properties);


            parsedArgs = DefaultParse(new string[] { @"/l:foo.dll", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(2, parsedArgs.MetadataReferences.Length);

            Assert.Equal(typeof(object).Assembly.Location, parsedArgs.MetadataReferences[0].Reference);
            Assert.Equal(MetadataReferenceProperties.Assembly, parsedArgs.MetadataReferences[0].Properties);

            Assert.Equal("foo.dll", parsedArgs.MetadataReferences[1].Reference);
            Assert.Equal(MetadataReferenceProperties.Assembly.WithEmbedInteropTypes(true), parsedArgs.MetadataReferences[1].Properties);


            parsedArgs = DefaultParse(new string[] { @"/addmodule:foo.dll", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(2, parsedArgs.MetadataReferences.Length);

            Assert.Equal(typeof(object).Assembly.Location, parsedArgs.MetadataReferences[0].Reference);
            Assert.Equal(MetadataReferenceProperties.Assembly, parsedArgs.MetadataReferences[0].Properties);

            Assert.Equal("foo.dll", parsedArgs.MetadataReferences[1].Reference);
            Assert.Equal(MetadataReferenceProperties.Module, parsedArgs.MetadataReferences[1].Properties);


            parsedArgs = DefaultParse(new string[] { @"/r:a=foo.dll", "/l:b=bar.dll", "/addmodule:c=mod.dll", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(4, parsedArgs.MetadataReferences.Length);

            Assert.Equal(typeof(object).Assembly.Location, parsedArgs.MetadataReferences[0].Reference);
            Assert.Equal(MetadataReferenceProperties.Assembly, parsedArgs.MetadataReferences[0].Properties);

            Assert.Equal("foo.dll", parsedArgs.MetadataReferences[1].Reference);
            Assert.Equal(MetadataReferenceProperties.Assembly.WithAliases(new[] { "a" }), parsedArgs.MetadataReferences[1].Properties);

            Assert.Equal("bar.dll", parsedArgs.MetadataReferences[2].Reference);
            Assert.Equal(MetadataReferenceProperties.Assembly.WithAliases(new[] { "b" }).WithEmbedInteropTypes(true), parsedArgs.MetadataReferences[2].Properties);

            Assert.Equal("c=mod.dll", parsedArgs.MetadataReferences[3].Reference);
            Assert.Equal(MetadataReferenceProperties.Module, parsedArgs.MetadataReferences[3].Properties);

            // TODO: multiple files, quotes, etc.
        }

        [Fact]
        public void ParseAnalyzers()
        {
            var parsedArgs = DefaultParse(new string[] { @"/a:foo.dll", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(1, parsedArgs.AnalyzerReferences.Length);
            Assert.Equal("foo.dll", parsedArgs.AnalyzerReferences[0].FilePath);

            parsedArgs = DefaultParse(new string[] { @"/analyzer:foo.dll", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(1, parsedArgs.AnalyzerReferences.Length);
            Assert.Equal("foo.dll", parsedArgs.AnalyzerReferences[0].FilePath);

            parsedArgs = DefaultParse(new string[] { "/analyzer:\"foo.dll\"", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(1, parsedArgs.AnalyzerReferences.Length);
            Assert.Equal("foo.dll", parsedArgs.AnalyzerReferences[0].FilePath);

            parsedArgs = DefaultParse(new string[] { @"/a:foo.dll;bar.dll", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(2, parsedArgs.AnalyzerReferences.Length);
            Assert.Equal("foo.dll", parsedArgs.AnalyzerReferences[0].FilePath);
            Assert.Equal("bar.dll", parsedArgs.AnalyzerReferences[1].FilePath);

            parsedArgs = DefaultParse(new string[] { @"/a:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("/a:"));

            parsedArgs = DefaultParse(new string[] { "/a", "a.cs" }, _baseDirectory);
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
            var csc = new MockCSharpCompiler(null, dir.Path, new[] { "/nologo", "/preferreduilang:en", "/a:missing.dll", "a.cs" });
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
            var csc = new MockCSharpCompiler(null, dir.Path, new[] { "/nologo", "/preferreduilang:en", "/t:library", "/a:" + typeof(object).Assembly.Location, "a.cs" });
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
            var parsedArgs = DefaultParse(new string[] { @"/ruleset:" + file.Path, "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
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
            var parsedArgs = DefaultParse(new string[] { @"/ruleset:" + "\"" + file.Path + "\"", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
        }

        [Fact]
        public void RuleSetSwitchParseErrors()
        {
            var parsedArgs = DefaultParse(new string[] { @"/ruleset", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                 Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "ruleset"));

            parsedArgs = DefaultParse(new string[] { @"/ruleset:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "ruleset"));

            parsedArgs = DefaultParse(new string[] { @"/ruleset:blah", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                Diagnostic(ErrorCode.ERR_CantReadRulesetFile).WithArguments(Path.Combine(TempRoot.Root, "blah"), "File not found."));

            parsedArgs = DefaultParse(new string[] { @"/ruleset:blah;blah.ruleset", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                Diagnostic(ErrorCode.ERR_CantReadRulesetFile).WithArguments(Path.Combine(TempRoot.Root, "blah;blah.ruleset"), "File not found."));

            var file = CreateRuleSetFile("Random text");
            parsedArgs = DefaultParse(new string[] { @"/ruleset:" + file.Path, "a.cs" }, _baseDirectory);
            //parsedArgs.Errors.Verify(
            //    Diagnostic(ErrorCode.ERR_CantReadRulesetFile).WithArguments(file.Path, "Data at the root level is invalid. Line 1, position 1."));
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
            var csc = new MockCSharpCompiler(null, dir.Path, new[] { "/nologo", "/preferreduilang:en", "/t:library", "/a:" + Assembly.GetExecutingAssembly().Location, "a.cs" });
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
            var csc = new MockCSharpCompiler(null, dir.Path, new[] { "/nologo", "/preferreduilang:en", "/t:library", "/a:" + Assembly.GetExecutingAssembly().Location, "a.cs", "/ruleset:" + ruleSetFile.Path });
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
            var csc = new MockCSharpCompiler(null, dir.Path,
                new[] {
                    "/nologo", "/preferreduilang:en", "/t:library",
                    "/a:" + Assembly.GetExecutingAssembly().Location, "a.cs",
                    "/ruleset:" + ruleSetFile.Path, "/warnaserror+", "/nowarn:8032" });
            int exitCode = csc.Run(outWriter);
            Assert.Equal(1, exitCode);
            // Diagnostic thrown as error: command line always overrides ruleset.
            Assert.Contains("a.cs(2,7): error Warning01: Throwing a diagnostic for types declared", outWriter.ToString(), StringComparison.Ordinal);

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            csc = new MockCSharpCompiler(null, dir.Path,
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
            var csc = new MockCSharpCompiler(null, dir.Path,
                new[] {
                    "/nologo", "/t:library",
                    "/a:" + Assembly.GetExecutingAssembly().Location, "a.cs",
                    "/ruleset:" + ruleSetFile.Path, "/warn:0" });
            int exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            // Diagnostic suppressed: commandline always overrides ruleset.
            Assert.DoesNotContain("Warning01", outWriter.ToString(), StringComparison.Ordinal);

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            csc = new MockCSharpCompiler(null, dir.Path,
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

        [ConditionalFact(typeof(WindowsOnly))]
        public void DiagnosticFormatting()
        {
            string source = @"
using System;

class C
{
        public static void Main()
        {
            Foo(0);
#line 10 ""c:\temp\a\1.cs""
            Foo(1);
#line 20 ""C:\a\..\b.cs""
            Foo(2);
#line 30 ""C:\a\../B.cs""
            Foo(3);
#line 40 ""../b.cs""
            Foo(4);
#line 50 ""..\b.cs""
            Foo(5);
#line 60 ""C:\X.cs""
            Foo(6);
#line 70 ""C:\x.cs""
            Foo(7);
#line 90 ""      ""
		    Foo(9);
#line 100 ""C:\*.cs""
		    Foo(10);
#line 110 """"
		    Foo(11);
#line hidden
            Foo(12);
#line default
            Foo(13);
#line 140 ""***""
            Foo(14);
        }
    }
";
            var dir = Temp.CreateDirectory();
            dir.CreateFile("a.cs").WriteAllText(source);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = new MockCSharpCompiler(null, dir.Path, new[] { "/nologo", "/preferreduilang:en", "/t:library", "a.cs" });
            int exitCode = csc.Run(outWriter);
            Assert.Equal(1, exitCode);

            // with /fullpaths off
            string expected = @"
a.cs(8,13): error CS0103: The name 'Foo' does not exist in the current context
c:\temp\a\1.cs(10,13): error CS0103: The name 'Foo' does not exist in the current context
C:\b.cs(20,13): error CS0103: The name 'Foo' does not exist in the current context
C:\B.cs(30,13): error CS0103: The name 'Foo' does not exist in the current context
" + Path.GetFullPath(Path.Combine(dir.Path, @"..\b.cs")) + @"(40,13): error CS0103: The name 'Foo' does not exist in the current context
" + Path.GetFullPath(Path.Combine(dir.Path, @"..\b.cs")) + @"(50,13): error CS0103: The name 'Foo' does not exist in the current context
C:\X.cs(60,13): error CS0103: The name 'Foo' does not exist in the current context
C:\x.cs(70,13): error CS0103: The name 'Foo' does not exist in the current context
      (90,7): error CS0103: The name 'Foo' does not exist in the current context
C:\*.cs(100,7): error CS0103: The name 'Foo' does not exist in the current context
(110,7): error CS0103: The name 'Foo' does not exist in the current context
(112,13): error CS0103: The name 'Foo' does not exist in the current context
a.cs(32,13): error CS0103: The name 'Foo' does not exist in the current context
***(140,13): error CS0103: The name 'Foo' does not exist in the current context";

            AssertEx.Equal(
                expected.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries),
                outWriter.ToString().Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries),
                itemSeparator: "\r\n");

            // with /fullpaths on
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            csc = new MockCSharpCompiler(null, dir.Path, new[] { "/nologo", "/preferreduilang:en", "/t:library", "/fullpaths", "a.cs" });
            exitCode = csc.Run(outWriter);
            Assert.Equal(1, exitCode);

            expected = @"
" + Path.Combine(dir.Path, @"a.cs") + @"(8,13): error CS0103: The name 'Foo' does not exist in the current context
c:\temp\a\1.cs(10,13): error CS0103: The name 'Foo' does not exist in the current context
C:\b.cs(20,13): error CS0103: The name 'Foo' does not exist in the current context
C:\B.cs(30,13): error CS0103: The name 'Foo' does not exist in the current context
" + Path.GetFullPath(Path.Combine(dir.Path, @"..\b.cs")) + @"(40,13): error CS0103: The name 'Foo' does not exist in the current context
" + Path.GetFullPath(Path.Combine(dir.Path, @"..\b.cs")) + @"(50,13): error CS0103: The name 'Foo' does not exist in the current context
C:\X.cs(60,13): error CS0103: The name 'Foo' does not exist in the current context
C:\x.cs(70,13): error CS0103: The name 'Foo' does not exist in the current context
      (90,7): error CS0103: The name 'Foo' does not exist in the current context
C:\*.cs(100,7): error CS0103: The name 'Foo' does not exist in the current context
(110,7): error CS0103: The name 'Foo' does not exist in the current context
(112,13): error CS0103: The name 'Foo' does not exist in the current context
" + Path.Combine(dir.Path, @"a.cs") + @"(32,13): error CS0103: The name 'Foo' does not exist in the current context
***(140,13): error CS0103: The name 'Foo' does not exist in the current context";

            AssertEx.Equal(
                expected.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries),
                outWriter.ToString().Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries),
                itemSeparator: "\r\n");
        }

        [WorkItem(540891, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540891")]
        [ConditionalFact(typeof(WindowsOnly))]
        public void ParseOut()
        {
            const string baseDirectory = @"C:\abc\def\baz";

            var parsedArgs = DefaultParse(new[] { @"/out:""""", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2021: File name '' contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments(""));

            parsedArgs = DefaultParse(new[] { @"/out:", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2005: Missing file specification for '/out:' option
                Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("/out:"));

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
                Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments(currentDrive + ":a.cs"));

            Assert.Null(parsedArgs.CompilationName);
            Assert.Null(parsedArgs.OutputFileName);
            Assert.Null(parsedArgs.CompilationOptions.ModuleName);
            Assert.Equal(baseDirectory, parsedArgs.OutputDirectory);

            // UNC
            parsedArgs = DefaultParse(new[] { @"/out:\\b", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2021: File name '.x' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments(@"\\b"));

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
                Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments("a.b\0b"));

            Assert.Null(parsedArgs.OutputFileName);
            Assert.Null(parsedArgs.CompilationName);
            Assert.Null(parsedArgs.CompilationOptions.ModuleName);

            // Temporary skip following scenarios because of the error message changed (path)
            //parsedArgs = DefaultParse(new[] { "/out:a\uD800b.dll", "a.cs" }, baseDirectory);
            //parsedArgs.Errors.Verify(
            //    // error CS2021: File name '.x' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
            //    Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments("a\uD800b.dll"));

            // Dev11 reports CS0016: Could not write to output file 'd:\Temp\q\a<>.z'
            parsedArgs = DefaultParse(new[] { @"/out:""a<>.dll""", "a.vb" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2021: File name 'a<>.dll' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments("a<>.dll"));

            Assert.Null(parsedArgs.OutputFileName);
            Assert.Null(parsedArgs.CompilationName);
            Assert.Null(parsedArgs.CompilationOptions.ModuleName);

            parsedArgs = DefaultParse(new[] { @"/out:.exe", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2021: File name '.exe' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments(".exe")
                );

            Assert.Null(parsedArgs.OutputFileName);
            Assert.Null(parsedArgs.CompilationName);
            Assert.Null(parsedArgs.CompilationOptions.ModuleName);

            parsedArgs = DefaultParse(new[] { @"/t:exe", @"/out:.exe", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2021: File name '.exe' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments(".exe")
                );

            Assert.Null(parsedArgs.OutputFileName);
            Assert.Null(parsedArgs.CompilationName);
            Assert.Null(parsedArgs.CompilationOptions.ModuleName);

            parsedArgs = DefaultParse(new[] { @"/t:library", @"/out:.dll", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2021: File name '.dll' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments(".dll")
                );

            Assert.Null(parsedArgs.OutputFileName);
            Assert.Null(parsedArgs.CompilationName);
            Assert.Null(parsedArgs.CompilationOptions.ModuleName);

            parsedArgs = DefaultParse(new[] { @"/t:module", @"/out:.netmodule", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2021: File name '.netmodule' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments(".netmodule")
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
                Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments(".dll")
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
            var parsedArgs = DefaultParse(new[] { "/out:.x", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2021: File name '.x' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments(".x"));

            Assert.Null(parsedArgs.OutputFileName);
            Assert.Null(parsedArgs.CompilationName);
            Assert.Null(parsedArgs.CompilationOptions.ModuleName);

            parsedArgs = DefaultParse(new[] { "/out:.x", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2021: File name '.x' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments(".x"));

            Assert.Null(parsedArgs.OutputFileName);
            Assert.Null(parsedArgs.CompilationName);
            Assert.Null(parsedArgs.CompilationOptions.ModuleName);
        }

        [ConditionalFact(typeof(WindowsOnly))]
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
                Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments(currentDrive + ":a.xml"));

            Assert.Null(parsedArgs.DocumentationPath);
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode); //Even though the format was incorrect

            // UNC
            parsedArgs = DefaultParse(new[] { @"/doc:\\b", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments(@"\\b"));

            Assert.Null(parsedArgs.DocumentationPath);
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode); //Even though the format was incorrect

            parsedArgs = DefaultParse(new[] { @"/doc:\\server\share\file.xml", "a.vb" }, baseDirectory);
            parsedArgs.Errors.Verify();

            Assert.Equal(@"\\server\share\file.xml", parsedArgs.DocumentationPath);
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode);

            // invalid name:
            parsedArgs = DefaultParse(new[] { "/doc:a.b\0b", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments("a.b\0b"));

            Assert.Null(parsedArgs.DocumentationPath);
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode); //Even though the format was incorrect

            // Temp
            // parsedArgs = DefaultParse(new[] { "/doc:a\uD800b.xml", "a.cs" }, baseDirectory);
            // parsedArgs.Errors.Verify(
            //    Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments("a\uD800b.xml"));

            // Assert.Null(parsedArgs.DocumentationPath);
            // Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode); //Even though the format was incorrect

            parsedArgs = DefaultParse(new[] { @"/doc:""a<>.xml""", "a.vb" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2021: File name 'a<>.xml' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments("a<>.xml"));

            Assert.Null(parsedArgs.DocumentationPath);
            Assert.Equal(DocumentationMode.Diagnose, parsedArgs.ParseOptions.DocumentationMode); //Even though the format was incorrect
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void ParseErrorLog()
        {
            const string baseDirectory = @"C:\abc\def\baz";

            var parsedArgs = DefaultParse(new[] { @"/errorlog:""""", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing ':<file>' for '/errorlog:' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments(":<file>", "/errorlog:"));
            Assert.Null(parsedArgs.ErrorLogPath);
            Assert.False(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics);

            parsedArgs = DefaultParse(new[] { @"/errorlog:", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing ':<file>' for '/errorlog:' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments(":<file>", "/errorlog:"));
            Assert.Null(parsedArgs.ErrorLogPath);
            Assert.False(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics);

            parsedArgs = DefaultParse(new[] { @"/errorlog", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing ':<file>' for '/errorlog' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments(":<file>", "/errorlog"));
            Assert.Null(parsedArgs.ErrorLogPath);
            Assert.False(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics);

            // Should preserve fully qualified paths
            parsedArgs = DefaultParse(new[] { @"/errorlog:C:\MyFolder\MyBinary.xml", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(@"C:\MyFolder\MyBinary.xml", parsedArgs.ErrorLogPath);
            Assert.True(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics);

            // Escaped quote in the middle is an error
            parsedArgs = DefaultParse(new[] { @"/errorlog:C:\""My Folder""\MyBinary.xml", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                 Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments(@"C:""My Folder\MyBinary.xml").WithLocation(1, 1));

            // Should handle quotes
            parsedArgs = DefaultParse(new[] { @"/errorlog:""C:\My Folder\MyBinary.xml""", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(@"C:\My Folder\MyBinary.xml", parsedArgs.ErrorLogPath);
            Assert.True(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics);

            // Should expand partially qualified paths
            parsedArgs = DefaultParse(new[] { @"/errorlog:MyBinary.xml", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(Path.Combine(baseDirectory, "MyBinary.xml"), parsedArgs.ErrorLogPath);
            Assert.True(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics);

            // Should expand partially qualified paths
            parsedArgs = DefaultParse(new[] { @"/errorlog:..\MyBinary.xml", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(@"C:\abc\def\MyBinary.xml", parsedArgs.ErrorLogPath);
            Assert.True(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics);

            // drive-relative path:
            char currentDrive = Directory.GetCurrentDirectory()[0];
            parsedArgs = DefaultParse(new[] { "/errorlog:" + currentDrive + @":a.xml", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2021: File name 'D:a.xml' is contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments(currentDrive + ":a.xml"));

            Assert.Null(parsedArgs.ErrorLogPath);
            Assert.False(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics);

            // UNC
            parsedArgs = DefaultParse(new[] { @"/errorlog:\\b", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments(@"\\b"));

            Assert.Null(parsedArgs.ErrorLogPath);
            Assert.False(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics);

            parsedArgs = DefaultParse(new[] { @"/errorlog:\\server\share\file.xml", "a.vb" }, baseDirectory);
            parsedArgs.Errors.Verify();

            Assert.Equal(@"\\server\share\file.xml", parsedArgs.ErrorLogPath);

            // invalid name:
            parsedArgs = DefaultParse(new[] { "/errorlog:a.b\0b", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify(
                Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments("a.b\0b"));

            Assert.Null(parsedArgs.ErrorLogPath);
            Assert.False(parsedArgs.CompilationOptions.ReportSuppressedDiagnostics);

            parsedArgs = DefaultParse(new[] { @"/errorlog:""a<>.xml""", "a.vb" }, baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2021: File name 'a<>.xml' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments("a<>.xml"));

            Assert.Null(parsedArgs.ErrorLogPath);
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

            var silverlight = Temp.CreateFile().WriteAllBytes(TestResources.NetFX.silverlight_v5_0_5_0.System_v5_0_5_0_silverlight).Path;
            var net4_0dll = Temp.CreateFile().WriteAllBytes(TestResources.NetFX.v4_0_30319.System).Path;

            // Test linking two appconfig dlls with simple src
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = new MockCSharpCompiler(null, srcDirectory,
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
            var exitCode = new MockCSharpCompiler(null, srcDirectory,
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

            Assert.Equal(@"C:\abc\def\baz\a\b.xml", parsedArgs.ErrorLogPath);

            Assert.Equal(@"C:\abc\def\baz\c", parsedArgs.OutputDirectory);
            Assert.Equal("d.exe", parsedArgs.OutputFileName);

            // XML does not fall back on output directory.
            parsedArgs = DefaultParse(new[] { @"/errorlog:b.xml", @"/out:c\d.exe", "a.cs" }, baseDirectory);
            parsedArgs.Errors.Verify();

            Assert.Equal(@"C:\abc\def\baz\b.xml", parsedArgs.ErrorLogPath);

            Assert.Equal(@"C:\abc\def\baz\c", parsedArgs.OutputDirectory);
            Assert.Equal("d.exe", parsedArgs.OutputFileName);
        }

        [Fact]
        public void ModuleAssemblyName()
        {
            var parsedArgs = DefaultParse(new[] { @"/target:module", "/moduleassemblyname:foo", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal("foo", parsedArgs.CompilationName);
            Assert.Equal("a.netmodule", parsedArgs.CompilationOptions.ModuleName);

            parsedArgs = DefaultParse(new[] { @"/target:library", "/moduleassemblyname:foo", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS0734: The /moduleassemblyname option may only be specified when building a target type of 'module'
                Diagnostic(ErrorCode.ERR_AssemblyNameOnNonModule));

            parsedArgs = DefaultParse(new[] { @"/target:exe", "/moduleassemblyname:foo", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS0734: The /moduleassemblyname option may only be specified when building a target type of 'module'
                Diagnostic(ErrorCode.ERR_AssemblyNameOnNonModule));

            parsedArgs = DefaultParse(new[] { @"/target:winexe", "/moduleassemblyname:foo", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS0734: The /moduleassemblyname option may only be specified when building a target type of 'module'
                Diagnostic(ErrorCode.ERR_AssemblyNameOnNonModule));
        }

        [Fact]
        public void ModuleName()
        {
            var parsedArgs = DefaultParse(new[] { @"/target:module", "/modulename:foo", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal("foo", parsedArgs.CompilationOptions.ModuleName);

            parsedArgs = DefaultParse(new[] { @"/target:library", "/modulename:bar", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal("bar", parsedArgs.CompilationOptions.ModuleName);

            parsedArgs = DefaultParse(new[] { @"/target:exe", "/modulename:CommonLanguageRuntimeLibrary", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal("CommonLanguageRuntimeLibrary", parsedArgs.CompilationOptions.ModuleName);

            parsedArgs = DefaultParse(new[] { @"/target:winexe", "/modulename:foo", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal("foo", parsedArgs.CompilationOptions.ModuleName);

            parsedArgs = DefaultParse(new[] { @"/target:exe", "/modulename:", "a.cs" }, _baseDirectory);
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
            var csc = new MockCSharpCompiler(null, dir.Path, new[] { "/modulename:hocusPocus ", "/out:" + exeName + " ", file1.Path });
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
            var parsedArgs = DefaultParse(new[] { @"/platform:x64", "a.cs" }, _baseDirectory);
            Assert.False(parsedArgs.Errors.Any());
            Assert.Equal(Platform.X64, parsedArgs.CompilationOptions.Platform);

            parsedArgs = DefaultParse(new[] { @"/platform:X86", "a.cs" }, _baseDirectory);
            Assert.False(parsedArgs.Errors.Any());
            Assert.Equal(Platform.X86, parsedArgs.CompilationOptions.Platform);

            parsedArgs = DefaultParse(new[] { @"/platform:itanum", "a.cs" }, _baseDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.ERR_BadPlatformType, parsedArgs.Errors.First().Code);
            Assert.Equal(Platform.AnyCpu, parsedArgs.CompilationOptions.Platform);

            parsedArgs = DefaultParse(new[] { "/platform:itanium", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(Platform.Itanium, parsedArgs.CompilationOptions.Platform);

            parsedArgs = DefaultParse(new[] { "/platform:anycpu", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(Platform.AnyCpu, parsedArgs.CompilationOptions.Platform);

            parsedArgs = DefaultParse(new[] { "/platform:anycpu32bitpreferred", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(Platform.AnyCpu32BitPreferred, parsedArgs.CompilationOptions.Platform);

            parsedArgs = DefaultParse(new[] { "/platform:arm", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(Platform.Arm, parsedArgs.CompilationOptions.Platform);

            parsedArgs = DefaultParse(new[] { "/platform", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing '<string>' for 'platform' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<string>", "/platform"));
            Assert.Equal(Platform.AnyCpu, parsedArgs.CompilationOptions.Platform);  //anycpu is default

            parsedArgs = DefaultParse(new[] { "/platform:", "a.cs" }, _baseDirectory);
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
            var parsedArgs = DefaultParse(new[] { @"/baseaddress:x64", "a.cs" }, _baseDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.ERR_BadBaseNumber, parsedArgs.Errors.First().Code);

            parsedArgs = DefaultParse(new[] { @"/platform:x64", @"/baseaddress:0x8000000000011111", "a.cs" }, _baseDirectory);
            Assert.False(parsedArgs.Errors.Any());
            Assert.Equal(0x8000000000011111ul, parsedArgs.EmitOptions.BaseAddress);

            parsedArgs = DefaultParse(new[] { @"/platform:x86", @"/baseaddress:0x8000000000011111", "a.cs" }, _baseDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.ERR_BadBaseNumber, parsedArgs.Errors.First().Code);

            parsedArgs = DefaultParse(new[] { @"/baseaddress:", "a.cs" }, _baseDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.ERR_SwitchNeedsNumber, parsedArgs.Errors.First().Code);

            parsedArgs = DefaultParse(new[] { @"/baseaddress:-23", "a.cs" }, _baseDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.ERR_BadBaseNumber, parsedArgs.Errors.First().Code);

            parsedArgs = DefaultParse(new[] { @"/platform:x64", @"/baseaddress:01777777777777777777777", "a.cs" }, _baseDirectory);
            Assert.Equal(ulong.MaxValue, parsedArgs.EmitOptions.BaseAddress);

            parsedArgs = DefaultParse(new[] { @"/platform:x64", @"/baseaddress:0x0000000100000000", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new[] { @"/platform:x64", @"/baseaddress:0xffff8000", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new[] { "test.cs", "/platform:x86", "/baseaddress:0xffffffff" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadBaseNumber).WithArguments("0xFFFFFFFF"));

            parsedArgs = DefaultParse(new[] { "test.cs", "/platform:x86", "/baseaddress:0xffff8000" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadBaseNumber).WithArguments("0xFFFF8000"));

            parsedArgs = DefaultParse(new[] { "test.cs", "/baseaddress:0xffff8000" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadBaseNumber).WithArguments("0xFFFF8000"));

            parsedArgs = DefaultParse(new[] { "C:\\test.cs", "/platform:x86", "/baseaddress:0xffff7fff" }, _baseDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new[] { "C:\\test.cs", "/platform:x64", "/baseaddress:0xffff8000" }, _baseDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new[] { "C:\\test.cs", "/platform:x64", "/baseaddress:0x100000000" }, _baseDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new[] { "test.cs", "/baseaddress:0xFFFF0000FFFF0000" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadBaseNumber).WithArguments("0xFFFF0000FFFF0000"));

            parsedArgs = DefaultParse(new[] { "C:\\test.cs", "/platform:x64", "/baseaddress:0x10000000000000000" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadBaseNumber).WithArguments("0x10000000000000000"));

            parsedArgs = DefaultParse(new[] { "C:\\test.cs", "/baseaddress:0xFFFF0000FFFF0000" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadBaseNumber).WithArguments("0xFFFF0000FFFF0000"));
        }

        [Fact]
        public void ParseFileAlignment()
        {
            var parsedArgs = DefaultParse(new[] { @"/filealign:x64", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2024: Invalid file section alignment number 'x64'
                Diagnostic(ErrorCode.ERR_InvalidFileAlignment).WithArguments("x64"));

            parsedArgs = DefaultParse(new[] { @"/filealign:0x200", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(0x200, parsedArgs.EmitOptions.FileAlignment);

            parsedArgs = DefaultParse(new[] { @"/filealign:512", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(512, parsedArgs.EmitOptions.FileAlignment);

            parsedArgs = DefaultParse(new[] { @"/filealign:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2035: Command-line syntax error: Missing ':<number>' for 'filealign' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsNumber).WithArguments("filealign"));

            parsedArgs = DefaultParse(new[] { @"/filealign:-23", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2024: Invalid file section alignment number '-23'
                Diagnostic(ErrorCode.ERR_InvalidFileAlignment).WithArguments("-23"));

            parsedArgs = DefaultParse(new[] { @"/filealign:020000", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(8192, parsedArgs.EmitOptions.FileAlignment);

            parsedArgs = DefaultParse(new[] { @"/filealign:0", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2024: Invalid file section alignment number '0'
                Diagnostic(ErrorCode.ERR_InvalidFileAlignment).WithArguments("0"));

            parsedArgs = DefaultParse(new[] { @"/filealign:123", "a.cs" }, _baseDirectory);
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

            var parsedArgs = DefaultParse(new[] { @"/lib:lib1", @"/libpath:lib2", @"/libpaths:lib3", "a.cs" }, dir.Path);
            AssertEx.Equal(new[]
            {
                s_defaultSdkDirectory,
                lib1.Path,
                lib2.Path,
                lib3.Path
            }, parsedArgs.ReferencePaths);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void SdkPathAndLibEnvVariable_Errors()
        {
            var parsedArgs = DefaultParse(new[] { @"/lib:c:lib2", @"/lib:o:\sdk1", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                // warning CS1668: Invalid search path 'c:lib2' specified in '/LIB option' -- 'path is too long or invalid'
                Diagnostic(ErrorCode.WRN_InvalidSearchPathDir).WithArguments(@"c:lib2", "/LIB option", "path is too long or invalid"),
                // warning CS1668: Invalid search path 'o:\sdk1' specified in '/LIB option' -- 'directory does not exist'
                Diagnostic(ErrorCode.WRN_InvalidSearchPathDir).WithArguments(@"o:\sdk1", "/LIB option", "directory does not exist"));

            parsedArgs = DefaultParse(new[] { @"/lib:c:\Windows,o:\Windows;e:;", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                // warning CS1668: Invalid search path 'o:\Windows' specified in '/LIB option' -- 'directory does not exist'
                Diagnostic(ErrorCode.WRN_InvalidSearchPathDir).WithArguments(@"o:\Windows", "/LIB option", "directory does not exist"),
                // warning CS1668: Invalid search path 'e:' specified in '/LIB option' -- 'path is too long or invalid'
                Diagnostic(ErrorCode.WRN_InvalidSearchPathDir).WithArguments(@"e:", "/LIB option", "path is too long or invalid"));

            parsedArgs = DefaultParse(new[] { @"/lib:c:\Windows,.\Windows;e;", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                // warning CS1668: Invalid search path '.\Windows' specified in '/LIB option' -- 'directory does not exist'
                Diagnostic(ErrorCode.WRN_InvalidSearchPathDir).WithArguments(@".\Windows", "/LIB option", "directory does not exist"),
                // warning CS1668: Invalid search path 'e' specified in '/LIB option' -- 'directory does not exist'
                Diagnostic(ErrorCode.WRN_InvalidSearchPathDir).WithArguments(@"e", "/LIB option", "directory does not exist"));

            parsedArgs = DefaultParse(new[] { @"/lib:c:\Windows,o:\Windows;e:; ; ; ; ", "a.cs" }, _baseDirectory);
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

            parsedArgs = DefaultParse(new[] { @"/lib", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<path list>", "lib"));

            parsedArgs = DefaultParse(new[] { @"/lib:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<path list>", "lib"));

            parsedArgs = DefaultParse(new[] { @"/lib+", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/lib+"));

            parsedArgs = DefaultParse(new[] { @"/lib: ", "a.cs" }, _baseDirectory);
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
            int exitCode = new MockCSharpCompiler(null, subDirectory, new[] { "/nologo", "/t:library", "/out:abc.xyz", src.ToString() }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString().Trim());

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = new MockCSharpCompiler(null, baseDirectory, new[] { "/nologo", "/lib:temp", "/r:abc.xyz", "/t:library", src.ToString() }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString().Trim());

            CleanupAllGeneratedFiles(src.Path);
        }

        [Fact]
        public void UnableWriteOutput()
        {
            var tempFolder = Temp.CreateDirectory();
            var baseDirectory = tempFolder.ToString();
            var subFolder = tempFolder.CreateDirectory("temp");

            var src = Temp.CreateFile("a.cs");
            src.WriteAllText("public class C{}");

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            int exitCode = new MockCSharpCompiler(null, baseDirectory, new[] { "/nologo", "/preferreduilang:en", "/t:library", "/out:" + subFolder.ToString(), src.ToString() }).Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.True(outWriter.ToString().Trim().StartsWith("error CS2012: Cannot open '" + subFolder.ToString() + "' for writing -- '", StringComparison.Ordinal)); // Cannot create a file when that file already exists.

            CleanupAllGeneratedFiles(src.Path);
        }

        [Fact]
        public void ParseHighEntropyVA()
        {
            var parsedArgs = DefaultParse(new[] { @"/highentropyva", "a.cs" }, _baseDirectory);
            Assert.False(parsedArgs.Errors.Any());
            Assert.True(parsedArgs.EmitOptions.HighEntropyVirtualAddressSpace);
            parsedArgs = DefaultParse(new[] { @"/highentropyva+", "a.cs" }, _baseDirectory);
            Assert.False(parsedArgs.Errors.Any());
            Assert.True(parsedArgs.EmitOptions.HighEntropyVirtualAddressSpace);
            parsedArgs = DefaultParse(new[] { @"/highentropyva-", "a.cs" }, _baseDirectory);
            Assert.False(parsedArgs.Errors.Any());
            Assert.False(parsedArgs.EmitOptions.HighEntropyVirtualAddressSpace);
            parsedArgs = DefaultParse(new[] { @"/highentropyva:-", "a.cs" }, _baseDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal(EmitOptions.Default.HighEntropyVirtualAddressSpace, parsedArgs.EmitOptions.HighEntropyVirtualAddressSpace);

            parsedArgs = DefaultParse(new[] { @"/highentropyva:", "a.cs" }, _baseDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal(EmitOptions.Default.HighEntropyVirtualAddressSpace, parsedArgs.EmitOptions.HighEntropyVirtualAddressSpace);

            //last one wins
            parsedArgs = DefaultParse(new[] { @"/highenTROPyva+", @"/HIGHentropyva-", "a.cs" }, _baseDirectory);
            Assert.False(parsedArgs.Errors.Any());
            Assert.False(parsedArgs.EmitOptions.HighEntropyVirtualAddressSpace);
        }

        [Fact]
        public void Checked()
        {
            var parsedArgs = DefaultParse(new[] { @"/checked+", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.CompilationOptions.CheckOverflow);

            parsedArgs = DefaultParse(new[] { @"/checked-", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.CompilationOptions.CheckOverflow);

            parsedArgs = DefaultParse(new[] { @"/checked", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.CompilationOptions.CheckOverflow);

            parsedArgs = DefaultParse(new[] { @"/checked-", @"/checked", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.CompilationOptions.CheckOverflow);

            parsedArgs = DefaultParse(new[] { @"/checked:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/checked:"));
        }

        [Fact]
        public void Usings()
        {
            CSharpCommandLineArguments parsedArgs;

            parsedArgs = CSharpCommandLineParser.ScriptRunner.Parse(new string[] { "/u:Foo.Bar" }, _baseDirectory, s_defaultSdkDirectory);
            parsedArgs.Errors.Verify();
            AssertEx.Equal(new[] { "Foo.Bar" }, parsedArgs.CompilationOptions.Usings.AsEnumerable());

            parsedArgs = CSharpCommandLineParser.ScriptRunner.Parse(new string[] { "/u:Foo.Bar;Baz", "/using:System.Core;System" }, _baseDirectory, s_defaultSdkDirectory);
            parsedArgs.Errors.Verify();
            AssertEx.Equal(new[] { "Foo.Bar", "Baz", "System.Core", "System" }, parsedArgs.CompilationOptions.Usings.AsEnumerable());

            parsedArgs = CSharpCommandLineParser.ScriptRunner.Parse(new string[] { "/u:Foo;;Bar" }, _baseDirectory, s_defaultSdkDirectory);
            parsedArgs.Errors.Verify();
            AssertEx.Equal(new[] { "Foo", "Bar" }, parsedArgs.CompilationOptions.Usings.AsEnumerable());

            parsedArgs = CSharpCommandLineParser.ScriptRunner.Parse(new string[] { "/u:" }, _baseDirectory, s_defaultSdkDirectory);
            parsedArgs.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing '<namespace>' for '/u:' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<namespace>", "/u:"));
        }

        [Fact]
        public void WarningsErrors()
        {
            var parsedArgs = DefaultParse(new string[] { "/nowarn", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2035: Command-line syntax error: Missing ':<number>' for 'nowarn' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsNumber).WithArguments("nowarn"));

            parsedArgs = DefaultParse(new string[] { "/nowarn:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2035: Command-line syntax error: Missing ':<number>' for 'nowarn' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsNumber).WithArguments("nowarn"));

            // Previous versions of the compiler used to report a warning (CS1691)
            // whenever an unrecognized warning code was supplied via /nowarn or /warnaserror.
            // We no longer generate a warning in such cases.
            parsedArgs = DefaultParse(new string[] { "/nowarn:-1", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new string[] { "/nowarn:abc", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new string[] { "/warnaserror:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2035: Command-line syntax error: Missing ':<number>' for 'warnaserror' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsNumber).WithArguments("warnaserror"));

            parsedArgs = DefaultParse(new string[] { "/warnaserror:-1", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new string[] { "/warnaserror:70000", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new string[] { "/warnaserror:abc", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new string[] { "/warnaserror+:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2035: Command-line syntax error: Missing ':<number>' for '/warnaserror+:' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsNumber).WithArguments("warnaserror+"));

            parsedArgs = DefaultParse(new string[] { "/warnaserror-:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2035: Command-line syntax error: Missing ':<number>' for '/warnaserror-:' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsNumber).WithArguments("warnaserror-"));

            parsedArgs = DefaultParse(new string[] { "/w", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2035: Command-line syntax error: Missing ':<number>' for '/w' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsNumber).WithArguments("w"));

            parsedArgs = DefaultParse(new string[] { "/w:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2035: Command-line syntax error: Missing ':<number>' for '/w:' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsNumber).WithArguments("w"));

            parsedArgs = DefaultParse(new string[] { "/warn:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2035: Command-line syntax error: Missing ':<number>' for '/warn:' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsNumber).WithArguments("warn"));

            parsedArgs = DefaultParse(new string[] { "/w:-1", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS1900: Warning level must be in the range 0-4
                Diagnostic(ErrorCode.ERR_BadWarningLevel).WithArguments("w"));

            parsedArgs = DefaultParse(new string[] { "/w:5", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS1900: Warning level must be in the range 0-4
                Diagnostic(ErrorCode.ERR_BadWarningLevel).WithArguments("w"));

            parsedArgs = DefaultParse(new string[] { "/warn:-1", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS1900: Warning level must be in the range 0-4
                Diagnostic(ErrorCode.ERR_BadWarningLevel).WithArguments("warn"));

            parsedArgs = DefaultParse(new string[] { "/warn:5", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS1900: Warning level must be in the range 0-4
                Diagnostic(ErrorCode.ERR_BadWarningLevel).WithArguments("warn"));

            // Previous versions of the compiler used to report a warning (CS1691)
            // whenever an unrecognized warning code was supplied via /nowarn or /warnaserror.
            // We no longer generate a warning in such cases.
            parsedArgs = DefaultParse(new string[] { "/warnaserror:1,2,3", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new string[] { "/nowarn:1,2,3", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new string[] { "/nowarn:1;2;;3", "a.cs" }, _baseDirectory);
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
            var parsedArgs = DefaultParse(new string[] { "/warnaserror", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ReportDiagnostic.Error, parsedArgs.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(4, parsedArgs.CompilationOptions.WarningLevel);
            Assert.Equal(0, parsedArgs.CompilationOptions.SpecificDiagnosticOptions.Count);

            parsedArgs = DefaultParse(new string[] { "/warnaserror:1062,1066,1734", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(4, parsedArgs.CompilationOptions.WarningLevel);
            AssertSpecificDiagnostics(new[] { 1062, 1066, 1734 }, new[] { ReportDiagnostic.Error, ReportDiagnostic.Error, ReportDiagnostic.Error }, parsedArgs);

            parsedArgs = DefaultParse(new string[] { "/warnaserror:+1062,+1066,+1734", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(4, parsedArgs.CompilationOptions.WarningLevel);
            AssertSpecificDiagnostics(new[] { 1062, 1066, 1734 }, new[] { ReportDiagnostic.Error, ReportDiagnostic.Error, ReportDiagnostic.Error }, parsedArgs);

            parsedArgs = DefaultParse(new string[] { "/warnaserror+", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ReportDiagnostic.Error, parsedArgs.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(4, parsedArgs.CompilationOptions.WarningLevel);
            AssertSpecificDiagnostics(new int[0], new ReportDiagnostic[0], parsedArgs);

            parsedArgs = DefaultParse(new string[] { "/warnaserror+:1062,1066,1734", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(4, parsedArgs.CompilationOptions.WarningLevel);
            AssertSpecificDiagnostics(new[] { 1062, 1066, 1734 }, new[] { ReportDiagnostic.Error, ReportDiagnostic.Error, ReportDiagnostic.Error }, parsedArgs);

            parsedArgs = DefaultParse(new string[] { "/warnaserror-", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(4, parsedArgs.CompilationOptions.WarningLevel);
            AssertSpecificDiagnostics(new int[0], new ReportDiagnostic[0], parsedArgs);

            parsedArgs = DefaultParse(new string[] { "/warnaserror-:1062,1066,1734", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(4, parsedArgs.CompilationOptions.WarningLevel);
            AssertSpecificDiagnostics(new[] { 1062, 1066, 1734 }, new[] { ReportDiagnostic.Default, ReportDiagnostic.Default, ReportDiagnostic.Default }, parsedArgs);

            parsedArgs = DefaultParse(new string[] { "/warnaserror+:1062,1066,1734", "/warnaserror-:1762,1974", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(4, parsedArgs.CompilationOptions.WarningLevel);
            AssertSpecificDiagnostics(
                new[] { 1062, 1066, 1734, 1762, 1974 },
                new[] { ReportDiagnostic.Error, ReportDiagnostic.Error, ReportDiagnostic.Error, ReportDiagnostic.Default, ReportDiagnostic.Default },
                parsedArgs);

            parsedArgs = DefaultParse(new string[] { "/warnaserror+:1062,1066,1734", "/warnaserror-:1062,1974", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(4, parsedArgs.CompilationOptions.WarningLevel);
            Assert.Equal(4, parsedArgs.CompilationOptions.SpecificDiagnosticOptions.Count);
            AssertSpecificDiagnostics(new[] { 1062, 1066, 1734, 1974 }, new[] { ReportDiagnostic.Default, ReportDiagnostic.Error, ReportDiagnostic.Error, ReportDiagnostic.Default }, parsedArgs);

            parsedArgs = DefaultParse(new string[] { "/warnaserror-:1062,1066,1734", "/warnaserror+:1062,1974", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(4, parsedArgs.CompilationOptions.WarningLevel);
            AssertSpecificDiagnostics(new[] { 1062, 1066, 1734, 1974 }, new[] { ReportDiagnostic.Error, ReportDiagnostic.Default, ReportDiagnostic.Default, ReportDiagnostic.Error }, parsedArgs);

            parsedArgs = DefaultParse(new string[] { "/w:1", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(1, parsedArgs.CompilationOptions.WarningLevel);
            AssertSpecificDiagnostics(new int[0], new ReportDiagnostic[0], parsedArgs);

            parsedArgs = DefaultParse(new string[] { "/warn:1", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(1, parsedArgs.CompilationOptions.WarningLevel);
            AssertSpecificDiagnostics(new int[0], new ReportDiagnostic[0], parsedArgs);

            parsedArgs = DefaultParse(new string[] { "/warn:1", "/warnaserror+:1062,1974", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(1, parsedArgs.CompilationOptions.WarningLevel);
            AssertSpecificDiagnostics(new[] { 1062, 1974 }, new[] { ReportDiagnostic.Error, ReportDiagnostic.Error }, parsedArgs);

            parsedArgs = DefaultParse(new string[] { "/nowarn:1062,1066,1734", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(4, parsedArgs.CompilationOptions.WarningLevel);
            AssertSpecificDiagnostics(new[] { 1062, 1066, 1734 }, new[] { ReportDiagnostic.Suppress, ReportDiagnostic.Suppress, ReportDiagnostic.Suppress }, parsedArgs);

            parsedArgs = DefaultParse(new string[] { @"/nowarn:""1062 1066 1734""", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(4, parsedArgs.CompilationOptions.WarningLevel);
            AssertSpecificDiagnostics(new[] { 1062, 1066, 1734 }, new[] { ReportDiagnostic.Suppress, ReportDiagnostic.Suppress, ReportDiagnostic.Suppress }, parsedArgs);

            parsedArgs = DefaultParse(new string[] { "/nowarn:1062,1066,1734", "/warnaserror:1066,1762", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(4, parsedArgs.CompilationOptions.WarningLevel);
            AssertSpecificDiagnostics(new[] { 1062, 1066, 1734, 1762 }, new[] { ReportDiagnostic.Suppress, ReportDiagnostic.Suppress, ReportDiagnostic.Suppress, ReportDiagnostic.Error }, parsedArgs);

            parsedArgs = DefaultParse(new string[] { "/warnaserror:1066,1762", "/nowarn:1062,1066,1734", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ReportDiagnostic.Default, parsedArgs.CompilationOptions.GeneralDiagnosticOption);
            Assert.Equal(4, parsedArgs.CompilationOptions.WarningLevel);
            AssertSpecificDiagnostics(new[] { 1062, 1066, 1734, 1762 }, new[] { ReportDiagnostic.Suppress, ReportDiagnostic.Suppress, ReportDiagnostic.Suppress, ReportDiagnostic.Error }, parsedArgs);
        }

        [Fact]
        public void AllowUnsafe()
        {
            CSharpCommandLineArguments parsedArgs = DefaultParse(new[] { "/unsafe", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.CompilationOptions.AllowUnsafe);

            parsedArgs = DefaultParse(new[] { "/unsafe+", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.CompilationOptions.AllowUnsafe);

            parsedArgs = DefaultParse(new[] { "/UNSAFE-", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.CompilationOptions.AllowUnsafe);

            parsedArgs = DefaultParse(new[] { "/unsafe-", "/unsafe+", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.CompilationOptions.AllowUnsafe);

            parsedArgs = DefaultParse(new[] { "a.cs" }, _baseDirectory); // default
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.CompilationOptions.AllowUnsafe);

            parsedArgs = DefaultParse(new[] { "/unsafe:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/unsafe:"));

            parsedArgs = DefaultParse(new[] { "/unsafe:+", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/unsafe:+"));

            parsedArgs = DefaultParse(new[] { "/unsafe-:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/unsafe-:"));
        }

        [Fact]
        public void DelaySign()
        {
            CSharpCommandLineArguments parsedArgs;

            parsedArgs = DefaultParse(new[] { "/delaysign", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.NotNull(parsedArgs.CompilationOptions.DelaySign);
            Assert.True((bool)parsedArgs.CompilationOptions.DelaySign);

            parsedArgs = DefaultParse(new[] { "/delaysign+", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.NotNull(parsedArgs.CompilationOptions.DelaySign);
            Assert.True((bool)parsedArgs.CompilationOptions.DelaySign);

            parsedArgs = DefaultParse(new[] { "/DELAYsign-", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.NotNull(parsedArgs.CompilationOptions.DelaySign);
            Assert.False((bool)parsedArgs.CompilationOptions.DelaySign);

            parsedArgs = DefaultParse(new[] { "/delaysign:-", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2007: Unrecognized option: '/delaysign:-'
                Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/delaysign:-"));

            Assert.Null(parsedArgs.CompilationOptions.DelaySign);
        }

        [Fact]
        public void PublicSign()
        {
            var parsedArgs = DefaultParse(new[] { "/publicsign", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.CompilationOptions.PublicSign);

            parsedArgs = DefaultParse(new[] { "/publicsign+", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.True(parsedArgs.CompilationOptions.PublicSign);

            parsedArgs = DefaultParse(new[] { "/PUBLICsign-", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.False(parsedArgs.CompilationOptions.PublicSign);

            parsedArgs = DefaultParse(new[] { "/publicsign:-", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2007: Unrecognized option: '/publicsign:-'
                Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/publicsign:-").WithLocation(1, 1));

            Assert.False(parsedArgs.CompilationOptions.PublicSign);
        }

        [WorkItem(546301, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546301")]
        [Fact]
        public void SubsystemVersionTests()
        {
            CSharpCommandLineArguments parsedArgs = DefaultParse(new[] { "/subsystemversion:4.0", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(SubsystemVersion.Create(4, 0), parsedArgs.EmitOptions.SubsystemVersion);

            // wrongly supported subsystem version. CompilationOptions data will be faithful to the user input.
            // It is normalized at the time of emit.
            parsedArgs = DefaultParse(new[] { "/subsystemversion:0.0", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(); // no error in Dev11
            Assert.Equal(SubsystemVersion.Create(0, 0), parsedArgs.EmitOptions.SubsystemVersion);

            parsedArgs = DefaultParse(new[] { "/subsystemversion:0", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(); // no error in Dev11
            Assert.Equal(SubsystemVersion.Create(0, 0), parsedArgs.EmitOptions.SubsystemVersion);

            parsedArgs = DefaultParse(new[] { "/subsystemversion:3.99", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(); // no error in Dev11
            Assert.Equal(SubsystemVersion.Create(3, 99), parsedArgs.EmitOptions.SubsystemVersion);

            parsedArgs = DefaultParse(new[] { "/subsystemversion:4.0", "/SUBsystemversion:5.333", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(SubsystemVersion.Create(5, 333), parsedArgs.EmitOptions.SubsystemVersion);

            parsedArgs = DefaultParse(new[] { "/subsystemversion:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "subsystemversion"));

            parsedArgs = DefaultParse(new[] { "/subsystemversion", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "subsystemversion"));

            parsedArgs = DefaultParse(new[] { "/subsystemversion-", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/subsystemversion-"));

            parsedArgs = DefaultParse(new[] { "/subsystemversion: ", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "subsystemversion"));

            parsedArgs = DefaultParse(new[] { "/subsystemversion: 4.1", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_InvalidSubsystemVersion).WithArguments(" 4.1"));

            parsedArgs = DefaultParse(new[] { "/subsystemversion:4 .0", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_InvalidSubsystemVersion).WithArguments("4 .0"));

            parsedArgs = DefaultParse(new[] { "/subsystemversion:4. 0", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_InvalidSubsystemVersion).WithArguments("4. 0"));

            parsedArgs = DefaultParse(new[] { "/subsystemversion:.", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_InvalidSubsystemVersion).WithArguments("."));

            parsedArgs = DefaultParse(new[] { "/subsystemversion:4.", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_InvalidSubsystemVersion).WithArguments("4."));

            parsedArgs = DefaultParse(new[] { "/subsystemversion:.0", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_InvalidSubsystemVersion).WithArguments(".0"));

            parsedArgs = DefaultParse(new[] { "/subsystemversion:4.2 ", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new[] { "/subsystemversion:4.65536", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_InvalidSubsystemVersion).WithArguments("4.65536"));

            parsedArgs = DefaultParse(new[] { "/subsystemversion:65536.0", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_InvalidSubsystemVersion).WithArguments("65536.0"));

            parsedArgs = DefaultParse(new[] { "/subsystemversion:-4.0", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_InvalidSubsystemVersion).WithArguments("-4.0"));

            // TODO: incompatibilities: versions lower than '6.2' and 'arm', 'winmdobj', 'appcontainer'
        }

        [Fact]
        public void MainType()
        {
            CSharpCommandLineArguments parsedArgs = DefaultParse(new[] { "/m:A.B.C", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal("A.B.C", parsedArgs.CompilationOptions.MainTypeName);

            parsedArgs = DefaultParse(new[] { "/m: ", "a.cs" }, _baseDirectory); // Mimicking Dev11
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "m"));
            Assert.Null(parsedArgs.CompilationOptions.MainTypeName);

            //  overriding the value
            parsedArgs = DefaultParse(new[] { "/m:A.B.C", "/MAIN:X.Y.Z", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal("X.Y.Z", parsedArgs.CompilationOptions.MainTypeName);

            //  error
            parsedArgs = DefaultParse(new[] { "/maiN:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "main"));

            parsedArgs = DefaultParse(new[] { "/MAIN+", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/MAIN+"));

            parsedArgs = DefaultParse(new[] { "/M", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "m"));

            //  incompatible values /main && /target
            parsedArgs = DefaultParse(new[] { "/main:a", "/t:library", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_NoMainOnDLL));

            parsedArgs = DefaultParse(new[] { "/main:a", "/t:module", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_NoMainOnDLL));
        }

        [Fact]
        public void Codepage()
        {
            CSharpCommandLineArguments parsedArgs = DefaultParse(new[] { "/CodePage:1200", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal("Unicode", parsedArgs.Encoding.EncodingName);

            parsedArgs = DefaultParse(new[] { "/CodePage:1200", "/codePAGE:65001", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal("Unicode (UTF-8)", parsedArgs.Encoding.EncodingName);

            //  error
            parsedArgs = DefaultParse(new[] { "/codepage:0", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.FTL_BadCodepage).WithArguments("0"));

            parsedArgs = DefaultParse(new[] { "/codepage:abc", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.FTL_BadCodepage).WithArguments("abc"));

            parsedArgs = DefaultParse(new[] { "/codepage:-5", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.FTL_BadCodepage).WithArguments("-5"));

            parsedArgs = DefaultParse(new[] { "/codepage: ", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.FTL_BadCodepage).WithArguments(""));

            parsedArgs = DefaultParse(new[] { "/codepage:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.FTL_BadCodepage).WithArguments(""));

            parsedArgs = DefaultParse(new[] { "/codepage", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "codepage"));

            parsedArgs = DefaultParse(new[] { "/codepage+", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/codepage+"));
        }

        [Fact]
        public void ChecksumAlgorithm()
        {
            CSharpCommandLineArguments parsedArgs = DefaultParse(new[] { "/checksumAlgorithm:sHa1", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(SourceHashAlgorithm.Sha1, parsedArgs.ChecksumAlgorithm);

            parsedArgs = DefaultParse(new[] { "/checksumAlgorithm:sha256", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(SourceHashAlgorithm.Sha256, parsedArgs.ChecksumAlgorithm);

            parsedArgs = DefaultParse(new[] { "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(SourceHashAlgorithm.Sha1, parsedArgs.ChecksumAlgorithm);

            //  error
            parsedArgs = DefaultParse(new[] { "/checksumAlgorithm:256", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.FTL_BadChecksumAlgorithm).WithArguments("256"));

            parsedArgs = DefaultParse(new[] { "/checksumAlgorithm:sha-1", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.FTL_BadChecksumAlgorithm).WithArguments("sha-1"));

            parsedArgs = DefaultParse(new[] { "/checksumAlgorithm:sha", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.FTL_BadChecksumAlgorithm).WithArguments("sha"));

            parsedArgs = DefaultParse(new[] { "/checksumAlgorithm: ", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "checksumalgorithm"));

            parsedArgs = DefaultParse(new[] { "/checksumAlgorithm:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "checksumalgorithm"));

            parsedArgs = DefaultParse(new[] { "/checksumAlgorithm", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "checksumalgorithm"));

            parsedArgs = DefaultParse(new[] { "/checksumAlgorithm+", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/checksumAlgorithm+"));
        }

        [Fact]
        public void AddModule()
        {
            CSharpCommandLineArguments parsedArgs = DefaultParse(new[] { "/noconfig", "/nostdlib", "/addmodule:abc.netmodule", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(1, parsedArgs.MetadataReferences.Length);
            Assert.Equal("abc.netmodule", parsedArgs.MetadataReferences[0].Reference);
            Assert.Equal(MetadataImageKind.Module, parsedArgs.MetadataReferences[0].Properties.Kind);

            parsedArgs = DefaultParse(new[] { "/noconfig", "/nostdlib", "/aDDmodule:c:\\abc;c:\\abc;d:\\xyz", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(3, parsedArgs.MetadataReferences.Length);
            Assert.Equal("c:\\abc", parsedArgs.MetadataReferences[0].Reference);
            Assert.Equal(MetadataImageKind.Module, parsedArgs.MetadataReferences[0].Properties.Kind);
            Assert.Equal("c:\\abc", parsedArgs.MetadataReferences[1].Reference);
            Assert.Equal(MetadataImageKind.Module, parsedArgs.MetadataReferences[1].Properties.Kind);
            Assert.Equal("d:\\xyz", parsedArgs.MetadataReferences[2].Reference);
            Assert.Equal(MetadataImageKind.Module, parsedArgs.MetadataReferences[2].Properties.Kind);

            //  error
            parsedArgs = DefaultParse(new[] { "/ADDMODULE", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "/addmodule:"));

            parsedArgs = DefaultParse(new[] { "/ADDMODULE+", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/ADDMODULE+"));

            parsedArgs = DefaultParse(new[] { "/ADDMODULE:", "a.cs" }, _baseDirectory);
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
            int exitCode = new MockCSharpCompiler(null, baseDir, new[] { "/nologo", "/t:module", source1 }).Run(outWriter);
            Assert.Equal(0, exitCode);

            var modfile = source1.Substring(0, source1.Length - 2) + "netmodule";
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var parsedArgs = DefaultParse(new[] { "/nologo", "/addmodule:" + modfile, source }, _baseDirectory);
            parsedArgs.Errors.Verify();
            exitCode = new MockCSharpCompiler(null, baseDir, new[] { "/nologo", "/addmodule:" + modfile, source }).Run(outWriter);
            Assert.Empty(outWriter.ToString());

            // === Scenario 2 ===
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = new MockCSharpCompiler(null, baseDir, new[] { "/nologo", "/t:module", source2 }).Run(outWriter);
            Assert.Equal(0, exitCode);

            modfile = source2.Substring(0, source2.Length - 2) + "netmodule";
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            parsedArgs = DefaultParse(new[] { "/nologo", "/addmodule:" + modfile, source }, _baseDirectory);
            parsedArgs.Errors.Verify();
            exitCode = new MockCSharpCompiler(null, baseDir, new[] { "/nologo", "/preferreduilang:en", "/addmodule:" + modfile, source }).Run(outWriter);
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
            int exitCode = new MockCSharpCompiler(null, baseDir, new[] { "/nologo", "/t:module", source1 }).Run(outWriter);
            Assert.Equal(0, exitCode);

            var modfile = source1.Substring(0, source1.Length - 2) + "netmodule";
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = new MockCSharpCompiler(null, baseDir, new[] { "/nologo", "/addmodule:" + modfile, source2 }).Run(outWriter);
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
            int exitCode = new MockCSharpCompiler(null, baseDir, new[] { "/nologo", "/t:module", source1 }).Run(outWriter);
            Assert.Equal(0, exitCode);

            var modfile = source1.Substring(0, source1.Length - 2) + "netmodule";
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = new MockCSharpCompiler(null, baseDir, new[] { "/nologo", "/preferreduilang:en", "/addmodule:" + modfile, "/linkres:" + modfile, source2 }).Run(outWriter);
            Assert.Equal(1, exitCode);
            // Native gives CS0013 at emit stage
            Assert.Equal("error CS7041: Each linked resource and module must have a unique filename. Filename '" + Path.GetFileName(modfile) + "' is specified more than once in this assembly", outWriter.ToString().Trim());

            CleanupAllGeneratedFiles(source1);
            CleanupAllGeneratedFiles(source2);
        }

        [Fact]
        public void Utf8Output()
        {
            CSharpCommandLineArguments parsedArgs = DefaultParse(new[] { "/utf8output", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.True((bool)parsedArgs.Utf8Output);

            parsedArgs = DefaultParse(new[] { "/utf8output", "/utf8output", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.True((bool)parsedArgs.Utf8Output);

            parsedArgs = DefaultParse(new[] { "/utf8output:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/utf8output:"));
        }

        [ConditionalFact(typeof(WindowsOnly))]
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

        [ConditionalFact(typeof(WindowsOnly))]
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
        [ConditionalFact(typeof(WindowsOnly))]
        public void NoSourcesWithModule()
        {
            var folder = Temp.CreateDirectory();
            var aCs = folder.CreateFile("a.cs");
            aCs.WriteAllText("public class C {}");

            var output = ProcessUtilities.RunAndGetOutput(s_CSharpCompilerExecutable, "/nologo /t:module /out:a.netmodule " + aCs, startFolder: folder.ToString());
            Assert.Equal("", output.Trim());

            output = ProcessUtilities.RunAndGetOutput(s_CSharpCompilerExecutable, "/nologo /t:library /out:b.dll /addmodule:a.netmodule ", startFolder: folder.ToString());
            Assert.Equal("", output.Trim());

            output = ProcessUtilities.RunAndGetOutput(s_CSharpCompilerExecutable, "/nologo /preferreduilang:en /t:module /out:b.dll /addmodule:a.netmodule ", startFolder: folder.ToString());
            Assert.Equal("warning CS2008: No source files specified.", output.Trim());

            CleanupAllGeneratedFiles(aCs.Path);
        }

        [WorkItem(546653, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546653")]
        [ConditionalFact(typeof(WindowsOnly))]
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
        [ConditionalFact(typeof(WindowsOnly))]
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
            CSharpCommandLineArguments parsedArgs = DefaultParse(new[] { "/keycontainer:RIPAdamYauch", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal("RIPAdamYauch", parsedArgs.CompilationOptions.CryptoKeyContainer);

            parsedArgs = DefaultParse(new[] { "/keycontainer", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing '<text>' for 'keycontainer' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "keycontainer"));
            Assert.Null(parsedArgs.CompilationOptions.CryptoKeyContainer);

            parsedArgs = DefaultParse(new[] { "/keycontainer-", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2007: Unrecognized option: '/keycontainer-'
                Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/keycontainer-"));
            Assert.Null(parsedArgs.CompilationOptions.CryptoKeyContainer);

            parsedArgs = DefaultParse(new[] { "/keycontainer:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2006: Command-line syntax error: Missing '<text>' for 'keycontainer' option
                Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "keycontainer"));
            Assert.Null(parsedArgs.CompilationOptions.CryptoKeyContainer);

            parsedArgs = DefaultParse(new[] { "/keycontainer: ", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<text>", "keycontainer"));
            Assert.Null(parsedArgs.CompilationOptions.CryptoKeyContainer);

            // KEYFILE
            parsedArgs = DefaultParse(new[] { @"/keyfile:\somepath\s""ome Fil""e.foo.bar", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            //EDMAURER let's not set the option in the event that there was an error.
            //Assert.Equal(@"\somepath\some File.foo.bar", parsedArgs.CompilationOptions.CryptoKeyFile);

            parsedArgs = DefaultParse(new[] { "/keyFile", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2005: Missing file specification for 'keyfile' option
                Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("keyfile"));
            Assert.Null(parsedArgs.CompilationOptions.CryptoKeyFile);

            parsedArgs = DefaultParse(new[] { "/keyFile: ", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(Diagnostic(ErrorCode.ERR_NoFileSpec).WithArguments("keyfile"));
            Assert.Null(parsedArgs.CompilationOptions.CryptoKeyFile);

            parsedArgs = DefaultParse(new[] { "/keyfile-", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify(
                // error CS2007: Unrecognized option: '/keyfile-'
                Diagnostic(ErrorCode.ERR_BadSwitch).WithArguments("/keyfile-"));
            Assert.Null(parsedArgs.CompilationOptions.CryptoKeyFile);

            // DEFAULTS
            parsedArgs = DefaultParse(new[] { "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Null(parsedArgs.CompilationOptions.CryptoKeyFile);
            Assert.Null(parsedArgs.CompilationOptions.CryptoKeyContainer);

            // KEYFILE | KEYCONTAINER conflicts
            parsedArgs = DefaultParse(new[] { "/keyFile:a", "/keyContainer:b", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal("a", parsedArgs.CompilationOptions.CryptoKeyFile);
            Assert.Equal("b", parsedArgs.CompilationOptions.CryptoKeyContainer);

            parsedArgs = DefaultParse(new[] { "/keyContainer:b", "/keyFile:a", "a.cs" }, _baseDirectory);
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

            CSharpCommandLineArguments parsedArgs = DefaultParse(new[] { "/t:library", kfile, "CS1698a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new[] { "/t:library", kfile, "/r:" + cs1698a.Path, "CS1698b.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();

            parsedArgs = DefaultParse(new[] { "/t:library", kfile, "/r:" + cs1698b.Path, "/out:" + cs1698a.Path, "CS1698.cs" }, _baseDirectory);

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

        [Fact]
        public void BinaryFileErrorTest()
        {
            var binaryPath = Temp.CreateFile().WriteAllBytes(TestResources.NetFX.v4_0_30319.mscorlib).Path;
            var csc = new MockCSharpCompiler(null, _baseDirectory, new[] { "/nologo", "/preferreduilang:en", binaryPath });
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            int exitCode = csc.Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Equal(
                "error CS2015: '" + binaryPath + "' is a binary file instead of a text file",
                outWriter.ToString().Trim());

            CleanupAllGeneratedFiles(binaryPath);
        }


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
            var csc = new MockCSharpCompiler(rsp, _baseDirectory, new[] { source, "/preferreduilang:en" });
            int exitCode = csc.Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Equal("error CS1680: Invalid reference alias option: 'myAlias=' -- missing filename", outWriter.ToString().Trim());

            CleanupAllGeneratedFiles(source);
            CleanupAllGeneratedFiles(rsp);
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
            var csc = new MockCSharpCompiler(rsp, _baseDirectory, new[] { source, "/preferreduilang:en" });
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
            var csc = new MockCSharpCompiler(rsp, _baseDirectory, new[] { source, "/preferreduilang:en" });
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
            var csc = new MockCSharpCompiler(rsp, _baseDirectory, new[] { source, "/preferreduilang:en" });
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
                @"  /o:""foo.cs"" /o:""abc def""\baz ""/o:baz bar""bing",
            };
            args = CSharpCommandLineParser.ParseResponseLines(responseFile);
            AssertEx.Equal(new[] { @"/o:""foo.cs""", @"/o:""abc def""\baz", @"""/o:baz bar""bing" }, args);
        }

        [ConditionalFact(typeof(WindowsOnly))]
        private void SourceFileQuoting()
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
            var csc = new MockCSharpCompiler(null, dir.Path, new[] { "/nologo", "/preferreduilang:en", "/target:exe", "a.cs" });
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
            var csc = new MockCSharpCompiler(null, dir.Path, new[] { "/nologo", "/target:exe", "a.cs" });
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
            var csc = new MockCSharpCompiler(null, dir.Path, new[] { "/nologo", "/target:library", "a.cs" });
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
            var csc = new MockCSharpCompiler(null, dir.Path, new[] { "/target:library", "/preferreduilang:en", "a.cs" });
            int exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal(@"
Microsoft (R) Visual C# Compiler version A.B.C.D
Copyright (C) Microsoft Corporation. All rights reserved.".Trim(),
                Regex.Replace(outWriter.ToString().Trim(), "version \\d+\\.\\d+\\.\\d+(\\.\\d+)?", "version A.B.C.D"));
            // Privately queued builds have 3-part version numbers instead of 4.  Since we're throwing away the version number,
            // making the last part optional will fix this.

            CleanupAllGeneratedFiles(file.Path);
        }

        private void CheckOutputFileName(string source1, string source2, string inputName1, string inputName2, string[] commandLineArguments, string expectedOutputName)
        {
            var dir = Temp.CreateDirectory();

            var file1 = dir.CreateFile(inputName1);
            file1.WriteAllText(source1);

            var file2 = dir.CreateFile(inputName2);
            file2.WriteAllText(source2);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = new MockCSharpCompiler(null, dir.Path, commandLineArguments.Concat(new[] { inputName1, inputName2 }).ToArray());
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
            var csc = new MockCSharpCompiler(null, dir.Path, new[] { "/nologo", "/preferreduilang:en", "/r:missing.dll", "a.cs" });
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
            var csc = new MockCSharpCompiler(null, dir.Path, commandLineArguments.Concat(new[] { fileName }).ToArray());
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
            var csc = new MockCSharpCompiler(null, dir.Path, new[] { fileName, "/preferreduilang:en", "/target:exe", "/out:sub\\a.exe" });
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
            var csc = new MockCSharpCompiler(null, dir.Path, new[] { fileName, "/preferreduilang:en", "/target:exe", "/out:sub\\" });
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
            var csc = new MockCSharpCompiler(null, dir.Path, new[] { fileName, "/preferreduilang:en", "/target:exe", "/out:sub\\ " });
            int exitCode = csc.Run(outWriter);

            Assert.Equal(1, exitCode);
            var message = outWriter.ToString();
            Assert.Contains("error CS2021: File name", message, StringComparison.Ordinal);
            Assert.Contains("sub", message, StringComparison.Ordinal);

            CleanupAllGeneratedFiles(file.Path);
        }

        [WorkItem(545247, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545247")]
        [ConditionalFact(typeof(WindowsOnly))]
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
            var csc = new MockCSharpCompiler(null, dir.Path, new[] { fileName, "/preferreduilang:en", "/target:exe", "/out:aaa:\\a.exe" });
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
            var csc = new MockCSharpCompiler(null, dir.Path, new[] { fileName, "/preferreduilang:en", "/target:exe", "/out: " });
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

        [ConditionalFact(typeof(WindowsOnly))]
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

            var output = ProcessUtilities.RunAndGetOutput(s_CSharpCompilerExecutable, "/nologo /t:library " + file, startFolder: dir.Path);
            Assert.Equal("", output); // Autodetected UTF8, NO ERROR

            output = ProcessUtilities.RunAndGetOutput(s_CSharpCompilerExecutable, "/nologo /preferreduilang:en /t:library /codepage:20127 " + file, expectedRetCode: 1, startFolder: dir.Path); // 20127: US-ASCII
            // 0xd0, 0x96 ==> ERROR
            Assert.Equal(@"
a.cs(1,7): error CS1001: Identifier expected
a.cs(1,7): error CS1514: { expected
a.cs(1,7): error CS1513: } expected
a.cs(1,7): error CS1022: Type or namespace definition, or end-of-file expected
a.cs(1,10): error CS1022: Type or namespace definition, or end-of-file expected".Trim(),
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
                csc = new MockCSharpCompiler(null, dir.Path, new[]
                {
                    string.Format("/target:{0}", target),
                    string.Format("/out:{0}", outputFileName),
                    Path.GetFileName(sourceFile.Path),
                });
            }
            else
            {
                var manifestFile = dir.CreateFile("Test.config").WriteAllText(explicitManifest);
                csc = new MockCSharpCompiler(null, dir.Path, new[]
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
        [ClrOnlyFact]
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
            var csc = new MockCSharpCompiler(rsp, _baseDirectory, new[] { source, "/preferreduilang:en" });
            int exitCode = csc.Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Contains("error CS0168: The variable 'x' is declared but never used\r\n", outWriter.ToString(), StringComparison.Ordinal);

            // Checks the case with /noconfig (expect to see warning, instead of error)
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            csc = new MockCSharpCompiler(rsp, _baseDirectory, new[] { source, "/noconfig", "/preferreduilang:en" });
            exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Contains("warning CS0168: The variable 'x' is declared but never used\r\n", outWriter.ToString(), StringComparison.Ordinal);

            // Checks the case with /NOCONFIG (expect to see warning, instead of error)
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            csc = new MockCSharpCompiler(rsp, _baseDirectory, new[] { source, "/NOCONFIG", "/preferreduilang:en" });
            exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Contains("warning CS0168: The variable 'x' is declared but never used\r\n", outWriter.ToString(), StringComparison.Ordinal);

            // Checks the case with -noconfig (expect to see warning, instead of error)
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            csc = new MockCSharpCompiler(rsp, _baseDirectory, new[] { source, "-noconfig", "/preferreduilang:en" });
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
            var csc = new MockCSharpCompiler(rsp, _baseDirectory, new[] { source, "/preferreduilang:en" });
            int exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Contains("warning CS2023: Ignoring /noconfig option because it was specified in a response file\r\n", outWriter.ToString(), StringComparison.Ordinal);

            // Checks the case with /noconfig inside the response file as along with /nowarn (expect to see warning)
            // to verify that this warning is not suppressed by the /nowarn option (See MSDN).
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            csc = new MockCSharpCompiler(rsp, _baseDirectory, new[] { source, "/preferreduilang:en", "/nowarn:2023" });
            exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Contains("warning CS2023: Ignoring /noconfig option because it was specified in a response file\r\n", outWriter.ToString(), StringComparison.Ordinal);

            CleanupAllGeneratedFiles(source);
            CleanupAllGeneratedFiles(rsp);
        }

        [WorkItem(544926, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544926")]
        [ClrOnlyFact]
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
            var csc = new MockCSharpCompiler(rsp, _baseDirectory, new[] { source, "/preferreduilang:en" });
            int exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Contains("warning CS2023: Ignoring /noconfig option because it was specified in a response file\r\n", outWriter.ToString(), StringComparison.Ordinal);

            // Checks the case with /NOCONFIG inside the response file as along with /nowarn (expect to see warning)
            // to verify that this warning is not suppressed by the /nowarn option (See MSDN).
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            csc = new MockCSharpCompiler(rsp, _baseDirectory, new[] { source, "/preferreduilang:en", "/nowarn:2023" });
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
            var csc = new MockCSharpCompiler(rsp, _baseDirectory, new[] { source, "/preferreduilang:en" });
            int exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Contains("warning CS2023: Ignoring /noconfig option because it was specified in a response file\r\n", outWriter.ToString(), StringComparison.Ordinal);

            // Checks the case with -noconfig inside the response file as along with /nowarn (expect to see warning)
            // to verify that this warning is not suppressed by the /nowarn option (See MSDN).
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            csc = new MockCSharpCompiler(rsp, _baseDirectory, new[] { source, "/preferreduilang:en", "/nowarn:2023" });
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
            int exitCode = new MockCSharpCompiler(null, _baseDirectory, new[] { "/nologo", "/t:library", src.ToString() }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString().Trim());

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = new MockCSharpCompiler(null, _baseDirectory, new[] { "/nologo", "/preferreduilang:en", "/nostdlib", "/t:library", src.ToString() }).Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Equal("{FILE}(1,14): error CS0518: Predefined type 'System.Object' is not defined or imported",
                         outWriter.ToString().Replace(Path.GetFileName(src.Path), "{FILE}").Trim());

            // Bug#15021: breaking change - empty source no error with /nostdlib
            src.WriteAllText("namespace System { }");
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = new MockCSharpCompiler(null, _baseDirectory, new[] { "/nologo", "/nostdlib", "/t:library", "/runtimemetadataversion:v4.0.30319", src.ToString() }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString().Trim());

            CleanupAllGeneratedFiles(src.Path);
        }

        private string GetDefaultResponseFilePath()
        {
            return Temp.CreateFile().WriteAllBytes(CommandLineTestResources.csc_rsp).Path;
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

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            int exitCode = new MockCSharpCompiler(null, _baseDirectory, new[] { "/nologo", "/noconfig", "/nostdlib", "/runtimemetadataversion:v4.0.30319", src.ToString() }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString().Trim());

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = new MockCSharpCompiler(null, _baseDirectory, new[] { "/nologo", "/nostdlib", "/runtimemetadataversion:v4.0.30319", src.ToString() }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString().Trim());
            string OriginalSource = src.Path;

            src = Temp.CreateFile("NoStdLib02b.cs");
            src.WriteAllText(mslib);
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = new MockCSharpCompiler(GetDefaultResponseFilePath(), _baseDirectory, new[] { "/nologo", "/noconfig", "/nostdlib", "/t:library", "/runtimemetadataversion:v4.0.30319", src.ToString() }).Run(outWriter);
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
            int exitCode = new MockCSharpCompiler(null, _baseDirectory, new[] { "/nologo", "/preferreduilang:en", src.ToString(), "/define" }).Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Equal("error CS2006: Command-line syntax error: Missing '<text>' for '/define' option", outWriter.ToString().Trim());

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = new MockCSharpCompiler(null, _baseDirectory, new[] { "/nologo", "/preferreduilang:en", "/t:library", src.ToString(), @"/define:""""" }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("warning CS2029: Invalid value for '/define'; '' is not a valid identifier", outWriter.ToString().Trim());

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = new MockCSharpCompiler(null, _baseDirectory, new[] { "/nologo", "/preferreduilang:en", "/t:library", src.ToString(), "/define: " }).Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Equal("error CS2006: Command-line syntax error: Missing '<text>' for '/define:' option", outWriter.ToString().Trim());

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = new MockCSharpCompiler(null, _baseDirectory, new[] { "/nologo", "/preferreduilang:en", "/t:library", src.ToString(), "/define:" }).Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Equal("error CS2006: Command-line syntax error: Missing '<text>' for '/define:' option", outWriter.ToString().Trim());

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = new MockCSharpCompiler(null, _baseDirectory, new[] { "/nologo", "/preferreduilang:en", "/t:library", src.ToString(), "/define:,,," }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("warning CS2029: Invalid value for '/define'; '' is not a valid identifier", outWriter.ToString().Trim());

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = new MockCSharpCompiler(null, _baseDirectory, new[] { "/nologo", "/preferreduilang:en", "/t:library", src.ToString(), "/define:,blah,Blah" }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("warning CS2029: Invalid value for '/define'; '' is not a valid identifier", outWriter.ToString().Trim());

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = new MockCSharpCompiler(null, _baseDirectory, new[] { "/nologo", "/preferreduilang:en", "/t:library", src.ToString(), "/define:a;;b@" }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("warning CS2029: Invalid value for '/define'; '' is not a valid identifier", outWriter.ToString().Trim());

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = new MockCSharpCompiler(null, _baseDirectory, new[] { "/nologo", "/preferreduilang:en", "/t:library", src.ToString(), "/define:a,b@;" }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("warning CS2029: Invalid value for '/define'; 'b@' is not a valid identifier", outWriter.ToString().Trim());

            //Bug 531612 - Native would normally not give the 2nd warning
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = new MockCSharpCompiler(null, _baseDirectory, new[] { "/nologo", "/preferreduilang:en", "/t:library", src.ToString(), @"/define:OE_WIN32=-1:LANG_HOST_EN=-1:LANG_OE_EN=-1:LANG_PRJ_EN=-1:HOST_COM20SDKEVERETT=-1:EXEMODE=-1:OE_NT5=-1:Win32=-1", @"/d:TRACE=TRUE,DEBUG=TRUE" }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal(@"warning CS2029: Invalid value for '/define'; 'OE_WIN32=-1:LANG_HOST_EN=-1:LANG_OE_EN=-1:LANG_PRJ_EN=-1:HOST_COM20SDKEVERETT=-1:EXEMODE=-1:OE_NT5=-1:Win32=-1' is not a valid identifier
warning CS2029: Invalid value for '/define'; 'TRACE=TRUE' is not a valid identifier", outWriter.ToString().Trim());

            CleanupAllGeneratedFiles(src.Path);
        }

        [WorkItem(733242, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/733242")]
        [ConditionalFact(typeof(WindowsOnly))]
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
                var output = ProcessUtilities.RunAndGetOutput(s_CSharpCompilerExecutable, String.Format("/nologo /t:library /doc:\"{1}\" {0}", src.ToString(), xml.ToString()), startFolder: dir.ToString());
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
        [ConditionalFact(typeof(WindowsOnly))]
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

            var output = ProcessUtilities.RunAndGetOutput(s_CSharpCompilerExecutable, String.Format("/nologo /t:library /doc:\"{1}\" {0}", src.ToString(), xml.ToString()), startFolder: dir.ToString());
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

            output = ProcessUtilities.RunAndGetOutput(s_CSharpCompilerExecutable, String.Format("/nologo /t:library /doc:\"{1}\" {0}", src.ToString(), xml.ToString()), startFolder: dir.ToString());
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
            var parsedArgs = DefaultParse(new[] { "a.cs" }, _baseDirectory);
            Assert.Equal(false, parsedArgs.PrintFullPaths);

            parsedArgs = DefaultParse(new[] { "a.cs", "/fullpaths" }, _baseDirectory);
            Assert.Equal(true, parsedArgs.PrintFullPaths);

            parsedArgs = DefaultParse(new[] { "a.cs", "/fullpaths:" }, _baseDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.ERR_BadSwitch, parsedArgs.Errors.First().Code);

            parsedArgs = DefaultParse(new[] { "a.cs", "/fullpaths: " }, _baseDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.ERR_BadSwitch, parsedArgs.Errors.First().Code);

            parsedArgs = DefaultParse(new[] { "a.cs", "/fullpaths+" }, _baseDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.ERR_BadSwitch, parsedArgs.Errors.First().Code);

            parsedArgs = DefaultParse(new[] { "a.cs", "/fullpaths+:" }, _baseDirectory);
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
            var csc = new MockCSharpCompiler(null, baseDir, new[] { source, "/preferreduilang:en" });
            int exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Contains(fileName + "(6,16): warning CS0168: The variable 'x' is declared but never used", outWriter.ToString(), StringComparison.Ordinal);

            // Checks the base case without /fullpaths when the file is located in the sub-folder (expect to see relative path name)
            //      c:\temp> csc.exe c:\temp\example\a.cs
            //      example\a.cs(6,16): warning CS0168: The variable 'x' is declared but never used
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            csc = new MockCSharpCompiler(null, Directory.GetParent(baseDir).FullName, new[] { source, "/preferreduilang:en" });
            exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Contains(fileName + "(6,16): warning CS0168: The variable 'x' is declared but never used", outWriter.ToString(), StringComparison.Ordinal);
            Assert.DoesNotContain(source, outWriter.ToString(), StringComparison.Ordinal);

            // Checks the base case without /fullpaths when the file is not located under the base directory (expect to see the full path name)
            //      c:\temp> csc.exe c:\test\a.cs
            //      c:\test\a.cs(6,16): warning CS0168: The variable 'x' is declared but never used
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            csc = new MockCSharpCompiler(null, Temp.CreateDirectory().Path, new[] { source, "/preferreduilang:en" });
            exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Contains(source + "(6,16): warning CS0168: The variable 'x' is declared but never used", outWriter.ToString(), StringComparison.Ordinal);

            // Checks the case with /fullpaths (expect to see the full paths)
            //      c:\temp> csc.exe c:\temp\a.cs /fullpaths
            //      c:\temp\a.cs(6,16): warning CS0168: The variable 'x' is declared but never used
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            csc = new MockCSharpCompiler(null, baseDir, new[] { source, "/fullpaths", "/preferreduilang:en" });
            exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Contains(source + @"(6,16): warning CS0168: The variable 'x' is declared but never used", outWriter.ToString(), StringComparison.Ordinal);

            // Checks the base case without /fullpaths when the file is located in the sub-folder (expect to see the full path name)
            //      c:\temp> csc.exe c:\temp\example\a.cs /fullpaths
            //      c:\temp\example\a.cs(6,16): warning CS0168: The variable 'x' is declared but never used
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            csc = new MockCSharpCompiler(null, Directory.GetParent(baseDir).FullName, new[] { source, "/preferreduilang:en", "/fullpaths" });
            exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Contains(source + "(6,16): warning CS0168: The variable 'x' is declared but never used", outWriter.ToString(), StringComparison.Ordinal);

            // Checks the base case without /fullpaths when the file is not located under the base directory (expect to see the full path name)
            //      c:\temp> csc.exe c:\test\a.cs /fullpaths
            //      c:\test\a.cs(6,16): warning CS0168: The variable 'x' is declared but never used
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            csc = new MockCSharpCompiler(null, Temp.CreateDirectory().Path, new[] { source, "/preferreduilang:en", "/fullpaths" });
            exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Contains(source + "(6,16): warning CS0168: The variable 'x' is declared but never used", outWriter.ToString(), StringComparison.Ordinal);

            CleanupAllGeneratedFiles(source);
        }

        [Fact]
        public void DefaultResponseFile()
        {
            MockCSharpCompiler csc = new MockCSharpCompiler(GetDefaultResponseFilePath(), _baseDirectory, new string[0]);
            AssertEx.Equal(csc.Arguments.MetadataReferences.Select(r => r.Reference), new string[]
            {
                typeof(object).Assembly.Location,
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
            MockCSharpCompiler csc = new MockCSharpCompiler(GetDefaultResponseFilePath(), _baseDirectory, new[] { "/noconfig" });
            Assert.Equal(csc.Arguments.MetadataReferences.Select(r => r.Reference), new string[]
            {
                typeof(object).Assembly.Location,
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
            int exitCode = new MockCSharpCompiler(null, baseDir, new[] { "/nologo", "/preferreduilang:en", source.ToString() }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal(Path.GetFileName(source) + "(7,17): warning CS1634: Expected disable or restore", outWriter.ToString().Trim());

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = new MockCSharpCompiler(null, baseDir, new[] { "/nologo", "/nowarn:1634", source.ToString() }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString().Trim());

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = new MockCSharpCompiler(null, baseDir, new[] { "/nologo", "/preferreduilang:en", Path.Combine(baseDir, "nonexistent.cs"), source.ToString() }).Run(outWriter);
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
            int exitCode = new MockCSharpCompiler(null, _baseDirectory, new[] { "/nologo", "/nowarn:1522,642", source.ToString() }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString().Trim());

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
            int exitCode = new MockCSharpCompiler(null, baseDir, new[] { "/nologo", "/preferreduilang:en", "/warn:3", "/warnaserror", source.ToString() }).Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Equal(fileName + "(12,20): error CS1522: Empty switch block", outWriter.ToString().Trim());

            CleanupAllGeneratedFiles(source);
        }

        [Fact(), WorkItem(546025, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546025")]
        public void TestWin32ResWithBadResFile_CS1583ERR_BadWin32Res()
        {
            string source = Temp.CreateFile(prefix: "", extension: ".cs").WriteAllText(@"class Test { static void Main() {} }").Path;
            string badres = Temp.CreateFile().WriteAllBytes(TestResources.DiagnosticTests.badresfile).Path;

            var baseDir = Path.GetDirectoryName(source);
            var fileName = Path.GetFileName(source);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            int exitCode = new MockCSharpCompiler(null, baseDir, new[]
            {
                "/nologo",
                "/preferreduilang:en",
                "/win32res:" + badres,
                source
            }).Run(outWriter);

            Assert.Equal(1, exitCode);
            Assert.Equal("error CS1583: Error reading Win32 resources -- Image is too small.", outWriter.ToString().Trim());

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
            int exitCode = new MockCSharpCompiler(null, _baseDirectory, new[] { "/nologo", "/target:library", "/out:foo.dll", "/nowarn:2008" }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString().Trim());

            System.IO.File.Delete(System.IO.Path.Combine(baseDir, "foo.dll"));
            CleanupAllGeneratedFiles(source);
        }

        [Fact, WorkItem(546452, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546452")]
        public void CS1691WRN_BadWarningNumber_Bug15905()
        {
            string source = Temp.CreateFile(prefix: "", extension: ".cs").WriteAllText(@"
class Program
{
#pragma warning disable 1998
        public static void Main() { }
#pragma warning restore 1998
} ").Path;
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);

            // Repro case 1
            int exitCode = new MockCSharpCompiler(null, _baseDirectory, new[] { "/nologo", "/warnaserror", source.ToString() }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString().Trim());

            // Repro case 2
            exitCode = new MockCSharpCompiler(null, _baseDirectory, new[] { "/nologo", "/nowarn:1998", source.ToString() }).Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString().Trim());

            CleanupAllGeneratedFiles(source);
        }

        [ConditionalFact(typeof(WindowsOnly))]
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

            int exitCode1 = new MockCSharpCompiler(null, dir.Path, new[] { "/debug:full", "/out:Program.exe", source1.Path }).Run(outWriter);
            Assert.NotEqual(0, exitCode1);

            ValidateZeroes(exe.Path, oldSize);
            ValidateZeroes(pdb.Path, oldSize);

            int exitCode2 = new MockCSharpCompiler(null, dir.Path, new[] { "/debug:full", "/out:Program.exe", source2.Path }).Run(outWriter);
            Assert.Equal(0, exitCode2);

            using (var peFile = File.OpenRead(exe.Path))
            {
                PdbValidation.ValidateDebugDirectory(peFile, null, pdb.Path, isDeterministic: false);
            }

            Assert.True(new FileInfo(exe.Path).Length < oldSize);
            Assert.True(new FileInfo(pdb.Path).Length < oldSize);

            int exitCode3 = new MockCSharpCompiler(null, dir.Path, new[] { "/debug:full", "/out:Program.exe", source3.Path }).Run(outWriter);
            Assert.Equal(0, exitCode3);

            using (var peFile = File.OpenRead(exe.Path))
            {
                PdbValidation.ValidateDebugDirectory(peFile, null, pdb.Path, isDeterministic: false);
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

        [Fact]
        public void IOFailure_OpenOutputFile()
        {
            string sourcePath = MakeTrivialExe();
            string exePath = Path.Combine(Path.GetDirectoryName(sourcePath), "test.exe");
            var csc = new MockCSharpCompiler(null, _baseDirectory, new[] { "/nologo", "/preferreduilang:en", $"/out:{exePath}", sourcePath });
            csc.FileOpen = (file, mode, access, share) =>
            {
                if (file == exePath)
                {
                    throw new IOException();
                }

                return File.Open(file, (FileMode)mode, (FileAccess)access, (FileShare)share);
            };

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
            var csc = new MockCSharpCompiler(null, _baseDirectory, new[] { "/nologo", "/debug-", $"/out:{exePath}", sourcePath });
            csc.FileOpen = (file, mode, access, share) =>
            {
                if (file == pdbPath)
                {
                    throw new IOException();
                }

                return File.Open(file, (FileMode)mode, (FileAccess)access, (FileShare)share);
            };

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
            string xmlPath = Path.Combine(_baseDirectory, "Test.xml");
            var csc = new MockCSharpCompiler(null, _baseDirectory, new[] { "/nologo", "/preferreduilang:en", "/doc:" + xmlPath, sourcePath });
            csc.FileOpen = (file, mode, access, share) =>
            {
                if (file == xmlPath)
                {
                    throw new IOException();
                }
                else
                {
                    return File.Open(file, (FileMode)mode, (FileAccess)access, (FileShare)share);
                }
            };

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            int exitCode = csc.Run(outWriter);

            var expectedOutput = string.Format("error CS0016: Could not write to output file '{0}' -- 'I/O error occurred.'", xmlPath);
            Assert.Equal(expectedOutput, outWriter.ToString().Trim());

            Assert.NotEqual(0, exitCode);

            System.IO.File.Delete(xmlPath);
            System.IO.File.Delete(sourcePath);
            CleanupAllGeneratedFiles(sourcePath);
        }

        private string MakeTrivialExe()
        {
            return Temp.CreateFile(prefix: "", extension: ".cs").WriteAllText(@"
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
            int exitCode = new MockCSharpCompiler(null, _baseDirectory, new[] { "/nologo", sourcePath }).Run(outWriter);
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

            var sourcePath = Temp.CreateFile(directory: _baseDirectory, extension: ".cs").WriteAllText(source).Path;
            string xmlPath = Path.Combine(_baseDirectory, "Test.xml");
            var csc = new MockCSharpCompiler(null, _baseDirectory, new[] { "/target:library", "/out:Test.dll", "/doc:" + xmlPath, sourcePath });

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
        [ConditionalFact(typeof(WindowsOnly))]
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
            // warning CS2002: Source file 'a.cs' specified multiple times
            var warnings = new[] { aWrnString, aWrnString };
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
                Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments(@"tmpDi\r*a?.cs"),
                // error CS2001: Source file 'tmpDi\r*a?.cs' could not be found.
                Diagnostic(ErrorCode.ERR_FileNotFound).WithArguments(@"tmpDi\r*a?.cs")};
            TestCS2002(commandLineArgs, tempParentDir.Path, 1, (string[])null, parseDiags);

            char currentDrive = Directory.GetCurrentDirectory()[0];
            commandLineArgs = new[] { tempFile.Path, currentDrive + @":a.cs" };
            parseDiags = new[] {
                // error CS2021: File name 'e:a.cs' is empty, contains invalid characters, has a drive specification without an absolute path, or is too long
                Diagnostic(ErrorCode.FTL_InputFileNameTooLong).WithArguments(currentDrive + @":a.cs")};
            TestCS2002(commandLineArgs, tempParentDir.Path, 1, (string[])null, parseDiags);

            commandLineArgs = new[] { "/preferreduilang:en", tempFile.Path, @":a.cs" };
            // error CS1504: Source file '{0}' could not be opened: {1}
            var formattedcs1504 = String.Format(cs1504, PathUtilities.CombineAbsoluteAndRelativePaths(tempParentDir.Path, @":a.cs"), @"The given path's format is not supported.");
            TestCS2002(commandLineArgs, tempParentDir.Path, 1, formattedcs1504);

            CleanupAllGeneratedFiles(tempFile.Path);
            System.IO.Directory.Delete(tempParentDir.Path, true);
        }

        private static void TestCS2002(string[] commandLineArgs, string baseDirectory, int expectedExitCode, string compileDiagnostic, params DiagnosticDescription[] parseDiagnostics)
        {
            TestCS2002(commandLineArgs, baseDirectory, expectedExitCode, new[] { compileDiagnostic }, parseDiagnostics);
        }

        private static void TestCS2002(string[] commandLineArgs, string baseDirectory, int expectedExitCode, string[] compileDiagnostics, params DiagnosticDescription[] parseDiagnostics)
        {
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var allCommandLineArgs = new[] { "/nologo", "/preferreduilang:en", "/t:library" }.Concat(commandLineArgs).ToArray();

            // Verify command line parser diagnostics.
            DefaultParse(allCommandLineArgs, baseDirectory).Errors.Verify(parseDiagnostics);

            // Verify compile.
            int exitCode = new MockCSharpCompiler(null, baseDirectory, allCommandLineArgs).Run(outWriter);
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
            var tree = SyntaxFactory.ParseSyntaxTree("class C public { }", path: "foo");

            var comp = new MockCSharpCompiler(null, _baseDirectory, new[] { "/errorendlocation" });
            var loc = new SourceLocation(tree.GetCompilationUnitRoot().FindToken(6));
            var diag = new CSDiagnostic(new DiagnosticInfo(MessageProvider.Instance, (int)ErrorCode.ERR_MetadataNameTooLong), loc);
            var text = comp.DiagnosticFormatter.Format(diag);

            string stringStart = "foo(1,7,1,8)";

            Assert.Equal(stringStart, text.Substring(0, stringStart.Length));
        }

        [Fact]
        public void ReportAnalyzer()
        {
            var parsedArgs1 = DefaultParse(new[] { "a.cs", "/reportanalyzer" }, _baseDirectory);
            Assert.True(parsedArgs1.ReportAnalyzer);

            var parsedArgs2 = DefaultParse(new[] { "a.cs", "" }, _baseDirectory);
            Assert.False(parsedArgs2.ReportAnalyzer);
        }

        [Fact]
        public void ReportAnalyzerOutput()
        {
            var srcFile = Temp.CreateFile().WriteAllText(@"class C {}");
            var srcDirectory = Path.GetDirectoryName(srcFile.Path);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = new MockCSharpCompiler(null, srcDirectory, new[] { "/reportanalyzer", "/t:library", "/a:" + Assembly.GetExecutingAssembly().Location, srcFile.Path });
            var exitCode = csc.Run(outWriter);
            Assert.Equal(0, exitCode);
            var output = outWriter.ToString();
            Assert.Contains(CodeAnalysisResources.AnalyzerExecutionTimeColumnHeader, output, StringComparison.Ordinal);
            Assert.Contains(new WarningDiagnosticAnalyzer().ToString(), output, StringComparison.Ordinal);
            CleanupAllGeneratedFiles(srcFile.Path);
        }

        [Fact]
        [WorkItem(1759, "https://github.com/dotnet/roslyn/issues/1759")]
        public void AnalyzerDiagnosticThrowsInGetMessage()
        {
            var srcFile = Temp.CreateFile().WriteAllText(@"class C {}");
            var srcDirectory = Path.GetDirectoryName(srcFile.Path);

            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var csc = new MockCSharpCompiler(null, _baseDirectory, new[] { "/t:library", srcFile.Path },
               analyzer: new AnalyzerThatThrowsInGetMessage());

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
            var csc = new MockCSharpCompiler(null, _baseDirectory, new[] { "/t:library", $"/warnaserror:{AnalyzerExecutor.AnalyzerExceptionDiagnosticId}", srcFile.Path },
               analyzer: new AnalyzerThatThrowsInGetMessage());

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
            var csc = new MockCSharpCompiler(null, _baseDirectory, new[] { "/t:library", srcFile.Path },
               analyzer: new AnalyzerReportingMisformattedDiagnostic());

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
            var comp = new MockCSharpCompiler(null, _baseDirectory, new string[] { });
            var text = comp.DiagnosticFormatter.Format(syntaxTree.GetDiagnostics().First());
            //Pull off the last segment of the current directory.
            var expectedPath = Path.GetDirectoryName(_baseDirectory);
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
#line 10 ""http://foo.bar/baz.aspx"" //URI
using System*
";
            syntaxTree = SyntaxFactory.ParseSyntaxTree(sampleProgram, path: "filename.cs");
            text = comp.DiagnosticFormatter.Format(syntaxTree.GetDiagnostics().First());
            Assert.True(text.StartsWith("http://foo.bar/baz.aspx", StringComparison.Ordinal));
        }

        [WorkItem(1119609, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1119609")]
        [Fact]
        public void PreferredUILang()
        {
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            int exitCode = new MockCSharpCompiler(null, _baseDirectory, new[] { "/preferreduilang" }).Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Contains("CS2006", outWriter.ToString(), StringComparison.Ordinal);

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = new MockCSharpCompiler(null, _baseDirectory, new[] { "/preferreduilang:" }).Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Contains("CS2006", outWriter.ToString(), StringComparison.Ordinal);

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = new MockCSharpCompiler(null, _baseDirectory, new[] { "/preferreduilang:zz" }).Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Contains("CS2038", outWriter.ToString(), StringComparison.Ordinal);

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = new MockCSharpCompiler(null, _baseDirectory, new[] { "/preferreduilang:en-zz" }).Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Contains("CS2038", outWriter.ToString(), StringComparison.Ordinal);

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = new MockCSharpCompiler(null, _baseDirectory, new[] { "/preferreduilang:en-US" }).Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.DoesNotContain("CS2038", outWriter.ToString(), StringComparison.Ordinal);

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = new MockCSharpCompiler(null, _baseDirectory, new[] { "/preferreduilang:de" }).Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.DoesNotContain("CS2038", outWriter.ToString(), StringComparison.Ordinal);

            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = new MockCSharpCompiler(null, _baseDirectory, new[] { "/preferreduilang:de-AT" }).Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.DoesNotContain("CS2038", outWriter.ToString(), StringComparison.Ordinal);
        }

        [WorkItem(531263, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531263")]
        [Fact]
        public void EmptyFileName()
        {
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = new MockCSharpCompiler(null, _baseDirectory, new[] { "" }).Run(outWriter);
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
            var cmd = new MockCSharpCompiler(null, _baseDirectory, new[] { "/nologo", "/target:library", filePath });
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = cmd.Run(outWriter);
            Assert.Equal(0, exitCode);
            Assert.Equal("", outWriter.ToString().Trim());

            CleanupAllGeneratedFiles(filePath);
        }

        [Fact]
        public void RuntimeMetadataVersion()
        {
            var parsedArgs = DefaultParse(new[] { "a.cs", "/runtimemetadataversion" }, _baseDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.ERR_SwitchNeedsString, parsedArgs.Errors.First().Code);

            parsedArgs = DefaultParse(new[] { "a.cs", "/runtimemetadataversion:" }, _baseDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.ERR_SwitchNeedsString, parsedArgs.Errors.First().Code);

            parsedArgs = DefaultParse(new[] { "a.cs", "/runtimemetadataversion:  " }, _baseDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.ERR_SwitchNeedsString, parsedArgs.Errors.First().Code);

            parsedArgs = DefaultParse(new[] { "a.cs", "/runtimemetadataversion:v4.0.30319" }, _baseDirectory);
            Assert.Equal(0, parsedArgs.Errors.Length);
            Assert.Equal("v4.0.30319", parsedArgs.EmitOptions.RuntimeMetadataVersion);

            parsedArgs = DefaultParse(new[] { "a.cs", "/runtimemetadataversion:-_+@%#*^" }, _baseDirectory);
            Assert.Equal(0, parsedArgs.Errors.Length);
            Assert.Equal("-_+@%#*^", parsedArgs.EmitOptions.RuntimeMetadataVersion);

            var comp = CreateCompilation(string.Empty);
            Assert.Equal(ModuleMetadata.CreateFromImage(comp.EmitToArray(new EmitOptions(runtimeMetadataVersion: "v4.0.30319"))).Module.MetadataVersion, "v4.0.30319");

            comp = CreateCompilation(string.Empty);
            Assert.Equal(ModuleMetadata.CreateFromImage(comp.EmitToArray(new EmitOptions(runtimeMetadataVersion: "_+@%#*^"))).Module.MetadataVersion, "_+@%#*^");
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
            DefaultParse(new[] { "/lib:" + invalidPath, sourceFile.Path }, _baseDirectory).Errors.Verify(
                // warning CS1668: Invalid search path '::' specified in '/LIB option' -- 'path is too long or invalid'
                Diagnostic(ErrorCode.WRN_InvalidSearchPathDir).WithArguments("::", "/LIB option", "path is too long or invalid"));
            DefaultParse(new[] { "/lib:" + nonExistentPath, sourceFile.Path }, _baseDirectory).Errors.Verify(
                // warning CS1668: Invalid search path 'DoesNotExist' specified in '/LIB option' -- 'directory does not exist'
                Diagnostic(ErrorCode.WRN_InvalidSearchPathDir).WithArguments("DoesNotExist", "/LIB option", "directory does not exist"));

            // LIB environment variable
            DefaultParse(new[] { sourceFile.Path }, _baseDirectory, additionalReferenceDirectories: invalidPath).Errors.Verify(
                // warning CS1668: Invalid search path '::' specified in 'LIB environment variable' -- 'path is too long or invalid'
                Diagnostic(ErrorCode.WRN_InvalidSearchPathDir).WithArguments("::", "LIB environment variable", "path is too long or invalid"));
            DefaultParse(new[] { sourceFile.Path }, _baseDirectory, additionalReferenceDirectories: nonExistentPath).Errors.Verify(
                // warning CS1668: Invalid search path 'DoesNotExist' specified in 'LIB environment variable' -- 'directory does not exist'
                Diagnostic(ErrorCode.WRN_InvalidSearchPathDir).WithArguments("DoesNotExist", "LIB environment variable", "directory does not exist"));

            CleanupAllGeneratedFiles(sourceFile.Path);
        }

        [WorkItem(650083, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/650083")]
        [ConditionalFact(typeof(WindowsOnly))]
        public void ReservedDeviceNameAsFileName()
        {
            var parsedArgs = DefaultParse(new[] { "com9.cs", "/t:library " }, _baseDirectory);
            Assert.Equal(0, parsedArgs.Errors.Length);

            parsedArgs = DefaultParse(new[] { "a.cs", "/t:library ", "/appconfig:.\\aux.config" }, _baseDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.FTL_InputFileNameTooLong, parsedArgs.Errors.First().Code);


            parsedArgs = DefaultParse(new[] { "a.cs", "/out:com1.dll " }, _baseDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.FTL_InputFileNameTooLong, parsedArgs.Errors.First().Code);

            parsedArgs = DefaultParse(new[] { "a.cs", "/doc:..\\lpt2.xml:  " }, _baseDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.FTL_InputFileNameTooLong, parsedArgs.Errors.First().Code);

            parsedArgs = DefaultParse(new[] { "a.cs", "/debug+", "/pdb:.\\prn.pdb" }, _baseDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.FTL_InputFileNameTooLong, parsedArgs.Errors.First().Code);

            parsedArgs = DefaultParse(new[] { "a.cs", "@con.rsp" }, _baseDirectory);
            Assert.Equal(1, parsedArgs.Errors.Length);
            Assert.Equal((int)ErrorCode.ERR_OpenResponseFile, parsedArgs.Errors.First().Code);
        }

        [Fact]
        public void ReservedDeviceNameAsFileName2()
        {
            string filePath = Temp.CreateFile().WriteAllText(@"class C {}").Path;
            // make sure reserved device names don't 
            var cmd = new MockCSharpCompiler(null, _baseDirectory, new[] { "/r:com2.dll", "/target:library", "/preferreduilang:en", filePath });
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = cmd.Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Contains("error CS0006: Metadata file 'com2.dll' could not be found", outWriter.ToString(), StringComparison.Ordinal);

            cmd = new MockCSharpCompiler(null, _baseDirectory, new[] { "/link:..\\lpt8.dll", "/target:library", "/preferreduilang:en", filePath });
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = cmd.Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Contains("error CS0006: Metadata file '..\\lpt8.dll' could not be found", outWriter.ToString(), StringComparison.Ordinal);

            cmd = new MockCSharpCompiler(null, _baseDirectory, new[] { "/lib:aux", "/preferreduilang:en", filePath });
            outWriter = new StringWriter(CultureInfo.InvariantCulture);
            exitCode = cmd.Run(outWriter);
            Assert.Equal(1, exitCode);
            Assert.Contains("warning CS1668: Invalid search path 'aux' specified in '/LIB option' -- 'directory does not exist'", outWriter.ToString(), StringComparison.Ordinal);

            CleanupAllGeneratedFiles(filePath);
        }

        [Fact]
        public void ParseFeatures()
        {
            var args = DefaultParse(new[] { "/features:Test", "a.vb" }, _baseDirectory);
            args.Errors.Verify();
            Assert.Equal("Test", args.ParseOptions.Features.Single().Key);

            args = DefaultParse(new[] { "/features:Test", "a.vb", "/Features:Experiment" }, _baseDirectory);
            args.Errors.Verify();
            Assert.Equal(2, args.ParseOptions.Features.Count);
            Assert.True(args.ParseOptions.Features.ContainsKey("Test"));
            Assert.True(args.ParseOptions.Features.ContainsKey("Experiment"));

            args = DefaultParse(new[] { "/features:Test=false,Key=value", "a.vb" }, _baseDirectory);
            args.Errors.Verify();
            Assert.True(args.ParseOptions.Features.SetEquals(new Dictionary<string, string> { { "Test", "false" }, { "Key", "value" } }));

            args = DefaultParse(new[] { "/features:Test,", "a.vb" }, _baseDirectory);
            args.Errors.Verify();
            Assert.True(args.ParseOptions.Features.SetEquals(new Dictionary<string, string> { { "Test", "true" } }));
        }

        [Fact]
        public void ParseAdditionalFile()
        {
            var args = DefaultParse(new[] { "/additionalfile:web.config", "a.cs" }, _baseDirectory);
            args.Errors.Verify();
            Assert.Equal(Path.Combine(_baseDirectory, "web.config"), args.AdditionalFiles.Single().Path);

            args = DefaultParse(new[] { "/additionalfile:web.config", "a.cs", "/additionalfile:app.manifest" }, _baseDirectory);
            args.Errors.Verify();
            Assert.Equal(2, args.AdditionalFiles.Length);
            Assert.Equal(Path.Combine(_baseDirectory, "web.config"), args.AdditionalFiles[0].Path);
            Assert.Equal(Path.Combine(_baseDirectory, "app.manifest"), args.AdditionalFiles[1].Path);

            args = DefaultParse(new[] { "/additionalfile:web.config", "a.cs", "/additionalfile:web.config" }, _baseDirectory);
            args.Errors.Verify();
            Assert.Equal(2, args.AdditionalFiles.Length);
            Assert.Equal(Path.Combine(_baseDirectory, "web.config"), args.AdditionalFiles[0].Path);
            Assert.Equal(Path.Combine(_baseDirectory, "web.config"), args.AdditionalFiles[1].Path);

            args = DefaultParse(new[] { "/additionalfile:..\\web.config", "a.cs" }, _baseDirectory);
            args.Errors.Verify();
            Assert.Equal(Path.Combine(_baseDirectory, "..\\web.config"), args.AdditionalFiles.Single().Path);

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

            args = DefaultParse(new[] { "/additionalfile:web.config;app.manifest", "a.cs" }, _baseDirectory);
            args.Errors.Verify();
            Assert.Equal(2, args.AdditionalFiles.Length);
            Assert.Equal(Path.Combine(_baseDirectory, "web.config"), args.AdditionalFiles[0].Path);
            Assert.Equal(Path.Combine(_baseDirectory, "app.manifest"), args.AdditionalFiles[1].Path);

            args = DefaultParse(new[] { "/additionalfile:web.config,app.manifest", "a.cs" }, _baseDirectory);
            args.Errors.Verify();
            Assert.Equal(2, args.AdditionalFiles.Length);
            Assert.Equal(Path.Combine(_baseDirectory, "web.config"), args.AdditionalFiles[0].Path);
            Assert.Equal(Path.Combine(_baseDirectory, "app.manifest"), args.AdditionalFiles[1].Path);

            args = DefaultParse(new[] { "/additionalfile:web.config:app.manifest", "a.cs" }, _baseDirectory);
            args.Errors.Verify();
            Assert.Equal(1, args.AdditionalFiles.Length);
            Assert.Equal(Path.Combine(_baseDirectory, "web.config:app.manifest"), args.AdditionalFiles[0].Path);

            args = DefaultParse(new[] { "/additionalfile", "a.cs" }, _baseDirectory);
            args.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<file list>", "additionalfile"));
            Assert.Equal(0, args.AdditionalFiles.Length);

            args = DefaultParse(new[] { "/additionalfile:", "a.cs" }, _baseDirectory);
            args.Errors.Verify(Diagnostic(ErrorCode.ERR_SwitchNeedsString).WithArguments("<file list>", "additionalfile"));
            Assert.Equal(0, args.AdditionalFiles.Length);
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

        private static string VerifyOutput(TempDirectory sourceDir, TempFile sourceFile,
                                           bool includeCurrentAssemblyAsAnalyzerReference = true,
                                           string[] additionalFlags = null,
                                           int expectedInfoCount = 0,
                                           int expectedWarningCount = 0,
                                           int expectedErrorCount = 0)
        {
            var args = new[] {
                                "/nologo", "/preferreduilang:en", "/t:library",
                                sourceFile.Path
                             };
            if (includeCurrentAssemblyAsAnalyzerReference)
            {
                args = args.Append("/a:" + Assembly.GetExecutingAssembly().Location);
            }
            if (additionalFlags != null)
            {
                args = args.Append(additionalFlags);
            }

            var csc = new MockCSharpCompiler(null, sourceDir.Path, args);
            var outWriter = new StringWriter(CultureInfo.InvariantCulture);
            var exitCode = csc.Run(outWriter);
            var output = outWriter.ToString();

            var expectedExitCode = expectedErrorCount > 0 ? 1 : 0;
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
        [Fact]
        public void NoWarnAndWarnAsError_InfoDiagnostic()
        {
            // This assembly has an InfoDiagnosticAnalyzer type which should produce custom info
            // diagnostics for the #pragma warning restore directives present in the compilations created in this test.
            var source = @"using System;
#pragma warning restore";
            var name = "a.cs";
            string output;
            output = GetOutput(name, source, expectedWarningCount: 1, expectedInfoCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(2,1): info Info01: Throwing a diagnostic for #pragma restore", output, StringComparison.Ordinal);

            // TEST: Verify that /warn:0 suppresses custom info diagnostic Info01.
            output = GetOutput(name, source, additionalFlags: new[] { "/warn:0" });

            // TEST: Verify that custom info diagnostic Info01 can be individually suppressed via /nowarn:.
            output = GetOutput(name, source, additionalFlags: new[] { "/nowarn:Info01" }, expectedWarningCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that custom info diagnostic Info01 can never be promoted to an error via /warnaserror+.
            output = GetOutput(name, source, additionalFlags: new[] { "/warnaserror+", "/nowarn:8032" }, expectedInfoCount: 1);
            Assert.Contains("a.cs(2,1): info Info01: Throwing a diagnostic for #pragma restore", output, StringComparison.Ordinal);

            // TEST: Verify that custom info diagnostic Info01 is still reported as an info when /warnaserror- is used.
            output = GetOutput(name, source, additionalFlags: new[] { "/warnaserror-" }, expectedWarningCount: 1, expectedInfoCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(2,1): info Info01: Throwing a diagnostic for #pragma restore", output, StringComparison.Ordinal);

            // TEST: Verify that custom info diagnostic Info01 can be individually promoted to an error via /warnaserror:.
            output = GetOutput(name, source, additionalFlags: new[] { "/warnaserror:Info01" }, expectedWarningCount: 1, expectedErrorCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(2,1): error Info01: Throwing a diagnostic for #pragma restore", output, StringComparison.Ordinal);

            // TEST: Verify that custom info diagnostic Info01 is still reported as an info when passed to /warnaserror-:.
            output = GetOutput(name, source, additionalFlags: new[] { "/warnaserror-:Info01" }, expectedWarningCount: 1, expectedInfoCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(2,1): info Info01: Throwing a diagnostic for #pragma restore", output, StringComparison.Ordinal);

            // TEST: Verify /nowarn overrides /warnaserror.
            output = GetOutput(name, source, additionalFlags: new[] { "/warnaserror:Info01", "/nowarn:Info01" }, expectedWarningCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify /nowarn overrides /warnaserror.
            output = GetOutput(name, source, additionalFlags: new[] { "/nowarn:Info01", "/warnaserror:Info01" }, expectedWarningCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify /nowarn overrides /warnaserror-.
            output = GetOutput(name, source, additionalFlags: new[] { "/warnaserror-:Info01", "/nowarn:Info01" }, expectedWarningCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify /nowarn overrides /warnaserror-.
            output = GetOutput(name, source, additionalFlags: new[] { "/nowarn:Info01", "/warnaserror-:Info01" }, expectedWarningCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that /warn:0 has no impact on custom info diagnostic Info01.
            output = GetOutput(name, source, additionalFlags: new[] { "/warn:0", "/warnaserror:Info01" });

            // TEST: Verify that /warn:0 has no impact on custom info diagnostic Info01.
            output = GetOutput(name, source, additionalFlags: new[] { "/warnaserror:Info01", "/warn:0" });

            // TEST: Verify that last /warnaserror[+/-]: flag on command line wins.
            output = GetOutput(name, source, additionalFlags: new[] { "/warnaserror+:Info01", "/warnaserror-:Info01" }, expectedWarningCount: 1, expectedInfoCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(2,1): info Info01: Throwing a diagnostic for #pragma restore", output, StringComparison.Ordinal);

            // TEST: Verify that last /warnaserror[+/-]: flag on command line wins.
            output = GetOutput(name, source, additionalFlags: new[] { "/warnaserror-:Info01", "/warnaserror+:Info01" }, expectedWarningCount: 1, expectedErrorCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(2,1): error Info01: Throwing a diagnostic for #pragma restore", output, StringComparison.Ordinal);

            // TEST: Verify that last one wins between /warnaserror[+/-] and /warnaserror[+/-]:.
            output = GetOutput(name, source, additionalFlags: new[] { "/warnaserror-", "/warnaserror+:Info01" }, expectedWarningCount: 1, expectedErrorCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(2,1): error Info01: Throwing a diagnostic for #pragma restore", output, StringComparison.Ordinal);

            // TEST: Verify that last one wins between /warnaserror[+/-]: and /warnaserror[+/-].
            output = GetOutput(name, source, additionalFlags: new[] { "/warnaserror-:Info01", "/warnaserror+", "/nowarn:8032" }, expectedInfoCount: 1);
            Assert.Contains("a.cs(2,1): info Info01: Throwing a diagnostic for #pragma restore", output, StringComparison.Ordinal);

            // TEST: Verify that last one wins between /warnaserror[+/-]: and /warnaserror[+/-].
            output = GetOutput(name, source, additionalFlags: new[] { "/warnaserror+:Info01", "/warnaserror+", "/nowarn:8032" }, expectedInfoCount: 1);
            Assert.Contains("a.cs(2,1): info Info01: Throwing a diagnostic for #pragma restore", output, StringComparison.Ordinal);

            // TEST: Verify that last one wins between /warnaserror[+/-] and /warnaserror[+/-]:.
            output = GetOutput(name, source, additionalFlags: new[] { "/warnaserror+", "/warnaserror+:Info01", "/nowarn:8032" }, expectedErrorCount: 1);
            Assert.Contains("a.cs(2,1): error Info01: Throwing a diagnostic for #pragma restore", output, StringComparison.Ordinal);

            // TEST: Verify that last one wins between /warnaserror[+/-]: and /warnaserror[+/-].
            output = GetOutput(name, source, additionalFlags: new[] { "/warnaserror+:Info01", "/warnaserror-" }, expectedWarningCount: 1, expectedInfoCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(2,1): info Info01: Throwing a diagnostic for #pragma restore", output, StringComparison.Ordinal);

            // TEST: Verify that last one wins between /warnaserror[+/-] and /warnaserror[+/-]:.
            output = GetOutput(name, source, additionalFlags: new[] { "/warnaserror+", "/warnaserror-:Info01", "/nowarn:8032" }, expectedInfoCount: 1);
            Assert.Contains("a.cs(2,1): info Info01: Throwing a diagnostic for #pragma restore", output, StringComparison.Ordinal);

            // TEST: Verify that last one wins between /warnaserror[+/-]: and /warnaserror[+/-].
            output = GetOutput(name, source, additionalFlags: new[] { "/warnaserror-:Info01", "/warnaserror-" }, expectedWarningCount: 1, expectedInfoCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(2,1): info Info01: Throwing a diagnostic for #pragma restore", output, StringComparison.Ordinal);

            // TEST: Verify that last one wins between /warnaserror[+/-] and /warnaserror[+/-]:.
            output = GetOutput(name, source, additionalFlags: new[] { "/warnaserror-", "/warnaserror-:Info01" }, expectedWarningCount: 1, expectedInfoCount: 1);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(2,1): info Info01: Throwing a diagnostic for #pragma restore", output, StringComparison.Ordinal);
        }

        private string GetOutput(
            string name,
            string source,
            bool includeCurrentAssemblyAsAnalyzerReference = true,
            string[] additionalFlags = null,
            int expectedInfoCount = 0,
            int expectedWarningCount = 0,
            int expectedErrorCount = 0)
        {
            var dir = Temp.CreateDirectory();
            var file = dir.CreateFile(name);
            file.WriteAllText(source);

            var output = VerifyOutput(dir, file, includeCurrentAssemblyAsAnalyzerReference, additionalFlags, expectedInfoCount, expectedWarningCount, expectedErrorCount);
            CleanupAllGeneratedFiles(file.Path);
            return output;
        }

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
            // Promoting compiler warning CS0168 to an error causes us to no longer report any custom warning diagnostics as errors (Bug 998069).
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror", "/nowarn:8032" }, expectedErrorCount: 1);
            Assert.Contains("a.cs(6,13): error CS0168: The variable 'i' is declared but never used", output, StringComparison.Ordinal);

            // TEST: Verify that compiler warning CS0168 as well as custom warning diagnostic Warning01 can be promoted to errors via /warnaserror+.
            // Promoting compiler warning CS0168 to an error causes us to no longer report any custom warning diagnostics as errors (Bug 998069).
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror+", "/nowarn:8032" }, expectedErrorCount: 1);
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
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror+:CS0168" }, expectedWarningCount: 1, expectedErrorCount: 1);
            Assert.Contains("a.cs(6,13): error CS0168: The variable 'i' is declared but never used", output, StringComparison.Ordinal);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that diagnostic ids are processed in case-sensitive fashion inside /warnaserror.
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror:cs0168,warning01,58000" }, expectedWarningCount: 3);
            Assert.Contains("a.cs(2,7): warning Warning01: Throwing a diagnostic for types declared", output, StringComparison.Ordinal);
            Assert.Contains("a.cs(6,13): warning CS0168: The variable 'i' is declared but never used", output, StringComparison.Ordinal);
            Assert.Contains("warning CS8032", output, StringComparison.Ordinal);

            // TEST: Verify that custom warning diagnostic Warning01 as well as compiler warning CS0168 can be promoted to errors via /warnaserror:.
            // This doesn't work currently - promoting CS0168 to an error causes us to no longer report any custom warning diagnostics as errors (Bug 998069).
            output = VerifyOutput(dir, file, additionalFlags: new[] { "/warnaserror:CS0168,Warning01" }, expectedWarningCount: 1, expectedErrorCount: 1);
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

            var comp = CreateCompilationWithMscorlib(@"[assembly: System.CLSCompliant(true)]
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

            var comp = CreateCompilationWithMscorlib(@"[assembly: System.CLSCompliant(true)]
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
        [ConditionalFact(typeof(WindowsOnly))]
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

            var output = ProcessUtilities.RunAndGetOutput(s_CSharpCompilerExecutable, String.Format("/nologo /doc:doc.xml /out:out.exe /resource:doc.xml {0}", src.ToString()), startFolder: dir.ToString());
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
            var args = ScriptParse(new[] { "--", "script.csx", "b", "c" }, baseDirectory: _baseDirectory);
            AssertEx.Equal(new[] { Path.Combine(_baseDirectory, "script.csx") }, args.SourceFiles.Select(f => f.Path));
            AssertEx.Equal(new[] { "b", "c" }, args.ScriptArguments);

            args = ScriptParse(new[] { "--", "@script.csx", "b", "c" }, baseDirectory: _baseDirectory);
            AssertEx.Equal(new[] { Path.Combine(_baseDirectory, "@script.csx") }, args.SourceFiles.Select(f => f.Path));
            AssertEx.Equal(new[] { "b", "c" }, args.ScriptArguments);

            args = ScriptParse(new[] { "--", "-script.csx", "b", "c" }, baseDirectory: _baseDirectory);
            AssertEx.Equal(new[] { Path.Combine(_baseDirectory, "-script.csx") }, args.SourceFiles.Select(f => f.Path));
            AssertEx.Equal(new[] { "b", "c" }, args.ScriptArguments);

            args = ScriptParse(new[] { "script.csx", "--", "b", "c" }, baseDirectory: _baseDirectory);
            AssertEx.Equal(new[] { Path.Combine(_baseDirectory, "script.csx") }, args.SourceFiles.Select(f => f.Path));
            AssertEx.Equal(new[] { "--", "b", "c" }, args.ScriptArguments);

            args = ScriptParse(new[] { "script.csx", "a", "b", "c" }, baseDirectory: _baseDirectory);
            AssertEx.Equal(new[] { Path.Combine(_baseDirectory, "script.csx") }, args.SourceFiles.Select(f => f.Path));
            AssertEx.Equal(new[] { "a", "b", "c" }, args.ScriptArguments);

            args = ScriptParse(new[] { "script.csx", "a", "--", "b", "c" }, baseDirectory: _baseDirectory);
            AssertEx.Equal(new[] { Path.Combine(_baseDirectory, "script.csx") }, args.SourceFiles.Select(f => f.Path));
            AssertEx.Equal(new[] { "a", "--", "b", "c" }, args.ScriptArguments);

            args = ScriptParse(new[] { "-i", "script.csx", "a", "b", "c" }, baseDirectory: _baseDirectory);
            Assert.True(args.InteractiveMode);
            AssertEx.Equal(new[] { Path.Combine(_baseDirectory, "script.csx") }, args.SourceFiles.Select(f => f.Path));
            AssertEx.Equal(new[] { "a", "b", "c" }, args.ScriptArguments);

            args = ScriptParse(new[] { "-i", "--", "script.csx", "a", "b", "c" }, baseDirectory: _baseDirectory);
            Assert.True(args.InteractiveMode);
            AssertEx.Equal(new[] { Path.Combine(_baseDirectory, "script.csx") }, args.SourceFiles.Select(f => f.Path));
            AssertEx.Equal(new[] { "a", "b", "c" }, args.ScriptArguments);

            args = ScriptParse(new[] { "-i", "--", "--", "--" }, baseDirectory: _baseDirectory);
            Assert.True(args.InteractiveMode);
            AssertEx.Equal(new[] { Path.Combine(_baseDirectory, "--") }, args.SourceFiles.Select(f => f.Path));
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
        public void PathMapParser()
        {
            var parsedArgs = DefaultParse(new[] { "/pathmap:", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(ImmutableArray.Create<KeyValuePair<string, string>>(), parsedArgs.PathMap);

            parsedArgs = DefaultParse(new[] { "/pathmap:K1=V1", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(KeyValuePair.Create("K1", "V1"), parsedArgs.PathMap[0]);

            parsedArgs = DefaultParse(new[] { "/pathmap:K1=V1,K2=V2", "a.cs" }, _baseDirectory);
            parsedArgs.Errors.Verify();
            Assert.Equal(KeyValuePair.Create("K1", "V1"), parsedArgs.PathMap[0]);
            Assert.Equal(KeyValuePair.Create("K2", "V2"), parsedArgs.PathMap[1]);

            parsedArgs = DefaultParse(new[] { "/pathmap:,,,", "a.cs" }, _baseDirectory);
            Assert.Equal(4, parsedArgs.Errors.Count());
            Assert.Equal((int)ErrorCode.ERR_InvalidPathMap, parsedArgs.Errors[0].Code);
            Assert.Equal((int)ErrorCode.ERR_InvalidPathMap, parsedArgs.Errors[1].Code);
            Assert.Equal((int)ErrorCode.ERR_InvalidPathMap, parsedArgs.Errors[2].Code);
            Assert.Equal((int)ErrorCode.ERR_InvalidPathMap, parsedArgs.Errors[3].Code);

            parsedArgs = DefaultParse(new[] { "/pathmap:k=,=v", "a.cs" }, _baseDirectory);
            Assert.Equal(2, parsedArgs.Errors.Count());
            Assert.Equal((int)ErrorCode.ERR_InvalidPathMap, parsedArgs.Errors[0].Code);
            Assert.Equal((int)ErrorCode.ERR_InvalidPathMap, parsedArgs.Errors[1].Code);

            parsedArgs = DefaultParse(new[] { "/pathmap:k=v=bad", "a.cs" }, _baseDirectory);
            Assert.Equal(1, parsedArgs.Errors.Count());
            Assert.Equal((int)ErrorCode.ERR_InvalidPathMap, parsedArgs.Errors[0].Code);
        }
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    internal abstract class CompilationStartedAnalyzer : DiagnosticAnalyzer
    {
        public override abstract ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
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
