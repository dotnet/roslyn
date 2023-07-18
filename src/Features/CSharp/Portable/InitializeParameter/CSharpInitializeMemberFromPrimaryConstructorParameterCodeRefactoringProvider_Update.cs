// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.InitializeParameter;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.InitializeParameter
{
    using static InitializeParameterHelpers;
    using static InitializeParameterHelpersCore;
    using static SyntaxFactory;

    internal sealed partial class CSharpInitializeMemberFromPrimaryConstructorParameterCodeRefactoringProvider : CodeRefactoringProvider
    {
        /// This functions are the workhorses that actually go and handle each parameter (either creating a
        /// field/property for it, or updates an existing field/property with it).  They are extracted out from the rest
        /// as we do not want it capturing anything.  Specifically, AddSingleSymbolInitializationAsync is called in a
        /// loop from AddAllSymbolInitializationsAsync for each parameter we're processing.  For each, we produce new
        /// solution snapshots and thus must ensure we're always pointing at the new view of the world, not anything
        /// from the original view.

        private static async Task<Solution> AddAllSymbolInitializationsAsync(
            Document document,
            TypeDeclarationSyntax typeDeclaration,
            ImmutableArray<IParameterSymbol> parameters,
            ImmutableArray<ISymbol> fieldsOrProperties,
            CodeGenerationOptionsProvider fallbackOptions,
            CancellationToken cancellationToken)
        {
            Debug.Assert(parameters.Length >= 2);
            Debug.Assert(fieldsOrProperties.Length > 0);
            Debug.Assert(parameters.Length == fieldsOrProperties.Length);

            // Process each param+field/prop in order.  Apply the pair to the document getting the updated document.
            // Then find all the current data in that updated document and move onto the next pair.

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var trackedRoot = root.TrackNodes(typeDeclaration);
            var currentSolution = document.WithSyntaxRoot(trackedRoot).Project.Solution;

            foreach (var (parameter, fieldOrProperty) in parameters.Zip(fieldsOrProperties, static (a, b) => (a, b)))
            {
                var currentDocument = currentSolution.GetRequiredDocument(document.Id);
                var currentCompilation = await currentDocument.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                var currentRoot = await currentDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var currentTypeDeclaration = currentRoot.GetCurrentNode(typeDeclaration);
                if (currentTypeDeclaration == null)
                    continue;

                var currentParameter = (IParameterSymbol?)parameter.GetSymbolKey(cancellationToken).Resolve(currentCompilation, cancellationToken: cancellationToken).GetAnySymbol();
                if (currentParameter == null)
                    continue;

                // fieldOrProperty is a new member.  So we don't have to track it to this edit we're making.

                currentSolution = await AddSingleSymbolInitializationAsync(
                    currentDocument,
                    currentTypeDeclaration,
                    currentParameter,
                    fieldOrProperty,
                    isThrowNotImplementedProperty: false,
                    fallbackOptions,
                    cancellationToken).ConfigureAwait(false);
            }

            return currentSolution;
        }

        private static async Task<Solution> AddSingleSymbolInitializationAsync(
            Document document,
            TypeDeclarationSyntax typeDeclaration,
            IParameterSymbol parameter,
            ISymbol fieldOrProperty,
            bool isThrowNotImplementedProperty,
            CodeGenerationOptionsProvider fallbackOptions,
            CancellationToken cancellationToken)
        {
            var project = document.Project;
            var solution = project.Solution;
            var services = solution.Services;

            var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            var parseOptions = document.DocumentState.ParseOptions!;

            var solutionEditor = new SolutionEditor(solution);
            var options = await document.GetCodeGenerationOptionsAsync(fallbackOptions, cancellationToken).ConfigureAwait(false);
            var codeGenerator = document.GetRequiredLanguageService<ICodeGenerationService>();

            // We're assigning the parameter to a field/prop (either new or existing).  Convert all existing references
            // to this primary constructor parameter (within this type) to refer to the field/prop now instead.
            await UpdateParameterReferencesAsync().ConfigureAwait(false);

            // Now, either add the new field/prop, or update the existing one by assigning the parameter to it.
            return fieldOrProperty.ContainingType == null
                ? await AddFieldOrPropertyAsync().ConfigureAwait(false)
                : await UpdateFieldOrPropertyAsync().ConfigureAwait(false);

            async Task UpdateParameterReferencesAsync()
            {
                var namedType = parameter.ContainingType;
                var documents = namedType.DeclaringSyntaxReferences
                    .Select(r => solution.GetRequiredDocument(r.SyntaxTree))
                    .ToImmutableHashSet();

                var references = await SymbolFinder.FindReferencesAsync(parameter, solution, documents, cancellationToken).ConfigureAwait(false);
                foreach (var group in references.SelectMany(r => r.Locations.Where(loc => !loc.IsImplicit).GroupBy(loc => loc.Document)))
                {
                    var editor = await solutionEditor.GetDocumentEditorAsync(group.Key.Id, cancellationToken).ConfigureAwait(false);
                    foreach (var location in group)
                    {
                        var node = location.Location.FindNode(getInnermostNodeForTie: true, cancellationToken);
                        if (node is IdentifierNameSyntax { Parent: not NameColonSyntax } identifierName &&
                            identifierName.Identifier.ValueText == parameter.Name)
                        {
                            // we may have things like `new MyType(x: ...)` we don't want to update `x` there to 'X'
                            // just because we're generating a new property 'X' for the parameter to be assigned to.
                            editor.ReplaceNode(
                                identifierName,
                                IdentifierName(fieldOrProperty.Name.EscapeIdentifier()).WithTriviaFrom(identifierName));
                        }
                    }
                }
            }

            async ValueTask<Solution> AddFieldOrPropertyAsync()
            {
                // We're generating a new field/property.  Place into the containing type, ideally before/after a
                // relevant existing member.
                var (sibling, siblingSyntax, addContext) = fieldOrProperty switch
                {
                    IPropertySymbol => GetAddContext<IPropertySymbol>(),
                    IFieldSymbol => GetAddContext<IFieldSymbol>(),
                    _ => throw ExceptionUtilities.UnexpectedValue(fieldOrProperty),
                };

                var preferredTypeDeclaration = siblingSyntax?.GetAncestorOrThis<TypeDeclarationSyntax>() ?? typeDeclaration;

                var editingDocument = solution.GetRequiredDocument(preferredTypeDeclaration.SyntaxTree);
                var editor = await solutionEditor.GetDocumentEditorAsync(editingDocument.Id, cancellationToken).ConfigureAwait(false);
                editor.ReplaceNode(
                    preferredTypeDeclaration,
                    (currentTypeDecl, _) =>
                    {
                        if (fieldOrProperty is IPropertySymbol property)
                        {
                            return codeGenerator.AddProperty(
                                currentTypeDecl, property,
                                codeGenerator.GetInfo(addContext, options, parseOptions),
                                cancellationToken);
                        }
                        else if (fieldOrProperty is IFieldSymbol field)
                        {
                            return codeGenerator.AddField(
                                currentTypeDecl, field,
                                codeGenerator.GetInfo(addContext, options, parseOptions),
                                cancellationToken);
                        }
                        else
                        {
                            throw ExceptionUtilities.Unreachable();
                        }
                    });

                return solutionEditor.GetChangedSolution();
            }

            (ISymbol? symbol, SyntaxNode? syntax, CodeGenerationContext context) GetAddContext<TSymbol>() where TSymbol : class, ISymbol
            {
                foreach (var (sibling, before) in GetSiblingParameters(parameter))
                {
                    var (initializer, fieldOrProperty) = TryFindFieldOrPropertyInitializerValue(
                        compilation, sibling, cancellationToken);

                    if (initializer != null &&
                        fieldOrProperty is TSymbol { DeclaringSyntaxReferences: [var syntaxReference, ..] } symbol)
                    {
                        var syntax = syntaxReference.GetSyntax(cancellationToken);
                        return (symbol, syntax, before
                            ? new CodeGenerationContext(afterThisLocation: syntax.GetLocation())
                            : new CodeGenerationContext(beforeThisLocation: syntax.GetLocation()));
                    }
                }

                return (symbol: null, syntax: null, CodeGenerationContext.Default);
            }

            async ValueTask<Solution> UpdateFieldOrPropertyAsync()
            {
                // We're updating an exiting field/prop.
                if (fieldOrProperty is IPropertySymbol property)
                {
                    foreach (var syntaxRef in property.DeclaringSyntaxReferences)
                    {
                        if (syntaxRef.GetSyntax(cancellationToken) is PropertyDeclarationSyntax propertyDeclaration)
                        {
                            var editingDocument = solution.GetRequiredDocument(propertyDeclaration.SyntaxTree);
                            var editor = await solutionEditor.GetDocumentEditorAsync(editingDocument.Id, cancellationToken).ConfigureAwait(false);

                            // If the user had a property that has 'throw NotImplementedException' in it, then remove those throws.
                            var newPropertyDeclaration = isThrowNotImplementedProperty ? RemoveThrowNotImplemented(propertyDeclaration) : propertyDeclaration;
                            editor.ReplaceNode(
                                propertyDeclaration,
                                newPropertyDeclaration.WithoutTrailingTrivia()
                                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken).WithTrailingTrivia(newPropertyDeclaration.GetTrailingTrivia()))
                                    .WithInitializer(EqualsValueClause(IdentifierName(parameter.Name.EscapeIdentifier()))));
                        }
                    }
                }
                else if (fieldOrProperty is IFieldSymbol field)
                {
                    foreach (var syntaxRef in field.DeclaringSyntaxReferences)
                    {
                        if (syntaxRef.GetSyntax(cancellationToken) is VariableDeclaratorSyntax variableDeclarator)
                        {
                            var editingDocument = solution.GetRequiredDocument(variableDeclarator.SyntaxTree);
                            var editor = await solutionEditor.GetDocumentEditorAsync(editingDocument.Id, cancellationToken).ConfigureAwait(false);
                            editor.ReplaceNode(
                                variableDeclarator,
                                variableDeclarator.WithInitializer(EqualsValueClause(IdentifierName(parameter.Name.EscapeIdentifier()))));
                            break;
                        }
                    }
                }
                else
                {
                    throw ExceptionUtilities.Unreachable();
                }

                return solutionEditor.GetChangedSolution();
            }
        }
    }
}
