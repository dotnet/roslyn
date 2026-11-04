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
using Roslyn.LanguageServer.Protocol;
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

    // Include directive cases:
    // - simple
    // - folder (sad)
    // - nested
    // - absolute paths
    // - globs

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileBasedProgramIncludeDirective(bool mutatingLspWorkspace)
    {
        var markup =
            """
            <Workspace>
                <Project Language="C#" CommonReferences="true" Features="FileBasedProgram=true">
                    <Document FilePath="App.cs">
                        #:include {|caret:|}Util.cs
                        System.Console.WriteLine();
                    </Document>
                    <Document FilePath="Util.cs">{|definition:|}class Util;</Document>
                </Project>
            </Workspace>
            """;
        await using var testLspServer = await CreateXmlTestLspServerAsync(markup, mutatingLspWorkspace);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());

        AssertLocationsEqual(testLspServer.GetLocations("definition"), results);
    }

    [ConditionalTheory(typeof(DesktopOnly)), CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileBasedProgramIncludeDirective_Glob_Desktop(bool mutatingLspWorkspace)
    {
        // Normalization of file paths containing globs throws on .NET Framework.
        // Specifically, 'Path.GetFullPath()' throws when the path has '*' in it on .NET Framework.
        // This test is here to demonstrate why the other glob tests are only run on .NET Core.
        var markup =
            """
            <Workspace>
                <Project Language="C#" CommonReferences="true" Features="FileBasedProgram=true">
                    <Document FilePath="App.cs">
                        #:include Files\{|caret:|}*.cs
                        System.Console.WriteLine();
                    </Document>
                    <Document FilePath="Files/Util.cs">{|definition:|}class Util;</Document>
                </Project>
            </Workspace>
            """;
        await using var testLspServer = await CreateXmlTestLspServerAsync(markup, mutatingLspWorkspace);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        Assert.Empty(results);
    }

    [ConditionalTheory(typeof(CoreClrOnly)), CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileBasedProgramIncludeDirective_Glob_01(bool mutatingLspWorkspace)
    {
        // Note: 'Document FilePath="..."' needs to use separators which work across platforms
        // in order for 'Path.Combine()' etc to work properly within the test harness.
        // But backslashes are expected to work within '#:<directive> <path>' on all platforms.
        // Therefore we tend to test using forward slashes for Document FilePaths and backslashes for directive paths.
        var markup =
            """
            <Workspace>
                <Project Language="C#" CommonReferences="true" Features="FileBasedProgram=true">
                    <Document FilePath="App.cs">
                        #:include Files\{|caret:|}*.cs
                        System.Console.WriteLine();
                    </Document>
                    <Document FilePath="Files/Util.cs">{|definition:|}class Util;</Document>
                </Project>
            </Workspace>
            """;
        await using var testLspServer = await CreateXmlTestLspServerAsync(markup, mutatingLspWorkspace);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());

        AssertLocationsEqual(testLspServer.GetLocations("definition"), results);
    }

    [ConditionalTheory(typeof(CoreClrOnly)), CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileBasedProgramIncludeDirective_Glob_02(bool mutatingLspWorkspace)
    {
        // A '*' glob does not match a file in nested folder
        var markup =
            """
            <Workspace>
                <Project Language="C#" CommonReferences="true" Features="FileBasedProgram=true">
                    <Document FilePath="App.cs">
                        #:include Files\{|caret:|}*.cs
                        System.Console.WriteLine();
                    </Document>
                    <Document FilePath="Files/Nested/Util.cs">class Util;</Document>
                </Project>
            </Workspace>
            """;
        await using var testLspServer = await CreateXmlTestLspServerAsync(markup, mutatingLspWorkspace);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        Assert.Empty(results);
    }

    [ConditionalTheory(typeof(CoreClrOnly)), CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileBasedProgramIncludeDirective_Glob_03(bool mutatingLspWorkspace)
    {
        // A '**' glob does match a file in a nested folder
        var markup =
            """
            <Workspace>
                <Project Language="C#" CommonReferences="true" Features="FileBasedProgram=true">
                    <Document FilePath="App.cs">
                        #:include Files\{|caret:|}**\*.cs
                        System.Console.WriteLine();
                    </Document>
                    <Document FilePath="Files/Nested/Util.cs">{|definition:|}class Util;</Document>
                </Project>
            </Workspace>
            """;
        await using var testLspServer = await CreateXmlTestLspServerAsync(markup, mutatingLspWorkspace);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        AssertLocationsEqual(testLspServer.GetLocations("definition"), results);
    }

    [ConditionalTheory(typeof(CoreClrOnly)), CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileBasedProgramIncludeDirective_Glob_04(bool mutatingLspWorkspace)
    {
        // Globs can match multiple files
        var markup =
            """
            <Workspace>
                <Project Language="C#" CommonReferences="true" Features="FileBasedProgram=true">
                    <Document FilePath="App.cs">
                        #:include Files\{|caret:|}*.cs
                        System.Console.WriteLine();
                    </Document>
                    <Document FilePath="Files/Util1.cs">{|definition:|}class Util1;</Document>
                    <Document FilePath="Files/Util2.cs">{|definition:|}class Util2;</Document>
                </Project>
            </Workspace>
            """;
        await using var testLspServer = await CreateXmlTestLspServerAsync(markup, mutatingLspWorkspace);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        Assert.Equal(2, results.Length);
        AssertLocationsEqual(testLspServer.GetLocations("definition"), results);
    }

    [ConditionalTheory(typeof(WindowsOnly), typeof(CoreClrOnly)), CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileBasedProgramIncludeDirective_Glob_05_Windows(bool mutatingLspWorkspace)
    {
        // Test a directive path which is both absolute in source and uses a glob
        var markup =
            """
            <Workspace>
                <Project Language="C#" CommonReferences="true" Features="FileBasedProgram=true">
                    <Document FilePath="C:\App.cs">
                        #:include C:\Files\{|caret:|}*.cs
                        System.Console.WriteLine();
                    </Document>
                    <Document FilePath="C:\Files\Util1.cs">{|definition:|}class Util1;</Document>
                    <Document FilePath="C:\Files\Util2.cs">{|definition:|}class Util2;</Document>
                </Project>
            </Workspace>
            """;
        await using var testLspServer = await CreateXmlTestLspServerAsync(markup, mutatingLspWorkspace);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());

        AssertLocationsEqual(testLspServer.GetLocations("definition"), results);
    }

    [ConditionalTheory(typeof(WindowsOnly), typeof(CoreClrOnly)), CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileBasedProgramIncludeDirective_Glob_05_Windows_MultiDrive(bool mutatingLspWorkspace)
    {
        // A directive path can refer to a different drive and also use a glob
        var markup =
            """
            <Workspace>
                <Project Language="C#" CommonReferences="true" Features="FileBasedProgram=true">
                    <Document FilePath="C:\App.cs">
                        #:include D:\Files\{|caret:|}*.cs
                        System.Console.WriteLine();
                    </Document>
                    <Document FilePath="D:\Files\Util1.cs">{|definition:|}class Util1;</Document>
                    <Document FilePath="D:\Files\Util2.cs">{|definition:|}class Util2;</Document>
                </Project>
            </Workspace>
            """;
        await using var testLspServer = await CreateXmlTestLspServerAsync(markup, mutatingLspWorkspace);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());

        AssertLocationsEqual(testLspServer.GetLocations("definition"), results);
    }

    [ConditionalTheory(typeof(WindowsOnly), typeof(CoreClrOnly)), CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileBasedProgramIncludeDirective_Glob_05_Unix(bool mutatingLspWorkspace)
    {
        // Test a directive path which is both absolute in source and uses a glob
        var markup =
            """
            <Workspace>
                <Project Language="C#" CommonReferences="true" Features="FileBasedProgram=true">
                    <Document FilePath="/App.cs">
                        #:include /Files/{|caret:|}*.cs
                        System.Console.WriteLine();
                    </Document>
                    <Document FilePath="/Files/Util1.cs">{|definition:|}class Util1;</Document>
                    <Document FilePath="/Files/Util2.cs">{|definition:|}class Util2;</Document>
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
        // An include directive referring to a directory, doesn't end up including anything
        var markup =
            """
            <Workspace>
                <Project Language="C#" CommonReferences="true" Features="FileBasedProgram=true">
                    <Document FilePath="App.cs">
                        #:include {|caret:|}Files
                        System.Console.WriteLine();
                    </Document>
                    <Document FilePath="Files/Util.cs">class Util;</Document>
                </Project>
            </Workspace>
            """;
        await using var testLspServer = await CreateXmlTestLspServerAsync(markup, mutatingLspWorkspace);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        Assert.Empty(results);
    }

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileBasedProgramProjectDirectiveDirectory(bool mutatingLspWorkspace)
    {
        // A project can be referenced by referencing its containing directory
        var markup =
            """
            <Workspace>
                <Project Language="C#" CommonReferences="true" Features="FileBasedProgram=true">
                    <Document FilePath="App.cs">
                        #:project {|caret:|}Lib
                        System.Console.WriteLine();
                    </Document>
                    <ProjectReference>Library</ProjectReference>
                </Project>
                <Project FilePath="Lib/Library.csproj" AssemblyName="Library" Language="C#" CommonReferences="true" Features="FileBasedProgram=true" />
            </Workspace>
            """;
        await using var testLspServer = await CreateXmlTestLspServerAsync(markup, mutatingLspWorkspace);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        var result = Assert.Single(results);
        var projectFilePath = Path.Combine(TestWorkspace.RootDirectory, "Lib", "Library.csproj");
        Assert.Equal(ProtocolConversions.CreateAbsoluteDocumentUri(projectFilePath), result.DocumentUri);
    }

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileBasedProgramProjectDirectiveDirectoryWithMultipleProjects(bool mutatingLspWorkspace)
    {
        // For '#:project Lib', return all projects directly contained in 'Lib/'.
        // Note: this doesn't actually verify whether the project system would give us such a structure.
        // An error at an earlier stage might block us from observing this in a real world project.
        var markup =
            """
            <Workspace>
                <Project Language="C#" CommonReferences="true" Features="FileBasedProgram=true">
                    <Document FilePath="App.cs">
                        #:project {|caret:|}Lib
                        System.Console.WriteLine();
                    </Document>
                    <ProjectReference>First</ProjectReference>
                    <ProjectReference>Second</ProjectReference>
                    <ProjectReference>Third</ProjectReference>
                </Project>
                <Project FilePath="Lib/First.csproj" AssemblyName="First" Language="C#" CommonReferences="true" Features="FileBasedProgram=true" />
                <Project FilePath="Lib/Second.csproj" AssemblyName="Second" Language="C#" CommonReferences="true" Features="FileBasedProgram=true" />
                <Project FilePath="Lib/Nested/Third.csproj" AssemblyName="Third" Language="C#" CommonReferences="true" Features="FileBasedProgram=true" />
            </Workspace>
            """;
        await using var testLspServer = await CreateXmlTestLspServerAsync(markup, mutatingLspWorkspace);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        Assert.Equal(2, results.Length);

        var firstProjectPath = Path.Combine(TestWorkspace.RootDirectory, "Lib", "First.csproj");
        Assert.Equal(ProtocolConversions.CreateAbsoluteDocumentUri(firstProjectPath), results[0].DocumentUri);
        var secondProjectPath = Path.Combine(TestWorkspace.RootDirectory, "Lib", "Second.csproj");
        Assert.Equal(ProtocolConversions.CreateAbsoluteDocumentUri(secondProjectPath), results[1].DocumentUri);
    }

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileBasedProgramProjectDirectiveDirectoryWithMultipleProjects_Glob(bool mutatingLspWorkspace)
    {
        // Globs can also match multiple project files
        var markup =
            """
            <Workspace>
                <Project Language="C#" CommonReferences="true" Features="FileBasedProgram=true">
                    <Document FilePath="App.cs">
                        #:project {|caret:|}Lib/*.csproj
                        System.Console.WriteLine();
                    </Document>
                    <ProjectReference>First</ProjectReference>
                    <ProjectReference>Second</ProjectReference>
                    <ProjectReference>Third</ProjectReference>
                </Project>
                <Project FilePath="Lib/First.csproj" AssemblyName="First" Language="C#" CommonReferences="true" Features="FileBasedProgram=true" />
                <Project FilePath="Lib/Second.csproj" AssemblyName="Second" Language="C#" CommonReferences="true" Features="FileBasedProgram=true" />
                <Project FilePath="Lib/Nested/Third.csproj" AssemblyName="Third" Language="C#" CommonReferences="true" Features="FileBasedProgram=true" />
            </Workspace>
            """;
        await using var testLspServer = await CreateXmlTestLspServerAsync(markup, mutatingLspWorkspace);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        Assert.Equal(2, results.Length);

        var firstProjectPath = Path.Combine(TestWorkspace.RootDirectory, "Lib", "First.csproj");
        Assert.Equal(ProtocolConversions.CreateAbsoluteDocumentUri(firstProjectPath), results[0].DocumentUri);
        var secondProjectPath = Path.Combine(TestWorkspace.RootDirectory, "Lib", "Second.csproj");
        Assert.Equal(ProtocolConversions.CreateAbsoluteDocumentUri(secondProjectPath), results[1].DocumentUri);
    }

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileBasedProgramProjectDirectiveFile(bool mutatingLspWorkspace)
    {
        // A project can be referenced by exact path
        var markup =
            """
            <Workspace>
                <Project Language="C#" CommonReferences="true" Features="FileBasedProgram=true">
                    <Document FilePath="App.cs">
                        #:project {|caret:|}Lib\Library.csproj
                        System.Console.WriteLine();
                    </Document>
                    <ProjectReference>Library</ProjectReference>
                </Project>
                <Project FilePath="Lib/Library.csproj" AssemblyName="Library" Language="C#" CommonReferences="true" Features="FileBasedProgram=true" />
            </Workspace>
            """;
        await using var testLspServer = await CreateXmlTestLspServerAsync(markup, mutatingLspWorkspace);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        var result = Assert.Single(results);
        var projectFilePath = Path.Combine(TestWorkspace.RootDirectory, "Lib", "Library.csproj");
        Assert.Equal(ProtocolConversions.CreateAbsoluteDocumentUri(projectFilePath), result.DocumentUri);
    }

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileBasedProgramRefDirective(bool mutatingLspWorkspace)
    {
        var markup =
            """
            <Workspace>
                <Project Language="C#" CommonReferences="true" Features="FileBasedProgram=true">
                    <Document FilePath="App.cs">
                        #:ref {|caret:|}Util.cs
                        System.Console.WriteLine();
                    </Document>
                    <ProjectReference>Util</ProjectReference>
                </Project>
                <Project FilePath="Util.cs" AssemblyName="Util" Language="C#" CommonReferences="true" Features="FileBasedProgram=true">
                    <Document FilePath="Util.cs">{|definition:|}class Util;</Document>
                </Project>
            </Workspace>
            """;
        await using var testLspServer = await CreateXmlTestLspServerAsync(markup, mutatingLspWorkspace);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());

        AssertLocationsEqual(testLspServer.GetLocations("definition"), results);
    }

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileBasedProgramRefDirective_Directory(bool mutatingLspWorkspace)
    {
        // A ref directive cannot refer to containing folder of the file to reference.
        var markup =
            """
            <Workspace>
                <Project Language="C#" CommonReferences="true" Features="FileBasedProgram=true">
                    <Document FilePath="App.cs">
                        #:ref {|caret:|}Files
                        System.Console.WriteLine();
                    </Document>
                    <ProjectReference>Util</ProjectReference>
                </Project>
                <Project FilePath="Files/Util.cs" AssemblyName="Util" Language="C#" CommonReferences="true" Features="FileBasedProgram=true">
                    <Document FilePath="Files/Util.cs">class Util;</Document>
                </Project>
            </Workspace>
            """;
        await using var testLspServer = await CreateXmlTestLspServerAsync(markup, mutatingLspWorkspace);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        Assert.Empty(results);
    }

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileBasedProgramRefDirective_Glob(bool mutatingLspWorkspace)
    {
        // A ref directive can refer to multiple files via glob
        var markup =
            """
            <Workspace>
                <Project Language="C#" CommonReferences="true" Features="FileBasedProgram=true">
                    <Document FilePath="App.cs">
                        #:ref {|caret:|}Files\*
                        System.Console.WriteLine();
                    </Document>
                    <ProjectReference>Util1</ProjectReference>
                    <ProjectReference>Util2</ProjectReference>
                </Project>
                <Project FilePath="Files/Util1.cs" AssemblyName="Util1" Language="C#" CommonReferences="true" Features="FileBasedProgram=true">
                    <Document FilePath="Files/Util1.cs">{|definition:|}class Util1;</Document>
                </Project>
                <Project FilePath="Files/Util2.cs" AssemblyName="Util2" Language="C#" CommonReferences="true" Features="FileBasedProgram=true">
                    <Document FilePath="Files/Util2.cs">{|definition:|}class Util2;</Document>
                </Project>
            </Workspace>
            """;
        await using var testLspServer = await CreateXmlTestLspServerAsync(markup, mutatingLspWorkspace);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        Assert.Equal(2, results.Length);

        AssertLocationsEqual(testLspServer.GetLocations("definition"), results);
    }

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileBasedProgramDirective_MSBuildVariable(
        bool mutatingLspWorkspace)
    {
        // MSBuild variables and globs are not expanded for navigation.
        // We could consider this as a future improvement to the feature.
        var markup =
            $$"""
            <Workspace>
                <Project FilePath="App/App.csproj" Language="C#" CommonReferences="true" Features="FileBasedProgram=true">
                    <Document FilePath="App/Program.cs">
                        #:include {|caret:|}$(MSBuildProjectDirectory)/Util.cs
                        System.Console.WriteLine();
                    </Document>
                    <Document FilePath="App/Util.cs">class Util;</Document>
                </Project>
            </Workspace>
            """;
        await using var testLspServer = await CreateXmlTestLspServerAsync(markup, mutatingLspWorkspace);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        Assert.Empty(results);
    }

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileBasedProgramDirectiveCaretNotOnPath(bool mutatingLspWorkspace)
    {
        // Navigation only works if the caret is on the path part of the directive
        // Note: there are "directive metadata" changes in flight here, possibly we wouldn't want to navigate if the caret is on the "metadata" part.
        // This could be a possible future improvement.
        var markup =
            """
            <Workspace>
                <Project Language="C#" CommonReferences="true" Features="FileBasedProgram=true">
                    <Document FilePath="App.cs">
                        #:{|caret:|}project Lib\Library.csproj
                        System.Console.WriteLine();
                    </Document>
                    <ProjectReference>Library</ProjectReference>
                </Project>
                <Project FilePath="Lib/Library.csproj" AssemblyName="Library" Language="C#" CommonReferences="true" Features="FileBasedProgram=true" />
            </Workspace>
            """;
        await using var testLspServer = await CreateXmlTestLspServerAsync(markup, mutatingLspWorkspace);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        Assert.Empty(results);
    }

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileBasedProgramDirectiveMissingTarget(bool mutatingLspWorkspace)
    {
        // Cannot navigate to a target that is not actually being referenced.
        var markup =
            """
            <Workspace>
                <Project Language="C#" CommonReferences="true" Features="FileBasedProgram=true">
                    <Document FilePath="App.cs">
                        #:{|caret:|}project Lib\Library.csproj
                        System.Console.WriteLine();
                    </Document>
                </Project>
                <Project FilePath="Lib/Library.csproj" AssemblyName="Library" Language="C#" CommonReferences="true" />
            </Workspace>
            """;
        await using var testLspServer = await CreateXmlTestLspServerAsync(markup, mutatingLspWorkspace);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        Assert.Empty(results);
    }

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileBasedProgramDirective_NotInFileBasedProgram(bool mutatingLspWorkspace)
    {
        // Cannot navigate via directive if not in a file-based program.
        var markup =
            """
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document FilePath="App.cs">
                        #:{|caret:|}project Lib\Library.csproj
                        System.Console.WriteLine();
                    </Document>
                    <ProjectReference>Library</ProjectReference>
                </Project>
                <Project FilePath="Lib/Library.csproj" AssemblyName="Library" Language="C#" CommonReferences="true" />
            </Workspace>
            """;
        await using var testLspServer = await CreateXmlTestLspServerAsync(markup, mutatingLspWorkspace);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        Assert.Empty(results);
    }

    [Theory, CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileBasedProgramWithRelativeDocumentPath(bool mutatingLspWorkspace)
    {
        // Document needs to have an absolute path for us to navigate to it.
        // Note: this mostly illustrates a theoretical limitation around the way we resolve directive paths and use the glob matcher.
        // Normal projects generally always use absolute paths for these.
        var markup =
            """
            <Workspace>
                <Project Language="C#" CommonReferences="true">
                    <Document FilePath="App.cs" ResolveFilePath="false">
                        #:{|caret:|}project Lib\Library.csproj
                        System.Console.WriteLine();
                    </Document>
                    <ProjectReference>Library</ProjectReference>
                </Project>
                <Project FilePath="Lib/Library.csproj" ResolveFilePath="false" AssemblyName="Library" Language="C#" CommonReferences="true" />
            </Workspace>
            """;
        await using var testLspServer = await CreateXmlTestLspServerAsync(markup, mutatingLspWorkspace);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        Assert.Null(results);
    }

    [ConditionalTheory(typeof(WindowsOnly)), CombinatorialData]
    public async Task TestGotoDefinitionAsync_FileBasedProgramWithCurrentDriveRootedDirectivePath(bool mutatingLspWorkspace)
    {
        // A path such as '\Path\To\File.cs' resolves against the source document's drive.
        var markup =
            """
            <Workspace>
                <Project Language="C#" CommonReferences="true" Features="FileBasedProgram=true">
                    <Document FilePath="D:\App.cs">
                        #:ref {|caret:|}\Files\Util.cs
                        System.Console.WriteLine();
                    </Document>
                    <ProjectReference>Util</ProjectReference>
                </Project>
                <Project FilePath="D:\Files\Util.cs" AssemblyName="Util" Language="C#" CommonReferences="true" Features="FileBasedProgram=true">
                    <Document FilePath="D:\Files\Util.cs">{|definition:|}class Util;</Document>
                </Project>
            </Workspace>
            """;
        await using var testLspServer = await CreateXmlTestLspServerAsync(markup, mutatingLspWorkspace);

        var results = await RunGotoDefinitionAsync(testLspServer, testLspServer.GetLocations("caret").Single());

        AssertLocationsEqual(testLspServer.GetLocations("definition"), results);
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
