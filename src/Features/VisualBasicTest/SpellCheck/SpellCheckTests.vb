' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.SpellCheck
Imports Microsoft.CodeAnalysis.VisualBasic.Diagnostics

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.Spellcheck
    <Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
    Public Class SpellCheckTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest_NoEditor

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (Nothing, New VisualBasicSpellCheckCodeFixProvider())
        End Function

        Protected Overrides Function MassageActions(actions As ImmutableArray(Of CodeAction)) As ImmutableArray(Of CodeAction)
            Return FlattenActions(actions)
        End Function

        <Fact>
        Public Async Function TestNoSpellcheckForIfOnly2Characters() As Task
            Dim text = <File>Class Goo
    Sub Bar()
        Dim a = new [|Fo|]
    End Sub
End Class</File>
            Await TestMissingAsync(text)
        End Function

        <Fact>
        Public Async Function TestAfterNewExpression() As Task
            Dim text = <File>Class Goo
    Sub Bar()
        Dim a = new [|Gooa|].ToString()
    End Sub
End Class</File>
            Await TestExactActionSetOfferedAsync(text.NormalizedValue, {String.Format(FeaturesResources.Change_0_to_1, "Gooa", "Goo")})
        End Function

        <Fact>
        Public Async Function TestInAsClause() As Task
            Dim text = <File>Class Goo
    Sub Bar()
        Dim a as [|Goa|]
    End Sub
End Class</File>
            Await TestExactActionSetOfferedAsync(text.NormalizedValue,
                {String.Format(FeaturesResources.Change_0_to_1, "Goa", "Goo")})
        End Function

        <Fact>
        Public Async Function TestInSimpleAsClause() As Task
            Dim text = <File>Class Goo
    Sub Bar()
        Dim a as [|Goa|]
    End Sub
End Class</File>
            Await TestExactActionSetOfferedAsync(text.NormalizedValue,
                {String.Format(FeaturesResources.Change_0_to_1, "Goa", "Goo")})
        End Function

        <Fact>
        Public Async Function TestInFunc() As Task
            Dim text = <File>Class Goo
    Sub Bar(a as Func(Of [|Goa|]))
    End Sub
End Class</File>
            Await TestExactActionSetOfferedAsync(text.NormalizedValue,
                {String.Format(FeaturesResources.Change_0_to_1, "Goa", "Goo")})
        End Function

        <Fact>
        Public Async Function TestCorrectIdentifier() As Task
            Dim text = <File>Module Program
    Sub Main(args As String())
        Dim zzz = 2
        Dim y = 2 + [|zza|]
    End Sub
End Module</File>
            Await TestExactActionSetOfferedAsync(text.NormalizedValue, {String.Format(FeaturesResources.Change_0_to_1, "zza", "zzz")})
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1065708")>
        Public Async Function TestInTypeOfIsExpression() As Task
            Dim text = <File>Imports System
Public Class Class1
    Sub F()
        If TypeOf x Is [|Boolea|] Then
        End If
    End Sub
End Class</File>
            Await TestExactActionSetOfferedAsync(text.NormalizedValue, {String.Format(FeaturesResources.Change_0_to_1, "Boolea", "Boolean")})
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1065708")>
        Public Async Function TestInTypeOfIsNotExpression() As Task
            Dim text = <File>Imports System
Public Class Class1
    Sub F()
        If TypeOf x IsNot [|Boolea|] Then
        End If
    End Sub
End Class</File>
            Await TestExactActionSetOfferedAsync(text.NormalizedValue, {String.Format(FeaturesResources.Change_0_to_1, "Boolea", "Boolean")})
        End Function

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
        Public Async Function TestGenericName1() As Task
            Dim text = <File>Class Goo(Of T)
    Dim x As [|Goo2(Of T)|]
End Class</File>

            Dim expected = <File>Class Goo(Of T)
    Dim x As [|Goo(Of T)|]
End Class</File>

            Await TestAsync(text, expected)
        End Function

        <Fact>
        Public Async Function TestGenericName2() As Task
            Dim text = <File>Class Goo(Of T)
    Dim x As [|Goo2|]
End Class</File>

            Dim expected = <File>Class Goo(Of T)
    Dim x As [|Goo|]
End Class</File>

            Await TestAsync(text, expected)
        End Function

        <Fact>
        Public Async Function TestQualifiedName1() As Task
            Dim text = <File>Module Program
    Dim x As New [|Goo2.Bar|]
End Module

Class Goo
    Class Bar

    End Class
End Class</File>

            Dim expected = <File>Module Program
    Dim x As New Goo.Bar
End Module

Class Goo
    Class Bar

    End Class
End Class</File>

            Await TestAsync(text, expected)
        End Function

        <Fact>
        Public Async Function TestQualifiedName2() As Task
            Dim text = <File>Module Program
    Dim x As New [|Goo.Ba2|]
End Module

Class Goo
    Class Bar

    End Class
End Class</File>

            Dim expected = <File>Module Program
    Dim x As New Goo.Bar
End Module

Class Goo
    Class Bar

    End Class
End Class</File>

            Await TestAsync(text, expected)
        End Function

        <Fact>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547161")>
        Public Async Function TestNotForOverloadResolutionFailure() As Task
            Dim text = <File>Module Program
    Sub Main(args As String())

    End Sub
    Sub Goo()
        [|Method|]()
    End Sub

    Function Method(argument As Integer) As Integer
        Return 0
    End Function
End Module</File>

            Await TestMissingAsync(text)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547169")>
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

            Await TestActionCountAsync(text.ConvertTestSourceTag(), 2)
            Await TestAsync(text, expected, index:=0)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547166")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547166")>
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

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/5391")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775448")>
        Public Async Function TestShouldTriggerOnBC32045() As Task
            ' BC32045: 'A' has no type parameters and so cannot have type arguments.

            Dim text = <File>
' Import System.Collections to ensure we get BC32045
Imports System.Collections

Interface Enumerable(Of T)
End Interface

Class C
    Sub Main(args As String())
        Dim x as [|IEnumerable(Of Integer)|]
    End Sub
End Class</File>

            Dim expected = <File>
' Import System.Collections to ensure we get BC32045
Imports System.Collections

Interface Enumerable(Of T)
End Interface

Class C
    Sub Main(args As String())
        Dim x as Enumerable(Of Integer)
    End Sub
End Class</File>

            Await TestActionCountAsync(text.ConvertTestSourceTag(), 1)
            Await TestAsync(text, expected, index:=0)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/908322")>
        Public Async Function TestObjectConstruction() As Task
            Await TestInRegularAndScriptAsync(
"Class AwesomeClass
    Sub M()
        Dim goo = New [|AwesomeClas()|]
    End Sub
End Class",
"Class AwesomeClass
    Sub M()
        Dim goo = New AwesomeClass()
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/6338")>
        Public Async Function TestTestMissingName() As Task
            Await TestMissingInRegularAndScriptAsync(
"<Assembly: Microsoft.CodeAnalysis.[||]>")
        End Function

        <Fact>
        Public Async Function TestTrivia1() As Task
            Await TestInRegularAndScriptAsync(
"Class AwesomeClass
    Sub M()
        Dim goo = New [|AwesomeClas|] ' trailing trivia
    End Sub
End Class",
"Class AwesomeClass
    Sub M()
        Dim goo = New AwesomeClass ' trailing trivia
    End Sub
End Class")
        End Function

        Public Class AddImportTestsWithAddImportDiagnosticProvider
            Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest_NoEditor

            Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
                Return (New VisualBasicUnboundIdentifiersDiagnosticAnalyzer(),
                        New VisualBasicSpellCheckCodeFixProvider())
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSpellcheck)>
            <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/829970")>
            Public Async Function TestIncompleteStatement() As Task
                Await TestInRegularAndScriptAsync(
"Class AwesomeClass
    Inherits System.Attribute
End Class
Module Program
    <[|AwesomeClas|]>
End Module",
"Class AwesomeClass
    Inherits System.Attribute
End Class
Module Program
    <AwesomeClass>
End Module")
            End Function
        End Class
    End Class
End Namespace
