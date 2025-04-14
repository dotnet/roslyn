// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET9_0_OR_GREATER

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.CSharp.FileBasedPrograms;

/// <summary>
/// Represents an error that should be reported to the user as a compiler diagnostic.
/// </summary>
[Experimental(RoslynExperiments.VirtualProjectGenerator, UrlFormat = RoslynExperiments.VirtualProjectGenerator_Url)]
public sealed class DiagnosticException : Exception
{
    internal DiagnosticException(string message) : base(message) { }

    internal DiagnosticException(string format, params ReadOnlySpan<object?> args)
        : this(string.Format(format, args)) { }
}

#endif
