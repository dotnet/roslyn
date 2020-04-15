' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Reflection.Metadata

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE

    Friend NotInheritable Class PEGlobalNamespaceSymbol
        Inherits PENamespaceSymbol

        ''' <summary>
        ''' The module containing the namespace.
        ''' </summary>
        ''' <remarks></remarks>
        Private ReadOnly _moduleSymbol As PEModuleSymbol

        Friend Sub New(moduleSymbol As PEModuleSymbol)
            Debug.Assert(moduleSymbol IsNot Nothing)
            _moduleSymbol = moduleSymbol
        End Sub

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _moduleSymbol
            End Get
        End Property

        Friend Overrides ReadOnly Property ContainingPEModule As PEModuleSymbol
            Get
                Return _moduleSymbol
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return String.Empty
            End Get
        End Property

        Public Overrides ReadOnly Property IsGlobalNamespace As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingAssembly As AssemblySymbol
            Get
                Return _moduleSymbol.ContainingAssembly
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingModule As ModuleSymbol
            Get
                Return _moduleSymbol
            End Get
        End Property

        Protected Overrides Sub EnsureAllMembersLoaded()
            If m_lazyTypes Is Nothing OrElse m_lazyMembers Is Nothing Then
                Dim groups As IEnumerable(Of IGrouping(Of String, TypeDefinitionHandle))

                Try
                    groups = _moduleSymbol.Module.GroupTypesByNamespaceOrThrow(IdentifierComparison.Comparer)
                Catch mrEx As BadImageFormatException
                    groups = SpecializedCollections.EmptyEnumerable(Of IGrouping(Of String, TypeDefinitionHandle))()
                End Try

                LoadAllMembers(groups)
            End If
        End Sub

        ''' <remarks>
        ''' This is for perf, not for correctness.
        ''' </remarks>
        Friend Overrides ReadOnly Property DeclaringCompilation As VisualBasicCompilation
            Get
                Return Nothing
            End Get
        End Property
    End Class

End Namespace
