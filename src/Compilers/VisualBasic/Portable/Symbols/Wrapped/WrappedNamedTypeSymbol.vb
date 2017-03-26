' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.Cci

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents a named type that is based on another named type.
    ''' When inheriting from this class, one shouldn't assume that 
    ''' the default behavior it has is appropriate for every case.
    ''' That behavior should be carefully reviewed and derived type
    ''' should override behavior as appropriate.
    ''' </summary>
    Friend MustInherit Class WrappedNamedTypeSymbol
        Inherits NamedTypeSymbol

        Public ReadOnly Property UnderlyingNamedType As NamedTypeSymbol

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return UnderlyingNamedType.IsImplicitlyDeclared
            End Get
        End Property

        Public Overrides ReadOnly Property Arity As Integer
            Get
                Return UnderlyingNamedType.Arity
            End Get
        End Property

        Public Overrides ReadOnly Property MightContainExtensionMethods As Boolean
            Get
                Return UnderlyingNamedType.MightContainExtensionMethods
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return UnderlyingNamedType.Name
            End Get
        End Property

        Public Overrides ReadOnly Property MetadataName As String
            Get
                Return UnderlyingNamedType.MetadataName
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return UnderlyingNamedType.HasSpecialName
            End Get
        End Property

        Friend Overrides ReadOnly Property MangleName As Boolean
            Get
                Return UnderlyingNamedType.MangleName
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return UnderlyingNamedType.DeclaredAccessibility
            End Get
        End Property

        Public Overrides ReadOnly Property TypeKind As TypeKind
            Get
                Return UnderlyingNamedType.TypeKind
            End Get
        End Property

        Friend Overrides ReadOnly Property IsInterface As Boolean
            Get
                Return UnderlyingNamedType.IsInterface
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return UnderlyingNamedType.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return UnderlyingNamedType.DeclaringSyntaxReferences
            End Get
        End Property

        Public Overrides ReadOnly Property IsMustInherit As Boolean
            Get
                Return UnderlyingNamedType.IsMustInherit
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotInheritable As Boolean
            Get
                Return UnderlyingNamedType.IsNotInheritable
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataAbstract As Boolean
            Get
                Return UnderlyingNamedType.IsMetadataAbstract
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataSealed As Boolean
            Get
                Return UnderlyingNamedType.IsMetadataSealed
            End Get
        End Property

        Friend Overrides ReadOnly Property DefaultPropertyName As String
            Get
                Return UnderlyingNamedType.DefaultPropertyName
            End Get
        End Property

        Friend Overrides ReadOnly Property CoClassType As TypeSymbol
            Get
                Return UnderlyingNamedType.CoClassType
            End Get
        End Property

        Friend Overrides ReadOnly Property HasEmbeddedAttribute As Boolean
            Get
                Return UnderlyingNamedType.HasEmbeddedAttribute
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return UnderlyingNamedType.ObsoleteAttributeData
            End Get
        End Property

        Friend Overrides ReadOnly Property ShouldAddWinRTMembers As Boolean
            Get
                Return UnderlyingNamedType.ShouldAddWinRTMembers
            End Get
        End Property

        Friend Overrides ReadOnly Property IsWindowsRuntimeImport As Boolean
            Get
                Return UnderlyingNamedType.IsWindowsRuntimeImport
            End Get
        End Property

        Friend Overrides ReadOnly Property Layout As TypeLayout
            Get
                Return UnderlyingNamedType.Layout
            End Get
        End Property

        Friend Overrides ReadOnly Property MarshallingCharSet As CharSet
            Get
                Return UnderlyingNamedType.MarshallingCharSet
            End Get
        End Property

        Friend Overrides ReadOnly Property IsSerializable As Boolean
            Get
                Return UnderlyingNamedType.IsSerializable
            End Get
        End Property

        Friend Overrides ReadOnly Property HasDeclarativeSecurity As Boolean
            Get
                Return UnderlyingNamedType.HasDeclarativeSecurity
            End Get
        End Property

        Public Sub New(underlyingType As NamedTypeSymbol)
            Debug.Assert(underlyingType IsNot Nothing)
            UnderlyingNamedType = underlyingType
        End Sub

        Public Overrides Function GetDocumentationCommentXml(
                                                     Optional preferredCulture As CultureInfo = Nothing,
                                                     Optional expandIncludes As Boolean = False,
                                                     Optional cancellationToken As CancellationToken = Nothing
                                                            ) As String
            Return UnderlyingNamedType.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken)
        End Function

        Friend Overrides Function GetSecurityInformation() As IEnumerable(Of SecurityAttribute)
            Return UnderlyingNamedType.GetSecurityInformation()
        End Function

        Friend Overrides Function GetAppliedConditionalSymbols() As ImmutableArray(Of String)
            Return UnderlyingNamedType.GetAppliedConditionalSymbols()
        End Function

        Friend Overrides Function GetAttributeUsageInfo() As AttributeUsageInfo
            Return UnderlyingNamedType.GetAttributeUsageInfo()
        End Function

        Friend Overrides Function GetGuidString(<Out()> ByRef guidString As String) As Boolean
            Return UnderlyingNamedType.GetGuidString(guidString)
        End Function
    End Class
End Namespace
