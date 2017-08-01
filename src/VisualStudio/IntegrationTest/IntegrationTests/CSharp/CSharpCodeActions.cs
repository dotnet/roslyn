// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpCodeActions : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        public CSharpCodeActions(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpCodeActions))
        {
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18295"), Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
        public async Task GenerateMethodInClosedFileAsync()
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
            await VisualStudio.Editor.Verify.CodeActionAsync("Generate method 'Foo.Bar'", applyFix: true);
            VisualStudio.SolutionExplorer.Verify.FileContents(project, "Foo.cs", @"
using System;

public class Foo
{
    internal void Bar() => throw new NotImplementedException();
}
");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
        public async Task FastDoubleInvokeAsync()
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
            await VisualStudio.Editor.Verify.CodeActionAsync("using System;", applyFix: true, blockUntilComplete: true);
            VisualStudio.Editor.InvokeCodeActionListWithoutWaiting();
            await VisualStudio.Editor.Verify.CodeActionAsync("Simplify name 'System.ArgumentException'", applyFix: true, blockUntilComplete: true);

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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
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

            SetUpEditor(markup);
            VisualStudio.Editor.InvokeCodeActionList();
            await VisualStudio.Editor.Verify.CodeActionAsync("Delegate invocation can be simplified.", applyFix: true, ensureExpectedItemsAreOrdered: true, blockUntilComplete: true);
            VisualStudio.Editor.PlaceCaret("temp2", 0, 0, extendSelection: false, selectBlock: false);
            VisualStudio.Editor.InvokeCodeActionList();
            await VisualStudio.Editor.Verify.CodeActionAsync("Delegate invocation can be simplified.", applyFix: true, ensureExpectedItemsAreOrdered: true, blockUntilComplete: true);
            VisualStudio.Editor.Verify.TextContains("First?.");
            VisualStudio.Editor.Verify.TextContains("Second?.");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateMethod)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
        public async Task AddUsingExactMatchBeforeRenameTracking()
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
                "System.IO.Stream",
                "Generate class 'Stream' in new file",
                "Generate class 'Stream'",
                "Generate nested class 'Stream'",
                "Generate new type...",
                "Rename 'P2' to 'Stream'",
                "Suppress CS0168",
                "in Source"
            };

            await VisualStudio.Editor.Verify.CodeActionsAsync(expectedItems, applyFix: expectedItems[0], ensureExpectedItemsAreOrdered: true);
            VisualStudio.Editor.Verify.TextContains("using System.IO;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateType)]
        public async Task GFUFuzzyMatchAfterRenameTrackingAsync()
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
                "Suppress CS0168",
                "in Source",
            };

            await VisualStudio.Editor.Verify.CodeActionsAsync(expectedItems, applyFix: expectedItems[0], ensureExpectedItemsAreOrdered: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task SuppressionAfterRefactoringsAsync()
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
            var expectedItems = new[]
            {
                "Generate implicit conversion operator in 'C'",
                "Introduce constant for '2'",
                "Introduce constant for all occurrences of '2'",
                "Introduce local constant for '2'",
                "Introduce local constant for all occurrences of '2'",
                "Extract Method",
                "Suppress CS0612",
                "in Source",
            };

            await VisualStudio.Editor.Verify.CodeActionsAsync(expectedItems, applyFix: expectedItems[0], ensureExpectedItemsAreOrdered: true);
            VisualStudio.Editor.Verify.TextContains("implicit");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
        public async Task OrderFixesByCursorProximityLeftAsync()
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

            await VisualStudio.Editor.Verify.CodeActionsAsync(expectedItems, applyFix: expectedItems[0], ensureExpectedItemsAreOrdered: true);
            VisualStudio.Editor.Verify.TextContains("using System.Runtime.InteropServices");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
        public async Task OrderFixesByCursorProximityRightAsync()
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

            await VisualStudio.Editor.Verify.CodeActionsAsync(expectedItems, applyFix: expectedItems[0], ensureExpectedItemsAreOrdered: true);
            VisualStudio.Editor.Verify.TextContains("using System.Runtime.InteropServices");
        }
    }
}
