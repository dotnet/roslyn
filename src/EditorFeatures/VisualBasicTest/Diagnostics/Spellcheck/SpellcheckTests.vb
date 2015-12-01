' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.Spellcheck
Imports Microsoft.CodeAnalysis.VisualBasic.Diagnostics

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.Spellcheck
    Public Class SpellcheckTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(Nothing, New SpellCheckCodeFixProvider())
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Async Function TestNoSpellcheckForIfOnly2Characters() As Task
            Dim text = <File>Class Foo
    Sub Bar()
        Dim a = new [|Fo|]
    End Sub
End Class</File>
            Await TestMissingAsync(text)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Async Function TestAfterNewExpression() As Task
            Dim text = <File>Class Foo
    Sub Bar()
        Dim a = new [|Fooa|].ToString()
    End Sub
End Class</File>
            Await TestExactActionSetOfferedAsync(text.NormalizedValue, {String.Format(FeaturesResources.ChangeTo, "Fooa", "Foo")})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Async Function TestInAsClause() As Task
            Dim text = <File>Class Foo
    Sub Bar()
        Dim a as [|Foa|]
    End Sub
End Class</File>
            Await TestExactActionSetOfferedAsync(text.NormalizedValue,
                {String.Format(FeaturesResources.ChangeTo, "Foa", "Foo")})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Async Function TestInSimpleAsClause() As Task
            Dim text = <File>Class Foo
    Sub Bar()
        Dim a as [|Foa|]
    End Sub
End Class</File>
            Await TestExactActionSetOfferedAsync(text.NormalizedValue,
                {String.Format(FeaturesResources.ChangeTo, "Foa", "Foo")})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Async Function TestInFunc() As Task
            Dim text = <File>Class Foo
    Sub Bar(a as Func(Of [|Foa|]))
    End Sub
End Class</File>
            Await TestExactActionSetOfferedAsync(text.NormalizedValue,
                {String.Format(FeaturesResources.ChangeTo, "Foa", "Foo")})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Async Function TestCorrectIdentifier() As Task
            Dim text = <File>Module Program
    Sub Main(args As String())
        Dim zzz = 2
        Dim y = 2 + [|zza|]
    End Sub
End Module</File>
            Await TestExactActionSetOfferedAsync(text.NormalizedValue, {String.Format(FeaturesResources.ChangeTo, "zza", "zzz")})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        <WorkItem(1065708)>
        Public Async Function TestInTypeOfIsExpression() As Task
            Dim text = <File>Imports System
Public Class Class1
    Sub F()
        If TypeOf x Is [|Boolea|] Then
        End If
    End Sub
End Class</File>
            Await TestExactActionSetOfferedAsync(text.NormalizedValue, {String.Format(FeaturesResources.ChangeTo, "Boolea", "Boolean")})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        <WorkItem(1065708)>
        Public Async Function TestInTypeOfIsNotExpression() As Task
            Dim text = <File>Imports System
Public Class Class1
    Sub F()
        If TypeOf x IsNot [|Boolea|] Then
        End If
    End Sub
End Class</File>
            Await TestExactActionSetOfferedAsync(text.NormalizedValue, {String.Format(FeaturesResources.ChangeTo, "Boolea", "Boolean")})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Async Function TestInvokeCorrectIdentifier() As Task
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

            Await TestAsync(text, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Async Function TestAfterDot() As Task
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

            Await TestAsync(text, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Async Function TestNotInaccessibleProperty() As Task
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

            Await TestMissingAsync(text)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Async Function TestGenericName1() As Task
            Dim text = <File>Class Foo(Of T)
    Dim x As [|Foo2(Of T)|]
End Class</File>

            Dim expected = <File>Class Foo(Of T)
    Dim x As [|Foo(Of T)|]
End Class</File>

            Await TestAsync(text, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Async Function TestGenericName2() As Task
            Dim text = <File>Class Foo(Of T)
    Dim x As [|Foo2|]
End Class</File>

            Dim expected = <File>Class Foo(Of T)
    Dim x As [|Foo|]
End Class</File>

            Await TestAsync(text, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Async Function TestQualifiedName1() As Task
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

            Await TestAsync(text, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Async Function TestQualifiedName2() As Task
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

            Await TestAsync(text, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Async Function TestMiddleOfDottedExpression() As Task
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

            Await TestAsync(text, expected)
        End Function

        <WorkItem(547161)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Async Function TestNotForOverloadResolutionFailure() As Task
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

            Await TestMissingAsync(text)
        End Function

        <WorkItem(547169)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Async Function TestHandlePredefinedTypeKeywordCorrectly() As Task
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

            Await TestActionCountAsync(text.ConvertTestSourceTag(), 3)
            Await TestAsync(text, expected, index:=0)
        End Function

        <WorkItem(547166)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Async Function TestKeepEscapedIdentifiersEscaped() As Task
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

            Await TestAsync(text, expected)
        End Function

        <WorkItem(547166)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Async Function TestNoDuplicateCorrections() As Task
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

            Await TestActionCountAsync(text.ConvertTestSourceTag(), 1)
            Await TestAsync(text, expected)
        End Function

        <ConditionalFact(GetType(x86))>
        <Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        <WorkItem(5391, "https://github.com/dotnet/roslyn/issues/5391")>
        Public Async Function TestSuggestEscapedPredefinedTypes() As Task
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

            Await TestActionCountAsync(text.ConvertTestSourceTag(), 3)
            Await TestAsync(text, expected0, index:=0)
            Await TestAsync(text, expected1, index:=1)
        End Function

        <WorkItem(775448)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
        Public Async Function TestShouldTriggerOnBC32045() As Task
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

            Await TestActionCountAsync(text.ConvertTestSourceTag(), 1)
            Await TestAsync(text, expected, index:=0)
        End Function

        <WorkItem(908322)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestTestObjectConstruction() As Task
            Await TestAsync(
NewLines("Class AwesomeClass \n Sub M() \n Dim foo = New [|AwesomeClas()|] \n End Sub \n End Class"),
NewLines("Class AwesomeClass \n Sub M() \n Dim foo = New AwesomeClass() \n End Sub \n End Class"),
index:=0)
        End Function

        <WorkItem(6338, "https://github.com/dotnet/roslyn/issues/6338")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestTestMissingName() As Task
            Await TestMissingAsync(
NewLines("<Assembly: Microsoft.CodeAnalysis.[||]>"))
        End Function

        Public Class AddImportTestsWithAddImportDiagnosticProvider
            Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

            Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
                Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(
                    New VisualBasicUnboundIdentifiersDiagnosticAnalyzer(),
                    New SpellCheckCodeFixProvider())
            End Function

            <WorkItem(829970)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
            Public Async Function TestIncompleteStatement() As Task
                Await TestAsync(
NewLines("Class AwesomeClass \n Inherits System.Attribute \n End Class \n Module Program \n <[|AwesomeClas|]> \n End Module"),
NewLines("Class AwesomeClass \n Inherits System.Attribute \n End Class \n Module Program \n <AwesomeClass> \n End Module"),
index:=0)
            End Function
        End Class
    End Class
End Namespace
