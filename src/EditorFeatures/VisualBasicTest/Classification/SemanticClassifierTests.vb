' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Extensions
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Classification
    Public Class SemanticClassifierTests
        Inherits AbstractVisualBasicClassifierTests

        Friend Overrides Function GetClassificationSpans(code As String, textSpan As TextSpan) As IEnumerable(Of ClassifiedSpan)
            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromFile(code)
                Dim document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id)

                Dim service = document.GetLanguageService(Of IClassificationService)()

                Dim tree = document.GetSyntaxTreeAsync().Result

                Dim result = New List(Of ClassifiedSpan)
                Dim classifiers = service.GetDefaultSyntaxClassifiers()
                Dim extensionManager = workspace.Services.GetService(Of IExtensionManager)

                service.AddSemanticClassificationsAsync(document, textSpan,
                    extensionManager.CreateNodeExtensionGetter(classifiers, Function(c) c.SyntaxNodeTypes),
                    extensionManager.CreateTokenExtensionGetter(classifiers, Function(c) c.SyntaxTokenKinds),
                    result, CancellationToken.None).Wait()

                Return result
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub TypeName1()
            TestInMethod(
                className:="C(Of T)",
                methodName:="M",
                code:="Dim x As New C(Of Integer)()",
                expected:={[Class]("C")})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub ImportsType()
            Test("Imports System.Console",
                [Class]("Console"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub ImportsAlias()
            Test("Imports M = System.Math",
                 [Class]("M"),
                 [Class]("Math"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub MSCorlibTypes()
            Dim text = StringFromLines(
                "Imports System",
                "Module Program",
                "    Sub Main(args As String())",
                "        Console.WriteLine()",
                "    End Sub",
                "End Module")
            Test(text,
                [Class]("Console"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub ConstructedGenericWithInvalidTypeArg()
            TestInMethod(
                className:="C(Of T)",
                methodName:="M",
                code:="Dim x As New C(Of UnknownType)()",
                expected:={[Class]("C")})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub MethodCall()
            TestInMethod(
                className:="Program",
                methodName:="M",
                code:="Program.Main()",
                expected:={[Class]("Program")})
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(538647)>
        Public Sub Regression4315_VariableNamesClassifiedAsType()
            Dim text = StringFromLines(
                "Module M",
                "    Sub S()",
                "        Dim foo",
                "    End Sub",
                "End Module")
            Test(text)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(541267)>
        Public Sub Regression7925_TypeParameterCantCastToMethod()
            Dim text = StringFromLines(
                "Class C",
                "    Sub GenericMethod(Of T1)(i As T1)",
                "    End Sub",
                "End Class")
            Test(text,
                TypeParameter("T1"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(541610)>
        Public Sub Regression8394_AliasesShouldBeClassified1()
            Dim text = StringFromLines(
                "Imports S = System.String",
                "Class T",
                "    Dim x As S = ""hello""",
                "End Class")
            Test(text,
                [Class]("S"),
                [Class]("String"),
                [Class]("S"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        <WorkItem(541610)>
        Public Sub Regression8394_AliasesShouldBeClassified2()
            Dim text = StringFromLines(
                "Imports D = System.IDisposable",
                "Class T",
                "    Dim x As D = Nothing",
                "End Class")
            Test(text,
                [Interface]("D"),
                [Interface]("IDisposable"),
                [Interface]("D"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub TestConstructorNew1()
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
            Test(text,
                 Keyword("New"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub TestConstructorNew2()
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
            Test(text,
                 [Class]("B"),
                 Keyword("New"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub TestConstructorNew3()
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
            Test(text,
                 Keyword("New"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub TestConstructorNew4()
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
            Test(text,
                 Keyword("New"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub TestAlias()
            Dim text = StringFromLines(
                "Imports E = System.Exception",
                "Class C",
                "    Inherits E",
                "End Class")
            Test(text,
                [Class]("E"),
                [Class]("Exception"),
                [Class]("E"))
        End Sub

        <WorkItem(542685)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub OptimisticallyColorFromInDeclaration()
            TestInExpression("From ", Keyword("From"))
        End Sub

        <WorkItem(542685)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub OptimisticallyColorFromInAssignment()
            TestInMethod(<text><![CDATA[
                            Dim q = 3
                            q = From 
                            ]]></text>.NormalizedValue, Keyword("From"))
        End Sub

        <WorkItem(542685)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub DontColorThingsOtherThanFromInDeclaration()
            TestInExpression("Fro ")
        End Sub

        <WorkItem(542685)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub DontColorThingsOtherThanFromInAssignment()
            TestInMethod(<text><![CDATA[
                            Dim q = 3
                            q = Fro 
                            ]]></text>.Value)
        End Sub

        <WorkItem(542685)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub DontColorFromWhenBoundInDeclaration()
            TestInMethod(<text><![CDATA[
                            Dim From = 3
                            Dim q = From
                            ]]></text>.Value)
        End Sub

        <WorkItem(542685)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub DontColorFromWhenBoundInAssignment()
            TestInMethod(<text><![CDATA[
                            Dim From = 3
                            Dim q = 3
                            q = From
                            ]]></text>.Value)
        End Sub

        <WpfFact, WorkItem(10507, "DevDiv_Projects/Roslyn"), Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub TestArraysInGetType()
            TestInMethod("GetType(System.Exception()",
                         [Class]("Exception"))
            TestInMethod("GetType(System.Exception(,)",
                         [Class]("Exception"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub NewOfInterface()
            TestInMethod("Dim a = New System.IDisposable()",
                         [Interface]("IDisposable"))
        End Sub

        <WorkItem(543404)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub NewOfClassWithNoPublicConstructors()
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

            Test(text,
                [Class]("C1"))
        End Sub

        <WorkItem(578145)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub AsyncKeyword1()
            Dim text =
<code>
Class C
    Sub M()
        Dim x = Async
    End Sub
End Class
</code>.NormalizedValue()

            Test(text,
                Keyword("Async"))
        End Sub

        <WorkItem(578145)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub AsyncKeyword2()
            Dim text =
<code>
Class C
    Sub M()
        Dim x = Async S
    End Sub
End Class
</code>.NormalizedValue()

            Test(text,
                Keyword("Async"))
        End Sub

        <WorkItem(578145)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub AsyncKeyword3()
            Dim text =
<code>
Class C
    Sub M()
        Dim x = Async Su
    End Sub
End Class
</code>.NormalizedValue()

            Test(text,
                Keyword("Async"))
        End Sub

        <WorkItem(578145)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub AsyncKeyword4()
            Dim text =
<code>
Class C
    Async
End Class
</code>.NormalizedValue()

            Test(text,
                Keyword("Async"))
        End Sub

        <WorkItem(578145)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub AsyncKeyword5()
            Dim text =
<code>
Class C
    Private Async
End Class
</code>.NormalizedValue()

            Test(text,
                Keyword("Async"))
        End Sub

        <WorkItem(578145)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub AsyncKeyword6()
            Dim text =
<code>
Class C
    Private Async As
End Class
</code>.NormalizedValue()

            Test(text)
        End Sub

        <WorkItem(578145)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub AsyncKeyword7()
            Dim text =
<code>
Class C
    Private Async =
End Class
</code>.NormalizedValue()

            Test(text)
        End Sub

        <WorkItem(578145)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub IteratorKeyword1()
            Dim text =
<code>
Class C
    Sub M()
        Dim x = Iterator
    End Sub
End Class
</code>.NormalizedValue()

            Test(text,
                Keyword("Iterator"))
        End Sub

        <WorkItem(578145)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub IteratorKeyword2()
            Dim text =
<code>
Class C
    Sub M()
        Dim x = Iterator F
    End Sub
End Class
</code>.NormalizedValue()

            Test(text,
                Keyword("Iterator"))
        End Sub

        <WorkItem(578145)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub IteratorKeyword3()
            Dim text =
<code>
Class C
    Sub M()
        Dim x = Iterator Functio
    End Sub
End Class
</code>.NormalizedValue()

            Test(text,
                Keyword("Iterator"))
        End Sub

        <WorkItem(578145)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub IteratorKeyword4()
            Dim text =
<code>
Class C
    Iterator
End Class
</code>.NormalizedValue()

            Test(text,
                Keyword("Iterator"))
        End Sub

        <WorkItem(578145)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub IteratorKeyword5()
            Dim text =
<code>
Class C
    Private Iterator
End Class
</code>.NormalizedValue()

            Test(text,
                Keyword("Iterator"))
        End Sub

        <WorkItem(578145)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub IteratorKeyword6()
            Dim text =
<code>
Class C
    Private Iterator As
End Class
</code>.NormalizedValue()

            Test(text)
        End Sub

        <WorkItem(578145)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub IteratorKeyword7()
            Dim text =
<code>
Class C
    Private Iterator =
End Class
</code>.NormalizedValue()

            Test(text)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub MyNamespace()
            Dim text =
<code>
Class C
    Sub M()
        Dim m = My.Foo
    End Sub
End Class
</code>.NormalizedValue()

            Test(text,
                 Keyword("My"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub TestAwaitInNonAsyncFunction1()
            Dim text =
<code>
dim m = Await
</code>.NormalizedValue()

            TestInMethod(text,
                 Keyword("Await"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Sub TestAwaitInNonAsyncFunction2()
            Dim text =
<code>
sub await()
end sub

sub test()
    dim m = Await
end sub
</code>.NormalizedValue()

            TestInClass(text)
        End Sub

    End Class
End Namespace
