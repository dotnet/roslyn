// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpCodeActions : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpCodeActions(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper, nameof(CSharpCodeActions))
        {
        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/26204"), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public void GenerateMethodInClosedFile()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.AddFile(project, "Foo.cs", contents: @"
public class Foo
{
}
");

            SetUpEditor(@"
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

            VisualStudio.Editor.InvokeCodeActionList();
            VisualStudio.Editor.Verify.CodeAction("Generate method 'Foo.Bar'", applyFix: true);
            VisualStudio.SolutionExplorer.Verify.FileContents(project, "Foo.cs", @"
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
        public void FastDoubleInvoke()
        {
            // We want to invoke the first smart tag and then *immediately * try invoking the next.
            // The next will happen to be the 'Simplify name' smart tag.  We should be able
            // to get it to invoke without any sort of waiting to happen.  This helps address a bug
            // we had where our asynchronous smart tags interfered with asynchrony in VS, which caused
            // the second smart tag to not expand if you tried invoking it too quickly
            SetUpEditor(@"
class Program
{
    static void Main(string[] args)
    {
        Exception $$ex = new System.ArgumentException();
    }
}
");
            VisualStudio.Editor.InvokeCodeActionList();
            VisualStudio.Editor.Verify.CodeAction("using System;", applyFix: true, blockUntilComplete: true);
            VisualStudio.Editor.InvokeCodeActionListWithoutWaiting();
            VisualStudio.Editor.Verify.CodeAction("Simplify name 'System.ArgumentException'", applyFix: true, blockUntilComplete: true);

            VisualStudio.Editor.Verify.TextContains(
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public void InvokeDelegateWithConditionalAccessMultipleTimes()
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

            SetUpEditor(markup);
            VisualStudio.Editor.InvokeCodeActionList();
            VisualStudio.Editor.Verify.CodeAction("Delegate invocation can be simplified.", applyFix: true, ensureExpectedItemsAreOrdered: true, blockUntilComplete: true);
            VisualStudio.Editor.PlaceCaret("temp2", 0, 0, extendSelection: false, selectBlock: false);
            VisualStudio.Editor.InvokeCodeActionList();
            VisualStudio.Editor.Verify.CodeAction("Delegate invocation can be simplified.", applyFix: true, ensureExpectedItemsAreOrdered: true, blockUntilComplete: true);
            VisualStudio.Editor.Verify.TextContains("First?.");
            VisualStudio.Editor.Verify.TextContains("Second?.");
        }

        [ConditionalWpfFact(typeof(LegacyEditorConfigCondition))]
        [Trait(Traits.Feature, Traits.Features.EditorConfig)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        [WorkItem(15003, "https://github.com/dotnet/roslyn/issues/15003")]
        [WorkItem(19089, "https://github.com/dotnet/roslyn/issues/19089")]
        public void ApplyEditorConfigAndFixAllOccurrences()
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
            VisualStudio.SolutionExplorer.OpenFile(new ProjectUtils.Project(ProjectName), @"Properties\AssemblyInfo.cs");

            // Switch back to the main document we'll be editing
            VisualStudio.SolutionExplorer.OpenFile(new ProjectUtils.Project(ProjectName), "Class1.cs");

            /*
             * The first portion of this test adds a .editorconfig file to configure the analyzer behavior, and verifies
             * that diagnostics appear automatically in response to the newly-created file. A fix all operation is
             * applied, and the result is verified against the expected outcome for the .editorconfig style.
             */

            MarkupTestFile.GetSpans(markup, out var text, out ImmutableArray<TextSpan> spans);
            SetUpEditor(markup);
            VisualStudio.WaitForApplicationIdle(CancellationToken.None);
            VisualStudio.Workspace.WaitForAllAsyncOperations(
                Helper.HangMitigatingTimeout,
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawler,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.ErrorSquiggles);
            VisualStudio.Editor.Verify.CodeActionsNotShowing();

            var editorConfig = @"root = true

[*.cs]
csharp_style_expression_bodied_properties = true:warning
";

            VisualStudio.SolutionExplorer.BeginWatchForCodingConventionsChange(new ProjectUtils.Project(ProjectName), "Class1.cs");
            try
            {
                VisualStudio.SolutionExplorer.AddFile(new ProjectUtils.Project(ProjectName), ".editorconfig", editorConfig, open: false);
            }
            finally
            {
                VisualStudio.SolutionExplorer.EndWaitForCodingConventionsChange(Helper.HangMitigatingTimeout);
            }

            // Wait for CodingConventions library events to propagate to the workspace
            VisualStudio.WaitForApplicationIdle(CancellationToken.None);
            VisualStudio.Workspace.WaitForAllAsyncOperations(
                Helper.HangMitigatingTimeout,
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawler,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.ErrorSquiggles);
            VisualStudio.Editor.InvokeCodeActionList();
            VisualStudio.Editor.Verify.CodeAction(
                "Use expression body for properties",
                applyFix: true,
                fixAllScope: FixAllScope.Project);

            Assert.Equal(expectedText, VisualStudio.Editor.GetText());

            /*
             * The second portion of this test modifier the existing .editorconfig file to configure the analyzer to the
             * opposite style of the initial configuration, and verifies that diagnostics update automatically in
             * response to the changes. A fix all operation is applied, and the result is verified against the expected
             * outcome for the modified .editorconfig style.
             */

            VisualStudio.SolutionExplorer.BeginWatchForCodingConventionsChange(new ProjectUtils.Project(ProjectName), "Class1.cs");
            try
            {
                VisualStudio.SolutionExplorer.SetFileContents(new ProjectUtils.Project(ProjectName), ".editorconfig", editorConfig.Replace("true:warning", "false:warning"));
            }
            finally
            {
                VisualStudio.SolutionExplorer.EndWaitForCodingConventionsChange(Helper.HangMitigatingTimeout);
            }

            // Wait for CodingConventions library events to propagate to the workspace
            VisualStudio.WaitForApplicationIdle(CancellationToken.None);
            VisualStudio.Workspace.WaitForAllAsyncOperations(
                Helper.HangMitigatingTimeout,
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawler,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.ErrorSquiggles);
            VisualStudio.Editor.InvokeCodeActionList();
            VisualStudio.Editor.Verify.CodeAction(
                "Use block body for properties",
                applyFix: true,
                fixAllScope: FixAllScope.Project);

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

            Assert.Equal(expectedText, VisualStudio.Editor.GetText());
        }

        [CriticalWpfTheory]
        [InlineData(FixAllScope.Project)]
        [InlineData(FixAllScope.Solution)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        [WorkItem(33507, "https://github.com/dotnet/roslyn/issues/33507")]
        public void FixAllOccurrencesIgnoresGeneratedCode(FixAllScope scope)
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

            VisualStudio.SolutionExplorer.AddFile(new ProjectUtils.Project(ProjectName), "D.cs", generatedSource, open: false);

            // Switch to the main document we'll be editing
            VisualStudio.SolutionExplorer.OpenFile(new ProjectUtils.Project(ProjectName), "Class1.cs");

            // Verify that applying a Fix All operation does not change generated files.
            // This is a regression test for correctness with respect to the design.
            SetUpEditor(markup);
            VisualStudio.WaitForApplicationIdle(CancellationToken.None);
            VisualStudio.Editor.InvokeCodeActionList();
            VisualStudio.Editor.Verify.CodeAction(
                "Remove Unnecessary Usings",
                applyFix: true,
                fixAllScope: scope);

            Assert.Equal(expectedText, VisualStudio.Editor.GetText());

            VisualStudio.SolutionExplorer.OpenFile(new ProjectUtils.Project(ProjectName), "D.cs");
            Assert.Equal(generatedSource, VisualStudio.Editor.GetText());

            // Verify that a Fix All in Document in the generated file still does nothing.
            // ⚠ This is a statement of the current behavior, and not a claim regarding correctness of the design.
            // The current behavior is observable; any change to this behavior should be part of an intentional design
            // change.
            VisualStudio.Editor.MoveCaret(generatedSourcePosition);
            VisualStudio.Editor.InvokeCodeActionList();
            VisualStudio.Editor.Verify.CodeAction(
                "Remove Unnecessary Usings",
                applyFix: true,
                fixAllScope: FixAllScope.Document);

            Assert.Equal(generatedSource, VisualStudio.Editor.GetText());

            // Verify that the code action can still be applied manually from within the generated file.
            // This is a regression test for correctness with respect to the design.
            VisualStudio.Editor.MoveCaret(generatedSourcePosition);
            VisualStudio.Editor.InvokeCodeActionList();
            VisualStudio.Editor.Verify.CodeAction(
                "Remove Unnecessary Usings",
                applyFix: true,
                fixAllScope: null);

            Assert.Equal(expectedGeneratedSource, VisualStudio.Editor.GetText());
        }

        [CriticalWpfTheory]
        [InlineData(FixAllScope.Project)]
        [InlineData(FixAllScope.Solution)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        [WorkItem(33507, "https://github.com/dotnet/roslyn/issues/33507")]
        public void FixAllOccurrencesTriggeredFromGeneratedCode(FixAllScope scope)
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

            VisualStudio.SolutionExplorer.AddFile(new ProjectUtils.Project(ProjectName), "D.cs", secondFile, open: false);

            // Switch to the main document we'll be editing
            VisualStudio.SolutionExplorer.OpenFile(new ProjectUtils.Project(ProjectName), "Class1.cs");

            // Verify that applying a Fix All operation does not change generated file, but does change other files.
            // ⚠ This is a statement of the current behavior, and not a claim regarding correctness of the design.
            // The current behavior is observable; any change to this behavior should be part of an intentional design
            // change.
            MarkupTestFile.GetPosition(markup, out var expectedText, out int _);
            SetUpEditor(markup);
            VisualStudio.WaitForApplicationIdle(CancellationToken.None);
            VisualStudio.Editor.InvokeCodeActionList();
            VisualStudio.Editor.Verify.CodeAction(
                "Remove Unnecessary Usings",
                applyFix: true,
                fixAllScope: scope);

            Assert.Equal(expectedText, VisualStudio.Editor.GetText());

            VisualStudio.SolutionExplorer.OpenFile(new ProjectUtils.Project(ProjectName), "D.cs");
            Assert.Equal(expectedSecondFile, VisualStudio.Editor.GetText());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public void ClassificationInPreviewPane()
        {
            SetUpEditor(@"
class Program
{
    int Main()
    {
        Foo$$();
    }
}");
            VisualStudio.Editor.InvokeCodeActionList();
            var classifiedTokens = GetLightbulbPreviewClassification("Generate method 'Program.Foo'");
            Assert.True(classifiedTokens.Any(c => c.Text == "void" && c.Classification == "keyword"));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
        public void AddUsingExactMatchBeforeRenameTracking()
        {
            SetUpEditor(@"
public class Program
{
    static void Main(string[] args)
    {
        P2$$ p;
    }
}

public class P2 { }");

            VisualStudio.Editor.SendKeys(VirtualKey.Backspace, VirtualKey.Backspace, "Stream");

            VisualStudio.Editor.InvokeCodeActionList();
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
                "Configure or Suppress issues",
                // https://github.com/dotnet/roslyn/issues/36330
                //"Configure CS0168 severity",
                //"None",
                //"Silent",
                //"Suggestion",
                //"Warning",
                //"Error",
                "Suppress CS0168",
                "in Source"
            };

            VisualStudio.Editor.Verify.CodeActions(expectedItems, applyFix: expectedItems[0], ensureExpectedItemsAreOrdered: true);
            VisualStudio.Editor.Verify.TextContains("using System.IO;");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public void GFUFuzzyMatchAfterRenameTracking()
        {
            SetUpEditor(@"
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
            VisualStudio.Editor.SendKeys(VirtualKey.Backspace, VirtualKey.Backspace,
                "Foober");

            VisualStudio.Editor.InvokeCodeActionList();
            var expectedItems = new[]
            {
                "Rename 'P2' to 'Foober'",
                "Generate type 'Foober'",
                "Generate class 'Foober' in new file",
                "Generate class 'Foober'",
                "Generate nested class 'Foober'",
                "Generate new type...",
                "Goober - using N;",
                "Configure or Suppress issues",
                // https://github.com/dotnet/roslyn/issues/36330
                //"Configure CS0168 severity",
                //"None",
                //"Silent",
                //"Suggestion",
                //"Warning",
                //"Error",
                "Suppress CS0168",
                "in Source",
            };

            VisualStudio.Editor.Verify.CodeActions(expectedItems, applyFix: expectedItems[0], ensureExpectedItemsAreOrdered: true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public void SuppressionAfterRefactorings()
        {
            SetUpEditor(@"
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
            VisualStudio.Editor.SelectTextInCurrentDocument("2");

            VisualStudio.Editor.InvokeCodeActionList();

            var generateImplicitTitle = "Generate implicit conversion operator in 'C'";
            var expectedItems = new[]
            {
                "Introduce constant for '2'",
                "Introduce constant for all occurrences of '2'",
                "Introduce local constant for '2'",
                "Introduce local constant for all occurrences of '2'",
                "Extract Method",
                generateImplicitTitle,
                "Configure or Suppress issues",
                // https://github.com/dotnet/roslyn/issues/36330
                //"Configure CS0612 severity",
                //"None",
                //"Silent",
                //"Suggestion",
                //"Warning",
                //"Error",
                "Suppress CS0612",
                "in Source",
            };

            VisualStudio.Editor.Verify.CodeActions(expectedItems, applyFix: generateImplicitTitle, ensureExpectedItemsAreOrdered: true);
            VisualStudio.Editor.Verify.TextContains("implicit");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
        public void OrderFixesByCursorProximityLeft()
        {
            SetUpEditor(@"
using System;
public class Program
{
    static void Main(string[] args)
    {
        Byte[] bytes = null;
        GCHandle$$ handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
    }
}");
            VisualStudio.Editor.InvokeCodeActionList();
            var expectedItems = new[]
            {
                "using System.Runtime.InteropServices;",
                "System.Runtime.InteropServices.GCHandle"
            };

            VisualStudio.Editor.Verify.CodeActions(expectedItems, applyFix: expectedItems[0], ensureExpectedItemsAreOrdered: true);
            VisualStudio.Editor.Verify.TextContains("using System.Runtime.InteropServices");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
        public void OrderFixesByCursorProximityRight()
        {
            SetUpEditor(@"
using System;
public class Program
{
    static void Main(string[] args)
    {
        Byte[] bytes = null;
        GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.$$Pinned);
    }
}");
            VisualStudio.Editor.InvokeCodeActionList();
            var expectedItems = new[]
            {
                "using System.Runtime.InteropServices;",
                "System.Runtime.InteropServices.GCHandle"
            };

            VisualStudio.Editor.Verify.CodeActions(expectedItems, applyFix: expectedItems[0], ensureExpectedItemsAreOrdered: true);
            VisualStudio.Editor.Verify.TextContains("using System.Runtime.InteropServices");

        }

        [WpfFact(Skip = "https://github.com/dotnet/roslyn/issues/36330"), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)]
        public void ConfigureCodeStyleOptionValueAndSeverity()
        {
            SetUpEditor(@"
using System;
public class Program
{
    static void Main(string[] args)
    {
        var $$x = new Program();
    }
}");
            VisualStudio.Editor.InvokeCodeActionList();
            var expectedItems = new[]
            {
                "Use discard '__'",  // IDE0059
                "Use explicit type instead of 'var'",   // IDE0008
                "Configure or Suppress issues",
                    "Configure IDE0008 code style",
                        "csharp__style__var__elsewhere",
                            "true",
                            "false",
                        "csharp__style__var__for__built__in__types",
                            "true",
                            "false",
                        "csharp__style__var__when__type__is__apparent",
                            "true",
                            "false",
                    "Configure IDE0008 severity",
                        "None",
                        "Silent",
                        "Suggestion",
                        "Warning",
                        "Error",
                    "Configure IDE0059 code style",
                        "unused__local__variable",
                        "discard__variable",
                    "Configure IDE0059 severity",
                        "None",
                        "Silent",
                        "Suggestion",
                        "Warning",
                        "Error",
                    "Suppress IDE0059",
                        "in Source",
                        "in Suppression File",
                        "in Source (attribute)",
            };

            VisualStudio.Editor.Verify.CodeActions(expectedItems, ensureExpectedItemsAreOrdered: true);
        }
    }
}
