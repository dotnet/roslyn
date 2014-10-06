' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        Private ReadOnly m_RetargetingModule As RetargetingModuleSymbol

        ''' <summary>
        ''' The underlying PropertySymbol, cannot be another RetargetingPropertySymbol.
        ''' </summary>
        Private ReadOnly m_UnderlyingProperty As PropertySymbol

        Private m_LazyParameters As ImmutableArray(Of ParameterSymbol)

        Private m_LazyCustomModifiers As ImmutableArray(Of CustomModifier)

        ''' <summary>
        ''' Retargeted custom attributes
        ''' </summary>
        ''' <remarks></remarks>
        Private m_LazyCustomAttributes As ImmutableArray(Of VisualBasicAttributeData)

        Private m_LazyExplicitInterfaceImplementations As ImmutableArray(Of PropertySymbol)

        Private m_lazyUseSiteErrorInfo As DiagnosticInfo = ErrorFactory.EmptyErrorInfo ' Indicates unknown state. 

        Public Sub New(retargetingModule As RetargetingModuleSymbol, underlyingProperty As PropertySymbol)
            Debug.Assert(retargetingModule IsNot Nothing)
            Debug.Assert(underlyingProperty IsNot Nothing)

            If TypeOf underlyingProperty Is RetargetingPropertySymbol Then
                Throw New ArgumentException()
            End If

            m_RetargetingModule = retargetingModule
            m_UnderlyingProperty = underlyingProperty
        End Sub

        Private ReadOnly Property RetargetingTranslator As RetargetingModuleSymbol.RetargetingSymbolTranslator
            Get
                Return m_RetargetingModule.RetargetingTranslator
            End Get
        End Property

        Public ReadOnly Property UnderlyingProperty As PropertySymbol
            Get
                Return m_UnderlyingProperty
            End Get
        End Property

        Public ReadOnly Property RetargetingModule As RetargetingModuleSymbol
            Get
                Return m_RetargetingModule
            End Get
        End Property

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return m_UnderlyingProperty.IsImplicitlyDeclared
            End Get
        End Property

        Public Overrides ReadOnly Property IsWithEvents As Boolean
            Get
                Return m_UnderlyingProperty.IsWithEvents
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return RetargetingTranslator.Retarget(m_UnderlyingProperty.ContainingSymbol)
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return m_UnderlyingProperty.DeclaredAccessibility
            End Get
        End Property

        Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return RetargetingTranslator.GetRetargetedAttributes(m_UnderlyingProperty, m_LazyCustomAttributes)
        End Function

        Friend Overrides Function GetCustomAttributesToEmit() As IEnumerable(Of VisualBasicAttributeData)
            Return RetargetingTranslator.RetargetAttributes(m_UnderlyingProperty.GetCustomAttributesToEmit())
        End Function

        Public Overrides ReadOnly Property GetMethod As MethodSymbol
            Get
                Return If(m_UnderlyingProperty.GetMethod Is Nothing, Nothing, RetargetingTranslator.Retarget(m_UnderlyingProperty.GetMethod))
            End Get
        End Property

        Public Overrides ReadOnly Property SetMethod As MethodSymbol
            Get
                Return If(m_UnderlyingProperty.SetMethod Is Nothing, Nothing, RetargetingTranslator.Retarget(m_UnderlyingProperty.SetMethod))
            End Get
        End Property

        Friend Overrides ReadOnly Property AssociatedField As FieldSymbol
            Get
                Return If(m_UnderlyingProperty.AssociatedField Is Nothing, Nothing, RetargetingTranslator.Retarget(m_UnderlyingProperty.AssociatedField))
            End Get
        End Property

        Public Overrides ReadOnly Property IsDefault As Boolean
            Get
                Return m_UnderlyingProperty.IsDefault
            End Get
        End Property

        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return m_UnderlyingProperty.IsMustOverride
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return m_UnderlyingProperty.IsNotOverridable
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return m_UnderlyingProperty.IsOverridable
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return m_UnderlyingProperty.IsOverrides
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverloads As Boolean
            Get
                Return m_UnderlyingProperty.IsOverloads
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return m_UnderlyingProperty.IsShared
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return m_UnderlyingProperty.Name
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataName As String
            Get
                Return m_UnderlyingProperty.MetadataName
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return m_UnderlyingProperty.HasSpecialName
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return m_UnderlyingProperty.ObsoleteAttributeData
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return m_UnderlyingProperty.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return m_UnderlyingProperty.DeclaringSyntaxReferences
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                If m_LazyParameters.IsDefault Then
                    ImmutableInterlocked.InterlockedCompareExchange(m_LazyParameters, RetargetParameters(), Nothing)
                End If

                Return m_LazyParameters
            End Get
        End Property

        Public Overrides ReadOnly Property ParameterCount As Integer
            Get
                Return m_UnderlyingProperty.ParameterCount
            End Get
        End Property

        Private Function RetargetParameters() As ImmutableArray(Of ParameterSymbol)
            Dim list = m_UnderlyingProperty.Parameters
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

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return RetargetingTranslator.Retarget(m_UnderlyingProperty.Type, RetargetOptions.RetargetPrimitiveTypesByTypeCode)
            End Get
        End Property

        Public Overrides ReadOnly Property TypeCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return RetargetingTranslator.RetargetModifiers(m_UnderlyingProperty.TypeCustomModifiers, m_LazyCustomModifiers)
            End Get
        End Property

        Friend Overrides ReadOnly Property CallingConvention As Microsoft.Cci.CallingConvention
            Get
                Return m_UnderlyingProperty.CallingConvention
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of PropertySymbol)
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
            If m_lazyUseSiteErrorInfo Is ErrorFactory.EmptyErrorInfo Then
                m_lazyUseSiteErrorInfo = CalculateUseSiteErrorInfo()
            End If

            Return m_lazyUseSiteErrorInfo
        End Function

        Friend Overrides ReadOnly Property IsMyGroupCollectionProperty As Boolean
            Get
                Return m_UnderlyingProperty.IsMyGroupCollectionProperty
            End Get
        End Property

        Friend Overrides ReadOnly Property HasRuntimeSpecialName As Boolean
            Get
                Return m_UnderlyingProperty.HasRuntimeSpecialName
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
            Return m_UnderlyingProperty.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken)
        End Function
    End Class
End Namespace
