Option Strict Off
' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.GenerateDefaultConstructors
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.GenerateDefaultConstructors
    Public Class GenerateDefaultConstructorsTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace) As Object
            Return New GenerateDefaultConstructorsCodeRefactoringProvider()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestException0() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class Program \n Inherits [||]Exception \n Sub Main(args As String()) \n End Sub \n End Class"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class Program \n Inherits Exception \n Public Sub New(message As String) \n MyBase.New(message) \n End Sub \n Sub Main(args As String()) \n End Sub \n End Class"),
index:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestException1() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class Program \n Inherits [||]Exception \n Sub Main(args As String()) \n End Sub \n End Class"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class Program \n Inherits Exception \n Public Sub New(message As String, innerException As Exception) \n MyBase.New(message, innerException) \n End Sub \n Sub Main(args As String()) \n End Sub \n End Class"),
index:=1)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestException2() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class Program \n Inherits [||]Exception \n Sub Main(args As String()) \n End Sub \n End Class"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Imports System.Runtime.Serialization \n Class Program \n Inherits Exception \n Protected Sub New(info As SerializationInfo, context As StreamingContext) \n MyBase.New(info, context) \n End Sub \n Sub Main(args As String()) \n End Sub \n End Class"),
index:=2)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestException3() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class Program \n Inherits [||]Exception \n Sub Main(args As String()) \n End Sub \n End Class"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Imports System.Runtime.Serialization \n Class Program \n Inherits Exception \n Public Sub New() \n End Sub \n Public Sub New(message As String) \n MyBase.New(message) \n End Sub \n Public Sub New(message As String, innerException As Exception) \n MyBase.New(message, innerException) \n End Sub \n Protected Sub New(info As SerializationInfo, context As StreamingContext) \n MyBase.New(info, context) \n End Sub \n Sub Main(args As String()) \n End Sub \n End Class"),
index:=3)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        <WorkItem(539676)>
        Public Async Function TestNotOfferedOnResolvedBaseClassName() As Task
            Await TestMissingAsync(
NewLines("Class Base \n End Class \n Class Derived \n Inherits B[||]ase \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestNotOfferedOnUnresolvedBaseClassName() As Task
            Await TestMissingAsync(
NewLines("Class Derived \n Inherits [||]Base \n End Class"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestNotOfferedOnInheritsStatementForStructures() As Task
            Await TestMissingAsync(
NewLines("Structure Derived \n Inherits [||]Base \n End Structure"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestNotOfferedForIncorrectlyParentedInheritsStatement() As Task
            Await TestMissingAsync(
NewLines("Inherits [||]Foo"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestWithDefaultConstructor() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class Program \n Inherits [||]Exception \n Public Sub New() \n End Sub \n Sub Main(args As String()) \n End Sub \n End Class"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Imports System.Runtime.Serialization \n Class Program \n Inherits Exception \n Public Sub New() \n End Sub \n Public Sub New(message As String) \n MyBase.New(message) \n End Sub \n Public Sub New(message As String, innerException As Exception) \n MyBase.New(message, innerException) \n End Sub \n Protected Sub New(info As SerializationInfo, context As StreamingContext) \n MyBase.New(info, context) \n End Sub \n Sub Main(args As String()) \n End Sub \n End Class"),
index:=3)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestWithDefaultConstructorMissing1() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class Program \n Inherits [||]Exception \n Public Sub New(message As String) \n MyBase.New(message) \n End Sub \n Public Sub New(message As String, innerException As Exception) \n MyBase.New(message, innerException) \n End Sub \n Protected Sub New(info As Runtime.Serialization.SerializationInfo, context As Runtime.Serialization.StreamingContext) \n MyBase.New(info, context) \n End Sub \n Sub Main(args As String()) \n End Sub \n End Class"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class Program \n Inherits Exception \n Public Sub New() \n End Sub \n Public Sub New(message As String) \n MyBase.New(message) \n End Sub \n Public Sub New(message As String, innerException As Exception) \n MyBase.New(message, innerException) \n End Sub \n Protected Sub New(info As Runtime.Serialization.SerializationInfo, context As Runtime.Serialization.StreamingContext) \n MyBase.New(info, context) \n End Sub \n Sub Main(args As String()) \n End Sub \n End Class"),
index:=0)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestWithDefaultConstructorMissing2() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class Program \n Inherits [||]Exception \n Public Sub New(message As String, innerException As Exception) \n MyBase.New(message, innerException) \n End Sub \n Protected Sub New(info As Runtime.Serialization.SerializationInfo, context As Runtime.Serialization.StreamingContext) \n MyBase.New(info, context) \n End Sub \n Sub Main(args As String()) \n End Sub \n End Class"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class Program \n Inherits Exception \n Public Sub New() \n End Sub \n Public Sub New() \n End Sub \n Public Sub New(message As String) \n MyBase.New(message) \n End Sub \n Public Sub New(message As String, innerException As Exception) \n MyBase.New(message, innerException) \n End Sub \n Protected Sub New(info As Runtime.Serialization.SerializationInfo, context As Runtime.Serialization.StreamingContext) \n MyBase.New(info, context) \n End Sub \n Sub Main(args As String()) \n End Sub \n End Class"),
index:=2)
        End Function

        <WorkItem(540712)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestEndOfToken() As Task
            Await TestAsync(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class Program \n Inherits Exception[||] \n Sub Main(args As String()) \n End Sub \n End Class"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class Program \n Inherits Exception \n Public Sub New(message As String) \n MyBase.New(message) \n End Sub \n Sub Main(args As String()) \n End Sub \n End Class"),
index:=0)
        End Function

        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
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

        <WorkItem(889349)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
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
