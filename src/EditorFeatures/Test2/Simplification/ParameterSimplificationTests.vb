' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Simplification
    Public Class ParameterSimplificationTests
        Inherits AbstractSimplificationTests

#Region "CSharp tests"
        Private Async Function TestDocumentSimplificationAsync(input As String, expected As String) As System.Threading.Tasks.Task
            Using workspace = New AdhocWorkspace()
                Dim solution = workspace.CurrentSolution
                Dim projId = ProjectId.CreateNewId()
                Dim project = solution.AddProject(projId, "Project", "Project.dll", LanguageNames.CSharp) _
                    .GetProject(projId)

                Dim document = project.AddMetadataReference(TestReferences.NetFx.v4_0_30319.mscorlib) _
                    .AddDocument("Document", SourceText.From(input))

                Dim annotatedDocument = document.WithSyntaxRoot(
                    (Await document.GetSyntaxRootAsync()).WithAdditionalAnnotations(Simplifier.Annotation))

                Dim simplifiedDocument = Await Simplifier.ReduceAsync(annotatedDocument)

                Assert.Equal(expected, (Await simplifiedDocument.GetTextAsync()).ToString())
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function CSharp_ParameterCanBeSimplified() As System.Threading.Tasks.Task
            Dim code = <![CDATA[
using System;

class C
{
    static void Main(string[] args)
    {
        Action<int> a = (int j) => { };
    }
}]]>
            Dim expected =
            <![CDATA[
using System;

class C
{
    static void Main(string[] args)
    {
        Action<int> a = (j) => { };
    }
}]]>
            Await TestDocumentSimplificationAsync(code.Value, expected.Value)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function CSharp_ParameterCannotBeSimplified() As System.Threading.Tasks.Task
            Dim code = <![CDATA[
using System;

class C
{
    static void Main(string[] args)
    {
        Action<int> a = j => { };
    }
}]]>
            Await TestDocumentSimplificationAsync(code.Value, code.Value)
        End Function
#End Region
    End Class
End Namespace