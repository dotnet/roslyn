' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Library.ClassView
Imports Microsoft.VisualStudio.Shell

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ObjectBrowser
    <ExportCommandHandler(PredefinedCommandHandlerNames.ClassView, ContentTypeNames.VisualBasicContentType)>
    Friend Class VisualBasicSyncClassViewCommandHandler
        Inherits AbstractSyncClassViewCommandHandler

        <ImportingConstructor>
        Private Sub New(serviceProvider As SVsServiceProvider, waitIndicator As IWaitIndicator)
            MyBase.New(serviceProvider, waitIndicator)
        End Sub
    End Class
End Namespace
