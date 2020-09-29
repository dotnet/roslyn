// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using System;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.UnitTests
{
    public class ObjectIdTests : CSharpResultProviderTestBase
    {
        [Fact]
        public void SpecialTypes()
        {
            var objectType = new DkmClrType((TypeImpl)typeof(object));
            DkmClrValue value;
            // int
            value = CreateDkmClrValue(value: 1, type: typeof(int), alias: "$1", evalFlags: DkmEvaluationResultFlags.HasObjectId);
            Verify(
                FormatResult("i", value, objectType),
                EvalResult("i", "1 {$1}", "object {int}", "i", DkmEvaluationResultFlags.HasObjectId));
            // char
            value = CreateDkmClrValue(value: 'c', type: typeof(char), alias: "$2", evalFlags: DkmEvaluationResultFlags.HasObjectId);
            Verify(
                FormatResult("c", value, objectType),
                EvalResult("c", "99 'c' {$2}", "object {char}", "c", DkmEvaluationResultFlags.HasObjectId, editableValue: "'c'"));
            // char (hex)
            value = CreateDkmClrValue(value: 'c', type: typeof(char), alias: "$3", evalFlags: DkmEvaluationResultFlags.HasObjectId);
            Verify(
                FormatResult("c", value, objectType, inspectionContext: CreateDkmInspectionContext(radix: 16)),
                EvalResult("c", "0x0063 'c' {$3}", "object {char}", "c", DkmEvaluationResultFlags.HasObjectId, editableValue: "'c'"));
            // enum
            value = CreateDkmClrValue(value: DkmEvaluationResultFlags.HasObjectId, type: typeof(DkmEvaluationResultFlags), alias: "$Four", evalFlags: DkmEvaluationResultFlags.HasObjectId);
            Verify(
                FormatResult("e", value, objectType),
                EvalResult("e", "HasObjectId {$Four}", "object {Microsoft.VisualStudio.Debugger.Evaluation.DkmEvaluationResultFlags}", "e", DkmEvaluationResultFlags.HasObjectId, editableValue: "Microsoft.VisualStudio.Debugger.Evaluation.DkmEvaluationResultFlags.HasObjectId"));
            // string
            value = CreateDkmClrValue(value: "str", type: typeof(string), alias: "$5", evalFlags: DkmEvaluationResultFlags.HasObjectId);
            Verify(
                FormatResult("s", value),
                EvalResult("s", "\"str\" {$5}", "string", "s", DkmEvaluationResultFlags.RawString | DkmEvaluationResultFlags.HasObjectId, editableValue: "\"str\""));
            // decimal
            value = CreateDkmClrValue(value: 6m, type: typeof(decimal), alias: "$6", evalFlags: DkmEvaluationResultFlags.HasObjectId);
            Verify(
                FormatResult("d", value, objectType),
                EvalResult("d", "6 {$6}", "object {decimal}", "d", DkmEvaluationResultFlags.HasObjectId, editableValue: "6M"));
            // array
            value = CreateDkmClrValue(value: new int[] { 1, 2 }, type: typeof(int[]), alias: "$7", evalFlags: DkmEvaluationResultFlags.HasObjectId);
            Verify(
                FormatResult("a", value, objectType),
                EvalResult("a", "{int[2]} {$7}", "object {int[]}", "a", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.HasObjectId));
        }

        /// <summary>
        /// Aliases that are not positive integers or valid
        /// identifiers (perhaps created from another EE).
        /// </summary>
        [Fact]
        public void OtherIds()
        {
            DkmClrValue value;
            // ""
            value = CreateDkmClrValue(value: new object(), type: typeof(object), alias: "", evalFlags: DkmEvaluationResultFlags.HasObjectId);
            Verify(FormatResult("o", value), EvalResult("o", "{object}", "object", "o", DkmEvaluationResultFlags.HasObjectId));
            // "$"
            value = CreateDkmClrValue(value: new object(), type: typeof(object), alias: "$", evalFlags: DkmEvaluationResultFlags.HasObjectId);
            Verify(FormatResult("o", value), EvalResult("o", "{object} {$}", "object", "o", DkmEvaluationResultFlags.HasObjectId));
            // "$ "
            value = CreateDkmClrValue(value: new object(), type: typeof(object), alias: "$ ", evalFlags: DkmEvaluationResultFlags.HasObjectId);
            Verify(FormatResult("o", value), EvalResult("o", "{object} {$ }", "object", "o", DkmEvaluationResultFlags.HasObjectId));
            // "$-1"
            value = CreateDkmClrValue(value: new object(), type: typeof(object), alias: "$-1", evalFlags: DkmEvaluationResultFlags.HasObjectId);
            Verify(FormatResult("o", value), EvalResult("o", "{object} {$-1}", "object", "o", DkmEvaluationResultFlags.HasObjectId));
            // "$1.1AB"
            value = CreateDkmClrValue(value: new object(), type: typeof(object), alias: "$1.1AB", evalFlags: DkmEvaluationResultFlags.HasObjectId);
            Verify(FormatResult("o", value), EvalResult("o", "{object} {$1.1AB}", "object", "o", DkmEvaluationResultFlags.HasObjectId));
            // "1#"
            value = CreateDkmClrValue(value: new object(), type: typeof(object), alias: "1#", evalFlags: DkmEvaluationResultFlags.HasObjectId);
            Verify(FormatResult("o", value), EvalResult("o", "{object} {1#}", "object", "o", DkmEvaluationResultFlags.HasObjectId));
            // "$1#"
            value = CreateDkmClrValue(value: new object(), type: typeof(object), alias: "$1#", evalFlags: DkmEvaluationResultFlags.HasObjectId);
            Verify(FormatResult("o", value), EvalResult("o", "{object} {$1#}", "object", "o", DkmEvaluationResultFlags.HasObjectId));
            // "$${}"
            value = CreateDkmClrValue(value: new object(), type: typeof(object), alias: "$${}", evalFlags: DkmEvaluationResultFlags.HasObjectId);
            Verify(FormatResult("o", value), EvalResult("o", "{object} {$${}}", "object", "o", DkmEvaluationResultFlags.HasObjectId));
        }

        [Fact]
        public void BaseAndDerived()
        {
            var source =
@"class A { }
class B : A { }
class C : B { }";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(
                Activator.CreateInstance(type),
                type,
                alias: "$2",
                evalFlags: DkmEvaluationResultFlags.HasObjectId);
            var evalResult = FormatResult("o", value, new DkmClrType((TypeImpl)type.BaseType));
            Verify(evalResult,
                EvalResult("o", "{C} {$2}", "B {C}", "o", DkmEvaluationResultFlags.HasObjectId));
        }

        [Fact]
        public void ToStringOverride()
        {
            var source =
@"class C
{
    public override string ToString()
    {
        return ""ToString"";
    }
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(
                Activator.CreateInstance(type),
                type,
                alias: "$3",
                evalFlags: DkmEvaluationResultFlags.HasObjectId);
            var evalResult = FormatResult("o", value);
            Verify(evalResult,
                EvalResult("o", "{ToString} {$3}", "C", "o", DkmEvaluationResultFlags.HasObjectId));
        }

        [Fact]
        public void DebuggerDisplay()
        {
            var source =
@"using System.Diagnostics;
[DebuggerDisplay(""{F}"")]
class C
{
    object F = 2;
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(
                Activator.CreateInstance(type),
                type,
                alias: "$4321",
                evalFlags: DkmEvaluationResultFlags.HasObjectId);
            var evalResult = FormatResult("o", value);
            Verify(evalResult,
                EvalResult("o", "2 {$4321}", "C", "o", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.HasObjectId));
        }

        [Fact]
        public void DebuggerTypeProxy()
        {
            var source =
@"using System.Diagnostics;
[DebuggerTypeProxy(typeof(P))]
class C
{
}
class P
{
    public P(C c) { }
}";
            var assembly = GetAssembly(source);
            var type = assembly.GetType("C");
            var value = CreateDkmClrValue(
                Activator.CreateInstance(type),
                type,
                alias: "$5",
                evalFlags: DkmEvaluationResultFlags.HasObjectId);
            var evalResult = FormatResult("o", value);
            Verify(evalResult,
                EvalResult("o", "{C} {$5}", "C", "o", DkmEvaluationResultFlags.Expandable | DkmEvaluationResultFlags.HasObjectId));
            var children = GetChildren(evalResult);
            Verify(children,
                EvalResult("Raw View", null, "", "o, raw", DkmEvaluationResultFlags.ReadOnly | DkmEvaluationResultFlags.HasObjectId, DkmEvaluationResultCategory.Data));
        }
    }
}
