' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Editor.UnitTests.RefactoringHelpers
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.RefactoringHelpers
    Partial Public Class RefactoringHelpersTests
        Inherits RefactoringHelpersTestBase(Of VisualBasicTestWorkspaceFixture)

        Public Sub New(ByVal workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        <Fact>
        <WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")>
        Public Async Function TestArgumentsExtractionsSelectModifiedIdentifier() As Task
            Dim testText = "
Imports System

class C
    public sub new({|result:[|s|] as string|})
    end sub
end class"
            Await TestAsync(Of ParameterSyntax)(testText)
        End Function

        <Fact>
        <WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")>
        Public Async Function TestArgumentsExtractionsInHeader() As Task
            Dim testText = "
Imports System

class CC
    public sub new({|result:s as C[||]C|})
    end sub
end class"
            Await TestAsync(Of ParameterSyntax)(testText)
        End Function

        <Fact>
        <WorkItem(35525, "https://github.com/dotnet/roslyn/issues/35525")>
        Public Async Function TestMissingArgumentsExtractionsSelectPartOfHeader() As Task
            Dim testText = "
Imports System

class CC
    public sub new(s as [|CC|])
    end sub
end class"
            Await TestMissingAsync(Of ParameterSyntax)(testText)
        End Function

    End Class
End Namespace
