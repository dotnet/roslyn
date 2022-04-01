' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.ObjectModel
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.VisualStudio.Debugger.ComponentInterfaces
Imports Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation
Imports Microsoft.VisualStudio.Debugger.Metadata

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    ''' <summary>
    ''' Computes string representations of <see cref="DkmClrValue"/> instances.
    ''' </summary>
    Partial Friend NotInheritable Class VisualBasicFormatter
        Inherits Formatter

        ''' <summary>
        ''' Singleton instance of VisualBasicFormatter (created using default constructor).
        ''' </summary>
        Friend Shared ReadOnly Instance As VisualBasicFormatter = New VisualBasicFormatter()

        Public Sub New()
            MyBase.New(defaultFormat:="{{{0}}}", nullString:="Nothing", thisString:="Me")
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

        ' TODO https://github.com/dotnet/roslyn/issues/37536 
        ' This parsing is imprecise and may result in bad expressions.
        Friend Overrides Function TrimAndGetFormatSpecifiers(expression As String, ByRef formatSpecifiers As ReadOnlyCollection(Of String)) As String
            expression = RemoveComments(expression)
            expression = RemoveFormatSpecifiers(expression, formatSpecifiers)
            Return RemoveLeadingAndTrailingWhitespace(expression)
        End Function

        ' TODO https://github.com/dotnet/roslyn/issues/60581
        Friend Overrides Function GetOriginalLocalVariableName(name As String) As String
            Return name
        End Function

        ' TODO Implement this VB. https://github.com/dotnet/roslyn/issues/60581
        Friend Overrides Function GetOriginalFieldName(name As String) As String
            Return name
        End Function

        Private Shared Function RemoveComments(expression As String) As String
            ' Workaround for https://dev.azure.com/devdiv/DevDiv/_workitems/edit/847849
            ' Do not remove any comments that might be in a string. 
            ' This won't work when there are quotes in the comment, but that's not that common.
            Dim lastQuote As Integer = expression.LastIndexOf(""""c) + 1

            Dim index = expression.IndexOf("'"c, lastQuote)
            If index < 0 Then
                Return expression
            End If
            Return expression.Substring(0, index)
        End Function

    End Class

End Namespace
