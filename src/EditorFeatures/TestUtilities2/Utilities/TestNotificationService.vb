' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Notification

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
    Friend Class TestNotificationService
        Implements INotificationService

        Public MessageText As String
        Public MessageTitle As String
        Public MessageSeverity As NotificationSeverity

        Public ConfirmBoxText As String
        Public ConfirmBoxTitle As String
        Public ConfirmBoxSeverity As NotificationSeverity

        Public DesiredConfirmBoxResult As Boolean

        Public Sub SendNotification(message As String, Optional title As String = Nothing, Optional severity As NotificationSeverity = NotificationSeverity.Warning) Implements INotificationService.SendNotification
            MessageText = message
            MessageTitle = title
            MessageSeverity = severity
        End Sub

        Public Function ConfirmMessageBox(message As String, Optional title As String = Nothing, Optional severity As NotificationSeverity = NotificationSeverity.Warning) As Boolean Implements INotificationService.ConfirmMessageBox
            ConfirmBoxText = message
            ConfirmBoxTitle = title
            ConfirmBoxSeverity = severity

            Return DesiredConfirmBoxResult
        End Function
    End Class
End Namespace
