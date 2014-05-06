// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Represents an analyzer assembly reference that contains diagnostic analyzers.
    /// </summary>
    /// <remarks>
    /// Represents a logical location of the analyzer reference, not the content of the reference. 
    /// The content might change in time. A snapshot is taken when the compiler queries the reference for its analyzers.
    /// </remarks>
    public abstract class AnalyzerReference
    {
        internal AnalyzerReference()
        {
        }

        /// <summary>
        /// Full path describing the location of the analyzer reference, or null if the reference has no location.
        /// </summary>
        public abstract string FullPath { get; }

        /// <summary>
        /// Path or name used in error messages to identity the reference.
        /// </summary>
        public virtual string Display
        {
            get { return null; }
        }

        /// <summary>
        /// Returns true if this reference is an unresolved reference.
        /// </summary>
        public virtual bool IsUnresolved
        {
            get { return false; }
        }

        public abstract ImmutableArray<IDiagnosticAnalyzer> GetAnalyzers();
    }
}