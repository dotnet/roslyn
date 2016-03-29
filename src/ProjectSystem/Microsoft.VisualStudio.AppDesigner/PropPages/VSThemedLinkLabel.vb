'******************************************************************************
'* VSThemedLinkLabel.vb
'*
'* Copyright (C) Microsoft Corporation. All Rights Reserved.
'* Information Contained Herein Is Proprietary and Confidential.
'******************************************************************************

Imports System
Imports System.Drawing
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.Editors.AppDesCommon

Public Class VSThemedLinkLabel
    Inherits System.Windows.Forms.LinkLabel

    Sub New()
        MyBase.New()

        m_VsThemedLinkColor = MyBase.LinkColor
        m_VsThemedLinkColorHover = MyBase.LinkColor

    End Sub

    Private m_VsThemedLinkColor As System.Drawing.Color
    Private m_VsThemedLinkColorHover As System.Drawing.Color

    Public Sub SetThemedColor(ByVal vsUIShell5 As IVsUIShell5)

        Dim environmentThemeCategory As Guid = New Guid("624ed9c3-bdfd-41fa-96c3-7c824ea32e3d")

        ' The default value is the actual value of DiagReportLinkTextHover and DiagReportLinkText defined by Dev11
        m_VsThemedLinkColorHover = ShellUtil.GetDesignerThemeColor(vsUIShell5, environmentThemeCategory, "DiagReportLinkTextHover", __THEMEDCOLORTYPE.TCT_Background, Color.FromArgb(&HFF1382CE))
        m_VsThemedLinkColor = ShellUtil.GetDesignerThemeColor(vsUIShell5, environmentThemeCategory, "DiagReportLinkText", __THEMEDCOLORTYPE.TCT_Background, Color.FromArgb(&HFF1382CE))

        ' By design, active link color also maps to DiagReportLinkTextHover
        MyBase.ActiveLinkColor = m_VsThemedLinkColorHover
        MyBase.LinkColor = m_VsThemedLinkColor

    End Sub

    Private Sub VsThemedLinkLabel_MouseEnter(sender As Object, e As System.EventArgs) Handles MyBase.MouseEnter
        MyBase.LinkColor = m_VsThemedLinkColorHover
    End Sub

    Private Sub VsThemedLinkLabel_MouseLeave(sender As Object, e As System.EventArgs) Handles MyBase.MouseLeave
        MyBase.LinkColor = m_VsThemedLinkColor
    End Sub
End Class
