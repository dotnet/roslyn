// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ProjName = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils.Project;

namespace Roslyn.VisualStudio.IntegrationTests.VisualBasic
{
    [TestClass]
    public class BasicLineCommit : AbstractEditorTest
    {
        protected override string LanguageName => LanguageNames.VisualBasic;

        public BasicLineCommit( )
            : base( nameof(BasicLineCommit))
        {
        }

        [TestMethod, TestCategory(Traits.Features.LineCommit)]
        public void CaseCorrection()
        {
            VisualStudioInstance.Editor.SetText(@"Module Goo
    Sub M()
Dim x = Sub()
    End Sub
End Module");

            VisualStudioInstance.Editor.PlaceCaret("Sub()", charsOffset: 1);
            VisualStudioInstance.Editor.SendKeys(VirtualKey.Enter);
            VisualStudioInstance.Editor.Verify.CaretPosition(48);
        }

        [TestMethod, TestCategory(Traits.Features.LineCommit)]
        public void UndoWithEndConstruct()
        {
            VisualStudioInstance.Editor.SetText(@"Module Module1
    Sub Main()
    End Sub
    REM
End Module");

            VisualStudioInstance.Editor.PlaceCaret("    REM");
            VisualStudioInstance.Editor.SendKeys("sub", VirtualKey.Escape, " goo()", VirtualKey.Enter);
            VisualStudioInstance.Editor.Verify.TextContains(@"Sub goo()

    End Sub");
            VisualStudioInstance.ExecuteCommand(WellKnownCommandNames.Edit_Undo);
            VisualStudioInstance.Editor.Verify.CaretPosition(54);
        }

        [TestMethod, TestCategory(Traits.Features.LineCommit)]
        void UndoWithoutEndConstruct()
        {
            VisualStudioInstance.Editor.SetText(@"Module Module1

    ''' <summary></summary>
    Sub Main()
    End Sub
End Module");

            VisualStudioInstance.Editor.PlaceCaret("Module1");
            VisualStudioInstance.Editor.SendKeys(VirtualKey.Down, VirtualKey.Enter);
            VisualStudioInstance.Editor.Verify.TextContains(@"Module Module1


    ''' <summary></summary>
    Sub Main()
    End Sub
End Module");
            VisualStudioInstance.Editor.Verify.CaretPosition(18);
            VisualStudioInstance.ExecuteCommand(WellKnownCommandNames.Edit_Undo);
            VisualStudioInstance.Editor.Verify.CaretPosition(16);
        }

        [TestMethod, Ignore("https://github.com/dotnet/roslyn/issues/20991"), TestCategory(Traits.Features.LineCommit)]
        public void CommitOnSave()
        {
            VisualStudioInstance.Editor.SetText(@"Module Module1
    Sub Main()
    End Sub
End Module
");

            VisualStudioInstance.Editor.PlaceCaret("(", charsOffset: 1);
            VisualStudioInstance.Editor.SendKeys("x   as   integer", VirtualKey.Tab);
            VisualStudioInstance.ExecuteCommand("File.SaveSelectedItems");
            VisualStudioInstance.Editor.Verify.TextContains(@"Sub Main(x As Integer)");
            VisualStudioInstance.ExecuteCommand(WellKnownCommandNames.Edit_Undo);
            VisualStudioInstance.Editor.Verify.TextContains(@"Sub Main(x   As   Integer)");
            VisualStudioInstance.Editor.Verify.CaretPosition(45);
        }

        [TestMethod, TestCategory(Traits.Features.LineCommit)]
        public void CommitOnFocusLost()
        {
            VisualStudioInstance.Editor.SetText(@"Module M
    Sub M()
    End Sub
End Module");

            VisualStudioInstance.Editor.PlaceCaret("End Sub", charsOffset: -1);
            VisualStudioInstance.Editor.SendKeys(" ");
            VisualStudioInstance.SolutionExplorer.AddFile(new ProjName(ProjectName), "TestZ.vb", open: true); // Cause focus lost
            VisualStudioInstance.SolutionExplorer.OpenFile(new ProjName(ProjectName), "TestZ.vb"); // Work around https://github.com/dotnet/roslyn/issues/18488
            VisualStudioInstance.Editor.SendKeys("                  ");
            VisualStudioInstance.SolutionExplorer.CloseFile(new ProjName(ProjectName), "TestZ.vb", saveFile: false);
            VisualStudioInstance.Editor.Verify.TextContains(@"
    Sub M()
    End Sub
");
        }

        [TestMethod, TestCategory(Traits.Features.LineCommit)]
        public void CommitOnFocusLostDoesNotFormatWithPrettyListingOff()
        {
            try
            {
                VisualStudioInstance.Workspace.SetPerLanguageOption("PrettyListing", "FeatureOnOffOptions", LanguageNames.VisualBasic, false);
                VisualStudioInstance.Editor.SetText(@"Module M
    Sub M()
    End Sub
End Module");

                VisualStudioInstance.Editor.PlaceCaret("End Sub", charsOffset: -1);
                VisualStudioInstance.Editor.SendKeys(" ");
                VisualStudioInstance.SolutionExplorer.AddFile(new ProjName(ProjectName), "TestZ.vb", open: true); // Cause focus lost
                VisualStudioInstance.SolutionExplorer.OpenFile(new ProjName(ProjectName), "TestZ.vb"); // Work around https://github.com/dotnet/roslyn/issues/18488
                VisualStudioInstance.Editor.SendKeys("                  ");
                VisualStudioInstance.SolutionExplorer.CloseFile(new ProjName(ProjectName), "TestZ.vb", saveFile: false);
                VisualStudioInstance.Editor.Verify.TextContains(@"
    Sub M()
     End Sub
");
            }
            finally
            {
                VisualStudioInstance.Workspace.SetPerLanguageOption("PrettyListing", "FeatureOnOffOptions", LanguageNames.VisualBasic, true);
            }
        }
    }
}
