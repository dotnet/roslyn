' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        Private ReadOnly _retargetingModule As RetargetingModuleSymbol

        ''' <summary>
        ''' The underlying EventSymbol, cannot be another RetargetingEventSymbol.
        ''' </summary>
        Private ReadOnly _underlyingEvent As EventSymbol

        Private _lazyCustomModifiers As ImmutableArray(Of CustomModifier)

        ''' <summary>
        ''' Retargeted custom attributes
        ''' </summary>
        ''' <remarks></remarks>
        Private _lazyCustomAttributes As ImmutableArray(Of VisualBasicAttributeData)

        Private _lazyExplicitInterfaceImplementations As ImmutableArray(Of EventSymbol)

        Private _lazyUseSiteErrorInfo As DiagnosticInfo = ErrorFactory.EmptyErrorInfo ' Indicates unknown state. 

        Public Sub New(retargetingModule As RetargetingModuleSymbol, underlyingEvent As EventSymbol)
            Debug.Assert(retargetingModule IsNot Nothing)
            Debug.Assert(underlyingEvent IsNot Nothing)

            If TypeOf underlyingEvent Is RetargetingEventSymbol Then
                Throw New ArgumentException()
            End If

            _retargetingModule = retargetingModule
            _underlyingEvent = underlyingEvent
        End Sub

        Private ReadOnly Property RetargetingTranslator As RetargetingModuleSymbol.RetargetingSymbolTranslator
            Get
                Return _retargetingModule.RetargetingTranslator
            End Get
        End Property

        Public ReadOnly Property UnderlyingEvent As EventSymbol
            Get
                Return _underlyingEvent
            End Get
        End Property

        Public ReadOnly Property RetargetingModule As RetargetingModuleSymbol
            Get
                Return _retargetingModule
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return RetargetingTranslator.Retarget(_underlyingEvent.ContainingSymbol)
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return _underlyingEvent.DeclaredAccessibility
            End Get
        End Property

        Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return RetargetingTranslator.GetRetargetedAttributes(_underlyingEvent, _lazyCustomAttributes)
        End Function

        Friend Overrides Function GetCustomAttributesToEmit(compilationState As ModuleCompilationState) As IEnumerable(Of VisualBasicAttributeData)
            Return RetargetingTranslator.RetargetAttributes(_underlyingEvent.GetCustomAttributesToEmit(compilationState))
        End Function

        Public Overrides ReadOnly Property AddMethod As MethodSymbol
            Get
                Return If(_underlyingEvent.AddMethod Is Nothing, Nothing, RetargetingTranslator.Retarget(_underlyingEvent.AddMethod))
            End Get
        End Property

        Friend Overrides ReadOnly Property AssociatedField As FieldSymbol
            Get
                Return If(_underlyingEvent.AssociatedField Is Nothing, Nothing, RetargetingTranslator.Retarget(_underlyingEvent.AssociatedField))
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of EventSymbol)
            Get
                If _lazyExplicitInterfaceImplementations.IsDefault Then
                    ImmutableInterlocked.InterlockedCompareExchange(
                        _lazyExplicitInterfaceImplementations,
                        Me.RetargetExplicitInterfaceImplementations(),
                        Nothing)
                End If

                Return _lazyExplicitInterfaceImplementations
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
                Return _underlyingEvent.IsMustOverride
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return _underlyingEvent.IsNotOverridable
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return _underlyingEvent.IsOverridable
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return _underlyingEvent.IsOverrides
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return _underlyingEvent.IsShared
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return _underlyingEvent.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return _underlyingEvent.DeclaringSyntaxReferences
            End Get
        End Property

        Public Overrides ReadOnly Property RaiseMethod As MethodSymbol
            Get
                Return If(_underlyingEvent.RaiseMethod Is Nothing, Nothing, RetargetingTranslator.Retarget(_underlyingEvent.RaiseMethod))
            End Get
        End Property

        Public Overrides ReadOnly Property RemoveMethod As MethodSymbol
            Get
                Return If(_underlyingEvent.RemoveMethod Is Nothing, Nothing, RetargetingTranslator.Retarget(_underlyingEvent.RemoveMethod))
            End Get
        End Property

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return RetargetingTranslator.Retarget(_underlyingEvent.Type, RetargetOptions.RetargetPrimitiveTypesByTypeCode)
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return _underlyingEvent.Name
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataName As String
            Get
                Return _underlyingEvent.MetadataName
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return _underlyingEvent.HasSpecialName
            End Get
        End Property

        Friend Overrides ReadOnly Property HasRuntimeSpecialName As Boolean
            Get
                Return _underlyingEvent.HasRuntimeSpecialName
            End Get
        End Property

        Friend Overrides Function GetUseSiteErrorInfo() As DiagnosticInfo
            If _lazyUseSiteErrorInfo Is ErrorFactory.EmptyErrorInfo Then
                _lazyUseSiteErrorInfo = CalculateUseSiteErrorInfo()
            End If

            Return _lazyUseSiteErrorInfo
        End Function

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return _underlyingEvent.ObsoleteAttributeData
            End Get
        End Property

        Public Overrides ReadOnly Property IsWindowsRuntimeEvent As Boolean
            Get
                Return _underlyingEvent.IsWindowsRuntimeEvent
            End Get
        End Property

        ''' <remarks>
        ''' This is for perf, not for correctness.
        ''' </remarks>
        Friend Overrides ReadOnly Property DeclaringCompilation As VisualBasicCompilation
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            Return _underlyingEvent.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken)
        End Function
    End Class
End Namespace

