Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    Public Class VisualBasicCompletionCommandHandlerTests_InternalsVisibleTo

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionContainsOtherAssembliesOfSolution() As Task
            Using state = TestState.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="ClassLibrary1"/>
                    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="ClassLibrary2"/>
                    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="ClassLibrary3"/>
                    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="A.vb"><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("$$
]]>
                        </Document>
                    </Project>
                </Workspace>)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                Assert.True(state.CompletionItemsContainsAll({"ClassLibrary1", "ClassLibrary2", "ClassLibrary3"}))
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionContainsOtherAssemblyIfAttributeSuffixIsPresent() As Task
            Using state = TestState.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="ClassLibrary1"/>
                    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="A.vb"><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleToAttribute("$$
]]>
                        </Document>
                    </Project>
                </Workspace>)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                Assert.True(state.CompletionItemsContainsAll({"ClassLibrary1"}))
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionIsTriggeredWhenDoubleQuoteIsEntered() As Task
            Using state = TestState.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="ClassLibrary1"/>
                    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="A.vb"><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo($$
]]>
                        </Document>
                    </Project>
                </Workspace>)
                Await state.AssertNoCompletionSession()
                state.SendTypeChars(""""c)
                Await state.AssertCompletionSession()
                Assert.True(state.CompletionItemsContainsAll({"ClassLibrary1"}))
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionContainsIsEmptyUntilDoubleQuotesAreEntered() As Task
            Using state = TestState.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="ClassLibrary1"/>
                    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="A.vb"><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo$$
]]>
                        </Document>
                    </Project>
                </Workspace>)
                Await state.AssertNoCompletionSession()
                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync()
                Assert.False(state.CompletionItemsContainsAny({"ClassLibrary1"}))
                state.SendTypeChars("("c)
                Await state.WaitForAsynchronousOperationsAsync()
                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync()
                Assert.False(state.CompletionItemsContainsAny({"ClassLibrary1"}))
                state.SendTypeChars(""""c)
                Await state.AssertCompletionSession()
                Assert.True(state.CompletionItemsContainsAll({"ClassLibrary1"}))
            End Using
        End Function

        Private Async Function AssertCompletionListHasItems(code As String, hasItems As Boolean) As Task
            Using state = TestState.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="ClassLibrary1"/>
                    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="A.vb">
Imports System.Runtime.CompilerServices
Imports System.Reflection
<%= code %>
                        </Document>
                    </Project>
                </Workspace>)
                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync()
                If hasItems Then
                    Await state.AssertCompletionSession()
                    Assert.True(state.CompletionItemsContainsAll({"ClassLibrary1"}))
                Else
                    If Not state.CurrentCompletionPresenterSession Is Nothing Then
                        Assert.False(state.CompletionItemsContainsAny({"ClassLibrary1"}))
                    End If
                End If
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AssertCompletionListHasItems_AfterSingleDoubleQuoteAndClosing() As Task
            Await AssertCompletionListHasItems("<Assembly: InternalsVisibleTo(""$$)>", True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AssertCompletionListHasItems_AfterText() As Task
            Await AssertCompletionListHasItems("<Assembly: InternalsVisibleTo(""Test$$)>", True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AssertCompletionListHasItems_IfCursorIsInSecondParameter() As Task
            Await AssertCompletionListHasItems("<Assembly: InternalsVisibleTo(""Test"", ""$$", True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AssertCompletionListHasNoItems_IfCursorIsClosingDoubleQuote1() As Task
            Await AssertCompletionListHasItems("<Assembly: InternalsVisibleTo(""Test""$$", False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AssertCompletionListHasNoItems_IfCursorIsClosingDoubleQuote2() As Task
            Await AssertCompletionListHasItems("<Assembly: InternalsVisibleTo(""""$$", False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AssertCompletionListHasItems_IfNamedParameterIsPresent() As Task
            Await AssertCompletionListHasItems("<Assembly: InternalsVisibleTo(""$$, AllInternalsVisible:=True)>", True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AssertCompletionListHasNoItems_IfNotInternalsVisibleToAttribute() As Task
            Await AssertCompletionListHasItems("<Assembly: AssemblyVersion(""$$"")>", False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AssertCompletionListHasItems_IfOtherAttributeIsPresent1() As Task
            Await AssertCompletionListHasItems("<Assembly: AssemblyVersion(""1.0.0.0""), InternalsVisibleTo(""$$", True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AssertCompletionListHasItems_IfOtherAttributeIsPresent2() As Task
            Await AssertCompletionListHasItems("<Assembly: InternalsVisibleTo(""$$""), AssemblyVersion(""1.0.0.0"")>", True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AssertCompletionListHasItems_IfOtherAttributesAreAhead() As Task
            Await AssertCompletionListHasItems("
                <Assembly: AssemblyVersion(""1.0.0.0"")>
                <Assembly: InternalsVisibleTo(""$$", True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AssertCompletionListHasItems_IfOtherAttributesAreFollowing() As Task
            Await AssertCompletionListHasItems("
            <Assembly: InternalsVisibleTo(""$$
            <Assembly: AssemblyVersion(""1.0.0.0"")>
            <Assembly: AssemblyCompany(""Test"")>", True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AssertCompletionListHasItems_IfNamespaceIsFollowing() As Task
            Await AssertCompletionListHasItems("
            <Assembly: InternalsVisibleTo(""$$
            Namespace A             
                Public Class A
                End Class
            End Namespace", True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionHasItemsIfInteralVisibleToIsReferencedByTypeAlias() As Task
            Using state = TestState.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="ClassLibrary1"/>
                    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="A.vb"><![CDATA[
Imports IVT = System.Runtime.CompilerServices.InternalsVisibleToAttribute
<Assembly: IVT("$$
]]>
                        </Document>
                    </Project>
                </Workspace>)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                Assert.True(state.CompletionItemsContainsAll({"ClassLibrary1"}))
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionDoesNotContainCurrentAssembly() As Task
            Using state = TestState.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="ClassLibrary1"/>
                    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="A.vb"><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("$$")>
]]>
                        </Document>
                    </Project>
                </Workspace>)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                Assert.False(state.CompletionItemsContainsAny({"TestAssembly"}))
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionInsertsAssemblyNameOnCommit() As Task
            Using state = TestState.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="ClassLibrary1">
                    </Project>
                    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("$$")>
]]>
                        </Document>
                    </Project>
                </Workspace>)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("ClassLibrary1")
                state.SendTab()
                Await state.WaitForAsynchronousOperationsAsync()
                state.AssertMatchesTextStartingAtLine(1, "<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ClassLibrary1"")>")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionInsertsPublicKeyOnCommit() As Task
            Using strongKeyFileFixture = New StrongNameKeyFileFixture
                Using state = TestState.CreateTestStateFromWorkspace(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true" AssemblyName="ClassLibrary1">
                            <CompilationOptions CryptoKeyFile=<%= strongKeyFileFixture.StrongKeyFile %> StrongNameProvider="Microsoft.CodeAnalysis.DesktopStrongNameProvider,Microsoft.CodeAnalysis"/>
                        </Project>
                        <Project Language="Visual Basic" CommonReferences="true" AssemblyName="TestAssembly">
                            <Document><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("$$")>
]]>
                            </Document>
                        </Project>
                    </Workspace>)
                    state.SendInvokeCompletionList()
                    Await state.AssertSelectedCompletionItem("ClassLibrary1")
                    state.SendTab()
                    Await state.WaitForAsynchronousOperationsAsync()
                    state.AssertMatchesTextStartingAtLine(1, "<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ClassLibrary1, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c5c26d5ff8f59b47acc55d4feb5c19009317137e55a34ca2a107ac29b3badc6870d858cb2fbf0a71a04caf749f0517dc31a89b85b004f030ed3e785bb45090499682fb0b2a78e02a9b94de1e778e0611648d0925f3c1e6c76d099f668a79b1868940a20a48c7695ffcfb75fe0946692aa8dedafe727e3cbc07f64d646d2ef6f5"")>")
                End Using
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionContainsPublicKeyIfKeyIsSpecifiedByAttribute() As Task
            Using strongKeyFileFixture = New StrongNameKeyFileFixture
                Using state = TestState.CreateTestStateFromWorkspace(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true" AssemblyName="ClassLibrary1">
                            <CompilationOptions StrongNameProvider="Microsoft.CodeAnalysis.DesktopStrongNameProvider,Microsoft.CodeAnalysis"/>
                            <Document>
                                &lt;Assembly: System.Reflection.AssemblyKeyFile("<%= strongKeyFileFixture.StrongKeyFile %>")&gt;
                            </Document>
                        </Project>
                        <Project Language="Visual Basic" CommonReferences="true" AssemblyName="TestAssembly">
                            <Document><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("$$")>
]]>
                            </Document>
                        </Project>
                    </Workspace>)
                    state.SendInvokeCompletionList()
                    Await state.AssertSelectedCompletionItem("ClassLibrary1")
                    state.SendTab()
                    Await state.WaitForAsynchronousOperationsAsync()
                    state.AssertMatchesTextStartingAtLine(1, "<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ClassLibrary1, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c5c26d5ff8f59b47acc55d4feb5c19009317137e55a34ca2a107ac29b3badc6870d858cb2fbf0a71a04caf749f0517dc31a89b85b004f030ed3e785bb45090499682fb0b2a78e02a9b94de1e778e0611648d0925f3c1e6c76d099f668a79b1868940a20a48c7695ffcfb75fe0946692aa8dedafe727e3cbc07f64d646d2ef6f5"")>")
                End Using
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionContainsPublicKeyIfDelayedSigningIsEnabled() As Task
            Using strongKeyFileFixture = New StrongNameKeyFileFixture
                Using state = TestState.CreateTestStateFromWorkspace(
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true" AssemblyName="ClassLibrary1">
                            <CompilationOptions
                                CryptoKeyFile=<%= strongKeyFileFixture.StrongKeyFile %>
                                StrongNameProvider="Microsoft.CodeAnalysis.DesktopStrongNameProvider,Microsoft.CodeAnalysis"
                                DelaySign="True"
                            />
                        </Project>
                        <Project Language="Visual Basic" CommonReferences="true" AssemblyName="TestAssembly">
                            <Document><![CDATA[
<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("$$")>
]]>
                            </Document>
                        </Project>
                    </Workspace>)
                    state.SendInvokeCompletionList()
                    Await state.AssertSelectedCompletionItem("ClassLibrary1")
                    state.SendTab()
                    Await state.WaitForAsynchronousOperationsAsync()
                    state.AssertMatchesTextStartingAtLine(1, "<Assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ClassLibrary1, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c5c26d5ff8f59b47acc55d4feb5c19009317137e55a34ca2a107ac29b3badc6870d858cb2fbf0a71a04caf749f0517dc31a89b85b004f030ed3e785bb45090499682fb0b2a78e02a9b94de1e778e0611648d0925f3c1e6c76d099f668a79b1868940a20a48c7695ffcfb75fe0946692aa8dedafe727e3cbc07f64d646d2ef6f5"")>")
                End Using
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionListIsEmptyIfAttributeIsNotTheBCLAttribute() As Task
            Using state = TestState.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="ClassLibrary1"/>
                    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="A.vb"><![CDATA[
<Assembly: Test.InternalsVisibleTo("$$")>
Namespace Test
	<System.AttributeUsage(System.AttributeTargets.Assembly)> _
	Public NotInheritable Class InternalsVisibleToAttribute
		Inherits System.Attribute

		Public Sub New(ignore As String)
		End Sub
	End Class
End Namespace
]]>
                        </Document>
                    </Project>
                </Workspace>)
                state.SendInvokeCompletionList()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

    End Class
End Namespace
