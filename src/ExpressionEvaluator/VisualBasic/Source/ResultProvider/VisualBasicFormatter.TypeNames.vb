' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.ObjectModel
Imports System.Runtime.InteropServices
Imports System.Text
Imports Type = Microsoft.VisualStudio.Debugger.Metadata.Type

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    ' Implementation for "displaying type name as string" aspect of Visual Basic Formatter component
    Partial Friend NotInheritable Class VisualBasicFormatter

        Private Shared Function IsPotentialKeyword(identifier As String) As Boolean
            Return SyntaxFacts.GetKeywordKind(identifier) <> SyntaxKind.None OrElse SyntaxFacts.GetContextualKeywordKind(identifier) <> SyntaxKind.None
        End Function

        Protected Overrides Sub AppendIdentifierEscapingPotentialKeywords(builder As StringBuilder, identifier As String, <Out> ByRef sawInvalidIdentifier As Boolean)
            sawInvalidIdentifier = Not IsValidIdentifier(identifier)

            If IsPotentialKeyword(identifier) Then
                builder.Append("["c)
                builder.Append(identifier)
                builder.Append("]"c)
            Else
                builder.Append(identifier)
            End If
        End Sub

        Protected Overrides Sub AppendGenericTypeArguments(
            builder As StringBuilder,
            typeArguments() As Type,
            typeArgumentOffset As Integer,
            dynamicFlags As ReadOnlyCollection(Of Byte),
            ByRef dynamicFlagIndex As Integer,
            tupleElementNames As ReadOnlyCollection(Of String),
            ByRef tupleElementIndex As Integer,
            arity As Integer,
            escapeKeywordIdentifiers As Boolean,
            <Out> ByRef sawInvalidIdentifier As Boolean)

            sawInvalidIdentifier = False
            builder.Append("(Of ")
            For i = 0 To arity - 1
                If i > 0 Then
                    builder.Append(", ")
                End If

                Dim sawSingleInvalidIdentifier = False
                Dim typeArgument As Type = typeArguments(typeArgumentOffset + i)
                AppendQualifiedTypeName(
                    builder,
                    typeArgument,
                    dynamicFlags,
                    dynamicFlagIndex,
                    tupleElementNames,
                    tupleElementIndex,
                    escapeKeywordIdentifiers,
                    sawSingleInvalidIdentifier)
                sawInvalidIdentifier = sawInvalidIdentifier Or sawSingleInvalidIdentifier
            Next
            builder.Append(")"c)
        End Sub

        Protected Overrides Sub AppendTupleElement(
            builder As StringBuilder,
            type As Type,
            nameOpt As String,
            dynamicFlags As ReadOnlyCollection(Of Byte),
            ByRef dynamicFlagIndex As Integer,
            tupleElementNames As ReadOnlyCollection(Of String),
            ByRef tupleElementIndex As Integer,
            escapeKeywordIdentifiers As Boolean,
            <Out> ByRef sawInvalidIdentifier As Boolean)

            sawInvalidIdentifier = False
            Dim sawSingleInvalidIdentifier = False
            If Not String.IsNullOrEmpty(nameOpt) Then
                AppendIdentifier(builder, escapeKeywordIdentifiers, nameOpt, sawSingleInvalidIdentifier)
                sawInvalidIdentifier = sawInvalidIdentifier Or sawSingleInvalidIdentifier
                builder.Append(" As ")
            End If
            AppendQualifiedTypeName(
                    builder,
                    type,
                    dynamicFlags,
                    dynamicFlagIndex,
                    tupleElementNames,
                    tupleElementIndex,
                    escapeKeywordIdentifiers,
                    sawSingleInvalidIdentifier)
            Debug.Assert(Not sawSingleInvalidIdentifier)
        End Sub

        Protected Overrides Sub AppendRankSpecifier(builder As StringBuilder, rank As Integer)
            Debug.Assert(rank > 0)

            builder.Append("("c)
            builder.Append(","c, rank - 1)
            builder.Append(")"c)
        End Sub

        Protected Overrides Function AppendSpecialTypeName(builder As StringBuilder, type As Type, isDynamic As Boolean) As Boolean
            ' NOTE: isDynamic is ignored in VB.

            If type.IsPredefinedType() Then
                builder.Append(type.GetPredefinedTypeName()) ' Not an identifier, does not require escaping.
                Return True
            End If

            Return False
        End Function

    End Class

End Namespace
