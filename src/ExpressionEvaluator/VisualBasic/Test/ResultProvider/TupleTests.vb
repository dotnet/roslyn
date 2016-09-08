' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Microsoft.VisualStudio.Debugger.Clr
Imports Microsoft.VisualStudio.Debugger.Evaluation
Imports Roslyn.Test.Utilities
Imports System.Collections.Immutable
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.UnitTests

    Public Class TupleTests
        Inherits VisualBasicResultProviderTestBase

        <Fact>
        Public Sub LongTuple()
            Const source =
"Class C
    Private _17 As (Short, Short, Short, Short, Short, Short, Short, Short, Short, Short, Short, Short, Short, Short, Short, Short, Short) =
        (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17)
End Class"
            Dim assembly0 = GenerateTupleAssembly()
            Dim reference0 = AssemblyMetadata.CreateFromImage(assembly0).GetReference()
            Dim compilation1 = CreateCompilationWithMscorlib({source}, options:=TestOptions.ReleaseDll, references:={reference0}, assemblyName:=GetUniqueName())
            Dim assembly1 = compilation1.EmitToArray()
            Dim runtime = New DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(ReflectionUtilities.Load(assembly0), ReflectionUtilities.Load(assembly1)))
            Using runtime.Load()
                Dim type = runtime.GetType("C")
                Dim value = type.Instantiate()
                Dim result = FormatResult("o", value)
                Verify(result,
                       EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable))
                Dim children = GetChildren(result)
                Verify(children,
                    EvalResult(
                        "_17",
                        "(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17)",
                        "(Short, Short, Short, Short, Short, Short, Short, Short, Short, Short, Short, Short, Short, Short, Short, Short, Short)",
                        "o._17",
                        DkmEvaluationResultFlags.Expandable))
                children = GetChildren(children(0))
                Assert.Equal(8, children.Length) ' Should be 18. https://github.com/dotnet/roslyn/issues/13421
                Dim child = children(children.Length - 1)
                Verify(child,
                    EvalResult(
                        "Rest",
                        "(8, 9, 10, 11, 12, 13, 14, 15, 16, 17)",
                        "(Short, Short, Short, Short, Short, Short, Short, Short, Short, Short)",
                        "o._17.Rest",
                        DkmEvaluationResultFlags.Expandable))
                children = GetChildren(child)
                Assert.Equal(8, children.Length) ' Should be 11. https://github.com/dotnet/roslyn/issues/13421
                child = children(children.Length - 1)
                Verify(child,
                    EvalResult(
                        "Rest",
                        "(15, 16, 17)",
                        "(Short, Short, Short)",
                        "o._17.Rest.Rest",
                        DkmEvaluationResultFlags.Expandable))
            End Using
        End Sub

        <WorkItem(13625, "https://github.com/dotnet/roslyn/issues/13625")>
        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/12347")>
        Public Sub Names_LongTuple()
            Const source =
"Class C
    Private F As (K As (A As Integer, D As (B As Integer, C As Integer), E As Integer, F As Integer, G As Integer, H As Integer, I As Integer, J As Integer), O As (L As Integer, M As Integer, N As Integer)) =
        ((1, (2, 3), 4, 5, 6, 7, 8, 9), (10, 11, 12))
End Class"
            Dim assembly0 = GenerateTupleAssembly()
            Dim reference0 = AssemblyMetadata.CreateFromImage(assembly0).GetReference()
            Dim compilation1 = CreateCompilationWithMscorlib({source}, options:=TestOptions.ReleaseDll, references:={reference0}, assemblyName:=GetUniqueName())
            Dim assembly1 = compilation1.EmitToArray()
            Dim runtime = New DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(ReflectionUtilities.Load(assembly0), ReflectionUtilities.Load(assembly1)))
            Using runtime.Load()
                Dim type = runtime.GetType("C")
                Dim value = type.Instantiate()
                Dim result = FormatResult("o", value)
                Verify(result,
                       EvalResult("o", "{C}", "C", "o", DkmEvaluationResultFlags.Expandable))
                Dim children = GetChildren(result)
                Verify(children,
                    EvalResult(
                        "F",
                        "((1, (2, 3), 4, 5, 6, 7, 8, 9), (10, 11, 12))",
                        "(K As (A As Integer, D As (B As Integer, C As Integer), E As Integer, F As Integer, G As Integer, H As Integer, I As Integer, J As Integer), O As (L As Integer, M As Integer, N As Integer))",
                        "o.F",
                        DkmEvaluationResultFlags.Expandable))
            End Using
        End Sub

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/12347")>
        Public Sub NamesFromTypeArguments()
            Const source =
"Class A(Of T, U)
    Private F As T
    Private G() As U = New U() { }
End Class
Class B(Of T)
    Friend Structure S
    End Structure
    Private F As (X As Object, Y As T)
End Class
Class C
    Private F As New A(Of (A As Object, B As Object)(), (C As Object, D As Object()))()
    Private G As New B(Of (E As Object, H As B(Of (F As Object, G As Object)).S))()
End Class"
            Dim assembly0 = GenerateTupleAssembly()
            Dim reference0 = AssemblyMetadata.CreateFromImage(assembly0).GetReference()
            Dim compilation1 = CreateCompilationWithMscorlib({source}, options:=TestOptions.ReleaseDll, references:={reference0}, assemblyName:=GetUniqueName())
            Dim assembly1 = compilation1.EmitToArray()
            Dim runtime = New DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(ReflectionUtilities.Load(assembly0), ReflectionUtilities.Load(assembly1)))
            Using runtime.Load()
                Dim type = runtime.GetType("C")
                Dim value = type.Instantiate()
                Dim result = FormatResult("o", value)
                Dim children = GetChildren(result)
                Verify(children,
                    EvalResult("F", "{A(Of (Object, Object)(), (Object, Object()))}", "A(Of (A As Object, B As Object)(), (Object, Object()))", "o.F", DkmEvaluationResultFlags.Expandable),
                    EvalResult("G", "{B(Of (Object, B(Of (Object, Object)).S))}", "B(Of (E As Object, H As B(Of (F As Object, G As Object)).S))", "o.G", DkmEvaluationResultFlags.Expandable))
                Dim moreChildren = GetChildren(children(0))
                Verify(moreChildren,
                    EvalResult("F", "Nothing", "(A As Object, B As Object)()", "o.F.F"),
                    EvalResult("G", "{Length=0}", "(C As Object, D As Object())()", "o.F.G"))
                moreChildren = GetChildren(children(1))
                Verify(moreChildren,
                    EvalResult("F", "(Nothing, (Nothing, {B(Of (Object, Object)).S}))", "(X As Object, Y As (E As Object, H As B(Of (F As Object, G As Object)).S))", "o.G.F", DkmEvaluationResultFlags.Expandable))
                moreChildren = GetChildren(moreChildren(0))
                Verify(moreChildren,
                    EvalResult("Item1", "Nothing", "Object", "o.G.F.Item1"),
                    EvalResult("Item2", "(Nothing, {B(Of (Object, Object)).S})", "(E As Object, H As B(Of (F As Object, G As Object)).S)", "o.G.F.Item2", DkmEvaluationResultFlags.Expandable))
            End Using
        End Sub

        <Fact>
        Public Sub Keywords()
            Const source =
"Namespace [Namespace]
    Structure [Structure]
    End Structure
End Namespace
Class [As]
    Shared Function F() As ([As] As [As], [Class] As [Namespace].[Structure])
        Return (Nothing, Nothing)
    End Function
    Private _f As Object = F()
End Class"
            Dim assembly0 = GenerateTupleAssembly()
            Dim reference0 = AssemblyMetadata.CreateFromImage(assembly0).GetReference()
            Dim compilation1 = CreateCompilationWithMscorlib({source}, options:=TestOptions.ReleaseDll, references:={reference0}, assemblyName:=GetUniqueName())
            Dim assembly1 = compilation1.EmitToArray()
            Dim runtime = New DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(ReflectionUtilities.Load(assembly0), ReflectionUtilities.Load(assembly1)))
            Using runtime.Load()
                Dim type = runtime.GetType("As")
                Dim value = type.Instantiate()
                Dim result = FormatResult("o", value)
                Verify(result,
                       EvalResult("o", "{As}", "As", "o", DkmEvaluationResultFlags.Expandable))
                Dim children = GetChildren(result)
                Verify(children,
                    EvalResult("_f", "(Nothing, {Namespace.Structure})", "Object {(As, Namespace.Structure)}", "o._f", DkmEvaluationResultFlags.Expandable))
                children = GetChildren(children(0))
                Verify(children,
                    EvalResult("Item1", "Nothing", "As", "DirectCast(o._f, ([As], [Namespace].[Structure])).Item1"),
                    EvalResult("Item2", "{Namespace.Structure}", "Namespace.Structure", "DirectCast(o._f, ([As], [Namespace].[Structure])).Item2"))
            End Using
        End Sub

        Private Shared Function GenerateTupleAssembly() As ImmutableArray(Of Byte)
            Const source =
"Namespace System
    Public Structure ValueTuple(Of T1)
        Public Item1 As T1
        Public Sub New(_1 As T1)
            Item1 = _1
        End Sub
    End Structure
    Public Structure ValueTuple(Of T1, T2)
        Public Item1 As T1
        Public Item2 As T2
        Public Sub New(_1 As T1, _2 As T2)
            Item1 = _1
            Item2 = _2
        End Sub
    End Structure
    Public Structure ValueTuple(Of T1, T2, T3)
        Public Item1 As T1
        Public Item2 As T2
        Public Item3 As T3
        Public Sub New(_1 As T1, _2 As T2, _3 As T3)
            Item1 = _1
            Item2 = _2
            Item3 = _3
        End Sub
    End Structure
    Public Structure ValueTuple(Of T1, T2, T3, T4)
        Public Item1 As T1
        Public Item2 As T2
        Public Item3 As T3
        Public Item4 As T4
        Public Sub New(_1 As T1, _2 As T2, _3 As T3, _4 As T4)
            Item1 = _1
            Item2 = _2
            Item3 = _3
            Item4 = _4
        End Sub
    End Structure
    Public Structure ValueTuple(Of T1, T2, T3, T4, T5)
        Public Item1 As T1
        Public Item2 As T2
        Public Item3 As T3
        Public Item4 As T4
        Public Item5 As T5
        Public Sub New(_1 As T1, _2 As T2, _3 As T3, _4 As T4, _5 As T5)
            Item1 = _1
            Item2 = _2
            Item3 = _3
            Item4 = _4
            Item5 = _5
        End Sub
    End Structure
    Public Structure ValueTuple(Of T1, T2, T3, T4, T5, T6)
        Public Item1 As T1
        Public Item2 As T2
        Public Item3 As T3
        Public Item4 As T4
        Public Item5 As T5
        Public Item6 As T6
        Public Sub New(_1 As T1, _2 As T2, _3 As T3, _4 As T4, _5 As T5, _6 As T6)
            Item1 = _1
            Item2 = _2
            Item3 = _3
            Item4 = _4
            Item5 = _5
            Item6 = _6
        End Sub
    End Structure
    Public Structure ValueTuple(Of T1, T2, T3, T4, T5, T6, T7)
        Public Item1 As T1
        Public Item2 As T2
        Public Item3 As T3
        Public Item4 As T4
        Public Item5 As T5
        Public Item6 As T6
        Public Item7 As T7
        Public Sub New(_1 As T1, _2 As T2, _3 As T3, _4 As T4, _5 As T5, _6 As T6, _7 As T7)
            Item1 = _1
            Item2 = _2
            Item3 = _3
            Item4 = _4
            Item5 = _5
            Item6 = _6
            Item7 = _7
        End Sub
    End Structure
    Public Structure ValueTuple(Of T1, T2, T3, T4, T5, T6, T7, T8)
        Public Item1 As T1
        Public Item2 As T2
        Public Item3 As T3
        Public Item4 As T4
        Public Item5 As T5
        Public Item6 As T6
        Public Item7 As T7
        Public Rest As T8
        Public Sub New(_1 As T1, _2 As T2, _3 As T3, _4 As T4, _5 As T5, _6 As T6, _7 As T7, _8 As T8)
            Item1 = _1
            Item2 = _2
            Item3 = _3
            Item4 = _4
            Item5 = _5
            Item6 = _6
            Item7 = _7
            Rest = _8
        End Sub
    End Structure
End Namespace
Namespace System.Runtime.CompilerServices
    Public Class TupleElementNamesAttribute
        Inherits Attribute
        Public Sub TupleElementNamesAttribute(names As String())
        End Sub
    End Class
End Namespace"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.ReleaseDll, assemblyName:=GetUniqueName())
            comp.VerifyDiagnostics()
            Return comp.EmitToArray()
        End Function

    End Class

End Namespace
