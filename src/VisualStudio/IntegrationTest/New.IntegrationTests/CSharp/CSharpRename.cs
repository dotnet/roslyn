// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.InlineRename;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.IntegrationTests.InProcess;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using WindowsInput.Native;
using Xunit;
using Xunit.Abstractions;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

[Trait(Traits.Feature, Traits.Features.Rename)]
public sealed class CSharpRename(ITestOutputHelper output) : AbstractEditorTest(nameof(CSharpRename))
{
    private readonly ITestOutputHelper _output = output;

    protected override string LanguageName => LanguageNames.CSharp;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // reset relevant global options to default values:
        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        globalOptions.SetGlobalOption(InlineRenameSessionOptionsStorage.RenameInComments, false);
        globalOptions.SetGlobalOption(InlineRenameSessionOptionsStorage.RenameInStrings, false);
        globalOptions.SetGlobalOption(InlineRenameSessionOptionsStorage.RenameOverloads, false);
        globalOptions.SetGlobalOption(InlineRenameSessionOptionsStorage.RenameFile, true);
        globalOptions.SetGlobalOption(InlineRenameSessionOptionsStorage.PreviewChanges, false);
    }

    [IdeFact]
    public async Task VerifyLocalVariableRename()
    {
        _output.WriteLine("CSharpRename.VerifyLocalVariableRename: action 1");
        var markup = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    int [|x|]$$ = 0;
                    [|x|] = 5;
                    TestMethod([|x|]);
                }

                static void TestMethod(int y)
                {

                }
            }
            """;
        _output.WriteLine("CSharpRename.VerifyLocalVariableRename: action 2");
        await using var telemetry = await TestServices.Telemetry.EnableTestTelemetryChannelAsync(HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyLocalVariableRename: action 3");
        await SetUpEditorAsync(markup, HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyLocalVariableRename: action 4");
        await TestServices.InlineRename.InvokeAsync(HangMitigatingCancellationToken);

        _output.WriteLine("CSharpRename.VerifyLocalVariableRename: action 5");
        MarkupTestFile.GetSpans(markup, out var _, out var renameSpans);
        _output.WriteLine("CSharpRename.VerifyLocalVariableRename: action 6");
        var tags = await TestServices.Editor.GetRenameTagsAsync(HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyLocalVariableRename: action 7");
        var tagSpans = tags.SelectAsArray(tag => new TextSpan(tag.Span.Start, tag.Span.Length));
        _output.WriteLine("CSharpRename.VerifyLocalVariableRename: action 8");
        AssertEx.SetEqual(renameSpans, tagSpans);

        _output.WriteLine("CSharpRename.VerifyLocalVariableRename: action 9");
        await TestServices.Input.SendWithoutActivateAsync([VirtualKeyCode.VK_Y, VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyLocalVariableRename: action 10");
        await TestServices.Workspace.WaitForRenameAsync(HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyLocalVariableRename: action 11");
        await TestServices.EditorVerifier.TextEqualsAsync("""
                using System;
                using System.Collections.Generic;
                using System.Linq;

                class Program
                {
                    static void Main(string[] args)
                    {
                        int y$$ = 0;
                        y = 5;
                        TestMethod(y);
                    }

                    static void TestMethod(int y)
                    {

                    }
                }
                """, HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyLocalVariableRename: action 12");
        await telemetry.VerifyFiredAsync(["vs/ide/vbcs/rename/inlinesession/session", "vs/ide/vbcs/rename/commitcore"], HangMitigatingCancellationToken);
    }

    [IdeFact, WorkItem("https://github.com/dotnet/roslyn/issues/21657")]
    public async Task VerifyAttributeRename()
    {
        var markup = """
            using System;

            class [|$$ustom|]Attribute : Attribute
            {
            }
            """;
        await SetUpEditorAsync(markup, HangMitigatingCancellationToken);
        await TestServices.InlineRename.InvokeAsync(HangMitigatingCancellationToken);

        MarkupTestFile.GetSpans(markup, out var _, out var renameSpans);
        var tags = await TestServices.Editor.GetRenameTagsAsync(HangMitigatingCancellationToken);
        var tagSpans = tags.SelectAsArray(tag => new TextSpan(tag.Span.Start, tag.Span.Length));
        AssertEx.SetEqual(renameSpans, tagSpans);

        await TestServices.Input.SendWithoutActivateAsync(["Custom", VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForRenameAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextEqualsAsync("""
            using System;

            class Custom$$Attribute : Attribute
            {
            }
            """, HangMitigatingCancellationToken);
    }

    [IdeFact, WorkItem("https://github.com/dotnet/roslyn/issues/21657")]
    public async Task VerifyAttributeRenameWhileRenameClasss()
    {
        _output.WriteLine("CSharpRename.VerifyAttributeRenameWhileRenameClasss: action 1");
        var markup = """
            using System;

            class [|$$stom|]Attribute : Attribute
            {
            }
            """;
        _output.WriteLine("CSharpRename.VerifyAttributeRenameWhileRenameClasss: action 2");
        await SetUpEditorAsync(markup, HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyAttributeRenameWhileRenameClasss: action 3");
        await TestServices.InlineRename.InvokeAsync(HangMitigatingCancellationToken);

        _output.WriteLine("CSharpRename.VerifyAttributeRenameWhileRenameClasss: action 4");
        MarkupTestFile.GetSpans(markup, out var _, out var renameSpans);
        _output.WriteLine("CSharpRename.VerifyAttributeRenameWhileRenameClasss: action 5");
        var tags = await TestServices.Editor.GetRenameTagsAsync(HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyAttributeRenameWhileRenameClasss: action 6");
        var tagSpans = tags.SelectAsArray(tag => new TextSpan(tag.Span.Start, tag.Span.Length));
        _output.WriteLine("CSharpRename.VerifyAttributeRenameWhileRenameClasss: action 7");
        AssertEx.SetEqual(renameSpans, tagSpans);

        _output.WriteLine("CSharpRename.VerifyAttributeRenameWhileRenameClasss: action 8");
        await TestServices.Input.SendWithoutActivateAsync("Custom", HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyAttributeRenameWhileRenameClasss: action 9");
        await TestServices.Input.SendWithoutActivateAsync([VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyAttributeRenameWhileRenameClasss: action 10");
        await TestServices.Workspace.WaitForRenameAsync(HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyAttributeRenameWhileRenameClasss: action 11");
        await TestServices.EditorVerifier.TextEqualsAsync("""
            using System;

            class Custom$$Attribute : Attribute
            {
            }
            """, HangMitigatingCancellationToken);
    }

    [IdeFact, WorkItem("https://github.com/dotnet/roslyn/issues/21657")]
    public async Task VerifyAttributeRenameWhileRenameAttribute()
    {
        var markup = """
            using System;

            [[|$$stom|]]
            class Bar 
            {
            }

            class [|stom|]Attribute : Attribute
            {
            }
            """;
        await SetUpEditorAsync(markup, HangMitigatingCancellationToken);
        await TestServices.InlineRename.InvokeAsync(HangMitigatingCancellationToken);

        MarkupTestFile.GetSpans(markup, out _, out var renameSpans);
        var tags = await TestServices.Editor.GetRenameTagsAsync(HangMitigatingCancellationToken);
        var tagSpans = tags.SelectAsArray(tag => new TextSpan(tag.Span.Start, tag.Span.Length));
        AssertEx.SetEqual(renameSpans, tagSpans);

        await TestServices.Input.SendWithoutActivateAsync("Custom", HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync([VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForRenameAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextEqualsAsync("""
            using System;

            [Custom$$]
            class Bar 
            {
            }

            class CustomAttribute : Attribute
            {
            }
            """, HangMitigatingCancellationToken);
    }

    [IdeFact, WorkItem("https://github.com/dotnet/roslyn/issues/21657")]
    public async Task VerifyAttributeRenameWhileRenameAttributeClass()
    {
        _output.WriteLine("CSharpRename.VerifyAttributeRenameWhileRenameAttributeClass: action 1");
        var markup = """
            using System;

            [[|stom|]]
            class Bar 
            {
            }

            class [|$$stom|]Attribute : Attribute
            {
            }
            """;
        _output.WriteLine("CSharpRename.VerifyAttributeRenameWhileRenameAttributeClass: action 2");
        await SetUpEditorAsync(markup, HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyAttributeRenameWhileRenameAttributeClass: action 3");
        await TestServices.InlineRename.InvokeAsync(HangMitigatingCancellationToken);

        _output.WriteLine("CSharpRename.VerifyAttributeRenameWhileRenameAttributeClass: action 4");
        MarkupTestFile.GetSpans(markup, out _, out var renameSpans);
        _output.WriteLine("CSharpRename.VerifyAttributeRenameWhileRenameAttributeClass: action 5");
        var tags = await TestServices.Editor.GetRenameTagsAsync(HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyAttributeRenameWhileRenameAttributeClass: action 6");
        var tagSpans = tags.SelectAsArray(tag => new TextSpan(tag.Span.Start, tag.Span.Length));
        _output.WriteLine("CSharpRename.VerifyAttributeRenameWhileRenameAttributeClass: action 7");
        AssertEx.SetEqual(renameSpans, tagSpans);

        _output.WriteLine("CSharpRename.VerifyAttributeRenameWhileRenameAttributeClass: action 8");
        await TestServices.Input.SendWithoutActivateAsync("Custom", HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyAttributeRenameWhileRenameAttributeClass: action 9");
        await TestServices.Input.SendWithoutActivateAsync([VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyAttributeRenameWhileRenameAttributeClass: action 10");
        await TestServices.Workspace.WaitForRenameAsync(HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyAttributeRenameWhileRenameAttributeClass: action 11");
        await TestServices.EditorVerifier.TextEqualsAsync("""
            using System;

            [Custom]
            class Bar 
            {
            }

            class Custom$$Attribute : Attribute
            {
            }
            """, HangMitigatingCancellationToken);
    }

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/63576")]
    public async Task VerifyLocalVariableRenameWithCommentsUpdated()
    {
        // "variable" is intentionally misspelled as "varixable" and "this" is misspelled as
        // "thix" below to ensure we don't change instances of "x" in comments that are part of
        // larger words
        var markup = """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                /// <summary>
                /// creates a varixable named [|x|] xx
                /// </summary>
                /// <param name="args"></param>
                static void Main(string[] args)
                {
                    // thix varixable is named [|x|] xx
                    int [|x|]$$ = 0;
                    [|x|] = 5;
                    TestMethod([|x|]);
                }

                static void TestMethod(int y)
                {
                    /*
                     * [|x|]
                     * xx
                     */
                }
            }
            """;
        await SetUpEditorAsync(markup, HangMitigatingCancellationToken);
        await TestServices.InlineRename.InvokeAsync(HangMitigatingCancellationToken);
        await TestServices.InlineRename.ToggleIncludeCommentsAsync(HangMitigatingCancellationToken);

        MarkupTestFile.GetSpans(markup, out var _, out var renameSpans);
        var tags = await TestServices.Editor.GetRenameTagsAsync(HangMitigatingCancellationToken);
        var tagSpans = tags.SelectAsArray(tag => new TextSpan(tag.Span.Start, tag.Span.Length));
        AssertEx.SetEqual(renameSpans, tagSpans);

        await TestServices.Input.SendWithoutActivateAsync([VirtualKeyCode.VK_Y, VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForRenameAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextEqualsAsync("""
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                /// <summary>
                /// creates a varixable named y xx
                /// </summary>
                /// <param name="args"></param>
                static void Main(string[] args)
                {
                    // thix varixable is named y xx
                    int y$$ = 0;
                    y = 5;
                    TestMethod(y);
                }

                static void TestMethod(int y)
                {
                    /*
                     * y
                     * xx
                     */
                }
            }
            """, HangMitigatingCancellationToken);
    }

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/63576")]
    public async Task VerifyLocalVariableRenameWithStringsUpdated()
    {
        var markup = """
            class Program
            {
                static void Main(string[] args)
                {
                    int [|x|]$$ = 0;
                    [|x|] = 5;
                    var s = "[|x|] xx [|x|]";
                    var sLiteral = 
                        @"
                        [|x|]
                        xx
                        [|x|]
                        ";
                    char c = 'x';
                    char cUnit = '\u0078';
                }
            }
            """;
        await SetUpEditorAsync(markup, HangMitigatingCancellationToken);

        await TestServices.InlineRename.InvokeAsync(HangMitigatingCancellationToken);
        await TestServices.InlineRename.ToggleIncludeStringsAsync(HangMitigatingCancellationToken);

        MarkupTestFile.GetSpans(markup, out var _, out var renameSpans);
        var tags = await TestServices.Editor.GetRenameTagsAsync(HangMitigatingCancellationToken);
        var tagSpans = tags.SelectAsArray(tag => new TextSpan(tag.Span.Start, tag.Span.Length));
        AssertEx.SetEqual(renameSpans, tagSpans);

        await TestServices.Input.SendWithoutActivateAsync([VirtualKeyCode.VK_Y, VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForRenameAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextEqualsAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    int y$$ = 0;
                    y = 5;
                    var s = "y xx y";
                    var sLiteral = 
                        @"
                        y
                        xx
                        y
                        ";
                    char c = 'x';
                    char cUnit = '\u0078';
                }
            }
            """, HangMitigatingCancellationToken);
    }

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/63576")]
    public async Task VerifyOverloadsUpdated()
    {
        var markup = """
            interface I
            {
                void [|TestMethod|]$$(int y);
                void [|TestMethod|](string y);
            }

            class B : I
            {
                public virtual void [|TestMethod|](int y)
                { }

                public virtual void [|TestMethod|](string y)
                { }
            }
            """;
        await SetUpEditorAsync(markup, HangMitigatingCancellationToken);

        await TestServices.InlineRename.InvokeAsync(HangMitigatingCancellationToken);
        await TestServices.InlineRename.ToggleIncludeOverloadsAsync(HangMitigatingCancellationToken);

        MarkupTestFile.GetSpans(markup, out var _, out var renameSpans);
        var tags = await TestServices.Editor.GetRenameTagsAsync(HangMitigatingCancellationToken);
        var tagSpans = tags.SelectAsArray(tag => new TextSpan(tag.Span.Start, tag.Span.Length));
        AssertEx.SetEqual(renameSpans, tagSpans);

        await TestServices.Input.SendWithoutActivateAsync([VirtualKeyCode.VK_Y, VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForRenameAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextEqualsAsync("""
            interface I
            {
                void y$$(int y);
                void y(string y);
            }

            class B : I
            {
                public virtual void y(int y)
                { }

                public virtual void y(string y)
                { }
            }
            """, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task VerifyMultiFileRename()
    {
        _output.WriteLine("CSharpRename.VerifyMultiFileRename: action 1");
        await SetUpEditorAsync("""
            class $$Program
            {
            }
            """, HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyMultiFileRename: action 2");
        await TestServices.SolutionExplorer.AddFileAsync(ProjectName, "Class2.cs", @"", cancellationToken: HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyMultiFileRename: action 3");
        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "Class2.cs", HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyMultiFileRename: action 4");
        MarkupTestFile.GetSpans("""
            class SomeOtherClass
            {
                void M()
                {
                    [|Program|] p = new [|Program|]();
                }
            }
            """, out var code, out var renameSpans);

        _output.WriteLine("CSharpRename.VerifyMultiFileRename: action 5");
        await TestServices.Editor.SetTextAsync(code, HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyMultiFileRename: action 6");
        await TestServices.Editor.PlaceCaretAsync("Program", charsOffset: 0, HangMitigatingCancellationToken);

        _output.WriteLine("CSharpRename.VerifyMultiFileRename: action 7");
        await TestServices.InlineRename.InvokeAsync(HangMitigatingCancellationToken);

        _output.WriteLine("CSharpRename.VerifyMultiFileRename: action 8");
        var tags = await TestServices.Editor.GetRenameTagsAsync(HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyMultiFileRename: action 9");
        var tagSpans = tags.SelectAsArray(tag => new TextSpan(tag.Span.Start, tag.Span.Length));
        _output.WriteLine("CSharpRename.VerifyMultiFileRename: action 10");
        AssertEx.SetEqual(renameSpans, tagSpans);

        _output.WriteLine("CSharpRename.VerifyMultiFileRename: action 11");
        await TestServices.Input.SendWithoutActivateAsync([VirtualKeyCode.VK_Y, VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyMultiFileRename: action 12");
        await TestServices.Workspace.WaitForRenameAsync(HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyMultiFileRename: action 13");
        await TestServices.EditorVerifier.TextEqualsAsync("""
            class SomeOtherClass
            {
                void M()
                {
                    y$$ p = new y();
                }
            }
            """, HangMitigatingCancellationToken);

        _output.WriteLine("CSharpRename.VerifyMultiFileRename: action 14");
        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "Class1.cs", HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyMultiFileRename: action 15");
        await TestServices.EditorVerifier.TextEqualsAsync("""
            class y$$
            {
            }
            """, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task VerifyRenameCancellation()
    {
        _output.WriteLine("CSharpRename.VerifyRenameCancellation: action 1");
        await SetUpEditorAsync("""
            class $$Program
            {
            }
            """, HangMitigatingCancellationToken);

        _output.WriteLine("CSharpRename.VerifyRenameCancellation: action 2");
        await TestServices.SolutionExplorer.AddFileAsync(ProjectName, "Class2.cs", @"", cancellationToken: HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyRenameCancellation: action 3");
        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "Class2.cs", HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyRenameCancellation: action 4");
        await TestServices.Editor.SetTextAsync("""
            class SomeOtherClass
            {
                void M()
                {
                    Program p = new Program();
                }
            }
            """, HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyRenameCancellation: action 5");
        await TestServices.Editor.PlaceCaretAsync("Program", charsOffset: 0, HangMitigatingCancellationToken);

        _output.WriteLine("CSharpRename.VerifyRenameCancellation: action 6");
        await TestServices.InlineRename.InvokeAsync(HangMitigatingCancellationToken);

        _output.WriteLine("CSharpRename.VerifyRenameCancellation: action 7");
        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.VK_Y, HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyRenameCancellation: action 8");
        await TestServices.Workspace.WaitForRenameAsync(HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyRenameCancellation: action 9");
        await TestServices.EditorVerifier.TextEqualsAsync("""
            class SomeOtherClass
            {
                void M()
                {
                    y$$ p = new y();
                }
            }
            """, HangMitigatingCancellationToken);

        _output.WriteLine("CSharpRename.VerifyRenameCancellation: action 10");
        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "Class1.cs", HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyRenameCancellation: action 11");
        await TestServices.EditorVerifier.TextEqualsAsync("""
            class y$$
            {
            }
            """, HangMitigatingCancellationToken);

        _output.WriteLine("CSharpRename.VerifyRenameCancellation: action 12");
        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.ESCAPE, HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyRenameCancellation: action 13");
        await TestServices.Workspace.WaitForRenameAsync(HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyRenameCancellation: action 14");
        await TestServices.EditorVerifier.TextEqualsAsync("""
            class Program$$
            {
            }
            """, HangMitigatingCancellationToken);

        _output.WriteLine("CSharpRename.VerifyRenameCancellation: action 15");
        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "Class2.cs", HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyRenameCancellation: action 16");
        await TestServices.EditorVerifier.TextEqualsAsync("""
            class SomeOtherClass
            {
                void M()
                {
                    Program$$ p = new Program();
                }
            }
            """, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task VerifyCrossProjectRename()
    {
        _output.WriteLine("CSharpRename.VerifyCrossProjectRename: action 1");
        await SetUpEditorAsync("""
            $$class RenameRocks 
            {
                static void Main(string[] args)
                {
                    Class2 c = null;
                    c.ToString();
                }
            }
            """, HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyCrossProjectRename: action 2");
        var project1 = ProjectName;
        _output.WriteLine("CSharpRename.VerifyCrossProjectRename: action 3");
        var project2 = "Project2";

        _output.WriteLine("CSharpRename.VerifyCrossProjectRename: action 4");
        await TestServices.SolutionExplorer.AddProjectAsync(project2, WellKnownProjectTemplates.ClassLibrary, LanguageName, HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyCrossProjectRename: action 5");
        await TestServices.SolutionExplorer.AddProjectReferenceAsync(projectName: project1, projectToReferenceName: project2, HangMitigatingCancellationToken);

        _output.WriteLine("CSharpRename.VerifyCrossProjectRename: action 6");
        await TestServices.SolutionExplorer.AddFileAsync(project2, "Class2.cs", @"", cancellationToken: HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyCrossProjectRename: action 7");
        await TestServices.SolutionExplorer.OpenFileAsync(project2, "Class2.cs", HangMitigatingCancellationToken);

        _output.WriteLine("CSharpRename.VerifyCrossProjectRename: action 8");
        await TestServices.Editor.SetTextAsync("""

            public class Class2 { static void Main(string [] args) { } }
            """, HangMitigatingCancellationToken);

        _output.WriteLine("CSharpRename.VerifyCrossProjectRename: action 9");
        await TestServices.SolutionExplorer.OpenFileAsync(project1, "Class1.cs", HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyCrossProjectRename: action 10");
        await TestServices.Editor.PlaceCaretAsync("Class2", charsOffset: 0, HangMitigatingCancellationToken);

        _output.WriteLine("CSharpRename.VerifyCrossProjectRename: action 11");
        await TestServices.InlineRename.InvokeAsync(HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyCrossProjectRename: action 12");
        await TestServices.Input.SendWithoutActivateAsync([VirtualKeyCode.VK_Y, VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyCrossProjectRename: action 13");
        await TestServices.Workspace.WaitForRenameAsync(HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyCrossProjectRename: action 14");
        await TestServices.EditorVerifier.TextEqualsAsync("""
            class RenameRocks 
            {
                static void Main(string[] args)
                {
                    y$$ c = null;
                    c.ToString();
                }
            }
            """, HangMitigatingCancellationToken);

        _output.WriteLine("CSharpRename.VerifyCrossProjectRename: action 15");
        await TestServices.SolutionExplorer.OpenFileAsync(project2, "y.cs", HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyCrossProjectRename: action 16");
        await TestServices.EditorVerifier.TextEqualsAsync("""

            public class y { static void Main(string [] args) { } }$$
            """, cancellationToken: HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task VerifyRenameUndo()
    {
        _output.WriteLine("CSharpRename.VerifyRenameUndo: action 1");
        await VerifyCrossProjectRename();

        _output.WriteLine("CSharpRename.VerifyRenameUndo: action 2");
        await TestServices.Input.SendWithoutActivateAsync((VirtualKeyCode.VK_Z, VirtualKeyCode.CONTROL), HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyRenameUndo: action 3");
        await TestServices.Workspace.WaitForRenameAsync(HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyRenameUndo: action 4");
        await TestServices.EditorVerifier.TextEqualsAsync("""

            public class Class2 { static void Main(string [] args) { } }$$
            """, HangMitigatingCancellationToken);

        _output.WriteLine("CSharpRename.VerifyRenameUndo: action 5");
        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "Class1.cs", HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyRenameUndo: action 6");
        await TestServices.EditorVerifier.TextEqualsAsync("""
            class RenameRocks 
            {
                static void Main(string[] args)
                {
                    Class2$$ c = null;
                    c.ToString();
                }
            }
            """, HangMitigatingCancellationToken);
    }

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/63576")]
    public async Task VerifyRenameInStandaloneFiles()
    {
        await TestServices.SolutionExplorer.CloseSolutionAsync(HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.AddStandaloneFileAsync("StandaloneFile1.cs", HangMitigatingCancellationToken);
        await TestServices.Editor.SetTextAsync("""
            class Program
            {
                void Goo()
                {
                    var ids = 1;
                    ids = 2;
                }
            }
            """, HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("ids", charsOffset: 0, HangMitigatingCancellationToken);

        await TestServices.InlineRename.InvokeAsync(HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync([VirtualKeyCode.VK_Y, VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForRenameAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextEqualsAsync("""
            class Program
            {
                void Goo()
                {
                    var y$$ = 1;
                    y = 2;
                }
            }
            """, HangMitigatingCancellationToken);
    }

    [IdeFact, WorkItem("https://github.com/dotnet/roslyn/issues/39617")]
    public async Task VerifyRenameCaseChange()
    {
        _output.WriteLine("CSharpRename.VerifyRenameCaseChange: action 1");
        await TestServices.SolutionExplorer.AddFileAsync(ProjectName, "Program.cs",
            """
            class Program
            {
                static void Main(string[] args)
                {
                }
            }
            """, cancellationToken: HangMitigatingCancellationToken);

        _output.WriteLine("CSharpRename.VerifyRenameCaseChange: action 2");
        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "Program.cs", HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyRenameCaseChange: action 3");
        await TestServices.Editor.PlaceCaretAsync("Program", charsOffset: 0, HangMitigatingCancellationToken);

        _output.WriteLine("CSharpRename.VerifyRenameCaseChange: action 4");
        await TestServices.InlineRename.InvokeAsync(HangMitigatingCancellationToken);

        _output.WriteLine("CSharpRename.VerifyRenameCaseChange: action 5");
        await TestServices.Input.SendWithoutActivateAsync([VirtualKeyCode.HOME, VirtualKeyCode.DELETE, VirtualKeyCode.VK_P, VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyRenameCaseChange: action 6");
        await TestServices.Workspace.WaitForRenameAsync(HangMitigatingCancellationToken);

        _output.WriteLine("CSharpRename.VerifyRenameCaseChange: action 7");
        await TestServices.EditorVerifier.TextEqualsAsync(
            """
            class p$$rogram
            {
                static void Main(string[] args)
                {
                }
            }
            """, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task VerifyTextSync()
    {
        _output.WriteLine("CSharpRename.VerifyTextSync: action 1");
        await TestServices.SolutionExplorer.AddFileAsync(ProjectName, "Program.cs",
            """
            public class Class2
            {
                public int Field123;
            }
            """, cancellationToken: HangMitigatingCancellationToken);

        _output.WriteLine("CSharpRename.VerifyTextSync: action 2");
        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "Program.cs", HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyTextSync: action 3");
        await TestServices.Editor.PlaceCaretAsync("Field123", charsOffset: 0, HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyTextSync: action 4");
        await TestServices.InlineRename.InvokeAsync(HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyTextSync: action 5");
        await TestServices.Input.SendWithoutActivateAsync(["F", "i"], HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyTextSync: action 6");
        await TestServices.Workspace.WaitForRenameAsync(HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyTextSync: action 7");
        await TestServices.EditorVerifier.TextEqualsAsync(
            """
            public class Class2
            {
                public int Fi$$;
            }
            """, HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyTextSync: action 8");
        await TestServices.InlineRename.VerifyStringInFlyout("Fi", HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyTextSync: action 9");
        await TestServices.Input.SendWithoutActivateAsync(["e", "l", "d", "3", "2", "1"], HangMitigatingCancellationToken);

        _output.WriteLine("CSharpRename.VerifyTextSync: action 10");
        await TestServices.Workspace.WaitForRenameAsync(HangMitigatingCancellationToken);

        _output.WriteLine("CSharpRename.VerifyTextSync: action 11");
        await TestServices.EditorVerifier.TextEqualsAsync(
            """
            public class Class2
            {
                public int Field321$$;
            }
            """, HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyTextSync: action 12");
        await TestServices.InlineRename.VerifyStringInFlyout("Field321", HangMitigatingCancellationToken);
    }

    [IdeFact, WorkItem("https://github.com/dotnet/roslyn/issues/68374")]
    public async Task VerifySelectionAsync()
    {
        _output.WriteLine("CSharpRename.VerifySelectionAsync: action 1");
        await TestServices.SolutionExplorer.AddFileAsync(ProjectName, "Program.cs",
            """
            public class Class2
            {
                public int LongLongField;
            }
            """, cancellationToken: HangMitigatingCancellationToken);

        _output.WriteLine("CSharpRename.VerifySelectionAsync: action 2");
        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "Program.cs", HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifySelectionAsync: action 3");
        await TestServices.Editor.PlaceCaretAsync("LongLongField", charsOffset: 0, HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifySelectionAsync: action 4");
        await TestServices.InlineRename.InvokeAsync(HangMitigatingCancellationToken);

        _output.WriteLine("CSharpRename.VerifySelectionAsync: action 5");
        await TestServices.Editor.SendExplicitFocusAsync(HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifySelectionAsync: action 6");
        await TestServices.Editor.PlaceCaretAsync("LongLongField", charsOffset: "Long".Length, HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifySelectionAsync: action 7");
        MarkupTestFile.GetPositionAndSpans("""
            public class Class2
            {
                public int Long{|selection:Long|}Field;
            }
            """, out var _, out int? _, out var spans);
        _output.WriteLine("CSharpRename.VerifySelectionAsync: action 8");
        var selectedSpan = spans["selection"].Single();
        _output.WriteLine("CSharpRename.VerifySelectionAsync: action 9");
        await TestServices.Editor.SetSelectionAsync(selectedSpan, HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifySelectionAsync: action 10");
        await TestServices.Input.SendWithoutActivateAsync(
            new InputKey(VirtualKeyCode.BACK, []), HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifySelectionAsync: action 11");
        await TestServices.Input.SendWithoutActivateAsync(["Other", "Stuff"], HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifySelectionAsync: action 12");
        await TestServices.EditorVerifier.TextEqualsAsync(
            """
            public class Class2
            {
                public int LongOtherStuff$$Field;
            }
            """, HangMitigatingCancellationToken);
    }

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/73630"), WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1903953/")]
    public async Task VerifyRenameLinkedDocumentsAsync()
    {
        var projectName = "MultiTFMProject";
        await TestServices.SolutionExplorer.AddCustomProjectAsync(projectName, ".csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFrameworks>net6.0-windows;net48</TargetFrameworks>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <LangVersion>preview</LangVersion>
              </PropertyGroup>
            </Project>
            """, HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.AddFileAsync(projectName, "TestClass.cs", """
            public class TestClass
            {
            }
            """, cancellationToken: HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.AddFileAsync(projectName, "MyClass.cs", """
            public class MyClass
            {
                void Method()
                {
                    TestClass x = new TestClass();
                }
            }
            """, cancellationToken: HangMitigatingCancellationToken);
        // We made csproj changes, so need to wait for PS to finish all the tasks before moving on.
        await TestServices.Workspace.WaitForProjectSystemAsync(HangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.OpenFileAsync(projectName, "TestClass.cs", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.OpenFileAsync(projectName, "MyClass.cs", HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("TestClass", charsOffset: 0, HangMitigatingCancellationToken);
        await TestServices.InlineRename.InvokeAsync(HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync([VirtualKeyCode.HOME, "M", "y", VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForRenameAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextEqualsAsync(
            """
            public class MyClass
            {
                void Method()
                {
                    MyTestClass$$ x = new MyTestClass();
                }
            }
            """, HangMitigatingCancellationToken);
        // Make sure the file is renamed. If the file is not found, this call would throw exception
        await TestServices.SolutionExplorer.GetProjectItemAsync(projectName, "MyTestClass.cs", HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task VerifyAsyncRename()
    {
        _output.WriteLine("CSharpRename.VerifyAsyncRename: action 1");
        await SetUpEditorAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    int x = 100;
                    Te$$stMethod(x);
                }

                static void TestMethod(int y)
                {

                }
            }
            """, HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyAsyncRename: action 2");
        await TestServices.InlineRename.InvokeAsync(HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyAsyncRename: action 3");
        await TestServices.Input.SendWithoutActivateAsync(["AsyncRenameMethod", VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyAsyncRename: action 4");
        await TestServices.Workspace.WaitForRenameAsync(HangMitigatingCancellationToken);
        _output.WriteLine("CSharpRename.VerifyAsyncRename: action 5");
        await TestServices.EditorVerifier.TextEqualsAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    int x = 100;
                    AsyncRenameMethod$$(x);
                }

                static void AsyncRenameMethod(int y)
                {

                }
            }
            """, HangMitigatingCancellationToken);
    }
}
