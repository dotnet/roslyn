// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.StackTraceExplorer;

/// <summary>
/// Determines what type of symbol to look for
/// when mapping a <see cref="ParsedStackFrame"/> to
/// a location.
/// </summary>
internal enum StackFrameSymbolPart
{
    /// <summary>
    /// The intended lookup is to find a method symbol associated 
    /// with a <see cref="ParsedStackFrame"/>
    /// </summary>
    Method,

    /// <summary>
    /// The intended lookup is to find a type symbol associated
    /// with a <see cref="ParsedStackFrame"/>
    /// </summary>
    ContainingType
}
