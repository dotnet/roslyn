' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.VisualStudio.GraphModel
Imports Microsoft.VisualStudio.Language.Intellisense
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Progression
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.CodeAnalysis.Editor.Host

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Progression
    <GraphProvider(Name:="VisualBasicRoslynProvider", ProjectCapability:="VB")>
    Friend NotInheritable Class VisualBasicGraphProvider
        Inherits AbstractGraphProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(
                threadingContext As IThreadingContext,
                glyphService As IGlyphService,
                serviceProvider As SVsServiceProvider,
                workspace As VisualStudioWorkspace,
                streamingPresenter As Lazy(Of IStreamingFindUsagesPresenter),
                listenerProvider As IAsynchronousOperationListenerProvider)
            MyBase.New(threadingContext, glyphService, serviceProvider, workspace, streamingPresenter, listenerProvider)
        End Sub
    End Class
End Namespace
