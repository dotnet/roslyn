// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.\

using System;

namespace AnalyzerRunner
{
    internal class Utilities
    {
        internal static void WriteLine(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        internal static void PrintHelp()
        {
            Console.WriteLine("Usage: AnalyzerRunner [options] <AnalyzerAssemblyOrFolder> <Solution>");
            Console.WriteLine("Options:");
            Console.WriteLine("/all             Run all StyleCopAnalyzers analyzers, including ones that are disabled by default");
            Console.WriteLine("/stats           Display statistics of the solution");
            Console.WriteLine("/codefixes       Test single code fixes");
            Console.WriteLine("/fixall          Test fix all providers");
            Console.WriteLine("/id:<id>         Enable analyzer with diagnostic ID < id > (when this is specified, only this analyzer is enabled)");
            Console.WriteLine("/apply           Write code fix changes back to disk");
            Console.WriteLine("/concurrent      Executes analyzers in concurrent mode");
            Console.WriteLine("/suppressed      Reports suppressed diagnostics");
        }
    }
}
