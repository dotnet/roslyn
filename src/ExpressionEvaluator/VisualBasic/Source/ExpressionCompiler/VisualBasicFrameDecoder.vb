' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    <DkmReportNonFatalWatsonException(ExcludeExceptionType:=GetType(NotImplementedException)), DkmContinueCorruptingException>
    Friend NotInheritable Class VisualBasicFrameDecoder : Inherits FrameDecoder(Of VisualBasicCompilation, MethodSymbol, PEModuleSymbol, TypeSymbol, TypeParameterSymbol)

        Public Sub New()
            MyBase.New(VisualBasicInstructionDecoder.Instance)
        End Sub

    End Class

End Namespace
