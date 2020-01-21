// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpRename : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        private InlineRenameDialog_OutOfProc InlineRenameDialog => VisualStudio.InlineRenameDialog;

        public CSharpRename(VisualStudioInstanceFactory instanceFactory, ITestOutputHelper testOutputHelper)
            : base(instanceFactory, testOutputHelper, nameof(CSharpRename))
        {
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Rename)]
        public void VerifyLocalVariableRename()
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
            using (var telemetry = VisualStudio.EnableTestTelemetryChannel())
            {
                SetUpEditor(markup);
                InlineRenameDialog.Invoke();

                MarkupTestFile.GetSpans(markup, out var _, out ImmutableArray<TextSpan> renameSpans);
                var tags = VisualStudio.Editor.GetTagSpans(InlineRenameDialog.ValidRenameTag);
                AssertEx.SetEqual(renameSpans, tags);

                VisualStudio.Editor.SendKeys(VirtualKey.Y, VirtualKey.Enter);
                VisualStudio.Editor.Verify.TextContains(@"
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
                telemetry.VerifyFired("vs/ide/vbcs/rename/inlinesession/session", "vs/ide/vbcs/rename/commitcore");
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Rename)]
        [WorkItem(21657, "https://github.com/dotnet/roslyn/issues/21657")]
        public void VerifyAttributeRename()
        {
            var markup = @"
using System;

class [|$$ustom|]Attribute : Attribute
{
}
";
            SetUpEditor(markup);
            InlineRenameDialog.Invoke();

            MarkupTestFile.GetSpans(markup, out var _, out ImmutableArray<TextSpan> renameSpans);
            var tags = VisualStudio.Editor.GetTagSpans(InlineRenameDialog.ValidRenameTag);
            AssertEx.SetEqual(renameSpans, tags);

            VisualStudio.Editor.SendKeys("Custom", VirtualKey.Enter);
            VisualStudio.Editor.Verify.TextContains(@"
using System;

class CustomAttribute : Attribute
{
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Rename)]
        [WorkItem(21657, "https://github.com/dotnet/roslyn/issues/21657")]
        public void VerifyAttributeRenameWhileRenameClasss()
        {
            var markup = @"
using System;

class [|$$stom|]Attribute : Attribute
{
}
";
            SetUpEditor(markup);
            InlineRenameDialog.Invoke();

            MarkupTestFile.GetSpans(markup, out var _, out ImmutableArray<TextSpan> renameSpans);
            var tags = VisualStudio.Editor.GetTagSpans(InlineRenameDialog.ValidRenameTag);
            AssertEx.SetEqual(renameSpans, tags);

            VisualStudio.Editor.SendKeys("Custom");
            VisualStudio.Editor.Verify.TextContains(@"
using System;

class Custom$$Attribute : Attribute
{
}
", true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Rename)]
        [WorkItem(21657, "https://github.com/dotnet/roslyn/issues/21657")]
        public void VerifyAttributeRenameWhileRenameAttribute()
        {
            var markup = @"
using System;

[[|$$stom|]]
class Bar 
{
}

class stomAttribute : Attribute
{
}
";
            SetUpEditor(markup);
            InlineRenameDialog.Invoke();

            MarkupTestFile.GetSpans(markup, out var _, out ImmutableArray<TextSpan> renameSpans);
            var tags = VisualStudio.Editor.GetTagSpans(InlineRenameDialog.ValidRenameTag);

            VisualStudio.Editor.SendKeys("Custom");
            VisualStudio.Editor.Verify.TextContains(@"
using System;

[Custom$$]
class Bar 
{
}

class CustomAttribute : Attribute
{
}
", true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Rename)]
        [WorkItem(21657, "https://github.com/dotnet/roslyn/issues/21657")]
        public void VerifyAttributeRenameWhileRenameAttributeClass()
        {
            var markup = @"
using System;

[stom]
class Bar 
{
}

class [|$$stom|]Attribute : Attribute
{
}
";
            SetUpEditor(markup);
            InlineRenameDialog.Invoke();

            MarkupTestFile.GetSpans(markup, out var _, out ImmutableArray<TextSpan> renameSpans);
            var tags = VisualStudio.Editor.GetTagSpans(InlineRenameDialog.ValidRenameTag);

            VisualStudio.Editor.SendKeys("Custom");
            VisualStudio.Editor.Verify.TextContains(@"
using System;

[Custom]
class Bar 
{
}

class Custom$$Attribute : Attribute
{
}
", true);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Rename)]
        public void VerifyLocalVariableRenameWithCommentsUpdated()
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
            SetUpEditor(markup);
            InlineRenameDialog.Invoke();
            InlineRenameDialog.ToggleIncludeComments();

            MarkupTestFile.GetSpans(markup, out var _, out ImmutableArray<TextSpan> renameSpans);
            var tags = VisualStudio.Editor.GetTagSpans(InlineRenameDialog.ValidRenameTag);
            AssertEx.SetEqual(renameSpans, tags);

            VisualStudio.Editor.SendKeys(VirtualKey.Y, VirtualKey.Enter);
            VisualStudio.Editor.Verify.TextContains(@"
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Rename)]
        public void VerifyLocalVariableRenameWithStringsUpdated()
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
            SetUpEditor(markup);

            InlineRenameDialog.Invoke();
            InlineRenameDialog.ToggleIncludeStrings();

            MarkupTestFile.GetSpans(markup, out var _, out ImmutableArray<TextSpan> renameSpans);
            var tags = VisualStudio.Editor.GetTagSpans(InlineRenameDialog.ValidRenameTag);
            AssertEx.SetEqual(renameSpans, tags);

            VisualStudio.Editor.SendKeys(VirtualKey.Y, VirtualKey.Enter);
            VisualStudio.Editor.Verify.TextContains(@"
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Rename)]
        public void VerifyOverloadsUpdated()
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
            SetUpEditor(markup);

            InlineRenameDialog.Invoke();
            InlineRenameDialog.ToggleIncludeOverloads();

            MarkupTestFile.GetSpans(markup, out var _, out ImmutableArray<TextSpan> renameSpans);
            var tags = VisualStudio.Editor.GetTagSpans(InlineRenameDialog.ValidRenameTag);
            AssertEx.SetEqual(renameSpans, tags);

            VisualStudio.Editor.SendKeys(VirtualKey.Y, VirtualKey.Enter);
            VisualStudio.Editor.Verify.TextContains(@"
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.Rename)]
        public void VerifyMultiFileRename()
        {
            SetUpEditor(@"
class $$Program
{
}");
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.AddFile(project, "Class2.cs", @"");
            VisualStudio.SolutionExplorer.OpenFile(project, "Class2.cs");

            const string class2Markup = @"
class SomeOtherClass
{
    void M()
    {
        [|Program|] p = new [|Program|]();
    }
}";
            MarkupTestFile.GetSpans(class2Markup, out var code, out ImmutableArray<TextSpan> renameSpans);

            VisualStudio.Editor.SetText(code);
            VisualStudio.Editor.PlaceCaret("Program");

            InlineRenameDialog.Invoke();

            var tags = VisualStudio.Editor.GetTagSpans(InlineRenameDialog.ValidRenameTag);
            AssertEx.SetEqual(renameSpans, tags);

            VisualStudio.Editor.SendKeys(VirtualKey.Y, VirtualKey.Enter);
            VisualStudio.Editor.Verify.TextContains(@"
class SomeOtherClass
{
    void M()
    {
        y p = new y();
    }
}");

            VisualStudio.SolutionExplorer.OpenFile(project, "Class1.cs");
            VisualStudio.Editor.Verify.TextContains(@"
class y
{
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Rename)]
        public void VerifyRenameCancellation()
        {
            SetUpEditor(@"
class $$Program
{
}");

            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.SolutionExplorer.AddFile(project, "Class2.cs", @"");
            VisualStudio.SolutionExplorer.OpenFile(project, "Class2.cs");
            VisualStudio.Editor.SetText(@"
class SomeOtherClass
{
    void M()
    {
        Program p = new Program();
    }
}");
            VisualStudio.Editor.PlaceCaret("Program");

            InlineRenameDialog.Invoke();

            VisualStudio.Editor.SendKeys(VirtualKey.Y);
            VisualStudio.Editor.Verify.TextContains(@"class SomeOtherClass
{
    void M()
    {
        y p = new y();
    }
}");

            VisualStudio.SolutionExplorer.OpenFile(project, "Class1.cs");
            VisualStudio.Editor.Verify.TextContains(@"
class y
{
}");

            VisualStudio.Editor.SendKeys(VirtualKey.Escape);
            VisualStudio.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Rename);

            VisualStudio.Editor.Verify.TextContains(@"
class Program
{
}");

            VisualStudio.SolutionExplorer.OpenFile(project, "Class2.cs");
            VisualStudio.Editor.Verify.TextContains(@"
class SomeOtherClass
{
    void M()
    {
        Program p = new Program();
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Rename)]
        public void VerifyCrossProjectRename()
        {
            SetUpEditor(@"
$$class RenameRocks 
{
    static void Main(string[] args)
    {
        Class2 c = null;
        c.ToString();
    }
}");
            var project1 = new ProjectUtils.Project(ProjectName);
            var project2 = new ProjectUtils.Project("Project2");

            VisualStudio.SolutionExplorer.AddProject(project2, WellKnownProjectTemplates.ClassLibrary, LanguageName);
            VisualStudio.SolutionExplorer.AddProjectReference(fromProjectName: project1, toProjectName: new ProjectUtils.ProjectReference("Project2"));

            VisualStudio.SolutionExplorer.AddFile(project2, "Class2.cs", @"");
            VisualStudio.SolutionExplorer.OpenFile(project2, "Class2.cs");


            VisualStudio.Editor.SetText(@"
public class Class2 { static void Main(string [] args) { } }");

            VisualStudio.SolutionExplorer.OpenFile(project1, "Class1.cs");
            VisualStudio.Editor.PlaceCaret("Class2");

            InlineRenameDialog.Invoke();
            VisualStudio.Editor.SendKeys(VirtualKey.Y, VirtualKey.Enter);

            VisualStudio.Editor.Verify.TextContains(@"
class RenameRocks 
{
    static void Main(string[] args)
    {
        y c = null;
        c.ToString();
    }
}");

            VisualStudio.SolutionExplorer.OpenFile(project2, "Class2.cs");
            VisualStudio.Editor.Verify.TextContains(@"
public class y { static void Main(string [] args) { } }");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Rename)]
        public void VerifyRenameUndo()
        {
            VerifyCrossProjectRename();

            VisualStudio.Editor.SendKeys(Ctrl(VirtualKey.Z));

            VisualStudio.Editor.Verify.TextContains(@"
public class Class2 { static void Main(string [] args) { } }");

            VisualStudio.SolutionExplorer.OpenFile(new ProjectUtils.Project(ProjectName), "Class1.cs");
            VisualStudio.Editor.Verify.TextContains(@"
class RenameRocks 
{
    static void Main(string[] args)
    {
        Class2 c = null;
        c.ToString();
    }
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Rename)]
        public void VerifyRenameInStandaloneFiles()
        {
            VisualStudio.SolutionExplorer.CloseSolution();
            VisualStudio.SolutionExplorer.AddStandaloneFile("StandaloneFile1.cs");
            VisualStudio.Editor.SetText(@"
class Program
{
    void Goo()
    {
        var ids = 1;
        ids = 2;
    }
}");
            VisualStudio.Editor.PlaceCaret("ids");

            InlineRenameDialog.Invoke();

            VisualStudio.Editor.SendKeys(VirtualKey.Y, VirtualKey.Enter);

            VisualStudio.Editor.Verify.TextContains(@"
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
