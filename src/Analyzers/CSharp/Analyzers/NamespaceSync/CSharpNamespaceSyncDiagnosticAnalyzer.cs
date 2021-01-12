// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.Analyzers.NamespaceSync;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.NamespaceSync
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpNamespaceSyncDiagnosticAnalyzer : AbstractNamespaceSyncDiagnosticAnalyzer<NamespaceDeclarationSyntax>
    {
        private static readonly LocalizableResourceString s_localizableTitle = new LocalizableResourceString(
          nameof(CSharpAnalyzersResources.Namespace_does_not_match_folder_structure), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources));

        private static readonly LocalizableResourceString s_localizableInsideMessage = new LocalizableResourceString(
            nameof(CSharpAnalyzersResources.Namespace_0_does_not_match_folder_structure_expected_1), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources));

        public CSharpNamespaceSyncDiagnosticAnalyzer()
            : base(s_localizableTitle,
                s_localizableInsideMessage)
        {
        }

        protected override ISyntaxFacts GetSyntaxFacts() => CSharpSyntaxFacts.Instance;

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNamespaceNode, SyntaxKind.NamespaceDeclaration);
        }

        protected override bool ContainsPartialTypeWithMultipleDeclarations(NamespaceDeclarationSyntax namespaceDeclaration, SemanticModel semanticModel)
        {
            var partialMemberDecls = namespaceDeclaration
                .Members.OfType<TypeDeclarationSyntax>()
                .Where(t => t.Modifiers.Any(SyntaxKind.PartialKeyword));

            foreach (var memberDecl in partialMemberDecls)
            {
                var memberSymbol = semanticModel.GetDeclaredSymbol(memberDecl);

                // Simplify the check by assuming no multiple partial declarations in one document
                if (memberSymbol is ITypeSymbol typeSymbol && typeSymbol.DeclaringSyntaxReferences.Length > 1)
                {
                    return true;
                }
            }

            return false;
        }

        protected override SyntaxNode GetNameSyntax(NamespaceDeclarationSyntax namespaceDeclaration) => namespaceDeclaration.Name;
    }
}
