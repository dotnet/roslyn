// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// The base interface for types that implement program analyzers that are capable of
    /// producing diagnostics at compile-time.
    /// </summary>
    public interface IDiagnosticAnalyzer
    {
        /// <summary>
        /// Returns a set of descriptors for the diagnostics that this analyzer is capable of producing.
        /// </summary>
        ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
    }
}
