' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Host.Mef
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
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(threadingContext As IThreadingContext, <Import(GetType(SVsServiceProvider))> serviceProvider As IServiceProvider)
            MyBase.New(threadingContext, serviceProvider, LanguageNames.VisualBasic)
        End Sub
    End Class
End Namespace
