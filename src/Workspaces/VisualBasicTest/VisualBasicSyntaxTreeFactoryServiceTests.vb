' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    <UseExportProvider>
    Public Class VisualBasicSyntaxTreeFactoryServiceTests
        <Fact>
        Public Sub LanguageServiceFactoryCreatesSyntaxTrees()
            Using workspace = New AdhocWorkspace()
                Dim service = workspace.Services.GetLanguageServices(LanguageNames.VisualBasic).GetRequiredService(Of ISyntaxTreeFactoryService)()
                Dim text = SourceText.From("Class C" & vbCrLf & "End Class")

                Dim tree = service.ParseSyntaxTree("test.vb", VisualBasicParseOptions.Default, text, CancellationToken.None)

                Assert.Equal("test.vb", tree.FilePath)
                Assert.Same(text, tree.GetText())
                Assert.IsType(Of CompilationUnitSyntax)(tree.GetRoot())
            End Using
        End Sub
    End Class
End Namespace
