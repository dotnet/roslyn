' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.CSharp
Imports Microsoft.CodeAnalysis.Editor.CSharp.Formatting
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Tags
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Text.Projection
Imports Microsoft.VisualStudio.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <[UseExportProvider]>
    Public Class CSharpCompletionCommandHandlerTests

        Public Shared ReadOnly Property AllCompletionImplementations() As IEnumerable(Of Object())
            Get
                Return TestStateFactory.GetAllCompletionImplementations()
            End Get
        End Property

        <WorkItem(541201, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541201")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TabCommitsWithoutAUniqueMatch(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document>
                                  $$
                              </Document>)

                state.SendTypeChars("using System.Ne")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem(displayText:="Net", isHardSelected:=True)
                state.SendTypeChars("x")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem(displayText:="Net", isSoftSelected:=True)
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("using System.Net", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAtEndOfFile(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document>$$</Document>)

                state.SendTypeChars("us")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("using", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotAtStartOfExistingWord(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document>$$using</Document>)

                state.SendTypeChars("u")
                Await state.AssertNoCompletionSession()
                Assert.Contains("using", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMSCorLibTypes(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document>
using System;

class c : $$
                              </Document>)

                state.SendTypeChars("A")
                Await state.AssertCompletionSession()

                Assert.True(state.CompletionItemsContainsAll(displayText:={"Attribute", "Exception", "IDisposable"}))
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestFiltering1(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document>
using System;
      
class c { $$
                              </Document>)

                state.SendTypeChars("Sy")
                Await state.AssertCompletionSession()

                Assert.True(state.CompletionItemsContainsAll(displayText:={"OperatingSystem", "System", "SystemException"}))
                Assert.False(state.CompletionItemsContainsAny(displayText:={"Exception", "Activator"}))
            End Using
        End Function

        ' NOTE(cyrusn): This should just be a unit test for SymbolCompletionProvider.  However, I'm
        ' just porting the integration tests to here for now.
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMultipleTypes(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document>
class C { $$ } struct S { } enum E { } interface I { } delegate void D();
                              </Document>)

                state.SendTypeChars("C")
                Await state.AssertCompletionSession()
                Assert.True(state.CompletionItemsContainsAll(displayText:={"C", "S", "E", "I", "D"}))
            End Using
        End Function

        ' NOTE(cyrusn): This should just be a unit test for KeywordCompletionProvider.  However, I'm
        ' just porting the integration tests to here for now.
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInEmptyFile(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document>
$$
                              </Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                Assert.True(state.CompletionItemsContainsAll(displayText:={"abstract", "class", "namespace"}))
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotAfterTypingDotAfterIntegerLiteral(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document>
class c { void M() { 3$$ } }
                              </Document>)

                state.SendTypeChars(".")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAfterExplicitInvokeAfterDotAfterIntegerLiteral(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document>
class c { void M() { 3.$$ } }
                              </Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                Assert.True(state.CompletionItemsContainsAll({"ToString"}))
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEnterIsConsumed(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document>
class Class1
{
    void Main(string[] args)
    {
        $$
    }
}</Document>)

                state.SendTypeChars("System.TimeSpan.FromMin")
                state.SendReturn()
                Await state.WaitForAsynchronousOperationsAsync()
                Assert.Equal(<text>
class Class1
{
    void Main(string[] args)
    {
        System.TimeSpan.FromMinutes
    }
}</text>.NormalizedValue, state.GetDocumentText())
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDescription1(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
using System;

/// <summary>
/// TestDocComment
/// </summary>
class TestException : Exception { }

class MyException : $$]]></Document>)

                state.SendTypeChars("Test")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem(description:="class TestException" & vbCrLf & "TestDocComment")
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestObjectCreationPreselection1(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
using System.Collections.Generic;

class C
{
    public void Goo()
    {
        List<int> list = new$$
    }
}]]></Document>)

                state.SendTypeChars(" ")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem(displayText:="List<int>", isHardSelected:=True)
                Assert.True(state.CompletionItemsContainsAll(displayText:={"LinkedList", "List", "System"}))
                state.SendTypeChars("Li")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem(displayText:="List<int>", isHardSelected:=True)
                Assert.True(state.CompletionItemsContainsAll(displayText:={"LinkedList", "List"}))
                Assert.False(state.CompletionItemsContainsAny(displayText:={"System"}))
                state.SendTypeChars("n")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem(displayText:="LinkedList", displayTextSuffix:="<>", isHardSelected:=True)
                state.SendBackspace()
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem(displayText:="List<int>", isHardSelected:=True)
                state.SendTab()
                Assert.Contains("new List<int>", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDeconstructionDeclaration(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class C
{
    public void Goo()
    {
       var ($$
    }
}]]></Document>)

                state.SendTypeChars("i")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDeconstructionDeclaration2(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class C
{
    public void Goo()
    {
       var (a, $$
    }
}]]></Document>)

                state.SendTypeChars("i")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDeconstructionDeclaration3(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class C
{
    public void Goo()
    {
       var ($$) = (1, 2);
    }
}]]></Document>)

                state.SendTypeChars("i")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParenthesizedDeconstructionDeclarationWithVar(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class Variable
{
    public void Goo()
    {
       (var a$$) = (1, 2);
    }
}]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem(displayText:="as", isHardSelected:=False)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParenthesizedDeconstructionDeclarationWithVarAfterComma(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class Variable
{
    public void Goo()
    {
       (var a, var a$$) = (1, 2);
    }
}]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem(displayText:="as", isHardSelected:=False)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParenthesizedVarDeconstructionDeclarationWithVar(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class Variable
{
    public void Goo()
    {
       (var a, var ($$)) = (1, 2);
    }
}]]></Document>)


                state.SendTypeChars("a")
                Await state.AssertNoCompletionSession()

                state.SendTypeChars(", a")
                Await state.AssertNoCompletionSession()
                Assert.Contains("(var a, var (a, a)) = ", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestVarDeconstructionDeclarationWithVar(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class Variable
{
    public void Goo()
    {
        $$
    }
}]]></Document>)

                state.SendTypeChars("va")
                Await state.AssertSelectedCompletionItem(displayText:="var", isHardSelected:=True)

                state.SendTypeChars(" (a")
                Await state.AssertNoCompletionSession()

                state.SendTypeChars(", a")
                Await state.AssertNoCompletionSession()
                Assert.Contains("var (a, a", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParenthesizedDeconstructionDeclarationWithSymbol(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class Variable
{
    public void Goo()
    {
       ($$) = (1, 2);
    }
}]]></Document>)

                state.SendTypeChars("vari")
                Await state.AssertSelectedCompletionItem(displayText:="Variable", isHardSelected:=True)
                state.SendTypeChars(" ")
                Assert.Contains("(Variable ", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                Await state.AssertNoCompletionSession()

                state.SendTypeChars("x, vari")
                Await state.AssertSelectedCompletionItem(displayText:="Variable", isHardSelected:=True)
                state.SendTypeChars(" ")
                Assert.Contains("(Variable x, Variable ", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                Await state.AssertSelectedCompletionItem(displayText:="Variable", isHardSelected:=False)
                Assert.True(state.CompletionItemsContainsAll({"variable"}))
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParenthesizedDeconstructionDeclarationWithInt(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class Integer
{
    public void Goo()
    {
       ($$) = (1, 2);
    }
}]]></Document>)

                state.SendTypeChars("int")
                Await state.AssertSelectedCompletionItem(displayText:="int", isHardSelected:=True)
                state.SendTypeChars(" ")
                Assert.Contains("(int ", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                Await state.AssertNoCompletionSession()

                state.SendTypeChars("x, int")
                Await state.AssertSelectedCompletionItem(displayText:="int", isHardSelected:=True)
                state.SendTypeChars(" ")
                Assert.Contains("(int x, int ", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestIncompleteParenthesizedDeconstructionDeclaration(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class Variable
{
    public void Goo()
    {
       ($$
    }
}]]></Document>)

                state.SendTypeChars("va")
                Await state.AssertSelectedCompletionItem(displayText:="var", isHardSelected:=True)
                state.SendTypeChars(" ")
                Await state.AssertNoCompletionSession()

                state.SendTypeChars("a")
                Await state.AssertSelectedCompletionItem(displayText:="as", isSoftSelected:=True)

                state.SendTypeChars(", va")
                Await state.AssertSelectedCompletionItem(displayText:="var", isHardSelected:=True)
                state.SendTypeChars(" ")
                Await state.AssertNoCompletionSession()

                state.SendTypeChars("a")
                Await state.AssertSelectedCompletionItem(displayText:="as", isSoftSelected:=True)
                state.SendTypeChars(")")
                Assert.Contains("(var a, var a)", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestIncompleteParenthesizedDeconstructionDeclaration2(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class Variable
{
    public void Goo()
    {
       ($$)
    }
}]]></Document>)

                state.SendTypeChars("va")
                Await state.AssertSelectedCompletionItem(displayText:="var", isHardSelected:=True)
                state.SendTypeChars(" ")
                Await state.AssertNoCompletionSession()

                state.SendTypeChars("a")
                Await state.AssertSelectedCompletionItem(displayText:="as", isSoftSelected:=True)

                state.SendTypeChars(", va")
                Await state.AssertSelectedCompletionItem(displayText:="var", isHardSelected:=True)
                state.SendTypeChars(" ")
                Await state.AssertNoCompletionSession()

                state.SendTypeChars("a")
                Await state.AssertSelectedCompletionItem(displayText:="as", isSoftSelected:=True)
                state.SendReturn()
                Assert.Contains("(var a, var a", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBackspaceInIncompleteParenthesizedDeconstructionDeclaration(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class Variable
{
    public void Goo()
    {
       (var as$$
    }
}]]></Document>)

                state.Workspace.Options = state.Workspace.Options.WithChangedOption(
                    CompletionOptions.TriggerOnDeletion, LanguageNames.CSharp, True)

                state.SendBackspace()
                ' This completionImplementation is hard-selected because the suggestion mode never triggers on backspace
                ' See issue https://github.com/dotnet/roslyn/issues/15302
                Await state.AssertSelectedCompletionItem(displayText:="as", isHardSelected:=True)

                state.SendTypeChars(", var as")
                state.SendBackspace()
                Await state.AssertSelectedCompletionItem(displayText:="as", isSoftSelected:=True)

                state.SendTypeChars(")")
                Await state.AssertNoCompletionSession()
                Assert.Contains("(var as, var a)", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBackspaceInParenthesizedDeconstructionDeclaration(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class Variable
{
    public void Goo()
    {
       (var as$$)
    }
}]]></Document>)

                state.Workspace.Options = state.Workspace.Options.WithChangedOption(
                    CompletionOptions.TriggerOnDeletion, LanguageNames.CSharp, True)

                state.SendBackspace()
                ' This completionImplementation is hard-selected because the suggestion mode never triggers on backspace
                ' See issue https://github.com/dotnet/roslyn/issues/15302
                Await state.AssertSelectedCompletionItem(displayText:="as", isHardSelected:=True)

                state.SendTypeChars(", var as")
                state.SendBackspace()
                Await state.AssertSelectedCompletionItem(displayText:="as", isSoftSelected:=True)

                state.SendReturn()
                Await state.AssertNoCompletionSession()
                Assert.Contains("(var as, var a", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(17256, "https://github.com/dotnet/roslyn/issues/17256")>
        Public Async Function TestThrowExpression(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
using System;
class C
{
    public object Goo()
    {
        return null ?? throw new$$
    }
}]]></Document>)

                state.SendTypeChars(" ")
                Await state.AssertSelectedCompletionItem(displayText:="Exception", isHardSelected:=True)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(17256, "https://github.com/dotnet/roslyn/issues/17256")>
        Public Async Function TestThrowStatement(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
using System;
class C
{
    public object Goo()
    {
        throw new$$
    }
}]]></Document>)

                state.SendTypeChars(" ")
                Await state.AssertSelectedCompletionItem(displayText:="Exception", isHardSelected:=True)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory>
        Public Async Function TestNonTrailingNamedArgumentInCSharp7_1(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(completionImplementation,
                 <Workspace>
                     <Project Language="C#" LanguageVersion="CSharp7_1" CommonReferences="true" AssemblyName="CSProj">
                         <Document FilePath="C.cs">
class C
{
    public void M()
    {
        int better = 2;
        M(a: 1, $$)
    }
    public void M(int a, int bar, int c) { }
}
                         </Document>
                     </Project>
                 </Workspace>)

                state.SendTypeChars("b")
                Await state.AssertSelectedCompletionItem(displayText:="bar", displayTextSuffix:=":", isHardSelected:=True)
                state.SendTypeChars("e")
                Await state.AssertSelectedCompletionItem(displayText:="bar", displayTextSuffix:=":", isSoftSelected:=True)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory>
        Public Async Function TestNonTrailingNamedArgumentInCSharp7_2(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(completionImplementation,
                 <Workspace>
                     <Project Language="C#" LanguageVersion="CSharp7_2" CommonReferences="true" AssemblyName="CSProj">
                         <Document FilePath="C.cs">
class C
{
    public void M()
    {
        int better = 2;
        M(a: 1, $$)
    }
    public void M(int a, int bar, int c) { }
}
                         </Document>
                     </Project>
                 </Workspace>)

                state.SendTypeChars("b")
                Await state.AssertSelectedCompletionItem(displayText:="better", isHardSelected:=True)
                state.SendTypeChars("a")
                Await state.AssertSelectedCompletionItem(displayText:="bar", displayTextSuffix:=":", isHardSelected:=True)
                state.SendBackspace()
                Await state.AssertSelectedCompletionItem(displayText:="better", isHardSelected:=True)
                state.SendTypeChars(", ")
                Assert.Contains("M(a: 1, better,", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(4677, "https://github.com/dotnet/roslyn/issues/4677")>
        Public Async Function TestDefaultSwitchLabel(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class C
{
    public void M(object o)
    {
        switch (o)
        {
            default:
                goto $$
        }
    }
}]]></Document>)

                state.SendTypeChars("d")
                Await state.AssertSelectedCompletionItem(displayText:="default", isHardSelected:=True)
                state.SendTypeChars(";")
                Assert.Contains("goto default;", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(4677, "https://github.com/dotnet/roslyn/issues/4677")>
        Public Async Function TestGotoOrdinaryLabel(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class C
{
    public void M(object o)
    {
label1:
        goto $$
    }
}]]></Document>)

                state.SendTypeChars("l")
                Await state.AssertSelectedCompletionItem(displayText:="label1", isHardSelected:=True)
                state.SendTypeChars(";")
                Assert.Contains("goto label1;", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(4677, "https://github.com/dotnet/roslyn/issues/4677")>
        Public Async Function TestEscapedDefaultLabel(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class C
{
    public void M(object o)
    {
@default:
        goto $$
    }
}]]></Document>)

                state.SendTypeChars("d")
                Await state.AssertSelectedCompletionItem(displayText:="@default", isHardSelected:=True)
                state.SendTypeChars(";")
                Assert.Contains("goto @default;", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(4677, "https://github.com/dotnet/roslyn/issues/4677")>
        Public Async Function TestEscapedDefaultLabel2(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class C
{
    public void M(object o)
    {
        switch (o)
        {
            default:
@default:
                goto $$
        }
    }
}]]></Document>)

                state.SendTypeChars("d")
                Await state.AssertSelectedCompletionItem(displayText:="default", isHardSelected:=True)
                state.SendTypeChars(";")
                Assert.Contains("goto default;", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(4677, "https://github.com/dotnet/roslyn/issues/4677")>
        Public Async Function TestEscapedDefaultLabelWithoutSwitch(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class C
{
    public void M(object o)
    {
@default:
        goto $$
    }
}]]></Document>)

                state.SendTypeChars("d")
                Await state.AssertSelectedCompletionItem(displayText:="@default", isHardSelected:=True)
                state.SendTypeChars(";")
                Assert.Contains("goto @default;", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(24432, "https://github.com/dotnet/roslyn/issues/24432")>
        Public Async Function TestArrayInitialization(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class Class
{
    public void M()
    {
        Class[] x = $$
    }
}]]></Document>)

                state.SendTypeChars("new ")
                Await state.AssertSelectedCompletionItem(displayText:="Class", isSoftSelected:=True)
                state.SendTypeChars("C")
                Await state.AssertSelectedCompletionItem(displayText:="Class", isHardSelected:=True)
                state.SendTypeChars("[")
                Assert.Contains("Class[] x = new Class[", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                state.SendTypeChars("] {")
                Assert.Contains("Class[] x = new Class[] {", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(24432, "https://github.com/dotnet/roslyn/issues/24432")>
        Public Async Function TestImplicitArrayInitialization(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class Class
{
    public void M()
    {
        Class[] x = $$
    }
}]]></Document>)

                state.SendTypeChars("n")
                Await state.AssertSelectedCompletionItem(displayText:="nameof", isHardSelected:=True)
                state.SendTypeChars("e")
                Await state.AssertSelectedCompletionItem(displayText:="new", isHardSelected:=True)
                state.SendTypeChars(" ")
                Await state.AssertSelectedCompletionItem(displayText:="Class", isSoftSelected:=True)
                state.SendTypeChars("[")
                Assert.Contains("Class[] x = new [", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                state.SendTypeChars("] {")
                Assert.Contains("Class[] x = new [] {", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(24432, "https://github.com/dotnet/roslyn/issues/24432")>
        Public Async Function TestImplicitArrayInitialization2(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class Class
{
    public void M()
    {
        Class[] x = $$
    }
}]]></Document>)

                state.SendTypeChars("ne")
                Await state.AssertSelectedCompletionItem(displayText:="new", isHardSelected:=True)
                state.SendTypeChars("[")
                Assert.Contains("Class[] x = new[", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(24432, "https://github.com/dotnet/roslyn/issues/24432")>
        Public Async Function TestImplicitArrayInitialization3(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class Class
{
    public void M()
    {
        Class[] x = $$
    }
}]]></Document>)

                state.SendTypeChars("ne")
                Await state.AssertSelectedCompletionItem(displayText:="new", isHardSelected:=True)
                state.SendTypeChars(" ")
                Await state.AssertSelectedCompletionItem(displayText:="Class", isSoftSelected:=True)
                Assert.Contains("Class[] x = new ", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                state.SendTypeChars("[")
                Assert.Contains("Class[] x = new [", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(24432, "https://github.com/dotnet/roslyn/issues/24432")>
        Public Async Function TestImplicitArrayInitialization4(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class Class
{
    public void M()
    {
        Class[] x =$$
    }
}]]></Document>)

                state.SendTypeChars(" ")
                Await state.AssertNoCompletionSession()
                state.SendTypeChars("{")
                Assert.Contains("Class[] x = {", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(24432, "https://github.com/dotnet/roslyn/issues/24432")>
        Public Async Function TestImplicitArrayInitialization_WithTab(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class Class
{
    public void M()
    {
        Class[] x = $$
    }
}]]></Document>)

                state.SendTypeChars("ne")
                Await state.AssertSelectedCompletionItem(displayText:="new", isHardSelected:=True)
                state.SendTypeChars(" ")
                Await state.AssertSelectedCompletionItem(displayText:="Class", isSoftSelected:=True)
                Assert.Contains("Class[] x = new ", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                state.SendTab()
                Assert.Contains("Class[] x = new Class", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(24432, "https://github.com/dotnet/roslyn/issues/24432")>
        Public Async Function TestTypelessImplicitArrayInitialization(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class Class
{
    public void M()
    {
        var x = $$
    }
}]]></Document>)

                state.SendTypeChars("ne")
                Await state.AssertSelectedCompletionItem(displayText:="new", isHardSelected:=True)
                state.SendTypeChars(" ")
                Await state.AssertNoCompletionSession()
                state.SendTypeChars("[")
                Assert.Contains("var x = new [", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                state.SendTypeChars("] {")
                Assert.Contains("var x = new [] {", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(24432, "https://github.com/dotnet/roslyn/issues/24432")>
        Public Async Function TestTypelessImplicitArrayInitialization2(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class Class
{
    public void M()
    {
        var x = $$
    }
}]]></Document>)

                state.SendTypeChars("ne")
                Await state.AssertSelectedCompletionItem(displayText:="new", isHardSelected:=True)
                state.SendTypeChars("[")
                Assert.Contains("var x = new[", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(24432, "https://github.com/dotnet/roslyn/issues/24432")>
        Public Async Function TestTypelessImplicitArrayInitialization3(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class Class
{
    public void M()
    {
        var x = $$
    }
}]]></Document>)

                state.SendTypeChars("ne")
                Await state.AssertSelectedCompletionItem(displayText:="new", isHardSelected:=True)
                state.SendTypeChars(" ")
                Assert.Contains("var x = new ", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
                state.SendTypeChars("[")
                Assert.Contains("var x = new [", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(13527, "https://github.com/dotnet/roslyn/issues/13527")>
        Public Async Function TestSymbolInTupleLiteral(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class C
{
    public void Fo()
    {
        ($$)
    }
}]]></Document>)

                state.SendTypeChars("F")
                Await state.AssertSelectedCompletionItem(displayText:="Fo", isHardSelected:=True)
                state.SendTypeChars(":")
                Assert.Contains("(F:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(13527, "https://github.com/dotnet/roslyn/issues/13527")>
        Public Async Function TestSymbolInTupleLiteralAfterComma(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class C
{
    public void Fo()
    {
        (x, $$)
    }
}]]></Document>)

                state.SendTypeChars("F")
                Await state.AssertSelectedCompletionItem(displayText:="Fo", isHardSelected:=True)
                state.SendTypeChars(":")
                Assert.Contains("(x, F:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(19335, "https://github.com/dotnet/roslyn/issues/19335")>
        Public Async Function ColonInTupleNameInTupleLiteral(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class C
{
    public void M()
    {
        (int first, int second) t = ($$
    }
}]]></Document>)

                state.SendTypeChars("fi")
                Await state.AssertSelectedCompletionItem(displayText:="first", displayTextSuffix:=":", isHardSelected:=True)
                Assert.Equal("first", state.GetSelectedItem().FilterText)
                state.SendTypeChars(":")
                Assert.Contains("(first:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(19335, "https://github.com/dotnet/roslyn/issues/19335")>
        Public Async Function ColonInExactTupleNameInTupleLiteral(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class C
{
    public void M()
    {
        (int first, int second) t = ($$
    }
}]]></Document>)

                state.SendTypeChars("first")
                Await state.AssertSelectedCompletionItem(displayText:="first", displayTextSuffix:=":", isHardSelected:=True)
                Assert.Equal("first", state.GetSelectedItem().FilterText)
                state.SendTypeChars(":")
                Assert.Contains("(first:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(19335, "https://github.com/dotnet/roslyn/issues/19335")>
        Public Async Function ColonInTupleNameInTupleLiteralAfterComma(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class C
{
    public void M()
    {
        (int first, int second) t = (0, $$
    }
}]]></Document>)

                state.SendTypeChars("se")
                Await state.AssertSelectedCompletionItem(displayText:="second", displayTextSuffix:=":", isHardSelected:=True)
                Assert.Equal("second", state.GetSelectedItem().FilterText)
                state.SendTypeChars(":")
                Assert.Contains("(0, second:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(19335, "https://github.com/dotnet/roslyn/issues/19335")>
        Public Async Function TabInTupleNameInTupleLiteral(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class C
{
    public void M()
    {
        (int first, int second) t = ($$
    }
}]]></Document>)

                state.SendTypeChars("fi")
                Await state.AssertSelectedCompletionItem(displayText:="first", displayTextSuffix:=":", isHardSelected:=True)
                Assert.Equal("first", state.GetSelectedItem().FilterText)
                state.SendTab()
                state.SendTypeChars(":")
                state.SendTypeChars("0")
                Assert.Contains("(first:0", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(19335, "https://github.com/dotnet/roslyn/issues/19335")>
        Public Async Function TabInExactTupleNameInTupleLiteral(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class C
{
    public void M()
    {
        (int first, int second) t = ($$
    }
}]]></Document>)

                state.SendTypeChars("first")
                Await state.AssertSelectedCompletionItem(displayText:="first", displayTextSuffix:=":", isHardSelected:=True)
                Assert.Equal("first", state.GetSelectedItem().FilterText)
                state.SendTab()
                state.SendTypeChars(":")
                state.SendTypeChars("0")
                Assert.Contains("(first:0", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(19335, "https://github.com/dotnet/roslyn/issues/19335")>
        Public Async Function TabInTupleNameInTupleLiteralAfterComma(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class C
{
    public void M()
    {
        (int first, int second) t = (0, $$
    }
}]]></Document>)

                state.SendTypeChars("se")
                Await state.AssertSelectedCompletionItem(displayText:="second", displayTextSuffix:=":", isHardSelected:=True)
                Assert.Equal("second", state.GetSelectedItem().FilterText)
                state.SendTab()
                state.SendTypeChars(":")
                state.SendTypeChars("1")
                Assert.Contains("(0, second:1", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(13527, "https://github.com/dotnet/roslyn/issues/13527")>
        Public Async Function TestKeywordInTupleLiteral(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class C
{
    public void Goo()
    {
        ($$)
    }
}]]></Document>)

                state.SendTypeChars("d")
                Await state.AssertSelectedCompletionItem(displayText:="decimal", isHardSelected:=True)
                state.SendTypeChars(":")
                Assert.Contains("(d:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(13527, "https://github.com/dotnet/roslyn/issues/13527")>
        Public Async Function TestTupleType(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class C
{
    public void Goo()
    {
        ($$)
    }
}]]></Document>)

                state.SendTypeChars("d")
                Await state.AssertSelectedCompletionItem(displayText:="decimal", isHardSelected:=True)
                state.SendTypeChars(" ")
                Assert.Contains("(decimal ", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(13527, "https://github.com/dotnet/roslyn/issues/13527")>
        Public Async Function TestDefaultKeyword(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class C
{
    public void Goo()
    {
        switch(true)
        {
            $$
        }
    }
}]]></Document>)

                state.SendTypeChars("def")
                Await state.AssertSelectedCompletionItem(displayText:="default", isHardSelected:=True)
                state.SendTypeChars(":")
                Assert.Contains("default:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(13527, "https://github.com/dotnet/roslyn/issues/13527")>
        Public Async Function TestParenthesizedExpression(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class C
{
    public void Fo()
    {
        ($$)
    }
}]]></Document>)

                state.SendTypeChars("F")
                Await state.AssertSelectedCompletionItem(displayText:="Fo", isHardSelected:=True)
                state.SendTypeChars(".")
                Assert.Contains("(Fo.", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(13527, "https://github.com/dotnet/roslyn/issues/13527")>
        Public Async Function TestInvocationExpression(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class C
{
    public void Goo(int Alice)
    {
        Goo($$)
    }
}]]></Document>)

                state.SendTypeChars("A")
                Await state.AssertSelectedCompletionItem(displayText:="Alice", isHardSelected:=True)
                state.SendTypeChars(":")
                Assert.Contains("Goo(Alice:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(13527, "https://github.com/dotnet/roslyn/issues/13527")>
        Public Async Function TestInvocationExpressionAfterComma(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class C
{
    public void Goo(int Alice, int Bob)
    {
        Goo(1, $$)
    }
}]]></Document>)

                state.SendTypeChars("B")
                Await state.AssertSelectedCompletionItem(displayText:="Bob", isHardSelected:=True)
                state.SendTypeChars(":")
                Assert.Contains("Goo(1, Bob:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(13527, "https://github.com/dotnet/roslyn/issues/13527")>
        Public Async Function TestCaseLabel(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class C
{
    public void Fo()
    {
        switch (1)
        {
            case $$
        }
    }
}]]></Document>)

                state.SendTypeChars("F")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem(displayText:="Fo", isHardSelected:=True)
                state.SendTypeChars(":")
                Assert.Contains("case Fo:", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(543268, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543268")>
        Public Async Function TestTypePreselection1(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
partial class C
{
}
partial class C
{
    $$
}]]></Document>)

                state.SendTypeChars("C")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem(displayText:="C", isHardSelected:=True)
                state.SendTypeChars(" ")
                Await state.AssertCompletionSession()
            End Using
        End Function

        <WorkItem(543519, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543519")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNewPreselectionAfterVar(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class C
{
    void M()
    {
        var c = $$
    }
}]]></Document>)

                state.SendTypeChars("new ")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WorkItem(543559, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543559")>
        <WorkItem(543561, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543561")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEscapedIdentifiers(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class @return
{
    void goo()
    {
        $$
    }
}
]]></Document>)

                state.SendTypeChars("@")
                Await state.AssertNoCompletionSession()
                state.SendTypeChars("r")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem(displayText:="@return", isHardSelected:=True)
                state.SendTab()
                Assert.Contains("@return", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(543771, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543771")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitUniqueItem1(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
using System;
 
class Program
{
    static void Main(string[] args)
    {
        Console.WriteL$$();
    }
}]]></Document>)

                state.SendCommitUniqueCompletionListItem()
                Await state.AssertNoCompletionSession()
                Assert.Contains("WriteLine()", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(543771, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543771")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitUniqueItem2(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
using System;
 
class Program
{
    static void Main(string[] args)
    {
        Console.WriteL$$ine();
    }
}]]></Document>)

                state.SendCommitUniqueCompletionListItem()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitForUsingDirective1(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document>
                                  $$
                              </Document>)

                state.SendTypeChars("using Sys")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem(displayText:="System", isHardSelected:=True)
                state.SendTypeChars("(")
                Await state.AssertNoCompletionSession()
                Assert.Contains("using Sys(", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitForUsingDirective2(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document>
                                  $$
                              </Document>)

                state.SendTypeChars("using Sys")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem(displayText:="System", isHardSelected:=True)
                state.SendTypeChars(".")
                Await state.AssertCompletionSession()
                Assert.Contains("using System.", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitForUsingDirective3(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document>
                                  $$
                              </Document>, extraExportedTypes:={GetType(CSharpEditorFormattingService)}.ToList())

                state.SendTypeChars("using Sys")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem(displayText:="System", isHardSelected:=True)
                state.SendTypeChars(";")
                Await state.AssertNoCompletionSession()
                state.AssertMatchesTextStartingAtLine(1, "using System;")
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitForUsingDirective4(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                            <Document>
                                $$
                            </Document>)

                state.SendTypeChars("using Sys")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem(displayText:="System", isHardSelected:=True)
                state.SendTypeChars(" ")
                Await state.AssertNoCompletionSession()
                Assert.Contains("using Sys ", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function KeywordsIncludedInObjectCreationCompletion(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document>
class C
{
    void Goo()
    {
        string s = new$$
    }
}
                              </Document>)

                state.SendTypeChars(" ")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem(displayText:="string", isHardSelected:=True)
                Assert.True(state.GetCompletionItems().Any(Function(c) c.DisplayText = "int"))
            End Using
        End Function

        <WorkItem(544293, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544293")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NoKeywordsOrSymbolsAfterNamedParameterWithCSharp7() As Task
            Using state = TestState.CreateTestStateFromWorkspace(
                        <Workspace>
                            <Project Language="C#" CommonReferences="true" LanguageVersion="7">
                                <Document>
class Goo
{
    void Test()
    {
        object m = null;
        Method(obj:m, $$
    }
 
    void Method(object obj, int num = 23, string str = "")
    {
    }
}
                              </Document>
                            </Project>
                        </Workspace>)

                state.SendTypeChars("a")
                Await state.AssertCompletionSession()
                Assert.True(state.GetCompletionItems().Any(Function(i) i.DisplayText = "num" AndAlso i.DisplayTextSuffix = ":"))
                Assert.False(state.GetCompletionItems().Any(Function(i) i.DisplayText = "System"))
                Assert.False(state.GetCompletionItems().Any(Function(c) c.DisplayText = "int"))
            End Using
        End Function

        <WpfFact(Skip:="https://github.com/dotnet/roslyn/pull/29820"), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function KeywordsOrSymbolsAfterNamedParameter() As Task
            Using state = TestState.CreateCSharpTestState(
                                <Document>
class Goo
{
    void Test()
    {
        object m = null;
        Method(obj:m, $$
    }

    void Method(object obj, int num = 23, string str = "")
    {
    }
}
                              </Document>, languageVersion:=LanguageVersion.CSharp7)

                state.SendTypeChars("a")
                Await state.AssertCompletionSession()
                Assert.True(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(i) i.DisplayText = "num" AndAlso i.DisplayTextSuffix = ":"))
                Assert.True(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(i) i.DisplayText = "System"))
                Assert.True(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(c) c.DisplayText = "int"))
            End Using
        End Function

        <WorkItem(544017, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544017")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function EnumCompletionTriggeredOnSpace(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document>
enum Numeros { Uno, Dos }
class Goo
{
    void Bar(int a, Numeros n) { }
    void Baz()
    {
        Bar(0$$
    }
}
                              </Document>)

                state.SendTypeChars(", ")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem(displayText:="Numeros", isHardSelected:=True)
                Assert.Equal(1, state.GetCompletionItems().Where(Function(c) c.DisplayText = "Numeros").Count())
            End Using
        End Function

        <WorkItem(479078, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/479078")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function EnumCompletionTriggeredOnSpaceForNullables(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document>
enum Numeros { Uno, Dos }
class Goo
{
    void Bar(int a, Numeros? n) { }
    void Baz()
    {
        Bar(0$$
    }
}
                              </Document>)

                state.SendTypeChars(", ")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem(displayText:="Numeros", isHardSelected:=True)
                Assert.Equal(1, state.GetCompletionItems().Where(Function(c) c.DisplayText = "Numeros").Count())
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function EnumCompletionTriggeredOnDot(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document>
enum Numeros { Uno, Dos }
class Goo
{
    void Bar()
    {
        Numeros num = $$
    }
}
                </Document>)

                state.SendTypeChars("Nu.")
                Await state.WaitForAsynchronousOperationsAsync()
                Assert.Contains("Numeros num = Numeros.", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function EnumCompletionNotTriggeredOnPlusCommitCharacter(completionImplementation As CompletionImplementation) As Task
            Await EnumCompletionNotTriggeredOn("+"c, completionImplementation)
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function EnumCompletionNotTriggeredOnLeftBraceCommitCharacter(completionImplementation As CompletionImplementation) As Task
            Await EnumCompletionNotTriggeredOn("{"c, completionImplementation)
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function EnumCompletionNotTriggeredOnSpaceCommitCharacter(completionImplementation As CompletionImplementation) As Task
            Await EnumCompletionNotTriggeredOn(" "c, completionImplementation)
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function EnumCompletionNotTriggeredOnSemicolonCommitCharacter(completionImplementation As CompletionImplementation) As Task
            Await EnumCompletionNotTriggeredOn(";"c, completionImplementation)
        End Function

        Private Async Function EnumCompletionNotTriggeredOn(c As Char, completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document>
enum Numeros { Uno, Dos }
class Goo
{
    void Bar()
    {
        Numeros num = $$
    }
}
                </Document>)

                state.SendTypeChars("Nu")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem(displayText:="Numeros", isHardSelected:=True)
                state.SendTypeChars(c.ToString())
                Await state.WaitForAsynchronousOperationsAsync()
                Assert.NotEqual("Numberos", state.GetSelectedItemOpt()?.DisplayText)
                Assert.Contains(String.Format("Numeros num = Nu{0}", c), state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(544296, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544296")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestVerbatimNamedIdentifierFiltering(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document>
class Program
{
    void Goo(int @int)
    {
        Goo($$
    }
}
                              </Document>)

                state.SendTypeChars("i")
                Await state.AssertCompletionSession()
                Assert.True(state.GetCompletionItems().Any(Function(i) i.DisplayText = "@int" AndAlso i.DisplayTextSuffix = ":"))
                state.SendTypeChars("n")
                Await state.WaitForAsynchronousOperationsAsync()
                Assert.True(state.GetCompletionItems().Any(Function(i) i.DisplayText = "@int" AndAlso i.DisplayTextSuffix = ":"))
                state.SendTypeChars("t")
                Await state.WaitForAsynchronousOperationsAsync()
                Assert.True(state.GetCompletionItems().Any(Function(i) i.DisplayText = "@int" AndAlso i.DisplayTextSuffix = ":"))
            End Using
        End Function

        <WorkItem(543687, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543687")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoPreselectInInvalidObjectCreationLocation(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document><![CDATA[
using System;

class Program
{
    void Test()
    {
        $$
    }
}

class Bar { }

class Goo<T> : IGoo<T>
{
}

interface IGoo<T>
{
}]]>
                              </Document>)

                state.SendTypeChars("IGoo<Bar> a = new ")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WorkItem(544925, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544925")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestQualifiedEnumSelection(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document>
using System;
 
class Program
{
    void Main()
    {
        Environment.GetFolderPath$$
    }
}
                              </Document>)

                state.SendTypeChars("(")
                state.SendTab()
                Await state.WaitForAsynchronousOperationsAsync()
                Assert.Contains("Environment.SpecialFolder", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(545070, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545070")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTextChangeSpanWithAtCharacter(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document>
public class @event
{
    $$@event()
    {
    }
}
                              </Document>)

                state.SendTypeChars("public ")
                Await state.AssertNoCompletionSession()
                Assert.Contains("public @event", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDoNotInsertColonSoThatUserCanCompleteOutAVariableNameThatDoesNotCurrentlyExist_IE_TheCyrusCase(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document>
using System.Threading;

class Program
{
    static void Main(string[] args)
    {
        Goo($$)
    }

    void Goo(CancellationToken cancellationToken)
    {
    }
}
                              </Document>)

                state.SendTypeChars("can")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("Goo(cancellationToken)", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

#If False Then
    <Scenario Name="Verify correct intellisense selection on ENTER">
      <SetEditorText>
        <![CDATA[class Class1
{
    void Main(string[] args)
    {
        //
    }
}]]>
      </SetEditorText>
      <PlaceCursor Marker="//" />
      <SendKeys>var a = System.TimeSpan.FromMin{ENTER}{(}</SendKeys>
      <VerifyEditorContainsText>
        <![CDATA[class Class1
{
    void Main(string[] args)
    {
        var a = System.TimeSpan.FromMinutes(
    }
}]]>        
      </VerifyEditorContainsText>
    </Scenario>
#End If

        <WorkItem(544940, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544940")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AttributeNamedPropertyCompletionCommitWithTab(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                            <Document>
class MyAttribute : System.Attribute
{
    public string Name { get; set; }
}

[MyAttribute($$
public class Goo
{
}
                            </Document>)
                state.SendTypeChars("Nam")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Equal("[MyAttribute(Name =", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        <WorkItem(544940, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544940")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AttributeNamedPropertyCompletionCommitWithEquals(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                            <Document>
class MyAttribute : System.Attribute
{
    public string Name { get; set; }
}

[MyAttribute($$
public class Goo
{
}
                            </Document>)
                state.SendTypeChars("Nam=")
                Await state.AssertNoCompletionSession()
                Assert.Equal("[MyAttribute(Name =", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        <WorkItem(544940, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544940")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AttributeNamedPropertyCompletionCommitWithSpace(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                            <Document>
class MyAttribute : System.Attribute
{
    public string Name { get; set; }
}

[MyAttribute($$
public class Goo
{
}
                            </Document>)
                state.SendTypeChars("Nam ")
                Await state.AssertNoCompletionSession()
                Assert.Equal("[MyAttribute(Name ", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        <WorkItem(545590, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545590")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestOverrideDefaultParameter_CSharp7() As Task
            Using state = TestState.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    public virtual void Goo<S>(S x = default(S))
    {
    }
}

class D : C
{
    override $$
}
            ]]></Document>,
                   languageVersion:=LanguageVersion.CSharp7)
                state.SendTypeChars(" Goo")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("public override void Goo<S>(S x = default(S))", state.SubjectBuffer.CurrentSnapshot.GetText(), StringComparison.Ordinal)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestOverrideDefaultParameter(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
class C
{
    public virtual void Goo<S>(S x = default(S))
    {
    }
}

class D : C
{
    override $$
}
            ]]></Document>)
                state.SendTypeChars(" Goo")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("public override void Goo<S>(S x = default(S))", state.SubjectBuffer.CurrentSnapshot.GetText(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(545664, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545664")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestArrayAfterOptionalParameter(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
class A
{
    public virtual void Goo(int x = 0, int[] y = null) { }
}

class B : A
{
public override void Goo(int x = 0, params int[] y) { }
}

class C : B
{
    override$$
}
            ]]></Document>)
                state.SendTypeChars(" Goo")
                state.SendTab()
                Await state.AssertNoCompletionSession()
                Assert.Contains("    public override void Goo(int x = 0, int[] y = null)", state.SubjectBuffer.CurrentSnapshot.GetText(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(545967, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545967")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestVirtualSpaces(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
class C
{
    public string P { get; set; }
    void M()
    {
        var v = new C
        {$$
        };
    }
}
            ]]></Document>)
                state.SendReturn()
                Assert.True(state.TextView.Caret.InVirtualSpace)
                Assert.Equal(12, state.TextView.Caret.Position.VirtualSpaces)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("P", isSoftSelected:=True)
                state.SendDownKey()
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem("P", isHardSelected:=True)
                state.SendTab()
                Await state.WaitForAsynchronousOperationsAsync()
                Assert.Equal("            P", state.GetLineFromCurrentCaretPosition().GetText())

                Dim bufferPosition = state.TextView.Caret.Position.BufferPosition
                Assert.Equal(13, bufferPosition.Position - bufferPosition.GetContainingLine().Start.Position)
                Assert.False(state.TextView.Caret.InVirtualSpace)
            End Using
        End Function

        <WorkItem(546561, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546561")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNamedParameterAgainstMRU(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
class Program
{
    void Goo(string s) { }

    static void Main()
    {
        $$
    }
}
            ]]></Document>)
                ' prime the MRU
                state.SendTypeChars("string")
                state.SendTab()
                Await state.AssertNoCompletionSession()

                ' Delete what we just wrote.
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendEscape()
                Await state.AssertNoCompletionSession()

                ' ensure we still select the named param even though 'string' is in the MRU.
                state.SendTypeChars("Goo(s")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem("s", displayTextSuffix:=":")
            End Using
        End Function

        <WorkItem(546403, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546403")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMissingOnObjectCreationAfterVar1(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
class A
{
    void Goo()
    {
        var v = new$$
    }
}
            ]]></Document>)
                state.SendTypeChars(" ")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WorkItem(546403, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546403")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMissingOnObjectCreationAfterVar2(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
class A
{
    void Goo()
    {
        var v = new $$
    }
}
            ]]></Document>)
                state.SendTypeChars("X")
                Await state.AssertCompletionSession()
                Assert.False(state.GetCompletionItems().Any(Function(i) i.DisplayText = "X"))
            End Using
        End Function

        <WorkItem(546917, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546917")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEnumInSwitch(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
enum Numeros
{
}
class C
{
    void M()
    {
        Numeros n;
        switch (n)
        {
            case$$
        }
    }
}
            ]]></Document>)
                state.SendTypeChars(" ")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem(displayText:="Numeros")
            End Using
        End Function

        <WorkItem(547016, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547016")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAmbiguityInLocalDeclaration(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
class C
{
    public int W;
    public C()
    {
        $$
        W = 0;
    }
}

            ]]></Document>)
                state.SendTypeChars("w")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem(displayText:="W")
            End Using
        End Function

        <WorkItem(530835, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530835")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCompletionFilterSpanCaretBoundary(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
class C
{
    public void Method()
    {
        $$
    }
}
            ]]></Document>)
                state.SendTypeChars("Met")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem(displayText:="Method")
                state.SendLeftKey()
                state.SendLeftKey()
                state.SendLeftKey()
                state.SendTypeChars("new")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem(displayText:="Method", isSoftSelected:=True)
            End Using
        End Function

        <WorkItem(5487, "https://github.com/dotnet/roslyn/issues/5487")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitCharTypedAtTheBeginingOfTheFilterSpan(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
class C
{
    public bool Method()
    {
        if ($$
    }
}
            ]]></Document>)

                state.SendTypeChars("Met")
                Await state.AssertCompletionSession()
                state.SendLeftKey()
                state.SendLeftKey()
                state.SendLeftKey()
                Await state.AssertSelectedCompletionItem(isSoftSelected:=True)
                state.SendTypeChars("!")
                Await state.AssertNoCompletionSession()
                Await state.WaitForAsynchronousOperationsAsync()
                Assert.Equal("if (!Met", state.GetLineTextFromCaretPosition().Trim())
                Assert.Equal("M", state.GetCaretPoint().BufferPosition.GetChar())
            End Using
        End Function

        <WorkItem(622957, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/622957")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBangFiltersInDocComment(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
using System;

/// $$
/// TestDocComment
/// </summary>
class TestException : Exception { }
]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSession()
                state.SendTypeChars("!")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("!--")
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeCompletionDoesNotFilter(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
using System;
class C
{
    public void Method()
    {
        string$$
    }
}
            ]]></Document>)
                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem("string")
                state.CompletionItemsContainsAll({"integer", "Method"})
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeBeforeWordDoesNotSelect(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
using System;
class C
{
    public void Method()
    {
        $$string
    }
}
            ]]></Document>)
                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem("AccessViolationException")
                state.CompletionItemsContainsAll({"integer", "Method"})
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeCompletionSelectsWithoutRegardToCaretPosition(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
using System;
class C
{
    public void Method()
    {
        s$$tring
    }
}
            ]]></Document>)
                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem("string")
                state.CompletionItemsContainsAll({"integer", "Method"})
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TabAfterQuestionMark(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
using System;
class C
{
    public void Method()
    {
        ?$$
    }
}
            ]]></Document>)
                state.SendTab()
                Await state.WaitForAsynchronousOperationsAsync()
                Assert.Equal(state.GetLineTextFromCaretPosition(), "        ?" + vbTab)
            End Using
        End Function

        <WorkItem(657658, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/657658")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function PreselectionIgnoresBrackets(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                  <Document><![CDATA[
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
 
class Program
{
    $$
 
    static void Main(string[] args)
    {
      
    }
}]]></Document>)

                state.SendTypeChars("static void F<T>(int a, Func<T, int> b) { }")
                state.SendEscape()

                state.TextView.Caret.MoveTo(New VisualStudio.Text.SnapshotPoint(state.SubjectBuffer.CurrentSnapshot, 220))

                state.SendTypeChars("F")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("F", displayTextSuffix:="<>")
            End Using
        End Function

        <WorkItem(672474, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/672474")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInvokeSnippetCommandDismissesCompletion(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document>$$</Document>)

                state.SendTypeChars("us")
                Await state.AssertCompletionSession()
                state.SendInsertSnippetCommand()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WorkItem(672474, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/672474")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSurroundWithCommandDismissesCompletion(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document>$$</Document>)

                state.SendTypeChars("us")
                Await state.AssertCompletionSession()
                state.SendSurroundWithCommand()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WorkItem(737239, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/737239")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function LetEditorHandleOpenParen(completionImplementation As CompletionImplementation) As Task
            Dim expected = <Document><![CDATA[
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        List<int> x = new List<int>(
    }
}]]></Document>.Value.Replace(vbLf, vbCrLf)

            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation, <Document><![CDATA[
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        List<int> x = new$$
    }
}]]></Document>)


                state.SendTypeChars(" ")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("List<int>")
                state.SendTypeChars("(")
                Await state.WaitForAsynchronousOperationsAsync()
                Assert.Equal(expected, state.GetDocumentText())
            End Using
        End Function

        <WorkItem(785637, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/785637")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitMovesCaretToWordEnd(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
using System;
class C
{
    public void Main()
    {
        M$$ain
    }
}
            ]]></Document>)
                state.SendCommitUniqueCompletionListItem()
                Await state.WaitForAsynchronousOperationsAsync()
                Assert.Equal(state.GetLineFromCurrentCaretPosition().End, state.GetCaretPoint().BufferPosition)
            End Using
        End Function

        <WorkItem(775370, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/775370")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function MatchingConsidersAtSign(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
using System;
class C
{
    public void Main()
    {
        $$
    }
}
            ]]></Document>)
                state.SendTypeChars("var @this = ""goo""")
                state.SendReturn()
                state.SendTypeChars("string str = this.ToString();")
                state.SendReturn()
                state.SendTypeChars("str = @th")

                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem("@this")
            End Using
        End Function

        <WorkItem(865089, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/865089")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AttributeFilterTextRemovesAttributeSuffix(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
[$$]
class AtAttribute : System.Attribute { }]]></Document>)
                state.SendTypeChars("At")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem("At")
                Assert.Equal("At", state.GetSelectedItem().FilterText)
            End Using
        End Function

        <WorkItem(852578, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/852578")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function PreselectExceptionOverSnippet(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
using System;
class C
{
    Exception goo() {
        return new $$
    }
}]]></Document>)
                state.SendTypeChars(" ")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem("Exception")
            End Using
        End Function

        <WorkItem(868286, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/868286")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitNameAfterAlias(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
using goo = System$$]]></Document>)
                state.SendTypeChars(".act<")
                Await state.WaitForAsynchronousOperationsAsync()
                state.AssertMatchesTextStartingAtLine(1, "using goo = System.Action<")
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCompletionInLinkedFiles(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(completionImplementation,
                <Workspace>
                    <Project Language="C#" CommonReferences="true" AssemblyName="CSProj" PreprocessorSymbols="Thing2">
                        <Document FilePath="C.cs">
class C
{
    void M()
    {
        $$
    }

#if Thing1
    void Thing1() { }
#elif Thing2
    void Thing2() { }
#endif
}
                              </Document>
                    </Project>
                    <Project Language="C#" CommonReferences="true" PreprocessorSymbols="Thing1">
                        <Document IsLinkFile="true" LinkAssemblyName="CSProj" LinkFilePath="C.cs"/>
                    </Project>
                </Workspace>)

                Dim documents = state.Workspace.Documents
                Dim linkDocument = documents.Single(Function(d) d.IsLinkFile)
                state.SendTypeChars("Thing1")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem("Thing1")
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendEscape()
                state.Workspace.SetDocumentContext(linkDocument.Id)
                state.SendTypeChars("Thing1")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem("Thing1")
                Assert.True(state.CurrentCompletionPresenterSession.SelectedItem.Tags.Contains(WellKnownTags.Warning))
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendTypeChars("M")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem("M")
                Assert.False(state.CurrentCompletionPresenterSession.SelectedItem.Tags.Contains(WellKnownTags.Warning))
            End Using
        End Function

        <WorkItem(951726, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/951726")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function DismissUponSave(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
class C
{
    $$
}]]></Document>)
                state.SendTypeChars("voi")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem("void")
                state.SendSave()
                Await state.AssertNoCompletionSession()
                state.AssertMatchesTextStartingAtLine(3, "    voi")
            End Using
        End Function

        <WorkItem(930254, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/930254")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NoCompletionWithBoxSelection(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
class C
{
    {|Selection:$$int x;|}
    {|Selection:int y;|}
}]]></Document>)
                state.SendInvokeCompletionList()
                Await state.AssertNoCompletionSession()
                state.SendTypeChars("goo")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WorkItem(839555, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/839555")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TriggeredOnHash(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
$$]]></Document>)
                state.SendTypeChars("#")
                Await state.AssertCompletionSession()
            End Using
        End Function

        <WorkItem(771761, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/771761")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function RegionCompletionCommitTriggersFormatting_1(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
class C
{
    $$
}]]></Document>)
                state.SendTypeChars("#reg")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem("region")
                state.SendReturn()
                Await state.WaitForAsynchronousOperationsAsync()
                state.AssertMatchesTextStartingAtLine(3, "    #region")
            End Using
        End Function

        <WorkItem(771761, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/771761")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function RegionCompletionCommitTriggersFormatting_2(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
class C
{
    $$
}]]></Document>)
                state.SendTypeChars("#reg")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem("region")
                state.SendTypeChars(" ")
                Await state.WaitForAsynchronousOperationsAsync()
                state.AssertMatchesTextStartingAtLine(3, "    #region ")
            End Using
        End Function

        <WorkItem(771761, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/771761")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function EndRegionCompletionCommitTriggersFormatting_2(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
class C
{
    #region NameIt
    $$
}]]></Document>)
                state.SendTypeChars("#endreg")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem("endregion")
                state.SendReturn()
                Await state.WaitForAsynchronousOperationsAsync()
                state.AssertMatchesTextStartingAtLine(4, "    #endregion ")
            End Using
        End Function

        Private Class SlowProvider
            Inherits CommonCompletionProvider

            Public checkpoint As Checkpoint = New Checkpoint()

            Public Overrides Async Function ProvideCompletionsAsync(context As CompletionContext) As Task
                Await checkpoint.Task.ConfigureAwait(False)
            End Function

            Friend Overrides Function IsInsertionTrigger(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
                Return True
            End Function
        End Class

        <WorkItem(1015893, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1015893")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BackspaceDismissesIfComputationIsIncomplete(completionImplementation As CompletionImplementation) As Task
            Dim slowProvider = New SlowProvider()
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
class C
{
    void goo()
    {
        goo($$
    }
}]]></Document>, {slowProvider})

                state.SendTypeChars("f")
                state.SendBackspace()

                ' Send a backspace that goes beyond the session's applicable span
                ' before the model computation has finished. Then, allow the 
                ' computation to complete. There should still be no session.
                state.SendBackspace()
                slowProvider.checkpoint.Release()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WorkItem(1065600, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1065600")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitUniqueItemWithBoxSelection(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
class C
{
    void goo(int x)
    {
       [|$$ |]
    }
}]]></Document>)
                state.SendReturn()
                state.TextView.Selection.Mode = VisualStudio.Text.Editor.TextSelectionMode.Box
                state.SendCommitUniqueCompletionListItem()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WorkItem(1594, "https://github.com/dotnet/roslyn/issues/1594")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NoPreselectionOnSpaceWhenAbuttingWord(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
class Program
{
    void Main()
    {
        Program p = new $$Program();
    }
}]]></Document>)
                state.SendTypeChars(" ")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WorkItem(1594, "https://github.com/dotnet/roslyn/issues/1594")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SpacePreselectionAtEndOfFile(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
class Program
{
    void Main()
    {
        Program p = new $$]]></Document>)
                state.SendTypeChars(" ")
                Await state.AssertCompletionSession()
            End Using
        End Function

        <WorkItem(1659, "https://github.com/dotnet/roslyn/issues/1659")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function DismissOnSelectAllCommand(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
class C
{
    void goo(int x)
    {
        $$]]></Document>)
                ' Note: the caret is at the file, so the Select All command's movement
                ' of the caret to the end of the selection isn't responsible for 
                ' dismissing the session.
                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                state.SendSelectAll()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WorkItem(588, "https://github.com/dotnet/roslyn/issues/588")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionCommitAndFormatAreSeparateUndoTransactions(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
class C
{
    void goo(int x)
    {
        int doodle;
$$]]></Document>, extraExportedTypes:={GetType(CSharpEditorFormattingService)}.ToList())
                state.SendTypeChars("doo;")
                Await state.WaitForAsynchronousOperationsAsync()
                state.AssertMatchesTextStartingAtLine(6, "        doodle;")
                state.SendUndo()
                Await state.WaitForAsynchronousOperationsAsync()
                state.AssertMatchesTextStartingAtLine(6, "doo;")
            End Using
        End Function

        <WorkItem(4978, "https://github.com/dotnet/roslyn/issues/4978")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SessionNotStartedWhenCaretNotMappableIntoSubjectBuffer(completionImplementation As CompletionImplementation) As Task
            ' In inline diff view, typing delete next to a "deletion",
            ' can cause our CommandChain to be called with a subjectbuffer
            ' and TextView such that the textView's caret can't be mapped
            ' into our subject buffer. 
            '
            ' To test this, we create a projection buffer with 2 source 
            ' spans: one of "text" content type and one based on a C#
            ' buffer. We create a TextView with that projection as 
            ' its buffer, setting the caret such that it maps only
            ' into the "text" buffer. We then call the completionImplementation
            ' command handlers with commandargs based on that TextView
            ' but with the C# buffer as the SubjectBuffer.

            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
class C
{
    void goo(int x)
    {$$
        /********/
        int doodle;
        }
}]]></Document>, extraExportedTypes:={GetType(CSharpEditorFormattingService)}.ToList())

                Dim textBufferFactoryService = state.GetExportedValue(Of ITextBufferFactoryService)()
                Dim contentTypeService = state.GetExportedValue(Of IContentTypeRegistryService)()
                Dim contentType = contentTypeService.GetContentType(ContentTypeNames.CSharpContentType)
                Dim textViewFactory = state.GetExportedValue(Of ITextEditorFactoryService)()
                Dim editorOperationsFactory = state.GetExportedValue(Of IEditorOperationsFactoryService)()

                Dim otherBuffer = textBufferFactoryService.CreateTextBuffer("text", contentType)
                Dim otherExposedSpan = otherBuffer.CurrentSnapshot.CreateTrackingSpan(0, 4, SpanTrackingMode.EdgeExclusive, TrackingFidelityMode.Forward)

                Dim subjectBufferExposedSpan = state.SubjectBuffer.CurrentSnapshot.CreateTrackingSpan(0, state.SubjectBuffer.CurrentSnapshot.Length, SpanTrackingMode.EdgeExclusive, TrackingFidelityMode.Forward)

                Dim projectionBufferFactory = state.GetExportedValue(Of IProjectionBufferFactoryService)()
                Dim projection = projectionBufferFactory.CreateProjectionBuffer(Nothing, New Object() {otherExposedSpan, subjectBufferExposedSpan}.ToList(), ProjectionBufferOptions.None)

                Using disposableView As DisposableTextView = textViewFactory.CreateDisposableTextView(projection)
                    disposableView.TextView.Caret.MoveTo(New SnapshotPoint(disposableView.TextView.TextBuffer.CurrentSnapshot, 0))

                    Dim editorOperations = editorOperationsFactory.GetEditorOperations(disposableView.TextView)
                    state.GetCompletionCommandHandler.ExecuteCommand(New DeleteKeyCommandArgs(disposableView.TextView, state.SubjectBuffer), Sub() editorOperations.Delete(), TestCommandExecutionContext.Create())

                    Await state.AssertNoCompletionSession()
                End Using
            End Using
        End Function

        <WorkItem(588, "https://github.com/dotnet/roslyn/issues/588")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMatchWithTurkishIWorkaround1(completionImplementation As CompletionImplementation) As Task
            Using New CultureContext(New CultureInfo("tr-TR", useUserOverride:=False))
                Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                               <Document><![CDATA[
        class C
        {
            void goo(int x)
            {
                string.$$]]></Document>, extraExportedTypes:={GetType(CSharpEditorFormattingService)}.ToList())
                    state.SendTypeChars("is")
                    Await state.WaitForAsynchronousOperationsAsync()
                    Await state.AssertSelectedCompletionItem("IsInterned")
                End Using
            End Using

        End Function

        <WorkItem(588, "https://github.com/dotnet/roslyn/issues/588")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMatchWithTurkishIWorkaround2(completionImplementation As CompletionImplementation) As Task
            Using New CultureContext(New CultureInfo("tr-TR", useUserOverride:=False))
                Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                               <Document><![CDATA[
        class C
        {
            void goo(int x)
            {
                string.$$]]></Document>, extraExportedTypes:={GetType(CSharpEditorFormattingService)}.ToList())
                    state.SendTypeChars("ı")
                    Await state.WaitForAsynchronousOperationsAsync()
                    Await state.AssertSelectedCompletionItem()
                End Using
            End Using

        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TargetTypePreselection1(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                           <Document><![CDATA[
using System.Threading;
class Program
{
    void Cancel(int x, CancellationToken cancellationToken)
    {
        Cancel(x + 1, cancellationToken: $$)
    }
}]]></Document>, extraExportedTypes:={GetType(CSharpEditorFormattingService)}.ToList())
                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("cancellationToken", isHardSelected:=True).ConfigureAwait(True)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TargetTypePreselection2(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                           <Document><![CDATA[
class Program
{
    static void Main(string[] args)
    {
        int aaz = 0;
        args = $$
    }
}]]></Document>, extraExportedTypes:={GetType(CSharpEditorFormattingService)}.ToList())
                state.SendTypeChars("a")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("args", isHardSelected:=True).ConfigureAwait(True)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TargetTypePreselection_DoesNotOverrideEnumPreselection(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                           <Document><![CDATA[
enum E
{

}

class Program
{
    static void Main(string[] args)
    {
        E e;
        e = $$
    }
}]]></Document>, extraExportedTypes:={GetType(CSharpEditorFormattingService)}.ToList())
                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("E", isHardSelected:=True).ConfigureAwait(True)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TargetTypePreselection_DoesNotOverrideEnumPreselection2(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                           <Document><![CDATA[
enum E
{
    A
}

class Program
{
    static void Main(string[] args)
    {
        E e = E.A;
        if (e == $$
    }
}]]></Document>, extraExportedTypes:={GetType(CSharpEditorFormattingService)}.ToList())
                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("E", isHardSelected:=True).ConfigureAwait(True)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TargetTypePreselection3(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                           <Document><![CDATA[
class D {}

class Program
{
    static void Main(string[] args)
    {
       int cw = 7;
       D cx = new D();
       D cx2 = $$
    }
}]]></Document>, extraExportedTypes:={GetType(CSharpEditorFormattingService)}.ToList())
                state.SendTypeChars("c")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("cx", isHardSelected:=True).ConfigureAwait(True)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TargetTypePreselectionLocalsOverType(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                           <Document><![CDATA[
class A {}

class Program
{
    static void Main(string[] args)
    {
       A cx = new A();
       A cx2 = $$
    }
}]]></Document>, extraExportedTypes:={GetType(CSharpEditorFormattingService)}.ToList())
                state.SendTypeChars("c")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("cx", isHardSelected:=True).ConfigureAwait(True)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TargetTypePreselectionParameterOverMethod(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                           <Document><![CDATA[
class Program
{
    bool f;

    void goo(bool x) { }

    void Main(string[] args) 
    {
        goo($$) // Not "Equals"
    }
}]]></Document>, extraExportedTypes:={GetType(CSharpEditorFormattingService)}.ToList())
                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("f", isHardSelected:=True).ConfigureAwait(True)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/6942"), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TargetTypePreselectionConvertibility1(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                           <Document><![CDATA[
abstract class C {}
class D : C {}
class Program
{
    static void Main(string[] args)
    {
       D cx = new D();
       C cx2 = $$
    }
}]]></Document>, extraExportedTypes:={GetType(CSharpEditorFormattingService)}.ToList())
                state.SendTypeChars("c")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("cx", isHardSelected:=True).ConfigureAwait(True)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TargetTypePreselectionLocalOverProperty(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                           <Document><![CDATA[
class Program
{
    public int aaa { get; }

     void Main(string[] args)
    {
        int aaq;

        int y = a$$
    }
}]]></Document>, extraExportedTypes:={GetType(CSharpEditorFormattingService)}.ToList())
                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("aaq", isHardSelected:=True).ConfigureAwait(True)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(12254, "https://github.com/dotnet/roslyn/issues/12254")>
        Public Async Function TestGenericCallOnTypeContainingAnonymousType(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                           <Document><![CDATA[
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        new[] { new { x = 1 } }.ToArr$$
    }
}]]></Document>, extraExportedTypes:={GetType(CSharpEditorFormattingService)}.ToList())

                state.SendInvokeCompletionList()
                state.SendTypeChars("(")

                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                state.AssertMatchesTextStartingAtLine(7, "new[] { new { x = 1 } }.ToArray(")
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TargetTypePreselectionSetterValuey(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                           <Document><![CDATA[
class Program
{
    int _x;
    int X
    {
        set
        {
            _x = $$
        }
    }
}]]></Document>, extraExportedTypes:={GetType(CSharpEditorFormattingService)}.ToList())
                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("value", isHardSelected:=True).ConfigureAwait(True)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(12530, "https://github.com/dotnet/roslyn/issues/12530")>
        Public Async Function TestAnonymousTypeDescription(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                           <Document><![CDATA[
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        new[] { new { x = 1 } }.ToArr$$
    }
}]]></Document>, extraExportedTypes:={GetType(CSharpEditorFormattingService)}.ToList())
                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem(description:=
$"({ CSharpFeaturesResources.extension }) 'a[] System.Collections.Generic.IEnumerable<'a>.ToArray<'a>()

{ FeaturesResources.Anonymous_Types_colon }
    'a { FeaturesResources.is_ } new {{ int x }}")
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestRecursiveGenericSymbolKey(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                           <Document><![CDATA[
using System.Collections.Generic;

class Program
{
    static void ReplaceInList<T>(List<T> list, T oldItem, T newItem)
    {
        $$
    }
}]]></Document>, extraExportedTypes:={GetType(CSharpEditorFormattingService)}.ToList())

                state.SendTypeChars("list")
                state.SendTypeChars(".")
                Await state.AssertCompletionSession()
                state.SendTypeChars("Add")

                Await state.AssertSelectedCompletionItem("Add", description:="void List<T>.Add(T item)")
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitNamedParameterWithColon(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                           <Document><![CDATA[
using System.Collections.Generic;

class Program
{
    static void Main(int args)
    {
        Main(args$$
    }
}]]></Document>, extraExportedTypes:={GetType(CSharpEditorFormattingService)}.ToList())

                state.SendInvokeCompletionList()
                state.SendTypeChars(":")
                Await state.AssertNoCompletionSession()
                Assert.Contains("args:", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        <WorkItem(13481, "https://github.com/dotnet/roslyn/issues/13481")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBackspaceSelection1(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
using System;

class Program
{
    static void Main()
    {
        DateTimeOffset$$
    }
}
            ]]></Document>)
                state.Workspace.Options = state.Workspace.Options.WithChangedOption(
                    CompletionOptions.TriggerOnDeletion, LanguageNames.CSharp, True)

                For Each c In "Offset"
                    state.SendBackspace()
                    Await state.WaitForAsynchronousOperationsAsync()
                Next

                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("DateTime")
            End Using
        End Function

        <WorkItem(13481, "https://github.com/dotnet/roslyn/issues/13481")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBackspaceSelection2(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
using System;

class Program
{
    static void Main()
    {
        DateTimeOffset.$$
    }
}
            ]]></Document>)
                state.Workspace.Options = state.Workspace.Options.WithChangedOption(
                    CompletionOptions.TriggerOnDeletion, LanguageNames.CSharp, True)

                For Each c In "Offset."
                    state.SendBackspace()
                    Await state.WaitForAsynchronousOperationsAsync()
                Next

                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("DateTime")
            End Using
        End Function

        <WorkItem(14465, "https://github.com/dotnet/roslyn/issues/14465")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TypingNumberShouldNotDismiss1(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
class C
{
    void Moo1()
    {
        new C()$$
    }
}
            ]]></Document>)

                state.SendTypeChars(".")
                Await state.AssertCompletionSession()
                state.SendTypeChars("1")
                Await state.AssertSelectedCompletionItem("Moo1")
            End Using
        End Function

        <WorkItem(14085, "https://github.com/dotnet/roslyn/issues/14085")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TargetTypingDoesNotOverrideExactMatch(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
using System.IO;
class C
{
    void Moo1()
    {
        string path = $$
    }
}
            ]]></Document>)

                state.SendTypeChars("Path")
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("Path")
            End Using
        End Function

        <WorkItem(14085, "https://github.com/dotnet/roslyn/issues/14085")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function MRUOverTargetTyping(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
using System.IO;
using System.Threading.Tasks;
class C
{
    async Task Moo()
    {
        await Moo().$$
    }
}
            ]]></Document>)

                state.SendTypeChars("Configure")
                state.SendTab()
                For i = 1 To "ConfigureAwait".Length
                    state.SendBackspace()
                Next
                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem("ConfigureAwait")
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function MovingCaretToStartSoftSelects(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document>
using System;

class C
{
    void M()
    {
        $$
    }
}
                              </Document>)

                state.SendTypeChars("Conso")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertSelectedCompletionItem(displayText:="Console", isHardSelected:=True)
                For Each ch In "Conso"
                    state.SendLeftKey()
                Next

                Await state.AssertSelectedCompletionItem(displayText:="Console", isHardSelected:=False)

                state.SendRightKey()
                Await state.AssertSelectedCompletionItem(displayText:="Console", isHardSelected:=True)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoBlockOnCompletionItems1(completionImplementation As CompletionImplementation) As Task
            Dim tcs = New TaskCompletionSource(Of Boolean)
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document>
                                  using $$
                              </Document>, {New TaskControlledCompletionProvider(tcs.Task)})

                state.Workspace.Options = state.Workspace.Options.WithChangedOption(
                    CompletionOptions.BlockForCompletionItems, LanguageNames.CSharp, False)

                state.SendTypeChars("Sys.")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertNoCompletionSession()
                Assert.Contains("Sys.", state.GetLineTextFromCaretPosition())

                tcs.SetResult(True)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoBlockOnCompletionItems2(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document>
                                  using $$
                              </Document>, {New TaskControlledCompletionProvider(Task.FromResult(True))})

                state.Workspace.Options = state.Workspace.Options.WithChangedOption(
                    CompletionOptions.BlockForCompletionItems, LanguageNames.CSharp, False)

                state.SendTypeChars("Sys")
                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertCompletionSession()
                state.SendTypeChars(".")
                Assert.Contains("System.", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoBlockOnCompletionItems4(completionImplementation As CompletionImplementation) As Task
            Dim tcs = New TaskCompletionSource(Of Boolean)
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document>
                                  using $$
                              </Document>, {New TaskControlledCompletionProvider(tcs.Task)})

                state.Workspace.Options = state.Workspace.Options.WithChangedOption(
                    CompletionOptions.BlockForCompletionItems, LanguageNames.CSharp, False)

                state.SendTypeChars("Sys")
                state.SendCommitUniqueCompletionListItem()
                Await Task.Delay(250)
                Await state.AssertNoCompletionSession(block:=False)
                Assert.Contains("Sys", state.GetLineTextFromCaretPosition())
                Assert.DoesNotContain("System", state.GetLineTextFromCaretPosition())

                tcs.SetResult(True)

                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertNoCompletionSession()
                Assert.Contains("System", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoBlockOnCompletionItems3(completionImplementation As CompletionImplementation) As Task
            Dim tcs = New TaskCompletionSource(Of Boolean)
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document>
                                  using $$
                              </Document>, {New TaskControlledCompletionProvider(tcs.Task)})

                state.Workspace.Options = state.Workspace.Options.WithChangedOption(
                    CompletionOptions.BlockForCompletionItems, LanguageNames.CSharp, False)

                state.SendTypeChars("Sys")
                state.SendCommitUniqueCompletionListItem()
                Await Task.Delay(250)
                Await state.AssertNoCompletionSession(block:=False)
                Assert.Contains("Sys", state.GetLineTextFromCaretPosition())
                Assert.DoesNotContain("System", state.GetLineTextFromCaretPosition())

                state.SendTypeChars("a")

                tcs.SetResult(True)

                Await state.WaitForAsynchronousOperationsAsync()
                Await state.AssertCompletionSession()
                Assert.Contains("Sysa", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        Private Class TaskControlledCompletionProvider
            Inherits CompletionProvider

            Private ReadOnly _task As Task

            Public Sub New(task As Task)
                _task = task
            End Sub

            Public Overrides Function ProvideCompletionsAsync(context As CompletionContext) As Task
                Return _task
            End Function
        End Class

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function Filters_EmptyList1(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
using System.IO;
using System.Threading.Tasks;
class C
{
    async Task Moo()
    {
        var x = asd$$
    }
}
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync()
                Dim filters = state.GetCompletionItemFilters()
                Dim dict = New Dictionary(Of CompletionItemFilter, Boolean)
                For Each f In filters
                    dict(f) = False
                Next

                dict(CompletionItemFilter.InterfaceFilter) = True

                Dim args = New CompletionItemFilterStateChangedEventArgs(dict.ToImmutableDictionary())
                state.RaiseFiltersChanged(args)
                Await state.WaitForAsynchronousOperationsAsync()
                Assert.Null(state.GetSelectedItem())

            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function Filters_EmptyList2(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
using System.IO;
using System.Threading.Tasks;
class C
{
    async Task Moo()
    {
        var x = asd$$
    }
}
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync()
                Dim filters = state.GetCompletionItemFilters()
                Dim dict = New Dictionary(Of CompletionItemFilter, Boolean)
                For Each f In filters
                    dict(f) = False
                Next

                dict(CompletionItemFilter.InterfaceFilter) = True

                Dim args = New CompletionItemFilterStateChangedEventArgs(dict.ToImmutableDictionary())
                state.RaiseFiltersChanged(args)
                Await state.WaitForAsynchronousOperationsAsync()
                Assert.Null(state.GetSelectedItem())
                state.SendTab()
                Await state.AssertNoCompletionSession()

            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function Filters_EmptyList3(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
using System.IO;
using System.Threading.Tasks;
class C
{
    async Task Moo()
    {
        var x = asd$$
    }
}
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync()
                Dim filters = state.GetCompletionItemFilters()
                Dim dict = New Dictionary(Of CompletionItemFilter, Boolean)
                For Each f In filters
                    dict(f) = False
                Next

                dict(CompletionItemFilter.InterfaceFilter) = True

                Dim args = New CompletionItemFilterStateChangedEventArgs(dict.ToImmutableDictionary())
                state.RaiseFiltersChanged(args)
                Await state.WaitForAsynchronousOperationsAsync()
                Assert.Null(state.GetSelectedItem())
                state.SendReturn()
                Await state.AssertNoCompletionSession()

            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function Filters_EmptyList4(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
using System.IO;
using System.Threading.Tasks;
class C
{
    async Task Moo()
    {
        var x = asd$$
    }
}
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync()
                Dim filters = state.GetCompletionItemFilters()
                Dim dict = New Dictionary(Of CompletionItemFilter, Boolean)
                For Each f In filters
                    dict(f) = False
                Next

                dict(CompletionItemFilter.InterfaceFilter) = True

                Dim args = New CompletionItemFilterStateChangedEventArgs(dict.ToImmutableDictionary())
                state.RaiseFiltersChanged(args)
                Await state.WaitForAsynchronousOperationsAsync()
                Assert.Null(state.GetSelectedItem())
                state.SendTypeChars(".")
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(15881, "https://github.com/dotnet/roslyn/issues/15881")>
        Public Async Function CompletionAfterDotBeforeAwaitTask(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
using System.Threading.Tasks;

class C
{
    async Task Moo()
    {
        Task.$$
        await Task.Delay(50);
    }
}
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
            End Using
        End Function

        <WorkItem(14704, "https://github.com/dotnet/roslyn/issues/14704")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BackspaceTriggerSubstringMatching(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document>
using System;
class Program
{
    static void Main(string[] args)
    {
        if (Environment$$
    }
}
                              </Document>)

                Dim key = New OptionKey(CompletionOptions.TriggerOnDeletion, LanguageNames.CSharp)
                state.Workspace.Options = state.Workspace.Options.WithChangedOption(key, True)

                state.SendBackspace()
                Await state.AssertSelectedCompletionItem(displayText:="Environment", isHardSelected:=True)
            End Using
        End Function

        <WorkItem(16236, "https://github.com/dotnet/roslyn/issues/16236")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AttributeNamedParameterEqualsItemCommittedOnSpace(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document>
[A($$)]
class AAttribute: Attribute
{
    public string Skip { get; set; }
} </Document>)
                state.SendTypeChars("Skip")
                Await state.AssertCompletionSession()
                state.SendTypeChars(" ")
                Await state.AssertNoCompletionSession()
                Assert.Equal("[A(Skip )]", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        <WorkItem(362890, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=362890")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestFilteringAfterSimpleInvokeShowsAllItemsMatchingFilter(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[

enum Color
{
    Red,
    Green,
    Blue
}

class C
{
    void M()
    {
        Color.Re$$d
    }
}
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("Red")
                state.CompletionItemsContainsAll(displayText:={"Red", "Green", "Blue", "Equals"})

                Dim filters = state.GetCompletionItemFilters()
                Dim dict = New Dictionary(Of CompletionItemFilter, Boolean)
                For Each f In filters
                    dict(f) = False
                Next

                dict(CompletionItemFilter.EnumFilter) = True

                Dim args = New CompletionItemFilterStateChangedEventArgs(dict.ToImmutableDictionary())
                state.RaiseFiltersChanged(args)
                Await state.AssertSelectedCompletionItem("Red")
                state.CompletionItemsContainsAll(displayText:={"Red", "Green", "Blue"})
                Assert.False(state.GetCompletionItems().Any(Function(i) i.DisplayText = "Equals"))

                For Each f In filters
                    dict(f) = False
                Next

                args = New CompletionItemFilterStateChangedEventArgs(dict.ToImmutableDictionary())
                state.RaiseFiltersChanged(args)
                Await state.AssertSelectedCompletionItem("Red")
                state.CompletionItemsContainsAll(displayText:={"Red", "Green", "Blue", "Equals"})

            End Using
        End Function

        <WorkItem(16236, "https://github.com/dotnet/roslyn/issues/16236")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NameCompletionSorting(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document>
interface ISyntaxFactsService {}
class C
{
    void M()
    {
        ISyntaxFactsService $$
    }
} </Document>)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()

                Dim expectedOrder =
                    {
                        "syntaxFactsService",
                        "syntaxFacts",
                        "factsService",
                        "syntax",
                        "service"
                    }

                state.AssertItemsInOrder(expectedOrder)
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestLargeChangeBrokenUpIntoSmallTextChanges(completionImplementation As CompletionImplementation)
            Dim provider = New MultipleChangeCompletionProvider()

            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
using System;
class C
{
    void goo() {
        return $$
    }
}]]></Document>, {provider})

                Dim testDocument = state.Workspace.Documents(0)
                Dim textBuffer = testDocument.TextBuffer

                Dim snapshotBeforeCommit = textBuffer.CurrentSnapshot
                provider.SetInfo(snapshotBeforeCommit.GetText(), testDocument.CursorPosition.Value)

                ' First send a space to trigger out special completionImplementation provider.
                state.SendInvokeCompletionList()
                state.SendTab()

                ' Verify that we see the entire change
                Dim finalText = textBuffer.CurrentSnapshot.GetText()
                Assert.Equal(
"using NewUsing;
using System;
class C
{
    void goo() {
        return InsertedItem
    }
}", finalText)

                ' This should have happened as two text changes to the buffer.
                Dim changes = snapshotBeforeCommit.Version.Changes
                Assert.Equal(2, changes.Count)

                Dim actualChanges = changes.ToArray()
                Dim firstChange = actualChanges(0)
                Assert.Equal(New Span(0, 0), firstChange.OldSpan)
                Assert.Equal("using NewUsing;", firstChange.NewText)

                Dim secondChange = actualChanges(1)
                Assert.Equal(New Span(testDocument.CursorPosition.Value, 0), secondChange.OldSpan)
                Assert.Equal("InsertedItem", secondChange.NewText)

                ' Make sure new edits happen after the text that was inserted.
                state.SendTypeChars("1")

                finalText = textBuffer.CurrentSnapshot.GetText()
                Assert.Equal(
"using NewUsing;
using System;
class C
{
    void goo() {
        return InsertedItem1
    }
}", finalText)
            End Using
        End Sub

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestLargeChangeBrokenUpIntoSmallTextChanges2(completionImplementation As CompletionImplementation)
            Dim provider = New MultipleChangeCompletionProvider()

            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                <Document><![CDATA[
using System;
class C
{
    void goo() {
        return Custom$$
    }
}]]></Document>, {provider})

                Dim testDocument = state.Workspace.Documents(0)
                Dim textBuffer = testDocument.TextBuffer

                Dim snapshotBeforeCommit = textBuffer.CurrentSnapshot
                provider.SetInfo(snapshotBeforeCommit.GetText(), testDocument.CursorPosition.Value)

                ' First send a space to trigger out special completionImplementation provider.
                state.SendInvokeCompletionList()
                state.SendTab()

                ' Verify that we see the entire change
                Dim finalText = textBuffer.CurrentSnapshot.GetText()
                Assert.Equal(
"using NewUsing;
using System;
class C
{
    void goo() {
        return InsertedItem
    }
}", finalText)

                ' This should have happened as two text changes to the buffer.
                Dim changes = snapshotBeforeCommit.Version.Changes
                Assert.Equal(2, changes.Count)

                Dim actualChanges = changes.ToArray()
                Dim firstChange = actualChanges(0)
                Assert.Equal(New Span(0, 0), firstChange.OldSpan)
                Assert.Equal("using NewUsing;", firstChange.NewText)

                Dim secondChange = actualChanges(1)
                Assert.Equal(New Span(testDocument.CursorPosition.Value - "Custom".Length, "Custom".Length), secondChange.OldSpan)
                Assert.Equal("InsertedItem", secondChange.NewText)

                ' Make sure new edits happen after the text that was inserted.
                state.SendTypeChars("1")

                finalText = textBuffer.CurrentSnapshot.GetText()
                Assert.Equal(
"using NewUsing;
using System;
class C
{
    void goo() {
        return InsertedItem1
    }
}", finalText)
            End Using
        End Sub

        <WorkItem(296512, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=296512")>
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestRegionDirectiveIndentation(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document>
class C
{
    $$
}
                              </Document>, includeFormatCommandHandler:=True)

                state.SendTypeChars("#")
                Await state.WaitForAsynchronousOperationsAsync()

                Assert.Equal("#", state.GetLineFromCurrentCaretPosition().GetText())
                Await state.AssertNoCompletionSession()
                state.SendTypeChars("reg")
                Await state.AssertSelectedCompletionItem(displayText:="region")
                state.SendReturn()
                Await state.AssertNoCompletionSession()
                Assert.Equal("    #region", state.GetLineFromCurrentCaretPosition().GetText())
                Assert.Equal(state.GetLineFromCurrentCaretPosition().End, state.GetCaretPoint().BufferPosition)

                state.SendReturn()
                Assert.Equal("", state.GetLineFromCurrentCaretPosition().GetText())
                state.SendTypeChars("#")
                Await state.WaitForAsynchronousOperationsAsync()

                Assert.Equal("#", state.GetLineFromCurrentCaretPosition().GetText())
                Await state.AssertNoCompletionSession()
                state.SendTypeChars("endr")
                Await state.AssertSelectedCompletionItem(displayText:="endregion")
                state.SendReturn()
                Await state.AssertNoCompletionSession()
                Assert.Equal("    #endregion", state.GetLineFromCurrentCaretPosition().GetText())
                Assert.Equal(state.GetLineFromCurrentCaretPosition().End, state.GetCaretPoint().BufferPosition)

            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AfterIdentifierInCaseLabel(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document>
class C
{
    void M()
    {
        switch (true)
        {
            case identifier $$
        }
    }
}
                              </Document>)

                state.SendTypeChars("w")
                Await state.AssertSelectedCompletionItem(displayText:="when", isHardSelected:=False)

                state.SendBackspace()
                state.SendTypeChars("i")
                Await state.AssertSelectedCompletionItem(displayText:="identifier", isHardSelected:=False)

            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AfterIdentifierInCaseLabel_ColorColor(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document>
class identifier { }
class C
{
    const identifier identifier = null;
    void M()
    {
        switch (true)
        {
            case identifier $$
        }
    }
}
                              </Document>)

                state.SendTypeChars("w")
                Await state.AssertSelectedCompletionItem(displayText:="when", isHardSelected:=False)

                state.SendBackspace()
                state.SendTypeChars("i")
                Await state.AssertSelectedCompletionItem(displayText:="identifier", isHardSelected:=False)

            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AfterIdentifierInCaseLabel_ClassNameOnly(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document>
class identifier { }
class C
{
    void M()
    {
        switch (true)
        {
            case identifier $$
        }
    }
}
                              </Document>)

                state.SendTypeChars("w")
                Await state.AssertSelectedCompletionItem(displayText:="identifier", isHardSelected:=False)

                state.SendBackspace()
                state.SendTypeChars("i")
                Await state.AssertSelectedCompletionItem(displayText:="identifier", isHardSelected:=False)

            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AfterDoubleIdentifierInCaseLabel(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document>
class C
{
    void M()
    {
        switch (true)
        {
            case identifier identifier $$
        }
    }
}
                              </Document>)

                state.SendTypeChars("w")
                Await state.AssertSelectedCompletionItem(displayText:="when", isHardSelected:=True)

            End Using
        End Function

        Private Class MultipleChangeCompletionProvider
            Inherits CompletionProvider

            Private _text As String
            Private _caretPosition As Integer

            Public Sub SetInfo(text As String, caretPosition As Integer)
                _text = text
                _caretPosition = caretPosition
            End Sub

            Public Overrides Function ProvideCompletionsAsync(context As CompletionContext) As Task
                context.AddItem(CompletionItem.Create(
                    "CustomItem",
                    rules:=CompletionItemRules.Default.WithMatchPriority(1000)))
                Return Task.CompletedTask
            End Function

            Public Overrides Function ShouldTriggerCompletion(text As SourceText, caretPosition As Integer, trigger As CompletionTrigger, options As OptionSet) As Boolean
                Return True
            End Function

            Public Overrides Function GetChangeAsync(document As Document, item As CompletionItem, commitKey As Char?, cancellationToken As CancellationToken) As Task(Of CompletionChange)
                Dim newText =
"using NewUsing;
using System;
class C
{
    void goo() {
        return InsertedItem"

                Dim change = CompletionChange.Create(
                    New TextChange(New TextSpan(0, _caretPosition), newText))
                Return Task.FromResult(change)
            End Function

            <WorkItem(15348, "https://github.com/dotnet/roslyn/issues/15348")>
            <MemberData(NameOf(AllCompletionImplementations))>
            <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
            Public Async Function TestAfterCasePatternSwitchLabel(completionImplementation As CompletionImplementation) As Task
                Using state = TestStateFactory.CreateCSharpTestState(completionImplementation,
                              <Document>
class C
{
    void M()
    {
        object o = 1;
        switch(o)
        {
            case int i:
                $$
                break;
        }
    }
}
                              </Document>)

                    state.SendTypeChars("this")
                    Await state.AssertSelectedCompletionItem(displayText:="this", isHardSelected:=True)
                End Using
            End Function
        End Class
    End Class
End Namespace
