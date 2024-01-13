' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' An error type symbol with name and diagnostic. More info can be added in the future.
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class ExtendedErrorTypeSymbol
        Inherits InstanceErrorTypeSymbol

        Private ReadOnly _diagnosticInfo As DiagnosticInfo
        Private ReadOnly _reportErrorWhenReferenced As Boolean
        Private ReadOnly _name As String

        ' Best guess at what user meant, and why they weren't accepted.
        Private ReadOnly _candidateSymbols As ImmutableArray(Of Symbol)
        Private ReadOnly _resultKind As LookupResultKind

        Private ReadOnly _containingSymbol As NamespaceOrTypeSymbol

        ' Only compiler creates an error symbol.
        Friend Sub New(errorInfo As DiagnosticInfo,
                       Optional reportErrorWhenReferenced As Boolean = False,
                       Optional nonErrorGuessType As NamedTypeSymbol = Nothing)
            Me.New(errorInfo, String.Empty, 0, reportErrorWhenReferenced, nonErrorGuessType)
        End Sub

        Friend Sub New(errorInfo As DiagnosticInfo,
                       name As String,
                       Optional reportErrorWhenReferenced As Boolean = False,
                       Optional nonErrorGuessType As NamedTypeSymbol = Nothing)
            Me.New(errorInfo, name, 0, reportErrorWhenReferenced, nonErrorGuessType)
        End Sub

        Friend Sub New(errorInfo As DiagnosticInfo,
                       name As String,
                       arity As Integer,
                       candidateSymbols As ImmutableArray(Of Symbol),
                       resultKind As LookupResultKind,
                       Optional reportErrorWhenReferenced As Boolean = False)
            MyBase.New(arity)
            Debug.Assert(errorInfo Is Nothing OrElse errorInfo.Severity = DiagnosticSeverity.Error)

            _name = name
            _diagnosticInfo = errorInfo
            _reportErrorWhenReferenced = reportErrorWhenReferenced

            If candidateSymbols.Length = 1 AndAlso
               candidateSymbols(0).Kind = SymbolKind.Namespace AndAlso
               DirectCast(candidateSymbols(0), NamespaceSymbol).NamespaceKind = NamespaceKindNamespaceGroup Then
                _candidateSymbols = StaticCast(Of Symbol).From(DirectCast(candidateSymbols(0), NamespaceSymbol).ConstituentNamespaces)
            Else
                _candidateSymbols = candidateSymbols
            End If

#If DEBUG Then
            For Each item In _candidateSymbols
                Debug.Assert(item.Kind <> SymbolKind.Namespace OrElse DirectCast(item, NamespaceSymbol).NamespaceKind <> NamespaceKindNamespaceGroup)
            Next
#End If
            _resultKind = resultKind
        End Sub

        Friend Sub New(errorInfo As DiagnosticInfo,
                       name As String,
                       arity As Integer,
                       Optional reportErrorWhenReferenced As Boolean = False,
                       Optional nonErrorGuessType As NamedTypeSymbol = Nothing)
            MyBase.New(arity)
            Debug.Assert(errorInfo Is Nothing OrElse errorInfo.Severity = DiagnosticSeverity.Error)

            _name = name
            _diagnosticInfo = errorInfo
            _reportErrorWhenReferenced = reportErrorWhenReferenced
            If nonErrorGuessType IsNot Nothing Then
                _candidateSymbols = ImmutableArray.Create(Of Symbol)(nonErrorGuessType)
                _resultKind = LookupResultKind.NotATypeOrNamespace ' TODO: Replace.
            Else
                _candidateSymbols = ImmutableArray(Of Symbol).Empty
                _resultKind = LookupResultKind.Empty
            End If
        End Sub

        Friend Sub New(containingSymbol As NamespaceOrTypeSymbol,
                       name As String,
                       arity As Integer)
            Me.New(DirectCast(Nothing, DiagnosticInfo), name, arity)
            Me._containingSymbol = containingSymbol
        End Sub

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _containingSymbol
            End Get
        End Property

        Public Overrides ReadOnly Property CandidateSymbols As ImmutableArray(Of Symbol)
            Get
                Return _candidateSymbols
            End Get
        End Property

        Friend Overrides ReadOnly Property ResultKind As LookupResultKind
            Get
                Return _resultKind
            End Get
        End Property

        ''' <summary>
        ''' Returns information about the reason that this type is in error.
        ''' </summary>
        Friend Overrides ReadOnly Property ErrorInfo As DiagnosticInfo
            Get
                Return _diagnosticInfo
            End Get
        End Property

        Friend Overrides Function GetUseSiteInfo() As UseSiteInfo(Of AssemblySymbol)
            If _reportErrorWhenReferenced Then
                Return New UseSiteInfo(Of AssemblySymbol)(Me.ErrorInfo)
            End If

            Return Nothing
        End Function

        Public Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Friend Overrides ReadOnly Property MangleName As Boolean
            Get
                Return Arity > 0
            End Get
        End Property

        Protected Overrides Function SpecializedEquals(obj As InstanceErrorTypeSymbol) As Boolean
            Debug.Assert(obj IsNot Me)

            '' Error type symbols representing errors that have been reported compare based on the full
            '' name of the potential type they are representing.  If not reported, subclasses of ErrorTypeSymbol
            '' (such as MissingMetadataTypeSymbol) can define a more refined Equals relation.
            Dim other As ExtendedErrorTypeSymbol = TryCast(obj, ExtendedErrorTypeSymbol)
            If other Is Nothing Then
                Return False
            End If

            Return Object.Equals(Me.ContainingSymbol, other.ContainingSymbol) AndAlso String.Equals(Me.Name, other.Name, StringComparison.Ordinal) AndAlso Me.Arity = other.Arity
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return Hash.Combine(Me.Arity,
                    Hash.Combine(If(Me.ContainingSymbol Is Nothing, 0, Me.ContainingSymbol.GetHashCode()),
                                 If(Me.Name Is Nothing, 0, Me.Name.GetHashCode())))
        End Function
    End Class
End Namespace
