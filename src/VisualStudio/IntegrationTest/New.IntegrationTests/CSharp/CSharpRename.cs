// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.InlineRename;
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

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

[Trait(Traits.Feature, Traits.Features.Rename)]
public class CSharpRename : AbstractEditorTest
{
    protected override string LanguageName => LanguageNames.CSharp;

    public CSharpRename()
        : base(nameof(CSharpRename))
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // reset relevant global options to default values:
        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        globalOptions.SetGlobalOption(InlineRenameUIOptionsStorage.UseInlineAdornment, false);
        globalOptions.SetGlobalOption(InlineRenameSessionOptionsStorage.RenameInComments, false);
        globalOptions.SetGlobalOption(InlineRenameSessionOptionsStorage.RenameInStrings, false);
        globalOptions.SetGlobalOption(InlineRenameSessionOptionsStorage.RenameOverloads, false);
        globalOptions.SetGlobalOption(InlineRenameSessionOptionsStorage.RenameFile, true);
        globalOptions.SetGlobalOption(InlineRenameSessionOptionsStorage.PreviewChanges, false);
    }

    [IdeFact]
    public async Task VerifyLocalVariableRename()
    {
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
        await using var telemetry = await TestServices.Telemetry.EnableTestTelemetryChannelAsync(HangMitigatingCancellationToken);
        await SetUpEditorAsync(markup, HangMitigatingCancellationToken);
        await TestServices.InlineRename.InvokeAsync(HangMitigatingCancellationToken);

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
        var markup = """
            using System;

            class [|$$stom|]Attribute : Attribute
            {
            }
            """;
        await SetUpEditorAsync(markup, HangMitigatingCancellationToken);
        await TestServices.InlineRename.InvokeAsync(HangMitigatingCancellationToken);

        MarkupTestFile.GetSpans(markup, out var _, out var renameSpans);
        var tags = await TestServices.Editor.GetRenameTagsAsync(HangMitigatingCancellationToken);
        var tagSpans = tags.SelectAsArray(tag => new TextSpan(tag.Span.Start, tag.Span.Length));
        AssertEx.SetEqual(renameSpans, tagSpans);

        await TestServices.Input.SendWithoutActivateAsync("Custom", HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForRenameAsync(HangMitigatingCancellationToken);
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
        await SetUpEditorAsync(markup, HangMitigatingCancellationToken);
        await TestServices.InlineRename.InvokeAsync(HangMitigatingCancellationToken);

        MarkupTestFile.GetSpans(markup, out _, out var renameSpans);
        var tags = await TestServices.Editor.GetRenameTagsAsync(HangMitigatingCancellationToken);
        var tagSpans = tags.SelectAsArray(tag => new TextSpan(tag.Span.Start, tag.Span.Length));
        AssertEx.SetEqual(renameSpans, tagSpans);

        await TestServices.Input.SendWithoutActivateAsync("Custom", HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForRenameAsync(HangMitigatingCancellationToken);
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

    [IdeFact]
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

    [IdeFact]
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

    [IdeFact]
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
        await SetUpEditorAsync("""
            class $$Program
            {
            }
            """, HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.AddFileAsync(ProjectName, "Class2.cs", @"", cancellationToken: HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "Class2.cs", HangMitigatingCancellationToken);

        const string class2Markup = """
            class SomeOtherClass
            {
                void M()
                {
                    [|Program|] p = new [|Program|]();
                }
            }
            """;
        MarkupTestFile.GetSpans(class2Markup, out var code, out var renameSpans);

        await TestServices.Editor.SetTextAsync(code, HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("Program", charsOffset: 0, HangMitigatingCancellationToken);

        await TestServices.InlineRename.InvokeAsync(HangMitigatingCancellationToken);

        var tags = await TestServices.Editor.GetRenameTagsAsync(HangMitigatingCancellationToken);
        var tagSpans = tags.SelectAsArray(tag => new TextSpan(tag.Span.Start, tag.Span.Length));
        AssertEx.SetEqual(renameSpans, tagSpans);

        await TestServices.Input.SendWithoutActivateAsync([VirtualKeyCode.VK_Y, VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForRenameAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextEqualsAsync("""
            class SomeOtherClass
            {
                void M()
                {
                    y$$ p = new y();
                }
            }
            """, HangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "Class1.cs", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextEqualsAsync("""
            class y$$
            {
            }
            """, HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task VerifyRenameCancellation()
    {
        await SetUpEditorAsync("""
            class $$Program
            {
            }
            """, HangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.AddFileAsync(ProjectName, "Class2.cs", @"", cancellationToken: HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "Class2.cs", HangMitigatingCancellationToken);
        await TestServices.Editor.SetTextAsync("""
            class SomeOtherClass
            {
                void M()
                {
                    Program p = new Program();
                }
            }
            """, HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("Program", charsOffset: 0, HangMitigatingCancellationToken);

        await TestServices.InlineRename.InvokeAsync(HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.VK_Y, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForRenameAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextEqualsAsync("""
            class SomeOtherClass
            {
                void M()
                {
                    y$$ p = new y();
                }
            }
            """, HangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "Class1.cs", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextEqualsAsync("""
            class y$$
            {
            }
            """, HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync(VirtualKeyCode.ESCAPE, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForRenameAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextEqualsAsync("""
            class Program$$
            {
            }
            """, HangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "Class2.cs", HangMitigatingCancellationToken);
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
        var project1 = ProjectName;
        var project2 = "Project2";

        await TestServices.SolutionExplorer.AddProjectAsync(project2, WellKnownProjectTemplates.ClassLibrary, LanguageName, HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.AddProjectReferenceAsync(projectName: project1, projectToReferenceName: project2, HangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.AddFileAsync(project2, "Class2.cs", @"", cancellationToken: HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.OpenFileAsync(project2, "Class2.cs", HangMitigatingCancellationToken);

        await TestServices.Editor.SetTextAsync(@"
public class Class2 { static void Main(string [] args) { } }", HangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.OpenFileAsync(project1, "Class1.cs", HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("Class2", charsOffset: 0, HangMitigatingCancellationToken);

        await TestServices.InlineRename.InvokeAsync(HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync([VirtualKeyCode.VK_Y, VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForRenameAsync(HangMitigatingCancellationToken);
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

        await TestServices.SolutionExplorer.OpenFileAsync(project2, "y.cs", HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextEqualsAsync(@"
public class y { static void Main(string [] args) { } }$$", cancellationToken: HangMitigatingCancellationToken);
    }

    [IdeFact]
    public async Task VerifyRenameUndo()
    {
        await VerifyCrossProjectRename();

        await TestServices.Input.SendWithoutActivateAsync((VirtualKeyCode.VK_Z, VirtualKeyCode.CONTROL), HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForRenameAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextEqualsAsync(@"
public class Class2 { static void Main(string [] args) { } }$$", HangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "Class1.cs", HangMitigatingCancellationToken);
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

    [IdeFact]
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
        await TestServices.SolutionExplorer.AddFileAsync(ProjectName, "Program.cs",
            """
            class Program
            {
                static void Main(string[] args)
                {
                }
            }
            """, cancellationToken: HangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "Program.cs", HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("Program", charsOffset: 0, HangMitigatingCancellationToken);

        await TestServices.InlineRename.InvokeAsync(HangMitigatingCancellationToken);

        await TestServices.Input.SendWithoutActivateAsync([VirtualKeyCode.HOME, VirtualKeyCode.DELETE, VirtualKeyCode.VK_P, VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForRenameAsync(HangMitigatingCancellationToken);

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
        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        globalOptions.SetGlobalOption(InlineRenameUIOptionsStorage.UseInlineAdornment, true);
        await TestServices.SolutionExplorer.AddFileAsync(ProjectName, "Program.cs",
            """
            public class Class2
            {
                public int Field123;
            }
            """, cancellationToken: HangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "Program.cs", HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("Field123", charsOffset: 0, HangMitigatingCancellationToken);
        await TestServices.InlineRename.InvokeAsync(HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync(["F", "i"], HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForRenameAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextEqualsAsync(
            """
            public class Class2
            {
                public int Fi$$;
            }
            """, HangMitigatingCancellationToken);
        await TestServices.InlineRename.VerifyStringInFlyout("Fi", HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync(["e", "l", "d", "3", "2", "1"], HangMitigatingCancellationToken);

        await TestServices.Workspace.WaitForRenameAsync(HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.TextEqualsAsync(
            """
            public class Class2
            {
                public int Field321$$;
            }
            """, HangMitigatingCancellationToken);
        await TestServices.InlineRename.VerifyStringInFlyout("Field321", HangMitigatingCancellationToken);
    }

    [IdeFact, WorkItem("https://github.com/dotnet/roslyn/issues/68374")]
    public async Task VerifySelectionAsync()
    {
        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        globalOptions.SetGlobalOption(InlineRenameUIOptionsStorage.UseInlineAdornment, true);
        var startCode = """
            public class Class2
            {
                public int LongLongField;
            }
            """;
        await TestServices.SolutionExplorer.AddFileAsync(ProjectName, "Program.cs",
            startCode, cancellationToken: HangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "Program.cs", HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("LongLongField", charsOffset: 0, HangMitigatingCancellationToken);
        await TestServices.InlineRename.InvokeAsync(HangMitigatingCancellationToken);

        await TestServices.Editor.SendExplicitFocusAsync(HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("LongLongField", charsOffset: "Long".Length, HangMitigatingCancellationToken);

        var markedCode = """
            public class Class2
            {
                public int Long{|selection:Long|}Field;
            }
            """;
        MarkupTestFile.GetPositionAndSpans(markedCode, out var _, out int? _, out var spans);
        var selectedSpan = spans["selection"].Single();
        await TestServices.Editor.SetSelectionAsync(selectedSpan, HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync(
            new InputKey(VirtualKeyCode.BACK, ImmutableArray<VirtualKeyCode>.Empty), HangMitigatingCancellationToken);
        await TestServices.Input.SendWithoutActivateAsync(["Other", "Stuff"], HangMitigatingCancellationToken);
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
        var globalOptions = await TestServices.Shell.GetComponentModelServiceAsync<IGlobalOptionService>(HangMitigatingCancellationToken);
        globalOptions.SetGlobalOption(InlineRenameUIOptionsStorage.UseInlineAdornment, true);
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

        var startCode = """
            public class TestClass
            {
            }
            """;
        await TestServices.SolutionExplorer.AddFileAsync(projectName, "TestClass.cs", startCode, cancellationToken: HangMitigatingCancellationToken);

        var referencedCode = """
            public class MyClass
            {
                void Method()
                {
                    TestClass x = new TestClass();
                }
            }
            """;
        await TestServices.SolutionExplorer.AddFileAsync(projectName, "MyClass.cs", referencedCode, cancellationToken: HangMitigatingCancellationToken);
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
}
