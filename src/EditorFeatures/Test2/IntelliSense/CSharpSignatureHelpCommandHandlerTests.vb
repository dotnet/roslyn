' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CSharp
Imports Microsoft.CodeAnalysis.Editor.Options

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Public Class CSharpSignatureHelpCommandHandlerTests
        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestCreateAndDismiss()
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
                state.AssertSelectedSignatureHelpItem(displayText:="void C.Foo()")
                state.SendTypeChars(")")
                state.AssertNoSignatureHelpSession()
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TypingUpdatesParameters()
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
                state.AssertSelectedSignatureHelpItem(displayText:="void C.Foo(int i, string j)", selectedParameter:="int i")
                state.SendTypeChars("1,")
                state.AssertSelectedSignatureHelpItem(displayText:="void C.Foo(int i, string j)", selectedParameter:="string j")
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TypingChangeParameterByNavigating()
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
                state.AssertSelectedSignatureHelpItem(displayText:="void C.Foo(int i, string j)", selectedParameter:="string j")
                state.SendLeftKey()
                state.AssertSelectedSignatureHelpItem(displayText:="void C.Foo(int i, string j)", selectedParameter:="int i")
                state.SendRightKey()
                state.AssertSelectedSignatureHelpItem(displayText:="void C.Foo(int i, string j)", selectedParameter:="string j")
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub NavigatingOutOfSpanDismissesSignatureHelp()
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
                state.AssertSelectedSignatureHelpItem(displayText:="void C.Foo()")
                state.SendRightKey()
                state.AssertNoSignatureHelpSession()
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestNestedCalls()
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
                state.AssertSelectedSignatureHelpItem(displayText:="void C.Foo()")
                state.SendTypeChars("Bar(")
                state.AssertSelectedSignatureHelpItem(displayText:="void C.Bar()")
                state.SendTypeChars(")")
                state.AssertSelectedSignatureHelpItem(displayText:="void C.Foo()")
            End Using
        End Sub

        <WorkItem(544547)>
        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestNoSigHelpOnGenericNamespace()
            Using state = TestState.CreateCSharpTestState(
                              <Document>
namespace global::F$$
                              </Document>)

                state.SendTypeChars("<")
                state.AssertNoSignatureHelpSession()
            End Using
        End Sub

        <WorkItem(544547)>
        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestSigHelpOnExtraSpace()
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
                state.AssertSignatureHelpSession()
            End Using
        End Sub

        <WorkItem(544551)>
        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestFilterOnNamedParameters1()
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
                state.AssertSignatureHelpSession()
                state.AssertSelectedSignatureHelpItem("void C.M(int third)")
                Assert.Equal(2, state.CurrentSignatureHelpPresenterSession.SignatureHelpItems.Count)

                state.SendTypeChars(":")
                state.AssertSignatureHelpSession()
                state.AssertSelectedSignatureHelpItem("void C.M(int first, int second)")
                Assert.Equal(1, state.CurrentSignatureHelpPresenterSession.SignatureHelpItems.Count)

                ' Keep the same item selected when the colon is deleted, but now both items are
                ' available again.
                state.SendBackspace()
                state.AssertSignatureHelpSession()
                state.AssertSelectedSignatureHelpItem("void C.M(int first, int second)")
                Assert.Equal(2, state.CurrentSignatureHelpPresenterSession.SignatureHelpItems.Count)
            End Using
        End Sub

        <WorkItem(545488)>
        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestKeepSelectedItemWhenNoneAreViable()
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
                state.AssertSignatureHelpSession()
                state.AssertSelectedSignatureHelpItem("void Program.F(int i)")
                Assert.Equal(2, state.CurrentSignatureHelpPresenterSession.SignatureHelpItems.Count)

                state.SendTypeChars(""""",")
                state.AssertSignatureHelpSession()
                state.AssertSelectedSignatureHelpItem("void Program.F(int i)")
                Assert.Equal(2, state.CurrentSignatureHelpPresenterSession.SignatureHelpItems.Count)
            End Using
        End Sub

        <WorkItem(691648)>
        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestKeepSelectedItemAfterComma()
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
                state.AssertSignatureHelpSession()
                state.AssertSelectedSignatureHelpItem("void C.M()")
                Assert.Equal(4, state.CurrentSignatureHelpPresenterSession.SignatureHelpItems.Count)

                state.SendUpKey()
                state.AssertSelectedSignatureHelpItem("void C.M(int i, int j, int k)")

                state.SendTypeChars("1, ")
                state.AssertSelectedSignatureHelpItem("void C.M(int i, int j, int k)")
            End Using
        End Sub

        <WorkItem(819063)>
        <WorkItem(843508)>
        <WorkItem(636117)>
        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestSessionMaintainedDuringIndexerErrorToleranceTransition()
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
                state.AssertSignatureHelpSession()
                state.AssertSelectedSignatureHelpItem("char string[int index]")

                state.SendTypeChars("x")
                state.AssertSignatureHelpSession()
                state.AssertSelectedSignatureHelpItem("char string[int index]")
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestSigHelpInLinkedFiles()
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
                state.AssertSelectedSignatureHelpItem("void C.M2(int x)")
                state.SendEscape()
                state.Workspace.SetDocumentContext(linkDocument.Id)
                state.SendInvokeSignatureHelp()
                state.AssertSelectedSignatureHelpItem("void C.M2(string x)")
            End Using
        End Sub

        <WorkItem(1060850)>
        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestSigHelpNotDismissedAfterQuote()
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
                state.AssertSelectedSignatureHelpItem("void C.M()")
                state.SendTypeChars("""")
                state.AssertSignatureHelpSession()
                state.AssertSelectedSignatureHelpItem("void C.M(string s)")
            End Using
        End Sub

        <WorkItem(1060850)>
        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestSigHelpDismissedAfterComment()
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
                state.AssertSelectedSignatureHelpItem("void C.M()")
                state.SendTypeChars("//")
                state.AssertNoSignatureHelpSession()
            End Using
        End Sub

        <WorkItem(1598, "https://github.com/dotnet/roslyn/issues/1598")>
        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestGenericNameSigHelpInTypeParameterListAfterConditionalAccess()
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
                state.AssertSelectedSignatureHelpItem($"({CSharpFeaturesResources.Extension}) IEnumerable<TResult> IEnumerable.OfType<TResult>()")
            End Using
        End Sub

        <WorkItem(1598, "https://github.com/dotnet/roslyn/issues/1598")>
        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestGenericNameSigHelpInTypeParameterListAfterMultipleConditionalAccess()
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
                state.AssertSelectedSignatureHelpItem($"({CSharpFeaturesResources.Extension}) IEnumerable<TResult> IEnumerable.OfType<TResult>()")
            End Using
        End Sub

        <WorkItem(1598, "https://github.com/dotnet/roslyn/issues/1598")>
        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestGenericNameSigHelpInTypeParameterListMuchAfterConditionalAccess()
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
                state.AssertSelectedSignatureHelpItem($"({CSharpFeaturesResources.Extension}) IEnumerable<TResult> IEnumerable.OfType<TResult>()")
            End Using
        End Sub

        <WorkItem(1598, "https://github.com/dotnet/roslyn/issues/1598")>
        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestGenericNameSigHelpInTypeParameterListAfterConditionalAccessAndNullCoalesce()
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
                state.AssertSelectedSignatureHelpItem($"({CSharpFeaturesResources.Extension}) IEnumerable<TResult> IEnumerable.OfType<TResult>()")
            End Using
        End Sub

        <WorkItem(5174, "https://github.com/dotnet/roslyn/issues/5174")>
        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub DontShowSignatureHelpIfOptionIsTurnedOffUnlessExplicitlyInvoked()
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
                state.AssertNoSignatureHelpSession()

                ' force-invoke -> session should be available
                state.SendInvokeSignatureHelp()
                state.AssertSignatureHelpSession()
            End Using
        End Sub
    End Class
End Namespace
