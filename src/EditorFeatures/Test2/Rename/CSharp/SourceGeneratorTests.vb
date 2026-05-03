' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Rename.ConflictEngine
Imports Microsoft.CodeAnalysis.Testing
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.CSharp
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.Rename)>
    Public Class SourceGeneratorTests
        Private ReadOnly _outputHelper As Abstractions.ITestOutputHelper

        Public Sub New(outputHelper As Abstractions.ITestOutputHelper)
            _outputHelper = outputHelper
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameColorColorCaseWithGeneratedClassName(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" AssemblyName="ClassLibrary1" CommonReferences="true">
                            <Document>
public class RegularClass
{
    public GeneratedClass [|$$GeneratedClass|];
}
                            </Document>
                            <DocumentFromSourceGenerator>
public class GeneratedClass
{
}
                            </DocumentFromSourceGenerator>
                        </Project>
                    </Workspace>, host:=host, renameTo:="A")

            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Sub RenameWithReferenceInGeneratedFile(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" AssemblyName="ClassLibrary1" CommonReferences="true">
                            <Document>
public class [|$$RegularClass|]
{
}
                            </Document>
                            <DocumentFromSourceGenerator>
public class GeneratedClass
{
    public void M(RegularClass c) { }
}
                            </DocumentFromSourceGenerator>
                        </Project>
                    </Workspace>, host:=host, renameTo:="A")

            End Using
        End Sub

        <Theory, CombinatorialData>
        Public Async Function RenameWithReferenceInRazorGeneratedFile(host As RenameTestHost) As Task
            Dim generatedMarkup = "
public class GeneratedClass
{
    public void M([|RegularClass|] c) { }
}
"
            Dim generatedCode As String = ""
            Dim span As TextSpan
            TestFileMarkupParser.GetSpan(generatedMarkup, generatedCode, span)
            Dim sourceGenerator = New Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator(Sub(c) c.AddSource("generated_file.cs", generatedCode))
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" AssemblyName="ClassLibrary1" CommonReferences="true">
                            <Document>
public class [|$$RegularClass|]
{
}
                            </Document>
                        </Project>
                    </Workspace>, host:=host, sourceGenerator:=sourceGenerator, renameTo:="A")

                Dim project = result.ConflictResolution.OldSolution.Projects.Single()
                Dim generatedDocuments = Await project.GetSourceGeneratedDocumentsAsync(CancellationToken.None)
                Dim generatedTree = Await generatedDocuments.Single().GetSyntaxTreeAsync(CancellationToken.None)
                Dim location = CodeAnalysis.Location.Create(generatedTree, span)
                'Manually assert the generated location, because the test workspace doesn't know about it
                result.AssertLocationReferencedAs(location, RelatedLocationType.NoConflict)

            End Using
        End Function

        <Theory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/51537")>
        Public Sub RenameWithCascadeIntoGeneratedFile(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                    <Workspace>
                        <Project Language="C#" AssemblyName="ClassLibrary1" CommonReferences="true">
                            <Document>
public interface IInterface
{
    int [|$$Property|] { get; set; }
}

public partial class GeneratedClass : IInterface { }
                            </Document>
                        </Project>
                    </Workspace>, host:=host, renameTo:="A", sourceGenerator:=New GeneratorThatImplementsInterfaceMethod())
            End Using
        End Sub

#Disable Warning RS1042
        Private Class GeneratorThatImplementsInterfaceMethod
            Implements ISourceGenerator
#Enable Warning RS1042

#Disable Warning BC40000
            Public Sub Initialize(context As GeneratorInitializationContext) Implements ISourceGenerator.Initialize
            End Sub

            Public Sub Execute(context As GeneratorExecutionContext) Implements ISourceGenerator.Execute
#Enable Warning BC40000
                Dim [interface] = context.Compilation.GetTypeByMetadataName("IInterface")
                Dim memberName = [interface].MemberNames.Single()

                Dim text = "public partial class GeneratedClass { public int " + memberName + " { get; set; } }"
                context.AddSource("Implementation.cs", text)
            End Sub
        End Class
    End Class
End Namespace
