' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Immutable
Imports System.Reflection
Imports System.Threading
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

        Private ReadOnly m_Name As String
        Private ReadOnly m_ContainingSymbol As Symbol

        Public Sub New(containingModule As MissingModuleSymbol)
            Debug.Assert(containingModule IsNot Nothing)

            m_ContainingSymbol = containingModule
            m_Name = String.Empty
        End Sub

        Public Sub New(containingNamespace As NamespaceSymbol, name As String)
            Debug.Assert(containingNamespace IsNot Nothing)
            Debug.Assert(name IsNot Nothing)

            m_ContainingSymbol = containingNamespace
            m_Name = name
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return m_Name
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return m_ContainingSymbol
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingAssembly As AssemblySymbol
            Get
                Return ContainingModule.ContainingAssembly
            End Get
        End Property

        Friend Overrides ReadOnly Property Extent As NamespaceExtent
            Get
                Return New NamespaceExtent(ContainingModule)
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingModule As ModuleSymbol
            Get
                If m_ContainingSymbol.Kind = SymbolKind.NetModule Then
                    Return DirectCast(m_ContainingSymbol, ModuleSymbol)
                End If

                Return m_ContainingSymbol.ContainingModule
            End Get
        End Property

        Public Overrides Function GetHashCode() As Integer
            Return Hash.Combine(m_ContainingSymbol.GetHashCode(), m_Name.GetHashCode())
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            If Me Is obj Then
                Return True
            End If

            Dim other = TryCast(obj, MissingNamespaceSymbol)

            Return other IsNot Nothing AndAlso String.Equals(m_Name, other.m_Name, StringComparison.Ordinal) AndAlso m_ContainingSymbol.Equals(other.m_ContainingSymbol)
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