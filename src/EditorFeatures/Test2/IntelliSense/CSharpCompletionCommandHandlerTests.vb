' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TabCommitsWithoutAUniqueMatch()
            Using state = TestState.CreateCSharpTestState(
                              <Document>
                                  $$
                              </Document>)

                state.SendTypeChars("using System.Ne")
                state.AssertSelectedCompletionItem(displayText:="Net", isHardSelected:=True)
                state.SendTypeChars("x")
                state.AssertSelectedCompletionItem(displayText:="Net", isSoftSelected:=True)
                state.SendTab()
                state.AssertNoCompletionSession()
                Assert.Contains("using System.Net", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestAtEndOfFile()
            Using state = TestState.CreateCSharpTestState(
                              <Document>$$</Document>)

                state.SendTypeChars("us")
                state.SendTab()
                state.AssertNoCompletionSession()
                Assert.Contains("using", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestNotAtStartOfExistingWord()
            Using state = TestState.CreateCSharpTestState(
                              <Document>$$using</Document>)

                state.SendTypeChars("u")
                state.AssertNoCompletionSession()
                Assert.Contains("using", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestMSCorLibTypes()
            Using state = TestState.CreateCSharpTestState(
                              <Document>
using System;

class c : $$
                              </Document>)

                state.SendTypeChars("A")
                state.AssertCompletionSession()

                Assert.True(state.CompletionItemsContainsAll(displayText:={"Attribute", "Exception", "IDisposable"}))
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestFiltering1()
            Using state = TestState.CreateCSharpTestState(
                              <Document>
using System;
      
class c { $$
                              </Document>)

                state.SendTypeChars("Sy")
                state.AssertCompletionSession()

                Assert.True(state.CompletionItemsContainsAll(displayText:={"OperatingSystem", "System", "SystemException"}))
                Assert.False(state.CompletionItemsContainsAny(displayText:={"Exception", "Activator"}))
            End Using
        End Sub

        ' NOTE(cyrusn): This should just be a unit test for SymbolCompletionProvider.  However, i'm
        ' just porting the integration tests to here for now.
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestMultipleTypes()
            Using state = TestState.CreateCSharpTestState(
                              <Document>
class C { $$ } struct S { } enum E { } interface I { } delegate void D();
                              </Document>)

                state.SendTypeChars("C")
                state.AssertCompletionSession()
                Assert.True(state.CompletionItemsContainsAll(displayText:={"C", "S", "E", "I", "D"}))
            End Using
        End Sub

        ' NOTE(cyrusn): This should just be a unit test for KeywordCompletionProvider.  However, i'm
        ' just porting the integration tests to here for now.
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestInEmptyFile()
            Using state = TestState.CreateCSharpTestState(
                              <Document>
$$
                              </Document>)

                state.SendInvokeCompletionList()
                state.AssertCompletionSession()
                Assert.True(state.CompletionItemsContainsAll(displayText:={"abstract", "class", "namespace"}))
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestNotAfterTypingDotAfterIntegerLiteral()
            Using state = TestState.CreateCSharpTestState(
                              <Document>
class c { void M() { 3$$ } }
                              </Document>)

                state.SendTypeChars(".")
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestAfterExplicitInvokeAfterDotAfterIntegerLiteral()
            Using state = TestState.CreateCSharpTestState(
                              <Document>
class c { void M() { 3.$$ } }
                              </Document>)

                state.SendInvokeCompletionList()
                state.AssertCompletionSession()
                Assert.True(state.CompletionItemsContainsAll({"ToString"}))
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestEnterIsConsumed()
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
                Assert.Equal(<text>
class Class1
{
    void Main(string[] args)
    {
        System.TimeSpan.FromMinutes
    }
}</text>.NormalizedValue, state.GetDocumentText())
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestDescription1()
            Using state = TestState.CreateCSharpTestState(
                  <Document><![CDATA[
using System;

/// <summary>
/// TestDocComment
/// </summary>
class TestException : Exception { }

class MyException : $$]]></Document>)

                state.SendTypeChars("Test")
                state.AssertSelectedCompletionItem(description:="class TestException" & vbCrLf & "TestDocComment")
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestObjectCreationPreselection1()
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
                state.AssertSelectedCompletionItem(displayText:="List<int>", isHardSelected:=True)
                Assert.True(state.CompletionItemsContainsAll(displayText:={"LinkedList<>", "List<>", "System"}))
                state.SendTypeChars("Li")
                state.AssertSelectedCompletionItem(displayText:="List<int>", isHardSelected:=True)
                Assert.True(state.CompletionItemsContainsAll(displayText:={"LinkedList<>", "List<>"}))
                Assert.False(state.CompletionItemsContainsAny(displayText:={"System"}))
                state.SendTypeChars("n")
                state.AssertSelectedCompletionItem(displayText:="LinkedList<>", isHardSelected:=True)
                state.SendBackspace()
                state.AssertSelectedCompletionItem(displayText:="List<int>", isHardSelected:=True)
                state.SendTab()
                Assert.Contains("new List<int>", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(543268)>
        Public Sub TestTypePreselection1()
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
                state.AssertSelectedCompletionItem(displayText:="C", isHardSelected:=True)
                state.SendTypeChars(" ")
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <WorkItem(543519)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestNewPreselectionAfterVar()
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
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <WorkItem(543559)>
        <WorkItem(543561)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestEscapedIdentifiers()
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
                state.AssertNoCompletionSession()
                state.SendTypeChars("r")
                state.AssertSelectedCompletionItem(displayText:="@return", isHardSelected:=True)
                state.SendTab()
                Assert.Contains("@return", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Sub

        <WorkItem(543771)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestCommitUniqueItem1()
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
                state.AssertNoCompletionSession()
                Assert.Contains("WriteLine()", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Sub

        <WorkItem(543771)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestCommitUniqueItem2()
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
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CommitForUsingDirective1()
            Using state = TestState.CreateCSharpTestState(
                              <Document>
                                  $$
                              </Document>)

                state.SendTypeChars("using Sys")
                state.AssertSelectedCompletionItem(displayText:="System", isHardSelected:=True)
                state.SendTypeChars("(")
                state.AssertNoCompletionSession()
                Assert.Contains("using Sys(", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CommitForUsingDirective2()
            Using state = TestState.CreateCSharpTestState(
                              <Document>
                                  $$
                              </Document>)

                state.SendTypeChars("using Sys")
                state.AssertSelectedCompletionItem(displayText:="System", isHardSelected:=True)
                state.SendTypeChars(".")
                state.AssertCompletionSession()
                Assert.Contains("using System.", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CommitForUsingDirective3()
            Using state = TestState.CreateCSharpTestState(
                              <Document>
                                  $$
                              </Document>, extraExportedTypes:={GetType(CSharpEditorFormattingService)}.ToList())

                state.SendTypeChars("using Sys")
                state.AssertSelectedCompletionItem(displayText:="System", isHardSelected:=True)
                state.SendTypeChars(";")
                state.AssertNoCompletionSession()
                state.AssertMatchesTextStartingAtLine(1, "using System;")
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CommitForUsingDirective4()
            Using state = TestState.CreateCSharpTestState(
                            <Document>
                                $$
                            </Document>)

                state.SendTypeChars("using Sys")
                state.AssertSelectedCompletionItem(displayText:="System", isHardSelected:=True)
                state.SendTypeChars(" ")
                state.AssertNoCompletionSession()
                Assert.Contains("using Sys ", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub KeywordsIncludedInObjectCreationCompletion()
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
                state.AssertSelectedCompletionItem(displayText:="string", isHardSelected:=True)
                Assert.True(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(c) c.DisplayText = "int"))
            End Using
        End Sub

        <WorkItem(544293)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoKeywordsOrSymbolsAfterNamedParameter()
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
                state.AssertCompletionSession()
                Assert.True(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(i) i.DisplayText = "num:"))
                Assert.False(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(i) i.DisplayText = "System"))
                Assert.False(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(c) c.DisplayText = "int"))
            End Using
        End Sub

        <WorkItem(544017)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EnumCompletionTriggeredOnSpace()
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
                state.AssertSelectedCompletionItem(displayText:="Numeros", isHardSelected:=True)
                Assert.Equal(1, state.CurrentCompletionPresenterSession.CompletionItems.Where(Function(c) c.DisplayText = "Numeros").Count())
            End Using
        End Sub

        <WorkItem(479078)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EnumCompletionTriggeredOnSpaceForNullables()
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
                state.AssertSelectedCompletionItem(displayText:="Numeros", isHardSelected:=True)
                Assert.Equal(1, state.CurrentCompletionPresenterSession.CompletionItems.Where(Function(c) c.DisplayText = "Numeros").Count())
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EnumCompletionTriggeredOnDot()
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
                Assert.Contains("Numeros num = Numeros.", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EnumCompletionNotTriggeredOnOtherCommitCharacters()
            EnumCompletionNotTriggeredOn("+"c)
            EnumCompletionNotTriggeredOn("{"c)
            EnumCompletionNotTriggeredOn(" "c)
            EnumCompletionNotTriggeredOn(";"c)
        End Sub

        Private Sub EnumCompletionNotTriggeredOn(c As Char)
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
                state.AssertSelectedCompletionItem(displayText:="Numeros", isHardSelected:=True)
                state.SendTypeChars(c.ToString())
                state.AssertNoCompletionSession()
                Assert.Contains(String.Format("Numeros num = Nu{0}", c), state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Sub

        <WorkItem(544296)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestVerbatimNamedIdentifierFiltering()
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
                state.AssertCompletionSession()
                Assert.True(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(i) i.DisplayText = "@int:"))
                state.SendTypeChars("n")
                state.WaitForAsynchronousOperations()
                Assert.True(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(i) i.DisplayText = "@int:"))
                state.SendTypeChars("t")
                state.WaitForAsynchronousOperations()
                Assert.True(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(i) i.DisplayText = "@int:"))
            End Using
        End Sub

        <WorkItem(543687)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestNoPreselectInInvalidObjectCreationLocation()
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
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <WorkItem(544925)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestQualifiedEnumSelection()
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
                Assert.Contains("Environment.SpecialFolder", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Sub

        <WorkItem(545070)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestTextChangeSpanWithAtCharacter()
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
                state.AssertNoCompletionSession()
                Assert.Contains("public @event", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestDoNotInsertColonSoThatUserCanCompleteOutAVariableNameThatDoesNotCurrentlyExist_IE_TheCyrusCase()
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
                state.AssertNoCompletionSession()
                Assert.Contains("Foo(cancellationToken)", state.GetLineTextFromCaretPosition(), StringComparison.Ordinal)
            End Using
        End Sub

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
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AttributeNamedPropertyCompletionCommitWithTab()
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
                state.AssertNoCompletionSession()
                Assert.Equal("[MyAttribute(Name =", state.GetLineTextFromCaretPosition())
            End Using
        End Sub

        <WorkItem(544940)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AttributeNamedPropertyCompletionCommitWithEquals()
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
                state.AssertNoCompletionSession()
                Assert.Equal("[MyAttribute(Name =", state.GetLineTextFromCaretPosition())
            End Using
        End Sub

        <WorkItem(544940)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AttributeNamedPropertyCompletionCommitWithSpace()
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
                state.AssertNoCompletionSession()
                Assert.Equal("[MyAttribute(Name ", state.GetLineTextFromCaretPosition())
            End Using
        End Sub

        <WorkItem(545590)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestOverrideDefaultParameter()
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
                state.AssertNoCompletionSession()
                Assert.Contains("public override void Foo<S>(S x = default(S))", state.SubjectBuffer.CurrentSnapshot.GetText(), StringComparison.Ordinal)
            End Using
        End Sub

        <WorkItem(545664)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestArrayAfterOptionalParameter()
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
                state.AssertNoCompletionSession()
                Assert.Contains("    public override void Foo(int x = 0, int[] y = null)", state.SubjectBuffer.CurrentSnapshot.GetText(), StringComparison.Ordinal)
            End Using
        End Sub

        <WorkItem(545967)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestVirtualSpaces()
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
                state.AssertCompletionSession()
                state.AssertSelectedCompletionItem("P", isSoftSelected:=True)
                state.SendDownKey()
                state.AssertSelectedCompletionItem("P", isHardSelected:=True)
                state.SendTab()
                Assert.Equal("            P", state.GetLineFromCurrentCaretPosition().GetText())

                Dim bufferPosition = state.TextView.Caret.Position.BufferPosition
                Assert.Equal(13, bufferPosition.Position - bufferPosition.GetContainingLine().Start.Position)
                Assert.False(state.TextView.Caret.InVirtualSpace)
            End Using
        End Sub

        <WorkItem(546561)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestNamedParameterAgainstMRU()
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
                state.AssertNoCompletionSession()

                ' Delete what we just wrote.
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendEscape()
                state.AssertNoCompletionSession()

                ' ensure we still select the named param even though 'string' is in the MRU.
                state.SendTypeChars("Foo(s")
                state.AssertSelectedCompletionItem("s:")
            End Using
        End Sub

        <WorkItem(546403)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestMissingOnObjectCreationAfterVar1()
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
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <WorkItem(546403)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestMissingOnObjectCreationAfterVar2()
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
                state.AssertCompletionSession()
                Assert.False(state.CurrentCompletionPresenterSession.CompletionItems.Any(Function(i) i.DisplayText = "X"))
            End Using
        End Sub

        <WorkItem(546917)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestEnumInSwitch()
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
                state.AssertSelectedCompletionItem(displayText:="Numeros")
            End Using
        End Sub

        <WorkItem(547016)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestAmbiguityInLocalDeclaration()
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
                state.AssertSelectedCompletionItem(displayText:="W")
            End Using
        End Sub

        <WorkItem(530835)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestCompletionFilterSpanCaretBoundary()
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
                state.AssertSelectedCompletionItem(displayText:="Method")
                state.SendLeftKey()
                state.SendLeftKey()
                state.SendLeftKey()
                state.SendTypeChars("new")
                state.AssertSelectedCompletionItem(displayText:="Method", isSoftSelected:=True)
            End Using
        End Sub

        <WorkItem(622957)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestBangFiltersInDocComment()
            Using state = TestState.CreateCSharpTestState(
                  <Document><![CDATA[
using System;

/// $$
/// TestDocComment
/// </summary>
class TestException : Exception { }
]]></Document>)

                state.SendTypeChars("<")
                state.AssertCompletionSession()
                state.SendTypeChars("!")
                state.AssertCompletionSession()
                state.AssertSelectedCompletionItem("!--")
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InvokeCompletionDoesNotFilter()
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
                state.AssertSelectedCompletionItem("string")
                state.CompletionItemsContainsAll({"integer", "Method"})
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InvokeBeforeWordDoesNotSelect()
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
                state.AssertSelectedCompletionItem("AccessViolationException")
                state.CompletionItemsContainsAll({"integer", "Method"})
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub InvokeCompletionSelectsWithoutRegardToCaretPosition()
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
                state.AssertSelectedCompletionItem("string")
                state.CompletionItemsContainsAll({"integer", "Method"})
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TabAfterQuestionMark()
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
                Assert.Equal(state.GetLineTextFromCaretPosition(), "        ?" + vbTab)
            End Using
        End Sub

        <WorkItem(657658)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub PreselectionIgnoresBrackets()
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
                state.AssertCompletionSession()
                state.AssertSelectedCompletionItem("F<>")
            End Using
        End Sub

        <WorkItem(672474)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestInvokeSnippetCommandDismissesCompletion()
            Using state = TestState.CreateCSharpTestState(
                              <Document>$$</Document>)

                state.SendTypeChars("us")
                state.AssertCompletionSession()
                state.SendInsertSnippetCommand()
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <WorkItem(672474)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestSurroundWithCommandDismissesCompletion()
            Using state = TestState.CreateCSharpTestState(
                              <Document>$$</Document>)

                state.SendTypeChars("us")
                state.AssertCompletionSession()
                state.SendSurroundWithCommand()
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <WorkItem(737239)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub LetEditorHandleOpenParen()
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
                state.AssertCompletionSession()
                state.AssertSelectedCompletionItem("List<int>")
                state.SendTypeChars("(")
                state.WaitForAsynchronousOperations()
                Assert.Equal(expected, state.GetDocumentText())
            End Using
        End Sub

        <WorkItem(785637)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CommitMovesCaretToWordEnd()
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
                Assert.Equal(state.GetLineFromCurrentCaretPosition().End, state.GetCaretPoint().BufferPosition)
            End Using
        End Sub

        <WorkItem(775370)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub MatchingConsidersAtSign()
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

                state.AssertSelectedCompletionItem("@this")
            End Using
        End Sub

        <WorkItem(865089)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub AttributeFilterTextRemovesAttributeSuffix()
            Using state = TestState.CreateCSharpTestState(
                <Document><![CDATA[
[$$]
class AtAttribute : System.Attribute { }]]></Document>)
                state.SendTypeChars("At")
                state.AssertSelectedCompletionItem("At")
                Assert.Equal("At", state.CurrentCompletionPresenterSession.SelectedItem.FilterText)
            End Using
        End Sub

        <WorkItem(852578)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub PreselectExceptionOverSnippet()
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
                state.AssertSelectedCompletionItem("Exception")
            End Using
        End Sub

        <WorkItem(868286)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CommitNameAfterAlias()
            Using state = TestState.CreateCSharpTestState(
                <Document><![CDATA[
using foo = System$$]]></Document>)
                state.SendTypeChars(".act<")
                state.AssertMatchesTextStartingAtLine(1, "using foo = System.Action<")
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TestCompletionInLinkedFiles()
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
                state.AssertSelectedCompletionItem("Thing1")
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendEscape()
                state.Workspace.SetDocumentContext(linkDocument.Id)
                state.SendTypeChars("Thing1")
                state.AssertSelectedCompletionItem("Thing1")
                Assert.True(state.CurrentCompletionPresenterSession.SelectedItem.ShowsWarningIcon)
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendBackspace()
                state.SendTypeChars("M")
                state.AssertSelectedCompletionItem("M")
                Assert.False(state.CurrentCompletionPresenterSession.SelectedItem.ShowsWarningIcon)
            End Using
        End Sub

        <WorkItem(951726)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub DismissUponSave()
            Using state = TestState.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    $$
}]]></Document>)
                state.SendTypeChars("voi")
                state.AssertSelectedCompletionItem("void")
                state.SendSave()
                state.AssertNoCompletionSession(block:=True)
                state.AssertMatchesTextStartingAtLine(3, "    voi")
            End Using
        End Sub

        <WorkItem(930254)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoCompletionWithBoxSelection()
            Using state = TestState.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    {|Selection:$$int x;|}
    {|Selection:int y;|}
}]]></Document>)
                state.SendInvokeCompletionList()
                state.AssertNoCompletionSession()
                state.SendTypeChars("foo")
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <WorkItem(839555)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub TriggeredOnHash()
            Using state = TestState.CreateCSharpTestState(
                <Document><![CDATA[
$$]]></Document>)
                state.SendTypeChars("#")
                state.AssertCompletionSession()
            End Using
        End Sub

        <WorkItem(771761)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub RegionCompletionCommitTriggersFormatting_1()
            Using state = TestState.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    $$
}]]></Document>)
                state.SendTypeChars("#reg")
                state.AssertSelectedCompletionItem("region")
                state.SendReturn()
                state.AssertMatchesTextStartingAtLine(3, "    #region")
            End Using
        End Sub

        <WorkItem(771761)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub RegionCompletionCommitTriggersFormatting_2()
            Using state = TestState.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    $$
}]]></Document>)
                state.SendTypeChars("#reg")
                state.AssertSelectedCompletionItem("region")
                state.SendTypeChars(" ")
                state.AssertMatchesTextStartingAtLine(3, "    #region ")
            End Using
        End Sub

        <WorkItem(771761)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub EndRegionCompletionCommitTriggersFormatting_2()
            Using state = TestState.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    #region NameIt
    $$
}]]></Document>)
                state.SendTypeChars("#endreg")
                state.AssertSelectedCompletionItem("endregion")
                state.SendReturn()
                state.AssertMatchesTextStartingAtLine(4, "    #endregion ")
            End Using
        End Sub

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
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub BackspaceDismissesIfComputationIsIncomplete()
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
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <WorkItem(1065600)>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CommitUniqueItemWithBoxSelection()
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
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <WorkItem(1594, "https://github.com/dotnet/roslyn/issues/1594")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub NoPreselectionOnSpaceWhenAbuttingWord()
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
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <WorkItem(1594, "https://github.com/dotnet/roslyn/issues/1594")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SpacePreselectionAtEndOfFile()
            Using state = TestState.CreateCSharpTestState(
                <Document><![CDATA[
class Program
{
    void Main()
    {
        Program p = new $$]]></Document>)
                state.SendTypeChars(" ")
                state.AssertCompletionSession()
            End Using
        End Sub

        <WorkItem(1659, "https://github.com/dotnet/roslyn/issues/1659")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub DismissOnSelectAllCommand()
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
                state.AssertCompletionSession()
                state.SendSelectAll()
                state.AssertNoCompletionSession()
            End Using
        End Sub

        <WorkItem(588, "https://github.com/dotnet/roslyn/issues/588")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub CompletionCommitAndFormatAreSeparateUndoTransactions()
            Using state = TestState.CreateCSharpTestState(
                <Document><![CDATA[
class C
{
    void foo(int x)
    {
        int doodle;
$$]]></Document>, extraExportedTypes:={GetType(CSharpEditorFormattingService)}.ToList())
                state.SendTypeChars("doo;")
                state.AssertMatchesTextStartingAtLine(6, "        doodle;")
                state.SendUndo()
                state.AssertMatchesTextStartingAtLine(6, "doodle;")
                state.SendUndo()
                state.AssertMatchesTextStartingAtLine(6, "doo;")
            End Using
        End Sub

        <WorkItem(4978, "https://github.com/dotnet/roslyn/issues/4978")>
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Sub SessionNotStartedWhenCaretNotMappableIntoSubjectBuffer()
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

                    state.AssertNoCompletionSession()
                Finally
                    view.Close()
                End Try
            End Using
        End Sub
    End Class
End Namespace
