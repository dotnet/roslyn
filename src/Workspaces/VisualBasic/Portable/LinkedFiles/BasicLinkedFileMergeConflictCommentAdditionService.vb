' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic
    <ExportLanguageService(GetType(ILinkedFileMergeConflictCommentAdditionService), LanguageNames.VisualBasic), [Shared]>
    Friend Class BasicLinkedFileMergeConflictCommentAdditionService
        Inherits AbstractLinkedFileMergeConflictCommentAdditionService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub
    End Class
End Namespace
