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
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.LanguageServices;

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

            protected override IEnumerable<TypeInferenceInfo> GetTypes_DoNotCallDirectly(ExpressionSyntax expression, bool objectAsDefault)
            {
                var types = GetTypesSimple(expression).Where(IsUsableTypeFunc);
                if (types.Any())
                {
                    return types;
                }

                return GetTypesComplex(expression).Where(IsUsableTypeFunc);
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

            private IEnumerable<TypeInferenceInfo> GetTypesComplex(ExpressionSyntax expression)
            {
                SyntaxToken operatorToken;
                ExpressionSyntax left, right;
                if (DecomposeBinaryOrAssignmentExpression(expression, out operatorToken, out left, out right))
                {
                    var types = InferTypeInBinaryOrAssignmentExpression(expression, operatorToken, left, right, left).Where(IsUsableTypeFunc);
                    if (types.IsEmpty())
                    {
                        types = InferTypeInBinaryOrAssignmentExpression(expression, operatorToken, left, right, right).Where(IsUsableTypeFunc);
                    }

                    return types;
                }

                // TODO(cyrusn): More cases if necessary.
                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            private IEnumerable<TypeInferenceInfo> GetTypesSimple(ExpressionSyntax expression)
            {
                if (expression != null)
                {
                    var typeInfo = SemanticModel.GetTypeInfo(expression, CancellationToken);
                    var symbolInfo = SemanticModel.GetSymbolInfo(expression, CancellationToken);

                    if (symbolInfo.CandidateReason != CandidateReason.WrongArity)
                    {
                        var typeInferenceInfo = new TypeInferenceInfo(typeInfo.Type);

                        // If it bound to a method, try to get the Action/Func form of that method.
                        if (typeInferenceInfo.InferredType == null &&
                            symbolInfo.GetAllSymbols().Count() == 1 &&
                            symbolInfo.GetAllSymbols().First().Kind == SymbolKind.Method)
                        {
                            var method = symbolInfo.GetAllSymbols().First();
                            typeInferenceInfo = new TypeInferenceInfo(method.ConvertToType(this.Compilation));
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
                ExpressionSyntax expression)
            {
                expression = expression.WalkUpParentheses();
                var parent = expression.Parent;

                return parent.TypeSwitch(
                    (AnonymousObjectMemberDeclaratorSyntax memberDeclarator) => InferTypeInMemberDeclarator(memberDeclarator),
                    (ArgumentSyntax argument) => InferTypeInArgument(argument),
                    (ArrayCreationExpressionSyntax arrayCreationExpression) => InferTypeInArrayCreationExpression(arrayCreationExpression),
                    (ArrayRankSpecifierSyntax arrayRankSpecifier) => InferTypeInArrayRankSpecifier(arrayRankSpecifier),
                    (ArrayTypeSyntax arrayType) => InferTypeInArrayType(arrayType),
                    (ArrowExpressionClauseSyntax arrowClause) => InferTypeInArrowExpressionClause(arrowClause),
                    (AssignmentExpressionSyntax assignmentExpression) => InferTypeInBinaryOrAssignmentExpression(assignmentExpression, assignmentExpression.OperatorToken, assignmentExpression.Left, assignmentExpression.Right, expression),
                    (AttributeArgumentSyntax attribute) => InferTypeInAttributeArgument(attribute),
                    (AttributeSyntax attribute) => InferTypeInAttribute(attribute),
                    (AwaitExpressionSyntax awaitExpression) => InferTypeInAwaitExpression(awaitExpression),
                    (BinaryExpressionSyntax binaryExpression) => InferTypeInBinaryOrAssignmentExpression(binaryExpression, binaryExpression.OperatorToken, binaryExpression.Left, binaryExpression.Right, expression),
                    (CastExpressionSyntax castExpression) => InferTypeInCastExpression(castExpression, expression),
                    (CatchDeclarationSyntax catchDeclaration) => InferTypeInCatchDeclaration(catchDeclaration),
                    (CatchFilterClauseSyntax catchFilterClause) => InferTypeInCatchFilterClause(catchFilterClause),
                    (CheckedExpressionSyntax checkedExpression) => InferTypes(checkedExpression),
                    (ConditionalAccessExpressionSyntax conditionalAccessExpression) => InferTypeInConditionalAccessExpression(conditionalAccessExpression),
                    (ConditionalExpressionSyntax conditionalExpression) => InferTypeInConditionalExpression(conditionalExpression, expression),
                    (DoStatementSyntax doStatement) => InferTypeInDoStatement(doStatement),
                    (EqualsValueClauseSyntax equalsValue) => InferTypeInEqualsValueClause(equalsValue),
                    (ExpressionStatementSyntax expressionStatement) => InferTypeInExpressionStatement(expressionStatement),
                    (ForEachStatementSyntax forEachStatement) => InferTypeInForEachStatement(forEachStatement, expression),
                    (ForStatementSyntax forStatement) => InferTypeInForStatement(forStatement, expression),
                    (IfStatementSyntax ifStatement) => InferTypeInIfStatement(ifStatement),
                    (InitializerExpressionSyntax initializerExpression) => InferTypeInInitializerExpression(initializerExpression, expression),
                    (IsPatternExpressionSyntax isPatternExpression) => InferTypeInIsPatternExpression(isPatternExpression, expression),
                    (LockStatementSyntax lockStatement) => InferTypeInLockStatement(lockStatement),
                    (MemberAccessExpressionSyntax memberAccessExpression) => InferTypeInMemberAccessExpression(memberAccessExpression, expression),
                    (NameEqualsSyntax nameEquals) => InferTypeInNameEquals(nameEquals),
                    (ParenthesizedLambdaExpressionSyntax parenthesizedLambdaExpression) => InferTypeInParenthesizedLambdaExpression(parenthesizedLambdaExpression),
                    (PostfixUnaryExpressionSyntax postfixUnary) => InferTypeInPostfixUnaryExpression(postfixUnary),
                    (PrefixUnaryExpressionSyntax prefixUnary) => InferTypeInPrefixUnaryExpression(prefixUnary),
                    (ReturnStatementSyntax returnStatement) => InferTypeForReturnStatement(returnStatement),
                    (SimpleLambdaExpressionSyntax simpleLambdaExpression) => InferTypeInSimpleLambdaExpression(simpleLambdaExpression),
                    (SwitchLabelSyntax switchLabel) => InferTypeInSwitchLabel(switchLabel),
                    (SwitchStatementSyntax switchStatement) => InferTypeInSwitchStatement(switchStatement),
                    (ThrowStatementSyntax throwStatement) => InferTypeInThrowStatement(throwStatement),
                    (UsingStatementSyntax usingStatement) => InferTypeInUsingStatement(usingStatement),
                    (VariableComponentAssignmentSyntax variableComponentAssignment) => InferTypeInVariableComponentAssignment(variableComponentAssignment, expression),
                    (WhileStatementSyntax whileStatement) => InferTypeInWhileStatement(whileStatement),
                    (YieldStatementSyntax yieldStatement) => InferTypeInYieldStatement(yieldStatement),
                    _ => SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>());
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInArrowExpressionClause(ArrowExpressionClauseSyntax arrowClause)
            {
                if (arrowClause.IsParentKind(SyntaxKind.PropertyDeclaration))
                {
                    return InferTypeInPropertyDeclaration(arrowClause.Parent as PropertyDeclarationSyntax);
                }

                if (arrowClause.Parent is BaseMethodDeclarationSyntax)
                {
                    return InferTypeInBaseMethodDeclaration(arrowClause.Parent as BaseMethodDeclarationSyntax);
                }

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            protected override IEnumerable<TypeInferenceInfo> InferTypesWorker_DoNotCallDirectly(int position)
            {
                var syntaxTree = SemanticModel.SyntaxTree;
                var token = syntaxTree.FindTokenOnLeftOfPosition(position, CancellationToken);
                token = token.GetPreviousTokenIfTouchingWord(position);

                var parent = token.Parent;

                return parent.TypeSwitch(
                    (AnonymousObjectMemberDeclaratorSyntax memberDeclarator) => InferTypeInMemberDeclarator(memberDeclarator, token),
                    (ArgumentListSyntax argument) => InferTypeInArgumentList(argument, token),
                    (ArgumentSyntax argument) => InferTypeInArgument(argument, token),
                    (ArrayCreationExpressionSyntax arrayCreationExpression) => InferTypeInArrayCreationExpression(arrayCreationExpression, token),
                    (ArrayRankSpecifierSyntax arrayRankSpecifier) => InferTypeInArrayRankSpecifier(arrayRankSpecifier, token),
                    (ArrayTypeSyntax arrayType) => InferTypeInArrayType(arrayType, token),
                    (ArrowExpressionClauseSyntax arrowClause) => InferTypeInArrowExpressionClause(arrowClause),
                    (AssignmentExpressionSyntax assignmentExpression) => InferTypeInBinaryOrAssignmentExpression(assignmentExpression, assignmentExpression.OperatorToken, assignmentExpression.Left, assignmentExpression.Right, previousToken: token),
                    (AttributeArgumentListSyntax attributeArgumentList) => InferTypeInAttributeArgumentList(attributeArgumentList, token),
                    (AttributeArgumentSyntax argument) => InferTypeInAttributeArgument(argument, token),
                    (AttributeListSyntax attributeDeclaration) => InferTypeInAttributeDeclaration(attributeDeclaration, token),
                    (AttributeTargetSpecifierSyntax attributeTargetSpecifier) => InferTypeInAttributeTargetSpecifier(attributeTargetSpecifier, token),
                    (AwaitExpressionSyntax awaitExpression) => InferTypeInAwaitExpression(awaitExpression, token),
                    (BinaryExpressionSyntax binaryExpression) => InferTypeInBinaryOrAssignmentExpression(binaryExpression, binaryExpression.OperatorToken, binaryExpression.Left, binaryExpression.Right, previousToken: token),
                    (BracketedArgumentListSyntax bracketedArgumentList) => InferTypeInBracketedArgumentList(bracketedArgumentList, token),
                    (CastExpressionSyntax castExpression) => InferTypeInCastExpression(castExpression, previousToken: token),
                    (CatchDeclarationSyntax catchDeclaration) => InferTypeInCatchDeclaration(catchDeclaration, token),
                    (CatchFilterClauseSyntax catchFilterClause) => InferTypeInCatchFilterClause(catchFilterClause, token),
                    (CheckedExpressionSyntax checkedExpression) => InferTypes(checkedExpression),
                    (ConditionalExpressionSyntax conditionalExpression) => InferTypeInConditionalExpression(conditionalExpression, previousToken: token),
                    (DefaultExpressionSyntax defaultExpression) => InferTypeInDefaultExpression(defaultExpression),
                    (DoStatementSyntax doStatement) => InferTypeInDoStatement(doStatement, token),
                    (EqualsValueClauseSyntax equalsValue) => InferTypeInEqualsValueClause(equalsValue, token),
                    (ExpressionStatementSyntax expressionStatement) => InferTypeInExpressionStatement(expressionStatement, token),
                    (ForEachStatementSyntax forEachStatement) => InferTypeInForEachStatement(forEachStatement, previousToken: token),
                    (ForStatementSyntax forStatement) => InferTypeInForStatement(forStatement, previousToken: token),
                    (IfStatementSyntax ifStatement) => InferTypeInIfStatement(ifStatement, token),
                    (InitializerExpressionSyntax initializerExpression) => InferTypeInInitializerExpression(initializerExpression, previousToken: token),
                    (LockStatementSyntax lockStatement) => InferTypeInLockStatement(lockStatement, token),
                    (MemberAccessExpressionSyntax memberAccessExpression) => InferTypeInMemberAccessExpression(memberAccessExpression, previousToken: token),
                    (NameColonSyntax nameColon) => InferTypeInNameColon(nameColon, token),
                    (NameEqualsSyntax nameEquals) => InferTypeInNameEquals(nameEquals, token),
                    (ObjectCreationExpressionSyntax objectCreation) => InferTypeInObjectCreationExpression(objectCreation, token),
                    (ParenthesizedLambdaExpressionSyntax parenthesizedLambdaExpression) => InferTypeInParenthesizedLambdaExpression(parenthesizedLambdaExpression, token),
                    (PostfixUnaryExpressionSyntax postfixUnary) => InferTypeInPostfixUnaryExpression(postfixUnary, token),
                    (PrefixUnaryExpressionSyntax prefixUnary) => InferTypeInPrefixUnaryExpression(prefixUnary, token),
                    (ReturnStatementSyntax returnStatement) => InferTypeForReturnStatement(returnStatement, token),
                    (SimpleLambdaExpressionSyntax simpleLambdaExpression) => InferTypeInSimpleLambdaExpression(simpleLambdaExpression, token),
                    (SwitchLabelSyntax switchLabel) => InferTypeInSwitchLabel(switchLabel, token),
                    (SwitchStatementSyntax switchStatement) => InferTypeInSwitchStatement(switchStatement, token),
                    (ThrowStatementSyntax throwStatement) => InferTypeInThrowStatement(throwStatement, token),
                    (UsingStatementSyntax usingStatement) => InferTypeInUsingStatement(usingStatement, token),
                    (WhileStatementSyntax whileStatement) => InferTypeInWhileStatement(whileStatement, token),
                    (YieldStatementSyntax yieldStatement) => InferTypeInYieldStatement(yieldStatement, token),
                    _ => SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>());
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
                    var types = GetTypes(objectCreation).Select(t => t.InferredType);

                    if (types.Any(t => t is INamedTypeSymbol))
                    {
                        return types.OfType<INamedTypeSymbol>().SelectMany(t =>
                            GetCollectionElementType(t,
                                parameterIndex: 0));
                    }
                }

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInAttributeArgument(AttributeArgumentSyntax argument, SyntaxToken? previousToken = null, ArgumentSyntax argumentOpt = null)
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
                    var attribute = argument.Parent.Parent as AttributeSyntax;
                    if (attribute != null)
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
                return InferTypeInArgument(index, methods, argument);
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
                if (previousToken.Kind() == SyntaxKind.NewKeyword &&
                    previousToken.GetPreviousToken().Kind() == SyntaxKind.EqualsToken)
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
                    return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(type));
                }

                var constructors = type.InstanceConstructors.Where(m => m.Parameters.Length > index);
                return InferTypeInArgument(index, constructors, argumentOpt);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInInvocationExpression(
                InvocationExpressionSyntax invocation, int index, ArgumentSyntax argumentOpt = null)
            {
                // Check all the methods that have at least enough arguments to support
                // being called with argument at this position.  Note: if they're calling an
                // extension method then it will need one more argument in order for us to
                // call it.
                var info = SemanticModel.GetSymbolInfo(invocation, CancellationToken);
                IEnumerable<IMethodSymbol> methods = null;

                // Overload resolution (see DevDiv 611477) in certain extension method cases
                // can result in GetSymbolInfo returning nothing. In this case, get the 
                // method group info, which is what signature help already does.
                if (info.CandidateReason == CandidateReason.None)
                {
                    methods = SemanticModel.GetMemberGroup(invocation.Expression, CancellationToken)
                                                            .OfType<IMethodSymbol>();
                }
                else
                {
                    methods = info.GetBestOrAllSymbols().OfType<IMethodSymbol>();
                }

                return InferTypeInArgument(index, methods, argumentOpt);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInArgumentList(ArgumentListSyntax argumentList, SyntaxToken previousToken)
            {
                // Has to follow the ( or a ,
                if (previousToken != argumentList.OpenParenToken && previousToken.Kind() != SyntaxKind.CommaToken)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
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

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInAttributeArgumentList(AttributeArgumentListSyntax attributeArgumentList, SyntaxToken previousToken)
            {
                // Has to follow the ( or a ,
                if (previousToken != attributeArgumentList.OpenParenToken && previousToken.Kind() != SyntaxKind.CommaToken)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                var attribute = attributeArgumentList.Parent as AttributeSyntax;
                if (attribute != null)
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
                return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(this.Compilation.GetSpecialType(SpecialType.System_Int32)));
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInAttributeArgument(int index, IEnumerable<IMethodSymbol> methods, AttributeArgumentSyntax argumentOpt = null)
            {
                return InferTypeInAttributeArgument(index, methods.Select(m => m.Parameters), argumentOpt);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInArgument(int index, IEnumerable<IMethodSymbol> methods, ArgumentSyntax argumentOpt)
            {
                if (argumentOpt != null)
                {
                    var invocation = argumentOpt?.Parent?.Parent as InvocationExpressionSyntax;
                    if (invocation != null)
                    {
                        // We're trying to figure out the signature of a method we're an argument to. 
                        // That method may be generic, and we might end up using one of its generic
                        // type parameters in the type we infer.  First, let's see if we can instantiate
                        // the methods so that the type can be inferred better.
                        var invocationTypes = this.InferTypes(invocation).Select(t => t.InferredType).ToList();
                        var instantiatedMethods = methods.Select(m => Instantiate(m, invocationTypes)).ToList();

                        // Now that we've instantiated the methods, filter down to the ones that 
                        // will actually return a viable type given where this invocation expression
                        // is.
                        var filteredMethods = instantiatedMethods.Where(m =>
                            invocationTypes.Any(t => Compilation.ClassifyConversion(m.ReturnType, t).IsImplicit)).ToList();

                        // If we filtered down to nothing, then just fall back to the instantiated list.
                        // this is a best effort after all.
                        methods = filteredMethods.Any() ? filteredMethods : instantiatedMethods;
                    }
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

                var typeArguments = method.ConstructedFrom.TypeParameters.Select(tp => bestMap.GetValueOrDefault(tp) ?? tp).ToArray();
                return method.ConstructedFrom.Construct(typeArguments);
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
                return InferTypeInArgument(index, parameterizedSymbols, name);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInArgument(
                int index,
                IEnumerable<ImmutableArray<IParameterSymbol>> parameterizedSymbols,
                ArgumentSyntax argumentOpt)
            {
                var name = argumentOpt != null && argumentOpt.NameColon != null ? argumentOpt.NameColon.Name.Identifier.ValueText : null;
                return InferTypeInArgument(index, parameterizedSymbols, name);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInArgument(
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
                                                        .Select(p => new TypeInferenceInfo(p.Type, p.IsParams));
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
                            let param = parameterSet[index]
                            select new TypeInferenceInfo(param.Type, param.IsParams);

                    return q;
                }

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
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

                return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(this.Compilation.GetSpecialType(SpecialType.System_Int32)));
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
                    currentTypes = currentTypes.Where(c => c.InferredType is IArrayTypeSymbol)
                        .Select(c => new TypeInferenceInfo(((IArrayTypeSymbol)c.InferredType).ElementType));
                }
                return currentTypes;
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInAttribute(AttributeSyntax attribute)
            {
                return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(this.Compilation.AttributeType()));
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInAttributeDeclaration(AttributeListSyntax attributeDeclaration, SyntaxToken? previousToken)
            {
                // If we have a position, then it has to be after the open bracket.
                if (previousToken.HasValue && previousToken.Value != attributeDeclaration.OpenBracketToken)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(this.Compilation.AttributeType()));
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

                return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(this.Compilation.AttributeType()));
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInBracketedArgumentList(BracketedArgumentListSyntax bracketedArgumentList, SyntaxToken previousToken)
            {
                // Has to follow the [ or a ,
                if (previousToken != bracketedArgumentList.OpenBracketToken && previousToken.Kind() != SyntaxKind.CommaToken)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                var elementAccess = bracketedArgumentList.Parent as ElementAccessExpressionSyntax;
                if (elementAccess != null)
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
                            // x << Foo(), x >> Foo(), x <<= Foo(), x >>= Foo()
                            return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(this.Compilation.GetSpecialType(SpecialType.System_Int32)));
                        }

                        break;
                }

                // Infer operands of && and || as bool regardless of the other operand.
                if (operatorToken.Kind() == SyntaxKind.AmpersandAmpersandToken ||
                    operatorToken.Kind() == SyntaxKind.BarBarToken)
                {
                    return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(this.Compilation.GetSpecialType(SpecialType.System_Boolean)));
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
                        return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(this.Compilation.GetSpecialType(SpecialType.System_Int32)));

                    case SyntaxKind.BarEqualsToken:
                    case SyntaxKind.AmpersandEqualsToken:
                        // NOTE(cyrusn): |= and &= can be used for both ints and bools  However, in the
                        // case where there isn't enough information to determine which the user wanted,
                        // I'm just defaulting to bool based on personal preference.
                        return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(this.Compilation.GetSpecialType(SpecialType.System_Boolean)));
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

                return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(this.Compilation.ExceptionType()));
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInCatchFilterClause(CatchFilterClauseSyntax catchFilterClause, SyntaxToken? previousToken = null)
            {
                // If we have a position, it has to be after "if ("
                if (previousToken.HasValue && previousToken.Value != catchFilterClause.OpenParenToken)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(this.Compilation.GetSpecialType(SpecialType.System_Boolean)));
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
                            ? new TypeInferenceInfo(((INamedTypeSymbol)x.InferredType).TypeArguments[0]) // nullableExpr ?? Foo()
                            : x); // normalExpr ?? Foo() 
                }

                var rightTypes = GetTypes(coalesceExpression.Right);
                if (!rightTypes.Any())
                {
                    return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(Compilation.GetSpecialType(SpecialType.System_Object)));
                }

                return rightTypes
                    .Select(x => x.InferredType.IsValueType
                                     ? new TypeInferenceInfo(this.Compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(x.InferredType)) // Foo() ?? 0
                                     : x); // Foo() ?? ""
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInConditionalAccessExpression(ConditionalAccessExpressionSyntax expression)
            {
                return InferTypes(expression);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInConditionalExpression(ConditionalExpressionSyntax conditional, ExpressionSyntax expressionOpt = null, SyntaxToken? previousToken = null)
            {
                if (expressionOpt != null && conditional.Condition == expressionOpt)
                {
                    // Foo() ? a : b
                    return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(this.Compilation.GetSpecialType(SpecialType.System_Boolean)));
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

                return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(this.Compilation.GetSpecialType(SpecialType.System_Boolean)));
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
                    var parameter = SemanticModel.GetDeclaredSymbol(equalsValue.Parent, CancellationToken) as IParameterSymbol;
                    if (parameter != null)
                    {
                        return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(parameter.Type));
                    }
                }

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInPropertyDeclaration(PropertyDeclarationSyntax propertyDeclaration)
            {
                Contract.Assert(propertyDeclaration?.Type != null, "Property type should never be null");

                var typeInfo = SemanticModel.GetTypeInfo(propertyDeclaration.Type);
                return typeInfo.Type != null
                    ? SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(typeInfo.Type))
                    : SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInBaseMethodDeclaration(BaseMethodDeclarationSyntax declaration)
            {
                var methodSymbol = SemanticModel.GetDeclaredSymbol(declaration);
                return methodSymbol?.ReturnType != null
                    ? SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(methodSymbol.ReturnType))
                    : SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInExpressionStatement(ExpressionStatementSyntax expressionStatement, SyntaxToken? previousToken = null)
            {
                // If we're position based, then that means we're after the semicolon.  In this case
                // we don't have any sort of type to infer.
                if (previousToken.HasValue)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(this.Compilation.GetSpecialType(SpecialType.System_Void)));
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

                // foreach (int v = Foo())
                var variableTypes = GetTypes(forEachStatementSyntax.Type);
                if (!variableTypes.Any())
                {
                    return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(
                        this.Compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T)
                        .Construct(Compilation.GetSpecialType(SpecialType.System_Object))));
                }

                var type = this.Compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T);
                return variableTypes.Select(v => new TypeInferenceInfo(type.Construct(v.InferredType)));
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

                return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(this.Compilation.GetSpecialType(SpecialType.System_Boolean)));
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInIfStatement(IfStatementSyntax ifStatement, SyntaxToken? previousToken = null)
            {
                // If we have a position, we have to be after the "if("
                if (previousToken.HasValue && previousToken.Value != ifStatement.OpenParenToken)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(this.Compilation.GetSpecialType(SpecialType.System_Boolean)));
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
                        .Select(a => new TypeInferenceInfo(a.Parameters.ElementAtOrDefault(parameterIndex)?.Type))
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
                            .Select(a => new TypeInferenceInfo(a.Parameters[0].Type));

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
                    var elementTypes = arrayTypes.OfType<IArrayTypeSymbol>().Select(a => new TypeInferenceInfo(a.ElementType)).Where(IsUsableTypeFunc);

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
                    // = { Foo() }
                    var equalsValueClause = (EqualsValueClauseSyntax)initializerExpression.Parent;
                    IEnumerable<ITypeSymbol> types = InferTypeInEqualsValueClause(equalsValueClause).Select(t => t.InferredType);

                    if (types.Any(t => t is IArrayTypeSymbol))
                    {
                        return types.OfType<IArrayTypeSymbol>().Select(t => new TypeInferenceInfo(t.ElementType));
                    }
                }
                else if (initializerExpression.IsParentKind(SyntaxKind.ArrayCreationExpression))
                {
                    // new int[] { Foo() } 
                    var arrayCreation = (ArrayCreationExpressionSyntax)initializerExpression.Parent;
                    IEnumerable<ITypeSymbol> types = GetTypes(arrayCreation).Select(t => t.InferredType);

                    if (types.Any(t => t is IArrayTypeSymbol))
                    {
                        return types.OfType<IArrayTypeSymbol>().Select(t => new TypeInferenceInfo(t.ElementType));
                    }
                }
                else if (initializerExpression.IsParentKind(SyntaxKind.ObjectCreationExpression))
                {
                    // new List<T> { Foo() } 

                    var objectCreation = (ObjectCreationExpressionSyntax)initializerExpression.Parent;

                    IEnumerable<ITypeSymbol> types = GetTypes(objectCreation).Select(t => t.InferredType);
                    if (types.Any(t => t is INamedTypeSymbol))
                    {
                        return types.OfType<INamedTypeSymbol>().SelectMany(t =>
                            GetCollectionElementType(t, parameterIndex: 0));
                    }
                }
                else if (initializerExpression.IsParentKind(SyntaxKind.SimpleAssignmentExpression))
                {
                    // new Foo { a = { Foo() } }

                    if (expressionOpt != null)
                    {
                        var addMethodSymbols = SemanticModel.GetCollectionInitializerSymbolInfo(expressionOpt).GetAllSymbols();
                        var addMethodParameterTypes = addMethodSymbols.Select(a => new TypeInferenceInfo(((IMethodSymbol)a).Parameters[0].Type));
                        if (addMethodParameterTypes.Any())
                        {
                            return addMethodParameterTypes;
                        }
                    }

                    var assignExpression = (AssignmentExpressionSyntax)initializerExpression.Parent;
                    IEnumerable<ITypeSymbol> types = GetTypes(assignExpression.Left).Select(t => t.InferredType);

                    if (types.Any(t => t is INamedTypeSymbol))
                    {
                        // new Foo { a = { Foo() } }
                        var parameterIndex = previousToken.HasValue
                                ? initializerExpression.Expressions.GetSeparators().ToList().IndexOf(previousToken.Value) + 1
                                : initializerExpression.Expressions.IndexOf(expressionOpt);

                        return types.OfType<INamedTypeSymbol>().SelectMany(t =>
                            GetCollectionElementType(t, 0));
                    }
                }

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInIsPatternExpression(
                IsPatternExpressionSyntax isPatternExpression,
                ExpressionSyntax expression)
            {
                if (expression == isPatternExpression.Expression)
                {
                    return GetPatternTypes(isPatternExpression.Pattern);
                }

                return null;
            }

            private IEnumerable<TypeInferenceInfo> GetPatternTypes(PatternSyntax pattern)
            {
                return pattern.TypeSwitch(
                    (DeclarationPatternSyntax declarationPattern) => GetTypes(declarationPattern.Type),
                    (ConstantPatternSyntax constantPattern) => GetTypes(constantPattern.Expression));
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInLockStatement(LockStatementSyntax lockStatement, SyntaxToken? previousToken = null)
            {
                // If we're position based, then we have to be after the "lock("
                if (previousToken.HasValue && previousToken.Value != lockStatement.OpenParenToken)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(this.Compilation.GetSpecialType(SpecialType.System_Object)));
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax lambdaExpression, SyntaxToken? previousToken = null)
            {
                // If we have a position, it has to be after the lambda arrow.
                if (previousToken.HasValue && previousToken.Value != lambdaExpression.ArrowToken)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                return InferTypeInLambdaExpression(lambdaExpression);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInSimpleLambdaExpression(SimpleLambdaExpressionSyntax lambdaExpression, SyntaxToken? previousToken = null)
            {
                // If we have a position, it has to be after the lambda arrow.
                if (previousToken.HasValue && previousToken.Value != lambdaExpression.ArrowToken)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                return InferTypeInLambdaExpression(lambdaExpression);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInLambdaExpression(ExpressionSyntax lambdaExpression)
            {
                // Func<int,string> = i => Foo();
                var types = InferTypes(lambdaExpression);
                var type = types.FirstOrDefault().InferredType.GetDelegateType(this.Compilation);

                if (type != null)
                {
                    var invoke = type.DelegateInvokeMethod;
                    if (invoke != null)
                    {
                        return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(invoke.ReturnType));
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
                            .Select(p => new TypeInferenceInfo(p.Type)));
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

                var argumentSyntax = nameColon.Parent as ArgumentSyntax;
                if (argumentSyntax != null)
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
                // the member access expression.  i.e. if we have "Foo.Bar" then we can 
                // def infer what the type of 'Bar' should be (it's whatever type we infer
                // for 'Foo.Bar' itself.  However, if we're on 'Foo' then we can't figure
                // out anything about its type.
                if (previousToken != null)
                {
                    if (previousToken.Value != memberAccessExpression.OperatorToken)
                    {
                        return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                    }

                    // We're right after the dot in "Foo.Bar".  The type for "Bar" should be
                    // whatever type we'd infer for "Foo.Bar" itself.
                    return InferTypes(memberAccessExpression);
                }
                else
                {
                    Debug.Assert(expressionOpt != null);
                    if (expressionOpt == memberAccessExpression.Expression)
                    {
                        return InferTypeForExpressionOfMemberAccessExpression(memberAccessExpression);
                    }

                    // We're right after the dot in "Foo.Bar".  The type for "Bar" should be
                    // whatever type we'd infer for "Foo.Bar" itself.
                    return InferTypes(memberAccessExpression);
                }
            }

            private IEnumerable<TypeInferenceInfo> InferTypeForExpressionOfMemberAccessExpression(
                MemberAccessExpressionSyntax memberAccessExpression)
            {
                // If we're on the left side of a dot, it's possible in a few cases
                // to figure out what type we should be.  Specifically, if we have
                //
                //      await foo.ConfigureAwait()
                //
                // then we can figure out what 'foo' should be based on teh await
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
                    // foo.ContinueWith(...)
                    // We want to infer Task<T>.  For now, we'll just do Task<object>,
                    // in the future it would be nice to figure out the actual result
                    // type based on the argument to ContinueWith.
                    var taskOfT = this.Compilation.TaskOfTType();
                    if (taskOfT != null)
                    {
                        return SpecializedCollections.SingletonEnumerable(
                            new TypeInferenceInfo(taskOfT.Construct(this.Compilation.ObjectType)));
                    }
                }
                else if (name.Equals(nameof(Enumerable.Select)) ||
                         name.Equals(nameof(Enumerable.Where)))
                {
                    var ienumerableType = this.Compilation.IEnumerableOfTType();

                    // foo.Select
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

                                return SpecializedCollections.SingletonEnumerable(
                                    new TypeInferenceInfo(ienumerableType.Construct(typeArg)));
                            }
                        }
                    }
                }

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            private ITypeSymbol InferTypeForFirstParameterOfLambda(
                LambdaExpressionSyntax lambdaExpression)
            {
                if (lambdaExpression is ParenthesizedLambdaExpressionSyntax)
                {
                    return InferTypeForFirstParameterOfParenthesizedLambda(
                        (ParenthesizedLambdaExpressionSyntax)lambdaExpression);
                }
                else if (lambdaExpression is SimpleLambdaExpressionSyntax)
                {
                    return InferTypeForFirstParameterOfSimpleLambda(
                        (SimpleLambdaExpressionSyntax)lambdaExpression);
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

            private IEnumerable<TypeInferenceInfo> InferTypeInNameEquals(NameEqualsSyntax nameEquals, SyntaxToken? previousToken = null)
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
                        return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(this.Compilation.GetSpecialType(SpecialType.System_Int32)));
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
                        // ++, --, +Foo(), -Foo();
                        return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(this.Compilation.GetSpecialType(SpecialType.System_Int32)));

                    case SyntaxKind.BitwiseNotExpression:
                        // ~Foo()
                        var types = InferTypes(prefixUnaryExpression);
                        if (!types.Any())
                        {
                            return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(this.Compilation.GetSpecialType(SpecialType.System_Int32)));
                        }
                        else
                        {
                            return types;
                        }

                    case SyntaxKind.LogicalNotExpression:
                        // !Foo()
                        return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(this.Compilation.GetSpecialType(SpecialType.System_Boolean)));
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
                    return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(task));
                }

                return types.Select(t => t.InferredType.SpecialType == SpecialType.System_Void ? new TypeInferenceInfo(task) : new TypeInferenceInfo(taskOfT.Construct(t.InferredType)));
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInYieldStatement(YieldStatementSyntax yieldStatement, SyntaxToken? previousToken = null)
            {
                // If we are position based, then we have to be after the return keyword
                if (previousToken.HasValue && (previousToken.Value != yieldStatement.ReturnOrBreakKeyword || yieldStatement.ReturnOrBreakKeyword.IsKind(SyntaxKind.BreakKeyword)))
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                var memberSymbol = GetDeclaredMemberSymbolFromOriginalSemanticModel(SemanticModel, yieldStatement.GetAncestorOrThis<MemberDeclarationSyntax>());

                var memberType = memberSymbol.TypeSwitch(
                        (IMethodSymbol method) => method.ReturnType,
                        (IPropertySymbol property) => property.Type);

                if (memberType is INamedTypeSymbol)
                {
                    if (memberType.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T ||
                        memberType.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerator_T)
                    {
                        return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(((INamedTypeSymbol)memberType).TypeArguments[0]));
                    }
                }

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            private IEnumerable<TypeInferenceInfo> InferTypeForReturnStatement(ReturnStatementSyntax returnStatement, SyntaxToken? previousToken = null)
            {
                bool isAsync;
                IEnumerable<TypeInferenceInfo> types;

                InferTypeForReturnStatement(returnStatement, previousToken, out isAsync, out types);

                if (!isAsync)
                {
                    return types;
                }

                var taskOfT = this.Compilation.TaskOfTType();
                if (taskOfT == null || types == null)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                return from t in types
                       where t.InferredType != null && t.InferredType.OriginalDefinition.Equals(taskOfT)
                       let nt = (INamedTypeSymbol)t.InferredType
                       where nt.TypeArguments.Length == 1
                       select new TypeInferenceInfo(nt.TypeArguments[0]);
            }

            private void InferTypeForReturnStatement(
                ReturnStatementSyntax returnStatement, SyntaxToken? previousToken, out bool isAsync, out IEnumerable<TypeInferenceInfo> types)
            {
                isAsync = false;
                types = SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();

                // If we are position based, then we have to be after the return statement.
                if (previousToken.HasValue && previousToken.Value != returnStatement.ReturnKeyword)
                {
                    return;
                }

                var ancestorExpressions = returnStatement.GetAncestorsOrThis<ExpressionSyntax>();

                // If we're in a lambda, then use the return type of the lambda to figure out what to
                // infer.  i.e.   Func<int,string> f = i => { return Foo(); }
                var lambda = ancestorExpressions.FirstOrDefault(e => e.IsKind(SyntaxKind.ParenthesizedLambdaExpression, SyntaxKind.SimpleLambdaExpression));
                if (lambda != null)
                {
                    types = InferTypeInLambdaExpression(lambda);
                    isAsync = lambda is ParenthesizedLambdaExpressionSyntax && ((ParenthesizedLambdaExpressionSyntax)lambda).AsyncKeyword.Kind() != SyntaxKind.None;
                    return;
                }

                // If we are inside a delegate then use the return type of the Invoke Method of the delegate type
                var delegateExpression = (AnonymousMethodExpressionSyntax)ancestorExpressions.FirstOrDefault(e => e.IsKind(SyntaxKind.AnonymousMethodExpression));
                if (delegateExpression != null)
                {
                    var delegateType = InferTypes(delegateExpression).FirstOrDefault().InferredType;
                    if (delegateType != null && delegateType.IsDelegateType())
                    {
                        var delegateInvokeMethod = delegateType.GetDelegateType(this.Compilation).DelegateInvokeMethod;
                        if (delegateInvokeMethod != null)
                        {
                            types = SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(delegateInvokeMethod.ReturnType));
                            isAsync = delegateExpression.AsyncKeyword.Kind() != SyntaxKind.None;
                            return;
                        }
                    }
                }

                var memberSymbol = GetDeclaredMemberSymbolFromOriginalSemanticModel(SemanticModel, returnStatement.GetAncestorOrThis<MemberDeclarationSyntax>());

                if (memberSymbol.IsKind(SymbolKind.Method))
                {
                    var method = memberSymbol as IMethodSymbol;

                    isAsync = method.IsAsync;
                    types = SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(method.ReturnType));
                    return;
                }
                else if (memberSymbol.IsKind(SymbolKind.Property))
                {
                    types = SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo((memberSymbol as IPropertySymbol).Type));
                    return;
                }
                else if (memberSymbol.IsKind(SymbolKind.Field))
                {
                    types = SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo((memberSymbol as IFieldSymbol).Type));
                    return;
                }
            }

            private ISymbol GetDeclaredMemberSymbolFromOriginalSemanticModel(SemanticModel currentSemanticModel, MemberDeclarationSyntax declarationInCurrentTree)
            {
                var originalSemanticModel = currentSemanticModel.GetOriginalSemanticModel();
                MemberDeclarationSyntax declaration;

                if (currentSemanticModel.IsSpeculativeSemanticModel)
                {
                    var tokenInOriginalTree = originalSemanticModel.SyntaxTree.GetRoot(CancellationToken).FindToken(currentSemanticModel.OriginalPositionForSpeculation);
                    declaration = tokenInOriginalTree.GetAncestor<MemberDeclarationSyntax>();
                }
                else
                {
                    declaration = declarationInCurrentTree;
                }

                return originalSemanticModel.GetDeclaredSymbol(declaration, CancellationToken);
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

                return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(this.Compilation.GetSpecialType(SpecialType.System_Int32)));
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInThrowStatement(ThrowStatementSyntax throwStatement, SyntaxToken? previousToken = null)
            {
                // If we have a position, it has to be after the 'throw' keyword.
                if (previousToken.HasValue && previousToken.Value != throwStatement.ThrowKeyword)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(this.Compilation.ExceptionType()));
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInUsingStatement(UsingStatementSyntax usingStatement, SyntaxToken? previousToken = null)
            {
                // If we have a position, it has to be after "using("
                if (previousToken.HasValue && previousToken.Value != usingStatement.OpenParenToken)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(this.Compilation.GetSpecialType(SpecialType.System_IDisposable)));
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInVariableDeclarator(VariableDeclaratorSyntax variableDeclarator)
            {
                var variableType = variableDeclarator.GetVariableType();
                if (variableType == null)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                var types = GetTypes(variableType).Where(IsUsableTypeFunc);

                if (variableType.IsVar)
                {
                    var variableDeclaration = variableDeclarator.Parent as VariableDeclarationSyntax;
                    if (variableDeclaration != null)
                    {
                        if (variableDeclaration.IsParentKind(SyntaxKind.UsingStatement))
                        {
                            // using (var v = Foo())
                            return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(this.Compilation.GetSpecialType(SpecialType.System_IDisposable)));
                        }

                        if (variableDeclaration.IsParentKind(SyntaxKind.ForStatement))
                        {
                            // for (var v = Foo(); ..
                            return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(this.Compilation.GetSpecialType(SpecialType.System_Int32)));
                        }

                        // Return the types here if they actually bound to a type called 'var'.
                        return types.Where(t => t.InferredType.Name == "var");
                    }
                }

                return types;
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInVariableComponentAssignment(
                VariableComponentAssignmentSyntax variableComponentAssigment, ExpressionSyntax expression)
            {
                if (expression == variableComponentAssigment.Value)
                {
                    var variableComponent = variableComponentAssigment.VariableComponent;
                    if (variableComponent.IsKind(SyntaxKind.TypedVariableComponent))
                    {
                        var typedVariable = (TypedVariableComponentSyntax)variableComponent;
                        return GetTypes(typedVariable.Type);
                    }
                    else if (variableComponent.IsKind(SyntaxKind.ParenthesizedVariableComponent))
                    {
                        // We have something of the form:
                        //   (int a, int b) = ...
                        //
                        // This is a deconstruction, and a decent deconstructable type we can infer here
                        // is ValueTuple<int,int>.
                        var parenthesizedVariable = (ParenthesizedVariableComponentSyntax)variableComponent;
                        var tupleType = GetTupleType(parenthesizedVariable);

                        if (tupleType != null)
                        {
                            return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(tupleType));
                        }
                    }
                }

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }

            private ITypeSymbol GetTupleType(
                ParenthesizedVariableComponentSyntax parenthesizedVariableComponent)
            {
                ImmutableArray<ITypeSymbol> elementTypes;
                ImmutableArray<string> elementNames;

                if (!TryGetTupleTypesAndNames(parenthesizedVariableComponent.Variables,
                        out elementTypes, out elementNames))
                {
                    return null;
                }

                return Compilation.CreateTupleTypeSymbol(elementTypes, elementNames);
            }

            private bool TryGetTupleTypesAndNames(
                SeparatedSyntaxList<VariableComponentSyntax> variables,
                out ImmutableArray<ITypeSymbol> elementTypes,
                out ImmutableArray<string> elementNames)
            {
                elementTypes = default(ImmutableArray<ITypeSymbol>);
                elementNames = default(ImmutableArray<string>);

                var elementTypesBuilder = ArrayBuilder<ITypeSymbol>.GetInstance();
                var elementNamesBuilder = ArrayBuilder<string>.GetInstance();
                try
                {
                    foreach (var component in variables)
                    {
                        if (component.IsKind(SyntaxKind.TypedVariableComponent))
                        {
                            AddTypeAndName((TypedVariableComponentSyntax)component, elementTypesBuilder, elementNamesBuilder);
                        }
                        else if (component.IsKind(SyntaxKind.ParenthesizedVariableComponent))
                        {
                            AddTypeAndName((ParenthesizedVariableComponentSyntax)component, elementTypesBuilder, elementNamesBuilder);
                        }
                    }

                    if (elementTypesBuilder.Contains(null))
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
                TypedVariableComponentSyntax component, 
                ArrayBuilder<ITypeSymbol> elementTypesBuilder, 
                ArrayBuilder<string> elementNamesBuilder)
            {
                elementTypesBuilder.Add(GetTypes(component.Type).FirstOrDefault().InferredType);

                var designation = component.Designation;
                if (designation.IsKind(SyntaxKind.SingleVariableDesignation))
                {
                    var singleVariable = (SingleVariableDesignationSyntax)designation;
                    elementNamesBuilder.Add(singleVariable.Identifier.ValueText);
                }
                else
                {
                    elementNamesBuilder.Add(null);
                }
            }

            private void AddTypeAndName(
                ParenthesizedVariableComponentSyntax component, 
                ArrayBuilder<ITypeSymbol> elementTypesBuilder, 
                ArrayBuilder<string> elementNamesBuilder)
            {
                var tupleType = GetTupleType(component);
                elementTypesBuilder.Add(tupleType);
                elementNamesBuilder.Add(null);
            }

            private IEnumerable<TypeInferenceInfo> InferTypeInWhileStatement(WhileStatementSyntax whileStatement, SyntaxToken? previousToken = null)
            {
                // If we're position based, then we have to be after the "while("
                if (previousToken.HasValue && previousToken.Value != whileStatement.OpenParenToken)
                {
                    return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
                }

                return SpecializedCollections.SingletonEnumerable(new TypeInferenceInfo(this.Compilation.GetSpecialType(SpecialType.System_Boolean)));
            }

            private IEnumerable<TypeInferenceInfo> GetCollectionElementType(INamedTypeSymbol type, int parameterIndex)
            {
                if (type != null)
                {
                    var parameters = type.GetAllTypeArguments();

                    var elementType = parameters.ElementAtOrDefault(parameterIndex);
                    if (elementType != null)
                    {
                        return SpecializedCollections.SingletonCollection(new TypeInferenceInfo(elementType));
                    }
                }

                return SpecializedCollections.EmptyEnumerable<TypeInferenceInfo>();
            }
        }
    }
}
