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
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Analyzers.NamespaceSync
{
    internal abstract class AbstractNamespaceSyncDiagnosticAnalyzer<TNamespaceSyntax> : AbstractBuiltInCodeStyleDiagnosticAnalyzer
        where TNamespaceSyntax : SyntaxNode
    {
        public const string RootNamespaceOption = "build_property.RootNamespace";
        public const string ProjectDirOption = "build_property.ProjectDir";

        public AbstractNamespaceSyncDiagnosticAnalyzer(LocalizableResourceString title, LocalizableResourceString message)
            : base(IDEDiagnosticIds.NamespaceSyncAnalyzerDiagnosticId,
                EnforceOnBuild.Recommended,
                (IPerLanguageOption?)null,
                title,
                message)
        {
        }

        /// <summary>
        /// Gets the language specific syntax facts
        /// </summary>
        protected abstract ISyntaxFacts GetSyntaxFacts();

        /// <summary>
        /// Returns true if the namespace declaration contains one or more partial types with multiple declarations.
        /// </summary>
        /// <returns></returns>
        protected abstract bool ContainsPartialTypeWithMultipleDeclarations(TNamespaceSyntax namespaceDeclaration, SemanticModel semanticModel);

        /// <summary>
        /// Gets the syntax representing the name of a namespace declaration
        /// </summary>
        protected abstract SyntaxNode GetNameSyntax(TNamespaceSyntax namespaceDeclaration);

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected void AnalyzeNamespaceNode(SyntaxNodeAnalysisContext context)
        {
            // It's ok to not have a rootnamespace property, but if it's there we want to use it correctly
            if (!context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(RootNamespaceOption, out var rootNamespace))
            {
                rootNamespace = string.Empty;
            }

            // Project directory is a must to correctly get the relative path and construct a namespace
            if (!context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(ProjectDirOption, out var projectDir)
                || string.IsNullOrEmpty(projectDir))
            {
                return;
            }

            if (context.Node is TNamespaceSyntax namespaceDecl)
            {
                var currentNamespace = GetNamespaceName(namespaceDecl, rootNamespace);

                if (IsFileAndNamespaceMismatch(namespaceDecl, rootNamespace, projectDir, out var targetNamespace) &&
                    IsFixSupported(context.SemanticModel, namespaceDecl, context.CancellationToken))
                {
                    var nameSyntax = GetNameSyntax(namespaceDecl);
                    AbstractNamespaceSyncDiagnosticAnalyzer<TNamespaceSyntax>.ReportDiagnostics(context, Descriptor, nameSyntax.GetLocation(), targetNamespace, currentNamespace);
                }
            }
        }

        private bool IsFixSupported(SemanticModel semanticModel, TNamespaceSyntax namespaceDeclaration, CancellationToken cancellationToken)
        {
            var root = namespaceDeclaration.SyntaxTree.GetRoot(cancellationToken);

            // It should not be nested in other namespaces
            if (namespaceDeclaration
                .Ancestors()
                .OfType<TNamespaceSyntax>()
                .Any())
            {
                return false;
            }

            // It should not contain a namespace
            var containsNamespace = namespaceDeclaration
                 .DescendantNodes(n => n is TNamespaceSyntax)
                 .OfType<TNamespaceSyntax>().Any();
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
            var isCurrentNamespaceInvalid = GetNameSyntax(namespaceDeclaration)
                .GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error);
            if (isCurrentNamespaceInvalid)
            {
                return false;
            }

            return true;
        }

        private bool IsFileAndNamespaceMismatch(
            TNamespaceSyntax namespaceDeclaration,
            string rootNamespace,
            string projectDir,
            [NotNullWhen(returnValue: true)] out string? targetNamespace)
        {
            if (!PathUtilities.IsChildPath(projectDir, namespaceDeclaration.SyntaxTree.FilePath))
            {
                // The file does not exist within the project directory
                targetNamespace = null;
                return false;
            }

            var relativeDirectoryPath = PathUtilities.GetRelativePath(
                projectDir,
                PathUtilities.GetDirectoryName(namespaceDeclaration.SyntaxTree.FilePath)!);
            var folders = relativeDirectoryPath.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

            var expectedNamespace = PathMetadataUtilities.TryBuildNamespaceFromFolders(folders, GetSyntaxFacts(), rootNamespace);

            if (expectedNamespace is null || expectedNamespace.Equals(GetNamespaceName(namespaceDeclaration, rootNamespace), StringComparison.OrdinalIgnoreCase))
            {
                // The namespace currently matches the folder structure
                targetNamespace = null;
                return false;
            }

            targetNamespace = expectedNamespace;
            return true;
        }

        private string GetNamespaceName(TNamespaceSyntax namespaceSyntax, string rootNamespace)
        {
            var namespaceNameSyntax = GetNameSyntax(namespaceSyntax);
            var syntaxFacts = GetSyntaxFacts();
            return syntaxFacts.GetDisplayName(namespaceNameSyntax, DisplayNameOptions.None, rootNamespace);
        }

        private static void ReportDiagnostics(
           SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor,
           Location location, string targetNamespace, string originalNamespace)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                descriptor,
                location,
                additionalLocations: null,
                properties: ImmutableDictionary<string, string?>.Empty.Add("TargetNamespace", targetNamespace),
                messageArgs: new[] { originalNamespace, targetNamespace }));
        }
    }
}
