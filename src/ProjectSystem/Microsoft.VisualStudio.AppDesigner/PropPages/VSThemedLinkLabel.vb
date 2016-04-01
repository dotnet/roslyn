' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Drawing
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.Editors.AppDesCommon

Public Class VSThemedLinkLabel
    Inherits System.Windows.Forms.LinkLabel

    Public Sub New()
        MyBase.New()

        _vsThemedLinkColor = MyBase.LinkColor
        _vsThemedLinkColorHover = MyBase.LinkColor

    End Sub

    Private _vsThemedLinkColor As System.Drawing.Color
    Private _vsThemedLinkColorHover As System.Drawing.Color

    Public Sub SetThemedColor(ByVal vsUIShell5 As IVsUIShell5)

        Dim environmentThemeCategory As Guid = New Guid("624ed9c3-bdfd-41fa-96c3-7c824ea32e3d")

        ' The default value is the actual value of DiagReportLinkTextHover and DiagReportLinkText defined by Dev11
        _vsThemedLinkColorHover = ShellUtil.GetDesignerThemeColor(vsUIShell5, environmentThemeCategory, "DiagReportLinkTextHover", __THEMEDCOLORTYPE.TCT_Background, Color.FromArgb(&HFF1382CE))
        _vsThemedLinkColor = ShellUtil.GetDesignerThemeColor(vsUIShell5, environmentThemeCategory, "DiagReportLinkText", __THEMEDCOLORTYPE.TCT_Background, Color.FromArgb(&HFF1382CE))

        ' By design, active link color also maps to DiagReportLinkTextHover
        MyBase.ActiveLinkColor = _vsThemedLinkColorHover
        MyBase.LinkColor = _vsThemedLinkColor

    End Sub

    Private Sub VsThemedLinkLabel_MouseEnter(sender As Object, e As System.EventArgs) Handles MyBase.MouseEnter
        MyBase.LinkColor = _vsThemedLinkColorHover
    End Sub

    Private Sub VsThemedLinkLabel_MouseLeave(sender As Object, e As System.EventArgs) Handles MyBase.MouseLeave
        MyBase.LinkColor = _vsThemedLinkColor
    End Sub
End Class
