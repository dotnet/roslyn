' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols.Retargeting

    ''' <summary>
    ''' Represents a field in a RetargetingModuleSymbol. Essentially this is a wrapper around 
    ''' another FieldSymbol that is responsible for retargeting symbols from one assembly to another. 
    ''' It can retarget symbols for multiple assemblies at the same time.
    ''' </summary>
    Friend NotInheritable Class RetargetingFieldSymbol
        Inherits FieldSymbol

        ''' <summary>
        ''' Owning RetargetingModuleSymbol.
        ''' </summary>
        Private ReadOnly m_RetargetingModule As RetargetingModuleSymbol

        ''' <summary>
        ''' The underlying FieldSymbol, cannot be another RetargetingFieldSymbol.
        ''' </summary>
        Private ReadOnly m_UnderlyingField As FieldSymbol

        Private m_LazyCustomModifiers As ImmutableArray(Of CustomModifier)

        ''' <summary>
        ''' Retargeted custom attributes
        ''' </summary>
        ''' <remarks></remarks>
        Private m_LazyCustomAttributes As ImmutableArray(Of VisualBasicAttributeData)

        Private m_lazyUseSiteErrorInfo As DiagnosticInfo = ErrorFactory.EmptyErrorInfo ' Indicates unknown state. 

        Public Sub New(retargetingModule As RetargetingModuleSymbol, underlyingField As FieldSymbol)

            Debug.Assert(retargetingModule IsNot Nothing)
            Debug.Assert(underlyingField IsNot Nothing)

            If TypeOf underlyingField Is RetargetingFieldSymbol Then
                Throw New ArgumentException()
            End If

            m_RetargetingModule = retargetingModule
            m_UnderlyingField = underlyingField
        End Sub

        Private ReadOnly Property RetargetingTranslator As RetargetingModuleSymbol.RetargetingSymbolTranslator
            Get
                Return m_RetargetingModule.RetargetingTranslator
            End Get
        End Property

        Public ReadOnly Property UnderlyingField As FieldSymbol
            Get
                Return m_UnderlyingField
            End Get
        End Property

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return m_UnderlyingField.IsImplicitlyDeclared
            End Get
        End Property

        Public ReadOnly Property RetargetingModule As RetargetingModuleSymbol
            Get
                Return m_RetargetingModule
            End Get
        End Property

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return RetargetingTranslator.Retarget(m_UnderlyingField.Type, RetargetOptions.RetargetPrimitiveTypesByTypeCode)
            End Get
        End Property

        Public Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return RetargetingTranslator.RetargetModifiers(m_UnderlyingField.CustomModifiers, m_LazyCustomModifiers)
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return RetargetingTranslator.Retarget(m_UnderlyingField.ContainingSymbol)
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return m_UnderlyingField.DeclaredAccessibility
            End Get
        End Property

        Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return RetargetingTranslator.GetRetargetedAttributes(m_UnderlyingField, m_LazyCustomAttributes)
        End Function

        Friend Overrides Function GetCustomAttributesToEmit() As IEnumerable(Of VisualBasicAttributeData)
            Return RetargetingTranslator.RetargetAttributes(m_UnderlyingField.GetCustomAttributesToEmit())
        End Function

        Public Overrides ReadOnly Property ContainingAssembly As AssemblySymbol
            Get
                Return m_RetargetingModule.ContainingAssembly
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingModule As ModuleSymbol
            Get
                Return m_RetargetingModule
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return m_UnderlyingField.Name
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataName As String
            Get
                Return m_UnderlyingField.MetadataName
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return m_UnderlyingField.HasSpecialName
            End Get
        End Property

        Friend Overrides ReadOnly Property HasRuntimeSpecialName As Boolean
            Get
                Return m_UnderlyingField.HasRuntimeSpecialName
            End Get
        End Property

        Friend Overrides ReadOnly Property IsNotSerialized As Boolean
            Get
                Return m_UnderlyingField.IsNotSerialized
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMarshalledExplicitly As Boolean
            Get
                Return m_UnderlyingField.IsMarshalledExplicitly
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingInformation As MarshalPseudoCustomAttributeData
            Get
                Return Me.RetargetingTranslator.Retarget(Me.UnderlyingField.MarshallingInformation)
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingDescriptor As ImmutableArray(Of Byte)
            Get
                Return m_UnderlyingField.MarshallingDescriptor
            End Get
        End Property

        Friend Overrides ReadOnly Property TypeLayoutOffset As Integer?
            Get
                Return m_UnderlyingField.TypeLayoutOffset
            End Get
        End Property

        Public Overrides ReadOnly Property AssociatedSymbol As Symbol
            Get
                Dim associated As Symbol = m_UnderlyingField.AssociatedSymbol
                Return If(associated Is Nothing, Nothing, Me.RetargetingTranslator.Retarget(associated))
            End Get
        End Property

        Public Overrides ReadOnly Property IsReadOnly As Boolean
            Get
                Return m_UnderlyingField.IsReadOnly
            End Get
        End Property

        Public Overrides ReadOnly Property IsConst As Boolean
            Get
                Return m_UnderlyingField.IsConst
            End Get
        End Property

        Public Overrides ReadOnly Property ConstantValue As Object
            Get
                Return m_UnderlyingField.ConstantValue
            End Get
        End Property

        Friend Overrides Function GetConstantValue(inProgress As SymbolsInProgress(Of FieldSymbol)) As ConstantValue
            Return m_UnderlyingField.GetConstantValue(inProgress)
        End Function

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return m_UnderlyingField.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return m_UnderlyingField.DeclaringSyntaxReferences
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return m_UnderlyingField.IsShared
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return m_UnderlyingField.ObsoleteAttributeData
            End Get
        End Property

        Friend Overrides Function GetUseSiteErrorInfo() As DiagnosticInfo
            If m_lazyUseSiteErrorInfo Is ErrorFactory.EmptyErrorInfo Then
                m_lazyUseSiteErrorInfo = CalculateUseSiteErrorInfo()
            End If

            Return m_lazyUseSiteErrorInfo
        End Function

        ''' <remarks>
        ''' This is for perf, not for correctness.
        ''' </remarks>
        Friend Overrides ReadOnly Property DeclaringCompilation As VBCompilation
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            Return m_UnderlyingField.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken)
        End Function
    End Class
End Namespace
