' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

Namespace Microsoft.CodeAnalysis.Editor.CodeDefinitionWindow.UnitTests

    <UseExportProvider>
    <Trait(Traits.Feature, Traits.Features.CodeDefinitionWindow)>
    Public Class CSharpCodeDefinitionWindowTests
        Inherits AbstractCodeDefinitionWindowTests

        <Fact>
        Public Async Function ClassFromDefinition() As Task
            Const code As String = "
class $$[|C|]
{
}"

            Await VerifyContextLocationAsync(code, "class C")
        End Function

        <Fact>
        Public Async Function ClassFromReference() As Task
            Const code As String = "
class [|C|]
{
    static void M()
    {
        $$C.M();
    }
}"

            Await VerifyContextLocationAsync(code, "class C")
        End Function

        <Fact>
        Public Async Function MethodFromDefinition() As Task
            Const code As String = "
class C
{
    void $$[|M|]()
    {
    }
}"

            Await VerifyContextLocationAsync(code, "void C.M()")
        End Function

        <Fact>
        Public Async Function MethodFromReference() As Task
            Const code As String = "
class C
{
    void [|M|]()
    {
        this.$$M();
    }
}"

            Await VerifyContextLocationAsync(code, "void C.M()")
        End Function

        <Fact>
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

            Await VerifyContextLocationAsync(code, "static void Ex.M<T>(List<T>)")
        End Function

        <Fact>
        Public Async Function ToMetadataAsSource() As Task
            Const code As String = "
class C
{
    void M($$int i) { }
}"

            Await VerifyContextLocationInMetadataAsSource(code, "int", "Int32.cs")

        End Function

        <Theory, WorkItem("https://github.com/dotnet/roslyn/issues/71680")>
        <InlineData("ValueTuple<int> valueTuple1;", "System.ValueTuple<int>")>
        <InlineData("ValueTuple<int, int> valueTuple2;", "(int, int)")>
        <InlineData("ValueTuple<int, int, int> valueTuple3;", "(int, int, int)")>
        <InlineData("ValueTuple<int, int, int, int> valueTuple4;", "(int, int, int, int)")>
        <InlineData("ValueTuple<int, int, int, int, int> valueTuple5;", "(int, int, int, int, int)")>
        <InlineData("ValueTuple<int, int, int, int, int, int> valueTuple6;", "(int, int, int, int, int, int)")>
        <InlineData("ValueTuple<int, int, int, int, int, int, int> valueTuple7;", "(int, int, int, int, int, int, int)")>
        <InlineData("ValueTuple<int, int, int, int, int, int, int, int> valueTuple8;", "System.ValueTuple<int, int, int, int, int, int, int, int>")>
        Public Async Function ToValueTupleMetadataAsSource(expression As String, expectedDisplayName As String) As Task
            Dim code As String = $"using System;
class C
{{
    void M()
    {{
        $${expression} valueTuple;
    }}
}}"

            Await VerifyContextLocationInMetadataAsSource(code, expectedDisplayName, "ValueTuple.cs")

        End Function

        Protected Overrides Function CreateWorkspace(code As String, testComposition As TestComposition) As EditorTestWorkspace
            Return EditorTestWorkspace.CreateCSharp(code, composition:=testComposition)
        End Function
    End Class
End Namespace
