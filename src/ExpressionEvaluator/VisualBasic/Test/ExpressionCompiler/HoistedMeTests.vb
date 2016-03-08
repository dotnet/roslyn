' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests
Imports Roslyn.Test.PdbUtilities
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.UnitTests
    Public Class HoistedMeTests
        Inherits ExpressionCompilerTestBase

        <WorkItem(1067379, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1067379")>
        <Fact>
        Public Sub InstanceIterator_NoCapturing()
            Const source = "
Class C
    Iterator Function F() As System.Collections.IEnumerable
        Yield 1
    End Function
End Class
"
            Const expectedIL = "
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Boolean V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_1_F.$VB$Me As C""
  IL_0006:  ret
}
"
            VerifyHasMe(source, "C.VB$StateMachine_1_F.MoveNext", "C", expectedIL)
        End Sub

        <WorkItem(1067379, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1067379")>
        <Fact>
        Public Sub InstanceAsync_NoCapturing()
            Const source = "
Imports System
Imports System.Threading.Tasks

Class C
    Async Function F() As Task
        Await Console.Out.WriteLineAsync(""a"")
    End Function
End Class
"
            Const expectedIL = "
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C.VB$StateMachine_1_F V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_1_F.$VB$Me As C""
  IL_0006:  ret
}
"
            VerifyHasMe(source, "C.VB$StateMachine_1_F.MoveNext", "C", expectedIL)
        End Sub

        <WorkItem(1067379, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1067379")>
        <Fact>
        Public Sub InstanceLambda_NoCapturing()
            Const source = "
Class C
    Sub M()
        Dim a As System.Action = Sub() Equals(1, 2)
        a()
    End Sub
End Class
"
            ' This test documents the fact that, as in dev12, "Me"
            ' is unavailable while stepping through the lambda.  It
            ' would be preferable if it were.
            VerifyNoMe(source, "C._Closure$__._Lambda$__1-0")
        End Sub

        <Fact()>
        Public Sub InstanceLambda_NoCapturingExceptThis()
            Const source = "
Class C
    Sub M()
        Dim a As System.Action = Sub() Equals(Me.GetHashCode(), 2)
        a()
    End Sub
End Class
"
            Const expectedIL = "
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}
"
            VerifyHasMe(source, "C._Lambda$__1-0", "C", expectedIL)
        End Sub

        <Fact>
        Public Sub InstanceIterator_CapturedMe()
            Const source = "
Class C
    Iterator Function F() As System.Collections.IEnumerable
        Yield Me
    End Function
End Class
"
            Const expectedIL = "
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Boolean V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_1_F.$VB$Me As C""
  IL_0006:  ret
}
"
            VerifyHasMe(source, "C.VB$StateMachine_1_F.MoveNext", "C", expectedIL)
        End Sub

        <Fact>
        Public Sub InstanceAsync_CapturedMe()
            Const source = "
Imports System
Imports System.Threading.Tasks

Class C
    Async Function F() As Task
        Await Console.Out.WriteLineAsync(Me.ToString())
    End Function
End Class
"
            Const expectedIL = "
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C.VB$StateMachine_1_F V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_1_F.$VB$Me As C""
  IL_0006:  ret
}
"
            VerifyHasMe(source, "C.VB$StateMachine_1_F.MoveNext", "C", expectedIL)
        End Sub

        <Fact()>
        Public Sub InstanceLambda_CapturedMe_DisplayClass()
            Const source = "
Class C
    Private x As Integer

    Sub M(y As Integer)
        Dim a As System.Action = Sub() Equals(x, y)
        a()
    End Sub
End Class
"
            Const expectedIL = "
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C._Closure$__2-0.$VB$Me As C""
  IL_0006:  ret
}
"
            VerifyHasMe(source, "C._Closure$__2-0._Lambda$__0", "C", expectedIL)
        End Sub

        <Fact()>
        Public Sub InstanceLambda_CapturedMe_NoDisplayClass()
            Const source = "
Class C
    Private x As Integer

    Sub M()
        Dim a As System.Action = Sub() Equals(x, 1)
        a()
    End Sub
End Class
"
            Const expectedIL = "
{
  // Code size        2 (0x2)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ret
}
"
            VerifyHasMe(source, "C._Lambda$__2-0", "C", expectedIL)
        End Sub

        <Fact>
        Public Sub InstanceIterator_Generic()
            Const source = "
Class C(Of T)
    Iterator Function F(Of U)() As System.Collections.IEnumerable
        Yield Me
    End Function
End Class
"
            Const expectedIL = "
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Boolean V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C(Of T).VB$StateMachine_1_F(Of U).$VB$Me As C(Of T)""
  IL_0006:  ret
}
"
            VerifyHasMe(source, "C.VB$StateMachine_1_F.MoveNext", "C(Of T)", expectedIL)
        End Sub

        <Fact>
        Public Sub InstanceAsync_Generic()
            Const source = "
Imports System
Imports System.Threading.Tasks

Class C(Of T)
    Async Function F(Of U)() As Task
        Await Console.Out.WriteLineAsync(Me.ToString())
    End Function
End Class
"
            Const expectedIL = "
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C(Of T).VB$StateMachine_1_F(Of U) V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C(Of T).VB$StateMachine_1_F(Of U).$VB$Me As C(Of T)""
  IL_0006:  ret
}
"
            VerifyHasMe(source, "C.VB$StateMachine_1_F.MoveNext", "C(Of T)", expectedIL)
        End Sub

        <Fact()>
        Public Sub InstanceLambda_Generic()
            Const source = "
Class C(Of T)
    Private x As Integer

    Sub M(Of U)(y As Integer)
        Dim a As System.Action = Sub() Equals(x, y)
        a()
    End Sub
End Class
"
            Const expectedIL = "
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C(Of T)._Closure$__2-0(Of $CLS0).$VB$Me As C(Of T)""
  IL_0006:  ret
}
"
            VerifyHasMe(source, "C._Closure$__2-0._Lambda$__0", "C(Of T)", expectedIL)
        End Sub

        ' Note: Not actually an issue in VB, since the name isn't mangled.
        <Fact>
        Public Sub InstanceIterator_ExplicitInterfaceImplementation()
            Const source = "
Interface I
    Function F() As System.Collections.IEnumerable
End Interface

Class C : Implements I
    Iterator Function F() As System.Collections.IEnumerable Implements I.F
        Yield Me
    End Function
End Class
"
            Const expectedIL = "
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Boolean V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_1_F.$VB$Me As C""
  IL_0006:  ret
}
"
            VerifyHasMe(source, "C.VB$StateMachine_1_F.MoveNext", "C", expectedIL)
        End Sub

        ' Note: Not actually an issue in VB, since the name isn't mangled.
        <Fact>
        Public Sub InstanceAsync_ExplicitInterfaceImplementation()
            Const source = "
Imports System
Imports System.Threading.Tasks

Interface I
    Function F() As Task
End Interface

Class C : Implements I
    Async Function F() As Task Implements I.F
        Await Console.Out.WriteLineAsync(Me.ToString())
    End Function
End Class
"
            Const expectedIL = "
{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C.VB$StateMachine_1_F V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_1_F.$VB$Me As C""
  IL_0006:  ret
}
"
            VerifyHasMe(source, "C.VB$StateMachine_1_F.MoveNext", "C", expectedIL)
        End Sub

        <Fact()>
        Public Sub InstanceLambda_ExplicitInterfaceImplementation()
            Const source = "
Interface I
    Sub M(y As Integer)
End Interface

Class C : Implements I
    Private x As Integer

    Sub M(y As Integer) Implements I.M
        Dim a As System.Action = Sub() Equals(x, y)
        a()
    End Sub
End Class
"
            Const expectedIL = "
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C._Closure$__2-0.$VB$Me As C""
  IL_0006:  ret
}
"
            VerifyHasMe(source, "C._Closure$__2-0._Lambda$__0", "C", expectedIL)
        End Sub

        <Fact>
        Public Sub SharedIterator()
            Const source = "
Module M
    Iterator Function F() As System.Collections.IEnumerable
        Yield 1
    End Function
End Module
"
            VerifyNoMe(source, "M.VB$StateMachine_0_F.MoveNext")
        End Sub

        <Fact>
        Public Sub SharedAsync()
            Const source = "
Imports System
Imports System.Threading.Tasks

Module M
    Async Function F() As Task
        Await Console.Out.WriteLineAsync(""A"")
    End Function
End Module
"
            VerifyNoMe(source, "M.VB$StateMachine_0_F.MoveNext")
        End Sub

        <Fact()>
        Public Sub SharedLambda()
            Const source = "
Module M
    Sub M(y As Integer)
        Dim a As System.Action = Sub() Equals(1, y)
        a()
    End Sub
End Module
"
            VerifyNoMe(source, "M._Closure$__0-0._Lambda$__0")
        End Sub

        <Fact>
        Public Sub ExtensionIterator()
            Const source = "
Module M
    <System.Runtime.CompilerServices.Extension>
    Iterator Function F(x As Integer) As System.Collections.IEnumerable
        Yield x
    End Function
End Module
"
            VerifyNoMe(source, "M.VB$StateMachine_0_F.MoveNext")
        End Sub

        <Fact>
        Public Sub ExtensionAsync()
            Const source = "
Imports System
Imports System.Threading.Tasks

Module M
    <System.Runtime.CompilerServices.Extension>
    Async Function F(x As Integer) As Task
        Await Console.Out.WriteLineAsync(""A"")
    End Function
End Module
"
            VerifyNoMe(source, "M.VB$StateMachine_0_F.MoveNext")
        End Sub

        <Fact()>
        Public Sub ExtensionLambda()
            Const source = "
Module M
    <System.Runtime.CompilerServices.Extension>
    Sub M(y As Integer)
        Dim a As System.Action = Sub() Equals(1, y)
        a()
    End Sub
End Module
"
            VerifyNoMe(source, "M._Closure$__0-0._Lambda$__0")
        End Sub

        <WorkItem(1072296, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1072296")>
        <Fact>
        Public Sub OldStyleNonCapturingLambda()
            Const ilSource = "
.class public auto ansi C
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .method public instance void  M() cil managed
  {
    ldnull
    throw
  }

  .method private specialname static int32 
          _Lambda$__1() cil managed
  {
    ldnull
    throw
  }

} // end of class C
"

            Dim ilModule = ExpressionCompilerTestHelpers.GetModuleInstanceForIL(ilSource)
            Dim runtime = CreateRuntimeInstance(ilModule, {MscorlibRef})
            Dim context = CreateMethodContext(runtime, "C._Lambda$__1")
            VerifyNoMe(context)
        End Sub

        <WorkItem(1067379, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1067379")>
        <WorkItem(1069554, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1069554")>
        <Fact>
        Public Sub LambdaLocations_HasThis()
            Const source = "
Imports System

Class C
    Private _toBeCaptured As Integer

    Private _f As Integer = (Function(x) (Function() _toBeCaptured + x + 1)() + 1)(1)

    Public Sub New()
        Dim l As Integer = (Function(x) (Function() _toBeCaptured + x + 2)() + 1)(1)
    End Sub

    Protected Overrides Sub Finalize()
        Dim l As Integer = (Function(x) (Function() _toBeCaptured + x + 3)() + 1)(1)
    End Sub

    Public Property P As Integer
        Get
            Return (Function(x) (Function() _toBeCaptured + x + 4)() + 1)(1)
        End Get
        Set(value As Integer)
            value = (Function(x) (Function() _toBeCaptured + x + 5)() + 1)(1)
        End Set
    End Property

    Public Custom Event E As Action
        AddHandler(value As Action)
            Dim l As Integer = (Function(x) (Function() _toBeCaptured + x + 6)() + 1)(1)
        End AddHandler
        RemoveHandler(value As Action)
            Dim l As Integer = (Function(x) (Function() _toBeCaptured + x + 7)() + 1)(1)
        End RemoveHandler
        RaiseEvent()
            Dim l As Integer = (Function(x) (Function() _toBeCaptured + x + 8)() + 1)(1)
        End RaiseEvent
    End Event
End Class
"

            Const expectedILTemplate = "
{{
  // Code size        7 (0x7)
  .maxstack  1
  .locals init (Object V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.{0}.$VB$Me As C""
  IL_0006:  ret
}}
"

            Dim comp = CreateCompilationWithReferences({VisualBasicSyntaxTree.ParseText(source)}, {MscorlibRef, MsvbRef}, TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim compOptions = TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All)
                    Dim dummyComp = CreateCompilationWithMscorlibAndVBRuntimeAndReferences((<Compilation/>), {comp.EmitToImageReference()}, compOptions)
                    Dim typeC = dummyComp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
                    Dim displayClassTypes = typeC.GetMembers().OfType(Of NamedTypeSymbol)()
                    Assert.True(displayClassTypes.Any())
                    For Each displayClassType In displayClassTypes
                        Dim displayClassName = displayClassType.Name
                        Assert.True(displayClassName.StartsWith(StringConstants.DisplayClassPrefix, StringComparison.Ordinal))
                        For Each displayClassMethod In displayClassType.GetMembers().OfType(Of MethodSymbol)().Where(AddressOf IsLambda)
                            Dim lambdaMethodName = String.Format("C.{0}.{1}", displayClassName, displayClassMethod.Name)
                            Dim context = CreateMethodContext(runtime, lambdaMethodName)
                            Dim expectedIL = String.Format(expectedILTemplate, displayClassName)
                            VerifyHasMe(context, "C", expectedIL)
                        Next
                    Next
                End Sub)
        End Sub

        <WorkItem(1069554, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1069554")>
        <Fact>
        Public Sub LambdaLocations_NoThis()
            Const source = "
Imports System

Module M
    Private _f As Integer = (Function(x) (Function() x + 1)() + 1)(1)

    Sub New()
        Dim l As Integer = (Function(x) (Function() x + 2)() + 1)(1)
    End Sub

    Public Property P As Integer
        Get
            Return (Function(x) (Function() x + 3)() + 1)(1)
        End Get
        Set(value As Integer)
            value = (Function(x) (Function() x + 4)() + 1)(1)
        End Set
    End Property

    Public Custom Event E As Action
        AddHandler(value As Action)
            Dim l As Integer = (Function(x) (Function() x + 5)() + 1)(1)
        End AddHandler
        RemoveHandler(value As Action)
            Dim l As Integer = (Function(x) (Function() x + 6)() + 1)(1)
        End RemoveHandler
        RaiseEvent()
            Dim l As Integer = (Function(x) (Function() x + 7)() + 1)(1)
        End RaiseEvent
    End Event
End Module
"

            Dim comp = CreateCompilationWithReferences({VisualBasicSyntaxTree.ParseText(source)}, {MscorlibRef, MsvbRef}, TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)

                    Dim dummyComp As VisualBasicCompilation = MakeDummyCompilation(comp)
                    Dim typeC = dummyComp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("M")
                    Dim displayClassTypes = typeC.GetMembers().OfType(Of NamedTypeSymbol)()
                    Assert.True(displayClassTypes.Any())
                    For Each displayClassType In displayClassTypes
                        Dim displayClassName = displayClassType.Name
                        Assert.True(displayClassName.StartsWith(StringConstants.DisplayClassPrefix, StringComparison.Ordinal))
                        For Each displayClassMethod In displayClassType.GetMembers().OfType(Of MethodSymbol)().Where(AddressOf IsLambda)
                            Dim lambdaMethodName = String.Format("M.{0}.{1}", displayClassName, displayClassMethod.Name)
                            Dim context = CreateMethodContext(runtime, lambdaMethodName)
                            VerifyNoMe(context)
                        Next
                    Next
                End Sub)
        End Sub

        Private Sub VerifyHasMe(source As String, moveNextMethodName As String, expectedType As String, expectedIL As String)
            Dim sourceCompilation = CreateCompilationWithReferences(
                {VisualBasicSyntaxTree.ParseText(source)},
                {MscorlibRef_v4_0_30316_17626, SystemRef_v4_0_30319_17929, MsvbRef_v4_0_30319_17929},
                TestOptions.DebugDll)

            WithRuntimeInstance(sourceCompilation,
                Sub(runtime)
                    VerifyHasMe(CreateMethodContext(runtime, moveNextMethodName), expectedType, expectedIL)
                End Sub)

            ' Now recompile and test CompileExpression with optimized code.
            WithRuntimeInstance(sourceCompilation.WithOptions(sourceCompilation.Options.WithOptimizationLevel(OptimizationLevel.Release)),
                Sub(runtime)
                    ' In VB, "Me" is never optimized away.
                    VerifyHasMe(CreateMethodContext(runtime, moveNextMethodName), expectedType, expectedIL:=Nothing)
                End Sub)
        End Sub

        Private Sub VerifyHasMe(context As EvaluationContext, expectedType As String, expectedIL As String)
            Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
            Dim typeName As String = Nothing
            Dim testData As New CompilationTestData()
            Dim assembly = context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)
            Assert.NotNull(assembly)
            Assert.NotEqual(0, assembly.Count)
            Dim localAndMethod = locals.Single(Function(l) l.LocalName = "Me")
            If expectedIL IsNot Nothing Then
                VerifyMethodData(testData.Methods.Single(Function(m) m.Key.Contains(localAndMethod.MethodName)).Value, expectedType, expectedIL)
            End If
            locals.Free()

            Dim errorMessage As String = Nothing
            testData = New CompilationTestData()
            context.CompileExpression("Me", errorMessage, testData)
            Assert.Null(errorMessage)
            If expectedIL IsNot Nothing Then
                VerifyMethodData(testData.Methods.Single(Function(m) m.Key.Contains("<>m0")).Value, expectedType, expectedIL)
            End If
        End Sub

        Private Shared Sub VerifyMethodData(methodData As CompilationTestData.MethodData, expectedType As String, expectedIL As String)
            methodData.VerifyIL(expectedIL)
            Dim method As MethodSymbol = DirectCast(methodData.Method, MethodSymbol)
            VerifyTypeParameters(method)
            Assert.Equal(expectedType, method.ReturnType.ToTestDisplayString())
        End Sub

        Private Sub VerifyNoMe(source As String, moveNextMethodName As String)
            Dim comp = CreateCompilationWithReferences(
                {VisualBasicSyntaxTree.ParseText(source)},
                {MscorlibRef_v4_0_30316_17626, SystemRef_v4_0_30319_17929, MsvbRef_v4_0_30319_17929},
                TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    VerifyNoMe(CreateMethodContext(runtime, moveNextMethodName))
                End Sub)
        End Sub

        Private Shared Sub VerifyNoMe(context As EvaluationContext)
            Dim locals = ArrayBuilder(Of LocalAndMethod).GetInstance()
            Dim typeName As String = Nothing
            Dim testData As New CompilationTestData()
            Dim assembly = context.CompileGetLocals(locals, argumentsOnly:=False, typeName:=typeName, testData:=testData)
            Assert.NotNull(assembly)
            AssertEx.None(locals, Function(l) l.LocalName.Contains("Me"))
            locals.Free()

            Dim errorMessage As String = Nothing
            testData = New CompilationTestData()
            context.CompileExpression("Me", errorMessage, testData)
            Assert.Contains(errorMessage,
                            {
                                "error BC32001: 'Me' is not valid within a Module.",
                                "error BC30043: 'Me' is valid only within an instance method."
                            })

            testData = New CompilationTestData()
            context.CompileExpression("MyBase.ToString()", errorMessage, testData)
            Assert.Contains(errorMessage,
                            {
                                "error BC32001: 'MyBase' is not valid within a Module.",
                                "error BC30043: 'MyBase' is valid only within an instance method."
                            })

            testData = New CompilationTestData()
            context.CompileExpression("MyClass.ToString()", errorMessage, testData)
            Assert.Contains(errorMessage,
                            {
                                "error BC30470: 'MyClass' cannot be used outside of a class.",
                                "error BC32001: 'MyClass' is not valid within a Module.",
                                "error BC30043: 'MyClass' is valid only within an instance method."
                            })
        End Sub

        <WorkItem(1024137, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1024137")>
        <Fact>
        Public Sub InstanceMembersInIterator()
            Const source = "
Class C
    Private x As Object

    Iterator Function F() As System.Collections.IEnumerable
        Yield Me.x
    End Function
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.VB$StateMachine_2_F.MoveNext")

                    Dim resultProperties As ResultProperties = Nothing
                    Dim errorMessage As String = Nothing
                    Dim testData = New CompilationTestData()
                    context.CompileExpression("Me.x", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (Boolean V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_2_F.$VB$Me As C""
  IL_0006:  ldfld      ""C.x As Object""
  IL_000b:  ret
}
")
                End Sub)
        End Sub

        <WorkItem(1024137, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1024137")>
        <Fact>
        Public Sub InstanceMembersInLambda()
            Const source = "
Class C
    Private x As Object

    Sub F()
        Dim a As System.Action = Sub() Me.x.ToString()
        a()
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C._Lambda$__2-0")

                    Dim resultProperties As ResultProperties = Nothing
                    Dim errorMessage As String = Nothing
                    Dim testData = New CompilationTestData()
                    context.CompileExpression("Me.x", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.x As Object""
  IL_0006:  ret
}
")
                End Sub)
        End Sub

        <WorkItem(1024137, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1024137")>
        <Fact>
        Public Sub InstanceMembersInAsync()
            Const source = "
Imports System
Imports System.Threading.Tasks

Class C
    Private x As Object

    Async Function F() As Task
        Await Console.Out.WriteLineAsync(Me.ToString())
    End Function
End Class
"
            Dim comp = CreateCompilationWithReferences(
                {VisualBasicSyntaxTree.ParseText(source)},
                {MscorlibRef_v4_0_30316_17626, SystemRef_v4_0_30319_17929, MsvbRef_v4_0_30319_17929},
                TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "C.VB$StateMachine_2_F.MoveNext")

                    Dim resultProperties As ResultProperties = Nothing
                    Dim errorMessage As String = Nothing
                    Dim testData = New CompilationTestData()
                    context.CompileExpression("Me.x", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                C.VB$StateMachine_2_F V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C.VB$StateMachine_2_F.$VB$Me As C""
  IL_0006:  ldfld      ""C.x As Object""
  IL_000b:  ret
}
")
                End Sub)
        End Sub

        <Fact>
        Public Sub BaseMembersInIterator()
            Const source = "
Class Base
    Protected x As Integer
End Class

Class Derived : Inherits Base
    Shadows Protected x As Object

    Iterator Function F() As System.Collections.IEnumerable
        Yield MyBase.x
    End Function
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "Derived.VB$StateMachine_2_F.MoveNext")

                    Dim resultProperties As ResultProperties = Nothing
                    Dim errorMessage As String = Nothing
                    Dim testData = New CompilationTestData()
                    context.CompileExpression("MyBase.x", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (Boolean V_0,
                Integer V_1)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""Derived.VB$StateMachine_2_F.$VB$Me As Derived""
  IL_0006:  ldfld      ""Base.x As Integer""
  IL_000b:  ret
}
")
                End Sub)
        End Sub

        <Fact>
        Public Sub BaseMembersInAsync()
            Const source = "
Imports System
Imports System.Threading.Tasks

Class Base
    Protected x As Integer
End Class

Class Derived : Inherits Base
    Shadows Protected x As Object

    Async Function F() As Task
        Await Console.Out.WriteLineAsync(Me.ToString())
    End Function
End Class
"
            Dim comp = CreateCompilationWithReferences(
                {VisualBasicSyntaxTree.ParseText(source)},
                {MscorlibRef_v4_0_30316_17626, SystemRef_v4_0_30319_17929, MsvbRef_v4_0_30319_17929},
                TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "Derived.VB$StateMachine_2_F.MoveNext")

                    Dim resultProperties As ResultProperties = Nothing
                    Dim errorMessage As String = Nothing
                    Dim testData = New CompilationTestData()
                    context.CompileExpression("MyBase.x", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size       12 (0xc)
  .maxstack  1
  .locals init (Integer V_0,
                System.Runtime.CompilerServices.TaskAwaiter V_1,
                Derived.VB$StateMachine_2_F V_2,
                System.Exception V_3)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""Derived.VB$StateMachine_2_F.$VB$Me As Derived""
  IL_0006:  ldfld      ""Base.x As Integer""
  IL_000b:  ret
}
")
                End Sub)
        End Sub

        <Fact>
        Public Sub BaseMembersInLambda()
            Const source = "
Class Base
    Protected x As Integer
End Class

Class Derived : Inherits Base
    Shadows Protected x As Object

    Sub F()
        Dim a As System.Action = Sub() Me.x.ToString()
        a()
    End Sub
End Class
"
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            WithRuntimeInstance(comp,
                Sub(runtime)
                    Dim context = CreateMethodContext(runtime, "Derived._Lambda$__2-0")

                    Dim resultProperties As ResultProperties = Nothing
                    Dim errorMessage As String = Nothing
                    Dim testData = New CompilationTestData()
                    context.CompileExpression("MyBase.x", errorMessage, testData)
                    Assert.Null(errorMessage)
                    testData.GetMethodData("<>x.<>m0").VerifyIL("
{
  // Code size        7 (0x7)
  .maxstack  1
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""Base.x As Integer""
  IL_0006:  ret
}
")
                End Sub)
        End Sub

        <Fact>
        Public Sub IteratorOverloading_Parameters1()
            Const source = "
Imports System.Collections

Public Class C
    Public Iterator Function M() As IEnumerable
        Yield Me
    End Function

    Public Function M(x As Integer) As IEnumerable
        Return Nothing
    End Function
End Class
"
            CheckIteratorOverloading(source, Function(m) m.ParameterCount = 0)
        End Sub

        <Fact>
        Public Sub IteratorOverloading_Parameters2() ' Same as above, but declarations reversed.
            Const source = "
Imports System.Collections

Public Class C
    Public Function M(x As Integer) As IEnumerable
        Return Nothing
    End Function

    Public Iterator Function M() As IEnumerable
        Yield Me
    End Function
End Class
"
            ' NB: We pick the wrong overload, but it doesn't matter because 
            ' the methods have the same characteristics.
            ' Also, we don't require this behavior, we're just documenting it.
            CheckIteratorOverloading(source, Function(m) m.ParameterCount = 1)
        End Sub

        <Fact>
        Public Sub IteratorOverloading_Sharedness()
            Const source = "
Imports System.Collections

Public Class C
    Public Shared Function M(x As Integer) As IEnumerable
        Return Nothing
    End Function

    ' NB: We declare the interesting overload last so we know we're not
    ' just picking the first one by mistake.
    Public Iterator Function M() As IEnumerable
        Yield Me
    End Function
End Class
"
            CheckIteratorOverloading(source, Function(m) Not m.IsShared)
        End Sub

        <Fact>
        Public Sub IteratorOverloading_MustOverrideness()
            Const source = "
Imports System.Collections

Public MustInherit Class C
    Public MustOverride Function M(x As Integer) As IEnumerable

    ' NB: We declare the interesting overload last so we know we're not
    ' just picking the first one by mistake.
    Public Iterator Function M() As IEnumerable
        Yield Me
    End Function
End Class
"
            CheckIteratorOverloading(source, Function(m) Not m.IsMustOverride)
        End Sub

        <Fact>
        Public Sub IteratorOverloading_Arity1()
            Const source = "
Imports System.Collections

Public Class C
    Public Function M(Of T)(x As Integer) As IEnumerable
        Return Nothing
    End Function

    ' NB: We declare the interesting overload last so we know we're not
    ' just picking the first one by mistake.
    Public Iterator Function M() As IEnumerable
        Yield Me
    End Function
End Class
"
            CheckIteratorOverloading(source, Function(m) m.Arity = 0)
        End Sub

        <Fact>
        Public Sub IteratorOverloading_Arity2()
            Const source = "
Imports System.Collections

Public Class C
    Public Function M(x As Integer) As IEnumerable
        Return Nothing
    End Function

    ' NB: We declare the interesting overload last so we know we're not
    ' just picking the first one by mistake.
    Public Iterator Function M(Of T)() As IEnumerable
        Yield Me
    End Function
End Class
"
            CheckIteratorOverloading(source, Function(m) m.Arity = 1)
        End Sub

        <Fact>
        Public Sub IteratorOverloading_Constraints1()
            Const source = "
Imports System.Collections

Public Class C
    Public Function M(Of T As Structure)(x As Integer) As IEnumerable
        Return Nothing
    End Function

    ' NB: We declare the interesting overload last so we know we're not
    ' just picking the first one by mistake.
    Public Iterator Function M(Of T As Class)() As IEnumerable
        Yield Me
    End Function
End Class
"
            CheckIteratorOverloading(source, Function(m) m.TypeParameters.Single().HasReferenceTypeConstraint)
        End Sub

        <Fact>
        Public Sub IteratorOverloading_Constraints2()
            Const source = "
Imports System.Collections
Imports System.Collections.Generic

Public Class C
    ' NB: We declare the interesting overload last so we know we're not
    ' just picking the first one by mistake.
    Public Iterator Function M(Of T As IEnumerable(Of U), U As Class)() As IEnumerable
        Yield Me
    End Function
End Class
"
            ' NOTE: this isn't the feature we're switching on, but it is a convenient differentiator.
            CheckIteratorOverloading(source, Function(m) m.ParameterCount = 0)
        End Sub

        <Fact>
        Public Sub LambdaOverloading_NonGeneric()
            Const source = "
Public Class C
    Public Sub M(x As Integer)
        Dim a As System.Action = Sub() x.ToString()
    End Sub
End Class
"
            ' Note: We're picking the first method with the correct generic arity, etc.
            CheckLambdaOverloading(source, Function(m) m.MethodKind = MethodKind.Constructor)
        End Sub

        <Fact>
        Public Sub LambdaOverloading_Generic()
            Const source = "
Public Class C
    Public Sub M(Of T)(x As Integer)
        Dim a As System.Action = Sub() x.ToString()
    End Sub
End Class
"
            CheckLambdaOverloading(source, Function(m) m.Arity = 1)
        End Sub

        Private Shared Sub CheckIteratorOverloading(source As String, isDesiredOverload As Func(Of MethodSymbol, Boolean))
            CheckOverloading(
                source,
                Function(m) m.Name = "M" AndAlso isDesiredOverload(m),
                Function(originalType)
                    Dim stateMachineType = originalType.GetMembers().OfType(Of NamedTypeSymbol).Single(Function(t) t.Name.StartsWith(StringConstants.StateMachineTypeNamePrefix, StringComparison.Ordinal))
                    Return stateMachineType.GetMember(Of MethodSymbol)("MoveNext")
                End Function)
        End Sub

        Private Shared Sub CheckLambdaOverloading(source As String, isDesiredOverload As Func(Of MethodSymbol, Boolean))
            CheckOverloading(
                source,
                isDesiredOverload,
                Function(originalType)
                    Dim displayClass As NamedTypeSymbol = originalType.GetMembers().OfType(Of NamedTypeSymbol).Single(Function(t) t.Name.StartsWith(StringConstants.DisplayClassPrefix, StringComparison.Ordinal))
                    Return displayClass.GetMembers().OfType(Of MethodSymbol).Single(AddressOf IsLambda)
                End Function)
        End Sub

        Private Shared Function IsLambda(method As MethodSymbol) As Boolean
            Return method.Name.StartsWith(StringConstants.LambdaMethodNamePrefix, StringComparison.Ordinal)
        End Function

        Private Shared Sub CheckOverloading(source As String, isDesiredOverload As Func(Of MethodSymbol, Boolean), getSynthesizedMethod As Func(Of NamedTypeSymbol, MethodSymbol))
            Dim comp = CreateCompilationWithMscorlib({source}, options:=TestOptions.DebugDll)
            Dim dummyComp = MakeDummyCompilation(comp)

            Dim originalType = dummyComp.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C")
            Dim desiredMethod = originalType.GetMembers().OfType(Of MethodSymbol).Single(isDesiredOverload)

            Dim synthesizedMethod As MethodSymbol = getSynthesizedMethod(originalType)

            Dim guessedMethod = CompilationContext.GetSubstitutedSourceMethod(synthesizedMethod, sourceMethodMustBeInstance:=True)
            Assert.Equal(desiredMethod, guessedMethod.OriginalDefinition)
        End Sub

        Private Shared Function MakeDummyCompilation(comp As VisualBasicCompilation) As VisualBasicCompilation
            Dim compOptions = TestOptions.DebugDll.WithMetadataImportOptions(MetadataImportOptions.All)
            Return CreateCompilationWithMscorlibAndVBRuntimeAndReferences(<Compilation/>, {comp.EmitToImageReference()}, compOptions)
        End Function
    End Class
End Namespace