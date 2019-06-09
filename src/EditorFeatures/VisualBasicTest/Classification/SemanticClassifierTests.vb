' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Classification.FormattedClassifications
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Classification
    Public Class SemanticClassifierTests
        Inherits AbstractVisualBasicClassifierTests

        Protected Overrides Function GetClassificationSpansAsync(code As String, span As TextSpan, parseOptions As ParseOptions) As Task(Of ImmutableArray(Of ClassifiedSpan))
            Using workspace = TestWorkspace.CreateVisualBasic(code)
                Dim document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id)

                Return GetSemanticClassificationsAsync(document, span)
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestTypeName1() As Task
            Await TestInMethodAsync(
                className:="C(Of T)",
                methodName:="M",
                code:="Dim x As New C(Of Integer)()",
                [Class]("C"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestImportsType() As Task
            Await TestAsync("Imports System.Console",
            [Namespace]("System"),
            [Class]("Console"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestImportsAlias() As Task
            Await TestAsync("Imports M = System.Math",
                [Class]("M"),
                [Namespace]("System"),
                [Class]("Math"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestMSCorlibTypes() As Task
            Dim code =
"Imports System
Module Program
    Sub Main(args As String())
        Console.WriteLine()
    End Sub
End Module"

            Await TestAsync(code,
                [Namespace]("System"),
                [Class]("Console"),
                Method("WriteLine"),
                [Static]("WriteLine"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestConstructedGenericWithInvalidTypeArg() As Task
            Await TestInMethodAsync(
                className:="C(Of T)",
                methodName:="M",
                code:="Dim x As New C(Of UnknownType)()",
                [Class]("C"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestMethodCall() As Task
            Await TestInMethodAsync(
                className:="Program",
                methodName:="M",
                code:="Program.Main()",
                [Class]("Program"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(538647, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538647")>
        Public Async Function TestRegression4315_VariableNamesClassifiedAsType() As Task
            Dim code =
"Module M
    Sub S()
        Dim goo
    End Sub
End Module"

            Await TestAsync(code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(541267, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541267")>
        Public Async Function TestRegression7925_TypeParameterCantCastToMethod() As Task
            Dim code =
"Class C
    Sub GenericMethod(Of T1)(i As T1)
    End Sub
End Class"

            Await TestAsync(code,
                TypeParameter("T1"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(541610, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541610")>
        Public Async Function TestRegression8394_AliasesShouldBeClassified1() As Task
            Dim code =
"Imports S = System.String
Class T
    Dim x As S = ""hello""
End Class"

            Await TestAsync(code,
                [Class]("S"),
                [Namespace]("System"),
                [Class]("String"),
                [Class]("S"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(541610, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541610")>
        Public Async Function TestRegression8394_AliasesShouldBeClassified2() As Task
            Dim code =
"Imports D = System.IDisposable
Class T
    Dim x As D = Nothing
End Class"

            Await TestAsync(code,
                [Interface]("D"),
                [Namespace]("System"),
                [Interface]("IDisposable"),
                [Interface]("D"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestConstructorNew1() As Task
            Dim code =
"Class C
    Sub New
    End Sub
    Sub [New]
    End Sub
    Sub New(x)
        Me.New
    End Sub
End Class"

            Await TestAsync(code,
                Keyword("New"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestConstructorNew2() As Task
            Dim code =
"Class B
    Sub New()
    End Sub
End Class
Class C
    Inherits B
    Sub New(x As Integer)
        MyBase.New
    End Sub
End Class"

            Await TestAsync(code,
                [Class]("B"),
                Keyword("New"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestConstructorNew3() As Task
            Dim code =
"Class C
    Sub New
    End Sub
    Sub [New]
    End Sub
    Sub New(x)
        MyClass.New
    End Sub
End Class"

            Await TestAsync(code,
                Keyword("New"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestConstructorNew4() As Task
            Dim code =
"Class C
    Sub New
    End Sub
    Sub [New]
    End Sub
    Sub New(x)
        With Me
            .New
        End With
    End Sub
End Class"

            Await TestAsync(code,
                Keyword("New"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestAlias() As Task
            Dim code =
"Imports E = System.Exception
Class C
    Inherits E
End Class"

            Await TestAsync(code,
                [Class]("E"),
                [Namespace]("System"),
                [Class]("Exception"),
                [Class]("E"))
        End Function

        <WorkItem(542685, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542685")>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestOptimisticallyColorFromInDeclaration() As Task
            Await TestInExpressionAsync("From ",
                Keyword("From"))
        End Function

        <WorkItem(542685, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542685")>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestOptimisticallyColorFromInAssignment() As Task
            Dim code =
"Dim q = 3
q = From"

            Await TestInMethodAsync(code,
                Local("q"),
                Keyword("From"))
        End Function

        <WorkItem(542685, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542685")>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestDontColorThingsOtherThanFromInDeclaration() As Task
            Await TestInExpressionAsync("Fro ")
        End Function

        <WorkItem(542685, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542685")>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestDontColorThingsOtherThanFromInAssignment() As Task
            Dim code =
"Dim q = 3
q = Fro "

            Await TestInMethodAsync(code,
                Local("q"))
        End Function

        <WorkItem(542685, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542685")>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestDontColorFromWhenBoundInDeclaration() As Task
            Dim code =
"Dim From = 3
Dim q = From"

            Await TestInMethodAsync(code,
                Local("From"))
        End Function

        <WorkItem(542685, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542685")>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestDontColorFromWhenBoundInAssignment() As Task
            Dim code =
"Dim From = 3
Dim q = 3
q = From"

            Await TestInMethodAsync(code,
                Local("q"),
                Local("From"))
        End Function

        <Fact, WorkItem(10507, "DevDiv_Projects/Roslyn"), Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestArraysInGetType() As Task
            Await TestInMethodAsync("GetType(System.Exception()",
                [Namespace]("System"),
                [Class]("Exception"))
            Await TestInMethodAsync("GetType(System.Exception(,)",
                [Namespace]("System"),
                [Class]("Exception"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestNewOfInterface() As Task
            Await TestInMethodAsync("Dim a = New System.IDisposable()",
                [Namespace]("System"),
                [Interface]("IDisposable"))
        End Function

        <WorkItem(543404, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543404")>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestNewOfClassWithNoPublicConstructors() As Task
            Dim code =
"Public Class C1
    Private Sub New()
    End Sub
End Class
Module Program
    Sub Main()
        Dim f As New C1()
    End Sub
End Module"

            Await TestAsync(code,
                [Class]("C1"))
        End Function

        <WorkItem(578145, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578145")>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestAsyncKeyword1() As Task
            Dim code =
"Class C
    Sub M()
        Dim x = Async
    End Sub
End Class"

            Await TestAsync(code,
                Keyword("Async"))
        End Function

        <WorkItem(578145, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578145")>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestAsyncKeyword2() As Task
            Dim code =
"Class C
    Sub M()
        Dim x = Async S
    End Sub
End Class"

            Await TestAsync(code,
                Keyword("Async"))
        End Function

        <WorkItem(578145, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578145")>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestAsyncKeyword3() As Task
            Dim code =
"Class C
    Sub M()
        Dim x = Async Su
    End Sub
End Class"

            Await TestAsync(code,
                Keyword("Async"))
        End Function

        <WorkItem(578145, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578145")>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestAsyncKeyword4() As Task
            Dim code =
"Class C
    Async
End Class"

            Await TestAsync(code,
                Keyword("Async"))
        End Function

        <WorkItem(578145, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578145")>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestAsyncKeyword5() As Task
            Dim code =
"Class C
    Private Async
End Class"

            Await TestAsync(code,
                Keyword("Async"))
        End Function

        <WorkItem(578145, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578145")>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestAsyncKeyword6() As Task
            Dim code =
"Class C
    Private Async As
End Class"

            Await TestAsync(code)
        End Function

        <WorkItem(578145, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578145")>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestAsyncKeyword7() As Task
            Dim code =
"Class C
    Private Async =
End Class"

            Await TestAsync(code)
        End Function

        <WorkItem(578145, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578145")>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestIteratorKeyword1() As Task
            Dim code =
"Class C
    Sub M()
        Dim x = Iterator
    End Sub
End Class"

            Await TestAsync(code,
                Keyword("Iterator"))
        End Function

        <WorkItem(578145, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578145")>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestIteratorKeyword2() As Task
            Dim code =
"Class C
    Sub M()
        Dim x = Iterator F
    End Sub
End Class"

            Await TestAsync(code,
                Keyword("Iterator"))
        End Function

        <WorkItem(578145, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578145")>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestIteratorKeyword3() As Task
            Dim code =
"Class C
    Sub M()
        Dim x = Iterator Functio
    End Sub
End Class"

            Await TestAsync(code,
                Keyword("Iterator"))
        End Function

        <WorkItem(578145, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578145")>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestIteratorKeyword4() As Task
            Dim code =
"Class C
    Iterator
End Class"

            Await TestAsync(code,
                Keyword("Iterator"))
        End Function

        <WorkItem(578145, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578145")>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestIteratorKeyword5() As Task
            Dim code =
"Class C
    Private Iterator
End Class"

            Await TestAsync(code,
                Keyword("Iterator"))
        End Function

        <WorkItem(578145, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578145")>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestIteratorKeyword6() As Task
            Dim code =
"Class C
    Private Iterator As
End Class"

            Await TestAsync(code)
        End Function

        <WorkItem(578145, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578145")>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestIteratorKeyword7() As Task
            Dim code =
"Class C
    Private Iterator =
End Class"

            Await TestAsync(code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestMyNamespace() As Task
            Dim code =
"Class C
    Sub M()
        Dim m = My.Goo
    End Sub
End Class"

            Await TestAsync(code,
                Keyword("My"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestAwaitInNonAsyncFunction1() As Task
            Dim code = "dim m = Await"

            Await TestInMethodAsync(code,
                Keyword("Await"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestAwaitInNonAsyncFunction2() As Task
            Dim code =
"sub await()
end sub

sub test()
    dim m = Await
end sub"

            Await TestInClassAsync(code,
                Method("Await"))
        End Function

        <WorkItem(21524, "https://github.com/dotnet/roslyn/issues/21524")>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestAttribute() As Task
            Dim code =
"Imports System

<AttributeUsage()>
Class Program
End Class"

            Await TestAsync(code,
                [Namespace]("System"), [Class]("AttributeUsage"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestRegex1() As Task
            Await TestAsync(
"
imports System.Text.RegularExpressions

class Program
    sub Goo()
        ' language=regex
        var r = ""$(\b\G\z)|(?<name>sub){0,5}?^""
    end sub
end class",
[Namespace]("System"),
[Namespace]("Text"),
[Namespace]("RegularExpressions"),
Regex.Anchor("$"),
Regex.Grouping("("),
Regex.Anchor("\"),
Regex.Anchor("b"),
Regex.Anchor("\"),
Regex.Anchor("G"),
Regex.Anchor("\"),
Regex.Anchor("z"),
Regex.Grouping(")"),
Regex.Alternation("|"),
Regex.Grouping("("),
Regex.Grouping("?"),
Regex.Grouping("<"),
Regex.Grouping("name"),
Regex.Grouping(">"),
Regex.Text("sub"),
Regex.Grouping(")"),
Regex.Quantifier("{"),
Regex.Quantifier("0"),
Regex.Quantifier(","),
Regex.Quantifier("5"),
Regex.Quantifier("}"),
Regex.Quantifier("?"),
Regex.Anchor("^"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestConstField() As Task
            Dim code =
"Const Number = 42
Dim x As Integer = Number"

            Await TestInClassAsync(code,
                Constant("Number"),
                [Static]("Number"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestConstLocal() As Task
            Dim code =
"Const Number = 42
Dim x As Integer = Number"

            Await TestInMethodAsync(code,
                Constant("Number"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestModifiedIdentifiersInLocals() As Task
            Dim code =
"Dim x$ = ""23""
x$ = ""19"""

            Await TestInMethodAsync(code,
                Local("x$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestModifiedIdentifiersInFields() As Task
            Dim code =
"Const x$ = ""23""
Dim y$ = x$"

            Await TestInClassAsync(code,
                Constant("x$"),
                [Static]("x$"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestFunctionNamesWithTypeCharacters() As Task
            Dim code =
"Function x%()
    x% = 42
End Function"

            Await TestInClassAsync(code,
                Local("x%"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestExtensionMethod() As Task
            Dim code = "
Imports System.Runtime.CompilerServices

Module M
    <Extension>
    Sub Square(ByRef x As Integer)
        x = x * x
    End Sub
End Module

Class C
    Sub Test()
        Dim x = 42
        x.Square()
        M.Square(x)
    End Sub
End Class"

            Await TestAsync(code,
                [Namespace]("System"),
                [Namespace]("Runtime"),
                [Namespace]("CompilerServices"),
                [Class]("Extension"),
                ExtensionMethod("Square"),
                Parameter("x"),
                Parameter("x"),
                Parameter("x"),
                Local("x"),
                ExtensionMethod("Square"),
                [Module]("M"),
                Method("Square"),
                [Static]("Square"),
                Local("x"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestSimpleEvent() As Task
            Dim code = "
Event E(x As Integer)

Sub M()
    RaiseEvent E(42)
End Sub"

            Await TestInClassAsync(code,
                [Event]("E"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestOperators() As Task
            Dim code = "
Public Shared Operator Not(t As Test) As Test
    Return New Test()
End Operator
Public Shared Operator +(t1 As Test, t2 As Test) As Integer
    Return 1
End Operator"

            Await TestInClassAsync(code)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestStringEscape1() As Task
            Await TestInMethodAsync("dim goo = ""goo""""bar""",
                Escape(""""""))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestStringEscape2() As Task
            Await TestInMethodAsync("dim goo = $""goo{{1}}bar""",
                Escape("{{"),
                Escape("}}"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestStringEscape3() As Task
            Await TestInMethodAsync("dim goo = $""goo""""{{1}}""""bar""",
                Escape(""""""),
                Escape("{{"),
                Escape("}}"),
                Escape(""""""))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestStringEscape4() As Task
            Await TestInMethodAsync("dim goo = $""goo""""{1}""""bar""",
                Escape(""""""),
                Escape(""""""))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestStringEscape5() As Task
            Await TestInMethodAsync("dim goo = $""{{goo{1}bar}}""",
                Escape("{{"),
                Escape("}}"))
        End Function

        <WorkItem(29451, "https://github.com/dotnet/roslyn/issues/29451")>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestDirectiveStringLiteral() As Task
            Await TestAsync("#region ""goo""""bar""",
                Escape(""""""))
        End Function

        <WorkItem(30378, "https://github.com/dotnet/roslyn/issues/30378")>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestFormatSpecifierInInterpolation() As Task
            Await TestInMethodAsync("dim goo = $""goo{{1:0000}}bar""",
                Escape("{{"),
                Escape("}}"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestLabelName() As Task
            Dim code = "
Sub M()
E:
    GoTo E
End Sub"

            Await TestInClassAsync(code,
                [Label]("E"))
        End Function

        <WorkItem(29492, "https://github.com/dotnet/roslyn/issues/29492")>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestOperatorOverloads_BinaryExpression() As Task
            Dim code =
"Class C
    Public Sub M(a As C)
        Dim b = 1 + 1
        Dim c = a + Me
    End Sub

    Public Shared Operator +(a As C, b As C) As C
        Return New C
    End Operator
End Class"

            Await TestAsync(code,
                [Class]("C"),
                Parameter("a"),
                OverloadedOperators.Plus,
                [Class]("C"),
                [Class]("C"),
                [Class]("C"),
                [Class]("C"))
        End Function

        <WorkItem(29492, "https://github.com/dotnet/roslyn/issues/29492")>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestOperatorOverloads_UnaryExpression() As Task
            Dim code =
"Class C
    Public Sub M()
        Dim b = -1
        Dim c = -Me
    End Sub

    Public Shared Operator -(a As C) As C
        Return New C
    End Operator
End Class"

            Await TestAsync(code,
                OverloadedOperators.Minus,
                [Class]("C"),
                [Class]("C"),
                [Class]("C"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestCatchStatement() As Task
            Dim code =
"Try

Catch ex As Exception
    Throw ex
End Try"

            Await TestInMethodAsync(code,
                Local("ex"),
                [Class]("Exception"),
                Local("ex"))
        End Function
    End Class
End Namespace
