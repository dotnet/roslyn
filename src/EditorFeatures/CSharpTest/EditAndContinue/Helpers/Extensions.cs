// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.EditAndContinue.UnitTests;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EditAndContinue;
using Microsoft.CodeAnalysis.SyntaxDifferencing;

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
                description.OldSpans,
                description.OldTrackingSpans,
                description.NewSpans,
                description.OldRegions,
                description.NewRegions);
        }

        internal static void VerifyRudeDiagnostics(
            this SyntaxEditScript editScript,
            params RudeEditDiagnosticDescription[] expectedDiagnostics)
        {
            VerifyRudeDiagnostics(editScript, ActiveStatementsDescription.Empty, expectedDiagnostics);
        }

        internal static void VerifyRudeDiagnostics(
            this SyntaxEditScript editScript,
            ActiveStatementsDescription description,
            params RudeEditDiagnosticDescription[] expectedDiagnostics)
        {
            CSharpEditAndContinueTestHelpers.Instance.VerifyRudeDiagnostics(
                editScript,
                description,
                expectedDiagnostics);
        }

        internal static void VerifyLineEdits(
            this SyntaxEditScript editScript,
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
            this SyntaxEditScript editScript,
            params RudeEditDiagnosticDescription[] expectedDiagnostics)
        {
            VerifySemantics(editScript, ActiveStatementsDescription.Empty, null, expectedDiagnostics);
        }

        internal static void VerifySemantics(
            this SyntaxEditScript editScript,
            ActiveStatementsDescription activeStatements,
            SemanticEditDescription[] expectedSemanticEdits,
            params RudeEditDiagnosticDescription[] expectedDiagnostics)
        {
            VerifySemantics(editScript, activeStatements, null, null, expectedSemanticEdits, expectedDiagnostics);
        }

        internal static void VerifySemantics(
            this SyntaxEditScript editScript,
            ActiveStatementsDescription activeStatements,
            IEnumerable<string> additionalOldSources,
            IEnumerable<string> additionalNewSources,
            SemanticEditDescription[] expectedSemanticEdits,
            params RudeEditDiagnosticDescription[] expectedDiagnostics)
        {
            CSharpEditAndContinueTestHelpers.Instance.VerifySemantics(
                editScript,
                activeStatements,
                additionalOldSources,
                additionalNewSources,
                expectedSemanticEdits,
                expectedDiagnostics);
        }
    }
}
