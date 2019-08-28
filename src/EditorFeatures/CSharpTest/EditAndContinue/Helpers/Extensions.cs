// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.EditAndContinue.UnitTests;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EditAndContinue;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;

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
            VerifySemantics(
                editScript,
                expectedDiagnostics: expectedDiagnostics);
        }

        internal static void VerifySemanticDiagnostics(
            this EditScript<SyntaxNode> editScript,
            TargetFramework[] targetFrameworks,
            params RudeEditDiagnosticDescription[] expectedDiagnostics)
        {
            VerifySemantics(
                editScript,
                targetFrameworks: targetFrameworks,
                expectedDiagnostics: expectedDiagnostics);
        }

        internal static void VerifySemanticDiagnostics(
            this EditScript<SyntaxNode> editScript,
            DiagnosticDescription expectedDeclarationError,
            params RudeEditDiagnosticDescription[] expectedDiagnostics)
        {
            VerifySemantics(
                editScript,
                expectedDeclarationError: expectedDeclarationError,
                expectedDiagnostics: expectedDiagnostics);
        }

        internal static void VerifySemantics(
            this EditScript<SyntaxNode> editScript,
            ActiveStatementsDescription activeStatements,
            SemanticEditDescription[] expectedSemanticEdits)
        {
            VerifySemantics(
                editScript,
                activeStatements,
                expectedSemanticEdits: expectedSemanticEdits,
                expectedDiagnostics: null);
        }

        internal static void VerifySemantics(
            this EditScript<SyntaxNode> editScript,
            ActiveStatementsDescription activeStatements = null,
            TargetFramework[] targetFrameworks = null,
            IEnumerable<string> additionalOldSources = null,
            IEnumerable<string> additionalNewSources = null,
            SemanticEditDescription[] expectedSemanticEdits = null,
            DiagnosticDescription expectedDeclarationError = null,
            RudeEditDiagnosticDescription[] expectedDiagnostics = null)
        {
            foreach (var targetFramework in targetFrameworks ?? new[] { TargetFramework.NetStandard20, TargetFramework.NetCoreApp30 })
            {
                new CSharpEditAndContinueTestHelpers(targetFramework).VerifySemantics(
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
}
