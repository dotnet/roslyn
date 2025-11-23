// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.EditAndContinue.UnitTests;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.TaskList;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Threading;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Diagnostics;

public sealed class PullDiagnosticTests(ITestOutputHelper testOutputHelper) : AbstractPullDiagnosticTestsBase(testOutputHelper)
{
    #region Document Diagnostics

    [Theory, CombinatorialData]
    public async Task TestNoDocumentDiagnosticsForClosedFilesWithFSAOff(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup = @"class A {";
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(markup, mutatingLspWorkspace, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics);

        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

        var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI(), useVSDiagnostics);
        Assert.Empty(results.Single().Diagnostics!);

        // Verify document pull diagnostics are unaffected by running code analysis.
        await testLspServer.RunCodeAnalysisAsync(document.Project.Id);
        results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI(), useVSDiagnostics);
        Assert.Empty(results.Single().Diagnostics!);
    }

    [Theory, CombinatorialData]
    public async Task TestDocumentDiagnosticsForOpenFilesWithFSAOff(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup = @"class A {";
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(markup, mutatingLspWorkspace, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics);

        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

        await OpenDocumentAsync(testLspServer, document);

        var results = await RunGetDocumentPullDiagnosticsAsync(
            testLspServer, document.GetURI(), useVSDiagnostics);

        Assert.Equal("CS1513", results.Single().Diagnostics!.Single().Code);
        Assert.NotNull(results.Single().Diagnostics!.Single().CodeDescription!.Href.ParsedUri);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/fsharp/issues/15972")]
    public async Task TestDocumentDiagnosticsForOpenFilesWithFSAOff_Categories(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup = @"class A : B {";

        var additionalAnalyzers = new DiagnosticAnalyzer[] { new CSharpSyntaxAnalyzer(), new CSharpSemanticAnalyzer() };
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
            markup, mutatingLspWorkspace, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics, additionalAnalyzers: additionalAnalyzers);

        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

        await OpenDocumentAsync(testLspServer, document);

        var syntaxResults = await RunGetDocumentPullDiagnosticsAsync(
            testLspServer, document.GetURI(), useVSDiagnostics, category: PullDiagnosticCategories.DocumentCompilerSyntax);

        var semanticResults = await RunGetDocumentPullDiagnosticsAsync(
            testLspServer, document.GetURI(), useVSDiagnostics, category: PullDiagnosticCategories.DocumentCompilerSemantic);

        Assert.Equal("CS1513", syntaxResults.Single().Diagnostics!.Single().Code);
        Assert.Equal("CS0246", semanticResults.Single().Diagnostics!.Single().Code);

        var syntaxResults2 = await RunGetDocumentPullDiagnosticsAsync(
            testLspServer, document.GetURI(), useVSDiagnostics, previousResultId: syntaxResults.Single().ResultId, category: PullDiagnosticCategories.DocumentCompilerSyntax);
        var semanticResults2 = await RunGetDocumentPullDiagnosticsAsync(
            testLspServer, document.GetURI(), useVSDiagnostics, previousResultId: semanticResults.Single().ResultId, category: PullDiagnosticCategories.DocumentCompilerSemantic);

        Assert.Equal(syntaxResults.Single().ResultId, syntaxResults2.Single().ResultId);
        Assert.Equal(semanticResults.Single().ResultId, semanticResults2.Single().ResultId);

        var syntaxAnalyzerResults = await RunGetDocumentPullDiagnosticsAsync(
            testLspServer, document.GetURI(), useVSDiagnostics, category: PullDiagnosticCategories.DocumentAnalyzerSyntax);

        var semanticAnalyzerResults = await RunGetDocumentPullDiagnosticsAsync(
            testLspServer, document.GetURI(), useVSDiagnostics, category: PullDiagnosticCategories.DocumentAnalyzerSemantic);

        Assert.Equal(CSharpSyntaxAnalyzer.RuleId, syntaxAnalyzerResults.Single().Diagnostics!.Single().Code);
        Assert.Equal(CSharpSemanticAnalyzer.RuleId, semanticAnalyzerResults.Single().Diagnostics!.Single().Code);

        var syntaxAnalyzerResults2 = await RunGetDocumentPullDiagnosticsAsync(
            testLspServer, document.GetURI(), useVSDiagnostics, previousResultId: syntaxAnalyzerResults.Single().ResultId, category: PullDiagnosticCategories.DocumentAnalyzerSyntax);
        var semanticAnalyzerResults2 = await RunGetDocumentPullDiagnosticsAsync(
            testLspServer, document.GetURI(), useVSDiagnostics, previousResultId: semanticAnalyzerResults.Single().ResultId, category: PullDiagnosticCategories.DocumentAnalyzerSemantic);

        Assert.Equal(syntaxAnalyzerResults.Single().ResultId, syntaxAnalyzerResults2.Single().ResultId);
        Assert.Equal(semanticAnalyzerResults.Single().ResultId, semanticAnalyzerResults2.Single().ResultId);
    }

    private sealed class CSharpSyntaxAnalyzer : DiagnosticAnalyzer
    {
        public const string RuleId = "SYN0001";
        private readonly DiagnosticDescriptor _descriptor = new(RuleId, "Title", "Message", "Category", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_descriptor];

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxTreeAction(context =>
                context.ReportDiagnostic(Diagnostic.Create(_descriptor, context.Tree.GetRoot(context.CancellationToken).GetLocation())));
        }
    }

    private sealed class CSharpSemanticAnalyzer : DiagnosticAnalyzer
    {
        public const string RuleId = "SEM0001";
        private readonly DiagnosticDescriptor _descriptor = new(RuleId, "Title", "Message", "Category", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_descriptor];

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(context =>
                context.ReportDiagnostic(Diagnostic.Create(_descriptor, context.Node.GetLocation())),
                CSharp.SyntaxKind.CompilationUnit);
        }
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/65172")]
    public async Task TestDocumentDiagnosticsHasVSExpandedMessage(bool mutatingLspWorkspace)
    {
        var markup =
            """
            internal class Program
            {
                static void Main(string[] args)
                {
                }
            }
            """;
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(markup, mutatingLspWorkspace, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics: true);

        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

        await OpenDocumentAsync(testLspServer, document);

        var results = await RunGetDocumentPullDiagnosticsAsync(
            testLspServer, document.GetURI(), useVSDiagnostics: true);
        Assert.Equal("IDE0060", results.Single().Diagnostics!.Single().Code);
        var vsDiagnostic = (VSDiagnostic)results.Single().Diagnostics!.Single();
        Assert.Equal(vsDiagnostic.ExpandedMessage, AnalyzersResources.Avoid_unused_parameters_in_your_code_If_the_parameter_cannot_be_removed_then_change_its_name_so_it_starts_with_an_underscore_and_is_optionally_followed_by_an_integer_such_as__comma__1_comma__2_etc_These_are_treated_as_special_discard_symbol_names);
    }

    [Theory, CombinatorialData, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2050705")]
    public async Task TestDocumentDiagnosticsUsesNullForExpandedMessage(bool mutatingLspWorkspace)
    {
        var markup = @"class A {";
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(markup, mutatingLspWorkspace, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics: true);

        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

        await OpenDocumentAsync(testLspServer, document);

        var results = await RunGetDocumentPullDiagnosticsAsync(
            testLspServer, document.GetURI(), useVSDiagnostics: true);

        Assert.Equal("CS1513", results.Single().Diagnostics!.Single().Code);
        var vsDiagnostic = (VSDiagnostic)results.Single().Diagnostics!.Single();
        Assert.Null(vsDiagnostic.ExpandedMessage);
    }

    [Theory, CombinatorialData]
    public async Task TestDocumentTodoCommentsDiagnosticsForOpenFile_Category(bool mutatingLspWorkspace)
    {
        var markup =
            """
            // todo: goo
            class A {
            }
            """;
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(markup, mutatingLspWorkspace, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics: true);

        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

        await OpenDocumentAsync(testLspServer, document);

        var results = await RunGetDocumentPullDiagnosticsAsync(
            testLspServer, document.GetURI(), useVSDiagnostics: true, category: PullDiagnosticCategories.Task);

        Assert.Equal("TODO", results.Single().Diagnostics!.Single().Code);
        Assert.Equal("todo: goo", results.Single().Diagnostics!.Single().Message);
    }

    [Theory, CombinatorialData]
    public async Task TestDocumentDiagnosticsForOpenFilesIfDefaultAndFeatureFlagOn(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup = @"class A {";
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace,
            GetInitializationOptions(BackgroundAnalysisScope.OpenFiles, CompilerDiagnosticsScope.OpenFiles, useVSDiagnostics));

        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();
        await OpenDocumentAsync(testLspServer, document);

        var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI(), useVSDiagnostics);
        Assert.Equal("CS1513", results.Single().Diagnostics!.Single().Code);
    }

    [Theory, CombinatorialData]
    public async Task TestDocumentDiagnosticsForRemovedDocument(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup = @"class A {";
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(markup, mutatingLspWorkspace, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics);
        var workspace = testLspServer.TestWorkspace;

        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

        // Get the diagnostics for the solution containing the doc.
        var solution = document.Project.Solution;

        await OpenDocumentAsync(testLspServer, document);

        var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI(), useVSDiagnostics).ConfigureAwait(false);

        Assert.Equal("CS1513", results.Single().Diagnostics!.Single().Code);

        // Now remove the doc.
        workspace.OnDocumentRemoved(workspace.Documents.Single().Id);
        await CloseDocumentAsync(testLspServer, document);

        results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI(), useVSDiagnostics, results.Single().ResultId).ConfigureAwait(false);

        // VS represents removal with null diagnostics, VS code represents with an empty diagnostics array.
        Assert.Equal(useVSDiagnostics ? null : [], results.Single().Diagnostics);
        Assert.Null(results.Single().ResultId);
    }

    [Theory, CombinatorialData]
    public async Task TestNoChangeIfDocumentDiagnosticsCalledTwice(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup = @"class A {";
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(markup, mutatingLspWorkspace, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics);

        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

        await OpenDocumentAsync(testLspServer, document);

        var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI(), useVSDiagnostics);

        Assert.Equal("CS1513", results.Single().Diagnostics!.Single().Code);

        var resultId = results.Single().ResultId;
        results = await RunGetDocumentPullDiagnosticsAsync(
            testLspServer, document.GetURI(), useVSDiagnostics, previousResultId: resultId);

        Assert.Null(results.Single().Diagnostics);
        Assert.Equal(resultId, results.Single().ResultId);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1481208")]
    public async Task TestDocumentDiagnosticsWhenGlobalStateChanges(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup = @"class A {";
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(markup, mutatingLspWorkspace, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics);

        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

        await OpenDocumentAsync(testLspServer, document);

        var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI(), useVSDiagnostics);

        Assert.Equal("CS1513", results.Single().Diagnostics!.Single().Code);

        var resultId = results.Single().ResultId;

        // Trigger refresh due to a change to global state that affects diagnostics.
        var refresher = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<IDiagnosticsRefresher>();
        refresher.RequestWorkspaceRefresh();

        results = await RunGetDocumentPullDiagnosticsAsync(
            testLspServer, document.GetURI(), useVSDiagnostics, previousResultId: resultId);

        // Diagnostics should be re-calculated, but re-use the same resultId since they are the same).
        Assert.Equal(resultId, results.Single().ResultId);
    }

    [Theory, CombinatorialData]
    public async Task TestDocumentDiagnosticsRemovedAfterErrorIsFixed(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup = @"class A {";
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(markup, mutatingLspWorkspace, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics);

        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();
        var text = await document.GetTextAsync();

        await OpenDocumentAsync(testLspServer, document);

        var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI(), useVSDiagnostics);
        Assert.Equal("CS1513", results[0].Diagnostics!.Single().Code);

        await InsertTextAsync(testLspServer, document, text.Length, "}");

        results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI(), useVSDiagnostics, results.Single().ResultId);
        AssertEx.Empty(results[0].Diagnostics);
    }

    [Theory, CombinatorialData]
    public async Task TestDocumentDiagnosticsRemainAfterErrorIsNotFixed(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup = @"class A {";
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(markup, mutatingLspWorkspace, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics);

        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();
        var text = await document.GetTextAsync();

        await OpenDocumentAsync(testLspServer, document);
        var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI(), useVSDiagnostics);
        Assert.Equal("CS1513", results[0].Diagnostics!.Single().Code);
        Assert.Equal(new Position { Line = 0, Character = 9 }, results[0].Diagnostics!.Single().Range.Start);

        await InsertTextAsync(testLspServer, document, position: 0, text: " ");

        results = await RunGetDocumentPullDiagnosticsAsync(
            testLspServer, document.GetURI(),
            useVSDiagnostics,
            previousResultId: results[0].ResultId);
        Assert.Equal("CS1513", results[0].Diagnostics!.Single().Code);
        Assert.Equal(new Position { Line = 0, Character = 10 }, results[0].Diagnostics!.Single().Range.Start);
    }

    [Theory, CombinatorialData]
    public async Task TestDocumentDiagnosticsAreNotMapped(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup =
            """
            #line 1 "test.txt"
            class A {
            """;
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(markup, mutatingLspWorkspace, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics);

        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

        await OpenDocumentAsync(testLspServer, document);

        var results = await RunGetDocumentPullDiagnosticsAsync(
            testLspServer, document.GetURI(), useVSDiagnostics);

        Assert.Equal("CS1513", results.Single().Diagnostics!.Single().Code);
        Assert.Equal(1, results.Single().Diagnostics!.Single().Range.Start.Line);
    }

    [Theory, CombinatorialData]
    public async Task TestStreamingDocumentDiagnostics(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup = @"class A {";
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(markup, mutatingLspWorkspace, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics);

        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

        await OpenDocumentAsync(testLspServer, document);

        var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI(), useVSDiagnostics, useProgress: true);

        Assert.Equal("CS1513", results!.Single().Diagnostics!.Single().Code);
    }

    [Theory, CombinatorialData]
    public async Task TestDocumentDiagnosticsForOpenFilesUsesActiveContext(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var documentText =
            """
            #if ONE
            class A {
            #endif
            class B {
            """;
        var workspaceXml =
            $"""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj1" PreprocessorSymbols="ONE">
                    <Document FilePath="C:\C.cs">{documentText}</Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj2">
                    <Document IsLinkFile="true" LinkFilePath="C:\C.cs" LinkAssemblyName="CSProj1">{documentText}</Document>
                </Project>
            </Workspace>
            """;

        await using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, mutatingLspWorkspace, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics);

        var csproj1Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj1").Single().Documents.First();
        var csproj2Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj2").Single().Documents.First();

        // Open either of the documents via LSP, we're tracking the URI and text.
        await OpenDocumentAsync(testLspServer, csproj1Document);

        // If we don't have a mutating workspace, we need to manually open all linked documents in the workspace (otherwise updating the context will not succeed).
        if (!mutatingLspWorkspace)
        {
            await testLspServer.OpenDocumentInWorkspaceAsync(csproj2Document.Id, openAllLinkedDocuments: true);
        }

        // Set CSProj2 as the active context and get diagnostics.
        testLspServer.TestWorkspace.SetDocumentContext(csproj2Document.Id);
        var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, csproj2Document.GetURI(), useVSDiagnostics);
        Assert.Equal("CS1513", results.Single().Diagnostics!.Single().Code);
        if (useVSDiagnostics)
        {
            // Only VSDiagnostics will have the project.
            var vsDiagnostic = (LSP.VSDiagnostic)results.Single().Diagnostics!.Single();
            Assert.Equal("CSProj2", vsDiagnostic.Projects!.Single().ProjectName);
        }

        // Set CSProj1 as the active context and get diagnostics.
        testLspServer.TestWorkspace.SetDocumentContext(csproj1Document.Id);
        results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, csproj1Document.GetURI(), useVSDiagnostics);
        Assert.Equal(2, results.Single().Diagnostics!.Length);
        AssertEx.All(results.Single().Diagnostics, d => Assert.Equal("CS1513", d.Code));

        if (useVSDiagnostics)
        {
            AssertEx.All(results.Single().Diagnostics, d => Assert.Equal("CSProj1", ((VSDiagnostic)d).Projects!.Single().ProjectName));
        }
    }

    [Theory, CombinatorialData]
    public async Task TestDocumentDiagnosticsHasSameIdentifierForLinkedFile(bool mutatingLspWorkspace)
    {
        var documentText = @"class A { err }";
        var workspaceXml =
            $"""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj1" PreprocessorSymbols="ONE">
                    <Document FilePath="C:\C.cs">{documentText}</Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj2">
                    <Document IsLinkFile="true" LinkFilePath="C:\C.cs" LinkAssemblyName="CSProj1">{documentText}</Document>
                </Project>
            </Workspace>
            """;

        await using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, mutatingLspWorkspace, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics: false);

        var csproj1Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj1").Single().Documents.First();
        var csproj2Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj2").Single().Documents.First();

        // Open either of the documents via LSP, we're tracking the URI and text.
        await OpenDocumentAsync(testLspServer, csproj1Document);

        var csproj1Results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, GetVsTextDocumentIdentifier(csproj1Document), useVSDiagnostics: true);
        var csproj1Diagnostic = (VSDiagnostic)csproj1Results.Single().Diagnostics!.Single();
        var csproj2Results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, GetVsTextDocumentIdentifier(csproj2Document), useVSDiagnostics: true);
        var csproj2Diagnostic = (VSDiagnostic)csproj2Results.Single().Diagnostics!.Single();
        Assert.Equal(csproj1Diagnostic.Identifier, csproj2Diagnostic.Identifier);

        static VSTextDocumentIdentifier GetVsTextDocumentIdentifier(Document document)
        {
            var projectContext = new VSProjectContext
            {
                Id = ProtocolConversions.ProjectIdToProjectContextId(document.Project.Id),
                Label = document.Project.Name
            };
            return new VSTextDocumentIdentifier
            {
                ProjectContext = projectContext,
                DocumentUri = document.GetURI(),
            };
        }
    }

    [Theory, CombinatorialData]
    public async Task TestDocumentDiagnosticsWithChangeInReferencedProject(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup1 =
            """
            namespace M
            {
                class A : B { }
            }
            """;
        var markup2 =
            """
            namespace M
            {
                public class {|caret:|} { }
            }
            """;

        var workspaceXml =
            $"""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj1">
                    <Document FilePath="C:\A.cs">{markup1}</Document>
                    <ProjectReference>CSProj2</ProjectReference>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj2">
                    <Document FilePath="C:\B.cs">{markup2}</Document>
                </Project>
            </Workspace>
            """;

        await using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics).ConfigureAwait(false);
        var csproj1Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj1").Single().Documents.First();
        var csproj2Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj2").Single().Documents.First();

        await testLspServer.OpenDocumentAsync(csproj1Document.GetURI());
        await testLspServer.OpenDocumentAsync(csproj2Document.GetURI());

        // Verify we a diagnostic in A.cs since B does not exist.
        var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, csproj1Document.GetURI(), useVSDiagnostics);
        Assert.Single(results);
        Assert.Equal("CS0246", results.Single().Diagnostics!.Single().Code);

        // Insert B into B.cs and verify that the error in A.cs is now gone.
        var locationToReplace = testLspServer.GetLocations("caret").Single().Range;
        await testLspServer.ReplaceTextAsync(csproj2Document.GetURI(), (locationToReplace, "B"));
        var originalResultId = results.Single().ResultId;
        results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, csproj1Document.GetURI(), useVSDiagnostics, originalResultId);
        Assert.Single(results);
        AssertEx.Empty(results.Single().Diagnostics);
        Assert.NotEqual(originalResultId, results.Single().ResultId);
    }

    [Theory, CombinatorialData]
    public async Task TestDocumentDiagnosticsWithChangeInNotReferencedProject(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup1 =
            """
            namespace M
            {
                class A : B { }
            }
            """;
        var markup2 =
            """
            namespace M
            {
                public class {|caret:|} { }
            }
            """;

        var workspaceXml =
            $"""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj1">
                    <Document FilePath="C:\A.cs">{markup1}</Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj2">
                    <Document FilePath="C:\B.cs">{markup2}</Document>
                </Project>
            </Workspace>
            """;

        await using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics).ConfigureAwait(false);
        var csproj1Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj1").Single().Documents.First();
        var csproj2Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj2").Single().Documents.First();

        await testLspServer.OpenDocumentAsync(csproj1Document.GetURI());
        await testLspServer.OpenDocumentAsync(csproj2Document.GetURI());

        // Verify we get a diagnostic in A since the class B does not exist.
        var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, csproj1Document.GetURI(), useVSDiagnostics);
        Assert.Single(results);
        Assert.Equal("CS0246", results.Single().Diagnostics!.Single().Code);

        // Add B to CSProj2 and verify that we get an unchanged result (still has diagnostic) for A.cs
        // since CSProj1 does not reference CSProj2
        var locationToReplace = testLspServer.GetLocations("caret").Single().Range;
        await testLspServer.ReplaceTextAsync(csproj2Document.GetURI(), (locationToReplace, "B"));
        var originalResultId = results.Single().ResultId;
        results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, csproj1Document.GetURI(), useVSDiagnostics, originalResultId);
        Assert.Single(results);
        Assert.Null(results.Single().Diagnostics);
        Assert.Equal(originalResultId, results.Single().ResultId);
    }

    [Theory, CombinatorialData]
    public async Task TestDocumentDiagnosticsFromRazorServer(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup = @"class A {";

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace,
            GetInitializationOptions(BackgroundAnalysisScope.OpenFiles, CompilerDiagnosticsScope.OpenFiles, useVSDiagnostics, WellKnownLspServerKinds.RazorLspServer));

        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

        await OpenDocumentAsync(testLspServer, document);

        var results = await RunGetDocumentPullDiagnosticsAsync(
            testLspServer, document.GetURI(), useVSDiagnostics);

        // Assert that we have diagnostics even though the option is set to push.
        Assert.Equal("CS1513", results.Single().Diagnostics!.Single().Code);
        Assert.NotNull(results.Single().Diagnostics!.Single().CodeDescription!.Href.ParsedUri);
    }

    [Theory, CombinatorialData]
    public async Task TestDocumentDiagnosticsFromLiveShareServer(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup = @"class A {";

        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace,
            GetInitializationOptions(BackgroundAnalysisScope.OpenFiles, CompilerDiagnosticsScope.OpenFiles, useVSDiagnostics, WellKnownLspServerKinds.LiveShareLspServer));

        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

        await OpenDocumentAsync(testLspServer, document);

        var results = await RunGetDocumentPullDiagnosticsAsync(
            testLspServer, document.GetURI(), useVSDiagnostics);

        // Assert that we have diagnostics even though the option is set to push.
        Assert.Equal("CS1513", results.Single().Diagnostics!.Single().Code);
        Assert.NotNull(results.Single().Diagnostics!.Single().CodeDescription!.Href.ParsedUri);
    }

    [Theory, CombinatorialData]
    internal async Task TestDocumentDiagnosticsUpdatesSourceGeneratorDiagnostics(bool useVSDiagnostics, bool mutatingLspWorkspace, SourceGeneratorExecutionPreference executionPreference)
    {
        var markup = """
            public {|insert:|}class C
            {
                public int F => _field;
            }
            """;

        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
            markup, mutatingLspWorkspace, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics);

        // Set execution mode preference.
        var configService = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<TestWorkspaceConfigurationService>();
        configService.Options = new WorkspaceConfigurationOptions(SourceGeneratorExecution: executionPreference);

        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

        // Add a generator to the solution that reports a diagnostic if the text matches original markup
        var generator = new CallbackGenerator(onInit: (_) => { }, onExecute: context =>
        {
            var tree = context.Compilation.SyntaxTrees.Single();
            var text = tree.GetText(CancellationToken.None).ToString();
            if (text.Contains("public partial class C"))
            {
                context.AddSource("blah", """
                    public partial class C
                    {
                        private readonly int _field = 1;
                    }
                    """);
            }
        });

        testLspServer.TestWorkspace.OnAnalyzerReferenceAdded(
            document.Project.Id,
            new TestGeneratorReference(generator));
        await testLspServer.WaitForSourceGeneratorsAsync();

        await OpenDocumentAsync(testLspServer, document);

        // First diagnostic request should report a diagnostic since the generator does not produce any source (text does not match).
        var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI(), useVSDiagnostics);
        var firstResultId = results.Single().ResultId;
        var diagnostic = AssertEx.Single(results.Single().Diagnostics);
        Assert.Equal("CS0103", diagnostic.Code);

        // Update the text to include the text trigger the source generator relies on.
        var textLocation = testLspServer.GetLocations()["insert"].Single();

        var textEdit = new LSP.TextEdit { Range = textLocation.Range, NewText = "partial " };
        if (mutatingLspWorkspace)
        {
            // In the mutating workspace, we just need to update the LSP text (which will flow into the workspace).
            await testLspServer.ReplaceTextAsync(textLocation.DocumentUri, (textEdit.Range, textEdit.NewText));
            await WaitForWorkspaceOperationsAsync(testLspServer.TestWorkspace);
        }
        else
        {
            // In the non-mutating workspace, we need to ensure that both the workspace and LSP view of the world are updated.
            var workspaceText = await document.GetTextAsync(CancellationToken.None);
            var textChange = ProtocolConversions.TextEditToTextChange(textEdit, workspaceText);
            await testLspServer.TestWorkspace.ChangeDocumentAsync(document.Id, workspaceText.WithChanges(textChange));
            await testLspServer.ReplaceTextAsync(textLocation.DocumentUri, (textEdit.Range, textEdit.NewText));
        }

        await testLspServer.WaitForSourceGeneratorsAsync();
        results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI(), useVSDiagnostics, previousResultId: firstResultId);
        var secondResultId = results.Single().ResultId;

        if (executionPreference == SourceGeneratorExecutionPreference.Automatic)
        {
            // In automatic mode, the diagnostic should be immediately removed as the source generator ran and produced the required field.
            Assert.Empty(results.Single().Diagnostics!);
        }
        else
        {
            // In balanced mode, the diagnostic should be unchanged until there is a manual source generator run that updates the sg text.
            Assert.Null(results.Single().Diagnostics);
            Assert.Equal(firstResultId, secondResultId);

            testLspServer.TestWorkspace.EnqueueUpdateSourceGeneratorVersion(document.Project.Id, forceRegeneration: false);
            await testLspServer.WaitForSourceGeneratorsAsync();

            results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI(), useVSDiagnostics, previousResultId: secondResultId);
            var thirdResultId = results.Single().ResultId;
            AssertEx.NotNull(results.Single().Diagnostics);
            Assert.Empty(results.Single().Diagnostics!);
            Assert.NotEqual(firstResultId, thirdResultId);
        }
    }

    [Theory, CombinatorialData]
    public async Task TestDocumentDiagnosticsIncludesSourceGeneratorDiagnostics(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup = "// Hello, World";
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
            markup, mutatingLspWorkspace, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics);

        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

        var generator = new DiagnosticProducingGenerator(context =>
        {
            return Location.Create(context.Compilation.SyntaxTrees.Single(), new TextSpan(0, 10));
        });

        testLspServer.TestWorkspace.OnAnalyzerReferenceAdded(
            document.Project.Id,
            new TestGeneratorReference(generator));

        await OpenDocumentAsync(testLspServer, document);

        var results = await RunGetDocumentPullDiagnosticsAsync(
            testLspServer, document.GetURI(), useVSDiagnostics);

        var diagnostic = AssertEx.Single(results.Single().Diagnostics);
        Assert.Equal(DiagnosticProducingGenerator.Descriptor.Id, diagnostic.Code);
    }

    [Theory, CombinatorialData]
    public async Task TestDocumentDiagnosticsWithFadingOptionOn(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|first:using System.Linq;
            using System.Threading;|}
            class A
            {
            }
            """;
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(markup, mutatingLspWorkspace, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics);
        var firstLocation = testLspServer.GetLocations("first").Single().Range;
        testLspServer.TestWorkspace.GlobalOptions.SetGlobalOption(FadingOptions.FadeOutUnusedImports, LanguageNames.CSharp, true);

        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

        await OpenDocumentAsync(testLspServer, document);

        var results = await RunGetDocumentPullDiagnosticsAsync(
            testLspServer, document.GetURI(), useVSDiagnostics);

        if (useVSDiagnostics)
        {
            // We should have an unnecessary diagnostic marking all the usings.
            Assert.True(results.Single().Diagnostics![0].Tags!.Contains(DiagnosticTag.Unnecessary));
            Assert.Equal(firstLocation, results.Single().Diagnostics![1].Range);

            // We should have a regular diagnostic marking all the usings that doesn't fade.
            Assert.False(results.Single().Diagnostics![1].Tags!.Contains(DiagnosticTag.Unnecessary));
            Assert.Equal(firstLocation, results.Single().Diagnostics![1].Range);
        }
        else
        {
            // We should have just one diagnostic that fades since the public spec does not support fully hidden diagnostics.
            Assert.True(results.Single().Diagnostics![0].Tags!.Contains(DiagnosticTag.Unnecessary));
            Assert.Equal(firstLocation, results.Single().Diagnostics![0].Range);
        }
    }

    [Theory, CombinatorialData]
    public async Task TestDocumentDiagnosticsWithFadingOptionOff(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup =
            """
            {|first:using System.Linq;
            using System.Threading;|}
            class A
            {
            }
            """;
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(markup, mutatingLspWorkspace, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics);
        var firstLocation = testLspServer.GetLocations("first").Single().Range;
        testLspServer.TestWorkspace.GlobalOptions.SetGlobalOption(FadingOptions.FadeOutUnusedImports, LanguageNames.CSharp, false);

        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

        await OpenDocumentAsync(testLspServer, document);

        var results = await RunGetDocumentPullDiagnosticsAsync(
            testLspServer, document.GetURI(), useVSDiagnostics);

        AssertEx.All(results.Single().Diagnostics, d => Assert.False(d.Tags!.Contains(DiagnosticTag.Unnecessary)));
    }

    [Theory, CombinatorialData]
    public async Task TestDocumentDiagnosticsWithNotConfigurableFading(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup =
            """
            class A
            {
                void M()
                {
                    _ = {|line:{|open:(|}1 + 2 +|}
                        3 + 4{|close:)|};
                }
            }
            """;
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(markup, mutatingLspWorkspace, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics);
        var openLocation = testLspServer.GetLocations("open").Single().Range;
        var closeLocation = testLspServer.GetLocations("close").Single().Range;
        var lineLocation = testLspServer.GetLocations("line").Single().Range;

        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

        await OpenDocumentAsync(testLspServer, document);

        var results = await RunGetDocumentPullDiagnosticsAsync(
            testLspServer, document.GetURI(), useVSDiagnostics);

        if (useVSDiagnostics)
        {
            // The first line should have a diagnostic on it that is not marked as unnecessary.
            Assert.False(results.Single().Diagnostics![0].Tags!.Contains(DiagnosticTag.Unnecessary));
            Assert.Equal(lineLocation, results.Single().Diagnostics![0].Range);

            // The open paren should have an unnecessary diagnostic.
            Assert.True(results.Single().Diagnostics![1].Tags!.Contains(DiagnosticTag.Unnecessary));
            Assert.Equal(openLocation, results.Single().Diagnostics![1].Range);

            // The close paren should have an unnecessary diagnostic.
            Assert.True(results.Single().Diagnostics![2].Tags!.Contains(DiagnosticTag.Unnecessary));
            Assert.Equal(closeLocation, results.Single().Diagnostics![2].Range);
        }
        else
        {
            // There should be one unnecessary diagnostic.
            Assert.True(results.Single().Diagnostics!.Single().Tags!.Contains(DiagnosticTag.Unnecessary));
            Assert.Equal(lineLocation, results.Single().Diagnostics!.Single().Range);

            // There should be an additional location for the open paren.
            Assert.Equal(openLocation, results.Single().Diagnostics!.Single().RelatedInformation![0].Location.Range);

            // There should be an additional location for the close paren.
            Assert.Equal(closeLocation, results.Single().Diagnostics!.Single().RelatedInformation![1].Location.Range);
        }
    }

    [Theory, CombinatorialData, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1806590")]
    public async Task TestDocumentDiagnosticsForUnnecessarySuppressions(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup = "#pragma warning disable IDE0000";
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(markup, mutatingLspWorkspace, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics);

        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

        await OpenDocumentAsync(testLspServer, document);

        var results = await RunGetDocumentPullDiagnosticsAsync(
            testLspServer, document.GetURI(), useVSDiagnostics);

        Assert.Equal(IDEDiagnosticIds.RemoveUnnecessarySuppressionDiagnosticId, results.Single().Diagnostics!.Single().Code);
    }

    [Theory, CombinatorialData, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1824321")]
    public async Task TestDocumentDiagnosticsForSourceSuppressions(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup = """
            class C
            { 
                void M()
                {
            #pragma warning disable CS0168 // Variable is declared but never used
                    int x;
            #pragma warning restore CS0168 // Variable is declared but never used
                }
            }
            """;
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(markup, mutatingLspWorkspace, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics);

        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

        await OpenDocumentAsync(testLspServer, document);

        var results = await RunGetDocumentPullDiagnosticsAsync(
            testLspServer, document.GetURI(), useVSDiagnostics);

        AssertEx.Empty(results.Single().Diagnostics);
    }

    [Theory, CombinatorialData]
    public async Task TestInfoDiagnosticsAreReportedAsInformationInVS(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class A
            {
                public A SomeA = new A();
            }
            """;
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(markup, mutatingLspWorkspace, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics: true);

        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

        await OpenDocumentAsync(testLspServer, document);

        var results = await RunGetDocumentPullDiagnosticsAsync(
            testLspServer, document.GetURI(), useVSDiagnostics: true);

        Assert.Equal("IDE0090", results.Single().Diagnostics!.Single().Code);
        Assert.Equal(LSP.DiagnosticSeverity.Information, results.Single().Diagnostics!.Single().Severity);
    }

    [ConditionalTheory(typeof(UnixLikeOnly)), CombinatorialData]
    public async Task TestDocumentDiagnosticsWhenClientRequestsWithDifferentCasing(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup = @"class A {";

        var workspaceXml =
            $"""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj1">
                    <Document FilePath="/Users/ConsoleApp1/Class1.cs">{markup}</Document>
                </Project>
            </Workspace>
            """;

        await using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, mutatingLspWorkspace, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics);

        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

        var loweredUri = ProtocolConversions.CreateAbsoluteDocumentUri(document.FilePath!.ToLowerInvariant());
        await testLspServer.OpenDocumentAsync(loweredUri);

        var results = await RunGetDocumentPullDiagnosticsAsync(
            testLspServer, loweredUri, useVSDiagnostics);

        // When looking up the document based on a URI, the workspace ignores casing differences and can open the document with different casing.
        // When diagnostics checks if the document is open, it should also be able to tell that the document is open, even though the URI casing is different from the file path.

        Assert.Equal("CS1513", results.Single().Diagnostics!.Single().Code);
        Assert.NotNull(results.Single().Diagnostics!.Single().CodeDescription!.Href.ParsedUri);
    }

    #endregion

    #region Workspace Diagnostics

    [Theory, CombinatorialData]
    public async Task TestNoWorkspaceDiagnosticsForClosedFilesWithFSAOff(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup1 = @"class A {";
        var markup2 = "";
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
            [markup1, markup2], mutatingLspWorkspace, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics);

        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);

        Assert.Empty(results);
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceDiagnosticsForClosedFilesWithFSAOn(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup1 = @"class A {";
        var markup2 = "";
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
            [markup1, markup2], mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics);

        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);

        Assert.Equal(3, results.Length);
        Assert.Equal("CS1513", results[0].Diagnostics!.Single().Code);
        AssertEx.Empty(results[1].Diagnostics);
        AssertEx.Empty(results[2].Diagnostics);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/65967")]
    public async Task TestWorkspaceDiagnosticsForClosedFilesWithRunCodeAnalysisAndFSAOff(bool useVSDiagnostics, bool mutatingLspWorkspace, bool scopeRunCodeAnalysisToProject)
    {
        var markup1 = @"class A {";
        var markup2 = "";
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
            [markup1, markup2], mutatingLspWorkspace, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics);

        var projectId = scopeRunCodeAnalysisToProject ? testLspServer.GetCurrentSolution().Projects.Single().Id : null;
        await testLspServer.RunCodeAnalysisAsync(projectId);

        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);

        Assert.Equal(3, results.Length);
        Assert.Equal("CS1513", results[0].Diagnostics!.Single().Code);
        // this should be considered a build-error, since it was produced by the last code-analysis run.
        Assert.Contains(VSDiagnosticTags.BuildError, results[0].Diagnostics!.Single().Tags!);
        AssertEx.Empty(results[1].Diagnostics);
        AssertEx.Empty(results[2].Diagnostics);

        // Now fix the compiler error, but don't re-execute code analysis.
        // Verify that we still get the workspace diagnostics from the prior snapshot on which code analysis was executed.
        await InsertInClosedDocumentAsync(testLspServer, testLspServer.TestWorkspace.Documents.First().Id, "}");

        var results2 = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics, previousResults: CreateDiagnosticParamsFromPreviousReports(results));

        // We did not run code analysis - we should not get any new diagnostics.
        AssertEx.Empty(results2);

        // Re-run code analysis and verify up-to-date diagnostics are returned now, i.e. there are no compiler errors.
        await testLspServer.RunCodeAnalysisAsync(projectId);

        var results3 = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics, previousResults: CreateDiagnosticParamsFromPreviousReports(results));

        // We should get 1 changed diagnostic now that we have re-run code analysis, the rest are unchanged.
        Assert.Equal(1, results3.Length);
        AssertEx.Empty(results3[0].Diagnostics);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/65967")]
    public async Task TestWorkspaceDiagnosticsForClosedFilesWithRunCodeAnalysisFSAOn(bool useVSDiagnostics, bool mutatingLspWorkspace, bool scopeRunCodeAnalysisToProject)
    {
        var markup1 = @"class A {";
        var markup2 = "";
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
            [markup1, markup2], mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics);

        // Run code analysis on the initial project snapshot with compiler error.
        var projectId = scopeRunCodeAnalysisToProject ? testLspServer.GetCurrentSolution().Projects.Single().Id : null;
        await testLspServer.RunCodeAnalysisAsync(projectId);

        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);

        Assert.Equal(3, results.Length);
        Assert.Equal("CS1513", results[0].Diagnostics!.Single().Code);
        // this should *not* be considered a build-error, since it was produced by the live workspace results.
        Assert.DoesNotContain(VSDiagnosticTags.BuildError, results[0].Diagnostics!.Single().Tags!);
        AssertEx.Empty(results[1].Diagnostics);
        AssertEx.Empty(results[2].Diagnostics);

        // Now fix the compiler error, but don't rerun code analysis.
        // Verify that we get up-to-date workspace diagnostics, i.e. no compiler errors, from the current snapshot because FSA is enabled.
        await InsertInClosedDocumentAsync(testLspServer, testLspServer.TestWorkspace.Documents.First().Id, "}");

        var results2 = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics, previousResults: CreateDiagnosticParamsFromPreviousReports(results));

        // We should get 1 report clearing out the diagnostics for the changed file.  The other files have unchanged diagnostics and are not reported.
        Assert.Equal(1, results2.Length);
        AssertEx.Empty(results2[0].Diagnostics);

        // Now rerun code analysis and verify we still get up-to-date workspace diagnostics.
        await testLspServer.RunCodeAnalysisAsync(projectId);

        // Concat the single changed result from result2 with the unchanged reports from results to mimick the client re-using unchanged reports.
        var previousParams = results2.Concat(results[1]).Concat(results[2]);
        var results3 = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics, previousResults: CreateDiagnosticParamsFromPreviousReports(previousParams));

        // The diagnostics did not change, so we should get nothing back.
        AssertEx.Empty(results3);
    }

    [Theory, CombinatorialData]
    public async Task SourceGeneratorFailures_FSA(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        await using var testLspServer = await CreateTestLspServerAsync(["class C {}"], mutatingLspWorkspace,
            GetInitializationOptions(BackgroundAnalysisScope.FullSolution, CompilerDiagnosticsScope.FullSolution, useVSDiagnostics));

        var generator = new TestSourceGenerator()
        {
            ExecuteImpl = context => throw new InvalidOperationException("Source generator failed")
        };

        var solution = testLspServer.TestWorkspace.CurrentSolution;
        solution = solution.AddAnalyzerReference(solution.ProjectIds.Single(), new TestGeneratorReference(generator));
        Assert.True(testLspServer.TestWorkspace.TryApplyChanges(solution));

        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);

        Assert.Equal(2, results.Length);
        AssertEx.Empty(results[0].Diagnostics);
        Assert.True(results[1].Diagnostics!.Single().Message.Contains("Source generator failed"));
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceTodoForClosedFilesWithFSAOffAndTodoOff(bool mutatingLspWorkspace)
    {
        var markup1 =
            """
            // todo: goo
            class A {
            }
            """;
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
            [markup1], mutatingLspWorkspace, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics: true);

        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics: true, includeTaskListItems: false, category: PullDiagnosticCategories.Task);

        Assert.Empty(results);
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceTodoForClosedFilesWithFSAOffAndTodoOn(bool mutatingLspWorkspace)
    {
        var markup1 =
            """
            // todo: goo
            class A {
            }
            """;
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
            [markup1], mutatingLspWorkspace, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics: true);

        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics: true, includeTaskListItems: true, category: PullDiagnosticCategories.Task);

        Assert.Equal(1, results.Length);
        Assert.Equal("TODO", results[0].Diagnostics!.Single().Code);
        Assert.Equal("todo: goo", results[0].Diagnostics!.Single().Message);
        Assert.Equal(VSDiagnosticRank.Default, ((VSDiagnostic)results[0].Diagnostics!.Single()).DiagnosticRank);
    }

    [Theory]
    [InlineData("1", (int)VSDiagnosticRank.Low, false)]
    [InlineData("1", (int)VSDiagnosticRank.Low, true)]
    [InlineData("2", (int)VSDiagnosticRank.Default, false)]
    [InlineData("2", (int)VSDiagnosticRank.Default, true)]
    [InlineData("3", (int)VSDiagnosticRank.High, false)]
    [InlineData("3", (int)VSDiagnosticRank.High, true)]
    public async Task TestWorkspaceTodoForClosedFilesWithFSAOffAndTodoOn_Priorities(
        string priString, int intRank, bool mutatingLspWorkspace)
    {
        var rank = (VSDiagnosticRank)intRank;
        var markup1 =
            """
            // todo: goo
            class A {
            }
            """;
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
            [markup1], mutatingLspWorkspace, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics: true);

        testLspServer.TestWorkspace.GlobalOptions.SetGlobalOption(
            TaskListOptionsStorage.Descriptors,
            ["HACK:2", $"TODO:{priString}", "UNDONE:2", "UnresolvedMergeConflict:3"]);

        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics: true, includeTaskListItems: true, category: PullDiagnosticCategories.Task);

        Assert.Equal(1, results.Length);
        Assert.Equal("TODO", results[0].Diagnostics!.Single().Code);
        Assert.Equal("todo: goo", results[0].Diagnostics!.Single().Message);
        Assert.Equal(rank, ((VSDiagnostic)results[0].Diagnostics!.Single()).DiagnosticRank);
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceTodoForClosedFilesWithFSAOnAndTodoOff(bool mutatingLspWorkspace)
    {
        var markup1 =
            """
            // todo: goo
            class A {
            }
            """;
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
            [markup1], mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics: true);

        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics: true, includeTaskListItems: false, category: PullDiagnosticCategories.Task);

        Assert.Empty(results);
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceTodoForClosedFilesWithFSAOnAndTodoOn(bool mutatingLspWorkspace)
    {
        var markup1 =
            """
            // todo: goo
            class A {
            }
            """;
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
            [markup1], mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics: true);

        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics: true, includeTaskListItems: true, category: PullDiagnosticCategories.Task);

        Assert.Equal(1, results.Length);

        Assert.Equal("TODO", results[0].Diagnostics!.Single().Code);
        Assert.Equal("todo: goo", results[0].Diagnostics!.Single().Message);
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceTodoAndDiagnosticForClosedFilesWithFSAOnAndTodoOn(bool mutatingLspWorkspace)
    {
        var markup1 =
            """
            // todo: goo
            class A {

            """;
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
            [markup1], mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics: true);

        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics: true, includeTaskListItems: true, category: PullDiagnosticCategories.Task);

        Assert.Equal(1, results.Length);
        Assert.Equal("TODO", results[0].Diagnostics![0].Code);
    }

    [Theory, CombinatorialData]
    public async Task EditAndContinue_NonHostWorkspace(bool mutatingLspWorkspace)
    {
        var xmlWorkspace = """
            <Workspace>
                <Project Language='C#' CommonReferences='true' AssemblyName='Submission' Name='Submission1'>
                    <Document FilePath='C:\Submission#1.cs'>1+1</Document>
                </Project>
            </Workspace>
            """;

        var options = GetInitializationOptions(BackgroundAnalysisScope.OpenFiles, compilerDiagnosticsScope: null, useVSDiagnostics: false);
        await using var testLspServer = await CreateXmlTestLspServerAsync(xmlWorkspace, mutatingLspWorkspace, WorkspaceKind.Interactive, options);

        var document = testLspServer.TestWorkspace.CurrentSolution.Projects.Single().Documents.Single();
        await OpenDocumentAsync(testLspServer, document);

        var encSessionState = testLspServer.TestWorkspace.GetService<EditAndContinueSessionState>();

        // active session, but should get no EnC diagnostics for Interactive workspace
        encSessionState.IsSessionActive = true;

        var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI(), useVSDiagnostics: false, category: PullDiagnosticCategories.EditAndContinue);
        AssertEx.Empty(results.Single().Diagnostics);
    }

    [Theory, CombinatorialData]
    public async Task EditAndContinue_NoActiveSession(bool mutatingLspWorkspace)
    {
        var markup1 = "class C {}";

        var options = GetInitializationOptions(BackgroundAnalysisScope.OpenFiles, compilerDiagnosticsScope: null, useVSDiagnostics: false);

        await using var testLspServer = await CreateTestLspServerAsync([markup1], LanguageNames.CSharp, mutatingLspWorkspace, options);

        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics: false, includeTaskListItems: false, category: PullDiagnosticCategories.EditAndContinue);
        Assert.Empty(results);
    }

    [Theory, CombinatorialData]
    public async Task EditAndContinue(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var options = GetInitializationOptions(BackgroundAnalysisScope.OpenFiles, compilerDiagnosticsScope: null, useVSDiagnostics);
        var composition = Composition
            .AddExcludedPartTypes(typeof(EditAndContinueService))
            .AddParts(typeof(MockEditAndContinueService));

        await using var testLspServer = await CreateTestLspServerAsync(["class C;", "class D;"], LanguageNames.CSharp, mutatingLspWorkspace, options, composition);

        var encSessionState = testLspServer.TestWorkspace.GetService<EditAndContinueSessionState>();
        var encService = (MockEditAndContinueService)testLspServer.TestWorkspace.GetService<IEditAndContinueService>();
        var diagnosticsRefresher = testLspServer.TestWorkspace.GetService<IDiagnosticsRefresher>();

        var project = testLspServer.TestWorkspace.CurrentSolution.Projects.Single();
        var openDocument = project.Documents.First();
        var closedDocument = project.Documents.Skip(1).First();

        await OpenDocumentAsync(testLspServer, openDocument);

        var projectDiagnostic = CreateDiagnostic("ENC_PROJECT", project: project);
        var openDocumentDiagnostic1 = CreateDocumentDiagnostic("ENC_OPEN_DOC1", openDocument);
        var openDocumentDiagnostic2 = await CreateDocumentDiagnostic("ENC_OPEN_DOC2", openDocument).ToDiagnosticAsync(project, CancellationToken.None);
        var closedDocumentDiagnostic = CreateDocumentDiagnostic("ENC_CLOSED_DOC", closedDocument);

        encSessionState.IsSessionActive = true;
        encSessionState.ApplyChangesDiagnostics = [projectDiagnostic, openDocumentDiagnostic1, closedDocumentDiagnostic];
        encService.GetDocumentDiagnosticsImpl = (_, _) => [openDocumentDiagnostic2];

        var documentResults1 = await RunGetDocumentPullDiagnosticsAsync(testLspServer, openDocument.GetURI(), useVSDiagnostics, category: PullDiagnosticCategories.EditAndContinue);

        var rootUri = ProtocolConversions.GetAbsoluteUriString(TestWorkspace.RootDirectory);

        // both diagnostics located in the open document are reported:
        AssertEx.Equal(
        [
            $"{rootUri}/test1.cs -> [ENC_OPEN_DOC1,ENC_OPEN_DOC2]",
        ], documentResults1.Select(Inspect));

        var workspaceResults1 = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics, includeTaskListItems: false, category: PullDiagnosticCategories.EditAndContinue);

        AssertEx.Equal(
        [
            $"{rootUri}/test2.cs -> [ENC_CLOSED_DOC]",
            $"{rootUri}/Test.csproj -> [ENC_PROJECT]",
        ], workspaceResults1.Select(Inspect));

        // clear workspace diagnostics:

        encSessionState.ApplyChangesDiagnostics = [];
        diagnosticsRefresher.RequestWorkspaceRefresh();

        var documentResults2 = await RunGetDocumentPullDiagnosticsAsync(
            testLspServer, openDocument.GetURI(), previousResultId: documentResults1.Single().ResultId, useVSDiagnostics: useVSDiagnostics, category: PullDiagnosticCategories.EditAndContinue);

        AssertEx.Equal(
        [
           $"{rootUri}/test1.cs -> [ENC_OPEN_DOC2]",
        ], documentResults2.Select(Inspect));

        var workspaceResults2 = await RunGetWorkspacePullDiagnosticsAsync(
            testLspServer, useVSDiagnostics, previousResults: CreateDiagnosticParamsFromPreviousReports(workspaceResults1), includeTaskListItems: false, category: PullDiagnosticCategories.EditAndContinue);
        AssertEx.Equal(
        [
            $"{rootUri}/test2.cs -> []",
            $"{rootUri}/Test.csproj -> []",
        ], workspaceResults2.Select(Inspect));

        // deactivate EnC session:

        encSessionState.IsSessionActive = false;
        diagnosticsRefresher.RequestWorkspaceRefresh();

        var documentResults3 = await RunGetDocumentPullDiagnosticsAsync(
            testLspServer, openDocument.GetURI(), previousResultId: documentResults2.Single().ResultId, useVSDiagnostics: useVSDiagnostics, category: PullDiagnosticCategories.EditAndContinue);
        AssertEx.Equal(
        [
           $"{rootUri}/test1.cs -> []",
        ], documentResults3.Select(Inspect));

        var workspaceResults3 = await RunGetWorkspacePullDiagnosticsAsync(
            testLspServer, useVSDiagnostics, previousResults: CreateDiagnosticParamsFromPreviousReports(workspaceResults2), includeTaskListItems: false, category: PullDiagnosticCategories.EditAndContinue);
        AssertEx.Equal([], workspaceResults3.Select(Inspect));

        static DiagnosticData CreateDocumentDiagnostic(string id, Document document)
            => CreateDiagnostic(id, document.Project, document);

        static DiagnosticData CreateDiagnostic(string id, Project project, Document? document = null)
            => new(
                id,
                category: "EditAndContinue",
                message: "test message",
                severity: DiagnosticSeverity.Error,
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                warningLevel: 0,
                projectId: project.Id,
                customTags: [],
                properties: ImmutableDictionary<string, string?>.Empty,
                location: new DiagnosticDataLocation(new FileLinePositionSpan("file", span: default), document?.Id),
                additionalLocations: [],
                language: project.Language);

        static string Inspect(TestDiagnosticResult result)
            => $"{result.TextDocument.DocumentUri} -> [{string.Join(",", result.Diagnostics?.Select(d => d.Code?.Value) ?? [])}]";
    }

    [Theory, CombinatorialData]
    public async Task TestNoWorkspaceDiagnosticsForClosedFilesWithFSAOffWithFileInProjectOpen(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup1 = @"class A {";
        var markup2 = "";
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
            [markup1, markup2], mutatingLspWorkspace, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics);

        var firstDocument = testLspServer.GetCurrentSolution().Projects.Single().Documents.First();
        await OpenDocumentAsync(testLspServer, firstDocument);

        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);

        Assert.Empty(results);
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceDiagnosticsIncludesSourceGeneratorDiagnosticsClosedFSAOn(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup = "// Hello, World";
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
            markup, mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics);

        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();

        var generator = new DiagnosticProducingGenerator(context => Location.Create(context.Compilation.SyntaxTrees.Single(), new TextSpan(0, 10)));

        testLspServer.TestWorkspace.OnAnalyzerReferenceAdded(
            document.Project.Id,
            new TestGeneratorReference(generator));

        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);

        Assert.Equal(DiagnosticProducingGenerator.Descriptor.Id, results[0].Diagnostics!.Single().Code);
        AssertEx.Empty(results[1].Diagnostics);
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceDiagnosticsDoesNotIncludeSourceGeneratorDiagnosticsClosedFSAOffAndNoFilesOpen(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup = "// Hello, World";
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
            markup, mutatingLspWorkspace, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics);

        var generator = new DiagnosticProducingGenerator(
            context => Location.Create(
                context.Compilation.SyntaxTrees.Single(),
                new TextSpan(0, 10)));

        testLspServer.TestWorkspace.OnAnalyzerReferenceAdded(
            testLspServer.GetCurrentSolution().Projects.Single().Id,
            new TestGeneratorReference(generator));

        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);
        Assert.Empty(results);
    }

    [Theory, CombinatorialData]
    public async Task TestNoWorkspaceDiagnosticsForClosedFilesInProjectsWithIncorrectLanguage(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var csharpMarkup = @"class A {";
        var typeScriptMarkup = "???";

        var workspaceXml =
            $"""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj1" FilePath="C:\CSProj1.csproj">
                    <Document FilePath="C:\C.cs">{csharpMarkup}</Document>
                </Project>
                <Project Language="TypeScript" CommonReferences="true" AssemblyName="TypeScriptProj" FilePath="C:\TypeScriptProj.csproj">
                    <Document FilePath="C:\T.ts">{typeScriptMarkup}</Document>
                </Project>
            </Workspace>
            """;

        await using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics).ConfigureAwait(false);

        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);

        Assert.False(results.Any(r => r.TextDocument!.DocumentUri.GetRequiredParsedUri().LocalPath.Contains(".ts")));
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceDiagnosticsForSourceGeneratedFiles(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup1 = @"class A {";
        var markup2 = "";
        await using var testLspServer = await CreateTestLspServerAsync(
            markups: [], mutatingLspWorkspace,
            GetInitializationOptions(BackgroundAnalysisScope.FullSolution, CompilerDiagnosticsScope.FullSolution, useVSDiagnostics, sourceGeneratedMarkups: [markup1, markup2]));

        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);

        // Project.GetSourceGeneratedDocumentsAsync may not return documents in a deterministic order, so we sort
        // the results here to ensure subsequent assertions are not dependent on the order of items provided by the
        // project.
        results = results.Sort((x, y) => x.Uri.ToString().CompareTo(y.Uri.ToString()));

        Assert.Equal(3, results.Length);
        // Since we sorted above by URI the first result is the project.
        AssertEx.Empty(results[0].Diagnostics);
        Assert.Equal("CS1513", results[1].Diagnostics!.Single().Code);
        AssertEx.Empty(results[2].Diagnostics);
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceDiagnosticsForRemovedDocument(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup1 = @"class A {";
        var markup2 = "";
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
            [markup1, markup2], mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics);

        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);

        Assert.Equal(3, results.Length);
        Assert.Equal("CS1513", results[0].Diagnostics!.Single().Code);
        AssertEx.Empty(results[1].Diagnostics);
        AssertEx.Empty(results[2].Diagnostics);

        testLspServer.TestWorkspace.OnDocumentRemoved(testLspServer.TestWorkspace.Documents.First().Id);

        var results2 = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics, previousResults: CreateDiagnosticParamsFromPreviousReports(results));

        // First doc should show up as removed.
        Assert.Equal(1, results2.Length);
        // VS represents removal with null diagnostics, VS code represents with an empty diagnostics array.
        Assert.Equal(useVSDiagnostics ? null : [], results2[0].Diagnostics);
        Assert.Null(results2[0].ResultId);
    }

    [Theory, CombinatorialData]
    public async Task TestNoChangeIfWorkspaceDiagnosticsCalledTwice(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup1 = @"class A {";
        var markup2 = "";
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
             [markup1, markup2], mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics);

        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);

        Assert.Equal(3, results.Length);
        Assert.Equal("CS1513", results[0].Diagnostics!.Single().Code);
        AssertEx.Empty(results[1].Diagnostics);
        AssertEx.Empty(results[2].Diagnostics);

        var results2 = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics, previousResults: CreateDiagnosticParamsFromPreviousReports(results));

        // 'no changes' will be reported as an empty array.
        Assert.Empty(results2);
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceDiagnosticsRemovedAfterErrorIsFixed(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup1 = @"class A {";
        var markup2 = "";
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
             [markup1, markup2], mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics);

        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);

        Assert.Equal(3, results.Length);
        Assert.Equal("CS1513", results[0].Diagnostics!.Single().Code);
        AssertEx.Empty(results[1].Diagnostics);
        AssertEx.Empty(results[2].Diagnostics);

        await InsertInClosedDocumentAsync(testLspServer, testLspServer.TestWorkspace.Documents.First().Id, "}");

        var results2 = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics, previousResults: CreateDiagnosticParamsFromPreviousReports(results));

        // Only 1 file has changed diagnostics, we should get 1 report with an empty set.
        // Reports for the other files are not sent (they are unchanged and we skip reporting unchanged docs in workspace diags).
        Assert.Equal(1, results2.Length);
        AssertEx.Empty(results2[0].Diagnostics);
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceDiagnosticsRemainAfterErrorIsNotFixed(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup1 = @"class A {";
        var markup2 = "";
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
             [markup1, markup2], mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics);

        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);

        Assert.Equal(3, results.Length);
        Assert.Equal("CS1513", results[0].Diagnostics!.Single().Code);
        Assert.Equal(new Position { Line = 0, Character = 9 }, results[0].Diagnostics!.Single().Range.Start);

        AssertEx.Empty(results[1].Diagnostics);
        AssertEx.Empty(results[2].Diagnostics);

        await InsertInClosedDocumentAsync(testLspServer, testLspServer.TestWorkspace.Documents.First().Id, " ", position: 0);

        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.First();
        var text = await document.GetTextAsync();

        var results2 = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);

        Assert.Equal("CS1513", results2[0].Diagnostics!.Single().Code);
        Assert.Equal(new Position { Line = 0, Character = 10 }, results2[0].Diagnostics!.Single().Range.Start);

        AssertEx.Empty(results2[1].Diagnostics);
        Assert.NotEqual(results[1].ResultId, results2[1].ResultId);
        AssertEx.Empty(results2[2].Diagnostics);
        Assert.NotEqual(results[2].ResultId, results2[2].ResultId);
    }

    [Theory, CombinatorialData]
    public async Task TestStreamingWorkspaceDiagnostics(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup1 = @"class A {";
        var markup2 = "";
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
             [markup1, markup2], mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics);

        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);

        Assert.Equal(3, results.Length);
        Assert.Equal("CS1513", results[0].Diagnostics!.Single().Code);
        Assert.Equal(new Position { Line = 0, Character = 9 }, results[0].Diagnostics!.Single().Range.Start);

        results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics, useProgress: true);

        Assert.Equal("CS1513", results[0].Diagnostics![0].Code);
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceDiagnosticsAreNotMapped(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup1 =
            """
            #line 1 "test.txt"
            class A {
            """;
        var markup2 = "";
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
            [markup1, markup2], mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics);

        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);
        Assert.Equal(3, results.Length);
        Assert.Equal(ProtocolConversions.CreateAbsoluteDocumentUri(Path.Combine(TestWorkspace.RootDirectory, "test1.cs")), results[0].TextDocument!.DocumentUri);
        Assert.Equal("CS1513", results[0].Diagnostics!.Single().Code);
        Assert.Equal(1, results[0].Diagnostics!.Single().Range.Start.Line);
        AssertEx.Empty(results[1].Diagnostics);
        AssertEx.Empty(results[2].Diagnostics);
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceDiagnosticsWithChangeInReferencedProject(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup1 =
            """
            namespace M
            {
                class A : B { }
            }
            """;
        var markup2 =
            """
            namespace M
            {
                public class {|caret:|} { }
            }
            """;

        var workspaceXml =
            $"""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj1" FilePath="C:\CSProj1.csproj">
                    <Document FilePath="C:\A.cs">{markup1}</Document>
                    <ProjectReference>CSProj2</ProjectReference>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj2" FilePath="C:\CSProj2.csproj">
                    <Document FilePath="C:\B.cs">{markup2}</Document>
                </Project>
            </Workspace>
            """;

        await using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics).ConfigureAwait(false);
        var csproj2Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj2").Single().Documents.First();

        // Verify we a diagnostic in A.cs since B does not exist
        // and a diagnostic in B.cs since it is missing the class name.
        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);
        AssertEx.NotNull(results);
        Assert.Equal(4, results.Length);
        Assert.Equal("CS0246", results[0].Diagnostics!.Single().Code);
        Assert.Equal("CS1001", results[2].Diagnostics!.Single().Code);

        // Insert B into B.cs via the workspace.
        var caretLocation = testLspServer.GetLocations("caret").First().Range;
        var csproj2DocumentText = await csproj2Document.GetTextAsync();
        var newCsProj2Document = csproj2Document.WithText(csproj2DocumentText.WithChanges(new TextChange(ProtocolConversions.RangeToTextSpan(caretLocation, csproj2DocumentText), "B")));
        await testLspServer.TestWorkspace.ChangeDocumentAsync(csproj2Document.Id, newCsProj2Document.Project.Solution);

        // Get updated workspace diagnostics for the change.
        var previousResultIds = CreateDiagnosticParamsFromPreviousReports(results);
        results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics, previousResults: previousResultIds);
        AssertEx.NotNull(results);

        // We should get updated diagnostics for both A and B now that B exists.
        Assert.Equal(2, results.Length);

        // Verify diagnostics for A.cs are updated as the type B now exists.
        AssertEx.Empty(results[0].Diagnostics);
        Assert.NotEqual(previousResultIds[0].resultId, results[0].ResultId);

        // Verify diagnostics for B.cs are updated as the class definition is now correct.
        AssertEx.Empty(results[1].Diagnostics);
        Assert.NotEqual(previousResultIds[2].resultId, results[1].ResultId);
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceDiagnosticsWithChangeInRecursiveReferencedProject(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup1 =
            """
            namespace M
            {
                public class A : B
                {
                }
            }
            """;
        var markup2 =
            """
            namespace M
            {
                public class B : C
                {
                }
            }
            """;
        var markup3 =
            """
            namespace M
            {
                public class {|caret:|}
                {
                }
            }
            """;

        var workspaceXml =
            $"""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj1" FilePath="C:\CSProj1.csproj">
                    <ProjectReference>CSProj2</ProjectReference>
                    <Document FilePath="C:\A.cs">{markup1}</Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj2" FilePath="C:\CSProj2.csproj">
                    <ProjectReference>CSProj3</ProjectReference>
                    <Document FilePath="C:\B.cs">{markup2}</Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj3" FilePath="C:\CSProj3.csproj">
                    <Document FilePath="C:\C.cs">{markup3}</Document>
                </Project>
            </Workspace>
            """;

        await using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics).ConfigureAwait(false);
        var csproj3Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj3").Single().Documents.First();

        // Verify we have diagnostics in A.cs, B.cs, and C.cs initially.
        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);
        AssertEx.NotNull(results);
        Assert.Equal(6, results.Length);
        // Type C does not exist.
        Assert.Equal("CS0246", results[0].Diagnostics!.Single().Code);
        AssertEx.Empty(results[1].Diagnostics);
        // Type C does not exist.
        Assert.Equal("CS0246", results[2].Diagnostics!.Single().Code);
        AssertEx.Empty(results[3].Diagnostics);
        // Syntax error missing identifier.
        Assert.Equal("CS1001", results[4].Diagnostics!.Single().Code);
        AssertEx.Empty(results[5].Diagnostics);

        // Insert C into C.cs via the workspace.
        var caretLocation = testLspServer.GetLocations("caret").First().Range;
        var csproj3DocumentText = await csproj3Document.GetTextAsync().ConfigureAwait(false);
        var newCsProj3Document = csproj3Document.WithText(csproj3DocumentText.WithChanges(new TextChange(ProtocolConversions.RangeToTextSpan(caretLocation, csproj3DocumentText), "C")));
        await testLspServer.TestWorkspace.ChangeDocumentAsync(csproj3Document.Id, newCsProj3Document.Project.Solution).ConfigureAwait(false);

        // Get updated workspace diagnostics for the change.
        var previousResultIds = CreateDiagnosticParamsFromPreviousReports(results);
        results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics, previousResults: previousResultIds).ConfigureAwait(false);
        AssertEx.NotNull(results);

        // Verify that we get 3 new reports as the diagnostics in A.cs, B.cs, and C.cs have all changed due to the transitive change in C.cs.
        Assert.Equal(3, results.Length);

        // A.cs should report CS0012 indicating that C is not directly referenced.
        Assert.Equal("CS0012", results[0].Diagnostics!.Single().Code);
        Assert.NotEqual(previousResultIds[0].resultId, results[0].ResultId);

        // B.cs should no longer have a diagnostic since C exists.
        AssertEx.Empty(results[1].Diagnostics);
        Assert.NotEqual(previousResultIds[2].resultId, results[1].ResultId);

        // C.cs should no longer have a diagnostic since C was fully defined.
        AssertEx.Empty(results[2].Diagnostics);
        Assert.NotEqual(previousResultIds[4].resultId, results[2].ResultId);
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceDiagnosticsWithChangeInNotReferencedProject(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup1 =
            """
            namespace M
            {
                class A : B { }
            }
            """;
        var markup2 =
            """
            namespace M
            {
                public class {|caret:|} { }
            }
            """;

        var workspaceXml =
            $"""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj1" FilePath="C:\CSProj1.csproj">
                    <Document FilePath="C:\A.cs">{markup1}</Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj2" FilePath="C:\CSProj2.csproj">
                    <Document FilePath="C:\B.cs">{markup2}</Document>
                </Project>
            </Workspace>
            """;

        await using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics).ConfigureAwait(false);
        var csproj2Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj2").Single().Documents.First();

        // Verify we a diagnostic in A.cs since B does not exist
        // and a diagnostic in B.cs since it is missing the class name.
        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);
        AssertEx.NotNull(results);
        Assert.Equal(4, results.Length);
        Assert.Equal("CS0246", results[0].Diagnostics!.Single().Code);
        AssertEx.Empty(results[1].Diagnostics);
        Assert.Equal("CS1001", results[2].Diagnostics!.Single().Code);
        AssertEx.Empty(results[3].Diagnostics);

        // Insert B into B.cs via the workspace.
        var caretLocation = testLspServer.GetLocations("caret").First().Range;
        var csproj2DocumentText = await csproj2Document.GetTextAsync();
        var newCsProj2Document = csproj2Document.WithText(csproj2DocumentText.WithChanges(new TextChange(ProtocolConversions.RangeToTextSpan(caretLocation, csproj2DocumentText), "B")));
        await testLspServer.TestWorkspace.ChangeDocumentAsync(csproj2Document.Id, newCsProj2Document.Project.Solution);

        // Get updated workspace diagnostics for the change.
        var previousResultIds = CreateDiagnosticParamsFromPreviousReports(results);
        results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics, previousResultIds);
        AssertEx.NotNull(results);

        // We should get 1 report from B.cs reflecting the change we made to it.
        // A.cs is unchanged and we will not get a report for it.
        Assert.Equal(1, results.Length);
        AssertEx.Empty(results[0].Diagnostics);
        Assert.NotEqual(previousResultIds[2].resultId, results[0].ResultId);
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceDiagnosticsWithDependentProjectReloadedAndChanged(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup1 =
            """
            namespace M
            {
                class A : B { }
            }
            """;
        var markup2 =
            """
            namespace M
            {
                public class B
                {
                    public static void Do()
                    {
                        int unusedVariable;
                    }
                }
            }
            """;

        var workspaceXml =
            $"""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj1" FilePath="C:\CSProj1.csproj">
                    <Document FilePath="C:\A.cs">{markup1}</Document>
                    <ProjectReference>CSProj2</ProjectReference>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj2" FilePath="C:\CSProj2.csproj">
                    <Document FilePath="C:\B.cs">{markup2}</Document>
                </Project>
            </Workspace>
            """;

        await using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics).ConfigureAwait(false);
        var csproj2Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj2").Single().Documents.First();

        // Verify we a diagnostic in A.cs since B does not exist
        // and a diagnostic in B.cs since it is missing the class name.
        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);
        AssertEx.NotNull(results);
        Assert.Equal(4, results.Length);
        AssertEx.Empty(results[0].Diagnostics);
        Assert.Equal("CS0168", results[2].Diagnostics!.Single().Code);
        Assert.Equal(LSP.DiagnosticSeverity.Warning, results[2].Diagnostics!.Single().Severity);

        // Change and reload the project via the workspace.
        var projectInfo = testLspServer.TestWorkspace.Projects.Where(p => p.AssemblyName == "CSProj2").Single().ToProjectInfo();
        projectInfo = projectInfo.WithCompilationOptions(projectInfo.CompilationOptions!.WithGeneralDiagnosticOption(ReportDiagnostic.Error));
        testLspServer.TestWorkspace.OnProjectReloaded(projectInfo);
        var operations = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>();
        await operations.GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();

        // Get updated workspace diagnostics for the change.
        var previousResultIds = CreateDiagnosticParamsFromPreviousReports(results);
        results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics, previousResults: previousResultIds);

        AssertEx.NotNull(results);

        // We should get a single report back for B.cs now that the diagnostic has been promoted to an error.
        // The diagnostics in A.cs did not change and so are not reported again.
        Assert.Equal(1, results.Length);
        Assert.Equal("CS0168", results[0].Diagnostics!.Single().Code);
        Assert.Equal(LSP.DiagnosticSeverity.Error, results[0].Diagnostics!.Single().Severity);
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceDiagnosticsWithDependentProjectReloadedUnchanged(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup1 =
            """
            namespace M
            {
                class A : B { }
            }
            """;
        var markup2 =
            """
            namespace M
            {
                public class {|caret:|} { }
            }
            """;

        var workspaceXml =
            $"""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj1" FilePath="C:\CSProj1.csproj">
                    <Document FilePath="C:\A.cs">{markup1}</Document>
                    <ProjectReference>CSProj2</ProjectReference>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj2" FilePath="C:\CSProj2.csproj">
                    <Document FilePath="C:\B.cs">{markup2}</Document>
                </Project>
            </Workspace>
            """;

        await using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics).ConfigureAwait(false);
        var csproj2Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj2").Single().Documents.First();

        // Verify we a diagnostic in A.cs since B does not exist
        // and a diagnostic in B.cs since it is missing the class name.
        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);
        AssertEx.NotNull(results);
        Assert.Equal(4, results.Length);
        Assert.Equal("CS0246", results[0].Diagnostics!.Single().Code);
        Assert.Equal("CS1001", results[2].Diagnostics!.Single().Code);

        // Reload the project via the workspace.
        var projectInfo = testLspServer.TestWorkspace.Projects.Where(p => p.AssemblyName == "CSProj2").Single().ToProjectInfo();
        testLspServer.TestWorkspace.OnProjectReloaded(projectInfo);
        var operations = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>();
        await operations.GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();

        // Get updated workspace diagnostics for the change.
        var previousResultIds = CreateDiagnosticParamsFromPreviousReports(results);
        results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics, previousResults: previousResultIds);

        // Verify that since no actual changes have been made we report unchanged diagnostics.
        // We get an empty array here as this is workspace diagnostics, and we do not report unchanged
        // docs there for efficiency.
        AssertEx.NotNull(results);
        Assert.Empty(results);
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceDiagnosticsOrderOfReferencedProjectsReloadedDoesNotMatter(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup1 =
            """
            namespace M
            {
                class A : B { }
            }
            """;

        var workspaceXml =
            $"""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj1" FilePath="C:\CSProj1.csproj">
                    <Document FilePath="C:\A.cs">{markup1}</Document>
                    <ProjectReference>CSProj2</ProjectReference>
                    <ProjectReference>CSProj3</ProjectReference>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj2" FilePath="C:\CSProj2.csproj">
                    <Document FilePath="C:\B.cs"></Document>
                </Project>
            <Project Language="C#" CommonReferences="true" AssemblyName="CSProj3" FilePath="C:\CSProj3.csproj">
                    <Document FilePath="C:\C.cs"></Document>
                </Project>
            </Workspace>
            """;

        await using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics).ConfigureAwait(false);
        var csproj2Document = testLspServer.GetCurrentSolution().Projects.Where(p => p.Name == "CSProj2").Single().Documents.First();

        // Verify we a diagnostic in A.cs since B does not exist
        // and a diagnostic in B.cs since it is missing the class name.
        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);
        AssertEx.NotNull(results);
        Assert.Equal(6, results.Length);
        Assert.Equal("CS0246", results[0].Diagnostics!.Single().Code);

        // Reload the project via the workspace.
        var projectInfo = testLspServer.TestWorkspace.Projects.Where(p => p.AssemblyName == "CSProj2").Single().ToProjectInfo();
        testLspServer.TestWorkspace.OnProjectReloaded(projectInfo);
        var operations = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>();
        await operations.GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();

        // Get updated workspace diagnostics for the change.
        var previousResults = CreateDiagnosticParamsFromPreviousReports(results);
        var previousResultIds = previousResults.SelectAsArray(param => param.resultId);
        results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics, previousResults: previousResults);

        // Verify that since no actual changes have been made we report unchanged diagnostics.
        // We get an empty array here as this is workspace diagnostics, and we do not report unchanged
        // docs there for efficiency.
        AssertEx.NotNull(results);
        Assert.Empty(results);
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceDiagnosticsDoesNotThrowIfProjectWithoutFilePathExists(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var csharpMarkup = @"class A {";
        var workspaceXml =
            $"""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj1" FilePath="CSProj1.csproj">
                    <Document FilePath="C.cs">{csharpMarkup}</Document>
                </Project>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj2" FilePath="{TestWorkspace.NullFilePath}">
                    <Document FilePath="C2.cs"></Document>
                </Project>
            </Workspace>
            """;

        await using var testLspServer = await CreateTestWorkspaceFromXmlAsync(workspaceXml, mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics).ConfigureAwait(false);

        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);

        var dir = TestWorkspace.RootDirectory.Replace("\\", "/");

        AssertEx.SequenceEqual(
        [
            $"{dir}/C.cs",
            $"{dir}/CSProj1.csproj",
            $"{dir}/C2.cs"
        ], results.Select(r => r.TextDocument.DocumentUri.GetRequiredParsedUri().AbsolutePath));
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceDiagnosticsWaitsForLspTextChanges(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup1 = @"class A {";
        var markup2 = "";
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
            [markup1, markup2], mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics);

        // The very first request should return immediately (as we're have no prior state to tell if the sln changed).
        var resultTask = RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics, useProgress: true, triggerConnectionClose: false);
        await resultTask;

        // The second request should wait for a solution change before returning.
        resultTask = RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics, useProgress: true, triggerConnectionClose: false);

        // Assert that the connection isn't closed and task doesn't complete even after some delay.
        await Task.Delay(TimeSpan.FromSeconds(5));
        Assert.False(resultTask.IsCompleted);

        // Make an LSP document change that will trigger connection close.
        var uri = testLspServer.GetCurrentSolution().Projects.First().Documents.First().GetURI();
        await testLspServer.OpenDocumentAsync(uri);

        // Assert the task completes after a change occurs
        var results = await resultTask.WithTimeout(TestHelpers.HangMitigatingTimeout);
        Assert.NotEmpty(results);
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceDiagnosticsWaitsForLspSolutionChanges(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup1 = @"class A {";
        var markup2 = "";
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
            [markup1, markup2], mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics);

        // The very first request should return immediately (as we're have no prior state to tell if the sln changed).
        var resultTask = RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics, useProgress: true, triggerConnectionClose: false);
        await resultTask;

        // The second request should wait for a solution change before returning.
        resultTask = RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics, useProgress: true, triggerConnectionClose: false);

        // Assert that the connection isn't closed and task doesn't complete even after some delay.
        await Task.Delay(TimeSpan.FromSeconds(5));
        Assert.False(resultTask.IsCompleted);

        // Make workspace change that will trigger connection close.
        var projectInfo = testLspServer.TestWorkspace.Projects.Single().ToProjectInfo();
        testLspServer.TestWorkspace.OnProjectReloaded(projectInfo);

        // Assert the task completes after a change occurs
        var results = await resultTask.WithTimeout(TestHelpers.HangMitigatingTimeout);
        Assert.NotEmpty(results);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/77495")]
    public async Task TestWorkspaceDiagnosticsWaitsForRefreshRequestedEvent(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup1 = @"class A {";
        var markup2 = "";
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
            [markup1, markup2], mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics);

        // The very first request should return immediately (as we're have no prior state to tell if the sln changed).
        var resultTask = RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics, useProgress: true, triggerConnectionClose: false);
        await resultTask;

        // The second request should wait for a solution change before returning.
        resultTask = RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics, useProgress: true, triggerConnectionClose: false);

        // Assert that the connection isn't closed and task doesn't complete even after some delay.
        await Task.Delay(TimeSpan.FromSeconds(5));
        Assert.False(resultTask.IsCompleted);

        // Make workspace change that will trigger connection close.
        var refreshService = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<IDiagnosticsRefresher>();
        refreshService.RequestWorkspaceRefresh();

        // Assert the task completes after a change occurs
        var results = await resultTask.WithTimeout(TestHelpers.HangMitigatingTimeout);
        Assert.NotEmpty(results);
    }

    [Theory(Skip = "https://github.com/dotnet/roslyn/issues/76503"), CombinatorialData]
    public async Task TestWorkspaceDiagnosticsWaitsForLspTextChangesWithMultipleSources(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup1 = @"class A {";
        var markup2 = "";
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
            [markup1, markup2], mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics);

        var resultTaskOne = RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics, useProgress: true, category: PullDiagnosticCategories.WorkspaceDocumentsAndProject, triggerConnectionClose: false);
        var resultTaskTwo = RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics, useProgress: true, category: PullDiagnosticCategories.EditAndContinue, triggerConnectionClose: false);
        // The very first requests should return immediately (as we're have no prior state to tell if the sln changed).
        await Task.WhenAll(resultTaskOne, resultTaskTwo);

        // The second request for each source should wait for a solution change before returning.
        resultTaskOne = RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics, useProgress: true, category: PullDiagnosticCategories.WorkspaceDocumentsAndProject, triggerConnectionClose: false);
        resultTaskTwo = RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics, useProgress: true, category: PullDiagnosticCategories.EditAndContinue, triggerConnectionClose: false);

        // Assert that the connection isn't closed and task doesn't complete even after some delay.
        await Task.Delay(TimeSpan.FromSeconds(5));
        Assert.False(resultTaskOne.IsCompleted);
        Assert.False(resultTaskTwo.IsCompleted);

        // Make an LSP document change that will trigger connection close.
        var uri = testLspServer.GetCurrentSolution().Projects.First().Documents.First().GetURI();
        await testLspServer.OpenDocumentAsync(uri);

        // Assert that both tasks completes after a change occurs
        var resultsOne = await resultTaskOne;
        var resultsTwo = await resultTaskTwo;
        Assert.NotEmpty(resultsOne);
        Assert.Empty(resultsTwo);

        // For the mutating workspace, the change in the document can cause to change events
        //    1.  LSP changed, which triggers immediately via the queue.
        //    2.  Workspace changed, which can be delayed until after the requests complete.
        // To ensure the workspace changed is processed, we need to wait for all workspace events.
        var listenerProvider = testLspServer.TestWorkspace.GetService<IAsynchronousOperationListenerProvider>();
        await listenerProvider.WaitAllDispatcherOperationAndTasksAsync(testLspServer.TestWorkspace);

        // Make new requests - these requests should again wait for new changes.
        resultTaskOne = RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics, useProgress: true, category: PullDiagnosticCategories.WorkspaceDocumentsAndProject, triggerConnectionClose: false);
        resultTaskTwo = RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics, useProgress: true, category: PullDiagnosticCategories.EditAndContinue, triggerConnectionClose: false);

        // Assert that the new requests correctly wait for new changes and do not complete even after some delay.
        await Task.Delay(TimeSpan.FromSeconds(5));
        Assert.False(resultTaskOne.IsCompleted);
        Assert.False(resultTaskTwo.IsCompleted);

        // Make an LSP document change that will trigger connection close.
        await testLspServer.CloseDocumentAsync(uri);

        // Assert that both tasks again complete after a change occurs
        resultsOne = await resultTaskOne;
        resultsTwo = await resultTaskTwo;
        Assert.NotEmpty(resultsOne);
        Assert.Empty(resultsTwo);
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceDiagnosticsForClosedFilesSwitchFSAFromOnToOff(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup1 = @"class A {";
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
            [markup1], mutatingLspWorkspace, BackgroundAnalysisScope.FullSolution, useVSDiagnostics);

        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);

        Assert.Equal(2, results.Length);
        Assert.Equal("CS1513", results[0].Diagnostics!.Single().Code);
        AssertEx.Empty(results[1].Diagnostics);

        var options = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<IGlobalOptionService>();
        options.SetGlobalOption(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.CSharp, BackgroundAnalysisScope.OpenFiles);
        options.SetGlobalOption(SolutionCrawlerOptionsStorage.CompilerDiagnosticsScopeOption, LanguageNames.CSharp, CompilerDiagnosticsScope.OpenFiles);

        results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics, previousResults: CreateDiagnosticParamsFromPreviousReports(results));

        Assert.Equal(2, results.Length);

        Assert.Null(results[0].ResultId);
        Assert.Null(results[1].ResultId);

        // VS represents removal with null diagnostics, VS code represents with an empty diagnostics array.
        if (useVSDiagnostics)
        {
            Assert.Null(results[0].Diagnostics);
            Assert.Null(results[1].Diagnostics);
        }
        else
        {
            AssertEx.Empty(results[0].Diagnostics);
            AssertEx.Empty(results[1].Diagnostics);
        }
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceDiagnosticsForClosedFilesSwitchFSAFromOffToOn(bool useVSDiagnostics, bool mutatingLspWorkspace)
    {
        var markup1 = @"class A {";
        await using var testLspServer = await CreateTestWorkspaceWithDiagnosticsAsync(
            [markup1], mutatingLspWorkspace, BackgroundAnalysisScope.OpenFiles, useVSDiagnostics);

        var results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics);

        Assert.Empty(results);

        var options = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<IGlobalOptionService>();
        options.SetGlobalOption(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.CSharp, BackgroundAnalysisScope.FullSolution);
        options.SetGlobalOption(SolutionCrawlerOptionsStorage.CompilerDiagnosticsScopeOption, LanguageNames.CSharp, CompilerDiagnosticsScope.FullSolution);

        results = await RunGetWorkspacePullDiagnosticsAsync(testLspServer, useVSDiagnostics, previousResults: CreateDiagnosticParamsFromPreviousReports(results));

        Assert.Equal(2, results.Length);
        Assert.Equal("CS1513", results[0].Diagnostics!.Single().Code);
        AssertEx.Empty(results[1].Diagnostics);
    }

    internal static async Task InsertInClosedDocumentAsync(TestLspServer testLspServer, DocumentId documentId, string textToInsert, int? position = null)
    {
        var text = await testLspServer.GetCurrentSolution().GetDocument(documentId)!.GetTextAsync(CancellationToken.None);
        position ??= text.Length;
        text = text.WithChanges(new TextChange(new TextSpan(position.Value, 0), textToInsert));
        await testLspServer.TestWorkspace.ChangeDocumentAsync(documentId, text);
    }

    #endregion
}
