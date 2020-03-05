// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
        private readonly string _unresolvedPath;

        public UnresolvedAnalyzerReference(string unresolvedPath)
        {
            if (unresolvedPath == null)
            {
                throw new ArgumentNullException(nameof(unresolvedPath));
            }

            _unresolvedPath = unresolvedPath;
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
                return _unresolvedPath;
            }
        }

        public override object Id
        {
            get
            {
                return _unresolvedPath;
            }
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
