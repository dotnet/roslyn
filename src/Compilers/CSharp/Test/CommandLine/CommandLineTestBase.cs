﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Caravela.Compiler;
using PostSharp.Backstage.Licensing.Consumption;
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

        internal MockCSharpCompiler CreateCSharpCompiler(string[] args, ImmutableArray<DiagnosticAnalyzer> analyzers = default, ImmutableArray<ISourceGenerator> generators = default, AnalyzerAssemblyLoader loader = null, GeneratorDriverCache driverCache = null)
        {
            // <Caravela>
            return CreateCSharpCompiler(null, WorkingDirectory, args, analyzers, generators, default, loader, driverCache);
            // </Caravela>
        }

        internal MockCSharpCompiler CreateCSharpCompiler(string responseFile, string workingDirectory, string[] args, ImmutableArray<DiagnosticAnalyzer> analyzers = default, ImmutableArray<ISourceGenerator> generators = default, ImmutableArray<ISourceTransformer> transformers = default, AnalyzerAssemblyLoader loader = null, GeneratorDriverCache driverCache = null, ILicenseConsumptionManager customLicenseConsumptionManager = null)
        {
            var buildPaths = RuntimeUtilities.CreateBuildPaths(workingDirectory, sdkDirectory: SdkDirectory);
            // <Caravela>
            return new MockCSharpCompiler(responseFile, buildPaths, args, analyzers, generators, transformers, loader, driverCache, customLicenseConsumptionManager);
            // </Caravela>
        }
    }
}
