// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

internal static class Program
{
    internal const int ExitFailure = 1;
    internal const int ExitSuccess = 0;

    public static int Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("preparetests <path to binaries> <output path>");
            return ExitFailure;
        }

        MinimizeUtil.Run(args[0], args[1]);
        return ExitSuccess;
    }
}
