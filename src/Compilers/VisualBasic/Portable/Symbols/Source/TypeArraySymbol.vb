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
                Throw New NotImplementedException()
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Throw New NotImplementedException()
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Throw New NotImplementedException()
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Throw New NotImplementedException()
            End Get
        End Property

        Friend Overrides ReadOnly Property BaseTypeNoUseSiteDiagnostics As NamedTypeSymbol
            Get
                Throw New NotImplementedException()
            End Get
        End Property

        Friend Overrides ReadOnly Property InterfacesNoUseSiteDiagnostics As ImmutableArray(Of NamedTypeSymbol)
            Get
                Throw New NotImplementedException()
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Throw New NotImplementedException()
            End Get
        End Property

        Public Overrides Sub Accept(visitor As SymbolVisitor)
            Throw New NotImplementedException()
        End Sub

        Public Overrides Sub Accept(visitor As VisualBasicSymbolVisitor)
            Throw New NotImplementedException()
        End Sub

        Public Overrides Function GetMembers() As ImmutableArray(Of Symbol)
            Throw New NotImplementedException()
        End Function

        Public Overrides Function GetMembers(name As String) As ImmutableArray(Of Symbol)
            Throw New NotImplementedException()
        End Function

        Public Overrides Function GetTypeMembers() As ImmutableArray(Of NamedTypeSymbol)
            Throw New NotImplementedException()
        End Function

        Public Overrides Function GetTypeMembers(name As String) As ImmutableArray(Of NamedTypeSymbol)
            Throw New NotImplementedException()
        End Function

        Public Overrides Function Accept(Of TResult)(visitor As SymbolVisitor(Of TResult)) As TResult
            Throw New NotImplementedException()
        End Function

        Public Overrides Function Accept(Of TResult)(visitor As VisualBasicSymbolVisitor(Of TResult)) As TResult
            Throw New NotImplementedException()
        End Function

        Friend Overrides Function InternalSubstituteTypeParameters(substitution As TypeSubstitution) As TypeWithModifiers
            Throw New NotImplementedException()
        End Function

        Friend Overrides Function GetUnificationUseSiteDiagnosticRecursive(owner As Symbol, ByRef checkedTypes As HashSet(Of TypeSymbol)) As DiagnosticInfo
            Throw New NotImplementedException()
        End Function

        Friend Overrides Function Accept(Of TArgument, TResult)(visitor As VisualBasicSymbolVisitor(Of TArgument, TResult), arg As TArgument) As TResult
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace