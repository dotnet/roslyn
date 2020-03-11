' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.VisualStudio.Debugger.Clr
Imports Microsoft.VisualStudio.Debugger.Evaluation
Imports Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.UnitTests

    Public Class ObjectIdTests
        Inherits VisualBasicResultProviderTestBase

        <Fact>
        Public Sub SpecialTypes()
            Dim objectType = New DkmClrType(CType(GetType(Object), TypeImpl))
            Dim value As DkmClrValue
            ' Integer
            value = CreateDkmClrValue(value:=1, type:=GetType(Integer), alias:="$1", evalFlags:=DkmEvaluationResultFlags.HasObjectId)
            Verify(
                FormatResult("i", value, objectType),
                EvalResult("i", "1 {$1}", "Object {Integer}", "i", DkmEvaluationResultFlags.HasObjectId))
            ' Integer (hex)
            value = CreateDkmClrValue(value:=2, type:=GetType(Integer), alias:="$2", evalFlags:=DkmEvaluationResultFlags.HasObjectId)
            Verify(
                FormatResult("i", value, objectType, inspectionContext:=CreateDkmInspectionContext(radix:=16)),
                EvalResult("i", "&H00000002 {$2}", "Object {Integer}", "i", DkmEvaluationResultFlags.HasObjectId))
            ' Char
            value = CreateDkmClrValue(value:="c"c, type:=GetType(Char), alias:="$3", evalFlags:=DkmEvaluationResultFlags.HasObjectId)
            Verify(
                FormatResult("c", value, objectType),
                EvalResult("c", """c""c {$3}", "Object {Char}", "c", DkmEvaluationResultFlags.HasObjectId, editableValue:="""c""c"))
            ' Enum
            value = CreateDkmClrValue(value:=DkmEvaluationResultFlags.HasObjectId, type:=GetType(DkmEvaluationResultFlags), alias:="$Four", evalFlags:=DkmEvaluationResultFlags.HasObjectId)
            Verify(
                FormatResult("e", value, objectType),
                EvalResult("e", "HasObjectId {512} {$Four}", "Object {Microsoft.VisualStudio.Debugger.Evaluation.DkmEvaluationResultFlags}", "e", DkmEvaluationResultFlags.HasObjectId, editableValue:="Microsoft.VisualStudio.Debugger.Evaluation.DkmEvaluationResultFlags.HasObjectId"))
            ' String
            value = CreateDkmClrValue(value:="str", type:=GetType(String), alias:="$5", evalFlags:=DkmEvaluationResultFlags.HasObjectId)
            Verify(
                FormatResult("s", value),
                EvalResult("s", """str"" {$5}", "String", "s", DkmEvaluationResultFlags.RawString Or DkmEvaluationResultFlags.HasObjectId, editableValue:="""str"""))
            ' Decimal
            value = CreateDkmClrValue(value:=6D, type:=GetType(Decimal), alias:="$6", evalFlags:=DkmEvaluationResultFlags.HasObjectId)
            Verify(
                FormatResult("d", value, objectType),
                EvalResult("d", "6 {$6}", "Object {Decimal}", "d", DkmEvaluationResultFlags.HasObjectId, editableValue:="6D"))
            ' Array
            value = CreateDkmClrValue(value:={1, 2}, type:=GetType(Integer()), alias:="$7", evalFlags:=DkmEvaluationResultFlags.HasObjectId)
            Verify(
                FormatResult("a", value, objectType),
                EvalResult("a", "{Length=2} {$7}", "Object {Integer()}", "a", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.HasObjectId))
        End Sub

    End Class

End Namespace
