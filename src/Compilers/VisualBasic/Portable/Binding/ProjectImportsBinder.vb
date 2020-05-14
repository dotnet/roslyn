' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.RuntimeMembers
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' A ProjectImportsBinder provides the equivalent of a SourceFileBinder, but for the project-level
    ''' imports, which don't live in any file.
    ''' It primarily provides the services of getting locations of nodes, since it holds onto a SyntaxTree, although
    ''' that tree isn't technically a source tree.
    ''' </summary>
    Friend Class ProjectImportsBinder
        Inherits Binder

        ' The syntax tree this binder is associated with
        Private ReadOnly _tree As SyntaxTree

        Public Sub New(containingBinder As Binder, tree As SyntaxTree)
            MyBase.New(containingBinder)
            _tree = tree
        End Sub

        Public Overrides Function GetSyntaxReference(node As VisualBasicSyntaxNode) As SyntaxReference
            Return _tree.GetReference(node)
        End Function

        Friend Overrides ReadOnly Property SuppressObsoleteDiagnostics As Boolean
            Get
                ' Obsolete diagnostics is not reported for project level imports. This is VS2013 behavior.
                ' This also ensures that we don't get into circularity while binding imports because we are 
                ' remapping diagnostics in SourceModuleSymbol.BindImports
                Return True
            End Get
        End Property
    End Class

End Namespace
