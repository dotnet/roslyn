' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off
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
        Public Sub TestSimpleImportFromSameFile()
            Test(
NewLines("Class Class1 \n Dim v As [|SomeClass1|] \n End Class \n Namespace SomeNamespace \n Public Class SomeClass1 \n End Class \n End Namespace"),
NewLines("Imports SomeNamespace \n Class Class1 \n Dim v As SomeClass1 \n End Class \n Namespace SomeNamespace \n Public Class SomeClass1 \n End Class \n End Namespace"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestSimpleImportFromReference()
            Test(
NewLines("Class Class1 \n Dim v As [|Thread|] \n End Class"),
NewLines("Imports System.Threading \n Class Class1 \n Dim v As Thread \n End Class"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestSmartTagDisplay()
            TestSmartTagText(
NewLines("Class Class1 \n Dim v As [|Thread|] \n End Class"),
"Imports System.Threading")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestGenericClassDefinitionAsClause()
            Test(
NewLines("Namespace SomeNamespace \n Class Base \n End Class \n End Namespace \n Class SomeClass(Of x As [|Base|]) \n End Class"),
NewLines("Imports SomeNamespace \n Namespace SomeNamespace \n Class Base \n End Class \n End Namespace \n Class SomeClass(Of x As Base) \n End Class"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestGenericClassInstantiationOfClause()
            Test(
NewLines("Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class GenericClass(Of T) \n End Class \n Class Foo \n Sub Method1() \n Dim q As GenericClass(Of [|SomeClass|]) \n End Sub \n End Class"),
NewLines("Imports SomeNamespace \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class GenericClass(Of T) \n End Class \n Class Foo \n Sub Method1() \n Dim q As GenericClass(Of SomeClass) \n End Sub \n End Class"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestGenericMethodDefinitionAsClause()
            Test(
NewLines("Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class Foo \n Sub Method1(Of T As [|SomeClass|]) \n End Sub \n End Class"),
NewLines("Imports SomeNamespace \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class Foo \n Sub Method1(Of T As SomeClass) \n End Sub \n End Class"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestGenericMethodInvocationOfClause()
            Test(
NewLines("Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class Foo \n Sub Method1(Of T) \n End Sub \n Sub Method2() \n Method1(Of [|SomeClass|]) \n End Sub \n End Class"),
NewLines("Imports SomeNamespace \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class Foo \n Sub Method1(Of T) \n End Sub \n Sub Method2() \n Method1(Of SomeClass) \n End Sub \n End Class"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestAttributeApplication()
            Test(
NewLines("<[|Something|]()> \n Class Foo \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits System.Attribute \n End Class \n End Namespace"),
NewLines("Imports SomeNamespace \n <Something()> \n Class Foo \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits System.Attribute \n End Class \n End Namespace"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestMultipleAttributeApplicationBelow()
            Test(
NewLines("<Existing()> \n <[|Something|]()> \n Class Foo \n End Class \n Class ExistingAttribute \n Inherits System.Attribute \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits System.Attribute \n End Class \n End Namespace"),
NewLines("Imports SomeNamespace \n <Existing()> \n <Something()> \n Class Foo \n End Class \n Class ExistingAttribute \n Inherits System.Attribute \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits System.Attribute \n End Class \n End Namespace"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestMultipleAttributeApplicationAbove()
            Test(
NewLines("<[|Something|]()> \n <Existing()> \n Class Foo \n End Class \n Class ExistingAttribute \n Inherits System.Attribute \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits System.Attribute \n End Class \n End Namespace"),
NewLines("Imports SomeNamespace \n <Something()> \n <Existing()> \n Class Foo \n End Class \n Class ExistingAttribute \n Inherits System.Attribute \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits System.Attribute \n End Class \n End Namespace"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestImportsIsEscapedWhenNamespaceMatchesKeyword()
            Test(
NewLines("Class SomeClass \n Dim x As [|Something|] \n End Class \n Namespace [Namespace] \n Class Something \n End Class \n End Namespace"),
NewLines("Imports [Namespace] \n Class SomeClass \n Dim x As Something \n End Class \n Namespace [Namespace] \n Class Something \n End Class \n End Namespace"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestImportsIsNOTEscapedWhenNamespaceMatchesKeywordButIsNested()
            Test(
NewLines("Class SomeClass \n Dim x As [|Something|] \n End Class \n Namespace Outer \n Namespace [Namespace] \n Class Something \n End Class \n End Namespace \n End Namespace"),
NewLines("Imports Outer.Namespace \n Class SomeClass \n Dim x As Something \n End Class \n Namespace Outer \n Namespace [Namespace] \n Class Something \n End Class \n End Namespace \n End Namespace"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestAddImportsNotSuggestedForImportsStatement()
            TestMissing(
NewLines("Imports [|InnerNamespace|] \n Namespace SomeNamespace \n Namespace InnerNamespace \n Class SomeClass \n End Class \n End Namespace \n End Namespace"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestAddImportsNotSuggestedForGenericTypeParametersOfClause()
            TestMissing(
NewLines("Class SomeClass \n Sub Foo(Of [|SomeClass|])(x As SomeClass) \n End Sub \n End Class \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestAddImportsNotSuggestedForGenericTypeParametersAsClause()
            TestMissing(
NewLines("Class SomeClass \n Sub Foo(Of SomeClass)(x As [|SomeClass|]) \n End Sub \n End Class \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace"))
        End Sub

        <WorkItem(540543)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestCaseSensitivity1()
            Test(
NewLines("Class Foo \n Dim x As [|someclass|] \n End Class \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace"),
NewLines("Imports SomeNamespace \n Class Foo \n Dim x As SomeClass \n End Class \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestTypeFromMultipleNamespaces1()
            Test(
NewLines("Class Foo \n Function F() As [|IDictionary|] \n End Function \n End Class"),
NewLines("Imports System.Collections \n Class Foo \n Function F() As IDictionary \n End Function \n End Class"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestTypeFromMultipleNamespaces2()
            Test(
NewLines("Class Foo \n Function F() As [|IDictionary|] \n End Function \n End Class"),
NewLines("Imports System.Collections.Generic \n Class Foo \n Function F() As IDictionary \n End Function \n End Class"),
index:=1)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestGenericWithNoArgs()
            Test(
NewLines("Class Foo \n Function F() As [|List|] \n End Function \n End Class"),
NewLines("Imports System.Collections.Generic \n Class Foo \n Function F() As List \n End Function \n End Class"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestGenericWithCorrectArgs()
            Test(
NewLines("Class Foo \n Function F() As [|List(Of Integer)|] \n End Function \n End Class"),
NewLines("Imports System.Collections.Generic \n Class Foo \n Function F() As List(Of Integer) \n End Function \n End Class"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestGenericWithWrongArgs()
            TestMissing(
NewLines("Class Foo \n Function F() As [|List(Of Integer, String)|] \n End Function \n End Class"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestGenericInLocalDeclaration()
            Test(
NewLines("Class Foo \n Sub Test() \n Dim x As New [|List(Of Integer)|] \n End Sub \n End Class"),
NewLines("Imports System.Collections.Generic \n Class Foo \n Sub Test() \n Dim x As New List(Of Integer) \n End Sub \n End Class"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestGenericItemType()
            Test(
NewLines("Class Foo \n Sub Test() \n Dim x As New List(Of [|Int32|]) \n End Sub \n End Class"),
NewLines("Imports System \n Class Foo \n Sub Test() \n Dim x As New List(Of Int32) \n End Sub \n End Class"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestGenerateWithExistingUsings()
            Test(
NewLines("Imports System \n Class Foo \n Sub Test() \n Dim x As New [|List(Of Integer)|] \n End Sub \n End Class"),
NewLines("Imports System \n Imports System.Collections.Generic \n Class Foo \n Sub Test() \n Dim x As New List(Of Integer) \n End Sub \n End Class"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestGenerateInNamespace()
            Test(
NewLines("Imports System \n Namespace NS \n Class Foo \n Sub Test() \n Dim x As New [|List(Of Integer)|] \n End Sub \n End Class \n End Namespace"),
NewLines("Imports System \n Imports System.Collections.Generic \n Namespace NS \n Class Foo \n Sub Test() \n Dim x As New List(Of Integer) \n End Sub \n End Class \n End Namespace"))
        End Sub

        <WorkItem(540519)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestCodeIssueCountInExistingUsing()
            TestActionCount(
NewLines("Imports System.Collections.Generic \n Namespace NS \n Class Foo \n Function Test() As [|IDictionary|] \n End Function \n End Class \n End Namespace"),
count:=1)
        End Sub

        <WorkItem(540519)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestFixInExistingUsing()
            Test(
NewLines("Imports System.Collections.Generic \n Namespace NS \n Class Foo \n Function Test() As [|IDictionary|] \n End Function \n End Class \n End Namespace"),
NewLines("Imports System.Collections \n Imports System.Collections.Generic \n Namespace NS \n Class Foo \n Function Test() As IDictionary \n End Function \n End Class \n End Namespace"))
        End Sub

        <WorkItem(541731)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestGenericExtensionMethod()
            Test(
NewLines("Imports System.Collections.Generic \n Class Test \n Private Sub Method(args As IList(Of Integer)) \n args.[|Where|]() \n End Sub \n End Class"),
NewLines("Imports System.Collections.Generic \n Imports System.Linq \n Class Test \n Private Sub Method(args As IList(Of Integer)) \n args.Where() \n End Sub \n End Class"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestParameterType()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String(), f As [|FileMode|]) \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.IO \n Imports System.Linq \n Module Program \n Sub Main(args As String(), f As FileMode) \n End Sub \n End Module"))
        End Sub

        <WorkItem(540519)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestAddWithExistingConflictWithDifferentArity()
            Test(
NewLines("Imports System.Collections.Generic \n Namespace NS \n Class Foo \n Function Test() As [|IDictionary|] \n End Function \n End Class \n End Namespace"),
NewLines("Imports System.Collections \n Imports System.Collections.Generic \n Namespace NS \n Class Foo \n Function Test() As IDictionary \n End Function \n End Class \n End Namespace"))
        End Sub

        <WorkItem(540673)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestImportNamespace()
            Test(
NewLines("Class FOo \n Sub bar() \n Dim q As [|innernamespace|].someClass \n End Sub \n End Class \n Namespace SomeNamespace \n Namespace InnerNamespace \n Class SomeClass \n End Class \n End Namespace \n End Namespace"),
NewLines("Imports SomeNamespace \n Class FOo \n Sub bar() \n Dim q As InnerNamespace.SomeClass \n End Sub \n End Class \n Namespace SomeNamespace \n Namespace InnerNamespace \n Class SomeClass \n End Class \n End Namespace \n End Namespace"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestCaseSensitivity2()
            Test(
NewLines("Class FOo \n Sub bar() \n Dim q As [|innernamespace|].someClass \n End Sub \n End Class \n Namespace SomeNamespace \n Namespace InnerNamespace \n Class SomeClass \n End Class \n End Namespace \n End Namespace"),
NewLines("Imports SomeNamespace \n Class FOo \n Sub bar() \n Dim q As InnerNamespace.SomeClass \n End Sub \n End Class \n Namespace SomeNamespace \n Namespace InnerNamespace \n Class SomeClass \n End Class \n End Namespace \n End Namespace"))
        End Sub

        <WorkItem(540745)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestCaseSensitivity3()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As [|foo|] \n End Sub \n End Module \n Namespace OUTER \n Namespace INNER \n Friend Class FOO \n End Class \n End Namespace \n End Namespace"),
NewLines("Imports OUTER.INNER \n Module Program \n Sub Main(args As String()) \n Dim x As FOO \n End Sub \n End Module \n Namespace OUTER \n Namespace INNER \n Friend Class FOO \n End Class \n End Namespace \n End Namespace"))
        End Sub

        <WorkItem(541746)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub AddBlankLineAfterLastImports()
            Test(
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestSimpleWhereClause()
            Test(
NewLines("Class Program \n Public Sub Linq1() \n Dim numbers() As Integer = New Integer(9) {5, 4, 1, 3, 9, 8, 6, 7, 2, 0} \n Dim lowNums = [|From n In numbers _ \n Where n < 5 _ \n Select n|] \n End Sub \n End Class"),
NewLines("Imports System.Linq \n Class Program \n Public Sub Linq1() \n Dim numbers() As Integer = New Integer(9) {5, 4, 1, 3, 9, 8, 6, 7, 2, 0} \n Dim lowNums = From n In numbers _ \n Where n < 5 _ \n Select n \n End Sub \n End Class"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestAggregateClause()
            Test(
NewLines("Imports System.Collections.Generic \n Class Program \n Public Sub Linq1() \n Dim numbers() As Integer = New Integer(9) {5, 4, 1, 3, 9, 8, 6, 7, 2, 0} \n Dim greaterNums = [|Aggregate n In numbers \n Into greaterThan5 = All(n > 5)|] \n End Sub \n End Class"),
NewLines("Imports System.Collections.Generic \n Imports System.Linq \n Class Program \n Public Sub Linq1() \n Dim numbers() As Integer = New Integer(9) {5, 4, 1, 3, 9, 8, 6, 7, 2, 0} \n Dim greaterNums = Aggregate n In numbers \n Into greaterThan5 = All(n > 5) \n End Sub \n End Class"))
        End Sub

        <WorkItem(543107)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestNoCrashOnMissingLeftSide()
            TestMissing(
NewLines("Imports System \n Class C1 \n Sub foo() \n Dim s = .[|first|] \n End Sub \n End Class"))
        End Sub

        <WorkItem(544335)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestOnCallWithoutArgumentList()
            Test(
NewLines("Module Program \n Sub Main(args As String()) \n [|File|] \n End Sub \n End Module"),
NewLines("Imports System.IO \n Module Program \n Sub Main(args As String()) \n File \n End Sub \n End Module"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestAddToVisibleRegion()
            Test(
NewLines("#ExternalSource (""Default.aspx"", 1) \n Imports System \n #End ExternalSource \n #ExternalSource (""Default.aspx"", 2) \n Class C \n Sub Foo() \n Dim x As New [|StreamReader|] \n #End ExternalSource \n End Sub \n End Class"),
NewLines("#ExternalSource (""Default.aspx"", 1) \n Imports System \n Imports System.IO \n #End ExternalSource \n #ExternalSource (""Default.aspx"", 2) \n Class C \n Sub Foo() \n Dim x As New [|StreamReader|] \n #End ExternalSource \n End Sub \n End Class"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestDoNotAddIntoHiddenRegion()
            TestMissing(
NewLines("Imports System \n #ExternalSource (""Default.aspx"", 2) \n Class C \n Sub Foo() \n Dim x As New [|StreamReader|] \n #End ExternalSource \n End Sub \n End Class"))
        End Sub

        <WorkItem(546369)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestFormattingAfterImports()
            Test(
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
        End Sub

        <WorkItem(775448)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub ShouldTriggerOnBC32045()
            ' BC32045: 'A' has no type parameters and so cannot have type arguments.
            Test(
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
        End Sub

        <WorkItem(867425)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestUnknownIdentifierInModule()
            Test(
    NewLines("Module Foo \n Sub Bar(args As String()) \n Dim a = From f In args \n Let ext = [|Path|] \n End Sub \n End Module"),
    NewLines("Imports System.IO \n Module Foo \n Sub Bar(args As String()) \n Dim a = From f In args \n Let ext = Path \n End Sub \n End Module"))
        End Sub

        <WorkItem(872908)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestConflictedGenericName()
            Test(
    NewLines("Module Foo \n Sub Bar(args As String()) \n Dim a = From f In args \n Let ext = [|Path|] \n End Sub \n End Module"),
    NewLines("Imports System.IO \n Module Foo \n Sub Bar(args As String()) \n Dim a = From f In args \n Let ext = Path \n End Sub \n End Module"))
        End Sub

        <WorkItem(838253)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestConflictedInaccessibleType()
            Test(
    NewLines("Imports System.Diagnostics \n Namespace N \n Public Class Log \n End Class \n End Namespace \n Class C \n Public Function Foo() \n [|Log|] \n End Function \n End Class"),
    NewLines("Imports System.Diagnostics \n Imports N \n Namespace N \n Public Class Log \n End Class \n End Namespace \n Class C \n Public Function Foo() \n Log \n End Function \n End Class"), 1)
        End Sub

        <WorkItem(858085)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestConflictedAttributeName()
            Test(
    NewLines("<[|Description|]> Public Class Description \n End Class"),
    NewLines("Imports System.ComponentModel \n <[|Description|]> Public Class Description \n End Class"))
        End Sub

        <WorkItem(772321)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestExtensionWithThePresenceOfTheSameNameNonExtensionMethod()
            Test(
    NewLines("Option Strict On \n Imports System.Runtime.CompilerServices \n Namespace NS1 \n Class Program \n Sub main() \n Dim c = New C() \n [|c.Foo(4)|] \n End Sub \n End Class \n Class C \n Sub Foo(ByVal m As String) \n End Sub \n End Class \n End Namespace \n Namespace NS2 \n Module A \n <Extension()> \n Sub Foo(ByVal ec As NS1.C, ByVal n As Integer) \n End Sub \n End Module \n End Namespace "),
    NewLines("Option Strict On \n Imports System.Runtime.CompilerServices \n Imports NS2 \n Namespace NS1 \n Class Program \n Sub main() \n Dim c = New C() \n c.Foo(4) \n End Sub \n End Class \n Class C \n Sub Foo(ByVal m As String) \n End Sub \n End Class \n End Namespace \n Namespace NS2 \n Module A \n <Extension()> \n Sub Foo(ByVal ec As NS1.C, ByVal n As Integer) \n End Sub \n End Module \n End Namespace "))
        End Sub

        <WorkItem(772321)>
        <WorkItem(920398)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestExtensionWithThePresenceOfTheSameNameNonExtensionPrivateMethod()
            Test(
    NewLines("Option Strict On \n Imports System.Runtime.CompilerServices \n Namespace NS1 \n Class Program \n Sub main() \n Dim c = New C() \n [|c.Foo(4)|] \n End Sub \n End Class \n Class C \n Private Sub Foo(ByVal m As Integer) \n End Sub \n End Class \n End Namespace \n Namespace NS2 \n Module A \n <Extension()> \n Sub Foo(ByVal ec As NS1.C, ByVal n As Integer) \n End Sub \n End Module \n End Namespace "),
    NewLines("Option Strict On \n Imports System.Runtime.CompilerServices \n Imports NS2 \n Namespace NS1 \n Class Program \n Sub main() \n Dim c = New C() \n c.Foo(4) \n End Sub \n End Class \n Class C \n Private Sub Foo(ByVal m As Integer) \n End Sub \n End Class \n End Namespace \n Namespace NS2 \n Module A \n <Extension()> \n Sub Foo(ByVal ec As NS1.C, ByVal n As Integer) \n End Sub \n End Module \n End Namespace "))
        End Sub

        <WorkItem(772321)>
        <WorkItem(920398)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestExtensionWithThePresenceOfTheSameNameExtensionPrivateMethod()
            Test(
    NewLines("Option Strict On \n Imports System.Runtime.CompilerServices \n Imports NS2 \n Namespace NS1 \n Class Program \n Sub main() \n Dim c = New C() \n [|c.Foo(4)|] \n End Sub \n End Class \n Class C \n Sub Foo(ByVal m As String) \n End Sub \n End Class \n End Namespace \n Namespace NS2 \n Module A \n <Extension()> \n Private Sub Foo(ByVal ec As NS1.C, ByVal n As Integer) \n End Sub \n End Module \n End Namespace \n \n Namespace NS3 \n Module A \n <Extension()> \n Sub Foo(ByVal ec As NS1.C, ByVal n As Integer) \n End Sub \n End Module \n End Namespace "),
    NewLines("Option Strict On \n Imports System.Runtime.CompilerServices \n Imports NS2 \n Imports NS3 \n Namespace NS1 \n Class Program \n Sub main() \n Dim c = New C() \n [|c.Foo(4)|] \n End Sub \n End Class \n Class C \n Sub Foo(ByVal m As String) \n End Sub \n End Class \n End Namespace \n Namespace NS2 \n Module A \n <Extension()> \n Private Sub Foo(ByVal ec As NS1.C, ByVal n As Integer) \n End Sub \n End Module \n End Namespace \n \n Namespace NS3 \n Module A \n <Extension()> \n Sub Foo(ByVal ec As NS1.C, ByVal n As Integer) \n End Sub \n End Module \n End Namespace "))
        End Sub

        <WorkItem(916368)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestAddImportForCref()
            Dim initialText As String = NewLines("''' <summary>\n''' This is just like <see cref=[|""INotifyPropertyChanged""|]/>, but this one is mine.\n''' </summary>\nInterface IMyInterface\nEnd Interface")
            Dim expectedText As String = NewLines("Imports System.ComponentModel\n''' <summary>\n''' This is just like <see cref=""INotifyPropertyChanged""/>, but this one is mine.\n''' </summary>\nInterface IMyInterface\nEnd Interface")
            Dim options = New VisualBasicParseOptions(documentationMode:=DocumentationMode.Diagnose)
            Test(
                initialText,
                expectedText,
                parseOptions:=options)
        End Sub

        <WorkItem(916368)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestAddImportForCref2()
            Dim initialText As String = NewLines("''' <summary>\n''' This is just like <see cref=[|""INotifyPropertyChanged.PropertyChanged""|]/>, but this one is mine.\n''' </summary>\nInterface IMyInterface\nEnd Interface")
            Dim expectedText As String = NewLines("Imports System.ComponentModel\n''' <summary>\n''' This is just like <see cref=""INotifyPropertyChanged.PropertyChanged""/>, but this one is mine.\n''' </summary>\nInterface IMyInterface\nEnd Interface")
            Dim options = New VisualBasicParseOptions(documentationMode:=DocumentationMode.Diagnose)
            Test(
                initialText,
                expectedText,
                parseOptions:=options)
        End Sub

        <WorkItem(916368)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestAddImportForCref3()
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
            Test(
                initialText,
                expectedText,
                parseOptions:=options)
        End Sub

        <WorkItem(916368)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestAddImportForCref4()
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
            Test(
                initialText,
                expectedText,
                parseOptions:=options)
        End Sub

        <WorkItem(916368)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestAddImportForCref5()
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
            Test(
                initialText,
                expectedText,
                parseOptions:=options)
        End Sub

        <WorkItem(772321)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestExtensionMethodNoMemberAccessOverload()
            Test(
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
        End Sub

        <WorkItem(772321)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestExtensionMethodNoMemberAccess()
            Test(
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
        End Sub

        <WorkItem(1003618)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub AddImportsTypeParsedAsNamespace()
            Test(
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
        End Sub

        <WorkItem(773614)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub AddImportsForTypeAttribute()
            Test(
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
        End Sub

        <WorkItem(773614)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub AddImportsForTypeAttributeMultipleNestedClasses()
            Test(
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
        End Sub

        <WorkItem(773614)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub AddImportsForTypeAttributePartiallyQualified()
            Test(
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
        End Sub

        <WorkItem(1064815)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestConditionalAccessExtensionMethod()
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
            Test(initial, expected, compareTokens:=False)
        End Sub

        <WorkItem(1064815)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestConditionalAccessExtensionMethod2()
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
            Test(initial, expected, compareTokens:=False)
        End Sub

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestAddUsingInDirective()
            Test(
NewLines("#Const Debug\nImports System\nImports System.Collections.Generic\n#If Debug Then\nImports System.Linq\n#End If\nModule Program\n    Sub Main(args As String()) \n        Dim a = [|File|].OpenRead("""") \n    End Sub \n End Module"),
NewLines("#Const Debug\nImports System\nImports System.Collections.Generic\nImports System.IO\n#If Debug Then\nImports System.Linq\n#End If\nModule Program\n    Sub Main(args As String())\n        Dim a = File.OpenRead("""")\n    End Sub\nEnd Module"),
compareTokens:=False)
        End Sub

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestAddUsingInDirective2()
            Test(
NewLines("#Const Debug\n#If Debug Then\nImports System\n#End If\nImports System.Collections.Generic\nImports System.Linq\n Module Program\n    Sub Main(args As String())\n        Dim a = [|File|].OpenRead("""")\n End Sub\n End Module"),
NewLines("#Const Debug\n#If Debug Then\nImports System\n#End If\nImports System.Collections.Generic\nImports System.IO\nImports System.Linq\nModule Program\n    Sub Main(args As String())\n        Dim a = File.OpenRead("""")\n    End Sub\nEnd Module"),
compareTokens:=False)
        End Sub

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestAddUsingInDirective3()
            Test(
NewLines("#Const Debug\n#If Debug Then\nImports System\nImports System.Collections.Generic\nImports System.Linq\n#End If\nModule Program\n    Sub Main(args As String())\n        Dim a = [|File|].OpenRead("""") \n End Sub \n End Module"),
NewLines("#Const Debug\n#If Debug Then\nImports System\nImports System.Collections.Generic\nImports System.IO\nImports System.Linq\n#End If\nModule Program\n    Sub Main(args As String())\n        Dim a = File.OpenRead("""")\n    End Sub\nEnd Module"),
compareTokens:=False)
        End Sub

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestInaccessibleExtensionMethod()
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
            Test(initial, expected, compareTokens:=False)
        End Sub

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestInaccessibleExtensionMethod2()
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
            TestMissing(initial)
        End Sub

        <WorkItem(269)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestAddImportForAddExtentionMethod()
            Test(
NewLines("Imports System \n Imports System.Collections \n Imports System.Runtime.CompilerServices \n Class X \n Implements IEnumerable \n Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator \n Dim a = New X [|From {1}|] \n Return a.GetEnumerator() \n End Function \n End Class \n Namespace Ext \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Integer) \n End Sub \n End Module \n End Namespace"),
NewLines("Imports System \n Imports System.Collections \n Imports System.Runtime.CompilerServices \n Imports Ext \n Class X \n Implements IEnumerable \n Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator \n Dim a = New X From {1} \n Return a.GetEnumerator() \n End Function \n End Class \n Namespace Ext \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Integer) \n End Sub \n End Module \n End Namespace"),
parseOptions:=Nothing)
        End Sub

        <WorkItem(269)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestAddImportForAddExtentionMethod2()
            Test(
NewLines("Imports System \n Imports System.Collections \n Imports System.Runtime.CompilerServices \n Class X \n Implements IEnumerable \n Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator \n Dim a = New X [|From {1, 2, 3}|] \n Return a.GetEnumerator() \n End Function \n End Class \n Namespace Ext \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Integer) \n End Sub \n End Module \n End Namespace"),
NewLines("Imports System \n Imports System.Collections \n Imports System.Runtime.CompilerServices \n Imports Ext \n Class X \n Implements IEnumerable \n Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator \n Dim a = New X From {1, 2, 3} \n Return a.GetEnumerator() \n End Function \n End Class \n Namespace Ext \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Integer) \n End Sub \n End Module \n End Namespace"),
parseOptions:=Nothing)
        End Sub

        <WorkItem(269)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestAddImportForAddExtentionMethod3()
            Test(
NewLines("Imports System \n Imports System.Collections \n Imports System.Runtime.CompilerServices \n Class X \n Implements IEnumerable \n Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator \n Dim a = New X [|From {{1, 2, 3}, {4, 5, 6}, {7, 8, 9}}|] \n Return a.GetEnumerator() \n End Function \n End Class \n Namespace Ext \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Integer) \n End Sub \n End Module \n End Namespace"),
NewLines("Imports System \n Imports System.Collections \n Imports System.Runtime.CompilerServices \n Imports Ext \n Class X \n Implements IEnumerable \n Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator \n Dim a = New X From {{1, 2, 3}, {4, 5, 6}, {7, 8, 9}} \n Return a.GetEnumerator() \n End Function \n End Class \n Namespace Ext \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Integer) \n End Sub \n End Module \n End Namespace"),
parseOptions:=Nothing)
        End Sub

        <WorkItem(269)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestAddImportForAddExtentionMethod4()
            Test(
NewLines("Imports System \n Imports System.Collections \n Imports System.Runtime.CompilerServices \n Class X \n Implements IEnumerable \n Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator \n Dim a = New X [|From {{1, 2, 3}, {""Four"", ""Five"", ""Six""}, {7, 8, 9}}|] \n Return a.GetEnumerator() \n End Function \n End Class \n Namespace Ext \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Integer) \n End Sub \n End Module \n End Namespace"),
NewLines("Imports System \n Imports System.Collections \n Imports System.Runtime.CompilerServices \n Imports Ext \n Class X \n Implements IEnumerable \n Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator \n Dim a = New X From {{1, 2, 3}, {""Four"", ""Five"", ""Six""}, {7, 8, 9}} \n Return a.GetEnumerator() \n End Function \n End Class \n Namespace Ext \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Integer) \n End Sub \n End Module \n End Namespace"),
parseOptions:=Nothing)
        End Sub

        <WorkItem(269)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestAddImportForAddExtentionMethod5()
            Test(
NewLines("Imports System \n Imports System.Collections \n Imports System.Runtime.CompilerServices \n Class X \n Implements IEnumerable \n Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator \n Dim a = New X [|From {""This""}|] \n Return a.GetEnumerator() \n End Function \n End Class \n Namespace Ext \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Integer) \n End Sub \n End Module \n End Namespace"),
NewLines("Imports System \n Imports System.Collections \n Imports System.Runtime.CompilerServices \n Imports Ext \n Class X \n Implements IEnumerable \n Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator \n Dim a = New X From {""This""} \n Return a.GetEnumerator() \n End Function \n End Class \n Namespace Ext \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Integer) \n End Sub \n End Module \n End Namespace"),
parseOptions:=Nothing)
        End Sub

        <WorkItem(269)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestAddImportForAddExtentionMethod6()
            Test(
NewLines("Imports System \n Imports System.Collections \n Imports System.Runtime.CompilerServices \n Class X \n Implements IEnumerable \n Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator \n Dim a = New X [|From {""This""}|] \n Return a.GetEnumerator() \n End Function \n End Class \n Namespace Ext \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Integer) \n End Sub \n End Module \n End Namespace \n Namespace Ext2 \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Object()) \n End Sub \n End Module \n End Namespace"),
NewLines("Imports System \n Imports System.Collections \n Imports System.Runtime.CompilerServices \n Imports Ext \n Class X \n Implements IEnumerable \n Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator \n Dim a = New X From {""This""} \n Return a.GetEnumerator() \n End Function \n End Class \n Namespace Ext \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Integer) \n End Sub \n End Module \n End Namespace \n Namespace Ext2 \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Object()) \n End Sub \n End Module \n End Namespace"),
parseOptions:=Nothing)
        End Sub

        <WorkItem(269)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestAddImportForAddExtentionMethod7()
            Test(
NewLines("Imports System \n Imports System.Collections \n Imports System.Runtime.CompilerServices \n Class X \n Implements IEnumerable \n Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator \n Dim a = New X [|From {""This""}|] \n Return a.GetEnumerator() \n End Function \n End Class \n Namespace Ext \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Integer) \n End Sub \n End Module \n End Namespace \n Namespace Ext2 \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Object()) \n End Sub \n End Module \n End Namespace"),
NewLines("Imports System \n Imports System.Collections \n Imports System.Runtime.CompilerServices \n Imports Ext2 \n Class X \n Implements IEnumerable \n Public Function GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator \n Dim a = New X From {""This""} \n Return a.GetEnumerator() \n End Function \n End Class \n Namespace Ext \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Integer) \n End Sub \n End Module \n End Namespace \n Namespace Ext2 \n Module Extensions \n <Extension> \n Public Sub Add(x As X, i As Object()) \n End Sub \n End Module \n End Namespace"),
index:=1,
parseOptions:=Nothing)
        End Sub

        <WorkItem(935, "https://github.com/dotnet/roslyn/issues/935")>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestAddUsingWithOtherExtensionsInScope()
            Test(
NewLines("Imports System.Linq \n Imports System.Runtime.CompilerServices \n Module Program \n Sub Main(args As String()) \n Dim i = [|0.All|]() \n End Sub \n End Module \n Namespace X \n Module E \n <Extension> \n Public Function All(a As Integer) As Integer \n Return a \n End Function \n End Module \n End Namespace"),
NewLines("Imports System.Linq \n Imports System.Runtime.CompilerServices \n Imports X \n Module Program \n Sub Main(args As String()) \n Dim i = 0.All() \n End Sub \n End Module \n Namespace X \n Module E \n <Extension> \n Public Function All(a As Integer) As Integer \n Return a \n End Function \n End Module \n End Namespace"))
        End Sub

        <WorkItem(935, "https://github.com/dotnet/roslyn/issues/935")>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestAddUsingWithOtherExtensionsInScope2()
            Test(
NewLines("Imports System.Linq \n Imports System.Runtime.CompilerServices \n Module Program \n Sub Main(args As String()) \n Dim a = New Integer? \n Dim i = a?[|.All|]() \n End Sub \n End Module \n Namespace X \n Module E \n <Extension> \n Public Function All(a As Integer?) As Integer \n Return 0 \n End Function \n End Module \n End Namespace"),
NewLines("Imports System.Linq \n Imports System.Runtime.CompilerServices \n Imports X \n Module Program \n Sub Main(args As String()) \n Dim a = New Integer? \n Dim i = a?.All() \n End Sub \n End Module \n Namespace X \n Module E \n <Extension> \n Public Function All(a As Integer?) As Integer \n Return 0 \n End Function \n End Module \n End Namespace"))
        End Sub

        <WorkItem(562, "https://github.com/dotnet/roslyn/issues/562")>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestAddUsingWithOtherExtensionsInScope3()
            Test(
NewLines("Imports System.Runtime.CompilerServices \n Imports X \n Module Program \n Sub Main(args As String()) \n Dim a = 0 \n Dim i = [|a.All|](0) \n End Sub \n End Module \n Namespace X \n Module E \n <Extension> \n Public Function All(a As Integer) As Integer \n Return a \n End Function \n End Module \n End Namespace \n Namespace Y \n Module E \n <Extension> \n Public Function All(a As Integer, v As Integer) As Integer \n Return a \n End Function \n End Module \n End Namespace"),
NewLines("Imports System.Runtime.CompilerServices \n Imports X \n Imports Y \n Module Program \n Sub Main(args As String()) \n Dim a = 0 \n Dim i = a.All(0) \n End Sub \n End Module \n Namespace X \n Module E \n <Extension> \n Public Function All(a As Integer) As Integer \n Return a \n End Function \n End Module \n End Namespace \n Namespace Y \n Module E \n <Extension> \n Public Function All(a As Integer, v As Integer) As Integer \n Return a \n End Function \n End Module \n End Namespace"))
        End Sub

        <WorkItem(562, "https://github.com/dotnet/roslyn/issues/562")>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
        Public Sub TestAddUsingWithOtherExtensionsInScope4()
            Test(
NewLines("Imports System.Runtime.CompilerServices \n Imports X \n Module Program \n Sub Main(args As String()) \n Dim a = New Integer? \n Dim i = a?[|.All|](0) \n End Sub \n End Module \n Namespace X \n Module E \n <Extension> \n Public Function All(a As Integer?) As Integer \n Return 0 \n End Function \n End Module \n End Namespace \n Namespace Y \n Module E \n <Extension> \n Public Function All(a As Integer?, v As Integer) As Integer \n Return 0 \n End Function \n End Module \n End Namespace"),
NewLines("Imports System.Runtime.CompilerServices \n Imports X \n Imports Y \n Module Program \n Sub Main(args As String()) \n Dim a = New Integer? \n Dim i = a?.All(0) \n End Sub \n End Module \n Namespace X \n Module E \n <Extension> \n Public Function All(a As Integer?) As Integer \n Return 0 \n End Function \n End Module \n End Namespace \n Namespace Y \n Module E \n <Extension> \n Public Function All(a As Integer?, v As Integer) As Integer \n Return 0 \n End Function \n End Module \n End Namespace"))
        End Sub

        Public Class AddImportTestsWithAddImportDiagnosticProvider
            Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

            Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
                Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(
                    New VisualBasicUnboundIdentifiersDiagnosticAnalyzer(),
                    New VisualBasicAddImportCodeFixProvider())
            End Function

            <WorkItem(829970)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
            Public Sub TestUnknownIdentifierInAttributeSyntaxWithoutTarget()
                Test(
    NewLines("Class Class1 \n <[|Extension|]> \n End Class"),
    NewLines("Imports System.Runtime.CompilerServices \n Class Class1 \n <Extension> \n End Class"))
            End Sub

            <WorkItem(829970)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
            Public Sub TestUnknownIdentifierGenericName()
                Test(
    NewLines("Class C \n    Inherits Attribute \n    Public Sub New(x As System.Type) \n    End Sub \n    <C([|List(Of Integer)|])> \n End Class"),
    NewLines("Imports System.Collections.Generic \n Class C \n    Inherits Attribute \n    Public Sub New(x As System.Type) \n    End Sub \n    <C(List(Of Integer))> \n End Class"))
            End Sub

            <WorkItem(829970)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
            Public Sub TestUnknownIdentifierAddNamespaceImport()
                Test(
    NewLines("Class Class1 \n <[|Tasks.Task|]> \n End Class"),
    NewLines("Imports System.Threading \n Class Class1 \n <Tasks.Task> \n End Class"))
            End Sub

            <WorkItem(829970)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
            Public Sub TestUnknownAttributeInModule()
                Test(
    NewLines("Module Foo \n <[|Extension|]> \n End Module"),
    NewLines("Imports System.Runtime.CompilerServices \n Module Foo \n <Extension> \n End Module"))

                Test(
    NewLines("Module Foo \n <[|Extension()|]> \n End Module"),
    NewLines("Imports System.Runtime.CompilerServices \n Module Foo \n <Extension()> \n End Module"))
            End Sub

            <WorkItem(938296)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
            Public Sub TestNullParentInNode()
                TestMissing(
"Imports System.Collections.Generic

Class MultiDictionary(Of K, V)
    Inherits Dictionary(Of K, HashSet(Of V))

    Sub M()
        Dim hs = New HashSet(Of V)([|Comparer|])
    End Sub
End Class")
            End Sub

            <WorkItem(1744, "https://github.com/dotnet/roslyn/issues/1744")>
            <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
            Public Sub TestImportIncompleteSub()
                Test(
    NewLines("Class A \n Dim a As Action = Sub() \n Try \n Catch ex As [|TestException|] \n End Sub \n End Class \n Namespace T \n Class TestException \n Inherits Exception \n End Class \n End Namespace"),
    NewLines("Imports T \n Class A \n Dim a As Action = Sub() \n Try \n Catch ex As TestException \n End Sub \n End Class \n Namespace T \n Class TestException \n Inherits Exception \n End Class \n End Namespace"))
            End Sub

            <WorkItem(1239, "https://github.com/dotnet/roslyn/issues/1239")>
            <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)>
            Public Sub TestImportIncompleteSub2()
                Test(
    NewLines("Imports System.Linq \n Namespace X \n Class Test \n End Class \n End Namespace \n Class C \n Sub New() \n Dim s As Action = Sub() \n Dim a = New [|Test|]()"),
    NewLines("Imports System.Linq \n Imports X \n Namespace X \n Class Test \n End Class \n End Namespace \n Class C \n Sub New() \n Dim s As Action = Sub() \n Dim a = New Test()"))
            End Sub
        End Class
    End Class
End Namespace
