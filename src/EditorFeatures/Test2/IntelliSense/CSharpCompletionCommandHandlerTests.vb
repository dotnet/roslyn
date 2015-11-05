' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.Commands
Imports Microsoft.CodeAnalysis.Editor.CSharp.Formatting
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Differencing
Imports Microsoft.VisualStudio.Text.Editor
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Text.Projection
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Public Class CSharpCompletionCommandHandlerTests
        <WorkItem(541201)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TabCommitsWithoutAUniqueMatch() As Task
            Using state = TestState.CreateCSharpTestState(
                              <Document>
                                  $$
                              </Document>)

                state.SendTypeChars("using System.Ne")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="Net", isHardSelected:=True).ConfigureAwait(True)
                state.SendTypeChars("x")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="Net", isSoftSelected:=True).ConfigureAwait(True)
                state.SendTab()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                Assert.Contains("using System.Net", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAtEndOfFile() As Task
            Using state = TestState.CreateCSharpTestState(
                              <Document>$$</Document>)

                state.SendTypeChars("us")
                state.SendTab()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                Assert.Contains("using", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotAtStartOfExistingWord() As Task
            Using state = TestState.CreateCSharpTestState(
                              <Document>$$using</Document>)

                state.SendTypeChars("u")
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                Assert.Contains("using", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMSCorLibTypes() As Task
            Using state = TestState.CreateCSharpTestState(
                              <Document>
using System;

class c : $$
                              </Document>)

                state.SendTypeChars("A")
                Await state.AssertCompletionSession().ConfigureAwait(True)

                Assert.True(state.CompletionItemsContainsAll(displayText:={"Attribute", "Exception", "IDisposable"}))
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestFiltering1() As Task
            Using state = TestState.CreateCSharpTestState(
                              <Document>
using System;
      
class c { $$
                              </Document>)

                state.SendTypeChars("Sy")
                Await state.AssertCompletionSession().ConfigureAwait(True)

                Assert.True(state.CompletionItemsContainsAll(displayText:={"OperatingSystem", "System", "SystemException"}))
                Assert.False(state.CompletionItemsContainsAny(displayText:={"Exception", "Activator"}))
            End Using
        End Function

        ' NOTE(cyrusn): This should just be a unit test for SymbolCompletionProvider.  However, I'm
        ' just porting the integration tests to here for now.
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMultipleTypes() As Task
            Using state = TestState.CreateCSharpTestState(
                              <Document>
class C { $$ } struct S { } enum E { } interface I { } delegate void D();
                              </Document>)

                state.SendTypeChars("C")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Assert.True(state.CompletionItemsContainsAll(displayText:={"C", "S", "E", "I", "D"}))
            End Using
        End Function

        ' NOTE(cyrusn): This should just be a unit test for KeywordCompletionProvider.  However, I'm
        ' just porting the integration tests to here for now.
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInEmptyFile() As Task
            Using state = TestState.CreateCSharpTestState(
                              <Document>
$$
                              </Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Assert.True(state.CompletionItemsContainsAll(displayText:={"abstract", "class", "namespace"}))
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotAfterTypingDotAfterIntegerLiteral() As Task
            Using state = TestState.CreateCSharpTestState(
                              <Document>
class c { void M() { 3$$ } }
                              </Document>)

                state.SendTypeChars(".")
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAfterExplicitInvokeAfterDotAfterIntegerLiteral() As Task
            Using state = TestState.CreateCSharpTestState(
                              <Document>
class c { void M() { 3.$$ } }
                              </Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Assert.True(state.CompletionItemsContainsAll({"ToString"}))
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEnterIsConsumed() As Task
            Using state = TestState.CreateCSharpTestState(
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
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDescription1() As Task
            Using state = TestState.CreateCSharpTestState(
                  <Document><![CDATA[
using System;

/// <summary>
/// TestDocComment
/// </summary>
class TestException : Exception { }

class MyException : $$]]></Document>)

                state.SendTypeChars("Test")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(description:="class TestException" & vbCrLf & "TestDocComment").ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestObjectCreationPreselection1() As Task
            Using state = TestState.CreateCSharpTestState(
                  <Document><![CDATA[
using System.Collections.Generic;

class C
{
    public void Foo()
    {
        List<int> list = new$$
    }
}]]></Document>)

                state.SendTypeChars(" ")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="List<int>", isHardSelected:=True).ConfigureAwait(True)
                Assert.True(state.CompletionItemsContainsAll(displayText:={"LinkedList<>", "List<>", "System"}))
                state.SendTypeChars("Li")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="List<int>", isHardSelected:=True).ConfigureAwait(True)
                Assert.True(state.CompletionItemsContainsAll(displayText:={"LinkedList<>", "List<>"}))
                Assert.False(state.CompletionItemsContainsAny(displayText:={"System"}))
                state.SendTypeChars("n")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="LinkedList<>", isHardSelected:=True).ConfigureAwait(True)
                state.SendBackspace()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="List<int>", isHardSelected:=True).ConfigureAwait(True)
                state.SendTab()
                Assert.Contains("new List<int>", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(543268)>
        Public Async Function TestTypePreselection1() As Task
            Using state = TestState.CreateCSharpTestState(
                  <Document><![CDATA[
partial class C
{
}
partial class C
{
    $$
}]]></Document>)

                state.SendTypeChars("C")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="C", isHardSelected:=True).ConfigureAwait(True)
                state.SendTypeChars(" ")
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(543519)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNewPreselectionAfterVar() As Task
            Using state = TestState.CreateCSharpTestState(
                  <Document><![CDATA[
class C
{
    void M()
    {
        var c = $$
    }
}]]></Document>)

                state.SendTypeChars("new ")
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(543559)>
        <WorkItem(543561)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEscapedIdentifiers() As Task
            Using state = TestState.CreateCSharpTestState(
                  <Document><![CDATA[
class @return
{
    void foo()
    {
        $$
    }
}
]]></Document>)

                state.SendTypeChars("@")
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                state.SendTypeChars("r")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="@return", isHardSelected:=True).ConfigureAwait(True)
                state.SendTab()
                Assert.Contains("@return", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(543771)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitUniqueItem1() As Task
            Using state = TestState.CreateCSharpTestState(
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
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                Assert.Contains("WriteLine()", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(543771)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCommitUniqueItem2() As Task
            Using state = TestState.CreateCSharpTestState(
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
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitForUsingDirective1() As Task
            Using state = TestState.CreateCSharpTestState(
                              <Document>
                                  $$
                              </Document>)

                state.SendTypeChars("using Sys")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="System", isHardSelected:=True).ConfigureAwait(True)
                state.SendTypeChars("(")
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                Assert.Contains("using Sys(", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitForUsingDirective2() As Task
            Using state = TestState.CreateCSharpTestState(
                              <Document>
                                  $$
                              </Document>)

                state.SendTypeChars("using Sys")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="System", isHardSelected:=True).ConfigureAwait(True)
                state.SendTypeChars(".")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Assert.Contains("using System.", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitForUsingDirective3() As Task
            Using state = TestState.CreateCSharpTestState(
                              <Document>
                                  $$
                              </Document>, extraExportedTypes:={GetType(CSharpEditorFormattingService)}.ToList())

                state.SendTypeChars("using Sys")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="System", isHardSelected:=True).ConfigureAwait(True)
                state.SendTypeChars(";")
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                state.AssertMatchesTextStartingAtLine(1, "using System;")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitForUsingDirective4() As Task
            Using state = TestState.CreateCSharpTestState(
                            <Document>
                                $$
                            </Document>)

                state.SendTypeChars("using Sys")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="System", isHardSelected:=True).ConfigureAwait(True)
                state.SendTypeChars(" ")
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                Assert.Contains("using Sys ", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function KeywordsIncludedInObjectCreationCompletion() As Task
            Using state = TestState.CreateCSharpTestState(
                              <Document>
class C
{
    void Foo()
    {
        string s = new$$
    }
}
                              </Document>)

                state.SendTypeChars(" ")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="string", isHardSelected:=True).ConfigureAwait(True)
                Assert.True(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(c) c.DisplayText = "int"))
            End Using
        End Function

        <WorkItem(544293)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NoKeywordsOrSymbolsAfterNamedParameter() As Task
            Using state = TestState.CreateCSharpTestState(
                              <Document>
class Foo
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
                              </Document>)

                state.SendTypeChars("a")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Assert.True(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(i) i.DisplayText = "num:"))
                Assert.False(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(i) i.DisplayText = "System"))
                Assert.False(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(c) c.DisplayText = "int"))
            End Using
        End Function

        <WorkItem(544017)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function EnumCompletionTriggeredOnSpace() As Task
            Using state = TestState.CreateCSharpTestState(
                              <Document>
enum Numeros { Uno, Dos }
class Foo
{
    void Bar(int a, Numeros n) { }
    void Baz()
    {
        Bar(0$$
    }
}
                              </Document>)

                state.SendTypeChars(", ")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="Numeros", isHardSelected:=True).ConfigureAwait(True)
                Assert.Equal(1, state.CurrentCompletionPresenterSession.CompletionItems.Where(Function(c) c.DisplayText = "Numeros").Count())
            End Using
        End Function

        <WorkItem(479078)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function EnumCompletionTriggeredOnSpaceForNullables() As Task
            Using state = TestState.CreateCSharpTestState(
                              <Document>
enum Numeros { Uno, Dos }
class Foo
{
    void Bar(int a, Numeros? n) { }
    void Baz()
    {
        Bar(0$$
    }
}
                              </Document>)

                state.SendTypeChars(", ")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="Numeros", isHardSelected:=True).ConfigureAwait(True)
                Assert.Equal(1, state.CurrentCompletionPresenterSession.CompletionItems.Where(Function(c) c.DisplayText = "Numeros").Count())
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function EnumCompletionTriggeredOnDot() As Task
            Using state = TestState.CreateCSharpTestState(
                <Document>
enum Numeros { Uno, Dos }
class Foo
{
    void Bar()
    {
        Numeros num = $$
    }
}
                </Document>)

                state.SendTypeChars("Nu.")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Assert.Contains("Numeros num = Numeros.", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function EnumCompletionNotTriggeredOnOtherCommitCharacters() As Task
            Await EnumCompletionNotTriggeredOn("+"c).ConfigureAwait(True)
            Await EnumCompletionNotTriggeredOn("{"c).ConfigureAwait(True)
            Await EnumCompletionNotTriggeredOn(" "c).ConfigureAwait(True)
            Await EnumCompletionNotTriggeredOn(";"c).ConfigureAwait(True)
        End Function

        Private Async Function EnumCompletionNotTriggeredOn(c As Char) As Task
            Using state = TestState.CreateCSharpTestState(
                <Document>
enum Numeros { Uno, Dos }
class Foo
{
    void Bar()
    {
        Numeros num = $$
    }
}
                </Document>)

                state.SendTypeChars("Nu")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="Numeros", isHardSelected:=True).ConfigureAwait(True)
                state.SendTypeChars(c.ToString())
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                Assert.Contains(String.Format("Numeros num = Nu{0}", c), state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(544296)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestVerbatimNamedIdentifierFiltering() As Task
            Using state = TestState.CreateCSharpTestState(
                              <Document>
class Program
{
    void Foo(int @int)
    {
        Foo($$
    }
}
                              </Document>)

                state.SendTypeChars("i")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Assert.True(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(i) i.DisplayText = "@int:"))
                state.SendTypeChars("n")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Assert.True(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(i) i.DisplayText = "@int:"))
                state.SendTypeChars("t")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Assert.True(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(i) i.DisplayText = "@int:"))
            End Using
        End Function

        <WorkItem(543687)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoPreselectInInvalidObjectCreationLocation() As Task
            Using state = TestState.CreateCSharpTestState(
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

class Foo<T> : IFoo<T>
{
}

interface IFoo<T>
{
}]]>
                              </Document>)

                state.SendTypeChars("IFoo<Bar> a = new ")
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(544925)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestQualifiedEnumSelection() As Task
            Using state = TestState.CreateCSharpTestState(
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
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Assert.Contains("Environment.SpecialFolder", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(545070)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTextChangeSpanWithAtCharacter() As Task
            Using state = TestState.CreateCSharpTestState(
                              <Document>
public class @event
{
    $$@event()
    {
    }
}
                              </Document>)

                state.SendTypeChars("public ")
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                Assert.Contains("public @event", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestDoNotInsertColonSoThatUserCanCompleteOutAVariableNameThatDoesNotCurrentlyExist_IE_TheCyrusCase() As Task
            Using state = TestState.CreateCSharpTestState(
                              <Document>
using System.Threading;

class Program
{
    static void Main(string[] args)
    {
        Foo($$)
    }

    void Foo(CancellationToken cancellationToken)
    {
    }
}
                              </Document>)

                state.SendTypeChars("can")
                state.SendTab()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                Assert.Contains("Foo(cancellationToken)", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
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

        <WorkItem(544940)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AttributeNamedPropertyCompletionCommitWithTab() As Task
            Using state = TestState.CreateCSharpTestState(
                            <Document>
class MyAttribute : System.Attribute
{
    public string Name { get; set; }
}

[MyAttribute($$
public class Foo
{
}
                            </Document>)
                state.SendTypeChars("Nam")
                state.SendTab()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                Assert.Equal("[MyAttribute(Name =", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        <WorkItem(544940)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AttributeNamedPropertyCompletionCommitWithEquals() As Task
            Using state = TestState.CreateCSharpTestState(
                            <Document>
class MyAttribute : System.Attribute
{
    public string Name { get; set; }
}

[MyAttribute($$
public class Foo
{
}
                            </Document>)
                state.SendTypeChars("Nam=")
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                Assert.Equal("[MyAttribute(Name =", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        <WorkItem(544940)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AttributeNamedPropertyCompletionCommitWithSpace() As Task
            Using state = TestState.CreateCSharpTestState(
                            <Document>
class MyAttribute : System.Attribute
{
    public string Name { get; set; }
}

[MyAttribute($$
public class Foo
{
}
                            </Document>)
                state.SendTypeChars("Nam ")
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                Assert.Equal("[MyAttribute(Name ", state.GetLineTextFromCaretPosition())
            End Using
        End Function

        <WorkItem(545590)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestOverrideDefaultParameter() As Task
            Using state = TestState.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    public virtual void Foo<S>(S x = default(S))
    {
    }
}

class D : C
{
    override $$
}
            ]]></Document>)
                state.SendTypeChars(" Foo")
                state.SendTab()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                Assert.Contains("public override void Foo<S>(S x = default(S))", state.SubjectBuffer.CurrentSnapshot.GetText(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(545664)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestArrayAfterOptionalParameter() As Task
            Using state = TestState.CreateCSharpTestState(
                <Document><![CDATA[
class A
{
    public virtual void Foo(int x = 0, int[] y = null) { }
}

class B : A
{
public override void Foo(int x = 0, params int[] y) { }
}

class C : B
{
    override$$
}
            ]]></Document>)
                state.SendTypeChars(" Foo")
                state.SendTab()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                Assert.Contains("    public override void Foo(int x = 0, int[] y = null)", state.SubjectBuffer.CurrentSnapshot.GetText(), StringComparison.Ordinal)
            End Using
        End Function

        <WorkItem(545967)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestVirtualSpaces() As Task
            Using state = TestState.CreateCSharpTestState(
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
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("P", isSoftSelected:=True).ConfigureAwait(True)
                state.SendDownKey()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("P", isHardSelected:=True).ConfigureAwait(True)
                state.SendTab()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Assert.Equal("            P", state.GetLineFromCurrentCaretPosition().GetText())

                Dim bufferPosition = state.TextView.Caret.Position.BufferPosition
                Assert.Equal(13, bufferPosition.Position - bufferPosition.GetContainingLine().Start.Position)
                Assert.False(state.TextView.Caret.InVirtualSpace)
            End Using
        End Function

        <WorkItem(546561)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNamedParameterAgainstMRU() As Task
            Using state = TestState.CreateCSharpTestState(
                <Document><![CDATA[
class Program
{
    void Foo(string s) { }

    static void Main()
    {
        $$
    }
}
            ]]></Document>)
                ' prime the MRU
                state.SendTypeChars("string")
                state.SendTab()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)

                ' Delete what we just wrote.
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendEscape()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)

                ' ensure we still select the named param even though 'string' is in the MRU.
                state.SendTypeChars("Foo(s")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("s:").ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(546403)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMissingOnObjectCreationAfterVar1() As Task
            Using state = TestState.CreateCSharpTestState(
                <Document><![CDATA[
class A
{
    void Foo()
    {
        var v = new$$
    }
}
            ]]></Document>)
                state.SendTypeChars(" ")
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(546403)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMissingOnObjectCreationAfterVar2() As Task
            Using state = TestState.CreateCSharpTestState(
                <Document><![CDATA[
class A
{
    void Foo()
    {
        var v = new $$
    }
}
            ]]></Document>)
                state.SendTypeChars("X")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Assert.False(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(i) i.DisplayText = "X"))
            End Using
        End Function

        <WorkItem(546917)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestEnumInSwitch() As Task
            Using state = TestState.CreateCSharpTestState(
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
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="Numeros").ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(547016)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAmbiguityInLocalDeclaration() As Task
            Using state = TestState.CreateCSharpTestState(
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
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="W").ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(530835)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCompletionFilterSpanCaretBoundary() As Task
            Using state = TestState.CreateCSharpTestState(
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
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="Method").ConfigureAwait(True)
                state.SendLeftKey()
                state.SendLeftKey()
                state.SendLeftKey()
                state.SendTypeChars("new")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem(displayText:="Method", isSoftSelected:=True).ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(622957)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestBangFiltersInDocComment() As Task
            Using state = TestState.CreateCSharpTestState(
                  <Document><![CDATA[
using System;

/// $$
/// TestDocComment
/// </summary>
class TestException : Exception { }
]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                state.SendTypeChars("!")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("!--").ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeCompletionDoesNotFilter() As Task
            Using state = TestState.CreateCSharpTestState(
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
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("string").ConfigureAwait(True)
                state.CompletionItemsContainsAll({"integer", "Method"})
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeBeforeWordDoesNotSelect() As Task
            Using state = TestState.CreateCSharpTestState(
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
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("AccessViolationException").ConfigureAwait(True)
                state.CompletionItemsContainsAll({"integer", "Method"})
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeCompletionSelectsWithoutRegardToCaretPosition() As Task
            Using state = TestState.CreateCSharpTestState(
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
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("string").ConfigureAwait(True)
                state.CompletionItemsContainsAll({"integer", "Method"})
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TabAfterQuestionMark() As Task
            Using state = TestState.CreateCSharpTestState(
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
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Assert.Equal(state.GetLineTextFromCaretPosition(), "        ?" + vbTab)
            End Using
        End Function

        <WorkItem(657658)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function PreselectionIgnoresBrackets() As Task
            Using state = TestState.CreateCSharpTestState(
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
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("F<>").ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(672474)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInvokeSnippetCommandDismissesCompletion() As Task
            Using state = TestState.CreateCSharpTestState(
                              <Document>$$</Document>)

                state.SendTypeChars("us")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                state.SendInsertSnippetCommand()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(672474)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSurroundWithCommandDismissesCompletion() As Task
            Using state = TestState.CreateCSharpTestState(
                              <Document>$$</Document>)

                state.SendTypeChars("us")
                Await state.AssertCompletionSession().ConfigureAwait(True)
                state.SendSurroundWithCommand()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(737239)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function LetEditorHandleOpenParen() As Task
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

            Using state = TestState.CreateCSharpTestState(<Document><![CDATA[
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
                Await state.AssertCompletionSession().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("List<int>").ConfigureAwait(True)
                state.SendTypeChars("(")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Assert.Equal(expected, state.GetDocumentText())
            End Using
        End Function

        <WorkItem(785637)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitMovesCaretToWordEnd() As Task
            Using state = TestState.CreateCSharpTestState(
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
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Assert.Equal(state.GetLineFromCurrentCaretPosition().End, state.GetCaretPoint().BufferPosition)
            End Using
        End Function

        <WorkItem(775370)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function MatchingConsidersAtSign() As Task
            Using state = TestState.CreateCSharpTestState(
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
                state.SendTypeChars("var @this = ""foo""")
                state.SendReturn()
                state.SendTypeChars("string str = this.ToString();")
                state.SendReturn()
                state.SendTypeChars("str = @th")

                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("@this").ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(865089)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AttributeFilterTextRemovesAttributeSuffix() As Task
            Using state = TestState.CreateCSharpTestState(
                <Document><![CDATA[
[$$]
class AtAttribute : System.Attribute { }]]></Document>)
                state.SendTypeChars("At")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("At").ConfigureAwait(True)
                Assert.Equal("At", state.CurrentCompletionPresenterSession.SelectedItem.FilterText)
            End Using
        End Function

        <WorkItem(852578)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function PreselectExceptionOverSnippet() As Task
            Using state = TestState.CreateCSharpTestState(
                <Document><![CDATA[
using System;
class C
{
    Exception foo() {
        return new $$
    }
}]]></Document>)
                state.SendTypeChars(" ")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("Exception").ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(868286)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitNameAfterAlias() As Task
            Using state = TestState.CreateCSharpTestState(
                <Document><![CDATA[
using foo = System$$]]></Document>)
                state.SendTypeChars(".act<")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                state.AssertMatchesTextStartingAtLine(1, "using foo = System.Action<")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestCompletionInLinkedFiles() As Task
            Using state = TestState.CreateTestStateFromWorkspace(
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
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("Thing1").ConfigureAwait(True)
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendEscape()
                state.Workspace.SetDocumentContext(linkDocument.Id)
                state.SendTypeChars("Thing1")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("Thing1").ConfigureAwait(True)
                Assert.True(state.CurrentCompletionPresenterSession.SelectedItem.ShowsWarningIcon)
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendTypeChars("M")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("M").ConfigureAwait(True)
                Assert.False(state.CurrentCompletionPresenterSession.SelectedItem.ShowsWarningIcon)
            End Using
        End Function

        <WorkItem(951726)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function DismissUponSave() As Task
            Using state = TestState.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    $$
}]]></Document>)
                state.SendTypeChars("voi")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("void").ConfigureAwait(True)
                state.SendSave()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                state.AssertMatchesTextStartingAtLine(3, "    voi")
            End Using
        End Function

        <WorkItem(930254)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NoCompletionWithBoxSelection() As Task
            Using state = TestState.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    {|Selection:$$int x;|}
    {|Selection:int y;|}
}]]></Document>)
                state.SendInvokeCompletionList()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
                state.SendTypeChars("foo")
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(839555)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TriggeredOnHash() As Task
            Using state = TestState.CreateCSharpTestState(
                <Document><![CDATA[
$$]]></Document>)
                state.SendTypeChars("#")
                Await state.AssertCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(771761)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function RegionCompletionCommitTriggersFormatting_1() As Task
            Using state = TestState.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    $$
}]]></Document>)
                state.SendTypeChars("#reg")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("region").ConfigureAwait(True)
                state.SendReturn()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                state.AssertMatchesTextStartingAtLine(3, "    #region")
            End Using
        End Function

        <WorkItem(771761)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function RegionCompletionCommitTriggersFormatting_2() As Task
            Using state = TestState.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    $$
}]]></Document>)
                state.SendTypeChars("#reg")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("region").ConfigureAwait(True)
                state.SendTypeChars(" ")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                state.AssertMatchesTextStartingAtLine(3, "    #region ")
            End Using
        End Function

        <WorkItem(771761)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function EndRegionCompletionCommitTriggersFormatting_2() As Task
            Using state = TestState.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    #region NameIt
    $$
}]]></Document>)
                state.SendTypeChars("#endreg")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                Await state.AssertSelectedCompletionItem("endregion").ConfigureAwait(True)
                state.SendReturn()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                state.AssertMatchesTextStartingAtLine(4, "    #endregion ")
            End Using
        End Function

        Private Class SlowProvider
            Inherits CompletionListProvider

            Public checkpoint As Checkpoint = New Checkpoint()

            Public Overrides Async Function ProduceCompletionListAsync(context As CompletionListContext) As Task
                Await checkpoint.Task.ConfigureAwait(False)
            End Function

            Public Overrides Function IsTriggerCharacter(text As SourceText, characterPosition As Integer, options As OptionSet) As Boolean
                Return True
            End Function
        End Class

        <WorkItem(1015893)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function BackspaceDismissesIfComputationIsIncomplete() As Task
            Dim slowProvider = New SlowProvider()
            Using state = TestState.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    void foo()
    {
        foo($$
    }
}]]></Document>, {slowProvider})

                state.SendTypeChars("f")
                state.SendBackspace()

                ' Send a backspace that goes beyond the session's applicable span
                ' before the model computation has finished. Then, allow the 
                ' computation to complete. There should still be no session.
                state.SendBackspace()
                slowProvider.checkpoint.Release()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(1065600)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitUniqueItemWithBoxSelection() As Task
            Using state = TestState.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    void foo(int x)
    {
       [|$$ |]
    }
}]]></Document>)
                state.SendReturn()
                state.TextView.Selection.Mode = VisualStudio.Text.Editor.TextSelectionMode.Box
                state.SendCommitUniqueCompletionListItem()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(1594, "https://github.com/dotnet/roslyn/issues/1594")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function NoPreselectionOnSpaceWhenAbuttingWord() As Task
            Using state = TestState.CreateCSharpTestState(
                <Document><![CDATA[
class Program
{
    void Main()
    {
        Program p = new $$Program();
    }
}]]></Document>)
                state.SendTypeChars(" ")
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(1594, "https://github.com/dotnet/roslyn/issues/1594")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SpacePreselectionAtEndOfFile() As Task
            Using state = TestState.CreateCSharpTestState(
                <Document><![CDATA[
class Program
{
    void Main()
    {
        Program p = new $$]]></Document>)
                state.SendTypeChars(" ")
                Await state.AssertCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(1659, "https://github.com/dotnet/roslyn/issues/1659")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function DismissOnSelectAllCommand() As Task
            Using state = TestState.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    void foo(int x)
    {
        $$]]></Document>)
                ' Note: the caret is at the file, so the Select All command's movement
                ' of the caret to the end of the selection isn't responsible for 
                ' dismissing the session.
                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession().ConfigureAwait(True)
                state.SendSelectAll()
                Await state.AssertNoCompletionSession().ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(588, "https://github.com/dotnet/roslyn/issues/588")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CompletionCommitAndFormatAreSeparateUndoTransactions() As Task
            Using state = TestState.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    void foo(int x)
    {
        int doodle;
$$]]></Document>, extraExportedTypes:={GetType(CSharpEditorFormattingService)}.ToList())
                state.SendTypeChars("doo;")
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                state.AssertMatchesTextStartingAtLine(6, "        doodle;")
                state.SendUndo()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                state.AssertMatchesTextStartingAtLine(6, "doodle;")
                state.SendUndo()
                Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                state.AssertMatchesTextStartingAtLine(6, "doo;")
            End Using
        End Function

        <WorkItem(4978, "https://github.com/dotnet/roslyn/issues/4978")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function SessionNotStartedWhenCaretNotMappableIntoSubjectBuffer() As Task
            ' In inline diff view, typing delete next to a "deletion",
            ' can cause our CommandChain to be called with a subjectbuffer
            ' and TextView such that the textView's caret can't be mapped
            ' into our subject buffer. 
            '
            ' To test this, we create a projection buffer with 2 source 
            ' spans: one of "text" content type and one based on a C#
            ' buffer. We create a TextView with that projection as 
            ' its buffer, setting the caret such that it maps only
            ' into the "text" buffer. We then call the completion
            ' command handlers with commandargs based on that TextView
            ' but with the C# buffer as the SubjectBuffer.

            Using state = TestState.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    void foo(int x)
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

                Dim view = textViewFactory.CreateTextView(projection)
                Try
                    view.Caret.MoveTo(New SnapshotPoint(view.TextBuffer.CurrentSnapshot, 0))

                    Dim editorOperations = editorOperationsFactory.GetEditorOperations(view)
                    state.CompletionCommandHandler.ExecuteCommand(New DeleteKeyCommandArgs(view, state.SubjectBuffer), Sub() editorOperations.Delete())

                    Await state.AssertNoCompletionSession().ConfigureAwait(True)
                Finally
                    view.Close()
                End Try
            End Using
        End Function

        <WorkItem(588, "https://github.com/dotnet/roslyn/issues/588")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMatchWithTurkishIWorkaround1() As Task
            Using New CultureContext("tr-TR")
                Using state = TestState.CreateCSharpTestState(
                               <Document><![CDATA[
        class C
        {
            void foo(int x)
            {
                string.$$]]></Document>, extraExportedTypes:={GetType(CSharpEditorFormattingService)}.ToList())
                    state.SendTypeChars("is")
                    Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                    Await state.AssertSelectedCompletionItem("IsInterned").ConfigureAwait(True)
                End Using
            End Using

        End Function

        <WorkItem(588, "https://github.com/dotnet/roslyn/issues/588")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMatchWithTurkishIWorkaround2() As Task
            Using New CultureContext("tr-TR")
                Using state = TestState.CreateCSharpTestState(
                               <Document><![CDATA[
        class C
        {
            void foo(int x)
            {
                string.$$]]></Document>, extraExportedTypes:={GetType(CSharpEditorFormattingService)}.ToList())
                    state.SendTypeChars("ı")
                    Await state.WaitForAsynchronousOperationsAsync().ConfigureAwait(True)
                    Await state.AssertSelectedCompletionItem().ConfigureAwait(True)
                End Using
            End Using

        End Function
    End Class
End Namespace
