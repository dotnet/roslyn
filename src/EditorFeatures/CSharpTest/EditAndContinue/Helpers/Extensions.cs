// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.EditAndContinue.UnitTests;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EditAndContinue;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Composition;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    internal static class Extensions
    {
        private static readonly IExportProviderFactory s_exportProviderFactoryWithTestActiveStatementSpanTracker =
            ExportProviderCache.GetOrCreateExportProviderFactory(TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic
                .WithPart(typeof(TestActiveStatementSpanTracker)));

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
            ActiveStatementsDescription? activeStatements = null,
            TargetFramework[]? targetFrameworks = null,
            IEnumerable<string>? additionalOldSources = null,
            IEnumerable<string>? additionalNewSources = null,
            SemanticEditDescription[]? expectedSemanticEdits = null,
            DiagnosticDescription? expectedDeclarationError = null,
            RudeEditDiagnosticDescription[]? expectedDiagnostics = null)
        {
            using var workspace = TestWorkspace.CreateCSharp("", exportProvider: s_exportProviderFactoryWithTestActiveStatementSpanTracker.CreateExportProvider());
            foreach (var targetFramework in targetFrameworks ?? new[] { TargetFramework.NetStandard20, TargetFramework.NetCoreApp30 })
            {
                new CSharpEditAndContinueTestHelpers(targetFramework).VerifySemantics(
                    workspace,
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
