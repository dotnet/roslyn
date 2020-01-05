' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.VisualStudio.GraphModel
Imports Microsoft.VisualStudio.Language.Intellisense
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Progression
Imports Microsoft.VisualStudio.Shell

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Progression

    <GraphProvider(Name:="VisualBasicRoslynProvider", ProjectCapability:="VB")>
    Friend NotInheritable Class VisualBasicGraphProvider
        Inherits AbstractGraphProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(threadingContext As IThreadingContext, glyphService As IGlyphService, serviceProvider As SVsServiceProvider, workspaceProvider As IProgressionPrimaryWorkspaceProvider, listenerProvider As IAsynchronousOperationListenerProvider)
            MyBase.New(threadingContext, glyphService, serviceProvider, workspaceProvider.PrimaryWorkspace, listenerProvider)
        End Sub
    End Class
End Namespace
