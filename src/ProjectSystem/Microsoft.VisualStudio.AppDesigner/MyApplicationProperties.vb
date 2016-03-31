Namespace Microsoft.VisualStudio.Editors.MyApplication

    Public Class MyApplicationPropertiesBase
        ''' <summary>
        ''' Returns the set of files that need to be checked out to change the given property
        ''' Must be overriden in sub-class
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Overridable Function FilesToCheckOut(ByVal CreateIfNotExist As Boolean) As String()
            Return New String() {}
        End Function


    End Class ' Class MyApplicationPropertiesBase

End Namespace
