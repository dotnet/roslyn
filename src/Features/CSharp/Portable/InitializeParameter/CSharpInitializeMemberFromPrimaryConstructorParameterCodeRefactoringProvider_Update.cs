﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.InitializeParameter;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.InitializeParameter;

using static InitializeParameterHelpers;
using static InitializeParameterHelpersCore;
using static SyntaxFactory;

internal sealed partial class CSharpInitializeMemberFromPrimaryConstructorParameterCodeRefactoringProvider
{
    private static async Task<Solution> AddMultipleMembersAsync(
        Document document,
        TypeDeclarationSyntax typeDeclaration,
        ImmutableArray<IParameterSymbol> parameters,
        ImmutableArray<ISymbol> fieldsOrProperties,
        CodeGenerationOptionsProvider fallbackOptions,
        CancellationToken cancellationToken)
    {
        Debug.Assert(parameters.Length >= 1);
        Debug.Assert(fieldsOrProperties.Length >= 1);
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

            currentSolution = await AddSingleMemberAsync(
                currentDocument,
                currentTypeDeclaration,
                currentParameter,
                fieldOrProperty,
                fallbackOptions,
                cancellationToken).ConfigureAwait(false);
        }

        return currentSolution;

        // Intentionally static so that we do not capture outer state.  This function is called in a loop with a fresh
        // fork of the solution after each change that has been made.  This ensures we don't accidentally refer to the
        // original state in any way.

        static async Task<Solution> AddSingleMemberAsync(
            Document document,
            TypeDeclarationSyntax typeDeclaration,
            IParameterSymbol parameter,
            ISymbol fieldOrProperty,
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

            // We're assigning the parameter to a new field/prop .  Convert all existing references to this primary
            // constructor parameter (within this type) to refer to the field/prop now instead.
            await UpdateParameterReferencesAsync(
                solutionEditor, parameter, fieldOrProperty, cancellationToken).ConfigureAwait(false);

            // We're generating a new field/property.  Place into the containing type, ideally before/after a
            // relevant existing member.
            var (sibling, siblingSyntax, addContext) = fieldOrProperty switch
            {
                IPropertySymbol => GetAddContext<IPropertySymbol>(compilation, parameter, cancellationToken),
                IFieldSymbol => GetAddContext<IFieldSymbol>(compilation, parameter, cancellationToken),
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

        static (ISymbol? symbol, SyntaxNode? syntax, CodeGenerationContext context) GetAddContext<TSymbol>(
            Compilation compilation, IParameterSymbol parameter, CancellationToken cancellationToken) where TSymbol : class, ISymbol
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
    }

    private static async Task UpdateParameterReferencesAsync(
        SolutionEditor solutionEditor,
        IParameterSymbol parameter,
        ISymbol fieldOrProperty,
        CancellationToken cancellationToken)
    {
        var solution = solutionEditor.OriginalSolution;
        var namedType = parameter.ContainingType;
        var documents = namedType.DeclaringSyntaxReferences
            .Select(r => solution.GetRequiredDocument(r.SyntaxTree))
            .ToImmutableHashSet();

        var references = await SymbolFinder.FindReferencesAsync(parameter, solution, documents, cancellationToken).ConfigureAwait(false);
        var groups = references.SelectMany(static r => r.Locations.Where(loc => !loc.IsImplicit)).GroupBy(static loc => loc.Document);

        foreach (var group in groups)
        {
            var editor = await solutionEditor.GetDocumentEditorAsync(group.Key.Id, cancellationToken).ConfigureAwait(false);

            // We may hit a location multiple times due to how we do FAR for linked symbols, but each linked symbol is
            // allowed to report the entire set of references it think it is compatible with.  So ensure we're hitting
            // each location only once.
            foreach (var location in group.Distinct(LinkedFileReferenceLocationEqualityComparer.Instance))
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

    private static async Task<Solution> UpdateExistingMemberAsync(
        Document document,
        IParameterSymbol parameter,
        ISymbol fieldOrProperty,
        CancellationToken cancellationToken)
    {
        var project = document.Project;
        var solution = project.Solution;

        var solutionEditor = new SolutionEditor(solution);
        var initializer = EqualsValueClause(IdentifierName(parameter.Name.EscapeIdentifier()));

        // We're assigning the parameter to a field/prop.  Convert all existing references to this primary constructor
        // parameter (within this type) to refer to the field/prop now instead.
        await UpdateParameterReferencesAsync(
            solutionEditor, parameter, fieldOrProperty, cancellationToken).ConfigureAwait(false);

        // We're updating an exiting field/prop.
        if (fieldOrProperty is IPropertySymbol property)
        {
            var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            var isThrowNotImplementedProperty = IsThrowNotImplementedProperty(compilation, property, cancellationToken);

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
                            .WithInitializer(initializer));
                    break;
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
                        variableDeclarator.WithInitializer(initializer));
                    break;
                }
            }
        }

        return solutionEditor.GetChangedSolution();
    }
}
