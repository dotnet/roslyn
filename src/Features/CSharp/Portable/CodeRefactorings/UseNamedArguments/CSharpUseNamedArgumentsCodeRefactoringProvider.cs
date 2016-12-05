// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.UseNamedArguments;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.UseNamedArguments
{
    [ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.IntroduceVariable)]
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpUseNamedArgumentsCodeRefactoringProvider)), Shared]
    internal class CSharpUseNamedArgumentsCodeRefactoringProvider : AbstractUseNamedArgumentsCodeRefactoringProvider
    {
        protected override SyntaxNode GetOrSynthesizeNamedArguments(ImmutableArray<IParameterSymbol> parameters, SyntaxNode argumentList, int index)
        {
            switch (argumentList.Kind())
            {
                case SyntaxKind.ArgumentList:
                    {
                        var node = (ArgumentListSyntax)argumentList;
                        var namedArguments = node.Arguments
                            .Select((argument, i) => i < index || argument.NameColon != null
                                ? argument : argument.WithNameColon(SyntaxFactory.NameColon(parameters[i].Name)
                                    .WithTriviaFrom(argument)));

                        return node.WithArguments(SyntaxFactory.SeparatedList(namedArguments));
                    }

                case SyntaxKind.BracketedArgumentList:
                    {
                        var node = (BracketedArgumentListSyntax)argumentList;
                        var namedArguments = node.Arguments
                            .Select((argument, i) => i < index || argument.NameColon != null
                                ? argument : argument.WithNameColon(SyntaxFactory.NameColon(parameters[i].Name)
                                    .WithTriviaFrom(argument)));

                        return node.WithArguments(SyntaxFactory.SeparatedList(namedArguments));
                    }

                case SyntaxKind.AttributeArgumentList:
                    {
                        var node = (AttributeArgumentListSyntax)argumentList;
                        var namedArguments = node.Arguments
                            .Select((argument, i) => i < index || argument.NameColon != null || argument.NameEquals != null
                                ? argument : argument.WithNameColon(SyntaxFactory.NameColon(parameters[i].Name)
                                    .WithTriviaFrom(argument)));

                        return node.WithArguments(SyntaxFactory.SeparatedList(namedArguments));
                    }

                default:
                    return null;
            }
        }

        protected override SyntaxNode GetReceiver(SyntaxNode argument)
        {
            switch (argument.Parent.Kind())
            {
                case SyntaxKind.ArgumentList:
                case SyntaxKind.BracketedArgumentList:
                case SyntaxKind.AttributeArgumentList:
                    return argument.Parent.Parent;

                default:
                    return null;
            }
        }

        protected override ValueTuple<int, int> GetArgumentListIndexAndCount(SyntaxNode node)
        {
            switch (node.Parent.Kind())
            {
                case SyntaxKind.ArgumentList:
                case SyntaxKind.BracketedArgumentList:
                    var argumentListSyntax = (BaseArgumentListSyntax)node.Parent;
                    return ValueTuple.Create(argumentListSyntax.Arguments.IndexOf((ArgumentSyntax)node), argumentListSyntax.Arguments.Count);

                case SyntaxKind.AttributeArgumentList:
                    var attributeArgumentSyntax = (AttributeArgumentListSyntax)node.Parent;
                    return ValueTuple.Create(attributeArgumentSyntax.Arguments.IndexOf((AttributeArgumentSyntax)node), attributeArgumentSyntax.Arguments.Count);

                default:
                    return default(ValueTuple<int, int>);
            }
        }

        protected override bool IsCandidate(SyntaxNode node)
        {
            return node.IsKind(SyntaxKind.Argument)
                || node.IsKind(SyntaxKind.AttributeArgument);
        }

        protected override bool IsPositionalArgument(SyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.Argument:
                    var argument = (ArgumentSyntax)node;
                    return argument.NameColon == null;

                case SyntaxKind.AttributeArgument:
                    var attributeArgument = (AttributeArgumentSyntax)node;
                    return attributeArgument.NameColon == null
                        && attributeArgument.NameEquals == null;

                default:
                    return false;
            }
        }

        protected override bool IsLegalToAddNamedArguments(ImmutableArray<IParameterSymbol> parameters, int argumentCount)
        {
            return !parameters.LastOrDefault().IsParams || parameters.Length >= argumentCount;
        }
    }
}
