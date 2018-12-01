// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Roslyn.Test.Utilities;

using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [TestClass]
    public class CSharpExtractInterfaceDialog : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.CSharp;

        private ExtractInterfaceDialog_OutOfProc ExtractInterfaceDialog => VisualStudioInstance.ExtractInterfaceDialog;

        public CSharpExtractInterfaceDialog( )
            : base( nameof(CSharpExtractInterfaceDialog))
        {
        }

        [TestMethod, TestCategory(Traits.Features.CodeActionsExtractInterface)]
        public void VerifyCancellation()
        {
            SetUpEditor(@"class C$$
{
    public void M() { }
}
");
            VisualStudioInstance.Editor.InvokeCodeActionList();
            VisualStudioInstance.Editor.Verify.CodeAction("Extract Interface...",
                applyFix: true,
                blockUntilComplete: false);

            ExtractInterfaceDialog.VerifyOpen();
            ExtractInterfaceDialog.ClickCancel();
            ExtractInterfaceDialog.VerifyClosed();

            VisualStudioInstance.Editor.Verify.TextContains(@"class C
{
    public void M() { }
}
");
        }

        [TestMethod, TestCategory(Traits.Features.CodeActionsExtractInterface)]
        public void CheckFileName()
        {
            SetUpEditor(@"class C$$
{
    public void M() { }
}
");
            VisualStudioInstance.Editor.InvokeCodeActionList();
            VisualStudioInstance.Editor.Verify.CodeAction("Extract Interface...",
                applyFix: true,
                blockUntilComplete: false);

            ExtractInterfaceDialog.VerifyOpen();

            var targetFileName = ExtractInterfaceDialog.GetTargetFileName();
            Assert.AreEqual(expected: "IC.cs", actual: targetFileName);

            ExtractInterfaceDialog.ClickCancel();
            ExtractInterfaceDialog.VerifyClosed();
        }

        [TestMethod, TestCategory(Traits.Features.CodeActionsExtractInterface)]
        public void VerifySelectAndDeselectAllButtons()
        {
            SetUpEditor(@"class C$$
{
    public void M1() { }
    public void M2() { }
}
");

            VisualStudioInstance.Editor.InvokeCodeActionList();
            VisualStudioInstance.Editor.Verify.CodeAction("Extract Interface...",
                applyFix: true,
                blockUntilComplete: false);

            ExtractInterfaceDialog.VerifyOpen();

            var selectedItems = ExtractInterfaceDialog.GetSelectedItems();
            Assert.AreEqual(
                expected: new[] { "M1()", "M2()" },
                actual: selectedItems);

            ExtractInterfaceDialog.ClickDeselectAll();

            selectedItems = ExtractInterfaceDialog.GetSelectedItems();
            ExtendedAssert.Empty(selectedItems);

            ExtractInterfaceDialog.ClickSelectAll();

            selectedItems = ExtractInterfaceDialog.GetSelectedItems();
            Assert.AreEqual(
                expected: new[] { "M1()", "M2()" },
                actual: selectedItems);

            ExtractInterfaceDialog.ClickCancel();
            ExtractInterfaceDialog.VerifyClosed();
        }

        [TestMethod, TestCategory(Traits.Features.CodeActionsExtractInterface)]
        public void OnlySelectedItemsAreGenerated()
        {
            SetUpEditor(@"class C$$
{
    public void M1() { }
    public void M2() { }
}
");

            VisualStudioInstance.Editor.InvokeCodeActionList();
            VisualStudioInstance.Editor.Verify.CodeAction("Extract Interface...",
                applyFix: true,
                blockUntilComplete: false);

            ExtractInterfaceDialog.VerifyOpen();
            ExtractInterfaceDialog.ClickDeselectAll();
            ExtractInterfaceDialog.ToggleItem("M2()");
            ExtractInterfaceDialog.ClickOK();
            ExtractInterfaceDialog.VerifyClosed();

            var project = new ProjectUtils.Project(ProjectName);
            VisualStudioInstance.SolutionExplorer.OpenFile(project, "Class1.cs");
            VisualStudioInstance.Editor.Verify.TextContains(@"class C : IC
{
    public void M1() { }
    public void M2() { }
}
");

            VisualStudioInstance.SolutionExplorer.OpenFile(project, "IC.cs");
            VisualStudioInstance.Editor.Verify.TextContains(@"interface IC
{
    void M2();
}");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractInterface)]
        public void CheckSameFile()
        {
            SetUpEditor(@"class C$$
{
    public void M() { }
}
");
            VisualStudio.Editor.InvokeCodeActionList();
            VisualStudio.Editor.Verify.CodeAction("Extract Interface...",
                applyFix: true,
                blockUntilComplete: false);

            ExtractInterfaceDialog.VerifyOpen();

            ExtractInterfaceDialog.SelectSameFile();

            ExtractInterfaceDialog.ClickOK();
            ExtractInterfaceDialog.VerifyClosed();

            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.Editor.Verify.TextContains(@"interface IC
{
    void M();
}

class C : IC
{
    public void M() { }
}
");

        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractInterface)]
        public void CheckSameFileOnlySelectedItems()
        {
            SetUpEditor(@"class C$$
{
    public void M1() { }
    public void M2() { }
}
");

            VisualStudio.Editor.InvokeCodeActionList();
            VisualStudio.Editor.Verify.CodeAction("Extract Interface...",
                applyFix: true,
                blockUntilComplete: false);

            ExtractInterfaceDialog.VerifyOpen();
            ExtractInterfaceDialog.ClickDeselectAll();
            ExtractInterfaceDialog.ToggleItem("M2()");
            ExtractInterfaceDialog.SelectSameFile();
            ExtractInterfaceDialog.ClickOK();
            ExtractInterfaceDialog.VerifyClosed();

            VisualStudio.Editor.Verify.TextContains(@"interface IC
{
    void M2();
}

class C : IC
{
    public void M1() { }
    public void M2() { }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractInterface)]
        public void CheckSameFileNamespace()
        {
            SetUpEditor(@"namespace A
{
    class C$$
    {
        public void M() { }
    }
}
");
            VisualStudio.Editor.InvokeCodeActionList();
            VisualStudio.Editor.Verify.CodeAction("Extract Interface...",
                applyFix: true,
                blockUntilComplete: false);

            ExtractInterfaceDialog.VerifyOpen();

            ExtractInterfaceDialog.SelectSameFile();

            ExtractInterfaceDialog.ClickOK();
            ExtractInterfaceDialog.VerifyClosed();

            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.Editor.Verify.TextContains(@"namespace A
{
    interface IC
    {
        void M();
    }

    class C : IC
    {
        public void M() { }
    }
}
");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsExtractInterface)]
        public void CheckSameWithTypes()
        {
            SetUpEditor(@"class C$$
{
    public bool M() => false;
}
");
            VisualStudio.Editor.InvokeCodeActionList();
            VisualStudio.Editor.Verify.CodeAction("Extract Interface...",
                applyFix: true,
                blockUntilComplete: false);

            ExtractInterfaceDialog.VerifyOpen();

            ExtractInterfaceDialog.SelectSameFile();

            ExtractInterfaceDialog.ClickOK();
            ExtractInterfaceDialog.VerifyClosed();

            var project = new ProjectUtils.Project(ProjectName);
            VisualStudio.Editor.Verify.TextContains(@"interface IC
{
    bool M();
}

class C : IC
{
    public bool M() => false;
}
");
        }
    }
}
