// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Analyzers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CSharpUpgradeMSBuildWorkspaceAnalyzer : UpgradeMSBuildWorkspaceAnalyzer
    {
        private CSharpUpgradeMSBuildWorkspaceAnalyzer(bool performAssemblyChecks)
            : base(performAssemblyChecks)
        {
        }

        public CSharpUpgradeMSBuildWorkspaceAnalyzer()
            : this(performAssemblyChecks: true)
        {
        }

        internal static CSharpUpgradeMSBuildWorkspaceAnalyzer CreateForTests()
            => new(performAssemblyChecks: false);

        protected override void RegisterIdentifierAnalysis(CompilationStartAnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeIdentifier, SyntaxKind.IdentifierName);
        }

        protected override void RegisterIdentifierAnalysis(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeIdentifier, SyntaxKind.IdentifierName);
        }

        private void AnalyzeIdentifier(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is IdentifierNameSyntax identifierName &&
                identifierName.Identifier.ToString() == MSBuildWorkspace)
            {
                var symbolInfo = context.SemanticModel.GetSymbolInfo(identifierName);
                if (symbolInfo.Symbol == null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(UpgradeMSBuildWorkspaceDiagnosticRule, identifierName.GetLocation()));
                }
            }
        }
    }
}
