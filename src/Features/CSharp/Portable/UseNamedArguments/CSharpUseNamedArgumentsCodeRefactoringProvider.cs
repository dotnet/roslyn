// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.UseNamedArguments;

namespace Microsoft.CodeAnalysis.CSharp.UseNamedArguments
{
    [ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.IntroduceVariable)]
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.UseNamedArguments), Shared]
    internal class CSharpUseNamedArgumentsCodeRefactoringProvider : AbstractUseNamedArgumentsCodeRefactoringProvider
    {
        private abstract class BaseAnalyzer<TSyntax, TSyntaxList> : Analyzer<TSyntax, TSyntax, TSyntaxList>
            where TSyntax : SyntaxNode
            where TSyntaxList : SyntaxNode
        {
            protected abstract ExpressionSyntax GetArgumentExpression(TSyntax argumentSyntax);

            protected sealed override SyntaxNode? GetReceiver(SyntaxNode argument)
                => argument.Parent?.Parent;

            protected sealed override bool IsLegalToAddNamedArguments(ImmutableArray<IParameterSymbol> parameters, int argumentCount)
                => !parameters.Last().IsParams || parameters.Length >= argumentCount;

            protected override bool SupportsNonTrailingNamedArguments(ParseOptions options)
                => options.LanguageVersion() >= LanguageVersion.CSharp7_2;

            protected override bool IsImplicitIndexOrRangeIndexer(ImmutableArray<IParameterSymbol> parameters, TSyntax argument, SemanticModel semanticModel)
            {
                // There is no direct way to tell if an implicit range or index indexer was used.
                // The heuristic we use here is to check if the parameter doesn't fit the method it's being used with. 
                // The easiest way to check that is to see if the argType only has at most an explicit conversion 
                // to the indexers parameter types.

                var argType = semanticModel.GetTypeInfo(GetArgumentExpression(argument)).Type;
                if (argType?.ContainingNamespace is { Name: nameof(System), ContainingNamespace: { IsGlobalNamespace: true } } &&
                    (argType.Name == "Range" || argType.Name == "Index"))
                {
                    var conversion = semanticModel.Compilation.ClassifyConversion(argType, parameters[0].Type);
                    if (!conversion.Exists || conversion.IsExplicit)
                    {
                        return true;
                    }
                }

                return false;
            }
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

            protected override ExpressionSyntax GetArgumentExpression(ArgumentSyntax argumentSyntax)
                => argumentSyntax.Expression;
        }

        private class AttributeArgumentAnalyzer :
            BaseAnalyzer<AttributeArgumentSyntax, AttributeArgumentListSyntax>
        {
            protected override bool IsPositionalArgument(AttributeArgumentSyntax argument)
                => argument.NameColon == null && argument.NameEquals == null;

            protected override SeparatedSyntaxList<AttributeArgumentSyntax> GetArguments(AttributeArgumentListSyntax argumentList)
                => argumentList.Arguments;

            protected override AttributeArgumentListSyntax WithArguments(
                AttributeArgumentListSyntax argumentList, IEnumerable<AttributeArgumentSyntax> namedArguments, IEnumerable<SyntaxToken> separators)
                => argumentList.WithArguments(SyntaxFactory.SeparatedList(namedArguments, separators));

            protected override AttributeArgumentSyntax WithName(AttributeArgumentSyntax argument, string name)
                => argument.WithNameColon(SyntaxFactory.NameColon(name.ToIdentifierName()));

            protected override ExpressionSyntax GetArgumentExpression(AttributeArgumentSyntax argumentSyntax)
                => argumentSyntax.Expression;
        }

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpUseNamedArgumentsCodeRefactoringProvider()
            : base(new ArgumentAnalyzer(), new AttributeArgumentAnalyzer())
        {
        }
    }
}
