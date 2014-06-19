Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports System.Text
Imports System.Threading
Imports Roslyn.Compilers.Internal
Imports Roslyn.Compilers.Internal.Contract

Namespace Roslyn.Compilers.VisualBasic

    Public Enum VarianceKind
        VarianceNone  ' invariant
        VarianceOut   ' "Out" - covariant
        VarianceIn    ' "In" - contravariant
    End Enum

End Namespace