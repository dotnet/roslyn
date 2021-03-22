// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.IntroduceVariable;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.IntroduceVariable
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp), Shared]
    internal partial class CSharpIntroduceParameterCodeRefactoringProvider : AbstractIntroduceParameterService<
        CSharpIntroduceParameterCodeRefactoringProvider,
        ExpressionSyntax,
        InvocationExpressionSyntax,
        IdentifierNameSyntax>
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpIntroduceParameterCodeRefactoringProvider()
        {
        }

        protected override SeparatedSyntaxList<SyntaxNode> AddArgumentToArgumentList(SeparatedSyntaxList<SyntaxNode> invocationArguments,
                                                                                     SyntaxNode newArgumentExpression,
                                                                                     int insertionIndex,
                                                                                     string name,
                                                                                     bool named)
        {
            ArgumentSyntax argument;
            if (named)
            {
                var nameColon = SyntaxFactory.NameColon(name);
                argument = SyntaxFactory.Argument(nameColon, default, (ExpressionSyntax)newArgumentExpression.WithAdditionalAnnotations(Simplifier.Annotation));
            }
            else
            {
                argument = SyntaxFactory.Argument((ExpressionSyntax)newArgumentExpression.WithAdditionalAnnotations(Simplifier.Annotation));
            }
            return invocationArguments.Insert(insertionIndex, argument);
        }

        protected override SyntaxNode GenerateExpressionFromOptionalParameter(IParameterSymbol parameterSymbol)
        {
            return ExpressionGenerator.GenerateExpression(parameterSymbol.Type, parameterSymbol.ExplicitDefaultValue, canUseFieldReference: true);
        }

        protected override ImmutableArray<SyntaxNode> AddExpressionArgumentToArgumentList(ImmutableArray<SyntaxNode> arguments, SyntaxNode expression)
        {
            var newArgument = SyntaxFactory.Argument((ExpressionSyntax)expression);
            return arguments.Add(newArgument);
        }

        protected override List<IParameterSymbol> GetParameterList(SemanticDocument document, SyntaxNode parameterList, CancellationToken cancellationToken)
        {
            var semanticModel = document.SemanticModel;
            var parameterSyntaxList = ((ParameterListSyntax)parameterList).Parameters;
            var parameterSymbolList = new List<IParameterSymbol>();

            foreach (var parameterSyntax in parameterSyntaxList)
            {
                var symbolInfo = semanticModel.GetDeclaredSymbol(parameterSyntax, cancellationToken);
                if (symbolInfo is IParameterSymbol parameterSymbol)
                {
                    parameterSymbolList.Add(parameterSymbol);
                }
            }

            return parameterSymbolList;
        }

        protected override bool IsMethodDeclaration(SyntaxNode node)
            => node.IsKind(SyntaxKind.LocalFunctionStatement) || node.IsKind(SyntaxKind.MethodDeclaration) || node.IsKind(SyntaxKind.SimpleLambdaExpression);
    }
}
