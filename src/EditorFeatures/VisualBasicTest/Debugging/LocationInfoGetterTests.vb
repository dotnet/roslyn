' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Debugging
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Debugging

    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
    Public Class LocationInfoGetterTests

        Private Shared Async Function TestAsync(text As String, expectedName As String, expectedLineOffset As Integer, Optional parseOptions As VisualBasicParseOptions = Nothing, Optional rootNamespace As String = Nothing) As Tasks.Task
            Dim position As Integer
            MarkupTestFile.GetPosition(text, text, position)
            Dim compilationOptions = If(rootNamespace IsNot Nothing, New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, rootNamespace:=rootNamespace), Nothing)
            Using workspace = EditorTestWorkspace.Create(LanguageNames.VisualBasic, compilationOptions, parseOptions, text)
                Dim locationInfo = Await LocationInfoGetter.GetInfoAsync(
                    workspace.CurrentSolution.Projects.Single().Documents.Single(),
                    position,
                    CancellationToken.None)

                Assert.Equal(expectedName, locationInfo.Name)
                Assert.Equal(expectedLineOffset, locationInfo.LineOffset)
            End Using
        End Function

        <Fact>
        Public Async Function TestClass() As Task
            Await TestAsync(<text>
Class Goo$$
End Class
</text>.NormalizedValue, "Goo", 0)
        End Function

        <Fact>
        Public Async Function TestSub() As Task
            Await TestAsync(<text>
Class C
  Sub Goo()$$
  End Sub
End Class
</text>.NormalizedValue, "C.Goo()", 0)
        End Function

        <Fact>
        Public Async Function TestFunction() As Task
            Await TestAsync(<text>
Class C
  $$Function Goo() As Integer
  End Function
End Class
</text>.NormalizedValue, "C.Goo() As Integer", 0)
        End Function

        <Fact>
        Public Async Function TestNamespace() As Task
            Await TestAsync(<text>
Namespace NS1
  Class C
    Sub Method()
    End Sub$$
  End Class
End Namespace
</text>.NormalizedValue, "NS1.C.Method()", 1)
        End Function

        <Fact>
        Public Async Function TestDottedNamespace() As Task
            Await TestAsync(<text>
Namespace NS1.Another
  Class C
    Sub Method()
    End Sub$$
  End Class
End Namespace
</text>.NormalizedValue, "NS1.Another.C.Method()", 1)
        End Function

        <Fact>
        Public Async Function TestNestedNamespace() As Task
            Await TestAsync(<text>
Namespace NS1
  Namespace Another
    Class C
      Sub Method()
      End Sub$$
    End Class
  End Namespace
End Namespace
</text>.NormalizedValue, "NS1.Another.C.Method()", 1)
        End Function

        <Fact>
        Public Async Function TestRootNamespace() As Task
            Await TestAsync(<text>
Namespace NS1
  Class C
    Sub Method()
    End Sub$$
  End Class
End Namespace
</text>.NormalizedValue, "Root.NS1.C.Method()", 1, rootNamespace:="Root")
        End Function

        <Fact>
        Public Async Function TestGlobalNamespaceWithRootNamespace() As Task
            Await TestAsync(<text>
Namespace Global.NS1
  Class C
    Sub Method()
    End Sub$$
  End Class
End Namespace
</text>.NormalizedValue, "NS1.C.Method()", 1, rootNamespace:="Root")

            Await TestAsync(<text>
Namespace Global
  Namespace NS1
    Class C
      Sub Method()
      End Sub$$
    End Class
  End Namespace
End Namespace
</text>.NormalizedValue, "NS1.C.Method()", 1, rootNamespace:="Root")
        End Function

        <Fact>
        Public Async Function TestNestedType() As Task
            Await TestAsync(<text>
Class Outer
  Class Inner
    Sub Quux()$$
    End Sub
  End Class
End Class
</text>.NormalizedValue, "Outer.Inner.Quux()", 0)
        End Function

        <Fact>
        Public Async Function TestPropertyGetter() As Task
            Await TestAsync(<text>
Class C1
  ReadOnly Property P() As String
    Get
      Return Nothing$$
    End Get
  End Property
End Class
</text>.NormalizedValue, "C1.P() As String", 2)
        End Function

        <Fact>
        Public Async Function TestPropertySetter() As Task
            Await TestAsync(<text>
Class C1
  ReadOnly Property P() As String
    Get
      Return Nothing
    End Get
    Set
      Dim s = $$value
    End Set
  End Property
End Class
</text>.NormalizedValue, "C1.P() As String", 5)
        End Function

        <Fact>
        Public Async Function TestAutoProperty() As Task
            Await TestAsync(<text>
Class C1
  $$ReadOnly Property P As Object = 42
End Class
</text>.NormalizedValue, "C1.P As Object", 0)
        End Function

        <Fact>
        Public Async Function TestParameterizedProperty() As Task
            Await TestAsync(<text>
Class C1
  ReadOnly Property P(x As Integer) As C1
    Get
      Return Nothing$$
    End Get
  End Property
End Class
</text>.NormalizedValue, "C1.P(x As Integer) As C1", 2)
        End Function

        <Fact>
        Public Async Function TestField() As Task
            Await TestAsync(<text>
Class C1
  Dim fi$$eld As Integer
End Class
</text>.NormalizedValue, "C1", 1)
        End Function

        <Fact>
        Public Async Function TestLambdaInFieldInitializer() As Task
            Await TestAsync(<text>
Class C1
  Dim a As Action(Of Integer) = Sub(b) Dim c As In$$teger
End Class
</text>.NormalizedValue, "C1", 1)
        End Function

        <Fact>
        Public Async Function TestMultipleFields() As Task
            Await TestAsync(<text>
Class C1
  Dim a1, a$$2
End Class
</text>.NormalizedValue, "C1", 1)
        End Function

        <Fact>
        Public Async Function TestConstructor() As Task
            Await TestAsync(<text>
Class C1
  Sub New()
  $$End Sub
End Class
</text>.NormalizedValue, "C1.New()", 1)
        End Function

        <Fact>
        Public Async Function TestOperator() As Task
            Await TestAsync(<text>
Namespace NS1
  Class C1
    Public Shared Operator +(x As C1, y As C1) As Integer
      $$Return 42
    End Operator
  End Class
End Namespace
</text>.NormalizedValue, "NS1.C1.+(x As C1, y As C1) As Integer", 1) ' Old implementation reports "Operator +" (rather than "+")...
        End Function

        <Fact>
        Public Async Function TestConversionOperator() As Task
            Await TestAsync(<text>
Namespace NS1
  Class C1
    Public Shared Narrowing Operator CType(x As NS1.C1) As NS1.C2
      $$Return Nothing
    End Operator
  End Class
  Class C2
  End Class
End Namespace
</text>.NormalizedValue, "NS1.C1.CType(x As NS1.C1) As NS1.C2", 1) ' Old implementation reports "Operator CType" (rather than "CType")...
        End Function

        <Fact>
        Public Async Function TestEvent() As Task
            Await TestAsync(<text>
Class C1
  Event E1(x)$$
End Class
</text>.NormalizedValue, "C1.E1(x)", 0) ' Old implementation did not show the parameters ("x")...
        End Function

        <Fact>
        Public Async Function TestParamArrayParameter() As Task
            Await TestAsync(<text>
Class C1
  Sub M1(ParamArray x() As Integer)$$
  End Sub
End Class
</text>.NormalizedValue, "C1.M1(ParamArray x() As Integer)", 0)
        End Function

        <Fact>
        Public Async Function TestByRefParameter() As Task
            Await TestAsync(<text>
Class C1
  Sub M1( &lt;Out&gt; ByRef x As Integer )
    $$x = 1
  End Sub
End Class
</text>.NormalizedValue, "C1.M1( <Out> ByRef x As Integer )", 1) ' Old implementation did not show extra spaces around the parameters...
        End Function

        <Fact>
        Public Async Function TestOptionalParameter() As Task
            Await TestAsync(<text>
Class C1
  Sub M1(Optional x As Integer =1)
    $$x = 1
  End Sub
End Class
</text>.NormalizedValue, "C1.M1(Optional x As Integer =1)", 1)
        End Function

        <Fact>
        Public Async Function TestGenericType() As Task
            Await TestAsync(<text>
Class C1(Of T, U)
  Shared Sub M1()$$
  End Sub
End Class
</text>.NormalizedValue, "C1(Of T, U).M1()", 0)
        End Function

        <Fact>
        Public Async Function TestGenericMethod() As Task
            Await TestAsync(<text>
Class C1(Of T, U)
  Shared Sub M1(Of V)()$$
  End Sub
End Class
</text>.NormalizedValue, "C1(Of T, U).M1(Of V)()", 0)
        End Function

        <Fact>
        Public Async Function TestGenericParametersAndReturn() As Task
            Await TestAsync(<text>
Class C1(Of T, U)
  Shared Function M1(Of V)(C1(Of Integer, V) x, V y) As C1(Of Integer, V)$$
  End Function
End Class
</text>.NormalizedValue, "C1(Of T, U).M1(Of V)(C1(Of Integer, V) x, V y) As C1(Of Integer, V)", 0)
        End Function

        <Fact>
        Public Async Function TestMissingNamespace() As Task
            Await TestAsync(<text>
  Class C1
    Dim a1, a$$2
  End Class
End Namespace
</text>.NormalizedValue, "C1", 1)
        End Function

        <Fact>
        Public Async Function TestMissingNamespaceName() As Task
            Await TestAsync(<text>
Namespace
  Class C1
    Function M1() As Integer
$$    End Function
  End Class
End Namespace
</text>.NormalizedValue, "?.C1.M1() As Integer", 1)
        End Function

        <Fact>
        Public Async Function TestMissingClassName() As Task
            Await TestAsync(<text>
Namespace N1
  Class
    Function M1() As Integer
$$    End Function
  End Class
End Namespace
</text>.NormalizedValue, "N1.?.M1() As Integer", 1)
        End Function

        <Fact>
        Public Async Function TestMissingMethodName() As Task
            Await TestAsync(<text>
Namespace N1
  Class C1
    Shared Sub (x As Integer)
$$    End Sub
  End Class
End Namespace
</text>.NormalizedValue, "N1.C1.?(x As Integer)", 1)
        End Function

        <Fact>
        Public Async Function TestMissingParameterList() As Task
            Await TestAsync(<text>
Namespace N1
  Class C1
    Shared Sub M1
$$    End Sub
  End Class
End Namespace
</text>.NormalizedValue, "N1.C1.M1", 1)
        End Function

        <Fact>
        Public Async Function TestMissingAsClause() As Task
            Await TestAsync(<text>
Namespace N1
  Class C1
    Shared Function F1(x As Integer)
$$    End Function
  End Class
End Namespace
</text>.NormalizedValue, "N1.C1.F1(x As Integer)", 1)
        End Function

        <Fact>
        Public Async Function TestTopLevelField() As Task
            ' Unlike C#, VB will not report a name for top level fields (consistent with old implementation).
            Await TestAsync(<text>
$$Dim f1 As Integer
</text>.NormalizedValue, Nothing, 0, New VisualBasicParseOptions(kind:=SourceCodeKind.Script))
        End Function

        <Fact>
        Public Async Function TestTopLevelMethod() As Task
            Await TestAsync(<text>
Function F1(x As Integer) As Integer
$$End Function
</text>.NormalizedValue, "F1(x As Integer) As Integer", 1, New VisualBasicParseOptions(kind:=SourceCodeKind.Script))
        End Function

        <Fact>
        Public Async Function TestTopLevelStatement() As Task
            Await TestAsync(<text>

$$System.Console.WriteLine("Hello")
</text>.NormalizedValue, Nothing, 0, New VisualBasicParseOptions(kind:=SourceCodeKind.Script))
        End Function
    End Class
End Namespace
