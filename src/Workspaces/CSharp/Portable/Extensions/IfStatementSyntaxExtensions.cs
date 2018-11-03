// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class IfStatementSyntaxExtensions
    {
        /// <summary>
        /// Returns the 'header' span of the if statement.  Header-span goes from the <see
        /// cref="SyntaxNode.SpanStart"/> if the if-statement up to the end of its <see
        /// cref="IfStatementSyntax.CloseParenToken"/> if it has one, or the end of its <see
        /// cref="IfStatementSyntax.Condition"/> otherwise.
        public static TextSpan GetHeaderSpan(this IfStatementSyntax ifStatement)
        {
            var end = ifStatement.CloseParenToken == default
                ? (SyntaxNodeOrToken)ifStatement.Condition
                : ifStatement.CloseParenToken;

            return TextSpan.FromBounds(ifStatement.SpanStart, end.Span.End);
        }

        /// <summary>
        /// Returns true if this is an `if !(...)` guard-if statement.
        /// </summary>
        public static bool IsIfGuard(this IfStatementSyntax ifStatement)
            => ifStatement.OpenParenToken == default;
    }
}
