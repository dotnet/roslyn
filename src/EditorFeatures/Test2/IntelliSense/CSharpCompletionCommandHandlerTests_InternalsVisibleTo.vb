Imports System.IO
Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    Public Class CSharpCompletionCommandHandlerTests_InternalsVisibleTo

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionContainsOtherAssembliesOfSolution() As Task
            Using state = TestState.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary1"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary2"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary3"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="C.cs">
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("$$
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
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary1"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="C.cs">
[assembly: System.Runtime.CompilerServices.InternalsVisibleToAttribute("$$
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
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary1"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="C.cs">
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo($$
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
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary1"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="C.cs">
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo$$
                        </Document>
                    </Project>
                </Workspace>)
                Await state.AssertNoCompletionSession()
                state.SendInvokeCompletionList()
                Await state.WaitForAsynchronousOperationsAsync()
                Assert.False(state.CompletionItemsContainsAny({"ClassLibrary1"}))
                state.SendTypeChars("("c)
                Await state.AssertNoCompletionSession()
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
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary1"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="C.cs">
using System.Runtime.CompilerServices;
using System.Reflection;
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
                    Await state.AssertNoCompletionSession
                End If
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AssertCompletionListHasItems_AfterSingleDoubleQuoteAndClosing() As Task
            Await AssertCompletionListHasItems("[assembly: InternalsVisibleTo(""$$)]", True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AssertCompletionListHasItems_AfterText() As Task
            Await AssertCompletionListHasItems("[assembly: InternalsVisibleTo(""Test$$)]", True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AssertCompletionListHasItems_IfCursorIsInSecondParameter() As Task
            Await AssertCompletionListHasItems("[assembly: InternalsVisibleTo(""Test"", ""$$", True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AssertCompletionListHasNoItems_IfCursorIsClosingDoubleQuote1() As Task
            Await AssertCompletionListHasItems("[assembly: InternalsVisibleTo(""Test""$$", False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AssertCompletionListHasNoItems_IfCursorIsClosingDoubleQuote2() As Task
            Await AssertCompletionListHasItems("[assembly: InternalsVisibleTo(""""$$", False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AssertCompletionListHasItems_IfNamedParameterIsPresent() As Task
            Await AssertCompletionListHasItems("[assembly: InternalsVisibleTo(""$$, AllInternalsVisible = true)]", True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AssertCompletionListHasItems_IfNamedParameterAndNamedPositionalParametersArePresent() As Task
            Await AssertCompletionListHasItems("[assembly: InternalsVisibleTo(assemblyName: ""$$, AllInternalsVisible = true)]", True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AssertCompletionListHasNoItems_IfNotInternalsVisibleToAttribute() As Task
            Await AssertCompletionListHasItems("[assembly: AssemblyVersion(""$$"")]", False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AssertCompletionListHasItems_IfOtherAttributeIsPresent1() As Task
            Await AssertCompletionListHasItems("[assembly: AssemblyVersion(""1.0.0.0""), InternalsVisibleTo(""$$", True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AssertCompletionListHasItems_IfOtherAttributeIsPresent2() As Task
            Await AssertCompletionListHasItems("[assembly: InternalsVisibleTo(""$$""), AssemblyVersion(""1.0.0.0"")]", True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AssertCompletionListHasItems_IfOtherAttributesAreAhead() As Task
            Await AssertCompletionListHasItems("
                [assembly: AssemblyVersion(""1.0.0.0"")]
                [assembly: InternalsVisibleTo(""$$", True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AssertCompletionListHasItems_IfOtherAttributesAreFollowing() As Task
            Await AssertCompletionListHasItems("
            [assembly: InternalsVisibleTo(""$$
            [assembly: AssemblyVersion(""1.0.0.0"")]
            [assembly: AssemblyCompany(""Test"")]", True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AssertCompletionListHasItems_IfNamespaceIsFollowing() As Task
            Await AssertCompletionListHasItems("
            [assembly: InternalsVisibleTo(""$$
            namespace A {            
                public class A { }
            }", True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionHasItemsIfInteralVisibleToIsReferencedByTypeAlias() As Task
            Using state = TestState.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary1"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="C.cs">
using IVT = System.Runtime.CompilerServices.InternalsVisibleToAttribute;
[assembly: IVT("$$
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
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary1"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="C.cs">
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("$$")]
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
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary1">
                    </Project>
                    <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document>
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("$$")]
                        </Document>
                    </Project>
                </Workspace>)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("ClassLibrary1")
                state.SendTab()
                Await state.WaitForAsynchronousOperationsAsync()
                state.AssertMatchesTextStartingAtLine(1, "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ClassLibrary1"")]")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionInsertsPublicKeyOnCommit() As Task
            Using strongKeyFileFixture = New StrongNameKeyFileFixture
                Using state = TestState.CreateTestStateFromWorkspace(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary1">
                            <CompilationOptions CryptoKeyFile=<%= strongKeyFileFixture.StrongKeyFile %> StrongNameProvider="Microsoft.CodeAnalysis.DesktopStrongNameProvider,Microsoft.CodeAnalysis"/>
                        </Project>
                        <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                            <Document>
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("$$")]
                        </Document>
                        </Project>
                    </Workspace>)
                    state.SendInvokeCompletionList()
                    Await state.AssertSelectedCompletionItem("ClassLibrary1")
                    state.SendTab()
                    Await state.WaitForAsynchronousOperationsAsync()
                    state.AssertMatchesTextStartingAtLine(1, "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ClassLibrary1, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c5c26d5ff8f59b47acc55d4feb5c19009317137e55a34ca2a107ac29b3badc6870d858cb2fbf0a71a04caf749f0517dc31a89b85b004f030ed3e785bb45090499682fb0b2a78e02a9b94de1e778e0611648d0925f3c1e6c76d099f668a79b1868940a20a48c7695ffcfb75fe0946692aa8dedafe727e3cbc07f64d646d2ef6f5"")]")
                End Using
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionContainsPublicKeyIfKeyIsSpecifiedByAttribute() As Task
            Using strongKeyFileFixture = New StrongNameKeyFileFixture
                Using state = TestState.CreateTestStateFromWorkspace(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary1">
                            <CompilationOptions StrongNameProvider="Microsoft.CodeAnalysis.DesktopStrongNameProvider,Microsoft.CodeAnalysis"/>
                            <Document>
                                [assembly: System.Reflection.AssemblyKeyFile("<%= strongKeyFileFixture.StrongKeyFile.Replace("\", "\\") %>")]
                            </Document>
                        </Project>
                        <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                            <Document>
    [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("$$")]
                            </Document>
                        </Project>
                    </Workspace>)
                    state.SendInvokeCompletionList()
                    Await state.AssertSelectedCompletionItem("ClassLibrary1")
                    state.SendTab()
                    Await state.WaitForAsynchronousOperationsAsync()
                    state.AssertMatchesTextStartingAtLine(1, "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ClassLibrary1, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c5c26d5ff8f59b47acc55d4feb5c19009317137e55a34ca2a107ac29b3badc6870d858cb2fbf0a71a04caf749f0517dc31a89b85b004f030ed3e785bb45090499682fb0b2a78e02a9b94de1e778e0611648d0925f3c1e6c76d099f668a79b1868940a20a48c7695ffcfb75fe0946692aa8dedafe727e3cbc07f64d646d2ef6f5"")]")
                End Using
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionContainsPublicKeyIfDelayedSigningIsEnabled() As Task
            Using strongKeyFileFixture = New StrongNameKeyFileFixture
                Using state = TestState.CreateTestStateFromWorkspace(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary1">
                            <CompilationOptions
                                CryptoKeyFile=<%= strongKeyFileFixture.StrongKeyFile %>
                                StrongNameProvider="Microsoft.CodeAnalysis.DesktopStrongNameProvider,Microsoft.CodeAnalysis"
                                DelaySign="True"
                            />
                        </Project>
                        <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                            <Document>
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("$$")]
                        </Document>
                        </Project>
                    </Workspace>)
                    state.SendInvokeCompletionList()
                    Await state.AssertSelectedCompletionItem("ClassLibrary1")
                    state.SendTab()
                    Await state.WaitForAsynchronousOperationsAsync()
                    state.AssertMatchesTextStartingAtLine(1, "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ClassLibrary1, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c5c26d5ff8f59b47acc55d4feb5c19009317137e55a34ca2a107ac29b3badc6870d858cb2fbf0a71a04caf749f0517dc31a89b85b004f030ed3e785bb45090499682fb0b2a78e02a9b94de1e778e0611648d0925f3c1e6c76d099f668a79b1868940a20a48c7695ffcfb75fe0946692aa8dedafe727e3cbc07f64d646d2ef6f5"")]")
                End Using
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionListIsEmptyIfAttributeIsNotTheBCLAttribute() As Task
            Using state = TestState.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary1"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="C.cs">
[assembly: Test.InternalsVisibleTo("$$")]
namespace Test
{
    [System.AttributeUsage(System.AttributeTargets.Assembly)]
    public sealed class InternalsVisibleToAttribute: System.Attribute
    {
        public InternalsVisibleToAttribute(string ignore)
        {

        }
    }
}
                        </Document>
                    </Project>
                </Workspace>)
                state.SendInvokeCompletionList()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

    End Class
End Namespace
