// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;
using WorkItemAttribute = Roslyn.Test.Utilities.WorkItemAttribute;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpRename : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        private InlineRenameDialog_OutOfProc InlineRenameDialog => VisualStudioInstance.InlineRenameDialog;

        public CSharpRename() : base(nameof(CSharpRename))
        {
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.Rename)]
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
            using (var telemetry = VisualStudioInstance.EnableTestTelemetryChannel())
            {
                SetUpEditor(markup);
                InlineRenameDialog.Invoke();

                MarkupTestFile.GetSpans(markup, out var _, out ImmutableArray<TextSpan> renameSpans);
                var tags = VisualStudioInstance.Editor.GetTagSpans(InlineRenameDialog.ValidRenameTag);
                AssertEx.SetEqual(renameSpans, tags);

                VisualStudioInstance.Editor.SendKeys(VirtualKey.Y, VirtualKey.Enter);
                VisualStudioInstance.Editor.Verify.TextContains(@"
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

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.Rename)]
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
            var tags = VisualStudioInstance.Editor.GetTagSpans(InlineRenameDialog.ValidRenameTag);
            AssertEx.SetEqual(renameSpans, tags);

            VisualStudioInstance.Editor.SendKeys("Custom", VirtualKey.Enter);
            VisualStudioInstance.Editor.Verify.TextContains(@"
using System;

class CustomAttribute : Attribute
{
}");
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.Rename)]
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
            var tags = VisualStudioInstance.Editor.GetTagSpans(InlineRenameDialog.ValidRenameTag);
            AssertEx.SetEqual(renameSpans, tags);

            VisualStudioInstance.Editor.SendKeys("Custom");
            VisualStudioInstance.Editor.Verify.TextContains(@"
using System;

class Custom$$Attribute : Attribute
{
}
", true);
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.Rename)]
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
            var tags = VisualStudioInstance.Editor.GetTagSpans(InlineRenameDialog.ValidRenameTag);

            VisualStudioInstance.Editor.SendKeys("Custom");
            VisualStudioInstance.Editor.Verify.TextContains(@"
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

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.Rename)]
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
            var tags = VisualStudioInstance.Editor.GetTagSpans(InlineRenameDialog.ValidRenameTag);

            VisualStudioInstance.Editor.SendKeys("Custom");
            VisualStudioInstance.Editor.Verify.TextContains(@"
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

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.Rename)]
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
            var tags = VisualStudioInstance.Editor.GetTagSpans(InlineRenameDialog.ValidRenameTag);
            AssertEx.SetEqual(renameSpans, tags);

            VisualStudioInstance.Editor.SendKeys(VirtualKey.Y, VirtualKey.Enter);
            VisualStudioInstance.Editor.Verify.TextContains(@"
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

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.Rename)]
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
            var tags = VisualStudioInstance.Editor.GetTagSpans(InlineRenameDialog.ValidRenameTag);
            AssertEx.SetEqual(renameSpans, tags);

            VisualStudioInstance.Editor.SendKeys(VirtualKey.Y, VirtualKey.Enter);
            VisualStudioInstance.Editor.Verify.TextContains(@"
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

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.Rename)]
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
            var tags = VisualStudioInstance.Editor.GetTagSpans(InlineRenameDialog.ValidRenameTag);
            AssertEx.SetEqual(renameSpans, tags);

            VisualStudioInstance.Editor.SendKeys(VirtualKey.Y, VirtualKey.Enter);
            VisualStudioInstance.Editor.Verify.TextContains(@"
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

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.Rename)]
        public void VerifyMultiFileRename()
        {
            SetUpEditor(@"
class $$Program
{
}");
            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.AddFile(project, "Class2.cs", @"");
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "Class2.cs");

            const string class2Markup = @"
class SomeOtherClass
{
    void M()
    {
        [|Program|] p = new [|Program|]();
    }
}";
            MarkupTestFile.GetSpans(class2Markup, out var code, out ImmutableArray<TextSpan> renameSpans);

            VisualStudioInstance.Editor.SetText(code);
            VisualStudioInstance.Editor.PlaceCaret("Program");

            InlineRenameDialog.Invoke();

            var tags = VisualStudioInstance.Editor.GetTagSpans(InlineRenameDialog.ValidRenameTag);
            AssertEx.SetEqual(renameSpans, tags);

            VisualStudioInstance.Editor.SendKeys(VirtualKey.Y, VirtualKey.Enter);
            VisualStudioInstance.Editor.Verify.TextContains(@"
class SomeOtherClass
{
    void M()
    {
        y p = new y();
    }
}");

            VisualStudioInstance.SolutionExplorer.OpenFile(project, "Class1.cs");
            VisualStudioInstance.Editor.Verify.TextContains(@"
class y
{
}");
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.Rename)]
        public void VerifyRenameCancellation()
        {
            SetUpEditor(@"
class $$Program
{
}");

            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.AddFile(project, "Class2.cs", @"");
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "Class2.cs");
            VisualStudioInstance.Editor.SetText(@"
class SomeOtherClass
{
    void M()
    {
        Program p = new Program();
    }
}");
            VisualStudioInstance.Editor.PlaceCaret("Program");

            InlineRenameDialog.Invoke();

            VisualStudioInstance.Editor.SendKeys(VirtualKey.Y);
            VisualStudioInstance.Editor.Verify.TextContains(@"class SomeOtherClass
{
    void M()
    {
        y p = new y();
    }
}");

            VisualStudioInstance.SolutionExplorer.OpenFile(project, "Class1.cs");
            VisualStudioInstance.Editor.Verify.TextContains(@"
class y
{
}");

            VisualStudioInstance.Editor.SendKeys(VirtualKey.Escape);
            VisualStudioInstance.Workspace.WaitForAsyncOperations(FeatureAttribute.Rename);

            VisualStudioInstance.Editor.Verify.TextContains(@"
class Program
{
}");

            VisualStudioInstance.SolutionExplorer.OpenFile(project, "Class2.cs");
            VisualStudioInstance.Editor.Verify.TextContains(@"
class SomeOtherClass
{
    void M()
    {
        Program p = new Program();
    }
}");
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.Rename)]
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

            VisualStudioInstance.SolutionExplorer.AddProject(project2, WellKnownProjectTemplates.ClassLibrary, LanguageName);
            VisualStudioInstance.SolutionExplorer.AddProjectReference(fromProjectName: project1, toProjectName: new ProjectUtils.ProjectReference("Project2"));

            VisualStudioInstance.SolutionExplorer.AddFile(project2, "Class2.cs", @"");
            VisualStudioInstance.SolutionExplorer.OpenFile(project2, "Class2.cs");


            VisualStudioInstance.Editor.SetText(@"
public class Class2 { static void Main(string [] args) { } }");

            VisualStudioInstance.SolutionExplorer.OpenFile(project1, "Class1.cs");
            VisualStudioInstance.Editor.PlaceCaret("Class2");

            InlineRenameDialog.Invoke();
            VisualStudioInstance.Editor.SendKeys(VirtualKey.Y, VirtualKey.Enter);

            VisualStudioInstance.Editor.Verify.TextContains(@"
class RenameRocks 
{
    static void Main(string[] args)
    {
        y c = null;
        c.ToString();
    }
}");

            VisualStudioInstance.SolutionExplorer.OpenFile(project2, "Class2.cs");
            VisualStudioInstance.Editor.Verify.TextContains(@"
public class y { static void Main(string [] args) { } }");
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.Rename)]
        public void VerifyRenameUndo()
        {
            VerifyCrossProjectRename();

            VisualStudioInstance.Editor.SendKeys(Ctrl(VirtualKey.Z));

            VisualStudioInstance.Editor.Verify.TextContains(@"
public class Class2 { static void Main(string [] args) { } }");

            VisualStudioInstance.SolutionExplorer.OpenFile(new ProjectUtils.Project(ProjectName), "Class1.cs");
            VisualStudioInstance.Editor.Verify.TextContains(@"
class RenameRocks 
{
    static void Main(string[] args)
    {
        Class2 c = null;
        c.ToString();
    }
}");
        }

        [TestMethod, TestProperty(Traits.Feature, Traits.Features.Rename)]
        public void VerifyRenameInStandaloneFiles()
        {
            VisualStudioInstance.SolutionExplorer.CloseSolution();
            VisualStudioInstance.SolutionExplorer.AddStandaloneFile("StandaloneFile1.cs");
            VisualStudioInstance.Editor.SetText(@"
class Program
{
    void Goo()
    {
        var ids = 1;
        ids = 2;
    }
}");
            VisualStudioInstance.Editor.PlaceCaret("ids");

            InlineRenameDialog.Invoke();

            VisualStudioInstance.Editor.SendKeys(VirtualKey.Y, VirtualKey.Enter);

            VisualStudioInstance.Editor.Verify.TextContains(@"
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
