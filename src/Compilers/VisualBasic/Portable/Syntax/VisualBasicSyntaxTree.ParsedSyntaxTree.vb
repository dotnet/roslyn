' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Public Class VisualBasicSyntaxTree

        ''' <summary>
        ''' A SyntaxTree is a tree of nodes that represents an entire file of VB
        ''' code, and is parsed by the parser.
        ''' </summary>
        Partial Private Class ParsedSyntaxTree
            Inherits VisualBasicSyntaxTree

            Private ReadOnly _options As VisualBasicParseOptions
            Private ReadOnly _path As String
            Private ReadOnly _root As VisualBasicSyntaxNode
            Private ReadOnly _hasCompilationUnitRoot As Boolean
            Private ReadOnly _isMyTemplate As Boolean
            Private ReadOnly _encodingOpt As Encoding
            Private ReadOnly _checksumAlgorithm As SourceHashAlgorithm
            Private ReadOnly _diagnosticOptions As ImmutableDictionary(Of String, ReportDiagnostic)

            Private _lazyText As SourceText

            ''' <summary>
            ''' Used to create new tree incrementally.
            ''' </summary>
            Friend Sub New(textOpt As SourceText,
                           encodingOpt As Encoding,
                           checksumAlgorithm As SourceHashAlgorithm,
                           path As String,
                           options As VisualBasicParseOptions,
                           syntaxRoot As VisualBasicSyntaxNode,
                           isMyTemplate As Boolean,
                           diagnosticOptions As ImmutableDictionary(Of String, ReportDiagnostic),
                           Optional cloneRoot As Boolean = True)

                Debug.Assert(syntaxRoot IsNot Nothing)
                Debug.Assert(options IsNot Nothing)
                Debug.Assert(textOpt Is Nothing OrElse textOpt.Encoding Is encodingOpt AndAlso textOpt.ChecksumAlgorithm = checksumAlgorithm)
                Debug.Assert(SourceHashAlgorithms.IsSupportedAlgorithm(checksumAlgorithm))

                _lazyText = textOpt
                _encodingOpt = If(encodingOpt, textOpt?.Encoding)
                _checksumAlgorithm = checksumAlgorithm
                _options = options
                _path = If(path, String.Empty)
                _root = If(cloneRoot, Me.CloneNodeAsRoot(syntaxRoot), syntaxRoot)
                _hasCompilationUnitRoot = (syntaxRoot.Kind = SyntaxKind.CompilationUnit)
                _isMyTemplate = isMyTemplate

                _diagnosticOptions = If(diagnosticOptions, EmptyDiagnosticOptions)
            End Sub

            Public Overrides ReadOnly Property FilePath As String
                Get
                    Return _path
                End Get
            End Property

            Friend Overrides ReadOnly Property IsMyTemplate As Boolean
                Get
                    Return _isMyTemplate
                End Get
            End Property

            Public Overrides Function GetText(Optional cancellationToken As CancellationToken = Nothing) As SourceText
                If _lazyText Is Nothing Then
                    Dim treeText = Me.GetRoot(cancellationToken).GetText(_encodingOpt, _checksumAlgorithm)
                    Interlocked.CompareExchange(_lazyText, treeText, Nothing)
                End If

                Return _lazyText
            End Function

            Public Overrides Function TryGetText(ByRef text As SourceText) As Boolean
                text = _lazyText
                Return text IsNot Nothing
            End Function

            Public Overrides ReadOnly Property Encoding As Encoding
                Get
                    Return _encodingOpt
                End Get
            End Property

            Public Overrides ReadOnly Property Length As Integer
                Get
                    Return _root.FullSpan.Length
                End Get
            End Property

            Public Overrides Function GetRoot(Optional cancellationToken As CancellationToken = Nothing) As VisualBasicSyntaxNode
                Return _root
            End Function

            Public Overrides Function GetRootAsync(Optional cancellationToken As CancellationToken = Nothing) As Task(Of VisualBasicSyntaxNode)
                Return Task.FromResult(_root)
            End Function

            Public Overrides Function TryGetRoot(ByRef root As VisualBasicSyntaxNode) As Boolean
                root = _root
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

            Public Overrides ReadOnly Property DiagnosticOptions As ImmutableDictionary(Of String, ReportDiagnostic)
                Get
                    Return _diagnosticOptions
                End Get
            End Property

            ''' <summary>
            ''' Get a reference to the given node.
            ''' </summary>
            Public Overrides Function GetReference(node As SyntaxNode) As SyntaxReference
                Return New SimpleSyntaxReference(Me, node)
            End Function

            Public Overrides Function WithRootAndOptions(root As SyntaxNode, options As ParseOptions) As SyntaxTree
                If _root Is root AndAlso _options Is options Then
                    Return Me
                End If

                Return New ParsedSyntaxTree(
                    Nothing,
                    _encodingOpt,
                    _checksumAlgorithm,
                    _path,
                    DirectCast(options, VisualBasicParseOptions),
                    DirectCast(root, VisualBasicSyntaxNode),
                    _isMyTemplate,
                    _diagnosticOptions,
                    cloneRoot:=True)
            End Function

            Public Overrides Function WithFilePath(path As String) As SyntaxTree
                If String.Equals(Me._path, path) Then
                    Return Me
                End If

                Return New ParsedSyntaxTree(
                    _lazyText,
                    _encodingOpt,
                    _checksumAlgorithm,
                    path,
                    _options,
                    _root,
                    _isMyTemplate,
                    _diagnosticOptions,
                    cloneRoot:=True)
            End Function

            Public Overrides Function WithDiagnosticOptions(options As ImmutableDictionary(Of String, ReportDiagnostic)) As SyntaxTree
                If options Is Nothing Then
                    options = EmptyDiagnosticOptions
                End If

                If ReferenceEquals(_diagnosticOptions, options) Then
                    Return Me
                End If

                Return New ParsedSyntaxTree(_lazyText,
                                            _encodingOpt,
                                            _checksumAlgorithm,
                                            _path,
                                            _options,
                                            _root,
                                            _isMyTemplate,
                                            options,
                                            cloneRoot:=True)
            End Function
        End Class
    End Class
End Namespace
