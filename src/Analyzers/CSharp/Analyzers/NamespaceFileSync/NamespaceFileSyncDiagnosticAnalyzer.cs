// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.NamespaceFileSync
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class NamespaceFileSyncDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        private static readonly LocalizableResourceString s_localizableTitle = new LocalizableResourceString(
          nameof(CSharpAnalyzersResources.Namespace_named_incorrectly), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources));

        private static readonly LocalizableResourceString s_localizableInsideMessage = new LocalizableResourceString(
            nameof(CSharpAnalyzersResources.Namespace_must_match_folder_structure), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources));

        public const string RootNamespaceOption = "build_property.RootNamespace";
        public const string ProjectDirOption = "build_property.ProjectDir";

        public NamespaceFileSyncDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.NamespaceSyncAnalyzerDiagnosticId,
                   CSharpCodeStyleOptions.PreferNamespaceMatchFolderStructure,
                   LanguageNames.CSharp,
                   s_localizableInsideMessage)
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNamespaceNode, SyntaxKind.NamespaceDeclaration);
        }

        private void AnalyzeNamespaceNode(SyntaxNodeAnalysisContext context)
        {
            //TODO: is this needed in Code_Style? if so, GlobalOptions support needs to be added in the package.
            if (!context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(RootNamespaceOption, out var rootNamespace)
                || rootNamespace is null)
            {
                return;
            }


            if (!context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(ProjectDirOption, out var projectDir)
                || projectDir is null or { Length: 0 })
            {
                return;
            }

            if (context.Node is NamespaceDeclarationSyntax namespaceDecl &&
                IsFileAndNamespaceValid(namespaceDecl, rootNamespace, projectDir, out var targetNamespace) &&
                IsNamespaceSyncSupported(namespaceDecl, context.SemanticModel))
            {
                ReportDiagnostics(context, this.Descriptor, namespaceDecl, ReportDiagnostic.Warn, targetNamespace);
            }
        }

        private static bool IsNamespaceSyncSupported(NamespaceDeclarationSyntax namespaceDeclaration, SemanticModel semanticModel)
        {
            var root = namespaceDeclaration.SyntaxTree.GetRoot();

            // It should not be nested in other namespaces
            var namespaceDeclCount = namespaceDeclaration
                .AncestorsAndSelf().OfType<NamespaceDeclarationSyntax>()
                .Count();
            if (namespaceDeclCount > 1)
            {
                return false;
            }

            // It should not contain a namespace
            var containsNamespace = namespaceDeclaration
                 .DescendantNodes(n => n is NamespaceDeclarationSyntax)
                 .OfType<NamespaceDeclarationSyntax>().Any();
            if (containsNamespace)
            {
                return false;
            }

            // It should not contain partial classes with more than one instance in the semantic model
            var containsPartialType = ContainsPartialTypeWithMultipleDeclarations(namespaceDeclaration, semanticModel);
            if (containsPartialType)
            {
                return false;
            }

            // The current namespace should be valid
            // TODO : can this cause a loop? This condition was used by the service before,
            // but now could possibly add an Error severity as an analyzer
            var isCurrentNamespaceInvalid = namespaceDeclaration.Name
                .GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error);
            if (isCurrentNamespaceInvalid)
            {
                return false;
            }

            return true;
        }

        private static bool ContainsPartialTypeWithMultipleDeclarations(NamespaceDeclarationSyntax namespaceDeclaration, SemanticModel semanticModel)
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

        private static bool IsFileAndNamespaceValid(
            NamespaceDeclarationSyntax namespaceDeclaration,
            string rootNamespace,
            string projectDir,
            out string? targetNamespace)
        {
            var filePath = namespaceDeclaration.SyntaxTree.FilePath;
            if (!filePath.Contains(projectDir))
            {
                // The file does not exist withing the project directory
                targetNamespace = null;
                return false;
            }

            var relativeFilePath = filePath.Substring(projectDir.Length);
            var namespaceElements = Path.ChangeExtension(relativeFilePath, null).Split(Path.DirectorySeparatorChar).Where(e => e.Length > 0);
            var expectedNamespace = $"{rootNamespace}.{string.Join(".", namespaceElements)}";

            if (expectedNamespace.Equals(namespaceDeclaration.Name.ToString()))
            {
                // The namespace currently matches the folder structure
                targetNamespace = null;
                return false;
            }

            targetNamespace = expectedNamespace;
            return true;
        }

        private static void ReportDiagnostics(
           SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor,
           NamespaceDeclarationSyntax namespaceDeclaration, ReportDiagnostic reportDiagnostic,
           string targetNamespace)
        {
            var properties = new Dictionary<string, string>()
                { { "TargetNamespace", targetNamespace } }
                .ToImmutableDictionary();

            context.ReportDiagnostic(DiagnosticHelper.Create(
                descriptor,
                namespaceDeclaration.Name.GetLocation(),
                reportDiagnostic,
                additionalLocations: null,
                properties: properties));
        }
    }
}
