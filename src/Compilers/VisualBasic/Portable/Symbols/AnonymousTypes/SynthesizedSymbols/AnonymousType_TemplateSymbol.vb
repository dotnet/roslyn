' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Emit

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Partial Friend NotInheritable Class AnonymousTypeManager

        Private NotInheritable Class AnonymousTypeTemplateSymbol
            Inherits AnonymousTypeOrDelegateTemplateSymbol

            Private ReadOnly _properties As ImmutableArray(Of AnonymousTypePropertySymbol)
            Private ReadOnly _members As ImmutableArray(Of Symbol)
            Private ReadOnly _interfaces As ImmutableArray(Of NamedTypeSymbol)
            Friend ReadOnly HasAtLeastOneKeyField As Boolean

            Public Sub New(manager As AnonymousTypeManager,
                           typeDescr As AnonymousTypeDescriptor)
                MyBase.New(manager, typeDescr)

                Dim fieldsCount As Integer = typeDescr.Fields.Length

                Dim methodMembersBuilder = ArrayBuilder(Of Symbol).GetInstance()
                Dim otherMembersBuilder = ArrayBuilder(Of Symbol).GetInstance()

                ' The array storing property symbols to be used in 
                ' generation of constructor and other methods
                Dim propertiesArray = New AnonymousTypePropertySymbol(fieldsCount - 1) {}

                ' Anonymous types with at least one Key field are being generated slightly different
                HasAtLeastOneKeyField = False

                '  Process fields
                For fieldIndex = 0 To fieldsCount - 1

                    Dim field As AnonymousTypeField = typeDescr.Fields(fieldIndex)
                    If field.IsKey Then
                        HasAtLeastOneKeyField = True
                    End If

                    ' Add a property
                    Dim [property] As New AnonymousTypePropertySymbol(Me, field, fieldIndex, Me.TypeParameters(fieldIndex))
                    propertiesArray(fieldIndex) = [property]

                    ' Property related symbols
                    otherMembersBuilder.Add([property])
                    methodMembersBuilder.Add([property].GetMethod)
                    If [property].SetMethod IsNot Nothing Then
                        methodMembersBuilder.Add([property].SetMethod)
                    End If

                    otherMembersBuilder.Add([property].AssociatedField)
                Next

                _properties = propertiesArray.AsImmutableOrNull()

                ' Add a constructor
                methodMembersBuilder.Add(New AnonymousTypeConstructorSymbol(Me))
                ' Add 'ToString'
                methodMembersBuilder.Add(New AnonymousTypeToStringMethodSymbol(Me))

                ' Add optional members
                If HasAtLeastOneKeyField AndAlso Me.Manager.System_IEquatable_T_Equals IsNot Nothing Then

                    ' Add 'GetHashCode'
                    methodMembersBuilder.Add(New AnonymousTypeGetHashCodeMethodSymbol(Me))

                    ' Add optional 'Inherits IEquatable'
                    Dim equatableInterface As NamedTypeSymbol = Me.Manager.System_IEquatable_T.Construct(ImmutableArray.Create(Of TypeSymbol)(Me))
                    _interfaces = ImmutableArray.Create(Of NamedTypeSymbol)(equatableInterface)

                    ' Add 'IEquatable.Equals'
                    Dim method As Symbol = DirectCast(equatableInterface, SubstitutedNamedType).GetMemberForDefinition(Me.Manager.System_IEquatable_T_Equals)
                    Dim iEquatableEquals As MethodSymbol = New AnonymousType_IEquatable_EqualsMethodSymbol(Me, DirectCast(method, MethodSymbol))
                    methodMembersBuilder.Add(iEquatableEquals)

                    ' Add 'Equals'
                    methodMembersBuilder.Add(New AnonymousTypeEqualsMethodSymbol(Me, iEquatableEquals))

                Else
                    _interfaces = ImmutableArray(Of NamedTypeSymbol).Empty
                End If

                methodMembersBuilder.AddRange(otherMembersBuilder)
                otherMembersBuilder.Free()
                _members = methodMembersBuilder.ToImmutableAndFree()
            End Sub

            Friend Overrides Function GetAnonymousTypeKey() As AnonymousTypeKey
                Dim properties = _properties.SelectAsArray(Function(p) New AnonymousTypeKeyField(p.Name, isKey:=p.IsReadOnly, ignoreCase:=True))
                Return New AnonymousTypeKey(properties)
            End Function

            Friend Overrides ReadOnly Property GeneratedNamePrefix As String
                Get
                    Return GeneratedNameConstants.AnonymousTypeTemplateNamePrefix
                End Get
            End Property

            Public ReadOnly Property Properties As ImmutableArray(Of AnonymousTypePropertySymbol)
                Get
                    Return Me._properties
                End Get
            End Property

            Public Overrides Function GetMembers() As ImmutableArray(Of Symbol)
                Return _members
            End Function

            Friend Overrides Iterator Function GetFieldsToEmit() As IEnumerable(Of FieldSymbol)
                For Each m In GetMembers()
                    If m.Kind = SymbolKind.Field Then
                        Yield DirectCast(m, FieldSymbol)
                    End If
                Next
            End Function

            Friend Overrides Function MakeAcyclicBaseType(diagnostics As BindingDiagnosticBag) As NamedTypeSymbol
                Return Me.Manager.System_Object
            End Function

            Friend Overrides Function MakeAcyclicInterfaces(diagnostics As BindingDiagnosticBag) As ImmutableArray(Of NamedTypeSymbol)
                Return _interfaces
            End Function

            Public Overrides ReadOnly Property TypeKind As TypeKind
                Get
                    Return TypeKind.Class
                End Get
            End Property

            Friend Overrides ReadOnly Property IsInterface As Boolean
                Get
                    Return False
                End Get
            End Property

            Friend Overrides Sub AddSynthesizedAttributes(moduleBuilder As PEModuleBuilder, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
                MyBase.AddSynthesizedAttributes(moduleBuilder, attributes)

                ' Attribute: System.Runtime.CompilerServices.CompilerGeneratedAttribute()
                AddSynthesizedAttribute(attributes, Manager.Compilation.TrySynthesizeAttribute(
                    WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor))

                ' VB emits this attribute regardless of /debug settings (unlike C#, which only emits it for /debug:full)
                ' Attribute: System.Diagnostics.DebuggerDisplayAttribute("a={a}, b={b}, c={c}, ...")
                AddSynthesizedAttribute(attributes, SynthesizeDebuggerDisplayAttribute())
            End Sub

            Private Function SynthesizeDebuggerDisplayAttribute() As SynthesizedAttributeData
                ' VB doesn't allow empty anon types
                Debug.Assert(Me.Properties.Length > 0)

                Dim builder = PooledStringBuilder.GetInstance()
                Dim sb = builder.Builder
                Dim displayCount As Integer = Math.Min(Me.Properties.Length, 4)

                For fieldIndex = 0 To displayCount - 1
                    Dim fieldName As String = Me.Properties(fieldIndex).Name
                    If fieldIndex > 0 Then
                        sb.Append(", ")
                    End If

                    sb.Append(fieldName)
                    sb.Append("={")
                    sb.Append(fieldName)
                    sb.Append("}")
                Next

                If Me.Properties.Length > displayCount Then
                    sb.Append(", ...")
                End If

                Return Manager.Compilation.TrySynthesizeAttribute(
                    WellKnownMember.System_Diagnostics_DebuggerDisplayAttribute__ctor,
                    ImmutableArray.Create(New TypedConstant(Manager.System_String, TypedConstantKind.Primitive, builder.ToStringAndFree())))
            End Function

        End Class
    End Class
End Namespace

