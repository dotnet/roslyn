Imports System.Diagnostics

Namespace Roslyn.Compilers.VisualBasic

    ''' <summary>
    ''' Assembyl symbol referenced by a AssemblyRef for which we couldn't find a matching 
    ''' compilation reference but we found one that differs in version.
    ''' </summary>
    Friend Structure UnifiedAssembly

        ''' <summary>
        ''' Original reference that was unified to the identity of the <see cref="P:TargetAssembly"/>.
        ''' </summary>
        Friend ReadOnly OriginalReference As AssemblyIdentity

        Friend ReadOnly TargetAssembly As AssemblySymbol

        Public Sub New(targetAssembly As AssemblySymbol, originalReference As AssemblyIdentity)
            Debug.Assert(originalReference IsNot Nothing)
            Debug.Assert(targetAssembly IsNot Nothing)
            Me.OriginalReference = originalReference
            Me.TargetAssembly = targetAssembly
        End Sub
    End Structure
End Namespace
