' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    ''' <summary>
    ''' Tests that want to exercise as much of the real command handling stack as possible.
    ''' </summary>
    <[UseExportProvider]>
    Public Class FormattingCommandHandlerTests

        <WorkItem(912965, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/912965")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Formatting)>
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

        Private Shared Sub AssertVirtualCaretColumn(state As TestState, expectedCol As Integer)
            Dim caretLine = state.GetLineFromCurrentCaretPosition()
            Dim caret = state.GetCaretPoint()
            Assert.Equal(expectedCol, caret.VirtualBufferPosition.VirtualSpaces)
        End Sub
    End Class
End Namespace
