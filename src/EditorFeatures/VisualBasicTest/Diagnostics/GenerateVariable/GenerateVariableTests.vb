﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.GenerateVariable

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.GenerateVariable
    Public Class GenerateVariableTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (Nothing, New VisualBasicGenerateVariableCodeFixProvider())
        End Function

        Protected Overrides Function MassageActions(actions As ImmutableArray(Of CodeAction)) As ImmutableArray(Of CodeAction)
            Return FlattenActions(actions)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateSimpleProperty() As Threading.Tasks.Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Goo([|Bar|])
    End Sub
End Module",
"Module Program
    Public Property Bar As Object

    Sub Main(args As String())
        Goo(Bar)
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateSimpleField() As Threading.Tasks.Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Goo([|Bar|])
    End Sub
End Module",
"Module Program
    Private Bar As Object

    Sub Main(args As String())
        Goo(Bar)
    End Sub
End Module",
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateReadOnlyField() As Threading.Tasks.Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Goo([|Bar|])
    End Sub
End Module",
"Module Program
    Private ReadOnly Bar As Object

    Sub Main(args As String())
        Goo(Bar)
    End Sub
End Module",
index:=2)
        End Function

        <WorkItem(539692, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539692")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateFromAssignment() As Threading.Tasks.Task
            Await TestInRegularAndScriptAsync(
"Class C
    Shared Sub M
        [|Goo|] = 3
    End Sub
End Class",
"Class C
    Private Shared Goo As Integer

    Shared Sub M
        Goo = 3
    End Sub
End Class",
index:=1)
        End Function

        <WorkItem(539694, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539694")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateReadOnlyProperty() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Dim i As IGoo
        Main(i.[|Blah|])
    End Sub
End Module
Interface IGoo
End Interface",
"Module Program
    Sub Main(args As String())
        Dim i As IGoo
        Main(i.Blah)
    End Sub
End Module
Interface IGoo
    ReadOnly Property Blah As String()
End Interface")
        End Function

        <WorkItem(539694, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539694")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateReadWriteProperty() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Dim i As IGoo
        Main(i.[|Blah|])
    End Sub
End Module
Interface IGoo
End Interface",
"Module Program
    Sub Main(args As String())
        Dim i As IGoo
        Main(i.Blah)
    End Sub
End Module
Interface IGoo
    Property Blah As String()
End Interface",
index:=1)
        End Function

        <WorkItem(539695, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539695")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateProtectedSharedFieldIntoBase() As Threading.Tasks.Task
            Await TestInRegularAndScriptAsync(
"Class Base
End Class
Class Derived
    Inherits Base
    Shared Sub Main
        Dim a = Base.[|Goo|]
    End Sub
End Class",
"Class Base
    Protected Shared Goo As Object
End Class
Class Derived
    Inherits Base
    Shared Sub Main
        Dim a = Base.Goo
    End Sub
End Class",
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestNotOfferedForSharedAccessOffInterface() As Task
            Await TestMissingInRegularAndScriptAsync(
"Interface IGoo
End Interface
Class Program
    Sub Main
        IGoo.[|Bar|] = 3
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateFriendAccessibilityForField() As Threading.Tasks.Task
            Await TestInRegularAndScriptAsync(
"Class A
End Class
Class B
    Sub Main
        Dim x = A.[|Goo|]
    End Sub
End Class",
"Class A
    Friend Shared Goo As Object
End Class
Class B
    Sub Main
        Dim x = A.Goo
    End Sub
End Class",
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGeneratePropertyOnInterface1() As Threading.Tasks.Task
            Await TestInRegularAndScriptAsync(
"Interface IGoo
End Interface
Class C
    Sub Main
        Dim goo As IGoo
        Dim b = goo.[|Bar|]
    End Sub
End Class",
"Interface IGoo
    ReadOnly Property Bar As Object
End Interface
Class C
    Sub Main
        Dim goo As IGoo
        Dim b = goo.Bar
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGeneratePropertyOnInterface2() As Threading.Tasks.Task
            Await TestInRegularAndScriptAsync(
"Interface IGoo
End Interface
Class C
    Sub Main
        Dim goo As IGoo
        Dim b = goo.[|Bar|]
    End Sub
End Class",
"Interface IGoo
    Property Bar As Object
End Interface
Class C
    Sub Main
        Dim goo As IGoo
        Dim b = goo.Bar
    End Sub
End Class", index:=1)
        End Function

        <WorkItem(539796, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539796")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGeneratePropertyIntoModule() As Threading.Tasks.Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
    End Sub
End Module
Class C
    Sub M()
        Program.[|P|] = 10
    End Sub
End Class",
"Module Program
    Public Property P As Integer

    Sub Main(args As String())
    End Sub
End Module
Class C
    Sub M()
        Program.P = 10
    End Sub
End Class")
        End Function

        <WorkItem(539796, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539796")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestFieldPropertyIntoModule() As Threading.Tasks.Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
    End Sub
End Module
Class C
    Sub M()
        [|Program.P|] = 10
    End Sub
End Class",
"Module Program
    Friend P As Integer

    Sub Main(args As String())
    End Sub
End Module
Class C
    Sub M()
        Program.P = 10
    End Sub
End Class",
index:=1)
        End Function

        <WorkItem(539848, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539848")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestOnLeftOfMemberAccess() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        [|HERE|].ToString()
    End Sub
End Module",
"Module Program
    Private HERE As Object

    Sub Main(args As String())
        HERE.ToString()
    End Sub
End Module",
index:=1)
        End Function

        <WorkItem(539725, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539725")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestMissingWhenInterfacePropertyAlreadyExists() As Task
            Await TestMissingInRegularAndScriptAsync(
"Interface IGoo
    Property Blah As String()
End Interface
Module Program
    Sub Main(args As String())
        Dim goo As IGoo
        Main(goo.[|Blah|])
    End Sub
End Module")
        End Function

        <WorkItem(540013, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540013")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestMissingInAddressOf() As Task
            Await TestMissingInRegularAndScriptAsync(
"Delegate Sub D(x As Integer)
Class C
    Public Sub Goo()
        Dim x As D = New D(AddressOf [|Method|])
    End Sub
End Class")
        End Function

        <WorkItem(540578, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540578")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestInferProperReturnType() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Function Fun() As Integer
        Return [|P|]
    End Function
End Module",
"Module Program
    Public Property P As Integer

    Function Fun() As Integer
        Return P
    End Function
End Module")
        End Function

        <WorkItem(540576, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540576")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestAssignment() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Dim x As Integer
        x = [|P|]
    End Sub
End Module",
"Module Program
    Public Property P As Integer

    Sub Main(args As String())
        Dim x As Integer
        x = P
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateFromSharedMethod() As Task
            Await TestInRegularAndScriptAsync(
"Class GenPropTest
    Public Shared Sub Main()
        [|genStaticUnqualified|] = """"
    End Sub
End Class",
"Class GenPropTest
    Private Shared genStaticUnqualified As String

    Public Shared Sub Main()
        genStaticUnqualified = """"
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateSharedField() As Task
            Await TestInRegularAndScriptAsync(
"Class GenPropTest
    Public Sub Main()
        GenPropTest.[|genStaticUnqualified|] = """"
    End Sub
End Class",
"Class GenPropTest
    Private Shared genStaticUnqualified As String

    Public Sub Main()
        GenPropTest.genStaticUnqualified = """"
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateInstanceFieldOffMe() As Task
            Await TestInRegularAndScriptAsync(
"Class GenPropTest
    Public Sub Main()
        Me.[|field|] = """"
    End Sub
End Class",
"Class GenPropTest
    Private field As String

    Public Sub Main()
        Me.field = """"
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestSimpleInstanceField() As Task
            Await TestInRegularAndScriptAsync(
"Class GenPropTest
    Public Sub Main()
        [|field|] = """"
    End Sub
End Class",
"Class GenPropTest
    Private field As String

    Public Sub Main()
        field = """"
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestFieldOnByRefParam() As Task
            Await TestInRegularAndScriptAsync(
"Class A
End Class
Class B
    Public Sub Goo(ByRef d As Integer)
    End Sub
    Public Sub Bar()
        Dim s As New A()
        Goo(s.[|field|])
    End Sub
End Class",
"Class A
    Friend field As Integer
End Class
Class B
    Public Sub Goo(ByRef d As Integer)
    End Sub
    Public Sub Bar()
        Dim s As New A()
        Goo(s.field)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateFieldInByRefProperty() As Task
            Await TestInRegularAndScriptAsync(
"Class A
End Class
Class B
    Public Sub Goo(ByRef d As Integer)
    End Sub
    Public Sub Bar()
        Dim s As New A()
        Goo(s.[|field|])
    End Sub
End Class",
"Class A
    Friend field As Integer
End Class
Class B
    Public Sub Goo(ByRef d As Integer)
    End Sub
    Public Sub Bar()
        Dim s As New A()
        Goo(s.field)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGeneratePropertyInByRefProperty() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class A
End Class
Class B
    Public Sub Goo(ByRef d As Integer)
    End Sub
    Public Sub Bar()
        Dim s As New A()
        Goo(s.[|field|])
    End Sub
End Class",
"
Imports System

Class A
    Public ReadOnly Property field As Integer
        Get
            Throw New NotImplementedException()
        End Get
    End Property
End Class
Class B
    Public Sub Goo(ByRef d As Integer)
    End Sub
    Public Sub Bar()
        Dim s As New A()
        Goo(s.field)
    End Sub
End Class", index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateFieldIsFirstWithLowerCase() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        [|field|] = 5
    End Sub
End Module",
"Module Program
    Private field As Integer

    Sub Main(args As String())
        field = 5
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGeneratePropertyIsFirstWithUpperCase() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        [|Field|] = 5
    End Sub
End Module",
"Module Program
    Public Property Field As Integer

    Sub Main(args As String())
        Field = 5
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestNestedTypesAndInference() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.Collections.Generic
Class A
    Sub Main()
        Dim field As List(Of C) = B.[|C|]
    End Sub
End Class
Class B
End Class
Class C
End Class",
"Imports System.Collections.Generic
Class A
    Sub Main()
        Dim field As List(Of C) = B.C
    End Sub
End Class
Class B
    Public Shared Property C As List(Of C)
End Class
Class C
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestTypeInferenceWithGenerics1() As Task
            Await TestInRegularAndScriptAsync(
"Class A
    Sub Main()
        [|field|] = New C(Of B)
    End Sub
End Class
Class B
End Class
Class C(Of T)
End Class",
"Class A
    Private field As C(Of B)

    Sub Main()
        field = New C(Of B)
    End Sub
End Class
Class B
End Class
Class C(Of T)
End Class")
        End Function

        <WorkItem(540693, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540693")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestErrorType() As Task
            Await TestInRegularAndScriptAsync(
"Class A
    Sub Main()
        Dim field As List(Of C) = B.[|C|]
    End Sub
End Class
Class B
End Class
Class C(Of B)
End Class",
"Class A
    Sub Main()
        Dim field As List(Of C) = B.C
    End Sub
End Class
Class B
    Public Shared Property C As List
End Class
Class C(Of B)
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestTypeParameter() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class A
    Sub Goo(Of T)
        [|z|] = GetType(T)
    End Sub
End Class",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class A
    Private z As Type

    Sub Goo(Of T)
        z = GetType(T)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestInterfaceProperty() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class A
    Implements IGoo
    Public Property X As Integer Implements [|IGoo.X|]
    Sub Bar()
    End Sub
End Class
Interface IGoo
End Interface",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class A
    Implements IGoo
    Public Property X As Integer Implements IGoo.X
    Sub Bar()
    End Sub
End Class
Interface IGoo
    Property X As Integer
End Interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateEscapedKeywords() As Task
            Await TestInRegularAndScriptAsync(
"Class [Class]
    Private Sub Method(i As Integer)
        [|[Enum]|] = 5
    End Sub
End Class",
"Class [Class]
    Public Property [Enum] As Integer

    Private Sub Method(i As Integer)
        [Enum] = 5
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateEscapedKeywords2() As Task
            Await TestInRegularAndScriptAsync(
"Class [Class]
    Private Sub Method(i As Integer)
        [|[Enum]|] = 5
    End Sub
End Class",
"Class [Class]
    Private [Enum] As Integer

    Private Sub Method(i As Integer)
        [Enum] = 5
    End Sub
End Class",
index:=1)
        End Function

        <WorkItem(528229, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528229")>
        <WpfFact(Skip:="528229"), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestRefLambda() As Task
            Await TestInRegularAndScriptAsync(
"Class [Class]
    Private Sub Method()
        [|test|] = Function(ByRef x As Integer) InlineAssignHelper(x, 10)
    End Sub
    Private Shared Function InlineAssignHelper(Of T)(ByRef target As T, value As T) As T
        target = value
        Return value
    End Function
End Class",
"Class [Class]
    Private test As Object

    Private Sub Method()
        test = Function(ByRef x As Integer) InlineAssignHelper(x, 10)
    End Sub
    Private Shared Function InlineAssignHelper(Of T)(ByRef target As T, value As T) As T
        target = value
        Return value
    End Function
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestPropertyParameters1() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class A
    Implements IGoo
    Public Property Item1(i As Integer) As String Implements [|IGoo.Item1|]
        Get
            Throw New NotImplementedException()
        End Get
        Set(value As String)
            Throw New NotImplementedException()
        End Set
    End Property
    Sub Bar()
    End Sub
End Class
Interface IGoo
    ' Default Property Item(i As Integer) As String 
End Interface",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class A
    Implements IGoo
    Public Property Item1(i As Integer) As String Implements IGoo.Item1
        Get
            Throw New NotImplementedException()
        End Get
        Set(value As String)
            Throw New NotImplementedException()
        End Set
    End Property
    Sub Bar()
    End Sub
End Class
Interface IGoo
    Property Item1(i As Integer) As String
    ' Default Property Item(i As Integer) As String 
End Interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestPropertyParameters2() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class A
    Implements IGoo
    Public Property Item1(i As Integer) As String Implements [|IGoo.Item1|]
        Get
            Throw New NotImplementedException()
        End Get
        Set(value As String)
            Throw New NotImplementedException()
        End Set
    End Property
    Sub Bar()
    End Sub
End Class
Interface IGoo
    ' Default Property Item(i As Integer) As String 
End Interface",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class A
    Implements IGoo
    Public Property Item1(i As Integer) As String Implements IGoo.Item1
        Get
            Throw New NotImplementedException()
        End Get
        Set(value As String)
            Throw New NotImplementedException()
        End Set
    End Property
    Sub Bar()
    End Sub
End Class
Interface IGoo
    Property Item1(i As Integer) As String
    ' Default Property Item(i As Integer) As String 
End Interface")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestDefaultProperty1() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class A
    Implements IGoo
    Default Public Property Item(i As Integer) As String Implements [|IGoo.Item|]
        Get
            Throw New NotImplementedException()
        End Get
        Set(value As String)
            Throw New NotImplementedException()
        End Set
    End Property
    Sub Bar()
    End Sub
End Class
Interface IGoo
    ' Default Property Item(i As Integer) As String 
End Interface",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class A
    Implements IGoo
    Default Public Property Item(i As Integer) As String Implements IGoo.Item
        Get
            Throw New NotImplementedException()
        End Get
        Set(value As String)
            Throw New NotImplementedException()
        End Set
    End Property
    Sub Bar()
    End Sub
End Class
Interface IGoo
    Default Property Item(i As Integer) As String
    ' Default Property Item(i As Integer) As String 
End Interface")
        End Function

        <WorkItem(540703, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540703")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestDefaultProperty2() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class A
    Implements IGoo
    Default Public Property Item(i As Integer) As String Implements [|IGoo.Item|]
        Get
            Throw New NotImplementedException()
        End Get
        Set(value As String)
            Throw New NotImplementedException()
        End Set
    End Property
    Sub Bar()
    End Sub
End Class
Interface IGoo
    ' Default Property Item(i As Integer) As String 
End Interface",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class A
    Implements IGoo
    Default Public Property Item(i As Integer) As String Implements IGoo.Item
        Get
            Throw New NotImplementedException()
        End Get
        Set(value As String)
            Throw New NotImplementedException()
        End Set
    End Property
    Sub Bar()
    End Sub
End Class
Interface IGoo
    Default Property Item(i As Integer) As String
    ' Default Property Item(i As Integer) As String 
End Interface")
        End Function

        <WorkItem(540737, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540737")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestErrorInGenericType() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.Collections.Generic
Class A
    Sub Main()
        Dim field As List(Of C) = B.[|C|]
    End Sub
End Class
Class B
End Class
Class C(Of T)
End Class",
"Imports System.Collections.Generic
Class A
    Sub Main()
        Dim field As List(Of C) = B.C
    End Sub
End Class
Class B
    Public Shared Property C As List(Of C)
End Class
Class C(Of T)
End Class")
        End Function

        <WorkItem(542241, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542241")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestFieldWithAnonymousTypeType() As Threading.Tasks.Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main(args As String())
        [|a|] = New With {.a = ., .b = 1}
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Private a As Object

    Sub Main(args As String())
        a = New With {.a = ., .b = 1}
    End Sub
End Module")
        End Function

        <WorkItem(542395, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542395")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestUnqualifiedModuleMethod1() As Task
            Await TestAsync(
"Imports System.Runtime.CompilerServices
Module StringExtensions
    Public Sub Print(ByVal aString As String)
        Console.WriteLine(aString)
    End Sub
End Module
Module M
    Sub Main()
        Print([|s|])
    End Sub
End Module",
"Imports System.Runtime.CompilerServices
Module StringExtensions
    Public Sub Print(ByVal aString As String)
        Console.WriteLine(aString)
    End Sub
End Module
Module M
    Private s As String

    Sub Main()
        Print(s)
    End Sub
End Module",
parseOptions:=Nothing) ' TODO (tomat): Modules nested in Script class not supported yet
        End Function

        <WorkItem(542395, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542395")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestUnqualifiedModuleMethod2() As Task
            Await TestAsync(
"Imports System.Runtime.CompilerServices
Module StringExtensions
    <Extension()>
    Public Sub Print(ByVal aString As String)
        Console.WriteLine(aString)
    End Sub
End Module
Module M
    Sub Main()
        Print([|s|])
    End Sub
End Module",
"Imports System.Runtime.CompilerServices
Module StringExtensions
    <Extension()>
    Public Sub Print(ByVal aString As String)
        Console.WriteLine(aString)
    End Sub
End Module
Module M
    Private s As String

    Sub Main()
        Print(s)
    End Sub
End Module",
parseOptions:=Nothing) ' TODO (tomat): Modules nested in Script class not supported yet)
        End Function

        <WorkItem(542942, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542942")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestInsideLambda() As Task
            Await TestInRegularAndScriptAsync(
"Module P
    Sub M()
        Dim t As System.Action = Sub()
                                     [|P.Goo|] = 5
                                 End Sub
    End Sub
End Module",
"Module P
    Public Property Goo As Integer

    Sub M()
        Dim t As System.Action = Sub()
                                     P.Goo = 5
                                 End Sub
    End Sub
End Module")
        End Function

        <WorkItem(544632, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544632")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestMissingOnForEachExpression() As Task
            Await TestMissingInRegularAndScriptAsync(
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
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Main([|a|] + b)
    End Sub
End Module",
"Module Program
    Private a As Integer

    Sub Main(args As String())
        Main(a + b)
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestRightOfBinaryExpression() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Main(a + [|b|])
    End Sub
End Module",
"Module Program
    Private b As Integer

    Sub Main(args As String())
        Main(a + b)
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateLocal() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Goo([|bar|])
    End Sub
End Module",
"Module Program
    Private bar As Object

    Sub Main(args As String())
        Goo(bar)
    End Sub
End Module")
        End Function

        <WorkItem(809542, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/809542")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateLocalBeforeComment() As Task
            Await TestInRegularAndScriptAsync(
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
End Module", index:=2)
        End Function

        <WorkItem(809542, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/809542")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateLocalAfterComment() As Task
            Await TestInRegularAndScriptAsync(
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
End Module", index:=2)
        End Function

        <WorkItem(545218, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545218")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestTypeForLocal() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        goo([|xyz|])
    End Sub
    Sub goo(x As Integer)
    End Sub
End Module",
"Module Program
    Sub Main(args As String())
        Dim xyz As Integer = Nothing
        goo(xyz)
    End Sub
    Sub goo(x As Integer)
    End Sub
End Module",
index:=3)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestInSelect() As Task
            Await TestInRegularAndScriptAsync(
"Imports System.Linq
Module Program
    Sub Main(args As String())
        Dim q = From a In args
                Select [|v|]
    End Sub
End Module",
"Imports System.Linq
Module Program
    Private v As Object

    Sub Main(args As String())
        Dim q = From a In args
                Select v
    End Sub
End Module")
        End Function

        <WorkItem(545400, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545400")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateLocalInIfPart() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main()
        If ([|a|] Mod b <> 0) Then
        End If
    End Sub
End Module",
"Module Program
    Sub Main()
        Dim a As Object = Nothing

        If (a Mod b <> 0) Then
        End If
    End Sub
End Module",
index:=3)
        End Function

        <WorkItem(545672, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545672")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestCrashOnAggregateSelect() As Task
            Await TestMissingInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Dim q2 = From j In {1} Select j Aggregate i In {1}
        Select [|i|] Into Count(), Sum(i) Select Count, Sum, j
    End Sub
End Module")
        End Function

        <WorkItem(546753, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546753")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestAddressOf() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        [|d|] = AddressOf test
    End Sub
    Public Function test() As String
        Return ""hello"" 
 End Function
End Module",
"Imports System

Module Program
    Private d As Func(Of String)

    Sub Main(args As String())
        d = AddressOf test
    End Sub
    Public Function test() As String
        Return ""hello"" 
 End Function
End Module")
        End Function

        <WorkItem(530756, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530756")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestMissingOnDictionaryAccess1() As Task
            Await TestMissingInRegularAndScriptAsync(
"Imports System.Collections

Module Program
    Sub Goo()
        Dim x = New Hashtable![|Goo|]!Bar
    End Sub
End Module")
        End Function

        <WorkItem(530756, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530756")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestMissingOnDictionaryAccess2() As Task
            Await TestMissingInRegularAndScriptAsync(
"Imports System.Collections

Module Program
    Sub Goo()
        Dim x = New Hashtable!Goo![|Bar|]
    End Sub
End Module")
        End Function

        <WorkItem(530756, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530756")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestMissingOnDictionaryAccess3() As Task
            Await TestMissingInRegularAndScriptAsync(
"Imports System.Collections

Module Program
    Sub Goo()
        Dim x = New Hashtable![|Goo!Bar|]
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestFormattingInGenerateVariable() As Task
            Await TestInRegularAndScriptAsync(
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
index:=2)
        End Function

        <WorkItem(666189, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/666189")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGeneratePropertyInScript() As Task
            Await TestAsync(
<Text>Dim x As Integer
x = [|Goo|]</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Dim x As Integer
Public Property Goo As Integer
x = Goo</Text>.Value.Replace(vbLf, vbCrLf),
parseOptions:=New VisualBasicParseOptions(kind:=SourceCodeKind.Script))
        End Function
        <WorkItem(666189, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/666189")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateFieldInScript() As Task
            Await TestAsync(
<Text>Dim x As Integer
x = [|Goo|]</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Dim x As Integer
Private Goo As Integer
x = Goo</Text>.Value.Replace(vbLf, vbCrLf),
parseOptions:=New VisualBasicParseOptions(kind:=SourceCodeKind.Script),
index:=1)
        End Function

        <WorkItem(977580, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/977580")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestWithThrow() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Public Class A
    Public Sub B()
        Throw [|MyExp|]
    End Sub
End Class",
"Imports System
Public Class A
    Private MyExp As Exception

    Public Sub B()
        Throw MyExp
    End Sub
End Class", index:=1)
        End Function

        <WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestInsideNameOfProperty() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Class C
    Sub M()
        Dim x = NameOf([|Z|])
    End Sub
End Class",
"Imports System
Class C
    Public Property Z As Object

    Sub M()
        Dim x = NameOf(Z)
    End Sub
End Class")
        End Function

        <WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestInsideNameOfField() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Class C
    Sub M()
        Dim x = NameOf([|Z|])
    End Sub
End Class",
"Imports System
Class C
    Private Z As Object

    Sub M()
        Dim x = NameOf(Z)
    End Sub
End Class", index:=1)
        End Function

        <WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestInsideNameOfReadonlyField() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Class C
    Sub M()
        Dim x = NameOf([|Z|])
    End Sub
End Class",
"Imports System
Class C
    Private ReadOnly Z As Object

    Sub M()
        Dim x = NameOf(Z)
    End Sub
End Class", index:=2)
        End Function

        <WorkItem(1032176, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1032176")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestInsideNameOfLocal() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Class C
    Sub M()
        Dim x = NameOf([|Z|])
    End Sub
End Class",
"Imports System
Class C
    Sub M()
        Dim Z As Object = Nothing
        Dim x = NameOf(Z)
    End Sub
End Class", index:=3)
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessProperty() As Task
            Await TestInRegularAndScriptAsync(
"Public Class C
    Sub Main(a As C)
        Dim x As C = a?[|.B|]
    End Sub
End Class",
"Public Class C
    Public Property B As C

    Sub Main(a As C)
        Dim x As C = a?.B
    End Sub
End Class")
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessProperty2() As Task
            Await TestInRegularAndScriptAsync(
"Public Class C
    Sub Main(a As C)
        Dim x = a?[|.B|]
    End Sub
End Class",
"Public Class C
    Public Property B As Object

    Sub Main(a As C)
        Dim x = a?.B
    End Sub
End Class")
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessProperty3() As Task
            Await TestInRegularAndScriptAsync(
"Public Class C
    Sub Main(a As C)
        Dim x As Integer? = a?[|.B|]
    End Sub
End Class",
"Public Class C
    Public Property B As Integer

    Sub Main(a As C)
        Dim x As Integer? = a?.B
    End Sub
End Class")
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessProperty4() As Task
            Await TestInRegularAndScriptAsync(
"Public Class C
    Sub Main(a As C)
        Dim x As C? = a?[|.B|]
    End Sub
End Class",
"Public Class C
    Public Property B As C

    Sub Main(a As C)
        Dim x As C? = a?.B
    End Sub
End Class")
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessField() As Task
            Await TestInRegularAndScriptAsync(
"Public Class C
    Sub Main(a As C)
        Dim x As C = a?[|.B|]
    End Sub
End Class",
"Public Class C
    Private B As C

    Sub Main(a As C)
        Dim x As C = a?.B
    End Sub
End Class",
index:=1)
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessField2() As Task
            Await TestInRegularAndScriptAsync(
"Public Class C
    Sub Main(a As C)
        Dim x = a?[|.B|]
    End Sub
End Class",
"Public Class C
    Private B As Object

    Sub Main(a As C)
        Dim x = a?.B
    End Sub
End Class",
index:=1)
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessField3() As Task
            Await TestInRegularAndScriptAsync(
"Public Class C
    Sub Main(a As C)
        Dim x As Integer? = a?[|.B|]
    End Sub
End Class",
"Public Class C
    Private B As Integer

    Sub Main(a As C)
        Dim x As Integer? = a?.B
    End Sub
End Class",
index:=1)
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessField4() As Task
            Await TestInRegularAndScriptAsync(
"Public Class C
    Sub Main(a As C)
        Dim x As C? = a?[|.B|]
    End Sub
End Class",
"Public Class C
    Private B As C

    Sub Main(a As C)
        Dim x As C? = a?.B
    End Sub
End Class",
index:=1)
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessReadOnlyField() As Task
            Await TestInRegularAndScriptAsync(
"Public Class C
    Sub Main(a As C)
        Dim x As C = a?[|.B|]
    End Sub
End Class",
"Public Class C
    Private ReadOnly B As C

    Sub Main(a As C)
        Dim x As C = a?.B
    End Sub
End Class",
index:=2)
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessReadOnlyField2() As Task
            Await TestInRegularAndScriptAsync(
"Public Class C
    Sub Main(a As C)
        Dim x = a?[|.B|]
    End Sub
End Class",
"Public Class C
    Private ReadOnly B As Object

    Sub Main(a As C)
        Dim x = a?.B
    End Sub
End Class",
index:=2)
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessReadOnlyField3() As Task
            Await TestInRegularAndScriptAsync(
"Public Class C
    Sub Main(a As C)
        Dim x As Integer? = a?[|.B|]
    End Sub
End Class",
"Public Class C
    Private ReadOnly B As Integer

    Sub Main(a As C)
        Dim x As Integer? = a?.B
    End Sub
End Class",
index:=2)
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessReadOnlyField4() As Task
            Await TestInRegularAndScriptAsync(
"Public Class C
    Sub Main(a As C)
        Dim x As C? = a?[|.B|]
    End Sub
End Class",
"Public Class C
    Private ReadOnly B As C

    Sub Main(a As C)
        Dim x As C? = a?.B
    End Sub
End Class",
index:=2)
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessPropertyInsideReferencedClass() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x As C = a?[|.B.Z|]
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
    End Class
End Class",
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x As C = a?.B.Z
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
        Public Property Z As C
    End Class
End Class")
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessPropertyInsideReferencedClass2() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x As Integer = a?[|.B.Z|]
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
    End Class
End Class",
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x As Integer = a?.B.Z
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
        Public Property Z As Integer
    End Class
End Class")
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessPropertyInsideReferencedClass3() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x As Integer? = a?[|.B.Z|]
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
    End Class
End Class",
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x As Integer? = a?.B.Z
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
        Public Property Z As Integer
    End Class
End Class")
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessPropertyInsideReferencedClass4() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x = a?[|.B.Z|]
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
    End Class
End Class",
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x = a?.B.Z
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
        Public Property Z As Object
    End Class
End Class")
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessFieldInsideReferencedClass() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x As C = a?[|.B.Z|]
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
    End Class
End Class",
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x As C = a?.B.Z
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
        Friend Z As C
    End Class
End Class",
index:=1)
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessFieldInsideReferencedClass2() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x As Integer = a?[|.B.Z|]
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
    End Class
End Class",
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x As Integer = a?.B.Z
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
        Friend Z As Integer
    End Class
End Class",
index:=1)
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessFieldInsideReferencedClass3() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x As Integer? = a?[|.B.Z|]
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
    End Class
End Class",
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x As Integer? = a?.B.Z
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
        Friend Z As Integer
    End Class
End Class",
index:=1)
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessFieldInsideReferencedClass4() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x = a?[|.B.Z|]
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
    End Class
End Class",
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x = a?.B.Z
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
        Friend Z As Object
    End Class
End Class",
index:=1)
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessReadOnlyFieldInsideReferencedClass() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x As C = a?[|.B.Z|]
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
    End Class
End Class",
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x As C = a?.B.Z
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
        Friend ReadOnly Z As C
    End Class
End Class",
index:=2)
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessReadOnlyFieldInsideReferencedClass2() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x As Integer = a?[|.B.Z|]
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
    End Class
End Class",
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x As Integer = a?.B.Z
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
        Friend ReadOnly Z As Integer
    End Class
End Class",
index:=2)
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessReadOnlyFieldInsideReferencedClass3() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x As Integer? = a?[|.B.Z|]
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
    End Class
End Class",
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x As Integer? = a?.B.Z
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
        Friend ReadOnly Z As Integer
    End Class
End Class",
index:=2)
        End Function

        <WorkItem(1064815, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064815")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestConditionalAccessReadOnlyFieldInsideReferencedClass4() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x = a?[|.B.Z|]
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
    End Class
End Class",
"Imports System
Public Class C
    Sub Main(a As C)
        Dim x = a?.B.Z
    End Sub
    Private Function B() As D
        Throw New NotImplementedException()
    End Function
    Private Class D
        Friend ReadOnly Z As Object
    End Class
End Class",
index:=2)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGeneratePropertyInPropertyInitializer() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Property a As Integer = [|y|]
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Private y As Integer
    Property a As Integer = y
End Module")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateFieldInPropertyInitializer() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Property a As Integer = [|y|]
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Private ReadOnly y As Integer
    Property a As Integer = y
End Module",
index:=1)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateReadonlyFieldInPropertyInitializer() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Property a As Integer = [|y|]
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Property a As Integer = y
    Public Property y As Integer
End Module",
index:=2)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGeneratePropertyInObjectInitializer1() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Dim x As New Customer With {.[|Name|] = ""blah""}
    End Sub
End Module
Friend Class Customer
End Class",
"Module Program
    Sub Main(args As String())
        Dim x As New Customer With {.Name = ""blah""}
    End Sub
End Module
Friend Class Customer
    Public Property Name As String
End Class")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGeneratePropertyInObjectInitializer2() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Dim x As New Customer With {.Name = ""blah"", .[|Age|] = blah}
    End Sub
End Module
Friend Class Customer
    Public Property Name As String
End Class",
"Module Program
    Sub Main(args As String())
        Dim x As New Customer With {.Name = ""blah"", .Age = blah}
    End Sub
End Module
Friend Class Customer
    Public Property Name As String
    Public Property Age As Object
End Class")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGeneratePropertyInObjectInitializer3() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Dim x As New Customer With {.Name = [|name|]}
    End Sub
End Module
Friend Class Customer
End Class",
"Module Program
    Public Property name As Object

    Sub Main(args As String())
        Dim x As New Customer With {.Name = name}
    End Sub
End Module
Friend Class Customer
End Class",
index:=2)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateFieldInObjectInitializer1() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Dim x As New Customer With {.[|Name|] = ""blah""}
    End Sub
End Module
Friend Class Customer
End Class",
"Module Program
    Sub Main(args As String())
        Dim x As New Customer With {.Name = ""blah""}
    End Sub
End Module
Friend Class Customer
    Friend Name As String
End Class",
index:=1)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateFieldInObjectInitializer2() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Dim x As New Customer With {.[|Name|] = name}
    End Sub
End Module
Friend Class Customer
End Class",
"Module Program
    Sub Main(args As String())
        Dim x As New Customer With {.Name = name}
    End Sub
End Module
Friend Class Customer
    Friend Name As Object
End Class",
index:=1)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateFieldInObjectInitializer3() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Dim x As New Customer With {.Name = [|name|]}
    End Sub
End Module
Friend Class Customer
End Class",
"Module Program
    Private name As Object

    Sub Main(args As String())
        Dim x As New Customer With {.Name = name}
    End Sub
End Module
Friend Class Customer
End Class")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestInvalidObjectInitializer() As Task
            Await TestMissingInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Dim x As New Customer With { [|Name|] = ""blkah""}
    End Sub
End Module
Friend Class Customer
End Class")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestOnlyPropertyAndFieldOfferedForObjectInitializer() As Task
            Await TestActionCountAsync(
"Module Program
    Sub Main(args As String())
        Dim x As New Customer With {.[|Name|] = ""blah""}
    End Sub
End Module
Friend Class Customer
End Class",
2)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateLocalInObjectInitializerValue() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Dim x As New Customer With {.Name = [|blah|]}
    End Sub
End Module
Friend Class Customer
End Class",
"Module Program
    Sub Main(args As String())
        Dim blah As Object = Nothing
        Dim x As New Customer With {.Name = blah}
    End Sub
End Module
Friend Class Customer
End Class",
index:=3)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGeneratePropertyInTypeOf() As Task
            Await TestInRegularAndScriptAsync(
"Module C
    Sub Test()
        If TypeOf [|B|] Is String Then
        End If
    End Sub
End Module",
"Module C
    Public Property B As String

    Sub Test()
        If TypeOf B Is String Then
        End If
    End Sub
End Module")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateFieldInTypeOf() As Task
            Await TestInRegularAndScriptAsync(
"Module C
    Sub Test()
        If TypeOf [|B|] Is String Then
        End If
    End Sub
End Module",
"Module C
    Private B As String

    Sub Test()
        If TypeOf B Is String Then
        End If
    End Sub
End Module",
index:=1)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateReadOnlyFieldInTypeOf() As Task
            Await TestInRegularAndScriptAsync(
"Module C
    Sub Test()
        If TypeOf [|B|] Is String Then
        End If
    End Sub
End Module",
"Module C
    Private ReadOnly B As String

    Sub Test()
        If TypeOf B Is String Then
        End If
    End Sub
End Module",
index:=2)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateLocalInTypeOf() As Task
            Await TestInRegularAndScriptAsync(
"Module C
    Sub Test()
        If TypeOf [|B|] Is String Then
        End If
    End Sub
End Module",
"Module C
    Sub Test()
        Dim B As String = Nothing

        If TypeOf B Is String Then
        End If
    End Sub
End Module",
index:=3)
        End Function

        <WorkItem(1130960, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1130960")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)>
        Public Async Function TestGeneratePropertyInTypeOfIsNot() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub M()
        If TypeOf [|Prop|] IsNot TypeOfIsNotDerived Then
        End If
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Public Property Prop As TypeOfIsNotDerived

    Sub M()
        If TypeOf Prop IsNot TypeOfIsNotDerived Then
        End If
    End Sub
End Module")
        End Function

        <WorkItem(1130960, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1130960")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateFieldInTypeOfIsNot() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub M()
        If TypeOf [|Prop|] IsNot TypeOfIsNotDerived Then
        End If
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Private Prop As TypeOfIsNotDerived

    Sub M()
        If TypeOf Prop IsNot TypeOfIsNotDerived Then
        End If
    End Sub
End Module",
index:=1)
        End Function

        <WorkItem(1130960, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1130960")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateReadOnlyFieldInTypeOfIsNot() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub M()
        If TypeOf [|Prop|] IsNot TypeOfIsNotDerived Then
        End If
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Private ReadOnly Prop As TypeOfIsNotDerived

    Sub M()
        If TypeOf Prop IsNot TypeOfIsNotDerived Then
        End If
    End Sub
End Module",
index:=2)
        End Function

        <WorkItem(1130960, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1130960")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateLocalInTypeOfIsNot() As Task
            Await TestInRegularAndScriptAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub M()
        If TypeOf [|Prop|] IsNot TypeOfIsNotDerived Then
        End If
    End Sub
End Module",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub M()
        Dim Prop As TypeOfIsNotDerived = Nothing

        If TypeOf Prop IsNot TypeOfIsNotDerived Then
        End If
    End Sub
End Module",
index:=3)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateVariableFromLambda() As Task
            Await TestInRegularAndScriptAsync(
"Class [Class]
    Private Sub Method(i As Integer)
        [|goo|] = Function()
                  Return 2
              End Function
    End Sub
End Class",
"Imports System

Class [Class]
    Private goo As Func(Of Integer)

    Private Sub Method(i As Integer)
        goo = Function()
                  Return 2
              End Function
    End Sub
End Class")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateVariableFromLambda2() As Task
            Await TestInRegularAndScriptAsync(
"Class [Class]
    Private Sub Method(i As Integer)
        [|goo|] = Function()
                  Return 2
              End Function
    End Sub
End Class",
"Imports System

Class [Class]
    Public Property goo As Func(Of Integer)

    Private Sub Method(i As Integer)
        goo = Function()
                  Return 2
              End Function
    End Sub
End Class",
index:=1)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateVariableFromLambda3() As Task
            Await TestInRegularAndScriptAsync(
"Class [Class]
    Private Sub Method(i As Integer)
        [|goo|] = Function()
                  Return 2
              End Function
    End Sub
End Class",
"Class [Class]
    Private Sub Method(i As Integer)
        Dim goo As System.Func(Of Integer)
        goo = Function()
                  Return 2
              End Function
    End Sub
End Class",
index:=2)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TupleRead() As Task
            Await TestInRegularAndScriptAsync(
"Class [Class]
    Private Sub Method(i As (Integer, String))
        Method([|tuple|])
    End Sub
End Class",
"Class [Class]
    Private tuple As (Integer, String)

    Private Sub Method(i As (Integer, String))
        Method(tuple)
    End Sub
End Class")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TupleWithOneNameRead() As Task
            Await TestInRegularAndScriptAsync(
"Class [Class]
    Private Sub Method(i As (a As Integer, String)) 
 Method([|tuple|])
    End Sub
End Class",
"Class [Class]
    Private tuple As (a As Integer, String)

    Private Sub Method(i As (a As Integer, String)) 
 Method(tuple)
    End Sub
End Class")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TupleWrite() As Task
            Await TestInRegularAndScriptAsync(
"Class [Class]
    Private Sub Method()
        [|tuple|] = (1, ""hello"") 
 End Sub
End Class",
"Class [Class]
    Private tuple As (Integer, String)

    Private Sub Method()
        tuple = (1, ""hello"") 
 End Sub
End Class")
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TupleWithOneNameWrite() As Task
            Await TestInRegularAndScriptAsync(
"Class [Class]
    Private Sub Method()
        [|tuple|] = (a:=1, ""hello"") 
 End Sub
End Class",
"Class [Class]
    Private tuple As (a As Integer, String)

    Private Sub Method()
        tuple = (a:=1, ""hello"") 
 End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestPreferReadOnlyIfAfterReadOnlyAssignment() As Task
            Await TestInRegularAndScriptAsync(
"class C
    private readonly _goo as integer

    public sub new()
        _goo = 0
        [|_bar|] = 1
    end sub
end class",
"class C
    private readonly _goo as integer
    Private ReadOnly _bar As Integer

    public sub new()
        _goo = 0
        _bar = 1
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestPreferReadOnlyIfBeforeReadOnlyAssignment() As Task
            Await TestInRegularAndScriptAsync(
"class C
    private readonly _goo as integer

    public sub new()
        [|_bar|] = 1
        _goo = 0
    end sub
end class",
"class C
    Private ReadOnly _bar As Integer
    private readonly _goo as integer

    public sub new()
        _bar = 1
        _goo = 0
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestPlaceFieldBasedOnSurroundingStatements() As Task
            Await TestInRegularAndScriptAsync(
"class Class
    private _goo as integer
    private _quux as integer

    public sub new()
        _goo = 0
        [|_bar|] = 1
        _quux = 2
    end sub
end class",
"class Class
    private _goo as integer
    Private _bar As Integer
    private _quux as integer

    public sub new()
        _goo = 0
        _bar = 1
        _quux = 2
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestPlacePropertyBasedOnSurroundingStatements() As Task
            Await TestInRegularAndScriptAsync(
"class Class
    public readonly property Goo as integer
    public readonly property Quux as integer

    public sub new()
        Goo = 0
        [|Bar|] = 1
        Quux = 2
    end sub
end class",
"class Class
    public readonly property Goo as integer
    Public ReadOnly Property Bar As Integer
    public readonly property Quux as integer

    public sub new()
        Goo = 0
        Bar = 1
        Quux = 2
    end sub
end class")
        End Function

        <WorkItem(18988, "https://github.com/dotnet/roslyn/issues/18988")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function GroupNonReadonlyFieldsTogether() As Task
            Await TestInRegularAndScriptAsync(
"
class C
    public isDisposed as boolean

    public readonly x as integer
    public readonly m as integer

    public sub new()
        me.[|y|] = 0
    end sub
end class",
"
class C
    public isDisposed as boolean
    Private y As Integer
    public readonly x as integer
    public readonly m as integer

    public sub new()
        me.y = 0
    end sub
end class")
        End Function

        <WorkItem(18988, "https://github.com/dotnet/roslyn/issues/18988")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function GroupReadonlyFieldsTogether() As Task
            Await TestInRegularAndScriptAsync("
class C
    public readonly x as integer
    public readonly m as integer

    public isDisposed as boolean

    public sub new()
        me.[|y|] = 0
    end sub
end class",
"
class C
    public readonly x as integer
    public readonly m as integer
    Private ReadOnly y As Integer
    public isDisposed as boolean

    public sub new()
        me.y = 0
    end sub
end class", index:=1)
        End Function


        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateSimplePropertyInSyncLock() As Threading.Tasks.Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        SyncLock [|Bar|]
        End SyncLock
    End Sub
End Module",
"Module Program
    Public Property Bar As Object

    Sub Main(args As String())
        SyncLock Bar
        End SyncLock
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateSimpleFieldInSyncLock() As Threading.Tasks.Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        SyncLock [|Bar|]
        End SyncLock
    End Sub
End Module",
"Module Program
    Private Bar As Object

    Sub Main(args As String())
        SyncLock Bar
        End SyncLock
    End Sub
End Module",
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestGenerateReadOnlyFieldInSyncLock() As Threading.Tasks.Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        SyncLock [|Bar|]
        End SyncLock
    End Sub
End Module",
"Module Program
    Private ReadOnly Bar As Object

    Sub Main(args As String())
        SyncLock Bar
        End SyncLock
    End Sub
End Module",
index:=2)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestAddParameter() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Goo([|bar|])
    End Sub
End Module",
"Module Program
    Sub Main(args As String(), bar As Object)
        Goo(bar)
    End Sub
End Module",
index:=4)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestAddParameterDoesntAddToOverride() As Task
            Await TestInRegularAndScriptAsync(
"Class Base
    Public Overridable Sub Method(args As String())
    End Sub
End Class
Class Program
    Public Overrides Sub Main(args As String())
        Goo([|bar|])
    End Sub
End Class",
"Class Base
    Public Overridable Sub Method(args As String())
    End Sub
End Class
Class Program
    Public Overrides Sub Main(args As String(), bar As Object)
        Goo(bar)
    End Sub
End Class",
index:=4)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestAddParameterAndOverridesAddsToOverrides() As Task
            Await TestInRegularAndScriptAsync(
"Class Base
    Public Overridable Sub Method(args As String())
    End Sub
End Class
Class Program
    Inherits Base
    Public Overrides Sub Method(args As String())
        Goo([|bar|])
    End Sub
End Class",
"Class Base
    Public Overridable Sub Method(args As String(), bar As Object)
    End Sub
End Class
Class Program
    Inherits Base
    Public Overrides Sub Method(args As String(), bar As Object)
        Goo(bar)
    End Sub
End Class",
index:=5)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestAddParameterIsOfCorrectType() As Task
            Await TestInRegularAndScriptAsync(
"Module Program
    Sub Main(args As String())
        Goo([|bar|])
    End Sub
    Sub Goo(arg As Integer)
    End Sub
End Module",
"Module Program
    Sub Main(args As String(), bar As Integer)
        Goo(bar)
    End Sub
    Sub Goo(arg As Integer)
    End Sub
End Module",
index:=4)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestAddParameterAndOverridesIsOfCorrectType() As Task
            Await TestInRegularAndScriptAsync(
"Class Base
    Public Overridable Sub Method(args As String())
    End Sub
End Class
Class Program
    Inherits Base
    Public Overrides Sub Method(args As String())
        Goo([|bar|])
    End Sub
    Sub Goo(arg As Integer)
    End Sub
End Class",
"Class Base
    Public Overridable Sub Method(args As String(), bar As Integer)
    End Sub
End Class
Class Program
    Inherits Base
    Public Overrides Sub Method(args As String(), bar As Integer)
        Goo(bar)
    End Sub
    Sub Goo(arg As Integer)
    End Sub
End Class",
index:=5)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestAddParameterAndOverridesNotOfferedToNonOverride1() As Task
            Await TestActionCountAsync(
"Module Program
    Sub Main(args As String())
        Goo([|bar|])
    End Sub
End Module",
count:=5)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateVariable)>
        Public Async Function TestAddParameterAndOverridesNotOfferedToNonOverride2() As Task
            Await TestActionCountAsync(
"Class Base
    Public Overridable Sub Method(args As String())
    End Sub
End Class
Class Program
    Inherits Base
    Public Sub Method(args As String())
        Goo([|bar|])
    End Sub
    Sub Goo(arg As Integer)
    End Sub
End Class",
count:=5)
        End Function
    End Class
End Namespace
