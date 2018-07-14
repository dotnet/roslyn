// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpCodeActions : AbstractIdeEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpCodeActions()
            : base(nameof(CSharpCodeActions))
        {
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task GenerateMethodInClosedFileAsync()
        {
            await VisualStudio.SolutionExplorer.AddFileAsync(ProjectName, "Foo.cs", contents: @"
public class Foo
{
}
");

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
");

            await VisualStudio.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            await VisualStudio.Editor.Verify.CodeActionAsync("Generate method 'Foo.Bar'", applyFix: true, cancellationToken: HangMitigatingCancellationToken);
            VisualStudio.SolutionExplorer.Verify.FileContents(ProjectName, "Foo.cs", @"
using System;

public class Foo
{
    internal void Bar()
    {
        throw new NotImplementedException();
    }
}
");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
        public async Task FastDoubleInvokeAsync()
        {
            // We want to invoke the first smart tag and then *immediately * try invoking the next.
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
");
            await VisualStudio.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            await VisualStudio.Editor.Verify.CodeActionAsync("using System;", applyFix: true, willBlockUntilComplete: true, cancellationToken: HangMitigatingCancellationToken);
            await VisualStudio.Editor.InvokeCodeActionListWithoutWaitingAsync(HangMitigatingCancellationToken);
            await VisualStudio.Editor.Verify.CodeActionAsync("Simplify name 'System.ArgumentException'", applyFix: true, willBlockUntilComplete: true, cancellationToken: HangMitigatingCancellationToken);

            await VisualStudio.Editor.Verify.TextContainsAsync(
                @"
using System;

class Program
{
    static void Main(string[] args)
    {
        Exception ex = new ArgumentException();
    }
}");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public async Task InvokeDelegateWithConditionalAccessMultipleTimesAsync()
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

            MarkupTestFile.GetSpans(markup, out var text, out ImmutableArray<TextSpan> spans);

            await SetUpEditorAsync(markup);
            await VisualStudio.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            await VisualStudio.Editor.Verify.CodeActionAsync("Delegate invocation can be simplified.", applyFix: true, ensureExpectedItemsAreOrdered: true, willBlockUntilComplete: true, cancellationToken: HangMitigatingCancellationToken);
            await VisualStudio.Editor.PlaceCaretAsync("temp2", 0, 0, extendSelection: false, selectBlock: false);
            await VisualStudio.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            await VisualStudio.Editor.Verify.CodeActionAsync("Delegate invocation can be simplified.", applyFix: true, ensureExpectedItemsAreOrdered: true, willBlockUntilComplete: true, cancellationToken: HangMitigatingCancellationToken);
            await VisualStudio.Editor.Verify.TextContainsAsync("First?.");
            await VisualStudio.Editor.Verify.TextContainsAsync("Second?.");
        }

        [IdeFact]
        [Trait(Traits.Feature, Traits.Features.EditorConfig)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        [WorkItem(15003, "https://github.com/dotnet/roslyn/issues/15003")]
        [WorkItem(19089, "https://github.com/dotnet/roslyn/issues/19089")]
        public async Task ApplyEditorConfigAndFixAllOccurrencesAsync()
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

            // CodingConventions only sends notifications if a file is open for all directories in the project
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, @"Properties\AssemblyInfo.cs");

            // Switch back to the main document we'll be editing
            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "Class1.cs");

            /*
             * The first portion of this test adds a .editorconfig file to configure the analyzer behavior, and verifies
             * that diagnostics appear automatically in response to the newly-created file. A fix all operation is
             * applied, and the result is verified against the expected outcome for the .editorconfig style.
             */

            MarkupTestFile.GetSpans(markup, out var text, out ImmutableArray<TextSpan> spans);
            await SetUpEditorAsync(markup);
            await VisualStudio.VisualStudio.WaitForApplicationIdleAsync(CancellationToken.None);
            await VisualStudio.Editor.Verify.CodeActionsNotShowingAsync();

            var editorConfig = @"root = true

[*.cs]
csharp_style_expression_bodied_properties = true:warning
";

            await VisualStudio.SolutionExplorer.AddFileAsync(ProjectName, ".editorconfig", editorConfig, open: false);

            // Wait for CodingConventions library events to propagate to the workspace
            await VisualStudio.VisualStudio.WaitForApplicationIdleAsync(CancellationToken.None);
            await VisualStudio.Workspace.WaitForAllAsyncOperationsAsync(
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawler,
                FeatureAttribute.DiagnosticService);
            await VisualStudio.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            await VisualStudio.Editor.Verify.CodeActionAsync(
                "Use expression body for properties",
                applyFix: true,
                fixAllScope: FixAllScope.Project,
                cancellationToken: HangMitigatingCancellationToken);

            Assert.Equal(expectedText, await VisualStudio.Editor.GetTextAsync());

            /*
             * The second portion of this test modifier the existing .editorconfig file to configure the analyzer to the
             * opposite style of the initial configuration, and verifies that diagnostics update automatically in
             * response to the changes. A fix all operation is applied, and the result is verified against the expected
             * outcome for the modified .editorconfig style.
             */

            VisualStudio.SolutionExplorer.SetFileContents(ProjectName, ".editorconfig", editorConfig.Replace("true:warning", "false:warning"));

            // Wait for CodingConventions library events to propagate to the workspace
            await VisualStudio.VisualStudio.WaitForApplicationIdleAsync(CancellationToken.None);
            await VisualStudio.Workspace.WaitForAllAsyncOperationsAsync(
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawler,
                FeatureAttribute.DiagnosticService);
            await VisualStudio.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            await VisualStudio.Editor.Verify.CodeActionAsync(
                "Use block body for properties",
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

            Assert.Equal(expectedText, await VisualStudio.Editor.GetTextAsync());
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task ClassificationInPreviewPaneAsync()
        {
            await SetUpEditorAsync(@"
class Program
{
    int Main()
    {
        Foo$$();
    }
}");
            await VisualStudio.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            var classifiedTokens = await GetLightbulbPreviewClassificationAsync("Generate method 'Program.Foo'", HangMitigatingCancellationToken);
            Assert.True(classifiedTokens.Any(c => c.Span.GetText() == "void" && c.ClassificationType.Classification == "keyword"));
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
        public async Task AddUsingExactMatchBeforeRenameTrackingAsync()
        {
            await SetUpEditorAsync(@"
public class Program
{
    static void Main(string[] args)
    {
        P2$$ p;
    }
}

public class P2 { }");

            await VisualStudio.Editor.SendKeysAsync(VirtualKey.Backspace, VirtualKey.Backspace, "Stream");

            await VisualStudio.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            var expectedItems = new[]
            {
                "using System.IO;",
                "System.IO.Stream",
                "Generate class 'Stream' in new file",
                "Generate class 'Stream'",
                "Generate nested class 'Stream'",
                "Generate new type...",
                "Rename 'P2' to 'Stream'",
                "Suppress CS0168",
                "in Source"
            };

            await VisualStudio.Editor.Verify.CodeActionsAsync(expectedItems, applyFix: expectedItems[0], ensureExpectedItemsAreOrdered: true, cancellationToken: HangMitigatingCancellationToken);
            await VisualStudio.Editor.Verify.TextContainsAsync("using System.IO;");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GFUFuzzyMatchAfterRenameTrackingAsync()
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
}");
            await VisualStudio.Editor.SendKeysAsync(VirtualKey.Backspace, VirtualKey.Backspace,
                "Foober");

            await VisualStudio.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            var expectedItems = new[]
            {
                "Rename 'P2' to 'Foober'",
                "Generate type 'Foober'",
                "Generate class 'Foober' in new file",
                "Generate class 'Foober'",
                "Generate nested class 'Foober'",
                "Generate new type...",
                "Goober - using N;",
                "Suppress CS0168",
                "in Source",
            };

            await VisualStudio.Editor.Verify.CodeActionsAsync(expectedItems, applyFix: expectedItems[0], ensureExpectedItemsAreOrdered: true, cancellationToken: HangMitigatingCancellationToken);
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task SuppressionAfterRefactoringsAsync()
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
}");
            await VisualStudio.Editor.SelectTextInCurrentDocumentAsync("2");

            await VisualStudio.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);

            var generateImplicitTitle = "Generate implicit conversion operator in 'C'";
            var expectedItems = new[]
            {
                "Introduce constant for '2'",
                "Introduce constant for all occurrences of '2'",
                "Introduce local constant for '2'",
                "Introduce local constant for all occurrences of '2'",
                "Extract Method",
                generateImplicitTitle,
                "Suppress CS0612",
                "in Source",
            };

            await VisualStudio.Editor.Verify.CodeActionsAsync(expectedItems, applyFix: generateImplicitTitle, ensureExpectedItemsAreOrdered: true, cancellationToken: HangMitigatingCancellationToken);
            await VisualStudio.Editor.Verify.TextContainsAsync("implicit");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
        public async Task OrderFixesByCursorProximityLeftAsync()
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
}");
            await VisualStudio.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            var expectedItems = new[]
            {
                "using System.Runtime.InteropServices;",
                "System.Runtime.InteropServices.GCHandle"
            };

            await VisualStudio.Editor.Verify.CodeActionsAsync(expectedItems, applyFix: expectedItems[0], ensureExpectedItemsAreOrdered: true, cancellationToken: HangMitigatingCancellationToken);
            await VisualStudio.Editor.Verify.TextContainsAsync("using System.Runtime.InteropServices");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
        public async Task OrderFixesByCursorProximityRightAsync()
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
}");
            await VisualStudio.Editor.InvokeCodeActionListAsync(HangMitigatingCancellationToken);
            var expectedItems = new[]
            {
                "using System.Runtime.InteropServices;",
                "System.Runtime.InteropServices.GCHandle"
            };

            await VisualStudio.Editor.Verify.CodeActionsAsync(expectedItems, applyFix: expectedItems[0], ensureExpectedItemsAreOrdered: true, cancellationToken: HangMitigatingCancellationToken);
            await VisualStudio.Editor.Verify.TextContainsAsync("using System.Runtime.InteropServices");

        }
    }
}
