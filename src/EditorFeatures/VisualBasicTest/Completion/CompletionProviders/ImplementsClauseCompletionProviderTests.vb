' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    <Trait(Traits.Feature, Traits.Features.Completion)>
    Public Class ImplementsClauseCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Friend Overrides Function GetCompletionProviderType() As Type
            Return GetType(ImplementsClauseCompletionProvider)
        End Function

        <Fact>
        Public Async Function TestSuggestInterfaces() As Task
            Dim text = <text>Interface I
    Sub Goo()
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/995986")>
        Public Async Function TestSuggestAliasedInterfaces() As Task
            Dim text = <text>Imports IAliasToI = I
Imports IAliasToJ = J
Interface I
    Sub Goo()
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/995986")>
        Public Async Function TestSuggestAliasedNamespace() As Task
            Dim text = <text>Imports AliasedNS = NS
Namespace NS
    Interface I
        Sub Goo()
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

        <Fact>
        Public Async Function TestSubSuggestSub() As Task
            Dim text = <text>Interface I
    Sub Goo()
    Function Bar()
End Interface

Class C
    Implements I
    Public Sub test Implements I.$$
End Class</text>.Value

            Await VerifyItemExistsAsync(text, "Goo")
            Await VerifyItemIsAbsentAsync(text, "Bar")
        End Function

        <Fact>
        Public Async Function TestFunctionSuggestFunction() As Task
            Dim text = <text>Interface I
    Sub Goo()
    Function Bar()
End Interface

Class C
    Implements I
    Public Function test as Integer Implements I.$$
End Class</text>.Value

            Await VerifyItemExistsAsync(text, "Bar")
            Await VerifyItemIsAbsentAsync(text, "Goo")
        End Function

        <Fact>
        Public Async Function TestSuggestClassContainingInterface() As Task
            Dim text = <text>Public Class B
    Public Interface I
        Sub Goo()
        Function Bar()
    End Interface
End Class

Class C
    Implements B.I
    Public Function test as Integer Implements $$
End Class</text>.Value

            Await VerifyItemExistsAsync(text, "B")
        End Function

        <Fact>
        Public Async Function TestDoNotSuggestAlreadyImplementedMember() As Task
            Dim text = <text>Interface I
    Sub Goo()
    Function Bar()
End Interface


Class C
    Implements I
    Public Sub test Implements I.Goo
    End Sub
            
    Public Sub blah Implements I.$$
End Class</text>.Value

            Await VerifyNoItemsExistAsync(text)
        End Function

        <Fact>
        Public Async Function TestNoInterfaceImplementations() As Task
            Dim text = <text>Interface I
    Sub Goo()
    Function Bar()
End Interface


Class C
    Public Function test as Integer Implements $$
End Class</text>.Value

            Await VerifyNoItemsExistAsync(text)
        End Function

        <Fact>
        Public Async Function TestPropertyImplementation() As Task
            Dim text = <text>Interface I
    Sub Goo()
    Property Green() as Integer
End Interface


Class C
    Implements I
    Public Property test as Integer Implements I.$$
End Class</text>.Value

            Await VerifyItemExistsAsync(text, "Green")
            Await VerifyItemIsAbsentAsync(text, "Goo")
        End Function

        <Fact>
        Public Async Function TestEventImplementation() As Task
            Dim text = <text>Interface I
    Sub Goo()
    Event Green()
End Interface


Class C
    Implements I
    Public Event test Implements I.$$
End Class</text>.Value

            Await VerifyItemExistsAsync(text, "Green")
            Await VerifyItemIsAbsentAsync(text, "Goo")
        End Function

        <Fact>
        Public Async Function TestAfterGlobal() As Task
            Dim text = <text>Interface I
    Sub Goo()
    Event Green()
End Interface


Class C
    Implements I
    Public Event test Implements Global.$$
End Class</text>.Value

            Await VerifyItemExistsAsync(text, "I")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546391")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546410")>
        Public Async Function TestSuggestionInImplementsList() As Task
            Dim text = <text>Imports System
Interface I
    Sub Goo()
End Interface
Interface J
    Sub Baz()
End Interface
Class C
    Implements I, J

    Public Sub goo() Implements I.Goo, $$

End Class
</text>.Value
            Await VerifyItemExistsAsync(text, "J", Nothing, Nothing, True)
            Await VerifyItemIsAbsentAsync(text, "I", Nothing, Nothing, True)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546413")>
        Public Async Function TestNestedInterface() As Task
            Dim text = <text>Namespace Outer
    Namespace Inner
        Public Interface I
            Sub Goo()
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546413")>
        Public Async Function TestNoNestedInterface() As Task
            Dim text = <text>Public Class B
    Public Interface I
        Sub Goo()
    End Interface
End Class
Class C
    Implements B.I
    Public Sub Goo() Implements $$
    End Sub
End Class

</text>.Value
            Await VerifyItemExistsAsync(text, "B")
            Await VerifyItemIsAbsentAsync(text, "I")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546405")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546415")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546415")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546488")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546415")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530353")>
        Public Async Function TestNothingToImplement() As Task
            Dim text = <text>Interface I
    Sub Goo()
    Sub Bar()
End Interface

Class C
    Implements I

    Sub f1() Implements I.Goo
    End Sub

    Sub f2() Implements I.Bar
    End Sub

    Sub f3() Implements $$

End Class</text>.Value
            Await VerifyItemExistsAsync(text, "I")
            Await VerifyItemExistsAsync(text, "Global")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546431")>
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546431")>
        Public Async Function TestNextToImplicitLineContinuation2() As Task
            Dim text = <text>Public Interface I2
    Function Goo() As Boolean
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

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546406")>
        Public Async Function TestDisplayTypeArguments() As Task
            Dim text = <text>Imports System
Class A
    Implements IEquatable(Of Integer)
    Public Function Equals(other As Integer) As Boolean Implements $$
End Class


</text>.Value
            Await VerifyItemExistsAsync(text, "IEquatable(Of Integer)")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546406")>
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

            Await VerifyProviderCommitAsync(text, "IEquatable(Of Integer)", expected, "("c)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546802")>
        Public Async Function TestKeywordIdentifierShowUnescaped() As Task
            Dim text = <text>Interface [Interface]
    Sub Goo()
    Function Bar()
End Interface

Class C
    Implements [Interface]
    Public Sub test Implements $$
End Class</text>.Value

            Await VerifyItemExistsAsync(text, "Interface")
        End Function

        <Fact>
        Public Async Function TestKeywordIdentifierCommitEscaped() As Task
            Dim text = <text>Interface [Interface]
    Sub Goo()
    Function Bar()
End Interface

Class C
    Implements [Interface]
    Public Sub test Implements $$
End Class</text>.Value

            Dim expected = <text>Interface [Interface]
    Sub Goo()
    Function Bar()
End Interface

Class C
    Implements [Interface]
    Public Sub test Implements [Interface].
End Class</text>.Value

            Await VerifyProviderCommitAsync(text, "Interface", expected, "."c)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543812")>
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

        <Fact>
        Public Async Function TestInterfaceImplementsSub() As Task
            Dim test = <Text>
Interface IGoo
    Sub S1()
    Function F1() As Integer
    Property P1 As Integer
End Interface

Class C : Implements IGoo
    Sub S() Implements IGoo.$$
End Class
</Text>.Value

            Await VerifyItemExistsAsync(test, "S1")
            Await VerifyItemIsAbsentAsync(test, "F1")
            Await VerifyItemIsAbsentAsync(test, "P1")
        End Function

        <Fact>
        Public Async Function TestInterfaceImplementsFunction() As Task
            Dim test = <Text>
Interface IGoo
    Sub S1()
    Function F1() As Integer
    Property P1 As Integer
End Interface

Class C : Implements IGoo
    Function F() As Integer Implements IGoo.$$
End Class
</Text>.Value

            Await VerifyItemIsAbsentAsync(test, "S1")
            Await VerifyItemExistsAsync(test, "F1")
            Await VerifyItemIsAbsentAsync(test, "P1")
        End Function

        <Fact>
        Public Async Function TestInterfaceImplementsProperty() As Task
            Dim test = <Text>
Interface IGoo
    Sub S1()
    Function F1() As Integer
    Property P1 As Integer
End Interface

Class C : Implements IGoo
    Property P As Integer Implements IGoo.$$
End Class
</Text>.Value

            Await VerifyItemIsAbsentAsync(test, "S1")
            Await VerifyItemIsAbsentAsync(test, "F1")
            Await VerifyItemExistsAsync(test, "P1")
        End Function

        <Fact>
        Public Async Function TestVerifyDescription() As Task
            Dim test = <Text><![CDATA[
Interface IGoo
    ''' <summary>
    ''' Some Summary
    ''' </summary>
    Sub Bar()
End Interface

Class SomeClass
    Implements IGoo
    Public Sub something() Implements IGoo.$$
    End Sub
End Class
                       ]]></Text>

            Await VerifyItemExistsAsync(test.Value, "Bar", "Sub IGoo.Bar()" & vbCrLf & "Some Summary")
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530507")>
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

            Using workspace = EditorTestWorkspace.Create(element, composition:=GetComposition())
                Dim position = workspace.Documents.Single().CursorPosition.Value
                Dim document = workspace.CurrentSolution.GetDocument(workspace.Documents.Single().Id)
                Dim service = GetCompletionService(document.Project)
                Dim completionList = Await GetCompletionListAsync(service, document, position, CompletionTrigger.Invoke)
                AssertEx.Any(completionList.ItemsList, Function(c) c.DisplayText = "Workcover")
            End Using
        End Function

        <Fact>
        Public Async Function TestNotInTrivia() As Task
            Dim text = <text>Interface I
    Sub Goo()
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

        <Fact>
        Public Async Function TestReimplementInterfaceImplementedByBase() As Task
            Dim text = <text>Interface I
    Sub Goo()
End Interface

Class B
    Implements I

    Public Sub Goo() Implements I.Goo
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

        <Fact>
        Public Async Function TestReimplementInterfaceImplementedByBase2() As Task
            Dim text = <text>Interface I
    Sub Goo()
    Sub Quux()
End Interface

Class B
    Implements I

    Public Sub Goo() Implements I.Goo
        Throw New NotImplementedException()
    End Sub

    Public Sub Quux() Implements I.Quux
        Throw New NotImplementedException()
    End Sub
End Class

Class D
    Inherits B
    Implements I

    Sub Goo2() Implements I.Goo
    End Sub

    Sub Bar() Implements I.$$
End Class</text>.Value

            Await VerifyItemExistsAsync(text, "Quux")
        End Function

        <Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=402811")>
        Public Async Function DoNotCrashWithOnlyDotTyped() As Task
            Dim text = <text>Interface I
    Sub Goo()
    Sub Quux()
End Interface

Class B
    Implements I

    Public Sub Goo Implements .$$

   </text>.Value

            Await VerifyNoItemsExistAsync(text)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18006")>
        Public Async Function ShowGenericTypes() As Task
            Dim text = <text>Interface I(Of T)
    Sub Goo()
End Interface

Class B
    Implements I(Of Integer)

    Public Sub Goo() Implements $$

   </text>.Value

            Await VerifyItemExistsAsync(text, "I(Of Integer)")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18006")>
        Public Async Function ShowGenericTypes2() As Task
            Dim text = <text>Interface I(Of T)
    Sub Goo()
End Interface

Class B(Of T)
    Implements I(Of T)

    Public Sub Goo() Implements $$
    End Sub
End Class

   </text>.Value

            Await VerifyItemExistsAsync(text, "I(Of T)")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18006")>
        Public Async Function ShowGenericTypes3() As Task
            Dim text = <text>Interface I(Of T)
    Sub Goo()
End Interface

Class B(Of T)
    Implements I(Of Integer)

    Public Sub Goo() Implements $$
    End Sub
End Class

   </text>.Value

            Await VerifyItemExistsAsync(text, "I(Of Integer)")
        End Function
    End Class
End Namespace
