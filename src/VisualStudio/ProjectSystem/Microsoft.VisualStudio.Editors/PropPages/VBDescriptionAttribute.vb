Imports System
Imports System.ComponentModel
Imports System.Diagnostics
Imports Microsoft.VisualStudio.Editors

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <Summary>
    '''  a sub class of DescriptionAttribute to help localizating the description...
    ''' </Summary>
    <AttributeUsage(AttributeTargets.All)> _
    Friend Class VBDescriptionAttribute
        Inherits DescriptionAttribute

        Private replaced As Boolean

        Public Sub New(ByVal description As String)
            MyBase.New(description)
        End Sub

        Public Overrides ReadOnly Property Description() As String
            Get
                If Not replaced Then
                    replaced = True
                    DescriptionValue = SR.ResourceManager.GetString(MyBase.DescriptionValue)
                End If
                Return DescriptionValue
            End Get
        End Property
    End Class

End Namespace

