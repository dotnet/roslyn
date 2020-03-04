﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CSharp
Imports Microsoft.CodeAnalysis.Editor.Options

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <[UseExportProvider]>
    Public Class CSharpSignatureHelpCommandHandlerTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestCreateAndDismiss() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void Goo()
    {
        Goo$$
    }
}
                              </Document>)

                state.SendTypeChars("(")
                Await state.AssertSelectedSignatureHelpItem(displayText:="void C.Goo()")
                state.SendTypeChars(")")
                Await state.AssertNoSignatureHelpSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TypingUpdatesParameters() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void Goo(int i, string j)
    {
        Goo$$
    }
}
                              </Document>)

                state.SendTypeChars("(")
                Await state.AssertSelectedSignatureHelpItem(displayText:="void C.Goo(int i, string j)", selectedParameter:="int i")
                state.SendTypeChars("1,")
                Await state.AssertSelectedSignatureHelpItem(displayText:="void C.Goo(int i, string j)", selectedParameter:="string j")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TypingChangeParameterByNavigating() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void Goo(int i, string j)
    {
        Goo(1$$
    }
}
                              </Document>)

                state.SendTypeChars(",")
                Await state.AssertSelectedSignatureHelpItem(displayText:="void C.Goo(int i, string j)", selectedParameter:="string j")
                state.SendLeftKey()
                Await state.AssertSelectedSignatureHelpItem(displayText:="void C.Goo(int i, string j)", selectedParameter:="int i")
                state.SendRightKey()
                Await state.AssertSelectedSignatureHelpItem(displayText:="void C.Goo(int i, string j)", selectedParameter:="string j")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function NavigatingOutOfSpanDismissesSignatureHelp() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void Goo()
    {
        Goo($$)
    }
}
                              </Document>)

                state.SendInvokeSignatureHelp()
                Await state.AssertSelectedSignatureHelpItem(displayText:="void C.Goo()")
                state.SendRightKey()
                Await state.AssertNoSignatureHelpSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestNestedCalls() As Task
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
                              </Document>)

                state.SendTypeChars("(")
                Await state.AssertSelectedSignatureHelpItem(displayText:="void C.Goo()")
                state.SendTypeChars("Bar(")
                Await state.AssertSelectedSignatureHelpItem(displayText:="void C.Bar()")
                state.SendTypeChars(")")
                Await state.AssertSelectedSignatureHelpItem(displayText:="void C.Goo()")
            End Using
        End Function

        <WorkItem(544547, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544547")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestNoSigHelpOnGenericNamespace() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
namespace global::F$$
                              </Document>)

                state.SendTypeChars("<")
                Await state.AssertNoSignatureHelpSession()
            End Using
        End Function

        <WorkItem(544547, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544547")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestSigHelpOnExtraSpace() As Task
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
                              </Document>)

                state.SendInvokeSignatureHelp()
                Await state.AssertSignatureHelpSession()
            End Using
        End Function

        <WorkItem(544551, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544551")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestFilterOnNamedParameters1() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    public void M(int first, int second) { }
    public void M(int third) { }
}
 
class Program
{
    void Main()
    {
        new C().M(first$$
    }
}

                              </Document>)

                state.SendInvokeSignatureHelp()
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("void C.M(int third)")
                Assert.Equal(2, state.GetSignatureHelpItems().Count)

                state.SendTypeChars(":")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("void C.M(int first, int second)")
                Assert.Equal(1, state.GetSignatureHelpItems().Count)

                ' Now both items are available again, and we're sticking with last selection
                state.SendBackspace()
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("void C.M(int first, int second)")
                Assert.Equal(2, state.GetSignatureHelpItems().Count)
            End Using
        End Function

        <WorkItem(545488, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545488")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestUseBestOverload() As Task
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
]]></Document>)

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

        <WorkItem(545488, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545488")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestForgetSelectedItemWhenNoneAreViable() As Task
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
]]></Document>)

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

        <WorkItem(691648, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/691648")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestKeepUserSelectedItem() As Task
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
]]></Document>)

                state.SendTypeChars("(")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("void C.M()")
                Assert.Equal(4, state.GetSignatureHelpItems().Count)

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

        <WorkItem(691648, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/691648")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestKeepUserSelectedItem2() As Task
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
]]></Document>)

                state.SendTypeChars("(")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("void C.M()")
                Assert.Equal(5, state.GetSignatureHelpItems().Count)

                state.SendTypeChars("1, ")
                Await state.AssertSelectedSignatureHelpItem("void C.M(int i, int j)")

                state.SendDownKey()
                Await state.AssertSelectedSignatureHelpItem("void C.M(int i, string x)")

                state.SendTypeChars("1")
                Await state.AssertSelectedSignatureHelpItem("void C.M(int i, string x)")
            End Using
        End Function

        <WorkItem(691648, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/691648")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestKeepUserSelectedItem3() As Task
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
]]></Document>)

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

        <WorkItem(25830, "https://github.com/dotnet/roslyn/issues/25830")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestPathIndependent() As Task
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
]]></Document>)

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

        <WorkItem(25830, "https://github.com/dotnet/roslyn/issues/25830")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestPathIndependent2() As Task
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
]]></Document>)

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

        <WorkItem(819063, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819063")>
        <WorkItem(843508, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/843508")>
        <WorkItem(636117, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/636117")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestSessionMaintainedDuringIndexerErrorToleranceTransition() As Task
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
]]></Document>)

                state.SendTypeChars("[")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("char string[int index]")

                state.SendTypeChars("x")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("char string[int index]")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestSigHelpInLinkedFiles() As Task
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
                </Workspace>)

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

        <WorkItem(1060850, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1060850")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestSigHelpNotDismissedAfterQuote() As Task
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
]]></Document>)

                state.SendInvokeSignatureHelp()
                Await state.AssertSelectedSignatureHelpItem("void C.M()")
                state.SendTypeChars("""")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("void C.M(string s)")
            End Using
        End Function

        <WorkItem(1060850, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1060850")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestSigHelpDismissedAfterComment() As Task
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
]]></Document>)

                state.SendInvokeSignatureHelp()
                Await state.AssertSelectedSignatureHelpItem("void C.M()")
                state.SendTypeChars("//")
                Await state.AssertNoSignatureHelpSession()
            End Using
        End Function

        <WorkItem(1598, "https://github.com/dotnet/roslyn/issues/1598")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestGenericNameSigHelpInTypeParameterListAfterConditionalAccess() As Task
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
]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertSelectedSignatureHelpItem($"({CSharpFeaturesResources.extension}) IEnumerable<TResult> IEnumerable.OfType<TResult>()")
            End Using
        End Function

        <WorkItem(1598, "https://github.com/dotnet/roslyn/issues/1598")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestGenericNameSigHelpInTypeParameterListAfterMultipleConditionalAccess() As Task
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
]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertSelectedSignatureHelpItem($"({CSharpFeaturesResources.extension}) IEnumerable<TResult> IEnumerable.OfType<TResult>()")
            End Using
        End Function

        <WorkItem(1598, "https://github.com/dotnet/roslyn/issues/1598")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestGenericNameSigHelpInTypeParameterListMuchAfterConditionalAccess() As Task
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
]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertSelectedSignatureHelpItem($"({CSharpFeaturesResources.extension}) IEnumerable<TResult> IEnumerable.OfType<TResult>()")
            End Using
        End Function

        <WorkItem(1598, "https://github.com/dotnet/roslyn/issues/1598")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestGenericNameSigHelpInTypeParameterListAfterConditionalAccessAndNullCoalesce() As Task
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
]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertSelectedSignatureHelpItem($"({CSharpFeaturesResources.extension}) IEnumerable<TResult> IEnumerable.OfType<TResult>()")
            End Using
        End Function

        <WorkItem(5174, "https://github.com/dotnet/roslyn/issues/5174")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function DontShowSignatureHelpIfOptionIsTurnedOffUnlessExplicitlyInvoked() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void M(int i)
    {
        M$$
    }
}
                              </Document>)

                ' disable implicit sig help then type a trigger character -> no session should be available
                Dim workspace = state.Workspace
                workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(workspace.Options _
                    .WithChangedOption(SignatureHelpOptions.ShowSignatureHelp, "C#", False)))
                state.SendTypeChars("(")
                Await state.AssertNoSignatureHelpSession()

                ' force-invoke -> session should be available
                state.SendInvokeSignatureHelp()
                Await state.AssertSignatureHelpSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function MixedTupleNaming() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void Goo()
    {
        (int, int x) t = (5$$
    }
}
                              </Document>)

                state.SendTypeChars(",")
                Await state.AssertSelectedSignatureHelpItem(displayText:="(int, int x)", selectedParameter:="int x")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function ParameterSelectionWhileParsedAsParenthesizedExpression() As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                              <Document>
class C
{
    void Goo()
    {
        (int a, string b) x = (b$$
    }
}
                              </Document>)

                state.SendInvokeSignatureHelp()
                Await state.AssertSelectedSignatureHelpItem(displayText:="(int a, string b)", selectedParameter:="int a")
            End Using
        End Function

    End Class
End Namespace
