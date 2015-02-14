' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    ''' <summary>
    ''' Computes expansion of <see cref="DkmClrValue"/> instances.
    ''' </summary>
    ''' <remarks>
    ''' This class provides implementation for the Visual Basic ResultProvider component.
    ''' </remarks>
    <DkmReportNonFatalWatsonException(ExcludeExceptionType:=GetType(NotImplementedException)), DkmContinueCorruptingException>
    Friend NotInheritable Class VisualBasicResultProvider : Inherits ResultProvider

        ' Currently, falls back on default (C#) implementation for all behavior...
        Public Sub New()
            MyBase.New(VisualBasicFormatter.Instance)
        End Sub

    End Class

End Namespace
