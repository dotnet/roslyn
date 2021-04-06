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
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.IntroduceVariable
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp), Shared]
    internal partial class CSharpIntroduceParameterCodeRefactoringProvider : AbstractIntroduceParameterService<
        ExpressionSyntax,
        InvocationExpressionSyntax,
        ObjectCreationExpressionSyntax,
        IdentifierNameSyntax>
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpIntroduceParameterCodeRefactoringProvider()
        {
        }

        protected override SyntaxNode GenerateExpressionFromOptionalParameter(IParameterSymbol parameterSymbol)
        {
            return ExpressionGenerator.GenerateExpression(parameterSymbol.Type, parameterSymbol.ExplicitDefaultValue, canUseFieldReference: true);
        }

        protected override SyntaxNode? GetLocalDeclarationFromDeclarator(SyntaxNode variableDecl)
        {
            return variableDecl.Parent?.Parent as LocalDeclarationStatementSyntax;
        }

        protected override SyntaxNode UpdateArgumentListSyntax(SyntaxNode argumentList, SeparatedSyntaxList<SyntaxNode> arguments)
            => (argumentList as ArgumentListSyntax)!.WithArguments(arguments);

        protected override bool IsClassSpecificExpression(SyntaxNode variable)
            => variable.Kind() is SyntaxKind.ThisExpression or SyntaxKind.BaseExpression;
    }
}
