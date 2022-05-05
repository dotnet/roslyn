' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Classification.FormattedClassifications
Imports Microsoft.CodeAnalysis.Remote.Testing
Imports Microsoft.CodeAnalysis.Test.Utilities.EmbeddedLanguages
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Classification
    <Trait(Traits.Feature, Traits.Features.Classification)>
    Public Class SemanticClassifierTests
        Inherits AbstractVisualBasicClassifierTests

        Protected Overrides Async Function GetClassificationSpansAsync(code As String, span As TextSpan, parseOptions As ParseOptions, testHost As TestHost) As Task(Of ImmutableArray(Of ClassifiedSpan))
            Using workspace = CreateWorkspace(code, testHost)
                Dim document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id)

                Return Await GetSemanticClassificationsAsync(document, span)
            End Using
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestTypeName1(testHost As TestHost) As Task
            Await TestInMethodAsync(
                className:="C(Of T)",
                methodName:="M",
                code:="Dim x As New C(Of Integer)()",
                testHost,
                [Class]("C"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestImportsType(testHost As TestHost) As Task
            Await TestAsync("Imports System.Console",
                testHost,
                [Namespace]("System"),
                [Class]("Console"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestImportsAlias(testHost As TestHost) As Task
            Await TestAsync("Imports M = System.Math",
                testHost,
                [Class]("M"),
                [Namespace]("System"),
                [Class]("Math"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestMSCorlibTypes(testHost As TestHost) As Task
            Dim code =
"Imports System
Module Program
    Sub Main(args As String())
        Console.WriteLine()
    End Sub
End Module"

            Await TestAsync(code,
                testHost,
                [Namespace]("System"),
                [Class]("Console"),
                Method("WriteLine"),
                [Static]("WriteLine"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestConstructedGenericWithInvalidTypeArg(testHost As TestHost) As Task
            Await TestInMethodAsync(
                className:="C(Of T)",
                methodName:="M",
                code:="Dim x As New C(Of UnknownType)()",
                testHost:=testHost,
                [Class]("C"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestMethodCall(testHost As TestHost) As Task
            Await TestInMethodAsync(
                className:="Program",
                methodName:="M",
                code:="Program.Main()",
                testHost:=testHost,
                [Class]("Program"))
        End Function

        <Theory, CombinatorialData>
        <WorkItem(538647, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538647")>
        Public Async Function TestRegression4315_VariableNamesClassifiedAsType(testHost As TestHost) As Task
            Dim code =
"Module M
    Sub S()
        Dim goo
    End Sub
End Module"

            Await TestAsync(code, testHost)
        End Function

        <Theory, CombinatorialData>
        <WorkItem(541267, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541267")>
        Public Async Function TestRegression7925_TypeParameterCantCastToMethod(testHost As TestHost) As Task
            Dim code =
"Class C
    Sub GenericMethod(Of T1)(i As T1)
    End Sub
End Class"

            Await TestAsync(code,
                testHost,
                TypeParameter("T1"))
        End Function

        <Theory, CombinatorialData>
        <WorkItem(541610, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541610")>
        Public Async Function TestRegression8394_AliasesShouldBeClassified1(testHost As TestHost) As Task
            Dim code =
"Imports S = System.String
Class T
    Dim x As S = ""hello""
End Class"

            Await TestAsync(code,
                testHost,
                [Class]("S"),
                [Namespace]("System"),
                [Class]("String"),
                [Class]("S"))
        End Function

        <Theory, CombinatorialData>
        <WorkItem(541610, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541610")>
        Public Async Function TestRegression8394_AliasesShouldBeClassified2(testHost As TestHost) As Task
            Dim code =
"Imports D = System.IDisposable
Class T
    Dim x As D = Nothing
End Class"

            Await TestAsync(code,
                testHost,
                [Interface]("D"),
                [Namespace]("System"),
                [Interface]("IDisposable"),
                [Interface]("D"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestConstructorNew1(testHost As TestHost) As Task
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
                testHost,
                Keyword("New"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestConstructorNew2(testHost As TestHost) As Task
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
                testHost,
                [Class]("B"),
                Keyword("New"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestConstructorNew3(testHost As TestHost) As Task
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
                testHost,
                Keyword("New"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestConstructorNew4(testHost As TestHost) As Task
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
                testHost,
                Keyword("New"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestAlias(testHost As TestHost) As Task
            Dim code =
"Imports E = System.Exception
Class C
    Inherits E
End Class"

            Await TestAsync(code,
                testHost,
                [Class]("E"),
                [Namespace]("System"),
                [Class]("Exception"),
                [Class]("E"))
        End Function

        <WorkItem(542685, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542685")>
        <Theory, CombinatorialData>
        Public Async Function TestOptimisticallyColorFromInDeclaration(testHost As TestHost) As Task
            Await TestInExpressionAsync("From ",
                testHost,
                Keyword("From"))
        End Function

        <WorkItem(542685, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542685")>
        <Theory, CombinatorialData>
        Public Async Function TestOptimisticallyColorFromInAssignment(testHost As TestHost) As Task
            Dim code =
"Dim q = 3
q = From"

            Await TestInMethodAsync(code,
                testHost,
                Local("q"),
                Keyword("From"))
        End Function

        <WorkItem(542685, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542685")>
        <Theory, CombinatorialData>
        Public Async Function TestDontColorThingsOtherThanFromInDeclaration(testHost As TestHost) As Task
            Await TestInExpressionAsync("Fro ", testHost)
        End Function

        <WorkItem(542685, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542685")>
        <Theory, CombinatorialData>
        Public Async Function TestDontColorThingsOtherThanFromInAssignment(testHost As TestHost) As Task
            Dim code =
"Dim q = 3
q = Fro "

            Await TestInMethodAsync(code,
                testHost,
                Local("q"))
        End Function

        <WorkItem(542685, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542685")>
        <Theory, CombinatorialData>
        Public Async Function TestDontColorFromWhenBoundInDeclaration(testHost As TestHost) As Task
            Dim code =
"Dim From = 3
Dim q = From"

            Await TestInMethodAsync(code,
                testHost,
                Local("From"))
        End Function

        <Theory, CombinatorialData>
        <WorkItem(542685, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542685")>
        Public Async Function TestDontColorFromWhenBoundInAssignment(testHost As TestHost) As Task
            Dim code =
"Dim From = 3
Dim q = 3
q = From"

            Await TestInMethodAsync(code,
                testHost,
                Local("q"),
                Local("From"))
        End Function

        <Theory, CombinatorialData>
        <WorkItem(10507, "DevDiv_Projects/Roslyn")>
        Public Async Function TestArraysInGetType(testHost As TestHost) As Task
            Await TestInMethodAsync("GetType(System.Exception()",
                testHost,
                [Namespace]("System"),
                [Class]("Exception"))
            Await TestInMethodAsync("GetType(System.Exception(,)",
                testHost,
                [Namespace]("System"),
                [Class]("Exception"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestNewOfInterface(testHost As TestHost) As Task
            Await TestInMethodAsync("Dim a = New System.IDisposable()",
                testHost,
                [Namespace]("System"),
                [Interface]("IDisposable"))
        End Function

        <WorkItem(543404, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543404")>
        <Theory, CombinatorialData>
        Public Async Function TestNewOfClassWithNoPublicConstructors(testHost As TestHost) As Task
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
                testHost,
                [Class]("C1"))
        End Function

        <WorkItem(578145, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578145")>
        <Theory, CombinatorialData>
        Public Async Function TestAsyncKeyword1(testHost As TestHost) As Task
            Dim code =
"Class C
    Sub M()
        Dim x = Async
    End Sub
End Class"

            Await TestAsync(code,
                testHost,
                Keyword("Async"))
        End Function

        <WorkItem(578145, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578145")>
        <Theory, CombinatorialData>
        Public Async Function TestAsyncKeyword2(testHost As TestHost) As Task
            Dim code =
"Class C
    Sub M()
        Dim x = Async S
    End Sub
End Class"

            Await TestAsync(code,
                testHost,
                Keyword("Async"))
        End Function

        <WorkItem(578145, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578145")>
        <Theory, CombinatorialData>
        Public Async Function TestAsyncKeyword3(testHost As TestHost) As Task
            Dim code =
"Class C
    Sub M()
        Dim x = Async Su
    End Sub
End Class"

            Await TestAsync(code,
                testHost,
                Keyword("Async"))
        End Function

        <WorkItem(578145, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578145")>
        <Theory, CombinatorialData>
        Public Async Function TestAsyncKeyword4(testHost As TestHost) As Task
            Dim code =
"Class C
    Async
End Class"

            Await TestAsync(code,
                testHost,
                Keyword("Async"))
        End Function

        <WorkItem(578145, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578145")>
        <Theory, CombinatorialData>
        Public Async Function TestAsyncKeyword5(testHost As TestHost) As Task
            Dim code =
"Class C
    Private Async
End Class"

            Await TestAsync(code,
                testHost,
                Keyword("Async"))
        End Function

        <WorkItem(578145, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578145")>
        <Theory, CombinatorialData>
        Public Async Function TestAsyncKeyword6(testHost As TestHost) As Task
            Dim code =
"Class C
    Private Async As
End Class"

            Await TestAsync(code, testHost)
        End Function

        <WorkItem(578145, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578145")>
        <Theory, CombinatorialData>
        Public Async Function TestAsyncKeyword7(testHost As TestHost) As Task
            Dim code =
"Class C
    Private Async =
End Class"

            Await TestAsync(code, testHost)
        End Function

        <WorkItem(578145, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578145")>
        <Theory, CombinatorialData>
        Public Async Function TestIteratorKeyword1(testHost As TestHost) As Task
            Dim code =
"Class C
    Sub M()
        Dim x = Iterator
    End Sub
End Class"

            Await TestAsync(code,
                testHost,
                Keyword("Iterator"))
        End Function

        <WorkItem(578145, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578145")>
        <Theory, CombinatorialData>
        Public Async Function TestIteratorKeyword2(testHost As TestHost) As Task
            Dim code =
"Class C
    Sub M()
        Dim x = Iterator F
    End Sub
End Class"

            Await TestAsync(code,
                testHost,
                Keyword("Iterator"))
        End Function

        <WorkItem(578145, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578145")>
        <Theory, CombinatorialData>
        Public Async Function TestIteratorKeyword3(testHost As TestHost) As Task
            Dim code =
"Class C
    Sub M()
        Dim x = Iterator Functio
    End Sub
End Class"

            Await TestAsync(code,
                testHost,
                Keyword("Iterator"))
        End Function

        <WorkItem(578145, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578145")>
        <Theory, CombinatorialData>
        Public Async Function TestIteratorKeyword4(testHost As TestHost) As Task
            Dim code =
"Class C
    Iterator
End Class"

            Await TestAsync(code,
                testHost,
                Keyword("Iterator"))
        End Function

        <WorkItem(578145, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578145")>
        <Theory, CombinatorialData>
        Public Async Function TestIteratorKeyword5(testHost As TestHost) As Task
            Dim code =
"Class C
    Private Iterator
End Class"

            Await TestAsync(code,
                testHost,
                Keyword("Iterator"))
        End Function

        <WorkItem(578145, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578145")>
        <Theory, CombinatorialData>
        Public Async Function TestIteratorKeyword6(testHost As TestHost) As Task
            Dim code =
"Class C
    Private Iterator As
End Class"

            Await TestAsync(code, testHost)
        End Function

        <WorkItem(578145, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578145")>
        <Theory, CombinatorialData>
        Public Async Function TestIteratorKeyword7(testHost As TestHost) As Task
            Dim code =
"Class C
    Private Iterator =
End Class"

            Await TestAsync(code, testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestMyNamespace(testHost As TestHost) As Task
            Dim code =
"Class C
    Sub M()
        Dim m = My.Goo
    End Sub
End Class"

            Await TestAsync(code,
                testHost,
                Keyword("My"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestAwaitInNonAsyncFunction1(testHost As TestHost) As Task
            Dim code = "dim m = Await"

            Await TestInMethodAsync(code,
                testHost,
                Keyword("Await"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestAwaitInNonAsyncFunction2(testHost As TestHost) As Task
            Dim code =
"sub await()
end sub

sub test()
    dim m = Await
end sub"

            Await TestInClassAsync(code,
                testHost,
                Method("Await"))
        End Function

        <Theory, CombinatorialData>
        <WorkItem(21524, "https://github.com/dotnet/roslyn/issues/21524")>
        Public Async Function TestAttribute(testHost As TestHost) As Task
            Dim code =
"Imports System

<AttributeUsage()>
Class Program
End Class"

            Await TestAsync(code,
                testHost,
                [Namespace]("System"), [Class]("AttributeUsage"))
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestRegex1(testHost As TestHost) As Task
            Await TestAsync(
"
imports System.Text.RegularExpressions

class Program
    sub Goo()
        ' language=regex
        var r = ""$(\b\G\z)|(?<name>sub){0,5}?^""
    end sub
end class",
                testHost,
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

        <WpfTheory, CombinatorialData>
        Public Async Function TestRegexStringSyntaxAttribute_Field(testHost As TestHost) As Task
            Await TestAsync(
"
imports System.Diagnostics.CodeAnalysis
imports System.Text.RegularExpressions

class Program
    <StringSyntax(StringSyntaxAttribute.Regex)>
    dim field as string

    sub Goo()
        [|me.field = ""$(\b\G\z)""|]
    end sub
end class" & EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeVB,
                testHost,
Field("field"),
Regex.Anchor("$"),
Regex.Grouping("("),
Regex.Anchor("\"),
Regex.Anchor("b"),
Regex.Anchor("\"),
Regex.Anchor("G"),
Regex.Anchor("\"),
Regex.Anchor("z"),
Regex.Grouping(")"))
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestRegexStringSyntaxAttribute_Attribute(testHost As TestHost) As Task
            Await TestAsync(
"
imports system
imports System.Diagnostics.CodeAnalysis
imports System.Text.RegularExpressions

<AttributeUsage(AttributeTargets.Field)>
class RegexTestAttribute 
    inherits Attribute

    public sub new(<StringSyntax(StringSyntaxAttribute.Regex)> value as string)
    end sub
end class

class Program
    [|<RegexTest(""$(\b\G\z)"")>|]
    dim field as string
end class" & EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeVB,
                testHost,
[Class]("RegexTest"),
Regex.Anchor("$"),
Regex.Grouping("("),
Regex.Anchor("\"),
Regex.Anchor("b"),
Regex.Anchor("\"),
Regex.Anchor("G"),
Regex.Anchor("\"),
Regex.Anchor("z"),
Regex.Grouping(")"))
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestRegexStringSyntaxAttribute_Property(testHost As TestHost) As Task
            Await TestAsync(
"
imports System.Diagnostics.CodeAnalysis
imports System.Text.RegularExpressions

class Program
    <StringSyntax(StringSyntaxAttribute.Regex)>
    property prop as string

    sub Goo()
        [|me.prop = ""$(\b\G\z)""|]
    end sub
end class" & EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeVB,
                testHost,
[Property]("prop"),
Regex.Anchor("$"),
Regex.Grouping("("),
Regex.Anchor("\"),
Regex.Anchor("b"),
Regex.Anchor("\"),
Regex.Anchor("G"),
Regex.Anchor("\"),
Regex.Anchor("z"),
Regex.Grouping(")"))
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestRegexStringSyntaxAttribute_Sub(testHost As TestHost) As Task
            Await TestAsync(
"
imports System.Diagnostics.CodeAnalysis
imports System.Text.RegularExpressions

class Program
    sub M(<StringSyntax(StringSyntaxAttribute.Regex)>p as string)
    end sub

    sub Goo()
        [|M(""$(\b\G\z)"")|]
    end sub
end class" & EmbeddedLanguagesTestConstants.StringSyntaxAttributeCodeVB,
                testHost,
Method("M"),
Regex.Anchor("$"),
Regex.Grouping("("),
Regex.Anchor("\"),
Regex.Anchor("b"),
Regex.Anchor("\"),
Regex.Anchor("G"),
Regex.Anchor("\"),
Regex.Anchor("z"),
Regex.Grouping(")"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestConstField(testHost As TestHost) As Task
            Dim code =
"Const Number = 42
Dim x As Integer = Number"

            Await TestInClassAsync(code,
                testHost,
                Constant("Number"),
                [Static]("Number"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestConstLocal(testHost As TestHost) As Task
            Dim code =
"Const Number = 42
Dim x As Integer = Number"

            Await TestInMethodAsync(code,
                testHost,
                Constant("Number"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestModifiedIdentifiersInLocals(testHost As TestHost) As Task
            Dim code =
"Dim x$ = ""23""
x$ = ""19"""

            Await TestInMethodAsync(code,
                testHost,
                Local("x$"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestModifiedIdentifiersInFields(testHost As TestHost) As Task
            Dim code =
"Const x$ = ""23""
Dim y$ = x$"

            Await TestInClassAsync(code,
                testHost,
                Constant("x$"),
                [Static]("x$"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestFunctionNamesWithTypeCharacters(testHost As TestHost) As Task
            Dim code =
"Function x%()
    x% = 42
End Function"

            Await TestInClassAsync(code,
                testHost,
                Local("x%"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestExtensionMethod(testHost As TestHost) As Task
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
                testHost,
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

        <Theory, CombinatorialData>
        Public Async Function TestSimpleEvent(testHost As TestHost) As Task
            Dim code = "
Event E(x As Integer)

Sub M()
    RaiseEvent E(42)
End Sub"

            Await TestInClassAsync(code,
                testHost,
                [Event]("E"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestOperators(testHost As TestHost) As Task
            Dim code = "
Public Shared Operator Not(t As Test) As Test
    Return New Test()
End Operator
Public Shared Operator +(t1 As Test, t2 As Test) As Integer
    Return 1
End Operator"

            Await TestInClassAsync(code, testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestStringEscape1(testHost As TestHost) As Task
            Await TestInMethodAsync("dim goo = ""goo""""bar""",
                testHost,
                Escape(""""""))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestStringEscape2(testHost As TestHost) As Task
            Await TestInMethodAsync("dim goo = $""goo{{1}}bar""",
                testHost,
                Escape("{{"),
                Escape("}}"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestStringEscape3(testHost As TestHost) As Task
            Await TestInMethodAsync("dim goo = $""goo""""{{1}}""""bar""",
                testHost,
                Escape(""""""),
                Escape("{{"),
                Escape("}}"),
                Escape(""""""))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestStringEscape4(testHost As TestHost) As Task
            Await TestInMethodAsync("dim goo = $""goo""""{1}""""bar""",
                testHost,
                Escape(""""""),
                Escape(""""""))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestStringEscape5(testHost As TestHost) As Task
            Await TestInMethodAsync("dim goo = $""{{goo{1}bar}}""",
                testHost,
                Escape("{{"),
                Escape("}}"))
        End Function

        <WorkItem(29451, "https://github.com/dotnet/roslyn/issues/29451")>
        <Theory, CombinatorialData>
        Public Async Function TestDirectiveStringLiteral(testHost As TestHost) As Task
            Await TestAsync("#region ""goo""""bar""",
                testHost,
                Escape(""""""))
        End Function

        <WorkItem(30378, "https://github.com/dotnet/roslyn/issues/30378")>
        <Theory, CombinatorialData>
        Public Async Function TestFormatSpecifierInInterpolation(testHost As TestHost) As Task
            Await TestInMethodAsync("dim goo = $""goo{{1:0000}}bar""",
                testHost,
                Escape("{{"),
                Escape("}}"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestLabelName(testHost As TestHost) As Task
            Dim code = "
Sub M()
E:
    GoTo E
End Sub"

            Await TestInClassAsync(code,
                testHost,
                [Label]("E"))
        End Function

        <WorkItem(29492, "https://github.com/dotnet/roslyn/issues/29492")>
        <Theory, CombinatorialData>
        Public Async Function TestOperatorOverloads_BinaryExpression(testHost As TestHost) As Task
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
                testHost,
                [Class]("C"),
                Parameter("a"),
                OverloadedOperators.Plus,
                [Class]("C"),
                [Class]("C"),
                [Class]("C"),
                [Class]("C"))
        End Function

        <WorkItem(29492, "https://github.com/dotnet/roslyn/issues/29492")>
        <Theory, CombinatorialData>
        Public Async Function TestOperatorOverloads_UnaryExpression(testHost As TestHost) As Task
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
                testHost,
                OverloadedOperators.Minus,
                [Class]("C"),
                [Class]("C"),
                [Class]("C"))
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestCatchStatement(testHost As TestHost) As Task
            Dim code =
"Try

Catch ex As Exception
    Throw ex
End Try"

            Await TestInMethodAsync(code,
                testHost,
                Local("ex"),
                [Class]("Exception"),
                Local("ex"))
        End Function
    End Class
End Namespace
