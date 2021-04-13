' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' A NamespaceExtent represents whether a namespace contains types and sub-namespaces from a particular module,
    ''' assembly, or merged across all modules (source and metadata) in a particular compilation.
    ''' </summary>
    Partial Friend Structure NamespaceExtent
        Private ReadOnly _kind As NamespaceKind
        Private ReadOnly _symbolOrCompilation As Object

        ''' <summary>
        ''' Returns what kind of extent: Module, Assembly, or Compilation.
        ''' </summary>
        Public ReadOnly Property Kind As NamespaceKind
            Get
                Return _kind
            End Get
        End Property

        ''' <summary>
        ''' If the Kind is ExtendKind.Module, returns the module symbol that this namespace
        ''' encompasses. Otherwise throws InvalidOperationException.
        ''' </summary>
        Public ReadOnly Property [Module] As ModuleSymbol
            Get
                If Kind = NamespaceKind.Module Then
                    Return DirectCast(_symbolOrCompilation, ModuleSymbol)
                Else
                    Throw New InvalidOperationException()
                End If
            End Get
        End Property

        ''' <summary>
        ''' If the Kind is ExtendKind.Assembly, returns the assembly symbol that this namespace
        ''' encompasses. Otherwise throws InvalidOperationException.
        ''' </summary>
        Public ReadOnly Property Assembly As AssemblySymbol
            Get
                If Kind = NamespaceKind.Assembly Then
                    Return DirectCast(_symbolOrCompilation, AssemblySymbol)
                Else
                    Throw New InvalidOperationException()
                End If
            End Get
        End Property

        ''' <summary>
        ''' If the Kind is ExtendKind.Compilation, returns the compilation symbol that this namespace
        ''' encompasses. Otherwise throws InvalidOperationException.
        ''' </summary>
        Public ReadOnly Property Compilation As VisualBasicCompilation
            Get
                If Kind = NamespaceKind.Compilation Then
                    Return DirectCast(_symbolOrCompilation, VisualBasicCompilation)
                Else
                    Throw New InvalidOperationException()
                End If
            End Get
        End Property

        Public Overrides Function ToString() As String
            Return String.Format("{0}: {1}", Kind.ToString(), _symbolOrCompilation.ToString())
        End Function

        ''' <summary>
        ''' Create a NamespaceExtent that represents a given ModuleSymbol.
        ''' </summary>
        Friend Sub New([module] As ModuleSymbol)
            _kind = NamespaceKind.Module
            _symbolOrCompilation = [module]
        End Sub

        ''' <summary>
        ''' Create a NamespaceExtent that represents a given AssemblySymbol.
        ''' </summary>
        Friend Sub New(assembly As AssemblySymbol)
            _kind = NamespaceKind.Assembly
            _symbolOrCompilation = assembly
        End Sub

        ''' <summary>
        ''' Create a NamespaceExtent that represents a given Compilation.
        ''' </summary>
        Friend Sub New(compilation As VisualBasicCompilation)
            _kind = NamespaceKind.Compilation
            _symbolOrCompilation = compilation
        End Sub

#If DEBUG Then
        Shared Sub New()

            ' Below is a set of compile time asserts, they break build if violated.

            ' Assert(NamespaceKind.Compilation <> NamespaceKindNamespaceGroup)
            Const assert1 As Integer = 1 Mod (NamespaceKind.Compilation - NamespaceKindNamespaceGroup)

            ' Assert(NamespaceKind.Assembly <> NamespaceKindNamespaceGroup)
            Const assert2 As Integer = 1 Mod (NamespaceKind.Assembly - NamespaceKindNamespaceGroup)

            ' Assert(NamespaceKind.Module <> NamespaceKindNamespaceGroup)
            Const assert3 As Integer = 1 Mod (NamespaceKind.Module - NamespaceKindNamespaceGroup)

            Dim dummy = assert1 + assert2 + assert3
        End Sub
#End If
    End Structure
End Namespace
