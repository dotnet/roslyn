' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Text
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis.Scripting.VisualBasic

    Public NotInheritable Class VisualBasicObjectFormatter
        Inherits ObjectFormatter

        Public Shared ReadOnly Instance As VisualBasicObjectFormatter = New VisualBasicObjectFormatter()

        Private Sub New()
        End Sub

        Public Overrides ReadOnly Property VoidDisplayString As Object
            Get
                ' TODO
                Return ""
            End Get
        End Property

        Public Overrides ReadOnly Property NullLiteral As String
            Get
                Return "Nothing"
            End Get
        End Property

        Public Overrides Function FormatLiteral(value As Boolean) As String
            Return ObjectDisplay.FormatLiteral(value)
        End Function

        Public Overloads Function FormatLiteral(value As DateTime) As String
            Return ObjectDisplay.FormatLiteral(value)
        End Function

        Public Overrides Function FormatLiteral(value As String, quote As Boolean, Optional useHexadecimalNumbers As Boolean = False) As String
            Dim options = ObjectDisplayOptions.None
            If quote Then
                options = options Or ObjectDisplayOptions.UseQuotes
            End If
            If useHexadecimalNumbers Then
                options = options Or ObjectDisplayOptions.UseHexadecimalNumbers
            End If
            Return ObjectDisplay.FormatLiteral(value, options)
        End Function

        Public Overrides Function FormatLiteral(c As Char, quote As Boolean, Optional includeCodePoints As Boolean = False, Optional useHexadecimalNumbers As Boolean = False) As String
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

        Public Overrides Function FormatLiteral(value As SByte, Optional useHexadecimalNumbers As Boolean = False) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers))
        End Function

        Public Overrides Function FormatLiteral(value As Byte, Optional useHexadecimalNumbers As Boolean = False) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers))
        End Function

        Public Overrides Function FormatLiteral(value As Short, Optional useHexadecimalNumbers As Boolean = False) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers))
        End Function

        Public Overrides Function FormatLiteral(value As UShort, Optional useHexadecimalNumbers As Boolean = False) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers))
        End Function

        Public Overrides Function FormatLiteral(value As Integer, Optional useHexadecimalNumbers As Boolean = False) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers))
        End Function

        Public Overrides Function FormatLiteral(value As UInteger, Optional useHexadecimalNumbers As Boolean = False) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers))
        End Function

        Public Overrides Function FormatLiteral(value As Long, Optional useHexadecimalNumbers As Boolean = False) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers))
        End Function

        Public Overrides Function FormatLiteral(value As ULong, Optional useHexadecimalNumbers As Boolean = False) As String
            Return ObjectDisplay.FormatLiteral(value, GetObjectDisplayOptions(useHexadecimalNumbers))
        End Function

        Public Overrides Function FormatLiteral(value As Double) As String
            Return ObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.None)
        End Function

        Public Overrides Function FormatLiteral(value As Single) As String
            Return ObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.None)
        End Function

        Public Overrides Function FormatLiteral(value As Decimal) As String
            Return ObjectDisplay.FormatLiteral(value, ObjectDisplayOptions.None)
        End Function

        Public Overrides Function FormatTypeName(type As Type, options As ObjectFormattingOptions) As String
            Return If(GetPrimitiveTypeName(type), AppendComplexTypeName(New StringBuilder(), type, options).ToString())
        End Function

        Private Shared Function GetPrimitiveTypeName(type As Type) As String
            Select Case Type.GetTypeCode(type)
                Case TypeCode.Boolean
                    Return "Boolean"
                Case TypeCode.Byte
                    Return "Byte"
                Case TypeCode.Char
                    Return "Char"
                Case TypeCode.Decimal
                    Return "Decimal"
                Case TypeCode.Double
                    Return "Double"
                Case TypeCode.Int16
                    Return "Short"
                Case TypeCode.Int32
                    Return "Integer"
                Case TypeCode.Int64
                    Return "Long"
                Case TypeCode.SByte
                    Return "SByte"
                Case TypeCode.Single
                    Return "Single"
                Case TypeCode.String
                    Return "String"
                Case TypeCode.UInt16
                    Return "UShort"
                Case TypeCode.UInt32
                    Return "UInteger"
                Case TypeCode.UInt64
                    Return "ULong"
                Case Else
                    If type = GetType(Object) Then
                        Return "Object"
                    End If

                    Return Nothing
            End Select
        End Function

        Private Function AppendComplexTypeName(builder As StringBuilder, type As Type, options As ObjectFormattingOptions) As StringBuilder
            If type.IsArray Then
                builder.Append(FormatArrayTypeName(type, arrayOpt:=Nothing, options:=options))
                Return builder
            End If

            ' compiler generated (e.g. iterator/async)
            ' Dim stateMachineName As String
            ' If GeneratedNames.TryParseSourceMethodNameFromGeneratedName(type.Name, GeneratedNameKind.StateMachineType, stateMachineName) Then
            '     builder.Append(stateMachineName)
            '     Return builder
            ' End If

            If type.IsGenericType Then
                ' consolidated generic arguments (includes arguments of all declaring types):
                Dim genericArguments As Type() = type.GetGenericArguments()
                If type.DeclaringType IsNot Nothing Then
                    Dim nestedTypes As List(Of Type) = New List(Of Type)()
                    Do
                        nestedTypes.Add(type)
                        type = type.DeclaringType
                    Loop While type IsNot Nothing

                    Dim typeArgumentIndex As Integer = 0
                    Dim i As Integer = nestedTypes.Count - 1

                    While i >= 0
                        AppendTypeInstantiation(builder, nestedTypes(i), genericArguments, typeArgumentIndex, options)
                        If i > 0 Then
                            builder.Append("."c)
                        End If

                        i = i - 1
                    End While
                Else
                    Dim typeArgumentIndex As Integer = 0
                    Return AppendTypeInstantiation(builder, type, genericArguments, typeArgumentIndex, options)
                End If
            ElseIf type.DeclaringType IsNot Nothing Then
                builder.Append(type.Name.Replace("+"c, "."c))
            Else
                builder.Append(type.Name)
            End If

            Return builder
        End Function

        Private Function AppendTypeInstantiation(builder As StringBuilder, type As Type, genericArguments As Type(), ByRef genericArgIndex As Integer, options As ObjectFormattingOptions) As StringBuilder
            ' generic arguments of all the outer types and the current type;
            Dim currentGenericArgs As Type() = type.GetGenericArguments()
            Dim currentArgCount As Integer = currentGenericArgs.Length - genericArgIndex
            If currentArgCount > 0 Then
                Dim backtick As Integer = type.Name.IndexOf("`"c)
                If backtick > 0 Then
                    builder.Append(type.Name.Substring(0, backtick))
                Else
                    builder.Append(type.Name)
                End If

                builder.Append("(Of ")
                Dim i As Integer = 0

                While i < currentArgCount
                    If i > 0 Then
                        builder.Append(", ")
                    End If

                    builder.Append(FormatTypeName(genericArguments(genericArgIndex), options))
                    genericArgIndex = genericArgIndex + 1
                    i = i + 1
                End While

                builder.Append(")"c)
            Else
                builder.Append(type.Name)
            End If

            Return builder
        End Function

        Public Overrides Function FormatArrayTypeName(array As Array, options As ObjectFormattingOptions) As String
            Return FormatArrayTypeName(array.[GetType](), array, options)
        End Function

        Private Overloads Function FormatArrayTypeName(arrayType As Type, arrayOpt As Array, options As ObjectFormattingOptions) As String
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
                        Dim length As Long = arrayOpt.GetLongLength(i)
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

        Public Overrides Function FormatMemberName(member As System.Reflection.MemberInfo) As String
            Return member.Name
        End Function

        Public Overrides Function IsHiddenMember(member As System.Reflection.MemberInfo) As Boolean
            ' TODO (tomat)
            Return False
        End Function
    End Class
End Namespace

