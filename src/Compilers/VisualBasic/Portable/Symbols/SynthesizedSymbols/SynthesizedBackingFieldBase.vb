' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Represents a base type for compiler generated backing fields.
    ''' </summary>
    Friend MustInherit Class SynthesizedBackingFieldBase(Of T As Symbol)
        Inherits FieldSymbol

        Protected ReadOnly _propertyOrEvent As T
        Protected ReadOnly _name As String
        Protected ReadOnly _isShared As Boolean

        Public Sub New(propertyOrEvent As T, name As String, isShared As Boolean)
            Debug.Assert(propertyOrEvent IsNot Nothing)
            Debug.Assert(Not String.IsNullOrEmpty(name))

            _propertyOrEvent = propertyOrEvent
            _name = name
            _isShared = isShared
        End Sub

        Public Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return ImmutableArray(Of CustomModifier).Empty
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property AssociatedSymbol As Symbol
            Get
                Return _propertyOrEvent
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property ShadowsExplicitly As Boolean
            Get
                Return _propertyOrEvent.ShadowsExplicitly
            End Get
        End Property

        Public Overrides ReadOnly Property IsReadOnly As Boolean
            Get
                Return False
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

        Public NotOverridable Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _propertyOrEvent.ContainingType
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property ContainingType As NamedTypeSymbol
            Get
                Return _propertyOrEvent.ContainingType
            End Get
        End Property

        Friend NotOverridable Overrides Function GetLexicalSortKey() As LexicalSortKey
            Return _propertyOrEvent.GetLexicalSortKey()
        End Function

        Public NotOverridable Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return _propertyOrEvent.Locations
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Accessibility.Private
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsShared As Boolean
            Get
                Return _isShared
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return True
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property ImplicitlyDefinedBy(Optional membersInProgress As Dictionary(Of String, ArrayBuilder(Of Symbol)) = Nothing) As Symbol
            Get
                Return _propertyOrEvent
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Friend Overrides Sub AddSynthesizedAttributes(moduleBuilder As PEModuleBuilder, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
            MyBase.AddSynthesizedAttributes(moduleBuilder, attributes)

            Dim compilation = Me.DeclaringCompilation

            ' Dev11 only synthesizes these attributes for backing field of auto-property, not for Events or WithEvents.

            If Not Me.ContainingType.IsImplicitlyDeclared Then
                AddSynthesizedAttribute(attributes, compilation.TrySynthesizeAttribute(
                    WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor))
            End If

            AddSynthesizedAttribute(attributes, compilation.SynthesizeDebuggerBrowsableNeverAttribute())

            If Type.ContainsTupleNames() AndAlso
                compilation.HasTupleNamesAttributes Then

                AddSynthesizedAttribute(attributes, compilation.SynthesizeTupleNamesAttribute(Type))
            End If
        End Sub

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property HasRuntimeSpecialName As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property IsNotSerialized As Boolean
            Get
                Return False
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Nothing
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

        Public NotOverridable Overrides ReadOnly Property IsRequired As Boolean
            Get
                Return False
            End Get
        End Property
    End Class
End Namespace
