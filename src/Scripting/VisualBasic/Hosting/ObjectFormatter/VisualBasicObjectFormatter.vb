' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Reflection
Imports System.Text
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis.Scripting.Hosting.VisualBasic

    Public NotInheritable Class VisualBasicObjectFormatter
        Inherits ObjectFormatter

        Public Shared ReadOnly Property Instance As VisualBasicObjectFormatter = New VisualBasicObjectFormatter()

        Private Sub New()
        End Sub

        Friend Overrides ReadOnly Property VoidDisplayString As Object
            Get
                ' TODO
                Return ""
            End Get
        End Property

        Friend Overrides ReadOnly Property NullLiteral As String
            Get
                Return "Nothing"
            End Get
        End Property

        Friend Overrides Function FormatLiteral(value As Boolean) As String
            Return ObjectDisplay.FormatLiteral(value)
        End Function

        Friend Overrides Function FormatLiteral(value As Date) As String
            Return ObjectDisplay.FormatLiteral(value)
        End Function

        Friend Overrides Function FormatLiteral(value As String, quote As Boolean, Optional useHexadecimalNumbers As Boolean = False) As String
            Dim options = ObjectDisplayOptions.None
            If quote Then
                options = options Or ObjectDisplayOptions.UseQuotes
            End If
            If useHexadecimalNumbers Then
                options = options Or ObjectDisplayOptions.UseHexadecimalNumbers
            End If
            Return ObjectDisplay.FormatLiteral(value, options)
        End Function

        Friend Overrides Function FormatLiteral(c As Char, quote As Boolean, Optional includeCodePoints As Boolean = False, Optional useHexadecimalNumbers As Boolean = False) As String
            Dim options = ObjectDisplayOptions.None
            If quote Then
                options = options Or ObjectDisplayOptions.UseQuotes
            End If
            If includeCodePoints Then
                options = options Or ObjectDisplayOptions.IncludeCodePoints
            End If
            If useHexadecimalNumbers Then
                options = options Or ObjectDisplayOptions.UseHexadecimalNumbers
            End If
            Return ObjectDisplay.FormatLiteral(c, options)
        End Function

        Friend Overrides Function FormatLiteral(value As SByte, Optional useHexadecimalNumbers As Boolean = False) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers))
        End Function

        Friend Overrides Function FormatLiteral(value As Byte, Optional useHexadecimalNumbers As Boolean = False) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers))
        End Function

        Friend Overrides Function FormatLiteral(value As Short, Optional useHexadecimalNumbers As Boolean = False) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers))
        End Function

        Friend Overrides Function FormatLiteral(value As UShort, Optional useHexadecimalNumbers As Boolean = False) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers))
        End Function

        Friend Overrides Function FormatLiteral(value As Integer, Optional useHexadecimalNumbers As Boolean = False) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers))
        End Function

        Friend Overrides Function FormatLiteral(value As UInteger, Optional useHexadecimalNumbers As Boolean = False) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers))
        End Function

        Friend Overrides Function FormatLiteral(value As Long, Optional useHexadecimalNumbers As Boolean = False) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers))
        End Function

        Friend Overrides Function FormatLiteral(value As ULong, Optional useHexadecimalNumbers As Boolean = False) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers))
        End Function

        Friend Overrides Function FormatLiteral(value As Double) As String
            Return ObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.None)
        End Function

        Friend Overrides Function FormatLiteral(value As Single) As String
            Return ObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.None)
        End Function

        Friend Overrides Function FormatLiteral(value As Decimal) As String
            Return ObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.None)
        End Function

        Friend Overrides Function GetPrimitiveTypeName(type As SpecialType) As String
            Select Case type
                Case SpecialType.System_Boolean
                    Return "Boolean"
                Case SpecialType.System_Byte
                    Return "Byte"
                Case SpecialType.System_Char
                    Return "Char"
                Case SpecialType.System_Decimal
                    Return "Decimal"
                Case SpecialType.System_Double
                    Return "Double"
                Case SpecialType.System_Int16
                    Return "Short"
                Case SpecialType.System_Int32
                    Return "Integer"
                Case SpecialType.System_Int64
                    Return "Long"
                Case SpecialType.System_SByte
                    Return "SByte"
                Case SpecialType.System_Single
                    Return "Single"
                Case SpecialType.System_String
                    Return "String"
                Case SpecialType.System_UInt16
                    Return "UShort"
                Case SpecialType.System_UInt32
                    Return "UInteger"
                Case SpecialType.System_UInt64
                    Return "ULong"
                Case SpecialType.System_DateTime
                    Return "Date"
                Case SpecialType.System_Object
                    Return "Object"
                Case Else
                    Return Nothing
            End Select
        End Function

        Friend Overrides ReadOnly Property GenericParameterOpening As String
            Get
                Return "(Of "
            End Get
        End Property

        Friend Overrides ReadOnly Property GenericParameterClosing As String
            Get
                Return ")"
            End Get
        End Property

        Friend Overrides Function FormatGeneratedTypeName(type As Type) As String
            ' TODO:  https://github.com/dotnet/roslyn/issues/3739
            Return Nothing
        End Function

        Friend Overrides Function FormatArrayTypeName(arrayType As Type, arrayOpt As Array, options As ObjectFormattingOptions) As String
            Dim sb As StringBuilder = New StringBuilder()
            ' print the inner-most element type first:
            Dim elementType As Type = arrayType.GetElementType()
            While elementType.IsArray
                elementType = elementType.GetElementType()
            End While

            sb.Append(FormatTypeName(elementType, options))

            ' print all components of a jagged array:
            Dim type As Type = arrayType
            Do
                If arrayOpt IsNot Nothing Then
                    sb.Append("("c)
                    Dim rank As Integer = type.GetArrayRank()
                    Dim anyNonzeroLowerBound As Boolean = False

                    For i = 0 To rank - 1
                        If arrayOpt.GetLowerBound(i) > 0 Then
                            anyNonzeroLowerBound = True
                            Exit Do
                        End If

                        i = i + 1
                    Next

                    For i = 0 To rank - 1
                        Dim lowerBound As Integer = arrayOpt.GetLowerBound(i)
                        Dim length As Integer = arrayOpt.GetLength(i)
                        If i > 0 Then
                            sb.Append(", ")
                        End If

                        If anyNonzeroLowerBound Then
                            AppendArrayBound(sb, lowerBound, options.UseHexadecimalNumbers)
                            sb.Append("..")
                            AppendArrayBound(sb, length + lowerBound, options.UseHexadecimalNumbers)
                        Else
                            AppendArrayBound(sb, length, options.UseHexadecimalNumbers)
                        End If

                        i = i + 1
                    Next

                    sb.Append(")"c)
                    arrayOpt = Nothing
                Else
                    AppendArrayRank(sb, type)
                End If

                type = type.GetElementType()
            Loop While type.IsArray

            Return sb.ToString()
        End Function

        Private Sub AppendArrayBound(sb As StringBuilder, bound As Long, useHexadecimalNumbers As Boolean)
            If bound <= Int32.MaxValue Then
                sb.Append(FormatLiteral(CType(bound, Integer), useHexadecimalNumbers))
            Else
                sb.Append(FormatLiteral(bound, useHexadecimalNumbers))
            End If
        End Sub

        Private Shared Sub AppendArrayRank(sb As StringBuilder, arrayType As Type)
            sb.Append("["c)
            Dim rank As Integer = arrayType.GetArrayRank()
            If rank > 1 Then
                sb.Append(","c, rank - 1)
            End If

            sb.Append("]"c)
        End Sub

        Friend Overrides Function FormatMemberName(member As MemberInfo) As String
            Return member.Name
        End Function

        Friend Overrides Function IsHiddenMember(member As MemberInfo) As Boolean
            ' TODO (tomat)
            Return False
        End Function
    End Class
End Namespace

