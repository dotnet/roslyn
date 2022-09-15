// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.EditAndContinue.Contracts;
using Microsoft.CodeAnalysis.EditAndContinue.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    internal static class EditAndContinueValidation
    {
        internal static void VerifyRudeDiagnostics(
            this EditScript<SyntaxNode> editScript,
            params RudeEditDiagnosticDescription[] expectedDiagnostics)
        {
            VerifySemanticDiagnostics(
               editScript,
               ActiveStatementsDescription.Empty,
               capabilities: null,
               expectedDiagnostics);
        }

        internal static void VerifyRudeDiagnostics(
            this EditScript<SyntaxNode> editScript,
            EditAndContinueCapabilities? capabilities = null,
            params RudeEditDiagnosticDescription[] expectedDiagnostics)
        {
            VerifySemanticDiagnostics(
               editScript,
               ActiveStatementsDescription.Empty,
               capabilities,
               expectedDiagnostics);
        }

        internal static void VerifyRudeDiagnostics(
            this EditScript<SyntaxNode> editScript,
            ActiveStatementsDescription description,
            params RudeEditDiagnosticDescription[] expectedDiagnostics)
        {
            VerifySemanticDiagnostics(
                editScript,
                description,
                capabilities: null,
                expectedDiagnostics);
        }

        internal static void VerifyLineEdits(
            this EditScript<SyntaxNode> editScript,
            SourceLineUpdate[] lineEdits,
            SemanticEditDescription[]? semanticEdits = null,
            RudeEditDiagnosticDescription[]? diagnostics = null)
        {
            Assert.NotEmpty(lineEdits);

            VerifyLineEdits(
                editScript,
                new[] { new SequencePointUpdates(editScript.Match.OldRoot.SyntaxTree.FilePath, lineEdits.ToImmutableArray()) },
                semanticEdits,
                diagnostics);
        }

        internal static void VerifyLineEdits(
            this EditScript<SyntaxNode> editScript,
            SequencePointUpdates[] lineEdits,
            SemanticEditDescription[]? semanticEdits = null,
            RudeEditDiagnosticDescription[]? diagnostics = null)
        {
            new CSharpEditAndContinueTestHelpers().VerifyLineEdits(
                editScript,
                lineEdits,
                semanticEdits,
                diagnostics);
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
            VerifySemanticDiagnostics(editScript, activeStatements, capabilities: null, expectedDiagnostics);
        }

        internal static void VerifySemanticDiagnostics(
            this EditScript<SyntaxNode> editScript,
            ActiveStatementsDescription activeStatements,
            EditAndContinueCapabilities? capabilities = null,
            params RudeEditDiagnosticDescription[] expectedDiagnostics)
        {
            VerifySemantics(
                new[] { editScript },
                new[] { new DocumentAnalysisResultsDescription(activeStatements: activeStatements, diagnostics: expectedDiagnostics) },
                capabilities: capabilities);
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
            SemanticEditDescription[] expectedSemanticEdits,
            EditAndContinueCapabilities? capabilities = null)
        {
            VerifySemantics(
                new[] { editScript },
                new[] { new DocumentAnalysisResultsDescription(activeStatements, semanticEdits: expectedSemanticEdits) },
                capabilities: capabilities);
        }

        internal static void VerifySemantics(
            this EditScript<SyntaxNode> editScript,
            params SemanticEditDescription[] expectedSemanticEdits)
        {
            VerifySemantics(editScript, ActiveStatementsDescription.Empty, expectedSemanticEdits, capabilities: null);
        }

        internal static void VerifySemantics(
            EditScript<SyntaxNode>[] editScripts,
            DocumentAnalysisResultsDescription[] expected,
            TargetFramework[]? targetFrameworks = null,
            EditAndContinueCapabilities? capabilities = null)
        {
            foreach (var targetFramework in targetFrameworks ?? new[] { TargetFramework.NetStandard20, TargetFramework.NetCoreApp })
            {
                new CSharpEditAndContinueTestHelpers().VerifySemantics(editScripts, targetFramework, expected, capabilities);
            }
        }
    }
}
