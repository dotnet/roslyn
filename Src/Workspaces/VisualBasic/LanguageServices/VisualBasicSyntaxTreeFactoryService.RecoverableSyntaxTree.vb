' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.IO
Imports System.Runtime.CompilerServices
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class VisualBasicSyntaxTreeFactoryServiceFactory

        Partial Private Class VisualBasicSyntaxTreeFactoryService

            ''' <summary>
            ''' Represents a syntax tree that only has a weak reference to its 
            ''' underlying data.  This way it can be passed around without forcing
            ''' the underlying full tree to stay alive.  Think of it more as a 
            ''' key that can be used to identify a tree rather than the tree itself.
            ''' </summary>
            Friend MustInherit Class RecoverableSyntaxTree
                Inherits VisualBasicSyntaxTree
                Implements IRecoverableSyntaxTree(Of CompilationUnitSyntax)

                Private ReadOnly _recoverableRoot As AbstractRecoverableSyntaxRoot(Of CompilationUnitSyntax)

                Public Sub New(recoverableRoot As AbstractRecoverableSyntaxRoot(Of CompilationUnitSyntax))
                    _recoverableRoot = recoverableRoot
                    _recoverableRoot.SetContainingTree(Me)
                End Sub

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

                Public Overrides Function TryGetText(ByRef text As Microsoft.CodeAnalysis.Text.SourceText) As Boolean
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
            End Class

            Friend Class SerializedSyntaxTree
                Inherits RecoverableSyntaxTree

                Public Sub New(service As VisualBasicSyntaxTreeFactoryService, filePath As String, options As ParseOptions, text As ValueSource(Of TextAndVersion), root As CompilationUnitSyntax)
                    MyBase.New(New SerializedSyntaxRoot(Of CompilationUnitSyntax)(service, filePath, options, text, root))
                End Sub
            End Class

            Friend Class ReparsedSyntaxTree
                Inherits RecoverableSyntaxTree

                Public Sub New(service As VisualBasicSyntaxTreeFactoryService, filePath As String, options As ParseOptions, text As ValueSource(Of TextAndVersion), root As CompilationUnitSyntax)
                    MyBase.New(New ReparsedSyntaxRoot(Of CompilationUnitSyntax)(service, filePath, options, text, root))
                End Sub
            End Class
        End Class
    End Class
End Namespace

