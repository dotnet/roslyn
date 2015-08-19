' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SuggestInterfaces()
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

            VerifyItemExists(text, "I")
            VerifyItemExists(text, "J")
        End Sub

        <WorkItem(995986)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SuggestAliasedInterfaces()
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

            VerifyItemExists(text, "IAliasToI")
            VerifyItemExists(text, "IAliasToJ")
            VerifyItemExists(text, "I")
            VerifyItemExists(text, "J")
        End Sub

        <WorkItem(995986)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SuggestAliasedNamespace()
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

            VerifyItemExists(text, "AliasedNS")
            VerifyItemExists(text, "NS")
            VerifyItemExists(text, "I")
            VerifyItemExists(text, "J")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SubSuggestSub()
            Dim text = <text>Interface I
    Sub Foo()
    Function Bar()
End Interface

Class C
    Implements I
    Public Sub test Implements I.$$
End Class</text>.Value

            VerifyItemExists(text, "Foo")
            VerifyItemIsAbsent(text, "Bar")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub FunctionSuggestFunction()
            Dim text = <text>Interface I
    Sub Foo()
    Function Bar()
End Interface

Class C
    Implements I
    Public Function test as Integer Implements I.$$
End Class</text>.Value

            VerifyItemExists(text, "Bar")
            VerifyItemIsAbsent(text, "Foo")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SuggestClassContainingInterface()
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

            VerifyItemExists(text, "B")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub DoNotSuggestAlreadyImplementedMember()
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

            VerifyNoItemsExist(text)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoInterfaceImplementations()
            Dim text = <text>Interface I
    Sub Foo()
    Function Bar()
End Interface


Class C
    Public Function test as Integer Implements $$
End Class</text>.Value

            VerifyNoItemsExist(text)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub PropertyImplementation()
            Dim text = <text>Interface I
    Sub Foo()
    Property Green() as Integer
End Interface


Class C
    Implements I
    Public Property test as Integer Implements I.$$
End Class</text>.Value

            VerifyItemExists(text, "Green")
            VerifyItemIsAbsent(text, "Foo")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EventImplementation()
            Dim text = <text>Interface I
    Sub Foo()
    Event Green()
End Interface


Class C
    Implements I
    Public Event test Implements I.$$
End Class</text>.Value

            VerifyItemExists(text, "Green")
            VerifyItemIsAbsent(text, "Foo")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AfterGlobal()
            Dim text = <text>Interface I
    Sub Foo()
    Event Green()
End Interface


Class C
    Implements I
    Public Event test Implements Global.$$
End Class</text>.Value

            VerifyItemExists(text, "I")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(546391)>
        Public Sub AfterProperty()
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

            VerifyItemExists(text, "IA")
            VerifyItemExists(text, "Global")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(546410)>
        Public Sub SuggestionInImplementsList()
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
            VerifyItemExists(text, "J", Nothing, Nothing, True)
            VerifyItemIsAbsent(text, "I", Nothing, Nothing, True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(546413)>
        Public Sub NestedInterface()
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
            VerifyItemExists(text, "Outer")
            VerifyItemIsAbsent(text, "Inner")
            VerifyItemIsAbsent(text, "I")
            VerifyItemIsAbsent(text, "J")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(546413)>
        Public Sub NoNestedInterface()
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
            VerifyItemExists(text, "B")
            VerifyItemIsAbsent(text, "I")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(546405)>
        Public Sub DotIntoGlobal()
            Dim text = <text>Imports System
Class C
    Implements ICloneable
    Public Function Cl() As Object Implements Global.$$
    End Function
End Class
</text>.Value
            VerifyItemExists(text, "System")
            VerifyItemIsAbsent(text, "I")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(546415)>
        Public Sub InheritedInterfaceMembers()
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
            VerifyItemExists(text, "I1")
            VerifyItemExists(text, "I2")
            VerifyItemExists(text, "Global")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(546415)>
        <WorkItem(546488)>
        Public Sub InheritedInterfaceMembers2()
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
            VerifyItemExists(text, "Bar")
            VerifyItemIsAbsent(text, "Equals")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(546415)>
        Public Sub InheritedInterface()
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
            VerifyItemExists(text, "I1")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(530353)>
        Public Sub NothingToImplement()
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
            VerifyItemExists(text, "I")
            VerifyItemExists(text, "Global")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(546431)>
        Public Sub NextToImplicitLineContinuation()
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
            VerifyItemExists(text, "Goo")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(546431)>
        Public Sub NextToImplicitLineContinuation2()
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
            VerifyItemExists(text, "I2")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(546406)>
        Public Sub DisplayTypeArguments()
            Dim text = <text>Imports System
Class A
    Implements IEquatable(Of Integer)
    Public Function Equals(other As Integer) As Boolean Implements $$
End Class


</text>.Value
            VerifyItemExists(text, "IEquatable(Of Integer)")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(546406)>
        Public Sub CommitTypeArgumentsOnParen()
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

            VerifyProviderCommit(text, "IEquatable(Of Integer)", expected, "("c, "")
        End Sub

        <WorkItem(546802)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub KeywordIdentifierShowUnescaped()
            Dim text = <text>Interface [Interface]
    Sub Foo()
    Function Bar()
End Interface

Class C
    Implements [Interface]
    Public Sub test Implements $$
End Class</text>.Value

            VerifyItemExists(text, "Interface")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub KeywordIdentifierCommitEscaped()
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

            VerifyProviderCommit(text, "Interface", expected, "."c, "")
        End Sub

        <WorkItem(543812)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EventsAfterDotInImplementsClause()
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

            VerifyItemExists(markup, "myevent")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InterfaceImplementsSub()
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

            VerifyItemExists(test, "S1")
            VerifyItemIsAbsent(test, "F1")
            VerifyItemIsAbsent(test, "P1")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InterfaceImplementsFunction()
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

            VerifyItemIsAbsent(test, "S1")
            VerifyItemExists(test, "F1")
            VerifyItemIsAbsent(test, "P1")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InterfaceImplementsProperty()
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

            VerifyItemIsAbsent(test, "S1")
            VerifyItemIsAbsent(test, "F1")
            VerifyItemExists(test, "P1")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub VerifyDescription()
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

            VerifyItemExists(test.Value, "Bar", "Sub IFoo.Bar()" & vbCrLf & "Some Summary")
        End Sub

        <WorkItem(530507)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub RootNamespaceInDefaultListing()

            Dim workspace =
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

            Using testWorkspace = TestWorkspaceFactory.CreateWorkspace(workspace)
                Dim position = testWorkspace.Documents.Single().CursorPosition.Value
                Dim document = testWorkspace.CurrentSolution.GetDocument(testWorkspace.Documents.Single().Id)
                Dim triggerInfo = New CompletionTriggerInfo()

                Dim completionList = GetCompletionList(document, position, triggerInfo)
                AssertEx.Any(completionList.Items, Function(c) c.DisplayText = "Workcover")

            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NotInTrivia()
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

            VerifyNoItemsExist(text)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ReimplementInterfaceImplementedByBase()
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

            VerifyItemExists(text, "I")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub ReimplementInterfaceImplementedByBase2()
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

            VerifyItemExists(text, "Quux")
        End Sub
    End Class
End Namespace


