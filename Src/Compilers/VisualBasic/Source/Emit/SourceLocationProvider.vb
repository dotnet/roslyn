Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Reflection.Emit
Imports System.Runtime.InteropServices
Imports System.Text
Imports Roslyn.Compilers.Internal
Imports Roslyn.Compilers.VisualBasic.Emit

Namespace Roslyn.Compilers.VisualBasic

    Friend Class SourceLocationProvider
        Implements Microsoft.Cci.ISourceLocationProvider

        Public Function GetPrimarySourceLocationsFor(
            locations As Microsoft.Cci.SequencePoint
        ) As IEnumerable(Of Microsoft.Cci.SequencePoint) Implements Microsoft.Cci.ISourceLocationProvider.GetPrimarySourceLocationsFor
            Return {locations}
        End Function

        Public Function GetSourceNameFor(
            localDefinition As Microsoft.Cci.ILocalDefinition,
            <Out()> ByRef isCompilerGenerated As Boolean
        ) As String Implements Microsoft.Cci.ISourceLocationProvider.GetSourceNameFor
            isCompilerGenerated = False
            Return localDefinition.Name
        End Function
    End Class

End Namespace