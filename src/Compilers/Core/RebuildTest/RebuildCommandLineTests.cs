// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Rebuild.UnitTests
{
    public sealed partial class RebuildCommandLineTests : CSharpTestBase
    {
        private record CommandInfo(string CommandLine, string PeFileName, string? PdbFileName, string? CommandLineSuffix = null);

        internal static BuildPaths BuildPaths { get; } = TestableCompiler.StandardBuildPaths;
        internal static string RootDirectory { get; } = TestableCompiler.RootDirectory;
        internal static string OutputDirectory { get; } = Path.Combine(TestableCompiler.RootDirectory, "output");

        public ITestOutputHelper TestOutputHelper { get; }
        public Dictionary<string, TestableFile> FilePathToStreamMap { get; } = new Dictionary<string, TestableFile>(StringComparer.OrdinalIgnoreCase);

        public RebuildCommandLineTests(ITestOutputHelper testOutputHelper)
        {
            TestOutputHelper = testOutputHelper;
        }

        private void AddSourceFile(string filePath, string content)
        {
            FilePathToStreamMap.Add(Path.Combine(BuildPaths.WorkingDirectory, filePath), new TestableFile(content));
        }

        private void AddOutputFile(ref string? filePath)
        {
            if (filePath is object)
            {
                filePath = Path.Combine(OutputDirectory, filePath);
                FilePathToStreamMap.Add(filePath, new TestableFile());
            }
        }

        private static IEnumerable<CommandInfo> PermutateDllKinds(CommandInfo commandInfo)
        {
            yield return commandInfo with
            {
                CommandLine = commandInfo.CommandLine + " /target:library",
                PeFileName = Path.ChangeExtension(commandInfo.PeFileName, "dll"),
            };
            yield return commandInfo with
            {
                CommandLine = commandInfo.CommandLine + " /target:module",
                PeFileName = Path.ChangeExtension(commandInfo.PeFileName, "netmodule"),
            };
            yield return commandInfo with
            {
                CommandLine = commandInfo.CommandLine + " /target:winmdobj",
                PeFileName = Path.ChangeExtension(commandInfo.PeFileName, "dll"),
            };
        }

        private static IEnumerable<CommandInfo> PermutateExeKinds(CommandInfo commandInfo)
        {
            yield return commandInfo with
            {
                CommandLine = commandInfo.CommandLine + " /target:exe",
                PeFileName = Path.ChangeExtension(commandInfo.PeFileName, "exe"),
            };
            yield return commandInfo with
            {
                CommandLine = commandInfo.CommandLine + " /target:winexe",
                PeFileName = Path.ChangeExtension(commandInfo.PeFileName, "exe"),
            };
            yield return commandInfo with
            {
                CommandLine = commandInfo.CommandLine + " /target:appcontainerexe",
                PeFileName = Path.ChangeExtension(commandInfo.PeFileName, "exe"),
            };
        }

        private void VerifyRoundTrip(CommonCompiler commonCompiler, string peFilePath, string? pdbFilePath = null, CancellationToken cancellationToken = default)
        {
            Assert.True(commonCompiler.Arguments.CompilationOptions.Deterministic);
            var (result, output) = commonCompiler.Run(cancellationToken);
            TestOutputHelper.WriteLine(output);
            Assert.Equal(0, result);

            var peStream = FilePathToStreamMap[peFilePath].GetStream();
            var pdbStream = pdbFilePath is object ? FilePathToStreamMap[pdbFilePath].GetStream() : null;

            using var writer = new StringWriter();
            var compilation = commonCompiler.CreateCompilation(
                writer,
                touchedFilesLogger: null,
                errorLoggerOpt: null,
                analyzerConfigOptions: default,
                globalConfigOptions: default);
            AssertEx.NotNull(compilation);
            RoundTripUtil.VerifyCompilationOptions(commonCompiler.Arguments.CompilationOptions, compilation.Options);

            RoundTripUtil.VerifyRoundTrip(
                peStream,
                pdbStream,
                Path.GetFileName(peFilePath),
                new CompilationRebuildArtifactResolver(compilation));
        }

        private void AddCSharpSourceFiles()
        {
            AddSourceFile("hw.cs", @"
using System;
Console.WriteLine(""Hello World"");
");

            AddSourceFile("lib1.cs", @"
using System;

class Library
{
    void Method()
    {
        var lib = new Library();
        Console.WriteLine(lib);
    }
}
");

            AddSourceFile("lib2.cs", @"
extern alias SystemRuntime;
using System;

class Library
{
    void Method()
    {
        System.Reflection.Assembly a1 = null;
        SystemRuntime::System.Reflection.Assembly a2 = null;
        Console.WriteLine(a1);
        Console.WriteLine(a2);
    }
}
");

            AddSourceFile("lib3.cs", @"
extern alias SystemRuntime1;
extern alias SystemRuntime2;
using System;

class Library
{
    void Method()
    {
        System.Reflection.Assembly a1 = null;
        SystemRuntime1::System.Reflection.Assembly a2 = null;
        SystemRuntime2::System.Reflection.Assembly a3 = null;
        Console.WriteLine(a1);
        Console.WriteLine(a2);
        Console.WriteLine(a3);
    }
}
");

            AddSourceFile("lib4.cs", @"
using System;

class Library4
{
#line 42 ""data.txt""
    void Method()
    {
    }
}
");

            AddSourceFile("lib5.cs", @"
using System;

class Library5
{
#line 42 ""data.txt""
    void Method()
    {
    }
}
");

            AddSourceFile(Path.Combine("dir1", "lib1.cs"), @"
using System;

namespace Nested
{
    #line 13 ""data.txt""
    class NestedLibrary
    {
        void Method()
        {
        }
    }
}
");
        }

        public static IEnumerable<object?[]> GetCSharpData()
        {
            var list = new List<object?[]>();

            Permutate(
                new CommandInfo("hw.cs", "test.exe", null),
                PermutateOptimizations, PermutateExeKinds, PermutatePdbFormat);
            Permutate(new CommandInfo("lib1.cs", "test.dll", null),
                PermutateOptimizations, PermutateDllKinds, PermutatePdbFormat, PermutatePathMap);
            Permutate(new CommandInfo("lib2.cs /target:library /r:SystemRuntime=System.Runtime.dll /debug:embedded", "test.dll", null),
                PermutateOptimizations);
            Permutate(new CommandInfo("lib3.cs /target:library", "test.dll", null),
                PermutateOptimizations, PermutateExternAlias, PermutatePdbFormat);
            Permutate(new CommandInfo("lib4.cs /target:library", "test.dll", null),
                PermutateOptimizations, PermutatePdbFormat, PermutatePathMap);

            // This uses a #line directive with the same file name but in different source directories.
            // Need to make sure that we map the same file name but different base paths correctly
            Permutate(new CommandInfo($"lib4.cs {Path.Combine("dir1", "lib1.cs")} /target:library", "test.dll", null),
                PermutatePdbFormat, PermutatePathMap);

            Permutate(new CommandInfo("lib4.cs lib5.cs /target:library", "test.dll", null),
                PermutateOptimizations, PermutatePdbFormat, PermutatePathMap);

            return list;

            void Permutate(CommandInfo commandInfo, params Func<CommandInfo, IEnumerable<CommandInfo>>[] permutations)
            {
                IEnumerable<CommandInfo> e = new[] { commandInfo };
                foreach (var p in permutations)
                {
                    e = e.SelectMany(p);
                }

                Add(e);
            }

            static IEnumerable<CommandInfo> PermutatePathMap(CommandInfo commandInfo)
            {
                yield return commandInfo;
                yield return commandInfo with
                {
                    CommandLine = commandInfo.CommandLine + $" /pathmap:{RootDirectory}=/root/test",
                };
                yield return commandInfo with
                {
                    CommandLine = commandInfo.CommandLine + $@" /pathmap:{RootDirectory}=j:\other_root",
                };

                // Path map doesn't care about path legality, it's a simple substitute 
                yield return commandInfo with
                {
                    CommandLine = commandInfo.CommandLine + $@" /pathmap:""{RootDirectory}=???""",
                };
            }

            // Permutate the alias before and after the standard references so that we make sure the 
            // rebuild is resistent to the ordering of aliases. 
            static IEnumerable<CommandInfo> PermutateExternAlias(CommandInfo commandInfo)
            {
                var alias = @" /r:SystemRuntime1=System.Runtime.dll /r:SystemRuntime2=System.Runtime.dll";

                yield return commandInfo with
                {
                    CommandLine = commandInfo.CommandLine + alias
                };

                yield return commandInfo with
                {
                    CommandLineSuffix = commandInfo.CommandLineSuffix + alias
                };
            }

            static IEnumerable<CommandInfo> PermutatePdbFormat(CommandInfo commandInfo)
            {
                yield return commandInfo with
                {
                    CommandLine = commandInfo.CommandLine + " /debug:portable",
                    PdbFileName = "test.pdb"
                };

                yield return commandInfo with
                {
                    CommandLine = commandInfo.CommandLine + " /debug:embedded"
                };
            }

            static IEnumerable<CommandInfo> PermutateOptimizations(CommandInfo commandInfo)
            {
                // No options at all for optimization
                yield return commandInfo;
                yield return commandInfo with
                {
                    CommandLine = commandInfo.CommandLine + " /debug+ /optimize+"
                };
                yield return commandInfo with
                {
                    CommandLine = commandInfo.CommandLine + " /debug+ /optimize-"
                };
                yield return commandInfo with
                {
                    CommandLine = commandInfo.CommandLine + " /optimize-"
                };
                yield return commandInfo with
                {
                    CommandLine = commandInfo.CommandLine + " /optimize+"
                };
            }

            void Add(IEnumerable<CommandInfo> commandInfos)
            {
                foreach (var commandInfo in commandInfos)
                {
                    list.Add(new object?[] { commandInfo.CommandLine, commandInfo.PeFileName, commandInfo.PdbFileName, commandInfo.CommandLineSuffix });
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetCSharpData))]
        public void CSharp(string commandLine, string peFilePath, string? pdbFilePath, string? commandLineSuffix)
        {
            TestOutputHelper.WriteLine($"Command Line: {commandLine}");
            AddCSharpSourceFiles();
            var args = new List<string>(commandLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            args.Add("/nostdlib");
            args.Add("/deterministic");

            AddOutputFile(ref peFilePath!);
            args.Add($"/out:{peFilePath}");
            AddOutputFile(ref pdbFilePath);
            if (pdbFilePath is object)
            {
                args.Add($"/pdb:{pdbFilePath}");
            }

            if (commandLineSuffix is object)
            {
                args.AddRange(commandLineSuffix.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            }

            TestOutputHelper.WriteLine($"Final Line: {string.Join(" ", args)}");
            var compiler = TestableCompiler.CreateCSharpNetCoreApp(
                TestableFileSystem.CreateForMap(FilePathToStreamMap),
                BuildPaths,
                args);
            VerifyRoundTrip(compiler.Compiler, peFilePath, pdbFilePath);
        }

        private void AddVisualBasicSourceFiles()
        {
            AddSourceFile("hw.vb", @"
Imports System
Public Module M
    Public Sub Main()
        Console.WriteLine(CStr(True))
    End Sub
End Module
");

            AddSourceFile("lib1.vb", @"
Imports System
Public Module M
    Public Function Add(left As Integer, right As Integer) As Integer
        return left + right
    End Function
End Module
");

            AddSourceFile("lib2.vb", @"
Imports System
Public Module Lib2
#ExternalSource(""data.txt"", 30)
    Public Function Add(left As Integer, right As Integer) As Integer
        return left + right
    End Function
#End ExternalSource
End Module
");

            AddSourceFile(Path.Combine("dir1", "lib1.vb"), @"
Imports System
Namespace Nested
    Public Module Lib1
#ExternalSource(""data.txt"", 30)
        Public Function Add(left As Integer, right As Integer) As Integer
            return left + right
        End Function
#End ExternalSource
    End Module
End Namespace
");
        }

        public static IEnumerable<object?[]> GetVisualBasicData()
        {
            var list = new List<object?[]>();

            Permutate(
                new CommandInfo("hw.vb /debug:embedded", "test.exe", null),
                PermutateOptimizations, PermutateRuntime, PermutateExeKinds);
            Permutate(
                new CommandInfo("lib1.vb /target:library /debug:embedded", "test.dll", null),
                PermutateOptimizations, PermutateRuntime);
            Permutate(
                new CommandInfo(@"lib1.vb /debug:embedded /d:_MYTYPE=""Empty"" /vbruntime:Microsoft.VisualBasic.dll", "test.dll", null),
                PermutateOptimizations, PermutateDllKinds);
            Permutate(
                new CommandInfo("lib2.vb /target:library /debug:embedded", "test.dll", null),
                PermutatePathMap, PermutateRuntime);

            // This uses a #ExternalSource directive with the same file name but in different source directories.
            // Need to make sure that we map the same file name but different base paths correctly
            Permutate(
                new CommandInfo(@$"lib2.vb {Path.Combine("dir1", "lib1.vb")} /target:library /debug:embedded", "test.dll", null),
                PermutatePathMap, PermutateRuntime);

            return list;

            void Permutate(CommandInfo commandInfo, params Func<CommandInfo, IEnumerable<CommandInfo>>[] permutations)
            {
                IEnumerable<CommandInfo> e = new[] { commandInfo };
                foreach (var p in permutations)
                {
                    e = e.SelectMany(p);
                }

                Add(e);
            }

            static IEnumerable<CommandInfo> PermutatePathMap(CommandInfo commandInfo)
            {
                yield return commandInfo;
                yield return commandInfo with
                {
                    CommandLine = commandInfo.CommandLine + $" /pathmap:{RootDirectory}=/root/test",
                };
            }

            static IEnumerable<CommandInfo> PermutateOptimizations(CommandInfo commandInfo)
            {
                // No options at all for optimization
                yield return commandInfo;
                yield return commandInfo with
                {
                    CommandLine = commandInfo.CommandLine + " /debug+ /optimize+"
                };
                yield return commandInfo with
                {
                    CommandLine = commandInfo.CommandLine + " /debug+ /optimize-"
                };
                yield return commandInfo with
                {
                    CommandLine = commandInfo.CommandLine + " /optimize-"
                };
                yield return commandInfo with
                {
                    CommandLine = commandInfo.CommandLine + " /optimize+"
                };
            }

            static IEnumerable<CommandInfo> PermutateRuntime(CommandInfo commandInfo)
            {
                yield return commandInfo with
                {
                    CommandLine = commandInfo.CommandLine + " /vbruntime*"
                };

                yield return commandInfo with
                {
                    CommandLine = commandInfo.CommandLine + @" /d:_MYTYPE=""Empty"" /vbruntime:Microsoft.VisualBasic.dll"
                };
            }

            void Add(IEnumerable<CommandInfo> commandInfos)
            {
                foreach (var commandInfo in commandInfos)
                {
                    list.Add(new object?[] { commandInfo.CommandLine, commandInfo.PeFileName, commandInfo.PdbFileName });
                }
            }
        }

        [ConditionalTheory(typeof(IsEnglishLocal))]
        [MemberData(nameof(GetVisualBasicData))]
        public void VisualBasic(string commandLine, string peFilePath, string? pdbFilePath)
        {
            TestOutputHelper.WriteLine($"Command Line: {commandLine}");
            AddVisualBasicSourceFiles();
            var args = new List<string>(commandLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            args.Add("/nostdlib");
            args.Add("/deterministic");
            AddOutputFile(ref peFilePath!);
            args.Add($"/out:{peFilePath}");
            AddOutputFile(ref pdbFilePath);
            if (pdbFilePath is object)
            {
                args.Add($"/pdb:{pdbFilePath}");
            }

            TestOutputHelper.WriteLine($"Final Line: {string.Join(" ", args)}");
            var compiler = TestableCompiler.CreateBasicNetCoreApp(
                TestableFileSystem.CreateForMap(FilePathToStreamMap),
                BuildPaths,
                BasicRuntimeOption.Manual,
                args);
            VerifyRoundTrip(compiler.Compiler, peFilePath, pdbFilePath);
        }
    }
}
