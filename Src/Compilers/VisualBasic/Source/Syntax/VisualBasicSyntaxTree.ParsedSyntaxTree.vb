' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.ObjectModel
Imports System.IO
Imports System.Runtime.CompilerServices
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Instrumentation
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Class VisualBasicSyntaxTree

        ''' <summary>
        ''' A SyntaxTree is a tree of nodes that represents an entire file of VB
        ''' code, and is parsed by the parser.
        ''' </summary>
        Partial Private Class ParsedSyntaxTree
            Inherits VisualBasicSyntaxTree

            Private ReadOnly _path As String
            Private _text As SourceText
            Private ReadOnly _options As VisualBasicParseOptions
            Private ReadOnly _reparseCnt As Integer
            Private ReadOnly _hasCompilationUnitRoot As Boolean

            ' This root is attached to this syntaxTree, so that you can navigate to the syntax tree
            ' via the root.
            Private ReadOnly _syntaxRoot As VisualBasicSyntaxNode

            ''' <summary>
            ''' Used to create new tree incrementally.
            ''' </summary>
            Friend Sub New(sourceText As SourceText,
                           path As String,
                           syntaxRoot As VisualBasicSyntaxNode,
                           options As VisualBasicParseOptions,
                           reparseCnt As Integer,
                           Optional cloneRoot As Boolean = True)

                Debug.Assert(syntaxRoot IsNot Nothing)
                Debug.Assert(options IsNot Nothing)
                Debug.Assert(path IsNot Nothing)

                _syntaxRoot = If(cloneRoot, Me.CloneNodeAsRoot(syntaxRoot), syntaxRoot)
                _path = path
                _text = sourceText
                _options = options
                _reparseCnt = reparseCnt
                _hasCompilationUnitRoot = (syntaxRoot.Kind = SyntaxKind.CompilationUnit)
            End Sub

            Friend ReadOnly Property ReparseCount As Integer
                Get
                    Return Me._reparseCnt
                End Get
            End Property

            Public Overrides ReadOnly Property FilePath As String
                Get
                    Return _path
                End Get
            End Property

            Public Overrides Function GetText(Optional cancellationToken As CancellationToken = Nothing) As SourceText
                If _text Is Nothing Then
                    Using Logger.LogBlock(FunctionId.VisualBasic_SyntaxTree_GetText, message:=Me.FilePath, cancellationToken:=cancellationToken)
                        Dim treeText = Me.GetRoot(cancellationToken).GetText()
                        Interlocked.CompareExchange(_text, treeText, Nothing)
                    End Using
                End If

                Return _text
            End Function

            Public Overrides Function TryGetText(ByRef text As SourceText) As Boolean
                text = _text
                Return text IsNot Nothing
            End Function

            Public Overrides ReadOnly Property Length As Integer
                Get
                    Return _syntaxRoot.FullSpan.Length
                End Get
            End Property

            Public Overrides Function GetRoot(Optional cancellationToken As CancellationToken = Nothing) As VisualBasicSyntaxNode
                Return _syntaxRoot
            End Function

            Public Overrides Function GetRootAsync(Optional cancellationToken As CancellationToken = Nothing) As Task(Of VisualBasicSyntaxNode)
                Return Task.FromResult(_syntaxRoot)
            End Function

            Public Overrides Function TryGetRoot(ByRef root As VisualBasicSyntaxNode) As Boolean
                root = _syntaxRoot
                Return True
            End Function

            Public Overrides ReadOnly Property HasCompilationUnitRoot As Boolean
                Get
                    Return _hasCompilationUnitRoot
                End Get
            End Property

            Public Overrides ReadOnly Property Options As VisualBasicParseOptions
                Get
                    Return _options
                End Get
            End Property

            ''' <summary>
            ''' Get a reference to the given node.
            ''' </summary>
            Public Overrides Function GetReference(node As SyntaxNode) As SyntaxReference
                Return New SimpleSyntaxReference(Me, node)
            End Function

            ''' <summary>
            ''' Returns a <see cref="System.String" /> that represents the source code of this parsed tree.
            ''' </summary>
            Public Overrides Function ToString() As String
                Return Me.GetText().ToString()
            End Function
        End Class

        Private Class MyTemplateSyntaxTree
            Inherits ParsedSyntaxTree

            Friend Sub New(sourceText As SourceText,
                           path As String,
                           syntaxRoot As CompilationUnitSyntax,
                           options As VisualBasicParseOptions,
                           reparseCnt As Integer)
                MyBase.New(sourceText, path, syntaxRoot, options, reparseCnt)
            End Sub

            Friend Overrides ReadOnly Property IsMyTemplate As Boolean
                Get
                    Return True
                End Get
            End Property
        End Class
    End Class
End Namespace