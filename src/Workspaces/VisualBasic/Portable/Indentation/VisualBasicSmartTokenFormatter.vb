' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Indentation
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Indentation
    Friend Class VisualBasicSmartTokenFormatter
        Implements ISmartTokenFormatter

        Private ReadOnly _optionSet As OptionSet
        Private ReadOnly _formattingRules As IEnumerable(Of AbstractFormattingRule)

        Private ReadOnly _root As CompilationUnitSyntax

        Public Sub New(optionSet As OptionSet,
                       formattingRules As IEnumerable(Of AbstractFormattingRule),
                       root As CompilationUnitSyntax)
            Contract.ThrowIfNull(optionSet)
            Contract.ThrowIfNull(formattingRules)
            Contract.ThrowIfNull(root)

            Me._optionSet = optionSet
            Me._formattingRules = formattingRules

            Me._root = root
        End Sub

        Public Function FormatTokenAsync(workspace As Workspace, token As SyntaxToken, cancellationToken As CancellationToken) As Tasks.Task(Of IList(Of TextChange)) Implements ISmartTokenFormatter.FormatTokenAsync
            Contract.ThrowIfTrue(token.Kind = SyntaxKind.None OrElse token.Kind = SyntaxKind.EndOfFileToken)

            ' get previous token
            Dim previousToken = token.GetPreviousToken()

            Dim spans = SpecializedCollections.SingletonEnumerable(TextSpan.FromBounds(previousToken.SpanStart, token.Span.End))
            Return Task.FromResult(Formatter.GetFormattedTextChanges(_root, spans, workspace, _optionSet, _formattingRules, cancellationToken))
        End Function
    End Class
End Namespace
