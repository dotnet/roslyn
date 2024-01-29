' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' This class represents a base class for compiler generated synthesized method symbols
    ''' that must be emitted in the compiler generated PrivateImplementationDetails class.
    ''' SynthesizedGlobalMethodBase symbols don't have a ContainingType, there are global to
    ''' the containing source module and are Public Shared methods.
    ''' </summary>
    Friend MustInherit Class SynthesizedGlobalMethodBase
        Inherits MethodSymbol
        Implements ISynthesizedGlobalMethodSymbol

        Protected ReadOnly m_privateImplType As PrivateImplementationDetails

        Protected ReadOnly m_containingModule As SourceModuleSymbol
        Protected ReadOnly m_name As String

        Protected Sub New(containingModule As SourceModuleSymbol, name As String, privateImplType As PrivateImplementationDetails)
            Debug.Assert(containingModule IsNot Nothing)

            m_containingModule = containingModule
            m_name = name
            m_privateImplType = privateImplType
        End Sub

        Public NotOverridable Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return True
            End Get
        End Property

        ''' <summary>
        ''' Gets the symbol name. Returns the empty string if unnamed.
        ''' </summary>
        Public NotOverridable Overrides ReadOnly Property Name As String
            Get
                Return m_name
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides Function GetAppliedConditionalSymbols() As ImmutableArray(Of String)
            Return ImmutableArray(Of String).Empty
        End Function

        ''' <summary>
        ''' Gets a value indicating whether this instance is abstract or not.
        ''' </summary>
        ''' <value>
        ''' <c>true</c> if this instance is abstract; otherwise, <c>false</c>.
        ''' </value>
        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Gets a value indicating whether this instance is not overridable.
        ''' </summary>
        ''' <value>
        ''' <c>true</c> if this instance is not overridable; otherwise, <c>false</c>.
        ''' </value>
        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Gets a value indicating whether this instance is overloads.
        ''' </summary>
        ''' <value>
        ''' <c>true</c> if this instance is overloads; otherwise, <c>false</c>.
        ''' </value>
        Public Overrides ReadOnly Property IsOverloads As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Gets a value indicating whether this instance is overridable.
        ''' </summary>
        ''' <value>
        ''' <c>true</c> if this instance is overridable; otherwise, <c>false</c>.
        ''' </value>
        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Gets a value indicating whether this instance is overrides.
        ''' </summary>
        ''' <value>
        ''' <c>true</c> if this instance is overrides; otherwise, <c>false</c>.
        ''' </value>
        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Gets a value indicating whether this instance is shared.
        ''' </summary>
        ''' <value>
        '''   <c>true</c> if this instance is shared; otherwise, <c>false</c>.
        ''' </value>
        Public NotOverridable Overrides ReadOnly Property IsShared As Boolean
            Get
                Return True
            End Get
        End Property

        Friend Overrides Function GetLexicalSortKey() As LexicalSortKey
            Return LexicalSortKey.NotInSource
        End Function

        ''' <summary>
        ''' A potentially empty collection of locations that correspond to this instance.
        ''' </summary>
        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray(Of Location).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property

        ''' <summary>
        ''' Gets what kind of method this is. There are several different kinds of things in the
        ''' VB language that are represented as methods. This property allow distinguishing those things
        ''' without having to decode the name of the method.
        ''' </summary>
        Public Overrides ReadOnly Property MethodKind As MethodKind
            Get
                Return MethodKind.Ordinary
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property IsMethodKindBasedOnSyntax As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' The parameters forming part of this signature.
        ''' </summary>
        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                Return ImmutableArray(Of ParameterSymbol).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Accessibility.Friend
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property Syntax As SyntaxNode
            Get
                Return VisualBasic.VisualBasicSyntaxTree.Dummy.GetRoot()
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property Arity As Integer
            Get
                Return 0
            End Get
        End Property

        Public Overrides ReadOnly Property AssociatedSymbol As Symbol
            Get
                Return Nothing
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property CallingConvention As Microsoft.Cci.CallingConvention
            Get
                Return If(IsShared, Microsoft.Cci.CallingConvention.Default, Microsoft.Cci.CallingConvention.HasThis)
            End Get
        End Property

        Public ReadOnly Property ContainingPrivateImplementationDetailsType As PrivateImplementationDetails Implements ISynthesizedGlobalMethodSymbol.ContainingPrivateImplementationDetailsType
            Get
                Return m_privateImplType
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingType As NamedTypeSymbol
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingModule As ModuleSymbol
            Get
                Return m_containingModule
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingAssembly As AssemblySymbol
            Get
                Return m_containingModule.ContainingAssembly
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of MethodSymbol)
            Get
                Return ImmutableArray(Of MethodSymbol).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property OverriddenMethod As MethodSymbol
            Get
                Return Nothing
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsExtensionMethod As Boolean
            Get
                Return False
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsExternalMethod As Boolean
            Get
                Return False
            End Get
        End Property

        Public NotOverridable Overrides Function GetDllImportData() As DllImportData
            Return Nothing
        End Function

        Friend NotOverridable Overrides ReadOnly Property ReturnTypeMarshallingInformation As MarshalPseudoCustomAttributeData
            Get
                Return Nothing
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property ImplementationAttributes As Reflection.MethodImplAttributes
            Get
                Return Nothing
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property HasDeclarativeSecurity As Boolean
            Get
                Return False
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Nothing
            End Get
        End Property

        Friend NotOverridable Overrides Function GetSecurityInformation() As IEnumerable(Of Microsoft.Cci.SecurityAttribute)
            Throw ExceptionUtilities.Unreachable
        End Function

        Public NotOverridable Overrides ReadOnly Property IsVararg As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property TypeArguments As ImmutableArray(Of TypeSymbol)
            Get
                Return ImmutableArray(Of TypeSymbol).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                Return ImmutableArray(Of TypeParameterSymbol).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnTypeCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return ImmutableArray(Of CustomModifier).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property RefCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return ImmutableArray(Of CustomModifier).Empty
            End Get
        End Property

        Public Overrides Function GetReturnTypeAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return ImmutableArray(Of VisualBasicAttributeData).Empty
        End Function

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

        Public NotOverridable Overrides ReadOnly Property IsInitOnly As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend NotOverridable Overrides ReadOnly Property HasSetsRequiredMembers As Boolean
            Get
                Return False
            End Get
        End Property
    End Class

End Namespace
