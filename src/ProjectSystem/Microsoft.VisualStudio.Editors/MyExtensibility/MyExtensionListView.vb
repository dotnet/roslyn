Option Strict On
Option Explicit On

Imports System
Imports System.ComponentModel.Design
Imports System.Diagnostics
Imports System.Windows.Forms

Imports Microsoft.VisualStudio.Editors
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
                Debug.Assert(m_MenuCommandService IsNot Nothing)
                Return m_MenuCommandService
            End Get
            Set(ByVal value As IMenuCommandService)
                Debug.Assert(value IsNot Nothing)
                Me.UnregisterMenuCommands()
                m_MenuCommandService = value
                Me.RegisterMenuCommands()
            End Set
        End Property

        Private Sub MyExtensionListView_ContextMenuShow( _
                ByVal sender As Object, ByVal e As MouseEventArgs) Handles Me.ContextMenuShow

            m_MenuCommandRemoveExtension.Enabled = Me.SelectedItems.Count > 0

            Me.MenuCommandService.ShowContextMenu( _
                Constants.MenuConstants.CommandIDMYEXTENSIONContextMenu, e.X, e.Y)
        End Sub

        Private Sub RegisterMenuCommands()
            For Each menuCommand As MenuCommand In m_MenuCommands
                Dim existingCommand As MenuCommand = Me.MenuCommandService.FindCommand(menuCommand.CommandID)
                If existingCommand IsNot Nothing Then
                    Me.MenuCommandService.RemoveCommand(existingCommand)
                End If
                Me.MenuCommandService.AddCommand(menuCommand)
            Next
        End Sub

        Private Sub UnregisterMenuCommands()
            If m_MenuCommandService IsNot Nothing Then
                For Each menuCommand As MenuCommand In m_MenuCommands
                    m_MenuCommandService.RemoveCommand(menuCommand)
                Next
            End If
        End Sub

        Private Sub AddExtension_Click(ByVal sender As Object, ByVal e As EventArgs)
            RaiseEvent AddExtension(sender, e)
        End Sub

        Private Sub RemoveExtension_Click(ByVal sender As Object, ByVal e As EventArgs)
            RaiseEvent RemoveExtension(sender, e)
        End Sub

        Private m_MenuCommandService As IMenuCommandService

        Private m_MenuCommandAddExtension As New MenuCommand( _
            New EventHandler(AddressOf AddExtension_Click), _
            Constants.MenuConstants.CommandIDMyEXTENSIONAddExtension)
        Private m_MenuCommandRemoveExtension As New MenuCommand( _
            New EventHandler(AddressOf RemoveExtension_Click), _
            Constants.MenuConstants.CommandIDMyEXTENSIONRemoveExtension)
        Private m_MenuCommands() As MenuCommand = _
            {m_MenuCommandAddExtension, m_MenuCommandRemoveExtension}
    End Class
End Namespace