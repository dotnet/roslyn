// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.UseInterpolatedString;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseInterpolatedString;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class CSharpUseInterpolatedStringDiagnosticAnalyzer() : AbstractUseInterpolatedStringDiagnosticAnalyzer<SyntaxKind, ExpressionSyntax, LiteralExpressionSyntax>
{
    protected override ISyntaxFacts GetSyntaxFacts() => CSharpSyntaxFacts.Instance;

    protected sealed override bool CanConvertToInterpolatedString(LiteralExpressionSyntax stringLiteralExpression, ParseOptions parseOptions)
    {
        // Check if the token is a string literal
        if (stringLiteralExpression.Kind() != SyntaxKind.StringLiteralExpression)
            return false;

        // Check the string literal for errors
        if (stringLiteralExpression.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
            return false;

        // Check if the string contains '{' or '}' characters
        if (!stringLiteralExpression.Token.Text.Contains('{') || !stringLiteralExpression.Token.Text.Contains('}'))
            return false;

        // If there is a const keyword, do not offer the refactoring
        // An interpolated string is not const
        var syntaxFacts = this.GetSyntaxFacts();

        if (!syntaxFacts.SupportsConstantInterpolatedStrings(parseOptions))
        {
            // If there is a const keyword, do not offer the refactoring (an interpolated string is not const)
            var declarator = stringLiteralExpression.FirstAncestorOrSelf<SyntaxNode>(syntaxFacts.IsVariableDeclarator);
            if (declarator != null)
            {
                var modifiers = syntaxFacts.GetModifiers(declarator);
                if (modifiers.Any(SyntaxKind.ConstKeyword))
                    return false;
            }

            // Attributes also only allow constant values
            var attribute = stringLiteralExpression.FirstAncestorOrSelf<AttributeSyntax>(syntaxFacts.IsAttribute);
            if (attribute != null)
                return false;
        }

        // If all checks passed, the string can be converted to an interpolated string
        return true;
    }
}
