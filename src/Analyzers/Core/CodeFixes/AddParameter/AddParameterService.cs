// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.GenerateMember.GenerateConstructor;
using Microsoft.CodeAnalysis.InitializeParameter;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddParameter;

internal static class AddParameterService
{
    private static readonly SyntaxAnnotation s_annotation = new();

    /// <summary>
    /// Checks if there are indications that there might be more than one declarations that need to be fixed.
    /// The check does not look-up if there are other declarations (this is done later in the CodeAction).
    /// </summary>
    public static bool HasCascadingDeclarations(IMethodSymbol method)
    {
        // Don't cascade constructors
        if (method.IsConstructor())
        {
            return false;
        }

        // Virtual methods of all kinds might have overrides somewhere else that need to be fixed.
        if (method.IsVirtual || method.IsOverride || method.IsAbstract)
        {
            return true;
        }

        // If interfaces are involved we will fix those too
        // Explicit interface implementations are easy to detect
        if (method.ExplicitInterfaceImplementations.Length > 0)
        {
            return true;
        }

        // For implicit interface implementations lets check if the characteristic of the method
        // allows it to implicit implement an interface member.
        if (method.DeclaredAccessibility != Accessibility.Public)
        {
            return false;
        }

        if (method.IsStatic)
        {
            return false;
        }

        // Now check if the method does implement an interface member
        if (method.ExplicitOrImplicitInterfaceImplementations().Length > 0)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Adds a parameter to a method.
    /// </summary>
    /// <param name="newParameterIndex"><see langword="null"/> to add as the final parameter</param>
    /// <returns></returns>
    public static async Task<Solution> AddParameterAsync<TExpressionSyntax>(
        Document invocationDocument,
        IMethodSymbol method,
        ITypeSymbol newParameterType,
        RefKind refKind,
        ParameterName parameterName,
        Argument<TExpressionSyntax>? argument,
        int? newParameterIndex,
        bool fixAllReferences,
        CancellationToken cancellationToken)
        where TExpressionSyntax : SyntaxNode
    {
        var solution = invocationDocument.Project.Solution;

        var referencedSymbols = fixAllReferences
            ? await FindMethodDeclarationReferencesAsync(invocationDocument, method, cancellationToken).ConfigureAwait(false)
            : method.GetAllMethodSymbolsOfPartialParts();

        var anySymbolReferencesNotInSource = referencedSymbols.Any(static symbol => !symbol.IsFromSource());
        var locationsInSource = referencedSymbols.Where(symbol => symbol.IsFromSource());

        // Indexing Locations[0] is valid because IMethodSymbols have one location at most
        // and IsFromSource() tests if there is at least one location.
        var locationsByDocument = locationsInSource.ToLookup(
            declarationLocation => solution.GetRequiredDocument(declarationLocation.Locations[0].SourceTree!));

        foreach (var documentLookup in locationsByDocument)
        {
            var document = documentLookup.Key;

            // May not have syntax facts for a different language in CodeStyle layer.
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            if (syntaxFacts is null)
                continue;

            var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var syntaxRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var editor = new SyntaxEditor(syntaxRoot, solution.Services);
            var generator = editor.Generator;
            foreach (var currentMethodToUpdate in documentLookup)
            {
                var methodNode = syntaxRoot.FindNode(currentMethodToUpdate.Locations[0].SourceSpan, getInnermostNodeForTie: true);
                var existingParameters = generator.GetParameters(methodNode);
                var insertionIndex = newParameterIndex ?? existingParameters.Count;

                // if the preceding parameter is optional, the new parameter must also be optional 
                // see also BC30202 and CS1737
                var parameterMustBeOptional = insertionIndex > 0 &&
                    syntaxFacts.GetDefaultOfParameter(existingParameters[insertionIndex - 1]) != null;

                var parameterSymbol = CreateParameterSymbol(
                    currentMethodToUpdate, newParameterType, refKind, parameterMustBeOptional, parameterName.BestNameForParameter);

                var argumentInitializer = parameterMustBeOptional ? generator.DefaultExpression(newParameterType) : null;
                var parameterDeclaration = generator
                    .ParameterDeclaration(parameterSymbol, argumentInitializer)
                    .WithAdditionalAnnotations(Formatter.Annotation, s_annotation);

                if (anySymbolReferencesNotInSource && currentMethodToUpdate == method)
                {
                    parameterDeclaration = parameterDeclaration.WithAdditionalAnnotations(
                        ConflictAnnotation.Create(CodeFixesResources.Related_method_signatures_found_in_metadata_will_not_be_updated));
                }

                if (method.MethodKind == MethodKind.ReducedExtension && insertionIndex < existingParameters.Count)
                    insertionIndex++;

                AddParameterEditor.AddParameter(
                    syntaxFacts, editor, methodNode, insertionIndex, parameterDeclaration, cancellationToken);
            }

            var newRoot = editor.GetChangedRoot();
            solution = solution.WithDocumentSyntaxRoot(document.Id, newRoot);
        }

        // Now that we've added the parameter to the method, see if we added to a constructor that we then want to
        // assign that parameter to a field/property to as well.
        solution = await AddConstructorAssignmentsAsync(solution).ConfigureAwait(false);

        return solution;

        async Task<Solution> AddConstructorAssignmentsAsync(Solution rewrittenSolution)
        {
            var finalSolution = await TryAddConstructorAssignmentsAsync(rewrittenSolution).ConfigureAwait(false);
            return finalSolution ?? rewrittenSolution;
        }

        async Task<Solution?> TryAddConstructorAssignmentsAsync(Solution rewrittenSolution)
        {
            // If we weren't adding a parameter to a constructor, we have nothing to do here.
            if (method.MethodKind != MethodKind.Constructor)
                return null;

            // If we didn't have an argument indicating what was passed to the constructor, then we have nothing to do.
            if (argument is null)
                return null;

            // Only want to do this if we updated a single constructor in a single document.
            var documentsUpdated = locationsByDocument.Select(g => g.Key).ToSet();
            if (documentsUpdated.Count != 1)
                return null;

            var documentId = documentsUpdated.Single().Id;

            var memberToAssignTo = await GetMemberToAssignToAsync(documentId).ConfigureAwait(false);
            if (memberToAssignTo is null)
                return null;

            // Now go find the constructor after the parameter was added to it.
            var rewrittenDocument = rewrittenSolution.GetRequiredDocument(documentId);
            var rewrittenSyntaxRoot = await rewrittenDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var parameterDeclaration = rewrittenSyntaxRoot.GetAnnotatedNodes(s_annotation).SingleOrDefault();
            if (parameterDeclaration is null)
                return null;

            var initializeParameterService = rewrittenDocument.GetRequiredLanguageService<IInitializeParameterService>();
            var semanticModel = await rewrittenDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel.GetDeclaredSymbol(parameterDeclaration, cancellationToken) is not IParameterSymbol parameter)
                return null;

            if (parameter.ContainingSymbol is not IMethodSymbol { MethodKind: MethodKind.Constructor, DeclaringSyntaxReferences: [var reference] })
                return null;

            var methodNode = reference.GetSyntax(cancellationToken);
            var body = initializeParameterService.GetBody(methodNode);
            if (semanticModel.GetOperation(body, cancellationToken) is not IBlockOperation blockOperation)
                return rewrittenSolution;

            var editor = new SyntaxEditor(rewrittenSyntaxRoot, rewrittenSolution.Services);
            initializeParameterService.AddAssignment(
                methodNode, blockOperation, parameter, memberToAssignTo, editor);

            var finalDocument = rewrittenDocument.WithSyntaxRoot(editor.GetChangedRoot());
            return finalDocument.Project.Solution;
        }

        async Task<ISymbol?> GetMemberToAssignToAsync(DocumentId documentId)
        {
            var constructorDocument = invocationDocument.Project.Solution.GetRequiredDocument(documentId);
            var constructorSemanticDocument = await SemanticDocument.CreateAsync(constructorDocument, cancellationToken).ConfigureAwait(false);

            var (_, parameterToExistingMember, _, _) = await GenerateConstructorHelpers.GetParametersAsync(
                constructorSemanticDocument,
                method.ContainingType,
                [argument.Value],
                [newParameterType],
                [parameterName],
                cancellationToken).ConfigureAwait(false);

            return parameterToExistingMember.FirstOrDefault().Value;
        }
    }

    private static async Task<ImmutableArray<IMethodSymbol>> FindMethodDeclarationReferencesAsync(
        Document invocationDocument, IMethodSymbol method, CancellationToken cancellationToken)
    {
        var referencedSymbols = await SymbolFinder.FindReferencesAsync(
            method, invocationDocument.Project.Solution, cancellationToken).ConfigureAwait(false);

        return referencedSymbols.Select(referencedSymbol => referencedSymbol.Definition)
                                .OfType<IMethodSymbol>()
                                .Distinct()
                                .ToImmutableArray();
    }

    private static IParameterSymbol CreateParameterSymbol(
        IMethodSymbol method,
        ITypeSymbol parameterType,
        RefKind refKind,
        bool isOptional,
        string argumentNameSuggestion)
    {
        var uniqueName = NameGenerator.EnsureUniqueness(argumentNameSuggestion, method.Parameters.Select(p => p.Name));
        var newParameterSymbol = CodeGenerationSymbolFactory.CreateParameterSymbol(
                attributes: default, refKind: refKind, isOptional: isOptional, isParams: false, type: parameterType, name: uniqueName);
        return newParameterSymbol;
    }
}
