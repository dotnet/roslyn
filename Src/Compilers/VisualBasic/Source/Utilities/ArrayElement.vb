Imports Roslyn.Compilers.Internal

Namespace Roslyn.Compilers.VisualBasic
    Friend Structure ArrayElement(Of T As Class)
        Friend value As T

        Public Shared Widening Operator CType(element As ArrayElement(Of T)) As T
            Return element.value
        End Operator
    End Structure
End Namespace
