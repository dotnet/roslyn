' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.LineCommit
    <Export(GetType(CommitBufferManagerFactory))>
    Friend Class CommitBufferManagerFactory
        Private ReadOnly _commitFormatter As ICommitFormatter
        Private ReadOnly _inlineRenameService As IInlineRenameService

        <ImportingConstructor()>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New(commitFormatter As ICommitFormatter, inlineRenameService As IInlineRenameService, threadingContext As IThreadingContext)
            _commitFormatter = commitFormatter
            _inlineRenameService = inlineRenameService
        End Sub

        Public Function CreateForBuffer(buffer As ITextBuffer) As CommitBufferManager
            Return buffer.Properties.GetOrCreateSingletonProperty(Function() New CommitBufferManager(buffer, _commitFormatter, _inlineRenameService))
        End Function
    End Class
End Namespace
