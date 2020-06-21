' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    Public Module TypedConstantExtensions
        ''' <summary>
        ''' Returns the System.String that represents the current TypedConstant.
        ''' </summary>
        ''' <returns>A System.String that represents the current TypedConstant.</returns>
        <Extension>
        Public Function ToVisualBasicString(constant As TypedConstant) As String
            If constant.IsNull Then
                Return "Nothing"
            End If

            If constant.Kind = TypedConstantKind.Array Then
                Return "{" & String.Join(", ", constant.Values.Select(Function(v) v.ToVisualBasicString())) & "}"
            End If

            If constant.Kind = TypedConstantKind.Type OrElse constant.TypeInternal.SpecialType = SpecialType.System_Object Then
                Return "GetType(" & constant.Value.ToString() & ")"
            End If

            If constant.Kind = TypedConstantKind.Enum Then
                ' TODO (tomat): use SymbolDisplay instead
                Return DisplayEnumConstant(constant)
            End If

            Return SymbolDisplay.FormatPrimitive(constant.ValueInternal, quoteStrings:=True, useHexadecimalNumbers:=False)
        End Function

        ' Decode the value of enum constant
        Private Function DisplayEnumConstant(constant As TypedConstant) As String
            Debug.Assert(constant.Kind = TypedConstantKind.Enum)

            ' Create a ConstantValue of enum underlying type
            Dim splType As SpecialType = DirectCast(constant.TypeInternal, NamedTypeSymbol).EnumUnderlyingType.SpecialType
            Dim valueConstant As ConstantValue = ConstantValue.Create(constant.ValueInternal, splType)

            Dim typeName As String = constant.Type.ToDisplayString(SymbolDisplayFormat.QualifiedNameOnlyFormat)
            If valueConstant.IsUnsigned Then
                Return DisplayUnsignedEnumConstant(constant, splType, valueConstant.UInt64Value, typeName)
            Else
                Return DisplaySignedEnumConstant(constant, splType, valueConstant.Int64Value, typeName)
            End If

        End Function

        Private Function DisplayUnsignedEnumConstant(constant As TypedConstant, splType As SpecialType, ByVal constantToDecode As ULong, ByVal typeName As String) As String
            ' Specified valueConstant might have an exact matching enum field
            ' or it might be a bitwise Or of multiple enum fields.
            ' For the later case, we keep track of the current value of
            ' bitwise Or of possible enum fields.
            Dim curValue As ULong = 0

            ' Initialize the value string to empty
            Dim pooledBuilder As PooledStringBuilder = Nothing
            Dim valueStringBuilder As StringBuilder = Nothing

            ' Iterate through all the constant members in the enum type
            Dim members = constant.Type.GetMembers()
            For Each member In members
                Dim field = TryCast(member, IFieldSymbol)
                If field IsNot Nothing AndAlso field.HasConstantValue Then
                    Dim memberConstant = ConstantValue.Create(field.ConstantValue, splType)
                    Dim memberValue = memberConstant.UInt64Value

                    ' Do we have an exact matching enum field
                    If memberValue = constantToDecode Then
                        If Not pooledBuilder Is Nothing Then
                            pooledBuilder.Free()
                        End If

                        Return typeName & "." & field.Name
                    End If

                    ' specifiedValue might be a bitwise Or of multiple enum fields
                    ' Is the current member included in the specified value?
                    If (memberValue And constantToDecode) = memberValue Then
                        ' update the current value
                        curValue = curValue Or memberValue

                        If valueStringBuilder Is Nothing Then
                            pooledBuilder = PooledStringBuilder.GetInstance()
                            valueStringBuilder = pooledBuilder.Builder

                        Else
                            valueStringBuilder.Append(" Or ")

                        End If

                        valueStringBuilder.Append(typeName)
                        valueStringBuilder.Append(".")
                        valueStringBuilder.Append(field.Name)
                    End If
                End If
            Next

            If Not pooledBuilder Is Nothing Then
                If curValue = constantToDecode Then
                    ' return decoded enum constant
                    Return pooledBuilder.ToStringAndFree()
                End If

                ' Unable to decode the enum constant
                pooledBuilder.Free()
            End If

            ' Unable to decode the enum constant, just display the integral value
            Return constant.ValueInternal.ToString()
        End Function

        Private Function DisplaySignedEnumConstant(constant As TypedConstant, ByVal splType As SpecialType, ByVal constantToDecode As Long, ByVal typeName As String) As String
            ' Specified valueConstant might have an exact matching enum field
            ' or it might be a bitwise Or of multiple enum fields.
            ' For the later case, we keep track of the current value of
            ' bitwise Or of possible enum fields.
            Dim curValue As Long = 0

            ' Initialize the value string to empty
            Dim pooledBuilder As PooledStringBuilder = Nothing
            Dim valueStringBuilder As StringBuilder = Nothing

            ' Iterate through all the constant members in the enum type
            Dim members = constant.Type.GetMembers()
            For Each member In members

                Dim field = TryCast(member, IFieldSymbol)
                If field IsNot Nothing AndAlso field.HasConstantValue Then
                    Dim memberConstant = ConstantValue.Create(field.ConstantValue, splType)
                    Dim memberValue = memberConstant.Int64Value

                    ' Do we have an exact matching enum field
                    If memberValue = constantToDecode Then
                        If Not pooledBuilder Is Nothing Then
                            pooledBuilder.Free()
                        End If

                        Return typeName & "." & field.Name
                    End If

                    ' specifiedValue might be a bitwise Or of multiple enum fields
                    ' Is the current member included in the specified value?
                    If (memberValue And constantToDecode) = memberValue Then
                        ' update the current value
                        curValue = curValue Or memberValue

                        If valueStringBuilder Is Nothing Then
                            pooledBuilder = PooledStringBuilder.GetInstance()
                            valueStringBuilder = pooledBuilder.Builder

                        Else
                            valueStringBuilder.Append(" Or ")

                        End If

                        valueStringBuilder.Append(typeName)
                        valueStringBuilder.Append(".")
                        valueStringBuilder.Append(field.Name)
                    End If
                End If
            Next

            If Not pooledBuilder Is Nothing Then
                If curValue = constantToDecode Then
                    ' return decoded enum constant
                    Return pooledBuilder.ToStringAndFree()
                End If

                ' Unable to decode the enum constant
                pooledBuilder.Free()
            End If

            ' Unable to decode the enum constant, just display the integral value
            Return constant.ValueInternal.ToString()
        End Function

    End Module

#If False Then
    ''' <summary>
    ''' TypedConstant represents a constant value used as an argument to an Attribute. The Typed constant can represent
    ''' a primitive type, an enum type, a system.type or an array of TypedConstants. 
    ''' 
    ''' Kind            _value                              _type
    ''' Primitive       Boxed value, string or nothing      TypeSymbol for the boxed value possibly nothing for nothing literal
    ''' Enum            Boxed value of the underlying type  TypeSymbol for enum
    ''' Type            TypeSymbol or nothing               TypeSymbol for System.Type
    ''' Array           TypeConstant() or nothing           ArrayTypeSymbol
    ''' Error           nothing                             TypeSymbol or ErrorTypeSymbol
    ''' 
    ''' </summary>
    Public Structure TypedConstant
        Private ReadOnly _kind As TypedConstantKind
        Private ReadOnly _type As TypeSymbol
        Private ReadOnly _value As TypedConstantValue

        Private Sub New(type As TypeSymbol, kind As TypedConstantKind, value As TypedConstantValue)
            Debug.Assert(type IsNot Nothing OrElse kind = TypedConstantKind.Error AndAlso value.IsNull)
            _value = value
            _kind = kind
            _type = type
        End Sub

        Friend Sub New(constant As TypedConstant)
            MyClass.New(DirectCast(constant.Type, TypeSymbol), constant.Kind, constant.RawValue)
        End Sub

        Friend Sub New(type As TypeSymbol, array As ImmutableArray(Of TypedConstant))
            MyClass.New(type, TypedConstantKind.Array, array)
        End Sub

        Friend Sub New(type As TypeSymbol, kind As TypedConstantKind, value As Object)
            MyClass.New(type, kind, New TypedConstantValue(value))

            Debug.Assert(kind <> TypedConstantKind.Array)
        End Sub

        Public Shared Widening Operator CType(constant As TypedConstant) As TypedConstant
            Return New TypedConstant(constant._type, constant._kind, constant._value)
        End Operator

        Public Shared Narrowing Operator CType(constant As TypedConstant) As TypedConstant
            Return New TypedConstant(constant)
        End Operator

        ''' <summary>
        ''' The TypedConstant's kind. Can be one of Primitive, Enum, Type, Array or Error
        ''' </summary>
        Public ReadOnly Property Kind As TypedConstantKind
            Get
                Return _kind
            End Get
        End Property

        ''' <summary>
        ''' True if the constant represents a null literal.
        ''' </summary>
        Public ReadOnly Property IsNull As Boolean
            Get
                Return _value.IsNull
            End Get
        End Property

        ''' <summary>
        ''' Returns the value of a non-array <see cref="TypedConstant"/>,
        ''' for primitive constants a boxed value, string, or Nothing,
        ''' for enum constants a boxed value of the underlying type,
        ''' for type constants a <see cref="TypeSymbol"/>.
        ''' </summary>
        ''' <exception cref="InvalidOperationException">Constant represents an array, use <see cref="Values"/> to read its value.</exception>
        Public ReadOnly Property Value As Object
            Get
                If _kind = TypedConstantKind.Array Then
                    Throw New InvalidOperationException("TypedConstant is an array. Use Values property.")
                End If

                Return _value.Object
            End Get
        End Property

        ''' <summary>
        ''' Returns the value for array constants.
        ''' </summary>
        ''' <exception cref="InvalidOperationException">Constant represents an array, use <see cref="Values"/> to read its value.</exception>
        Public ReadOnly Property Values As IEnumerable(Of TypedConstant)
            Get
                If _kind <> TypedConstantKind.Array Then
                    Throw New InvalidOperationException("TypedConstant is not an array. Use Value property.")
                End If

                If IsNull Then
                    Return Nothing
                End If

                Return _value.Array.Select(Function(constant) New TypedConstant(constant))
            End Get
        End Property

        ''' <summary>
        ''' The TypedConstant's type. This is either a TypeSymbol of one of the primitive types, a TypeSymbol of an enum
        ''' type, a TypeSymbol for System.Type or an ArrayTypeSymbol.
        ''' </summary>
        Public ReadOnly Property Type As TypeSymbol
            Get
                Return _type
            End Get
        End Property

#Region "Testing & Debugging"

        ''' <summary>
        ''' Returns the System.String that represents the current TypedConstant.
        ''' </summary>
        ''' <returns>A System.String that represents the current TypedConstant.</returns>
        Public Overrides Function ToString() As String
            If _value.IsNull Then
                Return "Nothing"
            End If

            If _kind = TypedConstantKind.Array Then
                Return "{" & String.Join(", ", _value.Array.Select(Function(v) New TypedConstant(v).ToString())) & "}"
            End If

            If _kind = TypedConstantKind.Type OrElse _type.SpecialType = SpecialType.System_Object Then
                Return "GetType(" & _value.Object.ToString() & ")"
            End If

            If _kind = TypedConstantKind.Enum Then
                ' TODO (tomat): use SymbolDisplay instead
                Return DisplayEnumConstant()
            End If

            Return SymbolDisplay.FormatPrimitive(_value.Object, quoteStrings:=True, useHexadecimalNumbers:=False)
        End Function

        ' Decode the value of enum constant
        Private Function DisplayEnumConstant() As String
            Debug.Assert(Kind = TypedConstantKind.Enum)

            ' Create a ConstantValue of enum underlying type
            Dim splType As SpecialType = Me.Type.GetEnumUnderlyingTypeOrSelf().SpecialType
            Dim valueConstant As ConstantValue = ConstantValue.Create(Me.Value, splType)

            Dim typeName As String = Me.Type.ToDisplayString(SymbolDisplayFormat.QualifiedNameOnlyFormat)
            If valueConstant.IsUnsigned Then
                Return DisplayUnsignedEnumConstant(splType, valueConstant.UInt64Value, typeName)
            Else
                Return DisplaySignedEnumConstant(splType, valueConstant.Int64Value, typeName)
            End If

        End Function

        Private Function DisplayUnsignedEnumConstant(ByVal splType As SpecialType, ByVal constantToDecode As ULong, ByVal typeName As String) As String
            ' Specified valueConstant might have an exact matching enum field
            ' or it might be a bitwise Or of multiple enum fields.
            ' For the later case, we keep track of the current value of
            ' bitwise Or of possible enum fields.
            Dim curValue As ULong = 0

            ' Initialize the value string to empty
            Dim pooledBuilder As PooledStringBuilder = Nothing
            Dim valueStringBuilder As StringBuilder = Nothing

            ' Iterate through all the constant members in the enum type
            Dim members As ImmutableArray(Of Symbol) = Me.Type.GetMembers()
            For Each member As Symbol In members
                Dim field = TryCast(member, FieldSymbol)
                If field IsNot Nothing AndAlso field.HasConstantValue Then
                    Dim memberConstant = ConstantValue.Create(field.ConstantValue, splType)
                    Dim memberValue = memberConstant.UInt64Value

                    ' Do we have an exact matching enum field
                    If memberValue = constantToDecode Then
                        If Not pooledBuilder Is Nothing Then
                            pooledBuilder.Free()
                        End If

                        Return typeName & "." & field.Name
                    End If

                    ' specifiedValue might be a bitwise Or of multiple enum fields
                    ' Is the current member included in the specified value?
                    If (memberValue And constantToDecode) = memberValue Then
                        ' update the current value
                        curValue = curValue Or memberValue

                        If valueStringBuilder Is Nothing Then
                            pooledBuilder = PooledStringBuilder.GetInstance()
                            valueStringBuilder = pooledBuilder.Builder

                        Else
                            valueStringBuilder.Append(" Or ")

                        End If

                        valueStringBuilder.Append(typeName)
                        valueStringBuilder.Append(".")
                        valueStringBuilder.Append(field.Name)
                    End If
                End If
            Next

            If Not pooledBuilder Is Nothing Then
                If curValue = constantToDecode Then
                    ' return decoded enum constant
                    Return pooledBuilder.ToStringAndFree()
                End If

                ' Unable to decode the enum constant
                pooledBuilder.Free()
            End If

            ' Unable to decode the enum constant, just display the integral value
            Return Value.ToString()
        End Function

        Private Function DisplaySignedEnumConstant(ByVal splType As SpecialType, ByVal constantToDecode As Long, ByVal typeName As String) As String
            ' Specified valueConstant might have an exact matching enum field
            ' or it might be a bitwise Or of multiple enum fields.
            ' For the later case, we keep track of the current value of
            ' bitwise Or of possible enum fields.
            Dim curValue As Long = 0

            ' Initialize the value string to empty
            Dim pooledBuilder As PooledStringBuilder = Nothing
            Dim valueStringBuilder As StringBuilder = Nothing

            ' Iterate through all the constant members in the enum type
            Dim members As ImmutableArray(Of Symbol) = Me.Type.GetMembers()
            For Each member As Symbol In members

                Dim field = TryCast(member, FieldSymbol)
                If field IsNot Nothing AndAlso field.HasConstantValue Then
                    Dim memberConstant = ConstantValue.Create(field.ConstantValue, splType)
                    Dim memberValue = memberConstant.Int64Value

                    ' Do we have an exact matching enum field
                    If memberValue = constantToDecode Then
                        If Not pooledBuilder Is Nothing Then
                            pooledBuilder.Free()
                        End If

                        Return typeName & "." & field.Name
                    End If

                    ' specifiedValue might be a bitwise Or of multiple enum fields
                    ' Is the current member included in the specified value?
                    If (memberValue And constantToDecode) = memberValue Then
                        ' update the current value
                        curValue = curValue Or memberValue

                        If valueStringBuilder Is Nothing Then
                            pooledBuilder = PooledStringBuilder.GetInstance()
                            valueStringBuilder = pooledBuilder.Builder

                        Else
                            valueStringBuilder.Append(" Or ")

                        End If

                        valueStringBuilder.Append(typeName)
                        valueStringBuilder.Append(".")
                        valueStringBuilder.Append(field.Name)
                    End If
                End If
            Next

            If Not pooledBuilder Is Nothing Then
                If curValue = constantToDecode Then
                    ' return decoded enum constant
                    Return pooledBuilder.ToStringAndFree()
                End If

                ' Unable to decode the enum constant
                pooledBuilder.Free()
            End If

            ' Unable to decode the enum constant, just display the integral value
            Return Value.ToString()
        End Function

#End Region
    End Structure
#End If
End Namespace
