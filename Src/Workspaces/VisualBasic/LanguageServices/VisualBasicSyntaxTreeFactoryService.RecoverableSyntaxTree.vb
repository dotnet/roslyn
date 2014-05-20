' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class VisualBasicSyntaxTreeFactoryServiceFactory

        Partial Friend Class VisualBasicSyntaxTreeFactoryService

            ''' <summary>
            ''' Represents a syntax tree that only has a weak reference to its 
            ''' underlying data.  This way it can be passed around without forcing
            ''' the underlying full tree to stay alive.  Think of it more as a 
            ''' key that can be used to identify a tree rather than the tree itself.
            ''' </summary>
            Friend NotInheritable Class RecoverableSyntaxTree
                Inherits VisualBasicSyntaxTree
                Implements IRecoverableSyntaxTree(Of CompilationUnitSyntax)

                Private ReadOnly _recoverableRoot As RecoverableSyntaxRoot(Of CompilationUnitSyntax)

                Private Sub New(recoverableRoot As RecoverableSyntaxRoot(Of CompilationUnitSyntax))
                    Debug.Assert(recoverableRoot IsNot Nothing)
                    _recoverableRoot = recoverableRoot
                End Sub

                Friend Shared Function CreateRecoverableTree(service As AbstractSyntaxTreeFactoryService, filePath As String, options As ParseOptions, text As ValueSource(Of TextAndVersion), root As CompilationUnitSyntax, reparse As Boolean) As SyntaxTree
                    Dim recoverableRoot = CachedRecoverableSyntaxRoot(Of CompilationUnitSyntax).Create(service, filePath, options, text, root, reparse)
                    Dim recoverableTree = New RecoverableSyntaxTree(recoverableRoot)
                    recoverableRoot.SetContainingTree(recoverableTree)
                    Return recoverableTree
                End Function

                Public Overrides ReadOnly Property FilePath As String
                    Get
                        Return _recoverableRoot.FilePath
                    End Get
                End Property

                Public Overrides ReadOnly Property Options As VisualBasicParseOptions
                    Get
                        Return DirectCast(_recoverableRoot.Options, VisualBasicParseOptions)
                    End Get
                End Property

                Public Overrides ReadOnly Property Length As Integer
                    Get
                        Return _recoverableRoot.Length
                    End Get
                End Property

                Public Overrides Function TryGetText(ByRef text As SourceText) As Boolean
                    Return _recoverableRoot.TryGetText(text)
                End Function

                Public Overrides Function GetText(Optional cancellationToken As CancellationToken = Nothing) As SourceText
                    Return _recoverableRoot.GetText(cancellationToken)
                End Function

                Public Overrides Function GetTextAsync(Optional cancellationToken As CancellationToken = Nothing) As Task(Of SourceText)
                    Return _recoverableRoot.GetTextAsync(cancellationToken)
                End Function

                Public Overrides Function TryGetRoot(ByRef root As VisualBasicSyntaxNode) As Boolean
                    Dim compilationRoot As CompilationUnitSyntax = Nothing
                    Dim status = TryGetRoot(compilationRoot)
                    root = compilationRoot
                    Return status
                End Function

                Public Overloads Function TryGetRoot(ByRef root As CompilationUnitSyntax) As Boolean
                    Return _recoverableRoot.TryGetRoot(root)
                End Function

                Public Overrides Function GetRoot(Optional cancellationToken As CancellationToken = Nothing) As VisualBasicSyntaxNode
                    Return _recoverableRoot.GetRoot(cancellationToken)
                End Function

                Public Overrides Async Function GetRootAsync(Optional cancellationToken As CancellationToken = Nothing) As Task(Of VisualBasicSyntaxNode)
                    Return Await _recoverableRoot.GetRootAsync(cancellationToken).ConfigureAwait(False)
                End Function

                Public Overrides ReadOnly Property HasCompilationUnitRoot As Boolean
                    Get
                        Return True
                    End Get
                End Property

                Public Overrides Function GetReference(node As SyntaxNode) As SyntaxReference
                    If node IsNot Nothing Then
                        If node.Span.Length = 0 Then
                            Return New PathSyntaxReference(Me, node)
                        Else
                            Return New PositionalSyntaxReference(Me, node)
                        End If
                    Else
                        Return New NullSyntaxReference(Me)
                    End If
                End Function

                Private Function IRecoverableSyntaxTree_CloneNodeAsRoot(root As CompilationUnitSyntax) As CompilationUnitSyntax Implements IRecoverableSyntaxTree(Of CompilationUnitSyntax).CloneNodeAsRoot
                    Return CloneNodeAsRoot(root)
                End Function

                Public Overrides Function WithRootAndOptions(root As SyntaxNode, options As ParseOptions) As SyntaxTree
                    Dim oldRoot As CompilationUnitSyntax = Nothing
                    If _recoverableRoot.Options Is options AndAlso TryGetRoot(oldRoot) AndAlso root Is oldRoot Then
                        Return Me
                    End If

                    Return New RecoverableSyntaxTree(_recoverableRoot.WithRootAndOptions(DirectCast(root, CompilationUnitSyntax), options))
                End Function

                Public Overrides Function WithFilePath(path As String) As SyntaxTree
                    If String.Equals(path, _recoverableRoot.FilePath) Then
                        Return Me
                    End If

                    Return New RecoverableSyntaxTree(_recoverableRoot.WithFilePath(path))
                End Function

                Public ReadOnly Property IsReparsed As Boolean
                    Get
                        Return _recoverableRoot.IsReparsed
                    End Get
                End Property
            End Class
        End Class
    End Class
End Namespace

