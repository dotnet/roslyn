' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.ImplementInterface
    <ExportLanguageService(GetType(IEditorInlineRenameService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicEditorInlineRenameService
        Inherits AbstractEditorInlineRenameService

        <ImportingConstructor>
        Public Sub New(<ImportMany> refactorNotifyServices As IEnumerable(Of IRefactorNotifyService))
            MyBase.New(refactorNotifyServices)
        End Sub
    End Class
End Namespace
