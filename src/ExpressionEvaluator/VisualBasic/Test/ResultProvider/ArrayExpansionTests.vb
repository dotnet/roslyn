' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.VisualStudio.Debugger.Evaluation
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.UnitTests

    Public Class ArrayExpansionTests : Inherits VisualBasicResultProviderTestBase

        <Fact>
        Public Sub MultiDimensionalArrays()
            Dim source = "
Class C
    Dim _2d(,) as Integer = {{1,2},{3, 4}}
    Dim _3d(,,)(,) as Integer = {{{_2d}}}
End Class
"
            Dim assembly = GetAssembly(source)
            Dim typeC = assembly.GetType("C")

            Dim children = GetChildren(FormatResult("c", CreateDkmClrValue(Activator.CreateInstance(typeC))))
            Verify(children,
                EvalResult("_2d", "{Length=4}", "Integer(,)", "c._2d", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.CanFavorite),
                EvalResult("_3d", "{Length=1}", "Integer(,,)(,)", "c._3d", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.CanFavorite))

            children = GetChildren(children(1))
            Verify(children,
                   EvalResult("(0, 0, 0)", "{Length=4}", "Integer(,)", "c._3d(0, 0, 0)", DkmEvaluationResultFlags.Expandable))

            children = GetChildren(children(0))
            Verify(children,
                EvalResult("(0, 0)", "1", "Integer", "c._3d(0, 0, 0)(0, 0)"),
                EvalResult("(0, 1)", "2", "Integer", "c._3d(0, 0, 0)(0, 1)"),
                EvalResult("(1, 0)", "3", "Integer", "c._3d(0, 0, 0)(1, 0)"),
                EvalResult("(1, 1)", "4", "Integer", "c._3d(0, 0, 0)(1, 1)"))
        End Sub

    End Class

End Namespace
