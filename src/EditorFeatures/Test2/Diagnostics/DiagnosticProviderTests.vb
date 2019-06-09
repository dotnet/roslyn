' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CSharp
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Shared.Options
Imports Microsoft.CodeAnalysis.SolutionCrawler
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics.UnitTests

    ''' <summary>
    ''' Tests for Error List. Since it is language agnostic there are no C# or VB Specific tests
    ''' </summary>
    <[UseExportProvider]>
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
                                        class Goo { }
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
                                        class Goo { dontcompile }
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
                                        class Goo { dontcompile }
                                        #line 1000
                                        class Goo2 { dontcompile }
                                        #line default
                                        class Goo4 { dontcompile }
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
                                        class Goo { int a = "test"; }
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
                                        class Goo { gibberish }
                                        class Goo2 { as; }
                                        class Goo3 { long q = 1l; }
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
            '    Message = "The field 'Goo3.q' is assigned but its value is never used" />

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
                                        Class GooClass
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
                                        Class GooClass
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

        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub DiagnosticsInNoCompilationProjects()
            Dim test =
                <Workspace>
                    <Project Language="NoCompilation">
                        <Document FilePath="A.ts">
                            Dummy content.
                        </Document>
                    </Project>
                </Workspace>

            Dim diagnostics =
                <Diagnostics>
                    <Error Id=<%= NoCompilationDocumentDiagnosticAnalyzer.Descriptor.Id %>
                        MappedFile="A.ts" MappedLine="0" MappedColumn="0"
                        OriginalFile="A.ts" OriginalLine="0" OriginalColumn="0"
                        Message=<%= NoCompilationDocumentDiagnosticAnalyzer.Descriptor.MessageFormat.ToString() %>/>
                </Diagnostics>

            VerifyAllAvailableDiagnostics(test, diagnostics)
        End Sub

        Private Sub VerifyAllAvailableDiagnostics(test As XElement, diagnostics As XElement, Optional ordered As Boolean = True, Optional enabled As Boolean = True)
            Using workspace = TestWorkspace.CreateWorkspace(test)

                ' turn off diagnostic
                If Not enabled Then
                    workspace.Options = workspace.Options _
                                                  .WithChangedOption(ServiceComponentOnOffOptions.DiagnosticProvider, False) _
                                                  .WithChangedOption(ServiceFeatureOnOffOptions.ClosedFileDiagnostic, LanguageNames.CSharp, False) _
                                                  .WithChangedOption(ServiceFeatureOnOffOptions.ClosedFileDiagnostic, LanguageNames.VisualBasic, False)
                End If

                Dim registrationService = workspace.Services.GetService(Of ISolutionCrawlerRegistrationService)()
                registrationService.Register(workspace)

                Dim diagnosticProvider = GetDiagnosticProvider(workspace)
                Dim actualDiagnostics = diagnosticProvider.GetCachedDiagnosticsAsync(workspace).Result

                registrationService.Unregister(workspace)

                If diagnostics Is Nothing Then
                    Assert.Equal(0, actualDiagnostics.Length)
                Else
                    Dim expectedDiagnostics = GetExpectedDiagnostics(workspace, diagnostics)

                    If ordered Then
                        AssertEx.Equal(expectedDiagnostics, actualDiagnostics, New Comparer())
                    Else
                        AssertEx.SetEqual(expectedDiagnostics, actualDiagnostics, New Comparer())
                    End If
                End If
            End Using
        End Sub

        Private Function GetDiagnosticProvider(workspace As TestWorkspace) As DiagnosticAnalyzerService
            Dim snapshot = workspace.CurrentSolution

            Dim notificationService = New TestForegroundNotificationService()

            Dim compilerAnalyzersMap = DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap().Add(
                NoCompilationConstants.LanguageName, ImmutableArray.Create(Of DiagnosticAnalyzer)(New NoCompilationDocumentDiagnosticAnalyzer()))

            Dim analyzerService = New TestDiagnosticAnalyzerService(compilerAnalyzersMap)

            ' CollectErrors generates interleaved background and foreground tasks.
            Dim service = DirectCast(workspace.Services.GetService(Of ISolutionCrawlerRegistrationService)(), SolutionCrawlerRegistrationService)
            service.WaitUntilCompletion_ForTestingPurposesOnly(workspace, SpecializedCollections.SingletonEnumerable(analyzerService.CreateIncrementalAnalyzer(workspace)).WhereNotNull().ToImmutableArray())

            Return analyzerService
        End Function

        Private Function GetExpectedDiagnostics(workspace As TestWorkspace, diagnostics As XElement) As List(Of DiagnosticData)
            Dim result As New List(Of DiagnosticData)
            Dim mappedLine As Integer, mappedColumn As Integer, originalLine As Integer, originalColumn As Integer
            Dim Id As String, message As String, originalFile As String, mappedFile As String
            Dim documentId As DocumentId

            For Each diagnostic As XElement In diagnostics.Elements()
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
                    result.Add(SourceError(Id, message, documentId, documentId.ProjectId, mappedLine, originalLine, mappedColumn, originalColumn, mappedFile, originalFile))
                Else
                    result.Add(SourceWarning(Id, message, documentId, documentId.ProjectId, mappedLine, originalLine, mappedColumn, originalColumn, mappedFile, originalFile))
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

        Private Function SourceError(id As String, message As String, docId As DocumentId, projId As ProjectId, mappedLine As Integer, originalLine As Integer, mappedColumn As Integer,
            originalColumn As Integer, mappedFile As String, originalFile As String) As DiagnosticData
            Return CreateDiagnostic(id, message, DiagnosticSeverity.Error, docId, projId, mappedLine, originalLine, mappedColumn, originalColumn, mappedFile, originalFile)
        End Function

        Private Function SourceWarning(id As String, message As String, docId As DocumentId, projId As ProjectId, mappedLine As Integer, originalLine As Integer, mappedColumn As Integer,
            originalColumn As Integer, mappedFile As String, originalFile As String) As DiagnosticData
            Return CreateDiagnostic(id, message, DiagnosticSeverity.Warning, docId, projId, mappedLine, originalLine, mappedColumn, originalColumn, mappedFile, originalFile)
        End Function

        Private Function CreateDiagnostic(id As String, message As String, severity As DiagnosticSeverity, docId As DocumentId, projId As ProjectId, mappedLine As Integer, originalLine As Integer, mappedColumn As Integer,
            originalColumn As Integer, mappedFile As String, originalFile As String) As DiagnosticData
            Return New DiagnosticData(id, "test", message, message, severity, severity, True, 0,
                                      ImmutableArray(Of String).Empty, ImmutableDictionary(Of String, String).Empty,
                                      projId, New DiagnosticDataLocation(docId, Nothing,
                                        originalFile, originalLine, originalColumn, originalLine, originalColumn,
                                        mappedFile, mappedLine, mappedColumn, mappedLine, mappedColumn),
                                      Nothing, Nothing, Nothing)
        End Function

        Private Class Comparer
            Implements IEqualityComparer(Of DiagnosticData)

            Public Overloads Function Equals(x As DiagnosticData, y As DiagnosticData) As Boolean Implements IEqualityComparer(Of DiagnosticData).Equals
                Return x.Id = y.Id AndAlso
                       x.Message = y.Message AndAlso
                       x.Severity = y.Severity AndAlso
                       x.ProjectId = y.ProjectId AndAlso
                       x.DocumentId = y.DocumentId AndAlso
                       Equals(x.DataLocation?.OriginalStartLine, y.DataLocation?.OriginalStartLine) AndAlso
                       Equals(x.DataLocation?.OriginalStartColumn, y.DataLocation?.OriginalStartColumn)
            End Function

            Public Overloads Function GetHashCode(obj As DiagnosticData) As Integer Implements IEqualityComparer(Of DiagnosticData).GetHashCode
                Return Hash.Combine(obj.Id,
                       Hash.Combine(obj.Message,
                       Hash.Combine(obj.ProjectId,
                       Hash.Combine(obj.DocumentId,
                       Hash.Combine(If(obj.DataLocation?.OriginalStartLine, 0),
                       Hash.Combine(If(obj.DataLocation?.OriginalStartColumn, 0), obj.Severity))))))
            End Function
        End Class
    End Class
End Namespace
