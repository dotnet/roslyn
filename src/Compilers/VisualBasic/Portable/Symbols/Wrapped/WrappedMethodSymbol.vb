' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Reflection
Imports System.Threading
Imports Microsoft.Cci

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents a method that is based on another method.
    ''' When inheriting from this class, one shouldn't assume that 
    ''' the default behavior it has is appropriate for every case.
    ''' That behavior should be carefully reviewed and derived type
    ''' should override behavior as appropriate.
    ''' </summary>
    Friend MustInherit Class WrappedMethodSymbol
        Inherits MethodSymbol

        Public MustOverride ReadOnly Property UnderlyingMethod As MethodSymbol

        Public Overrides ReadOnly Property IsVararg As Boolean
            Get
                Return Me.UnderlyingMethod.IsVararg
            End Get
        End Property

        Public Overrides ReadOnly Property IsGenericMethod As Boolean
            Get
                Return Me.UnderlyingMethod.IsGenericMethod
            End Get
        End Property

        Public Overrides ReadOnly Property Arity As Integer
            Get
                Return Me.UnderlyingMethod.Arity
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnsByRef As Boolean
            Get
                Return Me.UnderlyingMethod.ReturnsByRef
            End Get
        End Property

        Friend Overrides ReadOnly Property ParameterCount As Integer
            Get
                Return Me.UnderlyingMethod.ParameterCount
            End Get
        End Property

        Public Overrides ReadOnly Property IsExtensionMethod As Boolean
            Get
                Return Me.UnderlyingMethod.IsExtensionMethod
            End Get
        End Property

        Friend Overrides ReadOnly Property IsHiddenBySignature As Boolean
            Get
                Return Me.UnderlyingMethod.IsHiddenBySignature
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return Me.UnderlyingMethod.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return Me.UnderlyingMethod.DeclaringSyntaxReferences
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Me.UnderlyingMethod.DeclaredAccessibility
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return Me.UnderlyingMethod.IsShared
            End Get
        End Property

        Public Overrides ReadOnly Property IsExternalMethod As Boolean
            Get
                Return Me.UnderlyingMethod.IsExternalMethod
            End Get
        End Property

        Public Overrides ReadOnly Property IsAsync As Boolean
            Get
                Return Me.UnderlyingMethod.IsAsync
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return Me.UnderlyingMethod.IsOverrides
            End Get
        End Property

        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return Me.UnderlyingMethod.IsMustOverride
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return Me.UnderlyingMethod.IsNotOverridable
            End Get
        End Property

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return Me.UnderlyingMethod.IsImplicitlyDeclared
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMetadataFinal As Boolean
            Get
                Return Me.UnderlyingMethod.IsMetadataFinal
            End Get
        End Property

        Friend Overrides ReadOnly Property ReturnTypeMarshallingInformation As MarshalPseudoCustomAttributeData
            Get
                Return Me.UnderlyingMethod.ReturnTypeMarshallingInformation
            End Get
        End Property

        Friend Overrides ReadOnly Property HasDeclarativeSecurity As Boolean
            Get
                Return Me.UnderlyingMethod.HasDeclarativeSecurity
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Me.UnderlyingMethod.ObsoleteAttributeData
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return Me.UnderlyingMethod.Name
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return Me.UnderlyingMethod.HasSpecialName
            End Get
        End Property

        Friend Overrides ReadOnly Property ImplementationAttributes As MethodImplAttributes
            Get
                Return Me.UnderlyingMethod.ImplementationAttributes
            End Get
        End Property

        Public Overrides ReadOnly Property MethodKind As MethodKind
            Get
                Return Me.UnderlyingMethod.MethodKind
            End Get
        End Property

        Friend Overrides ReadOnly Property CallingConvention As Cci.CallingConvention
            Get
                Return Me.UnderlyingMethod.CallingConvention
            End Get
        End Property

        Friend Overrides ReadOnly Property IsAccessCheckedOnOverride As Boolean
            Get
                Return Me.UnderlyingMethod.IsAccessCheckedOnOverride
            End Get
        End Property

        Friend Overrides ReadOnly Property IsExternal As Boolean
            Get
                Return Me.UnderlyingMethod.IsExternal
            End Get
        End Property

        Friend Overrides ReadOnly Property HasRuntimeSpecialName As Boolean
            Get
                Return Me.UnderlyingMethod.HasRuntimeSpecialName
            End Get
        End Property

        Friend Overrides ReadOnly Property ReturnValueIsMarshalledExplicitly As Boolean
            Get
                Return Me.UnderlyingMethod.ReturnValueIsMarshalledExplicitly
            End Get
        End Property

        Friend Overrides ReadOnly Property ReturnValueMarshallingDescriptor As ImmutableArray(Of Byte)
            Get
                Return Me.UnderlyingMethod.ReturnValueMarshallingDescriptor
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMethodKindBasedOnSyntax As Boolean
            Get
                Return Me.UnderlyingMethod.IsMethodKindBasedOnSyntax
            End Get
        End Property

        Public Overrides ReadOnly Property IsIterator As Boolean
            Get
                Return Me.UnderlyingMethod.IsIterator
            End Get
        End Property

        Public Overrides ReadOnly Property IsInitOnly As Boolean
            Get
                Return Me.UnderlyingMethod.IsInitOnly
            End Get
        End Property

        Friend Overrides ReadOnly Property Syntax As SyntaxNode
            Get
                Return Me.UnderlyingMethod.Syntax
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverloads As Boolean
            Get
                Return Me.UnderlyingMethod.IsOverloads
            End Get
        End Property

        Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
            Get
                Return Me.UnderlyingMethod.GenerateDebugInfoImpl
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return Me.UnderlyingMethod.IsOverridable
            End Get
        End Property

        Friend Overrides Function IsMetadataNewSlot(Optional ignoreInterfaceImplementationChanges As Boolean = False) As Boolean
            Return Me.UnderlyingMethod.IsMetadataNewSlot(ignoreInterfaceImplementationChanges)
        End Function

        Public Overrides Function GetDllImportData() As DllImportData
            Return Me.UnderlyingMethod.GetDllImportData()
        End Function

        Friend Overrides Function GetSecurityInformation() As IEnumerable(Of SecurityAttribute)
            Return Me.UnderlyingMethod.GetSecurityInformation()
        End Function

        Friend Overrides Function GetAppliedConditionalSymbols() As ImmutableArray(Of String)
            Return Me.UnderlyingMethod.GetAppliedConditionalSymbols()
        End Function

        Public Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            Return Me.UnderlyingMethod.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken)
        End Function
    End Class
End Namespace
