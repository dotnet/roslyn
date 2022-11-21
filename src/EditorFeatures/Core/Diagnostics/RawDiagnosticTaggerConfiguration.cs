// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal enum RawDiagnosticTaggerConfiguration
    {
        Compiler = 1 << 0,
        Analyzer = 1 << 1,
        Syntax = 1 << 2,
        Semantic = 1 << 3,
    }
}
