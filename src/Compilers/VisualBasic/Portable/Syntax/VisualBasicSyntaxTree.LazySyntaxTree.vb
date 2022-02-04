' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Public Class VisualBasicSyntaxTree

        Private NotInheritable Class LazySyntaxTree
            Inherits VisualBasicSyntaxTree

            Private ReadOnly _text As SourceText
            Private ReadOnly _options As VisualBasicParseOptions
            Private ReadOnly _path As String
            Private ReadOnly _diagnosticOptions As ImmutableDictionary(Of String, ReportDiagnostic)
            Private _lazyRoot As VisualBasicSyntaxNode

            ''' <summary>
            ''' Used to create new tree incrementally.
            ''' </summary>
            Friend Sub New(text As SourceText,
                           options As VisualBasicParseOptions,
                           path As String,
                           diagnosticOptions As ImmutableDictionary(Of String, ReportDiagnostic))

                Debug.Assert(options IsNot Nothing)

                _text = text
                _options = options
                _path = If(path, String.Empty)
                _diagnosticOptions = If(diagnosticOptions, EmptyDiagnosticOptions)
            End Sub

            Public Overrides ReadOnly Property FilePath As String
                Get
                    Return _path
                End Get
            End Property

            Friend Overrides ReadOnly Property IsMyTemplate As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides Function GetText(Optional cancellationToken As CancellationToken = Nothing) As SourceText
                Return _text
            End Function

            Public Overrides Function TryGetText(ByRef text As SourceText) As Boolean
                text = _text
                Return True
            End Function

            Public Overrides ReadOnly Property Encoding As Encoding
                Get
                    Return _text.Encoding
                End Get
            End Property

            Public Overrides ReadOnly Property Length As Integer
                Get
                    Return _text.Length
                End Get
            End Property

            Public Overrides Function GetRoot(Optional cancellationToken As CancellationToken = Nothing) As VisualBasicSyntaxNode
                If _lazyRoot Is Nothing Then
                    ' Parse the syntax tree
                    Dim tree = SyntaxFactory.ParseSyntaxTree(_text, _options, _path, cancellationToken)
                    Dim root = CloneNodeAsRoot(CType(tree.GetRoot(cancellationToken), VisualBasicSyntaxNode))

                    Interlocked.CompareExchange(_lazyRoot, root, Nothing)
                End If

                Return _lazyRoot
            End Function

            Public Overrides Function TryGetRoot(ByRef root As VisualBasicSyntaxNode) As Boolean
                root = _lazyRoot
                Return root IsNot Nothing
            End Function

            Public Overrides ReadOnly Property HasCompilationUnitRoot As Boolean
                Get
                    Return True
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
                If _lazyRoot Is root AndAlso _options Is options Then
                    Return Me
                End If

                Return New ParsedSyntaxTree(
                    Nothing,
                    _text.Encoding,
                    _text.ChecksumAlgorithm,
                    _path,
                    DirectCast(options, VisualBasicParseOptions),
                    DirectCast(root, VisualBasicSyntaxNode),
                    isMyTemplate:=False,
                    _diagnosticOptions,
                    cloneRoot:=True)
            End Function

            Public Overrides Function WithFilePath(path As String) As SyntaxTree
                If String.Equals(Me._path, path) Then
                    Return Me
                End If

                Dim root As VisualBasicSyntaxNode = Nothing
                If TryGetRoot(root) Then
                    Return New ParsedSyntaxTree(
                        _text,
                        _text.Encoding,
                        _text.ChecksumAlgorithm,
                        path,
                        _options,
                        root,
                        isMyTemplate:=False,
                        _diagnosticOptions,
                        cloneRoot:=True)
                Else
                    Return New LazySyntaxTree(_text, _options, path, _diagnosticOptions)
                End If
            End Function

            Public Overrides Function WithDiagnosticOptions(options As ImmutableDictionary(Of String, ReportDiagnostic)) As SyntaxTree
                If options Is Nothing Then
                    options = EmptyDiagnosticOptions
                End If

                If ReferenceEquals(_diagnosticOptions, options) Then
                    Return Me
                End If

                Dim root As VisualBasicSyntaxNode = Nothing
                If TryGetRoot(root) Then
                    Return New ParsedSyntaxTree(
                        _text,
                        _text.Encoding,
                        _text.ChecksumAlgorithm,
                        _path,
                        _options,
                        root,
                        isMyTemplate:=False,
                        options,
                        cloneRoot:=True)
                Else
                    Return New LazySyntaxTree(_text, _options, _path, options)
                End If
            End Function
        End Class
    End Class
End Namespace
