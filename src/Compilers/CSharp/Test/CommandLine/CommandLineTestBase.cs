// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using static Roslyn.Test.Utilities.TestMetadata;

namespace Microsoft.CodeAnalysis.CSharp.CommandLine.UnitTests
{
    public abstract class CommandLineTestBase : CSharpTestBase
    {
        public string WorkingDirectory { get; }
        public string SdkDirectory { get; }
        public string MscorlibFullPath { get; }

        public CommandLineTestBase()
        {
            WorkingDirectory = TempRoot.Root;
            SdkDirectory = getSdkDirectory(Temp);
            MscorlibFullPath = Path.Combine(SdkDirectory, "mscorlib.dll");

            // This will return a directory which contains mscorlib for use in the compiler instances created for
            // this set of tests
            string getSdkDirectory(TempRoot temp)
            {
                if (ExecutionConditionUtil.IsCoreClr)
                {
                    var dir = temp.CreateDirectory();
                    File.WriteAllBytes(Path.Combine(dir.Path, "mscorlib.dll"), ResourcesNet461.mscorlib);
                    return dir.Path;
                }

                return RuntimeEnvironment.GetRuntimeDirectory();
            }
        }

        internal CSharpCommandLineArguments DefaultParse(IEnumerable<string> args, string baseDirectory, string sdkDirectory = null, string additionalReferenceDirectories = null)
        {
            sdkDirectory = sdkDirectory ?? SdkDirectory;
            return CSharpCommandLineParser.Default.Parse(args, baseDirectory, sdkDirectory, additionalReferenceDirectories);
        }

        internal MockCSharpCompiler CreateCSharpCompiler(string[] args, ImmutableArray<DiagnosticAnalyzer> analyzers = default, ImmutableArray<ISourceGenerator> generators = default, AnalyzerAssemblyLoader loader = null)
        {
            return CreateCSharpCompiler(null, WorkingDirectory, args, analyzers, generators, loader);
        }

        internal MockCSharpCompiler CreateCSharpCompiler(string responseFile, string workingDirectory, string[] args, ImmutableArray<DiagnosticAnalyzer> analyzers = default, ImmutableArray<ISourceGenerator> generators = default, AnalyzerAssemblyLoader loader = null)
        {
            var buildPaths = RuntimeUtilities.CreateBuildPaths(workingDirectory, sdkDirectory: SdkDirectory);
            return new MockCSharpCompiler(responseFile, buildPaths, args, analyzers, generators, loader);
        }
    }
}
