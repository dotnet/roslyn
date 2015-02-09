Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    <DkmReportNonFatalWatsonException(ExcludeExceptionType:=GetType(NotImplementedException)), DkmContinueCorruptingException>
    Friend NotInheritable Class VisualBasicLanguageInstructionDecoder : Inherits LanguageInstructionDecoder(Of PEMethodSymbol)

        Public Sub New()
            MyBase.New(VisualBasicInstructionDecoder.Instance)
        End Sub

    End Class

End Namespace