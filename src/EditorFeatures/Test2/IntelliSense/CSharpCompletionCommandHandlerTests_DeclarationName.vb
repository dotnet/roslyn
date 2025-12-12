' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CSharp

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.Completion)>
    Public Class CSharpCompletionCommandHandlerTests_DeclarationName
        <WpfTheory, CombinatorialData>
        Public Async Function SuggestParameterNamesFromExistingOverloads(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document>
using System.Threading;
public class C
{
    void M(CancellationToken myTok) { }
    void M(CancellationToken$$
}
                </Document>,
                showCompletionInArgumentLists:=showCompletionInArgumentLists, languageVersion:=LanguageVersion.CSharp12)

                state.SendTypeChars(" ")
                Await state.AssertSelectedCompletionItem(displayText:="myTok", isHardSelected:=False)
            End Using
        End Function
    End Class
End Namespace
