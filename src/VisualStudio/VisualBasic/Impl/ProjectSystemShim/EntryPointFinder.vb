' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim
    Friend Class EntryPointFinder

        <Obsolete("FindEntryPoints on a INamespaceSymbol is deprecated, please pass in the Compilation instead.")>
        Public Shared Function FindEntryPoints(symbol As INamespaceSymbol, findFormsOnly As Boolean) As IEnumerable(Of INamedTypeSymbol)
            If findFormsOnly Then
                Return FindEntryPoints(symbol)
            Else
                Return FindEntryPoints(symbol.ContainingCompilation)
            End If
        End Function

        Public Shared Function FindEntryPoints(compilation As Compilation, findFormsOnly As Boolean) As IEnumerable(Of INamedTypeSymbol)
            If findFormsOnly Then
                Return FindEntryPoints(compilation.SourceModule.GlobalNamespace)
            Else
                Return FindEntryPoints(compilation)
            End If
        End Function

        Private Shared Function FindEntryPoints(compilation As Compilation) As IEnumerable(Of INamedTypeSymbol)
            Return compilation.GetEntryPointCandidates(CancellationToken.None).
                    SelectAsArray(Function(x) TryCast(x.ContainingSymbol, INamedTypeSymbol)).
                    WhereNotNull()
        End Function

        Private Shared Function FindEntryPoints(symbol As INamespaceSymbol) As IEnumerable(Of INamedTypeSymbol)
            Dim visitor = New WinformsEntryPointFinder()
            ' Attempt to only search source symbols
            ' Some callers will give a symbol that is not part of a compilation
            If symbol.ContainingCompilation IsNot Nothing Then
                symbol = symbol.ContainingCompilation.SourceModule.GlobalNamespace
            End If

            visitor.Visit(symbol)
            Return visitor.EntryPoints
        End Function

        Private Class WinformsEntryPointFinder
            Inherits SymbolVisitor

            Public ReadOnly EntryPoints As New HashSet(Of INamedTypeSymbol)

            Public Overrides Sub VisitNamespace(symbol As INamespaceSymbol)
                For Each member In symbol.GetMembers()
                    member.Accept(Me)
                Next
            End Sub

            Public Overrides Sub VisitNamedType(symbol As INamedTypeSymbol)
                ' It's a form if it Inherits System.Windows.Forms.Form.
                Dim baseType = symbol.BaseType
                While baseType IsNot Nothing
                    If baseType.ToDisplayString() = "System.Windows.Forms.Form" Then
                        EntryPoints.Add(symbol)
                        Exit While
                    End If

                    baseType = baseType.BaseType
                End While

                For Each member In symbol.GetMembers()
                    member.Accept(Me)
                Next
            End Sub
        End Class

    End Class
End Namespace
