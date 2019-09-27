' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.Debugger.Clr
Imports Microsoft.VisualStudio.Debugger.Evaluation
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.UnitTests

    Public Class ObjectFavoritesTests : Inherits VisualBasicResultProviderTestBase

        <Fact>
        Public Sub Expansion()

            Dim source =
"Class A
    Dim s1 As String = ""S1""
    Dim s2 As String = ""S2""
End Class
Class B : Inherits A
    Dim s3 As String = ""S3""
    Dim s4 As String = ""S4""
End Class
Class C
    Dim a As A = new A()
    Dim b As B = new B()
End Class"

            Dim assembly = GetAssembly(source)
            Dim type = assembly.GetType("C")
            Dim rootExpr = "new C()"

            Dim favoritesByTypeName = New Dictionary(Of String, DkmClrObjectFavoritesInfo) From
            {
                {"C", New DkmClrObjectFavoritesInfo(New String() {"b"})},
                {"B", New DkmClrObjectFavoritesInfo(New String() {"s4", "s2"})}
            }

            Dim runtime = New DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(assembly), favoritesByTypeName)

            Dim value = CreateDkmClrValue(
                value:=Activator.CreateInstance(type),
                type:=runtime.GetType(type))

            Dim result = FormatResult(rootExpr, value)
            Verify(result,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.HasFavorites))
            Dim children = GetChildren(result)
            Verify(children,
                EvalResult("b", "{B}", "B", "(new C()).b", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.CanFavorite Or DkmEvaluationResultFlags.IsFavorite Or DkmEvaluationResultFlags.HasFavorites),
                EvalResult("a", "{A}", "A", "(new C()).a", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.CanFavorite))

            ' B b = New B()
            Dim more = GetChildren(children(0))
            Verify(more,
                EvalResult("s4", """S4""", "String", "(new C()).b.s4", DkmEvaluationResultFlags.RawString Or DkmEvaluationResultFlags.CanFavorite Or DkmEvaluationResultFlags.IsFavorite, editableValue:="""S4"""),
                EvalResult("s2", """S2""", "String", "(new C()).b.s2", DkmEvaluationResultFlags.RawString Or DkmEvaluationResultFlags.CanFavorite Or DkmEvaluationResultFlags.IsFavorite, editableValue:="""S2"""),
                EvalResult("s1", """S1""", "String", "(new C()).b.s1", DkmEvaluationResultFlags.RawString Or DkmEvaluationResultFlags.CanFavorite, editableValue:="""S1"""),
                EvalResult("s3", """S3""", "String", "(new C()).b.s3", DkmEvaluationResultFlags.RawString Or DkmEvaluationResultFlags.CanFavorite, editableValue:="""S3"""))

            ' A a = New A()
            more = GetChildren(children(1))
            Verify(more,
                EvalResult("s1", """S1""", "String", "(new C()).a.s1", DkmEvaluationResultFlags.RawString Or DkmEvaluationResultFlags.CanFavorite, editableValue:="""S1"""),
                EvalResult("s2", """S2""", "String", "(new C()).a.s2", DkmEvaluationResultFlags.RawString Or DkmEvaluationResultFlags.CanFavorite, editableValue:="""S2"""))
        End Sub

        <Fact>
        Public Sub FilteredExpansion()

            Dim source =
"Class A
    Dim s1 As String = ""S1""
    Dim s2 As String = ""S2""
End Class
Class B : Inherits A
    Dim s3 As String = ""S3""
    Dim s4 As String = ""S4""
End Class
Class C
    Dim a As A = new A()
    Dim b As B = new B()
End Class"

            Dim assembly = GetAssembly(source)
            Dim type = assembly.GetType("C")
            Dim rootExpr = "new C()"

            Dim favoritesByTypeName = New Dictionary(Of String, DkmClrObjectFavoritesInfo) From
            {
                {"C", New DkmClrObjectFavoritesInfo(New String() {"b"})},
                {"B", New DkmClrObjectFavoritesInfo(New String() {"s4", "s2"})}
            }

            Dim runtime = New DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(assembly), favoritesByTypeName)

            Dim value = CreateDkmClrValue(
                value:=Activator.CreateInstance(type),
                type:=runtime.GetType(type))

            Dim result = FormatResult(rootExpr, value, Nothing, CreateDkmInspectionContext(DkmEvaluationFlags.FilterToFavorites))
            Verify(result,
                EvalResult(rootExpr, "{C}", "C", rootExpr, DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.HasFavorites))
            Dim children = GetChildren(result, CreateDkmInspectionContext(DkmEvaluationFlags.FilterToFavorites))
            Verify(children,
                EvalResult("b", "{B}", "B", "(new C()).b", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.CanFavorite Or DkmEvaluationResultFlags.IsFavorite Or DkmEvaluationResultFlags.HasFavorites))

            ' B b = New B()
            Dim more = GetChildren(children(0), CreateDkmInspectionContext(DkmEvaluationFlags.FilterToFavorites))
            Verify(more,
                EvalResult("s4", """S4""", "String", "(new C()).b.s4", DkmEvaluationResultFlags.RawString Or DkmEvaluationResultFlags.CanFavorite Or DkmEvaluationResultFlags.IsFavorite, editableValue:="""S4"""),
                EvalResult("s2", """S2""", "String", "(new C()).b.s2", DkmEvaluationResultFlags.RawString Or DkmEvaluationResultFlags.CanFavorite Or DkmEvaluationResultFlags.IsFavorite, editableValue:="""S2"""))
        End Sub

        <Fact>
        Public Sub DisplayString()

            Dim source =
"Class A
    Dim s1 As String = ""S1""
    Dim s2 As String = ""S2""
    Dim s3 As String = ""S3""
    Dim s4 As String = ""S4""
End Class"

            Dim assembly = GetAssembly(source)
            Dim type = assembly.GetType("A")
            Dim rootExpr = "new A()"

            Dim favoritesByTypeName = New Dictionary(Of String, DkmClrObjectFavoritesInfo) From
            {
                {"A", New DkmClrObjectFavoritesInfo(New String() {"s4", "s2"}, "s4 = {s4}, s2 = {s2}")}
            }

            Dim runtime = New DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(assembly), favoritesByTypeName)

            Dim value = CreateDkmClrValue(
                value:=Activator.CreateInstance(type),
                type:=runtime.GetType(type))

            Dim result = FormatResult(rootExpr, value)
            Verify(result,
                EvalResult(rootExpr, "s4 = ""S4"", s2 = ""S2""", "A", rootExpr, DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.HasFavorites))
        End Sub

        <Fact>
        Public Sub SimpleDisplayString()

            Dim source =
"Class A
    Dim s1 As String = ""S1""
    Dim s2 As String = ""S2""
    Dim s3 As String = ""S3""
    Dim s4 As String = ""S4""
End Class"

            Dim assembly = GetAssembly(source)
            Dim type = assembly.GetType("A")
            Dim rootExpr = "new A()"

            Dim favoritesByTypeName = New Dictionary(Of String, DkmClrObjectFavoritesInfo) From
            {
                {"A", New DkmClrObjectFavoritesInfo(New String() {"s4", "s2"}, "s4 = {s4}, s2 = {s2}", "{s4}, {s2}")}
            }

            Dim runtime = New DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(assembly), favoritesByTypeName)

            Dim value = CreateDkmClrValue(
                value:=Activator.CreateInstance(type),
                type:=runtime.GetType(type))

            Dim result = FormatResult(rootExpr, value, Nothing, CreateDkmInspectionContext(DkmEvaluationFlags.UseSimpleDisplayString))
            Verify(result,
                EvalResult(rootExpr, """S4"", ""S2""", "A", rootExpr, DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.HasFavorites))
        End Sub

    End Class

End Namespace
