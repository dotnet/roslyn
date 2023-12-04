' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Reflection.Metadata
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' A representation of a method symbol that is intended only to be used for comparison purposes
    ''' (esp in MethodSignatureComparer).
    ''' </summary>
    Friend NotInheritable Class SignatureOnlyMethodSymbol
        Inherits MethodSymbol
        Private ReadOnly _name As String
        Private ReadOnly _containingType As TypeSymbol
        Private ReadOnly _methodKind As MethodKind
        Private ReadOnly _callingConvention As CallingConvention
        Private ReadOnly _typeParameters As ImmutableArray(Of TypeParameterSymbol)
        Private ReadOnly _parameters As ImmutableArray(Of ParameterSymbol)
        Private ReadOnly _returnsByRef As Boolean
        Private ReadOnly _returnType As TypeSymbol
        Private ReadOnly _returnTypeCustomModifiers As ImmutableArray(Of CustomModifier)
        Private ReadOnly _refCustomModifiers As ImmutableArray(Of CustomModifier)
        Private ReadOnly _explicitInterfaceImplementations As ImmutableArray(Of MethodSymbol)
        Private ReadOnly _isOverrides As Boolean

        Public Sub New(ByVal name As String, ByVal m_containingType As TypeSymbol, ByVal methodKind As MethodKind, ByVal callingConvention As CallingConvention, ByVal typeParameters As ImmutableArray(Of TypeParameterSymbol), ByVal parameters As ImmutableArray(Of ParameterSymbol),
                       ByVal returnsByRef As Boolean, ByVal returnType As TypeSymbol, ByVal returnTypeCustomModifiers As ImmutableArray(Of CustomModifier), refCustomModifiers As ImmutableArray(Of CustomModifier),
                       ByVal explicitInterfaceImplementations As ImmutableArray(Of MethodSymbol),
                       Optional isOverrides As Boolean = False)
            _callingConvention = callingConvention
            _typeParameters = typeParameters
            _returnsByRef = returnsByRef
            _returnType = returnType
            _returnTypeCustomModifiers = returnTypeCustomModifiers
            _refCustomModifiers = refCustomModifiers
            _parameters = parameters
            _explicitInterfaceImplementations = explicitInterfaceImplementations.NullToEmpty()
            _containingType = m_containingType
            _methodKind = methodKind
            _name = name
            _isOverrides = isOverrides
        End Sub

        Friend Overrides ReadOnly Property CallingConvention() As CallingConvention
            Get
                Return _callingConvention
            End Get
        End Property

        Public Overrides ReadOnly Property IsVararg() As Boolean
            Get
                Return New SignatureHeader(CByte(_callingConvention)).CallingConvention = SignatureCallingConvention.VarArgs
            End Get
        End Property

        Public Overrides ReadOnly Property IsGenericMethod() As Boolean
            Get
                Return Arity > 0
            End Get
        End Property

        Public Overrides ReadOnly Property Arity() As Integer
            Get
                Return _typeParameters.Length
            End Get
        End Property

        Public Overrides ReadOnly Property TypeParameters() As ImmutableArray(Of TypeParameterSymbol)
            Get
                Return _typeParameters
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnsByRef As Boolean
            Get
                Return _returnsByRef
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnType() As TypeSymbol
            Get
                Return _returnType
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnTypeCustomModifiers() As ImmutableArray(Of CustomModifier)
            Get
                Return _returnTypeCustomModifiers
            End Get
        End Property

        Public Overrides ReadOnly Property RefCustomModifiers() As ImmutableArray(Of CustomModifier)
            Get
                Return _refCustomModifiers
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters() As ImmutableArray(Of ParameterSymbol)
            Get
                Return _parameters
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations() As ImmutableArray(Of MethodSymbol)
            Get
                Return _explicitInterfaceImplementations
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol() As Symbol
            Get
                Return _containingType
            End Get
        End Property

        Public Overrides ReadOnly Property MethodKind() As MethodKind
            Get
                Return _methodKind
            End Get
        End Property

        Friend Overrides ReadOnly Property IsMethodKindBasedOnSyntax As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property Name() As String
            Get
                Return _name
            End Get
        End Property

        Public Overrides ReadOnly Property IsSub As Boolean
            Get
                Return ReturnType.SpecialType = SpecialType.System_Void
            End Get
        End Property

        Public Overrides ReadOnly Property IsAsync As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsIterator As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsInitOnly As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Nothing
            End Get
        End Property

#Region "Not used by MethodSignatureComparer"

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property TypeArguments() As ImmutableArray(Of TypeSymbol)
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property AssociatedSymbol() As Symbol
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property IsExtensionMethod() As Boolean
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property Locations() As ImmutableArray(Of Location)
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility() As Accessibility
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingAssembly() As AssemblySymbol
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return _isOverrides
            End Get
        End Property

        Friend Overrides ReadOnly Property Syntax As SyntaxNode
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverloads As Boolean
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property IsExternalMethod As Boolean
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides Function GetDllImportData() As DllImportData
            Return Nothing
        End Function

        Friend Overrides ReadOnly Property ReturnTypeMarshallingInformation As MarshalPseudoCustomAttributeData
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Friend Overrides ReadOnly Property ImplementationAttributes As Reflection.MethodImplAttributes
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Friend Overrides ReadOnly Property HasDeclarativeSecurity As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides Function GetSecurityInformation() As IEnumerable(Of SecurityAttribute)
            Throw ExceptionUtilities.Unreachable
        End Function

        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Friend Overrides Function GetAppliedConditionalSymbols() As ImmutableArray(Of String)
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend Overrides Function IsMetadataNewSlot(Optional ignoreInterfaceImplementationChanges As Boolean = False) As Boolean
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend Overrides ReadOnly Property OverriddenMembers As OverriddenMembersResult(Of MethodSymbol)
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property OverriddenMethod As MethodSymbol
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Friend Overrides Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend Overrides ReadOnly Property HasSetsRequiredMembers As Boolean
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property
#End Region
    End Class
End Namespace
