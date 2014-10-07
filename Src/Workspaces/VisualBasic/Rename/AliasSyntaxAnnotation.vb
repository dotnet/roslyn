Namespace Microsoft.CodeAnalysis.VisualBasic.Rename
    <Serializable()> Friend Class AliasSyntaxAnnotation
        Inherits SyntaxAnnotation

        Public ReadOnly AliasName As String

        Sub New(aliasName As String)
            Me.AliasName = aliasName
        End Sub
    End Class
End Namespace

