// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddParameter
{
#pragma warning disable RS1016 // Code fix providers should provide FixAll support. https://github.com/dotnet/roslyn/issues/23528
    internal abstract class AbstractAddParameterCodeFixProvider<
#pragma warning restore RS1016 // Code fix providers should provide FixAll support.
        TArgumentSyntax,
        TAttributeArgumentSyntax,
        TArgumentListSyntax,
        TAttributeArgumentListSyntax,
        TInvocationExpressionSyntax,
        TObjectCreationExpressionSyntax> : CodeFixProvider
        where TArgumentSyntax : SyntaxNode
        where TArgumentListSyntax : SyntaxNode
        where TAttributeArgumentListSyntax : SyntaxNode
        where TInvocationExpressionSyntax : SyntaxNode
        where TObjectCreationExpressionSyntax : SyntaxNode
    {
        protected abstract ImmutableArray<string> TooManyArgumentsDiagnosticIds { get; }
        protected abstract ImmutableArray<string> CannotConvertDiagnosticIds { get; }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var cancellationToken = context.CancellationToken;
            var diagnostic = context.Diagnostics.First();

            var document = context.Document;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var initialNode = root.FindNode(diagnostic.Location.SourceSpan);

            for (var node = initialNode; node != null; node = node.Parent)
            {
                if (node is TObjectCreationExpressionSyntax objectCreation)
                {
                    var argumentOpt = TryGetRelevantArgument(initialNode, node, diagnostic);
                    await HandleObjectCreationExpressionAsync(context, objectCreation, argumentOpt).ConfigureAwait(false);
                    return;
                }
                else if (node is TInvocationExpressionSyntax invocationExpression)
                {
                    var argumentOpt = TryGetRelevantArgument(initialNode, node, diagnostic);
                    await HandleInvocationExpressionAsync(context, invocationExpression, argumentOpt).ConfigureAwait(false);
                    return;
                }
            }
        }

        /// <summary>
        /// If the diagnostic is on a argument, the argument is considered to be the argument to fix.
        /// There are some exceptions to this rule. Returning null indicates that the fixer needs
        /// to find the relevant argument by itself.
        /// </summary>
        private TArgumentSyntax TryGetRelevantArgument(
            SyntaxNode initialNode, SyntaxNode node, Diagnostic diagnostic)
        {
            if (this.TooManyArgumentsDiagnosticIds.Contains(diagnostic.Id))
            {
                return null;
            }

            if (this.CannotConvertDiagnosticIds.Contains(diagnostic.Id))
            {
                return null;
            }

            return initialNode.GetAncestorsOrThis<TArgumentSyntax>()
                              .LastOrDefault(a => a.AncestorsAndSelf().Contains(node));
        }

        private async Task HandleInvocationExpressionAsync(
            CodeFixContext context, TInvocationExpressionSyntax invocationExpression, TArgumentSyntax argumentOpt)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            var expression = syntaxFacts.GetExpressionOfInvocationExpression(invocationExpression);
            var candidates = semanticModel.GetMemberGroup(expression, cancellationToken).OfType<IMethodSymbol>().ToImmutableArray();

            var arguments = (SeparatedSyntaxList<TArgumentSyntax>)syntaxFacts.GetArgumentsOfInvocationExpression(invocationExpression);
            var argumentInsertPositionInMethodCandidates = GetArgumentInsertPositionForMethodCandidates(
                argumentOpt, semanticModel, syntaxFacts, arguments, candidates);
            RegisterFixForMethodOverloads(context, arguments, argumentInsertPositionInMethodCandidates);
        }

        private async Task HandleObjectCreationExpressionAsync(
            CodeFixContext context,
            TObjectCreationExpressionSyntax objectCreation,
            TArgumentSyntax argumentOpt)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            // Not supported if this is "new { ... }" (as there are no parameters at all.
            var typeNode = syntaxFacts.GetObjectCreationType(objectCreation);
            if (typeNode == null)
            {
                return;
            }

            // If we can't figure out the type being created, or the type isn't in source,
            // then there's nothing we can do.
            var type = semanticModel.GetSymbolInfo(typeNode, cancellationToken).GetAnySymbol() as INamedTypeSymbol;
            if (type == null)
            {
                return;
            }

            if (!type.IsNonImplicitAndFromSource())
            {
                return;
            }

            var arguments = (SeparatedSyntaxList<TArgumentSyntax>)syntaxFacts.GetArgumentsOfObjectCreationExpression(objectCreation);
            var methodCandidates = type.InstanceConstructors;

            var insertionData = GetArgumentInsertPositionForMethodCandidates(
                argumentOpt, semanticModel, syntaxFacts, arguments, methodCandidates);

            RegisterFixForMethodOverloads(context, arguments, insertionData);
        }

        private ImmutableArray<ArgumentInsertPositionData<TArgumentSyntax>> GetArgumentInsertPositionForMethodCandidates(
            TArgumentSyntax argumentOpt,
            SemanticModel semanticModel,
            ISyntaxFactsService syntaxFacts,
            SeparatedSyntaxList<TArgumentSyntax> arguments,
            ImmutableArray<IMethodSymbol> methodCandidates)
        {
            var comparer = syntaxFacts.StringComparer;
            var methodsAndArgumentToAdd = ArrayBuilder<ArgumentInsertPositionData<TArgumentSyntax>>.GetInstance();

            foreach (var method in methodCandidates.OrderBy(m => m.Parameters.Length))
            {
                if (method.IsNonImplicitAndFromSource())
                {
                    var isNamedArgument = !string.IsNullOrWhiteSpace(syntaxFacts.GetNameForArgument(argumentOpt));

                    if (isNamedArgument || NonParamsParameterCount(method) < arguments.Count)
                    {
                        var argumentToAdd = DetermineFirstArgumentToAdd(
                            semanticModel, syntaxFacts, comparer, method,
                            arguments, argumentOpt);

                        if (argumentToAdd != null)
                        {
                            if (argumentOpt != null && argumentToAdd != argumentOpt)
                            {
                                // We were trying to fix a specific argument, but the argument we want
                                // to fix is something different.  That means there was an error earlier
                                // than this argument.  Which means we're looking at a non-viable 
                                // constructor or method.  Skip this one.
                                continue;
                            }

                            methodsAndArgumentToAdd.Add(new ArgumentInsertPositionData<TArgumentSyntax>(
                                method, argumentToAdd, arguments.IndexOf(argumentToAdd)));
                        }
                    }
                }
            }

            return methodsAndArgumentToAdd.ToImmutableAndFree();
        }

        private int NonParamsParameterCount(IMethodSymbol method)
            => method.IsParams() ? method.Parameters.Length - 1 : method.Parameters.Length;

        private void RegisterFixForMethodOverloads(
            CodeFixContext context,
            SeparatedSyntaxList<TArgumentSyntax> arguments,
            ImmutableArray<ArgumentInsertPositionData<TArgumentSyntax>> methodsAndArgumentsToAdd)
        {
            var codeFixData = PrepareCreationOfCodeActions(context.Document, arguments, methodsAndArgumentsToAdd);

            // To keep the list of offered fixes short we create one menu entry per overload only
            // as long as there are two or less overloads present. If there are more overloads we
            // create two menu entries. One entry for non-cascading fixes and one with cascading fixes.
            var fixes = codeFixData.Length <= 2
                ? NestByOverload()
                : NestByCascading();

            context.RegisterFixes(fixes, context.Diagnostics);
            return;

            ImmutableArray<CodeAction> NestByOverload()
            {
                var builder = ArrayBuilder<CodeAction>.GetInstance(codeFixData.Length);
                foreach (var data in codeFixData)
                {
                    // We create the mandatory data.CreateChangedSolutionNonCascading fix first.
                    var title = GetCodeFixTitle(FeaturesResources.Add_parameter_to_0, data.Method, includeParameters: true);
                    CodeAction codeAction = new MyCodeAction(
                        title: title,
                        data.CreateChangedSolutionNonCascading);
                    if (data.CreateChangedSolutionCascading != null)
                    {
                        // We have two fixes to offer. We nest the two fixes in an inlinable CodeAction 
                        // so the IDE is free to either show both at once or to create a sub-menu.
                        var titleForNesting = GetCodeFixTitle(FeaturesResources.Add_parameter_to_0, data.Method, includeParameters: true);
                        var titleCascading = GetCodeFixTitle(FeaturesResources.Add_parameter_to_0_and_overrides_implementations, data.Method,
                                                             includeParameters: true);
                        codeAction = new CodeAction.CodeActionWithNestedActions(
                            title: titleForNesting,
                            ImmutableArray.Create(
                                codeAction,
                                new MyCodeAction(
                                    title: titleCascading,
                                    data.CreateChangedSolutionCascading)),
                            isInlinable: true);
                    }

                    // codeAction is now either a single fix or two fixes wrapped in a CodeActionWithNestedActions
                    builder.Add(codeAction);
                }

                return builder.ToImmutableAndFree();
            }

            ImmutableArray<CodeAction> NestByCascading()
            {
                var builder = ArrayBuilder<CodeAction>.GetInstance(2);

                var nonCascadingActions = ImmutableArray.CreateRange<CodeFixData, CodeAction>(codeFixData, data =>
                {
                    var title = GetCodeFixTitle(FeaturesResources.Add_to_0, data.Method, includeParameters: true);
                    return new MyCodeAction(title: title, data.CreateChangedSolutionNonCascading);
                });

                var cascading = codeFixData.Where(data => data.CreateChangedSolutionCascading != null);
                var cascadingActions = ImmutableArray.CreateRange<CodeAction>(cascading.Select(data =>
                {
                    var title = GetCodeFixTitle(FeaturesResources.Add_to_0, data.Method, includeParameters: true);
                    return new MyCodeAction(title: title, data.CreateChangedSolutionCascading);
                }));

                var aMethod = codeFixData.First().Method; // We need to term the MethodGroup and need an arbitrary IMethodSymbol to do so.
                var nestedNonCascadingTitle = GetCodeFixTitle(FeaturesResources.Add_parameter_to_0, aMethod, includeParameters: false);

                // Create a sub-menu entry with all the non-cascading CodeActions.
                // We make sure the IDE does not inline. Otherwise the context menu gets flooded with our fixes.
                builder.Add(new CodeAction.CodeActionWithNestedActions(nestedNonCascadingTitle, nonCascadingActions, isInlinable: false));

                if (cascadingActions.Length > 0)
                {
                    // if there are cascading CodeActions create a second sub-menu.
                    var nestedCascadingTitle = GetCodeFixTitle(FeaturesResources.Add_parameter_to_0_and_overrides_implementations,
                                                               aMethod, includeParameters: false);
                    builder.Add(new CodeAction.CodeActionWithNestedActions(nestedCascadingTitle, cascadingActions, isInlinable: false));
                }

                return builder.ToImmutableAndFree();
            }
        }

        private ImmutableArray<CodeFixData> PrepareCreationOfCodeActions(
            Document document,
            SeparatedSyntaxList<TArgumentSyntax> arguments,
            ImmutableArray<ArgumentInsertPositionData<TArgumentSyntax>> methodsAndArgumentsToAdd)
        {
            var builder = ArrayBuilder<CodeFixData>.GetInstance(methodsAndArgumentsToAdd.Length);

            // Order by the furthest argument index to the nearest argument index.  The ones with
            // larger argument indexes mean that we matched more earlier arguments (and thus are
            // likely to be the correct match).
            foreach (var argumentInsertPositionData in methodsAndArgumentsToAdd.OrderByDescending(t => t.ArgumentInsertionIndex))
            {
                var methodToUpdate = argumentInsertPositionData.MethodToUpdate;
                var argumentToInsert = argumentInsertPositionData.ArgumentToInsert;

                var cascadingFix = HasCascadingDeclarations(methodToUpdate)
                    ? new Func<CancellationToken, Task<Solution>>(c => FixAsync(document, methodToUpdate, argumentToInsert, arguments, fixAllReferences: true, c))
                    : null;

                var codeFixData = new CodeFixData(
                    methodToUpdate,
                    c => FixAsync(document, methodToUpdate, argumentToInsert, arguments, fixAllReferences: false, c),
                    cascadingFix);

                builder.Add(codeFixData);
            }

            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Checks if there are indications that there might be more than one declarations that need to be fixed.
        /// The check does not look-up if there are other declarations (this is done later in the CodeAction).
        /// </summary>
        private bool HasCascadingDeclarations(IMethodSymbol method)
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

        private static string GetCodeFixTitle(string resourceString, IMethodSymbol methodToUpdate, bool includeParameters)
        {
            var methodDisplay = methodToUpdate.ToDisplayString(new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
                extensionMethodStyle: SymbolDisplayExtensionMethodStyle.StaticMethod,
                parameterOptions: SymbolDisplayParameterOptions.None,
                memberOptions: methodToUpdate.IsConstructor()
                    ? SymbolDisplayMemberOptions.None
                    : SymbolDisplayMemberOptions.IncludeContainingType));

            var parameters = methodToUpdate.Parameters.Select(p => p.ToDisplayString(SimpleFormat));
            var signature = includeParameters
                ? $"{methodDisplay}({string.Join(", ", parameters)})"
                : methodDisplay;
            var title = string.Format(resourceString, signature);
            return title;
        }

        private async Task<Solution> FixAsync(
            Document invocationDocument,
            IMethodSymbol method,
            TArgumentSyntax argument,
            SeparatedSyntaxList<TArgumentSyntax> argumentList,
            bool fixAllReferences,
            CancellationToken cancellationToken)
        {
            var solution = invocationDocument.Project.Solution;
            var (argumentType, refKind) = await GetArgumentTypeAndRefKindAsync(invocationDocument, argument, cancellationToken).ConfigureAwait(false);

            // The argumentNameSuggestion is the base for the parameter name.
            // For each method declaration the name is made unique to avoid name collisions.
            var (argumentNameSuggestion, isNamedArgument) = await GetNameSuggestionForArgumentAsync(
                invocationDocument, argument, cancellationToken).ConfigureAwait(false);

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
                    var insertionIndex = isNamedArgument
                        ? existingParameters.Count
                        : argumentList.IndexOf(argument);

                    // if the preceding parameter is optional, the new parameter must also be optional 
                    // see also BC30202 and CS1737
                    var parameterMustBeOptional = insertionIndex > 0 &&
                        syntaxFacts.GetDefaultOfParameter(existingParameters[insertionIndex - 1]) != null;

                    var parameterSymbol = CreateParameterSymbol(
                        methodDeclaration, argumentType, refKind, parameterMustBeOptional, argumentNameSuggestion);

                    var argumentInitializer = parameterMustBeOptional ? generator.DefaultExpression(argumentType) : null;
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

                    AddParameter(
                        syntaxFacts, editor, methodNode, argument,
                        insertionIndex, parameterDeclaration, cancellationToken);
                }

                var newRoot = editor.GetChangedRoot();
                solution = solution.WithDocumentSyntaxRoot(document.Id, newRoot);
            }

            return solution;
        }

        private static async Task<(ITypeSymbol, RefKind)> GetArgumentTypeAndRefKindAsync(Document invocationDocument, TArgumentSyntax argument, CancellationToken cancellationToken)
        {
            var syntaxFacts = invocationDocument.GetLanguageService<ISyntaxFactsService>();
            var semanticModel = await invocationDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var argumentExpression = syntaxFacts.GetExpressionOfArgument(argument);
            var argumentType = semanticModel.GetTypeInfo(argumentExpression).Type ?? semanticModel.Compilation.ObjectType;
            var refKind = syntaxFacts.GetRefKindOfArgument(argument);
            return (argumentType, refKind);
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
                cancellationToken: cancellationToken).ConfigureAwait(false);
            var referencedSymbols = progress.GetReferencedSymbols();
            return referencedSymbols.Select(referencedSymbol => referencedSymbol.Definition)
                                    .OfType<IMethodSymbol>()
                                    .Distinct()
                                    .ToImmutableArray();
        }

        private async Task<(string argumentNameSuggestion, bool isNamed)> GetNameSuggestionForArgumentAsync(
            Document invocationDocument, TArgumentSyntax argument, CancellationToken cancellationToken)
        {
            var syntaxFacts = invocationDocument.GetLanguageService<ISyntaxFactsService>();

            var argumentName = syntaxFacts.GetNameForArgument(argument);
            if (!string.IsNullOrWhiteSpace(argumentName))
            {
                return (argumentNameSuggestion: argumentName, isNamed: true);
            }
            else
            {
                var semanticModel = await invocationDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var expression = syntaxFacts.GetExpressionOfArgument(argument);
                var semanticFacts = invocationDocument.GetLanguageService<ISemanticFactsService>();
                argumentName = semanticFacts.GenerateNameForExpression(
                    semanticModel, expression, capitalize: false, cancellationToken: cancellationToken);
                return (argumentNameSuggestion: argumentName, isNamed: false);
            }
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
            TArgumentSyntax argument,
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

                    // We want to insert the parameter at the front of the exsiting parameter
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

                    // We want to insert the parameter at the front of the exsiting parameter
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

        private static readonly SymbolDisplayFormat SimpleFormat =
                    new SymbolDisplayFormat(
                        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                        parameterOptions: SymbolDisplayParameterOptions.IncludeParamsRefOut | SymbolDisplayParameterOptions.IncludeType,
                        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        private TArgumentSyntax DetermineFirstArgumentToAdd(
            SemanticModel semanticModel,
            ISyntaxFactsService syntaxFacts,
            StringComparer comparer,
            IMethodSymbol method,
            SeparatedSyntaxList<TArgumentSyntax> arguments,
            TArgumentSyntax argumentOpt)
        {
            var methodParameterNames = new HashSet<string>(comparer);
            methodParameterNames.AddRange(method.Parameters.Select(p => p.Name));

            for (int i = 0, n = arguments.Count; i < n; i++)
            {
                var argument = arguments[i];
                var argumentName = syntaxFacts.GetNameForArgument(argument);

                if (!string.IsNullOrWhiteSpace(argumentName))
                {
                    // If the user provided an argument-name and we don't have any parameters that
                    // match, then this is the argument we want to add a parameter for.
                    if (!methodParameterNames.Contains(argumentName))
                    {
                        return argument;
                    }
                }
                else
                {
                    // Positional argument.  If the position is beyond what the method supports,
                    // then this definitely is an argument we could add.
                    if (i >= method.Parameters.Length)
                    {
                        if (method.Parameters.LastOrDefault()?.IsParams == true)
                        {
                            // Last parameter is a params.  We can't place any parameters past it.
                            return null;
                        }

                        return argument;
                    }

                    // Now check the type of the argument versus the type of the parameter.  If they
                    // don't match, then this is the argument we should make the parameter for.
                    var expressionOfArgument = syntaxFacts.GetExpressionOfArgument(argument);
                    if (expressionOfArgument is null)
                    {
                        return null;
                    }
                    var argumentTypeInfo = semanticModel.GetTypeInfo(expressionOfArgument);
                    var isNullLiteral = syntaxFacts.IsNullLiteralExpression(expressionOfArgument);
                    var isDefaultLiteral = syntaxFacts.IsDefaultLiteralExpression(expressionOfArgument);

                    if (argumentTypeInfo.Type == null && argumentTypeInfo.ConvertedType == null)
                    {
                        // Didn't know the type of the argument.  We shouldn't assume it doesn't
                        // match a parameter.  However, if the user wrote 'null' and it didn't
                        // match anything, then this is the problem argument.
                        if (!isNullLiteral && !isDefaultLiteral)
                        {
                            continue;
                        }
                    }

                    var parameter = method.Parameters[i];

                    if (!TypeInfoMatchesType(argumentTypeInfo, parameter.Type, isNullLiteral, isDefaultLiteral))
                    {
                        if (TypeInfoMatchesWithParamsExpansion(argumentTypeInfo, parameter, isNullLiteral, isDefaultLiteral))
                        {
                            // The argument matched if we expanded out the params-parameter.
                            // As the params-parameter has to be last, there's nothing else to 
                            // do here.
                            return null;
                        }

                        return argument;
                    }
                }
            }

            return null;
        }

        private bool TypeInfoMatchesWithParamsExpansion(
            TypeInfo argumentTypeInfo, IParameterSymbol parameter,
            bool isNullLiteral, bool isDefaultLiteral)
        {
            if (parameter.IsParams && parameter.Type is IArrayTypeSymbol arrayType)
            {
                if (TypeInfoMatchesType(argumentTypeInfo, arrayType.ElementType, isNullLiteral, isDefaultLiteral))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TypeInfoMatchesType(
            TypeInfo argumentTypeInfo, ITypeSymbol type,
            bool isNullLiteral, bool isDefaultLiteral)
        {
            if (type.Equals(argumentTypeInfo.Type) || type.Equals(argumentTypeInfo.ConvertedType))
            {
                return true;
            }

            if (isDefaultLiteral)
            {
                return true;
            }

            if (isNullLiteral)
            {
                return type.IsReferenceType || type.IsNullable();
            }

            // Overload resolution couldn't resolve the actual type of the type parameter. We assume
            // that the type parameter can be the argument's type (ignoring any type parameter constraints).
            if (type.Kind == SymbolKind.TypeParameter)
            {
                return true;
            }

            return false;
        }

        private class MyCodeAction : CodeAction.SolutionChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution)
                : base(title, createChangedSolution)
            {
            }
        }
    }
}
