// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
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
    internal class AddParameterService : IAddParameterService
    {
        private AddParameterService()
        {
        }

        public static AddParameterService Instance = new AddParameterService();

        public bool HasCascadingDeclarations(IMethodSymbol method)
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

        public async Task<Solution> AddParameterAsync(
            Document invocationDocument,
            IMethodSymbol method,
            ITypeSymbol newParamaterType,
            RefKind refKind,
            string parameterName,
            int? newParameterIndex,
            bool fixAllReferences,
            CancellationToken cancellationToken)
        {
            var solution = invocationDocument.Project.Solution;

            var referencedSymbols = fixAllReferences
                ? await FindMethodDeclarationReferences(invocationDocument, method, cancellationToken).ConfigureAwait(false)
                : method.GetAllMethodSymbolsOfPartialParts();

            var anySymbolReferencesNotInSource = referencedSymbols.Any(symbol => !symbol.IsFromSource());
            var locationsInSource = referencedSymbols.Where(symbol => symbol.IsFromSource());

            // Indexing Locations[0] is valid because IMethodSymbols have one location at most
            // and IsFromSource() tests if there is at least one location.
            var locationsByDocument = locationsInSource.ToLookup(declarationLocation
                => solution.GetDocument(declarationLocation.Locations[0].SourceTree));

            foreach (var documentLookup in locationsByDocument)
            {
                var document = documentLookup.Key;
                var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
                var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var editor = new SyntaxEditor(syntaxRoot, solution.Workspace);
                var generator = editor.Generator;
                foreach (var methodDeclaration in documentLookup)
                {
                    var methodNode = syntaxRoot.FindNode(methodDeclaration.Locations[0].SourceSpan);
                    var existingParameters = generator.GetParameters(methodNode);
                    var insertionIndex = newParameterIndex ?? existingParameters.Count;

                    // if the preceding parameter is optional, the new parameter must also be optional 
                    // see also BC30202 and CS1737
                    var parameterMustBeOptional = insertionIndex > 0 &&
                        syntaxFacts.GetDefaultOfParameter(existingParameters[insertionIndex - 1]) != null;

                    var parameterSymbol = CreateParameterSymbol(
                        methodDeclaration, newParamaterType, refKind, parameterMustBeOptional, parameterName);

                    var argumentInitializer = parameterMustBeOptional ? generator.DefaultExpression(newParamaterType) : null;
                    var parameterDeclaration = generator.ParameterDeclaration(parameterSymbol, argumentInitializer)
                                                        .WithAdditionalAnnotations(Formatter.Annotation);
                    if (anySymbolReferencesNotInSource && methodDeclaration == method)
                    {
                        parameterDeclaration = parameterDeclaration.WithAdditionalAnnotations(
                            ConflictAnnotation.Create(FeaturesResources.Related_method_signatures_found_in_metadata_will_not_be_updated));
                    }


                    if (method.MethodKind == MethodKind.ReducedExtension)
                    {
                        insertionIndex++;
                    }

                    AddParameter(syntaxFacts, editor, methodNode, insertionIndex, parameterDeclaration, cancellationToken);
                }

                var newRoot = editor.GetChangedRoot();
                solution = solution.WithDocumentSyntaxRoot(document.Id, newRoot);
            }

            return solution;
        }

        private static async Task<ImmutableArray<IMethodSymbol>> FindMethodDeclarationReferences(
            Document invocationDocument, IMethodSymbol method, CancellationToken cancellationToken)
        {
            var progress = new StreamingProgressCollector(StreamingFindReferencesProgress.Instance);

            await SymbolFinder.FindReferencesAsync(
                symbolAndProjectId: SymbolAndProjectId.Create(method, invocationDocument.Project.Id),
                solution: invocationDocument.Project.Solution,
                documents: null,
                progress: progress,
                options: FindReferencesSearchOptions.Default,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            var referencedSymbols = progress.GetReferencedSymbols();
            return referencedSymbols.Select(referencedSymbol => referencedSymbol.Definition)
                                    .OfType<IMethodSymbol>()
                                    .Distinct()
                                    .ToImmutableArray();
        }

        private IParameterSymbol CreateParameterSymbol(
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

        private static void AddParameter(
            ISyntaxFactsService syntaxFacts,
            SyntaxEditor editor,
            SyntaxNode declaration,
            int insertionIndex,
            SyntaxNode parameterDeclaration,
            CancellationToken cancellationToken)
        {
            var sourceText = declaration.SyntaxTree.GetText(cancellationToken);
            var generator = editor.Generator;

            var existingParameters = generator.GetParameters(declaration);
            var placeOnNewLine = ShouldPlaceParametersOnNewLine(existingParameters, cancellationToken);

            if (!placeOnNewLine)
            {
                // Trivial case.  Just let the stock editor impl handle this for us.
                editor.InsertParameter(declaration, insertionIndex, parameterDeclaration);
                return;
            }

            if (insertionIndex == existingParameters.Count)
            {
                // Placing the last parameter on its own line.  Get the indentation of the 
                // curent last parameter and give the new last parameter the same indentation.
                var leadingIndentation = GetDesiredLeadingIndentation(
                    generator, syntaxFacts, existingParameters[existingParameters.Count - 1], includeLeadingNewLine: true);
                parameterDeclaration = parameterDeclaration.WithPrependedLeadingTrivia(leadingIndentation)
                                                           .WithAdditionalAnnotations(Formatter.Annotation);

                editor.AddParameter(declaration, parameterDeclaration);
            }
            else if (insertionIndex == 0)
            {
                // Inserting into the start of the list.  The existing first parameter might
                // be on the same line as the parameter list, or it might be on the next line.
                var firstParameter = existingParameters[0];
                var previousToken = firstParameter.GetFirstToken().GetPreviousToken();

                if (sourceText.AreOnSameLine(previousToken, firstParameter.GetFirstToken()))
                {
                    // First parameter is on hte same line as the method.  

                    // We want to insert the parameter at the front of the existing parameter
                    // list.  That means we need to move the current first parameter to a new
                    // line.  Give the current first parameter the indentation of the second
                    // parameter in the list.
                    editor.InsertParameter(declaration, insertionIndex, parameterDeclaration);
                    var nextParameter = existingParameters[insertionIndex];

                    var nextLeadingIndentation = GetDesiredLeadingIndentation(
                        generator, syntaxFacts, existingParameters[insertionIndex + 1], includeLeadingNewLine: true);
                    editor.ReplaceNode(
                        nextParameter,
                        nextParameter.WithPrependedLeadingTrivia(nextLeadingIndentation)
                                     .WithAdditionalAnnotations(Formatter.Annotation));
                }
                else
                {
                    // First parameter is on its own line.  No need to adjust its indentation.
                    // Just copy its indentation over to the parameter we're inserting, and
                    // make sure the current first parameter gets a newline so it stays on 
                    // its own line.

                    // We want to insert the parameter at the front of the existing parameter
                    // list.  That means we need to move the current first parameter to a new
                    // line.  Give the current first parameter the indentation of the second
                    // parameter in the list.
                    var firstLeadingIndentation = GetDesiredLeadingIndentation(
                        generator, syntaxFacts, existingParameters[0], includeLeadingNewLine: false);

                    editor.InsertParameter(declaration, insertionIndex,
                        parameterDeclaration.WithLeadingTrivia(firstLeadingIndentation));
                    var nextParameter = existingParameters[insertionIndex];

                    editor.ReplaceNode(
                        nextParameter,
                        nextParameter.WithPrependedLeadingTrivia(generator.ElasticCarriageReturnLineFeed)
                                     .WithAdditionalAnnotations(Formatter.Annotation));
                }
            }
            else
            {
                // We're inserting somewhere after the start (but not at the end). Because 
                // we've set placeOnNewLine, we know that the current comma we'll be placed
                // after already have a newline following it.  So all we need for this new 
                // parameter is to get the indentation of the following parameter.
                // Because we're going to 'steal' the existing comma from that parameter,
                // ensure that the next parameter has a new-line added to it so that it will
                // still stay on a new line.
                var nextParameter = existingParameters[insertionIndex];
                var leadingIndentation = GetDesiredLeadingIndentation(
                    generator, syntaxFacts, existingParameters[insertionIndex], includeLeadingNewLine: false);
                parameterDeclaration = parameterDeclaration.WithPrependedLeadingTrivia(leadingIndentation);

                editor.InsertParameter(declaration, insertionIndex, parameterDeclaration);
                editor.ReplaceNode(
                    nextParameter,
                    nextParameter.WithPrependedLeadingTrivia(generator.ElasticCarriageReturnLineFeed)
                                 .WithAdditionalAnnotations(Formatter.Annotation));
            }
        }

        private static List<SyntaxTrivia> GetDesiredLeadingIndentation(
            SyntaxGenerator generator, ISyntaxFactsService syntaxFacts,
            SyntaxNode node, bool includeLeadingNewLine)
        {
            var triviaList = new List<SyntaxTrivia>();
            if (includeLeadingNewLine)
            {
                triviaList.Add(generator.ElasticCarriageReturnLineFeed);
            }

            var lastWhitespace = default(SyntaxTrivia);
            foreach (var trivia in node.GetLeadingTrivia().Reverse())
            {
                if (syntaxFacts.IsWhitespaceTrivia(trivia))
                {
                    lastWhitespace = trivia;
                }
                else if (syntaxFacts.IsEndOfLineTrivia(trivia))
                {
                    break;
                }
            }

            if (lastWhitespace.RawKind != 0)
            {
                triviaList.Add(lastWhitespace);
            }

            return triviaList;
        }

        private static bool ShouldPlaceParametersOnNewLine(
            IReadOnlyList<SyntaxNode> parameters, CancellationToken cancellationToken)
        {
            if (parameters.Count <= 1)
            {
                return false;
            }

            var text = parameters[0].SyntaxTree.GetText(cancellationToken);
            for (int i = 1, n = parameters.Count; i < n; i++)
            {
                var lastParameter = parameters[i - 1];
                var thisParameter = parameters[i];

                if (text.AreOnSameLine(lastParameter.GetLastToken(), thisParameter.GetFirstToken()))
                {
                    return false;
                }
            }

            // All parameters are on different lines.  Place the new parameter on a new line as well.
            return true;
        }
    }
}
