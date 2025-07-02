' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Test.Utilities.ChangeSignature
Imports Microsoft.VisualStudio.Text.Operations

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ChangeSignature
    <Trait(Traits.Feature, Traits.Features.ChangeSignature)>
    Partial Public Class ChangeSignatureTests
        Inherits AbstractChangeSignatureTests

        Private Shared ReadOnly s_composition As TestComposition = EditorTestCompositions.EditorFeatures.AddParts(GetType(TestChangeSignatureOptionsService))

        <WpfFact>
        Public Async Function TestReorderParameters_AcrossLanguages_InvokeFromDeclaration() As Task
            Dim workspaceXml = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                                       <Document FilePath="VBDocument">
Public Class Test
    Public Sub $$Goo(x as Integer, y as Integer)
    End Sub
End Class</Document>
                                   </Project>
                                   <Project Language="C#" AssemblyName="CSAssembly" CommonReferences="true">
                                       <ProjectReference>VBAssembly</ProjectReference>
                                       <Document FilePath="CSharpDocument">
class C
{
    void M()
    {
        new Test().Goo(1, 2);
    }
}</Document>
                                   </Project>
                               </Workspace>

            Dim permutation = {New AddedParameterOrExistingIndex(1), New AddedParameterOrExistingIndex(0)}

            Dim expectedVBCode = <Text><![CDATA[
Public Class Test
    Public Sub Goo(y as Integer, x as Integer)
    End Sub
End Class]]></Text>.NormalizedValue()

            Dim expectedCSharpCode = <Text><![CDATA[
class C
{
    void M()
    {
        new Test().Goo(2, 1);
    }
}]]></Text>.NormalizedValue()

            Dim workspace = EditorTestWorkspace.Create(workspaceXml, composition:=s_composition)
            Using testState = New ChangeSignatureTestState(workspace)
                Dim history = testState.Workspace.GetService(Of ITextUndoHistoryRegistry)().RegisterHistory(workspace.Documents.First().GetTextBuffer())
                testState.TestChangeSignatureOptionsService.UpdatedSignature = permutation
                Dim result = Await testState.ChangeSignatureAsync().ConfigureAwait(False)

                Dim vbdoc = result.UpdatedSolution.Projects.Single(Function(p) p.AssemblyName = "VBAssembly").Documents.Single()
                Dim csdoc = result.UpdatedSolution.Projects.Single(Function(p) p.AssemblyName = "CSAssembly").Documents.Single()

                Assert.Equal(expectedCSharpCode, (Await csdoc.GetTextAsync()).ToString())
                Assert.Equal(expectedVBCode, (Await vbdoc.GetTextAsync()).ToString())
            End Using
        End Function

        <WpfFact>
        Public Async Function TestReorderParameters_AcrossLanguages_InvokeFromReference() As Task
            Dim workspaceXml = <Workspace>
                                   <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                                       <Document FilePath="VBDocument">
Public Class Test
    Public Sub Goo(x as Integer, y as Integer)
    End Sub
End Class</Document>
                                   </Project>
                                   <Project Language="C#" AssemblyName="CSAssembly" CommonReferences="true">
                                       <ProjectReference>VBAssembly</ProjectReference>
                                       <Document FilePath="CSharpDocument">
class C
{
    void M()
    {
        new Test().Goo$$(1, 2);
    }
}</Document>
                                   </Project>
                               </Workspace>

            Dim permutation = {New AddedParameterOrExistingIndex(1), New AddedParameterOrExistingIndex(0)}

            Dim expectedVBCode = <Text><![CDATA[
Public Class Test
    Public Sub Goo(y as Integer, x as Integer)
    End Sub
End Class]]></Text>.NormalizedValue()

            Dim expectedCSharpCode = <Text><![CDATA[
class C
{
    void M()
    {
        new Test().Goo(2, 1);
    }
}]]></Text>.NormalizedValue()

            Dim workspace = EditorTestWorkspace.Create(workspaceXml, composition:=s_composition)
            Using testState = New ChangeSignatureTestState(workspace)
                Dim history = testState.Workspace.GetService(Of ITextUndoHistoryRegistry)().RegisterHistory(workspace.Documents.First().GetTextBuffer())
                testState.TestChangeSignatureOptionsService.UpdatedSignature = permutation
                Dim result = Await testState.ChangeSignatureAsync().ConfigureAwait(False)

                Dim vbdoc = result.UpdatedSolution.Projects.Single(Function(p) p.AssemblyName = "VBAssembly").Documents.Single()
                Dim csdoc = result.UpdatedSolution.Projects.Single(Function(p) p.AssemblyName = "CSAssembly").Documents.Single()

                Assert.Equal(expectedCSharpCode, (Await csdoc.GetTextAsync()).ToString())
                Assert.Equal(expectedVBCode, (Await vbdoc.GetTextAsync()).ToString())
            End Using
        End Function
    End Class
End Namespace
