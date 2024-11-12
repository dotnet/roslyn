' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CSharp
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
        Public Sub LongTuple_NoNames()
            Const source =
"Class C
    Private _17 As (Short, Short, Short, Short, Short, Short, Short, Short, Short, Short, Short, Short, Short, Short, Short, Short, Short) =
        (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17)
End Class"
            Dim assembly0 = GenerateTupleAssembly()
            Dim reference0 = AssemblyMetadata.CreateFromImage(assembly0).GetReference()
            Dim compilation1 = CreateCompilationWithMscorlib40({source}, options:=TestOptions.ReleaseDll, references:={reference0}, assemblyName:=GetUniqueName())
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
                        DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.CanFavorite))
                children = GetChildren(children(0))
                Verify(children,
                    EvalResult("Item1", "1", "Short", "o._17.Item1"),
                    EvalResult("Item2", "2", "Short", "o._17.Item2"),
                    EvalResult("Item3", "3", "Short", "o._17.Item3"),
                    EvalResult("Item4", "4", "Short", "o._17.Item4"),
                    EvalResult("Item5", "5", "Short", "o._17.Item5"),
                    EvalResult("Item6", "6", "Short", "o._17.Item6"),
                    EvalResult("Item7", "7", "Short", "o._17.Item7"),
                    EvalResult("Item8", "8", "Short", "o._17.Rest.Item1"),
                    EvalResult("Item9", "9", "Short", "o._17.Rest.Item2"),
                    EvalResult("Item10", "10", "Short", "o._17.Rest.Item3"),
                    EvalResult("Item11", "11", "Short", "o._17.Rest.Item4"),
                    EvalResult("Item12", "12", "Short", "o._17.Rest.Item5"),
                    EvalResult("Item13", "13", "Short", "o._17.Rest.Item6"),
                    EvalResult("Item14", "14", "Short", "o._17.Rest.Item7"),
                    EvalResult("Item15", "15", "Short", "o._17.Rest.Rest.Item1"),
                    EvalResult("Item16", "16", "Short", "o._17.Rest.Rest.Item2"),
                    EvalResult("Item17", "17", "Short", "o._17.Rest.Rest.Item3"),
                    EvalResult(
                        "Raw View",
                        "(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17)",
                        "(Short, Short, Short, Short, Short, Short, Short, Short, Short, Short, Short, Short, Short, Short, Short, Short, Short)",
                        "o._17, raw",
                        DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly))
                children = GetChildren(children(children.Length - 1))
                Verify(children,
                    EvalResult("Item1", "1", "Short", "o._17.Item1"),
                    EvalResult("Item2", "2", "Short", "o._17.Item2"),
                    EvalResult("Item3", "3", "Short", "o._17.Item3"),
                    EvalResult("Item4", "4", "Short", "o._17.Item4"),
                    EvalResult("Item5", "5", "Short", "o._17.Item5"),
                    EvalResult("Item6", "6", "Short", "o._17.Item6"),
                    EvalResult("Item7", "7", "Short", "o._17.Item7"),
                    EvalResult("Rest", "(8, 9, 10, 11, 12, 13, 14, 15, 16, 17)", "(Short, Short, Short, Short, Short, Short, Short, Short, Short, Short)", "o._17.Rest, raw", DkmEvaluationResultFlags.Expandable))
                children = GetChildren(children(children.Length - 1))
                Verify(children,
                    EvalResult("Item1", "8", "Short", "o._17.Rest.Item1"),
                    EvalResult("Item2", "9", "Short", "o._17.Rest.Item2"),
                    EvalResult("Item3", "10", "Short", "o._17.Rest.Item3"),
                    EvalResult("Item4", "11", "Short", "o._17.Rest.Item4"),
                    EvalResult("Item5", "12", "Short", "o._17.Rest.Item5"),
                    EvalResult("Item6", "13", "Short", "o._17.Rest.Item6"),
                    EvalResult("Item7", "14", "Short", "o._17.Rest.Item7"),
                    EvalResult("Rest", "(15, 16, 17)", "(Short, Short, Short)", "o._17.Rest.Rest, raw", DkmEvaluationResultFlags.Expandable))
                children = GetChildren(children(children.Length - 1))
                Verify(children,
                    EvalResult("Item1", "15", "Short", "o._17.Rest.Rest.Item1"),
                    EvalResult("Item2", "16", "Short", "o._17.Rest.Rest.Item2"),
                    EvalResult("Item3", "17", "Short", "o._17.Rest.Rest.Item3"))
            End Using
        End Sub

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/13625")>
        Public Sub Names_LongTuple()
            Const source =
"class C
{
    ((int A, (int B, int C) D, int E, int F, int G, int H, int I, int J) K, (int L, int M, int N) O) F =
        ((1, (2, 3), 4, 5, 6, 7, 8, 9), (10, 11, 12));
}"
            Dim assembly0 = GenerateTupleAssembly()
            Dim reference0 = AssemblyMetadata.CreateFromImage(assembly0).GetReference()
            Dim compilation1 = CreateCSharpCompilation(source, references:={TestBase.MscorlibRef, TestBase.SystemCoreRef, reference0})
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
                        DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.CanFavorite))
            End Using
        End Sub

        <Fact>
        Public Sub NamesFromTypeArguments()
            Const source =
"class A<T, U>
{
    T F;
    U[] G = new U[0];
}
class B<T>
{
    internal struct S { }
    (dynamic X, T Y) F = (null, default(T));
}
class C
{
    A<(dynamic A, object B)[], (object C, dynamic[] D)> F = new A<(dynamic A, object B)[], (object, dynamic[])>();
    B<(object E, B<(object F, dynamic G)>.S H)> G = new B<(object E, B<(object F, dynamic G)>.S H)>();
}"
            Dim assembly0 = GenerateTupleAssembly()
            Dim reference0 = AssemblyMetadata.CreateFromImage(assembly0).GetReference()
            Dim compilation1 = CreateCSharpCompilation(source, references:={TestBase.MscorlibRef, TestBase.SystemCoreRef, reference0})
            Dim assembly1 = compilation1.EmitToArray()
            Dim runtime = New DkmClrRuntimeInstance(ReflectionUtilities.GetMscorlib(ReflectionUtilities.Load(assembly0), ReflectionUtilities.Load(assembly1)))
            Using runtime.Load()
                Dim type = runtime.GetType("C")
                Dim value = type.Instantiate()
                Dim result = FormatResult("o", value)
                Dim children = GetChildren(result)
                Verify(children,
                    EvalResult("F", "{A(Of (Object, Object)(), (Object, Object()))}", "A(Of (A As Object, B As Object)(), (C As Object, D As Object()))", "o.F", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.CanFavorite),
                    EvalResult("G", "{B(Of (Object, B(Of (Object, Object)).S))}", "B(Of (E As Object, H As B(Of (F As Object, G As Object)).S))", "o.G", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.CanFavorite))
                Dim moreChildren = GetChildren(children(0))
                Verify(moreChildren,
                    EvalResult("F", "Nothing", "(A As Object, B As Object)()", "o.F.F", DkmEvaluationResultFlags.CanFavorite),
                    EvalResult("G", "{Length=0}", "(C As Object, D As Object())()", "o.F.G", DkmEvaluationResultFlags.CanFavorite))
                moreChildren = GetChildren(children(1))
                Verify(moreChildren,
                    EvalResult("F", "(Nothing, (Nothing, {B(Of (Object, Object)).S}))", "(X As Object, Y As (E As Object, H As B(Of (F As Object, G As Object)).S))", "o.G.F", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.CanFavorite))
                moreChildren = GetChildren(moreChildren(0))
                Verify(moreChildren,
                    EvalResult("X", "Nothing", "Object", "o.G.F.Item1"),
                    EvalResult("Y", "(Nothing, {B(Of (Object, Object)).S})", "(E As Object, H As B(Of (F As Object, G As Object)).S)", "o.G.F.Item2", DkmEvaluationResultFlags.Expandable),
                    EvalResult("Raw View", "(Nothing, (Nothing, {B(Of (Object, Object)).S}))", "(X As Object, Y As (E As Object, H As B(Of (F As Object, G As Object)).S))", "o.G.F, raw", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly))
                moreChildren = GetChildren(moreChildren(1))
                Verify(moreChildren,
                    EvalResult("E", "Nothing", "Object", "o.G.F.Item2.Item1"),
                    EvalResult("H", "{B(Of (Object, Object)).S}", "B(Of (F As Object, G As Object)).S", "o.G.F.Item2.Item2"),
                    EvalResult("Raw View", "(Nothing, {B(Of (Object, Object)).S})", "(E As Object, H As B(Of (F As Object, G As Object)).S)", "o.G.F.Item2, raw", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.ReadOnly))
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
            Dim compilation1 = CreateCompilationWithMscorlib40({source}, options:=TestOptions.ReleaseDll, references:={reference0}, assemblyName:=GetUniqueName())
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
                    EvalResult("_f", "(Nothing, {Namespace.Structure})", "Object {(As, Namespace.Structure)}", "o._f", DkmEvaluationResultFlags.Expandable Or DkmEvaluationResultFlags.CanFavorite))
                children = GetChildren(children(0))
                Verify(children,
                    EvalResult("Item1", "Nothing", "As", "DirectCast(o._f, ([As], [Namespace].[Structure])).Item1"),
                    EvalResult("Item2", "{Namespace.Structure}", "Namespace.Structure", "DirectCast(o._f, ([As], [Namespace].[Structure])).Item2"))
            End Using
        End Sub

        Private Shared Function CreateCSharpCompilation(source As String, references As IEnumerable(Of MetadataReference)) As CSharpCompilation
            Dim tree = CSharpSyntaxTree.ParseText(source)
            Dim options = New CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, concurrentBuild:=False)
            Return CSharpCompilation.Create(GetUniqueName(), {tree}, references, options)
        End Function

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
        Public Sub New(names As String())
        End Sub
    End Class
End Namespace"
            Dim comp = CreateCompilationWithMscorlib40({source}, options:=TestOptions.ReleaseDll, assemblyName:=GetUniqueName())
            comp.VerifyDiagnostics()
            Return comp.EmitToArray()
        End Function

    End Class

End Namespace
