// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Utilities
{
    /// <summary>
    /// Helper class to analyze the semantic effects of a speculated syntax node replacement on the parenting nodes.
    /// Given an expression node from a syntax tree and a new expression from a different syntax tree,
    /// it replaces the expression with the new expression to create a speculated syntax tree.
    /// It uses the original tree's semantic model to create a speculative semantic model and verifies that
    /// the syntax replacement doesn't break the semantics of any parenting nodes of the original expression.
    /// </summary>
    internal class SpeculationAnalyzer : AbstractSpeculationAnalyzer<
        ExpressionSyntax,
        TypeSyntax,
        AttributeSyntax,
        ArgumentSyntax,
        CommonForEachStatementSyntax,
        ThrowStatementSyntax,
        Conversion>
    {
        /// <summary>
        /// Creates a semantic analyzer for speculative syntax replacement.
        /// </summary>
        /// <param name="expression">Original expression to be replaced.</param>
        /// <param name="newExpression">New expression to replace the original expression.</param>
        /// <param name="semanticModel">Semantic model of <paramref name="expression"/> node's syntax tree.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="skipVerificationForReplacedNode">
        /// True if semantic analysis should be skipped for the replaced node and performed starting from parent of the original and replaced nodes.
        /// This could be the case when custom verifications are required to be done by the caller or
        /// semantics of the replaced expression are different from the original expression.
        /// </param>
        /// <param name="failOnOverloadResolutionFailuresInOriginalCode">
        /// True if semantic analysis should fail when any of the invocation expression ancestors of <paramref name="expression"/> in original code has overload resolution failures.
        /// </param>
        public SpeculationAnalyzer(
            ExpressionSyntax expression,
            ExpressionSyntax newExpression,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            bool skipVerificationForReplacedNode = false,
            bool failOnOverloadResolutionFailuresInOriginalCode = false)
            : base(expression, newExpression, semanticModel, cancellationToken, skipVerificationForReplacedNode, failOnOverloadResolutionFailuresInOriginalCode)
        {
        }

        protected override SyntaxNode GetSemanticRootForSpeculation(ExpressionSyntax expression)
        {
            Debug.Assert(expression != null);

            var parentNodeToSpeculate = expression
                .AncestorsAndSelf(ascendOutOfTrivia: false)
                .Where(node => CanSpeculateOnNode(node))
                .LastOrDefault();

            return parentNodeToSpeculate ?? expression;
        }

        public static bool CanSpeculateOnNode(SyntaxNode node)
        {
            return (node is StatementSyntax && node.Kind() != SyntaxKind.Block) ||
                node is TypeSyntax ||
                node is CrefSyntax ||
                node.Kind() == SyntaxKind.Attribute ||
                node.Kind() == SyntaxKind.ThisConstructorInitializer ||
                node.Kind() == SyntaxKind.BaseConstructorInitializer ||
                node.Kind() == SyntaxKind.EqualsValueClause ||
                node.Kind() == SyntaxKind.ArrowExpressionClause;
        }

        protected override void ValidateSpeculativeSemanticModel(SemanticModel speculativeSemanticModel, SyntaxNode nodeToSpeculate)
        {
            Debug.Assert(speculativeSemanticModel != null ||
                nodeToSpeculate is ExpressionSyntax ||
                this.SemanticRootOfOriginalExpression.GetAncestors().Any(node => node.IsKind(SyntaxKind.UnknownAccessorDeclaration) ||
                    node.IsKind(SyntaxKind.IncompleteMember) ||
                    node.IsKind(SyntaxKind.BracketedArgumentList)),
                "SemanticModel.TryGetSpeculativeSemanticModel() API returned false.");
        }

        protected override SemanticModel CreateSpeculativeSemanticModel(SyntaxNode originalNode, SyntaxNode nodeToSpeculate, SemanticModel semanticModel)
        {
            return CreateSpeculativeSemanticModelForNode(originalNode, nodeToSpeculate, semanticModel);
        }

        public static SemanticModel CreateSpeculativeSemanticModelForNode(SyntaxNode originalNode, SyntaxNode nodeToSpeculate, SemanticModel semanticModel)
        {
            var position = originalNode.SpanStart;
            var isInNamespaceOrTypeContext = SyntaxFacts.IsInNamespaceOrTypeContext(originalNode as ExpressionSyntax);
            return CreateSpeculativeSemanticModelForNode(nodeToSpeculate, semanticModel, position, isInNamespaceOrTypeContext);
        }

        public static SemanticModel CreateSpeculativeSemanticModelForNode(SyntaxNode nodeToSpeculate, SemanticModel semanticModel, int position, bool isInNamespaceOrTypeContext)
        {
            if (semanticModel.IsSpeculativeSemanticModel)
            {
                // Chaining speculative model not supported, speculate off the original model.
                Debug.Assert(semanticModel.ParentModel != null);
                Debug.Assert(!semanticModel.ParentModel.IsSpeculativeSemanticModel);
                position = semanticModel.OriginalPositionForSpeculation;
                semanticModel = semanticModel.ParentModel;
            }

            SemanticModel speculativeModel;
            if (nodeToSpeculate is StatementSyntax statementNode)
            {
                semanticModel.TryGetSpeculativeSemanticModel(position, statementNode, out speculativeModel);
                return speculativeModel;
            }

            if (nodeToSpeculate is TypeSyntax typeNode)
            {
                var bindingOption = isInNamespaceOrTypeContext ?
                    SpeculativeBindingOption.BindAsTypeOrNamespace :
                    SpeculativeBindingOption.BindAsExpression;
                semanticModel.TryGetSpeculativeSemanticModel(position, typeNode, out speculativeModel, bindingOption);
                return speculativeModel;
            }

            if (nodeToSpeculate is CrefSyntax cref)
            {
                semanticModel.TryGetSpeculativeSemanticModel(position, cref, out speculativeModel);
                return speculativeModel;
            }

            switch (nodeToSpeculate.Kind())
            {
                case SyntaxKind.Attribute:
                    semanticModel.TryGetSpeculativeSemanticModel(position, (AttributeSyntax)nodeToSpeculate, out speculativeModel);
                    return speculativeModel;

                case SyntaxKind.BaseConstructorInitializer:
                case SyntaxKind.ThisConstructorInitializer:
                    semanticModel.TryGetSpeculativeSemanticModel(position, (ConstructorInitializerSyntax)nodeToSpeculate, out speculativeModel);
                    return speculativeModel;

                case SyntaxKind.EqualsValueClause:
                    semanticModel.TryGetSpeculativeSemanticModel(position, (EqualsValueClauseSyntax)nodeToSpeculate, out speculativeModel);
                    return speculativeModel;

                case SyntaxKind.ArrowExpressionClause:
                    semanticModel.TryGetSpeculativeSemanticModel(position, (ArrowExpressionClauseSyntax)nodeToSpeculate, out speculativeModel);
                    return speculativeModel;
            }

            // CONSIDER: Do we care about this case?
            Debug.Assert(nodeToSpeculate is ExpressionSyntax);
            return null;
        }

        /// <summary>
        /// Determines whether performing the syntax replacement in one of the sibling nodes of the given lambda expressions will change the lambda binding semantics.
        /// This is done by first determining the lambda parameters whose type differs in the replaced lambda node.
        /// For each of these parameters, we find the descendant identifier name nodes in the lambda body and check if semantics of any of the parenting nodes of these
        /// identifier nodes have changed in the replaced lambda.
        /// </summary>
        public bool ReplacementChangesSemanticsOfUnchangedLambda(ExpressionSyntax originalLambda, ExpressionSyntax replacedLambda)
        {
            originalLambda = originalLambda.WalkDownParentheses();
            replacedLambda = replacedLambda.WalkDownParentheses();

            SyntaxNode originalLambdaBody, replacedLambdaBody;
            List<string> paramNames;

            switch (originalLambda.Kind())
            {
                case SyntaxKind.ParenthesizedLambdaExpression:
                    {
                        var originalParenthesizedLambda = (ParenthesizedLambdaExpressionSyntax)originalLambda;
                        var originalParams = originalParenthesizedLambda.ParameterList.Parameters;
                        if (!originalParams.Any())
                        {
                            return false;
                        }

                        var replacedParenthesizedLambda = (ParenthesizedLambdaExpressionSyntax)replacedLambda;
                        var replacedParams = replacedParenthesizedLambda.ParameterList.Parameters;
                        Debug.Assert(originalParams.Count == replacedParams.Count);

                        paramNames = new List<string>();
                        for (var i = 0; i < originalParams.Count; i++)
                        {
                            var originalParam = originalParams[i];
                            var replacedParam = replacedParams[i];
                            if (!HaveSameParameterType(originalParam, replacedParam))
                            {
                                paramNames.Add(originalParam.Identifier.ValueText);
                            }
                        }

                        if (!paramNames.Any())
                        {
                            return false;
                        }

                        originalLambdaBody = originalParenthesizedLambda.Body;
                        replacedLambdaBody = replacedParenthesizedLambda.Body;
                        break;
                    }

                case SyntaxKind.SimpleLambdaExpression:
                    {
                        var originalSimpleLambda = (SimpleLambdaExpressionSyntax)originalLambda;
                        var replacedSimpleLambda = (SimpleLambdaExpressionSyntax)replacedLambda;

                        if (HaveSameParameterType(originalSimpleLambda.Parameter, replacedSimpleLambda.Parameter))
                        {
                            return false;
                        }

                        paramNames = new List<string>() { originalSimpleLambda.Parameter.Identifier.ValueText };
                        originalLambdaBody = originalSimpleLambda.Body;
                        replacedLambdaBody = replacedSimpleLambda.Body;
                        break;
                    }

                default:
                    throw ExceptionUtilities.UnexpectedValue(originalLambda.Kind());
            }

            var originalIdentifierNodes = originalLambdaBody.DescendantNodes().OfType<IdentifierNameSyntax>().Where(node => paramNames.Contains(node.Identifier.ValueText));
            if (!originalIdentifierNodes.Any())
            {
                return false;
            }

            var replacedIdentifierNodes = replacedLambdaBody.DescendantNodes().OfType<IdentifierNameSyntax>().Where(node => paramNames.Contains(node.Identifier.ValueText));
            return ReplacementChangesSemanticsForNodes(originalIdentifierNodes, replacedIdentifierNodes, originalLambdaBody, replacedLambdaBody);
        }

        private bool HaveSameParameterType(ParameterSyntax originalParam, ParameterSyntax replacedParam)
        {
            var originalParamType = this.OriginalSemanticModel.GetDeclaredSymbol(originalParam).Type;
            var replacedParamType = this.SpeculativeSemanticModel.GetDeclaredSymbol(replacedParam).Type;
            return Equals(originalParamType, replacedParamType);
        }

        private bool ReplacementChangesSemanticsForNodes(
            IEnumerable<IdentifierNameSyntax> originalIdentifierNodes,
            IEnumerable<IdentifierNameSyntax> replacedIdentifierNodes,
            SyntaxNode originalRoot,
            SyntaxNode replacedRoot)
        {
            Debug.Assert(originalIdentifierNodes.Any());
            Debug.Assert(originalIdentifierNodes.Count() == replacedIdentifierNodes.Count());

            var originalChildNodeEnum = originalIdentifierNodes.GetEnumerator();
            var replacedChildNodeEnum = replacedIdentifierNodes.GetEnumerator();

            while (originalChildNodeEnum.MoveNext())
            {
                replacedChildNodeEnum.MoveNext();
                if (ReplacementChangesSemantics(originalChildNodeEnum.Current, replacedChildNodeEnum.Current, originalRoot, skipVerificationForCurrentNode: true))
                {
                    return true;
                }
            }

            return false;
        }

        protected override bool ReplacementChangesSemanticsForNodeLanguageSpecific(SyntaxNode currentOriginalNode, SyntaxNode currentReplacedNode, SyntaxNode previousOriginalNode, SyntaxNode previousReplacedNode)
        {
            Debug.Assert(previousOriginalNode == null || previousOriginalNode.Parent == currentOriginalNode);
            Debug.Assert(previousReplacedNode == null || previousReplacedNode.Parent == currentReplacedNode);

            if (currentOriginalNode.IsKind(SyntaxKind.CaseSwitchLabel, SyntaxKind.ConstantPattern))
            {
                var expression = (ExpressionSyntax)currentReplacedNode.ChildNodes().First();
                if (expression.WalkDownParentheses().IsKind(SyntaxKind.DefaultLiteralExpression))
                {
                    // We can't have a default literal inside a case label or constant pattern.
                    return true;
                }
            }

            if (currentOriginalNode is BinaryExpressionSyntax binaryExpression)
            {
                // If replacing the node will result in a broken binary expression, we won't remove it.
                return ReplacementBreaksBinaryExpression(binaryExpression, (BinaryExpressionSyntax)currentReplacedNode);
            }
            else if (currentOriginalNode.Kind() == SyntaxKind.LogicalNotExpression)
            {
                return !TypesAreCompatible(((PrefixUnaryExpressionSyntax)currentOriginalNode).Operand, ((PrefixUnaryExpressionSyntax)currentReplacedNode).Operand);
            }
            else if (currentOriginalNode.Kind() == SyntaxKind.ConditionalAccessExpression)
            {
                return ReplacementBreaksConditionalAccessExpression((ConditionalAccessExpressionSyntax)currentOriginalNode, (ConditionalAccessExpressionSyntax)currentReplacedNode);
            }
            else if (currentOriginalNode is AssignmentExpressionSyntax assignment)
            {
                // If replacing the node will result in a broken assignment expression, we won't remove it.
                return ReplacementBreaksAssignmentExpression(assignment, (AssignmentExpressionSyntax)currentReplacedNode);
            }
            else if (currentOriginalNode is SelectOrGroupClauseSyntax || currentOriginalNode is OrderingSyntax)
            {
                return !SymbolsAreCompatible(currentOriginalNode, currentReplacedNode);
            }
            else if (currentOriginalNode is QueryClauseSyntax queryClause)
            {
                return ReplacementBreaksQueryClause(queryClause, (QueryClauseSyntax)currentReplacedNode);
            }
            else if (currentOriginalNode.Kind() == SyntaxKind.VariableDeclarator)
            {
                // Heuristic: If replacing the node will result in changing the type of a local variable
                // that is type-inferred, we won't remove it. It's possible to do this analysis, but it's
                // very expensive and the benefit to the user is small.
                var originalDeclarator = (VariableDeclaratorSyntax)currentOriginalNode;
                var newDeclarator = (VariableDeclaratorSyntax)currentReplacedNode;

                if (originalDeclarator.Initializer == null)
                {
                    return newDeclarator.Initializer != null;
                }
                else if (newDeclarator.Initializer == null)
                {
                    return true;
                }

                if (!originalDeclarator.Initializer.IsMissing &&
                    originalDeclarator.IsTypeInferred(this.OriginalSemanticModel) &&
                    !TypesAreCompatible(originalDeclarator.Initializer.Value, newDeclarator.Initializer.Value))
                {
                    return true;
                }

                return false;
            }
            else if (currentOriginalNode.Kind() == SyntaxKind.ConditionalExpression)
            {
                var originalExpression = (ConditionalExpressionSyntax)currentOriginalNode;
                var newExpression = (ConditionalExpressionSyntax)currentReplacedNode;

                if (originalExpression.Condition != previousOriginalNode)
                {
                    ExpressionSyntax originalOtherPartOfConditional, newOtherPartOfConditional;

                    if (originalExpression.WhenTrue == previousOriginalNode)
                    {
                        Debug.Assert(newExpression.WhenTrue == previousReplacedNode);
                        originalOtherPartOfConditional = originalExpression.WhenFalse;
                        newOtherPartOfConditional = newExpression.WhenFalse;
                    }
                    else
                    {
                        Debug.Assert(newExpression.WhenFalse == previousReplacedNode);
                        originalOtherPartOfConditional = originalExpression.WhenTrue;
                        newOtherPartOfConditional = newExpression.WhenTrue;
                    }

                    var originalExpressionTypeInfo = this.OriginalSemanticModel.GetTypeInfo(originalExpression, this.CancellationToken);
                    var newExpressionTypeInfo = this.SpeculativeSemanticModel.GetTypeInfo(newExpression, this.CancellationToken);

                    var originalExpressionType = originalExpressionTypeInfo.Type;
                    var newExpressionType = newExpressionTypeInfo.Type;

                    if (originalExpressionType == null || newExpressionType == null)
                    {
                        // With the current implementation of the C# binder, this is impossible, but it's probably not wise to
                        // depend on an implementation detail of another layer.
                        return originalExpressionType != newExpressionType;
                    }

                    var originalConversion = this.OriginalSemanticModel.ClassifyConversion(originalOtherPartOfConditional, originalExpressionType);
                    var newConversion = this.SpeculativeSemanticModel.ClassifyConversion(newOtherPartOfConditional, newExpressionType);

                    // If this changes a boxing operation in one of the branches, we assume that semantics will change.
                    if (originalConversion.IsBoxing != newConversion.IsBoxing)
                    {
                        return true;
                    }

                    if (ReplacementBreaksBoxingInConditionalExpression(originalExpressionTypeInfo, newExpressionTypeInfo, (ExpressionSyntax)previousOriginalNode, (ExpressionSyntax)previousReplacedNode))
                    {
                        return true;
                    }

                    if (!ConversionsAreCompatible(originalConversion, newConversion))
                    {
                        return true;
                    }
                }
            }
            else if (currentOriginalNode.Kind() == SyntaxKind.CaseSwitchLabel)
            {
                var originalCaseSwitchLabel = (CaseSwitchLabelSyntax)currentOriginalNode;
                var newCaseSwitchLabel = (CaseSwitchLabelSyntax)currentReplacedNode;

                // If case label is changing, then need to check if the semantics will change for the switch expression.  
                // e.g. if switch expression is "switch(x)" where "object x = 1f", then "case 1:" and "case (float) 1:" are different.
                var originalCaseType = this.OriginalSemanticModel.GetTypeInfo(previousOriginalNode, this.CancellationToken).Type;
                var newCaseType = this.SpeculativeSemanticModel.GetTypeInfo(previousReplacedNode, this.CancellationToken).Type;

                if (Equals(originalCaseType, newCaseType))
                    return false;

                var oldSwitchStatement = (SwitchStatementSyntax)originalCaseSwitchLabel.Parent.Parent;
                var newSwitchStatement = (SwitchStatementSyntax)newCaseSwitchLabel.Parent.Parent;

                var originalConversion = this.OriginalSemanticModel.ClassifyConversion(oldSwitchStatement.Expression, originalCaseType);
                var newConversion = this.SpeculativeSemanticModel.ClassifyConversion(newSwitchStatement.Expression, newCaseType);

                // if conversion only exists for either original or new, then semantics changed.
                if (originalConversion.Exists != newConversion.Exists)
                {
                    return true;
                }

                // Same conversion cannot result in both originalCaseType and newCaseType, which means the semantics changed
                // (since originalCaseType != newCaseType)
                return originalConversion == newConversion;
            }
            else if (currentOriginalNode.Kind() == SyntaxKind.SwitchStatement)
            {
                var originalSwitchStatement = (SwitchStatementSyntax)currentOriginalNode;
                var newSwitchStatement = (SwitchStatementSyntax)currentReplacedNode;

                if (originalSwitchStatement.Expression == previousOriginalNode)
                {
                    // Switch expression changed, verify that the conversions from switch case labels to new switch expression type are not broken.

                    var originalExpressionType = this.OriginalSemanticModel.GetTypeInfo(originalSwitchStatement.Expression, this.CancellationToken).Type;
                    var newExpressionType = this.SpeculativeSemanticModel.GetTypeInfo(newSwitchStatement.Expression, this.CancellationToken).Type;

                    var originalSwitchLabels = originalSwitchStatement.Sections.SelectMany(section => section.Labels).ToArray();
                    var newSwitchLabels = newSwitchStatement.Sections.SelectMany(section => section.Labels).ToArray();
                    for (var i = 0; i < originalSwitchLabels.Length; i++)
                    {
                        if (originalSwitchLabels[i] is CaseSwitchLabelSyntax originalSwitchLabel)
                        {
                            if (newSwitchLabels[i] is CaseSwitchLabelSyntax newSwitchLabel && !ImplicitConversionsAreCompatible(originalSwitchLabel.Value, newSwitchLabel.Value))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            else if (currentOriginalNode.Kind() == SyntaxKind.IfStatement)
            {
                var originalIfStatement = (IfStatementSyntax)currentOriginalNode;
                var newIfStatement = (IfStatementSyntax)currentReplacedNode;

                if (originalIfStatement.Condition == previousOriginalNode)
                {
                    // If condition changed, verify that original and replaced expression types are compatible.
                    if (!TypesAreCompatible(originalIfStatement.Condition, newIfStatement.Condition))
                    {
                        return true;
                    }
                }
            }
            else if (currentOriginalNode is ConstructorInitializerSyntax originalCtorInitializer)
            {
                var newCtorInitializer = (ConstructorInitializerSyntax)currentReplacedNode;
                return ReplacementBreaksConstructorInitializer(originalCtorInitializer, newCtorInitializer);
            }
            else if (currentOriginalNode.Kind() == SyntaxKind.CollectionInitializerExpression)
            {
                return previousOriginalNode != null &&
                    ReplacementBreaksCollectionInitializerAddMethod((ExpressionSyntax)previousOriginalNode, (ExpressionSyntax)previousReplacedNode);
            }
            else if (currentOriginalNode.Kind() == SyntaxKind.Interpolation)
            {
                return ReplacementBreaksInterpolation((InterpolationSyntax)currentOriginalNode, (InterpolationSyntax)currentReplacedNode);
            }
            else if (currentOriginalNode.Kind() == SyntaxKind.ImplicitArrayCreationExpression)
            {
                return !TypesAreCompatible((ImplicitArrayCreationExpressionSyntax)currentOriginalNode, (ImplicitArrayCreationExpressionSyntax)currentReplacedNode);
            }
            else if (currentOriginalNode is AnonymousObjectMemberDeclaratorSyntax originalAnonymousObjectMemberDeclarator)
            {
                var replacedAnonymousObjectMemberDeclarator = (AnonymousObjectMemberDeclaratorSyntax)currentReplacedNode;
                return ReplacementBreaksAnonymousObjectMemberDeclarator(originalAnonymousObjectMemberDeclarator, replacedAnonymousObjectMemberDeclarator);
            }
            else if (currentOriginalNode.Kind() == SyntaxKind.DefaultExpression)
            {
                return !TypesAreCompatible((ExpressionSyntax)currentOriginalNode, (ExpressionSyntax)currentReplacedNode);
            }

            return false;
        }

        /// <summary>
        /// Checks if the conversion might change the resultant boxed type.
        /// Similar boxing checks are performed elsewhere, but in this case we need to perform the check on the entire conditional expression.
        /// This will make sure the resultant cast is proper for the type of the conditional expression.
        /// </summary>
        private bool ReplacementBreaksBoxingInConditionalExpression(TypeInfo originalExpressionTypeInfo, TypeInfo newExpressionTypeInfo, ExpressionSyntax previousOriginalNode, ExpressionSyntax previousReplacedNode)
        {
            // If the resultant types are different and it is boxing to the converted type then semantics could be changing.
            if (!Equals(originalExpressionTypeInfo.Type, newExpressionTypeInfo.Type))
            {
                var originalConvertedTypeConversion = this.OriginalSemanticModel.ClassifyConversion(previousOriginalNode, originalExpressionTypeInfo.ConvertedType);
                var newExpressionConvertedTypeConversion = this.SpeculativeSemanticModel.ClassifyConversion(previousReplacedNode, newExpressionTypeInfo.ConvertedType);

                if (originalConvertedTypeConversion.IsBoxing && newExpressionConvertedTypeConversion.IsBoxing)
                {
                    return true;
                }
            }

            return false;
        }

        private bool ReplacementBreaksAnonymousObjectMemberDeclarator(AnonymousObjectMemberDeclaratorSyntax originalAnonymousObjectMemberDeclarator, AnonymousObjectMemberDeclaratorSyntax replacedAnonymousObjectMemberDeclarator)
        {
            var originalExpressionType = this.OriginalSemanticModel.GetTypeInfo(originalAnonymousObjectMemberDeclarator.Expression, this.CancellationToken).Type;
            var newExpressionType = this.SpeculativeSemanticModel.GetTypeInfo(replacedAnonymousObjectMemberDeclarator.Expression, this.CancellationToken).Type;
            return !object.Equals(originalExpressionType, newExpressionType);
        }

        private bool ReplacementBreaksConstructorInitializer(ConstructorInitializerSyntax ctorInitializer, ConstructorInitializerSyntax newCtorInitializer)
        {
            var originalSymbol = this.OriginalSemanticModel.GetSymbolInfo(ctorInitializer, CancellationToken).Symbol;
            var newSymbol = this.SpeculativeSemanticModel.GetSymbolInfo(newCtorInitializer, CancellationToken).Symbol;
            return !SymbolsAreCompatible(originalSymbol, newSymbol);
        }

        private bool ReplacementBreaksCollectionInitializerAddMethod(ExpressionSyntax originalInitializer, ExpressionSyntax newInitializer)
        {
            var originalSymbol = this.OriginalSemanticModel.GetCollectionInitializerSymbolInfo(originalInitializer, CancellationToken).Symbol;
            var newSymbol = this.SpeculativeSemanticModel.GetCollectionInitializerSymbolInfo(newInitializer, CancellationToken).Symbol;
            return !SymbolsAreCompatible(originalSymbol, newSymbol);
        }

        protected override bool ExpressionMightReferenceMember(SyntaxNode node)
        {
            return node.IsKind(
                SyntaxKind.InvocationExpression,
                SyntaxKind.ElementAccessExpression,
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxKind.ImplicitElementAccess,
                SyntaxKind.ObjectCreationExpression);
        }

        protected override ImmutableArray<ArgumentSyntax> GetArguments(ExpressionSyntax expression)
        {
            var argumentsList = GetArgumentList(expression);
            return argumentsList != null ?
                argumentsList.Arguments.AsImmutableOrEmpty() :
                default;
        }

        private static BaseArgumentListSyntax GetArgumentList(ExpressionSyntax expression)
        {
            expression = expression.WalkDownParentheses();

            return expression.Kind() switch
            {
                SyntaxKind.InvocationExpression => ((InvocationExpressionSyntax)expression).ArgumentList,
                SyntaxKind.ObjectCreationExpression => ((ObjectCreationExpressionSyntax)expression).ArgumentList,
                SyntaxKind.ElementAccessExpression => ((ElementAccessExpressionSyntax)expression).ArgumentList,
                _ => (BaseArgumentListSyntax)null,
            };
        }

        protected override ExpressionSyntax GetReceiver(ExpressionSyntax expression)
        {
            expression = expression.WalkDownParentheses();

            switch (expression.Kind())
            {
                case SyntaxKind.SimpleMemberAccessExpression:
                    return ((MemberAccessExpressionSyntax)expression).Expression;

                case SyntaxKind.InvocationExpression:
                    {
                        var result = ((InvocationExpressionSyntax)expression).Expression;
                        if (result.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                        {
                            return GetReceiver(result);
                        }

                        return result;
                    }

                case SyntaxKind.ElementAccessExpression:
                    {
                        var result = ((ElementAccessExpressionSyntax)expression).Expression;
                        if (result.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                        {
                            result = GetReceiver(result);
                        }

                        return result;
                    }

                default:
                    return null;
            }
        }

        protected override bool IsInNamespaceOrTypeContext(ExpressionSyntax node)
        {
            return SyntaxFacts.IsInNamespaceOrTypeContext(node);
        }

        protected override ExpressionSyntax GetForEachStatementExpression(CommonForEachStatementSyntax forEachStatement)
        {
            return forEachStatement.Expression;
        }

        protected override ExpressionSyntax GetThrowStatementExpression(ThrowStatementSyntax throwStatement)
        {
            return throwStatement.Expression;
        }

        protected override bool IsForEachTypeInferred(CommonForEachStatementSyntax forEachStatement, SemanticModel semanticModel)
        {
            return forEachStatement.IsTypeInferred(semanticModel);
        }

        protected override bool IsParenthesizedExpression(SyntaxNode node)
        {
            return node.IsKind(SyntaxKind.ParenthesizedExpression);
        }

        protected override bool IsNamedArgument(ArgumentSyntax argument)
        {
            return argument.NameColon != null && !argument.NameColon.IsMissing;
        }

        protected override string GetNamedArgumentIdentifierValueText(ArgumentSyntax argument)
        {
            return argument.NameColon.Name.Identifier.ValueText;
        }

        private bool ReplacementBreaksBinaryExpression(BinaryExpressionSyntax binaryExpression, BinaryExpressionSyntax newBinaryExpression)
        {
            if ((binaryExpression.IsKind(SyntaxKind.AsExpression) ||
                 binaryExpression.IsKind(SyntaxKind.IsExpression)) &&
                 ReplacementBreaksIsOrAsExpression(binaryExpression, newBinaryExpression))
            {
                return true;
            }

            return !SymbolsAreCompatible(binaryExpression, newBinaryExpression) ||
                !TypesAreCompatible(binaryExpression, newBinaryExpression) ||
                !ImplicitConversionsAreCompatible(binaryExpression, newBinaryExpression);
        }

        private bool ReplacementBreaksConditionalAccessExpression(ConditionalAccessExpressionSyntax conditionalAccessExpression, ConditionalAccessExpressionSyntax newConditionalAccessExpression)
        {
            return !SymbolsAreCompatible(conditionalAccessExpression, newConditionalAccessExpression) ||
                !TypesAreCompatible(conditionalAccessExpression, newConditionalAccessExpression) ||
                !SymbolsAreCompatible(conditionalAccessExpression.WhenNotNull, newConditionalAccessExpression.WhenNotNull) ||
                !TypesAreCompatible(conditionalAccessExpression.WhenNotNull, newConditionalAccessExpression.WhenNotNull);
        }

        private bool ReplacementBreaksInterpolation(InterpolationSyntax interpolation, InterpolationSyntax newInterpolation)
        {
            return !TypesAreCompatible(interpolation.Expression, newInterpolation.Expression);
        }

        private bool ReplacementBreaksIsOrAsExpression(BinaryExpressionSyntax originalIsOrAsExpression, BinaryExpressionSyntax newIsOrAsExpression)
        {
            // Special case: Lambda expressions and anonymous delegates cannot appear
            // on the left-side of an 'is' or 'as' cast. We can handle this case syntactically.
            if (!originalIsOrAsExpression.Left.WalkDownParentheses().IsAnyLambdaOrAnonymousMethod() &&
                newIsOrAsExpression.Left.WalkDownParentheses().IsAnyLambdaOrAnonymousMethod())
            {
                return true;
            }

            var originalConvertedType = this.OriginalSemanticModel.GetTypeInfo(originalIsOrAsExpression.Right).Type;
            var newConvertedType = this.SpeculativeSemanticModel.GetTypeInfo(newIsOrAsExpression.Right).Type;

            if (originalConvertedType == null || newConvertedType == null)
            {
                return originalConvertedType != newConvertedType;
            }

            var originalConversion = this.OriginalSemanticModel.ClassifyConversion(originalIsOrAsExpression.Left, originalConvertedType, isExplicitInSource: true);
            var newConversion = this.SpeculativeSemanticModel.ClassifyConversion(newIsOrAsExpression.Left, newConvertedType, isExplicitInSource: true);

            // Is and As operators do not consider any user-defined operators, just ensure that the conversion exists.
            return originalConversion.Exists != newConversion.Exists;
        }

        private bool ReplacementBreaksAssignmentExpression(AssignmentExpressionSyntax assignmentExpression, AssignmentExpressionSyntax newAssignmentExpression)
        {
            if (assignmentExpression.IsCompoundAssignExpression() &&
                assignmentExpression.Kind() != SyntaxKind.LeftShiftAssignmentExpression &&
                assignmentExpression.Kind() != SyntaxKind.RightShiftAssignmentExpression &&
                ReplacementBreaksCompoundAssignment(assignmentExpression.Left, assignmentExpression.Right, newAssignmentExpression.Left, newAssignmentExpression.Right))
            {
                return true;
            }

            return !SymbolsAreCompatible(assignmentExpression, newAssignmentExpression) ||
                !TypesAreCompatible(assignmentExpression, newAssignmentExpression) ||
                !ImplicitConversionsAreCompatible(assignmentExpression, newAssignmentExpression);
        }

        private bool ReplacementBreaksQueryClause(QueryClauseSyntax originalClause, QueryClauseSyntax newClause)
        {
            // Ensure QueryClauseInfos are compatible.
            var originalClauseInfo = this.OriginalSemanticModel.GetQueryClauseInfo(originalClause, this.CancellationToken);
            var newClauseInfo = this.SpeculativeSemanticModel.GetQueryClauseInfo(newClause, this.CancellationToken);

            return !SymbolInfosAreCompatible(originalClauseInfo.CastInfo, newClauseInfo.CastInfo) ||
                !SymbolInfosAreCompatible(originalClauseInfo.OperationInfo, newClauseInfo.OperationInfo);
        }

        protected override bool ConversionsAreCompatible(SemanticModel originalModel, ExpressionSyntax originalExpression, SemanticModel newModel, ExpressionSyntax newExpression)
        {
            return ConversionsAreCompatible(originalModel.GetConversion(originalExpression), newModel.GetConversion(newExpression));
        }

        protected override bool ConversionsAreCompatible(ExpressionSyntax originalExpression, ITypeSymbol originalTargetType, ExpressionSyntax newExpression, ITypeSymbol newTargetType)
        {
            this.GetConversions(originalExpression, originalTargetType, newExpression, newTargetType, out var originalConversion, out var newConversion);

            if (originalConversion == null || newConversion == null)
            {
                return false;
            }

            return ConversionsAreCompatible(originalConversion.Value, newConversion.Value);
        }

        private bool ConversionsAreCompatible(Conversion originalConversion, Conversion newConversion)
        {
            if (originalConversion.Exists != newConversion.Exists ||
                (!originalConversion.IsExplicit && newConversion.IsExplicit))
            {
                return false;
            }

            var originalIsUserDefined = originalConversion.IsUserDefined;
            var newIsUserDefined = newConversion.IsUserDefined;

            if (originalIsUserDefined != newIsUserDefined)
            {
                return false;
            }

            if (originalIsUserDefined || originalConversion.MethodSymbol != null || newConversion.MethodSymbol != null)
            {
                return SymbolsAreCompatible(originalConversion.MethodSymbol, newConversion.MethodSymbol);
            }

            return true;
        }

        protected override bool ForEachConversionsAreCompatible(SemanticModel originalModel, CommonForEachStatementSyntax originalForEach, SemanticModel newModel, CommonForEachStatementSyntax newForEach)
        {
            var originalInfo = originalModel.GetForEachStatementInfo(originalForEach);
            var newInfo = newModel.GetForEachStatementInfo(newForEach);
            return ConversionsAreCompatible(originalInfo.CurrentConversion, newInfo.CurrentConversion)
                && ConversionsAreCompatible(originalInfo.ElementConversion, newInfo.ElementConversion);
        }

        protected override void GetForEachSymbols(SemanticModel model, CommonForEachStatementSyntax forEach, out IMethodSymbol getEnumeratorMethod, out ITypeSymbol elementType)
        {
            var info = model.GetForEachStatementInfo(forEach);
            getEnumeratorMethod = info.GetEnumeratorMethod;
            elementType = info.ElementType;
        }

        protected override bool IsReferenceConversion(Compilation compilation, ITypeSymbol sourceType, ITypeSymbol targetType)
        {
            return compilation.ClassifyConversion(sourceType, targetType).IsReference;
        }

        protected override Conversion ClassifyConversion(SemanticModel model, ExpressionSyntax expression, ITypeSymbol targetType) =>
            model.ClassifyConversion(expression, targetType);

        protected override Conversion ClassifyConversion(SemanticModel model, ITypeSymbol originalType, ITypeSymbol targetType) =>
            model.Compilation.ClassifyConversion(originalType, targetType);
    }
}
