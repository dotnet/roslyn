// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;

namespace Roslyn.Test.Utilities
{
    internal readonly struct TestableCompilerFile
    {
        public string FilePath { get; }
        public TestableFile TestableFile { get; }
        public List<byte> Contents => TestableFile.Contents;

        public TestableCompilerFile(string filePath, TestableFile testableFile)
        {
            FilePath = filePath;
            TestableFile = testableFile;
        }
    }

    internal enum BasicRuntimeOption
    {
        Include,
        Exclude,
        Embed,

        // Consumer manually controls the vb runtime via the provided arguments
        Manual,
    }

    /// <summary>
    /// Provides an easy to test version of <see cref="CommonCompiler"/>. This uses <see cref="TestableFileSystem"/> 
    /// to abstract way all of the file system access (typically the hardest part about testing CommonCompiler).
    /// </summary>
    internal sealed class TestableCompiler
    {
        internal static string RootDirectory => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"q:\" : "/";
        internal static BuildPaths StandardBuildPaths => new BuildPaths(
            clientDir: Path.Combine(RootDirectory, "compiler"),
            workingDir: Path.Combine(RootDirectory, "source"),
            sdkDir: Path.Combine(RootDirectory, "sdk"),
            tempDir: null);

        internal CommonCompiler Compiler { get; }
        internal TestableFileSystem FileSystem { get; }
        internal BuildPaths BuildPaths { get; }

        internal TestableCompiler(CommonCompiler compiler, TestableFileSystem fileSystem, BuildPaths buildPaths)
        {
            if (!object.ReferenceEquals(compiler.FileSystem, fileSystem))
            {
                throw new ArgumentException(null, nameof(fileSystem));
            }

            Compiler = compiler;
            FileSystem = fileSystem;
            BuildPaths = buildPaths;
        }

        internal (int Result, string Output) Run(CancellationToken cancellationToken = default)
            => Compiler.Run(cancellationToken);

        internal TestableCompilerFile AddSourceFile(string filePath, string content)
        {
            var file = new TestableFile(content);
            filePath = Path.Combine(BuildPaths.WorkingDirectory, filePath);
            FileSystem.Map.Add(filePath, file);
            return new TestableCompilerFile(filePath, file);
        }

        internal TestableCompilerFile AddReference(string filePath, byte[] imageBytes)
        {
            var file = new TestableFile(imageBytes);
            filePath = Path.Combine(BuildPaths.SdkDirectory!, filePath);
            FileSystem.Map.Add(filePath, file);
            return new TestableCompilerFile(filePath, file);
        }

        internal TestableCompilerFile AddOutputFile(string filePath)
        {
            var file = new TestableFile();
            filePath = Path.Combine(BuildPaths.WorkingDirectory, filePath);
            FileSystem.Map.Add(filePath, file);
            return new TestableCompilerFile(filePath, file);
        }

        internal static TestableCompiler CreateCSharp(
            string[] commandLineArguments,
            TestableFileSystem fileSystem,
            BuildPaths? buildPaths = null)
        {
            var p = buildPaths ?? StandardBuildPaths;
            var compiler = new CSharpCompilerImpl(commandLineArguments, p, fileSystem);
            return new TestableCompiler(compiler, fileSystem, p);
        }

        internal static TestableCompiler CreateCSharpNetCoreApp(
            TestableFileSystem? fileSystem,
            BuildPaths buildPaths,
            IEnumerable<string> commandLineArguments)
        {
            if (buildPaths.SdkDirectory is null)
            {
                throw new ArgumentException(null, nameof(buildPaths));
            }

            fileSystem ??= TestableFileSystem.CreateForMap();
            var args = new List<string>();
            AppendNetCoreApp(fileSystem, buildPaths.SdkDirectory!, args, includeVisualBasicRuntime: false);
            args.AddRange(commandLineArguments);

            var compiler = new CSharpCompilerImpl(args.ToArray(), buildPaths, fileSystem);
            return new TestableCompiler(compiler, fileSystem, buildPaths);
        }

        internal static TestableCompiler CreateCSharpNetCoreApp(
            TestableFileSystem? fileSystem,
            BuildPaths buildPaths,
            params string[] commandLineArguments)
            => CreateCSharpNetCoreApp(fileSystem, buildPaths, (IEnumerable<string>)commandLineArguments);

        internal static TestableCompiler CreateCSharpNetCoreApp(params string[] commandLineArguments)
            => CreateCSharpNetCoreApp(null, StandardBuildPaths, commandLineArguments);

        private sealed class CSharpCompilerImpl : CSharpCompiler
        {
            internal CSharpCompilerImpl(string[] args, BuildPaths buildPaths, TestableFileSystem? fileSystem)
                : base(CSharpCommandLineParser.Default, responseFile: null, args, buildPaths, additionalReferenceDirectories: null, new DefaultAnalyzerAssemblyLoader(), fileSystem: fileSystem)
            {
            }
        }

        internal static TestableCompiler CreateBasic(
            string[] commandLineArguments,
            TestableFileSystem fileSystem,
            BuildPaths? buildPaths = null)
        {
            var p = buildPaths ?? StandardBuildPaths;
            var compiler = new BasicCompilerImpl(commandLineArguments, p, fileSystem);
            return new TestableCompiler(compiler, fileSystem, p);
        }

        internal static TestableCompiler CreateBasicNetCoreApp(
            TestableFileSystem? fileSystem,
            BuildPaths buildPaths,
            BasicRuntimeOption basicRuntimeOption,
            IEnumerable<string> commandLineArguments)
        {
            if (buildPaths.SdkDirectory is null)
            {
                throw new ArgumentException(null, nameof(buildPaths));
            }

            fileSystem ??= TestableFileSystem.CreateForMap();
            var args = new List<string>();
            args.Add("-nostdlib");

            switch (basicRuntimeOption)
            {
                case BasicRuntimeOption.Include:
                    // VB will just find this in the SDK path and auto-add it
                    args.Add($@"-vbruntime:""{Path.Combine(buildPaths.SdkDirectory, "Microsoft.VisualBasic.dll")}""");
                    break;
                case BasicRuntimeOption.Exclude:
                    args.Add("-vbruntime-");
                    break;
                case BasicRuntimeOption.Embed:
                    args.Add("-vbruntime+");
                    break;
                case BasicRuntimeOption.Manual:
                    break;
                default:
                    throw new Exception("invalid value");
            }

            AppendNetCoreApp(fileSystem, buildPaths.SdkDirectory!, args, includeVisualBasicRuntime: false);
            args.AddRange(commandLineArguments);

            var compiler = new BasicCompilerImpl(args.ToArray(), buildPaths, fileSystem);
            return new TestableCompiler(compiler, fileSystem, buildPaths);
        }

        internal static TestableCompiler CreateBasicNetCoreApp(
            TestableFileSystem? fileSystem,
            BuildPaths buildPaths,
            params string[] commandLineArguments)
            => CreateBasicNetCoreApp(fileSystem, buildPaths, BasicRuntimeOption.Include, (IEnumerable<string>)commandLineArguments);

        internal static TestableCompiler CreateBasicNetCoreApp(params string[] commandLineArguments)
            => CreateBasicNetCoreApp(null, StandardBuildPaths, BasicRuntimeOption.Include, commandLineArguments);

        private sealed class BasicCompilerImpl : VisualBasicCompiler
        {
            internal BasicCompilerImpl(string[] args, BuildPaths buildPaths, TestableFileSystem? fileSystem)
                : base(VisualBasicCommandLineParser.Default, responseFile: null, args, buildPaths, additionalReferenceDirectories: null, new DefaultAnalyzerAssemblyLoader(), fileSystem: fileSystem)
            {
            }
        }

        private static void AppendNetCoreApp(TestableFileSystem fileSystem, string sdkPath, List<string> commandLineArgs, bool includeVisualBasicRuntime)
        {
            Debug.Assert(fileSystem.UsingMap);
            foreach (var referenceInfo in NetCoreApp.AllReferenceInfos)
            {
                fileSystem.Map[Path.Combine(sdkPath, referenceInfo.FileName)] = new TestableFile(referenceInfo.ImageBytes);

                // The command line needs to make a decision about how to embed the VB runtime
                if (!(!includeVisualBasicRuntime && referenceInfo.FileName == "Microsoft.VisualBasic.dll"))
                {
                    commandLineArgs.Add($@"/reference:{referenceInfo.FileName}");
                }
            }
        }
    }
}
