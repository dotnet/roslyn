Namespace Roslyn.Compilers.VisualBasic
    ''' <summary>
    ''' Represents the possible compilation stages for which it is possible to get diagnostics (errors).
    ''' </summary>
    Friend Enum CompilationStage
        Parse
        [Declare]
        Compile
        Emit
    End Enum
End Namespace