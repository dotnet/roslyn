// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Rename;

public sealed class RenameTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    protected override TestComposition Composition => base.Composition.AddParts(typeof(TestSourceGeneratedDocumentSpanMappingService));

    [Theory, CombinatorialData]
    public async Task TestRenameAsync(bool mutatingLspWorkspace)
    {
        await using var testLspServer = await CreateTestLspServerAsync("""
            class A
            {
                void {|caret:|}{|renamed:M|}()
                {
                }
                void M2()
                {
                    {|renamed:M|}()
                }
            }
            """, mutatingLspWorkspace);
        var renameLocation = testLspServer.GetLocations("caret").First();
        var renameValue = "RENAME";
        var expectedEdits = testLspServer.GetLocations("renamed").Select(location => new LSP.TextEdit() { NewText = renameValue, Range = location.Range });

        var results = await RunRenameAsync(testLspServer, CreateRenameParams(renameLocation, renameValue));
        AssertJsonEquals(expectedEdits, ((TextDocumentEdit[])results.DocumentChanges).First().Edits);
    }

    [Theory, CombinatorialData]
    public async Task TestRename_InvalidIdentifierAsync(bool mutatingLspWorkspace)
    {
        await using var testLspServer = await CreateTestLspServerAsync("""
            class A
            {
                void {|caret:|}{|renamed:M|}()
                {
                }
                void M2()
                {
                    {|renamed:M|}()
                }
            }
            """, mutatingLspWorkspace);
        var renameLocation = testLspServer.GetLocations("caret").First();
        var renameValue = "$RENAMED$";

        var results = await RunRenameAsync(testLspServer, CreateRenameParams(renameLocation, renameValue));
        Assert.Null(results);
    }

    [Theory, CombinatorialData]
    public async Task TestRename_WithLinkedFilesAsync(bool mutatingLspWorkspace)
    {
        var markup = """
            class A
            {
                void {|caret:|}{|renamed:M|}()
                {
                }
                void M2()
                {
                    {|renamed:M|}()
                }
            }
            """;
        await using var testLspServer = await CreateXmlTestLspServerAsync($"""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj" PreprocessorSymbols="Proj1">
                    <Document FilePath = "C:\C.cs"><![CDATA[{markup}]]></Document>
                </Project>
                <Project Language = "C#" CommonReferences="true" PreprocessorSymbols="Proj2">
                    <Document IsLinkFile = "true" LinkAssemblyName="CSProj" LinkFilePath="C:\C.cs"/>
                </Project>
            </Workspace>
            """, mutatingLspWorkspace);
        var renameLocation = testLspServer.GetLocations("caret").First();
        var renameValue = "RENAME";
        var expectedEdits = testLspServer.GetLocations("renamed").Select(location => new LSP.TextEdit() { NewText = renameValue, Range = location.Range });

        var results = await RunRenameAsync(testLspServer, CreateRenameParams(renameLocation, renameValue));
        AssertJsonEquals(expectedEdits, ((TextDocumentEdit[])results.DocumentChanges).First().Edits);
    }

    [Theory, CombinatorialData]
    public async Task TestRename_WithLinkedFilesAndPreprocessorAsync(bool mutatingLspWorkspace)
    {
        var markup = """
            class A
            {
                void {|caret:|}{|renamed:M|}()
                {
                }
                void M2()
                {
                    {|renamed:M|}()
                }
                void M3()
                {
            #if Proj1
                    {|renamed:M|}()
            #endif
                }
                void M4()
                {
            #if Proj2
                    {|renamed:M|}()
            #endif
                }
            }
            """;
        await using var testLspServer = await CreateXmlTestLspServerAsync($"""
            <Workspace>
                <Project Language="C#" CommonReferences="true" AssemblyName="CSProj" PreprocessorSymbols="Proj1">
                    <Document FilePath = "C:\C.cs"><![CDATA[{markup}]]></Document>
                </Project>
                <Project Language = "C#" CommonReferences="true" PreprocessorSymbols="Proj2">
                    <Document IsLinkFile = "true" LinkAssemblyName="CSProj" LinkFilePath="C:\C.cs"/>
                </Project>
            </Workspace>
            """, mutatingLspWorkspace);
        var renameLocation = testLspServer.GetLocations("caret").First();
        var renameValue = "RENAME";
        var expectedEdits = testLspServer.GetLocations("renamed").Select(location => new LSP.TextEdit() { NewText = renameValue, Range = location.Range });

        var results = await RunRenameAsync(testLspServer, CreateRenameParams(renameLocation, renameValue));
        AssertJsonEquals(expectedEdits, ((TextDocumentEdit[])results.DocumentChanges).First().Edits);
    }

    [Theory, CombinatorialData]
    public async Task TestRename_WithMappedFileAsync(bool mutatingLspWorkspace)
    {
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace);

        AddMappedDocument(testLspServer.TestWorkspace, """
            class A
            {
                void M()
                {
                }
                void M2()
                {
                    M()
                }
            }
            """);

        var startPosition = new LSP.Position { Line = 2, Character = 9 };
        var endPosition = new LSP.Position { Line = 2, Character = 10 };
        var renameText = "RENAME";
        var renameParams = CreateRenameParams(new LSP.Location
        {
            DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri($"C:\\{TestSpanMapper.GeneratedFileName}"),
            Range = new LSP.Range { Start = startPosition, End = endPosition }
        }, "RENAME");

        var results = await RunRenameAsync(testLspServer, renameParams);

        // There are two rename locations, so we expect two mapped locations.
        var expectedMappedRanges = ImmutableArray.Create(TestSpanMapper.MappedFileLocation.Range, TestSpanMapper.MappedFileLocation.Range);
        var expectedMappedDocument = TestSpanMapper.MappedFileLocation.DocumentUri;

        var documentEdit = results.DocumentChanges.Value.First.Single();
        Assert.Equal(expectedMappedDocument, documentEdit.TextDocument.DocumentUri);
        Assert.Equal(expectedMappedRanges, documentEdit.Edits.Select(edit => edit.Unify().Range));
        Assert.True(documentEdit.Edits.All(edit => edit.Unify().NewText == renameText));
    }

    [Theory, CombinatorialData]
    public async Task TestRename_WithSourceGeneratedFile(bool mutatingLspWorkspace)
    {
        var generatedMarkup = """
            class B
            {
                void M()
                {
                    new A().M();

                    var a = new A();
                    a.M();
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync("""
            public class A
            {
                public void {|caret:|}{|renamed:M|}()
                {
                }

                void M2()
                {
                    {|renamed:M|}()
                }
            }
            """, mutatingLspWorkspace,
            new InitializationOptions()
            {
                SourceGeneratedMarkups = [generatedMarkup]
            });

        var renameLocation = testLspServer.GetLocations("caret").First();
        var renameValue = "RENAME";
        var expectedEdits = testLspServer.GetLocations("renamed").Select(location => new LSP.TextEdit() { NewText = renameValue, Range = location.Range });

        var results = await RunRenameAsync(testLspServer, CreateRenameParams(renameLocation, renameValue));
        AssertJsonEquals(expectedEdits, ((TextDocumentEdit[])results.DocumentChanges).SelectMany(e => e.Edits));
    }

    [Theory, CombinatorialData]
    public async Task TestRename_WithRazorSourceGeneratedFile(bool mutatingLspWorkspace)
    {
        var generatedMarkup = """
            class B
            {
                void M()
                {
                    new A().{|renamed:M|}();

                    var a = new A();
                    a.{|renamed:M|}();
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync("""
            public class A
            {
                public void {|caret:|}{|renamed:M|}()
                {
                }

                void M2()
                {
                    {|renamed:M|}()
                }
            }
            """, mutatingLspWorkspace);

        TestFileMarkupParser.GetSpans(generatedMarkup, out var generatedCode, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans);
        var generatedSourceText = SourceText.From(generatedCode);

        var razorGenerator = new Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator((c) => c.AddSource("generated_file.cs", generatedCode));
        var workspace = testLspServer.TestWorkspace;
        var project = workspace.CurrentSolution.Projects.First().AddAnalyzerReference(new TestGeneratorReference(razorGenerator));
        // Add a pretend razor document, but using the generated code so that we don't need to actually map positions
        var razorFilePath = Path.Combine(Path.GetDirectoryName(project.FilePath), "file.razor");
        var razorDocument = project.AddAdditionalDocument("file.razor", SourceText.From(generatedCode), filePath: razorFilePath);
        workspace.TryApplyChanges(razorDocument.Project.Solution);

        var generatedDocument = (await workspace.CurrentSolution.Projects.First().GetSourceGeneratedDocumentsAsync()).First();

        var service = Assert.IsType<TestSourceGeneratedDocumentSpanMappingService>(workspace.Services.GetService<ISourceGeneratedDocumentSpanMappingService>());
        service.AddMappedFileName(generatedDocument.FilePath, razorFilePath);

        var renameLocation = testLspServer.GetLocations("caret").First();
        var renameValue = "RENAME";
        var expectedEdits = testLspServer.GetLocations("renamed").Select(location => new LSP.TextEdit() { NewText = renameValue, Range = location.Range });
        var expectedGeneratedEdits = spans["renamed"].Select(span => new LSP.TextEdit() { NewText = renameValue, Range = ProtocolConversions.TextSpanToRange(span, generatedSourceText) });

        var results = await RunRenameAsync(testLspServer, CreateRenameParams(renameLocation, renameValue));
        AssertJsonEquals(expectedEdits.Concat(expectedGeneratedEdits), ((TextDocumentEdit[])results.DocumentChanges).SelectMany(e => e.Edits));

        Assert.True(service.DidMapEdits);
    }

    [Theory, CombinatorialData]
    public async Task TestRename_WithRazorSourceGeneratedFile_NoMappingService(bool mutatingLspWorkspace)
    {
        var generatedMarkup = """
            class B
            {
                void M()
                {
                    new A().{|renamed:M|}();

                    var a = new A();
                    a.{|renamed:M|}();
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync("""
            public class A
            {
                public void {|caret:|}{|renamed:M|}()
                {
                }

                void M2()
                {
                    {|renamed:M|}()
                }
            }
            """, mutatingLspWorkspace);

        TestFileMarkupParser.GetSpans(generatedMarkup, out var generatedCode, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans);
        var generatedSourceText = SourceText.From(generatedCode);

        var razorGenerator = new Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator((c) => c.AddSource("generated_file.cs", generatedCode));
        var workspace = testLspServer.TestWorkspace;
        var project = workspace.CurrentSolution.Projects.First().AddAnalyzerReference(new TestGeneratorReference(razorGenerator));
        workspace.TryApplyChanges(project.Solution);

        var generatedDocument = (await workspace.CurrentSolution.Projects.First().GetSourceGeneratedDocumentsAsync()).First();

        var service = Assert.IsType<TestSourceGeneratedDocumentSpanMappingService>(workspace.Services.GetService<ISourceGeneratedDocumentSpanMappingService>());
        service.Enable = false;

        var renameLocation = testLspServer.GetLocations("caret").First();
        var renameValue = "RENAME";
        var expectedEdits = testLspServer.GetLocations("renamed").Select(location => new LSP.TextEdit() { NewText = renameValue, Range = location.Range });
        var expectedGeneratedEdits = spans["renamed"].Select(span => new LSP.TextEdit() { NewText = renameValue, Range = ProtocolConversions.TextSpanToRange(span, generatedSourceText) });

        var results = await RunRenameAsync(testLspServer, CreateRenameParams(renameLocation, renameValue));
        AssertJsonEquals(expectedEdits.Concat(expectedGeneratedEdits), ((TextDocumentEdit[])results.DocumentChanges).SelectMany(e => e.Edits));

        Assert.False(service.DidMapEdits);
    }

    [Theory, CombinatorialData]
    public async Task TestRename_OriginateInSourceGeneratedFile(bool mutatingLspWorkspace)
    {
        var generatedMarkup = """
            class B
            {
                void M()
                {
                    new A().{|caret:|}{|renamed:M|}();

                    var a = new A();
                    a.{|renamed:M|}();
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync("""
            public class A
            {
                public void {|renamed:M|}()
                {
                }

                void M2()
                {
                    {|renamed:M|}()
                }
            }
            """, mutatingLspWorkspace);

        TestFileMarkupParser.GetSpans(generatedMarkup, out var generatedCode, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans);
        var generatedSourceText = SourceText.From(generatedCode);

        var razorGenerator = new Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator((c) => c.AddSource("generated_file.cs", generatedCode));
        var workspace = testLspServer.TestWorkspace;
        var project = workspace.CurrentSolution.Projects.First().AddAnalyzerReference(new TestGeneratorReference(razorGenerator));
        // Add a pretend razor document, but using the generated code so that we don't need to actually map positions
        var razorFilePath = Path.Combine(Path.GetDirectoryName(project.FilePath), "file.razor");
        var razorDocument = project.AddAdditionalDocument("file.razor", SourceText.From(generatedCode), filePath: razorFilePath);
        workspace.TryApplyChanges(razorDocument.Project.Solution);

        var generatedDocument = (await workspace.CurrentSolution.Projects.First().GetSourceGeneratedDocumentsAsync()).First();

        var service = Assert.IsType<TestSourceGeneratedDocumentSpanMappingService>(workspace.Services.GetService<ISourceGeneratedDocumentSpanMappingService>());
        service.AddMappedFileName(generatedDocument.FilePath, razorFilePath);

        var renameLocation = new LSP.Location()
        {
            DocumentUri = generatedDocument.GetURI(),
            Range = ProtocolConversions.TextSpanToRange(spans["caret"].First(), generatedSourceText)
        };
        var renameValue = "RENAME";
        var expectedEdits = testLspServer.GetLocations("renamed").Select(location => new LSP.TextEdit() { NewText = renameValue, Range = location.Range });
        var expectedGeneratedEdits = spans["renamed"].Select(span => new LSP.TextEdit() { NewText = renameValue, Range = ProtocolConversions.TextSpanToRange(span, generatedSourceText) });

        var results = await RunRenameAsync(testLspServer, CreateRenameParams(renameLocation, renameValue));
        AssertJsonEquals(expectedEdits.Concat(expectedGeneratedEdits), ((TextDocumentEdit[])results.DocumentChanges).SelectMany(e => e.Edits));

        Assert.True(service.DidMapEdits);
    }

    [Theory, CombinatorialData]
    public async Task TestRename_OriginateInSourceGeneratedFile_Local(bool mutatingLspWorkspace)
    {
        var generatedMarkup = """
            class B
            {
                void M()
                {
                    var {|caret:|}{|renamed:a|} = new A();
                    {|renamed:a|}.M();
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync("""
            public class A
            {
                public void M()
                {
                }
            }
            """, mutatingLspWorkspace);

        TestFileMarkupParser.GetSpans(generatedMarkup, out var generatedCode, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans);
        var generatedSourceText = SourceText.From(generatedCode);

        var razorGenerator = new Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator((c) => c.AddSource("generated_file.cs", generatedCode));
        var workspace = testLspServer.TestWorkspace;
        var project = workspace.CurrentSolution.Projects.First().AddAnalyzerReference(new TestGeneratorReference(razorGenerator));
        // Add a pretend razor document, but using the generated code so that we don't need to actually map positions
        var razorFilePath = Path.Combine(Path.GetDirectoryName(project.FilePath), "file.razor");
        var razorDocument = project.AddAdditionalDocument("file.razor", SourceText.From(generatedCode), filePath: razorFilePath);
        workspace.TryApplyChanges(razorDocument.Project.Solution);

        var generatedDocument = (await workspace.CurrentSolution.Projects.First().GetSourceGeneratedDocumentsAsync()).First();

        var service = Assert.IsType<TestSourceGeneratedDocumentSpanMappingService>(workspace.Services.GetService<ISourceGeneratedDocumentSpanMappingService>());
        service.AddMappedFileName(generatedDocument.FilePath, razorFilePath);

        var renameLocation = new LSP.Location()
        {
            DocumentUri = generatedDocument.GetURI(),
            Range = ProtocolConversions.TextSpanToRange(spans["caret"].First(), generatedSourceText)
        };
        var renameValue = "RENAME";
        var expectedGeneratedEdits = spans["renamed"].Select(span => new LSP.TextEdit() { NewText = renameValue, Range = ProtocolConversions.TextSpanToRange(span, generatedSourceText) });

        var results = await RunRenameAsync(testLspServer, CreateRenameParams(renameLocation, renameValue));
        AssertJsonEquals(expectedGeneratedEdits, ((TextDocumentEdit[])results.DocumentChanges).SelectMany(e => e.Edits));

        Assert.True(service.DidMapEdits);
    }

    private static LSP.RenameParams CreateRenameParams(LSP.Location location, string newName)
        => new()
        {
            NewName = newName,
            Position = location.Range.Start,
            TextDocument = CreateTextDocumentIdentifier(location.DocumentUri)
        };

    private static async Task<WorkspaceEdit> RunRenameAsync(TestLspServer testLspServer, LSP.RenameParams renameParams)
    {
        return await testLspServer.ExecuteRequestAsync<LSP.RenameParams, LSP.WorkspaceEdit>(LSP.Methods.TextDocumentRenameName, renameParams, CancellationToken.None);
    }
}
