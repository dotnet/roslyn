﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.SemanticSnippets
    <[UseExportProvider]>
    Public Class SemanticSnippetsCommandHandlerTests
        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInsertingConsoleSnippet() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class MyClass
{
    public void MyMethod()
    {
        $$
    }
}
                              </Document>)

                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ShowNewSnippetExperience, LanguageNames.CSharp), True)
                state.SendTypeChars("cw")
                Await state.AssertSelectedCompletionItem(displayText:="cw", inlineDescription:=Nothing, isHardSelected:=True)
                state.SendDownKey()
                Await state.AssertSelectedCompletionItem(displayText:="cw", inlineDescription:="Console.WriteLine", isHardSelected:=True)
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Equal("using System;
class MyClass
{
    public void MyMethod()
    {
        Console.WriteLine($$);
    }
}
", state.GetDocumentText())
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInsertingIfSnippet() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class MyClass
{
    public void MyMethod()
    {
        $$
    }
}
                              </Document>)

                state.Workspace.GlobalOptions.SetGlobalOption(New OptionKey(CompletionOptionsStorage.ShowNewSnippetExperience, LanguageNames.CSharp), True)
                state.SendTypeChars("if")
                Await state.AssertSelectedCompletionItem(displayText:="if", inlineDescription:=Nothing, isHardSelected:=True)
                state.SendDownKey()
                Await state.AssertSelectedCompletionItem(displayText:="if", inlineDescription:="if statement", isHardSelected:=True)
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Equal("
class MyClass
{
    public void MyMethod()
    {
        if (true)
        {$$
        }
    }
}", state.GetDocumentText())
            End Using
        End Function
    End Class
End Namespace
