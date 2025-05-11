// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType;

internal abstract class AbstractMoveTypeService : IMoveTypeService
{
    /// <summary>
    /// Annotation to mark the namespace encapsulating the type that has been moved
    /// </summary>
    public static SyntaxAnnotation NamespaceScopeMovedAnnotation = new(nameof(MoveTypeOperationKind.MoveTypeNamespaceScope));

    public abstract Task<Solution> GetModifiedSolutionAsync(Document document, TextSpan textSpan, MoveTypeOperationKind operationKind, CancellationToken cancellationToken);
    public abstract Task<ImmutableArray<CodeAction>> GetRefactoringAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken);
}

internal abstract partial class AbstractMoveTypeService<TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TCompilationUnitSyntax> :
    AbstractMoveTypeService
    where TService : AbstractMoveTypeService<TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TCompilationUnitSyntax>
    where TTypeDeclarationSyntax : SyntaxNode
    where TNamespaceDeclarationSyntax : SyntaxNode
    where TCompilationUnitSyntax : SyntaxNode
{
    protected abstract (string name, int arity) GetSymbolNameAndArity(TTypeDeclarationSyntax syntax);
    protected abstract Task<TTypeDeclarationSyntax?> GetRelevantNodeAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken);

    protected abstract bool IsMemberDeclaration(SyntaxNode syntaxNode);

    protected string GetSymbolName(TTypeDeclarationSyntax syntax)
        => GetSymbolNameAndArity(syntax).name;

    public override async Task<ImmutableArray<CodeAction>> GetRefactoringAsync(
        Document document, TextSpan textSpan, CancellationToken cancellationToken)
    {
        var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var typeDeclaration = await GetTypeDeclarationAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
        return CreateActions(semanticDocument, typeDeclaration);
    }

    public override async Task<Solution> GetModifiedSolutionAsync(Document document, TextSpan textSpan, MoveTypeOperationKind operationKind, CancellationToken cancellationToken)
    {
        var typeDeclaration = await GetTypeDeclarationAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
        if (typeDeclaration == null)
            return document.Project.Solution;

        var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var suggestedFileNames = GetSuggestedFileNames(semanticDocument, typeDeclaration, includeComplexFileNames: false);

        var editor = Editor.GetEditor(operationKind, (TService)this, semanticDocument, typeDeclaration, suggestedFileNames.First(), cancellationToken);
        var modifiedSolution = await editor.GetModifiedSolutionAsync().ConfigureAwait(false);
        return modifiedSolution ?? document.Project.Solution;
    }

    private async Task<TTypeDeclarationSyntax?> GetTypeDeclarationAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
    {
        var nodeToAnalyze = await GetRelevantNodeAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
        if (nodeToAnalyze == null)
            return null;

        var name = this.GetSymbolName(nodeToAnalyze);
        return name == "" ? null : nodeToAnalyze;
    }

    private ImmutableArray<CodeAction> CreateActions(
        SemanticDocument document, TTypeDeclarationSyntax? typeDeclaration)
    {
        if (typeDeclaration is null)
            return [];

        var documentNameWithoutExtension = GetDocumentNameWithoutExtension(document);
        var typeMatchesDocumentName = TypeMatchesDocumentName(typeDeclaration, documentNameWithoutExtension);

        // if type name matches document name, per style conventions, we have nothing to do.
        if (typeMatchesDocumentName)
            return [];

        using var _ = ArrayBuilder<CodeAction>.GetInstance(out var actions);

        var manyTypes = MultipleTopLevelTypeDeclarationInSourceDocument(document.Root);
        var isNestedType = IsNestedType(typeDeclaration);

        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var isClassNextToGlobalStatements = !manyTypes && ClassNextToGlobalStatements(document.Root, syntaxFacts);

        var suggestedFileNames = GetSuggestedFileNames(
            document, typeDeclaration, includeComplexFileNames: false);

        // (1) Add Move type to new file code action:
        // case 1: There are multiple type declarations in current document. offer, move to new file.
        // case 2: This is a nested type, offer to move to new file.
        // case 3: If there is a single type decl in current file, *do not* offer move to new file,
        //         rename actions are sufficient in this case.
        // case 4: If there are top level statements(Global statements) offer to move even
        //         in cases where there are only one class in the file.
        if (manyTypes || isNestedType || isClassNextToGlobalStatements)
        {
            foreach (var fileName in suggestedFileNames)
                actions.Add(GetCodeAction(fileName, operationKind: MoveTypeOperationKind.MoveType));
        }

        // (2) Add rename file and rename type code actions:
        // Case: No type declaration in file matches the file name.
        if (!AnyTopLevelTypeMatchesDocumentName())
        {
            foreach (var fileName in suggestedFileNames)
                actions.Add(GetCodeAction(fileName, operationKind: MoveTypeOperationKind.RenameFile));

            // Only if the document name can be legal identifier in the language, offer to rename type with document name
            if (syntaxFacts.IsValidIdentifier(documentNameWithoutExtension))
            {
                actions.Add(GetCodeAction(
                    fileName: documentNameWithoutExtension,
                    operationKind: MoveTypeOperationKind.RenameType));
            }
        }

        Debug.Assert(actions.Count != 0, "No code actions found for MoveType Refactoring");

        return actions.ToImmutableAndClear();

        bool AnyTopLevelTypeMatchesDocumentName()
            => TopLevelTypeDeclarations(document.Root).Any(
                typeDeclaration => TypeMatchesDocumentName(
                    typeDeclaration, documentNameWithoutExtension));

        MoveTypeCodeAction GetCodeAction(string fileName, MoveTypeOperationKind operationKind)
            => new((TService)this, document, typeDeclaration, operationKind, fileName);
    }

    private static bool ClassNextToGlobalStatements(SyntaxNode root, ISyntaxFactsService syntaxFacts)
        => syntaxFacts.ContainsGlobalStatement(root);

    private static bool IsNestedType(TTypeDeclarationSyntax typeNode)
        => typeNode.Parent is TTypeDeclarationSyntax;

    /// <summary>
    /// checks if there is a single top level type declaration in a document
    /// </summary>
    /// <remarks>
    /// optimized for perf, uses Skip(1).Any() instead of Count() > 1
    /// </remarks>
    private static bool MultipleTopLevelTypeDeclarationInSourceDocument(SyntaxNode root)
        => TopLevelTypeDeclarations(root).Skip(1).Any();

    private static IEnumerable<TTypeDeclarationSyntax> TopLevelTypeDeclarations(SyntaxNode root)
        => root.DescendantNodes(n => n is TCompilationUnitSyntax or TNamespaceDeclarationSyntax).OfType<TTypeDeclarationSyntax>();

    private static string GetDocumentNameWithoutExtension(SemanticDocument document)
        => Path.GetFileNameWithoutExtension(document.Document.Name);

    /// <summary>
    /// checks if type name matches its parent document name, per style rules.
    /// </summary>
    /// <remarks>
    /// Note: For a nested type, a matching document name could be just the type name or a
    /// dotted qualified name of its type hierarchy.
    /// </remarks>
    protected bool TypeMatchesDocumentName(
        TTypeDeclarationSyntax typeNode,
        string documentNameWithoutExtension)
    {
        // If it is not a nested type, we compare the unqualified type name with the document name.
        // If it is a nested type, the type name `Outer.Inner` matches file names `Inner.cs` and `Outer.Inner.cs`
        var (typeName, arity) = GetSymbolNameAndArity(typeNode);
        if (TypeNameMatches(documentNameWithoutExtension, typeName, arity))
            return true;

        var typeNameParts = GetTypeNamePartsForNestedTypeNode(typeNode).ToImmutableArray();
        var fileNameParts = documentNameWithoutExtension.Split('.', '+');

        if (typeNameParts.Length != fileNameParts.Length)
            return false;

        // qualified type name `Outer.Inner` matches file names `Inner.cs` and `Outer.Inner.cs` as well as
        // Outer`1.Inner`2.cs
        for (int i = 0, n = typeNameParts.Length; i < n; i++)
        {
            if (!TypeNameMatches(fileNameParts[i], typeNameParts[i].name, typeNameParts[i].arity))
                return false;
        }

        return true;
    }

    private static bool TypeNameMatches(string documentNameWithoutExtension, string typeName, int arity)
    {
        if (typeName.Equals(documentNameWithoutExtension, StringComparison.CurrentCulture))
            return true;

        if ($"{typeName}`{arity}".Equals(documentNameWithoutExtension, StringComparison.CurrentCulture))
            return true;

        return false;
    }

    private ImmutableArray<string> GetSuggestedFileNames(
        SemanticDocument document,
        TTypeDeclarationSyntax typeNode,
        bool includeComplexFileNames)
    {
        var documentNameWithExtension = document.Document.Name;
        var isNestedType = IsNestedType(typeNode);
        var (typeName, arity) = this.GetSymbolNameAndArity(typeNode);
        var fileExtension = Path.GetExtension(documentNameWithExtension);

        var standaloneName = typeName + fileExtension;

        using var _ = ArrayBuilder<string>.GetInstance(out var suggestedFileNames);

        suggestedFileNames.Add(typeName + fileExtension);
        if (includeComplexFileNames && arity > 0)
            suggestedFileNames.Add($"{typeName}`{arity}{fileExtension}");

        if (isNestedType)
        {
            var typeNameParts = GetTypeNamePartsForNestedTypeNode(typeNode);
            AddNameParts(typeNameParts.Select(t => t.name));

            if (includeComplexFileNames && typeNameParts.Any(t => t.arity > 0))
                AddNameParts(typeNameParts.Select(t => t.arity > 0 ? $"{t.name}`{t.arity}" : t.name));
        }

        return suggestedFileNames.ToImmutableAndClear();

        void AddNameParts(IEnumerable<string> parts)
        {
            AddNamePartsWithSeparator(parts, ".");

            if (includeComplexFileNames)
                AddNamePartsWithSeparator(parts, "+");
        }

        void AddNamePartsWithSeparator(IEnumerable<string> parts, string separator)
        {
            suggestedFileNames.Add(parts.Join(separator) + fileExtension);
        }
    }

    private IEnumerable<(string name, int arity)> GetTypeNamePartsForNestedTypeNode(TTypeDeclarationSyntax typeNode)
        => typeNode.AncestorsAndSelf()
                   .OfType<TTypeDeclarationSyntax>()
                   .Select(GetSymbolNameAndArity)
                   .Reverse();
}
