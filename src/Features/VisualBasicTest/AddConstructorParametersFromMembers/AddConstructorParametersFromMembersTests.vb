' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.Testing
Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeRefactoringVerifier(Of
    Microsoft.CodeAnalysis.AddConstructorParametersFromMembers.AddConstructorParametersFromMembersCodeRefactoringProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.AddConstructorParametersFromMembers
    <UseExportProvider>
    <Trait(Traits.Feature, Traits.Features.CodeActionsAddConstructorParametersFromMembers)>
    Public Class AddConstructorParametersFromMembersTests
        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530592")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/33603")>
        Public Async Function TestAdd1() As Task
            Dim source =
"Class Program
    [|Private i As Integer
    Private s As String|]
    Public Sub New(i As Integer)
        Me.i = i
    End Sub
End Class"
            Dim fixedSource =
"Class Program
    Private i As Integer
    Private s As String
    Public Sub New(i As Integer, s As String)
        Me.i = i
        Me.s = s
    End Sub
End Class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = fixedSource
            test.CodeActionVerifier = Sub(codeAction As CodeAction, verifier As IVerifier)
                                          verifier.Equal(String.Format(FeaturesResources.Add_parameters_to_0, "Program(Integer)"), codeAction.Title)
                                      End Sub
            Await test.RunAsync()

        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58040")>
        Public Async Function TestProperlyWrapParameters1() As Task
            Dim source =
"Class Program
    [|Private i As Integer
    Private s As String|]
    Public Sub New(
            i As Integer)
        Me.i = i
    End Sub
End Class"
            Dim fixedSource =
"Class Program
    Private i As Integer
    Private s As String
    Public Sub New(
            i As Integer, s As String)
        Me.i = i
        Me.s = s
    End Sub
End Class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = fixedSource
            test.CodeActionVerifier = Sub(codeAction As CodeAction, verifier As IVerifier)
                                          verifier.Equal(String.Format(FeaturesResources.Add_parameters_to_0, "Program(Integer)"), codeAction.Title)
                                      End Sub
            Await test.RunAsync()
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58040")>
        Public Async Function TestProperlyWrapParameters2() As Task
            Dim source =
"Class Program
    [|Private i As Integer
    Private s As String
    Private b As Boolean|]
    Public Sub New(
            i As Integer,
            s As String)
        Me.i = i
        Me.s = s
    End Sub
End Class"
            Dim fixedSource =
"Class Program
    Private i As Integer
    Private s As String
    Private b As Boolean
    Public Sub New(
            i As Integer,
            s As String,
            b As Boolean)
        Me.i = i
        Me.s = s
        Me.b = b
    End Sub
End Class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = fixedSource
            test.CodeActionVerifier = Sub(codeAction As CodeAction, verifier As IVerifier)
                                          verifier.Equal(String.Format(FeaturesResources.Add_parameters_to_0, "Program(Integer, String)"), codeAction.Title)
                                      End Sub
            Await test.RunAsync()
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58040")>
        Public Async Function TestProperlyWrapParameters3() As Task
            Dim source =
"Class Program
    [|Private i As Integer
    Private s As String
    Private b As Boolean|]
    Public Sub New(i As Integer,
            s As String)
        Me.i = i
        Me.s = s
    End Sub
End Class"
            Dim fixedSource =
"Class Program
    Private i As Integer
    Private s As String
    Private b As Boolean
    Public Sub New(i As Integer,
            s As String,
            b As Boolean)
        Me.i = i
        Me.s = s
        Me.b = b
    End Sub
End Class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = fixedSource
            test.CodeActionVerifier = Sub(codeAction As CodeAction, verifier As IVerifier)
                                          verifier.Equal(String.Format(FeaturesResources.Add_parameters_to_0, "Program(Integer, String)"), codeAction.Title)
                                      End Sub
            Await test.RunAsync()
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58040")>
        Public Async Function TestProperlyWrapParameters4() As Task
            Dim source =
"Class Program
    [|Private i As Integer
    Private s As String
    Private b As Boolean|]
    Public Sub New(i As Integer,
                   s As String)
        Me.i = i
        Me.s = s
    End Sub
End Class"
            Dim fixedSource =
"Class Program
    Private i As Integer
    Private s As String
    Private b As Boolean
    Public Sub New(i As Integer,
                   s As String,
                   b As Boolean)
        Me.i = i
        Me.s = s
        Me.b = b
    End Sub
End Class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = fixedSource
            test.CodeActionVerifier = Sub(codeAction As CodeAction, verifier As IVerifier)
                                          verifier.Equal(String.Format(FeaturesResources.Add_parameters_to_0, "Program(Integer, String)"), codeAction.Title)
                                      End Sub
            Await test.RunAsync()
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530592")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/33603")>
        Public Async Function TestAddOptional1() As Task
            Dim source =
"Class Program
    [|Private i As Integer
    Private s As String|]
    Public Sub New(i As Integer)
        Me.i = i
    End Sub
End Class"
            Dim fixedSource =
"Class Program
    Private i As Integer
    Private s As String
    Public Sub New(i As Integer, Optional s As String = Nothing)
        Me.i = i
        Me.s = s
    End Sub
End Class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = fixedSource
            test.CodeActionIndex = 1
            test.CodeActionVerifier = Sub(codeAction As CodeAction, verifier As IVerifier)
                                          verifier.Equal(String.Format(FeaturesResources.Add_optional_parameters_to_0, "Program(Integer)"), codeAction.Title)
                                      End Sub
            Await test.RunAsync()

        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530592")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/33603")>
        Public Async Function TestAddToConstructorWithMostMatchingParameters1() As Task
            ' behavior change with 33603, now all constructors offered
            Dim source =
"Class Program
    [|Private i As Integer
    Private s As String
    Private b As Boolean|]
    Public Sub New(i As Integer)
        Me.i = i
    End Sub
    Public Sub New(i As Integer, s As String)
        Me.New(i)
        Me.s = s
    End Sub
End Class"
            Dim fixedSource =
"Class Program
    Private i As Integer
    Private s As String
    Private b As Boolean
    Public Sub New(i As Integer)
        Me.i = i
    End Sub
    Public Sub New(i As Integer, s As String, b As Boolean)
        Me.New(i)
        Me.s = s
        Me.b = b
    End Sub
End Class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = fixedSource
            test.CodeActionIndex = 1
            test.CodeActionVerifier = Sub(codeAction As CodeAction, verifier As IVerifier)
                                          verifier.Equal(String.Format(CodeFixesResources.Add_to_0, "Program(Integer, String)"), codeAction.Title)
                                      End Sub
            Await test.RunAsync()

        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530592")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/33603")>
        Public Async Function TestAddOptionalToConstructorWithMostMatchingParameters1() As Task
            ' behavior change with 33603, now all constructors offered
            Dim source =
"Class Program
    [|Private i As Integer
    Private s As String
    Private b As Boolean|]
    Public Sub New(i As Integer)
        Me.i = i
    End Sub
    Public Sub New(i As Integer, s As String)
        Me.New(i)
        Me.s = s
    End Sub
End Class"
            Dim fixedSource =
"Class Program
    Private i As Integer
    Private s As String
    Private b As Boolean
    Public Sub New(i As Integer)
        Me.i = i
    End Sub
    Public Sub New(i As Integer, s As String, Optional b As Boolean = Nothing)
        Me.New(i)
        Me.s = s
        Me.b = b
    End Sub
End Class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = fixedSource
            test.CodeActionIndex = 3
            test.CodeActionVerifier = Sub(codeAction As CodeAction, verifier As IVerifier)
                                          verifier.Equal(String.Format(CodeFixesResources.Add_to_0, "Program(Integer, String)"), codeAction.Title)
                                      End Sub
            Await test.RunAsync()

        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28775")>
        Public Async Function TestAddParamtersToConstructorBySelectOneMember() As Task
            Dim source =
"Class Program
    Private i As Integer
    [|Private k As Integer|]
    Private j As Integer
    Public Sub New(i As Integer, j As Integer)
        Me.i = i
        Me.j = j
    End Sub
End Class"
            Dim fixedSource =
"Class Program
    Private i As Integer
    Private k As Integer
    Private j As Integer
    Public Sub New(i As Integer, j As Integer, k As Integer)
        Me.i = i
        Me.j = j
        Me.k = k
    End Sub
End Class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = fixedSource
            Await test.RunAsync()

        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28775")>
        Public Async Function TestParametersAreStillRightIfMembersAreOutOfOrder() As Task
            Dim source =
"Class Program
    [|Private i As Integer
    Private k As Integer
    Private j As Integer|]
    Public Sub New(i As Integer, j As Integer)
        Me.i = i
        Me.j = j
    End Sub
End Class"
            Dim fixedSource =
"Class Program
    [|Private i As Integer
    Private k As Integer
    Private j As Integer|]
    Public Sub New(i As Integer, j As Integer, k As Integer)
        Me.i = i
        Me.j = j
        Me.k = k
    End Sub
End Class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = fixedSource
            Await test.RunAsync()

        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28775")>
        Public Async Function TestNormalProperty() As Task
            Dim source =
"
Class Program
    [|Private i As Integer
    Property Hello As Integer = 1|]
    Public Sub New(i As Integer)
    End Sub
End Class"
            Dim fixedSource =
"
Class Program
    Private i As Integer
    Property Hello As Integer = 1
    Public Sub New(i As Integer, hello As Integer)
        Me.Hello = hello
    End Sub
End Class"

            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = fixedSource
            Await test.RunAsync()

        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28775")>
        Public Async Function TestMissingIfFieldsAndPropertyAlreadyExists() As Task
            Dim source =
"
Class Program
    [|Private i As Integer
    Property Hello As Integer = 1|]
    Public Sub New(i As Integer, hello As Integer)
    End Sub
End Class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = source
            Await test.RunAsync()

        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33602")>
        Public Async Function TestConstructorWithNoParameters() As Task
            Dim source =
"
Class Program
    [|Private i As Integer
    Property Hello As Integer = 1|]
    Public Sub New()
    End Sub
End Class"
            Dim fixedSource =
"
Class Program
    [|Private i As Integer
    Property Hello As Integer = 1|]
    Public Sub New(i As Integer, hello As Integer)
        Me.i = i
        Me.Hello = hello
    End Sub
End Class"

            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = fixedSource
            Await test.RunAsync()

        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33602")>
        Public Async Function TestDefaultConstructor() As Task
            Dim source =
"
Class Program
    [|Private i As Integer|]
End Class"

            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = source
            Await test.RunAsync()
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33601")>
        Public Async Function TestPartialSelection() As Task
            Dim source =
"Class Program
    Private i As Integer
    Private [|s|] As String
    Public Sub New(i As Integer)
        Me.i = i
    End Sub
End Class"
            Dim fixedSource =
"Class Program
    Private i As Integer
    Private s As String
    Public Sub New(i As Integer, s As String)
        Me.i = i
        Me.s = s
    End Sub
End Class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = fixedSource
            Await test.RunAsync()

        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33601")>
        Public Async Function TestMultiplePartialSelection() As Task
            Dim source =
"Class Program
    Private i As Integer
    Private [|s As String
    Private j|] As Integer
    Public Sub New(i As Integer)
        Me.i = i
    End Sub
End Class"
            Dim fixedSource =
"Class Program
    Private i As Integer
    Private s As String
    Private j As Integer
    Public Sub New(i As Integer, s As String, j As Integer)
        Me.i = i
        Me.s = s
        Me.j = j
    End Sub
End Class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = fixedSource
            Await test.RunAsync()

        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33601")>
        Public Async Function TestMultiplePartialSelection2() As Task
            Dim source =
"Class Program
    Private i As Integer
    Private [|s As String
    Private |]j As Integer
    Public Sub New(i As Integer)
        Me.i = i
    End Sub
End Class"
            Dim fixedSource =
"Class Program
    Private i As Integer
    Private s As String
    Private j As Integer
    Public Sub New(i As Integer, s As String)
        Me.i = i
        Me.s = s
    End Sub
End Class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = fixedSource
            Await test.RunAsync()

        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33603")>
        Public Async Function TestMultipleConstructors_FirstOfThree() As Task
            Dim source =
"Class Program
    Private i as Integer
    Private [|l|] As Integer

    Public Sub New(i As Integer)
        Me.i = i
    End Sub

    Public Sub New(i As Integer, j As Integer)
    End Sub

    Public Sub New(i As Integer, j As Integer, k As Integer)
    End Sub
End Class"
            Dim fixedSource =
"Class Program
    Private i as Integer
    Private [|l|] As Integer

    Public Sub {|BC30269:New|}(i As Integer, l As Integer)
        Me.i = i
        Me.l = l
    End Sub

    Public Sub New(i As Integer, j As Integer)
    End Sub

    Public Sub New(i As Integer, j As Integer, k As Integer)
    End Sub
End Class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = fixedSource
            test.CodeActionVerifier = Sub(codeAction As CodeAction, verifier As IVerifier)
                                          verifier.Equal(String.Format(CodeFixesResources.Add_to_0, "Program(Integer)"), codeAction.Title)
                                      End Sub
            Await test.RunAsync()

        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33603")>
        Public Async Function TestMultipleConstructors_SecondOfThree() As Task
            Dim source =
"Class Program
    Private i as Integer
    Private [|l|] As Integer

    Public Sub New(i As Integer)
        Me.i = i
    End Sub

    Public Sub New(i As Integer, j As Integer)
    End Sub

    Public Sub New(i As Integer, j As Integer, k As Integer)
    End Sub
End Class"
            Dim fixedSource =
"Class Program
    Private i as Integer
    Private [|l|] As Integer

    Public Sub New(i As Integer)
        Me.i = i
    End Sub

    Public Sub {|BC30269:New|}(i As Integer, j As Integer, l As Integer)
        Me.l = l
    End Sub

    Public Sub New(i As Integer, j As Integer, k As Integer)
    End Sub
End Class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = fixedSource
            test.CodeActionIndex = 1
            test.CodeActionVerifier = Sub(codeAction As CodeAction, verifier As IVerifier)
                                          verifier.Equal(String.Format(CodeFixesResources.Add_to_0, "Program(Integer, Integer)"), codeAction.Title)
                                      End Sub
            Await test.RunAsync()

        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33603")>
        Public Async Function TestMultipleConstructors_ThirdOfThree() As Task
            Dim source =
"Class Program
    Private i as Integer
    Private [|l|] As Integer

    Public Sub New(i As Integer)
        Me.i = i
    End Sub

    Public Sub New(i As Integer, j As Integer)
    End Sub

    Public Sub New(i As Integer, j As Integer, k As Integer)
    End Sub
End Class"
            Dim fixedSource =
"Class Program
    Private i as Integer
    Private [|l|] As Integer

    Public Sub New(i As Integer)
        Me.i = i
    End Sub

    Public Sub New(i As Integer, j As Integer)
    End Sub

    Public Sub New(i As Integer, j As Integer, k As Integer, l As Integer)
        Me.l = l
    End Sub
End Class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = fixedSource
            test.CodeActionIndex = 2
            test.CodeActionVerifier = Sub(codeAction As CodeAction, verifier As IVerifier)
                                          verifier.Equal(String.Format(CodeFixesResources.Add_to_0, "Program(Integer, Integer, Integer)"), codeAction.Title)
                                      End Sub
            Await test.RunAsync()

        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33603")>
        Public Async Function TestMultipleConstructors_OneMustBeOptional() As Task
            Dim source =
"Class Program
    Private i as Integer
    Private [|l|] As Integer

    ' index 0 as required
    ' index 2 as optional
    Public Sub New(i As Integer)
        Me.i = i
    End Sub

    ' index 3 as optional
    Public Sub New(Optional j As Double = Nothing)
    End Sub

    ' index 1 as required
    ' index 4 as optional
    Public Sub New(i As Integer, j As Double)
    End Sub
End Class"
            Dim fixedSource =
"Class Program
    Private i as Integer
    Private [|l|] As Integer

    ' index 0 as required
    ' index 2 as optional
    Public Sub New(i As Integer)
        Me.i = i
    End Sub

    ' index 3 as optional
    Public Sub New(Optional j As Double = Nothing)
    End Sub

    ' index 1 as required
    ' index 4 as optional
    Public Sub New(i As Integer, j As Double, l As Integer)
        Me.l = l
    End Sub
End Class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = fixedSource
            test.CodeActionIndex = 1
            test.CodeActionVerifier = Sub(codeAction As CodeAction, verifier As IVerifier)
                                          verifier.Equal(String.Format(CodeFixesResources.Add_to_0, "Program(Integer, Double)"), codeAction.Title)
                                      End Sub
            Await test.RunAsync()

        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33603")>
        Public Async Function TestMultipleConstructors_OneMustBeOptional2() As Task
            Dim source =
"Class Program
    Private i as Integer
    Private [|l|] As Integer

    ' index 0, and 2 as optional
    Public Sub New(i As Integer)
        Me.i = i
    End Sub

    ' index 3 as optional
    Public Sub New(Optional j As Double = Nothing)
    End Sub

    ' index 1, and 4 as optional
    Public Sub New(i As Integer, j As Double)
    End Sub
End Class"
            Dim fixedSource =
"Class Program
    Private i as Integer
    Private [|l|] As Integer

    ' index 0, and 2 as optional
    Public Sub New(i As Integer)
        Me.i = i
    End Sub

    ' index 3 as optional
    Public Sub New(Optional j As Double = Nothing, Optional l As Integer = Nothing)
        Me.l = l
    End Sub

    ' index 1, and 4 as optional
    Public Sub New(i As Integer, j As Double)
    End Sub
End Class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = fixedSource
            test.CodeActionIndex = 3
            test.CodeActionVerifier = Sub(codeAction As CodeAction, verifier As IVerifier)
                                          verifier.Equal(String.Format(CodeFixesResources.Add_to_0, "Program(Double)"), codeAction.Title)
                                      End Sub
            Await test.RunAsync()

        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33603")>
        Public Async Function TestMultipleConstructors_AllMustBeOptional1() As Task
            Dim source =
"Class Program
    Private i as Integer
    Private [|p|] As Integer

    Public Sub New(Optional i As Integer = Nothing)
        Me.i = i
    End Sub

    Public Sub New(j As Double, Optional k As Double = Nothing)
    End Sub

    Public Sub New(l As Integer, m As Integer, Optional n As Integer = Nothing)
    End Sub
End Class"
            Dim fixedSource =
"Class Program
    Private i as Integer
    Private p As Integer

    Public Sub New(Optional i As Integer = Nothing, Optional p As Integer = Nothing)
        Me.i = i
        Me.p = p
    End Sub

    Public Sub New(j As Double, Optional k As Double = Nothing)
    End Sub

    Public Sub New(l As Integer, m As Integer, Optional n As Integer = Nothing)
    End Sub
End Class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = fixedSource
            test.CodeActionVerifier = Sub(codeAction As CodeAction, verifier As IVerifier)
                                          verifier.Equal(String.Format(CodeFixesResources.Add_to_0, "Program(Integer)"), codeAction.Title)
                                      End Sub
            Await test.RunAsync()

        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33603")>
        Public Async Function TestMultipleConstructors_AllMustBeOptional2() As Task
            Dim source =
"Class Program
    Private i as Integer
    Private [|p|] As Integer

    Public Sub New(Optional i As Integer = Nothing)
        Me.i = i
    End Sub

    Public Sub New(j As Double, Optional k As Double = Nothing)
    End Sub

    Public Sub New(l As Integer, m As Integer, Optional n As Integer = Nothing)
    End Sub
End Class"
            Dim fixedSource =
"Class Program
    Private i as Integer
    Private p As Integer

    Public Sub New(Optional i As Integer = Nothing)
        Me.i = i
    End Sub

    Public Sub New(j As Double, Optional k As Double = Nothing)
    End Sub

    Public Sub New(l As Integer, m As Integer, Optional n As Integer = Nothing, Optional p As Integer = Nothing)
        Me.p = p
    End Sub
End Class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = fixedSource
            test.CodeActionIndex = 2
            test.CodeActionVerifier = Sub(codeAction As CodeAction, verifier As IVerifier)
                                          verifier.Equal(String.Format(CodeFixesResources.Add_to_0, "Program(Integer, Integer, Integer)"), codeAction.Title)
                                      End Sub
            Await test.RunAsync()

        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")>
        Public Async Function TestNonSelection1() As Task
            Dim source =
"imports System.Collections.Generic

class Program
    dim i As Integer
  [||]  dim s As String

    public sub new(i As Integer)
        Me.i = i
    end sub
end class"
            Dim fixedSource =
"imports System.Collections.Generic

class Program
    dim i As Integer
    dim s As String

    public sub new(i As Integer, s As String)
        Me.i = i
        Me.s = s
    end sub
end class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = fixedSource
            test.CodeActionVerifier = Sub(codeAction As CodeAction, verifier As IVerifier)
                                          verifier.Equal(String.Format(FeaturesResources.Add_parameters_to_0, "Program(Integer)"), codeAction.Title)
                                      End Sub
            Await test.RunAsync()

        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")>
        Public Async Function TestNonSelection2() As Task
            Dim source =
"imports System.Collections.Generic

class Program
    dim i As Integer
    [||]dim s As String

    public sub new(i As Integer)
        Me.i = i
    end sub
end class"
            Dim fixedSource =
"imports System.Collections.Generic

class Program
    dim i As Integer
    dim s As String

    public sub new(i As Integer, s As String)
        Me.i = i
        Me.s = s
    end sub
end class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = fixedSource
            test.CodeActionVerifier = Sub(codeAction As CodeAction, verifier As IVerifier)
                                          verifier.Equal(String.Format(FeaturesResources.Add_parameters_to_0, "Program(Integer)"), codeAction.Title)
                                      End Sub
            Await test.RunAsync()

        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")>
        Public Async Function TestNonSelection3() As Task
            Dim source =
"imports System.Collections.Generic

class Program
    dim i As Integer
    dim [||]s As String

    public sub new(i As Integer)
        Me.i = i
    end sub
end class"
            Dim fixedSource =
"imports System.Collections.Generic

class Program
    dim i As Integer
    dim s As String

    public sub new(i As Integer, s As String)
        Me.i = i
        Me.s = s
    end sub
end class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = fixedSource
            test.CodeActionVerifier = Sub(codeAction As CodeAction, verifier As IVerifier)
                                          verifier.Equal(String.Format(FeaturesResources.Add_parameters_to_0, "Program(Integer)"), codeAction.Title)
                                      End Sub
            Await test.RunAsync()

        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")>
        Public Async Function TestNonSelection4() As Task
            Dim source =
"imports System.Collections.Generic

class Program
    dim i As Integer
    dim s[||] As String

    public sub new(i As Integer)
        Me.i = i
    end sub
end class"
            Dim fixedSource =
"imports System.Collections.Generic

class Program
    dim i As Integer
    dim s As String

    public sub new(i As Integer, s As String)
        Me.i = i
        Me.s = s
    end sub
end class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = fixedSource
            test.CodeActionVerifier = Sub(codeAction As CodeAction, verifier As IVerifier)
                                          verifier.Equal(String.Format(FeaturesResources.Add_parameters_to_0, "Program(Integer)"), codeAction.Title)
                                      End Sub
            Await test.RunAsync()

        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")>
        Public Async Function TestNonSelection5() As Task
            Dim source =
"imports System.Collections.Generic

class Program
    dim i As Integer
    dim s As String [||]

    public sub new(i As Integer)
        Me.i = i
    end sub
end class"
            Dim fixedSource =
"imports System.Collections.Generic

class Program
    dim i As Integer
    dim s As String

    public sub new(i As Integer, s As String)
        Me.i = i
        Me.s = s
    end sub
end class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = fixedSource
            test.CodeActionVerifier = Sub(codeAction As CodeAction, verifier As IVerifier)
                                          verifier.Equal(String.Format(FeaturesResources.Add_parameters_to_0, "Program(Integer)"), codeAction.Title)
                                      End Sub
            Await test.RunAsync()

        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")>
        Public Async Function TestNonSelectionMultiVar1() As Task
            Dim source =
"imports System.Collection.Generic

class Program
    dim i As Integer
    [||]dim s, t As String

    public sub new(i As Integer)
        Me.i = i
    end sub
end class"
            Dim fixedSource =
"imports System.Collection.Generic

class Program
    dim i As Integer
    dim s, t As String

    public sub new(i As Integer, s As String, t As String)
        Me.i = i
        Me.s = s
        Me.t = t
    end sub
end class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = fixedSource
            test.CodeActionVerifier = Sub(codeAction As CodeAction, verifier As IVerifier)
                                          verifier.Equal(String.Format(FeaturesResources.Add_parameters_to_0, "Program(Integer)"), codeAction.Title)
                                      End Sub
            Await test.RunAsync()

        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")>
        Public Async Function TestNonSelectionMultiVar2() As Task
            Dim source =
"imports System.Collection.Generic

class Program
    dim i As Integer
    dim s, t As String[||]

    public sub new(i As Integer)
        Me.i = i
    end sub
end class"
            Dim fixedSource =
"imports System.Collection.Generic

class Program
    dim i As Integer
    dim s, t As String

    public sub new(i As Integer, s As String, t As String)
        Me.i = i
        Me.s = s
        Me.t = t
    end sub
end class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = fixedSource
            test.CodeActionVerifier = Sub(codeAction As CodeAction, verifier As IVerifier)
                                          verifier.Equal(String.Format(FeaturesResources.Add_parameters_to_0, "Program(Integer)"), codeAction.Title)
                                      End Sub
            Await test.RunAsync()

        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")>
        Public Async Function TestNonSelectionMultiVar3() As Task
            Dim source =
"imports System.Collection.Generic

class Program
    dim i As Integer
    dim [||]s, t As String

    public sub new(i As Integer)
        Me.i = i
    end sub
end class"
            Dim fixedSource =
"imports System.Collection.Generic

class Program
    dim i As Integer
    dim s, t As String

    public sub new(i As Integer, s As String)
        Me.i = i
        Me.s = s
    end sub
end class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = fixedSource
            test.CodeActionVerifier = Sub(codeAction As CodeAction, verifier As IVerifier)
                                          verifier.Equal(String.Format(FeaturesResources.Add_parameters_to_0, "Program(Integer)"), codeAction.Title)
                                      End Sub
            Await test.RunAsync()

        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")>
        Public Async Function TestNonSelectionMultiVar4() As Task
            Dim source =
"imports System.Collection.Generic

class Program
    dim i As Integer
    dim s[||], t As String

    public sub new(i As Integer)
        Me.i = i
    end sub
end class"
            Dim fixedSource =
"imports System.Collection.Generic

class Program
    dim i As Integer
    dim s, t As String

    public sub new(i As Integer, s As String)
        Me.i = i
        Me.s = s
    end sub
end class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = fixedSource
            test.CodeActionVerifier = Sub(codeAction As CodeAction, verifier As IVerifier)
                                          verifier.Equal(String.Format(FeaturesResources.Add_parameters_to_0, "Program(Integer)"), codeAction.Title)
                                      End Sub
            Await test.RunAsync()

        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")>
        Public Async Function TestNonSelectionMultiVar5() As Task
            Dim source =
"imports System.Collection.Generic

class Program
    dim i As Integer
    dim s, [||]t As String

    public sub new(i As Integer)
        Me.i = i
    end sub
end class"
            Dim fixedSource =
"imports System.Collection.Generic

class Program
    dim i As Integer
    dim s, t As String

    public sub new(i As Integer, t As String)
        Me.i = i
        Me.t = t
    end sub
end class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = fixedSource
            test.CodeActionVerifier = Sub(codeAction As CodeAction, verifier As IVerifier)
                                          verifier.Equal(String.Format(FeaturesResources.Add_parameters_to_0, "Program(Integer)"), codeAction.Title)
                                      End Sub
            Await test.RunAsync()

        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")>
        Public Async Function TestNonSelectionMultiVar6() As Task
            Dim source =
"imports System.Collection.Generic

class Program
    dim i As Integer
    dim s, t[||] As String

    public sub new(i As Integer)
        Me.i = i
    end sub
end class"
            Dim fixedSource =
"imports System.Collection.Generic

class Program
    dim i As Integer
    dim s, t As String

    public sub new(i As Integer, t As String)
        Me.i = i
        Me.t = t
    end sub
end class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = fixedSource
            test.CodeActionVerifier = Sub(codeAction As CodeAction, verifier As IVerifier)
                                          verifier.Equal(String.Format(FeaturesResources.Add_parameters_to_0, "Program(Integer)"), codeAction.Title)
                                      End Sub
            Await test.RunAsync()

        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")>
        Public Async Function TestNonSelectionMissing1() As Task
            Dim source =
"imports System.Collection.Generic

class Program
    dim i As Integer
    [||]
    dim s, t As String

    public sub new(i As Integer)
        Me.i = i
    end sub
end class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = source
            Await test.RunAsync()

        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")>
        Public Async Function TestNonSelectionMissing2() As Task
            Dim source =
"imports System.Collection.Generic

class Program
    dim i As Integer
    d[||]im s, t As String

    public sub new(i As Integer)
        Me.i = i
    end sub
end class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = source
            Await test.RunAsync()

        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")>
        Public Async Function TestNonSelectionMissing3() As Task
            Dim source =
"imports System.Collection.Generic

class Program
    dim i As Integer
    dim[||] s, t As String

    public sub new(i As Integer)
        Me.i = i
    end sub
end class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = source
            Await test.RunAsync()

        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23271")>
        Public Async Function TestNonSelectionMissing4() As Task
            Dim source =
"imports System.Collection.Generic

class Program
    dim i As Integer
    dim s,[||] t As String

    public sub new(i As Integer)
        Me.i = i
    end sub
end class"
            Dim test As New VerifyVB.Test()
            test.TestCode = source
            test.FixedCode = source
            Await test.RunAsync()

        End Function
    End Class
End Namespace
