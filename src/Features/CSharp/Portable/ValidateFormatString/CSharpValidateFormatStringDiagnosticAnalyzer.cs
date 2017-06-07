// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.ValidateFormatString;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.ValidateFormatString
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpValidateFormatStringDiagnosticAnalyzer :
        AbstractValidateFormatStringDiagnosticAnalyzer<SyntaxKind>
    {
        protected override ISyntaxFactsService GetSyntaxFactsService()
            => CSharpSyntaxFactsService.Instance;

        protected override SyntaxKind GetInvocationExpressionSyntaxKind()
            => SyntaxKind.InvocationExpression;

        protected override ITypeSymbol GetArgumentExpressionType(
            SemanticModel semanticModel, 
            SyntaxNode argsArgument)
                => semanticModel.GetTypeInfo(((ArgumentSyntax)argsArgument).Expression).Type;

        protected override SyntaxNode GetMatchingNamedArgument(
            SeparatedSyntaxList<SyntaxNode> arguments, 
            string searchArgumentName)
        {
            foreach (var argument in arguments.Cast<ArgumentSyntax>())
            {
                if (argument.NameColon != null && argument.NameColon.Name.Identifier.ValueText.Equals(searchArgumentName))
                {
                return argument;
                }
            }

            return null;
        }

        protected override bool ArgumentExpressionIsStringLiteral(SyntaxNode syntaxNode)
            => ((ArgumentSyntax)syntaxNode).Expression.IsKind(SyntaxKind.StringLiteralExpression);

        protected override SyntaxNode GetArgumentExpression(SyntaxNode syntaxNode)
            => ((ArgumentSyntax)syntaxNode).Expression;
    }
}