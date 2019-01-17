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
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;
using WorkItemAttribute = Roslyn.Test.Utilities.WorkItemAttribute;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpCodeActions : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpCodeActions()
            : base(nameof(CSharpCodeActions))
        {
        }

        [Ignore("https://github.com/dotnet/roslyn/issues/26204"), TestProperty(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public void GenerateMethodInClosedFile()
        {
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.AddFile(project, "Foo.cs", contents: @"
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

            VisualStudioInstance.Editor.InvokeCodeActionList();
            VisualStudioInstance.Editor.Verify.CodeAction("Generate method 'Foo.Bar'", applyFix: true);
            VisualStudioInstance.SolutionExplorer.Verify.FileContents(project, "Foo.cs", @"
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

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.CodeActionsAddImport)]
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
            VisualStudioInstance.Editor.InvokeCodeActionList();
            VisualStudioInstance.Editor.Verify.CodeAction("using System;", applyFix: true, blockUntilComplete: true);
            VisualStudioInstance.Editor.InvokeCodeActionListWithoutWaiting();
            VisualStudioInstance.Editor.Verify.CodeAction("Simplify name 'System.ArgumentException'", applyFix: true, blockUntilComplete: true);

            VisualStudioInstance.Editor.Verify.TextContains(
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

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
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
            VisualStudioInstance.Editor.InvokeCodeActionList();
            VisualStudioInstance.Editor.Verify.CodeAction("Delegate invocation can be simplified.", applyFix: true, ensureExpectedItemsAreOrdered: true, blockUntilComplete: true);
            VisualStudioInstance.Editor.PlaceCaret("temp2", 0, 0, extendSelection: false, selectBlock: false);
            VisualStudioInstance.Editor.InvokeCodeActionList();
            VisualStudioInstance.Editor.Verify.CodeAction("Delegate invocation can be simplified.", applyFix: true, ensureExpectedItemsAreOrdered: true, blockUntilComplete: true);
            VisualStudioInstance.Editor.Verify.TextContains("First?.");
            VisualStudioInstance.Editor.Verify.TextContains("Second?.");
        }

        [Ignore("https://github.com/dotnet/roslyn/issues/30015")]
        [TestProperty(Traits.Feature, Traits.Features.EditorConfig)]
        [TestProperty(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
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
            VisualStudioInstance.SolutionExplorer.OpenFile(new ProjectUtils.Project(ProjectName), @"Properties\AssemblyInfo.cs");

            // Switch back to the main document we'll be editing
            VisualStudioInstance.SolutionExplorer.OpenFile(new ProjectUtils.Project(ProjectName), "Class1.cs");

            /*
             * The first portion of this test adds a .editorconfig file to configure the analyzer behavior, and verifies
             * that diagnostics appear automatically in response to the newly-created file. A fix all operation is
             * applied, and the result is verified against the expected outcome for the .editorconfig style.
             */

            MarkupTestFile.GetSpans(markup, out var text, out ImmutableArray<TextSpan> spans);
            SetUpEditor(markup);
            VisualStudioInstance.WaitForApplicationIdle(CancellationToken.None);
            VisualStudioInstance.Editor.Verify.CodeActionsNotShowing();

            var editorConfig = @"root = true

[*.cs]
csharp_style_expression_bodied_properties = true:warning
";

            VisualStudioInstance.SolutionExplorer.AddFile(new ProjectUtils.Project(ProjectName), ".editorconfig", editorConfig, open: false);

            // Wait for CodingConventions library events to propagate to the workspace
            VisualStudioInstance.WaitForApplicationIdle(CancellationToken.None);
            VisualStudioInstance.Workspace.WaitForAllAsyncOperations(
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawler,
                FeatureAttribute.DiagnosticService);
            VisualStudioInstance.Editor.InvokeCodeActionList();
            VisualStudioInstance.Editor.Verify.CodeAction(
                "Use expression body for properties",
                applyFix: true,
                fixAllScope: FixAllScope.Project);

            Assert.AreEqual(expectedText, VisualStudioInstance.Editor.GetText());

            /*
             * The second portion of this test modifier the existing .editorconfig file to configure the analyzer to the
             * opposite style of the initial configuration, and verifies that diagnostics update automatically in
             * response to the changes. A fix all operation is applied, and the result is verified against the expected
             * outcome for the modified .editorconfig style.
             */

            VisualStudioInstance.SolutionExplorer.SetFileContents(new ProjectUtils.Project(ProjectName), ".editorconfig", editorConfig.Replace("true:warning", "false:warning"));

            // Wait for CodingConventions library events to propagate to the workspace
            VisualStudioInstance.WaitForApplicationIdle(CancellationToken.None);
            VisualStudioInstance.Workspace.WaitForAllAsyncOperations(
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawler,
                FeatureAttribute.DiagnosticService);
            VisualStudioInstance.Editor.InvokeCodeActionList();
            VisualStudioInstance.Editor.Verify.CodeAction(
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

            Assert.AreEqual(expectedText, VisualStudioInstance.Editor.GetText());
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
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
            VisualStudioInstance.Editor.InvokeCodeActionList();
            var classifiedTokens = GetLightbulbPreviewClassification("Generate method 'Program.Foo'");
            Assert.IsTrue(classifiedTokens.Any(c => c.Text == "void" && c.Classification == "keyword"));
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.CodeActionsAddImport)]
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

            VisualStudioInstance.Editor.SendKeys(VirtualKey.Backspace, VirtualKey.Backspace, "Stream");

            VisualStudioInstance.Editor.InvokeCodeActionList();
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

            VisualStudioInstance.Editor.Verify.CodeActions(expectedItems, applyFix: expectedItems[0], ensureExpectedItemsAreOrdered: true);
            VisualStudioInstance.Editor.Verify.TextContains("using System.IO;");
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
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
            VisualStudioInstance.Editor.SendKeys(VirtualKey.Backspace, VirtualKey.Backspace,
                "Foober");

            VisualStudioInstance.Editor.InvokeCodeActionList();
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

            VisualStudioInstance.Editor.Verify.CodeActions(expectedItems, applyFix: expectedItems[0], ensureExpectedItemsAreOrdered: true);
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.CodeGeneration)]
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
            VisualStudioInstance.Editor.SelectTextInCurrentDocument("2");

            VisualStudioInstance.Editor.InvokeCodeActionList();

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

            VisualStudioInstance.Editor.Verify.CodeActions(expectedItems, applyFix: generateImplicitTitle, ensureExpectedItemsAreOrdered: true);
            VisualStudioInstance.Editor.Verify.TextContains("implicit");
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.CodeActionsAddImport)]
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
            VisualStudioInstance.Editor.InvokeCodeActionList();
            var expectedItems = new[]
            {
                "using System.Runtime.InteropServices;",
                "System.Runtime.InteropServices.GCHandle"
            };

            VisualStudioInstance.Editor.Verify.CodeActions(expectedItems, applyFix: expectedItems[0], ensureExpectedItemsAreOrdered: true);
            VisualStudioInstance.Editor.Verify.TextContains("using System.Runtime.InteropServices");
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.CodeActionsAddImport)]
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
            VisualStudioInstance.Editor.InvokeCodeActionList();
            var expectedItems = new[]
            {
                "using System.Runtime.InteropServices;",
                "System.Runtime.InteropServices.GCHandle"
            };

            VisualStudioInstance.Editor.Verify.CodeActions(expectedItems, applyFix: expectedItems[0], ensureExpectedItemsAreOrdered: true);
            VisualStudioInstance.Editor.Verify.TextContains("using System.Runtime.InteropServices");

        }
    }
}
