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
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.AddParameter
{
    internal static class AddParameterService
    {
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
        public static async Task<Solution> AddParameterAsync(
            Document invocationDocument,
            IMethodSymbol method,
            ITypeSymbol newParameterType,
            RefKind refKind,
            string parameterName,
            int? newParameterIndex,
            bool fixAllReferences,
            CancellationToken cancellationToken)
        {
            var solution = invocationDocument.Project.Solution;

            var referencedSymbols = fixAllReferences
                ? await FindMethodDeclarationReferencesAsync(invocationDocument, method, cancellationToken).ConfigureAwait(false)
                : method.GetAllMethodSymbolsOfPartialParts();

            var anySymbolReferencesNotInSource = referencedSymbols.Any(symbol => !symbol.IsFromSource());
            var locationsInSource = referencedSymbols.Where(symbol => symbol.IsFromSource());

            // Indexing Locations[0] is valid because IMethodSymbols have one location at most
            // and IsFromSource() tests if there is at least one location.
            var locationsByDocument = locationsInSource.ToLookup(declarationLocation
                => solution.GetRequiredDocument(declarationLocation.Locations[0].SourceTree!));

            foreach (var documentLookup in locationsByDocument)
            {
                var document = documentLookup.Key;
                var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
                var syntaxRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var editor = new SyntaxEditor(syntaxRoot, solution.Workspace.Services);
                var generator = editor.Generator;
                foreach (var methodDeclaration in documentLookup)
                {
                    var methodNode = syntaxRoot.FindNode(methodDeclaration.Locations[0].SourceSpan, getInnermostNodeForTie: true);
                    var existingParameters = generator.GetParameters(methodNode);
                    var insertionIndex = newParameterIndex ?? existingParameters.Count;

                    // if the preceding parameter is optional, the new parameter must also be optional 
                    // see also BC30202 and CS1737
                    var parameterMustBeOptional = insertionIndex > 0 &&
                        syntaxFacts.GetDefaultOfParameter(existingParameters[insertionIndex - 1]) != null;

                    var parameterSymbol = CreateParameterSymbol(
                        methodDeclaration, newParameterType, refKind, parameterMustBeOptional, parameterName);

                    var argumentInitializer = parameterMustBeOptional ? generator.DefaultExpression(newParameterType) : null;
                    var parameterDeclaration = generator.ParameterDeclaration(parameterSymbol, argumentInitializer)
                                                        .WithAdditionalAnnotations(Formatter.Annotation);
                    if (anySymbolReferencesNotInSource && methodDeclaration == method)
                    {
                        parameterDeclaration = parameterDeclaration.WithAdditionalAnnotations(
                            ConflictAnnotation.Create(FeaturesResources.Related_method_signatures_found_in_metadata_will_not_be_updated));
                    }

                    if (method.MethodKind == MethodKind.ReducedExtension && insertionIndex < existingParameters.Count)
                    {
                        insertionIndex++;
                    }

                    AddParameterEditor.AddParameter(syntaxFacts, editor, methodNode, insertionIndex, parameterDeclaration, cancellationToken);
                }

                var newRoot = editor.GetChangedRoot();
                solution = solution.WithDocumentSyntaxRoot(document.Id, newRoot);
            }

            return solution;
        }

        private static async Task<ImmutableArray<IMethodSymbol>> FindMethodDeclarationReferencesAsync(
            Document invocationDocument, IMethodSymbol method, CancellationToken cancellationToken)
        {
            var progress = new StreamingProgressCollector();

            await SymbolFinder.FindReferencesAsync(
                method, invocationDocument.Project.Solution, progress: progress,
                documents: null, FindReferencesSearchOptions.Default, cancellationToken).ConfigureAwait(false);
            var referencedSymbols = progress.GetReferencedSymbols();
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
}
