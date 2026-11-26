' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions

Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeRefactoringVerifier(Of
    Microsoft.CodeAnalysis.VisualBasic.GenerateToStringFromMembers.GenerateToStringFromMembersCodeRefactoringProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.GenerateToStringFromMembers
    <UseExportProvider>
    <Trait(Traits.Feature, Traits.Features.CodeActionsGenerateToString)>
    Public Class GenerateToStringFromMembersTests

        <Fact>
        Public Function TestClass_ToString() As Task
            Return New VerifyVB.Test With {
                .TestCode = "Class Program
    [|Dim a As Integer|]
End Class",
                .FixedCode = "Imports System.Text

Class Program
    Dim a As Integer

    Public Overrides Function ToString() As String
        Dim builder = New StringBuilder()
        builder.Append(""Program"")
        builder.Append("" { "")
        builder.Append(""a = "")
        builder.Append(a.ToString())
        builder.Append(""}""c)
        Return builder.ToString()
    End Function
End Class",
                .CodeActionEquivalenceKey = FeaturesResources.Generate_ToString
            }.RunAsync()
        End Function

        <Fact>
        Public Function TestClass_ToString_MultipleMembers() As Task
            Return New VerifyVB.Test With {
                .TestCode = "Class Program
    [|Dim a As Integer
    Dim b As String|]
End Class",
                .FixedCode = "Imports System.Text

Class Program
    Dim a As Integer
    Dim b As String

    Public Overrides Function ToString() As String
        Dim builder = New StringBuilder()
        builder.Append(""Program"")
        builder.Append("" { "")
        builder.Append(""a = "")
        builder.Append(a.ToString())
        builder.Append("", b = "")
        builder.Append(DirectCast(b, Object))
        builder.Append(""}""c)
        Return builder.ToString()
    End Function
End Class",
                .CodeActionEquivalenceKey = FeaturesResources.Generate_ToString
            }.RunAsync()
        End Function

        <Fact>
        Public Function TestStructure_ToString() As Task
            Return New VerifyVB.Test With {
                .TestCode = "Structure Program
    [|Dim a As Integer|]
End Structure",
                .FixedCode = "Imports System.Text

Structure Program
    Dim a As Integer

    Public Overrides Function ToString() As String
        Dim builder = New StringBuilder()
        builder.Append(""Program"")
        builder.Append("" { "")
        builder.Append(""a = "")
        builder.Append(a.ToString())
        builder.Append(""}""c)
        Return builder.ToString()
    End Function
End Structure",
                .CodeActionEquivalenceKey = FeaturesResources.Generate_ToString
            }.RunAsync()
        End Function

        <Fact>
        Public Function TestClass_ToString_NoSelection() As Task
            Return New VerifyVB.Test With {
                .TestCode = "Class [||]Program
    Public a As Integer
End Class",
                .FixedCode = "Imports System.Text

Class Program
    Public a As Integer

    Public Overrides Function ToString() As String
        Dim builder = New StringBuilder()
        builder.Append(""Program"")
        builder.Append("" { "")
        builder.Append(""a = "")
        builder.Append(a.ToString())
        builder.Append(""}""c)
        Return builder.ToString()
    End Function
End Class",
                .CodeActionEquivalenceKey = FeaturesResources.Generate_ToString
            }.RunAsync()
        End Function

        <Fact>
        Public Function TestClass_ToString_NoActionWhenToStringExists() As Task
            Return New VerifyVB.Test With {
                .TestCode = "Class Program
    [|Dim a As Integer|]

    Public Overrides Function ToString() As String
        Return ""Program""
    End Function
End Class"
            }.RunAsync()
        End Function

        <Fact>
        Public Function TestClass_NotOfferedBetweenMembers() As Task
            Return New VerifyVB.Test With {
                .TestCode = "Class Program
    Public a As Integer
    [||]
End Class"
            }.RunAsync()
        End Function
    End Class
End Namespace
