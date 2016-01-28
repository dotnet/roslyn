' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.GenerateVariable

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.GenerateVariable
    Public Class GenerateVariableTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return New Tuple(Of DiagnosticAnalyzer, CodeFixProvider)(Nothing, New GenerateVariableCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateSimpleProperty() As Threading.Tasks.Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Foo([|Bar|]) \n End Sub \n End Module"),
NewLines("Module Program \n Public Property Bar As Object \n Sub Main(args As String()) \n Foo(Bar) \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateSimpleField() As Threading.Tasks.Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Foo([|Bar|]) \n End Sub \n End Module"),
NewLines("Module Program \n Private Bar As Object \n Sub Main(args As String()) \n Foo(Bar) \n End Sub \n End Module"),
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateReadOnlyField() As Threading.Tasks.Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Foo([|Bar|]) \n End Sub \n End Module"),
NewLines("Module Program \n Private ReadOnly Bar As Object \n Sub Main(args As String()) \n Foo(Bar) \n End Sub \n End Module"),
index:=2)
        End Function

        <WorkItem(539692, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539692")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateFromAssignment() As Threading.Tasks.Task
            Await TestAsync(
NewLines("Class C \n Shared Sub M \n [|Foo|] = 3 \n End Sub \n End Class"),
NewLines("Class C \n Private Shared Foo As Integer \n Shared Sub M \n Foo = 3 \n End Sub \n End Class"),
index:=1)
        End Function

        <WorkItem(539694, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539694")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateReadOnlyProperty() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Dim i As IFoo \n Main(i.[|Blah|]) \n End Sub \n End Module \n Interface IFoo \n End Interface"),
NewLines("Module Program \n Sub Main(args As String()) \n Dim i As IFoo \n Main(i.Blah) \n End Sub \n End Module \n Interface IFoo \n ReadOnly Property Blah As String() \n End Interface"),
index:=1)
        End Function

        <WorkItem(539695, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539695")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateProtectedSharedFieldIntoBase() As Threading.Tasks.Task
            Await TestAsync(
NewLines("Class Base \n End Class \n Class Derived \n Inherits Base \n Shared Sub Main \n Dim a = Base.[|Foo|] \n End Sub \n End Class"),
NewLines("Class Base \n Protected Shared Foo As Object \n End Class \n Class Derived \n Inherits Base \n Shared Sub Main \n Dim a = Base.Foo \n End Sub \n End Class"),
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestNotOfferedForSharedAccessOffInterface() As Task
            Await TestMissingAsync(
NewLines("Interface IFoo \n End Interface \n Class Program \n Sub Main \n IFoo.[|Bar|] = 3 \n End Sub \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateFriendAccessibilityForField() As Threading.Tasks.Task
            Await TestAsync(
NewLines("Class A \n End Class \n Class B \n Sub Main \n Dim x = A.[|Foo|] \n End Sub \n End Class"),
NewLines("Class A \n Friend Shared Foo As Object \n End Class \n Class B \n Sub Main \n Dim x = A.Foo \n End Sub \n End Class"),
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGeneratePropertyOnInterface() As Threading.Tasks.Task
            Await TestAsync(
NewLines("Interface IFoo \n End Interface \n Class C \n Sub Main \n Dim foo As IFoo \n Dim b = foo.[|Bar|] \n End Sub \n End Class"),
NewLines("Interface IFoo \n Property Bar As Object \n End Interface \n Class C \n Sub Main \n Dim foo As IFoo \n Dim b = foo.Bar \n End Sub \n End Class"))
        End Function

        <WorkItem(539796, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539796")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGeneratePropertyIntoModule() As Threading.Tasks.Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n End Sub \n End Module \n Class C \n Sub M() \n Program.[|P|] = 10 \n End Sub \n End Class"),
NewLines("Module Program \n Public Property P As Integer \n Sub Main(args As String()) \n End Sub \n End Module \n Class C \n Sub M() \n Program.P = 10 \n End Sub \n End Class"))
        End Function

        <WorkItem(539796, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539796")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestFieldPropertyIntoModule() As Threading.Tasks.Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n End Sub \n End Module \n Class C \n Sub M() \n [|Program.P|] = 10 \n End Sub \n End Class"),
NewLines("Module Program \n Friend P As Integer \n Sub Main(args As String()) \n End Sub \n End Module \n Class C \n Sub M() \n Program.P = 10 \n End Sub \n End Class"),
index:=1)
        End Function

        <WorkItem(539848, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539848")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestOnLeftOfMemberAccess() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n [|HERE|].ToString() \n End Sub \n End Module"),
NewLines("Module Program \n Private HERE As Object \n Sub Main(args As String()) \n HERE.ToString() \n End Sub \n End Module"),
index:=1)
        End Function

        <WorkItem(539725, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539725")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestMissingWhenInterfacePropertyAlreadyExists() As Task
            Await TestMissingAsync(
NewLines("Interface IFoo \n Property Blah As String() \n End Interface \n Module Program \n Sub Main(args As String()) \n Dim foo As IFoo \n Main(foo.[|Blah|]) \n End Sub \n End Module"))
        End Function

        <WorkItem(540013, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540013")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestMissingInAddressOf() As Task
            Await TestMissingAsync(
NewLines("Delegate Sub D(x As Integer) \n Class C \n Public Sub Foo() \n Dim x As D = New D(AddressOf [|Method|]) \n End Sub \n End Class"))
        End Function

        <WorkItem(540578, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540578")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestInferProperReturnType() As Task
            Await TestAsync(
NewLines("Module Program \n Function Fun() As Integer \n Return [|P|] \n End Function \n End Module"),
NewLines("Module Program \n Public Property P As Integer \n Function Fun() As Integer \n Return P \n End Function \n End Module"))
        End Function

        <WorkItem(540576, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540576")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestAssignment() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As Integer \n x = [|P|] \n End Sub \n End Module"),
NewLines("Module Program \n Public Property P As Integer \n Sub Main(args As String()) \n Dim x As Integer \n x = P \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateFromSharedMethod() As Task
            Await TestAsync(
NewLines("Class GenPropTest \n Public Shared Sub Main() \n [|genStaticUnqualified|] = """" \n End Sub \n End Class"),
NewLines("Class GenPropTest \n Private Shared genStaticUnqualified As String \n Public Shared Sub Main() \n genStaticUnqualified = """" \n End Sub \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateSharedField() As Task
            Await TestAsync(
NewLines("Class GenPropTest \n Public Sub Main() \n GenPropTest.[|genStaticUnqualified|] = """" \n End Sub \n End Class"),
NewLines("Class GenPropTest \n Private Shared genStaticUnqualified As String \n Public Sub Main() \n GenPropTest.genStaticUnqualified = """" \n End Sub \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateInstanceFieldOffMe() As Task
            Await TestAsync(
NewLines("Class GenPropTest \n Public Sub Main() \n Me.[|field|] = """" \n End Sub \n End Class"),
NewLines("Class GenPropTest \n Private field As String \n Public Sub Main() \n Me.field = """" \n End Sub \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestSimpleInstanceField() As Task
            Await TestAsync(
NewLines("Class GenPropTest \n Public Sub Main() \n [|field|] = """" \n End Sub \n End Class"),
NewLines("Class GenPropTest \n Private field As String \n Public Sub Main() \n field = """" \n End Sub \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestFieldOnByRefParam() As Task
            Await TestAsync(
NewLines("Class A \n End Class \n Class B \n Public Sub Foo(ByRef d As Integer) \n End Sub \n Public Sub Bar() \n Dim s As New A() \n Foo(s.[|field|]) \n End Sub \n End Class"),
NewLines("Class A \n Friend field As Integer \n End Class \n Class B \n Public Sub Foo(ByRef d As Integer) \n End Sub \n Public Sub Bar() \n Dim s As New A() \n Foo(s.field) \n End Sub \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestOnlyGenerateFieldInByRefProperty() As Task
            Await TestExactActionSetOfferedAsync(
NewLines("Class A \n End Class \n Class B \n Public Sub Foo(ByRef d As Integer) \n End Sub \n Public Sub Bar() \n Dim s As New A() \n Foo(s.[|field|]) \n End Sub \n End Class"),
{String.Format(FeaturesResources.GenerateFieldIn, "field", "A")})
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateFieldIsFirstWithLowerCase() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n [|field|] = 5 \n End Sub \n End Module"),
NewLines("Module Program \n Private field As Integer \n Sub Main(args As String()) \n field = 5 \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGeneratePropertyIsFirstWithUpperCase() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n [|Field|] = 5 \n End Sub \n End Module"),
NewLines("Module Program \n Public Property Field As Integer \n Sub Main(args As String()) \n Field = 5 \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestNestedTypesAndInference() As Task
            Await TestAsync(
NewLines("Imports System.Collections.Generic \n Class A \n Sub Main() \n Dim field As List(Of C) = B.[|C|] \n End Sub \n End Class \n Class B \n End Class \n Class C \n End Class"),
NewLines("Imports System.Collections.Generic \n Class A \n Sub Main() \n Dim field As List(Of C) = B.C \n End Sub \n End Class \n Class B \n Public Shared Property C As List(Of C) \n End Class \n Class C \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestTypeInferenceWithGenerics1() As Task
            Await TestAsync(
NewLines("Class A \n Sub Main() \n [|field|] = New C(Of B) \n End Sub \n End Class \n Class B \n End Class \n Class C(Of T) \n End Class"),
NewLines("Class A \n Private field As C(Of B) \n Sub Main() \n field = New C(Of B) \n End Sub \n End Class \n Class B \n End Class \n Class C(Of T) \n End Class"))
        End Function

        <WorkItem(540693, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540693")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestErrorType() As Task
            Await TestAsync(
NewLines("Class A \n Sub Main() \n Dim field As List(Of C) = B.[|C|] \n End Sub \n End Class \n Class B \n End Class \n Class C(Of B) \n End Class"),
NewLines("Class A \n Sub Main() \n Dim field As List(Of C) = B.C \n End Sub \n End Class \n Class B \n Public Shared Property C As List \n End Class \n Class C(Of B) \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestTypeParameter() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class A \n Sub Foo(Of T) \n [|z|] = GetType(T) \n End Sub \n End Class"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class A \n Private z As Type \n Sub Foo(Of T) \n z = GetType(T) \n End Sub \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestInterfaceProperty() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class A \n Implements IFoo \n Public Property X As Integer Implements [|IFoo.X|] \n Sub Bar() \n End Sub \n End Class \n Interface IFoo \n End Interface"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class A \n Implements IFoo \n Public Property X As Integer Implements IFoo.X \n Sub Bar() \n End Sub \n End Class \n Interface IFoo \n Property X As Integer \n End Interface"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateEscapedKeywords() As Task
            Await TestAsync(
NewLines("Class [Class] \n Private Sub Method(i As Integer) \n [|[Enum]|] = 5 \n End Sub \n End Class"),
NewLines("Class [Class] \n Public Property [Enum] As Integer \n Private Sub Method(i As Integer) \n [Enum] = 5 \n End Sub \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateEscapedKeywords2() As Task
            Await TestAsync(
NewLines("Class [Class] \n Private Sub Method(i As Integer) \n [|[Enum]|] = 5 \n End Sub \n End Class"),
NewLines("Class [Class] \n Private [Enum] As Integer \n Private Sub Method(i As Integer) \n [Enum] = 5 \n End Sub \n End Class"),
index:=1)
        End Function

        <WorkItem(528229, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528229")>
        <WpfFact(Skip:="528229"), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestRefLambda() As Task
            Await TestAsync(
NewLines("Class [Class] \n Private Sub Method() \n [|test|] = Function(ByRef x As Integer) InlineAssignHelper(x, 10) \n End Sub \n Private Shared Function InlineAssignHelper(Of T)(ByRef target As T, value As T) As T \n target = value \n Return value \n End Function \n End Class"),
NewLines("Class [Class] \n Private test As Object \n Private Sub Method() \n test = Function(ByRef x As Integer) InlineAssignHelper(x, 10) \n End Sub \n Private Shared Function InlineAssignHelper(Of T)(ByRef target As T, value As T) As T \n target = value \n Return value \n End Function \n End Class"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestPropertyParameters1() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class A \n Implements IFoo \n Public Property Item1(i As Integer) As String Implements [|IFoo.Item1|] \n Get \n Throw New NotImplementedException() \n End Get \n Set(value As String) \n Throw New NotImplementedException() \n End Set \n End Property \n Sub Bar() \n End Sub \n End Class \n Interface IFoo \n ' Default Property Item(i As Integer) As String \n End Interface"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class A \n Implements IFoo \n Public Property Item1(i As Integer) As String Implements IFoo.Item1 \n Get \n Throw New NotImplementedException() \n End Get \n Set(value As String) \n Throw New NotImplementedException() \n End Set \n End Property \n Sub Bar() \n End Sub \n End Class \n Interface IFoo \n Property Item1(i As Integer) As String \n ' Default Property Item(i As Integer) As String \n End Interface"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestPropertyParameters2() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class A \n Implements IFoo \n Public Property Item1(i As Integer) As String Implements [|IFoo.Item1|] \n Get \n Throw New NotImplementedException() \n End Get \n Set(value As String) \n Throw New NotImplementedException() \n End Set \n End Property \n Sub Bar() \n End Sub \n End Class \n Interface IFoo \n ' Default Property Item(i As Integer) As String \n End Interface"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class A \n Implements IFoo \n Public Property Item1(i As Integer) As String Implements IFoo.Item1 \n Get \n Throw New NotImplementedException() \n End Get \n Set(value As String) \n Throw New NotImplementedException() \n End Set \n End Property \n Sub Bar() \n End Sub \n End Class \n Interface IFoo \n Property Item1(i As Integer) As String \n ' Default Property Item(i As Integer) As String \n End Interface"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestDefaultProperty1() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class A \n Implements IFoo \n Default Public Property Item(i As Integer) As String Implements [|IFoo.Item|] \n Get \n Throw New NotImplementedException() \n End Get \n Set(value As String) \n Throw New NotImplementedException() \n End Set \n End Property \n Sub Bar() \n End Sub \n End Class \n Interface IFoo \n ' Default Property Item(i As Integer) As String \n End Interface"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class A \n Implements IFoo \n Default Public Property Item(i As Integer) As String Implements IFoo.Item \n Get \n Throw New NotImplementedException() \n End Get \n Set(value As String) \n Throw New NotImplementedException() \n End Set \n End Property \n Sub Bar() \n End Sub \n End Class \n Interface IFoo \n Default Property Item(i As Integer) As String \n ' Default Property Item(i As Integer) As String \n End Interface"))
        End Function

        <WorkItem(540703, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540703")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestDefaultProperty2() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class A \n Implements IFoo \n Default Public Property Item(i As Integer) As String Implements [|IFoo.Item|] \n Get \n Throw New NotImplementedException() \n End Get \n Set(value As String) \n Throw New NotImplementedException() \n End Set \n End Property \n Sub Bar() \n End Sub \n End Class \n Interface IFoo \n ' Default Property Item(i As Integer) As String \n End Interface"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class A \n Implements IFoo \n Default Public Property Item(i As Integer) As String Implements IFoo.Item \n Get \n Throw New NotImplementedException() \n End Get \n Set(value As String) \n Throw New NotImplementedException() \n End Set \n End Property \n Sub Bar() \n End Sub \n End Class \n Interface IFoo \n Default Property Item(i As Integer) As String \n ' Default Property Item(i As Integer) As String \n End Interface"))
        End Function

        <WorkItem(540737, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540737")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestErrorInGenericType() As Task
            Await TestAsync(
NewLines("Imports System.Collections.Generic \n Class A \n Sub Main() \n Dim field As List(Of C) = B.[|C|] \n End Sub \n End Class \n Class B \n End Class \n Class C(Of T) \n End Class"),
NewLines("Imports System.Collections.Generic \n Class A \n Sub Main() \n Dim field As List(Of C) = B.C \n End Sub \n End Class \n Class B \n Public Shared Property C As List(Of C) \n End Class \n Class C(Of T) \n End Class"))
        End Function

        <WorkItem(542241, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542241")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestFieldWithAnonymousTypeType() As Threading.Tasks.Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub Main(args As String()) \n [|a|] = New With {.a =., .b = 1} \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Private a As Object \n Sub Main(args As String()) \n a = New With {.a =., .b = 1} \n End Sub \n End Module"))
        End Function

        <WorkItem(542395, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542395")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestUnqualifiedModuleMethod1() As Task
            Await TestAsync(
NewLines("Imports System.Runtime.CompilerServices \n Module StringExtensions \n Public Sub Print(ByVal aString As String) \n Console.WriteLine(aString) \n End Sub \n End Module \n Module M \n Sub Main() \n Print([|s|]) \n End Sub \n End Module"),
NewLines("Imports System.Runtime.CompilerServices \n Module StringExtensions \n Public Sub Print(ByVal aString As String) \n Console.WriteLine(aString) \n End Sub \n End Module \n Module M \n Private s As String \n Sub Main() \n Print(s) \n End Sub \n End Module"),
parseOptions:=Nothing) ' TODO (tomat): Modules nested in Script class not supported yet
        End Function

        <WorkItem(542395, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542395")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestUnqualifiedModuleMethod2() As Task
            Await TestAsync(
NewLines("Imports System.Runtime.CompilerServices \n Module StringExtensions \n <Extension()> \n Public Sub Print(ByVal aString As String) \n Console.WriteLine(aString) \n End Sub \n End Module \n Module M \n Sub Main() \n Print([|s|]) \n End Sub \n End Module"),
NewLines("Imports System.Runtime.CompilerServices \n Module StringExtensions \n <Extension()> \n Public Sub Print(ByVal aString As String) \n Console.WriteLine(aString) \n End Sub \n End Module \n Module M \n Private s As String \n Sub Main() \n Print(s) \n End Sub \n End Module"),
parseOptions:=Nothing) ' TODO (tomat): Modules nested in Script class not supported yet)
        End Function

        <WorkItem(542942, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542942")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestInsideLambda() As Task
            Await TestAsync(
NewLines("Module P \n Sub M() \n Dim t As System.Action = Sub() \n [|P.Foo|] = 5 \n End Sub \n End Sub \n End Module"),
NewLines("Module P \n Public Property Foo As Integer \n Sub M() \n Dim t As System.Action = Sub() \n P.Foo = 5 \n End Sub \n End Sub \n End Module"))
        End Function

        <WorkItem(544632, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544632")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestMissingOnForEachExpression() As Task
            Await TestMissingAsync(
<Text>
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Public Sub Linq103()
        Dim categories As String() = {"Beverages", "Condiments", "Vegetables", "Dairy Products", "Seafood"}

        Dim productList = GetProductList()

        Dim categorizedProducts = From cat In categories
                                  Group Join prod In productList On cat Equals prod.Category
                                  Into Products = Group
                                  Select Category = cat, Products

        For Each v In categorizedProducts
            Console.WriteLine(v.Category &amp; ":")
            For Each p In v.[|Products|]
                Console.WriteLine("   " &amp; p.ProductName)
            Next
        Next

    End Sub
End Module
</Text>.Value)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestLeftOfBinaryExpression() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Main([|a|] + b) \n End Sub \n End Module"),
NewLines("Module Program \n Private a As Integer \n Sub Main(args As String()) \n Main(a + b) \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestRightOfBinaryExpression() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Main(a + [|b|]) \n End Sub \n End Module"),
NewLines("Module Program \n Private b As Integer \n Sub Main(args As String()) \n Main(a + b) \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateLocal() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Foo([|bar|]) \n End Sub \n End Module"),
NewLines("Module Program \n Private bar As Object \n Sub Main(args As String()) \n Foo(bar) \n End Sub \n End Module"))
        End Function

        <WorkItem(809542, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/809542")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateLocalBeforeComment() As Task
            Await TestAsync(
"Imports System
Module Program
    Sub Main
#If True
        ' Banner Line 1
        ' Banner Line 2
        Integer.TryParse(""123"", [|local|])
#End If
    End Sub
End Module",
"Imports System
Module Program
    Sub Main
#If True
        Dim local As Integer = Nothing
        ' Banner Line 1
        ' Banner Line 2
        Integer.TryParse(""123"", local)
#End If
    End Sub
End Module", index:=1)
        End Function

        <WorkItem(809542, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/809542")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateLocalAfterComment() As Task
            Await TestAsync(
"Imports System
Module Program
    Sub Main
#If True
        ' Banner Line 1
        ' Banner Line 2

        Integer.TryParse(""123"", [|local|])
#End If
    End Sub
End Module",
"Imports System
Module Program
    Sub Main
#If True
        ' Banner Line 1
        ' Banner Line 2
        Dim local As Integer = Nothing
        Integer.TryParse(""123"", local)
#End If
    End Sub
End Module", index:=1)
        End Function

        <WorkItem(545218, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545218")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestTypeForLocal() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n foo([|xyz|]) \n End Sub \n Sub foo(x As Integer) \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main(args As String()) \n Dim xyz As Integer = Nothing \n foo(xyz) \n End Sub \n Sub foo(x As Integer) \n End Sub \n End Module"),
index:=3)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestInSelect() As Task
            Await TestAsync(
NewLines("Imports System.Linq \n Module Program \n Sub Main(args As String()) \n Dim q = From a In args \n Select [|v|] \n End Sub \n End Module"),
NewLines("Imports System.Linq \n Module Program \n Private v As Object \n Sub Main(args As String()) \n Dim q = From a In args \n Select v \n End Sub \n End Module"))
        End Function

        <WorkItem(545400, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545400")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateLocalInIfPart() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main() \n If ([|a|] Mod b <> 0) Then \n End If \n End Sub \n End Module"),
NewLines("Module Program \n Sub Main() \n Dim a As Object = Nothing \n If (a Mod b <> 0) Then \n End If \n End Sub \n End Module"),
index:=3)
        End Function

        <WorkItem(545672, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545672")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestCrashOnAggregateSelect() As Task
            Await TestMissingAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Dim q2 = From j In {1} Select j Aggregate i In {1} \n Select [|i|] Into Count(), Sum(i) Select Count, Sum, j \n End Sub \n End Module"))
        End Function

        <WorkItem(546753, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546753")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestAddressOf() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n [|d|] = AddressOf test \n End Sub \n Public Function test() As String \n Return ""hello"" \n End Function \n End Module"),
NewLines("Imports System \n Module Program \n Private d As Func(Of String) \n Sub Main(args As String()) \n d = AddressOf test \n End Sub \n Public Function test() As String \n Return ""hello"" \n End Function \n End Module"))
        End Function

        <WorkItem(530756, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530756")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestMissingOnDictionaryAccess1() As Task
            Await TestMissingAsync(
NewLines("Imports System.Collections \n  \n Module Program \n Sub Foo() \n Dim x = New Hashtable![|Foo|]!Bar \n End Sub \n End Module"))
        End Function

        <WorkItem(530756, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530756")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestMissingOnDictionaryAccess2() As Task
            Await TestMissingAsync(
NewLines("Imports System.Collections \n  \n Module Program \n Sub Foo() \n Dim x = New Hashtable!Foo![|Bar|] \n End Sub \n End Module"))
        End Function

        <WorkItem(530756, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530756")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestMissingOnDictionaryAccess3() As Task
            Await TestMissingAsync(
NewLines("Imports System.Collections \n  \n Module Program \n Sub Foo() \n Dim x = New Hashtable![|Foo!Bar|] \n End Sub \n End Module"))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestFormattingInGenerateVariable() As Task
            Await TestAsync(
<Text>Module Program
    Sub Main()
        If ([|a|] Mod b &lt;&gt; 0) Then
        End If
    End Sub
End Module</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Module Program
    Public Property a As Object

    Sub Main()
        If (a Mod b &lt;&gt; 0) Then
        End If
    End Sub
End Module</Text>.Value.Replace(vbLf, vbCrLf),
index:=2,
compareTokens:=False)
        End Function

        <WorkItem(666189, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/666189")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGeneratePropertyInScript() As Task
            Await TestAsync(
<Text>Dim x As Integer
x = [|Foo|]</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Dim x As Integer
Public Property Foo As Integer
x = Foo</Text>.Value.Replace(vbLf, vbCrLf),
parseOptions:=New VisualBasicParseOptions(kind:=SourceCodeKind.Script),
compareTokens:=False)
        End Function
        <WorkItem(666189, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/666189")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateFieldInScript() As Task
            Await TestAsync(
<Text>Dim x As Integer
x = [|Foo|]</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Dim x As Integer
Private Foo As Integer
x = Foo</Text>.Value.Replace(vbLf, vbCrLf),
parseOptions:=New VisualBasicParseOptions(kind:=SourceCodeKind.Script),
compareTokens:=False,
index:=1)
        End Function

        <WorkItem(977580, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/977580")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestWithThrow() As Task
            Await TestAsync(
NewLines("Imports System \n Public Class A \n Public Sub B() \n Throw [|MyExp|] \n End Sub \n End Class"),
NewLines("Imports System \n Public Class A \n Private MyExp As Exception \n Public Sub B() \n Throw MyExp \n End Sub \n End Class"), index:=1)
        End Function

        <WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestInsideNameOfProperty() As Task
            Await TestAsync(
NewLines("Imports System \n Class C \n Sub M() \n Dim x = NameOf ([|Z|]) \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Public Property Z As Object \n Sub M() \n Dim x = NameOf (Z) \n End Sub \n End Class"))
        End Function

        <WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestInsideNameOfField() As Task
            Await TestAsync(
NewLines("Imports System \n Class C \n Sub M() \n Dim x = NameOf ([|Z|]) \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Private Z As Object \n Sub M() \n Dim x = NameOf (Z) \n End Sub \n End Class"), index:=1)
        End Function

        <WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestInsideNameOfReadonlyField() As Task
            Await TestAsync(
NewLines("Imports System \n Class C \n Sub M() \n Dim x = NameOf ([|Z|]) \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Private ReadOnly Z As Object \n Sub M() \n Dim x = NameOf (Z) \n End Sub \n End Class"), index:=2)
        End Function

        <WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestInsideNameOfLocal() As Task
            Await TestAsync(
NewLines("Imports System \n Class C \n Sub M() \n Dim x = NameOf ([|Z|]) \n End Sub \n End Class"),
NewLines("Imports System \n Class C \n Sub M() \n Dim Z As Object = Nothing \n Dim x = NameOf (Z) \n End Sub \n End Class"), index:=3)
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessProperty() As Task
            Await TestAsync(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x As C = a?[|.B|] \n End Sub \n End Class"),
NewLines("Public Class C \n Public Property B As C \n Sub Main(a As C) \n Dim x As C = a?.B \n End Sub \n End Class"))
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessProperty2() As Task
            Await TestAsync(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x = a?[|.B|] \n End Sub \n End Class"),
NewLines("Public Class C \n Public Property B As Object \n Sub Main(a As C) \n Dim x = a?.B \n End Sub \n End Class"))
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessProperty3() As Task
            Await TestAsync(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x As Integer? = a?[|.B|] \n End Sub \n End Class"),
NewLines("Public Class C \n Public Property B As Integer \n Sub Main(a As C) \n Dim x As Integer? = a?.B \n End Sub \n End Class"))
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessProperty4() As Task
            Await TestAsync(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x As C? = a?[|.B|] \n End Sub \n End Class"),
NewLines("Public Class C \n Public Property B As C \n Sub Main(a As C) \n Dim x As C? = a?.B \n End Sub \n End Class"))
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessField() As Task
            Await TestAsync(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x As C = a?[|.B|] \n End Sub \n End Class"),
NewLines("Public Class C \n Private B As C \n Sub Main(a As C) \n Dim x As C = a?.B \n End Sub \n End Class"),
index:=1)
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessField2() As Task
            Await TestAsync(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x = a?[|.B|] \n End Sub \n End Class"),
NewLines("Public Class C \n Private B As Object \n Sub Main(a As C) \n Dim x = a?.B \n End Sub \n End Class"),
index:=1)
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessField3() As Task
            Await TestAsync(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x As Integer? = a?[|.B|] \n End Sub \n End Class"),
NewLines("Public Class C \n Private B As Integer \n Sub Main(a As C) \n Dim x As Integer? = a?.B \n End Sub \n End Class"),
index:=1)
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessField4() As Task
            Await TestAsync(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x As C? = a?[|.B|] \n End Sub \n End Class"),
NewLines("Public Class C \n Private B As C \n Sub Main(a As C) \n Dim x As C? = a?.B \n End Sub \n End Class"),
index:=1)
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessReadOnlyField() As Task
            Await TestAsync(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x As C = a?[|.B|] \n End Sub \n End Class"),
NewLines("Public Class C \n Private ReadOnly B As C \n Sub Main(a As C) \n Dim x As C = a?.B \n End Sub \n End Class"),
index:=2)
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessReadOnlyField2() As Task
            Await TestAsync(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x = a?[|.B|] \n End Sub \n End Class"),
NewLines("Public Class C \n Private ReadOnly B As Object \n Sub Main(a As C) \n Dim x = a?.B \n End Sub \n End Class"),
index:=2)
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessReadOnlyField3() As Task
            Await TestAsync(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x As Integer? = a?[|.B|] \n End Sub \n End Class"),
NewLines("Public Class C \n Private ReadOnly B As Integer \n Sub Main(a As C) \n Dim x As Integer? = a?.B \n End Sub \n End Class"),
index:=2)
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessReadOnlyField4() As Task
            Await TestAsync(
NewLines("Public Class C \n Sub Main(a As C) \n Dim x As C? = a?[|.B|] \n End Sub \n End Class"),
NewLines("Public Class C \n Private ReadOnly B As C \n Sub Main(a As C) \n Dim x As C? = a?.B \n End Sub \n End Class"),
index:=2)
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessPropertyInsideReferencedClass() As Task
            Await TestAsync(
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As C = a?[|.B.Z|] \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n End Class \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As C = a?.B.Z \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n Public Property Z As C \n End Class \n End Class"))
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessPropertyInsideReferencedClass2() As Task
            Await TestAsync(
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As Integer = a?[|.B.Z|] \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n End Class \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As Integer = a?.B.Z \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n Public Property Z As Integer \n End Class \n End Class"))
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessPropertyInsideReferencedClass3() As Task
            Await TestAsync(
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As Integer? = a?[|.B.Z|] \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n End Class \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As Integer? = a?.B.Z \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n Public Property Z As Integer \n End Class \n End Class"))
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessPropertyInsideReferencedClass4() As Task
            Await TestAsync(
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x = a?[|.B.Z|] \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n End Class \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x = a?.B.Z \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n Public Property Z As Object \n End Class \n End Class"))
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessFieldInsideReferencedClass() As Task
            Await TestAsync(
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As C = a?[|.B.Z|] \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n End Class \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As C = a?.B.Z \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n Friend Z As C \n End Class \n End Class"),
index:=1)
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessFieldInsideReferencedClass2() As Task
            Await TestAsync(
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As Integer = a?[|.B.Z|] \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n End Class \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As Integer = a?.B.Z \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n Friend Z As Integer \n End Class \n End Class"),
index:=1)
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessFieldInsideReferencedClass3() As Task
            Await TestAsync(
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As Integer? = a?[|.B.Z|] \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n End Class \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As Integer? = a?.B.Z \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n Friend Z As Integer \n End Class \n End Class"),
index:=1)
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessFieldInsideReferencedClass4() As Task
            Await TestAsync(
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x = a?[|.B.Z|] \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n End Class \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x = a?.B.Z \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n Friend Z As Object \n End Class \n End Class"),
index:=1)
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessReadOnlyFieldInsideReferencedClass() As Task
            Await TestAsync(
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As C = a?[|.B.Z|] \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n End Class \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As C = a?.B.Z \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n Friend ReadOnly Z As C \n End Class \n End Class"),
index:=2)
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessReadOnlyFieldInsideReferencedClass2() As Task
            Await TestAsync(
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As Integer = a?[|.B.Z|] \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n End Class \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As Integer = a?.B.Z \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n Friend ReadOnly Z As Integer \n End Class \n End Class"),
index:=2)
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessReadOnlyFieldInsideReferencedClass3() As Task
            Await TestAsync(
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As Integer? = a?[|.B.Z|] \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n End Class \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x As Integer? = a?.B.Z \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n Friend ReadOnly Z As Integer \n End Class \n End Class"),
index:=2)
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessReadOnlyFieldInsideReferencedClass4() As Task
            Await TestAsync(
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x = a?[|.B.Z|] \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n End Class \n End Class"),
NewLines("Imports System \n Public Class C \n Sub Main(a As C) \n Dim x = a?.B.Z \n End Sub \n Private Function B() As D \n Throw New NotImplementedException() \n End Function \n Private Class D \n Friend ReadOnly Z As Object \n End Class \n End Class"),
index:=2)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGeneratePropertyInPropertyInitializer() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Property a As Integer = [|y|] \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Private y As Integer \n Property a As Integer = y \n End Module"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateFieldInPropertyInitializer() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Property a As Integer = [|y|] \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Private ReadOnly y As Integer \n Property a As Integer = y \n End Module"),
index:=1)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateReadonlyFieldInPropertyInitializer() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Property a As Integer = [|y|] \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Property a As Integer = y \n Public Property y As Integer \n End Module"),
index:=2)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGeneratePropertyInObjectInitializer1() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As New Customer With {.[|Name|] = ""blah""} \n End Sub \n End Module \n Friend Class Customer \n End Class"),
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As New Customer With {.Name = ""blah""} \n End Sub \n End Module \n Friend Class Customer \n Public Property Name As String \n End Class"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGeneratePropertyInObjectInitializer2() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As New Customer With {.Name = ""blah"", .[|Age|] = blah} \n End Sub \n End Module \n Friend Class Customer \n Public Property Name As String \n End Class"),
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As New Customer With {.Name = ""blah"", .Age = blah} \n End Sub \n End Module \n Friend Class Customer \n Public Property Age As Object \n Public Property Name As String \n End Class"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGeneratePropertyInObjectInitializer3() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As New Customer With {.Name = [|name|]} \n End Sub \n End Module \n Friend Class Customer \n End Class"),
NewLines("Module Program \n Public Property name As Object \n Sub Main(args As String()) \n Dim x As New Customer With {.Name = name} \n End Sub \n End Module \n Friend Class Customer \n End Class"),
index:=2)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateFieldInObjectInitializer1() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As New Customer With {.[|Name|] = ""blah""} \n End Sub \n End Module \n Friend Class Customer \n End Class"),
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As New Customer With {.Name = ""blah""} \n End Sub \n End Module \n Friend Class Customer \n Friend Name As String \n End Class"),
index:=1)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateFieldInObjectInitializer2() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As New Customer With {.[|Name|] = name} \n End Sub \n End Module \n Friend Class Customer \n End Class"),
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As New Customer With {.Name = name} \n End Sub \n End Module \n Friend Class Customer \n Friend Name As Object \n End Class"),
index:=1)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateFieldInObjectInitializer3() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As New Customer With {.Name = [|name|]} \n End Sub \n End Module \n Friend Class Customer \n End Class"),
NewLines("Module Program \n Private name As Object \n Sub Main(args As String()) \n Dim x As New Customer With {.Name = name} \n End Sub \n End Module \n Friend Class Customer \n End Class"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestInvalidObjectInitializer() As Task
            Await TestMissingAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As New Customer With { [|Name|] = ""blkah""} \n End Sub \n End Module \n Friend Class Customer \n End Class"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestOnlyPropertyAndFieldOfferedForObjectInitializer() As Task
            Await TestActionCountAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As New Customer With {.[|Name|] = ""blah""} \n End Sub \n End Module \n Friend Class Customer \n End Class"),
2)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateLocalInObjectInitializerValue() As Task
            Await TestAsync(
NewLines("Module Program \n Sub Main(args As String()) \n Dim x As New Customer With {.Name = [|blah|]} \n End Sub \n End Module \n Friend Class Customer \n End Class"),
NewLines("Module Program \n Sub Main(args As String()) \n Dim blah As Object = Nothing \n Dim x As New Customer With {.Name = blah} \n End Sub \n End Module \n Friend Class Customer \n End Class"),
index:=3)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGeneratePropertyInTypeOf() As Task
            Await TestAsync(
NewLines("Module C \n Sub Test() \n If TypeOf [|B|] Is String Then \n End If \n End Sub \n End Module"),
NewLines("Module C \n Public Property B As String \n Sub Test() \n If TypeOf B Is String Then \n End If \n End Sub \n End Module"))
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateFieldInTypeOf() As Task
            Await TestAsync(
NewLines("Module C \n Sub Test() \n If TypeOf [|B|] Is String Then \n End If \n End Sub \n End Module"),
NewLines("Module C \n Private B As String \n Sub Test() \n If TypeOf B Is String Then \n End If \n End Sub \n End Module"),
index:=1)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateReadOnlyFieldInTypeOf() As Task
            Await TestAsync(
NewLines("Module C \n Sub Test() \n If TypeOf [|B|] Is String Then \n End If \n End Sub \n End Module"),
NewLines("Module C \n Private ReadOnly B As String \n Sub Test() \n If TypeOf B Is String Then \n End If \n End Sub \n End Module"),
index:=2)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateLocalInTypeOf() As Task
            Await TestAsync(
NewLines("Module C \n Sub Test() \n If TypeOf [|B|] Is String Then \n End If \n End Sub \n End Module"),
NewLines("Module C \n Sub Test() \n Dim B As String = Nothing \n If TypeOf B Is String Then \n End If \n End Sub \n End Module"),
index:=3)
        End Function

        <WorkItem(1130960, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1130960")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGeneratePropertyInTypeOfIsNot() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub M() \n If TypeOf [|Prop|] IsNot TypeOfIsNotDerived Then \n End If \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Public Property Prop As TypeOfIsNotDerived \n Sub M() \n If TypeOf Prop IsNot TypeOfIsNotDerived Then \n End If \n End Sub \n End Module"),
index:=0)
        End Function

        <WorkItem(1130960, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1130960")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateFieldInTypeOfIsNot() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub M() \n If TypeOf [|Prop|] IsNot TypeOfIsNotDerived Then \n End If \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Private Prop As TypeOfIsNotDerived \n Sub M() \n If TypeOf Prop IsNot TypeOfIsNotDerived Then \n End If \n End Sub \n End Module"),
index:=1)
        End Function

        <WorkItem(1130960, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1130960")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateReadOnlyFieldInTypeOfIsNot() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub M() \n If TypeOf [|Prop|] IsNot TypeOfIsNotDerived Then \n End If \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Private ReadOnly Prop As TypeOfIsNotDerived \n Sub M() \n If TypeOf Prop IsNot TypeOfIsNotDerived Then \n End If \n End Sub \n End Module"),
index:=2)
        End Function

        <WorkItem(1130960, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1130960")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateLocalInTypeOfIsNot() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub M() \n If TypeOf [|Prop|] IsNot TypeOfIsNotDerived Then \n End If \n End Sub \n End Module"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Module Program \n Sub M() \n Dim Prop As TypeOfIsNotDerived = Nothing \n If TypeOf Prop IsNot TypeOfIsNotDerived Then \n End If \n End Sub \n End Module"),
index:=3)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateVariableFromLambda() As Task
            Await TestAsync(
NewLines("Class [Class] \n Private Sub Method(i As Integer) \n [|foo|] = Function() \n Return 2 \n End Function \n End Sub \n End Class"),
NewLines("Imports System\n\nClass [Class]\n    Private foo As Func(Of Integer)\n\n    Private Sub Method(i As Integer)\n        foo = Function()\n                  Return 2\n              End Function\n    End Sub\nEnd Class"),
index:=0)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateVariableFromLambda2() As Task
            Await TestAsync(
NewLines("Class [Class] \n Private Sub Method(i As Integer) \n [|foo|] = Function() \n Return 2 \n End Function \n End Sub \n End Class"),
NewLines("Imports System\n\nClass [Class]\n    Public Property foo As Func(Of Integer)\n\n    Private Sub Method(i As Integer)\n        foo = Function()\n                  Return 2\n              End Function\n    End Sub\nEnd Class"),
index:=1)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateVariableFromLambda3() As Task
            Await TestAsync(
NewLines("Class [Class] \n Private Sub Method(i As Integer) \n [|foo|] = Function() \n Return 2 \n End Function \n End Sub \n End Class"),
NewLines("Class [Class] \n Private Sub Method(i As Integer)\n        Dim foo As System.Func(Of Integer)\n        foo = Function() \n Return 2 \n End Function \n End Sub \n End Class"),
index:=2)
        End Function
    End Class
End Namespace
