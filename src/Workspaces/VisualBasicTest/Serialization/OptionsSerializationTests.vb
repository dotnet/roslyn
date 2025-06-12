' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports System.Threading
Imports Microsoft.CodeAnalysis.Serialization
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Serialization
    <UseExportProvider>
    Public NotInheritable Class OptionsSerializationTests
        <Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2325692")>
        Public Sub TestNullGlobalImport()
            Dim options = New VisualBasicCompilationOptions(
                OutputKind.ConsoleApplication,
                "module",
                globalImports:={Nothing})

            Using workspace = New AdhocWorkspace()
                Dim service = workspace.Services.GetLanguageServices(LanguageNames.VisualBasic).GetService(Of IOptionsSerializationService)()

                Using stream = New MemoryStream()
                    Dim writer = New ObjectWriter(stream, leaveOpen:=True)
                    service.WriteTo(options, writer, CancellationToken.None)
                    stream.Position = 0

                    Dim reader = ObjectReader.TryGetReader(stream, leaveOpen:=True)
                    Dim serializedOptions = DirectCast(
                        service.ReadCompilationOptionsFrom(reader, CancellationToken.None),
                        VisualBasicCompilationOptions)

                    Assert.Empty(serializedOptions.GlobalImports)
                End Using
            End Using
        End Sub
    End Class
End Namespace
