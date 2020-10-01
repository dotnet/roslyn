' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Editor.Implementation.SplitComment
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.SplitComment
    <ExportLanguageService(GetType(ISplitCommentService), LanguageNames.VisualBasic), [Shared]>
    Partial Friend Class VisualBasicSplitCommentService
        Implements ISplitCommentService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public ReadOnly Property CommentStart As String Implements ISplitCommentService.CommentStart
            Get
                Return "'"
            End Get
        End Property
    End Class
End Namespace
