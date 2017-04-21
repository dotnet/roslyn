' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.LineCommit
    <Export(GetType(CommitBufferManagerFactory))>
    Friend Class CommitBufferManagerFactory
        Private ReadOnly _commitFormatter As ICommitFormatter
        Private ReadOnly _inlineRenameService As IInlineRenameService

        <ImportingConstructor()>
        Public Sub New(commitFormatter As ICommitFormatter, inlineRenameService As IInlineRenameService)
            _commitFormatter = commitFormatter
            _inlineRenameService = inlineRenameService
        End Sub

        Public Function CreateForBuffer(buffer As ITextBuffer) As CommitBufferManager
            Return buffer.Properties.GetOrCreateSingletonProperty(Function() New CommitBufferManager(buffer, _commitFormatter, _inlineRenameService))
        End Function
    End Class
End Namespace
