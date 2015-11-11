Option Strict Off
' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestGenericNames() As Task
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
            Await TestAsync(source.Value, expected.Value)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestArgument() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As [|System.String|]()) \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n End Sub \n End Module"),
index:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestAliasWithMemberAccess() As Task
            Await TestAsync(
NewLines("Imports Foo = System.Int32 \n Module Program \n Sub Main(args As String()) \n Dim x = [|System.Int32|].MaxValue \n End Sub \n End Module"),
NewLines("Imports Foo = System.Int32 \n Module Program \n Sub Main(args As String()) \n Dim x = Foo.MaxValue \n End Sub \n End Module"),
index:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestWithCursorAtBeginning() As Task
            Await TestAsync(
NewLines("Imports System.IO \n Module Program \n Sub Main(args As String()) \n Dim x As [|System.IO.File|] \n End Sub \n End Module"),
NewLines("Imports System.IO \n Module Program \n Sub Main(args As String()) \n Dim x As File \n End Sub \n End Module"),
index:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestMinimalSimplifyOnNestedNamespaces() As Task
            Dim source =
NewLines("Imports Outer \n Namespace Outer \n Namespace Inner \n Class Foo \n End Class \n End Namespace \n End Namespace \n Module Program \n Sub Main(args As String()) \n Dim x As [|Outer.Inner.Foo|] \n End Sub \n End Module")

            Await TestAsync(source,
NewLines("Imports Outer \n Namespace Outer \n Namespace Inner \n Class Foo \n End Class \n End Namespace \n End Namespace \n Module Program \n Sub Main(args As String()) \n Dim x As Inner.Foo \n End Sub \n End Module"),
index:=0)
            Await TestActionCountAsync(source, 1)
        End Function

        <WorkItem(540567)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestMinimalSimplifyOnNestedNamespacesFromMetadataAlias() As Task
            Await TestAsync(
NewLines("Imports A1 = System.IO.File \n Class Foo \n Dim x As [|System.IO.File|] \n End Class"),
NewLines("Imports A1 = System.IO.File \n Class Foo \n Dim x As A1 \n End Class"),
index:=0)
        End Function

        <WorkItem(540567)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestMinimalSimplifyOnNestedNamespacesFromMetadata() As Task
            Await TestAsync(
NewLines("Imports System \n Class Foo \n Dim x As [|System.IO.File|] \n End Class"),
NewLines("Imports System \n Class Foo \n Dim x As IO.File \n End Class"),
index:=0)
        End Function

        <WorkItem(540569)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestFixAllOccurrences() As Task
            Dim actionId = SimplifyTypeNamesCodeFixProvider.GetCodeActionId(IDEDiagnosticIds.SimplifyNamesDiagnosticId, "NS1.SomeClass")
            Await TestAsync(
NewLines("Imports NS1 \n Namespace NS1 \n Class SomeClass \n End Class \n End Namespace \n Class Foo \n Dim x As {|FixAllInDocument:NS1.SomeClass|} \n Dim y As NS1.SomeClass \n End Class"),
NewLines("Imports NS1 \n Namespace NS1 \n Class SomeClass \n End Class \n End Namespace \n Class Foo \n Dim x As SomeClass \n Dim y As SomeClass \n End Class"),
fixAllActionEquivalenceKey:=actionId)
        End Function

        <WorkItem(578686)>
        <WpfFact(Skip:="1033012"), Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestFixAllOccurrencesForAliases() As Task
            Await TestAsync(
NewLines("Imports System \n Imports foo = C.D \n Imports bar = A.B \n Namespace C \n Class D \n End Class \n End Namespace \n Module Program Sub Main(args As String()) \n Dim Local1 = [|New A.B().prop|] \n End Sub \n End Module \n Namespace A \n Class B \n Public Property prop As C.D \n End Class \n End Namespace"),
NewLines("Imports System \n Imports foo = C.D \n Imports bar = A.B \n Namespace C \n Class D \n End Class \n End Namespace \n Module Program Sub Main(args As String()) \n Dim Local1 = New bar().prop \n End Sub \n End Module \n Namespace A \n Class B \n Public Property prop As foo \n End Class \n End Namespace"),
index:=1)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestSimplifyFromReference() As Task
            Await TestAsync(
        NewLines("Imports System.Threading \n Class Class1 \n Dim v As [|System.Threading.Thread|] \n End Class"),
        NewLines("Imports System.Threading \n Class Class1 \n Dim v As Thread \n End Class"),
        index:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestGenericClassDefinitionAsClause() As Task
            Await TestAsync(
        NewLines("Imports SomeNamespace \n Namespace SomeNamespace \n Class Base \n End Class \n End Namespace \n Class SomeClass(Of x As [|SomeNamespace.Base|]) \n End Class"),
        NewLines("Imports SomeNamespace \n Namespace SomeNamespace \n Class Base \n End Class \n End Namespace \n Class SomeClass(Of x As Base) \n End Class"),
        index:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestGenericClassInstantiationOfClause() As Task
            Await TestAsync(
        NewLines("Imports SomeNamespace \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class GenericClass(Of T) \n End Class \n Class Foo \n Sub Method1() \n Dim q As GenericClass(Of [|SomeNamespace.SomeClass|]) \n End Sub \n End Class"),
        NewLines("Imports SomeNamespace \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class GenericClass(Of T) \n End Class \n Class Foo \n Sub Method1() \n Dim q As GenericClass(Of SomeClass) \n End Sub \n End Class"),
        index:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestGenericMethodDefinitionAsClause() As Task
            Await TestAsync(
        NewLines("Imports SomeNamespace \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class Foo \n Sub Method1(Of T As [|SomeNamespace.SomeClass|]) \n End Sub \n End Class"),
        NewLines("Imports SomeNamespace \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class Foo \n Sub Method1(Of T As SomeClass) \n End Sub \n End Class"),
        index:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestGenericMethodInvocationOfClause() As Task
            Await TestAsync(
        NewLines("Imports SomeNamespace \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class Foo \n Sub Method1(Of T) \n End Sub \n Sub Method2() \n Method1(Of [|SomeNamespace.SomeClass|]) \n End Sub \n End Class"),
        NewLines("Imports SomeNamespace \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace \n Class Foo \n Sub Method1(Of T) \n End Sub \n Sub Method2() \n Method1(Of SomeClass) \n End Sub \n End Class"),
        index:=0)
        End Function

        <WorkItem(6872, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestAttributeApplication() As Task
            Await TestAsync(
        NewLines("Imports SomeNamespace \n <[|SomeNamespace.Something|]()> \n Class Foo \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits System.Attribute \n End Class \n End Namespace"),
        NewLines("Imports SomeNamespace \n <Something()> \n Class Foo \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits System.Attribute \n End Class \n End Namespace"),
        index:=0)
        End Function

        <WorkItem(6872, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestMultipleAttributeApplicationBelow() As Task

            'IMPLEMENT NOT ESCAPE ATTRIBUTE DEPENDENT ON CONTEXT

            Await TestAsync(
        NewLines("Imports System \n Imports SomeNamespace \n <Existing()> \n <[|SomeNamespace.Something|]()> \n Class Foo \n End Class \n Class ExistingAttribute \n Inherits System.Attribute \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits System.Attribute \n End Class \n End Namespace"),
        NewLines("Imports System \n Imports SomeNamespace \n <Existing()> \n <Something()> \n Class Foo \n End Class \n Class ExistingAttribute \n Inherits System.Attribute \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits System.Attribute \n End Class \n End Namespace"),
        index:=0)
        End Function

        <WorkItem(6872, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestMultipleAttributeApplicationAbove() As Task
            Await TestAsync(
        NewLines("Imports System \n Imports SomeNamespace \n <[|SomeNamespace.Something|]()> \n <Existing()> \n Class Foo \n End Class \n Class ExistingAttribute \n Inherits System.Attribute \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits System.Attribute \n End Class \n End Namespace"),
        NewLines("Imports System \n Imports SomeNamespace \n <Something()> \n <Existing()> \n Class Foo \n End Class \n Class ExistingAttribute \n Inherits System.Attribute \n End Class \n Namespace SomeNamespace \n Class SomethingAttribute \n Inherits System.Attribute \n End Class \n End Namespace"),
        index:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestSimplifiedLeftmostQualifierIsEscapedWhenMatchesKeyword() As Task
            Await TestAsync(
        NewLines("Imports Outer \n Class SomeClass \n Dim x As [|Outer.Namespace.Something|] \n End Class \n Namespace Outer \n Namespace [Namespace] \n Class Something \n End Class \n End Namespace \n End Namespace"),
        NewLines("Imports Outer \n Class SomeClass \n Dim x As [Namespace].Something \n End Class \n Namespace Outer \n Namespace [Namespace] \n Class Something \n End Class \n End Namespace \n End Namespace"),
        index:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestTypeNameIsEscapedWhenMatchingKeyword() As Task
            Await TestAsync(
        NewLines("Imports Outer \n Class SomeClass \n Dim x As [|Outer.Class|] \n End Class \n Namespace Outer \n Class [Class] \n End Class \n End Namespace"),
        NewLines("Imports Outer \n Class SomeClass \n Dim x As [Class] \n End Class \n Namespace Outer \n Class [Class] \n End Class \n End Namespace"),
        index:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestSimplifyNotSuggestedInImportsStatement() As Task
            Await TestMissingAsync(
        NewLines("[|Imports SomeNamespace \n Imports SomeNamespace.InnerNamespace \n Namespace SomeNamespace \n Namespace InnerNamespace \n Class SomeClass \n End Class \n End Namespace \n End Namespace|]"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestNoSimplifyInGenericAsClauseIfConflictsWithTypeParameterName() As Task
            Await TestMissingAsync(
        NewLines("[|Imports SomeNamespace \n Class Class1 \n Sub Foo(Of SomeClass)(x As SomeNamespace.SomeClass) \n End Sub \n End Class \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace|]"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestSimplifyNotOfferedIfSimplifyingWouldCauseAmbiguity() As Task
            Await TestMissingAsync(
        NewLines("[|Imports SomeNamespace \n Class SomeClass \n End Class \n Class Class1 \n Dim x As SomeNamespace.SomeClass \n End Class \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace|]"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestSimplifyInGenericAsClauseIfNoConflictWithTypeParameterName() As Task
            Await TestAsync(
        NewLines("Imports SomeNamespace \n Class Class1 \n Sub Foo(Of T)(x As [|SomeNamespace.SomeClass|]) \n End Sub \n End Class \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace"),
        NewLines("Imports SomeNamespace \n Class Class1 \n Sub Foo(Of T)(x As SomeClass) \n End Sub \n End Class \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace"),
        index:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestCaseInsensitivity() As Task
            Await TestAsync(
        NewLines("Imports SomeNamespace \n Class Foo \n Dim x As [|SomeNamespace.someclass|] \n End Class \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace"),
        NewLines("Imports SomeNamespace \n Class Foo \n Dim x As someclass \n End Class \n Namespace SomeNamespace \n Class SomeClass \n End Class \n End Namespace"),
        index:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestSimplifyGenericTypeWithArguments() As Task
            Dim source =
        NewLines("Imports System.Collections.Generic \n Class Foo \n Function F() As [|System.Collections.Generic.List(Of Integer)|] \n End Function \n End Class")

            Await TestAsync(source,
        NewLines("Imports System.Collections.Generic \n Class Foo \n Function F() As List(Of Integer) \n End Function \n End Class"),
        index:=0)
            Await TestActionCountAsync(source, 1)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestParameterType() As Task
            Await TestAsync(
        NewLines("Imports System.IO \n Module Program \n Sub Main(args As String(), f As [|System.IO.FileMode|]) \n End Sub \n End Module"),
        NewLines("Imports System.IO \n Module Program \n Sub Main(args As String(), f As FileMode) \n End Sub \n End Module"),
        index:=0)
        End Function

        <WorkItem(540565)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestLocation1() As Task
            Await TestAsync(
        NewLines("Imports Foo \n Namespace Foo \n Class FooClass \n End Class \n End Namespace \n Module Program \n Sub Main(args As String()) \n Dim x As [|Foo.FooClass|] \n End Sub \n End Module"),
        NewLines("Imports Foo \n Namespace Foo \n Class FooClass \n End Class \n End Namespace \n Module Program \n Sub Main(args As String()) \n Dim x As FooClass \n End Sub \n End Module"),
        index:=0)
        End Function

        <WpfFact(Skip:="1033012"), Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestFixAllFixesUnrelatedTypes() As Task
            Await TestAsync(
        NewLines("Imports A \n Imports B \n Imports C \n Module Program \n Sub Method1(a As [|A.FooA|], b As B.FooB, c As C.FooC) \n Dim qa As A.FooA \n Dim qb As B.FooB \n Dim qc As C.FooC \n End Sub \n End Module \n Namespace A \n Class FooA \n End Class \n End Namespace \n Namespace B \n Class FooB \n End Class \n End Namespace \n Namespace C \n Class FooC \n End Class \n End Namespace"),
        NewLines("Imports A \n Imports B \n Imports C \n Module Program \n Sub Method1(a As FooA, b As FooB, c As FooC) \n Dim qa As FooA \n Dim qb As FooB \n Dim qc As FooC \n End Sub \n End Module \n Namespace A \n Class FooA \n End Class \n End Namespace \n Namespace B \n Class FooB \n End Class \n End Namespace \n Namespace C \n Class FooC \n End Class \n End Namespace"))
        End Function

        <WpfFact(Skip:="1033012"), Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestSimplifyFixesAllNestedTypeNames() As Task
            Dim source =
        NewLines("Imports A \n Imports B \n Imports C \n Module Program \n Sub Method1(a As [|A.FooA(Of B.FooB(Of C.FooC))|] ) \n End Sub \n End Module \n Namespace A \n Class FooA(Of T) \n End Class \n End Namespace \n Namespace B \n Class FooB(Of T) \n End Class \n End Namespace \n Namespace C \n Class FooC \n End Class \n End Namespace")

            Await TestAsync(source,
        NewLines("Imports A \n Imports B \n Imports C \n Module Program \n Sub Method1(a As FooA(Of FooB(Of FooC))) \n End Sub \n End Module \n Namespace A \n Class FooA(Of T) \n End Class \n End Namespace \n Namespace B \n Class FooB(Of T) \n End Class \n End Namespace \n Namespace C \n Class FooC \n End Class \n End Namespace"),
        index:=1)
            Await TestActionCountAsync(source, 1)
        End Function

        <WorkItem(551040)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestSimplifyNestedType() As Task
            Dim source =
        NewLines("Class Preserve \n Public Class X \n Public Shared Y \n End Class \n End Class \n Class Z(Of T) \n Inherits Preserve \n End Class \n Class M \n Public Shared Sub Main() \n Redim [|Z(Of Integer).X|].Y(1) \n End Function \n End Class")

            Await TestAsync(source,
        NewLines("Class Preserve \n Public Class X \n Public Shared Y \n End Class \n End Class \n Class Z(Of T) \n Inherits Preserve \n End Class \n Class M \n Public Shared Sub Main() \n Redim [Preserve].X.Y(1) \n End Function \n End Class"),
        index:=0)
            Await TestActionCountAsync(source, 1)
        End Function

        <WorkItem(551040)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestSimplifyStaticMemberAccess() As Task
            Dim source =
        NewLines("Class Preserve \n Public Shared Y \n End Class \n Class Z(Of T) \n Inherits Preserve \n End Class \n Class M \n Public Shared Sub Main() \n Redim [|Z(Of Integer).Y(1)|] \n End Function \n End Class")

            Await TestAsync(source,
        NewLines("Class Preserve \n Public Shared Y \n End Class \n Class Z(Of T) \n Inherits Preserve \n End Class \n Class M \n Public Shared Sub Main() \n Redim [Preserve].Y(1) \n End Function \n End Class"),
        index:=0)
            Await TestActionCountAsync(source, 1)
        End Function

        <WorkItem(540398)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestImplementsClause() As Task
            Await TestAsync(
        NewLines("Imports System \n Class Foo \n Implements IComparable(Of String) \n Public Function CompareTo(other As String) As Integer Implements [|System.IComparable(Of String).CompareTo|] \n Return Nothing \n End Function \n End Class"),
        NewLines("Imports System \n Class Foo \n Implements IComparable(Of String) \n Public Function CompareTo(other As String) As Integer Implements IComparable(Of String).CompareTo \n Return Nothing \n End Function \n End Class"),
        index:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestSimpleArray() As Task
            Await TestAsync(
        NewLines("Imports System.Collections.Generic \n Namespace N1 \n Class Test \n Private a As [|System.Collections.Generic.List(Of System.String())|] \n End Class \n End Namespace"),
        NewLines("Imports System.Collections.Generic \n Namespace N1 \n Class Test \n Private a As List(Of String()) \n End Class \n End Namespace"),
        index:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestSimpleMultiDimArray() As Task
            Await TestAsync(
        NewLines("Imports System.Collections.Generic \n Namespace N1 \n Class Test \n Private a As [|System.Collections.Generic.List(Of System.String()(,)(,,,)) |]  \n End Class \n End Namespace"),
        NewLines("Imports System.Collections.Generic \n Namespace N1 \n Class Test \n Private a As List(Of String()(,)(,,,)) \n End Class \n End Namespace"),
        index:=0)
        End Function

        <WorkItem(542093)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestNoSimplificationOfParenthesizedPredefinedTypes() As Task
            Await TestMissingAsync(
        NewLines("[|Module M \n Sub Main() \n Dim x = (System.String).Equals("", "") \n End Sub \n End Module|]"))
        End Function

        <WpfFact(Skip:="1033012"), Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        <Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)>
        Public Async Function TestConflicts() As Task
            Await TestAsync(
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
        End Function

        <WorkItem(542138)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestSimplifyModuleWithReservedName() As Task
            Await TestAsync(
        NewLines("Namespace X \n Module [String] \n Sub Main() \n [|X.String.Main|] \n End Sub \n End Module \n End Namespace"),
        NewLines("Namespace X \n Module [String] \n Sub Main() \n Main \n End Sub \n End Module \n End Namespace"),
        index:=0)
        End Function

        <WorkItem(542348)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestPreserve1() As Task
            Await TestAsync(
        NewLines("Module M \n Dim preserve() \n Sub Main() \n ReDim [|M.preserve|](1) \n End Sub \n End Module"),
        NewLines("Module M \n Dim preserve() \n Sub Main() \n ReDim [preserve](1) \n End Sub \n End Module"),
        index:=0)
        End Function

        <WorkItem(551040)>
        <WorkItem(542348)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestPreserve3() As Task
            Await TestAsync(
        NewLines("Class Preserve \n Class X \n Public Shared Dim Y \n End Class \n End Class \n Class Z(Of T) \n Inherits Preserve \n End Class \n Module M \n Sub Main() \n ReDim [|Z(Of Integer).X.Y|](1) ' Simplify Z(Of Integer).X \n End Sub \n End Module"),
        NewLines("Class Preserve \n Class X \n Public Shared Dim Y \n End Class \n End Class \n Class Z(Of T) \n Inherits Preserve \n End Class \n Module M \n Sub Main() \n ReDim [Preserve].X.Y(1) ' Simplify Z(Of Integer).X \n End Sub \n End Module"))
        End Function

        <WorkItem(545603)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestNullableInImports1() As Task
            Await TestMissingAsync(
        NewLines("Imports [|System.Nullable(Of Integer)|]"))
        End Function

        <WorkItem(545603)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestNullableInImports2() As Task
            Await TestMissingAsync(
        NewLines("Imports [|System.Nullable(Of Integer)|]"))
        End Function

        <WorkItem(545795)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestColorColor1() As Task
            Await TestAsync(
        NewLines("Namespace N \n Class Color \n Shared Sub Foo() \n End Class \n  \n Class Program \n Shared Property Color As Color \n  \n Shared Sub Main() \n Dim c = [|N.Color.Foo|]() \n End Sub \n End Class \n End Namespace"),
        NewLines("Namespace N \n Class Color \n Shared Sub Foo() \n End Class \n  \n Class Program \n Shared Property Color As Color \n  \n Shared Sub Main() \n Dim c = Color.Foo() \n End Sub \n End Class \n End Namespace"))
        End Function

        <WorkItem(545795)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestColorColor2() As Task
            Await TestAsync(
        NewLines("Namespace N \n Class Color \n Shared Sub Foo() \n End Class \n  \n Class Program \n Shared Property Color As Color \n  \n Shared Sub Main() \n Dim c = [|N.Color.Foo|]() \n End Sub \n End Class \n End Namespace"),
        NewLines("Namespace N \n Class Color \n Shared Sub Foo() \n End Class \n  \n Class Program \n Shared Property Color As Color \n  \n Shared Sub Main() \n Dim c = Color.Foo() \n End Sub \n End Class \n End Namespace"))
        End Function

        <WorkItem(545795)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestColorColor3() As Task
            Await TestAsync(
        NewLines("Namespace N \n Class Color \n Shared Sub Foo() \n End Class \n  \n Class Program \n Shared Property Color As Color \n  \n Shared Sub Main() \n Dim c = [|N.Color.Foo|]() \n End Sub \n End Class \n End Namespace"),
        NewLines("Namespace N \n Class Color \n Shared Sub Foo() \n End Class \n  \n Class Program \n Shared Property Color As Color \n  \n Shared Sub Main() \n Dim c = Color.Foo() \n End Sub \n End Class \n End Namespace"))
        End Function

        <WorkItem(546829)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestKeyword1() As Task
            Await TestAsync(
        NewLines("Module m \n Sub main() \n Dim x = [|m.Equals|](1, 1) \n End Sub \n End Module"),
        NewLines("Module m \n Sub main() \n Dim x = Equals(1, 1) \n End Sub \n End Module"))
        End Function

        <WorkItem(546844)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestKeyword2() As Task
            Await TestAsync(
        NewLines("Module M \n Sub main() \n Dim x = [|M.Class|] \n End Sub \n Dim [Class] \n End Module"),
        NewLines("Module M \n Sub main() \n Dim x = [Class] \n End Sub \n Dim [Class] \n End Module"))
        End Function

        <WorkItem(546907)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestDoNotSimplifyNullableInMemberAccessExpression() As Task
            Await TestMissingAsync(
        NewLines("Imports System \n Module Program \n Dim x = [|Nullable(Of Guid).op_Implicit|](Nothing) \n End Module"))
        End Function

        <WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestMissingNullableSimplificationInsideCref() As Task
            Await TestMissingAsync(
"Imports System
''' <summary>
''' <see cref=""[|Nullable(Of T)|]""/>
''' </summary>
Class A
End Class")
        End Function

        <WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestMissingNullableSimplificationInsideCref2() As Task
            Await TestMissingAsync(
"''' <summary>
''' <see cref=""[|System.Nullable(Of T)|]""/>
''' </summary>
Class A
End Class")
        End Function

        <WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestMissingNullableSimplificationInsideCref3() As Task
            Await TestMissingAsync(
"''' <summary>
''' <see cref=""[|System.Nullable(Of T)|].Value""/>
''' </summary>
Class A
End Class")
        End Function

        <WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestMissingNullableSimplificationInsideCref4() As Task
            Await TestMissingAsync(
"Imports System
''' <summary>
''' <see cref=""[|Nullable(Of Integer)|].Value""/>
''' </summary>
Class A
End Class")
        End Function

        <WorkItem(2196, "https://github.com/dotnet/roslyn/issues/2196")>
        <WorkItem(2197, "https://github.com/dotnet/roslyn/issues/2197")>
        <WorkItem(29, "https: //github.com/dotnet/roslyn/issues/29")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestNullableSimplificationInsideCref() As Task
            ' NOTE: This will probably stop working if issues 2196 / 2197 related to VB compiler and semantic model are fixed.
            ' It is unclear whether Nullable(Of Integer) is legal in the below case. Currently the VB compiler allows this while
            ' C# doesn't allow similar case. If this Nullable(Of Integer) becomes illegal in VB in the below case then the simplification
            ' from Nullable(Of Integer) -> Integer will also stop working and the baseline for this test will have to be updated.
            Await TestAsync(
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
        End Function

        <WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")>
        <WorkItem(2189, "https://github.com/dotnet/roslyn/issues/2189")>
        <WorkItem(2196, "https://github.com/dotnet/roslyn/issues/2196")>
        <WorkItem(2197, "https://github.com/dotnet/roslyn/issues/2197")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestNullableSimplificationInsideCref2() As Task
            ' NOTE: This will probably stop working if issues 2196 / 2197 related to VB compiler and semantic model are fixed.
            ' It is unclear whether Nullable(Of Integer) is legal in the below case. Currently the VB compiler allows this while
            ' C# doesn't allow similar case. If this Nullable(Of Integer) becomes illegal in VB in the below case then the simplification
            ' from Nullable(Of Integer) -> Integer will also stop working and the baseline for this test will have to be updated.
            Await TestAsync(
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
        End Function

        <WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestNullableSimplificationInsideCref3() As Task
            Await TestAsync(
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
        End Function

        <WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestNullableSimplificationInsideCref4() As Task
            Await TestAsync(
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
        End Function

        <WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestNullableSimplificationInsideCref5() As Task
            Await TestAsync(
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
        End Function

        <WorkItem(29, "https://github.com/dotnet/roslyn/issues/29")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestNullableSimplificationInsideCref6() As Task
            Await TestAsync(
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
        End Function

        <WorkItem(529930)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestReservedNameInAttribute1() As Task
            Await TestMissingAsync(
        NewLines("<[|Global.Assembly|]> ' Simplify \n Class Assembly \n Inherits Attribute \n End Class"))
        End Function

        <WorkItem(529930)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestReservedNameInAttribute2() As Task
            Await TestMissingAsync(
        NewLines("<[|Global.Assembly|]> ' Simplify \n Class Assembly \n Inherits Attribute \n End Class"))
        End Function

        <WorkItem(529930)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestReservedNameInAttribute3() As Task
            Await TestMissingAsync(
        NewLines("<[|Global.Module|]> ' Simplify \n Class Module \n Inherits Attribute \n End Class"))
        End Function

        <WorkItem(529930)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestReservedNameInAttribute4() As Task
            Await TestMissingAsync(
        NewLines("<[|Global.Module|]> ' Simplify \n Class Module \n Inherits Attribute \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestAliasedType() As Task
            Dim source =
        NewLines("Class Program \n Sub Foo() \n Dim x As New [|Global.Program|] \n End Sub \n End Class")
            Await TestAsync(source,
        NewLines("Class Program \n Sub Foo() \n Dim x As New Program \n End Sub \n End Class"), Nothing, 0)

            Await TestMissingAsync(source, GetScriptOptions())
        End Function

        <WorkItem(674789)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestCheckForAssemblyNameInFullWidthIdentifier() As Task
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
            Await TestAsync(source.Value, expected.Value)
        End Function

        <WorkItem(568043)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestDontSimplifyNamesWhenThereAreParseErrors() As Task
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

            Await TestMissingAsync(source.Value)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestShowModuleNameAsUnnecessaryMemberAccess() As Task
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
            Await TestAsync(source.Value, expected.Value)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestShowModuleNameAsUnnecessaryQualifiedName() As Task
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

            Await TestAsync(source.Value, expected.Value)
        End Function

        <WorkItem(608200)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestBugfix_608200() As Task
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

            Await TestAsync(source.Value, expected.Value)
        End Function

        <WorkItem(578686)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestDontUseAlias() As Task
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

            Await TestMissingAsync(source.Value)
        End Function

        <WorkItem(547246)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestCreateCodeIssueWithProperIssueSpan() As Task
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

            Await TestAsync(source.Value, expected.Value)
            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromFile(source.Value, Nothing, Nothing)
                Dim diagnosticAndFix = Await GetDiagnosticAndFixAsync(workspace)
                Dim span = diagnosticAndFix.Item1.Location.SourceSpan
                Assert.NotEqual(span.Start, 0)
                Assert.NotEqual(span.End, 0)
            End Using
        End Function

        <WorkItem(629572)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestDoNotIncludeAliasNameIfLastTargetNameIsTheSame_1() As Task
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

            Await TestAsync(source.Value, expected.Value)
            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromFile(source.Value, Nothing, Nothing)
                Dim diagnosticAndFix = Await GetDiagnosticAndFixAsync(workspace)
                Dim span = diagnosticAndFix.Item1.Location.SourceSpan
                Assert.Equal(span.Start, expected.Value.ToString.Replace(vbLf, vbCrLf).IndexOf("new C", StringComparison.Ordinal) + 4)
                Assert.Equal(span.Length, "A.B".Length)
            End Using
        End Function

        <WorkItem(629572)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestDoNotIncludeAliasNameIfLastTargetNameIsTheSame_2() As Task
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

            Await TestAsync(source.Value, expected.Value)
            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromFile(source.Value, Nothing, Nothing)
                Dim diagnosticAndFix = Await GetDiagnosticAndFixAsync(workspace)
                Dim span = diagnosticAndFix.Item1.Location.SourceSpan
                Assert.Equal(span.Start, expected.Value.ToString.Replace(vbLf, vbCrLf).IndexOf("Console.WriteLine(""foo"")", StringComparison.Ordinal))
                Assert.Equal(span.Length, "System".Length)
            End Using
        End Function

        <WorkItem(686306)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestDontSimplifyNameSyntaxToTypeSyntaxInVBCref() As Task
            Dim source =
        <Code>
Imports System
''' &lt;see cref="[|Object|]"/>
Module Program
End Module
</Code>

            Await TestMissingAsync(source.Value)
        End Function

        <WorkItem(721817)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestDontSimplifyNameSyntaxToPredefinedTypeSyntaxInVBCref() As Task
            Dim source =
        <Code>
Public Class Test
    '''&lt;summary&gt;
    ''' &lt;see cref="[|Test.ReferenceEquals|](Object, Object)"/&gt;   
    ''' &lt;/Code&gt;
        Public Async Function TestFoo() As Task

        End Sub

    End Class


</Code>

            Await TestMissingAsync(source.Value)
        End Function

        <WorkItem(721694)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestEnableReducersInsideVBCref() As Task
            Dim source =
        <Code>
Public Class Test_Dev11
    '''&lt;summary&gt;
    ''' &lt;see cref="[|Global.Microsoft|].VisualBasic.Left"/&gt;
    ''' &lt;/Code&gt;
        Public Async Function Testtestscenarios() As Task
        End Sub
    End Class
</Code>

            Dim expected =
        <Code>
Public Class Test_Dev11
    '''&lt;summary&gt;
    ''' &lt;see cref="Microsoft.VisualBasic.Left"/&gt;
    ''' &lt;/Code&gt;
        Public Async Function Testtestscenarios() As Task
        End Sub
    End Class
</Code>
            Await TestAsync(source.Value, expected.Value)
        End Function

        <WorkItem(736377)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestDontSimplifyTypeNameBrokenCode() As Task
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

            Await TestMissingAsync(source.Value)
        End Function

        <WorkItem(860565)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestSimplifyGenericTypeName_Bug860565() As Task
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

            Await TestMissingAsync(source.Value)
        End Function

        <WorkItem(813385)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestDontSimplifyAliases() As Task
            Dim source =
        <Code>
            <![CDATA[
Imports Foo = System.Int32

Class P
    Dim F As [|Foo|]
End Class
]]>
        </Code>

            Await TestMissingAsync(source.Value)
        End Function

        <WorkItem(942568)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestIntrinsicTypesInLocalDeclarationDefaultValue_1() As Task
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

            Await TestAsync(source.Value, expected.Value, compareTokens:=False)
        End Function

        <WorkItem(942568)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestIntrinsicTypesInLocalDeclarationDefaultValue_2() As Task
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

            Await TestAsync(source.Value, expected.Value, compareTokens:=False)
        End Function

        <WorkItem(942568)>
        <WorkItem(954536)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestIntrinsicTypesInCref1() As Task
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

            Await TestAsync(source.Value, expected.Value, compareTokens:=False)
        End Function

        <WorkItem(942568)>
        <WorkItem(954536)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestIntrinsicTypesInCref2() As Task
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

            Await TestAsync(source.Value, expected.Value, compareTokens:=False)
        End Function

        <WorkItem(1012713)>
        <WorkItem(942568)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestIntrinsicTypesInCref3() As Task
            Dim source =
        <Code>
''' &lt;see cref="[|System.Int32|].MaxValue"/>
Module Program
End Module
</Code>

            Await TestMissingAsync(source.Value)
        End Function

        <WorkItem(942568)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestIntrinsicTypesInLocalDeclarationNonDefaultValue_1() As Task
            Dim source =
        <Code>
Class Program
    Private x As [|System.Int32|]
    Sub Main(args As System.Int32)
        Dim a As System.Int32 = 9
    End Sub
End Class
</Code>
            Await TestMissingAsync(source.Value, options:=New Dictionary(Of OptionKey, Object) From {{New OptionKey(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, LanguageNames.VisualBasic), False}})
        End Function

        <WorkItem(942568)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestIntrinsicTypesInLocalDeclarationNonDefaultValue_2() As Task
            Dim source =
        <Code>
Class Program
    Private x As System.Int32
    Sub Main(args As [|System.Int32|])
        Dim a As System.Int32 = 9
    End Sub
End Class
</Code>
            Await TestMissingAsync(source.Value, options:=New Dictionary(Of OptionKey, Object) From {{New OptionKey(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, LanguageNames.VisualBasic), False}})
        End Function

        <WorkItem(942568)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestIntrinsicTypesInLocalDeclarationNonDefaultValue_3() As Task
            Dim source =
        <Code>
Class Program
    Private x As System.Int32
    Sub Main(args As System.Int32)
        Dim a As [|System.Int32|] = 9
    End Sub
End Class
</Code>
            Await TestMissingAsync(source.Value, options:=New Dictionary(Of OptionKey, Object) From {{New OptionKey(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, LanguageNames.VisualBasic), False}})
        End Function

        <WorkItem(942568)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestIntrinsicTypesInMemberAccess_Default_1() As Task
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
            Await TestAsync(source.Value, expected.Value, compareTokens:=False)
        End Function

        <WorkItem(942568)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestIntrinsicTypesInMemberAccess_Default_2() As Task
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
            Await TestAsync(source.Value, expected.Value, compareTokens:=False)
        End Function

        <WorkItem(956667)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestIntrinsicTypesInMemberAccess_Default_3() As Task
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
            Await TestMissingAsync(source.Value)
        End Function

        <WorkItem(942568)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestIntrinsicTypesInMemberAccess_NonDefault_1() As Task
            Dim source =
        <Code>
Module Program
    Sub Main(args As String())
        Dim s = [|System.Int32|].MaxValue
    End Sub
End Module
</Code>
            Await TestMissingAsync(source.Value, options:=New Dictionary(Of OptionKey, Object) From {{New OptionKey(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, LanguageNames.VisualBasic), False}})
        End Function

        <WorkItem(942568)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestIntrinsicTypesInMemberAccess_NonDefault_2() As Task
            Dim source =
        <Code>
Imports System
Module Program
    Sub Main(args As String())
        Dim s = [|Int32|].MaxValue
    End Sub
End Module
</Code>
            Await TestMissingAsync(source.Value, options:=New Dictionary(Of OptionKey, Object) From {{New OptionKey(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, LanguageNames.VisualBasic), False}})
        End Function

        <WorkItem(954536)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestIntrinsicTypesInCref_NonDefault_1() As Task
            Dim source =
        <Code>
''' &lt;see cref="[|System.Int32|]"/>
Module Program
End Module
</Code>

            Await TestMissingAsync(source.Value, options:=New Dictionary(Of OptionKey, Object) From {{New OptionKey(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, LanguageNames.VisualBasic), False}})
        End Function

        <WorkItem(954536)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestIntrinsicTypesInCref_NonDefault_2() As Task
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

            Await TestAsync(source.Value, expected.Value, options:=New Dictionary(Of OptionKey, Object) From {{New OptionKey(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, LanguageNames.VisualBasic), False}})
        End Function

        <WorkItem(954536)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestIntrinsicTypesInCref_NonDefault_3() As Task
            Dim source =
        <Code>
''' &lt;see cref="System.Collections.Generic.List(Of T).CopyTo(Integer, T(), Integer, [|System.Int32|])"/>
Module Program
End Module
</Code>

            Await TestMissingAsync(source.Value, options:=New Dictionary(Of OptionKey, Object) From {{New OptionKey(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, LanguageNames.VisualBasic), False}})
        End Function

        <WorkItem(954536)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestIntrinsicTypesInCref_NonDefault_4() As Task
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

            Await TestAsync(source.Value, expected.Value, options:=New Dictionary(Of OptionKey, Object) From {{New OptionKey(SimplificationOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, LanguageNames.VisualBasic), False}})
        End Function

        <WorkItem(965208)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestSimplifyDiagnosticId() As Task
            Dim source =
        <Code>
Imports System
Module Program
    Sub Main(args As String())
        [|System.Console.WriteLine|]("")
    End Sub
End Module
</Code>

            Using workspace = Await CreateWorkspaceFromFileAsync(source, Nothing, Nothing)
                Dim diagnostics = (Await GetDiagnosticsAsync(workspace)).Where(Function(d) d.Id = IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId)
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

            Using workspace = Await CreateWorkspaceFromFileAsync(source, Nothing, Nothing)
                Dim diagnostics = (Await GetDiagnosticsAsync(workspace)).Where(Function(d) d.Id = IDEDiagnosticIds.SimplifyNamesDiagnosticId)
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

            Using workspace = Await CreateWorkspaceFromFileAsync(source, Nothing, Nothing)
                Dim diagnostics = (Await GetDiagnosticsAsync(workspace)).Where(Function(d) d.Id = IDEDiagnosticIds.SimplifyThisOrMeDiagnosticId)
                Assert.Equal(1, diagnostics.Count)
            End Using
        End Function

        <WorkItem(995168)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestSimplifyToPredefinedTypeNameShouldNotBeOfferedInsideNameOf1() As Task
            Await TestMissingAsync("Imports System
Module Program
    Sub Main()
        Dim x = NameOf([|Int32|])
    End Sub
End Module")
        End Function

        <WorkItem(995168)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestSimplifyToPredefinedTypeNameShouldNotBeOfferedInsideNameOf2() As Task
            Await TestMissingAsync("
Module Program
    Sub Main()
        Dim x = NameOf([|System.Int32|])
    End Sub
End Module")
        End Function

        <WorkItem(995168)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestSimplifyToPredefinedTypeNameShouldNotBeOfferedInsideNameOf3() As Task
            Await TestMissingAsync("Imports System
Module Program
    Sub Main()
        Dim x = NameOf([|Int32|].MaxValue)
    End Sub
End Module")
        End Function

        <WorkItem(995168)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Async Function TestSimplifyTypeNameInsideNameOf() As Task
            Await TestAsync("Imports System
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
        End Function
    End Class
End Namespace
