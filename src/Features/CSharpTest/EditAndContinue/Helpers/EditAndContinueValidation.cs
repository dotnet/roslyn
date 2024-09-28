// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.EditAndContinue.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests;

internal static class EditAndContinueValidation
{
    internal static void VerifyLineEdits(
        this EditScriptDescription editScript,
        SourceLineUpdate[] lineEdits,
        SemanticEditDescription[]? semanticEdits = null,
        RudeEditDiagnosticDescription[]? diagnostics = null,
        EditAndContinueCapabilities? capabilities = null)
    {
        Assert.NotEmpty(lineEdits);

        VerifyLineEdits(
            editScript,
            new[] { new SequencePointUpdates(editScript.Match.OldRoot.SyntaxTree.FilePath, [.. lineEdits]) },
            semanticEdits,
            diagnostics,
            capabilities);
    }

    internal static void VerifyLineEdits(
        this EditScriptDescription editScript,
        SequencePointUpdates[] lineEdits,
        SemanticEditDescription[]? semanticEdits = null,
        RudeEditDiagnosticDescription[]? diagnostics = null,
        EditAndContinueCapabilities? capabilities = null)
    {
        new CSharpEditAndContinueTestVerifier().VerifyLineEdits(
            editScript,
            lineEdits,
            semanticEdits,
            diagnostics,
            capabilities);
    }

    internal static void VerifySemanticDiagnostics(
        this EditScriptDescription editScript,
        params RudeEditDiagnosticDescription[] diagnostics)
    {
        VerifySemanticDiagnostics(editScript, activeStatements: null, targetFrameworks: null, capabilities: null, diagnostics);
    }

    internal static void VerifySemanticDiagnostics(
         this EditScriptDescription editScript,
         ActiveStatementsDescription activeStatements,
         params RudeEditDiagnosticDescription[] diagnostics)
    {
        VerifySemanticDiagnostics(editScript, activeStatements, targetFrameworks: null, capabilities: null, diagnostics);
    }

    internal static void VerifySemanticDiagnostics(
        this EditScriptDescription editScript,
        RudeEditDiagnosticDescription[] diagnostics,
        EditAndContinueCapabilities? capabilities)
    {
        VerifySemanticDiagnostics(editScript, activeStatements: null, targetFrameworks: null, capabilities, diagnostics);
    }

    internal static void VerifySemanticDiagnostics(
        this EditScriptDescription editScript,
        ActiveStatementsDescription? activeStatements = null,
        TargetFramework[]? targetFrameworks = null,
        EditAndContinueCapabilities? capabilities = null,
        params RudeEditDiagnosticDescription[] diagnostics)
    {
        VerifySemantics(
            [editScript],
            [new DocumentAnalysisResultsDescription(activeStatements: activeStatements, diagnostics: diagnostics)],
            targetFrameworks,
            capabilities);
    }

    internal static void VerifySemantics(
        this EditScriptDescription editScript,
        ActiveStatementsDescription activeStatements,
        SemanticEditDescription[] semanticEdits,
        EditAndContinueCapabilities? capabilities = null)
    {
        VerifySemantics(
            [editScript],
            [new DocumentAnalysisResultsDescription(activeStatements, semanticEdits: semanticEdits)],
            capabilities: capabilities);
    }

    internal static void VerifySemantics(
        this EditScriptDescription editScript,
        SemanticEditDescription[] semanticEdits,
        EditAndContinueCapabilities capabilities)
    {
        VerifySemantics(editScript, ActiveStatementsDescription.Empty, semanticEdits, capabilities);
    }

    internal static void VerifySemantics(
        this EditScriptDescription editScript,
        params SemanticEditDescription[] semanticEdits)
    {
        VerifySemantics(editScript, ActiveStatementsDescription.Empty, semanticEdits, capabilities: null);
    }

    internal static void VerifySemantics(
        this EditScriptDescription editScript,
        SemanticEditDescription[] semanticEdits,
        RudeEditDiagnosticDescription[] warnings,
        EditAndContinueCapabilities? capabilities = null)
    {
        VerifySemantics(
            [editScript],
            [new DocumentAnalysisResultsDescription(ActiveStatementsDescription.Empty, semanticEdits: semanticEdits, diagnostics: warnings)],
            capabilities: capabilities);
    }

    internal static void VerifySemantics(
        EditScriptDescription[] editScripts,
        DocumentAnalysisResultsDescription[] results,
        TargetFramework[]? targetFrameworks = null,
        EditAndContinueCapabilities? capabilities = null)
    {
        foreach (var targetFramework in targetFrameworks ?? [TargetFramework.NetCoreApp, TargetFramework.NetFramework])
        {
            new CSharpEditAndContinueTestVerifier().VerifySemantics(editScripts, targetFramework, results, capabilities);
        }
    }
}
