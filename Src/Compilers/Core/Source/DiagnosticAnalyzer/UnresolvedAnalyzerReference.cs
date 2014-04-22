// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Represents an analyzer reference that can't be resolved.
    /// </summary>
    /// <remarks>
    /// For error reporting only, can't be used to reference an analyzer assembly.
    /// </remarks>
    public sealed class UnresolvedAnalyzerReference : AnalyzerReference
    {
        private readonly string unresolvedPath;

        public UnresolvedAnalyzerReference(string unresolvedPath)
        {
            this.unresolvedPath = unresolvedPath;
        }

        public override string Display
        {
            get
            {
                return CodeAnalysisResources.Unresolved + FullPath;
            }
        }

        public override string FullPath
        {
            get
            {
                return unresolvedPath;
            }
        }

        public override bool IsUnresolved
        {
            get { return true; }
        }

        public override ImmutableArray<IDiagnosticAnalyzer> GetAnalyzers()
        {
            return ImmutableArray<IDiagnosticAnalyzer>.Empty;
        }
    }
}
