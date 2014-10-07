Imports System.Linq
Imports System.Runtime.CompilerServices
Imports System.Text
Imports System.Threading
Imports Roslyn.Compilers
Imports Roslyn.Compilers.Common
Imports Roslyn.Compilers.Internal
Imports Roslyn.Compilers.VisualBasic
Imports Roslyn.Services.Shared.Utilities

Namespace Roslyn.Services.VisualBasic.Extensions
    Friend Module ConversionExtensions
        <Extension()>
        Public Function IsIdentityOrWidening(conversion As Conversion) As Boolean
            Return conversion.IsIdentity OrElse conversion.IsWidening
        End Function
    End Module
End Namespace