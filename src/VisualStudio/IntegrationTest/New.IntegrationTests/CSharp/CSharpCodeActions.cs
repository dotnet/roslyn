// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using WindowsInput.Native;
using Xunit;

namespace Roslyn.VisualStudio.NewIntegrationTests.CSharp;

public class CSharpCodeActions : AbstractEditorTest
{
    public CSharpCodeActions()
        : base(nameof(CSharpCodeActions))
    {
    }

    protected override string LanguageName => LanguageNames.CSharp;

    [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
    public async Task GenerateMethodInClosedFile()
    {
        var project = ProjectName;
        await TestServices.SolutionExplorer.AddFileAsync(project, "Foo.cs", contents: @"
public class Foo
{
}
", cancellationToken: HangMitigatingCancellationToken);

        await SetUpEditorAsync(@"
using System;

public class Program
{
    public static void Main(string[] args)
    {
        Foo f = new Foo();
        f.Bar()$$
    }
}
", HangMitigatingCancellationToken);

        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CodeActionAsync("Generate method 'Bar'", applyFix: true, cancellationToken: HangMitigatingCancellationToken);
        await TestServices.SolutionVerifier.FileContentsAsync(project, "Foo.cs", @"
using System;

public class Foo
{
    internal void Bar()
    {
        throw new NotImplementedException();
    }
}
", HangMitigatingCancellationToken);
    }

    [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
    public async Task AddUsingOnIncompleteMember()
    {
        // Need to ensure that incomplete member diagnostics run at high pri so that add-using can be
        // triggered by them.
        await SetUpEditorAsync(@"
class Program
{
    DateTime$$
}
", HangMitigatingCancellationToken);
        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CodeActionAsync("using System;", cancellationToken: HangMitigatingCancellationToken);
    }

    [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
    public async Task FastDoubleInvoke()
    {
        // We want to invoke the first smart tag and then *immediately* try invoking the next.
        // The next will happen to be the 'Simplify name' smart tag.  We should be able
        // to get it to invoke without any sort of waiting to happen.  This helps address a bug
        // we had where our asynchronous smart tags interfered with asynchrony in VS, which caused
        // the second smart tag to not expand if you tried invoking it too quickly
        await SetUpEditorAsync(@"
class Program
{
    static void Main(string[] args)
    {
        Exception $$ex = new System.ArgumentException();
    }
}
", HangMitigatingCancellationToken);

        // Suspend file change notification during code action application, since spurious file change notifications
        // can cause silent failure to apply the code action if they occur within this block.
        await using (var fileChangeRestorer = await TestServices.Shell.PauseFileChangesAsync(HangMitigatingCancellationToken))
        {
            await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CodeActionAsync("using System;", applyFix: true, blockUntilComplete: true, cancellationToken: HangMitigatingCancellationToken);
        }

        // Suspend file change notification during code action application, since spurious file change notifications
        // can cause silent failure to apply the code action if they occur within this block.
        await using (var fileChangeRestorer = await TestServices.Shell.PauseFileChangesAsync(HangMitigatingCancellationToken))
        {
            await TestServices.Editor.InvokeCodeActionListWithoutWaitingAsync(HangMitigatingCancellationToken);
            await TestServices.EditorVerifier.CodeActionAsync("Simplify name 'System.ArgumentException'", applyFix: true, blockUntilComplete: true, cancellationToken: HangMitigatingCancellationToken);
        }

        await TestServices.EditorVerifier.TextContainsAsync(
            @"
using System;

class Program
{
    static void Main(string[] args)
    {
        Exception ex = new ArgumentException();
    }
}", cancellationToken: HangMitigatingCancellationToken);
    }

    [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
    public async Task InvokeDelegateWithConditionalAccessMultipleTimes()
    {
        var markup = @"
using System;
class C
{
    public event EventHandler First;
    public event EventHandler Second;
    void RaiseFirst()
    {
        var temp1 = First;
        if (temp1 != null)
        {
            temp1$$(this, EventArgs.Empty);
        }
    }
    void RaiseSecond()
    {
        var temp2 = Second;
        if (temp2 != null)
        {
            temp2(this, EventArgs.Empty);
        }
    }
}";
        MarkupTestFile.GetSpans(markup, out _, out var _);

        await SetUpEditorAsync(markup, HangMitigatingCancellationToken);
        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CodeActionAsync("Simplify delegate invocation", applyFix: true, ensureExpectedItemsAreOrdered: true, blockUntilComplete: true, cancellationToken: HangMitigatingCancellationToken);
        await TestServices.Editor.PlaceCaretAsync("temp2(", 0, 0, extendSelection: false, selectBlock: false, HangMitigatingCancellationToken);
        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CodeActionAsync("Simplify delegate invocation", applyFix: true, ensureExpectedItemsAreOrdered: true, blockUntilComplete: true, cancellationToken: HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextContainsAsync("First?.", cancellationToken: HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextContainsAsync("Second?.", cancellationToken: HangMitigatingCancellationToken);
    }

    [IdeFact]
    [Trait(Traits.Feature, Traits.Features.EditorConfig)]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/15003")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/19089")]
    public async Task ApplyEditorConfigAndFixAllOccurrences()
    {
        var markup = @"
class C
{
    public int X1
    {
        get
        {
            $$return 3;
        }
    }

    public int Y1 => 5;

    public int X2
    {
        get
        {
            return 3;
        }
    }

    public int Y2 => 5;
}";
        var expectedText = @"
class C
{
    public int X1 => 3;

    public int Y1 => 5;

    public int X2 => 3;

    public int Y2 => 5;
}";

        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "Class1.cs", HangMitigatingCancellationToken);

        /*
         * The first portion of this test adds a .editorconfig file to configure the analyzer behavior, and verifies
         * that diagnostics appear automatically in response to the newly-created file. A fix all operation is
         * applied, and the result is verified against the expected outcome for the .editorconfig style.
         */

        MarkupTestFile.GetSpans(markup, out _, out var _);
        await SetUpEditorAsync(markup, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawlerLegacy,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.ErrorSquiggles
            ],
            HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CodeActionsNotShowingAsync(HangMitigatingCancellationToken);

        var editorConfig = @"root = true

[*.cs]
csharp_style_expression_bodied_properties = true:warning
";

        await TestServices.SolutionExplorer.AddFileAsync(ProjectName, ".editorconfig", editorConfig, open: false, HangMitigatingCancellationToken);

        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawlerLegacy,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.ErrorSquiggles
            ],
            HangMitigatingCancellationToken);
        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CodeActionAsync(
            "Use expression body for property",
            applyFix: true,
            fixAllScope: FixAllScope.Project,
            cancellationToken: HangMitigatingCancellationToken);

        AssertEx.EqualOrDiff(expectedText, await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));

        /*
         * The second portion of this test modifier the existing .editorconfig file to configure the analyzer to the
         * opposite style of the initial configuration, and verifies that diagnostics update automatically in
         * response to the changes. A fix all operation is applied, and the result is verified against the expected
         * outcome for the modified .editorconfig style.
         */

        await TestServices.SolutionExplorer.SetFileContentsAsync(ProjectName, ".editorconfig", editorConfig.Replace("true:warning", "false:warning"), HangMitigatingCancellationToken);

        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawlerLegacy,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.ErrorSquiggles
            ],
            HangMitigatingCancellationToken);
        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CodeActionAsync(
            "Use block body for property",
            applyFix: true,
            fixAllScope: FixAllScope.Project,
            cancellationToken: HangMitigatingCancellationToken);

        expectedText = @"
class C
{
    public int X1
    {
        get
        {
            return 3;
        }
    }

    public int Y1
    {
        get
        {
            return 5;
        }
    }

    public int X2
    {
        get
        {
            return 3;
        }
    }

    public int Y2
    {
        get
        {
            return 5;
        }
    }
}";

        AssertEx.EqualOrDiff(expectedText, await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
    }

    [CriticalIdeTheory]
    [InlineData(FixAllScope.Project)]
    [InlineData(FixAllScope.Solution)]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/33507")]
    public async Task FixAllOccurrencesIgnoresGeneratedCode(FixAllScope scope)
    {
        var markup = @"
using System;
using $$System.Threading;

class C
{
    public IntPtr X1 { get; set; }
}";
        var expectedText = @"
using System;

class C
{
    public IntPtr X1 { get; set; }
}";
        var generatedSourceMarkup = @"// <auto-generated/>
using System;
using $$System.Threading;

class D
{
    public IntPtr X1 { get; set; }
}";
        var expectedGeneratedSource = @"// <auto-generated/>
using System;

class D
{
    public IntPtr X1 { get; set; }
}";

        MarkupTestFile.GetPosition(generatedSourceMarkup, out var generatedSource, out int generatedSourcePosition);

        await TestServices.SolutionExplorer.AddFileAsync(ProjectName, "D.cs", generatedSource, open: false, HangMitigatingCancellationToken);

        // Switch to the main document we'll be editing
        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "Class1.cs", HangMitigatingCancellationToken);

        // Verify that applying a Fix All operation does not change generated files.
        // This is a regression test for correctness with respect to the design.
        await SetUpEditorAsync(markup, HangMitigatingCancellationToken);
        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CodeActionAsync(
            "Remove unnecessary usings",
            applyFix: true,
            fixAllScope: scope,
            cancellationToken: HangMitigatingCancellationToken);

        AssertEx.EqualOrDiff(expectedText, await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));

        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "D.cs", HangMitigatingCancellationToken);
        AssertEx.EqualOrDiff(generatedSource, await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));

        // Verify that a Fix All in Document in the generated file still does nothing.
        // ⚠ This is a statement of the current behavior, and not a claim regarding correctness of the design.
        // The current behavior is observable; any change to this behavior should be part of an intentional design
        // change.
        await TestServices.Editor.MoveCaretAsync(generatedSourcePosition, HangMitigatingCancellationToken);
        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CodeActionAsync(
            "Remove unnecessary usings",
            applyFix: true,
            fixAllScope: FixAllScope.Document,
            cancellationToken: HangMitigatingCancellationToken);

        AssertEx.EqualOrDiff(generatedSource, await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));

        // Verify that the code action can still be applied manually from within the generated file.
        // This is a regression test for correctness with respect to the design.
        await TestServices.Editor.MoveCaretAsync(generatedSourcePosition, HangMitigatingCancellationToken);
        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CodeActionAsync(
            "Remove unnecessary usings",
            applyFix: true,
            fixAllScope: null,
            cancellationToken: HangMitigatingCancellationToken);

        AssertEx.EqualOrDiff(expectedGeneratedSource, await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
    }

    [CriticalIdeTheory]
    [InlineData(FixAllScope.Project)]
    [InlineData(FixAllScope.Solution)]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/33507")]
    public async Task FixAllOccurrencesTriggeredFromGeneratedCode(FixAllScope scope)
    {
        var markup = @"// <auto-generated/>
using System;
using $$System.Threading;

class C
{
    public IntPtr X1 { get; set; }
}";
        var secondFile = @"
using System;
using System.Threading;

class D
{
    public IntPtr X1 { get; set; }
}";
        var expectedSecondFile = @"
using System;

class D
{
    public IntPtr X1 { get; set; }
}";

        await TestServices.SolutionExplorer.AddFileAsync(ProjectName, "D.cs", secondFile, open: false, HangMitigatingCancellationToken);

        // Switch to the main document we'll be editing
        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "Class1.cs", HangMitigatingCancellationToken);

        // Verify that applying a Fix All operation does not change generated file, but does change other files.
        // ⚠ This is a statement of the current behavior, and not a claim regarding correctness of the design.
        // The current behavior is observable; any change to this behavior should be part of an intentional design
        // change.
        MarkupTestFile.GetPosition(markup, out var expectedText, out int _);
        await SetUpEditorAsync(markup, HangMitigatingCancellationToken);
        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.CodeActionAsync(
            "Remove unnecessary usings",
            applyFix: true,
            fixAllScope: scope,
            cancellationToken: HangMitigatingCancellationToken);

        AssertEx.EqualOrDiff(expectedText, await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));

        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "D.cs", HangMitigatingCancellationToken);
        AssertEx.EqualOrDiff(expectedSecondFile, await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
    }

    [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
    public async Task ClassificationInPreviewPane()
    {
        await SetUpEditorAsync(@"
class Program
{
    int Main()
    {
        Foo$$();
    }
}", HangMitigatingCancellationToken);
        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
        var classifiedTokens = await TestServices.Editor.GetLightBulbPreviewClassificationsAsync("Generate method 'Foo'", HangMitigatingCancellationToken);
        Assert.True(classifiedTokens.Any(c => c.Span.GetText().ToString() == "void" && c.ClassificationType.Classification == "keyword"));
    }

    [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
    public async Task AddUsingExactMatchBeforeRenameTracking()
    {
        await SetUpEditorAsync(@"
public class Program
{
    static void Main(string[] args)
    {
        P2$$ p;
    }
}

public class P2 { }", HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync([VirtualKeyCode.BACK, VirtualKeyCode.BACK, "Stream"], HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [
                FeatureAttribute.Workspace,
                FeatureAttribute.EventHookup,
                FeatureAttribute.Rename,
                FeatureAttribute.RenameTracking,
                FeatureAttribute.InlineRenameFlyout,
                FeatureAttribute.SolutionCrawlerLegacy,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.ErrorSquiggles,
            ],
            HangMitigatingCancellationToken);

        // Suspend file change notification during code action application, since spurious file change notifications
        // can cause silent failure to apply the code action if they occur within this block.
        await using (var fileChangeRestorer = await TestServices.Shell.PauseFileChangesAsync(HangMitigatingCancellationToken))
        {
            await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            var expectedItems = new[]
            {
                "using System.IO;",
                "Rename 'P2' to 'Stream'",
                "System.IO.Stream",
                "Generate class 'Stream' in new file",
                "Generate class 'Stream'",
                "Generate nested class 'Stream'",
                "Generate new type...",
                "Remove unused variable",
                "Suppress or configure issues",
                "Suppress CS0168",
                "in Source",
                "Configure CS0168 severity",
                "None",
                "Silent",
                "Suggestion",
                "Warning",
                "Error",
            };

            await TestServices.EditorVerifier.CodeActionsAsync(expectedItems, applyFix: expectedItems[0], ensureExpectedItemsAreOrdered: true, cancellationToken: HangMitigatingCancellationToken);
        }

        await TestServices.EditorVerifier.TextContainsAsync("using System.IO;", cancellationToken: HangMitigatingCancellationToken);
    }

    [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
    public async Task GFUFuzzyMatchAfterRenameTrackingAndAfterGenerateType()
    {
        await SetUpEditorAsync(@"
namespace N
{
    class Goober { }
}

namespace NS
{
    public class P2
    {
        static void Main(string[] args)
        {
            P2$$ p;
        }
    }
}", HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync([VirtualKeyCode.BACK, VirtualKeyCode.BACK, "Foober"], HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [
                FeatureAttribute.Workspace,
                FeatureAttribute.EventHookup,
                FeatureAttribute.Rename,
                FeatureAttribute.RenameTracking,
                FeatureAttribute.InlineRenameFlyout,
                FeatureAttribute.SolutionCrawlerLegacy,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.ErrorSquiggles,
            ],
            HangMitigatingCancellationToken);

        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
        var expectedItems = new[]
        {
            "Rename 'P2' to 'Foober'",
            "Generate class 'Foober' in new file",
            "Generate class 'Foober'",
            "Generate nested class 'Foober'",
            "Generate new type...",
            "Goober - using N;",
            "Suppress or configure issues",
            "Suppress CS0168",
            "in Source",
            "Configure CS0168 severity",
            "None",
            "Silent",
            "Suggestion",
            "Warning",
            "Error",
        };

        await TestServices.EditorVerifier.CodeActionsAsync(expectedItems, applyFix: expectedItems[0], ensureExpectedItemsAreOrdered: true, cancellationToken: HangMitigatingCancellationToken);
    }

    [IdeFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
    public async Task SuppressionAfterRefactorings()
    {
        await SetUpEditorAsync(@"
[System.Obsolete]
class C
{
}
class Program
{
    static void Main(string[] args)
    {
        C p = $$2;
    }
}", HangMitigatingCancellationToken);
        await TestServices.Editor.SelectTextInCurrentDocumentAsync("2", HangMitigatingCancellationToken);

        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);

        var generateImplicitTitle = "Generate implicit conversion operator in 'C'";
        var expectedItems = new[]
        {
            "Introduce constant for '2'",
            "Introduce constant for all occurrences of '2'",
            "Introduce local constant for '2'",
            "Introduce local constant for all occurrences of '2'",
            "Extract method",
            generateImplicitTitle,
            "Suppress or configure issues",
            "Suppress CS0612",
            "in Source",
            "Configure CS0612 severity",
            "None",
            "Silent",
            "Suggestion",
            "Warning",
            "Error",
        };

        await TestServices.EditorVerifier.CodeActionsAsync(expectedItems, applyFix: generateImplicitTitle, ensureExpectedItemsAreOrdered: true, cancellationToken: HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextContainsAsync("implicit", cancellationToken: HangMitigatingCancellationToken);
    }

    [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
    public async Task OrderFixesByCursorProximityLeft()
    {
        await SetUpEditorAsync(@"
using System;
public class Program
{
    static void Main(string[] args)
    {
        Byte[] bytes = null;
        GCHandle$$ handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
    }
}", HangMitigatingCancellationToken);
        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
        var expectedItems = new[]
        {
            "using System.Runtime.InteropServices;",
            "System.Runtime.InteropServices.GCHandle"
        };

        await TestServices.EditorVerifier.CodeActionsAsync(expectedItems, applyFix: expectedItems[0], ensureExpectedItemsAreOrdered: true, cancellationToken: HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextContainsAsync("using System.Runtime.InteropServices", cancellationToken: HangMitigatingCancellationToken);
    }

    [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
    public async Task OrderFixesByCursorProximityRight()
    {
        await SetUpEditorAsync(@"
using System;
public class Program
{
    static void Main(string[] args)
    {
        Byte[] bytes = null;
        GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.$$Pinned);
    }
}", HangMitigatingCancellationToken);
        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
        var expectedItems = new[]
        {
            "using System.Runtime.InteropServices;",
            "System.Runtime.InteropServices.GCHandle"
        };

        await TestServices.EditorVerifier.CodeActionsAsync(expectedItems, applyFix: expectedItems[0], ensureExpectedItemsAreOrdered: true, cancellationToken: HangMitigatingCancellationToken);
        await TestServices.EditorVerifier.TextContainsAsync("using System.Runtime.InteropServices", cancellationToken: HangMitigatingCancellationToken);
    }

    [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
    public async Task ConfigureCodeStyleOptionValueAndSeverity()
    {
        await SetUpEditorAsync(@"
using System;
public class Program
{
    static void Main(string[] args)
    {
        var $$x = new Program();
    }
}", HangMitigatingCancellationToken);
        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.CodeActionsAsync([
            "Use discard '__'",
            "Use explicit type instead of 'var'",
            "Introduce local",
            "Introduce parameter for 'new Program()'",
            "Suppress or configure issues",
        ], cancellationToken: HangMitigatingCancellationToken);
    }

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/72428"), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/46784")]
    public async Task ConfigureSeverity()
    {
        var markup = @"
class C
{
    public static void Main()
    {
        // CS0168: The variable 'x' is declared but never used
        int $$x;
    }
}";
        await SetUpEditorAsync(markup, HangMitigatingCancellationToken);

        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawlerLegacy,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.ErrorSquiggles,
            ],
            HangMitigatingCancellationToken);

        // Verify CS0168 warning in original code.
        await VerifyDiagnosticInErrorListAsync("warning", TestServices, HangMitigatingCancellationToken);

        // Apply configuration severity fix to change CS0168 to be an error.
        await SetUpEditorAsync(markup, HangMitigatingCancellationToken);

        // Suspend file change notification during code action application, since spurious file change notifications
        // can cause silent failure to apply the code action if they occur within this block.
        await using (var fileChangeRestorer = await TestServices.Shell.PauseFileChangesAsync(HangMitigatingCancellationToken))
        {
            await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            var expectedItems = new[]
            {
                "Remove unused variable",
                "Suppress or configure issues",
                    "Suppress CS0168",
                        "in Source",
                    "Configure CS0168 severity",
                        "None",
                        "Silent",
                        "Suggestion",
                        "Warning",
                        "Error",
            };
            await TestServices.EditorVerifier.CodeActionsAsync(expectedItems, applyFix: "Error", ensureExpectedItemsAreOrdered: true, cancellationToken: HangMitigatingCancellationToken);
        }

        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawlerLegacy,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.ErrorSquiggles,
            ],
            HangMitigatingCancellationToken);

        // Verify CS0168 is now reported as an error.
        await VerifyDiagnosticInErrorListAsync("error", TestServices, HangMitigatingCancellationToken);

        static async Task VerifyDiagnosticInErrorListAsync(string expectedSeverity, TestServices testServices, CancellationToken cancellationToken)
        {
            await testServices.ErrorList.ShowErrorListAsync(cancellationToken);
            await testServices.Editor.GetTagsAsync<IErrorTag>(cancellationToken);

            var expectedContents = $"Class1.cs(7, 13): {expectedSeverity} CS0168: The variable 'x' is declared but never used";

            var builder = new StringBuilder();

            for (var i = 0; i < 120; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(TimeSpan.FromSeconds(1));

                var actualContents = string.Join(Environment.NewLine, await testServices.ErrorList.GetErrorsAsync(cancellationToken));
                if (expectedContents == actualContents)
                    return;

                builder.AppendLine($"Error list result {i}: {actualContents}");
            }

            AssertEx.Fail(builder.ToString());
        }
    }

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/72428"), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/46784")]
    public async Task ConfigureSeverityWithManualEditsToEditorconfig()
    {
        var markup = @"
class C
{
    public static void Main()
    {
        // CS0168: The variable 'x' is declared but never used
        int $$x;
    }
}";
        await SetUpEditorAsync(markup, HangMitigatingCancellationToken);

        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawlerLegacy,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.ErrorSquiggles,
            ],
            HangMitigatingCancellationToken);

        // Verify CS0168 warning in original code.
        await VerifyDiagnosticInErrorListAsync("warning", TestServices, HangMitigatingCancellationToken);

        // Add an .editorconfig file to the project to change severity to error.
        await TestServices.SolutionExplorer.AddFileAsync(ProjectName, ".editorconfig", open: true, cancellationToken: HangMitigatingCancellationToken);
        await TestServices.Input.SendAsync(@"
[*.cs]
dotnet_diagnostic.CS0168.severity = ", HangMitigatingCancellationToken);

        // NOTE: Below wait is a critical step in repro-ing the original regression.
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawlerLegacy,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.ErrorSquiggles,
            ],
            HangMitigatingCancellationToken);

        await TestServices.Input.SendAsync("error", HangMitigatingCancellationToken);

        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawlerLegacy,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.ErrorSquiggles,
            ],
            HangMitigatingCancellationToken);

        // Verify CS0168 is now reported as an error.
        await VerifyDiagnosticInErrorListAsync("error", TestServices, HangMitigatingCancellationToken);

        static async Task VerifyDiagnosticInErrorListAsync(string expectedSeverity, TestServices testServices, CancellationToken cancellationToken)
        {
            await testServices.ErrorList.ShowErrorListAsync(cancellationToken);
            await testServices.Editor.GetTagsAsync<IErrorTag>(cancellationToken);

            var expectedContents = $"Class1.cs(7, 13): {expectedSeverity} CS0168: The variable 'x' is declared but never used";
            var builder = new StringBuilder();

            for (var i = 0; i < 120; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(TimeSpan.FromSeconds(1));

                var actualContents = string.Join(Environment.NewLine, await testServices.ErrorList.GetErrorsAsync(cancellationToken));
                if (expectedContents == string.Join(Environment.NewLine, actualContents))
                    return;

                builder.AppendLine($"Error list result {i}: {actualContents}");
            }

            AssertEx.Fail(builder.ToString());
        }
    }

    [IdeTheory(Skip = "https://github.com/dotnet/roslyn/issues/72428"), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
    [InlineData(BackgroundAnalysisScope.VisibleFilesAndOpenFilesWithPreviouslyReportedDiagnostics, CompilerDiagnosticsScope.VisibleFilesAndOpenFilesWithPreviouslyReportedDiagnostics)]
    [InlineData(BackgroundAnalysisScope.FullSolution, CompilerDiagnosticsScope.FullSolution)]
    internal async Task ConfigureSeverityWithManualEditsToEditorconfig_CurrentDocumentScope(BackgroundAnalysisScope analyzerScope, CompilerDiagnosticsScope compilerScope)
    {
        var markup1 = @"
class C
{
    public static void Main()
    {
        // CS0219: The variable 'x' is assigned but its value is never used
        // IDE0059: Unnecessary assignment of a value to 'x'
        int x = 0;
    }
}";

        var markup2 = @"
class C2
{
    public static void M()
    {
        // CS0219: The variable 'y' is assigned but its value is never used
        // IDE0059: Unnecessary assignment of a value to 'y'
        int $$y = 0;
    }
}";
        await TestServices.Workspace.SetBackgroundAnalysisOptionsAsync(analyzerScope, compilerScope, HangMitigatingCancellationToken);

        await SetUpEditorAsync(markup2, HangMitigatingCancellationToken);

        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawlerLegacy,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.ErrorSquiggles,
            ],
            HangMitigatingCancellationToken);

        await TestServices.SolutionExplorer.AddFileAsync(ProjectName, "Class2.cs", markup1, open: true, cancellationToken: HangMitigatingCancellationToken);

        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawlerLegacy,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.ErrorSquiggles,
            ],
            HangMitigatingCancellationToken);

        // Verify compiler and analyzer diagnostics in original code.
        await VerifyDiagnosticsInErrorListAsync("warning", "info", TestServices, HangMitigatingCancellationToken);

        // Add an .editorconfig file to the project to change severities to error.
        await TestServices.SolutionExplorer.AddFileAsync(ProjectName, ".editorconfig", open: true, cancellationToken: HangMitigatingCancellationToken);
        await TestServices.Editor.SetTextAsync(@"
[*.cs]
dotnet_diagnostic.CS0219.severity = error
dotnet_diagnostic.IDE0059.severity = error", HangMitigatingCancellationToken);

        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawlerLegacy,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.ErrorSquiggles,
            ],
            HangMitigatingCancellationToken);

        // Verify compiler and analyzer diagnostics are now reported as errors.
        await VerifyDiagnosticsInErrorListAsync("error", "error", TestServices, HangMitigatingCancellationToken);

        // Edit editorconfig file to disable both compiler and analyzer diagnostics.
        await TestServices.Editor.SetTextAsync(@"
[*.cs]
dotnet_diagnostic.CS0219.severity = none
dotnet_diagnostic.IDE0059.severity = none", HangMitigatingCancellationToken);

        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawlerLegacy,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.ErrorSquiggles,
            ],
            HangMitigatingCancellationToken);

        // Verify compiler and analyzer diagnostics are now cleared.
        await VerifyDiagnosticsInErrorListAsync("none", "none", TestServices, HangMitigatingCancellationToken);

        static async Task VerifyDiagnosticsInErrorListAsync(string expectedCompilerDiagnosticSeverity, string expectedAnalyzerDiagnosticSeverity, TestServices testServices, CancellationToken cancellationToken)
        {
            await testServices.ErrorList.ShowErrorListAsync(cancellationToken);
            await testServices.Editor.GetTagsAsync<IErrorTag>(cancellationToken);

            using var _ = ArrayBuilder<string>.GetInstance(out var expectedContentsBuilder);

            if (expectedCompilerDiagnosticSeverity != "none")
            {
                expectedContentsBuilder.Add($"Class1.cs(8, 13): {expectedCompilerDiagnosticSeverity} CS0219: The variable 'y' is assigned but its value is never used");
                expectedContentsBuilder.Add($"Class2.cs(8, 13): {expectedCompilerDiagnosticSeverity} CS0219: The variable 'x' is assigned but its value is never used");
            }

            if (expectedAnalyzerDiagnosticSeverity != "none")
            {
                expectedContentsBuilder.Add($"Class1.cs(8, 13): {expectedAnalyzerDiagnosticSeverity} IDE0059: Unnecessary assignment of a value to 'y'");
                expectedContentsBuilder.Add($"Class2.cs(8, 13): {expectedAnalyzerDiagnosticSeverity} IDE0059: Unnecessary assignment of a value to 'x'");
            }

            var expectedContents = string.Join(Environment.NewLine, expectedContentsBuilder.ToImmutable().Sort());

            var builder = new StringBuilder();

            for (var i = 0; i < 120; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(TimeSpan.FromSeconds(1));

                var actualContents = string.Join(Environment.NewLine, await testServices.ErrorList.GetErrorsAsync(ErrorSource.Other, Microsoft.VisualStudio.Shell.Interop.__VSERRORCATEGORY.EC_MESSAGE, cancellationToken));
                if (expectedContents == actualContents)
                    return;
            }

            AssertEx.Fail(builder.ToString());
        }
    }

    [IdeFact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public async Task TestFixAllOccurrences_CodeFix_ContainingMember()
    {
        var markup = @"
class Program1
{
    static void Main()
    {
        $$if (true) if (true) return;

        if (false) if (false) return;
    }

    void OtherMethod()
    {
        if (true) if (true) return;
    }
}

class OtherType
{
    void OtherMethod()
    {
        if (true) if (true) return;
    }
}";
        var expectedText = @"
class Program1
{
    static void Main()
    {
        if (true)
        {
            if (true)
            {
                return;
            }
        }

        if (false)
        {
            if (false)
            {
                return;
            }
        }
    }

    void OtherMethod()
    {
        if (true) if (true) return;
    }
}

class OtherType
{
    void OtherMethod()
    {
        if (true) if (true) return;
    }
}";

        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "Class1.cs", HangMitigatingCancellationToken);

        MarkupTestFile.GetSpans(markup, out _, out var _);
        await SetUpEditorAsync(markup, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawlerLegacy,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.ErrorSquiggles
            ],
            HangMitigatingCancellationToken);

        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.CodeActionAsync(
            "Add braces",
            applyFix: true,
            fixAllScope: FixAllScope.ContainingMember,
            cancellationToken: HangMitigatingCancellationToken);

        AssertEx.EqualOrDiff(expectedText, await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
    }

    [IdeFact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public async Task TestFixAllOccurrences_CodeFix_ContainingType()
    {
        var markup1 = @"
partial class Program1
{
    static void Main()
    {
        $$if (true) if (true) return;

        if (false) if (false) return;
    }

    void M1()
    {
        if (true) if (true) return;
    }
}

class OtherType1
{
    void OtherMethod()
    {
        if (true) if (true) return;
    }
}

partial class Program1
{
    void M2()
    {
        if (true) if (true) return;
    }
}";
        var expectedText1 = @"
partial class Program1
{
    static void Main()
    {
        if (true)
        {
            if (true)
            {
                return;
            }
        }

        if (false)
        {
            if (false)
            {
                return;
            }
        }
    }

    void M1()
    {
        if (true)
        {
            if (true)
            {
                return;
            }
        }
    }
}

class OtherType1
{
    void OtherMethod()
    {
        if (true) if (true) return;
    }
}

partial class Program1
{
    void M2()
    {
        if (true)
        {
            if (true)
            {
                return;
            }
        }
    }
}";

        var markup2 = @"
partial class Program1
{
    void OtherFileMethod()
    {
        if (true) if (true) return;

        if (false) if (false) return;
    }
}

class OtherType2
{
    void OtherMethod()
    {
        if (true) if (true) return;
    }
}";
        var expectedText2 = @"
partial class Program1
{
    void OtherFileMethod()
    {
        if (true)
        {
            if (true)
            {
                return;
            }
        }

        if (false)
        {
            if (false)
            {
                return;
            }
        }
    }
}

class OtherType2
{
    void OtherMethod()
    {
        if (true) if (true) return;
    }
}";

        await TestServices.SolutionExplorer.AddFileAsync(ProjectName, "Class2.cs", markup2, cancellationToken: HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "Class1.cs", HangMitigatingCancellationToken);

        MarkupTestFile.GetSpans(markup1, out _, out var _);
        await SetUpEditorAsync(markup1, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawlerLegacy,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.ErrorSquiggles
            ],
            HangMitigatingCancellationToken);

        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.CodeActionAsync(
            "Add braces",
            applyFix: true,
            fixAllScope: FixAllScope.ContainingType,
            cancellationToken: HangMitigatingCancellationToken);

        AssertEx.EqualOrDiff(expectedText1, await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));

        AssertEx.EqualOrDiff(expectedText2, await TestServices.SolutionExplorer.GetFileContentsAsync(ProjectName, "Class2.cs", HangMitigatingCancellationToken));
    }

    [IdeFact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public async Task TestFixAllOccurrences_CodeRefactoring_ContainingMember()
    {
        var markup = @"
class C1
{
    void M()
    {
        var singleLine1 = $$""a"";
        var singleLine2 = @""goo""""bar"";
    }

    void M2()
    {
        var singleLine1 = ""a"";
        var singleLine2 = @""goo""""bar"";
    }
}

class C2
{
    void M3()
    {
        var singleLine1 = ""a"";
        var singleLine2 = @""goo""""bar"";
    }
}";
        var expectedText = @"
class C1
{
    void M()
    {
        var singleLine1 = """"""a"""""";
        var singleLine2 = """"""goo""bar"""""";
    }

    void M2()
    {
        var singleLine1 = ""a"";
        var singleLine2 = @""goo""""bar"";
    }
}

class C2
{
    void M3()
    {
        var singleLine1 = ""a"";
        var singleLine2 = @""goo""""bar"";
    }
}";

        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "Class1.cs", HangMitigatingCancellationToken);

        MarkupTestFile.GetSpans(markup, out _, out var _);
        await SetUpEditorAsync(markup, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawlerLegacy,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.ErrorSquiggles
            ],
            HangMitigatingCancellationToken);

        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.CodeActionAsync(
            "Convert to raw string",
            applyFix: true,
            fixAllScope: FixAllScope.ContainingMember,
            cancellationToken: HangMitigatingCancellationToken);

        AssertEx.EqualOrDiff(expectedText, await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
    }

    [IdeFact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public async Task TestFixAllOccurrences_CodeRefactoring_ContainingType()
    {
        var markup1 = @"
partial class C1
{
    void M()
    {
        var singleLine1 = $$""a"";
        var singleLine2 = @""goo""""bar"";
    }

    void M2()
    {
        var singleLine1 = ""a"";
        var singleLine2 = @""goo""""bar"";
    }
}

class C2
{
    void M3()
    {
        var singleLine1 = ""a"";
        var singleLine2 = @""goo""""bar"";
    }
}

partial class C1
{
    void M4()
    {
        var singleLine1 = ""a"";
        var singleLine2 = @""goo""""bar"";
    }
}";
        var expectedText1 = @"
partial class C1
{
    void M()
    {
        var singleLine1 = """"""a"""""";
        var singleLine2 = """"""goo""bar"""""";
    }

    void M2()
    {
        var singleLine1 = """"""a"""""";
        var singleLine2 = """"""goo""bar"""""";
    }
}

class C2
{
    void M3()
    {
        var singleLine1 = ""a"";
        var singleLine2 = @""goo""""bar"";
    }
}

partial class C1
{
    void M4()
    {
        var singleLine1 = """"""a"""""";
        var singleLine2 = """"""goo""bar"""""";
    }
}";

        var markup2 = @"
partial class C1
{
    void M5()
    {
        var singleLine1 = ""a"";
        var singleLine2 = @""goo""""bar"";
    }
}

class C2
{
    void M6()
    {
        var singleLine1 = ""a"";
        var singleLine2 = @""goo""""bar"";
    }
}";
        var expectedText2 = @"
partial class C1
{
    void M5()
    {
        var singleLine1 = """"""a"""""";
        var singleLine2 = """"""goo""bar"""""";
    }
}

class C2
{
    void M6()
    {
        var singleLine1 = ""a"";
        var singleLine2 = @""goo""""bar"";
    }
}";

        await TestServices.SolutionExplorer.AddFileAsync(ProjectName, "Class2.cs", markup2, cancellationToken: HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "Class1.cs", HangMitigatingCancellationToken);

        MarkupTestFile.GetSpans(markup1, out _, out var _);
        await SetUpEditorAsync(markup1, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawlerLegacy,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.ErrorSquiggles
            ],
            HangMitigatingCancellationToken);

        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.CodeActionAsync(
            "Convert to raw string",
            applyFix: true,
            fixAllScope: FixAllScope.ContainingType,
            cancellationToken: HangMitigatingCancellationToken);

        AssertEx.EqualOrDiff(expectedText1, await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));

        AssertEx.EqualOrDiff(expectedText2, await TestServices.SolutionExplorer.GetFileContentsAsync(ProjectName, "Class2.cs", HangMitigatingCancellationToken));
    }

    [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/61334")]
    public async Task UseExpressionBodyBeforeExtractBaseClass()
    {
        await SetUpEditorAsync(@"
public class Program
{
    $$public void M()
    {
        System.Console.WriteLine(0);
    }
}
", HangMitigatingCancellationToken);

        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [
                FeatureAttribute.Workspace,
                FeatureAttribute.EventHookup,
                FeatureAttribute.Rename,
                FeatureAttribute.RenameTracking,
                FeatureAttribute.InlineRenameFlyout,
                FeatureAttribute.SolutionCrawlerLegacy,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.ErrorSquiggles,
            ],
            HangMitigatingCancellationToken);

        // Suspend file change notification during code action application, since spurious file change notifications
        // can cause silent failure to apply the code action if they occur within this block.
        await using var fileChangeRestorer = await TestServices.Shell.PauseFileChangesAsync(HangMitigatingCancellationToken);

        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
        var expectedItems = new[]
        {
            "Use expression body for method",
            "Extract base class...",
        };

        await TestServices.EditorVerifier.CodeActionsAsync(expectedItems, ensureExpectedItemsAreOrdered: true, cancellationToken: HangMitigatingCancellationToken);
    }

    [IdeFact(Skip = "https://github.com/dotnet/roslyn/issues/64567")]
    public async Task TestNonSourceDocumentRefactoring()
    {
        var markup = @"$$# Editorconfig File";
        var expectedText = @"# Editorconfig File
# Refactored";

        await TestServices.SolutionExplorer.OpenFileAsync(ProjectName, "Class1.cs", HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.AddAnalyzerReferenceAsync(ProjectName, typeof(NonSourceFileRefactoring).Assembly.Location, HangMitigatingCancellationToken);
        await TestServices.SolutionExplorer.AddFileAsync(ProjectName, ".editorconfig", contents: "", open: true, HangMitigatingCancellationToken);

        MarkupTestFile.GetSpans(markup, out _, out var _);
        await SetUpEditorAsync(markup, HangMitigatingCancellationToken);
        await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
            [
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawlerLegacy,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.ErrorSquiggles
            ],
            HangMitigatingCancellationToken);

        await TestServices.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);

        await TestServices.EditorVerifier.CodeActionAsync(
            nameof(NonSourceFileRefactoring),
            applyFix: true,
            cancellationToken: HangMitigatingCancellationToken);

        AssertEx.EqualOrDiff(expectedText, await TestServices.Editor.GetTextAsync(HangMitigatingCancellationToken));
    }

    [IdeFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
    public async Task TestRefactoringsAreSortedByPriority()
    {
        var codeFormat = @"
#pragma warning disable IDE0060 // Remove unused parameter
class C
{ 
    public C(int x1, int x2, int x3)
    {
    }
};";
        for (var i = 1; i <= 3; i++)
        {
            var code = codeFormat.Replace($"x{i}", $"$$x{i}");
            await SetUpEditorAsync(code, HangMitigatingCancellationToken);

            var expectedItems = new[]
            {
                $"Create and assign property 'X{i}'",
                $"Create and assign field 'x{i}'",
                "Create and assign remaining as properties",
                "Create and assign remaining as fields",
                "Change signature...",
                "Wrap every parameter",
                "Align wrapped parameters",
                "Indent all parameters",
                "Indent wrapped parameters",
                "Unwrap and indent all parameters",
            };

            await TestServices.EditorVerifier.CodeActionsAsync(expectedItems, ensureExpectedItemsAreOrdered: true, cancellationToken: HangMitigatingCancellationToken);
        }
    }
}
