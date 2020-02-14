﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    internal sealed class CompilerAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        public static CompilerAnalyzerConfigOptions Empty { get; } = new CompilerAnalyzerConfigOptions();

        private CompilerAnalyzerConfigOptions()
        {
        }

        public override bool TryGetValue(string key, out string value)
        {
            value = null;
            return false;
        }
    }
}
