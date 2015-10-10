' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Simplification
    Public Class ParameterSimplificationTests
        Inherits AbstractSimplificationTests

#Region "CSharp tests"
        Private Sub TestDocumentSimplification(input As String, expected As String)
            Using workspace = New AdhocWorkspace()
                Dim solution = workspace.CurrentSolution
                Dim projId = ProjectId.CreateNewId()
                Dim project = solution.AddProject(projId, "Project", "Project.dll", LanguageNames.CSharp) _
                    .GetProject(projId)

                Dim document = project.AddMetadataReference(TestReferences.NetFx.v4_0_30319.mscorlib) _
                    .AddDocument("Document", SourceText.From(input))

                Dim annotatedDocument = document.WithSyntaxRoot(
                    document.GetSyntaxRootAsync().Result.WithAdditionalAnnotations(Simplifier.Annotation))

                Dim simplifiedDocument = Simplifier.ReduceAsync(annotatedDocument).Result

                Assert.Equal(expected, simplifiedDocument.GetTextAsync().Result.ToString())
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_ParameterCanBeSimplified()
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
            TestDocumentSimplification(code.Value, expected.Value)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Sub CSharp_ParameterCannotBeSimplified()
            Dim code = <![CDATA[
using System;

class C
{
    static void Main(string[] args)
    {
        Action<int> a = j => { };
    }
}]]>
            TestDocumentSimplification(code.Value, code.Value)
        End Sub
#End Region
    End Class
End Namespace
