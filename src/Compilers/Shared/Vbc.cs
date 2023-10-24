// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CommandLine;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Microsoft.CodeAnalysis.VisualBasic.CommandLine
{
    internal sealed class Vbc : VisualBasicCompiler
    {
        internal Vbc(string responseFile, BuildPaths buildPaths, string[] args, IAnalyzerAssemblyLoader analyzerLoader)
            : base(VisualBasicCommandLineParser.Default, responseFile, args, buildPaths, Environment.GetEnvironmentVariable("LIB"), analyzerLoader)
        {
        }

        internal static int Run(string[] args, BuildPaths buildPaths, TextWriter textWriter, IAnalyzerAssemblyLoader analyzerLoader)
        {
            FatalError.SetHandlers(FailFast.Handler, nonFatalHandler: null);

            var responseFile = Path.Combine(buildPaths.ClientDirectory, VisualBasicCompiler.ResponseFileName);
            var compiler = new Vbc(responseFile, buildPaths, args, analyzerLoader);
            return ConsoleUtil.RunWithUtf8Output(compiler.Arguments.Utf8Output, textWriter, tw => compiler.Run(tw));
        }
    }
}

