' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.SimplifyTypeNames

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.SimplifyTypeNames
    Partial Public Class SimplifyTypeNamesTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return New Tuple(Of DiagnosticAnalyzer, CodeFixProvider)(New VisualBasicSimplifyTypeNamesDiagnosticAnalyzer(), New SimplifyTypeNamesCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestGenericNames()
            Dim source =
        <Code>
Imports System
Imports System.Collections.Generic
Imports System.Linq

Class C
    Shared Sub F(Of T)(x As Func(Of Integer, T))

    End Sub

    Shared Sub main()
        [|F(Of Integer)|](Function(a) a)
    End Sub
End Class
</Code>
            Dim expected =
        <Code>
Imports System
Imports System.Collections.Generic
Imports System.Linq

Class C
    Shared Sub F(Of T)(x As Func(Of Integer, T))

    End Sub

    Shared Sub main()
        F(Function(a) a)
    End Sub
End Class
</Code>
            Test(source.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestArgument()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As [|System.String|]()) \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n End Sub \n End Module"),
index:=0)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestAliasWithMemberAccess()
            Test(
NewLines("Imports Foo = System.Int32 \n Module Program \n Sub Main(args As String()) \n Dim x = [|System.Int32|].MaxValue \n End Sub \n End Module"),
NewLines("Imports Foo = System.Int32 \n Module Program \n Sub Main(args As String()) \n Dim x = Foo.MaxValue \n End Sub \n End Module"),
index:=0)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestWithCursorAtBeginning()
            Test(
NewLines("Imports System.IO \n Module Program \n Sub Main(args As String()) \n Dim x As [|System.IO.File|] \n End Sub \n End Module"),
NewLines("Imports System.IO \n Module Program \n Sub Main(args As String()) \n Dim x As File \n End Sub \n End Module"),
index:=0)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestMinimalSimplifyOnNestedNamespaces()
            Dim source =
NewLines("Imports Outer \n Namespace Outer \n Namespace Inner \n Class Foo \n End Class \n End Namespace \n End Namespace \n Module Program \n Sub Main(args As String()) \n Dim x As [|Outer.Inner.Foo|] \n End Sub \n End Module")

            Test(source,
NewLines("Imports Outer \n Namespace Outer \n Namespace Inner \n Class Foo \n End Class \n End Namespace \n End Namespace \n Module Program \n Sub Main(args As String()) \n Dim x As Inner.Foo \n End Sub \n End Module"),
index:=0)
            TestActionCount(source, 1)
        End Sub

        <WorkItem(540567)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestMinimalSimplifyOnNestedNamespacesFromMetadataAlias()
            Test(
NewLines("Imports A1 = System.IO.File \n Class Foo \n Dim x As [|System.IO.File|] \n End Class"),
NewLines("Imports A1 = System.IO.File \n Class Foo \n Dim x As A1 \n End Class"),
index:=0)
        End Sub

        <WorkItem(540567)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestMinimalSimplifyOnNestedNamespacesFromMetadata()
            Test(
NewLines("Imports System \n Class Foo \n Dim x As [|System.IO.File|] \n End Class"),
NewLines("Imports System \n Class Foo \n Dim x As IO.File \n End Class"),
index:=0)
        End Sub

        <WorkItem(540569)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestFixAllOccurrences()
            Dim actionId = SimplifyTypeNamesCodeFixProvider.GetCodeActionId(IDEDiagnosticIds.SimplifyNamesDiagnosticId, "NS1.SomeClass")
            Test(
NewLines("Imports NS1 \n Namespace NS1 \n Class SomeClass \n End Class \n End Namespace \n Class Foo \n Dim x As {|FixAllInDocument:NS1.SomeClass|} \n Dim y As NS1.SomeClass \n End Class"),
NewLines("Imports NS1 \n Namespace NS1 \n Class SomeClass \n End Class \n End Namespace \n Class Foo \n Dim x As SomeClass \n Dim y As SomeClass \n End Class"),
fixAllActionEquivalenceKey:=actionId)
        End Sub

        <WorkItem(578686)>
        <Fact(Skip:="1033012"), Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Sub TestFixAllOccurrencesForAliases()
            Test(
NewLines("Imports System \n Imports foo = C.D \n Imports bar = A.B \n Namespace C \n Class D \n End Class \n End Namespace \n Module Program Sub Main(args As String()) \n Dim Local1 = [|New A.B().prop|] \n End Sub \n End Module \n Namespace A \n Class B \n Public Property prop As C.D \n End Class \n End Namespace"),
NewLines("Imports System \n Imports foo = C.D \n Imports bar = A.B \n Namespace C \n Class D \n End Class \n End Namespace \n Module Program Sub Main(args As String()) \n Dim Local1 = New bar().prop \n End Sub \n End Module \n Namespace A \n Class B \n Public Property prop As foo \n End Class \n End Namespace"),
index:=1)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestSimplifyFromReference()
            Test(
        NewLines("Imports System.Threading \n Class Class1 \n Dim v As [|System.Threading.Thread|] \n End Class"),
        NewLines("Imports System.Threading \n Class Class1 \n Dim v As Thread \n End Class"),
        index:=0)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestGenericClassDefinitionAsClause()
            Test(
        NewLines("Imports SomeNamespace \n Namespace SomeNamespace \n Class Base \n End Class \n End Namespace \n Class SomeClass(Of x As [|SomeNamespace.Base|]) \n End Class"),
        NewLines("Imports SomeNamespace \n Namespace SomeNamespace \n Class Base \n End Class \n End Namespace \n Class SomeClass(Of x As Base) \n End Class"),
        index:=0)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestGenericClassInstantiationOfClause()
            Test(
        NewLines("Imports SomeNamespace \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class GenericClass(Of T) \n End Class \n Class Foo \n Sub Method1() \n Dim q As GenericClass(Of [|SomeNamespace.SomeClass|]) \n End Sub \n End Class"),
        NewLines("Imports SomeNamespace \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class GenericClass(Of T) \n End Class \n Class Foo \n Sub Method1() \n Dim q As GenericClass(Of SomeClass) \n End Sub \n End Class"),
        index:=0)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestGenericMethodDefinitionAsClause()
            Test(
        NewLines("Imports SomeNamespace \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class Foo \n Sub Method1(Of T As [|SomeNamespace.SomeClass|]) \n End Sub \n End Class"),
        NewLines("Imports SomeNamespace \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class Foo \n Sub Method1(Of T As SomeClass) \n End Sub \n End Class"),
        index:=0)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestGenericMethodInvocationOfClause()
            Test(
        NewLines("Imports SomeNamespace \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class Foo \n Sub Method1(Of T) \n End Sub \n Sub Method2() \n Method1(Of [|SomeNamespace.SomeClass|]) \n End Sub \n End Class"),
        NewLines("Imports SomeNamespace \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class Foo \n Sub Method1(Of T) \n End Sub \n Sub Method2() \n Method1(Of SomeClass) \n End Sub \n End Class"),
        index:=0)
        End Sub

        <WorkItem(6872, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestAttributeApplication()
            Test(
        NewLines("Imports SomeNamespace \n <[|SomeNamespace.Something|]()> \n Class Foo \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits System.Attribute \n End Class \n End Namespace"),
        NewLines("Imports SomeNamespace \n <Something()> \n Class Foo \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits System.Attribute \n End Class \n End Namespace"),
        index:=0)
        End Sub

        <WorkItem(6872, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestMultipleAttributeApplicationBelow()

            'IMPLEMENT NOT ESCAPE ATTRIBUTE DEPENDENT ON CONTEXT

            Test(
        NewLines("Imports System \n Imports SomeNamespace \n <Existing()> \n <[|SomeNamespace.Something|]()> \n Class Foo \n End Class \n Class ExistingAttribute \n Inherits System.Attribute \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits System.Attribute \n End Class \n End Namespace"),
        NewLines("Imports System \n Imports SomeNamespace \n <Existing()> \n <Something()> \n Class Foo \n End Class \n Class ExistingAttribute \n Inherits System.Attribute \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits System.Attribute \n End Class \n End Namespace"),
        index:=0)
        End Sub

        <WorkItem(6872, "DevDiv_Projects/Roslyn")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestMultipleAttributeApplicationAbove()
            Test(
        NewLines("Imports System \n Imports SomeNamespace \n <[|SomeNamespace.Something|]()> \n <Existing()> \n Class Foo \n End Class \n Class ExistingAttribute \n Inherits System.Attribute \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits System.Attribute \n End Class \n End Namespace"),
        NewLines("Imports System \n Imports SomeNamespace \n <Something()> \n <Existing()> \n Class Foo \n End Class \n Class ExistingAttribute \n Inherits System.Attribute \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits System.Attribute \n End Class \n End Namespace"),
        index:=0)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestSimplifiedLeftmostQualifierIsEscapedWhenMatchesKeyword()
            Test(
        NewLines("Imports Outer \n Class SomeClass \n Dim x As [|Outer.Namespace.Something|] \n End Class \n Namespace Outer \n Namespace [Namespace] \n Class Something \n End Class \n End Namespace \n End Namespace"),
        NewLines("Imports Outer \n Class SomeClass \n Dim x As [Namespace].Something \n End Class \n Namespace Outer \n Namespace [Namespace] \n Class Something \n End Class \n End Namespace \n End Namespace"),
        index:=0)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestTypeNameIsEscapedWhenMatchingKeyword()
            Test(
        NewLines("Imports Outer \n Class SomeClass \n Dim x As [|Outer.Class|] \n End Class \n Namespace Outer \n Class [Class] \n End Class \n End Namespace"),
        NewLines("Imports Outer \n Class SomeClass \n Dim x As [Class] \n End Class \n Namespace Outer \n Class [Class] \n End Class \n End Namespace"),
        index:=0)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestSimplifyNotSuggestedInImportsStatement()
            TestMissing(
        NewLines("[|Imports SomeNamespace \n Imports SomeNamespace.InnerNamespace \n Namespace SomeNamespace \n Namespace InnerNamespace \n Class SomeClass \n End Class \n End Namespace \n End Namespace|]"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestNoSimplifyInGenericAsClauseIfConflictsWithTypeParameterName()
            TestMissing(
        NewLines("[|Imports SomeNamespace \n Class Class1 \n Sub Foo(Of SomeClass)(x As SomeNamespace.SomeClass) \n End Sub \n End Class \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace|]"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestSimplifyNotOfferedIfSimplifyingWouldCauseAmbiguity()
            TestMissing(
        NewLines("[|Imports SomeNamespace \n Class SomeClass \n End Class \n Class Class1 \n Dim x As SomeNamespace.SomeClass \n End Class \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace|]"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestSimplifyInGenericAsClauseIfNoConflictWithTypeParameterName()
            Test(
        NewLines("Imports SomeNamespace \n Class Class1 \n Sub Foo(Of T)(x As [|SomeNamespace.SomeClass|]) \n End Sub \n End Class \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace"),
        NewLines("Imports SomeNamespace \n Class Class1 \n Sub Foo(Of T)(x As SomeClass) \n End Sub \n End Class \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace"),
        index:=0)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestCaseInsensitivity()
            Test(
        NewLines("Imports SomeNamespace \n Class Foo \n Dim x As [|SomeNamespace.someclass|] \n End Class \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace"),
        NewLines("Imports SomeNamespace \n Class Foo \n Dim x As someclass \n End Class \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace"),
        index:=0)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestSimplifyGenericTypeWithArguments()
            Dim source =
        NewLines("Imports System.Collections.Generic \n Class Foo \n Function F() As [|System.Collections.Generic.List(Of Integer)|] \n End Function \n End Class")

            Test(source,
        NewLines("Imports System.Collections.Generic \n Class Foo \n Function F() As List(Of Integer) \n End Function \n End Class"),
        index:=0)
            TestActionCount(source, 1)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestParameterType()
            Test(
        NewLines("Imports System.IO \n Module Program \n Sub Main(args As String(), f As [|System.IO.FileMode|]) \n End Sub \n End Module"),
        NewLines("Imports System.IO \n Module Program \n Sub Main(args As String(), f As FileMode) \n End Sub \n End Module"),
        index:=0)
        End Sub

        <WorkItem(540565)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestLocation1()
            Test(
        NewLines("Imports Foo \n Namespace Foo \n Class FooClass \n End Class \n End Namespace \n Module Program \n Sub Main(args As String()) \n Dim x As [|Foo.FooClass|] \n End Sub \n End Module"),
        NewLines("Imports Foo \n Namespace Foo \n Class FooClass \n End Class \n End Namespace \n Module Program \n Sub Main(args As String()) \n Dim x As FooClass \n End Sub \n End Module"),
        index:=0)
        End Sub

        <Fact(Skip:="1033012"), Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Sub TestFixAllFixesUnrelatedTypes()
            Test(
        NewLines("Imports A \n Imports B \n Imports C \n Module Program \n Sub Method1(a As [|A.FooA|], b As B.FooB, c As C.FooC) \n Dim qa As A.FooA \n Dim qb As B.FooB \n Dim qc As C.FooC \n End Sub \n End Module \n Namespace A \n Class FooA \n End Class \n End Namespace \n Namespace B \n Class FooB \n End Class \n End Namespace \n Namespace C \n Class FooC \n End Class \n End Namespace"),
        NewLines("Imports A \n Imports B \n Imports C \n Module Program \n Sub Method1(a As FooA, b As FooB, c As FooC) \n Dim qa As FooA \n Dim qb As FooB \n Dim qc As FooC \n End Sub \n End Module \n Namespace A \n Class FooA \n End Class \n End Namespace \n Namespace B \n Class FooB \n End Class \n End Namespace \n Namespace C \n Class FooC \n End Class \n End Namespace"))
        End Sub

        <Fact(Skip:="1033012"), Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Sub TestSimplifyFixesAllNestedTypeNames()
            Dim source =
        NewLines("Imports A \n Imports B \n Imports C \n Module Program \n Sub Method1(a As [|A.FooA(Of B.FooB(Of C.FooC))|] ) \n End Sub \n End Module \n Namespace A \n Class FooA(Of T) \n End Class \n End Namespace \n Namespace B \n Class FooB(Of T) \n End Class \n End Namespace \n Namespace C \n Class FooC \n End Class \n End Namespace")

            Test(source,
        NewLines("Imports A \n Imports B \n Imports C \n Module Program \n Sub Method1(a As FooA(Of FooB(Of FooC))) \n End Sub \n End Module \n Namespace A \n Class FooA(Of T) \n End Class \n End Namespace \n Namespace B \n Class FooB(Of T) \n End Class \n End Namespace \n Namespace C \n Class FooC \n End Class \n End Namespace"),
        index:=1)
            TestActionCount(source, 1)
        End Sub

        <WorkItem(551040)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestSimplifyNestedType()
            Dim source =
        NewLines("Class Preserve \n Public Class X \n Public Shared Y \n End Class \n End Class \n Class Z(Of T) \n Inherits Preserve \n End Class \n Class M \n Public Shared Sub Main() \n Redim [|Z(Of Integer).X|].Y(1) \n End Function \n End Class")

            Test(source,
        NewLines("Class Preserve \n Public Class X \n Public Shared Y \n End Class \n End Class \n Class Z(Of T) \n Inherits Preserve \n End Class \n Class M \n Public Shared Sub Main() \n Redim [Preserve].X.Y(1) \n End Function \n End Class"),
        index:=0)
            TestActionCount(source, 1)
        End Sub

        <WorkItem(551040)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestSimplifyStaticMemberAccess()
            Dim source =
        NewLines("Class Preserve \n Public Shared Y \n End Class \n Class Z(Of T) \n Inherits Preserve \n End Class \n Class M \n Public Shared Sub Main() \n Redim [|Z(Of Integer).Y(1)|] \n End Function \n End Class")

            Test(source,
        NewLines("Class Preserve \n Public Shared Y \n End Class \n Class Z(Of T) \n Inherits Preserve \n End Class \n Class M \n Public Shared Sub Main() \n Redim [Preserve].Y(1) \n End Function \n End Class"),
        index:=0)
            TestActionCount(source, 1)
        End Sub

        <WorkItem(540398)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestImplementsClause()
            Test(
        NewLines("Imports System \n Class Foo \n Implements IComparable(Of String) \n Public Function CompareTo(other As String) As Integer Implements [|System.IComparable(Of String).CompareTo|] \n Return Nothing \n End Function \n End Class"),
        NewLines("Imports System \n Class Foo \n Implements IComparable(Of String) \n Public Function CompareTo(other As String) As Integer Implements IComparable(Of String).CompareTo \n Return Nothing \n End Function \n End Class"),
        index:=0)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestSimpleArray()
            Test(
        NewLines("Imports System.Collections.Generic \n Namespace N1 \n Class Test \n Private a As [|System.Collections.Generic.List(Of System.String())|] \n End Class \n End Namespace"),
        NewLines("Imports System.Collections.Generic \n Namespace N1 \n Class Test \n Private a As List(Of String()) \n End Class \n End Namespace"),
        index:=0)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestSimpleMultiDimArray()
            Test(
        NewLines("Imports System.Collections.Generic \n Namespace N1 \n Class Test \n Private a As [|System.Collections.Generic.List(Of System.String()(,)(,,,)) |]  \n End Class \n End Namespace"),
        NewLines("Imports System.Collections.Generic \n Namespace N1 \n Class Test \n Private a As List(Of String()(,)(,,,)) \n End Class \n End Namespace"),
        index:=0)
        End Sub

        <WorkItem(542093)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestNoSimplificationOfParenthesizedPredefinedTypes()
            TestMissing(
        NewLines("[|Module M \n Sub Main() \n Dim x = (System.String).Equals("", "") \n End Sub \n End Module|]"))
        End Sub

        <Fact(Skip:="1033012"), Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Sub TestConflicts()
            Test(
        <Text>
Namespace OuterNamespace
    Namespace InnerNamespace

        Class InnerClass1

        End Class
    End Namespace

    Class OuterClass1

        Function M1() As [|OuterNamespace.OuterClass1|]
            Dim c1 As OuterNamespace.OuterClass1
            OuterNamespace.OuterClass1.Equals(1, 2)
        End Function

        Function M2() As OuterNamespace.OuterClass2
            Dim c1 As OuterNamespace.OuterClass2
            OuterNamespace.OuterClass2.Equals(1, 2)
        End Function

        Function M3() As OuterNamespace.InnerNamespace.InnerClass1
            Dim c1 As OuterNamespace.InnerNamespace.InnerClass1
            OuterNamespace.InnerNamespace.InnerClass1.Equals(1, 2)
        End Function

        Function M3() As InnerNamespace.InnerClass1
            Dim c1 As InnerNamespace.InnerClass1
            InnerNamespace.InnerClass1.Equals(1, 2)
        End Function

        Sub OuterClass2()
        End Sub

        Sub InnerClass1()
        End Sub

        Sub InnerNamespace()
        End Sub
    End Class

    Class OuterClass2

        Function M1() As OuterNamespace.OuterClass1
            Dim c1 As OuterNamespace.OuterClass1
            OuterNamespace.OuterClass1.Equals(1, 2)
        End Function

        Function M2() As OuterNamespace.OuterClass2
            Dim c1 As OuterNamespace.OuterClass2
            OuterNamespace.OuterClass2.Equals(1, 2)
        End Function

        Function M3() As OuterNamespace.InnerNamespace.InnerClass1
            Dim c1 As OuterNamespace.InnerNamespace.InnerClass1
            OuterNamespace.InnerNamespace.InnerClass1.Equals(1, 2)
        End Function

        Function M3() As InnerNamespace.InnerClass1
            Dim c1 As InnerNamespace.InnerClass1
            InnerNamespace.InnerClass1.Equals(1, 2)
        End Function
    End Class
End Namespace

</Text>.Value.Replace(vbLf, vbCrLf),
        <Text>
Namespace OuterNamespace
    Namespace InnerNamespace

        Class InnerClass1

        End Class
    End Namespace

    Class OuterClass1

        Function M1() As OuterClass1
            Dim c1 As OuterClass1
            Equals(1, 2)
        End Function

        Function M2() As OuterClass2
            Dim c1 As OuterClass2
            Equals(1, 2)
        End Function

        Function M3() As InnerNamespace.InnerClass1
            Dim c1 As InnerNamespace.InnerClass1
            Equals(1, 2)
        End Function

        Function M3() As InnerNamespace.InnerClass1
            Dim c1 As InnerNamespace.InnerClass1
            InnerNamespace.InnerClass1.Equals(1, 2)
        End Function

        Sub OuterClass2()
        End Sub

        Sub InnerClass1()
        End Sub

        Sub InnerNamespace()
        End Sub
    End Class

    Class OuterClass2

        Function M1() As OuterClass1
            Dim c1 As OuterClass1
            Equals(1, 2)
        End Function

        Function M2() As OuterClass2
            Dim c1 As OuterClass2
            Equals(1, 2)
        End Function

        Function M3() As InnerNamespace.InnerClass1
            Dim c1 As InnerNamespace.InnerClass1
            Equals(1, 2)
        End Function

        Function M3() As InnerNamespace.InnerClass1
            Dim c1 As InnerNamespace.InnerClass1
            Equals(1, 2)
        End Function
    End Class
End Namespace

</Text>.Value.Replace(vbLf, vbCrLf),
        index:=1,
        compareTokens:=False)
        End Sub

        <WorkItem(542138)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestSimplifyModuleWithReservedName()
            Test(
        NewLines("Namespace X \n Module [String] \n Sub Main() \n [|X.String.Main|] \n End Sub \n End Module \n End Namespace"),
        NewLines("Namespace X \n Module [String] \n Sub Main() \n Main \n End Sub \n End Module \n End Namespace"),
        index:=0)
        End Sub

        <WorkItem(542348)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestPreserve1()
            Test(
        NewLines("Module M \n Dim preserve() \n Sub Main() \n ReDim [|M.preserve|](1) \n End Sub \n End Module"),
        NewLines("Module M \n Dim preserve() \n Sub Main() \n ReDim [preserve](1) \n End Sub \n End Module"),
        index:=0)
        End Sub

        <WorkItem(551040)>
        <WorkItem(542348)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestPreserve3()
            Test(
        NewLines("Class Preserve \n Class X \n Public Shared Dim Y \n End Class \n End Class \n Class Z(Of T) \n Inherits Preserve \n End Class \n Module M \n Sub Main() \n ReDim [|Z(Of Integer).X.Y|](1) ' Simplify Z(Of Integer).X \n End Sub \n End Module"),
        NewLines("Class Preserve \n Class X \n Public Shared Dim Y \n End Class \n End Class \n Class Z(Of T) \n Inherits Preserve \n End Class \n Module M \n Sub Main() \n ReDim [Preserve].X.Y(1) ' Simplify Z(Of Integer).X \n End Sub \n End Module"))
        End Sub

        <WorkItem(545603)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestNullableInImports1()
            TestMissing(
        NewLines("Imports [|System.Nullable(Of Integer)|]"))
        End Sub

        <WorkItem(545603)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestNullableInImports2()
            TestMissing(
        NewLines("Imports [|System.Nullable(Of Integer)|]"))
        End Sub

        <WorkItem(545795)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestColorColor1()
            Test(
        NewLines("Namespace N \n Class Color \n Shared Sub Foo() \n End Class \n  \n Class Program \n Shared Property Color As Color \n  \n Shared Sub Main() \n Dim c = [|N.Color.Foo|]() \n End Sub \n End Class \n End Namespace"),
        NewLines("Namespace N \n Class Color \n Shared Sub Foo() \n End Class \n  \n Class Program \n Shared Property Color As Color \n  \n Shared Sub Main() \n Dim c = Color.Foo() \n End Sub \n End Class \n End Namespace"))
        End Sub

        <WorkItem(545795)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestColorColor2()
            Test(
        NewLines("Namespace N \n Class Color \n Shared Sub Foo() \n End Class \n  \n Class Program \n Shared Property Color As Color \n  \n Shared Sub Main() \n Dim c = [|N.Color.Foo|]() \n End Sub \n End Class \n End Namespace"),
        NewLines("Namespace N \n Class Color \n Shared Sub Foo() \n End Class \n  \n Class Program \n Shared Property Color As Color \n  \n Shared Sub Main() \n Dim c = Color.Foo() \n End Sub \n End Class \n End Namespace"))
        End Sub

        <WorkItem(545795)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestColorColor3()
            Test(
        NewLines("Namespace N \n Class Color \n Shared Sub Foo() \n End Class \n  \n Class Program \n Shared Property Color As Color \n  \n Shared Sub Main() \n Dim c = [|N.Color.Foo|]() \n End Sub \n End Class \n End Namespace"),
        NewLines("Namespace N \n Class Color \n Shared Sub Foo() \n End Class \n  \n Class Program \n Shared Property Color As Color \n  \n Shared Sub Main() \n Dim c = Color.Foo() \n End Sub \n End Class \n End Namespace"))
        End Sub

        <WorkItem(546829)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestKeyword1()
            Test(
        NewLines("Module m \n Sub main() \n Dim x = [|m.Equals|](1, 1) \n End Sub \n End Module"),
        NewLines("Module m \n Sub main() \n Dim x = Equals(1, 1) \n End Sub \n End Module"))
        End Sub

        <WorkItem(546844)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestKeyword2()
            Test(
        NewLines("Module M \n Sub main() \n Dim x = [|M.Class|] \n End Sub \n Dim [Class] \n End Module"),
        NewLines("Module M \n Sub main() \n Dim x = [Class] \n End Sub \n Dim [Class] \n End Module"))
        End Sub

        <WorkItem(546907)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestDoNotSimplifyNullableInMemberAccessExpression()
            TestMissing(
        NewLines("Imports System \n Module Program \n Dim x = [|Nullable(Of Guid).op_Implicit|](Nothing) \n End Module"))
        End Sub

        <WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestMissingNullableSimplificationInsideCref()
            TestMissing(
"Imports System
''' <summary>
''' <see cref=""[|Nullable(Of T)|]""/>
''' </summary>
Class A
End Class")
        End Sub

        <WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestMissingNullableSimplificationInsideCref2()
            TestMissing(
"''' <summary>
''' <see cref=""[|System.Nullable(Of T)|]""/>
''' </summary>
Class A
End Class")
        End Sub

        <WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestMissingNullableSimplificationInsideCref3()
            TestMissing(
"''' <summary>
''' <see cref=""[|System.Nullable(Of T)|].Value""/>
''' </summary>
Class A
End Class")
        End Sub

        <WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestMissingNullableSimplificationInsideCref4()
            TestMissing(
"Imports System
''' <summary>
''' <see cref=""[|Nullable(Of Integer)|].Value""/>
''' </summary>
Class A
End Class")
        End Sub

        <WorkItem(2196, "https://github.com/dotnet/roslyn/issues/2196")>
        <WorkItem(2197, "https://github.com/dotnet/roslyn/issues/2197")>
        <WorkItem(29, "https: //github.com/dotnet/roslyn/issues/29")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestNullableSimplificationInsideCref()
            ' NOTE: This will probably stop working if issues 2196 / 2197 related to VB compiler and semantic model are fixed.
            ' It is unclear whether Nullable(Of Integer) is legal in the below case. Currently the VB compiler allows this while
            ' C# doesn't allow similar case. If this Nullable(Of Integer) becomes illegal in VB in the below case then the simplification
            ' from Nullable(Of Integer) -> Integer will also stop working and the baseline for this test will have to be updated.
            Test(
"Imports System
''' <summary>
        ''' <see cref=""C(Of [|Nullable(Of Integer)|])""/>
        ''' </summary>
Class C(Of T)
End Class",
"Imports System
''' <summary>
''' <see cref=""C(Of Integer?)""/>
''' </summary>
Class C(Of T)
End Class")
        End Sub

        <WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")>
        <WorkItem(2189, "https://github.com/dotnet/roslyn/issues/2189")>
        <WorkItem(2196, "https://github.com/dotnet/roslyn/issues/2196")>
        <WorkItem(2197, "https://github.com/dotnet/roslyn/issues/2197")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestNullableSimplificationInsideCref2()
            ' NOTE: This will probably stop working if issues 2196 / 2197 related to VB compiler and semantic model are fixed.
            ' It is unclear whether Nullable(Of Integer) is legal in the below case. Currently the VB compiler allows this while
            ' C# doesn't allow similar case. If this Nullable(Of Integer) becomes illegal in VB in the below case then the simplification
            ' from Nullable(Of Integer) -> Integer will also stop working and the baseline for this test will have to be updated.
            Test(
"Imports System
''' <summary>
''' <see cref=""C.M(Of [|Nullable(Of Integer)|])""/>
''' </summary>
Class C
    Sub M(Of T As Structure)()
    End Sub
End Class",
"Imports System
''' <summary>
''' <see cref=""C.M(Of Integer?)""/>
''' </summary>
Class C
    Sub M(Of T As Structure)()
    End Sub
End Class")
        End Sub

        <WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestNullableSimplificationInsideCref3()
            Test(
"Imports System
''' <summary>
''' <see cref=""A.M([|Nullable(Of A)|])""/>
''' </summary>
Structure A
    Sub M(x As A?)
    End Sub
End Structure",
"Imports System
''' <summary>
''' <see cref=""A.M(A?)""/>
''' </summary>
Structure A
    Sub M(x As A?)
    End Sub
End Structure")
        End Sub

        <WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestNullableSimplificationInsideCref4()
            Test(
"Imports System
Imports System.Collections.Generic
''' <summary>
''' <see cref=""A.M(List(Of [|Nullable(Of Integer)|]))""/>
''' </summary>
Structure A
    Sub M(x As List(Of Integer?))
    End Sub
End Structure",
"Imports System
Imports System.Collections.Generic
''' <summary>
''' <see cref=""A.M(List(Of Integer?))""/>
''' </summary>
Structure A
    Sub M(x As List(Of Integer?))
    End Sub
End Structure")
        End Sub

        <WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestNullableSimplificationInsideCref5()
            Test(
"Imports System
Imports System.Collections.Generic
''' <summary>
''' <see cref=""A.M(Of T)(List(Of [|Nullable(Of T)|]))""/>
''' </summary>
Structure A
    Sub M(Of U As Structure)(x As List(Of U?))
    End Sub
End Structure",
"Imports System
Imports System.Collections.Generic
''' <summary>
''' <see cref=""A.M(Of T)(List(Of T?))""/>
''' </summary>
Structure A
    Sub M(Of U As Structure)(x As List(Of U?))
    End Sub
End Structure")
        End Sub

        <WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestNullableSimplificationInsideCref6()
            Test(
"Imports System
Imports System.Collections.Generic
''' <summary>
''' <see cref=""A.M(Of U)(List(Of Nullable(Of Integer)), [|Nullable(Of U)|])""/>
''' </summary>
Structure A
    Sub M(Of U As Structure)(x As List(Of Integer?), y As U?)
    End Sub
End Structure",
"Imports System
Imports System.Collections.Generic
''' <summary>
''' <see cref=""A.M(Of U)(List(Of Nullable(Of Integer)), U?)""/>
''' </summary>
Structure A
    Sub M(Of U As Structure)(x As List(Of Integer?), y As U?)
    End Sub
End Structure")
        End Sub

        <WorkItem(529930)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestReservedNameInAttribute1()
            TestMissing(
        NewLines("<[|Global.Assembly|]> ' Simplify \n Class Assembly \n Inherits Attribute \n End Class"))
        End Sub

        <WorkItem(529930)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestReservedNameInAttribute2()
            TestMissing(
        NewLines("<[|Global.Assembly|]> ' Simplify \n Class Assembly \n Inherits Attribute \n End Class"))
        End Sub

        <WorkItem(529930)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestReservedNameInAttribute3()
            TestMissing(
        NewLines("<[|Global.Module|]> ' Simplify \n Class Module \n Inherits Attribute \n End Class"))
        End Sub

        <WorkItem(529930)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestReservedNameInAttribute4()
            TestMissing(
        NewLines("<[|Global.Module|]> ' Simplify \n Class Module \n Inherits Attribute \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestAliasedType()
            Dim source =
        NewLines("Class Program \n Sub Foo() \n Dim x As New [|Global.Program|] \n End Sub \n End Class")
            Test(source,
        NewLines("Class Program \n Sub Foo() \n Dim x As New Program \n End Sub \n End Class"), Nothing, 0)

            TestMissing(source, GetScriptOptions())
        End Sub

        <WorkItem(674789)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub CheckForAssemblyNameInFullWidthIdentifier()
            Dim source =
        <Code>
Imports System

Namespace N
    &lt;[|N.ＡＳＳＥＭＢＬＹ|]&gt;
    Class ＡＳＳＥＭＢＬＹ
        Inherits Attribute
    End Class
End Namespace
</Code>

            Dim expected =
        <Code>
Imports System

Namespace N
    &lt;[ＡＳＳＥＭＢＬＹ]&gt;
    Class ＡＳＳＥＭＢＬＹ
        Inherits Attribute
    End Class
End Namespace
</Code>
            Test(source.Value, expected.Value)
        End Sub

        <WorkItem(568043)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub DontSimplifyNamesWhenThereAreParseErrors()
            Dim source =
        <Code>
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks

Module Program
    Sub Main(args() As String)
        Console.[||]
    End Sub
End Module
</Code>

            TestMissing(source.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub ShowModuleNameAsUnnecessaryMemberAccess()
            Dim source =
        <Code>
Imports System

Namespace foo
    Module Program
        Sub Main(args As String())
        End Sub
    End Module
End Namespace

Namespace bar
    Module b
        Sub m()
             [|foo.Program.Main|](Nothing)
        End Sub
    End Module
End Namespace
</Code>

            Dim expected =
            <Code>
Imports System

Namespace foo
    Module Program
        Sub Main(args As String())
        End Sub
    End Module
End Namespace

Namespace bar
    Module b
        Sub m()
             foo.Main(Nothing)
        End Sub
    End Module
End Namespace
</Code>
            Test(source.Value, expected.Value)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub ShowModuleNameAsUnnecessaryQualifiedName()
            Dim source =
        <Code>
Imports System

Namespace foo
    Module Program
        Sub Main(args As String())
        End Sub
        Class C1
        End Class
    End Module
End Namespace

Namespace bar
    Module b
        Sub m()
             dim x as [|foo.Program.C1|]
        End Sub
    End Module
End Namespace
</Code>

            Dim expected =
        <Code>
Imports System

Namespace foo
    Module Program
        Sub Main(args As String())
        End Sub
        Class C1
        End Class
    End Module
End Namespace

Namespace bar
    Module b
        Sub m()
             dim x as foo.C1
        End Sub
    End Module
End Namespace
</Code>

            Test(source.Value, expected.Value)
        End Sub

        <WorkItem(608200)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub Bugfix_608200()
            Dim source =
        <Code>
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())

    End Sub
End Module

Module M
    Dim e = GetType([|System.Collections.Generic.List(Of ).Enumerator|])
End Module
</Code>

            Dim expected =
        <Code>
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())

    End Sub
End Module

Module M
    Dim e = GetType(List(Of ).Enumerator)
End Module
</Code>

            Test(source.Value, expected.Value)
        End Sub

        <WorkItem(578686)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub DontUseAlias()
            Dim source =
        <Code>
Imports System
Imports System.Collections.Generic
Imports System.Linq

Imports Foo = A.B
Imports Bar = C.D

Module Program
    Sub Main(args As String())
        Dim local1 = New A.B()
        Dim local2 = New C.D()

        Dim local3 = New List(Of NoAlias.Test)

        Dim local As NoAlias.Test
        For Each local In local3
            Dim x = [|local.prop|]
        Next
    End Sub
End Module

Namespace A
    Class B
    End Class
End Namespace

Namespace C
    Class D
    End Class
End Namespace

Namespace NoAlias
    Class Test
        Public Property prop As A.B
    End Class
End Namespace
</Code>

            TestMissing(source.Value)
        End Sub

        <WorkItem(547246)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub CreateCodeIssueWithProperIssueSpan()
            Dim source =
        <Code>
Imports Foo = System.Console

Module Program
    Sub Main(args As String())
        [|System.Console|].Read()
    End Sub
End Module
</Code>

            Dim expected =
            <Code>
Imports Foo = System.Console

Module Program
    Sub Main(args As String())
        Foo.Read()
    End Sub
End Module
</Code>

            Test(source.Value, expected.Value)
            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromFile(source.Value, Nothing, Nothing)
                Dim diagnosticAndFix = GetDiagnosticAndFix(workspace)
                Dim span = diagnosticAndFix.Item1.Location.SourceSpan
                Assert.NotEqual(span.Start, 0)
                Assert.NotEqual(span.End, 0)
            End Using
        End Sub

        <WorkItem(629572)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub DoNotIncludeAliasNameIfLastTargetNameIsTheSame_1()
            Dim source =
        <Code>
Imports C = A.B.C

Module Program
    Sub Main(args As String())
        dim x = new [|A.B.C|]()
    End Sub
End Module

Namespace A
    Namespace B
        Class C
        End Class
    End Namespace
End Namespace
</Code>

            Dim expected =
            <Code>
Imports C = A.B.C

Module Program
    Sub Main(args As String())
        dim x = new C()
    End Sub
End Module

Namespace A
    Namespace B
        Class C
        End Class
    End Namespace
End Namespace
</Code>

            Test(source.Value, expected.Value)
            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromFile(source.Value, Nothing, Nothing)
                Dim diagnosticAndFix = GetDiagnosticAndFix(workspace)
                Dim span = diagnosticAndFix.Item1.Location.SourceSpan
                Assert.Equal(span.Start, expected.Value.ToString.Replace(vbLf, vbCrLf).IndexOf("new C", StringComparison.Ordinal) + 4)
                Assert.Equal(span.Length, "A.B".Length)
            End Using
        End Sub

        <WorkItem(629572)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub DoNotIncludeAliasNameIfLastTargetNameIsTheSame_2()
            Dim source =
        <Code>
Imports Console = System.Console

Module Program
    Sub Main(args As String())
        [|System.Console|].WriteLine("foo")
    End Sub
End Module
</Code>

            Dim expected =
            <Code>
Imports Console = System.Console

Module Program
    Sub Main(args As String())
        Console.WriteLine("foo")
    End Sub
End Module
</Code>

            Test(source.Value, expected.Value)
            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromFile(source.Value, Nothing, Nothing)
                Dim diagnosticAndFix = GetDiagnosticAndFix(workspace)
                Dim span = diagnosticAndFix.Item1.Location.SourceSpan
                Assert.Equal(span.Start, expected.Value.ToString.Replace(vbLf, vbCrLf).IndexOf("Console.WriteLine(""foo"")", StringComparison.Ordinal))
                Assert.Equal(span.Length, "System".Length)
            End Using
        End Sub

        <WorkItem(686306)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub DontSimplifyNameSyntaxToTypeSyntaxInVBCref()
            Dim source =
        <Code>
Imports System
''' &lt;see cref="[|Object|]"/>
Module Program
End Module
</Code>

            TestMissing(source.Value)
        End Sub

        <WorkItem(721817)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub DontSimplifyNameSyntaxToPredefinedTypeSyntaxInVBCref()
            Dim source =
        <Code>
Public Class Test
    '''&lt;summary&gt;
    ''' &lt;see cref="[|Test.ReferenceEquals|](Object, Object)"/&gt;   
    ''' &lt;/Code&gt;
        Public Sub Foo()

        End Sub

    End Class


</Code>

            TestMissing(source.Value)
        End Sub

        <WorkItem(721694)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub EnableReducersInsideVBCref()
            Dim source =
        <Code>
Public Class Test_Dev11
    '''&lt;summary&gt;
    ''' &lt;see cref="[|Global.Microsoft|].VisualBasic.Left"/&gt;
    ''' &lt;/Code&gt;
        Public Sub testscenarios()
        End Sub
    End Class
</Code>

            Dim expected =
        <Code>
Public Class Test_Dev11
    '''&lt;summary&gt;
    ''' &lt;see cref="Microsoft.VisualBasic.Left"/&gt;
    ''' &lt;/Code&gt;
        Public Sub testscenarios()
        End Sub
    End Class
</Code>
            Test(source.Value, expected.Value)
        End Sub

        <WorkItem(736377)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub DontSimplifyTypeNameBrokenCode()
            Dim source =
        <Code>
            <![CDATA[
Imports System.Collections.Generic

Class Program
    Public Shared GetA([|System.Diagnostics|])
    Public Shared Function GetAllFilesInSolution() As ISet(Of String)
		Return Nothing
    End Function
End Class
]]>
        </Code>

            TestMissing(source.Value)
        End Sub

        <WorkItem(860565)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub SimplifyGenericTypeName_Bug860565()
            Dim source =
        <Code>
            <![CDATA[
Interface A(Of T)
    Interface B(Of S)
        Inherits A(Of B(Of B(Of S)))
        Interface C(Of U)
            Inherits B(Of [|C(Of C(Of U)).B(Of T)|])
        End Interface
    End Interface
End Interface
]]>
        </Code>

            TestMissing(source.Value)
        End Sub

        <WorkItem(813385)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub DontSimplifyAliases()
            Dim source =
        <Code>
            <![CDATA[
Imports Foo = System.Int32

Class P
    Dim F As [|Foo|]
End Class
]]>
        </Code>

            TestMissing(source.Value)
        End Sub

        <WorkItem(942568)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestIntrinsicTypesInLocalDeclarationDefaultValue_1()
            Dim source =
        <Code>
Module Program
    Sub Main(args As String())
        Dim x As [|System.Int32|]
    End Sub
End Module
</Code>

            Dim expected =
        <Code>
Module Program
    Sub Main(args As String())
        Dim x As Integer
    End Sub
End Module
</Code>

            Test(source.Value, expected.Value, compareTokens:=False)
        End Sub

        <WorkItem(942568)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestIntrinsicTypesInLocalDeclarationDefaultValue_2()
            Dim source =
        <Code>
Module Program
    Sub Main(args As String())
        Dim s = New List(Of [|System.Int32|])()
    End Sub
End Module
</Code>

            Dim expected =
        <Code>
Module Program
    Sub Main(args As String())
        Dim s = New List(Of Integer)()
    End Sub
End Module
</Code>

            Test(source.Value, expected.Value, compareTokens:=False)
        End Sub

        <WorkItem(942568)>
        <WorkItem(954536)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestIntrinsicTypesInCref1()
            Dim source =
        <Code>
Imports System
''' &lt;see cref="[|Int32|]"/>
Module Program
End Module
</Code>
            Dim expected =
        <Code>
Imports System
''' &lt;see cref="Integer"/>
Module Program
End Module
</Code>

            Test(source.Value, expected.Value, compareTokens:=False)
        End Sub

        <WorkItem(942568)>
        <WorkItem(954536)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestIntrinsicTypesInCref2()
            Dim source =
        <Code>
''' &lt;see cref="[|System.Int32|]"/>
Module Program
End Module
</Code>
            Dim expected =
        <Code>
''' &lt;see cref="Integer"/>
Module Program
End Module
</Code>

            Test(source.Value, expected.Value, compareTokens:=False)
        End Sub

        <WorkItem(1012713)>
        <WorkItem(942568)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestIntrinsicTypesInCref3()
            Dim source =
        <Code>
''' &lt;see cref="[|System.Int32|].MaxValue"/>
Module Program
End Module
</Code>

            TestMissing(source.Value)
        End Sub

        <WorkItem(942568)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestIntrinsicTypesInLocalDeclarationNonDefaultValue_1()
            Dim source =
        <Code>
Class Program
    Private x As [|System.Int32|]
    Sub Main(args As System.Int32)
        Dim a As System.Int32 = 9
    End Sub
End Class
</Code>
            TestMissing(source.Value, options:=New Dictionary(Of OptionKey, Object) From {{New OptionKey(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, LanguageNames.VisualBasic), False}})
        End Sub

        <WorkItem(942568)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestIntrinsicTypesInLocalDeclarationNonDefaultValue_2()
            Dim source =
        <Code>
Class Program
    Private x As System.Int32
    Sub Main(args As [|System.Int32|])
        Dim a As System.Int32 = 9
    End Sub
End Class
</Code>
            TestMissing(source.Value, options:=New Dictionary(Of OptionKey, Object) From {{New OptionKey(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, LanguageNames.VisualBasic), False}})
        End Sub

        <WorkItem(942568)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestIntrinsicTypesInLocalDeclarationNonDefaultValue_3()
            Dim source =
        <Code>
Class Program
    Private x As System.Int32
    Sub Main(args As System.Int32)
        Dim a As [|System.Int32|] = 9
    End Sub
End Class
</Code>
            TestMissing(source.Value, options:=New Dictionary(Of OptionKey, Object) From {{New OptionKey(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, LanguageNames.VisualBasic), False}})
        End Sub

        <WorkItem(942568)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestIntrinsicTypesInMemberAccess_Default_1()
            Dim source =
        <Code>
Imports System

Module Program
    Sub Main(args As String())
        Dim s = [|Int32|].MaxValue
    End Sub
End Module
</Code>
            Dim expected =
        <Code>
Imports System

Module Program
    Sub Main(args As String())
        Dim s = Integer.MaxValue
    End Sub
End Module
</Code>
            Test(source.Value, expected.Value, compareTokens:=False)
        End Sub

        <WorkItem(942568)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestIntrinsicTypesInMemberAccess_Default_2()
            Dim source =
        <Code>
Module Program
    Sub Main(args As String())
        Dim s = [|System.Int32|].MaxValue
    End Sub
End Module
</Code>
            Dim expected =
        <Code>
Module Program
    Sub Main(args As String())
        Dim s = Integer.MaxValue
    End Sub
End Module
</Code>
            Test(source.Value, expected.Value, compareTokens:=False)
        End Sub

        <WorkItem(956667)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestIntrinsicTypesInMemberAccess_Default_3()
            Dim source =
        <Code>
Imports System

Class Program1
    Sub Main(args As String())
        Dim s = [|Program2.memb|].ToString()
    End Sub
End Class

Class Program2
    Public Shared Property memb As Integer
End Class

</Code>
            TestMissing(source.Value)
        End Sub

        <WorkItem(942568)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestIntrinsicTypesInMemberAccess_NonDefault_1()
            Dim source =
        <Code>
Module Program
    Sub Main(args As String())
        Dim s = [|System.Int32|].MaxValue
    End Sub
End Module
</Code>
            TestMissing(source.Value, options:=New Dictionary(Of OptionKey, Object) From {{New OptionKey(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, LanguageNames.VisualBasic), False}})
        End Sub

        <WorkItem(942568)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestIntrinsicTypesInMemberAccess_NonDefault_2()
            Dim source =
        <Code>
Imports System
Module Program
    Sub Main(args As String())
        Dim s = [|Int32|].MaxValue
    End Sub
End Module
</Code>
            TestMissing(source.Value, options:=New Dictionary(Of OptionKey, Object) From {{New OptionKey(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, LanguageNames.VisualBasic), False}})
        End Sub

        <WorkItem(954536)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestIntrinsicTypesInCref_NonDefault_1()
            Dim source =
        <Code>
''' &lt;see cref="[|System.Int32|]"/>
Module Program
End Module
</Code>

            TestMissing(source.Value, options:=New Dictionary(Of OptionKey, Object) From {{New OptionKey(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, LanguageNames.VisualBasic), False}})
        End Sub

        <WorkItem(954536)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestIntrinsicTypesInCref_NonDefault_2()
            Dim source =
        <Code>
''' &lt;see cref="[|System.Int32|]"/>
Module Program
End Module
</Code>

            Dim expected =
        <Code>
''' &lt;see cref="Integer"/>
Module Program
End Module
</Code>

            Test(source.Value, expected.Value, options:=New Dictionary(Of OptionKey, Object) From {{New OptionKey(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, LanguageNames.VisualBasic), False}})
        End Sub

        <WorkItem(954536)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestIntrinsicTypesInCref_NonDefault_3()
            Dim source =
        <Code>
''' &lt;see cref="System.Collections.Generic.List(Of T).CopyTo(Integer, T(), Integer, [|System.Int32|])"/>
Module Program
End Module
</Code>

            TestMissing(source.Value, options:=New Dictionary(Of OptionKey, Object) From {{New OptionKey(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, LanguageNames.VisualBasic), False}})
        End Sub

        <WorkItem(954536)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestIntrinsicTypesInCref_NonDefault_4()
            Dim source =
        <Code>
''' &lt;see cref="System.Collections.Generic.List(Of T).CopyTo(Integer, T(), Integer, [|System.Int32|])"/>
Module Program
End Module
</Code>

            Dim expected =
        <Code>
''' &lt;see cref="System.Collections.Generic.List(Of T).CopyTo(Integer, T(), Integer, Integer)"/>
Module Program
End Module
</Code>

            Test(source.Value, expected.Value, options:=New Dictionary(Of OptionKey, Object) From {{New OptionKey(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, LanguageNames.VisualBasic), False}})
        End Sub

        <WorkItem(965208)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestSimplifyDiagnosticId()
            Dim source =
        <Code>
Imports System
Module Program
    Sub Main(args As String())
        [|System.Console.WriteLine|]("")
    End Sub
End Module
</Code>

            Using workspace = CreateWorkspaceFromFile(source, Nothing, Nothing)
                Dim diagnostics = GetDiagnostics(workspace).Where(Function(d) d.Id = IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId)
                Assert.Equal(1, diagnostics.Count)
            End Using

            source =
        <Code>
Imports System
Module Program
    Sub Main(args As String())
        Dim a As [|System.Int32|]
    End Sub
End Module
</Code>

            Using workspace = CreateWorkspaceFromFile(source, Nothing, Nothing)
                Dim diagnostics = GetDiagnostics(workspace).Where(Function(d) d.Id = IDEDiagnosticIds.SimplifyNamesDiagnosticId)
                Assert.Equal(1, diagnostics.Count)
            End Using

            source =
        <Code>
Imports System
Class C
    Dim x as Integer
    Sub F()
        Dim z = [|Me.x|]
    End Sub
End Module
</Code>

            Using workspace = CreateWorkspaceFromFile(source, Nothing, Nothing)
                Dim diagnostics = GetDiagnostics(workspace).Where(Function(d) d.Id = IDEDiagnosticIds.SimplifyThisOrMeDiagnosticId)
                Assert.Equal(1, diagnostics.Count)
            End Using
        End Sub

        <WorkItem(995168)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub SimplifyToPredefinedTypeNameShouldNotBeOfferedInsideNameOf1()
            TestMissing("Imports System
Module Program
    Sub Main()
        Dim x = NameOf([|Int32|])
    End Sub
End Module")
        End Sub

        <WorkItem(995168)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub SimplifyToPredefinedTypeNameShouldNotBeOfferedInsideNameOf2()
            TestMissing("
Module Program
    Sub Main()
        Dim x = NameOf([|System.Int32|])
    End Sub
End Module")
        End Sub

        <WorkItem(995168)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub SimplifyToPredefinedTypeNameShouldNotBeOfferedInsideNameOf3()
            TestMissing("Imports System
Module Program
    Sub Main()
        Dim x = NameOf([|Int32|].MaxValue)
    End Sub
End Module")
        End Sub

        <WorkItem(995168)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub SimplifyTypeNameInsideNameOf()
            Test("Imports System
Module Program
    Sub Main()
        Dim x = NameOf([|System.Int32|])
    End Sub
End Module",
"Imports System
Module Program
    Sub Main()
        Dim x = NameOf(Int32)
    End Sub
End Module")
        End Sub
    End Class
End Namespace
