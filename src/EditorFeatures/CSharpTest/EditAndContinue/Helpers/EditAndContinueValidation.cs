// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.EditAndContinue.UnitTests;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EditAndContinue;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    internal static class EditAndContinueValidation
    {
        internal static void VerifyUnchangedDocument(
            string source,
            ActiveStatementsDescription description)
        {
            new CSharpEditAndContinueTestHelpers().VerifyUnchangedDocument(
                ActiveStatementsDescription.ClearTags(source),
                description.OldStatements,
                description.NewSpans,
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
            VerifySemanticDiagnostics(
                editScript,
                description,
                expectedDiagnostics);
        }

        internal static void VerifyLineEdits(
            this EditScript<SyntaxNode> editScript,
            IEnumerable<SourceLineUpdate> expectedLineEdits,
            IEnumerable<string> expectedNodeUpdates,
            params RudeEditDiagnosticDescription[] expectedDiagnostics)
        {
            new CSharpEditAndContinueTestHelpers().VerifyLineEdits(
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
                new[] { editScript },
                new[] { new DocumentAnalysisResultsDescription(diagnostics: expectedDiagnostics) });
        }

        internal static void VerifySemanticDiagnostics(
            this EditScript<SyntaxNode> editScript,
            ActiveStatementsDescription activeStatements,
            params RudeEditDiagnosticDescription[] expectedDiagnostics)
        {
            VerifySemantics(
                new[] { editScript },
                new[] { new DocumentAnalysisResultsDescription(activeStatements: activeStatements, diagnostics: expectedDiagnostics) });
        }

        internal static void VerifySemanticDiagnostics(
            this EditScript<SyntaxNode> editScript,
            TargetFramework[] targetFrameworks,
            params RudeEditDiagnosticDescription[] expectedDiagnostics)
        {
            VerifySemantics(
                new[] { editScript },
                new[] { new DocumentAnalysisResultsDescription(diagnostics: expectedDiagnostics) },
                targetFrameworks: targetFrameworks);
        }

        internal static void VerifySemantics(
            this EditScript<SyntaxNode> editScript,
            ActiveStatementsDescription activeStatements,
            SemanticEditDescription[] expectedSemanticEdits)
        {
            VerifySemantics(
                new[] { editScript },
                new[] { new DocumentAnalysisResultsDescription(activeStatements, semanticEdits: expectedSemanticEdits) });
        }

        internal static void VerifySemantics(
            this EditScript<SyntaxNode> editScript,
            params SemanticEditDescription[] expectedSemanticEdits)
        {
            VerifySemantics(editScript, ActiveStatementsDescription.Empty, expectedSemanticEdits);
        }

        internal static void VerifySemantics(
            EditScript<SyntaxNode>[] editScripts,
            DocumentAnalysisResultsDescription[] expected,
            TargetFramework[]? targetFrameworks = null)
        {
            foreach (var targetFramework in targetFrameworks ?? new[] { TargetFramework.NetStandard20, TargetFramework.NetCoreApp })
            {
                new CSharpEditAndContinueTestHelpers().VerifySemantics(editScripts, targetFramework, expected);
            }
        }
    }
}
