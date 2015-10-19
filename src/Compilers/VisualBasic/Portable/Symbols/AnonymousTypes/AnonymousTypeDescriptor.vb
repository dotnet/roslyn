' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics.CodeAnalysis
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary> 
    ''' Describes anonymous type/delegate in terms of fields/parameters
    ''' </summary>
    Friend Structure AnonymousTypeDescriptor
        Implements IEquatable(Of AnonymousTypeDescriptor)

        Public Shared ReadOnly SubReturnParameterName As String = "Sub"
        Public Shared ReadOnly FunctionReturnParameterName As String = "Function"

        Friend Shared Function GetReturnParameterName(isFunction As Boolean) As String
            Return If(isFunction, FunctionReturnParameterName, SubReturnParameterName)
        End Function

        ''' <summary> Anonymous type/delegate location </summary>
        Public ReadOnly Location As Location

        ''' <summary> Anonymous type fields </summary>
        Public ReadOnly Fields As ImmutableArray(Of AnonymousTypeField)

        ''' <summary> 
        ''' Anonymous type descriptor Key 
        ''' 
        ''' The key is being used to separate anonymous type templates, for example in an anonymous type 
        ''' symbol cache. The type descriptors with the same keys are supposed to map to 'the same' anonymous
        ''' type template in terms of the same generic type being used for their implementation.
        ''' </summary>
        Public ReadOnly Key As String

        ''' <summary> Anonymous type is implicitly declared </summary>
        Public ReadOnly IsImplicitlyDeclared As Boolean

        ''' <summary> Anonymous delegate parameters, including one for return type </summary>
        Public ReadOnly Property Parameters As ImmutableArray(Of AnonymousTypeField)
            Get
                Return Fields
            End Get
        End Property

        Public Sub New(fields As ImmutableArray(Of AnonymousTypeField), _location As Location, _isImplicitlyDeclared As Boolean)
            Me.Fields = fields
            Me.Location = _location
            Me.IsImplicitlyDeclared = _isImplicitlyDeclared
            Me.Key = ComputeKey(fields, Function(f) f.Name, Function(f) f.IsKey)
        End Sub

        Friend Shared Function ComputeKey(Of T)(fields As ImmutableArray(Of T), getName As Func(Of T, String), getIsKey As Func(Of T, Boolean)) As String
            Dim pooledBuilder = PooledStringBuilder.GetInstance()
            Dim builder = pooledBuilder.Builder
            For Each field In fields
                builder.Append("|"c)
                builder.Append(getName(field))
                builder.Append(If(getIsKey(field), "+"c, "-"c))
            Next

            IdentifierComparison.ToLower(builder)
            Return pooledBuilder.ToStringAndFree()

        End Function

        ''' <summary>
        ''' This is ONLY used for debugging purpose
        ''' </summary>
        <Conditional("DEBUG")>
        Friend Sub AssertGood()
            ' Fields exist
            Debug.Assert(Not Fields.IsEmpty)

            ' All fields are good
            For Each field In Fields
                field.AssertGood()
            Next
        End Sub

        Public Overloads Function Equals(other As AnonymousTypeDescriptor) As Boolean Implements IEquatable(Of AnonymousTypeDescriptor).Equals
            ' Comparing keys ensures field count, field names and keyness are equal
            If Not Me.Key.Equals(other.Key) Then
                Return False
            End If

            ' Compare field types
            Dim myFields As ImmutableArray(Of AnonymousTypeField) = Me.Fields
            Dim count As Integer = myFields.Length
            Dim otherFields As ImmutableArray(Of AnonymousTypeField) = other.Fields
            For i = 0 To count - 1
                If Not myFields(i).Type.Equals(otherFields(i).Type) Then
                    Return False
                End If
            Next

            Return True
        End Function

        Public Overloads Overrides Function Equals(obj As Object) As Boolean
            Return TypeOf obj Is AnonymousTypeDescriptor AndAlso Equals(DirectCast(obj, AnonymousTypeDescriptor))
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return Me.Key.GetHashCode()
        End Function

        ''' <summary>
        ''' Performs internal substitution of types in anonymous type descriptor fields and returns True 
        ''' if any of the fields was changed, in which case a new descriptor is returned in newDescriptor
        ''' </summary>
        Public Function SubstituteTypeParametersIfNeeded(substitution As TypeSubstitution, <Out> ByRef newDescriptor As AnonymousTypeDescriptor) As Boolean
            Dim fieldCount = Me.Fields.Length
            Dim newFields(fieldCount - 1) As AnonymousTypeField
            Dim anyChange As Boolean = False

            For i = 0 To fieldCount - 1
                Dim current As AnonymousTypeField = Me.Fields(i)
                newFields(i) = New AnonymousTypeField(current.Name,
                                                      current.Type.InternalSubstituteTypeParameters(substitution).Type,
                                                      current.Location,
                                                      current.IsKey)
                If Not anyChange Then
                    anyChange = current.Type IsNot newFields(i).Type
                End If
            Next

            If anyChange Then
                newDescriptor = New AnonymousTypeDescriptor(newFields.AsImmutableOrNull(), Me.Location, Me.IsImplicitlyDeclared)
            Else
                newDescriptor = Nothing
            End If

            Return anyChange
        End Function
    End Structure

    ''' <summary> 
    ''' Describes anonymous type field in terms of its name, type and other attributes.
    ''' Or describes anonymous delegate parameter, including "return" parameter, in terms 
    ''' of its name, type and other attributes.
    ''' </summary>
    Friend Structure AnonymousTypeField

        ''' <summary> Anonymous type field/parameter name, not nothing and not empty </summary>
        Public ReadOnly Name As String

        ''' <summary>Location of the field</summary>
        Public ReadOnly Location As Location

        ''' <summary> Anonymous type field/parameter type, must be not nothing when 
        ''' the field is passed to anonymous type descriptor </summary>
        Public ReadOnly Property Type As TypeSymbol
            Get
                Return Me._type
            End Get
        End Property

        ''' <summary> 
        ''' Anonymous type field/parameter type, may be nothing when field descriptor is created,
        ''' must be assigned before passing the descriptor to anonymous type descriptor.
        ''' Once assigned, is considered to be 'sealed'. 
        ''' </summary>
        Private _type As TypeSymbol

        ''' <summary> Anonymous type field is declared as a 'Key' field </summary>
        Public ReadOnly IsKey As Boolean

        ''' <summary>
        ''' Does this describe a ByRef parameter of an Anonymous Delegate type
        ''' </summary>
        Public ReadOnly Property IsByRef As Boolean
            Get
                Return IsKey
            End Get
        End Property

        Public Sub New(name As String, type As TypeSymbol, location As Location, Optional isKeyOrByRef As Boolean = False)
            Me.Name = If(String.IsNullOrWhiteSpace(name), "<Empty Name>", name)
            Me._type = type
            Me.IsKey = isKeyOrByRef
            Me.Location = location
        End Sub

        Public Sub New(name As String, location As Location, isKey As Boolean)
            Me.New(name, Nothing, location, isKey)
        End Sub

        ''' <summary>
        ''' This is ONLY used for debugging purpose
        ''' </summary>
        <Conditional("DEBUG")>
        Friend Sub AssertGood()
            Debug.Assert(Name IsNot Nothing AndAlso Me.Type IsNot Nothing AndAlso Me.Location IsNot Nothing)
        End Sub

        Friend Sub AssignFieldType(newType As TypeSymbol)
            Debug.Assert(newType IsNot Nothing)
            Debug.Assert(Me._type Is Nothing)
            Me._type = newType
        End Sub

    End Structure

End Namespace
