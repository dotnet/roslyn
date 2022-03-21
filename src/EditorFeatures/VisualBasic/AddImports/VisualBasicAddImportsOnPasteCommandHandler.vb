' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports Microsoft.CodeAnalysis.AddImport
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.AddImports
    <Export>
    <Export(GetType(ICommandHandler))>
    <ContentType(ContentTypeNames.VisualBasicContentType)>
    <Name(PredefinedCommandHandlerNames.AddImportsPaste)>
    <Order(After:=PredefinedCommandHandlerNames.PasteTrackingPaste)>
    <Order(Before:=PredefinedCommandHandlerNames.FormatDocument)>
    Friend Class VisualBasicAddImportsOnPasteCommandHandler
        Inherits AbstractAddImportsPasteCommandHandler

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New(threadingContext As [Shared].Utilities.IThreadingContext,
                       globalOptions As IGlobalOptionService)
            MyBase.New(threadingContext, globalOptions)
        End Sub

        Public Overrides ReadOnly Property DisplayName As String = VBEditorResources.Add_Missing_Imports_on_Paste
        Protected Overrides ReadOnly Property DialogText As String = VBEditorResources.Adding_missing_imports
    End Class
End Namespace
