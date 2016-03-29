' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CSharp
Imports Microsoft.CodeAnalysis.Editor.Options

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Public Class CSharpSignatureHelpCommandHandlerTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestCreateAndDismiss() As Task
            Using state = TestState.CreateCSharpTestState(
                              <Document>
class C
{
    void Foo()
    {
        Foo$$
    }
}
                              </Document>)

                state.SendTypeChars("(")
                Await state.AssertSelectedSignatureHelpItem(displayText:="void C.Foo()")
                state.SendTypeChars(")")
                Await state.AssertNoSignatureHelpSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TypingUpdatesParameters() As Task
            Using state = TestState.CreateCSharpTestState(
                              <Document>
class C
{
    void Foo(int i, string j)
    {
        Foo$$
    }
}
                              </Document>)

                state.SendTypeChars("(")
                Await state.AssertSelectedSignatureHelpItem(displayText:="void C.Foo(int i, string j)", selectedParameter:="int i")
                state.SendTypeChars("1,")
                Await state.AssertSelectedSignatureHelpItem(displayText:="void C.Foo(int i, string j)", selectedParameter:="string j")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TypingChangeParameterByNavigating() As Task
            Using state = TestState.CreateCSharpTestState(
                              <Document>
class C
{
    void Foo(int i, string j)
    {
        Foo(1$$
    }
}
                              </Document>)

                state.SendTypeChars(",")
                Await state.AssertSelectedSignatureHelpItem(displayText:="void C.Foo(int i, string j)", selectedParameter:="string j")
                state.SendLeftKey()
                Await state.AssertSelectedSignatureHelpItem(displayText:="void C.Foo(int i, string j)", selectedParameter:="int i")
                state.SendRightKey()
                Await state.AssertSelectedSignatureHelpItem(displayText:="void C.Foo(int i, string j)", selectedParameter:="string j")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function NavigatingOutOfSpanDismissesSignatureHelp() As Task
            Using state = TestState.CreateCSharpTestState(
                              <Document>
class C
{
    void Foo()
    {
        Foo($$)
    }
}
                              </Document>)

                state.SendInvokeSignatureHelp()
                Await state.AssertSelectedSignatureHelpItem(displayText:="void C.Foo()")
                state.SendRightKey()
                Await state.AssertNoSignatureHelpSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestNestedCalls() As Task
            Using state = TestState.CreateCSharpTestState(
                              <Document>
class C
{
    void Bar() { }
    void Foo()
    {
        Foo$$
    }
}
                              </Document>)

                state.SendTypeChars("(")
                Await state.AssertSelectedSignatureHelpItem(displayText:="void C.Foo()")
                state.SendTypeChars("Bar(")
                Await state.AssertSelectedSignatureHelpItem(displayText:="void C.Bar()")
                state.SendTypeChars(")")
                Await state.AssertSelectedSignatureHelpItem(displayText:="void C.Foo()")
            End Using
        End Function

        <WorkItem(544547, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544547")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestNoSigHelpOnGenericNamespace() As Task
            Using state = TestState.CreateCSharpTestState(
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
            Using state = TestState.CreateCSharpTestState(
                              <Document>
class G&lt;S, T&gt; { };

class C
{
    void Foo()
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
            Using state = TestState.CreateCSharpTestState(
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
                Assert.Equal(2, state.CurrentSignatureHelpPresenterSession.SignatureHelpItems.Count)

                state.SendTypeChars(":")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("void C.M(int first, int second)")
                Assert.Equal(1, state.CurrentSignatureHelpPresenterSession.SignatureHelpItems.Count)

                ' Keep the same item selected when the colon is deleted, but now both items are
                ' available again.
                state.SendBackspace()
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("void C.M(int first, int second)")
                Assert.Equal(2, state.CurrentSignatureHelpPresenterSession.SignatureHelpItems.Count)
            End Using
        End Function

        <WorkItem(545488, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545488")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestKeepSelectedItemWhenNoneAreViable() As Task
            Using state = TestState.CreateCSharpTestState(
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

                state.SendTypeChars("(")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("void Program.F(int i)")
                Assert.Equal(2, state.CurrentSignatureHelpPresenterSession.SignatureHelpItems.Count)

                state.SendTypeChars(""""",")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("void Program.F(int i)")
                Assert.Equal(2, state.CurrentSignatureHelpPresenterSession.SignatureHelpItems.Count)
            End Using
        End Function

        <WorkItem(691648, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/691648")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestKeepSelectedItemAfterComma() As Task
            Using state = TestState.CreateCSharpTestState(
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
                Assert.Equal(4, state.CurrentSignatureHelpPresenterSession.SignatureHelpItems.Count)

                state.SendUpKey()
                Await state.AssertSelectedSignatureHelpItem("void C.M(int i, int j, int k)")

                state.SendTypeChars("1, ")
                Await state.AssertSelectedSignatureHelpItem("void C.M(int i, int j, int k)")
            End Using
        End Function

        <WorkItem(819063, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/819063")>
        <WorkItem(843508, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/843508")>
        <WorkItem(636117, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/636117")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestSessionMaintainedDuringIndexerErrorToleranceTransition() As Task
            Using state = TestState.CreateCSharpTestState(
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
            Using state = TestState.CreateTestStateFromWorkspace(
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
            Using state = TestState.CreateCSharpTestState(
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
            Using state = TestState.CreateCSharpTestState(
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
            Using state = TestState.CreateCSharpTestState(
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
                Await state.AssertSelectedSignatureHelpItem($"({CSharpFeaturesResources.Extension}) IEnumerable<TResult> IEnumerable.OfType<TResult>()")
            End Using
        End Function

        <WorkItem(1598, "https://github.com/dotnet/roslyn/issues/1598")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestGenericNameSigHelpInTypeParameterListAfterMultipleConditionalAccess() As Task
            Using state = TestState.CreateCSharpTestState(
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
                Await state.AssertSelectedSignatureHelpItem($"({CSharpFeaturesResources.Extension}) IEnumerable<TResult> IEnumerable.OfType<TResult>()")
            End Using
        End Function

        <WorkItem(1598, "https://github.com/dotnet/roslyn/issues/1598")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestGenericNameSigHelpInTypeParameterListMuchAfterConditionalAccess() As Task
            Using state = TestState.CreateCSharpTestState(
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
                Await state.AssertSelectedSignatureHelpItem($"({CSharpFeaturesResources.Extension}) IEnumerable<TResult> IEnumerable.OfType<TResult>()")
            End Using
        End Function

        <WorkItem(1598, "https://github.com/dotnet/roslyn/issues/1598")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestGenericNameSigHelpInTypeParameterListAfterConditionalAccessAndNullCoalesce() As Task
            Using state = TestState.CreateCSharpTestState(
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
                Await state.AssertSelectedSignatureHelpItem($"({CSharpFeaturesResources.Extension}) IEnumerable<TResult> IEnumerable.OfType<TResult>()")
            End Using
        End Function

        <WorkItem(5174, "https://github.com/dotnet/roslyn/issues/5174")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function DontShowSignatureHelpIfOptionIsTurnedOffUnlessExplicitlyInvoked() As Task
            Using state = TestState.CreateCSharpTestState(
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
                state.Workspace.Options = state.Workspace.Options.WithChangedOption(SignatureHelpOptions.ShowSignatureHelp, "C#", False)
                state.SendTypeChars("(")
                Await state.AssertNoSignatureHelpSession()

                ' force-invoke -> session should be available
                state.SendInvokeSignatureHelp()
                Await state.AssertSignatureHelpSession()
            End Using
        End Function
    End Class
End Namespace
