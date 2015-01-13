' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Threading
Imports System.Reflection
Imports System.Reflection.Metadata
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE

    ''' <summary>
    ''' The class to represent all events imported from a PE/module.
    ''' </summary>
    Friend NotInheritable Class PEEventSymbol
        Inherits EventSymbol

        Private ReadOnly m_name As String
        Private ReadOnly m_flags As EventAttributes
        Private ReadOnly m_containingType As PENamedTypeSymbol
        Private ReadOnly m_handle As EventDefinitionHandle
        Private ReadOnly m_eventType As TypeSymbol
        Private ReadOnly m_addMethod As PEMethodSymbol
        Private ReadOnly m_removeMethod As PEMethodSymbol

        Private ReadOnly m_raiseMethod As PEMethodSymbol

        Private m_lazyCustomAttributes As ImmutableArray(Of VisualBasicAttributeData)
        Private m_lazyDocComment As Tuple(Of CultureInfo, String)
        Private m_lazyUseSiteErrorInfo As DiagnosticInfo = ErrorFactory.EmptyErrorInfo ' Indicates unknown state. 
        Private m_lazyObsoleteAttributeData As ObsoleteAttributeData = ObsoleteAttributeData.Uninitialized

        ' Distinct accessibility value to represent unset.
        Private Const UnsetAccessibility As Integer = -1
        Private m_lazyDeclaredAccessibility As Integer = UnsetAccessibility

        Friend Sub New(moduleSymbol As PEModuleSymbol,
                       containingType As PENamedTypeSymbol,
                       handle As EventDefinitionHandle,
                       addMethod As PEMethodSymbol,
                       removeMethod As PEMethodSymbol,
                       raiseMethod As PEMethodSymbol)

            Debug.Assert(moduleSymbol IsNot Nothing)
            Debug.Assert(containingType IsNot Nothing)
            Debug.Assert(Not handle.IsNil)
            Debug.Assert(addMethod IsNot Nothing)
            Debug.Assert(removeMethod IsNot Nothing)

            Me.m_containingType = containingType

            Dim [module] = moduleSymbol.Module
            Dim eventType As Handle

            Try
                [module].GetEventDefPropsOrThrow(handle, Me.m_name, Me.m_flags, eventType)
            Catch mrEx As BadImageFormatException
                If Me.m_name Is Nothing Then
                    Me.m_name = String.Empty
                End If

                m_lazyUseSiteErrorInfo = ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedEvent1, Me)

                If eventType.IsNil Then
                    Me.m_eventType = New UnsupportedMetadataTypeSymbol(mrEx)
                End If
            End Try

            Me.m_addMethod = addMethod
            Me.m_removeMethod = removeMethod
            Me.m_raiseMethod = raiseMethod
            Me.m_handle = handle

            If m_eventType Is Nothing Then
                Dim metadataDecoder = New MetadataDecoder(moduleSymbol, containingType)
                Me.m_eventType = MetadataDecoder.GetTypeOfToken(eventType)
            End If

            If Me.m_addMethod IsNot Nothing Then
                Me.m_addMethod.SetAssociatedEvent(Me, MethodKind.EventAdd)
            End If

            If Me.m_removeMethod IsNot Nothing Then
                Me.m_removeMethod.SetAssociatedEvent(Me, MethodKind.EventRemove)
            End If

            If Me.m_raiseMethod IsNot Nothing Then
                Me.m_raiseMethod.SetAssociatedEvent(Me, MethodKind.EventRaise)
            End If
        End Sub

        Public Overrides ReadOnly Property IsWindowsRuntimeEvent As Boolean
            Get
                Dim evt = DirectCast(Me.ContainingModule, PEModuleSymbol).GetEventRegistrationTokenType()
                ' WinRT events look different from normal events.
                ' The add method returns an EventRegistrationToken,
                ' and the remove method takes an EventRegistrationToken
                ' as a parameter.
                Return _
                    m_addMethod.ReturnType = evt AndAlso
                    m_addMethod.ParameterCount = 1 AndAlso
                    m_removeMethod.ParameterCount = 1 AndAlso
                    m_removeMethod.Parameters(0).Type = evt
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return m_containingType
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingType As NamedTypeSymbol
            Get
                Return m_containingType
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return m_name
            End Get
        End Property

        Friend ReadOnly Property EventFlags As EventAttributes
            Get
                Return m_flags
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return (m_flags And EventAttributes.SpecialName) <> 0
            End Get
        End Property

        Friend Overrides ReadOnly Property HasRuntimeSpecialName As Boolean
            Get
                Return (m_flags And EventAttributes.RTSpecialName) <> 0
            End Get
        End Property

        Friend ReadOnly Property Handle As EventDefinitionHandle
            Get
                Return m_handle
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                If Me.m_lazyDeclaredAccessibility = UnsetAccessibility Then
                    Dim accessibility As accessibility = PEPropertyOrEventHelpers.GetDeclaredAccessibilityFromAccessors(Me.AddMethod, Me.RemoveMethod)
                    Interlocked.CompareExchange(Me.m_lazyDeclaredAccessibility, DirectCast(accessibility, Integer), UnsetAccessibility)
                End If

                Return DirectCast(Me.m_lazyDeclaredAccessibility, Accessibility)
            End Get
        End Property

        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Dim method = Me.AddMethod
                Return (method IsNot Nothing) AndAlso method.IsMustOverride
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Dim method = Me.AddMethod
                Return (method IsNot Nothing) AndAlso method.IsNotOverridable
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Dim method = Me.AddMethod
                Return (method IsNot Nothing) AndAlso method.IsOverridable
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Dim method = Me.AddMethod
                Return (method IsNot Nothing) AndAlso method.IsOverrides
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Dim method = Me.AddMethod
                Return method Is Nothing OrElse method.IsShared
            End Get
        End Property

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return Me.m_eventType
            End Get
        End Property

        Public Overrides ReadOnly Property AddMethod As MethodSymbol
            Get
                Return Me.m_addMethod
            End Get
        End Property

        Public Overrides ReadOnly Property RemoveMethod As MethodSymbol
            Get
                Return Me.m_removeMethod
            End Get
        End Property

        Public Overrides ReadOnly Property RaiseMethod As MethodSymbol
            Get
                Return Me.m_raiseMethod
            End Get
        End Property

        Friend Overrides ReadOnly Property AssociatedField As FieldSymbol
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return m_containingType.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                ObsoleteAttributeHelpers.InitializeObsoleteDataFromMetadata(m_lazyObsoleteAttributeData, m_handle, DirectCast(ContainingModule, PEModuleSymbol))
                Return m_lazyObsoleteAttributeData
            End Get
        End Property

        Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            If m_lazyCustomAttributes.IsDefault Then
                Dim containingPEModuleSymbol = DirectCast(ContainingModule(), PEModuleSymbol)
                containingPEModuleSymbol.LoadCustomAttributes(m_handle, m_lazyCustomAttributes)
            End If
            Return m_lazyCustomAttributes
        End Function

        Friend Overrides Function GetCustomAttributesToEmit(compilationState As ModuleCompilationState) As IEnumerable(Of VisualBasicAttributeData)
            Return GetAttributes()
        End Function

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of EventSymbol)
            Get
                If Me.AddMethod.ExplicitInterfaceImplementations.Length = 0 AndAlso Me.RemoveMethod.ExplicitInterfaceImplementations.Length = 0 Then
                    Return ImmutableArray(Of EventSymbol).Empty
                End If

                Dim implementedEvents = PEPropertyOrEventHelpers.GetEventsForExplicitlyImplementedAccessor(Me.AddMethod)
                implementedEvents.IntersectWith(PEPropertyOrEventHelpers.GetEventsForExplicitlyImplementedAccessor(Me.RemoveMethod))
                Dim builder = ArrayBuilder(Of EventSymbol).GetInstance()
                For Each [event] In implementedEvents
                    builder.Add([event])
                Next

                Return builder.ToImmutableAndFree()
            End Get
        End Property

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            Return PEDocumentationCommentUtils.GetDocumentationComment(Me, m_containingType.ContainingPEModule, preferredCulture, cancellationToken, m_lazyDocComment)
        End Function

        Friend Overrides Function GetUseSiteErrorInfo() As DiagnosticInfo
            If m_lazyUseSiteErrorInfo Is ErrorFactory.EmptyErrorInfo Then
                m_lazyUseSiteErrorInfo = CalculateUseSiteErrorInfo()
            End If

            Return m_lazyUseSiteErrorInfo
        End Function

        ''' <remarks>
        ''' This is for perf, not for correctness.
        ''' </remarks>
        Friend Overrides ReadOnly Property DeclaringCompilation As VisualBasicCompilation
            Get
                Return Nothing
            End Get
        End Property

    End Class

End Namespace

