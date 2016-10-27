// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.UseNamedArguments;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.UseNamedArguments
{
    [ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.IntroduceVariable)]
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpUseNamedArgumentsCodeRefactoringProvider)), Shared]
    internal class CSharpUseNamedArgumentsCodeRefactoringProvider : AbstractUseNamedArgumentsCodeRefactoringProvider<ArgumentSyntax>
    {
        protected override bool TryGetOrSynthesizeNamedArguments(
            SeparatedSyntaxList<ArgumentSyntax> arguments,
            ImmutableArray<IParameterSymbol> parameters,
            out ArgumentSyntax[] namedArguments,
            out bool hasLiteral)
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
                        hasLiteral |= IsLiteral(argument);

                        // Preserve trivia when argument is ref or out
                        namedArgument = namedArgument.WithTriviaFrom(argument);

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
                    return conditional.WhenNotNull.IsKind(SyntaxKind.ElementBindingExpression);

                default:
                    return false;
            }
        }


        protected override SeparatedSyntaxList<ArgumentSyntax> GetArguments(SyntaxNode node, out SyntaxNode targetNode)
        {
            targetNode = node;
            switch (node.Kind())
            {
                case SyntaxKind.InvocationExpression:
                    var invocation = (InvocationExpressionSyntax)node;
                    return invocation.ArgumentList.Arguments;

                case SyntaxKind.ElementAccessExpression:
                    var elementAccess = (ElementAccessExpressionSyntax)node;
                    return elementAccess.ArgumentList.Arguments;

                case SyntaxKind.ObjectCreationExpression:
                    var objectCreation = (ObjectCreationExpressionSyntax)node;
                    return objectCreation.ArgumentList.Arguments;

                case SyntaxKind.ConditionalAccessExpression:
                    var conditional = (ConditionalAccessExpressionSyntax)node;
                    var elementBinding = (ElementBindingExpressionSyntax)conditional.WhenNotNull;
                    targetNode = elementBinding;
                    return elementBinding.ArgumentList.Arguments;

                case SyntaxKind.BaseConstructorInitializer:
                case SyntaxKind.ThisConstructorInitializer:
                    var constructorInitializer = (ConstructorInitializerSyntax)node;
                    return constructorInitializer.ArgumentList.Arguments;

                default:
                    return default(SeparatedSyntaxList<ArgumentSyntax>);
            }
        }

        protected override SyntaxNode ReplaceArgumentList(SyntaxNode node, SeparatedSyntaxList<ArgumentSyntax> arguments)
        {
            switch (node.Kind())
            {
                case SyntaxKind.InvocationExpression:
                    var invocation = (InvocationExpressionSyntax)node;
                    return invocation.WithArgumentList(invocation.ArgumentList.WithArguments(arguments));

                case SyntaxKind.ObjectCreationExpression:
                    var objectCreation = (ObjectCreationExpressionSyntax)node;
                    return objectCreation.WithArgumentList(objectCreation.ArgumentList.WithArguments(arguments));

                case SyntaxKind.ElementAccessExpression:
                    var elementAccess = (ElementAccessExpressionSyntax)node;
                    return elementAccess.WithArgumentList(elementAccess.ArgumentList.WithArguments(arguments));

                case SyntaxKind.ConditionalAccessExpression:
                    var conditional = (ConditionalAccessExpressionSyntax)node;
                    var whenNotNull = (ElementBindingExpressionSyntax)conditional.WhenNotNull;
                    return conditional.WithWhenNotNull(whenNotNull.WithArgumentList(whenNotNull.ArgumentList.WithArguments(arguments)));

                case SyntaxKind.BaseConstructorInitializer:
                case SyntaxKind.ThisConstructorInitializer:
                    var constructorInitializer = (ConstructorInitializerSyntax)node;
                    return constructorInitializer.WithArgumentList(constructorInitializer.ArgumentList.WithArguments(arguments));

                default:
                    return default(SyntaxNode);
            }
        }

        protected override bool IsLiteral(ArgumentSyntax argument)
            => argument.Expression.IsAnyLiteralExpression();
    }
}
