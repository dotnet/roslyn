// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractInterface;

internal abstract class AbstractExtractInterfaceService : ILanguageService
{
    protected abstract Task<SyntaxNode> GetTypeDeclarationAsync(
        Document document,
        int position,
        TypeDiscoveryRule typeDiscoveryRule,
        CancellationToken cancellationToken);

    protected abstract Task<Solution> UpdateMembersWithExplicitImplementationsAsync(
        Solution unformattedSolution,
        IReadOnlyList<DocumentId> documentId,
        INamedTypeSymbol extractedInterfaceSymbol,
        INamedTypeSymbol typeToExtractFrom,
        IEnumerable<ISymbol> includedMembers,
        ImmutableDictionary<ISymbol, SyntaxAnnotation> symbolToDeclarationAnnotationMap,
        CancellationToken cancellationToken);

    internal abstract string GetContainingNamespaceDisplay(INamedTypeSymbol typeSymbol, CompilationOptions compilationOptions);

    internal abstract bool ShouldIncludeAccessibilityModifier(SyntaxNode typeNode);

    public async Task<ImmutableArray<ExtractInterfaceCodeAction>> GetExtractInterfaceCodeActionAsync(Document document, TextSpan span, CancellationToken cancellationToken)
    {
        var typeAnalysisResult = await AnalyzeTypeAtPositionAsync(document, span.Start, TypeDiscoveryRule.TypeNameOnly, cancellationToken).ConfigureAwait(false);

        return typeAnalysisResult.CanExtractInterface
            ? [new ExtractInterfaceCodeAction(this, typeAnalysisResult)]
            : [];
    }

    public async Task<ExtractInterfaceResult> ExtractInterfaceAsync(
        Document documentWithTypeToExtractFrom,
        int position,
        Action<string, NotificationSeverity> errorHandler,
        CancellationToken cancellationToken)
    {
        var typeAnalysisResult = await AnalyzeTypeAtPositionAsync(
            documentWithTypeToExtractFrom,
            position,
            TypeDiscoveryRule.TypeDeclaration,
            cancellationToken).ConfigureAwait(false);

        if (!typeAnalysisResult.CanExtractInterface)
        {
            errorHandler(typeAnalysisResult.ErrorMessage, NotificationSeverity.Error);
            return new ExtractInterfaceResult(succeeded: false);
        }

        return await ExtractInterfaceFromAnalyzedTypeAsync(typeAnalysisResult, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ExtractInterfaceTypeAnalysisResult> AnalyzeTypeAtPositionAsync(
        Document document,
        int position,
        TypeDiscoveryRule typeDiscoveryRule,
        CancellationToken cancellationToken)
    {
        var typeNode = await GetTypeDeclarationAsync(document, position, typeDiscoveryRule, cancellationToken).ConfigureAwait(false);
        if (typeNode == null)
        {
            var errorMessage = FeaturesResources.Could_not_extract_interface_colon_The_selection_is_not_inside_a_class_interface_struct;
            return new ExtractInterfaceTypeAnalysisResult(errorMessage);
        }

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel.GetDeclaredSymbol(typeNode, cancellationToken) is not INamedTypeSymbol { TypeKind: not TypeKind.Extension } typeToExtractFrom)
        {
            var errorMessage = FeaturesResources.Could_not_extract_interface_colon_The_selection_is_not_inside_a_class_interface_struct;
            return new ExtractInterfaceTypeAnalysisResult(errorMessage);
        }

        var extractableMembers = typeToExtractFrom.GetMembers().WhereAsArray(IsExtractableMember);
        if (!extractableMembers.Any())
        {
            var errorMessage = FeaturesResources.Could_not_extract_interface_colon_The_type_does_not_contain_any_member_that_can_be_extracted_to_an_interface;
            return new ExtractInterfaceTypeAnalysisResult(errorMessage);
        }

        var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);
        return new ExtractInterfaceTypeAnalysisResult(document, typeNode, typeToExtractFrom, extractableMembers, formattingOptions);
    }

    public async Task<ExtractInterfaceResult> ExtractInterfaceFromAnalyzedTypeAsync(
        ExtractInterfaceTypeAnalysisResult refactoringResult,
        CancellationToken cancellationToken)
    {
        var containingNamespaceDisplay = refactoringResult.TypeToExtractFrom.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : refactoringResult.TypeToExtractFrom.ContainingNamespace.ToDisplayString();

        var extractInterfaceOptions = GetExtractInterfaceOptions(
            refactoringResult.DocumentToExtractFrom,
            refactoringResult.TypeToExtractFrom,
            refactoringResult.ExtractableMembers,
            containingNamespaceDisplay,
            refactoringResult.FormattingOptions,
            cancellationToken);

        if (extractInterfaceOptions.IsCancelled)
        {
            return new ExtractInterfaceResult(succeeded: false);
        }

        return await ExtractInterfaceFromAnalyzedTypeAsync(refactoringResult, extractInterfaceOptions, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ExtractInterfaceResult> ExtractInterfaceFromAnalyzedTypeAsync(
        ExtractInterfaceTypeAnalysisResult refactoringResult,
        ExtractInterfaceOptionsResult extractInterfaceOptions,
        CancellationToken cancellationToken)
    {
        var solution = refactoringResult.DocumentToExtractFrom.Project.Solution;

        var extractedInterfaceSymbol = CodeGenerationSymbolFactory.CreateNamedTypeSymbol(
            attributes: default,
            accessibility: ShouldIncludeAccessibilityModifier(refactoringResult.TypeNode) ? refactoringResult.TypeToExtractFrom.DeclaredAccessibility : Accessibility.NotApplicable,
            modifiers: DeclarationModifiers.None,
            typeKind: TypeKind.Interface,
            name: extractInterfaceOptions.InterfaceName,
            typeParameters: ExtractTypeHelpers.GetRequiredTypeParametersForMembers(refactoringResult.TypeToExtractFrom, extractInterfaceOptions.IncludedMembers),
            members: CreateInterfaceMembers(extractInterfaceOptions.IncludedMembers));

        switch (extractInterfaceOptions.Location)
        {
            case ExtractInterfaceOptionsResult.ExtractLocation.NewFile:
                var containingNamespaceDisplay = GetContainingNamespaceDisplay(refactoringResult.TypeToExtractFrom, refactoringResult.DocumentToExtractFrom.Project.CompilationOptions!);
                return await ExtractInterfaceToNewFileAsync(
                    solution,
                    containingNamespaceDisplay,
                    extractedInterfaceSymbol,
                    refactoringResult,
                    extractInterfaceOptions,
                    cancellationToken).ConfigureAwait(false);

            case ExtractInterfaceOptionsResult.ExtractLocation.SameFile:
                return await ExtractInterfaceToSameFileAsync(
                    solution,
                    refactoringResult,
                    extractedInterfaceSymbol,
                    extractInterfaceOptions,
                    cancellationToken).ConfigureAwait(false);

            default: throw new InvalidOperationException($"Unable to extract interface for operation of type {extractInterfaceOptions.GetType()}");
        }
    }

    private async Task<ExtractInterfaceResult> ExtractInterfaceToNewFileAsync(
        Solution solution, string containingNamespaceDisplay, INamedTypeSymbol extractedInterfaceSymbol,
        ExtractInterfaceTypeAnalysisResult refactoringResult, ExtractInterfaceOptionsResult extractInterfaceOptions,
        CancellationToken cancellationToken)
    {
        var symbolMapping = await AnnotatedSymbolMapping.CreateAsync(
            extractInterfaceOptions.IncludedMembers,
            solution,
            refactoringResult.TypeNode,
            cancellationToken).ConfigureAwait(false);

        var (unformattedInterfaceDocument, _) = await ExtractTypeHelpers.AddTypeToNewFileAsync(
            symbolMapping.AnnotatedSolution,
            containingNamespaceDisplay,
            extractInterfaceOptions.FileName,
            refactoringResult.DocumentToExtractFrom.Project.Id,
            refactoringResult.DocumentToExtractFrom.Folders,
            extractedInterfaceSymbol,
            refactoringResult.DocumentToExtractFrom,
            cancellationToken).ConfigureAwait(false);

        var completedUnformattedSolution = await GetSolutionWithOriginalTypeUpdatedAsync(
            unformattedInterfaceDocument.Project.Solution,
            [.. symbolMapping.DocumentIdsToSymbolMap.Keys],
            symbolMapping.TypeNodeAnnotation,
            refactoringResult.TypeToExtractFrom,
            extractedInterfaceSymbol,
            extractInterfaceOptions.IncludedMembers,
            symbolMapping.SymbolToDeclarationAnnotationMap,
            cancellationToken).ConfigureAwait(false);

        var completedSolution = await GetFormattedSolutionAsync(
            completedUnformattedSolution,
            symbolMapping.DocumentIdsToSymbolMap.Keys.Concat(unformattedInterfaceDocument.Id),
            cancellationToken).ConfigureAwait(false);

        return new ExtractInterfaceResult(
            succeeded: true,
            updatedSolution: completedSolution,
            navigationDocumentId: unformattedInterfaceDocument.Id);
    }

    private async Task<ExtractInterfaceResult> ExtractInterfaceToSameFileAsync(
        Solution solution, ExtractInterfaceTypeAnalysisResult refactoringResult, INamedTypeSymbol extractedInterfaceSymbol,
        ExtractInterfaceOptionsResult extractInterfaceOptions, CancellationToken cancellationToken)
    {
        // Track all of the symbols we need to modify, which includes the original type declaration being modified
        var symbolMapping = await AnnotatedSymbolMapping.CreateAsync(
            extractInterfaceOptions.IncludedMembers,
            solution,
            refactoringResult.TypeNode,
            cancellationToken).ConfigureAwait(false);

        var document = symbolMapping.AnnotatedSolution.GetDocument(refactoringResult.DocumentToExtractFrom.Id);

        var (documentWithInterface, _) = await ExtractTypeHelpers.AddTypeToExistingFileAsync(
            document,
            extractedInterfaceSymbol,
            symbolMapping,
            cancellationToken).ConfigureAwait(false);

        var unformattedSolution = documentWithInterface.Project.Solution;

        // After the interface is inserted, update the original type to show it implements the new interface
        var unformattedSolutionWithUpdatedType = await GetSolutionWithOriginalTypeUpdatedAsync(
            unformattedSolution, [.. symbolMapping.DocumentIdsToSymbolMap.Keys],
            symbolMapping.TypeNodeAnnotation,
            refactoringResult.TypeToExtractFrom, extractedInterfaceSymbol,
            extractInterfaceOptions.IncludedMembers, symbolMapping.SymbolToDeclarationAnnotationMap, cancellationToken).ConfigureAwait(false);

        var completedSolution = await GetFormattedSolutionAsync(
            unformattedSolutionWithUpdatedType,
            symbolMapping.DocumentIdsToSymbolMap.Keys.Concat(refactoringResult.DocumentToExtractFrom.Id),
            cancellationToken).ConfigureAwait(false);

        return new ExtractInterfaceResult(
            succeeded: true,
            updatedSolution: completedSolution,
            navigationDocumentId: refactoringResult.DocumentToExtractFrom.Id);
    }

    internal static ExtractInterfaceOptionsResult GetExtractInterfaceOptions(
        Document document,
        INamedTypeSymbol type,
        ImmutableArray<ISymbol> extractableMembers,
        string containingNamespace,
        SyntaxFormattingOptions formattingOptions,
        CancellationToken cancellationToken)
    {
        var conflictingTypeNames = type.ContainingNamespace.GetAllTypes(cancellationToken).SelectAsArray(t => t.Name);
        var candidateInterfaceName = type.TypeKind == TypeKind.Interface ? type.Name : "I" + type.Name;
        var defaultInterfaceName = NameGenerator.GenerateUniqueName(candidateInterfaceName, name => !conflictingTypeNames.Contains(name));
        var generatedNameTypeParameterSuffix = ExtractTypeHelpers.GetTypeParameterSuffix(document, formattingOptions, type, extractableMembers, cancellationToken);

        var service = document.Project.Solution.Services.GetRequiredService<IExtractInterfaceOptionsService>();
        return service.GetExtractInterfaceOptions(
            document,
            extractableMembers,
            defaultInterfaceName,
            conflictingTypeNames,
            containingNamespace,
            generatedNameTypeParameterSuffix);
    }

    private static async Task<Solution> GetFormattedSolutionAsync(Solution unformattedSolution, IEnumerable<DocumentId> documentIds, CancellationToken cancellationToken)
    {
        // Since code action performs formatting and simplification on a single document, 
        // this ensures that anything marked with formatter or simplifier annotations gets 
        // correctly handled as long as it it's in the listed documents
        var formattedSolution = unformattedSolution;
        foreach (var documentId in documentIds)
        {
            var document = formattedSolution.GetRequiredDocument(documentId);

            var cleanupOptions = await document.GetCodeCleanupOptionsAsync(cancellationToken).ConfigureAwait(false);

            var formattedDocument = await Formatter.FormatAsync(
                document,
                Formatter.Annotation,
                cleanupOptions.FormattingOptions,
                cancellationToken).ConfigureAwait(false);

            var simplifiedDocument = await Simplifier.ReduceAsync(
                formattedDocument,
                Simplifier.Annotation,
                cleanupOptions.SimplifierOptions,
                cancellationToken).ConfigureAwait(false);

            formattedSolution = simplifiedDocument.Project.Solution;
        }

        return formattedSolution;
    }

    private async Task<Solution> GetSolutionWithOriginalTypeUpdatedAsync(
        Solution solution,
        ImmutableArray<DocumentId> documentIds,
        SyntaxAnnotation typeNodeAnnotation,
        INamedTypeSymbol typeToExtractFrom,
        INamedTypeSymbol extractedInterfaceSymbol,
        IEnumerable<ISymbol> includedMembers,
        ImmutableDictionary<ISymbol, SyntaxAnnotation> symbolToDeclarationAnnotationMap,
        CancellationToken cancellationToken)
    {
        // If an interface "INewInterface" is extracted from an interface "IExistingInterface",
        // then "INewInterface" is not marked as implementing "IExistingInterface" and its 
        // extracted members are also not updated.
        if (typeToExtractFrom.TypeKind == TypeKind.Interface)
        {
            return solution;
        }

        var unformattedSolution = solution;
        foreach (var documentId in documentIds)
        {
            var document = solution.GetRequiredDocument(documentId);
            var currentRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var editor = new SyntaxEditor(currentRoot, solution.Services);

            var syntaxGenerator = SyntaxGenerator.GetGenerator(document);
            var typeReference = syntaxGenerator.TypeExpression(extractedInterfaceSymbol);

            var typeDeclaration = currentRoot.GetAnnotatedNodes(typeNodeAnnotation).SingleOrDefault();

            if (typeDeclaration == null)
            {
                continue;
            }

            var unformattedTypeDeclaration = syntaxGenerator.AddInterfaceType(typeDeclaration, typeReference).WithAdditionalAnnotations(Formatter.Annotation);
            editor.ReplaceNode(typeDeclaration, unformattedTypeDeclaration);

            unformattedSolution = document.WithSyntaxRoot(editor.GetChangedRoot()).Project.Solution;

            // Only update the first instance of the typedeclaration,
            // since it's not needed in all declarations
            break;
        }

        var updatedUnformattedSolution = await UpdateMembersWithExplicitImplementationsAsync(
            unformattedSolution,
            documentIds,
            extractedInterfaceSymbol,
            typeToExtractFrom,
            includedMembers,
            symbolToDeclarationAnnotationMap,
            cancellationToken).ConfigureAwait(false);

        return updatedUnformattedSolution;
    }

    private static ImmutableArray<ISymbol> CreateInterfaceMembers(IEnumerable<ISymbol> includedMembers)
    {
        using var _ = ArrayBuilder<ISymbol>.GetInstance(out var interfaceMembers);

        foreach (var member in includedMembers)
        {
            switch (member)
            {
                case IEventSymbol @event:
                    interfaceMembers.Add(CodeGenerationSymbolFactory.CreateEventSymbol(
                        attributes: [],
                        accessibility: Accessibility.Public,
                        modifiers: new DeclarationModifiers(isAbstract: true, isStatic: member.IsStatic),
                        type: @event.Type,
                        explicitInterfaceImplementations: default,
                        name: @event.Name));
                    break;
                case IMethodSymbol method:
                    interfaceMembers.Add(CodeGenerationSymbolFactory.CreateMethodSymbol(
                        attributes: [],
                        accessibility: Accessibility.Public,
                        modifiers: new DeclarationModifiers(isAbstract: true, isStatic: member.IsStatic, isUnsafe: method.RequiresUnsafeModifier()),
                        returnType: method.ReturnType,
                        refKind: method.RefKind,
                        explicitInterfaceImplementations: default,
                        name: method.Name,
                        typeParameters: method.TypeParameters,
                        parameters: method.Parameters,
                        isInitOnly: method.IsInitOnly));
                    break;
                case IPropertySymbol property:
                    // We recreate the get accessor because it is possible it has the readonly modifier due
                    // to being an auto property on a struct which is invalid for an interface member

                    var getMethod = property.GetMethod != null && property.GetMethod.DeclaredAccessibility == Accessibility.Public
                        ? CodeGenerationSymbolFactory.CreateAccessorSymbol(property.GetMethod, property.GetMethod.GetAttributes())
                        : null;

                    interfaceMembers.Add(CodeGenerationSymbolFactory.CreatePropertySymbol(
                        attributes: [],
                        accessibility: Accessibility.Public,
                        modifiers: new DeclarationModifiers(isAbstract: true, isStatic: member.IsStatic, isUnsafe: property.RequiresUnsafeModifier()),
                        type: property.Type,
                        refKind: property.RefKind,
                        explicitInterfaceImplementations: default,
                        name: property.Name,
                        parameters: property.Parameters,
                        getMethod: getMethod,
                        setMethod: property is { SetMethod.DeclaredAccessibility: Accessibility.Public } ? property.SetMethod : null,
                        isIndexer: property.IsIndexer));
                    break;
                default:
                    Debug.Assert(false, string.Format(FeaturesResources.Unexpected_interface_member_kind_colon_0, member.Kind.ToString()));
                    break;
            }
        }

        return interfaceMembers.ToImmutableAndClear();
    }

    internal virtual bool IsExtractableMember(ISymbol m)
    {
        if (m.DeclaredAccessibility != Accessibility.Public ||
            m.Name == "<Clone>$") // TODO: Use WellKnownMemberNames.CloneMethodName when it's public.
        {
            return false;
        }

        if (m.Kind == SymbolKind.Event || m.IsOrdinaryMethod())
            return true;

        if (m is IPropertySymbol prop)
        {
            return !prop.IsWithEvents &&
                (prop is { GetMethod.DeclaredAccessibility: Accessibility.Public } ||
                 prop is { SetMethod.DeclaredAccessibility: Accessibility.Public });
        }

        return false;
    }
}
