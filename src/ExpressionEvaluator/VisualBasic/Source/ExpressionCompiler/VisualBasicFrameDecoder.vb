Imports Microsoft.CodeAnalysis.ExpressionEvaluator

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    <DkmReportNonFatalWatsonException(ExcludeExceptionType:=GetType(NotImplementedException)), DkmContinueCorruptingException>
    Friend NotInheritable Class VisualBasicFrameDecoder : Inherits FrameDecoder

        Public Sub New()
            MyBase.New(VisualBasicInstructionDecoder.Instance)
        End Sub

    End Class

End Namespace
