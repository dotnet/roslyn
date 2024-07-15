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
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Analyzers.MatchFolderAndNamespace;

internal abstract class AbstractMatchFolderAndNamespaceDiagnosticAnalyzer<TSyntaxKind, TNamespaceSyntax>
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    where TSyntaxKind : struct
    where TNamespaceSyntax : SyntaxNode
{
    private static readonly LocalizableResourceString s_localizableTitle = new(
     nameof(AnalyzersResources.Namespace_does_not_match_folder_structure), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));

    private static readonly LocalizableResourceString s_localizableInsideMessage = new(
        nameof(AnalyzersResources.Namespace_0_does_not_match_folder_structure_expected_1), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));

    private static readonly SymbolDisplayFormat s_namespaceDisplayFormat = SymbolDisplayFormat
        .FullyQualifiedFormat
        .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted);

    protected AbstractMatchFolderAndNamespaceDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.MatchFolderAndNamespaceDiagnosticId,
            EnforceOnBuildValues.MatchFolderAndNamespace,
            CodeStyleOptions2.PreferNamespaceAndFolderMatchStructure,
            s_localizableTitle,
            s_localizableInsideMessage)
    {
    }

    protected abstract ISyntaxFacts GetSyntaxFacts();
    protected abstract ImmutableArray<TSyntaxKind> GetSyntaxKindsToAnalyze();

    protected sealed override void InitializeWorker(AnalysisContext context)
        => context.RegisterSyntaxNodeAction(AnalyzeNamespaceNode, GetSyntaxKindsToAnalyze());

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    private void AnalyzeNamespaceNode(SyntaxNodeAnalysisContext context)
    {
        var option = context.GetAnalyzerOptions().PreferNamespaceAndFolderMatchStructure;
        if (!option.Value || ShouldSkipAnalysis(context, option.Notification))
        {
            return;
        }

        // It's ok to not have a rootnamespace property, but if it's there we want to use it correctly
        context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(MatchFolderAndNamespaceConstants.RootNamespaceOption, out var rootNamespace);

        // Project directory is a must to correctly get the relative path and construct a namespace
        if (!context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(MatchFolderAndNamespaceConstants.ProjectDirOption, out var projectDir)
            || string.IsNullOrEmpty(projectDir))
        {
            return;
        }

        var namespaceDecl = (TNamespaceSyntax)context.Node;
        var symbol = context.SemanticModel.GetDeclaredSymbol(namespaceDecl);
        RoslynDebug.AssertNotNull(symbol);

        var currentNamespace = symbol.ToDisplayString(s_namespaceDisplayFormat);

        if (IsFileAndNamespaceMismatch(namespaceDecl, rootNamespace, projectDir, currentNamespace, out var targetNamespace) &&
            IsFixSupported(context.SemanticModel, namespaceDecl, context.CancellationToken))
        {
            var nameSyntax = GetSyntaxFacts().GetNameOfBaseNamespaceDeclaration(namespaceDecl);
            RoslynDebug.AssertNotNull(nameSyntax);

            context.ReportDiagnostic(Diagnostic.Create(
                Descriptor,
                nameSyntax.GetLocation(),
                additionalLocations: null,
                properties: ImmutableDictionary<string, string?>.Empty.Add(MatchFolderAndNamespaceConstants.TargetNamespace, targetNamespace),
                messageArgs: new[] { currentNamespace, targetNamespace }));
        }
    }

    private bool IsFixSupported(SemanticModel semanticModel, TNamespaceSyntax namespaceDeclaration, CancellationToken cancellationToken)
    {
        var root = namespaceDeclaration.SyntaxTree.GetRoot(cancellationToken);

        // It should not be nested in other namespaces
        if (namespaceDeclaration.Ancestors().OfType<TNamespaceSyntax>().Any())
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

        // The current namespace should be valid
        var isCurrentNamespaceInvalid = GetSyntaxFacts()
            .GetNameOfBaseNamespaceDeclaration(namespaceDeclaration)
            ?.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error)
            ?? false;

        if (isCurrentNamespaceInvalid)
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

        return true;
    }

    private bool IsFileAndNamespaceMismatch(
        TNamespaceSyntax namespaceDeclaration,
        string? rootNamespace,
        string projectDir,
        string currentNamespace,
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

        if (RoslynString.IsNullOrWhiteSpace(expectedNamespace) || expectedNamespace.Equals(currentNamespace, StringComparison.OrdinalIgnoreCase))
        {
            // The namespace currently matches the folder structure or is invalid, in which case we don't want
            // to provide a diagnostic.
            targetNamespace = null;
            return false;
        }

        targetNamespace = expectedNamespace;
        return true;
    }

    /// <summary>
    /// Returns true if the namespace declaration contains one or more partial types with multiple declarations.
    /// </summary>
    protected bool ContainsPartialTypeWithMultipleDeclarations(TNamespaceSyntax namespaceDeclaration, SemanticModel semanticModel)
    {
        var syntaxFacts = GetSyntaxFacts();

        var typeDeclarations = syntaxFacts.GetMembersOfBaseNamespaceDeclaration(namespaceDeclaration)
            .Where(syntaxFacts.IsTypeDeclaration);

        foreach (var typeDecl in typeDeclarations)
        {
            var symbol = semanticModel.GetDeclaredSymbol(typeDecl);

            // Simplify the check by assuming no multiple partial declarations in one document
            if (symbol is ITypeSymbol typeSymbol && typeSymbol.DeclaringSyntaxReferences.Length > 1)
            {
                return true;
            }
        }

        return false;
    }
}
