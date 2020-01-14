// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// Disable nullable in legacy test framework
#nullable disable

using System.Linq;

namespace Test.Utilities
{
    public static class DiagnosticAnalyzerTestsExtensions
    {
        public static FileAndSource[] ToFileAndSource(this string[] sources)
        {
            return sources.Select(s => new FileAndSource() { FilePath = null, Source = s }).ToArray();
        }
    }
}
