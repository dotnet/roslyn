' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.VisualStudio.Debugger.Clr
Imports Microsoft.VisualStudio.Debugger.Evaluation
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Public Class FormatSpecifierTests
        Inherits VisualBasicResultProviderTestBase

        <Fact>
        Public Sub NoQuotes_String()
            Dim runtime = New DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib())
            Dim inspectionContext = CreateDkmInspectionContext(DkmEvaluationFlags.NoQuotes)
            Dim stringType = runtime.GetType(GetType(String))

            ' Nothing
            Dim value = CreateDkmClrValue(Nothing, type:=stringType, inspectionContext:=inspectionContext)
            Dim result = FormatResult("s", value)
            Verify(result,
                   EvalResult("s", "Nothing", "String", "s", editableValue:=Nothing, flags:=DkmEvaluationResultFlags.None))

            ' ""
            value = CreateDkmClrValue(String.Empty, type:=stringType, inspectionContext:=inspectionContext)
            result = FormatResult("s", value)
            Verify(result,
                   EvalResult("s", "", "String", "s", editableValue:="""""", flags:=DkmEvaluationResultFlags.RawString))

            ' "'"
            value = CreateDkmClrValue("'", type:=stringType, inspectionContext:=inspectionContext)
            result = FormatResult("s", value)
            Verify(result,
                   EvalResult("s", "'", "String", "s", editableValue:="""'""", flags:=DkmEvaluationResultFlags.RawString))

            ' """"
            value = CreateDkmClrValue("""", type:=stringType, inspectionContext:=inspectionContext)
            result = FormatResult("s", value)
            Verify(result,
                   EvalResult("s", """", "String", "s", editableValue:="""""""""", flags:=DkmEvaluationResultFlags.RawString))

            ' " " with alias
            value = CreateDkmClrValue(" ", type:=stringType, [alias]:="1", evalFlags:=DkmEvaluationResultFlags.HasObjectId, inspectionContext:=inspectionContext)
            result = FormatResult("s", value)
            Verify(result,
                   EvalResult("s", "  {$1}", "String", "s", editableValue:=""" """, flags:=DkmEvaluationResultFlags.RawString Or DkmEvaluationResultFlags.HasObjectId))

            ' array
            value = CreateDkmClrValue({"1"}, type:=stringType.MakeArrayType(), inspectionContext:=inspectionContext)
            result = FormatResult("a", value)
            Verify(result,
                   EvalResult("a", "{Length=1}", "String()", "a", editableValue:=Nothing, flags:=DkmEvaluationResultFlags.Expandable))
            Dim children = GetChildren(result)
            ' TODO: InspectionContext should not be inherited. See IDkmClrFormatter.GetValueString.
            Verify(children,
                   EvalResult("(0)", "1", "String", "a(0)", editableValue:="""1""", flags:=DkmEvaluationResultFlags.RawString))
        End Sub

        <Fact>
        Public Sub NoQuotes_Char()
            Dim runtime = New DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib())
            Dim inspectionContext = CreateDkmInspectionContext(DkmEvaluationFlags.NoQuotes)
            Dim charType = runtime.GetType(GetType(Char))

            ' 0
            Dim value = CreateDkmClrValue(ChrW(0), type:=charType, inspectionContext:=inspectionContext)
            Dim result = FormatResult("c", value)
            Verify(result,
                   EvalResult("c", "vbNullChar", "Char", "c", editableValue:="vbNullChar", flags:=DkmEvaluationResultFlags.None))

            ' "'"c
            value = CreateDkmClrValue("'"c, type:=charType, inspectionContext:=inspectionContext)
            result = FormatResult("c", value)
            Verify(result,
                   EvalResult("c", "'", "Char", "c", editableValue:="""'""c", flags:=DkmEvaluationResultFlags.None))

            ' """"c
            value = CreateDkmClrValue(""""c, type:=charType, inspectionContext:=inspectionContext)
            result = FormatResult("c", value)
            Verify(result,
                   EvalResult("c", """"c, "Char", "c", editableValue:="""""""""c", flags:=DkmEvaluationResultFlags.None))

            ' array
            value = CreateDkmClrValue({"1"c}, type:=charType.MakeArrayType(), inspectionContext:=inspectionContext)
            result = FormatResult("a", value)
            Verify(result,
                   EvalResult("a", "{Length=1}", "Char()", "a", editableValue:=Nothing, flags:=DkmEvaluationResultFlags.Expandable))
            Dim children = GetChildren(result)
            ' TODO: InspectionContext should not be inherited. See IDkmClrFormatter.GetValueString.
            Verify(children,
                   EvalResult("(0)", "1", "Char", "a(0)", editableValue:="""1""c", flags:=DkmEvaluationResultFlags.None))
        End Sub

    End Class

End Namespace