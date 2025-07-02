// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis.CommandLine;
using Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax;

namespace Microsoft.CodeAnalysis.CSharp.CommandLine
{
    public class Program
    {
        public static int Main(string[] args)
        {
            foreach (var file in Directory.EnumerateFiles(@"d:\github\roslyn2", "*.cs", SearchOption.AllDirectories))
            {
                SyntaxFactory.ParseSyntaxTree(File.ReadAllText(file), path: file);
            }

            Console.WriteLine(SlidingTextWindow.GetTextOutsideWindowCount);
            Console.WriteLine(SlidingTextWindow.GetTextInsideWindowCount);
            Console.WriteLine(SlidingTextWindow.TotalTextSize);

            Console.ReadLine();

            try
            {
                return MainCore(args);
            }
            catch (FileNotFoundException e)
            {
                // Catch exception from missing compiler assembly.
                // Report the exception message and terminate the process.
                Console.WriteLine(e.Message);
                return CommonCompiler.Failed;
            }
        }

        private static int MainCore(string[] args)
        {
            using var logger = new CompilerServerLogger($"csc {Process.GetCurrentProcess().Id}");

#if BOOTSTRAP
            ExitingTraceListener.Install(logger);
#endif

            return BuildClient.Run(args, RequestLanguage.CSharpCompile, Csc.Run, BuildClient.GetCompileOnServerFunc(logger), logger);
        }

        public static int Run(string[] args, string clientDir, string workingDir, string sdkDir, string? tempDir, TextWriter textWriter, IAnalyzerAssemblyLoader analyzerLoader)
            => Csc.Run(args, new BuildPaths(clientDir: clientDir, workingDir: workingDir, sdkDir: sdkDir, tempDir: tempDir), textWriter, analyzerLoader);
    }
}
