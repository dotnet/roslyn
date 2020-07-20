' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Immutable
Imports System.Reflection
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities
Imports Binder = Microsoft.CodeAnalysis.VisualBasic.Binder

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' A <see cref="MissingNamespaceSymbol"/> is a special kind of <see cref="NamespaceSymbol"/> that represents
    ''' a namespace that couldn't be found.
    ''' </summary>
    Friend Class MissingNamespaceSymbol
        Inherits NamespaceSymbol

        Private ReadOnly _name As String
        Private ReadOnly _containingSymbol As Symbol

        Public Sub New(containingModule As MissingModuleSymbol)
            Debug.Assert(containingModule IsNot Nothing)

            _containingSymbol = containingModule
            _name = String.Empty
        End Sub

        Public Sub New(containingNamespace As NamespaceSymbol, name As String)
            Debug.Assert(containingNamespace IsNot Nothing)
            Debug.Assert(name IsNot Nothing)

            _containingSymbol = containingNamespace
            _name = name
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _containingSymbol
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingAssembly As AssemblySymbol
            Get
                Return _containingSymbol.ContainingAssembly
            End Get
        End Property

        Friend Overrides ReadOnly Property Extent As NamespaceExtent
            Get
                If _containingSymbol.Kind = SymbolKind.NetModule Then
                    Return New NamespaceExtent(DirectCast(_containingSymbol, ModuleSymbol))
                End If

                Return DirectCast(_containingSymbol, NamespaceSymbol).Extent
            End Get
        End Property

        Public Overrides Function GetHashCode() As Integer
            Return Hash.Combine(_containingSymbol.GetHashCode(), _name.GetHashCode())
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            If Me Is obj Then
                Return True
            End If

            Dim other = TryCast(obj, MissingNamespaceSymbol)

            Return other IsNot Nothing AndAlso String.Equals(_name, other._name, StringComparison.Ordinal) AndAlso _containingSymbol.Equals(other._containingSymbol)
        End Function

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

        Public Overrides Function GetTypeMembers() As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Public Overrides Function GetTypeMembers(name As String) As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Public Overrides Function GetTypeMembers(name As String, arity As Integer) As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Public Overrides Function GetModuleMembers() As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Public Overrides Function GetModuleMembers(name As String) As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

        Public Overrides Function GetMembers() As ImmutableArray(Of Symbol)
            Return ImmutableArray(Of Symbol).Empty
        End Function

        Public Overrides Function GetMembers(name As String) As ImmutableArray(Of Symbol)
            Return ImmutableArray(Of Symbol).Empty
        End Function

        Friend Overrides ReadOnly Property DeclaredAccessibilityOfMostAccessibleDescendantType As Accessibility
            Get
                Return Accessibility.Private
            End Get
        End Property

        Friend Overrides Sub AppendProbableExtensionMethods(name As String, methods As ArrayBuilder(Of MethodSymbol))
            Return
        End Sub

        Friend Overrides Sub AddExtensionMethodLookupSymbolsInfo(nameSet As LookupSymbolsInfo,
                                                                  options As LookupOptions,
                                                                  originalBinder As Binder,
                                                                  appendThrough As NamespaceSymbol)
            Return
        End Sub

        Friend Overrides ReadOnly Property TypesToCheckForExtensionMethods As ImmutableArray(Of NamedTypeSymbol)
            Get
                Return ImmutableArray(Of NamedTypeSymbol).Empty
            End Get
        End Property
    End Class

End Namespace
