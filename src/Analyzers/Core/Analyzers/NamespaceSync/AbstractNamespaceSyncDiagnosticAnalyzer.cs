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

        protected abstract ISyntaxFacts GetSyntaxFacts();
        protected abstract bool ContainsPartialTypeWithMultipleDeclarations(TNamespaceSyntax namespaceDeclaration, SemanticModel semanticModel);
        protected abstract SyntaxNode GetNameSyntax(TNamespaceSyntax namespaceDeclaration);

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected void AnalyzeNamespaceNode(SyntaxNodeAnalysisContext context)
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

            if (context.Node is TNamespaceSyntax namespaceDecl)
            {
                var currentNamespace = GetNamespaceName(namespaceDecl, rootNamespace);

                if (IsFileAndNamespaceMismatch(namespaceDecl, rootNamespace, projectDir, out var targetNamespace) &&
                    IsFixSupported(context.SemanticModel, namespaceDecl, context.CancellationToken))
                {
                    var nameSyntax = GetNameSyntax(namespaceDecl);
                    ReportDiagnostics(context, Descriptor, nameSyntax.GetLocation(), targetNamespace, currentNamespace);
                }
            }
        }

        private bool IsFixSupported(SemanticModel semanticModel, TNamespaceSyntax namespaceDeclaration, CancellationToken cancellationToken)
        {
            var root = namespaceDeclaration.SyntaxTree.GetRoot(cancellationToken);

            // It should not be nested in other namespaces
            if (namespaceDeclaration
                .AncestorsAndSelf().OfType<TNamespaceSyntax>()
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

            var expectedNamespace = PathMetadataUtilities.TryBuildNamespaceFromFolders(folders, GetSyntaxFacts(), rootNamespace);

            if (expectedNamespace is null || expectedNamespace.Equals(GetNamespaceName(namespaceDeclaration, rootNamespace)))
            {
                // The namespace currently matches the folder structure
                targetNamespace = null;
                return false;
            }

            targetNamespace = expectedNamespace;
            return true;
        }

        private void ReportDiagnostics(
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

        private string GetNamespaceName(TNamespaceSyntax namespaceSyntax, string rootNamespace)
        {
            var namespaceNameSyntax = GetNameSyntax(namespaceSyntax);
            var syntaxFacts = GetSyntaxFacts();
            return syntaxFacts.GetDisplayName(namespaceNameSyntax, DisplayNameOptions.IncludeNamespaces, rootNamespace);
        }
    }
}
