' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.GenerateDefaultConstructors

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.GenerateDefaultConstructors
    Public Class GenerateConstructorForAbstractClassTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New GenerateDefaultConstructorsCodeRefactoringProvider()
        End Function

        <WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestGenerateConstructorFromFriendConstructor() As Task
            Await TestInRegularAndScriptAsync(
<Text>Class C
    Inherits B[||]
End Class

MustInherit Class B
    Friend Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Class C
    Inherits B

    Public Sub New(x As Integer)
        MyBase.New(x)
    End Sub
End Class

MustInherit Class B
    Friend Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestGenerateConstructorFromFriendConstructor2() As Task
            Await TestInRegularAndScriptAsync(
<Text>MustInherit Class C
    Inherits B[||]
End Class

MustInherit Class B
    Friend Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf),
<Text>MustInherit Class C
    Inherits B

    Friend Sub New(x As Integer)
        MyBase.New(x)
    End Sub
End Class

MustInherit Class B
    Friend Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestGenerateConstructorFromProtectedConstructor() As Task
            Await TestInRegularAndScriptAsync(
<Text>Class C
    Inherits B[||]
End Class

MustInherit Class B
    Protected Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Class C
    Inherits B

    Public Sub New(x As Integer)
        MyBase.New(x)
    End Sub
End Class

MustInherit Class B
    Protected Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestGenerateConstructorFromProtectedConstructor2() As Task
            Await TestInRegularAndScriptAsync(
<Text>MustInherit Class C
    Inherits B[||]
End Class

MustInherit Class B
    Protected Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf),
<Text>MustInherit Class C
    Inherits B

    Protected Sub New(x As Integer)
        MyBase.New(x)
    End Sub
End Class

MustInherit Class B
    Protected Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestGenerateConstructorFromProtectedFriendConstructor() As Task
            Await TestInRegularAndScriptAsync(
<Text>Class C
    Inherits B[||]
End Class

MustInherit Class B
    Protected Friend Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Class C
    Inherits B

    Public Sub New(x As Integer)
        MyBase.New(x)
    End Sub
End Class

MustInherit Class B
    Protected Friend Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestGenerateConstructorFromProtectedFriendConstructor2() As Task
            Await TestInRegularAndScriptAsync(
<Text>MustInherit Class C
    Inherits B[||]
End Class

MustInherit Class B
    Protected Friend Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf),
<Text>MustInherit Class C
    Inherits B

    Protected Friend Sub New(x As Integer)
        MyBase.New(x)
    End Sub
End Class

MustInherit Class B
    Protected Friend Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestGenerateConstructorFromPublicConstructor() As Task
            Await TestInRegularAndScriptAsync(
<Text>Class C
    Inherits B[||]
End Class

MustInherit Class B
    Public Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf),
<Text>Class C
    Inherits B

    Public Sub New(x As Integer)
        MyBase.New(x)
    End Sub
End Class

MustInherit Class B
    Public Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf))
        End Function

        <WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")>
        <Fact(), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)>
        Public Async Function TestGenerateConstructorFromPublicConstructor2() As Task
            Await TestInRegularAndScriptAsync(
<Text>MustInherit Class C
    Inherits B[||]
End Class

MustInherit Class B
    Public Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf),
<Text>MustInherit Class C
    Inherits B

    Public Sub New(x As Integer)
        MyBase.New(x)
    End Sub
End Class

MustInherit Class B
    Public Sub New(x As Integer)
    End Sub
End Class</Text>.Value.Replace(vbLf, vbCrLf))
        End Function
    End Class
End Namespace
