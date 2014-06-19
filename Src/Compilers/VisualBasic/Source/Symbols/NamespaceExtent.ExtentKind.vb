Imports System.Collections.Generic
Imports System.Threading
Imports Roslyn.Compilers.Internal

Namespace Roslyn.Compilers.VisualBasic

    Partial Structure NamespaceExtent

        ''' <summary>
        ''' Describes the kind of the namespace extent.
        ''' </summary>
        Public Enum ExtentKind
            [Module]
            Assembly
            Compilation
        End Enum

    End Structure

End Namespace