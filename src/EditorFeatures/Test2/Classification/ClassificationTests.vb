Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Editor.Implementation.Classification
Imports Microsoft.CodeAnalysis.Editor.Shared.Extensions
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Tagging

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Classification
    Public Class ClassificationTests
        Inherits AbstractClassifierTests

        <WpfFact, Trait(Traits.Feature, Traits.Features.Classification)>
        Public Async Function TestClassificationInLinkedFiles() As Task
            Dim workspaceElement =
                <Workspace>
                    <Project Language="C#" CommonReferences="true" AssemblyName="Shared">
                        <Document FilePath="C.cs"></Document>
                    </Project>
                    <Project Language="C#" CommonReferences="true" AssemblyName="Project1" PreprocessorSymbols="Project1">
                        <Document IsLinkFile="true" LinkAssemblyName="Shared" LinkFilePath="C.cs"/>
                    </Project>
                    <Project Language="C#" CommonReferences="true" AssemblyName="Project2" PreprocessorSymbols="Project2">
                        <Document IsLinkFile="true" LinkAssemblyName="Shared" LinkFilePath="C.cs"/>
                    </Project>
                </Workspace>

            Using workspace = TestWorkspaceFactory.CreateWorkspace(workspaceElement)
                Dim project1 = workspace.Projects.Single(Function(p) p.AssemblyName = "Project1")
                Dim project2 = workspace.Projects.Single(Function(p) p.AssemblyName = "Project2")
                Dim linkDocument1 = project1.Documents.Single()
                Dim linkDocument2 = project2.Documents.Single()

                Assert.True(linkDocument1.IsLinkFile)
                Assert.True(linkDocument2.IsLinkFile)

                workspace.SetDocumentContext(linkDocument1.Id)

                Dim subjectBuffer = linkDocument1.GetTextBuffer()
                Assert.Equal(subjectBuffer, linkDocument2.GetTextBuffer())

                Dim notificationService = workspace.GetService(Of IForegroundNotificationService)()
                Dim typeMap = workspace.ExportProvider.GetExport(Of ClassificationTypeMap)().Value
                Dim tagComputer = New SyntacticClassificationTaggerProvider.TagComputer(
                    subjectBuffer,
                    notificationService,
                    AggregateAsynchronousOperationListener.CreateEmptyListener(),
                    typeMap,
                    New SyntacticClassificationTaggerProvider(notificationService, typeMap, Nothing))

                Dim span As SnapshotSpan = Nothing
                Dim checkpoint = New Checkpoint()
                AddHandler tagComputer.TagsChanged, Sub(s, e)
                                                        span = e.Span
                                                        checkpoint.Release()
                                                    End Sub

                subjectBuffer.Insert(0, "class C
{
#if Project1
    int M() { }
#elif Project2
    string M() { }
#endif
}")

                Await checkpoint.Task.ConfigureAwait(True)
                Dim tags = tagComputer.GetTags(New NormalizedSnapshotSpanCollection(span))

                Validate(subjectBuffer.CurrentSnapshot.GetText(),
                         tags,
                         Keyword("class"),
                         [Class]("C"),
                         Punctuation.OpenCurly,
                         PPKeyword("#"),
                         PPKeyword("if"),
                         Identifier("Project1"),
                         Keyword("int"),
                         Identifier("M"),
                         Punctuation.OpenParen,
                         Punctuation.CloseParen,
                         Punctuation.OpenCurly,
                         Punctuation.CloseCurly,
                         PPKeyword("#"),
                         PPKeyword("elif"),
                         Identifier("Project2"),
                         ExcludedCode("    string M() { }" + vbCrLf),
                         PPKeyword("#"),
                         PPKeyword("endif"),
                         Punctuation.CloseCurly)

                checkpoint = New Checkpoint()
                workspace.SetDocumentContext(linkDocument2.Id)

                Await checkpoint.Task.ConfigureAwait(True)
                tags = tagComputer.GetTags(New NormalizedSnapshotSpanCollection(span))

                Validate(subjectBuffer.CurrentSnapshot.GetText(),
                         tags,
                         Keyword("class"),
                         [Class]("C"),
                         Punctuation.OpenCurly,
                         PPKeyword("#"),
                         PPKeyword("if"),
                         Identifier("Project1"),
                         ExcludedCode("    int M() { }" + vbCrLf),
                         PPKeyword("#"),
                         PPKeyword("elif"),
                         Identifier("Project2"),
                         Keyword("string"),
                         Identifier("M"),
                         Punctuation.OpenParen,
                         Punctuation.CloseParen,
                         Punctuation.OpenCurly,
                         Punctuation.CloseCurly,
                         PPKeyword("#"),
                         PPKeyword("endif"),
                         Punctuation.CloseCurly)
            End Using
        End Function

        Private Overloads Sub Validate(text As String, tags As IEnumerable(Of ITagSpan(Of IClassificationTag)), ParamArray expected As Tuple(Of String, String)())
            Validate(text, expected, ConvertTags(tags))
        End Sub

        Private Function ConvertTags(tags As IEnumerable(Of ITagSpan(Of IClassificationTag))) As List(Of ClassifiedSpan)
            Return tags.Select(Function(t) New ClassifiedSpan(t.Tag.ClassificationType.Classification, t.Span.Span.ToTextSpan())).ToList()
        End Function
    End Class
End Namespace