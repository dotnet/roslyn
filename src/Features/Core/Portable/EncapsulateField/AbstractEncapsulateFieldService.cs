﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EncapsulateField
{
    internal abstract partial class AbstractEncapsulateFieldService : ILanguageService
    {
        protected abstract Task<SyntaxNode> RewriteFieldNameAndAccessibilityAsync(string originalFieldName, bool makePrivate, Document document, SyntaxAnnotation declarationAnnotation, CodeAndImportGenerationOptionsProvider fallbackOptions, CancellationToken cancellationToken);
        protected abstract Task<ImmutableArray<IFieldSymbol>> GetFieldsAsync(Document document, TextSpan span, CancellationToken cancellationToken);

        public async Task<EncapsulateFieldResult> EncapsulateFieldsInSpanAsync(Document document, TextSpan span, CleanCodeGenerationOptionsProvider fallbackOptions, bool useDefaultBehavior, CancellationToken cancellationToken)
        {
            var fields = await GetFieldsAsync(document, span, cancellationToken).ConfigureAwait(false);
            if (fields.IsDefaultOrEmpty)
                return null;

            var firstField = fields[0];
            return new EncapsulateFieldResult(
                firstField.ToDisplayString(),
                firstField.GetGlyph(),
                c => EncapsulateFieldsAsync(document, fields, fallbackOptions, useDefaultBehavior, c));
        }

        public async Task<ImmutableArray<CodeAction>> GetEncapsulateFieldCodeActionsAsync(Document document, TextSpan span, CleanCodeGenerationOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var fields = await GetFieldsAsync(document, span, cancellationToken).ConfigureAwait(false);
            if (fields.IsDefaultOrEmpty)
                return ImmutableArray<CodeAction>.Empty;

            if (fields.Length == 1)
            {
                // there is only one field
                return EncapsulateOneField(document, fields[0], fallbackOptions);
            }

            // there are multiple fields.
            using var _ = ArrayBuilder<CodeAction>.GetInstance(out var builder);

            if (span.IsEmpty)
            {
                // if there is no selection, get action for each field + all of them.
                foreach (var field in fields)
                    builder.AddRange(EncapsulateOneField(document, field, fallbackOptions));
            }

            builder.AddRange(EncapsulateAllFields(document, fields, fallbackOptions));
            return builder.ToImmutable();
        }

        private ImmutableArray<CodeAction> EncapsulateAllFields(Document document, ImmutableArray<IFieldSymbol> fields, CleanCodeGenerationOptionsProvider fallbackOptions)
        {
            return ImmutableArray.Create(
                CodeAction.Create(
                    FeaturesResources.Encapsulate_fields_and_use_property,
                    c => EncapsulateFieldsAsync(document, fields, fallbackOptions, updateReferences: true, c),
                    nameof(FeaturesResources.Encapsulate_fields_and_use_property)),
                CodeAction.Create(
                    FeaturesResources.Encapsulate_fields_but_still_use_field,
                    c => EncapsulateFieldsAsync(document, fields, fallbackOptions, updateReferences: false, c),
                    nameof(FeaturesResources.Encapsulate_fields_but_still_use_field)));
        }

        private ImmutableArray<CodeAction> EncapsulateOneField(Document document, IFieldSymbol field, CleanCodeGenerationOptionsProvider fallbackOptions)
        {
            var fields = ImmutableArray.Create(field);
            return ImmutableArray.Create(
                CodeAction.Create(
                    string.Format(FeaturesResources.Encapsulate_field_colon_0_and_use_property, field.Name),
                    c => EncapsulateFieldsAsync(document, fields, fallbackOptions, updateReferences: true, c),
                    nameof(FeaturesResources.Encapsulate_field_colon_0_and_use_property) + "_" + field.Name),
                CodeAction.Create(
                    string.Format(FeaturesResources.Encapsulate_field_colon_0_but_still_use_field, field.Name),
                    c => EncapsulateFieldsAsync(document, fields, fallbackOptions, updateReferences: false, c),
                    nameof(FeaturesResources.Encapsulate_field_colon_0_but_still_use_field) + "_" + field.Name));
        }

        public async Task<Solution> EncapsulateFieldsAsync(
            Document document, ImmutableArray<IFieldSymbol> fields,
            CleanCodeGenerationOptionsProvider fallbackOptions,
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
                        (service, solutionInfo, callbackId, cancellationToken) => service.EncapsulateFieldsAsync(solutionInfo, callbackId, document.Id, fieldSymbolKeys, updateReferences, cancellationToken),
                        callbackTarget: new RemoteOptionsProvider<CleanCodeGenerationOptions>(solution.Services, fallbackOptions),
                        cancellationToken).ConfigureAwait(false);

                    if (!result.HasValue)
                    {
                        return solution;
                    }

                    return await RemoteUtilities.UpdateSolutionAsync(
                        solution, result.Value, cancellationToken).ConfigureAwait(false);
                }
            }

            return await EncapsulateFieldsInCurrentProcessAsync(
                document, fields, fallbackOptions, updateReferences, cancellationToken).ConfigureAwait(false);
        }

        private async Task<Solution> EncapsulateFieldsInCurrentProcessAsync(Document document, ImmutableArray<IFieldSymbol> fields, CleanCodeGenerationOptionsProvider fallbackOptions, bool updateReferences, CancellationToken cancellationToken)
        {
            Contract.ThrowIfTrue(fields.Length == 0);

            // For now, build up the multiple field case by encapsulating one at a time.
            var currentSolution = document.Project.Solution;
            foreach (var field in fields)
            {
                document = currentSolution.GetDocument(document.Id);
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var compilation = semanticModel.Compilation;

                // We couldn't resolve this field. skip it
                if (field.GetSymbolKey(cancellationToken).Resolve(compilation, cancellationToken: cancellationToken).Symbol is not IFieldSymbol currentField)
                    continue;

                var nextSolution = await EncapsulateFieldAsync(document, currentField, updateReferences, fallbackOptions, cancellationToken).ConfigureAwait(false);
                if (nextSolution == null)
                    continue;

                currentSolution = nextSolution;
            }

            return currentSolution;
        }

        private async Task<Solution> EncapsulateFieldAsync(
            Document document,
            IFieldSymbol field,
            bool updateReferences,
            CleanCodeGenerationOptionsProvider fallbackOptions,
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

            foreach (var linkedDocumentId in document.GetLinkedDocumentIds())
            {
                var linkedDocument = solution.GetDocument(linkedDocumentId);
                var linkedRoot = await linkedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var linkedFieldNode = linkedRoot.FindNode(fieldDeclaration.Span);
                if (linkedFieldNode.Span != fieldDeclaration.Span)
                {
                    continue;
                }

                var updatedRoot = linkedRoot.ReplaceNode(linkedFieldNode, linkedFieldNode.WithAdditionalAnnotations(declarationAnnotation));
                solution = solution.WithDocumentSyntaxRoot(linkedDocumentId, updatedRoot);
            }

            document = solution.GetDocument(document.Id);

            // Resolve the annotated symbol and prepare for rename.

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var compilation = semanticModel.Compilation;
            field = field.GetSymbolKey(cancellationToken).Resolve(compilation, cancellationToken: cancellationToken).Symbol as IFieldSymbol;

            // We couldn't resolve field after annotating its declaration. Bail
            if (field == null)
                return null;

            var solutionNeedingProperty = await UpdateReferencesAsync(
                updateReferences, solution, document, field, finalFieldName, generatedPropertyName, fallbackOptions, cancellationToken).ConfigureAwait(false);
            document = solutionNeedingProperty.GetDocument(document.Id);

            var markFieldPrivate = field.DeclaredAccessibility != Accessibility.Private;
            var rewrittenFieldDeclaration = await RewriteFieldNameAndAccessibilityAsync(finalFieldName, markFieldPrivate, document, declarationAnnotation, fallbackOptions, cancellationToken).ConfigureAwait(false);

            var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(fallbackOptions, cancellationToken).ConfigureAwait(false);

            document = await Formatter.FormatAsync(document.WithSyntaxRoot(rewrittenFieldDeclaration), Formatter.Annotation, formattingOptions, cancellationToken).ConfigureAwait(false);

            solution = document.Project.Solution;
            foreach (var linkedDocumentId in document.GetLinkedDocumentIds())
            {
                var linkedDocument = solution.GetDocument(linkedDocumentId);
                var linkedDocumentFormattingOptions = await linkedDocument.GetSyntaxFormattingOptionsAsync(fallbackOptions, cancellationToken).ConfigureAwait(false);
                var updatedLinkedRoot = await RewriteFieldNameAndAccessibilityAsync(finalFieldName, markFieldPrivate, linkedDocument, declarationAnnotation, fallbackOptions, cancellationToken).ConfigureAwait(false);
                var updatedLinkedDocument = await Formatter.FormatAsync(linkedDocument.WithSyntaxRoot(updatedLinkedRoot), Formatter.Annotation, linkedDocumentFormattingOptions, cancellationToken).ConfigureAwait(false);
                solution = updatedLinkedDocument.Project.Solution;
            }

            document = solution.GetDocument(document.Id);

            semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var newRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newDeclaration = newRoot.GetAnnotatedNodes<SyntaxNode>(declarationAnnotation).First();
            field = semanticModel.GetDeclaredSymbol(newDeclaration, cancellationToken) as IFieldSymbol;

            var generatedProperty = GenerateProperty(
                generatedPropertyName,
                finalFieldName,
                originalField.DeclaredAccessibility,
                originalField,
                field.ContainingType,
                new SyntaxAnnotation(),
                document);

            var simplifierOptions = await document.GetSimplifierOptionsAsync(fallbackOptions, cancellationToken).ConfigureAwait(false);

            var documentWithProperty = await AddPropertyAsync(
                document, document.Project.Solution, field, generatedProperty, fallbackOptions, cancellationToken).ConfigureAwait(false);

            documentWithProperty = await Formatter.FormatAsync(documentWithProperty, Formatter.Annotation, formattingOptions, cancellationToken).ConfigureAwait(false);
            documentWithProperty = await Simplifier.ReduceAsync(documentWithProperty, simplifierOptions, cancellationToken).ConfigureAwait(false);

            return documentWithProperty.Project.Solution;
        }

        private async Task<Solution> UpdateReferencesAsync(
            bool updateReferences, Solution solution, Document document, IFieldSymbol field, string finalFieldName, string generatedPropertyName, CodeCleanupOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            if (!updateReferences)
            {
                return solution;
            }

            var projectId = document.Project.Id;
            if (field.IsReadOnly)
            {
                // Inside the constructor we want to rename references the field to the final field name.
                var constructorLocations = GetConstructorLocations(solution, field.ContainingType);
                if (finalFieldName != field.Name && constructorLocations.Count > 0)
                {
                    solution = await RenameAsync(
                        solution, field, finalFieldName,
                        (docId, span) => IntersectsWithAny(docId, span, constructorLocations),
                        fallbackOptions,
                        cancellationToken).ConfigureAwait(false);

                    document = solution.GetDocument(document.Id);
                    var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

                    field = field.GetSymbolKey(cancellationToken).Resolve(compilation, cancellationToken: cancellationToken).Symbol as IFieldSymbol;
                    constructorLocations = GetConstructorLocations(solution, field.ContainingType);
                }

                // Outside the constructor we want to rename references to the field to final property name.
                return await RenameAsync(
                    solution, field, generatedPropertyName,
                    (documentId, span) => !IntersectsWithAny(documentId, span, constructorLocations),
                    fallbackOptions,
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Just rename everything.
                return await Renamer.RenameSymbolAsync(
                    solution, field, new SymbolRenameOptions(), generatedPropertyName, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<Solution> RenameAsync(
            Solution solution,
            IFieldSymbol field,
            string finalName,
            Func<DocumentId, TextSpan, bool> filter,
            CodeCleanupOptionsProvider fallbackOptions,
            CancellationToken cancellationToken)
        {
            var options = new SymbolRenameOptions(
                RenameOverloads: false,
                RenameInStrings: false,
                RenameInComments: false,
                RenameFile: false);

            var initialLocations = await Renamer.FindRenameLocationsAsync(
                solution, field, options, fallbackOptions, cancellationToken).ConfigureAwait(false);

            var resolution = await initialLocations.Filter(filter).ResolveConflictsAsync(
                field, finalName, nonConflictSymbolKeys: default, cancellationToken).ConfigureAwait(false);

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
            => GetConstructorNodes(containingType).Select(n => (solution.GetRequiredDocument(n.SyntaxTree).Id, n.Span)).ToSet();

        internal abstract IEnumerable<SyntaxNode> GetConstructorNodes(INamedTypeSymbol containingType);

        protected static async Task<Document> AddPropertyAsync(
            Document document,
            Solution destinationSolution,
            IFieldSymbol field,
            IPropertySymbol property,
            CodeAndImportGenerationOptionsProvider fallbackOptions,
            CancellationToken cancellationToken)
        {
            var codeGenerationService = document.GetLanguageService<ICodeGenerationService>();

            var fieldDeclaration = field.DeclaringSyntaxReferences.First();

            var context = new CodeGenerationSolutionContext(
                destinationSolution,
                new CodeGenerationContext(
                    contextLocation: fieldDeclaration.SyntaxTree.GetLocation(fieldDeclaration.Span)),
                fallbackOptions);

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
            var factory = document.GetLanguageService<SyntaxGenerator>();

            var propertySymbol = annotation.AddAnnotationToSymbol(CodeGenerationSymbolFactory.CreatePropertySymbol(containingType: containingSymbol,
                attributes: ImmutableArray<AttributeData>.Empty,
                accessibility: ComputeAccessibility(accessibility, field.Type),
                modifiers: new DeclarationModifiers(isStatic: field.IsStatic, isReadOnly: field.IsReadOnly, isUnsafe: field.RequiresUnsafeModifier()),
                type: field.GetSymbolType(),
                refKind: RefKind.None,
                explicitInterfaceImplementations: default,
                name: propertyName,
                parameters: ImmutableArray<IParameterSymbol>.Empty,
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
                ImmutableArray<AttributeData>.Empty,
                Accessibility.NotApplicable,
                ImmutableArray.Create(body));
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
                ImmutableArray<AttributeData>.Empty,
                Accessibility.NotApplicable,
                ImmutableArray.Create(body));
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

        private static readonly CultureInfo EnUSCultureInfo = new("en-US");
    }
}
