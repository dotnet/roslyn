' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Remote.Testing
Imports Microsoft.CodeAnalysis.VisualBasic.AddImport

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeActions.AddImport

    <Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
    Public Class AddImportTests
        Inherits AbstractAddImportTests

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (Nothing, New VisualBasicAddImportCodeFixProvider())
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestSimpleImportFromSameFile(testHost As TestHost) As Task
            Await TestAsync(
"Class Class1
    Dim v As [|SomeClass1|]
End Class
Namespace SomeNamespace
    Public Class SomeClass1
    End Class
End Namespace",
                "Imports SomeNamespace

Class Class1
    Dim v As SomeClass1
End Class
Namespace SomeNamespace
    Public Class SomeClass1
    End Class
End Namespace", testHost)
        End Function

        <Theory(Skip:="https://github.com/dotnet/roslyn/issues/41484")>
        <CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/11241")>
        Public Async Function TestAddImportWithCaseChange(testHost As TestHost) As Task
            Await TestAsync(
"Namespace N1
    Public Class TextBox
    End Class
End Namespace

Class Class1
    Inherits [|Textbox|]

End Class",
                "Imports N1

Namespace N1
    Public Class TextBox
    End Class
End Namespace

Class Class1
    Inherits TextBox

End Class", testHost, priority:=CodeActionPriority.Medium)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestSimpleImportFromReference(testHost As TestHost) As Task
            Await TestAsync(
"Class Class1
    Dim v As [|Thread|]
End Class",
                "Imports System.Threading

Class Class1
    Dim v As Thread
End Class", testHost)
        End Function

        <ConditionalFact>
        Public Async Function TestSmartTagDisplay() As Task
            Await TestSmartTagTextAsync(
"Class Class1
    Dim v As [|Thread|]
End Class",
"Imports System.Threading")
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestGenericClassDefinitionAsClause(testHost As TestHost) As Task
            Await TestAsync(
"Namespace SomeNamespace
    Class Base
    End Class
End Namespace
Class SomeClass(Of x As [|Base|])
End Class",
                "Imports SomeNamespace

Namespace SomeNamespace
    Class Base
    End Class
End Namespace
Class SomeClass(Of x As Base)
End Class", testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestGenericClassInstantiationOfClause(testHost As TestHost) As Task
            Await TestAsync(
"Namespace SomeNamespace
    Class SomeClass
    End Class
End Namespace
Class GenericClass(Of T)
End Class
Class Goo
    Sub Method1()
        Dim q As GenericClass(Of [|SomeClass|])
    End Sub
End Class",
                "Imports SomeNamespace

Namespace SomeNamespace
    Class SomeClass
    End Class
End Namespace
Class GenericClass(Of T)
End Class
Class Goo
    Sub Method1()
        Dim q As GenericClass(Of SomeClass)
    End Sub
End Class", testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestGenericMethodDefinitionAsClause(testHost As TestHost) As Task
            Await TestAsync(
"Namespace SomeNamespace
    Class SomeClass
    End Class
End Namespace
Class Goo
    Sub Method1(Of T As [|SomeClass|])
    End Sub
End Class",
                "Imports SomeNamespace

Namespace SomeNamespace
    Class SomeClass
    End Class
End Namespace
Class Goo
    Sub Method1(Of T As SomeClass)
    End Sub
End Class", testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestGenericMethodInvocationOfClause(testHost As TestHost) As Task
            Await TestAsync(
"Namespace SomeNamespace
    Class SomeClass
    End Class
End Namespace
Class Goo
    Sub Method1(Of T)
    End Sub
    Sub Method2()
        Method1(Of [|SomeClass|])
    End Sub
End Class",
                "Imports SomeNamespace

Namespace SomeNamespace
    Class SomeClass
    End Class
End Namespace
Class Goo
    Sub Method1(Of T)
    End Sub
    Sub Method2()
        Method1(Of SomeClass)
    End Sub
End Class", testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestAttributeApplication(testHost As TestHost) As Task
            Await TestAsync(
"<[|Something|]()>
Class Goo
End Class
Namespace SomeNamespace
    Class SomethingAttribute
        Inherits System.Attribute
    End Class
End Namespace",
                "Imports SomeNamespace

<Something()>
Class Goo
End Class
Namespace SomeNamespace
    Class SomethingAttribute
        Inherits System.Attribute
    End Class
End Namespace", testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestMultipleAttributeApplicationBelow(testHost As TestHost) As Task
            Await TestAsync(
"<Existing()>
<[|Something|]()>
Class Goo
End Class
Class ExistingAttribute
    Inherits System.Attribute
End Class
Namespace SomeNamespace
    Class SomethingAttribute
        Inherits System.Attribute
    End Class
End Namespace",
                "Imports SomeNamespace

<Existing()>
<Something()>
Class Goo
End Class
Class ExistingAttribute
    Inherits System.Attribute
End Class
Namespace SomeNamespace
    Class SomethingAttribute
        Inherits System.Attribute
    End Class
End Namespace", testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestMultipleAttributeApplicationAbove(testHost As TestHost) As Task
            Await TestAsync(
"<[|Something|]()>
<Existing()>
Class Goo
End Class
Class ExistingAttribute
    Inherits System.Attribute
End Class
Namespace SomeNamespace
    Class SomethingAttribute
        Inherits System.Attribute
    End Class
End Namespace",
                "Imports SomeNamespace

<Something()>
<Existing()>
Class Goo
End Class
Class ExistingAttribute
    Inherits System.Attribute
End Class
Namespace SomeNamespace
    Class SomethingAttribute
        Inherits System.Attribute
    End Class
End Namespace", testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestImportsIsEscapedWhenNamespaceMatchesKeyword(testHost As TestHost) As Task
            Await TestAsync(
"Class SomeClass
    Dim x As [|Something|]
End Class
Namespace [Namespace]
    Class Something
    End Class
End Namespace",
                "Imports [Namespace]

Class SomeClass
    Dim x As Something
End Class
Namespace [Namespace]
    Class Something
    End Class
End Namespace", testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestImportsIsNOTEscapedWhenNamespaceMatchesKeywordButIsNested(testHost As TestHost) As Task
            Await TestAsync(
"Class SomeClass
    Dim x As [|Something|]
End Class
Namespace Outer
    Namespace [Namespace]
        Class Something
        End Class
    End Namespace
End Namespace",
                "Imports Outer.Namespace

Class SomeClass
    Dim x As Something
End Class
Namespace Outer
    Namespace [Namespace]
        Class Something
        End Class
    End Namespace
End Namespace", testHost)
        End Function

        <ConditionalFact>
        Public Async Function TestAddImportsNotSuggestedForImportsStatement() As Task
            Await TestMissingInRegularAndScriptAsync(
"Imports [|InnerNamespace|]
Namespace SomeNamespace
    Namespace InnerNamespace
        Class SomeClass
        End Class
    End Namespace
End Namespace")
        End Function

        <ConditionalFact>
        Public Async Function TestAddImportsNotSuggestedForGenericTypeParametersOfClause() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class SomeClass
    Sub Goo(Of [|SomeClass|])(x As SomeClass)
    End Sub
End Class
Namespace SomeNamespace
    Class SomeClass
    End Class
End Namespace")
        End Function

        <ConditionalFact>
        Public Async Function TestAddImportsNotSuggestedForGenericTypeParametersAsClause() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class SomeClass
    Sub Goo(Of SomeClass)(x As [|SomeClass|])
    End Sub
End Class
Namespace SomeNamespace
    Class SomeClass
    End Class
End Namespace")
        End Function

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540543")>
        <CombinatorialData>
        Public Async Function TestCaseSensitivity1(testHost As TestHost) As Task
            Await TestAsync(
"Class Goo
    Dim x As [|someclass|]
End Class
Namespace SomeNamespace
    Class SomeClass
    End Class
End Namespace",
                "Imports SomeNamespace

Class Goo
    Dim x As SomeClass
End Class
Namespace SomeNamespace
    Class SomeClass
    End Class
End Namespace", testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestTypeFromMultipleNamespaces1(testHost As TestHost) As Task
            Await TestAsync(
"Class Goo
    Function F() As [|IDictionary|]
    End Function
End Class",
                "Imports System.Collections

Class Goo
    Function F() As IDictionary
    End Function
End Class", testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestTypeFromMultipleNamespaces2(testHost As TestHost) As Task
            Await TestAsync(
"Class Goo
    Function F() As [|IDictionary|]
    End Function
End Class",
                "Imports System.Collections.Generic

Class Goo
    Function F() As IDictionary
    End Function
End Class",
                testHost, index:=1)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestGenericWithNoArgs(testHost As TestHost) As Task
            Await TestAsync(
"Class Goo
    Function F() As [|List|]
    End Function
End Class",
                "Imports System.Collections.Generic

Class Goo
    Function F() As List
    End Function
End Class", testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestGenericWithCorrectArgs(testHost As TestHost) As Task
            Await TestAsync(
"Class Goo
    Function F() As [|List(Of Integer)|]
    End Function
End Class",
                "Imports System.Collections.Generic

Class Goo
    Function F() As List(Of Integer)
    End Function
End Class", testHost)
        End Function

        <ConditionalFact>
        Public Async Function TestGenericWithWrongArgs1() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class Goo
    Function F() As [|List(Of Integer, String, Boolean)|]
    End Function
End Class")
        End Function

        <ConditionalFact>
        Public Async Function TestGenericWithWrongArgs2() As Task
            Await TestMissingInRegularAndScriptAsync(
"Class Goo
    Function F() As [|List(Of Integer, String)|]
    End Function
End Class")
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestGenericInLocalDeclaration(testHost As TestHost) As Task
            Await TestAsync(
"Class Goo
    Sub Test()
        Dim x As New [|List(Of Integer)|]
    End Sub
End Class",
                "Imports System.Collections.Generic

Class Goo
    Sub Test()
        Dim x As New List(Of Integer)
    End Sub
End Class", testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestGenericItemType(testHost As TestHost) As Task
            Await TestAsync(
"Class Goo
    Sub Test()
        Dim x As New List(Of [|Int32|])
    End Sub
End Class",
                "Imports System

Class Goo
    Sub Test()
        Dim x As New List(Of Int32)
    End Sub
End Class", testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestGenerateWithExistingUsings(testHost As TestHost) As Task
            Await TestAsync(
"Imports System
Class Goo
    Sub Test()
        Dim x As New [|List(Of Integer)|]
    End Sub
End Class",
                "Imports System
Imports System.Collections.Generic

Class Goo
    Sub Test()
        Dim x As New List(Of Integer)
    End Sub
End Class", testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestGenerateInNamespace(testHost As TestHost) As Task
            Await TestAsync(
"Imports System
Namespace NS
    Class Goo
        Sub Test()
            Dim x As New [|List(Of Integer)|]
        End Sub
    End Class
End Namespace",
                "Imports System
Imports System.Collections.Generic

Namespace NS
    Class Goo
        Sub Test()
            Dim x As New List(Of Integer)
        End Sub
    End Class
End Namespace", testHost)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540519")>
        Public Async Function TestCodeIssueCountInExistingUsing() As Task
            Await TestActionCountAsync(
"Imports System.Collections.Generic
Namespace NS
    Class Goo
        Function Test() As [|IDictionary|]
        End Function
    End Class
End Namespace",
count:=1)
        End Function

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540519")>
        <CombinatorialData>
        Public Async Function TestFixInExistingUsing(testHost As TestHost) As Task
            Await TestAsync(
"Imports System.Collections.Generic
Namespace NS
    Class Goo
        Function Test() As [|IDictionary|]
        End Function
    End Class
End Namespace",
                "Imports System.Collections
Imports System.Collections.Generic
Namespace NS
    Class Goo
        Function Test() As IDictionary
        End Function
    End Class
End Namespace", testHost)
        End Function

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541731")>
        <CombinatorialData>
        Public Async Function TestGenericExtensionMethod(testHost As TestHost) As Task
            Await TestAsync(
"Imports System.Collections.Generic
Class Test
    Private Sub Method(args As IList(Of Integer))
        args.[|Where|]()
    End Sub
End Class",
                "Imports System.Collections.Generic
Imports System.Linq

Class Test
    Private Sub Method(args As IList(Of Integer))
        args.Where()
    End Sub
End Class", testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestParameterType(testHost As TestHost) As Task
            Await TestAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String(), f As [|FileMode|])
    End Sub
End Module",
                "Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Module Program
    Sub Main(args As String(), f As FileMode)
    End Sub
End Module", testHost)
        End Function

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540519")>
        <CombinatorialData>
        Public Async Function TestAddWithExistingConflictWithDifferentArity(testHost As TestHost) As Task
            Await TestAsync(
"Imports System.Collections.Generic
Namespace NS
    Class Goo
        Function Test() As [|IDictionary|]
        End Function
    End Class
End Namespace",
                "Imports System.Collections
Imports System.Collections.Generic
Namespace NS
    Class Goo
        Function Test() As IDictionary
        End Function
    End Class
End Namespace", testHost)
        End Function

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540673")>
        <CombinatorialData>
        Public Async Function TestImportNamespace(testHost As TestHost) As Task
            Await TestAsync(
"Class GOo
    Sub bar()
        Dim q As [|innernamespace|].someClass
    End Sub
End Class
Namespace SomeNamespace
    Namespace InnerNamespace
        Class SomeClass
        End Class
    End Namespace
End Namespace",
                "Imports SomeNamespace

Class GOo
    Sub bar()
        Dim q As InnerNamespace.SomeClass
    End Sub
End Class
Namespace SomeNamespace
    Namespace InnerNamespace
        Class SomeClass
        End Class
    End Namespace
End Namespace", testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestCaseSensitivity2(testHost As TestHost) As Task
            Await TestAsync(
"Class GOo
    Sub bar()
        Dim q As [|innernamespace|].someClass
    End Sub
End Class
Namespace SomeNamespace
    Namespace InnerNamespace
        Class SomeClass
        End Class
    End Namespace
End Namespace",
                "Imports SomeNamespace

Class GOo
    Sub bar()
        Dim q As InnerNamespace.SomeClass
    End Sub
End Class
Namespace SomeNamespace
    Namespace InnerNamespace
        Class SomeClass
        End Class
    End Namespace
End Namespace", testHost)
        End Function

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540745")>
        <CombinatorialData>
        Public Async Function TestCaseSensitivity3(testHost As TestHost) As Task
            Await TestAsync(
"Module Program
    Sub Main(args As String())
        Dim x As [|goo|]
    End Sub
End Module
Namespace OUTER
    Namespace INNER
        Friend Class GOO
        End Class
    End Namespace
End Namespace",
                "Imports OUTER.INNER

Module Program
    Sub Main(args As String())
        Dim x As GOO
    End Sub
End Module
Namespace OUTER
    Namespace INNER
        Friend Class GOO
        End Class
    End Namespace
End Namespace", testHost)
        End Function

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541746")>
        <CombinatorialData>
        Public Async Function TestAddBlankLineAfterLastImports(testHost As TestHost) As Task
            Await TestAsync(
<Text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
    End Sub
End Module

&lt;[|SomeAttr|]&gt;
Class Goo
End Class
Namespace SomeNamespace
    Friend Class SomeAttrAttribute
        Inherits Attribute
    End Class
End Namespace</Text>.Value.Replace(vbLf, vbCrLf),
                <Text>Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports SomeNamespace

Module Program
    Sub Main(args As String())
    End Sub
End Module

&lt;SomeAttr&gt;
Class Goo
End Class
Namespace SomeNamespace
    Friend Class SomeAttrAttribute
        Inherits Attribute
    End Class
End Namespace</Text>.Value.Replace(vbLf, vbCrLf), testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestSimpleWhereClause(testHost As TestHost) As Task
            Await TestAsync(
"Class Program
    Public Sub Linq1()
        Dim numbers() As Integer = New Integer(9) {5, 4, 1, 3, 9, 8, 6, 7, 2, 0}
        Dim lowNums = [|From n In numbers _
                      Where n < 5 _
                      Select n|]
    End Sub
End Class",
                "Imports System.Linq

Class Program
    Public Sub Linq1()
        Dim numbers() As Integer = New Integer(9) {5, 4, 1, 3, 9, 8, 6, 7, 2, 0}
        Dim lowNums = From n In numbers _
                      Where n < 5 _
                      Select n
    End Sub
End Class", testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestAggregateClause(testHost As TestHost) As Task
            Await TestAsync(
"Imports System.Collections.Generic
Class Program
    Public Sub Linq1()
        Dim numbers() As Integer = New Integer(9) {5, 4, 1, 3, 9, 8, 6, 7, 2, 0}
        Dim greaterNums = [|Aggregate n In numbers
        Into greaterThan5 = All(n > 5)|]
    End Sub
End Class",
                "Imports System.Collections.Generic
Imports System.Linq

Class Program
    Public Sub Linq1()
        Dim numbers() As Integer = New Integer(9) {5, 4, 1, 3, 9, 8, 6, 7, 2, 0}
        Dim greaterNums = Aggregate n In numbers
        Into greaterThan5 = All(n > 5)
    End Sub
End Class", testHost)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543107")>
        Public Async Function TestNoCrashOnMissingLeftSide() As Task
            Await TestMissingInRegularAndScriptAsync(
"Imports System
Class C1
    Sub goo()
        Dim s = .[|first|]
    End Sub
End Class")
        End Function

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544335")>
        <CombinatorialData>
        Public Async Function TestOnCallWithoutArgumentList(testHost As TestHost) As Task
            Await TestAsync(
"Module Program
    Sub Main(args As String())
        [|File|]
    End Sub
End Module",
                "Imports System.IO

Module Program
    Sub Main(args As String())
        File
    End Sub
End Module", testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestAddToVisibleRegion(testHost As TestHost) As Task
            Await TestAsync(
"#ExternalSource (""Default.aspx"", 1) 
Imports System
#End ExternalSource
#ExternalSource (""Default.aspx"", 2) 
Class C
    Sub Goo()
        Dim x As New [|StreamReader|]
#End ExternalSource
    End Sub
End Class",
                "#ExternalSource (""Default.aspx"", 1)
Imports System
Imports System.IO
#End ExternalSource
#ExternalSource (""Default.aspx"", 2)
Class C
    Sub Goo()
        Dim x As New [|StreamReader|]
#End ExternalSource
    End Sub
End Class", testHost)
        End Function

        <ConditionalFact>
        Public Async Function TestDoNotAddIntoHiddenRegion() As Task
            Await TestMissingInRegularAndScriptAsync(
"Imports System
#ExternalSource (""Default.aspx"", 2) 
Class C
    Sub Goo()
        Dim x As New [|StreamReader|]
#End ExternalSource
    End Sub
End Class")
        End Function

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546369")>
        <CombinatorialData>
        Public Async Function TestFormattingAfterImports(testHost As TestHost) As Task
            Await TestAsync(
<Text>Imports B
Imports A
Module Program
    Sub Main()
        [|Debug|]
    End Sub
End Module
</Text>.Value.Replace(vbLf, vbCrLf),
                <Text>Imports B
Imports A
Imports System.Diagnostics

Module Program
    Sub Main()
        Debug
    End Sub
End Module
</Text>.Value.Replace(vbLf, vbCrLf), testHost)
        End Function

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775448")>
        <CombinatorialData>
        Public Async Function TestShouldTriggerOnBC32045(testHost As TestHost) As Task
            ' BC32045: 'A' has no type parameters and so cannot have type arguments.
            Await TestAsync(
<Text>Imports System.Collections

Module Program
    Sub Main(args As String())
        Dim x As [|IEnumerable(Of Integer)|]
    End Sub
End Module</Text>.Value.Replace(vbLf, vbCrLf),
                <Text>Imports System.Collections
Imports System.Collections.Generic

Module Program
    Sub Main(args As String())
        Dim x As IEnumerable(Of Integer)
    End Sub
End Module</Text>.Value.Replace(vbLf, vbCrLf), testHost)
        End Function

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/867425")>
        <CombinatorialData>
        Public Async Function TestUnknownIdentifierInModule(testHost As TestHost) As Task
            Await TestAsync(
"Module Goo
    Sub Bar(args As String())
        Dim a = From f In args
                Let ext = [|Path|]
    End Sub
End Module",
                "Imports System.IO

Module Goo
    Sub Bar(args As String())
        Dim a = From f In args
                Let ext = Path
    End Sub
End Module", testHost)
        End Function

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/872908")>
        <CombinatorialData>
        Public Async Function TestConflictedGenericName(testHost As TestHost) As Task
            Await TestAsync(
"Module Goo
    Sub Bar(args As String())
        Dim a = From f In args
                Let ext = [|Path|]
    End Sub
End Module",
                "Imports System.IO

Module Goo
    Sub Bar(args As String())
        Dim a = From f In args
                Let ext = Path
    End Sub
End Module", testHost)
        End Function

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/838253")>
        <CombinatorialData>
        Public Async Function TestConflictedInaccessibleType(testHost As TestHost) As Task
            Await TestAsync(
"Imports System.Diagnostics
Namespace N
    Public Class Log
    End Class
End Namespace
Class C
    Public Function Goo()
        [|Log|]
    End Function
End Class",
                "Imports System.Diagnostics
Imports N

Namespace N
    Public Class Log
    End Class
End Namespace
Class C
    Public Function Goo()
        Log
    End Function
End Class", testHost, index:=1)
        End Function

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/858085")>
        <CombinatorialData>
        Public Async Function TestConflictedAttributeName(testHost As TestHost) As Task
            Await TestAsync(
"<[|Description|]> Public Class Description
End Class",
                "Imports System.ComponentModel

<[|Description|]> Public Class Description
End Class", testHost)
        End Function

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/772321")>
        <CombinatorialData>
        Public Async Function TestExtensionWithThePresenceOfTheSameNameNonExtensionMethod(testHost As TestHost) As Task
            Await TestAsync(
"Option Strict On
Imports System.Runtime.CompilerServices
Namespace NS1
    Class Program
        Sub main()
            Dim c = New C()
            [|c.Goo(4)|]
        End Sub
    End Class
    Class C
        Sub Goo(ByVal m As String)
        End Sub
    End Class
End Namespace
Namespace NS2
    Module A
        <Extension()>
        Sub Goo(ByVal ec As NS1.C, ByVal n As Integer)
        End Sub
    End Module
End Namespace",
                "Option Strict On
Imports System.Runtime.CompilerServices
Imports NS2

Namespace NS1
    Class Program
        Sub main()
            Dim c = New C()
            c.Goo(4)
        End Sub
    End Class
    Class C
        Sub Goo(ByVal m As String)
        End Sub
    End Class
End Namespace
Namespace NS2
    Module A
        <Extension()>
        Sub Goo(ByVal ec As NS1.C, ByVal n As Integer)
        End Sub
    End Module
End Namespace", testHost)
        End Function

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/772321")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/920398")>
        <CombinatorialData>
        Public Async Function TestExtensionWithThePresenceOfTheSameNameNonExtensionPrivateMethod(testHost As TestHost) As Task
            Await TestAsync(
"Option Strict On
Imports System.Runtime.CompilerServices
Namespace NS1
    Class Program
        Sub main()
            Dim c = New C()
            [|c.Goo(4)|]
        End Sub
    End Class
    Class C
        Private Sub Goo(ByVal m As Integer)
        End Sub
    End Class
End Namespace
Namespace NS2
    Module A
        <Extension()>
        Sub Goo(ByVal ec As NS1.C, ByVal n As Integer)
        End Sub
    End Module
End Namespace",
                "Option Strict On
Imports System.Runtime.CompilerServices
Imports NS2

Namespace NS1
    Class Program
        Sub main()
            Dim c = New C()
            c.Goo(4)
        End Sub
    End Class
    Class C
        Private Sub Goo(ByVal m As Integer)
        End Sub
    End Class
End Namespace
Namespace NS2
    Module A
        <Extension()>
        Sub Goo(ByVal ec As NS1.C, ByVal n As Integer)
        End Sub
    End Module
End Namespace", testHost)
        End Function

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/772321")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/920398")>
        <CombinatorialData>
        Public Async Function TestExtensionWithThePresenceOfTheSameNameExtensionPrivateMethod(testHost As TestHost) As Task
            Await TestAsync(
"Option Strict On
Imports System.Runtime.CompilerServices
Imports NS2
Namespace NS1
    Class Program
        Sub main()
            Dim c = New C()
            [|c.Goo(4)|]
        End Sub
    End Class
    Class C
        Sub Goo(ByVal m As String)
        End Sub
    End Class
End Namespace
Namespace NS2
    Module A
        <Extension()>
        Private Sub Goo(ByVal ec As NS1.C, ByVal n As Integer)
        End Sub
    End Module
End Namespace

Namespace NS3
    Module A
        <Extension()>
        Sub Goo(ByVal ec As NS1.C, ByVal n As Integer)
        End Sub
    End Module
End Namespace",
                "Option Strict On
Imports System.Runtime.CompilerServices
Imports NS2
Imports NS3

Namespace NS1
    Class Program
        Sub main()
            Dim c = New C()
            [|c.Goo(4)|]
        End Sub
    End Class
    Class C
        Sub Goo(ByVal m As String)
        End Sub
    End Class
End Namespace
Namespace NS2
    Module A
        <Extension()>
        Private Sub Goo(ByVal ec As NS1.C, ByVal n As Integer)
        End Sub
    End Module
End Namespace

Namespace NS3
    Module A
        <Extension()>
        Sub Goo(ByVal ec As NS1.C, ByVal n As Integer)
        End Sub
    End Module
End Namespace", testHost)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/916368")>
        Public Async Function TestAddImportForCref() As Task
            Dim initialText As String = "''' <summary>
''' This is just like <see cref=[|""INotifyPropertyChanged""|]/>, but this one is mine.
''' </summary>
Interface IMyInterface
End Interface"
            Dim expectedText As String = "Imports System.ComponentModel
''' <summary>
''' This is just like <see cref=""INotifyPropertyChanged""/>, but this one is mine.
''' </summary>
Interface IMyInterface
End Interface"
            Dim options = New VisualBasicParseOptions(documentationMode:=DocumentationMode.Diagnose)
            Await TestAsync(
                initialText,
                expectedText,
                parseOptions:=options)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/916368")>
        Public Async Function TestAddImportForCref2() As Task
            Dim initialText As String = "''' <summary>
''' This is just like <see cref=[|""INotifyPropertyChanged.PropertyChanged""|]/>, but this one is mine.
''' </summary>
Interface IMyInterface
End Interface"
            Dim expectedText As String = "Imports System.ComponentModel
''' <summary>
''' This is just like <see cref=""INotifyPropertyChanged.PropertyChanged""/>, but this one is mine.
''' </summary>
Interface IMyInterface
End Interface"
            Dim options = New VisualBasicParseOptions(documentationMode:=DocumentationMode.Diagnose)
            Await TestAsync(
                initialText,
                expectedText,
                parseOptions:=options)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/916368")>
        Public Async Function TestAddImportForCref3() As Task
            Dim initialText =
"
Namespace Goo
    Public Class C
        Public Sub M(a As Bar.D)
        End Sub
    End Class
End Namespace

Namespace Goo.Bar
    Public Class D
    End Class
End Namespace

Module Program
    ''' <summary>
    ''' <see cref='[|C.M(D)|]'/>
    ''' </summary>
    Sub Main(args As String())
    End Sub
End Module
"
            Dim expectedText =
"
Imports Goo

Namespace Goo
    Public Class C
        Public Sub M(a As Bar.D)
        End Sub
    End Class
End Namespace

Namespace Goo.Bar
    Public Class D
    End Class
End Namespace

Module Program
    ''' <summary>
    ''' <see cref='C.M(D)'/>
    ''' </summary>
    Sub Main(args As String())
    End Sub
End Module
"
            Dim options = New VisualBasicParseOptions(documentationMode:=DocumentationMode.Diagnose)
            Await TestAsync(
                initialText,
                expectedText,
                parseOptions:=options)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/916368")>
        Public Async Function TestAddImportForCref4() As Task
            Dim initialText =
"
Imports Goo

Namespace Goo
    Public Class C
        Public Sub M(a As Bar.D)
        End Sub
    End Class
End Namespace

Namespace Goo.Bar
    Public Class D
    End Class
End Namespace

Module Program
    ''' <summary>
    ''' <see cref='[|C.M(D)|]'/>
    ''' </summary>
    Sub Main(args As String())
    End Sub
End Module
"
            Dim expectedText =
"
Imports Goo
Imports Goo.Bar

Namespace Goo
    Public Class C
        Public Sub M(a As Bar.D)
        End Sub
    End Class
End Namespace

Namespace Goo.Bar
    Public Class D
    End Class
End Namespace

Module Program
    ''' <summary>
    ''' <see cref='C.M(D)'/>
    ''' </summary>
    Sub Main(args As String())
    End Sub
End Module
"
            Dim options = New VisualBasicParseOptions(documentationMode:=DocumentationMode.Diagnose)
            Await TestAsync(
                initialText,
                expectedText,
                parseOptions:=options)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/916368")>
        Public Async Function TestAddImportForCref5() As Task
            Dim initialText =
"
Namespace N1
    Public Class D
    End Class
End Namespace

''' <seealso cref='[|Test(D)|]'/>
Public Class MyClass2
    Sub Test(i As N1.D)
    End Sub
End Class
"
            Dim expectedText =
"
Imports N1

Namespace N1
    Public Class D
    End Class
End Namespace

''' <seealso cref='Test(D)'/>
Public Class MyClass2
    Sub Test(i As N1.D)
    End Sub
End Class
"
            Dim options = New VisualBasicParseOptions(documentationMode:=DocumentationMode.Diagnose)
            Await TestAsync(
                initialText,
                expectedText,
                parseOptions:=options)
        End Function

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/772321")>
        <CombinatorialData>
        Public Async Function TestExtensionMethodNoMemberAccessOverload(testHost As TestHost) As Task
            Await TestAsync(
"Option Strict On
Imports System.Runtime.CompilerServices
Namespace NS1
    Class C
        Sub Goo(ByVal m As String)
        End Sub
        Sub Bar()
            [|Goo(5)|]
        End Sub
    End Class
End Namespace
Namespace NS2
    Module A
        <Extension()>
        Sub Goo(ByVal ec As NS1.C, ByVal n As Integer)
        End Sub
    End Module
End Namespace",
                "Option Strict On
Imports System.Runtime.CompilerServices
Imports NS2

Namespace NS1
    Class C
        Sub Goo(ByVal m As String)
        End Sub
        Sub Bar()
            Goo(5)
        End Sub
    End Class
End Namespace
Namespace NS2
    Module A
        <Extension()>
        Sub Goo(ByVal ec As NS1.C, ByVal n As Integer)
        End Sub
    End Module
End Namespace", testHost, )
        End Function

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/772321")>
        <CombinatorialData>
        Public Async Function TestExtensionMethodNoMemberAccess(testHost As TestHost) As Task
            Await TestAsync(
"Option Strict On
Imports System.Runtime.CompilerServices
Namespace NS1
    Class C
        Sub Bar()
            [|Test(5)|]
        End Sub
    End Class
End Namespace
Namespace NS2
    Module A
        <Extension()>
        Sub Test(ByVal ec As NS1.C, ByVal n As Integer)
        End Sub
    End Module
End Namespace",
                "Option Strict On
Imports System.Runtime.CompilerServices
Imports NS2

Namespace NS1
    Class C
        Sub Bar()
            Test(5)
        End Sub
    End Class
End Namespace
Namespace NS2
    Module A
        <Extension()>
        Sub Test(ByVal ec As NS1.C, ByVal n As Integer)
        End Sub
    End Module
End Namespace", testHost, )
        End Function

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1003618")>
        <CombinatorialData>
        Public Async Function TestAddImportsTypeParsedAsNamespace(testHost As TestHost) As Task
            Await TestAsync(
"Imports System

Namespace Microsoft.VisualStudio.Utilities
    Public Class ContentTypeAttribute
        Inherits Attribute
    End Class
End Namespace

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.ContentType
End Namespace

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.AutomaticCompletion
    <[|ContentType|]>
    Public Class A
    End Class
End Namespace",
                "Imports System
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.VisualStudio.Utilities
    Public Class ContentTypeAttribute
        Inherits Attribute
    End Class
End Namespace

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.ContentType
End Namespace

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.AutomaticCompletion
    <ContentType>
    Public Class A
    End Class
End Namespace", testHost)
        End Function

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/773614")>
        <CombinatorialData>
        Public Async Function TestAddImportsForTypeAttribute(testHost As TestHost) As Task
            Await TestAsync(
"Imports System

Namespace N
    Class Outer
        <AttributeUsage(AttributeTargets.All)> Class MyAttribute
            Inherits Attribute
        End Class
    End Class
    <[|My()|]>
    Class Test
    End Class
End Namespace",
                "Imports System
Imports N.Outer

Namespace N
    Class Outer
        <AttributeUsage(AttributeTargets.All)> Class MyAttribute
            Inherits Attribute
        End Class
    End Class
    <My()>
    Class Test
    End Class
End Namespace", testHost)
        End Function

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/773614")>
        <CombinatorialData>
        Public Async Function TestAddImportsForTypeAttributeMultipleNestedClasses(testHost As TestHost) As Task
            Await TestAsync(
"Imports System

Namespace N
    Class Outer
        Class Inner
            <AttributeUsage(AttributeTargets.All)> Class MyAttribute
                Inherits Attribute
            End Class
        End Class
    End Class
    <[|My()|]>
    Class Test
    End Class
End Namespace",
                "Imports System
Imports N.Outer.Inner

Namespace N
    Class Outer
        Class Inner
            <AttributeUsage(AttributeTargets.All)> Class MyAttribute
                Inherits Attribute
            End Class
        End Class
    End Class
    <My()>
    Class Test
    End Class
End Namespace", testHost)
        End Function

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/773614")>
        <CombinatorialData>
        Public Async Function TestAddImportsForTypeAttributePartiallyQualified(testHost As TestHost) As Task
            Await TestAsync(
"Imports System

Namespace N
    Class Outer
        Class Inner
            <AttributeUsage(AttributeTargets.All)> Class MyAttribute
                Inherits Attribute
            End Class
        End Class
    End Class
    <[|Inner.My()|]>
    Class Test
    End Class
End Namespace",
                "Imports System
Imports N.Outer

Namespace N
    Class Outer
        Class Inner
            <AttributeUsage(AttributeTargets.All)> Class MyAttribute
                Inherits Attribute
            End Class
        End Class
    End Class
    <Inner.My()>
    Class Test
    End Class
End Namespace", testHost)
        End Function

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <CombinatorialData>
        Public Async Function TestConditionalAccessExtensionMethod(testHost As TestHost) As Task
            Dim initial = <Workspace>
                              <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                                  <Document FilePath="Program">
Public Class C
    Sub Main(a As C)
        Dim x As Integer? = a?[|.B|]
    End Sub
End Class
                                      </Document>
                                  <Document FilePath="Extensions">
Imports System.Runtime.CompilerServices
Namespace Extensions
    Public Module E
        &lt;Extension&gt;
        Public Function B(value As C) As C
            Return value
        End Function
    End Module
End Namespace
                                      </Document>
                              </Project>
                          </Workspace>.ToString
            Dim expected = "
Imports Extensions

Public Class C
    Sub Main(a As C)
        Dim x As Integer? = a?.B
    End Sub
End Class
"
            Await TestAsync(initial, expected, testHost)
        End Function

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <CombinatorialData>
        Public Async Function TestConditionalAccessExtensionMethod2(testHost As TestHost) As Task
            Dim initial = <Workspace>
                              <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                                  <Document FilePath="Program">
Option Strict On
Public Class C
    Sub Main(a As C)
        Dim x As Integer = a?.B[|.C|]
    End Sub

    Private Function B() As E
        Throw New NotImplementedException()
    End Function

    Public Class E
    End Class
End Class
                                      </Document>
                                  <Document FilePath="Extensions">
Imports System.Runtime.CompilerServices
Namespace Extensions
    Public Module D
        &lt;Extension&gt;
        Public Function C(value As C.E) As C.E
            Return value
        End Function
    End Module
End Namespace
                                      </Document>
                              </Project>
                          </Workspace>.ToString
            Dim expected = "Option Strict On

Imports Extensions

Public Class C
    Sub Main(a As C)
        Dim x As Integer = a?.B.C
    End Sub

    Private Function B() As E
        Throw New NotImplementedException()
    End Function

    Public Class E
    End Class
End Class
"
            Await TestAsync(initial, expected, testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestAddUsingInDirective(testHost As TestHost) As Task
            Await TestAsync(
"#Const Debug
Imports System
Imports System.Collections.Generic
#If Debug Then
Imports System.Linq
#End If
Module Program
    Sub Main(args As String())
        Dim a = [|File|].OpenRead("""")
    End Sub
End Module",
                "#Const Debug
Imports System
Imports System.Collections.Generic
Imports System.IO
#If Debug Then
Imports System.Linq
#End If
Module Program
    Sub Main(args As String())
        Dim a = File.OpenRead("""")
    End Sub
End Module", testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestAddUsingInDirective2(testHost As TestHost) As Task
            Await TestAsync(
"#Const Debug
#If Debug Then
Imports System
#End If
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        Dim a = [|File|].OpenRead("""")
    End Sub
End Module",
                "#Const Debug
#If Debug Then
Imports System
#End If
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Module Program
    Sub Main(args As String())
        Dim a = File.OpenRead("""")
    End Sub
End Module", testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestAddUsingInDirective3(testHost As TestHost) As Task
            Await TestAsync(
"#Const Debug
#If Debug Then
Imports System
Imports System.Collections.Generic
Imports System.Linq
#End If
Module Program
    Sub Main(args As String())
        Dim a = [|File|].OpenRead("""")
    End Sub
End Module",
                "#Const Debug
#If Debug Then
Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
#End If
Module Program
    Sub Main(args As String())
        Dim a = File.OpenRead("""")
    End Sub
End Module", testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestInaccessibleExtensionMethod(testHost As TestHost) As Task
            Dim initial = <Workspace>
                              <Project Language="Visual Basic" AssemblyName="lib" CommonReferences="true">
                                  <Document FilePath="Extension">
Imports System.Runtime.CompilerServices

Namespace MyLib
    Public Module Module1
        &lt;Extension()&gt;
        Public Function ExtMethod1(ByVal arg1 As String)
            Console.WriteLine(arg1)
            Return True
        End Function
    End Module
End Namespace
                                      </Document>
                              </Project>
                              <Project Language="Visual Basic" AssemblyName="Console" CommonReferences="true">
                                  <ProjectReference>lib</ProjectReference>
                                  <Document FilePath="ConsoleApp">
Module Module1

    Sub Main()
        Dim myStr = "".[|ExtMethod1()|]
    End Sub

End Module
                                      </Document>
                              </Project>
                          </Workspace>.ToString
            Dim expected = "
Imports MyLib

Module Module1

    Sub Main()
        Dim myStr = """".ExtMethod1()
    End Sub

End Module
"
            Await TestAsync(initial, expected, testHost)
        End Function

        <Theory, CombinatorialData>
        Public Async Function TestInaccessibleExtensionMethod2(testHost As TestHost) As Task
            Dim initial = <Workspace>
                              <Project Language="Visual Basic" AssemblyName="lib" CommonReferences="true">
                                  <Document FilePath="Extension">
Imports System.Runtime.CompilerServices

Namespace MyLib
    Module Module1
        &lt;Extension()&gt;
        Public Function ExtMethod1(ByVal arg1 As String)
            Console.WriteLine(arg1)
            Return True
        End Function
    End Module
End Namespace
                                      </Document>
                              </Project>
                              <Project Language="Visual Basic" AssemblyName="Console" CommonReferences="true">
                                  <ProjectReference>lib</ProjectReference>
                                  <Document FilePath="ConsoleApp">
Module Module1

    Sub Main()
        Dim myStr = "".[|ExtMethod1()|]
    End Sub

End Module
                                      </Document>
                              </Project>
                          </Workspace>.ToString
            Await TestMissingInRegularAndScriptAsync(initial)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/269")>
        Public Async Function TestAddImportForAddExtensionMethod() As Task
            Await TestAsync(
"Imports System
Imports System.Collections
Imports System.Runtime.CompilerServices
Class X
    Implements IEnumerable
    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Dim a = New X [|From {1}|]
        Return a.GetEnumerator()
    End Function
End Class
Namespace Ext
    Module Extensions
        <Extension>
        Public Sub Add(x As X, i As Integer)
        End Sub
    End Module
End Namespace",
"Imports System
Imports System.Collections
Imports System.Runtime.CompilerServices
Imports Ext

Class X
    Implements IEnumerable
    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Dim a = New X From {1}
        Return a.GetEnumerator()
    End Function
End Class
Namespace Ext
    Module Extensions
        <Extension>
        Public Sub Add(x As X, i As Integer)
        End Sub
    End Module
End Namespace",
parseOptions:=Nothing)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/269")>
        Public Async Function TestAddImportForAddExtensionMethod2() As Task
            Await TestAsync(
"Imports System
Imports System.Collections
Imports System.Runtime.CompilerServices
Class X
    Implements IEnumerable
    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Dim a = New X [|From {1, 2, 3}|]
        Return a.GetEnumerator()
    End Function
End Class
Namespace Ext
    Module Extensions
        <Extension>
        Public Sub Add(x As X, i As Integer)
        End Sub
    End Module
End Namespace",
"Imports System
Imports System.Collections
Imports System.Runtime.CompilerServices
Imports Ext

Class X
    Implements IEnumerable
    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Dim a = New X From {1, 2, 3}
        Return a.GetEnumerator()
    End Function
End Class
Namespace Ext
    Module Extensions
        <Extension>
        Public Sub Add(x As X, i As Integer)
        End Sub
    End Module
End Namespace",
parseOptions:=Nothing)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/269")>
        Public Async Function TestAddImportForAddExtensionMethod3() As Task
            Await TestAsync(
"Imports System
Imports System.Collections
Imports System.Runtime.CompilerServices
Class X
    Implements IEnumerable
    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Dim a = New X [|From {{1, 2, 3}, {4, 5, 6}, {7, 8, 9}}|]
        Return a.GetEnumerator()
    End Function
End Class
Namespace Ext
    Module Extensions
        <Extension>
        Public Sub Add(x As X, i As Integer)
        End Sub
    End Module
End Namespace",
"Imports System
Imports System.Collections
Imports System.Runtime.CompilerServices
Imports Ext

Class X
    Implements IEnumerable
    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Dim a = New X From {{1, 2, 3}, {4, 5, 6}, {7, 8, 9}}
        Return a.GetEnumerator()
    End Function
End Class
Namespace Ext
    Module Extensions
        <Extension>
        Public Sub Add(x As X, i As Integer)
        End Sub
    End Module
End Namespace",
parseOptions:=Nothing)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/269")>
        Public Async Function TestAddImportForAddExtensionMethod4() As Task
            Await TestAsync(
"Imports System
Imports System.Collections
Imports System.Runtime.CompilerServices
Class X
    Implements IEnumerable
    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Dim a = New X [|From {{1, 2, 3}, {""Four"", ""Five"", ""Six""}, {7, 8, 9}}|]
        Return a.GetEnumerator()
    End Function
End Class
Namespace Ext
    Module Extensions
        <Extension>
        Public Sub Add(x As X, i As Integer)
        End Sub
    End Module
End Namespace",
"Imports System
Imports System.Collections
Imports System.Runtime.CompilerServices
Imports Ext

Class X
    Implements IEnumerable
    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Dim a = New X From {{1, 2, 3}, {""Four"", ""Five"", ""Six""}, {7, 8, 9}}
        Return a.GetEnumerator()
    End Function
End Class
Namespace Ext
    Module Extensions
        <Extension>
        Public Sub Add(x As X, i As Integer)
        End Sub
    End Module
End Namespace",
parseOptions:=Nothing)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/269")>
        Public Async Function TestAddImportForAddExtensionMethod5() As Task
            Await TestAsync(
"Imports System
Imports System.Collections
Imports System.Runtime.CompilerServices
Class X
    Implements IEnumerable
    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Dim a = New X [|From {""This""}|]
        Return a.GetEnumerator()
    End Function
End Class
Namespace Ext
    Module Extensions
        <Extension>
        Public Sub Add(x As X, i As Integer)
        End Sub
    End Module
End Namespace",
"Imports System
Imports System.Collections
Imports System.Runtime.CompilerServices
Imports Ext

Class X
    Implements IEnumerable
    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Dim a = New X From {""This""}
        Return a.GetEnumerator()
    End Function
End Class
Namespace Ext
    Module Extensions
        <Extension>
        Public Sub Add(x As X, i As Integer)
        End Sub
    End Module
End Namespace",
parseOptions:=Nothing)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/269")>
        Public Async Function TestAddImportForAddExtensionMethod6() As Task
            Await TestAsync(
"Imports System
Imports System.Collections
Imports System.Runtime.CompilerServices
Class X
    Implements IEnumerable
    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Dim a = New X [|From {""This""}|]
        Return a.GetEnumerator()
    End Function
End Class
Namespace Ext
    Module Extensions
        <Extension>
        Public Sub Add(x As X, i As Integer)
        End Sub
    End Module
End Namespace
Namespace Ext2
    Module Extensions
        <Extension>
        Public Sub Add(x As X, i As Object())
        End Sub
    End Module
End Namespace",
"Imports System
Imports System.Collections
Imports System.Runtime.CompilerServices
Imports Ext

Class X
    Implements IEnumerable
    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Dim a = New X From {""This""}
        Return a.GetEnumerator()
    End Function
End Class
Namespace Ext
    Module Extensions
        <Extension>
        Public Sub Add(x As X, i As Integer)
        End Sub
    End Module
End Namespace
Namespace Ext2
    Module Extensions
        <Extension>
        Public Sub Add(x As X, i As Object())
        End Sub
    End Module
End Namespace",
parseOptions:=Nothing)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/269")>
        Public Async Function TestAddImportForAddExtensionMethod7() As Task
            Await TestAsync(
"Imports System
Imports System.Collections
Imports System.Runtime.CompilerServices
Class X
    Implements IEnumerable
    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Dim a = New X [|From {""This""}|]
        Return a.GetEnumerator()
    End Function
End Class
Namespace Ext
    Module Extensions
        <Extension>
        Public Sub Add(x As X, i As Integer)
        End Sub
    End Module
End Namespace
Namespace Ext2
    Module Extensions
        <Extension>
        Public Sub Add(x As X, i As Object())
        End Sub
    End Module
End Namespace",
"Imports System
Imports System.Collections
Imports System.Runtime.CompilerServices
Imports Ext2

Class X
    Implements IEnumerable
    Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Dim a = New X From {""This""}
        Return a.GetEnumerator()
    End Function
End Class
Namespace Ext
    Module Extensions
        <Extension>
        Public Sub Add(x As X, i As Integer)
        End Sub
    End Module
End Namespace
Namespace Ext2
    Module Extensions
        <Extension>
        Public Sub Add(x As X, i As Object())
        End Sub
    End Module
End Namespace",
index:=1,
parseOptions:=Nothing)
        End Function

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/935")>
        Public Async Function TestAddUsingWithOtherExtensionsInScope(testHost As TestHost) As Task
            Await TestAsync(
"Imports System.Linq
Imports System.Runtime.CompilerServices
Module Program
    Sub Main(args As String())
        Dim i = [|0.All|]()
    End Sub
End Module
Namespace X
    Module E
        <Extension>
        Public Function All(a As Integer) As Integer
            Return a
        End Function
    End Module
End Namespace",
                "Imports System.Linq
Imports System.Runtime.CompilerServices
Imports X

Module Program
    Sub Main(args As String())
        Dim i = 0.All()
    End Sub
End Module
Namespace X
    Module E
        <Extension>
        Public Function All(a As Integer) As Integer
            Return a
        End Function
    End Module
End Namespace", testHost)
        End Function

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/935")>
        Public Async Function TestAddUsingWithOtherExtensionsInScope2(testHost As TestHost) As Task
            Await TestAsync(
"Imports System.Linq
Imports System.Runtime.CompilerServices
Module Program
    Sub Main(args As String())
        Dim a = New Integer?
        Dim i = a?[|.All|]()
    End Sub
End Module
Namespace X
    Module E
        <Extension>
        Public Function All(a As Integer?) As Integer
            Return 0
        End Function
    End Module
End Namespace",
                "Imports System.Linq
Imports System.Runtime.CompilerServices
Imports X

Module Program
    Sub Main(args As String())
        Dim a = New Integer?
        Dim i = a?.All()
    End Sub
End Module
Namespace X
    Module E
        <Extension>
        Public Function All(a As Integer?) As Integer
            Return 0
        End Function
    End Module
End Namespace", testHost)
        End Function

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/562")>
        Public Async Function TestAddUsingWithOtherExtensionsInScope3(testHost As TestHost) As Task
            Await TestAsync(
"Imports System.Runtime.CompilerServices 
Imports X 
Module Program 
    Sub Main(args As String()) 
        Dim a = 0
        Dim i = [|a.All|](0)
    End Sub
End Module 
Namespace X 
    Module E 
        <Extension> 
        Public Function All(a As Integer) As Integer 
            Return a 
        End Function 
    End Module 
End Namespace 
Namespace Y 
    Module E 
        <Extension> 
        Public Function All(a As Integer, v As Integer) As Integer 
            Return a 
        End Function 
    End Module 
End Namespace",
                "Imports System.Runtime.CompilerServices
Imports X
Imports Y

Module Program
    Sub Main(args As String())
        Dim a = 0
        Dim i = a.All(0)
    End Sub
End Module
Namespace X
    Module E
        <Extension>
        Public Function All(a As Integer) As Integer
            Return a
        End Function
    End Module
End Namespace
Namespace Y
    Module E
        <Extension>
        Public Function All(a As Integer, v As Integer) As Integer
            Return a
        End Function
    End Module
End Namespace", testHost)
        End Function

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/562")>
        Public Async Function TestAddUsingWithOtherExtensionsInScope4(testHost As TestHost) As Task
            Await TestAsync(
"Imports System.Runtime.CompilerServices
Imports X
Module Program
    Sub Main(args As String())
        Dim a = New Integer?
        Dim i = a?[|.All|](0)
    End Sub
End Module
Namespace X
    Module E
        <Extension>
        Public Function All(a As Integer?) As Integer
            Return 0
        End Function
    End Module
End Namespace
Namespace Y
    Module E
        <Extension>
        Public Function All(a As Integer?, v As Integer) As Integer
            Return 0
        End Function
    End Module
End Namespace",
                "Imports System.Runtime.CompilerServices
Imports X
Imports Y

Module Program
    Sub Main(args As String())
        Dim a = New Integer?
        Dim i = a?.All(0)
    End Sub
End Module
Namespace X
    Module E
        <Extension>
        Public Function All(a As Integer?) As Integer
            Return 0
        End Function
    End Module
End Namespace
Namespace Y
    Module E
        <Extension>
        Public Function All(a As Integer?, v As Integer) As Integer
            Return 0
        End Function
    End Module
End Namespace", testHost)
        End Function

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/19796")>
        Public Async Function TestWhenInRome1(testHost As TestHost) As Task
            Await TestAsync(
"
Imports System
Imports B

Class Class1
    Dim v As [|AType|]
End Class
Namespace A
    Public Class AType
    End Class
End Namespace",
                "
Imports System
Imports A
Imports B

Class Class1
    Dim v As AType
End Class
Namespace A
    Public Class AType
    End Class
End Namespace", testHost, placeSystemFirst:=False)
        End Function

        <Theory, WorkItem("https://github.com/dotnet/roslyn/issues/19796")>
        <CombinatorialData>
        Public Async Function TestWhenInRome2(testHost As TestHost) As Task
            Await TestAsync(
"
Imports B
Imports System

Class Class1
    Dim v As [|AType|]
End Class
Namespace A
    Public Class AType
    End Class
End Namespace",
                "
Imports A
Imports B
Imports System

Class Class1
    Dim v As AType
End Class
Namespace A
    Public Class AType
    End Class
End Namespace", testHost, placeSystemFirst:=True)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1744")>
        Public Async Function TestImportIncompleteSub() As Task
            Await TestAsync(
"Imports System

Class A
    Dim a As Action = Sub()
                          Try
                          Catch ex As [|TestException|]
 End Sub
End Class
Namespace T
    Class TestException
        Inherits Exception
    End Class
End Namespace",
"Imports System
Imports T

Class A
    Dim a As Action = Sub()
                          Try
                          Catch ex As TestException
 End Sub
End Class
Namespace T
    Class TestException
        Inherits Exception
    End Class
End Namespace", TestHost.InProcess)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/1239")>
        Public Async Function TestImportIncompleteSub2() As Task
            Await TestAsync(
"Imports System
Imports System.Linq

Namespace X
    Class Test
    End Class
End Namespace
Class C
    Sub New()
        Dim s As Action = Sub()
                              Dim a = New [|Test|]()",
"Imports System
Imports System.Linq
Imports X

Namespace X
    Class Test
    End Class
End Namespace
Class C
    Sub New()
        Dim s As Action = Sub()
                              Dim a = New Test()", TestHost.InProcess)
        End Function
    End Class
End Namespace
