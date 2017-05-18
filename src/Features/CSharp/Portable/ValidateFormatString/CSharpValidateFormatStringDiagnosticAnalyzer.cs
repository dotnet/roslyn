// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.ValidateFormatString;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.ValidateFormatString
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpValidateFormatStringDiagnosticAnalyzer : 
        AbstractValidateFormatStringDiagnosticAnalyzer<SyntaxKind>
    {
        protected override ISyntaxFactsService GetSyntaxFactsService()
            => CSharpSyntaxFactsService.Instance;

        internal override bool TryGetArgsArgumentType(
            SemanticModel semanticModel,
            SeparatedSyntaxList<SyntaxNode> arguments,
            ImmutableArray<IParameterSymbol> parameters,
            out ITypeSymbol argsArgumentType)
        {
            if (!TryGetArgument(semanticModel, "args", arguments, parameters, out var argsArgument))
            {
                argsArgumentType = null;
                return false;
            }

            argsArgumentType = semanticModel.GetTypeInfo(argsArgument.Expression).Type;
            return true;
        }

        private static bool TryGetArgument(
            SemanticModel semanticModel,
            string searchArgumentName,
            SeparatedSyntaxList<ArgumentSyntax> arguments,
            ImmutableArray<IParameterSymbol> parameters,
            out ArgumentSyntax argumentSyntax)
        {
            argumentSyntax = null;

            // First, look for a named argument that matches
            var namedArgument = arguments.SingleOrDefault(
                p => p.NameColon?.Name.Identifier.ValueText.Equals(searchArgumentName) == true);

            if (namedArgument != null)
            {
                argumentSyntax = namedArgument;
                return true;
            }

            // If no named argument exists, look for the named parameter
            // and return the corresponding argument
            var namedParameter = parameters.SingleOrDefault(p => p.Name.Equals(searchArgumentName) == true);
            if (namedParameter != null)
            {
                // For the case string.Format("Test string"), there is only one argument
                // but the compiler created an empty parameter array to bind to an overload
                if (namedParameter.Ordinal >= arguments.Count)
                {
                    return false;
                }

                // Multiple arguments could have been converted to a single params array, 
                // so there wouldn't be a corresponding argument
                if (namedParameter.IsParams && parameters.Length != arguments.Count)
                {
                    return false;
                }

                argumentSyntax = arguments[namedParameter.Ordinal];
                return true;
            }

            return false;
        }

        protected override bool TryGetFormatStringLiteralExpressionSyntax(
            SemanticModel semanticModel,
            SeparatedSyntaxList<SyntaxNode> arguments,
            ImmutableArray<IParameterSymbol> parameters,
            out SyntaxNode formatStringLiteralExpressionSyntax)
        {
            formatStringLiteralExpressionSyntax = null;

            if (!TryGetArgument(semanticModel, "format", arguments, parameters, out var formatArgumentSyntax))
            {
                return false;
            }

            if (!formatArgumentSyntax.Expression.IsKind(SyntaxKind.StringLiteralExpression))
            {
                return false;
            }

            formatStringLiteralExpressionSyntax = formatArgumentSyntax.Expression;
            return true;
        }

        protected override SyntaxKind GetInvocationExpressionSyntaxKind()
            => SyntaxKind.InvocationExpression;

        protected override string GetLiteralExpressionSyntaxAsString(SyntaxNode syntaxNode) 
            => ((LiteralExpressionSyntax)syntaxNode).Token.Text;

        protected override int GetLiteralExpressionSyntaxSpanStart(SyntaxNode syntaxNode)
            => ((LiteralExpressionSyntax)syntaxNode).Token.SpanStart;
    }
}