' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.Debugging
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.UnitTests.Debugging

    Public Class LocationInfoGetterTests

        Private Sub Test(text As String, expectedName As String, expectedLineOffset As Integer, Optional parseOptions As VisualBasicParseOptions = Nothing, Optional rootNamespace As String = Nothing)
            Dim position As Integer
            MarkupTestFile.GetPosition(text, text, position)
            Dim compilationOptions = If(rootNamespace IsNot Nothing, New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, rootNamespace:=rootNamespace), Nothing)
            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromLines(LanguageNames.VisualBasic, compilationOptions, parseOptions, text)
                Dim locationInfo = LocationInfoGetter.GetInfoAsync(
                    workspace.CurrentSolution.Projects.Single().Documents.Single(),
                    position,
                    CancellationToken.None).WaitAndGetResult(CancellationToken.None)

                Assert.Equal(expectedName, locationInfo.Name)
                Assert.Equal(expectedLineOffset, locationInfo.LineOffset)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TestClass()
            Test(<text>
Class Foo$$
End Class
</text>.NormalizedValue, "Foo", 0)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TestSub()
            Test(<text>
Class C
  Sub Foo()$$
  End Sub
End Class
</text>.NormalizedValue, "C.Foo()", 0)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TestFunction()
            Test(<text>
Class C
  $$Function Foo() As Integer
  End Function
End Class
</text>.NormalizedValue, "C.Foo() As Integer", 0)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TestNamespace()
            Test(<text>
Namespace NS1
  Class C
    Sub Method()
    End Sub$$
  End Class
End Namespace
</text>.NormalizedValue, "NS1.C.Method()", 1)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TestDottedNamespace()
            Test(<text>
Namespace NS1.Another
  Class C
    Sub Method()
    End Sub$$
  End Class
End Namespace
</text>.NormalizedValue, "NS1.Another.C.Method()", 1)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TestNestedNamespace()
            Test(<text>
Namespace NS1
  Namespace Another
    Class C
      Sub Method()
      End Sub$$
    End Class
  End Namespace
End Namespace
</text>.NormalizedValue, "NS1.Another.C.Method()", 1)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TestRootNamespace()
            Test(<text>
Namespace NS1
  Class C
    Sub Method()
    End Sub$$
  End Class
End Namespace
</text>.NormalizedValue, "Root.NS1.C.Method()", 1, rootNamespace:="Root")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TestGlobalNamespaceWithRootNamespace()
            Test(<text>
Namespace Global.NS1
  Class C
    Sub Method()
    End Sub$$
  End Class
End Namespace
</text>.NormalizedValue, "NS1.C.Method()", 1, rootNamespace:="Root")

            Test(<text>
Namespace Global
  Namespace NS1
    Class C
      Sub Method()
      End Sub$$
    End Class
  End Namespace
End Namespace
</text>.NormalizedValue, "NS1.C.Method()", 1, rootNamespace:="Root")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TestNestedType()
            Test(<text>
Class Outer
  Class Inner
    Sub Quux()$$
    End Sub
  End Class
End Class
</text>.NormalizedValue, "Outer.Inner.Quux()", 0)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TestPropertyGetter()
            Test(<text>
Class C1
  ReadOnly Property P() As String
    Get
      Return Nothing$$
    End Get
  End Property
End Class
</text>.NormalizedValue, "C1.P() As String", 2)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TestPropertySetter()
            Test(<text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TestAutoProperty()
            Test(<text>
Class C1
  $$ReadOnly Property P As Object = 42
End Class
</text>.NormalizedValue, "C1.P As Object", 0)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TestParameterizedProperty()
            Test(<text>
Class C1
  ReadOnly Property P(x As Integer) As C1
    Get
      Return Nothing$$
    End Get
  End Property
End Class
</text>.NormalizedValue, "C1.P(x As Integer) As C1", 2)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TestField()
            Test(<text>
Class C1
  Dim fi$$eld As Integer
End Class
</text>.NormalizedValue, "C1", 1)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TestLambdaInFieldInitializer()
            Test(<text>
Class C1
  Dim a As Action(Of Integer) = Sub(b) Dim c As In$$teger
End Class
</text>.NormalizedValue, "C1", 1)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TestMultipleFields()
            Test(<text>
Class C1
  Dim a1, a$$2
End Class
</text>.NormalizedValue, "C1", 1)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TestConstructor()
            Test(<text>
Class C1
  Sub New()
  $$End Sub
End Class
</text>.NormalizedValue, "C1.New()", 1)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TestOperator()
            Test(<text>
Namespace NS1
  Class C1
    Public Shared Operator +(x As C1, y As C1) As Integer
      $$Return 42
    End Operator
  End Class
End Namespace
</text>.NormalizedValue, "NS1.C1.+(x As C1, y As C1) As Integer", 1) ' Old implementation reports "Operator +" (rather than "+")...
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TestConversionOperator()
            Test(<text>
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TestEvent()
            Test(<text>
Class C1
  Event E1(x)$$
End Class
</text>.NormalizedValue, "C1.E1(x)", 0) ' Old implementation did not show the parameters ("x")...
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TestParamArrayParameter()
            Test(<text>
Class C1
  Sub M1(ParamArray x() As Integer)$$
  End Sub
End Class
</text>.NormalizedValue, "C1.M1(ParamArray x() As Integer)", 0)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TestByRefParameter()
            Test(<text>
Class C1
  Sub M1( &lt;Out&gt; ByRef x As Integer )
    $$x = 1
  End Sub
End Class
</text>.NormalizedValue, "C1.M1( <Out> ByRef x As Integer )", 1) ' Old implementation did not show extra spaces around the parameters...
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TestOptionalParameter()
            Test(<text>
Class C1
  Sub M1(Optional x As Integer =1)
    $$x = 1
  End Sub
End Class
</text>.NormalizedValue, "C1.M1(Optional x As Integer =1)", 1)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TestGenericType()
            Test(<text>
Class C1(Of T, U)
  Shared Sub M1()$$
  End Sub
End Class
</text>.NormalizedValue, "C1(Of T, U).M1()", 0)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TestGenericMethod()
            Test(<text>
Class C1(Of T, U)
  Shared Sub M1(Of V)()$$
  End Sub
End Class
</text>.NormalizedValue, "C1(Of T, U).M1(Of V)()", 0)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TestGenericParametersAndReturn()
            Test(<text>
Class C1(Of T, U)
  Shared Function M1(Of V)(C1(Of Integer, V) x, V y) As C1(Of Integer, V)$$
  End Function
End Class
</text>.NormalizedValue, "C1(Of T, U).M1(Of V)(C1(Of Integer, V) x, V y) As C1(Of Integer, V)", 0)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TestMissingNamespace()
            Test(<text>
  Class C1
    Dim a1, a$$2
  End Class
End Namespace
</text>.NormalizedValue, "C1", 1)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TestMissingNamespaceName()
            Test(<text>
Namespace
  Class C1
    Function M1() As Integer
$$    End Function
  End Class
End Namespace
</text>.NormalizedValue, "?.C1.M1() As Integer", 1)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TestMissingClassName()
            Test(<text>
Namespace N1
  Class
    Function M1() As Integer
$$    End Function
  End Class
End Namespace
</text>.NormalizedValue, "N1.?.M1() As Integer", 1)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TestMissingMethodName()
            Test(<text>
Namespace N1
  Class C1
    Shared Sub (x As Integer)
$$    End Sub
  End Class
End Namespace
</text>.NormalizedValue, "N1.C1.?(x As Integer)", 1)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TestMissingParameterList()
            Test(<text>
Namespace N1
  Class C1
    Shared Sub M1
$$    End Sub
  End Class
End Namespace
</text>.NormalizedValue, "N1.C1.M1", 1)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TestMissingAsClause()
            Test(<text>
Namespace N1
  Class C1
    Shared Function F1(x As Integer)
$$    End Function
  End Class
End Namespace
</text>.NormalizedValue, "N1.C1.F1(x As Integer)", 1)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TopLevelField()
            ' Unlike C#, VB will not report a name for top level fields (consistent with old implementation).
            Test(<text>
$$Dim f1 As Integer
</text>.NormalizedValue, Nothing, 0, New VisualBasicParseOptions(kind:=SourceCodeKind.Script))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TopLevelMethod()
            Test(<text>
Function F1(x As Integer) As Integer
$$End Function
</text>.NormalizedValue, "F1(x As Integer) As Integer", 1, New VisualBasicParseOptions(kind:=SourceCodeKind.Script))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingLocationName)>
        Public Sub TopLevelStatement()
            Test(<text>

$$System.Console.WriteLine("Hello")
</text>.NormalizedValue, Nothing, 0, New VisualBasicParseOptions(kind:=SourceCodeKind.Script))
        End Sub

    End Class

End Namespace
