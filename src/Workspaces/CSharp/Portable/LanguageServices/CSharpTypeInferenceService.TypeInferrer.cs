// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class CSharpTypeInferenceService
    {
        private class TypeInferrer : AbstractTypeInferrer
        {
            internal TypeInferrer(
                SemanticModel semanticModel,
                CancellationToken cancellationToken) : base(semanticModel, cancellationToken)
            {
            }

            protected override bool IsUnusableType(ITypeSymbol otherSideType)
            {
                return otherSideType.IsErrorType() &&
                    (otherSideType.Name == string.Empty || otherSideType.Name == "var");
            }

            protected override IEnumerable<TypeInferenceInfo> GetTypes_DoNotCallDirectly(SyntaxNode node, bool objectAsDefault)
            {
                var types = GetTypesSimple(node).Where(IsUsableTypeFunc);
                if (types.Any())
                {
                    return types;
                }

                return GetTypesComplex(node).Where(IsUsableTypeFunc);
            }

            private static bool DecomposeBinaryOrAssignmentExpression(SyntaxNode node, out SyntaxToken operatorToken, out ExpressionSyntax left, out ExpressionSyntax right)
            {
                if (node is BinaryExpressionSyntax binaryExpression)
                {
                    operatorToken = binaryExpression.OperatorToken;
                    left = binaryExpression.Left;
                    right = binaryExpression.Right;
                    return true;
                }

                if (node is AssignmentExpressionSyntax assignmentExpression)
                {
                    operatorToken = assignmentExpression.OperatorToken;
                    left = assignmentExpression.Left;
                    right = assignmentExpression.Right;
                    return true;
                }

                operatorToken = default;
                left = right = null;
                return false;
            }

            private IEnumerable<TypeInferenceInfo> GetTypesComplex(SyntaxNode node)
            {
                if (DecomposeBinaryOrAssignmentExpression(node,
                        out var operatorToken, out var left, out var right))
                {
                    var types = InferTypeInBinaryOrAssignmentExpression((ExpressionSyntax)node, operatorToken, left, right, left).Where(IsUsableTypeFunc);
                    if (types.IsEmpty())
                    {
                        types = InferTypeInBinaryOrAssignmentExpression((ExpressionSyntax)node, operatorToken, left, right, right).Where(IsUsableTypeFunc);
                    }

                    return types;
                }

                // TODO(cyrusn): More cases if necessary.
                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            private IEnumerable<TypeInferenceInfo> GetTypesSimple(SyntaxNode node)
            {
                if (node is RefTypeSyntax refType)
                {
                    return GetTypes(refType.Type);
                }
                else if (node != null)
                {
                    var typeInfo = SemanticModel.GetTypeInfo(node, CancellationToken);
                    var symbolInfo = SemanticModel.GetSymbolInfo(node, CancellationToken);

                    if (symbolInfo.CandidateReason != CandidateReason.WrongArity)
                    {
                        var typeInferenceInfo = new TypeInferenceInfo(typeInfo.GetTypeWithFlowNullability());

                        // If it bound to a method, try to get the Action/Func form of that method.
                        if (typeInferenceInfo.InferredType == null)
                        {
                            var allSymbols = symbolInfo.GetAllSymbols();
                            if (allSymbols.Length == 1 &&
                                allSymbols[0].Kind == SymbolKind.Method)
                            {
                                var method = allSymbols[0];
                                typeInferenceInfo = new TypeInferenceInfo(method.ConvertToType(this.Compilation));
                            }
                        }

                        if (IsUsableTypeFunc(typeInferenceInfo))
                        {
                            return SpecializedCollections.SingletonEnumerable(typeInferenceInfo);
                        }
                    }
                }

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            protected override IEnumerable<TypeInferenceInfo> InferTypesWorker_DoNotCallDirectly(
                SyntaxNode node)
            {
                var expression = node as ExpressionSyntax;
                if (expression != null)
                {
                    expression = expression.WalkUpParentheses();
                    node = expression;
                }

                var parent = node.Parent;

                switch (parent)
                {
                    case AnonymousObjectMemberDeclaratorSyntax memberDeclarator: return InferTypeInMemberDeclarator(memberDeclarator);
                    case ArgumentSyntax argument: return InferTypeInArgument(argument);
                    case ArrayCreationExpressionSyntax arrayCreationExpression: return InferTypeInArrayCreationExpression(arrayCreationExpression);
                    case ArrayRankSpecifierSyntax arrayRankSpecifier: return InferTypeInArrayRankSpecifier(arrayRankSpecifier);
                    case ArrayTypeSyntax arrayType: return InferTypeInArrayType(arrayType);
                    case ArrowExpressionClauseSyntax arrowClause: return InferTypeInArrowExpressionClause(arrowClause);
                    case AssignmentExpressionSyntax assignmentExpression: return InferTypeInBinaryOrAssignmentExpression(assignmentExpression, assignmentExpression.OperatorToken, assignmentExpression.Left, assignmentExpression.Right, expression);
                    case AttributeArgumentSyntax attribute: return InferTypeInAttributeArgument(attribute);
                    case AttributeSyntax attribute: return InferTypeInAttribute(attribute);
                    case AwaitExpressionSyntax awaitExpression: return InferTypeInAwaitExpression(awaitExpression);
                    case BinaryExpressionSyntax binaryExpression: return InferTypeInBinaryOrAssignmentExpression(binaryExpression, binaryExpression.OperatorToken, binaryExpression.Left, binaryExpression.Right, expression);
                    case CastExpressionSyntax castExpression: return InferTypeInCastExpression(castExpression, expression);
                    case CatchDeclarationSyntax catchDeclaration: return InferTypeInCatchDeclaration(catchDeclaration);
                    case CatchFilterClauseSyntax catchFilterClause: return InferTypeInCatchFilterClause(catchFilterClause);
                    case CheckedExpressionSyntax checkedExpression: return InferTypes(checkedExpression);
                    case ConditionalAccessExpressionSyntax conditionalAccessExpression: return InferTypeInConditionalAccessExpression(conditionalAccessExpression);
                    case ConditionalExpressionSyntax conditionalExpression: return InferTypeInConditionalExpression(conditionalExpression, expression);
                    case ConstantPatternSyntax constantPattern: return InferTypeInConstantPattern(constantPattern);
                    case DoStatementSyntax doStatement: return InferTypeInDoStatement(doStatement);
                    case EqualsValueClauseSyntax equalsValue: return InferTypeInEqualsValueClause(equalsValue);
                    case ExpressionStatementSyntax expressionStatement: return InferTypeInExpressionStatement(expressionStatement);
                    case ForEachStatementSyntax forEachStatement: return InferTypeInForEachStatement(forEachStatement, expression);
                    case ForStatementSyntax forStatement: return InferTypeInForStatement(forStatement, expression);
                    case IfStatementSyntax ifStatement: return InferTypeInIfStatement(ifStatement);
                    case InitializerExpressionSyntax initializerExpression: return InferTypeInInitializerExpression(initializerExpression, expression);
                    case IsPatternExpressionSyntax isPatternExpression: return InferTypeInIsPatternExpression(isPatternExpression, node);
                    case LockStatementSyntax lockStatement: return InferTypeInLockStatement(lockStatement);
                    case MemberAccessExpressionSyntax memberAccessExpression: return InferTypeInMemberAccessExpression(memberAccessExpression, expression);
                    case NameColonSyntax nameColon: return InferTypeInNameColon(nameColon);
                    case NameEqualsSyntax nameEquals: return InferTypeInNameEquals(nameEquals);
                    case LambdaExpressionSyntax lambdaExpression: return InferTypeInLambdaExpression(lambdaExpression);
                    case PostfixUnaryExpressionSyntax postfixUnary: return InferTypeInPostfixUnaryExpression(postfixUnary);
                    case PrefixUnaryExpressionSyntax prefixUnary: return InferTypeInPrefixUnaryExpression(prefixUnary);
                    case RecursivePatternSyntax propertyPattern: return InferTypeInRecursivePattern(propertyPattern);
                    case PropertyPatternClauseSyntax propertySubpattern: return InferTypeInPropertyPatternClause(propertySubpattern, node);
                    case RefExpressionSyntax refExpression: return InferTypeInRefExpression(refExpression);
                    case ReturnStatementSyntax returnStatement: return InferTypeForReturnStatement(returnStatement);
                    case SubpatternSyntax subpattern: return InferTypeInSubpattern(subpattern, node);
                    case SwitchLabelSyntax switchLabel: return InferTypeInSwitchLabel(switchLabel);
                    case SwitchStatementSyntax switchStatement: return InferTypeInSwitchStatement(switchStatement);
                    case ThrowExpressionSyntax throwExpression: return InferTypeInThrowExpression(throwExpression);
                    case ThrowStatementSyntax throwStatement: return InferTypeInThrowStatement(throwStatement);
                    case UsingStatementSyntax usingStatement: return InferTypeInUsingStatement(usingStatement);
                    case WhenClauseSyntax whenClause: return InferTypeInWhenClause(whenClause);
                    case WhileStatementSyntax whileStatement: return InferTypeInWhileStatement(whileStatement);
                    case YieldStatementSyntax yieldStatement: return InferTypeInYieldStatement(yieldStatement);
                    default: return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }
            }

            protected override IEnumerable<TypeInferenceInfo> InferTypesWorker_DoNotCallDirectly(int position)
            {
                var syntaxTree = SemanticModel.SyntaxTree;
                var token = syntaxTree.FindTokenOnLeftOfPosition(position, CancellationToken);
                token = token.GetPreviousTokenIfTouchingWord(position);

                var parent = token.Parent;

                switch (parent)
                {
                    case AnonymousObjectCreationExpressionSyntax anonymousObjectCreation: return InferTypeInAnonymousObjectCreation(anonymousObjectCreation, token);
                    case AnonymousObjectMemberDeclaratorSyntax memberDeclarator: return InferTypeInMemberDeclarator(memberDeclarator, token);
                    case ArgumentListSyntax argument: return InferTypeInArgumentList(argument, token);
                    case ArgumentSyntax argument: return InferTypeInArgument(argument, token);
                    case ArrayCreationExpressionSyntax arrayCreationExpression: return InferTypeInArrayCreationExpression(arrayCreationExpression, token);
                    case ArrayRankSpecifierSyntax arrayRankSpecifier: return InferTypeInArrayRankSpecifier(arrayRankSpecifier, token);
                    case ArrayTypeSyntax arrayType: return InferTypeInArrayType(arrayType, token);
                    case ArrowExpressionClauseSyntax arrowClause: return InferTypeInArrowExpressionClause(arrowClause);
                    case AssignmentExpressionSyntax assignmentExpression: return InferTypeInBinaryOrAssignmentExpression(assignmentExpression, assignmentExpression.OperatorToken, assignmentExpression.Left, assignmentExpression.Right, previousToken: token);
                    case AttributeArgumentListSyntax attributeArgumentList: return InferTypeInAttributeArgumentList(attributeArgumentList, token);
                    case AttributeArgumentSyntax argument: return InferTypeInAttributeArgument(argument, token);
                    case AttributeListSyntax attributeDeclaration: return InferTypeInAttributeDeclaration(attributeDeclaration, token);
                    case AttributeTargetSpecifierSyntax attributeTargetSpecifier: return InferTypeInAttributeTargetSpecifier(attributeTargetSpecifier, token);
                    case AwaitExpressionSyntax awaitExpression: return InferTypeInAwaitExpression(awaitExpression, token);
                    case BinaryExpressionSyntax binaryExpression: return InferTypeInBinaryOrAssignmentExpression(binaryExpression, binaryExpression.OperatorToken, binaryExpression.Left, binaryExpression.Right, previousToken: token);
                    case BracketedArgumentListSyntax bracketedArgumentList: return InferTypeInBracketedArgumentList(bracketedArgumentList, token);
                    case CastExpressionSyntax castExpression: return InferTypeInCastExpression(castExpression, previousToken: token);
                    case CatchDeclarationSyntax catchDeclaration: return InferTypeInCatchDeclaration(catchDeclaration, token);
                    case CatchFilterClauseSyntax catchFilterClause: return InferTypeInCatchFilterClause(catchFilterClause, token);
                    case CheckedExpressionSyntax checkedExpression: return InferTypes(checkedExpression);
                    case ConditionalExpressionSyntax conditionalExpression: return InferTypeInConditionalExpression(conditionalExpression, previousToken: token);
                    case DefaultExpressionSyntax defaultExpression: return InferTypeInDefaultExpression(defaultExpression);
                    case DoStatementSyntax doStatement: return InferTypeInDoStatement(doStatement, token);
                    case EqualsValueClauseSyntax equalsValue: return InferTypeInEqualsValueClause(equalsValue, token);
                    case ExpressionStatementSyntax expressionStatement: return InferTypeInExpressionStatement(expressionStatement, token);
                    case ForEachStatementSyntax forEachStatement: return InferTypeInForEachStatement(forEachStatement, previousToken: token);
                    case ForStatementSyntax forStatement: return InferTypeInForStatement(forStatement, previousToken: token);
                    case IfStatementSyntax ifStatement: return InferTypeInIfStatement(ifStatement, token);
                    case ImplicitArrayCreationExpressionSyntax implicitArray: return InferTypeInImplicitArrayCreation(implicitArray, token);
                    case InitializerExpressionSyntax initializerExpression: return InferTypeInInitializerExpression(initializerExpression, previousToken: token);
                    case LockStatementSyntax lockStatement: return InferTypeInLockStatement(lockStatement, token);
                    case MemberAccessExpressionSyntax memberAccessExpression: return InferTypeInMemberAccessExpression(memberAccessExpression, previousToken: token);
                    case NameColonSyntax nameColon: return InferTypeInNameColon(nameColon, token);
                    case NameEqualsSyntax nameEquals: return InferTypeInNameEquals(nameEquals, token);
                    case ObjectCreationExpressionSyntax objectCreation: return InferTypeInObjectCreationExpression(objectCreation, token);
                    case LambdaExpressionSyntax lambdaExpression: return InferTypeInLambdaExpression(lambdaExpression, token);
                    case PostfixUnaryExpressionSyntax postfixUnary: return InferTypeInPostfixUnaryExpression(postfixUnary, token);
                    case PrefixUnaryExpressionSyntax prefixUnary: return InferTypeInPrefixUnaryExpression(prefixUnary, token);
                    case ReturnStatementSyntax returnStatement: return InferTypeForReturnStatement(returnStatement, token);
                    case SwitchLabelSyntax switchLabel: return InferTypeInSwitchLabel(switchLabel, token);
                    case SwitchStatementSyntax switchStatement: return InferTypeInSwitchStatement(switchStatement, token);
                    case ThrowStatementSyntax throwStatement: return InferTypeInThrowStatement(throwStatement, token);
                    case UsingStatementSyntax usingStatement: return InferTypeInUsingStatement(usingStatement, token);
                    case WhenClauseSyntax whenClause: return InferTypeInWhenClause(whenClause, token);
                    case WhileStatementSyntax whileStatement: return InferTypeInWhileStatement(whileStatement, token);
                    case YieldStatementSyntax yieldStatement: return InferTypeInYieldStatement(yieldStatement, token);
                    default: return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInAnonymousObjectCreation(AnonymousObjectCreationExpressionSyntax expression, SyntaxToken previousToken)
            {
                if (previousToken == expression.NewKeyword)
                {
                    return InferTypes(expression.SpanStart);
                }

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInArgument(
                ArgumentSyntax argument, SyntaxToken? previousToken = null)
            {
                if (previousToken.HasValue)
                {
                    // If we have a position, then it must be after the colon in a named argument.
                    if (argument.NameColon == null || argument.NameColon.ColonToken != previousToken)
                    {
                        return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                    }
                }

                if (argument.Parent != null)
                {
                    if (argument.Parent.Parent is ConstructorInitializerSyntax initializer)
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
                        // new Outer(Goo());
                        //
                        // new Outer(a: Goo());
                        //
                        // etc.
                        var creation = argument.Parent.Parent as ObjectCreationExpressionSyntax;
                        var index = creation.ArgumentList.Arguments.IndexOf(argument);

                        return InferTypeInObjectCreationExpression(creation, index, argument);
                    }

                    if (argument.Parent.IsParentKind(SyntaxKind.ElementAccessExpression))
                    {
                        // Outer[Goo()];
                        //
                        // Outer[a: Goo()];
                        //
                        // etc.
                        var elementAccess = argument.Parent.Parent as ElementAccessExpressionSyntax;
                        var index = elementAccess.ArgumentList.Arguments.IndexOf(argument);

                        return InferTypeInElementAccessExpression(elementAccess, index, argument);
                    }

                    if (argument.IsParentKind(SyntaxKind.TupleExpression))
                    {
                        return InferTypeInTupleExpression((TupleExpressionSyntax)argument.Parent, argument);
                    }
                }

                if (argument.Parent.IsParentKind(SyntaxKind.ImplicitElementAccess) &&
                    argument.Parent.Parent.IsParentKind(SyntaxKind.SimpleAssignmentExpression) &&
                    argument.Parent.Parent.Parent.IsParentKind(SyntaxKind.ObjectInitializerExpression) &&
                    argument.Parent.Parent.Parent.Parent.IsParentKind(SyntaxKind.ObjectCreationExpression))
                {
                    var objectCreation = (ObjectCreationExpressionSyntax)argument.Parent.Parent.Parent.Parent.Parent;
                    var types = GetTypes(objectCreation).Select(t => t.InferredType);

                    if (types.Any(t => t is INamedTypeSymbol))
                    {
                        return types.OfType<INamedTypeSymbol>().SelectMany(t =>
                            GetCollectionElementType(t));
                    }
                }

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInTupleExpression(
                TupleExpressionSyntax tupleExpression, ArgumentSyntax argument)
            {
                var index = tupleExpression.Arguments.IndexOf(argument);
                var parentTypes = InferTypes(tupleExpression);

                return parentTypes.Select(typeInfo => typeInfo.InferredType)
                                  .OfType<INamedTypeSymbol>()
                                  .Where(namedType => namedType.IsTupleType && index < namedType.TupleElements.Length)
                                  .Select(tupleType => new TypeInferenceInfo(tupleType.TupleElements[index].GetTypeWithAnnotatedNullability()));
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInAttributeArgument(AttributeArgumentSyntax argument, SyntaxToken? previousToken = null)
            {
                if (previousToken.HasValue)
                {
                    // If we have a position, then it must be after the colon or equals in an argument.
                    if (argument.NameColon == null || argument.NameColon.ColonToken != previousToken || argument.NameEquals.EqualsToken != previousToken)
                    {
                        return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                    }
                }

                if (argument.Parent != null)
                {
                    if (argument.Parent.Parent is AttributeSyntax attribute)
                    {
                        var index = attribute.ArgumentList.Arguments.IndexOf(argument);
                        return InferTypeInAttribute(attribute, index, argument);
                    }
                }

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInConstructorInitializer(ConstructorInitializerSyntax initializer, int index, ArgumentSyntax argument = null)
            {
                var info = SemanticModel.GetSymbolInfo(initializer, CancellationToken);
                var methods = info.GetBestOrAllSymbols().OfType<IMethodSymbol>();
                return InferTypeInArgument(index, methods, argument, parentInvocationExpressionToTypeInfer: null);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInObjectCreationExpression(ObjectCreationExpressionSyntax expression, SyntaxToken previousToken)
            {
                // A couple of broken code scenarios where the new keyword in objectcreationexpression
                // appears to be a part of a subsequent assignment.  For example:
                //
                //       new Form
                //       {
                //           Location = new $$
                //           StartPosition = FormStartPosition.CenterParent
                //       };
                //  The 'new' token is part of an assignment of the assignment to StartPosition,
                //  but the user is really trying to assign to Location.
                //
                // Similarly:
                //      bool b;
                //      Task task = new $$
                //      b = false;
                // The 'new' token is part of an assignment of the assignment to b, but the user 
                // is really trying to assign to task.
                //
                // In both these cases, we simply back up before the 'new' if it follows an equals
                // and start the inference again.
                //
                // Analogously, but in a method call:
                //      Test(new $$
                //      o = s
                // or:
                //      Test(1, new $$
                //      o = s
                // The new is part of the assignment to o but the user is really trying to 
                // add a parameter to the method call.
                if (previousToken.Kind() == SyntaxKind.NewKeyword &&
                    previousToken.GetPreviousToken().IsKind(SyntaxKind.EqualsToken, SyntaxKind.OpenParenToken, SyntaxKind.CommaToken))
                {
                    return InferTypes(previousToken.SpanStart);
                }

                return InferTypes(expression);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInObjectCreationExpression(ObjectCreationExpressionSyntax creation, int index, ArgumentSyntax argumentOpt = null)
            {
                var info = SemanticModel.GetSymbolInfo(creation.Type, CancellationToken);
                var type = info.Symbol as INamedTypeSymbol;

                if (type == null)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                if (type.TypeKind == TypeKind.Delegate)
                {
                    // new SomeDelegateType( here );
                    //
                    // They're actually instantiating a delegate, so the delegate type is
                    // that type.
                    return CreateResult(type);
                }

                var constructors = type.InstanceConstructors.Where(m => m.Parameters.Length > index);
                return InferTypeInArgument(index, constructors, argumentOpt, parentInvocationExpressionToTypeInfer: null);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInInvocationExpression(
                InvocationExpressionSyntax invocation, int index, ArgumentSyntax argumentOpt = null)
            {
                // Check all the methods that have at least enough arguments to support
                // being called with argument at this position.  Note: if they're calling an
                // extension method then it will need one more argument in order for us to
                // call it.
                var info = SemanticModel.GetSymbolInfo(invocation, CancellationToken);
                var methods = info.GetBestOrAllSymbols().OfType<IMethodSymbol>();

                // Overload resolution (see DevDiv 611477) in certain extension method cases
                // can result in GetSymbolInfo returning nothing. In this case, get the 
                // method group info, which is what signature help already does.
                if (info.Symbol == null)
                {
                    var memberGroupMethods =
                        SemanticModel.GetMemberGroup(invocation.Expression, CancellationToken)
                                     .OfType<IMethodSymbol>();

                    methods = methods.Concat(memberGroupMethods).Distinct();
                }

                return InferTypeInArgument(index, methods, argumentOpt, invocation);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInArgumentList(ArgumentListSyntax argumentList, SyntaxToken previousToken)
            {
                // Has to follow the ( or a ,
                if (previousToken != argumentList.OpenParenToken && previousToken.Kind() != SyntaxKind.CommaToken)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                switch (argumentList.Parent)
                {
                    case InvocationExpressionSyntax invocation:
                        {
                            var index = this.GetArgumentListIndex(argumentList, previousToken);
                            return InferTypeInInvocationExpression(invocation, index);
                        }

                    case ObjectCreationExpressionSyntax objectCreation:
                        {
                            var index = this.GetArgumentListIndex(argumentList, previousToken);
                            return InferTypeInObjectCreationExpression(objectCreation, index);
                        }

                    case ConstructorInitializerSyntax constructorInitializer:
                        {
                            var index = this.GetArgumentListIndex(argumentList, previousToken);
                            return InferTypeInConstructorInitializer(constructorInitializer, index);
                        }
                }

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInAttributeArgumentList(AttributeArgumentListSyntax attributeArgumentList, SyntaxToken previousToken)
            {
                // Has to follow the ( or a ,
                if (previousToken != attributeArgumentList.OpenParenToken && previousToken.Kind() != SyntaxKind.CommaToken)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                if (attributeArgumentList.Parent is AttributeSyntax attribute)
                {
                    var index = this.GetArgumentListIndex(attributeArgumentList, previousToken);
                    return InferTypeInAttribute(attribute, index);
                }

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInAttribute(AttributeSyntax attribute, int index, AttributeArgumentSyntax argumentOpt = null)
            {
                var info = SemanticModel.GetSymbolInfo(attribute, CancellationToken);
                var methods = info.GetBestOrAllSymbols().OfType<IMethodSymbol>();
                return InferTypeInAttributeArgument(index, methods, argumentOpt);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInElementAccessExpression(
                ElementAccessExpressionSyntax elementAccess, int index, ArgumentSyntax argumentOpt = null)
            {
                var info = SemanticModel.GetTypeInfo(elementAccess.Expression, CancellationToken);
                if (info.Type is INamedTypeSymbol type)
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
                return CreateResult(this.Compilation.GetSpecialType(SpecialType.System_Int32));
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInAttributeArgument(int index, IEnumerable<IMethodSymbol> methods, AttributeArgumentSyntax argumentOpt = null)
            {
                return InferTypeInAttributeArgument(index, methods.Select(m => m.Parameters), argumentOpt);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInArgument(int index, IEnumerable<IMethodSymbol> methods, ArgumentSyntax argumentOpt, InvocationExpressionSyntax parentInvocationExpressionToTypeInfer)
            {
                if (parentInvocationExpressionToTypeInfer != null)
                {
                    // We're trying to figure out the signature of a method we're an argument to. 
                    // That method may be generic, and we might end up using one of its generic
                    // type parameters in the type we infer.  First, let's see if we can instantiate
                    // the methods so that the type can be inferred better.
                    var invocationTypes = this.InferTypes(parentInvocationExpressionToTypeInfer).Select(t => t.InferredType).ToList();
                    var instantiatedMethods = methods.Select(m => Instantiate(m, invocationTypes)).ToList();

                    // Now that we've instantiated the methods, filter down to the ones that 
                    // will actually return a viable type given where this invocation expression
                    // is.
                    var filteredMethods = instantiatedMethods.Where(m =>
                        invocationTypes.Any(t => Compilation.ClassifyConversion(m.ReturnType.WithoutNullability(), t.WithoutNullability()).IsImplicit)).ToList();

                    // If we filtered down to nothing, then just fall back to the instantiated list.
                    // this is a best effort after all.
                    methods = filteredMethods.Any() ? filteredMethods : instantiatedMethods;
                }

                return InferTypeInArgument(index, methods.Select(m => m.Parameters), argumentOpt);
            }

            private IMethodSymbol Instantiate(IMethodSymbol method, IList<ITypeSymbol> invocationTypes)
            {
                // No need to instantiate if this isn't a generic method.
                if (method.TypeArguments.Length == 0)
                {
                    return method;
                }

                // Can't infer the type parameters if this method doesn't have a return type.
                // Note: this is because this code path is specifically flowing type information
                // backward through the return type.  Type information is already flowed forward
                // through arguments by the compiler when we get the initial set of methods.
                if (method.ReturnsVoid)
                {
                    return method;
                }

                // If the method has already been constructed poorly (i.e. with error types for type 
                // arguments), then unconstruct it.
                if (method.TypeArguments.Any(t => t.Kind == SymbolKind.ErrorType))
                {
                    method = method.ConstructedFrom;
                }

                IDictionary<ITypeParameterSymbol, ITypeSymbol> bestMap = null;
                foreach (var type in invocationTypes)
                {
                    // Ok.  We inferred a type for this location, and we have the return type of this 
                    // method.  See if we can then assign any values for type parameters.
                    var map = DetermineTypeParameterMapping(type, method.ReturnType);
                    if (map.Count > 0 && (bestMap == null || map.Count > bestMap.Count))
                    {
                        bestMap = map;
                    }
                }

                if (bestMap == null)
                {
                    return method;
                }

                var typeArguments = method.ConstructedFrom.TypeParameters
                    .Select(tp => bestMap.GetValueOrDefault(tp) ?? tp).ToArray();
                return method.ConstructedFrom.ConstructWithNullability(typeArguments);
            }

            private Dictionary<ITypeParameterSymbol, ITypeSymbol> DetermineTypeParameterMapping(ITypeSymbol inferredType, ITypeSymbol returnType)
            {
                var result = new Dictionary<ITypeParameterSymbol, ITypeSymbol>();
                DetermineTypeParameterMapping(inferredType, returnType, result);
                return result;
            }

            private void DetermineTypeParameterMapping(ITypeSymbol inferredType, ITypeSymbol returnType, Dictionary<ITypeParameterSymbol, ITypeSymbol> result)
            {
                if (inferredType == null || returnType == null)
                {
                    return;
                }

                if (returnType.Kind == SymbolKind.TypeParameter)
                {
                    if (inferredType.Kind != SymbolKind.TypeParameter)
                    {
                        var returnTypeParameter = (ITypeParameterSymbol)returnType;
                        if (!result.ContainsKey(returnTypeParameter))
                        {
                            result[returnTypeParameter] = inferredType;
                        }
                        return;
                    }
                }

                if (inferredType.Kind != returnType.Kind)
                {
                    return;
                }

                switch (inferredType.Kind)
                {
                    case SymbolKind.ArrayType:
                        DetermineTypeParameterMapping(((IArrayTypeSymbol)inferredType).ElementType, ((IArrayTypeSymbol)returnType).ElementType, result);
                        return;
                    case SymbolKind.PointerType:
                        DetermineTypeParameterMapping(((IPointerTypeSymbol)inferredType).PointedAtType, ((IPointerTypeSymbol)returnType).PointedAtType, result);
                        return;
                    case SymbolKind.NamedType:
                        var inferredNamedType = (INamedTypeSymbol)inferredType;
                        var returnNamedType = (INamedTypeSymbol)returnType;
                        if (inferredNamedType.TypeArguments.Length == returnNamedType.TypeArguments.Length)
                        {
                            for (int i = 0, n = inferredNamedType.TypeArguments.Length; i < n; i++)
                            {
                                DetermineTypeParameterMapping(inferredNamedType.TypeArguments[i], returnNamedType.TypeArguments[i], result);
                            }
                        }
                        return;
                }
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInAttributeArgument(
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
                return InferTypeInArgument(index, parameterizedSymbols, name, RefKind.None);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInArgument(
                int index,
                IEnumerable<ImmutableArray<IParameterSymbol>> parameterizedSymbols,
                ArgumentSyntax argumentOpt)
            {
                var name = argumentOpt != null && argumentOpt.NameColon != null ? argumentOpt.NameColon.Name.Identifier.ValueText : null;
                var refKind = argumentOpt.GetRefKind();
                return InferTypeInArgument(index, parameterizedSymbols, name, refKind);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInArgument(
                int index,
                IEnumerable<ImmutableArray<IParameterSymbol>> parameterizedSymbols,
                string name,
                RefKind refKind)
            {
                // If the callsite has a named argument, then try to find a method overload that has a
                // parameter with that name.  If we can find one, then return the type of that one.
                if (name != null)
                {
                    var matchingNameParameters = parameterizedSymbols.SelectMany(m => m)
                                                                     .Where(p => p.Name == name)
                                                                     .Select(p => new TypeInferenceInfo(p.GetTypeWithAnnotatedNullability(), p.IsParams));

                    return matchingNameParameters;
                }

                var allParameters = ArrayBuilder<TypeInferenceInfo>.GetInstance();
                var matchingRefParameters = ArrayBuilder<TypeInferenceInfo>.GetInstance();
                try
                {
                    foreach (var parameterSet in parameterizedSymbols)
                    {
                        if (index < parameterSet.Length)
                        {
                            var parameter = parameterSet[index];
                            var info = new TypeInferenceInfo(parameter.GetTypeWithAnnotatedNullability(), parameter.IsParams);
                            allParameters.Add(info);

                            if (parameter.RefKind == refKind)
                            {
                                matchingRefParameters.Add(info);
                            }
                        }
                    }

                    return matchingRefParameters.Count > 0
                        ? matchingRefParameters.ToImmutable()
                        : allParameters.ToImmutable();
                }
                finally
                {
                    allParameters.Free();
                    matchingRefParameters.Free();
                }
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInArrayCreationExpression(
                ArrayCreationExpressionSyntax arrayCreationExpression, SyntaxToken? previousToken = null)
            {
                if (previousToken.HasValue && previousToken.Value != arrayCreationExpression.NewKeyword)
                {
                    // Has to follow the 'new' keyword. 
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                if (previousToken.HasValue && previousToken.Value.GetPreviousToken().Kind() == SyntaxKind.EqualsToken)
                {
                    // We parsed an array creation but the token before `new` is `=`.
                    // This could be a case like:
                    //
                    // int[] array;
                    // Program p = new |
                    // array[4] = 4;
                    //
                    // This is similar to the cases described in `InferTypeInObjectCreationExpression`.
                    // Again, all we have to do is back up to before `new`.

                    return InferTypes(previousToken.Value.SpanStart);
                }

                var outerTypes = InferTypes(arrayCreationExpression);
                return outerTypes.Where(o => o.InferredType is IArrayTypeSymbol);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInArrayRankSpecifier(ArrayRankSpecifierSyntax arrayRankSpecifier, SyntaxToken? previousToken = null)
            {
                // If we have a token, and it's not the open bracket or one of the commas, then no
                // inference.
                if (previousToken == arrayRankSpecifier.CloseBracketToken)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                return CreateResult(this.Compilation.GetSpecialType(SpecialType.System_Int32));
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInArrayType(ArrayTypeSyntax arrayType, SyntaxToken? previousToken = null)
            {
                if (previousToken.HasValue)
                {
                    // TODO(cyrusn): NYI.  Handle this appropriately if we need to.
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                // Bind the array type, then unwrap whatever we get back based on the number of rank
                // specifiers we see.
                var currentTypes = InferTypes(arrayType);
                for (var i = 0; i < arrayType.RankSpecifiers.Count; i++)
                {
                    currentTypes = currentTypes.Select(t => t.InferredType).OfType<IArrayTypeSymbol>()
                                               .SelectAsArray(a => new TypeInferenceInfo(a.GetElementTypeWithAnnotatedNullability()));
                }
                return currentTypes;
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInAttribute(AttributeSyntax attribute)
                => CreateResult(this.Compilation.AttributeType());

            private IEnumerable<TypeInferenceInfo> InferTypeInAttributeDeclaration(AttributeListSyntax attributeDeclaration, SyntaxToken? previousToken)
            {
                // If we have a position, then it has to be after the open bracket.
                if (previousToken.HasValue && previousToken.Value != attributeDeclaration.OpenBracketToken)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                return CreateResult(this.Compilation.AttributeType());
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInAttributeTargetSpecifier(
                AttributeTargetSpecifierSyntax attributeTargetSpecifier,
                SyntaxToken? previousToken)
            {
                // If we have a position, then it has to be after the colon.
                if (previousToken.HasValue && previousToken.Value != attributeTargetSpecifier.ColonToken)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                return CreateResult(this.Compilation.AttributeType());
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInBracketedArgumentList(BracketedArgumentListSyntax bracketedArgumentList, SyntaxToken previousToken)
            {
                // Has to follow the [ or a ,
                if (previousToken != bracketedArgumentList.OpenBracketToken && previousToken.Kind() != SyntaxKind.CommaToken)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                if (bracketedArgumentList.Parent is ElementAccessExpressionSyntax elementAccess)
                {
                    var index = GetArgumentListIndex(bracketedArgumentList, previousToken);
                    return InferTypeInElementAccessExpression(
                        elementAccess, index);
                }

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
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

            private IEnumerable<TypeInferenceInfo> InferTypeInBinaryOrAssignmentExpression(ExpressionSyntax binop, SyntaxToken operatorToken, ExpressionSyntax left, ExpressionSyntax right, ExpressionSyntax expressionOpt = null, SyntaxToken? previousToken = null)
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
                            // x << Goo(), x >> Goo(), x <<= Goo(), x >>= Goo()
                            return CreateResult(this.Compilation.GetSpecialType(SpecialType.System_Int32));
                        }

                        break;
                }

                // Infer operands of && and || as bool regardless of the other operand.
                if (operatorToken.Kind() == SyntaxKind.AmpersandAmpersandToken ||
                    operatorToken.Kind() == SyntaxKind.BarBarToken)
                {
                    return CreateResult(SpecialType.System_Boolean);
                }

                // Infer type for deconstruction
                if (binop.Kind() == SyntaxKind.SimpleAssignmentExpression &&
                    ((AssignmentExpressionSyntax)binop).IsDeconstruction())
                {
                    return InferTypeInVariableComponentAssignment(left);
                }

                // Try to figure out what's on the other side of the binop.  If we can, then just that
                // type.  This is often a reasonable heuristics to use for most operators.  NOTE(cyrusn):
                // we could try to bind the token to see what overloaded operators it corresponds to.
                // But the gain is pretty marginal IMO.
                var otherSide = onRightOfToken ? left : right;

                var otherSideTypes = GetTypes(otherSide);
                if (otherSideTypes.Any())
                {
                    // Don't infer delegate types except in assignments. They're unlikely to be what the
                    // user needs and can cause lambda suggestion mode while
                    // typing type arguments:
                    // https://github.com/dotnet/roslyn/issues/14492
                    if (!(binop is AssignmentExpressionSyntax))
                    {
                        otherSideTypes = otherSideTypes.Where(t => !t.InferredType.IsDelegateType());
                    }

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
                    if (parentTypes.Any(parentType => parentType.InferredType.SpecialType == SpecialType.System_String || parentType.InferredType.TypeKind == TypeKind.Delegate))
                    {
                        return parentTypes.Where(parentType => parentType.InferredType.SpecialType == SpecialType.System_String || parentType.InferredType.TypeKind == TypeKind.Delegate);
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
                        return CreateResult(this.Compilation.GetSpecialType(SpecialType.System_Int32));

                    case SyntaxKind.BarEqualsToken:
                    case SyntaxKind.AmpersandEqualsToken:
                        // NOTE(cyrusn): |= and &= can be used for both ints and bools  However, in the
                        // case where there isn't enough information to determine which the user wanted,
                        // I'm just defaulting to bool based on personal preference.
                        return CreateResult(SpecialType.System_Boolean);
                }

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInCastExpression(CastExpressionSyntax castExpression, ExpressionSyntax expressionOpt = null, SyntaxToken? previousToken = null)
            {
                if (expressionOpt != null && castExpression.Expression != expressionOpt)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                // If we have a position, then it has to be after the close paren.
                if (previousToken.HasValue && previousToken.Value != castExpression.CloseParenToken)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                return this.GetTypes(castExpression.Type);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInCatchDeclaration(CatchDeclarationSyntax catchDeclaration, SyntaxToken? previousToken = null)
            {
                // If we have a position, it has to be after "catch("
                if (previousToken.HasValue && previousToken.Value != catchDeclaration.OpenParenToken)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                return CreateResult(this.Compilation.ExceptionType());
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInCatchFilterClause(CatchFilterClauseSyntax catchFilterClause, SyntaxToken? previousToken = null)
            {
                // If we have a position, it has to be after "if ("
                if (previousToken.HasValue && previousToken.Value != catchFilterClause.OpenParenToken)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                return CreateResult(SpecialType.System_Boolean);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInCoalesceExpression(
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
                        .Select(x => x.InferredType.IsNullable()
                            ? new TypeInferenceInfo(((INamedTypeSymbol)x.InferredType).TypeArguments[0]) // nullableExpr ?? Goo()
                            : x); // normalExpr ?? Goo() 
                }

                var rightTypes = GetTypes(coalesceExpression.Right);
                if (!rightTypes.Any())
                {
                    return CreateResult(SpecialType.System_Object, NullableAnnotation.Annotated);
                }

                return rightTypes
                    .Select(x => new TypeInferenceInfo(MakeNullable(x.InferredType, this.Compilation))); // Goo() ?? ""

                static ITypeSymbol MakeNullable(ITypeSymbol symbol, Compilation compilation)
                {
                    if (symbol.IsErrorType())
                    {
                        // We could be smart and infer this as an ErrorType?, but in the #nullable disable case we don't know if this is intended to be
                        // a struct (where the question mark is legal) or a class (where it isn't). We'll thus avoid sticking question marks in this case.
                        // https://github.com/dotnet/roslyn/issues/37852 tracks fixing this is a much fancier way.
                        return symbol;
                    }
                    else if (symbol.IsValueType)
                    {
                        return compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(symbol.WithoutNullability());
                    }
                    else if (symbol.IsReferenceType)
                    {
                        return symbol.WithNullability(NullableAnnotation.Annotated);
                    }
                    else // it's neither a value nor reference type, so is an unconstrained generic
                    {
                        return symbol;
                    }
                }
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInConditionalAccessExpression(ConditionalAccessExpressionSyntax expression)
            {
                return InferTypes(expression);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInConditionalExpression(ConditionalExpressionSyntax conditional, ExpressionSyntax expressionOpt = null, SyntaxToken? previousToken = null)
            {
                if (expressionOpt != null && conditional.Condition == expressionOpt)
                {
                    // Goo() ? a : b
                    return CreateResult(SpecialType.System_Boolean);
                }

                // a ? Goo() : b
                //
                // a ? b : Goo()
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
                                           : SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();

                return otherTypes.IsEmpty()
                           ? InferTypes(conditional)
                           : otherTypes;
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInDefaultExpression(DefaultExpressionSyntax defaultExpression)
            {
                return InferTypes(defaultExpression);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInDoStatement(DoStatementSyntax doStatement, SyntaxToken? previousToken = null)
            {
                // If we have a position, we need to be after "do { } while("
                if (previousToken.HasValue && previousToken.Value != doStatement.OpenParenToken)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                return CreateResult(SpecialType.System_Boolean);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInEqualsValueClause(EqualsValueClauseSyntax equalsValue, SyntaxToken? previousToken = null)
            {
                // If we have a position, it has to be after the =
                if (previousToken.HasValue && previousToken.Value != equalsValue.EqualsToken)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
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
                    if (SemanticModel.GetDeclaredSymbol(equalsValue.Parent, CancellationToken) is IParameterSymbol parameter)
                    {
                        return CreateResult(parameter.GetTypeWithAnnotatedNullability());
                    }
                }

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInPropertyDeclaration(PropertyDeclarationSyntax propertyDeclaration)
            {
                Debug.Assert(propertyDeclaration?.Type != null, "Property type should never be null");

                var typeInfo = SemanticModel.GetTypeInfo(propertyDeclaration.Type);
                return CreateResult(typeInfo.GetTypeWithAnnotatedNullability());
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInExpressionStatement(ExpressionStatementSyntax expressionStatement, SyntaxToken? previousToken = null)
            {
                // If we're position based, then that means we're after the semicolon.  In this case
                // we don't have any sort of type to infer.
                if (previousToken.HasValue)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                return CreateResult(SpecialType.System_Void);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInForEachStatement(ForEachStatementSyntax forEachStatementSyntax, ExpressionSyntax expressionOpt = null, SyntaxToken? previousToken = null)
            {
                // If we have a position, then we have to be after "foreach(... in"
                if (previousToken.HasValue && previousToken.Value != forEachStatementSyntax.InKeyword)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                if (expressionOpt != null && expressionOpt != forEachStatementSyntax.Expression)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                // foreach (int v = Goo())
                var variableTypes = GetTypes(forEachStatementSyntax.Type);
                if (!variableTypes.Any())
                {
                    return CreateResult(
                        this.Compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T)
                            .Construct(Compilation.GetSpecialType(SpecialType.System_Object)));
                }

                var type = this.Compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T);

                return variableTypes.Select(v => new TypeInferenceInfo(type.ConstructWithNullability(v.InferredType)));
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInForStatement(ForStatementSyntax forStatement, ExpressionSyntax expressionOpt = null, SyntaxToken? previousToken = null)
            {
                // If we have a position, it has to be after "for(...;"
                if (previousToken.HasValue && previousToken.Value != forStatement.FirstSemicolonToken)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                if (expressionOpt != null && forStatement.Condition != expressionOpt)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                return CreateResult(SpecialType.System_Boolean);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInIfStatement(IfStatementSyntax ifStatement, SyntaxToken? previousToken = null)
            {
                // If we have a position, we have to be after the "if("
                if (previousToken.HasValue && previousToken.Value != ifStatement.OpenParenToken)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                return CreateResult(SpecialType.System_Boolean);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInImplicitArrayCreation(ImplicitArrayCreationExpressionSyntax implicitArray, SyntaxToken previousToken)
            {
                return InferTypes(implicitArray.SpanStart);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInInitializerExpression(
                InitializerExpressionSyntax initializerExpression,
                ExpressionSyntax expressionOpt = null,
                SyntaxToken? previousToken = null)
            {
                if (initializerExpression.IsKind(SyntaxKind.ComplexElementInitializerExpression))
                {
                    // new Dictionary<K,V> { { x, ... } }
                    // new C { Prop = { { x, ... } } }
                    var parameterIndex = previousToken.HasValue
                            ? initializerExpression.Expressions.GetSeparators().ToList().IndexOf(previousToken.Value) + 1
                            : initializerExpression.Expressions.IndexOf(expressionOpt);

                    var addMethodSymbols = SemanticModel.GetCollectionInitializerSymbolInfo(initializerExpression).GetAllSymbols();
                    var addMethodParameterTypes = addMethodSymbols
                        .Cast<IMethodSymbol>()
                        .Where(a => a.Parameters.Length == initializerExpression.Expressions.Count)
                        .Select(a => new TypeInferenceInfo(a.Parameters.ElementAtOrDefault(parameterIndex)?.GetTypeWithAnnotatedNullability()))
                        .Where(t => t.InferredType != null);

                    if (addMethodParameterTypes.Any())
                    {
                        return addMethodParameterTypes;
                    }
                }
                else if (initializerExpression.IsKind(SyntaxKind.CollectionInitializerExpression))
                {
                    if (expressionOpt != null)
                    {
                        // new List<T> { x, ... }
                        // new C { Prop = { x, ... } }
                        var addMethodSymbols = SemanticModel.GetCollectionInitializerSymbolInfo(expressionOpt).GetAllSymbols();
                        var addMethodParameterTypes = addMethodSymbols
                            .Cast<IMethodSymbol>()
                            .Where(a => a.Parameters.Length == 1)
                            .Select(a => new TypeInferenceInfo(a.Parameters[0].GetTypeWithAnnotatedNullability()));

                        if (addMethodParameterTypes.Any())
                        {
                            return addMethodParameterTypes;
                        }
                    }
                    else
                    {
                        // new List<T> { x,
                        // new C { Prop = { x,

                        foreach (var sibling in initializerExpression.Expressions.Where(e => e.Kind() != SyntaxKind.ComplexElementInitializerExpression))
                        {
                            var types = GetTypes(sibling);
                            if (types.Any())
                            {
                                return types;
                            }
                        }
                    }
                }

                if (initializerExpression.IsParentKind(SyntaxKind.ImplicitArrayCreationExpression))
                {
                    // new[] { 1, x }

                    // First, try to infer the type that the array should be.  If we can infer an
                    // appropriate array type, then use the element type of the array.  Otherwise,
                    // look at the siblings of this expression and use their type instead.

                    var arrayTypes = this.InferTypes((ExpressionSyntax)initializerExpression.Parent);
                    var elementTypes = arrayTypes.OfType<IArrayTypeSymbol>().Select(a => new TypeInferenceInfo(a.GetElementTypeWithAnnotatedNullability())).Where(IsUsableTypeFunc);

                    if (elementTypes.Any())
                    {
                        return elementTypes;
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
                    // = { Goo() }
                    var equalsValueClause = (EqualsValueClauseSyntax)initializerExpression.Parent;
                    var types = InferTypeInEqualsValueClause(equalsValueClause).Select(t => t.InferredType);

                    if (types.Any(t => t is IArrayTypeSymbol))
                    {
                        return types.OfType<IArrayTypeSymbol>().Select(t => new TypeInferenceInfo(t.GetElementTypeWithAnnotatedNullability()));
                    }
                }
                else if (initializerExpression.IsParentKind(SyntaxKind.ArrayCreationExpression))
                {
                    // new int[] { Goo() } 
                    var arrayCreation = (ArrayCreationExpressionSyntax)initializerExpression.Parent;
                    var types = GetTypes(arrayCreation).Select(t => t.InferredType);

                    if (types.Any(t => t is IArrayTypeSymbol))
                    {
                        return types.OfType<IArrayTypeSymbol>().Select(t => new TypeInferenceInfo(t.GetElementTypeWithAnnotatedNullability()));
                    }
                }
                else if (initializerExpression.IsParentKind(SyntaxKind.ObjectCreationExpression))
                {
                    // new List<T> { Goo() } 

                    var objectCreation = (ObjectCreationExpressionSyntax)initializerExpression.Parent;

                    var types = GetTypes(objectCreation).Select(t => t.InferredType);
                    if (types.Any(t => t is INamedTypeSymbol))
                    {
                        return types.OfType<INamedTypeSymbol>().SelectMany(t =>
                            GetCollectionElementType(t));
                    }
                }
                else if (initializerExpression.IsParentKind(SyntaxKind.SimpleAssignmentExpression))
                {
                    // new Goo { a = { Goo() } }

                    if (expressionOpt != null)
                    {
                        var addMethodSymbols = SemanticModel.GetCollectionInitializerSymbolInfo(expressionOpt).GetAllSymbols();
                        var addMethodParameterTypes = addMethodSymbols.Select(m => ((IMethodSymbol)m).Parameters[0]).Select(p => new TypeInferenceInfo(p.GetTypeWithAnnotatedNullability()));
                        if (addMethodParameterTypes.Any())
                        {
                            return addMethodParameterTypes;
                        }
                    }

                    var assignExpression = (AssignmentExpressionSyntax)initializerExpression.Parent;
                    var types = GetTypes(assignExpression.Left).Select(t => t.InferredType);

                    if (types.Any(t => t is INamedTypeSymbol))
                    {
                        // new Goo { a = { Goo() } }
                        var parameterIndex = previousToken.HasValue
                                ? initializerExpression.Expressions.GetSeparators().ToList().IndexOf(previousToken.Value) + 1
                                : initializerExpression.Expressions.IndexOf(expressionOpt);

                        return types.OfType<INamedTypeSymbol>().SelectMany(t =>
                            GetCollectionElementType(t));
                    }
                }

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInRecursivePattern(RecursivePatternSyntax recursivePattern)
            {
                var type = this.SemanticModel.GetTypeInfo(recursivePattern).ConvertedType;
                return CreateResult(type);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInConstantPattern(
                ConstantPatternSyntax constantPattern)
            {
                return InferTypes(constantPattern);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInPropertyPatternClause(
                PropertyPatternClauseSyntax propertySubpattern,
                SyntaxNode child)
            {
                return InferTypes(propertySubpattern);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInSubpattern(
                SubpatternSyntax subpattern,
                SyntaxNode child)
            {
                // we have  { X: ... }.  The type of ... is whatever the type of 'X' is in its
                // parent type.  So look up the parent type first, then find the X member in it
                // and use that type.
                if (child == subpattern.Pattern &&
                    subpattern.NameColon != null)
                {
                    var result = ArrayBuilder<TypeInferenceInfo>.GetInstance();

                    foreach (var symbol in this.SemanticModel.GetSymbolInfo(subpattern.NameColon.Name).GetAllSymbols())
                    {
                        switch (symbol)
                        {
                            case IFieldSymbol field:
                                result.Add(new TypeInferenceInfo(field.GetTypeWithAnnotatedNullability()));
                                break;
                            case IPropertySymbol property:
                                result.Add(new TypeInferenceInfo(property.GetTypeWithAnnotatedNullability()));
                                break;
                        }
                    }

                    return result.ToImmutableAndFree();
                }

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInIsPatternExpression(
                IsPatternExpressionSyntax isPatternExpression,
                SyntaxNode child)
            {
                if (child == isPatternExpression.Expression)
                {
                    return GetPatternTypes(isPatternExpression.Pattern);
                }
                else if (child == isPatternExpression.Pattern)
                {
                    return GetTypes(isPatternExpression.Expression);
                }

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            private IEnumerable<TypeInferenceInfo> GetPatternTypes(PatternSyntax pattern)
            {
                switch (pattern)
                {
                    case ConstantPatternSyntax constantPattern: return GetTypes(constantPattern.Expression);
                    case DeclarationPatternSyntax declarationPattern: return GetTypes(declarationPattern.Type);
                    case RecursivePatternSyntax recursivePattern: return GetTypesForRecursivePattern(recursivePattern);
                    default: return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }
            }

            private IEnumerable<TypeInferenceInfo> GetTypesForRecursivePattern(RecursivePatternSyntax recursivePattern)
            {
                // if it's of the for "X (...)" then just infer 'X' as the type.
                if (recursivePattern.Type != null)
                {
                    var typeInfo = SemanticModel.GetTypeInfo(recursivePattern);
                    return CreateResult(typeInfo.GetConvertedTypeWithAnnotatedNullability());
                }

                // If it's of the form (...) then infer that the type should be a 
                // tuple, whose elements are inferred from the individual patterns
                // in the deconstruction.
                var positionalPart = recursivePattern.PositionalPatternClause;
                if (positionalPart != null)
                {
                    var subPatternCount = positionalPart.Subpatterns.Count;
                    if (subPatternCount >= 2)
                    {
                        // infer a tuple type for this deconstruction.
                        var elementTypesBuilder = ArrayBuilder<ITypeSymbol>.GetInstance(subPatternCount);
                        var elementNamesBuilder = ArrayBuilder<string>.GetInstance(subPatternCount);

                        foreach (var subPattern in positionalPart.Subpatterns)
                        {
                            elementNamesBuilder.Add(subPattern.NameColon?.Name.Identifier.ValueText);

                            var patternType = GetPatternTypes(subPattern.Pattern).FirstOrDefault();
                            if (patternType.InferredType == null)
                            {
                                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                            }

                            elementTypesBuilder.Add(patternType.InferredType.WithoutNullability());
                        }

                        var type = Compilation.CreateTupleTypeSymbol(
                            elementTypesBuilder.ToImmutableAndFree(), elementNamesBuilder.ToImmutableAndFree());
                        return CreateResult(type);
                    }
                }

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInLockStatement(LockStatementSyntax lockStatement, SyntaxToken? previousToken = null)
            {
                // If we're position based, then we have to be after the "lock("
                if (previousToken.HasValue && previousToken.Value != lockStatement.OpenParenToken)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                return CreateResult(SpecialType.System_Object);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInLambdaExpression(LambdaExpressionSyntax lambdaExpression, SyntaxToken? previousToken = null)
            {
                // If we have a position, it has to be after the lambda arrow.
                if (previousToken.HasValue && previousToken.Value != lambdaExpression.ArrowToken)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                return InferTypeInAnonymousFunctionExpression(lambdaExpression);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInAnonymousFunctionExpression(AnonymousFunctionExpressionSyntax anonymousFunction)
            {
                // Func<int,string> = i => Goo();
                // Func<int,string> = delegate (int i) { return Goo(); };
                var types = InferTypes(anonymousFunction);
                var type = types.FirstOrDefault().InferredType.GetDelegateType(this.Compilation);

                if (type != null)
                {
                    var invoke = type.DelegateInvokeMethod;
                    if (invoke != null)
                    {
                        var isAsync = anonymousFunction.AsyncKeyword.Kind() != SyntaxKind.None;
                        return SpecializedCollections.SingletonEnumerable(
                            new TypeInferenceInfo(UnwrapTaskLike(invoke.GetReturnTypeWithAnnotatedNullability(), isAsync)));
                    }
                }

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInMemberDeclarator(AnonymousObjectMemberDeclaratorSyntax memberDeclarator, SyntaxToken? previousTokenOpt = null)
            {
                if (memberDeclarator.NameEquals != null && memberDeclarator.Parent is AnonymousObjectCreationExpressionSyntax)
                {
                    // If we're position based, then we have to be after the = 
                    if (previousTokenOpt.HasValue && previousTokenOpt.Value != memberDeclarator.NameEquals.EqualsToken)
                    {
                        return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                    }

                    var types = InferTypes((AnonymousObjectCreationExpressionSyntax)memberDeclarator.Parent);

                    return types.Where(t => t.InferredType.IsAnonymousType())
                        .SelectMany(t => t.InferredType.GetValidAnonymousTypeProperties()
                            .Where(p => p.Name == memberDeclarator.NameEquals.Name.Identifier.ValueText)
                            .Select(p => new TypeInferenceInfo(p.GetTypeWithAnnotatedNullability())));
                }

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInNameColon(NameColonSyntax nameColon, SyntaxToken previousToken)
            {
                if (previousToken != nameColon.ColonToken)
                {
                    // Must follow the colon token.
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                if (nameColon.Parent is ArgumentSyntax argumentSyntax)
                {
                    return InferTypeInArgument(argumentSyntax);
                }

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInMemberAccessExpression(
                MemberAccessExpressionSyntax memberAccessExpression,
                ExpressionSyntax expressionOpt = null,
                SyntaxToken? previousToken = null)
            {
                // We need to be on the right of the dot to infer an appropriate type for
                // the member access expression.  i.e. if we have "Goo.Bar" then we can 
                // def infer what the type of 'Bar' should be (it's whatever type we infer
                // for 'Goo.Bar' itself.  However, if we're on 'Goo' then we can't figure
                // out anything about its type.
                if (previousToken != null)
                {
                    if (previousToken.Value != memberAccessExpression.OperatorToken)
                    {
                        return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                    }

                    // We're right after the dot in "Goo.Bar".  The type for "Bar" should be
                    // whatever type we'd infer for "Goo.Bar" itself.
                    return InferTypes(memberAccessExpression);
                }
                else
                {
                    Debug.Assert(expressionOpt != null);
                    if (expressionOpt == memberAccessExpression.Expression)
                    {
                        return InferTypeForExpressionOfMemberAccessExpression(memberAccessExpression);
                    }

                    // We're right after the dot in "Goo.Bar".  The type for "Bar" should be
                    // whatever type we'd infer for "Goo.Bar" itself.
                    return InferTypes(memberAccessExpression);
                }
            }

            private IEnumerable<TypeInferenceInfo> InferTypeForExpressionOfMemberAccessExpression(
                MemberAccessExpressionSyntax memberAccessExpression)
            {
                // If we're on the left side of a dot, it's possible in a few cases
                // to figure out what type we should be.  Specifically, if we have
                //
                //      await goo.ConfigureAwait()
                //
                // then we can figure out what 'goo' should be based on teh await
                // context.
                var name = memberAccessExpression.Name.Identifier.Value;
                if (name.Equals(nameof(Task<int>.ConfigureAwait)) &&
                    memberAccessExpression.IsParentKind(SyntaxKind.InvocationExpression) &&
                    memberAccessExpression.Parent.IsParentKind(SyntaxKind.AwaitExpression))
                {
                    return InferTypes((ExpressionSyntax)memberAccessExpression.Parent);
                }
                else if (name.Equals(nameof(Task<int>.ContinueWith)))
                {
                    // goo.ContinueWith(...)
                    // We want to infer Task<T>.  For now, we'll just do Task<object>,
                    // in the future it would be nice to figure out the actual result
                    // type based on the argument to ContinueWith.
                    var taskOfT = this.Compilation.TaskOfTType();
                    if (taskOfT != null)
                    {
                        return CreateResult(taskOfT.Construct(this.Compilation.ObjectType));
                    }
                }
                else if (name.Equals(nameof(Enumerable.Select)) ||
                         name.Equals(nameof(Enumerable.Where)))
                {
                    var ienumerableType = this.Compilation.IEnumerableOfTType();

                    // goo.Select
                    // We want to infer IEnumerable<T>.  We can try to figure out what 
                    // T if we get a delegate as the first argument to Select/Where.
                    if (ienumerableType != null && memberAccessExpression.IsParentKind(SyntaxKind.InvocationExpression))
                    {
                        var invocation = (InvocationExpressionSyntax)memberAccessExpression.Parent;
                        if (invocation.ArgumentList.Arguments.Count > 0)
                        {
                            var argumentExpression = invocation.ArgumentList.Arguments[0].Expression;

                            if (argumentExpression != null)
                            {
                                var argumentTypes = GetTypes(argumentExpression);
                                var delegateType = argumentTypes.FirstOrDefault().InferredType.GetDelegateType(this.Compilation);
                                var typeArg = delegateType?.TypeArguments.Length > 0
                                    ? delegateType.TypeArguments[0]
                                    : this.Compilation.ObjectType;

                                if (IsUnusableType(typeArg) && argumentExpression is LambdaExpressionSyntax)
                                {
                                    typeArg = InferTypeForFirstParameterOfLambda((LambdaExpressionSyntax)argumentExpression) ??
                                        this.Compilation.ObjectType;
                                }

                                return CreateResult(ienumerableType.ConstructWithNullability(typeArg));
                            }
                        }
                    }
                }

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            private ITypeSymbol InferTypeForFirstParameterOfLambda(
                LambdaExpressionSyntax lambdaExpression)
            {
                if (lambdaExpression is ParenthesizedLambdaExpressionSyntax parenLambda)
                {
                    return InferTypeForFirstParameterOfParenthesizedLambda(parenLambda);
                }
                else if (lambdaExpression is SimpleLambdaExpressionSyntax simpleLambda)
                {
                    return InferTypeForFirstParameterOfSimpleLambda(simpleLambda);
                }

                return null;
            }

            private ITypeSymbol InferTypeForFirstParameterOfParenthesizedLambda(
                ParenthesizedLambdaExpressionSyntax lambdaExpression)
            {
                return lambdaExpression.ParameterList.Parameters.Count == 0
                    ? null
                    : InferTypeForFirstParameterOfLambda(
                        lambdaExpression, lambdaExpression.ParameterList.Parameters[0]);
            }

            private ITypeSymbol InferTypeForFirstParameterOfSimpleLambda(
                SimpleLambdaExpressionSyntax lambdaExpression)
            {
                return InferTypeForFirstParameterOfLambda(
                    lambdaExpression, lambdaExpression.Parameter);
            }

            private ITypeSymbol InferTypeForFirstParameterOfLambda(
                LambdaExpressionSyntax lambdaExpression, ParameterSyntax parameter)
            {
                return InferTypeForFirstParameterOfLambda(
                    parameter.Identifier.ValueText, lambdaExpression.Body);
            }

            private ITypeSymbol InferTypeForFirstParameterOfLambda(
                string parameterName,
                SyntaxNode node)
            {
                if (node.IsKind(SyntaxKind.IdentifierName))
                {
                    var identifierName = (IdentifierNameSyntax)node;
                    if (identifierName.Identifier.ValueText.Equals(parameterName) &&
                        SemanticModel.GetSymbolInfo(identifierName.Identifier).Symbol?.Kind == SymbolKind.Parameter)
                    {
                        return InferTypes(identifierName).FirstOrDefault().InferredType;
                    }
                }
                else
                {
                    foreach (var child in node.ChildNodesAndTokens())
                    {
                        if (child.IsNode)
                        {
                            var type = InferTypeForFirstParameterOfLambda(parameterName, child.AsNode());
                            if (type != null)
                            {
                                return type;
                            }
                        }
                    }
                }

                return null;
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInNameColon(NameColonSyntax nameColon)
            {
                if (nameColon.Parent is SubpatternSyntax subpattern)
                {
                    return GetPatternTypes(subpattern.Pattern);
                }

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInNameEquals(NameEqualsSyntax nameEquals, SyntaxToken? previousToken = null)
            {
                if (previousToken == nameEquals.EqualsToken)
                {
                    // we're on the right of the equals.  Try to bind the left name to see if it
                    // gives us anything useful.
                    return GetTypes(nameEquals.Name);
                }

                if (nameEquals.Parent is AttributeArgumentSyntax attributeArgumentSyntax)
                {
                    var argumentExpression = attributeArgumentSyntax.Expression;
                    return this.GetTypes(argumentExpression);
                }

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInPostfixUnaryExpression(PostfixUnaryExpressionSyntax postfixUnaryExpressionSyntax, SyntaxToken? previousToken = null)
            {
                // If we're after a postfix ++ or -- then we can't infer anything.
                if (previousToken.HasValue)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                switch (postfixUnaryExpressionSyntax.Kind())
                {
                    case SyntaxKind.PostDecrementExpression:
                    case SyntaxKind.PostIncrementExpression:
                        return CreateResult(this.Compilation.GetSpecialType(SpecialType.System_Int32));
                }

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInPrefixUnaryExpression(PrefixUnaryExpressionSyntax prefixUnaryExpression, SyntaxToken? previousToken = null)
            {
                // If we have a position, then we must be after the prefix token.
                Contract.ThrowIfTrue(previousToken.HasValue && previousToken.Value != prefixUnaryExpression.OperatorToken);

                switch (prefixUnaryExpression.Kind())
                {
                    case SyntaxKind.PreDecrementExpression:
                    case SyntaxKind.PreIncrementExpression:
                    case SyntaxKind.UnaryPlusExpression:
                    case SyntaxKind.UnaryMinusExpression:
                        // ++, --, +Goo(), -Goo();
                        return CreateResult(this.Compilation.GetSpecialType(SpecialType.System_Int32));

                    case SyntaxKind.BitwiseNotExpression:
                        // ~Goo()
                        var types = InferTypes(prefixUnaryExpression);
                        if (!types.Any())
                        {
                            return CreateResult(this.Compilation.GetSpecialType(SpecialType.System_Int32));
                        }
                        else
                        {
                            return types;
                        }

                    case SyntaxKind.LogicalNotExpression:
                        // !Goo()
                        return CreateResult(SpecialType.System_Boolean);
                }

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInAwaitExpression(AwaitExpressionSyntax awaitExpression, SyntaxToken? previousToken = null)
            {
                // If we have a position, then we must be after the prefix token.
                Contract.ThrowIfTrue(previousToken.HasValue && previousToken.Value != awaitExpression.AwaitKeyword);

                // await <expression>
                var types = InferTypes(awaitExpression);

                var task = this.Compilation.TaskType();
                var taskOfT = this.Compilation.TaskOfTType();

                if (task == null || taskOfT == null)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                if (!types.Any())
                {
                    return CreateResult(task);
                }

                return types.Select(t => t.InferredType.SpecialType == SpecialType.System_Void ? new TypeInferenceInfo(task) : new TypeInferenceInfo(taskOfT.ConstructWithNullability(t.InferredType)));
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInYieldStatement(YieldStatementSyntax yieldStatement, SyntaxToken? previousToken = null)
            {
                // If we are position based, then we have to be after the return keyword
                if (previousToken.HasValue && (previousToken.Value != yieldStatement.ReturnOrBreakKeyword || yieldStatement.ReturnOrBreakKeyword.IsKind(SyntaxKind.BreakKeyword)))
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                var declaration = yieldStatement.FirstAncestorOrSelf<SyntaxNode>(n => n.IsReturnableConstruct());
                var memberSymbol = GetDeclaredMemberSymbolFromOriginalSemanticModel(declaration);

                var memberType = memberSymbol.GetMemberType();

                // We don't care what the type is, as long as it has 1 type argument. This will work for IEnumerable, IEnumerator,
                // IAsyncEnumerable, IAsyncEnumerator and it's also good for error recovery in case there is a missing using.
                return memberType is INamedTypeSymbol namedType && namedType.TypeArguments.Length == 1
                    ? SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(namedType.TypeArguments[0].WithNullability(namedType.TypeArgumentNullableAnnotations[0])))
                    : SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInRefExpression(RefExpressionSyntax refExpression)
                => InferTypes(refExpression);

            private ITypeSymbol UnwrapTaskLike(ITypeSymbol type, bool isAsync)
            {
                if (isAsync)
                {
                    if (type.OriginalDefinition.Equals(this.Compilation.TaskOfTType()))
                    {
                        var namedTypeSymbol = (INamedTypeSymbol)type;
                        return namedTypeSymbol.TypeArguments[0].WithNullability(namedTypeSymbol.TypeArgumentNullableAnnotations[0]);
                    }

                    if (type.OriginalDefinition.Equals(this.Compilation.TaskType()))
                    {
                        return this.Compilation.GetSpecialType(SpecialType.System_Void);
                    }
                }

                return type;
            }

            private IEnumerable<TypeInferenceInfo> InferTypeForReturnStatement(
                ReturnStatementSyntax returnStatement, SyntaxToken? previousToken = null)
            {
                // If we are position based, then we have to be after the return statement.
                if (previousToken.HasValue && previousToken.Value != returnStatement.ReturnKeyword)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                var ancestor = returnStatement.FirstAncestorOrSelf<SyntaxNode>(n => n.IsReturnableConstruct());

                return ancestor is AnonymousFunctionExpressionSyntax anonymousFunction
                    ? InferTypeInAnonymousFunctionExpression(anonymousFunction)
                    : InferTypeInMethodLikeDeclaration(ancestor);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInArrowExpressionClause(ArrowExpressionClauseSyntax arrowClause)
                => InferTypeInMethodLikeDeclaration(arrowClause.Parent);

            private IEnumerable<TypeInferenceInfo> InferTypeInMethodLikeDeclaration(SyntaxNode declaration)
            {
                // `declaration` can be a base-method member, property, accessor or local function

                var symbol = GetDeclaredMemberSymbolFromOriginalSemanticModel(declaration);
                var type = symbol.GetMemberType();
                var isAsync = symbol is IMethodSymbol methodSymbol && methodSymbol.IsAsync;

                return type != null
                    ? SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(UnwrapTaskLike(type, isAsync)))
                    : SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            private ISymbol GetDeclaredMemberSymbolFromOriginalSemanticModel(SyntaxNode declarationInCurrentTree)
            {
                var currentSemanticModel = SemanticModel;
                var originalSemanticModel = currentSemanticModel.GetOriginalSemanticModel();

                if (declarationInCurrentTree is MemberDeclarationSyntax &&
                    currentSemanticModel.IsSpeculativeSemanticModel)
                {
                    var tokenInOriginalTree = originalSemanticModel.SyntaxTree.GetRoot(CancellationToken).FindToken(currentSemanticModel.OriginalPositionForSpeculation);
                    var declaration = tokenInOriginalTree.GetAncestor<MemberDeclarationSyntax>();
                    return originalSemanticModel.GetDeclaredSymbol(declaration, CancellationToken);
                }

                return declarationInCurrentTree != null
                    ? currentSemanticModel.GetDeclaredSymbol(declarationInCurrentTree, CancellationToken)
                    : null;
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInSwitchLabel(
                SwitchLabelSyntax switchLabel, SyntaxToken? previousToken = null)
            {
                if (previousToken.HasValue)
                {
                    if (previousToken.Value != switchLabel.Keyword ||
                        switchLabel.Kind() != SyntaxKind.CaseSwitchLabel)
                    {
                        return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                    }
                }

                var switchStatement = (SwitchStatementSyntax)switchLabel.Parent.Parent;
                return GetTypes(switchStatement.Expression);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInSwitchStatement(
                SwitchStatementSyntax switchStatement, SyntaxToken? previousToken = null)
            {
                // If we have a position, then it has to be after "switch("
                if (previousToken.HasValue && previousToken.Value != switchStatement.OpenParenToken)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                // Use the first case label to determine the return type.
                if (switchStatement.Sections.SelectMany(ss => ss.Labels)
                                                  .FirstOrDefault(label => label.Kind() == SyntaxKind.CaseSwitchLabel) is CaseSwitchLabelSyntax firstCase)
                {
                    var result = GetTypes(firstCase.Value);
                    if (result.Any())
                    {
                        return result;
                    }
                }

                return CreateResult(this.Compilation.GetSpecialType(SpecialType.System_Int32));
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInThrowExpression(ThrowExpressionSyntax throwExpression, SyntaxToken? previousToken = null)
            {
                // If we have a position, it has to be after the 'throw' keyword.
                if (previousToken.HasValue && previousToken.Value != throwExpression.ThrowKeyword)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                return CreateResult(this.Compilation.ExceptionType());
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInThrowStatement(ThrowStatementSyntax throwStatement, SyntaxToken? previousToken = null)
            {
                // If we have a position, it has to be after the 'throw' keyword.
                if (previousToken.HasValue && previousToken.Value != throwStatement.ThrowKeyword)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                return CreateResult(this.Compilation.ExceptionType());
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInUsingStatement(UsingStatementSyntax usingStatement, SyntaxToken? previousToken = null)
            {
                // If we have a position, it has to be after "using("
                if (previousToken.HasValue && previousToken.Value != usingStatement.OpenParenToken)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                return CreateResult(SpecialType.System_IDisposable);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInVariableDeclarator(VariableDeclaratorSyntax variableDeclarator)
            {
                var variableType = variableDeclarator.GetVariableType();
                if (variableType == null)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                var symbol = SemanticModel.GetDeclaredSymbol(variableDeclarator);
                if (symbol == null)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                var type = symbol.GetSymbolType();
                var types = CreateResult(type).Where(IsUsableTypeFunc);

                if (variableType.IsVar)
                {
                    if (variableDeclarator.Parent is VariableDeclarationSyntax variableDeclaration)
                    {
                        if (variableDeclaration.IsParentKind(SyntaxKind.UsingStatement))
                        {
                            // using (var v = Goo())
                            return CreateResult(SpecialType.System_IDisposable);
                        }

                        if (variableDeclaration.IsParentKind(SyntaxKind.ForStatement))
                        {
                            // for (var v = Goo(); ..
                            return CreateResult(this.Compilation.GetSpecialType(SpecialType.System_Int32));
                        }

                        // Return the types here if they actually bound to a type called 'var'.
                        return types.Where(t => t.InferredType.Name == "var");
                    }
                }

                return types;
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInVariableComponentAssignment(ExpressionSyntax left)
            {
                if (left.IsKind(SyntaxKind.DeclarationExpression))
                {
                    return GetTypes(((DeclarationExpressionSyntax)left).Type);
                }
                else if (left.IsKind(SyntaxKind.TupleExpression))
                {
                    // We have something of the form:
                    //   (int a, int b) = ...
                    //
                    // This is a deconstruction, and a decent deconstructable type we can infer here
                    // is ValueTuple<int,int>.
                    var tupleType = GetTupleType((TupleExpressionSyntax)left);

                    if (tupleType != null)
                    {
                        return CreateResult(tupleType);
                    }
                }

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            private ITypeSymbol GetTupleType(
                TupleExpressionSyntax tuple)
            {
                if (!TryGetTupleTypesAndNames(tuple.Arguments, out var elementTypes, out var elementNames))
                {
                    return null;
                }

                return Compilation.CreateTupleTypeSymbol(elementTypes.SelectAsArray(t => t.WithoutNullability()), elementNames, elementNullableAnnotations: elementTypes.SelectAsArray(t => t.GetNullability()));
            }

            private bool TryGetTupleTypesAndNames(
                SeparatedSyntaxList<ArgumentSyntax> arguments,
                out ImmutableArray<ITypeSymbol> elementTypes,
                out ImmutableArray<string> elementNames)
            {
                elementTypes = default;
                elementNames = default;

                var elementTypesBuilder = ArrayBuilder<ITypeSymbol>.GetInstance();
                var elementNamesBuilder = ArrayBuilder<string>.GetInstance();
                try
                {
                    foreach (var arg in arguments)
                    {
                        var expr = arg.Expression;
                        if (expr.IsKind(SyntaxKind.DeclarationExpression))
                        {
                            AddTypeAndName((DeclarationExpressionSyntax)expr, elementTypesBuilder, elementNamesBuilder);
                        }
                        else if (expr.IsKind(SyntaxKind.TupleExpression))
                        {
                            AddTypeAndName((TupleExpressionSyntax)expr, elementTypesBuilder, elementNamesBuilder);
                        }
                        else if (expr is IdentifierNameSyntax name)
                        {
                            elementNamesBuilder.Add(name.Identifier.ValueText == "" ? null :
                                name.Identifier.ValueText);
                            elementTypesBuilder.Add(GetTypes(expr).FirstOrDefault().InferredType ?? this.Compilation.ObjectType);
                        }
                        else
                        {
                            return false;
                        }
                    }

                    if (elementTypesBuilder.Contains(null) || elementTypesBuilder.Count != arguments.Count)
                    {
                        return false;
                    }

                    elementTypes = elementTypesBuilder.ToImmutable();
                    elementNames = elementNamesBuilder.ToImmutable();
                    return true;
                }
                finally
                {
                    elementTypesBuilder.Free();
                    elementNamesBuilder.Free();
                }
            }

            private void AddTypeAndName(
                DeclarationExpressionSyntax declaration,
                ArrayBuilder<ITypeSymbol> elementTypesBuilder,
                ArrayBuilder<string> elementNamesBuilder)
            {
                elementTypesBuilder.Add(GetTypes(declaration.Type).FirstOrDefault().InferredType);

                var designation = declaration.Designation;
                if (designation.IsKind(SyntaxKind.SingleVariableDesignation))
                {
                    var singleVariable = (SingleVariableDesignationSyntax)designation;
                    var name = singleVariable.Identifier.ValueText;

                    if (name != string.Empty)
                    {
                        elementNamesBuilder.Add(name);
                        return;
                    }
                }

                elementNamesBuilder.Add(null);
            }

            private void AddTypeAndName(
                TupleExpressionSyntax tuple,
                ArrayBuilder<ITypeSymbol> elementTypesBuilder,
                ArrayBuilder<string> elementNamesBuilder)
            {
                var tupleType = GetTupleType(tuple);
                elementTypesBuilder.Add(tupleType);
                elementNamesBuilder.Add(null);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInWhenClause(WhenClauseSyntax whenClause, SyntaxToken? previousToken = null)
            {
                // If we have a position, we have to be after the "when"
                if (previousToken.HasValue && previousToken.Value != whenClause.WhenKeyword)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(Compilation.GetSpecialType(SpecialType.System_Boolean)));
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInWhileStatement(WhileStatementSyntax whileStatement, SyntaxToken? previousToken = null)
            {
                // If we're position based, then we have to be after the "while("
                if (previousToken.HasValue && previousToken.Value != whileStatement.OpenParenToken)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                return CreateResult(SpecialType.System_Boolean);
            }

            private IEnumerable<TypeInferenceInfo> GetCollectionElementType(INamedTypeSymbol type)
            {
                if (type != null)
                {
                    var parameters = type.TypeArguments;

                    var elementType = parameters.ElementAtOrDefault(0);
                    if (elementType != null)
                    {
                        return SpecializedCollections.SingletonCollection(new TypeInferenceInfo(elementType.WithNullability(type.TypeArgumentNullableAnnotations[0])));
                    }
                }

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }
        }
    }
}
