// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Scripting.CSharp;

namespace Microsoft.CodeAnalysis.Scripting.Hosting.CSharp
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
                    responseFile,
                    AppContext.BaseDirectory, 
                    args,
                    new NotImplementedAnalyzerLoader());

                var runner = new CommandLineRunner(
                    ConsoleIO.Default,
                    compiler,
                    CSharpScriptCompiler.Instance,
                    CSharpObjectFormatter.Instance);

                return runner.RunInteractive();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return 1;
            }
        }
    }
}
