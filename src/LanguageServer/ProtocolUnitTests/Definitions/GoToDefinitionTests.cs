// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.FileBasedPrograms;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Definitions;

public sealed class GoToDefinitionTests : AbstractLanguageServerProtocolTests
{
    public GoToDefinitionTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    protected override TestComposition Composition => base.Composition.AddParts(typeof(TestSourceGeneratedDocumentSpanMappingService));

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionAsync(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class A
            {
                string {|definition:aString|} = 'hello';
                void M()
                {
                    var len = {|caret:|}aString.Length;
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        // Verify that as originally serialized, the URI had a file scheme.
        Assert.True(results.Single().DocumentUri.GetRequiredParsedUri().ToString().StartsWith("file"));
        AssertLocationsEqual(testLspServer.GetLocations("definition"), results);
    }

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionAsync_DifferentDocument(bool mutatingLspWorkspace)
    {
        var markups = new string[]
        {
            """
            namespace One
            {
                class A
                {
                    public static int {|definition:aInt|} = 1;
                }
            }
            """,
            """
            namespace One
            {
                class B
                {
                    int bInt = One.A.{|caret:|}aInt;
                }
            }
            """
        };

        await using var testLspServer = await CreateTestLspServerAsync(markups, mutatingLspWorkspace);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        AssertLocationsEqual(testLspServer.GetLocations("definition"), results);
    }

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionAsync_MappedFile(bool mutatingLspWorkspace)
    {
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace);

        AddMappedDocument(testLspServer.TestWorkspace, """
            class A
            {
                string aString = 'hello';
                void M()
                {
                    var len = aString.Length;
                }
            }
            """);

        var position = new LSP.Position { Line = 5, Character = 18 };
        var results = await RunGotoDefinitionAsync(testLspServer, new LSP.Location
        {
            DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(TestHelpers.GetRootedPath(TestSpanMapper.GeneratedFileName)),
            Range = new LSP.Range { Start = position, End = position }
        });
        AssertLocationsEqual([TestSpanMapper.MappedFileLocation], results);
    }

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionAsync_InvalidLocation(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class A
            {
                void M()
                {{|caret:|}
                    var len = aString.Length;
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        Assert.Empty(results);
    }

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileBasedProgramRefDirective(bool mutatingLspWorkspace)
    {
        var referencedFilePath = Path.Combine(TestWorkspace.RootDirectory, "Util.cs");
        var markup =
            """
            #:ref {|caret:|}Util.cs
            System.Console.WriteLine();
            """;
        await using var testLspServer = await CreateTestLspServerAsync(
            markup,
            mutatingLspWorkspace,
            new InitializationOptions
            {
                ParseOptions = CSharpParseOptions.Default.WithFeatures([new("FileBasedProgram", "true")]),
            });

        AddFileBasedProgramTarget(testLspServer, "ref", referencedFilePath);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());

        var result = Assert.Single(results);
        Assert.Equal(ProtocolConversions.CreateAbsoluteDocumentUri(referencedFilePath), result.DocumentUri);
        Assert.Equal(new LSP.Position(), result.Range.Start);
        Assert.Equal(new LSP.Position(), result.Range.End);
    }

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileBasedProgramIncludeDirective(bool mutatingLspWorkspace)
    {
        var markup =
            """
            <Workspace>
                <Project Language="C#" CommonReferences="true" Features="FileBasedProgram=true">
                    <Document FilePath="C:\App.cs">
                        #:include {|caret:|}Util.cs
                        System.Console.WriteLine();
                    </Document>
                    <Document FilePath="C:\Util.cs">{|definition:|}class Util;</Document>
                </Project>
            </Workspace>
            """;
        await using var testLspServer = await CreateXmlTestLspServerAsync(markup, mutatingLspWorkspace);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());

        AssertLocationsEqual(testLspServer.GetLocations("definition"), results);
    }

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileBasedProgramIncludeDirectiveDirectory(bool mutatingLspWorkspace)
    {
        var markup =
            """
            #:include {|caret:|}Lib
            System.Console.WriteLine();
            """;
        await using var testLspServer = await CreateTestLspServerAsync(
            markup,
            mutatingLspWorkspace,
            new InitializationOptions
            {
                ParseOptions = CSharpParseOptions.Default.WithFeatures([new("FileBasedProgram", "true")]),
            });

        AddFileBasedProgramTarget(testLspServer, "include", Path.Combine(TestWorkspace.RootDirectory, "Lib", "Util.cs"));

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());

        Assert.Empty(results);
    }

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileBasedProgramProjectDirectiveDirectory(bool mutatingLspWorkspace)
    {
        var projectFilePath = Path.Combine(TestWorkspace.RootDirectory, "Lib", "Library.csproj");
        var markup =
            """
            #:project {|caret:|}Lib
            System.Console.WriteLine();
            """;
        await using var testLspServer = await CreateTestLspServerAsync(
            markup,
            mutatingLspWorkspace,
            new InitializationOptions
            {
                ParseOptions = CSharpParseOptions.Default.WithFeatures([new("FileBasedProgram", "true")]),
            });

        AddFileBasedProgramTarget(testLspServer, "project", projectFilePath);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());

        var result = Assert.Single(results);
        Assert.Equal(ProtocolConversions.CreateAbsoluteDocumentUri(projectFilePath), result.DocumentUri);
    }

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileBasedProgramProjectDirectiveDirectoryWithMultipleProjects(bool mutatingLspWorkspace)
    {
        var firstProjectFilePath = Path.Combine(TestWorkspace.RootDirectory, "Lib", "First.csproj");
        var secondProjectFilePath = Path.Combine(TestWorkspace.RootDirectory, "Lib", "Second.csproj");
        var markup =
            """
            #:project {|caret:|}Lib
            System.Console.WriteLine();
            """;
        await using var testLspServer = await CreateTestLspServerAsync(
            markup,
            mutatingLspWorkspace,
            new InitializationOptions
            {
                ParseOptions = CSharpParseOptions.Default.WithFeatures([new("FileBasedProgram", "true")]),
            });

        AddFileBasedProgramTarget(testLspServer, "project", firstProjectFilePath);
        AddFileBasedProgramTarget(testLspServer, "project", secondProjectFilePath);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());

        AssertEx.SetEqual(
            new[] { ProtocolConversions.CreateAbsoluteDocumentUri(firstProjectFilePath), ProtocolConversions.CreateAbsoluteDocumentUri(secondProjectFilePath) },
            results.Select(result => result.DocumentUri));
    }

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileBasedProgramProjectDirectiveFile(bool mutatingLspWorkspace)
    {
        var projectFilePath = Path.Combine(TestWorkspace.RootDirectory, "Library.csproj");
        var markup =
            """
            #:project {|caret:|}Library.csproj
            System.Console.WriteLine();
            """;
        await using var testLspServer = await CreateTestLspServerAsync(
            markup,
            mutatingLspWorkspace,
            new InitializationOptions
            {
                ParseOptions = CSharpParseOptions.Default.WithFeatures([new("FileBasedProgram", "true")]),
            });

        AddFileBasedProgramTarget(testLspServer, "project", projectFilePath);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());

        var result = Assert.Single(results);
        Assert.Equal(ProtocolConversions.CreateAbsoluteDocumentUri(projectFilePath), result.DocumentUri);
    }

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileBasedProgramDirectiveRelativeToSourceDirectory(
        [CombinatorialValues("project", "ref", "include")] string directiveName,
        [CombinatorialValues("/", "\\")] string directorySeparator,
        bool mutatingLspWorkspace)
    {
        var sourceFilePath = Path.Combine(TestWorkspace.RootDirectory, "App", "Program.cs");
        var targetFilePath = Path.Combine(TestWorkspace.RootDirectory, "Lib", directiveName == "project" ? "Library.csproj" : "Util.cs");
        var directivePath = directiveName == "project"
            ? $"..{directorySeparator}Lib"
            : $"..{directorySeparator}Lib{directorySeparator}Util.cs";
        var markup =
            $$"""
            <Workspace>
                <Project Language="C#" CommonReferences="true" Features="FileBasedProgram=true">
                    <Document FilePath="{{sourceFilePath}}">
                        #:{{directiveName}} {|caret:|}{{directivePath}}
                        System.Console.WriteLine();
                    </Document>
                </Project>
            </Workspace>
            """;
        await using var testLspServer = await CreateXmlTestLspServerAsync(markup, mutatingLspWorkspace);

        AddFileBasedProgramTarget(testLspServer, directiveName, targetFilePath);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());

        Assert.Equal(ProtocolConversions.CreateAbsoluteDocumentUri(targetFilePath), Assert.Single(results).DocumentUri);
    }

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileBasedProgramDirectiveWithDynamicPath(
        [CombinatorialValues("project", "ref", "include")] string directiveName,
        bool mutatingLspWorkspace)
    {
        // MSBuild variables and globs are not expanded for navigation.
        // We could consider this as a future improvement to the feature.
        var sourceFilePath = Path.Combine(TestWorkspace.RootDirectory, "App", "Program.cs");
        var targetFilePath = Path.Combine(TestWorkspace.RootDirectory,
            directiveName == "include" ? "App" : "Lib", directiveName == "project" ? "Library.csproj" : "Util.cs");
        var directivePath = directiveName switch
        {
            "project" => "$(MSBuildProjectDirectory)/../Lib",
            "ref" => "$(MSBuildProjectDirectory)/../Lib/Util.cs",
            "include" => "*.cs",
            _ => throw ExceptionUtilities.Unreachable(),
        };
        var markup =
            $$"""
            <Workspace>
                <Project Language="C#" CommonReferences="true" Features="FileBasedProgram=true">
                    <Document FilePath="{{sourceFilePath}}">
                        #:{{directiveName}} {|caret:|}{{directivePath}}
                        System.Console.WriteLine();
                    </Document>
                </Project>
            </Workspace>
            """;
        await using var testLspServer = await CreateXmlTestLspServerAsync(markup, mutatingLspWorkspace);

        AddFileBasedProgramTarget(testLspServer, directiveName, targetFilePath);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());

        Assert.Empty(results);
    }

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileBasedProgramDirectiveOutsidePath(bool mutatingLspWorkspace)
    {
        // Cannot navigate to the directive name, since it's not part of the path
        var markup =
            """
            #:{|caret:project|} Library.csproj
            System.Console.WriteLine();
            """;
        await using var testLspServer = await CreateTestLspServerAsync(
            markup,
            mutatingLspWorkspace,
            new InitializationOptions
            {
                ParseOptions = CSharpParseOptions.Default.WithFeatures([new("FileBasedProgram", "true")]),
            });

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());

        Assert.Empty(results);
    }

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileBasedProgramDirectiveMissingTarget(bool mutatingLspWorkspace)
    {
        // Cannot navigate to a target that is not part of the solution.
        var markup =
            """
            #:ref Missing{|caret:.|}cs
            System.Console.WriteLine();
            """;
        await using var testLspServer = await CreateTestLspServerAsync(
            markup,
            mutatingLspWorkspace,
            new InitializationOptions
            {
                ParseOptions = CSharpParseOptions.Default.WithFeatures([new("FileBasedProgram", "true")]),
            });

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());

        Assert.Empty(results);
    }

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileLevelDirectiveInProjectBasedProgram(bool mutatingLspWorkspace)
    {
        var referencedFilePath = Path.Combine(TestWorkspace.RootDirectory, "Util.cs");
        var markup =
            """
            #:ref {|caret:|}Util.cs
            class C;
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        AddFileBasedProgramTarget(testLspServer, "ref", referencedFilePath);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());

        Assert.Empty(results);
    }

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileBasedProgramWithRelativeDocumentPath(bool mutatingLspWorkspace)
    {
        // Cannot navigate when the resolved path referenced by a directive is relative.
        var markup =
            """
            #:ref {|caret:|}Util.cs
            System.Console.WriteLine();
            """;
        await using var testLspServer = await CreateTestLspServerAsync(
            markup,
            mutatingLspWorkspace,
            new InitializationOptions
            {
                ParseOptions = CSharpParseOptions.Default.WithFeatures([new("FileBasedProgram", "true")]),
            });

        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single()
            .WithFilePath(Path.Combine("relative", "Program.cs"));
        document = WithFileBasedProgramTarget(document, "ref", Path.Combine(TestWorkspace.RootDirectory, "Util.cs"));

        var paths = await document.Project.Solution.Services.GetRequiredService<IFileBasedProgramService>()
            .GetFileLevelDirectiveTargetPathsAsync(document, "#:ref ".Length, CancellationToken.None);

        Assert.Empty(paths);
    }

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileBasedProgramWithRelativeDocumentPathAndAbsoluteDirectivePath(
        [CombinatorialValues("project", "ref", "include")] string directiveName,
        bool mutatingLspWorkspace)
    {
        var targetDirectory = Path.Combine(TestWorkspace.RootDirectory, "Lib");
        var targetFilePath = Path.Combine(targetDirectory, directiveName == "project" ? "Library.csproj" : "Util.cs");
        var directivePath = directiveName == "project" ? targetDirectory : targetFilePath;
        var markup = $"#:{directiveName} {{|caret:|}}{directivePath}{Environment.NewLine}System.Console.WriteLine();";

        await using var testLspServer = await CreateTestLspServerAsync(
            markup,
            mutatingLspWorkspace,
            new InitializationOptions
            {
                ParseOptions = CSharpParseOptions.Default.WithFeatures([new("FileBasedProgram", "true")]),
            });

        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single()
            .WithFilePath(Path.Combine("relative", "Program.cs"));
        document = WithFileBasedProgramTarget(document, directiveName, targetFilePath);

        var paths = await document.Project.Solution.Services.GetRequiredService<IFileBasedProgramService>()
            .GetFileLevelDirectiveTargetPathsAsync(document, $"#:{directiveName} ".Length, CancellationToken.None);

        Assert.Equal(targetFilePath, Assert.Single(paths));
    }

    [ConditionalTheory(typeof(WindowsOnly)), CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileBasedProgramWithCurrentDriveRootedDirectivePath(bool mutatingLspWorkspace)
    {
        // A path such as '\Path\To\File.cs' resolves against the source document's drive.
        var referencedFilePath = Path.Combine(TestWorkspace.RootDirectory, "Util.cs");
        var currentDriveRootedPath = referencedFilePath.Substring(2);
        var markup = $"#:ref {{|caret:|}}{currentDriveRootedPath}";
        await using var testLspServer = await CreateTestLspServerAsync(
            markup,
            mutatingLspWorkspace,
            new InitializationOptions
            {
                ParseOptions = CSharpParseOptions.Default.WithFeatures([new("FileBasedProgram", "true")]),
            });

        AddFileBasedProgramTarget(testLspServer, "ref", referencedFilePath);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());

        Assert.Equal(ProtocolConversions.CreateAbsoluteDocumentUri(referencedFilePath), Assert.Single(results).DocumentUri);
    }

    [Theory, CombinatorialData, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1264627")]
    public async Task TestGotoDefinitionAsync_NoResultsOnNamespace(bool mutatingLspWorkspace)
    {
        var markup =
            """
            namespace {|caret:M|}
            {
                class A
                {
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        Assert.Empty(results);
    }

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionCrossLanguage(bool mutatingLspWorkspace)
    {
        var markup =
            """
            <Workspace>
                <Project Language="C#" Name="Definition" CommonReferences="true" FilePath="C:\CSProj1.csproj">
                    <Document FilePath="C:\A.cs">
                        public class {|definition:A|}
                        {
                        }
                    </Document>
                </Project>
                <Project Language="Visual Basic" CommonReferences="true" FilePath="C:\CSProj2.csproj">
                    <ProjectReference>Definition</ProjectReference>
                    <Document FilePath="C:\C.cs">
                        Class C
                            Dim a As {|caret:A|}
                        End Class
                    </Document>
                </Project>
            </Workspace>
            """;
        await using var testLspServer = await CreateXmlTestLspServerAsync(markup, mutatingLspWorkspace);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        AssertLocationsEqual(testLspServer.GetLocations("definition"), results);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/5740")]
    public async Task TestGotoDefinitionPartialMethods(bool mutatingLspWorkspace)
    {
        var markup =
            """
            using System;

            public partial class C
            {
                partial void {|caret:|}{|definition:P|}();
            }

            public partial class C
            {
                partial void P()
                {
                    Console.WriteLine(");
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        AssertLocationsEqual(testLspServer.GetLocations("definition"), results);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/vscode-csharp/issues/5740")]
    public async Task TestGotoDefinitionPartialProperties(bool mutatingLspWorkspace)
    {
        var markup =
            """
            using System;

            public partial class C
            {
                partial int {|caret:|}{|definition:Prop|} { get; set; }
            }

            public partial class C
            {
                partial int Prop { get => 1; set { } }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        AssertLocationsEqual(testLspServer.GetLocations("definition"), results);
    }

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionPartialEvents(bool mutatingLspWorkspace)
    {
        var markup = """
            using System;

            public partial class C
            {
                partial event Action {|caret:|}{|definition:E|};
            }

            public partial class C
            {
                partial event Action E { add { } remove { } }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        AssertLocationsEqual(testLspServer.GetLocations("definition"), results);
    }

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionPartialConstructors(bool mutatingLspWorkspace)
    {
        var markup = """
            using System;

            public partial class C
            {
                partial {|caret:|}{|definition:C|}();
            }

            public partial class C
            {
                partial C() { }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, new InitializationOptions
        {
            ParseOptions = TestOptions.RegularPreview,
        });

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        AssertLocationsEqual(testLspServer.GetLocations("definition"), results);
    }

    [Theory]
    [InlineData("ValueTuple<int> valueTuple1;")]
    [InlineData("ValueTuple<int, int> valueTuple2;")]
    [InlineData("ValueTuple<int, int, int> valueTuple3;")]
    [InlineData("ValueTuple<int, int, int, int> valueTuple4;")]
    [InlineData("ValueTuple<int, int, int, int, int> valueTuple5;")]
    [InlineData("ValueTuple<int, int, int, int, int, int> valueTuple6;")]
    [InlineData("ValueTuple<int, int, int, int, int, int, int> valueTuple7;")]
    [InlineData("ValueTuple<int, int, int, int, int, int, int, int> valueTuple8;")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/71680")]
    public async Task TestGotoDefinitionWithValueTuple(string statement)
    {
        var markup = $"using System; {{|caret:|}}{statement}";

        await using var testLspServer = await CreateTestLspServerAsync(markup, false);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        Assert.Single(results);
    }

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionAsync_SourceGeneratedDocument(bool mutatingLspWorkspace)
    {
        var source =
            """
            namespace M
            {
                class A
                {
                    public {|caret:|}B b;
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(source, mutatingLspWorkspace);
        await AddGeneratorAsync(new SingleFileTestGenerator("""
            namespace M
            {
                class B
                {
                }
            }
            """), testLspServer.TestWorkspace);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        var result = Assert.Single(results);
        Assert.Equal(SourceGeneratedDocumentUri.Scheme, result.DocumentUri.GetRequiredParsedUri().Scheme);
    }

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionMetadataIncludesTypeAsync(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class A
            {
                void M()
                {
                    System.Console.Write("Hel{|caret:|}lo");
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        Assert.True(results.Single().DocumentUri.UriString.EndsWith("String.cs"));
    }

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionAsync_WithRazorSourceGeneratedFile(bool mutatingLspWorkspace)
    {
        var generatedMarkup = """
            public class B
            {
                public void {|definition:M|}()
                {
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync("""
            public class A
            {
                public void M()
                {
                    new B().{|caret:M|}();
                }
            }
            """, mutatingLspWorkspace);

        TestFileMarkupParser.GetSpans(generatedMarkup, out var generatedCode, out ImmutableDictionary<string, ImmutableArray<TextSpan>> spans);
        var generatedSourceText = SourceText.From(generatedCode);

        var razorGenerator = new Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator((c) => c.AddSource("generated_file.cs", generatedCode));
        var workspace = testLspServer.TestWorkspace;
        var project = workspace.CurrentSolution.Projects.First().AddAnalyzerReference(new TestGeneratorReference(razorGenerator));
        workspace.TryApplyChanges(project.Solution);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        Assert.Contains("/generated_file.cs", results.Single().DocumentUri.UriString);

        var service = Assert.IsType<TestSourceGeneratedDocumentSpanMappingService>(workspace.Services.GetService<ISourceGeneratedDocumentSpanMappingService>());
        Assert.True(service.DidMapSpans);
    }

    private static void AddFileBasedProgramTarget(TestLspServer testLspServer, string directiveName, string targetPath)
    {
        var document = testLspServer.TestWorkspace.CurrentSolution.Projects.First().Documents.First();
        var updatedDocument = WithFileBasedProgramTarget(document, directiveName, targetPath);
        testLspServer.TestWorkspace.SetCurrentSolution(_ => updatedDocument.Project.Solution, WorkspaceChangeKind.SolutionChanged);
    }

    /// <summary>Create a changed solution which includes a stub item (file or project reference) which is matched by a directive with the given name and targetPath.</summary>
    private static Document WithFileBasedProgramTarget(Document document, string directiveName, string targetPath)
    {
        var project = document.Project;
        if (directiveName == "include")
        {
            return project.AddDocument(Path.GetFileName(targetPath), SourceText.From(""), filePath: targetPath)
                .Project.GetDocument(document.Id);
        }

        var targetProjectId = ProjectId.CreateNewId();
        var solution = project.Solution
            .AddProject(ProjectInfo.Create(targetProjectId, VersionStamp.Create(), "Target", "Target", LanguageNames.CSharp, filePath: targetPath))
            .AddProjectReference(project.Id, new ProjectReference(targetProjectId));
        return solution.GetDocument(document.Id);
    }

    private static async Task<LSP.Location[]> RunGotoDefinitionAsync(TestLspServer testLspServer, LSP.Location caret)
    {
        return await testLspServer.ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.Location[]>(LSP.Methods.TextDocumentDefinitionName,
                       CreateTextDocumentPositionParams(caret), CancellationToken.None);
    }
}
