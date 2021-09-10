// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Mono.Options;

internal static class Program
{
    internal const int ExitFailure = 1;
    internal const int ExitSuccess = 0;

    public static int Main(string[] args)
    {
        string? dllPath = null;
        string? pdbPath = null;

        var options = new OptionSet()
        {
            { "dll=", "Path to assembly file", (string s) => dllPath = s },
            { "pdb=", "Path to PDB file", (string s) => pdbPath = s }
        };
        options.Parse(args);

        if (dllPath is null)
        {
            Console.Error.WriteLine($"--dll is required");
            return ExitFailure;
        }

        if (pdbPath is null)
        {
            Console.Error.WriteLine($"--pdb is required");
            return ExitFailure;
        }

        Console.WriteLine("DLL path: " + dllPath);
        Console.WriteLine("PDB path: " + pdbPath);

        return ExitSuccess;
    }
}
