' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.VisualStudio.Debugger.Clr
Imports Microsoft.VisualStudio.Debugger.Evaluation
Imports System.Reflection
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.UnitTests

    Public Class ResultsViewTests
        Inherits VisualBasicResultProviderTestBase

        <Fact>
        Public Sub IEnumerableExplicitImplementation()
            Const source =
"Imports System.Collections
Class C
    Implements IEnumerable
    Private e As IEnumerable
    Sub New(e As IEnumerable)
        Me.e = e
    End Sub
    Private Function F() As IEnumerator Implements IEnumerable.GetEnumerator
        Return e.GetEnumerator()
    End Function
End Class"
            Dim assembly = GetAssembly(source)
            Dim assemblies = ReflectionUtilities.GetMscorlibAndSystemCore(assembly)
            Using ReflectionUtilities.LoadAssemblies(assemblies)
                Dim runtime = New DkmClrRuntimeInstance(assemblies)
                Dim type = assembly.GetType("C")
                Dim value = CreateDkmClrValue(
                    value:=type.Instantiate(New Integer() {1, 2}),
                    type:=runtime.GetType(CType(type, TypeImpl)))
                Dim result = FormatResult("o", value)
                Verify(result,
                       EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable))
                Dim children = GetChildren(result)
                Verify(children,
                    EvalResult(
                        "e",
                        "{Length=2}",
                        "System.Collections.IEnumerable {Integer()}",
                        "o.e",
                        DkmEvaluationResultFlags.Expandable),
                    EvalResult(
                        "Results View",
                        "Expanding the Results View will enumerate the IEnumerable",
                        "",
                        "o, results",
                        DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly,
                        DkmEvaluationResultCategory.Method))
                children = GetChildren(children(1))
                Verify(children,
                    EvalResult("(0)", "1", "Object {Integer}", "New System.Linq.SystemCore_EnumerableDebugView(o).Items(0)"),
                    EvalResult("(1)", "2", "Object {Integer}", "New System.Linq.SystemCore_EnumerableDebugView(o).Items(1)"))
            End Using
        End Sub

        <Fact>
        Public Sub IEnumerableOfTExplicitImplementation()
            Const source =
"Imports System.Collections
Imports System.Collections.Generic
Class C(Of T)
    Implements IEnumerable(Of T)
    Private e As IEnumerable(Of T)
    Sub New(e As IEnumerable(Of T))
        Me.e = e
    End Sub
    Private Function F() As IEnumerator(Of T) Implements IEnumerable(Of T).GetEnumerator
        Return e.GetEnumerator()
    End Function
    Private Function G() As IEnumerator Implements IEnumerable.GetEnumerator
        Return e.GetEnumerator()
    End Function
End Class"
            Dim assembly = GetAssembly(source)
            Dim assemblies = ReflectionUtilities.GetMscorlibAndSystemCore(assembly)
            Using ReflectionUtilities.LoadAssemblies(assemblies)
                Dim runtime = New DkmClrRuntimeInstance(assemblies)
                Dim type = assembly.GetType("C`1").MakeGenericType(GetType(Integer))
                Dim value = CreateDkmClrValue(
                    value:=type.Instantiate(New Integer() {1, 2}),
                    type:=runtime.GetType(CType(type, TypeImpl)))
                Dim result = FormatResult("o", value)
                Verify(result,
                       EvalResult("o", "{C(Of Integer)}", "C(Of Integer)", "o", DkmEvaluationResultFlags.Expandable))
                Dim children = GetChildren(result)
                Verify(children,
                    EvalResult(
                        "e",
                        "{Length=2}",
                        "System.Collections.Generic.IEnumerable(Of Integer) {Integer()}",
                        "o.e",
                        DkmEvaluationResultFlags.Expandable),
                    EvalResult(
                        "Results View",
                        "Expanding the Results View will enumerate the IEnumerable",
                        "",
                        "o, results",
                        DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly,
                        DkmEvaluationResultCategory.Method))
                children = GetChildren(children(1))
                Verify(children,
                    EvalResult("(0)", "1", "Integer", "New System.Linq.SystemCore_EnumerableDebugView(Of Integer)(o).Items(0)"),
                    EvalResult("(1)", "2", "Integer", "New System.Linq.SystemCore_EnumerableDebugView(Of Integer)(o).Items(1)"))
            End Using
        End Sub

        <WorkItem(1043746, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1043746")>
        <Fact>
        Public Sub GetProxyPropertyValueError()
            Const source =
"Imports System.Collections
Class C
    Implements IEnumerable
    Private Iterator Function F() As IEnumerator Implements IEnumerable.GetEnumerator
        Yield 1
    End Function
End Class"
            Dim runtime As DkmClrRuntimeInstance = Nothing
            Dim getMemberValue As GetMemberValueDelegate = Function(v, m) If(m = "Items", CreateErrorValue(runtime.GetType(GetType(Object)).MakeArrayType(), String.Format("Unable to evaluate '{0}'", m)), Nothing)
            runtime = New DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlibAndSystemCore(GetAssembly(source)), getMemberValue:=getMemberValue)
            Using runtime.Load()
                Dim type = runtime.GetType("C")
                Dim value = CreateDkmClrValue(type.Instantiate(), type:=type)
                Dim result = FormatResult("o", value)
                Verify(result,
                       EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable))
                Dim children = GetChildren(result)
                Verify(children,
                    EvalResult(
                        "Results View",
                        "Expanding the Results View will enumerate the IEnumerable",
                        "",
                        "o, results",
                        DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly,
                        DkmEvaluationResultCategory.Method))
                children = GetChildren(children(0))
                Verify(children,
                    EvalFailedResult("Error", "Unable to evaluate 'Items'", flags:=DkmEvaluationResultFlags.None))
            End Using
        End Sub

    End Class

End Namespace

