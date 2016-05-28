// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Scripting.Hosting
{
    internal static class Csi
    {
        private const string InteractiveResponseFileName = "csi.rsp";

        internal static int Main(string[] args)
        {
            try
            {
                var responseFile = Path.Combine(AppContext.BaseDirectory, InteractiveResponseFileName);

                var compiler = new CSharpInteractiveCompiler(
                    responseFile: responseFile,
                    baseDirectory: Directory.GetCurrentDirectory(),
                    sdkDirectoryOpt: CorLightup.Desktop.TryGetRuntimeDirectory(),
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
