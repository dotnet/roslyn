' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Linq
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.Debugging
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities
Imports Xunit

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.UnitTests.Debugging

    Public Class NameResolverTests

        Private Function TestAsync(text As String, searchText As String, ParamArray expectedNames() As String) As Tasks.Task
            Return TestWithRootNamespaceAsync(Nothing, text, searchText, expectedNames)
        End Function

        Private Async Function TestWithRootNamespaceAsync(rootNamespace As String, text As String, searchText As String, ParamArray expectedNames() As String) As Tasks.Task
            Dim compilationOptions = If(rootNamespace Is Nothing, Nothing, New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, rootNamespace:=rootNamespace))

            Using workspace = Await TestWorkspace.CreateAsync(LanguageNames.VisualBasic, compilationOptions, Nothing, text)
                Dim nameResolver = New BreakpointResolver(workspace.CurrentSolution, searchText)
                Dim results = Await nameResolver.DoAsync(CancellationToken.None)

                Assert.Equal(expectedNames, results.Select(Function(r) r.LocationNameOpt))
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
        Public Async Function TestSimpleNameInClass() As Task
            Dim text =
<text>
class C
  sub Foo()
  end sub
end class</text>.Value

            Await TestAsync(text, "Foo", "C.Foo()")
            Await TestAsync(text, "foo", "C.Foo()")
            Await TestAsync(text, "C.Foo", "C.Foo()")
            Await TestAsync(text, "N.C.Foo")
            Await TestAsync(text, "Foo(of T)")
            Await TestAsync(text, "C(of T).Foo")
            Await TestAsync(text, "Foo()", "C.Foo()")
            Await TestAsync(text, "Foo(i as Integer)")
            Await TestAsync(text, "Foo(Integer)")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
        Public Async Function TestSimpleNameInNamespace() As Tasks.Task
            Dim text =
<text>
namespace N
  class C
    sub Foo()
    end sub
  end class
end namespace</text>.Value

            Await TestAsync(text, "Foo", "N.C.Foo()")
            Await TestAsync(text, "foo", "N.C.Foo()")
            Await TestAsync(text, "C.Foo", "N.C.Foo()")
            Await TestAsync(text, "n.c.Foo", "N.C.Foo()")
            Await TestAsync(text, "Foo(of T)")
            Await TestAsync(text, "C(of T).Foo")
            Await TestAsync(text, "Foo()", "N.C.Foo()")
            Await TestAsync(text, "C.Foo()", "N.C.Foo()")
            Await TestAsync(text, "N.C.Foo()", "N.C.Foo()")
            Await TestAsync(text, "Foo(i as Integer)")
            Await TestAsync(text, "Foo(Integer)")
            Await TestAsync(text, "Foo(a)")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
        Public Async Function TestSimpleNameInGenericClassNamespace() As Tasks.Task
            Dim text =
<text>
namespace N
  class C(of T)
    sub Foo()
    end sub
  end class
end namespace</text>.Value

            Await TestAsync(text, "Foo", "N.C(Of T).Foo()")
            Await TestAsync(text, "foo", "N.C(Of T).Foo()")
            Await TestAsync(text, "C.Foo", "N.C(Of T).Foo()")
            Await TestAsync(text, "N.C.Foo", "N.C(Of T).Foo()")
            Await TestAsync(text, "Foo(of T)")
            Await TestAsync(text, "C(of T).Foo", "N.C(Of T).Foo()")
            Await TestAsync(text, "C(of T).Foo()", "N.C(Of T).Foo()")
            Await TestAsync(text, "Foo()", "N.C(Of T).Foo()")
            Await TestAsync(text, "C.Foo()", "N.C(Of T).Foo()")
            Await TestAsync(text, "N.C.Foo()", "N.C(Of T).Foo()")
            Await TestAsync(text, "Foo(i as Integer)")
            Await TestAsync(text, "Foo(Integer)")
            Await TestAsync(text, "Foo(a)")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
        Public Async Function TestGenericNameInClassNamespace() As Task
            Dim text =
<text>
namespace N
  class C
    sub Foo(of T)()
    end sub
  end class
end namespace</text>.Value

            Await TestAsync(text, "Foo", "N.C.Foo(Of T)()")
            Await TestAsync(text, "foo", "N.C.Foo(Of T)()")
            Await TestAsync(text, "C.Foo", "N.C.Foo(Of T)()")
            Await TestAsync(text, "N.C.Foo", "N.C.Foo(Of T)()")
            Await TestAsync(text, "Foo(of T)", "N.C.Foo(Of T)()")
            Await TestAsync(text, "Foo(of X)", "N.C.Foo(Of T)()")
            Await TestAsync(text, "Foo(of T,X)")
            Await TestAsync(text, "C(of T).Foo")
            Await TestAsync(text, "C(of T).Foo()")
            Await TestAsync(text, "Foo()", "N.C.Foo(Of T)()")
            Await TestAsync(text, "C.Foo()", "N.C.Foo(Of T)()")
            Await TestAsync(text, "N.C.Foo()", "N.C.Foo(Of T)()")
            Await TestAsync(text, "Foo(i as Integer)")
            Await TestAsync(text, "Foo(Integer)")
            Await TestAsync(text, "Foo(a)")
            Await TestAsync(text, "Foo(of T)(i as Integer)")
            Await TestAsync(text, "Foo(of T)(Integer)")
            Await TestAsync(text, "Foo(of T)(a)")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
        Public Async Function TestOverloadsInSingleClass() As Task
            Dim text =
<text>
class C
  sub Foo()
  end sub

  sub Foo(i as Integer)
  end sub
end class
</text>.Value

            Await TestAsync(text, "Foo", "C.Foo()", "C.Foo(Integer)")
            Await TestAsync(text, "foo", "C.Foo()", "C.Foo(Integer)")
            Await TestAsync(text, "C.Foo", "C.Foo()", "C.Foo(Integer)")
            Await TestAsync(text, "N.C.Foo")
            Await TestAsync(text, "Foo(of T)")
            Await TestAsync(text, "C(of T).Foo")
            Await TestAsync(text, "Foo()", "C.Foo()")
            Await TestAsync(text, "Foo(i as Integer)", "C.Foo(Integer)")
            Await TestAsync(text, "Foo(Integer)", "C.Foo(Integer)")
            Await TestAsync(text, "Foo(i)", "C.Foo(Integer)")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
        Public Async Function TestMethodsInMultipleClasses() As Task
            Dim text =
<text>
namespace N
  class C
    sub Foo()
    end sub
  end class
end namespace

namespace N1
  class C
    sub Foo(i as Integer)
    end sub
  end class
end namespace</text>.Value

            Await TestAsync(text, "Foo", "N1.C.Foo(Integer)", "N.C.Foo()")
            Await TestAsync(text, "foo", "N1.C.Foo(Integer)", "N.C.Foo()")
            Await TestAsync(text, "C.Foo", "N1.C.Foo(Integer)", "N.C.Foo()")
            Await TestAsync(text, "N.C.Foo", "N.C.Foo()")
            Await TestAsync(text, "N1.C.Foo", "N1.C.Foo(Integer)")
            Await TestAsync(text, "Foo(of T)")
            Await TestAsync(text, "C(of T).Foo")
            Await TestAsync(text, "Foo()", "N.C.Foo()")
            Await TestAsync(text, "Foo(i as Integer)", "N1.C.Foo(Integer)")
            Await TestAsync(text, "Foo(Integer)", "N1.C.Foo(Integer)")
            Await TestAsync(text, "Foo(i)", "N1.C.Foo(Integer)")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
        Public Async Function TestMethodsWithDifferentArityInMultipleClasses() As Task
            Dim text =
<text>
namespace N
  class C
    sub Foo()
    end sub
  end class
end namespace

namespace N1
  class C
    sub Foo(of T)(i as Integer)
    end sub
  end class
end namespace</text>.Value

            Await TestAsync(text, "Foo", "N1.C.Foo(Of T)(Integer)", "N.C.Foo()")
            Await TestAsync(text, "foo", "N1.C.Foo(Of T)(Integer)", "N.C.Foo()")
            Await TestAsync(text, "C.Foo", "N1.C.Foo(Of T)(Integer)", "N.C.Foo()")
            Await TestAsync(text, "N.C.Foo", "N.C.Foo()")
            Await TestAsync(text, "N1.C.Foo", "N1.C.Foo(Of T)(Integer)")
            Await TestAsync(text, "Foo(of T)", "N1.C.Foo(Of T)(Integer)")
            Await TestAsync(text, "C(of T).Foo")
            Await TestAsync(text, "Foo()", "N.C.Foo()")
            Await TestAsync(text, "Foo(of T)()")
            Await TestAsync(text, "Foo(i as Integer)", "N1.C.Foo(Of T)(Integer)")
            Await TestAsync(text, "Foo(Integer)", "N1.C.Foo(Of T)(Integer)")
            Await TestAsync(text, "Foo(i)", "N1.C.Foo(Of T)(Integer)")
            Await TestAsync(text, "Foo(of T)(i as Integer)", "N1.C.Foo(Of T)(Integer)")
            Await TestAsync(text, "Foo(of T)(Integer)", "N1.C.Foo(Of T)(Integer)")
            Await TestAsync(text, "Foo(of T)(i)", "N1.C.Foo(Of T)(Integer)")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
        Public Async Function TestOverloadsWithMultipleParametersInSingleClass() As Task
            Dim text =
<text>
class C
  sub Foo(a as Integer)
  end sub

  sub Foo(a as Integer, Optional b as String = "bb")
  end sub
end class</text>.Value

            Await TestAsync(text, "Foo", "C.Foo(Integer)", "C.Foo(Integer, [String])")
            Await TestAsync(text, "foo", "C.Foo(Integer)", "C.Foo(Integer, [String])")
            Await TestAsync(text, "C.Foo", "C.Foo(Integer)", "C.Foo(Integer, [String])")
            Await TestAsync(text, "N.C.Foo")
            Await TestAsync(text, "Foo(of T)")
            Await TestAsync(text, "C(of T).Foo")
            Await TestAsync(text, "Foo()")
            Await TestAsync(text, "Foo(i as Integer)", "C.Foo(Integer)")
            Await TestAsync(text, "Foo(Integer)", "C.Foo(Integer)")
            Await TestAsync(text, "Foo(i)", "C.Foo(Integer)")
            Await TestAsync(text, "Foo(i as Integer, int b)", "C.Foo(Integer, [String])")
            Await TestAsync(text, "Foo(int, boolean)", "C.Foo(Integer, [String])")
            Await TestAsync(text, "Foo(i, s)", "C.Foo(Integer, [String])")
            Await TestAsync(text, "Foo(,)", "C.Foo(Integer, [String])")
            Await TestAsync(text, "Foo(x As Integer = 42,)", "C.Foo(Integer, [String])")
            Await TestAsync(text, "Foo(Optional x As Integer = 42, y = 42)", "C.Foo(Integer, [String])")
            Await TestAsync(text, "Foo(i as Integer, int b, char c)")
            Await TestAsync(text, "Foo(int, bool, char)")
            Await TestAsync(text, "Foo(i, s, c)")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
        Public Async Function TestNegativeTests() As Task
            Dim text =
<text>
MustInherit Class C
    MustOverride Sub AbstractMethod(a As Integer)
    Dim Field As Integer
    Delegate Sub Delegate1()
    Event Event1 As Delegate1
    Sub Foo()
    End Sub
    Sub Foo(Optional x As Integer = 1, Optional y As Integer= 2)
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
            Await TestAsync(text, "Foo", "C.Foo()", "C.Foo([Integer], [Integer])") ' just making sure it would normally resolve before trying bad syntax
            Await TestAsync(text, "Foo Foo")
            Await TestAsync(text, "Foo()asdf")
            Await TestAsync(text, "Foo;")
            Await TestAsync(text, "Foo();")
            Await TestAsync(text, "Foo(),")
            Await TestAsync(text, "Foo(),f")
            Await TestAsync(text, "Foo().Foo")
            Await TestAsync(text, "Foo(")
            Await TestAsync(text, "(Foo")
            Await TestAsync(text, "Foo)")
            Await TestAsync(text, "(Foo)")
            Await TestAsync(text, "Foo(x = 42, y = 42)", "C.Foo([Integer], [Integer])") ' just making sure it would normally resolve before trying bad syntax
            Await TestAsync(text, "Foo[x = 42, y = 42]")
            Await TestAsync(text, "Dim x As Integer = 42")
            Await TestAsync(text, "Foo(Optional x As Integer = 42, y = 42")
            Await TestAsync(text, "C")
            Await TestAsync(text, "C.C")
            Await TestAsync(text, "")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
        Public Async Function TestLeadingAndTrailingText() As Task
            Dim text =
<text>
Class C
  Sub Foo()
  End Sub
End Class</text>.Value

            Await TestAsync(text, "  Foo", "C.Foo()")
            Await TestAsync(text, "Foo() ", "C.Foo()")
            Await TestAsync(text, " Foo (  )   ", "C.Foo()")
            Await TestAsync(text, "Foo() ' comment", "C.Foo()")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
        Public Async Function TestGlobalQualifiedNames() As Task
            Dim text =
<text>
Class C
  Sub Foo(c1 As C)
  End Sub
End Class</text>.Value

            Await TestAsync(text, "Global.Foo")
            Await TestAsync(text, "Global.C.Foo")
            Await TestAsync(text, "Global.C.Foo(C)")
            Await TestAsync(text, "C.Foo(Global.C)", "C.Foo(C)")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
        Public Async Function TestRootNamespaces() As Task
            Dim text =
<text>
Class C
  Sub Foo()
  End Sub
End Class
Namespace N1
  Class C
    Sub Foo()
    End Sub
  End Class
End Namespace</text>.Value

            Await TestWithRootNamespaceAsync("Root", text, "Foo", "Root.N1.C.Foo()", "Root.C.Foo()")
            Await TestWithRootNamespaceAsync("Root", text, "C.Foo", "Root.N1.C.Foo()", "Root.C.Foo()")
            Await TestWithRootNamespaceAsync("Root", text, "N1.C.Foo()", "Root.N1.C.Foo()")
            Await TestWithRootNamespaceAsync("Root", text, "Root.C.Foo()", "Root.C.Foo()")
            Await TestWithRootNamespaceAsync("Root", text, "Root.N1.C.Foo", "Root.N1.C.Foo()")
            Await TestWithRootNamespaceAsync("Root", text, "Root.Foo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
        Public Async Function TestNestedTypesAndNamespaces() As Task
            Dim text =
<text>
Namespace N1
  Class C
    Sub Foo()
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
      Sub Foo(x As Double)
      End Sub
      Class D
        Sub Foo()
        End Sub
        Class E
          Sub Foo()
          End Sub
        End Class
      End Class
    End Class
  End Namespace
  Namespace N5
  End Namespace
End Namespace</text>.Value

            Await TestAsync(text, "Foo", "N1.N4.C.Foo(Double)", "N1.N4.C.D.Foo()", "N1.N4.C.D.E.Foo()", "N1.C.Foo()")
            Await TestAsync(text, "C.Foo", "N1.N4.C.Foo(Double)", "N1.C.Foo()")
            Await TestAsync(text, "D.Foo", "N1.N4.C.D.Foo()")
            Await TestAsync(text, "N1.N4.C.D.Foo", "N1.N4.C.D.Foo()")
            Await TestAsync(text, "N1.Foo")
            Await TestAsync(text, "N3.C.Foo")
            Await TestAsync(text, "N5.C.Foo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
        Public Async Function TestInterfaces() As Task
            Dim text =
<text>
Interface I1
  Sub Foo()
  Sub Moo()
End Interface
Class C1 : Implements I1
  Sub Foo1() Implements I1.Foo
  End Sub
  Sub Moo() Implements I1.Moo
  End Sub
End Class
</text>.Value

            Await TestAsync(text, "Foo")
            Await TestAsync(text, "I1.Foo")
            Await TestAsync(text, "Foo1", "C1.Foo1()")
            Await TestAsync(text, "Moo", "C1.Moo()")
        End Function

    End Class

End Namespace
