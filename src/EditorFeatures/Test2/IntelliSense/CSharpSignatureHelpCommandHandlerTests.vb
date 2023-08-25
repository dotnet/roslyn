' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CSharp
Imports Microsoft.CodeAnalysis.Editor.Options
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.SignatureHelp)>
    Public Class CSharpSignatureHelpCommandHandlerTests

        <WpfTheory, CombinatorialData>
        Public Async Function TestCreateAndDismiss(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void Goo()
    {
        Goo$$
    }
}
                              </Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("(")
                Await state.AssertSelectedSignatureHelpItem(displayText:="void C.Goo()")
                state.SendTypeChars(")")
                Await state.AssertNoSignatureHelpSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TypingUpdatesParameters(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void Goo(int i, string j)
    {
        Goo$$
    }
}
                              </Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("(")
                Await state.AssertSelectedSignatureHelpItem(displayText:="void C.Goo(int i, string j)", selectedParameter:="int i")
                state.SendTypeChars("1,")
                Await state.AssertSelectedSignatureHelpItem(displayText:="void C.Goo(int i, string j)", selectedParameter:="string j")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TypingChangeParameterByNavigating(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void Goo(int i, string j)
    {
        Goo(1$$
    }
}
                              </Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(",")
                Await state.AssertSelectedSignatureHelpItem(displayText:="void C.Goo(int i, string j)", selectedParameter:="string j")
                state.SendLeftKey()
                Await state.AssertSelectedSignatureHelpItem(displayText:="void C.Goo(int i, string j)", selectedParameter:="int i")
                state.SendRightKey()
                Await state.AssertSelectedSignatureHelpItem(displayText:="void C.Goo(int i, string j)", selectedParameter:="string j")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function NavigatingOutOfSpanDismissesSignatureHelp(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void Goo()
    {
        Goo($$)
    }
}
                              </Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeSignatureHelp()
                Await state.AssertSelectedSignatureHelpItem(displayText:="void C.Goo()")
                state.SendRightKey()
                Await state.AssertNoSignatureHelpSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestNestedCalls(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void Bar() { }
    void Goo()
    {
        Goo$$
    }
}
                              </Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("(")
                Await state.AssertSelectedSignatureHelpItem(displayText:="void C.Goo()")
                state.SendTypeChars("Bar(")
                Await state.AssertSelectedSignatureHelpItem(displayText:="void C.Bar()")
                state.SendTypeChars(")")
                Await state.AssertSelectedSignatureHelpItem(displayText:="void C.Goo()")
            End Using
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544547")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestNoSigHelpOnGenericNamespace(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
namespace global::F$$
                              </Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("<")
                Await state.AssertNoSignatureHelpSession()
            End Using
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544547")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestSigHelpOnExtraSpace(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class G&lt;S, T&gt; { };

class C
{
    void Goo()
    {
        G&lt;int, $$
    }
}
                              </Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeSignatureHelp()
                Await state.AssertSignatureHelpSession()
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/6713")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestOnIncompleteInvocation(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document><![CDATA[
class Program
{
    static void Main(string[] args)
    {
        F$$
    }
    static void F(int i, int j) { }
    static void F(string s, int j, int k) { }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                ' We don't have a definite symbol, so default to first
                state.SendTypeChars("(")
                Await state.AssertSignatureHelpSession()
                Assert.Equal({"void Program.F(int i, int j)", "void Program.F(string s, int j, int k)"},
                             state.GetSignatureHelpItems().Select(Function(i) i.ToString()))
                Await state.AssertSelectedSignatureHelpItem("void Program.F(int i, int j)")
                Assert.Equal(2, state.GetSignatureHelpItems().Count)

                ' We have a definite guess (the string overload)
                state.SendTypeChars("""""")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("void Program.F(string s, int j, int k)")
                Assert.Equal(2, state.GetSignatureHelpItems().Count)

                state.SendTypeChars(", 2")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("void Program.F(string s, int j, int k)")

                state.SendTypeChars(", 3")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("void Program.F(string s, int j, int k)")

                ' Selection becomes invalid
                state.SendTypeChars(", 4")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("void Program.F(string s, int j, int k)")
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/6713")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestOnIncompleteInvocation_CommaMatters(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document><![CDATA[
class Program
{
    static void Main(string[] args)
    {
        F$$
    }
    static void F(int i) { }
    static void F(string s, int j) { }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                ' We don't have a definite symbol, so default to first
                state.SendTypeChars("(")
                Await state.AssertSignatureHelpSession()
                Assert.Equal({"void Program.F(int i)", "void Program.F(string s, int j)"},
                             state.GetSignatureHelpItems().Select(Function(i) i.ToString()))
                Await state.AssertSelectedSignatureHelpItem("void Program.F(int i)")
                Assert.Equal(2, state.GetSignatureHelpItems().Count)

                ' We have a definite guess (the first acceptable overload)
                state.SendTypeChars("default")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("void Program.F(int i)")
                Assert.Equal(2, state.GetSignatureHelpItems().Count)

                state.SendTypeChars(",")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("void Program.F(string s, int j)")

                state.SendBackspace()
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("void Program.F(int i)")
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/6713")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestOnIncompleteInvocation_WithRef(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document><![CDATA[
class Program
{
    static void Main(string args)
    {
        F$$
    }
    static void F(ref int i, int j) { }
    static void F(double d) { }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                ' We don't have a definite symbol, so default to first
                state.SendTypeChars("(")
                Await state.AssertSignatureHelpSession()
                Assert.Equal({"void Program.F(double d)", "void Program.F(ref int i, int j)"},
                             state.GetSignatureHelpItems().Select(Function(i) i.ToString()))
                Await state.AssertSelectedSignatureHelpItem("void Program.F(double d)")
                Assert.Equal(2, state.GetSignatureHelpItems().Count)

                ' We have a definite guess (the overload with ref)
                state.SendTypeChars("ref args")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("void Program.F(ref int i, int j)")

                ' Selection becomes invalid
                state.SendTypeChars(", 2, 3")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("void Program.F(ref int i, int j)")
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/6713")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestOnIncompleteInvocation_WithArgumentName(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document><![CDATA[
class Program
{
    static void Main(string args)
    {
        F$$
    }
    static void F(int i, int j) { }
    static void F(string name) { }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("(name: 1")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("void Program.F(string name)")
                Assert.Equal({"void Program.F(string name)"},
                             state.GetSignatureHelpItems().Select(Function(i) i.ToString()))
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/6713")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestOnIncompleteInvocation_WithExtension(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document><![CDATA[
class Program
{
    void M()
    {
        this.F$$
    }
}
public static class ProgramExtension
{
    public static void F(this Program p, int i, int j) { }
    public static void F(this Program p, string name) { }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("(")
                Await state.AssertSignatureHelpSession()
                Assert.Equal({$"({CSharpFeaturesResources.extension}) void Program.F(string name)", $"({CSharpFeaturesResources.extension}) void Program.F(int i, int j)"},
                             state.GetSignatureHelpItems().Select(Function(i) i.ToString()))
                Await state.AssertSelectedSignatureHelpItem($"({CSharpFeaturesResources.extension}) void Program.F(string name)")

                state.SendTypeChars("1")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem($"({CSharpFeaturesResources.extension}) void Program.F(int i, int j)")
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/6713")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestOnIncompleteObjectConstruction(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document><![CDATA[
class Program
{
    void M()
    {
        new Program$$
    }
    Program(int i, int j) { }
    Program(string name) { }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("(")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("Program(string name)")
                Assert.Equal({"Program(string name)", "Program(int i, int j)"},
                             state.GetSignatureHelpItems().Select(Function(i) i.ToString()))

                state.SendTypeChars("1")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("Program(int i, int j)")
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/6713")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestOnIncompleteConstructorInitializer(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document><![CDATA[
class Program
{
    Program() : this$$
    {
    }
    Program(int i, int j) { }
    Program(string name) { }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("(")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("Program(string name)")
                Assert.Equal({"Program(string name)", "Program(int i, int j)"},
                             state.GetSignatureHelpItems().Select(Function(i) i.ToString()))

                state.SendTypeChars("1")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("Program(int i, int j)")

                state.SendBackspace()
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("Program(int i, int j)")

                state.SendTypeChars("""""")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("Program(string name)")
            End Using
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545488")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestUseBestOverload(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document><![CDATA[
class Program
{
    static void Main()
    {
        F$$
    }
    static void F(int i) { }
    static void F(string s) { }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                ' We don't have a definite symbol, so default to first
                state.SendTypeChars("(")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("void Program.F(int i)")
                Assert.Equal(2, state.GetSignatureHelpItems().Count)
                Assert.Equal({"void Program.F(int i)", "void Program.F(string s)"},
                             state.GetSignatureHelpItems().Select(Function(i) i.ToString()))

                ' We now have a definite symbol (the string overload)
                state.SendTypeChars("""""")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("void Program.F(string s)")
                Assert.Equal(2, state.GetSignatureHelpItems().Count)

                ' We stick with the last selection after deleting
                state.SendBackspace()
                state.SendBackspace()
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("void Program.F(string s)")
                Assert.Equal(2, state.GetSignatureHelpItems().Count)

                ' We now have a definite symbol (the int overload)
                state.SendTypeChars("1")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("void Program.F(int i)")
                Assert.Equal(2, state.GetSignatureHelpItems().Count)
            End Using
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545488")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestForgetSelectedItemWhenNoneAreViable(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document><![CDATA[
class Program
{
    static void Main(string[] args)
    {
        F$$
    }
    static void F(int i) { }
    static void F(string s) { }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                ' We don't have a definite symbol, so default to first
                state.SendTypeChars("(")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("void Program.F(int i)")
                Assert.Equal(2, state.GetSignatureHelpItems().Count)
                Assert.Equal({"void Program.F(int i)", "void Program.F(string s)"},
                             state.GetSignatureHelpItems().Select(Function(i) i.ToString()))

                ' We now have a definite symbol (the string overload)
                state.SendTypeChars("""""")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("void Program.F(string s)")
                Assert.Equal(2, state.GetSignatureHelpItems().Count)

                ' We don't have a definite symbol again, so we stick with last selection
                state.SendTypeChars(",")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("void Program.F(string s)")
                Assert.Equal(2, state.GetSignatureHelpItems().Count)
            End Using
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/691648")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestKeepUserSelectedItem(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document><![CDATA[
class C
{
    void M()
    {
        M$$
    }
 
    void M(int i) {  }
    void M(int i, int j) { }
    void M(int i, int j, int k) { }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("(")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("void C.M()")
                Assert.Equal(4, state.GetSignatureHelpItems().Count)

                If showCompletionInArgumentLists Then
                    Await state.AssertCompletionSession()
                    state.SendEscape()
                End If

                state.SendUpKey()
                Await state.AssertSelectedSignatureHelpItem("void C.M(int i, int j, int k)")

                state.SendTypeChars("1")
                Await state.AssertSelectedSignatureHelpItem("void C.M(int i, int j, int k)")

                state.SendTypeChars(",")
                Await state.AssertSelectedSignatureHelpItem("void C.M(int i, int j, int k)")

                state.SendTypeChars("2,")
                Await state.AssertSelectedSignatureHelpItem("void C.M(int i, int j, int k)")
            End Using
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/691648")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestKeepUserSelectedItem2(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document><![CDATA[
class C
{
    void M()
    {
        M$$
    }

    void M(int i) {  }
    void M(int i, int j) { }
    void M(int i, string x) { }
    void M(int i, int j, int k) { }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("(")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("void C.M()")
                Assert.Equal(5, state.GetSignatureHelpItems().Count)

                state.SendTypeChars("1, ")
                Await state.AssertSelectedSignatureHelpItem("void C.M(int i, int j)")

                If showCompletionInArgumentLists Then
                    Await state.AssertCompletionSession()
                    state.SendEscape()
                End If

                state.SendDownKey()
                Await state.AssertSelectedSignatureHelpItem("void C.M(int i, string x)")

                state.SendTypeChars("1")
                Await state.AssertSelectedSignatureHelpItem("void C.M(int i, string x)")
            End Using
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/691648")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestKeepUserSelectedItem3(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document><![CDATA[
class C
{
    void M()
    {
        M$$
    }

    void M(int i) {  }
    void M(int i, int j) { }
    void M(int i, string x) { }
    void M(int i, int j, int k) { }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("(")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("void C.M()")
                Assert.Equal(5, state.GetSignatureHelpItems().Count)

                state.SendTypeChars("1, """" ")
                Await state.AssertSelectedSignatureHelpItem("void C.M(int i, string x)")

                state.SendUpKey()
                Await state.AssertSelectedSignatureHelpItem("void C.M(int i, int j)")

                state.SendTypeChars(",")
                Await state.AssertSelectedSignatureHelpItem("void C.M(int i, int j, int k)")

                state.SendBackspace()
                Await state.AssertSelectedSignatureHelpItem("void C.M(int i, string x)")
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/25830")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestPathIndependent(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document><![CDATA[
class C
{
    void M()
    {
        M$$
    }

    void M(int i) {  }
    void M(int i, int j) { }
    void M(int i, string x) { }
    void M(int i, int j, int k) { }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("(")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("void C.M()")
                Assert.Equal(5, state.GetSignatureHelpItems().Count)
                Assert.Equal({"void C.M()", "void C.M(int i)", "void C.M(int i, int j)", "void C.M(int i, string x)", "void C.M(int i, int j, int k)"},
                             state.GetSignatureHelpItems().Select(Function(i) i.ToString()))

                state.SendTypeChars("1")
                Await state.AssertSelectedSignatureHelpItem("void C.M(int i)")

                state.SendTypeChars(",")
                Await state.AssertSelectedSignatureHelpItem("void C.M(int i, int j)")

                state.SendTypeChars(" ""a"" ")
                Await state.AssertSelectedSignatureHelpItem("void C.M(int i, string x)")
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/25830")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestPathIndependent2(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document><![CDATA[
class C
{
    void M()
    {
        M$$
    }

    void M(int i) {  }
    void M(int i, int j) { }
    void M(int i, string x) { }
    void M(int i, int j, int k) { }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("(")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("void C.M()")
                Assert.Equal(5, state.GetSignatureHelpItems().Count)
                Assert.Equal({"void C.M()", "void C.M(int i)", "void C.M(int i, int j)", "void C.M(int i, string x)", "void C.M(int i, int j, int k)"},
                             state.GetSignatureHelpItems().Select(Function(i) i.ToString()))

                state.SendTypeChars("1, ""a"" ")
                Await state.AssertSelectedSignatureHelpItem("void C.M(int i, string x)")
            End Using
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819063")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/843508")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/636117")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestSessionMaintainedDuringIndexerErrorToleranceTransition(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document><![CDATA[
class Program
{
    void M(int x)
    {
        string s = "Test";
        s$$
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("[")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("char string[int index]")

                state.SendTypeChars("x")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("char string[int index]")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function TestSigHelpInLinkedFiles(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" CommonReferences="true" AssemblyName="CSProj" PreprocessorSymbols="Proj1">
                        <Document FilePath="C.cs">
class C
{
    void M()
    {
        M2($$);
    }

#if Proj1
    void M2(int x) { }
#endif
#if Proj2
    void M2(string x) { }
#endif
}
                              </Document>
                    </Project>
                    <Project Language="C#" CommonReferences="true" PreprocessorSymbols="Proj2">
                        <Document IsLinkFile="true" LinkAssemblyName="CSProj" LinkFilePath="C.cs"/>
                    </Project>
                </Workspace>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                Dim documents = state.Workspace.Documents
                Dim linkDocument = documents.Single(Function(d) d.IsLinkFile)

                state.SendInvokeSignatureHelp()
                Await state.AssertSelectedSignatureHelpItem("void C.M2(int x)")
                state.SendEscape()
                state.Workspace.SetDocumentContext(linkDocument.Id)
                state.SendInvokeSignatureHelp()
                Await state.AssertSelectedSignatureHelpItem("void C.M2(string x)")
            End Using
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1060850")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestSigHelpNotDismissedAfterQuote(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document><![CDATA[
class C
{
    void M()
    {
    }

    void M(string s)
    {
        M($$);
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeSignatureHelp()
                Await state.AssertSelectedSignatureHelpItem("void C.M()")
                state.SendTypeChars("""")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("void C.M(string s)")
            End Using
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1060850")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestSigHelpDismissedAfterComment(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document><![CDATA[
class C
{
    void M()
    {
    }

    void M(string s)
    {
        M($$);
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeSignatureHelp()
                Await state.AssertSelectedSignatureHelpItem("void C.M()")
                state.SendTypeChars("//")
                Await state.AssertNoSignatureHelpSession()
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/1598")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestGenericNameSigHelpInTypeParameterListAfterConditionalAccess(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document><![CDATA[
using System.Collections;
using System.Collections.Generic;
using System.Linq;

class C
{
    void M(object[] args)
    {
        var x = args?.OfType$$
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("<")
                Await state.AssertSelectedSignatureHelpItem($"({CSharpFeaturesResources.extension}) IEnumerable<TResult> IEnumerable.OfType<TResult>()")
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/1598")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestGenericNameSigHelpInTypeParameterListAfterMultipleConditionalAccess(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document><![CDATA[
using System.Collections;
using System.Collections.Generic;
using System.Linq;

class C
{
    void M(object[] args)
    {
        var x = args?.Select(a => a)?.OfType$$
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("<")
                Await state.AssertSelectedSignatureHelpItem($"({CSharpFeaturesResources.extension}) IEnumerable<TResult> IEnumerable.OfType<TResult>()")
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/1598")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestGenericNameSigHelpInTypeParameterListMuchAfterConditionalAccess(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document><![CDATA[
using System.Collections;
using System.Collections.Generic;
using System.Linq;

class C
{
    void M(object[] args)
    {
        var x = args?.Select(a => a).Where(_ => true).OfType$$
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("<")
                Await state.AssertSelectedSignatureHelpItem($"({CSharpFeaturesResources.extension}) IEnumerable<TResult> IEnumerable.OfType<TResult>()")
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/1598")>
        <WpfTheory, CombinatorialData>
        Public Async Function TestGenericNameSigHelpInTypeParameterListAfterConditionalAccessAndNullCoalesce(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document><![CDATA[
using System.Collections;
using System.Collections.Generic;
using System.Linq;

class C
{
    void M(object[] args)
    {
        var x = (args ?? args)?.OfType$$
    }
}
]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("<")
                Await state.AssertSelectedSignatureHelpItem($"({CSharpFeaturesResources.extension}) IEnumerable<TResult> IEnumerable.OfType<TResult>()")
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/5174")>
        <WpfTheory, CombinatorialData>
        Public Async Function DoNotShowSignatureHelpIfOptionIsTurnedOffUnlessExplicitlyInvoked(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void M(int i)
    {
        M$$
    }
}
                              </Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                ' disable implicit sig help then type a trigger character -> no session should be available
                state.Workspace.GetService(Of IGlobalOptionService).SetGlobalOption(SignatureHelpViewOptionsStorage.ShowSignatureHelp, LanguageNames.CSharp, False)

                state.SendTypeChars("(")
                Await state.AssertNoSignatureHelpSession()

                ' force-invoke -> session should be available
                state.SendInvokeSignatureHelp()
                Await state.AssertSignatureHelpSession()
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function MixedTupleNaming(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void Goo()
    {
        (int, int x) t = (5$$
    }
}
                              </Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars(",")
                Await state.AssertSelectedSignatureHelpItem(displayText:="(int, int x)", selectedParameter:="int x")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function ParameterSelectionWhileParsedAsParenthesizedExpression(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void Goo()
    {
        (int a, string b) x = (b$$
    }
}
                              </Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeSignatureHelp()
                Await state.AssertSelectedSignatureHelpItem(displayText:="(int a, string b)", selectedParameter:="int a")
            End Using
        End Function

    End Class
End Namespace
