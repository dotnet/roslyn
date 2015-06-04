' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.ObjectModel
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.VisualStudio.Debugger.Clr
Imports Microsoft.VisualStudio.Debugger.ComponentInterfaces
Imports Microsoft.VisualStudio.Debugger.Evaluation
Imports Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation
Imports Microsoft.VisualStudio.Debugger.Metadata

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    ''' <summary>
    ''' Computes string representations of <see cref="DkmClrValue"/> instances.
    ''' </summary>
    Partial Friend NotInheritable Class VisualBasicFormatter : Inherits Formatter : Implements IDkmClrFormatter

        ''' <summary>
        ''' Singleton instance of VisualBasicFormatter (created using default constructor).
        ''' </summary>
        Friend Shared ReadOnly Instance As VisualBasicFormatter = New VisualBasicFormatter()

        Public Sub New()
            MyBase.New(defaultFormat:="{{{0}}}",
                       nullString:="Nothing",
                       staticMembersString:=Resources.SharedMembers)
        End Sub

        Private Function IDkmClrFormatter_GetTypeName(inspectionContext As DkmInspectionContext, clrType As DkmClrType, clrTypeInfo As DkmClrCustomTypeInfo, formatSpecifiers As ReadOnlyCollection(Of String)) As String Implements IDkmClrFormatter.GetTypeName
            Return GetTypeName(New TypeAndCustomInfo(clrType.GetLmrType(), clrTypeInfo), escapeKeywordIdentifiers:=False, sawInvalidIdentifier:=Nothing)
        End Function

        Private Function IDkmClrFormatter_GetUnderlyingString(clrValue As DkmClrValue, inspectionContext As DkmInspectionContext) As String Implements IDkmClrFormatter.GetUnderlyingString
            Return GetUnderlyingString(clrValue, inspectionContext)
        End Function

        Private Function IDkmClrFormatter_GetValueString(clrValue As DkmClrValue, inspectionContext As DkmInspectionContext, formatSpecifiers As ReadOnlyCollection(Of String)) As String Implements IDkmClrFormatter.GetValueString
            Dim options = If((inspectionContext.EvaluationFlags And DkmEvaluationFlags.NoQuotes) = 0,
                ObjectDisplayOptions.UseQuotes,
                ObjectDisplayOptions.None)
            Return GetValueString(clrValue, inspectionContext, options, GetValueFlags.IncludeObjectId)
        End Function

        Private Function IDkmClrFormatter_HasUnderlyingString(clrValue As DkmClrValue, inspectionContext As DkmInspectionContext) As Boolean Implements IDkmClrFormatter.HasUnderlyingString
            Return HasUnderlyingString(clrValue, inspectionContext)
        End Function

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
