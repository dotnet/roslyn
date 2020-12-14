' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.CSharp
    <[UseExportProvider]>
    Public Class SourceGeneratorTests
        Private ReadOnly _outputHelper As Abstractions.ITestOutputHelper

        Public Sub New(outputHelper As Abstractions.ITestOutputHelper)
            _outputHelper = outputHelper
        End Sub

        <Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Rename)>
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
    End Class
End Namespace
