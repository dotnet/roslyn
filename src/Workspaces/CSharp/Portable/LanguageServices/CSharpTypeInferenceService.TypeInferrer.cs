// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class CSharpTypeInferenceService
    {
        private class TypeInferrer
        {
            private readonly SemanticModel _semanticModel;
            private readonly CancellationToken _cancellationToken;
            private readonly HashSet<ExpressionSyntax> _seenExpressionInferType = new HashSet<ExpressionSyntax>();
            private readonly HashSet<ExpressionSyntax> _seenExpressionGetType = new HashSet<ExpressionSyntax>();

            internal TypeInferrer(
                SemanticModel semanticModel,
                CancellationToken cancellationToken)
            {
                _semanticModel = semanticModel;
                _cancellationToken = cancellationToken;
            }

            private Compilation Compilation
            {
                get
                {
                    return _semanticModel.Compilation;
                }
            }

            public IEnumerable<ITypeSymbol> InferTypes(ExpressionSyntax expression)
            {
                if (expression != null)
                {
                    if (_seenExpressionInferType.Add(expression))
                    {
                        var types = InferTypesWorker(expression);
                        if (types.Any())
                        {
                            return types.Where(t => !IsUnusableType(t)).Distinct();
                        }
                    }
                }

                return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
            }

            public IEnumerable<ITypeSymbol> InferTypes(int position)
            {
                var types = InferTypesWorker(position);
                if (types.Any())
                {
                    return types.Where(t => !IsUnusableType(t)).Distinct();
                }

                return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
            }

            private static bool IsUnusableType(ITypeSymbol otherSideType)
            {
                if (otherSideType == null)
                {
                    return true;
                }

                return otherSideType.IsErrorType() &&
                    (otherSideType.Name == string.Empty || otherSideType.Name == "var");
            }

            // TODO: Add support for Expression<T>
            private IEnumerable<ITypeSymbol> GetTypes(ExpressionSyntax expression)
            {
                if (_seenExpressionGetType.Add(expression))
                {
                    IEnumerable<ITypeSymbol> types;

                    // BUG: (vladres) Are following expressions parenthesized correctly?
                    // BUG:
                    // BUG: (davip) It is parenthesized incorrectly. This problem was introduced in Changeset 822325 when 
                    // BUG: this method was changed from returning a single ITypeSymbol to returning an IEnumerable<ITypeSymbol> 
                    // BUG: to better deal with overloads. The old version was:
                    // BUG: 
                    // BUG:     if (!IsUnusableType(type = GetTypeSimple(expression)) ||
                    // BUG:         !IsUnusableType(type = GetTypeComplex(expression)))
                    // BUG:           { return type; }
                    // BUG: 
                    // BUG: The intent is to only use *usable* types, whether simple or complex. I have confirmed this intent with Ravi, who made the change. 
                    // BUG: 
                    // BUG: Note that the current implementation of GetTypesComplex and GetTypesSimple already ensure the returned value 
                    // BUG: is a usable type, so there should not (currently) be any observable effect of this logic error.
                    // BUG:
                    // BUG: (vladres) Please remove this comment once the bug is fixed.
                    if ((types = GetTypesSimple(expression).Where(t => !IsUnusableType(t))).Any() ||
                        (types = GetTypesComplex(expression)).Where(t => !IsUnusableType(t)).Any())
                    {
                        return types;
                    }
                }

                return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
            }

            private static bool DecomposeBinaryOrAssignmentExpression(ExpressionSyntax expression, out SyntaxToken operatorToken, out ExpressionSyntax left, out ExpressionSyntax right)
            {
                var binaryExpression = expression as BinaryExpressionSyntax;
                if (binaryExpression != null)
                {
                    operatorToken = binaryExpression.OperatorToken;
                    left = binaryExpression.Left;
                    right = binaryExpression.Right;
                    return true;
                }

                var assignmentExpression = expression as AssignmentExpressionSyntax;
                if (assignmentExpression != null)
                {
                    operatorToken = assignmentExpression.OperatorToken;
                    left = assignmentExpression.Left;
                    right = assignmentExpression.Right;
                    return true;
                }

                operatorToken = default(SyntaxToken);
                left = right = null;
                return false;
            }

            private IEnumerable<ITypeSymbol> GetTypesComplex(ExpressionSyntax expression)
            {
                SyntaxToken operatorToken;
                ExpressionSyntax left, right;
                if (DecomposeBinaryOrAssignmentExpression(expression, out operatorToken, out left, out right))
                {
                    var types = InferTypeInBinaryOrAssignmentExpression(expression, operatorToken, left, right, left).Where(t => !IsUnusableType(t));
                    if (types.IsEmpty())
                    {
                        types = InferTypeInBinaryOrAssignmentExpression(expression, operatorToken, left, right, right).Where(t => !IsUnusableType(t));
                    }

                    return types;
                }

                // TODO(cyrusn): More cases if necessary.
                return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
            }

            private IEnumerable<ITypeSymbol> GetTypesSimple(ExpressionSyntax expression)
            {
                if (expression != null)
                {
                    var typeInfo = _semanticModel.GetTypeInfo(expression, _cancellationToken);
                    var symbolInfo = _semanticModel.GetSymbolInfo(expression, _cancellationToken);

                    if (symbolInfo.CandidateReason != CandidateReason.WrongArity)
                    {
                        ITypeSymbol type = typeInfo.Type;

                        // If it bound to a method, try to get the Action/Func form of that method.
                        if (type == null &&
                            symbolInfo.GetAllSymbols().Count() == 1 &&
                            symbolInfo.GetAllSymbols().First().Kind == SymbolKind.Method)
                        {
                            var method = symbolInfo.GetAllSymbols().First();
                            type = method.ConvertToType(this.Compilation);
                        }

                        if (!IsUnusableType(type))
                        {
                            return SpecializedCollections.SingletonEnumerable(type);
                        }
                    }
                }

                return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
            }

            private IEnumerable<ITypeSymbol> InferTypesWorker(ExpressionSyntax expression)
            {
                expression = expression.WalkUpParentheses();
                var parent = expression.Parent;

                if (parent is ConditionalAccessExpressionSyntax)
                {
                    parent = parent.Parent;
                }


                if (parent is MemberAccessExpressionSyntax)
                {
                    var awaitExpression = parent.GetAncestor<AwaitExpressionSyntax>();
                    if (awaitExpression != null)
                    {
                        parent = awaitExpression;
                    }
                }

                return parent.TypeSwitch(
                    (AnonymousObjectMemberDeclaratorSyntax memberDeclarator) => InferTypeInMemberDeclarator(memberDeclarator),
                    (ArgumentSyntax argument) => InferTypeInArgument(argument),
                    (CheckedExpressionSyntax checkedExpression) => InferTypes(checkedExpression),
                    (ArrayCreationExpressionSyntax arrayCreationExpression) => InferTypeInArrayCreationExpression(arrayCreationExpression),
                    (ArrayRankSpecifierSyntax arrayRankSpecifier) => InferTypeInArrayRankSpecifier(arrayRankSpecifier),
                    (ArrayTypeSyntax arrayType) => InferTypeInArrayType(arrayType),
                    (AttributeArgumentSyntax attribute) => InferTypeInAttributeArgument(attribute),
                    (AttributeSyntax attribute) => InferTypeInAttribute(attribute),
                    (BinaryExpressionSyntax binaryExpression) => InferTypeInBinaryOrAssignmentExpression(binaryExpression, binaryExpression.OperatorToken, binaryExpression.Left, binaryExpression.Right, expression),
                    (AssignmentExpressionSyntax assignmentExpression) => InferTypeInBinaryOrAssignmentExpression(assignmentExpression, assignmentExpression.OperatorToken, assignmentExpression.Left, assignmentExpression.Right, expression),
                    (CastExpressionSyntax castExpression) => InferTypeInCastExpression(castExpression, expression),
                    (CatchDeclarationSyntax catchDeclaration) => InferTypeInCatchDeclaration(catchDeclaration),
                    (CatchFilterClauseSyntax catchFilterClause) => InferTypeInCatchFilterClause(catchFilterClause),
                    (ConditionalExpressionSyntax conditionalExpression) => InferTypeInConditionalExpression(conditionalExpression, expression),
                    (DoStatementSyntax doStatement) => InferTypeInDoStatement(doStatement),
                    (EqualsValueClauseSyntax equalsValue) => InferTypeInEqualsValueClause(equalsValue),
                    (ExpressionStatementSyntax expressionStatement) => InferTypeInExpressionStatement(expressionStatement),
                    (ForEachStatementSyntax forEachStatement) => InferTypeInForEachStatement(forEachStatement, expression),
                    (ForStatementSyntax forStatement) => InferTypeInForStatement(forStatement, expression),
                    (IfStatementSyntax ifStatement) => InferTypeInIfStatement(ifStatement),
                    (InitializerExpressionSyntax initializerExpression) => InferTypeInInitializerExpression(initializerExpression, expression),
                    (LockStatementSyntax lockStatement) => InferTypeInLockStatement(lockStatement),
                    (NameEqualsSyntax nameEquals) => InferTypeInNameEquals(nameEquals),
                    (ParenthesizedLambdaExpressionSyntax parenthesizedLambdaExpression) => InferTypeInParenthesizedLambdaExpression(parenthesizedLambdaExpression),
                    (PostfixUnaryExpressionSyntax postfixUnary) => InferTypeInPostfixUnaryExpression(postfixUnary),
                    (PrefixUnaryExpressionSyntax prefixUnary) => InferTypeInPrefixUnaryExpression(prefixUnary),
                    (AwaitExpressionSyntax awaitExpression) => InferTypeInAwaitExpression(awaitExpression),
                    (ArrowExpressionClauseSyntax arrowClause) => InferTypeInArrowExpressionClause(arrowClause),
                    (ReturnStatementSyntax returnStatement) => InferTypeForReturnStatement(returnStatement),
                    (SimpleLambdaExpressionSyntax simpleLambdaExpression) => InferTypeInSimpleLambdaExpression(simpleLambdaExpression),
                    (SwitchLabelSyntax switchLabel) => InferTypeInSwitchLabel(switchLabel),
                    (SwitchStatementSyntax switchStatement) => InferTypeInSwitchStatement(switchStatement),
                    (ThrowStatementSyntax throwStatement) => InferTypeInThrowStatement(throwStatement),
                    (UsingStatementSyntax usingStatement) => InferTypeInUsingStatement(usingStatement),
                    (WhileStatementSyntax whileStatement) => InferTypeInWhileStatement(whileStatement),
                    (YieldStatementSyntax yieldStatement) => InferTypeInYieldStatement(yieldStatement),
                    _ => SpecializedCollections.EmptyEnumerable<ITypeSymbol>());
            }

            private IEnumerable<ITypeSymbol> InferTypeInArrowExpressionClause(ArrowExpressionClauseSyntax arrowClause)
            {
                if (arrowClause.IsParentKind(SyntaxKind.PropertyDeclaration))
                {
                    return InferTypeInPropertyDeclaration(arrowClause.Parent as PropertyDeclarationSyntax);
                }

                if (arrowClause.Parent is BaseMethodDeclarationSyntax)
                {
                    return InferTypeInBaseMethodDeclaration(arrowClause.Parent as BaseMethodDeclarationSyntax);
                }

                return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
            }

            private IEnumerable<ITypeSymbol> InferTypesWorker(int position)
            {
                var syntaxTree = (SyntaxTree)_semanticModel.SyntaxTree;
                var token = syntaxTree.FindTokenOnLeftOfPosition(position, _cancellationToken);
                token = token.GetPreviousTokenIfTouchingWord(position);

                var parent = token.Parent;
                return parent.TypeSwitch(
                    (AnonymousObjectMemberDeclaratorSyntax memberDeclarator) => InferTypeInMemberDeclarator(memberDeclarator, token),
                    (ArgumentSyntax argument) => InferTypeInArgument(argument, token),
                    (ArgumentListSyntax argument) => InferTypeInArgumentList(argument, token),
                    (AttributeArgumentSyntax argument) => InferTypeInAttributeArgument(argument, token),
                    (AttributeArgumentListSyntax attributeArgumentList) => InferTypeInAttributeArgumentList(attributeArgumentList, token),
                    (CheckedExpressionSyntax checkedExpression) => InferTypes(checkedExpression),
                    (ArrayCreationExpressionSyntax arrayCreationExpression) => InferTypeInArrayCreationExpression(arrayCreationExpression, token),
                    (ArrayRankSpecifierSyntax arrayRankSpecifier) => InferTypeInArrayRankSpecifier(arrayRankSpecifier, token),
                    (ArrayTypeSyntax arrayType) => InferTypeInArrayType(arrayType, token),
                    (AttributeListSyntax attributeDeclaration) => InferTypeInAttributeDeclaration(attributeDeclaration, token),
                    (AttributeTargetSpecifierSyntax attributeTargetSpecifier) => InferTypeInAttributeTargetSpecifier(attributeTargetSpecifier, token),
                    (BinaryExpressionSyntax binaryExpression) => InferTypeInBinaryOrAssignmentExpression(binaryExpression, binaryExpression.OperatorToken, binaryExpression.Left, binaryExpression.Right, previousToken: token),
                    (AssignmentExpressionSyntax assignmentExpression) => InferTypeInBinaryOrAssignmentExpression(assignmentExpression, assignmentExpression.OperatorToken, assignmentExpression.Left, assignmentExpression.Right, previousToken: token),
                    (BracketedArgumentListSyntax bracketedArgumentList) => InferTypeInBracketedArgumentList(bracketedArgumentList, token),
                    (CastExpressionSyntax castExpression) => InferTypeInCastExpression(castExpression, previousToken: token),
                    (CatchDeclarationSyntax catchDeclaration) => InferTypeInCatchDeclaration(catchDeclaration, token),
                    (CatchFilterClauseSyntax catchFilterClause) => InferTypeInCatchFilterClause(catchFilterClause, token),
                    (ConditionalExpressionSyntax conditionalExpression) => InferTypeInConditionalExpression(conditionalExpression, previousToken: token),
                    (DoStatementSyntax doStatement) => InferTypeInDoStatement(doStatement, token),
                    (EqualsValueClauseSyntax equalsValue) => InferTypeInEqualsValueClause(equalsValue, token),
                    (ExpressionStatementSyntax expressionStatement) => InferTypeInExpressionStatement(expressionStatement, token),
                    (ForEachStatementSyntax forEachStatement) => InferTypeInForEachStatement(forEachStatement, previousToken: token),
                    (ForStatementSyntax forStatement) => InferTypeInForStatement(forStatement, previousToken: token),
                    (IfStatementSyntax ifStatement) => InferTypeInIfStatement(ifStatement, token),
                    (InitializerExpressionSyntax initializerExpression) => InferTypeInInitializerExpression(initializerExpression, previousToken: token),
                    (LockStatementSyntax lockStatement) => InferTypeInLockStatement(lockStatement, token),
                    (NameColonSyntax nameColon) => InferTypeInNameColon(nameColon, token),
                    (NameEqualsSyntax nameEquals) => InferTypeInNameEquals(nameEquals, token),
                    (ParenthesizedLambdaExpressionSyntax parenthesizedLambdaExpression) => InferTypeInParenthesizedLambdaExpression(parenthesizedLambdaExpression, token),
                    (PostfixUnaryExpressionSyntax postfixUnary) => InferTypeInPostfixUnaryExpression(postfixUnary, token),
                    (PrefixUnaryExpressionSyntax prefixUnary) => InferTypeInPrefixUnaryExpression(prefixUnary, token),
                    (AwaitExpressionSyntax awaitExpression) => InferTypeInAwaitExpression(awaitExpression, token),
                    (ReturnStatementSyntax returnStatement) => InferTypeForReturnStatement(returnStatement, token),
                    (SimpleLambdaExpressionSyntax simpleLambdaExpression) => InferTypeInSimpleLambdaExpression(simpleLambdaExpression, token),
                    (SwitchLabelSyntax switchLabel) => InferTypeInSwitchLabel(switchLabel, token),
                    (SwitchStatementSyntax switchStatement) => InferTypeInSwitchStatement(switchStatement, token),
                    (ThrowStatementSyntax throwStatement) => InferTypeInThrowStatement(throwStatement, token),
                    (UsingStatementSyntax usingStatement) => InferTypeInUsingStatement(usingStatement, token),
                    (WhileStatementSyntax whileStatement) => InferTypeInWhileStatement(whileStatement, token),
                    (YieldStatementSyntax yieldStatement) => InferTypeInYieldStatement(yieldStatement, token),
                    _ => SpecializedCollections.EmptyEnumerable<ITypeSymbol>());
            }

            private IEnumerable<ITypeSymbol> InferTypeInArgument(ArgumentSyntax argument, SyntaxToken? previousToken = null)
            {
                if (previousToken.HasValue)
                {
                    // If we have a position, then it must be after the colon in a named argument.
                    if (argument.NameColon == null || argument.NameColon.ColonToken != previousToken)
                    {
                        return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                    }
                }

                if (argument.Parent != null)
                {
                    var initializer = argument.Parent.Parent as ConstructorInitializerSyntax;
                    if (initializer != null)
                    {
                        var index = initializer.ArgumentList.Arguments.IndexOf(argument);
                        return InferTypeInConstructorInitializer(initializer, index, argument);
                    }

                    if (argument.Parent.IsParentKind(SyntaxKind.InvocationExpression))
                    {
                        var invocation = argument.Parent.Parent as InvocationExpressionSyntax;
                        var index = invocation.ArgumentList.Arguments.IndexOf(argument);

                        return InferTypeInInvocationExpression(invocation, index, argument);
                    }

                    if (argument.Parent.IsParentKind(SyntaxKind.ObjectCreationExpression))
                    {
                        // new Outer(Foo());
                        //
                        // new Outer(a: Foo());
                        //
                        // etc.
                        var creation = argument.Parent.Parent as ObjectCreationExpressionSyntax;
                        var index = creation.ArgumentList.Arguments.IndexOf(argument);

                        return InferTypeInObjectCreationExpression(creation, index, argument);
                    }

                    if (argument.Parent.IsParentKind(SyntaxKind.ElementAccessExpression))
                    {
                        // Outer[Foo()];
                        //
                        // Outer[a: Foo()];
                        //
                        // etc.
                        var elementAccess = argument.Parent.Parent as ElementAccessExpressionSyntax;
                        var index = elementAccess.ArgumentList.Arguments.IndexOf(argument);

                        return InferTypeInElementAccessExpression(elementAccess, index, argument);
                    }
                }

                if (argument.Parent.IsParentKind(SyntaxKind.ImplicitElementAccess) &&
                    argument.Parent.Parent.IsParentKind(SyntaxKind.SimpleAssignmentExpression) &&
                    argument.Parent.Parent.Parent.IsParentKind(SyntaxKind.ObjectInitializerExpression) &&
                    argument.Parent.Parent.Parent.Parent.IsParentKind(SyntaxKind.ObjectCreationExpression))
                {
                    var objectCreation = (ObjectCreationExpressionSyntax)argument.Parent.Parent.Parent.Parent.Parent;
                    var types = GetTypes(objectCreation);

                    if (types.Any(t => t is INamedTypeSymbol))
                    {
                        return types.OfType<INamedTypeSymbol>().SelectMany(t =>
                            GetCollectionElementType(t,
                                parameterIndex: 0));
                    }
                }

                return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
            }

            private IEnumerable<ITypeSymbol> InferTypeInAttributeArgument(AttributeArgumentSyntax argument, SyntaxToken? previousToken = null, ArgumentSyntax argumentOpt = null)
            {
                if (previousToken.HasValue)
                {
                    // If we have a position, then it must be after the colon or equals in an argument.
                    if (argument.NameColon == null || argument.NameColon.ColonToken != previousToken || argument.NameEquals.EqualsToken != previousToken)
                    {
                        return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                    }
                }

                if (argument.Parent != null)
                {
                    var attribute = argument.Parent.Parent as AttributeSyntax;
                    if (attribute != null)
                    {
                        var index = attribute.ArgumentList.Arguments.IndexOf(argument);
                        return InferTypeInAttribute(attribute, index, argument);
                    }
                }

                return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
            }

            private IEnumerable<ITypeSymbol> InferTypeInConstructorInitializer(ConstructorInitializerSyntax initializer, int index, ArgumentSyntax argument = null)
            {
                var info = _semanticModel.GetSymbolInfo(initializer, _cancellationToken);
                var methods = info.GetBestOrAllSymbols().OfType<IMethodSymbol>();
                return InferTypeInArgument(index, methods, argument);
            }

            private IEnumerable<ITypeSymbol> InferTypeInObjectCreationExpression(ObjectCreationExpressionSyntax creation, int index, ArgumentSyntax argumentOpt = null)
            {
                var info = _semanticModel.GetSymbolInfo(creation.Type, _cancellationToken);
                var type = info.Symbol as INamedTypeSymbol;

                if (type == null)
                {
                    return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                }

                if (type.TypeKind == TypeKind.Delegate)
                {
                    // new SomeDelegateType( here );
                    //
                    // They're actually instantiating a delegate, so the delegate type is
                    // that type.
                    return SpecializedCollections.SingletonEnumerable(type);
                }

                var constructors = type.InstanceConstructors.Where(m => m.Parameters.Length > index);
                return InferTypeInArgument(index, constructors, argumentOpt);
            }

            private IEnumerable<ITypeSymbol> InferTypeInInvocationExpression(
                InvocationExpressionSyntax invocation, int index, ArgumentSyntax argumentOpt = null)
            {
                // Check all the methods that have at least enough arguments to support
                // being called with argument at this position.  Note: if they're calling an
                // extension method then it will need one more argument in order for us to
                // call it.
                var info = _semanticModel.GetSymbolInfo(invocation, _cancellationToken);
                IEnumerable<IMethodSymbol> methods = null;

                // Overload resolution (see DevDiv 611477) in certain extension method cases
                // can result in GetSymbolInfo returning nothing. In this case, get the 
                // method group info, which is what signature help already does.
                if (info.CandidateReason == CandidateReason.None)
                {
                    methods = ((SemanticModel)_semanticModel).GetMemberGroup(invocation.Expression, _cancellationToken)
                                                            .OfType<IMethodSymbol>();
                }
                else
                {
                    methods = info.GetBestOrAllSymbols().OfType<IMethodSymbol>();
                }

                return InferTypeInArgument(index, methods, argumentOpt);
            }

            private IEnumerable<ITypeSymbol> InferTypeInArgumentList(ArgumentListSyntax argumentList, SyntaxToken previousToken)
            {
                // Has to follow the ( or a ,
                if (previousToken != argumentList.OpenParenToken && previousToken.Kind() != SyntaxKind.CommaToken)
                {
                    return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                }

                var invocation = argumentList.Parent as InvocationExpressionSyntax;
                if (invocation != null)
                {
                    var index = this.GetArgumentListIndex(argumentList, previousToken);
                    return InferTypeInInvocationExpression(invocation, index);
                }

                var objectCreation = argumentList.Parent as ObjectCreationExpressionSyntax;
                if (objectCreation != null)
                {
                    var index = this.GetArgumentListIndex(argumentList, previousToken);
                    return InferTypeInObjectCreationExpression(objectCreation, index);
                }

                var constructorInitializer = argumentList.Parent as ConstructorInitializerSyntax;
                if (constructorInitializer != null)
                {
                    var index = this.GetArgumentListIndex(argumentList, previousToken);
                    return InferTypeInConstructorInitializer(constructorInitializer, index);
                }

                return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
            }

            private IEnumerable<ITypeSymbol> InferTypeInAttributeArgumentList(AttributeArgumentListSyntax attributeArgumentList, SyntaxToken previousToken)
            {
                // Has to follow the ( or a ,
                if (previousToken != attributeArgumentList.OpenParenToken && previousToken.Kind() != SyntaxKind.CommaToken)
                {
                    return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                }

                var attribute = attributeArgumentList.Parent as AttributeSyntax;
                if (attribute != null)
                {
                    var index = this.GetArgumentListIndex(attributeArgumentList, previousToken);
                    return InferTypeInAttribute(attribute, index);
                }

                return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
            }

            private IEnumerable<ITypeSymbol> InferTypeInAttribute(AttributeSyntax attribute, int index, AttributeArgumentSyntax argumentOpt = null)
            {
                var info = _semanticModel.GetSymbolInfo(attribute, _cancellationToken);
                var methods = info.GetBestOrAllSymbols().OfType<IMethodSymbol>();
                return InferTypeInAttributeArgument(index, methods, argumentOpt);
            }

            private IEnumerable<ITypeSymbol> InferTypeInElementAccessExpression(
                ElementAccessExpressionSyntax elementAccess, int index, ArgumentSyntax argumentOpt = null)
            {
                var info = _semanticModel.GetTypeInfo(elementAccess.Expression, _cancellationToken);
                var type = info.Type as INamedTypeSymbol;
                if (type != null)
                {
                    var indexers = type.GetMembers().OfType<IPropertySymbol>()
                                                   .Where(p => p.IsIndexer && p.Parameters.Length > index);

                    if (indexers.Any())
                    {
                        return indexers.SelectMany(i =>
                            InferTypeInArgument(index, SpecializedCollections.SingletonEnumerable(i.Parameters), argumentOpt));
                    }
                }

                // For everything else, assume it's an integer.  Note: this won't be correct for
                // type parameters that implement some interface, but that seems like a major
                // corner case for now.
                // 
                // This does, however, cover the more common cases of
                // arrays/pointers/errors/dynamic.
                return SpecializedCollections.SingletonEnumerable(this.Compilation.GetSpecialType(SpecialType.System_Int32));
            }

            private IEnumerable<ITypeSymbol> InferTypeInAttributeArgument(int index, IEnumerable<IMethodSymbol> methods, AttributeArgumentSyntax argumentOpt = null)
            {
                return InferTypeInAttributeArgument(index, methods.Select(m => m.Parameters), argumentOpt);
            }

            private IEnumerable<ITypeSymbol> InferTypeInArgument(int index, IEnumerable<IMethodSymbol> methods, ArgumentSyntax argumentOpt)
            {
                return InferTypeInArgument(index, methods.Select(m => m.Parameters), argumentOpt);
            }

            private IEnumerable<ITypeSymbol> InferTypeInAttributeArgument(
                int index,
                IEnumerable<ImmutableArray<IParameterSymbol>> parameterizedSymbols,
                AttributeArgumentSyntax argumentOpt = null)
            {
                if (argumentOpt != null && argumentOpt.NameEquals != null)
                {
                    // [MyAttribute(Prop = ...

                    return InferTypeInNameEquals(argumentOpt.NameEquals, argumentOpt.NameEquals.EqualsToken);
                }

                var name = argumentOpt != null && argumentOpt.NameColon != null ? argumentOpt.NameColon.Name.Identifier.ValueText : null;
                return InferTypeInArgument(index, parameterizedSymbols, name);
            }

            private IEnumerable<ITypeSymbol> InferTypeInArgument(
                int index,
                IEnumerable<ImmutableArray<IParameterSymbol>> parameterizedSymbols,
                ArgumentSyntax argumentOpt)
            {
                var name = argumentOpt != null && argumentOpt.NameColon != null ? argumentOpt.NameColon.Name.Identifier.ValueText : null;
                return InferTypeInArgument(index, parameterizedSymbols, name);
            }

            private IEnumerable<ITypeSymbol> InferTypeInArgument(
                int index,
                IEnumerable<ImmutableArray<IParameterSymbol>> parameterizedSymbols,
                string name)
            {
                // If the callsite has a named argument, then try to find a method overload that has a
                // parameter with that name.  If we can find one, then return the type of that one.
                if (name != null)
                {
                    var parameters = parameterizedSymbols.SelectMany(m => m)
                                                        .Where(p => p.Name == name)
                                                        .Select(p => p.Type);
                    if (parameters.Any())
                    {
                        return parameters;
                    }
                }
                else
                {
                    // Otherwise, just take the first overload and pick what type this parameter is
                    // based on index.
                    var q = from parameterSet in parameterizedSymbols
                            where index < parameterSet.Length
                            select parameterSet[index].Type;

                    return q;
                }

                return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
            }

            private IEnumerable<ITypeSymbol> InferTypeInArrayCreationExpression(
                ArrayCreationExpressionSyntax arrayCreationExpression, SyntaxToken? previousToken = null)
            {
                if (previousToken.HasValue && previousToken.Value != arrayCreationExpression.NewKeyword)
                {
                    // Has to follow the 'new' keyword. 
                    return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                }

                var outerTypes = InferTypes(arrayCreationExpression);
                return outerTypes.Where(o => o is IArrayTypeSymbol);
            }

            private IEnumerable<ITypeSymbol> InferTypeInArrayRankSpecifier(ArrayRankSpecifierSyntax arrayRankSpecifier, SyntaxToken? previousToken = null)
            {
                // If we have a token, and it's not the open bracket or one of the commas, then no
                // inference.
                if (previousToken == arrayRankSpecifier.CloseBracketToken)
                {
                    return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                }

                return SpecializedCollections.SingletonEnumerable(this.Compilation.GetSpecialType(SpecialType.System_Int32));
            }

            private IEnumerable<ITypeSymbol> InferTypeInArrayType(ArrayTypeSyntax arrayType, SyntaxToken? previousToken = null)
            {
                if (previousToken.HasValue)
                {
                    // TODO(cyrusn): NYI.  Handle this appropriately if we need to.
                    return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                }

                // Bind the array type, then unwrap whatever we get back based on the number of rank
                // specifiers we see.
                var currentTypes = InferTypes(arrayType);
                for (var i = 0; i < arrayType.RankSpecifiers.Count; i++)
                {
                    currentTypes = currentTypes.OfType<IArrayTypeSymbol>().Select(c => c.ElementType);
                }

                return currentTypes;
            }

            private IEnumerable<ITypeSymbol> InferTypeInAttribute(AttributeSyntax attribute)
            {
                return SpecializedCollections.SingletonEnumerable(this.Compilation.AttributeType());
            }

            private IEnumerable<ITypeSymbol> InferTypeInAttributeDeclaration(AttributeListSyntax attributeDeclaration, SyntaxToken? previousToken)
            {
                // If we have a position, then it has to be after the open bracket.
                if (previousToken.HasValue && previousToken.Value != attributeDeclaration.OpenBracketToken)
                {
                    return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                }

                return SpecializedCollections.SingletonEnumerable(this.Compilation.AttributeType());
            }

            private IEnumerable<ITypeSymbol> InferTypeInAttributeTargetSpecifier(
                AttributeTargetSpecifierSyntax attributeTargetSpecifier,
                SyntaxToken? previousToken)
            {
                // If we have a position, then it has to be after the colon.
                if (previousToken.HasValue && previousToken.Value != attributeTargetSpecifier.ColonToken)
                {
                    return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                }

                return SpecializedCollections.SingletonEnumerable(this.Compilation.AttributeType());
            }

            private IEnumerable<ITypeSymbol> InferTypeInBracketedArgumentList(BracketedArgumentListSyntax bracketedArgumentList, SyntaxToken previousToken)
            {
                // Has to follow the [ or a ,
                if (previousToken != bracketedArgumentList.OpenBracketToken && previousToken.Kind() != SyntaxKind.CommaToken)
                {
                    return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                }

                var elementAccess = bracketedArgumentList.Parent as ElementAccessExpressionSyntax;
                if (elementAccess != null)
                {
                    var index = GetArgumentListIndex(bracketedArgumentList, previousToken);
                    return InferTypeInElementAccessExpression(
                        elementAccess, index);
                }

                return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
            }

            private int GetArgumentListIndex(BaseArgumentListSyntax argumentList, SyntaxToken previousToken)
            {
                if (previousToken == argumentList.GetOpenToken())
                {
                    return 0;
                }

                ////    ( node0 , node1 , node2 , node3 ,
                //
                // Tokidx   0   1   2   3   4   5   6   7
                //
                // index        1       2       3
                //
                // index = (Tokidx + 1) / 2

                var tokenIndex = argumentList.Arguments.GetWithSeparators().IndexOf(previousToken);
                return (tokenIndex + 1) / 2;
            }

            private int GetArgumentListIndex(AttributeArgumentListSyntax attributeArgumentList, SyntaxToken previousToken)
            {
                if (previousToken == attributeArgumentList.OpenParenToken)
                {
                    return 0;
                }

                ////    ( node0 , node1 , node2 , node3 ,
                //
                // Tokidx   0   1   2   3   4   5   6   7
                //
                // index        1       2       3
                //
                // index = (Tokidx + 1) / 2

                var tokenIndex = attributeArgumentList.Arguments.GetWithSeparators().IndexOf(previousToken);
                return (tokenIndex + 1) / 2;
            }

            private IEnumerable<ITypeSymbol> InferTypeInBinaryOrAssignmentExpression(ExpressionSyntax binop, SyntaxToken operatorToken, ExpressionSyntax left, ExpressionSyntax right, ExpressionSyntax expressionOpt = null, SyntaxToken? previousToken = null)
            {
                // If we got here through a token, then it must have actually been the binary
                // operator's token.
                Contract.ThrowIfTrue(previousToken.HasValue && previousToken.Value != operatorToken);

                if (binop.Kind() == SyntaxKind.CoalesceExpression)
                {
                    return InferTypeInCoalesceExpression((BinaryExpressionSyntax)binop, expressionOpt, previousToken);
                }

                var onRightOfToken = right == expressionOpt || previousToken.HasValue;
                switch (operatorToken.Kind())
                {
                    case SyntaxKind.LessThanLessThanToken:
                    case SyntaxKind.GreaterThanGreaterThanToken:
                    case SyntaxKind.LessThanLessThanEqualsToken:
                    case SyntaxKind.GreaterThanGreaterThanEqualsToken:

                        if (onRightOfToken)
                        {
                            // x << Foo(), x >> Foo(), x <<= Foo(), x >>= Foo()
                            return SpecializedCollections.SingletonEnumerable(this.Compilation.GetSpecialType(SpecialType.System_Int32));
                        }

                        break;
                }

                // Infer operands of && and || as bool regardless of the other operand.
                if (operatorToken.Kind() == SyntaxKind.AmpersandAmpersandToken ||
                    operatorToken.Kind() == SyntaxKind.BarBarToken)
                {
                    return SpecializedCollections.SingletonEnumerable(this.Compilation.GetSpecialType(SpecialType.System_Boolean));
                }

                // Try to figure out what's on the other side of the binop.  If we can, then just that
                // type.  This is often a reasonable heuristics to use for most operators.  NOTE(cyrusn):
                // we could try to bind the token to see what overloaded operators it corresponds to.
                // But the gain is pretty marginal IMO.
                var otherSide = onRightOfToken ? left : right;

                var otherSideTypes = GetTypes(otherSide);
                if (otherSideTypes.Any())
                {
                    return otherSideTypes;
                }

                // For &, &=, |, |=, ^, and ^=, since we couldn't infer the type of either side, 
                // try to infer the type of the entire binary expression.
                if (operatorToken.Kind() == SyntaxKind.AmpersandToken ||
                    operatorToken.Kind() == SyntaxKind.AmpersandEqualsToken ||
                    operatorToken.Kind() == SyntaxKind.BarToken ||
                    operatorToken.Kind() == SyntaxKind.BarEqualsToken ||
                    operatorToken.Kind() == SyntaxKind.CaretToken ||
                    operatorToken.Kind() == SyntaxKind.CaretEqualsToken)
                {
                    var parentTypes = InferTypes(binop);
                    if (parentTypes.Any())
                    {
                        return parentTypes;
                    }
                }

                // If it's a plus operator, then do some smarts in case it might be a string or
                // delegate.
                if (operatorToken.Kind() == SyntaxKind.PlusToken)
                {
                    // See Bug 6045.  Note: we've already checked the other side of the operator.  So this
                    // is the case where the other side was also unknown.  So we walk one higher and if
                    // we get a delegate or a string type, then use that type here.
                    var parentTypes = InferTypes(binop);
                    if (parentTypes.Any(parentType => parentType.SpecialType == SpecialType.System_String || parentType.TypeKind == TypeKind.Delegate))
                    {
                        return parentTypes.Where(parentType => parentType.SpecialType == SpecialType.System_String || parentType.TypeKind == TypeKind.Delegate);
                    }
                }

                // Otherwise pick some sane defaults for certain common cases.
                switch (operatorToken.Kind())
                {
                    case SyntaxKind.BarToken:
                    case SyntaxKind.CaretToken:
                    case SyntaxKind.AmpersandToken:
                    case SyntaxKind.LessThanToken:
                    case SyntaxKind.LessThanEqualsToken:
                    case SyntaxKind.GreaterThanToken:
                    case SyntaxKind.GreaterThanEqualsToken:
                    case SyntaxKind.PlusToken:
                    case SyntaxKind.MinusToken:
                    case SyntaxKind.AsteriskToken:
                    case SyntaxKind.SlashToken:
                    case SyntaxKind.PercentToken:
                    case SyntaxKind.CaretEqualsToken:
                    case SyntaxKind.PlusEqualsToken:
                    case SyntaxKind.MinusEqualsToken:
                    case SyntaxKind.AsteriskEqualsToken:
                    case SyntaxKind.SlashEqualsToken:
                    case SyntaxKind.PercentEqualsToken:
                    case SyntaxKind.LessThanLessThanToken:
                    case SyntaxKind.GreaterThanGreaterThanToken:
                    case SyntaxKind.LessThanLessThanEqualsToken:
                    case SyntaxKind.GreaterThanGreaterThanEqualsToken:
                        return SpecializedCollections.SingletonEnumerable(this.Compilation.GetSpecialType(SpecialType.System_Int32));

                    case SyntaxKind.BarEqualsToken:
                    case SyntaxKind.AmpersandEqualsToken:
                        // NOTE(cyrusn): |= and &= can be used for both ints and bools  However, in the
                        // case where there isn't enough information to determine which the user wanted,
                        // i'm just defaulting to bool based on personal preference.
                        return SpecializedCollections.SingletonEnumerable(this.Compilation.GetSpecialType(SpecialType.System_Boolean));
                }

                return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
            }

            private IEnumerable<ITypeSymbol> InferTypeInCastExpression(CastExpressionSyntax castExpression, ExpressionSyntax expressionOpt = null, SyntaxToken? previousToken = null)
            {
                if (expressionOpt != null && castExpression.Expression != expressionOpt)
                {
                    return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                }

                // If we have a position, then it has to be after the close paren.
                if (previousToken.HasValue && previousToken.Value != castExpression.CloseParenToken)
                {
                    return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                }

                return this.GetTypes(castExpression.Type);
            }

            private IEnumerable<ITypeSymbol> InferTypeInCatchDeclaration(CatchDeclarationSyntax catchDeclaration, SyntaxToken? previousToken = null)
            {
                // If we have a position, it has to be after "catch("
                if (previousToken.HasValue && previousToken.Value != catchDeclaration.OpenParenToken)
                {
                    return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                }

                return SpecializedCollections.SingletonEnumerable(this.Compilation.ExceptionType());
            }

            private IEnumerable<ITypeSymbol> InferTypeInCatchFilterClause(CatchFilterClauseSyntax catchFilterClause, SyntaxToken? previousToken = null)
            {
                // If we have a position, it has to be after "if ("
                if (previousToken.HasValue && previousToken.Value != catchFilterClause.OpenParenToken)
                {
                    return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                }

                return SpecializedCollections.SingletonEnumerable(this.Compilation.GetSpecialType(SpecialType.System_Boolean));
            }

            private IEnumerable<ITypeSymbol> InferTypeInCoalesceExpression(
                BinaryExpressionSyntax coalesceExpression,
                ExpressionSyntax expressionOpt = null,
                SyntaxToken? previousToken = null)
            {
                // If we got here through a token, then it must have actually been the binary
                // operator's token.
                Contract.ThrowIfTrue(previousToken.HasValue && previousToken.Value != coalesceExpression.OperatorToken);

                var onRightSide = coalesceExpression.Right == expressionOpt || previousToken.HasValue;
                if (onRightSide)
                {
                    var leftTypes = GetTypes(coalesceExpression.Left);
                    return leftTypes
                        .Select(x => x.IsNullable()
                            ? ((INamedTypeSymbol)x).TypeArguments[0] // nullableExpr ?? Foo()
                            : x); // normalExpr ?? Foo() 
                }

                var rightTypes = GetTypes(coalesceExpression.Right);
                if (!rightTypes.Any())
                {
                    return SpecializedCollections.SingletonEnumerable(
                        this.Compilation.GetSpecialType(SpecialType.System_Nullable_T)
                            .Construct(Compilation.GetSpecialType(SpecialType.System_Object)));
                }

                return rightTypes
                    .Select(x => x.IsValueType
                                     ? this.Compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(x) // Foo() ?? 0
                                     : x); // Foo() ?? ""
            }

            private IEnumerable<ITypeSymbol> InferTypeInConditionalExpression(ConditionalExpressionSyntax conditional, ExpressionSyntax expressionOpt = null, SyntaxToken? previousToken = null)
            {
                if (expressionOpt != null && conditional.Condition == expressionOpt)
                {
                    // Foo() ? a : b
                    return SpecializedCollections.SingletonEnumerable(this.Compilation.GetSpecialType(SpecialType.System_Boolean));
                }

                // a ? Foo() : b
                //
                // a ? b : Foo()
                var inTrueClause =
                    (conditional.WhenTrue == expressionOpt) ||
                    (previousToken == conditional.QuestionToken);

                var inFalseClause =
                    (conditional.WhenFalse == expressionOpt) ||
                    (previousToken == conditional.ColonToken);

                var otherTypes = inTrueClause
                                     ? GetTypes(conditional.WhenFalse)
                                     : inFalseClause
                                           ? GetTypes(conditional.WhenTrue)
                                           : SpecializedCollections.EmptyEnumerable<ITypeSymbol>();

                return otherTypes.IsEmpty()
                           ? InferTypes(conditional)
                           : otherTypes;
            }

            private IEnumerable<ITypeSymbol> InferTypeInDoStatement(DoStatementSyntax doStatement, SyntaxToken? previousToken = null)
            {
                // If we have a position, we need to be after "do { } while("
                if (previousToken.HasValue && previousToken.Value != doStatement.OpenParenToken)
                {
                    return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                }

                return SpecializedCollections.SingletonEnumerable(this.Compilation.GetSpecialType(SpecialType.System_Boolean));
            }

            private IEnumerable<ITypeSymbol> InferTypeInEqualsValueClause(EqualsValueClauseSyntax equalsValue, SyntaxToken? previousToken = null)
            {
                // If we have a position, it has to be after the =
                if (previousToken.HasValue && previousToken.Value != equalsValue.EqualsToken)
                {
                    return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                }

                if (equalsValue.IsParentKind(SyntaxKind.VariableDeclarator))
                {
                    return InferTypeInVariableDeclarator((VariableDeclaratorSyntax)equalsValue.Parent);
                }

                if (equalsValue.IsParentKind(SyntaxKind.PropertyDeclaration))
                {
                    return InferTypeInPropertyDeclaration((PropertyDeclarationSyntax)equalsValue.Parent);
                }

                if (equalsValue.IsParentKind(SyntaxKind.Parameter))
                {
                    var parameter = _semanticModel.GetDeclaredSymbol(equalsValue.Parent, _cancellationToken) as IParameterSymbol;
                    if (parameter != null)
                    {
                        return SpecializedCollections.SingletonEnumerable(parameter.Type);
                    }
                }

                return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
            }

            private IEnumerable<ITypeSymbol> InferTypeInPropertyDeclaration(PropertyDeclarationSyntax propertyDeclaration)
            {
                Contract.Assert(propertyDeclaration?.Type != null, "Property type should never be null");

                var typeInfo = _semanticModel.GetTypeInfo(propertyDeclaration.Type);
                return typeInfo.Type != null
                    ? SpecializedCollections.SingletonEnumerable(typeInfo.Type)
                    : SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
            }

            private IEnumerable<ITypeSymbol> InferTypeInBaseMethodDeclaration(BaseMethodDeclarationSyntax declaration)
            {
                var methodSymbol = _semanticModel.GetDeclaredSymbol(declaration);
                return methodSymbol?.ReturnType != null
                    ? SpecializedCollections.SingletonEnumerable(methodSymbol.ReturnType)
                    : SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
            }

            private IEnumerable<ITypeSymbol> InferTypeInExpressionStatement(ExpressionStatementSyntax expressionStatement, SyntaxToken? previousToken = null)
            {
                // If we're position based, then that means we're after the semicolon.  In this case
                // we don't have any sort of type to infer.
                if (previousToken.HasValue)
                {
                    return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                }

                return SpecializedCollections.SingletonEnumerable(this.Compilation.GetSpecialType(SpecialType.System_Void));
            }

            private IEnumerable<ITypeSymbol> InferTypeInForEachStatement(ForEachStatementSyntax forEachStatementSyntax, ExpressionSyntax expressionOpt = null, SyntaxToken? previousToken = null)
            {
                // If we have a position, then we have to be after "foreach(... in"
                if (previousToken.HasValue && previousToken.Value != forEachStatementSyntax.InKeyword)
                {
                    return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                }

                if (expressionOpt != null && expressionOpt != forEachStatementSyntax.Expression)
                {
                    return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                }

                // foreach (int v = Foo())
                var variableTypes = GetTypes(forEachStatementSyntax.Type);
                if (!variableTypes.Any())
                {
                    return SpecializedCollections.SingletonEnumerable(
                        this.Compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T)
                        .Construct(Compilation.GetSpecialType(SpecialType.System_Object)));
                }

                var type = this.Compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T);
                return variableTypes.Select(v => type.Construct(v));
            }

            private IEnumerable<ITypeSymbol> InferTypeInForStatement(ForStatementSyntax forStatement, ExpressionSyntax expressionOpt = null, SyntaxToken? previousToken = null)
            {
                // If we have a position, it has to be after "for(...;"
                if (previousToken.HasValue && previousToken.Value != forStatement.FirstSemicolonToken)
                {
                    return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                }

                if (expressionOpt != null && forStatement.Condition != expressionOpt)
                {
                    return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                }

                return SpecializedCollections.SingletonEnumerable(this.Compilation.GetSpecialType(SpecialType.System_Boolean));
            }

            private IEnumerable<ITypeSymbol> InferTypeInIfStatement(IfStatementSyntax ifStatement, SyntaxToken? previousToken = null)
            {
                // If we have a position, we have to be after the "if("
                if (previousToken.HasValue && previousToken.Value != ifStatement.OpenParenToken)
                {
                    return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                }

                return SpecializedCollections.SingletonEnumerable(this.Compilation.GetSpecialType(SpecialType.System_Boolean));
            }

            private IEnumerable<ITypeSymbol> InferTypeInInitializerExpression(
                InitializerExpressionSyntax initializerExpression,
                ExpressionSyntax expressionOpt = null,
                SyntaxToken? previousToken = null)
            {
                if (initializerExpression.IsParentKind(SyntaxKind.ImplicitArrayCreationExpression))
                {
                    // First, try to infer the type that the array should be.  If we can infer an
                    // appropriate array type, then use the element type of the array.  Otherwise,
                    // look at the siblings of this expression and use their type instead.

                    var arrayTypes = this.InferTypes((ExpressionSyntax)initializerExpression.Parent);
                    var elementTypes = arrayTypes.OfType<IArrayTypeSymbol>().Select(a => a.ElementType).Where(e => !IsUnusableType(e));

                    if (elementTypes.Any())
                    {
                        return elementTypes;
                    }

                    // { foo(), |
                    if (previousToken.HasValue && previousToken.Value.Kind() == SyntaxKind.CommaToken)
                    {
                        var sibling = initializerExpression.Expressions.FirstOrDefault(e => e.SpanStart < previousToken.Value.SpanStart);
                        if (sibling != null)
                        {
                            var types = GetTypes(sibling);
                            if (types.Any())
                            {
                                return types;
                            }
                        }
                    }

                    foreach (var sibling in initializerExpression.Expressions)
                    {
                        if (sibling != expressionOpt)
                        {
                            var types = GetTypes(sibling);
                            if (types.Any())
                            {
                                return types;
                            }
                        }
                    }
                }
                else if (initializerExpression.IsParentKind(SyntaxKind.EqualsValueClause))
                {
                    // = { Foo() }
                    var equalsValueClause = (EqualsValueClauseSyntax)initializerExpression.Parent;
                    IEnumerable<ITypeSymbol> types = InferTypeInEqualsValueClause(equalsValueClause);

                    if (types.Any(t => t is IArrayTypeSymbol))
                    {
                        return types.OfType<IArrayTypeSymbol>().Select(t => t.ElementType);
                    }
                }
                else if (initializerExpression.IsParentKind(SyntaxKind.ArrayCreationExpression))
                {
                    // new int[] { Foo() } 
                    var symbols = _semanticModel.GetCollectionInitializerSymbolInfo(initializerExpression).GetAllSymbols();

                    if (symbols.Any(t => t is INamedTypeSymbol))
                    {
                        return symbols.OfType<INamedTypeSymbol>();
                    }

                    var arrayCreation = (ArrayCreationExpressionSyntax)initializerExpression.Parent;
                    IEnumerable<ITypeSymbol> types = GetTypes(arrayCreation);

                    if (types.Any(t => t is IArrayTypeSymbol))
                    {
                        return types.OfType<IArrayTypeSymbol>().Select(t => t.ElementType);
                    }
                }
                else if (initializerExpression.IsParentKind(SyntaxKind.ObjectCreationExpression))
                {
                    // new List<T> { Foo() } 

                    var objectCreation = (ObjectCreationExpressionSyntax)initializerExpression.Parent;

                    IEnumerable<ITypeSymbol> types = GetTypes(objectCreation);
                    if (types.Any(t => t is INamedTypeSymbol))
                    {
                        return types.OfType<INamedTypeSymbol>().SelectMany(t =>
                            GetCollectionElementType(t, parameterIndex: 0));
                    }
                }
                else if ((initializerExpression.IsParentKind(SyntaxKind.ComplexElementInitializerExpression) &&
                          initializerExpression.Parent.IsParentKind(SyntaxKind.ObjectCreationExpression)) ||
                         (initializerExpression.IsKind(SyntaxKind.ComplexElementInitializerExpression) &&
                          initializerExpression.IsParentKind(SyntaxKind.CollectionInitializerExpression) &&
                          initializerExpression.Parent.IsParentKind(SyntaxKind.ObjectCreationExpression)))
                {
                    // new Dictionary<K,V> { { Foo(), ... } }
                    var objectCreation = (ObjectCreationExpressionSyntax)initializerExpression.Parent.Parent;
                    var symbols = _semanticModel.GetCollectionInitializerSymbolInfo(initializerExpression).GetAllSymbols();

                    if (symbols.Any(t => t is INamedTypeSymbol))
                    {
                        return symbols.OfType<INamedTypeSymbol>();
                    }

                    IEnumerable<ITypeSymbol> types = GetTypes(objectCreation);

                    if (types.Any(t => t is INamedTypeSymbol))
                    {
                        var parameterIndex = previousToken.HasValue
                            ? initializerExpression.Expressions.GetWithSeparators().IndexOf(previousToken.Value) + 1
                            : initializerExpression.Expressions.IndexOf(expressionOpt);

                        return types.OfType<INamedTypeSymbol>().SelectMany(t =>
                            GetCollectionElementType(t,
                                parameterIndex: parameterIndex));
                    }
                }
                else if (initializerExpression.IsParentKind(SyntaxKind.SimpleAssignmentExpression))
                {
                    // new Foo { a = { Foo() } }
                    var symbols = _semanticModel.GetCollectionInitializerSymbolInfo(initializerExpression).GetAllSymbols();

                    if (symbols.Any(t => t is INamedTypeSymbol))
                    {
                        return symbols.OfType<INamedTypeSymbol>();
                    }

                    var assignExpression = (AssignmentExpressionSyntax)initializerExpression.Parent;
                    IEnumerable<ITypeSymbol> types = GetTypes(assignExpression.Left);

                    if (types.Any(t => t is INamedTypeSymbol))
                    {
                        var parameterIndex = previousToken.HasValue
                            ? initializerExpression.Expressions.GetWithSeparators().IndexOf(previousToken.Value) + 1
                            : initializerExpression.Expressions.IndexOf(expressionOpt);

                        return types.OfType<INamedTypeSymbol>().SelectMany(t =>
                            GetCollectionElementType(t,
                                parameterIndex: parameterIndex));
                    }
                }

                return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
            }

            private IEnumerable<ITypeSymbol> InferTypeInLockStatement(LockStatementSyntax lockStatement, SyntaxToken? previousToken = null)
            {
                // If we're position based, then we have to be after the "lock("
                if (previousToken.HasValue && previousToken.Value != lockStatement.OpenParenToken)
                {
                    return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                }

                return SpecializedCollections.SingletonEnumerable(this.Compilation.GetSpecialType(SpecialType.System_Object));
            }

            private IEnumerable<ITypeSymbol> InferTypeInParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax lambdaExpression, SyntaxToken? previousToken = null)
            {
                // If we have a position, it has to be after the lambda arrow.
                if (previousToken.HasValue && previousToken.Value != lambdaExpression.ArrowToken)
                {
                    return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                }

                return InferTypeInLambdaExpression(lambdaExpression);
            }

            private IEnumerable<ITypeSymbol> InferTypeInSimpleLambdaExpression(SimpleLambdaExpressionSyntax lambdaExpression, SyntaxToken? previousToken = null)
            {
                // If we have a position, it has to be after the lambda arrow.
                if (previousToken.HasValue && previousToken.Value != lambdaExpression.ArrowToken)
                {
                    return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                }

                return InferTypeInLambdaExpression(lambdaExpression);
            }

            private IEnumerable<ITypeSymbol> InferTypeInLambdaExpression(ExpressionSyntax lambdaExpression)
            {
                // Func<int,string> = i => Foo();
                var types = InferTypes(lambdaExpression);
                var type = types.FirstOrDefault().GetDelegateType(this.Compilation);

                if (type != null)
                {
                    var invoke = type.DelegateInvokeMethod;
                    if (invoke != null)
                    {
                        return SpecializedCollections.SingletonEnumerable(invoke.ReturnType);
                    }
                }

                return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
            }

            private IEnumerable<ITypeSymbol> InferTypeInMemberDeclarator(AnonymousObjectMemberDeclaratorSyntax memberDeclarator, SyntaxToken? previousTokenOpt = null)
            {
                if (memberDeclarator.NameEquals != null && memberDeclarator.Parent is AnonymousObjectCreationExpressionSyntax)
                {
                    // If we're position based, then we have to be after the = 
                    if (previousTokenOpt.HasValue && previousTokenOpt.Value != memberDeclarator.NameEquals.EqualsToken)
                    {
                        return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                    }

                    var types = InferTypes((AnonymousObjectCreationExpressionSyntax)memberDeclarator.Parent);

                    return types.Where(t => t.IsAnonymousType())
                        .SelectMany(t => t.GetValidAnonymousTypeProperties()
                            .Where(p => p.Name == memberDeclarator.NameEquals.Name.Identifier.ValueText)
                            .Select(p => p.Type));
                }

                return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
            }

            private IEnumerable<ITypeSymbol> InferTypeInNameColon(NameColonSyntax nameColon, SyntaxToken previousToken)
            {
                if (previousToken != nameColon.ColonToken)
                {
                    // Must follow the colon token.
                    return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                }

                var argumentSyntax = nameColon.Parent as ArgumentSyntax;
                if (argumentSyntax != null)
                {
                    return InferTypeInArgument(argumentSyntax);
                }

                return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
            }

            private IEnumerable<ITypeSymbol> InferTypeInNameEquals(NameEqualsSyntax nameEquals, SyntaxToken? previousToken = null)
            {
                if (previousToken == nameEquals.EqualsToken)
                {
                    // we're on the right of the equals.  Try to bind the left name to see if it
                    // gives us anything useful.
                    return GetTypes(nameEquals.Name);
                }

                var attributeArgumentSyntax = nameEquals.Parent as AttributeArgumentSyntax;
                if (attributeArgumentSyntax != null)
                {
                    var argumentExpression = attributeArgumentSyntax.Expression;
                    return this.GetTypes(argumentExpression);
                }

                return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
            }

            private IEnumerable<ITypeSymbol> InferTypeInPostfixUnaryExpression(PostfixUnaryExpressionSyntax postfixUnaryExpressionSyntax, SyntaxToken? previousToken = null)
            {
                // If we're after a postfix ++ or -- then we can't infer anything.
                if (previousToken.HasValue)
                {
                    return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                }

                switch (postfixUnaryExpressionSyntax.Kind())
                {
                    case SyntaxKind.PostDecrementExpression:
                    case SyntaxKind.PostIncrementExpression:
                        return SpecializedCollections.SingletonEnumerable(this.Compilation.GetSpecialType(SpecialType.System_Int32));
                }

                return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
            }

            private IEnumerable<ITypeSymbol> InferTypeInPrefixUnaryExpression(PrefixUnaryExpressionSyntax prefixUnaryExpression, SyntaxToken? previousToken = null)
            {
                // If we have a position, then we must be after the prefix token.
                Contract.ThrowIfTrue(previousToken.HasValue && previousToken.Value != prefixUnaryExpression.OperatorToken);

                switch (prefixUnaryExpression.Kind())
                {
                    case SyntaxKind.PreDecrementExpression:
                    case SyntaxKind.PreIncrementExpression:
                    case SyntaxKind.UnaryPlusExpression:
                    case SyntaxKind.UnaryMinusExpression:
                    case SyntaxKind.BitwiseNotExpression:
                        // ++, --, +Foo(), -Foo(), ~Foo();
                        return SpecializedCollections.SingletonEnumerable(this.Compilation.GetSpecialType(SpecialType.System_Int32));

                    case SyntaxKind.LogicalNotExpression:
                        // !Foo()
                        return SpecializedCollections.SingletonEnumerable(this.Compilation.GetSpecialType(SpecialType.System_Boolean));
                }

                return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
            }

            private IEnumerable<ITypeSymbol> InferTypeInAwaitExpression(AwaitExpressionSyntax awaitExpression, SyntaxToken? previousToken = null)
            {
                // If we have a position, then we must be after the prefix token.
                Contract.ThrowIfTrue(previousToken.HasValue && previousToken.Value != awaitExpression.AwaitKeyword);

                // await <expression>
                var types = InferTypes(awaitExpression);

                var task = this.Compilation.TaskType();
                var taskOfT = this.Compilation.TaskOfTType();

                if (task == null || taskOfT == null)
                {
                    return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                }

                if (!types.Any())
                {
                    return SpecializedCollections.SingletonEnumerable(task);
                }

                return types.Select(t => t.SpecialType == SpecialType.System_Void ? task : taskOfT.Construct(t));
            }

            private IEnumerable<ITypeSymbol> InferTypeInYieldStatement(YieldStatementSyntax yieldStatement, SyntaxToken? previousToken = null)
            {
                // If we are position based, then we have to be after the return keyword
                if (previousToken.HasValue && (previousToken.Value != yieldStatement.ReturnOrBreakKeyword || yieldStatement.ReturnOrBreakKeyword.IsKind(SyntaxKind.BreakKeyword)))
                {
                    return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                }

                var memberSymbol = GetDeclaredMemberSymbolFromOriginalSemanticModel(_semanticModel, yieldStatement.GetAncestorOrThis<MemberDeclarationSyntax>());

                var memberType = memberSymbol.TypeSwitch(
                        (IMethodSymbol method) => method.ReturnType,
                        (IPropertySymbol property) => property.Type);

                if (memberType is INamedTypeSymbol)
                {
                    if (memberType.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T ||
                        memberType.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerator_T)
                    {
                        return SpecializedCollections.SingletonEnumerable(((INamedTypeSymbol)memberType).TypeArguments[0]);
                    }
                }

                return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
            }

            private IEnumerable<ITypeSymbol> InferTypeForReturnStatement(ReturnStatementSyntax returnStatement, SyntaxToken? previousToken = null)
            {
                // If we are position based, then we have to be after the return statement.
                if (previousToken.HasValue && previousToken.Value != returnStatement.ReturnKeyword)
                {
                    return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                }

                var ancestorExpressions = returnStatement.GetAncestorsOrThis<ExpressionSyntax>();

                // If we're in a lambda, then use the return type of the lambda to figure out what to
                // infer.  i.e.   Func<int,string> f = i => { return Foo(); }
                var lambda = ancestorExpressions.FirstOrDefault(e => e.IsKind(SyntaxKind.ParenthesizedLambdaExpression, SyntaxKind.SimpleLambdaExpression));
                if (lambda != null)
                {
                    return InferTypeInLambdaExpression(lambda);
                }

                // If we are inside a delegate then use the return type of the Invoke Method of the delegate type
                var delegateExpression = ancestorExpressions.FirstOrDefault(e => e.IsKind(SyntaxKind.AnonymousMethodExpression));
                if (delegateExpression != null)
                {
                    var delegateType = InferTypesWorker(delegateExpression).FirstOrDefault();
                    if (delegateType != null && delegateType.IsDelegateType())
                    {
                        var delegateInvokeMethod = delegateType.GetDelegateType(this.Compilation).DelegateInvokeMethod;
                        if (delegateInvokeMethod != null)
                        {
                            return SpecializedCollections.SingletonEnumerable(delegateInvokeMethod.ReturnType);
                        }
                    }
                }

                var memberSymbol = GetDeclaredMemberSymbolFromOriginalSemanticModel(_semanticModel, returnStatement.GetAncestorOrThis<MemberDeclarationSyntax>());

                if (memberSymbol.IsKind(SymbolKind.Method))
                {
                    var method = memberSymbol as IMethodSymbol;
                    if (method.IsAsync)
                    {
                        var typeArguments = method.ReturnType.GetTypeArguments();
                        var taskOfT = this.Compilation.TaskOfTType();

                        return taskOfT != null && method.ReturnType.OriginalDefinition == taskOfT && typeArguments.Any()
                            ? SpecializedCollections.SingletonEnumerable(typeArguments.First())
                            : SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                    }
                    else
                    {
                        return SpecializedCollections.SingletonEnumerable(method.ReturnType);
                    }
                }
                else if (memberSymbol.IsKind(SymbolKind.Property))
                {
                    return SpecializedCollections.SingletonEnumerable((memberSymbol as IPropertySymbol).Type);
                }
                else if (memberSymbol.IsKind(SymbolKind.Field))
                {
                    return SpecializedCollections.SingletonEnumerable((memberSymbol as IFieldSymbol).Type);
                }

                return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
            }

            private ISymbol GetDeclaredMemberSymbolFromOriginalSemanticModel(SemanticModel currentSemanticModel, MemberDeclarationSyntax declarationInCurrentTree)
            {
                var originalSemanticModel = currentSemanticModel.GetOriginalSemanticModel();
                MemberDeclarationSyntax declaration;

                if (currentSemanticModel.IsSpeculativeSemanticModel)
                {
                    var tokenInOriginalTree = originalSemanticModel.SyntaxTree.GetRoot(_cancellationToken).FindToken(currentSemanticModel.OriginalPositionForSpeculation);
                    declaration = tokenInOriginalTree.GetAncestor<MemberDeclarationSyntax>();
                }
                else
                {
                    declaration = declarationInCurrentTree;
                }

                return originalSemanticModel.GetDeclaredSymbol(declaration, _cancellationToken);
            }

            private IEnumerable<ITypeSymbol> InferTypeInSwitchLabel(
                SwitchLabelSyntax switchLabel, SyntaxToken? previousToken = null)
            {
                if (previousToken.HasValue)
                {
                    if (previousToken.Value != switchLabel.Keyword ||
                        switchLabel.Kind() != SyntaxKind.CaseSwitchLabel)
                    {
                        return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                    }
                }

                var switchStatement = (SwitchStatementSyntax)switchLabel.Parent.Parent;
                return GetTypes(switchStatement.Expression);
            }

            private IEnumerable<ITypeSymbol> InferTypeInSwitchStatement(
                SwitchStatementSyntax switchStatement, SyntaxToken? previousToken = null)
            {
                // If we have a position, then it has to be after "switch("
                if (previousToken.HasValue && previousToken.Value != switchStatement.OpenParenToken)
                {
                    return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                }

                // Use the first case label to determine the return type.
                var firstCase =
                    switchStatement.Sections.SelectMany(ss => ss.Labels)
                                                  .FirstOrDefault(label => label.Kind() == SyntaxKind.CaseSwitchLabel)
                                                  as CaseSwitchLabelSyntax;
                if (firstCase != null)
                {
                    var result = GetTypes(firstCase.Value);
                    if (result.Any())
                    {
                        return result;
                    }
                }

                return SpecializedCollections.SingletonEnumerable(this.Compilation.GetSpecialType(SpecialType.System_Int32));
            }

            private IEnumerable<ITypeSymbol> InferTypeInThrowStatement(ThrowStatementSyntax throwStatement, SyntaxToken? previousToken = null)
            {
                // If we have a position, it has to be after the 'throw' keyword.
                if (previousToken.HasValue && previousToken.Value != throwStatement.ThrowKeyword)
                {
                    return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                }

                return SpecializedCollections.SingletonEnumerable(this.Compilation.ExceptionType());
            }

            private IEnumerable<ITypeSymbol> InferTypeInUsingStatement(UsingStatementSyntax usingStatement, SyntaxToken? previousToken = null)
            {
                // If we have a position, it has to be after "using("
                if (previousToken.HasValue && previousToken.Value != usingStatement.OpenParenToken)
                {
                    return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                }

                return SpecializedCollections.SingletonEnumerable(this.Compilation.GetSpecialType(SpecialType.System_IDisposable));
            }

            private IEnumerable<ITypeSymbol> InferTypeInVariableDeclarator(VariableDeclaratorSyntax variableDeclarator)
            {
                var variableType = variableDeclarator.GetVariableType();
                if (variableType == null)
                {
                    return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                }

                var types = GetTypes(variableType).Where(t => !IsUnusableType(t));

                if (variableType.IsVar)
                {
                    var variableDeclaration = variableDeclarator.Parent as VariableDeclarationSyntax;
                    if (variableDeclaration != null)
                    {
                        if (variableDeclaration.IsParentKind(SyntaxKind.UsingStatement))
                        {
                            // using (var v = Foo())
                            return SpecializedCollections.SingletonEnumerable(this.Compilation.GetSpecialType(SpecialType.System_IDisposable));
                        }

                        if (variableDeclaration.IsParentKind(SyntaxKind.ForStatement))
                        {
                            // for (var v = Foo(); ..
                            return SpecializedCollections.SingletonEnumerable(this.Compilation.GetSpecialType(SpecialType.System_Int32));
                        }

                        // Return the types here if they actually bound to a type called 'var'.
                        return types.Where(t => t.Name == "var");
                    }
                }

                return types;
            }

            private IEnumerable<ITypeSymbol> InferTypeInWhileStatement(WhileStatementSyntax whileStatement, SyntaxToken? previousToken = null)
            {
                // If we're position based, then we have to be after the "while("
                if (previousToken.HasValue && previousToken.Value != whileStatement.OpenParenToken)
                {
                    return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
                }

                return SpecializedCollections.SingletonEnumerable(this.Compilation.GetSpecialType(SpecialType.System_Boolean));
            }

            private IEnumerable<ITypeSymbol> GetCollectionElementType(INamedTypeSymbol type, int parameterIndex)
            {
                if (type != null)
                {
                    var parameters = type.GetAllTypeArguments();

                    var elementType = parameters.ElementAtOrDefault(parameterIndex);
                    if (elementType != null)
                    {
                        return SpecializedCollections.SingletonCollection(elementType);
                    }
                }

                return SpecializedCollections.EmptyEnumerable<ITypeSymbol>();
            }
        }
    }
}
