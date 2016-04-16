' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports Roslyn.Test.Utilities
Imports VB = Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Public Class MeMyBaseMyClassTests
        Inherits FlowTestBase

#Region "ControlFlowPass and DataflowAnalysis"

        <Fact>
        Public Sub SimpleForeachTest()
            Dim source =
<compilation name="MeIsKeyWord">
    <file name="a.vb">
Imports System
Class MeClass
    Public Sub test()
        [|
        Console.WriteLine(Me Is Me) 'BIND1:
        Dim x = Me
        |]
    End Sub
    Public Shared Sub Main()
        Dim x = New MeClass()
        x.test()
    End Sub
End Class
    </file>
</compilation>
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(source)

            Dim controlFlowResults = analysisResults.Item1
            Dim dataFlowResults = analysisResults.Item2

            Assert.Equal(0, controlFlowResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowResults.ExitPoints.Count())

            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact>
        Public Sub CallSharedFunctionInBaseClassByMe()
            Dim source =
<compilation name="CallSharedFunctionInBaseClassByMe">
    <file name="a.vb">
Imports System
Class BaseClass
    Function Method() As String
        Return "BaseClass"
    End Function
End Class
Class DerivedClass
    Inherits BaseClass
    Sub Test()
[|
        Console.WriteLine(Me.Method)
        Dim x = Me.Method
|]
    End Sub
End Class
    </file>
</compilation>
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(source)

            Dim controlFlowResults = analysisResults.Item1
            Dim dataFlowResults = analysisResults.Item2

            Assert.Equal(0, controlFlowResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowResults.ExitPoints.Count())

            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact>
        Public Sub UseMeInStructure()
            Dim source =
<compilation name="UseMeInStructure">
    <file name="a.vb">
Structure s1
    Dim x As Integer
    Sub foo()
[|
        Me.x = 1
        Dim y = Me.x
|]
    End Sub
End Structure
    </file>
</compilation>
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(source)

            Dim controlFlowResults = analysisResults.Item1
            Dim dataFlowResults = analysisResults.Item2

            Assert.Equal(0, controlFlowResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowResults.ExitPoints.Count())

            Assert.Equal("y", GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("y", GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("Me, y", GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact()>
        Public Sub CallMyBaseInLambda()
            Dim source =
<compilation name="CallMyBaseInLambda">
    <file name="a.vb">
Imports System
Module Module1
    Class Class1
        Function Bar(n As Integer) As Integer
            Return n + 1
        End Function
    End Class
    Class Class2 : Inherits Class1
        Sub TEST()
            Dim TEMP = [| Function(X) MyBase.Bar(x) |]
        End Sub
    End Class
End Module
    </file>
</compilation>

            Dim dataFlowResults = CompileAndAnalyzeDataFlow(source)

            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.Captured))
            Assert.Equal("X", GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me, X", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("X", GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("Me, TEMP", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact>
        Public Sub UseMyBaseInQuery()
            Dim source =
<compilation name="UseMyBaseInQuery">
    <file name="a.vb">
Imports System.Linq
Module Module1
    Class Class1
        Function Bar() As String
            Bar = "hello"
        End Function
    End Class
    Class Class2 : Inherits Class1
        Function TEST()
            [| TEST =  From x In  MyBase.Bar Select x |]
        End Function
    End Class
End Module
    </file>
</compilation>
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(source)

            Dim controlFlowResults = analysisResults.Item1
            Dim dataFlowResults = analysisResults.Item2

            Assert.Equal(0, controlFlowResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowResults.ExitPoints.Count())

            Assert.Equal("x, x", GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("TEST", GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me, x", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("TEST, x, x", GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact>
        Public Sub MyClassUsedToRefMethodDefinedInBaseClass()
            Dim source =
<compilation name="MyClassUsedToRefMethodDefinedInBaseClass">
    <file name="a.vb">
Class BaseClass
    Public Function foo()
        foo = "STRING"
    End Function
End Class
Class DerivedClass
    Inherits BaseClass
    Sub Test()
        [| Dim x = MyClass.foo() |]
    End Sub
End Class
    </file>
</compilation>
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(source)

            Dim controlFlowResults = analysisResults.Item1
            Dim dataFlowResults = analysisResults.Item2

            Assert.Equal(0, controlFlowResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowResults.ExitPoints.Count())

            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub

        <Fact>
        Public Sub MyClassUsedToQualifierSharedMember()
            Dim source =
<compilation name="MyClassUsedToQualifierSharedMember">
    <file name="a.vb">
Class BaseClass
    Private Sub foo()
    End Sub
End Class
Class DerivedClass
    Inherits BaseClass
    Shared age As Integer
    Sub Test()
        [| Dim x = MyClass.age |]
    End Sub
End Class
    </file>
</compilation>
            Dim analysisResults = CompileAndAnalyzeControlAndDataFlow(source)

            Dim controlFlowResults = analysisResults.Item1
            Dim dataFlowResults = analysisResults.Item2

            Assert.Equal(0, controlFlowResults.EntryPoints.Count())
            Assert.Equal(0, controlFlowResults.ExitPoints.Count())

            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.VariablesDeclared))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.AlwaysAssigned))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsIn))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.DataFlowsOut))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.ReadInside))
            Assert.Equal(Nothing, GetSymbolNamesJoined(dataFlowResults.ReadOutside))
            Assert.Equal("x", GetSymbolNamesJoined(dataFlowResults.WrittenInside))
            Assert.Equal("Me", GetSymbolNamesJoined(dataFlowResults.WrittenOutside))
        End Sub
#End Region

#Region "LookUpSymbol & GetSymbolInfo & GetTypeInfo Test"
        ' Call Me.[Me]
        <Fact>
        Public Sub CallMe()
            Dim comp = CreateCompilationWithMscorlib(
<compilation name="CallMe">
    <file name="a.vb">
Imports System
Class MeClass
    Function [Me]() As String 'BIND1:"[Me]"
        [Me] = "Hello" 
        Console.WriteLine(Me.Me)
    End Function
    Public Shared Sub Main()
        Dim x = New MeClass
        x.Me()
    End Sub
End Class
    </file>
</compilation>)
            Dim symbol = LookUpSymbolTest(comp, "Me", 1, 1, "Function MeClass.Me() As System.String")
            GetSymbolInfoTest(comp, "Me.Me", symbol)
            GetTypeInfoTest(comp, "Me.Me", "String")
        End Sub

        <Fact>
        Public Sub AssignMeToVar()
            Dim comp = CreateCompilationWithMscorlib(
<compilation name="AssignMeToVar">
    <file name="a.vb">
Option Infer On        
Imports System
Class C1
    Dim var = Me 'BIND1:"Me"
End Class
    </file>
</compilation>)

            ' get Me via the field
            Dim field = comp.GlobalNamespace.GetTypeMember("C1").GetMember("var")
            Dim meSymbol = DirectCast(field, SourceFieldSymbol).MeParameter

            ' must be same parameter as in the initializer
            GetSymbolInfoTest(comp, "Me", meSymbol)
            GetTypeInfoTest(comp, "Me", "C1")
        End Sub

        <Fact>
        Public Sub AssignMeToVar_Derived()
            Dim comp = CreateCompilationWithMscorlib(
<compilation name="AssignMeToVar">
    <file name="a.vb">
Option Infer On        
Imports System
Class base
End Class
Structure s1
    Class c1
        Inherits base
        Dim y As base = Me 'BIND1:"Me"
        Dim x As c1 = Me 'BIND2:"Me"
    End Class
End Structure
    </file>
</compilation>)

            LookUpSymbolTest(comp, "Me")
            LookUpSymbolTest(comp, "Me", 2)

            ' get Me via the field
            Dim field = comp.GlobalNamespace.GetTypeMember("s1").GetTypeMember("c1").GetMember("y")
            Dim meSymbol = DirectCast(field, SourceFieldSymbol).MeParameter

            GetSymbolInfoTest(comp, "Me", meSymbol)
            GetTypeInfoTest(comp, "Me", "s1.c1")
        End Sub

        <Fact>
        Public Sub CallFunctionInBaseClassByMe()
            Dim comp = CreateCompilationWithMscorlib(
<compilation name="CallFunctionInBaseClassByMe">
    <file name="a.vb">
Option Infer On        
Imports System
Class BaseClass
    Function Method() As String
        Return "BaseClass"
    End Function
End Class
Class DerivedClass
    Inherits BaseClass
    Sub Test()
        Console.WriteLine(Me.Method) 'BIND1:"Method"
    End Sub
End Class
    </file>
</compilation>)
            Dim symbol = LookUpSymbolTest(comp, "Method", expectedCount:=1, expectedString:="Function BaseClass.Method() As System.String")
            GetSymbolInfoTest(comp, "Me.Method", symbol)
            GetTypeInfoTest(comp, "Me.Method", "String")

            symbol = LookUpSymbolTest(comp, "DerivedClass", expectedCount:=1, expectedString:="DerivedClass")
            GetTypeInfoTest(comp, "Me", "DerivedClass")

        End Sub

        <WorkItem(529096, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529096")>
        <Fact()>
        Public Sub UseMeInLambda()
            Dim comp = CreateCompilationWithMscorlib(
<compilation name="UseMeInLambda">
    <file name="a.vb">
Option Infer On        
Module Module1
    Class Class1
        Function Bar() As Integer
            Return 1
        End Function
    End Class
    Class Class2 : Inherits Class1
        Sub TEST()
            Dim TEMP = Function(X) Me.Bar 'BIND1:"Bar"
        End Sub
    End Class
End Module
    </file>
</compilation>)
            Dim symbol = LookUpSymbolTest(comp, "Bar", expectedCount:=1, expectedString:="Function Module1.Class1.Bar() As System.Int32")
            GetSymbolInfoTest(comp, "Me.Bar", symbol)
            GetTypeInfoTest(comp, "Me.Bar", "Integer")
        End Sub

        <Fact>
        Public Sub UseMeInQuery()
            Dim comp = CreateCompilationWithMscorlib(
<compilation name="UseMeInQuery">
    <file name="a.vb">
Option Infer On        
Imports System.Linq
Module Module1
    Class Class1
        Function Bar1() As String 'BIND1:"Bar1"
            Bar1 = "hello"
        End Function
    End Class
    Class Class2 : Inherits Class1
        Function TEST()
            TEST = From x In Me.Bar1 Select Me 
        End Function
    End Class
End Module
    </file>
</compilation>)
            Dim symbol = LookUpSymbolTest(comp, "Bar1", expectedCount:=1, expectedString:="Function Module1.Class1.Bar1() As System.String")
            GetSymbolInfoTest(comp, "Me.Bar1", symbol)
            GetTypeInfoTest(comp, "Me.Bar1", "String")
        End Sub

        <Fact>
        Public Sub InvokeMyBaseAutoProperty()
            Dim comp = CreateCompilationWithMscorlib(
<compilation name="InvokeMyBaseAutoProperty">
    <file name="a.vb">
Option Infer On        
Imports System.Linq
Class GenBase
    Public Property Propabc As Integer = 1
    Public abc As Integer = 1
End Class
Class GenParent(Of t)
    Inherits GenBase
    Dim xyz = 1
    Public Property PropXyz = 1
    Sub foo()
        Dim x = Sub()
                    xyz = 2
                    MyBase.abc = 1
                    PropXyz = 3
                    MyBase.Propabc = 4 'BIND1:"Propabc"
                End Sub
        x.Invoke()
    End Sub
End Class
    </file>
</compilation>)
            Dim symbol = LookUpSymbolTest(comp, "Propabc", expectedCount:=1, expectedString:="Property GenBase.Propabc As System.Int32")
            GetSymbolInfoTest(comp, "MyBase.Propabc", symbol)
            GetTypeInfoTest(comp, "MyBase.Propabc", "Integer")

            symbol = LookUpSymbolTest(comp, "GenBase", expectedCount:=1, expectedString:="GenBase")
            GetTypeInfoTest(comp, "MyBase", "GenBase")
        End Sub

        <Fact>
        Public Sub InvokeMyBaseImplementMultInterface()
            Dim comp = CreateCompilationWithMscorlib(
<compilation name="InvokeMyBaseImplementMultInterface">
    <file name="a.vb">
Option Infer On        
Imports System.Linq
Class C1
    Implements System.Collections.Generic.IComparer(Of String)
    Implements System.Collections.Generic.IComparer(Of Integer)
    Public Function Compare1(ByVal x As String, ByVal y As String) As Integer Implements System.Collections.Generic.IComparer(Of String).Compare
        Return 0
    End Function
    Public Function Compare1(ByVal x As Integer, ByVal y As Integer) As Integer Implements System.Collections.Generic.IComparer(Of Integer).Compare
        Return 0
    End Function
    Sub FOO()
        Console.WriteLine(MyBase.ToString()) 'BIND1:"MyBase"
    End Sub
End Class
    </file>
</compilation>)

            GetTypeInfoTest(comp, "MyBase", "Object")
        End Sub

        <Fact>
        Public Sub InvokeExtensionMethodFromMyClass()

            Dim comp = CreateCompilationWithMscorlibAndReferences(
<compilation name="InvokeExtensionMethodFromMyClass">
    <file name="a.vb">
Option Infer On        
Imports System.Runtime.CompilerServices
Imports System
Class C1
    Sub Foo()
        Console.WriteLine(MyClass.Sum) 'BIND1:"Sum"
    End Sub
End Class
&lt;Extension()&gt;
Module MyExtensionModule
    &lt;Extension()&gt;
    Function Sum([Me] As C1) As Integer
        Sum = 1
    End Function
End Module
    </file>
</compilation>, {SystemCoreRef})
            Dim symbol = LookUpSymbolTest(comp, "Sum", expectedCount:=1, expectedString:="Function C1.Sum() As System.Int32")
            GetSymbolInfoTest(comp, "MyClass.Sum", symbol)
            GetTypeInfoTest(comp, "MyClass.Sum", "Integer")

            symbol = LookUpSymbolTest(comp, "C1", expectedCount:=1, expectedString:="C1")
            GetTypeInfoTest(comp, "MyClass", "C1")
        End Sub

        <Fact>
        Public Sub MyClassUsedInStructure()
            Dim comp = CreateCompilationWithMscorlib(
<compilation name="MyClassUsedInStructure">
    <file name="a.vb">
Option Infer On        
Structure s1
    Sub foo()
        Console.WriteLine(MyClass.ToString()) 'BIND1:"MyClass"
    End Sub
End Structure
    </file>
</compilation>)
            Dim symbol = LookUpSymbolTest(comp, "s1", expectedCount:=1, expectedString:="s1")
            GetTypeInfoTest(comp, "MyClass", "s1")
        End Sub

        Public Function LookUpSymbolTest(comp As VisualBasicCompilation, name As String, Optional index As Integer = 1, Optional expectedCount As Integer = 0, Optional expectedString As String = "") As ISymbol
            Dim tree = comp.SyntaxTrees.First
            Dim nodes As New List(Of VisualBasicSyntaxNode)
            Dim model = comp.GetSemanticModel(tree)
            Dim pos As Integer = CompilationUtils.FindBindingTextPosition(comp, "a.vb", Nothing, index)

            Dim symbol = model.LookupSymbols(pos, name:=name, includeReducedExtensionMethods:=True)

            Assert.Equal(expectedCount, symbol.Length)
            If expectedCount <> 0 Then
                Assert.Equal(expectedString, symbol.Single.ToTestDisplayString())
                Return symbol.Single
            End If
            Return Nothing

        End Function

        Public Sub GetSymbolInfoTest(comp As VisualBasicCompilation, nodeName As String, expectedSymbol As ISymbol)
            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim expressions = tree.GetCompilationUnitRoot().DescendantNodesAndSelf.Where(Function(x) x.Kind = SyntaxKind.MeExpression Or x.Kind = SyntaxKind.MyBaseExpression Or x.Kind = SyntaxKind.MyClassExpression Or x.Kind = SyntaxKind.SimpleMemberAccessExpression).ToList()
            Dim expression = expressions.Where(Function(x) x.ToString = nodeName).First()
            Dim symbolInfo = model.GetSymbolInfo(DirectCast(expression, ExpressionSyntax))

            If (Equals(expectedSymbol, Nothing)) Then
                Assert.Equal(expectedSymbol, symbolInfo.Symbol)
            ElseIf (DirectCast(expectedSymbol, Symbol).IsReducedExtensionMethod = False) Then
                Assert.Equal(expectedSymbol, symbolInfo.Symbol)
            Else
                Dim methodActual = DirectCast(symbolInfo.Symbol, MethodSymbol)
                Dim methodExpected = DirectCast(expectedSymbol, MethodSymbol)
                Assert.Equal(methodExpected.CallsiteReducedFromMethod, methodActual.CallsiteReducedFromMethod)
            End If
        End Sub

        Public Sub GetTypeInfoTest(comp As VisualBasicCompilation, nodeName As String, expectedTypeInfo As String)
            Dim tree = comp.SyntaxTrees.First
            Dim model = comp.GetSemanticModel(tree)
            Dim expressions = tree.GetCompilationUnitRoot().DescendantNodesAndSelf.Where(Function(x) x.Kind = SyntaxKind.MeExpression Or x.Kind = SyntaxKind.MyBaseExpression Or x.Kind = SyntaxKind.MyClassExpression Or x.Kind = SyntaxKind.SimpleMemberAccessExpression).ToList()
            Dim expression = expressions.Where(Function(x) x.ToString = nodeName).First()

            Dim typeInfo = model.GetTypeInfo(DirectCast(expression, ExpressionSyntax))
            Assert.NotNull(typeInfo.Type)
            Assert.Equal(expectedTypeInfo, typeInfo.Type.ToDisplayString())
        End Sub

#End Region

    End Class
End Namespace
