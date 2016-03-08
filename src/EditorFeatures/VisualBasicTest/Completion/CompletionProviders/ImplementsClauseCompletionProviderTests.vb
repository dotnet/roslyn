' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    Public Class ImplementsClauseCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Friend Overrides Function CreateCompletionProvider() As CompletionListProvider
            Return New ImplementsClauseCompletionProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSuggestInterfaces() As Task
            Dim text = <text>Interface I
    Sub Foo()
End Interface

Interface J
    Sub Bar()
End Interface

Class C
    Implements I, J
    Public Sub test Implements $$
End Class</text>.Value

            Await VerifyItemExistsAsync(text, "I")
            Await VerifyItemExistsAsync(text, "J")
        End Function

        <WorkItem(995986, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/995986")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSuggestAliasedInterfaces() As Task
            Dim text = <text>Imports IAliasToI = I
Imports IAliasToJ = J
Interface I
    Sub Foo()
End Interface

Interface J
    Sub Bar()
End Interface

Class C
    Implements I, J
    Public Sub test Implements $$
End Class</text>.Value

            Await VerifyItemExistsAsync(text, "IAliasToI")
            Await VerifyItemExistsAsync(text, "IAliasToJ")
            Await VerifyItemExistsAsync(text, "I")
            Await VerifyItemExistsAsync(text, "J")
        End Function

        <WorkItem(995986, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/995986")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSuggestAliasedNamespace() As Task
            Dim text = <text>Imports AliasedNS = NS
Namespace NS
    Interface I
        Sub Foo()
    End Interface

    Interface J
        Sub Bar()
    End Interface

    Class C
        Implements I, J
        Public Sub test Implements $$
    End Class
End Namespace</text>.Value

            Await VerifyItemExistsAsync(text, "AliasedNS")
            Await VerifyItemExistsAsync(text, "NS")
            Await VerifyItemExistsAsync(text, "I")
            Await VerifyItemExistsAsync(text, "J")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSubSuggestSub() As Task
            Dim text = <text>Interface I
    Sub Foo()
    Function Bar()
End Interface

Class C
    Implements I
    Public Sub test Implements I.$$
End Class</text>.Value

            Await VerifyItemExistsAsync(text, "Foo")
            Await VerifyItemIsAbsentAsync(text, "Bar")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestFunctionSuggestFunction() As Task
            Dim text = <text>Interface I
    Sub Foo()
    Function Bar()
End Interface

Class C
    Implements I
    Public Function test as Integer Implements I.$$
End Class</text>.Value

            Await VerifyItemExistsAsync(text, "Bar")
            Await VerifyItemIsAbsentAsync(text, "Foo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSuggestClassContainingInterface() As Task
            Dim text = <text>Public Class B
    Public Interface I
        Sub Foo()
        Function Bar()
    End Interface
End Class

Class C
    Implements B.I
    Public Function test as Integer Implements $$
End Class</text>.Value

            Await VerifyItemExistsAsync(text, "B")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDoNotSuggestAlreadyImplementedMember() As Task
            Dim text = <text>Interface I
    Sub Foo()
    Function Bar()
End Interface


Class C
    Implements I
    Public Sub test Implements I.Foo
    End Sub
            
    Public Sub blah Implements I.$$
End Class</text>.Value

            Await VerifyNoItemsExistAsync(text)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoInterfaceImplementations() As Task
            Dim text = <text>Interface I
    Sub Foo()
    Function Bar()
End Interface


Class C
    Public Function test as Integer Implements $$
End Class</text>.Value

            Await VerifyNoItemsExistAsync(text)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestPropertyImplementation() As Task
            Dim text = <text>Interface I
    Sub Foo()
    Property Green() as Integer
End Interface


Class C
    Implements I
    Public Property test as Integer Implements I.$$
End Class</text>.Value

            Await VerifyItemExistsAsync(text, "Green")
            Await VerifyItemIsAbsentAsync(text, "Foo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEventImplementation() As Task
            Dim text = <text>Interface I
    Sub Foo()
    Event Green()
End Interface


Class C
    Implements I
    Public Event test Implements I.$$
End Class</text>.Value

            Await VerifyItemExistsAsync(text, "Green")
            Await VerifyItemIsAbsentAsync(text, "Foo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAfterGlobal() As Task
            Dim text = <text>Interface I
    Sub Foo()
    Event Green()
End Interface


Class C
    Implements I
    Public Event test Implements Global.$$
End Class</text>.Value

            Await VerifyItemExistsAsync(text, "I")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(546391, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546391")>
        Public Async Function TestAfterProperty() As Task
            Dim text = <text>Imports System
Imports System.Runtime.InteropServices
Public Interface IA
    Property P() As Object
End Interface
Public Class A
    Implements IA
    Property P() As Object Implements $$
        Get
            Return Nothing
        End Get
        Set(value As Object)
        End Set
    End Property
End Class
</text>.Value

            Await VerifyItemExistsAsync(text, "IA")
            Await VerifyItemExistsAsync(text, "Global")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(546410, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546410")>
        Public Async Function TestSuggestionInImplementsList() As Task
            Dim text = <text>Imports System
Interface I
    Sub Foo()
End Interface
Interface J
    Sub Baz()
End Interface
Class C
    Implements I, J

    Public Sub foo() Implements I.Foo, $$

End Class
</text>.Value
            Await VerifyItemExistsAsync(text, "J", Nothing, Nothing, True)
            Await VerifyItemIsAbsentAsync(text, "I", Nothing, Nothing, True)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(546413, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546413")>
        Public Async Function TestNestedInterface() As Task
            Dim text = <text>Namespace Outer
    Namespace Inner
        Public Interface I
            Sub Foo()
            Public Interface J
                Sub Bar()
            End Interface
        End Interface
    End Namespace
End Namespace
Class C
    Implements Outer.Inner.I, Outer.Inner.I.J

    Public Sub A() Implements $$
End Class</text>.Value
            Await VerifyItemExistsAsync(text, "Outer")
            Await VerifyItemIsAbsentAsync(text, "Inner")
            Await VerifyItemIsAbsentAsync(text, "I")
            Await VerifyItemIsAbsentAsync(text, "J")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(546413, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546413")>
        Public Async Function TestNoNestedInterface() As Task
            Dim text = <text>Public Class B
    Public Interface I
        Sub Foo()
    End Interface
End Class
Class C
    Implements B.I
    Public Sub Foo() Implements $$
    End Sub
End Class

</text>.Value
            Await VerifyItemExistsAsync(text, "B")
            Await VerifyItemIsAbsentAsync(text, "I")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(546405, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546405")>
        Public Async Function TestDotIntoGlobal() As Task
            Dim text = <text>Imports System
Class C
    Implements ICloneable
    Public Function Cl() As Object Implements Global.$$
    End Function
End Class
</text>.Value
            Await VerifyItemExistsAsync(text, "System")
            Await VerifyItemIsAbsentAsync(text, "I")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(546415, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546415")>
        Public Async Function TestInheritedInterfaceMembers() As Task
            Dim text = <text>Interface I1
    Function Bar() As Object
End Interface
Interface I2
    Inherits I1
End Interface
Class C
    Implements I2
    Public Function Bar() As Object Implements $$

End Class

</text>.Value
            Await VerifyItemExistsAsync(text, "I1")
            Await VerifyItemExistsAsync(text, "I2")
            Await VerifyItemExistsAsync(text, "Global")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(546415, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546415")>
        <WorkItem(546488, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546488")>
        Public Async Function TestInheritedInterfaceMembers2() As Task
            Dim text = <text>Interface I1
    Function Bar() As Object
End Interface
Interface I2
    Inherits I1
End Interface
Class C
    Implements I2
    Public Function Bar() As Object Implements I1.$$

End Class

</text>.Value
            Await VerifyItemExistsAsync(text, "Bar")
            Await VerifyItemIsAbsentAsync(text, "Equals")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(546415, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546415")>
        Public Async Function TestInheritedInterface() As Task
            Dim text = <text>Interface I1
    Function Bar() As Object
End Interface

Interface I2
    Inherits I1
End Interface

Class C
    Implements I2
    Public Function Bar() As Object Implements $$

End Class</text>.Value
            Await VerifyItemExistsAsync(text, "I1")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(530353, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530353")>
        Public Async Function TestNothingToImplement() As Task
            Dim text = <text>Interface I
    Sub Foo()
    Sub Bar()
End Interface

Class C
    Implements I

    Sub f1() Implements I.Foo
    End Sub

    Sub f2() Implements I.Bar
    End Sub

    Sub f3() Implements $$

End Class</text>.Value
            Await VerifyItemExistsAsync(text, "I")
            Await VerifyItemExistsAsync(text, "Global")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(546431, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546431")>
        Public Async Function TestNextToImplicitLineContinuation() As Task
            Dim text = <text>Public Interface I2
    Function Goo() As Boolean
End Interface
Public Class Cls1
    Implements I2
    Function Cl1Goo() As Boolean Implements I2.$$
        Console.WriteLine()
    End Function
End Class
</text>.Value
            Await VerifyItemExistsAsync(text, "Goo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(546431, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546431")>
        Public Async Function TestNextToImplicitLineContinuation2() As Task
            Dim text = <text>Public Interface I2
    Function Foo() As Boolean
End Interface
Public Class Cls1
    Implements I2
    Function F() As Boolean Implements Global.$$
        Console.WriteLine()
    End Function
End Class

</text>.Value
            Await VerifyItemExistsAsync(text, "I2")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(546406, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546406")>
        Public Async Function TestDisplayTypeArguments() As Task
            Dim text = <text>Imports System
Class A
    Implements IEquatable(Of Integer)
    Public Function Equals(other As Integer) As Boolean Implements $$
End Class


</text>.Value
            Await VerifyItemExistsAsync(text, "IEquatable(Of Integer)")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(546406, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546406")>
        Public Async Function TestCommitTypeArgumentsOnParen() As Task
            Dim text = <text>Imports System
Class A
    Implements IEquatable(Of Integer)
    Public Function Equals(other As Integer) As Boolean Implements $$
End Class</text>.Value

            Dim expected = <text>Imports System
Class A
    Implements IEquatable(Of Integer)
    Public Function Equals(other As Integer) As Boolean Implements IEquatable(
End Class</text>.Value

            Await VerifyProviderCommitAsync(text, "IEquatable(Of Integer)", expected, "("c, "")
        End Function

        <WorkItem(546802, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546802")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestKeywordIdentifierShowUnescaped() As Task
            Dim text = <text>Interface [Interface]
    Sub Foo()
    Function Bar()
End Interface

Class C
    Implements [Interface]
    Public Sub test Implements $$
End Class</text>.Value

            Await VerifyItemExistsAsync(text, "Interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestKeywordIdentifierCommitEscaped() As Task
            Dim text = <text>Interface [Interface]
    Sub Foo()
    Function Bar()
End Interface

Class C
    Implements [Interface]
    Public Sub test Implements $$
End Class</text>.Value

            Dim expected = <text>Interface [Interface]
    Sub Foo()
    Function Bar()
End Interface

Class C
    Implements [Interface]
    Public Sub test Implements [Interface].
End Class</text>.Value

            Await VerifyProviderCommitAsync(text, "Interface", expected, "."c, "")
        End Function

        <WorkItem(543812, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543812")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEventsAfterDotInImplementsClause() As Task
            Dim markup = <Text>
Interface i
    Event myevent()
End Interface
Class base
    Event myevent()
End Class
Class C1(Of t)
    Implements i
    Event myevent Implements i.$$
</Text>.Value

            Await VerifyItemExistsAsync(markup, "myevent")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInterfaceImplementsSub() As Task
            Dim test = <Text>
Interface IFoo
    Sub S1()
    Function F1() As Integer
    Property P1 As Integer
End Interface

Class C : Implements IFoo
    Sub S() Implements IFoo.$$
End Class
</Text>.Value

            Await VerifyItemExistsAsync(test, "S1")
            Await VerifyItemIsAbsentAsync(test, "F1")
            Await VerifyItemIsAbsentAsync(test, "P1")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInterfaceImplementsFunction() As Task
            Dim test = <Text>
Interface IFoo
    Sub S1()
    Function F1() As Integer
    Property P1 As Integer
End Interface

Class C : Implements IFoo
    Function F() As Integer Implements IFoo.$$
End Class
</Text>.Value

            Await VerifyItemIsAbsentAsync(test, "S1")
            Await VerifyItemExistsAsync(test, "F1")
            Await VerifyItemIsAbsentAsync(test, "P1")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInterfaceImplementsProperty() As Task
            Dim test = <Text>
Interface IFoo
    Sub S1()
    Function F1() As Integer
    Property P1 As Integer
End Interface

Class C : Implements IFoo
    Property P As Integer Implements IFoo.$$
End Class
</Text>.Value

            Await VerifyItemIsAbsentAsync(test, "S1")
            Await VerifyItemIsAbsentAsync(test, "F1")
            Await VerifyItemExistsAsync(test, "P1")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestVerifyDescription() As Task
            Dim test = <Text><![CDATA[
Interface IFoo
    ''' <summary>
    ''' Some Summary
    ''' </summary>
    Sub Bar()
End Interface

Class SomeClass
    Implements IFoo
    Public Sub something() Implements IFoo.$$
    End Sub
End Class
                       ]]></Text>

            Await VerifyItemExistsAsync(test.Value, "Bar", "Sub IFoo.Bar()" & vbCrLf & "Some Summary")
        End Function

        <WorkItem(530507, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530507")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestRootNamespaceInDefaultListing() As Task

            Dim element =
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true">
                        <CompilationOptions RootNamespace="Workcover.Licensing"/>
                        <Document FilePath="document">
Imports Workcover.Licensing

Public Class Class1
    Implements WorkflowHandler

    Public ReadOnly Property ID As Guid Implements Workcover.Licensing.WorkflowHandler.ID
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public Sub ShowInterfaceToStartAnItem() Implements Workcover.Licensing.WorkflowHandler.ShowInterfaceToStartAnItem, $$
        Throw New NotImplementedException()
    End Sub
End Class

Interface WorkflowHandler
    ReadOnly Property ID() As Guid
    Sub ShowInterfaceToStartAnItem()

End Interface
                        </Document>
                    </Project>
                </Workspace>

            Using workspace = Await TestWorkspace.CreateAsync(element)
                Dim position = workspace.Documents.Single().CursorPosition.Value
                Dim document = workspace.CurrentSolution.GetDocument(workspace.Documents.Single().Id)
                Dim triggerInfo = New CompletionTriggerInfo()

                Dim completionList = Await GetCompletionListAsync(document, position, triggerInfo)
                AssertEx.Any(completionList.Items, Function(c) c.DisplayText = "Workcover")

            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotInTrivia() As Task
            Dim text = <text>Interface I
    Sub Foo()
End Interface

Interface J
    Sub Bar()
End Interface

Class C
    Implements I, J
    Public Sub test Implements '$$
End Class</text>.Value

            Await VerifyNoItemsExistAsync(text)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestReimplementInterfaceImplementedByBase() As Task
            Dim text = <text>Interface I
    Sub Foo()
End Interface

Class B
    Implements I

    Public Sub Foo() Implements I.Foo
        Throw New NotImplementedException()
    End Sub
End Class

Class D
    Inherits B
    Implements I

    Sub Bar() Implements $$
End Class</text>.Value

            Await VerifyItemExistsAsync(text, "I")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestReimplementInterfaceImplementedByBase2() As Task
            Dim text = <text>Interface I
    Sub Foo()
    Sub Quux()
End Interface

Class B
    Implements I

    Public Sub Foo() Implements I.Foo
        Throw New NotImplementedException()
    End Sub

    Public Sub Quux() Implements I.Quux
        Throw New NotImplementedException()
    End Sub
End Class

Class D
    Inherits B
    Implements I

    Sub Foo2() Implements I.Foo
    End Sub

    Sub Bar() Implements I.$$
End Class</text>.Value

            Await VerifyItemExistsAsync(text, "Quux")
        End Function
    End Class
End Namespace