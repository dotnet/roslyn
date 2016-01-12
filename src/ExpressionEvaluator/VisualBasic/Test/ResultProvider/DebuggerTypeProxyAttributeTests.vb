' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.VisualStudio.Debugger.Clr
Imports Microsoft.VisualStudio.Debugger.Evaluation
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Public Class DebuggerTypeProxyAttributeTests
        Inherits VisualBasicResultProviderTestBase

        <Fact>
        Public Sub GenericTypeWithGenericTypeArgument()
            Const source =
"Imports System.Diagnostics
<DebuggerTypeProxy(GetType(PA(Of )))>
Friend Class A(Of T)
    Public ReadOnly F As T
    Public Sub New(f As T)
        Me.F = f
    End Sub
End Class
Friend Class PA(Of T)
    Public ReadOnly PF As T
    Public Sub New(a As A(Of T))
        Me.PF = a.F
    End Sub
End Class
<DebuggerTypeProxy(GetType(PB(Of )))>
Friend Class B(Of T)
    Public ReadOnly G As T
    Public Sub New(g As T)
        Me.G = g
    End Sub
End Class
Friend Class PB(Of T)
    Public ReadOnly PG As T
    Public Sub New(b As B(Of T))
        Me.PG = b.G
    End Sub
End Class
Class C
    Private b As New B(Of A(Of String))(New A(Of String)(""A""))
End Class"
            Dim assembly = GetAssembly(source)
            Dim assemblies = ReflectionUtilities.GetMscorlib(assembly)
            Using ReflectionUtilities.LoadAssemblies(assemblies)
                Dim runtime = New DkmClrRuntimeInstance(assemblies)
                Dim type = assembly.GetType("C")
                Dim value = CreateDkmClrValue(
                    value:=type.Instantiate(),
                    type:=runtime.GetType(CType(type, TypeImpl)))
                Dim result = FormatResult("o", value)
                Verify(result,
                       EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable))
                Dim children = GetChildren(result)
                Verify(children,
                    EvalResult("b", "{B(Of A(Of String))}", "B(Of A(Of String))", "o.b", DkmEvaluationResultFlags.Expandable))
                children = GetChildren(children(0))
                Verify(children,
                    EvalResult("PG", "{A(Of String)}", "A(Of String)", "New PB(Of A(Of String))(o.b).PG", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly),
                    EvalResult("Raw View", Nothing, "", "o.b, raw", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data))
                Dim moreChildren = GetChildren(children(1))
                Verify(moreChildren,
                    EvalResult("G", "{A(Of String)}", "A(Of String)", "o.b.G", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly))
                moreChildren = GetChildren(children(0))
                Verify(moreChildren,
                    EvalResult("PF", """A""", "String", "New PA(Of String)(New PB(Of A(Of String))(o.b).PG).PF", DkmEvaluationResultFlags.RawString Or DkmEvaluationResultFlags.ReadOnly),
                    EvalResult("Raw View", Nothing, "", "New PB(Of A(Of String))(o.b).PG, raw", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly, DkmEvaluationResultCategory.Data))
                moreChildren = GetChildren(moreChildren(1))
                Verify(moreChildren,
                    EvalResult("F", """A""", "String", "(New PB(Of A(Of String))(o.b).PG).F", DkmEvaluationResultFlags.RawString Or DkmEvaluationResultFlags.ReadOnly))
            End Using
        End Sub

    End Class

End Namespace
