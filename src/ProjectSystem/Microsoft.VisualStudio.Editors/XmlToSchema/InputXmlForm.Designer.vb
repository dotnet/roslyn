Namespace Microsoft.VisualStudio.Editors.XmlToSchema
    <Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
    Partial Class InputXmlForm
        'Inherits System.Windows.Forms.Form
        Inherits Microsoft.VisualStudio.Editors.XmlToSchema.XmlToSchemaForm

        'Form overrides dispose to clean up the component list.
        <System.Diagnostics.DebuggerNonUserCode()> _
        Protected Overrides Sub Dispose(ByVal disposing As Boolean)
            Try
                If disposing AndAlso components IsNot Nothing Then
                    components.Dispose()
                End If
            Finally
                MyBase.Dispose(disposing)
            End Try
        End Sub

        'Required by the Windows Form Designer
        Private components As System.ComponentModel.IContainer

        'NOTE: The following procedure is required by the Windows Form Designer
        'It can be modified using the Windows Form Designer.  
        'Do not modify it using the code editor.
        <System.Diagnostics.DebuggerStepThrough()> _
        Private Sub InitializeComponent()
            Me.components = New System.ComponentModel.Container
            Dim _imageList1 As System.Windows.Forms.ImageList
            Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(InputXmlForm))
            Dim _cancelButton As System.Windows.Forms.Button
            Dim ColumnHeader1 As System.Windows.Forms.ColumnHeader
            Dim ColumnHeader2 As System.Windows.Forms.ColumnHeader
            Dim TableLayoutPanel1 As System.Windows.Forms.TableLayoutPanel
            Me._listView = New System.Windows.Forms.ListView
            Me._addAsTextButton = New System.Windows.Forms.Button
            Me._picutreBox = New System.Windows.Forms.PictureBox
            Me.Label1 = New System.Windows.Forms.Label
            Me._addFromFileButton = New System.Windows.Forms.Button
            Me._addFromWebButton = New System.Windows.Forms.Button
            Me.FlowLayoutPanel1 = New System.Windows.Forms.FlowLayoutPanel
            Me._okButton = New System.Windows.Forms.Button
            Me._imageList2 = New System.Windows.Forms.ImageList(Me.components)
            Me._xmlFileDialog = New System.Windows.Forms.OpenFileDialog
            _imageList1 = New System.Windows.Forms.ImageList(Me.components)
            _cancelButton = New System.Windows.Forms.Button
            ColumnHeader1 = New System.Windows.Forms.ColumnHeader
            ColumnHeader2 = New System.Windows.Forms.ColumnHeader
            TableLayoutPanel1 = New System.Windows.Forms.TableLayoutPanel
            TableLayoutPanel1.SuspendLayout()
            CType(Me._picutreBox, System.ComponentModel.ISupportInitialize).BeginInit()
            Me.FlowLayoutPanel1.SuspendLayout()
            Me.SuspendLayout()
            '
            '_imageList1
            '
            _imageList1.ImageStream = CType(resources.GetObject("_imageList1.ImageStream"), System.Windows.Forms.ImageListStreamer)
            _imageList1.TransparentColor = System.Drawing.Color.Black
            _imageList1.Images.SetKeyName(0, "openHS.bmp")
            _imageList1.Images.SetKeyName(1, "EditCodeHS.bmp")
            _imageList1.Images.SetKeyName(2, "WebInsertHyperlinkHS.bmp")
            '
            '_cancelButton
            '
            resources.ApplyResources(_cancelButton, "_cancelButton")
            _cancelButton.BackColor = System.Drawing.SystemColors.ButtonFace
            _cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel
            _cancelButton.Name = "_cancelButton"
            _cancelButton.UseVisualStyleBackColor = True
            '
            'ColumnHeader1
            '
            resources.ApplyResources(ColumnHeader1, "ColumnHeader1")
            '
            'ColumnHeader2
            '
            resources.ApplyResources(ColumnHeader2, "ColumnHeader2")
            '
            'TableLayoutPanel1
            '
            resources.ApplyResources(TableLayoutPanel1, "TableLayoutPanel1")
            TableLayoutPanel1.Controls.Add(Me._listView, 0, 1)
            TableLayoutPanel1.Controls.Add(Me._addAsTextButton, 2, 3)
            TableLayoutPanel1.Controls.Add(Me._picutreBox, 0, 0)
            TableLayoutPanel1.Controls.Add(Me.Label1, 1, 0)
            TableLayoutPanel1.Controls.Add(Me._addFromFileButton, 2, 1)
            TableLayoutPanel1.Controls.Add(Me._addFromWebButton, 2, 2)
            TableLayoutPanel1.Controls.Add(Me.FlowLayoutPanel1, 1, 5)
            TableLayoutPanel1.Name = "TableLayoutPanel1"
            '
            '_listView
            '
            Me._listView.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {ColumnHeader1, ColumnHeader2})
            TableLayoutPanel1.SetColumnSpan(Me._listView, 2)
            resources.ApplyResources(Me._listView, "_listView")
            Me._listView.FullRowSelect = True
            Me._listView.GridLines = True
            Me._listView.MultiSelect = False
            Me._listView.Name = "_listView"
            TableLayoutPanel1.SetRowSpan(Me._listView, 4)
            Me._listView.Sorting = System.Windows.Forms.SortOrder.Ascending
            Me._listView.UseCompatibleStateImageBehavior = False
            Me._listView.View = System.Windows.Forms.View.Details
            '
            '_addAsTextButton
            '
            resources.ApplyResources(Me._addAsTextButton, "_addAsTextButton")
            Me._addAsTextButton.ImageList = _imageList1
            Me._addAsTextButton.Name = "_addAsTextButton"
            Me._addAsTextButton.UseVisualStyleBackColor = True
            '
            '_picutreBox
            '
            Me._picutreBox.BackColor = System.Drawing.SystemColors.Window
            resources.ApplyResources(Me._picutreBox, "_picutreBox")
            Me._picutreBox.Name = "_picutreBox"
            Me._picutreBox.TabStop = False
            '
            'Label1
            '
            resources.ApplyResources(Me.Label1, "Label1")
            Me.Label1.BackColor = System.Drawing.SystemColors.Window
            TableLayoutPanel1.SetColumnSpan(Me.Label1, 2)
            Me.Label1.Name = "Label1"
            '
            '_addFromFileButton
            '
            resources.ApplyResources(Me._addFromFileButton, "_addFromFileButton")
            Me._addFromFileButton.ImageList = _imageList1
            Me._addFromFileButton.Name = "_addFromFileButton"
            Me._addFromFileButton.UseVisualStyleBackColor = True
            '
            '_addFromWebButton
            '
            resources.ApplyResources(Me._addFromWebButton, "_addFromWebButton")
            Me._addFromWebButton.ImageList = _imageList1
            Me._addFromWebButton.Name = "_addFromWebButton"
            Me._addFromWebButton.UseVisualStyleBackColor = True
            '
            'FlowLayoutPanel1
            '
            resources.ApplyResources(Me.FlowLayoutPanel1, "FlowLayoutPanel1")
            TableLayoutPanel1.SetColumnSpan(Me.FlowLayoutPanel1, 2)
            Me.FlowLayoutPanel1.Controls.Add(Me._okButton)
            Me.FlowLayoutPanel1.Controls.Add(_cancelButton)
            Me.FlowLayoutPanel1.Name = "FlowLayoutPanel1"
            '
            '_okButton
            '
            resources.ApplyResources(Me._okButton, "_okButton")
            Me._okButton.BackColor = System.Drawing.SystemColors.ButtonFace
            Me._okButton.DialogResult = System.Windows.Forms.DialogResult.OK
            Me._okButton.Name = "_okButton"
            Me._okButton.UseVisualStyleBackColor = True
            '
            '_imageList2
            '
            Me._imageList2.ImageStream = CType(resources.GetObject("_imageList2.ImageStream"), System.Windows.Forms.ImageListStreamer)
            Me._imageList2.TransparentColor = System.Drawing.Color.Black
            Me._imageList2.Images.SetKeyName(0, "XmlSchema.bmp")
            '
            '_xmlFileDialog
            '
            resources.ApplyResources(Me._xmlFileDialog, "_xmlFileDialog")
            Me._xmlFileDialog.Multiselect = True
            '
            'InputXmlForm
            '
            Me.AcceptButton = Me._okButton
            Me.AccessibleRole = System.Windows.Forms.AccessibleRole.Dialog
            resources.ApplyResources(Me, "$this")
            Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
            Me.CancelButton = _cancelButton
            Me.Controls.Add(TableLayoutPanel1)
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.Name = "InputXmlForm"
            Me.ShowIcon = False
            Me.ShowInTaskbar = False
            TableLayoutPanel1.ResumeLayout(False)
            TableLayoutPanel1.PerformLayout()
            CType(Me._picutreBox, System.ComponentModel.ISupportInitialize).EndInit()
            Me.FlowLayoutPanel1.ResumeLayout(False)
            Me.FlowLayoutPanel1.PerformLayout()
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub
        Friend WithEvents _okButton As System.Windows.Forms.Button
        Friend WithEvents _addFromWebButton As System.Windows.Forms.Button
        Friend WithEvents _addAsTextButton As System.Windows.Forms.Button
        Friend WithEvents _listView As System.Windows.Forms.ListView
        Friend WithEvents _xmlFileDialog As System.Windows.Forms.OpenFileDialog
        Friend WithEvents _picutreBox As System.Windows.Forms.PictureBox
        Friend WithEvents Label1 As System.Windows.Forms.Label
        Friend WithEvents _imageList2 As System.Windows.Forms.ImageList
        Friend WithEvents _addFromFileButton As System.Windows.Forms.Button
        Friend WithEvents FlowLayoutPanel1 As System.Windows.Forms.FlowLayoutPanel
    End Class
End Namespace