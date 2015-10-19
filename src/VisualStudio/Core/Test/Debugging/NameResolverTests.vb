' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Linq
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.Debugging
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities
Imports Xunit

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.UnitTests.Debugging

    Public Class NameResolverTests

        Private Sub Test(text As String, searchText As String, ParamArray expectedNames() As String)
            TestWithRootNamespace(Nothing, text, searchText, expectedNames)
        End Sub

        Private Sub TestWithRootNamespace(rootNamespace As String, text As String, searchText As String, ParamArray expectedNames() As String)
            Dim compilationOptions = If(rootNamespace Is Nothing, Nothing, New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary, rootNamespace:=rootNamespace))

            Using workspace = TestWorkspaceFactory.CreateWorkspaceFromLines(LanguageNames.VisualBasic, compilationOptions, Nothing, text)
                Dim nameResolver = New BreakpointResolver(workspace.CurrentSolution, searchText)
                Dim results = nameResolver.DoAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None)

                Assert.Equal(expectedNames, results.Select(Function(r) r.LocationNameOpt))
            End Using

        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
        Public Sub TestSimpleNameInClass()
            Dim text =
<text>
class C
  sub Foo()
  end sub
end class</text>.Value

            Test(text, "Foo", "C.Foo()")
            Test(text, "foo", "C.Foo()")
            Test(text, "C.Foo", "C.Foo()")
            Test(text, "N.C.Foo")
            Test(text, "Foo(of T)")
            Test(text, "C(of T).Foo")
            Test(text, "Foo()", "C.Foo()")
            Test(text, "Foo(i as Integer)")
            Test(text, "Foo(Integer)")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
        Public Sub TestSimpleNameInNamespace()
            Dim text =
<text>
namespace N
  class C
    sub Foo()
    end sub
  end class
end namespace</text>.Value

            Test(text, "Foo", "N.C.Foo()")
            Test(text, "foo", "N.C.Foo()")
            Test(text, "C.Foo", "N.C.Foo()")
            Test(text, "n.c.Foo", "N.C.Foo()")
            Test(text, "Foo(of T)")
            Test(text, "C(of T).Foo")
            Test(text, "Foo()", "N.C.Foo()")
            Test(text, "C.Foo()", "N.C.Foo()")
            Test(text, "N.C.Foo()", "N.C.Foo()")
            Test(text, "Foo(i as Integer)")
            Test(text, "Foo(Integer)")
            Test(text, "Foo(a)")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
        Public Sub TestSimpleNameInGenericClassNamespace()
            Dim text =
<text>
namespace N
  class C(of T)
    sub Foo()
    end sub
  end class
end namespace</text>.Value

            Test(text, "Foo", "N.C(Of T).Foo()")
            Test(text, "foo", "N.C(Of T).Foo()")
            Test(text, "C.Foo", "N.C(Of T).Foo()")
            Test(text, "N.C.Foo", "N.C(Of T).Foo()")
            Test(text, "Foo(of T)")
            Test(text, "C(of T).Foo", "N.C(Of T).Foo()")
            Test(text, "C(of T).Foo()", "N.C(Of T).Foo()")
            Test(text, "Foo()", "N.C(Of T).Foo()")
            Test(text, "C.Foo()", "N.C(Of T).Foo()")
            Test(text, "N.C.Foo()", "N.C(Of T).Foo()")
            Test(text, "Foo(i as Integer)")
            Test(text, "Foo(Integer)")
            Test(text, "Foo(a)")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
        Public Sub TestGenericNameInClassNamespace()
            Dim text =
<text>
namespace N
  class C
    sub Foo(of T)()
    end sub
  end class
end namespace</text>.Value

            Test(text, "Foo", "N.C.Foo(Of T)()")
            Test(text, "foo", "N.C.Foo(Of T)()")
            Test(text, "C.Foo", "N.C.Foo(Of T)()")
            Test(text, "N.C.Foo", "N.C.Foo(Of T)()")
            Test(text, "Foo(of T)", "N.C.Foo(Of T)()")
            Test(text, "Foo(of X)", "N.C.Foo(Of T)()")
            Test(text, "Foo(of T,X)")
            Test(text, "C(of T).Foo")
            Test(text, "C(of T).Foo()")
            Test(text, "Foo()", "N.C.Foo(Of T)()")
            Test(text, "C.Foo()", "N.C.Foo(Of T)()")
            Test(text, "N.C.Foo()", "N.C.Foo(Of T)()")
            Test(text, "Foo(i as Integer)")
            Test(text, "Foo(Integer)")
            Test(text, "Foo(a)")
            Test(text, "Foo(of T)(i as Integer)")
            Test(text, "Foo(of T)(Integer)")
            Test(text, "Foo(of T)(a)")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
        Public Sub TestOverloadsInSingleClass()
            Dim text =
<text>
class C
  sub Foo()
  end sub

  sub Foo(i as Integer)
  end sub
end class
</text>.Value

            Test(text, "Foo", "C.Foo()", "C.Foo(Integer)")
            Test(text, "foo", "C.Foo()", "C.Foo(Integer)")
            Test(text, "C.Foo", "C.Foo()", "C.Foo(Integer)")
            Test(text, "N.C.Foo")
            Test(text, "Foo(of T)")
            Test(text, "C(of T).Foo")
            Test(text, "Foo()", "C.Foo()")
            Test(text, "Foo(i as Integer)", "C.Foo(Integer)")
            Test(text, "Foo(Integer)", "C.Foo(Integer)")
            Test(text, "Foo(i)", "C.Foo(Integer)")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
        Public Sub TestMethodsInMultipleClasses()
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

            Test(text, "Foo", "N1.C.Foo(Integer)", "N.C.Foo()")
            Test(text, "foo", "N1.C.Foo(Integer)", "N.C.Foo()")
            Test(text, "C.Foo", "N1.C.Foo(Integer)", "N.C.Foo()")
            Test(text, "N.C.Foo", "N.C.Foo()")
            Test(text, "N1.C.Foo", "N1.C.Foo(Integer)")
            Test(text, "Foo(of T)")
            Test(text, "C(of T).Foo")
            Test(text, "Foo()", "N.C.Foo()")
            Test(text, "Foo(i as Integer)", "N1.C.Foo(Integer)")
            Test(text, "Foo(Integer)", "N1.C.Foo(Integer)")
            Test(text, "Foo(i)", "N1.C.Foo(Integer)")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
        Public Sub TestMethodsWithDifferentArityInMultipleClasses()
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

            Test(text, "Foo", "N1.C.Foo(Of T)(Integer)", "N.C.Foo()")
            Test(text, "foo", "N1.C.Foo(Of T)(Integer)", "N.C.Foo()")
            Test(text, "C.Foo", "N1.C.Foo(Of T)(Integer)", "N.C.Foo()")
            Test(text, "N.C.Foo", "N.C.Foo()")
            Test(text, "N1.C.Foo", "N1.C.Foo(Of T)(Integer)")
            Test(text, "Foo(of T)", "N1.C.Foo(Of T)(Integer)")
            Test(text, "C(of T).Foo")
            Test(text, "Foo()", "N.C.Foo()")
            Test(text, "Foo(of T)()")
            Test(text, "Foo(i as Integer)", "N1.C.Foo(Of T)(Integer)")
            Test(text, "Foo(Integer)", "N1.C.Foo(Of T)(Integer)")
            Test(text, "Foo(i)", "N1.C.Foo(Of T)(Integer)")
            Test(text, "Foo(of T)(i as Integer)", "N1.C.Foo(Of T)(Integer)")
            Test(text, "Foo(of T)(Integer)", "N1.C.Foo(Of T)(Integer)")
            Test(text, "Foo(of T)(i)", "N1.C.Foo(Of T)(Integer)")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
        Public Sub TestOverloadsWithMultipleParametersInSingleClass()
            Dim text =
<text>
class C
  sub Foo(a as Integer)
  end sub

  sub Foo(a as Integer, Optional b as String = "bb")
  end sub
end class</text>.Value

            Test(text, "Foo", "C.Foo(Integer)", "C.Foo(Integer, [String])")
            Test(text, "foo", "C.Foo(Integer)", "C.Foo(Integer, [String])")
            Test(text, "C.Foo", "C.Foo(Integer)", "C.Foo(Integer, [String])")
            Test(text, "N.C.Foo")
            Test(text, "Foo(of T)")
            Test(text, "C(of T).Foo")
            Test(text, "Foo()")
            Test(text, "Foo(i as Integer)", "C.Foo(Integer)")
            Test(text, "Foo(Integer)", "C.Foo(Integer)")
            Test(text, "Foo(i)", "C.Foo(Integer)")
            Test(text, "Foo(i as Integer, int b)", "C.Foo(Integer, [String])")
            Test(text, "Foo(int, boolean)", "C.Foo(Integer, [String])")
            Test(text, "Foo(i, s)", "C.Foo(Integer, [String])")
            Test(text, "Foo(,)", "C.Foo(Integer, [String])")
            Test(text, "Foo(x As Integer = 42,)", "C.Foo(Integer, [String])")
            Test(text, "Foo(Optional x As Integer = 42, y = 42)", "C.Foo(Integer, [String])")
            Test(text, "Foo(i as Integer, int b, char c)")
            Test(text, "Foo(int, bool, char)")
            Test(text, "Foo(i, s, c)")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
        Public Sub Properties()
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

            Test(text, "Property1", "C.Property1")
            Test(text, "property1()", "C.Property1")
            Test(text, "property2", "C.Property2")
            Test(text, "Property2(String)")
            Test(text, "property3", "C.Property3")
            Test(text, "property4", "C.Property4(Integer)")
            Test(text, "property5(j, i)", "C.Property5(Integer, String)")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
        Public Sub NegativeTests()
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

            Test(text, "AbstractMethod")
            Test(text, "Field")
            Test(text, "Delegate1")
            Test(text, "Event1")
            Test(text, "Property1")
            Test(text, "Property2")
            Test(text, "New")
            Test(text, "C.New")
            Test(text, "Foo", "C.Foo()", "C.Foo([Integer], [Integer])") ' just making sure it would normally resolve before trying bad syntax
            Test(text, "Foo Foo")
            Test(text, "Foo()asdf")
            Test(text, "Foo;")
            Test(text, "Foo();")
            Test(text, "Foo(),")
            Test(text, "Foo(),f")
            Test(text, "Foo().Foo")
            Test(text, "Foo(")
            Test(text, "(Foo")
            Test(text, "Foo)")
            Test(text, "(Foo)")
            Test(text, "Foo(x = 42, y = 42)", "C.Foo([Integer], [Integer])") ' just making sure it would normally resolve before trying bad syntax
            Test(text, "Foo[x = 42, y = 42]")
            Test(text, "Dim x As Integer = 42")
            Test(text, "Foo(Optional x As Integer = 42, y = 42")
            Test(text, "C")
            Test(text, "C.C")
            Test(text, "")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
        Public Sub TestInstanceConstructors()
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

            Test(text, "New", "G(Of T).New()", "C.New()")
            Test(text, "c.new", "C.New()")
            Test(text, "c.NEW()", "C.New()")
            Test(text, "New()", "G(Of T).New()", "C.New()")
            Test(text, "New(of T)")
            Test(text, "New(of T)()")
            Test(text, "New(i as Integer)")
            Test(text, "New(Integer)")
            Test(text, "New(i)")
            Test(text, "G.New", "G(Of T).New()")
            Test(text, "G.New()", "G(Of T).New()")
            Test(text, "G(Of t).new", "G(Of T).New()")
            Test(text, "G(of t).new()", "G(Of T).New()")
            Test(text, "G(Of T)")
            Test(text, "G(Of T)()")
            Test(text, "G.G(Of T)")
            Test(text, ".ctor")
            Test(text, ".ctor()")
            Test(text, "C.ctor")
            Test(text, "C.ctor()")
            Test(text, "G.ctor")
            Test(text, "G(Of T).ctor()")
            Test(text, "C")
            Test(text, "C.C")
            Test(text, "C.C()")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
        Public Sub TestStaticConstructors()
            Dim text =
<text>
class C
  shared sub new()
  end sub
end class</text>.Value

            Test(text, "New", "C.New()")
            Test(text, "C.New", "C.New()")
            Test(text, "C.New()", "C.New()")
            Test(text, "New()", "C.New()")
            Test(text, "New(of T)")
            Test(text, "New(of T)()")
            Test(text, "New(i as Integer)")
            Test(text, "New(Integer)")
            Test(text, "New(i)")
            Test(text, "C")
            Test(text, "C.C")
            Test(text, "C.C()")
            Test(text, "C.cctor")
            Test(text, "C.cctor()")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
        Public Sub TestAllConstructors()
            Dim text =
<text>
class C
  shared sub new()
  end sub

  public sub new(i as Integer)
  end sub
end class</text>.Value

            Test(text, "New", "C.New(Integer)", "C.New()")
            Test(text, "C.New", "C.New(Integer)", "C.New()")
            Test(text, "c.New()", "C.New()")
            Test(text, "new()", "C.New()")
            Test(text, "New(of T)")
            Test(text, "New(of T)()")
            Test(text, "New(i as Integer)", "C.New(Integer)")
            Test(text, "New(Integer)", "C.New(Integer)")
            Test(text, "New(i)", "C.New(Integer)")
            Test(text, "C")
            Test(text, "C.C")
            Test(text, "C.C()")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
        Public Sub TestPartialMethods()
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

            Test(text, "M1")
            Test(text, "C.M1")
            Test(text, "M2", "C.M2()")
            Test(text, "M3", "C.M3(Integer)")
            Test(text, "M3()")
            Test(text, "M3(y)", "C.M3(Integer)")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
        Public Sub TestLeadingAndTrailingText()
            Dim text =
<text>
Class C
  Sub Foo()
  End Sub
End Class</text>.Value

            Test(text, "  Foo", "C.Foo()")
            Test(text, "Foo() ", "C.Foo()")
            Test(text, " Foo (  )   ", "C.Foo()")
            Test(text, "Foo() ' comment", "C.Foo()")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
        Public Sub TestEscapedKeywords()
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

            Test(text, "where", "[for].where([true])")
            Test(text, "[where]", "[for].where([true])")
            Test(text, "[for].where", "[for].where([true])")
            Test(text, "for.where", "[for].where([true])")
            Test(text, "[for].where(true)", "[for].where([true])")
            Test(text, "[for].where([if])", "[for].where([true])")
            Test(text, "False", "[for].False()")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
        Public Sub TestGlobalQualifiedNames()
            Dim text =
<text>
Class C
  Sub Foo(c1 As C)
  End Sub
End Class</text>.Value

            Test(text, "Global.Foo")
            Test(text, "Global.C.Foo")
            Test(text, "Global.C.Foo(C)")
            Test(text, "C.Foo(Global.C)", "C.Foo(C)")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
        Public Sub TestRootNamespaces()
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

            TestWithRootNamespace("Root", text, "Foo", "Root.N1.C.Foo()", "Root.C.Foo()")
            TestWithRootNamespace("Root", text, "C.Foo", "Root.N1.C.Foo()", "Root.C.Foo()")
            TestWithRootNamespace("Root", text, "N1.C.Foo()", "Root.N1.C.Foo()")
            TestWithRootNamespace("Root", text, "Root.C.Foo()", "Root.C.Foo()")
            TestWithRootNamespace("Root", text, "Root.N1.C.Foo", "Root.N1.C.Foo()")
            TestWithRootNamespace("Root", text, "Root.Foo")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
        Public Sub TestNestedTypesAndNamespaces()
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

            Test(text, "Foo", "N1.N4.C.Foo(Double)", "N1.N4.C.D.Foo()", "N1.N4.C.D.E.Foo()", "N1.C.Foo()")
            Test(text, "C.Foo", "N1.N4.C.Foo(Double)", "N1.C.Foo()")
            Test(text, "D.Foo", "N1.N4.C.D.Foo()")
            Test(text, "N1.N4.C.D.Foo", "N1.N4.C.D.Foo()")
            Test(text, "N1.Foo")
            Test(text, "N3.C.Foo")
            Test(text, "N5.C.Foo")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)>
        Public Sub TestInterfaces()
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

            Test(text, "Foo")
            Test(text, "I1.Foo")
            Test(text, "Foo1", "C1.Foo1()")
            Test(text, "Moo", "C1.Moo()")
        End Sub

    End Class

End Namespace
