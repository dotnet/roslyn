Imports Microsoft.VisualBasic
Imports System
Imports System.Diagnostics
Imports System.ComponentModel.Design
Imports System.Windows.Forms
Imports System.Drawing
Imports Microsoft.VisualStudio.Editors
Imports Microsoft.VisualStudio.Editors.Common
Imports System.Runtime.InteropServices
Imports System.ComponentModel

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <summary>
    ''' C#/J# application property page - see comments in proppage.vb: "Application property pages (VB, C#, J#)"
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class CSharpApplicationPropPage
        'Inherits System.Windows.Forms.UserControl
        ' If you want to be able to use the forms designer to edit this file,
        ' change the base class from PropPageUserControlBase to UserControl
        Inherits ApplicationPropPage

#Region " Windows Form Designer generated code "

        Public Sub New()
            MyBase.New()

            'This call is required by the Windows Form Designer.
            InitializeComponent()

            'Add any initialization after the InitializeComponent() call
            AddChangeHandlers()
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
        <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
            Dim resources As System.Resources.ResourceManager = New System.Resources.ResourceManager(GetType(ApplicationPropPage))
            CType(Me.AppIconImage, System.ComponentModel.ISupportInitialize).BeginInit()
            Me.SuspendLayout()
            '
            'ApplicationPropPage
            '
            Me.Font = CType(resources.GetObject("$this.Font"), System.Drawing.Font)
            Me.ImeMode = CType(resources.GetObject("$this.ImeMode"), System.Windows.Forms.ImeMode)
            Me.Name = "ApplicationPropPage"
            Me.Size = CType(resources.GetObject("$this.Size"), System.Drawing.Size)
            CType(Me.AppIconImage, System.ComponentModel.ISupportInitialize).EndInit()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub
#End Region

        ''' <summary>
        ''' Populates the start-up object combobox box dropdown
        ''' </summary>
        ''' <param name="PopulateDropdown">If false, only the current text in the combobox is set.  If true, the entire dropdown list is populated.  For performance reasons, False should be used until the user actually drops down the list.</param>
        ''' <remarks></remarks>
        Protected Overrides Sub PopulateStartupObject(ByVal StartUpObjectSupported As Boolean, ByVal PopulateDropdown As Boolean)
            Dim InsideInitSave As Boolean = m_fInsideInit
            m_fInsideInit = True

            Try

                Dim StartupObjectPropertyControlData As PropertyControlData = GetPropertyControlData("StartupObject")

                If Not StartUpObjectSupported OrElse StartupObjectPropertyControlData.IsMissing Then
                    With StartupObject
                        .DropDownStyle = ComboBoxStyle.DropDownList
                        .Items.Clear()
                        .SelectedItem = .Items.Add(SR.GetString(SR.PPG_Application_StartupObjectNotSet))
                        .Text = SR.GetString(SR.PPG_Application_StartupObjectNotSet)
                        .SelectedIndex = 0  '// Set it to NotSet
                    End With

                    If StartupObjectPropertyControlData.IsMissing Then
                        Me.StartupObject.Enabled = False
                        Me.StartupObjectLabel.Enabled = False
                    End If
                Else
                    Dim prop As PropertyDescriptor = StartupObjectPropertyControlData.PropDesc

                    With StartupObject
                            .DropDownStyle = ComboBoxStyle.DropDownList
                            .Items.Clear()

                            ' (Not Set) should always be available in the list
                            .Items.Add(SR.GetString(SR.PPG_Application_StartupObjectNotSet))

                            If PopulateDropdown Then
                                RefreshPropertyStandardValues()

                                'Certain project types may not support standard values
                                If prop.Converter.GetStandardValuesSupported() Then
                                    Switches.TracePDPerf("*** Populating start-up object list from the project [may be slow for a large project]")
                                    Debug.Assert(Not InsideInitSave, "PERFORMANCE ALERT: We shouldn't be populating the start-up object dropdown list during page initialization, it should be done later if needed.")
                                    Using New WaitCursor
                                        For Each str As String In prop.Converter.GetStandardValues()
                                            .Items.Add(str)
                                        Next
                                    End Using
                                End If
                            End If

                            '(Okay to use InitialValue because we checked against IsMissing above)
                            Dim SelectedItemText As String = CStr(StartupObjectPropertyControlData.InitialValue)
                            If IsNothing(SelectedItemText) OrElse (SelectedItemText = "") Then
                                SelectedItemText = SR.GetString(SR.PPG_Application_StartupObjectNotSet)
                            End If

                            .SelectedItem = SelectedItemText
                            If .SelectedItem Is Nothing Then
                                .Items.Add(SelectedItemText)
                                'CONSIDER: Can we use the object returned by .Items.Add to set the selection?
                                .SelectedItem = SelectedItemText
                            End If
                        End With
                    End If
            Finally
                m_fInsideInit = InsideInitSave
            End Try
        End Sub

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="control"></param>
        ''' <param name="prop"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Overrides Function StartupObjectGet(ByVal control As Control, ByVal prop As PropertyDescriptor, ByRef value As Object) As Boolean
            If Not StartUpObjectSupported() Then
                value = ""
            Else
                If StartupObject.SelectedItem IsNot Nothing Then
                    Dim StartupObjectText As String = TryCast(StartupObject.SelectedItem, String)

                    If Not IsNothing(StartupObjectText) Then
                        If String.Compare(StartupObjectText, SR.GetString(SR.PPG_Application_StartupObjectNotSet)) <> 0 Then
                            value = StartupObjectText
                        Else
                            '// the value is (Not Set) so just leave it empty
                            value = ""
                        End If
                    Else
                        value = ""
                    End If
                Else
                    value = ""
                End If
            End If
            Return True
        End Function

    End Class


End Namespace
