' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Sub TestException0()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class Program \n Inherits [||]Exception \n Sub Main(args As String()) \n End Sub \n End Class"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class Program \n Inherits Exception \n Public Sub New(message As String) \n MyBase.New(message) \n End Sub \n Sub Main(args As String()) \n End Sub \n End Class"),
index:=0)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Sub TestException1()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class Program \n Inherits [||]Exception \n Sub Main(args As String()) \n End Sub \n End Class"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class Program \n Inherits Exception \n Public Sub New(message As String, innerException As Exception) \n MyBase.New(message, innerException) \n End Sub \n Sub Main(args As String()) \n End Sub \n End Class"),
index:=1)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Sub TestException2()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class Program \n Inherits [||]Exception \n Sub Main(args As String()) \n End Sub \n End Class"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Imports System.Runtime.Serialization \n Class Program \n Inherits Exception \n Protected Sub New(info As SerializationInfo, context As StreamingContext) \n MyBase.New(info, context) \n End Sub \n Sub Main(args As String()) \n End Sub \n End Class"),
index:=2)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Sub TestException3()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class Program \n Inherits [||]Exception \n Sub Main(args As String()) \n End Sub \n End Class"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Imports System.Runtime.Serialization \n Class Program \n Inherits Exception \n Public Sub New() \n End Sub \n Public Sub New(message As String) \n MyBase.New(message) \n End Sub \n Public Sub New(message As String, innerException As Exception) \n MyBase.New(message, innerException) \n End Sub \n Protected Sub New(info As SerializationInfo, context As StreamingContext) \n MyBase.New(info, context) \n End Sub \n Sub Main(args As String()) \n End Sub \n End Class"),
index:=3)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        <WorkItem(539676)>
        Public Sub TestNotOfferedOnResolvedBaseClassName()
            TestMissing(
NewLines("Class Base \n End Class \n Class Derived \n Inherits B[||]ase \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Sub TestNotOfferedOnUnresolvedBaseClassName()
            TestMissing(
NewLines("Class Derived \n Inherits [||]Base \n End Class"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Sub TestNotOfferedOnInheritsStatementForStructures()
            TestMissing(
NewLines("Structure Derived \n Inherits [||]Base \n End Structure"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Sub TestNotOfferedForIncorrectlyParentedInheritsStatement()
            TestMissing(
NewLines("Inherits [||]Foo"))
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Sub TestWithDefaultConstructor()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class Program \n Inherits [||]Exception \n Public Sub New() \n End Sub \n Sub Main(args As String()) \n End Sub \n End Class"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Imports System.Runtime.Serialization \n Class Program \n Inherits Exception \n Public Sub New() \n End Sub \n Public Sub New(message As String) \n MyBase.New(message) \n End Sub \n Public Sub New(message As String, innerException As Exception) \n MyBase.New(message, innerException) \n End Sub \n Protected Sub New(info As SerializationInfo, context As StreamingContext) \n MyBase.New(info, context) \n End Sub \n Sub Main(args As String()) \n End Sub \n End Class"),
index:=3)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Sub TestWithDefaultConstructorMissing1()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class Program \n Inherits [||]Exception \n Public Sub New(message As String) \n MyBase.New(message) \n End Sub \n Public Sub New(message As String, innerException As Exception) \n MyBase.New(message, innerException) \n End Sub \n Protected Sub New(info As Runtime.Serialization.SerializationInfo, context As Runtime.Serialization.StreamingContext) \n MyBase.New(info, context) \n End Sub \n Sub Main(args As String()) \n End Sub \n End Class"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class Program \n Inherits Exception \n Public Sub New() \n End Sub \n Public Sub New(message As String) \n MyBase.New(message) \n End Sub \n Public Sub New(message As String, innerException As Exception) \n MyBase.New(message, innerException) \n End Sub \n Protected Sub New(info As Runtime.Serialization.SerializationInfo, context As Runtime.Serialization.StreamingContext) \n MyBase.New(info, context) \n End Sub \n Sub Main(args As String()) \n End Sub \n End Class"),
index:=0)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Sub TestWithDefaultConstructorMissing2()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class Program \n Inherits [||]Exception \n Public Sub New(message As String, innerException As Exception) \n MyBase.New(message, innerException) \n End Sub \n Protected Sub New(info As Runtime.Serialization.SerializationInfo, context As Runtime.Serialization.StreamingContext) \n MyBase.New(info, context) \n End Sub \n Sub Main(args As String()) \n End Sub \n End Class"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class Program \n Inherits Exception \n Public Sub New() \n End Sub \n Public Sub New() \n End Sub \n Public Sub New(message As String) \n MyBase.New(message) \n End Sub \n Public Sub New(message As String, innerException As Exception) \n MyBase.New(message, innerException) \n End Sub \n Protected Sub New(info As Runtime.Serialization.SerializationInfo, context As Runtime.Serialization.StreamingContext) \n MyBase.New(info, context) \n End Sub \n Sub Main(args As String()) \n End Sub \n End Class"),
index:=2)
        End Sub

        <WorkItem(540712)>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Sub TestEndOfToken()
            Test(
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class Program \n Inherits Exception[||] \n Sub Main(args As String()) \n End Sub \n End Class"),
NewLines("Imports System \n Imports System.Collections.Generic \n Imports System.Linq \n Class Program \n Inherits Exception \n Public Sub New(message As String) \n MyBase.New(message) \n End Sub \n Sub Main(args As String()) \n End Sub \n End Class"),
index:=0)
        End Sub

        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Sub TestFormattingInGenerateDefaultConstructor()
            Test(
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
        End Sub

        <WorkItem(889349)>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Sub TestDefaultConstructorGeneration()
            Test(
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
        End Sub

    End Class
End Namespace
