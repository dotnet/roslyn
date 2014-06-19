Imports Roslyn.Utilities

Namespace Roslyn.Compilers.VisualBasic

    ''' <summary> 
    ''' Represents the value of #r reference along with its source location.
    ''' </summary>    
    Friend Structure ReferenceDirective
        Public ReadOnly File As String
        Public ReadOnly Location As SourceLocation

        Public Sub New(file As String, location As SourceLocation)
            Contract.Assert(file IsNot Nothing)
            Contract.Assert(location IsNot Nothing)

            file = file
            Location = location
        End Sub
    End Structure
End Namespace