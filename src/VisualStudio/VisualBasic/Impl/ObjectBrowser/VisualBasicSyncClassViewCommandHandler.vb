' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Library.ClassView
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ObjectBrowser
    <Export(GetType(Commanding.ICommandHandler))>
    <ContentType(ContentTypeNames.VisualBasicContentType)>
    <Name(PredefinedCommandHandlerNames.ClassView)>
    Friend Class VisualBasicSyncClassViewCommandHandler
        Inherits AbstractSyncClassViewCommandHandler

        <ImportingConstructor>
        Private Sub New(serviceProvider As SVsServiceProvider)
            MyBase.New(serviceProvider)
        End Sub
    End Class
End Namespace
