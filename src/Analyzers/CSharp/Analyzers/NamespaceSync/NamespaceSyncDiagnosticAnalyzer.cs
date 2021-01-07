// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.NamespaceFileSync
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class NamespaceSyncDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        private static readonly LocalizableResourceString s_localizableTitle = new LocalizableResourceString(
          nameof(CSharpAnalyzersResources.Namespace_does_not_match_folder_structure), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources));

        private static readonly LocalizableResourceString s_localizableInsideMessage = new LocalizableResourceString(
            nameof(CSharpAnalyzersResources.Namespace_0_does_not_match_folder_structure_expected_1), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources));

        public const string RootNamespaceOption = "build_property.RootNamespace";
        public const string ProjectDirOption = "build_property.ProjectDir";

        public NamespaceSyncDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.NamespaceSyncAnalyzerDiagnosticId,
                EnforceOnBuild.Recommended,
                (IPerLanguageOption?)null,
                s_localizableTitle,
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
            if (!context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(RootNamespaceOption, out var rootNamespace)
                || rootNamespace is null)
            {
                return;
            }

            if (!context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(ProjectDirOption, out var projectDir)
                || string.IsNullOrEmpty(projectDir))
            {
                return;
            }

            if (context.Node is NamespaceDeclarationSyntax namespaceDecl &&
                IsFileAndNamespaceMismatch(namespaceDecl, rootNamespace, projectDir, out var targetNamespace) &&
                IsFixSupported(context.SemanticModel, namespaceDecl, context.CancellationToken))
            {
                ReportDiagnostics(context, this.Descriptor, namespaceDecl, targetNamespace);
            }
        }

        private static bool IsFixSupported(SemanticModel semanticModel, NamespaceDeclarationSyntax namespaceDeclaration, CancellationToken cancellationToken)
        {
            var root = namespaceDeclaration.SyntaxTree.GetRoot(cancellationToken);

            // It should not be nested in other namespaces
            if (namespaceDeclaration
                .AncestorsAndSelf().OfType<NamespaceDeclarationSyntax>()
                .Any())
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

            // It should not contain partial classes with more than one instance in the semantic model. The
            // fixer does not support this scenario.
            var containsPartialType = ContainsPartialTypeWithMultipleDeclarations(namespaceDeclaration, semanticModel);
            if (containsPartialType)
            {
                return false;
            }

            // The current namespace should be valid
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

        private static bool IsFileAndNamespaceMismatch(
            NamespaceDeclarationSyntax namespaceDeclaration,
            string rootNamespace,
            string projectDir,
            [NotNullWhen(returnValue: true)] out string? targetNamespace)
        {
            var filePath = Path.Combine(namespaceDeclaration.SyntaxTree.FilePath, ".");
            if (!filePath.StartsWith(projectDir))
            {
                // The file does not exist within the project directory
                targetNamespace = null;
                return false;
            }

            var relativeFilePath = filePath.Substring(projectDir.Length);
            var folders = Path
                .ChangeExtension(relativeFilePath, null)
                .Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

            var expectedNamespace = PathMetadataUtilities.TryBuildNamespaceFromFolders(folders, CSharpSyntaxFacts.Instance, rootNamespace);

            if (expectedNamespace is null || expectedNamespace.Equals(namespaceDeclaration.Name.ToString()))
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
           NamespaceDeclarationSyntax namespaceDeclaration, string targetNamespace)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                descriptor,
                namespaceDeclaration.Name.GetLocation(),
                additionalLocations: null,
                properties: ImmutableDictionary<string, string?>.Empty.Add("TargetNamespace", targetNamespace),
                messageArgs: new[] { namespaceDeclaration.Name.ToString(), targetNamespace }));
        }
    }
}
