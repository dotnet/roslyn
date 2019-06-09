// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace Microsoft.CodeAnalysis.CSharp.Scripting.Hosting
{
    internal static class Csi
    {
        private const string InteractiveResponseFileName = "csi.rsp";

        internal static int Main(string[] args)
        {
            try
            {
                // Note that AppContext.BaseDirectory isn't necessarily the directory containing csi.exe.
                // For example, when executed via corerun it's the directory containing corerun.
                string csiDirectory = Path.GetDirectoryName(typeof(Csi).GetTypeInfo().Assembly.ManifestModule.FullyQualifiedName);

                var buildPaths = new BuildPaths(
                    clientDir: csiDirectory,
                    workingDir: Directory.GetCurrentDirectory(),
                    sdkDir: RuntimeMetadataReferenceResolver.GetDesktopFrameworkDirectory(),
                    tempDir: Path.GetTempPath());

                var compiler = new CSharpInteractiveCompiler(
                    responseFile: Path.Combine(csiDirectory, InteractiveResponseFileName),
                    buildPaths: buildPaths,
                    args: args,
                    analyzerLoader: new NotImplementedAnalyzerLoader());

                var runner = new CommandLineRunner(
                    ConsoleIO.Default,
                    compiler,
                    CSharpScriptCompiler.Instance,
                    CSharpObjectFormatter.Instance);

                return runner.RunInteractive();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return 1;
            }
        }
    }
}
