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

            var symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken);
            var candidates = symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().ToImmutableArray();

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

            var constructorsAndArgumentToAdd = GetArgumentInsertPositionForMethodCandidates(argumentOpt, semanticModel, syntaxFacts, arguments, methodCandidates);

            RegisterFixForMethodOverloads(context, arguments, constructorsAndArgumentToAdd);
        }

        private void RegisterFixForMethodOverloads(
            CodeFixContext context,
            SeparatedSyntaxList<TArgumentSyntax> arguments,
            ImmutableArray<ArgumentInsertPositionData<TArgumentSyntax>> methodsAndArgumentsToAdd)
        {
            // Order by the furthest argument index to the nearest argument index.  The ones with
            // larger argument indexes mean that we matched more earlier arguments (and thus are
            // likely to be the correct match).
            foreach (var argumentInsertPositionData in methodsAndArgumentsToAdd.OrderByDescending(t => t.ArgumentInsertionIndex))
            {
                var methodToUpdate = argumentInsertPositionData.MethodToUpdate;
                var argumentToInsert = argumentInsertPositionData.ArgumentToInsert;
                var parameters = methodToUpdate.Parameters.Select(p => p.ToDisplayString(SimpleFormat));
                var signature = $"{methodToUpdate.Name}({string.Join(", ", parameters)})";

                var title = string.Format(FeaturesResources.Add_parameter_to_0, signature);

                context.RegisterCodeFix(
                    new MyCodeAction(title, c => FixAsync(context.Document, methodToUpdate, argumentToInsert, arguments, c)),
                    context.Diagnostics);
            }
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

        private async Task<Document> FixAsync(
            Document invocationDocument,
            IMethodSymbol method,
            TArgumentSyntax argument,
            SeparatedSyntaxList<TArgumentSyntax> argumentList,
            CancellationToken cancellationToken)
        {
            var methodDeclaration = await method.DeclaringSyntaxReferences[0].GetSyntaxAsync(cancellationToken).ConfigureAwait(false);

            var (parameterSymbol, isNamedArgument) = await CreateParameterSymbolAsync(
                invocationDocument, method, argument, cancellationToken).ConfigureAwait(false);

            var methodDocument = invocationDocument.Project.Solution.GetDocument(methodDeclaration.SyntaxTree);
            var syntaxFacts = methodDocument.GetLanguageService<ISyntaxFactsService>();
            var methodDeclarationRoot = methodDeclaration.SyntaxTree.GetRoot(cancellationToken);
            var editor = new SyntaxEditor(methodDeclarationRoot, methodDocument.Project.Solution.Workspace);

            var parameterDeclaration = editor.Generator.ParameterDeclaration(parameterSymbol)
                                                       .WithAdditionalAnnotations(Formatter.Annotation);

            var existingParameters = editor.Generator.GetParameters(methodDeclaration);
            var insertionIndex = isNamedArgument
                ? existingParameters.Count
                : argumentList.IndexOf(argument);

            if (method.IsExtensionMethod)
            {
                insertionIndex++;
            }

            AddParameter(
                syntaxFacts, editor, methodDeclaration, argument,
                insertionIndex, parameterDeclaration, cancellationToken);

            var newRoot = editor.GetChangedRoot();
            var newDocument = methodDocument.WithSyntaxRoot(newRoot);

            return newDocument;
        }

        private async Task<(IParameterSymbol, bool isNamedArgument)> CreateParameterSymbolAsync(
            Document invocationDocument,
            IMethodSymbol method,
            TArgumentSyntax argument,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = invocationDocument.GetLanguageService<ISyntaxFactsService>();
            var semanticFacts = invocationDocument.GetLanguageService<ISemanticFactsService>();
            var argumentName = syntaxFacts.GetNameForArgument(argument);
            var expression = syntaxFacts.GetExpressionOfArgument(argument);

            var semanticModel = await invocationDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var parameterType = semanticModel.GetTypeInfo(expression).Type ?? semanticModel.Compilation.ObjectType;

            if (!string.IsNullOrWhiteSpace(argumentName))
            {
                var newParameterSymbol = CodeGenerationSymbolFactory.CreateParameterSymbol(
                    attributes: default, refKind: RefKind.None, isParams: false, type: parameterType, name: argumentName);

                return (newParameterSymbol, isNamedArgument: true);
            }
            else
            {
                var name = semanticFacts.GenerateNameForExpression(
                    semanticModel, expression, capitalize: false, cancellationToken: cancellationToken);
                var uniqueName = NameGenerator.EnsureUniqueness(name, method.Parameters.Select(p => p.Name));

                var newParameterSymbol = CodeGenerationSymbolFactory.CreateParameterSymbol(
                    attributes: default, refKind: RefKind.None, isParams: false, type: parameterType, name: uniqueName);

                return (newParameterSymbol, isNamedArgument: false);
            }
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

            if (type.Kind == SymbolKind.TypeParameter)
            {
                return true;
            }

            return false;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(
                string title,
                Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
