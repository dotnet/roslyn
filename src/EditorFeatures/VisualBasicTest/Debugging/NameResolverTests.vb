' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Debugging

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Debugging

    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
    Public Class NameResolverTests

        Private Shared Function TestAsync(text As String, searchText As String, ParamArray expectedNames() As String) As Tasks.Task
            Return TestWithRootNamespaceAsync(Nothing, text, searchText, expectedNames)
        End Function

        Private Shared Async Function TestWithRootNamespaceAsync(rootNamespace As String, text As String, searchText As String, ParamArray expectedNames() As String) As Tasks.Task
            Dim compilationOptions = If(rootNamespace Is Nothing, Nothing, New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, rootNamespace:=rootNamespace))

            Using workspace = EditorTestWorkspace.Create(LanguageNames.VisualBasic, compilationOptions, Nothing, text)
                Dim nameResolver = New BreakpointResolver(workspace.CurrentSolution, searchText)
                Dim results = Await nameResolver.DoAsync(CancellationToken.None)

                Assert.Equal(expectedNames, results.Select(Function(r) r.LocationNameOpt))
            End Using
        End Function

        <Fact>
        Public Async Function TestSimpleNameInClass() As Task
            Dim text =
<text>
class C
  sub Goo()
  end sub
end class</text>.Value

            Await TestAsync(text, "Goo", "C.Goo()")
            Await TestAsync(text, "goo", "C.Goo()")
            Await TestAsync(text, "C.Goo", "C.Goo()")
            Await TestAsync(text, "N.C.Goo")
            Await TestAsync(text, "Goo(of T)")
            Await TestAsync(text, "C(of T).Goo")
            Await TestAsync(text, "Goo()", "C.Goo()")
            Await TestAsync(text, "Goo(i as Integer)")
            Await TestAsync(text, "Goo(Integer)")
        End Function

        <Fact>
        Public Async Function TestSimpleNameInNamespace() As Tasks.Task
            Dim text =
<text>
namespace N
  class C
    sub Goo()
    end sub
  end class
end namespace</text>.Value

            Await TestAsync(text, "Goo", "N.C.Goo()")
            Await TestAsync(text, "goo", "N.C.Goo()")
            Await TestAsync(text, "C.Goo", "N.C.Goo()")
            Await TestAsync(text, "n.c.Goo", "N.C.Goo()")
            Await TestAsync(text, "Goo(of T)")
            Await TestAsync(text, "C(of T).Goo")
            Await TestAsync(text, "Goo()", "N.C.Goo()")
            Await TestAsync(text, "C.Goo()", "N.C.Goo()")
            Await TestAsync(text, "N.C.Goo()", "N.C.Goo()")
            Await TestAsync(text, "Goo(i as Integer)")
            Await TestAsync(text, "Goo(Integer)")
            Await TestAsync(text, "Goo(a)")
        End Function

        <Fact>
        Public Async Function TestSimpleNameInGenericClassNamespace() As Tasks.Task
            Dim text =
<text>
namespace N
  class C(of T)
    sub Goo()
    end sub
  end class
end namespace</text>.Value

            Await TestAsync(text, "Goo", "N.C(Of T).Goo()")
            Await TestAsync(text, "goo", "N.C(Of T).Goo()")
            Await TestAsync(text, "C.Goo", "N.C(Of T).Goo()")
            Await TestAsync(text, "N.C.Goo", "N.C(Of T).Goo()")
            Await TestAsync(text, "Goo(of T)")
            Await TestAsync(text, "C(of T).Goo", "N.C(Of T).Goo()")
            Await TestAsync(text, "C(of T).Goo()", "N.C(Of T).Goo()")
            Await TestAsync(text, "Goo()", "N.C(Of T).Goo()")
            Await TestAsync(text, "C.Goo()", "N.C(Of T).Goo()")
            Await TestAsync(text, "N.C.Goo()", "N.C(Of T).Goo()")
            Await TestAsync(text, "Goo(i as Integer)")
            Await TestAsync(text, "Goo(Integer)")
            Await TestAsync(text, "Goo(a)")
        End Function

        <Fact>
        Public Async Function TestGenericNameInClassNamespace() As Task
            Dim text =
<text>
namespace N
  class C
    sub Goo(of T)()
    end sub
  end class
end namespace</text>.Value

            Await TestAsync(text, "Goo", "N.C.Goo(Of T)()")
            Await TestAsync(text, "goo", "N.C.Goo(Of T)()")
            Await TestAsync(text, "C.Goo", "N.C.Goo(Of T)()")
            Await TestAsync(text, "N.C.Goo", "N.C.Goo(Of T)()")
            Await TestAsync(text, "Goo(of T)", "N.C.Goo(Of T)()")
            Await TestAsync(text, "Goo(of X)", "N.C.Goo(Of T)()")
            Await TestAsync(text, "Goo(of T,X)")
            Await TestAsync(text, "C(of T).Goo")
            Await TestAsync(text, "C(of T).Goo()")
            Await TestAsync(text, "Goo()", "N.C.Goo(Of T)()")
            Await TestAsync(text, "C.Goo()", "N.C.Goo(Of T)()")
            Await TestAsync(text, "N.C.Goo()", "N.C.Goo(Of T)()")
            Await TestAsync(text, "Goo(i as Integer)")
            Await TestAsync(text, "Goo(Integer)")
            Await TestAsync(text, "Goo(a)")
            Await TestAsync(text, "Goo(of T)(i as Integer)")
            Await TestAsync(text, "Goo(of T)(Integer)")
            Await TestAsync(text, "Goo(of T)(a)")
        End Function

        <Fact>
        Public Async Function TestOverloadsInSingleClass() As Task
            Dim text =
<text>
class C
  sub Goo()
  end sub

  sub Goo(i as Integer)
  end sub
end class
</text>.Value

            Await TestAsync(text, "Goo", "C.Goo()", "C.Goo(Integer)")
            Await TestAsync(text, "goo", "C.Goo()", "C.Goo(Integer)")
            Await TestAsync(text, "C.Goo", "C.Goo()", "C.Goo(Integer)")
            Await TestAsync(text, "N.C.Goo")
            Await TestAsync(text, "Goo(of T)")
            Await TestAsync(text, "C(of T).Goo")
            Await TestAsync(text, "Goo()", "C.Goo()")
            Await TestAsync(text, "Goo(i as Integer)", "C.Goo(Integer)")
            Await TestAsync(text, "Goo(Integer)", "C.Goo(Integer)")
            Await TestAsync(text, "Goo(i)", "C.Goo(Integer)")
        End Function

        <Fact>
        Public Async Function TestMethodsInMultipleClasses() As Task
            Dim text =
<text>
namespace N
  class C
    sub Goo()
    end sub
  end class
end namespace

namespace N1
  class C
    sub Goo(i as Integer)
    end sub
  end class
end namespace</text>.Value

            Await TestAsync(text, "Goo", "N1.C.Goo(Integer)", "N.C.Goo()")
            Await TestAsync(text, "goo", "N1.C.Goo(Integer)", "N.C.Goo()")
            Await TestAsync(text, "C.Goo", "N1.C.Goo(Integer)", "N.C.Goo()")
            Await TestAsync(text, "N.C.Goo", "N.C.Goo()")
            Await TestAsync(text, "N1.C.Goo", "N1.C.Goo(Integer)")
            Await TestAsync(text, "Goo(of T)")
            Await TestAsync(text, "C(of T).Goo")
            Await TestAsync(text, "Goo()", "N.C.Goo()")
            Await TestAsync(text, "Goo(i as Integer)", "N1.C.Goo(Integer)")
            Await TestAsync(text, "Goo(Integer)", "N1.C.Goo(Integer)")
            Await TestAsync(text, "Goo(i)", "N1.C.Goo(Integer)")
        End Function

        <Fact>
        Public Async Function TestMethodsWithDifferentArityInMultipleClasses() As Task
            Dim text =
<text>
namespace N
  class C
    sub Goo()
    end sub
  end class
end namespace

namespace N1
  class C
    sub Goo(of T)(i as Integer)
    end sub
  end class
end namespace</text>.Value

            Await TestAsync(text, "Goo", "N1.C.Goo(Of T)(Integer)", "N.C.Goo()")
            Await TestAsync(text, "goo", "N1.C.Goo(Of T)(Integer)", "N.C.Goo()")
            Await TestAsync(text, "C.Goo", "N1.C.Goo(Of T)(Integer)", "N.C.Goo()")
            Await TestAsync(text, "N.C.Goo", "N.C.Goo()")
            Await TestAsync(text, "N1.C.Goo", "N1.C.Goo(Of T)(Integer)")
            Await TestAsync(text, "Goo(of T)", "N1.C.Goo(Of T)(Integer)")
            Await TestAsync(text, "C(of T).Goo")
            Await TestAsync(text, "Goo()", "N.C.Goo()")
            Await TestAsync(text, "Goo(of T)()")
            Await TestAsync(text, "Goo(i as Integer)", "N1.C.Goo(Of T)(Integer)")
            Await TestAsync(text, "Goo(Integer)", "N1.C.Goo(Of T)(Integer)")
            Await TestAsync(text, "Goo(i)", "N1.C.Goo(Of T)(Integer)")
            Await TestAsync(text, "Goo(of T)(i as Integer)", "N1.C.Goo(Of T)(Integer)")
            Await TestAsync(text, "Goo(of T)(Integer)", "N1.C.Goo(Of T)(Integer)")
            Await TestAsync(text, "Goo(of T)(i)", "N1.C.Goo(Of T)(Integer)")
        End Function

        <Fact>
        Public Async Function TestOverloadsWithMultipleParametersInSingleClass() As Task
            Dim text =
<text>
class C
  sub Goo(a as Integer)
  end sub

  sub Goo(a as Integer, Optional b as String = "bb")
  end sub
end class</text>.Value

            Await TestAsync(text, "Goo", "C.Goo(Integer)", "C.Goo(Integer, [String])")
            Await TestAsync(text, "goo", "C.Goo(Integer)", "C.Goo(Integer, [String])")
            Await TestAsync(text, "C.Goo", "C.Goo(Integer)", "C.Goo(Integer, [String])")
            Await TestAsync(text, "N.C.Goo")
            Await TestAsync(text, "Goo(of T)")
            Await TestAsync(text, "C(of T).Goo")
            Await TestAsync(text, "Goo()")
            Await TestAsync(text, "Goo(i as Integer)", "C.Goo(Integer)")
            Await TestAsync(text, "Goo(Integer)", "C.Goo(Integer)")
            Await TestAsync(text, "Goo(i)", "C.Goo(Integer)")
            Await TestAsync(text, "Goo(i as Integer, int b)", "C.Goo(Integer, [String])")
            Await TestAsync(text, "Goo(int, boolean)", "C.Goo(Integer, [String])")
            Await TestAsync(text, "Goo(i, s)", "C.Goo(Integer, [String])")
            Await TestAsync(text, "Goo(,)", "C.Goo(Integer, [String])")
            Await TestAsync(text, "Goo(x As Integer = 42,)", "C.Goo(Integer, [String])")
            Await TestAsync(text, "Goo(Optional x As Integer = 42, y = 42)", "C.Goo(Integer, [String])")
            Await TestAsync(text, "Goo(i as Integer, int b, char c)")
            Await TestAsync(text, "Goo(int, bool, char)")
            Await TestAsync(text, "Goo(i, s, c)")
        End Function

        <Fact>
        Public Async Function TestProperties() As Task
            Dim text =
<text>
Class C
    ReadOnly Property Property1 As String

    WriteOnly Property Property2 As String
        Set
        End Set
    End Property

    Property Property3 As Date
        Get
            Return #1/1/1970#
        End Get
        Set
        End Set
    End Property

    Default ReadOnly Property Property4(i As Integer) As Integer
        Get
            Return i
        End Get
    End Property

    ReadOnly Property Property5(i As Integer, j As String) As Integer
        Get
            Return i + j
        End Get
    End Property
End Class</text>.Value

            Await TestAsync(text, "Property1", "C.Property1")
            Await TestAsync(text, "property1()", "C.Property1")
            Await TestAsync(text, "property2", "C.Property2")
            Await TestAsync(text, "Property2(String)")
            Await TestAsync(text, "property3", "C.Property3")
            Await TestAsync(text, "property4", "C.Property4(Integer)")
            Await TestAsync(text, "property5(j, i)", "C.Property5(Integer, String)")
        End Function

        <Fact>
        Public Async Function TestNegativeTests() As Task
            Dim text =
<text>
MustInherit Class C
    MustOverride Sub AbstractMethod(a As Integer)
    Dim Field As Integer
    Delegate Sub Delegate1()
    Event Event1 As Delegate1
    Sub Goo()
    End Sub
    Sub Goo(Optional x As Integer = 1, Optional y As Integer= 2)
    End Sub
End Class</text>.Value

            Await TestAsync(text, "AbstractMethod")
            Await TestAsync(text, "Field")
            Await TestAsync(text, "Delegate1")
            Await TestAsync(text, "Event1")
            Await TestAsync(text, "Property1")
            Await TestAsync(text, "Property2")
            Await TestAsync(text, "New")
            Await TestAsync(text, "C.New")
            Await TestAsync(text, "Goo", "C.Goo()", "C.Goo([Integer], [Integer])") ' just making sure it would normally resolve before trying bad syntax
            Await TestAsync(text, "Goo Goo")
            Await TestAsync(text, "Goo()asdf")
            Await TestAsync(text, "Goo;")
            Await TestAsync(text, "Goo();")
            Await TestAsync(text, "Goo(),")
            Await TestAsync(text, "Goo(),f")
            Await TestAsync(text, "Goo().Goo")
            Await TestAsync(text, "Goo(")
            Await TestAsync(text, "(Goo")
            Await TestAsync(text, "Goo)")
            Await TestAsync(text, "(Goo)")
            Await TestAsync(text, "Goo(x = 42, y = 42)", "C.Goo([Integer], [Integer])") ' just making sure it would normally resolve before trying bad syntax
            Await TestAsync(text, "Goo[x = 42, y = 42]")
            Await TestAsync(text, "Dim x As Integer = 42")
            Await TestAsync(text, "Goo(Optional x As Integer = 42, y = 42")
            Await TestAsync(text, "C")
            Await TestAsync(text, "C.C")
            Await TestAsync(text, "")
        End Function

        <Fact>
        Public Async Function TestInstanceConstructors() As Task
            Dim text =
<text>
class C
  public sub new()
  end sub
end class

Class G(Of T)
  Public Sub New()
  End Sub
End Class</text>.Value

            Await TestAsync(text, "New", "G(Of T).New()", "C.New()")
            Await TestAsync(text, "c.new", "C.New()")
            Await TestAsync(text, "c.NEW()", "C.New()")
            Await TestAsync(text, "New()", "G(Of T).New()", "C.New()")
            Await TestAsync(text, "New(of T)")
            Await TestAsync(text, "New(of T)()")
            Await TestAsync(text, "New(i as Integer)")
            Await TestAsync(text, "New(Integer)")
            Await TestAsync(text, "New(i)")
            Await TestAsync(text, "G.New", "G(Of T).New()")
            Await TestAsync(text, "G.New()", "G(Of T).New()")
            Await TestAsync(text, "G(Of t).new", "G(Of T).New()")
            Await TestAsync(text, "G(of t).new()", "G(Of T).New()")
            Await TestAsync(text, "G(Of T)")
            Await TestAsync(text, "G(Of T)()")
            Await TestAsync(text, "G.G(Of T)")
            Await TestAsync(text, ".ctor")
            Await TestAsync(text, ".ctor()")
            Await TestAsync(text, "C.ctor")
            Await TestAsync(text, "C.ctor()")
            Await TestAsync(text, "G.ctor")
            Await TestAsync(text, "G(Of T).ctor()")
            Await TestAsync(text, "C")
            Await TestAsync(text, "C.C")
            Await TestAsync(text, "C.C()")
        End Function

        <Fact>
        Public Async Function TestStaticConstructors() As Task
            Dim text =
<text>
class C
  shared sub new()
  end sub
end class</text>.Value

            Await TestAsync(text, "New", "C.New()")
            Await TestAsync(text, "C.New", "C.New()")
            Await TestAsync(text, "C.New()", "C.New()")
            Await TestAsync(text, "New()", "C.New()")
            Await TestAsync(text, "New(of T)")
            Await TestAsync(text, "New(of T)()")
            Await TestAsync(text, "New(i as Integer)")
            Await TestAsync(text, "New(Integer)")
            Await TestAsync(text, "New(i)")
            Await TestAsync(text, "C")
            Await TestAsync(text, "C.C")
            Await TestAsync(text, "C.C()")
            Await TestAsync(text, "C.cctor")
            Await TestAsync(text, "C.cctor()")
        End Function

        <Fact>
        Public Async Function TestAllConstructors() As Task
            Dim text =
<text>
class C
  shared sub new()
  end sub

  public sub new(i as Integer)
  end sub
end class</text>.Value

            Await TestAsync(text, "New", "C.New(Integer)", "C.New()")
            Await TestAsync(text, "C.New", "C.New(Integer)", "C.New()")
            Await TestAsync(text, "c.New()", "C.New()")
            Await TestAsync(text, "new()", "C.New()")
            Await TestAsync(text, "New(of T)")
            Await TestAsync(text, "New(of T)()")
            Await TestAsync(text, "New(i as Integer)", "C.New(Integer)")
            Await TestAsync(text, "New(Integer)", "C.New(Integer)")
            Await TestAsync(text, "New(i)", "C.New(Integer)")
            Await TestAsync(text, "C")
            Await TestAsync(text, "C.C")
            Await TestAsync(text, "C.C()")
        End Function

        <Fact>
        Public Async Function TestPartialMethods() As Task
            Dim text =
<text>
Partial Class C
    Partial Private Sub M1()
    End Sub

    Private Sub M2()
    End Sub

    Partial Private Sub M2()
    End Sub

    Partial Private Sub M3()
    End Sub

    Partial Private Sub M3(x As Integer)
    End Sub

    Private Sub M3(x As Integer)
    End Sub
End Class</text>.Value

            Await TestAsync(text, "M1")
            Await TestAsync(text, "C.M1")
            Await TestAsync(text, "M2", "C.M2()")
            Await TestAsync(text, "M3", "C.M3(Integer)")
            Await TestAsync(text, "M3()")
            Await TestAsync(text, "M3(y)", "C.M3(Integer)")
        End Function

        <Fact>
        Public Async Function TestLeadingAndTrailingText() As Task
            Dim text =
<text>
Class C
  Sub Goo()
  End Sub
End Class</text>.Value

            Await TestAsync(text, "  Goo", "C.Goo()")
            Await TestAsync(text, "Goo() ", "C.Goo()")
            Await TestAsync(text, " Goo (  )   ", "C.Goo()")
            Await TestAsync(text, "Goo() ' comment", "C.Goo()")
        End Function

        <Fact>
        Public Async Function TestEscapedKeywords() As Task
            Dim text =
                <text>
Structure [true]
End Structure
Class [for]
    Sub [where]([me] As [true])
    End Sub
    Sub [False]()
    End Sub
End Class</text>.Value

            Await TestAsync(text, "where", "[for].where([true])")
            Await TestAsync(text, "[where]", "[for].where([true])")
            Await TestAsync(text, "[for].where", "[for].where([true])")
            Await TestAsync(text, "for.where", "[for].where([true])")
            Await TestAsync(text, "[for].where(true)", "[for].where([true])")
            Await TestAsync(text, "[for].where([if])", "[for].where([true])")
            Await TestAsync(text, "False", "[for].False()")
        End Function

        <Fact>
        Public Async Function TestGlobalQualifiedNames() As Task
            Dim text =
<text>
Class C
  Sub Goo(c1 As C)
  End Sub
End Class</text>.Value

            Await TestAsync(text, "Global.Goo")
            Await TestAsync(text, "Global.C.Goo")
            Await TestAsync(text, "Global.C.Goo(C)")
            Await TestAsync(text, "C.Goo(Global.C)", "C.Goo(C)")
        End Function

        <Fact>
        Public Async Function TestRootNamespaces() As Task
            Dim text =
<text>
Class C
  Sub Goo()
  End Sub
End Class
Namespace N1
  Class C
    Sub Goo()
    End Sub
  End Class
End Namespace</text>.Value

            Await TestWithRootNamespaceAsync("Root", text, "Goo", "Root.N1.C.Goo()", "Root.C.Goo()")
            Await TestWithRootNamespaceAsync("Root", text, "C.Goo", "Root.N1.C.Goo()", "Root.C.Goo()")
            Await TestWithRootNamespaceAsync("Root", text, "N1.C.Goo()", "Root.N1.C.Goo()")
            Await TestWithRootNamespaceAsync("Root", text, "Root.C.Goo()", "Root.C.Goo()")
            Await TestWithRootNamespaceAsync("Root", text, "Root.N1.C.Goo", "Root.N1.C.Goo()")
            Await TestWithRootNamespaceAsync("Root", text, "Root.Goo")
        End Function

        <Fact>
        Public Async Function TestNestedTypesAndNamespaces() As Task
            Dim text =
<text>
Namespace N1
  Class C
    Sub Goo()
    End Sub
  End Class
  Namespace N2
    Class C
    End Class
  End Namespace
  Namespace N3
    Class D
    End Class
  End Namespace
  Namespace N4
    Class C
      Sub Goo(x As Double)
      End Sub
      Class D
        Sub Goo()
        End Sub
        Class E
          Sub Goo()
          End Sub
        End Class
      End Class
    End Class
  End Namespace
  Namespace N5
  End Namespace
End Namespace</text>.Value

            Await TestAsync(text, "Goo", "N1.N4.C.Goo(Double)", "N1.N4.C.D.Goo()", "N1.N4.C.D.E.Goo()", "N1.C.Goo()")
            Await TestAsync(text, "C.Goo", "N1.N4.C.Goo(Double)", "N1.C.Goo()")
            Await TestAsync(text, "D.Goo", "N1.N4.C.D.Goo()")
            Await TestAsync(text, "N1.N4.C.D.Goo", "N1.N4.C.D.Goo()")
            Await TestAsync(text, "N1.Goo")
            Await TestAsync(text, "N3.C.Goo")
            Await TestAsync(text, "N5.C.Goo")
        End Function

        <Fact>
        Public Async Function TestInterfaces() As Task
            Dim text =
<text>
Interface I1
  Sub Goo()
  Sub Moo()
End Interface
Class C1 : Implements I1
  Sub Goo1() Implements I1.Goo
  End Sub
  Sub Moo() Implements I1.Moo
  End Sub
End Class
</text>.Value

            Await TestAsync(text, "Goo")
            Await TestAsync(text, "I1.Goo")
            Await TestAsync(text, "Goo1", "C1.Goo1()")
            Await TestAsync(text, "Moo", "C1.Moo()")
        End Function

    End Class

End Namespace
