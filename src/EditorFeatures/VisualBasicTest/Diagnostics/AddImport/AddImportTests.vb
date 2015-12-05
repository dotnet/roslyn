' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.AddImport
Imports Microsoft.CodeAnalysis.VisualBasic.Diagnostics

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeActions.AddImport
    Public Class AddImportTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(
                Nothing,
                New VisualBasicAddImportCodeFixProvider())
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestSimpleImportFromSameFile() As Task
            Await TestAsync(
NewLines("Class Class1 \n Dim v As [|SomeClass1|] \n End Class \n Namespace SomeNamespace \n Public Class SomeClass1 \n End Class \n End Namespace"),
NewLines("Imports SomeNamespace \n Class Class1 \n Dim v As SomeClass1 \n End Class \n Namespace SomeNamespace \n Public Class SomeClass1 \n End Class \n End Namespace"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestSimpleImportFromReference() As Task
            Await TestAsync(
NewLines("Class Class1 \n Dim v As [|Thread|] \n End Class"),
NewLines("Imports System.Threading \n Class Class1 \n Dim v As Thread \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestSmartTagDisplay() As Task
            Await TestSmartTagTextAsync(
NewLines("Class Class1 \n Dim v As [|Thread|] \n End Class"),
"Imports System.Threading")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestGenericClassDefinitionAsClause() As Task
            Await TestAsync(
NewLines("Namespace SomeNamespace \n Class Base \n End Class \n End Namespace \n Class SomeClass(Of x As [|Base|]) \n End Class"),
NewLines("Imports SomeNamespace \n Namespace SomeNamespace \n Class Base \n End Class \n End Namespace \n Class SomeClass(Of x As Base) \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestGenericClassInstantiationOfClause() As Task
            Await TestAsync(
NewLines("Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class GenericClass(Of T) \n End Class \n Class Foo \n Sub Method1() \n Dim q As GenericClass(Of [|SomeClass|]) \n End Sub \n End Class"),
NewLines("Imports SomeNamespace \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class GenericClass(Of T) \n End Class \n Class Foo \n Sub Method1() \n Dim q As GenericClass(Of SomeClass) \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestGenericMethodDefinitionAsClause() As Task
            Await TestAsync(
NewLines("Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class Foo \n Sub Method1(Of T As [|SomeClass|]) \n End Sub \n End Class"),
NewLines("Imports SomeNamespace \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class Foo \n Sub Method1(Of T As SomeClass) \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestGenericMethodInvocationOfClause() As Task
            Await TestAsync(
NewLines("Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class Foo \n Sub Method1(Of T) \n End Sub \n Sub Method2() \n Method1(Of [|SomeClass|]) \n End Sub \n End Class"),
NewLines("Imports SomeNamespace \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class Foo \n Sub Method1(Of T) \n End Sub \n Sub Method2() \n Method1(Of SomeClass) \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestAttributeApplication() As Task
            Await TestAsync(
NewLines("<[|Something|]()> \n Class Foo \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits System.Attribute \n End Class \n End Namespace"),
NewLines("Imports SomeNamespace \n <Something()> \n Class Foo \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits System.Attribute \n End Class \n End Namespace"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestMultipleAttributeApplicationBelow() As Task
            Await TestAsync(
NewLines("<Existing()> \n <[|Something|]()> \n Class Foo \n End Class \n Class ExistingAttribute \n Inherits System.Attribute \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits System.Attribute \n End Class \n End Namespace"),
NewLines("Imports SomeNamespace \n <Existing()> \n <Something()> \n Class Foo \n End Class \n Class ExistingAttribute \n Inherits System.Attribute \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits System.Attribute \n End Class \n End Namespace"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestMultipleAttributeApplicationAbove() As Task
            Await TestAsync(
NewLines("<[|Something|]()> \n <Existing()> \n Class Foo \n End Class \n Class ExistingAttribute \n Inherits System.Attribute \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits System.Attribute \n End Class \n End Namespace"),
NewLines("Imports SomeNamespace \n <Something()> \n <Existing()> \n Class Foo \n End Class \n Class ExistingAttribute \n Inherits System.Attribute \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits System.Attribute \n End Class \n End Namespace"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestImportsIsEscapedWhenNamespaceMatchesKeyword() As Task
            Await TestAsync(
NewLines("Class SomeClass \n Dim x As [|Something|] \n End Class \n Namespace [Namespace] \n Class Something \n End Class \n End Namespace"),
NewLines("Imports [Namespace] \n Class SomeClass \n Dim x As Something \n End Class \n Namespace [Namespace] \n Class Something \n End Class \n End Namespace"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestImportsIsNOTEscapedWhenNamespaceMatchesKeywordButIsNested() As Task
            Await TestAsync(
NewLines("Class SomeClass \n Dim x As [|Something|] \n End Class \n Namespace Outer \n Namespace [Namespace] \n Class Something \n End Class \n End Namespace \n End Namespace"),
NewLines("Imports Outer.Namespace \n Class SomeClass \n Dim x As Something \n End Class \n Namespace Outer \n Namespace [Namespace] \n Class Something \n End Class \n End Namespace \n End Namespace"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestAddImportsNotSuggestedForImportsStatement() As Task
            Await TestMissingAsync(
NewLines("Imports [|InnerNamespace|] \n Namespace SomeNamespace \n Namespace InnerNamespace \n Class SomeClass \n End Class \n End Namespace \n End Namespace"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestAddImportsNotSuggestedForGenericTypeParametersOfClause() As Task
            Await TestMissingAsync(
NewLines("Class SomeClass \n Sub Foo(Of [|SomeClass|])(x As SomeClass) \n End Sub \n End Class \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestAddImportsNotSuggestedForGenericTypeParametersAsClause() As Task
            Await TestMissingAsync(
NewLines("Class SomeClass \n Sub Foo(Of SomeClass)(x As [|SomeClass|]) \n End Sub \n End Class \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace"))
        End Function

        <WorkItem(540543)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestCaseSensitivity1() As Task
            Await TestAsync(
NewLines("Class Foo \n Dim x As [|someclass|] \n End Class \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace"),
NewLines("Imports SomeNamespace \n Class Foo \n Dim x As SomeClass \n End Class \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestTypeFromMultipleNamespaces1() As Task
            Await TestAsync(
NewLines("Class Foo \n Function F() As [|IDictionary|] \n End Function \n End Class"),
NewLines("Imports System.Collections \n Class Foo \n Function F() As IDictionary \n End Function \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestTypeFromMultipleNamespaces2() As Task
            Await TestAsync(
NewLines("Class Foo \n Function F() As [|IDictionary|] \n End Function \n End Class"),
NewLines("Imports System.Collections.Generic \n Class Foo \n Function F() As IDictionary \n End Function \n End Class"),
index:=1)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestGenericWithNoArgs() As Task
            Await TestAsync(
NewLines("Class Foo \n Function F() As [|List|] \n End Function \n End Class"),
NewLines("Imports System.Collections.Generic \n Class Foo \n Function F() As List \n End Function \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestGenericWithCorrectArgs() As Task
            Await TestAsync(
NewLines("Class Foo \n Function F() As [|List(Of Integer)|] \n End Function \n End Class"),
NewLines("Imports System.Collections.Generic \n Class Foo \n Function F() As List(Of Integer) \n End Function \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestGenericWithWrongArgs1() As Task
            Await TestMissingAsync(
NewLines("Class Foo \n Function F() As [|List(Of Integer, String, Boolean)|] \n End Function \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestGenericWithWrongArgs2() As Task
            Await TestAsync(
NewLines("Class Foo \n Function F() As [|List(Of Integer, String)|] \n End Function \n End Class"),
NewLines("Imports System.Collections.Generic \n Class Foo \n Function F() As SortedList(Of Integer, String) \n End Function \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestGenericInLocalDeclaration() As Task
            Await TestAsync(
NewLines("Class Foo \n Sub Test() \n Dim x As New [|List(Of Integer)|] \n End Sub \n End Class"),
NewLines("Imports System.Collections.Generic \n Class Foo \n Sub Test() \n Dim x As New List(Of Integer) \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestGenericItemType() As Task
            Await TestAsync(
NewLines("Class Foo \n Sub Test() \n Dim x As New List(Of [|Int32|]) \n End Sub \n End Class"),
NewLines("Imports System \n Class Foo \n Sub Test() \n Dim x As New List(Of Int32) \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestGenerateWithExistingUsings() As Task
            Await TestAsync(
NewLines("Imports System \n Class Foo \n Sub Test() \n Dim x As New [|List(Of Integer)|] \n End Sub \n End Class"),
NewLines("Imports System \n Imports System.Collections.Generic \n Class Foo \n Sub Test() \n Dim x As New List(Of Integer) \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestGenerateInNamespace() As Task
            Await TestAsync(
NewLines("Imports System \n Namespace NS \n Class Foo \n Sub Test() \n Dim x As New [|List(Of Integer)|] \n End Sub \n End Class \n End Namespace"),
NewLines("Imports System \n Imports System.Collections.Generic \n Namespace NS \n Class Foo \n Sub Test() \n Dim x As New List(Of Integer) \n End Sub \n End Class \n End Namespace"))
        End Function

        <WorkItem(540519)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestCodeIssueCountInExistingUsing() As Task
            Await TestActionCountAsync(
NewLines("Imports System.Collections.Generic \n Namespace NS \n Class Foo \n Function Test() As [|IDictionary|] \n End Function \n End Class \n End Namespace"),
count:=1)
        End Function

        <WorkItem(540519)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestFixInExistingUsing() As Task
            Await TestAsync(
NewLines("Imports System.Collections.Generic \n Namespace NS \n Class Foo \n Function Test() As [|IDictionary|] \n End Function \n End Class \n End Namespace"),
NewLines("Imports System.Collections \n Imports System.Collections.Generic \n Namespace NS \n Class Foo \n Function Test() As IDictionary \n End Function \n End Class \n End Namespace"))
        End Function

        <WorkItem(541731)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestGenericExtensionMethod() As Task
            Await TestAsync(
NewLines("Imports System.Collections.Generic \n Class Test \n Private Sub Method(args As IList(Of Integer)) \n args.[|Where|]() \n End Sub \n End Class"),
NewLines("Imports System.Collections.Generic \n Imports System.Linq \n Class Test \n Private Sub Method(args As IList(Of Integer)) \n args.Where() \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestParameterType() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String(), f As [|FileMode|]) \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.IO \n Imports System.Linq \n Module Program \n Sub Main(args As String(), f As FileMode) \n End Sub \n End Module"))
        End Function

        <WorkItem(540519)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestAddWithExistingConflictWithDifferentArity() As Task
            Await TestAsync(
NewLines("Imports System.Collections.Generic \n Namespace NS \n Class Foo \n Function Test() As [|IDictionary|] \n End Function \n End Class \n End Namespace"),
NewLines("Imports System.Collections \n Imports System.Collections.Generic \n Namespace NS \n Class Foo \n Function Test() As IDictionary \n End Function \n End Class \n End Namespace"))
        End Function

        <WorkItem(540673)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestImportNamespace() As Task
            Await TestAsync(
NewLines("Class FOo \n Sub bar() \n Dim q As [|innernamespace|].someClass \n End Sub \n End Class \n Namespace SomeNamespace \n Namespace InnerNamespace \n Class SomeClass \n End Class \n End Namespace \n End Namespace"),
NewLines("Imports SomeNamespace \n Class FOo \n Sub bar() \n Dim q As InnerNamespace.SomeClass \n End Sub \n End Class \n Namespace SomeNamespace \n Namespace InnerNamespace \n Class SomeClass \n End Class \n End Namespace \n End Namespace"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestCaseSensitivity2() As Task
            Await TestAsync(
NewLines("Class FOo \n Sub bar() \n Dim q As [|innernamespace|].someClass \n End Sub \n End Class \n Namespace SomeNamespace \n Namespace InnerNamespace \n Class SomeClass \n End Class \n End Namespace \n End Namespace"),
NewLines("Imports SomeNamespace \n Class FOo \n Sub bar() \n Dim q As InnerNamespace.SomeClass \n End Sub \n End Class \n Namespace SomeNamespace \n Namespace InnerNamespace \n Class SomeClass \n End Class \n End Namespace \n End Namespace"))
        End Function

        <WorkItem(540745)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestCaseSensitivity3() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As [|foo|] \n End Sub \n End Module \n Namespace OUTER \n Namespace INNER \n Friend Class FOO \n End Class \n End Namespace \n End Namespace"),
NewLines("Imports OUTER.INNER \n Module Program \n Sub Main(args As String()) \n Dim x As FOO \n End Sub \n End Module \n Namespace OUTER \n Namespace INNER \n Friend Class FOO \n End Class \n End Namespace \n End Namespace"))
        End Function

        <WorkItem(541746)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestAddBlankLineAfterLastImports() As Task
            Await TestAsync(
<Text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
    End Sub
End Module

&lt;[|SomeAttr|]&gt;
Class Foo
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
Class Foo
End Class
Namespace SomeNamespace
    Friend Class SomeAttrAttribute
        Inherits Attribute
    End Class
End Namespace</Text>.Value.Replace(vbLf, vbCrLf),
index:=0,
compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestSimpleWhereClause() As Task
            Await TestAsync(
NewLines("Class Program \n Public Sub Linq1() \n Dim numbers() As Integer = New Integer(9) {5, 4, 1, 3, 9, 8, 6, 7, 2, 0} \n Dim lowNums = [|From n In numbers _ \n Where n < 5 _ \n Select n|] \n End Sub \n End Class"),
NewLines("Imports System.Linq \n Class Program \n Public Sub Linq1() \n Dim numbers() As Integer = New Integer(9) {5, 4, 1, 3, 9, 8, 6, 7, 2, 0} \n Dim lowNums = From n In numbers _ \n Where n < 5 _ \n Select n \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestAggregateClause() As Task
            Await TestAsync(
NewLines("Imports System.Collections.Generic \n Class Program \n Public Sub Linq1() \n Dim numbers() As Integer = New Integer(9) {5, 4, 1, 3, 9, 8, 6, 7, 2, 0} \n Dim greaterNums = [|Aggregate n In numbers \n Into greaterThan5 = All(n > 5)|] \n End Sub \n End Class"),
NewLines("Imports System.Collections.Generic \n Imports System.Linq \n Class Program \n Public Sub Linq1() \n Dim numbers() As Integer = New Integer(9) {5, 4, 1, 3, 9, 8, 6, 7, 2, 0} \n Dim greaterNums = Aggregate n In numbers \n Into greaterThan5 = All(n > 5) \n End Sub \n End Class"))
        End Function

        <WorkItem(543107)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestNoCrashOnMissingLeftSide() As Task
            Await TestMissingAsync(
NewLines("Imports System \n Class C1 \n Sub foo() \n Dim s = .[|first|] \n End Sub \n End Class"))
        End Function

        <WorkItem(544335)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestOnCallWithoutArgumentList() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n [|File|] \n End Sub \n End Module"),
NewLines("Imports System.IO \n Module Program \n Sub Main(args As String()) \n File \n End Sub \n End Module"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestAddToVisibleRegion() As Task
            Await TestAsync(
NewLines("#ExternalSource (""Default.aspx"", 1) \n Imports System \n #End ExternalSource \n #ExternalSource (""Default.aspx"", 2) \n Class C \n Sub Foo() \n Dim x As New [|StreamReader|] \n #End ExternalSource \n End Sub \n End Class"),
NewLines("#ExternalSource (""Default.aspx"", 1) \n Imports System \n Imports System.IO \n #End ExternalSource \n #ExternalSource (""Default.aspx"", 2) \n Class C \n Sub Foo() \n Dim x As New [|StreamReader|] \n #End ExternalSource \n End Sub \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestDoNotAddIntoHiddenRegion() As Task
            Await TestMissingAsync(
NewLines("Imports System \n #ExternalSource (""Default.aspx"", 2) \n Class C \n Sub Foo() \n Dim x As New [|StreamReader|] \n #End ExternalSource \n End Sub \n End Class"))
        End Function

        <WorkItem(546369)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestFormattingAfterImports() As Task
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
</Text>.Value.Replace(vbLf, vbCrLf),
compareTokens:=False)
        End Function

        <WorkItem(775448)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestShouldTriggerOnBC32045() As Task
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
End Module</Text>.Value.Replace(vbLf, vbCrLf),
index:=0,
compareTokens:=False)
        End Function

        <WorkItem(867425)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestUnknownIdentifierInModule() As Task
            Await TestAsync(
    NewLines("Module Foo \n Sub Bar(args As String()) \n Dim a = From f In args \n Let ext = [|Path|] \n End Sub \n End Module"),
    NewLines("Imports System.IO \n Module Foo \n Sub Bar(args As String()) \n Dim a = From f In args \n Let ext = Path \n End Sub \n End Module"))
        End Function

        <WorkItem(872908)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestConflictedGenericName() As Task
            Await TestAsync(
    NewLines("Module Foo \n Sub Bar(args As String()) \n Dim a = From f In args \n Let ext = [|Path|] \n End Sub \n End Module"),
    NewLines("Imports System.IO \n Module Foo \n Sub Bar(args As String()) \n Dim a = From f In args \n Let ext = Path \n End Sub \n End Module"))
        End Function

        <WorkItem(838253)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestConflictedInaccessibleType() As Task
            Await TestAsync(
    NewLines("Imports System.Diagnostics \n Namespace N \n Public Class Log \n End Class \n End Namespace \n Class C \n Public Function Foo() \n [|Log|] \n End Function \n End Class"),
    NewLines("Imports System.Diagnostics \n Imports N \n Namespace N \n Public Class Log \n End Class \n End Namespace \n Class C \n Public Function Foo() \n Log \n End Function \n End Class"), 1)
        End Function

        <WorkItem(858085)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestConflictedAttributeName() As Task
            Await TestAsync(
    NewLines("<[|Description|]> Public Class Description \n End Class"),
    NewLines("Imports System.ComponentModel \n <[|Description|]> Public Class Description \n End Class"))
        End Function

        <WorkItem(772321)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestExtensionWithThePresenceOfTheSameNameNonExtensionMethod() As Task
            Await TestAsync(
    NewLines("Option Strict On \n Imports System.Runtime.CompilerServices \n Namespace NS1 \n Class Program \n Sub main() \n Dim c = New C() \n [|c.Foo(4)|] \n End Sub \n End Class \n Class C \n Sub Foo(ByVal m As String) \n End Sub \n End Class \n End Namespace \n Namespace NS2 \n Module A \n <Extension()> \n Sub Foo(ByVal ec As NS1.C, ByVal n As Integer) \n End Sub \n End Module \n End Namespace "),
    NewLines("Option Strict On \n Imports System.Runtime.CompilerServices \n Imports NS2 \n Namespace NS1 \n Class Program \n Sub main() \n Dim c = New C() \n c.Foo(4) \n End Sub \n End Class \n Class C \n Sub Foo(ByVal m As String) \n End Sub \n End Class \n End Namespace \n Namespace NS2 \n Module A \n <Extension()> \n Sub Foo(ByVal ec As NS1.C, ByVal n As Integer) \n End Sub \n End Module \n End Namespace "))
        End Function

        <WorkItem(772321)>
        <WorkItem(920398)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestExtensionWithThePresenceOfTheSameNameNonExtensionPrivateMethod() As Task
            Await TestAsync(
    NewLines("Option Strict On \n Imports System.Runtime.CompilerServices \n Namespace NS1 \n Class Program \n Sub main() \n Dim c = New C() \n [|c.Foo(4)|] \n End Sub \n End Class \n Class C \n Private Sub Foo(ByVal m As Integer) \n End Sub \n End Class \n End Namespace \n Namespace NS2 \n Module A \n <Extension()> \n Sub Foo(ByVal ec As NS1.C, ByVal n As Integer) \n End Sub \n End Module \n End Namespace "),
    NewLines("Option Strict On \n Imports System.Runtime.CompilerServices \n Imports NS2 \n Namespace NS1 \n Class Program \n Sub main() \n Dim c = New C() \n c.Foo(4) \n End Sub \n End Class \n Class C \n Private Sub Foo(ByVal m As Integer) \n End Sub \n End Class \n End Namespace \n Namespace NS2 \n Module A \n <Extension()> \n Sub Foo(ByVal ec As NS1.C, ByVal n As Integer) \n End Sub \n End Module \n End Namespace "))
        End Function

        <WorkItem(772321)>
        <WorkItem(920398)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestExtensionWithThePresenceOfTheSameNameExtensionPrivateMethod() As Task
            Await TestAsync(
    NewLines("Option Strict On \n Imports System.Runtime.CompilerServices \n Imports NS2 \n Namespace NS1 \n Class Program \n Sub main() \n Dim c = New C() \n [|c.Foo(4)|] \n End Sub \n End Class \n Class C \n Sub Foo(ByVal m As String) \n End Sub \n End Class \n End Namespace \n Namespace NS2 \n Module A \n <Extension()> \n Private Sub Foo(ByVal ec As NS1.C, ByVal n As Integer) \n End Sub \n End Module \n End Namespace \n \n Namespace NS3 \n Module A \n <Extension()> \n Sub Foo(ByVal ec As NS1.C, ByVal n As Integer) \n End Sub \n End Module \n End Namespace "),
    NewLines("Option Strict On \n Imports System.Runtime.CompilerServices \n Imports NS2 \n Imports NS3 \n Namespace NS1 \n Class Program \n Sub main() \n Dim c = New C() \n [|c.Foo(4)|] \n End Sub \n End Class \n Class C \n Sub Foo(ByVal m As String) \n End Sub \n End Class \n End Namespace \n Namespace NS2 \n Module A \n <Extension()> \n Private Sub Foo(ByVal ec As NS1.C, ByVal n As Integer) \n End Sub \n End Module \n End Namespace \n \n Namespace NS3 \n Module A \n <Extension()> \n Sub Foo(ByVal ec As NS1.C, ByVal n As Integer) \n End Sub \n End Module \n End Namespace "))
        End Function

        <WorkItem(916368)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestAddImportForCref() As Task
            Dim initialText As String = NewLines("''' <summary>\n''' This is just like <see cref=[|""INotifyPropertyChanged""|]/>, but this one is mine.\n''' </summary>\nInterface IMyInterface\nEnd Interface")
            Dim expectedText As String = NewLines("Imports System.ComponentModel\n''' <summary>\n''' This is just like <see cref=""INotifyPropertyChanged""/>, but this one is mine.\n''' </summary>\nInterface IMyInterface\nEnd Interface")
            Dim options = New VisualBasicParseOptions(documentationMode:=DocumentationMode.Diagnose)
            Await TestAsync(
                initialText,
                expectedText,
                parseOptions:=options)
        End Function

        <WorkItem(916368)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestAddImportForCref2() As Task
            Dim initialText As String = NewLines("''' <summary>\n''' This is just like <see cref=[|""INotifyPropertyChanged.PropertyChanged""|]/>, but this one is mine.\n''' </summary>\nInterface IMyInterface\nEnd Interface")
            Dim expectedText As String = NewLines("Imports System.ComponentModel\n''' <summary>\n''' This is just like <see cref=""INotifyPropertyChanged.PropertyChanged""/>, but this one is mine.\n''' </summary>\nInterface IMyInterface\nEnd Interface")
            Dim options = New VisualBasicParseOptions(documentationMode:=DocumentationMode.Diagnose)
            Await TestAsync(
                initialText,
                expectedText,
                parseOptions:=options)
        End Function

        <WorkItem(916368)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestAddImportForCref3() As Task
            Dim initialText =
"
Namespace Foo
    Public Class C
        Public Sub M(a As Bar.D)
        End Sub
    End Class
End Namespace

Namespace Foo.Bar
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
Imports Foo

Namespace Foo
    Public Class C
        Public Sub M(a As Bar.D)
        End Sub
    End Class
End Namespace

Namespace Foo.Bar
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

        <WorkItem(916368)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestAddImportForCref4() As Task
            Dim initialText =
"
Imports Foo

Namespace Foo
    Public Class C
        Public Sub M(a As Bar.D)
        End Sub
    End Class
End Namespace

Namespace Foo.Bar
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
Imports Foo
Imports Foo.Bar

Namespace Foo
    Public Class C
        Public Sub M(a As Bar.D)
        End Sub
    End Class
End Namespace

Namespace Foo.Bar
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

        <WorkItem(916368)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
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

        <WorkItem(772321)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestExtensionMethodNoMemberAccessOverload() As Task
            Await TestAsync(
"Option Strict On
Imports System.Runtime.CompilerServices
Namespace NS1
    Class C
        Sub Foo(ByVal m As String)
        End Sub
        Sub Bar()
            [|Foo(5)|]
        End Sub
    End Class
End Namespace
Namespace NS2
    Module A
        <Extension()>
        Sub Foo(ByVal ec As NS1.C, ByVal n As Integer)
        End Sub
    End Module
End Namespace",
"Option Strict On
Imports System.Runtime.CompilerServices
Imports NS2

Namespace NS1
    Class C
        Sub Foo(ByVal m As String)
        End Sub
        Sub Bar()
            Foo(5)
        End Sub
    End Class
End Namespace
Namespace NS2
    Module A
        <Extension()>
        Sub Foo(ByVal ec As NS1.C, ByVal n As Integer)
        End Sub
    End Module
End Namespace",)
        End Function

        <WorkItem(772321)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestExtensionMethodNoMemberAccess() As Task
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
End Namespace",)
        End Function

        <WorkItem(1003618)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestAddImportsTypeParsedAsNamespace() As Task
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
End Namespace")
        End Function

        <WorkItem(773614)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestAddImportsForTypeAttribute() As Task
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
End Namespace", compareTokens:=False)
        End Function

        <WorkItem(773614)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestAddImportsForTypeAttributeMultipleNestedClasses() As Task
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
End Namespace", compareTokens:=False)
        End Function

        <WorkItem(773614)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestAddImportsForTypeAttributePartiallyQualified() As Task
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
End Namespace", compareTokens:=False)
        End Function

        <WorkItem(1064815)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestConditionalAccessExtensionMethod() As Task
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
            Dim expected = NewLines("\nImports Extensions\n\nPublic Class C\n    Sub Main(a As C)\n        Dim x As Integer? = a?.B\n    End Sub\nEnd Class\n")
            Await TestAsync(initial, expected, compareTokens:=False)
        End Function

        <WorkItem(1064815)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestConditionalAccessExtensionMethod2() As Task
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
            Dim expected = NewLines("Option Strict On\n\nImports Extensions\n\nPublic Class C\n    Sub Main(a As C)\n        Dim x As Integer = a?.B.C\n    End Sub\n\n    Private Function B() As E\n        Throw New NotImplementedException()\n    End Function\n\n    Public Class E\n    End Class\nEnd Class\n")
            Await TestAsync(initial, expected, compareTokens:=False)
        End Function

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestAddUsingInDirective() As Task
            Await TestAsync(
NewLines("#Const Debug\nImports System\nImports System.Collections.Generic\n#If Debug Then\nImports System.Linq\n#End If\nModule Program\n    Sub Main(args As String()) \n        Dim a = [|File|].OpenRead("""") \n    End Sub \n End Module"),
NewLines("#Const Debug\nImports System\nImports System.Collections.Generic\nImports System.IO\n#If Debug Then\nImports System.Linq\n#End If\nModule Program\n    Sub Main(args As String())\n        Dim a = File.OpenRead("""")\n    End Sub\nEnd Module"),
compareTokens:=False)
        End Function

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestAddUsingInDirective2() As Task
            Await TestAsync(
NewLines("#Const Debug\n#If Debug Then\nImports System\n#End If\nImports System.Collections.Generic\nImports System.Linq\n Module Program\n    Sub Main(args As String())\n        Dim a = [|File|].OpenRead("""")\n End Sub\n End Module"),
NewLines("#Const Debug\n#If Debug Then\nImports System\n#End If\nImports System.Collections.Generic\nImports System.IO\nImports System.Linq\nModule Program\n    Sub Main(args As String())\n        Dim a = File.OpenRead("""")\n    End Sub\nEnd Module"),
compareTokens:=False)
        End Function

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestAddUsingInDirective3() As Task
            Await TestAsync(
NewLines("#Const Debug\n#If Debug Then\nImports System\nImports System.Collections.Generic\nImports System.Linq\n#End If\nModule Program\n    Sub Main(args As String())\n        Dim a = [|File|].OpenRead("""") \n End Sub \n End Module"),
NewLines("#Const Debug\n#If Debug Then\nImports System\nImports System.Collections.Generic\nImports System.IO\nImports System.Linq\n#End If\nModule Program\n    Sub Main(args As String())\n        Dim a = File.OpenRead("""")\n    End Sub\nEnd Module"),
compareTokens:=False)
        End Function

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestInaccessibleExtensionMethod() As Task
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
            Dim expected = NewLines("\nImports MyLib\n\nModule Module1\n\n    Sub Main()\n        Dim myStr = """".ExtMethod1()\n    End Sub\n\nEnd Module\n")
            Await TestAsync(initial, expected, compareTokens:=False)
        End Function

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestInaccessibleExtensionMethod2() As Task
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
            Await TestMissingAsync(initial)
        End Function

        <WorkItem(269)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestAddImportForAddExtentionMethod() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections \n Imports System.Runtime.CompilerServices \n Class X \n Implements IEnumerable \n Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator \n Dim a = New X [|From {1}|] \n Return a.GetEnumerator() \n End Function \n End Class \n Namespace Ext \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Integer) \n End Sub \n End Module \n End Namespace"),
NewLines("Imports System \n Imports System.Collections \n Imports System.Runtime.CompilerServices \n Imports Ext \n Class X \n Implements IEnumerable \n Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator \n Dim a = New X From {1} \n Return a.GetEnumerator() \n End Function \n End Class \n Namespace Ext \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Integer) \n End Sub \n End Module \n End Namespace"),
parseOptions:=Nothing)
        End Function

        <WorkItem(269)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestAddImportForAddExtentionMethod2() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections \n Imports System.Runtime.CompilerServices \n Class X \n Implements IEnumerable \n Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator \n Dim a = New X [|From {1, 2, 3}|] \n Return a.GetEnumerator() \n End Function \n End Class \n Namespace Ext \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Integer) \n End Sub \n End Module \n End Namespace"),
NewLines("Imports System \n Imports System.Collections \n Imports System.Runtime.CompilerServices \n Imports Ext \n Class X \n Implements IEnumerable \n Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator \n Dim a = New X From {1, 2, 3} \n Return a.GetEnumerator() \n End Function \n End Class \n Namespace Ext \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Integer) \n End Sub \n End Module \n End Namespace"),
parseOptions:=Nothing)
        End Function

        <WorkItem(269)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestAddImportForAddExtentionMethod3() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections \n Imports System.Runtime.CompilerServices \n Class X \n Implements IEnumerable \n Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator \n Dim a = New X [|From {{1, 2, 3}, {4, 5, 6}, {7, 8, 9}}|] \n Return a.GetEnumerator() \n End Function \n End Class \n Namespace Ext \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Integer) \n End Sub \n End Module \n End Namespace"),
NewLines("Imports System \n Imports System.Collections \n Imports System.Runtime.CompilerServices \n Imports Ext \n Class X \n Implements IEnumerable \n Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator \n Dim a = New X From {{1, 2, 3}, {4, 5, 6}, {7, 8, 9}} \n Return a.GetEnumerator() \n End Function \n End Class \n Namespace Ext \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Integer) \n End Sub \n End Module \n End Namespace"),
parseOptions:=Nothing)
        End Function

        <WorkItem(269)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestAddImportForAddExtentionMethod4() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections \n Imports System.Runtime.CompilerServices \n Class X \n Implements IEnumerable \n Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator \n Dim a = New X [|From {{1, 2, 3}, {""Four"", ""Five"", ""Six""}, {7, 8, 9}}|] \n Return a.GetEnumerator() \n End Function \n End Class \n Namespace Ext \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Integer) \n End Sub \n End Module \n End Namespace"),
NewLines("Imports System \n Imports System.Collections \n Imports System.Runtime.CompilerServices \n Imports Ext \n Class X \n Implements IEnumerable \n Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator \n Dim a = New X From {{1, 2, 3}, {""Four"", ""Five"", ""Six""}, {7, 8, 9}} \n Return a.GetEnumerator() \n End Function \n End Class \n Namespace Ext \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Integer) \n End Sub \n End Module \n End Namespace"),
parseOptions:=Nothing)
        End Function

        <WorkItem(269)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestAddImportForAddExtentionMethod5() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections \n Imports System.Runtime.CompilerServices \n Class X \n Implements IEnumerable \n Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator \n Dim a = New X [|From {""This""}|] \n Return a.GetEnumerator() \n End Function \n End Class \n Namespace Ext \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Integer) \n End Sub \n End Module \n End Namespace"),
NewLines("Imports System \n Imports System.Collections \n Imports System.Runtime.CompilerServices \n Imports Ext \n Class X \n Implements IEnumerable \n Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator \n Dim a = New X From {""This""} \n Return a.GetEnumerator() \n End Function \n End Class \n Namespace Ext \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Integer) \n End Sub \n End Module \n End Namespace"),
parseOptions:=Nothing)
        End Function

        <WorkItem(269)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestAddImportForAddExtentionMethod6() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections \n Imports System.Runtime.CompilerServices \n Class X \n Implements IEnumerable \n Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator \n Dim a = New X [|From {""This""}|] \n Return a.GetEnumerator() \n End Function \n End Class \n Namespace Ext \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Integer) \n End Sub \n End Module \n End Namespace \n Namespace Ext2 \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Object()) \n End Sub \n End Module \n End Namespace"),
NewLines("Imports System \n Imports System.Collections \n Imports System.Runtime.CompilerServices \n Imports Ext \n Class X \n Implements IEnumerable \n Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator \n Dim a = New X From {""This""} \n Return a.GetEnumerator() \n End Function \n End Class \n Namespace Ext \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Integer) \n End Sub \n End Module \n End Namespace \n Namespace Ext2 \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Object()) \n End Sub \n End Module \n End Namespace"),
parseOptions:=Nothing)
        End Function

        <WorkItem(269)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestAddImportForAddExtentionMethod7() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections \n Imports System.Runtime.CompilerServices \n Class X \n Implements IEnumerable \n Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator \n Dim a = New X [|From {""This""}|] \n Return a.GetEnumerator() \n End Function \n End Class \n Namespace Ext \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Integer) \n End Sub \n End Module \n End Namespace \n Namespace Ext2 \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Object()) \n End Sub \n End Module \n End Namespace"),
NewLines("Imports System \n Imports System.Collections \n Imports System.Runtime.CompilerServices \n Imports Ext2 \n Class X \n Implements IEnumerable \n Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator \n Dim a = New X From {""This""} \n Return a.GetEnumerator() \n End Function \n End Class \n Namespace Ext \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Integer) \n End Sub \n End Module \n End Namespace \n Namespace Ext2 \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Object()) \n End Sub \n End Module \n End Namespace"),
index:=1,
parseOptions:=Nothing)
        End Function

        <WorkItem(935, "https://github.com/dotnet/roslyn/issues/935")>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestAddUsingWithOtherExtensionsInScope() As Task
            Await TestAsync(
NewLines("Imports System.Linq \n Imports System.Runtime.CompilerServices \n Module Program \n Sub Main(args As String()) \n Dim i = [|0.All|]() \n End Sub \n End Module \n Namespace X \n Module E \n <Extension> \n Public Function All(a As Integer) As Integer \n Return a \n End Function \n End Module \n End Namespace"),
NewLines("Imports System.Linq \n Imports System.Runtime.CompilerServices \n Imports X \n Module Program \n Sub Main(args As String()) \n Dim i = 0.All() \n End Sub \n End Module \n Namespace X \n Module E \n <Extension> \n Public Function All(a As Integer) As Integer \n Return a \n End Function \n End Module \n End Namespace"))
        End Function

        <WorkItem(935, "https://github.com/dotnet/roslyn/issues/935")>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestAddUsingWithOtherExtensionsInScope2() As Task
            Await TestAsync(
NewLines("Imports System.Linq \n Imports System.Runtime.CompilerServices \n Module Program \n Sub Main(args As String()) \n Dim a = New Integer? \n Dim i = a?[|.All|]() \n End Sub \n End Module \n Namespace X \n Module E \n <Extension> \n Public Function All(a As Integer?) As Integer \n Return 0 \n End Function \n End Module \n End Namespace"),
NewLines("Imports System.Linq \n Imports System.Runtime.CompilerServices \n Imports X \n Module Program \n Sub Main(args As String()) \n Dim a = New Integer? \n Dim i = a?.All() \n End Sub \n End Module \n Namespace X \n Module E \n <Extension> \n Public Function All(a As Integer?) As Integer \n Return 0 \n End Function \n End Module \n End Namespace"))
        End Function

        <WorkItem(562, "https://github.com/dotnet/roslyn/issues/562")>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestAddUsingWithOtherExtensionsInScope3() As Task
            Await TestAsync(
NewLines("Imports System.Runtime.CompilerServices \n Imports X \n Module Program \n Sub Main(args As String()) \n Dim a = 0 \n Dim i = [|a.All|](0) \n End Sub \n End Module \n Namespace X \n Module E \n <Extension> \n Public Function All(a As Integer) As Integer \n Return a \n End Function \n End Module \n End Namespace \n Namespace Y \n Module E \n <Extension> \n Public Function All(a As Integer, v As Integer) As Integer \n Return a \n End Function \n End Module \n End Namespace"),
NewLines("Imports System.Runtime.CompilerServices \n Imports X \n Imports Y \n Module Program \n Sub Main(args As String()) \n Dim a = 0 \n Dim i = a.All(0) \n End Sub \n End Module \n Namespace X \n Module E \n <Extension> \n Public Function All(a As Integer) As Integer \n Return a \n End Function \n End Module \n End Namespace \n Namespace Y \n Module E \n <Extension> \n Public Function All(a As Integer, v As Integer) As Integer \n Return a \n End Function \n End Module \n End Namespace"))
        End Function

        <WorkItem(562, "https://github.com/dotnet/roslyn/issues/562")>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Async Function TestAddUsingWithOtherExtensionsInScope4() As Task
            Await TestAsync(
NewLines("Imports System.Runtime.CompilerServices \n Imports X \n Module Program \n Sub Main(args As String()) \n Dim a = New Integer? \n Dim i = a?[|.All|](0) \n End Sub \n End Module \n Namespace X \n Module E \n <Extension> \n Public Function All(a As Integer?) As Integer \n Return 0 \n End Function \n End Module \n End Namespace \n Namespace Y \n Module E \n <Extension> \n Public Function All(a As Integer?, v As Integer) As Integer \n Return 0 \n End Function \n End Module \n End Namespace"),
NewLines("Imports System.Runtime.CompilerServices \n Imports X \n Imports Y \n Module Program \n Sub Main(args As String()) \n Dim a = New Integer? \n Dim i = a?.All(0) \n End Sub \n End Module \n Namespace X \n Module E \n <Extension> \n Public Function All(a As Integer?) As Integer \n Return 0 \n End Function \n End Module \n End Namespace \n Namespace Y \n Module E \n <Extension> \n Public Function All(a As Integer?, v As Integer) As Integer \n Return 0 \n End Function \n End Module \n End Namespace"))
        End Function

        Public Class AddImportTestsWithAddImportDiagnosticProvider
            Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

            Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
                Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(
                    New VisualBasicUnboundIdentifiersDiagnosticAnalyzer(),
                    New VisualBasicAddImportCodeFixProvider())
            End Function

            <WorkItem(829970)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
            Public Async Function TestUnknownIdentifierInAttributeSyntaxWithoutTarget() As Task
                Await TestAsync(
    NewLines("Class Class1 \n <[|Extension|]> \n End Class"),
    NewLines("Imports System.Runtime.CompilerServices \n Class Class1 \n <Extension> \n End Class"))
            End Function

            <WorkItem(829970)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
            Public Async Function TestUnknownIdentifierGenericName() As Task
                Await TestAsync(
    NewLines("Class C \n    Inherits Attribute \n    Public Sub New(x As System.Type) \n    End Sub \n    <C([|List(Of Integer)|])> \n End Class"),
    NewLines("Imports System.Collections.Generic \n Class C \n    Inherits Attribute \n    Public Sub New(x As System.Type) \n    End Sub \n    <C(List(Of Integer))> \n End Class"))
            End Function

            <WorkItem(829970)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
            Public Async Function TestUnknownIdentifierAddNamespaceImport() As Task
                Await TestAsync(
    NewLines("Class Class1 \n <[|Tasks.Task|]> \n End Class"),
    NewLines("Imports System.Threading \n Class Class1 \n <Tasks.Task> \n End Class"))
            End Function

            <WorkItem(829970)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
            Public Async Function TestUnknownAttributeInModule() As Task
                Await TestAsync(
    NewLines("Module Foo \n <[|Extension|]> \n End Module"),
    NewLines("Imports System.Runtime.CompilerServices \n Module Foo \n <Extension> \n End Module"))

                Await TestAsync(
    NewLines("Module Foo \n <[|Extension()|]> \n End Module"),
    NewLines("Imports System.Runtime.CompilerServices \n Module Foo \n <Extension()> \n End Module"))
            End Function

            <WorkItem(938296)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
            Public Async Function TestNullParentInNode() As Task
                Await TestMissingAsync(
"Imports System.Collections.Generic

Class MultiDictionary(Of K, V)
    Inherits Dictionary(Of K, HashSet(Of V))

    Sub M()
        Dim hs = New HashSet(Of V)([|Comparer|])
    End Sub
End Class")
            End Function

            <WorkItem(1744, "https://github.com/dotnet/roslyn/issues/1744")>
            <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
            Public Async Function TestImportIncompleteSub() As Task
                Await TestAsync(
    NewLines("Class A \n Dim a As Action = Sub() \n Try \n Catch ex As [|TestException|] \n End Sub \n End Class \n Namespace T \n Class TestException \n Inherits Exception \n End Class \n End Namespace"),
    NewLines("Imports T \n Class A \n Dim a As Action = Sub() \n Try \n Catch ex As TestException \n End Sub \n End Class \n Namespace T \n Class TestException \n Inherits Exception \n End Class \n End Namespace"))
            End Function

            <WorkItem(1239, "https://github.com/dotnet/roslyn/issues/1239")>
            <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
            Public Async Function TestImportIncompleteSub2() As Task
                Await TestAsync(
    NewLines("Imports System.Linq \n Namespace X \n Class Test \n End Class \n End Namespace \n Class C \n Sub New() \n Dim s As Action = Sub() \n Dim a = New [|Test|]()"),
    NewLines("Imports System.Linq \n Imports X \n Namespace X \n Class Test \n End Class \n End Namespace \n Class C \n Sub New() \n Dim s As Action = Sub() \n Dim a = New Test()"))
            End Function
        End Class
    End Class
End Namespace
