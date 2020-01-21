' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CommentSelection
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic.CommentSelection
    <ExportLanguageService(GetType(ICommentSelectionService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicCommentSelectionService
        Inherits AbstractCommentSelectionService

        <ImportingConstructor>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property SingleLineCommentString As String = "'"

        Public Overrides ReadOnly Property SupportsBlockComment As Boolean = False

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
