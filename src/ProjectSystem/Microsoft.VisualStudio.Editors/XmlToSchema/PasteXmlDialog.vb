Imports System.ComponentModel.Design
Imports System.Windows.Forms
Imports System.Xml.Linq

Namespace Microsoft.VisualStudio.Editors.XmlToSchema

    <HelpKeyword("vb.XmlToSchemaWizard")> _
    Friend NotInheritable Class PasteXmlDialog
        Private _xml As XElement
        Public ReadOnly Property Xml() As XElement
            <DebuggerStepThrough()> _
            Get
                Return _xml
            End Get
        End Property

        <System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")> _
        Protected Overrides Sub OnClosing(ByVal e As System.ComponentModel.CancelEventArgs)
            If Me.DialogResult = DialogResult.OK Then
                Try
                    _xml = XElement.Parse(_xmlTextBox.Text)
                Catch ex As Exception
                    If FilterException(ex) Then
                        ShowWarning(String.Format(SR.XmlToSchema_InvalidXMLFormat, ex.Message))
                        e.Cancel = True
                    Else
                        Throw
                    End If
                End Try
            End If
            MyBase.OnClosing(e)
        End Sub
    End Class
End Namespace