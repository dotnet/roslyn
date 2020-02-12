' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFacts

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions

    <Extension()>
    Friend Module StringExtensions

        <Extension()>
        Public Function TryReduceAttributeSuffix(
            identifierText As String,
            ByRef withoutSuffix As String) As Boolean

            ' we can't reduce _Attribute to _ because this is not a valid identifier
            Dim halfWidthValueText = SyntaxFacts.MakeHalfWidthIdentifier(identifierText)
            If halfWidthValueText.GetWithoutAttributeSuffix(isCaseSensitive:=False) <> Nothing AndAlso
                Not (halfWidthValueText.Length = 10 AndAlso halfWidthValueText(0) = "_") Then

                withoutSuffix = identifierText.Substring(0, identifierText.Length - 9)
                Return True
            End If

            Return False
        End Function

        <Extension()>
        Public Function EscapeIdentifier(text As String, Optional afterDot As Boolean = False, Optional symbol As ISymbol = Nothing, Optional withinAsyncMethod As Boolean = False) As String
            Dim keywordKind = SyntaxFacts.GetKeywordKind(text)
            Dim needsEscaping = keywordKind <> SyntaxKind.None

            ' REM and New must always be escaped, but there are some conditions where
            ' keywords are not escaped
            If needsEscaping AndAlso
                keywordKind <> SyntaxKind.REMKeyword AndAlso
                keywordKind <> SyntaxKind.NewKeyword Then

                needsEscaping = Not afterDot

                If needsEscaping Then
                    Dim typeSymbol = TryCast(symbol, ITypeSymbol)
                    needsEscaping = typeSymbol Is Nothing OrElse Not IsPredefinedType(typeSymbol)
                End If
            End If

            ' GetKeywordKind won't return SyntaxKind.AwaitKeyword (943836)
            If withinAsyncMethod AndAlso text = "Await" Then
                needsEscaping = True
            End If

            Return If(needsEscaping, "[" & text & "]", text)
        End Function

        <Extension()>
        Public Function ToIdentifierToken(text As String, Optional afterDot As Boolean = False, Optional symbol As ISymbol = Nothing, Optional withinAsyncMethod As Boolean = False) As SyntaxToken
            Contract.ThrowIfNull(text)

            Dim unescaped = text
            Dim wasAlreadyEscaped = False

            If text.Length > 2 AndAlso MakeHalfWidthIdentifier(text.First()) = "[" AndAlso MakeHalfWidthIdentifier(text.Last()) = "]" Then
                unescaped = text.Substring(1, text.Length() - 2)
                wasAlreadyEscaped = True
            End If

            Dim escaped = EscapeIdentifier(text, afterDot, symbol, withinAsyncMethod)
            Dim token = If(escaped.Length > 0 AndAlso escaped(0) = "["c,
                SyntaxFactory.Identifier(escaped, isBracketed:=True, identifierText:=unescaped, typeCharacter:=TypeCharacter.None),
                SyntaxFactory.Identifier(text))

            If Not wasAlreadyEscaped Then
                token = token.WithAdditionalAnnotations(Simplifier.Annotation)
            End If

            Return token
        End Function

        <Extension()>
        Public Function ToModifiedIdentifier(text As String) As ModifiedIdentifierSyntax
            Return SyntaxFactory.ModifiedIdentifier(text.ToIdentifierToken)
        End Function

        <Extension()>
        Public Function ToIdentifierName(text As String) As IdentifierNameSyntax
            Contract.ThrowIfNull(text)
            Return SyntaxFactory.IdentifierName(text.ToIdentifierToken()).WithAdditionalAnnotations(Simplifier.Annotation)
        End Function

        Private Function IsPredefinedType(type As ITypeSymbol) As Boolean
            Select Case type.SpecialType
                Case SpecialType.System_Boolean,
                     SpecialType.System_Byte,
                     SpecialType.System_SByte,
                     SpecialType.System_Int16,
                     SpecialType.System_UInt16,
                     SpecialType.System_Int32,
                     SpecialType.System_UInt32,
                     SpecialType.System_Int64,
                     SpecialType.System_UInt64,
                     SpecialType.System_Single,
                     SpecialType.System_Double,
                     SpecialType.System_Decimal,
                     SpecialType.System_DateTime,
                     SpecialType.System_Char,
                     SpecialType.System_String,
                     SpecialType.System_Object
                    Return True
                Case Else
                    Return False
            End Select
        End Function

    End Module
End Namespace
