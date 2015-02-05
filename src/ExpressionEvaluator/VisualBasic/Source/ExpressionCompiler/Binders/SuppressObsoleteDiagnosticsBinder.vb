Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

#Disable Warning RS0010
    ''' <summary>
    ''' Causes all diagnostics related to <see cref="ObsoleteAttribute"/>
    ''' and <see cref="T:Windows.Foundation.MetadataDeprecatedAttribute"/> 
    ''' to be suppressed.
    ''' </summary>
    Friend NotInheritable Class SuppressObsoleteDiagnosticsBinder
#Enable Warning RS0010
        Inherits Binder

        Public Sub New(containingBinder As Binder)
            MyBase.New(containingBinder)
        End Sub

        Friend Overrides ReadOnly Property SuppressObsoleteDiagnostics As Boolean
            Get
                Return True
            End Get
        End Property
    End Class

End Namespace