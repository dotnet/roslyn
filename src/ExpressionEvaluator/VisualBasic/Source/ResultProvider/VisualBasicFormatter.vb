' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.ObjectModel
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation
Imports Microsoft.VisualStudio.Debugger.Metadata

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    ''' <summary>
    ''' Computes string representations of <see cref="DkmClrValue"/> instances.
    ''' </summary>
    Partial Friend NotInheritable Class VisualBasicFormatter : Inherits Formatter

        ''' <summary>
        ''' Singleton instance of VisualBasicFormatter (created using default constructor).
        ''' </summary>
        Friend Shared ReadOnly Instance As VisualBasicFormatter = New VisualBasicFormatter()

        Public Sub New()
            MyBase.New(defaultFormat:="{{{0}}}",
                       nullString:="Nothing",
                       staticMembersString:=Resources.SharedMembers)
        End Sub

        Friend Overrides Function IsValidIdentifier(name As String) As Boolean
            Return SyntaxFacts.IsValidIdentifier(name)
        End Function

        Friend Overrides Function IsIdentifierPartCharacter(c As Char) As Boolean
            Return SyntaxFacts.IsIdentifierPartCharacter(c)
        End Function

        Friend Overrides Function IsPredefinedType(type As Type) As Boolean
            Return type.IsPredefinedType()
        End Function

        Friend Overrides Function IsWhitespace(c As Char) As Boolean
            Return SyntaxFacts.IsWhitespace(c)
        End Function

        Friend Overrides Function GetValueStringOptions(useQuotes As Boolean) As ObjectDisplayOptions
            Return If(useQuotes,
                ObjectDisplayOptions.UseQuotes Or ObjectDisplayOptions.EscapeNonPrintableStringCharacters,
                ObjectDisplayOptions.None)
        End Function

        Friend Overrides Function TrimAndGetFormatSpecifiers(expression As String, ByRef formatSpecifiers As ReadOnlyCollection(Of String)) As String
            expression = RemoveComments(expression)
            expression = RemoveFormatSpecifiers(expression, formatSpecifiers)
            Return RemoveLeadingAndTrailingWhitespace(expression)
        End Function

        Private Shared Function RemoveComments(expression As String) As String
            Dim index = expression.IndexOf("'"c)
            If index < 0 Then
                Return expression
            End If
            Return expression.Substring(0, index)
        End Function

    End Class

End Namespace
