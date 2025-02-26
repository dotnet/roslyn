// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Analyzers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CSharpUpgradeMSBuildWorkspaceAnalyzer : UpgradeMSBuildWorkspaceAnalyzer
    {
        protected override void RegisterIdentifierAnalysis(CompilationStartAnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeIdentifier, SyntaxKind.IdentifierName);
        }

        private void AnalyzeIdentifier(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is IdentifierNameSyntax identifierName &&
                identifierName.Identifier.ToString() == MSBuildWorkspace)
            {
                var symbolInfo = context.SemanticModel.GetSymbolInfo(identifierName, context.CancellationToken);
                if (symbolInfo.Symbol == null)
                {
                    context.ReportDiagnostic(identifierName.CreateDiagnostic(UpgradeMSBuildWorkspaceDiagnosticRule));
                }
            }
        }
    }
}
