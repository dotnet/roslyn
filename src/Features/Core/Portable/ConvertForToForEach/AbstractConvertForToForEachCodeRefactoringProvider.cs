// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ConvertForToForEach;

internal abstract class AbstractConvertForToForEachCodeRefactoringProvider<
    TStatementSyntax,
    TForStatementSyntax,
    TExpressionSyntax,
    TMemberAccessExpressionSyntax,
    TTypeNode,
    TVariableDeclaratorSyntax> : CodeRefactoringProvider
    where TStatementSyntax : SyntaxNode
    where TForStatementSyntax : TStatementSyntax
    where TExpressionSyntax : SyntaxNode
    where TMemberAccessExpressionSyntax : SyntaxNode
    where TTypeNode : SyntaxNode
    where TVariableDeclaratorSyntax : SyntaxNode
{
    protected abstract string GetTitle();

    protected abstract SyntaxList<TStatementSyntax> GetBodyStatements(TForStatementSyntax forStatement);
    protected abstract bool IsValidVariableDeclarator(TVariableDeclaratorSyntax firstVariable);

    protected abstract bool TryGetForStatementComponents(
        TForStatementSyntax forStatement,
        out SyntaxToken iterationVariable,
        [NotNullWhen(true)] out TExpressionSyntax? initializer,
        [NotNullWhen(true)] out TMemberAccessExpressionSyntax? memberAccess,
        out TExpressionSyntax? stepValueExpressionOpt,
        CancellationToken cancellationToken);

    protected abstract SyntaxNode ConvertForNode(
        TForStatementSyntax currentFor, TTypeNode? typeNode, SyntaxToken foreachIdentifier,
        TExpressionSyntax collectionExpression, ITypeSymbol iterationVariableType);

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, textSpan, cancellationToken) = context;
        var forStatement = await context.TryGetRelevantNodeAsync<TForStatementSyntax>().ConfigureAwait(false);
        if (forStatement == null)
            return;

        if (!TryGetForStatementComponents(forStatement,
                out var iterationVariable, out var initializer, out var memberAccess, out var stepValueExpressionOpt, cancellationToken))
        {
            return;
        }

        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        syntaxFacts.GetPartsOfMemberAccessExpression(memberAccess,
            out var collectionExpressionNode, out var memberAccessNameNode);

        var collectionExpression = (TExpressionSyntax)collectionExpressionNode;
        syntaxFacts.GetNameAndArityOfSimpleName(memberAccessNameNode, out var memberAccessName, out _);
        if (memberAccessName is not nameof(Array.Length) and not nameof(IList.Count))
            return;

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        // Make sure it's a single-variable for loop and that we're not a loop where we're
        // referencing some previously declared symbol.  i.e
        // VB allows:
        //
        //      dim i as integer
        //      for i = 0 to ...
        //
        // We can't convert this as it would change important semantics.
        // NOTE: we could potentially update this if we saw that the variable was not used
        // after the for-loop.  But, for now, we'll just be conservative and assume this means
        // the user wanted the 'i' for some other purpose and we should keep things as is.
        if (semanticModel.GetOperation(forStatement, cancellationToken) is not ILoopOperation { Locals.Length: 1 })
            return;

        // Make sure we're starting at 0.
        var initializerValue = semanticModel.GetConstantValue(initializer, cancellationToken);
        if (initializerValue is not { HasValue: true, Value: 0 })
            return;

        // Make sure we're incrementing by 1.
        if (stepValueExpressionOpt != null)
        {
            var stepValue = semanticModel.GetConstantValue(stepValueExpressionOpt);
            if (stepValue is not { HasValue: true, Value: 1 })
                return;
        }

        var collectionType = semanticModel.GetTypeInfo(collectionExpression, cancellationToken);
        if (collectionType.Type is null or IErrorTypeSymbol)
            return;

        var containingType = semanticModel.GetEnclosingNamedType(textSpan.Start, cancellationToken);
        if (containingType == null)
            return;

        var ienumerableType = semanticModel.Compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T);
        var ienumeratorType = semanticModel.Compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerator_T);

        // make sure the collection can be iterated.
        if (!TryGetIterationElementType(
                containingType, collectionType.Type,
                ienumerableType, ienumeratorType,
                out var iterationType))
        {
            return;
        }

        // If the user uses the iteration variable for any other reason, we can't convert this.
        var bodyStatements = GetBodyStatements(forStatement);
        foreach (var statement in bodyStatements)
        {
            if (IterationVariableIsUsedForMoreThanCollectionIndex(statement))
                return;
        }

        // Looks good.  We can convert this.
        var title = GetTitle();
        context.RegisterRefactoring(
            CodeAction.Create(
                title,
                cancellationToken => ConvertForToForEachAsync(
                    document, forStatement, iterationVariable, collectionExpression,
                    containingType, collectionType.Type, iterationType, cancellationToken),
                title),
            forStatement.Span);

        return;

        // local functions
        bool IterationVariableIsUsedForMoreThanCollectionIndex(SyntaxNode current)
        {
            if (syntaxFacts.IsIdentifierName(current))
            {
                syntaxFacts.GetNameAndArityOfSimpleName(current, out var name, out _);
                if (name == iterationVariable.ValueText)
                {
                    // found a reference.  make sure it's only used inside something like
                    // list[i]

                    var argument = current.Parent;
                    if (!syntaxFacts.IsSimpleArgument(argument))
                        return true;

                    // we support `list[i]` or `list.ElementAt(i)`
                    var argumentList = argument?.Parent;
                    if (argumentList is null)
                        return true;

                    var arguments = syntaxFacts.GetArgumentsOfArgumentList(argumentList);
                    // was used in a multi-dimensional indexing, or multiple argument method call.  Can't convert this.
                    if (arguments.Count != 1)
                        return true;

                    if (!IsGoodElementAccessExpression(argumentList) &&
                        !IsGoodInvocationExpression(argumentList))
                    {
                        // used in something other than accessing into a collection.
                        // can't convert this for-loop.
                        return true;
                    }
                }

                // this usage of the for-variable is fine.
            }

            foreach (var child in current.ChildNodesAndTokens())
            {
                if (child.AsNode(out var childNode) &&
                    IterationVariableIsUsedForMoreThanCollectionIndex(childNode))
                {
                    return true;
                }
            }

            return false;
        }

        bool IsGoodElementAccessExpression(SyntaxNode argumentList)
        {
            if (syntaxFacts.IsElementAccessExpression(argumentList.Parent))
            {
                var expr = syntaxFacts.GetExpressionOfElementAccessExpression(argumentList.Parent);

                // Have to be indexing into the collection.
                if (syntaxFacts.AreEquivalent(expr, collectionExpression))
                    return true;
            }

            return false;
        }

        bool IsGoodInvocationExpression(SyntaxNode argumentList)
        {
            if (syntaxFacts.IsInvocationExpression(argumentList.Parent))
            {
                var invokedExpression = syntaxFacts.GetExpressionOfInvocationExpression(argumentList.Parent);
                if (syntaxFacts.IsMemberAccessExpression(invokedExpression))
                {
                    syntaxFacts.GetPartsOfMemberAccessExpression(invokedExpression, out var accessedExpression, out var accessedName);
                    syntaxFacts.GetNameAndArityOfSimpleName(accessedName, out var memberName, out _);

                    // Have to be indexing into the collection.
                    if (memberName == nameof(Enumerable.ElementAt) &&
                        syntaxFacts.AreEquivalent(accessedExpression, collectionExpression))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    private static IEnumerable<TSymbol> TryFindMembersInThisOrBaseTypes<TSymbol>(
        INamedTypeSymbol containingType, ITypeSymbol type, string memberName) where TSymbol : class, ISymbol
    {
        var methods = type.GetAccessibleMembersInThisAndBaseTypes<TSymbol>(containingType);
        return methods.Where(m => m.Name == memberName);
    }

    private static TSymbol? TryFindMemberInThisOrBaseTypes<TSymbol>(
        INamedTypeSymbol containingType, ITypeSymbol type, string memberName) where TSymbol : class, ISymbol
    {
        return TryFindMembersInThisOrBaseTypes<TSymbol>(containingType, type, memberName).FirstOrDefault();
    }

    private static bool TryGetIterationElementType(
        INamedTypeSymbol containingType, ITypeSymbol collectionType,
        INamedTypeSymbol ienumerableType, INamedTypeSymbol ienumeratorType,
        [NotNullWhen(true)] out ITypeSymbol? iterationType)
    {
        if (collectionType is IArrayTypeSymbol arrayType)
        {
            iterationType = arrayType.ElementType;

            // We only support single-dimensional array iteration.
            return arrayType.Rank == 1;
        }

        // Check in the class/struct hierarchy first.
        var getEnumeratorMethod = TryFindMemberInThisOrBaseTypes<IMethodSymbol>(
            containingType, collectionType, WellKnownMemberNames.GetEnumeratorMethodName);
        if (getEnumeratorMethod != null)
        {
            return TryGetIterationElementTypeFromGetEnumerator(
                containingType, getEnumeratorMethod, ienumeratorType, out iterationType);
        }

        // couldn't find .GetEnumerator on the class/struct.  Check the interface hierarchy.
        var instantiatedIEnumerableType = collectionType.GetAllInterfacesIncludingThis().FirstOrDefault(
            t => Equals(t.OriginalDefinition, ienumerableType));

        if (instantiatedIEnumerableType != null)
        {
            iterationType = instantiatedIEnumerableType.TypeArguments[0];
            return true;
        }

        iterationType = null;
        return false;
    }

    private static bool TryGetIterationElementTypeFromGetEnumerator(
        INamedTypeSymbol containingType, IMethodSymbol getEnumeratorMethod,
        INamedTypeSymbol ienumeratorType, [NotNullWhen(true)] out ITypeSymbol? iterationType)
    {
        var getEnumeratorReturnType = getEnumeratorMethod.ReturnType;

        // Check in the class/struct hierarchy first.
        var currentProperty = TryFindMemberInThisOrBaseTypes<IPropertySymbol>(
            containingType, getEnumeratorReturnType, WellKnownMemberNames.CurrentPropertyName);
        if (currentProperty != null)
        {
            iterationType = currentProperty.Type;
            return true;
        }

        // couldn't find .Current on the class/struct.  Check the interface hierarchy.
        var instantiatedIEnumeratorType = getEnumeratorReturnType.GetAllInterfacesIncludingThis().FirstOrDefault(
            t => Equals(t.OriginalDefinition, ienumeratorType));

        if (instantiatedIEnumeratorType != null)
        {
            iterationType = instantiatedIEnumeratorType.TypeArguments[0];
            return true;
        }

        iterationType = null;
        return false;
    }

    private async Task<Document> ConvertForToForEachAsync(
        Document document,
        TForStatementSyntax forStatement,
        SyntaxToken iterationVariable,
        TExpressionSyntax collectionExpression,
        INamedTypeSymbol containingType,
        ITypeSymbol collectionType,
        ITypeSymbol iterationType,
        CancellationToken cancellationToken)
    {
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var semanticFacts = document.GetRequiredLanguageService<ISemanticFactsService>();
        var generator = SyntaxGenerator.GetGenerator(document);

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var editor = new SyntaxEditor(root, generator);

        // create dummy "list[i]" and "list.ElementAt(i)" expressions.  We'll use this to find all places to replace
        // in the current for statement.
        var indexExpression = generator.ElementAccessExpression(collectionExpression, generator.IdentifierName(iterationVariable));
        var elementAtExpression = generator.InvocationExpression(
            generator.MemberAccessExpression(collectionExpression, generator.IdentifierName(nameof(Enumerable.ElementAt))),
            generator.IdentifierName(iterationVariable));

        // See if the first statement in the for loop is of the form:
        //      var x = list[i]   or
        //
        // If so, we'll use those as the iteration variables for the new foreach statement.
        var (typeNode, foreachIdentifier, declarationStatement) = TryDeconstructInitialDeclaration();

        if (typeNode == null)
        {
            // user didn't provide an explicit type.  Check if the index-type of the collection
            // is different from than .Current type of the enumerator.  If so, add an explicit
            // type so that the foreach will coerce the types accordingly.
            var indexerType = GetIndexerType(containingType, collectionType, semanticModel.Compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T));
            if (!Equals(indexerType, iterationType))
            {
                typeNode = (TTypeNode)generator.TypeExpression(
                    indexerType ?? semanticModel.Compilation.GetSpecialType(SpecialType.System_Object));
            }
        }

        // If we couldn't find an appropriate existing variable to use as the foreach
        // variable, then generate one automatically.
        if (foreachIdentifier.RawKind == 0)
        {
            foreachIdentifier = semanticFacts.GenerateUniqueName(
                semanticModel, forStatement, container: null, baseName: "v", usedNames: [], cancellationToken);
            foreachIdentifier = foreachIdentifier.WithAdditionalAnnotations(RenameAnnotation.Create());
        }

        // Create the expression we'll use to replace all matches in the for-body.
        var foreachIdentifierReference = foreachIdentifier.WithoutAnnotations(RenameAnnotation.Kind).WithoutTrivia();

        // Walk the for statement, replacing any matches we find.
        FindAndReplaceMatches(forStatement);

        // Finally, remove the declaration statement if we found one.  Move all its leading
        // trivia to the next statement.
        if (declarationStatement != null)
        {
            editor.RemoveNode(declarationStatement,
                SyntaxGenerator.DefaultRemoveOptions | SyntaxRemoveOptions.KeepLeadingTrivia);
        }

        editor.ReplaceNode(
            forStatement,
            (currentFor, _) => ConvertForNode(
                (TForStatementSyntax)currentFor, typeNode, foreachIdentifier,
                collectionExpression, iterationType));

        return document.WithSyntaxRoot(editor.GetChangedRoot());

        // local functions
        (TTypeNode?, SyntaxToken, TStatementSyntax) TryDeconstructInitialDeclaration()
        {
            var bodyStatements = GetBodyStatements(forStatement);

            if (bodyStatements.Count >= 1)
            {
                var firstStatement = bodyStatements[0];
                if (syntaxFacts.IsLocalDeclarationStatement(firstStatement))
                {
                    var variables = syntaxFacts.GetVariablesOfLocalDeclarationStatement(firstStatement);
                    if (variables.Count == 1)
                    {
                        var firstVariable = (TVariableDeclaratorSyntax)variables[0];
                        if (IsValidVariableDeclarator(firstVariable))
                        {
                            var initializer = syntaxFacts.GetInitializerOfVariableDeclarator(firstVariable);
                            if (initializer != null)
                            {
                                var firstVariableInitializer = syntaxFacts.GetValueOfEqualsValueClause(initializer);
                                if (syntaxFacts.AreEquivalent(firstVariableInitializer, indexExpression))
                                {
                                    var type = (TTypeNode?)syntaxFacts.GetTypeOfVariableDeclarator(firstVariable)?.WithoutLeadingTrivia();
                                    var identifier = syntaxFacts.GetIdentifierOfVariableDeclarator(firstVariable);
                                    var statement = firstStatement;
                                    return (type, identifier, statement);
                                }
                            }
                        }
                    }
                }
            }

            return default;
        }

        void FindAndReplaceMatches(SyntaxNode current)
        {
            if (SemanticEquivalence.AreEquivalent(semanticModel, current, collectionExpression))
            {
                if (syntaxFacts.AreEquivalent(current.Parent, indexExpression))
                {
                    // Found a match.  replace with iteration variable.
                    var indexMatch = current.GetRequiredParent();
                    Replace(indexMatch);
                }
                else if (syntaxFacts.AreEquivalent(current.Parent?.Parent, elementAtExpression))
                {
                    // Found a match.  replace with iteration variable.
                    var indexMatch = current.GetRequiredParent().GetRequiredParent();
                    Replace(indexMatch);
                }
                else
                {
                    // Collection was used for some other purpose.  If it's passed as an argument
                    // to something, or is written to, or has a method invoked on it, we'll warn
                    // that it's potentially changing and may break if you switch to a foreach loop.
                    var shouldWarn = syntaxFacts.IsArgument(current.Parent);
                    shouldWarn |= semanticFacts.IsWrittenTo(semanticModel, current, cancellationToken);
                    shouldWarn |=
                        syntaxFacts.IsMemberAccessExpression(current.Parent) &&
                        syntaxFacts.IsInvocationExpression(current.Parent.Parent);

                    if (shouldWarn)
                    {
                        editor.ReplaceNode(
                            current,
                            (node, _) => node.WithAdditionalAnnotations(
                                WarningAnnotation.Create(FeaturesResources.Warning_colon_Iteration_variable_crossed_function_boundary)));
                    }
                }

                return;
            }

            foreach (var child in current.ChildNodesAndTokens())
            {
                if (child.AsNode(out var childNode))
                    FindAndReplaceMatches(childNode);
            }
        }

        bool CrossesFunctionBoundary(SyntaxNode node)
        {
            var containingFunction = node.AncestorsAndSelf().FirstOrDefault(
                n => syntaxFacts.IsLocalFunctionStatement(n) || syntaxFacts.IsAnonymousFunctionExpression(n));

            if (containingFunction == null)
                return false;

            return containingFunction.AncestorsAndSelf().Contains(forStatement);
        }

        void Replace(SyntaxNode indexMatch)
        {
            var replacementToken = foreachIdentifierReference;

            if (semanticFacts.IsWrittenTo(semanticModel, indexMatch, cancellationToken))
            {
                replacementToken = replacementToken.WithAdditionalAnnotations(
                    WarningAnnotation.Create(FeaturesResources.Warning_colon_Collection_was_modified_during_iteration));
            }

            if (CrossesFunctionBoundary(indexMatch))
            {
                replacementToken = replacementToken.WithAdditionalAnnotations(
                    WarningAnnotation.Create(FeaturesResources.Warning_colon_Iteration_variable_crossed_function_boundary));
            }

            editor.ReplaceNode(
                indexMatch,
                generator.IdentifierName(replacementToken).WithTriviaFrom(indexMatch));
        }
    }

    private static ITypeSymbol? GetIndexerType(
        INamedTypeSymbol containingType,
        ITypeSymbol collectionType,
        INamedTypeSymbol ienumerableType)
    {
        if (collectionType is IArrayTypeSymbol arrayType)
            return arrayType.Rank == 1 ? arrayType.ElementType : null;

        var indexer = collectionType
            .GetAccessibleMembersInThisAndBaseTypes<IPropertySymbol>(containingType)
            .Where(IsViableIndexer)
            .FirstOrDefault();

        if (indexer?.Type != null)
            return indexer.Type;

        if (collectionType.IsInterfaceType())
        {
            var interfaces = collectionType.GetAllInterfacesIncludingThis();
            indexer = interfaces.SelectMany(i => i.GetMembers().OfType<IPropertySymbol>().Where(IsViableIndexer)).FirstOrDefault();

            if (indexer?.Type != null)
                return indexer.Type;
        }

        foreach (var interfaceType in collectionType.GetAllInterfacesIncludingThis())
        {
            if (Equals(interfaceType.OriginalDefinition, ienumerableType))
                return interfaceType.TypeArguments[0];
        }

        return null;
    }

    private static bool IsViableIndexer(IPropertySymbol property)
        => property is { IsIndexer: true, Parameters: [{ Type.SpecialType: SpecialType.System_Int32 }] };
}
