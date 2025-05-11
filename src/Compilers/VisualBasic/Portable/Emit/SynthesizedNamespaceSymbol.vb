' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.PooledObjects

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Synthesized namespace that contains synthesized types or subnamespaces.
    ''' All its members are stored in a table on <see cref="CommonPEModuleBuilder"/>.
    ''' </summary>
    Friend NotInheritable Class SynthesizedNamespaceSymbol
        Inherits NamespaceSymbol

        Private ReadOnly _containingNamespace As NamespaceSymbol

        Public Overrides ReadOnly Property Name As String

        Sub New(containingNamespace As NamespaceSymbol, name As String)
            _containingNamespace = containingNamespace
            Me.Name = name
        End Sub

        Public Overrides ReadOnly Property ContainingAssembly As AssemblySymbol
            Get
                Return _containingNamespace.ContainingAssembly
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _containingNamespace
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

        Friend Overrides ReadOnly Property Extent As NamespaceExtent
            Get
                Return _containingNamespace.Extent
            End Get
        End Property

        Friend Overrides ReadOnly Property DeclaredAccessibilityOfMostAccessibleDescendantType As Accessibility
            Get
                Return Accessibility.Friend
            End Get
        End Property

        Friend Overrides ReadOnly Property TypesToCheckForExtensionMethods As ImmutableArray(Of NamedTypeSymbol)
            Get
                Return ImmutableArray(Of NamedTypeSymbol).Empty
            End Get
        End Property

        Friend Overrides Sub AppendProbableExtensionMethods(name As String, methods As ArrayBuilder(Of MethodSymbol))
        End Sub

        Friend Overrides Sub AddExtensionMethodLookupSymbolsInfo(nameSet As LookupSymbolsInfo, options As LookupOptions, originalBinder As Binder, appendThrough As NamespaceSymbol)
        End Sub

        Public Overrides Function GetModuleMembers() As ImmutableArray(Of NamedTypeSymbol)
            Return ImmutableArray(Of NamedTypeSymbol).Empty
        End Function

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
    End Class
End Namespace
