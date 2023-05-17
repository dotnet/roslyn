' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
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
        Private ReadOnly _retargetingModule As RetargetingModuleSymbol

        ''' <summary>
        ''' The underlying FieldSymbol, cannot be another RetargetingFieldSymbol.
        ''' </summary>
        Private ReadOnly _underlyingField As FieldSymbol

        Private _lazyCustomModifiers As ImmutableArray(Of CustomModifier)

        ''' <summary>
        ''' Retargeted custom attributes
        ''' </summary>
        ''' <remarks></remarks>
        Private _lazyCustomAttributes As ImmutableArray(Of VisualBasicAttributeData)

        Private _lazyCachedUseSiteInfo As CachedUseSiteInfo(Of AssemblySymbol) = CachedUseSiteInfo(Of AssemblySymbol).Uninitialized ' Indicates unknown state. 

        Public Sub New(retargetingModule As RetargetingModuleSymbol, underlyingField As FieldSymbol)

            Debug.Assert(retargetingModule IsNot Nothing)
            Debug.Assert(underlyingField IsNot Nothing)

            If TypeOf underlyingField Is RetargetingFieldSymbol Then
                Throw New ArgumentException()
            End If

            _retargetingModule = retargetingModule
            _underlyingField = underlyingField
        End Sub

        Private ReadOnly Property RetargetingTranslator As RetargetingModuleSymbol.RetargetingSymbolTranslator
            Get
                Return _retargetingModule.RetargetingTranslator
            End Get
        End Property

        Public ReadOnly Property UnderlyingField As FieldSymbol
            Get
                Return _underlyingField
            End Get
        End Property

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return _underlyingField.IsImplicitlyDeclared
            End Get
        End Property

        Public ReadOnly Property RetargetingModule As RetargetingModuleSymbol
            Get
                Return _retargetingModule
            End Get
        End Property

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return RetargetingTranslator.Retarget(_underlyingField.Type, RetargetOptions.RetargetPrimitiveTypesByTypeCode)
            End Get
        End Property

        Public Overrides ReadOnly Property CustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return RetargetingTranslator.RetargetModifiers(_underlyingField.CustomModifiers, _lazyCustomModifiers)
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return RetargetingTranslator.Retarget(_underlyingField.ContainingSymbol)
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return _underlyingField.DeclaredAccessibility
            End Get
        End Property

        Public Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return RetargetingTranslator.GetRetargetedAttributes(_underlyingField, _lazyCustomAttributes)
        End Function

        Friend Overrides Function GetCustomAttributesToEmit(moduleBuilder As PEModuleBuilder) As IEnumerable(Of VisualBasicAttributeData)
            Return RetargetingTranslator.RetargetAttributes(_underlyingField.GetCustomAttributesToEmit(moduleBuilder))
        End Function

        Public Overrides ReadOnly Property ContainingAssembly As AssemblySymbol
            Get
                Return _retargetingModule.ContainingAssembly
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingModule As ModuleSymbol
            Get
                Return _retargetingModule
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return _underlyingField.Name
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataName As String
            Get
                Return _underlyingField.MetadataName
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return _underlyingField.HasSpecialName
            End Get
        End Property

        Friend Overrides ReadOnly Property HasRuntimeSpecialName As Boolean
            Get
                Return _underlyingField.HasRuntimeSpecialName
            End Get
        End Property

        Friend Overrides ReadOnly Property IsNotSerialized As Boolean
            Get
                Return _underlyingField.IsNotSerialized
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMarshalledExplicitly As Boolean
            Get
                Return _underlyingField.IsMarshalledExplicitly
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingInformation As MarshalPseudoCustomAttributeData
            Get
                Return Me.RetargetingTranslator.Retarget(Me.UnderlyingField.MarshallingInformation)
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingDescriptor As ImmutableArray(Of Byte)
            Get
                Return _underlyingField.MarshallingDescriptor
            End Get
        End Property

        Friend Overrides ReadOnly Property TypeLayoutOffset As Integer?
            Get
                Return _underlyingField.TypeLayoutOffset
            End Get
        End Property

        Public Overrides ReadOnly Property AssociatedSymbol As Symbol
            Get
                Dim associated As Symbol = _underlyingField.AssociatedSymbol
                Return If(associated Is Nothing, Nothing, Me.RetargetingTranslator.Retarget(associated))
            End Get
        End Property

        Public Overrides ReadOnly Property IsReadOnly As Boolean
            Get
                Return _underlyingField.IsReadOnly
            End Get
        End Property

        Public Overrides ReadOnly Property IsConst As Boolean
            Get
                Return _underlyingField.IsConst
            End Get
        End Property

        Public Overrides ReadOnly Property ConstantValue As Object
            Get
                Return _underlyingField.ConstantValue
            End Get
        End Property

        Friend Overrides Function GetConstantValue(inProgress As ConstantFieldsInProgress) As ConstantValue
            Return _underlyingField.GetConstantValue(inProgress)
        End Function

        Public Overrides ReadOnly Property IsRequired As Boolean
            Get
                Debug.Assert(Not _underlyingField.IsRequired)
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return _underlyingField.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return _underlyingField.DeclaringSyntaxReferences
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return _underlyingField.IsShared
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return _underlyingField.ObsoleteAttributeData
            End Get
        End Property

        Friend Overrides Function GetUseSiteInfo() As UseSiteInfo(Of AssemblySymbol)
            Dim primaryDependency As AssemblySymbol = Me.PrimaryDependency

            If Not _lazyCachedUseSiteInfo.IsInitialized Then
                _lazyCachedUseSiteInfo.Initialize(primaryDependency, CalculateUseSiteInfo())
            End If

            Return _lazyCachedUseSiteInfo.ToUseSiteInfo(primaryDependency)
        End Function

        ''' <remarks>
        ''' This is for perf, not for correctness.
        ''' </remarks>
        Friend Overrides ReadOnly Property DeclaringCompilation As VisualBasicCompilation
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            Return _underlyingField.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken)
        End Function
    End Class
End Namespace
