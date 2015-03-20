' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CSharp
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Shared.Options
Imports Microsoft.CodeAnalysis.SolutionCrawler
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics.UnitTests

    ''' <summary>
    ''' Tests for Error List. Since it is language agnostic there are no C# or VB Specific tests
    ''' </summary>
    Public Class DiagnosticProviderTests
        Private Const s_errorElementName As String = "Error"
        Private Const s_projectAttributeName As String = "Project"
        Private Const s_codeAttributeName As String = "Code"
        Private Const s_mappedLineAttributeName As String = "MappedLine"
        Private Const s_mappedColumnAttributeName As String = "MappedColumn"
        Private Const s_originalLineAttributeName As String = "OriginalLine"
        Private Const s_originalColumnAttributeName As String = "OriginalColumn"
        Private Const s_idAttributeName As String = "Id"
        Private Const s_messageAttributeName As String = "Message"
        Private Const s_originalFileAttributeName As String = "OriginalFile"
        Private Const s_mappedFileAttributeName As String = "MappedFile"

        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestNoErrors()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                        class Foo { }
                                    </Document>
                           </Project>
                       </Workspace>

            VerifyAllAvailableDiagnostics(test, Nothing)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestSingleDeclarationError()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                        class Foo { dontcompile }
                                    </Document>
                           </Project>
                       </Workspace>
            Dim diagnostics = <Diagnostics>
                                  <Error Code="1519" Id="CS1519" MappedFile="Test.cs" MappedLine="1" MappedColumn="64" OriginalFile="Test.cs" OriginalLine="1" OriginalColumn="64"
                                      Message=<%= String.Format(CSharpResources.ERR_InvalidMemberDecl, "}") %>/>
                              </Diagnostics>

            VerifyAllAvailableDiagnostics(test, diagnostics)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestLineDirective()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                        class Foo { dontcompile }
                                        #line 1000
                                        class Foo2 { dontcompile }
                                        #line default
                                        class Foo4 { dontcompile }
                                    </Document>
                           </Project>
                       </Workspace>
            Dim diagnostics = <Diagnostics>
                                  <Error Code="1519" Id="CS1519" MappedFile="Test.cs" MappedLine="1" MappedColumn="64" OriginalFile="Test.cs" OriginalLine="1" OriginalColumn="64"
                                      Message=<%= String.Format(CSharpResources.ERR_InvalidMemberDecl, "}") %>/>
                                  <Error Code="1519" Id="CS1519" MappedFile="Test.cs" MappedLine="999" MappedColumn="65" OriginalFile="Test.cs" OriginalLine="3" OriginalColumn="65"
                                      Message=<%= String.Format(CSharpResources.ERR_InvalidMemberDecl, "}") %>/>
                                  <Error Code="1519" Id="CS1519" MappedFile="Test.cs" MappedLine="5" MappedColumn="65" OriginalFile="Test.cs" OriginalLine="5" OriginalColumn="65"
                                      Message=<%= String.Format(CSharpResources.ERR_InvalidMemberDecl, "}") %>/>
                              </Diagnostics>

            VerifyAllAvailableDiagnostics(test, diagnostics)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestSingleBindingError()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                        class Foo { int a = "test"; }
                                    </Document>
                           </Project>
                       </Workspace>

            Dim diagnostics = <Diagnostics>
                                  <Error Code="29" Id="CS0029" MappedFile="Test.cs" MappedLine="1" MappedColumn="60" OriginalFile="Test.cs" OriginalLine="1" OriginalColumn="60"
                                      Message=<%= String.Format(CSharpResources.ERR_NoImplicitConv, "string", "int") %>/>
                              </Diagnostics>

            VerifyAllAvailableDiagnostics(test, diagnostics)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestMultipleErrorsAndWarnings()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                        class Foo { gibberish }
                                        class Foo2 { as; }
                                        class Foo3 { long q = 1l; }
                                        #pragma disable 9999999"
                                    </Document>
                           </Project>
                       </Workspace>

            Dim diagnostics = <Diagnostics>
                                  <Error Code="1519" Id="CS1519" MappedFile="Test.cs" MappedLine="1" MappedColumn="62" OriginalFile="Test.cs" OriginalLine="1" OriginalColumn="62"
                                      Message=<%= String.Format(CSharpResources.ERR_InvalidMemberDecl, "}") %>/>
                                  <Error Code="1519" Id="CS1519" MappedFile="Test.cs" MappedLine="2" MappedColumn="53" OriginalFile="Test.cs" OriginalLine="2" OriginalColumn="53"
                                      Message=<%= String.Format(CSharpResources.ERR_InvalidMemberDecl, "as") %>/>
                                  <Warning Code="78" Id="CS0078" MappedFile="Test.cs" MappedLine="3" MappedColumn="63" OriginalFile="Test.cs" OriginalLine="3" OriginalColumn="63"
                                      Message=<%= CSharpResources.WRN_LowercaseEllSuffix %>/>
                                  <Warning Code="1633" Id="CS1633" MappedFile="Test.cs" MappedLine="4" MappedColumn="48" OriginalFile="Test.cs" OriginalLine="4" OriginalColumn="48"
                                      Message=<%= CSharpResources.WRN_IllegalPragma %>/>
                              </Diagnostics>

            ' Note: The below is removed because of bug # 550593.
            '<Warning Code = "414" Id="CS0414" MappedFile="Test.cs" MappedLine="3" MappedColumn="58" OriginalFile="Test.cs" OriginalLine="3" OriginalColumn="58"
            '    Message = "The field 'Foo3.q' is assigned but its value is never used" />

            VerifyAllAvailableDiagnostics(test, diagnostics)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestBindingAndDeclarationErrors()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                        class Program { void Main() { - } }
                                    </Document>
                           </Project>
                       </Workspace>

            Dim diagnostics = <Diagnostics>
                                  <Error Code="1525" Id="CS1525" MappedFile="Test.cs" MappedLine="1" MappedColumn="72" OriginalFile="Test.cs" OriginalLine="1" OriginalColumn="72"
                                      Message=<%= String.Format(CSharpResources.ERR_InvalidExprTerm, "}") %>/>
                                  <Error Code="1002" Id="CS1002" MappedFile="Test.cs" MappedLine="1" MappedColumn="72" OriginalFile="Test.cs" OriginalLine="1" OriginalColumn="72"
                                      Message=<%= CSharpResources.ERR_SemicolonExpected %>/>
                              </Diagnostics>

            VerifyAllAvailableDiagnostics(test, diagnostics)
        End Sub

        ' Diagnostics are ordered by project-id
        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestDiagnosticsFromMultipleProjects()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                        class Program
                                        {
                                            -
                                            void Test()
                                            {
                                                int a = 5 - "2";
                                            }
                                        }
                                    </Document>
                           </Project>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document FilePath="Test.vb">
                                        Class FooClass
                                            Sub Blah() End Sub
                                        End Class
                                   </Document>
                           </Project>
                       </Workspace>

            Dim diagnostics = <Diagnostics>
                                  <Error Code="1519" Id="CS1519" MappedFile="Test.cs" MappedLine="3" MappedColumn="44" OriginalFile="Test.cs" OriginalLine="3" OriginalColumn="44"
                                      Message=<%= String.Format(CSharpResources.ERR_InvalidMemberDecl, "-") %>/>
                                  <Error Code="19" Id="CS0019" MappedFile="Test.cs" MappedLine="6" MappedColumn="56" OriginalFile="Test.cs" OriginalLine="6" OriginalColumn="56"
                                      Message=<%= String.Format(CSharpResources.ERR_BadBinaryOps, "-", "int", "string") %>/>
                                  <Error Code="30026" Id="BC30026" MappedFile="Test.vb" MappedLine="2" MappedColumn="44" OriginalFile="Test.vb" OriginalLine="2" OriginalColumn="44"
                                      Message=<%= ERR_EndSubExpected %>/>
                                  <Error Code="30205" Id="BC30205" MappedFile="Test.vb" MappedLine="2" MappedColumn="55" OriginalFile="Test.vb" OriginalLine="2" OriginalColumn="55"
                                      Message=<%= ERR_ExpectedEOS %>/>
                              </Diagnostics>

            VerifyAllAvailableDiagnostics(test, diagnostics, ordered:=False)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub TestDiagnosticsFromTurnedOff()
            Dim test = <Workspace>
                           <Project Language="C#" CommonReferences="true">
                               <Document FilePath="Test.cs">
                                        class Program
                                        {
                                            -
                                            void Test()
                                            {
                                                int a = 5 - "2";
                                            }
                                        }
                                    </Document>
                           </Project>
                           <Project Language="Visual Basic" CommonReferences="true">
                               <Document FilePath="Test.vb">
                                        Class FooClass
                                            Sub Blah() End Sub
                                        End Class
                                   </Document>
                           </Project>
                       </Workspace>

            Dim diagnostics = <Diagnostics></Diagnostics>

            VerifyAllAvailableDiagnostics(test, diagnostics, ordered:=False, enabled:=False)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub WarningsAsErrors()
            Dim test =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <CompilationOptions ReportDiagnostic="Error"/>
                        <Document FilePath="Test.cs">
                            class Program
                            {
                                void Test()
                                {
                                    int a = 5;
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>

            Dim diagnostics =
                <Diagnostics>
                    <Error Code="219" Id="CS0219"
                        MappedFile="Test.cs" MappedLine="5" MappedColumn="40"
                        OriginalFile="Test.cs" OriginalLine="5" OriginalColumn="40"
                        Message=<%= String.Format(CSharpResources.WRN_UnreferencedVarAssg, "a") %>/>
                </Diagnostics>

            VerifyAllAvailableDiagnostics(test, diagnostics)
        End Sub

        Private Sub VerifyAllAvailableDiagnostics(test As XElement, diagnostics As XElement, Optional ordered As Boolean = True, Optional enabled As Boolean = True)
            Using workspace = TestWorkspaceFactory.CreateWorkspace(test)

                ' turn off diagnostic
                If Not enabled Then
                    Dim optionService = workspace.Services.GetService(Of IOptionService)()
                    optionService.SetOptions(
                        optionService.GetOptions().WithChangedOption(ServiceComponentOnOffOptions.DiagnosticProvider, False) _
                                                  .WithChangedOption(ServiceFeatureOnOffOptions.ClosedFileDiagnostic, LanguageNames.CSharp, False) _
                                                  .WithChangedOption(ServiceFeatureOnOffOptions.ClosedFileDiagnostic, LanguageNames.VisualBasic, False))
                End If

                Dim registrationService = workspace.Services.GetService(Of ISolutionCrawlerRegistrationService)()
                registrationService.Register(workspace)

                Dim diagnosticProvider = GetDiagnosticProvider(workspace)
                Dim actualDiagnostics = diagnosticProvider.GetCachedDiagnosticsAsync(workspace).Result _
                                                          .Select(Function(d) New [Shared].Diagnostics.DiagnosticTaskItem(d))

                registrationService.Unregister(workspace)

                If diagnostics Is Nothing Then
                    Assert.Equal(0, actualDiagnostics.Count)
                Else
                    Dim expectedDiagnostics = GetExpectedDiagnostics(workspace, diagnostics)

                    If ordered Then
                        AssertEx.Equal(expectedDiagnostics, actualDiagnostics, EqualityComparer(Of IErrorTaskItem).Default)
                    Else
                        AssertEx.SetEqual(expectedDiagnostics, actualDiagnostics, EqualityComparer(Of IErrorTaskItem).Default)
                    End If
                End If
            End Using
        End Sub

        Private Function GetDiagnosticProvider(workspace As TestWorkspace) As DiagnosticAnalyzerService
            Dim snapshot = workspace.CurrentSolution

            Dim notificationServie = New TestForegroundNotificationService()

            Dim compilerAnalyzersMap = DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap()
            Dim analyzerService = New TestDiagnosticAnalyzerService(compilerAnalyzersMap)

            ' CollectErrors generates interleaved background and foreground tasks.
            Dim service = DirectCast(workspace.Services.GetService(Of ISolutionCrawlerRegistrationService)(), SolutionCrawlerRegistrationService)
            service.WaitUntilCompletion_ForTestingPurposesOnly(workspace, SpecializedCollections.SingletonEnumerable(analyzerService.CreateIncrementalAnalyzer(workspace)).WhereNotNull().ToImmutableArray())

            Return analyzerService
        End Function

        Private Function GetExpectedDiagnostics(workspace As TestWorkspace, diagnostics As XElement) As List(Of IErrorTaskItem)
            Dim result As New List(Of IErrorTaskItem)
            Dim code As Integer, mappedLine As Integer, mappedColumn As Integer, originalLine As Integer, originalColumn As Integer
            Dim Id As String, message As String, originalFile As String, mappedFile As String
            Dim documentId As DocumentId

            For Each diagnostic As XElement In diagnostics.Elements()

                code = Integer.Parse(diagnostic.Attribute(s_codeAttributeName).Value)
                mappedLine = Integer.Parse(diagnostic.Attribute(s_mappedLineAttributeName).Value)
                mappedColumn = Integer.Parse(diagnostic.Attribute(s_mappedColumnAttributeName).Value)
                originalLine = Integer.Parse(diagnostic.Attribute(s_originalLineAttributeName).Value)
                originalColumn = Integer.Parse(diagnostic.Attribute(s_originalColumnAttributeName).Value)

                Id = diagnostic.Attribute(s_idAttributeName).Value
                message = diagnostic.Attribute(s_messageAttributeName).Value
                originalFile = diagnostic.Attribute(s_originalFileAttributeName).Value
                mappedFile = diagnostic.Attribute(s_mappedFileAttributeName).Value
                documentId = GetDocumentId(workspace, originalFile)

                If diagnostic.Name.LocalName.Equals(s_errorElementName) Then
                    result.Add(SourceError(Id, message, workspace, documentId, documentId.ProjectId, mappedLine, originalLine, mappedColumn, originalColumn, mappedFile, originalFile))
                Else
                    result.Add(SourceWarning(Id, message, workspace, documentId, documentId.ProjectId, mappedLine, originalLine, mappedColumn, originalColumn, mappedFile, originalFile))
                End If
            Next
            Return result
        End Function

        Private Function GetProjectId(workspace As TestWorkspace, projectName As String) As ProjectId
            Return (From doc In workspace.Documents
                    Where doc.Project.AssemblyName.Equals(projectName)
                    Select doc.Project.Id).Single()
        End Function

        Private Function GetDocumentId(workspace As TestWorkspace, document As String) As DocumentId
            Return (From doc In workspace.Documents
                    Where doc.FilePath.Equals(document)
                    Select doc.Id).Single()
        End Function

        Private Function SourceError(id As String, message As String, workspace As Workspace, docId As DocumentId, projId As ProjectId, mappedLine As Integer, originalLine As Integer,
                                       mappedColumn As Integer, originalColumn As Integer, mappedFile As String, originalFile As String) As DiagnosticTaskItem
            Return New DiagnosticTaskItem(id, DiagnosticSeverity.Error, message, workspace, docId, mappedLine, originalLine, mappedColumn, originalColumn, mappedFile, originalFile)
        End Function

        Private Function SourceWarning(id As String, message As String, workspace As Workspace, docId As DocumentId, projId As ProjectId, mappedLine As Integer, originalLine As Integer,
                                       mappedColumn As Integer, originalColumn As Integer, mappedFile As String, originalFile As String) As DiagnosticTaskItem
            Return New DiagnosticTaskItem(id, DiagnosticSeverity.Warning, message, workspace, docId, mappedLine, originalLine, mappedColumn, originalColumn, mappedFile, originalFile)
        End Function

        Private Class DiagnosticTaskItem
            Inherits TaskItem
            Implements IErrorTaskItem

            Private ReadOnly _id As String
            Private ReadOnly _projectId As ProjectId
            Private ReadOnly _severity As DiagnosticSeverity

            Public Sub New(id As String, severity As DiagnosticSeverity, message As String, workspace As Workspace, docId As DocumentId,
                           mappedLine As Integer, originalLine As Integer, mappedColumn As Integer, originalColumn As Integer, mappedFile As String, originalFile As String)
                MyBase.New(message, workspace, docId, mappedLine, originalLine, mappedColumn, originalColumn, mappedFile, originalFile)
                Me._id = id
                Me._projectId = docId.ProjectId
                Me._severity = severity
            End Sub

            Public ReadOnly Property Id As String Implements IErrorTaskItem.Id
                Get
                    Return Me._id
                End Get
            End Property

            Public ReadOnly Property ProjectId As ProjectId Implements IErrorTaskItem.ProjectId
                Get
                    Return Me._projectId
                End Get
            End Property

            Public ReadOnly Property Severity As DiagnosticSeverity Implements IErrorTaskItem.Severity
                Get
                    Return Me._severity
                End Get
            End Property

            Public Overrides Function Equals(obj As Object) As Boolean
                Dim other As IErrorTaskItem = TryCast(obj, IErrorTaskItem)
                If other Is Nothing Then
                    Return False
                End If

                If Not AbstractTaskItem.Equals(Me, other) Then
                    Return False
                End If

                Return Id = other.Id AndAlso ProjectId = other.ProjectId AndAlso Severity = other.Severity
            End Function

            Public Overrides Function GetHashCode() As Integer
                Return Hash.Combine(AbstractTaskItem.GetHashCode(Me), Hash.Combine(Id.GetHashCode(), CType(Severity, Integer)))
            End Function
        End Class
    End Class
End Namespace
