' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' A NoPiaMissingCanonicalTypeSymbol is a special kind of ErrorSymbol that represents
    ''' a NoPia embedded type symbol that was attempted to be substituted with canonical type, 
    ''' but the canonical type couldn't be found.
    ''' </summary>
    Friend NotInheritable Class NoPiaMissingCanonicalTypeSymbol
        Inherits ErrorTypeSymbol ' TODO: Should probably inherit from MissingMetadataType.TopLevel, but review TypeOf checks for MissingMetadataType.

        Private ReadOnly _embeddingAssembly As AssemblySymbol
        Private ReadOnly _guid As String
        Private ReadOnly _scope As String
        Private ReadOnly _identifier As String
        Private ReadOnly _fullTypeName As String

        Public Sub New(
            embeddingAssembly As AssemblySymbol,
            fullTypeName As String,
            guid As String,
            scope As String,
            identifier As String
        )
            _fullTypeName = fullTypeName
            _embeddingAssembly = embeddingAssembly
            _guid = guid
            _scope = scope
            _identifier = identifier
        End Sub

        Public ReadOnly Property EmbeddingAssembly As AssemblySymbol
            Get
                Return _embeddingAssembly
            End Get
        End Property

        Public ReadOnly Property FullTypeName As String
            Get
                Return _fullTypeName
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return _fullTypeName
            End Get
        End Property

        Friend Overrides ReadOnly Property MangleName As Boolean
            Get
                ' Canonical type cannot be generic.
                Debug.Assert(Arity = 0)
                Return False
            End Get
        End Property

        Public ReadOnly Property Guid As String
            Get
                Return _guid
            End Get
        End Property

        Public ReadOnly Property Scope As String
            Get
                Return _scope
            End Get
        End Property

        Public ReadOnly Property Identifier As String
            Get
                Return _identifier
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
                Return ErrorFactory.ErrorInfo(ERRID.ERR_AbsentReferenceToPIA1, _fullTypeName)
            End Get
        End Property
    End Class

End Namespace
