' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.ArgumentProviders
    <Trait(Traits.Feature, Traits.Features.Completion)>
    Public Class ContextVariableArgumentProviderTests
        Inherits AbstractVisualBasicArgumentProviderTests

        Friend Overrides Function GetArgumentProviderType() As Type
            Return GetType(ContextVariableArgumentProvider)
        End Function

        <Theory>
        <InlineData("String")>
        <InlineData("Boolean")>
        <InlineData("Integer?")>
        Public Async Function TestLocalVariable(type As String) As Task
            Dim markup = $"
Class C
    Sub Method()
        Dim arg As {type} = Nothing
        Me.Target($$)
    End Sub

    Sub Target(arg As {type})
    End Sub
End Class
"

            Await VerifyDefaultValueAsync(markup, "arg")
            Await VerifyDefaultValueAsync(markup, expectedDefaultValue:=Nothing, previousDefaultValue:="prior")
        End Function

        <Theory>
        <InlineData("String")>
        <InlineData("Boolean")>
        <InlineData("Integer?")>
        Public Async Function TestParameter(type As String) As Task
            Dim markup = $"
Class C
    Sub Method(arg As {type})
        Me.Target($$)
    End Sub

    Sub Target(arg As {type})
    End Sub
End Class
"

            Await VerifyDefaultValueAsync(markup, "arg")
            Await VerifyDefaultValueAsync(markup, expectedDefaultValue:=Nothing, previousDefaultValue:="prior")
        End Function

        <Theory>
        <InlineData("String", "String")>
        <InlineData("String", "IEnumerable(Of Char)")>
        <InlineData("Boolean", "Boolean")>
        <InlineData("Integer?", "Integer?")>
        <InlineData("Integer", "Integer?")>
        Public Async Function TestInstanceVariable(fieldType As String, parameterType As String) As Task
            Dim markup = $"
Imports System.Collections.Generic

Class C
    Dim arg As {fieldType}

    Sub Method()
        Me.Target($$)
    End Sub

    Sub Target(arg As {parameterType})
    End Sub
End Class
"

            Await VerifyDefaultValueAsync(markup, "arg")
            Await VerifyDefaultValueAsync(markup, expectedDefaultValue:=Nothing, previousDefaultValue:="prior")
        End Function

        <Theory>
        <InlineData("C")>
        <InlineData("B")>
        <InlineData("I")>
        Public Async Function TestMeInstance(parameterType As String) As Task
            Dim markup = $"
Imports System.Collections.Generic

Interface I : End Interface
Class B : End Class

Class C
    Inherits B
    Implements I

    Sub Method()
        Me.Target($$)
    End Sub

    Sub Target(arg As {parameterType})
    End Sub
End Class
"

            Await VerifyDefaultValueAsync(markup, "Me")
            Await VerifyDefaultValueAsync(markup, expectedDefaultValue:=Nothing, previousDefaultValue:="prior")
        End Function

        <Theory>
        <InlineData("Object")>
        <InlineData("String")>
        Public Async Function TestMeInstanceNotProvided1(parameterType As String) As Task
            Dim markup = $"
Imports System.Collections.Generic

Interface I : End Interface
Class B : End Class

Class C
    Inherits B
    Implements I

    Sub Method()
        Me.Target($$)
    End Sub

    Sub Target(arg As {parameterType})
    End Sub
End Class
"

            Await VerifyDefaultValueAsync(markup, Nothing)
        End Function

        <Fact>
        Public Async Function TestMeInstanceNotProvided2() As Task
            Dim markup = $"
Imports System.Collections.Generic

Class C

    Shared Sub Method()
        Target($$)
    End Sub

    Shared Sub Target(arg As C)
    End Sub
End Class
"

            Await VerifyDefaultValueAsync(markup, Nothing)
        End Function

        ' Note: The current implementation checks for exact type and name match. If this changes, some of these tests
        ' may need to be updated to account for the new behavior.
        <Theory>
        <InlineData("Object", "String")>
        <InlineData("String", "Object")>
        <InlineData("Boolean", "Boolean?")>
        <InlineData("Boolean", "Integer")>
        <InlineData("Integer", "Object")>
        Public Async Function TestMismatchType(parameterType As String, valueType As String) As Task
            Dim markup = $"
Class C
    Sub Method(arg As {valueType})
        Me.Target($$)
    End Sub

    Sub Target(arg As {parameterType})
    End Sub
End Class
"

            Await VerifyDefaultValueAsync(markup, Nothing)
        End Function
    End Class
End Namespace
