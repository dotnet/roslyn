// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ConvertForToForEach
{
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

        protected abstract SyntaxNode ConvertForNode(
            TForStatementSyntax currentFor, TTypeNode typeNode, SyntaxToken foreachIdentifier, 
            TExpressionSyntax collectionExpression, ITypeSymbol iterationVariableType, OptionSet options);
        protected abstract SyntaxList<TStatementSyntax> GetBodyStatements(TForStatementSyntax forStatement);
        protected abstract bool IsValidVariableDeclarator(TVariableDeclaratorSyntax firstVariable);
        protected abstract bool TryGetForStatementComponents(
            TForStatementSyntax forStatement,
            out SyntaxToken iterationVariable, out TExpressionSyntax initializer,
            out TMemberAccessExpressionSyntax memberAccess, out TExpressionSyntax stepValue,
            CancellationToken cancellationToken);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var cancellationToken = context.CancellationToken;
            var document = context.Document;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(context.Span.Start);

            // position has to be inside the 'for' span, or if there is a selection, it must
            // match the 'for' span exactly.
            if (context.Span.IsEmpty && !token.Span.IntersectsWith(context.Span.Start))
            {
                return;
            }

            if (!context.Span.IsEmpty && context.Span != token.Span)
            {
                return;
            }

            var forStatement = token.Parent as TForStatementSyntax;
            if (forStatement == null)
            {
                return;
            }

            if (!TryGetForStatementComponents(forStatement,
                    out var iterationVariable, out var initializer,
                    out var memberAccess, out var stepValueOpt, cancellationToken))
            {
                return;
            }

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            syntaxFacts.GetPartsOfMemberAccessExpression(memberAccess,
                out var collectionExpressionNode, out var memberAccessNameNode);

            var collectionExpression = (TExpressionSyntax)collectionExpressionNode;
            syntaxFacts.GetNameAndArityOfSimpleName(memberAccessNameNode, out var memberAccessName, out _);
            if (memberAccessName != nameof(Array.Length) && memberAccessName != nameof(IList.Count))
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            // Make sure we're starting at 0.
            var initializerValue = semanticModel.GetConstantValue(initializer, cancellationToken);
            if (!(initializerValue.HasValue && initializerValue.Value is 0))
            {
                return;
            }

            // Make sure we're incrementing by 1.
            if (stepValueOpt != null)
            {
                var stepValue = semanticModel.GetConstantValue(stepValueOpt);
                if (!(stepValue.HasValue && stepValue.Value is 1))
                {
                    return;
                }
            }

            var collectionType = semanticModel.GetTypeInfo(collectionExpression, cancellationToken);
            if (collectionType.Type == null && collectionType.Type.TypeKind == TypeKind.Error)
            {
                return;
            }

            var containingType = semanticModel.GetEnclosingNamedType(context.Span.Start, cancellationToken);
            if (containingType == null)
            {
                return;
            }

            var ienumerableType = semanticModel.Compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T);
            var ienumeratorType = semanticModel.Compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerator_T);

            // make sure the collection can be iterated.
            if (TryGetIterationElementType(
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
                {
                    return;
                }
            }

            // Looks good.  We can conver this.
            context.RegisterRefactoring(new MyCodeAction(GetTitle(),
                c => ConvertForToForEachAsync(
                    document, forStatement, iterationVariable, 
                    collectionExpression, iterationType, c)));

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

                        if (!syntaxFacts.IsElementAccessExpression(current.Parent))
                        {
                            // used in something other than accessing into a collection.
                            // can't convert this for-loop.
                            return true;
                        }

                        if (!syntaxFacts.AreEquivalent(current.Parent, collectionExpression))
                        {
                            // was indexing into something other than the collection.
                            // can't convert this for-loop.
                            return true;
                        }

                        // this usage of the for-variable is fine.
                    }
                }

                foreach (var child in current.ChildNodesAndTokens())
                {
                    if (child.IsNode)
                    {
                        if (IterationVariableIsUsedForMoreThanCollectionIndex(child.AsNode()))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        private bool TryGetIterationElementType(
            INamedTypeSymbol containingType, ITypeSymbol collectionType, 
            INamedTypeSymbol ienumerableType, INamedTypeSymbol ienumeratorType,
            out ITypeSymbol iterationType)
        {
            // Check in the class/struct hierarchy first.
            var methods = collectionType.GetAccessibleMembersInThisAndBaseTypes<IMethodSymbol>(containingType);
            var getEnumeratorMethod = methods.FirstOrDefault(m => m.Name == nameof(IEnumerable.GetEnumerator));
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

            iterationType = default;
            return false;
        }

        private bool TryGetIterationElementTypeFromGetEnumerator(
            INamedTypeSymbol containingType, IMethodSymbol getEnumeratorMethod, 
            INamedTypeSymbol ienumeratorType, out ITypeSymbol iterationType)
        {
            var getEnumeratorReturnType = getEnumeratorMethod.ReturnType;

            // Check in the class/struct hierarchy first.
            var properties = getEnumeratorReturnType.GetAccessibleMembersInThisAndBaseTypes<IPropertySymbol>(containingType);
            var currentProperty = properties.FirstOrDefault(m => m.Name == nameof(IEnumerator.Current));
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

            iterationType = default;
            return false;
        }

        private async Task<Document> ConvertForToForEachAsync(
            Document document, TForStatementSyntax forStatement,
            SyntaxToken iterationVariable, TExpressionSyntax collectionExpression,
            ITypeSymbol iterationType, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var semanticFacts = document.GetLanguageService<ISemanticFactsService>();
            var generator = SyntaxGenerator.GetGenerator(document);

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var editor = new SyntaxEditor(root, generator);

            // create a dummy "list[i]" expression.  We'll use this to find all places to replace
            // in the current for statement.
            var indexExpression = generator.ElementAccessExpression(
                collectionExpression, generator.IdentifierName(iterationVariable));

            // See if the first statement in the for loop is of the form:
            //      var x = list[i]   or
            //
            // If so, we'll use those as the iteration variables for the new foreach statement.
            var bodyStatements = GetBodyStatements(forStatement);

            TTypeNode typeNode = default;
            SyntaxToken foreachIdentifier = default;
            SyntaxNode ignoreStatement = default;
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
                            var firstVariableInitializer = syntaxFacts.GetInitializerOfVariableDeclarator(firstVariable);
                            if (syntaxFacts.AreEquivalent(firstVariableInitializer, indexExpression))
                            {
                                typeNode = (TTypeNode)syntaxFacts.GetTypeOfVariableDeclarator(firstVariable);
                                foreachIdentifier = syntaxFacts.GetIdentifierOfVariableDeclarator(firstVariable);
                                editor.RemoveNode(firstStatement);
                                ignoreStatement = firstStatement;
                            }
                        }
                    }
                }
            }

            if (foreachIdentifier.RawKind == 0)
            {
                foreachIdentifier = generator.Identifier("v");
            }

            var foreachIdentifierName = generator.IdentifierName(foreachIdentifier).WithoutTrivia();
            foreachIdentifier = foreachIdentifier.WithAdditionalAnnotations(RenameAnnotation.Create());

            // Walk the for statement, replacing any matches we find.
            recurse(forStatement);

            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            editor.ReplaceNode(
                forStatement,
                (currentFor, _) => this.ConvertForNode(
                    (TForStatementSyntax)currentFor, typeNode, foreachIdentifier,
                    collectionExpression, iterationType, options));

            return document.WithSyntaxRoot(editor.GetChangedRoot());

            // local functions
            void recurse(SyntaxNode current)
            {
                // Do not replace in the first statement if we're just going to remove it
                // anyways.
                if (current == ignoreStatement)
                {
                    return;
                }

                if (syntaxFacts.AreEquivalent(current, indexExpression))
                {
                    // Found a match.  replace with iteration variable.
                    if (semanticFacts.IsWrittenTo(semanticModel, current, cancellationToken))
                    {
                        editor.ReplaceNode(current, foreachIdentifierName.WithAdditionalAnnotations(
                            WarningAnnotation.Create(FeaturesResources.Warning_colon_Collection_was_modified_during_iteration)));
                    }
                    else
                    {
                        editor.ReplaceNode(current, foreachIdentifierName);
                    }
                }

                foreach (var child in current.ChildNodesAndTokens())
                {
                    if (child.IsNode)
                    {
                        recurse(child.AsNode());
                    }
                }
            }
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) 
                : base(title, createChangedDocument, title)
            {
            }
        }
    }
}
