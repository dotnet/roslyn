' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.VisualStudio.Text.Operations

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ChangeSignature
    Partial Public Class ChangeSignatureTests
        Inherits AbstractChangeSignatureTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderParameters_AcrossLanguages_InvokeFromDeclaration() As Task
            Dim workspace = <Workspace>
                                <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                                    <Document FilePath="VBDocument">
Public Class Test
    Public Sub $$Foo(x as Integer, y as Integer)
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
        new Test().Foo(1, 2);
    }
}</Document>
                                </Project>
                            </Workspace>

            Dim permutation = {1, 0}

            Dim expectedVBCode = <Text><![CDATA[
Public Class Test
    Public Sub Foo(y as Integer, x as Integer)
    End Sub
End Class]]></Text>.NormalizedValue()

            Dim expectedCSharpCode = <Text><![CDATA[
class C
{
    void M()
    {
        new Test().Foo(2, 1);
    }
}]]></Text>.NormalizedValue()

            Using testState = Await ChangeSignatureTestState.CreateAsync(workspace)
                Dim history = testState.Workspace.GetService(Of ITextUndoHistoryRegistry)().RegisterHistory(testState.Workspace.Documents.First().GetTextBuffer())
                testState.TestChangeSignatureOptionsService.IsCancelled = False
                testState.TestChangeSignatureOptionsService.UpdatedSignature = permutation
                Dim result = testState.ChangeSignature()

                Dim vbdoc = result.UpdatedSolution.Projects.Single(Function(p) p.AssemblyName = "VBAssembly").Documents.Single()
                Dim csdoc = result.UpdatedSolution.Projects.Single(Function(p) p.AssemblyName = "CSAssembly").Documents.Single()

                Assert.Equal(expectedCSharpCode, (Await csdoc.GetTextAsync()).ToString())
                Assert.Equal(expectedVBCode, (Await vbdoc.GetTextAsync()).ToString())
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestReorderParameters_AcrossLanguages_InvokeFromReference() As Task
            Dim workspace = <Workspace>
                                <Project Language="Visual Basic" AssemblyName="VBAssembly" CommonReferences="true">
                                    <Document FilePath="VBDocument">
Public Class Test
    Public Sub Foo(x as Integer, y as Integer)
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
        new Test().Foo$$(1, 2);
    }
}</Document>
                                </Project>
                            </Workspace>

            Dim permutation = {1, 0}

            Dim expectedVBCode = <Text><![CDATA[
Public Class Test
    Public Sub Foo(y as Integer, x as Integer)
    End Sub
End Class]]></Text>.NormalizedValue()

            Dim expectedCSharpCode = <Text><![CDATA[
class C
{
    void M()
    {
        new Test().Foo(2, 1);
    }
}]]></Text>.NormalizedValue()

            Using testState = Await ChangeSignatureTestState.CreateAsync(workspace)
                Dim history = testState.Workspace.GetService(Of ITextUndoHistoryRegistry)().RegisterHistory(testState.Workspace.Documents.First().GetTextBuffer())
                testState.TestChangeSignatureOptionsService.IsCancelled = False
                testState.TestChangeSignatureOptionsService.UpdatedSignature = permutation
                Dim result = testState.ChangeSignature()

                Dim vbdoc = result.UpdatedSolution.Projects.Single(Function(p) p.AssemblyName = "VBAssembly").Documents.Single()
                Dim csdoc = result.UpdatedSolution.Projects.Single(Function(p) p.AssemblyName = "CSAssembly").Documents.Single()

                Assert.Equal(expectedCSharpCode, (Await csdoc.GetTextAsync()).ToString())
                Assert.Equal(expectedVBCode, (Await vbdoc.GetTextAsync()).ToString())
            End Using
        End Function
    End Class
End Namespace