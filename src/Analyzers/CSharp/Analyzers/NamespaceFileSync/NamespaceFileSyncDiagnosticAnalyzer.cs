// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.NamespaceFileSync
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class NamespaceFileSyncDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        //TODO: replace with real name
        private static readonly LocalizableResourceString s_localizableTitle = new LocalizableResourceString(
          nameof(CSharpAnalyzersResources.Misplaced_using_directive), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources));

        private static readonly LocalizableResourceString s_localizableOutsideMessage = new LocalizableResourceString(
            nameof(CSharpAnalyzersResources.Using_directives_must_be_placed_outside_of_a_namespace_declaration), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources));

        private static readonly DiagnosticDescriptor s_outsideDiagnosticDescriptor = CreateDescriptorWithId(
            IDEDiagnosticIds.MoveMisplacedUsingDirectivesDiagnosticId, s_localizableTitle, s_localizableOutsideMessage);

        private static readonly LocalizableResourceString s_localizableInsideMessage = new LocalizableResourceString(
            nameof(CSharpAnalyzersResources.Using_directives_must_be_placed_inside_of_a_namespace_declaration), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources));

        private static readonly DiagnosticDescriptor s_insideDiagnosticDescriptor = CreateDescriptorWithId(
            IDEDiagnosticIds.MoveMisplacedUsingDirectivesDiagnosticId, s_localizableTitle, s_localizableInsideMessage);

        //TODO: replace with your name
        public NamespaceFileSyncDiagnosticAnalyzer()
            : base(ImmutableDictionary<DiagnosticDescriptor, ILanguageSpecificOption>.Empty
                    .Add(s_outsideDiagnosticDescriptor, CSharpCodeStyleOptions.PreferredUsingDirectivePlacement)
                    .Add(s_insideDiagnosticDescriptor, CSharpCodeStyleOptions.PreferredUsingDirectivePlacement),
                 LanguageNames.CSharp)
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() 
            => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNamespaceNode, SyntaxKind.NamespaceDeclaration);
        }

        private void AnalyzeNamespaceNode(SyntaxNodeAnalysisContext context)
        {
            //TODO: add <CompilerVisibleAttribute> to project file to request for RootNamespace property
            //context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue("build_propert.rootNamespace", out string rootNamespace);

            var rootnamespace = "";
            var projectdir = ""; //to determine files sthat don't exist under this project

            // TODo Report diagnostic if :
            // - Not a linked file
            // - rootnamespace isn't the containing namespace of current declared namespace.
            // - A valid namespace can be constructed from folder hierarchy

            var namespaceDecl = context.Node as NamespaceDeclarationSyntax;
            if (namespaceDecl == null ||
                IsFileAndNamespaceValid(namespaceDecl, rootnamespace, projectdir) ||
                !IsNamespaceSyncSupported(namespaceDecl))
            {
                return;
            }

            ReportDiagnostics(context, s_insideDiagnosticDescriptor, Enumerable.Empty<NamespaceDeclarationSyntax>(), ReportDiagnostic.Warn);
        }

        private static bool IsNamespaceSyncSupported(NamespaceDeclarationSyntax namespaceDeclaration)
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
            // TODO: is this check necessary?
            var containsNamespace = namespaceDeclaration
                 .DescendantNodes(n => n is NamespaceDeclarationSyntax)
                 .OfType<NamespaceDeclarationSyntax>().Any();
            if (containsNamespace)
            {
                return false;
            }

            // It should not contain partial classes
            // TODO: should we check whether this is only one instance of a partial class?
            // If so, we would need to switch to pulling in the semantic model.
            var containsPartialType = namespaceDeclaration
                .Members.OfType<TypeDeclarationSyntax>()
                .Where(t => t.Modifiers.Any(SyntaxKind.PartialKeyword)).Any();
            if (containsPartialType)
            {
                return false;
            }

            // The current namespace should be valid
            var isCurrentNamespaceInvalid = namespaceDeclaration.Name.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error);
            if (isCurrentNamespaceInvalid)
            {
                return false;
            }

            return true;
        }

        private static bool IsFileAndNamespaceValid(NamespaceDeclarationSyntax namespaceDeclaration, string rootNamespace, string projectDir)
        {
            var filePath = namespaceDeclaration.SyntaxTree.FilePath;
            if (!filePath.Contains(projectDir))
            {
                // The file does not exist withing the project directory
                return false;
            }

            var relativeFilePath = filePath.Substring(projectDir.Length);
            var namespaceElements = relativeFilePath.Split(Path.DirectorySeparatorChar).Where(e => e.Length > 0);
            var expectedNamespace = $"{rootNamespace}.{string.Join(".", namespaceElements)}";

            if (expectedNamespace.Equals(namespaceDeclaration.Name.ToString()))
            {
                // The namespace is already correct
                return false;
            }

            return true;
        }

        private static void ReportDiagnostics(
           SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor,
           IEnumerable<NamespaceDeclarationSyntax> namespaceDeclarations, ReportDiagnostic reportDiagnostic)
        {
            foreach (var namespaceDeclaration in namespaceDeclarations)
            {
                context.ReportDiagnostic(DiagnosticHelper.Create(
                    descriptor,
                    namespaceDeclaration.GetLocation(),
                    reportDiagnostic,
                    additionalLocations: null,
                    properties: null));
            }
        }
    }
}
