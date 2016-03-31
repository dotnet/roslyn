Imports System.Collections.Specialized
Imports System.Drawing.Design

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner

    ''' <summary>
    ''' Since the stringcollection isn't associated with the stringcollectioneditor class, we
    ''' invent our own little editor that uses the stringarrayeditor instead.
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class StringArrayEditorForStringCollections
        Inherits System.Drawing.Design.UITypeEditor

        Private m_parent As UITypeEditor

        ''' <summary>
        ''' Create a new StringArrayEditorForStringCollections
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub New()
            m_parent = DirectCast(System.ComponentModel.TypeDescriptor.GetEditor(GetType(String()), GetType(UITypeEditor)), UITypeEditor)
        End Sub

        ''' <summary>
        ''' Edit value by converting it from a string collection, passing that to the string array editor and
        ''' then 
        ''' </summary>
        ''' <param name="context"></param>
        ''' <param name="provider"></param>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Overrides Function EditValue(ByVal context As System.ComponentModel.ITypeDescriptorContext, ByVal provider As System.IServiceProvider, ByVal value As Object) As Object
            Dim result As Object = m_parent.EditValue(context, provider, ConvertToUITypeEditorSource(value))
            Return ConvertToOriginal(result)
        End Function

#Region "Forwarding UITypeEditor methods to our parent UITypeEditor"
        Public Overrides Function GetEditStyle(ByVal context As System.ComponentModel.ITypeDescriptorContext) As System.Drawing.Design.UITypeEditorEditStyle
            Return m_parent.GetEditStyle(context)
        End Function

        Public Overrides Sub PaintValue(ByVal e As System.Drawing.Design.PaintValueEventArgs)
            m_parent.PaintValue(e)
        End Sub

        Public Overrides ReadOnly Property IsDropDownResizable() As Boolean
            Get
                Return m_parent.IsDropDownResizable()
            End Get
        End Property

        Public Overrides Function GetPaintValueSupported(ByVal context As System.ComponentModel.ITypeDescriptorContext) As Boolean
            Return m_parent.GetPaintValueSupported(context)
        End Function
#End Region
        ''' <summary>
        ''' Convert from StringCollection to string()
        ''' </summary>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function ConvertToUITypeEditorSource(ByVal value As Object) As Object
            If value Is Nothing Then
                Return Nothing
            End If

            If value.GetType().Equals(GetType(StringCollection)) Then
                Dim strCol As StringCollection = DirectCast(value, StringCollection)
                Dim result(strCol.Count - 1) As String
                strCol.CopyTo(result, 0)
                Return result
            End If
            Return value
        End Function

        ''' <summary>
        ''' Convert back from String() to StringCollection
        ''' </summary>
        ''' <param name="value"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Function ConvertToOriginal(ByVal value As Object) As Object
            If value Is Nothing Then
                Return Nothing
            End If

            If value.GetType().Equals(GetType(String())) Then
                Dim strings() As String = DirectCast(value, String())
                Dim result As New StringCollection
                result.AddRange(strings)
                Return result
            End If
            Return value
        End Function
    End Class
End Namespace
