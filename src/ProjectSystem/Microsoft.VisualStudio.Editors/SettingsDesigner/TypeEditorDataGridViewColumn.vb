Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner
    ''' <summary>
    ''' UI Type editor column for DataGridView
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class DataGridViewUITypeEditorColumn
        Inherits DataGridViewColumn

        Public Sub New()
            MyBase.New(New DataGridViewUITypeEditorCell)
        End Sub

        Public Overrides Property CellTemplate() As DataGridViewCell
            Get
                Return MyBase.CellTemplate
            End Get
            Set(ByVal Value As DataGridViewCell)
                If Value IsNot Nothing AndAlso Not TypeOf Value Is DataGridViewUITypeEditorCell Then
                    Throw New InvalidCastException()
                End If

                MyBase.CellTemplate = Value
            End Set
        End Property
    End Class
End Namespace
