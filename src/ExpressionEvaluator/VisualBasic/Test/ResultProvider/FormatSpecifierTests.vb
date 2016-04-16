' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.VisualStudio.Debugger.Clr
Imports Microsoft.VisualStudio.Debugger.Evaluation
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.UnitTests

    Public Class FormatSpecifierTests
        Inherits VisualBasicResultProviderTestBase

        <Fact>
        Public Sub NoQuotes_String()
            Dim runtime = New DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib())
            Dim inspectionContext = CreateDkmInspectionContext(DkmEvaluationFlags.NoQuotes)
            Dim stringType = runtime.GetType(GetType(String))

            ' Nothing
            Dim value = CreateDkmClrValue(Nothing, type:=stringType)
            Dim result = FormatResult("s", value, inspectionContext:=inspectionContext)
            Verify(result,
                   EvalResult("s", "Nothing", "String", "s", editableValue:=Nothing, flags:=DkmEvaluationResultFlags.None))

            ' ""
            value = CreateDkmClrValue(String.Empty, type:=stringType)
            result = FormatResult("s", value, inspectionContext:=inspectionContext)
            Verify(result,
                   EvalResult("s", "", "String", "s", editableValue:="""""", flags:=DkmEvaluationResultFlags.RawString))

            ' "'"
            value = CreateDkmClrValue("'", type:=stringType)
            result = FormatResult("s", value, inspectionContext:=inspectionContext)
            Verify(result,
                   EvalResult("s", "'", "String", "s", editableValue:="""'""", flags:=DkmEvaluationResultFlags.RawString))

            ' """"
            value = CreateDkmClrValue("""", type:=stringType)
            result = FormatResult("s", value, inspectionContext:=inspectionContext)
            Verify(result,
                   EvalResult("s", """", "String", "s", editableValue:="""""""""", flags:=DkmEvaluationResultFlags.RawString))

            ' "a" & vbCrLf & "b" & vbTab & vbVerticalTab & vbBack & "c"
            value = CreateDkmClrValue("a" & vbCrLf & "b" & vbTab & vbVerticalTab & vbBack & "c", type:=stringType)
            result = FormatResult("s", value, inspectionContext:=inspectionContext)
            Verify(result,
                   EvalResult("s", "a" & vbCrLf & "b" & vbTab & vbVerticalTab & vbBack & "c", "String", "s", editableValue:="""a"" & vbCrLf & ""b"" & vbTab & vbVerticalTab & vbBack & ""c""", flags:=DkmEvaluationResultFlags.RawString))

            ' "a" & vbNullChar & "b"
            value = CreateDkmClrValue("a" & vbNullChar & "b", type:=stringType)
            result = FormatResult("s", value, inspectionContext:=inspectionContext)
            Verify(result,
                   EvalResult("s", "a" & vbNullChar & "b", "String", "s", editableValue:="""a"" & vbNullChar & ""b""", flags:=DkmEvaluationResultFlags.RawString))

            ' " " with alias
            value = CreateDkmClrValue(" ", type:=stringType, [alias]:="$1", evalFlags:=DkmEvaluationResultFlags.HasObjectId)
            result = FormatResult("s", value, inspectionContext:=inspectionContext)
            Verify(result,
                   EvalResult("s", "  {$1}", "String", "s", editableValue:=""" """, flags:=DkmEvaluationResultFlags.RawString Or DkmEvaluationResultFlags.HasObjectId))

            ' array
            value = CreateDkmClrValue({"1"}, type:=stringType.MakeArrayType())
            result = FormatResult("a", value, inspectionContext:=inspectionContext)
            Verify(result,
                   EvalResult("a", "{Length=1}", "String()", "a", editableValue:=Nothing, flags:=DkmEvaluationResultFlags.Expandable))
            Dim children = GetChildren(result)
            ' DkmInspectionContext should not be inherited.
            Verify(children,
                   EvalResult("(0)", """1""", "String", "a(0)", editableValue:="""1""", flags:=DkmEvaluationResultFlags.RawString))
        End Sub

        <Fact>
        Public Sub NoQuotes_Char()
            Dim runtime = New DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib())
            Dim inspectionContext = CreateDkmInspectionContext(DkmEvaluationFlags.NoQuotes)
            Dim charType = runtime.GetType(GetType(Char))

            ' 0
            Dim value = CreateDkmClrValue(ChrW(0), type:=charType)
            Dim result = FormatResult("c", value, inspectionContext:=inspectionContext)
            Verify(result,
                   EvalResult("c", vbNullChar, "Char", "c", editableValue:="vbNullChar", flags:=DkmEvaluationResultFlags.None))

            ' "'"c
            value = CreateDkmClrValue("'"c, type:=charType)
            result = FormatResult("c", value, inspectionContext:=inspectionContext)
            Verify(result,
                   EvalResult("c", "'", "Char", "c", editableValue:="""'""c", flags:=DkmEvaluationResultFlags.None))

            ' """"c
            value = CreateDkmClrValue(""""c, type:=charType)
            result = FormatResult("c", value, inspectionContext:=inspectionContext)
            Verify(result,
                   EvalResult("c", """"c, "Char", "c", editableValue:="""""""""c", flags:=DkmEvaluationResultFlags.None))

            ' "\"c
            value = CreateDkmClrValue("\"c, type:=charType)
            result = FormatResult("c", value, inspectionContext:=inspectionContext)
            Verify(result,
                   EvalResult("c", "\"c, "Char", "c", editableValue:="""\""c", flags:=DkmEvaluationResultFlags.None))

            ' vbLf
            value = CreateDkmClrValue(ChrW(10), type:=charType)
            result = FormatResult("c", value, inspectionContext:=inspectionContext)
            Verify(result,
                   EvalResult("c", vbLf, "Char", "c", editableValue:="vbLf", flags:=DkmEvaluationResultFlags.None))

            ' ChrW(&H001E)
            value = CreateDkmClrValue(ChrW(&H001E), type:=charType)
            result = FormatResult("c", value, inspectionContext:=inspectionContext)
            Verify(result,
                   EvalResult("c", New String({ChrW(&H001E)}), "Char", "c", editableValue:="ChrW(30)", flags:=DkmEvaluationResultFlags.None))

            ' array
            value = CreateDkmClrValue({"1"c}, type:=charType.MakeArrayType())
            result = FormatResult("a", value, inspectionContext:=inspectionContext)
            Verify(result,
                   EvalResult("a", "{Length=1}", "Char()", "a", editableValue:=Nothing, flags:=DkmEvaluationResultFlags.Expandable))
            Dim children = GetChildren(result)
            ' DkmInspectionContext should not be inherited.
            Verify(children,
                   EvalResult("(0)", """1""c", "Char", "a(0)", editableValue:="""1""c", flags:=DkmEvaluationResultFlags.None))
        End Sub

    End Class

End Namespace