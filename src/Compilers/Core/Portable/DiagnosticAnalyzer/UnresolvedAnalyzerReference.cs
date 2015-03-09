﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public UnresolvedAnalyzerReference(string unresolvedPath)
        {
            FullPath = unresolvedPath;
        }

        public override string Display
        {
            get
            {
                return CodeAnalysisResources.Unresolved + FullPath;
            }
        }

        public override string FullPath { get; }

        public override bool IsUnresolved
        {
            get { return true; }
        }

        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages()
        {
            return ImmutableArray<DiagnosticAnalyzer>.Empty;
        }

        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language)
        {
            return ImmutableArray<DiagnosticAnalyzer>.Empty;
        }
    }
}
