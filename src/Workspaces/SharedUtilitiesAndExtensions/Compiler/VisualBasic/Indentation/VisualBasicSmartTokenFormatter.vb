' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Indentation
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Indentation
    Friend Class VisualBasicSmartTokenFormatter
        Implements ISmartTokenFormatter

        Private ReadOnly _options As SyntaxFormattingOptions
        Private ReadOnly _formattingRules As ImmutableArray(Of AbstractFormattingRule)

        Private ReadOnly _root As CompilationUnitSyntax

        Public Sub New(options As SyntaxFormattingOptions,
                       formattingRules As ImmutableArray(Of AbstractFormattingRule),
                       root As CompilationUnitSyntax)
            Contract.ThrowIfNull(root)

            _options = options
            _formattingRules = formattingRules

            _root = root
        End Sub

        Public Function FormatToken(token As SyntaxToken, cancellationToken As CancellationToken) As IList(Of TextChange) Implements ISmartTokenFormatter.FormatToken
            Contract.ThrowIfTrue(token.Kind = SyntaxKind.None OrElse token.Kind = SyntaxKind.EndOfFileToken)

            ' get previous token
            Dim previousToken = token.GetPreviousToken()

            Dim spans = SpecializedCollections.SingletonEnumerable(TextSpan.FromBounds(previousToken.SpanStart, token.Span.End))
            Dim formatter = VisualBasicSyntaxFormatting.Instance
            Dim result = formatter.GetFormattingResult(_root, spans, _options, _formattingRules, cancellationToken)
            Return result.GetTextChanges(cancellationToken)
        End Function
    End Class
End Namespace
