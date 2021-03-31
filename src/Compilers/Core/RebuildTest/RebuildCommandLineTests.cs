﻿// Licensed to the .NET Foundation under one or more agreements.
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
        private record CommandInfo(string CommandLine, string PeFileName, string? PdbFileName);

        internal static string RootDirectory => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"q:\" : "/";
        internal static string ClientDirectory => Path.Combine(RootDirectory, "compiler");
        internal static string WorkingDirectory => Path.Combine(RootDirectory, "rebuild");
        internal static string SdkDirectory => Path.Combine(RootDirectory, "sdk");
        internal static string OutputDirectory => Path.Combine(RootDirectory, "output");

        internal static BuildPaths StandardBuildPaths => new BuildPaths(ClientDirectory, WorkingDirectory, SdkDirectory, tempDir: null);

        public ITestOutputHelper TestOutputHelper { get; }
        public Dictionary<string, TestableFile> FilePathToStreamMap { get; } = new Dictionary<string, TestableFile>(StringComparer.OrdinalIgnoreCase);

        public RebuildCommandLineTests(ITestOutputHelper testOutputHelper)
        {
            TestOutputHelper = testOutputHelper;
        }

        private void AddSourceFile(string filePath, string content)
        {
            FilePathToStreamMap.Add(Path.Combine(WorkingDirectory, filePath), new TestableFile(content));
        }

        private void AddReference(string filePath, byte[] imageBytes)
        {
            FilePathToStreamMap.Add(Path.Combine(SdkDirectory, filePath), new TestableFile(imageBytes));
        }

        private void AddOutputFile(ref string? filePath)
        {
            if (filePath is object)
            {
                filePath = Path.Combine(OutputDirectory, filePath);
                FilePathToStreamMap.Add(filePath, new TestableFile());
            }
        }

        private void VerifyRoundTrip(CommonCompiler commonCompiler, string peFilePath, string? pdbFilePath = null, CancellationToken cancellationToken = default)
        {
            Assert.True(commonCompiler.Arguments.CompilationOptions.Deterministic);
            using var writer = new StringWriter();
            commonCompiler.FileSystem = TestableFileSystem.CreateForMap(FilePathToStreamMap);
            var result = commonCompiler.Run(writer, cancellationToken);
            TestOutputHelper.WriteLine(writer.ToString());
            Assert.Equal(0, result);

            var peStream = FilePathToStreamMap[peFilePath].GetStream();
            var pdbStream = pdbFilePath is object ? FilePathToStreamMap[pdbFilePath].GetStream() : null;

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
                compilation.SyntaxTrees.ToImmutableArray(),
                compilation.References.ToImmutableArray());
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
        }

        public static IEnumerable<object?[]> GetCSharpData()
        {
            var list = new List<object?[]>();

            Add(Permutate(new CommandInfo("hw.cs /target:exe /debug:embedded", "test.exe", null)));
            Add(Permutate(new CommandInfo("lib1.cs /target:library /debug:embedded", "test.dll", null)));
            Add(Permutate(new CommandInfo("lib2.cs /target:library /r:SystemRuntime=System.Runtime.dll /debug:embedded", "test.dll", null)));
            Add(Permutate(new CommandInfo("lib3.cs /target:library /r:SystemRuntime1=System.Runtime.dll /r:SystemRuntime2=System.Runtime.dll /debug:embedded", "test.dll", null)));

            return list;

            IEnumerable<CommandInfo> Permutate(CommandInfo commandInfo)
            {
                IEnumerable<CommandInfo> e = new[] { commandInfo };
                e = e.SelectMany(PermutateOptimizations);
                return e;
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
                    list.Add(new object?[] { commandInfo.CommandLine, commandInfo.PeFileName, commandInfo.PdbFileName });
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetCSharpData))]
        public void CSharp(string commandLine, string peFilePath, string? pdbFilePath)
        {
            TestOutputHelper.WriteLine($"Command Line: {commandLine}");
            AddCSharpSourceFiles();
            var args = new List<string>(commandLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            args.Add("/nostdlib");
            args.Add("/deterministic");
            foreach (var referenceInfo in TestMetadata.ResourcesNetCoreApp.All)
            {
                AddReference(referenceInfo.FileName, referenceInfo.ImageBytes);
                args.Add($"/r:{referenceInfo.FileName}");
            }

            AddOutputFile(ref peFilePath!);
            args.Add($"/out:{peFilePath}");
            AddOutputFile(ref pdbFilePath);
            if (pdbFilePath is object)
            {
                args.Add($"/pdb:{pdbFilePath}");
            }

            TestOutputHelper.WriteLine($"Final Line: {string.Join(" ", args)}");
            var compiler = new CSharpRebuildCompiler(args.ToArray());
            VerifyRoundTrip(compiler, peFilePath, pdbFilePath);
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
        }

        public static IEnumerable<object?[]> GetVisualBasicData()
        {
            var list = new List<object?[]>();

            Add(Permutate(new CommandInfo("hw.vb /target:exe /debug:embedded", "test.exe", null)));

            return list;

            IEnumerable<CommandInfo> Permutate(CommandInfo commandInfo)
            {
                IEnumerable<CommandInfo> e = new[] { commandInfo };
                e = e.SelectMany(PermutateOptimizations);
                e = e.SelectMany(PermutateRuntime);
                return e;
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
            foreach (var referenceInfo in TestMetadata.ResourcesNetCoreApp.All)
            {
                AddReference(referenceInfo.FileName, referenceInfo.ImageBytes);

                // The command line needs to make a decision about how to embed the VB runtime
                if (referenceInfo.FileName != "Microsoft.VisualBasic.dll")
                {
                    args.Add($"/r:{referenceInfo.FileName}");
                }
            }

            AddOutputFile(ref peFilePath!);
            args.Add($"/out:{peFilePath}");
            AddOutputFile(ref pdbFilePath);
            if (pdbFilePath is object)
            {
                args.Add($"/pdb:{pdbFilePath}");
            }

            TestOutputHelper.WriteLine($"Final Line: {string.Join(" ", args)}");
            var compiler = new VisualBasicRebuildCompiler(args.ToArray());
            VerifyRoundTrip(compiler, peFilePath, pdbFilePath);
        }
    }
}
