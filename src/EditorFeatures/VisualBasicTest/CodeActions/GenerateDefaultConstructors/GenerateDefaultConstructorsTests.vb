' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.CodeRefactorings.GenerateDefaultConstructors

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.GenerateDefaultConstructors
    Public Class GenerateDefaultConstructorsTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace) As CodeRefactoringProvider
            Return New GenerateDefaultConstructorsCodeRefactoringProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestException0() As Task
            Await TestAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class Program
    Inherits [||]Exception
    Sub Main(args As String())
    End Sub
End Class",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class Program
    Inherits Exception
    Public Sub New(message As String)
        MyBase.New(message)
    End Sub
    Sub Main(args As String())
    End Sub
End Class",
index:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestException1() As Task
            Await TestAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class Program
    Inherits [||]Exception
    Sub Main(args As String())
    End Sub
End Class",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class Program
    Inherits Exception
    Public Sub New(message As String, innerException As Exception)
        MyBase.New(message, innerException)
    End Sub
    Sub Main(args As String())
    End Sub
End Class",
index:=1)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestException2() As Task
            Await TestAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class Program
    Inherits [||]Exception
    Sub Main(args As String())
    End Sub
End Class",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.Serialization
Class Program
    Inherits Exception
    Protected Sub New(info As SerializationInfo, context As StreamingContext)
        MyBase.New(info, context)
    End Sub
    Sub Main(args As String())
    End Sub
End Class",
index:=2)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestException3() As Task
            Await TestAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class Program
    Inherits [||]Exception
    Sub Main(args As String())
    End Sub
End Class",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.Serialization
Class Program
    Inherits Exception
    Public Sub New()
    End Sub
    Public Sub New(message As String)
        MyBase.New(message)
    End Sub
    Public Sub New(message As String, innerException As Exception)
        MyBase.New(message, innerException)
    End Sub
    Protected Sub New(info As SerializationInfo, context As StreamingContext)
        MyBase.New(info, context)
    End Sub
    Sub Main(args As String())
    End Sub
End Class",
index:=3)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        <WorkItem(539676, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539676")>
        Public Async Function TestNotOfferedOnResolvedBaseClassName() As Task
            Await TestMissingAsync(
"Class Base
End Class
Class Derived
    Inherits B[||]ase
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestNotOfferedOnUnresolvedBaseClassName() As Task
            Await TestMissingAsync(
"Class Derived
    Inherits [||]Base
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestNotOfferedOnInheritsStatementForStructures() As Task
            Await TestMissingAsync(
"Structure Derived
    Inherits [||]Base
End Structure")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestNotOfferedForIncorrectlyParentedInheritsStatement() As Task
            Await TestMissingAsync(
"Inherits [||]Foo")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestWithDefaultConstructor() As Task
            Await TestAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class Program
    Inherits [||]Exception
    Public Sub New()
    End Sub
    Sub Main(args As String())
    End Sub
End Class",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.Serialization
Class Program
    Inherits Exception
    Public Sub New()
    End Sub
    Public Sub New(message As String)
        MyBase.New(message)
    End Sub
    Public Sub New(message As String, innerException As Exception)
        MyBase.New(message, innerException)
    End Sub
    Protected Sub New(info As SerializationInfo, context As StreamingContext)
        MyBase.New(info, context)
    End Sub
    Sub Main(args As String())
    End Sub
End Class",
index:=3)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestWithDefaultConstructorMissing1() As Task
            Await TestAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class Program
    Inherits [||]Exception
    Public Sub New(message As String)
        MyBase.New(message)
    End Sub
    Public Sub New(message As String, innerException As Exception)
        MyBase.New(message, innerException)
    End Sub
    Protected Sub New(info As Runtime.Serialization.SerializationInfo, context As Runtime.Serialization.StreamingContext)
        MyBase.New(info, context)
    End Sub
    Sub Main(args As String())
    End Sub
End Class",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class Program
    Inherits Exception
    Public Sub New()
    End Sub
    Public Sub New(message As String)
        MyBase.New(message)
    End Sub
    Public Sub New(message As String, innerException As Exception)
        MyBase.New(message, innerException)
    End Sub
    Protected Sub New(info As Runtime.Serialization.SerializationInfo, context As Runtime.Serialization.StreamingContext)
        MyBase.New(info, context)
    End Sub
    Sub Main(args As String())
    End Sub
End Class",
index:=0)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestWithDefaultConstructorMissing2() As Task
            Await TestAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class Program
    Inherits [||]Exception
    Public Sub New(message As String, innerException As Exception)
        MyBase.New(message, innerException)
    End Sub
    Protected Sub New(info As Runtime.Serialization.SerializationInfo, context As Runtime.Serialization.StreamingContext)
        MyBase.New(info, context)
    End Sub
    Sub Main(args As String())
    End Sub
End Class",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class Program
    Inherits Exception
    Public Sub New()
    End Sub
    Public Sub New()
    End Sub
    Public Sub New(message As String)
        MyBase.New(message)
    End Sub
    Public Sub New(message As String, innerException As Exception)
        MyBase.New(message, innerException)
    End Sub
    Protected Sub New(info As Runtime.Serialization.SerializationInfo, context As Runtime.Serialization.StreamingContext)
        MyBase.New(info, context)
    End Sub
    Sub Main(args As String())
    End Sub
End Class",
index:=2)
        End Function

        <WorkItem(540712, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540712")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestEndOfToken() As Task
            Await TestAsync(
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class Program
    Inherits Exception[||]
    Sub Main(args As String())
    End Sub
End Class",
"Imports System
Imports System.Collections.Generic
Imports System.Linq
Class Program
    Inherits Exception
    Public Sub New(message As String)
        MyBase.New(message)
    End Sub
    Sub Main(args As String())
    End Sub
End Class",
index:=0)
        End Function

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestFormattingInGenerateDefaultConstructor() As Task
            Await TestAsync(
<Text>Imports System
Imports System.Collections.Generic
Imports System.Linq
Class Program
    Inherits Exce[||]ption
    Public Sub New()
    End Sub
    Sub Main(args As String())
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Imports System
Imports System.Collections.Generic
Imports System.Linq
Class Program
    Inherits Exception
    Public Sub New()
    End Sub

    Public Sub New(message As String)
        MyBase.New(message)
    End Sub

    Sub Main(args As String())
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf),
index:=0,
compareTokens:=False)
        End Function

        <WorkItem(889349, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/889349")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestDefaultConstructorGeneration() As Task
            Await TestAsync(
<Text>Class C
    Inherits B[||]
    Public Sub New(y As Integer)
    End Sub
End Class

Class B
    Friend Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Class C
    Inherits B
    Public Sub New(y As Integer)
    End Sub

    Friend Sub New(x As Integer)
        MyBase.New(x)
    End Sub
End Class

Class B
    Friend Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf),
index:=0,
compareTokens:=False)
        End Function
    End Class
End Namespace