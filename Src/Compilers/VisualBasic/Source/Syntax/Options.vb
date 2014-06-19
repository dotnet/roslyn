'-----------------------------------------------------------------------------
' Copyright (c) Microsoft Corporation. All rights reserved.
'
'-----------------------------------------------------------------------------

Imports System.Collections.ObjectModel

Namespace Roslyn.Compilers.VisualBasic
    ''' <summary>
    ''' Encapsulations of all the options that affect the parsing process.
    ''' </summary>
    ''' <remarks></remarks>
    Public Class Options
        Friend ReadOnly _preprocessorSymbols As ImmutableList(Of KeyValuePair(Of String, Object))
        Friend ReadOnly _suppressDocumentationCommentParse As Boolean

        ''' <summary>
        ''' The preprocessor symbols to parse with
        ''' </summary>
        Public ReadOnly Property PreprocessorSymbols As IList(Of KeyValuePair(Of String, Object))
            Get
                Return _preprocessorSymbols
            End Get
        End Property

        Public ReadOnly Property SuppressDocumentationCommentParse As Boolean
            Get
                Return _suppressDocumentationCommentParse
            End Get
        End Property

        Public Sub New(Optional ByVal preprocessorSymbols As IList(Of KeyValuePair(Of String, Object)) = Nothing,
                       Optional ByVal suppressDocumentationCommentParse As Boolean = False)

            If preprocessorSymbols IsNot Nothing Then
                _preprocessorSymbols = preprocessorSymbols.ToImmutableList
            End If
            _suppressDocumentationCommentParse = suppressDocumentationCommentParse
        End Sub
    End Class
End Namespace
