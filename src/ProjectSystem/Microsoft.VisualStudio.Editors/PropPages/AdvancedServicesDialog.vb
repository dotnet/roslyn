' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Diagnostics.CodeAnalysis
Imports System.Xml

Namespace Microsoft.VisualStudio.Editors.PropertyPages
    Friend Class AdvancedServicesDialog
        Inherits PropPageUserControlBase

        Private _savedXml As String
        Friend WithEvents RoleServiceCacheTimeoutLabel As System.Windows.Forms.Label
        Friend WithEvents TimeQuantity As System.Windows.Forms.NumericUpDown
        Friend WithEvents TimeUnitComboBox As System.Windows.Forms.ComboBox
        Friend WithEvents CustomConnectionString As System.Windows.Forms.TextBox
        Friend WithEvents UseCustomConnectionStringCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents HonorServerCookieExpirationCheckbox As System.Windows.Forms.CheckBox
        Friend WithEvents SavePasswordHashLocallyCheckbox As System.Windows.Forms.CheckBox
        Friend WithEvents TableLayoutPanel1 As System.Windows.Forms.TableLayoutPanel
        Private _appConfigDocument As XmlDocument

        Protected Overrides Sub PostInitPage()
            Try
                _appConfigDocument = ServicesPropPageAppConfigHelper.AppConfigXmlDocument(PropertyPageSite, ProjectHierarchy, False)
            Catch innerException As XmlException
                Dim ex As New XmlException(SR.GetString(SR.PPG_Services_InvalidAppConfigXml))
                DesignerFramework.DesignerMessageBox.Show(CType(ServiceProvider, IServiceProvider), "", ex, DesignerFramework.DesignUtil.GetDefaultCaption(Site))
                Enabled = False
                Return
            End Try

            Enabled = True

            SavePasswordHashLocallyCheckbox.Checked = ServicesPropPageAppConfigHelper.GetSavePasswordHashLocally(_appConfigDocument, ProjectHierarchy)
            Dim honorCookieExpiryValue As Nullable(Of Boolean) = ServicesPropPageAppConfigHelper.GetEffectiveHonorCookieExpiry(_appConfigDocument, ProjectHierarchy)
            If honorCookieExpiryValue.HasValue Then
                HonorServerCookieExpirationCheckbox.Checked = CBool(honorCookieExpiryValue)
            Else
                HonorServerCookieExpirationCheckbox.CheckState = System.Windows.Forms.CheckState.Indeterminate
            End If

            AddTimeUnitsToComboBox()
            SetCacheTimeoutControlValues(ServicesPropPageAppConfigHelper.GetCacheTimeout(_appConfigDocument, ProjectHierarchy))
            SetUseCustomConnectionStringControlValues(_appConfigDocument)

            _savedXml = _appConfigDocument.OuterXml
        End Sub

        Public Overrides Sub Apply()
            ServicesPropPageAppConfigHelper.TryWriteXml(_appConfigDocument, CType(ServiceProvider, IServiceProvider), ProjectHierarchy)
            Me.IsDirty = False
        End Sub

        Protected Overrides Sub Dispose(ByVal disposing As Boolean)
            Try
                If disposing AndAlso _components IsNot Nothing Then
                    _components.Dispose()
                End If
            Finally
                MyBase.Dispose(disposing)
            End Try
        End Sub


        'Required by the Windows Form Designer
        Private _components As System.ComponentModel.IContainer

        'NOTE: The following procedure is required by the Windows Form Designer
        'It can be modified using the Windows Form Designer.  
        'Do not modify it using the code editor.
        Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(AdvancedServicesDialog))
            Me.RoleServiceCacheTimeoutLabel = New System.Windows.Forms.Label
            Me.TimeQuantity = New System.Windows.Forms.NumericUpDown
            Me.TimeUnitComboBox = New System.Windows.Forms.ComboBox
            Me.CustomConnectionString = New System.Windows.Forms.TextBox
            Me.UseCustomConnectionStringCheckBox = New System.Windows.Forms.CheckBox
            Me.HonorServerCookieExpirationCheckbox = New System.Windows.Forms.CheckBox
            Me.SavePasswordHashLocallyCheckbox = New System.Windows.Forms.CheckBox
            Me.TableLayoutPanel1 = New System.Windows.Forms.TableLayoutPanel
            CType(Me.TimeQuantity, System.ComponentModel.ISupportInitialize).BeginInit()
            Me.TableLayoutPanel1.SuspendLayout()
            Me.SuspendLayout()
            '
            'RoleServiceCacheTimeoutLabel
            '
            resources.ApplyResources(Me.RoleServiceCacheTimeoutLabel, "RoleServiceCacheTimeoutLabel")
            Me.RoleServiceCacheTimeoutLabel.Name = "RoleServiceCacheTimeoutLabel"
            '
            'TimeQuantity
            '
            resources.ApplyResources(Me.TimeQuantity, "TimeQuantity")
            Me.TimeQuantity.Maximum = New Decimal(New Integer() {10000, 0, 0, 0})
            Me.TimeQuantity.Name = "TimeQuantity"
            Me.TimeQuantity.Value = New Decimal(New Integer() {60, 0, 0, 0})
            '
            'TimeUnitComboBox
            '
            Me.TimeUnitComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
            Me.TimeUnitComboBox.FormattingEnabled = True
            resources.ApplyResources(Me.TimeUnitComboBox, "TimeUnitComboBox")
            Me.TimeUnitComboBox.Name = "TimeUnitComboBox"
            '
            'CustomConnectionString
            '
            Me.TableLayoutPanel1.SetColumnSpan(Me.CustomConnectionString, 3)
            resources.ApplyResources(Me.CustomConnectionString, "CustomConnectionString")
            Me.CustomConnectionString.Name = "CustomConnectionString"
            '
            'UseCustomConnectionStringCheckBox
            '
            resources.ApplyResources(Me.UseCustomConnectionStringCheckBox, "UseCustomConnectionStringCheckBox")
            Me.TableLayoutPanel1.SetColumnSpan(Me.UseCustomConnectionStringCheckBox, 3)
            Me.UseCustomConnectionStringCheckBox.Name = "UseCustomConnectionStringCheckBox"
            Me.UseCustomConnectionStringCheckBox.UseVisualStyleBackColor = True
            '
            'HonorServerCookieExpirationCheckbox
            '
            resources.ApplyResources(Me.HonorServerCookieExpirationCheckbox, "HonorServerCookieExpirationCheckbox")
            Me.TableLayoutPanel1.SetColumnSpan(Me.HonorServerCookieExpirationCheckbox, 3)
            Me.HonorServerCookieExpirationCheckbox.Name = "HonorServerCookieExpirationCheckbox"
            Me.HonorServerCookieExpirationCheckbox.UseVisualStyleBackColor = True
            '
            'SavePasswordHashLocallyCheckbox
            '
            resources.ApplyResources(Me.SavePasswordHashLocallyCheckbox, "SavePasswordHashLocallyCheckbox")
            Me.TableLayoutPanel1.SetColumnSpan(Me.SavePasswordHashLocallyCheckbox, 3)
            Me.SavePasswordHashLocallyCheckbox.Name = "SavePasswordHashLocallyCheckbox"
            Me.SavePasswordHashLocallyCheckbox.UseVisualStyleBackColor = True
            '
            'TableLayoutPanel1
            '
            resources.ApplyResources(Me.TableLayoutPanel1, "TableLayoutPanel1")
            Me.TableLayoutPanel1.Controls.Add(Me.SavePasswordHashLocallyCheckbox, 0, 0)
            Me.TableLayoutPanel1.Controls.Add(Me.HonorServerCookieExpirationCheckbox, 0, 1)
            Me.TableLayoutPanel1.Controls.Add(Me.RoleServiceCacheTimeoutLabel, 0, 2)
            Me.TableLayoutPanel1.Controls.Add(Me.TimeQuantity, 1, 2)
            Me.TableLayoutPanel1.Controls.Add(Me.TimeUnitComboBox, 2, 2)
            Me.TableLayoutPanel1.Controls.Add(Me.UseCustomConnectionStringCheckBox, 0, 3)
            Me.TableLayoutPanel1.Controls.Add(Me.CustomConnectionString, 0, 4)
            Me.TableLayoutPanel1.Name = "TableLayoutPanel1"
            '
            'AdvancedServicesDialog
            '
            resources.ApplyResources(Me, "$this")
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.Controls.Add(Me.TableLayoutPanel1)
            Me.Name = "AdvancedServicesDialog"
            CType(Me.TimeQuantity, System.ComponentModel.ISupportInitialize).EndInit()
            Me.TableLayoutPanel1.ResumeLayout(False)
            Me.TableLayoutPanel1.PerformLayout()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub

        Protected Overrides Function GetF1HelpKeyword() As String
            Return HelpKeywords.VBProjPropAdvancedServices
        End Function

        Public Sub New()
            InitializeComponent()

            'Opt out of page scaling since we're using AutoScaleMode
            PageRequiresScaling = False
        End Sub

        Private Sub AddTimeUnitsToComboBox()
            If TimeUnitComboBox.Items.Count = 0 Then
                TimeUnitComboBox.Items.Add(SR.GetString(SR.PPG_Services_Seconds))
                TimeUnitComboBox.Items.Add(SR.GetString(SR.PPG_Services_Minutes))
                TimeUnitComboBox.Items.Add(SR.GetString(SR.PPG_Services_Hours))
                TimeUnitComboBox.Items.Add(SR.GetString(SR.PPG_Services_Days))
            End If
        End Sub

        Private Enum TimeUnit
            Seconds
            Minutes
            Hours
            Days
        End Enum

        Private Sub SetCacheTimeoutControlValues(ByVal cacheTimeout As Integer)
            If cacheTimeout < 0 Then cacheTimeout = 0
            If cacheTimeout > Integer.MaxValue Then cacheTimeout = Integer.MaxValue

            'The cache timeout value is in seconds.  
            Dim unit As TimeUnit = TimeUnit.Seconds

            If cacheTimeout <> 0 Then
                'Let's see whether we should display this as minutes, which we'll do if we have an even number of minutes...
                If cacheTimeout Mod 60 = 0 Then
                    cacheTimeout = cacheTimeout \ 60
                    unit = TimeUnit.Minutes
                End If

                'How about hours?
                If cacheTimeout Mod 60 = 0 Then
                    cacheTimeout = cacheTimeout \ 60
                    unit = TimeUnit.Hours
                End If

                'Days?
                If cacheTimeout Mod 24 = 0 Then
                    cacheTimeout = cacheTimeout \ 24
                    unit = TimeUnit.Days
                End If
            End If


            Select Case unit
                Case TimeUnit.Seconds
                    TimeUnitComboBox.Text = SR.GetString(SR.PPG_Services_Seconds)
                Case TimeUnit.Minutes
                    TimeUnitComboBox.Text = SR.GetString(SR.PPG_Services_Minutes)
                Case TimeUnit.Hours
                    TimeUnitComboBox.Text = SR.GetString(SR.PPG_Services_Hours)
                Case TimeUnit.Days
                    TimeUnitComboBox.Text = SR.GetString(SR.PPG_Services_Days)
            End Select

            TimeQuantity.Value = cacheTimeout
        End Sub

        Private Sub SetUseCustomConnectionStringControlValues(ByVal doc As XmlDocument)
            Dim connectionStringSpecified As Boolean
            Dim connectionString As String = ServicesPropPageAppConfigHelper.GetEffectiveDefaultConnectionString(doc, connectionStringSpecified, ProjectHierarchy)
            If Not connectionStringSpecified Then
                'There were connection strings, but they're not all the same connection string
                UseCustomConnectionStringCheckBox.Enabled = False
                UseCustomConnectionStringCheckBox.CheckState = System.Windows.Forms.CheckState.Indeterminate
            ElseIf connectionString Is Nothing Then
                'The default value
                UseCustomConnectionStringCheckBox.Enabled = True
                UseCustomConnectionStringCheckBox.CheckState = System.Windows.Forms.CheckState.Unchecked
                CustomConnectionString.Text = SR.GetString(SR.PPG_Services_connectionStringValueDefaultDisplayValue)
                CustomConnectionString.Enabled = False
            Else
                'Using a non-default connection string for all providers
                UseCustomConnectionStringCheckBox.Enabled = True
                UseCustomConnectionStringCheckBox.CheckState = System.Windows.Forms.CheckState.Checked
                CustomConnectionString.Text = connectionString
                CustomConnectionString.Enabled = True
            End If
            UpdateCustomConnectionStringControlBasedOnCheckState()
            Dim preferredHeight As Integer = CustomConnectionString.GetPreferredSize(New System.Drawing.Size(CustomConnectionString.Width, 0)).Height
            If CustomConnectionString.Height < preferredHeight Then CustomConnectionString.Height = preferredHeight
            SetDirtyFlag()
        End Sub

        Private Sub UseCustomConnectionStringCheckBox_CheckStateChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles UseCustomConnectionStringCheckBox.CheckStateChanged
            UpdateCustomConnectionStringControlBasedOnCheckState()
        End Sub


        <SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters")> _
        Private Sub UpdateCustomConnectionStringControlBasedOnCheckState()
            Select Case UseCustomConnectionStringCheckBox.CheckState
                Case System.Windows.Forms.CheckState.Indeterminate
                    'The connection strings don't match
                    CustomConnectionString.Text = SR.GetString(SR.PPG_Services_ConnectionStringsDontMatch)
                Case System.Windows.Forms.CheckState.Checked
                    'We're using a custom connection string
                    'Either the text has already been set (in which case we're good), or it's the display default message, in which case we should
                    'change it to the default value.
                    If CustomConnectionString.Text = SR.GetString(SR.PPG_Services_connectionStringValueDefaultDisplayValue) Then
                        CustomConnectionString.Text = ServicesPropPageAppConfigHelper.connectionStringValueDefault
                    End If
                    ServicesPropPageAppConfigHelper.SetConnectionStringText(_appConfigDocument, CustomConnectionString.Text, ProjectHierarchy)
                Case System.Windows.Forms.CheckState.Unchecked
                    'We're using the default
                    CustomConnectionString.Text = SR.GetString(SR.PPG_Services_connectionStringValueDefaultDisplayValue)
                    ServicesPropPageAppConfigHelper.SetConnectionStringText(_appConfigDocument, Nothing, ProjectHierarchy)
            End Select

            CustomConnectionString.Enabled = UseCustomConnectionStringCheckBox.CheckState = System.Windows.Forms.CheckState.Checked

            SetDirtyFlag()
        End Sub

        Private Sub CustomConnectionString_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles CustomConnectionString.TextChanged
            If CustomConnectionString.Enabled Then
                ServicesPropPageAppConfigHelper.SetConnectionStringText(_appConfigDocument, CustomConnectionString.Text, ProjectHierarchy)
                SetDirtyFlag()
            End If
        End Sub

        Private Sub TimeUnitComboBox_SelectedIndexChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles TimeUnitComboBox.SelectedIndexChanged
            SetCacheTimeout()
        End Sub

        Private Sub TimeQuantity_ValueChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles TimeQuantity.ValueChanged
            SetCacheTimeout()
        End Sub

        Private Sub SetCacheTimeout()
            Dim seconds As Integer
            Dim multiplier As Integer

            Select Case TimeUnitComboBox.SelectedIndex
                Case 0 'Seconds
                    multiplier = 1
                Case 1 'Minutes
                    multiplier = 60
                Case 2 'Hours
                    multiplier = 60 * 60
                Case 3 'Days
                    multiplier = 60 * 60 * 24
                Case Else 'Setting for the first time, or something wacky happened
                    multiplier = 1
            End Select
            TimeQuantity.Maximum = Integer.MaxValue \ multiplier
            seconds = CInt(TimeQuantity.Value) * multiplier
            ServicesPropPageAppConfigHelper.SetCacheTimeout(_appConfigDocument, seconds, ProjectHierarchy)
            SetDirtyFlag()
        End Sub

        Private Sub SavePasswordHashLocallyCheckbox_CheckedChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles SavePasswordHashLocallyCheckbox.CheckedChanged
            ServicesPropPageAppConfigHelper.SetSavePasswordHashLocally(_appConfigDocument, SavePasswordHashLocallyCheckbox.Checked, ProjectHierarchy)
            SetDirtyFlag()
        End Sub

        Private Sub HonorServerCookieExpirySavePasswordHashLocallyCheckbox_CheckedChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles HonorServerCookieExpirationCheckbox.CheckedChanged
            ServicesPropPageAppConfigHelper.SetHonorCookieExpiry(_appConfigDocument, HonorServerCookieExpirationCheckbox.Checked, ProjectHierarchy)
            SetDirtyFlag()
        End Sub

        Private Sub SetDirtyFlag()
            Me.IsDirty = _appConfigDocument IsNot Nothing AndAlso _appConfigDocument.OuterXml <> _savedXml
        End Sub
    End Class
End Namespace
