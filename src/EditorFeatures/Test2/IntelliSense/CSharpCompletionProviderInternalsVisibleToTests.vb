Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Public Class CSharpCompletionProviderInternalsVisibleToTests

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
            Dim tempFile = IO.Path.GetTempFileName()
            Dim keyFileContent = System.Convert.FromBase64String("
BwIAAAAkAABSU0EyAAQAAAEAAQDFwm1f+PWbR6zFXU/rXBkAkxcTflWjTKKhB6wps7rcaHDYWMsv
vwpxoEyvdJ8FF9wxqJuFsATwMO0+eFu0UJBJloL7Cyp44CqblN4ed44GEWSNCSXzwebHbQmfZop5
sYaJQKIKSMdpX/z7df4JRmkqqN7a/nJ+PLwH9k1kbS729Z+yZc8NsXysHla8QFo6nHeb8MPncf2y
qbkWOd/f7c25Oip36YQI8GfME+664yn+PLge2PvJioHQ4S6hKacGVf0blJDlYFDm21c7WxsB4Ln9
+nTboUFX+3jUxhBuZ+574hazkPzrdFleDvRqh78lwiTalLbIXHHthdYmd07pSI34I6DvEwwMEJHw
tn3rrWx4Rsd0gxtyGlNDgdyAs2sFpo7b2MDGRkgjxw5159e+wajWTAe7KBKaEiTCQq3HpngyKKVN
dC5Jfr7SCyz3M/do+rU9xLUxl1Bv12zuOVKNflIWgG/C+ofMK3/QfD0dPnrkjeiPohs0NsHtZjF3
oeCWysHnRXN3CV/udwOKjofYQc9fyPB2ilmxM2Jwsvd7hEHFpUBgMT6R28vosIxd5neNFAIan85i
Y5ghinxfxvK1u0wWSfyovuFabD4Ez1Ez6UqlgL1b9sPLoPqV8SYj1TASPOvdu5fqe8bzlgXILSZB
xiDimS2uguQQ5qX4kDphCt8judqpxTZKYcTKuKHYFrjzOwmkREl1ve4XfHCZIhhUMMDpkvQG351F
xHe4HK9zaRZZXWf2uqzcnuo5LTzBHtLLHhY=")
            Using stream = IO.File.OpenWrite(tempFile)
                stream.Write(keyFileContent, 0, keyFileContent.Length)
            End Using
            Using state = TestState.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" CommonReferences="true" AssemblyName="ClassLibrary1">
                        <CompilationOptions CryptoKeyFile=<%= tempFile %> StrongNameProvider="Microsoft.CodeAnalysis.DesktopStrongNameProvider,Microsoft.CodeAnalysis"/>
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
        End Function
    End Class
End Namespace
