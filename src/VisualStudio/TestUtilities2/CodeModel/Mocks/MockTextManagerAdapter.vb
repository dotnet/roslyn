' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.Mocks
    Friend NotInheritable Class MockTextManagerAdapter
        Implements ITextManagerAdapter

        Private ReadOnly _editorOptionsFactoryService As IEditorOptionsFactoryService

        Public Sub New(editorOptionsFactoryService As IEditorOptionsFactoryService)
            _editorOptionsFactoryService = editorOptionsFactoryService
        End Sub

        Public Function CreateTextPoint(fileCodeModel As FileCodeModel, point As VirtualTreePoint) As EnvDTE.TextPoint Implements ITextManagerAdapter.CreateTextPoint
            Dim textBuffer = point.Text.Container.TryGetTextBuffer()
            Dim tabSize = _editorOptionsFactoryService.GetOptions(textBuffer).GetTabSize()
            Return New MockTextPoint(point, tabSize)
        End Function
    End Class
End Namespace
