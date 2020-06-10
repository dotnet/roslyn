' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Text
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    <DebuggerDisplay("{GetDebuggerDisplay(), nq}")>
    Friend Class SynthesizedLocal
        Inherits LocalSymbol

        Private ReadOnly _kind As SynthesizedLocalKind
        Private ReadOnly _isByRef As Boolean
        Private ReadOnly _syntaxOpt As SyntaxNode

        Friend Sub New(container As Symbol,
                       type As TypeSymbol,
                       kind As SynthesizedLocalKind,
                       Optional syntaxOpt As SyntaxNode = Nothing,
                       Optional isByRef As Boolean = False)
            MyBase.New(container, type)

            Debug.Assert(Not kind.IsLongLived OrElse syntaxOpt IsNot Nothing)

            Me._kind = kind
            Me._syntaxOpt = syntaxOpt
            Me._isByRef = isByRef
        End Sub

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return If(_syntaxOpt Is Nothing, ImmutableArray(Of Location).Empty, ImmutableArray.Create(_syntaxOpt.GetLocation()))
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return If(_syntaxOpt Is Nothing, ImmutableArray(Of SyntaxReference).Empty, ImmutableArray.Create(_syntaxOpt.GetReference()))
            End Get
        End Property

        Friend Overrides Function GetDeclaratorSyntax() As SyntaxNode
            Debug.Assert(_syntaxOpt IsNot Nothing)
            Return _syntaxOpt
        End Function

        Friend NotOverridable Overrides ReadOnly Property IdentifierToken As SyntaxToken
            Get
                Return Nothing
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property IdentifierLocation As Location
            Get
                Return NoLocation.Singleton
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides ReadOnly Property IsByRef As Boolean
            Get
                Return _isByRef
            End Get
        End Property

        Friend Overrides ReadOnly Property SynthesizedKind As SynthesizedLocalKind
            Get
                Return _kind
            End Get
        End Property

        Friend Overrides ReadOnly Property DeclarationKind As LocalDeclarationKind
            Get
                Return LocalDeclarationKind.None
            End Get
        End Property

        Public Overrides ReadOnly Property IsFunctionValue As Boolean
            Get
                Return _kind = SynthesizedLocalKind.FunctionReturnValue
            End Get
        End Property

        Private Function GetDebuggerDisplay() As String
            Dim builder As New StringBuilder()

            builder.Append(_kind.ToString())
            builder.Append(" ")
            builder.Append(Me.Type.ToDisplayString(SymbolDisplayFormat.TestFormat))

            Return builder.ToString()
        End Function
    End Class

End Namespace
