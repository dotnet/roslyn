' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Class VBSyntaxTree
        Friend Class DummySyntaxTree
            Inherits VBSyntaxTree

            Private ReadOnly _node As CompilationUnitSyntax

            Public Sub New()
                _node = Me.CloneNodeAsRoot(SyntaxFactory.ParseCompilationUnit(String.Empty))
            End Sub

            Public Overrides Function ToString() As String
                Return String.Empty
            End Function

            Public Overrides Function GetText(Optional cancellationToken As CancellationToken = Nothing) As SourceText
                Return SourceText.From(String.Empty)
            End Function

            Public Overrides Function TryGetText(ByRef text As SourceText) As Boolean
                text = SourceText.From(String.Empty)
                Return True
            End Function

            Public Overrides ReadOnly Property Length As Integer
                Get
                    Return 0
                End Get
            End Property

            Public Overrides ReadOnly Property Options As VBParseOptions
                Get
                    Return VBParseOptions.Default
                End Get
            End Property

            Public Overrides ReadOnly Property FilePath As String
                Get
                    Return String.Empty
                End Get
            End Property

            Public Overrides Function GetReference(node As SyntaxNode) As SyntaxReference
                Return New SimpleSyntaxReference(Me, node)
            End Function

            Public Overrides Function WithChangedText(newText As SourceText) As SyntaxTree
                Throw New InvalidOperationException()
            End Function

            Public Overrides Function GetRoot(Optional cancellationToken As CancellationToken = Nothing) As VBSyntaxNode
                Return _node
            End Function

            Public Overrides Function TryGetRoot(ByRef root As VBSyntaxNode) As Boolean
                root = _node
                Return True
            End Function

            Public Overrides ReadOnly Property HasCompilationUnitRoot As Boolean
                Get
                    Return True
                End Get
            End Property

            Public Overrides Function WithRootAndOptions(root As SyntaxNode, options As ParseOptions) As SyntaxTree
                Return SyntaxFactory.SyntaxTree(root, options:=options, path:=FilePath, encoding:=Nothing)
            End Function

            Public Overrides Function WithFilePath(path As String) As SyntaxTree
                Return SyntaxFactory.SyntaxTree(_node, options:=Me.Options, path:=path, encoding:=Nothing)
            End Function
        End Class
    End Class
End Namespace