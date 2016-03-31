Imports System.ComponentModel
Imports System.Globalization
Imports VSLangProj80
Imports System.Windows.Forms

Imports Microsoft.VisualStudio.Editors.Common

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    Friend Class AssemblyInfoPropPage
        'Inherits System.Windows.Forms.UserControl
        Inherits PropPageUserControlBase

        Private m_FileVersionTextBoxes As System.Windows.Forms.TextBox()
        Private m_AssemblyVersionTextBoxes As System.Windows.Forms.TextBox()

        'After 65535, the project system doesn't complain, and in theory any value is allowed as
        '  the string version of this, but after this value the numeric version of the file version
        '  no longer matches the string version.
        Const MaxFileVersionPartValue As UInteger = 65535
        Friend WithEvents NeutralLanguageComboBox As System.Windows.Forms.ComboBox

        'After 65535, the project system doesn't complain, but you get a compile error.
        Const MaxAssemblyVersionPartValue As UInteger = 65534

        Private m_NeutralLanguageNoneText As String 'Text for "None" in the neutral language combobox (stored in case thread language changes)


        ''' <summary>
        ''' Customizable processing done before the class has populated controls in the ControlData array
        ''' </summary>
        ''' <remarks>
        ''' Override this to implement custom processing.
        ''' IMPORTANT NOTE: this method can be called multiple times on the same page.  In particular,
        '''   it is called on every SetObjects call, which means that when the user changes the
        '''   selected configuration, it is called again. 
        ''' </remarks>
        Protected Overrides Sub PreInitPage()
            MyBase.PreInitPage()
        End Sub

#Region " Windows Form Designer generated code "

        Public Sub New()
            MyBase.New()

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            'Add any initialization after the InitializeComponent() call

            AddChangeHandlers()

            MyBase.PageRequiresScaling = False

            m_FileVersionTextBoxes = New System.Windows.Forms.TextBox(3) { _
                Me.FileVersionMajorTextBox, Me.FileVersionMinorTextBox, Me.FileVersionBuildTextBox, Me.FileVersionRevisionTextBox}
            m_AssemblyVersionTextBoxes = New System.Windows.Forms.TextBox(3) { _
                Me.AssemblyVersionMajorTextBox, Me.AssemblyVersionMinorTextBox, Me.AssemblyVersionBuildTextBox, Me.AssemblyVersionRevisionTextBox}
            m_NeutralLanguageNoneText = SR.GetString(SR.PPG_NeutralLanguage_None)
        End Sub

        'Form overrides dispose to clean up the component list.
        Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
            If disposing Then
                If Not (components Is Nothing) Then
                    components.Dispose()
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub

        'Required by the Windows Form Designer
        Private components As System.ComponentModel.IContainer

        'NOTE: The following procedure is required by the Windows Form Designer
        'It can be modified using the Windows Form Designer.  
        'Do not modify it using the code editor.
        Friend WithEvents Title As System.Windows.Forms.TextBox
        Friend WithEvents Description As System.Windows.Forms.TextBox
        Friend WithEvents Company As System.Windows.Forms.TextBox
        Friend WithEvents Product As System.Windows.Forms.TextBox
        Friend WithEvents Copyright As System.Windows.Forms.TextBox
        Friend WithEvents Trademark As System.Windows.Forms.TextBox
        Friend WithEvents TitleLabel As System.Windows.Forms.Label
        Friend WithEvents TrademarkLabel As System.Windows.Forms.Label
        Friend WithEvents CopyrightLabel As System.Windows.Forms.Label
        Friend WithEvents ProductLabel As System.Windows.Forms.Label
        Friend WithEvents CompanyLabel As System.Windows.Forms.Label
        Friend WithEvents overarchingTableLayoutPanel As System.Windows.Forms.TableLayoutPanel
        Friend WithEvents AssemblyVersionLabel As System.Windows.Forms.Label
        Friend WithEvents FileVersionLabel As System.Windows.Forms.Label
        Friend WithEvents ComVisibleCheckBox As System.Windows.Forms.CheckBox
        Friend WithEvents GuidLabel As System.Windows.Forms.Label
        Friend WithEvents GuidTextBox As System.Windows.Forms.TextBox
        Friend WithEvents NeutralLanguageLabel As System.Windows.Forms.Label
        Friend WithEvents AssemblyVersionLayoutPanel As TableLayoutPanel
        Friend WithEvents AssemblyVersionMajorTextBox As System.Windows.Forms.TextBox
        Friend WithEvents AssemblyVersionMinorTextBox As System.Windows.Forms.TextBox
        Friend WithEvents AssemblyVersionBuildTextBox As System.Windows.Forms.TextBox
        Friend WithEvents AssemblyVersionRevisionTextBox As System.Windows.Forms.TextBox
        Friend WithEvents FileVersionLayoutPanel As TableLayoutPanel
        Friend WithEvents FileVersionMajorTextBox As System.Windows.Forms.TextBox
        Friend WithEvents FileVersionMinorTextBox As System.Windows.Forms.TextBox
        Friend WithEvents FileVersionBuildTextBox As System.Windows.Forms.TextBox
        Friend WithEvents FileVersionRevisionTextBox As System.Windows.Forms.TextBox
        Friend WithEvents DescriptionLabel As System.Windows.Forms.Label

        <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(AssemblyInfoPropPage))
            Me.TitleLabel = New System.Windows.Forms.Label()
            Me.TrademarkLabel = New System.Windows.Forms.Label()
            Me.CopyrightLabel = New System.Windows.Forms.Label()
            Me.ProductLabel = New System.Windows.Forms.Label()
            Me.CompanyLabel = New System.Windows.Forms.Label()
            Me.DescriptionLabel = New System.Windows.Forms.Label()
            Me.Title = New System.Windows.Forms.TextBox()
            Me.Description = New System.Windows.Forms.TextBox()
            Me.Company = New System.Windows.Forms.TextBox()
            Me.Product = New System.Windows.Forms.TextBox()
            Me.Copyright = New System.Windows.Forms.TextBox()
            Me.Trademark = New System.Windows.Forms.TextBox()
            Me.overarchingTableLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.ComVisibleCheckBox = New System.Windows.Forms.CheckBox()
            Me.NeutralLanguageLabel = New System.Windows.Forms.Label()
            Me.AssemblyVersionLabel = New System.Windows.Forms.Label()
            Me.AssemblyVersionMajorTextBox = New System.Windows.Forms.TextBox()
            Me.AssemblyVersionMinorTextBox = New System.Windows.Forms.TextBox()
            Me.AssemblyVersionBuildTextBox = New System.Windows.Forms.TextBox()
            Me.AssemblyVersionRevisionTextBox = New System.Windows.Forms.TextBox()
            Me.FileVersionLabel = New System.Windows.Forms.Label()
            Me.FileVersionMajorTextBox = New System.Windows.Forms.TextBox()
            Me.FileVersionMinorTextBox = New System.Windows.Forms.TextBox()
            Me.FileVersionBuildTextBox = New System.Windows.Forms.TextBox()
            Me.FileVersionRevisionTextBox = New System.Windows.Forms.TextBox()
            Me.GuidTextBox = New System.Windows.Forms.TextBox()
            Me.GuidLabel = New System.Windows.Forms.Label()
            Me.NeutralLanguageComboBox = New System.Windows.Forms.ComboBox()
            Me.AssemblyVersionLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.FileVersionLayoutPanel = New System.Windows.Forms.TableLayoutPanel()
            Me.overarchingTableLayoutPanel.SuspendLayout()
            Me.AssemblyVersionLayoutPanel.SuspendLayout()
            Me.FileVersionLayoutPanel.SuspendLayout()
            Me.SuspendLayout()
            '
            'TitleLabel
            '
            resources.ApplyResources(Me.TitleLabel, "TitleLabel")
            Me.TitleLabel.Name = "TitleLabel"
            '
            'TrademarkLabel
            '
            resources.ApplyResources(Me.TrademarkLabel, "TrademarkLabel")
            Me.TrademarkLabel.Name = "TrademarkLabel"
            '
            'CopyrightLabel
            '
            resources.ApplyResources(Me.CopyrightLabel, "CopyrightLabel")
            Me.CopyrightLabel.Name = "CopyrightLabel"
            '
            'ProductLabel
            '
            resources.ApplyResources(Me.ProductLabel, "ProductLabel")
            Me.ProductLabel.Name = "ProductLabel"
            '
            'CompanyLabel
            '
            resources.ApplyResources(Me.CompanyLabel, "CompanyLabel")
            Me.CompanyLabel.Name = "CompanyLabel"
            '
            'DescriptionLabel
            '
            resources.ApplyResources(Me.DescriptionLabel, "DescriptionLabel")
            Me.DescriptionLabel.Name = "DescriptionLabel"
            '
            'Title
            '
            resources.ApplyResources(Me.Title, "Title")
            Me.Title.Name = "Title"
            '
            'Description
            '
            resources.ApplyResources(Me.Description, "Description")
            Me.Description.Name = "Description"
            '
            'Company
            '
            resources.ApplyResources(Me.Company, "Company")
            Me.Company.Name = "Company"
            '
            'Product
            '
            resources.ApplyResources(Me.Product, "Product")
            Me.Product.Name = "Product"
            '
            'Copyright
            '
            resources.ApplyResources(Me.Copyright, "Copyright")
            Me.Copyright.Name = "Copyright"
            '
            'Trademark
            '
            resources.ApplyResources(Me.Trademark, "Trademark")
            Me.Trademark.Name = "Trademark"
            '
            'overarchingTableLayoutPanel
            '
            resources.ApplyResources(Me.overarchingTableLayoutPanel, "overarchingTableLayoutPanel")
            Me.overarchingTableLayoutPanel.Controls.Add(Me.TitleLabel, 0, 0)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.ComVisibleCheckBox, 0, 10)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.NeutralLanguageLabel, 0, 9)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.Title, 1, 0)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.DescriptionLabel, 0, 1)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.Description, 1, 1)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.CompanyLabel, 0, 2)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.Company, 1, 2)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.ProductLabel, 0, 3)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.Product, 1, 3)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.CopyrightLabel, 0, 4)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.Copyright, 1, 4)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.TrademarkLabel, 0, 5)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.Trademark, 1, 5)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.AssemblyVersionLabel, 0, 6)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.AssemblyVersionLayoutPanel, 1, 6)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.FileVersionLabel, 0, 7)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.FileVersionLayoutPanel, 1, 7)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.GuidTextBox, 1, 8)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.GuidLabel, 0, 8)
            Me.overarchingTableLayoutPanel.Controls.Add(Me.NeutralLanguageComboBox, 1, 9)
            Me.overarchingTableLayoutPanel.Name = "overarchingTableLayoutPanel"
            '
            'ComVisibleCheckBox
            '
            resources.ApplyResources(Me.ComVisibleCheckBox, "ComVisibleCheckBox")
            Me.overarchingTableLayoutPanel.SetColumnSpan(Me.ComVisibleCheckBox, 2)
            Me.ComVisibleCheckBox.Name = "ComVisibleCheckBox"
            '
            'NeutralLanguageLabel
            '
            resources.ApplyResources(Me.NeutralLanguageLabel, "NeutralLanguageLabel")
            Me.NeutralLanguageLabel.Name = "NeutralLanguageLabel"
            '
            'AssemblyVersionLabel
            '
            resources.ApplyResources(Me.AssemblyVersionLabel, "AssemblyVersionLabel")
            Me.AssemblyVersionLabel.Name = "AssemblyVersionLabel"
            '
            'AssemblyVersionMajorTextBox
            '
            resources.ApplyResources(Me.AssemblyVersionMajorTextBox, "AssemblyVersionMajorTextBox")
            Me.AssemblyVersionMajorTextBox.Name = "AssemblyVersionMajorTextBox"
            '
            'AssemblyVersionMinorTextBox
            '
            resources.ApplyResources(Me.AssemblyVersionMinorTextBox, "AssemblyVersionMinorTextBox")
            Me.AssemblyVersionMinorTextBox.Name = "AssemblyVersionMinorTextBox"
            '
            'AssemblyVersionBuildTextBox
            '
            resources.ApplyResources(Me.AssemblyVersionBuildTextBox, "AssemblyVersionBuildTextBox")
            Me.AssemblyVersionBuildTextBox.Name = "AssemblyVersionBuildTextBox"
            '
            'AssemblyVersionRevisionTextBox
            '
            resources.ApplyResources(Me.AssemblyVersionRevisionTextBox, "AssemblyVersionRevisionTextBox")
            Me.AssemblyVersionRevisionTextBox.Name = "AssemblyVersionRevisionTextBox"
            '
            'FileVersionLabel
            '
            resources.ApplyResources(Me.FileVersionLabel, "FileVersionLabel")
            Me.FileVersionLabel.Name = "FileVersionLabel"
            '
            'FileVersionMajorTextBox
            '
            resources.ApplyResources(Me.FileVersionMajorTextBox, "FileVersionMajorTextBox")
            Me.FileVersionMajorTextBox.Name = "FileVersionMajorTextBox"
            '
            'FileVersionMinorTextBox
            '
            resources.ApplyResources(Me.FileVersionMinorTextBox, "FileVersionMinorTextBox")
            Me.FileVersionMinorTextBox.Name = "FileVersionMinorTextBox"
            '
            'FileVersionBuildTextBox
            '
            resources.ApplyResources(Me.FileVersionBuildTextBox, "FileVersionBuildTextBox")
            Me.FileVersionBuildTextBox.Name = "FileVersionBuildTextBox"
            '
            'FileVersionRevisionTextBox
            '
            resources.ApplyResources(Me.FileVersionRevisionTextBox, "FileVersionRevisionTextBox")
            Me.FileVersionRevisionTextBox.Name = "FileVersionRevisionTextBox"
            '
            'GuidTextBox
            '
            resources.ApplyResources(Me.GuidTextBox, "GuidTextBox")
            Me.GuidTextBox.Name = "GuidTextBox"
            '
            'GuidLabel
            '
            resources.ApplyResources(Me.GuidLabel, "GuidLabel")
            Me.GuidLabel.Name = "GuidLabel"
            '
            'NeutralLanguageComboBox
            '
            Me.NeutralLanguageComboBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Append
            Me.NeutralLanguageComboBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems
            resources.ApplyResources(Me.NeutralLanguageComboBox, "NeutralLanguageComboBox")
            Me.NeutralLanguageComboBox.FormattingEnabled = True
            Me.NeutralLanguageComboBox.Name = "NeutralLanguageComboBox"
            Me.NeutralLanguageComboBox.Sorted = True
            '
            'AssemblyVersionLayoutPanel
            '
            resources.ApplyResources(Me.AssemblyVersionLayoutPanel, "AssemblyVersionLayoutPanel")
            Me.AssemblyVersionLayoutPanel.Controls.Add(Me.AssemblyVersionMajorTextBox, 0, 0)
            Me.AssemblyVersionLayoutPanel.Controls.Add(Me.AssemblyVersionMinorTextBox, 1, 0)
            Me.AssemblyVersionLayoutPanel.Controls.Add(Me.AssemblyVersionBuildTextBox, 2, 0)
            Me.AssemblyVersionLayoutPanel.Controls.Add(Me.AssemblyVersionRevisionTextBox, 3, 0)
            Me.AssemblyVersionLayoutPanel.Name = "AssemblyVersionLayoutPanel"
            '
            'FileVersionLayoutPanel
            '
            resources.ApplyResources(Me.FileVersionLayoutPanel, "FileVersionLayoutPanel")
            Me.FileVersionLayoutPanel.Controls.Add(Me.FileVersionMajorTextBox, 0, 0)
            Me.FileVersionLayoutPanel.Controls.Add(Me.FileVersionMinorTextBox, 1, 0)
            Me.FileVersionLayoutPanel.Controls.Add(Me.FileVersionBuildTextBox, 2, 0)
            Me.FileVersionLayoutPanel.Controls.Add(Me.FileVersionRevisionTextBox, 3, 0)
            Me.FileVersionLayoutPanel.Name = "FileVersionLayoutPanel"
            '
            'AssemblyInfoPropPage
            '
            resources.ApplyResources(Me, "$this")
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.Controls.Add(Me.overarchingTableLayoutPanel)
            Me.Name = "AssemblyInfoPropPage"
            Me.overarchingTableLayoutPanel.ResumeLayout(False)
            Me.overarchingTableLayoutPanel.PerformLayout()
            Me.AssemblyVersionLayoutPanel.ResumeLayout(False)
            Me.AssemblyVersionLayoutPanel.PerformLayout()
            Me.FileVersionLayoutPanel.ResumeLayout(False)
            Me.FileVersionLayoutPanel.PerformLayout()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub

#End Region

        Protected Overrides ReadOnly Property ControlData() As PropertyControlData()
            Get
                If m_ControlData Is Nothing Then

                    Dim datalist As List(Of PropertyControlData) = New List(Of PropertyControlData)
                    Dim data As PropertyControlData = New PropertyControlData(VsProjPropId80.VBPROJPROPID_AssemblyTitle, "Title", Me.Title, ControlDataFlags.PersistedInAssemblyInfoFile, New Control() {Me.TitleLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId80.VBPROJPROPID_AssemblyDescription, "Description", Me.Description, ControlDataFlags.PersistedInAssemblyInfoFile, New Control() {Me.DescriptionLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId80.VBPROJPROPID_AssemblyCompany, "Company", Me.Company, ControlDataFlags.PersistedInAssemblyInfoFile, New Control() {Me.CompanyLabel})
                    datalist.Add(data)
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId80.VBPROJPROPID_AssemblyProduct, "Product", Me.Product, ControlDataFlags.PersistedInAssemblyInfoFile, New Control() {Me.ProductLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId80.VBPROJPROPID_AssemblyCopyright, "Copyright", Me.Copyright, ControlDataFlags.PersistedInAssemblyInfoFile, New Control() {Me.CopyrightLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId80.VBPROJPROPID_AssemblyTrademark, "Trademark", Me.Trademark, ControlDataFlags.PersistedInAssemblyInfoFile, New Control() {Me.TrademarkLabel})
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId80.VBPROJPROPID_AssemblyVersion, "AssemblyVersion", Me.AssemblyVersionLayoutPanel, AddressOf VersionSet, AddressOf VersionGet, ControlDataFlags.UserHandledEvents Or ControlDataFlags.PersistedInAssemblyInfoFile, New Control() {Me.AssemblyVersionLabel})
                    data.DisplayPropertyName = SR.GetString(SR.PPG_Property_AssemblyVersion)
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId80.VBPROJPROPID_AssemblyFileVersion, "AssemblyFileVersion", Me.FileVersionLayoutPanel, AddressOf VersionSet, AddressOf VersionGet, ControlDataFlags.UserHandledEvents Or ControlDataFlags.PersistedInAssemblyInfoFile, New Control() {Me.FileVersionLabel})
                    data.DisplayPropertyName = SR.GetString(SR.PPG_Property_AssemblyFileVersion)
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId80.VBPROJPROPID_ComVisible, "ComVisible", Me.ComVisibleCheckBox, ControlDataFlags.PersistedInAssemblyInfoFile)
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId80.VBPROJPROPID_AssemblyGuid, "AssemblyGuid", Me.GuidTextBox, ControlDataFlags.PersistedInAssemblyInfoFile, New Control() {Me.GuidLabel})
                    data.DisplayPropertyName = SR.GetString(SR.PPG_Property_AssemblyGuid)
                    datalist.Add(data)
                    data = New PropertyControlData(VsProjPropId80.VBPROJPROPID_NeutralResourcesLanguage, "NeutralResourcesLanguage", Me.NeutralLanguageComboBox, AddressOf NeutralLanguageSet, AddressOf NeutralLanguageGet, ControlDataFlags.PersistedInAssemblyInfoFile, New Control() {Me.NeutralLanguageLabel})
                    datalist.Add(data)

                    m_ControlData = datalist.ToArray()
                End If
                Return m_ControlData
            End Get
        End Property


        ''' <summary>
        ''' Property get for file or assembly version.
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function VersionGet(ByVal control As System.Windows.Forms.Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            Dim Version As String = Nothing

            If (control Is Me.FileVersionLayoutPanel) Then
                ValidateAssemblyFileVersion(Version)
            Else
                Debug.Assert(control Is Me.AssemblyVersionLayoutPanel)
                ValidateAssemblyVersion(Version)
            End If

            value = Version
            Return True
        End Function


        ''' <summary>
        ''' Property set for either file or assembly version.
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function VersionSet(ByVal control As System.Windows.Forms.Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            Dim Major As String = Nothing, Minor As String = Nothing, Build As String = Nothing, Revision As String = Nothing
            Dim Version As String
            Dim Values As String()

            If PropertyControlData.IsSpecialValue(value) Then
                Version = ""
            Else
                Version = Trim(CStr(value))
            End If

            If Version <> "" Then
                'Dim VersionAttr As AssemblyVersionAttribute = New AssemblyVersionAttribute(Version)
                Values = Split(Version, ".")
            End If
            'Enforce 4 values 1.2.3.4
            ReDim Preserve Values(3)

            Dim Textboxes As System.Windows.Forms.TextBox()
            If (control Is Me.FileVersionLayoutPanel) Then
                Textboxes = Me.m_FileVersionTextBoxes
            Else
                Debug.Assert(control Is Me.AssemblyVersionLayoutPanel)
                Textboxes = Me.m_AssemblyVersionTextBoxes
            End If
            For index As Integer = 0 To 3
                Textboxes(index).Text = Values(index)
            Next
            Return True
        End Function


        ''' <summary>
        ''' Validates the version numbers entered into the given textboxes from the user.
        ''' </summary>
        ''' <param name="VersionTextboxes">The textboxes containing the version parts.</param>
        ''' <param name="MaxVersionPartValue">The maximum value allowed for each individual version part.</param>
        ''' <param name="PropertyName">The (localized) name of the property that is being validated.  Used for error messages.</param>
        ''' <param name="WildcardsAllowed">Whether or not wildcards are allowed.</param>
        ''' <param name="Version">[Out] the resulting combined version string, if valid.</param>
        ''' <remarks></remarks>
        Private Sub ValidateVersion(ByVal VersionTextboxes As System.Windows.Forms.TextBox(), ByVal MaxVersionPartValue As UInteger, ByVal PropertyName As String, ByVal WildcardsAllowed As Boolean, ByRef version As String)
            InternalParseVersion(VersionTextboxes(0).Text, _
                VersionTextboxes(1).Text, _
                VersionTextboxes(2).Text, _
                VersionTextboxes(3).Text, _
                PropertyName, _
                MaxVersionPartValue, _
                WildcardsAllowed, version)
        End Sub


        ''' <summary>
        ''' Validates the version numbers entered into the assembly version textboxes from the user.
        ''' </summary>
        ''' <param name="Version">[Out] the resulting combined version string, if valid.</param>
        ''' <remarks></remarks>
        Private Sub ValidateAssemblyVersion(ByRef Version As String)
            ValidateVersion(Me.m_AssemblyVersionTextBoxes, MaxAssemblyVersionPartValue, SR.GetString(SR.PPG_Property_AssemblyVersion), True, Version)
        End Sub


        ''' <summary>
        ''' Validates the version numbers entered into the assembly version textboxes from the user.
        ''' </summary>
        ''' <param name="Version">[Out] the resulting combined version string, if valid.</param>
        ''' <remarks></remarks>
        Private Sub ValidateAssemblyFileVersion(ByRef Version As String)
            ValidateVersion(Me.m_FileVersionTextBoxes, MaxFileVersionPartValue, SR.GetString(SR.PPG_Property_AssemblyFileVersion), False, Version)
        End Sub


        ''' <summary>
        ''' Returns true iff the given string value is a valid numeric part of a version.  I.e., 
        '''   all digits must be numeric and the range must not be exceeded.
        ''' </summary>
        ''' <param name="Value">The value (as a string) to validate.</param>
        ''' <param name="MaxValue">The maximum value allowable for the value.</param>
        ''' <returns>True if Value is valid.</returns>
        ''' <remarks></remarks>
        Private Function ValidateIsNumericVersionPart(ByVal Value As String, ByVal MaxValue As UInteger) As Boolean
            Dim numericValue As UInteger

            'Must be valid unsigned integer.
            If Not UInteger.TryParse(Value, numericValue) Then
                Return False
            End If

            If numericValue > MaxValue Then
                Return False
            End If

            Return True
        End Function


        ''' <summary>
        ''' Parses a version from separated string values into a combined string value for the project system.
        ''' </summary>
        ''' <param name="Major">Major version to parse (as string).</param>
        ''' <param name="Minor">Minor version to parse (as string).</param>
        ''' <param name="Build">Build version to parse (as string).</param>
        ''' <param name="Revision">Revision version to parse (as string).</param>
        ''' <param name="PropertyName">The (localized) name of the property that is being validated.  Used for error messages.</param>
        ''' <param name="MaxVersionPartValue">Maximum value of each part of the version.</param>
        ''' <param name="WildcardsAllowed">Whether or not wildcards are allowed.</param>
        ''' <param name="FormattedVersion">[out] The resulting combined version string.</param>
        ''' <remarks></remarks>
        Private Sub InternalParseVersion(ByVal Major As String, ByVal Minor As String, ByVal Build As String, ByVal Revision As String, ByVal PropertyName As String, ByVal MaxVersionPartValue As UInteger, ByVal WildcardsAllowed As Boolean, ByRef FormattedVersion As String)
            Major = Trim(Major)
            Minor = Trim(Minor)
            Build = Trim(Build)
            Revision = Trim(Revision)

            Dim Fields As String() = New String() {Major, Minor, Build, Revision}
            Dim CombinedVersion As String = String.Join(".", Fields)
            Dim IsValid As Boolean = True

            'Remove extra trailing '.'s
            Do While (CombinedVersion.Length > 0) AndAlso (CombinedVersion.Chars(CombinedVersion.Length - 1) = "."c)
                CombinedVersion = CombinedVersion.Substring(0, CombinedVersion.Length - 1)
            Loop

            Fields = CombinedVersion.Split("."c)

            If Fields.Length > 4 Then
                IsValid = False 'Too many fields (the user puts periods into a cell)
            ElseIf Fields.Length = 1 AndAlso Fields(0) = "" Then
                'All fields blank - this is legal

                '... but unfortunately for Whidbey the DTE project properties don't allow empty because the 
                '  attribute doesn't allow empty, and the project properties code doesn't handle removing the
                '  attribute if it's empty.  So we have to disallow this for now (work-around is to edit the
                '  AssemblyInfo.{vb,cs,js} file manually if you really need this (usually it won't be an issue).
                IsValid = False
            Else
                'The following are the only allowed patterns:
                '  X
                '  X.X
                '  X.X.*
                '  X.X.X
                '  X.X.X.*
                '  X.X.X.X
                '
                'The fields which allow wildcards are passed in, so we only need to validate the following:


                Dim AsteriskFound As Boolean = False
                For Field As Integer = 0 To Fields.Length - 1
                    If AsteriskFound Then
                        'If we previously found an asterisk, additional fields are not allowed
                        IsValid = False
                    End If

                    If Fields(Field) = "*" Then
                        AsteriskFound = True

                        'Verify an asterisk was allowed in that field                        
                        Select Case Field
                            Case 0, 1
                                'Wildcards never allowed in this field
                                Throw New ArgumentException(SR.GetString(SR.PPG_AssemblyInfo_BadWildcard))
                            Case 2, 3
                                If Not WildcardsAllowed Then
                                    Throw New ArgumentException(SR.GetString(SR.PPG_AssemblyInfo_BadWildcard))
                                End If
                            Case Else
                                Debug.Fail("Unexpected case")
                                IsValid = False
                        End Select
                    Else
                        'If not an asterisk, it had better be numeric in the accepted range
                        If Not ValidateIsNumericVersionPart(Fields(Field), MaxVersionPartValue) Then
                            Throw New ArgumentException(SR.GetString(SR.PPG_AssemblyInfo_VersionOutOfRange_2Args, PropertyName, CStr(MaxVersionPartValue)))
                        End If
                    End If
                Next
            End If

            If IsValid Then
                FormattedVersion = CombinedVersion
            Else
                Throw New ArgumentException(SR.GetString(SR.PPG_AssemblyInfo_InvalidVersion))
            End If

        End Sub


#Region "Neutral Language Combobox"

        ''' <summary>
        ''' Populate the neutral language combobox with cultures
        ''' </summary>
        ''' <remarks></remarks>
        Private Sub PopulateNeutralLanguageComboBox()
            'The list of cultures can't change on us, no reason to
            '  re-populate every time it's dropped down.
            If NeutralLanguageComboBox.Items.Count = 0 Then
                Using New WaitCursor
                    'First, the "None" entry
                    NeutralLanguageComboBox.Items.Add(m_NeutralLanguageNoneText)

                    'Followed by all possible cultures
                    Dim AllCultures As CultureInfo() = CultureInfo.GetCultures(CultureTypes.NeutralCultures Or CultureTypes.SpecificCultures Or CultureTypes.InstalledWin32Cultures)
                    For Each Culture As CultureInfo In AllCultures
                        NeutralLanguageComboBox.Items.Add(Culture.DisplayName)
                    Next
                End Using
            End If
        End Sub

        ''' <summary>
        ''' Occurs when the neutral language combobox is dropped down.  Use this to
        '''   populate it with entries.
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Private Sub NeutralLanguageComboBox_DropDown(ByVal sender As Object, ByVal e As System.EventArgs) Handles NeutralLanguageComboBox.DropDown
            PopulateNeutralLanguageComboBox()
            Common.SetComboBoxDropdownWidth(NeutralLanguageComboBox)
        End Sub

        ''' <summary>
        ''' Converts a value for neutral language into the display string used in the
        '''   combobox.
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function NeutralLanguageSet(ByVal control As System.Windows.Forms.Control, ByVal prop As PropertyDescriptor, ByVal value As Object) As Boolean
            'Value is the abbreviation of a culture, e.g. "de-ch"
            If PropertyControlData.IsSpecialValue(value) Then
                NeutralLanguageComboBox.SelectedIndex = -1
            Else
                Dim SelectedText As String = ""
                Dim LanguageAbbrev As String = CStr(value)
                Dim Culture As CultureInfo = Nothing
                If LanguageAbbrev = "" Then
                    SelectedText = m_NeutralLanguageNoneText
                Else
                    Try
                        Culture = CultureInfo.GetCultureInfo(LanguageAbbrev)
                        SelectedText = Culture.DisplayName
                    Catch ex As ArgumentException
                        SelectedText = LanguageAbbrev
                    End Try
                End If

                NeutralLanguageComboBox.Text = SelectedText
            End If
            Return True
        End Function


        ''' <summary>
        ''' Convert the value displayed in the neutral language combobox into the string format to actually
        '''   be stored in the project.
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function NeutralLanguageGet(ByVal control As System.Windows.Forms.Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            If NeutralLanguageComboBox.SelectedIndex < 0 Then
                'Nothing selected, return the typed-in text - we will try to accept it as is
                '  (i.e., they might have entered just a culture abbrevation, such as "de-ch", and
                '  we will accept it if it's valid)
                value = NeutralLanguageComboBox.Text
            Else
                Dim DisplayName As String = DirectCast(NeutralLanguageComboBox.SelectedItem, String)
                If DisplayName = "" OrElse DisplayName.Equals(m_NeutralLanguageNoneText, StringComparison.CurrentCultureIgnoreCase) Then
                    '"None"
                    value = ""
                Else
                    value = Nothing
                    For Each Culture As CultureInfo In CultureInfo.GetCultures(CultureTypes.NeutralCultures Or CultureTypes.SpecificCultures Or CultureTypes.InstalledWin32Cultures)
                        If Culture.DisplayName.Equals(DisplayName, StringComparison.CurrentCultureIgnoreCase) Then
                            value = Culture.Name
                            Exit For
                        End If
                    Next
                    If value Is Nothing Then
                        'Not recognized, return the typed-in text
                        Debug.Fail("How is the selected text not recognized as a culture when we put it into the combobox ourselves?")
                        value = NeutralLanguageComboBox.Text 'defensive
                    End If
                End If
            End If

            Return True
        End Function


#End Region

        Protected Overrides Function GetF1HelpKeyword() As String
            Return HelpKeywords.VBProjPropAssemblyInfo
        End Function

        Private Sub AssemblyVersionLayoutPanel_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles AssemblyVersionMajorTextBox.TextChanged, AssemblyVersionMinorTextBox.TextChanged, AssemblyVersionBuildTextBox.TextChanged, AssemblyVersionRevisionTextBox.TextChanged
            SetDirty(Me.AssemblyVersionLayoutPanel, False)
        End Sub

        Private Sub FileVersionLayoutPanel_TextChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles FileVersionMajorTextBox.TextChanged, FileVersionMinorTextBox.TextChanged, FileVersionBuildTextBox.TextChanged, FileVersionRevisionTextBox.TextChanged
            SetDirty(Me.FileVersionLayoutPanel, False)
        End Sub

        ''' <summary>
        ''' Validation properties
        ''' </summary>
        ''' <param name="controlData"></param>
        ''' <param name="message"></param>
        ''' <param name="returnControl"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overrides Function ValidateProperty(ByVal controlData As PropertyControlData, ByRef message As String, ByRef returnControl As System.Windows.Forms.Control) As ValidationResult
            If controlData.FormControl Is GuidTextBox Then
                Try
                    Dim guid As New Guid(Trim(GuidTextBox.Text))
                Catch e As FormatException
                    message = SR.GetString(SR.PPG_Application_BadGuid)
                    Return ValidationResult.Failed
                End Try
            ElseIf controlData.FormControl Is AssemblyVersionLayoutPanel Then
                Try
                    Dim Version As String = Nothing
                    ValidateAssemblyVersion(Version)
                Catch ex As ArgumentException
                    message = ex.Message
                    returnControl = m_AssemblyVersionTextBoxes(0)
                    Return ValidationResult.Failed
                End Try
            ElseIf controlData.FormControl Is FileVersionLayoutPanel Then
                Try
                    Dim Version As String = Nothing
                    ValidateAssemblyFileVersion(Version)
                Catch ex As ArgumentException
                    message = ex.Message
                    returnControl = m_FileVersionTextBoxes(0)
                    Return ValidationResult.Failed
                End Try
            End If
            Return ValidationResult.Succeeded
        End Function

    End Class

End Namespace
