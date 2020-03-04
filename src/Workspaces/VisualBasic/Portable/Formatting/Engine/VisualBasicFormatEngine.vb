﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Formatting.Rules

#If CODE_STYLE Then
Imports OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions
#Else
Imports Microsoft.CodeAnalysis.Options
#End If

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    Partial Friend Class VisualBasicFormatEngine
        Inherits AbstractFormatEngine

        Public Sub New(node As SyntaxNode,
                       optionSet As OptionSet,
                       formattingRules As IEnumerable(Of AbstractFormattingRule),
                       token1 As SyntaxToken,
                       token2 As SyntaxToken)
            MyBase.New(TreeData.Create(node),
                       optionSet,
                       formattingRules,
                       token1,
                       token2)
        End Sub

        Protected Overrides Function CreateTriviaFactory() As AbstractTriviaDataFactory
            Return New TriviaDataFactory(Me.TreeData, Me.OptionSet)
        End Function

        Protected Overrides Function CreateFormattingResult(tokenStream As TokenStream) As AbstractFormattingResult
            Contract.ThrowIfNull(tokenStream)

            Return New FormattingResult(Me.TreeData, tokenStream, Me.SpanToFormat)
        End Function
    End Class
End Namespace
