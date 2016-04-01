' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict On
Option Explicit On
Imports System.ComponentModel.Design
Imports System.Windows.Forms
Imports Microsoft.VisualStudio.Editors.DesignerFramework

Namespace Microsoft.VisualStudio.Editors.MyExtensibility

    ''' ;MyExtensionListView
    ''' <summary>
    ''' List view used on My Extension Property Page which can show context menu
    ''' using IMenuCommandService.
    ''' </summary>
    Friend Class MyExtensionListView
        Inherits DesignerListView

        Public Event AddExtension(ByVal sender As Object, ByVal e As EventArgs)
        Public Event RemoveExtension(ByVal sender As Object, ByVal e As EventArgs)

        Public Property MenuCommandService() As IMenuCommandService
            Get
                Debug.Assert(_menuCommandService IsNot Nothing)
                Return _menuCommandService
            End Get
            Set(ByVal value As IMenuCommandService)
                Debug.Assert(value IsNot Nothing)
                Me.UnregisterMenuCommands()
                _menuCommandService = value
                Me.RegisterMenuCommands()
            End Set
        End Property

        Private Sub MyExtensionListView_ContextMenuShow( _
                ByVal sender As Object, ByVal e As MouseEventArgs) Handles Me.ContextMenuShow

            _menuCommandRemoveExtension.Enabled = Me.SelectedItems.Count > 0

            Me.MenuCommandService.ShowContextMenu( _
                Constants.MenuConstants.CommandIDMYEXTENSIONContextMenu, e.X, e.Y)
        End Sub

        Private Sub RegisterMenuCommands()
            For Each menuCommand As MenuCommand In _menuCommands
                Dim existingCommand As MenuCommand = Me.MenuCommandService.FindCommand(menuCommand.CommandID)
                If existingCommand IsNot Nothing Then
                    Me.MenuCommandService.RemoveCommand(existingCommand)
                End If
                Me.MenuCommandService.AddCommand(menuCommand)
            Next
        End Sub

        Private Sub UnregisterMenuCommands()
            If _menuCommandService IsNot Nothing Then
                For Each menuCommand As MenuCommand In _menuCommands
                    _menuCommandService.RemoveCommand(menuCommand)
                Next
            End If
        End Sub

        Private Sub AddExtension_Click(ByVal sender As Object, ByVal e As EventArgs)
            RaiseEvent AddExtension(sender, e)
        End Sub

        Private Sub RemoveExtension_Click(ByVal sender As Object, ByVal e As EventArgs)
            RaiseEvent RemoveExtension(sender, e)
        End Sub

        Private _menuCommandService As IMenuCommandService

        Private _menuCommandAddExtension As New MenuCommand( _
            New EventHandler(AddressOf AddExtension_Click), _
            Constants.MenuConstants.CommandIDMyEXTENSIONAddExtension)
        Private _menuCommandRemoveExtension As New MenuCommand( _
            New EventHandler(AddressOf RemoveExtension_Click), _
            Constants.MenuConstants.CommandIDMyEXTENSIONRemoveExtension)
        Private _menuCommands() As MenuCommand = _
            {_menuCommandAddExtension, _menuCommandRemoveExtension}
    End Class
End Namespace