// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Diagnostics;

[Flags]
internal enum AnalyzerFilter
{
    /// <summary>
    /// The default 'compiler analyzer' which reports the standard set of compiler diagnostics.
    /// </summary>
    CompilerAnalyzer = 1,

    /// <summary>
    /// Any other analyzer that is not the default 'compiler analyzer'.
    /// </summary>
    NonCompilerAnalyzer = 2,

    /// <summary>
    /// Include both compiler and non-compiler analyzers.
    /// </summary>
    All = CompilerAnalyzer | NonCompilerAnalyzer,
}
