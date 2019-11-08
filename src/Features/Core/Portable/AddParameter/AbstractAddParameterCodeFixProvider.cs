// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddParameter
{
    internal abstract class AbstractAddParameterCodeFixProvider<
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

        public override FixAllProvider GetFixAllProvider()
        {
            // Fix All is not supported for this code fix.
            return null;
        }

        protected virtual RegisterFixData<TArgumentSyntax> TryGetLanguageSpecificFixInfo(
            SemanticModel semanticModel,
            SyntaxNode node,
            CancellationToken cancellationToken)
            => null;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var cancellationToken = context.CancellationToken;
            var diagnostic = context.Diagnostics.First();

            var document = context.Document;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var initialNode = root.FindNode(diagnostic.Location.SourceSpan);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            for (var node = initialNode; node != null; node = node.Parent)
            {
                var fixData =
                    TryGetInvocationExpressionFixInfo(semanticModel, syntaxFacts, node, cancellationToken) ??
                    TryGetObjectCreationFixInfo(semanticModel, syntaxFacts, node, cancellationToken) ??
                    TryGetLanguageSpecificFixInfo(semanticModel, node, cancellationToken);

                if (fixData != null)
                {
                    var candidates = fixData.MethodCandidates;
                    if (fixData.IsConstructorInitializer)
                    {
                        // The invocation is a :this() or :base() call. In  the 'this' case we need to exclude the 
                        // method with the diagnostic because otherwise we might introduce a call to itself (which is forbidden).
                        if (semanticModel.GetEnclosingSymbol(node.SpanStart, cancellationToken) is IMethodSymbol methodWithDiagnostic)
                        {
                            candidates = candidates.Remove(methodWithDiagnostic);
                        }
                    }

                    var argumentOpt = TryGetRelevantArgument(initialNode, node, diagnostic);
                    var argumentInsertPositionInMethodCandidates = GetArgumentInsertPositionForMethodCandidates(
                        argumentOpt, semanticModel, syntaxFacts, fixData.Arguments, candidates);
                    RegisterFixForMethodOverloads(context, fixData.Arguments, argumentInsertPositionInMethodCandidates);
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
            if (TooManyArgumentsDiagnosticIds.Contains(diagnostic.Id))
            {
                return null;
            }

            if (CannotConvertDiagnosticIds.Contains(diagnostic.Id))
            {
                return null;
            }

            return initialNode.GetAncestorsOrThis<TArgumentSyntax>()
                              .LastOrDefault(a => a.AncestorsAndSelf().Contains(node));
        }

        private static RegisterFixData<TArgumentSyntax> TryGetInvocationExpressionFixInfo(
            SemanticModel semanticModel,
            ISyntaxFactsService syntaxFacts,
            SyntaxNode node,
            CancellationToken cancellationToken)
        {
            if (node is TInvocationExpressionSyntax invocationExpression)
            {
                var expression = syntaxFacts.GetExpressionOfInvocationExpression(invocationExpression);
                var candidates = semanticModel.GetMemberGroup(expression, cancellationToken).OfType<IMethodSymbol>().ToImmutableArray();
                var arguments = (SeparatedSyntaxList<TArgumentSyntax>)syntaxFacts.GetArgumentsOfInvocationExpression(invocationExpression);

                // In VB a constructor calls other constructor overloads via a Me.New(..) invocation.
                // If the candidates are MethodKind.Constructor than these are the equivalent the a C# ConstructorInitializer.
                var isConstructorInitializer = candidates.All(m => m.MethodKind == MethodKind.Constructor);
                return new RegisterFixData<TArgumentSyntax>(arguments, candidates, isConstructorInitializer);
            }

            return null;
        }

        private static RegisterFixData<TArgumentSyntax> TryGetObjectCreationFixInfo(
            SemanticModel semanticModel,
            ISyntaxFactsService syntaxFacts,
            SyntaxNode node,
            CancellationToken cancellationToken)
        {
            if (node is TObjectCreationExpressionSyntax objectCreation)
            {

                // Not supported if this is "new { ... }" (as there are no parameters at all.
                var typeNode = syntaxFacts.GetObjectCreationType(objectCreation);
                if (typeNode == null)
                {
                    return new RegisterFixData<TArgumentSyntax>();
                }

                // If we can't figure out the type being created, or the type isn't in source,
                // then there's nothing we can do.
                if (!(semanticModel.GetSymbolInfo(typeNode, cancellationToken).GetAnySymbol() is INamedTypeSymbol type))
                {
                    return new RegisterFixData<TArgumentSyntax>();
                }

                if (!type.IsNonImplicitAndFromSource())
                {
                    return new RegisterFixData<TArgumentSyntax>();
                }

                var arguments = (SeparatedSyntaxList<TArgumentSyntax>)syntaxFacts.GetArgumentsOfObjectCreationExpression(objectCreation);
                var methodCandidates = type.InstanceConstructors;

                return new RegisterFixData<TArgumentSyntax>(arguments, methodCandidates, isConstructorInitializer: false);
            }

            return null;
        }

        private static ImmutableArray<ArgumentInsertPositionData<TArgumentSyntax>> GetArgumentInsertPositionForMethodCandidates(
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

        private static int NonParamsParameterCount(IMethodSymbol method)
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
                using var builderDisposer = ArrayBuilder<CodeAction>.GetInstance(codeFixData.Length, out var builder);
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

                return builder.ToImmutable();
            }

            ImmutableArray<CodeAction> NestByCascading()
            {
                using var builderDisposer = ArrayBuilder<CodeAction>.GetInstance(capacity: 2, out var builder);

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

                return builder.ToImmutable();
            }
        }

        private ImmutableArray<CodeFixData> PrepareCreationOfCodeActions(
            Document document,
            SeparatedSyntaxList<TArgumentSyntax> arguments,
            ImmutableArray<ArgumentInsertPositionData<TArgumentSyntax>> methodsAndArgumentsToAdd)
        {
            using var builderDisposer = ArrayBuilder<CodeFixData>.GetInstance(methodsAndArgumentsToAdd.Length, out var builder);

            // Order by the furthest argument index to the nearest argument index.  The ones with
            // larger argument indexes mean that we matched more earlier arguments (and thus are
            // likely to be the correct match).
            foreach (var argumentInsertPositionData in methodsAndArgumentsToAdd.OrderByDescending(t => t.ArgumentInsertionIndex))
            {
                var methodToUpdate = argumentInsertPositionData.MethodToUpdate;
                var argumentToInsert = argumentInsertPositionData.ArgumentToInsert;

                var cascadingFix = AddParameterService.Instance.HasCascadingDeclarations(methodToUpdate)
                    ? new Func<CancellationToken, Task<Solution>>(c => FixAsync(document, methodToUpdate, argumentToInsert, arguments, fixAllReferences: true, c))
                    : null;

                var codeFixData = new CodeFixData(
                    methodToUpdate,
                    c => FixAsync(document, methodToUpdate, argumentToInsert, arguments, fixAllReferences: false, c),
                    cascadingFix);

                builder.Add(codeFixData);
            }

            return builder.ToImmutable();
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
            var (argumentType, refKind) = await GetArgumentTypeAndRefKindAsync(invocationDocument, argument, cancellationToken).ConfigureAwait(false);

            // The argumentNameSuggestion is the base for the parameter name.
            // For each method declaration the name is made unique to avoid name collisions.
            var (argumentNameSuggestion, isNamedArgument) = await GetNameSuggestionForArgumentAsync(
                invocationDocument, argument, cancellationToken).ConfigureAwait(false);

            var newParameterIndex = isNamedArgument ? (int?)null : argumentList.IndexOf(argument);
            return await AddParameterService.Instance.AddParameterAsync(
                invocationDocument,
                method,
                argumentType,
                refKind,
                argumentNameSuggestion,
                newParameterIndex,
                fixAllReferences,
                cancellationToken).ConfigureAwait(false);
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

        private static readonly SymbolDisplayFormat SimpleFormat =
                    new SymbolDisplayFormat(
                        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                        parameterOptions: SymbolDisplayParameterOptions.IncludeParamsRefOut | SymbolDisplayParameterOptions.IncludeType,
                        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        private static TArgumentSyntax DetermineFirstArgumentToAdd(
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

                    if (argumentTypeInfo is { Type: null, ConvertedType: null })
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

        private static bool TypeInfoMatchesWithParamsExpansion(
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

        private static bool TypeInfoMatchesType(
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
