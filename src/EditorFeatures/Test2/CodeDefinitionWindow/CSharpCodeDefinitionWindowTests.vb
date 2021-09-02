' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

Namespace Microsoft.CodeAnalysis.Editor.CodeDefinitionWindow.UnitTests

    <UseExportProvider>
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

        <Fact, Trait(Traits.Feature, Traits.Features.CodeDefinitionWindow)>
        Public Async Function ReducedGenericExtensionMethod() As Task
            Const code As String = "
using System.Collections.Generic;
static class Ex
{
    public static void [|M|]<T>(this List<T> list) { }
}

class C
{
    void M()
    {
        var list = new List<int>();
        list.$$M();
    }
}"

            Await VerifyContextLocationInSameFile(code, "Ex.M<T>(System.Collections.Generic.List<T>)")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeDefinitionWindow)>
        Public Async Function ToMetadataAsSource() As Task
            Const code As String = "
class C
{
    void M($$int i) { }
}"

            Await VerifyContextLocationInMetadataAsSource(code, "int", "Int32.cs")

        End Function

        Protected Overrides Function CreateWorkspace(code As String, Optional testComposition As TestComposition = Nothing) As TestWorkspace
            Return TestWorkspace.CreateCSharp(code, composition:=testComposition)
        End Function
    End Class
End Namespace
