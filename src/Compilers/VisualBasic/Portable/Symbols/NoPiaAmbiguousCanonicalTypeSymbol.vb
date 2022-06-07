' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' A NoPiaAmbiguousCanonicalTypeSymbol is a special kind of ErrorSymbol that represents
    ''' a NoPia embedded type symbol that was attempted to be substituted with canonical type, 
    ''' but the canonical type was ambiguous.
    ''' </summary>
    Friend NotInheritable Class NoPiaAmbiguousCanonicalTypeSymbol
        Inherits ErrorTypeSymbol

        Private ReadOnly _embeddingAssembly As AssemblySymbol
        Private ReadOnly _firstCandidate As NamedTypeSymbol
        Private ReadOnly _secondCandidate As NamedTypeSymbol

        Public Sub New(
            embeddingAssembly As AssemblySymbol,
            firstCandidate As NamedTypeSymbol,
            secondCandidate As NamedTypeSymbol
        )
            _embeddingAssembly = embeddingAssembly
            _firstCandidate = firstCandidate
            _secondCandidate = secondCandidate
        End Sub

        Friend Overrides ReadOnly Property MangleName As Boolean
            Get
                Debug.Assert(Arity = 0)
                Return False
            End Get
        End Property

        Public ReadOnly Property EmbeddingAssembly As AssemblySymbol
            Get
                Return _embeddingAssembly
            End Get
        End Property

        Public ReadOnly Property FirstCandidate As NamedTypeSymbol
            Get
                Return _firstCandidate
            End Get
        End Property

        Public ReadOnly Property SecondCandidate As NamedTypeSymbol
            Get
                Return _secondCandidate
            End Get
        End Property

        Public Overrides Function GetHashCode() As Integer
            Return RuntimeHelpers.GetHashCode(Me)
        End Function

        Public Overrides Function Equals(obj As TypeSymbol, comparison As TypeCompareKind) As Boolean
            Return obj Is Me
        End Function

        Friend Overrides ReadOnly Property ErrorInfo As DiagnosticInfo
            Get
                ' It doesn't look like Dev10 had a special error for this particular scenario.
                ' ERR_AbsentReferenceToPIA1 should be good enough and we already ignore it, 
                ' when it comes from implemented interfaces.
                Return ErrorFactory.ErrorInfo(ERRID.ERR_AbsentReferenceToPIA1, CustomSymbolDisplayFormatter.QualifiedName(_firstCandidate))
            End Get
        End Property
    End Class

End Namespace
