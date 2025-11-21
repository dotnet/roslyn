' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    ''' <summary>
    ''' Tests that want to exercise as much of the real command handling stack as possible.
    ''' </summary>
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.Formatting)>
    Public Class FormattingCommandHandlerTests
        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/912965")>
        Public Sub TypingUsingStatementsProperlyAligns1()
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
using System;
class TestClass
{
    void TestMethod(IDisposable obj)
    {
        $$
    }
}
                              </Document>, includeFormatCommandHandler:=True)
                state.SendTypeChars("using (obj)")
                state.SendReturn()

                ' we should be indented one level from the using
                AssertVirtualCaretColumn(state, 12)

                ' We should continue being indentded one level in
                state.SendTypeChars("u")
                Assert.Equal("            u", state.GetLineTextFromCaretPosition())

                ' Until close paren is typed.  Then, we should align the usings
                state.SendTypeChars("sing (obj")
                Assert.Equal("            using (obj", state.GetLineTextFromCaretPosition())

                state.SendTypeChars(")")
                Assert.Equal("        using (obj)", state.GetLineTextFromCaretPosition())

                state.SendReturn()
                ' we should be indented one level from the using
                AssertVirtualCaretColumn(state, 12)

                ' typing open brace should align with using.
                state.SendTypeChars("{")
                Assert.Equal("        {", state.GetLineTextFromCaretPosition())

                state.SendReturn()
                ' we should be indented one level from the {
                AssertVirtualCaretColumn(state, 12)

                ' typing close brace should align with open brace.
                state.SendTypeChars("}")
                Assert.Equal("        }", state.GetLineTextFromCaretPosition())
            End Using
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/35702")>
        Public Sub TypingElseKeywordIndentsInAmbiguousScenario()
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
using System;
class TestClass
{
    void TestMethod(string[] args)
    {
        foreach (var v in args)
            if (v != null)
                Console.WriteLine("v is not null");
        els$$
    }
}
                              </Document>, includeFormatCommandHandler:=True)
                state.SendTypeChars("e")

                Assert.Equal("            else", state.GetLineTextFromCaretPosition())
            End Using
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/35702")>
        Public Sub TypingElseKeywordDoesNotIndentFollowingNonBlockConstruct()
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>using System;
class TestClass
{
    void TestMethod(string[] args)
    {
        if (v != null)
            Console.WriteLine("v is not null");
    els$$

        if (v != null) 
            Console.WriteLine("v is not null");         
    }
}</Document>, includeFormatCommandHandler:=True)
                state.SendTypeChars("e")

                AssertEx.Equal("using System;
class TestClass
{
    void TestMethod(string[] args)
    {
        if (v != null)
            Console.WriteLine(""v is not null"");
        else

        if (v != null) 
            Console.WriteLine(""v is not null"");         
    }
}", state.GetDocumentText())
            End Using
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/35702")>
        Public Sub TypingElseKeywordDoesNotIndentFollowingBlockConstruct()
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>using System;
class TestClass
{
    void TestMethod(string[] args)
    {
        if (v != null)
            Console.WriteLine("v is not null");
    els$$
    {
        Console.WriteLine("v is null");
    }
    }
}</Document>, includeFormatCommandHandler:=True)
                state.SendTypeChars("e")

                AssertEx.Equal("using System;
class TestClass
{
    void TestMethod(string[] args)
    {
        if (v != null)
            Console.WriteLine(""v is not null"");
        else
        {
            Console.WriteLine(""v is null"");
        }
    }
}", state.GetDocumentText())
            End Using
        End Sub

        Private Shared Sub AssertVirtualCaretColumn(state As TestState, expectedCol As Integer)
            Dim caretLine = state.GetLineFromCurrentCaretPosition()
            Dim caret = state.GetCaretPoint()
            Assert.Equal(expectedCol, caret.VirtualBufferPosition.VirtualSpaces)
        End Sub
    End Class
End Namespace
