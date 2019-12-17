// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Lsif.Generator
{
    internal static class Utilities
    {
        public static string ToDisplayString(this Stopwatch stopwatch)
        {
            return stopwatch.Elapsed.TotalSeconds.ToString("N2") + " second(s)";
        }
    }
}
