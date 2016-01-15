' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.UnitTests

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.AdditionalFiles
    Public Class AdditionalFileDiagnosticsTests
        Inherits AbstractCrossLanguageUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace, language As String) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(New AdditionalFileAnalyzer(), New AdditionalFileFixer())
        End Function

        <WpfFact>
        Public Async Function TestAdditionalFiles() As Task
            Dim input =
                <Workspace>
                    <Project Language='C#' AssemblyName='CSharpAssembly1' CommonReferences='true'>
                        <Document FilePath="Test1.cs">
                            using System.Runtime.Serialization;
                            public class Cl$$ass1 : ISerializable
                            {
                                public void GetObjectData(SerializationInfo info, StreamingContext context)
                                {
                                    throw new NotImplementedException();
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>

            Using workspace = Await TestWorkspaceFactory.CreateWorkspaceAsync(input)
                Dim project = workspace.Projects.First()
                Dim newSln = workspace.CurrentSolution.AddAdditionalDocument(DocumentId.CreateNewId(project.Id), "App.Config", SourceText.From("false"))
                workspace.TryApplyChanges(newSln)

                Dim diagnosticAndFix = Await GetDiagnosticAndFixAsync(workspace)
                Dim codeAction = diagnosticAndFix.Item2.Fixes.First().Action
                Dim operations = codeAction.GetOperationsAsync(CancellationToken.None).Result
                Dim edit = operations.OfType(Of ApplyChangesOperation)().First()

                Dim oldSolution = workspace.CurrentSolution
                Dim updatedSolution = edit.ChangedSolution

                Dim updatedDocument = SolutionUtilities.GetSingleChangedAdditionalDocument(oldSolution, updatedSolution)

                Dim actual = updatedDocument.GetTextAsync().Result.ToString().Trim()

                Assert.Equal("true", actual)
            End Using
        End Function
    End Class

    Public Class AdditionalFileAnalyzer
        Inherits DiagnosticAnalyzer

        Public Shared Rule As New DiagnosticDescriptor("OA1001", "Options test", "Serialization support has not been requested", "Test", DiagnosticSeverity.Error, True)

        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(Rule)
            End Get
        End Property

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterSymbolAction(AddressOf AnalyzeSymbol, SymbolKind.NamedType)
        End Sub

        Private Function IsSerializationAllowed(options As AnalyzerOptions) As Boolean
            Dim serializationAllowed = False
            For Each item In options.AdditionalFiles
                If item.Path.EndsWith("app.config", StringComparison.OrdinalIgnoreCase) Then
                    Dim text = item.GetText()
                    Boolean.TryParse(text.Lines(0).ToString(), serializationAllowed)
                End If
            Next

            Return serializationAllowed
        End Function

        Public Sub AnalyzeSymbol(context As SymbolAnalysisContext)
            Dim namedType = DirectCast(context.Symbol, INamedTypeSymbol)

            If namedType.AllInterfaces.Contains(context.Compilation.GetTypeByMetadataName("System.Runtime.Serialization.ISerializable")) Then
                If Not IsSerializationAllowed(context.Options) Then
                    context.ReportDiagnostic(Diagnostic.Create(Rule, context.Symbol.Locations.First()))
                End If
            End If
        End Sub
    End Class

    Public Class AdditionalFileFixer
        Inherits CodeFixProvider

        Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(AdditionalFileAnalyzer.Rule.Id)
            End Get
        End Property

        Public NotOverridable Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim project = context.Document.Project

            Dim appConfigDoc = project.AdditionalDocuments.Where(Function(d) d.Name.EndsWith("app.config", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault()

            If appConfigDoc IsNot Nothing Then
                Dim text = Await appConfigDoc.GetTextAsync().ConfigureAwait(False)
                Dim newText = "true"
                Dim newSln = appConfigDoc.Project.Solution.WithAdditionalDocumentText(appConfigDoc.Id, SourceText.From("true", text.Encoding))

#Disable Warning RS0005
                context.RegisterCodeFix(CodeAction.Create("Request serialization permission", Function(ct) Task.FromResult(newSln)), context.Diagnostics)
#Enable Warning RS0005
            End If
        End Function
    End Class
End Namespace
