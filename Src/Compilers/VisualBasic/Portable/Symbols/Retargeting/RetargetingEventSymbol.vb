' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols.Retargeting
    Friend NotInheritable Class RetargetingEventSymbol
        Inherits EventSymbol

        ''' <summary>
        ''' Owning RetargetingModuleSymbol.
        ''' </summary>
        Private ReadOnly m_RetargetingModule As RetargetingModuleSymbol

        ''' <summary>
        ''' The underlying EventSymbol, cannot be another RetargetingEventSymbol.
        ''' </summary>
        Private ReadOnly m_UnderlyingEvent As EventSymbol

        Private m_LazyCustomModifiers As ImmutableArray(Of CustomModifier)

        ''' <summary>
        ''' Retargeted custom attributes
        ''' </summary>
        ''' <remarks></remarks>
        Private m_LazyCustomAttributes As ImmutableArray(Of VisualBasicAttributeData)

        Private m_LazyExplicitInterfaceImplementations As ImmutableArray(Of EventSymbol)

        Private m_lazyUseSiteErrorInfo As DiagnosticInfo = ErrorFactory.EmptyErrorInfo ' Indicates unknown state. 

        Public Sub New(retargetingModule As RetargetingModuleSymbol, underlyingEvent As EventSymbol)
            Debug.Assert(retargetingModule IsNot Nothing)
            Debug.Assert(underlyingEvent IsNot Nothing)

            If TypeOf underlyingEvent Is RetargetingEventSymbol Then
                Throw New ArgumentException()
            End If

            m_RetargetingModule = retargetingModule
            m_UnderlyingEvent = underlyingEvent
        End Sub

        Private ReadOnly Property RetargetingTranslator As RetargetingModuleSymbol.RetargetingSymbolTranslator
            Get
                Return m_RetargetingModule.RetargetingTranslator
            End Get
        End Property

        Public ReadOnly Property UnderlyingEvent As EventSymbol
            Get
                Return m_UnderlyingEvent
            End Get
        End Property

        Public ReadOnly Property RetargetingModule As RetargetingModuleSymbol
            Get
                Return m_RetargetingModule
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return RetargetingTranslator.Retarget(m_UnderlyingEvent.ContainingSymbol)
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return m_UnderlyingEvent.DeclaredAccessibility
            End Get
        End Property

        Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return RetargetingTranslator.GetRetargetedAttributes(m_UnderlyingEvent, m_LazyCustomAttributes)
        End Function

        Friend Overrides Function GetCustomAttributesToEmit() As IEnumerable(Of VisualBasicAttributeData)
            Return RetargetingTranslator.RetargetAttributes(m_UnderlyingEvent.GetCustomAttributesToEmit())
        End Function

        Public Overrides ReadOnly Property AddMethod As MethodSymbol
            Get
                Return If(m_UnderlyingEvent.AddMethod Is Nothing, Nothing, RetargetingTranslator.Retarget(m_UnderlyingEvent.AddMethod))
            End Get
        End Property

        Friend Overrides ReadOnly Property AssociatedField As FieldSymbol
            Get
                Return If(m_UnderlyingEvent.AssociatedField Is Nothing, Nothing, RetargetingTranslator.Retarget(m_UnderlyingEvent.AssociatedField))
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of EventSymbol)
            Get
                If m_LazyExplicitInterfaceImplementations.IsDefault Then
                    ImmutableInterlocked.InterlockedCompareExchange(
                        m_LazyExplicitInterfaceImplementations,
                        Me.RetargetExplicitInterfaceImplementations(),
                        Nothing)
                End If

                Return m_LazyExplicitInterfaceImplementations
            End Get
        End Property

        Private Function RetargetExplicitInterfaceImplementations() As ImmutableArray(Of EventSymbol)
            Dim impls = Me.UnderlyingEvent.ExplicitInterfaceImplementations
            If impls.IsEmpty Then
                Return impls
            End If

            Dim builder = ArrayBuilder(Of EventSymbol).GetInstance()

            For i = 0 To impls.Length - 1
                Dim retargeted = Me.RetargetingModule.RetargetingTranslator.RetargetImplementedEvent(impls(i))
                If retargeted IsNot Nothing Then
                    builder.Add(retargeted)
                End If
            Next

            Return builder.ToImmutableAndFree()
        End Function

        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return m_UnderlyingEvent.IsMustOverride
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return m_UnderlyingEvent.IsNotOverridable
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return m_UnderlyingEvent.IsOverridable
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return m_UnderlyingEvent.IsOverrides
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return m_UnderlyingEvent.IsShared
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return m_UnderlyingEvent.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return m_UnderlyingEvent.DeclaringSyntaxReferences
            End Get
        End Property

        Public Overrides ReadOnly Property RaiseMethod As MethodSymbol
            Get
                Return If(m_UnderlyingEvent.RaiseMethod Is Nothing, Nothing, RetargetingTranslator.Retarget(m_UnderlyingEvent.RaiseMethod))
            End Get
        End Property

        Public Overrides ReadOnly Property RemoveMethod As MethodSymbol
            Get
                Return If(m_UnderlyingEvent.RemoveMethod Is Nothing, Nothing, RetargetingTranslator.Retarget(m_UnderlyingEvent.RemoveMethod))
            End Get
        End Property

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return RetargetingTranslator.Retarget(m_UnderlyingEvent.Type, RetargetOptions.RetargetPrimitiveTypesByTypeCode)
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return m_UnderlyingEvent.Name
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataName As String
            Get
                Return m_UnderlyingEvent.MetadataName
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return m_UnderlyingEvent.HasSpecialName
            End Get
        End Property

        Friend Overrides ReadOnly Property HasRuntimeSpecialName As Boolean
            Get
                Return m_UnderlyingEvent.HasRuntimeSpecialName
            End Get
        End Property

        Friend Overrides Function GetUseSiteErrorInfo() As DiagnosticInfo
            If m_lazyUseSiteErrorInfo Is ErrorFactory.EmptyErrorInfo Then
                m_lazyUseSiteErrorInfo = CalculateUseSiteErrorInfo()
            End If

            Return m_lazyUseSiteErrorInfo
        End Function

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return m_UnderlyingEvent.ObsoleteAttributeData
            End Get
        End Property

        Public Overrides ReadOnly Property IsWindowsRuntimeEvent As Boolean
            Get
                Return m_UnderlyingEvent.IsWindowsRuntimeEvent
            End Get
        End Property

        ''' <remarks>
        ''' This is for perf, not for correctness.
        ''' </remarks>
        Friend Overrides ReadOnly Property DeclaringCompilation As VBCompilation
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            Return m_UnderlyingEvent.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken)
        End Function
    End Class
End Namespace

