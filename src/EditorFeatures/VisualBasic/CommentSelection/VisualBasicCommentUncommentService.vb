' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Editor.Implementation.CommentSelection
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.CommentSelection
    <ExportLanguageService(GetType(ICommentUncommentService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicCommentUncommentService
        Inherits AbstractCommentUncommentService

        Public Overrides ReadOnly Property SingleLineCommentString As String
            Get
                Return "'"
            End Get
        End Property

        Public Overrides ReadOnly Property SupportsBlockComment As Boolean
            Get
                Return False
            End Get
        End Property

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
