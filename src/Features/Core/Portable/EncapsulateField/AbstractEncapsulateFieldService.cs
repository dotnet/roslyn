// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EncapsulateField;

internal abstract partial class AbstractEncapsulateFieldService<
    TConstructorDeclarationSyntax> : IEncapsulateFieldService
    where TConstructorDeclarationSyntax : SyntaxNode
{
    private static readonly CultureInfo EnUSCultureInfo = new("en-US");
    private static readonly SymbolRenameOptions s_symbolRenameOptions = new(
        RenameOverloads: false,
        RenameInStrings: false,
        RenameInComments: false,
        RenameFile: false);

    protected abstract Task<SyntaxNode> RewriteFieldNameAndAccessibilityAsync(string originalFieldName, bool makePrivate, Document document, SyntaxAnnotation declarationAnnotation, CancellationToken cancellationToken);
    protected abstract Task<ImmutableArray<IFieldSymbol>> GetFieldsAsync(Document document, TextSpan span, CancellationToken cancellationToken);
    protected abstract IEnumerable<TConstructorDeclarationSyntax> GetConstructorNodes(INamedTypeSymbol containingType);

    public async Task<EncapsulateFieldResult?> EncapsulateFieldsInSpanAsync(Document document, TextSpan span, bool useDefaultBehavior, CancellationToken cancellationToken)
    {
        var fields = await GetFieldsAsync(document, span, cancellationToken).ConfigureAwait(false);
        if (fields.IsDefaultOrEmpty)
            return null;

        var firstField = fields[0];
        return new EncapsulateFieldResult(
            firstField.ToDisplayString(),
            firstField.GetGlyph(),
            cancellationToken => EncapsulateFieldsAsync(document, fields, useDefaultBehavior, cancellationToken));
    }

    public async Task<ImmutableArray<CodeAction>> GetEncapsulateFieldCodeActionsAsync(Document document, TextSpan span, CancellationToken cancellationToken)
    {
        var fields = await GetFieldsAsync(document, span, cancellationToken).ConfigureAwait(false);
        if (fields.IsDefaultOrEmpty)
            return [];

        // there is only one field
        if (fields.Length == 1)
            return EncapsulateOneField(document, fields[0]);

        // there are multiple fields.
        using var builder = TemporaryArray<CodeAction>.Empty;

        if (span.IsEmpty)
        {
            // if there is no selection, get action for each field + all of them.
            foreach (var field in fields)
                builder.AddRange(EncapsulateOneField(document, field));
        }

        builder.AddRange(EncapsulateAllFields(document, fields));
        return builder.ToImmutableAndClear();
    }

    private ImmutableArray<CodeAction> EncapsulateAllFields(Document document, ImmutableArray<IFieldSymbol> fields)
        => [
            CodeAction.Create(
                FeaturesResources.Encapsulate_fields_and_use_property,
                cancellationToken => EncapsulateFieldsAsync(document, fields, updateReferences: true, cancellationToken),
                nameof(FeaturesResources.Encapsulate_fields_and_use_property)),
            CodeAction.Create(
                FeaturesResources.Encapsulate_fields_but_still_use_field,
                cancellationToken => EncapsulateFieldsAsync(document, fields, updateReferences: false, cancellationToken),
                nameof(FeaturesResources.Encapsulate_fields_but_still_use_field)),
        ];

    private ImmutableArray<CodeAction> EncapsulateOneField(Document document, IFieldSymbol field)
    {
        var fields = ImmutableArray.Create(field);
        return
        [
            CodeAction.Create(
                string.Format(FeaturesResources.Encapsulate_field_colon_0_and_use_property, field.Name),
                cancellationToken => EncapsulateFieldsAsync(document, fields, updateReferences: true, cancellationToken),
                nameof(FeaturesResources.Encapsulate_field_colon_0_and_use_property) + "_" + field.Name),
            CodeAction.Create(
                string.Format(FeaturesResources.Encapsulate_field_colon_0_but_still_use_field, field.Name),
                cancellationToken => EncapsulateFieldsAsync(document, fields, updateReferences: false, cancellationToken),
                nameof(FeaturesResources.Encapsulate_field_colon_0_but_still_use_field) + "_" + field.Name),
        ];
    }

    public async Task<Solution> EncapsulateFieldsAsync(
        Document document, ImmutableArray<IFieldSymbol> fields,
        bool updateReferences, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using (Logger.LogBlock(FunctionId.Renamer_FindRenameLocationsAsync, cancellationToken))
        {
            var solution = document.Project.Solution;
            var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                var fieldSymbolKeys = fields.SelectAsArray(f => SymbolKey.CreateString(f, cancellationToken));

                var result = await client.TryInvokeAsync<IRemoteEncapsulateFieldService, ImmutableArray<(DocumentId, ImmutableArray<TextChange>)>>(
                    solution,
                    (service, solutionInfo, cancellationToken) => service.EncapsulateFieldsAsync(solutionInfo, document.Id, fieldSymbolKeys, updateReferences, cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                if (!result.HasValue)
                    return solution;

                return await RemoteUtilities.UpdateSolutionAsync(
                    solution, result.Value, cancellationToken).ConfigureAwait(false);
            }
        }

        return await EncapsulateFieldsInCurrentProcessAsync(
            document, fields, updateReferences, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Solution> EncapsulateFieldsInCurrentProcessAsync(Document document, ImmutableArray<IFieldSymbol> fields, bool updateReferences, CancellationToken cancellationToken)
    {
        Contract.ThrowIfTrue(fields.Length == 0);

        // For now, build up the multiple field case by encapsulating one at a time.
        var currentSolution = document.Project.Solution;
        foreach (var field in fields)
        {
            document = currentSolution.GetRequiredDocument(document.Id);
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var compilation = semanticModel.Compilation;

            // We couldn't resolve this field. skip it
            if (field.GetSymbolKey(cancellationToken).Resolve(compilation, cancellationToken: cancellationToken).Symbol is not IFieldSymbol currentField)
                continue;

            var nextSolution = await EncapsulateFieldAsync(document, currentField, updateReferences, cancellationToken).ConfigureAwait(false);
            if (nextSolution == null)
                continue;

            currentSolution = nextSolution;
        }

        return currentSolution;
    }

    private async Task<Solution?> EncapsulateFieldAsync(
        Document document,
        IFieldSymbol field,
        bool updateReferences,
        CancellationToken cancellationToken)
    {
        var originalField = field;
        var (finalFieldName, generatedPropertyName) = GenerateFieldAndPropertyNames(field);

        // Annotate the field declarations so we can find it after rename.
        var fieldDeclaration = field.DeclaringSyntaxReferences.First();
        var declarationAnnotation = new SyntaxAnnotation();
        document = document.WithSyntaxRoot(fieldDeclaration.SyntaxTree.GetRoot(cancellationToken).ReplaceNode(fieldDeclaration.GetSyntax(cancellationToken),
            fieldDeclaration.GetSyntax(cancellationToken).WithAdditionalAnnotations(declarationAnnotation)));

        var solution = document.Project.Solution;
        // Resolve the annotated symbol and prepare for rename.

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var compilation = semanticModel.Compilation;
        field = (IFieldSymbol)field.GetSymbolKey(cancellationToken).Resolve(compilation, cancellationToken: cancellationToken).Symbol!;

        // We couldn't resolve field after annotating its declaration. Bail
        if (field == null)
            return null;

        var solutionNeedingProperty = await UpdateReferencesAsync(
            updateReferences, solution, document, field, finalFieldName, generatedPropertyName, cancellationToken).ConfigureAwait(false);
        document = solutionNeedingProperty.GetRequiredDocument(document.Id);

        var markFieldPrivate = field.DeclaredAccessibility != Accessibility.Private;
        var rewrittenFieldDeclaration = await RewriteFieldNameAndAccessibilityAsync(finalFieldName, markFieldPrivate, document, declarationAnnotation, cancellationToken).ConfigureAwait(false);

        var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);

        document = await Formatter.FormatAsync(document.WithSyntaxRoot(rewrittenFieldDeclaration), Formatter.Annotation, formattingOptions, cancellationToken).ConfigureAwait(false);

        semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var newRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var newDeclaration = newRoot.GetAnnotatedNodes<SyntaxNode>(declarationAnnotation).First();
        field = (IFieldSymbol)semanticModel.GetRequiredDeclaredSymbol(newDeclaration, cancellationToken);

        var generatedProperty = GenerateProperty(
            generatedPropertyName,
            finalFieldName,
            originalField.DeclaredAccessibility,
            originalField,
            field.ContainingType,
            new SyntaxAnnotation(),
            document);

        var simplifierOptions = await document.GetSimplifierOptionsAsync(cancellationToken).ConfigureAwait(false);

        var documentWithProperty = await AddPropertyAsync(
            document, document.Project.Solution, field, generatedProperty, cancellationToken).ConfigureAwait(false);

        documentWithProperty = await Formatter.FormatAsync(documentWithProperty, Formatter.Annotation, formattingOptions, cancellationToken).ConfigureAwait(false);
        documentWithProperty = await Simplifier.ReduceAsync(documentWithProperty, simplifierOptions, cancellationToken).ConfigureAwait(false);

        return documentWithProperty.Project.Solution;
    }

    private async Task<Solution> UpdateReferencesAsync(
        bool updateReferences, Solution solution, Document document, IFieldSymbol field, string finalFieldName, string generatedPropertyName, CancellationToken cancellationToken)
    {
        if (!updateReferences)
            return solution;

        var projectId = document.Project.Id;
        var linkedDocumentIds = document.GetLinkedDocumentIds();
        using var _ = PooledHashSet<ProjectId>.GetInstance(out var linkedProjectIds);
        linkedProjectIds.AddRange(linkedDocumentIds.Select(d => d.ProjectId));

        if (field.IsReadOnly)
        {
            // Inside the constructor we want to rename references the field to the final field name.
            var constructorLocations = GetConstructorLocations(solution, field.ContainingType);
            if (finalFieldName != field.Name && constructorLocations.Count > 0)
            {
                solution = await RenameAsync(
                    solution, field, finalFieldName, linkedProjectIds,
                    filter: (docId, span) => IntersectsWithAny(docId, span, constructorLocations),
                    cancellationToken).ConfigureAwait(false);

                document = solution.GetRequiredDocument(document.Id);
                var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

                field = (IFieldSymbol)field.GetSymbolKey(cancellationToken).Resolve(compilation, cancellationToken: cancellationToken).Symbol!;
                constructorLocations = GetConstructorLocations(solution, field.ContainingType);
            }

            // Outside the constructor we want to rename references to the field to final property name.
            return await RenameAsync(
                solution, field, generatedPropertyName, linkedProjectIds,
                filter: (documentId, span) => !IntersectsWithAny(documentId, span, constructorLocations),
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // Just rename everything.
            return await RenameAsync(
                solution, field, generatedPropertyName, linkedProjectIds,
                filter: static (documentId, span) => true,
                cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<Solution> RenameAsync(
        Solution solution,
        IFieldSymbol field,
        string finalName,
        HashSet<ProjectId> linkedProjectIds,
        Func<DocumentId, TextSpan, bool> filter,
        CancellationToken cancellationToken)
    {
        var initialLocations = await Renamer.FindRenameLocationsAsync(
            solution, field, s_symbolRenameOptions, cancellationToken).ConfigureAwait(false);

        // Ensure we don't update any files in projects linked to us.  That will be taken care of automatically when we
        // edit the files in the current project
        var resolution = await initialLocations
            .Filter((documentId, span) => !linkedProjectIds.Contains(documentId.ProjectId) && filter(documentId, span))
            .ResolveConflictsAsync(field, finalName, nonConflictSymbolKeys: default, cancellationToken).ConfigureAwait(false);

        Contract.ThrowIfFalse(resolution.IsSuccessful);

        return resolution.NewSolution;
    }

    private static bool IntersectsWithAny(DocumentId documentId, TextSpan span, ISet<(DocumentId documentId, TextSpan span)> constructorLocations)
    {
        foreach (var constructor in constructorLocations)
        {
            if (constructor.documentId == documentId &&
                span.IntersectsWith(constructor.span))
            {
                return true;
            }
        }

        return false;
    }

    private ISet<(DocumentId documentId, TextSpan span)> GetConstructorLocations(Solution solution, INamedTypeSymbol containingType)
        => GetConstructorNodes(containingType)
            .Select(n => (solution.GetRequiredDocument(n.SyntaxTree).Id, n.Span))
            .ToSet();

    protected static async Task<Document> AddPropertyAsync(
        Document document,
        Solution destinationSolution,
        IFieldSymbol field,
        IPropertySymbol property,
        CancellationToken cancellationToken)
    {
        var codeGenerationService = document.GetRequiredLanguageService<ICodeGenerationService>();

        var fieldDeclaration = field.DeclaringSyntaxReferences.First();

        var context = new CodeGenerationSolutionContext(
            destinationSolution,
            new CodeGenerationContext(
                contextLocation: fieldDeclaration.SyntaxTree.GetLocation(fieldDeclaration.Span)));

        var destination = field.ContainingType;
        return await codeGenerationService.AddPropertyAsync(
            context, destination, property, cancellationToken).ConfigureAwait(false);
    }

    protected static IPropertySymbol GenerateProperty(
        string propertyName, string fieldName,
        Accessibility accessibility,
        IFieldSymbol field,
        INamedTypeSymbol containingSymbol,
        SyntaxAnnotation annotation,
        Document document)
    {
        var factory = document.GetRequiredLanguageService<SyntaxGenerator>();

        var propertySymbol = annotation.AddAnnotationToSymbol(CodeGenerationSymbolFactory.CreatePropertySymbol(containingType: containingSymbol,
            attributes: [],
            accessibility: ComputeAccessibility(accessibility, field.Type),
            modifiers: new DeclarationModifiers(isStatic: field.IsStatic, isReadOnly: field.IsReadOnly, isUnsafe: field.RequiresUnsafeModifier()),
            type: field.Type,
            refKind: RefKind.None,
            explicitInterfaceImplementations: default,
            name: propertyName,
            parameters: [],
            getMethod: CreateGet(fieldName, field, factory),
            setMethod: field.IsReadOnly || field.IsConst ? null : CreateSet(fieldName, field, factory)));

        return Simplifier.Annotation.AddAnnotationToSymbol(
            Formatter.Annotation.AddAnnotationToSymbol(propertySymbol));
    }

    protected abstract (string fieldName, string propertyName) GenerateFieldAndPropertyNames(IFieldSymbol field);

    protected static Accessibility ComputeAccessibility(Accessibility accessibility, ITypeSymbol type)
    {
        var computedAccessibility = accessibility;
        if (accessibility is Accessibility.NotApplicable or Accessibility.Private)
        {
            computedAccessibility = Accessibility.Public;
        }

        var returnTypeAccessibility = type.DetermineMinimalAccessibility();

        return AccessibilityUtilities.Minimum(computedAccessibility, returnTypeAccessibility);
    }

    protected static IMethodSymbol CreateSet(string originalFieldName, IFieldSymbol field, SyntaxGenerator factory)
    {
        var assigned = !field.IsStatic
            ? factory.MemberAccessExpression(
                factory.ThisExpression(),
                factory.IdentifierName(originalFieldName))
            : factory.IdentifierName(originalFieldName);

        var body = factory.ExpressionStatement(
            factory.AssignmentStatement(
                assigned.WithAdditionalAnnotations(Simplifier.Annotation),
            factory.IdentifierName("value")));

        return CodeGenerationSymbolFactory.CreateAccessorSymbol(
            [],
            Accessibility.NotApplicable,
            [body]);
    }

    protected static IMethodSymbol CreateGet(string originalFieldName, IFieldSymbol field, SyntaxGenerator factory)
    {
        var value = !field.IsStatic
            ? factory.MemberAccessExpression(
                factory.ThisExpression(),
                factory.IdentifierName(originalFieldName))
            : factory.IdentifierName(originalFieldName);

        var body = factory.ReturnStatement(
            value.WithAdditionalAnnotations(Simplifier.Annotation));

        return CodeGenerationSymbolFactory.CreateAccessorSymbol(
            [],
            Accessibility.NotApplicable,
            [body]);
    }

    private static readonly char[] s_underscoreCharArray = ['_'];

    protected static string GeneratePropertyName(string fieldName)
    {
        // Trim leading underscores
        var baseName = fieldName.TrimStart(s_underscoreCharArray);

        // Trim leading "m_"
        if (baseName is ['m', '_', .. var rest])
            baseName = rest;

        // Take original name if no characters left
        if (baseName.Length == 0)
            baseName = fieldName;

        // Make the first character upper case using the "en-US" culture.  See discussion at
        // https://github.com/dotnet/roslyn/issues/5524.
        var firstCharacter = EnUSCultureInfo.TextInfo.ToUpper(baseName[0]);
        return firstCharacter.ToString() + baseName[1..];
    }
}
