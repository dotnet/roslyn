' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.InteropServices
Imports System.Windows
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Notification
Imports Microsoft.VisualStudio.ComponentModelHost
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    <Guid(Guids.VisualBasicOptionPageNamingStyleIdString)>
    Friend Class NamingStylesOptionPage
        Inherits AbstractOptionPage

        Private _grid As NamingStyleOptionPageControl
        Private _notificationService As INotificationService

        Protected Overrides Function CreateOptionPage(serviceProvider As IServiceProvider, optionStore As OptionStore) As AbstractOptionPageControl
            Dim componentModel = DirectCast(serviceProvider.GetService(GetType(SComponentModel)), IComponentModel)
            Dim workspace = componentModel.GetService(Of VisualStudioWorkspace)
            _notificationService = workspace.Services.GetService(Of INotificationService)

            _grid = New NamingStyleOptionPageControl(optionStore, _notificationService, LanguageNames.VisualBasic)
            Return _grid
        End Function

        Protected Overrides Sub OnApply(e As PageApplyEventArgs)
            If _grid.ContainsErrors() Then
                _notificationService.SendNotification(ServicesVSResources.Some_naming_rules_are_incomplete_Please_complete_or_remove_them)
                e.ApplyBehavior = ApplyKind.Cancel
                Return
            End If

            MyBase.OnApply(e)
        End Sub
    End Class
End Namespace
