' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Globalization
Imports System.Text
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    Partial Friend Module ExpressionGenerator
        Private Structure StringPiece
            Public ReadOnly Value As String
            Public ReadOnly Kind As StringPieceKind

            Public Sub New(value As String, kind As StringPieceKind)
                Me.Value = value
                Me.Kind = kind
            End Sub

            Private Shared Function IsPrintable(c As Char) As Boolean
                Dim category = CharUnicodeInfo.GetUnicodeCategory(c)
                Return IsPrintable(category) AndAlso Not IsQuoteCharacter(c)
            End Function

            Private Shared Function IsPrintable(c As UnicodeCategory) As Boolean
                Return c <> UnicodeCategory.OtherNotAssigned AndAlso
                    c <> UnicodeCategory.ParagraphSeparator AndAlso
                    c <> UnicodeCategory.Control AndAlso
                    c <> UnicodeCategory.Surrogate
            End Function

            Private Shared Function IsQuoteCharacter(c As Char) As Boolean
                Const DWCH_LSMART_DQ As Char = ChrW(&H201CS)      '// DW left single
                Const DWCH_RSMART_DQ As Char = ChrW(&H201DS)      '// DW right single smart quote
                Const DWCH_DQ As Char = ChrW(AscW(""""c) + (&HFF00US - &H20US))      '// DW dual quote 

                Return c = DWCH_LSMART_DQ OrElse c = DWCH_RSMART_DQ OrElse c = DWCH_DQ
            End Function

            Public Function GenerateExpression() As ExpressionSyntax
                Select Case Me.Kind
                    Case StringPieceKind.Normal
                        Dim literal = VisualBasic.SymbolDisplay.FormatPrimitive(Me.Value, quoteStrings:=True, useHexadecimalNumbers:=False)
                        Return SyntaxFactory.StringLiteralExpression(
                            SyntaxFactory.StringLiteralToken(literal, Me.Value))
                    Case StringPieceKind.NonPrintable
                        Return GenerateChrWExpression(Me.Value(0))
                    Case StringPieceKind.Cr
                        Return GenerateStringConstantExpression("vbCr")
                    Case StringPieceKind.Lf
                        Return GenerateStringConstantExpression("vbLf")
                    Case StringPieceKind.CrLf
                        Return GenerateStringConstantExpression("vbCrLf")
                    Case StringPieceKind.NullChar
                        Return GenerateStringConstantExpression("vbNullChar")
                    Case StringPieceKind.Back
                        Return GenerateStringConstantExpression("vbBack")
                    Case StringPieceKind.FormFeed
                        Return GenerateStringConstantExpression("vbFormFeed")
                    Case StringPieceKind.Tab
                        Return GenerateStringConstantExpression("vbTab")
                    Case StringPieceKind.VerticalTab
                        Return GenerateStringConstantExpression("vbVerticalTab")
                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(Me.Kind)
                End Select
            End Function

            Private Shared Function GenerateStringConstantExpression(name As String) As MemberAccessExpressionSyntax
                Dim result = GenerateMemberAccessExpression("Microsoft", "VisualBasic", "Constants", name)

                Return result.WithAdditionalAnnotations(Simplifier.Annotation)
            End Function

            Public Shared Function Split(value As String) As IList(Of StringPiece)
                Dim result = New List(Of StringPiece)

                Dim sb = New StringBuilder()

                Dim i = 0
                While i < value.Length
                    Dim c = value(i)
                    i += 1

                    ' Handle unicode surrogates.  If this character is a surrogate, but we get a
                    ' viable surrogate pair, then just add the pair to the resultant string piece.
                    Dim category = CharUnicodeInfo.GetUnicodeCategory(c)
                    If category = UnicodeCategory.Surrogate Then
                        Dim fullCategory = CharUnicodeInfo.GetUnicodeCategory(value, i - 1)
                        If IsPrintable(fullCategory) Then
                            sb.Append(c)
                            sb.Append(value(i))
                            i += 1
                            Continue While
                        End If
                    End If

                    If IsPrintable(c) Then
                        sb.Append(c)
                    Else
                        If sb.Length > 0 Then
                            result.Add(New StringPiece(sb.ToString(), StringPieceKind.Normal))
                            sb.Clear()
                        End If

                        If c = vbNullChar Then
                            result.Add(New StringPiece(Nothing, StringPieceKind.NullChar))
                        ElseIf c = vbBack Then
                            result.Add(New StringPiece(Nothing, StringPieceKind.Back))
                        ElseIf c = vbFormFeed Then
                            result.Add(New StringPiece(Nothing, StringPieceKind.FormFeed))
                        ElseIf c = vbTab Then
                            result.Add(New StringPiece(Nothing, StringPieceKind.Tab))
                        ElseIf c = vbVerticalTab Then
                            result.Add(New StringPiece(Nothing, StringPieceKind.VerticalTab))
                        ElseIf c = vbCr Then
                            If i < value.Length AndAlso value(i) = vbLf Then
                                result.Add(New StringPiece(Nothing, StringPieceKind.CrLf))
                                i = i + 1
                            Else
                                result.Add(New StringPiece(Nothing, StringPieceKind.Cr))
                            End If
                        ElseIf c = vbLf Then
                            result.Add(New StringPiece(Nothing, StringPieceKind.Lf))
                        Else
                            result.Add(New StringPiece(c, StringPieceKind.NonPrintable))
                        End If
                    End If
                End While

                If sb.Length > 0 Then
                    result.Add(New StringPiece(sb.ToString(), StringPieceKind.Normal))
                    sb.Clear()
                End If

                Return result
            End Function
        End Structure
    End Module
End Namespace
