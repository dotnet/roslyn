' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Design
Imports System.Runtime.InteropServices
Imports Microsoft.VisualStudio.LanguageServices.Interactive
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.InteractiveWindow.Shell
Imports LanguageServiceGuids = Microsoft.VisualStudio.LanguageServices.Guids

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Interactive

    <Guid(LanguageServiceGuids.VisualBasicReplPackageIdString)>
    <PackageRegistration(UseManagedResourcesOnly:=True)>
    <ProvideMenuResource("Menus.ctmenu", 17)>
    <ProvideInteractiveWindow(
        VisualBasicVsInteractiveWindowPackage.IdString,
        Orientation:=ToolWindowOrientation.Bottom,
        Style:=VsDockStyle.Tabbed,
        Window:=CommonVsUtils.OutputWindowId)>
    Partial Friend Class VisualBasicVsInteractiveWindowPackage
        Inherits VsInteractiveWindowPackage(Of VisualBasicVsInteractiveWindowProvider)

        Friend Const IdString As String = "0A6D502E-93F7-4FCC-90D9-D5020BD54D69"
        Friend Shared ReadOnly Id As New Guid(IdString)

        Protected Overrides ReadOnly Property ToolWindowId As Guid
            Get
                Return Id
            End Get
        End Property

        Protected Overrides ReadOnly Property LanguageServiceGuid As Guid
            Get
                Return LanguageServiceGuids.VisualBasicLanguageServiceId
            End Get
        End Property

        Protected Overrides Sub InitializeMenuCommands(menuCommandService As OleMenuCommandService)
            Dim openInteractiveCommand = New MenuCommand(
                handler:=Sub(sender, args) Me.InteractiveWindowProvider.Open(instanceId:=0, focus:=True),
                command:=New CommandID(VisualBasicInteractiveCommands.InteractiveCommandSetId, VisualBasicInteractiveCommands.InteractiveToolWindow))

            menuCommandService.AddCommand(openInteractiveCommand)
        End Sub
    End Class
End Namespace

