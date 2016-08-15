' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

Namespace Microsoft.CodeAnalysis.Editor.CodeDefinitionWindow.UnitTests

    Public Class CSharpCodeDefinitionWindowTests
        Inherits AbstractCodeDefinitionWindowTests

        <Fact, Trait(Traits.Feature, Traits.Features.CodeDefinitionWindow)>
        Public Async Function ClassFromDefinition() As Task
            Const code As String = "
class $$[|C|]
{
}"

            Await VerifyContextLocationInSameFile(code, "C")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeDefinitionWindow)>
        Public Async Function ClassFromReference() As Task
            Const code As String = "
class [|C|]
{
    static void M()
    {
        $$C.M();
    }
}"

            Await VerifyContextLocationInSameFile(code, "C")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeDefinitionWindow)>
        Public Async Function MethodFromDefinition() As Task
            Const code As String = "
class C
{
    void $$[|M|]()
    {
    }
}"

            Await VerifyContextLocationInSameFile(code, "C.M()")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeDefinitionWindow)>
        Public Async Function MethodFromReference() As Task
            Const code As String = "
class C
{
    void [|M|]()
    {
        this.$$M();
    }
}"

            Await VerifyContextLocationInSameFile(code, "C.M()")
        End Function

        Protected Overrides Function CreateWorkspaceAsync(code As String) As Task(Of TestWorkspace)
            Return TestWorkspace.CreateCSharpAsync(code)
        End Function
    End Class
End Namespace
