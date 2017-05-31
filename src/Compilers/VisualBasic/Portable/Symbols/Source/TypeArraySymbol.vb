' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Linq
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend NotInheritable Class TypeArraySymbol
        Inherits TypeSymbol
        Implements ITypeArraySymbol

        Private _Targets_ As ImmutableArray(Of TypeSymbol)

        Public Sub New(Targets As ImmutableArray(Of TypeSymbol))
            MyBase.New()
            _Targets_ = Targets
        End Sub

        Public Overrides ReadOnly Property IsReferenceType As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property IsValueType As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property TypeKind As TypeKind
            Get
                Return TypeKind.Array
            End Get
        End Property

        Public Overrides ReadOnly Property Kind As SymbolKind
            Get
                Return SymbolKind.ArrayType
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return Nothing
            End Get
        End Property

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

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Accessibility.NotApplicable
            End Get
        End Property

        Friend Overrides ReadOnly Property BaseTypeNoUseSiteDiagnostics As NamedTypeSymbol
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides ReadOnly Property InterfacesNoUseSiteDiagnostics As ImmutableArray(Of NamedTypeSymbol)
            Get
                Return ImmutableArray(Of NamedTypeSymbol).Empty
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides Function GetMembers() As ImmutableArray(Of Symbol)
            Return ImmutableArray(Of Symbol).Empty
        End Function

        Public Overrides Function GetMembers(name As String) As ImmutableArray(Of Symbol)
            Return ImmutableArray(Of Symbol).Empty
        End Function

        Public Overrides Function GetTypeMembers() As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Public Overrides Function GetTypeMembers(name As String) As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Public Overrides Sub Accept(visitor As SymbolVisitor)
            visitor.VisitTypeArray(Me)
        End Sub

        Public Overrides Function Accept(Of TResult)(visitor As SymbolVisitor(Of TResult)) As TResult
            Return visitor.VisitTypeArray(Me)
        End Function

        Public Overrides Sub Accept(visitor As VisualBasicSymbolVisitor)
            visitor.VisitTypeArray(Me)
        End Sub

        Public Overrides Function Accept(Of TResult)(visitor As VisualBasicSymbolVisitor(Of TResult)) As TResult
            Return visitor.VisitTypeArray(Me)
        End Function


        Friend Overrides Function InternalSubstituteTypeParameters(substitution As TypeSubstitution) As TypeWithModifiers
            Return Nothing
            '  Throw New NotImplementedException()
        End Function
        Friend Overrides Function Accept(Of TArgument, TResult)(visitor As VisualBasicSymbolVisitor(Of TArgument, TResult), arg As TArgument) As TResult
            Return visitor.VisitTypeArray(Me, arg)
        End Function
        Friend Overrides Function GetUnificationUseSiteDiagnosticRecursive(owner As Symbol, ByRef checkedTypes As HashSet(Of TypeSymbol)) As DiagnosticInfo
            Throw New NotImplementedException(NameOf(GetUnificationUseSiteDiagnosticRecursive))
        End Function

        Public Function Types() As ImmutableArray(Of ITypeSymbol) Implements ITypeArraySymbol.Types
            Return _Targets_.Cast(Of ITypeSymbol).ToImmutableArrayOrEmpty
        End Function

        Public Overloads Function Equals(other As IArrayTypeSymbol) As Boolean Implements ITypeArraySymbol.Equals
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace