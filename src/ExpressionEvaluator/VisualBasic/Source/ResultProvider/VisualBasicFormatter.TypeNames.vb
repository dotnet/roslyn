' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Text
Imports Type = Microsoft.VisualStudio.Debugger.Metadata.Type

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    ' Implementation for "displaying type name as string" aspect of Visual Basic Formatter component
    Partial Friend NotInheritable Class VisualBasicFormatter

        Private Shared Function IsPotentialKeyword(identifier As String) As Boolean
            Return SyntaxFacts.GetKeywordKind(identifier) <> SyntaxKind.None OrElse SyntaxFacts.GetContextualKeywordKind(identifier) <> SyntaxKind.None
        End Function

        Protected Overrides Sub AppendIdentifierEscapingPotentialKeywords(builder As StringBuilder, identifier As String)
            If IsPotentialKeyword(identifier) Then
                builder.Append("["c)
                builder.Append(identifier)
                builder.Append("]"c)
            Else
                builder.Append(identifier)
            End If
        End Sub

        Protected Overrides Sub AppendGenericTypeArgumentList(builder As StringBuilder, typeArguments() As Type, typeArgumentOffset As Integer, arity As Integer, escapeKeywordIdentifiers As Boolean)
            builder.Append("(Of ")
            For i = 0 To arity - 1
                If i > 0 Then
                    builder.Append(", ")
                End If

                Dim typeArgument As Type = typeArguments(typeArgumentOffset + i)
                AppendQualifiedTypeName(builder, typeArgument, escapeKeywordIdentifiers)
            Next
            builder.Append(")"c)
        End Sub

        Protected Overrides Sub AppendRankSpecifier(builder As StringBuilder, rank As Integer)
            Debug.Assert(rank > 0)

            builder.Append("("c)
            builder.Append(","c, rank - 1)
            builder.Append(")"c)
        End Sub

        Protected Overrides Function AppendSpecialTypeName(builder As StringBuilder, type As Type, escapeKeywordIdentifiers As Boolean) As Boolean
            If type.IsPredefinedType() Then
                builder.Append(type.GetPredefinedTypeName()) ' Not an identifier, does not require escaping.
                Return True
            End If

            If type.IsGenericParameter Then
                AppendIdentifier(builder, escapeKeywordIdentifiers, type.Name)
                Return True
            End If

            Return False
        End Function

    End Class

End Namespace
