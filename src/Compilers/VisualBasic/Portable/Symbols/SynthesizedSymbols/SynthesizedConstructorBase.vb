' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' This class represents a base class for compiler generated constructors
    ''' </summary>
    Friend MustInherit Class SynthesizedConstructorBase
        Inherits SynthesizedMethodBase

        ''' <summary>
        ''' Flag to indicate if this is a shared constructor or an instance constructor.
        ''' </summary>
        Protected ReadOnly m_isShared As Boolean

        Protected ReadOnly m_syntaxReference As SyntaxReference

        '  NOTE: m_voidType may be nothing if we generate the constructor for PE symbol
        Protected ReadOnly m_voidType As TypeSymbol

        ''' <summary>
        ''' Initializes a new instance of the <see cref="SynthesizedConstructorBase" /> class.
        ''' </summary>
        ''' <param name="container">The containing type for the synthesized constructor.</param>
        ''' <param name="isShared">if set to <c>true</c> if this is a shared constructor.</param>
        Protected Sub New(
            syntaxReference As SyntaxReference,
            container As NamedTypeSymbol,
            isShared As Boolean,
            binder As Binder,
            diagnostics As BindingDiagnosticBag
        )
            MyBase.New(container)

            m_syntaxReference = syntaxReference
            m_isShared = isShared

            If binder IsNot Nothing Then
                Debug.Assert(diagnostics IsNot Nothing)
                m_voidType = binder.GetSpecialType(SpecialType.System_Void, syntaxReference.GetSyntax(), diagnostics)
            Else
                ' NOTE: binder is Nothing for constructors generated for some 
                '       external types, like PENamedTypeSymbol; 
                '       in such cases use site error may not be reported
                m_voidType = ContainingAssembly.GetSpecialType(SpecialType.System_Void)
            End If
        End Sub

        ''' <summary>
        ''' Gets the symbol name. Returns the empty string if unnamed.
        ''' </summary>
        Public NotOverridable Overrides ReadOnly Property Name As String
            Get
                Return If(m_isShared, WellKnownMemberNames.StaticConstructorName, WellKnownMemberNames.InstanceConstructorName)
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return True
            End Get
        End Property

        ''' <summary>
        ''' Gets a <see cref="Accessibility" /> indicating the declared accessibility for the symbol.
        ''' Returns NotApplicable if no accessibility is declared.
        ''' </summary>
        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                If Me.IsShared Then
                    ' Shared constructor is supposed to be private
                    Return Accessibility.Private
                ElseIf m_containingType.IsMustInherit Then
                    Return Accessibility.Protected
                Else
                    Return Accessibility.Public
                End If
            End Get
        End Property

        ''' <summary>
        ''' Gets a value indicating whether this instance is abstract or not.
        ''' </summary>
        ''' <value>
        ''' <c>true</c> if this instance is abstract; otherwise, <c>false</c>.
        ''' </value>
        Public NotOverridable Overrides ReadOnly Property IsMustOverride As Boolean
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
        Public NotOverridable Overrides ReadOnly Property IsNotOverridable As Boolean
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
        Public NotOverridable Overrides ReadOnly Property IsOverridable As Boolean
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
        Public NotOverridable Overrides ReadOnly Property IsOverrides As Boolean
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
                Return m_isShared
            End Get
        End Property

        ''' <summary>
        ''' Gets a value indicating whether this instance is sub.
        ''' </summary>
        ''' <value>
        '''   <c>true</c> if this instance is sub; otherwise, <c>false</c>.
        ''' </value>
        Public NotOverridable Overrides ReadOnly Property IsSub As Boolean
            Get
                Return True
            End Get
        End Property

        Friend Overrides Function GetLexicalSortKey() As LexicalSortKey
            Return m_containingType.GetLexicalSortKey()
        End Function

        ''' <summary>
        ''' A potentially empty collection of locations that correspond to this instance.
        ''' </summary>
        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return m_containingType.Locations
            End Get
        End Property

        ''' <summary>
        ''' Gets what kind of method this is. There are several different kinds of things in the
        ''' VB language that are represented as methods. This property allow distinguishing those things
        ''' without having to decode the name of the method.
        ''' </summary>
        Public NotOverridable Overrides ReadOnly Property MethodKind As MethodKind
            Get
                Return If(m_isShared, MethodKind.SharedConstructor, MethodKind.Constructor)
            End Get
        End Property

        ''' <summary>
        ''' Gets the return type of the method.
        ''' </summary>
        Public NotOverridable Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                Return m_voidType
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property Syntax As SyntaxNode
            Get
                Return If(m_syntaxReference Is Nothing, Nothing, DirectCast(m_syntaxReference.GetSyntax(), VisualBasicSyntaxNode))
            End Get
        End Property

        Public Overrides ReadOnly Property IsExternalMethod As Boolean
            Get
                Dim containingType = Me.ContainingType
                Return containingType IsNot Nothing AndAlso containingType.IsComImport
            End Get
        End Property
    End Class

End Namespace
