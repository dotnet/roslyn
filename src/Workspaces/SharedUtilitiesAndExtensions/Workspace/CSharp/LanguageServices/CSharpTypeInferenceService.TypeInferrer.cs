// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp;

internal partial class CSharpTypeInferenceService
{
    private sealed class TypeInferrer : AbstractTypeInferrer
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
            return [];
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
                    var typeInferenceInfo = new TypeInferenceInfo(typeInfo.Type);

                    // If it bound to a method, try to get the Action/Func form of that method.
                    if (typeInferenceInfo.InferredType == null)
                    {
                        var allSymbols = symbolInfo.GetAllSymbols();
                        if (allSymbols is [IMethodSymbol method])
                            typeInferenceInfo = new TypeInferenceInfo(method.ConvertToType(this.Compilation));
                    }

                    if (IsUsableTypeFunc(typeInferenceInfo))
                        return [typeInferenceInfo];
                }
            }

            return [];
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

            return parent switch
            {
                AnonymousObjectMemberDeclaratorSyntax memberDeclarator => InferTypeInMemberDeclarator(memberDeclarator),
                ArgumentSyntax argument => InferTypeInArgument(argument),
                ArrayCreationExpressionSyntax arrayCreationExpression => InferTypeInArrayCreationExpression(arrayCreationExpression),
                ArrayRankSpecifierSyntax arrayRankSpecifier => InferTypeInArrayRankSpecifier(arrayRankSpecifier),
                ArrayTypeSyntax arrayType => InferTypeInArrayType(arrayType),
                ArrowExpressionClauseSyntax arrowClause => InferTypeInArrowExpressionClause(arrowClause),
                AssignmentExpressionSyntax assignmentExpression => InferTypeInBinaryOrAssignmentExpression(assignmentExpression, assignmentExpression.OperatorToken, assignmentExpression.Left, assignmentExpression.Right, expression),
                AttributeArgumentSyntax attribute => InferTypeInAttributeArgument(attribute),
                AttributeSyntax _ => InferTypeInAttribute(),
                AwaitExpressionSyntax awaitExpression => InferTypeInAwaitExpression(awaitExpression),
                BinaryExpressionSyntax binaryExpression => InferTypeInBinaryOrAssignmentExpression(binaryExpression, binaryExpression.OperatorToken, binaryExpression.Left, binaryExpression.Right, expression),
                CastExpressionSyntax castExpression => InferTypeInCastExpression(castExpression, expression),
                CatchDeclarationSyntax catchDeclaration => InferTypeInCatchDeclaration(catchDeclaration),
                CatchFilterClauseSyntax catchFilterClause => InferTypeInCatchFilterClause(catchFilterClause),
                CheckedExpressionSyntax checkedExpression => InferTypes(checkedExpression),
                ConditionalAccessExpressionSyntax conditionalAccessExpression => InferTypeInConditionalAccessExpression(conditionalAccessExpression),
                ConditionalExpressionSyntax conditionalExpression => InferTypeInConditionalExpression(conditionalExpression, expression),
                ConstantPatternSyntax constantPattern => InferTypeInConstantPattern(constantPattern),
                DoStatementSyntax doStatement => InferTypeInDoStatement(doStatement),
                EqualsValueClauseSyntax equalsValue => InferTypeInEqualsValueClause(equalsValue),
                ExpressionColonSyntax expressionColon => InferTypeInExpressionColon(expressionColon),
                ExpressionStatementSyntax _ => InferTypeInExpressionStatement(),
                ForEachStatementSyntax forEachStatement => InferTypeInForEachStatement(forEachStatement, expression),
                ForStatementSyntax forStatement => InferTypeInForStatement(forStatement, expression),
                IfStatementSyntax ifStatement => InferTypeInIfStatement(ifStatement),
                InitializerExpressionSyntax initializerExpression => InferTypeInInitializerExpression(initializerExpression, expression),
                IsPatternExpressionSyntax isPatternExpression => InferTypeInIsPatternExpression(isPatternExpression, node),
                LockStatementSyntax lockStatement => InferTypeInLockStatement(lockStatement),
                MemberAccessExpressionSyntax memberAccessExpression => InferTypeInMemberAccessExpression(memberAccessExpression, expression),
                NameColonSyntax nameColon => InferTypeInNameColon(nameColon),
                NameEqualsSyntax nameEquals => InferTypeInNameEquals(nameEquals),
                LambdaExpressionSyntax lambdaExpression => InferTypeInLambdaExpression(lambdaExpression),
                PostfixUnaryExpressionSyntax postfixUnary => InferTypeInPostfixUnaryExpression(postfixUnary),
                PrefixUnaryExpressionSyntax prefixUnary => InferTypeInPrefixUnaryExpression(prefixUnary),
                RecursivePatternSyntax propertyPattern => InferTypeInRecursivePattern(propertyPattern),
                PropertyPatternClauseSyntax propertySubpattern => InferTypeInPropertyPatternClause(propertySubpattern),
                RefExpressionSyntax refExpression => InferTypeInRefExpression(refExpression),
                ReturnStatementSyntax returnStatement => InferTypeForReturnStatement(returnStatement),
                SubpatternSyntax subpattern => InferTypeInSubpattern(subpattern, node),
                SwitchExpressionArmSyntax arm => InferTypeInSwitchExpressionArm(arm),
                SwitchLabelSyntax switchLabel => InferTypeInSwitchLabel(switchLabel),
                SwitchStatementSyntax switchStatement => InferTypeInSwitchStatement(switchStatement),
                ThrowExpressionSyntax throwExpression => InferTypeInThrowExpression(throwExpression),
                ThrowStatementSyntax throwStatement => InferTypeInThrowStatement(throwStatement),
                UsingStatementSyntax usingStatement => InferTypeInUsingStatement(usingStatement),
                WhenClauseSyntax whenClause => InferTypeInWhenClause(whenClause),
                WhileStatementSyntax whileStatement => InferTypeInWhileStatement(whileStatement),
                YieldStatementSyntax yieldStatement => InferTypeInYieldStatement(yieldStatement),
                _ => [],
            };
        }

        protected override IEnumerable<TypeInferenceInfo> InferTypesWorker_DoNotCallDirectly(int position)
        {
            var syntaxTree = SemanticModel.SyntaxTree;
            var token = syntaxTree.FindTokenOnLeftOfPosition(position, CancellationToken);
            token = token.GetPreviousTokenIfTouchingWord(position);

            var parent = token.Parent;

            return parent switch
            {
                AnonymousObjectCreationExpressionSyntax anonymousObjectCreation => InferTypeInAnonymousObjectCreation(anonymousObjectCreation, token),
                AnonymousObjectMemberDeclaratorSyntax memberDeclarator => InferTypeInMemberDeclarator(memberDeclarator, token),
                ArgumentListSyntax argument => InferTypeInArgumentList(argument, token),
                ArgumentSyntax argument => InferTypeInArgument(argument, token),
                ArrayCreationExpressionSyntax arrayCreationExpression => InferTypeInArrayCreationExpression(arrayCreationExpression, token),
                ArrayRankSpecifierSyntax arrayRankSpecifier => InferTypeInArrayRankSpecifier(arrayRankSpecifier, token),
                ArrayTypeSyntax arrayType => InferTypeInArrayType(arrayType, token),
                ArrowExpressionClauseSyntax arrowClause => InferTypeInArrowExpressionClause(arrowClause),
                AssignmentExpressionSyntax assignmentExpression => InferTypeInBinaryOrAssignmentExpression(assignmentExpression, assignmentExpression.OperatorToken, assignmentExpression.Left, assignmentExpression.Right, previousToken: token),
                AttributeArgumentListSyntax attributeArgumentList => InferTypeInAttributeArgumentList(attributeArgumentList, token),
                AttributeArgumentSyntax argument => InferTypeInAttributeArgument(argument, token),
                AttributeListSyntax attributeDeclaration => InferTypeInAttributeDeclaration(attributeDeclaration, token),
                AttributeTargetSpecifierSyntax attributeTargetSpecifier => InferTypeInAttributeTargetSpecifier(attributeTargetSpecifier, token),
                AwaitExpressionSyntax awaitExpression => InferTypeInAwaitExpression(awaitExpression, token),
                BinaryExpressionSyntax binaryExpression => InferTypeInBinaryOrAssignmentExpression(binaryExpression, binaryExpression.OperatorToken, binaryExpression.Left, binaryExpression.Right, previousToken: token),
                BinaryPatternSyntax binaryPattern => GetPatternTypes(binaryPattern),
                BracketedArgumentListSyntax bracketedArgumentList => InferTypeInBracketedArgumentList(bracketedArgumentList, token),
                CastExpressionSyntax castExpression => InferTypeInCastExpression(castExpression, previousToken: token),
                CatchDeclarationSyntax catchDeclaration => InferTypeInCatchDeclaration(catchDeclaration, token),
                CatchFilterClauseSyntax catchFilterClause => InferTypeInCatchFilterClause(catchFilterClause, token),
                CheckedExpressionSyntax checkedExpression => InferTypes(checkedExpression),
                ConditionalExpressionSyntax conditionalExpression => InferTypeInConditionalExpression(conditionalExpression, previousToken: token),
                DefaultExpressionSyntax defaultExpression => InferTypeInDefaultExpression(defaultExpression),
                DoStatementSyntax doStatement => InferTypeInDoStatement(doStatement, token),
                EqualsValueClauseSyntax equalsValue => InferTypeInEqualsValueClause(equalsValue, token),
                ExpressionColonSyntax expressionColon => InferTypeInExpressionColon(expressionColon, token),
                ExpressionStatementSyntax _ => InferTypeInExpressionStatement(token),
                ForEachStatementSyntax forEachStatement => InferTypeInForEachStatement(forEachStatement, previousToken: token),
                ForStatementSyntax forStatement => InferTypeInForStatement(forStatement, previousToken: token),
                IfStatementSyntax ifStatement => InferTypeInIfStatement(ifStatement, token),
                ImplicitArrayCreationExpressionSyntax implicitArray => InferTypeInImplicitArrayCreation(implicitArray),
                InitializerExpressionSyntax initializerExpression => InferTypeInInitializerExpression(initializerExpression, previousToken: token),
                LockStatementSyntax lockStatement => InferTypeInLockStatement(lockStatement, token),
                MemberAccessExpressionSyntax memberAccessExpression => InferTypeInMemberAccessExpression(memberAccessExpression, previousToken: token),
                NameColonSyntax nameColon => InferTypeInNameColon(nameColon, token),
                NameEqualsSyntax nameEquals => InferTypeInNameEquals(nameEquals, token),
                BaseObjectCreationExpressionSyntax objectCreation => InferTypeInObjectCreationExpression(objectCreation, token),
                LambdaExpressionSyntax lambdaExpression => InferTypeInLambdaExpression(lambdaExpression, token),
                PostfixUnaryExpressionSyntax postfixUnary => InferTypeInPostfixUnaryExpression(postfixUnary, token),
                PrefixUnaryExpressionSyntax prefixUnary => InferTypeInPrefixUnaryExpression(prefixUnary, token),
                RelationalPatternSyntax relationalPattern => InferTypeInRelationalPattern(relationalPattern),
                ReturnStatementSyntax returnStatement => InferTypeForReturnStatement(returnStatement, token),
                SingleVariableDesignationSyntax singleVariableDesignationSyntax => InferTypeForSingleVariableDesignation(singleVariableDesignationSyntax),
                SwitchLabelSyntax switchLabel => InferTypeInSwitchLabel(switchLabel, token),
                SwitchExpressionSyntax switchExpression => InferTypeInSwitchExpression(switchExpression, token),
                SwitchStatementSyntax switchStatement => InferTypeInSwitchStatement(switchStatement, token),
                ThrowStatementSyntax throwStatement => InferTypeInThrowStatement(throwStatement, token),
                TupleExpressionSyntax tupleExpression => InferTypeInTupleExpression(tupleExpression, token),
                UnaryPatternSyntax unaryPattern => GetPatternTypes(unaryPattern),
                UsingStatementSyntax usingStatement => InferTypeInUsingStatement(usingStatement, token),
                WhenClauseSyntax whenClause => InferTypeInWhenClause(whenClause, token),
                WhileStatementSyntax whileStatement => InferTypeInWhileStatement(whileStatement, token),
                YieldStatementSyntax yieldStatement => InferTypeInYieldStatement(yieldStatement, token),
                _ => [],
            };
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInAnonymousObjectCreation(AnonymousObjectCreationExpressionSyntax expression, SyntaxToken previousToken)
        {
            if (previousToken == expression.NewKeyword)
            {
                return InferTypes(expression.SpanStart);
            }

            return [];
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInArgument(
            ArgumentSyntax argument, SyntaxToken? previousToken = null)
        {
            if (previousToken.HasValue)
            {
                // If we have a position, then it must be after the colon in a named argument.
                if (argument.NameColon == null || argument.NameColon.ColonToken != previousToken)
                    return [];
            }

            if (argument is { Parent.Parent: ConstructorInitializerSyntax initializer })
            {
                var index = initializer.ArgumentList.Arguments.IndexOf(argument);
                return InferTypeInConstructorInitializer(initializer, index, argument);
            }

            if (argument is { Parent.Parent: InvocationExpressionSyntax invocation })
            {
                var index = invocation.ArgumentList.Arguments.IndexOf(argument);
                return InferTypeInInvocationExpression(invocation, index, argument);
            }

            if (argument is { Parent.Parent: BaseObjectCreationExpressionSyntax creation })
            {
                // new Outer(Goo());
                //
                // new Outer(a: Goo());
                //
                // etc.
                var index = creation.ArgumentList.Arguments.IndexOf(argument);
                return InferTypeInObjectCreationExpression(creation, index, argument);
            }

            if (argument is { Parent.Parent: PrimaryConstructorBaseTypeSyntax primaryConstructorBaseType })
            {
                // class C() : Base(Goo());
                var index = primaryConstructorBaseType.ArgumentList.Arguments.IndexOf(argument);
                return InferTypeInPrimaryConstructorBaseType(primaryConstructorBaseType, index, argument);
            }

            if (argument is { Parent.Parent: ElementAccessExpressionSyntax elementAccess })
            {
                // Outer[Goo()];
                //
                // Outer[a: Goo()];
                //
                // etc.
                var index = elementAccess.ArgumentList.Arguments.IndexOf(argument);
                return InferTypeInElementAccessExpression(elementAccess, index, argument);
            }

            if (argument is { Parent: TupleExpressionSyntax tupleExpression })
            {
                return InferTypeInTupleExpression(tupleExpression, argument);
            }

            if (argument.Parent.IsParentKind(SyntaxKind.ImplicitElementAccess) &&
                argument.Parent.Parent.IsParentKind(SyntaxKind.SimpleAssignmentExpression) &&
                argument.Parent.Parent.Parent.IsParentKind(SyntaxKind.ObjectInitializerExpression) &&
                argument.Parent.Parent.Parent.Parent?.Parent is BaseObjectCreationExpressionSyntax objectCreation)
            {
                var types = GetTypes(objectCreation).Select(t => t.InferredType);

                if (types.Any(t => t is INamedTypeSymbol))
                {
                    return types.OfType<INamedTypeSymbol>().SelectMany(t =>
                        GetCollectionElementType(t));
                }
            }

            return [];
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInTupleExpression(
            TupleExpressionSyntax tupleExpression, SyntaxToken previousToken)
        {
            if (previousToken == tupleExpression.OpenParenToken)
                return InferTypeInTupleExpression(tupleExpression, tupleExpression.Arguments[0]);

            if (previousToken.IsKind(SyntaxKind.CommaToken))
            {
                var argsAndCommas = tupleExpression.Arguments.GetWithSeparators();
                var commaIndex = argsAndCommas.IndexOf(previousToken);
                return InferTypeInTupleExpression(tupleExpression, (ArgumentSyntax)argsAndCommas[commaIndex + 1]);
            }

            return [];
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInTupleExpression(
            TupleExpressionSyntax tupleExpression, ArgumentSyntax argument)
        {
            var index = tupleExpression.Arguments.IndexOf(argument);
            var parentTypes = InferTypes(tupleExpression);

            return parentTypes.Select(typeInfo => typeInfo.InferredType)
                              .OfType<INamedTypeSymbol>()
                              .Where(namedType => namedType.IsTupleType && index < namedType.TupleElements.Length)
                              .Select(tupleType => new TypeInferenceInfo(tupleType.TupleElements[index].Type));
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInAttributeArgument(AttributeArgumentSyntax argument, SyntaxToken? previousToken = null)
        {
            if (previousToken.HasValue)
            {
                // If we have a position, then it must be after the colon or equals in an argument.
                if (argument.NameColon == null || argument.NameColon.ColonToken != previousToken || argument.NameEquals.EqualsToken != previousToken)
                    return [];
            }

            if (argument.Parent != null)
            {
                if (argument.Parent.Parent is AttributeSyntax attribute)
                {
                    var index = attribute.ArgumentList.Arguments.IndexOf(argument);
                    return InferTypeInAttribute(attribute, index, argument);
                }
            }

            return [];
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInConstructorInitializer(ConstructorInitializerSyntax initializer, int index, ArgumentSyntax argument = null)
        {
            var info = SemanticModel.GetSymbolInfo(initializer, CancellationToken);
            var methods = info.GetBestOrAllSymbols().OfType<IMethodSymbol>();
            return InferTypeInArgument(index, methods, argument, parentInvocationExpressionToTypeInfer: null);
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInObjectCreationExpression(BaseObjectCreationExpressionSyntax expression, SyntaxToken previousToken)
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
                previousToken.GetPreviousToken().Kind() is SyntaxKind.EqualsToken or SyntaxKind.OpenParenToken or SyntaxKind.CommaToken)
            {
                return InferTypes(previousToken.SpanStart);
            }

            return InferTypes(expression);
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInObjectCreationExpression(BaseObjectCreationExpressionSyntax creation, int index, ArgumentSyntax argumentOpt = null)
        {
            var info = SemanticModel.GetTypeInfo(creation, CancellationToken);

            if (info.Type is not INamedTypeSymbol type)
                return [];

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

        private IEnumerable<TypeInferenceInfo> InferTypeInPrimaryConstructorBaseType(
            PrimaryConstructorBaseTypeSyntax primaryConstructorBaseType, int index, ArgumentSyntax argumentOpt = null)
        {
            var info = SemanticModel.GetTypeInfo(primaryConstructorBaseType.Type, CancellationToken);

            if (info.Type is not INamedTypeSymbol type)
                return [];

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

            // 1. Overload resolution (see DevDiv 611477) in certain extension method cases
            //    can result in GetSymbolInfo returning nothing. 
            // 2. when trying to infer the type of the first argument, it's possible that nothing corresponding to
            //    the argument is typed and there exists an overload takes 0 argument as a viable match.
            // In one of these cases, get the method group info, which is what signature help already does.
            if (info.Symbol == null ||
                argumentOpt == null && info.Symbol is IMethodSymbol method && method.Parameters.All(p => p.IsOptional || p.IsParams))
            {
                var memberGroupMethods =
                    SemanticModel.GetMemberGroup(invocation.Expression, CancellationToken)
                                 .OfType<IMethodSymbol>();

                methods = methods.Concat(memberGroupMethods).Distinct().ToList();
            }

            // Special case: if this is an argument in Enum.HasFlag, infer the Enum type that we're invoking into,
            // as otherwise we infer "Enum" which isn't useful
            if (methods.Any(IsEnumHasFlag))
            {
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    var typeInfo = SemanticModel.GetTypeInfo(memberAccess.Expression, CancellationToken);

                    if (typeInfo.Type != null && typeInfo.Type.IsEnumType())
                    {
                        return CreateResult(typeInfo.Type);
                    }
                }
            }

            return InferTypeInArgument(index, methods, argumentOpt, invocation);
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInArgumentList(ArgumentListSyntax argumentList, SyntaxToken previousToken)
        {
            // Has to follow the ( or a ,
            if (previousToken != argumentList.OpenParenToken && previousToken.Kind() != SyntaxKind.CommaToken)
                return [];

            switch (argumentList.Parent)
            {
                case InvocationExpressionSyntax invocation:
                    {
                        var index = GetArgumentListIndex(argumentList, previousToken);
                        return InferTypeInInvocationExpression(invocation, index);
                    }

                case BaseObjectCreationExpressionSyntax objectCreation:
                    {
                        var index = GetArgumentListIndex(argumentList, previousToken);
                        return InferTypeInObjectCreationExpression(objectCreation, index);
                    }

                case ConstructorInitializerSyntax constructorInitializer:
                    {
                        var index = GetArgumentListIndex(argumentList, previousToken);
                        return InferTypeInConstructorInitializer(constructorInitializer, index);
                    }
            }

            return [];
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInAttributeArgumentList(AttributeArgumentListSyntax attributeArgumentList, SyntaxToken previousToken)
        {
            // Has to follow the ( or a ,
            if (previousToken != attributeArgumentList.OpenParenToken && previousToken.Kind() != SyntaxKind.CommaToken)
                return [];

            if (attributeArgumentList.Parent is AttributeSyntax attribute)
            {
                var index = GetArgumentListIndex(attributeArgumentList, previousToken);
                return InferTypeInAttribute(attribute, index);
            }

            return [];
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
                        InferTypeInArgument(index, [i.Parameters], argumentOpt));
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
            => InferTypeInAttributeArgument(index, methods.SelectAsArray(m => m.Parameters), argumentOpt);

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
                    invocationTypes.Any(t => Compilation.ClassifyConversion(m.ReturnType, t).IsImplicit)).ToList();

                // If we filtered down to nothing, then just fall back to the instantiated list.
                // this is a best effort after all.
                methods = filteredMethods.Any() ? filteredMethods : instantiatedMethods;
            }

            return InferTypeInArgument(index, methods.SelectAsArray(m => m.Parameters), argumentOpt);
        }

        private static IMethodSymbol Instantiate(IMethodSymbol method, IList<ITypeSymbol> invocationTypes)
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
            if (method.TypeArguments.Any(static t => t.Kind == SymbolKind.ErrorType))
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
            return method.ConstructedFrom.Construct(typeArguments);
        }

        private static Dictionary<ITypeParameterSymbol, ITypeSymbol> DetermineTypeParameterMapping(ITypeSymbol inferredType, ITypeSymbol returnType)
        {
            var result = new Dictionary<ITypeParameterSymbol, ITypeSymbol>();
            DetermineTypeParameterMapping(inferredType, returnType, result);
            return result;
        }

        private static void DetermineTypeParameterMapping(ITypeSymbol inferredType, ITypeSymbol returnType, Dictionary<ITypeParameterSymbol, ITypeSymbol> result)
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
            ImmutableArray<ImmutableArray<IParameterSymbol>> parameterizedSymbols,
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

        private static IEnumerable<TypeInferenceInfo> InferTypeInArgument(
            int index,
            ImmutableArray<ImmutableArray<IParameterSymbol>> parameterizedSymbols,
            ArgumentSyntax argumentOpt)
        {
            // Prefer parameter lists that match the original number of arguments passed.
            using var _1 = ArrayBuilder<ImmutableArray<IParameterSymbol>>.GetInstance(out var parameterListsWithMatchingCount);
            using var _2 = ArrayBuilder<ImmutableArray<IParameterSymbol>>.GetInstance(out var parameterListsWithoutMatchingCount);

            var argumentCount = argumentOpt?.Parent is BaseArgumentListSyntax baseArgumentList ? baseArgumentList.Arguments.Count : -1;
            foreach (var parameterList in parameterizedSymbols)
            {
                if (argumentCount == -1)
                {
                    // don't have a known argument count.  Just add this all to one of the lists.
                    parameterListsWithMatchingCount.Add(parameterList);
                }
                else
                {
                    var minParameterCount = parameterList.Count(p => !p.IsParams && !p.IsOptional);
                    var maxParameterCount = parameterList.Any(p => p.IsParams) ? int.MaxValue : parameterList.Length;
                    var list = argumentCount >= minParameterCount && argumentCount <= maxParameterCount
                        ? parameterListsWithMatchingCount
                        : parameterListsWithoutMatchingCount;

                    list.Add(parameterList);
                }
            }

            var name = argumentOpt != null && argumentOpt.NameColon != null ? argumentOpt.NameColon.Name.Identifier.ValueText : null;
            var refKind = argumentOpt.GetRefKind();
            return InferTypeInArgument(index, parameterListsWithMatchingCount.ToImmutable(), name, refKind).Concat(
                InferTypeInArgument(index, parameterListsWithoutMatchingCount.ToImmutable(), name, refKind));
        }

        private static IEnumerable<TypeInferenceInfo> InferTypeInArgument(
            int index,
            ImmutableArray<ImmutableArray<IParameterSymbol>> parameterizedSymbols,
            string name,
            RefKind refKind)
        {
            // If the callsite has a named argument, then try to find a method overload that has a
            // parameter with that name.  If we can find one, then return the type of that one.
            if (name != null)
            {
                var matchingNameParameters = parameterizedSymbols.SelectMany(m => m)
                                                                 .Where(p => p.Name == name)
                                                                 .Select(p => new TypeInferenceInfo(p.Type, p.IsParams));

                return matchingNameParameters;
            }

            using var _1 = ArrayBuilder<TypeInferenceInfo>.GetInstance(out var allParameters);
            using var _2 = ArrayBuilder<TypeInferenceInfo>.GetInstance(out var matchingRefParameters);

            foreach (var parameterSet in parameterizedSymbols)
            {
                if (index < parameterSet.Length)
                {
                    var parameter = parameterSet[index];
                    var info = new TypeInferenceInfo(parameter.Type, parameter.IsParams);
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

        private IEnumerable<TypeInferenceInfo> InferTypeInArrayCreationExpression(
            ArrayCreationExpressionSyntax arrayCreationExpression, SyntaxToken? previousToken = null)
        {
            if (previousToken.HasValue && previousToken.Value != arrayCreationExpression.NewKeyword)
            {
                // Has to follow the 'new' keyword. 
                return [];
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
                return [];

            return CreateResult(this.Compilation.GetSpecialType(SpecialType.System_Int32));
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInArrayType(ArrayTypeSyntax arrayType, SyntaxToken? previousToken = null)
        {
            if (previousToken.HasValue)
            {
                // TODO(cyrusn): NYI.  Handle this appropriately if we need to.
                return [];
            }

            // Bind the array type, then unwrap whatever we get back based on the number of rank
            // specifiers we see.
            var currentTypes = InferTypes(arrayType);
            for (var i = 0; i < arrayType.RankSpecifiers.Count; i++)
            {
                currentTypes = currentTypes.Select(t => t.InferredType).OfType<IArrayTypeSymbol>()
                                           .SelectAsArray(a => new TypeInferenceInfo(a.ElementType));
            }

            return currentTypes;
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInAttribute()
            => CreateResult(this.Compilation.AttributeType());

        private IEnumerable<TypeInferenceInfo> InferTypeInAttributeDeclaration(AttributeListSyntax attributeDeclaration, SyntaxToken? previousToken)
        {
            // If we have a position, then it has to be after the open bracket.
            if (previousToken.HasValue && previousToken.Value != attributeDeclaration.OpenBracketToken)
                return [];

            return CreateResult(this.Compilation.AttributeType());
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInAttributeTargetSpecifier(
            AttributeTargetSpecifierSyntax attributeTargetSpecifier,
            SyntaxToken? previousToken)
        {
            // If we have a position, then it has to be after the colon.
            if (previousToken.HasValue && previousToken.Value != attributeTargetSpecifier.ColonToken)
                return [];

            return CreateResult(this.Compilation.AttributeType());
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInBracketedArgumentList(BracketedArgumentListSyntax bracketedArgumentList, SyntaxToken previousToken)
        {
            // Has to follow the [ or a ,
            if (previousToken != bracketedArgumentList.OpenBracketToken && previousToken.Kind() != SyntaxKind.CommaToken)
                return [];

            if (bracketedArgumentList.Parent is ElementAccessExpressionSyntax elementAccess)
            {
                var index = GetArgumentListIndex(bracketedArgumentList, previousToken);
                return InferTypeInElementAccessExpression(
                    elementAccess, index);
            }

            return [];
        }

        private static int GetArgumentListIndex(BaseArgumentListSyntax argumentList, SyntaxToken previousToken)
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

        private static int GetArgumentListIndex(AttributeArgumentListSyntax attributeArgumentList, SyntaxToken previousToken)
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
                case SyntaxKind.GreaterThanGreaterThanGreaterThanToken:
                case SyntaxKind.LessThanLessThanEqualsToken:
                case SyntaxKind.GreaterThanGreaterThanEqualsToken:
                case SyntaxKind.GreaterThanGreaterThanGreaterThanEqualsToken:

                    if (onRightOfToken)
                    {
                        // x << Goo(), x >> Goo(), x >>> Goo(), x <<= Goo(), x >>= Goo(), x >>>= Goo()
                        return CreateResult(this.Compilation.GetSpecialType(SpecialType.System_Int32));
                    }

                    break;
            }

            // Infer operands of && and || as bool regardless of the other operand.
            if (operatorToken.Kind() is SyntaxKind.AmpersandAmpersandToken or
                SyntaxKind.BarBarToken)
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
                if (binop is not AssignmentExpressionSyntax)
                {
                    otherSideTypes = otherSideTypes.Where(t => !t.InferredType.IsDelegateType());
                }

                return otherSideTypes;
            }

            // For &, &=, |, |=, ^, and ^=, since we couldn't infer the type of either side, 
            // try to infer the type of the entire binary expression.
            if (operatorToken.Kind() is SyntaxKind.AmpersandToken or
                SyntaxKind.AmpersandEqualsToken or
                SyntaxKind.BarToken or
                SyntaxKind.BarEqualsToken or
                SyntaxKind.CaretToken or
                SyntaxKind.CaretEqualsToken)
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
                if (parentTypes.Any(static parentType => parentType.InferredType.SpecialType == SpecialType.System_String || parentType.InferredType.TypeKind == TypeKind.Delegate))
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
                case SyntaxKind.GreaterThanGreaterThanGreaterThanToken:
                case SyntaxKind.LessThanLessThanEqualsToken:
                case SyntaxKind.GreaterThanGreaterThanEqualsToken:
                case SyntaxKind.GreaterThanGreaterThanGreaterThanEqualsToken:
                    return CreateResult(this.Compilation.GetSpecialType(SpecialType.System_Int32));

                case SyntaxKind.BarEqualsToken:
                case SyntaxKind.AmpersandEqualsToken:
                    // NOTE(cyrusn): |= and &= can be used for both ints and bools  However, in the
                    // case where there isn't enough information to determine which the user wanted,
                    // I'm just defaulting to bool based on personal preference.
                    return CreateResult(SpecialType.System_Boolean);
            }

            return [];
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInCastExpression(CastExpressionSyntax castExpression, ExpressionSyntax expressionOpt = null, SyntaxToken? previousToken = null)
        {
            if (expressionOpt != null && castExpression.Expression != expressionOpt)
                return [];

            // If we have a position, then it has to be after the close paren.
            if (previousToken.HasValue && previousToken.Value != castExpression.CloseParenToken)
                return [];

            return this.GetTypes(castExpression.Type);
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInCatchDeclaration(CatchDeclarationSyntax catchDeclaration, SyntaxToken? previousToken = null)
        {
            // If we have a position, it has to be after "catch("
            if (previousToken.HasValue && previousToken.Value != catchDeclaration.OpenParenToken)
                return [];

            return CreateResult(this.Compilation.ExceptionType());
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInCatchFilterClause(CatchFilterClauseSyntax catchFilterClause, SyntaxToken? previousToken = null)
        {
            // If we have a position, it has to be after "if ("
            if (previousToken.HasValue && previousToken.Value != catchFilterClause.OpenParenToken)
                return [];

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
                return leftTypes.Select(x => x.InferredType.IsNullable(out var underlying)
                    ? new TypeInferenceInfo(underlying) // nullableExpr ?? Goo()
                    : x); // normalExpr ?? Goo() 
            }

            var rightTypes = GetTypes(coalesceExpression.Right);
            if (!rightTypes.Any())
                return CreateResult(SpecialType.System_Object, NullableAnnotation.Annotated);

            // Goo() ?? ""
            return rightTypes.Select(x => new TypeInferenceInfo(MakeNullable(x.InferredType, this.Compilation)));

            static ITypeSymbol MakeNullable(ITypeSymbol symbol, Compilation compilation)
            {
                if (symbol.IsErrorType())
                {
                    // We could be smart and infer this as an ErrorType?, but in the #nullable disable case we don't know if this is intended to be
                    // a struct (where the question mark is legal) or a class (where it isn't). We'll thus avoid sticking question marks in this case.
                    // https://github.com/dotnet/roslyn/issues/37852 tracks fixing this is a much fancier way.
                    return symbol;
                }
                else if (symbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                {
                    // We already have something nullable.  Don't wrap in another nullable layer.
                    return symbol;
                }
                else if (symbol.IsValueType)
                {
                    return compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(symbol);
                }
                else if (symbol.IsReferenceType)
                {
                    return symbol.WithNullableAnnotation(NullableAnnotation.Annotated);
                }
                else // it's neither a value nor reference type, so is an unconstrained generic
                {
                    return symbol;
                }
            }
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInConditionalAccessExpression(ConditionalAccessExpressionSyntax expression)
            => InferTypes(expression);

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
                                       : [];

            return otherTypes.IsEmpty()
                       ? InferTypes(conditional)
                       : otherTypes;
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInDefaultExpression(DefaultExpressionSyntax defaultExpression)
            => InferTypes(defaultExpression);

        private IEnumerable<TypeInferenceInfo> InferTypeInDoStatement(DoStatementSyntax doStatement, SyntaxToken? previousToken = null)
        {
            // If we have a position, we need to be after "do { } while("
            if (previousToken.HasValue && previousToken.Value != doStatement.OpenParenToken)
                return [];

            return CreateResult(SpecialType.System_Boolean);
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInEqualsValueClause(EqualsValueClauseSyntax equalsValue, SyntaxToken? previousToken = null)
        {
            // If we have a position, it has to be after the =
            if (previousToken.HasValue && previousToken.Value != equalsValue.EqualsToken)
                return [];

            if (equalsValue?.Parent is VariableDeclaratorSyntax varDecl)
                return InferTypeInVariableDeclarator(varDecl);

            if (equalsValue?.Parent is PropertyDeclarationSyntax propertyDecl)
                return InferTypeInPropertyDeclaration(propertyDecl);

            if (equalsValue.IsParentKind(SyntaxKind.Parameter) &&
                SemanticModel.GetDeclaredSymbol(equalsValue.Parent, CancellationToken) is IParameterSymbol parameter)
            {
                return CreateResult(parameter.Type);
            }

            return [];
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInPropertyDeclaration(PropertyDeclarationSyntax propertyDeclaration)
        {
            Debug.Assert(propertyDeclaration?.Type != null, "Property type should never be null");

            var typeInfo = SemanticModel.GetTypeInfo(propertyDeclaration.Type);
            return CreateResult(typeInfo.Type);
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInExpressionStatement(SyntaxToken? previousToken = null)
        {
            // If we're position based, then that means we're after the semicolon.  In this case
            // we don't have any sort of type to infer.
            if (previousToken.HasValue)
                return [];

            return CreateResult(SpecialType.System_Void);
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInForEachStatement(ForEachStatementSyntax forEachStatementSyntax, ExpressionSyntax expressionOpt = null, SyntaxToken? previousToken = null)
        {
            // If we have a position, then we have to be after "foreach(... in"
            if (previousToken.HasValue && previousToken.Value != forEachStatementSyntax.InKeyword)
                return [];

            if (expressionOpt != null && expressionOpt != forEachStatementSyntax.Expression)
                return [];

            var isAsync = forEachStatementSyntax.AwaitKeyword != default;
            var enumerableType = !isAsync
                ? this.Compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T)
                : this.Compilation.GetTypeByMetadataName(typeof(IAsyncEnumerable<>).FullName);

            enumerableType ??= this.Compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T);

            // foreach (int v = Goo())
            var variableTypes = GetTypes(forEachStatementSyntax.Type);
            var typeInferenceInfos = variableTypes.ToImmutableArray();

            if (!typeInferenceInfos.IsEmpty)
                return typeInferenceInfos.Select(v => new TypeInferenceInfo(enumerableType.Construct(v.InferredType)));

            var objectType = Compilation.GetSpecialType(SpecialType.System_Object);
            var results = CreateResult(enumerableType.Construct(objectType)).ToList();

            if (!isAsync)
            {
                var nonGenericEnumerable = Compilation.GetSpecialType(SpecialType.System_Collections_IEnumerable);
                results.AddRange(CreateResult(nonGenericEnumerable));
            }

            return results;
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInForStatement(ForStatementSyntax forStatement, ExpressionSyntax expressionOpt = null, SyntaxToken? previousToken = null)
        {
            // If we have a position, it has to be after "for(...;"
            if (previousToken.HasValue && previousToken.Value != forStatement.FirstSemicolonToken)
                return [];

            if (expressionOpt != null && forStatement.Condition != expressionOpt)
                return [];

            return CreateResult(SpecialType.System_Boolean);
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInIfStatement(IfStatementSyntax ifStatement, SyntaxToken? previousToken = null)
        {
            // If we have a position, we have to be after the "if("
            if (previousToken.HasValue && previousToken.Value != ifStatement.OpenParenToken)
                return [];

            return CreateResult(SpecialType.System_Boolean);
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInImplicitArrayCreation(ImplicitArrayCreationExpressionSyntax implicitArray)
            => InferTypes(implicitArray.SpanStart);

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

            if (initializerExpression?.Parent is ImplicitArrayCreationExpressionSyntax implicitArray)
            {
                // new[] { 1, x }

                // First, try to infer the type that the array should be.  If we can infer an
                // appropriate array type, then use the element type of the array.  Otherwise,
                // look at the siblings of this expression and use their type instead.

                var arrayTypes = this.InferTypes(implicitArray);
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
            else if (initializerExpression?.Parent is EqualsValueClauseSyntax equalsValueClause)
            {
                // = { Goo() }
                var types = InferTypeInEqualsValueClause(equalsValueClause).Select(t => t.InferredType);

                if (types.Any(t => t is IArrayTypeSymbol))
                {
                    return types.OfType<IArrayTypeSymbol>().Select(t => new TypeInferenceInfo(t.ElementType));
                }
            }
            else if (initializerExpression?.Parent is ArrayCreationExpressionSyntax arrayCreation)
            {
                // new int[] { Goo() } 
                var types = GetTypes(arrayCreation).Select(t => t.InferredType);

                if (types.Any(t => t is IArrayTypeSymbol))
                {
                    return types.OfType<IArrayTypeSymbol>().Select(t => new TypeInferenceInfo(t.ElementType));
                }
            }
            else if (initializerExpression?.Parent is ObjectCreationExpressionSyntax objectCreation)
            {
                // new List<T> { Goo() } 
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
                    var addMethodParameterTypes = addMethodSymbols.Select(m => ((IMethodSymbol)m).Parameters[0]).Select(p => new TypeInferenceInfo(p.Type));
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

            return [];
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
            PropertyPatternClauseSyntax propertySubpattern)
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
                subpattern.ExpressionColon != null)
            {
                using var result = TemporaryArray<TypeInferenceInfo>.Empty;

                foreach (var symbol in this.SemanticModel.GetSymbolInfo(subpattern.ExpressionColon.Expression).GetAllSymbols())
                {
                    switch (symbol)
                    {
                        case IFieldSymbol field:
                            result.Add(new TypeInferenceInfo(field.Type));
                            break;
                        case IPropertySymbol property:
                            result.Add(new TypeInferenceInfo(property.Type));
                            break;
                    }
                }

                return result.ToImmutableAndClear();
            }

            return [];
        }

        private IEnumerable<TypeInferenceInfo> InferTypeForSingleVariableDesignation(SingleVariableDesignationSyntax singleVariableDesignation)
        {
            if (singleVariableDesignation.Parent is DeclarationPatternSyntax declarationPattern)
            {
                // c is Color.Red or $$
                // "or" is not parsed as part of a BinaryPattern until the right hand side
                // is written. By making sure, the identifier
                // is "or" or "and", we can assume a BinaryPattern is upcoming.
                var identifier = singleVariableDesignation.Identifier;
                if (identifier.HasMatchingText(SyntaxKind.OrKeyword) ||
                    identifier.HasMatchingText(SyntaxKind.AndKeyword))
                {
                    return GetPatternTypes(declarationPattern);
                }
            }

            return [];
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

            return [];
        }

        private IEnumerable<TypeInferenceInfo> GetPatternTypes(PatternSyntax pattern)
        {
            return pattern switch
            {
                ConstantPatternSyntax constantPattern => GetTypes(constantPattern.Expression),
                RecursivePatternSyntax recursivePattern => GetTypesForRecursivePattern(recursivePattern),
                _ when SemanticModel.GetOperation(pattern, CancellationToken) is IPatternOperation patternOperation =>
                    // In cases like this: c is Color.Green or $$
                    // "pattern" is a DeclarationPatternSyntax and Color.Green is assumed to be the narrowed type.
                    // If the narrowed type can not be resolved, we fall back to the input type of the pattern, which
                    // is a good default for any related case.
                    CreateResult(patternOperation.NarrowedType.IsErrorType()
                        ? patternOperation.InputType
                        : patternOperation.NarrowedType),
                _ => [],
            };
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
                            return [];

                        elementTypesBuilder.Add(patternType.InferredType);
                    }

                    // Pass the nullable annotations explicitly to work around https://github.com/dotnet/roslyn/issues/40105
                    var elementTypes = elementTypesBuilder.ToImmutableAndFree();
                    var type = Compilation.CreateTupleTypeSymbol(
                        elementTypes, elementNamesBuilder.ToImmutableAndFree(), elementNullableAnnotations: GetNullableAnnotations(elementTypes));
                    return CreateResult(type);
                }
            }

            return [];
        }

        private static ImmutableArray<NullableAnnotation> GetNullableAnnotations(ImmutableArray<ITypeSymbol> elementTypes)
            => elementTypes.SelectAsArray(e => e.NullableAnnotation);

        private IEnumerable<TypeInferenceInfo> InferTypeInLockStatement(LockStatementSyntax lockStatement, SyntaxToken? previousToken = null)
        {
            // If we're position based, then we have to be after the "lock("
            if (previousToken.HasValue && previousToken.Value != lockStatement.OpenParenToken)
                return [];

            return CreateResult(SpecialType.System_Object);
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInLambdaExpression(LambdaExpressionSyntax lambdaExpression, SyntaxToken? previousToken = null)
        {
            // If we have a position, it has to be after the lambda arrow.
            if (previousToken.HasValue && previousToken.Value != lambdaExpression.ArrowToken)
                return [];

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
                    return [new TypeInferenceInfo(UnwrapTaskLike(invoke.ReturnType, isAsync))];
                }
            }

            return [];
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInMemberDeclarator(AnonymousObjectMemberDeclaratorSyntax memberDeclarator, SyntaxToken? previousTokenOpt = null)
        {
            if (memberDeclarator.NameEquals != null && memberDeclarator.Parent is AnonymousObjectCreationExpressionSyntax)
            {
                // If we're position based, then we have to be after the = 
                if (previousTokenOpt.HasValue && previousTokenOpt.Value != memberDeclarator.NameEquals.EqualsToken)
                    return [];

                var types = InferTypes((AnonymousObjectCreationExpressionSyntax)memberDeclarator.Parent);

                return types.Where(t => t.InferredType.IsAnonymousType())
                    .SelectMany(t => t.InferredType.GetValidAnonymousTypeProperties()
                        .Where(p => p.Name == memberDeclarator.NameEquals.Name.Identifier.ValueText)
                        .Select(p => new TypeInferenceInfo(p.Type)));
            }

            return [];
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInNameColon(NameColonSyntax nameColon, SyntaxToken previousToken)
        {
            if (previousToken != nameColon.ColonToken)
            {
                // Must follow the colon token.
                return [];
            }

            return nameColon.Parent switch
            {
                ArgumentSyntax argumentSyntax => InferTypeInArgument(argumentSyntax),
                SubpatternSyntax subPattern => InferTypeInSubpattern(subPattern, subPattern.Pattern),
                _ => [],
            };
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInExpressionColon(ExpressionColonSyntax expressionColon, SyntaxToken previousToken)
        {
            if (previousToken != expressionColon.ColonToken)
            {
                // Must follow the colon token.
                return [];
            }

            return expressionColon.Parent switch
            {
                SubpatternSyntax subPattern => InferTypeInSubpattern(subPattern, subPattern.Pattern),
                _ => [],
            };
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
                    return [];

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
            if (name.Equals(nameof(Task<>.ConfigureAwait)) &&
                memberAccessExpression?.Parent is InvocationExpressionSyntax invocation &&
                memberAccessExpression.Parent.IsParentKind(SyntaxKind.AwaitExpression))
            {
                return InferTypes(invocation);
            }
            else if (name.Equals(nameof(Task<>.ContinueWith)))
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
                if (ienumerableType != null && memberAccessExpression.IsParentKind(SyntaxKind.InvocationExpression, out invocation))
                {
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

                            if (IsUnusableType(typeArg) && argumentExpression is LambdaExpressionSyntax lambdaExpression)
                            {
                                typeArg = InferTypeForFirstParameterOfLambda(lambdaExpression) ?? this.Compilation.ObjectType;
                            }

                            return CreateResult(ienumerableType.Construct(typeArg));
                        }
                    }
                }
            }

            return [];
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
            if (node is IdentifierNameSyntax identifierName)
            {
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

            return [];
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInExpressionColon(ExpressionColonSyntax expressionColon)
        {
            if (expressionColon.Parent is SubpatternSyntax subpattern)
            {
                return GetPatternTypes(subpattern.Pattern);
            }

            return [];
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

            return [];
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInPostfixUnaryExpression(PostfixUnaryExpressionSyntax postfixUnaryExpressionSyntax, SyntaxToken? previousToken = null)
        {
            // If we're after a postfix ++ or -- then we can't infer anything.
            if (previousToken.HasValue)
                return [];

            switch (postfixUnaryExpressionSyntax.Kind())
            {
                case SyntaxKind.PostDecrementExpression:
                case SyntaxKind.PostIncrementExpression:
                    return CreateResult(this.Compilation.GetSpecialType(SpecialType.System_Int32));
            }

            return [];
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

                case SyntaxKind.AddressOfExpression:
                    return InferTypeInAddressOfExpression(prefixUnaryExpression);
            }

            return [];
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInAddressOfExpression(PrefixUnaryExpressionSyntax prefixUnaryExpression)
        {
            foreach (var inferredType in InferTypes(prefixUnaryExpression))
            {
                if (inferredType.InferredType is IPointerTypeSymbol pointerType)
                {
                    // If the code is `int* x = &...` then we want to infer `int` for `...`
                    yield return new TypeInferenceInfo(pointerType.PointedAtType);
                }
                else if (inferredType.InferredType is IFunctionPointerTypeSymbol functionPointerType)
                {
                    // If the code is `delegate*<int, void> x = &...` then we want to infer a signature of `void
                    // M(int)` here (which we encode as Action/Func as necessary). Higher layers (like
                    // generate-method), then can figure out what to do with that signature.
                    yield return new TypeInferenceInfo(functionPointerType.Signature.ConvertToType(this.Compilation));
                }
            }
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
                return [];

            if (!types.Any())
            {
                return CreateResult(task);
            }

            return types.Select(t => t.InferredType.SpecialType == SpecialType.System_Void ? new TypeInferenceInfo(task) : new TypeInferenceInfo(taskOfT.Construct(t.InferredType)));
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInYieldStatement(YieldStatementSyntax yieldStatement, SyntaxToken? previousToken = null)
        {
            // If we are position based, then we have to be after the return keyword
            if (previousToken.HasValue && (previousToken.Value != yieldStatement.ReturnOrBreakKeyword || yieldStatement.ReturnOrBreakKeyword.IsKind(SyntaxKind.BreakKeyword)))
                return [];

            var declaration = yieldStatement.FirstAncestorOrSelf<SyntaxNode>(n => n.IsReturnableConstruct());
            var memberSymbol = GetDeclaredMemberSymbolFromOriginalSemanticModel(declaration);

            var memberType = memberSymbol.GetMemberType();

            // We don't care what the type is, as long as it has 1 type argument. This will work for IEnumerable, IEnumerator,
            // IAsyncEnumerable, IAsyncEnumerator and it's also good for error recovery in case there is a missing using.
            return memberType is INamedTypeSymbol namedType && namedType.TypeArguments.Length == 1
                ? [new TypeInferenceInfo(namedType.TypeArguments[0])]
                : [];
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInRefExpression(RefExpressionSyntax refExpression)
            => InferTypes(refExpression);

        private ITypeSymbol UnwrapTaskLike(ITypeSymbol type, bool isAsync)
        {
            if (isAsync)
            {
                if (type.OriginalDefinition.Equals(this.Compilation.TaskOfTType()) || type.OriginalDefinition.Equals(this.Compilation.ValueTaskOfTType()))
                {
                    var namedTypeSymbol = (INamedTypeSymbol)type;
                    return namedTypeSymbol.TypeArguments[0];
                }

                if (type.OriginalDefinition.Equals(this.Compilation.TaskType()) || type.OriginalDefinition.Equals(this.Compilation.ValueTaskType()))
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
                return [];

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
                ? [new TypeInferenceInfo(UnwrapTaskLike(type, isAsync))]
                : [];
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

        private IEnumerable<TypeInferenceInfo> InferTypeInSwitchExpressionArm(
            SwitchExpressionArmSyntax arm)
        {
            if (arm.Parent is SwitchExpressionSyntax switchExpression)
            {
                // see if we can figure out an appropriate type from a prior/next arm.
                var armIndex = switchExpression.Arms.IndexOf(arm);
                if (armIndex > 0)
                {
                    var previousArm = switchExpression.Arms[armIndex - 1];
                    var priorArmTypes = GetTypes(previousArm.Expression, objectAsDefault: false);
                    if (priorArmTypes.Any())
                        return priorArmTypes;
                }

                if (armIndex < switchExpression.Arms.Count - 1)
                {
                    var nextArm = switchExpression.Arms[armIndex + 1];
                    var priorArmTypes = GetTypes(nextArm.Expression, objectAsDefault: false);
                    if (priorArmTypes.Any())
                        return priorArmTypes;
                }

                // if a prior arm gave us nothing useful, or we're the first arm, then try to infer looking at
                // what type gets inferred for the switch expression itself.
                return InferTypes(switchExpression);
            }

            return [];
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInSwitchExpression(SwitchExpressionSyntax switchExpression, SyntaxToken token)
        {
            if (token.Kind() is SyntaxKind.OpenBraceToken or SyntaxKind.CommaToken)
                return GetTypes(switchExpression.GoverningExpression);

            return [];
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInSwitchLabel(
            SwitchLabelSyntax switchLabel, SyntaxToken? previousToken = null)
        {
            if (previousToken.HasValue)
            {
                if (previousToken.Value != switchLabel.Keyword ||
                    switchLabel.Kind() != SyntaxKind.CaseSwitchLabel)
                {
                    return [];
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
                return [];

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
                return [];

            return CreateResult(this.Compilation.ExceptionType());
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInThrowStatement(ThrowStatementSyntax throwStatement, SyntaxToken? previousToken = null)
        {
            // If we have a position, it has to be after the 'throw' keyword.
            if (previousToken.HasValue && previousToken.Value != throwStatement.ThrowKeyword)
                return [];

            return CreateResult(this.Compilation.ExceptionType());
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInUsingStatement(UsingStatementSyntax usingStatement, SyntaxToken? previousToken = null)
        {
            // If we have a position, it has to be after "using("
            if (previousToken.HasValue && previousToken.Value != usingStatement.OpenParenToken)
                return [];

            return CreateResult(SpecialType.System_IDisposable);
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInVariableDeclarator(VariableDeclaratorSyntax variableDeclarator)
        {
            var variableType = variableDeclarator.GetVariableType();
            if (variableType == null)
                return [];

            var symbol = SemanticModel.GetDeclaredSymbol(variableDeclarator);
            if (symbol == null)
                return [];

            var type = symbol.GetSymbolType();
            var types = CreateResult(type).Where(IsUsableTypeFunc);

            if (!variableType.IsVar ||
                variableDeclarator.Parent is not VariableDeclarationSyntax variableDeclaration)
            {
                return types;
            }

            // using (var v = Goo())
            if (variableDeclaration.IsParentKind(SyntaxKind.UsingStatement))
                return CreateResult(SpecialType.System_IDisposable);

            // for (var v = Goo(); ..
            if (variableDeclaration.IsParentKind(SyntaxKind.ForStatement))
                return CreateResult(this.Compilation.GetSpecialType(SpecialType.System_Int32));

            var laterUsageInference = InferTypeBasedOnLaterUsage(symbol, variableDeclaration);
            if (laterUsageInference is not [] and not [{ InferredType.SpecialType: SpecialType.System_Object }])
                return laterUsageInference;

            // Return the types here if they actually bound to a type called 'var'.
            return types.Where(t => t.InferredType.Name == "var");
        }

        private ImmutableArray<TypeInferenceInfo> InferTypeBasedOnLaterUsage(ISymbol symbol, SyntaxNode afterNode)
        {
            // var v = expr.
            // Attempt to see how 'v' is used later in the current scope to determine what to do.
            var container = afterNode.AncestorsAndSelf().FirstOrDefault(a => a is BlockSyntax or SwitchSectionSyntax);
            if (container != null)
            {
                foreach (var descendant in container.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
                {
                    // only look after the variable we're declaring.
                    if (descendant.SpanStart <= afterNode.Span.End)
                        continue;

                    if (descendant.Identifier.ValueText != symbol.Name)
                        continue;

                    // Make sure it's actually a match for this variable.
                    var descendantSymbol = SemanticModel.GetSymbolInfo(descendant, CancellationToken).GetAnySymbol();
                    if (symbol.Equals(descendantSymbol))
                    {
                        // See if we can infer something interesting about this location.
                        var inferredDescendantTypes = InferTypes(descendant, filterUnusable: true);
                        if (inferredDescendantTypes is not [] and not [{ InferredType.SpecialType: SpecialType.System_Object }])
                            return inferredDescendantTypes;
                    }
                }
            }

            return [];
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInVariableComponentAssignment(ExpressionSyntax left)
        {
            if (left is DeclarationExpressionSyntax declExpr)
            {
                // var (x, y) = Expr();
                // Attempt to determine what x and y are based on their future usage.
                if (declExpr.Type.IsVar &&
                    declExpr.Designation is ParenthesizedVariableDesignationSyntax parenthesizedVariableDesignation &&
                    parenthesizedVariableDesignation.Variables.All(v => v is SingleVariableDesignationSyntax { Identifier.ValueText: not "" }))
                {
                    var elementNames = parenthesizedVariableDesignation.Variables.SelectAsArray(v => ((SingleVariableDesignationSyntax)v).Identifier.ValueText);
                    var elementTypes = parenthesizedVariableDesignation.Variables.SelectAsArray(v =>
                    {
                        var designation = (SingleVariableDesignationSyntax)v;

                        var symbol = SemanticModel.GetRequiredDeclaredSymbol(designation, CancellationToken);
                        var inferredFutureUsage = InferTypeBasedOnLaterUsage(symbol, afterNode: left.Parent);
                        return inferredFutureUsage.Length > 0 ? inferredFutureUsage[0].InferredType : Compilation.ObjectType;
                    });

                    return [new TypeInferenceInfo(
                        Compilation.CreateTupleTypeSymbol(elementTypes, elementNames))];
                }

                return GetTypes(declExpr.Type);
            }
            else if (left is TupleExpressionSyntax tupleExpression)
            {
                // We have something of the form:
                //   (int a, int b) = ...
                //
                // This is a deconstruction, and a decent deconstructable type we can infer here is
                // ValueTuple<int,int>.
                var tupleType = GetTupleType(tupleExpression);

                if (tupleType != null)
                    return CreateResult(tupleType);
            }

            return [];
        }

        private ITypeSymbol GetTupleType(
            TupleExpressionSyntax tuple)
        {
            if (!TryGetTupleTypesAndNames(tuple.Arguments, out var elementTypes, out var elementNames))
            {
                return null;
            }

            // Pass the nullable annotations explicitly to work around https://github.com/dotnet/roslyn/issues/40105
            return Compilation.CreateTupleTypeSymbol(elementTypes, elementNames, elementNullableAnnotations: GetNullableAnnotations(elementTypes));
        }

        private bool TryGetTupleTypesAndNames(
            SeparatedSyntaxList<ArgumentSyntax> arguments,
            out ImmutableArray<ITypeSymbol> elementTypes,
            out ImmutableArray<string> elementNames)
        {
            elementTypes = default;
            elementNames = default;

            using var _1 = ArrayBuilder<ITypeSymbol>.GetInstance(out var elementTypesBuilder);
            using var _2 = ArrayBuilder<string>.GetInstance(out var elementNamesBuilder);

            foreach (var arg in arguments)
            {
                var expr = arg.Expression;
                if (expr is DeclarationExpressionSyntax declExpr)
                {
                    AddTypeAndName(declExpr, elementTypesBuilder, elementNamesBuilder);
                }
                else if (expr is TupleExpressionSyntax tupleExpr)
                {
                    AddTypeAndName(tupleExpr, elementTypesBuilder, elementNamesBuilder);
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

        private void AddTypeAndName(
            DeclarationExpressionSyntax declaration,
            ArrayBuilder<ITypeSymbol> elementTypesBuilder,
            ArrayBuilder<string> elementNamesBuilder)
        {
            elementTypesBuilder.Add(GetTypes(declaration.Type).FirstOrDefault().InferredType);

            var designation = declaration.Designation;
            if (designation is SingleVariableDesignationSyntax singleVariable)
            {
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
                return [];

            return [new TypeInferenceInfo(Compilation.GetSpecialType(SpecialType.System_Boolean))];
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInWhileStatement(WhileStatementSyntax whileStatement, SyntaxToken? previousToken = null)
        {
            // If we're position based, then we have to be after the "while("
            if (previousToken.HasValue && previousToken.Value != whileStatement.OpenParenToken)
                return [];

            return CreateResult(SpecialType.System_Boolean);
        }

        private IEnumerable<TypeInferenceInfo> InferTypeInRelationalPattern(RelationalPatternSyntax relationalPattern)
            => InferTypes(relationalPattern);
    }
}
