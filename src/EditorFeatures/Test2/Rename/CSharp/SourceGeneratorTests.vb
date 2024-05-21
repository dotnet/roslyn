' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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

        Private Class GeneratorThatImplementsInterfaceMethod
            Implements ISourceGenerator

            Public Sub Initialize(context As GeneratorInitializationContext) Implements ISourceGenerator.Initialize
            End Sub

            Public Sub Execute(context As GeneratorExecutionContext) Implements ISourceGenerator.Execute
                Dim [interface] = context.Compilation.GetTypeByMetadataName("IInterface")
                Dim memberName = [interface].MemberNames.Single()

                Dim text = "public partial class GeneratedClass { public int " + memberName + " { get; set; } }"
                context.AddSource("Implementation.cs", text)
            End Sub
        End Class
    End Class
End Namespace
