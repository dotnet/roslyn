' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic.Utilities
    Friend Module SyntaxKindSet

        Public ReadOnly AllOperators As ISet(Of SyntaxKind) = New HashSet(Of SyntaxKind)(SyntaxFacts.EqualityComparer) From
        {
            SyntaxKind.ExclamationToken,
            SyntaxKind.AtToken,
            SyntaxKind.AmpersandToken,
            SyntaxKind.SingleQuoteToken,
            SyntaxKind.SemicolonToken,
            SyntaxKind.AsteriskToken,
            SyntaxKind.PlusToken,
            SyntaxKind.MinusToken,
            SyntaxKind.DotToken,
            SyntaxKind.SlashToken,
            SyntaxKind.LessThanToken,
            SyntaxKind.LessThanEqualsToken,
            SyntaxKind.LessThanGreaterThanToken,
            SyntaxKind.EqualsToken,
            SyntaxKind.GreaterThanToken,
            SyntaxKind.GreaterThanEqualsToken,
            SyntaxKind.BackslashToken,
            SyntaxKind.CaretToken,
            SyntaxKind.ColonEqualsToken,
            SyntaxKind.AmpersandEqualsToken,
            SyntaxKind.AsteriskEqualsToken,
            SyntaxKind.PlusEqualsToken,
            SyntaxKind.MinusEqualsToken,
            SyntaxKind.SlashEqualsToken,
            SyntaxKind.BackslashEqualsToken,
            SyntaxKind.CaretEqualsToken,
            SyntaxKind.LessThanLessThanToken,
            SyntaxKind.GreaterThanGreaterThanToken,
            SyntaxKind.LessThanLessThanEqualsToken,
            SyntaxKind.GreaterThanGreaterThanEqualsToken
        }

    End Module
End Namespace
