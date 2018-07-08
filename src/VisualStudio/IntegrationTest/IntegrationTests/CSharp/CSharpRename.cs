// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Harness;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpRename : AbstractIdeEditorTest
    {
        public CSharpRename()
            : base(nameof(CSharpRename))
        {
        }

        protected override string LanguageName => LanguageNames.CSharp;

        private InlineRenameDialog_InProc2 InlineRenameDialog => VisualStudio.InlineRenameDialog;

        [IdeFact, Trait(Traits.Feature, Traits.Features.Rename)]
        public async Task VerifyLocalVariableRenameAsync()
        {
            var markup = @"
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
}";
            using (var telemetry = await VisualStudio.VisualStudio.EnableTestTelemetryChannelAsync())
            {
                await SetUpEditorAsync(markup);
                await InlineRenameDialog.InvokeAsync();

                MarkupTestFile.GetSpans(markup, out var _, out ImmutableArray<TextSpan> renameSpans);
                var tags = await VisualStudio.Editor.GetTagSpansAsync(InlineRenameDialog.ValidRenameTag);
                AssertEx.SetEqual(renameSpans, tags);

                await VisualStudio.Editor.SendKeysAsync(VirtualKey.Y, VirtualKey.Enter);
                await VisualStudio.Editor.Verify.TextContainsAsync(@"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        int y = 0;
        y = 5;
        TestMethod(y);
    }

    static void TestMethod(int y)
    {

    }
}");
                await telemetry.VerifyFiredAsync("vs/ide/vbcs/rename/inlinesession/session", "vs/ide/vbcs/rename/commitcore");
            }
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Rename)]
        public async Task VerifyLocalVariableRenameWithCommentsUpdatedAsync()
        {
            // "variable" is intentionally misspelled as "varixable" and "this" is misspelled as
            // "thix" below to ensure we don't change instances of "x" in comments that are part of
            // larger words
            var markup = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    /// <summary>
    /// creates a varixable named [|x|] xx
    /// </summary>
    /// <param name=""args""></param>
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
}";
            await SetUpEditorAsync(markup);
            await InlineRenameDialog.InvokeAsync();
            await InlineRenameDialog.ToggleIncludeCommentsAsync();

            MarkupTestFile.GetSpans(markup, out var _, out ImmutableArray<TextSpan> renameSpans);
            var tags = await VisualStudio.Editor.GetTagSpansAsync(InlineRenameDialog.ValidRenameTag);
            AssertEx.SetEqual(renameSpans, tags);

            await VisualStudio.Editor.SendKeysAsync(VirtualKey.Y, VirtualKey.Enter);
            await VisualStudio.Editor.Verify.TextContainsAsync(@"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    /// <summary>
    /// creates a varixable named y xx
    /// </summary>
    /// <param name=""args""></param>
    static void Main(string[] args)
    {
        // thix varixable is named y xx
        int y = 0;
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
}");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Rename)]
        public async Task VerifyLocalVariableRenameWithStringsUpdatedAsync()
        {
            var markup = @"
class Program
{
    static void Main(string[] args)
    {
        int [|x|]$$ = 0;
        [|x|] = 5;
        var s = ""[|x|] xx [|x|]"";
        var sLiteral = 
            @""
            [|x|]
            xx
            [|x|]
            "";
        char c = 'x';
        char cUnit = '\u0078';
    }
}";
            await SetUpEditorAsync(markup);

            await InlineRenameDialog.InvokeAsync();
            await InlineRenameDialog.ToggleIncludeStringsAsync();

            MarkupTestFile.GetSpans(markup, out var _, out ImmutableArray<TextSpan> renameSpans);
            var tags = await VisualStudio.Editor.GetTagSpansAsync(InlineRenameDialog.ValidRenameTag);
            AssertEx.SetEqual(renameSpans, tags);

            await VisualStudio.Editor.SendKeysAsync(VirtualKey.Y, VirtualKey.Enter);
            await VisualStudio.Editor.Verify.TextContainsAsync(@"
class Program
{
    static void Main(string[] args)
    {
        int y = 0;
        y = 5;
        var s = ""y xx y"";
        var sLiteral = 
            @""
            y
            xx
            y
            "";
        char c = 'x';
        char cUnit = '\u0078';
    }
}");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Rename)]
        public async Task VerifyOverloadsUpdatedAsync()
        {
            var markup = @"
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
}";
            await SetUpEditorAsync(markup);

            await InlineRenameDialog.InvokeAsync();
            await InlineRenameDialog.ToggleIncludeOverloadsAsync();

            MarkupTestFile.GetSpans(markup, out var _, out ImmutableArray<TextSpan> renameSpans);
            var tags = await VisualStudio.Editor.GetTagSpansAsync(InlineRenameDialog.ValidRenameTag);
            AssertEx.SetEqual(renameSpans, tags);

            await VisualStudio.Editor.SendKeysAsync(VirtualKey.Y, VirtualKey.Enter);
            await VisualStudio.Editor.Verify.TextContainsAsync(@"
interface I
{
    void y(int y);
    void y(string y);
}

class B : I
{
    public virtual void y(int y)
    { }

    public virtual void y(string y)
    { }
}");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Rename)]
        public async Task VerifyMultiFileRenameAsync()
        {
            await SetUpEditorAsync(@"
class $$Program
{
}");
            var project = ProjectName;
            await VisualStudio.SolutionExplorer.AddFileAsync(project, "Class2.cs", @"");
            await VisualStudio.SolutionExplorer.OpenFileAsync(project, "Class2.cs");

            const string class2Markup = @"
class SomeOtherClass
{
    void M()
    {
        [|Program|] p = new [|Program|]();
    }
}";
            MarkupTestFile.GetSpans(class2Markup, out var code, out ImmutableArray<TextSpan> renameSpans);

            await VisualStudio.Editor.SetTextAsync(code);
            await VisualStudio.Editor.PlaceCaretAsync("Program");

            await InlineRenameDialog.InvokeAsync();

            var tags = await VisualStudio.Editor.GetTagSpansAsync(InlineRenameDialog.ValidRenameTag);
            AssertEx.SetEqual(renameSpans, tags);

            await VisualStudio.Editor.SendKeysAsync(VirtualKey.Y, VirtualKey.Enter);
            await VisualStudio.Editor.Verify.TextContainsAsync(@"
class SomeOtherClass
{
    void M()
    {
        y p = new y();
    }
}");

            await VisualStudio.SolutionExplorer.OpenFileAsync(project, "Class1.cs");
            await VisualStudio.Editor.Verify.TextContainsAsync(@"
class y
{
}");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Rename)]
        public async Task VerifyRenameCancellationAsync()
        {
            await SetUpEditorAsync(@"
class $$Program
{
}");

            var project = ProjectName;
            await VisualStudio.SolutionExplorer.AddFileAsync(project, "Class2.cs", @"");
            await VisualStudio.SolutionExplorer.OpenFileAsync(project, "Class2.cs");
            await VisualStudio.Editor.SetTextAsync(@"
class SomeOtherClass
{
    void M()
    {
        Program p = new Program();
    }
}");
            await VisualStudio.Editor.PlaceCaretAsync("Program");

            await InlineRenameDialog.InvokeAsync();

            await VisualStudio.Editor.SendKeysAsync(VirtualKey.Y);
            await VisualStudio.Editor.Verify.TextContainsAsync(@"class SomeOtherClass
{
    void M()
    {
        y p = new y();
    }
}");

            await VisualStudio.SolutionExplorer.OpenFileAsync(project, "Class1.cs");
            await VisualStudio.Editor.Verify.TextContainsAsync(@"
class y
{
}");

            await VisualStudio.Editor.SendKeysAsync(VirtualKey.Escape);
            await VisualStudio.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Rename);

            await VisualStudio.Editor.Verify.TextContainsAsync(@"
class Program
{
}");

            await VisualStudio.SolutionExplorer.OpenFileAsync(project, "Class2.cs");
            await VisualStudio.Editor.Verify.TextContainsAsync(@"
class SomeOtherClass
{
    void M()
    {
        Program p = new Program();
    }
}");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Rename)]
        public async Task VerifyCrossProjectRenameAsync()
        {
            await SetUpEditorAsync(@"
$$class RenameRocks 
{
    static void Main(string[] args)
    {
        Class2 c = null;
        c.ToString();
    }
}");
            var project1 = ProjectName;
            var project2 = "Project2";

            await VisualStudio.SolutionExplorer.AddProjectAsync(project2, WellKnownProjectTemplates.ClassLibrary, LanguageName);
            VisualStudio.SolutionExplorer.AddProjectReference(projectName: project1, projectToReferenceName: "Project2");

            await VisualStudio.SolutionExplorer.AddFileAsync(project2, "Class2.cs", @"");
            await VisualStudio.SolutionExplorer.OpenFileAsync(project2, "Class2.cs");


            await VisualStudio.Editor.SetTextAsync(@"
public class Class2 { static void Main(string [] args) { } }");

            await VisualStudio.SolutionExplorer.OpenFileAsync(project1, "Class1.cs");
            await VisualStudio.Editor.PlaceCaretAsync("Class2");

            await InlineRenameDialog.InvokeAsync();
            await VisualStudio.Editor.SendKeysAsync(VirtualKey.Y, VirtualKey.Enter);

            await VisualStudio.Editor.Verify.TextContainsAsync(@"
class RenameRocks 
{
    static void Main(string[] args)
    {
        y c = null;
        c.ToString();
    }
}");

            await VisualStudio.SolutionExplorer.OpenFileAsync(project2, "Class2.cs");
            await VisualStudio.Editor.Verify.TextContainsAsync(@"
public class y { static void Main(string [] args) { } }");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Rename)]
        public async Task VerifyRenameUndoAsync()
        {
            await VerifyCrossProjectRenameAsync();

            await VisualStudio.Editor.SendKeysAsync(Ctrl(VirtualKey.Z));

            await VisualStudio.Editor.Verify.TextContainsAsync(@"
public class Class2 { static void Main(string [] args) { } }");

            await VisualStudio.SolutionExplorer.OpenFileAsync(ProjectName, "Class1.cs");
            await VisualStudio.Editor.Verify.TextContainsAsync(@"
class RenameRocks 
{
    static void Main(string[] args)
    {
        Class2 c = null;
        c.ToString();
    }
}");
        }

        [IdeFact, Trait(Traits.Feature, Traits.Features.Rename)]
        public async Task VerifyRenameInStandaloneFilesAsync()
        {
            await VisualStudio.SolutionExplorer.CloseSolutionAsync();
            await VisualStudio.SolutionExplorer.AddStandaloneFileAsync("StandaloneFile1.cs");
            await VisualStudio.Editor.SetTextAsync(@"
class Program
{
    void Goo()
    {
        var ids = 1;
        ids = 2;
    }
}");
            await VisualStudio.Editor.PlaceCaretAsync("ids");

            await InlineRenameDialog.InvokeAsync();

            await VisualStudio.Editor.SendKeysAsync(VirtualKey.Y, VirtualKey.Enter);

            await VisualStudio.Editor.Verify.TextContainsAsync(@"
class Program
{
    void Goo()
    {
        var y = 1;
        y = 2;
    }
}");
        }
    }
}
