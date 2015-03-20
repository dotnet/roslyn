' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.VisualStudio.Editor
Imports Microsoft.VisualStudio.LanguageServices.Interactive
Imports Microsoft.VisualStudio.OLE.Interop
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Utilities
Imports Microsoft.VisualStudio.InteractiveWindow
Imports Microsoft.VisualStudio.InteractiveWindow.Commands
Imports Microsoft.VisualStudio.InteractiveWindow.Shell

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Interactive

    <Export(GetType(IVsInteractiveWindowOleCommandTargetProvider))>
    <ContentType(ContentTypeNames.VisualBasicContentType)>
    <ContentType(PredefinedInteractiveCommandsContentTypes.InteractiveCommandContentTypeName)>
    Friend NotInheritable Class VisualBasicVsInteractiveWindowCommandProvider
        Implements IVsInteractiveWindowOleCommandTargetProvider

        Private ReadOnly _editorAdaptersFactory As IVsEditorAdaptersFactoryService
        Private ReadOnly _commandHandlerServiceFactory As ICommandHandlerServiceFactory
        Private ReadOnly _serviceProvider As System.IServiceProvider

        <ImportingConstructor()>
        Public Sub New(commandHandlerServiceFactory As ICommandHandlerServiceFactory, editorAdaptersFactoryService As IVsEditorAdaptersFactoryService, serviceProvider As SVsServiceProvider)
            Me._commandHandlerServiceFactory = commandHandlerServiceFactory
            Me._editorAdaptersFactory = editorAdaptersFactoryService
            Me._serviceProvider = serviceProvider
        End Sub

        Public Function GetCommandTarget(textView As IWpfTextView, nextTarget As IOleCommandTarget) As IOleCommandTarget _
            Implements IVsInteractiveWindowOleCommandTargetProvider.GetCommandTarget

            Dim target = New ScriptingOleCommandTarget(textView, _commandHandlerServiceFactory, _editorAdaptersFactory, _serviceProvider)
            target.RefreshCommandFilters()
            target.NextCommandTarget = nextTarget
            Return target
        End Function
    End Class
End Namespace

