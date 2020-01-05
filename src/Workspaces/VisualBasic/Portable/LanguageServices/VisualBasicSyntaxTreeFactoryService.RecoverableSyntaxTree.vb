' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Text
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
                Implements ICachedObjectOwner

                Private ReadOnly _recoverableRoot As RecoverableSyntaxRoot(Of CompilationUnitSyntax)
                Private ReadOnly _info As SyntaxTreeInfo
                Private ReadOnly _projectCacheService As IProjectCacheHostService
                Private ReadOnly _cacheKey As ProjectId

                Private Property CachedObject As Object Implements ICachedObjectOwner.CachedObject

                Private Sub New(service As AbstractSyntaxTreeFactoryService, cacheKey As ProjectId, root As CompilationUnitSyntax, info As SyntaxTreeInfo)
                    _recoverableRoot = New RecoverableSyntaxRoot(Of CompilationUnitSyntax)(service, root, Me)
                    _info = info
                    _projectCacheService = service.LanguageServices.WorkspaceServices.GetService(Of IProjectCacheHostService)
                    _cacheKey = cacheKey
                End Sub

                Private Sub New(original As RecoverableSyntaxTree, info As SyntaxTreeInfo)
                    _recoverableRoot = original._recoverableRoot.WithSyntaxTree(Me)
                    _info = info
                    _projectCacheService = original._projectCacheService
                    _cacheKey = original._cacheKey
                End Sub

                Friend Shared Function CreateRecoverableTree(service As AbstractSyntaxTreeFactoryService,
                                                             cacheKey As ProjectId,
                                                             filePath As String,
                                                             options As ParseOptions,
                                                             text As ValueSource(Of TextAndVersion),
                                                             encoding As Encoding,
                                                             root As CompilationUnitSyntax,
                                                             diagnosticOptions As ImmutableDictionary(Of String, ReportDiagnostic)) As SyntaxTree
                    Return New RecoverableSyntaxTree(
                        service,
                        cacheKey,
                        root,
                        New SyntaxTreeInfo(
                            filePath,
                            options,
                            text,
                            encoding,
                            root.FullSpan.Length,
                            If(diagnosticOptions, EmptyDiagnosticOptions)))
                End Function

                Public Overrides ReadOnly Property FilePath As String Implements IRecoverableSyntaxTree(Of CompilationUnitSyntax).FilePath
                    Get
                        Return _info.FilePath
                    End Get
                End Property

                Public Overrides ReadOnly Property Options As VisualBasicParseOptions
                    Get
                        Return DirectCast(_info.Options, VisualBasicParseOptions)
                    End Get
                End Property

                Public Overrides ReadOnly Property DiagnosticOptions As ImmutableDictionary(Of String, ReportDiagnostic)
                    Get
                        Return _info.DiagnosticOptions
                    End Get
                End Property

                Public Overrides ReadOnly Property Length As Integer
                    Get
                        Return _info.Length
                    End Get
                End Property

                Public Overrides Function TryGetText(ByRef text As SourceText) As Boolean
                    Return _info.TryGetText(text)
                End Function

                Public Overrides Function GetText(Optional cancellationToken As CancellationToken = Nothing) As SourceText
                    Return _info.TextSource.GetValue(cancellationToken).Text
                End Function

                Public Overrides Function GetTextAsync(Optional cancellationToken As CancellationToken = Nothing) As Task(Of SourceText)
                    Return _info.GetTextAsync(cancellationToken)
                End Function

                Public Overrides ReadOnly Property Encoding As Encoding
                    Get
                        Return _info.Encoding
                    End Get
                End Property

                Public Overrides Function TryGetRoot(ByRef root As VisualBasicSyntaxNode) As Boolean
                    Dim compilationRoot As CompilationUnitSyntax = Nothing
                    Dim status = _recoverableRoot.TryGetValue(compilationRoot)
                    root = compilationRoot
                    CacheRootNode(compilationRoot)
                    Return status
                End Function

                Private Function CacheRootNode(compilationRoot As CompilationUnitSyntax) As CompilationUnitSyntax
                    Return _projectCacheService.CacheObjectIfCachingEnabledForKey(_cacheKey, Me, compilationRoot)
                End Function

                Public Overrides Function GetRoot(Optional cancellationToken As CancellationToken = Nothing) As VisualBasicSyntaxNode
                    Return CacheRootNode(_recoverableRoot.GetValue(cancellationToken))
                End Function

                Public Overrides Async Function GetRootAsync(Optional cancellationToken As CancellationToken = Nothing) As Task(Of VisualBasicSyntaxNode)
                    Return CacheRootNode(Await _recoverableRoot.GetValueAsync(cancellationToken).ConfigureAwait(False))
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
                    Dim oldRoot As VisualBasicSyntaxNode = Nothing
                    If _info.Options Is options AndAlso TryGetRoot(oldRoot) AndAlso root Is oldRoot Then
                        Return Me
                    End If

                    Return Create(DirectCast(root, VisualBasicSyntaxNode), Me.Options, _info.FilePath)
                End Function

                Public Overrides Function WithFilePath(path As String) As SyntaxTree
                    If String.Equals(path, _info.FilePath) Then
                        Return Me
                    End If

                    Return New RecoverableSyntaxTree(Me, _info.WithFilePath(path))
                End Function

                Public Overrides Function WithDiagnosticOptions(options As ImmutableDictionary(Of String, ReportDiagnostic)) As SyntaxTree
                    If options Is Nothing Then
                        options = EmptyDiagnosticOptions
                    End If

                    If ReferenceEquals(options, _info.DiagnosticOptions) Then
                        Return Me
                    End If

                    Return New RecoverableSyntaxTree(Me, _info.WithDiagnosticOptions(options))
                End Function
            End Class
        End Class
    End Class
End Namespace

