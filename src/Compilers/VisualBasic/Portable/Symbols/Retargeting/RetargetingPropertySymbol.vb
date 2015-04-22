' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols.Retargeting
    Friend NotInheritable Class RetargetingPropertySymbol
        Inherits PropertySymbol

        ''' <summary>
        ''' Owning RetargetingModuleSymbol.
        ''' </summary>
        Private ReadOnly _retargetingModule As RetargetingModuleSymbol

        ''' <summary>
        ''' The underlying PropertySymbol, cannot be another RetargetingPropertySymbol.
        ''' </summary>
        Private ReadOnly _underlyingProperty As PropertySymbol

        Private _lazyParameters As ImmutableArray(Of ParameterSymbol)

        Private _lazyCustomModifiers As ImmutableArray(Of CustomModifier)

        ''' <summary>
        ''' Retargeted custom attributes
        ''' </summary>
        ''' <remarks></remarks>
        Private _lazyCustomAttributes As ImmutableArray(Of VisualBasicAttributeData)

        Private _lazyExplicitInterfaceImplementations As ImmutableArray(Of PropertySymbol)

        Private _lazyUseSiteErrorInfo As DiagnosticInfo = ErrorFactory.EmptyErrorInfo ' Indicates unknown state. 

        Public Sub New(retargetingModule As RetargetingModuleSymbol, underlyingProperty As PropertySymbol)
            Debug.Assert(retargetingModule IsNot Nothing)
            Debug.Assert(underlyingProperty IsNot Nothing)

            If TypeOf underlyingProperty Is RetargetingPropertySymbol Then
                Throw New ArgumentException()
            End If

            _retargetingModule = retargetingModule
            _underlyingProperty = underlyingProperty
        End Sub

        Private ReadOnly Property RetargetingTranslator As RetargetingModuleSymbol.RetargetingSymbolTranslator
            Get
                Return _retargetingModule.RetargetingTranslator
            End Get
        End Property

        Public ReadOnly Property UnderlyingProperty As PropertySymbol
            Get
                Return _underlyingProperty
            End Get
        End Property

        Public ReadOnly Property RetargetingModule As RetargetingModuleSymbol
            Get
                Return _retargetingModule
            End Get
        End Property

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return _underlyingProperty.IsImplicitlyDeclared
            End Get
        End Property

        Public Overrides ReadOnly Property IsWithEvents As Boolean
            Get
                Return _underlyingProperty.IsWithEvents
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return RetargetingTranslator.Retarget(_underlyingProperty.ContainingSymbol)
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return _underlyingProperty.DeclaredAccessibility
            End Get
        End Property

        Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return RetargetingTranslator.GetRetargetedAttributes(_underlyingProperty, _lazyCustomAttributes)
        End Function

        Friend Overrides Function GetCustomAttributesToEmit(compilationState As ModuleCompilationState) As IEnumerable(Of VisualBasicAttributeData)
            Return RetargetingTranslator.RetargetAttributes(_underlyingProperty.GetCustomAttributesToEmit(compilationState))
        End Function

        Public Overrides ReadOnly Property GetMethod As MethodSymbol
            Get
                Return If(_underlyingProperty.GetMethod Is Nothing, Nothing, RetargetingTranslator.Retarget(_underlyingProperty.GetMethod))
            End Get
        End Property

        Public Overrides ReadOnly Property SetMethod As MethodSymbol
            Get
                Return If(_underlyingProperty.SetMethod Is Nothing, Nothing, RetargetingTranslator.Retarget(_underlyingProperty.SetMethod))
            End Get
        End Property

        Friend Overrides ReadOnly Property AssociatedField As FieldSymbol
            Get
                Return If(_underlyingProperty.AssociatedField Is Nothing, Nothing, RetargetingTranslator.Retarget(_underlyingProperty.AssociatedField))
            End Get
        End Property

        Public Overrides ReadOnly Property IsDefault As Boolean
            Get
                Return _underlyingProperty.IsDefault
            End Get
        End Property

        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return _underlyingProperty.IsMustOverride
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return _underlyingProperty.IsNotOverridable
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return _underlyingProperty.IsOverridable
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return _underlyingProperty.IsOverrides
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverloads As Boolean
            Get
                Return _underlyingProperty.IsOverloads
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return _underlyingProperty.IsShared
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return _underlyingProperty.Name
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataName As String
            Get
                Return _underlyingProperty.MetadataName
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return _underlyingProperty.HasSpecialName
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return _underlyingProperty.ObsoleteAttributeData
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return _underlyingProperty.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return _underlyingProperty.DeclaringSyntaxReferences
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                If _lazyParameters.IsDefault Then
                    ImmutableInterlocked.InterlockedCompareExchange(_lazyParameters, RetargetParameters(), Nothing)
                End If

                Return _lazyParameters
            End Get
        End Property

        Public Overrides ReadOnly Property ParameterCount As Integer
            Get
                Return _underlyingProperty.ParameterCount
            End Get
        End Property

        Private Function RetargetParameters() As ImmutableArray(Of ParameterSymbol)
            Dim list = _underlyingProperty.Parameters
            Dim count = list.Length

            If count = 0 Then
                Return ImmutableArray(Of ParameterSymbol).Empty
            Else
                Dim parameters = New ParameterSymbol(count - 1) {}

                For i As Integer = 0 To count - 1
                    parameters(i) = RetargetingParameterSymbol.CreatePropertyParameter(Me, list(i))
                Next

                Return parameters.AsImmutableOrNull()
            End If
        End Function

        Public Overrides ReadOnly Property ReturnsByRef As Boolean
            Get
                Return _underlyingProperty.ReturnsByRef
            End Get
        End Property

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return RetargetingTranslator.Retarget(_underlyingProperty.Type, RetargetOptions.RetargetPrimitiveTypesByTypeCode)
            End Get
        End Property

        Public Overrides ReadOnly Property TypeCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return RetargetingTranslator.RetargetModifiers(_underlyingProperty.TypeCustomModifiers, _lazyCustomModifiers)
            End Get
        End Property

        Friend Overrides ReadOnly Property CallingConvention As Microsoft.Cci.CallingConvention
            Get
                Return _underlyingProperty.CallingConvention
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of PropertySymbol)
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

        Private Function RetargetExplicitInterfaceImplementations() As ImmutableArray(Of PropertySymbol)
            Dim impls = Me.UnderlyingProperty.ExplicitInterfaceImplementations
            If impls.IsEmpty Then
                Return impls
            End If

            Dim builder = ArrayBuilder(Of PropertySymbol).GetInstance()
            For i = 0 To impls.Length - 1
                Dim retargeted = Me.RetargetingModule.RetargetingTranslator.Retarget(impls(i), PropertySignatureComparer.RetargetedExplicitPropertyImplementationComparer)
                If retargeted IsNot Nothing Then
                    builder.Add(retargeted)
                End If
            Next

            Return builder.ToImmutableAndFree()
        End Function

        Friend Overrides Function GetUseSiteErrorInfo() As DiagnosticInfo
            If _lazyUseSiteErrorInfo Is ErrorFactory.EmptyErrorInfo Then
                _lazyUseSiteErrorInfo = CalculateUseSiteErrorInfo()
            End If

            Return _lazyUseSiteErrorInfo
        End Function

        Friend Overrides ReadOnly Property IsMyGroupCollectionProperty As Boolean
            Get
                Return _underlyingProperty.IsMyGroupCollectionProperty
            End Get
        End Property

        Friend Overrides ReadOnly Property HasRuntimeSpecialName As Boolean
            Get
                Return _underlyingProperty.HasRuntimeSpecialName
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
            Return _underlyingProperty.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken)
        End Function
    End Class
End Namespace
