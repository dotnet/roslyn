// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.AddNamedArguments;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.AddNamedArguments
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpAddNamedArgumentsCodeRefactoringProvider)), Shared]
    internal class CSharpAddNamedArgumentsCodeRefactoringProvider : AbstractAddNamedArgumentsCodeRefactoringProvider<ArgumentSyntax>
    {
        protected override bool TryGetOrSynthesizeNamedArguments(SeparatedSyntaxList<ArgumentSyntax> arguments, ImmutableArray<IParameterSymbol> parameters, out ArgumentSyntax[] namedArguments, out bool hasLiteral)
        {
            hasLiteral = false;

            // Early return when already fixed
            if (arguments[0].NameColon != null)
            {
                namedArguments = default(ArgumentSyntax[]);
                return false;
            }

            var result = ArrayBuilder<ArgumentSyntax>.GetInstance(arguments.Count);
            for (int index = 0; index < arguments.Count; ++index)
            {
                var argument = arguments[index];
                if (argument.NameColon != null)
                {
                    result.Add(argument);
                }
                else
                {
                    var parameter = parameters[index];
                    var namedArgument = argument.WithNameColon(NameColon(parameter.Name));
                    if (parameter.IsParams && arguments.Count - index > 1)
                    {
                        var arrayBuilder = ArrayBuilder<ExpressionSyntax>.GetInstance(arguments.Count - index);
                        for (; index < arguments.Count; ++index)
                        {
                            arrayBuilder.Add(arguments[index].Expression);
                        }

                        var expressions = arrayBuilder.ToArrayAndFree();
                        var initializer = InitializerExpression(SyntaxKind.ArrayInitializerExpression, SeparatedList(expressions));
                        var arrayType = parameter.Type.GenerateTypeSyntax() as ArrayTypeSyntax;
                        var arrayCreation = ArrayCreationExpression(arrayType, initializer);
                        result.Add(namedArgument.WithExpression(arrayCreation).WithTriviaFrom(argument.Expression));
                        break;
                    }
                    else
                    {
                        if (!hasLiteral && IsLiteral(argument))
                        {
                            hasLiteral = true;
                        }

                        // Preserve trivia when argument is ref or out
                        if (argument.RefOrOutKeyword == null)
                        {
                            namedArgument = namedArgument.WithTriviaFrom(argument.Expression);
                        }

                        result.Add(namedArgument);
                    }
                }
            }

            namedArguments = result.ToArrayAndFree();
            return true;
        }

        protected override bool IsCandidate(SyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.InvocationExpression:
                case SyntaxKind.ElementAccessExpression:
                case SyntaxKind.ObjectCreationExpression:
                case SyntaxKind.BaseConstructorInitializer:
                case SyntaxKind.ThisConstructorInitializer:
                    return true;

                case SyntaxKind.ConditionalAccessExpression:
                    var conditional = (ConditionalAccessExpressionSyntax)node;
                    return conditional.WhenNotNull is ElementBindingExpressionSyntax;

                default:
                    return false;
            }
        }

        protected override bool TryGetArguments(SyntaxNode node, SemanticModel semanticModel, out SeparatedSyntaxList<ArgumentSyntax> arguments)
        {
            switch (node.Kind())
            {
                case SyntaxKind.InvocationExpression:
                    {
                        var invocation = (InvocationExpressionSyntax)node;
                        arguments = invocation.ArgumentList.Arguments;
                        return true;
                    }

                case SyntaxKind.ElementAccessExpression:
                    {
                        var elementAccess = (ElementAccessExpressionSyntax)node;
                        if (!IsArray(semanticModel, elementAccess.Expression))
                        {
                            arguments = elementAccess.ArgumentList.Arguments;
                            return true;
                        }

                        arguments = default(SeparatedSyntaxList<ArgumentSyntax>);
                        return false;
                    }

                case SyntaxKind.ObjectCreationExpression:
                    {
                        var objectCreation = (ObjectCreationExpressionSyntax)node;
                        arguments = objectCreation.ArgumentList.Arguments;
                        return true;
                    }

                case SyntaxKind.ConditionalAccessExpression:
                    {
                        var conditional = (ConditionalAccessExpressionSyntax)node;
                        if (!IsArray(semanticModel, conditional.Expression))
                        {
                            var elementBinding = (ElementBindingExpressionSyntax)conditional.WhenNotNull;
                            arguments = elementBinding.ArgumentList.Arguments;
                            return true;
                        }

                        arguments = default(SeparatedSyntaxList<ArgumentSyntax>);
                        return false;
                    }

                case SyntaxKind.BaseConstructorInitializer:
                case SyntaxKind.ThisConstructorInitializer:
                    {
                        var constructorInitializer = (ConstructorInitializerSyntax)node;
                        arguments = constructorInitializer.ArgumentList.Arguments;
                        return true;
                    }

                default:
                    arguments = default(SeparatedSyntaxList<ArgumentSyntax>);
                    return false;
            }
        }

        protected override SyntaxNode ReplaceArgumentList(SyntaxNode node, SeparatedSyntaxList<ArgumentSyntax> argumentList)
        {
            switch (node.Kind())
            {
                case SyntaxKind.InvocationExpression:
                    {
                        var invocation = (InvocationExpressionSyntax)node;
                        return invocation.WithArgumentList(invocation.ArgumentList.WithArguments(argumentList));
                    }

                case SyntaxKind.ObjectCreationExpression:
                    {
                        var objectCreation = (ObjectCreationExpressionSyntax)node;
                        return objectCreation.WithArgumentList(objectCreation.ArgumentList.WithArguments(argumentList));
                    }

                case SyntaxKind.ElementAccessExpression:
                    {
                        var elementAccess = (ElementAccessExpressionSyntax)node;
                        return elementAccess.WithArgumentList(elementAccess.ArgumentList.WithArguments(argumentList));
                    }

                case SyntaxKind.ConditionalAccessExpression:
                    {
                        var conditional = (ConditionalAccessExpressionSyntax)node;
                        var invocation = (ElementBindingExpressionSyntax)conditional.WhenNotNull;
                        return conditional.WithWhenNotNull(invocation.WithArgumentList(invocation.ArgumentList.WithArguments(argumentList)));
                    }

                case SyntaxKind.BaseConstructorInitializer:
                case SyntaxKind.ThisConstructorInitializer:
                    {
                        var constructorInitializer = (ConstructorInitializerSyntax)node;
                        return constructorInitializer.WithArgumentList(constructorInitializer.ArgumentList.WithArguments(argumentList));
                    }

                default:
                    return default(SyntaxNode);
            }
        }

        protected override SyntaxNode GetTargetNode(SyntaxNode node)
        {
            if (node.IsKind(SyntaxKind.ConditionalAccessExpression))
            {
                return ((ConditionalAccessExpressionSyntax)node).WhenNotNull;
            }

            return node;
        }

        protected override bool IsLiteral(ArgumentSyntax argument)
        {
            return argument.Expression.IsAnyLiteralExpression();
        }
    }
}
