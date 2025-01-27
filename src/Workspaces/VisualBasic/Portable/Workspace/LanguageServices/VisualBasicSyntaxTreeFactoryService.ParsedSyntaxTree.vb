' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class VisualBasicSyntaxTreeFactoryService
        ''' <summary>
        ''' Parsed <see cref="VisualBasicSyntaxTree"/> that creates <see cref="SourceText"/> with given encoding And checksum algorithm.
        ''' </summary>
        Partial Private NotInheritable Class ParsedSyntaxTree
            Inherits VisualBasicSyntaxTree

            Private ReadOnly _root As VisualBasicSyntaxNode
            Private ReadOnly _checksumAlgorithm As SourceHashAlgorithm

            Public Overrides ReadOnly Property Encoding As Encoding
            Public Overrides ReadOnly Property Options As VisualBasicParseOptions
            Public Overrides ReadOnly Property FilePath As String

            Private _lazyText As SourceText

            Public Sub New(
                    lazyText As SourceText,
                    root As VisualBasicSyntaxNode,
                    options As VisualBasicParseOptions,
                    filePath As String,
                    encoding As Encoding,
                    checksumAlgorithm As SourceHashAlgorithm)
                _lazyText = lazyText
                _root = CloneNodeAsRoot(root)
                _checksumAlgorithm = checksumAlgorithm
                Me.Encoding = encoding
                Me.Options = options
                Me.FilePath = filePath
            End Sub

            Public Overrides Function GetText(Optional cancellationToken As CancellationToken = Nothing) As SourceText
                If _lazyText Is Nothing Then
                    Interlocked.CompareExchange(_lazyText, GetRoot(cancellationToken).GetText(Encoding, _checksumAlgorithm), Nothing)
                End If

                Return _lazyText
            End Function

            Public Overrides Function TryGetText(<Out> ByRef text As SourceText) As Boolean
                text = _lazyText
                Return text IsNot Nothing
            End Function

            Public Overrides ReadOnly Property Length As Integer
                Get
                    Return _root.FullSpan.Length
                End Get
            End Property

            Public Overrides ReadOnly Property HasCompilationUnitRoot As Boolean
                Get
                    Return True
                End Get
            End Property

            Public Overrides Function GetRoot(Optional cancellationToken As CancellationToken = Nothing) As VisualBasicSyntaxNode
                Return _root
            End Function

            Public Overrides Function TryGetRoot(<Out> ByRef root As VisualBasicSyntaxNode) As Boolean
                root = _root
                Return True
            End Function

            Public Overrides Function WithRootAndOptions(root As SyntaxNode, options As ParseOptions) As SyntaxTree
                Return If(ReferenceEquals(root, _root) AndAlso options = Me.Options,
                    Me,
                    New ParsedSyntaxTree(
                        If(ReferenceEquals(root, _root), _lazyText, Nothing),
                        DirectCast(root, VisualBasicSyntaxNode),
                        DirectCast(options, VisualBasicParseOptions),
                        FilePath,
                        Encoding,
                        _checksumAlgorithm))
            End Function

            Public Overrides Function WithFilePath(path As String) As SyntaxTree
                Return If(path = FilePath,
                    Me,
                    New ParsedSyntaxTree(_lazyText, _root, Options, path, Encoding, _checksumAlgorithm))
            End Function

            Public Overrides Function GetReference(node As SyntaxNode) As SyntaxReference
                Return New NodeSyntaxReference(node)
            End Function
        End Class
    End Class
End Namespace
