// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.EditAndContinue.UnitTests;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EditAndContinue;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    internal static class Extensions
    {
        internal static void VerifyUnchangedDocument(
            string source,
            ActiveStatementsDescription description)
        {
            CSharpEditAndContinueTestHelpers.Instance.VerifyUnchangedDocument(
                ActiveStatementsDescription.ClearTags(source),
                description.OldStatements,
                description.OldTrackingSpans,
                description.NewSpans,
                description.OldRegions,
                description.NewRegions);
        }

        internal static void VerifyRudeDiagnostics(
            this EditScript<SyntaxNode> editScript,
            params RudeEditDiagnosticDescription[] expectedDiagnostics)
        {
            VerifyRudeDiagnostics(editScript, ActiveStatementsDescription.Empty, expectedDiagnostics);
        }

        internal static void VerifyRudeDiagnostics(
            this EditScript<SyntaxNode> editScript,
            ActiveStatementsDescription description,
            params RudeEditDiagnosticDescription[] expectedDiagnostics)
        {
            CSharpEditAndContinueTestHelpers.Instance.VerifyRudeDiagnostics(
                editScript,
                description,
                expectedDiagnostics);
        }

        internal static void VerifyLineEdits(
            this EditScript<SyntaxNode> editScript,
            IEnumerable<LineChange> expectedLineEdits,
            IEnumerable<string> expectedNodeUpdates,
            params RudeEditDiagnosticDescription[] expectedDiagnostics)
        {
            CSharpEditAndContinueTestHelpers.Instance.VerifyLineEdits(
                editScript,
                expectedLineEdits,
                expectedNodeUpdates,
                expectedDiagnostics);
        }

        internal static void VerifySemanticDiagnostics(
            this EditScript<SyntaxNode> editScript,
            params RudeEditDiagnosticDescription[] expectedDiagnostics)
        {
            VerifySemanticDiagnostics(editScript, null, expectedDiagnostics);
        }

        internal static void VerifySemanticDiagnostics(
            this EditScript<SyntaxNode> editScript,
            DiagnosticDescription expectedDeclarationError,
            params RudeEditDiagnosticDescription[] expectedDiagnostics)
        {
            VerifySemantics(editScript, ActiveStatementsDescription.Empty, null, expectedDeclarationError, expectedDiagnostics);
        }

        internal static void VerifySemantics(
            this EditScript<SyntaxNode> editScript,
            ActiveStatementsDescription activeStatements,
            SemanticEditDescription[] expectedSemanticEdits,
            params RudeEditDiagnosticDescription[] expectedDiagnostics)
        {
            VerifySemantics(editScript, activeStatements, expectedSemanticEdits, null, expectedDiagnostics);
        }

        internal static void VerifySemantics(
            this EditScript<SyntaxNode> editScript,
            ActiveStatementsDescription activeStatements,
            SemanticEditDescription[] expectedSemanticEdits,
            DiagnosticDescription expectedDeclarationError,
            params RudeEditDiagnosticDescription[] expectedDiagnostics)
        {
            VerifySemantics(editScript, activeStatements, null, null, expectedSemanticEdits, expectedDeclarationError, expectedDiagnostics);
        }

        internal static void VerifySemantics(
            this EditScript<SyntaxNode> editScript,
            ActiveStatementsDescription activeStatements,
            IEnumerable<string> additionalOldSources,
            IEnumerable<string> additionalNewSources,
            SemanticEditDescription[] expectedSemanticEdits,
            DiagnosticDescription expectedDeclarationError,
            params RudeEditDiagnosticDescription[] expectedDiagnostics)
        {
            CSharpEditAndContinueTestHelpers.Instance.VerifySemantics(
                editScript,
                activeStatements,
                additionalOldSources,
                additionalNewSources,
                expectedSemanticEdits,
                expectedDeclarationError,
                expectedDiagnostics);
        }
    }
}
