// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Analyzers.NamespaceSync;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.NamespaceMatchFolder
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpNamespaceMatchFolderDiagnosticAnalyzer : AbstractNamespaceMatchFolderDiagnosticAnalyzer<NamespaceDeclarationSyntax>
    {
        protected override SyntaxNode GetNameSyntax(NamespaceDeclarationSyntax namespaceDeclaration) => namespaceDeclaration.Name;

        protected override ISyntaxFacts GetSyntaxFacts() => CSharpSyntaxFacts.Instance;

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNamespaceNode, SyntaxKind.NamespaceDeclaration);
        }
    }
}
