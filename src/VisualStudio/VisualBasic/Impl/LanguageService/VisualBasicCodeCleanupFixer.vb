
' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeCleanup
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.LanguageService
    <Export(GetType(AbstractCodeCleanUpFixer))>
    <ContentType(ContentTypeNames.VisualBasicContentType)>
    Friend Class VisualBasicCodeCleanUpFixer
        Inherits AbstractCodeCleanUpFixer

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(threadingContext As IThreadingContext, workspace As VisualStudioWorkspaceImpl, vsHierarchyItemManager As IVsHierarchyItemManager)
            MyBase.New(threadingContext, workspace, vsHierarchyItemManager)
        End Sub
    End Class
End Namespace
