' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Extensions
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Classification
    Public Class SemanticClassifierTests
        Inherits AbstractVisualBasicClassifierTests

        Friend Overrides Async Function GetClassificationSpansAsync(code As String, textSpan As TextSpan) As Tasks.Task(Of IEnumerable(Of ClassifiedSpan))
            Using workspace = Await TestWorkspaceFactory.CreateVisualBasicWorkspaceAsync(code)
                Dim document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id)

                Dim service = document.GetLanguageService(Of IClassificationService)()

                Dim tree = Await document.GetSyntaxTreeAsync()

                Dim result = New List(Of ClassifiedSpan)
                Dim classifiers = service.GetDefaultSyntaxClassifiers()
                Dim extensionManager = workspace.Services.GetService(Of IExtensionManager)

                Await service.AddSemanticClassificationsAsync(document, textSpan,
                    extensionManager.CreateNodeExtensionGetter(classifiers, Function(c) c.SyntaxNodeTypes),
                    extensionManager.CreateTokenExtensionGetter(classifiers, Function(c) c.SyntaxTokenKinds),
                    result, CancellationToken.None)

                Return result
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestTypeName1() As Task
            Await TestInMethodAsync(
                className:="C(Of T)",
                methodName:="M",
                code:="Dim x As New C(Of Integer)()",
                expected:={[Class]("C")})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestImportsType() As Task
            Await TestAsync("Imports System.Console",
                [Class]("Console"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestImportsAlias() As Task
            Await TestAsync("Imports M = System.Math",
                 [Class]("M"),
                 [Class]("Math"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestMSCorlibTypes() As Task
            Dim text = StringFromLines(
                "Imports System",
                "Module Program",
                "    Sub Main(args As String())",
                "        Console.WriteLine()",
                "    End Sub",
                "End Module")
            Await TestAsync(text,
                [Class]("Console"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestConstructedGenericWithInvalidTypeArg() As Task
            Await TestInMethodAsync(
                className:="C(Of T)",
                methodName:="M",
                code:="Dim x As New C(Of UnknownType)()",
                expected:={[Class]("C")})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestMethodCall() As Task
            Await TestInMethodAsync(
                className:="Program",
                methodName:="M",
                code:="Program.Main()",
                expected:={[Class]("Program")})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(538647)>
        Public Async Function TestRegression4315_VariableNamesClassifiedAsType() As Task
            Dim text = StringFromLines(
                "Module M",
                "    Sub S()",
                "        Dim foo",
                "    End Sub",
                "End Module")
            Await TestAsync(text)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(541267)>
        Public Async Function TestRegression7925_TypeParameterCantCastToMethod() As Task
            Dim text = StringFromLines(
                "Class C",
                "    Sub GenericMethod(Of T1)(i As T1)",
                "    End Sub",
                "End Class")
            Await TestAsync(text,
                TypeParameter("T1"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(541610)>
        Public Async Function TestRegression8394_AliasesShouldBeClassified1() As Task
            Dim text = StringFromLines(
                "Imports S = System.String",
                "Class T",
                "    Dim x As S = ""hello""",
                "End Class")
            Await TestAsync(text,
                [Class]("S"),
                [Class]("String"),
                [Class]("S"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(541610)>
        Public Async Function TestRegression8394_AliasesShouldBeClassified2() As Task
            Dim text = StringFromLines(
                "Imports D = System.IDisposable",
                "Class T",
                "    Dim x As D = Nothing",
                "End Class")
            Await TestAsync(text,
                [Interface]("D"),
                [Interface]("IDisposable"),
                [Interface]("D"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestConstructorNew1() As Task
            Dim text = StringFromLines(
                "Class C",
                "    Sub New",
                "    End Sub",
                "    Sub [New]",
                "    End Sub",
                "    Sub New(x)",
                "        Me.New",
                "    End Sub",
                "End Class")
            Await TestAsync(text,
                 Keyword("New"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestConstructorNew2() As Task
            Dim text = StringFromLines(
                "Class B",
                "    Sub New()",
                "    End Sub",
                "End Class",
                "Class C",
                "    Inherits B",
                "    Sub New(x As Integer)",
                "        MyBase.New",
                "    End Sub",
                "End Class")
            Await TestAsync(text,
                 [Class]("B"),
                 Keyword("New"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestConstructorNew3() As Task
            Dim text = StringFromLines(
                "Class C",
                "    Sub New",
                "    End Sub",
                "    Sub [New]",
                "    End Sub",
                "    Sub New(x)",
                "        MyClass.New",
                "    End Sub",
                "End Class")
            Await TestAsync(text,
                 Keyword("New"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestConstructorNew4() As Task
            Dim text = StringFromLines(
                "Class C",
                "    Sub New",
                "    End Sub",
                "    Sub [New]",
                "    End Sub",
                "    Sub New(x)",
                "        With Me",
                "            .New",
                "        End With",
                "    End Sub",
                "End Class")
            Await TestAsync(text,
                 Keyword("New"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestAlias() As Task
            Dim text = StringFromLines(
                "Imports E = System.Exception",
                "Class C",
                "    Inherits E",
                "End Class")
            Await TestAsync(text,
                [Class]("E"),
                [Class]("Exception"),
                [Class]("E"))
        End Function

        <WorkItem(542685)>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestOptimisticallyColorFromInDeclaration() As Task
            Await TestInExpressionAsync("From ", Keyword("From"))
        End Function

        <WorkItem(542685)>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestOptimisticallyColorFromInAssignment() As Task
            Await TestInMethodAsync(<text><![CDATA[
                            Dim q = 3
                            q = From 
                            ]]></text>.NormalizedValue, Keyword("From"))
        End Function

        <WorkItem(542685)>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestDontColorThingsOtherThanFromInDeclaration() As Task
            Await TestInExpressionAsync("Fro ")
        End Function

        <WorkItem(542685)>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestDontColorThingsOtherThanFromInAssignment() As Task
            Await TestInMethodAsync(<text><![CDATA[
                            Dim q = 3
                            q = Fro 
                            ]]></text>.Value)
        End Function

        <WorkItem(542685)>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestDontColorFromWhenBoundInDeclaration() As Task
            Await TestInMethodAsync(<text><![CDATA[
                            Dim From = 3
                            Dim q = From
                            ]]></text>.Value)
        End Function

        <WorkItem(542685)>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestDontColorFromWhenBoundInAssignment() As Task
            Await TestInMethodAsync(<text><![CDATA[
                            Dim From = 3
                            Dim q = 3
                            q = From
                            ]]></text>.Value)
        End Function

        <Fact, WorkItem(10507, "DevDiv_Projects/Roslyn"), Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestArraysInGetType() As Task
            Await TestInMethodAsync("GetType(System.Exception()",
                         [Class]("Exception"))
            Await TestInMethodAsync("GetType(System.Exception(,)",
                         [Class]("Exception"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestNewOfInterface() As Task
            Await TestInMethodAsync("Dim a = New System.IDisposable()",
                         [Interface]("IDisposable"))
        End Function

        <WorkItem(543404)>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestNewOfClassWithNoPublicConstructors() As Task
            Dim text = StringFromLines(
                "Public Class C1",
                "    Private Sub New()",
                "    End Sub",
                "End Class",
                "Module Program",
                "    Sub Main()",
                "        Dim f As New C1()",
                "    End Sub",
                "End Module")

            Await TestAsync(text,
                [Class]("C1"))
        End Function

        <WorkItem(578145)>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestAsyncKeyword1() As Task
            Dim text =
<code>
Class C
    Sub M()
        Dim x = Async
    End Sub
End Class
</code>.NormalizedValue()

            Await TestAsync(text,
                Keyword("Async"))
        End Function

        <WorkItem(578145)>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestAsyncKeyword2() As Task
            Dim text =
<code>
Class C
    Sub M()
        Dim x = Async S
    End Sub
End Class
</code>.NormalizedValue()

            Await TestAsync(text,
                Keyword("Async"))
        End Function

        <WorkItem(578145)>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestAsyncKeyword3() As Task
            Dim text =
<code>
Class C
    Sub M()
        Dim x = Async Su
    End Sub
End Class
</code>.NormalizedValue()

            Await TestAsync(text,
                Keyword("Async"))
        End Function

        <WorkItem(578145)>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestAsyncKeyword4() As Task
            Dim text =
<code>
Class C
    Async
End Class
</code>.NormalizedValue()

            Await TestAsync(text,
                Keyword("Async"))
        End Function

        <WorkItem(578145)>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestAsyncKeyword5() As Task
            Dim text =
<code>
Class C
    Private Async
End Class
</code>.NormalizedValue()

            Await TestAsync(text,
                Keyword("Async"))
        End Function

        <WorkItem(578145)>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestAsyncKeyword6() As Task
            Dim text =
<code>
Class C
    Private Async As
End Class
</code>.NormalizedValue()

            Await TestAsync(text)
        End Function

        <WorkItem(578145)>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestAsyncKeyword7() As Task
            Dim text =
<code>
Class C
    Private Async =
End Class
</code>.NormalizedValue()

            Await TestAsync(text)
        End Function

        <WorkItem(578145)>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestIteratorKeyword1() As Task
            Dim text =
<code>
Class C
    Sub M()
        Dim x = Iterator
    End Sub
End Class
</code>.NormalizedValue()

            Await TestAsync(text,
                Keyword("Iterator"))
        End Function

        <WorkItem(578145)>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestIteratorKeyword2() As Task
            Dim text =
<code>
Class C
    Sub M()
        Dim x = Iterator F
    End Sub
End Class
</code>.NormalizedValue()

            Await TestAsync(text,
                Keyword("Iterator"))
        End Function

        <WorkItem(578145)>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestIteratorKeyword3() As Task
            Dim text =
<code>
Class C
    Sub M()
        Dim x = Iterator Functio
    End Sub
End Class
</code>.NormalizedValue()

            Await TestAsync(text,
                Keyword("Iterator"))
        End Function

        <WorkItem(578145)>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestIteratorKeyword4() As Task
            Dim text =
<code>
Class C
    Iterator
End Class
</code>.NormalizedValue()

            Await TestAsync(text,
                Keyword("Iterator"))
        End Function

        <WorkItem(578145)>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestIteratorKeyword5() As Task
            Dim text =
<code>
Class C
    Private Iterator
End Class
</code>.NormalizedValue()

            Await TestAsync(text,
                Keyword("Iterator"))
        End Function

        <WorkItem(578145)>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestIteratorKeyword6() As Task
            Dim text =
<code>
Class C
    Private Iterator As
End Class
</code>.NormalizedValue()

            Await TestAsync(text)
        End Function

        <WorkItem(578145)>
        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestIteratorKeyword7() As Task
            Dim text =
<code>
Class C
    Private Iterator =
End Class
</code>.NormalizedValue()

            Await TestAsync(text)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestMyNamespace() As Task
            Dim text =
<code>
Class C
    Sub M()
        Dim m = My.Foo
    End Sub
End Class
</code>.NormalizedValue()

            Await TestAsync(text,
                 Keyword("My"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestAwaitInNonAsyncFunction1() As Task
            Dim text =
<code>
dim m = Await
</code>.NormalizedValue()

            Await TestInMethodAsync(text,
                 Keyword("Await"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestAwaitInNonAsyncFunction2() As Task
            Dim text =
<code>
sub await()
end sub

sub test()
    dim m = Await
end sub
</code>.NormalizedValue()

            Await TestInClassAsync(text)
        End Function
    End Class
End Namespace