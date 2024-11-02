// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
#if !METALAMA_COMPILER_INTERFACE
#endif

#pragma warning disable CS8618
// ReSharper disable UnassignedGetOnlyAutoProperty

namespace Metalama.Compiler;

/// <summary>
///     Represents a mechanism able to suppress diagnostics with a delegate.
/// </summary>
public readonly struct DiagnosticFilter
{
    public DiagnosticFilter(SuppressionDescriptor descriptor, string filePath, DiagnosticFilterDelegate filter)
    {
        Descriptor = descriptor;
        FilePath = filePath;
        Filter = filter;
    }

    public string FilePath { get; }

    public SuppressionDescriptor Descriptor { get; }

    public DiagnosticFilterDelegate Filter { get; }
}

public delegate bool DiagnosticFilterDelegate(in DiagnosticFilteringRequest request);
