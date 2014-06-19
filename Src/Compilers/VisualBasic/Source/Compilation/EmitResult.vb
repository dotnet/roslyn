Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Emit

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    ''' <summary> 
    ''' The result of the Compilation Emit operation. 
    ''' </summary>    
    Public NotInheritable Class EmitResult
        Inherits CommonEmitResult

        Private ReadOnly m_diagnostics As ImmutableArray(Of Diagnostic)

        Public Overrides ReadOnly Property Diagnostics As ImmutableArray(Of Diagnostic)
            Get
                Return m_diagnostics
            End Get
        End Property



        Friend Sub New(success As Boolean, diagnostics As ImmutableArray(Of Diagnostic), baseline As EmitBaseline)
            MyBase.New(success, baseline)

            m_diagnostics = diagnostics
        End Sub
    End Class

End Namespace

