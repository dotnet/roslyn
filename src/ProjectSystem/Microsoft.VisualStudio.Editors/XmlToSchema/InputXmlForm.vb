Option Infer On

Imports System
Imports System.ComponentModel.Design
Imports System.Collections.Generic
Imports System.IO
Imports System.Xml
Imports System.Xml.Linq
Imports System.Xml.Schema
Imports System.Windows.Forms
Imports System.Windows.Forms.Design
Imports Microsoft.VisualBasic

Namespace Microsoft.VisualStudio.Editors.XmlToSchema

    Friend NotInheritable Class InputXmlForm
        Private ReadOnly _project As EnvDTE.Project
        Private ReadOnly _projectPath As String
        Private ReadOnly _schemaFileName As String

        Public Sub New(ByVal project As EnvDTE.Project, ByVal projectPath As String, ByVal schemaFileName As String)
            MyBase.New(Nothing)

            InitializeComponent()
            _project = project
            _projectPath = projectPath
            _schemaFileName = Path.GetFileNameWithoutExtension(schemaFileName)
            If String.IsNullOrEmpty(_schemaFileName) Then
                _schemaFileName = "XmlToSchema"
            End If
            _picutreBox.Image = _imageList2.Images(0)

            VBPackage.IncrementSqmDatapoint(Shell.Interop.VsSqmDataPoint.DATAID_SQM_DP_VB_XMLTOSCHEMAWIZARD_USEWIZARD)
        End Sub

        Protected Overrides Sub ScaleControl(ByVal factor As System.Drawing.SizeF, ByVal specified As System.Windows.Forms.BoundsSpecified)
            'First do standard DPI scaling logic
            MyBase.ScaleControl(factor, specified)

            'Prevent the dialog from getting too big
            Me.MaximumSize = Screen.FromHandle(Me.Handle).WorkingArea.Size
        End Sub

        Private Function ContainsFile(ByVal filePath As String) As Boolean
            Dim fileUri As New Uri(filePath, UriKind.RelativeOrAbsolute)
            For Each item As ListViewItem In _listView.Items
                If Uri.IsWellFormedUriString(item.Text, UriKind.RelativeOrAbsolute) Then
                    Dim itemUri As New Uri(item.SubItems(1).Text, UriKind.RelativeOrAbsolute)
                    If Uri.Compare(fileUri, itemUri, UriComponents.AbsoluteUri, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) = 0 Then
                        Return True
                    End If
                End If
            Next
            Return False
        End Function

        Private Sub AddFile(ByVal filePath As String)
            If Not String.IsNullOrEmpty(filePath) AndAlso Not ContainsFile(filePath) Then
                Dim item As New ListViewItem("File") With {.Tag = filePath}
                item.SubItems.Add(filePath)
                _listView.Items.Add(item)
            End If
        End Sub

        Private Sub _addFromFileButton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles _addFromFileButton.Click
            _xmlFileDialog.InitialDirectory = _projectPath
            If _xmlFileDialog.ShowDialog() = DialogResult.OK Then
                Dim anyInvalid = False
                Try
                    For Each fileName In _xmlFileDialog.FileNames
                        Me.UseWaitCursor = True
                        XElement.Load(fileName)
                    Next
                Catch ex As Exception
                    If FilterException(ex) Then
                        ShowWarning(String.Format(SR.XmlToSchema_ErrorLoadingXml, ex.Message))
                        anyInvalid = True
                    Else
                        Throw
                    End If
                Finally
                    Me.UseWaitCursor = False
                End Try

                If Not anyInvalid Then
                    For Each fileName In _xmlFileDialog.FileNames
                        AddFile(fileName)
                    Next
                End If
            End If

            VBPackage.IncrementSqmDatapoint(Shell.Interop.VsSqmDataPoint.DATAID_SQM_DP_VB_XMLTOSCHEMAWIZARD_FROMFILE)
        End Sub

        Private Sub _addFromWebButton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles _addFromWebButton.Click
            Using dialog As New WebUrlDialog()
                dialog.ServiceProvider = ServiceProvider
                Dim uiService As IUIService = CType(ServiceProvider.GetService(GetType(IUIService)), IUIService)
                If uiService.ShowDialog(dialog) = DialogResult.OK Then
                    If Not ContainsFile(dialog.Url) Then
                        Dim item As New ListViewItem("URL") With {.Tag = dialog.Xml}
                        item.SubItems.Add(dialog.Url)
                        _listView.Items.Add(item)
                    End If
                End If

                VBPackage.IncrementSqmDatapoint(Shell.Interop.VsSqmDataPoint.DATAID_SQM_DP_VB_XMLTOSCHEMAWIZARD_FROMWEB)
            End Using
        End Sub

        Private Sub _addAsTextButton_Click(ByVal sender As Object, ByVal e As EventArgs) Handles _addAsTextButton.Click
            Using dialog As New PasteXmlDialog()
                dialog.ServiceProvider = ServiceProvider
                Dim uiService As IUIService = CType(ServiceProvider.GetService(GetType(IUIService)), IUIService)
                If uiService.ShowDialog(dialog) = DialogResult.OK Then
                    Dim item As New ListViewItem("XML") With {.Tag = dialog.Xml}
                    Dim xmlText = dialog.Xml.ToString(SaveOptions.DisableFormatting)
                    If xmlText.Length > 128 Then
                        xmlText = xmlText.Substring(0, 128)
                    End If
                    item.SubItems.Add(xmlText)
                    _listView.Items.Add(item)
                End If

                VBPackage.IncrementSqmDatapoint(Shell.Interop.VsSqmDataPoint.DATAID_SQM_DP_VB_XMLTOSCHEMAWIZARD_FROMXML)
            End Using
        End Sub

        <System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")>
        Private Sub _okButtonClick(ByVal sender As Object, ByVal e As EventArgs) Handles _okButton.Click
            If _listView.Items.Count = 0 Then
                Return
            End If
            Try
                Me.UseWaitCursor = True
                Application.DoEvents()

                ' Infer schemas from XML sources.
                Dim schemaSet As New XmlSchemaSet
                Dim infer As New XmlSchemaInference
                For Each item As ListViewItem In _listView.Items
                    Dim element = TryCast(item.Tag, XElement)
                    Using reader = If(element Is Nothing, GetXmlTextReaderWithDtdProcessingProhibited(CStr(item.Tag)), element.CreateReader)

                        infer.InferSchema(reader, schemaSet)
                    End Using
                Next

                ' Add inferred schemas to the project.
                Dim settings As New XmlWriterSettings() With {.Indent = True}
                Dim index As Integer = 0
                For Each schema As XmlSchema In schemaSet.Schemas()
                    ' Find unused file name to save the schema.
                    Dim schemaFilePath As String
                    Do
                        schemaFilePath = Path.Combine(_projectPath, _schemaFileName & If(index > 0, CStr(index), "") & ".xsd")
                        index += 1
                    Loop While File.Exists(schemaFilePath)

                    ' Write inferred schema to the file.
                    Using writer = XmlWriter.Create(schemaFilePath, settings)
                        schema.Write(writer)
                    End Using

                    ' Add schema file to the project.
                    _project.ProjectItems.AddFromFile(schemaFilePath)
                Next
            Catch ex As Exception
                Dim sqmCode = ex.GetType.FullName.GetHashCode
                If sqmCode < 0 Then
                    sqmCode = sqmCode * -1
                End If
                VBPackage.AddSqmItemToStream(Shell.Interop.VsSqmDataPoint.DATAID_SQM_STRM_VB_XMLTOSCHEMAWIZARD_EXCEPTION, CUInt(sqmCode))
                If FilterException(ex) Then
                    ShowWarning(String.Format(SR.XmlToSchema_ErrorInXmlInference, ex.Message))
                Else
                    Throw
                End If
            Finally
                Me.UseWaitCursor = False
            End Try
        End Sub

        Private Function GetXmlTextReaderWithDtdProcessingProhibited(element As String) As XmlTextReader
            Dim reader As XmlTextReader = New XmlTextReader(element)

            ' Required by Fxcop rule CA3054 - DoNotAllowDTDXmlTextReader
            reader.DtdProcessing = DtdProcessing.Prohibit
        End Function

        Private Sub _listViewKeyPress(ByVal o As Object, ByVal e As KeyEventArgs) Handles _listView.KeyDown
            If e.KeyCode = Keys.Delete Then
                Dim toDelete = New List(Of ListViewItem)
                For Each cur As ListViewItem In _listView.SelectedItems
                    toDelete.Add(cur)
                Next

                For Each cur In toDelete
                    _listView.Items.Remove(cur)
                Next
            End If
        End Sub

    End Class

End Namespace
