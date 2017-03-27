// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ValidateFormatString;

namespace Microsoft.CodeAnalysis.CSharp.ValidateFormatString
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class ValidateFormatStringDiagnosticAnalyzer : AbstractValidateFormatStringDiagnosticAnalyzer
    {
        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var invocationExpr = (InvocationExpressionSyntax)context.Node;
            if (invocationExpr.ArgumentList?.Arguments == null)
            {
                return;
            }

            SimpleNameSyntax name;
            // When calling string.Format(...), Format will be of type MemberAccessExpressionSyntax 
            if (invocationExpr.Expression is MemberAccessExpressionSyntax memberAccessExpr)
            {
                if (memberAccessExpr.Name?.ToString() != "Format")
                {
                    return;
                }

                name = memberAccessExpr.Name;
            }
            // When using static System.String and calling Format(...), Format will be of type IdentifierNameSyntax
            else if (invocationExpr.Expression is IdentifierNameSyntax identifierNameSyntax)
            {
                if (identifierNameSyntax.ToString() != "Format")
                {
                    return;
                }

                name = identifierNameSyntax;
            }
            else
            {
                return;
            }

            var arguments = invocationExpr.ArgumentList.Arguments;
            var numberOfArguments = arguments.Count;

            var symbolInfo = context.SemanticModel.GetSymbolInfo(name, context.CancellationToken);
           
            if (!TryGetFormatMethod(context, numberOfArguments, symbolInfo, out var method))
            {
                return;
            }

            var hasIFormatProvider =
                method.Parameters[0].Type.ToString().Equals(typeof(IFormatProvider).FullName);

            if (!TryGetFormatStringAndLocation(
                arguments,
                hasIFormatProvider,
                out var formatString,
                out var formatStringLocation))
            {
                return;
            }

            ValidateAndReportDiagnostic(context, hasIFormatProvider, arguments.Count, formatString, formatStringLocation);
        }

        private static bool TryGetFormatStringAndLocation(
            SeparatedSyntaxList<ArgumentSyntax> arguments, 
            bool hasIFormatProvider, 
            out string formatString, 
            out Location formatStringLocation)
        {
            formatString = null;
            formatStringLocation = null;

            var formatArgumentSyntax = GetFormatStringArgument(arguments, hasIFormatProvider);
            if (formatArgumentSyntax == null)
            {
                return false;
            }

            if (!formatArgumentSyntax.Expression.IsKind(SyntaxKind.StringLiteralExpression))
            {
                return false;
            }

            var formatLiteralExpressionSyntax = (LiteralExpressionSyntax)formatArgumentSyntax.Expression;
            formatString = formatLiteralExpressionSyntax.Token.ValueText;
            formatStringLocation = formatArgumentSyntax.GetLocation();

            return true;
        }

        private static ArgumentSyntax GetFormatStringArgument(SeparatedSyntaxList<ArgumentSyntax> arguments, bool hasIFormatProvider)
        {
            foreach (var argument in arguments)
            {
                if (argument.NameColon != null && !argument.NameColon.IsMissing && argument.NameColon.Name.Identifier.ValueText.Equals("format"))
                {
                    return argument;
                }
            }

            // If using positional arguments, the format string will be the first or second
            // argument depending on whether there Is an IFormatProvider argument.
            return (hasIFormatProvider ? arguments[1] : arguments[0]);
        }
    }
}