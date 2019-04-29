' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Threading
Imports System.Reflection
Imports System.Reflection.Metadata
Imports Microsoft.CodeAnalysis.PooledObjects
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

        Private ReadOnly _name As String
        Private ReadOnly _flags As EventAttributes
        Private ReadOnly _containingType As PENamedTypeSymbol
        Private ReadOnly _handle As EventDefinitionHandle
        Private ReadOnly _eventType As TypeSymbol
        Private ReadOnly _addMethod As PEMethodSymbol
        Private ReadOnly _removeMethod As PEMethodSymbol

        Private ReadOnly _raiseMethod As PEMethodSymbol

        Private _lazyCustomAttributes As ImmutableArray(Of VisualBasicAttributeData)
        Private _lazyDocComment As Tuple(Of CultureInfo, String)
        Private _lazyUseSiteErrorInfo As DiagnosticInfo = ErrorFactory.EmptyErrorInfo ' Indicates unknown state. 
        Private _lazyObsoleteAttributeData As ObsoleteAttributeData = ObsoleteAttributeData.Uninitialized

        ' Distinct accessibility value to represent unset.
        Private Const s_unsetAccessibility As Integer = -1
        Private _lazyDeclaredAccessibility As Integer = s_unsetAccessibility

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

            Me._containingType = containingType

            Dim [module] = moduleSymbol.Module
            Dim eventType As EntityHandle

            Try
                [module].GetEventDefPropsOrThrow(handle, Me._name, Me._flags, eventType)
            Catch mrEx As BadImageFormatException
                If Me._name Is Nothing Then
                    Me._name = String.Empty
                End If

                _lazyUseSiteErrorInfo = ErrorFactory.ErrorInfo(ERRID.ERR_UnsupportedEvent1, Me)

                If eventType.IsNil Then
                    Me._eventType = New UnsupportedMetadataTypeSymbol(mrEx)
                End If
            End Try

            Me._addMethod = addMethod
            Me._removeMethod = removeMethod
            Me._raiseMethod = raiseMethod
            Me._handle = handle

            If _eventType Is Nothing Then
                Dim metadataDecoder = New MetadataDecoder(moduleSymbol, containingType)
                Me._eventType = metadataDecoder.GetTypeOfToken(eventType)
                _eventType = TupleTypeDecoder.DecodeTupleTypesIfApplicable(_eventType, handle, moduleSymbol)
            End If

            If Me._addMethod IsNot Nothing Then
                Me._addMethod.SetAssociatedEvent(Me, MethodKind.EventAdd)
            End If

            If Me._removeMethod IsNot Nothing Then
                Me._removeMethod.SetAssociatedEvent(Me, MethodKind.EventRemove)
            End If

            If Me._raiseMethod IsNot Nothing Then
                Me._raiseMethod.SetAssociatedEvent(Me, MethodKind.EventRaise)
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
                    TypeSymbol.Equals(_addMethod.ReturnType, evt, TypeCompareKind.ConsiderEverything) AndAlso
                    _addMethod.ParameterCount = 1 AndAlso
                    _removeMethod.ParameterCount = 1 AndAlso
                    TypeSymbol.Equals(_removeMethod.Parameters(0).Type, evt, TypeCompareKind.ConsiderEverything)
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _containingType
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingType As NamedTypeSymbol
            Get
                Return _containingType
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Friend ReadOnly Property EventFlags As EventAttributes
            Get
                Return _flags
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return (_flags And EventAttributes.SpecialName) <> 0
            End Get
        End Property

        Friend Overrides ReadOnly Property HasRuntimeSpecialName As Boolean
            Get
                Return (_flags And EventAttributes.RTSpecialName) <> 0
            End Get
        End Property

        Friend ReadOnly Property Handle As EventDefinitionHandle
            Get
                Return _handle
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                If Me._lazyDeclaredAccessibility = s_unsetAccessibility Then
                    Dim accessibility As accessibility = PEPropertyOrEventHelpers.GetDeclaredAccessibilityFromAccessors(Me.AddMethod, Me.RemoveMethod)
                    Interlocked.CompareExchange(Me._lazyDeclaredAccessibility, DirectCast(accessibility, Integer), s_unsetAccessibility)
                End If

                Return DirectCast(Me._lazyDeclaredAccessibility, Accessibility)
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
                Return Me._eventType
            End Get
        End Property

        Public Overrides ReadOnly Property AddMethod As MethodSymbol
            Get
                Return Me._addMethod
            End Get
        End Property

        Public Overrides ReadOnly Property RemoveMethod As MethodSymbol
            Get
                Return Me._removeMethod
            End Get
        End Property

        Public Overrides ReadOnly Property RaiseMethod As MethodSymbol
            Get
                Return Me._raiseMethod
            End Get
        End Property

        Friend Overrides ReadOnly Property AssociatedField As FieldSymbol
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return _containingType.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                ObsoleteAttributeHelpers.InitializeObsoleteDataFromMetadata(_lazyObsoleteAttributeData, _handle, DirectCast(ContainingModule, PEModuleSymbol))
                Return _lazyObsoleteAttributeData
            End Get
        End Property

        Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            If _lazyCustomAttributes.IsDefault Then
                Dim containingPEModuleSymbol = DirectCast(ContainingModule(), PEModuleSymbol)
                containingPEModuleSymbol.LoadCustomAttributes(_handle, _lazyCustomAttributes)
            End If
            Return _lazyCustomAttributes
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
            Return PEDocumentationCommentUtils.GetDocumentationComment(Me, _containingType.ContainingPEModule, preferredCulture, cancellationToken, _lazyDocComment)
        End Function

        Friend Overrides Function GetUseSiteErrorInfo() As DiagnosticInfo
            If _lazyUseSiteErrorInfo Is ErrorFactory.EmptyErrorInfo Then
                _lazyUseSiteErrorInfo = CalculateUseSiteErrorInfo()
            End If

            Return _lazyUseSiteErrorInfo
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

