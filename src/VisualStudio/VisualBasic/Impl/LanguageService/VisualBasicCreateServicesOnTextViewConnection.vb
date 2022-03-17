' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic
    <Export(GetType(IWpfTextViewConnectionListener))>
    <ContentType(ContentTypeNames.VisualBasicContentType)>
    <TextViewRole(PredefinedTextViewRoles.Interactive)>
    Friend Class VisualBasicCreateServicesOnTextViewConnection
        Inherits AbstractCreateServicesOnTextViewConnection

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(
            workspace As VisualStudioWorkspace,
            globalOptions As IGlobalOptionService,
            listenerProvider As IAsynchronousOperationListenerProvider,
            threadingContext As IThreadingContext)

            MyBase.New(workspace, globalOptions, listenerProvider, threadingContext, languageName:=LanguageNames.VisualBasic)
        End Sub
    End Class
End Namespace
