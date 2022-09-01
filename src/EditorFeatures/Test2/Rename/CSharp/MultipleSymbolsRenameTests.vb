' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Rename
Imports Xunit.Abstractions

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.CSharp

    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.Rename)>
    Public Class MultipleSymbolsRenameTests
        Private ReadOnly _outputHelper As ITestOutputHelper

        Public Sub New(outputHelper As ITestOutputHelper)
            _outputHelper = outputHelper
        End Sub

        <Theory, CombinatorialData>
        Public Async Function NonConflictRename(inProcess As Boolean) As Task
            Using result = Await RenameEngineResult.CreateForRenamingMultipleSymbolsAsync(_outputHelper,
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
namespace {|nonConflictLocation1:XYZ|}
{
    public class {|nonConflictLocation2:ABC|}
    {
        public void {|nonConflictLocation3:ZXVY|}() { }
    }
}
                        </Document>
                            <Document>
using {|renameSymbol1:XYZ|};

namespace ConsoleApp4
{
    internal class Class1
    {
        public Class1()
        {
            var abc = new {|renameSymbol2:ABC|}();
            abc.{|renameSymbol3:ZXVY|}();
        }
    }
}
                            </Document>
                        </Project>
                    </Workspace>, inProcess:=inProcess,
                          renameSymbolTagToReplacementStringAndOptions:=New Dictionary(Of String, (replacementText As String, renameOptions As SymbolRenameOptions)) From
                              {{"renameSymbol1", ("XYZ2", New SymbolRenameOptions())}, {"renameSymbol2", ("ABC2", New SymbolRenameOptions())}, {"renameSymbol3", ("ZXVY2", New SymbolRenameOptions())}},
                        nonConflictLocationTagToReplacementText:=New Dictionary(Of String, String) From
                        {{"nonConflictLocation1", "XYZ2"}, {"nonConflictLocation2", "ABC2"}, {"nonConflictLocation3", "ZXVY2"}, {"renameSymbol1", "XYZ2"}, {"renameSymbol2", "ABC2"}, {"renameSymbol3", "ZXVY2"}})

            End Using
        End Function

    End Class
End Namespace
