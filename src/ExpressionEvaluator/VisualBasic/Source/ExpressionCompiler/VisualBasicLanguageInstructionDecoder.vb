' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    <DkmReportNonFatalWatsonException(ExcludeExceptionType:=GetType(NotImplementedException)), DkmContinueCorruptingException>
    Friend NotInheritable Class VisualBasicLanguageInstructionDecoder : Inherits LanguageInstructionDecoder(Of VisualBasicCompilation, MethodSymbol, PEModuleSymbol, TypeSymbol, TypeParameterSymbol, ParameterSymbol)

        Public Sub New()
            MyBase.New(VisualBasicInstructionDecoder.Instance)
        End Sub

    End Class

End Namespace
