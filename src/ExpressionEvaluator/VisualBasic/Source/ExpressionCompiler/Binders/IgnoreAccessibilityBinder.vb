Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator
    Friend NotInheritable Class IgnoreAccessibilityBinder
        Inherits Binder

        Public Sub New(containingBinder As Binder)
            MyBase.New(containingBinder)
        End Sub

        Friend Overrides Function BinderSpecificLookupOptions(options As LookupOptions) As LookupOptions
            Return ContainingBinder.BinderSpecificLookupOptions(options) Or LookupOptions.IgnoreAccessibility
        End Function
    End Class
End Namespace
