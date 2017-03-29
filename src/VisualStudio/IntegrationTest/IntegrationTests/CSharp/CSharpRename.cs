// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.IntegrationTests.Extensions;
using Roslyn.VisualStudio.IntegrationTests.Extensions.Editor;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class CSharpRename : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        private InlineRenameDialog_OutOfProc InlineRenameDialog => VisualStudio.Instance.InlineRenameDialog;

        public CSharpRename(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory, nameof(CSharpRename))
        {
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Rename)]
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
            SetUpEditor(markup);
            InlineRenameDialog.Invoke();

            MarkupTestFile.GetSpans(markup, out var _, out ImmutableArray<TextSpan> renameSpans);
            var tags = Editor.GetTagSpans(InlineRenameDialog.ValidRenameTag);
            AssertEx.SetEqual(renameSpans, tags);

            Editor.SendKeys(VirtualKey.Y, VirtualKey.Enter);
            this.VerifyTextContains(@"
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Rename)]
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
            var tags = Editor.GetTagSpans(InlineRenameDialog.ValidRenameTag);
            AssertEx.SetEqual(renameSpans, tags);

            Editor.SendKeys(VirtualKey.Y, VirtualKey.Enter);
            this.VerifyTextContains(@"
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

        [Fact, Trait(Traits.Feature, Traits.Features.Rename)]
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
            var tags = Editor.GetTagSpans(InlineRenameDialog.ValidRenameTag);
            AssertEx.SetEqual(renameSpans, tags);

            Editor.SendKeys(VirtualKey.Y, VirtualKey.Enter);
            this.VerifyTextContains(@"
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

        [Fact, Trait(Traits.Feature, Traits.Features.Rename)]
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
            var tags = Editor.GetTagSpans(InlineRenameDialog.ValidRenameTag);
            AssertEx.SetEqual(renameSpans, tags);

            Editor.SendKeys(VirtualKey.Y, VirtualKey.Enter);
            this.VerifyTextContains(@"
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

        [Fact, Trait(Traits.Feature, Traits.Features.Rename)]
        public void VerifyMultiFileRename()
        {
            SetUpEditor(@"
class $$Program
{
}");

            VisualStudio.Instance.SolutionExplorer.AddFile(ProjectName, "Class2.cs", @"");
            VisualStudio.Instance.SolutionExplorer.OpenFile(ProjectName, "Class2.cs");

            const string class2Markup = @"
class SomeOtherClass
{
    void M()
    {
        [|Program|] p = new [|Program|]();
    }
}";
            MarkupTestFile.GetSpans(class2Markup, out var code, out ImmutableArray<TextSpan> renameSpans);

            Editor.SetText(code);
            this.PlaceCaret("Program");

            InlineRenameDialog.Invoke();

            var tags = Editor.GetTagSpans(InlineRenameDialog.ValidRenameTag);
            AssertEx.SetEqual(renameSpans, tags);

            Editor.SendKeys(VirtualKey.Y, VirtualKey.Enter);
            this.VerifyTextContains(@"
class SomeOtherClass
{
    void M()
    {
        y p = new y();
    }
}");

            VisualStudio.Instance.SolutionExplorer.OpenFile(ProjectName, "Class1.cs");
            this.VerifyTextContains(@"
class y
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Rename)]
        public void VerifyRenameCancellation()
        {
            SetUpEditor(@"
class $$Program
{
}");

            VisualStudio.Instance.SolutionExplorer.AddFile(ProjectName, "Class2.cs", @"");
            VisualStudio.Instance.SolutionExplorer.OpenFile(ProjectName, "Class2.cs");
            Editor.SetText(@"
class SomeOtherClass
{
    void M()
    {
        Program p = new Program();
    }
}");
            this.PlaceCaret("Program");

            InlineRenameDialog.Invoke();

            Editor.SendKeys(VirtualKey.Y);
            this.VerifyTextContains(@"class SomeOtherClass
{
    void M()
    {
        y p = new y();
    }
}");

            VisualStudio.Instance.SolutionExplorer.OpenFile(ProjectName, "Class1.cs");
            this.VerifyTextContains(@"
class y
{
}");

            Editor.SendKeys(VirtualKey.Escape);
            this.WaitForAsyncOperations(FeatureAttribute.Rename);

            this.VerifyTextContains(@"
class Program
{
}");

            VisualStudio.Instance.SolutionExplorer.OpenFile(ProjectName, "Class2.cs");
            this.VerifyTextContains(@"
class SomeOtherClass
{
    void M()
    {
        Program p = new Program();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Rename)]
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

            VisualStudio.Instance.SolutionExplorer.AddProject("Project2", WellKnownProjectTemplates.ClassLibrary, LanguageName);
            VisualStudio.Instance.SolutionExplorer.AddProjectReference(fromProjectName: ProjectName, toProjectName: "Project2");

            VisualStudio.Instance.SolutionExplorer.AddFile("Project2", "Class2.cs", @"");
            VisualStudio.Instance.SolutionExplorer.OpenFile("Project2", "Class2.cs");


            Editor.SetText(@"
public class Class2 { static void Main(string [] args) { } }");

            VisualStudio.Instance.SolutionExplorer.OpenFile(ProjectName, "Class1.cs");
            this.PlaceCaret("Class2");

            InlineRenameDialog.Invoke();
            Editor.SendKeys(VirtualKey.Y, VirtualKey.Enter);

            this.VerifyTextContains(@"
class RenameRocks 
{
    static void Main(string[] args)
    {
        y c = null;
        c.ToString();
    }
}");

            VisualStudio.Instance.SolutionExplorer.OpenFile("Project2", "Class2.cs");
            this.VerifyTextContains(@"
public class y { static void Main(string [] args) { } }");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Rename)]
        public void VerifyRenameUndo()
        {
            VerifyCrossProjectRename();

            Editor.SendKeys(Ctrl(VirtualKey.Z));

            this.VerifyTextContains(@"
public class Class2 { static void Main(string [] args) { } }");

            VisualStudio.Instance.SolutionExplorer.OpenFile(ProjectName, "Class1.cs");
            this.VerifyTextContains(@"
class RenameRocks 
{
    static void Main(string[] args)
    {
        Class2 c = null;
        c.ToString();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Rename)]
        public void VerifyRenameInStandaloneFiles()
        {
            VisualStudio.Instance.SolutionExplorer.CloseSolution();
            VisualStudio.Instance.SolutionExplorer.AddStandaloneFile("StandaloneFile1.cs");
            Editor.SetText(@"
class Program
{
    void Foo()
    {
        var ids = 1;
        ids = 2;
    }
}");
            this.PlaceCaret("ids");

            InlineRenameDialog.Invoke();

            Editor.SendKeys(VirtualKey.Y, VirtualKey.Enter);

            this.VerifyTextContains(@"
class Program
{
    void Foo()
    {
        var y = 1;
        y = 2;
    }
}");
        }
    }
}
