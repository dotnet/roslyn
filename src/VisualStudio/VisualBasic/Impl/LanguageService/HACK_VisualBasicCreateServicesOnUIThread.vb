' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic
    <Export(GetType(IWpfTextViewConnectionListener))>
    <ContentType(ContentTypeNames.VisualBasicContentType)>
    <TextViewRole(PredefinedTextViewRoles.Interactive)>
    Friend Class HACK_VisualBasicCreateServicesOnUIThread
        Inherits HACK_AbstractCreateServicesOnUiThread

        <ImportingConstructor>
        Public Sub New(<Import(GetType(SVsServiceProvider))> serviceProvider As IServiceProvider)
            MyBase.New(serviceProvider, LanguageNames.VisualBasic)
        End Sub
    End Class
End Namespace
