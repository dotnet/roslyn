// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.InlineRename;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using WindowsInput.Native;
using Xunit;
using Xunit.Sdk;

namespace Roslyn.VisualStudio.NewIntegrationTests.VisualBasic;

[Trait(Traits.Feature, Traits.Features.Rename)]
public sealed class BasicRename() : AbstractEditorTest(nameof(BasicRename))
{
    protected override string LanguageName => LanguageNames.VisualBasic;

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
        var markup = """

            Imports System
            Imports System.Collections.Generic
            Imports System.Linq

            Module Program
                Sub Main(args As String())
                    Dim [|x|]$$ As Integer = 0
                    [|x|] = 5
                    TestMethod([|x|])
                End Sub
                Sub TestMethod(y As Integer)

                End Sub
            End Module
            """;
        await SetUpEditorAsync(markup, HangMitigatingCancellationToken);
        await TestServices.InlineRename.InvokeAsync(HangMitigatingCancellationToken);

        MarkupTestFile.GetSpans(markup, out var _, out var renameSpans);
        var tags = await TestServices.Editor.GetRenameTagsAsync(HangMitigatingCancellationToken);
        var tagSpans = tags.SelectAsArray(tag => new TextSpan(tag.Span.Start, tag.Span.Length));
        AssertEx.SetEqual(renameSpans, tagSpans);

        await TestServices.Input.SendWithoutActivateAsync([VirtualKeyCode.VK_Y, VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForRenameAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextEqualsAsync("""

            Imports System
            Imports System.Collections.Generic
            Imports System.Linq

            Module Program
                Sub Main(args As String())
                    Dim y$$ As Integer = 0
                    y = 5
                    TestMethod(y)
                End Sub
                Sub TestMethod(y As Integer)

                End Sub
            End Module
            """, HangMitigatingCancellationToken);
    }

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/63576")]
    public async Task VerifyLocalVariableRenameWithCommentsUpdated()
    {
        // "variable" is intentionally misspelled as "varixable" and "this" is misspelled as
        // "thix" below to ensure we don't change instances of "x" in comments that are part of
        // larger words
        var markup = """

            Imports System
            Imports System.Collections.Generic
            Imports System.Linq

            Module Program
                ''' <summary>
                ''' creates a varixable named [|x|] xx
                ''' </summary>
                ''' <param name="args"></param>
                Sub Main(args As String())
                    ' thix varixable is named [|x|] xx
                    Dim [|x|]$$ As Integer = 0
                    [|x|] = 5
                    TestMethod([|x|])
            End Module
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

            Imports System
            Imports System.Collections.Generic
            Imports System.Linq

            Module Program
                ''' <summary>
                ''' creates a varixable named y xx
                ''' </summary>
                ''' <param name="args"></param>
                Sub Main(args As String())
                    ' thix varixable is named y xx
                    Dim y$$ As Integer = 0
                    y = 5
                    TestMethod(y)
            End Module
            """, HangMitigatingCancellationToken);
    }

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/63576")]
    public async Task VerifyLocalVariableRenameWithStringsUpdated()
    {
        var markup = """

            Imports System
            Imports System.Collections.Generic
            Imports System.Linq

            Module Program
                Sub Main(args As String())
                    Dim [|x|]$$ As Integer = 0
                    [|x|] = 5
                    Dim s = "[|x|] xx [|x|]"
                End Sub
            End Module
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

            Imports System
            Imports System.Collections.Generic
            Imports System.Linq

            Module Program
                Sub Main(args As String())
                    Dim y$$ As Integer = 0
                    y = 5
                    Dim s = "y xx y"
                End Sub
            End Module
            """, HangMitigatingCancellationToken);
    }

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/63576")]
    public async Task VerifyOverloadsUpdated()
    {
        var markup = """

            Interface I
                Sub [|TestMethod|]$$(y As Integer)
                Sub [|TestMethod|](y As String)
            End Interface

            Public MustInherit Class A
                Implements I
                Public MustOverride Sub [|TestMethod|](y As Integer) Implements I.[|TestMethod|]
                Public MustOverride Sub [|TestMethod|](y As String) Implements I.[|TestMethod|]
            End Class
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

            Interface I
                Sub y$$(y As Integer)
                Sub y(y As String)
            End Interface

            Public MustInherit Class A
                Implements I
                Public MustOverride Sub y(y As Integer) Implements I.y
                Public MustOverride Sub y(y As String) Implements I.y
            End Class
            """, HangMitigatingCancellationToken);
    }

    [IdeFact, WorkItem("https://github.com/dotnet/roslyn/issues/21657")]
    public async Task VerifyAttributeRename()
    {
        var markup = """

            Imports System

            Public Class [|$$ustom|]Attribute 
                    Inherits Attribute
            End Class
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

            Imports System

            Public Class Custom$$Attribute
                Inherits Attribute
            End Class
            """, HangMitigatingCancellationToken);
    }

    [IdeFact, WorkItem("https://github.com/dotnet/roslyn/issues/21657")]
    public async Task VerifyAttributeRenameWhileRenameClasss()
    {
        var markup = """

            Imports System

            Public Class [|$$ustom|]Attribute
                Inherits Attribute
            End Class
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

            Imports System

            Public Class Custom$$Attribute
                Inherits Attribute
            End Class
            """, HangMitigatingCancellationToken);
    }

    [IdeFact, WorkItem("https://github.com/dotnet/roslyn/issues/21657")]
    public async Task VerifyAttributeRenameWhileRenameAttribute()
    {
        var markup = """

            Imports System

            <[|$$ustom|]>
            Class Bar
            End Class

            Public Class [|ustom|]Attribute
                Inherits Attribute
            End Class
            """;
        await SetUpEditorAsync(markup, HangMitigatingCancellationToken);
        await TestServices.InlineRename.InvokeAsync(HangMitigatingCancellationToken);

        MarkupTestFile.GetSpans(markup, out _, out var renameSpans);
        var tags = await TestServices.Editor.GetRenameTagsAsync(HangMitigatingCancellationToken);
        var tagSpans = tags.SelectAsArray(tag => new TextSpan(tag.Span.Start, tag.Span.Length));
        AssertEx.SetEqual(renameSpans, tagSpans);

        await TestServices.Input.SendWithoutActivateAsync(["Custom", VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForRenameAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextEqualsAsync("""

            Imports System

            <Custom$$>
            Class Bar
            End Class

            Public Class CustomAttribute
                Inherits Attribute
            End Class
            """, HangMitigatingCancellationToken);
    }

    [IdeFact, WorkItem("https://github.com/dotnet/roslyn/issues/21657")]
    public async Task VerifyAttributeRenameWhileRenameAttributeClass()
    {
        var markup = """

            Imports System

            <[|ustom|]>
            Class Bar
            End Class

            Public Class [|$$ustom|]Attribute
                Inherits Attribute
            End Class
            """;
        await SetUpEditorAsync(markup, HangMitigatingCancellationToken);
        await TestServices.InlineRename.InvokeAsync(HangMitigatingCancellationToken);

        MarkupTestFile.GetSpans(markup, out _, out var renameSpans);
        var tags = await TestServices.Editor.GetRenameTagsAsync(HangMitigatingCancellationToken);
        var tagSpans = tags.SelectAsArray(tag => new TextSpan(tag.Span.Start, tag.Span.Length));
        AssertEx.SetEqual(renameSpans, tagSpans);

        await TestServices.Input.SendWithoutActivateAsync(["Custom", VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForRenameAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextEqualsAsync("""

            Imports System

            <Custom>
            Class Bar
            End Class

            Public Class Custom$$Attribute
                Inherits Attribute
            End Class
            """, HangMitigatingCancellationToken);
    }

    [IdeFact, WorkItem("https://github.com/dotnet/roslyn/issues/21657")]
    public async Task VerifyAttributeCapitalizedRename()
    {
        var markup = """

            Imports System

            Public Class [|$$ustom|]ATTRIBUTE
                    Inherits Attribute
            End Class
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

            Imports System

            Public Class CustomAttribute$$
                Inherits Attribute
            End Class
            """, HangMitigatingCancellationToken);
    }

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/79300"), WorkItem("https://github.com/dotnet/roslyn/issues/21657")]
    public async Task VerifyAttributeNotCapitalizedRename()
    {
        var markup = """

            Imports System

            Public Class [|$$ustom|]attribute
                    Inherits Attribute
            End Class
            """;
        await SetUpEditorAsync(markup, HangMitigatingCancellationToken);
        await TestServices.InlineRename.InvokeAsync(HangMitigatingCancellationToken);

        MarkupTestFile.GetSpans(markup, out var _, out var renameSpans);
        var tags = await TestServices.Editor.GetRenameTagsAsync(HangMitigatingCancellationToken);
        var tagSpans = tags.SelectAsArray(tag => new TextSpan(tag.Span.Start, tag.Span.Length));
        AssertEx.SetEqual(renameSpans, tagSpans);

        await TestServices.Input.SendWithoutActivateAsync(["Custom", VirtualKeyCode.RETURN], HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForRenameAsync(HangMitigatingCancellationToken);
        try
        {
            // This is the expected behavior
            await TestServices.EditorVerifier.TextEqualsAsync("""

                Imports System

                Public Class CustomAttribute$$
                    Inherits Attribute
                End Class
                """, HangMitigatingCancellationToken);
        }
        catch (XunitException)
        {
            // But sometimes we get this instead
            await TestServices.EditorVerifier.TextEqualsAsync("""

                Imports System

                Public Class CustomA$$ttribute
                    Inherits Attribute
                End Class
                """, HangMitigatingCancellationToken);
        }
    }
}
