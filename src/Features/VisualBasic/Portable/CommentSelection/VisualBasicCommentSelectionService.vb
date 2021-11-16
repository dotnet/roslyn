' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CommentSelection
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic.CommentSelection
    <ExportLanguageService(GetType(ICommentSelectionService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicCommentSelectionService
        Inherits AbstractCommentSelectionService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property SingleLineCommentString As String = "'"

        Public Overrides ReadOnly Property SupportsBlockComment As Boolean

        Public Overrides ReadOnly Property BlockCommentEndString As String
            Get
                Throw New NotSupportedException()
            End Get
        End Property

        Public Overrides ReadOnly Property BlockCommentStartString As String
            Get
                Throw New NotSupportedException()
            End Get
        End Property
    End Class
End Namespace
