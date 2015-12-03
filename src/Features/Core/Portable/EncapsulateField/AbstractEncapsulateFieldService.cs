// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EncapsulateField
{
    internal abstract class AbstractEncapsulateFieldService : ILanguageService
    {
        public async Task<EncapsulateFieldResult> EncapsulateFieldAsync(Document document, TextSpan span, bool useDefaultBehavior, CancellationToken cancellationToken)
        {
            var fields = await GetFieldsAsync(document, span, cancellationToken).ConfigureAwait(false);
            if (fields == null || !fields.Any())
            {
                return null;
            }

            return new EncapsulateFieldResult(c => EncapsulateFieldResultAsync(document, span, useDefaultBehavior, c));
        }

        public async Task<IEnumerable<EncapsulateFieldCodeAction>> GetEncapsulateFieldCodeActionsAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var fields = (await GetFieldsAsync(document, span, cancellationToken).ConfigureAwait(false)).ToImmutableArrayOrEmpty();
            if (fields.Length == 0)
            {
                return SpecializedCollections.EmptyEnumerable<EncapsulateFieldCodeAction>();
            }

            if (fields.Length == 1)
            {
                // there is only one field
                return EncapsulateOneField(document, span, fields[0], index: 0);
            }
            else
            {
                // there are multiple fields.
                var current = SpecializedCollections.EmptyEnumerable<EncapsulateFieldCodeAction>();

                if (span.IsEmpty)
                {
                    // if there is no selection, get action for each field + all of them.
                    for (var i = 0; i < fields.Length; i++)
                    {
                        current = current.Concat(EncapsulateOneField(document, span, fields[i], i));
                    }
                }

                return current.Concat(EncapsulateAllFields(document, span));
            }
        }

        private IEnumerable<EncapsulateFieldCodeAction> EncapsulateAllFields(Document document, TextSpan span)
        {
            var action1Text = FeaturesResources.EncapsulateFieldsUsages;
            var action2Text = FeaturesResources.EncapsulateFields;

            return new[]
            {
                new EncapsulateFieldCodeAction(new EncapsulateFieldResult(c => EncapsulateFieldResultAsync(document, span, true, c)), action1Text),
                new EncapsulateFieldCodeAction(new EncapsulateFieldResult(c => EncapsulateFieldResultAsync(document, span, false, c)), action2Text)
            };
        }

        private IEnumerable<EncapsulateFieldCodeAction> EncapsulateOneField(Document document, TextSpan span, IFieldSymbol field, int index)
        {
            var action1Text = string.Format(FeaturesResources.EncapsulateFieldUsages, field.Name);
            var action2Text = string.Format(FeaturesResources.EncapsulateField, field.Name);

            return new[]
            {
                new EncapsulateFieldCodeAction(new EncapsulateFieldResult(c => SingleEncapsulateFieldResultAsync(document, span, index, true, c)), action1Text),
                new EncapsulateFieldCodeAction(new EncapsulateFieldResult(c => SingleEncapsulateFieldResultAsync(document, span, index, false, c)), action2Text)
            };
        }

        private async Task<Result> SingleEncapsulateFieldResultAsync(Document document, TextSpan span, int index, bool updateReferences, CancellationToken cancellationToken)
        {
            var fields = (await GetFieldsAsync(document, span, cancellationToken).ConfigureAwait(false)).ToImmutableArrayOrEmpty();
            Contract.Requires(fields.Length > index);

            var field = fields[index];
            var result = await EncapsulateFieldAsync(field, document, updateReferences, cancellationToken).ConfigureAwait(false);
            if (result == null)
            {
                return new Result(document.Project.Solution, field);
            }

            return result;
        }

        private async Task<Result> EncapsulateFieldResultAsync(Document document, TextSpan span, bool updateReferences, CancellationToken cancellationToken)
        {
            // probably later we want to add field and reason why it failed.
            var failedFieldSymbols = new List<IFieldSymbol>();

            var fields = await GetFieldsAsync(document, span, cancellationToken).ConfigureAwait(false);
            Contract.Requires(fields.Any());

            // For now, build up the multiple field case by encapsulating one at a time.
            Result result = null;
            foreach (var field in fields)
            {
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var compilation = semanticModel.Compilation;
                var currentField = field.GetSymbolKey().Resolve(compilation, cancellationToken: cancellationToken).Symbol as IFieldSymbol;

                // We couldn't resolve this field. skip it
                if (currentField == null)
                {
                    failedFieldSymbols.Add(field);
                    continue;
                }

                result = await EncapsulateFieldAsync(currentField, document, updateReferences, cancellationToken).ConfigureAwait(false);
                if (result == null)
                {
                    failedFieldSymbols.Add(field);
                    continue;
                }

                document = result.Solution.GetDocument(document.Id);
            }

            if (result == null)
            {
                return new Result(document.Project.Solution, fields.ToArray());
            }

            // add failed field symbol info
            return result.WithFailedFields(failedFieldSymbols);
        }

        private async Task<Result> EncapsulateFieldAsync(IFieldSymbol field, Document document, bool updateReferences, CancellationToken cancellationToken)
        {
            var originalField = field;
            var finalNames = GeneratePropertyAndFieldNames(field);
            var finalFieldName = finalNames.Item1;
            var generatedPropertyName = finalNames.Item2;

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
            field = field.GetSymbolKey().Resolve(compilation, cancellationToken: cancellationToken).Symbol as IFieldSymbol;

            var solutionNeedingProperty = solution;

            // We couldn't resolve field after annotating its declaration. Bail
            if (field == null)
            {
                return null;
            }

            solutionNeedingProperty = await UpdateReferencesAsync(
                updateReferences, solution, document, field, finalFieldName, generatedPropertyName, cancellationToken).ConfigureAwait(false);
            document = solutionNeedingProperty.GetDocument(document.Id);

            var markFieldPrivate = field.DeclaredAccessibility != Accessibility.Private;
            var rewrittenFieldDeclaration = await RewriteFieldNameAndAccessibility(finalFieldName, markFieldPrivate, document, declarationAnnotation, cancellationToken).ConfigureAwait(false);

            document = await Formatter.FormatAsync(document.WithSyntaxRoot(rewrittenFieldDeclaration), Formatter.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);

            solution = document.Project.Solution;
            foreach (var linkedDocumentId in document.GetLinkedDocumentIds())
            {
                var linkedDocument = solution.GetDocument(linkedDocumentId);
                var updatedLinkedRoot = await RewriteFieldNameAndAccessibility(finalFieldName, markFieldPrivate, linkedDocument, declarationAnnotation, cancellationToken).ConfigureAwait(false);
                var updatedLinkedDocument = await Formatter.FormatAsync(linkedDocument.WithSyntaxRoot(updatedLinkedRoot), Formatter.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);
                solution = updatedLinkedDocument.Project.Solution;
            }

            document = solution.GetDocument(document.Id);

            semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            compilation = semanticModel.Compilation;

            var newRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newDeclaration = newRoot.GetAnnotatedNodes<SyntaxNode>(declarationAnnotation).First();
            field = semanticModel.GetDeclaredSymbol(newDeclaration, cancellationToken) as IFieldSymbol;

            var generatedProperty = GenerateProperty(generatedPropertyName, finalFieldName, originalField.DeclaredAccessibility, originalField, field.ContainingType, new SyntaxAnnotation(), document, cancellationToken);

            var codeGenerationService = document.GetLanguageService<ICodeGenerationService>();
            var solutionWithProperty = await AddPropertyAsync(document, document.Project.Solution, field, generatedProperty, cancellationToken).ConfigureAwait(false);

            return new Result(solutionWithProperty, originalField.ToDisplayString(), originalField.GetGlyph());
        }

        private async Task<Solution> UpdateReferencesAsync(
            bool updateReferences, Solution solution, Document document, IFieldSymbol field, string finalFieldName, string generatedPropertyName, CancellationToken cancellationToken)
        {
            if (!updateReferences)
            {
                return solution;
            }

            if (field.IsReadOnly)
            {
                // Inside the constructor we want to rename references the field to the final field name.
                var constructorSyntaxes = GetConstructorNodes(field.ContainingType).ToSet();
                if (finalFieldName != field.Name && constructorSyntaxes.Count > 0)
                {
                    solution = await Renamer.RenameSymbolAsync(solution, field, finalFieldName, solution.Workspace.Options,
                        location => constructorSyntaxes.Any(c => c.Span.IntersectsWith(location.SourceSpan)), cancellationToken: cancellationToken).ConfigureAwait(false);
                    document = solution.GetDocument(document.Id);

                    var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

                    field = field.GetSymbolKey().Resolve(compilation, cancellationToken: cancellationToken).Symbol as IFieldSymbol;
                }

                // Outside the constructor we want to rename references to the field to final property name.
                return await Renamer.RenameSymbolAsync(solution, field, generatedPropertyName, solution.Workspace.Options,
                    location => !constructorSyntaxes.Any(c => c.Span.IntersectsWith(location.SourceSpan)), cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Just rename everything.
                return await Renamer.RenameSymbolAsync(solution, field, generatedPropertyName, solution.Workspace.Options, cancellationToken).ConfigureAwait(false);
            }
        }

        internal abstract IEnumerable<SyntaxNode> GetConstructorNodes(INamedTypeSymbol containingType);

        protected async Task<Solution> AddPropertyAsync(Document document, Solution destinationSolution, IFieldSymbol field, IPropertySymbol property, CancellationToken cancellationToken)
        {
            var codeGenerationService = document.GetLanguageService<ICodeGenerationService>();

            var fieldDeclaration = field.DeclaringSyntaxReferences.First();
            var options = new CodeGenerationOptions(contextLocation: fieldDeclaration.SyntaxTree.GetLocation(fieldDeclaration.Span));

            var destination = field.ContainingType;
            var updatedDocument = await codeGenerationService.AddPropertyAsync(destinationSolution, destination, property, options, cancellationToken)
                .ConfigureAwait(false);

            updatedDocument = await Formatter.FormatAsync(updatedDocument, Formatter.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);
            updatedDocument = await Simplifier.ReduceAsync(updatedDocument, cancellationToken: cancellationToken).ConfigureAwait(false);

            return updatedDocument.Project.Solution;
        }

        protected IPropertySymbol GenerateProperty(string propertyName, string fieldName, Accessibility accessibility, IFieldSymbol field, INamedTypeSymbol containingSymbol, SyntaxAnnotation annotation, Document document, CancellationToken cancellationToken)
        {
            var factory = document.GetLanguageService<SyntaxGenerator>();

            var propertySymbol = annotation.AddAnnotationToSymbol(CodeGenerationSymbolFactory.CreatePropertySymbol(containingType: containingSymbol,
                attributes: SpecializedCollections.EmptyList<AttributeData>(),
                accessibility: ComputeAccessibility(accessibility, field.Type),
                modifiers: new DeclarationModifiers(isStatic: field.IsStatic, isReadOnly: field.IsReadOnly, isUnsafe: field.IsUnsafe()),
                type: field.Type,
                explicitInterfaceSymbol: null,
                name: propertyName,
                parameters: SpecializedCollections.EmptyList<IParameterSymbol>(),
                getMethod: CreateGet(fieldName, field, factory),
                setMethod: field.IsReadOnly || field.IsConst ? null : CreateSet(fieldName, field, factory)));

            return Simplifier.Annotation.AddAnnotationToSymbol(
                Formatter.Annotation.AddAnnotationToSymbol(propertySymbol));
        }

        protected abstract Tuple<string, string> GeneratePropertyAndFieldNames(IFieldSymbol field);

        protected Accessibility ComputeAccessibility(Accessibility accessibility, ITypeSymbol type)
        {
            var computedAccessibility = accessibility;
            if (accessibility == Accessibility.NotApplicable || accessibility == Accessibility.Private)
            {
                computedAccessibility = Accessibility.Public;
            }

            var returnTypeAccessibility = type.DetermineMinimalAccessibility();

            return AccessibilityUtilities.Minimum(computedAccessibility, returnTypeAccessibility);
        }

        protected IMethodSymbol CreateSet(string originalFieldName, IFieldSymbol field, SyntaxGenerator factory)
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

            return CodeGenerationSymbolFactory.CreateAccessorSymbol(SpecializedCollections.EmptyList<AttributeData>(),
                Accessibility.NotApplicable,
                new[] { body }.ToList());
        }

        protected IMethodSymbol CreateGet(string originalFieldName, IFieldSymbol field, SyntaxGenerator factory)
        {
            var value = !field.IsStatic
                ? factory.MemberAccessExpression(
                    factory.ThisExpression(),
                    factory.IdentifierName(originalFieldName))
                : factory.IdentifierName(originalFieldName);

            var body = factory.ReturnStatement(
                value.WithAdditionalAnnotations(Simplifier.Annotation));

            return CodeGenerationSymbolFactory.CreateAccessorSymbol(SpecializedCollections.EmptyList<AttributeData>(),
                Accessibility.NotApplicable,
                new[] { body }.ToList());
        }

        private static readonly char[] s_underscoreCharArray = new[] { '_' };

        protected string GeneratePropertyName(string fieldName)
        {
            // Trim leading underscores
            var baseName = fieldName.TrimStart(s_underscoreCharArray);

            // Trim leading "m_"
            if (baseName.Length >= 2 && baseName[0] == 'm' && baseName[1] == '_')
            {
                baseName = baseName.Substring(2);
            }

            // Take original name if no characters left
            if (baseName.Length == 0)
            {
                baseName = fieldName;
            }

            // Make the first character upper case using the "en-US" culture.  See discussion at
            // https://github.com/dotnet/roslyn/issues/5524.
            var firstCharacter = CompletionRules.EnUSCultureInfo.TextInfo.ToUpper(baseName[0]);
            return firstCharacter.ToString() + baseName.Substring(1);
        }

        protected abstract Task<SyntaxNode> RewriteFieldNameAndAccessibility(string originalFieldName, bool makePrivate, Document document, SyntaxAnnotation declarationAnnotation, CancellationToken cancellationToken);
        protected abstract Task<IEnumerable<IFieldSymbol>> GetFieldsAsync(Document document, TextSpan span, CancellationToken cancellationToken);

        internal class Result
        {
            public Result(Solution solutionWithProperty, string name, Glyph glyph)
            {
                this.Solution = solutionWithProperty;
                this.Name = name;
                this.Glyph = glyph;
            }

            public Result(Solution solutionWithProperty, string name, Glyph glyph, List<IFieldSymbol> failedFieldSymbols) :
                this(solutionWithProperty, name, glyph)
            {
                this.FailedFields = failedFieldSymbols.ToImmutableArrayOrEmpty();
            }

            public Result(Solution originalSolution, params IFieldSymbol[] fields) :
                this(originalSolution, string.Empty, Glyph.Error)
            {
                this.FailedFields = fields.ToImmutableArrayOrEmpty();
            }

            public Solution Solution { get; }
            public string Name { get; }
            public Glyph Glyph { get; }
            public ImmutableArray<IFieldSymbol> FailedFields { get; }

            public Result WithFailedFields(List<IFieldSymbol> failedFieldSymbols)
            {
                if (failedFieldSymbols.Count == 0)
                {
                    return this;
                }

                return new Result(Solution, Name, Glyph, failedFieldSymbols);
            }
        }
    }
}
