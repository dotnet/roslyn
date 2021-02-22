﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    internal readonly struct DocumentAnalysisResultsDescription
    {
        public readonly ActiveStatementsDescription ActiveStatements;

        /// <summary>
        /// Default if semantic edits are not validated by the test.
        /// </summary>
        public readonly ImmutableArray<SemanticEditDescription> SemanticEdits;

        public readonly ImmutableArray<RudeEditDiagnosticDescription> Diagnostics;

        public DocumentAnalysisResultsDescription(
            ActiveStatementsDescription? activeStatements = null,
            SemanticEditDescription[]? semanticEdits = null,
            RudeEditDiagnosticDescription[]? diagnostics = null)
        {
            // The test must validate semantic edits, diagnostics or both.
            // If neither is specified then assume the expectation is that
            // the documents has no edits and no diagnostics.
            if (semanticEdits is null && diagnostics is null)
            {
                SemanticEdits = ImmutableArray<SemanticEditDescription>.Empty;
                Diagnostics = ImmutableArray<RudeEditDiagnosticDescription>.Empty;
            }
            else
            {
                SemanticEdits = semanticEdits.AsImmutableOrNull();
                Diagnostics = diagnostics.AsImmutableOrEmpty();
            }

            ActiveStatements = activeStatements ?? ActiveStatementsDescription.Empty;
        }
    }
}
