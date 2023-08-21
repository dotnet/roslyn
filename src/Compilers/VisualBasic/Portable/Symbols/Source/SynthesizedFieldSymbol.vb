' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Represents a compiler generated field of given type and name.
    ''' </summary>
    Friend Class SynthesizedFieldSymbol
        Inherits FieldSymbol

        Protected ReadOnly _containingType As NamedTypeSymbol
        Protected ReadOnly _implicitlyDefinedBy As Symbol
        Protected ReadOnly _type As TypeSymbol
        Protected ReadOnly _name As String
        Protected ReadOnly _flags As SourceMemberFlags
        Protected ReadOnly _isSpecialNameAndRuntimeSpecial As Boolean

        Public Sub New(containingType As NamedTypeSymbol,
                      implicitlyDefinedBy As Symbol,
                      type As TypeSymbol,
                      name As String,
                      Optional accessibility As Accessibility = Accessibility.Private,
                      Optional isReadOnly As Boolean = False,
                      Optional isShared As Boolean = False,
                      Optional isSpecialNameAndRuntimeSpecial As Boolean = False)
            Debug.Assert(containingType IsNot Nothing)
            Debug.Assert(implicitlyDefinedBy IsNot Nothing)
            Debug.Assert(type IsNot Nothing)

            Me._containingType = containingType
            Me._implicitlyDefinedBy = implicitlyDefinedBy
            Me._type = type
            Me._name = name
            Me._flags = CType(accessibility, SourceMemberFlags) Or
                        If(isReadOnly, SourceMemberFlags.ReadOnly, SourceMemberFlags.None) Or
                        If(isShared, SourceMemberFlags.Shared, SourceMemberFlags.None)

            Me._isSpecialNameAndRuntimeSpecial = isSpecialNameAndRuntimeSpecial
        End Sub

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return Me._type
            End Get
        End Property

        Friend Overrides Sub AddSynthesizedAttributes(moduleBuilder As PEModuleBuilder, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
            MyBase.AddSynthesizedAttributes(moduleBuilder, attributes)

            ' enum fields do not need to be marked as generated
            If (_isSpecialNameAndRuntimeSpecial) Then
                Return
            End If

            Dim compilation = Me.DeclaringCompilation
            If Type.ContainsTupleNames() AndAlso
                compilation.HasTupleNamesAttributes Then

                AddSynthesizedAttribute(attributes, compilation.SynthesizeTupleNamesAttribute(Type))
            End If

            Dim sourceType = TryCast(ContainingSymbol, SourceMemberContainerTypeSymbol)

            ' if parent is not from source, it must be a frame.
            ' frame is already marked as generated, no need to mark members.
            If sourceType Is Nothing Then
                Return
            End If

            ' Attribute: System.Runtime.CompilerServices.CompilerGeneratedAttribute()
            AddSynthesizedAttribute(attributes, compilation.TrySynthesizeAttribute(
                    WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor))
        End Sub

        Public Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return ImmutableArray(Of CustomModifier).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property AssociatedSymbol As Symbol
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property IsReadOnly As Boolean
            Get
                Return (_flags And SourceMemberFlags.ReadOnly) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property IsConst As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides Function GetConstantValue(inProgress As ConstantFieldsInProgress) As ConstantValue
            Return Nothing
        End Function

        Public NotOverridable Overrides ReadOnly Property IsRequired As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return Me._containingType
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingType As NamedTypeSymbol
            Get
                Return Me._containingType
            End Get
        End Property

        Friend Overrides Function GetLexicalSortKey() As LexicalSortKey
            Return _implicitlyDefinedBy.GetLexicalSortKey()
        End Function

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return _implicitlyDefinedBy.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return CType((_flags And SourceMemberFlags.AccessibilityMask), Accessibility)
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return (_flags And SourceMemberFlags.Shared) <> 0
            End Get
        End Property

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return True
            End Get
        End Property

        Friend Overrides ReadOnly Property ImplicitlyDefinedBy(Optional membersInProgress As Dictionary(Of String, ArrayBuilder(Of Symbol)) = Nothing) As Symbol
            Get
                Return Me._implicitlyDefinedBy
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return Me._name
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return Me._isSpecialNameAndRuntimeSpecial
            End Get
        End Property

        Friend Overrides ReadOnly Property HasRuntimeSpecialName As Boolean
            Get
                Return Me._isSpecialNameAndRuntimeSpecial
            End Get
        End Property

        Friend Overrides ReadOnly Property IsNotSerialized As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingInformation As MarshalPseudoCustomAttributeData
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides ReadOnly Property TypeLayoutOffset As Integer?
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Nothing
            End Get
        End Property
    End Class
End Namespace

