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
    ''' A SourceFileBinder provides the context associated with a give source file, not including the
    ''' Imports statements (which have their own binders). It primarily provides the services of getting
    ''' locations of node, since it holds onto a SyntaxTree.
    ''' </summary>
    Friend Class SourceFileBinder
        Inherits Binder

        ' The source file this binder is associated with
        Private ReadOnly _sourceFile As SourceFile

        Public Sub New(containingBinder As Binder, sourceFile As SourceFile, tree As SyntaxTree)
            MyBase.New(containingBinder, tree)
            Debug.Assert(sourceFile IsNot Nothing)
            _sourceFile = sourceFile
        End Sub

        Public Overrides Function GetSyntaxReference(node As VisualBasicSyntaxNode) As SyntaxReference
            Return SyntaxTree.GetReference(node)
        End Function

        Public Overrides ReadOnly Property OptionStrict As OptionStrict
            Get
                ' If the source file had an option strict declaration in it, use that. Otherwise
                ' defer to the global options.
                If _sourceFile.OptionStrict.HasValue Then
                    Return If(_sourceFile.OptionStrict.Value, OptionStrict.On, OptionStrict.Off)
                Else
                    Return m_containingBinder.OptionStrict
                End If
            End Get
        End Property

        Public Overrides ReadOnly Property OptionInfer As Boolean
            Get
                ' If the source file had an option infer declaration in it, use that. Otherwise
                ' defer to the global options.
                If _sourceFile.OptionInfer.HasValue Then
                    Return _sourceFile.OptionInfer.Value
                Else
                    Return m_containingBinder.OptionInfer
                End If
            End Get
        End Property

        Public Overrides ReadOnly Property OptionExplicit As Boolean
            Get
                ' If the source file had an option explicit declaration in it, use that. Otherwise
                ' defer to the global options.
                If _sourceFile.OptionExplicit.HasValue Then
                    Return _sourceFile.OptionExplicit.Value
                Else
                    Return m_containingBinder.OptionExplicit
                End If
            End Get
        End Property

        Public Overrides ReadOnly Property OptionCompareText As Boolean
            Get
                ' If the source file had an option compare declaration in it, use that. Otherwise
                ' defer to the global options.
                If _sourceFile.OptionCompareText.HasValue Then
                    Return _sourceFile.OptionCompareText.Value
                Else
                    Return m_containingBinder.OptionCompareText
                End If
            End Get
        End Property

        Public Overrides ReadOnly Property QuickAttributeChecker As QuickAttributeChecker
            Get
                Return _sourceFile.QuickAttributeChecker
            End Get
        End Property
    End Class

End Namespace
