' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.Spellcheck
Imports Microsoft.CodeAnalysis.VisualBasic.Diagnostics

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.Spellcheck
    Public Class SpellcheckTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(Nothing, New SpellcheckCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Sub NoSpellcheckForIfOnly2Characters()
            Dim text = <File>Class Foo
    Sub Bar()
        Dim a = new [|Fo|]
    End Sub
End Class</File>
            TestMissing(text)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Sub AfterNewExpression()
            Dim text = <File>Class Foo
    Sub Bar()
        Dim a = new [|Fooa|].ToString()
    End Sub
End Class</File>
            TestExactActionSetOffered(text.NormalizedValue, {String.Format(FeaturesResources.ChangeTo, "Fooa", "Foo")})
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Sub InAsClause()
            Dim text = <File>Class Foo
    Sub Bar()
        Dim a as [|Foa|]
    End Sub
End Class</File>
            TestExactActionSetOffered(text.NormalizedValue,
                {String.Format(FeaturesResources.ChangeTo, "Foa", "Foo")})
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Sub InSimpleAsClause()
            Dim text = <File>Class Foo
    Sub Bar()
        Dim a as [|Foa|]
    End Sub
End Class</File>
            TestExactActionSetOffered(text.NormalizedValue,
                {String.Format(FeaturesResources.ChangeTo, "Foa", "Foo")})
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Sub InFunc()
            Dim text = <File>Class Foo
    Sub Bar(a as Func(Of [|Foa|]))
    End Sub
End Class</File>
            TestExactActionSetOffered(text.NormalizedValue,
                {String.Format(FeaturesResources.ChangeTo, "Foa", "Foo")})
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Sub CorrectIdentifier()
            Dim text = <File>Module Program
    Sub Main(args As String())
        Dim zzz = 2
        Dim y = 2 + [|zza|]
    End Sub
End Module</File>
            TestExactActionSetOffered(text.NormalizedValue, {String.Format(FeaturesResources.ChangeTo, "zza", "zzz")})
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        <WorkItem(1065708)>
        Public Sub InTypeOfIsExpression()
            Dim text = <File>Imports System
Public Class Class1
    Sub F()
        If TypeOf x Is [|Boolea|] Then
        End If
    End Sub
End Class</File>
            TestExactActionSetOffered(text.NormalizedValue, {String.Format(FeaturesResources.ChangeTo, "Boolea", "Boolean")})
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        <WorkItem(1065708)>
        Public Sub InTypeOfIsNotExpression()
            Dim text = <File>Imports System
Public Class Class1
    Sub F()
        If TypeOf x IsNot [|Boolea|] Then
        End If
    End Sub
End Class</File>
            TestExactActionSetOffered(text.NormalizedValue, {String.Format(FeaturesResources.ChangeTo, "Boolea", "Boolean")})
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Sub InvokeCorrectIdentifier()
            Dim text = <File>Module Program
    Sub Main(args As String())
        Dim zzz = 2
        Dim y = 2 + [|zza|]
    End Sub
End Module</File>

            Dim expected = <File>Module Program
    Sub Main(args As String())
        Dim zzz = 2
        Dim y = 2 + zzz
    End Sub
End Module</File>

            Test(text, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Sub AfterDot()
            Dim text = <File>Module Program
    Sub Main(args As String())
        Program.[|Mair|]
    End Sub
End Module</File>

            Dim expected = <File>Module Program
    Sub Main(args As String())
        Program.Main
    End Sub
End Module</File>

            Test(text, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Sub NotInaccessibleProperty()
            Dim text = <File>Module Program
    Sub Main(args As String())
        Dim z = New c().[|membr|]
    End Sub
End Module

Class c
    Protected Property member As Integer
        Get
            Return 0
        End Get
    End Property
End Class</File>

            TestMissing(text)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Sub GenericName1()
            Dim text = <File>Class Foo(Of T)
    Dim x As [|Foo2(Of T)|]
End Class</File>

            Dim expected = <File>Class Foo(Of T)
    Dim x As [|Foo(Of T)|]
End Class</File>

            Test(text, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Sub GenericName2()
            Dim text = <File>Class Foo(Of T)
    Dim x As [|Foo2|]
End Class</File>

            Dim expected = <File>Class Foo(Of T)
    Dim x As [|Foo|]
End Class</File>

            Test(text, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Sub QualifiedName1()
            Dim text = <File>Module Program
    Dim x As New [|Foo2.Bar|]
End Module

Class Foo
    Class Bar

    End Class
End Class</File>

            Dim expected = <File>Module Program
    Dim x As New Foo.Bar
End Module

Class Foo
    Class Bar

    End Class
End Class</File>

            Test(text, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Sub QualifiedName2()
            Dim text = <File>Module Program
    Dim x As New [|Foo.Ba2|]
End Module

Class Foo
    Class Bar

    End Class
End Class</File>

            Dim expected = <File>Module Program
    Dim x As New Foo.Bar
End Module

Class Foo
    Class Bar

    End Class
End Class</File>

            Test(text, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Sub MiddleOfDottedExpression()
            Dim text = <File>Module Program
    Sub Main(args As String())
        Dim z = New c().[|membr|].ToString()
    End Sub
End Module

Class c
    Public Property member As Integer
        Get
            Return 0
        End Get
    End Property
End Class</File>

            Dim expected = <File>Module Program
    Sub Main(args As String())
        Dim z = New c().member.ToString()
    End Sub
End Module

Class c
    Public Property member As Integer
        Get
            Return 0
        End Get
    End Property
End Class</File>

            Test(text, expected)
        End Sub

        <WorkItem(547161)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Sub NotForOverloadResolutionFailure()
            Dim text = <File>Module Program
    Sub Main(args As String())

    End Sub
    Sub Foo()
        [|Method|]()
    End Sub

    Function Method(argument As Integer) As Integer
        Return 0
    End Function
End Module</File>

            TestMissing(text)
        End Sub

        <WorkItem(547169)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Sub HandlePredefinedTypeKeywordCorrectly()
            Dim text = <File>
Imports System
Imports System.Collections.Generic
Imports System.Linq
                           
Module Program
    Sub Main(args As String())
        Dim x as [|intege|]
    End Sub
End Module</File>

            Dim expected = <File>
Imports System
Imports System.Collections.Generic
Imports System.Linq
                           
Module Program
    Sub Main(args As String())
        Dim x as Integer
    End Sub
End Module</File>

            TestActionCount(text.ConvertTestSourceTag(), 3)
            Test(text, expected, index:=0)
        End Sub

        <WorkItem(547166)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Sub KeepEscapedIdentifiersEscaped()
            Dim text = <File>
Module Program
    Sub Main(args As String())
        Dim q = From x In args
        [|[Taka]|]()
    End Sub

    Sub Take()
    End Sub
End Module</File>

            Dim expected = <File>
Module Program
    Sub Main(args As String())
        Dim q = From x In args
        [Take]()
    End Sub

    Sub Take()
    End Sub
End Module</File>

            Test(text, expected)
        End Sub

        <WorkItem(547166)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Sub NoDuplicateCorrections()
            Dim text = <File>
Module Program
    Sub Main(args As String())
        Dim q = From x In args
        [|[Taka]|]()
    End Sub

    Sub Take()
    End Sub
End Module</File>

            Dim expected = <File>
Module Program
    Sub Main(args As String())
        Dim q = From x In args
        [Take]()
    End Sub

    Sub Take()
    End Sub
End Module</File>

            TestActionCount(text.ConvertTestSourceTag(), 1)
            Test(text, expected)
        End Sub

        <ConditionalFact(GetType(x86))>
        <Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        <WorkItem(5391, "https://github.com/dotnet/roslyn/issues/5391")>
        Public Sub SuggestEscapedPredefinedTypes()
            Dim text = <File>
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Class [Integer]
    End Class

    Sub Main(args As String())
        Dim x as [|intege|]
    End Sub
End Module</File>

            Dim expected0 = <File>
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Class [Integer]
    End Class

    Sub Main(args As String())
        Dim x as [Integer]
    End Sub
End Module</File>

            Dim expected1 = <File>
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Class [Integer]
    End Class

    Sub Main(args As String())
        Dim x as Integer
    End Sub
End Module</File>

            TestActionCount(text.ConvertTestSourceTag(), 3)
            Test(text, expected0, index:=0)
            Test(text, expected1, index:=1)
        End Sub

        <WorkItem(775448)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Sub ShouldTriggerOnBC32045()
            ' BC32045: 'A' has no type parameters and so cannot have type arguments.

            Dim text = <File>
' Import System.Collections to ensure we get BC32045
Imports System.Collections

Interface IOrderable(Of T)
End Interface

Class C
    Sub Main(args As String())
        Dim x as [|IEnumerable(Of Integer)|]
    End Sub
End Class</File>

            Dim expected = <File>
' Import System.Collections to ensure we get BC32045
Imports System.Collections

Interface IOrderable(Of T)
End Interface

Class C
    Sub Main(args As String())
        Dim x as IOrderable(Of Integer)
    End Sub
End Class</File>

            TestActionCount(text.ConvertTestSourceTag(), 1)
            Test(text, expected, index:=0)
        End Sub

        <WorkItem(908322)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestObjectConstruction()
            Test(
NewLines("Class AwesomeClass \n Sub M() \n Dim foo = New [|AwesomeClas()|] \n End Sub \n End Class"),
NewLines("Class AwesomeClass \n Sub M() \n Dim foo = New AwesomeClass() \n End Sub \n End Class"),
index:=0)
        End Sub

        <WorkItem(6338, "https://github.com/dotnet/roslyn/issues/6338")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestMissingName()
            TestMissing(
NewLines("<Assembly: Microsoft.CodeAnalysis.[||]>"))
        End Sub

        Public Class AddImportTestsWithAddImportDiagnosticProvider
            Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

            Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
                Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(
                    New VisualBasicUnboundIdentifiersDiagnosticAnalyzer(),
                    New SpellCheckCodeFixProvider())
            End Function

            <WorkItem(829970)>
            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
            Public Sub TestIncompleteStatement()
                Test(
NewLines("Class AwesomeClass \n Inherits System.Attribute \n End Class \n Module Program \n <[|AwesomeClas|]> \n End Module"),
NewLines("Class AwesomeClass \n Inherits System.Attribute \n End Class \n Module Program \n <AwesomeClass> \n End Module"),
index:=0)
            End Sub
        End Class
    End Class
End Namespace
