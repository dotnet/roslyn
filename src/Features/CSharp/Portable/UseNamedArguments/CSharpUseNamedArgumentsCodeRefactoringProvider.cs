// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.UseNamedArguments;

namespace Microsoft.CodeAnalysis.CSharp.UseNamedArguments
{
    [ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.IntroduceVariable)]
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpUseNamedArgumentsCodeRefactoringProvider)), Shared]
    internal class CSharpUseNamedArgumentsCodeRefactoringProvider : AbstractUseNamedArgumentsCodeRefactoringProvider
    {
        private abstract class BaseAnalyzer<TSyntax, TSyntaxList> : Analyzer<TSyntax, TSyntax, TSyntaxList>
            where TSyntax : SyntaxNode
            where TSyntaxList : SyntaxNode
        {
            protected sealed override SyntaxNode GetReceiver(SyntaxNode argument)
                => argument.Parent.Parent;

            protected sealed override bool IsLegalToAddNamedArguments(ImmutableArray<IParameterSymbol> parameters, int argumentCount)
                => !parameters.Last().IsParams || parameters.Length >= argumentCount;

            protected sealed override bool IsCloseParenOrComma(SyntaxToken token)
                => token.IsKind(SyntaxKind.CloseParenToken, SyntaxKind.CommaToken);

            protected override bool SupportsNonTrailingNamedArguments(ParseOptions options)
                => ((CSharpParseOptions)options).LanguageVersion >= LanguageVersion.CSharp7_2;
        }

        private class ArgumentAnalyzer :
            BaseAnalyzer<ArgumentSyntax, BaseArgumentListSyntax>
        {
            protected override bool IsPositionalArgument(ArgumentSyntax node)
                => node.NameColon == null;

            protected override SeparatedSyntaxList<ArgumentSyntax> GetArguments(BaseArgumentListSyntax argumentList)
                => argumentList.Arguments;

            protected override BaseArgumentListSyntax WithArguments(
                BaseArgumentListSyntax argumentList, IEnumerable<ArgumentSyntax> namedArguments, IEnumerable<SyntaxToken> separators)
                => argumentList.WithArguments(SyntaxFactory.SeparatedList(namedArguments, separators));

            protected override ArgumentSyntax WithName(ArgumentSyntax argument, string name)
                => argument.WithNameColon(SyntaxFactory.NameColon(name.ToIdentifierName()));
        }

        private class AttributeArgumentAnalyzer :
            BaseAnalyzer<AttributeArgumentSyntax, AttributeArgumentListSyntax>
        {
            protected override bool IsPositionalArgument(AttributeArgumentSyntax argument)
                => argument is { NameColon: null, NameEquals: null };

            protected override SeparatedSyntaxList<AttributeArgumentSyntax> GetArguments(AttributeArgumentListSyntax argumentList)
                => argumentList.Arguments;

            protected override AttributeArgumentListSyntax WithArguments(
                AttributeArgumentListSyntax argumentList, IEnumerable<AttributeArgumentSyntax> namedArguments, IEnumerable<SyntaxToken> separators)
                => argumentList.WithArguments(SyntaxFactory.SeparatedList(namedArguments, separators));

            protected override AttributeArgumentSyntax WithName(AttributeArgumentSyntax argument, string name)
                => argument.WithNameColon(SyntaxFactory.NameColon(name.ToIdentifierName()));
        }

        [ImportingConstructor]
        public CSharpUseNamedArgumentsCodeRefactoringProvider()
            : base(new ArgumentAnalyzer(), new AttributeArgumentAnalyzer())
        {
        }
    }
}
