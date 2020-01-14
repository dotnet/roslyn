' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense

    <[UseExportProvider]>
    Public Class CSharpCompletionCommandHandlerTests_InternalsVisibleTo

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionContainsOtherAssembliesOfSolution() As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
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
                Await state.AssertCompletionItemsContainAll("ClassLibrary1", "ClassLibrary2", "ClassLibrary3")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionContainsOtherAssemblyIfAttributeSuffixIsPresent() As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary1"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="C.cs">
[assembly: System.Runtime.CompilerServices.InternalsVisibleToAttribute("$$
                        </Document>
                    </Project>
                </Workspace>)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsContainAll("ClassLibrary1")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionIsTriggeredWhenDoubleQuoteIsEntered() As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
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
                Await state.AssertCompletionItemsContainAll("ClassLibrary1")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionIsEmptyUntilDoubleQuotesAreEntered() As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
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
                Await state.AssertCompletionItemsDoNotContainAny("ClassLibrary1")
                state.SendTypeChars("("c)
                Await state.AssertNoCompletionSession()
                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsDoNotContainAny("ClassLibrary1")
                state.SendTypeChars(""""c)
                Await state.AssertCompletionItemsContainAll("ClassLibrary1")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionIsTriggeredWhenCharacterIsEnteredAfterOpeningDoubleQuote() As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary1"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="C.cs">
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("$$")]
                        </Document>
                    </Project>
                </Workspace>)
                Await state.AssertNoCompletionSession()
                state.SendTypeChars("a"c)
                Await state.AssertCompletionItemsContainAll("ClassLibrary1")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionIsNotTriggeredWhenCharacterIsEnteredThatIsNotRightBesideTheOpeniningDoubleQuote() As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary1"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="C.cs">
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("a$$")]
                        </Document>
                    </Project>
                </Workspace>)
                Await state.AssertNoCompletionSession()
                state.SendTypeChars("b"c)
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionIsNotTriggeredWhenDoubleQuoteIsEnteredAtStartOfFile() As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary1"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="C.cs">$$
                        </Document>
                    </Project>
                </Workspace>)
                Await state.AssertNoCompletionSession()
                state.SendTypeChars("a"c)
                Await state.AssertCompletionItemsDoNotContainAny("ClassLibrary1")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionIsNotTriggeredByArrayElementAccess() As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary1"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="C.cs"><![CDATA[
namespace A
{
    public class C
    {
        public void M()
        {
            var d = new System.Collections.Generic.Dictionary<string, string>();
            var v = d$$;
        }
    }
}
]]>
                        </Document>
                    </Project>
                </Workspace>)
                Dim AssertNoCompletionAndCompletionDoesNotContainClassLibrary1 As Func(Of Task) =
                    Async Function()
                        Await state.AssertNoCompletionSession()
                        state.SendInvokeCompletionList()
                        Await state.AssertSessionIsNothingOrNoCompletionItemLike("ClassLibrary1")
                    End Function
                Await AssertNoCompletionAndCompletionDoesNotContainClassLibrary1()
                state.SendTypeChars("["c)
                Await AssertNoCompletionAndCompletionDoesNotContainClassLibrary1()
                state.SendTypeChars(""""c)
                Await AssertNoCompletionAndCompletionDoesNotContainClassLibrary1()
            End Using
        End Function

        Private Async Function AssertCompletionListHasItems(code As String, hasItems As Boolean) As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
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
                If hasItems Then
                    Await state.AssertCompletionItemsContainAll("ClassLibrary1")
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
        Public Async Function AssertCompletionListHasNoItems_IfNumberIsEntered() As Task
            Await AssertCompletionListHasItems("[assembly: InternalsVisibleTo(1$$2)]", False)
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
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
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
                Await state.AssertCompletionItemsContainAll("ClassLibrary1")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionDoesNotContainCurrentAssembly() As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary1"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="C.cs">
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("$$")]
                        </Document>
                    </Project>
                </Workspace>)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsDoNotContainAny("TestAssembly")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionInsertsAssemblyNameOnCommit() As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
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
                state.AssertMatchesTextStartingAtLine(1, "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ClassLibrary1"")]")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionInsertsPublicKeyOnCommit() As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary1">
                        <CompilationOptions
                            CryptoKeyFile=<%= SigningTestHelpers.PublicKeyFile %>
                            StrongNameProvider=<%= SigningTestHelpers.DefaultDesktopStrongNameProvider.GetType().AssemblyQualifiedName %>/>
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
                state.AssertMatchesTextStartingAtLine(1, "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ClassLibrary1, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb"")]")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionContainsPublicKeyIfKeyIsSpecifiedByAttribute() As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary1">
                        <CompilationOptions
                            StrongNameProvider=<%= SigningTestHelpers.DefaultDesktopStrongNameProvider.GetType().AssemblyQualifiedName %>/>
                        <Document>
                            [assembly: System.Reflection.AssemblyKeyFile("<%= SigningTestHelpers.PublicKeyFile.Replace("\", "\\") %>")]
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
                state.AssertMatchesTextStartingAtLine(1, "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ClassLibrary1, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb"")]")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionContainsPublicKeyIfDelayedSigningIsEnabled() As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary1">
                        <CompilationOptions
                            CryptoKeyFile=<%= SigningTestHelpers.PublicKeyFile %>
                            StrongNameProvider=<%= SigningTestHelpers.DefaultDesktopStrongNameProvider.GetType().AssemblyQualifiedName %>
                            DelaySign="True"/>
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
                state.AssertMatchesTextStartingAtLine(1, "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ClassLibrary1, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb"")]")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionListIsEmptyIfAttributeIsNotTheBCLAttribute() As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionContainsOnlyAssembliesThatAreNotAlreadyIVT() As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary1"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary2"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary3"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="C.cs">
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ClassLibrary1")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ClassLibrary2")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("$$
                        </Document>
                    </Project>
                </Workspace>)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsDoNotContainAny("ClassLibrary1", "ClassLibrary2")
                Await state.AssertCompletionItemsContainAll("ClassLibrary3")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionContainsOnlyAssembliesThatAreNotAlreadyIVTIfAssemblyNameIsAConstant() As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary1"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary2"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary3"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                        <MetadataReferenceFromSource Language="C#" CommonReferences="true">
                            <Document FilePath="ReferencedDocument.cs">
namespace A {
    public static class Constants
    {
        public const string AssemblyName1 = "ClassLibrary1";
    }
}
                            </Document>
                        </MetadataReferenceFromSource>
                        <Document FilePath="C.cs">
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(A.Constants.AssemblyName1)]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ClassLibrary2")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("$$
                        </Document>
                    </Project>
                </Workspace>)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsDoNotContainAny("ClassLibrary1", "ClassLibrary2")
                Await state.AssertCompletionItemsContainAll("ClassLibrary3")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionContainsOnlyAssembliesThatAreNotAlreadyIVTForDifferentSyntax() As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary1"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary2"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary3"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary4"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary5"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary6"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary7"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary8"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary9"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="C.cs">
// Code comment
using System.Runtime.CompilerServices;
using System.Reflection;
using IVT = System.Runtime.CompilerServices.InternalsVisibleToAttribute;
// Code comment
[assembly: InternalsVisibleTo("ClassLibrary1", AllInternalsVisible = true)]
[assembly: InternalsVisibleTo(assemblyName: "ClassLibrary2", AllInternalsVisible = true)]
[assembly: AssemblyVersion("1.0.0.0"), InternalsVisibleTo("ClassLibrary3")]
[assembly: InternalsVisibleTo("ClassLibrary4"), AssemblyCopyright("Copyright")]
[assembly: AssemblyDescription("Description")]
[assembly: InternalsVisibleTo("ClassLibrary5")]
[assembly: InternalsVisibleTo("ClassLibrary6, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb")]
[assembly: InternalsVisibleTo("ClassLibrary" + "7")]
[assembly: IVT("ClassLibrary8")]
[assembly: InternalsVisibleTo("$$
namespace A {
    public class A { }
}
                        </Document>
                    </Project>
                </Workspace>)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsDoNotContainAny("ClassLibrary1", "ClassLibrary2", "ClassLibrary3", "ClassLibrary4", "ClassLibrary5", "ClassLibrary6", "ClassLibrary7", "ClassLibrary8")
                Await state.AssertCompletionItemsContainAll("ClassLibrary9")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionContainsOnlyAssembliesThatAreNotAlreadyIVTWithSyntaxError() As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary1"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="C.cs">
using System.Runtime.CompilerServices;
using System.Reflection;

[assembly: InternalsVisibleTo("ClassLibrary" + 1)] // Not a constant
[assembly: InternalsVisibleTo("$$
                        </Document>
                    </Project>
                </Workspace>)
                state.SendInvokeCompletionList()
                ' ClassLibrary1 must be listed because the existing attribute argument can't be resolved to a constant.
                Await state.AssertCompletionItemsContainAll("ClassLibrary1")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionContainsOnlyAssembliesThatAreNotAlreadyIVTWithMoreThanOneDocument() As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary1"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary2"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="OtherDocument.cs">
using System.Runtime.CompilerServices;
using System.Reflection;
[assembly: InternalsVisibleTo("ClassLibrary1")]
[assembly: AssemblyDescription("Description")]
                        </Document>
                        <Document FilePath="C.cs">
using System.Runtime.CompilerServices;
using System.Reflection;
[assembly: InternalsVisibleTo("$$
                        </Document>
                    </Project>
                </Workspace>)
                state.SendInvokeCompletionList()
                Await state.AssertCompletionItemsDoNotContainAny("ClassLibrary1")
                Await state.AssertCompletionItemsContainAll("ClassLibrary2")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionIgnoresUnsupportedProjectTypes() As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="NoCompilation" AssemblyName="ClassLibrary1"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="C.cs">
using System.Runtime.CompilerServices;
using System.Reflection;
[assembly: InternalsVisibleTo("$$
                        </Document>
                    </Project>
                </Workspace>)
                state.SendInvokeCompletionList()
                Await state.AssertNoCompletionSession()
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(29447, "https://github.com/dotnet/roslyn/pull/29447")>
        Public Async Function CodeCompletionReplacesExisitingAssemblyNameWithDots_1() As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" AssemblyName="Dotted.Assembly.Name"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="C.cs">
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Dotted.Assem$$bly")]
                        </Document>
                    </Project>
                </Workspace>)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("Dotted.Assembly.Name")
                state.SendTab()
                state.AssertMatchesTextStartingAtLine(1, "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""Dotted.Assembly.Name"")]")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(29447, "https://github.com/dotnet/roslyn/pull/29447")>
        Public Async Function CodeCompletionReplacesExisitingAssemblyNameWithDots_2() As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" AssemblyName="Dotted.Assembly.Name"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="C.cs">
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Dotted.Assem$$bly
                        </Document>
                    </Project>
                </Workspace>)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("Dotted.Assembly.Name")
                state.SendTab()
                state.AssertMatchesTextStartingAtLine(1, "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""Dotted.Assembly.Name")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(29447, "https://github.com/dotnet/roslyn/pull/29447")>
        Public Async Function CodeCompletionReplacesExisitingAssemblyNameWithDots_3() As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" AssemblyName="Dotted.Assembly.Name"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="C.cs">
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("  Dotted.Assem$$bly  ")]
                        </Document>
                    </Project>
                </Workspace>)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("Dotted.Assembly.Name")
                state.SendTab()
                state.AssertMatchesTextStartingAtLine(1, "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""Dotted.Assembly.Name"")]")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(29447, "https://github.com/dotnet/roslyn/pull/29447")>
        Public Async Function CodeCompletionReplacesExisitingAssemblyNameWithDots_4() As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" AssemblyName="Dotted1.Dotted2.Assembly.Dotted3"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="C.cs">
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Dotted1.Dotted2.Assem$$bly.Dotted")]
                        </Document>
                    </Project>
                </Workspace>)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("Dotted1.Dotted2.Assembly.Dotted3")
                state.SendTab()
                state.AssertMatchesTextStartingAtLine(1, "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""Dotted1.Dotted2.Assembly.Dotted3"")]")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(29447, "https://github.com/dotnet/roslyn/pull/29447")>
        Public Async Function CodeCompletionReplacesExisitingAssemblyNameWithDots_Verbatim_1() As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" AssemblyName="Dotted1.Dotted2.Assembly.Dotted3"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="C.cs">
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(@"Dotted1.Dotted2.Assem$$bly.Dotted")]
                        </Document>
                    </Project>
                </Workspace>)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("Dotted1.Dotted2.Assembly.Dotted3")
                state.SendTab()
                state.AssertMatchesTextStartingAtLine(1, "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(@""Dotted1.Dotted2.Assembly.Dotted3"")]")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(29447, "https://github.com/dotnet/roslyn/pull/29447")>
        Public Async Function CodeCompletionReplacesExisitingAssemblyNameWithDots_Verbatim_2() As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" AssemblyName="Dotted1.Dotted2.Assembly.Dotted3"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="C.cs">
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(@"Dotted1.Dotted2.Assem$$bly
                        </Document>
                    </Project>
                </Workspace>)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("Dotted1.Dotted2.Assembly.Dotted3")
                state.SendTab()
                state.AssertMatchesTextStartingAtLine(1, "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(@""Dotted1.Dotted2.Assembly.Dotted3")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(29447, "https://github.com/dotnet/roslyn/pull/29447")>
        Public Async Function CodeCompletionReplacesExisitingAssemblyNameWithDots_EscapeSequence_1() As Task
            ' Escaped double quotes are not handled properly: The selection is expanded from the cursor position until
            ' a double quote or new line is reached. But because double quotes are not allowed in this context this 
            ' case is rare enough to ignore. Supporting it would require more complicated code that was reverted in
            ' https://github.com/dotnet/roslyn/pull/29447/commits/e7a852a7e83fffe1f25a8dee0aaec68f67fcc1d8
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" AssemblyName="Dotted1.Dotted2.Assembly.Dotted3"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="C.cs">
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("\"Dotted1.Dotted2.Assem$$bly\"")]
                        </Document>
                    </Project>
                </Workspace>)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("Dotted1.Dotted2.Assembly.Dotted3")
                state.SendTab()
                state.AssertMatchesTextStartingAtLine(1, "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""\""Dotted1.Dotted2.Assembly.Dotted3"""")]")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(29447, "https://github.com/dotnet/roslyn/pull/29447")>
        Public Async Function CodeCompletionReplacesExisitingAssemblyNameWithDots_OpenEnded() As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" AssemblyName="Dotted1.Dotted2.Assembly.Dotted3"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="C.cs">
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Dotted1.Dotted2.Assem$$bly.Dotted4
                        </Document>
                    </Project>
                </Workspace>)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("Dotted1.Dotted2.Assembly.Dotted3")
                state.SendTab()
                state.AssertMatchesTextStartingAtLine(1, "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""Dotted1.Dotted2.Assembly.Dotted3")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(29447, "https://github.com/dotnet/roslyn/pull/29447")>
        Public Async Function CodeCompletionReplacesExisitingAssemblyNameWithDots_AndPublicKey_OpenEnded() As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" AssemblyName="Dotted1.Dotted2.Assembly.Dotted3"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="C.cs">
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Company.Asse$$mbly, PublicKey=123
                        </Document>
                    </Project>
                </Workspace>)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("Dotted1.Dotted2.Assembly.Dotted3")
                state.SendTab()
                state.AssertMatchesTextStartingAtLine(1, "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""Dotted1.Dotted2.Assembly.Dotted3")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(29447, "https://github.com/dotnet/roslyn/pull/29447")>
        Public Async Function CodeCompletionReplacesExisitingAssemblyNameWithDots_AndPublicKey_LineBreakExampleFromMSDN() As Task
            ' Source https://msdn.microsoft.com/de-de/library/system.runtime.compilerservices.internalsvisibletoattribute(v=vs.110).aspx
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" AssemblyName="Dotted1.Dotted2.Assembly.Dotted3"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="C.cs">
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("$$Friend1, PublicKey=002400000480000094" +
                                                              "0000000602000000240000525341310004000" +
                                                              "001000100bf8c25fcd44838d87e245ab35bf7" +
                                                              "3ba2615707feea295709559b3de903fb95a93" +
                                                              "3d2729967c3184a97d7b84c7547cd87e435b5" +
                                                              "6bdf8621bcb62b59c00c88bd83aa62c4fcdd4" +
                                                              "712da72eec2533dc00f8529c3a0bbb4103282" +
                                                              "f0d894d5f34e9f0103c473dce9f4b457a5dee" +
                                                              "fd8f920d8681ed6dfcb0a81e96bd9b176525a" +
                                                              "26e0b3")]
                        </Document>
                    </Project>
                </Workspace>)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("Dotted1.Dotted2.Assembly.Dotted3")
                state.SendTab()
                state.AssertMatchesTextStartingAtLine(1, "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""Dotted1.Dotted2.Assembly.Dotted3"" +")
                state.AssertMatchesTextStartingAtLine(2, "                                                              ""0000000602000000240000525341310004000"" +")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(29447, "https://github.com/dotnet/roslyn/pull/29447")>
        Public Async Function CodeCompletionReplacesExisitingAssemblyNameWithDots_OpenEndedStringFollowedByEOF() As Task
            ' Source https://msdn.microsoft.com/de-de/library/system.runtime.compilerservices.internalsvisibletoattribute(v=vs.110).aspx
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" AssemblyName="Dotted1.Dotted2.Assembly.Dotted3"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="C.cs">
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Friend1$$</Document>
                    </Project>
                </Workspace>)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("Dotted1.Dotted2.Assembly.Dotted3")
                state.SendTab()
                state.AssertMatchesTextStartingAtLine(1, "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""Dotted1.Dotted2.Assembly.Dotted3")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(29447, "https://github.com/dotnet/roslyn/pull/29447")>
        Public Async Function CodeCompletionReplacesExisitingAssemblyNameWithDots_OpenEndedStringFollowedByNewLines() As Task
            ' Source https://msdn.microsoft.com/de-de/library/system.runtime.compilerservices.internalsvisibletoattribute(v=vs.110).aspx
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" AssemblyName="Dotted1.Dotted2.Assembly.Dotted3"/>
                    <Project Language="C#" CommonReferences="true" AssemblyName="TestAssembly">
                        <Document FilePath="C.cs"><![CDATA[
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Friend1$$

]]>
                        </Document>
                    </Project>
                </Workspace>)
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem("Dotted1.Dotted2.Assembly.Dotted3")
                state.SendTab()
                state.AssertMatchesTextStartingAtLine(1, "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""Dotted1.Dotted2.Assembly.Dotted3")
                state.AssertMatchesTextStartingAtLine(2, "")
            End Using
        End Function
    End Class
End Namespace
