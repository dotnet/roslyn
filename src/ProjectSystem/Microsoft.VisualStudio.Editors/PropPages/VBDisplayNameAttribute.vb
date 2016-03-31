Imports System.ComponentModel

Namespace Microsoft.VisualStudio.Editors.PropertyPages

    ''' <Summary>
    '''  a sub class of DisplayNameAttribute to help localizating the property name...
    ''' </Summary>
    <AttributeUsage(AttributeTargets.All)> _
    Friend Class VBDisplayNameAttribute
        Inherits DisplayNameAttribute

        Private replaced As Boolean

        Public Sub New(ByVal description As String)
            MyBase.New(description)
        End Sub

        Public Overrides ReadOnly Property DisplayName() As String
            Get
                If Not replaced Then
                    replaced = True
                    DisplayNameValue = SR.ResourceManager.GetString(MyBase.DisplayNameValue)
                End If
                Return DisplayNameValue
            End Get
        End Property
    End Class

End Namespace

