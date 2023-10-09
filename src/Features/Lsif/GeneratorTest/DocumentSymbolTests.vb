' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports LSP = Microsoft.VisualStudio.LanguageServer.Protocol

Namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.UnitTests
    <UseExportProvider>
    Public Class DocumentSymbolTests
        <Theory>
        <InlineData("{|fullRange:class [|C|] { }|}", LSP.SymbolKind.Class, "C")>
        <InlineData(" {|fullRange:/* leading comment 1 */ /* leading comment 2 */ class [|C|] { } /* trailing comment 1*/ /* trailing comment 2 */|} ", LSP.SymbolKind.Class, "C")>
        <InlineData("{|fullRange:class [|@class|] { }|}", LSP.SymbolKind.Class, "@class")>
        <InlineData("{|fullRange:struct [|S|] { }|}", LSP.SymbolKind.Struct, "S")>
        <InlineData("class C { {|fullRange:void [|M|]() { }|} }", LSP.SymbolKind.Method, "M")>
        <InlineData("class C { int {|fullRange:[|field|]|}; }", LSP.SymbolKind.Field, "field")>
        Public Async Function TestDefinition(code As String, expectedSymbolKind As LSP.SymbolKind, expectedText As String) As Task
            Dim lsif = Await TestLsifOutput.GenerateForWorkspaceAsync(
                <Workspace>
                    <Project Language="C#" FilePath="Z:\TestProject.csproj" CommonReferences="true">
                        <Document Name="A.cs" FilePath="Z:\A.cs">
                            <%= code %>
                        </Document>
                    </Project>
                </Workspace>)

            Dim selectedRange = Await lsif.GetSelectedRangeAsync()
            Assert.NotNull(selectedRange)
            Dim definitionTag = Assert.IsType(Of DefinitionRangeTag)(selectedRange.Tag)
            Assert.Equal(expectedSymbolKind, definitionTag.Kind)

            Dim expectedFullRange = Await lsif.GetAnnotatedLspRangeAsync("fullRange")
            Assert.Equal(expectedFullRange, definitionTag.FullRange)

            Assert.Equal(expectedText, definitionTag.Text)
        End Function
    End Class
End Namespace
